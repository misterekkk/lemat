// @Role: Przezroczysta jednostka pracy do testów jednostkowych fasad: wykonuje przypadek użycia bez transakcji i zapisu (repozytoria są atrapami) | @DependsOn: Lemat.Application.Common.IJednostkaPracy | @UsedBy: testy fasad Klasy, Modele, Kalibracja | @Task: TASK-018
using Lemat.Application.Common;

namespace Lemat.Application.Tests.Support;

public sealed class PrzezroczystaJednostkaPracy : IJednostkaPracy
{
    public int LiczbaWykonan { get; private set; }

    public bool TransakcjaOtwarta => false;

    public async Task<T> WykonajAsync<T>(Func<CancellationToken, Task<T>> przypadekUzycia, CancellationToken ct)
    {
        LiczbaWykonan++;
        return await przypadekUzycia(ct);
    }

    public async Task WykonajAsync(Func<CancellationToken, Task> przypadekUzycia, CancellationToken ct)
    {
        LiczbaWykonan++;
        await przypadekUzycia(ct);
    }

    public void WymagajTransakcji()
    {
    }
}
