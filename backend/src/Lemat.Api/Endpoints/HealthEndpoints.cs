// @Role: Endpoint GET /api/v1/zdrowie (kontrola zdrowia procesu api) | @DependsOn: Lemat.Api.Contracts.Common | @UsedBy: Lemat.Api.Configuration.ApiHost | @Task: TASK-003
using Lemat.Api.Contracts.Common;

namespace Lemat.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/zdrowie", () => TypedResults.Ok(new ZdrowieWyjscie("OK")))
            .WithName("PobierzZdrowie")
            .Produces<ZdrowieWyjscie>();
        return app;
    }
}
