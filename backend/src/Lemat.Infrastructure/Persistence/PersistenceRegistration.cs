// @Role: Rejestracja warstwy trwałości w DI: źródło danych Npgsql z enumami i dynamicznym jsonb, LematDbContext (+ fabryka), interceptory, IJednostkaPracy; pule połączeń wg procesu (ADR-059) | @DependsOn: LematOpcje, RodzajProcesu, MapowanieTypowWyliczeniowych, JednostkaPracyEf | @UsedBy: Lemat.Api/Worker/Scheduler/Tools (Program.cs), testy | @Task: TASK-015, TASK-016, TASK-017, TASK-018, TASK-019, TASK-126, TASK-127, TASK-128
using Lemat.Application.Accounts;
using Lemat.Application.Calibration;
using Lemat.Application.Classes;
using Lemat.Application.Common;
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Application.Content;
using Lemat.Application.Grading;
using Lemat.Application.Models;
using Lemat.Application.Profile;
using Lemat.Application.Recommendations;
using Lemat.Application.Solutions;
using Lemat.Infrastructure.BackgroundJobs;
using Lemat.Infrastructure.Persistence.Conventions;
using Lemat.Infrastructure.Persistence.Interceptors;
using Lemat.Infrastructure.Persistence.Repositories.Accounts;
using Lemat.Infrastructure.Persistence.Repositories.Calibration;
using Lemat.Infrastructure.Persistence.Repositories.Classes;
using Lemat.Infrastructure.Persistence.Repositories.Content;
using Lemat.Infrastructure.Persistence.Repositories.Grading;
using Lemat.Infrastructure.Persistence.Repositories.Models;
using Lemat.Infrastructure.Persistence.Repositories.Profile;
using Lemat.Infrastructure.Persistence.Repositories.Recommendations;
using Lemat.Infrastructure.Persistence.Repositories.Solutions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Lemat.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    /// <summary>
    /// Connection string procesu: pula 30 (api) / 40 (worker) / 3 (scheduler) połączeń, <c>CommandTimeout</c> 5 s (api) albo 30 s,
    /// nazwa aplikacji ułatwiająca diagnostykę (ADR-059). Jedno dodatkowe połączenie <c>LISTEN</c> nie należy do puli EF.
    /// </summary>
    public static string CiagPolaczenia(this BazaOpcje baza, RodzajProcesu proces)
    {
        ArgumentNullException.ThrowIfNull(baza);
        if (string.IsNullOrWhiteSpace(baza.Polaczenie))
        {
            throw new InvalidOperationException("Brak Lemat:Baza:Polaczenie (LEMAT_BAZA).");
        }

        var builder = new NpgsqlConnectionStringBuilder(baza.Polaczenie)
        {
            MaxPoolSize = proces switch
            {
                RodzajProcesu.Api => 30,
                RodzajProcesu.Worker => 40,
                _ => 3,
            },
            CommandTimeout = proces == RodzajProcesu.Api ? 5 : 30,
            ApplicationName = $"lemat-{proces.ToString().ToLowerInvariant()}",
        };
        return builder.ConnectionString;
    }

    public static IServiceCollection DodajPersystencje(this IServiceCollection uslugi, LematOpcje opcje, RodzajProcesu proces)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        ArgumentNullException.ThrowIfNull(opcje);

        var zrodlo = new NpgsqlDataSourceBuilder(opcje.Baza.CiagPolaczenia(proces))
            .MapujTypyWyliczeniowe()
            .EnableDynamicJson()
            .Build();

        uslugi.TryAddSingleton(TimeProvider.System);
        uslugi.AddSingleton(zrodlo);
        uslugi.AddSingleton<ZnacznikiCzasuInterceptor>();
        uslugi.AddSingleton<WersjaAgregatuInterceptor>();
        uslugi.AddDbContext<LematDbContext>(
            (sp, builder) => builder
                .UseNpgsql(zrodlo, o => o
                    .MapujTypyWyliczenioweEf()
                    .MigrationsHistoryTable(LematDbContext.TabelaHistoriiMigracji))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<WersjaAgregatuInterceptor>(),
                    sp.GetRequiredService<ZnacznikiCzasuInterceptor>())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Singleton);
        uslugi.AddDbContextFactory<LematDbContext>(lifetime: ServiceLifetime.Singleton);
        uslugi.AddScoped<JednostkaPracyEf>();
        uslugi.AddScoped<IJednostkaPracy>(sp => sp.GetRequiredService<JednostkaPracyEf>());
        uslugi.AddScoped<IZlecaniePrac, ZlecaniePracSql>();
        uslugi.AddSingleton<IKolejkaPrac, KolejkaPracSql>();
        uslugi.AddScoped<IUzytkownikRepozytorium, UzytkownikRepozytoriumEf>();
        uslugi.AddScoped<IUczenRepozytorium, UczenRepozytoriumEf>();
        uslugi.AddScoped<INauczycielRepozytorium, NauczycielRepozytoriumEf>();
        uslugi.AddScoped<ITokenOdswiezaniaRepozytorium, TokenOdswiezaniaRepozytoriumEf>();
        uslugi.AddScoped<IProbaLogowaniaRepozytorium, ProbaLogowaniaRepozytoriumEf>();
        uslugi.AddScoped<IZadanieRepozytorium, ZadanieRepozytoriumEf>();
        uslugi.AddScoped<IWkladkaTeoretycznaRepozytorium, WkladkaTeoretycznaRepozytoriumEf>();
        uslugi.AddScoped<IDzialRepozytorium, DzialRepozytoriumEf>();
        uslugi.AddScoped<IUmiejetnoscRepozytorium, UmiejetnoscRepozytoriumEf>();
        uslugi.AddScoped<IKlasaRepozytorium, KlasaRepozytoriumEf>();
        uslugi.AddScoped<IKonfiguracjaModeluRepozytorium, KonfiguracjaModeluRepozytoriumEf>();
        uslugi.AddScoped<IDecyzjaModeluRepozytorium, DecyzjaModeluRepozytoriumEf>();
        uslugi.AddScoped<IKalibratorPewnosciRepozytorium, KalibratorPewnosciRepozytoriumEf>();
        uslugi.AddScoped<IProgPewnosciRepozytorium, ProgPewnosciRepozytoriumEf>();
        uslugi.AddScoped<IProfilRepozytorium, ProfilRepozytoriumEf>();
        uslugi.AddScoped<IWynikZadaniaRepozytorium, WynikZadaniaRepozytoriumEf>();
        uslugi.AddScoped<ITestDiagnostycznyRepozytorium, TestDiagnostycznyRepozytoriumEf>();
        uslugi.AddScoped<IParametryZadaniaDlaProfilu, ParametryZadaniaDlaProfiluEf>();
        uslugi.AddScoped<IAktualizacjaKalibracjiTrudnosci, AktualizacjaKalibracjiTrudnosciSql>();
        uslugi.AddScoped<ISciezkaNaukiRepozytorium, SciezkaNaukiRepozytoriumEf>();
        uslugi.AddScoped<IRekomendacjaRepozytorium, RekomendacjaRepozytoriumEf>();
        uslugi.AddScoped<IRozwiazanieRepozytorium, RozwiazanieRepozytoriumEf>();
        uslugi.AddScoped<ITranskrypcjaRepozytorium, TranskrypcjaRepozytoriumEf>();
        uslugi.AddScoped<IOcenaRepozytorium, OcenaRepozytoriumEf>();
        return uslugi;
    }
}
