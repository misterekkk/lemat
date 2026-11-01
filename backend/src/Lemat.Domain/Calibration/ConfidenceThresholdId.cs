// @Role: Identyfikator silnie typowany Progu pewności (prog_pewnosci.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: ProgPewnosci, Ocena (planowane TASK-022) | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Calibration;

public readonly record struct ProgPewnosciId(Guid Wartosc) : IIdentyfikator<Guid>;
