// @Role: Kompozycja procesu api (logi JSON, ProblemDetails, OpenAPI, adres dev) | @DependsOn: Microsoft.AspNetCore.OpenApi | @UsedBy: Lemat.Api.Program, Lemat.Architecture.Tests.KontenerDiTests | @Task: TASK-003
using Lemat.Api.Endpoints;

namespace Lemat.Api.Configuration;

/// <summary>Buduje i mapuje host procesu <c>api</c> (REST; SSE w kolejnych zadaniach).</summary>
public static class ApiHost
{
    public const string DomyslnyAdresDev = "http://localhost:5080";

    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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

        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi("v1");

        return builder;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapOpenApi("/api/v1/openapi.json");
        app.MapHealthEndpoints();
        return app;
    }
}
