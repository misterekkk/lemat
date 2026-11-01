// @Role: Wynik końcowy wzorcowy (value object, schemat jsonb 7.5.3): typ, latex, postać SymPy (wymagana poza typem TEKST) | @DependsOn: NiezmiennikNaruszonyException | @UsedBy: ZadanieWersja, konfiguracje EF (jsonb), Ocenianie (sprawdzanie symboliczne) | @Task: TASK-017
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

public enum TypWynikuKoncowego
{
    Wyrazenie,
    Rownanie,
    Zbior,
    Przedzial,
    WartoscLogiczna,
    Tekst,
}

public sealed record WynikKoncowyWzorcowy
{
    public WynikKoncowyWzorcowy(TypWynikuKoncowego typ, string latex, string? postacSympy)
    {
        if (!Enum.IsDefined(typ))
        {
            throw new NiezmiennikNaruszonyException("WYNIK_WZORCOWY_TYP_NIEZNANY", "Nieznany typ wyniku końcowego wzorcowego.");
        }

        if (string.IsNullOrWhiteSpace(latex))
        {
            throw new NiezmiennikNaruszonyException("WYNIK_WZORCOWY_LATEX_WYMAGANY", "Wynik końcowy wzorcowy wymaga zapisu latex.");
        }

        if (typ != TypWynikuKoncowego.Tekst && string.IsNullOrWhiteSpace(postacSympy))
        {
            throw new NiezmiennikNaruszonyException("WYNIK_WZORCOWY_SYMPY_WYMAGANY", "Postać SymPy jest wymagana dla wszystkich typów poza TEKST.");
        }

        Typ = typ;
        Latex = latex;
        PostacSympy = postacSympy;
    }

    public TypWynikuKoncowego Typ { get; }

    public string Latex { get; }

    public string? PostacSympy { get; }
}
