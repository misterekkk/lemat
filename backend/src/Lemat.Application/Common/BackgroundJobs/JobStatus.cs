// @Role: Status pracy w tle (typ PostgreSQL status_pracy) | @DependsOn: — | @UsedBy: IKolejkaPrac, KolejkaPracSql, ProcesorPrac | @Task: TASK-128
namespace Lemat.Application.Common.BackgroundJobs;

public enum StatusPracy
{
    Oczekuje,
    WToku,
    Zakonczona,
    Martwa,
}
