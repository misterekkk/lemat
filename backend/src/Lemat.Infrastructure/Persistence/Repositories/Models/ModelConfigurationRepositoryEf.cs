// @Role: Repozytorium EF Core agregatu Konfiguracja modelu: wczytanie do zmiany, odczyt aktywnej konfiguracji roli, lista, dodanie i wyłączenie poprzedniej aktywnej przez ExecuteUpdate | @DependsOn: LematDbContext, IKonfiguracjaModeluRepozytorium | @UsedBy: DI (PersistenceRegistration), ModeleFasada | @Task: TASK-018
using Lemat.Application.Models;
using Lemat.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Models;

public sealed class KonfiguracjaModeluRepozytoriumEf(LematDbContext kontekst) : IKonfiguracjaModeluRepozytorium
{
    public Task<KonfiguracjaModelu?> PobierzAsync(KonfiguracjaModeluId id, CancellationToken ct) =>
        kontekst.Set<KonfiguracjaModelu>().AsTracking().FirstOrDefaultAsync(k => k.Id == id, ct);

    public Task<KonfiguracjaModelu?> PobierzDoOdczytuAsync(KonfiguracjaModeluId id, CancellationToken ct) =>
        kontekst.Set<KonfiguracjaModelu>().AsNoTracking().FirstOrDefaultAsync(k => k.Id == id, ct);

    public Task<KonfiguracjaModelu?> PobierzAktywnaAsync(RolaModelu rola, CancellationToken ct) =>
        kontekst.Set<KonfiguracjaModelu>().AsNoTracking().FirstOrDefaultAsync(k => k.Rola == rola && k.Aktywna, ct);

    public async Task<IReadOnlyList<KonfiguracjaModelu>> ListaAsync(CancellationToken ct) =>
        await kontekst.Set<KonfiguracjaModelu>().AsNoTracking().OrderBy(k => k.CreatedAt).ThenBy(k => k.Id).ToListAsync(ct);

    public void Dodaj(KonfiguracjaModelu konfiguracja)
    {
        ArgumentNullException.ThrowIfNull(konfiguracja);
        kontekst.Add(konfiguracja);
    }

    public async Task DezaktywujAktywnaAsync(RolaModelu rola, KonfiguracjaModeluId pozostaw, CancellationToken ct) =>
        await kontekst.Set<KonfiguracjaModelu>()
            .Where(k => k.Rola == rola && k.Aktywna && k.Id != pozostaw)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.Aktywna, false), ct);
}
