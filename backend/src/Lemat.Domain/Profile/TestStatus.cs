// @Role: Status Testu diagnostycznego (W_TOKU, ZAKONCZONY, POMINIETY) | @DependsOn: — | @UsedBy: Lemat.Domain.Profile.TestDiagnostyczny, konfiguracja EF | @Task: TASK-019
namespace Lemat.Domain.Profile;

public enum StatusTestu
{
    WToku,
    Zakonczony,
    Pominiety,
}
