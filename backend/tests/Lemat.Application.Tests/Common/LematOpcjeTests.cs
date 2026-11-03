// @Role: Testy jednostkowe LematOpcje: wartości domyślne (tabela 22), nadpisanie zmiennymi LEMAT_*, walidacja startu | @DependsOn: Lemat.Application.Common | @UsedBy: dotnet test | @Task: TASK-004, TASK-127
using System.Collections;
using Lemat.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lemat.Application.Tests.Common;

public sealed class LematOpcjeTests
{
    private static LematOpcje Poprawne() => new() { Srodowisko = "Development", Magazyn = { Sciezka = "storage/magazyn" } };

    private static ValidateOptionsResult Waliduj(LematOpcje opcje) => new LematOpcjeWalidator().Validate(null, opcje);

    [Fact]
    public void WartosciDomyslneOdpowiadajaTabeli22()
    {
        var o = new LematOpcje();

        o.Zdjecia.MaksNaPrzeslanie.ShouldBe(5);
        o.Zdjecia.MaksRozmiarMb.ShouldBe(10);
        o.Zdjecia.DozwoloneTypyMime.ShouldBe(["image/jpeg", "image/png", "image/webp"]);
        o.Zdjecia.MaksPrzeslanNaRozwiazanie.ShouldBe(3);
        o.Zdjecia.MaksPonowienRozpoznania.ShouldBe(5);
        o.Transkrypcja.MaksKrokow.ShouldBe(40);
        o.Transkrypcja.ProgSprawdzOdczyt.ShouldBe(0.80);
        o.Transkrypcja.TauOdczyt.ShouldBe(0.20);
        o.Transkrypcja.TauFr25.ShouldBe(0.20);
        o.Ocenianie.MaksParQoc05.ShouldBe(12);
        o.Ocenianie.TerminMiekkiMinimumSekund.ShouldBe(10);
        o.Ocenianie.TerminMiekkiBazaSekund.ShouldBe(28);
        o.Ocenianie.TerminTwardySekund.ShouldBe(120);
        o.Ocenianie.TimeoutAgentaWyjasniajacegoSekund.ShouldBe(8);
        o.Strumien.HeartbeatSekund.ShouldBe(15);
        o.Progi.PoczatkowyJev.ShouldBe(0.95);
        o.Progi.PoczatkowyZapasowy.ShouldBe(0.98);
        o.Progi.OdsetekKontroliLosowej.ShouldBe(0.0);
        o.Poprawki.ZawszeDoWeryfikacji.ShouldBeTrue();
        o.Konta.AutozatwierdzanieNauczycieli.ShouldBeFalse();
        o.Modele.Dostawca.ShouldBe("mock");
        o.Magazyn.Typ.ShouldBe("lokalny");
        o.Obrazy.Typ.ShouldBe("prosta");
        o.Symbolika.Typ.ShouldBe("prosta");
        o.Email.Typ.ShouldBe("log");
    }

    [Fact]
    public void ZmienneLematNadpisujaKonfiguracje()
    {
        var zmienne = new Hashtable
        {
            ["LEMAT_MODELE_DOSTAWCA"] = "anthropic",
            ["LEMAT_MODELE_KLUCZ_API"] = "klucz",
            ["LEMAT_POTWIERDZONA_RETENCJA_DOSTAWCY"] = "true",
            ["LEMAT_MAGAZYN_SCIEZKA"] = "/dane",
            ["LEMAT_BAZA"] = "Host=x",
            ["LEMAT_MOCK_OPOZNIENIE_MS"] = "300",
            ["LEMAT_PG_PORT"] = "5433",
            ["LEMAT_KURSOR_KLUCZ"] = " ",
        };
        var konfiguracja = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Lemat:Modele:Dostawca"] = "mock", ["Lemat:Magazyn:Sciezka"] = "stara" })
            .DodajZmienneLemat(zmienne)
            .Build();
        var uslugi = new ServiceCollection().AddLematOpcje(konfiguracja, "Development");

        using var dostawca = uslugi.BuildServiceProvider();
        var o = dostawca.GetRequiredService<LematOpcje>();

        o.Modele.Dostawca.ShouldBe("anthropic");
        o.Modele.KluczApi.ShouldBe("klucz");
        o.Modele.PotwierdzonaRetencjaDostawcy.ShouldBeTrue();
        o.Modele.MockOpoznienieMs.ShouldBe(300);
        o.Magazyn.Sciezka.ShouldBe("/dane");
        o.Baza.Polaczenie.ShouldBe("Host=x");
        o.Kursor.Klucz.ShouldBeEmpty();
        o.Srodowisko.ShouldBe("Development");
    }

    [Fact]
    public void SrodowiskoZLematSrodowiskoMaPierwszenstwoPrzedHostem()
    {
        var konfiguracja = new ConfigurationBuilder()
            .DodajZmienneLemat(new Hashtable { ["LEMAT_SRODOWISKO"] = "E2E", ["LEMAT_MAGAZYN_SCIEZKA"] = "storage" })
            .Build();
        using var dostawca = new ServiceCollection().AddLematOpcje(konfiguracja, "Development").BuildServiceProvider();

        dostawca.GetRequiredService<LematOpcje>().Srodowisko.ShouldBe("E2E");
    }

    [Fact]
    public void ZmienneNaKluczeNieMapujeNieznanychZmiennych()
    {
        var klucze = LematKonfiguracja.ZmienneNaKlucze(new Hashtable { ["LEMAT_NIEZNANA"] = "x", ["PATH"] = "y" });

        klucze.ShouldBeEmpty();
    }

    [Fact]
    public void PoprawneOpcjeDevelopmentPrzechodzaWalidacje() => Waliduj(Poprawne()).Succeeded.ShouldBeTrue();

    [Fact]
    public void MockWProductionJestOdrzucony()
    {
        var o = Poprawne();
        o.Srodowisko = "Production";
        o.Kursor.Klucz = "k";
        o.Jwt.Klucz = "j";

        var wynik = Waliduj(o);

        wynik.Failed.ShouldBeTrue();
        wynik.FailureMessage!.ShouldContain("mock");
    }

    [Fact]
    public void DostawcaInnyNizMockBezPotwierdzeniaRetencjiJestOdrzucony()
    {
        var o = Poprawne();
        o.Modele.Dostawca = "openai-zgodny";
        o.Modele.KluczApi = "k";
        o.Modele.Endpoint = "https://llm.example";

        var wynik = Waliduj(o);

        wynik.Failed.ShouldBeTrue();
        wynik.FailureMessage!.ShouldContain("PotwierdzonaRetencjaDostawcy");

        o.Modele.PotwierdzonaRetencjaDostawcy = true;
        Waliduj(o).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void BrakSciezkiMagazynuLokalnegoJestOdrzucony()
    {
        var o = Poprawne();
        o.Magazyn.Sciezka = "";

        Waliduj(o).FailureMessage!.ShouldContain("Magazyn:Sciezka");
    }

    [Fact]
    public void AutozatwierdzanieNauczycieliWProductionJestOdrzucone()
    {
        var o = Poprawne();
        o.Srodowisko = "Production";
        o.Modele.Dostawca = "anthropic";
        o.Modele.KluczApi = "k";
        o.Modele.PotwierdzonaRetencjaDostawcy = true;
        o.Kursor.Klucz = "k";
        o.Jwt.Klucz = "j";
        o.Magazyn.Klucz = "m";
        Waliduj(o).Succeeded.ShouldBeTrue();

        o.Konta.AutozatwierdzanieNauczycieli = true;

        Waliduj(o).FailureMessage!.ShouldContain("AutozatwierdzanieNauczycieli");
    }

    [Fact]
    public void ProductionWymagaKluczaMagazynu()
    {
        var o = Poprawne();
        o.Srodowisko = "Production";
        o.Modele.Dostawca = "anthropic";
        o.Modele.KluczApi = "k";
        o.Modele.PotwierdzonaRetencjaDostawcy = true;
        o.Kursor.Klucz = "k";
        o.Jwt.Klucz = "j";

        Waliduj(o).FailureMessage!.ShouldContain("Magazyn:Klucz");

        o.Magazyn.Klucz = "m";

        Waliduj(o).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("nieznany")]
    [InlineData("")]
    public void NieznanyDostawcaJestOdrzucony(string dostawca)
    {
        var o = Poprawne();
        o.Modele.Dostawca = dostawca;

        Waliduj(o).Failed.ShouldBeTrue();
    }

    [Fact]
    public void WartosciPozaZakresemSaOdrzucane()
    {
        var o = Poprawne();
        o.Zdjecia.MaksNaPrzeslanie = 0;
        o.Progi.PoczatkowyJev = 0.5;
        o.Progi.OdsetekKontroliLosowej = 1.5;

        var wynik = Waliduj(o);

        wynik.Failed.ShouldBeTrue();
        wynik.Failures.Count().ShouldBe(3);
    }

    [Fact]
    public void ValidateOnStartWyjatekPrzyBledzieKonfiguracji()
    {
        var konfiguracja = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Lemat:Srodowisko"] = "Production" })
            .Build();
        using var dostawca = new ServiceCollection().AddLematOpcje(konfiguracja, "Production").BuildServiceProvider();

        Should.Throw<OptionsValidationException>(() => dostawca.GetRequiredService<LematOpcje>());
    }
}
