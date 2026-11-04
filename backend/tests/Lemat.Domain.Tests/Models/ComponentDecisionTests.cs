// @Role: Testy Decyzji składowej i Decyzji modelu: rozkład (suma 1 ± 0,001), moda, niezmienność, zgodność z Pytaniem, CHECK stanu, zerowanie stanu, zapis błędu | @DependsOn: Lemat.Domain.Models.DecyzjaSkladowa, DecyzjaModelu | @UsedBy: dotnet test | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Domain.Tests.Models;

public sealed class ComponentDecisionTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly KonfiguracjaModeluId Konfiguracja = new(Guid.NewGuid());

    private static readonly byte[] Skrot = new byte[32];

    private static Dictionary<string, decimal> Rozklad(decimal tak = 0.995m, decimal nie = 0.005m) =>
        new() { ["tak"] = tak, ["nie"] = nie };

    [Fact]
    public void DecyzjaZRozkladuWybieraModeIJejMase()
    {
        var decyzja = DecyzjaSkladowa.ZRozkladu("Q-OC-01", TypPytania.Noul, Rozklad(0.2m, 0.8m));

        decyzja.Wybor.ShouldBe("nie");
        decyzja.PWyboru.ShouldBe(0.8m);
    }

    [Theory]
    [InlineData(0.5, 0.4)]
    [InlineData(0.6, 0.5)]
    [InlineData(-0.1, 1.1)]
    [InlineData(1.2, -0.2)]
    public void RozkladONieprawidlowejSumieLubWartosciachJestOdrzucany(double tak, double nie) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => DecyzjaSkladowa.ZRozkladu("Q", TypPytania.Noul, Rozklad((decimal)tak, (decimal)nie)))
            .KodBledu.ShouldBe("ROZKLAD_NIEPRAWIDLOWY");

    [Fact]
    public void RozkladZTolerancjaSumyJestPrzyjmowany()
    {
        DecyzjaSkladowa.ZRozkladu("Q", TypPytania.Noul, Rozklad(0.9995m, 0.0005m + 0.0009m)).PWyboru.ShouldBe(0.9995m);
        Should.Throw<NiezmiennikNaruszonyException>(() => DecyzjaSkladowa.ZRozkladu("Q", TypPytania.Noul, Rozklad(0.9995m, 0.0005m + 0.0011m)));
        Should.Throw<NiezmiennikNaruszonyException>(() => DecyzjaSkladowa.ZRozkladu("Q", TypPytania.Noul, new Dictionary<string, decimal>()));
    }

    private static DecyzjaModelu Zapisz(
        Pytanie? pytanie = null,
        DecyzjaSkladowa? decyzja = null,
        UczenId? uczen = null,
        KontekstDecyzji kontekst = KontekstDecyzji.Ocena,
        string? stan = null,
        byte[]? skrot = null)
    {
        pytanie ??= new PytanieNoul("Q-OC-01", "?");
        decyzja ??= DecyzjaSkladowa.ZRozkladu("Q-OC-01", TypPytania.Noul, Rozklad());
        return DecyzjaModelu.Zapisz(
            new DecyzjaModeluId(Guid.NewGuid()),
            Teraz,
            kontekst,
            Guid.NewGuid(),
            uczen ?? new UczenId(Guid.NewGuid()),
            Guid.NewGuid(),
            Konfiguracja,
            pytanie,
            decyzja,
            120,
            skrot ?? Skrot,
            stan);
    }

    [Fact]
    public void DecyzjaModeluPrzechowujeWyborRozkladOpcjeICzas()
    {
        var pytanie = new PytanieChoice("Q-OC-03", "?", ["BRAK_BLEDU", "KROK_1"]);
        var rozklad = new Dictionary<string, decimal> { ["BRAK_BLEDU"] = 0.9m, ["KROK_1"] = 0.1m };

        var decyzja = Zapisz(pytanie, DecyzjaSkladowa.ZRozkladu("Q-OC-03", TypPytania.Choice, rozklad));

        decyzja.KodPytania.ShouldBe("Q-OC-03");
        decyzja.TypPytania.ShouldBe(TypPytania.Choice);
        decyzja.Wybor.ShouldBe("BRAK_BLEDU");
        decyzja.Rozklad!["KROK_1"].ShouldBe(0.1m);
        decyzja.Opcje!.Lista.ShouldBe(["BRAK_BLEDU", "KROK_1"]);
        decyzja.CzasMs.ShouldBe(120);
        decyzja.CreatedAt.ShouldBe(Teraz);
        decyzja.Blad.ShouldBeNull();
    }

    [Fact]
    public void OpcjeSkaliToZakresANoulNieMaOpcji()
    {
        var skala = Zapisz(new PytanieScore("Q-OC-02.1.K1", "?", 0, 2), DecyzjaSkladowa.ZRozkladu("Q-OC-02.1.K1", TypPytania.Score, new Dictionary<string, decimal> { ["0"] = 0.1m, ["1"] = 0.1m, ["2"] = 0.8m }));

        skala.Opcje.ShouldBe(new OpcjePytania(null, 0, 2));
        Zapisz().Opcje.ShouldBeNull();
    }

    [Fact]
    public void DecyzjaNiezgodnaZPytaniemAlboSpozaRozkladuJestOdrzucana()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Zapisz(new PytanieNoul("INNE", "?"))).KodBledu.ShouldBe("DECYZJA_NIEZGODNA_Z_PYTANIEM");
        Should.Throw<NiezmiennikNaruszonyException>(() => Zapisz(decyzja: new DecyzjaSkladowa("Q-OC-01", TypPytania.Noul, "moze", Rozklad(), 1m)))
            .KodBledu.ShouldBe("ROZKLAD_NIEPRAWIDLOWY");
    }

    [Fact]
    public void SkrotStanuToSha256()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Zapisz(skrot: new byte[16])).KodBledu.ShouldBe("DECYZJA_NIEPRAWIDLOWA");
    }

    [Fact]
    public void PelnyStanBezUczniaJestDozwolonyTylkoDlaWalidacjiZadania()
    {
        var bezUcznia = () => DecyzjaModelu.Zapisz(
            new DecyzjaModeluId(Guid.NewGuid()),
            Teraz,
            KontekstDecyzji.Ocena,
            Guid.NewGuid(),
            null,
            null,
            Konfiguracja,
            new PytanieNoul("Q-OC-01", "?"),
            DecyzjaSkladowa.ZRozkladu("Q-OC-01", TypPytania.Noul, Rozklad()),
            1,
            Skrot,
            "stan");

        Should.Throw<NiezmiennikNaruszonyException>(bezUcznia).KodBledu.ShouldBe("DECYZJA_NIEPRAWIDLOWA");
        DecyzjaModelu.Zapisz(
            new DecyzjaModeluId(Guid.NewGuid()),
            Teraz,
            KontekstDecyzji.WalidacjaZadania,
            Guid.NewGuid(),
            null,
            null,
            Konfiguracja,
            new PytanieNoul("Q-OC-01", "?"),
            DecyzjaSkladowa.ZRozkladu("Q-OC-01", TypPytania.Noul, Rozklad()),
            1,
            Skrot,
            "stan").Stan.ShouldBe("stan");
    }

    [Fact]
    public void ZapisBleduNieMaWyboruIRozkladuAStanMoznaWyzerowac()
    {
        var blad = DecyzjaModelu.ZapiszBlad(
            new DecyzjaModeluId(Guid.NewGuid()),
            Teraz,
            KontekstDecyzji.Ocena,
            Guid.NewGuid(),
            new UczenId(Guid.NewGuid()),
            null,
            Konfiguracja,
            new PytanieNoul("Q-OC-01", "?"),
            "TIMEOUT",
            5000,
            Skrot,
            "pelny stan");

        blad.Wybor.ShouldBeNull();
        blad.Rozklad.ShouldBeNull();
        blad.Blad.ShouldBe("TIMEOUT");
        blad.WyzerujStan();
        blad.Stan.ShouldBeNull();
    }
}
