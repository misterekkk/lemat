// @Role: Test kontenerów DI procesów api, worker, scheduler (ValidateOnBuild, ValidateScopes) | @DependsOn: Lemat.Api, Lemat.Worker, Lemat.Scheduler | @UsedBy: dotnet test | @Task: TASK-003
using Lemat.Api.Configuration;
using Lemat.Scheduler.Configuration;
using Lemat.Worker.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lemat.Architecture.Tests.Hosts;

public sealed class KontenerDiTests
{
    public static TheoryData<string> Procesy => ["api", "worker", "scheduler"];

    private static WebApplicationBuilder Utworz(string proces)
    {
        string[] args = ["--urls", "http://127.0.0.1:0", "--environment", "Development"];
        return proces switch
        {
            "api" => ApiHost.CreateBuilder(args),
            "worker" => WorkerHost.CreateBuilder(args),
            "scheduler" => SchedulerHost.CreateBuilder(args),
            _ => throw new ArgumentOutOfRangeException(nameof(proces)),
        };
    }

    [Theory]
    [MemberData(nameof(Procesy))]
    public void KontenerBudujeSieZWalidacja(string proces)
    {
        var builder = Utworz(proces);

        using var app = builder.Build();

        app.Services.ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(Procesy))]
    public void KontenerMaWlaczonaWalidacjeZakresowIBudowy(string proces)
    {
        var builder = Utworz(proces);

        // Builder jest budowany z ValidateScopes/ValidateOnBuild: usługa scoped w singletonie musi zostać odrzucona.
        builder.Services.AddScoped<ZalezneOdZakresu>();
        builder.Services.AddSingleton<SingletonZalezny>();

        Should.Throw<Exception>(() => builder.Build());
    }

    private sealed class ZalezneOdZakresu;

    private sealed class SingletonZalezny(ZalezneOdZakresu zaleznosc)
    {
        public ZalezneOdZakresu Zaleznosc { get; } = zaleznosc;
    }
}
