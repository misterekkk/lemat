// @Role: DTO Zadania w widoku Ucznia (ZadanieUczniaWyjscie, kontrakt POC 5) z Działem i Umiejętnościami; bez Rozwiązania wzorcowego, Kryteriów i klucza | @DependsOn: Lemat.Domain.Content.RodzajZadania, FormaZamknieta | @UsedBy: EndpointyTresci | @Task: TASK-125
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Api.Contracts.Content;

public sealed record DzialZadaniaWyjscie(int Id, string Nazwa);

public sealed record UmiejetnoscZadaniaWyjscie(int Id, string Nazwa, bool Glowna);

/// <summary>W POC wszystkie Zadania są otwarte: <c>forma_zamknieta</c>, <c>opcje</c> i <c>stwierdzenia</c> są <c>null</c>, <c>zalaczniki</c> puste.</summary>
public sealed record ZadanieUczniaWyjscie(
    Guid Id,
    Guid ZadanieWersjaId,
    RodzajZadania Rodzaj,
    FormaZamknieta? FormaZamknieta,
    string TrescMd,
    IReadOnlyList<object> Zalaczniki,
    object? Opcje,
    object? Stwierdzenia,
    int PunktyMax,
    int PoziomTrudnosci,
    DzialZadaniaWyjscie Dzial,
    IReadOnlyList<UmiejetnoscZadaniaWyjscie> Umiejetnosci);
