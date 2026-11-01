// @Role: Port repozytorium Tokenów odświeżania; rotacja z oknem tolerancji (ADR-057) dojdzie w TASK-072 jako atomowe UPDATE … RETURNING | @DependsOn: Lemat.Domain.Accounts.TokenOdswiezania | @UsedBy: uwierzytelnianie (TASK-072, TASK-081), RefreshTokenRepositoryEf | @Task: TASK-016
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public interface ITokenOdswiezaniaRepozytorium
{
    void Dodaj(TokenOdswiezania token);

    Task<TokenOdswiezania?> PobierzPoSkrocieAsync(byte[] tokenHash, CancellationToken ct);

    /// <summary>Unieważnia wszystkie aktywne tokeny rodziny (wykrycie ponownego użycia); zwraca liczbę zmienionych wierszy.</summary>
    Task<int> UniewaznRodzineAsync(Guid rodzinaId, DateTimeOffset teraz, CancellationToken ct);

    /// <summary>Unieważnia wszystkie aktywne tokeny konta (blokada, usunięcie, zmiana hasła); zwraca liczbę zmienionych wierszy.</summary>
    Task<int> UniewaznWszystkieAsync(UzytkownikId uzytkownik, DateTimeOffset teraz, CancellationToken ct);
}
