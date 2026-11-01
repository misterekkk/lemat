// @Role: DTO wejścia POST /auth/logowanie | @DependsOn: — | @UsedBy: AccountsEndpoints, LogowanieWejscieWalidator | @Task: TASK-123
namespace Lemat.Api.Contracts.Accounts;

public sealed record LogowanieWejscie(string Email, string Haslo);
