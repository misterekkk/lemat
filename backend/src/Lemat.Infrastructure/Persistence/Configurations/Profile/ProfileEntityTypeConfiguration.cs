// @Role: Konfiguracja EF Core agregatu Profil ucznia: tabele profil_ucznia i opanowanie_umiejetnosci (OwnsMany, klucz uczen_id + umiejetnosc_id), numeric(6,3)/(4,3), enum inicjalizacja_opanowania, token version (ef-core.md 6.5) | @DependsOn: Lemat.Domain.Profile.ProfilUcznia, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Profile;

public sealed class ProfilUczniaEntityTypeConfiguration : IEntityTypeConfiguration<ProfilUcznia>
{
    public void Configure(EntityTypeBuilder<ProfilUcznia> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("profil_ucznia");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("uczen_id").ValueGeneratedNever();
        builder.Ignore(p => p.Zdarzenia);
        builder.Property(p => p.Version).IsConcurrencyToken();

        // Relacja bez nawigacji: kolejność INSERT uczen -> profil_ucznia (ADR-063 p. 3).
        builder.HasOne<Uczen>().WithOne().HasForeignKey<ProfilUcznia>(p => p.Id);

        builder.OwnsMany(p => p.Opanowanie, o =>
        {
            o.ToTable("opanowanie_umiejetnosci");
            o.WithOwner().HasForeignKey("uczen_id");
            o.Property(x => x.UmiejetnoscId).ValueGeneratedNever();
            o.HasKey("uczen_id", nameof(OpanowanieUmiejetnosci.UmiejetnoscId));
            o.Property(x => x.Theta).HasColumnType("numeric(6,3)");
            o.Property(x => x.PoziomOpanowania)
                .HasColumnType("numeric(4,3)")
                .HasConversion(v => v.Wartosc, v => new PoziomOpanowania(v));
            o.Property(x => x.Inicjalizacja).HasColumnType("inicjalizacja_opanowania");
        });
        builder.Navigation(p => p.Opanowanie).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
