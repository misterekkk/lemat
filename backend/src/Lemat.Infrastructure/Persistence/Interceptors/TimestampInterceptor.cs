// @Role: Interceptor SavingChanges: ustawia updated_at (TimeProvider) dla zmienianych encji z właściwością UpdatedAt; created_at ma DEFAULT w bazie (ef-core.md 5) | @DependsOn: TimeProvider, Microsoft.EntityFrameworkCore | @UsedBy: PersistenceRegistration | @Task: TASK-015
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lemat.Infrastructure.Persistence.Interceptors;

public sealed class ZnacznikiCzasuInterceptor(TimeProvider zegar) : SaveChangesInterceptor
{
    public const string NazwaWlasciwosci = "UpdatedAt";

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Ustaw(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Ustaw(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Ustaw(DbContext? kontekst)
    {
        if (kontekst is null)
        {
            return;
        }

        var teraz = zegar.GetUtcNow();
        foreach (var wpis in kontekst.ChangeTracker.Entries().Where(w => w.State == EntityState.Modified))
        {
            var wlasciwosc = wpis.Metadata.FindProperty(NazwaWlasciwosci);
            if (wlasciwosc is not null && wlasciwosc.ClrType == typeof(DateTimeOffset))
            {
                wpis.Property(NazwaWlasciwosci).CurrentValue = teraz;
            }
        }
    }
}
