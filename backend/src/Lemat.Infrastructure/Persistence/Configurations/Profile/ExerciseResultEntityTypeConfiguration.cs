// @Role: Konfiguracja EF Core agregatu Wynik zadania: tabela wynik_zadania (bez version), numeric(4,3)/(3,2), enumy zrodlo_wyniku i powod_uniewaznienia_wyniku | @DependsOn: Lemat.Domain.Profile.WynikZadania, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-019
using Lemat.Domain.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Profile;

public sealed class WynikZadaniaEntityTypeConfiguration : IEntityTypeConfiguration<WynikZadania>
{
    public void Configure(EntityTypeBuilder<WynikZadania> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("wynik_zadania");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Ignore(w => w.Zdarzenia);
        builder.Ignore(w => w.Version);
        builder.Property(w => w.Zrodlo).HasColumnType("zrodlo_wyniku");
        builder.Property(w => w.PowodUniewaznienia).HasColumnType("powod_uniewaznienia_wyniku");
        builder.Property(w => w.Ulamek).HasColumnType("numeric(4,3)");
        builder.Property(w => w.Waga).HasColumnType("numeric(3,2)");
    }
}
