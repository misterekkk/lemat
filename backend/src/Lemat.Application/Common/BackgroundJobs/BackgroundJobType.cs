// @Role: Typ pracy w tle (katalog 13.3; wartość w bazie UPPER_SNAKE); w POC tylko ROZPOZNAJ_PISMO, kolejne typy dopisują zadania, które je wykonują | @DependsOn: — | @UsedBy: ZleceniePracy, KatalogPrac, ZlecaniePracSql, PrzeslijRozwiazanieHandler | @Task: TASK-127
namespace Lemat.Application.Common.BackgroundJobs;

public enum TypPracy
{
    RozpoznajPismo,
}
