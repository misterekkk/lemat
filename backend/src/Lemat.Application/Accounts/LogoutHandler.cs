// @Role: Przypadek użycia wylogowania: unieważnienie rodziny Tokenów odświeżania wskazanej ciasteczkiem albo wszystkich tokenów konta | @DependsOn: ITokenOdswiezaniaRepozytorium, IJednostkaPracy, TimeProvider | @UsedBy: AccountsEndpoints | @Task: TASK-123
using Lemat.Application.Common;
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public sealed class WylogujHandler(ITokenOdswiezaniaRepozytorium tokeny, IJednostkaPracy jednostkaPracy, TimeProvider czas)
{
    public Task WykonajAsync(UzytkownikId uzytkownik, string? tokenJawny, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var teraz = czas.GetUtcNow();
                var wskazany = string.IsNullOrWhiteSpace(tokenJawny)
                    ? null
                    : await tokeny.PobierzPoSkrocieAsync(GeneratorTokenowOdswiezania.Skrot(tokenJawny), token);
                if (wskazany is not null && wskazany.UzytkownikId == uzytkownik)
                {
                    await tokeny.UniewaznRodzineAsync(wskazany.RodzinaId, teraz, token);
                }
                else
                {
                    // Brak (albo cudze) ciasteczko: bezpieczny wariant to unieważnienie wszystkich sesji konta (U-22: bez wielokartowości).
                    await tokeny.UniewaznWszystkieAsync(uzytkownik, teraz, token);
                }
            },
            ct);
}
