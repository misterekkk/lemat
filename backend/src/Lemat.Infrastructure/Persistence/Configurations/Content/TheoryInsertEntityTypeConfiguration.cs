// @Role: Konfiguracja EF Core agregatu Wkładka teoretyczna: tabela wkladka_teoretyczna, enum status_wkladki, token version (ef-core.md 7) | @DependsOn: Lemat.Domain.Content.WkladkaTeoretyczna, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-017
using Lemat.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Content;

public sealed class WkladkaTeoretycznaEntityTypeConfiguration : IEntityTypeConfiguration<WkladkaTeoretyczna>
{
    public void Configure(EntityTypeBuilder<WkladkaTeoretyczna> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("wkladka_teoretyczna");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Ignore(w => w.Zdarzenia);
        builder.Property(w => w.SzacowanyCzasMin).HasColumnType("smallint");
        builder.Property(w => w.KosztGenerowaniaPln).HasColumnType("numeric(10,4)");
        builder.Property(w => w.Status).HasColumnType("status_wkladki");
        builder.Property(w => w.Version).IsConcurrencyToken();
    }
}
