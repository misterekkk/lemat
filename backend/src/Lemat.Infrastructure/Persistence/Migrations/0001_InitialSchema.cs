// @Role: Migracja początkowa SchematPoczatkowy — wykonuje wyłącznie pliki SQL 0000…0007 wycięte z docs/spec/ddl.md (brak ModelSnapshot; ADR-063) | @DependsOn: ZasobSql, LematDbContext, Persistence/Migrations/Sql/*.sql | @UsedBy: Lemat.Infrastructure.Persistence.MigratorBazy | @Task: TASK-013
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lemat.Infrastructure.Persistence.Migrations;

/// <summary>Pełne DDL jedną migracją (U-01). Identyfikator <c>0001_SchematPoczatkowy</c> trafia do tabeli historii migracji (7.7).</summary>
[DbContext(typeof(LematDbContext))]
[Migration("0001_SchematPoczatkowy")]
public sealed class SchematPoczatkowy : Migration
{
    /// <summary>Pliki SQL w kolejności wykonywania (blok A, potem blok B).</summary>
    public static readonly IReadOnlyList<string> PlikiSql =
    [
        "0000_schema.sql",
        "0001_functions.sql",
        "0002_invariant_triggers.sql",
        "0003_notifications.sql",
        "0004_partitions.sql",
        "0005_permissions.sql",
        "0006_background_jobs.sql",
        "0007_api.sql",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        foreach (var plik in PlikiSql)
        {
            migrationBuilder.Sql(ZasobSql.Wczytaj(plik));
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Migracje wyłącznie w przód (7.7 p. 3); reset pustej bazy: scripts/db-reset.sh.");
}
