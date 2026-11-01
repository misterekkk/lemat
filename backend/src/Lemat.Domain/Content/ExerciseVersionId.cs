// @Role: Identyfikator silnie typowany wersji Zadania (zadanie_wersja.id) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: moduły Treści, Rozwiązania, Ocenianie, Profil | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public readonly record struct ZadanieWersjaId(Guid Wartosc) : IIdentyfikator<Guid>;
