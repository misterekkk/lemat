// @Role: Repozytorium EF Core agregatu Ocena: dodawanie, Ocena aktualna (ze śledzeniem i bez) oraz mapowanie numerów Kryteriów na identyfikatory wierszy kryterium_oceniania | @DependsOn: LematDbContext, IOcenaRepozytorium, ZadanieWersja | @UsedBy: DI (PersistenceRegistration), OcenianieFasada | @Task: TASK-126
using Lemat.Application.Grading;
using Lemat.Domain.Content;
using Lemat.Domain.Grading;
using Lemat.Domain.Solutions;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Grading;

public sealed class OcenaRepozytoriumEf(LematDbContext kontekst) : IOcenaRepozytorium
{
    public void Dodaj(Ocena ocena)
    {
        ArgumentNullException.ThrowIfNull(ocena);
        kontekst.Add(ocena);
    }

    public Task<Ocena?> PobierzAktualnaAsync(RozwiazanieId rozwiazanie, CancellationToken ct) =>
        kontekst.Set<Ocena>().AsNoTracking().Include(o => o.Kryteria)
            .FirstOrDefaultAsync(o => o.RozwiazanieId == rozwiazanie && o.Status != StatusOceny.Zastapiona, ct);

    public Task<Ocena?> PobierzAktualnaDoZmianyAsync(RozwiazanieId rozwiazanie, CancellationToken ct) =>
        kontekst.Set<Ocena>().AsTracking().Include(o => o.Kryteria)
            .FirstOrDefaultAsync(o => o.RozwiazanieId == rozwiazanie && o.Status != StatusOceny.Zastapiona, ct);

    public async Task<IReadOnlyDictionary<int, Guid>> PobierzIdentyfikatoryKryteriowAsync(
        ZadanieWersjaId wersja,
        WariantRozwiazania wariant,
        CancellationToken ct)
    {
        var wiersze = await kontekst.Set<ZadanieWersja>().AsNoTracking()
            .Where(w => w.Id == wersja)
            .SelectMany(w => w.KryteriaOceniania)
            .Where(k => k.Wariant == wariant)
            .Select(k => new { k.NrKryterium, Id = EF.Property<Guid>(k, "id") })
            .ToListAsync(ct);
        return wiersze.ToDictionary(w => w.NrKryterium, w => w.Id);
    }
}
