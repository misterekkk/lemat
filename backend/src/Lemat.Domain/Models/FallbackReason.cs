// @Role: Wyliczenie Przyczyna użycia Modelu zapasowego (obwód otwarty, ograniczenie lokalne, błąd głównego) | @DependsOn: — | @UsedBy: RezultatPytan, adaptery (planowane TASK-054) | @Task: TASK-018
namespace Lemat.Domain.Models;

public enum PrzyczynaZapasowego
{
    ObwodOtwarty,
    OgraniczenieLokalne,
    BladGlownego,
}
