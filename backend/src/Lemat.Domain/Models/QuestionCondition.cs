// @Role: Warunek zadania Pytania w Planie pytań: zależność od mody rozkładu pytania nadrzędnego z tego samego planu (głębokość 1, ADR-026) | @DependsOn: — | @UsedBy: PytanieWPlanie, PlanPytan, adaptery Modelu decyzyjnego | @Task: TASK-018
namespace Lemat.Domain.Models;

/// <param name="KodNadrzedny">Pytanie bezwarunkowe z tego samego planu.</param>
/// <param name="Wartosci">Pytanie zadawane, gdy argmax rozkładu nadrzędnego należy do tych wartości.</param>
/// <param name="PominOpcje">Opcje pomijane przy wyznaczaniu argmax (np. ZADEN dla Q-OC-00).</param>
public sealed record WarunekPytania(string KodNadrzedny, IReadOnlyList<string> Wartosci, IReadOnlyList<string> PominOpcje);
