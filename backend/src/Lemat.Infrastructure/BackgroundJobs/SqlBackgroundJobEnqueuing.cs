// @Role: Adapter ZlecaniePracSql (ADR-056 p. 2): INSERT … ON CONFLICT DO NOTHING do praca_w_tle w transakcji bieżącej jednostki pracy; deduplikacja OCZEKUJE/W_TOKU po (typ, klucz); NOTIFY wysyła trigger bazy | @DependsOn: IZlecaniePrac, JednostkaPracyEf, KatalogPrac, Npgsql | @UsedBy: PersistenceRegistration, PrzeslijRozwiazanieHandler | @Task: TASK-127
using System.Text.Json;
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Domain.Common;
using Lemat.Infrastructure.Persistence;
using NpgsqlTypes;

namespace Lemat.Infrastructure.BackgroundJobs;

public sealed class ZlecaniePracSql(JednostkaPracyEf jednostkaPracy) : IZlecaniePrac
{
    // Ładunek jsonb: klucze snake_case wg nazw ze słownika (rozwiazanie_id, przeslanie_nr, mock_scenariusz).
    private static readonly JsonSerializerOptions OpcjeLadunku = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private const string Wstaw = """
        INSERT INTO praca_w_tle (typ, pula, klucz_deduplikacji, ladunek, priorytet, maks_prob, dostepna_od)
        VALUES (@typ, @pula, @klucz, @ladunek, @priorytet, @maks_prob, COALESCE(@dostepna_od, now()))
        ON CONFLICT (typ, klucz_deduplikacji)
            WHERE status IN ('OCZEKUJE','W_TOKU') AND klucz_deduplikacji IS NOT NULL
              AND typ NOT IN ('AKTUALIZUJ_PROFIL','SPRAWDZ_POPRAWKI_TRANSKRYPCJI')
        DO NOTHING
        RETURNING id
        """;

    private const string ZnajdzIstniejaca = """
        SELECT id FROM praca_w_tle
        WHERE typ = @typ AND klucz_deduplikacji = @klucz AND status IN ('OCZEKUJE','W_TOKU')
        ORDER BY id LIMIT 1
        """;

    public async Task<RezultatZlecenia> ZlecAsync(ZleceniePracy zlecenie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(zlecenie);
        var opis = KatalogPrac.Pobierz(zlecenie.Typ);
        var typ = KonwencjaNazw.PascalNaUpperSnake(zlecenie.Typ.ToString());

        await using (var wstaw = jednostkaPracy.UtworzPolecenie(Wstaw))
        {
            wstaw.Parameters.AddWithValue("typ", typ);
            wstaw.Parameters.AddWithValue("pula", KonwencjaNazw.PascalNaUpperSnake(opis.Pula.ToString()));
            wstaw.Parameters.Add(new Npgsql.NpgsqlParameter("klucz", NpgsqlDbType.Text) { Value = (object?)zlecenie.KluczDeduplikacji ?? DBNull.Value });
            wstaw.Parameters.Add(new Npgsql.NpgsqlParameter("ladunek", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(zlecenie.Ladunek, OpcjeLadunku) });
            wstaw.Parameters.Add(new Npgsql.NpgsqlParameter("priorytet", NpgsqlDbType.Smallint) { Value = zlecenie.Priorytet ?? opis.Priorytet });
            wstaw.Parameters.Add(new Npgsql.NpgsqlParameter("maks_prob", NpgsqlDbType.Smallint) { Value = opis.MaksProb });
            wstaw.Parameters.Add(new Npgsql.NpgsqlParameter("dostepna_od", NpgsqlDbType.TimestampTz)
            {
                Value = zlecenie.DostepnaOd is { } od ? od.UtcDateTime : DBNull.Value,
            });
            if (await wstaw.ExecuteScalarAsync(ct) is long wstawiona)
            {
                return new RezultatZlecenia(wstawiona, WynikZlecenia.Wstawiona);
            }
        }

        // DO NOTHING nie zwraca wiersza: pracę o tym kluczu zlecono już wcześniej (OCZEKUJE lub W_TOKU).
        await using var szukaj = jednostkaPracy.UtworzPolecenie(ZnajdzIstniejaca);
        szukaj.Parameters.AddWithValue("typ", typ);
        szukaj.Parameters.AddWithValue("klucz", zlecenie.KluczDeduplikacji ?? string.Empty);
        return await szukaj.ExecuteScalarAsync(ct) is long istniejaca
            ? new RezultatZlecenia(istniejaca, WynikZlecenia.Zdeduplikowana)
            : throw new InvalidOperationException("Zlecenie pracy w tle zdeduplikowane, ale istniejącej pracy nie znaleziono.");
    }
}
