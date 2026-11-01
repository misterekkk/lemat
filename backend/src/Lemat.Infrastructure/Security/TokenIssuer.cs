// @Role: Wystawca tokenów dostępu JWT ES256 (15 min; claims sub, rola, status, jti, exp) | @DependsOn: KluczJwt, JwtOpcje, IWystawcaTokenow, Microsoft.IdentityModel.JsonWebTokens | @UsedBy: DI (SecurityRegistration), RegisterUserHandler, LoginHandler, RefreshSessionHandler | @Task: TASK-123
using System.Security.Claims;
using Lemat.Application.Accounts;
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lemat.Infrastructure.Security;

public sealed class WystawcaTokenow(KluczJwt klucz, LematOpcje opcje) : IWystawcaTokenow
{
    public const string RoszczenieRola = "rola";
    public const string RoszczenieStatus = "status";

    private static readonly JsonWebTokenHandler Obslugujacy = new();

    public WystawionyTokenDostepu Wystaw(UzytkownikId uzytkownik, RolaUzytkownika rola, StatusKonta status, DateTimeOffset teraz)
    {
        var wygasa = teraz.AddMinutes(opcje.Jwt.WaznoscMinut);
        var opis = new SecurityTokenDescriptor
        {
            Issuer = opcje.Jwt.Wystawca,
            Audience = opcje.Jwt.Odbiorca,
            IssuedAt = teraz.UtcDateTime,
            NotBefore = teraz.UtcDateTime,
            Expires = wygasa.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, uzytkownik.Wartosc.ToString("D")),
                new Claim(RoszczenieRola, KonwencjaNazw.PascalNaUpperSnake(rola.ToString())),
                new Claim(RoszczenieStatus, KonwencjaNazw.PascalNaUpperSnake(status.ToString())),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            SigningCredentials = new SigningCredentials(klucz.Klucz, SecurityAlgorithms.EcdsaSha256),
        };
        return new WystawionyTokenDostepu(Obslugujacy.CreateToken(opis), wygasa);
    }
}
