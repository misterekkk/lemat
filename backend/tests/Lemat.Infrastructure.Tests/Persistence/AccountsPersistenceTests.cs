// @Role: Testy bazy modułu Konta (PostgreSQL): zapis uzytkownik+uczen w jednej transakcji (FK odroczony), EMAIL_ZAJETY, CHECK weryfikator_globalny, karencja, wersjonowanie/If-Match, tokeny, próby logowania, DI | @DependsOn: BazaTestowa, KontaFasada, repozytoria EF Kont, JednostkaPracyEf | @UsedBy: dotnet test | @Task: TASK-016
using System.Net;
using Lemat.Application.Accounts;
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Conventions;
using Lemat.Infrastructure.Persistence.Repositories.Accounts;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class AccountsPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private BazaTestowa _baza = null!;

    public async ValueTask InitializeAsync() => _baza = await BazaTestowa.UtworzAsync(postgres);

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    private async Task<KontoSkrot> ZarejestrujAsync(RolaUzytkownika rola, string email, bool oswiadczenie = true, bool autozatwierdz = false)
    {
        await using var kontekst = _baza.Kontekst();
        var fasada = Fasada(kontekst);
        return await fasada.ZarejestrujAsync(
            new RejestracjaKontaWejscie(rola, email, "argon2id$hash", "Jan", "Kowalski", oswiadczenie, autozatwierdz), Ct);
    }

    private KontaFasada Fasada(LematDbContext kontekst) => new KontaFasada(
        new UzytkownikRepozytoriumEf(kontekst),
        new UczenRepozytoriumEf(kontekst),
        new NauczycielRepozytoriumEf(kontekst),
        new JednostkaPracyEf(kontekst),
        _baza.Zegar);

    // ---------- rejestracja: zapis w jednej transakcji ----------

    [Fact]
    public async Task RejestracjaUczniaZapisujeUzytkownikaIUczniaWJednejTransakcji()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Uczen, "  Jan@Example.COM ");

        konto.Rola.ShouldBe(RolaUzytkownika.Uczen);
        konto.Status.ShouldBe(StatusKonta.Aktywne);
        var id = konto.UzytkownikId.Wartosc;
        (await _baza.SkalarAsync<string>($"SELECT email::text FROM uzytkownik WHERE id = '{id}'")).ShouldBe("jan@example.com");
        (await _baza.SkalarAsync<string>($"SELECT haslo_hash FROM uzytkownik WHERE id = '{id}'")).ShouldBe("argon2id$hash");
        (await _baza.SkalarAsync<string>($"SELECT rola::text FROM uzytkownik WHERE id = '{id}'")).ShouldBe("UCZEN");
        (await _baza.SkalarAsync<string>($"SELECT status::text FROM uzytkownik WHERE id = '{id}'")).ShouldBe("AKTYWNE");
        (await _baza.SkalarAsync<bool>($"SELECT oswiadczenie_wieku_16 FROM uczen WHERE uzytkownik_id = '{id}'")).ShouldBeTrue();
        (await _baza.SkalarAsync<int>($"SELECT version FROM uzytkownik WHERE id = '{id}'")).ShouldBe(1);
        (await _baza.SkalarAsync<int>($"SELECT version FROM uczen WHERE uzytkownik_id = '{id}'")).ShouldBe(1);
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM nauczyciel WHERE uzytkownik_id = '{id}'")).ShouldBe(0);
    }

    [Fact]
    public async Task GrupaEksperymentuZapisanaWBaziePasujeDoFnv1aIdentyfikatora()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Uczen, "grupa@example.com");

        var zBazy = await _baza.SkalarAsync<short>($"SELECT procent_grupy_eksperymentu FROM uczen WHERE uzytkownik_id = '{konto.UzytkownikId.Wartosc}'");
        zBazy.ShouldBe((short)GrupaEksperymentu.Dla(konto.UzytkownikId).ProcentGrupyEksperymentu);

        await using var kontekst = _baza.Kontekst();
        var grupa = await Fasada(kontekst).PobierzGrupeEksperymentuAsync(UczenId.Z(konto.UzytkownikId), Ct);
        grupa.ShouldBe(GrupaEksperymentu.Dla(konto.UzytkownikId));
        (await Fasada(kontekst).PobierzGrupeEksperymentuAsync(new UczenId(Guid.NewGuid()), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task RejestracjaUczniaBezOswiadczeniaNieZapisujeNiczego()
    {
        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZarejestrujAsync(RolaUzytkownika.Uczen, "x@example.com", oswiadczenie: false));

        blad.KodBledu.ShouldBe("OSWIADCZENIE_WIEKU_WYMAGANE");
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM uzytkownik")).ShouldBe(0);
    }

    [Fact]
    public async Task NauczycielStartujeJakoOczekujeNaZatwierdzenieAlboAktywnyPrzyAutozatwierdzeniu()
    {
        var oczekujacy = await ZarejestrujAsync(RolaUzytkownika.Nauczyciel, "n1@example.com");
        var autozatwierdzony = await ZarejestrujAsync(RolaUzytkownika.Nauczyciel, "n2@example.com", autozatwierdz: true);

        oczekujacy.Status.ShouldBe(StatusKonta.OczekujeNaZatwierdzenie);
        autozatwierdzony.Status.ShouldBe(StatusKonta.Aktywne);
        (await _baza.SkalarAsync<string>($"SELECT status::text FROM uzytkownik WHERE id = '{oczekujacy.UzytkownikId.Wartosc}'")).ShouldBe("OCZEKUJE_NA_ZATWIERDZENIE");
        (await _baza.SkalarAsync<string>($"SELECT status::text FROM uzytkownik WHERE id = '{autozatwierdzony.UzytkownikId.Wartosc}'")).ShouldBe("AKTYWNE");
        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM nauczyciel WHERE uzytkownik_id = '{oczekujacy.UzytkownikId.Wartosc}'")).ShouldBe(1);
        (await _baza.SkalarAsync<bool>($"SELECT weryfikator_globalny FROM nauczyciel WHERE uzytkownik_id = '{oczekujacy.UzytkownikId.Wartosc}'")).ShouldBeFalse();
    }

    [Fact]
    public async Task AdministratoraZakladaSieBezTabeliPomocniczej()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Administrator, "admin@example.com");

        konto.Status.ShouldBe(StatusKonta.Aktywne);
        (await _baza.SkalarAsync<int>($"SELECT ((SELECT count(*) FROM uczen) + (SELECT count(*) FROM nauczyciel))::int")).ShouldBe(0);
    }

    // ---------- duplikat e-mail ----------

    [Fact]
    public async Task DuplikatEmailBezWzgleduNaWielkoscLiterDajeEmailZajety()
    {
        await ZarejestrujAsync(RolaUzytkownika.Uczen, "dup@example.com");

        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => ZarejestrujAsync(RolaUzytkownika.Uczen, "DUP@Example.com"));

        blad.KodBledu.ShouldBe("EMAIL_ZAJETY");
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM uzytkownik")).ShouldBe(1);
        (await _baza.SkalarAsync<int>("SELECT count(*)::int FROM uczen")).ShouldBe(1);
    }

    // ---------- CHECK weryfikator_globalny ----------

    [Fact]
    public async Task CheckWeryfikatorGlobalnyWymagaUmowyPoufnosciIAtLeast90Minut()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Nauczyciel, "w@example.com", autozatwierdz: true);
        var id = konto.UzytkownikId.Wartosc;

        var bezUmowy = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"UPDATE nauczyciel SET weryfikator_globalny = true, deklarowane_minuty_weryfikacji = 120 WHERE uzytkownik_id = '{id}'"));
        bezUmowy.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);

        var zaMaloMinut = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync(
            $"UPDATE nauczyciel SET weryfikator_globalny = true, deklarowane_minuty_weryfikacji = 89, umowa_poufnosci_at = now() WHERE uzytkownik_id = '{id}'"));
        zaMaloMinut.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);

        await _baza.WykonajAsync(
            $"UPDATE nauczyciel SET weryfikator_globalny = true, deklarowane_minuty_weryfikacji = 90, umowa_poufnosci_at = now() WHERE uzytkownik_id = '{id}'");
    }

    [Fact]
    public async Task StanDozwolonyPrzezDomeneJestAkceptowanyPrzezCheckIOdczytywanyZPowrotem()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Nauczyciel, "w2@example.com", autozatwierdz: true);
        var id = NauczycielId.Z(konto.UzytkownikId);
        var umowa = new DateTimeOffset(2026, 10, 15, 8, 30, 0, TimeSpan.Zero);

        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new NauczycielRepozytoriumEf(kontekst);
            await new JednostkaPracyEf(kontekst).WykonajAsync(async ct =>
            {
                var nauczyciel = (await repo.PobierzAsync(id, null, ct))!;
                nauczyciel.NadajWeryfikatoraGlobalnego(150, StatusKonta.Aktywne, false, 0, umowa);
                nauczyciel.NadajRecenzentaTresci(StatusKonta.Aktywne);
            }, Ct);
        }

        await using var odczyt = _baza.Kontekst();
        var wczytany = (await new NauczycielRepozytoriumEf(odczyt).PobierzDoOdczytuAsync(id, Ct))!;
        wczytany.WeryfikatorGlobalny.ShouldBeTrue();
        wczytany.RecenzentTresci.ShouldBeTrue();
        wczytany.DeklarowaneMinutyWeryfikacji.ShouldBe(150);
        wczytany.UmowaPoufnosciAt.ShouldBe(umowa);
        wczytany.Version.ShouldBe(2);
    }

    // ---------- karencja ----------

    [Fact]
    public async Task KarencjaZapisujeSieWKolumnachIPrzywrocenieJaCzysci()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Uczen, "karencja@example.com");
        var teraz = _baza.Zegar.Teraz;

        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new UzytkownikRepozytoriumEf(kontekst);
            await new JednostkaPracyEf(kontekst).WykonajAsync(async ct =>
                (await repo.PobierzAsync(konto.UzytkownikId, null, ct))!.ZlozWniosekOUsuniecie(teraz), Ct);
        }

        var id = konto.UzytkownikId.Wartosc;
        (await _baza.SkalarAsync<string>($"SELECT status::text FROM uzytkownik WHERE id = '{id}'")).ShouldBe("USUWANE");
        (await _baza.SkalarAsync<string>($"SELECT status_przed_wnioskiem::text FROM uzytkownik WHERE id = '{id}'")).ShouldBe("AKTYWNE");
        (await _baza.SkalarAsync<DateTime>($"SELECT usuniecie_zaplanowane_at FROM uzytkownik WHERE id = '{id}'")).ShouldBe(teraz.AddDays(30).UtcDateTime);

        await using (var kontekst = _baza.Kontekst())
        {
            var wczytany = (await new UzytkownikRepozytoriumEf(kontekst).PobierzDoOdczytuAsync(konto.UzytkownikId, Ct))!;
            wczytany.Status.ShouldBe(StatusKonta.Usuwane);
            wczytany.OkresKarencji.ShouldNotBeNull().StatusPrzedWnioskiem.ShouldBe(StatusKonta.Aktywne);
            wczytany.OkresKarencji.UsuniecieZaplanowaneAt.ShouldBe(teraz.AddDays(30));
        }

        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new UzytkownikRepozytoriumEf(kontekst);
            await new JednostkaPracyEf(kontekst).WykonajAsync(async ct =>
                (await repo.PobierzAsync(konto.UzytkownikId, null, ct))!.Przywroc(teraz.AddDays(1)), Ct);
        }

        (await _baza.SkalarAsync<string>($"SELECT status::text FROM uzytkownik WHERE id = '{id}'")).ShouldBe("AKTYWNE");
        (await _baza.SkalarAsync<bool>($"SELECT usuniecie_zaplanowane_at IS NULL AND status_przed_wnioskiem IS NULL FROM uzytkownik WHERE id = '{id}'")).ShouldBeTrue();
        await using var po = _baza.Kontekst();
        (await new UzytkownikRepozytoriumEf(po).PobierzDoOdczytuAsync(konto.UzytkownikId, Ct))!.OkresKarencji.ShouldBeNull();
    }

    // ---------- wersjonowanie i If-Match ----------

    [Fact]
    public async Task ZmianaKontaPodnosiWersjeIUpdatedAtAleLogowanieWersjiNieRuszaOstatnieLogowanieZapisuje()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Uczen, "wersja@example.com");
        var id = konto.UzytkownikId.Wartosc;
        _baza.Zegar.Teraz = _baza.Zegar.Teraz.AddHours(1);

        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new UzytkownikRepozytoriumEf(kontekst);
            await new JednostkaPracyEf(kontekst).WykonajAsync(async ct =>
                (await repo.PobierzAsync(konto.UzytkownikId, 1, ct))!.ZarejestrujLogowanie(_baza.Zegar.Teraz), Ct);
        }

        (await _baza.SkalarAsync<int>($"SELECT version FROM uzytkownik WHERE id = '{id}'")).ShouldBe(1);
        (await _baza.SkalarAsync<DateTime>($"SELECT ostatnie_logowanie_at FROM uzytkownik WHERE id = '{id}'")).ShouldBe(_baza.Zegar.Teraz.UtcDateTime);

        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new UzytkownikRepozytoriumEf(kontekst);
            await new JednostkaPracyEf(kontekst).WykonajAsync(async ct =>
                (await repo.PobierzAsync(konto.UzytkownikId, 1, ct))!.Zablokuj(_baza.Zegar.Teraz), Ct);
        }

        (await _baza.SkalarAsync<int>($"SELECT version FROM uzytkownik WHERE id = '{id}'")).ShouldBe(2);
        (await _baza.SkalarAsync<DateTime>($"SELECT updated_at FROM uzytkownik WHERE id = '{id}'")).ShouldBe(_baza.Zegar.Teraz.UtcDateTime);
    }

    [Fact]
    public async Task NieaktualnaWersjaIfMatchDajeKonfliktWspolbieznosci()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Uczen, "konflikt@example.com");
        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new UzytkownikRepozytoriumEf(kontekst);
            await new JednostkaPracyEf(kontekst).WykonajAsync(async ct =>
                (await repo.PobierzAsync(konto.UzytkownikId, 1, ct))!.Zablokuj(_baza.Zegar.Teraz), Ct);
        }

        await using var nieaktualny = _baza.Kontekst();
        var repoNieaktualne = new UzytkownikRepozytoriumEf(nieaktualny);
        await Should.ThrowAsync<KonfliktWspolbieznosciException>(() => new JednostkaPracyEf(nieaktualny).WykonajAsync(async ct =>
            (await repoNieaktualne.PobierzAsync(konto.UzytkownikId, 1, ct))!.Odblokuj(), Ct));
    }

    [Fact]
    public async Task RownoleglaZmianaTegoSamegoKontaDajeKonflikt()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Uczen, "rownolegle@example.com");
        await using var a = _baza.Kontekst();
        await using var b = _baza.Kontekst();
        var repoA = new UzytkownikRepozytoriumEf(a);
        var repoB = new UzytkownikRepozytoriumEf(b);
        var kontoA = (await repoA.PobierzAsync(konto.UzytkownikId, null, Ct))!;
        var kontoB = (await repoB.PobierzAsync(konto.UzytkownikId, null, Ct))!;
        kontoA.Zablokuj(_baza.Zegar.Teraz);
        kontoB.ZlozWniosekOUsuniecie(_baza.Zegar.Teraz);

        await a.SaveChangesAsync(Ct);

        await Should.ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() => b.SaveChangesAsync(Ct));
    }

    // ---------- odczyty ----------

    [Fact]
    public async Task WyszukiwaniePoEmailuIDaneLogowaniaNieZalezaOdWielkosciLiter()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Nauczyciel, "Szukany@Example.com");
        await using var kontekst = _baza.Kontekst();
        var repo = new UzytkownikRepozytoriumEf(kontekst);

        (await repo.PobierzPoEmailuAsync("SZUKANY@example.COM", Ct))!.Id.ShouldBe(konto.UzytkownikId);
        var dane = (await repo.PobierzDaneLogowaniaAsync(" szukany@EXAMPLE.com ", Ct))!;
        dane.UzytkownikId.ShouldBe(konto.UzytkownikId);
        dane.HasloHash.ShouldBe("argon2id$hash");
        dane.Rola.ShouldBe(RolaUzytkownika.Nauczyciel);
        dane.Status.ShouldBe(StatusKonta.OczekujeNaZatwierdzenie);
        (await repo.PobierzDaneLogowaniaAsync("brak@example.com", Ct)).ShouldBeNull();
        (await repo.PobierzPoEmailuAsync("brak@example.com", Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task FasadaZwracaSkrotKontaAlboNull()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Nauczyciel, "fasada@example.com");
        await using var kontekst = _baza.Kontekst();

        var skrot = await Fasada(kontekst).PobierzKontoAsync(konto.UzytkownikId, Ct);

        skrot.ShouldBe(konto);
        (await Fasada(kontekst).PobierzKontoAsync(new UzytkownikId(Guid.NewGuid()), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task UczenOdczytanyZBazyMaPoziomTerminDzialIZgode()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Uczen, "uczen@example.com");
        await _baza.WykonajAsync("INSERT INTO dzial (id, kod, nazwa, kolejnosc) VALUES (1, 'I', 'Liczby', 1) ON CONFLICT DO NOTHING");
        var id = UczenId.Z(konto.UzytkownikId);

        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new UczenRepozytoriumEf(kontekst);
            await new JednostkaPracyEf(kontekst).WykonajAsync(async ct =>
            {
                var uczen = (await repo.PobierzAsync(id, 1, ct))!;
                uczen.UstawPoziomITermin(PoziomMatury.Rozszerzony, new DateOnly(2027, 5, 12), new DateOnly(2026, 11, 1));
                uczen.WybierzDzial(new Lemat.Domain.Content.DzialId(1));
                uczen.UstawZgodeNaTrening(true);
            }, Ct);
        }

        await using var odczyt = _baza.Kontekst();
        var wczytany = (await new UczenRepozytoriumEf(odczyt).PobierzDoOdczytuAsync(id, Ct))!;
        wczytany.PoziomMatury.ShouldBe(PoziomMatury.Rozszerzony);
        wczytany.TerminEgzaminu.ShouldBe(new DateOnly(2027, 5, 12));
        wczytany.WybranyDzialId.ShouldBe(new Lemat.Domain.Content.DzialId(1));
        wczytany.ZgodaNaTrening.ShouldBeTrue();
        wczytany.GrupaEksperymentu.ShouldBe(GrupaEksperymentu.Dla(konto.UzytkownikId));
        wczytany.Version.ShouldBe(2);
    }

    // ---------- tokeny odświeżania ----------

    private static byte[] Skrot(byte wartosc) => Enumerable.Repeat(wartosc, 32).ToArray();

    [Fact]
    public async Task TokenOdswiezaniaZapisujeSieZNastepcaIDaSieUniewaznicRodzineIKonto()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Uczen, "token@example.com");
        var teraz = _baza.Zegar.Teraz;
        var rodzina = Guid.NewGuid();
        var pierwszy = TokenOdswiezania.Wydaj(new TokenOdswiezaniaId(Guid.NewGuid()), konto.UzytkownikId, rodzina, Skrot(1), Skrot(9), teraz, TimeSpan.FromDays(30));
        var drugi = TokenOdswiezania.Wydaj(new TokenOdswiezaniaId(Guid.NewGuid()), konto.UzytkownikId, rodzina, Skrot(2), Skrot(9), teraz, TimeSpan.FromDays(30));
        var inny = TokenOdswiezania.Wydaj(new TokenOdswiezaniaId(Guid.NewGuid()), konto.UzytkownikId, Guid.NewGuid(), Skrot(3), Skrot(9), teraz, TimeSpan.FromDays(30));
        pierwszy.Zastap(drugi.Id, teraz);

        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new TokenOdswiezaniaRepozytoriumEf(kontekst);
            await new JednostkaPracyEf(kontekst).WykonajAsync(_ =>
            {
                repo.Dodaj(pierwszy);
                repo.Dodaj(drugi);
                repo.Dodaj(inny);
                return Task.CompletedTask;
            }, Ct);
        }

        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new TokenOdswiezaniaRepozytoriumEf(kontekst);
            var wczytany = (await repo.PobierzPoSkrocieAsync(Skrot(1), Ct))!;
            wczytany.Id.ShouldBe(pierwszy.Id);
            wczytany.ZastapionyPrzez.ShouldBe(drugi.Id);
            wczytany.ZastapionyAt.ShouldBe(teraz);
            wczytany.RodzinaId.ShouldBe(rodzina);
            wczytany.UzyciaWOknieTolerancji.ShouldBe(0);
            wczytany.SkrotKlientaHttp.ShouldBe(Skrot(9));
            (await repo.PobierzPoSkrocieAsync(Skrot(77), Ct)).ShouldBeNull();

            (await repo.UniewaznRodzineAsync(rodzina, teraz.AddMinutes(1), Ct)).ShouldBe(2);
            (await repo.UniewaznRodzineAsync(rodzina, teraz.AddMinutes(2), Ct)).ShouldBe(0);
            (await repo.UniewaznWszystkieAsync(konto.UzytkownikId, teraz.AddMinutes(3), Ct)).ShouldBe(1);
        }

        (await _baza.SkalarAsync<int>($"SELECT count(*)::int FROM token_odswiezania WHERE uniewazniony_at IS NOT NULL")).ShouldBe(3);
    }

    [Fact]
    public async Task UsuniecieNastepcyTokenuUstawiaNullWPoluZastapionyPrzez()
    {
        var konto = await ZarejestrujAsync(RolaUzytkownika.Uczen, "token2@example.com");
        var teraz = _baza.Zegar.Teraz;
        var rodzina = Guid.NewGuid();
        var pierwszy = TokenOdswiezania.Wydaj(new TokenOdswiezaniaId(Guid.NewGuid()), konto.UzytkownikId, rodzina, Skrot(4), Skrot(9), teraz, TimeSpan.FromDays(1));
        var drugi = TokenOdswiezania.Wydaj(new TokenOdswiezaniaId(Guid.NewGuid()), konto.UzytkownikId, rodzina, Skrot(5), Skrot(9), teraz, TimeSpan.FromDays(1));
        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new TokenOdswiezaniaRepozytoriumEf(kontekst);
            await new JednostkaPracyEf(kontekst).WykonajAsync(_ =>
            {
                repo.Dodaj(drugi);
                repo.Dodaj(pierwszy);
                pierwszy.Zastap(drugi.Id, teraz);
                return Task.CompletedTask;
            }, Ct);
        }

        // Niezmiennik DDL zastapiony_at/zastapiony_przez: usunięcie następcy zeruje wskazanie (ON DELETE SET NULL) — CHECK to wykrywa.
        var blad = await Should.ThrowAsync<PostgresException>(() => _baza.WykonajAsync($"DELETE FROM token_odswiezania WHERE id = '{drugi.Id.Wartosc}'"));
        blad.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
    }

    // ---------- próby logowania ----------

    [Fact]
    public async Task ProbyLogowaniaSaZapisywaneIZliczaneWOknieCzasu()
    {
        var teraz = _baza.Zegar.Teraz;
        await using (var kontekst = _baza.Kontekst())
        {
            var repo = new ProbaLogowaniaRepozytoriumEf(kontekst);
            await new JednostkaPracyEf(kontekst).WykonajAsync(_ =>
            {
                repo.Dodaj(ProbaLogowania.Zapisz("Jan@Example.com", IPAddress.Parse("203.0.113.7"), false, teraz.AddMinutes(-20)));
                repo.Dodaj(ProbaLogowania.Zapisz("jan@example.com", IPAddress.Parse("2001:db8::1"), false, teraz.AddMinutes(-5)));
                repo.Dodaj(ProbaLogowania.Zapisz("jan@example.com", IPAddress.Parse("203.0.113.7"), true, teraz.AddMinutes(-4)));
                repo.Dodaj(ProbaLogowania.Zapisz("inny@example.com", IPAddress.Parse("203.0.113.7"), false, teraz.AddMinutes(-3)));
                return Task.CompletedTask;
            }, Ct);
        }

        await using var odczyt = _baza.Kontekst();
        var repoOdczyt = new ProbaLogowaniaRepozytoriumEf(odczyt);
        (await repoOdczyt.PoliczNieudaneAsync("JAN@example.com", teraz.AddMinutes(-15), Ct)).ShouldBe(1);
        (await repoOdczyt.PoliczNieudaneAsync("jan@example.com", teraz.AddHours(-1), Ct)).ShouldBe(2);
        (await _baza.SkalarAsync<string>("SELECT host(ip) FROM proba_logowania ORDER BY id LIMIT 1")).ShouldBe("203.0.113.7");
        (await _baza.SkalarAsync<long>("SELECT min(id) FROM proba_logowania")).ShouldBeGreaterThan(0);
    }

    // ---------- rejestracja enumów i DI ----------

    [Fact]
    public void EnumyKontSaZarejestrowaneDlaTypowPostgres()
    {
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(RolaUzytkownika), "rola_uzytkownika"));
        MapowanieTypowWyliczeniowych.Lista.ShouldContain((typeof(StatusKonta), "status_konta"));
    }

    [Fact]
    public async Task ModulKontaRejestrujeSieWDiZWalidacjaZakresowIBudowy()
    {
        var opcje = new LematOpcje { Baza = new BazaOpcje { Polaczenie = _baza.Polaczenie } };
        var uslugi = new ServiceCollection().DodajPersystencje(opcje, RodzajProcesu.Api).DodajModulKonta();
        await using var dostawca = uslugi.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        await using var zakres = dostawca.CreateAsyncScope();
        var fasada = zakres.ServiceProvider.GetRequiredService<IKontaFasada>();
        var konto = await fasada.ZarejestrujAsync(
            new RejestracjaKontaWejscie(RolaUzytkownika.Uczen, "di@example.com", "hash", "Di", "Test", true), Ct);

        (await fasada.PobierzKontoAsync(konto.UzytkownikId, Ct))!.Rola.ShouldBe(RolaUzytkownika.Uczen);
    }
}
