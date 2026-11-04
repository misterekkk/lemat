// @Role: DTO wejścia POST /auth/rejestracja (api.md 3, kontrakt POC 3) | @DependsOn: RolaRejestracji | @UsedBy: AccountsEndpoints, RejestracjaWejscieWalidator | @Task: TASK-123
namespace Lemat.Api.Contracts.Accounts;

public sealed record RejestracjaWejscie(
    string Email,
    string Haslo,
    string Imie,
    string Nazwisko,
    RolaRejestracji Rola,
    bool OswiadczenieWieku16,
    bool ZgodaRegulamin,
    string? KodZaproszenia = null);
