// @Role: Repozytorium EF Core agregatu Klasa: wczytanie ze śledzeniem i If-Match (OriginalValue wersji), wyszukanie po Kodzie zaproszenia, listy Nauczyciela i Ucznia (od najpóźniej dołączonej) | @DependsOn: LematDbContext, IKlasaRepozytorium | @UsedBy: DI (PersistenceRegistration), KlasyFasada | @Task: TASK-018
using Lemat.Application.Classes;
using Lemat.Domain.Accounts;
using Lemat.Domain.Classes;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Classes;

public sealed class KlasaRepozytoriumEf(LematDbContext kontekst) : IKlasaRepozytorium
{
    public async Task<Klasa?> PobierzAsync(KlasaId id, int? oczekiwanaWersja, CancellationToken ct)
    {
        var klasa = await kontekst.Set<Klasa>().AsTracking().FirstOrDefaultAsync(k => k.Id == id, ct);
        if (klasa is not null && oczekiwanaWersja is { } wersja)
        {
            kontekst.Entry(klasa).Property(k => k.Version).OriginalValue = wersja;
        }

        return klasa;
    }

    public Task<Klasa?> PobierzDoOdczytuAsync(KlasaId id, CancellationToken ct) =>
        kontekst.Set<Klasa>().AsNoTracking().FirstOrDefaultAsync(k => k.Id == id, ct);

    public Task<Klasa?> PobierzPoKodzieAsync(string kod, CancellationToken ct) =>
        kontekst.Set<Klasa>().AsTracking().FirstOrDefaultAsync(k => EF.Property<string>(k, "KodWartosc") == kod, ct);

    public Task<bool> KodZajetyAsync(string kod, CancellationToken ct) =>
        kontekst.Set<Klasa>().AsNoTracking().AnyAsync(k => EF.Property<string>(k, "KodWartosc") == kod, ct);

    public async Task<IReadOnlyList<Klasa>> ListaNauczycielaAsync(NauczycielId nauczyciel, CancellationToken ct) =>
        await kontekst.Set<Klasa>().AsNoTracking()
            .Where(k => k.NauczycielId == nauczyciel)
            .OrderBy(k => k.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Klasa>> ListaUczniaAsync(UczenId uczen, CancellationToken ct) =>
        await kontekst.Set<Klasa>().AsNoTracking()
            .Where(k => k.Czlonkowie.Any(c => c.UczenId == uczen))
            .OrderByDescending(k => k.Czlonkowie.Where(c => c.UczenId == uczen).Max(c => c.DolaczonoAt))
            .ToListAsync(ct);

    public void Dodaj(Klasa klasa)
    {
        ArgumentNullException.ThrowIfNull(klasa);
        kontekst.Add(klasa);
    }
}
