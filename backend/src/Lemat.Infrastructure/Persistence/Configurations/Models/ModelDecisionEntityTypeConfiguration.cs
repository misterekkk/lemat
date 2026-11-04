// @Role: Konfiguracja EF Core agregatu Decyzja modelu (niezmienny): tabela partycjonowana decyzja_modelu z kluczem (id, created_at), enumy kontekst_decyzji i typ_pytania, jsonb opcje/wybor/rozklad, bytea skrot_stanu (ef-core.md 6.6) | @DependsOn: Lemat.Domain.Models.DecyzjaModelu, ModelsJson, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-018
using Lemat.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Models;

public sealed class DecyzjaModeluEntityTypeConfiguration : IEntityTypeConfiguration<DecyzjaModelu>
{
    public void Configure(EntityTypeBuilder<DecyzjaModelu> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("decyzja_modelu");
        builder.HasKey(d => new { d.Id, d.CreatedAt });
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Ignore(d => d.Zdarzenia);
        builder.Ignore(d => d.Version);
        builder.Property(d => d.Kontekst).HasColumnType("kontekst_decyzji");
        builder.Property(d => d.TypPytania).HasColumnType("typ_pytania");
        builder.Property(d => d.SkrotStanu).HasColumnType("bytea");
        builder.Property(d => d.Opcje).HasColumnType("jsonb").HasConversion(new OpcjePytaniaJsonbConverter());
        builder.Property(d => d.Wybor).HasColumnType("jsonb").HasConversion(new WyborJsonbConverter());
        builder.Property(d => d.Rozklad).HasColumnType("jsonb").HasConversion(new RozkladJsonbConverter());
        builder.HasIndex(d => new { d.Kontekst, d.KontekstId }).HasDatabaseName("ix_decyzja_kontekst");
    }
}
