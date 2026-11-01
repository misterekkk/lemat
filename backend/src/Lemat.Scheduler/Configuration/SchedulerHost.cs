// @Role: Kompozycja procesu scheduler (host ogólny + /zdrowie na porcie 8082, bez logiki) | @DependsOn: Lemat.Application.Common, Microsoft.AspNetCore.App | @UsedBy: Lemat.Scheduler.Program, Lemat.Architecture.Tests.KontenerDiTests | @Task: TASK-003, TASK-004
using Lemat.Application.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Lemat.Scheduler.Configuration;

/// <summary>Buduje host procesu <c>scheduler</c>; wyłącznie kontrola zdrowia.</summary>
public static class SchedulerHost
{
    public const string DomyslnyAdres = "http://0.0.0.0:8082";

    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.DodajZmienneLemat();
        builder.Services.AddLematOpcje(builder.Configuration, builder.Environment.EnvironmentName);

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
