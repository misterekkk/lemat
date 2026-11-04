// @Role: Agregat Uczeń: poziom matury, termin egzaminu, wybrany Dział, oświadczenie 16+ (A-06), zgoda na trening, Grupa eksperymentu (fnv1a) | @DependsOn: Lemat.Domain.Common.Agregat, UzytkownikId, GrupaEksperymentu, PoziomMatury, DzialId | @UsedBy: repozytoria Kont, IKontaFasada, moduły Profil, Rekomendacje, Klasy | @Task: TASK-016, TASK-124
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Accounts;

public sealed class Uczen : Agregat<UczenId>
{
    private Uczen()
    {
    }

    /// <summary>NULL do czasu FR-02.</summary>
    public PoziomMatury? PoziomMatury { get; private set; }

    public DateOnly? TerminEgzaminu { get; private set; }

    /// <summary>FR-09; <c>null</c> = wszystkie Działy.</summary>
    public DzialId? WybranyDzialId { get; private set; }

    /// <summary>Niezmiennik: zawsze <c>true</c> (A-06, CHECK 7.3).</summary>
    public bool OswiadczenieWieku16 { get; private init; }

    public bool ZgodaNaTrening { get; private set; }

    public GrupaEksperymentu GrupaEksperymentu { get; private init; }

    /// <summary>Tworzy Ucznia konta o roli UCZEN; bez oświadczenia 16+ rejestracja jest niemożliwa (A-06).</summary>
    public static Uczen Utworz(Uzytkownik konto, bool oswiadczenieWieku16)
    {
        ArgumentNullException.ThrowIfNull(konto);
        if (konto.Rola != RolaUzytkownika.Uczen)
        {
            throw new NiezmiennikNaruszonyException("KONTO_NIEPRAWIDLOWA_ROLA", "Uczeń wymaga konta o roli UCZEN.");
        }

        if (!oswiadczenieWieku16)
        {
            throw new NiezmiennikNaruszonyException("OSWIADCZENIE_WIEKU_WYMAGANE", "Rejestracja Ucznia wymaga oświadczenia o ukończeniu 16 lat.");
        }

        return new Uczen
        {
            Id = UczenId.Z(konto.Id),
            OswiadczenieWieku16 = true,
            GrupaEksperymentu = GrupaEksperymentu.Dla(konto.Id),
        };
    }

    public void UstawPoziomITermin(PoziomMatury poziom, DateOnly termin, DateOnly dzis)
    {
        if (termin < dzis)
        {
            throw new NiezmiennikNaruszonyException("TERMIN_EGZAMINU_W_PRZESZLOSCI", "Termin egzaminu nie może być w przeszłości.");
        }

        PoziomMatury = poziom;
        TerminEgzaminu = termin;
    }

    /// <summary>Zmiana samego Poziomu matury (PATCH /uczniowie/ja, FR-02); skutki dla Profilu i Ścieżki nauki są poza zakresem POC.</summary>
    public void UstawPoziom(PoziomMatury poziom) => PoziomMatury = poziom;

    /// <summary>Ustawia albo czyści (<c>null</c>) Termin egzaminu (FR-02).</summary>
    public void UstawTermin(DateOnly? termin) => TerminEgzaminu = termin;

    public void WybierzDzial(DzialId? dzial) => WybranyDzialId = dzial;

    public void UstawZgodeNaTrening(bool zgoda) => ZgodaNaTrening = zgoda;
}
