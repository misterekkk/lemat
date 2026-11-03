// @Role: Walidacja LematOpcje przy starcie (Production bez mocków, A-07 retencja, ścieżka magazynu, zakresy) | @DependsOn: LematOpcje, Microsoft.Extensions.Options | @UsedBy: LematKonfiguracja | @Task: TASK-004, TASK-127
using Microsoft.Extensions.Options;

namespace Lemat.Application.Common;

/// <summary>Walidator <see cref="LematOpcje"/> uruchamiany przez <c>ValidateOnStart</c>.</summary>
public sealed class LematOpcjeWalidator : IValidateOptions<LematOpcje>
{
    private static readonly string[] DostawcyModeli =
        [ModeleOpcje.Mock, ModeleOpcje.OpenAiZgodny, ModeleOpcje.AzureOpenAi, ModeleOpcje.Anthropic];

    public ValidateOptionsResult Validate(string? name, LematOpcje opcje)
    {
        ArgumentNullException.ThrowIfNull(opcje);
        var bledy = new List<string>();
        var produkcja = string.Equals(opcje.Srodowisko, "Production", StringComparison.OrdinalIgnoreCase);
        var dostawca = opcje.Modele.Dostawca?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!DostawcyModeli.Contains(dostawca))
        {
            bledy.Add($"Lemat:Modele:Dostawca '{opcje.Modele.Dostawca}' jest nieznany; dozwolone: {string.Join(", ", DostawcyModeli)}.");
        }
        else if (dostawca == ModeleOpcje.Mock)
        {
            if (produkcja)
            {
                bledy.Add("Dostawca modeli 'mock' jest zabroniony w środowisku Production (ADR-065).");
            }
        }
        else
        {
            if (!opcje.Modele.PotwierdzonaRetencjaDostawcy)
            {
                bledy.Add("Dostawca modeli inny niż 'mock' wymaga Lemat:Modele:PotwierdzonaRetencjaDostawcy=true (A-07, BRAK_RETENCJI_DOSTAWCY).");
            }

            if (string.IsNullOrWhiteSpace(opcje.Modele.KluczApi))
            {
                bledy.Add("Dostawca modeli inny niż 'mock' wymaga Lemat:Modele:KluczApi.");
            }

            if (dostawca is ModeleOpcje.OpenAiZgodny or ModeleOpcje.AzureOpenAi && string.IsNullOrWhiteSpace(opcje.Modele.Endpoint))
            {
                bledy.Add($"Dostawca '{dostawca}' wymaga Lemat:Modele:Endpoint.");
            }
        }

        if (!string.Equals(opcje.Magazyn.Typ, MagazynOpcje.Lokalny, StringComparison.OrdinalIgnoreCase))
        {
            bledy.Add($"Lemat:Magazyn:Typ '{opcje.Magazyn.Typ}' jest nieobsługiwany; dozwolone: {MagazynOpcje.Lokalny}.");
        }
        else if (string.IsNullOrWhiteSpace(opcje.Magazyn.Sciezka))
        {
            bledy.Add("Magazyn lokalny wymaga niepustej Lemat:Magazyn:Sciezka (LEMAT_MAGAZYN_SCIEZKA).");
        }

        if (!string.Equals(opcje.Obrazy.Typ, ObrazyOpcje.Prosta, StringComparison.OrdinalIgnoreCase))
        {
            bledy.Add($"Lemat:Obrazy:Typ '{opcje.Obrazy.Typ}' jest nieobsługiwany; dozwolone: {ObrazyOpcje.Prosta}.");
        }

        if (!string.Equals(opcje.Symbolika.Typ, SymbolikaOpcje.Prosta, StringComparison.OrdinalIgnoreCase))
        {
            bledy.Add($"Lemat:Symbolika:Typ '{opcje.Symbolika.Typ}' jest nieobsługiwany; dozwolone: {SymbolikaOpcje.Prosta}.");
        }

        if (!string.Equals(opcje.Email.Typ, EmailOpcje.Log, StringComparison.OrdinalIgnoreCase))
        {
            bledy.Add($"Lemat:Email:Typ '{opcje.Email.Typ}' jest nieobsługiwany; dozwolone: {EmailOpcje.Log}.");
        }

        if (produkcja && opcje.Konta.AutozatwierdzanieNauczycieli)
        {
            bledy.Add("Lemat:Konta:AutozatwierdzanieNauczycieli jest dozwolone wyłącznie poza Production.");
        }

        if (produkcja && string.IsNullOrWhiteSpace(opcje.Magazyn.Klucz))
        {
            bledy.Add("Production wymaga Lemat:Magazyn:Klucz (LEMAT_MAGAZYN_KLUCZ).");
        }

        if (produkcja && string.IsNullOrWhiteSpace(opcje.Kursor.Klucz))
        {
            bledy.Add("Production wymaga Lemat:Kursor:Klucz (LEMAT_KURSOR_KLUCZ).");
        }

        if (produkcja && string.IsNullOrWhiteSpace(opcje.Jwt.Klucz))
        {
            bledy.Add("Production wymaga Lemat:Jwt:Klucz (LEMAT_JWT_KLUCZ).");
        }

        SprawdzZakresy(opcje, bledy);

        return bledy.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(bledy);
    }

    private static void SprawdzZakresy(LematOpcje opcje, List<string> bledy)
    {
        Dodatnie(opcje.Zdjecia.MaksNaPrzeslanie, "Zdjecia:MaksNaPrzeslanie", bledy);
        Dodatnie(opcje.Zdjecia.MaksRozmiarMb, "Zdjecia:MaksRozmiarMb", bledy);
        Dodatnie(opcje.Zdjecia.MaksPrzeslanNaRozwiazanie, "Zdjecia:MaksPrzeslanNaRozwiazanie", bledy);
        Dodatnie(opcje.Zdjecia.MaksPonowienRozpoznania, "Zdjecia:MaksPonowienRozpoznania", bledy);
        Dodatnie(opcje.Transkrypcja.MaksKrokow, "Transkrypcja:MaksKrokow", bledy);
        Dodatnie(opcje.Ocenianie.MaksParQoc05, "Ocenianie:MaksParQoc05", bledy);
        Dodatnie(opcje.Ocenianie.TerminMiekkiMinimumSekund, "Ocenianie:TerminMiekkiMinimumSekund", bledy);
        Dodatnie(opcje.Ocenianie.TerminMiekkiBazaSekund, "Ocenianie:TerminMiekkiBazaSekund", bledy);
        Dodatnie(opcje.Ocenianie.TerminTwardySekund, "Ocenianie:TerminTwardySekund", bledy);
        Dodatnie(opcje.Ocenianie.TimeoutAgentaWyjasniajacegoSekund, "Ocenianie:TimeoutAgentaWyjasniajacegoSekund", bledy);
        Dodatnie(opcje.Strumien.HeartbeatSekund, "Strumien:HeartbeatSekund", bledy);
        Dodatnie(opcje.Jwt.WaznoscMinut, "Jwt:WaznoscMinut", bledy);
        Dodatnie(opcje.Jwt.OdswiezenieDni, "Jwt:OdswiezenieDni", bledy);

        if (opcje.Zdjecia.DozwoloneTypyMime.Count == 0)
        {
            bledy.Add("Lemat:Zdjecia:DozwoloneTypyMime nie może być puste.");
        }

        Ulamek(opcje.Transkrypcja.ProgSprawdzOdczyt, "Transkrypcja:ProgSprawdzOdczyt", bledy);
        Ulamek(opcje.Transkrypcja.TauOdczyt, "Transkrypcja:TauOdczyt", bledy);
        Ulamek(opcje.Transkrypcja.TauFr25, "Transkrypcja:TauFr25", bledy);
        Ulamek(opcje.Progi.OdsetekKontroliLosowej, "Progi:OdsetekKontroliLosowej", bledy);
        Ulamek(opcje.Modele.MockMasaModeluDecyzyjnego, "Modele:MockMasaModeluDecyzyjnego", bledy);

        // Zakres progu 0.800–0.990 (11.5); 1.000 oznacza tryb pełnej weryfikacji.
        ZakresProgu(opcje.Progi.PoczatkowyJev, "Progi:PoczatkowyJev", bledy);
        ZakresProgu(opcje.Progi.PoczatkowyZapasowy, "Progi:PoczatkowyZapasowy", bledy);

        if (opcje.Modele.MockOpoznienieMs < 0)
        {
            bledy.Add("Lemat:Modele:MockOpoznienieMs nie może być ujemne.");
        }
    }

    private static void Dodatnie(int wartosc, string klucz, List<string> bledy)
    {
        if (wartosc <= 0)
        {
            bledy.Add($"Lemat:{klucz} musi być dodatnie.");
        }
    }

    private static void Ulamek(double wartosc, string klucz, List<string> bledy)
    {
        if (double.IsNaN(wartosc) || wartosc < 0 || wartosc > 1)
        {
            bledy.Add($"Lemat:{klucz} musi mieścić się w przedziale [0, 1].");
        }
    }

    private static void ZakresProgu(double wartosc, string klucz, List<string> bledy)
    {
        if (double.IsNaN(wartosc) || wartosc < 0.8 || wartosc > 1.0)
        {
            bledy.Add($"Lemat:{klucz} musi mieścić się w przedziale [0.800, 1.000].");
        }
    }
}
