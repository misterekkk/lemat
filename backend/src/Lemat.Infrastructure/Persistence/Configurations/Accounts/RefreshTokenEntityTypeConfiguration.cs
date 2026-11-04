// @Role: Konfiguracja EF Core tabeli token_odswiezania: samoodwołujący się następca po rotacji (ON DELETE SET NULL), smallint okna tolerancji; bez tokenu współbieżności (rotacja atomowym SQL, ADR-057) | @DependsOn: Lemat.Domain.Accounts.TokenOdswiezania, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-016, TASK-123
using Lemat.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Accounts;

public sealed class TokenOdswiezaniaEntityTypeConfiguration : IEntityTypeConfiguration<TokenOdswiezania>
{
    public void Configure(EntityTypeBuilder<TokenOdswiezania> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("token_odswiezania");
        builder.HasKey(t => t.Id);
        // Relacja do konta w modelu EF (schemat i tak pochodzi z DDL): EF wstawia Użytkownika przed jego Tokenem odświeżania w jednej transakcji rejestracji.
        builder.HasOne<Uzytkownik>().WithMany().HasForeignKey(t => t.UzytkownikId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TokenOdswiezania>().WithMany().HasForeignKey(t => t.ZastapionyPrzez).OnDelete(DeleteBehavior.SetNull);
        builder.Property(t => t.UzyciaWOknieTolerancji).HasColumnType("smallint");
    }
}
