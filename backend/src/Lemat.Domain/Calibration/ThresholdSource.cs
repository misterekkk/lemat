// @Role: Wyliczenie Źródło progu (typ PostgreSQL zrodlo_progu): ewaluacja ZT-1, kalibracja automatyczna, Administrator, Bezpiecznik | @DependsOn: — | @UsedBy: ProgPewnosci, konfiguracja EF | @Task: TASK-018
namespace Lemat.Domain.Calibration;

public enum ZrodloProgu
{
    EwaluacjaZt1,
    KalibracjaAutomatyczna,
    Administrator,
    Bezpiecznik,
}
