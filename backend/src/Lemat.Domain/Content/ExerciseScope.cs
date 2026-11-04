// @Role: Wyliczenie Zasięg zadania (typ PostgreSQL zasieg_zadania) | @DependsOn: — | @UsedBy: Lemat.Domain.Content.Zadanie, konfiguracje EF | @Task: TASK-017
namespace Lemat.Domain.Content;

public enum ZasiegZadania
{
    Globalny,
    Prywatny,
}
