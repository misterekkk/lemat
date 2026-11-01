// @Role: Konwencja nazw (ADR-049): konwersje PascalCase, snake_case, UPPER_SNAKE, kebab-case | @DependsOn: System.Text.Json.JsonNamingPolicy | @UsedBy: konwencje EF Core (TASK-015), test słownika, serializacja | @Task: TASK-014
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
