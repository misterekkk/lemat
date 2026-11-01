// @Role: Abstrakcyjny rekord Pytanie do Modelu decyzyjnego: kod, treść, typ i dozwolone odpowiedzi (kontrakt portu, ADR-026) | @DependsOn: TypPytania | @UsedBy: PytanieNoul, PytanieScore, PytanieChoice, PlanPytan, FabrykaPlanuOceny (planowane TASK-022) | @Task: TASK-018
namespace Lemat.Domain.Models;

public abstract record Pytanie(string Kod, string Tresc)
{
    public abstract TypPytania Typ { get; }

    public abstract IReadOnlyList<string> DozwoloneOdpowiedzi { get; }
}
