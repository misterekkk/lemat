// @Role: Konfiguracja EF Core agregatu Ścieżka nauki: tabela sciezka_nauki (Seria w kolumnach seria_*, token version) i encji wewnętrznej element_sciezki (HasMany, tylko aktywne ładowane filtrowanym Include), enumy typ_elementu_sciezki, cel_elementu, status_elementu_sciezki (ef-core.md 6.5) | @DependsOn: Lemat.Domain.Recommendations.SciezkaNauki, ElementSciezki, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Recommendations;

public sealed class SciezkaNaukiEntityTypeConfiguration : IEntityTypeConfiguration<SciezkaNauki>
{
    public void Configure(EntityTypeBuilder<SciezkaNauki> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("sciezka_nauki");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Ignore(s => s.Zdarzenia);
        builder.Ignore(s => s.Seria);
        builder.Ignore(s => s.Proponowany);
        builder.Property(s => s.Version).IsConcurrencyToken();
        builder.HasIndex(s => s.UczenId).IsUnique();
        builder.HasOne<Uczen>().WithOne().HasForeignKey<SciezkaNauki>(s => s.UczenId);

        // Seria jest składana z kolumn seria_* (bez OwnsOne: brak Serii nie może zapisać NULL w seria_liczba_elementow NOT NULL).
        builder.Property<UmiejetnoscId?>("SeriaUmiejetnoscId").HasColumnName("seria_umiejetnosc_id");
        builder.Property<DateTimeOffset?>("SeriaRozpoczetaAt").HasColumnName("seria_rozpoczeta_at");
        builder.Property<int>("SeriaLiczbaElementow").HasColumnName("seria_liczba_elementow");
        builder.Property<int?>("SeriaPoziomTrudnosci").HasColumnName("seria_poziom_trudnosci");
        builder.Property<int>("SeriaKolejnePorazki").HasColumnName("seria_kolejne_porazki");

        builder.HasMany(s => s.AktywneElementy).WithOne().HasForeignKey("sciezka_nauki_id").IsRequired();
        builder.Navigation(s => s.AktywneElementy).HasField("_aktywne").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ElementSciezkiEntityTypeConfiguration : IEntityTypeConfiguration<ElementSciezki>
{
    public void Configure(EntityTypeBuilder<ElementSciezki> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("element_sciezki");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Ignore(e => e.CzyAktywny);
        builder.Property(e => e.Typ).HasColumnType("typ_elementu_sciezki");
        builder.Property(e => e.Cel).HasColumnType("cel_elementu");
        builder.Property(e => e.Status).HasColumnType("status_elementu_sciezki");
        builder.Property(e => e.WSerii).HasColumnName("w_serii");
    }
}
