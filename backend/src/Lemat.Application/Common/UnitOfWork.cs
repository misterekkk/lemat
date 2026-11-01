// @Role: Port jednostki pracy (ADR-056): jawna transakcja dla przypadku użycia zmieniającego stan; bez EnableRetryOnFailure | @DependsOn: — | @UsedBy: przypadki użycia wszystkich modułów, Lemat.Infrastructure.Persistence.JednostkaPracyEf, ZlecaniePracSql (TASK-024) | @Task: TASK-015
namespace Lemat.Application.Common;

/// <summary>
/// Jednostka pracy (ADR-056 p. 1): przypadek użycia wykonuje się w jednej jawnej transakcji
/// (<c>READ COMMITTED</c>) obejmującej zapisy EF Core oraz polecenia SQL (np. zlecanie prac w tle).
/// </summary>
public interface IJednostkaPracy
{
    /// <summary>Kod wyjątku <see cref="InvalidOperationException"/> zgłaszanego przez <see cref="WymagajTransakcji"/>.</summary>
    public const string KodPozaJednostkaPracy = "ZLECENIE_POZA_JEDNOSTKA_PRACY";

    /// <summary>Czy jednostka pracy ma teraz otwartą transakcję.</summary>
    bool TransakcjaOtwarta { get; }

    Task<T> WykonajAsync<T>(Func<CancellationToken, Task<T>> przypadekUzycia, CancellationToken ct);

    Task WykonajAsync(Func<CancellationToken, Task> przypadekUzycia, CancellationToken ct);

    /// <summary>Odrzuca (<see cref="InvalidOperationException"/>) operacje wymagające transakcji, gdy żadna nie jest otwarta.</summary>
    void WymagajTransakcji();
}
