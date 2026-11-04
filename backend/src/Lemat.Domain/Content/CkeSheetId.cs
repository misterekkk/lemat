// @Role: Identyfikator silnie typowany Arkusza CKE (arkusz_cke.id); agregat Arkusza dochodzi w TASK importu (FR-35) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Content.Zadanie, import Arkuszy CKE | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public readonly record struct ArkuszCkeId(Guid Wartosc) : IIdentyfikator<Guid>;
