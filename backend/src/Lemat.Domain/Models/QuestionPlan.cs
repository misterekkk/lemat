// @Role: Wartość Plan pytań (ADR-026): jedyny kontrakt Agent oceniający – Model decyzyjny; walidacja unikalności kodów, zależności tylko od pytań bezwarunkowych (głębokość 1, brak cykli), zgodność wartości warunku z opcjami nadrzędnego | @DependsOn: PytanieWPlanie, WarunekPytania, NiezmiennikNaruszonyException | @UsedBy: IModelDecyzyjny, FabrykaPlanuOceny (planowane TASK-022), adaptery | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Models;

public sealed class PlanPytan
{
    private PlanPytan(IReadOnlyList<PytanieWPlanie> pytania) => Pytania = pytania;

    public IReadOnlyList<PytanieWPlanie> Pytania { get; }

    public IEnumerable<PytanieWPlanie> Bezwarunkowe => Pytania.Where(p => p.Warunek is null);

    public IEnumerable<PytanieWPlanie> Warunkowe => Pytania.Where(p => p.Warunek is not null);

    public static PlanPytan Jedno(Pytanie pytanie) => Utworz([new PytanieWPlanie(pytanie, null)]);

    /// <summary>
    /// Waliduje plan: niepusty, kody unikalne, <c>KodNadrzedny</c> wskazuje pytanie bezwarunkowe z planu (stąd głębokość 1 i brak cykli),
    /// a wartości warunku należą do dozwolonych odpowiedzi pytania nadrzędnego. Naruszenie: <c>PLAN_PYTAN_NIEPRAWIDLOWY</c>.
    /// </summary>
    public static PlanPytan Utworz(IReadOnlyList<PytanieWPlanie> pytania)
    {
        ArgumentNullException.ThrowIfNull(pytania);
        if (pytania.Count == 0)
        {
            throw Blad("Plan pytań nie może być pusty.");
        }

        var poKodzie = new Dictionary<string, PytanieWPlanie>(StringComparer.Ordinal);
        foreach (var pozycja in pytania)
        {
            if (!poKodzie.TryAdd(pozycja.Pytanie.Kod, pozycja))
            {
                throw Blad($"Kod pytania '{pozycja.Pytanie.Kod}' występuje w planie więcej niż raz.");
            }
        }

        foreach (var pozycja in pytania.Where(p => p.Warunek is not null))
        {
            var warunek = pozycja.Warunek!;
            if (!poKodzie.TryGetValue(warunek.KodNadrzedny, out var nadrzedne))
            {
                throw Blad($"Pytanie '{pozycja.Pytanie.Kod}' zależy od nieistniejącego pytania '{warunek.KodNadrzedny}'.");
            }

            if (nadrzedne.Warunek is not null)
            {
                throw Blad($"Pytanie '{pozycja.Pytanie.Kod}' zależy od pytania warunkowego '{warunek.KodNadrzedny}' (dozwolona głębokość zależności: 1).");
            }

            if (warunek.Wartosci.Count == 0)
            {
                throw Blad($"Warunek pytania '{pozycja.Pytanie.Kod}' nie wskazuje żadnej wartości.");
            }

            var dozwolone = nadrzedne.Pytanie.DozwoloneOdpowiedzi;
            var obce = warunek.Wartosci.Concat(warunek.PominOpcje).FirstOrDefault(w => !dozwolone.Contains(w, StringComparer.Ordinal));
            if (obce is not null)
            {
                throw Blad($"Warunek pytania '{pozycja.Pytanie.Kod}' wskazuje '{obce}', której pytanie '{warunek.KodNadrzedny}' nie dopuszcza.");
            }
        }

        return new PlanPytan([.. pytania]);
    }

    private static NiezmiennikNaruszonyException Blad(string opis) => new("PLAN_PYTAN_NIEPRAWIDLOWY", opis);
}
