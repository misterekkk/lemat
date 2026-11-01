// @Role: Testy walidacji Planu pytań (ADR-026): unikalność kodów, zależności tylko od pytań bezwarunkowych (głębokość 1, brak cykli), zgodność warunku z opcjami pytania nadrzędnego, plan zbudowany jak w 10.4.4; testy Pytań | @DependsOn: Lemat.Domain.Models.PlanPytan, Pytanie, PytanieNoul, PytanieScore, PytanieChoice | @UsedBy: dotnet test | @Task: TASK-018
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Domain.Tests.Models;

public sealed class QuestionPlanTests
{
    private static PytanieChoice Wariant() => new("Q-OC-00", "Którym wariantem?", ["W1", "W2", "ZADEN"]);

    private static PytanieChoice PierwszyBlad(int kroki) =>
        new("Q-OC-03", "Pierwszy błąd?", ["BRAK_BLEDU", .. Enumerable.Range(1, kroki).Select(i => $"KROK_{i}"), "BRAK_WYMAGANEGO_KROKU"]);

    private static NiezmiennikNaruszonyException Odrzucony(IReadOnlyList<PytanieWPlanie> pytania) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => PlanPytan.Utworz(pytania));

    [Fact]
    public void PlanZPytaniemBezwarunkowymJestPoprawny()
    {
        var plan = PlanPytan.Jedno(new PytanieNoul("Q-OC-01", "Czy wynik równoważny?"));

        plan.Pytania.Count.ShouldBe(1);
        plan.Bezwarunkowe.Count().ShouldBe(1);
        plan.Warunkowe.ShouldBeEmpty();
    }

    [Fact]
    public void PlanPustyJestOdrzucany() =>
        Odrzucony([]).KodBledu.ShouldBe("PLAN_PYTAN_NIEPRAWIDLOWY");

    [Fact]
    public void PowtorzonyKodPytaniaJestOdrzucany() =>
        Odrzucony([new(new PytanieNoul("Q-OC-01", "a"), null), new(new PytanieNoul("Q-OC-01", "b"), null)])
            .KodBledu.ShouldBe("PLAN_PYTAN_NIEPRAWIDLOWY");

    [Fact]
    public void ZaleznoscOdNieistniejacegoPytaniaJestOdrzucana() =>
        Odrzucony([new(new PytanieNoul("Q-OC-04.1", "a"), new WarunekPytania("Q-OC-03", ["KROK_1"], []))])
            .KodBledu.ShouldBe("PLAN_PYTAN_NIEPRAWIDLOWY");

    [Fact]
    public void ZaleznoscOdPytaniaWarunkowegoJestOdrzucanaWiecGlebokoscWynosiJeden()
    {
        var pierwszy = new PytanieWPlanie(PierwszyBlad(2), null);
        var drugi = new PytanieWPlanie(new PytanieChoice("Q-OC-04.1", "Typ?", ["RACHUNKOWY", "MERYTORYCZNY"]), new WarunekPytania("Q-OC-03", ["KROK_1"], []));
        var trzeci = new PytanieWPlanie(new PytanieNoul("Q-X", "?"), new WarunekPytania("Q-OC-04.1", ["RACHUNKOWY"], []));

        Odrzucony([pierwszy, drugi, trzeci]).Message.ShouldContain("głębokość");
    }

    [Fact]
    public void CyklMiedzyPytaniamiWarunkowymiJestOdrzucany()
    {
        var a = new PytanieWPlanie(new PytanieNoul("A", "?"), new WarunekPytania("B", ["tak"], []));
        var b = new PytanieWPlanie(new PytanieNoul("B", "?"), new WarunekPytania("A", ["tak"], []));

        Odrzucony([a, b]).KodBledu.ShouldBe("PLAN_PYTAN_NIEPRAWIDLOWY");
    }

    [Fact]
    public void PytanieZaleznePoSamymSobieJestOdrzucane()
    {
        var a = new PytanieWPlanie(new PytanieNoul("A", "?"), new WarunekPytania("A", ["tak"], []));

        Odrzucony([a]).KodBledu.ShouldBe("PLAN_PYTAN_NIEPRAWIDLOWY");
    }

    [Fact]
    public void WarunekBezWartosciAlboZObcaWartosciaJestOdrzucany()
    {
        var nadrzedne = new PytanieWPlanie(PierwszyBlad(1), null);

        Odrzucony([nadrzedne, new(new PytanieNoul("Q-1", "?"), new WarunekPytania("Q-OC-03", [], []))]).KodBledu.ShouldBe("PLAN_PYTAN_NIEPRAWIDLOWY");
        Odrzucony([nadrzedne, new(new PytanieNoul("Q-2", "?"), new WarunekPytania("Q-OC-03", ["KROK_9"], []))]).KodBledu.ShouldBe("PLAN_PYTAN_NIEPRAWIDLOWY");
        Odrzucony([nadrzedne, new(new PytanieNoul("Q-3", "?"), new WarunekPytania("Q-OC-03", ["KROK_1"], ["NIE_MA"]))]).KodBledu.ShouldBe("PLAN_PYTAN_NIEPRAWIDLOWY");
    }

    [Fact]
    public void PlanOcenyZWieluWariantamiIKrokamiJestPoprawny()
    {
        var pytania = new List<PytanieWPlanie>
        {
            new(Wariant(), null),
            new(new PytanieNoul("Q-OC-01", "Czy wynik równoważny?"), null),
            new(new PytanieScore("Q-OC-02.1.K1", "Ile punktów za K1?", 0, 1), new WarunekPytania("Q-OC-00", ["W1"], ["ZADEN"])),
            new(new PytanieScore("Q-OC-02.2.K1", "Ile punktów za K1?", 0, 2), new WarunekPytania("Q-OC-00", ["W2"], ["ZADEN"])),
            new(PierwszyBlad(3), null),
            new(new PytanieChoice("Q-OC-04.1", "Typ błędu w kroku 1?", ["RACHUNKOWY", "MERYTORYCZNY", "NIEPELNE_ROZWIAZANIE", "BLAD_ODCZYTU"]), new WarunekPytania("Q-OC-03", ["KROK_1"], [])),
            new(new PytanieNoul("Q-OC-06", "Błąd odczytu?"), null),
        };

        var plan = PlanPytan.Utworz(pytania);

        plan.Pytania.Count.ShouldBe(7);
        plan.Bezwarunkowe.Count().ShouldBe(4);
        plan.Warunkowe.Count().ShouldBe(3);
    }

    // ---------- Pytania ----------

    [Fact]
    public void PytanieNoulDopuszczaTakINie()
    {
        var pytanie = new PytanieNoul("Q", "?");

        pytanie.Typ.ShouldBe(TypPytania.Noul);
        pytanie.DozwoloneOdpowiedzi.ShouldBe(["tak", "nie"]);
    }

    [Fact]
    public void PytanieSkaliWymieniaLiczbyZPrzedzialuIOdrzucaZaDuzaRozpietosc()
    {
        var pytanie = new PytanieScore("Q", "?", 0, 3);

        pytanie.Typ.ShouldBe(TypPytania.Score);
        pytanie.DozwoloneOdpowiedzi.ShouldBe(["0", "1", "2", "3"]);
        new PytanieScore("Q", "?", 0, 10).DozwoloneOdpowiedzi.Count.ShouldBe(11);
        Should.Throw<NiezmiennikNaruszonyException>(() => new PytanieScore("Q", "?", 0, 11)).KodBledu.ShouldBe("PYTANIE_NIEPRAWIDLOWE");
        Should.Throw<NiezmiennikNaruszonyException>(() => new PytanieScore("Q", "?", 2, 1)).KodBledu.ShouldBe("PYTANIE_NIEPRAWIDLOWE");
    }

    [Fact]
    public void PytanieChoiceWymagaDwochDoCzterdziestuDwochUnikalnychOpcji()
    {
        new PytanieChoice("Q", "?", ["a", "b"]).Typ.ShouldBe(TypPytania.Choice);
        new PytanieChoice("Q", "?", [.. Enumerable.Range(1, 42).Select(i => $"o{i}")]).Opcje.Count.ShouldBe(42);
        Should.Throw<NiezmiennikNaruszonyException>(() => new PytanieChoice("Q", "?", ["a"]));
        Should.Throw<NiezmiennikNaruszonyException>(() => new PytanieChoice("Q", "?", [.. Enumerable.Range(1, 43).Select(i => $"o{i}")]));
        Should.Throw<NiezmiennikNaruszonyException>(() => new PytanieChoice("Q", "?", ["a", "a"]));
        Should.Throw<NiezmiennikNaruszonyException>(() => new PytanieChoice("Q", "?", ["a", " "]));
    }
}
