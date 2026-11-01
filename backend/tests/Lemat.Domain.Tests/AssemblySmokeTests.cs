// @Role: Test dymny składu testowanego projektu Lemat.Domain (szkielet, do czasu pojawienia się typów) | @DependsOn: Lemat.Domain | @UsedBy: dotnet test | @Task: TASK-002
using System.Reflection;

namespace Lemat.Domain.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void ZespolProjektuDaSieZaladowac()
    {
        var zespol = Assembly.Load(new AssemblyName("Lemat.Domain"));

        zespol.GetName().Name.ShouldBe("Lemat.Domain");
    }
}
