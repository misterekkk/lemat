// @Role: Repozytorium EF Core agregatu Próg pewności: dodanie, odczyt aktywnego Progu pary i wyłączenie poprzedniego przez ExecuteUpdate (ux_prog_aktywny) | @DependsOn: LematDbContext, IProgPewnosciRepozytorium | @UsedBy: DI (PersistenceRegistration), KalibracjaFasada | @Task: TASK-018
using Lemat.Application.Calibration;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Calibration;

public sealed class ProgPewnosciRepozytoriumEf(LematDbContext kontekst) : IProgPewnosciRepozytorium
{
    public void Dodaj(ProgPewnosci prog)
    {
        ArgumentNullException.ThrowIfNull(prog);
        kontekst.Add(prog);
    }

    public Task<ProgPewnosci?> PobierzAktywnyAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct) =>
        kontekst.Set<ProgPewnosci>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.KonfiguracjaModeluId == konfiguracja && p.RodzajZadania == rodzaj && p.Aktywny, ct);

    public async Task DezaktywujAktywnyAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct) =>
        await kontekst.Set<ProgPewnosci>()
            .Where(p => p.KonfiguracjaModeluId == konfiguracja && p.RodzajZadania == rodzaj && p.Aktywny)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Aktywny, false), ct);
}
