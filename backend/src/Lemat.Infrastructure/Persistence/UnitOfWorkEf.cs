// @Role: Implementacja IJednostkaPracy na EF Core: jawna transakcja READ COMMITTED, SaveChanges, Commit, tłumaczenie błędów bazy; ta sama transakcja dla EF i poleceń Npgsql (ADR-056) | @DependsOn: LematDbContext, TlumaczBledowBazy, Lemat.Application.Common.IJednostkaPracy | @UsedBy: przypadki użycia, ZlecaniePracSql (TASK-024), PersistenceRegistration | @Task: TASK-015
using System.Data;
using Lemat.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Lemat.Infrastructure.Persistence;

public sealed class JednostkaPracyEf(LematDbContext kontekst) : IJednostkaPracy
{
    public bool TransakcjaOtwarta => kontekst.Database.CurrentTransaction is not null;

    public void WymagajTransakcji()
    {
        if (!TransakcjaOtwarta)
        {
            throw new InvalidOperationException(IJednostkaPracy.KodPozaJednostkaPracy);
        }
    }

    public async Task WykonajAsync(Func<CancellationToken, Task> przypadekUzycia, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(przypadekUzycia);
        await WykonajAsync<object?>(async token =>
        {
            await przypadekUzycia(token);
            return null;
        }, ct);
    }

    public async Task<T> WykonajAsync<T>(Func<CancellationToken, Task<T>> przypadekUzycia, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(przypadekUzycia);
        if (TransakcjaOtwarta)
        {
            // Zagnieżdżone wywołanie: transakcję, zapis i zatwierdzenie prowadzi wywołanie zewnętrzne.
            return await przypadekUzycia(ct);
        }

        await using var transakcja = await kontekst.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var wynik = await przypadekUzycia(ct);
            await kontekst.SaveChangesAsync(ct);
            await transakcja.CommitAsync(ct);
            return wynik;
        }
        catch (Exception e)
        {
            kontekst.ChangeTracker.Clear();
            try
            {
                await transakcja.RollbackAsync(CancellationToken.None);
            }
            catch (Exception) when (e is not null)
            {
                // Wycofanie po błędzie połączenia: pierwotny wyjątek jest ważniejszy.
            }

            var docelowy = TlumaczBledowBazy.Przetlumacz(e);
            if (docelowy is not null)
            {
                throw docelowy;
            }

            throw;
        }
    }

    /// <summary>Polecenie Npgsql na połączeniu i w transakcji bieżącej jednostki pracy (np. <c>INSERT … ON CONFLICT</c> prac w tle).</summary>
    public NpgsqlCommand UtworzPolecenie(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        WymagajTransakcji();
        var polaczenie = (NpgsqlConnection)kontekst.Database.GetDbConnection();
        var transakcja = (NpgsqlTransaction)kontekst.Database.CurrentTransaction!.GetDbTransaction();
        return new NpgsqlCommand(sql, polaczenie, transakcja);
    }
}
