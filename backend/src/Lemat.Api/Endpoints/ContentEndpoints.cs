// @Role: Grupa endpointów Treści: GET /api/v1/dzialy (13 Działów, kontrakt POC 4.2); kolejne endpointy Treści dopisują TASK-125 | @DependsOn: ITresciFasada, PolitykiAutoryzacji, DzialWyjscie | @UsedBy: ApiHost (IGrupaEndpointow) | @Task: TASK-124
using Lemat.Api.Contracts.Common;
using Lemat.Api.Security;
using Lemat.Application.Content;
using Lemat.Domain.Common;

namespace Lemat.Api.Endpoints;

public sealed class EndpointyTresci : IGrupaEndpointow
{
    public void Mapuj(IEndpointRouteBuilder trasy)
    {
        ArgumentNullException.ThrowIfNull(trasy);

        trasy.MapGet("/api/v1/dzialy", ListaDzialowAsync)
            .WithName("ListaDzialow")
            .RequireAuthorization(PolitykiAutoryzacji.Uczen)
            .Produces<IReadOnlyList<DzialWyjscie>>();
    }

    // Zwykła tablica, sort po id; poziom_matury = ROZSZERZONY dla Działu tylko_rozszerzony, inaczej PODSTAWOWY (OQ-14).
    private static async Task<IResult> ListaDzialowAsync(ITresciFasada tresci, CancellationToken ct)
    {
        var dzialy = await tresci.ListaDzialowAsync(ct);
        return Results.Ok(
            dzialy
                .OrderBy(d => d.Id.Wartosc)
                .Select(d => new DzialWyjscie(d.Id.Wartosc, d.Kod, d.Nazwa, d.TylkoRozszerzony ? PoziomMatury.Rozszerzony : PoziomMatury.Podstawowy))
                .ToList());
    }
}
