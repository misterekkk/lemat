// @Role: Repozytorium EF Core agregatu Transkrypcja: dodawanie, odczyt po identyfikatorze i najnowszej wersji Rozwiązania | @DependsOn: LematDbContext, ITranskrypcjaRepozytorium | @UsedBy: DI (PersistenceRegistration), RozwiazaniaFasada | @Task: TASK-126
using Lemat.Application.Solutions;
using Lemat.Domain.Solutions;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Solutions;

public sealed class TranskrypcjaRepozytoriumEf(LematDbContext kontekst) : ITranskrypcjaRepozytorium
{
    public void Dodaj(Transkrypcja transkrypcja)
    {
        ArgumentNullException.ThrowIfNull(transkrypcja);
        kontekst.Add(transkrypcja);
    }

    public Task<Transkrypcja?> PobierzAsync(TranskrypcjaId id, CancellationToken ct) =>
        kontekst.Set<Transkrypcja>().AsNoTracking().Include(t => t.Kroki).FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Transkrypcja?> PobierzNajnowszaAsync(RozwiazanieId rozwiazanie, CancellationToken ct) =>
        kontekst.Set<Transkrypcja>().AsNoTracking().Include(t => t.Kroki)
            .Where(t => t.RozwiazanieId == rozwiazanie)
            .OrderByDescending(t => t.Wersja)
            .FirstOrDefaultAsync(ct);
}
