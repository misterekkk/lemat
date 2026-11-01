// @Role: Fabryka WebApplicationFactory<Program> z endpointami testowymi i bazą z LEMAT_BAZA | @DependsOn: Lemat.Api.Program, TestoweEndpointy | @UsedBy: Lemat.Api.Tests | @Task: TASK-121
using Lemat.Api.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Api.Tests.Support;

/// <summary>Host API w środowisku Development (Mock) z dodatkowymi endpointami <c>/api/v1/test/*</c>.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Development");
        builder.ConfigureServices(uslugi => uslugi.AddSingleton<IGrupaEndpointow, TestoweEndpointy>());
    }
}
