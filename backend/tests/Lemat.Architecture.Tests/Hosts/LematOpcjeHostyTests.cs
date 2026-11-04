// @Role: Test hostów api/worker/scheduler: start w Development bez zmiennych, odrzucenie mocka w Production, spójność appsettings | @DependsOn: Lemat.Api, Lemat.Worker, Lemat.Scheduler, Lemat.Application | @UsedBy: dotnet test | @Task: TASK-004
using Lemat.Api.Configuration;
using Lemat.Application.Common;
using Lemat.Architecture.Tests.Support;
using Lemat.Scheduler.Configuration;
using Lemat.Worker.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lemat.Architecture.Tests.Hosts;

public sealed class LematOpcjeHostyTests
{
    private static readonly string[] ProjektyHostow = ["Lemat.Api", "Lemat.Worker", "Lemat.Scheduler"];

    public static TheoryData<string> Procesy => ["api", "worker", "scheduler"];

    private static WebApplicationBuilder Utworz(string proces, string srodowisko)
    {
        string[] args = ["--urls", "http://127.0.0.1:0", "--environment", srodowisko];
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
    public async Task HostStartujeWDevelopmentBezZmiennychSrodowiskowych(string proces)
    {
        await using var app = Utworz(proces, "Development").Build();

        await app.StartAsync(TestContext.Current.CancellationToken);

        var opcje = app.Services.GetRequiredService<LematOpcje>();
        opcje.Modele.Dostawca.ShouldBe("mock");
        opcje.Srodowisko.ShouldBe("Development");
        opcje.Konta.AutozatwierdzanieNauczycieli.ShouldBeTrue();
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [MemberData(nameof(Procesy))]
    public async Task HostOdrzucaMockaWProductionPrzyStarcie(string proces)
    {
        await using var app = Utworz(proces, "Production").Build();

        await Should.ThrowAsync<OptionsValidationException>(() => app.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void AppsettingsTrzechHostowMajaTaSamaSekcjeLemat()
    {
        var sekcje = ProjektyHostow
            .Select(projekt => Wczytaj(Path.Combine(RepositoryLocator.BackendDirectory, "src", projekt)))
            .ToList();

        sekcje[1].ShouldBe(sekcje[0]);
        sekcje[2].ShouldBe(sekcje[0]);
        sekcje[0].ShouldContain(para => para.Key == "Lemat:Konta:AutozatwierdzanieNauczycieli" && para.Value == "True");
    }

    [Fact]
    public void AppsettingsOdpowiadajaWartosciomDomyslnymLematOpcje()
    {
        var konfiguracja = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(RepositoryLocator.BackendDirectory, "src", "Lemat.Api", "appsettings.json"), optional: false)
            .Build();
        var zJson = new LematOpcje();
        konfiguracja.GetSection(LematOpcje.NazwaSekcji).Bind(zJson);
        var domyslne = new LematOpcje();

        zJson.Zdjecia.MaksNaPrzeslanie.ShouldBe(domyslne.Zdjecia.MaksNaPrzeslanie);
        zJson.Transkrypcja.MaksKrokow.ShouldBe(domyslne.Transkrypcja.MaksKrokow);
        zJson.Ocenianie.TerminTwardySekund.ShouldBe(domyslne.Ocenianie.TerminTwardySekund);
        zJson.Progi.PoczatkowyJev.ShouldBe(domyslne.Progi.PoczatkowyJev);
        zJson.Progi.PoczatkowyZapasowy.ShouldBe(domyslne.Progi.PoczatkowyZapasowy);
        zJson.Modele.Dostawca.ShouldBe(domyslne.Modele.Dostawca);
        zJson.Strumien.HeartbeatSekund.ShouldBe(domyslne.Strumien.HeartbeatSekund);
        zJson.Zdjecia.DozwoloneTypyMime.ShouldBe(domyslne.Zdjecia.DozwoloneTypyMime); // lista nie jest dublowana w JSON (binder dopisuje elementy do domyślnych)
    }

    private static List<KeyValuePair<string, string?>> Wczytaj(string katalog) =>
        new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(katalog, "appsettings.json"), optional: false)
            .AddJsonFile(Path.Combine(katalog, "appsettings.Development.json"), optional: false)
            .Build()
            .GetSection(LematOpcje.NazwaSekcji)
            .AsEnumerable()
            .Where(para => para.Value is not null)
            .OrderBy(para => para.Key, StringComparer.Ordinal)
            .ToList();
}
