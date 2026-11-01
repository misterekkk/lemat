// @Role: Testy Agregat<TId>: Version, kolekcja zdarzeń, kontrakt 3.6 | @DependsOn: Lemat.Domain.Common.Agregat | @UsedBy: dotnet test | @Task: TASK-014
using Lemat.Domain.Common;

namespace Lemat.Domain.Tests.Common;

public sealed class AggregateTests
{
    private readonly record struct PrzykladId(Guid Wartosc) : IIdentyfikator<Guid>;

    private sealed record PrzykladZdarzenie(DateTimeOffset Wystapilo) : IZdarzenieDomenowe;

    private sealed class PrzykladAgregat : Agregat<PrzykladId>
    {
        public PrzykladAgregat(PrzykladId id) => Id = id;

        public void Zmien(DateTimeOffset teraz) => Zglos(new PrzykladZdarzenie(teraz));

        public void ZglosNull() => Zglos(null!);
    }

    private static PrzykladAgregat Nowy() => new(new PrzykladId(GeneratorUuid.Nowy(TimeProvider.System)));

    [Fact]
    public void NowyAgregatMaWersje1IPusteZdarzenia()
    {
        var a = Nowy();

        a.Version.ShouldBe(1);
        a.Zdarzenia.ShouldBeEmpty();
    }

    [Fact]
    public void ZglosDodajeZdarzenieBezZmianyWersji()
    {
        var a = Nowy();
        var teraz = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

        a.Zmien(teraz);

        a.Zdarzenia.Count.ShouldBe(1);
        a.Zdarzenia[0].Wystapilo.ShouldBe(teraz);
        a.Version.ShouldBe(1);
    }

    [Fact]
    public void PotwierdzZapisZwiekszaWersjeOJeden()
    {
        var a = Nowy();

        a.PotwierdzZapis();
        a.PotwierdzZapis();

        a.Version.ShouldBe(3);
    }

    [Fact]
    public void WyczyscZdarzeniaCzysciKolekcjeAleNieWersje()
    {
        var a = Nowy();
        a.Zmien(DateTimeOffset.UnixEpoch);
        a.PotwierdzZapis();

        a.WyczyscZdarzenia();

        a.Zdarzenia.ShouldBeEmpty();
        a.Version.ShouldBe(2);
    }

    [Fact]
    public void ZglosNullRzucaWyjatek() => Should.Throw<ArgumentNullException>(() => Nowy().ZglosNull());
}
