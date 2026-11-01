// @Role: Wybór połączenia z bazą dla poleceń narzędzia: opcja --polaczenie=, potem LEMAT_BAZA_MIGRACJE, LEMAT_BAZA, domyślna baza deweloperska | @DependsOn: LematKonfiguracja, LematOpcje, BazaOpcje | @UsedBy: MigrujBazeCommand, SeedPocCommand | @Task: TASK-013, TASK-122
using System.Collections;
using Lemat.Application.Common;
using Microsoft.Extensions.Configuration;

namespace Lemat.Tools.Commands;

internal static class PolaczenieBazy
{
    public const string DomyslnePolaczenieDeweloperskie = "Host=localhost;Port=5433;Database=lemat_dev;Username=lemat;Password=lemat_dev";

    private const string OpcjaPolaczenie = "--polaczenie=";

    public static string Wybierz(string[] argumenty, IDictionary zmienneSrodowiskowe)
    {
        var zOpcji = argumenty.FirstOrDefault(a => a.StartsWith(OpcjaPolaczenie, StringComparison.Ordinal))?[OpcjaPolaczenie.Length..];
        if (!string.IsNullOrWhiteSpace(zOpcji))
        {
            return zOpcji;
        }

        var opcje = Opcje(zmienneSrodowiskowe);
        if (!string.IsNullOrWhiteSpace(opcje.Baza.PolaczenieMigracji))
        {
            return opcje.Baza.PolaczenieMigracji;
        }

        return string.IsNullOrWhiteSpace(opcje.Baza.Polaczenie) ? DomyslnePolaczenieDeweloperskie : opcje.Baza.Polaczenie;
    }

    public static LematOpcje Opcje(IDictionary zmienneSrodowiskowe)
    {
        var konfiguracja = new ConfigurationBuilder()
            .AddInMemoryCollection(LematKonfiguracja.ZmienneNaKlucze(zmienneSrodowiskowe))
            .Build();
        var opcje = new LematOpcje();
        konfiguracja.GetSection(LematOpcje.NazwaSekcji).Bind(opcje);
        return opcje;
    }
}
