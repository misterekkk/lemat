// @Role: Wyliczenie Rodzaj zadania (typ PostgreSQL rodzaj_zadania) | @DependsOn: — | @UsedBy: moduły Content, Solutions, Grading, Calibration | @Task: TASK-014
namespace Lemat.Domain.Common;

public enum RodzajZadania
{
    Zamkniete,
    OtwarteObliczeniowe,
    OtwarteZDowodem,
}
