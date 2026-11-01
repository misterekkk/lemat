// @Role: Port repozytorium agregatu Kalibrator pewności (niezmienny): dodanie i odczyt po Id oraz najnowszego dla pary (Konfiguracja modelu, Rodzaj zadania) | @DependsOn: Lemat.Domain.Calibration.KalibratorPewnosci | @UsedBy: KalibracjaFasada, ConfidenceCalibratorRepositoryEf | @Task: TASK-018
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Application.Calibration;

public interface IKalibratorPewnosciRepozytorium
{
    void Dodaj(KalibratorPewnosci kalibrator);

    Task<KalibratorPewnosci?> PobierzDoOdczytuAsync(KalibratorPewnosciId id, CancellationToken ct);

    Task<KalibratorPewnosci?> PobierzNajnowszyAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct);
}
