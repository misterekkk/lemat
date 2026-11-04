// @Role: Port repozytorium agregatu Zadanie (ze śledzeniem do zmiany z If-Match, do odczytu, po wersji, kandydaci doboru) | @DependsOn: Lemat.Domain.Content.Zadanie, ZadanieDoDoboru, FiltrKandydatow | @UsedBy: ContentFacade, ExerciseRepositoryEf, przypadki użycia Treści | @Task: TASK-017, TASK-125
using Lemat.Domain.Content;

namespace Lemat.Application.Content;

public interface IZadanieRepozytorium
{
    /// <summary>Wczytuje Zadanie do zmiany (ze śledzeniem, z wersjami); <paramref name="oczekiwanaWersja"/> = If-Match.</summary>
    Task<Zadanie?> PobierzAsync(ZadanieId id, int? oczekiwanaWersja, CancellationToken ct);

    Task<Zadanie?> PobierzDoOdczytuAsync(ZadanieId id, CancellationToken ct);

    /// <summary>Wiele Zadań do odczytu (bez śledzenia) w jednym zapytaniu; brakujące pomijane.</summary>
    Task<IReadOnlyList<Zadanie>> PobierzDoOdczytuAsync(IReadOnlyCollection<ZadanieId> identyfikatory, CancellationToken ct);

    /// <summary>Pozycje (Poziom trudności nominalny, id) Zadań OPUBLIKOWANYCH i GLOBALNYCH, opcjonalnie z jednego Działu; bez porządku.</summary>
    Task<IReadOnlyList<PozycjaListyZadan>> ListaOpublikowanychAsync(DzialId? dzial, CancellationToken ct);

    /// <summary>Zadanie zawierające wskazaną wersję (dowolną: aktualną lub zastąpioną).</summary>
    Task<Zadanie?> PobierzPoWersjiAsync(ZadanieWersjaId wersja, CancellationToken ct);

    Task<IReadOnlyList<ZadanieDoDoboru>> PobierzKandydatowAsync(FiltrKandydatow filtr, CancellationToken ct);

    /// <summary>Dodaje Zadanie razem z wierszem początkowym <c>kalibracja_trudnosci</c> (12.9).</summary>
    void Dodaj(Zadanie zadanie);
}
