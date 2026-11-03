// @Role: Punkty przyznane za jedno Kryterium oceniania w Ocenie (ocena_kryterium) wraz z prawdopodobieństwem z rozkładu Modelu decyzyjnego | @DependsOn: Lemat.Domain.Common.NiezmiennikNaruszonyException | @UsedBy: Ocena, konfiguracja EF | @Task: TASK-126
using Lemat.Domain.Common;

namespace Lemat.Domain.Grading;

public sealed record OcenaKryterium
{
    public OcenaKryterium(Guid kryteriumOcenianiaId, int punkty, decimal? prawdopodobienstwo)
    {
        if (kryteriumOcenianiaId == Guid.Empty)
        {
            throw new NiezmiennikNaruszonyException("OCENA_KRYTERIUM_ID_WYMAGANE", "Wymagany identyfikator Kryterium oceniania.");
        }

        if (punkty < 0)
        {
            throw new NiezmiennikNaruszonyException("OCENA_KRYTERIUM_PUNKTY_UJEMNE", "Punkty za Kryterium nie mogą być ujemne.");
        }

        if (prawdopodobienstwo is < 0m or > 1m)
        {
            throw new NiezmiennikNaruszonyException("OCENA_KRYTERIUM_PRAWDOPODOBIENSTWO", "Prawdopodobieństwo musi mieścić się w zakresie 0-1.");
        }

        KryteriumOcenianiaId = kryteriumOcenianiaId;
        Punkty = punkty;
        Prawdopodobienstwo = prawdopodobienstwo is { } p ? Math.Round(p, 4, MidpointRounding.ToEven) : null;
    }

    public Guid KryteriumOcenianiaId { get; }

    public int Punkty { get; }

    public decimal? Prawdopodobienstwo { get; }
}
