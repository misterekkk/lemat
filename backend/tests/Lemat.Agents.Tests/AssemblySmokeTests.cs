// @Role: Test dymny składu testowanego projektu Lemat.Agents (szkielet, do czasu pojawienia się typów) | @DependsOn: Lemat.Agents | @UsedBy: dotnet test | @Task: TASK-002
using System.Reflection;

namespace Lemat.Agents.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void ZespolProjektuDaSieZaladowac()
    {
        var zespol = Assembly.Load(new AssemblyName("Lemat.Agents"));

        zespol.GetName().Name.ShouldBe("Lemat.Agents");
    }
}
