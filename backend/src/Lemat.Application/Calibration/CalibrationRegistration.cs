// @Role: Rejestracja w DI usług aplikacyjnych modułu Kalibracja (fasada); repozytoria rejestruje warstwa trwałości | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, KalibracjaFasada | @UsedBy: Lemat.Api/Worker/Scheduler/Tools (Program.cs), testy | @Task: TASK-018
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Application.Calibration;

public static class CalibrationRegistration
{
    public static IServiceCollection DodajModulKalibracji(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<IKalibracjaFasada, KalibracjaFasada>();
        return uslugi;
    }
}
