// @Role: Testy agregatu Zadanie: niezmienniki CHECK 7.3 (forma, arkusz, zlecenie, diagnostyczne), wersjonowanie (nowa wersja zamyka starą, zastapiona_at), maszyna stanów 8.4, Promuj | @DependsOn: Lemat.Domain.Content.Zadanie | @UsedBy: dotnet test | @Task: TASK-017
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Tests.Content;

public sealed class ZadanieTests
{
    private static readonly DateTimeOffset Pozniej = ContentTestData.Teraz.AddDays(1);

    [Fact]
    public void NoweZadanieMaWersjeJedenIJestAktualna()
    {
        var zadanie = ContentTestData.Zadanie();

        zadanie.Wersje.Count.ShouldBe(1);
        zadanie.AktualnaWersja.NrWersji.ShouldBe(1);
        zadanie.AktualnaWersja.ZastapionaAt.ShouldBeNull();
        zadanie.AktualnaWersjaId.ShouldBe(zadanie.AktualnaWersja.Id);
        zadanie.Version.ShouldBe(1);
        zadanie.UmiejetnoscGlowna.ShouldBe(new UmiejetnoscId(501));
    }

    [Fact]
    public void UmiejetnosciDodatkoweNieDublujaGlownej()
    {
        var dane = ContentTestData.Nowe(dodatkowe: [new UmiejetnoscId(501), new UmiejetnoscId(502), new UmiejetnoscId(502)]);

        var zadanie = ContentTestData.Utworz(dane);

        zadanie.Umiejetnosci.Count.ShouldBe(2);
        zadanie.UmiejetnosciDodatkowe.ShouldBe([new UmiejetnoscId(502)]);
        zadanie.Umiejetnosci.Count(u => u.Glowna).ShouldBe(1);
    }

    [Fact]
    public void PoprawaTworzyNowaWersjeIZamykaStara()
    {
        var zadanie = ContentTestData.Zadanie();
        var pierwsza = zadanie.AktualnaWersja;

        var druga = zadanie.Popraw(ContentTestData.TrescObliczeniowa("Poprawiona treść."), ContentTestData.Autor(), Pozniej);

        druga.NrWersji.ShouldBe(2);
        druga.ZastapionaAt.ShouldBeNull();
        pierwsza.ZastapionaAt.ShouldBe(Pozniej);
        pierwsza.TrescMd.ShouldBe("Oblicz $x$.");
        zadanie.AktualnaWersja.ShouldBe(druga);
        zadanie.AktualnaWersjaId.ShouldBe(druga.Id);
        zadanie.Wersje.Count.ShouldBe(2);
        zadanie.Wersje.Count(w => w.ZastapionaAt is null).ShouldBe(1);

        var trzecia = zadanie.Popraw(ContentTestData.TrescObliczeniowa("Trzecia."), ContentTestData.Autor(), Pozniej.AddHours(1));
        trzecia.NrWersji.ShouldBe(3);
        druga.ZastapionaAt.ShouldBe(Pozniej.AddHours(1));
        pierwsza.ZastapionaAt.ShouldBe(Pozniej);
    }

    [Theory]
    [InlineData(StatusZadania.Wycofane)]
    [InlineData(StatusZadania.Odrzucone)]
    [InlineData(StatusZadania.OdrzuconeWWalidacji)]
    public void PoprawaZadaniaWStanieKoncowymJestOdrzucona(StatusZadania status)
    {
        var zadanie = ContentTestData.Zadanie();
        WymusStatus(zadanie, status);

        Should.Throw<NiezmiennikNaruszonyException>(() => zadanie.Popraw(ContentTestData.TrescObliczeniowa(), ContentTestData.Autor(), Pozniej))
            .KodBledu.ShouldBe("ZADANIE_NIEPRAWIDLOWY_STAN");
    }

    [Fact]
    public void PoprawaNieMozeZmienicRodzajuTresci()
    {
        var zadanie = ContentTestData.Zadanie(RodzajZadania.Zamkniete);

        Should.Throw<NiezmiennikNaruszonyException>(() => zadanie.Popraw(ContentTestData.TrescObliczeniowa(), ContentTestData.Autor(), Pozniej))
            .KodBledu.ShouldBe("OPCJE_ZAMKNIETE_WYMAGANE");
        zadanie.Wersje.Count.ShouldBe(1);
        zadanie.AktualnaWersja.ZastapionaAt.ShouldBeNull();
    }

    [Fact]
    public void ForaZamknietaZgodnaZRodzajem()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(RodzajZadania.Zamkniete, forma: null) with { FormaZamknieta = null }))
            .KodBledu.ShouldBe("ZADANIE_FORMA_NIESPOJNA");
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe() with { FormaZamknieta = FormaZamknieta.PrawdaFalsz }))
            .KodBledu.ShouldBe("ZADANIE_FORMA_NIESPOJNA");
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(RodzajZadania.Zamkniete) with { FormaZamknieta = FormaZamknieta.PrawdaFalsz }))
            .KodBledu.ShouldBe("OPCJE_ZAMKNIETE_WYMAGANE");
    }

    [Fact]
    public void ArkuszIZlecenieSaZwiazaneZPochodzeniem()
    {
        var arkusz = new ArkuszCkeId(Guid.NewGuid());
        var zlecenie = new ZlecenieGenerowaniaId(Guid.NewGuid());

        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(pochodzenie: PochodzenieZadania.BazoweArkuszCke)))
            .KodBledu.ShouldBe("ZADANIE_ARKUSZ_NIESPOJNY");
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(arkusz: arkusz)))
            .KodBledu.ShouldBe("ZADANIE_ARKUSZ_NIESPOJNY");
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(pochodzenie: PochodzenieZadania.Wygenerowane, status: StatusZadania.Walidacja)))
            .KodBledu.ShouldBe("ZADANIE_ZLECENIE_NIESPOJNE");
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(zlecenie: zlecenie)))
            .KodBledu.ShouldBe("ZADANIE_ZLECENIE_NIESPOJNE");

        var bazowe = ContentTestData.Utworz(ContentTestData.Nowe(pochodzenie: PochodzenieZadania.BazoweArkuszCke, arkusz: arkusz));
        bazowe.CzyBazowe.ShouldBeTrue();
        bazowe.CzyWygenerowane.ShouldBeFalse();
        var wygenerowane = ContentTestData.Utworz(ContentTestData.Nowe(pochodzenie: PochodzenieZadania.Wygenerowane, zlecenie: zlecenie, status: StatusZadania.Walidacja));
        wygenerowane.CzyWygenerowane.ShouldBeTrue();
    }

    [Fact]
    public void ZadanieZArkuszaCkeJestZawszeGlobalne()
    {
        var arkusz = new ArkuszCkeId(Guid.NewGuid());

        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(
            ContentTestData.Nowe(pochodzenie: PochodzenieZadania.BazoweArkuszCke, arkusz: arkusz, zasieg: ZasiegZadania.Prywatny)))
            .KodBledu.ShouldBe("ZASIEG_NIEDOZWOLONY");
        var zadanie = ContentTestData.Utworz(ContentTestData.Nowe(pochodzenie: PochodzenieZadania.BazoweArkuszCke, arkusz: arkusz));
        Should.Throw<NiezmiennikNaruszonyException>(() => zadanie.Promuj(ZasiegZadania.Prywatny)).KodBledu.ShouldBe("ZASIEG_NIEDOZWOLONY");
    }

    [Theory]
    [InlineData(RodzajZadania.OtwarteObliczeniowe, ZasiegZadania.Globalny)]
    [InlineData(RodzajZadania.OtwarteZDowodem, ZasiegZadania.Globalny)]
    [InlineData(RodzajZadania.Zamkniete, ZasiegZadania.Prywatny)]
    public void DiagnostyczneTylkoDlaZamknietychGlobalnych(RodzajZadania rodzaj, ZasiegZadania zasieg)
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(rodzaj, zasieg: zasieg, diagnostyczne: true)))
            .KodBledu.ShouldBe("ZADANIE_DIAGNOSTYCZNE_NIEDOZWOLONE");
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(rodzaj, zasieg: zasieg)).UstawDiagnostyczne(true))
            .KodBledu.ShouldBe("ZADANIE_DIAGNOSTYCZNE_NIEDOZWOLONE");
    }

    [Fact]
    public void DiagnostyczneZamknieteGlobalneJestDozwolone()
    {
        var zadanie = ContentTestData.Utworz(ContentTestData.Nowe(RodzajZadania.Zamkniete, diagnostyczne: true));

        zadanie.Diagnostyczne.ShouldBeTrue();
        Should.Throw<NiezmiennikNaruszonyException>(() => zadanie.Promuj(ZasiegZadania.Prywatny)).KodBledu.ShouldBe("ZASIEG_NIEDOZWOLONY");
    }

    [Fact]
    public void PoczatkowyStatusToWalidacjaAlboOpublikowane() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(status: StatusZadania.Wycofane)))
            .KodBledu.ShouldBe("ZADANIE_NIEPRAWIDLOWY_STAN");

    [Fact]
    public void MaszynaStanowWalidacjaDoPrzegladuDoOpublikowaniaIWycofania()
    {
        var zadanie = ContentTestData.Utworz(ContentTestData.Nowe(status: StatusZadania.Walidacja));

        zadanie.PrzekazDoPrzegladu();
        zadanie.Status.ShouldBe(StatusZadania.DoPrzegladu);
        zadanie.Opublikuj(ZasiegZadania.Prywatny);
        zadanie.Status.ShouldBe(StatusZadania.Opublikowane);
        zadanie.Zasieg.ShouldBe(ZasiegZadania.Prywatny);
        zadanie.Promuj(ZasiegZadania.Globalny);
        zadanie.Zasieg.ShouldBe(ZasiegZadania.Globalny);
        zadanie.Wycofaj();
        zadanie.Status.ShouldBe(StatusZadania.Wycofane);
    }

    [Fact]
    public void MaszynaStanowOdrzucenia()
    {
        var wWalidacji = ContentTestData.Utworz(ContentTestData.Nowe(status: StatusZadania.Walidacja));
        wWalidacji.OdrzucWWalidacji();
        wWalidacji.Status.ShouldBe(StatusZadania.OdrzuconeWWalidacji);

        var doPrzegladu = ContentTestData.Utworz(ContentTestData.Nowe(status: StatusZadania.Walidacja));
        doPrzegladu.PrzekazDoPrzegladu();
        doPrzegladu.Odrzuc();
        doPrzegladu.Status.ShouldBe(StatusZadania.Odrzucone);
    }

    [Fact]
    public void NiedozwolonePrzejsciaSaOdrzucane()
    {
        var opublikowane = ContentTestData.Zadanie();
        Should.Throw<NiezmiennikNaruszonyException>(opublikowane.PrzekazDoPrzegladu).KodBledu.ShouldBe("ZADANIE_NIEPRAWIDLOWY_STAN");
        Should.Throw<NiezmiennikNaruszonyException>(opublikowane.Odrzuc).KodBledu.ShouldBe("ZADANIE_NIEPRAWIDLOWY_STAN");
        Should.Throw<NiezmiennikNaruszonyException>(() => opublikowane.Opublikuj()).KodBledu.ShouldBe("ZADANIE_NIEPRAWIDLOWY_STAN");
        opublikowane.Wycofaj();
        Should.Throw<NiezmiennikNaruszonyException>(opublikowane.Wycofaj).KodBledu.ShouldBe("ZADANIE_NIEPRAWIDLOWY_STAN");
        Should.Throw<NiezmiennikNaruszonyException>(() => opublikowane.Opublikuj()).KodBledu.ShouldBe("ZADANIE_NIEPRAWIDLOWY_STAN");
    }

    [Fact]
    public void WersjaPilnujeTresciIPunktow()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(tresc: ContentTestData.TrescObliczeniowa(new string('x', 8001)))))
            .KodBledu.ShouldBe("TRESC_ZADANIA_NIEPRAWIDLOWA");
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(tresc: ContentTestData.TrescObliczeniowa(" "))))
            .KodBledu.ShouldBe("TRESC_ZADANIA_NIEPRAWIDLOWA");
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(tresc: ContentTestData.TrescObliczeniowa() with { PunktyMax = 7 })))
            .KodBledu.ShouldBe("PUNKTY_ZADANIA_POZA_ZAKRESEM");
        ContentTestData.Utworz(ContentTestData.Nowe(tresc: ContentTestData.TrescObliczeniowa(new string('x', 8000)))).AktualnaWersja.TrescMd.Length.ShouldBe(8000);
    }

    [Fact]
    public void WersjaWgRodzajuWymagaOdpowiednichPol()
    {
        // obliczeniowe bez wyniku końcowego
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(
            tresc: ContentTestData.TrescObliczeniowa() with { WynikKoncowyWzorcowy = null })))
            .KodBledu.ShouldBe("WYNIK_WZORCOWY_WYMAGANY");
        // dowód z wynikiem końcowym
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(
            RodzajZadania.OtwarteZDowodem, tresc: ContentTestData.TrescDowodu() with { WynikKoncowyWzorcowy = ContentTestData.Wynik() })))
            .KodBledu.ShouldBe("WYNIK_WZORCOWY_NIEDOZWOLONY");
        // zamknięte z kryteriami
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(
            RodzajZadania.Zamkniete, tresc: ContentTestData.TrescZamknieta() with { Kryteria = [ContentTestData.Kryterium(1, 1, 1)] })))
            .KodBledu.ShouldBe("KRYTERIA_NIESPOJNE");
        // zamknięte za 2 punkty
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(
            RodzajZadania.Zamkniete, tresc: ContentTestData.TrescZamknieta() with { PunktyMax = 2 })))
            .KodBledu.ShouldBe("PUNKTY_ZADANIA_POZA_ZAKRESEM");
        // obliczeniowe z opcjami zamkniętymi
        Should.Throw<NiezmiennikNaruszonyException>(() => ContentTestData.Utworz(ContentTestData.Nowe(
            tresc: ContentTestData.TrescObliczeniowa() with { OpcjeZamkniete = ContentTestData.WyborA() })))
            .KodBledu.ShouldBe("OPCJE_ZAMKNIETE_NIEDOZWOLONE");
    }

    private static void WymusStatus(Zadanie zadanie, StatusZadania docelowy)
    {
        switch (docelowy)
        {
            case StatusZadania.Wycofane:
                zadanie.Wycofaj();
                break;
            case StatusZadania.Odrzucone:
                zadanie.Wycofaj();
                typeof(Zadanie).GetProperty(nameof(Zadanie.Status))!.SetValue(zadanie, StatusZadania.Odrzucone);
                break;
            default:
                typeof(Zadanie).GetProperty(nameof(Zadanie.Status))!.SetValue(zadanie, docelowy);
                break;
        }
    }
}
