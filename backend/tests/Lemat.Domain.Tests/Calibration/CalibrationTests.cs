// @Role: Testy modułu Kalibracja: Punkty kalibratora (interpolacja, monotoniczność), KalibratorPewnosci.Tozsamosciowy zwraca wejście, ProgPewnosci (zakres 0,800–0,990, tryb pełnej weryfikacji 1,000, dowód wymusza 1,000), migawki | @DependsOn: Lemat.Domain.Calibration | @UsedBy: dotnet test | @Task: TASK-018
using FsCheck.Xunit;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Domain.Tests.Calibration;

public sealed class CalibrationTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly KonfiguracjaModeluId Konfiguracja = new(Guid.NewGuid());

    private static KalibratorPewnosci Tozsamosciowy(RodzajZadania rodzaj = RodzajZadania.OtwarteObliczeniowe) =>
        KalibratorPewnosci.Tozsamosciowy(new KalibratorPewnosciId(Guid.NewGuid()), Konfiguracja, rodzaj, Teraz);

    private static ProgPewnosci Prog(decimal wartosc, RodzajZadania rodzaj = RodzajZadania.OtwarteObliczeniowe, string uzasadnienie = "seed") =>
        ProgPewnosci.Utworz(
            new ProgPewnosciId(Guid.NewGuid()), Konfiguracja, rodzaj, new KalibratorPewnosciId(Guid.NewGuid()), wartosc, ZrodloProgu.Administrator, uzasadnienie, null, null, null, Teraz);

    private static PunktyKalibratora Izotoniczne() => PunktyKalibratora.Utworz(
        PunktyKalibratora.TypIzotoniczny,
        [new(0m, 0.12m), new(0.5m, 0.41m), new(0.8m, 0.86m), new(0.95m, 0.985m), new(1m, 0.995m)]);

    // ---------- Kalibrator tożsamościowy ----------

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.2)]
    [InlineData(0.5)]
    [InlineData(0.995)]
    [InlineData(1.0)]
    public void KalibratorTozsamosciowyZwracaWejscie(double wejscie) =>
        Tozsamosciowy().Kalibruj((decimal)wejscie).ShouldBe((decimal)wejscie);

    [Property]
    public bool KalibratorTozsamosciowyZwracaWejscieDlaKazdejPewnosci(ushort licznik)
    {
        var wejscie = (licznik % 10001) / 10000m;
        return Tozsamosciowy().Kalibruj(wejscie) == wejscie;
    }

    [Fact]
    public void KalibratorTozsamosciowyPrzycinaWejscieSpozaZakresu()
    {
        Tozsamosciowy().Kalibruj(-0.3m).ShouldBe(0m);
        Tozsamosciowy().Kalibruj(1.7m).ShouldBe(1m);
    }

    [Fact]
    public void KalibratorTozsamosciowyMaMetodeZrodloIBrakProbek()
    {
        var kalibrator = Tozsamosciowy();

        kalibrator.Metoda.ShouldBe(MetodaKalibratora.Tozsamosciowy);
        kalibrator.Zrodlo.ShouldBe(ZrodloKalibratora.KalibracjaAutomatyczna);
        kalibrator.LiczbaProbek.ShouldBe(0);
        kalibrator.Punkty.Typ.ShouldBe(PunktyKalibratora.TypTozsamosciowy);
        kalibrator.Punkty.Punkty.Count.ShouldBe(2);
    }

    [Fact]
    public void ZadaniaZamknieteNieMajaKalibratora() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Tozsamosciowy(RodzajZadania.Zamkniete)).KodBledu.ShouldBe("KALIBRATOR_DLA_ZAMKNIETYCH");

    // ---------- Punkty kalibratora ----------

    [Fact]
    public void InterpolacjaLiniowaMiedzyPunktamiIPrzycieciePozaZakresem()
    {
        var punkty = Izotoniczne();

        punkty.Kalibruj(0.5m).ShouldBe(0.41m);
        punkty.Kalibruj(0.65m).ShouldBe(0.635m);
        punkty.Kalibruj(0.875m).ShouldBe(0.9225m);
        punkty.Kalibruj(1m).ShouldBe(0.995m);
        PunktyKalibratora.Utworz(PunktyKalibratora.TypIzotoniczny, [new(0.2m, 0.3m), new(0.8m, 0.9m)]).Kalibruj(0.1m).ShouldBe(0.3m);
        PunktyKalibratora.Utworz(PunktyKalibratora.TypIzotoniczny, [new(0.2m, 0.3m), new(0.8m, 0.9m)]).Kalibruj(0.95m).ShouldBe(0.9m);
    }

    [Property]
    public bool FunkcjaIzotonicznaJestMonotoniczna(ushort a, ushort b)
    {
        var punkty = Izotoniczne();
        var x = (a % 1001) / 1000m;
        var y = (b % 1001) / 1000m;
        var (mniejsza, wieksza) = x <= y ? (x, y) : (y, x);
        return punkty.Kalibruj(mniejsza) <= punkty.Kalibruj(wieksza);
    }

    [Fact]
    public void NieprawidloweFunkcjeSaOdrzucane()
    {
        static void Odrzuc(string typ, params (decimal Surowa, decimal Skalibrowana)[] punkty) =>
            Should.Throw<NiezmiennikNaruszonyException>(() => PunktyKalibratora.Utworz(typ, [.. punkty.Select(p => new PunktKalibracji(p.Surowa, p.Skalibrowana))]))
                .KodBledu.ShouldBe("PUNKTY_KALIBRATORA_NIEPRAWIDLOWE");

        Odrzuc(PunktyKalibratora.TypIzotoniczny, (0m, 0m));
        Odrzuc(PunktyKalibratora.TypIzotoniczny, (0m, 0.5m), (1m, 0.4m));
        Odrzuc(PunktyKalibratora.TypIzotoniczny, (0.5m, 0.1m), (0.5m, 0.2m));
        Odrzuc(PunktyKalibratora.TypIzotoniczny, (0m, 0m), (1.1m, 1m));
        Odrzuc("NIEZNANY", (0m, 0m), (1m, 1m));
        Odrzuc(PunktyKalibratora.TypTozsamosciowy, (0m, 0m), (1m, 0.9m));
    }

    [Fact]
    public void KalibratorIzotonicznyWymagaFunkcjiIzotonicznej()
    {
        var kalibrator = KalibratorPewnosci.Izotoniczny(
            new KalibratorPewnosciId(Guid.NewGuid()), Konfiguracja, RodzajZadania.OtwarteObliczeniowe, Izotoniczne(), 240, 0.0123m, Teraz.AddDays(-30), Teraz, ZrodloKalibratora.EwaluacjaZt1, Teraz);

        kalibrator.Metoda.ShouldBe(MetodaKalibratora.IzotonicznyCv5);
        kalibrator.Kalibruj(0.8m).ShouldBe(0.86m);
        Should.Throw<NiezmiennikNaruszonyException>(() => KalibratorPewnosci.Izotoniczny(
            new KalibratorPewnosciId(Guid.NewGuid()), Konfiguracja, RodzajZadania.OtwarteObliczeniowe, PunktyKalibratora.Tozsamosciowe, 0, null, null, null, ZrodloKalibratora.EwaluacjaZt1, Teraz));
    }

    // ---------- Próg pewności ----------

    [Theory]
    [InlineData(0.800)]
    [InlineData(0.950)]
    [InlineData(0.990)]
    public void ProgZZakresuJestPrzyjmowany(double wartosc) =>
        Prog((decimal)wartosc).Wartosc.ShouldBe((decimal)wartosc);

    [Theory]
    [InlineData(0.799)]
    [InlineData(0.0)]
    [InlineData(0.9901)]
    [InlineData(0.995)]
    [InlineData(1.001)]
    [InlineData(-1.0)]
    public void ProgPozaZakresemJestOdrzucany(double wartosc) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Prog((decimal)wartosc)).KodBledu.ShouldBe("PROG_POZA_ZAKRESEM");

    [Fact]
    public void ProgTysiacOznaczaTrybPelnejWeryfikacji()
    {
        Prog(1.000m).TrybPelnejWeryfikacji.ShouldBeTrue();
        Prog(0.950m).TrybPelnejWeryfikacji.ShouldBeFalse();
        MigawkaProgu.Z(Prog(1.000m)).TrybPelnejWeryfikacji.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0.95)]
    [InlineData(0.99)]
    public void DlaDowoduProgWynosiZawszeTysiac(double wartosc)
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Prog((decimal)wartosc, RodzajZadania.OtwarteZDowodem)).KodBledu.ShouldBe("PROG_DOWODU");
        Prog(1.000m, RodzajZadania.OtwarteZDowodem).TrybPelnejWeryfikacji.ShouldBeTrue();
    }

    [Fact]
    public void ZadaniaZamknieteNieMajaProgu() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Prog(0.95m, RodzajZadania.Zamkniete)).KodBledu.ShouldBe("PROG_DLA_ZAMKNIETYCH");

    [Fact]
    public void ProgWymagaUzasadnieniaIMaCyklAktywnosci()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Prog(0.95m, uzasadnienie: "  ")).KodBledu.ShouldBe("UZASADNIENIE_WYMAGANE");
        var prog = Prog(0.95m);

        prog.Aktywny.ShouldBeFalse();
        prog.Aktywuj();
        prog.Aktywny.ShouldBeTrue();
        prog.Dezaktywuj();
        prog.Aktywny.ShouldBeFalse();
    }

    // ---------- Migawki ----------

    [Fact]
    public void MigawkaKalibratoraKalibrujePrzezPunktyIPrzycinaWejscie()
    {
        var migawka = MigawkaKalibratora.Z(Tozsamosciowy());

        migawka.Kalibruj(0.995m).ShouldBe(0.995m);
        migawka.Kalibruj(2m).ShouldBe(1m);
        migawka.Metoda.ShouldBe(MetodaKalibratora.Tozsamosciowy);
    }
}
