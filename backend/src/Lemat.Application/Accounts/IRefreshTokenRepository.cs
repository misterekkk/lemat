// @Role: Port repozytorium Tokenów odświeżania; rotacja pod blokadą wiersza FOR UPDATE bez okna tolerancji (U-22); okno ADR-057 post-POC | @DependsOn: Lemat.Domain.Accounts.TokenOdswiezania | @UsedBy: uwierzytelnianie (TASK-123), RefreshTokenRepositoryEf | @Task: TASK-016, TASK-123
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public interface ITokenOdswiezaniaRepozytorium
{
    void Dodaj(TokenOdswiezania token);

    Task<TokenOdswiezania?> PobierzPoSkrocieAsync(byte[] tokenHash, CancellationToken ct);

    /// <summary>
    /// Wczytuje token do rotacji ze śledzeniem i blokadą wiersza (<c>SELECT … FOR UPDATE</c>) — równoległe odświeżenia tym samym tokenem
    /// są serializowane, więc drugie widzi token już zastąpiony (U-22: bez okna tolerancji). Wymaga otwartej jednostki pracy.
    /// </summary>
    Task<TokenOdswiezania?> PobierzDoZmianyAsync(byte[] tokenHash, CancellationToken ct);

    /// <summary>Unieważnia wszystkie aktywne tokeny rodziny (wykrycie ponownego użycia); zwraca liczbę zmienionych wierszy.</summary>
    Task<int> UniewaznRodzineAsync(Guid rodzinaId, DateTimeOffset teraz, CancellationToken ct);

    /// <summary>Unieważnia wszystkie aktywne tokeny konta (blokada, usunięcie, zmiana hasła); zwraca liczbę zmienionych wierszy.</summary>
    Task<int> UniewaznWszystkieAsync(UzytkownikId uzytkownik, DateTimeOffset teraz, CancellationToken ct);
}
