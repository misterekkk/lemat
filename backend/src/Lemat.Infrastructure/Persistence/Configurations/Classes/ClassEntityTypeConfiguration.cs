// @Role: Konfiguracja EF Core agregatu Klasa: tabele klasa i czlonkostwo_klasy (OwnsMany, klucz klasa_id + uczen_id), Kod zaproszenia w kolumnach kod_zaproszenia i kod_wazny_do, token version (ef-core.md 7) | @DependsOn: Lemat.Domain.Classes.Klasa, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-018
using Lemat.Domain.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Classes;

public sealed class KlasaEntityTypeConfiguration : IEntityTypeConfiguration<Klasa>
{
    public void Configure(EntityTypeBuilder<Klasa> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("klasa");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).ValueGeneratedNever();
        builder.Ignore(k => k.Zdarzenia);
        builder.Ignore(k => k.KodZaproszenia);
        builder.Property(k => k.PoziomMatury).HasColumnType("poziom_matury");
        builder.Property<string>("KodWartosc").HasColumnName("kod_zaproszenia").HasColumnType("character(8)").IsRequired();
        builder.Property<DateTimeOffset>("KodWaznyDo").HasColumnName("kod_wazny_do");
        builder.Property(k => k.Version).IsConcurrencyToken();

        builder.OwnsMany(k => k.Czlonkowie, o =>
        {
            o.ToTable("czlonkostwo_klasy");
            o.WithOwner().HasForeignKey("klasa_id");
            o.Property(c => c.UczenId).ValueGeneratedNever();
            o.HasKey("klasa_id", nameof(CzlonkostwoKlasy.UczenId));
        });
        builder.Navigation(k => k.Czlonkowie).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

}
