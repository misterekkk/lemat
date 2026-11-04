// @Role: Fixture zestawu testów API: pusta baza PostgreSQL 16 z zastosowanymi migracjami (Testcontainers albo serwer z LEMAT_TEST_BAZA), connection string w LEMAT_BAZA | @DependsOn: Testcontainers.PostgreSql, Npgsql, MigratorBazy | @UsedBy: Lemat.Api.Tests (AssemblyFixture) | @Task: TASK-121
using Lemat.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Lemat.Api.Tests.Support.BazaTestowaFixture))]

namespace Lemat.Api.Tests.Support;

/// <summary>Baza współdzielona przez testy API; hosty czytają ją ze zmiennej <c>LEMAT_BAZA</c> (ustawianej tu na czas testów).</summary>
public sealed class BazaTestowaFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _kontener;
    private string _administracyjne = string.Empty;
    private string? _nazwaBazy;

    public string Polaczenie { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var zmienna = Environment.GetEnvironmentVariable("LEMAT_TEST_BAZA");
        if (!string.IsNullOrWhiteSpace(zmienna))
        {
            _administracyjne = zmienna;
        }
        else
        {
            _kontener = new PostgreSqlBuilder("postgres:16").Build();
            await _kontener.StartAsync();
            _administracyjne = _kontener.GetConnectionString();
        }

        _nazwaBazy = $"lemat_api_t_{Guid.NewGuid():N}";
        await using (var zrodlo = NpgsqlDataSource.Create(_administracyjne))
        {
            await using var polecenie = zrodlo.CreateCommand($"CREATE DATABASE \"{_nazwaBazy}\"");
            await polecenie.ExecuteNonQueryAsync();
        }

        Polaczenie = new NpgsqlConnectionStringBuilder(_administracyjne) { Database = _nazwaBazy, Pooling = false }.ConnectionString;
        await MigratorBazy.MigrujAsync(Polaczenie, TimeProvider.System);
        Environment.SetEnvironmentVariable("LEMAT_BAZA", Polaczenie);
    }

    public async ValueTask DisposeAsync()
    {
        Environment.SetEnvironmentVariable("LEMAT_BAZA", null);
        try
        {
            if (_nazwaBazy is not null && !string.IsNullOrEmpty(_administracyjne))
            {
                NpgsqlConnection.ClearAllPools();
                await using var zrodlo = NpgsqlDataSource.Create(_administracyjne);
                await using var polecenie = zrodlo.CreateCommand($"DROP DATABASE IF EXISTS \"{_nazwaBazy}\" WITH (FORCE)");
                await polecenie.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (_kontener is not null)
            {
                await _kontener.DisposeAsync();
            }
        }
    }
}
