// @Role: Typ błędu Ucznia w Ocenie (typ PostgreSQL typ_bledu); błąd odczytu nie jest typem błędu Ucznia (FR-28) | @DependsOn: — | @UsedBy: Ocena, konfiguracja EF, MapowanieTypowWyliczeniowych | @Task: TASK-126
namespace Lemat.Domain.Grading;

public enum TypBledu
{
    Rachunkowy,
    Merytoryczny,
    NiepelneRozwiazanie,
}
