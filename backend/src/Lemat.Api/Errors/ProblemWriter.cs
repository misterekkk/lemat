// @Role: Buduje i zapisuje odpowiedź application/problem+json (type, kod, trace_id, instance, szczegoly, bledy_pol) | @DependsOn: ErrorCatalog, ProblemDetailsLemat, JsonApi | @UsedBy: ObslugaBledowWiazania, ObslugaWyjatkowDomenowych, ApiHost (status code pages) | @Task: TASK-121
using System.Diagnostics;
using Lemat.Api.Configuration;

namespace Lemat.Api.Errors;

/// <summary>Jedyne miejsce, które zapisuje błędy API w kształcie kontraktu POC 1.1.</summary>
public static class ProblemWriter
{
    public const string TypMedium = "application/problem+json";

    public static ProblemDetailsLemat Zbuduj(
        HttpContext kontekst,
        KodBledu kod,
        string? opis = null,
        IReadOnlyDictionary<string, object?>? szczegoly = null,
        IReadOnlyList<BladPola>? bledyPol = null)
    {
        ArgumentNullException.ThrowIfNull(kontekst);
        var wpis = ErrorCatalog.Pobierz(kod);
        return new ProblemDetailsLemat(
            ErrorCatalog.Type(kod),
            wpis.Tytul,
            wpis.Status,
            string.IsNullOrWhiteSpace(opis) ? wpis.Opis : opis,
            kontekst.Request.Path.Value ?? string.Empty,
            kod,
            Activity.Current?.TraceId.ToString() ?? kontekst.TraceIdentifier,
            szczegoly,
            kod == KodBledu.Walidacja ? bledyPol ?? [] : null);
    }

    /// <summary>Zapisuje problem; zwraca <c>false</c>, gdy odpowiedź już się zaczęła (nie można zmienić statusu).</summary>
    public static async Task<bool> ZapiszAsync(
        HttpContext kontekst,
        KodBledu kod,
        string? opis = null,
        IReadOnlyDictionary<string, object?>? szczegoly = null,
        IReadOnlyList<BladPola>? bledyPol = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(kontekst);
        if (kontekst.Response.HasStarted)
        {
            return false;
        }

        var problem = Zbuduj(kontekst, kod, opis, szczegoly, bledyPol);
        kontekst.Response.Clear();
        kontekst.Response.StatusCode = problem.Status;
        await kontekst.Response.WriteAsJsonAsync(problem, JsonApi.Opcje, TypMedium, token);
        return true;
    }
}
