// @Role: Fasada modułu Profil (6.2): Profil ucznia (Elo), rejestracja i zastosowanie Wyników zadania, unieważnianie i odtwarzanie, Test diagnostyczny; bez zależności od Orchestration | @DependsOn: Lemat.Domain.Profile, ProfilSkrot, NowyWynikZadania, TestSkrot | @UsedBy: Rekomendacje, Rozwiązania (TASK-020), Ocenianie (TASK-021), Orkiestracja (AKTUALIZUJ_PROFIL), Api (planowane TASK-073) | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;

namespace Lemat.Application.Profile;

public interface IProfilFasada
{
    /// <summary>Zakłada pusty Profil (rejestracja Ucznia, 9.1); idempotentne.</summary>
    Task ZalozProfilAsync(UczenId uczen, CancellationToken ct);

    Task<ProfilSkrot?> PobierzProfilAsync(UczenId uczen, CancellationToken ct);

    /// <summary>Dodaje brakujące Umiejętności w stanie DOMYŚLNYM (po wyborze poziomu matury); zwraca liczbę dodanych. Zakłada Profil, gdy go brak.</summary>
    Task<int> InicjalizujOpanowanieAsync(UczenId uczen, IReadOnlyList<UmiejetnoscId> umiejetnosci, CancellationToken ct);

    /// <summary>
    /// Tworzy Wynik zadania z wagą <c>w_status · w_proba</c> (druga i kolejne próby w 60 dniach: 0,5). Ocena wstępna z możliwym błędem
    /// odczytu nie tworzy Wyniku (FR-28) — wtedy zwraca <c>null</c>. Idempotentne dla tej samej Oceny/Odpowiedzi.
    /// </summary>
    Task<WynikSkrot?> ZarejestrujWynikAsync(NowyWynikZadania wejscie, CancellationToken ct);

    /// <summary>
    /// AKTUALIZUJ_PROFIL: stosuje oczekujące Wyniki modelem Elo i kalibruje trudność Zadań (tylko <c>liczy_do_trudnosci</c>);
    /// gdy istnieją unieważnione Wyniki, odtwarza cały Profil od stanu DOMYŚLNEGO. Zwraca liczbę zastosowanych Wyników.
    /// </summary>
    Task<int> ZastosujOczekujaceWynikiAsync(UczenId uczen, CancellationToken ct);

    /// <summary>Unieważnia Wyniki powstałe z Oceny (np. zastąpionej); zwraca liczbę unieważnionych. Odtworzenie wykonuje <see cref="ZastosujOczekujaceWynikiAsync"/>.</summary>
    Task<int> UniewaznWynikiOcenyAsync(Guid ocenaId, PowodUniewaznieniaWyniku powod, CancellationToken ct);

    /// <summary>Tworzy Test diagnostyczny <c>W_TOKU</c>; drugi test na ten poziom matury kończy się <c>TEST_JUZ_WYKONANY</c> (<c>ux_test_jeden</c>).</summary>
    Task<TestSkrot> RozpocznijTestAsync(
        UczenId uczen,
        PoziomMatury poziom,
        IReadOnlyList<ZadanieWersjaId> wersjeZadan,
        IReadOnlyList<DzialId> pominieteDzialy,
        CancellationToken ct);

    Task<TestSkrot?> PobierzTestAsync(UczenId uczen, TestDiagnostycznyId id, CancellationToken ct);

    /// <summary>
    /// Kończy test: zapisuje Wyniki (waga 1,00, <c>created_at</c> = zakończenie), stosuje je razem z K = 0,8 i propagacją w Dziale
    /// oraz kalibruje trudność — w jednej transakcji.
    /// </summary>
    Task ZakonczTestAsync(UczenId uczen, TestDiagnostycznyId id, IReadOnlyList<OdpowiedzNaPozycje> odpowiedzi, CancellationToken ct);

    Task PominTestAsync(UczenId uczen, TestDiagnostycznyId id, CancellationToken ct);
}
