// @Role: Repozytorium EF Core agregatu Test diagnostyczny (z Pozycjami): wczytanie ze śledzeniem, odczyt bez śledzenia, test W_TOKU/ZAKONCZONY na poziom matury | @DependsOn: LematDbContext, ITestDiagnostycznyRepozytorium | @UsedBy: DI (PersistenceRegistration), ProfilFasada | @Task: TASK-019
using Lemat.Application.Profile;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Profile;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Profile;

public sealed class TestDiagnostycznyRepozytoriumEf(LematDbContext kontekst) : ITestDiagnostycznyRepozytorium
{
    public Task<TestDiagnostyczny?> PobierzAsync(TestDiagnostycznyId id, CancellationToken ct) =>
        kontekst.Set<TestDiagnostyczny>().AsTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<TestDiagnostyczny?> PobierzDoOdczytuAsync(TestDiagnostycznyId id, CancellationToken ct) =>
        kontekst.Set<TestDiagnostyczny>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<TestDiagnostyczny?> PobierzAktywnyLubZakonczonyAsync(UczenId uczen, PoziomMatury poziom, CancellationToken ct) =>
        kontekst.Set<TestDiagnostyczny>().AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.UczenId == uczen && t.PoziomMatury == poziom && (t.Status == StatusTestu.WToku || t.Status == StatusTestu.Zakonczony),
                ct);

    public void Dodaj(TestDiagnostyczny test)
    {
        ArgumentNullException.ThrowIfNull(test);
        kontekst.Add(test);
    }
}
