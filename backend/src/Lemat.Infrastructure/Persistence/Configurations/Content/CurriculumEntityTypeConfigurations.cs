// @Role: Konfiguracje EF Core tabel dzial, umiejetnosc (+ umiejetnosc_wymagana jako owned collection) i kalibracja_trudnosci (ef-core.md 6.2, 7) | @DependsOn: Lemat.Domain.Content, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-017
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Content;

public sealed class DzialEntityTypeConfiguration : IEntityTypeConfiguration<Dzial>
{
    public void Configure(EntityTypeBuilder<Dzial> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("dzial");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Kolejnosc).HasColumnType("smallint");
    }
}

public sealed class UmiejetnoscEntityTypeConfiguration : IEntityTypeConfiguration<Umiejetnosc>
{
    public void Configure(EntityTypeBuilder<Umiejetnosc> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("umiejetnosc");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.WagaEgzaminacyjna).HasColumnType("numeric(4,3)");
        builder.Ignore(u => u.WymaganeUmiejetnosci);
        builder.OwnsMany(u => u.Wymagania, o =>
        {
            o.ToTable("umiejetnosc_wymagana");
            o.WithOwner().HasForeignKey("umiejetnosc_id");
            o.Property(w => w.WymaganaId).HasColumnName("wymagana_umiejetnosc_id").ValueGeneratedNever();
            o.HasKey("umiejetnosc_id", nameof(WymaganaUmiejetnosc.WymaganaId));
        });
        builder.Navigation(u => u.Wymagania).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class KalibracjaTrudnosciEntityTypeConfiguration : IEntityTypeConfiguration<KalibracjaTrudnosci>
{
    public void Configure(EntityTypeBuilder<KalibracjaTrudnosci> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("kalibracja_trudnosci");
        builder.HasKey(k => k.ZadanieId);
        builder.Property(k => k.ZadanieId).ValueGeneratedNever();
        builder.HasOne<Zadanie>().WithOne().HasForeignKey<KalibracjaTrudnosci>(k => k.ZadanieId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(k => k.TrudnoscB).HasColumnType("numeric(6,3)");
        builder.Property(k => k.PoziomTrudnosci)
            .HasColumnType("smallint")
            .HasConversion(p => (short)p.Wartosc, v => new PoziomTrudnosci(v));
    }
}
