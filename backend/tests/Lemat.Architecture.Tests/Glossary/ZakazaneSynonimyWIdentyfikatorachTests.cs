// @Role: Test strażnika słownika: brak zakazanych synonimów w identyfikatorach backend/src i backend/tests (wersja lekka 3.4) | @DependsOn: ForbiddenSynonymAnalyzer, RepositoryLocator | @UsedBy: dotnet test | @Task: TASK-002, TASK-018
using Lemat.Architecture.Tests.Support;

namespace Lemat.Architecture.Tests.Glossary;

public sealed class ZakazaneSynonimyWIdentyfikatorachTests
{
    [Fact]
    public void ZakazaneSynonimyWIdentyfikatorach()
    {
        var naruszenia = RepositoryLocator.CSharpFiles()
            .Where(sciezka => !sciezka.EndsWith(".g.cs", StringComparison.Ordinal))
            .SelectMany(sciezka => ForbiddenSynonymAnalyzer.Analyze(File.ReadAllText(sciezka), RepositoryLocator.RelativeToRoot(sciezka)))
            .Select(n => $"{n.Path}:{n.Line} identyfikator '{n.Identifier}' zawiera zakazany token '{n.Token}'")
            .ToList();

        naruszenia.ShouldBeEmpty(string.Join(Environment.NewLine, naruszenia));
    }

    [Theory]
    [InlineData("class Student { }", "Student", "student")]
    [InlineData("class A { int grade; }", "grade", "grade")]
    [InlineData("class A { void M(int taskCount) { } }", "taskCount", "task")]
    [InlineData("class A { int ComputeScore() => 1; }", "ComputeScore", "score")]
    [InlineData("class A { string DifficultyLevel { get; } }", "DifficultyLevel", "difficulty")]
    [InlineData("enum E { Topic }", "Topic", "topic")]
    [InlineData("namespace Lemat.Skill.Core { }", "Skill", "skill")]
    [InlineData("class A { void M() { var lesson_plan = 1; } }", "lesson_plan", "lesson")]
    [InlineData("class A { void M() { var reviews = 1; } }", "reviews", "review")]
    [InlineData("class SubmissionHandler { }", "SubmissionHandler", "submission")]
    public void WykrywaZakazanySynonimWDeklaracji(string kod, string identyfikator, string token)
    {
        var naruszenia = ForbiddenSynonymAnalyzer.Analyze(kod, "probka.cs");

        naruszenia.ShouldContain(n => n.Identifier == identyfikator && n.Token == token);
    }

    [Theory]
    [InlineData("class Ocena { }")]
    [InlineData("class Uczen { int PunktyZaOcene; }")]
    [InlineData("enum PytanieJev { SCORE, RANKING }")]
    [InlineData("class A { const int SCORE = 1; }")]
    [InlineData("class A { Task M() => Task.CompletedTask; }")]
    [InlineData("class A { string Tekst = \"student grade score\"; }")]
    [InlineData("// student grade\nclass Zadanie { }")]
    [InlineData("class Studentka { }")]
    [InlineData("class Scoreboard { }")]
    public void NieZglaszaPoprawnychIdentyfikatorowIUzyc(string kod)
    {
        ForbiddenSynonymAnalyzer.Analyze(kod, "probka.cs").ShouldBeEmpty();
    }

    [Fact]
    public void WielkoscLiterWPozostalychPrzypadkachTokenJestZakazany()
    {
        ForbiddenSynonymAnalyzer.Analyze("enum E { Score }", "probka.cs").ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("enum TypPytania { Noul, Score, Choice }")]
    [InlineData("record PytanieScore { }")]
    public void DopuszczaTypPytaniaJevSkali(string kod)
    {
        ForbiddenSynonymAnalyzer.Analyze(kod, "probka.cs").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("StudentGrade", new[] { "student", "grade" })]
    [InlineData("task_id", new[] { "task", "id" })]
    [InlineData("HTTPClient", new[] { "http", "client" })]
    [InlineData("OcenaKryterium2", new[] { "ocena", "kryterium", "2" })]
    public void DzieliTokenyNaGranicachWielkosciLiterIPodkreslnikow(string identyfikator, string[] oczekiwane)
    {
        ForbiddenSynonymAnalyzer.SplitTokens(identyfikator).ShouldBe(oczekiwane);
    }
}
