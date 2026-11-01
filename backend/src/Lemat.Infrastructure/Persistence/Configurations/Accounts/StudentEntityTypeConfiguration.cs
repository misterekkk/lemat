// @Role: Konfiguracja EF Core tabeli uczen: PK uzytkownik_id (bez relacji EF do uzytkownik, FK odroczony w DDL), oswiadczenie_wieku_16, Grupa eksperymentu jako smallint (ef-core.md 6.1) | @DependsOn: Lemat.Domain.Accounts.Uczen, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-016
using Lemat.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Accounts;

public sealed class UczenEntityTypeConfiguration : IEntityTypeConfiguration<Uczen>
{
    public void Configure(EntityTypeBuilder<Uczen> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("uczen");
        builder.HasKey(u => u.Id);
        builder.Ignore(u => u.Zdarzenia);
        builder.Property(u => u.Id).HasColumnName("uzytkownik_id");
        builder.Property(u => u.OswiadczenieWieku16).HasColumnName("oswiadczenie_wieku_16");
        builder.Property(u => u.GrupaEksperymentu)
            .HasColumnName("procent_grupy_eksperymentu")
            .HasColumnType("smallint")
            .HasConversion(g => (short)g.ProcentGrupyEksperymentu, v => new GrupaEksperymentu(v));
        builder.Property(u => u.Version).IsConcurrencyToken();
    }
}
