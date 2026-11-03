// @Role: Wartość Wskazówka (tekst ≤ 400 znaków i źródło), zapisywana jednokrotnie w Ocenie (ADR-025) | @DependsOn: Lemat.Domain.Common.NiezmiennikNaruszonyException, ZrodloWskazowki | @UsedBy: Ocena, konfiguracja EF | @Task: TASK-126
using Lemat.Domain.Common;

namespace Lemat.Domain.Grading;

public sealed record Wskazowka
{
    public const int MaksDlugosc = 400;

    public Wskazowka(string tresc, ZrodloWskazowki zrodlo)
    {
        if (string.IsNullOrWhiteSpace(tresc))
        {
            throw new NiezmiennikNaruszonyException("WSKAZOWKA_PUSTA", "Wskazówka nie może być pusta.");
        }

        if (tresc.Length > MaksDlugosc)
        {
            throw new NiezmiennikNaruszonyException("WSKAZOWKA_ZA_DLUGA", "Wskazówka może mieć najwyżej 400 znaków.");
        }

        Tresc = tresc;
        Zrodlo = zrodlo;
    }

    public string Tresc { get; }

    public ZrodloWskazowki Zrodlo { get; }
}
