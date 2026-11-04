// @Role: Port repozytorium agregatu Test diagnostyczny (z Pozycjami): wczytanie do zmiany, odczyt aktywnego lub zakończonego testu na poziom matury, lista Ucznia | @DependsOn: Lemat.Domain.Profile.TestDiagnostyczny, UczenId, PoziomMatury | @UsedBy: ProfilFasada, DiagnosticTestRepositoryEf | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Profile;

namespace Lemat.Application.Profile;

public interface ITestDiagnostycznyRepozytorium
{
    Task<TestDiagnostyczny?> PobierzAsync(TestDiagnostycznyId id, CancellationToken ct);

    Task<TestDiagnostyczny?> PobierzDoOdczytuAsync(TestDiagnostycznyId id, CancellationToken ct);

    /// <summary>Test <c>W_TOKU</c> albo <c>ZAKONCZONY</c> Ucznia na poziom matury (<c>ux_test_jeden</c>) albo <c>null</c>.</summary>
    Task<TestDiagnostyczny?> PobierzAktywnyLubZakonczonyAsync(UczenId uczen, PoziomMatury poziom, CancellationToken ct);

    void Dodaj(TestDiagnostyczny test);
}
