// @Role: Port odczytu danych do uproszczonego Profilu ucznia (U-28): liczba Rozwiązań Ucznia oraz aktualne Oceny OSTATECZNA Rozwiązań OCENIONE | @DependsOn: UczenId, RozwiazanieId, ZadanieId, DzialId | @UsedBy: PobierzProfilUproszczonyHandler, SimplifiedProfileReaderEf | @Task: TASK-129
using Lemat.Domain.Accounts;
using Lemat.Domain.Content;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Profile;

/// <summary>Ocena OSTATECZNA ocenionego Rozwiązania wraz z Działem Zadania.</summary>
public sealed record OcenionePozycjaProfilu(
    RozwiazanieId RozwiazanieId,
    ZadanieId ZadanieId,
    DzialId DzialId,
    int Punkty,
    int PunktyMax,
    DateTimeOffset OcenionoAt);

public sealed record DaneProfiluUproszczonego(int LiczbaRozwiazan, IReadOnlyList<OcenionePozycjaProfilu> Oceny);

public interface IProfilUproszczonyCzytnik
{
    /// <summary>
    /// Liczba Rozwiązań Ucznia (bez <c>ANULOWANE</c> i <c>PORZUCONE</c>) oraz Oceny <c>OSTATECZNA</c> jego Rozwiązań w statusie <c>OCENIONE</c>.
    /// </summary>
    Task<DaneProfiluUproszczonego> PobierzAsync(UczenId uczen, CancellationToken ct);
}
