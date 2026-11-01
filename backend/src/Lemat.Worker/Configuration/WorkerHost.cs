// @Role: Kompozycja procesu worker (host ogólny + /zdrowie na porcie 8081, bez logiki) | @DependsOn: Microsoft.AspNetCore.App | @UsedBy: Lemat.Worker.Program, Lemat.Architecture.Tests.KontenerDiTests | @Task: TASK-003
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Lemat.Worker.Configuration;

/// <summary>Buduje host procesu <c>worker</c>; wyłącznie kontrola zdrowia.</summary>
public static class WorkerHost
{
    public const string DomyslnyAdres = "http://0.0.0.0:8081";

    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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
