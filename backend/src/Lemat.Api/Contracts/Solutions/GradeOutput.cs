// @Role: DTO Oceny w odpowiedzi API (OcenaWyjscie, kontrakt POC 7.5); w POC komunikat i termin zgłoszenia zawsze null | @DependsOn: KryteriumOcenyWyjscie, StatusOceny, ZrodloOceny, TypBledu | @UsedBy: RozwiazanieWyjscie, EndpointyRozwiazan | @Task: TASK-127
using Lemat.Domain.Grading;

namespace Lemat.Api.Contracts.Solutions;

public sealed record OcenaWyjscie(
    Guid Id,
    short Wersja,
    StatusOceny Status,
    ZrodloOceny Zrodlo,
    WidocznoscOcenyDlaUcznia WidocznoscDlaUcznia,
    decimal? Punkty,
    decimal PunktyMax,
    short Wariant,
    IReadOnlyList<KryteriumOcenyWyjscie> Kryteria,
    short? PierwszyBlednyKrok,
    bool BrakujacyKrok,
    TypBledu? TypBledu,
    short? KrokPodejrzanyOdczyt,
    bool MozliwyBladOdczytu,
    string? Wskazowka,
    bool WskazowkaWPrzygotowaniu,
    string Uzasadnienie,
    decimal? PewnoscOceny,
    string? Komunikat,
    DateTimeOffset? MozliweZgloszenieDo,
    DateTimeOffset CreatedAt);
