// @Role: Konfiguracja EF Core agregatu Próg pewności (niezmienny poza aktywny): tabela prog_pewnosci, numeric(4,3), enum zrodlo_progu; aktywność zmienia ExecuteUpdate w transakcji (ef-core.md 7) | @DependsOn: Lemat.Domain.Calibration.ProgPewnosci, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-018
using Lemat.Domain.Calibration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Calibration;

public sealed class ProgPewnosciEntityTypeConfiguration : IEntityTypeConfiguration<ProgPewnosci>
{
    public void Configure(EntityTypeBuilder<ProgPewnosci> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("prog_pewnosci");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Ignore(p => p.Zdarzenia);
        builder.Ignore(p => p.Version);
        builder.Ignore(p => p.TrybPelnejWeryfikacji);
        builder.Property(p => p.RodzajZadania).HasColumnType("rodzaj_zadania");
        builder.Property(p => p.Wartosc).HasColumnType("numeric(4,3)");
        builder.Property(p => p.GornaGranicaBleduAuto).HasColumnType("numeric(5,4)");
        builder.Property(p => p.NEff).HasColumnType("numeric(8,1)");
        builder.Property(p => p.Zrodlo).HasColumnType("zrodlo_progu");
    }
}
