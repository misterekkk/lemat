// @Role: Testy bazy modułu Treści (PostgreSQL): trg_suma_kryteriow, trg_umiejetnosc_glowna_w_dziale, CHECK diagnostyczne, round-trip jsonb (opcje_zamkniete, rozwiazanie_wzorcowe, wynik_koncowy_wzorcowy), Kryteria oceniania, wersjonowanie Zadania, kandydaci doboru, Wkładki, ITresciFasada, DI | @DependsOn: BazaTestowa, repozytoria EF Treści, TresciFasada, JednostkaPracyEf | @UsedBy: dotnet test | @Task: TASK-017
using Lemat.Application.Common;
using Lemat.Application.Content;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Conventions;
using Lemat.Infrastructure.Persistence.Repositories.Content;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class ContentPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly UmiejetnoscId UmiejetnoscA = new(501);
    private static readonly UmiejetnoscId UmiejetnoscB = new(502);
    private static readonly UmiejetnoscId UmiejetnoscInnegoDzialu = new(601);

    private BazaTestowa _baza = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _baza = await BazaTestowa.UtworzAsync(postgres);
        await using var kontekst = _baza.Kontekst();
        var dzialy = new DzialRepozytoriumEf(kontekst);
        var umiejetnosci = new UmiejetnoscRepozytoriumEf(kontekst);
        dzialy.Dodaj(Dzial.Utworz(new DzialId(5), "V", "Funkcje", false, 5));
        dzialy.Dodaj(Dzial.Utworz(new DzialId(6), "VI", "Ciągi", false, 6));
        dzialy.Dodaj(Dzial.Utworz(new DzialId(13), "XIII", "Optymalizacja i rachunek różniczkowy", true, 13));
        umiejetnosci.Dodaj(Umiejetnosc.Utworz(UmiejetnoscA, new DzialId(5), "P.V.1", "Funkcja liniowa", false, 0.4m));
        umiejetnosci.Dodaj(Umiejetnosc.Utworz(UmiejetnoscB, new DzialId(5), "P.V.2", "Funkcja kwadratowa", false, 0.6m, [UmiejetnoscA]));
        umiejetnosci.Dodaj(Umiejetnosc.Utworz(UmiejetnoscInnegoDzialu, new DzialId(6), "P.VI.1", "Ciąg arytmetyczny", false, 1m));
        await kontekst.SaveChangesAsync(Ct);
    }

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    // ---------- pomocnicy ----------

    private static RozwiazanieWzorcowe Wzorzec() => new(
    [
        new WariantWzorca(1, "Wyznaczenie wierzchołka", [new KrokWzorcowy(1, @"f(x) = -2x^2 + 8x - 3", "postać ogólna"), new KrokWzorcowy(2, @"p = -\frac{b}{2a} = 2", "odcięta")]),
        new WariantWzorca(2, "Postać kanoniczna", [new KrokWzorcowy(1, @"f(x) = -2(x-2)^2 + 5", "kanoniczna")]),
    ]);

    private static TrescWersji Obliczeniowa(string tresc = "Wyznacz wartość największą funkcji $f(x) = -2x^2 + 8x - 3$ & podaj <wynik>.") => new(
        tresc,
        null,
        Wzorzec(),
        new WynikKoncowyWzorcowy(TypWynikuKoncowego.Zbior, @"x \in \{-1, 3\}", "FiniteSet(-1, 3)"),
        3,
        [
            new KryteriumOceniania(new WariantRozwiazania(1), 1, "Wierzchołek", 1, false, null),
            new KryteriumOceniania(new WariantRozwiazania(1), 2, "Wartość", 1, false, 1),
            new KryteriumOceniania(new WariantRozwiazania(1), 3, "Wynik końcowy", 1, true, 2),
            new KryteriumOceniania(new WariantRozwiazania(2), 1, "Postać kanoniczna", 2, false, null),
            new KryteriumOceniania(new WariantRozwiazania(2), 2, "Wynik końcowy", 1, true, 1),
        ]);

    private static TrescWersji Zamknieta(OpcjeZamkniete opcje) => new("Wskaż poprawną odpowiedź.", opcje, Wzorzec(), null, 1, []);

    private static OpcjeZamkniete WyborJednokrotny() => OpcjeZamkniete.WyborJednokrotny(
        [new OpcjaWyboru("A", "$x = 2$"), new OpcjaWyboru("B", "$x = -2$"), new OpcjaWyboru("C", "$x = 4$"), new OpcjaWyboru("D", "$x = 0$")], "B");

    private static OpcjeZamkniete PrawdaFalsz() => OpcjeZamkniete.PrawdaFalsz(
        [new StwierdzeniePrawdaFalsz("1", "Funkcja jest rosnąca.", true), new StwierdzeniePrawdaFalsz("2", "Miejscem zerowym jest $3$.", false)]);

    private NoweZadanie Nowe(
        RodzajZadania rodzaj = RodzajZadania.OtwarteObliczeniowe,
        TrescWersji? tresc = null,
        FormaZamknieta? forma = null,
        ZasiegZadania zasieg = ZasiegZadania.Globalny,
        StatusZadania status = StatusZadania.Opublikowane,
        PoziomMatury poziomMatury = PoziomMatury.Podstawowy,
        UmiejetnoscId? glowna = null,
        IReadOnlyList<UmiejetnoscId>? dodatkowe = null,
        int poziom = 3,
        bool diagnostyczne = false,
        DzialId? dzial = null) => new(
            new ZadanieId(GeneratorUuid.Nowy(_baza.Zegar)),
            PochodzenieZadania.BazoweNauczyciel,
            zasieg,
            rodzaj,
            forma ?? (rodzaj == RodzajZadania.Zamkniete ? FormaZamknieta.WyborJednokrotny : null),
            dzial ?? new DzialId(5),
            poziomMatury,
            null,
            null,
            null,
            null,
            status,
            new PoziomTrudnosci(poziom),
            diagnostyczne,
            glowna ?? UmiejetnoscA,
            dodatkowe ?? [],
            tresc ?? (rodzaj == RodzajZadania.Zamkniete ? Zamknieta(WyborJednokrotny()) : Obliczeniowa()));

    private async Task<Zadanie> ZapiszAsync(NoweZadanie dane)
    {
        var zadanie = Zadanie.Utworz(dane, _baza.Zegar.GetUtcNow());
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);
        await uow.WykonajAsync(_ =>
        {
            new ZadanieRepozytoriumEf(kontekst).Dodaj(zadanie);
            return Task.CompletedTask;
        }, Ct);
        return zadanie;
    }

    private static TresciFasada Fasada(LematDbContext kontekst) => new(
        new ZadanieRepozytoriumEf(kontekst),
        new WkladkaTeoretycznaRepozytoriumEf(kontekst),
        new DzialRepozytoriumEf(kontekst),
        new UmiejetnoscRepozytoriumEf(kontekst));

    private static string Id(ZadanieId id) => id.Wartosc.ToString();

    private async Task<UzytkownikId> UtworzUzytkownikaAsync(string email, string rola = "NAUCZYCIEL")
    {
        var id = new UzytkownikId(GeneratorUuid.Nowy(_baza.Zegar));
        await _baza.WykonajAsync(
            $"INSERT INTO uzytkownik(id, email, haslo_hash, rola, status, imie, nazwisko, version) VALUES ('{id.Wartosc}', '{email}', 'h', '{rola}', 'AKTYWNE', 'A', 'B', 1)");
        return id;
    }

    // ---------- Działy i Umiejętności ----------

    [Fact]
    public async Task DzialyIUmiejetnosciCzytaneSaWKolejnosciZWymaganiami()
    {
        await using var kontekst = _baza.Kontekst();
        var fasada = Fasada(kontekst);

        var dzialy = await fasada.ListaDzialowAsync(Ct);
        var wszystkie = await fasada.ListaUmiejetnosciAsync(null, Ct);
        var dzialuV = await fasada.ListaUmiejetnosciAsync(new DzialId(5), Ct);

        dzialy.Select(d => d.Kod).ShouldBe(["V", "VI", "XIII"]);
        dzialy.Single(d => d.Kod == "XIII").TylkoRozszerzony.ShouldBeTrue();
        wszystkie.Select(u => u.Kod).ShouldBe(["P.V.1", "P.V.2", "P.VI.1"]);
        dzialuV.Select(u => u.Id).ShouldBe([UmiejetnoscA, UmiejetnoscB]);
        dzialuV.Single(u => u.Id == UmiejetnoscB).Wymagane.ShouldBe([UmiejetnoscA]);
        dzialuV.Single(u => u.Id == UmiejetnoscA).Wymagane.ShouldBeEmpty();
        dzialuV.Single(u => u.Id == UmiejetnoscB).WagaEgzaminacyjna.ShouldBe(0.6m);
    }

    [Fact]
    public async Task WagaEgzaminacyjnaPozaZakresemOdrzucaCheckBazy()
    {
        var blad = await Should.ThrowAsync<PostgresException>(() =>
            _baza.WykonajAsync("INSERT INTO umiejetnosc(id, dzial_id, kod, nazwa, waga_egzaminacyjna) VALUES (900, 5, 'X', 'x', 1.5)"));

        blad.SqlState.ShouldBe("23514");
    }

    // ---------- round-trip jsonb ----------

    [Fact]
    public async Task ZadanieObliczenioweZapisujeIOdczytujeWszystkieKolumnyJsonb()
    {
        var zadanie = await ZapiszAsync(Nowe(dodatkowe: [UmiejetnoscB]));

        await using var kontekst = _baza.Kontekst();
        var wczytane = (await new ZadanieRepozytoriumEf(kontekst).PobierzDoOdczytuAsync(zadanie.Id, Ct))!;
        var wersja = wczytane.AktualnaWersja;

        wersja.TrescMd.ShouldBe(zadanie.AktualnaWersja.TrescMd);
        wersja.OpcjeZamkniete.ShouldBeNull();
        wersja.RozwiazanieWzorcowe.Warianty.Count.ShouldBe(2);
        wersja.RozwiazanieWzorcowe.Warianty[0].Kroki[1].ShouldBe(new KrokWzorcowy(2, @"p = -\frac{b}{2a} = 2", "odcięta"));
        wersja.RozwiazanieWzorcowe.Warianty[1].Opis.ShouldBe("Postać kanoniczna");
        wersja.WynikKoncowyWzorcowy.ShouldBe(new WynikKoncowyWzorcowy(TypWynikuKoncowego.Zbior, @"x \in \{-1, 3\}", "FiniteSet(-1, 3)"));
        wersja.PunktyMax.ShouldBe(3);
        wersja.NrWersji.ShouldBe(1);
        wersja.ZastapionaAt.ShouldBeNull();
        wczytane.Status.ShouldBe(StatusZadania.Opublikowane);
        wczytane.Zasieg.ShouldBe(ZasiegZadania.Globalny);
        wczytane.Rodzaj.ShouldBe(RodzajZadania.OtwarteObliczeniowe);
        wczytane.PoziomTrudnosciNominalny.Wartosc.ShouldBe(3);
        wczytane.UmiejetnoscGlowna.ShouldBe(UmiejetnoscA);
        wczytane.UmiejetnosciDodatkowe.ShouldBe([UmiejetnoscB]);
        wczytane.AktualnaWersjaId.ShouldBe(wersja.Id);
        wczytane.Version.ShouldBe(1);

        // postać w bazie: klucze snake_case, wartości wyliczeniowe UPPER_SNAKE (7.5.2, 7.5.3)
        var id = Id(zadanie.Id);
        (await _baza.SkalarAsync<string>($"SELECT jsonb_path_query_first(rozwiazanie_wzorcowe, '$.warianty[0].kroki[0].latex') #>> '{{}}' FROM zadanie_wersja WHERE zadanie_id = '{id}'"))
            .ShouldBe("f(x) = -2x^2 + 8x - 3");
        (await _baza.SkalarAsync<string>($"SELECT wynik_koncowy_wzorcowy ->> 'typ' FROM zadanie_wersja WHERE zadanie_id = '{id}'")).ShouldBe("ZBIOR");
        (await _baza.SkalarAsync<string>($"SELECT wynik_koncowy_wzorcowy ->> 'sympy' FROM zadanie_wersja WHERE zadanie_id = '{id}'")).ShouldBe("FiniteSet(-1, 3)");
        (await _baza.SkalarAsync<string>($"SELECT tresc_md FROM zadanie_wersja WHERE zadanie_id = '{id}'")).ShouldContain("& podaj <wynik>.");
        (await _baza.SkalarAsync<bool>($"SELECT opcje_zamkniete IS NULL FROM zadanie_wersja WHERE zadanie_id = '{id}'")).ShouldBeTrue();
        (await _baza.SkalarAsync<string>($"SELECT jsonb_typeof(rozwiazanie_wzorcowe -> 'warianty') FROM zadanie_wersja WHERE zadanie_id = '{id}'")).ShouldBe("array");
    }

    [Fact]
    public async Task OpcjeZamknieteWyboruJednokrotnegoPrzechodzaRoundTrip()
    {
        var zadanie = await ZapiszAsync(Nowe(RodzajZadania.Zamkniete));

        await using var kontekst = _baza.Kontekst();
        var opcje = (await new ZadanieRepozytoriumEf(kontekst).PobierzDoOdczytuAsync(zadanie.Id, Ct))!.AktualnaWersja.OpcjeZamkniete!;

        opcje.Forma.ShouldBe(FormaZamknieta.WyborJednokrotny);
        opcje.Poprawna.ShouldBe("B");
        opcje.Opcje.Select(o => o.Id).ShouldBe(["A", "B", "C", "D"]);
        opcje.Opcje[0].TrescMd.ShouldBe("$x = 2$");
        opcje.Stwierdzenia.ShouldBeEmpty();
        opcje.Ocen(new WyborZamkniety(Opcja: "B")).Punkty.ShouldBe(1);
        var id = Id(zadanie.Id);
        (await _baza.SkalarAsync<string>($"SELECT opcje_zamkniete ->> 'forma' FROM zadanie_wersja WHERE zadanie_id = '{id}'")).ShouldBe("WYBOR_JEDNOKROTNY");
        (await _baza.SkalarAsync<string>($"SELECT opcje_zamkniete ->> 'poprawna' FROM zadanie_wersja WHERE zadanie_id = '{id}'")).ShouldBe("B");
        (await _baza.SkalarAsync<string>($"SELECT forma_zamknieta::text FROM zadanie WHERE id = '{id}'")).ShouldBe("WYBOR_JEDNOKROTNY");
        (await _baza.SkalarAsync<bool>($"SELECT wynik_koncowy_wzorcowy IS NULL FROM zadanie_wersja WHERE zadanie_id = '{id}'")).ShouldBeTrue();
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM kryterium_oceniania k JOIN zadanie_wersja w ON w.id = k.zadanie_wersja_id WHERE w.zadanie_id = '{id}'")).ShouldBe(0);
    }

    [Fact]
    public async Task OpcjeZamknieteMajaPostacPrawdaFalszRoundTrip()
    {
        var zadanie = await ZapiszAsync(Nowe(RodzajZadania.Zamkniete, Zamknieta(PrawdaFalsz()), FormaZamknieta.PrawdaFalsz));

        await using var kontekst = _baza.Kontekst();
        var opcje = (await new ZadanieRepozytoriumEf(kontekst).PobierzDoOdczytuAsync(zadanie.Id, Ct))!.AktualnaWersja.OpcjeZamkniete!;

        opcje.Forma.ShouldBe(FormaZamknieta.PrawdaFalsz);
        opcje.Stwierdzenia.ShouldBe(
            [new StwierdzeniePrawdaFalsz("1", "Funkcja jest rosnąca.", true), new StwierdzeniePrawdaFalsz("2", "Miejscem zerowym jest $3$.", false)]);
        opcje.Poprawna.ShouldBeNull();
        opcje.Opcje.ShouldBeEmpty();
        var id = Id(zadanie.Id);
        (await _baza.SkalarAsync<string>($"SELECT opcje_zamkniete #>> '{{stwierdzenia,1,poprawna}}' FROM zadanie_wersja WHERE zadanie_id = '{id}'")).ShouldBe("false");
        (await _baza.SkalarAsync<string>($"SELECT forma_zamknieta::text FROM zadanie WHERE id = '{id}'")).ShouldBe("PRAWDA_FALSZ");
    }

    [Theory]
    [InlineData(TypWynikuKoncowego.Wyrazenie, "WYRAZENIE", "x+1")]
    [InlineData(TypWynikuKoncowego.Rownanie, "ROWNANIE", "Eq(x, 1)")]
    [InlineData(TypWynikuKoncowego.Przedzial, "PRZEDZIAL", "Interval(0, 1)")]
    [InlineData(TypWynikuKoncowego.WartoscLogiczna, "WARTOSC_LOGICZNA", "true")]
    [InlineData(TypWynikuKoncowego.Tekst, "TEKST", null)]
    public async Task WszystkieTypyWynikuKoncowegoPrzechodzaRoundTrip(TypWynikuKoncowego typ, string wBazie, string? sympy)
    {
        var tresc = Obliczeniowa() with { WynikKoncowyWzorcowy = new WynikKoncowyWzorcowy(typ, "wynik", sympy) };
        var zadanie = await ZapiszAsync(Nowe(tresc: tresc));

        await using var kontekst = _baza.Kontekst();
        var wynik = (await new ZadanieRepozytoriumEf(kontekst).PobierzDoOdczytuAsync(zadanie.Id, Ct))!.AktualnaWersja.WynikKoncowyWzorcowy!;

        wynik.Typ.ShouldBe(typ);
        wynik.PostacSympy.ShouldBe(sympy);
        (await _baza.SkalarAsync<string>($"SELECT wynik_koncowy_wzorcowy ->> 'typ' FROM zadanie_wersja WHERE zadanie_id = '{Id(zadanie.Id)}'")).ShouldBe(wBazie);
    }

    [Fact]
    public async Task DowodNieMaWynikuKoncowegoWzorcowego()
    {
        var tresc = new TrescWersji("Udowodnij.", null, Wzorzec(), null, 2,
        [
            new KryteriumOceniania(new WariantRozwiazania(1), 1, "Założenie", 1, false, null),
            new KryteriumOceniania(new WariantRozwiazania(1), 2, "Teza", 1, false, 1),
        ]);
        var zadanie = await ZapiszAsync(Nowe(RodzajZadania.OtwarteZDowodem, tresc));

        await using var kontekst = _baza.Kontekst();
        var wersja = (await new ZadanieRepozytoriumEf(kontekst).PobierzDoOdczytuAsync(zadanie.Id, Ct))!.AktualnaWersja;

        wersja.WynikKoncowyWzorcowy.ShouldBeNull();
        wersja.KryteriaOceniania.Count.ShouldBe(2);
        (await _baza.SkalarAsync<string>($"SELECT rodzaj::text FROM zadanie WHERE id = '{Id(zadanie.Id)}'")).ShouldBe("OTWARTE_Z_DOWODEM");
    }

    // ---------- Kryteria oceniania ----------

    [Fact]
    public async Task KryteriaOcenianiaZapisujaSieWTabeliIOdczytujaWKolejnosci()
    {
        var zadanie = await ZapiszAsync(Nowe());

        await using var kontekst = _baza.Kontekst();
        var kryteria = (await new ZadanieRepozytoriumEf(kontekst).PobierzDoOdczytuAsync(zadanie.Id, Ct))!.AktualnaWersja.KryteriaOceniania;

        kryteria.Count.ShouldBe(5);
        kryteria.ShouldContain(new KryteriumOceniania(new WariantRozwiazania(1), 3, "Wynik końcowy", 1, true, 2));
        kryteria.ShouldContain(new KryteriumOceniania(new WariantRozwiazania(2), 1, "Postać kanoniczna", 2, false, null));
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM kryterium_oceniania k JOIN zadanie_wersja w ON w.id = k.zadanie_wersja_id WHERE w.zadanie_id = '{Id(zadanie.Id)}'")).ShouldBe(5);
        (await _baza.SkalarAsync<int>($"SELECT sum(k.punkty_max)::int FROM kryterium_oceniania k JOIN zadanie_wersja w ON w.id = k.zadanie_wersja_id WHERE w.zadanie_id = '{Id(zadanie.Id)}' AND k.wariant = 2")).ShouldBe(3);
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM kryterium_oceniania k JOIN zadanie_wersja w ON w.id = k.zadanie_wersja_id WHERE w.zadanie_id = '{Id(zadanie.Id)}' AND k.czy_wynik_koncowy")).ShouldBe(2);
    }

    // ---------- triggery i CHECK-i ----------

    [Fact]
    public async Task TriggerSumaKryteriowOdrzucaNiezgodnaSumeNaCommicie()
    {
        var zadanie = Zadanie.Utworz(Nowe(), Teraz);
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => uow.WykonajAsync(async ct =>
        {
            new ZadanieRepozytoriumEf(kontekst).Dodaj(zadanie);
            await kontekst.SaveChangesAsync(ct);
            await using var polecenie = uow.UtworzPolecenie(
                $"UPDATE kryterium_oceniania SET punkty_max = 3 WHERE nr_kryterium = 1 AND wariant = 1 AND zadanie_wersja_id = '{zadanie.AktualnaWersjaId.Wartosc}'");
            await polecenie.ExecuteNonQueryAsync(ct);
        }, Ct));

        blad.KodBledu.ShouldBe("KRYTERIA_NIESPOJNE");
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM zadanie")).ShouldBe(0);
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM kryterium_oceniania")).ShouldBe(0);
    }

    [Fact]
    public async Task TriggerSumaKryteriowOdrzucaDrugieKryteriumWynikuKoncowegoWWariancie()
    {
        var zadanie = Zadanie.Utworz(Nowe(), Teraz);
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => uow.WykonajAsync(async ct =>
        {
            new ZadanieRepozytoriumEf(kontekst).Dodaj(zadanie);
            await kontekst.SaveChangesAsync(ct);
            await using var polecenie = uow.UtworzPolecenie(
                $"UPDATE kryterium_oceniania SET czy_wynik_koncowy = true WHERE nr_kryterium = 2 AND wariant = 1 AND zadanie_wersja_id = '{zadanie.AktualnaWersjaId.Wartosc}'");
            await polecenie.ExecuteNonQueryAsync(ct);
        }, Ct));

        blad.KodBledu.ShouldBe("KRYTERIA_NIESPOJNE");
    }

    [Fact]
    public async Task TriggerUmiejetnoscGlownaWDzialeOdrzucaUmiejetnoscInnegoDzialu()
    {
        var dane = Nowe(glowna: UmiejetnoscInnegoDzialu);

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZapiszAsync(dane));

        blad.KodBledu.ShouldBe("UMIEJETNOSC_GLOWNA_POZA_DZIALEM");
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM zadanie")).ShouldBe(0);
    }

    [Fact]
    public async Task UmiejetnoscDodatkowaMozeNalezecDoInnegoDzialu()
    {
        var zadanie = await ZapiszAsync(Nowe(dodatkowe: [UmiejetnoscInnegoDzialu]));

        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM zadanie_umiejetnosc WHERE zadanie_id = '{Id(zadanie.Id)}'")).ShouldBe(2);
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM zadanie_umiejetnosc WHERE zadanie_id = '{Id(zadanie.Id)}' AND glowna AND umiejetnosc_id = 501")).ShouldBe(1);
    }

    [Theory]
    [InlineData(RodzajZadania.OtwarteObliczeniowe)]
    [InlineData(RodzajZadania.Zamkniete)]
    public async Task DiagnostyczneTylkoDlaZamknietychGlobalnychOdrzucaCheckBazy(RodzajZadania rodzaj)
    {
        var zadanie = await ZapiszAsync(Nowe(rodzaj, zasieg: rodzaj == RodzajZadania.Zamkniete ? ZasiegZadania.Prywatny : ZasiegZadania.Globalny));

        var blad = await Should.ThrowAsync<PostgresException>(() =>
            _baza.WykonajAsync($"UPDATE zadanie SET diagnostyczne = true WHERE id = '{Id(zadanie.Id)}'"));

        blad.SqlState.ShouldBe("23514");
    }

    [Fact]
    public async Task DiagnostyczneZamknieteGlobalneZapisujeSieIJestWPuliDiagnostycznej()
    {
        var zadanie = await ZapiszAsync(Nowe(RodzajZadania.Zamkniete, diagnostyczne: true));

        (await _baza.SkalarAsync<bool>($"SELECT diagnostyczne FROM zadanie WHERE id = '{Id(zadanie.Id)}'")).ShouldBeTrue();
    }

    [Fact]
    public async Task ChecksSpojnosciPochodzeniaIFormyDzialajaWBazie()
    {
        var zadanie = await ZapiszAsync(Nowe());

        var forma = await Should.ThrowAsync<PostgresException>(() =>
            _baza.WykonajAsync($"UPDATE zadanie SET forma_zamknieta = 'PRAWDA_FALSZ' WHERE id = '{Id(zadanie.Id)}'"));
        var arkusz = await Should.ThrowAsync<PostgresException>(() =>
            _baza.WykonajAsync($"UPDATE zadanie SET pochodzenie = 'BAZOWE_ARKUSZ_CKE' WHERE id = '{Id(zadanie.Id)}'"));

        forma.SqlState.ShouldBe("23514");
        arkusz.SqlState.ShouldBe("23514");
    }

    // ---------- wersjonowanie ----------

    [Fact]
    public async Task PoprawaZadaniaTworzyNowaWersjeIZamykaStaraZastapionaAt()
    {
        var zadanie = await ZapiszAsync(Nowe());
        var pierwszaWersja = zadanie.AktualnaWersjaId;
        _baza.Zegar.Teraz = Teraz.AddDays(2);
        var autor = await UtworzUzytkownikaAsync("a@example.com");

        await using (var kontekst = _baza.Kontekst())
        {
            var uow = new JednostkaPracyEf(kontekst);
            await uow.WykonajAsync(async ct =>
            {
                var repozytorium = new ZadanieRepozytoriumEf(kontekst);
                var wczytane = (await repozytorium.PobierzAsync(zadanie.Id, 1, ct))!;
                wczytane.Popraw(Obliczeniowa("Poprawiona treść."), autor, _baza.Zegar.GetUtcNow());
            }, Ct);
        }

        var id = Id(zadanie.Id);
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM zadanie_wersja WHERE zadanie_id = '{id}'")).ShouldBe(2);
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM zadanie_wersja WHERE zadanie_id = '{id}' AND zastapiona_at IS NULL")).ShouldBe(1);
        (await _baza.SkalarAsync<DateTime>($"SELECT zastapiona_at FROM zadanie_wersja WHERE id = '{pierwszaWersja.Wartosc}'")).ShouldBe(Teraz.AddDays(2).UtcDateTime);
        (await _baza.SkalarAsync<int>($"SELECT version FROM zadanie WHERE id = '{id}'")).ShouldBe(2);
        (await _baza.SkalarAsync<int>($"SELECT nr_wersji FROM zadanie_wersja WHERE id = (SELECT aktualna_wersja_id FROM zadanie WHERE id = '{id}')")).ShouldBe(2);
        (await _baza.SkalarAsync<string>($"SELECT tresc_md FROM zadanie_wersja WHERE id = '{pierwszaWersja.Wartosc}'")).ShouldStartWith("Wyznacz wartość największą");
        (await _baza.SkalarAsync<string>($"SELECT utworzona_przez::text FROM zadanie_wersja WHERE zadanie_id = '{id}' AND nr_wersji = 2")).ShouldBe(autor.Wartosc.ToString());
        // Kryteria obu wersji pozostają (stara wersja niezmienna)
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM kryterium_oceniania k JOIN zadanie_wersja w ON w.id = k.zadanie_wersja_id WHERE w.zadanie_id = '{id}' AND w.nr_wersji = 1")).ShouldBe(5);
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM kryterium_oceniania k JOIN zadanie_wersja w ON w.id = k.zadanie_wersja_id WHERE w.zadanie_id = '{id}' AND w.nr_wersji = 2")).ShouldBe(5);

        await using var odczyt = _baza.Kontekst();
        var wczytanePonownie = (await new ZadanieRepozytoriumEf(odczyt).PobierzDoOdczytuAsync(zadanie.Id, Ct))!;
        wczytanePonownie.Wersje.Count.ShouldBe(2);
        wczytanePonownie.AktualnaWersja.NrWersji.ShouldBe(2);
        wczytanePonownie.AktualnaWersja.ZastapionaAt.ShouldBeNull();
        wczytanePonownie.Wersje.Single(w => w.NrWersji == 1).ZastapionaAt.ShouldBe(Teraz.AddDays(2));
        wczytanePonownie.Version.ShouldBe(2);
    }

    [Fact]
    public async Task PoprawaZeStaraWersjaWIfMatchDajeKonfliktWspolbieznosci()
    {
        var zadanie = await ZapiszAsync(Nowe());
        var autor = await UtworzUzytkownikaAsync("g@example.com");
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);

        await Should.ThrowAsync<KonfliktWspolbieznosciException>(() => uow.WykonajAsync(async ct =>
        {
            var wczytane = (await new ZadanieRepozytoriumEf(kontekst).PobierzAsync(zadanie.Id, 7, ct))!;
            wczytane.Popraw(Obliczeniowa("Inna."), autor, Teraz.AddHours(1));
        }, Ct));

        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM zadanie_wersja WHERE zadanie_id = '{Id(zadanie.Id)}'")).ShouldBe(1);
    }

    [Fact]
    public async Task ZmianaStatuModyfikujeTylkoZadanieIPodnosiVersion()
    {
        var zadanie = await ZapiszAsync(Nowe());
        await using (var kontekst = _baza.Kontekst())
        {
            var uow = new JednostkaPracyEf(kontekst);
            await uow.WykonajAsync(async ct => (await new ZadanieRepozytoriumEf(kontekst).PobierzAsync(zadanie.Id, null, ct))!.Wycofaj(), Ct);
        }

        (await _baza.SkalarAsync<string>($"SELECT status::text FROM zadanie WHERE id = '{Id(zadanie.Id)}'")).ShouldBe("WYCOFANE");
        (await _baza.SkalarAsync<int>($"SELECT version FROM zadanie WHERE id = '{Id(zadanie.Id)}'")).ShouldBe(2);
    }

    // ---------- ITresciFasada ----------

    [Fact]
    public async Task FasadaZwracaZadanieDoOcenyDlaWersjiAktualnejIZastapionej()
    {
        var zadanie = await ZapiszAsync(Nowe());
        var pierwsza = zadanie.AktualnaWersjaId;
        var autor = await UtworzUzytkownikaAsync("f@example.com");
        await using (var kontekst = _baza.Kontekst())
        {
            var uow = new JednostkaPracyEf(kontekst);
            await uow.WykonajAsync(async ct =>
                (await new ZadanieRepozytoriumEf(kontekst).PobierzAsync(zadanie.Id, null, ct))!
                    .Popraw(Obliczeniowa("Druga wersja."), autor, Teraz.AddDays(1)), Ct);
        }

        await using var odczyt = _baza.Kontekst();
        var fasada = Fasada(odczyt);
        var stara = (await fasada.PobierzZadanieDoOcenyAsync(pierwsza, Ct))!;
        var aktualna = (await fasada.PobierzZadanieDoOcenyAsync(
            new ZadanieWersjaId(await _baza.SkalarAsync<Guid>($"SELECT aktualna_wersja_id FROM zadanie WHERE id = '{Id(zadanie.Id)}'")), Ct))!;

        stara.NrWersji.ShouldBe(1);
        stara.TrescMd.ShouldStartWith("Wyznacz wartość");
        aktualna.NrWersji.ShouldBe(2);
        aktualna.TrescMd.ShouldBe("Druga wersja.");
        aktualna.ZadanieId.ShouldBe(zadanie.Id);
        aktualna.Rodzaj.ShouldBe(RodzajZadania.OtwarteObliczeniowe);
        aktualna.UmiejetnoscGlowna.ShouldBe(UmiejetnoscA);
        aktualna.DzialId.ShouldBe(new DzialId(5));
        aktualna.PunktyMax.ShouldBe(3);
        aktualna.WynikKoncowyWzorcowy.ShouldNotBeNull();
        aktualna.RozwiazanieWzorcowe.Warianty.Count.ShouldBe(2);
        aktualna.KryteriaOceniania.Select(k => (k.Wariant.Wartosc, k.NrKryterium)).ShouldBe([(1, 1), (1, 2), (1, 3), (2, 1), (2, 2)]);
        (await fasada.PobierzZadanieDoOcenyAsync(new ZadanieWersjaId(Guid.NewGuid()), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task FasadaZwracaZadanieUczniaBezKluczaOdpowiedzi()
    {
        var zadanie = await ZapiszAsync(Nowe(RodzajZadania.Zamkniete, Zamknieta(PrawdaFalsz()), FormaZamknieta.PrawdaFalsz));

        await using var kontekst = _baza.Kontekst();
        var widok = (await Fasada(kontekst).PobierzZadanieUczniaAsync(zadanie.Id, Ct))!;

        widok.Rodzaj.ShouldBe(RodzajZadania.Zamkniete);
        widok.Status.ShouldBe(StatusZadania.Opublikowane);
        widok.Zasieg.ShouldBe(ZasiegZadania.Globalny);
        widok.Opcje!.Forma.ShouldBe(FormaZamknieta.PrawdaFalsz);
        widok.Opcje.Stwierdzenia.ShouldBe([new StwierdzenieDlaUcznia("1", "Funkcja jest rosnąca."), new StwierdzenieDlaUcznia("2", "Miejscem zerowym jest $3$.")]);
        widok.PunktyMax.ShouldBe(1);
        widok.UmiejetnoscGlowna.ShouldBe(UmiejetnoscA);
        (await Fasada(kontekst).PobierzZadanieUczniaAsync(new ZadanieId(Guid.NewGuid()), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task KandydaciDoboruSpelniajaWarunkiKroku7()
    {
        var pasujace = await ZapiszAsync(Nowe(poziom: 2, dodatkowe: [UmiejetnoscB]));
        var pasujaceZamkniete = await ZapiszAsync(Nowe(RodzajZadania.Zamkniete, Zamknieta(PrawdaFalsz()), FormaZamknieta.PrawdaFalsz, poziom: 4));
        var prywatne = await ZapiszAsync(Nowe(zasieg: ZasiegZadania.Prywatny));
        var wycofane = await ZapiszAsync(Nowe(status: StatusZadania.Walidacja));
        var innyPoziom = await ZapiszAsync(Nowe(poziomMatury: PoziomMatury.Rozszerzony));
        var inneUmiejetnosc = await ZapiszAsync(Nowe(glowna: UmiejetnoscB));
        var wstrzymane = await ZapiszAsync(Nowe());
        await _baza.WykonajAsync($"INSERT INTO wstrzymanie_w_doborze(zadanie_id, liczba_zgloszen) VALUES ('{Id(wstrzymane.Id)}', 3)");
        await _baza.WykonajAsync($"UPDATE kalibracja_trudnosci SET trudnosc_b = 0.5, liczba_wynikow_kalibracji = 45, poziom_trudnosci = 4 WHERE zadanie_id = '{Id(pasujace.Id)}'");

        await using var kontekst = _baza.Kontekst();
        var fasada = Fasada(kontekst);
        var wszystkie = await fasada.PobierzKandydatowDoboruAsync(
            new FiltrKandydatow(PoziomMatury.Podstawowy, UmiejetnoscA, new HashSet<RodzajZadania> { RodzajZadania.Zamkniete, RodzajZadania.OtwarteObliczeniowe }), Ct);
        var tylkoZamkniete = await fasada.PobierzKandydatowDoboruAsync(
            new FiltrKandydatow(PoziomMatury.Podstawowy, UmiejetnoscA, new HashSet<RodzajZadania> { RodzajZadania.Zamkniete }), Ct);
        var puste = await fasada.PobierzKandydatowDoboruAsync(
            new FiltrKandydatow(PoziomMatury.Podstawowy, UmiejetnoscA, new HashSet<RodzajZadania>()), Ct);

        wszystkie.Select(k => k.Id).ShouldBe([pasujace.Id, pasujaceZamkniete.Id], ignoreOrder: true);
        wszystkie.Select(k => k.Id).ShouldNotContain(prywatne.Id);
        wszystkie.Select(k => k.Id).ShouldNotContain(wycofane.Id);
        wszystkie.Select(k => k.Id).ShouldNotContain(innyPoziom.Id);
        wszystkie.Select(k => k.Id).ShouldNotContain(inneUmiejetnosc.Id);
        wszystkie.Select(k => k.Id).ShouldNotContain(wstrzymane.Id);
        var obliczeniowe = wszystkie.Single(k => k.Id == pasujace.Id);
        obliczeniowe.AktualnaWersjaId.ShouldBe(pasujace.AktualnaWersjaId);
        obliczeniowe.Dodatkowe.ShouldBe([UmiejetnoscB]);
        obliczeniowe.PoziomTrudnosci.Wartosc.ShouldBe(4);
        obliczeniowe.TrudnoscB.ShouldBe(0.5m);
        obliczeniowe.LiczbaWynikowKalibracji.ShouldBe(45);
        obliczeniowe.CzyBazowe.ShouldBeTrue();
        obliczeniowe.LiczbaStwierdzen.ShouldBe(0);
        var zamkniete = wszystkie.Single(k => k.Id == pasujaceZamkniete.Id);
        zamkniete.Forma.ShouldBe(FormaZamknieta.PrawdaFalsz);
        zamkniete.LiczbaStwierdzen.ShouldBe(2);
        zamkniete.PoziomTrudnosci.Wartosc.ShouldBe(4);
        zamkniete.TrudnoscB.ShouldBe(0.8m);
        zamkniete.LiczbaWynikowKalibracji.ShouldBe(0);
        tylkoZamkniete.Select(k => k.Id).ShouldBe([pasujaceZamkniete.Id]);
        puste.ShouldBeEmpty();
    }

    [Fact]
    public async Task DodanieZadaniaTworzyWierszPoczatkowyKalibracjiTrudnosci()
    {
        var zadanie = await ZapiszAsync(Nowe(poziom: 5));

        var id = Id(zadanie.Id);
        (await _baza.SkalarAsync<decimal>($"SELECT trudnosc_b FROM kalibracja_trudnosci WHERE zadanie_id = '{id}'")).ShouldBe(1.6m);
        (await _baza.SkalarAsync<short>($"SELECT poziom_trudnosci FROM kalibracja_trudnosci WHERE zadanie_id = '{id}'")).ShouldBe((short)5);
        (await _baza.SkalarAsync<int>($"SELECT liczba_wynikow_kalibracji FROM kalibracja_trudnosci WHERE zadanie_id = '{id}'")).ShouldBe(0);
    }

    // ---------- Wkładki teoretyczne ----------

    private async Task<WkladkaTeoretyczna> ZapiszWkladkeAsync(UmiejetnoscId umiejetnosc, Action<WkladkaTeoretyczna>? zmiana = null, bool wygenerowana = false)
    {
        var wkladka = WkladkaTeoretyczna.UtworzSzkic(
            new WkladkaTeoretycznaId(GeneratorUuid.Nowy(_baza.Zegar)), umiejetnosc, "Funkcja liniowa w pigułce", "Wzór: $y = ax + b$", 6, wygenerowana, wygenerowana, 0.0421m, Teraz);
        zmiana?.Invoke(wkladka);
        await using var kontekst = _baza.Kontekst();
        await new JednostkaPracyEf(kontekst).WykonajAsync(_ =>
        {
            new WkladkaTeoretycznaRepozytoriumEf(kontekst).Dodaj(wkladka);
            return Task.CompletedTask;
        }, Ct);
        return wkladka;
    }

    [Fact]
    public async Task WkladkaZapisujeSieZEnumemStatusuICzytaSieZPowrotem()
    {
        var wkladka = await ZapiszWkladkeAsync(UmiejetnoscA, wygenerowana: true);

        var id = wkladka.Id.Wartosc;
        (await _baza.SkalarAsync<string>($"SELECT status::text FROM wkladka_teoretyczna WHERE id = '{id}'")).ShouldBe("SZKIC");
        (await _baza.SkalarAsync<decimal>($"SELECT koszt_generowania_pln FROM wkladka_teoretyczna WHERE id = '{id}'")).ShouldBe(0.0421m);
        (await _baza.SkalarAsync<bool>($"SELECT wygenerowana_automatycznie FROM wkladka_teoretyczna WHERE id = '{id}'")).ShouldBeTrue();
        await using var kontekst = _baza.Kontekst();
        var wczytana = (await new WkladkaTeoretycznaRepozytoriumEf(kontekst).PobierzDoOdczytuAsync(wkladka.Id, Ct))!;
        wczytana.Tytul.ShouldBe("Funkcja liniowa w pigułce");
        wczytana.TrescMd.ShouldBe("Wzór: $y = ax + b$");
        wczytana.SzacowanyCzasMin.ShouldBe(6);
        wczytana.Status.ShouldBe(StatusWkladki.Szkic);
        wczytana.KosztGenerowaniaPln.ShouldBe(0.0421m);
        wczytana.Version.ShouldBe(1);
    }

    [Fact]
    public async Task PublikacjaWkladkiZapisujeZatwierdzajacegoIJestWidocznaWFasadzie()
    {
        var recenzent = await UtworzUzytkownikaAsync("r@example.com", "ADMINISTRATOR");
        var wkladka = await ZapiszWkladkeAsync(UmiejetnoscA);
        await using (var kontekst = _baza.Kontekst())
        {
            await new JednostkaPracyEf(kontekst).WykonajAsync(async ct =>
            {
                var wczytana = (await new WkladkaTeoretycznaRepozytoriumEf(kontekst).PobierzAsync(wkladka.Id, 1, ct))!;
                wczytana.PrzekazDoPrzegladu();
                wczytana.Opublikuj(recenzent);
            }, Ct);
        }

        await using var odczyt = _baza.Kontekst();
        var fasada = Fasada(odczyt);
        var opublikowana = await fasada.PobierzOpublikowanaWkladkeAsync(UmiejetnoscA, Ct);

        opublikowana.ShouldBe(new WkladkaDoDoboru(wkladka.Id, UmiejetnoscA, 6));
        (await fasada.PobierzOpublikowanaWkladkeAsync(UmiejetnoscB, Ct)).ShouldBeNull();
        (await _baza.SkalarAsync<string>($"SELECT zatwierdzona_przez::text FROM wkladka_teoretyczna WHERE id = '{wkladka.Id.Wartosc}'")).ShouldBe(recenzent.Wartosc.ToString());
        (await _baza.SkalarAsync<int>($"SELECT version FROM wkladka_teoretyczna WHERE id = '{wkladka.Id.Wartosc}'")).ShouldBe(2);
    }

    [Fact]
    public async Task DrugaOpublikowanaWkladkaTejSamejUmiejetnosciDajeKodZIndeksuUnikalnego()
    {
        var recenzent = await UtworzUzytkownikaAsync("r2@example.com", "ADMINISTRATOR");
        await ZapiszWkladkeAsync(UmiejetnoscA, w => w.Opublikuj(recenzent));

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZapiszWkladkeAsync(UmiejetnoscA, w => w.Opublikuj(recenzent)));

        blad.KodBledu.ShouldBe("WKLADKA_OPUBLIKOWANA_ISTNIEJE");
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM wkladka_teoretyczna")).ShouldBe(1);
    }

    [Fact]
    public async Task OpublikowanaWkladkaInnejUmiejetnosciIWycofanaNieKolidujaZIndeksem()
    {
        var recenzent = await UtworzUzytkownikaAsync("r3@example.com", "ADMINISTRATOR");
        await ZapiszWkladkeAsync(UmiejetnoscA, w => w.Opublikuj(recenzent));
        await ZapiszWkladkeAsync(UmiejetnoscB, w => w.Opublikuj(recenzent));
        var wycofana = await ZapiszWkladkeAsync(UmiejetnoscA, w => w.Wycofaj());

        wycofana.Status.ShouldBe(StatusWkladki.Wycofana);
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM wkladka_teoretyczna WHERE status = 'OPUBLIKOWANA'")).ShouldBe(2);
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM wkladka_teoretyczna")).ShouldBe(3);
    }

    // ---------- rejestracja enumów i DI ----------

    [Fact]
    public void EnumyTresciSaZarejestrowaneDlaTypowPostgres()
    {
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(PochodzenieZadania), "pochodzenie_zadania"));
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(ZasiegZadania), "zasieg_zadania"));
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(StatusZadania), "status_zadania"));
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(FormaZamknieta), "forma_zamknieta"));
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(StatusWkladki), "status_wkladki"));
    }

    [Fact]
    public async Task ModulTresciRejestrujeSieWDiIOdczytujeDzialy()
    {
        var opcje = new LematOpcje { Baza = new BazaOpcje { Polaczenie = _baza.Polaczenie } };
        var uslugi = new ServiceCollection().DodajPersystencje(opcje, RodzajProcesu.Api).DodajModulTresci();
        await using var dostawca = uslugi.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        await using var zakres = dostawca.CreateAsyncScope();
        var fasada = zakres.ServiceProvider.GetRequiredService<ITresciFasada>();

        (await fasada.ListaDzialowAsync(Ct)).Count.ShouldBe(3);
    }
}
