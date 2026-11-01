// @Role: Value object Wariant rozwiązania (1-4) | @DependsOn: Lemat.Domain.Common.NiezmiennikNaruszonyException | @UsedBy: Lemat.Domain.Content.KryteriumOceniania, moduł Ocenianie | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public readonly record struct WariantRozwiazania
{
    public const int Minimum = 1;
    public const int Maksimum = 4;

    public WariantRozwiazania(int wartosc)
    {
        if (wartosc is < Minimum or > Maksimum)
        {
            throw new NiezmiennikNaruszonyException("WARIANT_POZA_ZAKRESEM", "Wariant rozwiązania musi mieścić się w zakresie 1-4.");
        }

        Wartosc = wartosc;
    }

    public int Wartosc { get; }
}
