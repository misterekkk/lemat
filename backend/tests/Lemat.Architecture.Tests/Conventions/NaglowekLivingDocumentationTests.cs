// @Role: Testy nagłówka Living Documentation we wszystkich plikach .cs backendu (z testami negatywnymi na próbkach w pamięci) | @DependsOn: LivingDocumentationHeader, RepositoryLocator | @UsedBy: dotnet test | @Task: TASK-002
using Lemat.Architecture.Tests.Support;

namespace Lemat.Architecture.Tests.Conventions;

public sealed class NaglowekLivingDocumentationTests
{
    private const string PoprawnyNaglowek =
        "// @Role: Opis | @DependsOn: A, B | @UsedBy: C | @Task: TASK-002";

    [Fact]
    public void NaglowekLivingDocumentation()
    {
        var bledne = RepositoryLocator.CSharpFiles()
            .Where(sciezka => !LivingDocumentationHeader.IsExcluded(sciezka))
            .Where(sciezka => !LivingDocumentationHeader.IsValid(File.ReadAllText(sciezka)))
            .Select(RepositoryLocator.RelativeToRoot)
            .ToList();

        bledne.ShouldBeEmpty("Brak poprawnego nagłówka w: " + string.Join(", ", bledne));
    }

    [Theory]
    [InlineData(PoprawnyNaglowek + "\nclass A { }")]
    [InlineData(PoprawnyNaglowek + "\r\nclass A { }")]
    [InlineData("﻿" + PoprawnyNaglowek + "\nclass A { }")]
    [InlineData("// @Role: R | @DependsOn: brak | @UsedBy: brak | @Task: TASK-001, TASK-002, TASK-010")]
    public void AkceptujePoprawnyNaglowek(string zawartosc)
    {
        LivingDocumentationHeader.IsValid(zawartosc).ShouldBeTrue();
    }

    [Theory]
    [InlineData("class A { }")]
    [InlineData("")]
    [InlineData("using System;\n" + PoprawnyNaglowek)]
    [InlineData("// @Role: Opis | @DependsOn: A | @UsedBy: C\nclass A { }")]
    [InlineData("// @Role: Opis | @DependsOn: A | @UsedBy: C | @Task: 002\nclass A { }")]
    [InlineData("// @Role: Opis | @DependsOn: A | @UsedBy: C | @Task: TASK-02\nclass A { }")]
    [InlineData("// @Role: Opis | @DependsOn: A | @UsedBy: C | @Task: TASK-002 TASK-003\nclass A { }")]
    [InlineData("\n" + PoprawnyNaglowek)]
    public void OdrzucaPlikBezPoprawnegoNaglowka(string zawartosc)
    {
        LivingDocumentationHeader.IsValid(zawartosc).ShouldBeFalse();
    }

    [Theory]
    [InlineData("backend/src/Lemat.Api/obj/Debug/net10.0/Lemat.Api.AssemblyInfo.cs")]
    [InlineData("backend/src/Lemat.Api/bin/Debug/Foo.cs")]
    [InlineData("backend/src/Lemat.Infrastructure/Persistence/Foo.g.cs")]
    [InlineData("backend/src/Lemat.Infrastructure/Migrations/20260101_Init.Designer.cs")]
    public void PomijaPlikiGenerowane(string sciezka)
    {
        LivingDocumentationHeader.IsExcluded(sciezka).ShouldBeTrue();
    }

    [Fact]
    public void NieWylaczaZwyklychPlikow()
    {
        LivingDocumentationHeader.IsExcluded("backend/src/Lemat.Api/Program.cs").ShouldBeFalse();
    }
}
