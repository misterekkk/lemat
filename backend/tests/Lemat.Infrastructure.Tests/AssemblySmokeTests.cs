// @Role: Test dymny składu testowanego projektu Lemat.Infrastructure (szkielet, do czasu pojawienia się typów) | @DependsOn: Lemat.Infrastructure | @UsedBy: dotnet test | @Task: TASK-002
using System.Reflection;

namespace Lemat.Infrastructure.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void ZespolProjektuDaSieZaladowac()
    {
        var zespol = Assembly.Load(new AssemblyName("Lemat.Infrastructure"));

        zespol.GetName().Name.ShouldBe("Lemat.Infrastructure");
    }
}
