// @Role: DTO odpowiedzi sesji (access_token, wygasa_at, uzytkownik) dla logowania i odświeżenia | @DependsOn: UzytkownikSesji | @UsedBy: AccountsEndpoints, RejestracjaWyjscie | @Task: TASK-123
namespace Lemat.Api.Contracts.Accounts;

public sealed record SesjaWyjscie(string AccessToken, DateTimeOffset WygasaAt, UzytkownikSesji Uzytkownik);
