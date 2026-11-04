// @Role: Testy jednostkowe wystawcy tokenów dostępu JWT ES256: nagłówek, claims sub/rola/status/jti/exp, ważność 15 min, weryfikacja podpisu, unikalność jti | @DependsOn: WystawcaTokenow, KluczJwt, LematOpcje, Microsoft.IdentityModel.JsonWebTokens | @UsedBy: dotnet test | @Task: TASK-123
using System.Security.Cryptography;
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Infrastructure.Security;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lemat.Infrastructure.Tests.Security;

public sealed class TokenIssuerTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    private static (WystawcaTokenow Wystawca, KluczJwt Klucz, LematOpcje Opcje) Utworz()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var opcje = new LematOpcje { Jwt = { Klucz = ecdsa.ExportECPrivateKeyPem() } };
        var klucz = KluczJwt.Zaladuj(opcje.Jwt, "Development");
        return (new WystawcaTokenow(klucz, opcje), klucz, opcje);
    }

    [Fact]
    public void TokenMaAlgorytmES256ClaimsIWaznoscPietnastuMinut()
    {
        var (wystawca, _, opcje) = Utworz();
        var id = new UzytkownikId(Guid.Parse("c1000000-0000-4000-8000-000000000001"));

        var wystawiony = wystawca.Wystaw(id, RolaUzytkownika.Uczen, StatusKonta.Aktywne, Teraz);

        wystawiony.WygasaAt.ShouldBe(Teraz.AddMinutes(15));
        var token = new JsonWebToken(wystawiony.Token);
        token.Alg.ShouldBe("ES256");
        token.Kid.ShouldNotBeNullOrWhiteSpace();
        token.Subject.ShouldBe("c1000000-0000-4000-8000-000000000001");
        token.GetClaim("rola").Value.ShouldBe("UCZEN");
        token.GetClaim("status").Value.ShouldBe("AKTYWNE");
        token.Id.ShouldNotBeNullOrWhiteSpace();
        token.ValidTo.ShouldBe(Teraz.AddMinutes(15).UtcDateTime);
        token.Issuer.ShouldBe(opcje.Jwt.Wystawca);
        token.Audiences.ShouldBe([opcje.Jwt.Odbiorca]);
    }

    [Fact]
    public async Task PodpisJestWeryfikowalnyKluczemIOdrzucanyPrzyObcymKluczu()
    {
        var (wystawca, klucz, opcje) = Utworz();
        var token = wystawca.Wystaw(new UzytkownikId(Guid.NewGuid()), RolaUzytkownika.Nauczyciel, StatusKonta.OczekujeNaZatwierdzenie, DateTimeOffset.UtcNow).Token;
        var parametry = new TokenValidationParameters
        {
            ValidIssuer = opcje.Jwt.Wystawca,
            ValidAudience = opcje.Jwt.Odbiorca,
            IssuerSigningKey = klucz.Klucz,
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
        };
        var handler = new JsonWebTokenHandler();

        var poprawny = await handler.ValidateTokenAsync(token, parametry);
        poprawny.IsValid.ShouldBeTrue();
        poprawny.Claims["status"].ShouldBe("OCZEKUJE_NA_ZATWIERDZENIE");

        using var obcy = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        parametry.IssuerSigningKey = new ECDsaSecurityKey(obcy) { KeyId = klucz.Klucz.KeyId };
        (await handler.ValidateTokenAsync(token, parametry)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void KazdyTokenMaInnyJti()
    {
        var (wystawca, _, _) = Utworz();
        var id = new UzytkownikId(Guid.NewGuid());

        var a = new JsonWebToken(wystawca.Wystaw(id, RolaUzytkownika.Uczen, StatusKonta.Aktywne, Teraz).Token);
        var b = new JsonWebToken(wystawca.Wystaw(id, RolaUzytkownika.Uczen, StatusKonta.Aktywne, Teraz).Token);

        a.Id.ShouldNotBe(b.Id);
    }
}
