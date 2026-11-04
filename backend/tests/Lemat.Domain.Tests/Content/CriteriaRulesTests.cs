// @Role: Testy ZasadyKryteriow (lustro trg_suma_kryteriow): suma punktów wariantu, wynik końcowy, limit 24, brak Kryteriów dla Zadań zamkniętych, walidacja KryteriumOceniania i WariantRozwiazania | @DependsOn: Lemat.Domain.Content.ZasadyKryteriow | @UsedBy: dotnet test | @Task: TASK-017
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Tests.Content;

public sealed class ZasadyKryteriowTests
{
    private static KryteriumOceniania K(int wariant, int nr, int punkty, bool wynik = false, int? wymaga = null) =>
        ContentTestData.Kryterium(wariant, nr, punkty, wynik, wymaga);

    [Fact]
    public void PoprawneKryteriaDwochWariantowPrzechodza() =>
        Should.NotThrow(() => ZasadyKryteriow.Sprawdz(
            RodzajZadania.OtwarteObliczeniowe, 3, [K(1, 1, 2), K(1, 2, 1, true), K(2, 1, 3, true)]));

    [Fact]
    public void SumaPunktowWariantuMusiRownacSiePunktomZadania() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => ZasadyKryteriow.Sprawdz(RodzajZadania.OtwarteObliczeniowe, 3, [K(1, 1, 2)]))
            .KodBledu.ShouldBe("KRYTERIA_NIESPOJNE");

    [Fact]
    public void DrugiWariantTezMusiMiecPoprawnaSume() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => ZasadyKryteriow.Sprawdz(
            RodzajZadania.OtwarteObliczeniowe, 2, [K(1, 1, 2), K(2, 1, 1)]))
            .KodBledu.ShouldBe("KRYTERIA_NIESPOJNE");

    [Fact]
    public void NajwyzejJednoKryteriumWynikuKoncowegoNaWariant() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => ZasadyKryteriow.Sprawdz(
            RodzajZadania.OtwarteObliczeniowe, 2, [K(1, 1, 1, true), K(1, 2, 1, true)]))
            .KodBledu.ShouldBe("KRYTERIA_NIESPOJNE");

    [Fact]
    public void LaczniePowyzej24KryteriowToBlad()
    {
        var kryteria = Enumerable.Range(1, 4).SelectMany(w => Enumerable.Range(1, 7).Select(n => K(w, n, 1))).ToList();

        Should.Throw<NiezmiennikNaruszonyException>(() => ZasadyKryteriow.Sprawdz(RodzajZadania.OtwarteObliczeniowe, 7, kryteria))
            .KodBledu.ShouldBe("ZA_DUZO_KRYTERIOW");
    }

    [Fact]
    public void ZadanieZamknieteNieMaKryteriow() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => ZasadyKryteriow.Sprawdz(RodzajZadania.Zamkniete, 1, [K(1, 1, 1)]))
            .KodBledu.ShouldBe("KRYTERIA_NIESPOJNE");

    [Fact]
    public void ZadanieOtwarteWymagaKryteriow() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => ZasadyKryteriow.Sprawdz(RodzajZadania.OtwarteZDowodem, 2, []))
            .KodBledu.ShouldBe("KRYTERIA_NIESPOJNE");

    [Fact]
    public void NrKryteriumMusiBycUnikalnyWWariancie() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => ZasadyKryteriow.Sprawdz(
            RodzajZadania.OtwarteObliczeniowe, 2, [K(1, 1, 1), K(1, 1, 1)]))
            .KodBledu.ShouldBe("KRYTERIA_NIESPOJNE");

    [Theory]
    [InlineData(0, 1, "KRYTERIUM_NR_POZA_ZAKRESEM")]
    [InlineData(13, 1, "KRYTERIUM_NR_POZA_ZAKRESEM")]
    [InlineData(1, 0, "KRYTERIUM_PUNKTY_POZA_ZAKRESEM")]
    [InlineData(1, 4, "KRYTERIUM_PUNKTY_POZA_ZAKRESEM")]
    public void KryteriumPilnujeZakresow(int nr, int punkty, string kod) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => K(1, nr, punkty)).KodBledu.ShouldBe(kod);

    [Fact]
    public void KryteriumMozeWymagacTylkoNizszegoNumeru() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => K(1, 2, 1, wymaga: 2)).KodBledu.ShouldBe("KRYTERIUM_WARUNEK_NIEPRAWIDLOWY");

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void WariantPozaZakresemJestOdrzucony(int wariant) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => new WariantRozwiazania(wariant)).KodBledu.ShouldBe("WARIANT_POZA_ZAKRESEM");
}
