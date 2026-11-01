// @Role: Deterministyczny skrót FNV-1a 32-bit (fnv1a) nad bajtami identyfikatora UUID w kolejności sieciowej; podstawa przydziału do grupy eksperymentu i Kontroli losowej | @DependsOn: — | @UsedBy: Lemat.Domain.Accounts.GrupaEksperymentu, reguły trasowania T-5 (TASK-021) | @Task: TASK-016
namespace Lemat.Domain.Common;

public static class Fnv1a
{
    private const uint PodstawaOffset = 2166136261;
    private const uint Liczba = 16777619;

    public static uint Skrot32(ReadOnlySpan<byte> bajty)
    {
        var skrot = PodstawaOffset;
        foreach (var bajt in bajty)
        {
            skrot ^= bajt;
            skrot *= Liczba;
        }

        return skrot;
    }

    /// <summary>Skrót 16 bajtów UUID w kolejności RFC 9562 (jak w zapisie tekstowym, big-endian).</summary>
    public static uint Skrot32(Guid identyfikator)
    {
        Span<byte> bajty = stackalloc byte[16];
        identyfikator.TryWriteBytes(bajty, bigEndian: true, out _);
        return Skrot32(bajty);
    }
}
