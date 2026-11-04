// @Role: Identyfikator silnie typowany Decyzji modelu (decyzja_modelu.id; klucz główny tabeli to para id + created_at) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: DecyzjaModelu, repozytorium decyzji | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Models;

public readonly record struct DecyzjaModeluId(Guid Wartosc) : IIdentyfikator<Guid>;
