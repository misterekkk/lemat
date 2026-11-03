// @Role: Przypadek użycia odczytu własnego Ucznia (GET /uczniowie/ja): konto + Uczeń bez śledzenia zmian | @DependsOn: IUzytkownikRepozytorium, IUczenRepozytorium, UczenWidok | @UsedBy: Lemat.Api (EndpointyUcznia), ZmienUczniaHandler | @Task: TASK-124
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Application.Accounts;

public sealed class PobierzUczniaHandler(IUzytkownikRepozytorium uzytkownicy, IUczenRepozytorium uczniowie)
{
    public async Task<UczenWidok> WykonajAsync(UzytkownikId id, CancellationToken ct)
    {
        var konto = await uzytkownicy.PobierzDoOdczytuAsync(id, ct);
        var uczen = await uczniowie.PobierzDoOdczytuAsync(UczenId.Z(id), ct);
        if (konto is null || uczen is null)
        {
            throw new NiezmiennikNaruszonyException("NIE_ZNALEZIONO", "Nie znaleziono Ucznia.");
        }

        return Zloz(konto, uczen);
    }

    internal static UczenWidok Zloz(Uzytkownik konto, Uczen uczen) =>
        new(
            konto.Id.Wartosc,
            konto.Email,
            konto.Imie,
            konto.Nazwisko,
            uczen.PoziomMatury,
            uczen.TerminEgzaminu,
            uczen.WybranyDzialId?.Wartosc,
            uczen.ZgodaNaTrening,
            uczen.Version);
}
