// @Role: Grupa endpointów Profilu ucznia: GET /api/v1/uczniowie/ja/profil-uproszczony (kontrakt POC 4.3) | @DependsOn: PobierzProfilUproszczonyHandler, PolitykiAutoryzacji, ProfilUproszczonyWyjscie, BiezacyUzytkownik | @UsedBy: ApiHost (IGrupaEndpointow) | @Task: TASK-129
using Lemat.Api.Contracts.Profile;
using Lemat.Api.Http;
using Lemat.Api.Security;
using Lemat.Application.Profile;
using Lemat.Domain.Accounts;

namespace Lemat.Api.Endpoints;

public sealed class EndpointyProfilu : IGrupaEndpointow
{
    public void Mapuj(IEndpointRouteBuilder trasy)
    {
        ArgumentNullException.ThrowIfNull(trasy);

        trasy.MapGet("/api/v1/uczniowie/ja/profil-uproszczony", PobierzAsync)
            .WithName("PobierzProfilUproszczony")
            .RequireAuthorization(PolitykiAutoryzacji.Uczen)
            .Produces<ProfilUproszczonyWyjscie>();
    }

    private static async Task<IResult> PobierzAsync(PobierzProfilUproszczonyHandler handler, HttpContext kontekst, CancellationToken ct) =>
        Results.Ok(ProfilUproszczonyWyjscie.Z(await handler.WykonajAsync(UczenId.Z(BiezacyUzytkownik.Id(kontekst.User)), ct)));
}
