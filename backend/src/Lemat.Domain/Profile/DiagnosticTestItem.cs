// @Role: Encja Pozycja Testu diagnostycznego: kolejność i wersja Zadania wybrana przy tworzeniu testu (ADR-018) | @DependsOn: PozycjaTestuDiagnostycznegoId, ZadanieWersjaId | @UsedBy: TestDiagnostyczny, konfiguracja EF, Odpowiedź (planowane TASK-020) | @Task: TASK-019
using Lemat.Domain.Content;

namespace Lemat.Domain.Profile;

public sealed class PozycjaTestuDiagnostycznego
{
    private PozycjaTestuDiagnostycznego()
    {
    }

    public PozycjaTestuDiagnostycznegoId Id { get; private init; }

    public int Kolejnosc { get; private init; }

    public ZadanieWersjaId ZadanieWersjaId { get; private init; }

    internal static PozycjaTestuDiagnostycznego Utworz(PozycjaTestuDiagnostycznegoId id, int kolejnosc, ZadanieWersjaId wersja) =>
        new() { Id = id, Kolejnosc = kolejnosc, ZadanieWersjaId = wersja };
}
