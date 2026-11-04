// @Role: Testy infrastruktury HTTP /api/v1: ProblemDetails, wiązanie, kursor HMAC, If-Match, nagłówki, serializacja JSON, Mock, baza | @DependsOn: Lemat.Api, ApiFactory, TestoweEndpointy, BazaTestowaFixture | @UsedBy: dotnet test | @Task: TASK-121
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lemat.Api.Errors;
using Lemat.Api.Http;
using Lemat.Api.Pagination;
using Lemat.Api.Tests.Support;
using Lemat.Application.Common;
using Microsoft.AspNetCore.Http;

namespace Lemat.Api.Tests;

public sealed class HttpInfrastructureTests(ApiFactory fabryka, BazaTestowaFixture baza) : IClassFixture<ApiFactory>
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static StringContent Json(string tresc) => new(tresc, Encoding.UTF8, "application/json");

    private static async Task<JsonElement> Tresc(HttpResponseMessage odpowiedz) =>
        JsonDocument.Parse(await odpowiedz.Content.ReadAsStringAsync(Token)).RootElement.Clone();

    [Fact]
    public async Task WyjatekDomenowyJestProblemDetailsZKodemITraceId()
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.GetAsync("/api/v1/test/domena", Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        odpowiedz.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problem = await Tresc(odpowiedz);
        problem.GetProperty("type").GetString().ShouldBe("https://lemat.pl/bledy/rozwiazanie-nieprawidlowy-stan");
        problem.GetProperty("title").GetString().ShouldBe("Nieprawidłowy stan rozwiązania");
        problem.GetProperty("status").GetInt32().ShouldBe(409);
        problem.GetProperty("kod").GetString().ShouldBe("ROZWIAZANIE_NIEPRAWIDLOWY_STAN");
        problem.GetProperty("instance").GetString().ShouldBe("/api/v1/test/domena");
        problem.GetProperty("trace_id").GetString().ShouldNotBeNullOrWhiteSpace();
        problem.GetProperty("szczegoly").ValueKind.ShouldBe(JsonValueKind.Null);
        problem.TryGetProperty("bledy_pol", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task NieznanyKodDomenyKoncziSieBledemWewnetrznymBezSzczegolowTechnicznych()
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.GetAsync("/api/v1/test/wyjatek", Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var tekst = await odpowiedz.Content.ReadAsStringAsync(Token);
        tekst.ShouldNotContain("tajny");
        tekst.ShouldContain("\"kod\":\"BLAD_WEWNETRZNY\"");

        using var kodSpozaKatalogu = await klient.GetAsync("/api/v1/test/nieznany-kod-domeny", Token);
        kodSpozaKatalogu.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task NieznanePoleJsonTo422WalidacjaZPoleNieznane()
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.PostAsync(
            "/api/v1/test/wejscie",
            Json("""{"haslo":"x","oswiadczenie_wieku_16":true,"liczba_wynikow_7d":1,"status":"WSTEPNA","zbedne":1}"""),
            Token);

        odpowiedz.StatusCode.ShouldBe((HttpStatusCode)422);
        var problem = await Tresc(odpowiedz);
        problem.GetProperty("kod").GetString().ShouldBe("WALIDACJA");
        var blad = problem.GetProperty("bledy_pol")[0];
        blad.GetProperty("kod_reguly").GetString().ShouldBe("POLE_NIEZNANE");
        blad.GetProperty("pole").GetString().ShouldBe("zbedne");
        blad.GetProperty("komunikat").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ZlyTypPolaTo422TypNieprawidlowy()
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.PostAsync(
            "/api/v1/test/wejscie",
            Json("""{"haslo":"x","oswiadczenie_wieku_16":true,"liczba_wynikow_7d":"abc","status":"WSTEPNA"}"""),
            Token);

        odpowiedz.StatusCode.ShouldBe((HttpStatusCode)422);
        var blad = (await Tresc(odpowiedz)).GetProperty("bledy_pol")[0];
        blad.GetProperty("kod_reguly").GetString().ShouldBe("TYP_NIEPRAWIDLOWY");
        blad.GetProperty("pole").GetString().ShouldBe("liczba_wynikow_7d");
    }

    [Theory]
    [InlineData("""{"haslo":"x","oswiadczenie_wieku_16":true""")]
    [InlineData("""{"haslo":""")]
    [InlineData("to nie jest json")]
    public async Task UcietyLubNiepoprawnyJsonTo400ZapytanieNieprawidlowe(string tresc)
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.PostAsync("/api/v1/test/wejscie", Json(tresc), Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await Tresc(odpowiedz)).GetProperty("kod").GetString().ShouldBe("ZAPYTANIE_NIEPRAWIDLOWE");
    }

    [Fact]
    public async Task RekordSerializujeSieZSnakeCaseCyfrIEnumemUpperSnake()
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.PostAsync(
            "/api/v1/test/wejscie",
            Json("""{"haslo":"a<b>&c","oswiadczenie_wieku_16":true,"liczba_wynikow_7d":7,"status":"OCZEKUJE_NA_DECYZJE"}"""),
            Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tekst = await odpowiedz.Content.ReadAsStringAsync(Token);
        tekst.ShouldContain("\"oswiadczenie_wieku_16\":true");
        tekst.ShouldContain("\"liczba_wynikow_7d\":7");
        tekst.ShouldContain("\"status\":\"OCZEKUJE_NA_DECYZJE\"");
        tekst.ShouldContain("a<b>&c");
    }

    [Fact]
    public async Task StronicowanieZwracaKursorKtoryDziala()
    {
        using var klient = fabryka.CreateClient();

        var pierwsza = await Tresc(await klient.GetAsync("/api/v1/test/strona?limit=3", Token));
        pierwsza.GetProperty("elementy").EnumerateArray().Select(e => e.GetInt32()).ShouldBe([0, 1, 2]);
        var kursor = pierwsza.GetProperty("nastepny_kursor").GetString();
        kursor.ShouldNotBeNullOrWhiteSpace();

        var druga = await Tresc(await klient.GetAsync($"/api/v1/test/strona?limit=3&kursor={Uri.EscapeDataString(kursor!)}", Token));
        druga.GetProperty("elementy").EnumerateArray().Select(e => e.GetInt32()).ShouldBe([3, 4, 5]);
    }

    [Fact]
    public async Task PodrobionyKursorTo400KursorNieprawidlowy()
    {
        using var klient = fabryka.CreateClient();
        var pierwsza = await Tresc(await klient.GetAsync("/api/v1/test/strona?limit=3", Token));
        var kursor = pierwsza.GetProperty("nastepny_kursor").GetString()!;
        var czesci = kursor.Split('.');
        var podrobiony = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"pozycja":999}""")).TrimEnd('=') + "." + czesci[1];

        foreach (var zly in new[] { podrobiony, "smieci", "a.b", string.Empty + czesci[0] + ".x" })
        {
            using var odpowiedz = await klient.GetAsync($"/api/v1/test/strona?kursor={Uri.EscapeDataString(zly)}", Token);

            odpowiedz.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await Tresc(odpowiedz)).GetProperty("kod").GetString().ShouldBe("KURSOR_NIEPRAWIDLOWY");
        }
    }

    [Theory]
    [InlineData("0")]
    [InlineData("101")]
    public async Task LimitPozaZakresemTo422(string limit)
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.GetAsync($"/api/v1/test/strona?limit={limit}", Token);

        odpowiedz.StatusCode.ShouldBe((HttpStatusCode)422);
        (await Tresc(odpowiedz)).GetProperty("bledy_pol")[0].GetProperty("pole").GetString().ShouldBe("limit");
    }

    [Fact]
    public async Task BrakIfMatchTo428()
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.PatchAsync("/api/v1/test/zasob", Json("{}"), Token);

        odpowiedz.StatusCode.ShouldBe((HttpStatusCode)428);
        (await Tresc(odpowiedz)).GetProperty("kod").GetString().ShouldBe("WYMAGANY_IF_MATCH");
    }

    [Theory]
    [InlineData("3")]
    [InlineData("\"abc\"")]
    [InlineData("W/\"3\"")]
    [InlineData("\"\"")]
    public async Task ZlaFormaIfMatchTo400(string wartosc)
    {
        using var klient = fabryka.CreateClient();
        using var zadanie = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/test/zasob") { Content = Json("{}") };
        zadanie.Headers.TryAddWithoutValidation("If-Match", wartosc);

        using var odpowiedz = await klient.SendAsync(zadanie, Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await Tresc(odpowiedz)).GetProperty("kod").GetString().ShouldBe("NAGLOWEK_NIEPRAWIDLOWY");
    }

    [Fact]
    public async Task NiezgodnyIfMatchTo412ZAktualnymZasobem()
    {
        using var klient = fabryka.CreateClient();
        using var zadanie = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/test/zasob") { Content = Json("{}") };
        zadanie.Headers.TryAddWithoutValidation("If-Match", "\"2\"");

        using var odpowiedz = await klient.SendAsync(zadanie, Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        var problem = await Tresc(odpowiedz);
        problem.GetProperty("kod").GetString().ShouldBe("KONFLIKT_WERSJI");
        var aktualny = problem.GetProperty("szczegoly").GetProperty("aktualny");
        aktualny.GetProperty("version").GetInt64().ShouldBe(3);
        aktualny.GetProperty("id").GetString().ShouldBe(TestoweEndpointy.Zasob.Id.ToString());
    }

    [Fact]
    public async Task ZgodnyIfMatchPrzechodzi()
    {
        using var klient = fabryka.CreateClient();
        using var zadanie = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/test/zasob") { Content = Json("{}") };
        zadanie.Headers.TryAddWithoutValidation("If-Match", "\"3\"");

        using var odpowiedz = await klient.SendAsync(zadanie, Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Routing404I405MajaKodIProblemJson()
    {
        using var klient = fabryka.CreateClient();

        using var brak = await klient.GetAsync("/api/v1/nie-ma-takiego", Token);
        brak.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        brak.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problem = await Tresc(brak);
        problem.GetProperty("kod").GetString().ShouldBe("NIE_ZNALEZIONO");
        problem.GetProperty("type").GetString().ShouldBe("https://lemat.pl/bledy/nie-znaleziono");
        problem.GetProperty("instance").GetString().ShouldBe("/api/v1/nie-ma-takiego");

        using var metoda = await klient.PostAsync("/api/v1/zdrowie", Json("{}"), Token);
        metoda.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        (await Tresc(metoda)).GetProperty("kod").GetString().ShouldBe("METODA_NIEDOZWOLONA");
    }

    [Fact]
    public async Task NieobslugiwanyContentTypeTo415ZKodem()
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.PostAsync(
            "/api/v1/test/wejscie",
            new StringContent("a=b", Encoding.UTF8, "application/x-www-form-urlencoded"),
            Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        (await Tresc(odpowiedz)).GetProperty("kod").GetString().ShouldBe("NIEOBSLUGIWANY_FORMAT");
    }

    [Fact]
    public async Task OdpowiedziMajaNaglowkiNoStoreVaryIWersjeApi()
    {
        using var klient = fabryka.CreateClient();

        foreach (var sciezka in new[] { "/api/v1/zdrowie", "/api/v1/nie-ma-takiego", "/api/v1/test/domena" })
        {
            using var odpowiedz = await klient.GetAsync(sciezka, Token);

            odpowiedz.Headers.CacheControl?.NoStore.ShouldBeTrue(sciezka);
            odpowiedz.Headers.Vary.ShouldContain("Authorization", sciezka);
            odpowiedz.Headers.GetValues("X-Lemat-Wersja-Api").ShouldBe(["1.0.0"], sciezka);
        }
    }

    [Fact]
    public async Task ZdrowieNadal200()
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.GetAsync("/api/v1/zdrowie", Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NaglowekMockJestHonorowanyWDevelopmentINieznanaWartoscTo422()
    {
        using var klient = fabryka.CreateClient();

        using var domyslny = await klient.PostAsync("/api/v1/test/mock", null, Token);
        (await Tresc(domyslny)).GetProperty("scenariusz").GetString().ShouldBe("domyslny");

        using var zNaglowkiem = new HttpRequestMessage(HttpMethod.Post, "/api/v1/test/mock");
        zNaglowkiem.Headers.Add("X-Lemat-Mock-Scenariusz", "rozpoznanie-nieczytelne");
        using var odpowiedz = await klient.SendAsync(zNaglowkiem, Token);
        (await Tresc(odpowiedz)).GetProperty("scenariusz").GetString().ShouldBe("rozpoznanie-nieczytelne");

        using var nieznany = new HttpRequestMessage(HttpMethod.Post, "/api/v1/test/mock");
        nieznany.Headers.Add("X-Lemat-Mock-Scenariusz", "cos-innego");
        using var zle = await klient.SendAsync(nieznany, Token);
        zle.StatusCode.ShouldBe((HttpStatusCode)422);
        var blad = (await Tresc(zle)).GetProperty("bledy_pol")[0];
        blad.GetProperty("pole").GetString().ShouldBe("X-Lemat-Mock-Scenariusz");
        blad.GetProperty("kod_reguly").GetString().ShouldBe("MOCK_SCENARIUSZ_NIEZNANY");
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    [InlineData("Development", true)]
    [InlineData("E2E", true)]
    public async Task NaglowekMockJestIgnorowanyPozaDevelopmentIE2E(string srodowisko, bool honorowany)
    {
        var filtr = new FiltrScenariuszaMock(new LematOpcje { Srodowisko = srodowisko });
        var kontekst = new DefaultHttpContext();
        kontekst.Request.Headers["X-Lemat-Mock-Scenariusz"] = "rozpoznanie-nieczytelne";

        await filtr.InvokeAsync(
            new DefaultEndpointFilterInvocationContext(kontekst),
            _ => ValueTask.FromResult<object?>(null));

        FiltrScenariuszaMock.Pobierz(kontekst).ShouldBe(honorowany ? "rozpoznanie-nieczytelne" : "domyslny");
    }

    [Fact]
    public async Task NieznanyScenariuszPozaDevelopmentNieZglaszaBledu()
    {
        var filtr = new FiltrScenariuszaMock(new LematOpcje { Srodowisko = "Production" });
        var kontekst = new DefaultHttpContext();
        kontekst.Request.Headers["X-Lemat-Mock-Scenariusz"] = "dowolny";

        await filtr.InvokeAsync(new DefaultEndpointFilterInvocationContext(kontekst), _ => ValueTask.FromResult<object?>(null));

        FiltrScenariuszaMock.Pobierz(kontekst).ShouldBe("domyslny");
    }

    [Fact]
    public async Task ApiLaczySieZBazaPoMigracji()
    {
        baza.Polaczenie.ShouldNotBeNullOrWhiteSpace();
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.GetAsync("/api/v1/test/baza", Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Tresc(odpowiedz)).GetProperty("liczba").GetInt64().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void KatalogBledowMaWpisDlaKazdegoKoduIUnikalnyType()
    {
        var kody = Enum.GetValues<KodBledu>();

        ErrorCatalog.WszystkieKody.OrderBy(k => k).ShouldBe(kody.OrderBy(k => k));
        kody.Select(ErrorCatalog.Type).Distinct().Count().ShouldBe(kody.Length);
        kody.Select(ErrorCatalog.Nazwa).Distinct().Count().ShouldBe(kody.Length);
        ErrorCatalog.Nazwa(KodBledu.WymaganyIfMatch).ShouldBe("WYMAGANY_IF_MATCH");
        ErrorCatalog.TryParse("EMAIL_ZAJETY", out var kod).ShouldBeTrue();
        kod.ShouldBe(KodBledu.EmailZajety);
        ErrorCatalog.TryParse("KOD_SPOZA_KATALOGU", out _).ShouldBeFalse();
    }

    [Fact]
    public void KodekKursoraOdrzucaKursorPodpisanyInnymKluczem()
    {
        var obcy = new KodekKursora("inny-klucz").Zakoduj(new PozycjaKursora(1));

        var blad = Should.Throw<Lemat.Api.Errors.BladApiException>(() => new KodekKursora("klucz-a").Odkoduj<PozycjaKursora>(obcy));

        blad.Kod.ShouldBe(KodBledu.KursorNieprawidlowy);
    }
}
