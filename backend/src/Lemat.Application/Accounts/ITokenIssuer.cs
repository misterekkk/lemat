// @Role: Port wystawcy tokenów dostępu (JWT ES256, 15 min) dla sesji Użytkownika; implementacja w Infrastructure | @DependsOn: Lemat.Domain.Accounts, WystawionyTokenDostepu | @UsedBy: SessionFactory, RegisterUserHandler, LoginHandler, RefreshSessionHandler | @Task: TASK-123
using Lemat.Domain.Accounts;

namespace Lemat.Application.Accounts;

/// <summary>Port wystawcy tokenów dostępu (ADR-020): claims <c>sub</c>, <c>rola</c>, <c>status</c>, <c>jti</c>, <c>exp</c>.</summary>
public interface IWystawcaTokenow
{
    WystawionyTokenDostepu Wystaw(UzytkownikId uzytkownik, RolaUzytkownika rola, StatusKonta status, DateTimeOffset teraz);
}
