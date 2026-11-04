// @Role: Testy agregatu Konfiguracja modelu: walidacja dostawcy i Parametrów modelu, zależności parametrów od roli i dostawcy, aktywacja z kontrolą retencji (A-07) i zakazem Mocka w Production | @DependsOn: Lemat.Domain.Models.KonfiguracjaModelu, ParametryModelu | @UsedBy: dotnet test | @Task: TASK-018
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Domain.Tests.Models;

public sealed class ModelConfigurationTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static KonfiguracjaModelu Nowa(
        RolaModelu rola = RolaModelu.DecyzyjnyZapasowy,
        string dostawca = DostawcyModeli.Mock,
        ParametryModelu? parametry = null) =>
        KonfiguracjaModelu.Utworz(new KonfiguracjaModeluId(Guid.NewGuid()), rola, dostawca, "mock-1", parametry ?? ParametryModelu.Domyslne, "ocena-v1", null, Teraz);

    [Fact]
    public void NowaKonfiguracjaJestNieaktywnaIZachowujeDane()
    {
        var konfiguracja = Nowa();

        konfiguracja.Aktywna.ShouldBeFalse();
        konfiguracja.Rola.ShouldBe(RolaModelu.DecyzyjnyZapasowy);
        konfiguracja.Dostawca.ShouldBe("mock");
        konfiguracja.WersjaPromptu.ShouldBe("ocena-v1");
        konfiguracja.JestRoleDecyzyjna.ShouldBeTrue();
        Nowa(RolaModelu.GeneratywnyWskazowki).JestRoleDecyzyjna.ShouldBeFalse();
    }

    [Fact]
    public void NieznanyDostawcaIPustyModelSaOdrzucane()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(dostawca: "nieznany")).KodBledu.ShouldBe("DOSTAWCA_NIEZNANY");
        Should.Throw<NiezmiennikNaruszonyException>(() => KonfiguracjaModelu.Utworz(
            new KonfiguracjaModeluId(Guid.NewGuid()), RolaModelu.DecyzyjnyGlowny, "mock", " ", ParametryModelu.Domyslne, "p", null, Teraz))
            .KodBledu.ShouldBe("KONFIGURACJA_MODELU_NIEPRAWIDLOWA");
    }

    [Theory]
    [MemberData(nameof(NieprawidloweParametry))]
    public void NieprawidloweParametrySaOdrzucane(ParametryModelu parametry) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(parametry: parametry)).KodBledu.ShouldBe("PARAMETRY_MODELU_NIEPRAWIDLOWE");

    public static TheoryData<ParametryModelu> NieprawidloweParametry() =>
    [
        ParametryModelu.Domyslne with { TimeoutS = 0m },
        ParametryModelu.Domyslne with { MaxTokens = 0 },
        ParametryModelu.Domyslne with { Temperatura = 2.1m },
        ParametryModelu.Domyslne with { CenaWejsciePln1k = -0.01m },
        ParametryModelu.Domyslne with { CenaWyjsciePln1k = -0.01m },
        ParametryModelu.Domyslne with { RetencjaDostawcyDni = -1 },
        ParametryModelu.Domyslne with { MaksIteracjiFunkcji = 9 },
    ];

    [Fact]
    public void ParametryZalezneOdDostawcyIRoliSaWalidowane()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(parametry: ParametryModelu.Domyslne with { TrybJev = TrybJev.Wsadowy }));
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(parametry: ParametryModelu.Domyslne with { DostawcaLlm = "mock" }));
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa(parametry: ParametryModelu.Domyslne with { MaksIteracjiFunkcji = 4 }));

        Nowa(RolaModelu.DecyzyjnyGlowny, DostawcyModeli.TypesafeJev, ParametryModelu.Domyslne with { TrybJev = TrybJev.Wsadowy }).Parametry.TrybJev.ShouldBe(TrybJev.Wsadowy);
        Nowa(RolaModelu.DecyzyjnyZapasowy, DostawcyModeli.ZapasowyLlm, ParametryModelu.Domyslne with { DostawcaLlm = "azure-openai" }).Parametry.DostawcaLlm.ShouldBe("azure-openai");
        Nowa(RolaModelu.GeneratywnyZadania, parametry: ParametryModelu.Domyslne with { MaksIteracjiFunkcji = 8 }).Parametry.MaksIteracjiFunkcji.ShouldBe(8);
    }

    [Fact]
    public void AktywacjaUstawiaFlageADezaktywacjaJaZdejmuje()
    {
        var konfiguracja = Nowa();

        konfiguracja.Aktywuj(srodowiskoProdukcyjne: false);
        konfiguracja.Aktywna.ShouldBeTrue();
        konfiguracja.Dezaktywuj();
        konfiguracja.Aktywna.ShouldBeFalse();
    }

    [Fact]
    public void MockWProductionNieMozeBycAktywowany() =>
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowa().Aktywuj(srodowiskoProdukcyjne: true)).KodBledu.ShouldBe("DOSTAWCA_NIEDOZWOLONY");

    [Theory]
    [InlineData(RolaModelu.RozpoznawaniePisma, 0, true)]
    [InlineData(RolaModelu.RozpoznawaniePisma, 1, false)]
    [InlineData(RolaModelu.DecyzyjnyGlowny, 30, true)]
    [InlineData(RolaModelu.DecyzyjnyGlowny, 31, false)]
    [InlineData(RolaModelu.GeneratywnyWskazowki, 7, true)]
    public void AktywacjaSprawdzaRetencjeDostawcyZgodnieZA07(RolaModelu rola, int retencjaDni, bool dozwolona)
    {
        var konfiguracja = Nowa(rola, DostawcyModeli.AzureOpenAi, ParametryModelu.Domyslne with { RetencjaDostawcyDni = retencjaDni });

        if (dozwolona)
        {
            konfiguracja.Aktywuj(srodowiskoProdukcyjne: true);
            konfiguracja.Aktywna.ShouldBeTrue();
        }
        else
        {
            Should.Throw<NiezmiennikNaruszonyException>(() => konfiguracja.Aktywuj(srodowiskoProdukcyjne: true)).KodBledu.ShouldBe("BRAK_RETENCJI_DOSTAWCY");
            konfiguracja.Aktywna.ShouldBeFalse();
        }
    }
}
