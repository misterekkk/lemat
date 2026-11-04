// @Role: Identyfikator silnie typowany Nauczyciela (nauczyciel.uzytkownik_id; wartość równa UzytkownikId konta Nauczyciela) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Accounts.Nauczyciel, moduły Klasy, Weryfikacje | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public readonly record struct NauczycielId(Guid Wartosc) : IIdentyfikator<Guid>
{
    public static NauczycielId Z(UzytkownikId uzytkownik) => new(uzytkownik.Wartosc);
}
