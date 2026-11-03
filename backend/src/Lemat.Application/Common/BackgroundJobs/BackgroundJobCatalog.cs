// @Role: Katalog parametrów typów prac w tle (pula, priorytet, maks. prób wg tabeli 13.3) używany przy zlecaniu; w POC ROZPOZNAJ_PISMO i OCEN_ROZWIAZANIE (maks_prob 3 wg kontraktu POC; 13.3 podaje 4) | @DependsOn: TypPracy, PulaPracy | @UsedBy: ZlecaniePracSql | @Task: TASK-127, TASK-128
namespace Lemat.Application.Common.BackgroundJobs;

public static class KatalogPrac
{
    /// <summary>Stałe parametry typu pracy (13.3): pula, priorytet (1 = najwyższy) i liczba prób technicznych.</summary>
    public sealed record Opis(TypPracy Typ, PulaPracy Pula, short Priorytet, short MaksProb);

    private static readonly Opis OpisRozpoznajPismo = new(TypPracy.RozpoznajPismo, PulaPracy.Rozpoznawanie, 1, 3);

    private static readonly Opis OpisOcenRozwiazanie = new(TypPracy.OcenRozwiazanie, PulaPracy.Ocenianie, 1, 3);

    public static Opis Pobierz(TypPracy typ) => typ switch
    {
        TypPracy.RozpoznajPismo => OpisRozpoznajPismo,
        TypPracy.OcenRozwiazanie => OpisOcenRozwiazanie,
        _ => throw new ArgumentOutOfRangeException(nameof(typ), typ, "Typ pracy spoza katalogu."),
    };
}
