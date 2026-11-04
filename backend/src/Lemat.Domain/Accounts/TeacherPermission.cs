// @Role: Wyliczenie Uprawnienie Nauczyciela (flagi weryfikator_globalny, recenzent_tresci) | @DependsOn: — | @UsedBy: Lemat.Domain.Accounts.Nauczyciel, zdarzenie UprawnienieOdebrane | @Task: TASK-016
namespace Lemat.Domain.Accounts;

public enum UprawnienieNauczyciela
{
    WeryfikatorGlobalny,
    RecenzentTresci,
}
