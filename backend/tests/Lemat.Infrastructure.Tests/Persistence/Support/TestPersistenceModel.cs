// @Role: Model testowy dla LematDbContext: agregat testowy z identyfikatorem, enumami PostgreSQL, jsonb, owned JSON i tokenem version oraz fabryka kontekstu z interceptorami (bez encji produkcyjnych) | @DependsOn: LematDbContext, WersjaAgregatuInterceptor, ZnacznikiCzasuInterceptor, MapowanieTypowWyliczeniowych, MigratorBazy | @UsedBy: Lemat.Infrastructure.Tests.Persistence | @Task: TASK-015
using System.Text.Json;
using Lemat.Domain.Common;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Conventions;
using Lemat.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence.Support;

public readonly record struct TestowyAgregatId(Guid Wartosc) : IIdentyfikator<Guid>;

public sealed record DaneTestowe(string PoleDlugieZeZnakami, int Liczba);

public sealed class WpisTestowy
{
    public string Tresc { get; set; } = string.Empty;
}

public sealed class TestowyAgregat : Agregat<TestowyAgregatId>
{
    private TestowyAgregat()
    {
    }

    public string Nazwa { get; set; } = string.Empty;

    public PoziomMatury Poziom { get; set; }

    public RodzajZadania Rodzaj { get; set; }

    public DaneTestowe? Dane { get; set; }

    public JsonDocument? Dokument { get; set; }

    [BezZmianyWersji]
    public int Licznik { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<WpisTestowy> Wpisy { get; } = [];

    public static TestowyAgregat Nowy(string nazwa) => new()
    {
        Id = new TestowyAgregatId(GeneratorUuid.Nowy(TimeProvider.System)),
        Nazwa = nazwa,
        Poziom = PoziomMatury.Podstawowy,
        Rodzaj = RodzajZadania.Zamkniete,
    };
}

internal sealed class TestowyAgregatConfiguration : IEntityTypeConfiguration<TestowyAgregat>
{
    public void Configure(EntityTypeBuilder<TestowyAgregat> b)
    {
        b.ToTable("test_agregat");
        b.HasKey(a => a.Id);
        b.Ignore(a => a.Zdarzenia);
        b.Property(a => a.Version).IsConcurrencyToken();
        b.Property(a => a.Poziom).HasColumnType("poziom_matury");
        b.Property(a => a.Rodzaj).HasColumnType("rodzaj_zadania");
        b.Property(a => a.Dane!).HasJsonbRecord();
        b.Property(a => a.Dokument!).HasJsonbDocument();
        b.OwnsManyJsonb(a => a.Wpisy, "wpisy");
    }
}

/// <summary>Dokłada encje testowe do modelu <see cref="LematDbContext"/> po <c>OnModelCreating</c>.</summary>
internal sealed class ModelTestowyCustomizer(ModelCustomizerDependencies zaleznosci) : RelationalModelCustomizer(zaleznosci)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        modelBuilder.ApplyConfiguration(new TestowyAgregatConfiguration());
    }
}

public sealed class ZegarTestowy(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Teraz { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Teraz;
}

/// <summary>Migrowana baza z tabelą testową i fabryka kontekstów testowych.</summary>
public sealed class BazaTestowa : IAsyncDisposable
{
    private readonly NpgsqlDataSource _zrodlo;
    private readonly ZegarTestowy _zegar = new(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));

    static BazaTestowa() => KonwersjeIdentyfikatorow.DodajZespol(typeof(TestowyAgregatId).Assembly);

    private BazaTestowa(string polaczenie)
    {
        Polaczenie = polaczenie;
        _zrodlo = new NpgsqlDataSourceBuilder(polaczenie).MapujTypyWyliczeniowe().EnableDynamicJson().Build();
        Zegar = _zegar;
        Wersja = new WersjaAgregatuInterceptor();
        Czas = new ZnacznikiCzasuInterceptor(_zegar);
    }

    public string Polaczenie { get; }

    public NpgsqlDataSource Zrodlo => _zrodlo;

    public ZegarTestowy Zegar { get; }

    private WersjaAgregatuInterceptor Wersja { get; }

    private ZnacznikiCzasuInterceptor Czas { get; }

    public static async Task<BazaTestowa> UtworzAsync(PostgresFixture postgres)
    {
        var polaczenie = await postgres.UtworzPustaBazeAsync();
        await MigratorBazy.MigrujAsync(polaczenie, TimeProvider.System);
        var baza = new BazaTestowa(polaczenie);
        await baza.WykonajAsync(
            """
            CREATE TABLE test_agregat (
                id uuid PRIMARY KEY,
                nazwa text NOT NULL,
                poziom poziom_matury NOT NULL,
                rodzaj rodzaj_zadania NOT NULL,
                dane jsonb,
                dokument jsonb,
                wpisy jsonb,
                licznik integer NOT NULL DEFAULT 0,
                version integer NOT NULL,
                created_at timestamptz NOT NULL DEFAULT now(),
                updated_at timestamptz NOT NULL DEFAULT now()
            );
            CREATE SCHEMA test_s;
            CREATE TABLE test_s.unikalna (kod text NOT NULL, CONSTRAINT ux_jeden_proponowany UNIQUE (kod), CONSTRAINT ux_nieznany_w_katalogu UNIQUE (kod));
            """);
        return baza;
    }

    public LematDbContext Kontekst()
    {
        var opcje = new DbContextOptionsBuilder<LematDbContext>()
            .UseNpgsql(_zrodlo, o => o.MapujTypyWyliczenioweEf())
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCustomizer, ModelTestowyCustomizer>()
            .AddInterceptors(Wersja, Czas)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        return new LematDbContext(opcje);
    }

    public async Task WykonajAsync(string sql)
    {
        await using var polecenie = _zrodlo.CreateCommand(sql);
        await polecenie.ExecuteNonQueryAsync();
    }

    public async Task<T> SkalarAsync<T>(string sql)
    {
        await using var polecenie = _zrodlo.CreateCommand(sql);
        return (T)(await polecenie.ExecuteScalarAsync())!;
    }

    public async ValueTask DisposeAsync() => await _zrodlo.DisposeAsync();
}
