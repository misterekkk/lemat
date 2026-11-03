// @Role: Implementacja IRozwiazaniaFasada: odczyt migawek Rozwiązania (z własnością Ucznia) i najnowszej Transkrypcji | @DependsOn: IRozwiazanieRepozytorium, ITranskrypcjaRepozytorium | @UsedBy: DI (SolutionsRegistration), Api (planowane TASK-127) | @Task: TASK-126
using Lemat.Domain.Accounts;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Solutions;

public sealed class RozwiazaniaFasada(IRozwiazanieRepozytorium rozwiazania, ITranskrypcjaRepozytorium transkrypcje) : IRozwiazaniaFasada
{
    public async Task<RozwiazanieSkrot?> PobierzDlaUczniaAsync(RozwiazanieId id, UczenId uczen, CancellationToken ct)
    {
        var rozwiazanie = await rozwiazania.PobierzDoOdczytuAsync(id, ct);
        return rozwiazanie is null || rozwiazanie.UczenId != uczen ? null : Skrot(rozwiazanie);
    }

    public async Task<StronaRozwiazan> ListaDlaUczniaAsync(UczenId uczen, StatusRozwiazania? status, PozycjaListyRozwiazan? po, int limit, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        var wiersze = await rozwiazania.ListaDlaUczniaAsync(uczen, status, po, limit + 1, ct);
        return new StronaRozwiazan([.. wiersze.Take(limit).Select(Skrot)], wiersze.Count > limit);
    }

    public async Task<TranskrypcjaSkrot?> PobierzNajnowszaTranskrypcjeAsync(RozwiazanieId id, CancellationToken ct)
    {
        var transkrypcja = await transkrypcje.PobierzNajnowszaAsync(id, ct);
        return transkrypcja is null ? null : Skrot(transkrypcja);
    }

    private static RozwiazanieSkrot Skrot(Rozwiazanie r) => new(
        r.Id,
        r.UczenId,
        r.ZadanieId,
        r.ZadanieWersjaId,
        r.ElementSciezkiId,
        r.PrzypisanieId,
        r.Status,
        r.LiczbaPrzeslan,
        r.LiczbaPonowienRozpoznania,
        r.KodBleduRozpoznawania,
        r.NajnowszaTranskrypcjaId,
        r.NajnowszaWersjaTranskrypcji,
        r.PrzeslanoAt,
        r.TranskrypcjaGotowaAt,
        r.ZatwierdzonoAt,
        r.OcenaWystawionaAt,
        r.CzasSystemowy,
        r.CreatedAt,
        r.UpdatedAt,
        r.Version,
        [.. r.Zdjecia.OrderBy(z => z.PrzeslanieNr).ThenBy(z => z.Kolejnosc)
            .Select(z => new ZdjecieSkrot(z.Id, z.PrzeslanieNr, z.Kolejnosc, z.KluczObiektu, z.Szerokosc, z.Wysokosc, z.UsunietoAt is null))]);

    private static TranskrypcjaSkrot Skrot(Transkrypcja t) => new(
        t.Id,
        t.RozwiazanieId,
        t.Wersja,
        t.Zrodlo,
        t.PrzeslanieNr,
        t.ZgodnoscZZadaniem,
        t.CreatedAt,
        [.. t.Kroki.OrderBy(k => k.Nr).Select(k => new KrokSkrot(k.Nr, k.Tekst, k.Latex, k.PewnoscOdczytu))]);
}
