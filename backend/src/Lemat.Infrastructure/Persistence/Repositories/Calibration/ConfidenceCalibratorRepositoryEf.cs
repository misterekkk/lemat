// @Role: Repozytorium EF Core agregatu Kalibrator pewności (niezmienny): dodanie, odczyt po Id i najnowszego dla pary (Konfiguracja modelu, Rodzaj zadania) | @DependsOn: LematDbContext, IKalibratorPewnosciRepozytorium | @UsedBy: DI (PersistenceRegistration), KalibracjaFasada | @Task: TASK-018
using Lemat.Application.Calibration;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Calibration;

public sealed class KalibratorPewnosciRepozytoriumEf(LematDbContext kontekst) : IKalibratorPewnosciRepozytorium
{
    public void Dodaj(KalibratorPewnosci kalibrator)
    {
        ArgumentNullException.ThrowIfNull(kalibrator);
        kontekst.Add(kalibrator);
    }

    public Task<KalibratorPewnosci?> PobierzDoOdczytuAsync(KalibratorPewnosciId id, CancellationToken ct) =>
        kontekst.Set<KalibratorPewnosci>().AsNoTracking().FirstOrDefaultAsync(k => k.Id == id, ct);

    public Task<KalibratorPewnosci?> PobierzNajnowszyAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct) =>
        kontekst.Set<KalibratorPewnosci>().AsNoTracking()
            .Where(k => k.KonfiguracjaModeluId == konfiguracja && k.RodzajZadania == rodzaj)
            .OrderByDescending(k => k.CreatedAt).ThenByDescending(k => k.Id)
            .FirstOrDefaultAsync(ct);
}
