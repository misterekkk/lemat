// @Role: Odczyt tożsamości zalogowanego Użytkownika z claimów tokenu dostępu (sub) | @DependsOn: System.Security.Claims, Lemat.Domain.Accounts.UzytkownikId | @UsedBy: AccountsEndpoints, kolejne endpointy Ucznia (TASK-124+) | @Task: TASK-123
using System.Security.Claims;
using Lemat.Domain.Accounts;

namespace Lemat.Api.Http;

public static class BiezacyUzytkownik
{
    /// <summary>Identyfikator konta z claimu <c>sub</c>; poza polityką wymagającą logowania brak claimu to błąd programisty.</summary>
    public static UzytkownikId Id(ClaimsPrincipal uzytkownik)
    {
        ArgumentNullException.ThrowIfNull(uzytkownik);
        var wartosc = uzytkownik.FindFirstValue("sub");
        return Guid.TryParse(wartosc, out var id)
            ? new UzytkownikId(id)
            : throw new InvalidOperationException("Brak claimu sub w tokenie dostępu.");
    }
}
