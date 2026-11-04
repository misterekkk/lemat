// @Role: Agregat Wkładka teoretyczna: krótka teoria Umiejętności (Markdown + LaTeX, 1-15 min), szkic Agenta generującego lub treść ręczna, cykl SZKIC -> DO_PRZEGLADU -> OPUBLIKOWANA/WYCOFANA | @DependsOn: Lemat.Domain.Common.Agregat, UmiejetnoscId, StatusWkladki, UzytkownikId | @UsedBy: repozytoria Treści, ITresciFasada, moduł Rekomendacje (R-WKLADKA) | @Task: TASK-017
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public sealed class WkladkaTeoretyczna : Agregat<WkladkaTeoretycznaId>
{
    public const int MaksDlugoscTresci = 12000;
    public const int MinCzasMin = 1;
    public const int MaksCzasMin = 15;

    private WkladkaTeoretyczna()
    {
    }

    public UmiejetnoscId UmiejetnoscId { get; private init; }

    public string Tytul { get; private set; } = string.Empty;

    public string TrescMd { get; private set; } = string.Empty;

    public int SzacowanyCzasMin { get; private set; }

    /// <summary>Prawda = szkic Agenta generującego.</summary>
    public bool Wygenerowana { get; private init; }

    /// <summary>Prawda = praca GENERUJ_SZKIC_WKLADKI (E-20); wlicza się do limitu odrzuceń 13.3.</summary>
    public bool WygenerowanaAutomatycznie { get; private init; }

    public decimal KosztGenerowaniaPln { get; private init; }

    public StatusWkladki Status { get; private set; }

    /// <summary>Recenzent treści lub Administrator, który opublikował Wkładkę.</summary>
    public UzytkownikId? ZatwierdzonaPrzez { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static WkladkaTeoretyczna UtworzSzkic(
        WkladkaTeoretycznaId id,
        UmiejetnoscId umiejetnosc,
        string tytul,
        string trescMd,
        int szacowanyCzasMin,
        bool wygenerowana,
        bool wygenerowanaAutomatycznie,
        decimal kosztGenerowaniaPln,
        DateTimeOffset teraz)
    {
        if (wygenerowanaAutomatycznie && !wygenerowana)
        {
            throw new NiezmiennikNaruszonyException("WKLADKA_AUTOMATYCZNA_BEZ_GENEROWANIA", "Wkładka generowana automatycznie jest szkicem Agenta generującego.");
        }

        if (kosztGenerowaniaPln < 0m)
        {
            throw new NiezmiennikNaruszonyException("WKLADKA_KOSZT_UJEMNY", "Koszt generowania nie może być ujemny.");
        }

        Waliduj(tytul, trescMd, szacowanyCzasMin);
        return new WkladkaTeoretyczna
        {
            Id = id,
            UmiejetnoscId = umiejetnosc,
            Tytul = tytul,
            TrescMd = trescMd,
            SzacowanyCzasMin = szacowanyCzasMin,
            Wygenerowana = wygenerowana,
            WygenerowanaAutomatycznie = wygenerowanaAutomatycznie,
            KosztGenerowaniaPln = kosztGenerowaniaPln,
            Status = StatusWkladki.Szkic,
            CreatedAt = teraz,
            UpdatedAt = teraz,
        };
    }

    /// <summary>Edycja treści dozwolona tylko w SZKIC i DO_PRZEGLADU (7.x: opublikowana jest niezmienna, korekta = nowa Wkładka).</summary>
    public void Edytuj(string tytul, string trescMd, int szacowanyCzasMin)
    {
        if (Status is not (StatusWkladki.Szkic or StatusWkladki.DoPrzegladu))
        {
            throw new NiezmiennikNaruszonyException("WKLADKA_NIEPRAWIDLOWY_STAN", "Edytować można tylko szkic i Wkładkę do przeglądu.");
        }

        Waliduj(tytul, trescMd, szacowanyCzasMin);
        Tytul = tytul;
        TrescMd = trescMd;
        SzacowanyCzasMin = szacowanyCzasMin;
    }

    public void PrzekazDoPrzegladu() => Przejdz(StatusWkladki.DoPrzegladu, StatusWkladki.Szkic);

    /// <summary>Publikuje Wkładkę (recenzent treści lub Administrator); unikalność opublikowanej na Umiejętność pilnuje indeks <c>ux_wkladka_opublikowana</c>.</summary>
    public void Opublikuj(UzytkownikId zatwierdzajacy)
    {
        Przejdz(StatusWkladki.Opublikowana, StatusWkladki.Szkic, StatusWkladki.DoPrzegladu);
        ZatwierdzonaPrzez = zatwierdzajacy;
    }

    public void Wycofaj() => Przejdz(StatusWkladki.Wycofana, StatusWkladki.Szkic, StatusWkladki.DoPrzegladu, StatusWkladki.Opublikowana);

    private void Przejdz(StatusWkladki docelowy, params StatusWkladki[] dozwoloneZrodla)
    {
        if (!dozwoloneZrodla.Contains(Status))
        {
            throw new NiezmiennikNaruszonyException("WKLADKA_NIEPRAWIDLOWY_STAN", $"Przejście {Status} -> {docelowy} jest niedozwolone.");
        }

        Status = docelowy;
    }

    private static void Waliduj(string tytul, string trescMd, int szacowanyCzasMin)
    {
        if (string.IsNullOrWhiteSpace(tytul))
        {
            throw new NiezmiennikNaruszonyException("WKLADKA_TYTUL_WYMAGANY", "Wkładka wymaga tytułu.");
        }

        if (string.IsNullOrWhiteSpace(trescMd) || trescMd.Length > MaksDlugoscTresci)
        {
            throw new NiezmiennikNaruszonyException("WKLADKA_TRESC_NIEPRAWIDLOWA", "Treść Wkładki ma mieć 1-12000 znaków.");
        }

        if (szacowanyCzasMin is < MinCzasMin or > MaksCzasMin)
        {
            throw new NiezmiennikNaruszonyException("WKLADKA_CZAS_POZA_ZAKRESEM", "Szacowany czas czytania to 1-15 minut.");
        }
    }
}
