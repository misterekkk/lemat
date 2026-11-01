// @Role: Rejestracja w DI usług bezpieczeństwa: haszowanie haseł argon2id, klucz JWT ES256 i wystawca tokenów dostępu | @DependsOn: LematOpcje, HaszowanieHasel, KluczJwt, WystawcaTokenow | @UsedBy: Lemat.Api.Configuration.ApiHost, testy | @Task: TASK-123
using Lemat.Application.Accounts;
using Lemat.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lemat.Infrastructure.Security;

public static class SecurityRegistration
{
    public static IServiceCollection DodajBezpieczenstwo(this IServiceCollection uslugi, LematOpcje opcje)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        ArgumentNullException.ThrowIfNull(opcje);
        uslugi.TryAddSingleton<IHasherHasel, HaszowanieHasel>();
        uslugi.TryAddSingleton(_ => KluczJwt.Zaladuj(opcje.Jwt, opcje.Srodowisko));
        uslugi.TryAddSingleton<IWystawcaTokenow, WystawcaTokenow>();
        return uslugi;
    }
}
