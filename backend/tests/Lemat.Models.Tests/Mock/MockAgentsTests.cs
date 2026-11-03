// @Role: Testy Mocków modeli POC: dane osadzone dla 5 Zadań (liczba kroków, punkty), szablon uzasadnienia z przykładu kontraktu 7.5, scenariusz rozpoznanie-nieczytelne, nieznane Zadanie, odrzucenie dostawcy innego niż mock | @DependsOn: MockAgentRozpoznajacy, MockAgentOceniajacy, MockDane, DodajModeleMock | @UsedBy: dotnet test | @Task: TASK-128
using Lemat.Application.Common;
using Lemat.Application.Models;
using Lemat.Domain.Content;
using Lemat.Domain.Grading;
using Lemat.Domain.Solutions;
using Lemat.Models.Mock;
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Models.Tests.Mock;

public sealed class MockAgentsTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ZadanieId Zadanie(int numer) => new(Guid.Parse($"a1000000-0000-4000-8000-00000000000{numer}"));

    private static readonly MockDane Dane = MockDane.Wczytaj();

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 3)]
    [InlineData(5, 2)]
    public async Task RozpoznawanieDajeKrokiZTabeli72(int zadanie, int liczbaKrokow)
    {
        var wynik = await new MockAgentRozpoznajacy(Dane).RozpoznajAsync(Zadanie(zadanie), "domyslny", Ct);

        wynik.Blad.ShouldBeNull();
        wynik.ZgodnoscZZadaniem.ShouldBe(0.95m);
        wynik.Kroki.Count.ShouldBe(liczbaKrokow);
        wynik.Kroki.Select(k => k.Nr).ShouldBe(Enumerable.Range(1, liczbaKrokow));
    }

    [Fact]
    public async Task ScenariuszNieczytelneZwracaBladBezKrokow()
    {
        var wynik = await new MockAgentRozpoznajacy(Dane).RozpoznajAsync(Zadanie(1), "rozpoznanie-nieczytelne", Ct);

        wynik.Blad.ShouldBe(KodBleduRozpoznawania.Nieczytelne);
        wynik.Kroki.ShouldBeEmpty();
    }

    [Fact]
    public async Task NieznaneZadanieRzucaWyjatekZamiastZmyslacDane()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            () => new MockAgentRozpoznajacy(Dane).RozpoznajAsync(new ZadanieId(Guid.NewGuid()), "domyslny", Ct));
    }

    [Fact]
    public async Task OcenaZadania2SkladaUzasadnienieJakWPrzykladzieKontraktu()
    {
        var wejscie = new WejscieOceny(
            Zadanie(2),
            [
                new KryteriumDoOceny(1, "Obliczenie wyróżnika trójmianu: $\\Delta = 1$.", 1),
                new KryteriumDoOceny(2, "Obliczenie pierwiastków równania: $x_1 = 2$, $x_2 = 3$.", 1),
            ],
            "domyslny");

        var wynik = await new MockAgentOceniajacy(Dane, new LematOpcje()).OcenAsync(wejscie, Ct);

        wynik.Kryteria.Select(k => k.Punkty).ShouldBe([1, 0]);
        wynik.Kryteria.ShouldAllBe(k => k.Prawdopodobienstwo == 0.995m);
        wynik.PewnoscOceny.ShouldBe(0.995m);
        wynik.PierwszyBlednyKrok.ShouldBe(2);
        wynik.TypBledu.ShouldBe(TypBledu.Rachunkowy);
        wynik.Wskazowka.ShouldBe("Sprawdź jeszcze raz krok 2 — policz ponownie drugi pierwiastek.");
        wynik.Uzasadnienie.ShouldBe(
            "Kryteria: K1 „Obliczenie wyróżnika trójmianu: $\\Delta = 1$.” — 1/1 pkt; K2 „Obliczenie pierwiastków równania: $x_1 = 2$, $x_2 = 3$.” — 0/1 pkt. Pierwszy błędny krok: 2 (błąd rachunkowy). Razem 1/2 pkt.");
    }

    [Fact]
    public async Task OcenaBezBleduNieMaTypuBleduAniWskazowkiAUzasadnienieNieMaZdaniaOBledzie()
    {
        var wejscie = new WejscieOceny(Zadanie(1), [new KryteriumDoOceny(1, "A", 1), new KryteriumDoOceny(2, "B", 1)], "domyslny");

        var wynik = await new MockAgentOceniajacy(Dane, new LematOpcje()).OcenAsync(wejscie, Ct);

        wynik.TypBledu.ShouldBeNull();
        wynik.Wskazowka.ShouldBeNull();
        wynik.PierwszyBlednyKrok.ShouldBeNull();
        wynik.Uzasadnienie.ShouldBe("Kryteria: K1 „A” — 1/1 pkt; K2 „B” — 1/1 pkt. Razem 2/2 pkt.");
    }

    [Fact]
    public async Task LiczbaKryteriowNiezgodnaZDanymiMockaRzucaWyjatek()
    {
        var wejscie = new WejscieOceny(Zadanie(1), [new KryteriumDoOceny(1, "A", 1)], "domyslny");

        await Should.ThrowAsync<InvalidOperationException>(() => new MockAgentOceniajacy(Dane, new LematOpcje()).OcenAsync(wejscie, Ct));
    }

    [Fact]
    public void RejestracjaAkceptujeTylkoDostawceMock()
    {
        var opcje = new LematOpcje();
        opcje.Modele.Dostawca = "openai-zgodny";

        Should.Throw<InvalidOperationException>(() => new ServiceCollection().DodajModeleMock(opcje));
        var uslugi = new ServiceCollection().DodajModeleMock(new LematOpcje());
        uslugi.ShouldContain(d => d.ServiceType == typeof(IAgentOceniajacy));
        uslugi.ShouldContain(d => d.ServiceType == typeof(IAgentRozpoznajacy));
    }
}
