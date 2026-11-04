// @Role: Konfiguracja EF Core tabeli proba_logowania: bigint identity always, citext e-mail, inet adres IP | @DependsOn: Lemat.Domain.Accounts.ProbaLogowania, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-016
using Lemat.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Lemat.Infrastructure.Persistence.Configurations.Accounts;

public sealed class ProbaLogowaniaEntityTypeConfiguration : IEntityTypeConfiguration<ProbaLogowania>
{
    public void Configure(EntityTypeBuilder<ProbaLogowania> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("proba_logowania");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityAlwaysColumn();
        builder.Property(p => p.Email).HasColumnType("citext").IsRequired();
        builder.Property(p => p.Ip).HasColumnType("inet");
    }
}
