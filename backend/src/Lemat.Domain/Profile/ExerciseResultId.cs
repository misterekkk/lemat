// @Role: Identyfikator silnie typowany Wyniku zadania (wynik_zadania.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Profile.WynikZadania, IWynikZadaniaRepozytorium | @Task: TASK-019
using Lemat.Domain.Common;

namespace Lemat.Domain.Profile;

public readonly record struct WynikZadaniaId(Guid Wartosc) : IIdentyfikator<Guid>;
