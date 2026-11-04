// @Role: Testy Fnv1a: wektory referencyjne FNV-1a 32-bit i skrót UUID w kolejności sieciowej | @DependsOn: Lemat.Domain.Common.Fnv1a | @UsedBy: dotnet test | @Task: TASK-016
using System.Text;
using Lemat.Domain.Common;

namespace Lemat.Domain.Tests.Common;

public sealed class Fnv1aTests
{
    [Theory]
    [InlineData("", 0x811C9DC5u)]
    [InlineData("a", 0xE40C292Cu)]
    [InlineData("foobar", 0xBF9CF968u)]
    public void ZgadzaSieZWektoramiReferencyjnymi(string tekst, uint oczekiwany) =>
        Fnv1a.Skrot32(Encoding.ASCII.GetBytes(tekst)).ShouldBe(oczekiwany);

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", 1768495365u)]
    [InlineData("01234567-89ab-7def-8123-456789abcdef", 3288145941u)]
    [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff", 906459637u)]
    public void SkrotUuidLiczonyNadBajtamiWKolejnosciSieciowej(string uuid, uint oczekiwany) =>
        Fnv1a.Skrot32(Guid.Parse(uuid)).ShouldBe(oczekiwany);
}
