// @Role: Identyfikator silnie typowany Ścieżki nauki (sciezka_nauki.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Recommendations.SciezkaNauki | @Task: TASK-019
using Lemat.Domain.Common;

namespace Lemat.Domain.Recommendations;

public readonly record struct SciezkaNaukiId(Guid Wartosc) : IIdentyfikator<Guid>;
