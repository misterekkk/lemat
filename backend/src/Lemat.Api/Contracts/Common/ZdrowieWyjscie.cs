// @Role: DTO odpowiedzi GET /api/v1/zdrowie | @DependsOn: brak | @UsedBy: Lemat.Api.Endpoints.HealthEndpoints | @Task: TASK-003
namespace Lemat.Api.Contracts.Common;

/// <summary>Odpowiedź kontroli zdrowia procesu api.</summary>
public sealed record ZdrowieWyjscie(string Status);
