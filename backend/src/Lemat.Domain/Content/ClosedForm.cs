// @Role: Wyliczenie Forma zamknięta (typ PostgreSQL forma_zamknieta) | @DependsOn: — | @UsedBy: Lemat.Domain.Content.Zadanie, OpcjeZamkniete, konfiguracje EF | @Task: TASK-017
namespace Lemat.Domain.Content;

public enum FormaZamknieta
{
    WyborJednokrotny,
    PrawdaFalsz,
}
