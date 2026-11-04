// @Role: Testy agregatu Klasa: Kod zaproszenia (generowanie, format, ważność, unieważnianie, odnowienie), unikalność aktywnego członkostwa, opuszczanie, archiwizacja, Przekazanie Klasy | @DependsOn: Lemat.Domain.Classes.Klasa, KodZaproszenia, GeneratorKoduZaproszenia | @UsedBy: dotnet test | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Classes;
using Lemat.Domain.Common;

namespace Lemat.Domain.Tests.Classes;

public sealed class ClassTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly NauczycielId Prowadzacy = new(Guid.NewGuid());

    private static Klasa Nowa(string nazwa = "3A matematyka") =>
        Klasa.Zaloz(new KlasaId(Guid.NewGuid()), Prowadzacy, nazwa, PoziomMatury.Rozszerzony, GeneratorKoduZaproszenia.Nowy(Teraz), Teraz);

    private static UczenId NowyUczen() => new(Guid.NewGuid());

    // ---------- Kod zaproszenia ----------

    [Fact]
    public void WygenerowanyKodMaOsiemZnakowZAlfabetuBezMylacychSieZnakow()
    {
        for (var i = 0; i < 500; i++)
        {
            var kod = GeneratorKoduZaproszenia.Nowy(Teraz);

            kod.Wartosc.Length.ShouldBe(8);
            kod.Wartosc.ShouldAllBe(znak => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Contains(znak, StringComparison.Ordinal));
            kod.Wartosc.ShouldNotContain("I");
            kod.Wartosc.ShouldNotContain("O");
            kod.Wartosc.ShouldNotContain("0");
            kod.Wartosc.ShouldNotContain("1");
            kod.WaznyDo.ShouldBe(Teraz.AddDays(14));
        }
    }

    [Fact]
    public void KolejneKodySaRozne()
    {
        var kody = Enumerable.Range(0, 200).Select(_ => GeneratorKoduZaproszenia.Nowy(Teraz).Wartosc).ToHashSet();

        kody.Count.ShouldBe(200);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCDEFG0")]
    [InlineData("ABCDEFGI")]
    [InlineData("")]
    public void KodONieprawidlowejPostaciJestOdrzucany(string wartosc) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => new KodZaproszenia(wartosc, Teraz.AddDays(1)))
            .KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEPRAWIDLOWY");

    [Fact]
    public void KodJestWaznyDoTerminuIRozpoznajeMaleLiteryISpacje()
    {
        var kod = new KodZaproszenia("ABCD2345", Teraz.AddDays(14));

        kod.CzyWazny("abcd2345 ", Teraz).ShouldBeTrue();
        kod.CzyWazny("ABCD2345", Teraz.AddDays(14).AddTicks(-1)).ShouldBeTrue();
        kod.CzyWazny("ABCD2345", Teraz.AddDays(14)).ShouldBeFalse();
        kod.CzyWazny("ABCD2346", Teraz).ShouldBeFalse();
        kod.CzyWazny(null, Teraz).ShouldBeFalse();
    }

    // ---------- zakładanie ----------

    [Fact]
    public void ZalozonaKlasaMaProwadzacegoNazweKodIWersjeJeden()
    {
        var klasa = Nowa("  3A  ");

        klasa.NauczycielId.ShouldBe(Prowadzacy);
        klasa.Nazwa.ShouldBe("3A");
        klasa.PoziomMatury.ShouldBe(PoziomMatury.Rozszerzony);
        klasa.Zarchiwizowana.ShouldBeFalse();
        klasa.Czlonkowie.ShouldBeEmpty();
        klasa.KodZaproszenia.Wartosc.Length.ShouldBe(8);
        klasa.Version.ShouldBe(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PustaNazwaJestOdrzucana(string nazwa) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(nazwa)).KodBledu.ShouldBe("KLASA_NAZWA_NIEPRAWIDLOWA");

    [Fact]
    public void NazwaDluzszaNizOsiemdziesiatZnakowJestOdrzucana()
    {
        Nowa(new string('a', 80)).Nazwa.Length.ShouldBe(80);
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(new string('a', 81))).KodBledu.ShouldBe("KLASA_NAZWA_NIEPRAWIDLOWA");
    }

    // ---------- dołączanie, unikalność członkostwa, opuszczanie ----------

    [Fact]
    public void UczenDolaczaPoprawnymKodem()
    {
        var klasa = Nowa();
        var uczen = NowyUczen();

        klasa.Dolacz(uczen, klasa.KodZaproszenia.Wartosc.ToLowerInvariant(), Teraz.AddDays(1)).ShouldBeTrue();

        klasa.CzyCzlonek(uczen).ShouldBeTrue();
        klasa.Czlonkowie.ShouldHaveSingleItem().DolaczonoAt.ShouldBe(Teraz.AddDays(1));
    }

    [Fact]
    public void DrugieDolaczenieTegoSamegoUczniaJestIdempotentneIBezDuplikatu()
    {
        var klasa = Nowa();
        var uczen = NowyUczen();
        klasa.Dolacz(uczen, klasa.KodZaproszenia.Wartosc, Teraz);

        klasa.Dolacz(uczen, klasa.KodZaproszenia.Wartosc, Teraz.AddHours(1)).ShouldBeFalse();

        klasa.Czlonkowie.Count.ShouldBe(1);
        klasa.Czlonkowie[0].DolaczonoAt.ShouldBe(Teraz);
    }

    [Fact]
    public void BledneKodyIWygasleKodyNieDajaCzlonkostwa()
    {
        var klasa = Nowa();
        var uczen = NowyUczen();

        Should.Throw<NiezmiennikNaruszonyException>(() => klasa.Dolacz(uczen, "ZLYKOD22", Teraz)).KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEWAZNY");
        Should.Throw<NiezmiennikNaruszonyException>(() => klasa.Dolacz(uczen, klasa.KodZaproszenia.Wartosc, Teraz.AddDays(15)))
            .KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEWAZNY");
        klasa.Czlonkowie.ShouldBeEmpty();
    }

    [Fact]
    public void UczenOpuszczaKlaseAUczenObcyNie()
    {
        var klasa = Nowa();
        var uczen = NowyUczen();
        klasa.Dolacz(uczen, klasa.KodZaproszenia.Wartosc, Teraz);

        klasa.Opusc(NowyUczen()).ShouldBeFalse();
        klasa.Opusc(uczen).ShouldBeTrue();

        klasa.CzyCzlonek(uczen).ShouldBeFalse();
    }

    // ---------- unieważnianie i odnawianie kodu ----------

    [Fact]
    public void UniewaznionyKodNieDajeDolaczeniaAleOdnowionyTak()
    {
        var klasa = Nowa();
        var stary = klasa.KodZaproszenia.Wartosc;

        klasa.UniewaznKod(Teraz.AddHours(1));
        Should.Throw<NiezmiennikNaruszonyException>(() => klasa.Dolacz(NowyUczen(), stary, Teraz.AddHours(1)))
            .KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEWAZNY");

        var nowy = GeneratorKoduZaproszenia.Nowy(Teraz.AddHours(2));
        klasa.OdnowKod(nowy);
        klasa.Dolacz(NowyUczen(), nowy.Wartosc, Teraz.AddHours(2)).ShouldBeTrue();
        klasa.KodZaproszenia.ShouldBe(nowy);
    }

    [Fact]
    public void OdnowienieKoduUniewaznaStaryKod()
    {
        var klasa = Nowa();
        var stary = klasa.KodZaproszenia.Wartosc;

        klasa.OdnowKod(new KodZaproszenia("ZZZZ9999", Teraz.AddDays(14)));

        Should.Throw<NiezmiennikNaruszonyException>(() => klasa.Dolacz(NowyUczen(), stary, Teraz)).KodBledu.ShouldBe("KOD_ZAPROSZENIA_NIEWAZNY");
    }

    // ---------- archiwizacja i Przekazanie ----------

    [Fact]
    public void ZarchiwizowanaKlasaOdrzucaDolaczenieOdnowienieIPrzekazanie()
    {
        var klasa = Nowa();
        klasa.Archiwizuj(Teraz);

        klasa.Zarchiwizowana.ShouldBeTrue();
        klasa.ZarchiwizowanoAt.ShouldBe(Teraz);
        Should.Throw<NiezmiennikNaruszonyException>(() => klasa.Dolacz(NowyUczen(), klasa.KodZaproszenia.Wartosc, Teraz))
            .KodBledu.ShouldBe("KLASA_ZARCHIWIZOWANA");
        Should.Throw<NiezmiennikNaruszonyException>(() => klasa.OdnowKod(GeneratorKoduZaproszenia.Nowy(Teraz))).KodBledu.ShouldBe("KLASA_ZARCHIWIZOWANA");
        Should.Throw<NiezmiennikNaruszonyException>(() => klasa.Przekaz(new NauczycielId(Guid.NewGuid()), Teraz)).KodBledu.ShouldBe("KLASA_ZARCHIWIZOWANA");
    }

    [Fact]
    public void PonowneArchiwizowanieNieZmieniaZnacznika()
    {
        var klasa = Nowa();
        klasa.Archiwizuj(Teraz);

        klasa.Archiwizuj(Teraz.AddDays(5));

        klasa.ZarchiwizowanoAt.ShouldBe(Teraz);
    }

    [Fact]
    public void PrzekazanieZmieniaProwadzacegoIZglaszaZdarzenie()
    {
        var klasa = Nowa();
        var nowy = new NauczycielId(Guid.NewGuid());

        klasa.Przekaz(nowy, Teraz.AddDays(2));

        klasa.NauczycielId.ShouldBe(nowy);
        var zdarzenie = klasa.Zdarzenia.ShouldHaveSingleItem().ShouldBeOfType<KlasaPrzekazana>();
        zdarzenie.KlasaId.ShouldBe(klasa.Id);
        zdarzenie.Poprzedni.ShouldBe(Prowadzacy);
        zdarzenie.Nowy.ShouldBe(nowy);
        zdarzenie.Wystapilo.ShouldBe(Teraz.AddDays(2));
    }

    [Fact]
    public void UsuniecieKontaNauczycielaArchiwizujeKlaseBezProwadzacego()
    {
        var klasa = Nowa();

        klasa.ArchiwizujPoUsunieciuNauczyciela(Teraz);

        klasa.NauczycielId.ShouldBeNull();
        klasa.Zarchiwizowana.ShouldBeTrue();
    }
}
