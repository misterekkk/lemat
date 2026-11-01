// @Role: Konfiguracja EF Core tabeli nauczyciel: PK uzytkownik_id (bez relacji EF do uzytkownik), deklarowane minuty jako smallint (ef-core.md 6.1) | @DependsOn: Lemat.Domain.Accounts.Nauczyciel, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-016
using Lemat.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Accounts;

public sealed class NauczycielEntityTypeConfiguration : IEntityTypeConfiguration<Nauczyciel>
{
    public void Configure(EntityTypeBuilder<Nauczyciel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("nauczyciel");
        builder.HasKey(n => n.Id);
        builder.Ignore(n => n.Zdarzenia);
        builder.Property(n => n.Id).HasColumnName("uzytkownik_id");
        builder.Property(n => n.DeklarowaneMinutyWeryfikacji).HasColumnType("smallint");
        builder.Property(n => n.Version).IsConcurrencyToken();
    }
}
