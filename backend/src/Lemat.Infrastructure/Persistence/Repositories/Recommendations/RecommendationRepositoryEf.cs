// @Role: Repozytorium EF Core niezmiennej Rekomendacji: dodawanie i odczyt po elemencie Ścieżki | @DependsOn: LematDbContext, IRekomendacjaRepozytorium | @UsedBy: DI (PersistenceRegistration), RekomendacjeFasada | @Task: TASK-019
using Lemat.Application.Recommendations;
using Lemat.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Recommendations;

public sealed class RekomendacjaRepozytoriumEf(LematDbContext kontekst) : IRekomendacjaRepozytorium
{
    public void Dodaj(Rekomendacja rekomendacja)
    {
        ArgumentNullException.ThrowIfNull(rekomendacja);
        kontekst.Add(rekomendacja);
    }

    public Task<Rekomendacja?> PobierzPoElemencieAsync(ElementSciezkiId element, CancellationToken ct) =>
        kontekst.Set<Rekomendacja>().AsNoTracking().FirstOrDefaultAsync(r => r.ElementSciezkiId == element, ct);
}
