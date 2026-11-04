// @Role: Rola dozwolona przy rejestracji (bez ADMINISTRATOR, ADR-071 p. 4); w POC obsługiwany jest tylko UCZEN (U-21) | @DependsOn: — | @UsedBy: RejestracjaWejscie, RejestracjaWejscieWalidator | @Task: TASK-123
namespace Lemat.Api.Contracts.Accounts;

public enum RolaRejestracji
{
    Uczen,
    Nauczyciel,
}
