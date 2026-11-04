// @Role: Wejście przypadku użycia logowania (e-mail, jawne hasło, skrót klienta HTTP) | @DependsOn: — | @UsedBy: LoginHandler, AccountsEndpoints | @Task: TASK-123
namespace Lemat.Application.Accounts;

public sealed record ZalogujWejscie(string Email, string Haslo, byte[] SkrotKlientaHttp);
