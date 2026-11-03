// @Role: Testy integracyjne GET /api/v1/zadania i GET /api/v1/zadania/{id} (kontrakt POC 5, 6.3): odpowiedź identyczna z kontraktem po seed-poc, filtr dzial_id, 404, brak pól wzorcowych, paginacja kursorowa, 401 | @DependsOn: ApiFactory, KlientZalogowany, PocSeed | @UsedBy: dotnet test | @Task: TASK-125
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lemat.Api.Tests.Support;

namespace Lemat.Api.Tests.Content;

public sealed class ExercisesEndpointsTests(ApiFactory fabryka, BazaTestowaFixture baza) : IClassFixture<ApiFactory>
{
    private const string Id4 = "a1000000-0000-4000-8000-000000000004";

    private static readonly string[] KolejnoscKontraktu = ["2", "5", "1", "3", "4"];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string Uuid(string numer) => $"a1000000-0000-4000-8000-00000000000{numer}";

    private static string[] Zakazane => ["rozwiazanie_wzorcowe", "kryteria_oceniania", "wynik_koncowy_wzorcowy", "klucz_odpowiedzi", "wynik_koncowy"];

    private async Task<KlientZalogowany> KlientAsync()
    {
        await PocSeed.ZapewnijAsync(baza.Polaczenie, Token);
        return await KlientZalogowany.ZarejestrujAsync(fabryka);
    }

    private static JsonNode Wezel(JsonElement element) => JsonNode.Parse(element.GetRawText())!;

    [Fact]
    public async Task ListaJestIdentycznaZKontraktem()
    {
        using var klient = await KlientAsync();
        var oczekiwane = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Content", "zadania-poc.json"), Token))!;

        var odpowiedz = await klient.PobierzAsync("/api/v1/zadania");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonNode.DeepEquals(Wezel(odpowiedz.Tresc), oczekiwane).ShouldBeTrue(odpowiedz.Tresc.GetRawText());
        odpowiedz.Tresc.GetProperty("elementy").EnumerateArray().Select(e => e.GetProperty("id").GetString()![^1..]).ShouldBe(KolejnoscKontraktu);
    }

    [Fact]
    public async Task FiltrDzialuZwracaTylkoZadanieZTegoDzialu()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.PobierzAsync("/api/v1/zadania?dzial_id=5");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        odpowiedz.Tresc.GetProperty("elementy").EnumerateArray().Select(e => e.GetProperty("id").GetString()).ShouldBe([Uuid("1")]);
        odpowiedz.Tresc.GetProperty("nastepny_kursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task NieistniejacyDzialZwracaPustaListe()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.PobierzAsync("/api/v1/zadania?dzial_id=999");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        odpowiedz.Tresc.GetProperty("elementy").GetArrayLength().ShouldBe(0);
        odpowiedz.Tresc.GetProperty("nastepny_kursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task PojedynczeZadanieJestZgodneZPrzykladem()
    {
        using var klient = await KlientAsync();
        var oczekiwane = JsonNode.Parse("""
            {
              "id": "a1000000-0000-4000-8000-000000000004",
              "zadanie_wersja_id": "b1000000-0000-4000-8000-000000000004",
              "rodzaj": "OTWARTE_OBLICZENIOWE",
              "forma_zamknieta": null,
              "tresc_md": "Kąt $\\alpha$ jest ostry i $\\sin\\alpha = \\frac{3}{5}$. Oblicz $\\cos\\alpha$ oraz $\\operatorname{tg}\\alpha$.",
              "zalaczniki": [],
              "opcje": null,
              "stwierdzenia": null,
              "punkty_max": 3,
              "poziom_trudnosci": 3,
              "dzial": { "id": 7, "nazwa": "Trygonometria" },
              "umiejetnosci": [ { "id": 701, "nazwa": "Obliczanie wartości funkcji trygonometrycznych kąta ostrego", "glowna": true } ]
            }
            """)!;

        var odpowiedz = await klient.PobierzAsync($"/api/v1/zadania/{Id4}");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonNode.DeepEquals(Wezel(odpowiedz.Tresc), oczekiwane).ShouldBeTrue(odpowiedz.Tresc.GetRawText());
    }

    [Theory]
    [InlineData("a1000000-0000-4000-8000-0000000000ff")]
    [InlineData("to-nie-jest-uuid")]
    [InlineData("12345")]
    public async Task NieistniejaceLubNiepoprawneIdZwracaja404(string id)
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.PobierzAsync($"/api/v1/zadania/{id}");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("NIE_ZNALEZIONO");
    }

    [Fact]
    public async Task NieopublikowaneZadanieZwraca404WObuEndpointach()
    {
        using var klient = await KlientAsync();
        await UstawStatusAsync(Uuid("3"), "WYCOFANE");
        try
        {
            var pojedyncze = await klient.PobierzAsync($"/api/v1/zadania/{Uuid("3")}");
            var lista = await klient.PobierzAsync("/api/v1/zadania");

            pojedyncze.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            pojedyncze.Tresc.GetProperty("kod").GetString().ShouldBe("NIE_ZNALEZIONO");
            lista.Tresc.GetProperty("elementy").EnumerateArray().Select(e => e.GetProperty("id").GetString()).ShouldNotContain(Uuid("3"));
            lista.Tresc.GetProperty("elementy").GetArrayLength().ShouldBe(4);
        }
        finally
        {
            await UstawStatusAsync(Uuid("3"), "OPUBLIKOWANE");
        }
    }

    [Fact]
    public async Task OdpowiedziNieZawierajaPolWzorcowychAniKryteriow()
    {
        using var klient = await KlientAsync();

        var lista = await klient.PobierzAsync("/api/v1/zadania");
        var pojedyncze = await klient.PobierzAsync($"/api/v1/zadania/{Id4}");

        foreach (var zakazane in Zakazane)
        {
            NazwyPol(lista.Tresc).ShouldNotContain(zakazane);
            NazwyPol(pojedyncze.Tresc).ShouldNotContain(zakazane);
        }

        lista.Tresc.GetRawText().ShouldNotContain("wzorcow");
        lista.Tresc.GetRawText().ShouldNotContain("kryteri");
    }

    [Fact]
    public async Task PaginacjaZLimitemDwaPrzechodziCalaListeBezDuplikatow()
    {
        using var klient = await KlientAsync();
        var identyfikatory = new List<string>();
        string? kursor = null;
        var strony = 0;

        do
        {
            var sciezka = "/api/v1/zadania?limit=2" + (kursor is null ? string.Empty : "&kursor=" + Uri.EscapeDataString(kursor));
            var odpowiedz = await klient.PobierzAsync(sciezka);
            odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
            identyfikatory.AddRange(odpowiedz.Tresc.GetProperty("elementy").EnumerateArray().Select(e => e.GetProperty("id").GetString()!));
            var nastepny = odpowiedz.Tresc.GetProperty("nastepny_kursor");
            kursor = nastepny.ValueKind == JsonValueKind.Null ? null : nastepny.GetString();
            strony++;
        }
        while (kursor is not null && strony < 10);

        strony.ShouldBe(3);
        identyfikatory.Count.ShouldBe(5);
        identyfikatory.Distinct().Count().ShouldBe(5);
        identyfikatory.Select(i => i[^1..]).ShouldBe(KolejnoscKontraktu);
    }

    [Fact]
    public async Task PodrobionyKursorZwraca400()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.PobierzAsync("/api/v1/zadania?kursor=cos.zepsute");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("KURSOR_NIEPRAWIDLOWY");
    }

    [Theory]
    [InlineData("limit=0")]
    [InlineData("limit=101")]
    [InlineData("dzial_id=abc")]
    public async Task NiepoprawneParametryZwracaja422(string zapytanie)
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.PobierzAsync("/api/v1/zadania?" + zapytanie);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe((HttpStatusCode)422);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("WALIDACJA");
    }

    [Theory]
    [InlineData("/api/v1/zadania")]
    [InlineData("/api/v1/zadania/" + Id4)]
    public async Task BezTokenuZwraca401(string sciezka)
    {
        using var klient = KlientZalogowany.Anonimowy(fabryka);

        var odpowiedz = await klient.PobierzAsync(sciezka);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("NIEUWIERZYTELNIONY");
    }

    private static List<string> NazwyPol(JsonElement element)
    {
        var wynik = new List<string>();
        Zbierz(element, wynik);
        return wynik;
    }

    private static void Zbierz(JsonElement element, List<string> wynik)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var wlasciwosc in element.EnumerateObject())
                {
                    wynik.Add(wlasciwosc.Name);
                    Zbierz(wlasciwosc.Value, wynik);
                }

                break;
            case JsonValueKind.Array:
                foreach (var pozycja in element.EnumerateArray())
                {
                    Zbierz(pozycja, wynik);
                }

                break;
        }
    }

    private async Task UstawStatusAsync(string id, string status)
    {
        await using var zrodlo = Npgsql.NpgsqlDataSource.Create(baza.Polaczenie);
        await using var polecenie = zrodlo.CreateCommand("UPDATE zadanie SET status = @status::status_zadania WHERE id = @id");
        polecenie.Parameters.AddWithValue("status", status);
        polecenie.Parameters.AddWithValue("id", Guid.Parse(id));
        await polecenie.ExecuteNonQueryAsync(Token);
    }
}
