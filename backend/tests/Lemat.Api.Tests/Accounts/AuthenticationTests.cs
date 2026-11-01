// @Role: Testy integracyjne /api/v1/auth/* (kontrakt POC 3): rejestracja, logowanie, rotacja i unieważnianie rodziny refresh tokenów, wylogowanie, polityki 401/403, brak sekretów w logach | @DependsOn: ApiFactory, KlientAuth, BazaTestowaFixture, WystawcaTokenow, Npgsql | @UsedBy: dotnet test | @Task: TASK-123
using System.Net;
using System.Text.Json;
using Lemat.Api.Tests.Support;
using Lemat.Application.Accounts;
using Lemat.Domain.Accounts;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lemat.Api.Tests.Accounts;

public sealed class AuthenticationTests(ApiFactory fabryka, BazaTestowaFixture baza) : IClassFixture<ApiFactory>
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<long> LiczbaAsync(string tabela, string warunek, params (string Nazwa, object Wartosc)[] parametry)
    {
        await using var zrodlo = NpgsqlDataSource.Create(baza.Polaczenie);
        await using var polecenie = zrodlo.CreateCommand($"SELECT count(*) FROM {tabela} WHERE {warunek}");
        foreach (var (nazwa, wartosc) in parametry)
        {
            polecenie.Parameters.AddWithValue(nazwa, wartosc);
        }

        return (long)(await polecenie.ExecuteScalarAsync(Token))!;
    }

    private async Task WykonajAsync(string sql, params (string Nazwa, object Wartosc)[] parametry)
    {
        await using var zrodlo = NpgsqlDataSource.Create(baza.Polaczenie);
        await using var polecenie = zrodlo.CreateCommand(sql);
        foreach (var (nazwa, wartosc) in parametry)
        {
            polecenie.Parameters.AddWithValue(nazwa, wartosc);
        }

        await polecenie.ExecuteNonQueryAsync(Token);
    }

    [Fact]
    public async Task RejestracjaZwraca201CiasteczkoISesjeZgodnaZKontraktem()
    {
        using var klient = new KlientAuth(fabryka);
        var email = KlientAuth.NowyEmail();

        var odpowiedz = await klient.ZarejestrujAsync(KlientAuth.Rejestracja(email));

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Created);
        var tresc = odpowiedz.Tresc;
        tresc.EnumerateObject().Select(p => p.Name).ShouldBe(["status_konta", "sesja"]);
        tresc.GetProperty("status_konta").GetString().ShouldBe("AKTYWNE");
        var sesja = tresc.GetProperty("sesja");
        sesja.EnumerateObject().Select(p => p.Name).ShouldBe(["access_token", "wygasa_at", "uzytkownik"]);
        sesja.GetProperty("access_token").GetString()!.Split('.').Length.ShouldBe(3);
        sesja.GetProperty("wygasa_at").GetDateTimeOffset().ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(14));
        var uzytkownik = sesja.GetProperty("uzytkownik");
        uzytkownik.EnumerateObject().Select(p => p.Name).ShouldBe(["id", "rola", "imie", "flagi"]);
        uzytkownik.GetProperty("rola").GetString().ShouldBe("UCZEN");
        uzytkownik.GetProperty("imie").GetString().ShouldBe("Ola");
        uzytkownik.GetProperty("flagi").GetProperty("weryfikator_globalny").GetBoolean().ShouldBeFalse();
        uzytkownik.GetProperty("flagi").GetProperty("recenzent_tresci").GetBoolean().ShouldBeFalse();

        var naglowek = odpowiedz.NaglowekCiasteczka.ShouldNotBeNull().ToLowerInvariant();
        naglowek.ShouldContain("httponly");
        naglowek.ShouldContain("samesite=strict");
        naglowek.ShouldContain("path=/api/v1/auth");
        naglowek.ShouldContain("max-age=1209600");
        naglowek.ShouldNotContain("secure");
        odpowiedz.Ciasteczko.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RejestracjaTworzyUzytkownikaUczniaProfilISciezkeWJednejTransakcji()
    {
        using var klient = new KlientAuth(fabryka);
        var email = KlientAuth.NowyEmail();

        var odpowiedz = await klient.ZarejestrujAsync(KlientAuth.Rejestracja(email));

        var id = odpowiedz.Tresc.GetProperty("sesja").GetProperty("uzytkownik").GetProperty("id").GetGuid();
        (await LiczbaAsync("uzytkownik", "id = @id AND status = 'AKTYWNE'::status_konta AND rola = 'UCZEN'::rola_uzytkownika", ("id", id))).ShouldBe(1);
        (await LiczbaAsync("uczen", "uzytkownik_id = @id", ("id", id))).ShouldBe(1);
        (await LiczbaAsync("profil_ucznia", "uczen_id = @id", ("id", id))).ShouldBe(1);
        (await LiczbaAsync("sciezka_nauki", "uczen_id = @id", ("id", id))).ShouldBe(1);
        (await LiczbaAsync("token_odswiezania", "uzytkownik_id = @id", ("id", id))).ShouldBe(1);
    }

    [Fact]
    public async Task ZajetyEmailWTymZInnejWielkosciLiterDaje409()
    {
        using var klient = new KlientAuth(fabryka);
        var email = KlientAuth.NowyEmail();
        (await klient.ZarejestrujAsync(KlientAuth.Rejestracja(email))).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Created);

        var powtorzona = await klient.ZarejestrujAsync(KlientAuth.Rejestracja(email.ToUpperInvariant()));

        powtorzona.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        powtorzona.Tresc.GetProperty("kod").GetString().ShouldBe("EMAIL_ZAJETY");
        powtorzona.Tresc.GetProperty("detail").GetString().ShouldBe("Nie udało się założyć konta na ten adres — zaloguj się albo zresetuj hasło.");
        powtorzona.NaglowekCiasteczka.ShouldBeNull();
    }

    [Fact]
    public async Task RownoczesnaRejestracjaTegoSamegoEmailaKonczySieJednymSukcesem()
    {
        using var klient = new KlientAuth(fabryka);
        var tresc = KlientAuth.Rejestracja();

        var wyniki = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => klient.ZarejestrujAsync(tresc)));

        wyniki.Count(w => w.Odpowiedz.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        wyniki.Count(w => w.Odpowiedz.StatusCode == HttpStatusCode.Conflict).ShouldBe(3);
    }

    [Fact]
    public async Task ZaKrotkieHasloDaje422ZBledamiPol()
    {
        using var klient = new KlientAuth(fabryka);

        var odpowiedz = await klient.ZarejestrujAsync(KlientAuth.Rejestracja(haslo: "krotkie"));

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("WALIDACJA");
        var blad = odpowiedz.Tresc.GetProperty("bledy_pol").EnumerateArray().Single();
        blad.GetProperty("pole").GetString().ShouldBe("haslo");
        blad.GetProperty("kod_reguly").GetString().ShouldBe("ZA_KROTKIE");
        blad.GetProperty("komunikat").GetString().ShouldBe("Hasło musi mieć co najmniej 10 znaków.");
    }

    [Fact]
    public async Task BrakOswiadczeniaWiekuIRoleNauczycielaDajeKodyRegul()
    {
        using var klient = new KlientAuth(fabryka);

        var bezOswiadczenia = await klient.ZarejestrujAsync(KlientAuth.Rejestracja(oswiadczenie: false));
        var nauczyciel = await klient.ZarejestrujAsync(KlientAuth.Rejestracja(rola: "NAUCZYCIEL"));

        bezOswiadczenia.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var blad = bezOswiadczenia.Tresc.GetProperty("bledy_pol").EnumerateArray().Single();
        blad.GetProperty("pole").GetString().ShouldBe("oswiadczenie_wieku_16");
        blad.GetProperty("kod_reguly").GetString().ShouldBe("WYMAGANE_OSWIADCZENIE");

        nauczyciel.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var bladRoli = nauczyciel.Tresc.GetProperty("bledy_pol").EnumerateArray().Single();
        bladRoli.GetProperty("pole").GetString().ShouldBe("rola");
        bladRoli.GetProperty("kod_reguly").GetString().ShouldBe("ROLA_NIEOBSLUGIWANA");
    }

    [Fact]
    public async Task NiepoprawneDaneRejestracjiDajaKodyRegulDlaKazdegoPola()
    {
        using var klient = new KlientAuth(fabryka);
        var tresc = new
        {
            email = "to-nie-email",
            haslo = new string('x', 129),
            imie = "  ",
            nazwisko = new string('n', 81),
            rola = "UCZEN",
            oswiadczenie_wieku_16 = true,
            zgoda_regulamin = false,
        };

        var odpowiedz = await klient.ZarejestrujAsync(tresc);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var bledy = odpowiedz.Tresc.GetProperty("bledy_pol").EnumerateArray()
            .ToDictionary(b => b.GetProperty("pole").GetString()!, b => b.GetProperty("kod_reguly").GetString()!);
        bledy.ShouldBe(new Dictionary<string, string>
        {
            ["email"] = "FORMAT_EMAIL",
            ["haslo"] = "ZA_DLUGIE",
            ["imie"] = "WYMAGANE",
            ["nazwisko"] = "ZA_DLUGIE",
            ["zgoda_regulamin"] = "WYMAGANE_OSWIADCZENIE",
        });
    }

    [Fact]
    public async Task LogowanieZwraca200SesjeICiasteczko()
    {
        using var klient = new KlientAuth(fabryka);
        var email = KlientAuth.NowyEmail();
        await klient.ZarejestrujAsync(KlientAuth.Rejestracja(email));

        var odpowiedz = await klient.ZalogujAsync(email.ToUpperInvariant(), KlientAuth.Haslo);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        odpowiedz.Tresc.EnumerateObject().Select(p => p.Name).ShouldBe(["access_token", "wygasa_at", "uzytkownik"]);
        odpowiedz.Tresc.GetProperty("uzytkownik").GetProperty("rola").GetString().ShouldBe("UCZEN");
        odpowiedz.Ciasteczko.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ZlyEmailIZleHasloDajaIdentycznaOdpowiedz401()
    {
        using var klient = new KlientAuth(fabryka);
        var email = KlientAuth.NowyEmail();
        await klient.ZarejestrujAsync(KlientAuth.Rejestracja(email));

        var zlyEmail = await klient.ZalogujAsync(KlientAuth.NowyEmail(), KlientAuth.Haslo);
        var zleHaslo = await klient.ZalogujAsync(email, "Zupelnie-Inne#1");

        zlyEmail.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        zleHaslo.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        foreach (var pole in new[] { "type", "title", "status", "detail", "kod" })
        {
            zlyEmail.Tresc.GetProperty(pole).ToString().ShouldBe(zleHaslo.Tresc.GetProperty(pole).ToString());
        }

        zlyEmail.Tresc.GetProperty("kod").GetString().ShouldBe("NIEUWIERZYTELNIONY");
        zlyEmail.Tresc.GetProperty("detail").GetString().ShouldBe("Nieprawidłowy e-mail lub hasło.");
        zlyEmail.NaglowekCiasteczka.ShouldBeNull();
        zleHaslo.NaglowekCiasteczka.ShouldBeNull();
    }

    [Fact]
    public async Task LogowanieBezHaslaDaje422()
    {
        using var klient = new KlientAuth(fabryka);

        var odpowiedz = await klient.ZalogujAsync("ktos@example.com", string.Empty);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        odpowiedz.Tresc.GetProperty("bledy_pol").EnumerateArray().Single().GetProperty("kod_reguly").GetString().ShouldBe("WYMAGANE");
    }

    [Fact]
    public async Task LogowanieZablokowanegoKontaDaje403KontoNieaktywne()
    {
        using var klient = new KlientAuth(fabryka);
        var email = KlientAuth.NowyEmail();
        await klient.ZarejestrujAsync(KlientAuth.Rejestracja(email));
        await WykonajAsync("UPDATE uzytkownik SET status = 'ZABLOKOWANE'::status_konta WHERE email = @e", ("e", email));

        var poprawneHaslo = await klient.ZalogujAsync(email, KlientAuth.Haslo);
        var zleHaslo = await klient.ZalogujAsync(email, "Zupelnie-Inne#1");

        poprawneHaslo.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        poprawneHaslo.Tresc.GetProperty("kod").GetString().ShouldBe("KONTO_NIEAKTYWNE");
        zleHaslo.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OdswiezenieRotujeCiasteczkoIWydajeNowaSesje()
    {
        using var klient = new KlientAuth(fabryka);
        var rejestracja = await klient.ZarejestrujAsync();
        var stare = rejestracja.Ciasteczko!;

        var odswiezenie = await klient.OdswiezAsync(stare);

        odswiezenie.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        odswiezenie.Tresc.GetProperty("access_token").GetString().ShouldNotBeNullOrWhiteSpace();
        odswiezenie.Tresc.GetProperty("uzytkownik").GetProperty("id").GetGuid()
            .ShouldBe(rejestracja.Tresc.GetProperty("sesja").GetProperty("uzytkownik").GetProperty("id").GetGuid());
        var nowe = odswiezenie.Ciasteczko!;
        nowe.ShouldNotBeNullOrWhiteSpace();
        nowe.ShouldNotBe(stare);

        (await klient.OdswiezAsync(nowe)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UzycieStaregoCiasteczkaUniewaznieniaRodzineTakzeNoweCiasteczko()
    {
        using var klient = new KlientAuth(fabryka);
        var stare = (await klient.ZarejestrujAsync()).Ciasteczko!;
        var nowe = (await klient.OdswiezAsync(stare)).Ciasteczko!;

        var ponowneUzycie = await klient.OdswiezAsync(stare);

        ponowneUzycie.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ponowneUzycie.Tresc.GetProperty("kod").GetString().ShouldBe("SESJA_UNIEWAZNIONA");
        ponowneUzycie.NaglowekCiasteczka.ShouldNotBeNull().ToLowerInvariant().ShouldContain("max-age=0");
        ponowneUzycie.Ciasteczko.ShouldBe(string.Empty);

        var nowePoUniewaznieniu = await klient.OdswiezAsync(nowe);
        nowePoUniewaznieniu.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        nowePoUniewaznieniu.Tresc.GetProperty("kod").GetString().ShouldBe("SESJA_UNIEWAZNIONA");
    }

    [Fact]
    public async Task RownoleglaRotacjaTymSamymCiasteczkiemDajeJedenSukces()
    {
        using var klient = new KlientAuth(fabryka);
        var stare = (await klient.ZarejestrujAsync()).Ciasteczko!;

        var wyniki = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => klient.OdswiezAsync(stare)));

        wyniki.Count(w => w.Odpowiedz.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        wyniki.Count(w => w.Odpowiedz.StatusCode == HttpStatusCode.Unauthorized).ShouldBe(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nieznany-token")]
    public async Task BrakNieznaneIPustoCiasteczkoDaje401SesjaUniewazniona(string? ciasteczko)
    {
        using var klient = new KlientAuth(fabryka);

        var odpowiedz = await klient.OdswiezAsync(ciasteczko);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("SESJA_UNIEWAZNIONA");
    }

    [Fact]
    public async Task WygaslyRefreshTokenDaje401()
    {
        using var klient = new KlientAuth(fabryka);
        var rejestracja = await klient.ZarejestrujAsync();
        var id = rejestracja.Tresc.GetProperty("sesja").GetProperty("uzytkownik").GetProperty("id").GetGuid();
        await WykonajAsync("UPDATE token_odswiezania SET wygasa_at = now() - interval '1 minute' WHERE uzytkownik_id = @id", ("id", id));

        var odpowiedz = await klient.OdswiezAsync(rejestracja.Ciasteczko);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("SESJA_UNIEWAZNIONA");
    }

    [Fact]
    public async Task OdswiezenieZablokowanegoKontaDaje401()
    {
        using var klient = new KlientAuth(fabryka);
        var email = KlientAuth.NowyEmail();
        var rejestracja = await klient.ZarejestrujAsync(KlientAuth.Rejestracja(email));
        await WykonajAsync("UPDATE uzytkownik SET status = 'ZABLOKOWANE'::status_konta WHERE email = @e", ("e", email));

        var odpowiedz = await klient.OdswiezAsync(rejestracja.Ciasteczko);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("SESJA_UNIEWAZNIONA");
    }

    [Fact]
    public async Task WylogowanieDaje204CzysciCiasteczkoIUniewaznieniaSesje()
    {
        using var klient = new KlientAuth(fabryka);
        var rejestracja = await klient.ZarejestrujAsync();
        var token = rejestracja.Tresc.GetProperty("sesja").GetProperty("access_token").GetString();

        var wylogowanie = await klient.WylogujAsync(token, rejestracja.Ciasteczko);

        wylogowanie.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var naglowek = wylogowanie.NaglowekCiasteczka.ShouldNotBeNull().ToLowerInvariant();
        naglowek.ShouldContain("max-age=0");
        naglowek.ShouldContain("path=/api/v1/auth");
        (await klient.OdswiezAsync(rejestracja.Ciasteczko)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WylogowanieBezCiasteczkaUniewaznieniaWszystkieSesjeKonta()
    {
        using var klient = new KlientAuth(fabryka);
        var email = KlientAuth.NowyEmail();
        var rejestracja = await klient.ZarejestrujAsync(KlientAuth.Rejestracja(email));
        var drugie = (await klient.ZalogujAsync(email, KlientAuth.Haslo)).Ciasteczko;
        var token = rejestracja.Tresc.GetProperty("sesja").GetProperty("access_token").GetString();

        (await klient.WylogujAsync(token)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await klient.OdswiezAsync(rejestracja.Ciasteczko)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await klient.OdswiezAsync(drugie)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EndpointChronionyBezTokenuIZTokenemZlymAlboWygaslymDaje401()
    {
        using var klient = new KlientAuth(fabryka);
        var wystawca = fabryka.Services.GetRequiredService<IWystawcaTokenow>();
        var wygasly = wystawca.Wystaw(new UzytkownikId(Guid.NewGuid()), RolaUzytkownika.Uczen, StatusKonta.Aktywne, DateTimeOffset.UtcNow.AddHours(-1)).Token;

        var bezTokenu = await klient.WylogujAsync(null);
        var zlyToken = await klient.WylogujAsync("to.nie.jest.token");
        var wygaslyToken = await klient.WylogujAsync(wygasly);

        foreach (var odpowiedz in new[] { bezTokenu, zlyToken, wygaslyToken })
        {
            odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            odpowiedz.Odpowiedz.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
            odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("NIEUWIERZYTELNIONY");
        }
    }

    [Fact]
    public async Task TokenInnejRoliAlboNieaktywnegoKontaDaje403()
    {
        using var klient = new KlientAuth(fabryka);
        var wystawca = fabryka.Services.GetRequiredService<IWystawcaTokenow>();
        var teraz = DateTimeOffset.UtcNow;
        var nauczyciel = wystawca.Wystaw(new UzytkownikId(Guid.NewGuid()), RolaUzytkownika.Nauczyciel, StatusKonta.Aktywne, teraz).Token;
        var zablokowany = wystawca.Wystaw(new UzytkownikId(Guid.NewGuid()), RolaUzytkownika.Uczen, StatusKonta.Zablokowane, teraz).Token;

        var poRoli = await klient.WylogujAsync(nauczyciel);
        var poStatusie = await klient.WylogujAsync(zablokowany);

        poRoli.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        poRoli.Tresc.GetProperty("kod").GetString().ShouldBe("BRAK_UPRAWNIEN");
        poStatusie.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        poStatusie.Tresc.GetProperty("kod").GetString().ShouldBe("KONTO_NIEAKTYWNE");
    }

    [Fact]
    public async Task TokenPodpisanyInnymKluczemJestOdrzucony()
    {
        using var klient = new KlientAuth(fabryka);
        using var obcyKlucz = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        var opcje = fabryka.Services.GetRequiredService<Lemat.Application.Common.LematOpcje>();
        var obcy = new Lemat.Infrastructure.Security.WystawcaTokenow(
            Lemat.Infrastructure.Security.KluczJwt.Zaladuj(new Lemat.Application.Common.JwtOpcje { Klucz = obcyKlucz.ExportECPrivateKeyPem() }, "Development"),
            opcje);
        var token = obcy.Wystaw(new UzytkownikId(Guid.NewGuid()), RolaUzytkownika.Uczen, StatusKonta.Aktywne, DateTimeOffset.UtcNow).Token;

        var odpowiedz = await klient.WylogujAsync(token);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HaslaITokenyNieTrafiajaDoLogow()
    {
        using var klient = new KlientAuth(fabryka);
        var email = KlientAuth.NowyEmail();
        var haslo = $"Sekret-{Guid.NewGuid():N}";
        var rejestracja = await klient.ZarejestrujAsync(KlientAuth.Rejestracja(email, haslo));
        var logowanie = await klient.ZalogujAsync(email, haslo);
        var odswiezenie = await klient.OdswiezAsync(logowanie.Ciasteczko);
        await klient.ZalogujAsync(email, "Zle-Haslo-Zle-Haslo");
        await klient.OdswiezAsync(logowanie.Ciasteczko);
        await klient.WylogujAsync(odswiezenie.Tresc.GetProperty("access_token").GetString(), odswiezenie.Ciasteczko);

        var logi = fabryka.Logi.Calosc;

        logi.ShouldNotBeEmpty();
        logi.ShouldNotContain(haslo);
        logi.ShouldNotContain("Zle-Haslo-Zle-Haslo");
        foreach (var sekret in new[]
        {
            rejestracja.Ciasteczko, logowanie.Ciasteczko, odswiezenie.Ciasteczko,
            rejestracja.Tresc.GetProperty("sesja").GetProperty("access_token").GetString(),
            logowanie.Tresc.GetProperty("access_token").GetString(),
            odswiezenie.Tresc.GetProperty("access_token").GetString(),
        })
        {
            logi.ShouldNotContain(sekret!);
        }
    }

    [Fact]
    public async Task NieznanePoleWRejestracjiDaje422PoleNieznane()
    {
        using var klient = new KlientAuth(fabryka);
        var tresc = JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(KlientAuth.Rejestracja()))!;
        tresc["admin"] = true;

        var odpowiedz = await klient.ZarejestrujAsync(tresc);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        odpowiedz.Tresc.GetProperty("bledy_pol")[0].GetProperty("kod_reguly").GetString().ShouldBe("POLE_NIEZNANE");
    }
}
