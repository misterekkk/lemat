// @Role: Identyfikator silnie typowany Pozycji Testu diagnostycznego (pozycja_testu_diagnostycznego.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Profile.PozycjaTestuDiagnostycznego, Odpowiedź (planowane TASK-020) | @Task: TASK-019
using Lemat.Domain.Common;

namespace Lemat.Domain.Profile;

public readonly record struct PozycjaTestuDiagnostycznegoId(Guid Wartosc) : IIdentyfikator<Guid>;
