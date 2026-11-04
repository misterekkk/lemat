// @Role: Cel Elementu Ścieżki nauki: NAUKA albo POWTORKA (Powtórka dotyczy wyłącznie Zadania) | @DependsOn: — | @UsedBy: ElementSciezki, PropozycjaElementu, konfiguracja EF | @Task: TASK-019
namespace Lemat.Domain.Recommendations;

public enum CelElementu
{
    Nauka,
    Powtorka,
}
