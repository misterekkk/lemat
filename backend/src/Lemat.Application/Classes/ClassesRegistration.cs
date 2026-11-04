// @Role: Rejestracja w DI usług aplikacyjnych modułu Klasy (fasada); repozytorium rejestruje warstwa trwałości | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, KlasyFasada | @UsedBy: Lemat.Api/Worker/Scheduler/Tools (Program.cs), testy | @Task: TASK-018
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Application.Classes;

public static class ClassesRegistration
{
    public static IServiceCollection DodajModulKlasy(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<IKlasyFasada, KlasyFasada>();
        return uslugi;
    }
}
