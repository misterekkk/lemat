// @Role: Powód unieważnienia (zawieszenia) Wyniku zadania: Ocena zastąpiona albo Zgłoszenie błędu odczytu (8.6, 10.1.3) | @DependsOn: — | @UsedBy: Lemat.Domain.Profile.WynikZadania, konfiguracja EF | @Task: TASK-019
namespace Lemat.Domain.Profile;

public enum PowodUniewaznieniaWyniku
{
    OcenaZastapiona,
    ZgloszenieBleduOdczytu,
}
