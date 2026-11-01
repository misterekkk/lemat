// @Role: Repozytorium EF Core agregatu Zadanie (ze śledzeniem do zmiany, bez śledzenia do odczytu, If-Match, po wersji, kandydaci doboru z kalibracją trudności) | @DependsOn: LematDbContext, IZadanieRepozytorium, KalibracjaTrudnosci | @UsedBy: DI (PersistenceRegistration), TresciFasada | @Task: TASK-017
using Lemat.Application.Content;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace Lemat.Infrastructure.Persistence.Repositories.Content;

public sealed class ZadanieRepozytoriumEf(LematDbContext kontekst) : IZadanieRepozytorium
{
    public async Task<Zadanie?> PobierzAsync(ZadanieId id, int? oczekiwanaWersja, CancellationToken ct)
    {
        var zadanie = await kontekst.Set<Zadanie>().AsTracking().FirstOrDefaultAsync(z => z.Id == id, ct);
        if (zadanie is not null && oczekiwanaWersja is { } wersja)
        {
            kontekst.Entry(zadanie).Property(z => z.Version).OriginalValue = wersja;
        }

        return zadanie;
    }

    public Task<Zadanie?> PobierzDoOdczytuAsync(ZadanieId id, CancellationToken ct) =>
        kontekst.Set<Zadanie>().AsNoTracking().FirstOrDefaultAsync(z => z.Id == id, ct);

    public Task<Zadanie?> PobierzPoWersjiAsync(ZadanieWersjaId wersja, CancellationToken ct) =>
        kontekst.Set<Zadanie>().AsNoTracking().FirstOrDefaultAsync(z => z.Wersje.Any(w => w.Id == wersja), ct);

    public async Task<IReadOnlyList<ZadanieDoDoboru>> PobierzKandydatowAsync(FiltrKandydatow filtr, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filtr);
        var rodzaje = filtr.Rodzaje.ToList();
        var glowna = filtr.UmiejetnoscGlowna;
        var poziom = filtr.Poziom;
        var wstrzymane = (await kontekst.Database
                .SqlQuery<Guid>($"SELECT zadanie_id AS \"Value\" FROM wstrzymanie_w_doborze")
                .ToListAsync(ct))
            .Select(g => new ZadanieId(g))
            .ToList();

        var zadania = await kontekst.Set<Zadanie>()
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Include(z => z.Umiejetnosci)
            .Include(z => z.Wersje.Where(w => w.ZastapionaAt == null))
            .Where(z => z.Status == StatusZadania.Opublikowane
                && z.Zasieg == ZasiegZadania.Globalny
                && z.PoziomMatury == poziom
                && rodzaje.Contains(z.Rodzaj)
                && z.Umiejetnosci.Any(u => u.Glowna && u.UmiejetnoscId == glowna)
                && !wstrzymane.Contains(z.Id))
            .OrderBy(z => z.Id)
            .ToListAsync(ct);

        var identyfikatory = zadania.Select(z => z.Id).ToList();
        var kalibracje = (await kontekst.Set<KalibracjaTrudnosci>()
                .AsNoTracking()
                .Where(k => identyfikatory.Contains(k.ZadanieId))
                .ToListAsync(ct))
            .ToDictionary(k => k.ZadanieId);

        return [.. zadania.Select(z =>
        {
            var wersja = z.AktualnaWersja;
            kalibracje.TryGetValue(z.Id, out var kalibracja);
            return new ZadanieDoDoboru(
                z.Id,
                wersja.Id,
                z.Rodzaj,
                z.FormaZamknieta,
                wersja.OpcjeZamkniete?.Stwierdzenia.Count ?? 0,
                z.UmiejetnoscGlowna,
                z.UmiejetnosciDodatkowe,
                kalibracja?.PoziomTrudnosci ?? z.PoziomTrudnosciNominalny,
                kalibracja?.TrudnoscB ?? 0.8m * (z.PoziomTrudnosciNominalny.Wartosc - 3),
                kalibracja?.LiczbaWynikowKalibracji ?? 0,
                z.CzyBazowe);
        })];
    }

    public void Dodaj(Zadanie zadanie)
    {
        ArgumentNullException.ThrowIfNull(zadanie);
        kontekst.Add(zadanie);
        kontekst.Add(KalibracjaTrudnosci.Poczatkowa(zadanie.Id, zadanie.PoziomTrudnosciNominalny, zadanie.CreatedAt));
    }
}
