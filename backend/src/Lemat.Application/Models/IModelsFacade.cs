// @Role: Fasada modułu Modele (jedyne wejście z innych modułów, 6.2): konfiguracje modeli per Rola modelu (odczyt aktywnej, zapis, aktywacja) oraz zapis i odczyt Decyzji modelu (Z-4) | @DependsOn: Lemat.Domain.Models, KonfiguracjaModeluSkrot, NowaKonfiguracjaModelu | @UsedBy: Orkiestracja, Agent oceniający (planowane TASK-022), rejestr adapterów (TASK-054), Api (TASK-071) | @Task: TASK-018
using Lemat.Domain.Models;

namespace Lemat.Application.Models;

public interface IModeleFasada
{
    Task<KonfiguracjaModeluSkrot?> PobierzAktywnaKonfiguracjeAsync(RolaModelu rola, CancellationToken ct);

    Task<IReadOnlyList<KonfiguracjaModeluSkrot>> ListaKonfiguracjiAsync(CancellationToken ct);

    /// <summary>Zapisuje nową (nieaktywną) konfigurację roli.</summary>
    Task<KonfiguracjaModeluSkrot> ZapiszKonfiguracjeAsync(NowaKonfiguracjaModelu wejscie, CancellationToken ct);

    /// <summary>
    /// Aktywuje konfigurację, wyłączając poprzednią aktywną tej samej roli (jedna transakcja). Retencja ponad A-07 daje
    /// <c>BRAK_RETENCJI_DOSTAWCY</c>, mock w Production — <c>DOSTAWCA_NIEDOZWOLONY</c>.
    /// </summary>
    Task<KonfiguracjaModeluSkrot> AktywujAsync(KonfiguracjaModeluId id, CancellationToken ct);

    /// <summary>Zapisuje Decyzje modelu wsadowo (Z-4); wywołanie w otwartej jednostce pracy dołącza się do jej transakcji.</summary>
    Task ZapiszDecyzjeAsync(IReadOnlyList<DecyzjaModelu> decyzje, CancellationToken ct);

    Task<IReadOnlyList<DecyzjaModelu>> PobierzDecyzjeAsync(KontekstDecyzji kontekst, Guid kontekstId, CancellationToken ct);
}
