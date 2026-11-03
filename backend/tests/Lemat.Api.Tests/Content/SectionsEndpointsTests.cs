// @Role: Testy integracyjne GET /api/v1/dzialy (kontrakt POC 4.2): tablica 13 Działów zgodna z kontraktem, 401 bez tokenu | @DependsOn: ApiFactory, KlientZalogowany, DaneReferencyjne | @UsedBy: dotnet test | @Task: TASK-124
using System.Net;
using System.Text.Json;
using Lemat.Api.Tests.Support;

namespace Lemat.Api.Tests.Content;

public sealed class SectionsEndpointsTests(ApiFactory fabryka, BazaTestowaFixture baza) : IClassFixture<ApiFactory>
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ZwracaTrzynascieDzialowDokladnieJakWKontrakcie()
    {
        await DaneReferencyjne.ZapewnijDzialyAsync(baza.Polaczenie, Token);
        using var klient = await KlientZalogowany.ZarejestrujAsync(fabryka);

        var odpowiedz = await klient.PobierzAsync("/api/v1/dzialy");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        odpowiedz.Tresc.ValueKind.ShouldBe(JsonValueKind.Array);
        odpowiedz.Tresc.GetArrayLength().ShouldBe(13);
        for (var i = 0; i < 13; i++)
        {
            odpowiedz.Tresc[i].EnumerateObject().Select(p => p.Name).ShouldBe(["id", "kod", "nazwa", "poziom_matury"]);
            odpowiedz.Tresc[i].GetProperty("id").GetInt32().ShouldBe(i + 1);
            odpowiedz.Tresc[i].GetProperty("kod").GetString().ShouldBe(DaneReferencyjne.Dzialy[i].Kod);
            odpowiedz.Tresc[i].GetProperty("nazwa").GetString().ShouldBe(DaneReferencyjne.Dzialy[i].Nazwa);
            odpowiedz.Tresc[i].GetProperty("poziom_matury").GetString().ShouldBe(i == 12 ? "ROZSZERZONY" : "PODSTAWOWY");
        }
    }

    [Fact]
    public async Task BezTokenuZwraca401()
    {
        using var klient = KlientZalogowany.Anonimowy(fabryka);

        var odpowiedz = await klient.PobierzAsync("/api/v1/dzialy");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("NIEUWIERZYTELNIONY");
    }
}
