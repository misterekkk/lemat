// @Role: Repozytorium EF Core agregatu Rozwiązanie: dodawanie, odczyt ze Zdjęciami (ze śledzeniem i bez), nieukończone Rozwiązanie Ucznia przy Zadaniu | @DependsOn: LematDbContext, IRozwiazanieRepozytorium | @UsedBy: DI (PersistenceRegistration), RozwiazaniaFasada | @Task: TASK-126
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
