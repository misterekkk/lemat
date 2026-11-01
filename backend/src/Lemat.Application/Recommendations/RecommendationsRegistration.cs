// @Role: Rejestracja w DI usług aplikacyjnych modułu Rekomendacje (fasada); repozytoria rejestruje warstwa trwałości | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, RekomendacjeFasada | @UsedBy: Lemat.Api/Worker/Scheduler/Tools (Program.cs), testy | @Task: TASK-019
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Application.Recommendations;

public static class RecommendationsRegistration
{
    public static IServiceCollection DodajModulRekomendacje(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<IRekomendacjeFasada, RekomendacjeFasada>();
        return uslugi;
    }
}
