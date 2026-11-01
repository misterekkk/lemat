// @Role: Konfiguracja EF Core agregatu Test diagnostyczny: tabele test_diagnostyczny (bez version, pominiete_dzialy jako integer[]) i pozycja_testu_diagnostycznego (OwnsMany), enum status_testu | @DependsOn: Lemat.Domain.Profile.TestDiagnostyczny, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-019
using Lemat.Domain.Common;
using Lemat.Domain.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Profile;

public sealed class TestDiagnostycznyEntityTypeConfiguration : IEntityTypeConfiguration<TestDiagnostyczny>
{
    public void Configure(EntityTypeBuilder<TestDiagnostyczny> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("test_diagnostyczny");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Ignore(t => t.Zdarzenia);
        builder.Ignore(t => t.Version);
        builder.Ignore(t => t.PominieteDzialy);
        builder.Property<int[]>("PominieteDzialyKolumna").HasColumnName("pominiete_dzialy").HasColumnType("integer[]");
        builder.Property(t => t.PoziomMatury).HasColumnType("poziom_matury");
        builder.Property(t => t.Status).HasColumnType("status_testu");

        builder.OwnsMany(t => t.Pozycje, o =>
        {
            o.ToTable("pozycja_testu_diagnostycznego");
            o.WithOwner().HasForeignKey("test_diagnostyczny_id");
            o.HasKey(p => p.Id);
            o.Property(p => p.Id).ValueGeneratedNever();
        });
        builder.Navigation(t => t.Pozycje).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
