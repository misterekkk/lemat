// @Role: Błędy wiązania i deserializacji ciała jako ProblemDetails: ucięty JSON -> 400, nieznane pole/zły typ/brak pola -> 422 z bledy_pol, 413 | @DependsOn: ProblemWriter, ErrorCatalog, JsonApi | @UsedBy: ApiHost (AddExceptionHandler) | @Task: TASK-121
using System.Text.Json;
using System.Text.RegularExpressions;
using Lemat.Api.Configuration;
using Microsoft.AspNetCore.Diagnostics;

namespace Lemat.Api.Errors;

/// <summary>Tłumaczy <see cref="BadHttpRequestException"/> (przy <c>ThrowOnBadRequest</c>) na ProblemDetails (ADR-067 p. 6).</summary>
public sealed partial class ObslugaBledowWiazania : IExceptionHandler
{
    [GeneratedRegex("\"(?<n>[^\"]+)\"")]
    private static partial Regex NazwaWCudzyslowie();

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not BadHttpRequestException zle)
        {
            return false;
        }

        var (kod, pole, regula) = Klasyfikuj(zle);
        var bledy = kod == KodBledu.Walidacja && regula is not null
            ? new[] { new BladPola(pole ?? string.Empty, regula, ErrorCatalog.KomunikatReguly(regula)) }
            : null;
        return await ProblemWriter.ZapiszAsync(httpContext, kod, bledyPol: bledy, token: cancellationToken);
    }

    private static (KodBledu Kod, string? Pole, string? Regula) Klasyfikuj(BadHttpRequestException zle)
    {
        if (zle.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return (KodBledu.ZapytanieZaDuze, null, null);
        }

        if (zle.StatusCode == StatusCodes.Status415UnsupportedMediaType)
        {
            return (KodBledu.NieobslugiwanyFormat, null, null);
        }

        if (zle.InnerException is JsonException json)
        {
            return JestBlademSkladni(json)
                ? (KodBledu.ZapytanieNieprawidlowe, null, null)
                : (KodBledu.Walidacja, Sciezka(json.Path), json.Message.Contains("could not be mapped", StringComparison.Ordinal) ? "POLE_NIEZNANE" : "TYP_NIEPRAWIDLOWY");
        }

        var nazwa = NazwaWCudzyslowie().Match(zle.Message).Groups["n"].Value;
        var pole = string.IsNullOrEmpty(nazwa) ? null : JsonApi.Nazwy.ConvertName(nazwa.Split(' ')[^1]);
        return (KodBledu.Walidacja, pole, "WIAZANIE");
    }

    // Ucięty lub niepoprawny składniowo JSON: wyjątek czytnika (JsonReaderException) nie ma ścieżki właściwości ani LineNumber-less mapowania.
    private static bool JestBlademSkladni(JsonException json) =>
        json.InnerException?.GetType().Name == "JsonReaderException"
        || json.GetType().Name == "JsonReaderException"
        || (json.Path is null or "$" && json.BytePositionInLine is not null && json.Message.Contains("is invalid after a single JSON value", StringComparison.Ordinal))
        || json.Message.Contains("end of data", StringComparison.OrdinalIgnoreCase)
        || json.Message.Contains("is an invalid start of a value", StringComparison.OrdinalIgnoreCase);

    private static string Sciezka(string? sciezka) => (sciezka ?? "$").TrimStart('$').TrimStart('.');
}
