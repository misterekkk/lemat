// @Role: Testy agregatu Profil ucznia: stosowanie Wyników modelem Elo (główna i dodatkowe Umiejętności), idempotencja, powtórki, propagacja w Dziale po Teście diagnostycznym, deterministyczne odtwarzanie od stanu DOMYŚLNEGO, property-test monotoniczności | @DependsOn: Lemat.Domain.Profile.ProfilUcznia, WynikZadania, ParametryEloZadania | @UsedBy: dotnet test | @Task: TASK-019
using FsCheck.Xunit;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;

namespace Lemat.Domain.Tests.Profile;

public sealed class ProfileTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly UczenId Uczen = new(Guid.NewGuid());

    private static readonly UmiejetnoscId A = new(1);
    private static readonly UmiejetnoscId B = new(2);
    private static readonly UmiejetnoscId C = new(3);
    private static readonly UmiejetnoscId InnyDzial = new(10);

    private static readonly IReadOnlyDictionary<DzialId, IReadOnlyList<UmiejetnoscId>> Dzialy = new Dictionary<DzialId, IReadOnlyList<UmiejetnoscId>>
    {
        [new DzialId(5)] = [A, B, C],
        [new DzialId(6)] = [InnyDzial],
    };

    private static ParametryEloZadania Parametry(UmiejetnoscId glowna, decimal b = 0m, decimal c = 0m, params UmiejetnoscId[] dodatkowe) =>
        new(glowna, dodatkowe, b, c);

    private static ProfilUcznia Profil() => ProfilUcznia.Zaloz(Uczen, Teraz);

    private static WynikZadania Wynik(decimal ulamek, DateTimeOffset kiedy, bool pierwsza = true, bool wstepna = false) =>
        wstepna
            ? WynikZadania.ZOceny(new WynikZadaniaId(Guid.NewGuid()), Uczen, new ZadanieId(Guid.NewGuid()), new ZadanieWersjaId(Guid.NewGuid()), Guid.NewGuid(), ulamek, true, false, pierwsza, kiedy)!
            : WynikZadania.ZOdpowiedzi(new WynikZadaniaId(Guid.NewGuid()), Uczen, new ZadanieId(Guid.NewGuid()), new ZadanieWersjaId(Guid.NewGuid()), Guid.NewGuid(), ulamek, pierwsza, kiedy);

    private static WynikZadania WynikTestu(decimal ulamek, TestDiagnostycznyId test, DateTimeOffset kiedy) =>
        WynikZadania.ZTestuDiagnostycznego(
            new WynikZadaniaId(Guid.NewGuid()), Uczen, new ZadanieId(Guid.NewGuid()), new ZadanieWersjaId(Guid.NewGuid()), Guid.NewGuid(), test, ulamek, kiedy);

    // ---------- stosowanie Wyniku ----------

    [Fact]
    public void DomyslneOpanowanieMaThetaZeroIPoziomZSigmy()
    {
        var profil = Profil();

        profil.InicjalizujBrakujace([A, B, A]).ShouldBe(2);
        profil.InicjalizujBrakujace([A]).ShouldBe(0);

        var opanowanie = profil.Znajdz(A)!;
        opanowanie.Theta.ShouldBe(0m);
        opanowanie.PoziomOpanowania.Wartosc.ShouldBe(0.310m);
        opanowanie.LiczbaWynikow.ShouldBe(0);
        opanowanie.Inicjalizacja.ShouldBe(InicjalizacjaOpanowania.Domyslna);
        opanowanie.OstatniWynikAt.ShouldBeNull();
    }

    [Fact]
    public void PoprawnyWynikPodnosiThetaUmiejetnosciGlownejOCzterySetne()
    {
        var profil = Profil();
        var wynik = Wynik(1m, Teraz);

        var zastosowanie = profil.Zastosuj(wynik, Parametry(A), Teraz.AddMinutes(1));

        var opanowanie = profil.Znajdz(A)!;
        opanowanie.Theta.ShouldBe(0.400m);
        opanowanie.PoziomOpanowania.Wartosc.ShouldBe(0.401m);
        opanowanie.LiczbaWynikow.ShouldBe(1);
        opanowanie.OstatniWynikAt.ShouldBe(Teraz);
        zastosowanie.ShouldNotBeNull();
        zastosowanie.Delta.ShouldBe(0.5m);
        zastosowanie.Dotkniete.ShouldBe([A]);
        wynik.ZastosowanyWProfiluAt.ShouldBe(Teraz.AddMinutes(1));
    }

    [Fact]
    public void BlednyWynikObnizaTheta()
    {
        var profil = Profil();

        profil.Zastosuj(Wynik(0m, Teraz), Parametry(A), Teraz);

        profil.Znajdz(A)!.Theta.ShouldBe(-0.400m);
    }

    [Fact]
    public void WynikZOcenyWstepnejMaPolowiczyWplyw()
    {
        var profil = Profil();

        profil.Zastosuj(Wynik(1m, Teraz, wstepna: true), Parametry(A), Teraz);

        profil.Znajdz(A)!.Theta.ShouldBe(0.200m);
    }

    [Fact]
    public void UmiejetnoscDodatkowaDostajePolowePrzyrostuGlownej()
    {
        var profil = Profil();

        var zastosowanie = profil.Zastosuj(Wynik(1m, Teraz), Parametry(A, 0m, 0m, B), Teraz);

        profil.Znajdz(A)!.Theta.ShouldBe(0.400m);
        profil.Znajdz(B)!.Theta.ShouldBe(0.200m);
        profil.Znajdz(B)!.LiczbaWynikow.ShouldBe(1);
        zastosowanie!.Dotkniete.ShouldBe([A, B]);
    }

    [Fact]
    public void WspolczynnikKMalejeWrazZLiczbaWynikow()
    {
        var profil = Profil();
        profil.Zastosuj(Wynik(1m, Teraz), Parametry(A, 5m), Teraz);
        var po1 = profil.Znajdz(A)!.Theta;
        var przyrost1 = po1;

        profil.Zastosuj(Wynik(1m, Teraz.AddMinutes(1)), Parametry(A, 5m), Teraz);
        var przyrost2 = profil.Znajdz(A)!.Theta - po1;

        przyrost2.ShouldBeLessThan(przyrost1);
    }

    [Fact]
    public void ZastosowanyAlboUniewaznionyWynikJestPomijany()
    {
        var profil = Profil();
        var wynik = Wynik(1m, Teraz);
        profil.Zastosuj(wynik, Parametry(A), Teraz).ShouldNotBeNull();

        profil.Zastosuj(wynik, Parametry(A), Teraz).ShouldBeNull();

        var uniewazniony = Wynik(1m, Teraz);
        uniewazniony.Uniewaznij(PowodUniewaznieniaWyniku.OcenaZastapiona, Teraz);
        profil.Zastosuj(uniewazniony, Parametry(A), Teraz).ShouldBeNull();
        profil.Znajdz(A)!.LiczbaWynikow.ShouldBe(1);
    }

    [Fact]
    public void WynikCudzegoUczniaJestOdrzucany()
    {
        var obcy = WynikZadania.ZOdpowiedzi(
            new WynikZadaniaId(Guid.NewGuid()), new UczenId(Guid.NewGuid()), new ZadanieId(Guid.NewGuid()), new ZadanieWersjaId(Guid.NewGuid()), Guid.NewGuid(), 1m, true, Teraz);

        Should.Throw<NiezmiennikNaruszonyException>(() => Profil().Zastosuj(obcy, Parametry(A), Teraz))
            .KodBledu.ShouldBe("WYNIK_CUDZEGO_UCZNIA");
    }

    // ---------- powtórki ----------

    [Fact]
    public void PoprawneWynikiPrzyWysokimOpanowaniuWydluzajaOdstepyPowtorek()
    {
        var profil = Profil();
        var odstepy = new List<(int Licznik, DateTimeOffset? Termin, DateTimeOffset Kiedy)>();

        // trudność b = 5: kolejne poprawne Wyniki podnoszą theta, aż poziom opanowania przekroczy 0,80.
        for (var i = 0; i < 40; i++)
        {
            var kiedy = Teraz.AddDays(i);
            profil.Zastosuj(Wynik(1m, kiedy), Parametry(A, 5m), kiedy);
            var o = profil.Znajdz(A)!;
            if (o.PoziomOpanowania.Wartosc >= 0.80m && o.LicznikPowtorek > 0)
            {
                odstepy.Add((o.LicznikPowtorek, o.NastepnaPowtorkaAt, kiedy));
            }
        }

        odstepy.Count.ShouldBeGreaterThan(2);
        odstepy[0].Licznik.ShouldBe(1);
        odstepy[0].Termin.ShouldBe(odstepy[0].Kiedy.AddDays(3));
        odstepy[1].Licznik.ShouldBe(2);
        odstepy[1].Termin.ShouldBe(odstepy[1].Kiedy.AddDays(7));
        odstepy[2].Termin.ShouldBe(odstepy[2].Kiedy.AddDays(14));
    }

    [Fact]
    public void SlabyWynikZerujeLicznikPowtorekIUstawiaPowtorkeNaJutro()
    {
        var profil = Profil();
        for (var i = 0; i < 30; i++)
        {
            profil.Zastosuj(Wynik(1m, Teraz.AddDays(i)), Parametry(A, 5m), Teraz);
        }

        profil.Znajdz(A)!.LicznikPowtorek.ShouldBeGreaterThan(0);

        var kiedy = Teraz.AddDays(40);
        profil.Zastosuj(Wynik(0.2m, kiedy), Parametry(A, 5m), kiedy);

        profil.Znajdz(A)!.LicznikPowtorek.ShouldBe(0);
        profil.Znajdz(A)!.NastepnaPowtorkaAt.ShouldBe(kiedy.AddDays(1));
    }

    // ---------- Test diagnostyczny ----------

    [Fact]
    public void TestDiagnostycznyStosujeWynikiZeWspolczynnikiemOsiemDziesiatychIPropagujeSredniaWDziale()
    {
        var profil = Profil();
        profil.InicjalizujBrakujace([A, B, C, InnyDzial]);
        var test = new TestDiagnostycznyId(Guid.NewGuid());
        var dobry = WynikTestu(1m, test, Teraz);
        var zly = WynikTestu(0m, test, Teraz);

        var zastosowania = profil.ZastosujTestDiagnostyczny(
            [(dobry, Parametry(A)), (zly, Parametry(B))], Dzialy, Teraz.AddMinutes(1));

        zastosowania.Count.ShouldBe(2);
        profil.Znajdz(A)!.Theta.ShouldBe(0.400m);
        profil.Znajdz(B)!.Theta.ShouldBe(-0.400m);
        profil.Znajdz(A)!.Inicjalizacja.ShouldBe(InicjalizacjaOpanowania.TestDiagnostyczny);
        profil.Znajdz(B)!.Inicjalizacja.ShouldBe(InicjalizacjaOpanowania.TestDiagnostyczny);
        profil.Znajdz(C)!.Theta.ShouldBe(0m);
        profil.Znajdz(C)!.Inicjalizacja.ShouldBe(InicjalizacjaOpanowania.PropagacjaZDzialu);
        profil.Znajdz(C)!.LiczbaWynikow.ShouldBe(0);
        profil.Znajdz(InnyDzial)!.Inicjalizacja.ShouldBe(InicjalizacjaOpanowania.Domyslna);
        dobry.ZastosowanyWProfiluAt.ShouldNotBeNull();
        zly.ZastosowanyWProfiluAt.ShouldNotBeNull();
    }

    [Fact]
    public void PropagacjaUstawiaSredniaThetaPrzetestowanychUmiejetnosciDzialu()
    {
        var profil = Profil();
        var test = new TestDiagnostycznyId(Guid.NewGuid());

        profil.ZastosujTestDiagnostyczny([(WynikTestu(1m, test, Teraz), Parametry(A))], Dzialy, Teraz);

        profil.Znajdz(B)!.Theta.ShouldBe(0.400m);
        profil.Znajdz(C)!.Theta.ShouldBe(0.400m);
        profil.Znajdz(B)!.PoziomOpanowania.Wartosc.ShouldBe(0.401m);
    }

    [Fact]
    public void PropagacjaNieNadpisujeUmiejetnosciZWczesniejszymiWynikami()
    {
        var profil = Profil();
        profil.Zastosuj(Wynik(0m, Teraz.AddDays(-1)), Parametry(C), Teraz);
        var test = new TestDiagnostycznyId(Guid.NewGuid());

        profil.ZastosujTestDiagnostyczny([(WynikTestu(1m, test, Teraz), Parametry(A))], Dzialy, Teraz);

        profil.Znajdz(C)!.Theta.ShouldBe(-0.400m);
        profil.Znajdz(C)!.Inicjalizacja.ShouldBe(InicjalizacjaOpanowania.Domyslna);
        profil.Znajdz(B)!.Inicjalizacja.ShouldBe(InicjalizacjaOpanowania.PropagacjaZDzialu);
    }

    [Fact]
    public void WynikSpozaTestuJestOdrzucanyWZastosowaniuTestu() =>
        Should.Throw<NiezmiennikNaruszonyException>(() =>
                Profil().ZastosujTestDiagnostyczny([(Wynik(1m, Teraz), Parametry(A))], Dzialy, Teraz))
            .KodBledu.ShouldBe("WYNIK_NIE_Z_TESTU");

    // ---------- odtwarzanie ----------

    [Fact]
    public void OdtworzenieOdTegoSamegoZbioruWynikowDajeIdentycznyStanJakStosowanieKolejne()
    {
        var wyniki = new List<(WynikZadania Wynik, ParametryEloZadania Parametry)>
        {
            (Wynik(1m, Teraz.AddDays(0)), Parametry(A, -1.6m)),
            (Wynik(0m, Teraz.AddDays(1)), Parametry(A, 0.8m, 0.25m, B)),
            (Wynik(0.5m, Teraz.AddDays(2), pierwsza: false), Parametry(B, 0.4m)),
            (Wynik(1m, Teraz.AddDays(3), wstepna: true), Parametry(A, 1.2m)),
        };
        var przyrostowy = Profil();
        foreach (var (wynik, parametry) in wyniki)
        {
            przyrostowy.Zastosuj(wynik, parametry, Teraz);
        }

        var odtworzony = Profil();
        odtworzony.InicjalizujBrakujace([A, B]);
        var kopie = wyniki.Select(p => (Wynik: Kopia(p.Wynik), p.Parametry)).Reverse().ToList();
        odtworzony.Odtworz(kopie, [], Dzialy, Teraz);

        Stan(odtworzony).ShouldBe(Stan(przyrostowy));
        Stan(odtworzony).ShouldBe(Stan(Ponownie(odtworzony, kopie)));
    }

    [Fact]
    public void OdtworzeniePomijaUniewaznionyWynikIOznaczaGoJakoPrzetworzony()
    {
        var profil = Profil();
        var dobry = Wynik(1m, Teraz);
        var zastapiony = Wynik(0m, Teraz.AddDays(1));
        profil.Zastosuj(dobry, Parametry(A), Teraz);
        profil.Zastosuj(zastapiony, Parametry(A), Teraz);
        zastapiony.Uniewaznij(PowodUniewaznieniaWyniku.OcenaZastapiona, Teraz.AddDays(2));

        profil.Odtworz([(dobry, Parametry(A))], [zastapiony], Dzialy, Teraz.AddDays(3));

        profil.Znajdz(A)!.Theta.ShouldBe(0.400m);
        profil.Znajdz(A)!.LiczbaWynikow.ShouldBe(1);
        zastapiony.UniewaznieniePrzetworzoneAt.ShouldBe(Teraz.AddDays(3));
    }

    [Fact]
    public void OdtworzenieResetujeUmiejetnoscBezWazneWynikow()
    {
        var profil = Profil();
        var wynik = WynikZadania.ZOceny(
            new WynikZadaniaId(Guid.NewGuid()), Uczen, new ZadanieId(Guid.NewGuid()), new ZadanieWersjaId(Guid.NewGuid()), Guid.NewGuid(), 1m, false, false, true, Teraz)!;
        profil.Zastosuj(wynik, Parametry(A), Teraz);
        wynik.Uniewaznij(PowodUniewaznieniaWyniku.ZgloszenieBleduOdczytu, Teraz);

        profil.Odtworz([], [wynik], Dzialy, Teraz);

        var opanowanie = profil.Znajdz(A)!;
        opanowanie.Theta.ShouldBe(0m);
        opanowanie.LiczbaWynikow.ShouldBe(0);
        opanowanie.OstatniWynikAt.ShouldBeNull();
        opanowanie.LicznikPowtorek.ShouldBe(0);
        opanowanie.NastepnaPowtorkaAt.ShouldBeNull();
        opanowanie.Inicjalizacja.ShouldBe(InicjalizacjaOpanowania.Domyslna);
    }

    [Fact]
    public void OdtworzenieStosujeWynikiTestuDiagnostycznegoIPropagujePoOstatnimZNich()
    {
        var test = new TestDiagnostycznyId(Guid.NewGuid());
        var wyniki = new List<(WynikZadania Wynik, ParametryEloZadania Parametry)>
        {
            (WynikTestu(1m, test, Teraz), Parametry(A)),
            (WynikTestu(0m, test, Teraz), Parametry(B)),
        };
        var profil = Profil();
        profil.ZastosujTestDiagnostyczny(wyniki, Dzialy, Teraz);
        var przed = Stan(profil);

        profil.Odtworz(wyniki.Select(p => (Kopia(p.Wynik), p.Parametry)), [], Dzialy, Teraz.AddDays(1));

        Stan(profil).ShouldBe(przed);
        profil.Znajdz(C)!.Inicjalizacja.ShouldBe(InicjalizacjaOpanowania.PropagacjaZDzialu);
    }

    // ---------- property ----------

    [Property]
    public bool ThetaZmieniaSieMonotonicznieZPoprawnoscia(sbyte bPomnozone, byte szansa)
    {
        var b = bPomnozone / 40m;
        var c = Math.Min(szansa / 255m, 0.9m);
        var dobry = Profil();
        var zly = Profil();

        dobry.Zastosuj(Wynik(1m, Teraz), Parametry(A, b, c), Teraz);
        zly.Zastosuj(Wynik(0m, Teraz), Parametry(A, b, c), Teraz);

        var thetaDobrego = dobry.Znajdz(A)!.Theta;
        var thetaZlego = zly.Znajdz(A)!.Theta;
        return thetaDobrego >= 0m && thetaZlego <= 0m && thetaDobrego >= thetaZlego;
    }

    // ---------- pomocnicy ----------

    private static WynikZadania Kopia(WynikZadania w) =>
        w.TestDiagnostycznyId is { } test
            ? WynikZadania.ZTestuDiagnostycznego(w.Id, w.UczenId, w.ZadanieId, w.ZadanieWersjaId, w.OdpowiedzId!.Value, test, w.Ulamek, w.CreatedAt)
            : w.Zrodlo == ZrodloWyniku.Odpowiedz
                ? WynikZadania.ZOdpowiedzi(w.Id, w.UczenId, w.ZadanieId, w.ZadanieWersjaId, w.OdpowiedzId!.Value, w.Ulamek, w.Waga == 1.00m, w.CreatedAt)
                : WynikZadania.ZOceny(w.Id, w.UczenId, w.ZadanieId, w.ZadanieWersjaId, w.OcenaId!.Value, w.Ulamek, w.Waga < 1.00m, false, true, w.CreatedAt)!;

    private static ProfilUcznia Ponownie(ProfilUcznia profil, List<(WynikZadania Wynik, ParametryEloZadania Parametry)> wyniki)
    {
        profil.Odtworz(wyniki, [], Dzialy, Teraz);
        return profil;
    }

    private static string Stan(ProfilUcznia profil) => string.Join(
        "|",
        profil.Opanowanie.OrderBy(o => o.UmiejetnoscId.Wartosc).Select(o =>
            $"{o.UmiejetnoscId.Wartosc}:{o.Theta}:{o.PoziomOpanowania.Wartosc}:{o.LiczbaWynikow}:{o.OstatniWynikAt:O}:{o.LicznikPowtorek}:{o.NastepnaPowtorkaAt:O}:{o.Inicjalizacja}"));
}
