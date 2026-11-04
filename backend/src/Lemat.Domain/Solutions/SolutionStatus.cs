// @Role: Status Rozwiązania (typ PostgreSQL status_rozwiazania, maszyna stanów 8.1); w POC przejścia obejmują 7 pierwszych wartości, pozostałe istnieją w typie | @DependsOn: — | @UsedBy: Rozwiazanie, konfiguracja EF, MapowanieTypowWyliczeniowych | @Task: TASK-126
namespace Lemat.Domain.Solutions;

public enum StatusRozwiazania
{
    Przeslane,
    Rozpoznawanie,
    BladRozpoznawania,
    TranskrypcjaGotowa,
    Zatwierdzone,
    Ocenianie,
    OczekujeNaWeryfikacje,
    Ocenione,
    Porzucone,
    Anulowane,
}
