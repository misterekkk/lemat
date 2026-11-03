// @Role: Identyfikator silnie typowany Transkrypcji (transkrypcja.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Transkrypcja, Rozwiazanie, Ocena, repozytoria Rozwiązań | @Task: TASK-126
using Lemat.Domain.Common;

namespace Lemat.Domain.Solutions;

public readonly record struct TranskrypcjaId(Guid Wartosc) : IIdentyfikator<Guid>;
