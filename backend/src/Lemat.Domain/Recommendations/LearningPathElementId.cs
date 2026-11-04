// @Role: Identyfikator silnie typowany Elementu Ścieżki nauki (element_sciezki.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: ElementSciezki, Rekomendacja, Rozwiązania (planowane TASK-020) | @Task: TASK-019
using Lemat.Domain.Common;

namespace Lemat.Domain.Recommendations;

public readonly record struct ElementSciezkiId(Guid Wartosc) : IIdentyfikator<Guid>;
