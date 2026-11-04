// @Role: Implementacja IModeleFasada nad repozytoriami Konfiguracji modelu i Decyzji modelu oraz IJednostkaPracy; aktywacja w jednej transakcji z dezaktywacją poprzedniej | @DependsOn: IKonfiguracjaModeluRepozytorium, IDecyzjaModeluRepozytorium, IJednostkaPracy, LematOpcje, TimeProvider | @UsedBy: DI (ModelsRegistration), moduły przez IModeleFasada | @Task: TASK-018
using Lemat.Application.Common;
using Lemat.Domain.Common;
using Lemat.Domain.Models;
using Microsoft.Extensions.Options;

namespace Lemat.Application.Models;

public sealed class ModeleFasada(
    IKonfiguracjaModeluRepozytorium konfiguracje,
    IDecyzjaModeluRepozytorium repozytoriumDecyzji,
    IJednostkaPracy jednostkaPracy,
    IOptions<LematOpcje> opcje,
    TimeProvider czas) : IModeleFasada
{
    private bool Produkcja => string.Equals(opcje.Value.Srodowisko, "Production", StringComparison.OrdinalIgnoreCase);

    public async Task<KonfiguracjaModeluSkrot?> PobierzAktywnaKonfiguracjeAsync(RolaModelu rola, CancellationToken ct)
    {
        var konfiguracja = await konfiguracje.PobierzAktywnaAsync(rola, ct);
        return konfiguracja is null ? null : KonfiguracjaModeluSkrot.Z(konfiguracja);
    }

    public async Task<IReadOnlyList<KonfiguracjaModeluSkrot>> ListaKonfiguracjiAsync(CancellationToken ct) =>
        [.. (await konfiguracje.ListaAsync(ct)).Select(KonfiguracjaModeluSkrot.Z)];

    public async Task<KonfiguracjaModeluSkrot> ZapiszKonfiguracjeAsync(NowaKonfiguracjaModelu wejscie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(wejscie);
        var konfiguracja = await jednostkaPracy.WykonajAsync(
            _ =>
            {
                var nowa = KonfiguracjaModelu.Utworz(
                    new KonfiguracjaModeluId(GeneratorUuid.Nowy(czas)),
                    wejscie.Rola,
                    wejscie.Dostawca,
                    wejscie.Model,
                    wejscie.Parametry,
                    wejscie.WersjaPromptu,
                    wejscie.UtworzonaPrzez,
                    czas.GetUtcNow());
                konfiguracje.Dodaj(nowa);
                return Task.FromResult(nowa);
            },
            ct);
        return KonfiguracjaModeluSkrot.Z(konfiguracja);
    }

    public async Task<KonfiguracjaModeluSkrot> AktywujAsync(KonfiguracjaModeluId id, CancellationToken ct)
    {
        var konfiguracja = await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var znaleziona = await konfiguracje.PobierzAsync(id, token)
                    ?? throw new NiezmiennikNaruszonyException("KONFIGURACJA_MODELU_NIE_ZNALEZIONA", "Nie znaleziono Konfiguracji modelu.");
                znaleziona.Aktywuj(Produkcja);
                await konfiguracje.DezaktywujAktywnaAsync(znaleziona.Rola, znaleziona.Id, token);
                return znaleziona;
            },
            ct);
        return KonfiguracjaModeluSkrot.Z(konfiguracja);
    }

    public Task ZapiszDecyzjeAsync(IReadOnlyList<DecyzjaModelu> decyzje, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(decyzje);
        return jednostkaPracy.WykonajAsync(
            _ =>
            {
                repozytoriumDecyzji.DodajRange(decyzje);
                return Task.CompletedTask;
            },
            ct);
    }

    public Task<IReadOnlyList<DecyzjaModelu>> PobierzDecyzjeAsync(KontekstDecyzji kontekst, Guid kontekstId, CancellationToken ct) =>
        repozytoriumDecyzji.ListaDlaKontekstuAsync(kontekst, kontekstId, ct);
}
