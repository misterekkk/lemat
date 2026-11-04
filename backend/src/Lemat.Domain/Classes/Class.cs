// @Role: Agregat Klasa (FR-30): grupa Uczniów jednego Nauczyciela z Kodem zaproszenia, Członkostwem, archiwizacją i Przekazaniem Klasy | @DependsOn: Lemat.Domain.Common.Agregat, KodZaproszenia, CzlonkostwoKlasy, NauczycielId, UczenId | @UsedBy: IKlasaRepozytorium, KlasyFasada, konfiguracja EF | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Classes;

public sealed class Klasa : Agregat<KlasaId>
{
    public const int MaksDlugoscNazwy = 80;

    private readonly List<CzlonkostwoKlasy> _czlonkowie = [];

    private Klasa()
    {
    }

    /// <summary>Prowadzący Nauczyciel; <c>null</c> tylko po usunięciu konta bez Przekazania (ADR-044), wtedy Klasa jest zarchiwizowana.</summary>
    public NauczycielId? NauczycielId { get; private set; }

    public string Nazwa { get; private set; } = string.Empty;

    public PoziomMatury PoziomMatury { get; private init; }

    /// <summary>Kod zaproszenia składany z dwóch kolumn (<c>kod_zaproszenia</c>, <c>kod_wazny_do</c>); pola prywatne mapuje EF Core.</summary>
    public KodZaproszenia KodZaproszenia => new(KodWartosc, KodWaznyDo);

    public bool Zarchiwizowana { get; private set; }

    private string KodWartosc { get; set; } = string.Empty;

    private DateTimeOffset KodWaznyDo { get; set; }

    public DateTimeOffset? ZarchiwizowanoAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public IReadOnlyList<CzlonkostwoKlasy> Czlonkowie => _czlonkowie;

    public static Klasa Zaloz(KlasaId id, NauczycielId nauczyciel, string nazwa, PoziomMatury poziom, KodZaproszenia kod, DateTimeOffset teraz) =>
        new Klasa()
        {
            Id = id,
            NauczycielId = nauczyciel,
            Nazwa = WalidujNazwe(nazwa),
            PoziomMatury = poziom,
            CreatedAt = teraz,
        }.UstawKod(kod);

    public bool CzyCzlonek(UczenId uczen) => _czlonkowie.Exists(c => c.UczenId == uczen);

    /// <summary>
    /// Uczeń dołącza kodem. Idempotentne dla istniejącego członka (zwraca <c>false</c>); kod nieważny albo Klasa zarchiwizowana kończą się kodem błędu.
    /// </summary>
    public bool Dolacz(UczenId uczen, string? kod, DateTimeOffset teraz)
    {
        WymagajAktywnej();
        if (!KodZaproszenia.CzyWazny(kod, teraz))
        {
            throw new NiezmiennikNaruszonyException("KOD_ZAPROSZENIA_NIEWAZNY", "Kod zaproszenia jest nieważny albo wygasł.");
        }

        if (CzyCzlonek(uczen))
        {
            return false;
        }

        _czlonkowie.Add(new CzlonkostwoKlasy(uczen, teraz));
        return true;
    }

    /// <summary>Uczeń opuszcza Klasę (FR-30); zwraca <c>false</c>, gdy nie był członkiem.</summary>
    public bool Opusc(UczenId uczen) => _czlonkowie.RemoveAll(c => c.UczenId == uczen) > 0;

    /// <summary>Generuje nowy kod (stary przestaje działać).</summary>
    public void OdnowKod(KodZaproszenia nowy)
    {
        WymagajAktywnej();
        UstawKod(nowy);
    }

    /// <summary>Unieważnia bieżący kod; dołączenie wymaga potem nowego kodu.</summary>
    public void UniewaznKod(DateTimeOffset teraz)
    {
        WymagajAktywnej();
        UstawKod(KodZaproszenia.Uniewazniony(teraz));
    }

    public void ZmienNazwe(string nazwa)
    {
        WymagajAktywnej();
        Nazwa = WalidujNazwe(nazwa);
    }

    public void Archiwizuj(DateTimeOffset teraz)
    {
        if (Zarchiwizowana)
        {
            return;
        }

        Zarchiwizowana = true;
        ZarchiwizowanoAt = teraz;
    }

    /// <summary>Przekazanie Klasy (ADR-044): nowy Nauczyciel przejmuje prowadzenie.</summary>
    public void Przekaz(NauczycielId nowy, DateTimeOffset teraz)
    {
        WymagajAktywnej();
        var poprzedni = NauczycielId;
        NauczycielId = nowy;
        Zglos(new KlasaPrzekazana(Id, poprzedni, nowy, teraz));
    }

    /// <summary>Usunięcie konta Nauczyciela bez Przekazania: niezmiennik <c>NauczycielId != null ∨ Zarchiwizowana</c>.</summary>
    public void ArchiwizujPoUsunieciuNauczyciela(DateTimeOffset teraz)
    {
        NauczycielId = null;
        Archiwizuj(teraz);
    }

    private Klasa UstawKod(KodZaproszenia kod)
    {
        KodWartosc = kod.Wartosc;
        KodWaznyDo = kod.WaznyDo;
        return this;
    }

    private static string WalidujNazwe(string? nazwa)
    {
        var przyciete = (nazwa ?? string.Empty).Trim();
        if (przyciete.Length is < 1 or > MaksDlugoscNazwy)
        {
            throw new NiezmiennikNaruszonyException("KLASA_NAZWA_NIEPRAWIDLOWA", "Nazwa Klasy ma 1–80 znaków.");
        }

        return przyciete;
    }

    private void WymagajAktywnej()
    {
        if (Zarchiwizowana)
        {
            throw new NiezmiennikNaruszonyException("KLASA_ZARCHIWIZOWANA", "Klasa jest zarchiwizowana.");
        }
    }
}
