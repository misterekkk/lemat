// @Role: DTO Użytkownika w odpowiedzi sesji (id, rola, imię, flagi) | @DependsOn: Lemat.Domain.Accounts.RolaUzytkownika, FlagiUzytkownika | @UsedBy: SesjaWyjscie | @Task: TASK-123
using Lemat.Domain.Accounts;

namespace Lemat.Api.Contracts.Accounts;

public sealed record UzytkownikSesji(Guid Id, RolaUzytkownika Rola, string Imie, FlagiUzytkownika Flagi);
