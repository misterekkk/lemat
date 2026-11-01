// @Role: Polecenie `seed-poc` — idempotentnie ładuje dane startowe POC (Działy, Umiejętności, 5 Zadań, 2 konta demo, konfiguracja Mock, Próg pewności); odmawia w Production | @DependsOn: SeederPoc, DaneStartowePoc, HaszowanieHasel, PersistenceRegistration, IPolecenie | @UsedBy: Lemat.Tools.Program, operator, scripts | @Task: TASK-122
using System.Collections;
using Lemat.Application.Accounts;
using Lemat.Application.Common;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Seeding;
using Lemat.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Lemat.Tools.Commands;

/// <summary>
/// <c>Lemat.Tools seed-poc [--polaczenie=&lt;connection string&gt;]</c> (po <c>migrate</c>). Wypisuje konta demo; hasła nie trafiają do logów.
/// Odmawia, gdy <c>LEMAT_SRODOWISKO=Production</c> (kod wyjścia 1).
/// </summary>
public sealed class SeedPocCommand(IDictionary? zmienneSrodowiskowe = null) : IPolecenie
{
    public string Nazwa => "seed-poc";

    public string Opis => "Ładuje dane startowe POC (13 Działów, 5 Zadań, 2 konta demo, konfiguracja Mock); idempotentne, odmawia w Production.";

    public async Task<int> WykonajAsync(string[] argumenty, TextWriter wyjscie, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(argumenty);
        ArgumentNullException.ThrowIfNull(wyjscie);

        var zmienne = zmienneSrodowiskowe ?? Environment.GetEnvironmentVariables();
        var opcje = PolaczenieBazy.Opcje(zmienne);
        if (string.Equals(opcje.Srodowisko, "Production", StringComparison.OrdinalIgnoreCase))
        {
            await wyjscie.WriteLineAsync("seed-poc jest niedozwolone w środowisku Production (dane demo z jawnymi hasłami).");
            return 1;
        }

        opcje.Baza.Polaczenie = PolaczenieBazy.Wybierz(argumenty, zmienne);
        var uslugi = new ServiceCollection();
        uslugi.DodajPersystencje(opcje, RodzajProcesu.Scheduler);
        uslugi.AddSingleton<IHasherHasel, HaszowanieHasel>();
        uslugi.AddScoped<SeederPoc>();

        await using var dostawca = uslugi.BuildServiceProvider();
        await using var zakres = dostawca.CreateAsyncScope();
        var wynik = await zakres.ServiceProvider.GetRequiredService<SeederPoc>().WykonajAsync(DaneStartowePoc.Wczytaj(), token);

        await wyjscie.WriteLineAsync(
            $"seed-poc: dodano Działy {wynik.DodaneDzialy}, Umiejętności {wynik.DodaneUmiejetnosci}, Zadania {wynik.DodaneZadania}, konta {wynik.DodaneKonta}, konfiguracje i kalibrację {wynik.DodaneKonfiguracje}.");
        await wyjscie.WriteLineAsync("Konta demo (tylko lokalne środowisko POC):");
        foreach (var konto in wynik.KontaDemo)
        {
            await wyjscie.WriteLineAsync($"  {konto.Rola}  {konto.Email}  {konto.Haslo}");
        }

        return 0;
    }
}
