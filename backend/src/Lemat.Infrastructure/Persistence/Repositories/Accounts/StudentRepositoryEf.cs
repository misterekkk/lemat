// @Role: Repozytorium EF Core agregatu Uczeń (ze śledzeniem do zmiany, bez śledzenia do odczytu, If-Match) | @DependsOn: LematDbContext, IUczenRepozytorium | @UsedBy: DI (PersistenceRegistration), AccountsFacade | @Task: TASK-016
using Lemat.Application.Accounts;
using Lemat.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Accounts;

public sealed class UczenRepozytoriumEf(LematDbContext kontekst) : IUczenRepozytorium
{
    public async Task<Uczen?> PobierzAsync(UczenId id, int? oczekiwanaWersja, CancellationToken ct)
    {
        var uczen = await kontekst.Set<Uczen>().AsTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (uczen is not null && oczekiwanaWersja is { } wersja)
        {
            kontekst.Entry(uczen).Property(u => u.Version).OriginalValue = wersja;
        }

        return uczen;
    }

    public Task<Uczen?> PobierzDoOdczytuAsync(UczenId id, CancellationToken ct) =>
        kontekst.Set<Uczen>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public void Dodaj(Uczen uczen)
    {
        ArgumentNullException.ThrowIfNull(uczen);
        kontekst.Add(uczen);
    }
}
