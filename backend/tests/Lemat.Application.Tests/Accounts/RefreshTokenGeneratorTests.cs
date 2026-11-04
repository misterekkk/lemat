// @Role: Testy generatora tokenów odświeżania: losowość, Base64Url bez znaków specjalnych, deterministyczny skrót SHA-256 | @DependsOn: GeneratorTokenowOdswiezania | @UsedBy: dotnet test | @Task: TASK-123
using Lemat.Application.Accounts;

namespace Lemat.Application.Tests.Accounts;

public sealed class RefreshTokenGeneratorTests
{
    [Fact]
    public void TokenJestLosowyIBezZnakowSpecjalnychUrl()
    {
        var tokeny = Enumerable.Range(0, 50).Select(_ => GeneratorTokenowOdswiezania.Nowy()).ToList();

        tokeny.Distinct().Count().ShouldBe(50);
        tokeny.ShouldAllBe(t => t.Length == 43 && t.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_'));
    }

    [Fact]
    public void SkrotMa32BajtyIJestDeterministyczny()
    {
        var token = GeneratorTokenowOdswiezania.Nowy();

        GeneratorTokenowOdswiezania.Skrot(token).Length.ShouldBe(32);
        GeneratorTokenowOdswiezania.Skrot(token).ShouldBe(GeneratorTokenowOdswiezania.Skrot(token));
        GeneratorTokenowOdswiezania.Skrot(token).ShouldNotBe(GeneratorTokenowOdswiezania.Skrot(GeneratorTokenowOdswiezania.Nowy()));
    }
}
