// @Role: Konfiguracja EF Core tabeli uzytkownik: citext e-mail, prywatny skrót hasła (shadow haslo_hash), Okres karencji jako owned w kolumnach tabeli, token współbieżności version (ef-core.md 6.1) | @DependsOn: Lemat.Domain.Accounts.Uzytkownik, Microsoft.EntityFrameworkCore | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-016
using Lemat.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Accounts;

public sealed class UzytkownikEntityTypeConfiguration : IEntityTypeConfiguration<Uzytkownik>
{
    public void Configure(EntityTypeBuilder<Uzytkownik> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("uzytkownik");
        builder.HasKey(u => u.Id);
        builder.Ignore(u => u.Zdarzenia);
        builder.Ignore(u => u.CzyProcesyWstrzymane);
        builder.Property(u => u.Email).HasColumnType("citext").IsRequired();
        builder.Property<string>("HasloHash").HasColumnName("haslo_hash").IsRequired();
        builder.OwnsOne(u => u.OkresKarencji, k =>
        {
            k.Property(o => o.UsuniecieZaplanowaneAt).HasColumnName("usuniecie_zaplanowane_at");
            k.Property(o => o.StatusPrzedWnioskiem).HasColumnName("status_przed_wnioskiem");
        });
        builder.Navigation(u => u.OkresKarencji).IsRequired(false);
        builder.Property(u => u.Version).IsConcurrencyToken();
    }
}
