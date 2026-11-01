// @Role: Repozytorium EF Core agregatu Nauczyciel (ze śledzeniem do zmiany, bez śledzenia do odczytu, If-Match) | @DependsOn: LematDbContext, INauczycielRepozytorium | @UsedBy: DI (PersistenceRegistration), AccountsFacade | @Task: TASK-016
using Lemat.Application.Accounts;
using Lemat.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Accounts;

public sealed class NauczycielRepozytoriumEf(LematDbContext kontekst) : INauczycielRepozytorium
{
    public async Task<Nauczyciel?> PobierzAsync(NauczycielId id, int? oczekiwanaWersja, CancellationToken ct)
    {
        var nauczyciel = await kontekst.Set<Nauczyciel>().AsTracking().FirstOrDefaultAsync(n => n.Id == id, ct);
        if (nauczyciel is not null && oczekiwanaWersja is { } wersja)
        {
            kontekst.Entry(nauczyciel).Property(n => n.Version).OriginalValue = wersja;
        }

        return nauczyciel;
    }

    public Task<Nauczyciel?> PobierzDoOdczytuAsync(NauczycielId id, CancellationToken ct) =>
        kontekst.Set<Nauczyciel>().AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);

    public void Dodaj(Nauczyciel nauczyciel)
    {
        ArgumentNullException.ThrowIfNull(nauczyciel);
        kontekst.Add(nauczyciel);
    }
}
