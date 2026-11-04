// @Role: Mapowanie kolumn jsonb zadanie_wersja (opcje_zamkniete 7.5.1, rozwiazanie_wzorcowe 7.5.2, wynik_koncowy_wzorcowy 7.5.3) między obiektami domeny a DTO JSON (snake_case, wartości wyliczeniowe UPPER_SNAKE); konwertery EF | @DependsOn: Lemat.Domain.Content, LematJson, KonwencjaNazw | @UsedBy: ExerciseVersionEntityTypeConfiguration, testy round-trip | @Task: TASK-017
using System.Text.Json;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lemat.Infrastructure.Persistence.Configurations.Content;

internal sealed record OpcjaJson(string Id, string TrescMd);

internal sealed record StwierdzenieJson(string Id, string TrescMd, bool Poprawna);

internal sealed record OpcjeZamknieteJson(string Forma, List<OpcjaJson>? Opcje, string? Poprawna, List<StwierdzenieJson>? Stwierdzenia);

internal sealed record KrokJson(int Nr, string Latex, string Komentarz);

internal sealed record WariantJson(int Wariant, string Opis, List<KrokJson> Kroki);

internal sealed record RozwiazanieWzorcoweJson(List<WariantJson> Warianty);

internal sealed record WynikKoncowyJson(string Typ, string Latex, string? Sympy);

/// <summary>Zamiana UPPER_SNAKE &lt;-&gt; PascalCase dla wartości wyliczeniowych zapisanych w jsonb.</summary>
internal static class NazwyWyliczeniowJson
{
    public static string Zapisz<T>(T wartosc)
        where T : struct, Enum => KonwencjaNazw.PascalNaUpperSnake(wartosc.ToString());

    public static T Odczytaj<T>(string nazwa)
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

internal static class ContentJson
{
    public static string Serializuj(OpcjeZamkniete opcje) => JsonSerializer.Serialize(
        new OpcjeZamknieteJson(
            NazwyWyliczeniowJson.Zapisz(opcje.Forma),
            opcje.Forma == FormaZamknieta.WyborJednokrotny ? [.. opcje.Opcje.Select(o => new OpcjaJson(o.Id, o.TrescMd))] : null,
            opcje.Poprawna,
            opcje.Forma == FormaZamknieta.PrawdaFalsz ? [.. opcje.Stwierdzenia.Select(s => new StwierdzenieJson(s.Id, s.TrescMd, s.Poprawna))] : null),
        LematJson.Opcje);

    public static OpcjeZamkniete DeserializujOpcje(string json)
    {
        var dto = JsonSerializer.Deserialize<OpcjeZamknieteJson>(json, LematJson.Opcje)!;
        return NazwyWyliczeniowJson.Odczytaj<FormaZamknieta>(dto.Forma) switch
        {
            FormaZamknieta.WyborJednokrotny => OpcjeZamkniete.WyborJednokrotny(
                [.. (dto.Opcje ?? []).Select(o => new OpcjaWyboru(o.Id, o.TrescMd))],
                dto.Poprawna ?? throw new JsonException("Brak pola 'poprawna' w opcjach zamkniętych.")),
            _ => OpcjeZamkniete.PrawdaFalsz([.. (dto.Stwierdzenia ?? []).Select(s => new StwierdzeniePrawdaFalsz(s.Id, s.TrescMd, s.Poprawna))]),
        };
    }

    public static string Serializuj(RozwiazanieWzorcowe rozwiazanie) => JsonSerializer.Serialize(
        new RozwiazanieWzorcoweJson([.. rozwiazanie.Warianty.Select(w =>
            new WariantJson(w.Wariant, w.Opis, [.. w.Kroki.Select(k => new KrokJson(k.Nr, k.Latex, k.Komentarz))]))]),
        LematJson.Opcje);

    public static RozwiazanieWzorcowe DeserializujRozwiazanie(string json)
    {
        var dto = JsonSerializer.Deserialize<RozwiazanieWzorcoweJson>(json, LematJson.Opcje)!;
        return new RozwiazanieWzorcowe([.. dto.Warianty.Select(w =>
            new WariantWzorca(w.Wariant, w.Opis, [.. w.Kroki.Select(k => new KrokWzorcowy(k.Nr, k.Latex, k.Komentarz))]))]);
    }

    public static string Serializuj(WynikKoncowyWzorcowy wynik) => JsonSerializer.Serialize(
        new WynikKoncowyJson(NazwyWyliczeniowJson.Zapisz(wynik.Typ), wynik.Latex, wynik.PostacSympy),
        LematJson.Opcje);

    public static WynikKoncowyWzorcowy DeserializujWynik(string json)
    {
        var dto = JsonSerializer.Deserialize<WynikKoncowyJson>(json, LematJson.Opcje)!;
        return new WynikKoncowyWzorcowy(NazwyWyliczeniowJson.Odczytaj<TypWynikuKoncowego>(dto.Typ), dto.Latex, dto.Sympy);
    }
}

internal sealed class OpcjeZamknieteJsonbConverter()
    : ValueConverter<OpcjeZamkniete?, string>(o => ContentJson.Serializuj(o!), j => ContentJson.DeserializujOpcje(j));

internal sealed class RozwiazanieWzorcoweJsonbConverter()
    : ValueConverter<RozwiazanieWzorcowe, string>(r => ContentJson.Serializuj(r), j => ContentJson.DeserializujRozwiazanie(j));

internal sealed class WynikKoncowyJsonbConverter()
    : ValueConverter<WynikKoncowyWzorcowy?, string>(w => ContentJson.Serializuj(w!), j => ContentJson.DeserializujWynik(j));
