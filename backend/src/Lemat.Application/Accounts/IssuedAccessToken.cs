// @Role: Wystawiony token dostępu (JWT) wraz z chwilą wygaśnięcia | @DependsOn: — | @UsedBy: IWystawcaTokenow, SessionResult, endpointy /auth/* | @Task: TASK-123
namespace Lemat.Application.Accounts;

public sealed record WystawionyTokenDostepu(string Token, DateTimeOffset WygasaAt);
