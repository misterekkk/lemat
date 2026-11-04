// @Role: Opcje Zadania zamkniętego (value object, schemat jsonb 7.5.1): WYBOR_JEDNOKROTNY albo PRAWDA_FALSZ; Ocen = ocena kluczem (PRAWDA_FALSZ: punkt tylko gdy wszystkie stwierdzenia poprawne) | @DependsOn: FormaZamknieta, NiezmiennikNaruszonyException | @UsedBy: ZadanieWersja, konfiguracje EF (jsonb), moduły Rozwiązania (Odpowiedź), Ocenianie | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public sealed record OpcjaWyboru(string Id, string TrescMd);

public sealed record StwierdzeniePrawdaFalsz(string Id, string TrescMd, bool Poprawna);

/// <summary>Wybór Ucznia: <see cref="Opcja"/> dla WYBOR_JEDNOKROTNY albo <see cref="Stwierdzenia"/> (id -> prawda/fałsz) dla PRAWDA_FALSZ.</summary>
public sealed record WyborZamkniety(string? Opcja = null, IReadOnlyDictionary<string, bool>? Stwierdzenia = null);

public sealed record RezultatOcenyZamknietej(int Punkty, bool Poprawna);

public sealed record OpcjeZamkniete
{
    /// <summary>Zadanie zamknięte jest warte jeden punkt w obu formach (7.5.1).</summary>
    public const int PunktyZadaniaZamknietego = 1;

    private OpcjeZamkniete(FormaZamknieta forma, IReadOnlyList<OpcjaWyboru> opcje, string? poprawna, IReadOnlyList<StwierdzeniePrawdaFalsz> stwierdzenia)
    {
        Forma = forma;
        Opcje = opcje;
        Poprawna = poprawna;
        Stwierdzenia = stwierdzenia;
    }

    public FormaZamknieta Forma { get; }

    /// <summary>Niepuste (2+) wyłącznie dla WYBOR_JEDNOKROTNY.</summary>
    public IReadOnlyList<OpcjaWyboru> Opcje { get; }

    /// <summary>Identyfikator poprawnej opcji; wyłącznie dla WYBOR_JEDNOKROTNY.</summary>
    public string? Poprawna { get; }

    /// <summary>Niepuste wyłącznie dla PRAWDA_FALSZ.</summary>
    public IReadOnlyList<StwierdzeniePrawdaFalsz> Stwierdzenia { get; }

    public static OpcjeZamkniete WyborJednokrotny(IReadOnlyList<OpcjaWyboru> opcje, string poprawna)
    {
        ArgumentNullException.ThrowIfNull(opcje);
        if (opcje.Count < 2)
        {
            throw new NiezmiennikNaruszonyException("OPCJE_ZAMKNIETE_ZA_MALO_OPCJI", "Wybór jednokrotny wymaga co najmniej dwóch opcji.");
        }

        WymagajUnikalnychIdentyfikatorow(opcje.Select(o => o.Id));
        if (!opcje.Any(o => o.Id == poprawna))
        {
            throw new NiezmiennikNaruszonyException("OPCJE_ZAMKNIETE_POPRAWNA_NIEZNANA", "Poprawna opcja musi należeć do listy opcji.");
        }

        return new OpcjeZamkniete(FormaZamknieta.WyborJednokrotny, opcje, poprawna, []);
    }

    public static OpcjeZamkniete PrawdaFalsz(IReadOnlyList<StwierdzeniePrawdaFalsz> stwierdzenia)
    {
        ArgumentNullException.ThrowIfNull(stwierdzenia);
        if (stwierdzenia.Count < 2)
        {
            throw new NiezmiennikNaruszonyException("OPCJE_ZAMKNIETE_ZA_MALO_STWIERDZEN", "Prawda/fałsz wymaga co najmniej dwóch stwierdzeń.");
        }

        WymagajUnikalnychIdentyfikatorow(stwierdzenia.Select(s => s.Id));
        return new OpcjeZamkniete(FormaZamknieta.PrawdaFalsz, [], null, stwierdzenia);
    }

    /// <summary>
    /// Ocena kluczem. WYBOR_JEDNOKROTNY: 1 punkt, gdy wybrano poprawną opcję. PRAWDA_FALSZ: 1 punkt tylko wtedy, gdy każde
    /// stwierdzenie ocenione poprawnie (zasada CKE); brakująca ocena stwierdzenia = niepoprawna.
    /// </summary>
    public RezultatOcenyZamknietej Ocen(WyborZamkniety wybor)
    {
        ArgumentNullException.ThrowIfNull(wybor);
        var poprawna = Forma switch
        {
            FormaZamknieta.WyborJednokrotny => wybor.Opcja is not null && wybor.Opcja == Poprawna,
            FormaZamknieta.PrawdaFalsz => wybor.Stwierdzenia is { } oceny
                && Stwierdzenia.All(s => oceny.TryGetValue(s.Id, out var ocena) && ocena == s.Poprawna),
            _ => false,
        };
        return new RezultatOcenyZamknietej(poprawna ? PunktyZadaniaZamknietego : 0, poprawna);
    }

    private static void WymagajUnikalnychIdentyfikatorow(IEnumerable<string> identyfikatory)
    {
        var lista = identyfikatory.ToList();
        if (lista.Any(string.IsNullOrWhiteSpace) || lista.Distinct(StringComparer.Ordinal).Count() != lista.Count)
        {
            throw new NiezmiennikNaruszonyException("OPCJE_ZAMKNIETE_IDENTYFIKATORY", "Identyfikatory opcji/stwierdzeń muszą być niepuste i unikalne.");
        }
    }
}
