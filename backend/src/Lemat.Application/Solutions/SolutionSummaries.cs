// @Role: Migawki fasady Rozwiązań: RozwiazanieSkrot, ZdjecieSkrot, TranskrypcjaSkrot, KrokSkrot (bez kluczy obiektów poza zdjęciem) | @DependsOn: Lemat.Domain.Solutions | @UsedBy: IRozwiazaniaFasada, SolutionsFacade, Api (planowane TASK-127) | @Task: TASK-126
using Lemat.Domain.Accounts;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Solutions;

public sealed record ZdjecieSkrot(Guid Id, int PrzeslanieNr, int Kolejnosc, string KluczObiektu, int Szerokosc, int Wysokosc, bool Dostepne);

public sealed record RozwiazanieSkrot(
    RozwiazanieId Id,
    UczenId UczenId,
    ZadanieId ZadanieId,
    ZadanieWersjaId ZadanieWersjaId,
    ElementSciezkiId? ElementSciezkiId,
    Guid? PrzypisanieId,
    StatusRozwiazania Status,
    int LiczbaPrzeslan,
    int LiczbaPonowienRozpoznania,
    KodBleduRozpoznawania? KodBleduRozpoznawania,
    TranskrypcjaId? NajnowszaTranskrypcjaId,
    int? NajnowszaWersjaTranskrypcji,
    DateTimeOffset PrzeslanoAt,
    DateTimeOffset? TranskrypcjaGotowaAt,
    DateTimeOffset? ZatwierdzonoAt,
    DateTimeOffset? OcenaWystawionaAt,
    TimeSpan? CzasSystemowy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version,
    IReadOnlyList<ZdjecieSkrot> Zdjecia);

public sealed record KrokSkrot(int Nr, string Tekst, string Latex, decimal? PewnoscOdczytu);

public sealed record TranskrypcjaSkrot(
    TranskrypcjaId Id,
    RozwiazanieId RozwiazanieId,
    int Wersja,
    ZrodloTranskrypcji Zrodlo,
    int PrzeslanieNr,
    decimal? ZgodnoscZZadaniem,
    DateTimeOffset CreatedAt,
    IReadOnlyList<KrokSkrot> Kroki);

/// <summary>Pozycja listy Rozwiązań Ucznia w porządku (created_at malejąco, id rosnąco) — podstawa kursora (kontrakt POC 7.6).</summary>
public sealed record PozycjaListyRozwiazan(DateTimeOffset CreatedAt, RozwiazanieId Id);

/// <summary>Strona Rozwiązań Ucznia; <see cref="MaWiecej"/> = istnieją kolejne pozycje po ostatniej zwróconej.</summary>
public sealed record StronaRozwiazan(IReadOnlyList<RozwiazanieSkrot> Elementy, bool MaWiecej);
