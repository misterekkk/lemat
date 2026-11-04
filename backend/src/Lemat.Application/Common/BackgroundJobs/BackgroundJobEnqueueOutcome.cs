// @Role: Wynik zlecenia pracy w tle: wstawiona nowa albo zdeduplikowana (istniejąca OCZEKUJE/W_TOKU) | @DependsOn: — | @UsedBy: RezultatZlecenia, ZlecaniePracSql | @Task: TASK-127
namespace Lemat.Application.Common.BackgroundJobs;

public enum WynikZlecenia
{
    Wstawiona,
    Zdeduplikowana,
}
