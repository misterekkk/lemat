// @Role: Identyfikator silnie typowany Zlecenia generowania (zlecenie_generowania.id); agregat Zlecenia dochodzi w module generowania | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Content.Zadanie, Agent generujący | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public readonly record struct ZlecenieGenerowaniaId(Guid Wartosc) : IIdentyfikator<Guid>;
