// @Role: Grupa endpointów /api/v1/auth/* (rejestracja, logowanie, odświeżenie, wylogowanie) z refresh tokenem w ciasteczku lemat_rt (HttpOnly, SameSite=Strict, Path=/api/v1/auth) | @DependsOn: ZarejestrujUzytkownikaHandler, ZalogujHandler, OdswiezSesjeHandler, WylogujHandler, walidatory wejść, ProblemWriter | @UsedBy: ApiHost (IGrupaEndpointow) | @Task: TASK-123
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Lemat.Api.Contracts.Accounts;
using Lemat.Api.Errors;
using Lemat.Api.Http;
using Lemat.Api.Security;
using Lemat.Api.Validation;
using Lemat.Application.Accounts;
using Lemat.Application.Common;
using Lemat.Domain.Accounts;

namespace Lemat.Api.Endpoints;

public sealed class EndpointyKont : IGrupaEndpointow
{
    public const string NazwaCiasteczka = "lemat_rt";
    public const string SciezkaCiasteczka = "/api/v1/auth";

    public void Mapuj(IEndpointRouteBuilder trasy)
    {
        ArgumentNullException.ThrowIfNull(trasy);
        var grupa = trasy.MapGroup("/api/v1/auth");

        grupa.MapPost("/rejestracja", ZarejestrujAsync)
            .WithName("ZarejestrujUzytkownika")
            .RequireAuthorization(PolitykiAutoryzacji.Anonimowy)
            .AllowAnonymous()
            .Produces<RejestracjaWyjscie>(StatusCodes.Status201Created);

        grupa.MapPost("/logowanie", ZalogujAsync)
            .WithName("Zaloguj")
            .AllowAnonymous()
            .Produces<SesjaWyjscie>();

        grupa.MapPost("/odswiezenie", OdswiezAsync)
            .WithName("OdswiezSesje")
            .AllowAnonymous()
            .Produces<SesjaWyjscie>();

        grupa.MapPost("/wylogowanie", WylogujAsync)
            .WithName("Wyloguj")
            .RequireAuthorization(PolitykiAutoryzacji.Uczen)
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> ZarejestrujAsync(
        RejestracjaWejscie wejscie,
        IValidator<RejestracjaWejscie> walidator,
        ZarejestrujUzytkownikaHandler handler,
        LematOpcje opcje,
        HttpContext kontekst,
        CancellationToken ct)
    {
        await WalidacjaWejscia.WymagajAsync(walidator, wejscie, ct);
        var sesja = await handler.WykonajAsync(
            new ZarejestrujUzytkownikaWejscie(
                wejscie.Email,
                wejscie.Haslo,
                wejscie.Imie,
                wejscie.Nazwisko,
                RolaUzytkownika.Uczen,
                wejscie.OswiadczenieWieku16,
                SkrotKlienta(kontekst)),
            ct);
        UstawCiasteczko(kontekst, sesja, opcje);
        return Results.Json(new RejestracjaWyjscie(sesja.Status, DoWyjscia(sesja)), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> ZalogujAsync(
        LogowanieWejscie wejscie,
        IValidator<LogowanieWejscie> walidator,
        ZalogujHandler handler,
        LematOpcje opcje,
        HttpContext kontekst,
        CancellationToken ct)
    {
        await WalidacjaWejscia.WymagajAsync(walidator, wejscie, ct);
        var sesja = await handler.WykonajAsync(new ZalogujWejscie(wejscie.Email, wejscie.Haslo, SkrotKlienta(kontekst)), ct);
        UstawCiasteczko(kontekst, sesja, opcje);
        return Results.Ok(DoWyjscia(sesja));
    }

    private static async Task<IResult> OdswiezAsync(OdswiezSesjeHandler handler, LematOpcje opcje, HttpContext kontekst, CancellationToken ct)
    {
        kontekst.Request.Cookies.TryGetValue(NazwaCiasteczka, out var tokenJawny);
        var sesja = await handler.WykonajAsync(tokenJawny, SkrotKlienta(kontekst), ct);
        if (sesja is null)
        {
            await ProblemWriter.ZapiszAsync(kontekst, KodBledu.SesjaUniewazniona, token: ct, przedZapisem: odpowiedz => WyczyscCiasteczko(odpowiedz, kontekst.Request.IsHttps));
            return Results.Empty;
        }

        UstawCiasteczko(kontekst, sesja, opcje);
        return Results.Ok(DoWyjscia(sesja));
    }

    private static async Task<IResult> WylogujAsync(WylogujHandler handler, HttpContext kontekst, CancellationToken ct)
    {
        kontekst.Request.Cookies.TryGetValue(NazwaCiasteczka, out var tokenJawny);
        await handler.WykonajAsync(BiezacyUzytkownik.Id(kontekst.User), tokenJawny, ct);
        WyczyscCiasteczko(kontekst.Response, kontekst.Request.IsHttps);
        return Results.NoContent();
    }

    private static SesjaWyjscie DoWyjscia(SesjaRezultat sesja) =>
        new(
            sesja.TokenDostepu.Token,
            sesja.TokenDostepu.WygasaAt,
            new UzytkownikSesji(sesja.UzytkownikId.Wartosc, sesja.Rola, sesja.Imie, new FlagiUzytkownika(false, false)));

    private static byte[] SkrotKlienta(HttpContext kontekst) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(kontekst.Request.Headers.UserAgent.ToString()));

    private static void UstawCiasteczko(HttpContext kontekst, SesjaRezultat sesja, LematOpcje opcje) =>
        kontekst.Response.Cookies.Append(
            NazwaCiasteczka,
            sesja.TokenOdswiezania,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Path = SciezkaCiasteczka,
                Secure = kontekst.Request.IsHttps,
                MaxAge = TimeSpan.FromDays(opcje.Jwt.OdswiezenieDni),
            });

    private static void WyczyscCiasteczko(HttpResponse odpowiedz, bool https) =>
        odpowiedz.Cookies.Append(
            NazwaCiasteczka,
            string.Empty,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Path = SciezkaCiasteczka,
                Secure = https,
                MaxAge = TimeSpan.Zero,
                Expires = DateTimeOffset.UnixEpoch,
            });
}
