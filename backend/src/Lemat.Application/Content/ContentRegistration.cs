// @Role: Rejestracja w DI usług aplikacyjnych modułu Treści (fasada); repozytoria rejestruje warstwa trwałości | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, TresciFasada | @UsedBy: Lemat.Api/Worker/Scheduler/Tools (Program.cs), testy | @Task: TASK-017
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Application.Content;

public static class ContentRegistration
{
    public static IServiceCollection DodajModulTresci(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<ITresciFasada, TresciFasada>();
        return uslugi;
    }
}
