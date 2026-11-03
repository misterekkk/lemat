// @Role: Przypadek użycia: uproszczony Profil ucznia jako agregat po Ocenach OSTATECZNA (suma punktów, średni procent, rozbicie na Działy, 5 ostatnich Ocen); bez aktualizacji Elo (U-28) | @DependsOn: IProfilUproszczonyCzytnik, ITresciFasada | @UsedBy: EndpointyProfilu | @Task: TASK-129
using Lemat.Application.Content;
using Lemat.Domain.Accounts;

namespace Lemat.Application.Profile;

public sealed record DzialProfiluWidok(int Id, string Nazwa);

public sealed record DzialUproszczonyWidok(DzialProfiluWidok Dzial, int LiczbaOcenionych, decimal SredniProcentPunktow);

public sealed record OstatniaOcenaSkrotWidok(
    Guid RozwiazanieId,
    Guid ZadanieId,
    DzialProfiluWidok Dzial,
    int Punkty,
    int PunktyMax,
    DateTimeOffset OcenionoAt);

public sealed record ProfilUproszczonyWidok(
    Guid UczenId,
    int LiczbaRozwiazan,
    int LiczbaOcenionych,
    int SumaPunktow,
    int SumaPunktowMax,
    decimal? SredniProcentPunktow,
    IReadOnlyList<DzialUproszczonyWidok> Dzialy,
    IReadOnlyList<OstatniaOcenaSkrotWidok> OstatnieOceny);

public sealed class PobierzProfilUproszczonyHandler(IProfilUproszczonyCzytnik czytnik, ITresciFasada tresci)
{
    public const int LiczbaOstatnichOcen = 5;

    public async Task<ProfilUproszczonyWidok> WykonajAsync(UczenId uczen, CancellationToken ct)
    {
        var dane = await czytnik.PobierzAsync(uczen, ct);
        var nazwy = (await tresci.ListaDzialowAsync(ct)).ToDictionary(d => d.Id, d => d.Nazwa);
        DzialProfiluWidok Dzial(Lemat.Domain.Content.DzialId id) =>
            new(id.Wartosc, nazwy.TryGetValue(id, out var nazwa) ? nazwa : string.Empty);

        var sumaPunktow = dane.Oceny.Sum(o => o.Punkty);
        var sumaMax = dane.Oceny.Sum(o => o.PunktyMax);

        var dzialy = dane.Oceny
            .GroupBy(o => o.DzialId)
            .OrderBy(g => g.Key.Wartosc)
            .Select(g => new DzialUproszczonyWidok(Dzial(g.Key), g.Count(), Procent(g.Sum(o => o.Punkty), g.Sum(o => o.PunktyMax)) ?? 0m))
            .ToList();

        var ostatnie = dane.Oceny
            .OrderByDescending(o => o.OcenionoAt)
            .ThenByDescending(o => o.RozwiazanieId.Wartosc)
            .Take(LiczbaOstatnichOcen)
            .Select(o => new OstatniaOcenaSkrotWidok(o.RozwiazanieId.Wartosc, o.ZadanieId.Wartosc, Dzial(o.DzialId), o.Punkty, o.PunktyMax, o.OcenionoAt))
            .ToList();

        return new ProfilUproszczonyWidok(
            uczen.Wartosc,
            dane.LiczbaRozwiazan,
            dane.Oceny.Count,
            sumaPunktow,
            sumaMax,
            Procent(sumaPunktow, sumaMax),
            dzialy,
            ostatnie);
    }

    /// <summary>100 · punkty / punkty_max zaokrąglone do 1 miejsca; <c>null</c>, gdy brak punktów maksymalnych.</summary>
    private static decimal? Procent(int punkty, int punktyMax) =>
        punktyMax <= 0 ? null : Math.Round(100m * punkty / punktyMax, 1, MidpointRounding.AwayFromZero);
}
