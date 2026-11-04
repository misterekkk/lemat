// @Role: Repozytorium EF Core agregatu Rozwiązanie: dodawanie, odczyt ze Zdjęciami (ze śledzeniem i bez), lista Rozwiązań Ucznia (kursor), nieukończone Rozwiązanie Ucznia przy Zadaniu | @DependsOn: LematDbContext, IRozwiazanieRepozytorium | @UsedBy: DI (PersistenceRegistration), RozwiazaniaFasada | @Task: TASK-126, TASK-127
using Lemat.Application.Solutions;
using Lemat.Domain.Accounts;
using Lemat.Domain.Content;
using Lemat.Domain.Solutions;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Solutions;

public sealed class RozwiazanieRepozytoriumEf(LematDbContext kontekst) : IRozwiazanieRepozytorium
{
    private static readonly StatusRozwiazania[] StatusyWTrakcie =
    [
        StatusRozwiazania.Przeslane,
        StatusRozwiazania.Rozpoznawanie,
        StatusRozwiazania.TranskrypcjaGotowa,
        StatusRozwiazania.Zatwierdzone,
        StatusRozwiazania.Ocenianie,
        StatusRozwiazania.OczekujeNaWeryfikacje,
    ];

    public void Dodaj(Rozwiazanie rozwiazanie)
    {
        ArgumentNullException.ThrowIfNull(rozwiazanie);
        kontekst.Add(rozwiazanie);
    }

    public Task<Rozwiazanie?> PobierzAsync(RozwiazanieId id, CancellationToken ct) =>
        kontekst.Set<Rozwiazanie>().AsTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Rozwiazanie?> PobierzDoOdczytuAsync(RozwiazanieId id, CancellationToken ct) =>
        kontekst.Set<Rozwiazanie>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Rozwiazanie>> ListaDlaUczniaAsync(
        UczenId uczen,
        StatusRozwiazania? status,
        PozycjaListyRozwiazan? po,
        int limit,
        CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        IQueryable<Rozwiazanie> Baza() => kontekst.Set<Rozwiazanie>().AsNoTracking()
            .Where(r => r.UczenId == uczen && (status == null || r.Status == status));

        var wynik = new List<Rozwiazanie>();
        if (po is not null)
        {
            // Remisy created_at == kursor: porządek id jak w PostgreSQL (bajty UUID = porządek tekstu UUID); wierszy jest kilka.
            var remisy = await Baza().Where(r => r.CreatedAt == po.CreatedAt).ToListAsync(ct);
            wynik.AddRange(remisy
                .Where(r => string.CompareOrdinal(r.Id.Wartosc.ToString(), po.Id.Wartosc.ToString()) > 0)
                .OrderBy(r => r.Id.Wartosc.ToString(), StringComparer.Ordinal));
        }

        var pozostalo = limit - wynik.Count;
        if (pozostalo > 0)
        {
            var zapytanie = Baza();
            if (po is not null)
            {
                zapytanie = zapytanie.Where(r => r.CreatedAt < po.CreatedAt);
            }

            wynik.AddRange(await zapytanie.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id).Take(pozostalo).ToListAsync(ct));
        }

        return [.. wynik.Take(limit)];
    }

    public async Task<RozwiazanieId?> ZnajdzNieukonczoneAsync(UczenId uczen, ZadanieId zadanie, CancellationToken ct)
    {
        var id = await kontekst.Set<Rozwiazanie>().AsNoTracking()
            .Where(r => r.UczenId == uczen && r.ZadanieId == zadanie && StatusyWTrakcie.Contains(r.Status))
            .OrderBy(r => r.CreatedAt)
            .Select(r => (RozwiazanieId?)r.Id)
            .FirstOrDefaultAsync(ct);
        return id;
    }
}
