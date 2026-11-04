// @Role: Rekord Migawka progu – niezmienny odczyt aktywnego Progu pewności przekazywany regułom oceniania (Id, wartość, Tryb pełnej weryfikacji) | @DependsOn: ProgPewnosci, ProgPewnosciId | @UsedBy: IKalibracjaFasada, ZasadyOceniania i Agent oceniający (planowane TASK-022) | @Task: TASK-018
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Domain.Calibration;

public sealed record MigawkaProgu(
    ProgPewnosciId ProgPewnosciId,
    KonfiguracjaModeluId KonfiguracjaModeluId,
    RodzajZadania RodzajZadania,
    KalibratorPewnosciId KalibratorPewnosciId,
    decimal Wartosc,
    ZrodloProgu Zrodlo)
{
    public bool TrybPelnejWeryfikacji => Wartosc == ProgPewnosci.WartoscTrybuPelnejWeryfikacji;

    public static MigawkaProgu Z(ProgPewnosci prog)
    {
        ArgumentNullException.ThrowIfNull(prog);
        return new MigawkaProgu(prog.Id, prog.KonfiguracjaModeluId, prog.RodzajZadania, prog.KalibratorPewnosciId, prog.Wartosc, prog.Zrodlo);
    }
}
