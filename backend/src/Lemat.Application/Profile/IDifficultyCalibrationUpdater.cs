// @Role: Port atomowej kalibracji Poziomu trudności Zadania po Wyniku zadania (10.1.3, ADR-029): jedno polecenie SQL w transakcji jednostki pracy, wpis do dziennika audytu przy zmianie poziomu | @DependsOn: ZadanieId | @UsedBy: ProfilFasada, DifficultyCalibrationUpdaterSql | @Task: TASK-019
using Lemat.Domain.Content;

namespace Lemat.Application.Profile;

/// <summary>Zmiana skalibrowanego Poziomu trudności (stary i nowy poziom 1-5).</summary>
public sealed record ZmianaPoziomuTrudnosci(int Stary, int Nowy)
{
    public bool Zmieniony => Stary != Nowy;
}

public interface IAktualizacjaKalibracjiTrudnosci
{
    /// <summary>
    /// Przesuwa <c>trudnosc_b</c> o <c>-max(0,05; 0,4/(1 + n/30)) · delta</c> jednym atomowym poleceniem; wymaga otwartej transakcji.
    /// Zwraca <c>null</c>, gdy Zadanie nie ma wiersza kalibracji.
    /// </summary>
    Task<ZmianaPoziomuTrudnosci?> ZastosujAsync(ZadanieId zadanie, decimal delta, CancellationToken ct);
}
