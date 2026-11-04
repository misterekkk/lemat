// @Role: Testy value objectów Rozwiązanie wzorcowe i Wynik końcowy wzorcowy oraz Umiejętności/Działu (niezmienniki) | @DependsOn: Lemat.Domain.Content | @UsedBy: dotnet test | @Task: TASK-017
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Tests.Content;

public sealed class WzorceTests
{
    [Fact]
    public void RozwiazanieWzorcoweWymagaWariantow() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => new RozwiazanieWzorcowe([])).KodBledu.ShouldBe("ROZWIAZANIE_WZORCOWE_PUSTE");

    [Fact]
    public void WariantyWzorcaMajaUnikalneNumery() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => new RozwiazanieWzorcowe(
            [new WariantWzorca(1, "a", []), new WariantWzorca(1, "b", [])])).KodBledu.ShouldBe("ROZWIAZANIE_WZORCOWE_WARIANT_DUPLIKAT");

    [Fact]
    public void WynikKoncowyPozaTekstemWymagaSympy() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => new WynikKoncowyWzorcowy(TypWynikuKoncowego.Wyrazenie, "x+1", null))
            .KodBledu.ShouldBe("WYNIK_WZORCOWY_SYMPY_WYMAGANY");

    [Fact]
    public void WynikTekstowyNieWymagaSympy() =>
        new WynikKoncowyWzorcowy(TypWynikuKoncowego.Tekst, "funkcja jest malejąca", null).PostacSympy.ShouldBeNull();

    [Fact]
    public void UmiejetnoscPilnujeWagiIWymagan()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Umiejetnosc.Utworz(new UmiejetnoscId(1), new DzialId(1), "P.I.1", "x", false, 0m))
            .KodBledu.ShouldBe("WAGA_EGZAMINACYJNA_POZA_ZAKRESEM");
        Should.Throw<NiezmiennikNaruszonyException>(() => Umiejetnosc.Utworz(new UmiejetnoscId(1), new DzialId(1), "P.I.1", "x", false, 1.01m))
            .KodBledu.ShouldBe("WAGA_EGZAMINACYJNA_POZA_ZAKRESEM");
        Should.Throw<NiezmiennikNaruszonyException>(() => Umiejetnosc.Utworz(
            new UmiejetnoscId(1), new DzialId(1), "P.I.1", "x", false, 0.5m, [new UmiejetnoscId(1)]))
            .KodBledu.ShouldBe("UMIEJETNOSC_WYMAGA_SIEBIE");

        var poprawna = Umiejetnosc.Utworz(
            new UmiejetnoscId(2), new DzialId(1), "P.I.2", "y", false, 0.25m, [new UmiejetnoscId(1), new UmiejetnoscId(1)]);
        poprawna.WymaganeUmiejetnosci.ShouldBe([new UmiejetnoscId(1)]);
    }

    [Fact]
    public void KalibracjaPoczatkowaLiczyTrudnoscBZPoziomuNominalnego()
    {
        var kalibracja = KalibracjaTrudnosci.Poczatkowa(new ZadanieId(Guid.NewGuid()), new PoziomTrudnosci(5), ContentTestData.Teraz);

        kalibracja.TrudnoscB.ShouldBe(1.6m);
        kalibracja.PoziomTrudnosci.Wartosc.ShouldBe(5);
        kalibracja.LiczbaWynikowKalibracji.ShouldBe(0);
        KalibracjaTrudnosci.Poczatkowa(new ZadanieId(Guid.NewGuid()), new PoziomTrudnosci(1), ContentTestData.Teraz).TrudnoscB.ShouldBe(-1.6m);
    }
}
