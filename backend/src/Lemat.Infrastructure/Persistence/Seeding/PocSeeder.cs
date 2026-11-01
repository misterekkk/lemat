// @Role: Idempotentny seeder danych startowych POC: 13 Działów, Umiejętności, 5 Zadań z Kryteriami, 2 konta demo Uczniów, aktywna konfiguracja Mock, kalibrator tożsamościowy i Próg pewności 0.95 (U-10, U-29) | @DependsOn: IJednostkaPracy, repozytoria Treści/Kont/Profilu/Rekomendacji/Modeli/Kalibracji, IHasherHasel, DaneStartowePoc | @UsedBy: Lemat.Tools.Commands.SeedPocCommand, testy seed-poc | @Task: TASK-122
using Lemat.Application.Accounts;
using Lemat.Application.Calibration;
using Lemat.Application.Common;
using Lemat.Application.Content;
using Lemat.Application.Models;
using Lemat.Application.Profile;
using Lemat.Application.Recommendations;
using Lemat.Domain.Accounts;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Models;
using Lemat.Domain.Profile;
using Lemat.Domain.Recommendations;

namespace Lemat.Infrastructure.Persistence.Seeding;

/// <summary>Konto demo wypisywane przez polecenie <c>seed-poc</c> (hasło jawne tylko dla lokalnych kont POC).</summary>
public sealed record KontoDemo(string Email, string Haslo, string Rola);

/// <summary>Co zrobił seeder: liczby nowo dodanych wierszy (drugie uruchomienie daje same zera) i konta demo.</summary>
public sealed record WynikSeedPoc(
    int DodaneDzialy,
    int DodaneUmiejetnosci,
    int DodaneZadania,
    int DodaneKonta,
    int DodaneKonfiguracje,
    IReadOnlyList<KontoDemo> KontaDemo);

public sealed class SeederPoc(
    IJednostkaPracy jednostkaPracy,
    IDzialRepozytorium dzialy,
    IUmiejetnoscRepozytorium umiejetnosci,
    IZadanieRepozytorium zadania,
    IUzytkownikRepozytorium uzytkownicy,
    IUczenRepozytorium uczniowie,
    IProfilRepozytorium profile,
    ISciezkaNaukiRepozytorium sciezki,
    IKonfiguracjaModeluRepozytorium konfiguracje,
    IKalibratorPewnosciRepozytorium kalibratory,
    IProgPewnosciRepozytorium progi,
    IHasherHasel hasher,
    TimeProvider zegar)
{
    public const decimal ProgPoc = 0.95m;

    private const string UzasadnienieProgu = "Próg POC (U-10): stały 0.95 bez ewaluacji ZT-1.";

    private static readonly RolaModelu[] RoleMock = [RolaModelu.RozpoznawaniePisma, RolaModelu.DecyzyjnyGlowny];

    public async Task<WynikSeedPoc> WykonajAsync(DaneStartowePoc dane, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dane);
        var teraz = zegar.GetUtcNow();

        // Każda faza to osobna transakcja: późniejsze fazy odwołują się kluczami obcymi do wierszy wcześniejszych.
        var dodaneDzialy = await jednostkaPracy.WykonajAsync(token => DodajDzialyAsync(dane, token), ct);
        var dodaneUmiejetnosci = await jednostkaPracy.WykonajAsync(token => DodajUmiejetnosciAsync(dane, token), ct);
        var dodaneZadania = await jednostkaPracy.WykonajAsync(token => DodajZadaniaAsync(dane, teraz, token), ct);
        var konta = await jednostkaPracy.WykonajAsync(token => DodajKontaAsync(dane, teraz, token), ct);
        await jednostkaPracy.WykonajAsync(token => DodajUczniowAsync(dane, token), ct);
        await jednostkaPracy.WykonajAsync(token => DodajProfileISciezkiAsync(dane, teraz, token), ct);
        var dodaneKonfiguracje = await DodajKonfiguracjeAsync(teraz, ct);

        return new WynikSeedPoc(
            dodaneDzialy,
            dodaneUmiejetnosci,
            dodaneZadania,
            konta,
            dodaneKonfiguracje,
            [.. dane.Konta.Select(k => new KontoDemo(k.Email, k.Haslo, "UCZEN"))]);
    }

    private async Task<int> DodajDzialyAsync(DaneStartowePoc dane, CancellationToken ct)
    {
        var istniejace = (await dzialy.ListaAsync(ct)).Select(d => d.Id.Wartosc).ToHashSet();
        var nowe = dane.Dzialy.Where(d => !istniejace.Contains(d.Id)).ToList();
        foreach (var d in nowe)
        {
            dzialy.Dodaj(Dzial.Utworz(new DzialId(d.Id), d.Kod, d.Nazwa, d.TylkoRozszerzony, d.Kolejnosc));
        }

        return nowe.Count;
    }

    private async Task<int> DodajUmiejetnosciAsync(DaneStartowePoc dane, CancellationToken ct)
    {
        var istniejace = (await umiejetnosci.ListaAsync(null, ct)).Select(u => u.Id.Wartosc).ToHashSet();
        var nowe = dane.Umiejetnosci.Where(u => !istniejace.Contains(u.Id)).ToList();
        foreach (var u in nowe)
        {
            umiejetnosci.Dodaj(Umiejetnosc.Utworz(new UmiejetnoscId(u.Id), new DzialId(u.DzialId), u.Kod, u.Nazwa, false, u.WagaEgzaminacyjna));
        }

        return nowe.Count;
    }

    private async Task<int> DodajZadaniaAsync(DaneStartowePoc dane, DateTimeOffset teraz, CancellationToken ct)
    {
        var dodane = 0;
        foreach (var z in dane.Zadania)
        {
            if (await zadania.PobierzDoOdczytuAsync(new ZadanieId(z.ZadanieId), ct) is not null)
            {
                continue;
            }

            zadania.Dodaj(Zadanie.Utworz(ZbudujZadanie(z), teraz));
            dodane++;
        }

        return dodane;
    }

    private static NoweZadanie ZbudujZadanie(ZadaniePoc z)
    {
        var wzorzec = new RozwiazanieWzorcowe([.. z.RozwiazanieWzorcowe.Warianty.Select(w =>
            new WariantWzorca(w.Wariant, w.Opis, [.. w.Kroki.Select(k => new KrokWzorcowy(k.Nr, k.Latex, k.Komentarz))]))]);
        var wynik = new WynikKoncowyWzorcowy(
            Enum.Parse<TypWynikuKoncowego>(z.WynikKoncowyWzorcowy.Typ, ignoreCase: true),
            z.WynikKoncowyWzorcowy.Latex,
            z.WynikKoncowyWzorcowy.PostacSympy);
        var kryteria = z.Kryteria
            .Select(k => new KryteriumOceniania(new WariantRozwiazania(k.Wariant), k.NrKryterium, k.Opis, k.PunktyMax, k.CzyWynikKoncowy, k.WymagaNrKryterium))
            .ToList();

        return new NoweZadanie(
            new ZadanieId(z.ZadanieId),
            PochodzenieZadania.BazoweNauczyciel,
            ZasiegZadania.Globalny,
            RodzajZadania.OtwarteObliczeniowe,
            null,
            new DzialId(z.DzialId),
            PoziomMatury.Podstawowy,
            null,
            null,
            null,
            null,
            StatusZadania.Opublikowane,
            new PoziomTrudnosci(z.PoziomTrudnosci),
            false,
            new UmiejetnoscId(z.UmiejetnoscId),
            [],
            new TrescWersji(z.TrescMd, null, wzorzec, wynik, z.PunktyMax, kryteria),
            new ZadanieWersjaId(z.ZadanieWersjaId));
    }

    private async Task<int> DodajKontaAsync(DaneStartowePoc dane, DateTimeOffset teraz, CancellationToken ct)
    {
        var dodane = 0;
        foreach (var k in dane.Konta)
        {
            if (await uzytkownicy.PobierzPoEmailuAsync(k.Email, ct) is not null)
            {
                continue;
            }

            var konto = Uzytkownik.Zaloz(new UzytkownikId(k.Id), RolaUzytkownika.Uczen, k.Email, k.Imie, k.Nazwisko, teraz);
            uzytkownicy.Dodaj(konto, hasher.Haszuj(k.Haslo));
            dodane++;
        }

        return dodane;
    }

    private async Task DodajUczniowAsync(DaneStartowePoc dane, CancellationToken ct)
    {
        foreach (var k in dane.Konta)
        {
            var konto = await uzytkownicy.PobierzDoOdczytuAsync(new UzytkownikId(k.Id), ct);
            if (konto is null || await uczniowie.PobierzDoOdczytuAsync(UczenId.Z(konto.Id), ct) is not null)
            {
                continue;
            }

            uczniowie.Dodaj(Uczen.Utworz(konto, oswiadczenieWieku16: true));
        }
    }

    private async Task DodajProfileISciezkiAsync(DaneStartowePoc dane, DateTimeOffset teraz, CancellationToken ct)
    {
        foreach (var k in dane.Konta)
        {
            var uczen = UczenId.Z(new UzytkownikId(k.Id));
            if (await uczniowie.PobierzDoOdczytuAsync(uczen, ct) is null)
            {
                continue;
            }

            if (await profile.PobierzDoOdczytuAsync(uczen, ct) is null)
            {
                profile.Dodaj(ProfilUcznia.Zaloz(uczen, teraz));
            }

            if (await sciezki.PobierzDoOdczytuAsync(uczen, ct) is null)
            {
                sciezki.Dodaj(SciezkaNauki.Zaloz(new SciezkaNaukiId(GeneratorUuid.Nowy(zegar)), uczen, teraz));
            }
        }
    }

    /// <summary>Aktywne konfiguracje Mock dla ról POC; istniejącej aktywnej konfiguracji roli (także innego dostawcy) nie zmienia.</summary>
    private async Task<int> DodajKonfiguracjeAsync(DateTimeOffset teraz, CancellationToken ct)
    {
        var dodane = 0;
        KonfiguracjaModeluId? decyzyjna = null;
        foreach (var rola in RoleMock)
        {
            var aktywna = await konfiguracje.PobierzAktywnaAsync(rola, ct);
            if (aktywna is null)
            {
                var nowa = KonfiguracjaModelu.Utworz(
                    new KonfiguracjaModeluId(GeneratorUuid.Nowy(zegar)),
                    rola,
                    DostawcyModeli.Mock,
                    "mock",
                    ParametryModelu.Domyslne,
                    "mock-v1",
                    null,
                    teraz);
                nowa.Aktywuj(srodowiskoProdukcyjne: false);
                await jednostkaPracy.WykonajAsync(_ =>
                {
                    konfiguracje.Dodaj(nowa);
                    return Task.CompletedTask;
                }, ct);
                aktywna = nowa;
                dodane++;
            }

            if (rola == RolaModelu.DecyzyjnyGlowny)
            {
                decyzyjna = aktywna.Id;
            }
        }

        if (decyzyjna is { } konfiguracja)
        {
            var rodzaj = RodzajZadania.OtwarteObliczeniowe;
            var kalibrator = await kalibratory.PobierzNajnowszyAsync(konfiguracja, rodzaj, ct);
            if (kalibrator is null)
            {
                kalibrator = KalibratorPewnosci.Tozsamosciowy(new KalibratorPewnosciId(GeneratorUuid.Nowy(zegar)), konfiguracja, rodzaj, teraz);
                var nowy = kalibrator;
                await jednostkaPracy.WykonajAsync(_ =>
                {
                    kalibratory.Dodaj(nowy);
                    return Task.CompletedTask;
                }, ct);
                dodane++;
            }

            if (await progi.PobierzAktywnyAsync(konfiguracja, rodzaj, ct) is null)
            {
                var prog = ProgPewnosci.Utworz(
                    new ProgPewnosciId(GeneratorUuid.Nowy(zegar)),
                    konfiguracja,
                    rodzaj,
                    kalibrator.Id,
                    ProgPoc,
                    ZrodloProgu.Administrator,
                    UzasadnienieProgu,
                    null,
                    null,
                    null,
                    teraz);
                prog.Aktywuj();
                await jednostkaPracy.WykonajAsync(_ =>
                {
                    progi.Dodaj(prog);
                    return Task.CompletedTask;
                }, ct);
                dodane++;
            }
        }

        return dodane;
    }
}
