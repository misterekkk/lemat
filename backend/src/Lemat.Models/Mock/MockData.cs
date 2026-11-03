// @Role: Dane Mocka POC (kontrakt POC 7.2 i 7.3) wczytywane z zasobu osadzonego poc-mock.json: kroki Transkrypcji i wynik Oceny per Zadanie (klucz: identyfikator Zadania) | @DependsOn: poc-mock.json, System.Text.Json | @UsedBy: MockAgentRozpoznajacy, MockAgentOceniajacy, MockModelsRegistration | @Task: TASK-128
using System.Text.Json;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Grading;

namespace Lemat.Models.Mock;

public sealed record MockKrok(int Nr, string Tekst, string Latex, decimal PewnoscOdczytu);

public sealed record MockOcena(IReadOnlyList<int> PunktyKryteriow, int? PierwszyBlednyKrok, string? TypBledu, string? Wskazowka)
{
    public TypBledu? TypBleduWartosc => TypBledu is null ? null : Enum.Parse<TypBledu>(KonwencjaNazw.UpperSnakeNaPascal(TypBledu));
}

public sealed record MockZadanie(decimal ZgodnoscZZadaniem, IReadOnlyList<MockKrok> Kroki, MockOcena Ocena);

/// <summary>Deterministyczne dane Mocka: zawartość Transkrypcji i Oceny zależy wyłącznie od Zadania.</summary>
public sealed class MockDane
{
    private const string NazwaZasobu = "Lemat.Models.Mock.Data.poc-mock.json";

    private static readonly JsonSerializerOptions OpcjeJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    private readonly IReadOnlyDictionary<Guid, MockZadanie> _zadania;

    private MockDane(IReadOnlyDictionary<Guid, MockZadanie> zadania) => _zadania = zadania;

    public static MockDane Wczytaj()
    {
        using var strumien = typeof(MockDane).Assembly.GetManifestResourceStream(NazwaZasobu)
            ?? throw new InvalidOperationException($"Brak zasobu osadzonego {NazwaZasobu}.");
        var plik = JsonSerializer.Deserialize<PlikMock>(strumien, OpcjeJson)
            ?? throw new InvalidOperationException("Plik poc-mock.json jest pusty.");
        return new MockDane(plik.Zadania);
    }

    public MockZadanie Pobierz(ZadanieId zadanie) =>
        _zadania.TryGetValue(zadanie.Wartosc, out var dane)
            ? dane
            : throw new InvalidOperationException($"Mock nie ma danych dla Zadania {zadanie.Wartosc}.");

    private sealed record PlikMock(Dictionary<Guid, MockZadanie> Zadania);
}
