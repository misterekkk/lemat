// @Role: Fabryka konwerterów JSON dla Opcjonalne<T> (HandleNull = true: null w JSON daje Obecne = true, Wartosc = null; brak pola daje default) | @DependsOn: Opcjonalne<T>, System.Text.Json | @UsedBy: Opcjonalne<T> (atrybut JsonConverter), ZmianaUczniaWejscie | @Task: TASK-124
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lemat.Api.Contracts.Common;

public sealed class OpcjonalneKonwerter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Opcjonalne<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        var argument = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(typeof(Konwerter<>).MakeGenericType(argument))!;
    }

    private sealed class Konwerter<T> : JsonConverter<Opcjonalne<T>>
    {
        public override bool HandleNull => true;

        public override Opcjonalne<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new Opcjonalne<T>(true, default);
            }

            var wartosc = JsonSerializer.Deserialize<T>(ref reader, options);
            return new Opcjonalne<T>(true, wartosc);
        }

        public override void Write(Utf8JsonWriter writer, Opcjonalne<T> value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            if (!value.Obecne || value.Wartosc is null)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Wartosc, options);
        }
    }
}
