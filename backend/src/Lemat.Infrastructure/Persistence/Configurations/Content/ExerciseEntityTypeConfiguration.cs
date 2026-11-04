// @Role: Konfiguracje EF Core agregatu Zadanie: tabela zadanie (enumy PG, token version, aktualna_wersja_id), wersje (zadanie_wersja z jsonb 7.5.1-7.5.3 i Kryteriami oceniania jako owned collection) oraz zadanie_umiejetnosc jako owned collection (ef-core.md 6.2) | @DependsOn: Lemat.Domain.Content, ContentJson, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-017
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Content;

public sealed class ZadanieEntityTypeConfiguration : IEntityTypeConfiguration<Zadanie>
{
    public void Configure(EntityTypeBuilder<Zadanie> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("zadanie");
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id).ValueGeneratedNever();
        builder.Ignore(z => z.Zdarzenia);
        builder.Ignore(z => z.AktualnaWersja);
        builder.Ignore(z => z.UmiejetnoscGlowna);
        builder.Ignore(z => z.UmiejetnosciDodatkowe);
        builder.Ignore(z => z.CzyBazowe);
        builder.Ignore(z => z.CzyWygenerowane);
        builder.Property(z => z.Pochodzenie).HasColumnType("pochodzenie_zadania");
        builder.Property(z => z.Zasieg).HasColumnType("zasieg_zadania");
        builder.Property(z => z.Rodzaj).HasColumnType("rodzaj_zadania");
        builder.Property(z => z.FormaZamknieta).HasColumnType("forma_zamknieta");
        builder.Property(z => z.PoziomMatury).HasColumnType("poziom_matury");
        builder.Property(z => z.Status).HasColumnType("status_zadania");
        builder.Property(z => z.PoziomTrudnosciNominalny)
            .HasColumnType("smallint")
            .HasConversion(p => (short)p.Wartosc, v => new PoziomTrudnosci(v));
        builder.Property(z => z.Version).IsConcurrencyToken();

        builder.HasMany(z => z.Wersje).WithOne().HasForeignKey("zadanie_id").IsRequired();
        builder.Navigation(z => z.Wersje).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

        builder.OwnsMany(z => z.Umiejetnosci, o =>
        {
            o.ToTable("zadanie_umiejetnosc");
            o.WithOwner().HasForeignKey("zadanie_id");
            o.Property(u => u.UmiejetnoscId).ValueGeneratedNever();
            o.HasKey("zadanie_id", nameof(PowiazanieUmiejetnosci.UmiejetnoscId));
        });
        builder.Navigation(z => z.Umiejetnosci).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ZadanieWersjaEntityTypeConfiguration : IEntityTypeConfiguration<ZadanieWersja>
{
    public void Configure(EntityTypeBuilder<ZadanieWersja> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("zadanie_wersja");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Property(w => w.PunktyMax).HasColumnType("smallint");
        builder.Property(w => w.OpcjeZamkniete).HasColumnType("jsonb").HasConversion(new OpcjeZamknieteJsonbConverter());
        builder.Property(w => w.RozwiazanieWzorcowe).HasColumnType("jsonb").HasConversion(new RozwiazanieWzorcoweJsonbConverter()).IsRequired();
        builder.Property(w => w.WynikKoncowyWzorcowy).HasColumnType("jsonb").HasConversion(new WynikKoncowyJsonbConverter());

        builder.OwnsMany(w => w.KryteriaOceniania, k =>
        {
            k.ToTable("kryterium_oceniania");
            k.WithOwner().HasForeignKey("zadanie_wersja_id");
            k.Property<Guid>("id").HasDefaultValueSql("gen_random_uuid()");
            k.HasKey("id");
            k.Property(x => x.Wariant)
                .HasColumnType("smallint")
                .HasConversion(w => (short)w.Wartosc, v => new WariantRozwiazania(v));
            k.Property(x => x.NrKryterium).HasColumnType("smallint");
            k.Property(x => x.Opis).IsRequired();
            k.Property(x => x.CzyWynikKoncowy);
            k.Property(x => x.PunktyMax).HasColumnType("smallint");
            k.Property(x => x.WymagaNrKryterium).HasColumnType("smallint");
        });
        builder.Navigation(w => w.KryteriaOceniania).HasField("_kryteria").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
