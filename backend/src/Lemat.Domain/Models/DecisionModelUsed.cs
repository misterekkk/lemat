// @Role: Wyliczenie Model decyzyjny użyty (główny albo zapasowy) w Rezultacie pytań – metryki i uzasadnienie 11.7 | @DependsOn: — | @UsedBy: RezultatPytan, adaptery (planowane TASK-054) | @Task: TASK-018
namespace Lemat.Domain.Models;

public enum ModelDecyzyjnyUzyty
{
    Glowny,
    Zapasowy,
}
