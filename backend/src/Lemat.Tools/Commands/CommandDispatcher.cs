// @Role: Dispatcher poleceń narzędzia Lemat.Tools (--help; polecenia migrate i seed-dev w kolejnych zadaniach) | @DependsOn: brak | @UsedBy: Lemat.Tools.Program, Lemat.Architecture.Tests | @Task: TASK-003
namespace Lemat.Tools.Commands;

/// <summary>Polecenie narzędzia wiersza poleceń.</summary>
public interface IPolecenie
{
    string Nazwa { get; }

    string Opis { get; }

    Task<int> WykonajAsync(string[] argumenty, TextWriter wyjscie, CancellationToken token);
}

/// <summary>Wybiera polecenie po pierwszym argumencie; bez polecenia lub z <c>--help</c> drukuje pomoc.</summary>
public sealed class CommandDispatcher(IEnumerable<IPolecenie> polecenia)
{
    private readonly IReadOnlyList<IPolecenie> _polecenia = [.. polecenia];

    public async Task<int> WykonajAsync(string[] args, TextWriter wyjscie, TextWriter bledy, CancellationToken token = default)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            DrukujPomoc(wyjscie);
            return 0;
        }

        var polecenie = _polecenia.FirstOrDefault(p => string.Equals(p.Nazwa, args[0], StringComparison.Ordinal));
        if (polecenie is null)
        {
            await bledy.WriteLineAsync($"Nieznane polecenie: {args[0]}. Użyj --help.");
            return 2;
        }

        return await polecenie.WykonajAsync(args[1..], wyjscie, token);
    }

    private void DrukujPomoc(TextWriter wyjscie)
    {
        wyjscie.WriteLine("Użycie: Lemat.Tools <polecenie> [opcje]");
        if (_polecenia.Count == 0)
        {
            wyjscie.WriteLine("Brak zarejestrowanych poleceń (szkielet).");
            return;
        }

        foreach (var polecenie in _polecenia)
        {
            wyjscie.WriteLine($"  {polecenie.Nazwa}  {polecenie.Opis}");
        }
    }
}
