// @Role: Pomocnicy kolumn jsonb: konwerter System.Text.Json (snake_case, UnsafeRelaxedJsonEscaping) dla rekordów, JsonDocument i OwnsOne/OwnsMany ToJson (ef-core.md 3.3) | @DependsOn: Microsoft.EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL | @UsedBy: konfiguracje encji (TASK-016+), testy | @Task: TASK-015
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lemat.Infrastructure.Persistence.Conventions;

/// <summary>Opcje serializacji kolumn jsonb o stałym schemacie (7.5) — bez wyjścia ze znaków <c>&lt; &gt; &amp;</c>.</summary>
public static class LematJson
{
    public static JsonSerializerOptions Opcje { get; } = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false,
    };
}

/// <summary>Wartość obiektu zapisywana jako jsonb tekstem JSON (kolumny wyłącznie do zapisu i raportów).</summary>
public sealed class JsonbConverter<T>()
    : ValueConverter<T, string>(
        wartosc => JsonSerializer.Serialize(wartosc, LematJson.Opcje),
        json => JsonSerializer.Deserialize<T>(json, LematJson.Opcje)!)
    where T : class;

public static class JsonbExtensions
{
    /// <summary>Kolumna jsonb o stałym schemacie mapowana na rekord (konwerter System.Text.Json).</summary>
    public static PropertyBuilder<T> HasJsonbRecord<T>(this PropertyBuilder<T> builder)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasColumnType("jsonb").HasConversion(new JsonbConverter<T>());
    }

    /// <summary>Kolumna jsonb bez schematu mapowana na <see cref="JsonDocument"/> (wymaga <c>EnableDynamicJson</c> w źródle danych).</summary>
    public static PropertyBuilder<JsonDocument> HasJsonbDocument(this PropertyBuilder<JsonDocument> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasColumnType("jsonb");
    }

    /// <summary>Wartość obiektu domeny (7.5) jako <c>OwnsOne(...).ToJson()</c> w kolumnie jsonb o nazwie <paramref name="kolumna"/>.</summary>
    public static OwnedNavigationBuilder<TOwner, TValue> OwnsOneJsonb<TOwner, TValue>(
        this EntityTypeBuilder<TOwner> builder,
        System.Linq.Expressions.Expression<Func<TOwner, TValue?>> nawigacja,
        string kolumna)
        where TOwner : class
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        OwnedNavigationBuilder<TOwner, TValue>? wynik = null;
        builder.OwnsOne(nawigacja, b =>
        {
            b.ToJson(kolumna);
            wynik = b;
        });
        return wynik!;
    }

    /// <summary>Kolekcja wartości domeny jako <c>OwnsMany(...).ToJson()</c> w kolumnie jsonb.</summary>
    public static OwnedNavigationBuilder<TOwner, TValue> OwnsManyJsonb<TOwner, TValue>(
        this EntityTypeBuilder<TOwner> builder,
        System.Linq.Expressions.Expression<Func<TOwner, IEnumerable<TValue>?>> nawigacja,
        string kolumna)
        where TOwner : class
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        OwnedNavigationBuilder<TOwner, TValue>? wynik = null;
        builder.OwnsMany(nawigacja, b =>
        {
            b.ToJson(kolumna);
            wynik = b;
        });
        return wynik!;
    }
}
