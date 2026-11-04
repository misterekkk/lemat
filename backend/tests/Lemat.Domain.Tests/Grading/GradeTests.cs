// @Role: Testy agregatu Ocena i wartości domenowych (Z-5): fabryka Oceny automatycznej z niezmiennikami CHECK, jednokrotna Wskazówka, zastąpienie, widoczność, zaokrąglenia pewności | @DependsOn: Lemat.Domain.Grading.Ocena, DaneOcenyAutomatycznej, Wskazowka, PewnoscOceny | @UsedBy: dotnet test | @Task: TASK-126
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Grading;
using Lemat.Domain.Models;
using Lemat.Domain.Solutions;
using static Lemat.Domain.Tests.Solutions.SolutionsTestData;

namespace Lemat.Domain.Tests.Grading;

public sealed class OcenaTests
{
    private static readonly Guid Kryterium1 = Guid.NewGuid();

    private static readonly Guid Kryterium2 = Guid.NewGuid();

    private static DaneOcenyAutomatycznej Dane(
        int punkty = 1,
        int punktyMax = 2,
        int? pierwszyBlednyKrok = 2,
        TypBledu? typBledu = TypBledu.Rachunkowy,
        bool mozliwyBladOdczytu = false,
        int? krokPodejrzany = null,
        Wskazowka? wskazowka = null,
        StatusOceny status = StatusOceny.Ostateczna,
        string uzasadnienie = "Razem 1/2 pkt.",
        IReadOnlyList<OcenaKryterium>? kryteria = null) =>
        new(
            new RozwiazanieId(Guid.NewGuid()),
            1,
            new TranskrypcjaId(Guid.NewGuid()),
            status,
            new WariantRozwiazania(1),
            punkty,
            punkty,
            punktyMax,
            pierwszyBlednyKrok,
            false,
            typBledu,
            mozliwyBladOdczytu,
            krokPodejrzany,
            new PewnoscOceny(0.995m),
            null,
            new ProgPewnosciId(Guid.NewGuid()),
            new KalibratorPewnosciId(Guid.NewGuid()),
            new KonfiguracjaModeluId(Guid.NewGuid()),
            uzasadnienie,
            wskazowka,
            kryteria ?? [new OcenaKryterium(Kryterium1, 1, 0.995m), new OcenaKryterium(Kryterium2, punkty - 1, 0.995m)]);

    private static Ocena Nowa(DaneOcenyAutomatycznej? dane = null) =>
        Ocena.Automatyczna(new OcenaId(Guid.NewGuid()), dane ?? Dane(), Teraz);

    private static Ocena BezBledu() =>
        Nowa(Dane(punkty: 2, punktyMax: 2, pierwszyBlednyKrok: null, typBledu: null));

    [Fact]
    public void OcenaAutomatycznaMaPolaZDanychIPewnoscZaokraglona()
    {
        var ocena = Nowa();

        ocena.Zrodlo.ShouldBe(ZrodloOceny.Automatyczna);
        ocena.Status.ShouldBe(StatusOceny.Ostateczna);
        ocena.Wersja.ShouldBe(1);
        ocena.Punkty.ShouldBe(1);
        ocena.PunktyMax.ShouldBe(2);
        ocena.PunktySurowe.ShouldBe(1);
        ocena.PewnoscOceny!.Value.Wartosc.ShouldBe(0.995m);
        ocena.Kryteria.Count.ShouldBe(2);
        ocena.Widocznosc.ShouldBe(WidocznoscOcenyDlaUcznia.Pelna);
        ocena.CreatedAt.ShouldBe(Teraz);
        ocena.NauczycielId.ShouldBeNull();
    }

    [Fact]
    public void PewnoscJestZaokraglanaDoCzterechMiejscIOgraniczonaDoZakresu()
    {
        new PewnoscOceny(0.99951m).Wartosc.ShouldBe(0.9995m);
        new PewnoscSurowa(0.12345m).Wartosc.ShouldBe(0.1234m);
        Should.Throw<NiezmiennikNaruszonyException>(() => new PewnoscOceny(1.0001m)).KodBledu.ShouldBe("PEWNOSC_OCENY_POZA_ZAKRESEM");
        Should.Throw<NiezmiennikNaruszonyException>(() => new PewnoscSurowa(-0.1m)).KodBledu.ShouldBe("PEWNOSC_SUROWA_POZA_ZAKRESEM");
    }

    [Fact]
    public void PunktyMusZmiescicSieWZakresieZeroDoPunktyMax()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(Dane(punkty: 3, punktyMax: 2)))
            .KodBledu.ShouldBe("OCENA_PUNKTY_POZA_ZAKRESEM");
        Should.NotThrow(() => Nowa(Dane(punkty: 0, punktyMax: 2, kryteria: [new OcenaKryterium(Kryterium1, 0, 0.9m)])));
    }

    [Fact]
    public void TypBleduWystepujeWtedyITylkoWtedyGdyJestBlednyKrokAlboBrakujacy()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(Dane(pierwszyBlednyKrok: null, typBledu: TypBledu.Rachunkowy)))
            .KodBledu.ShouldBe("OCENA_TYP_BLEDU_NIESPOJNY");
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(Dane(pierwszyBlednyKrok: 2, typBledu: null)))
            .KodBledu.ShouldBe("OCENA_TYP_BLEDU_NIESPOJNY");
    }

    [Fact]
    public void KrokPodejrzanyOdczytWymagaFlagiMozliwegoBleduOdczytu()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(Dane(krokPodejrzany: 2, mozliwyBladOdczytu: false)))
            .KodBledu.ShouldBe("OCENA_KROK_PODEJRZANY_NIESPOJNY");
        Should.NotThrow(() => Nowa(Dane(krokPodejrzany: 2, mozliwyBladOdczytu: true)));
    }

    [Fact]
    public void WstepnaZMozliwymBledemOdczytuMaWidocznoscWstrzymana()
    {
        var ocena = Nowa(Dane(status: StatusOceny.Wstepna, mozliwyBladOdczytu: true, krokPodejrzany: 2));

        ocena.Widocznosc.ShouldBe(WidocznoscOcenyDlaUcznia.WstrzymanaBladOdczytu);
    }

    [Fact]
    public void WskazowkaBezTypuBleduJestOdrzucona()
    {
        var wskazowka = new Wskazowka("Sprawdź krok 2.", ZrodloWskazowki.Szablon);

        Should.Throw<NiezmiennikNaruszonyException>(
                () => Nowa(Dane(punkty: 2, punktyMax: 2, pierwszyBlednyKrok: null, typBledu: null, wskazowka: wskazowka)))
            .KodBledu.ShouldBe("WSKAZOWKA_BEZ_BLEDU");
    }

    [Fact]
    public void UzasadnienieIKryteriaSaWymagane()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(Dane(uzasadnienie: " ")))
            .KodBledu.ShouldBe("OCENA_UZASADNIENIE_WYMAGANE");
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(Dane(kryteria: [])))
            .KodBledu.ShouldBe("OCENA_KRYTERIA_WYMAGANE");
    }

    [Fact]
    public void KryteriaNieMogaSiePowtarzacISumujaSieDoPunktowSurowych()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(Dane(kryteria:
            [new OcenaKryterium(Kryterium1, 1, null), new OcenaKryterium(Kryterium1, 0, null)])))
            .KodBledu.ShouldBe("OCENA_KRYTERIA_DUPLIKAT");
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(Dane(punkty: 1, kryteria:
            [new OcenaKryterium(Kryterium1, 1, null), new OcenaKryterium(Kryterium2, 1, null)])))
            .KodBledu.ShouldBe("OCENA_PUNKTY_SUROWE_NIESPOJNE");
    }

    [Fact]
    public void NowaOcenaNieMozeBycZastapiona()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(Dane(status: StatusOceny.Zastapiona)))
            .KodBledu.ShouldBe("OCENA_STATUS_NIEPRAWIDLOWY");
    }

    [Fact]
    public void WskazowkeMoznaUzupelnicTylkoJednokrotnie()
    {
        var ocena = Nowa();

        ocena.UzupelnijWskazowke(new Wskazowka("Sprawdź krok 2.", ZrodloWskazowki.AgentWyjasniajacy));

        ocena.Wskazowka!.Tresc.ShouldBe("Sprawdź krok 2.");
        ocena.Wskazowka.Zrodlo.ShouldBe(ZrodloWskazowki.AgentWyjasniajacy);
        Should.Throw<NiezmiennikNaruszonyException>(() => ocena.UzupelnijWskazowke(new Wskazowka("Inna.", ZrodloWskazowki.Szablon)))
            .KodBledu.ShouldBe("OCENA_NIEZMIENNA");
    }

    [Fact]
    public void WskazowkaWymagaTypuBleduIOcenyNiezastapionej()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => BezBledu().UzupelnijWskazowke(new Wskazowka("x", ZrodloWskazowki.Szablon)))
            .KodBledu.ShouldBe("WSKAZOWKA_BEZ_BLEDU");

        var zastapiona = Nowa();
        zastapiona.Zastap();
        Should.Throw<NiezmiennikNaruszonyException>(() => zastapiona.UzupelnijWskazowke(new Wskazowka("x", ZrodloWskazowki.Szablon)))
            .KodBledu.ShouldBe("OCENA_ZASTAPIONA");
    }

    [Fact]
    public void ZastapienieZmieniaStatusRazIJestNieodwracalne()
    {
        var ocena = Nowa();

        ocena.Zastap();

        ocena.Status.ShouldBe(StatusOceny.Zastapiona);
        Should.Throw<NiezmiennikNaruszonyException>(() => ocena.Zastap()).KodBledu.ShouldBe("OCENA_NIEZMIENNA");
    }

    [Fact]
    public void WskazowkaMaNiepustaTrescDoCzterystuZnakow()
    {
        Should.NotThrow(() => new Wskazowka(new string('a', 400), ZrodloWskazowki.Szablon));
        Should.Throw<NiezmiennikNaruszonyException>(() => new Wskazowka(new string('a', 401), ZrodloWskazowki.Szablon))
            .KodBledu.ShouldBe("WSKAZOWKA_ZA_DLUGA");
        Should.Throw<NiezmiennikNaruszonyException>(() => new Wskazowka(" ", ZrodloWskazowki.Szablon))
            .KodBledu.ShouldBe("WSKAZOWKA_PUSTA");
    }

    [Fact]
    public void OcenaKryteriumWalidujeWartosci()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => new OcenaKryterium(Guid.Empty, 1, null)).KodBledu.ShouldBe("OCENA_KRYTERIUM_ID_WYMAGANE");
        Should.Throw<NiezmiennikNaruszonyException>(() => new OcenaKryterium(Kryterium1, -1, null)).KodBledu.ShouldBe("OCENA_KRYTERIUM_PUNKTY_UJEMNE");
        Should.Throw<NiezmiennikNaruszonyException>(() => new OcenaKryterium(Kryterium1, 1, 1.1m)).KodBledu.ShouldBe("OCENA_KRYTERIUM_PRAWDOPODOBIENSTWO");
        new OcenaKryterium(Kryterium1, 1, 0.99951m).Prawdopodobienstwo.ShouldBe(0.9995m);
    }
}
