// @Role: Reguła Rekomendacji (R-PRZYPISANIE, R-SERIA, ... 10.1) i jej kod tekstowy w kolumnie rekomendacja.regula | @DependsOn: — | @UsedBy: Rekomendacja, konfiguracja EF, szablony uzasadnień (planowane TASK-040) | @Task: TASK-019
using System.Collections.Frozen;

namespace Lemat.Domain.Recommendations;

public enum RegulaRekomendacji
{
    Przypisanie,
    Seria,
    Powtorka,
    Wkladka,
    WkladkaDiag,
    Najslabsza,
    TrudnoscWGore,
    Utrwalenie,
    JevWkladka,
    JevZadanieLatwe,
    JevZadanieMaturalne,
    JevPowtorka,
}

public static class KodyRegulRekomendacji
{
    private static readonly FrozenDictionary<RegulaRekomendacji, string> NaKod = new Dictionary<RegulaRekomendacji, string>
    {
        [RegulaRekomendacji.Przypisanie] = "R-PRZYPISANIE",
        [RegulaRekomendacji.Seria] = "R-SERIA",
        [RegulaRekomendacji.Powtorka] = "R-POWTORKA",
        [RegulaRekomendacji.Wkladka] = "R-WKLADKA",
        [RegulaRekomendacji.WkladkaDiag] = "R-WKLADKA-DIAG",
        [RegulaRekomendacji.Najslabsza] = "R-NAJSLABSZA",
        [RegulaRekomendacji.TrudnoscWGore] = "R-TRUDNOSC_W_GORE",
        [RegulaRekomendacji.Utrwalenie] = "R-UTRWALENIE",
        [RegulaRekomendacji.JevWkladka] = "R-JEV-WKLADKA",
        [RegulaRekomendacji.JevZadanieLatwe] = "R-JEV-ZADANIE_LATWE",
        [RegulaRekomendacji.JevZadanieMaturalne] = "R-JEV-ZADANIE_MATURALNE",
        [RegulaRekomendacji.JevPowtorka] = "R-JEV-POWTORKA",
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, RegulaRekomendacji> ZKodu = NaKod.ToFrozenDictionary(p => p.Value, p => p.Key);

    public static string Kod(RegulaRekomendacji regula) => NaKod[regula];

    public static RegulaRekomendacji Parsuj(string kod) =>
        ZKodu.TryGetValue(kod, out var regula) ? regula : throw new ArgumentOutOfRangeException(nameof(kod), kod, "Nieznana reguła Rekomendacji.");
}
