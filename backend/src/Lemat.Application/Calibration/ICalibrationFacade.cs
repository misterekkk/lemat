// @Role: Fasada modułu Kalibracja (jedyne wejście z innych modułów, 6.2): odczyt aktywnego Progu pewności i Kalibratora dla Konfiguracji modelu i Rodzaju zadania oraz zapis kalibratora tożsamościowego i nowego Progu (seed, Administrator) | @DependsOn: Lemat.Domain.Calibration, KonfiguracjaModeluId, RodzajZadania | @UsedBy: Agent oceniający (planowane TASK-022), seed-dev (TASK-026), Api (TASK-071), KALIBRUJ (TASK-032) | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Application.Calibration;

public interface IKalibracjaFasada
{
    /// <summary>Aktywny Próg pewności pary albo <c>null</c> (wtedy Ocena automatyczna jest niedozwolona — Z-3, trasa do Weryfikacji).</summary>
    Task<MigawkaProgu?> PobierzAktywnyProgAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct);

    Task<MigawkaKalibratora?> PobierzKalibratorAsync(KalibratorPewnosciId id, CancellationToken ct);

    /// <summary>Aktywny Próg razem z Kalibratorem, do którego się odnosi; <c>null</c>, gdy brak aktywnego Progu.</summary>
    Task<AktywnaKalibracja?> PobierzAktywnaKalibracjeAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct);

    /// <summary>Zapisuje kalibrator tożsamościowy (MVP, U-10).</summary>
    Task<MigawkaKalibratora> ZapiszKalibratorTozsamosciowyAsync(KonfiguracjaModeluId konfiguracja, RodzajZadania rodzaj, CancellationToken ct);

    /// <summary>Zapisuje nowy aktywny Próg pary, wyłączając poprzedni (jedna transakcja; Próg jest niezmienny, zmiana = nowy rekord).</summary>
    Task<MigawkaProgu> UstawProgAsync(
        KonfiguracjaModeluId konfiguracja,
        RodzajZadania rodzaj,
        KalibratorPewnosciId kalibrator,
        decimal wartosc,
        ZrodloProgu zrodlo,
        string uzasadnienie,
        UzytkownikId? utworzonyPrzez,
        CancellationToken ct);
}
