// @Role: Mapowanie kolumny jsonb kalibrator_pewnosci.punkty (schemat 11.2.2: typ + punkty [[surowa, skalibrowana], …]) między PunktyKalibratora a JSON; konwerter EF | @DependsOn: Lemat.Domain.Calibration.PunktyKalibratora, LematJson | @UsedBy: ConfidenceCalibratorEntityTypeConfiguration, testy round-trip | @Task: TASK-018
using System.Text.Json;
using Lemat.Domain.Calibration;
using Lemat.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lemat.Infrastructure.Persistence.Configurations.Calibration;

internal sealed record PunktyKalibratoraJson(string Typ, List<decimal[]> Punkty);

internal static class CalibrationJson
{
    public static string Serializuj(PunktyKalibratora punkty) => JsonSerializer.Serialize(
        new PunktyKalibratoraJson(punkty.Typ, [.. punkty.Punkty.Select(p => new[] { p.Surowa, p.Skalibrowana })]),
        LematJson.Opcje);

    public static PunktyKalibratora Deserializuj(string json)
    {
        var dto = JsonSerializer.Deserialize<PunktyKalibratoraJson>(json, LematJson.Opcje)
            ?? throw new JsonException("Brak punktów kalibratora.");
        if (dto.Punkty.Any(p => p.Length != 2))
        {
            throw new JsonException("Punkt kalibratora to para [surowa, skalibrowana].");
        }

        return PunktyKalibratora.Utworz(dto.Typ, [.. dto.Punkty.Select(p => new PunktKalibracji(p[0], p[1]))]);
    }
}

internal sealed class PunktyKalibratoraJsonbConverter()
    : ValueConverter<PunktyKalibratora, string>(p => CalibrationJson.Serializuj(p), j => CalibrationJson.Deserializuj(j));
