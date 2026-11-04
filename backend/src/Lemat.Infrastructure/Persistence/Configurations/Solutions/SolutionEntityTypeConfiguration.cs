// @Role: Konfiguracja EF Core agregatu Rozwiązanie: tabela rozwiazanie (enum status_rozwiazania, kod błędu rozpoznawania jako text UPPER_SNAKE, czas systemowy w ms, token version) i encji wewnętrznej zdjecie_rozwiazania (ef-core.md 6.3) | @DependsOn: Lemat.Domain.Solutions.Rozwiazanie, ZdjecieRozwiazania, UpperSnakeEnumConverter, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-126
using Lemat.Domain.Solutions;
using Lemat.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Solutions;

public sealed class RozwiazanieEntityTypeConfiguration : IEntityTypeConfiguration<Rozwiazanie>
{
    public void Configure(EntityTypeBuilder<Rozwiazanie> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("rozwiazanie");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Ignore(r => r.Zdarzenia);
        builder.Ignore(r => r.CzyTerminalne);
        builder.Property(r => r.Status).HasColumnType("status_rozwiazania");
        builder.Property(r => r.KodBleduRozpoznawania)
            .HasColumnType("text")
            .HasConversion(new UpperSnakeEnumConverter<KodBleduRozpoznawania>());
        builder.Property(r => r.LiczbaPrzeslan).HasColumnType("smallint");
        builder.Property(r => r.LiczbaPonowienRozpoznania).HasColumnType("smallint");
        builder.Property(r => r.NajnowszaWersjaTranskrypcji).HasColumnType("smallint");
        builder.Property(r => r.CzasSystemowy)
            .HasColumnName("czas_systemowy_ms")
            .HasConversion(
                v => v.HasValue ? (int?)(int)v.Value.TotalMilliseconds : null,
                v => v.HasValue ? TimeSpan.FromMilliseconds(v.Value) : null);
        builder.Property(r => r.Version).IsConcurrencyToken();

        builder.HasMany(r => r.Zdjecia).WithOne().HasForeignKey("rozwiazanie_id").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Zdjecia).HasField("_zdjecia").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

public sealed class ZdjecieRozwiazaniaEntityTypeConfiguration : IEntityTypeConfiguration<ZdjecieRozwiazania>
{
    public void Configure(EntityTypeBuilder<ZdjecieRozwiazania> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("zdjecie_rozwiazania");
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id).ValueGeneratedNever();
        builder.Property(z => z.PrzeslanieNr).HasColumnType("smallint");
        builder.Property(z => z.Kolejnosc).HasColumnType("smallint");
        builder.Property(z => z.Szerokosc).HasColumnType("smallint");
        builder.Property(z => z.Wysokosc).HasColumnType("smallint");
        builder.Property(z => z.Sha256).HasColumnType("bytea");
    }
}
