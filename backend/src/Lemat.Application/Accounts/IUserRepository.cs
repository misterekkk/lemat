// @Role: Port repozytorium agregatu Użytkownik (konto każdej roli); wczytuje agregaty do zmiany ze śledzeniem i ustawia oczekiwaną wersję (If-Match) | @DependsOn: Lemat.Domain.Accounts.Uzytkownik, LoginData | @UsedBy: AccountsFacade, uwierzytelnianie (TASK-072), RODO (TASK-033), UserRepositoryEf | @Task: TASK-016
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public interface IUzytkownikRepozytorium
{
    /// <summary>Wczytuje konto do zmiany (ze śledzeniem); gdy podano <paramref name="oczekiwanaWersja"/>, niezgodność kończy zapis konfliktem współbieżności.</summary>
    Task<Uzytkownik?> PobierzAsync(UzytkownikId id, int? oczekiwanaWersja, CancellationToken ct);

    /// <summary>Odczyt bez śledzenia (zapytania, fasada).</summary>
    Task<Uzytkownik?> PobierzDoOdczytuAsync(UzytkownikId id, CancellationToken ct);

    Task<Uzytkownik?> PobierzPoEmailuAsync(string email, CancellationToken ct);

    /// <summary>Dane do weryfikacji hasła: skrót hasła pozostaje poza agregatem.</summary>
    Task<DaneLogowania?> PobierzDaneLogowaniaAsync(string email, CancellationToken ct);

    /// <summary>Dodaje konto razem ze skrótem hasła (argon2id, liczony przez wywołującego). Duplikat e-mail kończy zapis kodem EMAIL_ZAJETY.</summary>
    void Dodaj(Uzytkownik uzytkownik, string hasloHash);
}
