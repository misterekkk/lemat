// @Role: Port repozytorium agregatu Rozwiązanie: dodawanie, odczyt ze śledzeniem (przejścia stanów) i bez (widoki), wyszukiwanie nieukończonego Rozwiązania Ucznia przy Zadaniu | @DependsOn: Lemat.Domain.Solutions.Rozwiazanie, UczenId, ZadanieId | @UsedBy: SolutionsFacade, SolutionRepositoryEf, handlery prac w tle (planowane TASK-127, TASK-128) | @Task: TASK-126
using Lemat.Domain.Accounts;
using Lemat.Domain.Content;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Solutions;

public interface IRozwiazanieRepozytorium
{
    void Dodaj(Rozwiazanie rozwiazanie);

    /// <summary>Rozwiązanie ze Zdjęciami do zmiany stanu (ze śledzeniem); <c>null</c>, gdy brak.</summary>
    Task<Rozwiazanie?> PobierzAsync(RozwiazanieId id, CancellationToken ct);

    /// <summary>Rozwiązanie ze Zdjęciami do odczytu (bez śledzenia); <c>null</c>, gdy brak.</summary>
    Task<Rozwiazanie?> PobierzDoOdczytuAsync(RozwiazanieId id, CancellationToken ct);

    /// <summary>
    /// Identyfikator Rozwiązania Ucznia przy Zadaniu, które jest w trakcie (PRZESLANE … OCENIANIE, także oczekujące na Weryfikację);
    /// <c>BLAD_ROZPOZNAWANIA</c>, <c>OCENIONE</c>, <c>PORZUCONE</c> i <c>ANULOWANE</c> nie blokują nowego Rozwiązania. <c>null</c>, gdy brak.
    /// </summary>
    Task<RozwiazanieId?> ZnajdzNieukonczoneAsync(UczenId uczen, ZadanieId zadanie, CancellationToken ct);
}
