// @Role: Port Agenta oceniającego (10.4, U-26): Ocena Rozwiązania względem Kryteriów (punkty za Kryterium z prawdopodobieństwem, błąd, Wskazówka, uzasadnienie); w POC Mock deterministyczny zamiast Modelu decyzyjnego | @DependsOn: ZadanieId, WejscieOceny, WynikOcenyAgenta | @UsedBy: OcenRozwiazanieHandler, MockAgentOceniajacy | @Task: TASK-128
using Lemat.Domain.Content;

namespace Lemat.Application.Models;

public interface IAgentOceniajacy
{
    Task<WynikOcenyAgenta> OcenAsync(WejscieOceny wejscie, CancellationToken ct);
}

public sealed record KryteriumDoOceny(int Nr, string Opis, int PunktyMax);

/// <param name="Scenariusz">Scenariusz Mocka (ADR-065); implementacje realne go ignorują.</param>
public sealed record WejscieOceny(ZadanieId Zadanie, IReadOnlyList<KryteriumDoOceny> Kryteria, string Scenariusz);
