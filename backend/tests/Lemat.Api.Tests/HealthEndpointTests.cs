// @Role: Test endpointu GET /api/v1/zdrowie i dokumentu OpenAPI (WebApplicationFactory) | @DependsOn: Lemat.Api | @UsedBy: dotnet test | @Task: TASK-003
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lemat.Api.Tests;

public sealed class HealthEndpointTests(WebApplicationFactory<Program> fabryka) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task ZdrowieZwraca200IJson()
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.GetAsync("/api/v1/zdrowie", TestContext.Current.CancellationToken);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        odpowiedz.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        (await odpowiedz.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("OK");
    }

    [Fact]
    public async Task DokumentOpenApiJestDostepny()
    {
        using var klient = fabryka.CreateClient();

        using var odpowiedz = await klient.GetAsync("/api/v1/openapi.json", TestContext.Current.CancellationToken);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await odpowiedz.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("/api/v1/zdrowie");
    }
}
