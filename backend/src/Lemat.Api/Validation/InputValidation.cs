// @Role: Pomocnik walidacji wejścia FluentValidation -> 422 WALIDACJA z bledy_pol (pole snake_case, kod_reguly z listy POC) | @DependsOn: FluentValidation, BladApiException, JsonApi | @UsedBy: AccountsEndpoints, walidatory wejść | @Task: TASK-123
using FluentValidation;
using Lemat.Api.Configuration;
using Lemat.Api.Errors;

namespace Lemat.Api.Validation;

public static class WalidacjaWejscia
{
    /// <summary>Waliduje wejście; pierwsze naruszenia każdego pola trafiają do <c>bledy_pol</c> (kolejność reguł), brak błędów — wynik bez efektu.</summary>
    public static async Task WymagajAsync<T>(IValidator<T> walidator, T wejscie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(walidator);
        ArgumentNullException.ThrowIfNull(wejscie);
        var wynik = await walidator.ValidateAsync(wejscie, ct);
        if (wynik.IsValid)
        {
            return;
        }

        var bledy = wynik.Errors
            .GroupBy(b => b.PropertyName, StringComparer.Ordinal)
            .Select(g => g.First())
            .Select(b => new BladPola(JsonApi.Nazwy.ConvertName(b.PropertyName), b.ErrorCode, b.ErrorMessage))
            .ToList();
        throw new BladApiException(KodBledu.Walidacja, null, null, bledy);
    }
}
