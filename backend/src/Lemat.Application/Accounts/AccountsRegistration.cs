// @Role: Rejestracja w DI usług aplikacyjnych modułu Konta (fasada); repozytoria rejestruje warstwa trwałości | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, KontaFasada, handlery uwierzytelniania | @UsedBy: Lemat.Api/Worker/Scheduler/Tools (Program.cs), testy | @Task: TASK-016, TASK-123
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

    /// <summary>
    /// Handlery uwierzytelniania (rejestracja, logowanie, odświeżenie, wylogowanie). Wymagają w DI: <c>IHasherHasel</c>, <c>IWystawcaTokenow</c>,
    /// <c>LematOpcje</c>, modułów Konta, Profil i Rekomendacje oraz warstwy trwałości — wołać tylko w procesie api.
    /// </summary>
    public static IServiceCollection DodajUwierzytelnianie(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<FabrykaSesji>();
        uslugi.AddScoped<ZarejestrujUzytkownikaHandler>();
        uslugi.AddScoped<ZalogujHandler>();
        uslugi.AddScoped<OdswiezSesjeHandler>();
        uslugi.AddScoped<WylogujHandler>();
        return uslugi;
    }
}
