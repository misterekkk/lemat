// @Role: Testy agregatu Uzytkownik: założenie konta (status startowy wg roli), normalizacja e-mail, walidacja nazw, przejścia statusów, karencja i przywrócenie | @DependsOn: Lemat.Domain.Accounts.Uzytkownik | @UsedBy: dotnet test | @Task: TASK-016
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Tests.Accounts;

public sealed class UserTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static Uzytkownik Konto(RolaUzytkownika rola = RolaUzytkownika.Uczen, string email = "jan@example.com") =>
        Uzytkownik.Zaloz(new UzytkownikId(GeneratorUuid.Nowy(TimeProvider.System)), rola, email, "Jan", "Kowalski", Teraz);

    [Theory]
    [InlineData(RolaUzytkownika.Uczen, StatusKonta.Aktywne)]
    [InlineData(RolaUzytkownika.Nauczyciel, StatusKonta.OczekujeNaZatwierdzenie)]
    [InlineData(RolaUzytkownika.Administrator, StatusKonta.Aktywne)]
    public void StatusStartowyZalezyOdRoli(RolaUzytkownika rola, StatusKonta oczekiwany)
    {
        var konto = Konto(rola);

        konto.Status.ShouldBe(oczekiwany);
        konto.Version.ShouldBe(1);
        konto.OkresKarencji.ShouldBeNull();
        konto.CzyProcesyWstrzymane.ShouldBeFalse();
    }

    [Theory]
    [InlineData("  Jan.Kowalski@Example.COM ", "jan.kowalski@example.com")]
    [InlineData("A@B.pl", "a@b.pl")]
    public void EmailJestNormalizowanyJakWCitext(string wejscie, string oczekiwany) =>
        Konto(email: wejscie).Email.ShouldBe(oczekiwany);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bez-malpy")]
    [InlineData("@example.com")]
    [InlineData("jan@")]
    [InlineData("jan@@example.com")]
    [InlineData("jan@example")]
    [InlineData("jan@.com")]
    [InlineData("jan kowalski@example.com")]
    [InlineData("jan@example.")]
    public void NieprawidlowyEmailJestOdrzucany(string email) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Konto(email: email)).KodBledu.ShouldBe("EMAIL_NIEPRAWIDLOWY");

    [Fact]
    public void ZaDlugiEmailJestOdrzucany()
    {
        var email = new string('a', 250) + "@b.pl";

        Should.Throw<NiezmiennikNaruszonyException>(() => Konto(email: email)).KodBledu.ShouldBe("EMAIL_NIEPRAWIDLOWY");
    }

    [Theory]
    [InlineData("", "Kowalski", "IMIE_NIEPRAWIDLOWE")]
    [InlineData("  ", "Kowalski", "IMIE_NIEPRAWIDLOWE")]
    [InlineData("Jan", "", "NAZWISKO_NIEPRAWIDLOWE")]
    public void ImieINazwiskoSaWymagane(string imie, string nazwisko, string kod) =>
        Should.Throw<NiezmiennikNaruszonyException>(() =>
            Uzytkownik.Zaloz(new UzytkownikId(Guid.NewGuid()), RolaUzytkownika.Uczen, "a@b.pl", imie, nazwisko, Teraz))
            .KodBledu.ShouldBe(kod);

    [Fact]
    public void LimityDlugosciImieniaINazwiska()
    {
        Should.NotThrow(() => Uzytkownik.Zaloz(new UzytkownikId(Guid.NewGuid()), RolaUzytkownika.Uczen, "a@b.pl", new string('i', 60), new string('n', 80), Teraz));
        Should.Throw<NiezmiennikNaruszonyException>(() =>
            Uzytkownik.Zaloz(new UzytkownikId(Guid.NewGuid()), RolaUzytkownika.Uczen, "a@b.pl", new string('i', 61), "N", Teraz));
        Should.Throw<NiezmiennikNaruszonyException>(() =>
            Uzytkownik.Zaloz(new UzytkownikId(Guid.NewGuid()), RolaUzytkownika.Uczen, "a@b.pl", "I", new string('n', 81), Teraz));
    }

    [Fact]
    public void NieznanaRolaJestOdrzucana() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Konto((RolaUzytkownika)99)).KodBledu.ShouldBe("ROLA_NIEZNANA");

    [Fact]
    public void ZatwierdzenieDotyczyTylkoOczekujacegoNauczyciela()
    {
        var nauczyciel = Konto(RolaUzytkownika.Nauczyciel);
        nauczyciel.Zatwierdz();
        nauczyciel.Status.ShouldBe(StatusKonta.Aktywne);

        Should.Throw<NiezmiennikNaruszonyException>(nauczyciel.Zatwierdz).KodBledu.ShouldBe("KONTO_NIEPRAWIDLOWY_STAN");
        Should.Throw<NiezmiennikNaruszonyException>(Konto().Zatwierdz).KodBledu.ShouldBe("KONTO_NIEPRAWIDLOWY_STAN");
    }

    [Fact]
    public void BlokadaIOdblokowanie()
    {
        var konto = Konto();

        konto.Zablokuj(Teraz);
        konto.Status.ShouldBe(StatusKonta.Zablokowane);
        konto.Zdarzenia.ShouldHaveSingleItem().ShouldBeOfType<KontoZablokowane>();
        Should.Throw<NiezmiennikNaruszonyException>(() => konto.Zablokuj(Teraz));

        konto.Odblokuj();
        konto.Status.ShouldBe(StatusKonta.Aktywne);
        Should.Throw<NiezmiennikNaruszonyException>(konto.Odblokuj).KodBledu.ShouldBe("KONTO_NIEPRAWIDLOWY_STAN");
    }

    [Fact]
    public void WniosekOUsuniecieStartujeKarencje30Dni()
    {
        var konto = Konto();

        konto.ZlozWniosekOUsuniecie(Teraz);

        konto.Status.ShouldBe(StatusKonta.Usuwane);
        konto.CzyProcesyWstrzymane.ShouldBeTrue();
        konto.OkresKarencji.ShouldNotBeNull().UsuniecieZaplanowaneAt.ShouldBe(Teraz.AddDays(30));
        konto.OkresKarencji.StatusPrzedWnioskiem.ShouldBe(StatusKonta.Aktywne);
        konto.Zdarzenia.ShouldHaveSingleItem().ShouldBeOfType<KontoUsuwane>();
        Should.Throw<NiezmiennikNaruszonyException>(() => konto.ZlozWniosekOUsuniecie(Teraz)).KodBledu.ShouldBe("KONTO_JUZ_USUWANE");
        Should.Throw<NiezmiennikNaruszonyException>(() => konto.Zablokuj(Teraz));
    }

    [Theory]
    [InlineData(RolaUzytkownika.Uczen, StatusKonta.Aktywne)]
    [InlineData(RolaUzytkownika.Nauczyciel, StatusKonta.OczekujeNaZatwierdzenie)]
    public void PrzywrocenieOdtwarzaStatusSprzedWniosku(RolaUzytkownika rola, StatusKonta oczekiwany)
    {
        var konto = Konto(rola);
        konto.ZlozWniosekOUsuniecie(Teraz);

        konto.Przywroc(Teraz.AddDays(29));

        konto.Status.ShouldBe(oczekiwany);
        konto.OkresKarencji.ShouldBeNull();
        konto.Zdarzenia[^1].ShouldBeOfType<KontoPrzywrocone>();
    }

    [Fact]
    public void PrzywrocenieZablokowanegoKontaZachowujeBlokade()
    {
        var konto = Konto();
        konto.Zablokuj(Teraz);
        konto.ZlozWniosekOUsuniecie(Teraz);

        konto.Przywroc(Teraz.AddDays(1));

        konto.Status.ShouldBe(StatusKonta.Zablokowane);
    }

    [Fact]
    public void PrzywrocenieNiejestMozliwePoKarencjiAniBezWniosku()
    {
        var konto = Konto();
        Should.Throw<NiezmiennikNaruszonyException>(() => konto.Przywroc(Teraz)).KodBledu.ShouldBe("PRZYWROCENIE_NIEMOZLIWE");

        konto.ZlozWniosekOUsuniecie(Teraz);
        Should.Throw<NiezmiennikNaruszonyException>(() => konto.Przywroc(Teraz.AddDays(30))).KodBledu.ShouldBe("PRZYWROCENIE_NIEMOZLIWE");
    }

    [Fact]
    public void OkresKarencjiOdrzucaStatusUsuwane() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => new OkresKarencji(Teraz, StatusKonta.Usuwane)).KodBledu.ShouldBe("STATUS_PRZED_WNIOSKIEM");
}
