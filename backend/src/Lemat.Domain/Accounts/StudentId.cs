// @Role: Identyfikator silnie typowany Ucznia (uczen.uzytkownik_id; wartość równa UzytkownikId konta Ucznia) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Accounts.Uczen, moduły Klasy, Profil, Rozwiązania | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public readonly record struct UczenId(Guid Wartosc) : IIdentyfikator<Guid>
{
    public static UczenId Z(UzytkownikId uzytkownik) => new(uzytkownik.Wartosc);
}
