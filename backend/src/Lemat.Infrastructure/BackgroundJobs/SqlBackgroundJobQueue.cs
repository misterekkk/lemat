// @Role: Adapter KolejkaPracSql (13.4, uproszczony U-26): atomowe pobranie pracy z dzierżawą (UPDATE … FOR UPDATE SKIP LOCKED), zakończenie, ponowienie przez funkcję ponow_prace (limit prób → MARTWA) i odzyskiwanie dzierżaw; krótkie polecenia w autocommit na NpgsqlDataSource, bez LISTEN/NOTIFY | @DependsOn: IKolejkaPrac, NpgsqlDataSource, KonwencjaNazw | @UsedBy: PersistenceRegistration, ProcesorPrac | @Task: TASK-128
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Domain.Common;
using Npgsql;
using NpgsqlTypes;

namespace Lemat.Infrastructure.BackgroundJobs;

public sealed class KolejkaPracSql(NpgsqlDataSource zrodlo) : IKolejkaPrac
{
    // Jedno polecenie wybiera i blokuje wiersz (SKIP LOCKED) oraz przyznaje dzierżawę — dwa workery nie pobiorą tej samej pracy.
    private const string Pobierz = """
        UPDATE praca_w_tle p
        SET status = 'W_TOKU', proby = p.proby + 1, wykonawca = @wykonawca,
            dzierzawa_do = now() + make_interval(secs => @dzierzawa_s), updated_at = now()
        WHERE p.id = (
            SELECT k.id FROM praca_w_tle k
            WHERE k.status = 'OCZEKUJE' AND k.dostepna_od <= now() AND k.typ = ANY(@typy)
            ORDER BY k.priorytet, k.dostepna_od, k.id
            FOR UPDATE SKIP LOCKED
            LIMIT 1)
        RETURNING p.id, p.typ, p.ladunek::text, p.proby, p.maks_prob
        """;

    private const string Zakoncz = """
        UPDATE praca_w_tle
        SET status = 'ZAKONCZONA', wykonawca = NULL, dzierzawa_do = NULL, updated_at = now()
        WHERE id = @id AND wykonawca = @wykonawca AND status = 'W_TOKU'
        """;

    public async Task<PracaWTle?> PobierzNastepnaAsync(IReadOnlyCollection<TypPracy> typy, string wykonawca, TimeSpan dzierzawa, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(typy);
        ArgumentException.ThrowIfNullOrWhiteSpace(wykonawca);
        if (typy.Count == 0)
        {
            return null;
        }

        await using var polecenie = zrodlo.CreateCommand(Pobierz);
        polecenie.Parameters.AddWithValue("wykonawca", wykonawca);
        polecenie.Parameters.AddWithValue("dzierzawa_s", dzierzawa.TotalSeconds);
        polecenie.Parameters.Add(new NpgsqlParameter("typy", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = typy.Select(t => KonwencjaNazw.PascalNaUpperSnake(t.ToString())).ToArray(),
        });
        await using var czytnik = await polecenie.ExecuteReaderAsync(ct);
        if (!await czytnik.ReadAsync(ct))
        {
            return null;
        }

        return new PracaWTle(
            czytnik.GetInt64(0),
            Enum.Parse<TypPracy>(KonwencjaNazw.UpperSnakeNaPascal(czytnik.GetString(1))),
            czytnik.GetString(2),
            czytnik.GetInt16(3),
            czytnik.GetInt16(4));
    }

    public async Task<bool> ZakonczAsync(long pracaId, string wykonawca, CancellationToken ct)
    {
        await using var polecenie = zrodlo.CreateCommand(Zakoncz);
        polecenie.Parameters.AddWithValue("id", pracaId);
        polecenie.Parameters.AddWithValue("wykonawca", wykonawca);
        return await polecenie.ExecuteNonQueryAsync(ct) == 1;
    }

    public async Task<StatusPracy?> PonowAsync(long pracaId, string wykonawca, string blad, TimeSpan odstep, CancellationToken ct)
    {
        await using var polecenie = zrodlo.CreateCommand("SELECT ponow_prace(@id, @wykonawca, @blad, make_interval(secs => @odstep_s))::text");
        polecenie.Parameters.AddWithValue("id", pracaId);
        polecenie.Parameters.AddWithValue("wykonawca", wykonawca);
        polecenie.Parameters.AddWithValue("blad", blad);
        polecenie.Parameters.AddWithValue("odstep_s", odstep.TotalSeconds);
        return await polecenie.ExecuteScalarAsync(ct) is string status
            ? Enum.Parse<StatusPracy>(KonwencjaNazw.UpperSnakeNaPascal(status))
            : null;
    }

    public async Task<int> OdzyskajDzierzawyAsync(CancellationToken ct)
    {
        await using var polecenie = zrodlo.CreateCommand("SELECT count(*) FROM odzyskaj_dzierzawy(100)");
        return Convert.ToInt32(await polecenie.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
    }
}
