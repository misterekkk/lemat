// @Role: DTO Transkrypcji w odpowiedzi API (TranskrypcjaWyjscie, kontrakt POC 7.5); zakres i zgodność Poprawek zawsze null w POC | @DependsOn: KrokTranskrypcjiWyjscie, ZrodloTranskrypcji | @UsedBy: RozwiazanieWyjscie, EndpointyRozwiazan | @Task: TASK-127
using Lemat.Domain.Solutions;

namespace Lemat.Api.Contracts.Solutions;

public sealed record TranskrypcjaWyjscie(
    Guid Id,
    short Wersja,
    ZrodloTranskrypcji Zrodlo,
    short PrzeslanieNr,
    decimal? ZgodnoscZZadaniem,
    IReadOnlyList<KrokTranskrypcjiWyjscie> Kroki,
    DateTimeOffset CreatedAt,
    object? ZakresPoprawek,
    bool KonfliktFiltra,
    object? ZgodnoscPoprawek);
