// @Role: Definicje reguł warstw 6.5 i ich ewaluacja na grafie zależności (referencje projektów/pakietów/zespołów) oraz na typach (NetArchTest) | @DependsOn: NetArchTest.Rules, System.Xml.Linq | @UsedBy: LayerRulesTests | @Task: TASK-002
using System.Reflection;
using System.Xml.Linq;
using Lemat.Architecture.Tests.Support;
using NetArchTest.Rules;

namespace Lemat.Architecture.Tests.Layers;

internal static class LayerRules
{
    public static readonly IReadOnlyList<string> SourceProjects =
    [
        "Lemat.Domain", "Lemat.Application", "Lemat.Agents", "Lemat.Models", "Lemat.Infrastructure",
        "Lemat.Api", "Lemat.Worker", "Lemat.Scheduler", "Lemat.Tools",
    ];

    private static readonly string[] Hosts = ["Lemat.Api", "Lemat.Worker", "Lemat.Scheduler", "Lemat.Tools"];

    private static readonly string[] ProviderSdks =
        ["OpenAI*", "Azure.AI*", "Anthropic*", "Microsoft.Extensions.AI.OpenAI"];

    private static readonly string[] ProviderSdkNamespaces = ["OpenAI", "Azure.AI", "Anthropic"];

    public static readonly IReadOnlyList<LayerRule> All =
    [
        new(
            "DomenaNiezaleznaOdInfrastruktury",
            ["Lemat.Domain"],
            [
                "Lemat.Application", "Lemat.Agents", "Lemat.Models", "Lemat.Infrastructure", .. Hosts,
                "System.Net.Http*", "Microsoft.EntityFrameworkCore*", "Microsoft.AspNetCore*", "Microsoft.Extensions*",
                "Microsoft.SemanticKernel*", "Npgsql*", .. ProviderSdks,
            ],
            [
                "Lemat.Application", "Lemat.Agents", "Lemat.Models", "Lemat.Infrastructure",
                "System.Net.Http", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Microsoft.Extensions",
                "Microsoft.SemanticKernel", "Npgsql", .. ProviderSdkNamespaces,
            ]),
        new(
            "AplikacjaBezInfrastruktury",
            ["Lemat.Application"],
            [
                "Lemat.Infrastructure", "Lemat.Agents", "Lemat.Models", .. Hosts,
                "Microsoft.EntityFrameworkCore*", "Npgsql*", "Microsoft.AspNetCore*", "Microsoft.SemanticKernel*",
                "Microsoft.Extensions.AI", .. ProviderSdks,
            ],
            [
                "Lemat.Infrastructure", "Lemat.Agents", "Lemat.Models",
                "Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore", "Microsoft.SemanticKernel",
                .. ProviderSdkNamespaces,
            ]),
        new(
            "AgenciTylkoPrzezPorty",
            ["Lemat.Agents"],
            [
                "Lemat.Infrastructure", "Lemat.Models", .. Hosts,
                "Microsoft.EntityFrameworkCore*", "Npgsql*", "Microsoft.AspNetCore*", .. ProviderSdks,
            ],
            [
                "Lemat.Infrastructure", "Lemat.Models",
                "Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore", .. ProviderSdkNamespaces,
            ]),
        new(
            "ModeleBezInfrastruktury",
            ["Lemat.Models"],
            ["Lemat.Infrastructure", "Lemat.Agents", .. Hosts, "Microsoft.EntityFrameworkCore*", "Npgsql*", "Microsoft.AspNetCore*"],
            ["Lemat.Infrastructure", "Lemat.Agents", "Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore"]),
        new(
            "InfrastrukturaBezWarstwWyzszych",
            ["Lemat.Infrastructure"],
            ["Lemat.Agents", "Lemat.Models", .. Hosts, "Microsoft.AspNetCore*", .. ProviderSdks],
            ["Lemat.Agents", "Lemat.Models", "Microsoft.AspNetCore", .. ProviderSdkNamespaces]),
        new(
            "SdkDostawcowTylkoWModelach",
            [.. SourceProjects.Where(projekt => projekt != "Lemat.Models")],
            ProviderSdks,
            ProviderSdkNamespaces),
        new(
            "HostyNieZalezaOdSiebie",
            ["Lemat.Api", "Lemat.Worker", "Lemat.Scheduler"],
            Hosts,
            []),
        new(
            "EndpointyTylkoPrzezAplikacje",
            ["Lemat.Api"],
            [],
            ["Lemat.Infrastructure", "Microsoft.EntityFrameworkCore", "Npgsql"],
            "Lemat.Api.Endpoints"),
    ];

    public static LayerRule ByName(string name) => All.Single(regula => regula.Name == name);

    /// <summary>Zwraca opisy naruszeń reguły na grafie zależności (projekt → nazwy zespołów, pakietów i projektów).</summary>
    public static IReadOnlyList<string> GraphViolations(
        LayerRule rule,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> dependencies)
    {
        var naruszenia = new List<string>();
        foreach (var projekt in rule.Projects)
        {
            if (!dependencies.TryGetValue(projekt, out var zaleznosci))
            {
                continue;
            }

            foreach (var zaleznosc in zaleznosci.Where(z => z != projekt))
            {
                var wzorzec = rule.ForbiddenAssemblies.FirstOrDefault(w => Matches(w, zaleznosc));
                if (wzorzec is not null)
                {
                    naruszenia.Add($"{rule.Name}: {projekt} zależy od {zaleznosc} (zabronione: {wzorzec})");
                }
            }
        }

        return naruszenia;
    }

    /// <summary>Zwraca typy z zespołu, które mają zabronioną zależność z reguły (NetArchTest).</summary>
    public static IReadOnlyList<string> TypeViolations(LayerRule rule, Assembly assembly)
    {
        if (rule.ForbiddenNamespaces.Count == 0)
        {
            return [];
        }

        var typy = Types.InAssembly(assembly);
        var wynik = (rule.NamespaceScope is null
                ? typy.ShouldNot()
                : typy.That().ResideInNamespaceStartingWith(rule.NamespaceScope).ShouldNot())
            .HaveDependencyOnAny([.. rule.ForbiddenNamespaces])
            .GetResult();

        return wynik.IsSuccessful
            ? []
            : [.. (wynik.FailingTypeNames ?? []).Select(typ => $"{rule.Name}: typ {typ} ma zabronioną zależność")];
    }

    /// <summary>Buduje rzeczywisty graf zależności projektów: csproj (referencje projektów i pakiety nieprywatne) oraz referencje zespołów.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> BuildRealGraph()
    {
        var graf = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
        foreach (var projekt in SourceProjects)
        {
            var zaleznosci = new HashSet<string>(ProjectFileDependencies(projekt), StringComparer.Ordinal);
            foreach (var zespol in LoadAssembly(projekt).GetReferencedAssemblies())
            {
                if (zespol.Name is not null)
                {
                    zaleznosci.Add(zespol.Name);
                }
            }

            graf[projekt] = zaleznosci;
        }

        return graf;
    }

    public static Assembly LoadAssembly(string project) => Assembly.Load(new AssemblyName(project));

    /// <summary>Referencje projektów i pakiety NuGet z pliku csproj (pakiety z PrivateAssets=all pomijamy — to analizatory).</summary>
    public static IReadOnlyCollection<string> ProjectFileDependencies(string project)
    {
        var dokument = XDocument.Load(CsprojPath(project));
        var projekty = dokument.Descendants("ProjectReference")
            .Select(ProjectNameFromInclude);
        var pakiety = dokument.Descendants("PackageReference")
            .Where(element => !IsPrivateAssets(element))
            .Select(element => (string)element.Attribute("Include")!);
        return [.. projekty, .. pakiety];
    }

    /// <summary>Nazwa projektu z atrybutu Include referencji (separatory Windows i Unix).</summary>
    public static string ProjectNameFromInclude(XElement projectReference) =>
        Path.GetFileNameWithoutExtension(((string)projectReference.Attribute("Include")!).Replace('\\', '/'));

    public static string CsprojPath(string project) =>
        Path.Combine(RepositoryLocator.BackendDirectory, "src", project, project + ".csproj");

    public static bool IsPrivateAssets(XElement packageReference) =>
        string.Equals((string?)packageReference.Attribute("PrivateAssets"), "all", StringComparison.OrdinalIgnoreCase);

    public static bool Matches(string pattern, string name) =>
        pattern.EndsWith('*')
            ? name.StartsWith(pattern[..^1], StringComparison.Ordinal)
            : string.Equals(pattern, name, StringComparison.Ordinal);
}
