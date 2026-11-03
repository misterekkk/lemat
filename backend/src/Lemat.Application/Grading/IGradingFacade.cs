// @Role: Fasada modułu Ocenianie (6.2): odczyt Oceny aktualnej Rozwiązania; wystawianie Oceny dojdzie w TASK-128 | @DependsOn: Lemat.Domain.Grading, OcenaSkrot, RozwiazanieId | @UsedBy: Api (planowane TASK-127, TASK-128), moduły Rozwiązania i Weryfikacje | @Task: TASK-126
using Lemat.Domain.Solutions;

namespace Lemat.Application.Grading;

public interface IOcenianieFasada
{
    /// <summary>Ocena aktualna (status inny niż ZASTAPIONA); <c>null</c>, gdy Rozwiązanie nie ma jeszcze Oceny.</summary>
    Task<OcenaSkrot?> PobierzAktualnaAsync(RozwiazanieId rozwiazanie, CancellationToken ct);
}
