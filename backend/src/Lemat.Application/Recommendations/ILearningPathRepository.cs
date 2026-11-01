// @Role: Port repozytorium agregatu Ścieżka nauki: wczytanie z elementami aktywnymi (PROPONOWANY, ROZPOCZETY) oraz wskazanym elementem, odczyt bez śledzenia, 10 ostatnich elementów dla Agenta rekomendującego | @DependsOn: Lemat.Domain.Recommendations.SciezkaNauki, UczenId, ElementSciezkiId | @UsedBy: RekomendacjeFasada, LearningPathRepositoryEf | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Recommendations;

namespace Lemat.Application.Recommendations;

public interface ISciezkaNaukiRepozytorium
{
    /// <summary>
    /// Wczytuje Ścieżkę Ucznia do zmiany (ze śledzeniem) tylko z elementami PROPONOWANY i ROZPOCZETY oraz — gdy podano —
    /// z elementem <paramref name="dodatkowyElement"/> (nawet terminalnym); <paramref name="oczekiwanaWersja"/> = If-Match.
    /// </summary>
    Task<SciezkaNauki?> PobierzAsync(UczenId uczen, ElementSciezkiId? dodatkowyElement, int? oczekiwanaWersja, CancellationToken ct);

    Task<SciezkaNauki?> PobierzDoOdczytuAsync(UczenId uczen, CancellationToken ct);

    /// <summary>Ostatnie elementy Ścieżki (najnowsze pierwsze) — historia bez ładowania agregatu (C6-15 p. 4).</summary>
    Task<IReadOnlyList<ElementSciezki>> OstatnieElementyAsync(UczenId uczen, int liczba, CancellationToken ct);

    void Dodaj(SciezkaNauki sciezka);
}
