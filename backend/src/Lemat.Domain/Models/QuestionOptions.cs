// @Role: Wartość Opcje pytania zapisywana z Decyzją modelu (CHOICE: lista opcji, SCORE: zakres, NOUL: brak) | @DependsOn: Pytanie, PytanieChoice, PytanieScore | @UsedBy: DecyzjaModelu, konwerter jsonb | @Task: TASK-018
namespace Lemat.Domain.Models;

/// <summary>Opcje pytania zapisane razem z decyzją (CHOICE: lista opcji, SCORE: zakres, NOUL: brak).</summary>
public sealed record OpcjePytania(IReadOnlyList<string>? Lista, int? Min, int? Max)
{
    public static OpcjePytania? Z(Pytanie pytanie) => pytanie switch
    {
        PytanieChoice choice => new OpcjePytania(choice.Opcje, null, null),
        PytanieScore skala => new OpcjePytania(null, skala.Min, skala.Max),
        _ => null,
    };
}
