// @Role: DTO odpowiedzi POST /auth/rejestracja (status konta i sesja Ucznia) | @DependsOn: Lemat.Domain.Accounts.StatusKonta, SesjaWyjscie | @UsedBy: AccountsEndpoints | @Task: TASK-123
using Lemat.Domain.Accounts;

namespace Lemat.Api.Contracts.Accounts;

public sealed record RejestracjaWyjscie(StatusKonta StatusKonta, SesjaWyjscie? Sesja);
