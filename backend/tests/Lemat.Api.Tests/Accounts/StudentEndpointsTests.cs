// @Role: Testy integracyjne GET/PATCH /api/v1/uczniowie/ja (kontrakt POC 4.1): odczyt, Merge Patch, If-Match, null/NIE_MOZNA_WYCZYSCIC, DZIAL_NIEZNANY, Content-Type, wyścig dwóch PATCH | @DependsOn: ApiFactory, KlientZalogowany, DaneReferencyjne, BazaTestowaFixture | @UsedBy: dotnet test | @Task: TASK-124
using System.Net;
using System.Text.Json;
using Lemat.Api.Tests.Support;

namespace Lemat.Api.Tests.Accounts;

public sealed class UczenEndpointsTests(ApiFactory fabryka, BazaTestowaFixture baza) : IClassFixture<ApiFactory>
{
    private const string Sciezka = "/api/v1/uczniowie/ja";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<KlientZalogowany> NowyUczenAsync()
    {
        await DaneReferencyjne.ZapewnijDzialyAsync(baza.Polaczenie, Token);
        return await KlientZalogowany.ZarejestrujAsync(fabryka);
    }

    private static string KodBledu(OdpowiedzTestowa odpowiedz) => odpowiedz.Tresc.GetProperty("kod").GetString()!;

    [Fact]
    public async Task GetZwracaUczniaZgodnegoZPrzykladem()
    {
        using var klient = await NowyUczenAsync();

        var odpowiedz = await klient.PobierzAsync(Sciezka);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tresc = odpowiedz.Tresc;
        tresc.EnumerateObject().Select(p => p.Name).ShouldBe(
            ["id", "email", "imie", "nazwisko", "poziom_matury", "termin_egzaminu", "wybrany_dzial_id", "zgoda_na_trening", "test_diagnostyczny", "klasy", "version"]);
        tresc.GetProperty("id").GetGuid().ShouldBe(klient.Id);
        tresc.GetProperty("email").GetString().ShouldBe(klient.Email);
        tresc.GetProperty("imie").GetString().ShouldBe("Ola");
        tresc.GetProperty("nazwisko").GetString().ShouldBe("Wiśniewska");
        tresc.GetProperty("poziom_matury").ValueKind.ShouldBe(JsonValueKind.Null);
        tresc.GetProperty("termin_egzaminu").ValueKind.ShouldBe(JsonValueKind.Null);
        tresc.GetProperty("wybrany_dzial_id").ValueKind.ShouldBe(JsonValueKind.Null);
        tresc.GetProperty("zgoda_na_trening").GetBoolean().ShouldBeFalse();
        tresc.GetProperty("test_diagnostyczny").GetString().ShouldBe("BRAK");
        tresc.GetProperty("klasy").GetArrayLength().ShouldBe(0);
        tresc.GetProperty("version").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task BezTokenuZwracaja401()
    {
        using var klient = KlientZalogowany.Anonimowy(fabryka);

        var odczyt = await klient.PobierzAsync(Sciezka);
        var zmiana = await klient.ZmienAsync("{}", "\"1\"");

        odczyt.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        KodBledu(odczyt).ShouldBe("NIEUWIERZYTELNIONY");
        zmiana.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchUstawiaPolaIZwiekszaVersionOJeden()
    {
        using var klient = await NowyUczenAsync();

        var odpowiedz = await klient.ZmienAsync("""{ "poziom_matury": "PODSTAWOWY", "termin_egzaminu": "2027-05-05", "wybrany_dzial_id": 5 }""", "\"1\"");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        odpowiedz.Tresc.GetProperty("poziom_matury").GetString().ShouldBe("PODSTAWOWY");
        odpowiedz.Tresc.GetProperty("termin_egzaminu").GetString().ShouldBe("2027-05-05");
        odpowiedz.Tresc.GetProperty("wybrany_dzial_id").GetInt32().ShouldBe(5);
        odpowiedz.Tresc.GetProperty("zgoda_na_trening").GetBoolean().ShouldBeFalse();
        odpowiedz.Tresc.GetProperty("version").GetInt32().ShouldBe(2);

        var odczyt = await klient.PobierzAsync(Sciezka);
        odczyt.Tresc.GetProperty("version").GetInt32().ShouldBe(2);
        odczyt.Tresc.GetProperty("poziom_matury").GetString().ShouldBe("PODSTAWOWY");
    }

    [Fact]
    public async Task PatchAkceptujeTakzeApplicationJson()
    {
        using var klient = await NowyUczenAsync();

        var odpowiedz = await klient.ZmienAsync("""{ "zgoda_na_trening": true }""", "\"1\"", "application/json");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        odpowiedz.Tresc.GetProperty("zgoda_na_trening").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task PolaNieobecneNieZmieniajaWartosci()
    {
        using var klient = await NowyUczenAsync();
        (await klient.ZmienAsync("""{ "poziom_matury": "ROZSZERZONY", "termin_egzaminu": "2027-05-05", "wybrany_dzial_id": 3 }""", "\"1\"")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);

        var odpowiedz = await klient.ZmienAsync("""{ "zgoda_na_trening": true }""", "\"2\"");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        odpowiedz.Tresc.GetProperty("poziom_matury").GetString().ShouldBe("ROZSZERZONY");
        odpowiedz.Tresc.GetProperty("termin_egzaminu").GetString().ShouldBe("2027-05-05");
        odpowiedz.Tresc.GetProperty("wybrany_dzial_id").GetInt32().ShouldBe(3);
        odpowiedz.Tresc.GetProperty("zgoda_na_trening").GetBoolean().ShouldBeTrue();
        odpowiedz.Tresc.GetProperty("version").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task NullCzyscitTerminEgzaminuIWybranyDzial()
    {
        using var klient = await NowyUczenAsync();
        (await klient.ZmienAsync("""{ "poziom_matury": "PODSTAWOWY", "termin_egzaminu": "2027-05-05", "wybrany_dzial_id": 5 }""", "\"1\"")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);

        var odpowiedz = await klient.ZmienAsync("""{ "termin_egzaminu": null, "wybrany_dzial_id": null }""", "\"2\"");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        odpowiedz.Tresc.GetProperty("termin_egzaminu").ValueKind.ShouldBe(JsonValueKind.Null);
        odpowiedz.Tresc.GetProperty("wybrany_dzial_id").ValueKind.ShouldBe(JsonValueKind.Null);
        odpowiedz.Tresc.GetProperty("poziom_matury").GetString().ShouldBe("PODSTAWOWY");
        odpowiedz.Tresc.GetProperty("version").GetInt32().ShouldBe(3);
    }

    [Theory]
    [InlineData("poziom_matury")]
    [InlineData("zgoda_na_trening")]
    public async Task NullWPolachBezWyczyszczeniaZwraca422(string pole)
    {
        using var klient = await NowyUczenAsync();

        var odpowiedz = await klient.ZmienAsync($$"""{ "{{pole}}": null }""", "\"1\"");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        KodBledu(odpowiedz).ShouldBe("WALIDACJA");
        var blad = odpowiedz.Tresc.GetProperty("bledy_pol")[0];
        blad.GetProperty("pole").GetString().ShouldBe(pole);
        blad.GetProperty("kod_reguly").GetString().ShouldBe("NIE_MOZNA_WYCZYSCIC");
    }

    [Fact]
    public async Task NieznanyDzialZwraca422DzialNieznany()
    {
        using var klient = await NowyUczenAsync();

        var odpowiedz = await klient.ZmienAsync("""{ "wybrany_dzial_id": 999 }""", "\"1\"");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        KodBledu(odpowiedz).ShouldBe("WALIDACJA");
        var blad = odpowiedz.Tresc.GetProperty("bledy_pol")[0];
        blad.GetProperty("pole").GetString().ShouldBe("wybrany_dzial_id");
        blad.GetProperty("kod_reguly").GetString().ShouldBe("DZIAL_NIEZNANY");

        (await klient.PobierzAsync(Sciezka)).Tresc.GetProperty("version").GetInt32().ShouldBe(1);
    }

    [Theory]
    [InlineData("""{ "nieznane": 1 }""", "POLE_NIEZNANE")]
    [InlineData("""{ "poziom_matury": "INNY" }""", "TYP_NIEPRAWIDLOWY")]
    [InlineData("""{ "termin_egzaminu": "jutro" }""", "TYP_NIEPRAWIDLOWY")]
    [InlineData("""{ "wybrany_dzial_id": "pięć" }""", "TYP_NIEPRAWIDLOWY")]
    public async Task BledneCialoZwraca422(string json, string regula)
    {
        using var klient = await NowyUczenAsync();

        var odpowiedz = await klient.ZmienAsync(json, "\"1\"");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        odpowiedz.Tresc.GetProperty("bledy_pol")[0].GetProperty("kod_reguly").GetString().ShouldBe(regula);
    }

    [Fact]
    public async Task BrakIfMatchZwraca428()
    {
        using var klient = await NowyUczenAsync();

        var odpowiedz = await klient.ZmienAsync("""{ "zgoda_na_trening": true }""", null);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe((HttpStatusCode)428);
        KodBledu(odpowiedz).ShouldBe("WYMAGANY_IF_MATCH");
    }

    [Fact]
    public async Task ZlaFormaIfMatchZwraca400()
    {
        using var klient = await NowyUczenAsync();

        var odpowiedz = await klient.ZmienAsync("""{ "zgoda_na_trening": true }""", "1");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        KodBledu(odpowiedz).ShouldBe("NAGLOWEK_NIEPRAWIDLOWY");
    }

    [Fact]
    public async Task NiezgodnyIfMatchZwraca412ZAktualnymZasobem()
    {
        using var klient = await NowyUczenAsync();
        (await klient.ZmienAsync("""{ "zgoda_na_trening": true }""", "\"1\"")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);

        var odpowiedz = await klient.ZmienAsync("""{ "zgoda_na_trening": false }""", "\"1\"");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        KodBledu(odpowiedz).ShouldBe("KONFLIKT_WERSJI");
        var aktualny = odpowiedz.Tresc.GetProperty("szczegoly").GetProperty("aktualny");
        aktualny.GetProperty("version").GetInt32().ShouldBe(2);
        aktualny.GetProperty("zgoda_na_trening").GetBoolean().ShouldBeTrue();
        aktualny.GetProperty("id").GetGuid().ShouldBe(klient.Id);
    }

    [Fact]
    public async Task InnyContentTypeZwraca415()
    {
        using var klient = await NowyUczenAsync();

        var odpowiedz = await klient.ZmienAsync("""{ "zgoda_na_trening": true }""", "\"1\"", "text/plain");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        KodBledu(odpowiedz).ShouldBe("NIEOBSLUGIWANY_FORMAT");
    }

    [Fact]
    public async Task DwaRownoleglePatchZTymSamymIfMatchDajaJeden200IJeden412()
    {
        using var klient = await NowyUczenAsync();

        var wyniki = await Task.WhenAll(
            klient.ZmienAsync("""{ "poziom_matury": "PODSTAWOWY" }""", "\"1\""),
            klient.ZmienAsync("""{ "poziom_matury": "ROZSZERZONY" }""", "\"1\""));

        wyniki.Count(w => w.Odpowiedz.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        var przegrany = wyniki.Single(w => w.Odpowiedz.StatusCode == HttpStatusCode.PreconditionFailed);
        KodBledu(przegrany).ShouldBe("KONFLIKT_WERSJI");
        przegrany.Tresc.GetProperty("szczegoly").GetProperty("aktualny").GetProperty("version").GetInt32().ShouldBe(2);
        (await klient.PobierzAsync(Sciezka)).Tresc.GetProperty("version").GetInt32().ShouldBe(2);
    }
}
