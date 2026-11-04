// @Role: Konfiguracja EF Core agregatu Konfiguracja modelu: tabela konfiguracja_modelu, enum rola_modelu, parametry jako jsonb (ParametryModelu), token współbieżności xmin zamiast version (ef-core.md 3.4, 7) | @DependsOn: Lemat.Domain.Models.KonfiguracjaModelu, ModelsJson, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-018
using Lemat.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Models;

public sealed class KonfiguracjaModeluEntityTypeConfiguration : IEntityTypeConfiguration<KonfiguracjaModelu>
{
    public void Configure(EntityTypeBuilder<KonfiguracjaModelu> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("konfiguracja_modelu");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).ValueGeneratedNever();
        builder.Ignore(k => k.Zdarzenia);
        builder.Ignore(k => k.Version);
        builder.Ignore(k => k.JestRoleDecyzyjna);
        builder.Property(k => k.Rola).HasColumnType("rola_modelu");
        builder.Property(k => k.Parametry).HasColumnType("jsonb").HasConversion(new ParametryModeluJsonbConverter()).IsRequired();
        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
