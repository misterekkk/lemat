// @Role: Handler pracy OCEN_ROZWIAZANIE (9.5, 11.1 podzbiór, U-26): ZATWIERDZONE→OCENIANIE, opóźnienie i wywołanie Agenta oceniającego POZA transakcją, zapis Oceny OSTATECZNEJ + Decyzji modelu (po jednej na Kryterium, Z-4), a następnie OCENIONE + Wynik zadania (waga 1,00 przy pierwszej próbie) + ukończenie elementu Ścieżki; idempotentny | @DependsOn: IRozwiazanieRepozytorium, ITranskrypcjaRepozytorium, IOcenaRepozytorium, ITresciFasada, IModeleFasada, IKalibracjaFasada, IProfilFasada, IRekomendacjeFasada, IAgentOceniajacy, IJednostkaPracy, TimeProvider | @UsedBy: ProcesorPrac (przez IHandlerPracy), DI (GradingRegistration) | @Task: TASK-128
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lemat.Application.Calibration;
using Lemat.Application.Common;
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Application.Content;
using Lemat.Application.Models;
using Lemat.Application.Profile;
using Lemat.Application.Recommendations;
using Lemat.Application.Solutions;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Grading;
using Lemat.Domain.Models;
using Lemat.Domain.Profile;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Grading;

/// <summary>Ładunek pracy OCEN_ROZWIAZANIE (klucze snake_case): <c>rozwiazanie_id</c>, <c>transkrypcja_wersja</c>, <c>mock_scenariusz</c>.</summary>
public sealed record LadunekOcenRozwiazanie(Guid RozwiazanieId, int TranskrypcjaWersja, string? MockScenariusz);

public sealed class OcenRozwiazanieHandler(
    IRozwiazanieRepozytorium rozwiazania,
    IOcenaRepozytorium oceny,
    ITresciFasada tresci,
    IModeleFasada modele,
    IKalibracjaFasada kalibracja,
    IProfilFasada profil,
    IRekomendacjeFasada rekomendacje,
    IAgentOceniajacy agent,
    IJednostkaPracy jednostkaPracy,
    TimeProvider czas,
    LematOpcje opcje) : IHandlerPracy
{
    /// <summary>Wariant Kryteriów w POC: wszystkie Zadania mają jeden sposób rozwiązania (kontrakt POC 6.3).</summary>
    private static readonly WariantRozwiazania Wariant1 = new(1);

    public TypPracy Typ => TypPracy.OcenRozwiazanie;

    public async Task WykonajAsync(PracaWTle praca, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(praca);
        var ladunek = JsonSerializer.Deserialize<LadunekOcenRozwiazanie>(praca.Ladunek, LadunekJson.Opcje)
            ?? throw new InvalidOperationException("Pusty ładunek pracy OCEN_ROZWIAZANIE.");
        var id = new RozwiazanieId(ladunek.RozwiazanieId);

        // Warunek wykonania (13.3): Rozwiązanie zatwierdzone albo w trakcie oceniania (ponowienie); OCENIONE = nic do zrobienia.
        var obecne = await rozwiazania.PobierzDoOdczytuAsync(id, ct);
        if (obecne is null || obecne.Status is not (StatusRozwiazania.Zatwierdzone or StatusRozwiazania.Ocenianie)
            || obecne.ZatwierdzonaTranskrypcjaId is not { } transkrypcjaId)
        {
            return;
        }

        if (obecne.Status == StatusRozwiazania.Zatwierdzone)
        {
            await jednostkaPracy.WykonajAsync(
                async token =>
                {
                    var r = await rozwiazania.PobierzAsync(id, token);
                    if (r is { Status: StatusRozwiazania.Zatwierdzone })
                    {
                        r.RozpocznijOcenianie();
                    }
                },
                ct);
        }

        // Odczyty i „wywołanie modelu” poza transakcją (ADR-056).
        var zadanie = await tresci.PobierzZadanieDoOcenyAsync(obecne.ZadanieWersjaId, ct)
            ?? throw new InvalidOperationException("Nie znaleziono wersji Zadania do oceny.");
        var kryteria = zadanie.KryteriaOceniania.Where(k => k.Wariant == Wariant1).OrderBy(k => k.NrKryterium).ToList();
        var konfiguracja = await modele.PobierzAktywnaKonfiguracjeAsync(RolaModelu.DecyzyjnyGlowny, ct)
            ?? throw new InvalidOperationException("Brak aktywnej konfiguracji modelu dla roli DECYZYJNY_GLOWNY.");
        var aktywna = await kalibracja.PobierzAktywnaKalibracjeAsync(konfiguracja.Id, zadanie.Rodzaj, ct)
            ?? throw new InvalidOperationException("Brak aktywnego Progu pewności (Z-3: Ocena automatyczna niedozwolona).");

        var start = czas.GetTimestamp();
        await Task.Delay(TimeSpan.FromMilliseconds(opcje.Poc.OpoznienieOcenyMs), czas, ct);
        var wynik = await agent.OcenAsync(
            new WejscieOceny(
                zadanie.ZadanieId,
                [.. kryteria.Select(k => new KryteriumDoOceny(k.NrKryterium, k.Opis, k.PunktyMax))],
                ladunek.MockScenariusz ?? RozpoznajPismoHandler.ScenariuszDomyslny),
            ct);
        var czasMs = (int)czas.GetElapsedTime(start).TotalMilliseconds;

        var ocena = await ZapiszOceneAsync(obecne, id, transkrypcjaId, zadanie, kryteria, konfiguracja.Id, aktywna, wynik, czasMs, ct);

        // Osobna jednostka pracy po zapisie Oceny: wynik_zadania.ocena_id ma nieodroczony klucz obcy do ocena(id).
        await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var r = await rozwiazania.PobierzAsync(id, token);
                if (r is not { Status: StatusRozwiazania.Ocenianie })
                {
                    return;
                }

                r.ZarejestrujOcene(czas.GetUtcNow());
                await profil.ZarejestrujWynikAsync(
                    new NowyWynikZadania(
                        r.UczenId,
                        r.ZadanieId,
                        r.ZadanieWersjaId,
                        ZrodloWyniku.Ocena,
                        null,
                        ocena.Id.Wartosc,
                        ocena.PunktyMax == 0 ? 0m : (decimal)ocena.Punkty / ocena.PunktyMax,
                        OcenaWstepna: false,
                        MozliwyBladOdczytu: false),
                    token);

                // Rozstrzygnięcie punktu otwartego z TASK-127: techniczny element Ścieżki (U-24) zamykamy po Ocenie.
                if (r.ElementSciezkiId is { } element)
                {
                    await rekomendacje.UkonczAsync(r.UczenId, element, null, token);
                }
            },
            ct);
    }

    private async Task<Ocena> ZapiszOceneAsync(
        Rozwiazanie rozwiazanie,
        RozwiazanieId id,
        TranskrypcjaId transkrypcjaId,
        ZadanieDoOceny zadanie,
        IReadOnlyList<KryteriumOceniania> kryteria,
        KonfiguracjaModeluId konfiguracjaId,
        AktywnaKalibracja aktywna,
        WynikOcenyAgenta wynik,
        int czasMs,
        CancellationToken ct)
    {
        var istniejaca = await oceny.PobierzAktualnaAsync(id, ct);
        if (istniejaca is not null)
        {
            // Ponowienie po awarii między zapisem Oceny a OCENIONE: Ocena jest niezmienna, nie tworzymy drugiej.
            return istniejaca;
        }

        var identyfikatory = await oceny.PobierzIdentyfikatoryKryteriowAsync(zadanie.ZadanieWersjaId, Wariant1, ct);
        var punktyKryteriow = wynik.Kryteria.ToDictionary(k => k.Nr);
        var kryteriaOceny = kryteria
            .Select(k =>
            {
                var p = punktyKryteriow[k.NrKryterium];
                return new OcenaKryterium(identyfikatory[k.NrKryterium], p.Punkty, p.Prawdopodobienstwo);
            })
            .ToList();
        var suma = kryteriaOceny.Sum(k => k.Punkty);
        var teraz = czas.GetUtcNow();
        var ocenaId = new OcenaId(GeneratorUuid.Nowy(czas));

        var ocena = Ocena.Automatyczna(
            ocenaId,
            new DaneOcenyAutomatycznej(
                id,
                Wersja: 1,
                transkrypcjaId,
                StatusOceny.Ostateczna,
                Wariant1,
                PunktySurowe: suma,
                Punkty: suma,
                zadanie.PunktyMax,
                wynik.PierwszyBlednyKrok,
                BrakujacyKrok: false,
                wynik.TypBledu,
                MozliwyBladOdczytu: false,
                KrokPodejrzanyOdczyt: null,
                new PewnoscOceny(wynik.PewnoscOceny),
                new PewnoscSurowa(wynik.PewnoscOceny),
                aktywna.Prog.ProgPewnosciId,
                aktywna.Kalibrator.KalibratorPewnosciId,
                konfiguracjaId,
                wynik.Uzasadnienie,
                wynik.Wskazowka is { } tresc ? new Wskazowka(tresc, ZrodloWskazowki.Szablon) : null,
                kryteriaOceny),
            teraz);

        await jednostkaPracy.WykonajAsync(
            async token =>
            {
                oceny.Dodaj(ocena);
                await modele.ZapiszDecyzjeAsync(Decyzje(rozwiazanie, id, transkrypcjaId, ocenaId, konfiguracjaId, kryteria, wynik, czasMs, teraz), token);
            },
            ct);
        return ocena;
    }

    // Z-4: każda decyzja modelu trafia do decyzja_modelu — po jednym wierszu na Kryterium (pytanie NOUL: czy Kryterium spełnione).
    private List<DecyzjaModelu> Decyzje(
        Rozwiazanie rozwiazanie,
        RozwiazanieId id,
        TranskrypcjaId transkrypcjaId,
        OcenaId ocenaId,
        KonfiguracjaModeluId konfiguracjaId,
        IReadOnlyList<KryteriumOceniania> kryteria,
        WynikOcenyAgenta wynik,
        int czasMs,
        DateTimeOffset teraz)
    {
        var decyzje = new List<DecyzjaModelu>(kryteria.Count);
        foreach (var kryterium in kryteria)
        {
            var punkty = wynik.Kryteria.Single(k => k.Nr == kryterium.NrKryterium);
            var spelnione = punkty.Punkty > 0;
            var p = punkty.Prawdopodobienstwo;
            var pytanie = new PytanieNoul(
                $"Q-OC-POC.K{kryterium.NrKryterium.ToString(CultureInfo.InvariantCulture)}",
                $"Czy Kryterium {kryterium.NrKryterium.ToString(CultureInfo.InvariantCulture)} jest spełnione?");
            var rozklad = new Dictionary<string, decimal>(StringComparer.Ordinal)
            {
                ["tak"] = spelnione ? p : 1m - p,
                ["nie"] = spelnione ? 1m - p : p,
            };
            var skrot = SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{id.Wartosc:N}|{transkrypcjaId.Wartosc:N}|{pytanie.Kod}"));
            decyzje.Add(DecyzjaModelu.Zapisz(
                new DecyzjaModeluId(GeneratorUuid.Nowy(czas)),
                teraz,
                KontekstDecyzji.Ocena,
                ocenaId.Wartosc,
                rozwiazanie.UczenId,
                id.Wartosc,
                konfiguracjaId,
                pytanie,
                DecyzjaSkladowa.ZRozkladu(pytanie.Kod, pytanie.Typ, rozklad),
                czasMs,
                skrot,
                stan: null));
        }

        return decyzje;
    }
}
