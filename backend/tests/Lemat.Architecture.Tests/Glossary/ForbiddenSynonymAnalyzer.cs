// @Role: Analizator Roslyn deklaracji identyfikatorów pod kątem zakazanych synonimów ze słownika (3.1, 3.4) | @DependsOn: Microsoft.CodeAnalysis.CSharp | @UsedBy: ZakazaneSynonimyWIdentyfikatorachTests | @Task: TASK-002
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lemat.Architecture.Tests.Glossary;

internal sealed record SynonymViolation(string Path, int Line, string Identifier, string Token);

internal static partial class ForbiddenSynonymAnalyzer
{
    public static readonly IReadOnlyList<string> ForbiddenTokens =
        ["student", "task", "submission", "grade", "score", "difficulty", "topic", "skill", "lesson", "review"];

    /// <summary>Dzieli identyfikator na tokeny na granicach wielkości liter, podkreślników i cyfr; tokeny zwraca małymi literami.</summary>
    public static IReadOnlyList<string> SplitTokens(string identifier) =>
        [.. TokenPattern().Matches(identifier).Select(dopasowanie => dopasowanie.Value.ToLowerInvariant())];

    /// <summary>Zwraca naruszenia w deklaracjach identyfikatorów (typy, składowe, parametry, zmienne lokalne, przestrzenie nazw); użycia nie są sprawdzane.</summary>
    public static IReadOnlyList<SynonymViolation> Analyze(string sourceText, string path)
    {
        var drzewo = CSharpSyntaxTree.ParseText(sourceText);
        var naruszenia = new List<SynonymViolation>();
        foreach (var token in drzewo.GetRoot().DescendantTokens())
        {
            if (!token.IsKind(SyntaxKind.IdentifierToken) || !IsDeclaration(token))
            {
                continue;
            }

            var identyfikator = token.ValueText;
            foreach (var zakazany in FindForbiddenTokens(identyfikator))
            {
                var linia = drzewo.GetLineSpan(token.Span).StartLinePosition.Line + 1;
                naruszenia.Add(new SynonymViolation(path, linia, identyfikator, zakazany));
            }
        }

        return naruszenia;
    }

    public static IReadOnlyList<string> FindForbiddenTokens(string identifier)
    {
        // Wyjątek: wartość wyliczeniowa/stała pytania Jev SCORE (UPPER_SNAKE).
        var wielkieLitery = !identifier.Any(char.IsLower);
        return [.. SplitTokens(identifier)
            .Select(token => ForbiddenTokens.FirstOrDefault(zakazany => token == zakazany || token == zakazany + "s"))
            .OfType<string>()
            .Where(zakazany => !(wielkieLitery && zakazany == "score"))
            .Distinct(StringComparer.Ordinal)];
    }

    private static bool IsDeclaration(SyntaxToken token) => token.Parent switch
    {
        BaseTypeDeclarationSyntax deklaracja => deklaracja.Identifier == token,
        DelegateDeclarationSyntax deklaracja => deklaracja.Identifier == token,
        MethodDeclarationSyntax deklaracja => deklaracja.Identifier == token,
        LocalFunctionStatementSyntax deklaracja => deklaracja.Identifier == token,
        PropertyDeclarationSyntax deklaracja => deklaracja.Identifier == token,
        EventDeclarationSyntax deklaracja => deklaracja.Identifier == token,
        VariableDeclaratorSyntax deklaracja => deklaracja.Identifier == token,
        ParameterSyntax deklaracja => deklaracja.Identifier == token,
        TypeParameterSyntax deklaracja => deklaracja.Identifier == token,
        EnumMemberDeclarationSyntax deklaracja => deklaracja.Identifier == token,
        SingleVariableDesignationSyntax deklaracja => deklaracja.Identifier == token,
        ForEachStatementSyntax deklaracja => deklaracja.Identifier == token,
        CatchDeclarationSyntax deklaracja => deklaracja.Identifier == token,
        TupleElementSyntax deklaracja => deklaracja.Identifier == token,
        IdentifierNameSyntax nazwa => IsNamespaceSegment(nazwa),
        _ => false,
    };

    private static bool IsNamespaceSegment(IdentifierNameSyntax name)
    {
        var przestrzen = name.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return przestrzen is not null && przestrzen.Name.Span.Contains(name.Span);
    }

    [GeneratedRegex("[A-Z]+(?![a-z])|[A-Z]?[a-z]+|[0-9]+")]
    private static partial Regex TokenPattern();
}
