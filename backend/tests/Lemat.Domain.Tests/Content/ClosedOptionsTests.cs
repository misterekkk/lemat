// @Role: Testy OpcjeZamkniete.Ocen dla obu form (WYBOR_JEDNOKROTNY, PRAWDA_FALSZ: punkt tylko gdy wszystkie stwierdzenia poprawne) i walidacji opcji | @DependsOn: Lemat.Domain.Content.OpcjeZamkniete | @UsedBy: dotnet test | @Task: TASK-017
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Tests.Content;

public sealed class OpcjeZamknieteTests
{
    [Theory]
    [InlineData("B", 1, true)]
    [InlineData("A", 0, false)]
    [InlineData("Z", 0, false)]
    [InlineData(null, 0, false)]
    public void WyborJednokrotnyDajeJedenPunktTylkoZaPoprawnaOpcje(string? wybrana, int punkty, bool poprawna)
    {
        var rezultat = ContentTestData.WyborA().Ocen(new WyborZamkniety(Opcja: wybrana));

        rezultat.Punkty.ShouldBe(punkty);
        rezultat.Poprawna.ShouldBe(poprawna);
    }

    [Fact]
    public void PrawdaFalszDajePunktTylkoGdyWszystkieStwierdzeniaOcenionePoprawnie()
    {
        var opcje = ContentTestData.PrawdaFalszDwa();

        opcje.Ocen(new WyborZamkniety(Stwierdzenia: new Dictionary<string, bool> { ["1"] = true, ["2"] = false })).Punkty.ShouldBe(1);
        opcje.Ocen(new WyborZamkniety(Stwierdzenia: new Dictionary<string, bool> { ["1"] = true, ["2"] = true })).Punkty.ShouldBe(0);
        opcje.Ocen(new WyborZamkniety(Stwierdzenia: new Dictionary<string, bool> { ["1"] = false, ["2"] = false })).Punkty.ShouldBe(0);
        opcje.Ocen(new WyborZamkniety(Stwierdzenia: new Dictionary<string, bool> { ["1"] = true })).Punkty.ShouldBe(0);
        opcje.Ocen(new WyborZamkniety(Opcja: "1")).Punkty.ShouldBe(0);
    }

    [Fact]
    public void FormaZostajeZgodnaZFabryka()
    {
        ContentTestData.WyborA().Forma.ShouldBe(FormaZamknieta.WyborJednokrotny);
        ContentTestData.PrawdaFalszDwa().Forma.ShouldBe(FormaZamknieta.PrawdaFalsz);
        ContentTestData.PrawdaFalszDwa().Opcje.ShouldBeEmpty();
        ContentTestData.WyborA().Stwierdzenia.ShouldBeEmpty();
    }

    [Fact]
    public void PoprawnaOpcjaMusiNalezecDoListy() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => OpcjeZamkniete.WyborJednokrotny([new OpcjaWyboru("A", "a"), new OpcjaWyboru("B", "b")], "C"))
            .KodBledu.ShouldBe("OPCJE_ZAMKNIETE_POPRAWNA_NIEZNANA");

    [Fact]
    public void IdentyfikatoryMusząBycUnikalne() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => OpcjeZamkniete.PrawdaFalsz(
            [new StwierdzeniePrawdaFalsz("1", "a", true), new StwierdzeniePrawdaFalsz("1", "b", false)]))
            .KodBledu.ShouldBe("OPCJE_ZAMKNIETE_IDENTYFIKATORY");

    [Fact]
    public void WymagaCoNajmniejDwochOpcji() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => OpcjeZamkniete.WyborJednokrotny([new OpcjaWyboru("A", "a")], "A"))
            .KodBledu.ShouldBe("OPCJE_ZAMKNIETE_ZA_MALO_OPCJI");
}
