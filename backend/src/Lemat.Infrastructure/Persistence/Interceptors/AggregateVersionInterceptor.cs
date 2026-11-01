// @Role: Interceptor SavingChanges: podnosi version korzenia agregatu (Agregat.PotwierdzZapis) przy zmianie dowolnego wiersza agregatu (ef-core.md 4, ADR-029, 3.6 p. 3) | @DependsOn: Lemat.Domain.Common.IKorzenAgregatu, Lemat.Domain.Common.BezZmianyWersjiAttribute, Microsoft.EntityFrameworkCore | @UsedBy: PersistenceRegistration | @Task: TASK-015
using System.Reflection;
using Lemat.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lemat.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Dla każdego wpisu <c>Added</c>/<c>Modified</c>/<c>Deleted</c> znajduje korzeń agregatu (sam wpis albo właściciel własności
/// <c>IsOwnership</c>, rekurencyjnie) i zwiększa jego <c>Version</c> o 1 — raz na zapis. Pomija: korzenie dodawane (mają
/// <c>Version = 1</c>), korzenie usuwane, korzenie z <c>Version</c> generowanym przez bazę (np. <c>Weryfikacja</c>, trigger
/// <c>trg_weryfikacja_wersja</c>) oraz zmiany wyłącznie właściwości <see cref="BezZmianyWersjiAttribute"/>.
/// </summary>
public sealed class WersjaAgregatuInterceptor : SaveChangesInterceptor
{
    private const string NazwaVersion = "Version";

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        PodniesWersje(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        PodniesWersje(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void PodniesWersje(DbContext? kontekst)
    {
        if (kontekst is null)
        {
            return;
        }

        var wpisy = kontekst.ChangeTracker.Entries().ToList();
        var korzenie = new HashSet<EntityEntry>();
        foreach (var wpis in wpisy)
        {
            if (wpis.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (ZmianaTylkoBezWersji(wpis))
            {
                continue;
            }

            var korzen = ZnajdzKorzen(wpis, wpisy);
            if (korzen is not null && korzen.State is EntityState.Unchanged or EntityState.Modified)
            {
                korzenie.Add(korzen);
            }
        }

        foreach (var korzen in korzenie)
        {
            var wlasciwosc = korzen.Metadata.FindProperty(NazwaVersion);
            if (wlasciwosc is null || wlasciwosc.ValueGenerated != ValueGenerated.Never || korzen.Entity is not IKorzenAgregatu agregat)
            {
                continue;
            }

            agregat.PotwierdzZapis();
            korzen.Property(NazwaVersion).IsModified = true;
        }
    }

    private static EntityEntry? ZnajdzKorzen(EntityEntry wpis, List<EntityEntry> wpisy)
    {
        var biezacy = wpis;
        for (var glebokosc = 0; glebokosc < 16; glebokosc++)
        {
            if (biezacy.Entity is IKorzenAgregatu)
            {
                return biezacy;
            }

            var wlasnosc = biezacy.Metadata.FindOwnership();
            if (wlasnosc is null)
            {
                return null;
            }

            var wartosci = wlasnosc.Properties
                .Select(p => biezacy.State == EntityState.Deleted ? biezacy.Property(p.Name).OriginalValue : biezacy.Property(p.Name).CurrentValue)
                .ToArray();
            var wlasciciel = wpisy.FirstOrDefault(w =>
                w.Metadata == wlasnosc.PrincipalEntityType
                && wlasnosc.PrincipalKey.Properties
                    .Select(p => w.Property(p.Name).CurrentValue)
                    .SequenceEqual(wartosci));
            if (wlasciciel is null)
            {
                return null;
            }

            biezacy = wlasciciel;
        }

        return null;
    }

    /// <summary>Prawda, gdy wpis jest zmodyfikowanym korzeniem, w którym zmieniły się wyłącznie właściwości [BezZmianyWersji]/Version/UpdatedAt.</summary>
    private static bool ZmianaTylkoBezWersji(EntityEntry wpis)
    {
        if (wpis.State != EntityState.Modified || wpis.Entity is not IKorzenAgregatu)
        {
            return false;
        }

        var zmienione = wpis.Properties.Where(p => p.IsModified && !p.Metadata.IsPrimaryKey()).ToList();
        return zmienione.Count > 0 && zmienione.All(p =>
            p.Metadata.Name is NazwaVersion or ZnacznikiCzasuInterceptor.NazwaWlasciwosci
            || p.Metadata.PropertyInfo?.GetCustomAttribute<BezZmianyWersjiAttribute>() is not null);
    }
}
