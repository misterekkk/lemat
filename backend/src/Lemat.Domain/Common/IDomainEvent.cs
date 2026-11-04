// @Role: Zdarzenie domenowe zgłaszane przez agregat (katalog zdarzeń 13.6) | @DependsOn: — | @UsedBy: Lemat.Domain.Common.Agregat, moduły domenowe | @Task: TASK-014
namespace Lemat.Domain.Common;

public interface IZdarzenieDomenowe
{
    DateTimeOffset Wystapilo { get; }
}
