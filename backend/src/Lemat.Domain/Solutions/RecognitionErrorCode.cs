// @Role: Kod błędu rozpoznawania pisma zapisany w Rozwiązaniu (kolumna text UPPER_SNAKE, CHECK w DDL) | @DependsOn: — | @UsedBy: Rozwiazanie, konfiguracja EF, handler ROZPOZNAJ_PISMO (planowane TASK-128) | @Task: TASK-126
namespace Lemat.Domain.Solutions;

public enum KodBleduRozpoznawania
{
    Nieczytelne,
    PrzekroczonyCzas,
    BladDostawcy,
    ZbytWieleKrokow,
}
