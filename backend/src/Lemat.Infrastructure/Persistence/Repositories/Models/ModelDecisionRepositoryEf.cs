// @Role: Repozytorium EF Core Decyzji modelu: wsadowe dodanie (AddRange) do tabeli partycjonowanej i odczyt po kontekście (ix_decyzja_kontekst) | @DependsOn: LematDbContext, IDecyzjaModeluRepozytorium | @UsedBy: DI (PersistenceRegistration), ModeleFasada | @Task: TASK-018
using Lemat.Application.Models;
using Lemat.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Models;

public sealed class DecyzjaModeluRepozytoriumEf(LematDbContext baza) : IDecyzjaModeluRepozytorium
{
    public void DodajRange(IEnumerable<DecyzjaModelu> decyzje)
    {
        ArgumentNullException.ThrowIfNull(decyzje);
        baza.AddRange(decyzje);
    }

    public async Task<IReadOnlyList<DecyzjaModelu>> ListaDlaKontekstuAsync(KontekstDecyzji kontekst, Guid kontekstId, CancellationToken ct) =>
        await baza.Set<DecyzjaModelu>().AsNoTracking()
            .Where(d => d.Kontekst == kontekst && d.KontekstId == kontekstId)
            .OrderBy(d => d.CreatedAt).ThenBy(d => d.KodPytania)
            .ToListAsync(ct);
}
