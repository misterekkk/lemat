// @Role: Testy bazy modułu Modele (PostgreSQL): round-trip Konfiguracji modelu (jsonb parametry, xmin), jedna aktywna na rolę (ux_konfiguracja_aktywna), A-07 przy aktywacji, zapis Decyzji modelu do partycji miesięcznej i odtworzenie rozkładu, brak partycji, DI | @DependsOn: BazaTestowa, ModeleFasada, repozytoria EF Modeli, JednostkaPracyEf | @UsedBy: dotnet test | @Task: TASK-018
using Lemat.Application.Common;
using Lemat.Application.Models;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Models;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Conventions;
using Lemat.Infrastructure.Persistence.Repositories.Models;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class ModelsPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private BazaTestowa _baza = null!;

    public async ValueTask InitializeAsync() => _baza = await BazaTestowa.UtworzAsync(postgres);

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    private async Task<T> ZFasadaAsync<T>(Func<ModeleFasada, Task<T>> operacja, string srodowisko = "Development")
    {
        await using var kontekst = _baza.Kontekst();
        return await operacja(new ModeleFasada(
            new KonfiguracjaModeluRepozytoriumEf(kontekst),
            new DecyzjaModeluRepozytoriumEf(kontekst),
            new JednostkaPracyEf(kontekst),
            Options.Create(new LematOpcje { Srodowisko = srodowisko }),
            _baza.Zegar));
    }

    private static NowaKonfiguracjaModelu Nowa(
        RolaModelu rola = RolaModelu.DecyzyjnyZapasowy,
        string dostawca = DostawcyModeli.Mock,
        ParametryModelu? parametry = null) =>
        new(rola, dostawca, "model-1", parametry ?? ParametryModelu.Domyslne, "ocena-v1", null);

    private Task<KonfiguracjaModeluSkrot> ZapiszAsync(NowaKonfiguracjaModelu konfiguracja) => ZFasadaAsync(f => f.ZapiszKonfiguracjeAsync(konfiguracja, Ct));

    // ---------- Konfiguracja modelu ----------

    [Fact]
    public async Task KonfiguracjaModeluZapisujeParametryJakoJsonbWSnakeCaseIOdtwarzaJe()
    {
        var parametry = new ParametryModelu(45.5m, 4096, 0.2m, 0.0123m, 0.0456m, 30, ReprezentacjaWzorow.LiniowaSympy, null, TrybJev.Wsadowy, null);
        var zapisana = await ZapiszAsync(Nowa(RolaModelu.DecyzyjnyGlowny, DostawcyModeli.TypesafeJev, parametry));
        var id = zapisana.Id.Wartosc;

        (await _baza.SkalarAsync<string>($"SELECT rola::text FROM konfiguracja_modelu WHERE id = '{id}'")).ShouldBe("DECYZYJNY_GLOWNY");
        (await _baza.SkalarAsync<string>($"SELECT parametry->>'reprezentacja_wzorow' FROM konfiguracja_modelu WHERE id = '{id}'")).ShouldBe("LINIOWA_SYMPY");
        (await _baza.SkalarAsync<string>($"SELECT parametry->>'tryb_jev' FROM konfiguracja_modelu WHERE id = '{id}'")).ShouldBe("WSADOWY");
        (await _baza.SkalarAsync<string>($"SELECT parametry->>'timeout_s' FROM konfiguracja_modelu WHERE id = '{id}'")).ShouldBe("45.5");
        (await _baza.SkalarAsync<string>($"SELECT parametry->>'retencja_dostawcy_dni' FROM konfiguracja_modelu WHERE id = '{id}'")).ShouldBe("30");
        (await _baza.SkalarAsync<bool>($"SELECT aktywna FROM konfiguracja_modelu WHERE id = '{id}'")).ShouldBeFalse();

        var lista = await ZFasadaAsync(f => f.ListaKonfiguracjiAsync(Ct));

        lista.ShouldHaveSingleItem().Parametry.ShouldBe(parametry);
    }

    [Fact]
    public async Task AktywacjaWylaczaPoprzedniaAktywnaTejSamejRoliAInneRoleZostaja()
    {
        var pierwsza = await ZapiszAsync(Nowa());
        var druga = await ZapiszAsync(Nowa());
        var innaRola = await ZapiszAsync(Nowa(RolaModelu.RozpoznawaniePisma));
        await ZFasadaAsync(f => f.AktywujAsync(pierwsza.Id, Ct));
        await ZFasadaAsync(f => f.AktywujAsync(innaRola.Id, Ct));

        var aktywna = await ZFasadaAsync(f => f.AktywujAsync(druga.Id, Ct));

        aktywna.Aktywna.ShouldBeTrue();
        (await _baza.SkalarAsync<bool>($"SELECT aktywna FROM konfiguracja_modelu WHERE id = '{pierwsza.Id.Wartosc}'")).ShouldBeFalse();
        (await _baza.SkalarAsync<bool>($"SELECT aktywna FROM konfiguracja_modelu WHERE id = '{druga.Id.Wartosc}'")).ShouldBeTrue();
        (await _baza.SkalarAsync<bool>($"SELECT aktywna FROM konfiguracja_modelu WHERE id = '{innaRola.Id.Wartosc}'")).ShouldBeTrue();
        (await ZFasadaAsync(f => f.PobierzAktywnaKonfiguracjeAsync(RolaModelu.DecyzyjnyZapasowy, Ct)))!.Id.ShouldBe(druga.Id);
        (await ZFasadaAsync(f => f.PobierzAktywnaKonfiguracjeAsync(RolaModelu.GeneratywnyWskazowki, Ct))).ShouldBeNull();
    }

    [Fact]
    public async Task BazaWymuszaJednaAktywnaKonfiguracjeNaRole()
    {
        var pierwsza = await ZapiszAsync(Nowa());
        var druga = await ZapiszAsync(Nowa());
        await ZFasadaAsync(f => f.AktywujAsync(pierwsza.Id, Ct));

        var wyjatek = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"UPDATE konfiguracja_modelu SET aktywna = true WHERE id = '{druga.Id.Wartosc}'"));

        wyjatek.ConstraintName.ShouldBe("ux_konfiguracja_aktywna");
    }

    [Fact]
    public async Task AktywacjaKonfiguracjiZRetencjaPonadA07IMockaWProductionJestOdrzucana()
    {
        var zaDlugaRetencja = await ZapiszAsync(Nowa(RolaModelu.RozpoznawaniePisma, DostawcyModeli.AzureOpenAi, ParametryModelu.Domyslne with { RetencjaDostawcyDni = 7 }));
        var mock = await ZapiszAsync(Nowa());

        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.AktywujAsync(zaDlugaRetencja.Id, Ct), "Production")))
            .KodBledu.ShouldBe("BRAK_RETENCJI_DOSTAWCY");
        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.AktywujAsync(mock.Id, Ct), "Production")))
            .KodBledu.ShouldBe("DOSTAWCA_NIEDOZWOLONY");
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM konfiguracja_modelu WHERE aktywna")).ShouldBe(0);
    }

    [Fact]
    public async Task RownoczesneZmianyKonfiguracjiWykrywaXminJakoKonfliktWspolbieznosci()
    {
        var zapisana = await ZapiszAsync(Nowa());
        await using var pierwszy = _baza.Kontekst();
        await using var drugi = _baza.Kontekst();
        var repozytoriumPierwszego = new KonfiguracjaModeluRepozytoriumEf(pierwszy);
        var repozytoriumDrugiego = new KonfiguracjaModeluRepozytoriumEf(drugi);
        var a = (await repozytoriumPierwszego.PobierzAsync(zapisana.Id, Ct))!;
        var b = (await repozytoriumDrugiego.PobierzAsync(zapisana.Id, Ct))!;
        a.Aktywuj(srodowiskoProdukcyjne: false);
        b.Aktywuj(srodowiskoProdukcyjne: false);
        await new JednostkaPracyEf(pierwszy).WykonajAsync(_ => Task.CompletedTask, Ct);

        await Should.ThrowAsync<KonfliktWspolbieznosciException>(() => new JednostkaPracyEf(drugi).WykonajAsync(_ => Task.CompletedTask, Ct));
    }

    // ---------- Decyzja modelu ----------

    private static byte[] Skrot(byte wartosc) => [.. Enumerable.Repeat(wartosc, 32)];

    private static IReadOnlyList<DecyzjaModelu> Decyzje(KonfiguracjaModeluId konfiguracja, Guid kontekstId, UczenId uczen, DateTimeOffset kiedy)
    {
        var rozkladChoice = new Dictionary<string, decimal> { ["BRAK_BLEDU"] = 0.7m, ["KROK_1"] = 0.2m, ["KROK_2"] = 0.1m };
        var rozkladSkali = new Dictionary<string, decimal> { ["0"] = 0.05m, ["1"] = 0.15m, ["2"] = 0.8m };
        var rozkladNoul = new Dictionary<string, decimal> { ["tak"] = 0.995m, ["nie"] = 0.005m };
        var pytanieChoice = new PytanieChoice("Q-OC-03", "Pierwszy błąd?", ["BRAK_BLEDU", "KROK_1", "KROK_2"]);
        var pytanieSkali = new PytanieScore("Q-OC-02.1.K1", "Ile punktów <K1> & więcej?", 0, 2);
        var pytanieNoul = new PytanieNoul("Q-OC-01", "Czy wynik równoważny?");
        var rozwiazanie = Guid.NewGuid();

        DecyzjaModelu Z(Pytanie p, Dictionary<string, decimal> r, string? stan = null) => DecyzjaModelu.Zapisz(
            new DecyzjaModeluId(Guid.NewGuid()), kiedy, KontekstDecyzji.Ocena, kontekstId, uczen, rozwiazanie, konfiguracja, p, DecyzjaSkladowa.ZRozkladu(p.Kod, p.Typ, r), 85, Skrot(7), stan);

        return
        [
            Z(pytanieChoice, rozkladChoice, "{\"stan\":\"pelny\"}"),
            Z(pytanieSkali, rozkladSkali),
            Z(pytanieNoul, rozkladNoul),
            DecyzjaModelu.ZapiszBlad(new DecyzjaModeluId(Guid.NewGuid()), kiedy, KontekstDecyzji.Ocena, kontekstId, uczen, rozwiazanie, konfiguracja, new PytanieNoul("Q-OC-06", "Błąd odczytu?"), "TIMEOUT", 30000, Skrot(9), null),
        ];
    }

    [Fact]
    public async Task ZapisDecyzjiModeluTrafiaDoPartycjiMiesiecznejIOdtwarzaRozklad()
    {
        var konfiguracja = await ZapiszAsync(Nowa());
        var kontekstId = Guid.NewGuid();
        var uczen = new UczenId(Guid.NewGuid());
        var teraz = TimeProvider.System.GetUtcNow();
        var zapisywane = Decyzje(konfiguracja.Id, kontekstId, uczen, teraz);

        await ZFasadaAsync(async f =>
        {
            await f.ZapiszDecyzjeAsync(zapisywane, Ct);
            return 0;
        });

        var partycja = $"decyzja_modelu_{teraz.UtcDateTime:yyyy_MM}";
        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM {partycja} WHERE kontekst_id = '{kontekstId}'")).ShouldBe(4);
        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM ONLY decyzja_modelu WHERE kontekst_id = '{kontekstId}'")).ShouldBe(0);
        (await _baza.SkalarAsync<string>($"SELECT DISTINCT tableoid::regclass::text FROM decyzja_modelu WHERE kontekst_id = '{kontekstId}'")).ShouldBe(partycja);
        (await _baza.SkalarAsync<string>($"SELECT rozklad->>'KROK_1' FROM decyzja_modelu WHERE kod_pytania = 'Q-OC-03'")).ShouldBe("0.2");
        (await _baza.SkalarAsync<string>("SELECT wybor #>> '{}' FROM decyzja_modelu WHERE kod_pytania = 'Q-OC-03'")).ShouldBe("BRAK_BLEDU");
        (await _baza.SkalarAsync<string>("SELECT opcje::text FROM decyzja_modelu WHERE kod_pytania = 'Q-OC-02.1.K1'")).ShouldBe("{\"max\": 2, \"min\": 0}");
        (await _baza.SkalarAsync<string>("SELECT typ_pytania::text FROM decyzja_modelu WHERE kod_pytania = 'Q-OC-02.1.K1'")).ShouldBe("SCORE");
        (await _baza.SkalarAsync<int>("SELECT length(skrot_stanu) FROM decyzja_modelu WHERE kod_pytania = 'Q-OC-03'")).ShouldBe(32);

        var odczytane = await ZFasadaAsync(f => f.PobierzDecyzjeAsync(KontekstDecyzji.Ocena, kontekstId, Ct));

        odczytane.Count.ShouldBe(4);
        var choice = odczytane.Single(d => d.KodPytania == "Q-OC-03");
        choice.Rozklad!.Count.ShouldBe(3);
        choice.Rozklad["BRAK_BLEDU"].ShouldBe(0.7m);
        choice.Rozklad["KROK_1"].ShouldBe(0.2m);
        choice.Rozklad["KROK_2"].ShouldBe(0.1m);
        choice.Rozklad.Values.Sum().ShouldBe(1m);
        choice.Wybor.ShouldBe("BRAK_BLEDU");
        choice.Opcje!.Lista.ShouldBe(["BRAK_BLEDU", "KROK_1", "KROK_2"]);
        choice.Stan.ShouldBe("{\"stan\":\"pelny\"}");
        choice.UczenId.ShouldBe(uczen);
        choice.KonfiguracjaModeluId.ShouldBe(konfiguracja.Id);
        var skala = odczytane.Single(d => d.KodPytania == "Q-OC-02.1.K1");
        skala.Opcje.ShouldBe(new OpcjePytania(null, 0, 2));
        skala.Rozklad!["2"].ShouldBe(0.8m);
        skala.Wybor.ShouldBe("2");
        odczytane.Single(d => d.KodPytania == "Q-OC-01").Opcje.ShouldBeNull();
        var blad = odczytane.Single(d => d.KodPytania == "Q-OC-06");
        blad.Blad.ShouldBe("TIMEOUT");
        blad.Wybor.ShouldBeNull();
        blad.Rozklad.ShouldBeNull();
        blad.CzasMs.ShouldBe(30000);
        blad.SkrotStanu.ShouldBe(Skrot(9));
    }

    [Fact]
    public async Task DecyzjeZRoznychMiesiecyTrafiajDoOdpowiednichPartycji()
    {
        var konfiguracja = await ZapiszAsync(Nowa());
        var kontekstId = Guid.NewGuid();
        var uczen = new UczenId(Guid.NewGuid());
        var teraz = TimeProvider.System.GetUtcNow();
        var pozniej = teraz.AddMonths(3);

        await ZFasadaAsync(async f =>
        {
            await f.ZapiszDecyzjeAsync([.. Decyzje(konfiguracja.Id, kontekstId, uczen, teraz).Take(1), .. Decyzje(konfiguracja.Id, kontekstId, uczen, pozniej).Skip(1).Take(1)], Ct);
            return 0;
        });

        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM decyzja_modelu_{teraz.UtcDateTime:yyyy_MM} WHERE kontekst_id = '{kontekstId}'")).ShouldBe(1);
        (await _baza.SkalarAsync<long>($"SELECT count(*) FROM decyzja_modelu_{pozniej.UtcDateTime:yyyy_MM} WHERE kontekst_id = '{kontekstId}'")).ShouldBe(1);
    }

    [Fact]
    public async Task ZapisDecyzjiBezPartycjiKonczySieBledemBazy()
    {
        var konfiguracja = await ZapiszAsync(Nowa());
        var odlegle = Decyzje(konfiguracja.Id, Guid.NewGuid(), new UczenId(Guid.NewGuid()), TimeProvider.System.GetUtcNow().AddYears(-5));

        await Should.ThrowAsync<Exception>(() => ZFasadaAsync(async f =>
        {
            await f.ZapiszDecyzjeAsync(odlegle, Ct);
            return 0;
        }));

        (await _baza.SkalarAsync<long>("SELECT count(*) FROM decyzja_modelu")).ShouldBe(0);
    }

    [Fact]
    public async Task OdczytDecyzjiInnegoKontekstuNieZwracaCudzychWierszy()
    {
        var konfiguracja = await ZapiszAsync(Nowa());
        var kontekstId = Guid.NewGuid();
        await ZFasadaAsync(async f =>
        {
            await f.ZapiszDecyzjeAsync(Decyzje(konfiguracja.Id, kontekstId, new UczenId(Guid.NewGuid()), TimeProvider.System.GetUtcNow()), Ct);
            return 0;
        });

        (await ZFasadaAsync(f => f.PobierzDecyzjeAsync(KontekstDecyzji.Rekomendacja, kontekstId, Ct))).ShouldBeEmpty();
        (await ZFasadaAsync(f => f.PobierzDecyzjeAsync(KontekstDecyzji.Ocena, Guid.NewGuid(), Ct))).ShouldBeEmpty();
    }

    // ---------- rejestracja enumów i DI ----------

    [Fact]
    public async Task EnumyModelowSaZarejestrowaneIPasujaDoTypowPostgres()
    {
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(RolaModelu), "rola_modelu"));
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(TypPytania), "typ_pytania"));
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(KontekstDecyzji), "kontekst_decyzji"));
        (await _baza.SkalarAsync<string>("SELECT string_agg(e.enumlabel, ',' ORDER BY e.enumsortorder) FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid WHERE t.typname = 'rola_modelu'"))
            .ShouldBe("DECYZYJNY_GLOWNY,DECYZYJNY_ZAPASOWY,ROZPOZNAWANIE_PISMA,GENERATYWNY_ZADANIA,GENERATYWNY_ROZWIAZUJACY_NIEZALEZNIE,GENERATYWNY_WSKAZOWKI,GENERATYWNY_WKLADKI,GENERATYWNY_OPINIA_POMOCNICZA");
        Enum.GetValues<RolaModelu>().Length.ShouldBe(8);
    }

    [Fact]
    public async Task ModulModeleRejestrujeSieWDiZWalidacjaZakresowIBudowy()
    {
        var opcje = new LematOpcje { Baza = new BazaOpcje { Polaczenie = _baza.Polaczenie } };
        var uslugi = new ServiceCollection().AddSingleton(Options.Create(opcje)).DodajPersystencje(opcje, RodzajProcesu.Api).DodajModulModele();
        await using var dostawca = uslugi.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        await using var zakres = dostawca.CreateAsyncScope();
        var fasada = zakres.ServiceProvider.GetRequiredService<IModeleFasada>();
        var zapisana = await fasada.ZapiszKonfiguracjeAsync(Nowa(), Ct);
        (await fasada.AktywujAsync(zapisana.Id, Ct)).Aktywna.ShouldBeTrue();
        (await fasada.PobierzAktywnaKonfiguracjeAsync(RolaModelu.DecyzyjnyZapasowy, Ct))!.Id.ShouldBe(zapisana.Id);
    }
}
