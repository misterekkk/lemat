// @Role: Rejestracja w DI usług aplikacyjnych modułu Rozwiązania (fasada) i przypadków użycia zapisu; repozytoria rejestruje warstwa trwałości | @DependsOn: Microsoft.Extensions.DependencyInjection.Abstractions, RozwiazaniaFasada | @UsedBy: Lemat.Api/Worker (hosty), testy | @Task: TASK-126, TASK-127
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

    /// <summary>
    /// Przypadki użycia zapisu w procesie api (POST /rozwiazania). Wymagają w DI modułów Treści i Rekomendacje, portów
    /// <c>IMagazynObiektow</c>, <c>IObrobkaObrazow</c>, <c>IZlecaniePrac</c> oraz warstwy trwałości.
    /// </summary>
    public static IServiceCollection DodajPrzypadkiUzyciaRozwiazan(this IServiceCollection uslugi)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        uslugi.AddScoped<PrzeslijRozwiazanieHandler>();
        return uslugi;
    }
}
