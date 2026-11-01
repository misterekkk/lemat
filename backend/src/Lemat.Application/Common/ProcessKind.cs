// @Role: Rodzaj procesu aplikacji (api, worker, scheduler) wybierający pulę połączeń i limity czasu bazy (ADR-001, ADR-059) | @DependsOn: — | @UsedBy: Lemat.Infrastructure.Persistence.PersistenceRegistration, hosty | @Task: TASK-015
namespace Lemat.Application.Common;

public enum RodzajProcesu
{
    Api,
    Worker,
    Scheduler,
}
