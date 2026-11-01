// @Role: Wyjątki domenowe: baza oraz naruszenie niezmiennika z kodem błędu 12.2 (mapowany na ProblemDetails) | @DependsOn: — | @UsedBy: agregaty domeny, TlumaczBledowBazy (TASK-015), Lemat.Api | @Task: TASK-014
namespace Lemat.Domain.Common;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710", Justification = "Nazwa WyjatekDomenowy wynika z TASK-014 i słownika pojęć.")]
public abstract class WyjatekDomenowy(string kodBledu, string opis) : Exception(opis)
{
    public string KodBledu { get; } = kodBledu;
}

public sealed class NiezmiennikNaruszonyException : WyjatekDomenowy
{
    public NiezmiennikNaruszonyException(string kodBledu, string? opis = null)
        : base(Waliduj(kodBledu), opis ?? kodBledu)
    {
    }

    private static string Waliduj(string kodBledu)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kodBledu);
        return kodBledu;
    }
}
