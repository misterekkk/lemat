// @Role: Mapowanie kolumn jsonb modułu Modele (konfiguracja_modelu.parametry 13.1; decyzja_modelu.opcje, wybor, rozklad) między obiektami domeny a JSON (snake_case, wartości wyliczeniowe UPPER_SNAKE, klucze rozkładu bez zmian); konwertery EF | @DependsOn: Lemat.Domain.Models, LematJson, KonwencjaNazw | @UsedBy: ModelConfigurationEntityTypeConfiguration, ModelDecisionEntityTypeConfiguration, testy round-trip | @Task: TASK-018
using System.Text.Encodings.Web;
using System.Text.Json;
using Lemat.Domain.Common;
using Lemat.Domain.Models;
using Lemat.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lemat.Infrastructure.Persistence.Configurations.Models;

internal sealed record ParametryModeluJson(
    decimal TimeoutS,
    int MaxTokens,
    decimal Temperatura,
    decimal CenaWejsciePln1k,
    decimal CenaWyjsciePln1k,
    int RetencjaDostawcyDni,
    string ReprezentacjaWzorow,
    int? MaksIteracjiFunkcji,
    string? TrybJev,
    string? DostawcaLlm);

internal sealed record OpcjeZakresuJson(int Min, int Max);

internal static class ModelsJson
{
    /// <summary>Klucze słowników (np. <c>KROK_1</c>, <c>tak</c>) zostają bez zmian.</summary>
    private static readonly JsonSerializerOptions BezPolityki = new(JsonSerializerDefaults.General)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Serializuj(ParametryModelu parametry) => JsonSerializer.Serialize(
        new ParametryModeluJson(
            parametry.TimeoutS,
            parametry.MaxTokens,
            parametry.Temperatura,
            parametry.CenaWejsciePln1k,
            parametry.CenaWyjsciePln1k,
            parametry.RetencjaDostawcyDni,
            KonwencjaNazw.PascalNaUpperSnake(parametry.ReprezentacjaWzorow.ToString()),
            parametry.MaksIteracjiFunkcji,
            parametry.TrybJev is { } tryb ? KonwencjaNazw.PascalNaUpperSnake(tryb.ToString()) : null,
            parametry.DostawcaLlm),
        LematJson.Opcje);

    public static ParametryModelu DeserializujParametry(string json)
    {
        var dto = JsonSerializer.Deserialize<ParametryModeluJson>(json, LematJson.Opcje)
            ?? throw new JsonException("Brak parametrów modelu.");
        return new ParametryModelu(
            dto.TimeoutS,
            dto.MaxTokens,
            dto.Temperatura,
            dto.CenaWejsciePln1k,
            dto.CenaWyjsciePln1k,
            dto.RetencjaDostawcyDni,
            Odczytaj<ReprezentacjaWzorow>(dto.ReprezentacjaWzorow),
            dto.MaksIteracjiFunkcji,
            dto.TrybJev is null ? null : Odczytaj<TrybJev>(dto.TrybJev),
            dto.DostawcaLlm);
    }

    public static string Serializuj(OpcjePytania opcje) => opcje.Lista is not null
        ? JsonSerializer.Serialize(opcje.Lista, BezPolityki)
        : JsonSerializer.Serialize(new OpcjeZakresuJson(opcje.Min ?? 0, opcje.Max ?? 0), LematJson.Opcje);

    public static OpcjePytania DeserializujOpcje(string json)
    {
        using var dokument = JsonDocument.Parse(json);
        return dokument.RootElement.ValueKind == JsonValueKind.Array
            ? new OpcjePytania([.. dokument.RootElement.EnumerateArray().Select(e => e.GetString()!)], null, null)
            : new OpcjePytania(null, dokument.RootElement.GetProperty("min").GetInt32(), dokument.RootElement.GetProperty("max").GetInt32());
    }

    public static string SerializujWybor(string wybor) => JsonSerializer.Serialize(wybor, BezPolityki);

    /// <summary>Wybór zapisany jako tekst JSON; liczba (np. zapisana przez innego pisarza) wraca jako jej zapis.</summary>
    public static string DeserializujWybor(string json)
    {
        using var dokument = JsonDocument.Parse(json);
        return dokument.RootElement.ValueKind == JsonValueKind.String ? dokument.RootElement.GetString()! : dokument.RootElement.GetRawText();
    }

    public static string SerializujRozklad(IReadOnlyDictionary<string, decimal> rozklad) => JsonSerializer.Serialize(rozklad, BezPolityki);

    public static IReadOnlyDictionary<string, decimal> DeserializujRozklad(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, decimal>>(json, BezPolityki) ?? throw new JsonException("Brak rozkładu.");

    private static T Odczytaj<T>(string nazwa)
        where T : struct, Enum
    {
        try
        {
            return Enum.Parse<T>(KonwencjaNazw.UpperSnakeNaPascal(nazwa), ignoreCase: false);
        }
        catch (ArgumentException e)
        {
            throw new JsonException($"Nieznana wartość '{nazwa}' typu {typeof(T).Name}.", e);
        }
    }
}

internal sealed class ParametryModeluJsonbConverter()
    : ValueConverter<ParametryModelu, string>(p => ModelsJson.Serializuj(p), j => ModelsJson.DeserializujParametry(j));

internal sealed class OpcjePytaniaJsonbConverter()
    : ValueConverter<OpcjePytania?, string>(o => ModelsJson.Serializuj(o!), j => ModelsJson.DeserializujOpcje(j));

internal sealed class WyborJsonbConverter()
    : ValueConverter<string?, string>(w => ModelsJson.SerializujWybor(w!), j => ModelsJson.DeserializujWybor(j));

internal sealed class RozkladJsonbConverter()
    : ValueConverter<IReadOnlyDictionary<string, decimal>?, string>(r => ModelsJson.SerializujRozklad(r!), j => ModelsJson.DeserializujRozklad(j));
