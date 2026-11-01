// @Role: Rejestracja w DI usług aplikacyjnych modułu Profil (fasada); repozytoria i porty rejestruje warstwa trwałości | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, ProfilFasada | @UsedBy: Lemat.Api/Worker/Scheduler/Tools (Program.cs), testy | @Task: TASK-019
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Application.Profile;

public static class ProfileRegistration
{
    public static IServiceCollection DodajModulProfil(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<IProfilFasada, ProfilFasada>();
        return uslugi;
    }
}
