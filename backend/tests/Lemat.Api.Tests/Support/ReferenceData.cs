// @Role: Dane referencyjne testów API: 13 Działów z jawnymi id 1…13 (zgodnie z seed-poc), wstawiane idempotentnie przez INSERT … ON CONFLICT DO NOTHING | @DependsOn: Npgsql, BazaTestowaFixture | @UsedBy: Accounts/UczenEndpointsTests, Content/SectionsEndpointsTests | @Task: TASK-124
using Npgsql;

namespace Lemat.Api.Tests.Support;

public static class DaneReferencyjne
{
    public static readonly IReadOnlyList<(int Id, string Kod, string Nazwa)> Dzialy =
    [
        (1, "I", "Liczby rzeczywiste"),
        (2, "II", "Wyrażenia algebraiczne"),
        (3, "III", "Równania i nierówności"),
        (4, "IV", "Układy równań"),
        (5, "V", "Funkcje"),
        (6, "VI", "Ciągi"),
        (7, "VII", "Trygonometria"),
        (8, "VIII", "Planimetria"),
        (9, "IX", "Geometria analityczna na płaszczyźnie kartezjańskiej"),
        (10, "X", "Stereometria"),
        (11, "XI", "Kombinatoryka"),
        (12, "XII", "Rachunek prawdopodobieństwa i statystyka"),
        (13, "XIII", "Optymalizacja i rachunek różniczkowy"),
    ];

    public static async Task ZapewnijDzialyAsync(string polaczenie, CancellationToken ct)
    {
        await using var zrodlo = NpgsqlDataSource.Create(polaczenie);
        foreach (var (id, kod, nazwa) in Dzialy)
        {
            await using var polecenie = zrodlo.CreateCommand(
                "INSERT INTO dzial (id, kod, nazwa, tylko_rozszerzony, kolejnosc) VALUES (@id, @kod, @nazwa, @rozszerzony, @kolejnosc) ON CONFLICT DO NOTHING");
            polecenie.Parameters.AddWithValue("id", id);
            polecenie.Parameters.AddWithValue("kod", kod);
            polecenie.Parameters.AddWithValue("nazwa", nazwa);
            polecenie.Parameters.AddWithValue("rozszerzony", id == 13);
            polecenie.Parameters.AddWithValue("kolejnosc", (short)id);
            await polecenie.ExecuteNonQueryAsync(ct);
        }
    }
}
