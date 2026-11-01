// @Role: Parametry Zadania potrzebne modelowi Elo: Umiejętność główna i dodatkowe, trudnosc_b oraz szansa odgadnięcia c | @DependsOn: UmiejetnoscId | @UsedBy: ProfilUcznia, ProfilFasada, IParametryZadaniaDlaProfilu | @Task: TASK-019
using Lemat.Domain.Content;

namespace Lemat.Domain.Profile;

public sealed record ParametryEloZadania(
    UmiejetnoscId UmiejetnoscGlowna,
    IReadOnlyList<UmiejetnoscId> UmiejetnosciDodatkowe,
    decimal TrudnoscB,
    decimal SzansaOdgadniecia);
