// @Role: Testy wspólnych typów domeny: PoziomTrudnosci, wyjątki, wyliczenia, generator UUIDv7 | @DependsOn: Lemat.Domain.Common | @UsedBy: dotnet test | @Task: TASK-014
using Lemat.Domain.Common;

namespace Lemat.Domain.Tests.Common;

public sealed class CommonTypesTests
{
    private sealed class StalyCzas(DateTimeOffset teraz) : TimeProvider
    {
        public DateTimeOffset Teraz { get; set; } = teraz;

        public override DateTimeOffset GetUtcNow() => Teraz;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void PoziomTrudnosciPrzyjmujeWartosci1Do5(int wartosc) => new PoziomTrudnosci(wartosc).Wartosc.ShouldBe(wartosc);

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void PoziomTrudnosciOdrzucaWartosciPozaZakresem(int wartosc)
    {
        var blad = Should.Throw<NiezmiennikNaruszonyException>(() => new PoziomTrudnosci(wartosc));

        blad.KodBledu.ShouldBe("POZIOM_TRUDNOSCI_POZA_ZAKRESEM");
    }

    [Fact]
    public void PoziomTrudnosciMaturalnyToCztery() => PoziomTrudnosci.Maturalny.Wartosc.ShouldBe(4);

    [Fact]
    public void NiezmiennikNaruszonyZachowujeKodIOpis()
    {
        var blad = new NiezmiennikNaruszonyException("ROZWIAZANIE_NIEPRAWIDLOWY_STAN", "Zły stan.");

        blad.KodBledu.ShouldBe("ROZWIAZANIE_NIEPRAWIDLOWY_STAN");
        blad.Message.ShouldBe("Zły stan.");
        blad.ShouldBeAssignableTo<WyjatekDomenowy>();
    }

    [Fact]
    public void NiezmiennikNaruszonyBezOpisuUzywaKoduJakoKomunikatu() =>
        new NiezmiennikNaruszonyException("TEST_KOD").Message.ShouldBe("TEST_KOD");

    [Fact]
    public void NiezmiennikNaruszonyOdrzucaPustyKod() =>
        Should.Throw<ArgumentException>(() => new NiezmiennikNaruszonyException(" "));

    [Fact]
    public void WyliczeniaMajaCzlonkiRownieZ3_6()
    {
        Enum.GetNames<PoziomMatury>().ShouldBe(["Podstawowy", "Rozszerzony"]);
        Enum.GetNames<RodzajZadania>().ShouldBe(["Zamkniete", "OtwarteObliczeniowe", "OtwarteZDowodem"]);
    }

    [Fact]
    public void GeneratorDajeUuidV7UporzadkowanyCzasowo()
    {
        var czas = new StalyCzas(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
        var pierwszy = GeneratorUuid.Nowy(czas);
        czas.Teraz = czas.Teraz.AddSeconds(1);
        var drugi = GeneratorUuid.Nowy(czas);

        pierwszy.Version.ShouldBe(7);
        drugi.Version.ShouldBe(7);
        pierwszy.ToByteArray(bigEndian: true).AsSpan().SequenceCompareTo(drugi.ToByteArray(bigEndian: true)).ShouldBeLessThan(0);
    }
}
