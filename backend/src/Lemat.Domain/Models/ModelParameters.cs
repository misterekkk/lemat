// @Role: Wartość Parametry modelu (schemat konfiguracja_modelu.parametry, 13.1): limity, ceny, retencja dostawcy; walidacja zakresów | @DependsOn: ReprezentacjaWzorow, TrybJev, NiezmiennikNaruszonyException | @UsedBy: KonfiguracjaModelu, konwerter jsonb, rejestr adapterów | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Models;

public sealed record ParametryModelu(
    decimal TimeoutS,
    int MaxTokens,
    decimal Temperatura,
    decimal CenaWejsciePln1k,
    decimal CenaWyjsciePln1k,
    int RetencjaDostawcyDni,
    ReprezentacjaWzorow ReprezentacjaWzorow = ReprezentacjaWzorow.Latex,
    int? MaksIteracjiFunkcji = null,
    TrybJev? TrybJev = null,
    string? DostawcaLlm = null)
{
    /// <summary>Parametry bezpieczne dla Mocka i testów (retencja 0, bez kosztów).</summary>
    public static ParametryModelu Domyslne { get; } = new(30m, 2048, 0m, 0m, 0m, 0);

    /// <summary>Sprawdza zakresy; zależności od roli i dostawcy sprawdza <see cref="KonfiguracjaModelu"/>.</summary>
    public void Waliduj()
    {
        if (TimeoutS <= 0m)
        {
            throw new NiezmiennikNaruszonyException("PARAMETRY_MODELU_NIEPRAWIDLOWE", "timeout_s musi być dodatni.");
        }

        if (MaxTokens <= 0)
        {
            throw new NiezmiennikNaruszonyException("PARAMETRY_MODELU_NIEPRAWIDLOWE", "max_tokens musi być dodatni.");
        }

        if (Temperatura is < 0m or > 2m)
        {
            throw new NiezmiennikNaruszonyException("PARAMETRY_MODELU_NIEPRAWIDLOWE", "temperatura musi mieścić się w 0–2.");
        }

        if (CenaWejsciePln1k < 0m || CenaWyjsciePln1k < 0m)
        {
            throw new NiezmiennikNaruszonyException("PARAMETRY_MODELU_NIEPRAWIDLOWE", "Ceny nie mogą być ujemne.");
        }

        if (RetencjaDostawcyDni < 0)
        {
            throw new NiezmiennikNaruszonyException("PARAMETRY_MODELU_NIEPRAWIDLOWE", "retencja_dostawcy_dni nie może być ujemna.");
        }

        if (MaksIteracjiFunkcji is < 1 or > 8)
        {
            throw new NiezmiennikNaruszonyException("PARAMETRY_MODELU_NIEPRAWIDLOWE", "maks_iteracji_funkcji musi mieścić się w 1–8.");
        }
    }
}
