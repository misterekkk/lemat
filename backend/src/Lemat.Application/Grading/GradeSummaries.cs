// @Role: Migawki fasady Ocen: OcenaSkrot i OcenaKryteriumSkrot (bez pól wewnętrznych progu i kalibratora) | @DependsOn: Lemat.Domain.Grading | @UsedBy: IOcenianieFasada, GradingFacade, Api (planowane TASK-127, TASK-128) | @Task: TASK-126
using Lemat.Domain.Content;
using Lemat.Domain.Grading;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Grading;

public sealed record OcenaKryteriumSkrot(Guid KryteriumOcenianiaId, int NrKryterium, int Punkty, decimal? Prawdopodobienstwo);

public sealed record OcenaSkrot(
    OcenaId Id,
    RozwiazanieId RozwiazanieId,
    int Wersja,
    StatusOceny Status,
    ZrodloOceny Zrodlo,
    TranskrypcjaId TranskrypcjaId,
    WariantRozwiazania Wariant,
    int Punkty,
    int PunktyMax,
    int? PierwszyBlednyKrok,
    bool BrakujacyKrok,
    TypBledu? TypBledu,
    bool MozliwyBladOdczytu,
    int? KrokPodejrzanyOdczyt,
    decimal? PewnoscOceny,
    string Uzasadnienie,
    string? Wskazowka,
    ZrodloWskazowki? ZrodloWskazowki,
    WidocznoscOcenyDlaUcznia Widocznosc,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OcenaKryteriumSkrot> Kryteria);
