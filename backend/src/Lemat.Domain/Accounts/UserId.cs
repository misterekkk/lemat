// @Role: Identyfikator silnie typowany konta Użytkownika (uzytkownik.id, UUIDv7 z aplikacji) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Accounts.*, Lemat.Application.Accounts, repozytoria Kont, inne moduły (autor, Administrator) | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public readonly record struct UzytkownikId(Guid Wartosc) : IIdentyfikator<Guid>;
