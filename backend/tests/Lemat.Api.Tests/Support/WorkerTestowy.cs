// @Role: Host procesu worker budowany w teście (bez startu Kestrela i pętli odpytywania) z zegarem testowym; testy wywołują ProcesorPrac wprost, więc potok PRZESLANE→OCENIONE przebiega deterministycznie na tej samej bazie co API | @DependsOn: WorkerHost, ZegarTestowy, ProcesorPrac | @UsedBy: Solutions/SolutionsFlowTests | @Task: TASK-128
extern alias WorkerAsm;

using Lemat.Application.Common.BackgroundJobs;
using WorkerAsm::Lemat.Worker.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Api.Tests.Support;

public sealed class WorkerTestowy : IAsyncDisposable
{
    private readonly Microsoft.AspNetCore.Builder.WebApplication _host;

    public WorkerTestowy(ZegarTestowy zegar)
    {
        ArgumentNullException.ThrowIfNull(zegar);
        Zegar = zegar;
        // Baza z LEMAT_BAZA (BazaTestowaFixture); środowisko Development daje Mocki i ścieżkę magazynu z appsettings.Development.json.
        var builder = WorkerHost.CreateBuilder(["--environment=Development"]);
        builder.Services.AddSingleton<TimeProvider>(zegar);
        _host = builder.Build();
    }

    public ZegarTestowy Zegar { get; }

    public IServiceProvider Uslugi => _host.Services;

    public ProcesorPrac Procesor => Uslugi.GetRequiredService<ProcesorPrac>();

    /// <summary>Wykonuje wszystkie dostępne prace (także zlecone w trakcie); zwraca liczbę wykonanych.</summary>
    public async Task<int> PrzetworzWszystkoAsync(CancellationToken ct)
    {
        var liczba = 0;
        while (await Procesor.PrzetworzJednaAsync(ct))
        {
            liczba++;
        }

        return liczba;
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();
}
