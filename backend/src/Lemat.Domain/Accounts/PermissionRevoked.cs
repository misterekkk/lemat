// @Role: Zdarzenie domenowe UprawnienieOdebrane: odebrano Nauczycielowi uprawnienie (zwolnienie Blokad weryfikacji GLOBALNA) | @DependsOn: Lemat.Domain.Common.IZdarzenieDomenowe | @UsedBy: Lemat.Domain.Accounts.Nauczyciel, obsługi zdarzeń (moduł Weryfikacje) | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public sealed record UprawnienieOdebrane(NauczycielId NauczycielId, UprawnienieNauczyciela Uprawnienie, DateTimeOffset Wystapilo) : IZdarzenieDomenowe;
