// @Role: Polecenie `migrate` — stosuje migracje bazy i tworzy partycje decyzja_modelu na 12 miesięcy naprzód (idempotentne, U-08) | @DependsOn: MigratorBazy, LematKonfiguracja, IPolecenie | @UsedBy: Lemat.Tools.Program, operator, scripts | @Task: TASK-013
using System.Collections;
using Lemat.Application.Common;
using Lemat.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace Lemat.Tools.Commands;

/// <summary>
/// <c>Lemat.Tools migrate [--polaczenie=&lt;connection string&gt;]</c>. Kolejność wyboru połączenia: opcja, <c>LEMAT_BAZA_MIGRACJE</c>,
/// <c>LEMAT_BAZA</c>, domyślna baza deweloperska z docker-compose (port 5433).
/// </summary>
public sealed class MigrujBazeCommand(IDictionary? zmienneSrodowiskowe = null, TimeProvider? zegar = null) : IPolecenie
{
    public const string DomyslnePolaczenieDeweloperskie = "Host=localhost;Port=5433;Database=lemat_dev;Username=lemat;Password=lemat_dev";

    private const string OpcjaPolaczenie = "--polaczenie=";

    public string Nazwa => "migrate";

    public string Opis => "Stosuje migracje bazy (idempotentnie) i tworzy partycje decyzja_modelu na 12 miesięcy naprzód.";

    public async Task<int> WykonajAsync(string[] argumenty, TextWriter wyjscie, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(argumenty);
        ArgumentNullException.ThrowIfNull(wyjscie);

        var polaczenie = argumenty.FirstOrDefault(a => a.StartsWith(OpcjaPolaczenie, StringComparison.Ordinal))?[OpcjaPolaczenie.Length..]
            ?? WybierzPolaczenie();

        var zastosowane = await MigratorBazy.MigrujAsync(polaczenie, zegar ?? TimeProvider.System, token);
        await wyjscie.WriteLineAsync($"Migracje: zastosowano {zastosowane}; partycje decyzja_modelu na {MigratorBazy.MiesiecyNaprzod} miesięcy naprzód.");
        return 0;
    }

    private string WybierzPolaczenie()
    {
        var konfiguracja = new ConfigurationBuilder()
            .AddInMemoryCollection(LematKonfiguracja.ZmienneNaKlucze(zmienneSrodowiskowe ?? Environment.GetEnvironmentVariables()))
            .Build();
        var opcje = new BazaOpcje();
        konfiguracja.GetSection($"{LematOpcje.NazwaSekcji}:Baza").Bind(opcje);
        if (!string.IsNullOrWhiteSpace(opcje.PolaczenieMigracji))
        {
            return opcje.PolaczenieMigracji;
        }

        return string.IsNullOrWhiteSpace(opcje.Polaczenie) ? DomyslnePolaczenieDeweloperskie : opcje.Polaczenie;
    }
}
