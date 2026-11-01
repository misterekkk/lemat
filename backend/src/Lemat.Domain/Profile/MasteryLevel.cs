// @Role: Value object Poziom opanowania Umiejętności: prawdopodobieństwo sukcesu przy Zadaniu o Poziomie trudności 4 (0-1, 3 miejsca po przecinku) | @DependsOn: NiezmiennikNaruszonyException | @UsedBy: OpanowanieUmiejetnosci, ModelElo, IProfilFasada | @Task: TASK-019
using Lemat.Domain.Common;

namespace Lemat.Domain.Profile;

public readonly record struct PoziomOpanowania
{
    public PoziomOpanowania(decimal wartosc)
    {
        if (wartosc is < 0m or > 1m)
        {
            throw new NiezmiennikNaruszonyException("POZIOM_OPANOWANIA_POZA_ZAKRESEM", "Poziom opanowania musi mieścić się w zakresie 0-1.");
        }

        Wartosc = wartosc;
    }

    public decimal Wartosc { get; }
}
