// @Role: Repozytorium EF Core Tokenów odświeżania: dodanie, wyszukanie po skrócie, masowe unieważnienie rodziny i konta jednym UPDATE (bez śledzenia zmian) | @DependsOn: LematDbContext, ITokenOdswiezaniaRepozytorium | @UsedBy: DI (PersistenceRegistration), uwierzytelnianie (TASK-072) | @Task: TASK-016, TASK-123
using Lemat.Application.Accounts;
using Lemat.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Accounts;

public sealed class TokenOdswiezaniaRepozytoriumEf(LematDbContext kontekst) : ITokenOdswiezaniaRepozytorium
{
    public void Dodaj(TokenOdswiezania token)
    {
        ArgumentNullException.ThrowIfNull(token);
        kontekst.Add(token);
    }

    public Task<TokenOdswiezania?> PobierzPoSkrocieAsync(byte[] tokenHash, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);
        return kontekst.Set<TokenOdswiezania>().AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
    }

    public async Task<TokenOdswiezania?> PobierzDoZmianyAsync(byte[] tokenHash, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);
        var wiersze = await kontekst.Set<TokenOdswiezania>()
            .FromSqlInterpolated($"SELECT * FROM token_odswiezania WHERE token_hash = {tokenHash} FOR UPDATE")
            .AsTracking()
            .ToListAsync(ct);
        return wiersze.SingleOrDefault();
    }

    public Task<int> UniewaznRodzineAsync(Guid rodzinaId, DateTimeOffset teraz, CancellationToken ct) =>
        kontekst.Set<TokenOdswiezania>()
            .Where(t => t.RodzinaId == rodzinaId && t.UniewaznionyAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UniewaznionyAt, teraz), ct);

    public Task<int> UniewaznWszystkieAsync(UzytkownikId uzytkownik, DateTimeOffset teraz, CancellationToken ct) =>
        kontekst.Set<TokenOdswiezania>()
            .Where(t => t.UzytkownikId == uzytkownik && t.UniewaznionyAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UniewaznionyAt, teraz), ct);
}
