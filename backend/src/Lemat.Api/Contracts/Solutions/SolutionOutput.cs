// @Role: DTO Rozwiązania w odpowiedzi API (RozwiazanieWyjscie, kontrakt POC 7.4/7.5) ze statusem, Zdjęciami, Transkrypcją, Oceną i nastepne_odpytanie_ms; Weryfikacja i szkic zawsze null w POC | @DependsOn: ZdjecieRozwiazaniaWyjscie, TranskrypcjaWyjscie, OcenaWyjscie, StatusRozwiazania | @UsedBy: EndpointyRozwiazan | @Task: TASK-127
using Lemat.Domain.Solutions;

namespace Lemat.Api.Contracts.Solutions;

public sealed record RozwiazanieWyjscie(
    Guid Id,
    Guid ZadanieId,
    Guid ZadanieWersjaId,
    StatusRozwiazania Status,
    Guid? ElementSciezkiId,
    Guid? PrzypisanieId,
    IReadOnlyList<ZdjecieRozwiazaniaWyjscie> Zdjecia,
    short LiczbaPrzeslan,
    short LiczbaPonowienRozpoznania,
    KodBleduRozpoznawania? KodBleduRozpoznawania,
    TranskrypcjaWyjscie? Transkrypcja,
    OcenaWyjscie? Ocena,
    object? Weryfikacja,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int? NastepneOdpytanieMs,
    object? SzkicTranskrypcji);
