// @Role: Fabryka WebApplicationFactory<Program> z endpointami testowymi, bazą z LEMAT_BAZA, szybkim haszowaniem haseł i kolektorem logów | @DependsOn: Lemat.Api.Program, TestoweEndpointy, HaszowanieHasel, KolektorLogow | @UsedBy: Lemat.Api.Tests | @Task: TASK-121, TASK-123
using Lemat.Api.Endpoints;
using Lemat.Application.Accounts;
using Lemat.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lemat.Api.Tests.Support;

/// <summary>Host API w środowisku Development (Mock) z dodatkowymi endpointami <c>/api/v1/test/*</c>.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Wszystko, co host zapisał do logów (do sprawdzenia, że hasła i tokeny tam nie trafiają).</summary>
    public KolektorLogow Logi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logowanie => logowanie.AddProvider(Logi));
        builder.ConfigureServices(uslugi =>
        {
            uslugi.AddSingleton<IGrupaEndpointow, TestoweEndpointy>();

            // Testy nie sprawdzają siły argon2id (to robi PasswordHashingTests), więc przyspieszamy: 1 MiB, 1 iteracja.
            uslugi.RemoveAll<IHasherHasel>();
            uslugi.AddSingleton<IHasherHasel>(new HaszowanieHasel(1024, 1, 1));
        });
    }
}
