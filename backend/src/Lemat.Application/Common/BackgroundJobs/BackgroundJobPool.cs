// @Role: Pula pracy w tle (13.4; kolumna praca_w_tle.pula, UPPER_SNAKE) | @DependsOn: — | @UsedBy: KatalogPrac, ZlecaniePracSql | @Task: TASK-127
namespace Lemat.Application.Common.BackgroundJobs;

public enum PulaPracy
{
    Rozpoznawanie,
    Sprawdzanie,
    Ocenianie,
    Pomocnicza,
    Wolna,
}
