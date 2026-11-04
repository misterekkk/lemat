// @Role: Repozytorium EF Core agregatu Użytkownik: wczytanie ze śledzeniem i If-Match (OriginalValue wersji), odczyt bez śledzenia, dane logowania ze skrótem hasła z shadow property | @DependsOn: LematDbContext, IUzytkownikRepozytorium | @UsedBy: DI (PersistenceRegistration), AccountsFacade | @Task: TASK-016
using Lemat.Application.Accounts;
using Lemat.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Accounts;

public sealed class UzytkownikRepozytoriumEf(LematDbContext kontekst) : IUzytkownikRepozytorium
{
    public async Task<Uzytkownik?> PobierzAsync(UzytkownikId id, int? oczekiwanaWersja, CancellationToken ct)
    {
        var konto = await kontekst.Set<Uzytkownik>().AsTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (konto is not null && oczekiwanaWersja is { } wersja)
        {
            kontekst.Entry(konto).Property(u => u.Version).OriginalValue = wersja;
        }

        return konto;
    }

    public Task<Uzytkownik?> PobierzDoOdczytuAsync(UzytkownikId id, CancellationToken ct) =>
        kontekst.Set<Uzytkownik>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Uzytkownik?> PobierzPoEmailuAsync(string email, CancellationToken ct)
    {
        var szukany = Normalizuj(email);
        return kontekst.Set<Uzytkownik>().AsNoTracking().FirstOrDefaultAsync(u => u.Email == szukany, ct);
    }

    public Task<DaneLogowania?> PobierzDaneLogowaniaAsync(string email, CancellationToken ct)
    {
        var szukany = Normalizuj(email);
        return kontekst.Set<Uzytkownik>().AsNoTracking()
            .Where(u => u.Email == szukany)
            .Select(u => new DaneLogowania(u.Id, EF.Property<string>(u, "HasloHash"), u.Rola, u.Status))
            .FirstOrDefaultAsync(ct);
    }

    public void Dodaj(Uzytkownik uzytkownik, string hasloHash)
    {
        ArgumentNullException.ThrowIfNull(uzytkownik);
        ArgumentException.ThrowIfNullOrWhiteSpace(hasloHash);
        var wpis = kontekst.Add(uzytkownik);
        wpis.Property("HasloHash").CurrentValue = hasloHash;
    }

    private static string Normalizuj(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
