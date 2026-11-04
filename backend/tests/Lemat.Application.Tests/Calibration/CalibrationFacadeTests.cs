// @Role: Testy jednostkowe KalibracjaFasada (atrapy repozytoriów): odczyt aktywnej kalibracji (próg + kalibrator), brak progu, zapis kalibratora tożsamościowego, ustawienie progu z dezaktywacją poprzedniego, reguły progu dowodu | @DependsOn: KalibracjaFasada, NSubstitute, Lemat.Domain.Calibration | @UsedBy: dotnet test | @Task: TASK-018
using Lemat.Application.Calibration;
using Lemat.Application.Tests.Support;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Models;
using NSubstitute;

namespace Lemat.Application.Tests.Calibration;

public sealed class CalibrationFacadeTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly KonfiguracjaModeluId Konfiguracja = new(Guid.NewGuid());

    private readonly IKalibratorPewnosciRepozytorium _kalibratory = Substitute.For<IKalibratorPewnosciRepozytorium>();
    private readonly IProgPewnosciRepozytorium _progi = Substitute.For<IProgPewnosciRepozytorium>();
    private readonly PrzezroczystaJednostkaPracy _jednostkaPracy = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private KalibracjaFasada Fasada() => new(_kalibratory, _progi, _jednostkaPracy, new Models.FakeTimeProvider(Teraz));

    [Fact]
    public async Task ZapisKalibratoraTozsamosciowegoDodajeKalibratorIZwracaMigawke()
    {
        var migawka = await Fasada().ZapiszKalibratorTozsamosciowyAsync(Konfiguracja, RodzajZadania.OtwarteObliczeniowe, Ct);

        migawka.Metoda.ShouldBe(MetodaKalibratora.Tozsamosciowy);
        migawka.Kalibruj(0.7m).ShouldBe(0.7m);
        _kalibratory.Received(1).Dodaj(Arg.Is<KalibratorPewnosci>(k => k.Id == migawka.KalibratorPewnosciId && k.KonfiguracjaModeluId == Konfiguracja));
    }

    [Fact]
    public async Task UstawienieProguDezaktywujePoprzedniIDodajeAktywny()
    {
        var kalibrator = new KalibratorPewnosciId(Guid.NewGuid());

        var migawka = await Fasada().UstawProgAsync(Konfiguracja, RodzajZadania.OtwarteObliczeniowe, kalibrator, 0.95m, ZrodloProgu.Administrator, "seed-dev", null, Ct);

        migawka.Wartosc.ShouldBe(0.95m);
        migawka.TrybPelnejWeryfikacji.ShouldBeFalse();
        await _progi.Received(1).DezaktywujAktywnyAsync(Konfiguracja, RodzajZadania.OtwarteObliczeniowe, Arg.Any<CancellationToken>());
        _progi.Received(1).Dodaj(Arg.Is<ProgPewnosci>(p => p.Aktywny && p.KalibratorPewnosciId == kalibrator));
    }

    [Fact]
    public async Task ProgPozaZakresemNieDezaktywujePoprzedniego()
    {
        var wyjatek = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().UstawProgAsync(
            Konfiguracja, RodzajZadania.OtwarteObliczeniowe, new KalibratorPewnosciId(Guid.NewGuid()), 0.5m, ZrodloProgu.Administrator, "x", null, Ct));

        wyjatek.KodBledu.ShouldBe("PROG_POZA_ZAKRESEM");
        await _progi.DidNotReceive().DezaktywujAktywnyAsync(Arg.Any<KonfiguracjaModeluId>(), Arg.Any<RodzajZadania>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProgDowoduWymuszaTysiac()
    {
        var wyjatek = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().UstawProgAsync(
            Konfiguracja, RodzajZadania.OtwarteZDowodem, new KalibratorPewnosciId(Guid.NewGuid()), 0.95m, ZrodloProgu.Administrator, "x", null, Ct));

        wyjatek.KodBledu.ShouldBe("PROG_DOWODU");
    }

    [Fact]
    public async Task AktywnaKalibracjaLaczyProgZKalibratoremAGdyBrakProguZwracaNull()
    {
        var kalibrator = KalibratorPewnosci.Tozsamosciowy(new KalibratorPewnosciId(Guid.NewGuid()), Konfiguracja, RodzajZadania.OtwarteObliczeniowe, Teraz);
        var prog = ProgPewnosci.Utworz(
            new ProgPewnosciId(Guid.NewGuid()), Konfiguracja, RodzajZadania.OtwarteObliczeniowe, kalibrator.Id, 0.95m, ZrodloProgu.Administrator, "seed", null, null, null, Teraz);
        _progi.PobierzAktywnyAsync(Konfiguracja, RodzajZadania.OtwarteObliczeniowe, Arg.Any<CancellationToken>()).Returns(prog);
        _kalibratory.PobierzDoOdczytuAsync(kalibrator.Id, Arg.Any<CancellationToken>()).Returns(kalibrator);

        var kalibracja = await Fasada().PobierzAktywnaKalibracjeAsync(Konfiguracja, RodzajZadania.OtwarteObliczeniowe, Ct);

        kalibracja.ShouldNotBeNull().Prog.Wartosc.ShouldBe(0.95m);
        kalibracja.Kalibrator.KalibratorPewnosciId.ShouldBe(kalibrator.Id);
        (await Fasada().PobierzAktywnaKalibracjeAsync(Konfiguracja, RodzajZadania.OtwarteZDowodem, Ct)).ShouldBeNull();
    }
}
