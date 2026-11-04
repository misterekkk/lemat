// @Role: Testy agregatu Nauczyciel: flagi, Weryfikator globalny (A-13, A-14, umowa poufności), Recenzent treści, zdarzenie odebrania uprawnienia | @DependsOn: Lemat.Domain.Accounts.Nauczyciel | @UsedBy: dotnet test | @Task: TASK-016
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Tests.Accounts;

public sealed class TeacherTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static Nauczyciel Nowy() => Nauczyciel.Utworz(
        Uzytkownik.Zaloz(new UzytkownikId(GeneratorUuid.Nowy(TimeProvider.System)), RolaUzytkownika.Nauczyciel, "n@example.com", "Anna", "Lis", Teraz));

    [Fact]
    public void NauczycielStartujeBezUprawnien()
    {
        var nauczyciel = Nowy();

        nauczyciel.WeryfikatorGlobalny.ShouldBeFalse();
        nauczyciel.RecenzentTresci.ShouldBeFalse();
        nauczyciel.DeklarowaneMinutyWeryfikacji.ShouldBe(0);
        nauczyciel.UmowaPoufnosciAt.ShouldBeNull();
    }

    [Fact]
    public void NauczycielWymagaKontaORoliNauczyciel() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Nauczyciel.Utworz(
            Uzytkownik.Zaloz(new UzytkownikId(Guid.NewGuid()), RolaUzytkownika.Uczen, "u@example.com", "U", "U", Teraz)))
            .KodBledu.ShouldBe("KONTO_NIEPRAWIDLOWA_ROLA");

    [Fact]
    public void NadanieWeryfikatoraGlobalnegoUstawiaFlageMinutyIUmowe()
    {
        var nauczyciel = Nowy();

        nauczyciel.NadajWeryfikatoraGlobalnego(120, StatusKonta.Aktywne, prowadziKlase: false, 0, Teraz);

        nauczyciel.WeryfikatorGlobalny.ShouldBeTrue();
        nauczyciel.DeklarowaneMinutyWeryfikacji.ShouldBe(120);
        nauczyciel.UmowaPoufnosciAt.ShouldBe(Teraz);
    }

    [Theory]
    [InlineData(89, "DEKLAROWANY_CZAS_ZA_KROTKI")]
    [InlineData(481, "DEKLAROWANY_CZAS_ZA_KROTKI")]
    public void MinutyWeryfikatoraGlobalnegoMiesciaSieW90Do480(int minuty, string kod) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowy().NadajWeryfikatoraGlobalnego(minuty, StatusKonta.Aktywne, false, 0, Teraz))
            .KodBledu.ShouldBe(kod);

    [Fact]
    public void GranicaDeklarowanychMinut()
    {
        Should.NotThrow(() => Nowy().NadajWeryfikatoraGlobalnego(90, StatusKonta.Aktywne, false, 0, Teraz));
        Should.NotThrow(() => Nowy().NadajWeryfikatoraGlobalnego(480, StatusKonta.Aktywne, false, 0, Teraz));
    }

    [Fact]
    public void WeryfikatorGlobalnyWymagaUmowyPoufnosciIKontaAktywnego()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowy().NadajWeryfikatoraGlobalnego(120, StatusKonta.Aktywne, false, 0, null))
            .KodBledu.ShouldBe("UMOWA_POUFNOSCI_WYMAGANA");
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowy().NadajWeryfikatoraGlobalnego(120, StatusKonta.Zablokowane, false, 0, Teraz))
            .KodBledu.ShouldBe("KONTO_NIEAKTYWNE");
    }

    [Fact]
    public void LimitLacznegoObciazeniaDotyczyTylkoProwadzacegoKlase()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowy().NadajWeryfikatoraGlobalnego(120, StatusKonta.Aktywne, true, 61, Teraz))
            .KodBledu.ShouldBe("LIMIT_OBCIAZENIA_WERYFIKATORA");
        Should.NotThrow(() => Nowy().NadajWeryfikatoraGlobalnego(120, StatusKonta.Aktywne, true, 60, Teraz));
        Should.NotThrow(() => Nowy().NadajWeryfikatoraGlobalnego(120, StatusKonta.Aktywne, false, 500, Teraz));
    }

    [Fact]
    public void OdebranieWeryfikatoraGlobalnegoZerujeMinutyIZglaszaZdarzenie()
    {
        var nauczyciel = Nowy();
        nauczyciel.NadajWeryfikatoraGlobalnego(120, StatusKonta.Aktywne, false, 0, Teraz);

        nauczyciel.OdbierzWeryfikatoraGlobalnego(Teraz);

        nauczyciel.WeryfikatorGlobalny.ShouldBeFalse();
        nauczyciel.DeklarowaneMinutyWeryfikacji.ShouldBe(0);
        var zdarzenie = nauczyciel.Zdarzenia.ShouldHaveSingleItem().ShouldBeOfType<UprawnienieOdebrane>();
        zdarzenie.Uprawnienie.ShouldBe(UprawnienieNauczyciela.WeryfikatorGlobalny);
    }

    [Fact]
    public void OdebranieNieprzyznanegoUprawnieniaNieZglaszaZdarzenia()
    {
        var nauczyciel = Nowy();

        nauczyciel.OdbierzWeryfikatoraGlobalnego(Teraz);

        nauczyciel.Zdarzenia.ShouldBeEmpty();
    }

    [Fact]
    public void RecenzentTresciTylkoDlaKontaAktywnego()
    {
        var nauczyciel = Nowy();
        Should.Throw<NiezmiennikNaruszonyException>(() => nauczyciel.NadajRecenzentaTresci(StatusKonta.OczekujeNaZatwierdzenie))
            .KodBledu.ShouldBe("KONTO_NIEAKTYWNE");

        nauczyciel.NadajRecenzentaTresci(StatusKonta.Aktywne);
        nauczyciel.RecenzentTresci.ShouldBeTrue();
        nauczyciel.OdbierzRecenzentaTresci();
        nauczyciel.RecenzentTresci.ShouldBeFalse();
    }
}
