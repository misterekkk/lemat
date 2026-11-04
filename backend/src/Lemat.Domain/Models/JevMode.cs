// @Role: Wyliczenie Tryb Jev (parametry.tryb_jev): wsadowy (jedno żądanie na rundę) albo pojedynczy (jedno na pytanie) | @DependsOn: — | @UsedBy: ParametryModelu, JevAdapter (POST-MVP) | @Task: TASK-018
namespace Lemat.Domain.Models;

public enum TrybJev
{
    Wsadowy,
    Pojedynczy,
}
