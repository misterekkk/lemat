// @Role: Rozwiązanie wzorcowe (value object, schemat jsonb 7.5.2): warianty z krokami (nr, latex, komentarz) | @DependsOn: NiezmiennikNaruszonyException | @UsedBy: ZadanieWersja, konfiguracje EF (jsonb), Agenci oceniający i generujący | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

/// <summary>Krok Rozwiązania wzorcowego (numer kroku to nie numer Kryterium).</summary>
public sealed record KrokWzorcowy(int Nr, string Latex, string Komentarz);

public sealed record WariantWzorca(int Wariant, string Opis, IReadOnlyList<KrokWzorcowy> Kroki);

public sealed record RozwiazanieWzorcowe
{
    public RozwiazanieWzorcowe(IReadOnlyList<WariantWzorca> warianty)
    {
        ArgumentNullException.ThrowIfNull(warianty);
        if (warianty.Count == 0)
        {
            throw new NiezmiennikNaruszonyException("ROZWIAZANIE_WZORCOWE_PUSTE", "Rozwiązanie wzorcowe wymaga co najmniej jednego wariantu.");
        }

        if (warianty.Select(w => w.Wariant).Distinct().Count() != warianty.Count)
        {
            throw new NiezmiennikNaruszonyException("ROZWIAZANIE_WZORCOWE_WARIANT_DUPLIKAT", "Numery wariantów muszą być unikalne.");
        }

        foreach (var wariant in warianty)
        {
            _ = new WariantRozwiazania(wariant.Wariant);
        }

        Warianty = warianty;
    }

    public IReadOnlyList<WariantWzorca> Warianty { get; }
}
