// @Role: Testy jednostkowe haszowania haseł argon2id: format PHC, losowa sól, weryfikacja poprawnego i błędnego hasła, odporność na uszkodzony skrót | @DependsOn: HaszowanieHasel | @UsedBy: dotnet test | @Task: TASK-122
using Lemat.Infrastructure.Security;

namespace Lemat.Infrastructure.Tests.Security;

public sealed class PasswordHashingTests
{
    // Lekkie parametry przyspieszają test; format i logika są te same co przy parametrach produkcyjnych.
    private static readonly HaszowanieHasel Hasher = new(pamiecKiB: 1024, iteracje: 1, rownoleglosc: 1);

    [Fact]
    public void SkrotMaFormatArgon2idIJestRozniZaKazdymRazem()
    {
        var a = Hasher.Haszuj("Lemat-Demo-2026!");
        var b = Hasher.Haszuj("Lemat-Demo-2026!");

        a.ShouldStartWith("$argon2id$v=19$m=1024,t=1,p=1$");
        a.ShouldNotBe(b);
        a.ShouldNotContain("Lemat-Demo-2026!");
    }

    [Fact]
    public void WeryfikujAkceptujePoprawneHasloIOdrzucaBledne()
    {
        var skrot = Hasher.Haszuj("Lemat-Demo-2026!");

        Hasher.Weryfikuj("Lemat-Demo-2026!", skrot).ShouldBeTrue();
        Hasher.Weryfikuj("lemat-demo-2026!", skrot).ShouldBeFalse();
        Hasher.Weryfikuj(string.Empty, skrot).ShouldBeFalse();
    }

    [Fact]
    public void WeryfikujDlaDomyslnychParametrowProdukcyjnychDzialaMiedzyInstancjami()
    {
        var skrot = new HaszowanieHasel().Haszuj("Lemat-Demo-2026!");

        skrot.ShouldContain("m=65536,t=3,p=4");
        Hasher.Weryfikuj("Lemat-Demo-2026!", skrot).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("nie-skrot")]
    [InlineData("$argon2id$v=19$m=1024,t=1,p=1$!!$!!")]
    [InlineData("$argon2i$v=19$m=1024,t=1,p=1$c29s$aGFzaA")]
    [InlineData("$argon2id$v=19$m=x,t=1,p=1$c29s$aGFzaA")]
    public void WeryfikujUszkodzonySkrotDajeFalszBezWyjatku(string skrot) =>
        Hasher.Weryfikuj("haslo", skrot).ShouldBeFalse();
}
