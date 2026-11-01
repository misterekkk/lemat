// @Role: Testy bazy modułu Rekomendacje (PostgreSQL): założenie Ścieżki nauki, element PROPONOWANY z Rekomendacją w jednej transakcji, ux_jeden_proponowany (ELEMENT_JUZ_PROPONOWANY), przejścia 8.5 zapisane w bazie, wersja Ścieżki podnoszona zmianą elementu, filtrowane ładowanie aktywnych elementów, Seria, jsonb Rekomendacji | @DependsOn: BazaTestowa, RekomendacjeFasada, repozytoria EF Rekomendacji, KontaFasada, JednostkaPracyEf | @UsedBy: dotnet test | @Task: TASK-019
using Lemat.Application.Accounts;
using Lemat.Application.Recommendations;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Repositories.Accounts;
using Lemat.Infrastructure.Persistence.Repositories.Content;
using Lemat.Infrastructure.Persistence.Repositories.Recommendations;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class RecommendationsPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly UmiejetnoscId UmiejetnoscA = new(501);

    private BazaTestowa _baza = null!;

    private ZadanieId _zadanie;
    private ZadanieWersjaId _wersja;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _baza = await BazaTestowa.UtworzAsync(postgres);
        await using var kontekst = _baza.Kontekst();
        new DzialRepozytoriumEf(kontekst).Dodaj(Dzial.Utworz(new DzialId(5), "V", "Funkcje", false, 5));
        new UmiejetnoscRepozytoriumEf(kontekst).Dodaj(Umiejetnosc.Utworz(UmiejetnoscA, new DzialId(5), "P.V.1", "Funkcja liniowa", false, 1m));
        await kontekst.SaveChangesAsync(Ct);

        var zadanie = await ZadaniaTestowe.UtworzAsync(_baza, RodzajZadania.OtwarteObliczeniowe, 3, UmiejetnoscA, [], Ct);
        _zadanie = zadanie.Id;
        _wersja = zadanie.AktualnaWersjaId;
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

    private async Task<T> ZFasadaAsync<T>(Func<IRekomendacjeFasada, Task<T>> operacja)
    {
        await using var kontekst = _baza.Kontekst();
        return await operacja(new RekomendacjeFasada(
            new SciezkaNaukiRepozytoriumEf(kontekst),
            new RekomendacjaRepozytoriumEf(kontekst),
            new JednostkaPracyEf(kontekst),
            _baza.Zegar));
    }

    private async Task ZFasadaAsync(Func<IRekomendacjeFasada, Task> operacja) =>
        await ZFasadaAsync(async f =>
        {
            await operacja(f);
            return 0;
        });

    private PropozycjaElementu Zadanie(bool wSerii = false, DateTimeOffset? wygasaAt = null, CelElementu cel = CelElementu.Nauka) =>
        new(TypElementuSciezki.Zadanie, cel, UmiejetnoscA, _zadanie, null, null, wSerii, wygasaAt);

    private static NowaRekomendacja Rekomendacja(RegulaRekomendacji regula = RegulaRekomendacji.Powtorka) => new(
        StrategiaRekomendacji.PolitykaElo,
        regula,
        """{"poziom_matury":"PODSTAWOWY","dni_do_egzaminu":142}""",
        """{"kandydaci":[{"umiejetnosc_id":501,"priorytet":0.029}],"wybor":501,"powrot_do_polityki":false}""",
        0.66666m,
        "Powtórka: ostatnie ćwiczenie z „Funkcja liniowa” było 7 dni temu.",
        """{"umiejetnosc":"Funkcja liniowa","dni":7}""");

    private Task<string> StatusAsync(ElementSciezkiId element) =>
        _baza.SkalarAsync<string>($"SELECT status::text FROM element_sciezki WHERE id = '{element.Wartosc}'");

    // ---------- Ścieżka nauki ----------

    [Fact]
    public async Task ZalozenieSciezkiJestIdempotenteIMaWersjeJeden()
    {
        var uczen = await NowyUczenAsync();

        var pierwsza = await ZFasadaAsync(f => f.ZalozSciezkeAsync(uczen, Ct));
        var druga = await ZFasadaAsync(f => f.ZalozSciezkeAsync(uczen, Ct));

        druga.Id.ShouldBe(pierwsza.Id);
        pierwsza.AktywneElementy.ShouldBeEmpty();
        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBe(1);
        (await _baza.SkalarAsync<int>($"SELECT version FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBe(1);
        (await _baza.SkalarAsync<int>($"SELECT seria_liczba_elementow::integer FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBe(0);
    }

    [Fact]
    public async Task ZaproponowanieZapisujeElementIRekomendacjeWJednejTransakcji()
    {
        var uczen = await NowyUczenAsync();

        var nastepny = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(), Ct));

        nastepny.Element.Status.ShouldBe(StatusElementuSciezki.Proponowany);
        nastepny.Element.Kolejnosc.ShouldBe(1);
        nastepny.Rekomendacja!.Regula.ShouldBe(RegulaRekomendacji.Powtorka);
        var element = nastepny.Element.Id.Wartosc;
        (await _baza.SkalarAsync<string>($"SELECT typ::text FROM element_sciezki WHERE id = '{element}'")).ShouldBe("ZADANIE");
        (await _baza.SkalarAsync<string>($"SELECT cel::text FROM element_sciezki WHERE id = '{element}'")).ShouldBe("NAUKA");
        (await _baza.SkalarAsync<DateTime>($"SELECT wygasa_at FROM element_sciezki WHERE id = '{element}'")).ShouldBe(_baza.Zegar.GetUtcNow().AddHours(24).UtcDateTime);
        (await _baza.SkalarAsync<string>($"SELECT regula FROM rekomendacja WHERE element_sciezki_id = '{element}'")).ShouldBe("R-POWTORKA");
        (await _baza.SkalarAsync<string>($"SELECT strategia::text FROM rekomendacja WHERE element_sciezki_id = '{element}'")).ShouldBe("POLITYKA_ELO");
        (await _baza.SkalarAsync<string>($"SELECT cechy->>'dni_do_egzaminu' FROM rekomendacja WHERE element_sciezki_id = '{element}'")).ShouldBe("142");
        (await _baza.SkalarAsync<string>($"SELECT uzasadnienie_fakty->>'dni' FROM rekomendacja WHERE element_sciezki_id = '{element}'")).ShouldBe("7");
        (await _baza.SkalarAsync<decimal>($"SELECT przewidywane_p_sukcesu FROM rekomendacja WHERE element_sciezki_id = '{element}'")).ShouldBe(0.667m);
        (await _baza.SkalarAsync<int>($"SELECT ostatnia_kolejnosc FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBe(1);
    }

    [Fact]
    public async Task DrugiProponowanyElementJestOdrzuconyAIndeksUnikalnyBlokujeWstawienieBezposrednie()
    {
        var uczen = await NowyUczenAsync();
        var pierwszy = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(), Ct));

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() =>
            ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(), Ct)));

        blad.KodBledu.ShouldBe("ELEMENT_JUZ_PROPONOWANY");
        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM element_sciezki WHERE sciezka_nauki_id = (SELECT id FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}')")).ShouldBe(1);
        var sciezka = await _baza.SkalarAsync<Guid>($"SELECT id FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}'");
        var indeks = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"""
            INSERT INTO element_sciezki(id, sciezka_nauki_id, kolejnosc, typ, umiejetnosc_id, zadanie_id, status, wygasa_at)
            VALUES ('{Guid.NewGuid()}', '{sciezka}', 2, 'ZADANIE', 501, '{_zadanie.Wartosc}', 'PROPONOWANY', now() + interval '1 day')
            """));
        indeks.ConstraintName.ShouldBe("ux_jeden_proponowany");
        pierwszy.Element.Status.ShouldBe(StatusElementuSciezki.Proponowany);
    }

    [Fact]
    public async Task PrzeterminowanyProponowanyWygasaIPozwalaNaNowy()
    {
        var uczen = await NowyUczenAsync();
        var stary = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(), Ct));
        _baza.Zegar.Teraz = _baza.Zegar.Teraz.AddHours(25);

        var nowy = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(RegulaRekomendacji.Najslabsza), Ct));

        (await StatusAsync(stary.Element.Id)).ShouldBe("WYGASLY");
        (await StatusAsync(nowy.Element.Id)).ShouldBe("PROPONOWANY");
        nowy.Element.Kolejnosc.ShouldBe(2);
    }

    [Fact]
    public async Task TerminPrzypisaniaSkracaWygasanieElementu()
    {
        var uczen = await NowyUczenAsync();
        var termin = _baza.Zegar.GetUtcNow().AddHours(6);

        var nastepny = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(wygasaAt: termin), Rekomendacja(RegulaRekomendacji.Przypisanie), Ct));

        (await _baza.SkalarAsync<DateTime>($"SELECT wygasa_at FROM element_sciezki WHERE id = '{nastepny.Element.Id.Wartosc}'")).ShouldBe(termin.UtcDateTime);
        (await _baza.SkalarAsync<string>($"SELECT regula FROM rekomendacja WHERE element_sciezki_id = '{nastepny.Element.Id.Wartosc}'")).ShouldBe("R-PRZYPISANIE");
    }

    // ---------- przejścia 8.5 w bazie ----------

    [Fact]
    public async Task RozpoczecieIUkonczenieZapisujaWersjeZadaniaCzasyIPodnosząWersjeSciezki()
    {
        var uczen = await NowyUczenAsync();
        var nastepny = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(), Ct));
        var element = nastepny.Element.Id;
        _baza.Zegar.Teraz = _baza.Zegar.Teraz.AddMinutes(10);

        var wersjaSciezkiPrzed = await _baza.SkalarAsync<int>($"SELECT version FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}'");
        var rozpoczety = await ZFasadaAsync(f => f.RozpocznijAsync(uczen, element, _wersja, Ct));
        var ponownie = await ZFasadaAsync(f => f.RozpocznijAsync(uczen, element, _wersja, Ct));

        rozpoczety.Status.ShouldBe(StatusElementuSciezki.Rozpoczety);
        ponownie.Status.ShouldBe(StatusElementuSciezki.Rozpoczety);
        (await StatusAsync(element)).ShouldBe("ROZPOCZETY");
        (await _baza.SkalarAsync<DateTime>($"SELECT rozpoczeto_at FROM element_sciezki WHERE id = '{element.Wartosc}'")).ShouldBe(_baza.Zegar.GetUtcNow().UtcDateTime);
        (await _baza.SkalarAsync<DateTime>($"SELECT wygasa_at FROM element_sciezki WHERE id = '{element.Wartosc}'")).ShouldBe(_baza.Zegar.GetUtcNow().AddDays(7).UtcDateTime);
        (await _baza.SkalarAsync<Guid>($"SELECT zadanie_wersja_id FROM element_sciezki WHERE id = '{element.Wartosc}'")).ShouldBe(_wersja.Wartosc);
        (await _baza.SkalarAsync<int>($"SELECT version FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBe(wersjaSciezkiPrzed + 1);

        await ZFasadaAsync(f => f.UkonczAsync(uczen, element, null, Ct));

        (await StatusAsync(element)).ShouldBe("UKONCZONY");
        (await _baza.SkalarAsync<bool>($"SELECT zakonczono_at IS NOT NULL FROM element_sciezki WHERE id = '{element.Wartosc}'")).ShouldBeTrue();
    }

    [Fact]
    public async Task ZabronionePrzejscieKonczySieKodemBezZmianyWBazie()
    {
        var uczen = await NowyUczenAsync();
        var nastepny = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(), Ct));

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() =>
            ZFasadaAsync(f => f.UkonczAsync(uczen, nastepny.Element.Id, null, Ct)));

        blad.KodBledu.ShouldBe("WERSJA_ZADANIA_WYMAGANA");
        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.PrzeczytajAsync(uczen, nastepny.Element.Id, Ct))))
            .KodBledu.ShouldBe("ELEMENT_NIEPRAWIDLOWY_STAN");
        (await StatusAsync(nastepny.Element.Id)).ShouldBe("PROPONOWANY");
    }

    [Fact]
    public async Task PominietyElementTerminalnyJestNadalDostepnyDlaOperacjiAleOdrzucaZmiany()
    {
        var uczen = await NowyUczenAsync();
        var nastepny = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(), Ct));
        await ZFasadaAsync(f => f.PominAsync(uczen, nastepny.Element.Id, Ct));

        var sciezka = await ZFasadaAsync(f => f.PobierzSciezkeAsync(uczen, Ct));
        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() =>
            ZFasadaAsync(f => f.PominAsync(uczen, nastepny.Element.Id, Ct)));

        (await StatusAsync(nastepny.Element.Id)).ShouldBe("POMINIETY");
        sciezka!.AktywneElementy.ShouldBeEmpty();
        blad.KodBledu.ShouldBe("ELEMENT_NIEPRAWIDLOWY_STAN");
        (await ZFasadaAsync(f => f.PobierzNastepnyAsync(uczen, Ct))).ShouldBeNull();
    }

    [Fact]
    public async Task CudzyElementJestNiewidoczny()
    {
        var uczen = await NowyUczenAsync("a@example.com");
        var obcy = await NowyUczenAsync("b@example.com");
        var nastepny = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(), Ct));
        await ZFasadaAsync(f => f.ZalozSciezkeAsync(obcy, Ct));

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() =>
            ZFasadaAsync(f => f.PominAsync(obcy, nastepny.Element.Id, Ct)));

        blad.KodBledu.ShouldBe("ELEMENT_SCIEZKI_NIE_ZNALEZIONY");
        (await ZFasadaAsync(f => f.PobierzNastepnyAsync(obcy, Ct))).ShouldBeNull();
    }

    [Fact]
    public async Task PobranieNastepnegoZwracaProponowanyElementZRekomendacja()
    {
        var uczen = await NowyUczenAsync();
        var zapisany = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(), Ct));

        var nastepny = await ZFasadaAsync(f => f.PobierzNastepnyAsync(uczen, Ct));

        nastepny.ShouldNotBeNull();
        nastepny.Element.Id.ShouldBe(zapisany.Element.Id);
        nastepny.Rekomendacja!.Uzasadnienie.ShouldBe("Powtórka: ostatnie ćwiczenie z „Funkcja liniowa” było 7 dni temu.");
        nastepny.Rekomendacja.PrzewidywanePSukcesu.ShouldBe(0.667m);
        nastepny.Rekomendacja.UzasadnienieFaktyJson.ShouldContain("Funkcja liniowa");
    }

    [Fact]
    public async Task OstatnieElementyZwracajaNajnowszePierwszeBezLadowaniaAgregatu()
    {
        var uczen = await NowyUczenAsync();
        for (var i = 0; i < 3; i++)
        {
            var element = await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(), Rekomendacja(), Ct));
            await ZFasadaAsync(f => f.PominAsync(uczen, element.Element.Id, Ct));
        }

        var ostatnie = await ZFasadaAsync(f => f.OstatnieElementyAsync(uczen, 2, Ct));

        ostatnie.Select(e => e.Kolejnosc).ShouldBe([3, 2]);
        ostatnie.ShouldAllBe(e => e.Status == StatusElementuSciezki.Pominiety);
    }

    // ---------- Seria ----------

    [Fact]
    public async Task SeriaJestZapisywanaWKolumnachIZliczaElementy()
    {
        var uczen = await NowyUczenAsync();
        await ZFasadaAsync(f => f.RozpocznijSerieAsync(uczen, UmiejetnoscA, new PoziomTrudnosci(2), Ct));

        await ZFasadaAsync(f => f.ZaproponujAsync(uczen, Zadanie(wSerii: true), Rekomendacja(RegulaRekomendacji.Seria), Ct));
        await ZFasadaAsync(f => f.ZarejestrujWynikSeriiAsync(uczen, false, null, Ct));

        var sciezka = await ZFasadaAsync(f => f.PobierzSciezkeAsync(uczen, Ct));
        sciezka!.Seria.ShouldNotBeNull();
        sciezka.Seria.LiczbaElementow.ShouldBe(1);
        sciezka.Seria.PoziomTrudnosci.Wartosc.ShouldBe(2);
        sciezka.Seria.KolejnePorazki.ShouldBe(1);
        (await _baza.SkalarAsync<int>($"SELECT seria_umiejetnosc_id FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBe(501);
        (await _baza.SkalarAsync<bool>($"SELECT w_serii FROM element_sciezki WHERE sciezka_nauki_id = '{sciezka.Id.Wartosc}'")).ShouldBeTrue();

        await ZFasadaAsync(f => f.ZakonczSerieAsync(uczen, Ct));

        (await _baza.SkalarAsync<bool>($"SELECT seria_umiejetnosc_id IS NULL AND seria_rozpoczeta_at IS NULL AND seria_poziom_trudnosci IS NULL FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBeTrue();
        (await _baza.SkalarAsync<int>($"SELECT seria_liczba_elementow::integer FROM sciezka_nauki WHERE uczen_id = '{uczen.Wartosc}'")).ShouldBe(0);
        (await ZFasadaAsync(f => f.PobierzSciezkeAsync(uczen, Ct)))!.Seria.ShouldBeNull();
    }
}
