// @Role: Wyliczenie Źródło kalibratora (typ PostgreSQL zrodlo_kalibratora; rozłączne ze Źródłem progu) | @DependsOn: — | @UsedBy: KalibratorPewnosci, konfiguracja EF | @Task: TASK-018
namespace Lemat.Domain.Calibration;

public enum ZrodloKalibratora
{
    EwaluacjaZt1,
    KalibracjaAutomatyczna,
}
