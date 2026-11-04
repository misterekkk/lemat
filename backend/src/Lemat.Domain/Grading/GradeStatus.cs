// @Role: Status Oceny (typ PostgreSQL status_oceny): wstępna, ostateczna, zastąpiona | @DependsOn: — | @UsedBy: Ocena, konfiguracja EF, MapowanieTypowWyliczeniowych | @Task: TASK-126
namespace Lemat.Domain.Grading;

public enum StatusOceny
{
    Wstepna,
    Ostateczna,
    Zastapiona,
}
