// @Role: Test dymny składu testowanego projektu Lemat.Application (szkielet, do czasu pojawienia się typów) | @DependsOn: Lemat.Application | @UsedBy: dotnet test | @Task: TASK-002
using System.Reflection;

namespace Lemat.Application.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void ZespolProjektuDaSieZaladowac()
    {
        var zespol = Assembly.Load(new AssemblyName("Lemat.Application"));

        zespol.GetName().Name.ShouldBe("Lemat.Application");
    }
}
