// @Role: Port repozytorium Prób logowania (dziennik udanych i nieudanych prób) | @DependsOn: Lemat.Domain.Accounts.ProbaLogowania | @UsedBy: uwierzytelnianie (TASK-072), LoginAttemptRepositoryEf | @Task: TASK-016
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public interface IProbaLogowaniaRepozytorium
{
    void Dodaj(ProbaLogowania proba);

    /// <summary>Liczba nieudanych prób dla adresu e-mail od podanej chwili (indeks ix_proba_logowania_email).</summary>
    Task<int> PoliczNieudaneAsync(string email, DateTimeOffset od, CancellationToken ct);
}
