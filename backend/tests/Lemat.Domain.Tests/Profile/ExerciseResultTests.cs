// @Role: Testy agregatu Wynik zadania: wagi (1,00/0,50/0,25), Ocena wstępna 0,5, brak Wyniku przy możliwym błędzie odczytu (FR-28), flaga kalibracji, Test diagnostyczny 1,00, unieważnianie i zawieszanie | @DependsOn: Lemat.Domain.Profile.WynikZadania | @UsedBy: dotnet test | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;

namespace Lemat.Domain.Tests.Profile;

public sealed class ExerciseResultTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly UczenId Uczen = new(Guid.NewGuid());

    private static WynikZadania? ZOceny(bool wstepna, bool bladOdczytu = false, bool pierwsza = true, decimal ulamek = 0.5m) =>
        WynikZadania.ZOceny(
            new WynikZadaniaId(Guid.NewGuid()), Uczen, new ZadanieId(Guid.NewGuid()), new ZadanieWersjaId(Guid.NewGuid()), Guid.NewGuid(), ulamek, wstepna, bladOdczytu, pierwsza, Teraz);

    private static WynikZadania ZOdpowiedzi(bool pierwsza = true) =>
        WynikZadania.ZOdpowiedzi(
            new WynikZadaniaId(Guid.NewGuid()), Uczen, new ZadanieId(Guid.NewGuid()), new ZadanieWersjaId(Guid.NewGuid()), Guid.NewGuid(), 1m, pierwsza, Teraz);

    [Fact]
    public void WynikZOcenyWstepnejMaWageZeroPiec()
    {
        var wynik = ZOceny(wstepna: true)!;

        wynik.Waga.ShouldBe(0.50m);
        wynik.LiczyDoTrudnosci.ShouldBeFalse();
        wynik.Zrodlo.ShouldBe(ZrodloWyniku.Ocena);
        wynik.OcenaId.ShouldNotBeNull();
        wynik.OdpowiedzId.ShouldBeNull();
    }

    [Fact]
    public void WynikZOcenyOstatecznejMaWageJeden()
    {
        var wynik = ZOceny(wstepna: false)!;

        wynik.Waga.ShouldBe(1.00m);
        wynik.LiczyDoTrudnosci.ShouldBeTrue();
    }

    [Fact]
    public void OcenaWstepnaZMozliwymBledemOdczytuNieTworzyWyniku() =>
        ZOceny(wstepna: true, bladOdczytu: true).ShouldBeNull();

    [Fact]
    public void OcenaOstatecznaTworzyWynikNawetPrzyFladzeBleduOdczytu() =>
        ZOceny(wstepna: false, bladOdczytu: true).ShouldNotBeNull();

    [Theory]
    [InlineData(false, true, 1.00)]
    [InlineData(false, false, 0.50)]
    [InlineData(true, true, 0.50)]
    [InlineData(true, false, 0.25)]
    public void WagaToIloczynWagiStatusuIProby(bool wstepna, bool pierwsza, double oczekiwana) =>
        WynikZadania.ObliczWage(wstepna, pierwsza).ShouldBe((decimal)oczekiwana);

    [Fact]
    public void KolejnaProbaMaWageZeroPiecIJestPozaKalibracja()
    {
        var wynik = ZOdpowiedzi(pierwsza: false);

        wynik.Waga.ShouldBe(0.50m);
        wynik.LiczyDoTrudnosci.ShouldBeFalse();
        wynik.Zrodlo.ShouldBe(ZrodloWyniku.Odpowiedz);
    }

    [Fact]
    public void WynikZTestuDiagnostycznegoMaWageJedenICzasZakonczeniaTestu()
    {
        var zakonczono = Teraz.AddHours(-1);
        var wynik = WynikZadania.ZTestuDiagnostycznego(
            new WynikZadaniaId(Guid.NewGuid()), Uczen, new ZadanieId(Guid.NewGuid()), new ZadanieWersjaId(Guid.NewGuid()), Guid.NewGuid(), new TestDiagnostycznyId(Guid.NewGuid()), 1m, zakonczono);

        wynik.Waga.ShouldBe(1.00m);
        wynik.LiczyDoTrudnosci.ShouldBeTrue();
        wynik.CreatedAt.ShouldBe(zakonczono);
        wynik.TestDiagnostycznyId.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void UlamekPozaZakresemJestOdrzucany(double ulamek) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => ZOceny(wstepna: false, ulamek: (decimal)ulamek))
            .KodBledu.ShouldBe("ULAMEK_POZA_ZAKRESEM");

    [Fact]
    public void UlamekJestZaokraglanyDoTrzechMiejsc() =>
        ZOceny(wstepna: false, ulamek: 0.66666m)!.Ulamek.ShouldBe(0.667m);

    [Fact]
    public void UniewaznienieJestIdempotentneIZachowujePierwszyPowod()
    {
        var wynik = ZOceny(wstepna: false)!;

        wynik.Uniewaznij(PowodUniewaznieniaWyniku.OcenaZastapiona, Teraz);
        wynik.Uniewaznij(PowodUniewaznieniaWyniku.ZgloszenieBleduOdczytu, Teraz.AddHours(1));

        wynik.UniewaznionyAt.ShouldBe(Teraz);
        wynik.PowodUniewaznienia.ShouldBe(PowodUniewaznieniaWyniku.OcenaZastapiona);
    }

    [Fact]
    public void ZawieszenieDotyczyWylacznieWynikuZOceny()
    {
        var zOceny = ZOceny(wstepna: false)!;
        zOceny.Zawies(Teraz);

        zOceny.PowodUniewaznienia.ShouldBe(PowodUniewaznieniaWyniku.ZgloszenieBleduOdczytu);
        Should.Throw<NiezmiennikNaruszonyException>(() => ZOdpowiedzi().Zawies(Teraz))
            .KodBledu.ShouldBe("ZAWIESZENIE_NIEDOZWOLONE");
    }

    [Fact]
    public void PrzetworzenieUniewaznieniaWymagaUniewaznionegoWyniku()
    {
        var wynik = ZOceny(wstepna: false)!;
        Should.Throw<NiezmiennikNaruszonyException>(() => wynik.OznaczUniewaznieniePrzetworzone(Teraz))
            .KodBledu.ShouldBe("WYNIK_NIEUNIEWAZNIONY");

        wynik.Uniewaznij(PowodUniewaznieniaWyniku.OcenaZastapiona, Teraz);
        wynik.OznaczUniewaznieniePrzetworzone(Teraz.AddMinutes(1));

        wynik.UniewaznieniePrzetworzoneAt.ShouldBe(Teraz.AddMinutes(1));
    }

    [Fact]
    public void ZastosowanieWProfiluJestIdempotentne()
    {
        var wynik = ZOdpowiedzi();
        wynik.OznaczZastosowany(Teraz);
        wynik.OznaczZastosowany(Teraz.AddHours(1));

        wynik.ZastosowanyWProfiluAt.ShouldBe(Teraz);
    }
}
