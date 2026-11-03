// @Role: Handler pracy ROZPOZNAJ_PISMO (9.4, U-26): PRZESLANE→ROZPOZNAWANIE (zapis), opóźnienie i wywołanie Agenta rozpoznającego POZA transakcją, zapis Transkrypcji wersja 1 → TRANSKRYPCJA_GOTOWA albo BLAD_ROZPOZNAWANIA; idempotentny, sprawdza warunek wykonania | @DependsOn: IRozwiazanieRepozytorium, ITranskrypcjaRepozytorium, IAgentRozpoznajacy, IModeleFasada, IJednostkaPracy, TimeProvider, LematOpcje | @UsedBy: ProcesorPrac (przez IHandlerPracy), DI (SolutionsRegistration) | @Task: TASK-128
using System.Text.Json;
using Lemat.Application.Common;
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Application.Models;
using Lemat.Domain.Common;
using Lemat.Domain.Models;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Solutions;

/// <summary>Ładunek pracy ROZPOZNAJ_PISMO (klucze snake_case): <c>rozwiazanie_id</c>, <c>przeslanie_nr</c>, <c>ponowienie_nr</c>, <c>mock_scenariusz</c>.</summary>
public sealed record LadunekRozpoznajPismo(Guid RozwiazanieId, int PrzeslanieNr, int PonowienieNr, string? MockScenariusz);

public sealed class RozpoznajPismoHandler(
    IRozwiazanieRepozytorium rozwiazania,
    ITranskrypcjaRepozytorium transkrypcje,
    IAgentRozpoznajacy agent,
    IModeleFasada modele,
    IJednostkaPracy jednostkaPracy,
    TimeProvider czas,
    LematOpcje opcje) : IHandlerPracy
{
    public const string ScenariuszDomyslny = "domyslny";

    public TypPracy Typ => TypPracy.RozpoznajPismo;

    public async Task WykonajAsync(PracaWTle praca, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(praca);
        var ladunek = JsonSerializer.Deserialize<LadunekRozpoznajPismo>(praca.Ladunek, LadunekJson.Opcje)
            ?? throw new InvalidOperationException("Pusty ładunek pracy ROZPOZNAJ_PISMO.");
        var id = new RozwiazanieId(ladunek.RozwiazanieId);

        // Warunek wykonania (13.3): Rozwiązanie istnieje i czeka na rozpoznanie albo jest w jego trakcie (ponowienie).
        var obecne = await rozwiazania.PobierzDoOdczytuAsync(id, ct);
        if (obecne is null || obecne.Status is not (StatusRozwiazania.Przeslane or StatusRozwiazania.Rozpoznawanie))
        {
            return;
        }

        if (obecne.Status == StatusRozwiazania.Przeslane)
        {
            await jednostkaPracy.WykonajAsync(
                async token =>
                {
                    var r = await rozwiazania.PobierzAsync(id, token);
                    if (r is { Status: StatusRozwiazania.Przeslane })
                    {
                        r.RozpocznijRozpoznawanie();
                    }
                },
                ct);
        }

        // Opóźnienie i „wywołanie modelu” poza transakcją (ADR-056); czas z TimeProvider.
        await Task.Delay(TimeSpan.FromMilliseconds(opcje.Poc.OpoznienieRozpoznaniaMs), czas, ct);
        var wynik = await agent.RozpoznajAsync(obecne.ZadanieId, ladunek.MockScenariusz ?? ScenariuszDomyslny, ct);

        // Z-3: bez aktywnej konfiguracji roli nie zapisujemy Transkrypcji „z niczego” — praca zostanie ponowiona.
        var konfiguracja = wynik.Blad is null
            ? (await modele.PobierzAktywnaKonfiguracjeAsync(RolaModelu.RozpoznawaniePisma, ct))?.Id
                ?? throw new InvalidOperationException("Brak aktywnej konfiguracji modelu dla roli ROZPOZNAWANIE_PISMA.")
            : (KonfiguracjaModeluId?)null;

        await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var r = await rozwiazania.PobierzAsync(id, token);
                if (r is not { Status: StatusRozwiazania.Rozpoznawanie })
                {
                    return;
                }

                var teraz = czas.GetUtcNow();
                if (wynik.Blad is { } kod)
                {
                    r.ZapiszBladRozpoznawania(kod);
                    return;
                }

                var istniejaca = await transkrypcje.PobierzNajnowszaAsync(id, token);
                if (istniejaca is null)
                {
                    var nowa = Transkrypcja.ZAgenta(
                        new TranskrypcjaId(GeneratorUuid.Nowy(czas)),
                        id,
                        wersja: 1,
                        ladunek.PrzeslanieNr,
                        konfiguracja!.Value,
                        wynik.ZgodnoscZZadaniem,
                        [.. wynik.Kroki.Select(k => KrokTranskrypcji.OdczytanyPrzezAgenta(k.Nr, k.Tekst, k.Latex, k.PewnoscOdczytu))],
                        teraz);
                    transkrypcje.Dodaj(nowa);
                    r.ZapiszTranskrypcjeAgenta(nowa.Id, nowa.Wersja, teraz);
                }
                else
                {
                    r.ZapiszTranskrypcjeAgenta(istniejaca.Id, istniejaca.Wersja, teraz);
                }
            },
            ct);
    }
}
