// @Role: Reguła nazw plików: ścieżki wyłącznie z drukowalnych znaków ASCII bez spacji (ADR-075) | @DependsOn: brak | @UsedBy: BrakNiestandardowychZnakowWNazwachPlikowTests | @Task: TASK-002
namespace Lemat.Architecture.Tests.Conventions;

internal static class FileNamePolicy
{
    public static bool IsStandard(string path) =>
        path.Length > 0 && path.All(znak => znak is >= '!' and <= '~');

    public static IReadOnlyList<string> NonStandardChars(string path) =>
        [.. path.Where(znak => znak is < '!' or > '~').Distinct().Select(znak => $"U+{(int)znak:X4}")];
}
