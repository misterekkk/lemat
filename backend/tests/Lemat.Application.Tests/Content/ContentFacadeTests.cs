// @Role: Testy jednostkowe TresciFasada (atrapy repozytoriów): migawka do oceny, widok Ucznia bez klucza, kandydaci doboru, Działy, Umiejętności, opublikowana Wkładka | @DependsOn: TresciFasada, NSubstitute, Lemat.Domain.Content | @UsedBy: dotnet test | @Task: TASK-017, TASK-125
using Lemat.Application.Content;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using NSubstitute;

namespace Lemat.Application.Tests.Content;

public sealed class ContentFacadeTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IZadanieRepozytorium _zadania = Substitute.For<IZadanieRepozytorium>();
    private readonly IWkladkaTeoretycznaRepozytorium _wkladki = Substitute.For<IWkladkaTeoretycznaRepozytorium>();
    private readonly IDzialRepozytorium _dzialy = Substitute.For<IDzialRepozytorium>();
    private readonly IUmiejetnoscRepozytorium _umiejetnosci = Substitute.For<IUmiejetnoscRepozytorium>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private TresciFasada Fasada() => new(_zadania, _wkladki, _dzialy, _umiejetnosci);

    private static RozwiazanieWzorcowe Wzorzec() => new([new WariantWzorca(1, "w", [new KrokWzorcowy(1, "x", "k")])]);

    private static Zadanie Obliczeniowe()
    {
        var tresc = new TrescWersji(
            "Oblicz.",
            null,
            Wzorzec(),
            new WynikKoncowyWzorcowy(TypWynikuKoncowego.Wyrazenie, "2", "2"),
            2,
            [
                new KryteriumOceniania(new WariantRozwiazania(2), 1, "b", 2, true, null),
                new KryteriumOceniania(new WariantRozwiazania(1), 2, "a2", 1, true, 1),
                new KryteriumOceniania(new WariantRozwiazania(1), 1, "a1", 1, false, null),
            ]);
        return Zadanie.Utworz(Nowe(RodzajZadania.OtwarteObliczeniowe, null, tresc), Teraz);
    }

    private static Zadanie Zamkniete()
    {
        var opcje = OpcjeZamkniete.PrawdaFalsz(
            [new StwierdzeniePrawdaFalsz("1", "p", true), new StwierdzeniePrawdaFalsz("2", "q", false)]);
        var tresc = new TrescWersji("Wskaż.", opcje, Wzorzec(), null, 1, []);
        return Zadanie.Utworz(Nowe(RodzajZadania.Zamkniete, FormaZamknieta.PrawdaFalsz, tresc), Teraz);
    }

    private static NoweZadanie Nowe(RodzajZadania rodzaj, FormaZamknieta? forma, TrescWersji tresc) => new(
        new ZadanieId(Guid.CreateVersion7(Teraz)),
        PochodzenieZadania.BazoweNauczyciel,
        ZasiegZadania.Globalny,
        rodzaj,
        forma,
        new DzialId(5),
        PoziomMatury.Podstawowy,
        null,
        null,
        null,
        null,
        StatusZadania.Opublikowane,
        new PoziomTrudnosci(3),
        false,
        new UmiejetnoscId(501),
        [],
        tresc);

    [Fact]
    public async Task ZadanieDoOcenyZawieraWzorceIKryteriaPosortowane()
    {
        var zadanie = Obliczeniowe();
        _zadania.PobierzPoWersjiAsync(zadanie.AktualnaWersjaId, Ct).Returns(zadanie);

        var wynik = (await Fasada().PobierzZadanieDoOcenyAsync(zadanie.AktualnaWersjaId, Ct))!;

        wynik.ZadanieId.ShouldBe(zadanie.Id);
        wynik.ZadanieWersjaId.ShouldBe(zadanie.AktualnaWersjaId);
        wynik.PunktyMax.ShouldBe(2);
        wynik.UmiejetnoscGlowna.ShouldBe(new UmiejetnoscId(501));
        wynik.WynikKoncowyWzorcowy!.Latex.ShouldBe("2");
        wynik.KryteriaOceniania.Select(k => (k.Wariant.Wartosc, k.NrKryterium)).ShouldBe([(1, 1), (1, 2), (2, 1)]);
    }

    [Fact]
    public async Task ZadanieDoOcenyDlaNieznanejWersjiToNull()
    {
        var wersja = new ZadanieWersjaId(Guid.NewGuid());
        _zadania.PobierzPoWersjiAsync(wersja, Ct).Returns((Zadanie?)null);

        (await Fasada().PobierzZadanieDoOcenyAsync(wersja, Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task ZadanieDoOcenyDlaZastapionejWersjiZwracaTrescTejWersji()
    {
        var zadanie = Obliczeniowe();
        var pierwsza = zadanie.AktualnaWersja;
        zadanie.Popraw(
            new TrescWersji("Nowa.", null, Wzorzec(), new WynikKoncowyWzorcowy(TypWynikuKoncowego.Tekst, "t", null), 1,
                [new KryteriumOceniania(new WariantRozwiazania(1), 1, "k", 1, true, null)]),
            new Lemat.Domain.Accounts.UzytkownikId(Guid.NewGuid()),
            Teraz.AddDays(1));
        _zadania.PobierzPoWersjiAsync(pierwsza.Id, Ct).Returns(zadanie);

        var wynik = (await Fasada().PobierzZadanieDoOcenyAsync(pierwsza.Id, Ct))!;

        wynik.NrWersji.ShouldBe(1);
        wynik.TrescMd.ShouldBe("Oblicz.");
        wynik.PunktyMax.ShouldBe(2);
    }

    [Fact]
    public async Task WidokUczniaNieZawieraKluczaOdpowiedzi()
    {
        var zadanie = Zamkniete();
        _zadania.PobierzDoOdczytuAsync(zadanie.Id, Ct).Returns(zadanie);

        var widok = (await Fasada().PobierzZadanieUczniaAsync(zadanie.Id, Ct))!;

        widok.Opcje!.Forma.ShouldBe(FormaZamknieta.PrawdaFalsz);
        widok.Opcje.Stwierdzenia.Select(s => s.Id).ShouldBe(["1", "2"]);
        widok.Status.ShouldBe(StatusZadania.Opublikowane);
        widok.Zasieg.ShouldBe(ZasiegZadania.Globalny);
        widok.PunktyMax.ShouldBe(1);
        typeof(StwierdzenieDlaUcznia).GetProperties().Select(p => p.Name).ShouldBe(["Id", "TrescMd"]);
    }

    [Fact]
    public async Task WidokUczniaDlaZadaniaOtwartegoNieMaOpcji()
    {
        var zadanie = Obliczeniowe();
        _zadania.PobierzDoOdczytuAsync(zadanie.Id, Ct).Returns(zadanie);

        (await Fasada().PobierzZadanieUczniaAsync(zadanie.Id, Ct))!.Opcje.ShouldBeNull();
        _zadania.PobierzDoOdczytuAsync(new ZadanieId(Guid.NewGuid()), Ct).Returns((Zadanie?)null);
        (await Fasada().PobierzZadanieUczniaAsync(new ZadanieId(Guid.NewGuid()), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task ListaZadanUczniaSortujePoPoziomieIIdOrazStronicujePoKursorze()
    {
        var pozycje = new[]
        {
            new PozycjaListyZadan(new PoziomTrudnosci(3), new ZadanieId(Guid.Parse("a1000000-0000-4000-8000-000000000004"))),
            new PozycjaListyZadan(new PoziomTrudnosci(1), new ZadanieId(Guid.Parse("a1000000-0000-4000-8000-000000000005"))),
            new PozycjaListyZadan(new PoziomTrudnosci(1), new ZadanieId(Guid.Parse("a1000000-0000-4000-8000-000000000002"))),
        };
        _zadania.ListaOpublikowanychAsync(null, Ct).Returns(pozycje);
        _zadania.PobierzDoOdczytuAsync(Arg.Any<IReadOnlyCollection<ZadanieId>>(), Ct).Returns(_ => Task.FromResult<IReadOnlyList<Zadanie>>([Obliczeniowe()]));

        var pierwsza = await Fasada().ListaZadanUczniaAsync(null, null, 2, Ct);
        var druga = await Fasada().ListaZadanUczniaAsync(null, pozycje[1], 2, Ct);

        pierwsza.MaWiecej.ShouldBeTrue();
        await _zadania.Received(1).PobierzDoOdczytuAsync(
            Arg.Is<IReadOnlyCollection<ZadanieId>>(i => i.SequenceEqual(new[] { pozycje[2].ZadanieId, pozycje[1].ZadanieId })), Ct);
        druga.MaWiecej.ShouldBeFalse();
        await _zadania.Received(1).PobierzDoOdczytuAsync(
            Arg.Is<IReadOnlyCollection<ZadanieId>>(i => i.SequenceEqual(new[] { pozycje[0].ZadanieId })), Ct);
    }

    [Fact]
    public async Task KandydaciBezRodzajowNieOdpytujaRepozytorium()
    {
        var wynik = await Fasada().PobierzKandydatowDoboruAsync(
            new FiltrKandydatow(PoziomMatury.Podstawowy, new UmiejetnoscId(501), new HashSet<RodzajZadania>()), Ct);

        wynik.ShouldBeEmpty();
        await _zadania.DidNotReceiveWithAnyArgs().PobierzKandydatowAsync(default!, Ct);
    }

    [Fact]
    public async Task KandydaciSaPobieraniePrzezRepozytorium()
    {
        var filtr = new FiltrKandydatow(PoziomMatury.Podstawowy, new UmiejetnoscId(501), new HashSet<RodzajZadania> { RodzajZadania.Zamkniete });
        var kandydat = new ZadanieDoDoboru(
            new ZadanieId(Guid.NewGuid()), new ZadanieWersjaId(Guid.NewGuid()), RodzajZadania.Zamkniete, FormaZamknieta.WyborJednokrotny,
            0, new UmiejetnoscId(501), [], new PoziomTrudnosci(3), 0m, 0, true);
        _zadania.PobierzKandydatowAsync(filtr, Ct).Returns([kandydat]);

        (await Fasada().PobierzKandydatowDoboruAsync(filtr, Ct)).ShouldBe([kandydat]);
    }

    [Fact]
    public async Task DzialyIUmiejetnosciSaMapowaneNaSkroty()
    {
        _dzialy.ListaAsync(Ct).Returns([Dzial.Utworz(new DzialId(13), "XIII", "Optymalizacja", true, 13)]);
        _umiejetnosci.ListaAsync(new DzialId(13), Ct).Returns(
            [Umiejetnosc.Utworz(new UmiejetnoscId(2), new DzialId(13), "R.XIII.2", "Pochodna", true, 0.5m, [new UmiejetnoscId(1)])]);

        var dzialy = await Fasada().ListaDzialowAsync(Ct);
        var umiejetnosci = await Fasada().ListaUmiejetnosciAsync(new DzialId(13), Ct);

        dzialy.ShouldBe([new DzialSkrot(new DzialId(13), "XIII", "Optymalizacja", true, 13)]);
        umiejetnosci.Single().Wymagane.ShouldBe([new UmiejetnoscId(1)]);
        umiejetnosci.Single().Kod.ShouldBe("R.XIII.2");
    }

    [Fact]
    public async Task OpublikowanaWkladkaJestMapowanaNaSkrot()
    {
        var wkladka = WkladkaTeoretyczna.UtworzSzkic(
            new WkladkaTeoretycznaId(Guid.NewGuid()), new UmiejetnoscId(501), "T", "Treść", 4, false, false, 0m, Teraz);
        _wkladki.PobierzOpublikowanaAsync(new UmiejetnoscId(501), Ct).Returns(wkladka);
        _wkladki.PobierzOpublikowanaAsync(new UmiejetnoscId(502), Ct).Returns((WkladkaTeoretyczna?)null);

        (await Fasada().PobierzOpublikowanaWkladkeAsync(new UmiejetnoscId(501), Ct)).ShouldBe(new WkladkaDoDoboru(wkladka.Id, new UmiejetnoscId(501), 4));
        (await Fasada().PobierzOpublikowanaWkladkeAsync(new UmiejetnoscId(502), Ct)).ShouldBeNull();
    }
}
