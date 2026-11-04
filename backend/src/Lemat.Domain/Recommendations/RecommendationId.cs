// @Role: Identyfikator silnie typowany Rekomendacji (rekomendacja.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Recommendations.Rekomendacja | @Task: TASK-019
using Lemat.Domain.Common;

namespace Lemat.Domain.Recommendations;

public readonly record struct RekomendacjaId(Guid Wartosc) : IIdentyfikator<Guid>;
