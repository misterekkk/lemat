// @Role: Pozycja Planu pytań: Pytanie z opcjonalnym Warunkiem (null = bezwarunkowe) | @DependsOn: Pytanie, WarunekPytania | @UsedBy: PlanPytan, adaptery Modelu decyzyjnego | @Task: TASK-018
namespace Lemat.Domain.Models;

public sealed record PytanieWPlanie(Pytanie Pytanie, WarunekPytania? Warunek);
