// @Role: Identyfikator silnie typowany Konfiguracji modelu (konfiguracja_modelu.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: KonfiguracjaModelu, DecyzjaModelu, RezultatPytan, moduł Kalibracja | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Models;

public readonly record struct KonfiguracjaModeluId(Guid Wartosc) : IIdentyfikator<Guid>;
