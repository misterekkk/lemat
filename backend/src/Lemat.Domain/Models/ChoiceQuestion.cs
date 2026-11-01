// @Role: Pytanie typu CHOICE (2–42 unikalne opcje) do Modelu decyzyjnego | @DependsOn: Pytanie, TypPytania | @UsedBy: PlanPytan, FabrykaPlanuOceny (planowane TASK-022) | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Models;

public sealed record PytanieChoice : Pytanie
{
    public const int MinOpcji = 2;
    public const int MaksOpcji = 42;

    public PytanieChoice(string kod, string tresc, IReadOnlyList<string> opcje)
        : base(kod, tresc)
    {
        ArgumentNullException.ThrowIfNull(opcje);
        if (opcje.Count is < MinOpcji or > MaksOpcji || opcje.Distinct(StringComparer.Ordinal).Count() != opcje.Count || opcje.Any(string.IsNullOrWhiteSpace))
        {
            throw new NiezmiennikNaruszonyException("PYTANIE_NIEPRAWIDLOWE", $"Pytanie CHOICE wymaga {MinOpcji}–{MaksOpcji} unikalnych, niepustych opcji.");
        }

        Opcje = [.. opcje];
    }

    public IReadOnlyList<string> Opcje { get; }

    public override TypPytania Typ => TypPytania.Choice;

    public override IReadOnlyList<string> DozwoloneOdpowiedzi => Opcje;
}
