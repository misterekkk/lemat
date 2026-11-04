// @Role: Zdarzenie domenowe KontoUsuwane: złożono wniosek o usunięcie konta, start karencji (13.6, ADR-040) | @DependsOn: Lemat.Domain.Common.IZdarzenieDomenowe | @UsedBy: Lemat.Domain.Accounts.Uzytkownik, obsługi zdarzeń (TASK-033) | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public sealed record KontoUsuwane(UzytkownikId UzytkownikId, RolaUzytkownika Rola, DateTimeOffset Wystapilo) : IZdarzenieDomenowe;
