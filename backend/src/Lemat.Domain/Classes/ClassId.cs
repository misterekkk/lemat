// @Role: Identyfikator silnie typowany Klasy (klasa.id, uuid) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: Lemat.Domain.Classes.Klasa, moduły Weryfikacje, Przypisania, Api | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Classes;

public readonly record struct KlasaId(Guid Wartosc) : IIdentyfikator<Guid>;
