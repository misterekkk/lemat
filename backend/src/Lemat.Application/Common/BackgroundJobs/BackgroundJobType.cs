// @Role: Typ pracy w tle (katalog 13.3; wartość w bazie UPPER_SNAKE); w POC ROZPOZNAJ_PISMO i OCEN_ROZWIAZANIE, kolejne typy dopisują zadania, które je wykonują | @DependsOn: — | @UsedBy: ZleceniePracy, KatalogPrac, ZlecaniePracSql, PrzeslijRozwiazanieHandler, ZatwierdzRozwiazanieHandler | @Task: TASK-127, TASK-128
namespace Lemat.Application.Common.BackgroundJobs;

public enum TypPracy
{
    RozpoznajPismo,
    OcenRozwiazanie,
}
