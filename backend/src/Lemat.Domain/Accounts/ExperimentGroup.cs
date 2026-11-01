// @Role: Obiekt wartości Grupa eksperymentu: procent_grupy_eksperymentu = fnv1a(uzytkownik_id) mod 100 (przydział A/B, deterministyczny) | @DependsOn: Lemat.Domain.Common.Fnv1a, Lemat.Domain.Accounts.UzytkownikId | @UsedBy: Lemat.Domain.Accounts.Uczen, konfiguracja EF uczen, IKontaFasada | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public readonly record struct GrupaEksperymentu
{
    public GrupaEksperymentu(int procentGrupyEksperymentu)
    {
        if (procentGrupyEksperymentu is < 0 or > 99)
        {
            throw new NiezmiennikNaruszonyException("GRUPA_EKSPERYMENTU_POZA_ZAKRESEM", "Procent grupy eksperymentu mieści się w zakresie 0-99.");
        }

        ProcentGrupyEksperymentu = procentGrupyEksperymentu;
    }

    public int ProcentGrupyEksperymentu { get; }

    /// <summary>fnv1a (32 bity) nad 16 bajtami UUID konta w kolejności sieciowej, modulo 100.</summary>
    public static GrupaEksperymentu Dla(UzytkownikId uzytkownik) =>
        new((int)(Fnv1a.Skrot32(uzytkownik.Wartosc) % 100));
}
