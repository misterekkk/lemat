// @Role: Identyfikator silnie typowany Wkładki teoretycznej (wkladka_teoretyczna.id) | @DependsOn: Lemat.Domain.Common.IIdentyfikator | @UsedBy: moduły Treści, Rekomendacje | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public readonly record struct WkladkaTeoretycznaId(Guid Wartosc) : IIdentyfikator<Guid>;
