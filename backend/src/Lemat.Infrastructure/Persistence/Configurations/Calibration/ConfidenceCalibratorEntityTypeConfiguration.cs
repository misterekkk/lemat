// @Role: Konfiguracja EF Core agregatu Kalibrator pewności (niezmienny): tabela kalibrator_pewnosci, metoda jako text UPPER_SNAKE, zrodlo_kalibratora, punkty jako jsonb (JsonbConverter) | @DependsOn: Lemat.Domain.Calibration.KalibratorPewnosci, CalibrationJson, UpperSnakeEnumConverter | @UsedBy: LematDbContext (ApplyConfigurationsFromAssembly) | @Task: TASK-018
using Lemat.Domain.Calibration;
using Lemat.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lemat.Infrastructure.Persistence.Configurations.Calibration;

public sealed class KalibratorPewnosciEntityTypeConfiguration : IEntityTypeConfiguration<KalibratorPewnosci>
{
    public void Configure(EntityTypeBuilder<KalibratorPewnosci> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("kalibrator_pewnosci");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).ValueGeneratedNever();
        builder.Ignore(k => k.Zdarzenia);
        builder.Ignore(k => k.Version);
        builder.Property(k => k.RodzajZadania).HasColumnType("rodzaj_zadania");
        builder.Property(k => k.Metoda).HasColumnType("text").HasConversion(new UpperSnakeEnumConverter<MetodaKalibratora>());
        builder.Property(k => k.Punkty).HasColumnType("jsonb").HasConversion(new PunktyKalibratoraJsonbConverter()).IsRequired();
        builder.Property(k => k.Ece).HasColumnType("numeric(5,4)");
        builder.Property(k => k.Zrodlo).HasColumnType("zrodlo_kalibratora");
    }
}
