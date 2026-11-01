// @Role: Port grupy endpointów mapowanej przez ApiHost.MapEndpoints (moduły dopisują się rejestracją w DI; testy dodają własne) | @DependsOn: Microsoft.AspNetCore.Routing | @UsedBy: ApiHost, grupy endpointów TASK-123+, testy | @Task: TASK-121
namespace Lemat.Api.Endpoints;

/// <summary>Grupa endpointów rejestrowana w DI jako <c>IGrupaEndpointow</c> i mapowana przy starcie.</summary>
public interface IGrupaEndpointow
{
    void Mapuj(IEndpointRouteBuilder trasy);
}
