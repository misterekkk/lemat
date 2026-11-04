// @Role: Przypadek użycia POST /rozwiazania/{id}/zatwierdzenie (kontrakt POC 7.7): własność Rozwiązania, kontrola stanu, If-Match (version) i aktualności Transkrypcji; w JEDNEJ transakcji TRANSKRYPCJA_GOTOWA→ZATWIERDZONE + zlecenie OCEN_ROZWIAZANIE | @DependsOn: IRozwiazanieRepozytorium, IRozwiazaniaFasada, IZlecaniePrac, IJednostkaPracy, TimeProvider | @UsedBy: Lemat.Api (EndpointyRozwiazan) | @Task: TASK-128
using Lemat.Application.Common;
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Solutions;

public sealed record ZatwierdzRozwiazanieWejscie(UczenId Uczen, RozwiazanieId Rozwiazanie, long OczekiwanaWersja, TranskrypcjaId Transkrypcja, int WersjaTranskrypcji);

public enum RodzajWynikuZatwierdzenia
{
    Zatwierdzone,
    NieZnaleziono,
    NieprawidlowyStan,
    KonfliktWersji,
    TranskrypcjaNieaktualna,
}

/// <summary><see cref="Rozwiazanie"/> — migawka po zatwierdzeniu (<c>Zatwierdzone</c>) albo aktualna (<c>KonfliktWersji</c>, <c>NieprawidlowyStan</c>, <c>TranskrypcjaNieaktualna</c>).</summary>
public sealed record WynikZatwierdzenia(RodzajWynikuZatwierdzenia Rodzaj, RozwiazanieSkrot? Rozwiazanie = null);

public sealed class ZatwierdzRozwiazanieHandler(
    IRozwiazanieRepozytorium rozwiazania,
    IRozwiazaniaFasada fasada,
    IZlecaniePrac zlecanie,
    IJednostkaPracy jednostkaPracy,
    TimeProvider czas)
{
    public async Task<WynikZatwierdzenia> WykonajAsync(ZatwierdzRozwiazanieWejscie wejscie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(wejscie);

        var rodzaj = await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var r = await rozwiazania.PobierzAsync(wejscie.Rozwiazanie, token);
                if (r is null || r.UczenId != wejscie.Uczen)
                {
                    return RodzajWynikuZatwierdzenia.NieZnaleziono;
                }

                // Kolejność kontroli: stan (ponowne zatwierdzenie → 409 niezależnie od If-Match) → wersja (412) → aktualność Transkrypcji (409).
                if (r.Status != StatusRozwiazania.TranskrypcjaGotowa)
                {
                    return RodzajWynikuZatwierdzenia.NieprawidlowyStan;
                }

                if (r.Version != wejscie.OczekiwanaWersja)
                {
                    return RodzajWynikuZatwierdzenia.KonfliktWersji;
                }

                try
                {
                    r.Zatwierdz(wejscie.Transkrypcja, wejscie.WersjaTranskrypcji, czas.GetUtcNow());
                }
                catch (NiezmiennikNaruszonyException e) when (e.KodBledu == Rozwiazanie.KodTranskrypcjaNieaktualna)
                {
                    return RodzajWynikuZatwierdzenia.TranskrypcjaNieaktualna;
                }

                await zlecanie.ZlecAsync(
                    new ZleceniePracy(
                        TypPracy.OcenRozwiazanie,
                        new { RozwiazanieId = r.Id.Wartosc, TranskrypcjaWersja = wejscie.WersjaTranskrypcji, MockScenariusz = RozpoznajPismoHandler.ScenariuszDomyslny },
                        $"{r.Id.Wartosc}:{wejscie.WersjaTranskrypcji}"),
                    token);
                return RodzajWynikuZatwierdzenia.Zatwierdzone;
            },
            ct);

        if (rodzaj == RodzajWynikuZatwierdzenia.NieZnaleziono)
        {
            return new WynikZatwierdzenia(rodzaj);
        }

        var skrot = await fasada.PobierzDlaUczniaAsync(wejscie.Rozwiazanie, wejscie.Uczen, ct);
        return new WynikZatwierdzenia(rodzaj, skrot);
    }
}
