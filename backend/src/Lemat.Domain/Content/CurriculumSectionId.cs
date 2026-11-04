// @Role: Identyfikator silnie typowany Działu (dzial.id, integer); pozostałe typy modułu Treści dochodzą w TASK-017 | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Accounts.Uczen (wybrany Dział, FR-09), moduł Treści | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public readonly record struct DzialId(int Wartosc) : IIdentyfikator<int>;
