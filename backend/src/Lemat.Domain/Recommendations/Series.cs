// @Role: Value object Seria (ADR-030): kolejne Zadania z jednej Umiejętności na rosnącym Poziomie trudności (≤ 8 elementów, 3 porażki, 7 dni) | @DependsOn: UmiejetnoscId, PoziomTrudnosci | @UsedBy: SciezkaNauki, Agent rekomendujący (planowane TASK-040) | @Task: TASK-019
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Recommendations;

public sealed record Seria(
    UmiejetnoscId UmiejetnoscId,
    DateTimeOffset RozpoczetaAt,
    int LiczbaElementow,
    PoziomTrudnosci PoziomTrudnosci,
    int KolejnePorazki)
{
    public const int MaksElementow = 8;

    public const int MaksKolejnychPorazek = 3;

    public static readonly TimeSpan MaksCzasTrwania = TimeSpan.FromDays(7);

    /// <summary>Seria kończy się po 3 kolejnych porażkach, po 8 elementach albo po 7 dniach.</summary>
    public bool CzyDoZakonczenia(DateTimeOffset teraz) =>
        KolejnePorazki >= MaksKolejnychPorazek || LiczbaElementow >= MaksElementow || teraz - RozpoczetaAt >= MaksCzasTrwania;
}
