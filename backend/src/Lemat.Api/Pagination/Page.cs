// @Role: Odpowiedź stronicowana Strona<T> { elementy, nastepny_kursor } | @DependsOn: — | @UsedBy: endpointy list (TASK-125, TASK-127), testy | @Task: TASK-121
namespace Lemat.Api.Pagination;

/// <summary>Odpowiedź listy ze stronicowaniem kursorowym (ADR-062 p. 5).</summary>
public sealed record Strona<T>(IReadOnlyList<T> Elementy, string? NastepnyKursor);
