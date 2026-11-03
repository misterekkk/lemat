// @Role: Kompozycja procesu worker (host ogólny + /zdrowie na porcie 8081, wpięta trwałość PostgreSQL) | @DependsOn: Lemat.Application.Common, Lemat.Infrastructure.Persistence, Microsoft.AspNetCore.App | @UsedBy: Lemat.Worker.Program, Lemat.Architecture.Tests.KontenerDiTests | @Task: TASK-003, TASK-004, TASK-121, TASK-126
using Lemat.Application.Common;
using Lemat.Application.Grading;
using Lemat.Application.Solutions;
using Lemat.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Lemat.Worker.Configuration;

/// <summary>Buduje host procesu <c>worker</c>; wyłącznie kontrola zdrowia.</summary>
public static class WorkerHost
{
    public const string DomyslnyAdres = "http://0.0.0.0:8081";

    private const string PolaczenieZastepcze = "Host=localhost;Database=lemat;Username=lemat;Timeout=1;Command Timeout=1";

    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.DodajZmienneLemat();
        builder.Services.AddLematOpcje(builder.Configuration, builder.Environment.EnvironmentName);

        var opcje = builder.Configuration.GetSection(LematOpcje.NazwaSekcji).Get<LematOpcje>() ?? new LematOpcje();
        if (string.IsNullOrWhiteSpace(opcje.Baza.Polaczenie))
        {
            // Brak LEMAT_BAZA (np. test Production z błędem konfiguracji): źródło danych nie łączy się przy budowie.
            opcje.Baza.Polaczenie = PolaczenieZastepcze;
        }

        builder.Services.DodajPersystencje(opcje, RodzajProcesu.Worker);
        builder.Services.DodajModulRozwiazan();
        builder.Services.DodajModulOceniania();

        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole();

        if (string.IsNullOrEmpty(builder.Configuration["urls"])
            && string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_URLS"]))
        {
            builder.WebHost.UseUrls(DomyslnyAdres);
        }

        builder.Host.UseDefaultServiceProvider(opcje =>
        {
            opcje.ValidateScopes = true;
            opcje.ValidateOnBuild = true;
        });

        return builder;
    }

    public static WebApplication MapHealth(this WebApplication app)
    {
        app.MapGet("/zdrowie", () => Results.Ok(new { status = "OK" }));
        return app;
    }
}
