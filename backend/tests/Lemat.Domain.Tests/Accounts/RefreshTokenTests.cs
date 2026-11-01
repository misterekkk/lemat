// @Role: Testy Tokenu odświeżania i Próby logowania: wydanie, ważność, rotacja, okno tolerancji 30 s z limitem 5 użyć, unieważnienie | @DependsOn: Lemat.Domain.Accounts.TokenOdswiezania, ProbaLogowania | @UsedBy: dotnet test | @Task: TASK-016
using System.Net;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Tests.Accounts;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static TokenOdswiezania Nowy(TimeSpan? waznosc = null) => TokenOdswiezania.Wydaj(
        new TokenOdswiezaniaId(Guid.NewGuid()),
        new UzytkownikId(Guid.NewGuid()),
        Guid.NewGuid(),
        new byte[32],
        new byte[32],
        Teraz,
        waznosc ?? TimeSpan.FromDays(30));

    [Fact]
    public void WydanyTokenJestWaznyDoWygasniecia()
    {
        var token = Nowy();

        token.CzyWazny(Teraz).ShouldBeTrue();
        token.CzyWazny(Teraz.AddDays(30).AddTicks(-1)).ShouldBeTrue();
        token.CzyWazny(Teraz.AddDays(30)).ShouldBeFalse();
    }

    [Fact]
    public void SkrotyMusza32BajtySha256()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => TokenOdswiezania.Wydaj(
            new TokenOdswiezaniaId(Guid.NewGuid()), new UzytkownikId(Guid.NewGuid()), Guid.NewGuid(), new byte[31], new byte[32], Teraz, TimeSpan.FromDays(1)))
            .KodBledu.ShouldBe("TOKEN_SKROT_NIEPRAWIDLOWY");
        Should.Throw<NiezmiennikNaruszonyException>(() => Nowy(TimeSpan.Zero)).KodBledu.ShouldBe("TOKEN_WAZNOSC_NIEPRAWIDLOWA");
    }

    [Fact]
    public void ZastapionyTokenNiejestWazny_AleJestWOkniePrzez30Sekund()
    {
        var token = Nowy();
        var nastepca = new TokenOdswiezaniaId(Guid.NewGuid());

        token.Zastap(nastepca, Teraz);

        token.ZastapionyPrzez.ShouldBe(nastepca);
        token.CzyWazny(Teraz).ShouldBeFalse();
        token.CzyWOknieTolerancji(Teraz.AddSeconds(30)).ShouldBeTrue();
        token.CzyWOknieTolerancji(Teraz.AddSeconds(31)).ShouldBeFalse();
        Should.Throw<NiezmiennikNaruszonyException>(() => token.Zastap(nastepca, Teraz)).KodBledu.ShouldBe("TOKEN_NIEPRAWIDLOWY_STAN");
    }

    [Fact]
    public void OknoToleranciaDopuszczaNajwyzej5Uzyc()
    {
        var token = Nowy();
        token.Zastap(new TokenOdswiezaniaId(Guid.NewGuid()), Teraz);

        for (var i = 0; i < 5; i++)
        {
            token.ZarejestrujUzycieWOknieTolerancji(Teraz.AddSeconds(1));
        }

        token.UzyciaWOknieTolerancji.ShouldBe(5);
        token.CzyWOknieTolerancji(Teraz.AddSeconds(1)).ShouldBeFalse();
        Should.Throw<NiezmiennikNaruszonyException>(() => token.ZarejestrujUzycieWOknieTolerancji(Teraz.AddSeconds(1)))
            .KodBledu.ShouldBe("TOKEN_POZA_OKNEM_TOLERANCJI");
    }

    [Fact]
    public void UniewaznienieWylaczaWaznoscIOkno()
    {
        var token = Nowy();
        token.Zastap(new TokenOdswiezaniaId(Guid.NewGuid()), Teraz);

        token.Uniewaznij(Teraz.AddSeconds(2));
        token.Uniewaznij(Teraz.AddSeconds(9));

        token.UniewaznionyAt.ShouldBe(Teraz.AddSeconds(2));
        token.CzyWOknieTolerancji(Teraz.AddSeconds(3)).ShouldBeFalse();
        Should.Throw<NiezmiennikNaruszonyException>(() => token.Zastap(new TokenOdswiezaniaId(Guid.NewGuid()), Teraz));
    }

    [Fact]
    public void ProbaLogowaniaNormalizujeEmail()
    {
        var proba = ProbaLogowania.Zapisz("  JAN@Example.com ", IPAddress.Parse("203.0.113.7"), udana: false, Teraz);

        proba.Email.ShouldBe("jan@example.com");
        proba.Udana.ShouldBeFalse();
        proba.Ip.ToString().ShouldBe("203.0.113.7");
        proba.CreatedAt.ShouldBe(Teraz);
        Should.Throw<NiezmiennikNaruszonyException>(() => ProbaLogowania.Zapisz(" ", IPAddress.Loopback, true, Teraz));
    }
}
