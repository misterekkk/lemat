// @Role: Port repozytorium agregatu Konfiguracja modelu: odczyt aktywnej konfiguracji roli, lista, dodanie, dezaktywacja poprzedniej aktywnej (ux_konfiguracja_aktywna) | @DependsOn: Lemat.Domain.Models.KonfiguracjaModelu, RolaModelu | @UsedBy: ModeleFasada, rejestr adapterów (planowane TASK-054), ModelConfigurationRepositoryEf | @Task: TASK-018
using Lemat.Domain.Models;

namespace Lemat.Application.Models;

public interface IKonfiguracjaModeluRepozytorium
{
    /// <summary>Wczytuje konfigurację do zmiany (ze śledzeniem).</summary>
    Task<KonfiguracjaModelu?> PobierzAsync(KonfiguracjaModeluId id, CancellationToken ct);

    Task<KonfiguracjaModelu?> PobierzDoOdczytuAsync(KonfiguracjaModeluId id, CancellationToken ct);

    /// <summary>Aktywna konfiguracja roli (odczyt bez śledzenia) albo <c>null</c>.</summary>
    Task<KonfiguracjaModelu?> PobierzAktywnaAsync(RolaModelu rola, CancellationToken ct);

    Task<IReadOnlyList<KonfiguracjaModelu>> ListaAsync(CancellationToken ct);

    void Dodaj(KonfiguracjaModelu konfiguracja);

    /// <summary>
    /// Wyłącza aktywną konfigurację roli inną niż <paramref name="pozostaw"/> (pojedyncze UPDATE przed zapisem aktywacji nowej,
    /// w tej samej transakcji; kolejność wymuszona przez <c>ux_konfiguracja_aktywna</c>).
    /// </summary>
    Task DezaktywujAktywnaAsync(RolaModelu rola, KonfiguracjaModeluId pozostaw, CancellationToken ct);
}
