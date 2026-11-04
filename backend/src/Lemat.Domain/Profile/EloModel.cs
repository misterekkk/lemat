// @Role: Czysty model Elo Profilu ucznia (10.1.3, ADR-010): sigma, Umiejętność efektywna, szansa odgadnięcia, prawdopodobieństwo sukcesu P, współczynnik K, przyrost theta, poziom opanowania, odstępy powtórek | @DependsOn: FormaZamknieta | @UsedBy: ProfilUcznia, OpanowanieUmiejetnosci, testy | @Task: TASK-019
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Profile;

public static class ModelElo
{
    /// <summary>Przesunięcie Poziomu opanowania: <c>poziom = sigma(theta - 0.8)</c> = szansa na Zadanie o Poziomie trudności 4.</summary>
    public const double PrzesuniecieMaturalne = 0.8;

    public const decimal WagaUmiejetnosciGlownej = 0.7m;

    public const decimal WagaUmiejetnosciDodatkowych = 0.3m;

    /// <summary>Mnożnik aktualizacji Umiejętności dodatkowej względem głównej.</summary>
    public const decimal MnoznikDodatkowej = 0.5m;

    public const decimal WspolczynnikKTestu = 0.8m;

    public const decimal MinimalnyWspolczynnikK = 0.2m;

    public const int MiejscPoPrzecinku = 3;

    /// <summary>Kolejne odstępy powtórek w dniach (indeks = <c>min(licznik - 1, 4)</c>).</summary>
    public static IReadOnlyList<int> OdstepyPowtorekDni { get; } = [3, 7, 14, 30, 60];

    public static double Sigma(double x) => 1.0 / (1.0 + Math.Exp(-x));

    /// <summary><c>0,7 · theta_główna + 0,3 · średnia(theta_dodatkowe)</c>; bez dodatkowych: <c>theta_główna</c>.</summary>
    public static decimal ThetaEfektywna(decimal thetaGlowna, IReadOnlyList<decimal> thetaDodatkowe)
    {
        ArgumentNullException.ThrowIfNull(thetaDodatkowe);
        return thetaDodatkowe.Count == 0
            ? thetaGlowna
            : (WagaUmiejetnosciGlownej * thetaGlowna) + (WagaUmiejetnosciDodatkowych * thetaDodatkowe.Average());
    }

    /// <summary>Szansa odgadnięcia <c>c</c>: 0,25 dla WYBOR_JEDNOKROTNY, 0,5^n dla PRAWDA_FALSZ z n stwierdzeniami, 0 dla Zadań otwartych.</summary>
    public static decimal SzansaOdgadniecia(FormaZamknieta? forma, int liczbaStwierdzen) => forma switch
    {
        null => 0m,
        FormaZamknieta.WyborJednokrotny => 0.25m,
        FormaZamknieta.PrawdaFalsz => (decimal)Math.Pow(0.5, Math.Max(1, liczbaStwierdzen)),
        _ => throw new ArgumentOutOfRangeException(nameof(forma)),
    };

    /// <summary><c>P = c + (1 - c) · sigma(theta_efektywna - b)</c>.</summary>
    public static double PrawdopodobienstwoSukcesu(decimal thetaEfektywna, decimal trudnoscB, decimal szansaOdgadniecia)
    {
        var c = (double)szansaOdgadniecia;
        return c + ((1.0 - c) * Sigma((double)(thetaEfektywna - trudnoscB)));
    }

    /// <summary><c>K_u = max(0,2; 0,8 / (1 + n/20))</c>; dla Wyników z Testu diagnostycznego stałe 0,8.</summary>
    public static decimal WspolczynnikK(int liczbaWynikow, bool zTestuDiagnostycznego)
    {
        if (zTestuDiagnostycznego)
        {
            return WspolczynnikKTestu;
        }

        return Math.Max(MinimalnyWspolczynnikK, 0.8m / (1m + (Math.Max(0, liczbaWynikow) / 20m)));
    }

    /// <summary>Przyrost <c>theta</c>: <c>waga · mnożnik · K · (s - P)</c>, zaokrąglony do 3 miejsc (precyzja numeric(6,3)).</summary>
    public static decimal PrzyrostTheta(decimal waga, decimal mnoznik, decimal wspolczynnikK, decimal ulamek, double prawdopodobienstwoSukcesu) =>
        Zaokraglij(waga * mnoznik * wspolczynnikK * (ulamek - (decimal)prawdopodobienstwoSukcesu));

    /// <summary><c>poziom_opanowania = sigma(theta - 0,8)</c> zaokrąglony do 3 miejsc.</summary>
    public static decimal PoziomOpanowaniaZTheta(decimal theta) =>
        Zaokraglij((decimal)Sigma((double)theta - PrzesuniecieMaturalne));

    public static decimal Zaokraglij(decimal wartosc) => decimal.Round(wartosc, MiejscPoPrzecinku, MidpointRounding.AwayFromZero);

    /// <summary>Termin kolejnej powtórki po <paramref name="licznikPowtorek"/> udanych powtórkach (&gt;= 1).</summary>
    public static DateTimeOffset NastepnaPowtorka(int licznikPowtorek, DateTimeOffset teraz) =>
        teraz.AddDays(OdstepyPowtorekDni[Math.Min(Math.Max(licznikPowtorek, 1) - 1, OdstepyPowtorekDni.Count - 1)]);
}
