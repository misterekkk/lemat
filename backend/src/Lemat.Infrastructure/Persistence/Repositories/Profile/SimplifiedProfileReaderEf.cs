// @Role: Czytnik EF Core danych uproszczonego Profilu ucznia: liczba Rozwiązań (bez ANULOWANE/PORZUCONE) i Oceny OSTATECZNA Rozwiązań OCENIONE z Działem Zadania | @DependsOn: LematDbContext, IProfilUproszczonyCzytnik, Rozwiazanie, Ocena, Zadanie | @UsedBy: DI (PersistenceRegistration), PobierzProfilUproszczonyHandler | @Task: TASK-129
using Lemat.Application.Profile;
using Lemat.Domain.Accounts;
using Lemat.Domain.Content;
using Lemat.Domain.Grading;
using Lemat.Domain.Solutions;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Profile;

public sealed class ProfilUproszczonyCzytnikEf(LematDbContext kontekst) : IProfilUproszczonyCzytnik
{
    public async Task<DaneProfiluUproszczonego> PobierzAsync(UczenId uczen, CancellationToken ct)
    {
        var liczba = await kontekst.Set<Rozwiazanie>().AsNoTracking()
            .CountAsync(r => r.UczenId == uczen && r.Status != StatusRozwiazania.Anulowane && r.Status != StatusRozwiazania.Porzucone, ct);

        var wiersze = await (
            from r in kontekst.Set<Rozwiazanie>().AsNoTracking()
            join o in kontekst.Set<Ocena>().AsNoTracking() on r.Id equals o.RozwiazanieId
            join z in kontekst.Set<Zadanie>().AsNoTracking() on r.ZadanieId equals z.Id
            where r.UczenId == uczen && r.Status == StatusRozwiazania.Ocenione && o.Status == StatusOceny.Ostateczna
            select new { r.Id, r.ZadanieId, z.DzialId, o.Punkty, o.PunktyMax, o.CreatedAt, o.Wersja })
            .ToListAsync(ct);

        // Aktualna Ocena = najwyższa Wersja na Rozwiązanie (Z-5: korekta tworzy nową wersję).
        var oceny = wiersze
            .GroupBy(w => w.Id)
            .Select(g => g.OrderByDescending(w => w.Wersja).First())
            .Select(w => new OcenionePozycjaProfilu(w.Id, w.ZadanieId, w.DzialId, w.Punkty, w.PunktyMax, w.CreatedAt))
            .ToList();
        return new DaneProfiluUproszczonego(liczba, oceny);
    }
}
