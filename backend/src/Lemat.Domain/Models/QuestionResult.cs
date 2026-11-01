// @Role: Rekord Rezultat pytań – wynik wykonania Planu pytań przez Model decyzyjny (decyzje składowe, pominięte kody, czas, koszt, użyty model) | @DependsOn: DecyzjaSkladowa, KonfiguracjaModeluId, ModelDecyzyjnyUzyty, PrzyczynaZapasowego | @UsedBy: IModelDecyzyjny, ZasadyOceniania (planowane TASK-022), Agent oceniający | @Task: TASK-018
namespace Lemat.Domain.Models;

/// <param name="Decyzje">Tylko pytania zadane (warunek spełniony).</param>
/// <param name="Pominiete">Kody pytań warunkowych, których warunek nie zaszedł.</param>
public sealed record RezultatPytan(
    KonfiguracjaModeluId KonfiguracjaModeluId,
    IReadOnlyList<DecyzjaSkladowa> Decyzje,
    IReadOnlyList<string> Pominiete,
    TimeSpan Czas,
    decimal KosztPln,
    ModelDecyzyjnyUzyty Uzyty,
    PrzyczynaZapasowego? Przyczyna);
