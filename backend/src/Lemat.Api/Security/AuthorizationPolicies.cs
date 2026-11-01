// @Role: Nazwy i definicje polityk autoryzacji (Anonimowy, Zalogowany, Uczen = rola UCZEN ze statusem konta AKTYWNE) oraz konfiguracja uwierzytelniania JWT Bearer ES256 | @DependsOn: KluczJwt, LematOpcje, WystawcaTokenow, Microsoft.AspNetCore.Authentication.JwtBearer | @UsedBy: ApiHost, AccountsEndpoints, kolejne grupy endpointów | @Task: TASK-123
using Lemat.Application.Common;
using Lemat.Domain.Common;
using Lemat.Domain.Accounts;
using Lemat.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lemat.Api.Security;

public static class PolitykiAutoryzacji
{
    public const string Anonimowy = "Anonimowy";
    public const string Zalogowany = "Zalogowany";
    public const string Uczen = "Uczen";

    private static readonly string RolaUczen = KonwencjaNazw.PascalNaUpperSnake(nameof(RolaUzytkownika.Uczen));
    private static readonly string StatusAktywne = KonwencjaNazw.PascalNaUpperSnake(nameof(StatusKonta.Aktywne));

    public static IServiceCollection DodajUwierzytelnianieJwt(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        uslugi.AddSingleton<IConfigureOptions<JwtBearerOptions>, KonfiguracjaJwtBearer>();
        uslugi.AddSingleton<IAuthorizationMiddlewareResultHandler, WynikAutoryzacjiLemat>();
        uslugi.AddAuthorizationBuilder()
            .AddPolicy(Anonimowy, p => p.RequireAssertion(_ => true))
            .AddPolicy(Zalogowany, p => p.RequireAuthenticatedUser())
            .AddPolicy(Uczen, p => p
                .RequireAuthenticatedUser()
                .RequireClaim(WystawcaTokenow.RoszczenieRola, RolaUczen)
                .RequireClaim(WystawcaTokenow.RoszczenieStatus, StatusAktywne));
        return uslugi;
    }

    public static string StatusAktywneWartosc => StatusAktywne;

    private sealed class KonfiguracjaJwtBearer(KluczJwt klucz, LematOpcje opcje, TimeProvider czas) : IConfigureNamedOptions<JwtBearerOptions>
    {
        public void Configure(string? name, JwtBearerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (name != JwtBearerDefaults.AuthenticationScheme)
            {
                return;
            }

            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = opcje.Jwt.Wystawca,
                ValidateAudience = true,
                ValidAudience = opcje.Jwt.Odbiorca,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = klucz.Klucz,
                ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
                ClockSkew = TimeSpan.Zero,
                NameClaimType = "sub",
                RoleClaimType = WystawcaTokenow.RoszczenieRola,
                LifetimeValidator = (przed, wygasa, _, _) =>
                {
                    var teraz = czas.GetUtcNow().UtcDateTime;
                    return wygasa is { } koniec && teraz < koniec && (przed is not { } start || teraz >= start);
                },
            };
        }

        public void Configure(JwtBearerOptions options) => Configure(JwtBearerDefaults.AuthenticationScheme, options);
    }
}
