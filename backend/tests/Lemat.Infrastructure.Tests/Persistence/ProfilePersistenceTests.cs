// @Role: Testy bazy modułu Profil (PostgreSQL): założenie Profilu, inicjalizacja Opanowania, waga Wyniku (Ocena wstępna 0,5, kolejna próba), AKTUALIZUJ_PROFIL z Elo i atomową kalibracją trudności, odtworzenie po unieważnieniu, Test diagnostyczny (ux_test_jeden, propagacja w Dziale), wersja Profilu | @DependsOn: BazaTestowa, ProfilFasada, repozytoria EF Profilu i Treści, KontaFasada, JednostkaPracyEf | @UsedBy: dotnet test | @Task: TASK-019
using Lemat.Application.Accounts;
using Lemat.Application.Content;
using Lemat.Application.Profile;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Repositories.Accounts;
using Lemat.Infrastructure.Persistence.Repositories.Content;
using Lemat.Infrastructure.Persistence.Repositories.Profile;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class ProfilePersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly UmiejetnoscId UmiejetnoscA = new(501);
    private static readonly UmiejetnoscId UmiejetnoscB = new(502);
    private static readonly UmiejetnoscId UmiejetnoscC = new(503);
    private static readonly UmiejetnoscId UmiejetnoscInnegoDzialu = new(601);

    private BazaTestowa _baza = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _baza = await BazaTestowa.UtworzAsync(postgres);
        await using (var kontekst = _baza.Kontekst())
        {
            var dzialy = new DzialRepozytoriumEf(kontekst);
            var umiejetnosci = new UmiejetnoscRepozytoriumEf(kontekst);
            dzialy.Dodaj(Dzial.Utworz(new DzialId(5), "V", "Funkcje", false, 5));
            dzialy.Dodaj(Dzial.Utworz(new DzialId(6), "VI", "Ciągi", false, 6));
            umiejetnosci.Dodaj(Umiejetnosc.Utworz(UmiejetnoscA, new DzialId(5), "P.V.1", "Funkcja liniowa", false, 0.4m));
            umiejetnosci.Dodaj(Umiejetnosc.Utworz(UmiejetnoscB, new DzialId(5), "P.V.2", "Funkcja kwadratowa", false, 0.3m));
            umiejetnosci.Dodaj(Umiejetnosc.Utworz(UmiejetnoscC, new DzialId(5), "P.V.3", "Funkcja wykładnicza", false, 0.3m));
            umiejetnosci.Dodaj(Umiejetnosc.Utworz(UmiejetnoscInnegoDzialu, new DzialId(6), "P.VI.1", "Ciąg arytmetyczny", false, 1m));
            await kontekst.SaveChangesAsync(Ct);
        }

        // Odpowiedź (TASK-020) i Ocena (TASK-021) powstaną w kolejnych zadaniach — w tym teście ich klucze obce są zdjęte.
        await _baza.WykonajAsync("ALTER TABLE wynik_zadania DROP CONSTRAINT wynik_zadania_odpowiedz_id_fkey");
        await _baza.WykonajAsync("ALTER TABLE wynik_zadania DROP CONSTRAINT wynik_zadania_ocena_id_fkey");
    }

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    // ---------- pomocnicy ----------

    private async Task<UczenId> NowyUczenAsync(string email = "u@example.com")
    {
        await using var kontekst = _baza.Kontekst();
        var konta = new KontaFasada(
            new UzytkownikRepozytoriumEf(kontekst),
            new UczenRepozytoriumEf(kontekst),
            new NauczycielRepozytoriumEf(kontekst),
            new JednostkaPracyEf(kontekst),
            _baza.Zegar);
        var konto = await konta.ZarejestrujAsync(
            new RejestracjaKontaWejscie(RolaUzytkownika.Uczen, email, "argon2id$hash", "Jan", "Kowalski", true, AutozatwierdzNauczyciela: true), Ct);
        return UczenId.Z(konto.UzytkownikId);
    }

    private Task<Zadanie> NoweZadanieAsync(RodzajZadania rodzaj, int poziom, UmiejetnoscId glowna, params UmiejetnoscId[] dodatkowe) =>
        ZadaniaTestowe.UtworzAsync(_baza, rodzaj, poziom, glowna, dodatkowe, Ct);

    private async Task<T> ZFasadaAsync<T>(Func<IProfilFasada, Task<T>> operacja)
    {
        await using var kontekst = _baza.Kontekst();
        var jednostka = new JednostkaPracyEf(kontekst);
        var fasada = new ProfilFasada(
            new ProfilRepozytoriumEf(kontekst),
            new WynikZadaniaRepozytoriumEf(kontekst),
            new TestDiagnostycznyRepozytoriumEf(kontekst),
            new ParametryZadaniaDlaProfiluEf(kontekst),
            new AktualizacjaKalibracjiTrudnosciSql(jednostka),
            new TresciFasada(
                new ZadanieRepozytoriumEf(kontekst),
                new WkladkaTeoretycznaRepozytoriumEf(kontekst),
                new DzialRepozytoriumEf(kontekst),
                new UmiejetnoscRepozytoriumEf(kontekst)),
            jednostka,
            _baza.Zegar);
        return await operacja(fasada);
    }

    private async Task ZFasadaAsync(Func<IProfilFasada, Task> operacja) =>
        await ZFasadaAsync(async f =>
        {
            await operacja(f);
            return 0;
        });

    private static NowyWynikZadania Ocena(UczenId uczen, Zadanie zadanie, Guid ocenaId, decimal ulamek, bool wstepna, bool bladOdczytu = false) =>
        new(uczen, zadanie.Id, zadanie.AktualnaWersjaId, ZrodloWyniku.Ocena, null, ocenaId, ulamek, wstepna, bladOdczytu);

    private static NowyWynikZadania Odpowiedz(UczenId uczen, Zadanie zadanie, decimal ulamek) =>
        new(uczen, zadanie.Id, zadanie.AktualnaWersjaId, ZrodloWyniku.Odpowiedz, Guid.NewGuid(), null, ulamek, false, false);

    private Task<decimal> ThetaAsync(UczenId uczen, UmiejetnoscId umiejetnosc) =>
        _baza.SkalarAsync<decimal>($"SELECT theta FROM opanowanie_umiejetnosci WHERE uczen_id = '{uczen.Wartosc}' AND umiejetnosc_id = {umiejetnosc.Wartosc}");

    // ---------- Profil ----------

    [Fact]
    public async Task ZalozenieProfiluJestIdempotenteIMaWersjeJeden()
    {
        var uczen = await NowyUczenAsync();

        await ZFasadaAsync(f => f.ZalozProfilAsync(uczen, Ct));
        await ZFasadaAsync(f => f.ZalozProfilAsync(uczen, Ct));

        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM profil_ucznia WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBe(1);
        (await _baza.SkalarAsync<int>($"SELECT version FROM profil_ucznia WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBe(1);
    }

    [Fact]
    public async Task InicjalizacjaOpanowaniaDodajeUmiejetnosciWStanieDomyslnym()
    {
        var uczen = await NowyUczenAsync();

        var dodane = await ZFasadaAsync(f => f.InicjalizujOpanowanieAsync(uczen, [UmiejetnoscA, UmiejetnoscB], Ct));
        var ponownie = await ZFasadaAsync(f => f.InicjalizujOpanowanieAsync(uczen, [UmiejetnoscA, UmiejetnoscB, UmiejetnoscC], Ct));

        dodane.ShouldBe(2);
        ponownie.ShouldBe(1);
        var profil = await ZFasadaAsync(f => f.PobierzProfilAsync(uczen, Ct));
        profil.ShouldNotBeNull();
        profil.Opanowanie.Count.ShouldBe(3);
        (await ThetaAsync(uczen, UmiejetnoscA)).ShouldBe(0m);
        (await _baza.SkalarAsync<decimal>($"SELECT poziom_opanowania FROM opanowanie_umiejetnosci WHERE uczen_id = '{uczen.Wartosc}' AND umiejetnosc_id = 501")).ShouldBe(0.310m);
        (await _baza.SkalarAsync<string>($"SELECT inicjalizacja::text FROM opanowanie_umiejetnosci WHERE uczen_id = '{uczen.Wartosc}' AND umiejetnosc_id = 501")).ShouldBe("DOMYSLNA");
    }

    // ---------- Wynik zadania: waga ----------

    [Fact]
    public async Task WynikZOcenyWstepnejMaWageZeroPiecAKolejnaProbaTegoSamegoZadaniaTakze()
    {
        var uczen = await NowyUczenAsync();
        var zadanie = await NoweZadanieAsync(RodzajZadania.OtwarteObliczeniowe, 3, UmiejetnoscA);

        var wstepny = await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanie, Guid.NewGuid(), 0.5m, wstepna: true), Ct));
        var ostateczny = await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanie, Guid.NewGuid(), 1m, wstepna: false), Ct));

        wstepny.ShouldNotBeNull();
        wstepny.Waga.ShouldBe(0.50m);
        wstepny.LiczyDoTrudnosci.ShouldBeFalse();
        ostateczny.ShouldNotBeNull();
        ostateczny.Waga.ShouldBe(0.50m);
        (await _baza.SkalarAsync<decimal>($"SELECT waga FROM wynik_zadania WHERE id = '{wstepny.Id.Wartosc}'")).ShouldBe(0.50m);
        (await _baza.SkalarAsync<bool>($"SELECT liczy_do_trudnosci FROM wynik_zadania WHERE id = '{ostateczny.Id.Wartosc}'")).ShouldBeFalse();
    }

    [Fact]
    public async Task PierwszaProbaOcenyOstatecznejMaWageJedenALiczyDoTrudnosci()
    {
        var uczen = await NowyUczenAsync();
        var zadanie = await NoweZadanieAsync(RodzajZadania.OtwarteObliczeniowe, 3, UmiejetnoscA);

        var wynik = await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanie, Guid.NewGuid(), 1m, wstepna: false), Ct));

        wynik!.Waga.ShouldBe(1.00m);
        wynik.LiczyDoTrudnosci.ShouldBeTrue();
    }

    [Fact]
    public async Task KolejnaProbaPoWiecejNizSzescdziesieciuDniachMaZnowuWageJeden()
    {
        var uczen = await NowyUczenAsync();
        var zadanie = await NoweZadanieAsync(RodzajZadania.OtwarteObliczeniowe, 3, UmiejetnoscA);
        await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanie, Guid.NewGuid(), 1m, wstepna: false), Ct));

        _baza.Zegar.Teraz = _baza.Zegar.Teraz.AddDays(61);
        var pozniej = await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanie, Guid.NewGuid(), 1m, wstepna: false), Ct));

        pozniej!.Waga.ShouldBe(1.00m);
    }

    [Fact]
    public async Task OcenaWstepnaZMozliwymBledemOdczytuNieTworzyWyniku()
    {
        var uczen = await NowyUczenAsync();
        var zadanie = await NoweZadanieAsync(RodzajZadania.OtwarteObliczeniowe, 3, UmiejetnoscA);

        var wynik = await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanie, Guid.NewGuid(), 0m, wstepna: true, bladOdczytu: true), Ct));

        wynik.ShouldBeNull();
        (await _baza.SkalarAsync<long>("SELECT count(*) FROM wynik_zadania")).ShouldBe(0);
    }

    [Fact]
    public async Task RejestracjaTegoSamegoZrodlaJestIdempotentna()
    {
        var uczen = await NowyUczenAsync();
        var zadanie = await NoweZadanieAsync(RodzajZadania.OtwarteObliczeniowe, 3, UmiejetnoscA);
        var ocenaId = Guid.NewGuid();

        var pierwszy = await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanie, ocenaId, 1m, wstepna: false), Ct));
        var drugi = await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanie, ocenaId, 1m, wstepna: false), Ct));

        drugi!.Id.ShouldBe(pierwszy!.Id);
        (await _baza.SkalarAsync<long>("SELECT count(*) FROM wynik_zadania")).ShouldBe(1);
    }

    // ---------- AKTUALIZUJ_PROFIL + kalibracja ----------

    [Fact]
    public async Task ZastosowanieWynikuZamknietegoAktualizujeThetaIKalibrujeTrudnoscAtomowo()
    {
        var uczen = await NowyUczenAsync();
        var zadanie = await NoweZadanieAsync(RodzajZadania.Zamkniete, 2, UmiejetnoscA);
        await ZFasadaAsync(f => f.ZalozProfilAsync(uczen, Ct));
        await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Odpowiedz(uczen, zadanie, 1m), Ct));

        var zastosowane = await ZFasadaAsync(f => f.ZastosujOczekujaceWynikiAsync(uczen, Ct));

        // b = 0,8·(2-3) = -0,8; c = 0,25; theta = 0: P = 0,25 + 0,75·sigma(0,8) = 0,76748; delta = 0,23252
        zastosowane.ShouldBe(1);
        (await ThetaAsync(uczen, UmiejetnoscA)).ShouldBe(0.186m);
        (await _baza.SkalarAsync<int>($"SELECT liczba_wynikow FROM opanowanie_umiejetnosci WHERE uczen_id = '{uczen.Wartosc}' AND umiejetnosc_id = 501")).ShouldBe(1);
        (await _baza.SkalarAsync<bool>($"SELECT zastosowany_w_profilu_at IS NOT NULL FROM wynik_zadania WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBeTrue();
        // b' = -0,8 - 0,4/(1+0)·0,23252 = -0,893
        (await _baza.SkalarAsync<decimal>($"SELECT trudnosc_b FROM kalibracja_trudnosci WHERE zadanie_id = '{zadanie.Id.Wartosc}'")).ShouldBe(-0.893m);
        (await _baza.SkalarAsync<int>($"SELECT liczba_wynikow_kalibracji FROM kalibracja_trudnosci WHERE zadanie_id = '{zadanie.Id.Wartosc}'")).ShouldBe(1);
        (await _baza.SkalarAsync<int>($"SELECT version FROM profil_ucznia WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBe(2);
    }

    [Fact]
    public async Task WynikBezWagiPelnejNieKalibrujeTrudnosciAZastosowanieJestIdempotentne()
    {
        var uczen = await NowyUczenAsync();
        var zadanie = await NoweZadanieAsync(RodzajZadania.OtwarteObliczeniowe, 3, UmiejetnoscA, UmiejetnoscB);
        await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanie, Guid.NewGuid(), 1m, wstepna: true), Ct));

        var pierwsze = await ZFasadaAsync(f => f.ZastosujOczekujaceWynikiAsync(uczen, Ct));
        var drugie = await ZFasadaAsync(f => f.ZastosujOczekujaceWynikiAsync(uczen, Ct));

        pierwsze.ShouldBe(1);
        drugie.ShouldBe(0);
        // waga 0,5; theta = 0, b = 0, c = 0: P = 0,5; główna +0,5·0,8·0,5 = 0,2; dodatkowa +0,5·0,5·0,8·0,5 = 0,1
        (await ThetaAsync(uczen, UmiejetnoscA)).ShouldBe(0.200m);
        (await ThetaAsync(uczen, UmiejetnoscB)).ShouldBe(0.100m);
        (await _baza.SkalarAsync<int>($"SELECT liczba_wynikow_kalibracji FROM kalibracja_trudnosci WHERE zadanie_id = '{zadanie.Id.Wartosc}'")).ShouldBe(0);
    }

    [Fact]
    public async Task UniewaznieniePrzywracaStanDomyslnyIStosujePozostaleWyniki()
    {
        var uczen = await NowyUczenAsync();
        var zadanieA = await NoweZadanieAsync(RodzajZadania.OtwarteObliczeniowe, 3, UmiejetnoscA);
        var zadanieB = await NoweZadanieAsync(RodzajZadania.OtwarteObliczeniowe, 3, UmiejetnoscB);
        var ocenaA = Guid.NewGuid();
        await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanieA, ocenaA, 1m, wstepna: false), Ct));
        _baza.Zegar.Teraz = _baza.Zegar.Teraz.AddMinutes(5);
        await ZFasadaAsync(f => f.ZarejestrujWynikAsync(Ocena(uczen, zadanieB, Guid.NewGuid(), 0m, wstepna: false), Ct));
        await ZFasadaAsync(f => f.ZastosujOczekujaceWynikiAsync(uczen, Ct));
        (await ThetaAsync(uczen, UmiejetnoscA)).ShouldBe(0.400m);

        var uniewaznione = await ZFasadaAsync(f => f.UniewaznWynikiOcenyAsync(ocenaA, PowodUniewaznieniaWyniku.OcenaZastapiona, Ct));
        var odtworzone = await ZFasadaAsync(f => f.ZastosujOczekujaceWynikiAsync(uczen, Ct));
        var ponownie = await ZFasadaAsync(f => f.ZastosujOczekujaceWynikiAsync(uczen, Ct));

        uniewaznione.ShouldBe(1);
        odtworzone.ShouldBe(1);
        ponownie.ShouldBe(0);
        (await ThetaAsync(uczen, UmiejetnoscA)).ShouldBe(0m);
        (await _baza.SkalarAsync<int>($"SELECT liczba_wynikow FROM opanowanie_umiejetnosci WHERE uczen_id = '{uczen.Wartosc}' AND umiejetnosc_id = 501")).ShouldBe(0);
        // odtworzenie używa bieżącego trudnosc_b: po błędnym Wyniku b = 0 + 0,4·0,5 = 0,2, więc P = sigma(-0,2) = 0,450 i theta = -0,8·0,450
        (await ThetaAsync(uczen, UmiejetnoscB)).ShouldBe(-0.360m);
        (await _baza.SkalarAsync<string>($"SELECT powod_uniewaznienia::text FROM wynik_zadania WHERE ocena_id = '{ocenaA}'")).ShouldBe("OCENA_ZASTAPIONA");
        (await _baza.SkalarAsync<bool>($"SELECT uniewaznienie_przetworzone_at IS NOT NULL FROM wynik_zadania WHERE ocena_id = '{ocenaA}'")).ShouldBeTrue();
    }

    // ---------- Test diagnostyczny ----------

    [Fact]
    public async Task DrugiTestNaTenSamPoziomJestOdrzucanyPrzezFasadeIPrzezIndeksUnikalny()
    {
        var uczen = await NowyUczenAsync();
        var zadanie = await NoweZadanieAsync(RodzajZadania.Zamkniete, 2, UmiejetnoscA);

        var test = await ZFasadaAsync(f => f.RozpocznijTestAsync(uczen, PoziomMatury.Podstawowy, [zadanie.AktualnaWersjaId], [new DzialId(6)], Ct));
        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() =>
            ZFasadaAsync(f => f.RozpocznijTestAsync(uczen, PoziomMatury.Podstawowy, [zadanie.AktualnaWersjaId], [], Ct)));

        blad.KodBledu.ShouldBe("TEST_JUZ_WYKONANY");
        test.Status.ShouldBe(StatusTestu.WToku);
        test.Pozycje.Single().Kolejnosc.ShouldBe(1);
        (await _baza.SkalarAsync<string>($"SELECT pominiete_dzialy::text FROM test_diagnostyczny WHERE id = '{test.Id.Wartosc}'")).ShouldBe("{6}");

        var indeks = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"INSERT INTO test_diagnostyczny(id, uczen_id, poziom_matury, status) VALUES ('{Guid.NewGuid()}', '{uczen.Wartosc}', 'PODSTAWOWY', 'W_TOKU')"));
        indeks.ConstraintName.ShouldBe("ux_test_jeden");
    }

    [Fact]
    public async Task PominietyTestMoznaZastapicNowymIDrugiPoziomMaturyJestNiezalezny()
    {
        var uczen = await NowyUczenAsync();
        var zadanie = await NoweZadanieAsync(RodzajZadania.Zamkniete, 2, UmiejetnoscA);
        var pierwszy = await ZFasadaAsync(f => f.RozpocznijTestAsync(uczen, PoziomMatury.Podstawowy, [zadanie.AktualnaWersjaId], [], Ct));

        await ZFasadaAsync(f => f.PominTestAsync(uczen, pierwszy.Id, Ct));
        var drugi = await ZFasadaAsync(f => f.RozpocznijTestAsync(uczen, PoziomMatury.Podstawowy, [zadanie.AktualnaWersjaId], [], Ct));
        var rozszerzony = await ZFasadaAsync(f => f.RozpocznijTestAsync(uczen, PoziomMatury.Rozszerzony, [zadanie.AktualnaWersjaId], [], Ct));

        (await _baza.SkalarAsync<string>($"SELECT status::text FROM test_diagnostyczny WHERE id = '{pierwszy.Id.Wartosc}'")).ShouldBe("POMINIETY");
        (await _baza.SkalarAsync<bool>($"SELECT zakonczono_at IS NOT NULL FROM test_diagnostyczny WHERE id = '{pierwszy.Id.Wartosc}'")).ShouldBeTrue();
        drugi.Id.ShouldNotBe(pierwszy.Id);
        rozszerzony.PoziomMatury.ShouldBe(PoziomMatury.Rozszerzony);
    }

    [Fact]
    public async Task ZakonczenieTestuZapisujeWynikiStosujeEloIPropagujeWDziale()
    {
        var uczen = await NowyUczenAsync();
        var zadanieA = await NoweZadanieAsync(RodzajZadania.Zamkniete, 3, UmiejetnoscA);
        var zadanieB = await NoweZadanieAsync(RodzajZadania.Zamkniete, 3, UmiejetnoscB);
        await ZFasadaAsync(f => f.InicjalizujOpanowanieAsync(uczen, [UmiejetnoscA, UmiejetnoscB, UmiejetnoscC, UmiejetnoscInnegoDzialu], Ct));
        var test = await ZFasadaAsync(f => f.RozpocznijTestAsync(uczen, PoziomMatury.Podstawowy, [zadanieA.AktualnaWersjaId, zadanieB.AktualnaWersjaId], [], Ct));
        _baza.Zegar.Teraz = _baza.Zegar.Teraz.AddMinutes(20);

        await ZFasadaAsync(f => f.ZakonczTestAsync(
            uczen,
            test.Id,
            [
                new OdpowiedzNaPozycje(test.Pozycje[1].Id, Guid.NewGuid(), zadanieB.Id, 0m),
                new OdpowiedzNaPozycje(test.Pozycje[0].Id, Guid.NewGuid(), zadanieA.Id, 1m),
            ],
            Ct));

        var testId = test.Id.Wartosc;
        (await _baza.SkalarAsync<string>($"SELECT status::text FROM test_diagnostyczny WHERE id = '{testId}'")).ShouldBe("ZAKONCZONY");
        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM wynik_zadania WHERE test_diagnostyczny_id = '{testId}' AND waga = 1.00 AND zastosowany_w_profilu_at IS NOT NULL")).ShouldBe(2);
        (await _baza.SkalarAsync<bool>($"SELECT bool_and(created_at = (SELECT zakonczono_at FROM test_diagnostyczny WHERE id = '{testId}')) FROM wynik_zadania WHERE test_diagnostyczny_id = '{testId}'")).ShouldBeTrue();

        // K = 0,8, b = 0, c = 0,25: P = 0,25 + 0,75·0,5 = 0,625; A: +0,8·0,375 = 0,300; B: -0,8·0,625 = -0,500; C: średnia = -0,100
        (await ThetaAsync(uczen, UmiejetnoscA)).ShouldBe(0.300m);
        (await ThetaAsync(uczen, UmiejetnoscB)).ShouldBe(-0.500m);
        (await ThetaAsync(uczen, UmiejetnoscC)).ShouldBe(-0.100m);
        (await _baza.SkalarAsync<string>($"SELECT inicjalizacja::text FROM opanowanie_umiejetnosci WHERE uczen_id = '{uczen.Wartosc}' AND umiejetnosc_id = 501")).ShouldBe("TEST_DIAGNOSTYCZNY");
        (await _baza.SkalarAsync<string>($"SELECT inicjalizacja::text FROM opanowanie_umiejetnosci WHERE uczen_id = '{uczen.Wartosc}' AND umiejetnosc_id = 503")).ShouldBe("PROPAGACJA_Z_DZIALU");
        (await _baza.SkalarAsync<string>($"SELECT inicjalizacja::text FROM opanowanie_umiejetnosci WHERE uczen_id = '{uczen.Wartosc}' AND umiejetnosc_id = 601")).ShouldBe("DOMYSLNA");
        (await _baza.SkalarAsync<int>($"SELECT liczba_wynikow_kalibracji FROM kalibracja_trudnosci WHERE zadanie_id = '{zadanieA.Id.Wartosc}'")).ShouldBe(1);
    }

    [Fact]
    public async Task OdpowiedzSpozaTestuJestOdrzucona()
    {
        var uczen = await NowyUczenAsync();
        var zadanie = await NoweZadanieAsync(RodzajZadania.Zamkniete, 3, UmiejetnoscA);
        var test = await ZFasadaAsync(f => f.RozpocznijTestAsync(uczen, PoziomMatury.Podstawowy, [zadanie.AktualnaWersjaId], [], Ct));

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.ZakonczTestAsync(
            uczen, test.Id, [new OdpowiedzNaPozycje(new PozycjaTestuDiagnostycznegoId(Guid.NewGuid()), Guid.NewGuid(), zadanie.Id, 1m)], Ct)));

        blad.KodBledu.ShouldBe("POZYCJA_TESTU_NIEZNANA");
        (await _baza.SkalarAsync<string>($"SELECT status::text FROM test_diagnostyczny WHERE id = '{test.Id.Wartosc}'")).ShouldBe("W_TOKU");
        (await _baza.SkalarAsync<long>("SELECT count(*) FROM wynik_zadania")).ShouldBe(0);
    }

    [Fact]
    public async Task CudzyTestJestNiewidocznyIZabroniony()
    {
        var uczen = await NowyUczenAsync("a@example.com");
        var obcy = await NowyUczenAsync("b@example.com");
        var zadanie = await NoweZadanieAsync(RodzajZadania.Zamkniete, 3, UmiejetnoscA);
        var test = await ZFasadaAsync(f => f.RozpocznijTestAsync(uczen, PoziomMatury.Podstawowy, [zadanie.AktualnaWersjaId], [], Ct));

        (await ZFasadaAsync(f => f.PobierzTestAsync(obcy, test.Id, Ct))).ShouldBeNull();
        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.PominTestAsync(obcy, test.Id, Ct))))
            .KodBledu.ShouldBe("TEST_NIE_ZNALEZIONY");
        (await ZFasadaAsync(f => f.PobierzTestAsync(uczen, test.Id, Ct)))!.Id.ShouldBe(test.Id);
    }
}
