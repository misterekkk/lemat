// @Role: Testy jednostkowe PrzeslijRozwiazanieHandler (atrapy portów): kolejność kontroli (liczba zdjęć, Zadanie, wersja, rodzaj, stan, zdjęcia), mapowanie kodów zdjęć, zapis obiektów, zlecenie ROZPOZNAJ_PISMO z kluczem i ładunkiem, wyścig o nieukończone Rozwiązanie i sprzątanie obiektów po błędzie | @DependsOn: PrzeslijRozwiazanieHandler, NSubstitute, PrzezroczystaJednostkaPracy | @UsedBy: dotnet test | @Task: TASK-127
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Application.Content;
using Lemat.Application.Models;
using Lemat.Application.Recommendations;
using Lemat.Application.Solutions;
using Lemat.Application.Tests.Models;
using Lemat.Application.Tests.Support;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;
using Lemat.Domain.Solutions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Lemat.Application.Tests.Solutions;

public sealed class SubmitSolutionHandlerTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 2, 10, 5, 0, TimeSpan.Zero);
    private static readonly UczenId Uczen = new(Guid.Parse("11111111-1111-4111-8111-111111111111"));
    private static readonly ZadanieId IdZadania = new(Guid.Parse("a1000000-0000-4000-8000-000000000002"));
    private static readonly ZadanieWersjaId Wersja = new(Guid.Parse("b1000000-0000-4000-8000-000000000002"));
    private static readonly ZadanieWersjaId NowaWersja = new(Guid.Parse("b1000000-0000-4000-8000-0000000000f2"));
    private static readonly ElementSciezkiId Element = new(Guid.Parse("e5000000-0000-4000-8000-000000000011"));

    private readonly ITresciFasada _tresci = Substitute.For<ITresciFasada>();
    private readonly IRekomendacjeFasada _rekomendacje = Substitute.For<IRekomendacjeFasada>();
    private readonly IRozwiazanieRepozytorium _rozwiazania = Substitute.For<IRozwiazanieRepozytorium>();
    private readonly IRozwiazaniaFasada _fasada = Substitute.For<IRozwiazaniaFasada>();
    private readonly IMagazynObiektow _magazyn = Substitute.For<IMagazynObiektow>();
    private readonly IObrobkaObrazow _obrobka = Substitute.For<IObrobkaObrazow>();
    private readonly IZlecaniePrac _zlecanie = Substitute.For<IZlecaniePrac>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public SubmitSolutionHandlerTests()
    {
        UstawZadanie(RodzajZadania.OtwarteObliczeniowe, Wersja, StatusZadania.Opublikowane, ZasiegZadania.Globalny);
        _obrobka.NormalizujAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ObrazZnormalizowany(ci.Arg<ReadOnlyMemory<byte>>().ToArray(), 640, 480, new byte[32], false));
        _rekomendacje.RozpocznijProbeAsync(Arg.Any<UczenId>(), Arg.Any<ZadanieId>(), Arg.Any<UmiejetnoscId>(), Arg.Any<ZadanieWersjaId>(), Arg.Any<CancellationToken>())
            .Returns(new ElementSkrot(Element, 1, TypElementuSciezki.Zadanie, CelElementu.Nauka, new UmiejetnoscId(501), IdZadania, Wersja, null, null, false, StatusElementuSciezki.Rozpoczety, Teraz, Teraz, null));
        _zlecanie.ZlecAsync(Arg.Any<ZleceniePracy>(), Arg.Any<CancellationToken>()).Returns(new RezultatZlecenia(1, WynikZlecenia.Wstawiona));
        _fasada.PobierzDlaUczniaAsync(Arg.Any<RozwiazanieId>(), Uczen, Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult<RozwiazanieSkrot?>(Skrot(ci.Arg<RozwiazanieId>(), StatusRozwiazania.Przeslane)));
    }

    private PrzeslijRozwiazanieHandler Handler() =>
        new(_tresci, _rekomendacje, _rozwiazania, _fasada, _magazyn, _obrobka, _zlecanie, new PrzezroczystaJednostkaPracy(), new FakeTimeProvider(Teraz));

    private static PrzeslijRozwiazanieWejscie Wejscie(int liczbaZdjec = 2, string scenariusz = "domyslny") =>
        new(Uczen, Wersja, [.. Enumerable.Range(0, liczbaZdjec).Select(i => (ReadOnlyMemory<byte>)new byte[] { 0xFF, 0xD8, 0xFF, (byte)i })], scenariusz);

    private void UstawZadanie(RodzajZadania rodzaj, ZadanieWersjaId aktualna, StatusZadania status, ZasiegZadania zasieg)
    {
        _tresci.PobierzZadanieDoOcenyAsync(Wersja, Arg.Any<CancellationToken>()).Returns(new ZadanieDoOceny(
            IdZadania,
            Wersja,
            1,
            rodzaj,
            null,
            PoziomMatury.Podstawowy,
            new DzialId(5),
            new UmiejetnoscId(501),
            "Oblicz.",
            2,
            null,
            new RozwiazanieWzorcowe([new WariantWzorca(1, "w", [new KrokWzorcowy(1, "x", "k")])]),
            null,
            []));
        _tresci.PobierzZadanieUczniaAsync(IdZadania, Arg.Any<CancellationToken>()).Returns(new ZadanieUcznia(
            IdZadania,
            aktualna,
            1,
            status,
            zasieg,
            null,
            rodzaj,
            PoziomMatury.Podstawowy,
            new DzialId(5),
            new UmiejetnoscId(501),
            "Oblicz.",
            2,
            null,
            new PoziomTrudnosci(2),
            []));
    }

    private static RozwiazanieSkrot Skrot(RozwiazanieId id, StatusRozwiazania status) => new(
        id, Uczen, IdZadania, Wersja, Element, null, status, 1, 0, null, null, null, Teraz, null, null, null, null, Teraz, Teraz, 1, []);

    // ---------- kontrole przed zapisem ----------

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task NiepoprawnaLiczbaZdjecZwracaZaDuzoZdjecBezOdczytuZadania(int liczba)
    {
        var wynik = await Handler().WykonajAsync(Wejscie(liczba), Ct);

        wynik.Rodzaj.ShouldBe(RodzajWynikuPrzeslania.ZaDuzoZdjec);
        await _tresci.DidNotReceiveWithAnyArgs().PobierzZadanieDoOcenyAsync(default, Ct);
    }

    [Fact]
    public async Task NieznanaWersjaZwracaZadanieNieznane()
    {
        _tresci.PobierzZadanieDoOcenyAsync(Wersja, Arg.Any<CancellationToken>()).Returns((ZadanieDoOceny?)null);

        (await Handler().WykonajAsync(Wejscie(), Ct)).Rodzaj.ShouldBe(RodzajWynikuPrzeslania.ZadanieNieznane);
    }

    [Theory]
    [InlineData(StatusZadania.Wycofane, ZasiegZadania.Globalny)]
    [InlineData(StatusZadania.Opublikowane, ZasiegZadania.Prywatny)]
    public async Task ZadanieNieopublikowaneLubPrywatneZwracaZadanieNieznane(StatusZadania status, ZasiegZadania zasieg)
    {
        UstawZadanie(RodzajZadania.OtwarteObliczeniowe, Wersja, status, zasieg);

        (await Handler().WykonajAsync(Wejscie(), Ct)).Rodzaj.ShouldBe(RodzajWynikuPrzeslania.ZadanieNieznane);
    }

    [Fact]
    public async Task StaraWersjaZwracaAktualnaWersje()
    {
        UstawZadanie(RodzajZadania.OtwarteObliczeniowe, NowaWersja, StatusZadania.Opublikowane, ZasiegZadania.Globalny);

        var wynik = await Handler().WykonajAsync(Wejscie(), Ct);

        wynik.Rodzaj.ShouldBe(RodzajWynikuPrzeslania.WersjaNieaktualna);
        wynik.AktualnaWersja.ShouldBe(NowaWersja);
    }

    [Fact]
    public async Task ZadanieZamknieteNieMaRozwiazaniaZeZdjec()
    {
        UstawZadanie(RodzajZadania.Zamkniete, Wersja, StatusZadania.Opublikowane, ZasiegZadania.Globalny);

        (await Handler().WykonajAsync(Wejscie(), Ct)).Rodzaj.ShouldBe(RodzajWynikuPrzeslania.ZadanieZamkniete);
    }

    [Fact]
    public async Task NieukonczoneRozwiazanieBlokujeBezObrobkiIZapisuObiektow()
    {
        var istniejace = new RozwiazanieId(Guid.NewGuid());
        _rozwiazania.ZnajdzNieukonczoneAsync(Uczen, IdZadania, Arg.Any<CancellationToken>()).Returns(istniejace);
        _rozwiazania.PobierzDoOdczytuAsync(istniejace, Arg.Any<CancellationToken>()).Returns(Rozwiazanie(istniejace));

        var wynik = await Handler().WykonajAsync(Wejscie(), Ct);

        wynik.Rodzaj.ShouldBe(RodzajWynikuPrzeslania.StanNieprawidlowy);
        wynik.Istniejace!.Value.Id.ShouldBe(istniejace);
        wynik.Istniejace.Value.Status.ShouldBe(StatusRozwiazania.Przeslane);
        await _obrobka.DidNotReceiveWithAnyArgs().NormalizujAsync(default, Ct);
        await _magazyn.DidNotReceiveWithAnyArgs().ZapiszAsync(default!, default, default!, Ct);
    }

    [Theory]
    [InlineData(ZdjecieNieprawidloweException.KodZaDuze, RodzajWynikuPrzeslania.ZdjecieZaDuze)]
    [InlineData(ZdjecieNieprawidloweException.KodNieobslugiwanyFormat, RodzajWynikuPrzeslania.NieobslugiwanyFormat)]
    [InlineData(ZdjecieNieprawidloweException.KodNieprawidlowe, RodzajWynikuPrzeslania.ZdjecieNieprawidlowe)]
    public async Task KodZObrobkiObrazowJestMapowanyNaRodzajWyniku(string kod, RodzajWynikuPrzeslania oczekiwany)
    {
        _obrobka.NormalizujAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>()).ThrowsAsync(new ZdjecieNieprawidloweException(kod));

        var wynik = await Handler().WykonajAsync(Wejscie(), Ct);

        wynik.Rodzaj.ShouldBe(oczekiwany);
        await _magazyn.DidNotReceiveWithAnyArgs().ZapiszAsync(default!, default, default!, Ct);
    }

    // ---------- sukces i błędy po zapisie ----------

    [Fact]
    public async Task PoprawnePrzeslanieZapisujeObiektyDodajeRozwiazanieIZlecaPraceZKluczem()
    {
        Rozwiazanie? dodane = null;
        _rozwiazania.When(r => r.Dodaj(Arg.Any<Rozwiazanie>())).Do(ci => dodane = ci.Arg<Rozwiazanie>());
        ZleceniePracy? zlecenie = null;
        _zlecanie.ZlecAsync(Arg.Do<ZleceniePracy>(z => zlecenie = z), Arg.Any<CancellationToken>()).Returns(new RezultatZlecenia(1, WynikZlecenia.Wstawiona));

        var wynik = await Handler().WykonajAsync(Wejscie(2, "rozpoznanie-nieczytelne"), Ct);

        wynik.Rodzaj.ShouldBe(RodzajWynikuPrzeslania.Przyjete);
        wynik.Rozwiazanie.ShouldNotBeNull();
        dodane.ShouldNotBeNull();
        dodane.Status.ShouldBe(StatusRozwiazania.Przeslane);
        dodane.ElementSciezkiId.ShouldBe(Element);
        dodane.Zdjecia.Count.ShouldBe(2);
        dodane.Zdjecia.Select(z => z.Kolejnosc).ShouldBe([1, 2]);
        dodane.Zdjecia.ShouldAllBe(z => z.UsunPo == Teraz.AddDays(90) && z.KluczObiektu == $"zdjecia/{Uczen.Wartosc}/{dodane.Id.Wartosc}/{z.Id}.jpg");
        await _magazyn.Received(2).ZapiszAsync(Arg.Is<string>(k => k.StartsWith($"zdjecia/{Uczen.Wartosc}/{dodane.Id.Wartosc}/", StringComparison.Ordinal)), Arg.Any<ReadOnlyMemory<byte>>(), "image/jpeg", Arg.Any<CancellationToken>());
        zlecenie.ShouldNotBeNull();
        zlecenie.Typ.ShouldBe(TypPracy.RozpoznajPismo);
        zlecenie.KluczDeduplikacji.ShouldBe($"{dodane.Id.Wartosc}:1:0");
        var ladunek = System.Text.Json.JsonSerializer.Serialize(zlecenie.Ladunek);
        ladunek.ShouldContain(dodane.Id.Wartosc.ToString());
        ladunek.ShouldContain("rozpoznanie-nieczytelne");
        await _magazyn.DidNotReceiveWithAnyArgs().UsunAsync(default!, Ct);
    }

    [Fact]
    public async Task WyscigONieukonczoneRozwiazaniePoZapisieObiektowUsuwaObiektyIZwracaStan()
    {
        var istniejace = new RozwiazanieId(Guid.NewGuid());
        _rozwiazania.ZnajdzNieukonczoneAsync(Uczen, IdZadania, Arg.Any<CancellationToken>()).Returns((RozwiazanieId?)null, istniejace);
        _rozwiazania.PobierzDoOdczytuAsync(istniejace, Arg.Any<CancellationToken>()).Returns(Rozwiazanie(istniejace));

        var wynik = await Handler().WykonajAsync(Wejscie(2), Ct);

        wynik.Rodzaj.ShouldBe(RodzajWynikuPrzeslania.StanNieprawidlowy);
        _rozwiazania.DidNotReceiveWithAnyArgs().Dodaj(default!);
        await _zlecanie.DidNotReceiveWithAnyArgs().ZlecAsync(default!, Ct);
        await _magazyn.Received(2).UsunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BladPoZapisieObiektowUsuwaObiektyIPropagujeWyjatek()
    {
        _zlecanie.ZlecAsync(Arg.Any<ZleceniePracy>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("awaria"));

        await Should.ThrowAsync<InvalidOperationException>(() => Handler().WykonajAsync(Wejscie(3), Ct));

        await _magazyn.Received(3).UsunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static Rozwiazanie Rozwiazanie(RozwiazanieId id) => Domain.Solutions.Rozwiazanie.Przeslij(
        id,
        Uczen,
        IdZadania,
        Wersja,
        RodzajZadania.OtwarteObliczeniowe,
        Element,
        null,
        [ZdjecieRozwiazania.Utworz(Guid.NewGuid(), 1, 1, "zdjecia/a/b/c.jpg", 10, new byte[32], 10, 10, Teraz.AddDays(90), Teraz)],
        Teraz);
}
