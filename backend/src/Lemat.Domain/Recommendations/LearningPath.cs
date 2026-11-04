// @Role: Agregat Ścieżka nauki (FR-05, 10.1): jeden PROPONOWANY element naraz (ELEMENT_JUZ_PROPONOWANY), Seria (ADR-030), numeracja kolejności, wygaszanie; ładuje wyłącznie elementy aktywne | @DependsOn: Lemat.Domain.Common.Agregat, UczenId, ElementSciezki, Seria, PropozycjaElementu | @UsedBy: ISciezkaNaukiRepozytorium, RekomendacjeFasada, konfiguracja EF | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Recommendations;

public sealed record PropozycjaElementu(
    TypElementuSciezki Typ,
    CelElementu Cel,
    UmiejetnoscId UmiejetnoscId,
    ZadanieId? ZadanieId,
    WkladkaTeoretycznaId? WkladkaTeoretycznaId,
    Guid? PrzypisanieId,
    bool WSerii,
    DateTimeOffset? WygasaAt);

public sealed class SciezkaNauki : Agregat<SciezkaNaukiId>
{
    private readonly List<ElementSciezki> _aktywne = [];

    private SciezkaNauki()
    {
    }

    /// <summary>Jedna Ścieżka nauki na Ucznia (UNIQUE).</summary>
    public UczenId UczenId { get; private init; }

    public int OstatniaKolejnosc { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Aktywna Seria albo <c>null</c>; składana z kolumn <c>seria_*</c> (CHECK: umiejętność ⇔ początek).</summary>
    public Seria? Seria =>
        SeriaUmiejetnoscId is { } umiejetnosc && SeriaRozpoczetaAt is { } rozpoczeta && SeriaPoziomTrudnosci is { } poziom
            ? new Seria(umiejetnosc, rozpoczeta, SeriaLiczbaElementow, new PoziomTrudnosci(poziom), SeriaKolejnePorazki)
            : null;

    /// <summary>Elementy PROPONOWANY i ROZPOCZETY (≤ 2 po wczytaniu, C6-15 p. 4) oraz zmienione w tej sesji.</summary>
    public IReadOnlyList<ElementSciezki> AktywneElementy => _aktywne;

    private UmiejetnoscId? SeriaUmiejetnoscId { get; set; }

    private DateTimeOffset? SeriaRozpoczetaAt { get; set; }

    private int SeriaLiczbaElementow { get; set; }

    private int? SeriaPoziomTrudnosci { get; set; }

    private int SeriaKolejnePorazki { get; set; }

    public static SciezkaNauki Zaloz(SciezkaNaukiId id, UczenId uczen, DateTimeOffset teraz) =>
        new() { Id = id, UczenId = uczen, CreatedAt = teraz };

    public ElementSciezki? Znajdz(ElementSciezkiId id) => _aktywne.Find(e => e.Id == id);

    /// <summary>Proponowany element, jeśli istnieje i nie wygasł.</summary>
    public ElementSciezki? Proponowany => _aktywne.Find(e => e.Status == StatusElementuSciezki.Proponowany);

    /// <summary>
    /// Dodaje element PROPONOWANY (+24 h albo termin Przypisania). Najpierw wygasza przeterminowany PROPONOWANY (kolejność z 15.1:
    /// zmiana starego wiersza przed INSERT nowego); gdy nadal istnieje PROPONOWANY: <c>ELEMENT_JUZ_PROPONOWANY</c>.
    /// </summary>
    public ElementSciezki Zaproponuj(PropozycjaElementu propozycja, DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(propozycja);
        foreach (var przeterminowany in _aktywne.Where(e => e.Status == StatusElementuSciezki.Proponowany && teraz >= e.WygasaAt))
        {
            przeterminowany.Wygas(teraz);
        }

        if (Proponowany is not null)
        {
            throw new NiezmiennikNaruszonyException("ELEMENT_JUZ_PROPONOWANY", "Ścieżka nauki ma już element PROPONOWANY.");
        }

        if (propozycja.Typ == TypElementuSciezki.WkladkaTeoretyczna && propozycja.Cel == CelElementu.Powtorka)
        {
            throw new NiezmiennikNaruszonyException("CEL_ELEMENTU_NIEDOZWOLONY", "Powtórka dotyczy wyłącznie Zadania.");
        }

        if (propozycja.Typ == TypElementuSciezki.Zadanie && propozycja.ZadanieId is null)
        {
            throw new NiezmiennikNaruszonyException("ELEMENT_BEZ_ZADANIA", "Element typu ZADANIE wymaga Zadania.");
        }

        if (propozycja.Typ == TypElementuSciezki.WkladkaTeoretyczna && propozycja.WkladkaTeoretycznaId is null)
        {
            throw new NiezmiennikNaruszonyException("ELEMENT_BEZ_WKLADKI", "Element typu WKLADKA_TEORETYCZNA wymaga Wkładki.");
        }

        var wSerii = propozycja.WSerii && Seria is not null;
        if (wSerii && Seria!.LiczbaElementow >= Seria.MaksElementow)
        {
            throw new NiezmiennikNaruszonyException("SERIA_PELNA", "Seria ma najwyżej 8 elementów; zakończ Serię przed kolejnym elementem.");
        }

        OstatniaKolejnosc++;
        var element = ElementSciezki.Nowy(OstatniaKolejnosc, propozycja, wSerii, teraz);
        _aktywne.Add(element);
        if (wSerii)
        {
            SeriaLiczbaElementow++;
        }

        return element;
    }

    public void RozpocznijSerie(UmiejetnoscId umiejetnosc, PoziomTrudnosci poziom, DateTimeOffset teraz)
    {
        if (Seria is not null)
        {
            throw new NiezmiennikNaruszonyException("SERIA_AKTYWNA", "Seria już trwa.");
        }

        SeriaUmiejetnoscId = umiejetnosc;
        SeriaRozpoczetaAt = teraz;
        SeriaLiczbaElementow = 0;
        SeriaPoziomTrudnosci = poziom.Wartosc;
        SeriaKolejnePorazki = 0;
    }

    /// <summary>Zapisuje wynik elementu Serii: sukces zeruje porażki, porażka je zwiększa; opcjonalnie zmienia Poziom trudności.</summary>
    public void ZarejestrujWynikSerii(bool sukces, PoziomTrudnosci? nowyPoziom = null)
    {
        if (Seria is null)
        {
            throw new NiezmiennikNaruszonyException("SERIA_NIEAKTYWNA", "Nie ma aktywnej Serii.");
        }

        SeriaKolejnePorazki = sukces ? 0 : SeriaKolejnePorazki + 1;
        if (nowyPoziom is { } poziom)
        {
            SeriaPoziomTrudnosci = poziom.Wartosc;
        }
    }

    public void ZakonczSerie()
    {
        SeriaUmiejetnoscId = null;
        SeriaRozpoczetaAt = null;
        SeriaLiczbaElementow = 0;
        SeriaPoziomTrudnosci = null;
        SeriaKolejnePorazki = 0;
    }
}
