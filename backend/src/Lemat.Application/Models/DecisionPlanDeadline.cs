// @Role: Rekord Termin planu – terminy miękki i twardy wykonania Planu pytań przez Model decyzyjny oraz zakaz użycia Modelu zapasowego (13.1) | @DependsOn: — | @UsedBy: IModelDecyzyjny (planowane TASK-054), Agent oceniający (planowane TASK-022) | @Task: TASK-018
namespace Lemat.Application.Models;

public sealed record TerminPlanu(DateTimeOffset Miekki, DateTimeOffset Twardy, bool BezModeluZapasowego = false);
