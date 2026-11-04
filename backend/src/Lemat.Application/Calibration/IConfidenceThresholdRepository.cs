// @Role: Port repozytorium agregatu Próg pewności: dodanie, odczyt aktywnego dla pary (Konfiguracja modelu, Rodzaj zadania), dezaktywacja poprzedniego (ux_prog_aktywny) | @DependsOn: Lemat.Domain.Calibration.ProgPewnosci | @UsedBy: KalibracjaFasada, ConfidenceThresholdRepositoryEf | @Task: TASK-018
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Application.Calibration;

public interface IProgPewnosciRepozytorium
{
    void Dodaj(ProgPewnosci prog);

    Task<ProgPewnosci?> PobierzAktywnyAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct);

    /// <summary>Wyłącza aktywny Próg pary (pojedyncze UPDATE przed dodaniem nowego, w tej samej transakcji; kolejność wymuszona przez <c>ux_prog_aktywny</c>).</summary>
    Task DezaktywujAktywnyAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct);
}
