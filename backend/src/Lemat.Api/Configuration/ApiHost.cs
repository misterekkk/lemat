// @Role: Kompozycja procesu api (logi JSON, JSON API, ProblemDetails RFC 9457, nagłówki, kursor, trwałość i moduły Konta/Treści, OpenAPI, adres dev) | @DependsOn: Lemat.Application.Common, Lemat.Infrastructure.Persistence, JsonApi, Lemat.Api.Errors, Lemat.Api.Http | @UsedBy: Lemat.Api.Program, Lemat.Architecture.Tests.KontenerDiTests, Lemat.Api.Tests | @Task: TASK-003, TASK-004, TASK-121, TASK-123, TASK-124, TASK-126, TASK-127, TASK-128, TASK-129
using System.Reflection;
using Lemat.Api.Endpoints;
using Lemat.Api.Errors;
using Lemat.Api.Http;
using Lemat.Api.Pagination;
using Lemat.Application.Accounts;
using Lemat.Application.Common;
using Lemat.Application.Content;
using Lemat.Application.Grading;
using Lemat.Application.Profile;
using Lemat.Application.Recommendations;
using Lemat.Application.Solutions;
using FluentValidation;
using Lemat.Api.Contracts.Accounts;
using Lemat.Api.Contracts.Solutions;
using Lemat.Api.Security;
using Lemat.Api.Validation;
using Lemat.Infrastructure.Security;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Storage;
using Microsoft.AspNetCore.Diagnostics;

namespace Lemat.Api.Configuration;

/// <summary>Buduje i mapuje host procesu <c>api</c> (REST; SSE w kolejnych zadaniach).</summary>
public static class ApiHost
{
    public const string DomyslnyAdresDev = "http://localhost:5080";

    private const string PolaczenieZastepcze = "Host=localhost;Database=lemat;Username=lemat;Timeout=1;Command Timeout=1";

    private static string KluczKursora(LematOpcje opcje) =>
        !string.IsNullOrWhiteSpace(opcje.Kursor.Klucz) ? opcje.Kursor.Klucz : KodekKursora.KluczDeweloperski;

    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.DodajZmienneLemat();
        // Generator dokumentu OpenAPI przy budowie (GetDocument.Insider) startuje host w środowisku Production bez zmiennych LEMAT_*.
        var generatorOpenApi = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
        builder.Services.AddLematOpcje(builder.Configuration, builder.Environment.EnvironmentName, walidujPrzyStarcie: !generatorOpenApi);

        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole();

        if (builder.Environment.IsDevelopment()
            && string.IsNullOrEmpty(builder.Configuration["urls"])
            && string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_URLS"]))
        {
            builder.WebHost.UseUrls(DomyslnyAdresDev);
        }

        builder.Host.UseDefaultServiceProvider(opcje =>
        {
            opcje.ValidateScopes = true;
            opcje.ValidateOnBuild = true;
        });

        builder.Services.ConfigureHttpJsonOptions(opcje => JsonApi.Konfiguruj(opcje.SerializerOptions));
        builder.Services.Configure<RouteHandlerOptions>(opcje => opcje.ThrowOnBadRequest = true);
        builder.Services.AddExceptionHandler<ObslugaBledowWiazania>();
        builder.Services.AddExceptionHandler<ObslugaWyjatkowDomenowych>();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi("v1", opcjeOpenApi => opcjeOpenApi.AddSchemaTransformer(new OpcjonalneSchematOpenApi()));

        var opcje = builder.Configuration.GetSection(LematOpcje.NazwaSekcji).Get<LematOpcje>() ?? new LematOpcje();
        if (string.IsNullOrWhiteSpace(opcje.Srodowisko))
        {
            opcje.Srodowisko = builder.Environment.EnvironmentName;
        }

        if (string.IsNullOrWhiteSpace(opcje.Baza.Polaczenie))
        {
            // Brak LEMAT_BAZA (np. generator OpenAPI, test Production z błędem konfiguracji): źródło danych nie łączy się przy budowie.
            opcje.Baza.Polaczenie = PolaczenieZastepcze;
        }

        builder.Services.DodajPersystencje(opcje, RodzajProcesu.Api);
        builder.Services.DodajModulKonta();
        builder.Services.DodajModulTresci();
        builder.Services.DodajModulProfil();
        builder.Services.DodajModulRekomendacje();
        builder.Services.DodajModulRozwiazan();
        builder.Services.DodajModulOceniania();
        builder.Services.DodajMagazynIObrobkeObrazow(builder.Environment.ContentRootPath);
        builder.Services.DodajPrzypadkiUzyciaRozwiazan();
        builder.Services.AddScoped<MapperRozwiazan>();
        builder.Services.DodajBezpieczenstwo(opcje);
        builder.Services.DodajUwierzytelnianie();
        builder.Services.DodajPrzypadkiUzyciaUcznia();
        builder.Services.DodajUwierzytelnianieJwt();
        builder.Services.AddSingleton<IValidator<RejestracjaWejscie>, RejestracjaWejscieWalidator>();
        builder.Services.AddSingleton<IValidator<LogowanieWejscie>, LogowanieWejscieWalidator>();
        builder.Services.AddSingleton<IValidator<ZatwierdzenieRozwiazaniaWejscie>, ZatwierdzenieRozwiazaniaWejscieWalidator>();
        builder.Services.AddSingleton<IGrupaEndpointow, EndpointyKont>();
        builder.Services.AddSingleton<IGrupaEndpointow, EndpointyUcznia>();
        builder.Services.AddSingleton<IGrupaEndpointow, EndpointyTresci>();
        builder.Services.AddSingleton<IGrupaEndpointow, EndpointyRozwiazan>();
        builder.Services.AddSingleton<IGrupaEndpointow, EndpointyProfilu>();
        builder.Services.AddSingleton(sp => new KodekKursora(KluczKursora(sp.GetRequiredService<LematOpcje>())));
        builder.Services.AddScoped<FiltrScenariuszaMock>();

        return builder;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.UseMiddleware<NaglowkiOdpowiedziMiddleware>();
        app.UseWhen(
            kontekst => HttpMethods.IsPost(kontekst.Request.Method) && kontekst.Request.Path.Equals(SciezkaPrzeslaniaRozwiazania, StringComparison.OrdinalIgnoreCase),
            galaz => galaz.Use(UstawLimitCialaZdjec));
        app.UseExceptionHandler();
        app.UseStatusCodePages(ZapiszProblemDlaStatusu);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapOpenApi("/api/v1/openapi.json");
        app.MapHealthEndpoints();
        foreach (var grupa in app.Services.GetServices<IGrupaEndpointow>())
        {
            grupa.Mapuj(app);
        }

        return app;
    }

    /// <summary>Do 5 zdjęć po 10 MB plus pola formularza: 52 MB na całe ciało (kontrakt POC 7.4); pozostałe żądania zachowują domyślny limit Kestrela.</summary>
    private const long MaksCialoPrzeslaniaBajty = 52_428_800;

    private const string SciezkaPrzeslaniaRozwiazania = "/api/v1/rozwiazania";

    private static Task UstawLimitCialaZdjec(HttpContext kontekst, RequestDelegate nastepny)
    {
        if (kontekst.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
        {
            limit.MaxRequestBodySize = MaksCialoPrzeslaniaBajty;
        }

        return nastepny(kontekst);
    }

    private static readonly HashSet<int> StatusyZKodem = [400, 401, 403, 404, 405, 412, 413, 415, 422, 428, 500, 503];

    /// <summary>404/405/415 z routingu, 401/403 z uwierzytelniania itd. bez ciała dostają ProblemDetails z <c>kod</c> (ADR-067 p. 7).</summary>
    private static async Task ZapiszProblemDlaStatusu(StatusCodeContext kontekst)
    {
        var odpowiedz = kontekst.HttpContext.Response;
        if (!StatusyZKodem.Contains(odpowiedz.StatusCode) || odpowiedz.ContentLength is > 0 || !string.IsNullOrEmpty(odpowiedz.ContentType))
        {
            return;
        }

        await ProblemWriter.ZapiszAsync(kontekst.HttpContext, ErrorCatalog.DlaStatusu(odpowiedz.StatusCode));
    }
}
