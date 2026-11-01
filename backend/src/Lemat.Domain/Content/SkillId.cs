// @Role: Identyfikator silnie typowany Umiejętności (umiejetnosc.id, integer) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: moduły Treści, Profil, Rekomendacje, Rozwiązania | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public readonly record struct UmiejetnoscId(int Wartosc) : IIdentyfikator<int>;
