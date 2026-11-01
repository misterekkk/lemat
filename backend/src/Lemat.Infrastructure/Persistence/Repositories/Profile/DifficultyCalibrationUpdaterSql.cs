// @Role: Atomowa kalibracja Poziomu trudności (10.1.3, ADR-029): jedno polecenie SQL w transakcji jednostki pracy (CTE: FOR UPDATE stary poziom, UPDATE trudnosc_b/poziom z histerezą poziom_z_b, INSERT dziennik_audytu POZIOM_TRUDNOSCI_SKALIBROWANY przy zmianie) | @DependsOn: JednostkaPracyEf, Npgsql, funkcja SQL poziom_z_b | @UsedBy: DI (PersistenceRegistration), ProfilFasada | @Task: TASK-019
using Lemat.Application.Profile;
using Lemat.Domain.Content;
using Npgsql;
using NpgsqlTypes;

namespace Lemat.Infrastructure.Persistence.Repositories.Profile;

public sealed class AktualizacjaKalibracjiTrudnosciSql(JednostkaPracyEf jednostkaPracy) : IAktualizacjaKalibracjiTrudnosci
{
    private const string Sql =
        """
        WITH stary AS (
            SELECT poziom_trudnosci FROM kalibracja_trudnosci WHERE zadanie_id = @zadanie_id FOR UPDATE),
        nowy AS (
            UPDATE kalibracja_trudnosci k
            SET trudnosc_b = k.trudnosc_b - greatest(0.05, 0.4 / (1 + k.liczba_wynikow_kalibracji / 30.0)) * @delta,
                liczba_wynikow_kalibracji = k.liczba_wynikow_kalibracji + 1,
                poziom_trudnosci = CASE WHEN k.liczba_wynikow_kalibracji + 1 < 30 THEN k.poziom_trudnosci
                                        ELSE poziom_z_b(k.trudnosc_b - greatest(0.05, 0.4 / (1 + k.liczba_wynikow_kalibracji / 30.0)) * @delta,
                                                        k.poziom_trudnosci) END,
                updated_at = now()
            WHERE k.zadanie_id = @zadanie_id
            RETURNING k.poziom_trudnosci AS poziom),
        audyt AS (
            INSERT INTO dziennik_audytu (rola, akcja, zasob, zasob_id, dane)
            SELECT NULL, 'POZIOM_TRUDNOSCI_SKALIBROWANY', 'zadanie', @zadanie_id::text,
                   jsonb_build_object('stary', stary.poziom_trudnosci, 'nowy', nowy.poziom)
            FROM stary, nowy
            WHERE stary.poziom_trudnosci <> nowy.poziom
            RETURNING 1)
        SELECT stary.poziom_trudnosci::integer, nowy.poziom::integer FROM stary, nowy
        """;

    public async Task<ZmianaPoziomuTrudnosci?> ZastosujAsync(ZadanieId zadanie, decimal delta, CancellationToken ct)
    {
        await using var polecenie = jednostkaPracy.UtworzPolecenie(Sql);
        polecenie.Parameters.Add(new NpgsqlParameter("zadanie_id", NpgsqlDbType.Uuid) { Value = zadanie.Wartosc });
        polecenie.Parameters.Add(new NpgsqlParameter("delta", NpgsqlDbType.Numeric) { Value = delta });
        await using var czytnik = await polecenie.ExecuteReaderAsync(ct);
        return await czytnik.ReadAsync(ct) ? new ZmianaPoziomuTrudnosci(czytnik.GetInt32(0), czytnik.GetInt32(1)) : null;
    }
}
