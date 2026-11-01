// @Role: Testy bazy modułu Klasy (PostgreSQL): zapis Klasy z Kodem zaproszenia, unikalność Kodu i aktywnego członkostwa (PK klasa_id + uczen_id), dołączanie, opuszczanie, odnowienie i unieważnienie kodu, archiwizacja (CHECK), Przekazanie, If-Match, uprawnienia obiektowe, kolejność Klas Ucznia, DI | @DependsOn: BazaTestowa, KlasyFasada, repozytorium EF Klas, KontaFasada, JednostkaPracyEf | @UsedBy: dotnet test | @Task: TASK-018
using Lemat.Application.Accounts;
using Lemat.Application.Classes;
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Domain.Classes;
using Lemat.Domain.Common;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Repositories.Accounts;
using Lemat.Infrastructure.Persistence.Repositories.Classes;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class ClassesPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private BazaTestowa _baza = null!;

    public async ValueTask InitializeAsync() => _baza = await BazaTestowa.UtworzAsync(postgres);

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    private async Task<KontoSkrot> ZarejestrujAsync(RolaUzytkownika rola, string email)
    {
        await using var kontekst = _baza.Kontekst();
        var konta = new KontaFasada(
            new UzytkownikRepozytoriumEf(kontekst),
            new UczenRepozytoriumEf(kontekst),
            new NauczycielRepozytoriumEf(kontekst),
            new JednostkaPracyEf(kontekst),
            _baza.Zegar);
        return await konta.ZarejestrujAsync(
            new RejestracjaKontaWejscie(rola, email, "argon2id$hash", "Jan", "Kowalski", rola == RolaUzytkownika.Uczen, AutozatwierdzNauczyciela: true), Ct);
    }

    private async Task<NauczycielId> NowyNauczycielAsync(string email = "n@example.com") =>
        NauczycielId.Z((await ZarejestrujAsync(RolaUzytkownika.Nauczyciel, email)).UzytkownikId);

    private async Task<UczenId> NowyUczenAsync(string email = "u@example.com") =>
        UczenId.Z((await ZarejestrujAsync(RolaUzytkownika.Uczen, email)).UzytkownikId);

    private async Task<T> ZFasadaAsync<T>(Func<IKlasyFasada, Task<T>> operacja)
    {
        await using var kontekst = _baza.Kontekst();
        return await operacja(new KlasyFasada(new KlasaRepozytoriumEf(kontekst), new JednostkaPracyEf(kontekst), _baza.Zegar));
    }

    private async Task ZFasadaAsync(Func<IKlasyFasada, Task> operacja)
    {
        await using var kontekst = _baza.Kontekst();
        await operacja(new KlasyFasada(new KlasaRepozytoriumEf(kontekst), new JednostkaPracyEf(kontekst), _baza.Zegar));
    }

    private Task<KlasaSkrot> ZalozAsync(NauczycielId nauczyciel, string nazwa = "3A") =>
        ZFasadaAsync(f => f.ZalozAsync(nauczyciel, nazwa, PoziomMatury.Rozszerzony, Ct));

    // ---------- zakładanie ----------

    [Fact]
    public async Task ZalozenieKlasyZapisujeKlaseZKodemWaznymCzternascieDni()
    {
        var nauczyciel = await NowyNauczycielAsync();

        var klasa = await ZalozAsync(nauczyciel, "  3A matematyka ");

        var id = klasa.Id.Wartosc;
        (await _baza.SkalarAsync<string>($"SELECT nazwa FROM klasa WHERE id = '{id}'")).ShouldBe("3A matematyka");
        (await _baza.SkalarAsync<string>($"SELECT poziom_matury::text FROM klasa WHERE id = '{id}'")).ShouldBe("ROZSZERZONY");
        (await _baza.SkalarAsync<string>($"SELECT kod_zaproszenia FROM klasa WHERE id = '{id}'")).ShouldBe(klasa.KodZaproszenia!.Wartosc);
        (await _baza.SkalarAsync<DateTime>($"SELECT kod_wazny_do FROM klasa WHERE id = '{id}'")).ShouldBe(_baza.Zegar.GetUtcNow().AddDays(14).UtcDateTime);
        (await _baza.SkalarAsync<bool>($"SELECT zarchiwizowana FROM klasa WHERE id = '{id}'")).ShouldBeFalse();
        (await _baza.SkalarAsync<int>($"SELECT version FROM klasa WHERE id = '{id}'")).ShouldBe(1);
        (await _baza.SkalarAsync<Guid>($"SELECT nauczyciel_id FROM klasa WHERE id = '{id}'")).ShouldBe(nauczyciel.Wartosc);
    }

    [Fact]
    public async Task ZapisKlasyZTymSamymKodemDajeKodZaproszeniaZajety()
    {
        var nauczyciel = await NowyNauczycielAsync();
        var kod = GeneratorKoduZaproszenia.Nowy(_baza.Zegar.GetUtcNow());
        await using var kontekst = _baza.Kontekst();
        var jednostka = new JednostkaPracyEf(kontekst);
        var repozytorium = new KlasaRepozytoriumEf(kontekst);

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => jednostka.WykonajAsync(
            _ =>
            {
                repozytorium.Dodaj(Klasa.Zaloz(new KlasaId(Guid.NewGuid()), nauczyciel, "A", PoziomMatury.Podstawowy, kod, _baza.Zegar.GetUtcNow()));
                repozytorium.Dodaj(Klasa.Zaloz(new KlasaId(Guid.NewGuid()), nauczyciel, "B", PoziomMatury.Podstawowy, kod, _baza.Zegar.GetUtcNow()));
                return Task.CompletedTask;
            },
            Ct));

        blad.KodBledu.ShouldBe("KOD_ZAPROSZENIA_ZAJETY");
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM klasa")).ShouldBe(0);
    }

    [Fact]
    public async Task BazaOdrzucaKlaseBezNauczycielaKtoraNieJestZarchiwizowana()
    {
        var wyjatek = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            "INSERT INTO klasa (nazwa, poziom_matury, kod_zaproszenia, kod_wazny_do) VALUES ('X', 'PODSTAWOWY', 'ABCD2345', now())"));

        wyjatek.SqlState.ShouldBe("23514");
    }

    // ---------- dołączanie i unikalność członkostwa ----------

    [Fact]
    public async Task DolaczenieKodemZapisujeCzlonkostwoIPodnosiWersje()
    {
        var klasa = await ZalozAsync(await NowyNauczycielAsync());
        var uczen = await NowyUczenAsync();

        var po = await ZFasadaAsync(f => f.DolaczAsync(uczen, klasa.KodZaproszenia!.Wartosc.ToLowerInvariant(), Ct));

        po.LiczbaUczniow.ShouldBe(1);
        po.KodZaproszenia.ShouldBeNull();
        po.Version.ShouldBe(2);
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM czlonkostwo_klasy WHERE klasa_id = '{klasa.Id.Wartosc}' AND uczen_id = '{uczen.Wartosc}'")).ShouldBe(1);
        (await _baza.SkalarAsync<int>($"SELECT version FROM klasa WHERE id = '{klasa.Id.Wartosc}'")).ShouldBe(2);
        (await ZFasadaAsync(f => f.PobierzAsync(klasa.Id, Ct)))!.LiczbaUczniow.ShouldBe(1);
    }

    [Fact]
    public async Task PowtorneDolaczenieNieTworzyDuplikatuABazaWymuszaUnikalnoscCzlonkostwa()
    {
        var klasa = await ZalozAsync(await NowyNauczycielAsync());
        var uczen = await NowyUczenAsync();
        await ZFasadaAsync(f => f.DolaczAsync(uczen, klasa.KodZaproszenia!.Wartosc, Ct));

        await ZFasadaAsync(f => f.DolaczAsync(uczen, klasa.KodZaproszenia!.Wartosc, Ct));

        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM czlonkostwo_klasy WHERE klasa_id = '{klasa.Id.Wartosc}'")).ShouldBe(1);
        (await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"INSERT INTO czlonkostwo_klasy (klasa_id, uczen_id) VALUES ('{klasa.Id.Wartosc}', '{uczen.Wartosc}')"))).SqlState.ShouldBe("23505");
    }

    [Fact]
    public async Task NieznanyIWygaslyKodNieDajaCzlonkostwa()
    {
        var klasa = await ZalozAsync(await NowyNauczycielAsync());
        var uczen = await NowyUczenAsync();

        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.DolaczAsync(uczen, "ZLYKOD22", Ct)))).KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEWAZNY");
        _baza.Zegar.Teraz = _baza.Zegar.Teraz.AddDays(15);
        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.DolaczAsync(uczen, klasa.KodZaproszenia!.Wartosc, Ct))))
            .KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEWAZNY");
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM czlonkostwo_klasy")).ShouldBe(0);
    }

    [Fact]
    public async Task OpuszczenieKlasyUsuwaCzlonkostwo()
    {
        var klasa = await ZalozAsync(await NowyNauczycielAsync());
        var uczen = await NowyUczenAsync();
        await ZFasadaAsync(f => f.DolaczAsync(uczen, klasa.KodZaproszenia!.Wartosc, Ct));

        await ZFasadaAsync(f => f.OpuscAsync(uczen, klasa.Id, Ct));

        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM czlonkostwo_klasy")).ShouldBe(0);
        (await ZFasadaAsync(f => f.ListaKlasUczniaAsync(uczen, Ct))).ShouldBeEmpty();
    }

    // ---------- kod: odnowienie i unieważnienie ----------

    [Fact]
    public async Task OdnowienieKoduUniewaznaStaryAUniewaznienieBlokujeDolaczenie()
    {
        var nauczyciel = await NowyNauczycielAsync();
        var klasa = await ZalozAsync(nauczyciel);
        var uczen = await NowyUczenAsync();
        var staryKod = klasa.KodZaproszenia!.Wartosc;

        var nowyKod = await ZFasadaAsync(f => f.OdnowKodAsync(nauczyciel, klasa.Id, 1, Ct));

        nowyKod.Wartosc.ShouldNotBe(staryKod);
        (await _baza.SkalarAsync<string>($"SELECT kod_zaproszenia FROM klasa WHERE id = '{klasa.Id.Wartosc}'")).ShouldBe(nowyKod.Wartosc);
        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.DolaczAsync(uczen, staryKod, Ct)))).KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEWAZNY");

        await ZFasadaAsync(f => f.UniewaznKodAsync(nauczyciel, klasa.Id, null, Ct));
        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.DolaczAsync(uczen, nowyKod.Wartosc, Ct)))).KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEWAZNY");
        (await _baza.SkalarAsync<int>($"SELECT version FROM klasa WHERE id = '{klasa.Id.Wartosc}'")).ShouldBe(3);
    }

    // ---------- uprawnienia obiektowe, If-Match ----------

    [Fact]
    public async Task CudzaKlasaDajeKlasaNieZnalezionaDlaOdczytuIZmian()
    {
        var klasa = await ZalozAsync(await NowyNauczycielAsync("a@example.com"));
        var obcy = await NowyNauczycielAsync("b@example.com");

        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.PobierzDlaNauczycielaAsync(obcy, klasa.Id, Ct)))).KodBledu.ShouldBe("KLASA_NIE_ZNALEZIONA");
        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.ArchiwizujAsync(obcy, klasa.Id, null, Ct)))).KodBledu.ShouldBe("KLASA_NIE_ZNALEZIONA");
        (await ZFasadaAsync(f => f.ListaKlasNauczycielaAsync(obcy, Ct))).ShouldBeEmpty();
        (await ZFasadaAsync(f => f.ListaKlasNauczycielaAsync(klasa.NauczycielId!.Value, Ct))).ShouldHaveSingleItem().KodZaproszenia.ShouldNotBeNull();
    }

    [Fact]
    public async Task PrzestarzalaWersjaIfMatchDajeKonfliktWspolbieznosci()
    {
        var nauczyciel = await NowyNauczycielAsync();
        var klasa = await ZalozAsync(nauczyciel);
        await ZFasadaAsync(f => f.OdnowKodAsync(nauczyciel, klasa.Id, 1, Ct));

        await Should.ThrowAsync<KonfliktWspolbieznosciException>(() => ZFasadaAsync(f => f.OdnowKodAsync(nauczyciel, klasa.Id, 1, Ct)));
    }

    // ---------- archiwizacja i Przekazanie ----------

    [Fact]
    public async Task ArchiwizacjaZapisujeZnacznikIBlokujeDolaczanie()
    {
        var nauczyciel = await NowyNauczycielAsync();
        var klasa = await ZalozAsync(nauczyciel);
        var uczen = await NowyUczenAsync();

        await ZFasadaAsync(f => f.ArchiwizujAsync(nauczyciel, klasa.Id, 1, Ct));

        (await _baza.SkalarAsync<bool>($"SELECT zarchiwizowana FROM klasa WHERE id = '{klasa.Id.Wartosc}'")).ShouldBeTrue();
        (await _baza.SkalarAsync<bool>($"SELECT zarchiwizowano_at IS NOT NULL FROM klasa WHERE id = '{klasa.Id.Wartosc}'")).ShouldBeTrue();
        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZFasadaAsync(f => f.DolaczAsync(uczen, klasa.KodZaproszenia!.Wartosc, Ct)))).KodBledu.ShouldBe("KLASA_ZARCHIWIZOWANA");
        (await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"UPDATE klasa SET zarchiwizowana = false WHERE id = '{klasa.Id.Wartosc}'"))).SqlState.ShouldBe("23514");
    }

    [Fact]
    public async Task PrzekazanieKlasyZmieniaNauczycielaWBazie()
    {
        var klasa = await ZalozAsync(await NowyNauczycielAsync("a@example.com"));
        var nowy = await NowyNauczycielAsync("b@example.com");

        var po = await ZFasadaAsync(f => f.PrzekazAsync(klasa.Id, nowy, 1, Ct));

        po.NauczycielId.ShouldBe(nowy);
        po.Version.ShouldBe(2);
        (await _baza.SkalarAsync<Guid>($"SELECT nauczyciel_id FROM klasa WHERE id = '{klasa.Id.Wartosc}'")).ShouldBe(nowy.Wartosc);
    }

    // ---------- listy Ucznia ----------

    [Fact]
    public async Task KlasyUczniaSaOdNajpozniejDolaczonejAFasadaWskazujeNajpozniejDolaczona()
    {
        var nauczyciel = await NowyNauczycielAsync();
        var pierwsza = await ZalozAsync(nauczyciel, "Pierwsza");
        var druga = await ZalozAsync(nauczyciel, "Druga");
        var uczen = await NowyUczenAsync();
        await ZFasadaAsync(f => f.DolaczAsync(uczen, pierwsza.KodZaproszenia!.Wartosc, Ct));
        _baza.Zegar.Teraz = _baza.Zegar.Teraz.AddHours(1);
        await ZFasadaAsync(f => f.DolaczAsync(uczen, druga.KodZaproszenia!.Wartosc, Ct));

        var lista = await ZFasadaAsync(f => f.ListaKlasUczniaAsync(uczen, Ct));
        var najnowsza = await ZFasadaAsync(f => f.PobierzNajpozniejDolaczonaKlaseAsync(uczen, Ct));

        lista.Select(k => k.Nazwa).ShouldBe(["Druga", "Pierwsza"]);
        najnowsza.ShouldNotBeNull().Id.ShouldBe(druga.Id);

        await ZFasadaAsync(f => f.ArchiwizujAsync(nauczyciel, druga.Id, null, Ct));
        (await ZFasadaAsync(f => f.PobierzNajpozniejDolaczonaKlaseAsync(uczen, Ct)))!.Id.ShouldBe(pierwsza.Id);
    }

    // ---------- DI ----------

    [Fact]
    public async Task ModulKlasyRejestrujeSieWDiZWalidacjaZakresowIBudowy()
    {
        var opcje = new LematOpcje { Baza = new BazaOpcje { Polaczenie = _baza.Polaczenie } };
        var uslugi = new ServiceCollection().DodajPersystencje(opcje, RodzajProcesu.Api).DodajModulKlasy();
        await using var dostawca = uslugi.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        await using var zakres = dostawca.CreateAsyncScope();
        (await zakres.ServiceProvider.GetRequiredService<IKlasyFasada>().PobierzAsync(new KlasaId(Guid.NewGuid()), Ct)).ShouldBeNull();
    }
}
