// @Role: Testy bazy modułów Rozwiązania i Ocenianie (PostgreSQL 16): zapis i odczyt agregatu ze Zdjęciami i Transkrypcją, wersjonowanie przejść, konflikt współbieżności, CHECK-i DDL, trg_ocena_niezmienna, UNIQUE (rozwiazanie_id, wersja), Ocena z Kryteriami i Wskazówką, Wynik zadania z Oceny, rejestracja modułów w DI | @DependsOn: BazaTestowa, SeedPocCommand, RozwiazanieRepozytoriumEf, TranskrypcjaRepozytoriumEf, OcenaRepozytoriumEf, JednostkaPracyEf | @UsedBy: dotnet test | @Task: TASK-126
using Lemat.Application.Common;
using Lemat.Application.Grading;
using Lemat.Application.Solutions;
using Lemat.Domain.Accounts;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Grading;
using Lemat.Domain.Models;
using Lemat.Domain.Profile;
using Lemat.Domain.Recommendations;
using Lemat.Domain.Solutions;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Repositories.Grading;
using Lemat.Infrastructure.Persistence.Repositories.Profile;
using Lemat.Infrastructure.Persistence.Repositories.Solutions;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Lemat.Tools.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class RozwiazaniaPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Zadanie1 = "a1000000-0000-4000-8000-000000000001";

    private const string Wersja1 = "b1000000-0000-4000-8000-000000000001";

    private static readonly ZadanieId IdZadania = new(Guid.Parse(Zadanie1));

    private static readonly ZadanieWersjaId IdWersji = new(Guid.Parse(Wersja1));

    private BazaTestowa _baza = null!;

    private UczenId _uczen;

    private UczenId _innyUczen;

    private int _kolejnoscElementu;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private DateTimeOffset Teraz => _baza.Zegar.GetUtcNow();

    public async ValueTask InitializeAsync()
    {
        _baza = await BazaTestowa.UtworzAsync(postgres);
        (await new SeedPocCommand(new System.Collections.Hashtable())
            .WykonajAsync([$"--polaczenie={_baza.Polaczenie}"], new StringWriter(), Ct)).ShouldBe(0);
        _uczen = new UczenId(await _baza.SkalarAsync<Guid>("SELECT uzytkownik_id FROM uczen ORDER BY uzytkownik_id LIMIT 1"));
        _innyUczen = new UczenId(await _baza.SkalarAsync<Guid>("SELECT uzytkownik_id FROM uczen ORDER BY uzytkownik_id DESC LIMIT 1"));
    }

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    // ---------- pomocnicy ----------

    private async Task<ElementSciezkiId> NowyElementAsync(UczenId uczen)
    {
        var kolejnosc = ++_kolejnoscElementu;
        var id = await _baza.SkalarAsync<Guid>(
            $"""
            INSERT INTO element_sciezki (sciezka_nauki_id, kolejnosc, typ, umiejetnosc_id, zadanie_id, zadanie_wersja_id, status, wygasa_at, rozpoczeto_at)
            SELECT s.id, {kolejnosc}, 'ZADANIE', 101, '{Zadanie1}', '{Wersja1}', 'ROZPOCZETY', now() + interval '7 days', now()
            FROM sciezka_nauki s WHERE s.uczen_id = '{uczen.Wartosc}'
            RETURNING id
            """);
        return new ElementSciezkiId(id);
    }

    private ZdjecieRozwiazania Zdjecie(RozwiazanieId rozwiazanie, int kolejnosc)
    {
        var id = Guid.NewGuid();
        return ZdjecieRozwiazania.Utworz(
            id,
            1,
            kolejnosc,
            $"zdjecia/{_uczen.Wartosc}/{rozwiazanie.Wartosc}/{id}.jpg",
            4096 * kolejnosc,
            System.Security.Cryptography.SHA256.HashData([(byte)kolejnosc]),
            1200,
            1600,
            Teraz.AddDays(90),
            Teraz);
    }

    private async Task<RozwiazanieId> NoweRozwiazanieAsync(UczenId? uczen = null, int zdjec = 2)
    {
        var wlasciciel = uczen ?? _uczen;
        var element = await NowyElementAsync(wlasciciel);
        var id = new RozwiazanieId(GeneratorUuid.Nowy(_baza.Zegar));
        var rozwiazanie = Rozwiazanie.Przeslij(
            id,
            wlasciciel,
            IdZadania,
            IdWersji,
            RodzajZadania.OtwarteObliczeniowe,
            element,
            null,
            [.. Enumerable.Range(1, zdjec).Select(k => Zdjecie(id, k))],
            Teraz);
        await using var kontekst = _baza.Kontekst();
        await new JednostkaPracyEf(kontekst).WykonajAsync(
            _ =>
            {
                new RozwiazanieRepozytoriumEf(kontekst).Dodaj(rozwiazanie);
                return Task.CompletedTask;
            },
            Ct);
        return id;
    }

    /// <summary>Wczytuje Rozwiązanie ze śledzeniem, wykonuje zmianę i zapisuje w jednej jednostce pracy.</summary>
    private async Task ZmienAsync(RozwiazanieId id, Action<Rozwiazanie, LematDbContext> zmiana)
    {
        await using var kontekst = _baza.Kontekst();
        await new JednostkaPracyEf(kontekst).WykonajAsync(
            async token =>
            {
                var rozwiazanie = await new RozwiazanieRepozytoriumEf(kontekst).PobierzAsync(id, token);
                zmiana(rozwiazanie!, kontekst);
            },
            Ct);
    }

    private async Task<Rozwiazanie> OdczytajAsync(RozwiazanieId id)
    {
        await using var kontekst = _baza.Kontekst();
        return (await new RozwiazanieRepozytoriumEf(kontekst).PobierzDoOdczytuAsync(id, Ct))!;
    }

    private async Task<Transkrypcja> DodajTranskrypcjeAsync(RozwiazanieId id, int kroki = 3)
    {
        var konfiguracja = new KonfiguracjaModeluId(await _baza.SkalarAsync<Guid>("SELECT id FROM konfiguracja_modelu WHERE rola = 'ROZPOZNAWANIE_PISMA'"));
        var transkrypcja = Transkrypcja.ZAgenta(
            new TranskrypcjaId(GeneratorUuid.Nowy(_baza.Zegar)),
            id,
            1,
            1,
            konfiguracja,
            0.95m,
            [.. Enumerable.Range(1, kroki).Select(n => KrokTranskrypcji.OdczytanyPrzezAgenta(n, $"Krok {n}", $@"x_{n}=\frac{{1}}{{2}}", 0.9m + (n / 100m)))],
            Teraz);
        await ZmienAsync(id, (r, kontekst) =>
        {
            r.RozpocznijRozpoznawanie();
            new TranskrypcjaRepozytoriumEf(kontekst).Dodaj(transkrypcja);
            r.ZapiszTranskrypcjeAgenta(transkrypcja.Id, transkrypcja.Wersja, Teraz);
        });
        return transkrypcja;
    }

    private async Task<DaneOcenyAutomatycznej> DaneOcenyAsync(RozwiazanieId id, TranskrypcjaId transkrypcja, int punkty = 1, Wskazowka? wskazowka = null)
    {
        await using var kontekst = _baza.Kontekst();
        var kryteria = await new OcenaRepozytoriumEf(kontekst).PobierzIdentyfikatoryKryteriowAsync(IdWersji, new WariantRozwiazania(1), Ct);
        var blad = punkty < 2;
        return new DaneOcenyAutomatycznej(
            id,
            1,
            transkrypcja,
            StatusOceny.Ostateczna,
            new WariantRozwiazania(1),
            punkty,
            punkty,
            2,
            blad ? 2 : null,
            false,
            blad ? TypBledu.Rachunkowy : null,
            false,
            null,
            new PewnoscOceny(0.995m),
            new PewnoscSurowa(0.995m),
            new ProgPewnosciId(await _baza.SkalarAsync<Guid>("SELECT id FROM prog_pewnosci WHERE aktywny")),
            new KalibratorPewnosciId(await _baza.SkalarAsync<Guid>("SELECT id FROM kalibrator_pewnosci LIMIT 1")),
            new KonfiguracjaModeluId(await _baza.SkalarAsync<Guid>("SELECT id FROM konfiguracja_modelu WHERE rola = 'DECYZYJNY_GLOWNY'")),
            $"Razem {punkty}/2 pkt.",
            wskazowka,
            [
                new OcenaKryterium(kryteria[1], 1, 0.995m),
                new OcenaKryterium(kryteria[2], punkty - 1, 0.995m),
            ]);
    }

    /// <summary>Doprowadza Rozwiązanie do OCENIONE z zapisaną Oceną; zwraca identyfikatory.</summary>
    private async Task<(RozwiazanieId Rozwiazanie, Transkrypcja Transkrypcja, OcenaId Ocena)> OcenioneAsync(int punkty = 1, Wskazowka? wskazowka = null)
    {
        var id = await NoweRozwiazanieAsync();
        var transkrypcja = await DodajTranskrypcjeAsync(id);
        await ZmienAsync(id, (r, _) => r.Zatwierdz(transkrypcja.Id, 1, Teraz));
        await ZmienAsync(id, (r, _) => r.RozpocznijOcenianie());
        var dane = await DaneOcenyAsync(id, transkrypcja.Id, punkty, wskazowka);
        var ocena = Ocena.Automatyczna(new OcenaId(GeneratorUuid.Nowy(_baza.Zegar)), dane, Teraz);
        await ZmienAsync(id, (r, kontekst) =>
        {
            new OcenaRepozytoriumEf(kontekst).Dodaj(ocena);
            r.ZarejestrujOcene(Teraz.AddSeconds(2));
        });
        return (id, transkrypcja, ocena.Id);
    }

    private static async Task<PostgresException> BladBazyAsync(Func<Task> operacja) =>
        (await Should.ThrowAsync<PostgresException>(operacja));

    // ---------- Rozwiązanie, Zdjęcia, Transkrypcja ----------

    [Fact]
    public async Task ZapisIOdczytAgregatuZeZdjeciamiITranskrypcja()
    {
        var id = await NoweRozwiazanieAsync(zdjec: 3);
        var transkrypcja = await DodajTranskrypcjeAsync(id, kroki: 3);

        var rozwiazanie = await OdczytajAsync(id);
        rozwiazanie.UczenId.ShouldBe(_uczen);
        rozwiazanie.ZadanieId.ShouldBe(IdZadania);
        rozwiazanie.ZadanieWersjaId.ShouldBe(IdWersji);
        rozwiazanie.ElementSciezkiId.ShouldNotBeNull();
        rozwiazanie.Status.ShouldBe(StatusRozwiazania.TranskrypcjaGotowa);
        rozwiazanie.NajnowszaTranskrypcjaId.ShouldBe(transkrypcja.Id);
        rozwiazanie.NajnowszaWersjaTranskrypcji.ShouldBe(1);
        rozwiazanie.TranskrypcjaGotowaAt.ShouldNotBeNull();
        rozwiazanie.LiczbaPrzeslan.ShouldBe(1);
        rozwiazanie.Zdjecia.Count.ShouldBe(3);
        rozwiazanie.Zdjecia.Select(z => z.Kolejnosc).Order().ShouldBe([1, 2, 3]);
        var zdjecie = rozwiazanie.Zdjecia.Single(z => z.Kolejnosc == 2);
        zdjecie.TypMime.ShouldBe("image/jpeg");
        zdjecie.RozmiarBajty.ShouldBe(8192);
        zdjecie.Szerokosc.ShouldBe(1200);
        zdjecie.Wysokosc.ShouldBe(1600);
        zdjecie.Sha256.Length.ShouldBe(32);
        zdjecie.KluczObiektu.ShouldStartWith($"zdjecia/{_uczen.Wartosc}/{id.Wartosc}/");
        zdjecie.UsunPo.ShouldBe(Teraz.AddDays(90));
        zdjecie.UsunietoAt.ShouldBeNull();

        await using var kontekst = _baza.Kontekst();
        var zapisana = await new TranskrypcjaRepozytoriumEf(kontekst).PobierzAsync(transkrypcja.Id, Ct);
        zapisana.ShouldNotBeNull();
        zapisana.Wersja.ShouldBe(1);
        zapisana.Zrodlo.ShouldBe(ZrodloTranskrypcji.AgentRozpoznajacy);
        zapisana.KonfiguracjaModeluId.ShouldBe(transkrypcja.KonfiguracjaModeluId);
        zapisana.ZgodnoscZZadaniem.ShouldBe(0.95m);
        zapisana.Kroki.Select(k => k.Nr).Order().ShouldBe([1, 2, 3]);
        var krok = zapisana.Kroki.Single(k => k.Nr == 2);
        krok.Tekst.ShouldBe("Krok 2");
        krok.Latex.ShouldBe(@"x_2=\frac{1}{2}");
        krok.PewnoscOdczytu.ShouldBe(0.92m);
        krok.NrKrokuAgenta.ShouldBe(2);
        krok.ZmienionyPrzezUcznia.ShouldBeFalse();

        var najnowsza = await new TranskrypcjaRepozytoriumEf(kontekst).PobierzNajnowszaAsync(id, Ct);
        najnowsza!.Id.ShouldBe(transkrypcja.Id);
    }

    [Fact]
    public async Task KazdePrzejscieZapisaneWBazieZwiekszaVersionORazIStemplujeCzas()
    {
        var id = await NoweRozwiazanieAsync();
        (await _baza.SkalarAsync<int>($"SELECT version FROM rozwiazanie WHERE id = '{id.Wartosc}'")).ShouldBe(1);
        var oczekiwana = 1;

        async Task SprawdzAsync()
        {
            oczekiwana++;
            (await _baza.SkalarAsync<int>($"SELECT version FROM rozwiazanie WHERE id = '{id.Wartosc}'")).ShouldBe(oczekiwana);
            (await OdczytajAsync(id)).Version.ShouldBe(oczekiwana);
        }

        await ZmienAsync(id, (r, _) => r.RozpocznijRozpoznawanie());
        await SprawdzAsync();

        var kroki = new[] { KrokTranskrypcji.OdczytanyPrzezAgenta(1, "t", "l", 0.9m) };
        var transkrypcja = Transkrypcja.ZAgenta(
            new TranskrypcjaId(GeneratorUuid.Nowy(_baza.Zegar)),
            id,
            1,
            1,
            new KonfiguracjaModeluId(await _baza.SkalarAsync<Guid>("SELECT id FROM konfiguracja_modelu WHERE rola = 'ROZPOZNAWANIE_PISMA'")),
            null,
            kroki,
            Teraz);
        _baza.Zegar.Teraz = Teraz.AddSeconds(3);
        await ZmienAsync(id, (r, kontekst) =>
        {
            new TranskrypcjaRepozytoriumEf(kontekst).Dodaj(transkrypcja);
            r.ZapiszTranskrypcjeAgenta(transkrypcja.Id, 1, Teraz);
        });
        await SprawdzAsync();

        _baza.Zegar.Teraz = Teraz.AddSeconds(7);
        await ZmienAsync(id, (r, _) => r.Zatwierdz(transkrypcja.Id, 1, Teraz));
        await SprawdzAsync();

        await ZmienAsync(id, (r, _) => r.RozpocznijOcenianie());
        await SprawdzAsync();

        _baza.Zegar.Teraz = Teraz.AddSeconds(2);
        await ZmienAsync(id, (r, _) => r.ZarejestrujOcene(Teraz));
        await SprawdzAsync();

        var koniec = await OdczytajAsync(id);
        koniec.Status.ShouldBe(StatusRozwiazania.Ocenione);
        koniec.CzasSystemowy.ShouldBe(TimeSpan.FromSeconds(5));
        (await _baza.SkalarAsync<int>($"SELECT czas_systemowy_ms FROM rozwiazanie WHERE id = '{id.Wartosc}'")).ShouldBe(5000);
        koniec.OcenaWystawionaAt.ShouldNotBeNull();
        koniec.ZatwierdzonoAt.ShouldNotBeNull();
        koniec.UpdatedAt.ShouldBe(_baza.Zegar.Teraz);
    }

    [Fact]
    public async Task BladRozpoznawaniaZapisujeKodJakoTextUpperSnake()
    {
        var id = await NoweRozwiazanieAsync();
        await ZmienAsync(id, (r, _) =>
        {
            r.RozpocznijRozpoznawanie();
            r.ZapiszBladRozpoznawania(KodBleduRozpoznawania.Nieczytelne);
        });

        (await _baza.SkalarAsync<string>($"SELECT kod_bledu_rozpoznawania FROM rozwiazanie WHERE id = '{id.Wartosc}'")).ShouldBe("NIECZYTELNE");
        (await _baza.SkalarAsync<string>($"SELECT status::text FROM rozwiazanie WHERE id = '{id.Wartosc}'")).ShouldBe("BLAD_ROZPOZNAWANIA");
        var rozwiazanie = await OdczytajAsync(id);
        rozwiazanie.KodBleduRozpoznawania.ShouldBe(KodBleduRozpoznawania.Nieczytelne);
    }

    [Fact]
    public async Task RownoleglaZmianaTegoSamegoRozwiazaniaKonczySieKonfliktemWspolbieznosci()
    {
        var id = await NoweRozwiazanieAsync();
        await using var pierwszy = _baza.Kontekst();
        await using var drugi = _baza.Kontekst();
        var rozwiazaniePierwsze = await new RozwiazanieRepozytoriumEf(pierwszy).PobierzAsync(id, Ct);
        var rozwiazanieDrugie = await new RozwiazanieRepozytoriumEf(drugi).PobierzAsync(id, Ct);

        await new JednostkaPracyEf(pierwszy).WykonajAsync(_ =>
        {
            rozwiazaniePierwsze!.RozpocznijRozpoznawanie();
            return Task.CompletedTask;
        }, Ct);

        await Should.ThrowAsync<KonfliktWspolbieznosciException>(() => new JednostkaPracyEf(drugi).WykonajAsync(_ =>
        {
            rozwiazanieDrugie!.ZapiszBladRozpoznawania(KodBleduRozpoznawania.BladDostawcy);
            return Task.CompletedTask;
        }, Ct));

        (await OdczytajAsync(id)).Status.ShouldBe(StatusRozwiazania.Rozpoznawanie);
    }

    [Fact]
    public async Task NieukonczoneRozwiazanieUczniaPrzyZadaniuBlokujeTylkoStanyWTrakcie()
    {
        await using (var kontekst = _baza.Kontekst())
        {
            (await new RozwiazanieRepozytoriumEf(kontekst).ZnajdzNieukonczoneAsync(_uczen, IdZadania, Ct)).ShouldBeNull();
        }

        var id = await NoweRozwiazanieAsync();
        await using (var kontekst = _baza.Kontekst())
        {
            (await new RozwiazanieRepozytoriumEf(kontekst).ZnajdzNieukonczoneAsync(_uczen, IdZadania, Ct)).ShouldBe(id);
            (await new RozwiazanieRepozytoriumEf(kontekst).ZnajdzNieukonczoneAsync(_innyUczen, IdZadania, Ct)).ShouldBeNull();
        }

        await ZmienAsync(id, (r, _) =>
        {
            r.RozpocznijRozpoznawanie();
            r.ZapiszBladRozpoznawania(KodBleduRozpoznawania.Nieczytelne);
        });
        await using (var kontekst = _baza.Kontekst())
        {
            (await new RozwiazanieRepozytoriumEf(kontekst).ZnajdzNieukonczoneAsync(_uczen, IdZadania, Ct)).ShouldBeNull();
        }

        var ocenione = await OcenioneAsync();
        await using (var kontekst = _baza.Kontekst())
        {
            (await new RozwiazanieRepozytoriumEf(kontekst).ZnajdzNieukonczoneAsync(_uczen, IdZadania, Ct)).ShouldBeNull();
            (await new RozwiazanieRepozytoriumEf(kontekst).PobierzDoOdczytuAsync(ocenione.Rozwiazanie, Ct))!.Status.ShouldBe(StatusRozwiazania.Ocenione);
        }
    }

    [Fact]
    public async Task FasadaRozwiazanZwracaMigaweTylkoWlascicielowi()
    {
        var id = await NoweRozwiazanieAsync();
        var transkrypcja = await DodajTranskrypcjeAsync(id);

        await using var kontekst = _baza.Kontekst();
        var fasada = new RozwiazaniaFasada(new RozwiazanieRepozytoriumEf(kontekst), new TranskrypcjaRepozytoriumEf(kontekst));

        var wlasne = await fasada.PobierzDlaUczniaAsync(id, _uczen, Ct);
        wlasne.ShouldNotBeNull();
        wlasne.Status.ShouldBe(StatusRozwiazania.TranskrypcjaGotowa);
        wlasne.Zdjecia.Count.ShouldBe(2);
        wlasne.NajnowszaTranskrypcjaId.ShouldBe(transkrypcja.Id);
        (await fasada.PobierzDlaUczniaAsync(id, _innyUczen, Ct)).ShouldBeNull();
        (await fasada.PobierzDlaUczniaAsync(new RozwiazanieId(Guid.NewGuid()), _uczen, Ct)).ShouldBeNull();

        var skrot = await fasada.PobierzNajnowszaTranskrypcjeAsync(id, Ct);
        skrot!.Kroki.Count.ShouldBe(3);
        skrot.Kroki[0].Nr.ShouldBe(1);
        (await fasada.PobierzNajnowszaTranskrypcjeAsync(new RozwiazanieId(Guid.NewGuid()), Ct)).ShouldBeNull();
    }

    // ---------- CHECK-i i UNIQUE DDL ----------

    [Fact]
    public async Task RozwiazanieBezKontekstuProbyNaruszaCheckDdl()
    {
        var blad = await BladBazyAsync(() => _baza.WykonajAsync(
            $"INSERT INTO rozwiazanie (uczen_id, zadanie_id, zadanie_wersja_id) VALUES ('{_uczen.Wartosc}', '{Zadanie1}', '{Wersja1}')"));

        blad.SqlState.ShouldBe("23514");
    }

    [Fact]
    public async Task OcenaAutomatycznaBezProguIKalibratoraNaruszaCheckDdl()
    {
        var id = await NoweRozwiazanieAsync();
        var transkrypcja = await DodajTranskrypcjeAsync(id);

        var blad = await BladBazyAsync(() => _baza.WykonajAsync(
            $"""
            INSERT INTO ocena (rozwiazanie_id, wersja, status, zrodlo, transkrypcja_id, wariant, punkty, punkty_max, uzasadnienie, pewnosc_oceny)
            VALUES ('{id.Wartosc}', 1, 'OSTATECZNA', 'AUTOMATYCZNA', '{transkrypcja.Id.Wartosc}', 1, 1, 2, 'x', 0.99)
            """));

        blad.SqlState.ShouldBe("23514");
        blad.ConstraintName.ShouldNotBeNull().ShouldStartWith("ocena_check");
    }

    [Fact]
    public async Task DrugaTranskrypcjaTejSamejWersjiNaruszaUnique()
    {
        var id = await NoweRozwiazanieAsync();
        var transkrypcja = await DodajTranskrypcjeAsync(id);

        var blad = await BladBazyAsync(() => _baza.WykonajAsync(
            $"""
            INSERT INTO transkrypcja (rozwiazanie_id, wersja, zrodlo, przeslanie_nr, konfiguracja_modelu_id)
            SELECT rozwiazanie_id, wersja, zrodlo, przeslanie_nr, konfiguracja_modelu_id FROM transkrypcja WHERE id = '{transkrypcja.Id.Wartosc}'
            """));

        blad.SqlState.ShouldBe("23505");
        blad.ConstraintName.ShouldBe("transkrypcja_rozwiazanie_id_wersja_key");
    }

    [Fact]
    public async Task KrokiTranskrypcjiMajaUniqueNumeruWTranskrypcji()
    {
        var id = await NoweRozwiazanieAsync();
        var transkrypcja = await DodajTranskrypcjeAsync(id, kroki: 2);

        var blad = await BladBazyAsync(() => _baza.WykonajAsync(
            $"INSERT INTO krok_transkrypcji (transkrypcja_id, nr, tekst, latex, pewnosc_odczytu, nr_kroku_agenta) VALUES ('{transkrypcja.Id.Wartosc}', 2, 't', 'l', 0.5, 2)"));

        blad.SqlState.ShouldBe("23505");
    }

    [Fact]
    public async Task DrugaOcenaTejSamejWersjiNaruszaUniqueRozwiazanieWersja()
    {
        var (id, transkrypcja, _) = await OcenioneAsync();
        var dane = await DaneOcenyAsync(id, transkrypcja.Id);
        var duplikat = Ocena.Automatyczna(new OcenaId(GeneratorUuid.Nowy(_baza.Zegar)), dane with { Status = StatusOceny.Wstepna }, Teraz);

        await using var kontekst = _baza.Kontekst();
        await Should.ThrowAsync<Exception>(() => new JednostkaPracyEf(kontekst).WykonajAsync(
            _ =>
            {
                new OcenaRepozytoriumEf(kontekst).Dodaj(duplikat);
                return Task.CompletedTask;
            },
            Ct));
        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM ocena WHERE rozwiazanie_id = '{id.Wartosc}'")).ShouldBe(1);
    }

    // ---------- Ocena ----------

    [Fact]
    public async Task OcenaZapisujeSieZKryteriamiIWskazowkaIOdczytujeSieWPelni()
    {
        var wskazowka = new Wskazowka("Sprawdź jeszcze raz krok 2.", ZrodloWskazowki.Szablon);
        var (id, transkrypcja, ocenaId) = await OcenioneAsync(punkty: 1, wskazowka: wskazowka);

        await using var kontekst = _baza.Kontekst();
        var ocena = await new OcenaRepozytoriumEf(kontekst).PobierzAktualnaAsync(id, Ct);

        ocena.ShouldNotBeNull();
        ocena.Id.ShouldBe(ocenaId);
        ocena.Status.ShouldBe(StatusOceny.Ostateczna);
        ocena.Zrodlo.ShouldBe(ZrodloOceny.Automatyczna);
        ocena.TranskrypcjaId.ShouldBe(transkrypcja.Id);
        ocena.Wariant.Wartosc.ShouldBe(1);
        ocena.Punkty.ShouldBe(1);
        ocena.PunktyMax.ShouldBe(2);
        ocena.PunktySurowe.ShouldBe(1);
        ocena.PierwszyBlednyKrok.ShouldBe(2);
        ocena.TypBledu.ShouldBe(TypBledu.Rachunkowy);
        ocena.PewnoscOceny!.Value.Wartosc.ShouldBe(0.995m);
        ocena.PewnoscSurowa!.Value.Wartosc.ShouldBe(0.995m);
        ocena.ProgPewnosciId.ShouldNotBeNull();
        ocena.KalibratorPewnosciId.ShouldNotBeNull();
        ocena.KonfiguracjaModeluId.ShouldNotBeNull();
        ocena.Uzasadnienie.ShouldBe("Razem 1/2 pkt.");
        ocena.Wskazowka.ShouldNotBeNull();
        ocena.Wskazowka.Tresc.ShouldBe("Sprawdź jeszcze raz krok 2.");
        ocena.Wskazowka.Zrodlo.ShouldBe(ZrodloWskazowki.Szablon);
        ocena.Kryteria.Count.ShouldBe(2);
        ocena.Kryteria.Sum(k => k.Punkty).ShouldBe(1);
        ocena.Kryteria.ShouldAllBe(k => k.Prawdopodobienstwo == 0.995m);
        ocena.Widocznosc.ShouldBe(WidocznoscOcenyDlaUcznia.Pelna);
        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM decyzja_modelu WHERE rozwiazanie_id = '{id.Wartosc}'")).ShouldBe(0);
    }

    [Fact]
    public async Task OcenaBezBleduNieMaWskazowki()
    {
        var (id, _, _) = await OcenioneAsync(punkty: 2);

        await using var kontekst = _baza.Kontekst();
        var ocena = await new OcenaRepozytoriumEf(kontekst).PobierzAktualnaAsync(id, Ct);

        ocena!.Wskazowka.ShouldBeNull();
        ocena.TypBledu.ShouldBeNull();
        ocena.PierwszyBlednyKrok.ShouldBeNull();
        (await _baza.SkalarAsync<bool>($"SELECT wskazowka IS NULL AND zrodlo_wskazowki IS NULL FROM ocena WHERE id = '{ocena.Id.Wartosc}'")).ShouldBeTrue();
    }

    [Fact]
    public async Task TriggerNiezmiennosciOcenyBlokujeUpdatePunktow()
    {
        var (_, _, ocenaId) = await OcenioneAsync();

        var blad = await BladBazyAsync(() => _baza.WykonajAsync($"UPDATE ocena SET punkty = 0 WHERE id = '{ocenaId.Wartosc}'"));

        blad.MessageText.ShouldContain("OCENA_NIEZMIENNA");
        (await _baza.SkalarAsync<int>($"SELECT punkty::int FROM ocena WHERE id = '{ocenaId.Wartosc}'")).ShouldBe(1);
    }

    [Fact]
    public async Task TriggerNiezmiennosciOcenyBlokujeUpdateWskazowkiPoZapisie()
    {
        var wskazowka = new Wskazowka("Stara.", ZrodloWskazowki.Szablon);
        var (_, _, ocenaId) = await OcenioneAsync(wskazowka: wskazowka);

        var blad = await BladBazyAsync(() => _baza.WykonajAsync($"UPDATE ocena SET wskazowka = 'Nowa.' WHERE id = '{ocenaId.Wartosc}'"));

        blad.MessageText.ShouldContain("OCENA_NIEZMIENNA");
    }

    [Fact]
    public async Task WskazowkeMoznaUzupelnicJednokrotnieDomenaIBaza()
    {
        var (id, _, _) = await OcenioneAsync();

        await using (var kontekst = _baza.Kontekst())
        {
            await new JednostkaPracyEf(kontekst).WykonajAsync(
                async token =>
                {
                    var ocena = await new OcenaRepozytoriumEf(kontekst).PobierzAktualnaDoZmianyAsync(id, token);
                    ocena!.UzupelnijWskazowke(new Wskazowka("Sprawdź krok 2.", ZrodloWskazowki.AgentWyjasniajacy));
                },
                Ct);
        }

        await using (var kontekst = _baza.Kontekst())
        {
            var ocena = await new OcenaRepozytoriumEf(kontekst).PobierzAktualnaAsync(id, Ct);
            ocena!.Wskazowka!.Tresc.ShouldBe("Sprawdź krok 2.");
            ocena.Wskazowka.Zrodlo.ShouldBe(ZrodloWskazowki.AgentWyjasniajacy);
        }
    }

    [Fact]
    public async Task ZastapienieOcenyIDodanieNowejWersjiWSeriiZapisow()
    {
        var (id, transkrypcja, stara) = await OcenioneAsync();

        await using (var kontekst = _baza.Kontekst())
        {
            await new JednostkaPracyEf(kontekst).WykonajAsync(
                async token =>
                {
                    var ocena = await new OcenaRepozytoriumEf(kontekst).PobierzAktualnaDoZmianyAsync(id, token);
                    ocena!.Zastap();
                },
                Ct);
        }

        var dane = await DaneOcenyAsync(id, transkrypcja.Id, punkty: 2);
        var nowa = Ocena.Automatyczna(new OcenaId(GeneratorUuid.Nowy(_baza.Zegar)), dane with { Wersja = 2 }, Teraz);
        await using (var kontekst = _baza.Kontekst())
        {
            await new JednostkaPracyEf(kontekst).WykonajAsync(
                _ =>
                {
                    new OcenaRepozytoriumEf(kontekst).Dodaj(nowa);
                    return Task.CompletedTask;
                },
                Ct);
        }

        await using var odczyt = _baza.Kontekst();
        var aktualna = await new OcenaRepozytoriumEf(odczyt).PobierzAktualnaAsync(id, Ct);
        aktualna!.Id.ShouldBe(nowa.Id);
        aktualna.Wersja.ShouldBe(2);
        (await _baza.SkalarAsync<string>($"SELECT status::text FROM ocena WHERE id = '{stara.Wartosc}'")).ShouldBe("ZASTAPIONA");
    }

    [Fact]
    public async Task DwieAktualneOcenyTegoSamegoRozwiazaniaNaruszajaUniqueAktualnej()
    {
        var (id, transkrypcja, _) = await OcenioneAsync();
        var dane = await DaneOcenyAsync(id, transkrypcja.Id);
        var druga = Ocena.Automatyczna(new OcenaId(GeneratorUuid.Nowy(_baza.Zegar)), dane with { Wersja = 2 }, Teraz);

        await using var kontekst = _baza.Kontekst();
        var wyjatek = await Should.ThrowAsync<Exception>(() => new JednostkaPracyEf(kontekst).WykonajAsync(
            _ =>
            {
                new OcenaRepozytoriumEf(kontekst).Dodaj(druga);
                return Task.CompletedTask;
            },
            Ct));
        wyjatek.ToString().ShouldContain("ux_ocena_aktualna");
    }

    [Fact]
    public async Task IdentyfikatoryKryteriowSaMapowaneNaNumeryKryteriow()
    {
        await using var kontekst = _baza.Kontekst();

        var wariant1 = await new OcenaRepozytoriumEf(kontekst).PobierzIdentyfikatoryKryteriowAsync(IdWersji, new WariantRozwiazania(1), Ct);
        var wariant2 = await new OcenaRepozytoriumEf(kontekst).PobierzIdentyfikatoryKryteriowAsync(IdWersji, new WariantRozwiazania(2), Ct);

        wariant1.Keys.Order().ShouldBe([1, 2]);
        wariant1.Values.Distinct().Count().ShouldBe(2);
        wariant2.ShouldBeEmpty();
        (await _baza.SkalarAsync<Guid>($"SELECT id FROM kryterium_oceniania WHERE zadanie_wersja_id = '{Wersja1}' AND nr_kryterium = 2")).ShouldBe(wariant1[2]);
    }

    [Fact]
    public async Task FasadaOcenianiaZwracaMigaweOcenyAktualnej()
    {
        var (id, _, ocenaId) = await OcenioneAsync(punkty: 1, wskazowka: new Wskazowka("Wskazówka.", ZrodloWskazowki.Szablon));

        await using var kontekst = _baza.Kontekst();
        var fasada = new OcenianieFasada(new OcenaRepozytoriumEf(kontekst));

        var skrot = await fasada.PobierzAktualnaAsync(id, Ct);
        skrot.ShouldNotBeNull();
        skrot.Id.ShouldBe(ocenaId);
        skrot.Punkty.ShouldBe(1);
        skrot.PunktyMax.ShouldBe(2);
        skrot.PewnoscOceny.ShouldBe(0.995m);
        skrot.Wskazowka.ShouldBe("Wskazówka.");
        skrot.ZrodloWskazowki.ShouldBe(ZrodloWskazowki.Szablon);
        skrot.Kryteria.Count.ShouldBe(2);
        (await fasada.PobierzAktualnaAsync(new RozwiazanieId(Guid.NewGuid()), Ct)).ShouldBeNull();
    }

    // ---------- Wynik zadania z Oceny, Decyzja modelu ----------

    [Fact]
    public async Task WynikZadaniaZeZrodlemOcenaZapisujeSieRazemZOcenaWJednejTransakcji()
    {
        var id = await NoweRozwiazanieAsync();
        var transkrypcja = await DodajTranskrypcjeAsync(id);
        await ZmienAsync(id, (r, _) => r.Zatwierdz(transkrypcja.Id, 1, Teraz));
        await ZmienAsync(id, (r, _) => r.RozpocznijOcenianie());
        var ocena = Ocena.Automatyczna(new OcenaId(GeneratorUuid.Nowy(_baza.Zegar)), await DaneOcenyAsync(id, transkrypcja.Id), Teraz);
        var wynik = WynikZadania.ZOceny(
            new WynikZadaniaId(GeneratorUuid.Nowy(_baza.Zegar)),
            _uczen,
            IdZadania,
            IdWersji,
            ocena.Id.Wartosc,
            0.5m,
            ocenaWstepna: false,
            mozliwyBladOdczytu: false,
            pierwszaProba: true,
            Teraz)!;

        await ZmienAsync(id, (r, kontekst) =>
        {
            new WynikZadaniaRepozytoriumEf(kontekst).Dodaj(wynik);
            new OcenaRepozytoriumEf(kontekst).Dodaj(ocena);
            r.ZarejestrujOcene(Teraz);
        });

        (await _baza.SkalarAsync<string>($"SELECT zrodlo::text FROM wynik_zadania WHERE ocena_id = '{ocena.Id.Wartosc}'")).ShouldBe("OCENA");
        (await _baza.SkalarAsync<decimal>($"SELECT ulamek FROM wynik_zadania WHERE ocena_id = '{ocena.Id.Wartosc}'")).ShouldBe(0.5m);
        (await _baza.SkalarAsync<decimal>($"SELECT waga FROM wynik_zadania WHERE ocena_id = '{ocena.Id.Wartosc}'")).ShouldBe(1.00m);
        (await _baza.SkalarAsync<bool>($"SELECT liczy_do_trudnosci FROM wynik_zadania WHERE ocena_id = '{ocena.Id.Wartosc}'")).ShouldBeTrue();
    }

    [Fact]
    public async Task DecyzjaModeluDlaOcenyZapisujeSiePrzezIstniejacyPort()
    {
        var (id, _, ocenaId) = await OcenioneAsync();
        var konfiguracja = new KonfiguracjaModeluId(await _baza.SkalarAsync<Guid>("SELECT id FROM konfiguracja_modelu WHERE rola = 'DECYZYJNY_GLOWNY'"));
        var pytanie = new PytanieNoul("Q-OC-02.1.K1", "Czy Kryterium 1 jest spełnione?");
        var decyzja = DecyzjaModelu.Zapisz(
            new DecyzjaModeluId(GeneratorUuid.Nowy(_baza.Zegar)),
            Teraz,
            KontekstDecyzji.Ocena,
            ocenaId.Wartosc,
            _uczen,
            id.Wartosc,
            konfiguracja,
            pytanie,
            DecyzjaSkladowa.ZRozkladu(pytanie.Kod, pytanie.Typ, new Dictionary<string, decimal> { ["tak"] = 0.995m, ["nie"] = 0.005m }),
            12,
            System.Security.Cryptography.SHA256.HashData([1]),
            null);

        await using var kontekst = _baza.Kontekst();
        await new JednostkaPracyEf(kontekst).WykonajAsync(
            _ =>
            {
                new Lemat.Infrastructure.Persistence.Repositories.Models.DecyzjaModeluRepozytoriumEf(kontekst).DodajRange([decyzja]);
                return Task.CompletedTask;
            },
            Ct);

        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM decyzja_modelu WHERE kontekst = 'OCENA' AND rozwiazanie_id = '{id.Wartosc}' AND kontekst_id = '{ocenaId.Wartosc}'")).ShouldBe(1);
    }

    // ---------- DI ----------

    [Fact]
    public void ModulyRozwiazanIOcenianiaRejestrujaFasadyIRepozytoria()
    {
        var opcje = new LematOpcje { Baza = new BazaOpcje { Polaczenie = _baza.Polaczenie } };
        var uslugi = new ServiceCollection()
            .AddSingleton(Options.Create(opcje))
            .DodajPersystencje(opcje, RodzajProcesu.Api)
            .DodajModulRozwiazan()
            .DodajModulOceniania();

        using var dostawca = uslugi.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var zakres = dostawca.CreateScope();

        zakres.ServiceProvider.GetRequiredService<IRozwiazaniaFasada>().ShouldBeOfType<RozwiazaniaFasada>();
        zakres.ServiceProvider.GetRequiredService<IOcenianieFasada>().ShouldBeOfType<OcenianieFasada>();
        zakres.ServiceProvider.GetRequiredService<IRozwiazanieRepozytorium>().ShouldBeOfType<RozwiazanieRepozytoriumEf>();
        zakres.ServiceProvider.GetRequiredService<ITranskrypcjaRepozytorium>().ShouldBeOfType<TranskrypcjaRepozytoriumEf>();
        zakres.ServiceProvider.GetRequiredService<IOcenaRepozytorium>().ShouldBeOfType<OcenaRepozytoriumEf>();
    }
}
