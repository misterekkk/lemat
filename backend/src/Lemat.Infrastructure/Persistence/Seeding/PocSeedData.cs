// @Role: Dane startowe POC wczytywane z osadzonego pliku poc-seed.json (Działy, Umiejętności, Zadania z Kryteriami, konta demo) | @DependsOn: poc-seed.json (zasób osadzony), System.Text.Json | @UsedBy: SeederPoc, testy seed-poc | @Task: TASK-122
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lemat.Infrastructure.Persistence.Seeding;

public sealed record DzialPoc(int Id, string Kod, string Nazwa, bool TylkoRozszerzony, int Kolejnosc);

public sealed record UmiejetnoscPoc(int Id, string Kod, int DzialId, string Nazwa, decimal WagaEgzaminacyjna);

public sealed record KrokPoc(int Nr, string Latex, string Komentarz);

public sealed record WariantPoc(int Wariant, string Opis, IReadOnlyList<KrokPoc> Kroki);

public sealed record RozwiazanieWzorcowePoc(IReadOnlyList<WariantPoc> Warianty);

public sealed record WynikKoncowyPoc(string Typ, string Latex, string? PostacSympy);

public sealed record KryteriumPoc(int Wariant, int NrKryterium, string Opis, int PunktyMax, bool CzyWynikKoncowy, int? WymagaNrKryterium);

public sealed record ZadaniePoc(
    Guid ZadanieId,
    Guid ZadanieWersjaId,
    int DzialId,
    int UmiejetnoscId,
    int PoziomTrudnosci,
    int PunktyMax,
    string TrescMd,
    RozwiazanieWzorcowePoc RozwiazanieWzorcowe,
    WynikKoncowyPoc WynikKoncowyWzorcowy,
    IReadOnlyList<KryteriumPoc> Kryteria);

/// <summary>Konto demo Ucznia; hasło jawne tylko dla lokalnych kont POC (OQ-15).</summary>
public sealed record KontoPoc(Guid Id, string Email, string Haslo, string Imie, string Nazwisko);

public sealed record DaneStartowePoc(
    IReadOnlyList<DzialPoc> Dzialy,
    IReadOnlyList<UmiejetnoscPoc> Umiejetnosci,
    IReadOnlyList<ZadaniePoc> Zadania,
    IReadOnlyList<KontoPoc> Konta)
{
    private const string NazwaZasobu = "Lemat.Infrastructure.Persistence.Data.poc-seed.json";

    private static readonly JsonSerializerOptions OpcjeJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static DaneStartowePoc Wczytaj()
    {
        using var strumien = typeof(DaneStartowePoc).Assembly.GetManifestResourceStream(NazwaZasobu)
            ?? throw new InvalidOperationException($"Brak zasobu osadzonego {NazwaZasobu}.");
        return JsonSerializer.Deserialize<DaneStartowePoc>(strumien, OpcjeJson)
            ?? throw new InvalidOperationException("Plik poc-seed.json jest pusty.");
    }
}
