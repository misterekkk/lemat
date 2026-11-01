// @Role: Minimalny DbContext (Npgsql, snake_case); w TASK-013 bez encji — nosi wyłącznie migracje SQL (ADR-063), mapowania agregatów dochodzą w TASK-015+ | @DependsOn: Microsoft.EntityFrameworkCore, EFCore.NamingConventions | @UsedBy: Lemat.Infrastructure.Persistence.MigratorBazy, Lemat.Infrastructure.Persistence.Migrations.SchematPoczatkowy | @Task: TASK-013
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence;

/// <summary>
/// Kontekst bazy Lemat. Schemat tworzą wyłącznie migracje wykonujące pliki SQL wycięte z <c>docs/spec/ddl.md</c>;
/// model EF Core jest mapowaniem bez generowania schematu (ADR-063).
/// </summary>
public sealed class LematDbContext(DbContextOptions<LematDbContext> opcje) : DbContext(opcje)
{
    /// <summary>Tabela historii migracji (ef-core.md 1).</summary>
    public const string TabelaHistoriiMigracji = "__historia_migracji";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasPostgresExtension("citext").HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LematDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
        configurationBuilder.Properties<DateOnly>().HaveColumnType("date");
    }
}
