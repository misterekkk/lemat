// @Role: Umiejętność (dane referencyjne): należy do Działu, kod poziom.dział.punkt (np. R.XIII.2), waga egzaminacyjna (0,001-1), wymagane Umiejętności wstępne | @DependsOn: UmiejetnoscId, DzialId, WymaganaUmiejetnosc, NiezmiennikNaruszonyException | @UsedBy: ITresciFasada, repozytoria Treści, moduły Profil, Rekomendacje | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

/// <summary>Powiązanie Umiejętności z wymaganą Umiejętnością wstępną (tabela <c>umiejetnosc_wymagana</c>).</summary>
public sealed record WymaganaUmiejetnosc(UmiejetnoscId WymaganaId);

public sealed class Umiejetnosc
{
    private readonly List<WymaganaUmiejetnosc> _wymagania = [];

    private Umiejetnosc()
    {
    }

    public UmiejetnoscId Id { get; private init; }

    public DzialId DzialId { get; private init; }

    public string Kod { get; private init; } = string.Empty;

    public string Nazwa { get; private init; } = string.Empty;

    public bool TylkoRozszerzony { get; private init; }

    /// <summary>Udział punktów Umiejętności w Arkuszach CKE; zakres (0, 1] (CHECK 7.3).</summary>
    public decimal WagaEgzaminacyjna { get; private init; }

    public IReadOnlyList<WymaganaUmiejetnosc> Wymagania => _wymagania;

    public IReadOnlyList<UmiejetnoscId> WymaganeUmiejetnosci => [.. _wymagania.Select(w => w.WymaganaId)];

    public static Umiejetnosc Utworz(
        UmiejetnoscId id,
        DzialId dzial,
        string kod,
        string nazwa,
        bool tylkoRozszerzony,
        decimal wagaEgzaminacyjna,
        IEnumerable<UmiejetnoscId>? wymagane = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kod);
        ArgumentException.ThrowIfNullOrWhiteSpace(nazwa);
        if (wagaEgzaminacyjna is <= 0m or > 1m)
        {
            throw new NiezmiennikNaruszonyException("WAGA_EGZAMINACYJNA_POZA_ZAKRESEM", "Waga egzaminacyjna musi mieścić się w zakresie (0, 1].");
        }

        var umiejetnosc = new Umiejetnosc
        {
            Id = id,
            DzialId = dzial,
            Kod = kod,
            Nazwa = nazwa,
            TylkoRozszerzony = tylkoRozszerzony,
            WagaEgzaminacyjna = wagaEgzaminacyjna,
        };
        foreach (var wymagana in (wymagane ?? []).Distinct())
        {
            if (wymagana == id)
            {
                throw new NiezmiennikNaruszonyException("UMIEJETNOSC_WYMAGA_SIEBIE", "Umiejętność nie może wymagać samej siebie.");
            }

            umiejetnosc._wymagania.Add(new WymaganaUmiejetnosc(wymagana));
        }

        return umiejetnosc;
    }
}
