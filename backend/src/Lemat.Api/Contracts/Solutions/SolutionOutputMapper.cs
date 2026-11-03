// @Role: Składanie RozwiazanieWyjscie z migawek modułów Rozwiązania, Ocenianie i Treści: Transkrypcja (stałe pola POC), Ocena z opisami Kryteriów, nastepne_odpytanie_ms (1000 w stanach przejściowych) | @DependsOn: IRozwiazaniaFasada, IOcenianieFasada, ITresciFasada | @UsedBy: EndpointyRozwiazan | @Task: TASK-127
using Lemat.Application.Content;
using Lemat.Application.Grading;
using Lemat.Application.Solutions;
using Lemat.Domain.Solutions;

namespace Lemat.Api.Contracts.Solutions;

public sealed class MapperRozwiazan(IRozwiazaniaFasada rozwiazania, IOcenianieFasada oceny, ITresciFasada tresci)
{
    public const int OdstepOdpytaniaMs = 1000;

    public async Task<RozwiazanieWyjscie> ZlozAsync(RozwiazanieSkrot r, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(r);

        TranskrypcjaWyjscie? transkrypcja = null;
        if (r.NajnowszaTranskrypcjaId is not null && await rozwiazania.PobierzNajnowszaTranskrypcjeAsync(r.Id, ct) is { } t)
        {
            transkrypcja = Transkrypcja(t);
        }

        OcenaWyjscie? ocena = null;
        if (r.Status == StatusRozwiazania.Ocenione && await oceny.PobierzAktualnaAsync(r.Id, r.ZadanieWersjaId, ct) is { } o)
        {
            ocena = Ocena(o, await tresci.PobierzZadanieDoOcenyAsync(r.ZadanieWersjaId, ct));
        }

        return new RozwiazanieWyjscie(
            r.Id.Wartosc,
            r.ZadanieId.Wartosc,
            r.ZadanieWersjaId.Wartosc,
            r.Status,
            r.ElementSciezkiId?.Wartosc,
            r.PrzypisanieId,
            [.. r.Zdjecia.Select(z => new ZdjecieRozwiazaniaWyjscie(z.Id, (short)z.PrzeslanieNr, (short)z.Kolejnosc, z.Dostepne))],
            (short)r.LiczbaPrzeslan,
            (short)r.LiczbaPonowienRozpoznania,
            r.KodBleduRozpoznawania,
            transkrypcja,
            ocena,
            null,
            r.Version,
            r.CreatedAt,
            r.UpdatedAt,
            NastepneOdpytanie(r.Status),
            null);
    }

    /// <summary>1000 ms w stanach przejściowych POC, <c>null</c> w pozostałych (kontrakt POC 7.5).</summary>
    public static int? NastepneOdpytanie(StatusRozwiazania status) => status switch
    {
        StatusRozwiazania.Przeslane or StatusRozwiazania.Rozpoznawanie or StatusRozwiazania.Zatwierdzone or StatusRozwiazania.Ocenianie => OdstepOdpytaniaMs,
        _ => null,
    };

    private static TranskrypcjaWyjscie Transkrypcja(TranskrypcjaSkrot t) => new(
        t.Id.Wartosc,
        (short)t.Wersja,
        t.Zrodlo,
        (short)t.PrzeslanieNr,
        Normalizuj(t.ZgodnoscZZadaniem),
        [.. t.Kroki.Select(k => new KrokTranskrypcjiWyjscie((short)k.Nr, k.Tekst, k.Latex, Normalizuj(k.PewnoscOdczytu), [], false, null, false, false, false, []))],
        t.CreatedAt,
        null,
        false,
        null);

    private static OcenaWyjscie Ocena(OcenaSkrot o, ZadanieDoOceny? zadanie)
    {
        var kryteria = zadanie?.KryteriaOceniania.Where(k => k.Wariant == o.Wariant).ToDictionary(k => k.NrKryterium) ?? [];
        return new OcenaWyjscie(
            o.Id.Wartosc,
            (short)o.Wersja,
            o.Status,
            o.Zrodlo,
            o.Widocznosc,
            o.Punkty,
            o.PunktyMax,
            (short)o.Wariant.Wartosc,
            [.. o.Kryteria.OrderBy(k => k.NrKryterium).Select(k => new KryteriumOcenyWyjscie(
                (short)k.NrKryterium,
                kryteria.TryGetValue(k.NrKryterium, out var opis) ? opis.Opis : string.Empty,
                k.Punkty,
                kryteria.TryGetValue(k.NrKryterium, out var maks) ? maks.PunktyMax : 0,
                Normalizuj(k.Prawdopodobienstwo)))],
            (short?)o.PierwszyBlednyKrok,
            o.BrakujacyKrok,
            o.TypBledu,
            (short?)o.KrokPodejrzanyOdczyt,
            o.MozliwyBladOdczytu,
            o.Wskazowka,
            false,
            o.Uzasadnienie,
            Normalizuj(o.PewnoscOceny),
            null,
            null,
            o.CreatedAt);
    }

    // numeric(4,3) z bazy ma skalę 3 (0.960); odpowiedź ma mieć najkrótszy zapis liczby (0.96, 1).
    private static decimal? Normalizuj(decimal? wartosc) => wartosc / 1.000000000000000000000000000000000m;
}
