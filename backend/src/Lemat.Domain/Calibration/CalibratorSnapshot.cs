// @Role: Rekord Migawka kalibratora – niezmienny odczyt Kalibratora pewności (Id, metoda, punkty funkcji) przekazywany regułom oceniania; Kalibruj przelicza pewność surową | @DependsOn: KalibratorPewnosci, PunktyKalibratora | @UsedBy: IKalibracjaFasada, ZasadyOceniania i Agent oceniający (planowane TASK-022) | @Task: TASK-018
namespace Lemat.Domain.Calibration;

public sealed record MigawkaKalibratora(KalibratorPewnosciId KalibratorPewnosciId, MetodaKalibratora Metoda, PunktyKalibratora Punkty)
{
    public static MigawkaKalibratora Z(KalibratorPewnosci kalibrator)
    {
        ArgumentNullException.ThrowIfNull(kalibrator);
        return new MigawkaKalibratora(kalibrator.Id, kalibrator.Metoda, kalibrator.Punkty);
    }

    public decimal Kalibruj(decimal pewnoscSurowa) => Punkty.Kalibruj(Math.Clamp(pewnoscSurowa, 0m, 1m));
}
