// @Role: Wynik Agenta rozpoznającego: kroki odczytane z kartki z pewnością odczytu i zgodnością z Zadaniem albo kod błędu rozpoznawania | @DependsOn: KodBleduRozpoznawania | @UsedBy: IAgentRozpoznajacy, RozpoznajPismoHandler, MockAgentRozpoznajacy | @Task: TASK-128
using Lemat.Domain.Solutions;

namespace Lemat.Application.Models;

public sealed record KrokOdczytany(int Nr, string Tekst, string Latex, decimal PewnoscOdczytu);

/// <param name="Blad">Niepusty, gdy kartki nie udało się odczytać (wtedy <see cref="Kroki"/> jest puste).</param>
public sealed record WynikRozpoznania(KodBleduRozpoznawania? Blad, decimal ZgodnoscZZadaniem, IReadOnlyList<KrokOdczytany> Kroki)
{
    public static WynikRozpoznania Niepowodzenie(KodBleduRozpoznawania kod) => new(kod, 0m, []);
}
