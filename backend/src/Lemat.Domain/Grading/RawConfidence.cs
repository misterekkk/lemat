// @Role: Wartość Pewność surowa (przed kalibracją, 0-1, 4 miejsca po przecinku) | @DependsOn: Lemat.Domain.Common.NiezmiennikNaruszonyException | @UsedBy: Ocena, konfiguracja EF | @Task: TASK-126
using Lemat.Domain.Common;

namespace Lemat.Domain.Grading;

public readonly record struct PewnoscSurowa
{
    public PewnoscSurowa(decimal wartosc)
    {
        if (wartosc is < 0m or > 1m)
        {
            throw new NiezmiennikNaruszonyException("PEWNOSC_SUROWA_POZA_ZAKRESEM", "Pewność surowa musi mieścić się w zakresie 0-1.");
        }

        Wartosc = Math.Round(wartosc, 4, MidpointRounding.ToEven);
    }

    public decimal Wartosc { get; }
}
