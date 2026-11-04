// @Role: Wyliczenie Rola użytkownika (typ PostgreSQL rola_uzytkownika) | @DependsOn: — | @UsedBy: Lemat.Domain.Accounts.Uzytkownik, autoryzacja, Lemat.Application.Accounts | @Task: TASK-016
namespace Lemat.Domain.Accounts;

public enum RolaUzytkownika
{
    Uczen,
    Nauczyciel,
    Administrator,
}
