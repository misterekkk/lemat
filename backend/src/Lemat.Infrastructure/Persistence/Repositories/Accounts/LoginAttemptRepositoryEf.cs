// @Role: Repozytorium EF Core Prób logowania: dopisanie próby i zliczanie nieudanych prób dla e-maila w oknie czasu | @DependsOn: LematDbContext, IProbaLogowaniaRepozytorium | @UsedBy: DI (PersistenceRegistration), uwierzytelnianie (TASK-072) | @Task: TASK-016
using Lemat.Application.Accounts;
using Lemat.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Accounts;

public sealed class ProbaLogowaniaRepozytoriumEf(LematDbContext kontekst) : IProbaLogowaniaRepozytorium
{
    public void Dodaj(ProbaLogowania proba)
    {
        ArgumentNullException.ThrowIfNull(proba);
        kontekst.Add(proba);
    }

    public Task<int> PoliczNieudaneAsync(string email, DateTimeOffset od, CancellationToken ct)
    {
        var szukany = (email ?? string.Empty).Trim().ToLowerInvariant();
        return kontekst.Set<ProbaLogowania>().AsNoTracking()
            .CountAsync(p => p.Email == szukany && !p.Udana && p.CreatedAt >= od, ct);
    }
}
