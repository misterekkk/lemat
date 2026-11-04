// @Role: Testy agregatu Rozwiązanie: fabryka, każde dozwolone przejście POC (7.11), każde niedozwolone (teoria stan × operacja), idempotencja przejść pracy w tle, znaczniki czasu, czas systemowy i niezmienniki Zdjęcia | @DependsOn: Lemat.Domain.Solutions.Rozwiazanie, SolutionsTestData | @UsedBy: dotnet test | @Task: TASK-126
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;
using Lemat.Domain.Solutions;
using static Lemat.Domain.Tests.Solutions.SolutionsTestData;

namespace Lemat.Domain.Tests.Solutions;

public sealed class RozwiazanieTests
{
    public enum Operacja
    {
        RozpocznijRozpoznawanie,
        ZapiszTranskrypcje,
        ZapiszBlad,
        Zatwierdz,
        RozpocznijOcenianie,
        ZarejestrujOcene,
    }

    /// <summary>Stany, z których dana operacja jest dozwolona (diagram 7.11); pozostałe kombinacje są niedozwolone.</summary>
    private static readonly Dictionary<Operacja, StatusRozwiazania[]> Dozwolone = new()
    {
        [Operacja.RozpocznijRozpoznawanie] = [StatusRozwiazania.Przeslane, StatusRozwiazania.Rozpoznawanie],
        [Operacja.ZapiszTranskrypcje] = [StatusRozwiazania.Rozpoznawanie],
        [Operacja.ZapiszBlad] = [StatusRozwiazania.Przeslane, StatusRozwiazania.Rozpoznawanie],
        [Operacja.Zatwierdz] = [StatusRozwiazania.TranskrypcjaGotowa],
        [Operacja.RozpocznijOcenianie] = [StatusRozwiazania.Zatwierdzone, StatusRozwiazania.Ocenianie],
        [Operacja.ZarejestrujOcene] = [StatusRozwiazania.Ocenianie],
    };

    public static TheoryData<StatusRozwiazania, Operacja, bool> StanyIOperacje()
    {
        var dane = new TheoryData<StatusRozwiazania, Operacja, bool>();
        foreach (var status in Enum.GetValues<StatusRozwiazania>())
        {
            foreach (var operacja in Enum.GetValues<Operacja>())
            {
                dane.Add(status, operacja, Dozwolone[operacja].Contains(status));
            }
        }

        return dane;
    }

    private static void Wykonaj(Rozwiazanie r, Transkrypcja? t, Operacja operacja)
    {
        var idTranskrypcji = t?.Id ?? new TranskrypcjaId(Guid.NewGuid());
        var wersja = t?.Wersja ?? 1;
        switch (operacja)
        {
            case Operacja.RozpocznijRozpoznawanie:
                r.RozpocznijRozpoznawanie();
                break;
            case Operacja.ZapiszTranskrypcje:
                r.ZapiszTranskrypcjeAgenta(idTranskrypcji, wersja, Teraz.AddSeconds(3));
                break;
            case Operacja.ZapiszBlad:
                r.ZapiszBladRozpoznawania(KodBleduRozpoznawania.Nieczytelne);
                break;
            case Operacja.Zatwierdz:
                r.Zatwierdz(idTranskrypcji, wersja, Teraz.AddSeconds(10));
                break;
            case Operacja.RozpocznijOcenianie:
                r.RozpocznijOcenianie();
                break;
            case Operacja.ZarejestrujOcene:
                r.ZarejestrujOcene(Teraz.AddSeconds(12));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operacja));
        }
    }

    [Theory]
    [MemberData(nameof(StanyIOperacje))]
    public void PrzejsciaSaDozwolonePokrywajaSieZDiagramem(StatusRozwiazania status, Operacja operacja, bool dozwolona)
    {
        var (rozwiazanie, transkrypcja) = WStanie(status);

        if (dozwolona)
        {
            Should.NotThrow(() => Wykonaj(rozwiazanie, transkrypcja, operacja));
        }
        else
        {
            var wyjatek = Should.Throw<NiezmiennikNaruszonyException>(() => Wykonaj(rozwiazanie, transkrypcja, operacja));
            wyjatek.KodBledu.ShouldBe("ROZWIAZANIE_NIEPRAWIDLOWY_STAN");
            rozwiazanie.Status.ShouldBe(status);
        }
    }

    [Fact]
    public void PelnaSciezkaPoczatkowaKonczySieOcenionym()
    {
        var rozwiazanie = Nowe();
        rozwiazanie.Status.ShouldBe(StatusRozwiazania.Przeslane);

        rozwiazanie.RozpocznijRozpoznawanie();
        rozwiazanie.Status.ShouldBe(StatusRozwiazania.Rozpoznawanie);

        var transkrypcja = Transkrypcja(rozwiazanie.Id);
        rozwiazanie.ZapiszTranskrypcjeAgenta(transkrypcja.Id, 1, Teraz.AddSeconds(3));
        rozwiazanie.Status.ShouldBe(StatusRozwiazania.TranskrypcjaGotowa);
        rozwiazanie.NajnowszaTranskrypcjaId.ShouldBe(transkrypcja.Id);
        rozwiazanie.NajnowszaWersjaTranskrypcji.ShouldBe(1);
        rozwiazanie.TranskrypcjaGotowaAt.ShouldBe(Teraz.AddSeconds(3));

        rozwiazanie.Zatwierdz(transkrypcja.Id, 1, Teraz.AddSeconds(10));
        rozwiazanie.Status.ShouldBe(StatusRozwiazania.Zatwierdzone);
        rozwiazanie.ZatwierdzonaTranskrypcjaId.ShouldBe(transkrypcja.Id);
        rozwiazanie.ZatwierdzonoAt.ShouldBe(Teraz.AddSeconds(10));

        rozwiazanie.RozpocznijOcenianie();
        rozwiazanie.Status.ShouldBe(StatusRozwiazania.Ocenianie);

        rozwiazanie.ZarejestrujOcene(Teraz.AddSeconds(12));
        rozwiazanie.Status.ShouldBe(StatusRozwiazania.Ocenione);
        rozwiazanie.CzyTerminalne.ShouldBeTrue();
        rozwiazanie.OcenaWystawionaAt.ShouldBe(Teraz.AddSeconds(12));
    }

    [Fact]
    public void CzasSystemowyToSumaCzasuRozpoznaniaIOceny()
    {
        var (rozwiazanie, _) = WStanie(StatusRozwiazania.Ocenione);

        // 3 s rozpoznawania + (12 s − 10 s) oceniania.
        rozwiazanie.CzasSystemowy.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void BladRozpoznawaniaZapisujeKodIJestStanemKoncowymPoc()
    {
        var (rozwiazanie, _) = WStanie(StatusRozwiazania.Przeslane);

        rozwiazanie.ZapiszBladRozpoznawania(KodBleduRozpoznawania.PrzekroczonyCzas);

        rozwiazanie.Status.ShouldBe(StatusRozwiazania.BladRozpoznawania);
        rozwiazanie.KodBleduRozpoznawania.ShouldBe(KodBleduRozpoznawania.PrzekroczonyCzas);
        rozwiazanie.TranskrypcjaGotowaAt.ShouldBeNull();
        Should.Throw<NiezmiennikNaruszonyException>(() => rozwiazanie.RozpocznijRozpoznawanie());
    }

    [Fact]
    public void PowtorzoneRozpocznijRozpoznawanieIRozpocznijOcenianieSaIdempotentne()
    {
        var (rozwiazanie, _) = WStanie(StatusRozwiazania.Rozpoznawanie);
        Should.NotThrow(() => rozwiazanie.RozpocznijRozpoznawanie());
        rozwiazanie.Status.ShouldBe(StatusRozwiazania.Rozpoznawanie);

        var (ocenianie, _) = WStanie(StatusRozwiazania.Ocenianie);
        Should.NotThrow(() => ocenianie.RozpocznijOcenianie());
        ocenianie.Status.ShouldBe(StatusRozwiazania.Ocenianie);
    }

    [Fact]
    public void ZatwierdzenieNieNajnowszejTranskrypcjiToTranskrypcjaNieaktualna()
    {
        var (rozwiazanie, transkrypcja) = WStanie(StatusRozwiazania.TranskrypcjaGotowa);

        var innaWersja = Should.Throw<NiezmiennikNaruszonyException>(
            () => rozwiazanie.Zatwierdz(transkrypcja!.Id, 2, Teraz.AddSeconds(10)));
        var innyId = Should.Throw<NiezmiennikNaruszonyException>(
            () => rozwiazanie.Zatwierdz(new TranskrypcjaId(Guid.NewGuid()), 1, Teraz.AddSeconds(10)));

        innaWersja.KodBledu.ShouldBe("TRANSKRYPCJA_NIEAKTUALNA");
        innyId.KodBledu.ShouldBe("TRANSKRYPCJA_NIEAKTUALNA");
        rozwiazanie.Status.ShouldBe(StatusRozwiazania.TranskrypcjaGotowa);
        rozwiazanie.ZatwierdzonaTranskrypcjaId.ShouldBeNull();
    }

    [Fact]
    public void PonowneZatwierdzenieJestNiedozwolone()
    {
        var (rozwiazanie, transkrypcja) = WStanie(StatusRozwiazania.Zatwierdzone);

        var wyjatek = Should.Throw<NiezmiennikNaruszonyException>(
            () => rozwiazanie.Zatwierdz(transkrypcja!.Id, 1, Teraz.AddSeconds(11)));

        wyjatek.KodBledu.ShouldBe("ROZWIAZANIE_NIEPRAWIDLOWY_STAN");
    }

    [Fact]
    public void NoweRozwiazanieMaZnacznikiIKontekstProby()
    {
        var element = new ElementSciezkiId(Guid.NewGuid());
        var zdjecia = new[] { Zdjecie(1), Zdjecie(2) };
        var rozwiazanie = Rozwiazanie.Przeslij(
            new RozwiazanieId(Guid.NewGuid()),
            new UczenId(Guid.NewGuid()),
            new ZadanieId(Guid.NewGuid()),
            new ZadanieWersjaId(Guid.NewGuid()),
            RodzajZadania.OtwarteObliczeniowe,
            element,
            null,
            zdjecia,
            Teraz);

        rozwiazanie.Status.ShouldBe(StatusRozwiazania.Przeslane);
        rozwiazanie.Version.ShouldBe(1);
        rozwiazanie.ElementSciezkiId.ShouldBe(element);
        rozwiazanie.LiczbaPrzeslan.ShouldBe(1);
        rozwiazanie.LiczbaPonowienRozpoznania.ShouldBe(0);
        rozwiazanie.PrzeslanoAt.ShouldBe(Teraz);
        rozwiazanie.OstatnieRozpoznanieOdAt.ShouldBe(Teraz);
        rozwiazanie.TranskrypcjaGotowaAt.ShouldBeNull();
        rozwiazanie.ZatwierdzonoAt.ShouldBeNull();
        rozwiazanie.OcenaWystawionaAt.ShouldBeNull();
        rozwiazanie.CzasSystemowy.ShouldBeNull();
        rozwiazanie.Zdjecia.Count.ShouldBe(2);
        rozwiazanie.CzyTerminalne.ShouldBeFalse();
    }

    [Fact]
    public void VersionRosnieTylkoPrzezPotwierdzenieZapisu()
    {
        var rozwiazanie = Nowe();

        rozwiazanie.RozpocznijRozpoznawanie();
        rozwiazanie.Version.ShouldBe(1);

        rozwiazanie.PotwierdzZapis();
        rozwiazanie.Version.ShouldBe(2);
    }

    [Fact]
    public void ZadanieZamknieteNieMaRozwiazania()
    {
        var wyjatek = Should.Throw<NiezmiennikNaruszonyException>(() => Rozwiazanie.Przeslij(
            new RozwiazanieId(Guid.NewGuid()),
            new UczenId(Guid.NewGuid()),
            new ZadanieId(Guid.NewGuid()),
            new ZadanieWersjaId(Guid.NewGuid()),
            RodzajZadania.Zamkniete,
            new ElementSciezkiId(Guid.NewGuid()),
            null,
            [Zdjecie()],
            Teraz));

        wyjatek.KodBledu.ShouldBe("ZADANIE_ZAMKNIETE_BEZ_ROZWIAZANIA");
    }

    [Fact]
    public void BezKontekstuProbyRozwiazanieNiePowstaje()
    {
        var wyjatek = Should.Throw<NiezmiennikNaruszonyException>(() => Rozwiazanie.Przeslij(
            new RozwiazanieId(Guid.NewGuid()),
            new UczenId(Guid.NewGuid()),
            new ZadanieId(Guid.NewGuid()),
            new ZadanieWersjaId(Guid.NewGuid()),
            RodzajZadania.OtwarteObliczeniowe,
            null,
            null,
            [Zdjecie()],
            Teraz));

        wyjatek.KodBledu.ShouldBe("KONTEKST_PROBY_WYMAGANY");
    }

    [Fact]
    public void PrzypisanieWystarczaJakoKontekstProby()
    {
        var rozwiazanie = Rozwiazanie.Przeslij(
            new RozwiazanieId(Guid.NewGuid()),
            new UczenId(Guid.NewGuid()),
            new ZadanieId(Guid.NewGuid()),
            new ZadanieWersjaId(Guid.NewGuid()),
            RodzajZadania.OtwarteObliczeniowe,
            null,
            Guid.NewGuid(),
            [Zdjecie()],
            Teraz);

        rozwiazanie.PrzypisanieId.ShouldNotBeNull();
    }

    [Fact]
    public void PrzeslanieWymagaOdJednegoDoPieciuZdjecPierwszegoPrzeslania()
    {
        static Rozwiazanie Zdjeciami(IReadOnlyList<ZdjecieRozwiazania> zdjecia) => Nowe(zdjecia);

        Should.Throw<NiezmiennikNaruszonyException>(() => Zdjeciami([])).KodBledu.ShouldBe("PRZESLANIE_NIEPRAWIDLOWE");
        Should.Throw<NiezmiennikNaruszonyException>(() => Zdjeciami([.. Enumerable.Range(1, 5).Select(n => Zdjecie(n)), Zdjecie(5)]))
            .KodBledu.ShouldBe("PRZESLANIE_NIEPRAWIDLOWE");
        Should.Throw<NiezmiennikNaruszonyException>(() => Zdjeciami([Zdjecie(1, przeslanie: 2)]))
            .KodBledu.ShouldBe("PRZESLANIE_NIEPRAWIDLOWE");
        Should.NotThrow(() => Zdjeciami([.. Enumerable.Range(1, 5).Select(n => Zdjecie(n))]));
    }

    [Fact]
    public void TerminalneStanyPozaPocNieMajaPrzejsc()
    {
        foreach (var status in new[] { StatusRozwiazania.OczekujeNaWeryfikacje, StatusRozwiazania.Porzucone, StatusRozwiazania.Anulowane })
        {
            var (rozwiazanie, _) = WStanie(status);
            foreach (var operacja in Enum.GetValues<Operacja>())
            {
                Should.Throw<NiezmiennikNaruszonyException>(() => Wykonaj(rozwiazanie, null, operacja)).KodBledu
                    .ShouldBe("ROZWIAZANIE_NIEPRAWIDLOWY_STAN");
            }
        }
    }

    [Fact]
    public void DomenaNieSiegaPoZegarSystemowy()
    {
        var zrodla = Directory.EnumerateFiles(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Lemat.Domain", "Solutions")),
            "*.cs");

        foreach (var plik in zrodla)
        {
            var tresc = File.ReadAllText(plik);
            tresc.ShouldNotContain("DateTime.Now");
            tresc.ShouldNotContain("UtcNow");
        }
    }

    // ---------- Zdjęcie ----------

    [Fact]
    public void ZdjecieAkceptujeGranicznyOkresPrzechowywaniaDziewiecdziesieciuDni()
    {
        Should.NotThrow(() => Zdjecie(usunPo: Teraz.AddDays(90)));
    }

    [Fact]
    public void ZdjeciePrzechowywaneDluzejNizDziewiecdziesiatDniJestOdrzucone()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Zdjecie(usunPo: Teraz.AddDays(90).AddSeconds(1)))
            .KodBledu.ShouldBe("ZDJECIE_OKRES_PRZECHOWYWANIA");
    }

    [Theory]
    [InlineData(0, 1, 1, 100, 100, "ZDJECIE_PRZESLANIE_POZA_ZAKRESEM")]
    [InlineData(4, 1, 1, 100, 100, "ZDJECIE_PRZESLANIE_POZA_ZAKRESEM")]
    [InlineData(1, 0, 1, 100, 100, "ZDJECIE_KOLEJNOSC_POZA_ZAKRESEM")]
    [InlineData(1, 6, 1, 100, 100, "ZDJECIE_KOLEJNOSC_POZA_ZAKRESEM")]
    [InlineData(1, 1, 0, 100, 100, "ZDJECIE_ZA_DUZE")]
    [InlineData(1, 1, 10_485_761, 100, 100, "ZDJECIE_ZA_DUZE")]
    [InlineData(1, 1, 1, 0, 100, "ZDJECIE_WYMIARY_NIEPRAWIDLOWE")]
    [InlineData(1, 1, 1, 100, 40_000, "ZDJECIE_WYMIARY_NIEPRAWIDLOWE")]
    public void ZdjecieOdrzucaWartosciPozaZakresemDdl(int przeslanie, int kolejnosc, int rozmiar, int szerokosc, int wysokosc, string kod)
    {
        var wyjatek = Should.Throw<NiezmiennikNaruszonyException>(() => ZdjecieRozwiazania.Utworz(
            Guid.NewGuid(), przeslanie, kolejnosc, "zdjecia/k.jpg", rozmiar, new byte[32], szerokosc, wysokosc, Teraz.AddDays(1), Teraz));

        wyjatek.KodBledu.ShouldBe(kod);
    }

    [Fact]
    public void ZdjecieOdrzucaSkrotInnejDlugosciNizTrzydziesciDwa()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => ZdjecieRozwiazania.Utworz(
                Guid.NewGuid(), 1, 1, "zdjecia/k.jpg", 10, new byte[16], 10, 10, Teraz.AddDays(1), Teraz))
            .KodBledu.ShouldBe("ZDJECIE_SKROT_NIEPRAWIDLOWY");
    }
}
