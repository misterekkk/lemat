// @Role: Encja Element Ścieżki nauki z maszyną stanów 8.5 (PROPONOWANY, ROZPOCZETY, UKONCZONY, POMINIETY, WYGASLY): rozpoczęcie, ukończenie, pominięcie, wygaszanie, dowód przeczytania Wkładki | @DependsOn: ElementSciezkiId, TypElementuSciezki, StatusElementuSciezki, NiezmiennikNaruszonyException | @UsedBy: SciezkaNauki, konfiguracja EF, IRekomendacjeFasada | @Task: TASK-019
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Recommendations;

public sealed class ElementSciezki
{
    public const string KodNieprawidlowyStan = "ELEMENT_NIEPRAWIDLOWY_STAN";

    public static readonly TimeSpan OknoProponowanego = TimeSpan.FromHours(24);

    public static readonly TimeSpan OknoZadania = TimeSpan.FromDays(7);

    public static readonly TimeSpan OknoWkladki = TimeSpan.FromHours(24);

    /// <summary>Minimalny czas otwarcia Wkładki, po którym prośba o kolejny element liczy się jako przeczytanie.</summary>
    public static readonly TimeSpan MinimalnyCzasCzytaniaWkladki = TimeSpan.FromSeconds(60);

    private ElementSciezki()
    {
    }

    public ElementSciezkiId Id { get; private init; }

    public int Kolejnosc { get; private init; }

    public TypElementuSciezki Typ { get; private init; }

    public CelElementu Cel { get; private init; }

    public UmiejetnoscId UmiejetnoscId { get; private init; }

    public ZadanieId? ZadanieId { get; private init; }

    /// <summary>Wersja pokazana Uczniowi; ustawiana przy rozpoczęciu (C1-20).</summary>
    public ZadanieWersjaId? ZadanieWersjaId { get; private set; }

    public WkladkaTeoretycznaId? WkladkaTeoretycznaId { get; private init; }

    /// <summary>Identyfikator Przypisania (moduł Klasy, TASK-034); tylko reguła R-PRZYPISANIE (ADR-032).</summary>
    public Guid? PrzypisanieId { get; private init; }

    public bool WSerii { get; private init; }

    public StatusElementuSciezki Status { get; private set; }

    public DateTimeOffset WygasaAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? RozpoczetoAt { get; private set; }

    public DateTimeOffset? ZakonczonoAt { get; private set; }

    public bool CzyAktywny => Status is StatusElementuSciezki.Proponowany or StatusElementuSciezki.Rozpoczety;

    /// <summary>PROPONOWANY -> ROZPOCZETY (Zadanie +7 dni, Wkładka +24 h); Zadanie wymaga wersji pokazanej Uczniowi.</summary>
    public void Rozpocznij(ZadanieWersjaId? wersja, DateTimeOffset teraz)
    {
        if (Status != StatusElementuSciezki.Proponowany || teraz >= WygasaAt)
        {
            throw Blad("Element nie jest PROPONOWANY albo wygasł.");
        }

        if (Typ == TypElementuSciezki.Zadanie && wersja is null)
        {
            throw new NiezmiennikNaruszonyException("WERSJA_ZADANIA_WYMAGANA", "Rozpoczęcie Zadania wymaga wskazania wersji.");
        }

        ZadanieWersjaId = Typ == TypElementuSciezki.Zadanie ? wersja : null;
        Status = StatusElementuSciezki.Rozpoczety;
        RozpoczetoAt = teraz;
        WygasaAt = teraz + (Typ == TypElementuSciezki.Zadanie ? OknoZadania : OknoWkladki);
    }

    /// <summary>
    /// ROZPOCZETY -> UKONCZONY: Wynik zadania zarejestrowany (Zadanie) albo dowód przeczytania (Wkładka, <see cref="Przeczytaj"/>).
    /// </summary>
    public void Ukoncz(DateTimeOffset teraz)
    {
        WymagajStatusu(StatusElementuSciezki.Rozpoczety);
        Status = StatusElementuSciezki.Ukonczony;
        ZakonczonoAt = teraz;
    }

    /// <summary>Odpowiedź na Zadanie zamknięte w elemencie PROPONOWANY: rozpoczęcie i ukończenie w jednej operacji.</summary>
    public void RozpocznijIUkoncz(ZadanieWersjaId wersja, DateTimeOffset teraz)
    {
        Rozpocznij(wersja, teraz);
        Ukoncz(teraz);
    }

    /// <summary>POST przeczytanie: Wkładka ROZPOCZETY -> UKONCZONY (idempotentne dla już ukończonej).</summary>
    public void Przeczytaj(DateTimeOffset teraz)
    {
        WymagajWkladki();
        if (Status == StatusElementuSciezki.Ukonczony)
        {
            return;
        }

        Ukoncz(teraz);
    }

    /// <summary>Uczeń prosi o kolejny element: Wkładka otwarta &gt;= 60 s jest ukończona, krócej — pominięta (8.5).</summary>
    public void ZakonczWkladkePoProsbieOKolejny(DateTimeOffset teraz)
    {
        WymagajWkladki();
        WymagajStatusu(StatusElementuSciezki.Rozpoczety);
        if (teraz - RozpoczetoAt!.Value >= MinimalnyCzasCzytaniaWkladki)
        {
            Ukoncz(teraz);
        }
        else
        {
            Pomin(teraz);
        }
    }

    /// <summary>POST pominiecie: PROPONOWANY albo ROZPOCZETY -> POMINIETY.</summary>
    public void Pomin(DateTimeOffset teraz)
    {
        if (!CzyAktywny)
        {
            throw Blad("Pominąć można wyłącznie element PROPONOWANY albo ROZPOCZETY.");
        }

        Status = StatusElementuSciezki.Pominiety;
        ZakonczonoAt = teraz;
    }

    /// <summary>PROPONOWANY -> WYGASLY po <c>wygasa_at</c> albo przy wycofaniu Zadania (<paramref name="wycofanieZadania"/>).</summary>
    public void Wygas(DateTimeOffset teraz, bool wycofanieZadania = false)
    {
        WymagajStatusu(StatusElementuSciezki.Proponowany);
        if (!wycofanieZadania && teraz < WygasaAt)
        {
            throw Blad("Element PROPONOWANY jeszcze nie wygasł.");
        }

        Status = StatusElementuSciezki.Wygasly;
        ZakonczonoAt = teraz;
    }

    /// <summary>
    /// WYGAS_ELEMENTY: ROZPOCZETY po <c>wygasa_at</c> -> POMINIETY. Wywołujący sprawdza, że nie istnieje Rozwiązanie w stanie nieterminalnym.
    /// </summary>
    public void PominPoWygasnieciu(DateTimeOffset teraz)
    {
        WymagajStatusu(StatusElementuSciezki.Rozpoczety);
        if (teraz < WygasaAt)
        {
            throw Blad("Element ROZPOCZETY jeszcze nie wygasł.");
        }

        Status = StatusElementuSciezki.Pominiety;
        ZakonczonoAt = teraz;
    }

    internal static ElementSciezki Nowy(int kolejnosc, PropozycjaElementu propozycja, bool wSerii, DateTimeOffset teraz) => new()
    {
        Id = new ElementSciezkiId(Guid.CreateVersion7()),
        Kolejnosc = kolejnosc,
        Typ = propozycja.Typ,
        Cel = propozycja.Cel,
        UmiejetnoscId = propozycja.UmiejetnoscId,
        ZadanieId = propozycja.ZadanieId,
        WkladkaTeoretycznaId = propozycja.WkladkaTeoretycznaId,
        PrzypisanieId = propozycja.PrzypisanieId,
        WSerii = wSerii,
        Status = StatusElementuSciezki.Proponowany,
        CreatedAt = teraz,
        WygasaAt = propozycja.WygasaAt ?? teraz + OknoProponowanego,
    };

    private static NiezmiennikNaruszonyException Blad(string opis) => new(KodNieprawidlowyStan, opis);

    private void WymagajStatusu(StatusElementuSciezki oczekiwany)
    {
        if (Status != oczekiwany)
        {
            throw Blad($"Operacja wymaga statusu {oczekiwany}, a element ma {Status}.");
        }
    }

    private void WymagajWkladki()
    {
        if (Typ != TypElementuSciezki.WkladkaTeoretyczna)
        {
            throw Blad("Operacja dotyczy wyłącznie Wkładki teoretycznej.");
        }
    }
}
