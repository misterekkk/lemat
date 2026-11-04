// @Role: Wynik autoryzacji z kodami domenowymi: 403 KONTO_NIEAKTYWNE (status konta inny niż AKTYWNE) albo 403 BRAK_UPRAWNIEN; 401 obsługuje domyślny mechanizm (ProblemDetails dopisują strony statusów) | @DependsOn: ProblemWriter, PolitykiAutoryzacji, WystawcaTokenow | @UsedBy: PolitykiAutoryzacji (DI) | @Task: TASK-123
using Lemat.Api.Errors;
using Lemat.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Lemat.Api.Security;

public sealed class WynikAutoryzacjiLemat : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _domyslny = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        if (authorizeResult.Forbidden && context.User.Identity?.IsAuthenticated == true)
        {
            var status = context.User.FindFirst(WystawcaTokenow.RoszczenieStatus)?.Value;
            var kod = status is not null && status != PolitykiAutoryzacji.StatusAktywneWartosc ? KodBledu.KontoNieaktywne : KodBledu.BrakUprawnien;
            await ProblemWriter.ZapiszAsync(context, kod, token: context.RequestAborted);
            return;
        }

        await _domyslny.HandleAsync(next, context, policy, authorizeResult);
    }
}
