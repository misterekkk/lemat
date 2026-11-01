// @Role: Walidator nagłówka Living Documentation (@Role/@DependsOn/@UsedBy/@Task) w plikach .cs | @DependsOn: System.Text.RegularExpressions | @UsedBy: NaglowekLivingDocumentationTests | @Task: TASK-002
using System.Text.RegularExpressions;

namespace Lemat.Architecture.Tests.Conventions;

internal static partial class LivingDocumentationHeader
{
    /// <summary>Plik generowany nie wymaga nagłówka: *.g.cs, *.g.i.cs, *.Designer.cs, *.AssemblyInfo.cs oraz katalogi obj i bin.</summary>
    public static bool IsExcluded(string path)
    {
        var segmenty = path.Split('/', '\\');
        if (segmenty.Any(segment => segment is "obj" or "bin"))
        {
            return true;
        }

        var nazwa = segmenty[^1];
        return nazwa.EndsWith(".g.cs", StringComparison.Ordinal)
            || nazwa.EndsWith(".g.i.cs", StringComparison.Ordinal)
            || nazwa.EndsWith(".Designer.cs", StringComparison.Ordinal)
            || nazwa.EndsWith(".AssemblyInfo.cs", StringComparison.Ordinal);
    }

    /// <summary>Sprawdza, czy pierwszy wiersz treści pliku (po opcjonalnym BOM) jest poprawnym nagłówkiem.</summary>
    public static bool IsValid(string content)
    {
        var tresc = content.TrimStart('﻿');
        var koniec = tresc.IndexOf('\n', StringComparison.Ordinal);
        var pierwszyWiersz = (koniec < 0 ? tresc : tresc[..koniec]).TrimEnd('\r');
        return HeaderPattern().IsMatch(pierwszyWiersz);
    }

    [GeneratedRegex(@"^// @Role: .+ \| @DependsOn: .+ \| @UsedBy: .+ \| @Task: TASK-\d{3}(, TASK-\d{3})*$")]
    private static partial Regex HeaderPattern();
}
