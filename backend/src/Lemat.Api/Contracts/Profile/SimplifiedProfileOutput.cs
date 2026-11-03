// @Role: DTO uproszczonego Profilu ucznia (ProfilUproszczonyWyjscie, DzialUproszczony, OstatniaOcenaSkrot; kontrakt POC 4.3) | @DependsOn: ProfilUproszczonyWidok | @UsedBy: EndpointyProfilu | @Task: TASK-129
using Lemat.Application.Profile;

namespace Lemat.Api.Contracts.Profile;

public sealed record DzialProfiluWyjscie(int Id, string Nazwa);

public sealed record DzialUproszczony(DzialProfiluWyjscie Dzial, int LiczbaOcenionych, decimal SredniProcentPunktow);

public sealed record OstatniaOcenaSkrot(
    Guid RozwiazanieId,
    Guid ZadanieId,
    DzialProfiluWyjscie Dzial,
    int Punkty,
    int PunktyMax,
    DateTimeOffset OcenionoAt);

public sealed record ProfilUproszczonyWyjscie(
    Guid UczenId,
    int LiczbaRozwiazan,
    int LiczbaOcenionych,
    int SumaPunktow,
    int SumaPunktowMax,
    decimal? SredniProcentPunktow,
    IReadOnlyList<DzialUproszczony> Dzialy,
    IReadOnlyList<OstatniaOcenaSkrot> OstatnieOceny)
{
    public static ProfilUproszczonyWyjscie Z(ProfilUproszczonyWidok widok)
    {
        ArgumentNullException.ThrowIfNull(widok);
        return new ProfilUproszczonyWyjscie(
            widok.UczenId,
            widok.LiczbaRozwiazan,
            widok.LiczbaOcenionych,
            widok.SumaPunktow,
            widok.SumaPunktowMax,
            widok.SredniProcentPunktow,
            [.. widok.Dzialy.Select(d => new DzialUproszczony(new DzialProfiluWyjscie(d.Dzial.Id, d.Dzial.Nazwa), d.LiczbaOcenionych, d.SredniProcentPunktow))],
            [.. widok.OstatnieOceny.Select(o => new OstatniaOcenaSkrot(o.RozwiazanieId, o.ZadanieId, new DzialProfiluWyjscie(o.Dzial.Id, o.Dzial.Nazwa), o.Punkty, o.PunktyMax, o.OcenionoAt))]);
    }
}
