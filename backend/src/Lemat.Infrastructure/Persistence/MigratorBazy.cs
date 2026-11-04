// @Role: Stosuje migracje EF Core (pliki SQL) i dopilnowuje partycji decyzja_modelu na zadany horyzont (U-08) | @DependsOn: LematDbContext, SchematPoczatkowy, Npgsql, TimeProvider | @UsedBy: Lemat.Tools.Commands.MigrujBazeCommand, Lemat.Infrastructure.Tests | @Task: TASK-013
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Lemat.Infrastructure.Persistence;

/// <summary>Idempotentne wykonanie migracji i utworzenie partycji tabeli <c>decyzja_modelu</c>.</summary>
public static class MigratorBazy
{
    /// <summary>Liczba miesięcy naprzód, dla których istnieją partycje po migracji (U-08).</summary>
    public const int MiesiecyNaprzod = 12;

    public static DbContextOptions<LematDbContext> Opcje(string polaczenie)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(polaczenie);
        return new DbContextOptionsBuilder<LematDbContext>()
            .UseNpgsql(polaczenie, o => o.MigrationsHistoryTable(LematDbContext.TabelaHistoriiMigracji))
            .UseSnakeCaseNamingConvention()
            .Options;
    }

    /// <summary>
    /// Stosuje oczekujące migracje, a potem wywołuje <c>utworz_partycje_decyzji</c> dla bieżącego miesiąca i
    /// <see cref="MiesiecyNaprzod"/> kolejnych. Ponowne wywołanie niczego nie zmienia (migracje zapisane w historii,
    /// partycje <c>IF NOT EXISTS</c>). Zwraca liczbę zastosowanych migracji.
    /// </summary>
    public static async Task<int> MigrujAsync(string polaczenie, TimeProvider zegar, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(zegar);
        await using var kontekst = new LematDbContext(Opcje(polaczenie));
        var oczekujace = (await kontekst.Database.GetPendingMigrationsAsync(token)).Count();
        await kontekst.Database.MigrateAsync(token);
        await UtworzPartycjeAsync(polaczenie, zegar.GetUtcNow(), MiesiecyNaprzod, token);
        return oczekujace;
    }

    public static async Task UtworzPartycjeAsync(string polaczenie, DateTimeOffset teraz, int miesiecyNaprzod, CancellationToken token = default)
    {
        await using var zrodlo = NpgsqlDataSource.Create(polaczenie);
        await using var polecenie = zrodlo.CreateCommand("SELECT utworz_partycje_decyzji(@miesiac)");
        var parametr = polecenie.Parameters.Add(new NpgsqlParameter("miesiac", NpgsqlDbType.Date));
        var pierwszy = new DateOnly(teraz.UtcDateTime.Year, teraz.UtcDateTime.Month, 1);
        for (var m = 0; m <= miesiecyNaprzod; m++)
        {
            parametr.Value = pierwszy.AddMonths(m);
            await polecenie.ExecuteNonQueryAsync(token);
        }
    }
}
