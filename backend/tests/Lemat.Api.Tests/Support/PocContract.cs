// @Role: Parser tabel 7.2 (Transkrypcje) i 7.3 (Oceny) z docs/POC_CONTRACT.md — niezależne od pliku danych Mocka źródło oczekiwanych wartości testów potoku | @DependsOn: docs/POC_CONTRACT.md | @UsedBy: Solutions/SolutionsFlowTests | @Task: TASK-128
using System.Globalization;
using System.Text.RegularExpressions;

namespace Lemat.Api.Tests.Support;

public sealed record KontraktKrok(int Nr, string Tekst, string Latex, decimal Pewnosc);

public sealed record KontraktOcena(int Punkty, int PunktyMax, int[] PunktyKryteriow, int? PierwszyBlednyKrok, string? TypBledu, string? Wskazowka);

public static partial class KontraktPoc
{
    private static readonly Lazy<(Dictionary<int, List<KontraktKrok>> Kroki, Dictionary<int, KontraktOcena> Oceny)> Dane = new(Wczytaj);

    public static IReadOnlyList<KontraktKrok> Kroki(int zadanie) => Dane.Value.Kroki[zadanie];

    public static KontraktOcena Ocena(int zadanie) => Dane.Value.Oceny[zadanie];

    private static (Dictionary<int, List<KontraktKrok>>, Dictionary<int, KontraktOcena>) Wczytaj()
    {
        var katalog = new DirectoryInfo(AppContext.BaseDirectory);
        while (katalog is not null && !File.Exists(Path.Combine(katalog.FullName, "docs", "POC_CONTRACT.md")))
        {
            katalog = katalog.Parent;
        }

        var sciezka = Path.Combine(katalog?.FullName ?? throw new InvalidOperationException("Nie znaleziono docs/POC_CONTRACT.md."), "docs", "POC_CONTRACT.md");
        var kroki = new Dictionary<int, List<KontraktKrok>>();
        var oceny = new Dictionary<int, KontraktOcena>();
        foreach (var linia in File.ReadAllLines(sciezka))
        {
            if (WierszKroku().Match(linia) is { Success: true } k)
            {
                var zadanie = int.Parse(k.Groups[1].Value, CultureInfo.InvariantCulture);
                if (!kroki.TryGetValue(zadanie, out var lista))
                {
                    kroki[zadanie] = lista = [];
                }

                lista.Add(new KontraktKrok(
                    int.Parse(k.Groups[2].Value, CultureInfo.InvariantCulture),
                    k.Groups[3].Value,
                    k.Groups[4].Value,
                    decimal.Parse(k.Groups[5].Value, CultureInfo.InvariantCulture)));
            }
            else if (WierszOceny().Match(linia) is { Success: true } o)
            {
                var zadanie = int.Parse(o.Groups[1].Value, CultureInfo.InvariantCulture);
                oceny[zadanie] = new KontraktOcena(
                    int.Parse(o.Groups[2].Value, CultureInfo.InvariantCulture),
                    int.Parse(o.Groups[3].Value, CultureInfo.InvariantCulture),
                    [.. o.Groups[4].Value.Split(',').Select(p => int.Parse(p.Trim(), CultureInfo.InvariantCulture))],
                    o.Groups[5].Value == "`null`" ? null : int.Parse(o.Groups[5].Value, CultureInfo.InvariantCulture),
                    o.Groups[6].Value == "`null`" ? null : o.Groups[6].Value.Trim('`'),
                    o.Groups[7].Value == "`null`" ? null : o.Groups[7].Value.Trim('„', '”'));
            }
        }

        return (kroki, oceny);
    }

    [GeneratedRegex(@"^\| (\d) \| (\d) \| (.+?) \| `(.+)` \| (0\.\d+) \|$")]
    private static partial Regex WierszKroku();

    [GeneratedRegex(@"^\| (\d) \| (\d) / (\d) \| ([\d, ]+) \| (`null`|\d) \| (`null`|`[A-Z_]+`) \| (`null`|„.+”) \|$")]
    private static partial Regex WierszOceny();
}
