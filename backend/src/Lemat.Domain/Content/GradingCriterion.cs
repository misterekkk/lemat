// @Role: Kryterium oceniania (pozycja Kryteriów oceniania wariantu): nr 1-12, punkty 1-3, znacznik wyniku końcowego, warunek zaliczenia wcześniejszego kryterium | @DependsOn: WariantRozwiazania, NiezmiennikNaruszonyException | @UsedBy: ZadanieWersja, ZasadyKryteriow, moduły Ocenianie, Agenci | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public sealed record KryteriumOceniania
{
    public const int MaksNrKryterium = 12;
    public const int MaksPunktyKryterium = 3;

    public KryteriumOceniania(WariantRozwiazania wariant, int nrKryterium, string opis, int punktyMax, bool czyWynikKoncowy, int? wymagaNrKryterium)
    {
        if (nrKryterium is < 1 or > MaksNrKryterium)
        {
            throw new NiezmiennikNaruszonyException("KRYTERIUM_NR_POZA_ZAKRESEM", "Numer kryterium musi mieścić się w zakresie 1-12.");
        }

        if (punktyMax is < 1 or > MaksPunktyKryterium)
        {
            throw new NiezmiennikNaruszonyException("KRYTERIUM_PUNKTY_POZA_ZAKRESEM", "Kryterium ma 1-3 punkty.");
        }

        if (string.IsNullOrWhiteSpace(opis))
        {
            throw new NiezmiennikNaruszonyException("KRYTERIUM_OPIS_WYMAGANY", "Kryterium wymaga opisu.");
        }

        if (wymagaNrKryterium is { } wymagane && (wymagane < 1 || wymagane >= nrKryterium))
        {
            throw new NiezmiennikNaruszonyException("KRYTERIUM_WARUNEK_NIEPRAWIDLOWY", "Kryterium może wymagać tylko kryterium o niższym numerze.");
        }

        Wariant = wariant;
        NrKryterium = nrKryterium;
        Opis = opis;
        PunktyMax = punktyMax;
        CzyWynikKoncowy = czyWynikKoncowy;
        WymagaNrKryterium = wymagaNrKryterium;
    }

    public WariantRozwiazania Wariant { get; }

    public int NrKryterium { get; }

    public string Opis { get; }

    public int PunktyMax { get; }

    public bool CzyWynikKoncowy { get; }

    /// <summary>Punkt tylko po zaliczeniu wskazanego kryterium tego samego wariantu.</summary>
    public int? WymagaNrKryterium { get; }
}
