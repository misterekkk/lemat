// @Role: DbContext Lemat (Npgsql, snake_case): konwencje modelu (identyfikatory silnie typowane, timestamptz, text), konfiguracje encji z zespołu, migracje SQL (ADR-063); DbSet-y niepubliczne | @DependsOn: Microsoft.EntityFrameworkCore, EFCore.NamingConventions, KonwersjeIdentyfikatorow | @UsedBy: Lemat.Infrastructure.Persistence.MigratorBazy, JednostkaPracyEf, PersistenceRegistration, repozytoria (TASK-016+) | @Task: TASK-013, TASK-015
using Lemat.Infrastructure.Persistence.Conventions;
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
        configurationBuilder.Properties<string>().AreUnicode();
        KonwersjeIdentyfikatorow.Zarejestruj(configurationBuilder);
    }
}
