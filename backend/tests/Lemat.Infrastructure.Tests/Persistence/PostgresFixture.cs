// @Role: Fixture testów integracyjnych: PostgreSQL 16 z Testcontainers albo lokalny serwer z LEMAT_TEST_BAZA; każda baza testowa jest świeża i pusta | @DependsOn: Testcontainers.PostgreSql, Npgsql, xUnit v3 | @UsedBy: Lemat.Infrastructure.Tests.Persistence | @Task: TASK-013
using Npgsql;
using Testcontainers.PostgreSql;

namespace Lemat.Infrastructure.Tests.Persistence;

/// <summary>
/// Dostarcza puste bazy PostgreSQL 16. Gdy ustawiona jest zmienna <c>LEMAT_TEST_BAZA</c> (connection string z prawem
/// <c>CREATEDB</c>, np. <c>Host=localhost;Port=5433;Database=lemat_test;Username=lemat;Password=lemat_dev</c>), testy używają
/// tego serwera; w przeciwnym razie uruchamiany jest kontener Testcontainers (wymaga Dockera).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string ZmiennaSrodowiskowa = "LEMAT_TEST_BAZA";

    private readonly List<string> _utworzoneBazy = [];
    private PostgreSqlContainer? _kontener;
    private string _administracyjne = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var zmienna = Environment.GetEnvironmentVariable(ZmiennaSrodowiskowa);
        if (!string.IsNullOrWhiteSpace(zmienna))
        {
            _administracyjne = zmienna;
            return;
        }

        _kontener = new PostgreSqlBuilder("postgres:16").Build();
        await _kontener.StartAsync();
        _administracyjne = _kontener.GetConnectionString();
    }

    /// <summary>Tworzy nową, pustą bazę (bez tabel, bez danych) i zwraca jej connection string.</summary>
    public async Task<string> UtworzPustaBazeAsync()
    {
        var nazwa = $"lemat_t_{Guid.NewGuid():N}";
        await using (var zrodlo = NpgsqlDataSource.Create(_administracyjne))
        {
            await using var polecenie = zrodlo.CreateCommand($"CREATE DATABASE \"{nazwa}\"");
            await polecenie.ExecuteNonQueryAsync();
        }

        lock (_utworzoneBazy)
        {
            _utworzoneBazy.Add(nazwa);
        }

        return new NpgsqlConnectionStringBuilder(_administracyjne) { Database = nazwa, Pooling = false }.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            string[] bazy;
            lock (_utworzoneBazy)
            {
                bazy = [.. _utworzoneBazy];
            }

            if (bazy.Length > 0 && !string.IsNullOrEmpty(_administracyjne))
            {
                await using var zrodlo = NpgsqlDataSource.Create(_administracyjne);
                foreach (var nazwa in bazy)
                {
                    await using var polecenie = zrodlo.CreateCommand($"DROP DATABASE IF EXISTS \"{nazwa}\" WITH (FORCE)");
                    await polecenie.ExecuteNonQueryAsync();
                }
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

/// <summary>Kolekcja współdzielona przez testy bazy (jeden kontener na przebieg).</summary>
[CollectionDefinition(Nazwa)]
public sealed class PostgresKolekcjaTestow : ICollectionFixture<PostgresFixture>
{
    public const string Nazwa = "postgres";
}
