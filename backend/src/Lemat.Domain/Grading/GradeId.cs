// @Role: Identyfikator silnie typowany Oceny (ocena.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Ocena, IOcenaRepozytorium, WynikZadania (przez OcenaId), moduł Weryfikacje | @Task: TASK-126
using Lemat.Domain.Common;

namespace Lemat.Domain.Grading;

public readonly record struct OcenaId(Guid Wartosc) : IIdentyfikator<Guid>;
