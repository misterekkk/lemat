// @Role: Konwerter JSON DateTimeOffset: zapis zawsze w UTC jako RFC 3339 z sufiksem Z (kontrakt POC 1: 2026-11-02T10:00:00Z); odczyt standardowy | @DependsOn: System.Text.Json | @UsedBy: JsonApi | @Task: TASK-127
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lemat.Api.Configuration;

public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateTimeOffset.Parse(reader.GetString() ?? throw new JsonException("Oczekiwano daty RFC 3339."), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture));
    }
}
