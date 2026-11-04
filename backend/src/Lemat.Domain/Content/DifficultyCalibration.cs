// @Role: Kalibracja Poziomu trudności Zadania (tabela kalibracja_trudnosci, ADR-029): trudnosc_b modelu Elo, liczba Wyników kalibracji, skalibrowany Poziom trudności; aktualizowana wyłącznie atomowym SQL (10.1.3), poza zadanie.version | @DependsOn: ZadanieId, PoziomTrudnosci | @UsedBy: repozytorium Zadań (wiersz początkowy), ITresciFasada (dobór), moduł Profil (TASK-019) | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public sealed class KalibracjaTrudnosci
{
    private KalibracjaTrudnosci()
    {
    }

    public ZadanieId ZadanieId { get; private init; }

    public decimal TrudnoscB { get; private init; }

    public int LiczbaWynikowKalibracji { get; private init; }

    public PoziomTrudnosci PoziomTrudnosci { get; private init; }

    public DateTimeOffset UpdatedAt { get; private init; }

    /// <summary>Wiersz początkowy (12.9, 10.1.3): <c>b = 0,8 x (poziom - 3)</c>, poziom skalibrowany = nominalny.</summary>
    public static KalibracjaTrudnosci Poczatkowa(ZadanieId zadanie, PoziomTrudnosci nominalny, DateTimeOffset teraz) => new()
    {
        ZadanieId = zadanie,
        TrudnoscB = 0.8m * (nominalny.Wartosc - 3),
        LiczbaWynikowKalibracji = 0,
        PoziomTrudnosci = nominalny,
        UpdatedAt = teraz,
    };
}
