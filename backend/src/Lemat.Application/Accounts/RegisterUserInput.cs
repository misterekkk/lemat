// @Role: Wejście przypadku użycia rejestracji Użytkownika (jawne hasło; skrót liczy handler) | @DependsOn: Lemat.Domain.Accounts.RolaUzytkownika | @UsedBy: RegisterUserHandler, AccountsEndpoints | @Task: TASK-123
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

public sealed record ZarejestrujUzytkownikaWejscie(
    string Email,
    string Haslo,
    string Imie,
    string Nazwisko,
    RolaUzytkownika Rola,
    bool OswiadczenieWieku16,
    byte[] SkrotKlientaHttp);
