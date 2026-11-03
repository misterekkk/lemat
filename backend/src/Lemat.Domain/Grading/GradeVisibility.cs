// @Role: Widoczność Oceny dla Ucznia (pole wyliczane, bez kolumny): pełna albo wstrzymana przy podejrzeniu błędu odczytu | @DependsOn: — | @UsedBy: Ocena, OcenaWyjscie (planowane TASK-128) | @Task: TASK-126
namespace Lemat.Domain.Grading;

public enum WidocznoscOcenyDlaUcznia
{
    Pelna,
    WstrzymanaBladOdczytu,
}
