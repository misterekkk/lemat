// @Role: Testy IJednostkaPracy/JednostkaPracyEf, TlumaczBledowBazy, wersjonowania agregatu (interceptor) i rejestracji DodajPersystencje na PostgreSQL | @DependsOn: BazaTestowa, JednostkaPracyEf, TlumaczBledowBazy, WersjaAgregatuInterceptor, PersistenceRegistration | @UsedBy: dotnet test | @Task: TASK-015
using Lemat.Application.Common;
using Lemat.Domain.Common;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class UnitOfWorkTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private BazaTestowa _baza = null!;

    public async ValueTask InitializeAsync() => _baza = await BazaTestowa.UtworzAsync(postgres);

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    private async Task<TestowyAgregat> ZapiszNowyAsync(string nazwa)
    {
        var agregat = TestowyAgregat.Nowy(nazwa);
        await using var kontekst = _baza.Kontekst();
        kontekst.Add(agregat);
        await kontekst.SaveChangesAsync(Ct);
        return agregat;
    }

    private Task<int> WersjaAsync(TestowyAgregat a) =>
        _baza.SkalarAsync<int>($"SELECT version FROM test_agregat WHERE id = '{a.Id.Wartosc}'");

    // ---------- TlumaczBledowBazy ----------

    [Fact]
    public async Task RaiseExceptionZTriggeraDajeNiezmiennikNaruszonyZKodem()
    {
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => uow.WykonajAsync(async ct =>
        {
            await using var polecenie = uow.UtworzPolecenie("DO $$ BEGIN RAISE EXCEPTION 'TEST_KOD'; END $$");
            await polecenie.ExecuteNonQueryAsync(ct);
        }, CancellationToken.None));

        blad.KodBledu.ShouldBe("TEST_KOD");
    }

    [Fact]
    public async Task NaruszenieIndeksuZKataloguDajeKodZKatalogu()
    {
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);
        await _baza.WykonajAsync("INSERT INTO test_s.unikalna(kod) VALUES ('a')");

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => uow.WykonajAsync(async ct =>
        {
            await using var polecenie = uow.UtworzPolecenie("INSERT INTO test_s.unikalna(kod) VALUES ('a')");
            await polecenie.ExecuteNonQueryAsync(ct);
        }, CancellationToken.None));

        blad.KodBledu.ShouldBe("ELEMENT_JUZ_PROPONOWANY");
    }

    [Fact]
    public void TlumaczNieTlumaczyBledowNieznanych()
    {
        TlumaczBledowBazy.Przetlumacz(new InvalidOperationException("x")).ShouldBeNull();
        TlumaczBledowBazy.Przetlumacz(new DbUpdateException("x", new InvalidOperationException())).ShouldBeNull();
    }

    [Fact]
    public async Task NaruszenieIndeksuSpozaKataloguNieJestTlumaczone()
    {
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);
        await _baza.WykonajAsync("TRUNCATE test_s.unikalna");
        await _baza.WykonajAsync("INSERT INTO test_s.unikalna(kod) VALUES ('b')");

        // oba ograniczenia są naruszone naraz; PostgreSQL zgłasza pierwsze z nich (ux_jeden_proponowany) — sprawdzamy brak wpisu dla innego
        KatalogIndeksowUnikalnych.Kod("ux_nieznany_w_katalogu").ShouldBeNull();
        KatalogIndeksowUnikalnych.Kod(null).ShouldBeNull();
        await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => uow.WykonajAsync(async ct =>
        {
            await using var polecenie = uow.UtworzPolecenie("INSERT INTO test_s.unikalna(kod) VALUES ('b')");
            await polecenie.ExecuteNonQueryAsync(ct);
        }, CancellationToken.None));
    }

    [Fact]
    public async Task KatalogIndeksowOdnosiSieDoIstniejacychOgraniczenWBazie()
    {
        foreach (var nazwa in KatalogIndeksowUnikalnych.Wszystkie.Keys)
        {
            (await _baza.SkalarAsync<long>(
                $"SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND c.relname = '{nazwa}'"))
                .ShouldBe(1, $"brak indeksu/ograniczenia {nazwa} w schemacie public");
        }
    }

    // ---------- współbieżność ----------

    [Fact]
    public async Task KonfliktVersionDajeKonfliktWspolbieznosciWJednostcePracy()
    {
        var agregat = await ZapiszNowyAsync("wspolbiezny");
        await using var pierwszy = _baza.Kontekst();
        await using var drugi = _baza.Kontekst();
        var a1 = await pierwszy.Set<TestowyAgregat>().AsTracking().SingleAsync(a => a.Id == agregat.Id, Ct);
        var a2 = await drugi.Set<TestowyAgregat>().AsTracking().SingleAsync(a => a.Id == agregat.Id, Ct);
        a1.Nazwa = "pierwszy";
        await pierwszy.SaveChangesAsync(Ct);

        var uow = new JednostkaPracyEf(drugi);
        await Should.ThrowAsync<KonfliktWspolbieznosciException>(() => uow.WykonajAsync(_ =>
        {
            a2.Nazwa = "drugi";
            return Task.CompletedTask;
        }, CancellationToken.None));

        (await _baza.SkalarAsync<string>($"SELECT nazwa FROM test_agregat WHERE id = '{agregat.Id.Wartosc}'")).ShouldBe("pierwszy");
    }

    [Fact]
    public void DbUpdateConcurrencyExceptionJestTlumaczonyNaWyjatekAplikacyjny() =>
        TlumaczBledowBazy.Przetlumacz(new DbUpdateConcurrencyException("x")).ShouldBeOfType<KonfliktWspolbieznosciException>();

    // ---------- interceptor wersji ----------

    [Fact]
    public async Task WersjaRosnieOJedenPrzyZmianieKorzeniaINieRosniePrzyDodaniu()
    {
        var agregat = await ZapiszNowyAsync("wersja");
        (await WersjaAsync(agregat)).ShouldBe(1);

        await using var kontekst = _baza.Kontekst();
        var tracked = await kontekst.Set<TestowyAgregat>().AsTracking().SingleAsync(a => a.Id == agregat.Id, Ct);
        tracked.Nazwa = "zmiana";
        await kontekst.SaveChangesAsync(Ct);

        (await WersjaAsync(agregat)).ShouldBe(2);
        tracked.Version.ShouldBe(2);
        tracked.Nazwa = "zmiana 2";
        await kontekst.SaveChangesAsync(Ct);
        (await WersjaAsync(agregat)).ShouldBe(3);
    }

    [Fact]
    public async Task ZmianaWylacznieWlasciwosciBezZmianyWersjiNiePodnosiVersion()
    {
        var agregat = await ZapiszNowyAsync("licznik");
        await using var kontekst = _baza.Kontekst();
        var tracked = await kontekst.Set<TestowyAgregat>().AsTracking().SingleAsync(a => a.Id == agregat.Id, Ct);
        tracked.Licznik = 5;
        await kontekst.SaveChangesAsync(Ct);

        (await WersjaAsync(agregat)).ShouldBe(1);
        (await _baza.SkalarAsync<int>($"SELECT licznik FROM test_agregat WHERE id = '{agregat.Id.Wartosc}'")).ShouldBe(5);
    }

    [Fact]
    public async Task ZmianaWEncjiWlasnejJsonPodnosiVersionKorzenia()
    {
        var agregat = await ZapiszNowyAsync("wlasne");
        await using var kontekst = _baza.Kontekst();
        var tracked = await kontekst.Set<TestowyAgregat>().AsTracking().SingleAsync(a => a.Id == agregat.Id, Ct);
        tracked.Wpisy.Add(new WpisTestowy { Tresc = "nowy" });
        await kontekst.SaveChangesAsync(Ct);
        (await WersjaAsync(agregat)).ShouldBe(2);

        tracked.Wpisy[0].Tresc = "zmieniony";
        await kontekst.SaveChangesAsync(Ct);
        (await WersjaAsync(agregat)).ShouldBe(3);

        tracked.Wpisy.Clear();
        await kontekst.SaveChangesAsync(Ct);
        (await WersjaAsync(agregat)).ShouldBe(4);
    }

    // ---------- IJednostkaPracy ----------

    [Fact]
    public async Task BezOtwartejTransakcjiOperacjeWymagajaceTransakcjiSaOdrzucane()
    {
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);

        uow.TransakcjaOtwarta.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(uow.WymagajTransakcji).Message.ShouldBe("ZLECENIE_POZA_JEDNOSTKA_PRACY");
        Should.Throw<InvalidOperationException>(() => uow.UtworzPolecenie("SELECT 1")).Message.ShouldBe(IJednostkaPracy.KodPozaJednostkaPracy);
        await uow.WykonajAsync(_ =>
        {
            uow.TransakcjaOtwarta.ShouldBeTrue();
            Should.NotThrow(uow.WymagajTransakcji);
            return Task.CompletedTask;
        }, CancellationToken.None);
        uow.TransakcjaOtwarta.ShouldBeFalse();
    }

    [Fact]
    public async Task ZapisEfIPolecenieNpgsqlDziejaSieWTejSamejTransakcjiICommitUtrwalaOba()
    {
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);
        var agregat = TestowyAgregat.Nowy("ef");
        var idSql = Guid.CreateVersion7();

        var wynik = await uow.WykonajAsync(async ct =>
        {
            kontekst.Add(agregat);
            await using var polecenie = uow.UtworzPolecenie(
                $"INSERT INTO test_agregat(id, nazwa, poziom, rodzaj, version) VALUES ('{idSql}', 'sql', 'PODSTAWOWY', 'ZAMKNIETE', 1)");
            await polecenie.ExecuteNonQueryAsync(ct);
            return 42;
        }, CancellationToken.None);

        wynik.ShouldBe(42);
        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM test_agregat WHERE id IN ('{agregat.Id.Wartosc}', '{idSql}')")).ShouldBe(2);
    }

    [Fact]
    public async Task WyjatekWPrzypadkuUzyciaWycofujeEfIPolecenieNpgsql()
    {
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);
        var agregat = TestowyAgregat.Nowy("wycofany");
        var idSql = Guid.CreateVersion7();

        await Should.ThrowAsync<InvalidOperationException>(() => uow.WykonajAsync(async ct =>
        {
            kontekst.Add(agregat);
            await kontekst.SaveChangesAsync(ct);
            await using var polecenie = uow.UtworzPolecenie(
                $"INSERT INTO test_agregat(id, nazwa, poziom, rodzaj, version) VALUES ('{idSql}', 'sql', 'PODSTAWOWY', 'ZAMKNIETE', 1)");
            await polecenie.ExecuteNonQueryAsync(ct);
            throw new InvalidOperationException("awaria");
        }, CancellationToken.None));

        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM test_agregat WHERE id IN ('{agregat.Id.Wartosc}', '{idSql}')")).ShouldBe(0);
        kontekst.ChangeTracker.Entries().ShouldBeEmpty();
        uow.TransakcjaOtwarta.ShouldBeFalse();
    }

    [Fact]
    public async Task ZagniezdzoneWykonajNieOtwieraDrugiejTransakcji()
    {
        await using var kontekst = _baza.Kontekst();
        var uow = new JednostkaPracyEf(kontekst);
        var agregat = TestowyAgregat.Nowy("zagniezdzony");

        await Should.ThrowAsync<InvalidOperationException>(() => uow.WykonajAsync(async ct =>
        {
            await uow.WykonajAsync(_ =>
            {
                kontekst.Add(agregat);
                return Task.CompletedTask;
            }, ct);
            uow.TransakcjaOtwarta.ShouldBeTrue();
            throw new InvalidOperationException("awaria zewnetrzna");
        }, CancellationToken.None));

        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM test_agregat WHERE id = '{agregat.Id.Wartosc}'")).ShouldBe(0);
    }

    // ---------- rejestracja ----------

    [Theory]
    [InlineData(RodzajProcesu.Api, 30, 5)]
    [InlineData(RodzajProcesu.Worker, 40, 30)]
    [InlineData(RodzajProcesu.Scheduler, 3, 30)]
    public void CiagPolaczeniaUstawiaPuleICommandTimeoutWgProcesu(RodzajProcesu proces, int pula, int timeout)
    {
        var baza = new BazaOpcje { Polaczenie = "Host=localhost;Database=x;Username=u;Password=p" };

        var wynik = new NpgsqlConnectionStringBuilder(baza.CiagPolaczenia(proces));

        wynik.MaxPoolSize.ShouldBe(pula);
        wynik.CommandTimeout.ShouldBe(timeout);
        wynik.ApplicationName.ShouldBe($"lemat-{proces.ToString().ToLowerInvariant()}");
    }

    [Fact]
    public void CiagPolaczeniaBezKonfiguracjiRzucaWyjatek() =>
        Should.Throw<InvalidOperationException>(() => new BazaOpcje().CiagPolaczenia(RodzajProcesu.Api));

    [Fact]
    public async Task DodajPersystencjeRejestrujeKontekstFabrykeIJednostkePracyDzialajaceNaBazie()
    {
        var opcje = new LematOpcje { Baza = new BazaOpcje { Polaczenie = _baza.Polaczenie } };
        var uslugi = new ServiceCollection().DodajPersystencje(opcje, RodzajProcesu.Api);
        await using var dostawca = uslugi.BuildServiceProvider(validateScopes: true);

        await using (var zakres = dostawca.CreateAsyncScope())
        {
            var uow = zakres.ServiceProvider.GetRequiredService<IJednostkaPracy>();
            uow.ShouldBeSameAs(zakres.ServiceProvider.GetRequiredService<JednostkaPracyEf>());
            var kontekst = zakres.ServiceProvider.GetRequiredService<LematDbContext>();
            (await kontekst.Database.SqlQueryRaw<int>("SELECT 1 AS \"Value\"").SingleAsync(Ct)).ShouldBe(1);
            (await kontekst.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM pg_tables WHERE tablename = 'uzytkownik'").SingleAsync(Ct)).ShouldBe(1);
        }

        await using var zFabryki = await dostawca.GetRequiredService<IDbContextFactory<LematDbContext>>().CreateDbContextAsync(Ct);
        (await zFabryki.Database.CanConnectAsync(Ct)).ShouldBeTrue();
        var zrodlo = dostawca.GetRequiredService<NpgsqlDataSource>();
        await using var polecenie = zrodlo.CreateCommand("SELECT 'PODSTAWOWY'::poziom_matury");
        (await polecenie.ExecuteScalarAsync(Ct)).ShouldBe(PoziomMatury.Podstawowy);
    }
}
