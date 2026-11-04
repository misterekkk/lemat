// @Role: Wczytuje osadzone pliki SQL migracji (Persistence/Migrations/Sql/*.sql) | @DependsOn: zasoby osadzone zespołu Lemat.Infrastructure | @UsedBy: Lemat.Infrastructure.Persistence.Migrations.SchematPoczatkowy, Lemat.Infrastructure.Tests | @Task: TASK-013
using System.Reflection;

namespace Lemat.Infrastructure.Persistence.Migrations;

/// <summary>Dostęp do plików SQL osadzonych w zespole (nazwy logiczne <c>Persistence.Migrations.Sql.&lt;plik&gt;</c>).</summary>
public static class ZasobSql
{
    private const string Przedrostek = "Lemat.Infrastructure.Persistence.Migrations.Sql.";

    /// <summary>Zwraca treść pliku, np. <c>Wczytaj("0000_schema.sql")</c>.</summary>
    public static string Wczytaj(string nazwaPliku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nazwaPliku);
        var zespol = typeof(ZasobSql).Assembly;
        using var strumien = zespol.GetManifestResourceStream(Przedrostek + nazwaPliku)
            ?? throw new InvalidOperationException($"Brak osadzonego pliku SQL '{nazwaPliku}'.");
        using var czytnik = new StreamReader(strumien);
        return czytnik.ReadToEnd();
    }

    /// <summary>Nazwy wszystkich osadzonych plików SQL w kolejności rosnącej.</summary>
    public static IReadOnlyList<string> Nazwy() =>
    [
        .. typeof(ZasobSql).Assembly.GetManifestResourceNames()
            .Where(nazwa => nazwa.StartsWith(Przedrostek, StringComparison.Ordinal))
            .Select(nazwa => nazwa[Przedrostek.Length..])
            .Order(StringComparer.Ordinal),
    ];
}
