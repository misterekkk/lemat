// @Role: Testy agregatu Test diagnostyczny: tworzenie z pozycjami numerowanymi od 1, Działy pominięte, zakończenie i pominięcie tylko z W_TOKU | @DependsOn: Lemat.Domain.Profile.TestDiagnostyczny | @UsedBy: dotnet test | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;

namespace Lemat.Domain.Tests.Profile;

public sealed class DiagnosticTestTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static TestDiagnostyczny Nowy(int pozycje = 3) =>
        TestDiagnostyczny.Rozpocznij(
            new TestDiagnostycznyId(Guid.NewGuid()),
            new UczenId(Guid.NewGuid()),
            PoziomMatury.Podstawowy,
            [.. Enumerable.Range(0, pozycje).Select(_ => new ZadanieWersjaId(Guid.NewGuid()))],
            [new DzialId(7), new DzialId(7), new DzialId(9)],
            Teraz);

    [Fact]
    public void NowyTestJestWTokuZPozycjamiOdJedynki()
    {
        var test = Nowy(4);

        test.Status.ShouldBe(StatusTestu.WToku);
        test.ZakonczonoAt.ShouldBeNull();
        test.Pozycje.Select(p => p.Kolejnosc).ShouldBe([1, 2, 3, 4]);
        test.PominieteDzialy.ShouldBe([7, 9]);
        test.CreatedAt.ShouldBe(Teraz);
    }

    [Fact]
    public void TestBezPozycjiJestNiedostepny() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowy(0)).KodBledu.ShouldBe("TEST_NIEDOSTEPNY");

    [Fact]
    public void ZakonczenieUstawiaStatusICzas()
    {
        var test = Nowy();

        test.Zakoncz(Teraz.AddMinutes(30));

        test.Status.ShouldBe(StatusTestu.Zakonczony);
        test.ZakonczonoAt.ShouldBe(Teraz.AddMinutes(30));
    }

    [Fact]
    public void PominiecieUstawiaStatusICzas()
    {
        var test = Nowy();

        test.Pomin(Teraz.AddMinutes(1));

        test.Status.ShouldBe(StatusTestu.Pominiety);
        test.ZakonczonoAt.ShouldBe(Teraz.AddMinutes(1));
    }

    [Fact]
    public void ZamknietegoTestuNieMoznaZamknacPonownie()
    {
        var zakonczony = Nowy();
        zakonczony.Zakoncz(Teraz);
        var pominiety = Nowy();
        pominiety.Pomin(Teraz);

        Should.Throw<NiezmiennikNaruszonyException>(() => zakonczony.Pomin(Teraz)).KodBledu.ShouldBe("TEST_NIEPRAWIDLOWY_STAN");
        Should.Throw<NiezmiennikNaruszonyException>(() => zakonczony.Zakoncz(Teraz)).KodBledu.ShouldBe("TEST_NIEPRAWIDLOWY_STAN");
        Should.Throw<NiezmiennikNaruszonyException>(() => pominiety.Zakoncz(Teraz)).KodBledu.ShouldBe("TEST_NIEPRAWIDLOWY_STAN");
    }
}
