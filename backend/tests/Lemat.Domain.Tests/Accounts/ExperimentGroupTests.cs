// @Role: Testy Grupy eksperymentu: determinizm i wektory fnv1a(uzytkownik_id) mod 100, zakres 0-99 | @DependsOn: Lemat.Domain.Accounts.GrupaEksperymentu | @UsedBy: dotnet test | @Task: TASK-016
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Tests.Accounts;

public sealed class ExperimentGroupTests
{
    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", 65)]
    [InlineData("01234567-89ab-7def-8123-456789abcdef", 41)]
    [InlineData("018f3b5e-7c1a-7a2b-9c3d-0123456789ab", 18)]
    [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff", 37)]
    [InlineData("11111111-2222-7333-8444-555555555555", 9)]
    public void GrupaJestDeterministycznaIZgodnaZWektoramiFnv1a(string uuid, int oczekiwana)
    {
        var id = new UzytkownikId(Guid.Parse(uuid));

        GrupaEksperymentu.Dla(id).ProcentGrupyEksperymentu.ShouldBe(oczekiwana);
        GrupaEksperymentu.Dla(id).ShouldBe(GrupaEksperymentu.Dla(id));
    }

    [Fact]
    public void GrupaZawszeMiesciSieW0Do99()
    {
        for (var i = 0; i < 500; i++)
        {
            var procent = GrupaEksperymentu.Dla(new UzytkownikId(Guid.NewGuid())).ProcentGrupyEksperymentu;
            procent.ShouldBeInRange(0, 99);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void KonstruktorOdrzucaWartosciPozaZakresem(int procent) =>
        Should.Throw<NiezmiennikNaruszonyException>(() => new GrupaEksperymentu(procent))
            .KodBledu.ShouldBe("GRUPA_EKSPERYMENTU_POZA_ZAKRESEM");
}
