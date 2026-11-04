// @Role: Identyfikator silnie typowany Rozwiązania (rozwiazanie.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Rozwiazanie, Transkrypcja, Ocena, IRozwiazanieRepozytorium, moduły Ocenianie i Weryfikacje | @Task: TASK-126
using Lemat.Domain.Common;

namespace Lemat.Domain.Solutions;

public readonly record struct RozwiazanieId(Guid Wartosc) : IIdentyfikator<Guid>;
