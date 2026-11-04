// @Role: DTO UczenWyjscie (GET/PATCH /uczniowie/ja, kontrakt POC 4.1) wraz ze stanem testu diagnostycznego i Klasą Ucznia | @DependsOn: Lemat.Domain.Common.PoziomMatury | @UsedBy: EndpointyUcznia | @Task: TASK-124
using Lemat.Domain.Common;

namespace Lemat.Api.Contracts.Accounts;

public enum StanTestuDiagnostycznego
{
    Brak,
    WToku,
    Zakonczony,
    Pominiety,
    Niedostepny,
}

public sealed record KlasaUcznia(Guid Id, string Nazwa, string Nauczyciel);

public sealed record UczenWyjscie(
    Guid Id,
    string Email,
    string Imie,
    string Nazwisko,
    PoziomMatury? PoziomMatury,
    DateOnly? TerminEgzaminu,
    int? WybranyDzialId,
    bool ZgodaNaTrening,
    StanTestuDiagnostycznego TestDiagnostyczny,
    IReadOnlyList<KlasaUcznia> Klasy,
    int Version);
