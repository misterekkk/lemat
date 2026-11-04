// @Role: Konfiguracja EF Core agregatu Ocena (niezmienny, bez version): tabela ocena (enumy PG, Wskazówka w dwóch kolumnach, wartości pewności numeric(5,4)) i Kryteria jako owned collection w tabeli ocena_kryterium (ef-core.md 6.4) | @DependsOn: Lemat.Domain.Grading.Ocena, OcenaKryterium, Wskazowka, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-126
using Lemat.Domain.Content;
using Lemat.Domain.Grading;
using Lemat.Domain.Solutions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lemat.Infrastructure.Persistence.Configurations.Grading;

public sealed class OcenaEntityTypeConfiguration : IEntityTypeConfiguration<Ocena>
{
    public void Configure(EntityTypeBuilder<Ocena> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ocena");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Ignore(o => o.Zdarzenia);
        builder.Ignore(o => o.Version);
        builder.Ignore(o => o.Widocznosc);
        builder.Property(o => o.Status).HasColumnType("status_oceny");
        builder.Property(o => o.Zrodlo).HasColumnType("zrodlo_oceny");
        builder.Property(o => o.TypBledu).HasColumnType("typ_bledu");
        builder.Property(o => o.Wersja).HasColumnType("smallint");
        builder.Property(o => o.PunktySurowe).HasColumnType("smallint");
        builder.Property(o => o.Punkty).HasColumnType("smallint");
        builder.Property(o => o.PunktyMax).HasColumnType("smallint");
        builder.Property(o => o.PierwszyBlednyKrok).HasColumnType("smallint");
        builder.Property(o => o.KrokPodejrzanyOdczyt).HasColumnType("smallint");
        builder.Property(o => o.Wariant)
            .HasColumnType("smallint")
            .HasConversion(w => (short)w.Wartosc, v => new WariantRozwiazania(v));
        builder.Property(o => o.PewnoscOceny)
            .HasColumnType("numeric(5,4)")
            .HasConversion(new ValueConverter<PewnoscOceny, decimal>(p => p.Wartosc, d => new PewnoscOceny(d)));
        builder.Property(o => o.PewnoscSurowa)
            .HasColumnType("numeric(5,4)")
            .HasConversion(new ValueConverter<PewnoscSurowa, decimal>(p => p.Wartosc, d => new PewnoscSurowa(d)));
        builder.HasOne<Rozwiazanie>().WithMany().HasForeignKey(o => o.RozwiazanieId).IsRequired().OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(o => o.Wskazowka, w =>
        {
            w.Property(x => x.Tresc).HasColumnName("wskazowka").IsRequired();
            w.Property(x => x.Zrodlo).HasColumnName("zrodlo_wskazowki").HasColumnType("zrodlo_wskazowki");
        });

        builder.OwnsMany(o => o.Kryteria, k =>
        {
            k.ToTable("ocena_kryterium");
            k.WithOwner().HasForeignKey("ocena_id");
            k.Property(x => x.KryteriumOcenianiaId).ValueGeneratedNever();
            k.HasKey("ocena_id", nameof(OcenaKryterium.KryteriumOcenianiaId));
            k.Property(x => x.Punkty).HasColumnType("smallint");
            k.Property(x => x.Prawdopodobienstwo).HasColumnType("numeric(5,4)");
        });
        builder.Navigation(o => o.Kryteria).HasField("_kryteria").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
