// @Role: Repozytorium EF Core Wyników zadania: oczekujące na Profil, unieważnione do odtworzenia, ważne chronologicznie (created_at, id), wyszukiwanie po Ocenie/Odpowiedzi, liczba prób w oknie 60 dni | @DependsOn: LematDbContext, IWynikZadaniaRepozytorium | @UsedBy: DI (PersistenceRegistration), ProfilFasada | @Task: TASK-019
using Lemat.Application.Profile;
using Lemat.Domain.Accounts;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Profile;

public sealed class WynikZadaniaRepozytoriumEf(LematDbContext kontekst) : IWynikZadaniaRepozytorium
{
    public void Dodaj(WynikZadania wynik)
    {
        ArgumentNullException.ThrowIfNull(wynik);
        kontekst.Add(wynik);
    }

    public async Task<IReadOnlyList<WynikZadania>> PobierzOczekujaceNaProfilAsync(UczenId uczen, CancellationToken ct) =>
        Chronologicznie(await kontekst.Set<WynikZadania>().AsTracking()
            .Where(w => w.UczenId == uczen && w.ZastosowanyWProfiluAt == null && w.UniewaznionyAt == null)
            .ToListAsync(ct));

    public async Task<IReadOnlyList<WynikZadania>> PobierzUniewaznioneNieprzetworzoneAsync(UczenId uczen, CancellationToken ct) =>
        Chronologicznie(await kontekst.Set<WynikZadania>().AsTracking()
            .Where(w => w.UczenId == uczen && w.UniewaznionyAt != null && w.UniewaznieniePrzetworzoneAt == null)
            .ToListAsync(ct));

    public async Task<IReadOnlyList<WynikZadania>> PobierzWazneAsync(UczenId uczen, CancellationToken ct) =>
        Chronologicznie(await kontekst.Set<WynikZadania>().AsTracking()
            .Where(w => w.UczenId == uczen && w.UniewaznionyAt == null)
            .ToListAsync(ct));

    public async Task<IReadOnlyList<WynikZadania>> PobierzPoZrodleAsync(Guid? ocenaId, Guid? odpowiedzId, CancellationToken ct)
    {
        if (ocenaId is null && odpowiedzId is null)
        {
            return [];
        }

        var zapytanie = kontekst.Set<WynikZadania>().AsTracking();
        zapytanie = (ocenaId, odpowiedzId) switch
        {
            ({ } ocena, null) => zapytanie.Where(w => w.OcenaId == ocena),
            (null, { } odpowiedz) => zapytanie.Where(w => w.OdpowiedzId == odpowiedz),
            ({ } ocena, { } odpowiedz) => zapytanie.Where(w => w.OcenaId == ocena || w.OdpowiedzId == odpowiedz),
            _ => zapytanie,
        };
        return await zapytanie.ToListAsync(ct);
    }

    public Task<int> LiczbaWaznychOdAsync(UczenId uczen, ZadanieId zadanie, DateTimeOffset od, CancellationToken ct) =>
        kontekst.Set<WynikZadania>().AsNoTracking()
            .CountAsync(w => w.UczenId == uczen && w.ZadanieId == zadanie && w.UniewaznionyAt == null && w.CreatedAt >= od, ct);

    private static List<WynikZadania> Chronologicznie(List<WynikZadania> lista) =>
        [.. lista.OrderBy(w => w.CreatedAt).ThenBy(w => w.Id.Wartosc)];
}
