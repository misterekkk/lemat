// @Role: Otwiera sesję: wystawia token dostępu i zapisuje nowy Token odświeżania w rodzinie (wywoływana wewnątrz jednostki pracy) | @DependsOn: IWystawcaTokenow, ITokenOdswiezaniaRepozytorium, GeneratorTokenowOdswiezania, LematOpcje, TimeProvider | @UsedBy: RegisterUserHandler, LoginHandler, RefreshSessionHandler | @Task: TASK-123
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Application.Accounts;

public sealed class FabrykaSesji(
    IWystawcaTokenow wystawca,
    ITokenOdswiezaniaRepozytorium tokeny,
    LematOpcje opcje,
    TimeProvider czas)
{
    /// <summary>Wystawia token dostępu i dodaje (bez zapisu) Token odświeżania w rodzinie <paramref name="rodzinaId"/>; zapis należy do wywołującej jednostki pracy.</summary>
    public SesjaRezultat Otworz(
        UzytkownikId uzytkownik,
        RolaUzytkownika rola,
        StatusKonta status,
        string imie,
        Guid rodzinaId,
        byte[] skrotKlientaHttp,
        TokenOdswiezaniaId? identyfikator = null)
    {
        var teraz = czas.GetUtcNow();
        var jawny = GeneratorTokenowOdswiezania.Nowy();
        var token = TokenOdswiezania.Wydaj(
            identyfikator ?? new TokenOdswiezaniaId(GeneratorUuid.Nowy(czas)),
            uzytkownik,
            rodzinaId,
            GeneratorTokenowOdswiezania.Skrot(jawny),
            skrotKlientaHttp,
            teraz,
            TimeSpan.FromDays(opcje.Jwt.OdswiezenieDni));
        tokeny.Dodaj(token);
        return new SesjaRezultat(wystawca.Wystaw(uzytkownik, rola, status, teraz), uzytkownik, rola, status, imie, jawny, token.WygasaAt);
    }

    public Guid NowaRodzina() => GeneratorUuid.Nowy(czas);
}
