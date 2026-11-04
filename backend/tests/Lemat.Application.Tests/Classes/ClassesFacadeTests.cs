// @Role: Testy jednostkowe KlasyFasada (atrapa repozytorium): zakładanie z Kodem zaproszenia, dołączanie kodem, uprawnienia obiektowe prowadzącego (KLASA_NIE_ZNALEZIONA), odnowienie i unieważnienie kodu, archiwizacja, Przekazanie, ponawianie losowania kodu | @DependsOn: KlasyFasada, NSubstitute, Lemat.Domain.Classes | @UsedBy: dotnet test | @Task: TASK-018
using Lemat.Application.Classes;
using Lemat.Application.Tests.Support;
using Lemat.Domain.Accounts;
using Lemat.Domain.Classes;
using Lemat.Domain.Common;
using NSubstitute;

namespace Lemat.Application.Tests.Classes;

public sealed class ClassesFacadeTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly NauczycielId Prowadzacy = new(Guid.NewGuid());

    private readonly IKlasaRepozytorium _klasy = Substitute.For<IKlasaRepozytorium>();
    private readonly PrzezroczystaJednostkaPracy _jednostkaPracy = new();
    private readonly ZegarStaly _czas = new(Teraz);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private KlasyFasada Fasada() => new(_klasy, _jednostkaPracy, _czas);

    private static Klasa IstniejacaKlasa(NauczycielId? prowadzacy = null) =>
        Klasa.Zaloz(new KlasaId(Guid.NewGuid()), prowadzacy ?? Prowadzacy, "3A", PoziomMatury.Podstawowy, GeneratorKoduZaproszenia.Nowy(Teraz), Teraz);

    [Fact]
    public async Task ZalozenieKlasyDodajeJaZKodemWaznymCzternascieDni()
    {
        var skrot = await Fasada().ZalozAsync(Prowadzacy, " 3A ", PoziomMatury.Rozszerzony, Ct);

        skrot.Nazwa.ShouldBe("3A");
        skrot.NauczycielId.ShouldBe(Prowadzacy);
        skrot.KodZaproszenia.ShouldNotBeNull().WaznyDo.ShouldBe(Teraz.AddDays(14));
        skrot.KodZaproszenia!.Wartosc.Length.ShouldBe(8);
        skrot.LiczbaUczniow.ShouldBe(0);
        skrot.Version.ShouldBe(1);
        _klasy.Received(1).Dodaj(Arg.Is<Klasa>(k => k.Id == skrot.Id));
        _jednostkaPracy.LiczbaWykonan.ShouldBe(1);
    }

    [Fact]
    public async Task ZajetyKodJestLosowanyPonownie()
    {
        _klasy.KodZajetyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true, true, false);

        await Fasada().ZalozAsync(Prowadzacy, "3A", PoziomMatury.Podstawowy, Ct);

        await _klasy.Received(3).KodZajetyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PiecZajetychKodowKonczySieBledem()
    {
        _klasy.KodZajetyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var wyjatek = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().ZalozAsync(Prowadzacy, "3A", PoziomMatury.Podstawowy, Ct));

        wyjatek.KodBledu.ShouldBe("KOD_ZAPROSZENIA_ZAJETY");
    }

    [Fact]
    public async Task DolaczenieKodemDodajeCzlonkaIUkrywaKodWWidokuUcznia()
    {
        var klasa = IstniejacaKlasa();
        var uczen = new UczenId(Guid.NewGuid());
        _klasy.PobierzPoKodzieAsync(klasa.KodZaproszenia.Wartosc, Arg.Any<CancellationToken>()).Returns(klasa);

        var skrot = await Fasada().DolaczAsync(uczen, klasa.KodZaproszenia.Wartosc.ToLowerInvariant(), Ct);

        klasa.CzyCzlonek(uczen).ShouldBeTrue();
        skrot.LiczbaUczniow.ShouldBe(1);
        skrot.KodZaproszenia.ShouldBeNull();
    }

    [Fact]
    public async Task NieznanyKodDajeKodZaproszeniaNiewazny()
    {
        _klasy.PobierzPoKodzieAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Klasa?)null);

        var wyjatek = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().DolaczAsync(new UczenId(Guid.NewGuid()), "ZLYKOD22", Ct));

        wyjatek.KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEWAZNY");
    }

    [Fact]
    public async Task WygaslyKodDajeKodZaproszeniaNiewazny()
    {
        var klasa = IstniejacaKlasa();
        _klasy.PobierzPoKodzieAsync(klasa.KodZaproszenia.Wartosc, Arg.Any<CancellationToken>()).Returns(klasa);
        _czas.Ustaw(Teraz.AddDays(15));

        var wyjatek = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().DolaczAsync(new UczenId(Guid.NewGuid()), klasa.KodZaproszenia.Wartosc, Ct));

        wyjatek.KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEWAZNY");
    }

    [Fact]
    public async Task CudzaKlasaJestNiewidocznaDlaInnegoNauczyciela()
    {
        var klasa = IstniejacaKlasa();
        var obcy = new NauczycielId(Guid.NewGuid());
        _klasy.PobierzDoOdczytuAsync(klasa.Id, Arg.Any<CancellationToken>()).Returns(klasa);
        _klasy.PobierzAsync(klasa.Id, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(klasa);

        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().PobierzDlaNauczycielaAsync(obcy, klasa.Id, Ct))).KodBledu.ShouldBe("KLASA_NIE_ZNALEZIONA");
        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().OdnowKodAsync(obcy, klasa.Id, null, Ct))).KodBledu.ShouldBe("KLASA_NIE_ZNALEZIONA");
        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().ArchiwizujAsync(obcy, klasa.Id, null, Ct))).KodBledu.ShouldBe("KLASA_NIE_ZNALEZIONA");
        (await Fasada().PobierzDlaNauczycielaAsync(Prowadzacy, klasa.Id, Ct)).KodZaproszenia.ShouldNotBeNull();
    }

    [Fact]
    public async Task OdnowienieKoduZastepujeKodIUniewaznienieGoBlokuje()
    {
        var klasa = IstniejacaKlasa();
        var staryKod = klasa.KodZaproszenia.Wartosc;
        _klasy.PobierzAsync(klasa.Id, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(klasa);

        var nowy = await Fasada().OdnowKodAsync(Prowadzacy, klasa.Id, 1, Ct);

        nowy.Wartosc.ShouldBe(klasa.KodZaproszenia.Wartosc);
        nowy.Wartosc.ShouldNotBe(staryKod);
        await Fasada().UniewaznKodAsync(Prowadzacy, klasa.Id, 1, Ct);
        klasa.KodZaproszenia.CzyWazny(nowy.Wartosc, Teraz).ShouldBeFalse();
    }

    [Fact]
    public async Task OpuszczenieKlasyUsuwaCzlonkaAObcyUczenDostajeBrakKlasy()
    {
        var klasa = IstniejacaKlasa();
        var uczen = new UczenId(Guid.NewGuid());
        klasa.Dolacz(uczen, klasa.KodZaproszenia.Wartosc, Teraz);
        _klasy.PobierzAsync(klasa.Id, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(klasa);

        (await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().OpuscAsync(new UczenId(Guid.NewGuid()), klasa.Id, Ct))).KodBledu.ShouldBe("KLASA_NIE_ZNALEZIONA");
        await Fasada().OpuscAsync(uczen, klasa.Id, Ct);

        klasa.CzyCzlonek(uczen).ShouldBeFalse();
    }

    [Fact]
    public async Task PrzekazanieKlasyZmieniaProwadzacegoIZglaszaZdarzenie()
    {
        var klasa = IstniejacaKlasa();
        var nowy = new NauczycielId(Guid.NewGuid());
        _klasy.PobierzAsync(klasa.Id, 3, Arg.Any<CancellationToken>()).Returns(klasa);

        var skrot = await Fasada().PrzekazAsync(klasa.Id, nowy, 3, Ct);

        skrot.NauczycielId.ShouldBe(nowy);
        klasa.Zdarzenia.ShouldHaveSingleItem().ShouldBeOfType<KlasaPrzekazana>();
    }

    [Fact]
    public async Task NajpozniejDolaczonaKlasaPomijaZarchiwizowane()
    {
        var uczen = new UczenId(Guid.NewGuid());
        var archiwalna = IstniejacaKlasa();
        archiwalna.Archiwizuj(Teraz);
        var aktywna = IstniejacaKlasa();
        _klasy.ListaUczniaAsync(uczen, Arg.Any<CancellationToken>()).Returns([archiwalna, aktywna]);

        var wynik = await Fasada().PobierzNajpozniejDolaczonaKlaseAsync(uczen, Ct);

        wynik.ShouldNotBeNull().Id.ShouldBe(aktywna.Id);
    }
}

public sealed class ZegarStaly(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _teraz = start;

    public void Ustaw(DateTimeOffset teraz) => _teraz = teraz;

    public override DateTimeOffset GetUtcNow() => _teraz;
}
