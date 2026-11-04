// @Role: Identyfikator silnie typowany Kalibratora pewności (kalibrator_pewnosci.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: KalibratorPewnosci, ProgPewnosci, Ocena (planowane TASK-022) | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Calibration;

public readonly record struct KalibratorPewnosciId(Guid Wartosc) : IIdentyfikator<Guid>;
