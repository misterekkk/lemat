// @Role: Zdarzenie domenowe KontoPrzywrocone: konto odzyskało status sprzed wniosku o usunięcie (13.6, ADR-040) | @DependsOn: Lemat.Domain.Common.IZdarzenieDomenowe | @UsedBy: Lemat.Domain.Accounts.Uzytkownik, obsługi zdarzeń (TASK-033) | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public sealed record KontoPrzywrocone(UzytkownikId UzytkownikId, RolaUzytkownika Rola, StatusKonta Status, DateTimeOffset Wystapilo) : IZdarzenieDomenowe;
