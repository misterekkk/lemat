// @Role: Testy reguł warstw 6.5 na realnych projektach oraz testy negatywne na syntetycznych przykładach | @DependsOn: LayerRules, NetArchTest.Rules | @UsedBy: dotnet test | @Task: TASK-002
using System.Xml.Linq;
using Lemat.Architecture.Tests.Layers.Synthetic;
using Lemat.Architecture.Tests.Support;

namespace Lemat.Architecture.Tests.Layers;

public sealed class LayerRulesTests
{
    private static readonly Dictionary<string, string[]> ExpectedProjectReferences = new()
    {
        ["Lemat.Domain"] = [],
        ["Lemat.Application"] = ["Lemat.Domain"],
        ["Lemat.Agents"] = ["Lemat.Application", "Lemat.Domain"],
        ["Lemat.Models"] = ["Lemat.Application", "Lemat.Domain"],
        ["Lemat.Infrastructure"] = ["Lemat.Application", "Lemat.Domain"],
        ["Lemat.Api"] = ["Lemat.Agents", "Lemat.Application", "Lemat.Infrastructure", "Lemat.Models"],
        ["Lemat.Worker"] = ["Lemat.Agents", "Lemat.Application", "Lemat.Infrastructure", "Lemat.Models"],
        ["Lemat.Scheduler"] = ["Lemat.Application", "Lemat.Infrastructure"],
        ["Lemat.Tools"] = ["Lemat.Application", "Lemat.Infrastructure"],
    };

    public static TheoryData<string> RuleNames()
    {
        var dane = new TheoryData<string>();
        foreach (var regula in LayerRules.All.Where(r => r.ForbiddenAssemblies.Count > 0))
        {
            dane.Add(regula.Name);
        }

        return dane;
    }

    public static TheoryData<string> RealRuleNames()
    {
        var dane = new TheoryData<string>();
        foreach (var regula in LayerRules.All)
        {
            dane.Add(regula.Name);
        }

        return dane;
    }

    [Theory]
    [MemberData(nameof(RealRuleNames))]
    public void RegulaWarstwyJestSpelnionaWRealnymKodzie(string nazwaReguly)
    {
        var regula = LayerRules.ByName(nazwaReguly);
        var graf = LayerRules.BuildRealGraph();

        var naruszenia = new List<string>(LayerRules.GraphViolations(regula, graf));
        foreach (var projekt in regula.Projects)
        {
            naruszenia.AddRange(LayerRules.TypeViolations(regula, LayerRules.LoadAssembly(projekt)));
        }

        naruszenia.ShouldBeEmpty(string.Join(Environment.NewLine, naruszenia));
    }

    [Theory]
    [MemberData(nameof(RuleNames))]
    public void RegulaWarstwyWykrywaNaruszenieNaSyntetycznymGrafie(string nazwaReguly)
    {
        var regula = LayerRules.ByName(nazwaReguly);
        var wzorzec = regula.ForbiddenAssemblies.First(w => w != regula.Projects[0]);
        var zabroniona = wzorzec.EndsWith('*') ? wzorzec.TrimEnd('*') + ".Cos" : wzorzec;
        var graf = new Dictionary<string, IReadOnlyCollection<string>>
        {
            [regula.Projects[0]] = ["System.Runtime", zabroniona],
        };

        var naruszenia = LayerRules.GraphViolations(regula, graf);

        naruszenia.Count.ShouldBe(1);
        naruszenia[0].ShouldContain(regula.Projects[0]);
    }

    [Theory]
    [MemberData(nameof(RuleNames))]
    public void RegulaWarstwyAkceptujeDozwolonaZaleznoscNaSyntetycznymGrafie(string nazwaReguly)
    {
        var regula = LayerRules.ByName(nazwaReguly);
        var graf = new Dictionary<string, IReadOnlyCollection<string>>
        {
            [regula.Projects[0]] = ["System.Runtime", "System.Collections"],
        };

        LayerRules.GraphViolations(regula, graf).ShouldBeEmpty();
    }

    [Fact]
    public void AplikacjaMozeUzywacAbstrakcjiAiAleNieImplementacji()
    {
        var regula = LayerRules.ByName("AplikacjaBezInfrastruktury");
        var abstrakcje = new Dictionary<string, IReadOnlyCollection<string>> { ["Lemat.Application"] = ["Microsoft.Extensions.AI.Abstractions"] };
        var implementacja = new Dictionary<string, IReadOnlyCollection<string>> { ["Lemat.Application"] = ["Microsoft.Extensions.AI"] };

        LayerRules.GraphViolations(regula, abstrakcje).ShouldBeEmpty();
        LayerRules.GraphViolations(regula, implementacja).Count.ShouldBe(1);
    }

    [Fact]
    public void SdkDostawcowWModelachJestDozwolone()
    {
        var regula = LayerRules.ByName("SdkDostawcowTylkoWModelach");
        regula.Projects.ShouldNotContain("Lemat.Models");

        var graf = new Dictionary<string, IReadOnlyCollection<string>> { ["Lemat.Models"] = ["Azure.AI.OpenAI", "OpenAI"] };

        LayerRules.GraphViolations(regula, graf).ShouldBeEmpty();
    }

    [Fact]
    public void RegulaTypowWykrywaZaleznoscNaSyntetycznymTypie()
    {
        var regula = new LayerRule(
            "SyntetycznaDomena",
            ["Lemat.Architecture.Tests"],
            [],
            ["System.Net.Http"],
            typeof(SyntheticDomainType).Namespace);

        var naruszenia = LayerRules.TypeViolations(regula, typeof(SyntheticDomainType).Assembly);

        naruszenia.ShouldHaveSingleItem().ShouldContain(nameof(SyntheticDomainType));
    }

    [Fact]
    public void RegulaTypowNieZglaszaTypowPozaZakresem()
    {
        var regula = new LayerRule(
            "SyntetycznaDomena",
            ["Lemat.Architecture.Tests"],
            [],
            ["System.Net.Http"],
            "Lemat.Architecture.Tests.Layers.NieMaTakiejPrzestrzeni");

        LayerRules.TypeViolations(regula, typeof(SyntheticDomainType).Assembly).ShouldBeEmpty();
    }

    [Fact]
    public void ProjektDomenyBezPakietow()
    {
        var dokument = XDocument.Load(LayerRules.CsprojPath("Lemat.Domain"));
        dokument.Descendants("PackageReference").ShouldBeEmpty();
        dokument.Descendants("ProjectReference").ShouldBeEmpty();

        // Jedyne pakiety widoczne w grafie to analizatory dodawane z backend/src/Directory.Build.props z PrivateAssets=all.
        var wspolne = XDocument.Load(Path.Combine(RepositoryLocator.BackendDirectory, "src", "Directory.Build.props"));
        wspolne.Descendants("PackageReference")
            .Where(pakiet => !LayerRules.IsPrivateAssets(pakiet))
            .ShouldBeEmpty();
    }

    [Fact]
    public void ProjektDomenyBezPakietowWykrywaPakietNieprywatny()
    {
        var pakiet = XElement.Parse("<PackageReference Include=\"Npgsql\" />");
        var analizator = XElement.Parse("<PackageReference Include=\"X\" PrivateAssets=\"all\" />");

        LayerRules.IsPrivateAssets(pakiet).ShouldBeFalse();
        LayerRules.IsPrivateAssets(analizator).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Lemat.Domain")]
    [InlineData("Lemat.Application")]
    [InlineData("Lemat.Agents")]
    [InlineData("Lemat.Models")]
    [InlineData("Lemat.Infrastructure")]
    [InlineData("Lemat.Api")]
    [InlineData("Lemat.Worker")]
    [InlineData("Lemat.Scheduler")]
    [InlineData("Lemat.Tools")]
    public void ReferencjeProjektowZgodneZGrafem(string projekt)
    {
        var rzeczywiste = XDocument.Load(LayerRules.CsprojPath(projekt))
            .Descendants("ProjectReference")
            .Select(LayerRules.ProjectNameFromInclude)
            .Order(StringComparer.Ordinal)
            .ToArray();

        rzeczywiste.ShouldBe(ExpectedProjectReferences[projekt]);
    }
}
