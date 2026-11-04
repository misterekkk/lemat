// @Role: Testy bazy modułu Kalibracja (PostgreSQL): zapis Kalibratora tożsamościowego i izotonicznego (punkty jsonb), Próg pewności (CHECK zakresu 0,800–1,000, dowód = 1,000, rodzaj ≠ ZAMKNIETE), jeden aktywny Próg na parę (ux_prog_aktywny), odczyt Aktywnej kalibracji, DI | @DependsOn: BazaTestowa, KalibracjaFasada, repozytoria EF Kalibracji, ModeleFasada | @UsedBy: dotnet test | @Task: TASK-018
using Lemat.Application.Calibration;
using Lemat.Application.Common;
using Lemat.Application.Models;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Models;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Conventions;
using Lemat.Infrastructure.Persistence.Repositories.Calibration;
using Lemat.Infrastructure.Persistence.Repositories.Models;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class CalibrationPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private BazaTestowa _baza = null!;

    private KonfiguracjaModeluId _konfiguracja;

    public async ValueTask InitializeAsync()
    {
        _baza = await BazaTestowa.UtworzAsync(postgres);
        await using var kontekst = _baza.Kontekst();
        var modele = new ModeleFasada(
            new KonfiguracjaModeluRepozytoriumEf(kontekst),
            new DecyzjaModeluRepozytoriumEf(kontekst),
            new JednostkaPracyEf(kontekst),
            Options.Create(new LematOpcje()),
            _baza.Zegar);
        _konfiguracja = (await modele.ZapiszKonfiguracjeAsync(
            new NowaKonfiguracjaModelu(RolaModelu.DecyzyjnyZapasowy, DostawcyModeli.Mock, "mock-1", ParametryModelu.Domyslne, "ocena-v1", null), Ct)).Id;
    }

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    private async Task<T> ZFasadaAsync<T>(Func<KalibracjaFasada, Task<T>> operacja)
    {
        await using var kontekst = _baza.Kontekst();
        return await operacja(new KalibracjaFasada(
            new KalibratorPewnosciRepozytoriumEf(kontekst), new ProgPewnosciRepozytoriumEf(kontekst), new JednostkaPracyEf(kontekst), _baza.Zegar));
    }

    private Task<MigawkaKalibratora> KalibratorAsync(RodzajZadania rodzaj = RodzajZadania.OtwarteObliczeniowe) =>
        ZFasadaAsync(f => f.ZapiszKalibratorTozsamosciowyAsync(_konfiguracja, rodzaj, Ct));

    private Task<MigawkaProgu> UstawProgAsync(KalibratorPewnosciId kalibrator, decimal wartosc, RodzajZadania rodzaj = RodzajZadania.OtwarteObliczeniowe) =>
        ZFasadaAsync(f => f.UstawProgAsync(_konfiguracja, rodzaj, kalibrator, wartosc, ZrodloProgu.Administrator, "seed-dev", null, Ct));

    // ---------- Kalibrator pewności ----------

    [Fact]
    public async Task KalibratorTozsamosciowyZapisujePunktyJakoJsonbIZwracaWejscie()
    {
        var migawka = await KalibratorAsync();
        var id = migawka.KalibratorPewnosciId.Wartosc;

        (await _baza.SkalarAsync<string>($"SELECT metoda FROM kalibrator_pewnosci WHERE id = '{id}'")).ShouldBe("TOZSAMOSCIOWY");
        (await _baza.SkalarAsync<string>($"SELECT zrodlo::text FROM kalibrator_pewnosci WHERE id = '{id}'")).ShouldBe("KALIBRACJA_AUTOMATYCZNA");
        (await _baza.SkalarAsync<string>($"SELECT punkty->>'typ' FROM kalibrator_pewnosci WHERE id = '{id}'")).ShouldBe("TOZSAMOSCIOWY");
        (await _baza.SkalarAsync<string>($"SELECT punkty->'punkty'->1->>1 FROM kalibrator_pewnosci WHERE id = '{id}'")).ShouldBe("1");
        (await _baza.SkalarAsync<int>($"SELECT liczba_probek FROM kalibrator_pewnosci WHERE id = '{id}'")).ShouldBe(0);

        var odczytany = await ZFasadaAsync(f => f.PobierzKalibratorAsync(migawka.KalibratorPewnosciId, Ct));

        odczytany.ShouldNotBeNull().Metoda.ShouldBe(MetodaKalibratora.Tozsamosciowy);
        foreach (var x in new[] { 0m, 0.3m, 0.995m, 1m })
        {
            odczytany.Kalibruj(x).ShouldBe(x);
        }
    }

    [Fact]
    public async Task KalibratorIzotonicznyOdtwarzaPunktyIMetadaneZBazy()
    {
        var punkty = PunktyKalibratora.Utworz(
            PunktyKalibratora.TypIzotoniczny, [new(0m, 0.12m), new(0.5m, 0.41m), new(0.8m, 0.86m), new(0.95m, 0.985m), new(1m, 0.995m)]);
        var id = new KalibratorPewnosciId(Guid.NewGuid());
        await using (var kontekst = _baza.Kontekst())
        {
            new KalibratorPewnosciRepozytoriumEf(kontekst).Dodaj(KalibratorPewnosci.Izotoniczny(
                id, _konfiguracja, RodzajZadania.OtwarteObliczeniowe, punkty, 240, 0.0123m, _baza.Zegar.GetUtcNow().AddDays(-30), _baza.Zegar.GetUtcNow(), ZrodloKalibratora.EwaluacjaZt1, _baza.Zegar.GetUtcNow()));
            await new JednostkaPracyEf(kontekst).WykonajAsync(_ => Task.CompletedTask, Ct);
        }

        await using var odczyt = _baza.Kontekst();
        var kalibrator = (await new KalibratorPewnosciRepozytoriumEf(odczyt).PobierzNajnowszyAsync(_konfiguracja, RodzajZadania.OtwarteObliczeniowe, Ct))!;

        kalibrator.Id.ShouldBe(id);
        kalibrator.Metoda.ShouldBe(MetodaKalibratora.IzotonicznyCv5);
        kalibrator.Zrodlo.ShouldBe(ZrodloKalibratora.EwaluacjaZt1);
        kalibrator.LiczbaProbek.ShouldBe(240);
        kalibrator.Ece.ShouldBe(0.0123m);
        kalibrator.OknoOd.ShouldNotBeNull();
        kalibrator.Punkty.Punkty.ShouldBe(punkty.Punkty);
        kalibrator.Kalibruj(0.65m).ShouldBe(0.635m);
        (await _baza.SkalarAsync<string>($"SELECT metoda FROM kalibrator_pewnosci WHERE id = '{id.Wartosc}'")).ShouldBe("IZOTONICZNY_CV5");
    }

    [Fact]
    public async Task BazaOdrzucaKalibratorDlaZadanZamknietychIObcaMetode()
    {
        var zamkniete = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"INSERT INTO kalibrator_pewnosci (konfiguracja_modelu_id, rodzaj_zadania, metoda, punkty, liczba_probek, zrodlo) VALUES ('{_konfiguracja.Wartosc}', 'ZAMKNIETE', 'TOZSAMOSCIOWY', '{{}}', 0, 'EWALUACJA_ZT1')"));
        var metoda = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"INSERT INTO kalibrator_pewnosci (konfiguracja_modelu_id, rodzaj_zadania, metoda, punkty, liczba_probek, zrodlo) VALUES ('{_konfiguracja.Wartosc}', 'OTWARTE_OBLICZENIOWE', 'INNA', '{{}}', 0, 'EWALUACJA_ZT1')"));

        zamkniete.SqlState.ShouldBe("23514");
        metoda.SqlState.ShouldBe("23514");
    }

    // ---------- Próg pewności ----------

    [Fact]
    public async Task UstawienieProguZapisujeAktywnyProgIZwracaMigawke()
    {
        var kalibrator = await KalibratorAsync();

        var prog = await UstawProgAsync(kalibrator.KalibratorPewnosciId, 0.95m);

        prog.Wartosc.ShouldBe(0.95m);
        prog.TrybPelnejWeryfikacji.ShouldBeFalse();
        var id = prog.ProgPewnosciId.Wartosc;
        (await _baza.SkalarAsync<decimal>($"SELECT wartosc FROM prog_pewnosci WHERE id = '{id}'")).ShouldBe(0.95m);
        (await _baza.SkalarAsync<bool>($"SELECT aktywny FROM prog_pewnosci WHERE id = '{id}'")).ShouldBeTrue();
        (await _baza.SkalarAsync<string>($"SELECT zrodlo::text FROM prog_pewnosci WHERE id = '{id}'")).ShouldBe("ADMINISTRATOR");
        (await _baza.SkalarAsync<string>($"SELECT rodzaj_zadania::text FROM prog_pewnosci WHERE id = '{id}'")).ShouldBe("OTWARTE_OBLICZENIOWE");

        var aktywna = await ZFasadaAsync(f => f.PobierzAktywnaKalibracjeAsync(_konfiguracja, RodzajZadania.OtwarteObliczeniowe, Ct));

        aktywna.ShouldNotBeNull().Prog.ProgPewnosciId.ShouldBe(prog.ProgPewnosciId);
        aktywna.Kalibrator.KalibratorPewnosciId.ShouldBe(kalibrator.KalibratorPewnosciId);
        (await ZFasadaAsync(f => f.PobierzAktywnaKalibracjeAsync(_konfiguracja, RodzajZadania.OtwarteZDowodem, Ct))).ShouldBeNull();
    }

    [Fact]
    public async Task NowyProgDezaktywujePoprzedniAZmianaProguToNowyRekord()
    {
        var kalibrator = await KalibratorAsync();
        var pierwszy = await UstawProgAsync(kalibrator.KalibratorPewnosciId, 0.95m);

        var drugi = await UstawProgAsync(kalibrator.KalibratorPewnosciId, 0.97m);

        (await _baza.SkalarAsync<bool>($"SELECT aktywny FROM prog_pewnosci WHERE id = '{pierwszy.ProgPewnosciId.Wartosc}'")).ShouldBeFalse();
        (await _baza.SkalarAsync<bool>($"SELECT aktywny FROM prog_pewnosci WHERE id = '{drugi.ProgPewnosciId.Wartosc}'")).ShouldBeTrue();
        (await _baza.SkalarAsync<long>("SELECT count(*) FROM prog_pewnosci")).ShouldBe(2);
        (await ZFasadaAsync(f => f.PobierzAktywnyProgAsync(_konfiguracja, RodzajZadania.OtwarteObliczeniowe, Ct)))!.Wartosc.ShouldBe(0.97m);
    }

    [Fact]
    public async Task BazaWymuszaJedenAktywnyProgNaPare()
    {
        var kalibrator = await KalibratorAsync();
        await UstawProgAsync(kalibrator.KalibratorPewnosciId, 0.95m);

        var wyjatek = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"INSERT INTO prog_pewnosci (konfiguracja_modelu_id, rodzaj_zadania, kalibrator_pewnosci_id, wartosc, zrodlo, uzasadnienie, aktywny) VALUES ('{_konfiguracja.Wartosc}', 'OTWARTE_OBLICZENIOWE', '{kalibrator.KalibratorPewnosciId.Wartosc}', 0.9, 'ADMINISTRATOR', 'x', true)"));

        wyjatek.ConstraintName.ShouldBe("ux_prog_aktywny");
    }

    [Theory]
    [InlineData(0.799)]
    [InlineData(0.5)]
    [InlineData(1.001)]
    public async Task ProgPozaZakresemOdrzucajaDomenaIBaza(double wartosc)
    {
        var kalibrator = await KalibratorAsync();

        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => UstawProgAsync(kalibrator.KalibratorPewnosciId, (decimal)wartosc))).KodBledu.ShouldBe("PROG_POZA_ZAKRESEM");
        var wBazie = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"INSERT INTO prog_pewnosci (konfiguracja_modelu_id, rodzaj_zadania, kalibrator_pewnosci_id, wartosc, zrodlo, uzasadnienie) VALUES ('{_konfiguracja.Wartosc}', 'OTWARTE_OBLICZENIOWE', '{kalibrator.KalibratorPewnosciId.Wartosc}', {wartosc.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 'ADMINISTRATOR', 'x')"));
        wBazie.SqlState.ShouldBe("23514");
    }

    [Fact]
    public async Task DlaDowoduProgWynosiTysiacWDomenieIWBazie()
    {
        var kalibrator = await KalibratorAsync(RodzajZadania.OtwarteZDowodem);

        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => UstawProgAsync(kalibrator.KalibratorPewnosciId, 0.95m, RodzajZadania.OtwarteZDowodem)))
            .KodBledu.ShouldBe("PROG_DOWODU");
        var wBazie = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"INSERT INTO prog_pewnosci (konfiguracja_modelu_id, rodzaj_zadania, kalibrator_pewnosci_id, wartosc, zrodlo, uzasadnienie) VALUES ('{_konfiguracja.Wartosc}', 'OTWARTE_Z_DOWODEM', '{kalibrator.KalibratorPewnosciId.Wartosc}', 0.95, 'ADMINISTRATOR', 'x')"));
        wBazie.SqlState.ShouldBe("23514");

        var prog = await UstawProgAsync(kalibrator.KalibratorPewnosciId, 1.000m, RodzajZadania.OtwarteZDowodem);

        prog.TrybPelnejWeryfikacji.ShouldBeTrue();
        (await _baza.SkalarAsync<decimal>($"SELECT wartosc FROM prog_pewnosci WHERE id = '{prog.ProgPewnosciId.Wartosc}'")).ShouldBe(1.000m);
    }

    [Fact]
    public async Task ProgDlaZadanZamknietychOdrzucajaDomenaIBaza()
    {
        var kalibrator = await KalibratorAsync();

        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => UstawProgAsync(kalibrator.KalibratorPewnosciId, 0.95m, RodzajZadania.Zamkniete)))
            .KodBledu.ShouldBe("PROG_DLA_ZAMKNIETYCH");
        (await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"INSERT INTO prog_pewnosci (konfiguracja_modelu_id, rodzaj_zadania, kalibrator_pewnosci_id, wartosc, zrodlo, uzasadnienie) VALUES ('{_konfiguracja.Wartosc}', 'ZAMKNIETE', '{kalibrator.KalibratorPewnosciId.Wartosc}', 0.95, 'ADMINISTRATOR', 'x')")))
            .SqlState.ShouldBe("23514");
    }

    // ---------- rejestracja enumów i DI ----------

    [Fact]
    public async Task ModulKalibracjiRejestrujeSieWDiZWalidacjaZakresowIBudowy()
    {
        var opcje = new LematOpcje { Baza = new BazaOpcje { Polaczenie = _baza.Polaczenie } };
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(ZrodloProgu), "zrodlo_progu"));
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(ZrodloKalibratora), "zrodlo_kalibratora"));
        var uslugi = new ServiceCollection().DodajPersystencje(opcje, RodzajProcesu.Api).DodajModulKalibracji();
        await using var dostawca = uslugi.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        await using var zakres = dostawca.CreateAsyncScope();
        var fasada = zakres.ServiceProvider.GetRequiredService<IKalibracjaFasada>();
        (await fasada.PobierzAktywnyProgAsync(_konfiguracja, RodzajZadania.OtwarteObliczeniowe, Ct)).ShouldBeNull();
    }
}
