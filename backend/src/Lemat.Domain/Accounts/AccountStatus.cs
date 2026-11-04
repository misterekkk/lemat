// @Role: Wyliczenie Status konta (typ PostgreSQL status_konta; 8.x) | @DependsOn: — | @UsedBy: Lemat.Domain.Accounts.Uzytkownik, OkresKarencji, Lemat.Application.Accounts | @Task: TASK-016
namespace Lemat.Domain.Accounts;

public enum StatusKonta
{
    Aktywne,
    OczekujeNaZatwierdzenie,
    Zablokowane,
    Usuwane,
}
