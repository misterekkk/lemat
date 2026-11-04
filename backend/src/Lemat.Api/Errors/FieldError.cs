// @Role: Błąd pojedynczego pola w 422 WALIDACJA (pole, kod_reguly, komunikat) | @DependsOn: — | @UsedBy: ProblemDetailsLemat, BladApiException, ObslugaBledowWiazania | @Task: TASK-121
namespace Lemat.Api.Errors;

/// <summary>Pozycja <c>bledy_pol</c>; <paramref name="Pole"/> to ścieżka w ciele żądania w snake_case (albo nazwa nagłówka).</summary>
public sealed record BladPola(string Pole, string KodReguly, string Komunikat);
