// @Role: Wynik Agenta oceniającego: punkty i prawdopodobieństwo per Kryterium, pierwszy błędny krok, Typ błędu, Wskazówka, uzasadnienie i Pewność oceny | @DependsOn: TypBledu | @UsedBy: IAgentOceniajacy, OcenRozwiazanieHandler, MockAgentOceniajacy | @Task: TASK-128
using Lemat.Domain.Grading;

namespace Lemat.Application.Models;

public sealed record PunktyKryterium(int Nr, int Punkty, decimal Prawdopodobienstwo);

public sealed record WynikOcenyAgenta(
    IReadOnlyList<PunktyKryterium> Kryteria,
    int? PierwszyBlednyKrok,
    TypBledu? TypBledu,
    string? Wskazowka,
    string Uzasadnienie,
    decimal PewnoscOceny);
