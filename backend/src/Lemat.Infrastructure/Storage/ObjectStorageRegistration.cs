// @Role: Rejestracja w DI magazynu obiektów (lokalny, U-03) i obróbki obrazów (prosta, U-02) wybieranych konfiguracją Lemat:Magazyn/Obrazy (opcje czytane przy użyciu); ścieżka względna liczona od katalogu głównego aplikacji | @DependsOn: LematOpcje, MagazynObiektowLokalny, ProstaObrobkaObrazow | @UsedBy: Lemat.Api.Configuration.ApiHost | @Task: TASK-127
using Lemat.Application.Common;
using Lemat.Application.Models;
using Lemat.Infrastructure.Sandboxes;
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Infrastructure.Storage;

public static class ObjectStorageRegistration
{
    public static IServiceCollection DodajMagazynIObrobkeObrazow(this IServiceCollection uslugi, string korzenAplikacji)
    {
        ArgumentNullException.ThrowIfNull(uslugi);

        // Opcje czytane przy pierwszym użyciu (ostateczna konfiguracja hosta). Konstruktor adaptera nie dotyka dysku; brak ścieżki
        // kończy się błędem dopiero przy użyciu (start waliduje LematOpcjeWalidator).
        uslugi.AddSingleton(sp =>
        {
            var magazyn = sp.GetRequiredService<LematOpcje>().Magazyn;
            var katalog = string.IsNullOrWhiteSpace(magazyn.Sciezka) ? string.Empty : Path.GetFullPath(magazyn.Sciezka, korzenAplikacji);
            return new MagazynObiektowLokalny(katalog, magazyn.Klucz, sp.GetRequiredService<TimeProvider>());
        });
        uslugi.AddSingleton<IMagazynObiektow>(sp => sp.GetRequiredService<MagazynObiektowLokalny>());
        uslugi.AddSingleton<IOdczytPodpisanegoAdresu>(sp => sp.GetRequiredService<MagazynObiektowLokalny>());
        uslugi.AddSingleton<IObrobkaObrazow, ProstaObrobkaObrazow>();
        return uslugi;
    }
}
