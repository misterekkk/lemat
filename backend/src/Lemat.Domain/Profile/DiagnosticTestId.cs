// @Role: Identyfikator silnie typowany Testu diagnostycznego (test_diagnostyczny.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Profile.TestDiagnostyczny, WynikZadania | @Task: TASK-019
using Lemat.Domain.Common;

namespace Lemat.Domain.Profile;

public readonly record struct TestDiagnostycznyId(Guid Wartosc) : IIdentyfikator<Guid>;
