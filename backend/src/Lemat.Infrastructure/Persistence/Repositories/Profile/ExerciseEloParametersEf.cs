// @Role: Adapter IParametryZadaniaDlaProfilu nad tabelami Treści: Umiejętność główna i dodatkowe, trudnosc_b z kalibracji (lub początkowe 0,8·(poziom-3)) i szansa odgadnięcia z formy Zadania zamkniętego | @DependsOn: LematDbContext, Zadanie, KalibracjaTrudnosci, ModelElo | @UsedBy: DI (PersistenceRegistration), ProfilFasada | @Task: TASK-019
using Lemat.Application.Profile;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Profile;

public sealed class ParametryZadaniaDlaProfiluEf(LematDbContext kontekst) : IParametryZadaniaDlaProfilu
{
    public async Task<IReadOnlyDictionary<ZadanieId, ParametryEloZadania>> PobierzAsync(IReadOnlyCollection<ZadanieId> zadania, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(zadania);
        if (zadania.Count == 0)
        {
            return new Dictionary<ZadanieId, ParametryEloZadania>();
        }

        var identyfikatory = zadania.ToList();
        var znalezione = await kontekst.Set<Zadanie>()
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Include(z => z.Umiejetnosci)
            .Include(z => z.Wersje.Where(w => w.ZastapionaAt == null))
            .Where(z => identyfikatory.Contains(z.Id))
            .ToListAsync(ct);
        var kalibracje = (await kontekst.Set<KalibracjaTrudnosci>()
                .AsNoTracking()
                .Where(k => identyfikatory.Contains(k.ZadanieId))
                .ToListAsync(ct))
            .ToDictionary(k => k.ZadanieId);

        return znalezione.ToDictionary(
            z => z.Id,
            z =>
            {
                kalibracje.TryGetValue(z.Id, out var kalibracja);
                var trudnoscB = kalibracja?.TrudnoscB ?? 0.8m * (z.PoziomTrudnosciNominalny.Wartosc - 3);
                var liczbaStwierdzen = z.AktualnaWersja.OpcjeZamkniete?.Stwierdzenia.Count ?? 0;
                return new ParametryEloZadania(
                    z.UmiejetnoscGlowna,
                    z.UmiejetnosciDodatkowe,
                    trudnoscB,
                    ModelElo.SzansaOdgadniecia(z.FormaZamknieta, liczbaStwierdzen));
            });
    }
}
