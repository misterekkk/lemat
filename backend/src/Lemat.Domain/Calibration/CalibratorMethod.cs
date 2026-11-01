// @Role: Wyliczenie Metoda kalibratora (kolumna kalibrator_pewnosci.metoda, CHECK tekstowy): tożsamościowa (MVP) albo izotoniczna CV5 | @DependsOn: — | @UsedBy: KalibratorPewnosci, konfiguracja EF | @Task: TASK-018
namespace Lemat.Domain.Calibration;

public enum MetodaKalibratora
{
    Tozsamosciowy,
    IzotonicznyCv5,
}
