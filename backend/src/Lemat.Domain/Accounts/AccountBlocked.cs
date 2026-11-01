// @Role: Zdarzenie domenowe KontoZablokowane: Administrator zablokował konto (13.6) | @DependsOn: Lemat.Domain.Common.IZdarzenieDomenowe | @UsedBy: Lemat.Domain.Accounts.Uzytkownik, obsługi zdarzeń (unieważnienie rodzin tokenów, eskalacja Weryfikacji) | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public sealed record KontoZablokowane(UzytkownikId UzytkownikId, RolaUzytkownika Rola, DateTimeOffset Wystapilo) : IZdarzenieDomenowe;
