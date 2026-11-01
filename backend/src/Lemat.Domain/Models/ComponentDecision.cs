// @Role: Wartość Decyzja składowa (odpowiedź Modelu decyzyjnego na jedno Pytanie): wybór (moda) i rozkład prawdopodobieństw; walidacja sumy 1 ± 0,001 | @DependsOn: TypPytania, NiezmiennikNaruszonyException | @UsedBy: RezultatPytan, DecyzjaModelu, ZasadyOceniania (planowane TASK-022), adaptery | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Models;

/// <param name="Kod">Kod pytania z planu.</param>
/// <param name="Wybor">"tak"/"nie", "0".."n" albo opcja (moda rozkładu).</param>
/// <param name="Rozklad">Rozkład prawdopodobieństw po odpowiedziach; suma 1 ± 0,001.</param>
/// <param name="PWyboru">Prawdopodobieństwo wyboru = <c>Rozklad[Wybor]</c>.</param>
public sealed record DecyzjaSkladowa(string Kod, TypPytania Typ, string Wybor, IReadOnlyDictionary<string, decimal> Rozklad, decimal PWyboru)
{
    public const decimal TolerancjaSumy = 0.001m;

    /// <summary>Tworzy decyzję z rozkładu: <c>Wybor</c> = moda (przy remisie pierwsza w kolejności rozkładu), <c>PWyboru</c> = jej masa.</summary>
    public static DecyzjaSkladowa ZRozkladu(string kod, TypPytania typ, IReadOnlyDictionary<string, decimal> rozklad)
    {
        ArgumentNullException.ThrowIfNull(rozklad);
        WalidujRozklad(rozklad);
        var moda = rozklad.MaxBy(p => p.Value);
        return new DecyzjaSkladowa(kod, typ, moda.Key, rozklad, moda.Value);
    }

    /// <summary>Rozkład niepusty, prawdopodobieństwa w [0,1], suma 1 ± 0,001 (kod <c>ROZKLAD_NIEPRAWIDLOWY</c>).</summary>
    public static void WalidujRozklad(IReadOnlyDictionary<string, decimal> rozklad)
    {
        ArgumentNullException.ThrowIfNull(rozklad);
        if (rozklad.Count == 0 || rozklad.Values.Any(p => p is < 0m or > 1m) || Math.Abs(rozklad.Values.Sum() - 1m) > TolerancjaSumy)
        {
            throw new NiezmiennikNaruszonyException("ROZKLAD_NIEPRAWIDLOWY", "Rozkład prawdopodobieństw musi mieć sumę 1 ± 0,001 i wartości w przedziale 0–1.");
        }
    }
}
