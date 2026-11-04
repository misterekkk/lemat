// @Role: Testy czystego modelu Elo (10.1.3): tablica przypadków z wartościami liczbowymi, współczynnik K, szansa odgadnięcia, Umiejętność efektywna, poziom opanowania i property-test monotoniczności theta | @DependsOn: Lemat.Domain.Profile.ModelElo, Lemat.Domain.Content.FormaZamknieta | @UsedBy: dotnet test | @Task: TASK-019
using FsCheck.Xunit;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;

namespace Lemat.Domain.Tests.Profile;

public sealed class EloModelTests
{
    // ---------- tablica przypadków (obliczone ręcznie ze wzorów 10.1.3) ----------

    [Theory]
    [InlineData(0, 0, 0, 1, 1, 0, 0.400)]
    [InlineData(0, 0, 0, 0, 1, 0, -0.400)]
    [InlineData(0, 0, 0.25, 1, 1, 0, 0.300)]
    [InlineData(0, 0, 0.25, 0, 1, 0, -0.500)]
    [InlineData(0, 0, 0, 1, 0.5, 0, 0.200)]
    [InlineData(0, 0, 0, 1, 0.25, 0, 0.100)]
    [InlineData(0, -1.6, 0, 1, 1, 0, 0.134)]
    [InlineData(0, 1.6, 0, 1, 1, 0, 0.666)]
    [InlineData(0, 0, 0, 1, 1, 20, 0.200)]
    [InlineData(0, 0, 0, 0.5, 1, 0, 0.000)]
    public void PrzyrostThetaZgodnyZTablicaPrzypadkow(double theta, double b, double c, double ulamek, double waga, int n, double oczekiwany)
    {
        var p = ModelElo.PrawdopodobienstwoSukcesu((decimal)theta, (decimal)b, (decimal)c);
        var k = ModelElo.WspolczynnikK(n, zTestuDiagnostycznego: false);

        var przyrost = ModelElo.PrzyrostTheta((decimal)waga, 1m, k, (decimal)ulamek, p);

        przyrost.ShouldBe((decimal)oczekiwany);
    }

    [Theory]
    [InlineData(0, 0.800)]
    [InlineData(20, 0.400)]
    [InlineData(40, 0.267)]
    [InlineData(60, 0.200)]
    [InlineData(1000, 0.200)]
    public void WspolczynnikKMalejeDoMinimum(int n, double oczekiwany) =>
        ModelElo.Zaokraglij(ModelElo.WspolczynnikK(n, zTestuDiagnostycznego: false)).ShouldBe((decimal)oczekiwany);

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void WspolczynnikKDlaTestuDiagnostycznegoJestStaly(int n) =>
        ModelElo.WspolczynnikK(n, zTestuDiagnostycznego: true).ShouldBe(0.8m);

    [Fact]
    public void ThetaEfektywnaMieszaGlownaZeSredniaDodatkowych()
    {
        ModelElo.ThetaEfektywna(1.0m, [0m, 0.5m]).ShouldBe(0.775m);
        ModelElo.ThetaEfektywna(1.0m, []).ShouldBe(1.0m);
    }

    [Fact]
    public void SzansaOdgadnieciaZalezyOdFormieZadania()
    {
        ModelElo.SzansaOdgadniecia(null, 0).ShouldBe(0m);
        ModelElo.SzansaOdgadniecia(FormaZamknieta.WyborJednokrotny, 0).ShouldBe(0.25m);
        ModelElo.SzansaOdgadniecia(FormaZamknieta.PrawdaFalsz, 2).ShouldBe(0.25m);
        ModelElo.SzansaOdgadniecia(FormaZamknieta.PrawdaFalsz, 3).ShouldBe(0.125m);
    }

    [Theory]
    [InlineData(0, 0.310)]
    [InlineData(0.8, 0.500)]
    [InlineData(2.2, 0.802)]
    [InlineData(-3, 0.022)]
    public void PoziomOpanowaniaToSigmaThetaMinusOsiem(double theta, double oczekiwany) =>
        ModelElo.PoziomOpanowaniaZTheta((decimal)theta).ShouldBe((decimal)oczekiwany);

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 7)]
    [InlineData(3, 14)]
    [InlineData(4, 30)]
    [InlineData(5, 60)]
    [InlineData(9, 60)]
    public void OdstepyPowtorekRosnaAzDoSzesciuDziesieciu(int licznik, int dni)
    {
        var start = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

        ModelElo.NastepnaPowtorka(licznik, start).ShouldBe(start.AddDays(dni));
    }

    // ---------- property-testy ----------

    [Property]
    public bool PoprawnaOdpowiedzNigdyNieObnizaThetaABlednaNigdyNieZwiekszaJej(sbyte thetaPomnozone, sbyte bPomnozone, byte szansa, byte nWynikow)
    {
        var theta = thetaPomnozone / 20m;
        var b = bPomnozone / 40m;
        var c = Math.Min(szansa / 255m, 0.9m);
        var p = ModelElo.PrawdopodobienstwoSukcesu(theta, b, c);
        var k = ModelElo.WspolczynnikK(nWynikow, zTestuDiagnostycznego: false);

        var poPoprawnej = ModelElo.PrzyrostTheta(1m, 1m, k, 1m, p);
        var poBlednej = ModelElo.PrzyrostTheta(1m, 1m, k, 0m, p);

        return poPoprawnej >= 0m && poBlednej <= 0m && poPoprawnej >= poBlednej;
    }

    [Property]
    public bool PrawdopodobienstwoSukcesuRosnieZTheta(sbyte a, sbyte b)
    {
        var (mniejsza, wieksza) = a <= b ? (a, b) : (b, a);

        return ModelElo.PrawdopodobienstwoSukcesu(mniejsza / 20m, 0m, 0.25m) <= ModelElo.PrawdopodobienstwoSukcesu(wieksza / 20m, 0m, 0.25m);
    }
}
