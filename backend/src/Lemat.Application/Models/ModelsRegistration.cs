// @Role: Rejestracja w DI usług aplikacyjnych modułu Modele (fasada); repozytoria rejestruje warstwa trwałości, porty modeli – moduł Lemat.Models | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, ModeleFasada | @UsedBy: Lemat.Api/Worker/Scheduler/Tools (Program.cs), testy | @Task: TASK-018
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Application.Models;

public static class ModelsRegistration
{
    public static IServiceCollection DodajModulModele(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<IModeleFasada, ModeleFasada>();
        return uslugi;
    }
}
