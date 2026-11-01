// @Role: Filtr endpointu honorujący nagłówek X-Lemat-Mock-Scenariusz wyłącznie w Development/E2E (ADR-065); nieznana wartość -> 422 MOCK_SCENARIUSZ_NIEZNANY | @DependsOn: LematOpcje.Srodowisko, BladApiException | @UsedBy: POST /rozwiazania (TASK-127), ApiHost | @Task: TASK-121
using Lemat.Api.Errors;
using Lemat.Application.Common;

namespace Lemat.Api.Http;

/// <summary>Zapisuje wybrany scenariusz Mocka w <c>HttpContext.Items</c> (klucz <see cref="KluczItems"/>).</summary>
public sealed class FiltrScenariuszaMock(LematOpcje opcje) : IEndpointFilter
{
    public const string Naglowek = "X-Lemat-Mock-Scenariusz";
    public const string KluczItems = "lemat.mock.scenariusz";
    public const string Domyslny = "domyslny";
    public const string RozpoznanieNieczytelne = "rozpoznanie-nieczytelne";

    private static readonly string[] Znane = [Domyslny, RozpoznanieNieczytelne];

    /// <summary>Scenariusz wybrany dla bieżącego żądania (<c>domyslny</c>, gdy brak nagłówka lub poza Development/E2E).</summary>
    public static string Pobierz(HttpContext kontekst)
    {
        ArgumentNullException.ThrowIfNull(kontekst);
        return kontekst.Items.TryGetValue(KluczItems, out var wartosc) && wartosc is string scenariusz ? scenariusz : Domyslny;
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (DozwolonyW(opcje.Srodowisko) && context.HttpContext.Request.Headers.TryGetValue(Naglowek, out var wartosci) && !string.IsNullOrWhiteSpace(wartosci[0]))
        {
            var scenariusz = wartosci[0]!.Trim();
            if (!Znane.Contains(scenariusz, StringComparer.Ordinal))
            {
                throw BladApiException.Walidacja(Naglowek, "MOCK_SCENARIUSZ_NIEZNANY", ErrorCatalog.KomunikatReguly("MOCK_SCENARIUSZ_NIEZNANY"));
            }

            context.HttpContext.Items[KluczItems] = scenariusz;
        }

        return next(context);
    }

    private static bool DozwolonyW(string srodowisko) =>
        string.Equals(srodowisko, "Development", StringComparison.OrdinalIgnoreCase)
        || string.Equals(srodowisko, "E2E", StringComparison.OrdinalIgnoreCase);
}
