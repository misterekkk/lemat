// @Role: Rekord Aktywna kalibracja – para Migawka progu + Migawka kalibratora dla (Konfiguracja modelu, Rodzaj zadania) | @DependsOn: MigawkaProgu, MigawkaKalibratora | @UsedBy: IKalibracjaFasada, Agent oceniający (planowane TASK-022) | @Task: TASK-018
using Lemat.Domain.Calibration;

namespace Lemat.Application.Calibration;

public sealed record AktywnaKalibracja(MigawkaProgu Prog, MigawkaKalibratora Kalibrator);
