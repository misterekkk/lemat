// @Role: Repozytoria EF Core danych referencyjnych: Działy i Umiejętności (odczyt bez śledzenia, dodanie wiersza) | @DependsOn: LematDbContext, IDzialRepozytorium, IUmiejetnoscRepozytorium | @UsedBy: DI (PersistenceRegistration), TresciFasada, loader danych startowych (TASK-025) | @Task: TASK-017
using Lemat.Application.Content;
using Lemat.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Content;

public sealed class DzialRepozytoriumEf(LematDbContext kontekst) : IDzialRepozytorium
{
    public async Task<IReadOnlyList<Dzial>> ListaAsync(CancellationToken ct) =>
        await kontekst.Set<Dzial>().AsNoTracking().OrderBy(d => d.Kolejnosc).ThenBy(d => d.Id).ToListAsync(ct);

    public Task<Dzial?> PobierzAsync(DzialId id, CancellationToken ct) =>
        kontekst.Set<Dzial>().AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

    public void Dodaj(Dzial dzial)
    {
        ArgumentNullException.ThrowIfNull(dzial);
        kontekst.Add(dzial);
    }
}

public sealed class UmiejetnoscRepozytoriumEf(LematDbContext kontekst) : IUmiejetnoscRepozytorium
{
    public async Task<IReadOnlyList<Umiejetnosc>> ListaAsync(DzialId? dzial, CancellationToken ct)
    {
        var zapytanie = kontekst.Set<Umiejetnosc>().AsNoTracking();
        if (dzial is { } id)
        {
            zapytanie = zapytanie.Where(u => u.DzialId == id);
        }

        return await zapytanie.OrderBy(u => u.DzialId).ThenBy(u => u.Kod).ToListAsync(ct);
    }

    public Task<Umiejetnosc?> PobierzAsync(UmiejetnoscId id, CancellationToken ct) =>
        kontekst.Set<Umiejetnosc>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public void Dodaj(Umiejetnosc umiejetnosc)
    {
        ArgumentNullException.ThrowIfNull(umiejetnosc);
        kontekst.Add(umiejetnosc);
    }
}
