// @Role: DTO flag Użytkownika w sesji (weryfikator_globalny, recenzent_tresci; dla Ucznia zawsze false) | @DependsOn: — | @UsedBy: UzytkownikSesji | @Task: TASK-123
namespace Lemat.Api.Contracts.Accounts;

public sealed record FlagiUzytkownika(bool WeryfikatorGlobalny, bool RecenzentTresci);
