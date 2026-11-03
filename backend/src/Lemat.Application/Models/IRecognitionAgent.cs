// @Role: Port Agenta rozpoznającego (10.3, U-26): odczyt pisma Ucznia do kroków Transkrypcji; w POC implementacja Mock (deterministyczna), realna dojdzie w TASK-043/044 | @DependsOn: ZadanieId, KodBleduRozpoznawania, WynikRozpoznania | @UsedBy: RozpoznajPismoHandler, MockAgentRozpoznajacy | @Task: TASK-128
using Lemat.Domain.Content;

namespace Lemat.Application.Models;

public interface IAgentRozpoznajacy
{
    /// <param name="zadanie">Zadanie, którego dotyczy kartka (Mock wybiera dane po Zadaniu; treść zdjęcia nie ma znaczenia).</param>
    /// <param name="scenariusz">Scenariusz Mocka z nagłówka <c>X-Lemat-Mock-Scenariusz</c> (ADR-065); implementacje realne go ignorują.</param>
    Task<WynikRozpoznania> RozpoznajAsync(ZadanieId zadanie, string scenariusz, CancellationToken ct);
}
