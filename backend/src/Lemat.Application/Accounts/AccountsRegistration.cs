// @Role: Rejestracja w DI usług aplikacyjnych modułu Konta (fasada); repozytoria rejestruje warstwa trwałości | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, KontaFasada | @UsedBy: Lemat.Api/Worker/Scheduler/Tools (Program.cs), testy | @Task: TASK-016
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Application.Accounts;

public static class AccountsRegistration
{
    public static IServiceCollection DodajModulKonta(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<IKontaFasada, KontaFasada>();
        return uslugi;
    }
}
