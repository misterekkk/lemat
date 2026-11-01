// @Role: Identyfikator silnie typowany Zadania (zadanie.id) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: moduły Treści, Rozwiązania, Profil, Rekomendacje, Klasy | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public readonly record struct ZadanieId(Guid Wartosc) : IIdentyfikator<Guid>;
