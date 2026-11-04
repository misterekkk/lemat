// @Role: Testy agregatu WkladkaTeoretyczna: limity treści i czasu, cykl SZKIC -> DO_PRZEGLADU -> OPUBLIKOWANA/WYCOFANA, edycja tylko przed publikacją | @DependsOn: Lemat.Domain.Content.WkladkaTeoretyczna | @UsedBy: dotnet test | @Task: TASK-017
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Tests.Content;

public sealed class WkladkaTeoretycznaTests
{
    private static WkladkaTeoretyczna Szkic(string tresc = "Treść $x$", int czas = 5, bool wygenerowana = true, bool automatycznie = false) =>
        WkladkaTeoretyczna.UtworzSzkic(
            new WkladkaTeoretycznaId(Guid.CreateVersion7(ContentTestData.Teraz)), new UmiejetnoscId(501), "Tytuł", tresc, czas, wygenerowana, automatycznie, 0.12m, ContentTestData.Teraz);

    [Fact]
    public void SzkicStartujeJakoSzkicBezZatwierdzajacego()
    {
        var wkladka = Szkic();

        wkladka.Status.ShouldBe(StatusWkladki.Szkic);
        wkladka.ZatwierdzonaPrzez.ShouldBeNull();
        wkladka.Version.ShouldBe(1);
        wkladka.KosztGenerowaniaPln.ShouldBe(0.12m);
    }

    [Theory]
    [InlineData("", 5, "WKLADKA_TRESC_NIEPRAWIDLOWA")]
    [InlineData("a", 0, "WKLADKA_CZAS_POZA_ZAKRESEM")]
    [InlineData("a", 16, "WKLADKA_CZAS_POZA_ZAKRESEM")]
    public void WalidujeTrescICzas(string tresc, int czas, string kod) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Szkic(tresc, czas)).KodBledu.ShouldBe(kod);

    [Fact]
    public void TrescDo12000ZnakowJestDozwolona()
    {
        Should.NotThrow(() => Szkic(new string('x', 12000)));
        Should.Throw<NiezmiennikNaruszonyException>(() => Szkic(new string('x', 12001))).KodBledu.ShouldBe("WKLADKA_TRESC_NIEPRAWIDLOWA");
    }

    [Fact]
    public void AutomatycznaMusiBycWygenerowana() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Szkic(wygenerowana: false, automatycznie: true))
            .KodBledu.ShouldBe("WKLADKA_AUTOMATYCZNA_BEZ_GENEROWANIA");

    [Fact]
    public void CyklZyciaZPublikacja()
    {
        var wkladka = Szkic();
        var recenzent = ContentTestData.Autor();

        wkladka.PrzekazDoPrzegladu();
        wkladka.Status.ShouldBe(StatusWkladki.DoPrzegladu);
        wkladka.Edytuj("Nowy tytuł", "Nowa treść", 7);
        wkladka.Tytul.ShouldBe("Nowy tytuł");
        wkladka.Opublikuj(recenzent);
        wkladka.Status.ShouldBe(StatusWkladki.Opublikowana);
        wkladka.ZatwierdzonaPrzez.ShouldBe(recenzent);
        Should.Throw<NiezmiennikNaruszonyException>(() => wkladka.Edytuj("x", "y", 3)).KodBledu.ShouldBe("WKLADKA_NIEPRAWIDLOWY_STAN");
        Should.Throw<NiezmiennikNaruszonyException>(wkladka.PrzekazDoPrzegladu).KodBledu.ShouldBe("WKLADKA_NIEPRAWIDLOWY_STAN");
        wkladka.Wycofaj();
        wkladka.Status.ShouldBe(StatusWkladki.Wycofana);
        Should.Throw<NiezmiennikNaruszonyException>(() => wkladka.Opublikuj(recenzent)).KodBledu.ShouldBe("WKLADKA_NIEPRAWIDLOWY_STAN");
        Should.Throw<NiezmiennikNaruszonyException>(wkladka.Wycofaj).KodBledu.ShouldBe("WKLADKA_NIEPRAWIDLOWY_STAN");
    }
}
