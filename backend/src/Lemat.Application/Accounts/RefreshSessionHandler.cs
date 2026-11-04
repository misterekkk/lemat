// @Role: Przypadek użycia odświeżenia sesji: rotacja Tokenu odświeżania (wiersz blokowany FOR UPDATE), ponowne użycie unieważnia rodzinę, bez okna tolerancji (U-22) | @DependsOn: ITokenOdswiezaniaRepozytorium, IUzytkownikRepozytorium, FabrykaSesji, IJednostkaPracy, TimeProvider | @UsedBy: AccountsEndpoints | @Task: TASK-123
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Application.Accounts;

public sealed class OdswiezSesjeHandler(
    ITokenOdswiezaniaRepozytorium tokeny,
    IUzytkownikRepozytorium uzytkownicy,
    FabrykaSesji sesje,
    IJednostkaPracy jednostkaPracy,
    TimeProvider czas)
{
    /// <summary>Nowa sesja albo <c>null</c>, gdy sesja jest nieważna (brak, nieznany, wygasły lub użyty ponownie token); wynik <c>null</c> także zatwierdza unieważnienie rodziny.</summary>
    public async Task<SesjaRezultat?> WykonajAsync(string? tokenJawny, byte[] skrotKlientaHttp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenJawny))
        {
            return null;
        }

        var skrot = GeneratorTokenowOdswiezania.Skrot(tokenJawny);
        return await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var teraz = czas.GetUtcNow();
                var stary = await tokeny.PobierzDoZmianyAsync(skrot, token);
                if (stary is null)
                {
                    return null;
                }

                if (stary.UniewaznionyAt is not null || stary.ZastapionyAt is not null)
                {
                    // Ponowne użycie tokenu już zrotowanego albo unieważnionego: cała rodzina przestaje działać.
                    await tokeny.UniewaznRodzineAsync(stary.RodzinaId, teraz, token);
                    return null;
                }

                if (teraz >= stary.WygasaAt)
                {
                    return null;
                }

                var konto = await uzytkownicy.PobierzDoOdczytuAsync(stary.UzytkownikId, token);
                if (konto is null || konto.Status != StatusKonta.Aktywne)
                {
                    await tokeny.UniewaznRodzineAsync(stary.RodzinaId, teraz, token);
                    return null;
                }

                var nastepca = new TokenOdswiezaniaId(GeneratorUuid.Nowy(czas));
                var sesja = sesje.Otworz(konto.Id, konto.Rola, konto.Status, konto.Imie, stary.RodzinaId, skrotKlientaHttp, nastepca);
                stary.Zastap(nastepca, teraz);
                return sesja;
            },
            ct);
    }
}
