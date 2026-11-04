// @Role: Migawki i wejścia fasady Rekomendacji: ElementSkrot, SeriaSkrot, SciezkaSkrot, RekomendacjaSkrot, NowaRekomendacja | @DependsOn: Lemat.Domain.Recommendations, UczenId | @UsedBy: IRekomendacjeFasada, RekomendacjeFasada, Api (planowane TASK-073), Agent rekomendujący (TASK-040) | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;

namespace Lemat.Application.Recommendations;

public sealed record ElementSkrot(
    ElementSciezkiId Id,
    int Kolejnosc,
    TypElementuSciezki Typ,
    CelElementu Cel,
    UmiejetnoscId UmiejetnoscId,
    ZadanieId? ZadanieId,
    ZadanieWersjaId? ZadanieWersjaId,
    WkladkaTeoretycznaId? WkladkaTeoretycznaId,
    Guid? PrzypisanieId,
    bool WSerii,
    StatusElementuSciezki Status,
    DateTimeOffset WygasaAt,
    DateTimeOffset? RozpoczetoAt,
    DateTimeOffset? ZakonczonoAt);

public sealed record SeriaSkrot(UmiejetnoscId UmiejetnoscId, DateTimeOffset RozpoczetaAt, int LiczbaElementow, PoziomTrudnosci PoziomTrudnosci, int KolejnePorazki);

public sealed record SciezkaSkrot(SciezkaNaukiId Id, UczenId UczenId, int Version, SeriaSkrot? Seria, IReadOnlyList<ElementSkrot> AktywneElementy);

/// <summary>Dane Rekomendacji zapisywane razem z elementem; JSON w <c>snake_case</c> (10.1.4).</summary>
public sealed record NowaRekomendacja(
    StrategiaRekomendacji Strategia,
    RegulaRekomendacji Regula,
    string CechyJson,
    string DecyzjaJson,
    decimal? PrzewidywanePSukcesu,
    string Uzasadnienie,
    string UzasadnienieFaktyJson);

public sealed record RekomendacjaSkrot(
    RekomendacjaId Id,
    ElementSciezkiId ElementSciezkiId,
    StrategiaRekomendacji Strategia,
    RegulaRekomendacji Regula,
    decimal? PrzewidywanePSukcesu,
    string Uzasadnienie,
    string UzasadnienieFaktyJson,
    DateTimeOffset CreatedAt);

/// <summary>Element PROPONOWANY wraz z jego Rekomendacją (odpowiedź na „następne zadanie”).</summary>
public sealed record NastepnyElementSkrot(ElementSkrot Element, RekomendacjaSkrot? Rekomendacja);
