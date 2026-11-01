// @Role: Konwencja nazw (ADR-049): konwersje PascalCase, snake_case, UPPER_SNAKE, kebab-case | @DependsOn: System.Text.Json.JsonNamingPolicy | @UsedBy: konwencje EF Core (TASK-015), test słownika, serializacja | @Task: TASK-014, TASK-121
using System.Text;
using System.Text.Json;

namespace Lemat.Domain.Common;

public static class KonwencjaNazw
{
    public static string PascalNaSnake(string pascal) => JsonNamingPolicy.SnakeCaseLower.ConvertName(Wymagaj(pascal));

    public static string PascalNaUpperSnake(string pascal) => JsonNamingPolicy.SnakeCaseUpper.ConvertName(Wymagaj(pascal));

    public static string PascalNaKebab(string pascal) => JsonNamingPolicy.KebabCaseLower.ConvertName(Wymagaj(pascal));

    public static string SnakeNaPascal(string snake) => ZTokenow(Wymagaj(snake).Split('_', StringSplitOptions.RemoveEmptyEntries));

    public static string UpperSnakeNaPascal(string upperSnake) => SnakeNaPascal(Wymagaj(upperSnake));

    public static string KebabNaPascal(string kebab) => ZTokenow(Wymagaj(kebab).Split('-', StringSplitOptions.RemoveEmptyEntries));

    public static string SnakeNaUpperSnake(string snake) => Wymagaj(snake).ToUpperInvariant();

    public static string UpperSnakeNaSnake(string upperSnake) => Wymagaj(upperSnake).ToLowerInvariant();

    public static string SnakeNaKebab(string snake) => Wymagaj(snake).Replace('_', '-');

    public static string KebabNaSnake(string kebab) => Wymagaj(kebab).Replace('-', '_');

    /// <summary>Tokeny, w których cyfry są częścią oznaczenia, a nie liczbą (3.1 p. 6): bez separatora przed cyfrą.</summary>
    public static readonly IReadOnlySet<string> TokenyZCyframi = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "zt1", "cv5", "sha256", "fnv1a", "argon2id", "uuid5", "uuid7", "es256", "s3",
    };

    /// <summary>
    /// Działa na wyniku <c>SnakeCaseLower</c>/<c>SnakeCaseUpper</c>: wstawia <c>_</c> między literą a liczbą
    /// (<c>wieku16</c> → <c>wieku_16</c>), poza tokenami z <see cref="TokenyZCyframi"/> (ADR-049 p. 14, ADR-067 p. 3).
    /// </summary>
    public static string RozdzielCyfry(string snake)
    {
        ArgumentNullException.ThrowIfNull(snake);
        var tokeny = snake.Split('_');
        for (var i = 0; i < tokeny.Length; i++)
        {
            tokeny[i] = TokenyZCyframi.Contains(tokeny[i]) ? tokeny[i] : WstawSeparatorPrzedCyfra(tokeny[i]);
        }

        return string.Join('_', tokeny);
    }

    private static string WstawSeparatorPrzedCyfra(string token)
    {
        var wynik = new StringBuilder(token.Length + 2);
        for (var i = 0; i < token.Length; i++)
        {
            if (i > 0 && char.IsAsciiDigit(token[i]) && char.IsAsciiLetter(token[i - 1]))
            {
                wynik.Append('_');
            }

            wynik.Append(token[i]);
        }

        return wynik.ToString();
    }

    private static string ZTokenow(string[] tokeny)
    {
        var wynik = new StringBuilder();
        foreach (var token in tokeny)
        {
            wynik.Append(char.ToUpperInvariant(token[0])).Append(token.AsSpan(1).ToString().ToLowerInvariant());
        }

        return wynik.ToString();
    }

    private static string Wymagaj(string wartosc)
    {
        ArgumentException.ThrowIfNullOrEmpty(wartosc);
        return wartosc;
    }
}
