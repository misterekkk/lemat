// @Role: Repozytorium EF Core agregatu Profil ucznia: wczytanie ze śledzeniem i If-Match (OriginalValue wersji), odczyt bez śledzenia | @DependsOn: LematDbContext, IProfilRepozytorium | @UsedBy: DI (PersistenceRegistration), ProfilFasada | @Task: TASK-019
using Lemat.Application.Profile;
using Lemat.Domain.Accounts;
using Lemat.Domain.Profile;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Profile;

public sealed class ProfilRepozytoriumEf(LematDbContext kontekst) : IProfilRepozytorium
{
    public async Task<ProfilUcznia?> PobierzAsync(UczenId uczen, int? oczekiwanaWersja, CancellationToken ct)
    {
        var profil = await kontekst.Set<ProfilUcznia>().AsTracking().FirstOrDefaultAsync(p => p.Id == uczen, ct);
        if (profil is not null && oczekiwanaWersja is { } wersja)
        {
            kontekst.Entry(profil).Property(p => p.Version).OriginalValue = wersja;
        }

        return profil;
    }

    public Task<ProfilUcznia?> PobierzDoOdczytuAsync(UczenId uczen, CancellationToken ct) =>
        kontekst.Set<ProfilUcznia>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == uczen, ct);

    public void Dodaj(ProfilUcznia profil)
    {
        ArgumentNullException.ThrowIfNull(profil);
        kontekst.Add(profil);
    }
}
