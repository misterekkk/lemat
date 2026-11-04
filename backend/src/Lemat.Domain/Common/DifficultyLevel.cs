// @Role: Value object Poziom trudności (1–5) | @DependsOn: Lemat.Domain.Common.NiezmiennikNaruszonyException | @UsedBy: moduły Content, Profile, Recommendations | @Task: TASK-014
namespace Lemat.Domain.Common;

public readonly record struct PoziomTrudnosci
{
    public const int Minimum = 1;
    public const int Maksimum = 5;

    public static readonly PoziomTrudnosci Maturalny = new(4);

    public PoziomTrudnosci(int wartosc)
    {
        if (wartosc is < Minimum or > Maksimum)
        {
            throw new NiezmiennikNaruszonyException("POZIOM_TRUDNOSCI_POZA_ZAKRESEM", "Poziom trudności musi mieścić się w zakresie 1–5.");
        }

        Wartosc = wartosc;
    }

    public int Wartosc { get; }
}
