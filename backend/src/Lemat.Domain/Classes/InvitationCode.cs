// @Role: Wartość Kod zaproszenia Klasy (8 znaków z alfabetu [A-HJ-NP-Z2-9], ważny 14 dni) i sprawdzenie ważności | @DependsOn: NiezmiennikNaruszonyException | @UsedBy: Lemat.Domain.Classes.Klasa, GeneratorKoduZaproszenia, KlasyFasada | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Classes;

public readonly record struct KodZaproszenia
{
    /// <summary>Alfabet bez znaków mylących się (I, O, 0, 1): <c>[A-HJ-NP-Z2-9]</c>.</summary>
    public const string Alfabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public const int Dlugosc = 8;

    /// <summary>Czas ważności nowo wygenerowanego kodu (FR-30).</summary>
    public static readonly TimeSpan CzasWaznosci = TimeSpan.FromDays(14);

    public KodZaproszenia(string wartosc, DateTimeOffset waznyDo)
    {
        Wartosc = Normalizuj(wartosc);
        if (!CzyPoprawnaPostac(Wartosc))
        {
            throw new NiezmiennikNaruszonyException("KOD_ZAPROSZENIA_NIEPRAWIDLOWY", "Kod zaproszenia ma 8 znaków z alfabetu A-H, J-N, P-Z, 2-9.");
        }

        WaznyDo = waznyDo;
    }

    public string Wartosc { get; }

    public DateTimeOffset WaznyDo { get; }

    public static string Normalizuj(string? kod) => (kod ?? string.Empty).Trim().ToUpperInvariant();

    public static bool CzyPoprawnaPostac(string kod) =>
        kod.Length == Dlugosc && kod.All(znak => Alfabet.Contains(znak, StringComparison.Ordinal));

    /// <summary>Kod jest ważny, gdy zgadza się z zapisanym i nie minął <see cref="WaznyDo"/>.</summary>
    public bool CzyWazny(string? kod, DateTimeOffset teraz) =>
        string.Equals(Normalizuj(kod), Wartosc, StringComparison.Ordinal) && teraz < WaznyDo;

    /// <summary>Unieważnienie: ten sam kod, ale ważny do chwili <paramref name="teraz"/> (czyli już nieważny).</summary>
    public KodZaproszenia Uniewazniony(DateTimeOffset teraz) => new(Wartosc, teraz);
}
