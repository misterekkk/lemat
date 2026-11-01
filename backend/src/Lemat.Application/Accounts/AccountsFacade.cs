// @Role: Implementacja IKontaFasada nad repozytoriami i jednostką pracy: odczyt konta/Grupy eksperymentu, rejestracja Ucznia/Nauczyciela/Administratora w jednej transakcji | @DependsOn: IUzytkownikRepozytorium, IUczenRepozytorium, INauczycielRepozytorium, IJednostkaPracy, TimeProvider | @UsedBy: DI (AccountsRegistration), moduły przez IKontaFasada | @Task: TASK-016
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Application.Accounts;

public sealed class KontaFasada(
    IUzytkownikRepozytorium uzytkownicy,
    IUczenRepozytorium uczniowie,
    INauczycielRepozytorium nauczyciele,
    IJednostkaPracy jednostkaPracy,
    TimeProvider czas) : IKontaFasada
{
    public async Task<KontoSkrot?> PobierzKontoAsync(UzytkownikId id, CancellationToken ct)
    {
        var konto = await uzytkownicy.PobierzDoOdczytuAsync(id, ct);
        return konto is null ? null : Skrot(konto);
    }

    public async Task<GrupaEksperymentu?> PobierzGrupeEksperymentuAsync(UczenId id, CancellationToken ct)
    {
        var uczen = await uczniowie.PobierzDoOdczytuAsync(id, ct);
        return uczen?.GrupaEksperymentu;
    }

    public async Task<KontoSkrot> ZarejestrujAsync(RejestracjaKontaWejscie wejscie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(wejscie);
        if (string.IsNullOrWhiteSpace(wejscie.HasloHash))
        {
            throw new NiezmiennikNaruszonyException("HASLO_HASH_WYMAGANY", "Rejestracja wymaga skrótu hasła.");
        }

        return await jednostkaPracy.WykonajAsync(
            _ =>
            {
                var teraz = czas.GetUtcNow();
                var id = new UzytkownikId(GeneratorUuid.Nowy(czas));
                var konto = Uzytkownik.Zaloz(id, wejscie.Rola, wejscie.Email, wejscie.Imie, wejscie.Nazwisko, teraz);
                switch (wejscie.Rola)
                {
                    case RolaUzytkownika.Uczen:
                        uczniowie.Dodaj(Uczen.Utworz(konto, wejscie.OswiadczenieWieku16));
                        break;
                    case RolaUzytkownika.Nauczyciel:
                        nauczyciele.Dodaj(Nauczyciel.Utworz(konto));
                        if (wejscie.AutozatwierdzNauczyciela)
                        {
                            konto.Zatwierdz();
                        }

                        break;
                }

                uzytkownicy.Dodaj(konto, wejscie.HasloHash);
                return Task.FromResult(Skrot(konto));
            },
            ct);
    }

    private static KontoSkrot Skrot(Uzytkownik konto) => new(konto.Id, konto.Rola, konto.Status, konto.Version);
}
