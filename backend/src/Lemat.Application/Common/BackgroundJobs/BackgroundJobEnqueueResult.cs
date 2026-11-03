// @Role: Rezultat zlecenia pracy w tle: identyfikator pracy i rodzaj wyniku | @DependsOn: WynikZlecenia | @UsedBy: IZlecaniePrac, ZlecaniePracSql | @Task: TASK-127
namespace Lemat.Application.Common.BackgroundJobs;

public sealed record RezultatZlecenia(long PracaId, WynikZlecenia Wynik);
