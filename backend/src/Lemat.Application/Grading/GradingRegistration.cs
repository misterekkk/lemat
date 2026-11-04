// @Role: Rejestracja w DI usług aplikacyjnych modułu Ocenianie (fasada); repozytoria rejestruje warstwa trwałości | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, OcenianieFasada | @UsedBy: Lemat.Api/Worker (hosty), testy | @Task: TASK-126
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Application.Grading;

public static class GradingRegistration
{
    public static IServiceCollection DodajModulOceniania(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<IOcenianieFasada, OcenianieFasada>();
        return uslugi;
    }
}
