// @Role: Repozytorium EF Core agregatu Wkładka teoretyczna (ze śledzeniem do zmiany z If-Match, bez śledzenia do odczytu, opublikowana dla Umiejętności) | @DependsOn: LematDbContext, IWkladkaTeoretycznaRepozytorium | @UsedBy: DI (PersistenceRegistration), TresciFasada | @Task: TASK-017
using Lemat.Application.Content;
using Lemat.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Content;

public sealed class WkladkaTeoretycznaRepozytoriumEf(LematDbContext kontekst) : IWkladkaTeoretycznaRepozytorium
{
    public async Task<WkladkaTeoretyczna?> PobierzAsync(WkladkaTeoretycznaId id, int? oczekiwanaWersja, CancellationToken ct)
    {
        var wkladka = await kontekst.Set<WkladkaTeoretyczna>().AsTracking().FirstOrDefaultAsync(w => w.Id == id, ct);
        if (wkladka is not null && oczekiwanaWersja is { } wersja)
        {
            kontekst.Entry(wkladka).Property(w => w.Version).OriginalValue = wersja;
        }

        return wkladka;
    }

    public Task<WkladkaTeoretyczna?> PobierzDoOdczytuAsync(WkladkaTeoretycznaId id, CancellationToken ct) =>
        kontekst.Set<WkladkaTeoretyczna>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task<WkladkaTeoretyczna?> PobierzOpublikowanaAsync(UmiejetnoscId umiejetnosc, CancellationToken ct) =>
        kontekst.Set<WkladkaTeoretyczna>().AsNoTracking()
            .FirstOrDefaultAsync(w => w.UmiejetnoscId == umiejetnosc && w.Status == StatusWkladki.Opublikowana, ct);

    public void Dodaj(WkladkaTeoretyczna wkladka)
    {
        ArgumentNullException.ThrowIfNull(wkladka);
        kontekst.Add(wkladka);
    }
}
