// @Role: Lokalizacja katalogów repozytorium i plików źródłowych dla testów architektury | @DependsOn: system plików, git | @UsedBy: Layers, Glossary, Conventions | @Task: TASK-002
using System.Diagnostics;

namespace Lemat.Architecture.Tests.Support;

internal static class RepositoryLocator
{
    public static string BackendDirectory { get; } = FindBackendDirectory();

    public static string RepositoryRoot => Path.GetDirectoryName(BackendDirectory)!;

    /// <summary>Pliki .cs w backend/src i backend/tests z pominięciem katalogów obj i bin.</summary>
    public static IReadOnlyList<string> CSharpFiles() =>
    [
        .. Directory.EnumerateFiles(BackendDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(sciezka => !IsInBuildOutput(sciezka))
            .OrderBy(sciezka => sciezka, StringComparer.Ordinal),
    ];

    public static bool IsInBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "obj" or "bin");

    public static string RelativeToRoot(string path) =>
        Path.GetRelativePath(RepositoryRoot, path).Replace('\\', '/');

    /// <summary>Ścieżki plików śledzonych oraz nieśledzonych i nieignorowanych przez git (względem korzenia repozytorium).</summary>
    public static IReadOnlyList<string> GitFiles()
    {
        var info = new ProcessStartInfo("git")
        {
            WorkingDirectory = RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[] { "ls-files", "-z", "--cached", "--others", "--exclude-standard" })
        {
            info.ArgumentList.Add(argument);
        }

        using var proces = Process.Start(info) ?? throw new InvalidOperationException("Nie udało się uruchomić git.");
        var wyjscie = proces.StandardOutput.ReadToEnd();
        var bledy = proces.StandardError.ReadToEnd();
        proces.WaitForExit();
        if (proces.ExitCode != 0)
        {
            throw new InvalidOperationException($"git ls-files zakończył się kodem {proces.ExitCode}: {bledy}");
        }

        return [.. wyjscie.Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal)];
    }

    private static string FindBackendDirectory()
    {
        var katalog = new DirectoryInfo(AppContext.BaseDirectory);
        while (katalog is not null)
        {
            if (File.Exists(Path.Combine(katalog.FullName, "Lemat.slnx")))
            {
                return katalog.FullName;
            }

            katalog = katalog.Parent;
        }

        throw new InvalidOperationException("Nie znaleziono katalogu backend (plik Lemat.slnx) powyżej katalogu testów.");
    }
}
