// @Role: Implementacja IOcenianieFasada: odczyt migawki Oceny aktualnej | @DependsOn: IOcenaRepozytorium | @UsedBy: DI (GradingRegistration), Api (planowane TASK-127, TASK-128) | @Task: TASK-126
using Lemat.Domain.Grading;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Grading;

public sealed class OcenianieFasada(IOcenaRepozytorium oceny) : IOcenianieFasada
{
    public async Task<OcenaSkrot?> PobierzAktualnaAsync(RozwiazanieId rozwiazanie, CancellationToken ct)
    {
        var ocena = await oceny.PobierzAktualnaAsync(rozwiazanie, ct);
        return ocena is null ? null : Skrot(ocena);
    }

    private static OcenaSkrot Skrot(Ocena o) => new(
        o.Id,
        o.RozwiazanieId,
        o.Wersja,
        o.Status,
        o.Zrodlo,
        o.TranskrypcjaId,
        o.Wariant,
        o.Punkty,
        o.PunktyMax,
        o.PierwszyBlednyKrok,
        o.BrakujacyKrok,
        o.TypBledu,
        o.MozliwyBladOdczytu,
        o.KrokPodejrzanyOdczyt,
        o.PewnoscOceny?.Wartosc,
        o.Uzasadnienie,
        o.Wskazowka?.Tresc,
        o.Wskazowka?.Zrodlo,
        o.Widocznosc,
        o.CreatedAt,
        [.. o.Kryteria.Select(k => new OcenaKryteriumSkrot(k.KryteriumOcenianiaId, k.Punkty, k.Prawdopodobienstwo))]);
}
