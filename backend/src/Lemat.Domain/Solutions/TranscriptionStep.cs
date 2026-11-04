// @Role: Krok Transkrypcji (numer 1-40, opis słowny, LaTeX, pewność odczytu) z niezmiennikami CHECK krok_transkrypcji; pola Poprawek Ucznia i Filtra danych osobowych w stanie wyjściowym POC | @DependsOn: Lemat.Domain.Common.NiezmiennikNaruszonyException | @UsedBy: Transkrypcja, konfiguracja EF, handler ROZPOZNAJ_PISMO (planowane TASK-128) | @Task: TASK-126
using Lemat.Domain.Common;

namespace Lemat.Domain.Solutions;

public sealed class KrokTranskrypcji
{
    public const int MaksNr = 40;

    public const int MaksDlugoscTekstu = 2000;

    private KrokTranskrypcji()
    {
    }

    public int Nr { get; private init; }

    /// <summary>Opis słowny kroku.</summary>
    public string Tekst { get; private init; } = string.Empty;

    public string Latex { get; private init; } = string.Empty;

    /// <summary><c>null</c> wyłącznie dla kroku zmienionego przez Ucznia.</summary>
    public decimal? PewnoscOdczytu { get; private init; }

    public bool ZmienionyPrzezUcznia { get; private init; }

    /// <summary>Numer kroku w Transkrypcji agenta, który ten krok poprawia; <c>null</c> = krok wstawiony.</summary>
    public int? NrKrokuAgenta { get; private init; }

    public decimal? OdlegloscEdycyjna { get; private init; }

    public bool PodejrzenieDanychOsobowych { get; private init; }

    public bool TylkoDaneOsobowe { get; private init; }

    /// <summary>Krok odczytany przez Agenta rozpoznającego (wersja 1 Transkrypcji).</summary>
    public static KrokTranskrypcji OdczytanyPrzezAgenta(int nr, string tekst, string latex, decimal pewnoscOdczytu)
    {
        Waliduj(nr, tekst, latex);
        if (pewnoscOdczytu is < 0m or > 1m)
        {
            throw new NiezmiennikNaruszonyException("KROK_PEWNOSC_POZA_ZAKRESEM", "Pewność odczytu musi mieścić się w zakresie 0-1.");
        }

        return new KrokTranskrypcji
        {
            Nr = nr,
            Tekst = tekst,
            Latex = latex,
            PewnoscOdczytu = Math.Round(pewnoscOdczytu, 3, MidpointRounding.ToEven),
            NrKrokuAgenta = nr,
        };
    }

    private static void Waliduj(int nr, string tekst, string latex)
    {
        ArgumentNullException.ThrowIfNull(tekst);
        ArgumentNullException.ThrowIfNull(latex);
        if (nr is < 1 or > MaksNr)
        {
            throw new NiezmiennikNaruszonyException("KROK_NR_POZA_ZAKRESEM", "Numer kroku musi mieścić się w zakresie 1-40.");
        }

        if (tekst.Length > MaksDlugoscTekstu || latex.Length > MaksDlugoscTekstu)
        {
            throw new NiezmiennikNaruszonyException("KROK_ZA_DLUGI", "Tekst i LaTeX kroku mogą mieć najwyżej 2000 znaków.");
        }
    }
}
