// @Role: Jedne opcje System.Text.Json dla żądań, odpowiedzi i generatora OpenAPI (snake_case z separacją cyfr, enumy UPPER_SNAKE, null jawnie, brak nieznanych pól) | @DependsOn: Lemat.Domain.Common.KonwencjaNazw | @UsedBy: ApiHost, ObslugaBledowWiazania, KodekKursora, testy | @Task: TASK-121
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lemat.Domain.Common;

namespace Lemat.Api.Configuration;

/// <summary>Opcje serializacji JSON API (api.md 1, ADR-067).</summary>
public static class JsonApi
{
    public static readonly JsonNamingPolicy Nazwy = new NazwyLemat(JsonNamingPolicy.SnakeCaseLower);

    public static readonly JsonNamingPolicy NazwyEnum = new NazwyLemat(JsonNamingPolicy.SnakeCaseUpper);

    public static void Konfiguruj(JsonSerializerOptions opcje)
    {
        ArgumentNullException.ThrowIfNull(opcje);
        opcje.PropertyNamingPolicy = Nazwy;
        opcje.NumberHandling = JsonNumberHandling.Strict;
        opcje.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        opcje.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        opcje.AllowOutOfOrderMetadataProperties = true;
        opcje.Converters.Add(new JsonStringEnumConverter(NazwyEnum, allowIntegerValues: false));
        opcje.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    }

    /// <summary>Opcje samodzielne (np. kursor, testy) o tej samej konfiguracji co pipeline HTTP.</summary>
    public static JsonSerializerOptions Opcje { get; } = Utworz();

    private static JsonSerializerOptions Utworz()
    {
        var opcje = new JsonSerializerOptions();
        Konfiguruj(opcje);
        opcje.MakeReadOnly(populateMissingResolver: true);
        return opcje;
    }
}
