// @Role: Konfiguracja EF Core agregatu Transkrypcja (niezmienny, bez version): tabela transkrypcja z enumem zrodlo_transkrypcji, FK do rozwiazanie oraz kroki jako owned collection w tabeli krok_transkrypcji (ef-core.md 6.3) | @DependsOn: Lemat.Domain.Solutions.Transkrypcja, KrokTranskrypcji, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-126
using Lemat.Domain.Solutions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Solutions;

public sealed class TranskrypcjaEntityTypeConfiguration : IEntityTypeConfiguration<Transkrypcja>
{
    public void Configure(EntityTypeBuilder<Transkrypcja> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("transkrypcja");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Ignore(t => t.Zdarzenia);
        builder.Ignore(t => t.Version);
        builder.Property(t => t.Zrodlo).HasColumnType("zrodlo_transkrypcji");
        builder.Property(t => t.Wersja).HasColumnType("smallint");
        builder.Property(t => t.PrzeslanieNr).HasColumnType("smallint");
        builder.Property(t => t.DaneOsoboweUsuniete).HasColumnType("smallint");
        builder.Property(t => t.ZgodnoscZZadaniem).HasColumnType("numeric(4,3)");
        builder.HasOne<Rozwiazanie>().WithMany().HasForeignKey(t => t.RozwiazanieId).IsRequired().OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(t => t.Kroki, k =>
        {
            k.ToTable("krok_transkrypcji");
            k.WithOwner().HasForeignKey("transkrypcja_id");
            k.Property<Guid>("id").HasDefaultValueSql("gen_random_uuid()");
            k.HasKey("id");
            k.Property(x => x.Nr).HasColumnType("smallint");
            k.Property(x => x.NrKrokuAgenta).HasColumnType("smallint");
            k.Property(x => x.PewnoscOdczytu).HasColumnType("numeric(4,3)");
            k.Property(x => x.OdlegloscEdycyjna).HasColumnType("numeric(4,3)");
        });
        builder.Navigation(t => t.Kroki).HasField("_kroki").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
