// @Role: Testy maszyny stanów Elementu Ścieżki nauki 8.5: pełna macierz (każde dozwolone i każde zabronione przejście), okna wygasania, reguły Wkładki (przeczytanie, 60 s), Odpowiedź na Zadanie zamknięte | @DependsOn: Lemat.Domain.Recommendations.SciezkaNauki, ElementSciezki | @UsedBy: dotnet test | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;

namespace Lemat.Domain.Tests.Recommendations;

public sealed class LearningPathElementTests
{
    private const string KodStanu = "ELEMENT_NIEPRAWIDLOWY_STAN";

    private static readonly DateTimeOffset Poczatek = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly ZadanieWersjaId Wersja = new(Guid.NewGuid());

    public enum Operacja
    {
        Rozpocznij,
        Ukoncz,
        Pomin,
        WygasWycofanie,
        PominPoWygasnieciu,
    }

    // ---------- macierz przejść dla Zadania ----------

    public static TheoryData<StatusElementuSciezki, Operacja, bool> Macierz() => new()
    {
        { StatusElementuSciezki.Proponowany, Operacja.Rozpocznij, true },
        { StatusElementuSciezki.Proponowany, Operacja.Ukoncz, false },
        { StatusElementuSciezki.Proponowany, Operacja.Pomin, true },
        { StatusElementuSciezki.Proponowany, Operacja.WygasWycofanie, true },
        { StatusElementuSciezki.Proponowany, Operacja.PominPoWygasnieciu, false },
        { StatusElementuSciezki.Rozpoczety, Operacja.Rozpocznij, false },
        { StatusElementuSciezki.Rozpoczety, Operacja.Ukoncz, true },
        { StatusElementuSciezki.Rozpoczety, Operacja.Pomin, true },
        { StatusElementuSciezki.Rozpoczety, Operacja.WygasWycofanie, false },
        { StatusElementuSciezki.Rozpoczety, Operacja.PominPoWygasnieciu, true },
        { StatusElementuSciezki.Ukonczony, Operacja.Rozpocznij, false },
        { StatusElementuSciezki.Ukonczony, Operacja.Ukoncz, false },
        { StatusElementuSciezki.Ukonczony, Operacja.Pomin, false },
        { StatusElementuSciezki.Ukonczony, Operacja.WygasWycofanie, false },
        { StatusElementuSciezki.Ukonczony, Operacja.PominPoWygasnieciu, false },
        { StatusElementuSciezki.Pominiety, Operacja.Rozpocznij, false },
        { StatusElementuSciezki.Pominiety, Operacja.Ukoncz, false },
        { StatusElementuSciezki.Pominiety, Operacja.Pomin, false },
        { StatusElementuSciezki.Pominiety, Operacja.WygasWycofanie, false },
        { StatusElementuSciezki.Pominiety, Operacja.PominPoWygasnieciu, false },
        { StatusElementuSciezki.Wygasly, Operacja.Rozpocznij, false },
        { StatusElementuSciezki.Wygasly, Operacja.Ukoncz, false },
        { StatusElementuSciezki.Wygasly, Operacja.Pomin, false },
        { StatusElementuSciezki.Wygasly, Operacja.WygasWycofanie, false },
        { StatusElementuSciezki.Wygasly, Operacja.PominPoWygasnieciu, false },
    };

    [Theory]
    [MemberData(nameof(Macierz))]
    public void PrzejsciaZadaniaSaZgodneZMaszynaStanow(StatusElementuSciezki status, Operacja operacja, bool dozwolone)
    {
        var element = ElementWStatusie(TypElementuSciezki.Zadanie, status);
        var teraz = operacja == Operacja.PominPoWygasnieciu ? element.WygasaAt : Poczatek.AddMinutes(30);

        if (dozwolone)
        {
            Wykonaj(element, operacja, teraz);
            element.Status.ShouldBe(StatusDocelowy(operacja));
        }
        else
        {
            var status0 = element.Status;
            Should.Throw<NiezmiennikNaruszonyException>(() => Wykonaj(element, operacja, teraz)).KodBledu.ShouldBe(KodStanu);
            element.Status.ShouldBe(status0);
        }
    }

    private static StatusElementuSciezki StatusDocelowy(Operacja operacja) => operacja switch
    {
        Operacja.Rozpocznij => StatusElementuSciezki.Rozpoczety,
        Operacja.Ukoncz => StatusElementuSciezki.Ukonczony,
        Operacja.Pomin or Operacja.PominPoWygasnieciu => StatusElementuSciezki.Pominiety,
        Operacja.WygasWycofanie => StatusElementuSciezki.Wygasly,
        _ => throw new ArgumentOutOfRangeException(nameof(operacja)),
    };

    private static void Wykonaj(ElementSciezki element, Operacja operacja, DateTimeOffset teraz)
    {
        switch (operacja)
        {
            case Operacja.Rozpocznij:
                element.Rozpocznij(Wersja, teraz);
                break;
            case Operacja.Ukoncz:
                element.Ukoncz(teraz);
                break;
            case Operacja.Pomin:
                element.Pomin(teraz);
                break;
            case Operacja.WygasWycofanie:
                element.Wygas(teraz, wycofanieZadania: true);
                break;
            case Operacja.PominPoWygasnieciu:
                element.PominPoWygasnieciu(teraz);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operacja));
        }
    }

    // ---------- okna i znaczniki czasu ----------

    [Fact]
    public void ProponowanyWygasaPoDwudziestuCzterechGodzinach()
    {
        var element = ElementWStatusie(TypElementuSciezki.Zadanie, StatusElementuSciezki.Proponowany);

        element.WygasaAt.ShouldBe(Poczatek.AddHours(24));
        Should.Throw<NiezmiennikNaruszonyException>(() => element.Wygas(Poczatek.AddHours(23))).KodBledu.ShouldBe(KodStanu);
        element.Wygas(Poczatek.AddHours(24));

        element.Status.ShouldBe(StatusElementuSciezki.Wygasly);
        element.ZakonczonoAt.ShouldBe(Poczatek.AddHours(24));
    }

    [Fact]
    public void RozpoczecieZadaniaDajeSiedemDniIZapisujeWersje()
    {
        var element = ElementWStatusie(TypElementuSciezki.Zadanie, StatusElementuSciezki.Proponowany);
        var teraz = Poczatek.AddHours(2);

        element.Rozpocznij(Wersja, teraz);

        element.WygasaAt.ShouldBe(teraz.AddDays(7));
        element.RozpoczetoAt.ShouldBe(teraz);
        element.ZadanieWersjaId.ShouldBe(Wersja);
    }

    [Fact]
    public void RozpoczecieWkladkiDajeDwadziesciaCzteryGodzinyBezWersji()
    {
        var element = ElementWStatusie(TypElementuSciezki.WkladkaTeoretyczna, StatusElementuSciezki.Proponowany);
        var teraz = Poczatek.AddHours(2);

        element.Rozpocznij(null, teraz);

        element.WygasaAt.ShouldBe(teraz.AddHours(24));
        element.ZadanieWersjaId.ShouldBeNull();
    }

    [Fact]
    public void RozpoczecieZadaniaWymagaWersji()
    {
        var element = ElementWStatusie(TypElementuSciezki.Zadanie, StatusElementuSciezki.Proponowany);

        Should.Throw<NiezmiennikNaruszonyException>(() => element.Rozpocznij(null, Poczatek.AddHours(1)))
            .KodBledu.ShouldBe("WERSJA_ZADANIA_WYMAGANA");
    }

    [Fact]
    public void RozpoczecieWygaslegoPoTerminieJestZabronione()
    {
        var element = ElementWStatusie(TypElementuSciezki.Zadanie, StatusElementuSciezki.Proponowany);

        Should.Throw<NiezmiennikNaruszonyException>(() => element.Rozpocznij(Wersja, element.WygasaAt))
            .KodBledu.ShouldBe(KodStanu);
    }

    [Fact]
    public void RozpoczetyPrzedTerminemNieMozeBycPominietyPrzezWygaszenie()
    {
        var element = ElementWStatusie(TypElementuSciezki.Zadanie, StatusElementuSciezki.Rozpoczety);

        Should.Throw<NiezmiennikNaruszonyException>(() => element.PominPoWygasnieciu(element.WygasaAt.AddSeconds(-1)))
            .KodBledu.ShouldBe(KodStanu);
    }

    [Fact]
    public void OdpowiedzNaZadanieZamknieteWProponowanymRozpoczynaIKonczyWJednejOperacji()
    {
        var element = ElementWStatusie(TypElementuSciezki.Zadanie, StatusElementuSciezki.Proponowany);
        var teraz = Poczatek.AddMinutes(5);

        element.RozpocznijIUkoncz(Wersja, teraz);

        element.Status.ShouldBe(StatusElementuSciezki.Ukonczony);
        element.RozpoczetoAt.ShouldBe(teraz);
        element.ZakonczonoAt.ShouldBe(teraz);
        element.ZadanieWersjaId.ShouldBe(Wersja);
    }

    // ---------- Wkładka ----------

    [Fact]
    public void PrzeczytanieUkonczaRozpoczetaWkladkeIJestIdempotentne()
    {
        var element = ElementWStatusie(TypElementuSciezki.WkladkaTeoretyczna, StatusElementuSciezki.Rozpoczety);

        element.Przeczytaj(Poczatek.AddMinutes(10));
        element.Przeczytaj(Poczatek.AddMinutes(20));

        element.Status.ShouldBe(StatusElementuSciezki.Ukonczony);
        element.ZakonczonoAt.ShouldBe(Poczatek.AddMinutes(10));
    }

    [Fact]
    public void PrzeczytanieProponowanejWkladkiJestZabronione()
    {
        var element = ElementWStatusie(TypElementuSciezki.WkladkaTeoretyczna, StatusElementuSciezki.Proponowany);

        Should.Throw<NiezmiennikNaruszonyException>(() => element.Przeczytaj(Poczatek)).KodBledu.ShouldBe(KodStanu);
    }

    [Fact]
    public void PrzeczytanieZadaniaJestZabronione()
    {
        var element = ElementWStatusie(TypElementuSciezki.Zadanie, StatusElementuSciezki.Rozpoczety);

        Should.Throw<NiezmiennikNaruszonyException>(() => element.Przeczytaj(Poczatek)).KodBledu.ShouldBe(KodStanu);
    }

    [Fact]
    public void ProsbaOKolejnyPoSzescdziesieciuSekundachUkonczaWkladke()
    {
        var element = ElementWStatusie(TypElementuSciezki.WkladkaTeoretyczna, StatusElementuSciezki.Rozpoczety);

        element.ZakonczWkladkePoProsbieOKolejny(element.RozpoczetoAt!.Value.AddSeconds(60));

        element.Status.ShouldBe(StatusElementuSciezki.Ukonczony);
    }

    [Fact]
    public void ProsbaOKolejnyPrzedSzescdziesieciomaSekundamiPomijaWkladke()
    {
        var element = ElementWStatusie(TypElementuSciezki.WkladkaTeoretyczna, StatusElementuSciezki.Rozpoczety);

        element.ZakonczWkladkePoProsbieOKolejny(element.RozpoczetoAt!.Value.AddSeconds(59));

        element.Status.ShouldBe(StatusElementuSciezki.Pominiety);
    }

    [Fact]
    public void ProsbaOKolejnyDlaZadaniaLubNierozpoczetejWkladkiJestZabroniona()
    {
        var zadanie = ElementWStatusie(TypElementuSciezki.Zadanie, StatusElementuSciezki.Rozpoczety);
        var wkladka = ElementWStatusie(TypElementuSciezki.WkladkaTeoretyczna, StatusElementuSciezki.Proponowany);

        Should.Throw<NiezmiennikNaruszonyException>(() => zadanie.ZakonczWkladkePoProsbieOKolejny(Poczatek.AddHours(1))).KodBledu.ShouldBe(KodStanu);
        Should.Throw<NiezmiennikNaruszonyException>(() => wkladka.ZakonczWkladkePoProsbieOKolejny(Poczatek.AddHours(1))).KodBledu.ShouldBe(KodStanu);
    }

    // ---------- pomocnicy ----------

    internal static ElementSciezki ElementWStatusie(TypElementuSciezki typ, StatusElementuSciezki status)
    {
        var sciezka = SciezkaNauki.Zaloz(new SciezkaNaukiId(Guid.NewGuid()), new UczenId(Guid.NewGuid()), Poczatek);
        var element = sciezka.Zaproponuj(Propozycja(typ), Poczatek);
        var rozpoczecie = Poczatek.AddHours(1);
        switch (status)
        {
            case StatusElementuSciezki.Proponowany:
                break;
            case StatusElementuSciezki.Rozpoczety:
                element.Rozpocznij(typ == TypElementuSciezki.Zadanie ? Wersja : null, rozpoczecie);
                break;
            case StatusElementuSciezki.Ukonczony:
                element.Rozpocznij(typ == TypElementuSciezki.Zadanie ? Wersja : null, rozpoczecie);
                element.Ukoncz(rozpoczecie.AddMinutes(10));
                break;
            case StatusElementuSciezki.Pominiety:
                element.Pomin(rozpoczecie);
                break;
            case StatusElementuSciezki.Wygasly:
                element.Wygas(element.WygasaAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        return element;
    }

    internal static PropozycjaElementu Propozycja(
        TypElementuSciezki typ = TypElementuSciezki.Zadanie,
        CelElementu cel = CelElementu.Nauka,
        bool wSerii = false,
        DateTimeOffset? wygasaAt = null) =>
        new(
            typ,
            cel,
            new UmiejetnoscId(501),
            typ == TypElementuSciezki.Zadanie ? new ZadanieId(Guid.NewGuid()) : null,
            typ == TypElementuSciezki.WkladkaTeoretyczna ? new WkladkaTeoretycznaId(Guid.NewGuid()) : null,
            null,
            wSerii,
            wygasaAt);
}
