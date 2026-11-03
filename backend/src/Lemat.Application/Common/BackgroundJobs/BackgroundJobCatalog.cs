// @Role: Katalog parametrów typów prac w tle (pula, priorytet, maks. prób wg tabeli 13.3) używany przy zlecaniu; w POC tylko ROZPOZNAJ_PISMO | @DependsOn: TypPracy, PulaPracy | @UsedBy: ZlecaniePracSql | @Task: TASK-127
namespace Lemat.Application.Common.BackgroundJobs;

public static class KatalogPrac
{
    /// <summary>Stałe parametry typu pracy (13.3): pula, priorytet (1 = najwyższy) i liczba prób technicznych.</summary>
    public sealed record Opis(TypPracy Typ, PulaPracy Pula, short Priorytet, short MaksProb);

    private static readonly Opis OpisRozpoznajPismo = new(TypPracy.RozpoznajPismo, PulaPracy.Rozpoznawanie, 1, 3);

    public static Opis Pobierz(TypPracy typ) => typ switch
    {
        TypPracy.RozpoznajPismo => OpisRozpoznajPismo,
        _ => throw new ArgumentOutOfRangeException(nameof(typ), typ, "Typ pracy spoza katalogu."),
    };
}
