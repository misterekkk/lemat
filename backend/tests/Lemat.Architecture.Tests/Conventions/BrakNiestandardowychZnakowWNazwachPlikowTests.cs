// @Role: Test braku niestandardowych znaków w ścieżkach plików repozytorium (git ls-files, ADR-075) | @DependsOn: FileNamePolicy, RepositoryLocator | @UsedBy: dotnet test | @Task: TASK-002
using Lemat.Architecture.Tests.Support;

namespace Lemat.Architecture.Tests.Conventions;

public sealed class BrakNiestandardowychZnakowWNazwachPlikowTests
{
    [Fact]
    public void BrakNiestandardowychZnakowWNazwachPlikow()
    {
        var pliki = RepositoryLocator.GitFiles();

        pliki.ShouldNotBeEmpty();
        var bledne = pliki.Where(sciezka => !FileNamePolicy.IsStandard(sciezka)).ToList();
        bledne.ShouldBeEmpty("Niestandardowe znaki w ścieżkach: " + string.Join(", ", bledne));
    }

    [Theory]
    [InlineData("backend/src/Lemat.Api/Program.cs")]
    [InlineData("frontend/src/app/(student)/exercises/[exerciseId]/page.tsx")]
    [InlineData("docs/adr/ADR-075.md")]
    public void AkceptujeSciezkiAscii(string sciezka)
    {
        FileNamePolicy.IsStandard(sciezka).ShouldBeTrue();
    }

    [Theory]
    [InlineData("docs/zażółć.md")]
    [InlineData("backend/src/Uczeń.cs")]
    [InlineData("docs/plik ze spacją.md")]
    [InlineData("docs/tab\tplik.md")]
    [InlineData("")]
    public void OdrzucaSciezkiZNiestandardowymiZnakami(string sciezka)
    {
        FileNamePolicy.IsStandard(sciezka).ShouldBeFalse();
    }

    [Fact]
    public void WskazujeNiestandardowyZnak()
    {
        FileNamePolicy.NonStandardChars("a/ż.md").ShouldBe(["U+017C"]);
    }
}
