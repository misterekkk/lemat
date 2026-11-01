// @Role: Pytanie typu NOUL (tak/nie) do Modelu decyzyjnego | @DependsOn: Pytanie, TypPytania | @UsedBy: PlanPytan, FabrykaPlanuOceny (planowane TASK-022) | @Task: TASK-018
namespace Lemat.Domain.Models;

public sealed record PytanieNoul(string Kod, string Tresc) : Pytanie(Kod, Tresc)
{
    public override TypPytania Typ => TypPytania.Noul;

    public override IReadOnlyList<string> DozwoloneOdpowiedzi => ["tak", "nie"];
}
