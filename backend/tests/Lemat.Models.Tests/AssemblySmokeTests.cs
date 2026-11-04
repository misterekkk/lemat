// @Role: Test dymny składu testowanego projektu Lemat.Models (szkielet, do czasu pojawienia się typów) | @DependsOn: Lemat.Models | @UsedBy: dotnet test | @Task: TASK-002
using System.Reflection;

namespace Lemat.Models.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void ZespolProjektuDaSieZaladowac()
    {
        var zespol = Assembly.Load(new AssemblyName("Lemat.Models"));

        zespol.GetName().Name.ShouldBe("Lemat.Models");
    }
}
