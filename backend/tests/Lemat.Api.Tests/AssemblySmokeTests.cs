// @Role: Test dymny składu testowanego projektu Lemat.Api (szkielet, do czasu pojawienia się typów) | @DependsOn: Lemat.Api | @UsedBy: dotnet test | @Task: TASK-002
using System.Reflection;

namespace Lemat.Api.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void ZespolProjektuDaSieZaladowac()
    {
        var zespol = Assembly.Load(new AssemblyName("Lemat.Api"));

        zespol.GetName().Name.ShouldBe("Lemat.Api");
    }
}
