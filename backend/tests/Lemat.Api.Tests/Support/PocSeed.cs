// @Role: Ładuje do bazy testowej pełne dane startowe POC (jak polecenie seed-poc: Działy, Umiejętności, 5 Zadań, konta demo) przez SeederPoc; idempotentnie | @DependsOn: SeederPoc, DaneStartowePoc, PersistenceRegistration, HaszowanieHasel | @UsedBy: Content/ExercisesEndpointsTests | @Task: TASK-125
using Lemat.Application.Accounts;
using Lemat.Application.Common;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Seeding;
using Lemat.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Api.Tests.Support;

public static class PocSeed
{
    public static async Task ZapewnijAsync(string polaczenie, CancellationToken ct)
    {
        var opcje = new LematOpcje();
        opcje.Baza.Polaczenie = polaczenie;
        var uslugi = new ServiceCollection();
        uslugi.DodajPersystencje(opcje, RodzajProcesu.Scheduler);

        // Szybkie haszowanie: konta demo nie są tu logowane.
        uslugi.AddSingleton<IHasherHasel>(new HaszowanieHasel(1024, 1, 1));
        uslugi.AddScoped<SeederPoc>();

        await using var dostawca = uslugi.BuildServiceProvider();
        await using var zakres = dostawca.CreateAsyncScope();
        await zakres.ServiceProvider.GetRequiredService<SeederPoc>().WykonajAsync(DaneStartowePoc.Wczytaj(), ct);
    }
}
