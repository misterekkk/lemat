// @Role: Testy agregatu Uczen: oświadczenie 16+ (A-06), rola konta, Grupa eksperymentu z identyfikatora konta, poziom i termin egzaminu | @DependsOn: Lemat.Domain.Accounts.Uczen | @UsedBy: dotnet test | @Task: TASK-016
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Tests.Accounts;

public sealed class UczenTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static Uzytkownik Konto(RolaUzytkownika rola = RolaUzytkownika.Uczen) =>
        Uzytkownik.Zaloz(new UzytkownikId(GeneratorUuid.Nowy(TimeProvider.System)), rola, "u@example.com", "Ewa", "Nowak", Teraz);

    [Fact]
    public void TworzenieUczniaWymagaOswiadczenia16Plus() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Uczen.Utworz(Konto(), oswiadczenieWieku16: false))
            .KodBledu.ShouldBe("OSWIADCZENIE_WIEKU_WYMAGANE");

    [Theory]
    [InlineData(RolaUzytkownika.Nauczyciel)]
    [InlineData(RolaUzytkownika.Administrator)]
    public void UczenWymagaKontaORoliUczen(RolaUzytkownika rola) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Uczen.Utworz(Konto(rola), true)).KodBledu.ShouldBe("KONTO_NIEPRAWIDLOWA_ROLA");

    [Fact]
    public void UczenDziedziczyIdentyfikatorKontaIMaGrupeEksperymentu()
    {
        var konto = Konto();

        var uczen = Uczen.Utworz(konto, true);

        uczen.Id.Wartosc.ShouldBe(konto.Id.Wartosc);
        uczen.OswiadczenieWieku16.ShouldBeTrue();
        uczen.GrupaEksperymentu.ShouldBe(GrupaEksperymentu.Dla(konto.Id));
        uczen.PoziomMatury.ShouldBeNull();
        uczen.TerminEgzaminu.ShouldBeNull();
        uczen.WybranyDzialId.ShouldBeNull();
        uczen.ZgodaNaTrening.ShouldBeFalse();
        uczen.Version.ShouldBe(1);
    }

    [Fact]
    public void TerminEgzaminuNieMozeBycWPrzeszlosci()
    {
        var uczen = Uczen.Utworz(Konto(), true);
        var dzis = new DateOnly(2026, 11, 1);

        Should.Throw<NiezmiennikNaruszonyException>(() => uczen.UstawPoziomITermin(PoziomMatury.Rozszerzony, dzis.AddDays(-1), dzis))
            .KodBledu.ShouldBe("TERMIN_EGZAMINU_W_PRZESZLOSCI");

        uczen.UstawPoziomITermin(PoziomMatury.Rozszerzony, dzis, dzis);
        uczen.PoziomMatury.ShouldBe(PoziomMatury.Rozszerzony);
        uczen.TerminEgzaminu.ShouldBe(dzis);
    }

    [Fact]
    public void WyborDzialuIZgodaNaTrening()
    {
        var uczen = Uczen.Utworz(Konto(), true);

        uczen.WybierzDzial(new DzialId(3));
        uczen.UstawZgodeNaTrening(true);

        uczen.WybranyDzialId.ShouldBe(new DzialId(3));
        uczen.ZgodaNaTrening.ShouldBeTrue();
        uczen.WybierzDzial(null);
        uczen.WybranyDzialId.ShouldBeNull();
    }
}
