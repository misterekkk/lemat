// @Role: Pytanie typu SCORE (liczba całkowita z przedziału, rozpiętość ≤ 10) do Modelu decyzyjnego | @DependsOn: Pytanie, TypPytania | @UsedBy: PlanPytan, FabrykaPlanuOceny (planowane TASK-022) | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Models;

public sealed record PytanieScore : Pytanie
{
    public const int MaksRozpietosc = 10;

    public PytanieScore(string kod, string tresc, int min, int max)
        : base(kod, tresc)
    {
        if (max < min || max - min > MaksRozpietosc)
        {
            throw new NiezmiennikNaruszonyException("PYTANIE_NIEPRAWIDLOWE", $"Pytanie SCORE wymaga min ≤ max i max − min ≤ {MaksRozpietosc}.");
        }

        Min = min;
        Max = max;
    }

    public int Min { get; }

    public int Max { get; }

    public override TypPytania Typ => TypPytania.Score;

    public override IReadOnlyList<string> DozwoloneOdpowiedzi =>
        [.. Enumerable.Range(Min, Max - Min + 1).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture))];
}
