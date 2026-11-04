// @Role: Testy agregatu Ścieżka nauki: jeden element PROPONOWANY naraz (ELEMENT_JUZ_PROPONOWANY), wygaszanie przeterminowanego przed nowym, numeracja kolejności, termin Przypisania, Seria (≤ 8 elementów, 3 porażki, 7 dni), Rekomendacja i kody reguł | @DependsOn: Lemat.Domain.Recommendations | @UsedBy: dotnet test | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;

namespace Lemat.Domain.Tests.Recommendations;

public sealed class LearningPathTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static SciezkaNauki Nowa() => SciezkaNauki.Zaloz(new SciezkaNaukiId(Guid.NewGuid()), new UczenId(Guid.NewGuid()), Teraz);

    private static PropozycjaElementu Propozycja(
        TypElementuSciezki typ = TypElementuSciezki.Zadanie,
        CelElementu cel = CelElementu.Nauka,
        bool wSerii = false,
        DateTimeOffset? wygasaAt = null) =>
        LearningPathElementTests.Propozycja(typ, cel, wSerii, wygasaAt);

    [Fact]
    public void PierwszyElementJestProponowanyZNumeremJedenIOknemDwudziestuCzterechGodzin()
    {
        var sciezka = Nowa();

        var element = sciezka.Zaproponuj(Propozycja(), Teraz);

        element.Status.ShouldBe(StatusElementuSciezki.Proponowany);
        element.Kolejnosc.ShouldBe(1);
        element.WygasaAt.ShouldBe(Teraz.AddHours(24));
        sciezka.OstatniaKolejnosc.ShouldBe(1);
        sciezka.Proponowany.ShouldBe(element);
        sciezka.Znajdz(element.Id).ShouldBe(element);
    }

    [Fact]
    public void DrugiProponowanyElementJestOdrzucony()
    {
        var sciezka = Nowa();
        sciezka.Zaproponuj(Propozycja(), Teraz);

        Should.Throw<NiezmiennikNaruszonyException>(() => sciezka.Zaproponuj(Propozycja(), Teraz.AddHours(1)))
            .KodBledu.ShouldBe("ELEMENT_JUZ_PROPONOWANY");
    }

    [Fact]
    public void PrzeterminowanyProponowanyWygasaPrzedDodaniemNowego()
    {
        var sciezka = Nowa();
        var stary = sciezka.Zaproponuj(Propozycja(), Teraz);

        var nowy = sciezka.Zaproponuj(Propozycja(), Teraz.AddHours(25));

        stary.Status.ShouldBe(StatusElementuSciezki.Wygasly);
        nowy.Status.ShouldBe(StatusElementuSciezki.Proponowany);
        nowy.Kolejnosc.ShouldBe(2);
    }

    [Fact]
    public void RozpoczetyElementNieBlokujeNowegoProponowanego()
    {
        var sciezka = Nowa();
        var pierwszy = sciezka.Zaproponuj(Propozycja(), Teraz);
        pierwszy.Rozpocznij(new ZadanieWersjaId(Guid.NewGuid()), Teraz.AddMinutes(1));

        var drugi = sciezka.Zaproponuj(Propozycja(), Teraz.AddMinutes(2));

        drugi.Kolejnosc.ShouldBe(2);
        sciezka.AktywneElementy.Count(e => e.CzyAktywny).ShouldBe(2);
    }

    [Fact]
    public void TerminPrzypisaniaSkracaOknoProponowanego()
    {
        var sciezka = Nowa();
        var termin = Teraz.AddHours(5);

        var element = sciezka.Zaproponuj(Propozycja(wygasaAt: termin), Teraz);

        element.WygasaAt.ShouldBe(termin);
    }

    [Fact]
    public void PowtorkaDotyczyWylacznieZadania() =>
        Should.Throw<NiezmiennikNaruszonyException>(() =>
                Nowa().Zaproponuj(Propozycja(TypElementuSciezki.WkladkaTeoretyczna, CelElementu.Powtorka), Teraz))
            .KodBledu.ShouldBe("CEL_ELEMENTU_NIEDOZWOLONY");

    [Fact]
    public void ElementTypuZadanieWymagaZadaniaAWkladkaWkladki()
    {
        var bezZadania = Propozycja() with { ZadanieId = null };
        var bezWkladki = Propozycja(TypElementuSciezki.WkladkaTeoretyczna) with { WkladkaTeoretycznaId = null };

        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa().Zaproponuj(bezZadania, Teraz)).KodBledu.ShouldBe("ELEMENT_BEZ_ZADANIA");
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa().Zaproponuj(bezWkladki, Teraz)).KodBledu.ShouldBe("ELEMENT_BEZ_WKLADKI");
    }

    // ---------- Seria ----------

    [Fact]
    public void SeriaLiczyElementyWSerii()
    {
        var sciezka = Nowa();
        sciezka.RozpocznijSerie(new UmiejetnoscId(501), new PoziomTrudnosci(2), Teraz);

        var element = sciezka.Zaproponuj(Propozycja(wSerii: true), Teraz);

        element.WSerii.ShouldBeTrue();
        sciezka.Seria!.LiczbaElementow.ShouldBe(1);
        sciezka.Seria.PoziomTrudnosci.Wartosc.ShouldBe(2);
        sciezka.Seria.KolejnePorazki.ShouldBe(0);
    }

    [Fact]
    public void ElementBezAktywnejSeriiNieJestWSerii()
    {
        var element = Nowa().Zaproponuj(Propozycja(wSerii: true), Teraz);

        element.WSerii.ShouldBeFalse();
    }

    [Fact]
    public void SeriaMaNajwyzejOsiemElementow()
    {
        var sciezka = Nowa();
        sciezka.RozpocznijSerie(new UmiejetnoscId(501), new PoziomTrudnosci(3), Teraz);
        for (var i = 0; i < 8; i++)
        {
            sciezka.Zaproponuj(Propozycja(wSerii: true), Teraz.AddMinutes(i)).Pomin(Teraz.AddMinutes(i));
        }

        sciezka.Seria!.LiczbaElementow.ShouldBe(8);
        sciezka.Seria.CzyDoZakonczenia(Teraz).ShouldBeTrue();
        Should.Throw<NiezmiennikNaruszonyException>(() => sciezka.Zaproponuj(Propozycja(wSerii: true), Teraz.AddMinutes(9)))
            .KodBledu.ShouldBe("SERIA_PELNA");
    }

    [Fact]
    public void SeriaKonczySiePoTrzechPorazkachAlboSiedmiuDniach()
    {
        var sciezka = Nowa();
        sciezka.RozpocznijSerie(new UmiejetnoscId(501), new PoziomTrudnosci(3), Teraz);

        sciezka.ZarejestrujWynikSerii(false);
        sciezka.ZarejestrujWynikSerii(false);
        sciezka.Seria!.CzyDoZakonczenia(Teraz).ShouldBeFalse();
        sciezka.ZarejestrujWynikSerii(false);

        sciezka.Seria!.KolejnePorazki.ShouldBe(3);
        sciezka.Seria.CzyDoZakonczenia(Teraz).ShouldBeTrue();
        sciezka.ZarejestrujWynikSerii(true, new PoziomTrudnosci(4));
        sciezka.Seria!.KolejnePorazki.ShouldBe(0);
        sciezka.Seria.PoziomTrudnosci.Wartosc.ShouldBe(4);
        sciezka.Seria.CzyDoZakonczenia(Teraz.AddDays(6)).ShouldBeFalse();
        sciezka.Seria.CzyDoZakonczenia(Teraz.AddDays(7)).ShouldBeTrue();
    }

    [Fact]
    public void DrugaSeriaWymagaZakonczeniaPierwszej()
    {
        var sciezka = Nowa();
        sciezka.RozpocznijSerie(new UmiejetnoscId(501), new PoziomTrudnosci(3), Teraz);

        Should.Throw<NiezmiennikNaruszonyException>(() => sciezka.RozpocznijSerie(new UmiejetnoscId(502), new PoziomTrudnosci(3), Teraz))
            .KodBledu.ShouldBe("SERIA_AKTYWNA");

        sciezka.ZakonczSerie();
        sciezka.Seria.ShouldBeNull();
        sciezka.RozpocznijSerie(new UmiejetnoscId(502), new PoziomTrudnosci(3), Teraz);
        sciezka.Seria!.UmiejetnoscId.ShouldBe(new UmiejetnoscId(502));
    }

    [Fact]
    public void WynikSeriiBezAktywnejSeriiJestZabroniony() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa().ZarejestrujWynikSerii(true)).KodBledu.ShouldBe("SERIA_NIEAKTYWNA");

    // ---------- Rekomendacja ----------

    [Fact]
    public void RekomendacjaWymagaUzasadnieniaIPrawdopodobienstwaWZakresie()
    {
        var element = new ElementSciezkiId(Guid.NewGuid());

        Should.Throw<NiezmiennikNaruszonyException>(() => Utworz(element, "  ")).KodBledu.ShouldBe("UZASADNIENIE_WYMAGANE");
        Should.Throw<NiezmiennikNaruszonyException>(() => Utworz(element, "ok", 1.2m)).KodBledu.ShouldBe("PRAWDOPODOBIENSTWO_POZA_ZAKRESEM");

        var rekomendacja = Utworz(element, " Powtórka: ostatnie ćwiczenie było 7 dni temu. ", 0.66666m);
        rekomendacja.Uzasadnienie.ShouldBe("Powtórka: ostatnie ćwiczenie było 7 dni temu.");
        rekomendacja.PrzewidywanePSukcesu.ShouldBe(0.667m);
        rekomendacja.ElementSciezkiId.ShouldBe(element);
    }

    [Fact]
    public void KodyReguljSaWzajemnieJednoznacznePokrywajaWszystkieWartosciIMajaFormatZDdl()
    {
        var wszystkie = Enum.GetValues<RegulaRekomendacji>();

        var kody = wszystkie.Select(KodyRegulRekomendacji.Kod).ToList();

        kody.Distinct().Count().ShouldBe(wszystkie.Length);
        kody.ShouldAllBe(k => k.StartsWith("R-", StringComparison.Ordinal));
        kody.ShouldContain("R-TRUDNOSC_W_GORE");
        kody.ShouldContain("R-WKLADKA-DIAG");
        kody.ShouldContain("R-JEV-ZADANIE_MATURALNE");
        wszystkie.ShouldAllBe(r => KodyRegulRekomendacji.Parsuj(KodyRegulRekomendacji.Kod(r)) == r);
        Should.Throw<ArgumentOutOfRangeException>(() => KodyRegulRekomendacji.Parsuj("R-NIEZNANA"));
    }

    private static Rekomendacja Utworz(ElementSciezkiId element, string uzasadnienie, decimal? p = 0.5m) =>
        Rekomendacja.Utworz(
            new RekomendacjaId(Guid.NewGuid()),
            new UczenId(Guid.NewGuid()),
            element,
            StrategiaRekomendacji.PolitykaElo,
            RegulaRekomendacji.Powtorka,
            "{}",
            "{}",
            p,
            uzasadnienie,
            "{}",
            Teraz);
}
