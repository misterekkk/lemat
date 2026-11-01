// @Role: Konfiguracja EF Core niezmiennego agregatu Rekomendacja: tabela rekomendacja (bez version), jsonb jako tekst JSON, reguła jako kod tekstowy (R-SERIA …), enum strategia_rekomendacji, relacja 1:1 do elementu Ścieżki (kolejność INSERT) | @DependsOn: Lemat.Domain.Recommendations.Rekomendacja, KodyRegulRekomendacji, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-019
using Lemat.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Recommendations;

public sealed class RekomendacjaEntityTypeConfiguration : IEntityTypeConfiguration<Rekomendacja>
{
    public void Configure(EntityTypeBuilder<Rekomendacja> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("rekomendacja");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Ignore(r => r.Zdarzenia);
        builder.Ignore(r => r.Version);
        builder.HasOne<ElementSciezki>().WithOne().HasForeignKey<Rekomendacja>(r => r.ElementSciezkiId);
        builder.Property(r => r.Strategia).HasColumnType("strategia_rekomendacji");
        builder.Property(r => r.Regula)
            .HasColumnType("text")
            .HasConversion(v => KodyRegulRekomendacji.Kod(v), v => KodyRegulRekomendacji.Parsuj(v));
        builder.Property(r => r.CechyJson).HasColumnName("cechy").HasColumnType("jsonb");
        builder.Property(r => r.DecyzjaJson).HasColumnName("decyzja").HasColumnType("jsonb");
        builder.Property(r => r.UzasadnienieFaktyJson).HasColumnName("uzasadnienie_fakty").HasColumnType("jsonb");
        builder.Property(r => r.PrzewidywanePSukcesu).HasColumnName("przewidywane_p_sukcesu").HasColumnType("numeric(4,3)");
    }
}
