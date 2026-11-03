// @Role: Rejestracja w DI usług aplikacyjnych modułu Rozwiązania (fasada); repozytoria rejestruje warstwa trwałości | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, RozwiazaniaFasada | @UsedBy: Lemat.Api/Worker (hosty), testy | @Task: TASK-126
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Application.Solutions;

public static class SolutionsRegistration
{
    public static IServiceCollection DodajModulRozwiazan(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<IRozwiazaniaFasada, RozwiazaniaFasada>();
        return uslugi;
    }
}
