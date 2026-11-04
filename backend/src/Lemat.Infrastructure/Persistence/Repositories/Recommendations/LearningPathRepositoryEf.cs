// @Role: Repozytorium EF Core agregatu Ścieżka nauki: wczytanie z filtrowanym Include elementów PROPONOWANY/ROZPOCZETY (+ wskazany element), If-Match, 10 ostatnich elementów osobnym zapytaniem (ef-core.md 6.5, C6-15 p. 4) | @DependsOn: LematDbContext, ISciezkaNaukiRepozytorium | @UsedBy: DI (PersistenceRegistration), RekomendacjeFasada | @Task: TASK-019
using Lemat.Application.Recommendations;
using Lemat.Domain.Accounts;
using Lemat.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Recommendations;

public sealed class SciezkaNaukiRepozytoriumEf(LematDbContext kontekst) : ISciezkaNaukiRepozytorium
{
    public async Task<SciezkaNauki?> PobierzAsync(UczenId uczen, ElementSciezkiId? dodatkowyElement, int? oczekiwanaWersja, CancellationToken ct)
    {
        var maDodatkowy = dodatkowyElement.HasValue;
        var dodatkowyId = dodatkowyElement.GetValueOrDefault();
        var sciezka = await kontekst.Set<SciezkaNauki>()
            .AsTracking()
            .Include(s => s.AktywneElementy.Where(e =>
                e.Status == StatusElementuSciezki.Proponowany
                || e.Status == StatusElementuSciezki.Rozpoczety
                || (maDodatkowy && e.Id == dodatkowyId)))
            .FirstOrDefaultAsync(s => s.UczenId == uczen, ct);
        if (sciezka is not null && oczekiwanaWersja is { } wersja)
        {
            kontekst.Entry(sciezka).Property(s => s.Version).OriginalValue = wersja;
        }

        return sciezka;
    }

    public Task<SciezkaNauki?> PobierzDoOdczytuAsync(UczenId uczen, CancellationToken ct) =>
        kontekst.Set<SciezkaNauki>()
            .AsNoTracking()
            .Include(s => s.AktywneElementy.Where(e =>
                e.Status == StatusElementuSciezki.Proponowany || e.Status == StatusElementuSciezki.Rozpoczety))
            .FirstOrDefaultAsync(s => s.UczenId == uczen, ct);

    public async Task<IReadOnlyList<ElementSciezki>> OstatnieElementyAsync(UczenId uczen, int liczba, CancellationToken ct) =>
        await kontekst.Set<SciezkaNauki>()
            .AsNoTracking()
            .Where(s => s.UczenId == uczen)
            .SelectMany(s => s.AktywneElementy)
            .OrderByDescending(e => e.Kolejnosc)
            .Take(Math.Max(1, liczba))
            .ToListAsync(ct);

    public void Dodaj(SciezkaNauki sciezka)
    {
        ArgumentNullException.ThrowIfNull(sciezka);
        kontekst.Add(sciezka);
    }
}
