// @Role: Testy jednostkowe KontaFasada (atrapy repozytoriów): rejestracja ról, autozatwierdzenie Nauczyciela, wymóg skrótu hasła, odczyt konta | @DependsOn: KontaFasada, NSubstitute | @UsedBy: dotnet test | @Task: TASK-016
using Lemat.Application.Accounts;
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using NSubstitute;

namespace Lemat.Application.Tests.Accounts;

public sealed class AccountsFacadeTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly IUzytkownikRepozytorium _uzytkownicy = Substitute.For<IUzytkownikRepozytorium>();
    private readonly IUczenRepozytorium _uczniowie = Substitute.For<IUczenRepozytorium>();
    private readonly INauczycielRepozytorium _nauczyciele = Substitute.For<INauczycielRepozytorium>();
    private readonly IJednostkaPracy _jednostkaPracy = Substitute.For<IJednostkaPracy>();

    private KontaFasada Fasada()
    {
        _jednostkaPracy.WykonajAsync(Arg.Any<Func<CancellationToken, Task<KontoSkrot>>>(), Arg.Any<CancellationToken>())
            .Returns(wywolanie => wywolanie.Arg<Func<CancellationToken, Task<KontoSkrot>>>()(Ct));
        return new KontaFasada(_uzytkownicy, _uczniowie, _nauczyciele, _jednostkaPracy, TimeProvider.System);
    }

    [Fact]
    public async Task RejestracjaUczniaDodajeKontoIUczniaZTymSamymIdentyfikatorem()
    {
        var wynik = await Fasada().ZarejestrujAsync(
            new RejestracjaKontaWejscie(RolaUzytkownika.Uczen, "U@Example.com", "hash", "Ewa", "Nowak", true), Ct);

        wynik.Status.ShouldBe(StatusKonta.Aktywne);
        _uzytkownicy.Received(1).Dodaj(Arg.Is<Uzytkownik>(u => u.Email == "u@example.com" && u.Id == wynik.UzytkownikId), "hash");
        _uczniowie.Received(1).Dodaj(Arg.Is<Uczen>(u => u.Id.Wartosc == wynik.UzytkownikId.Wartosc && u.OswiadczenieWieku16));
        _nauczyciele.DidNotReceiveWithAnyArgs().Dodaj(default!);
    }

    [Theory]
    [InlineData(false, StatusKonta.OczekujeNaZatwierdzenie)]
    [InlineData(true, StatusKonta.Aktywne)]
    public async Task RejestracjaNauczycielaWgFlagiAutozatwierdzenia(bool autozatwierdz, StatusKonta oczekiwany)
    {
        var wynik = await Fasada().ZarejestrujAsync(
            new RejestracjaKontaWejscie(RolaUzytkownika.Nauczyciel, "n@example.com", "hash", "Anna", "Lis", false, autozatwierdz), Ct);

        wynik.Status.ShouldBe(oczekiwany);
        _nauczyciele.Received(1).Dodaj(Arg.Any<Nauczyciel>());
        _uczniowie.DidNotReceiveWithAnyArgs().Dodaj(default!);
    }

    [Fact]
    public async Task RejestracjaUczniaBezOswiadczeniaJestOdrzucona()
    {
        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().ZarejestrujAsync(
            new RejestracjaKontaWejscie(RolaUzytkownika.Uczen, "u@example.com", "hash", "Ewa", "Nowak", false), Ct));

        blad.KodBledu.ShouldBe("OSWIADCZENIE_WIEKU_WYMAGANE");
        _uzytkownicy.DidNotReceiveWithAnyArgs().Dodaj(default!, default!);
    }

    [Fact]
    public async Task RejestracjaWymagaSkrotuHasla()
    {
        var blad = await Should.ThrowAsync<NiezmiennikNaruszonyException>(() => Fasada().ZarejestrujAsync(
            new RejestracjaKontaWejscie(RolaUzytkownika.Uczen, "u@example.com", " ", "Ewa", "Nowak", true), Ct));

        blad.KodBledu.ShouldBe("HASLO_HASH_WYMAGANY");
    }

    [Fact]
    public async Task OdczytKontaZwracaNullGdyBrak()
    {
        _uzytkownicy.PobierzDoOdczytuAsync(Arg.Any<UzytkownikId>(), Arg.Any<CancellationToken>()).Returns((Uzytkownik?)null);

        (await Fasada().PobierzKontoAsync(new UzytkownikId(Guid.NewGuid()), Ct)).ShouldBeNull();
    }
}
