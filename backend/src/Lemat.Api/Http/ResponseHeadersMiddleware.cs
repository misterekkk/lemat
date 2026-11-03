// @Role: Middleware ustawiający nagłówki odpowiedzi API: Cache-Control: no-store, Vary: Authorization, X-Lemat-Wersja-Api | @DependsOn: — | @UsedBy: ApiHost | @Task: TASK-121, TASK-127
namespace Lemat.Api.Http;

/// <summary>Dopisuje nagłówki wspólne dla wszystkich odpowiedzi (kontrakt POC 1).</summary>
public sealed class NaglowkiOdpowiedziMiddleware(RequestDelegate nastepny)
{
    public const string WersjaApi = "1.0.0";

    public Task InvokeAsync(HttpContext kontekst)
    {
        ArgumentNullException.ThrowIfNull(kontekst);
        kontekst.Response.OnStarting(static stan =>
        {
            var odpowiedz = (HttpResponse)stan;
            // Endpoint może ustawić własne Cache-Control (np. obraz z magazynu lokalnego: private, max-age=300).
            if (string.IsNullOrEmpty(odpowiedz.Headers.CacheControl))
            {
                odpowiedz.Headers.CacheControl = "no-store";
            }

            odpowiedz.Headers.Append("Vary", "Authorization");
            odpowiedz.Headers["X-Lemat-Wersja-Api"] = WersjaApi;
            return Task.CompletedTask;
        }, kontekst.Response);
        return nastepny(kontekst);
    }
}
