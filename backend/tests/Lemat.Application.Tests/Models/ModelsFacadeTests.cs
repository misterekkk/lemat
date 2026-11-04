// @Role: Testy jednostkowe ModeleFasada (atrapy repozytoriów): zapis i aktywacja Konfiguracji modelu (dezaktywacja poprzedniej, retencja A-07, Mock w Production), zapis i odczyt Decyzji modelu | @DependsOn: ModeleFasada, NSubstitute, Lemat.Domain.Models | @UsedBy: dotnet test | @Task: TASK-018
using Lemat.Application.Common;
using Lemat.Application.Models;
using Lemat.Application.Tests.Support;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Models;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Lemat.Application.Tests.Models;

public sealed class ModelsFacadeTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IKonfiguracjaModeluRepozytorium _konfiguracje = Substitute.For<IKonfiguracjaModeluRepozytorium>();
    private readonly IDecyzjaModeluRepozytorium _decyzje = Substitute.For<IDecyzjaModeluRepozytorium>();
    private readonly PrzezroczystaJednostkaPracy _jednostkaPracy = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ModeleFasada Fasada(string srodowisko = "Development") => new(
        _konfiguracje,
        _decyzje,
        _jednostkaPracy,
        Options.Create(new LematOpcje { Srodowisko = srodowisko }),
        new FakeTimeProvider(Teraz));

    private static NowaKonfiguracjaModelu Nowa(string dostawca = DostawcyModeli.Mock, int retencja = 0) =>
        new(RolaModelu.DecyzyjnyZapasowy, dostawca, "model-1", ParametryModelu.Domyslne with { RetencjaDostawcyDni = retencja }, "ocena-v1", null);

    private static KonfiguracjaModelu Zapisana(string dostawca = DostawcyModeli.Mock, int retencja = 0) =>
        KonfiguracjaModelu.Utworz(
            new KonfiguracjaModeluId(Guid.NewGuid()), RolaModelu.DecyzyjnyZapasowy, dostawca, "model-1", ParametryModelu.Domyslne with { RetencjaDostawcyDni = retencja }, "ocena-v1", null, Teraz);

    [Fact]
    public async Task ZapisKonfiguracjiDodajeNieaktywnaKonfiguracje()
    {
        var skrot = await Fasada().ZapiszKonfiguracjeAsync(Nowa(), Ct);

        skrot.Aktywna.ShouldBeFalse();
        skrot.Rola.ShouldBe(RolaModelu.DecyzyjnyZapasowy);
        _konfiguracje.Received(1).Dodaj(Arg.Is<KonfiguracjaModelu>(k => k.Id == skrot.Id));
    }

    [Fact]
    public async Task AktywacjaWylaczaPoprzedniaAktywnaTejSamejRoli()
    {
        var konfiguracja = Zapisana();
        _konfiguracje.PobierzAsync(konfiguracja.Id, Arg.Any<CancellationToken>()).Returns(konfiguracja);

        var skrot = await Fasada().AktywujAsync(konfiguracja.Id, Ct);

        skrot.Aktywna.ShouldBeTrue();
        await _konfiguracje.Received(1).DezaktywujAktywnaAsync(RolaModelu.DecyzyjnyZapasowy, konfiguracja.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AktywacjaMockaWProductionJestOdrzucanaBezDezaktywacjiPoprzedniej()
    {
        var konfiguracja = Zapisana();
        _konfiguracje.PobierzAsync(konfiguracja.Id, Arg.Any<CancellationToken>()).Returns(konfiguracja);

        var wyjatek = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada("Production").AktywujAsync(konfiguracja.Id, Ct));

        wyjatek.KodBledu.ShouldBe("DOSTAWCA_NIEDOZWOLONY");
        await _konfiguracje.DidNotReceive().DezaktywujAktywnaAsync(Arg.Any<RolaModelu>(), Arg.Any<KonfiguracjaModeluId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AktywacjaZRetencjaPonadA07JestOdrzucana()
    {
        var konfiguracja = Zapisana(DostawcyModeli.AzureOpenAi, retencja: 90);
        _konfiguracje.PobierzAsync(konfiguracja.Id, Arg.Any<CancellationToken>()).Returns(konfiguracja);

        var wyjatek = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada("Production").AktywujAsync(konfiguracja.Id, Ct));

        wyjatek.KodBledu.ShouldBe("BRAK_RETENCJI_DOSTAWCY");
    }

    [Fact]
    public async Task AktywacjaNieistniejacejKonfiguracjiDajeKodBledu()
    {
        _konfiguracje.PobierzAsync(Arg.Any<KonfiguracjaModeluId>(), Arg.Any<CancellationToken>()).Returns((KonfiguracjaModelu?)null);

        var wyjatek = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().AktywujAsync(new KonfiguracjaModeluId(Guid.NewGuid()), Ct));

        wyjatek.KodBledu.ShouldBe("KONFIGURACJA_MODELU_NIE_ZNALEZIONA");
    }

    [Fact]
    public async Task OdczytAktywnejKonfiguracjiMapujeNaSkrot()
    {
        var konfiguracja = Zapisana();
        konfiguracja.Aktywuj(srodowiskoProdukcyjne: false);
        _konfiguracje.PobierzAktywnaAsync(RolaModelu.DecyzyjnyZapasowy, Arg.Any<CancellationToken>()).Returns(konfiguracja);

        (await Fasada().PobierzAktywnaKonfiguracjeAsync(RolaModelu.DecyzyjnyZapasowy, Ct)).ShouldNotBeNull().Id.ShouldBe(konfiguracja.Id);
        (await Fasada().PobierzAktywnaKonfiguracjeAsync(RolaModelu.RozpoznawaniePisma, Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task ZapisDecyzjiPrzekazujeWszystkieWierszeDoRepozytorium()
    {
        var decyzje = new List<DecyzjaModelu>
        {
            DecyzjaModelu.ZapiszBlad(new DecyzjaModeluId(Guid.NewGuid()), Teraz, KontekstDecyzji.Ocena, Guid.NewGuid(), new UczenId(Guid.NewGuid()), null, new KonfiguracjaModeluId(Guid.NewGuid()), new PytanieNoul("Q-1", "?"), "TIMEOUT", 10, new byte[32], null),
            DecyzjaModelu.ZapiszBlad(new DecyzjaModeluId(Guid.NewGuid()), Teraz, KontekstDecyzji.Ocena, Guid.NewGuid(), new UczenId(Guid.NewGuid()), null, new KonfiguracjaModeluId(Guid.NewGuid()), new PytanieNoul("Q-2", "?"), "TIMEOUT", 10, new byte[32], null),
        };

        await Fasada().ZapiszDecyzjeAsync(decyzje, Ct);

        _decyzje.Received(1).DodajRange(Arg.Is<IEnumerable<DecyzjaModelu>>(d => d.Count() == 2));
    }
}

internal sealed class FakeTimeProvider(DateTimeOffset teraz) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => teraz;
}
