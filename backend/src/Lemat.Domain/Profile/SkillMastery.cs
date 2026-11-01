// @Role: Encja Opanowanie Umiejętności w Profilu ucznia: theta, Poziom opanowania, licznik Wyników, powtórki (10.1.3) i źródło inicjalizacji | @DependsOn: UmiejetnoscId, PoziomOpanowania, ModelElo, InicjalizacjaOpanowania | @UsedBy: ProfilUcznia, konfiguracja EF, IProfilFasada | @Task: TASK-019
using Lemat.Domain.Content;

namespace Lemat.Domain.Profile;

public sealed class OpanowanieUmiejetnosci
{
    private OpanowanieUmiejetnosci()
    {
    }

    public UmiejetnoscId UmiejetnoscId { get; private init; }

    public decimal Theta { get; private set; }

    public PoziomOpanowania PoziomOpanowania { get; private set; }

    public int LiczbaWynikow { get; private set; }

    public DateTimeOffset? OstatniWynikAt { get; private set; }

    public int LicznikPowtorek { get; private set; }

    public DateTimeOffset? NastepnaPowtorkaAt { get; private set; }

    public InicjalizacjaOpanowania Inicjalizacja { get; private set; }

    /// <summary>Stan DOMYŚLNY (10.1.3): <c>theta = 0</c>, bez Wyników i powtórek.</summary>
    public static OpanowanieUmiejetnosci Domyslne(UmiejetnoscId umiejetnosc) => new()
    {
        UmiejetnoscId = umiejetnosc,
        Theta = 0m,
        PoziomOpanowania = new PoziomOpanowania(ModelElo.PoziomOpanowaniaZTheta(0m)),
        Inicjalizacja = InicjalizacjaOpanowania.Domyslna,
    };

    internal void Resetuj()
    {
        Theta = 0m;
        PoziomOpanowania = new PoziomOpanowania(ModelElo.PoziomOpanowaniaZTheta(0m));
        LiczbaWynikow = 0;
        OstatniWynikAt = null;
        LicznikPowtorek = 0;
        NastepnaPowtorkaAt = null;
        Inicjalizacja = InicjalizacjaOpanowania.Domyslna;
    }

    /// <summary>Stosuje przyrost <c>theta</c> i aktualizuje powtórki: <c>ulamek &gt;= 0,7</c> przy poziomie &gt;= 0,80 wydłuża odstęp, <c>ulamek &lt; 0,5</c> zeruje licznik.</summary>
    internal void ZastosujWynik(decimal przyrostTheta, decimal ulamek, DateTimeOffset czasWyniku)
    {
        Theta = ModelElo.Zaokraglij(Theta + przyrostTheta);
        PoziomOpanowania = new PoziomOpanowania(ModelElo.PoziomOpanowaniaZTheta(Theta));
        LiczbaWynikow++;
        OstatniWynikAt = czasWyniku;
        if (ulamek >= 0.7m && PoziomOpanowania.Wartosc >= 0.80m)
        {
            LicznikPowtorek++;
            NastepnaPowtorkaAt = ModelElo.NastepnaPowtorka(LicznikPowtorek, czasWyniku);
        }
        else if (ulamek < 0.5m)
        {
            LicznikPowtorek = 0;
            NastepnaPowtorkaAt = czasWyniku.AddDays(1);
        }
    }

    internal void OznaczJakoPrzetestowane() => Inicjalizacja = InicjalizacjaOpanowania.TestDiagnostyczny;

    internal void UstawZPropagacji(decimal theta)
    {
        Theta = ModelElo.Zaokraglij(theta);
        PoziomOpanowania = new PoziomOpanowania(ModelElo.PoziomOpanowaniaZTheta(Theta));
        Inicjalizacja = InicjalizacjaOpanowania.PropagacjaZDzialu;
    }
}
