// @Role: Reguły spójności Kryteriów oceniania (lustro trg_suma_kryteriow, W-1): suma punktów wariantu = punkty_max wersji, <= 1 kryterium wyniku końcowego na wariant, <= 24 kryteria, brak Kryteriów dla Zadań zamkniętych | @DependsOn: KryteriumOceniania, RodzajZadania, NiezmiennikNaruszonyException | @UsedBy: ZadanieWersja, Agent generujący (Walidacja W-1) | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public static class ZasadyKryteriow
{
    public const int MaksLiczbaKryteriow = 24;
    public const string KodNiespojne = "KRYTERIA_NIESPOJNE";
    public const string KodZaDuzo = "ZA_DUZO_KRYTERIOW";

    public static void Sprawdz(RodzajZadania rodzaj, int punktyMax, IReadOnlyCollection<KryteriumOceniania> kryteria)
    {
        ArgumentNullException.ThrowIfNull(kryteria);
        if (rodzaj == RodzajZadania.Zamkniete)
        {
            if (kryteria.Count > 0)
            {
                throw new NiezmiennikNaruszonyException(KodNiespojne, "Zadanie zamknięte nie ma Kryteriów oceniania (ocena kluczem).");
            }

            return;
        }

        if (kryteria.Count == 0)
        {
            throw new NiezmiennikNaruszonyException(KodNiespojne, "Zadanie otwarte wymaga Kryteriów oceniania.");
        }

        if (kryteria.Count > MaksLiczbaKryteriow)
        {
            throw new NiezmiennikNaruszonyException(KodZaDuzo, "Łączna liczba Kryteriów nie może przekraczać 24.");
        }

        if (kryteria.GroupBy(k => (k.Wariant, k.NrKryterium)).Any(g => g.Count() > 1))
        {
            throw new NiezmiennikNaruszonyException(KodNiespojne, "Numer kryterium musi być unikalny w wariancie.");
        }

        foreach (var wariant in kryteria.GroupBy(k => k.Wariant))
        {
            if (wariant.Sum(k => k.PunktyMax) != punktyMax || wariant.Count(k => k.CzyWynikKoncowy) > 1)
            {
                throw new NiezmiennikNaruszonyException(KodNiespojne, "Suma punktów Kryteriów wariantu musi być równa punktom Zadania, a wynik końcowy może być tylko jeden.");
            }

            var numery = wariant.Select(k => k.NrKryterium).ToHashSet();
            if (wariant.Any(k => k.WymagaNrKryterium is { } wymagane && !numery.Contains(wymagane)))
            {
                throw new NiezmiennikNaruszonyException(KodNiespojne, "Kryterium wymaga kryterium, którego nie ma w wariancie.");
            }
        }
    }
}
