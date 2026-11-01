// @Role: Pomocnik If-Match/version (ADR-069): brak -> 428 WYMAGANY_IF_MATCH, zła forma -> 400 NAGLOWEK_NIEPRAWIDLOWY, niezgodność -> 412 KONFLIKT_WERSJI z szczegoly.aktualny | @DependsOn: BladApiException | @UsedBy: PATCH /uczniowie/ja, POST /rozwiazania/{id}/zatwierdzenie (TASK-124, TASK-128) | @Task: TASK-121
using System.Globalization;
using Lemat.Api.Errors;

namespace Lemat.Api.Http;

/// <summary>Odczyt i porównanie nagłówka <c>If-Match: "&lt;version&gt;"</c> (liczba w cudzysłowach).</summary>
public static class NaglowekIfMatch
{
    public const string Nazwa = "If-Match";

    public static long Wymagaj(HttpRequest zadanie)
    {
        ArgumentNullException.ThrowIfNull(zadanie);
        if (!zadanie.Headers.TryGetValue(Nazwa, out var wartosci) || wartosci.Count == 0 || string.IsNullOrWhiteSpace(wartosci[0]))
        {
            throw new BladApiException(KodBledu.WymaganyIfMatch);
        }

        var surowa = wartosci[0]!.Trim();
        if (wartosci.Count > 1 || surowa.Length < 3 || surowa[0] != '"' || surowa[^1] != '"'
            || !long.TryParse(surowa.AsSpan(1, surowa.Length - 2), NumberStyles.None, CultureInfo.InvariantCulture, out var wersja))
        {
            throw new BladApiException(KodBledu.NaglowekNieprawidlowy, "Nagłówek If-Match musi mieć postać \"<numer wersji>\" (liczba w cudzysłowach).");
        }

        return wersja;
    }

    /// <summary>Zgłasza <c>412 KONFLIKT_WERSJI</c> z aktualnym zasobem w <c>szczegoly.aktualny</c>, gdy wersje się różnią.</summary>
    public static void SprawdzZgodnosc(long oczekiwana, long aktualna, Func<object> aktualnyZasob)
    {
        ArgumentNullException.ThrowIfNull(aktualnyZasob);
        if (oczekiwana != aktualna)
        {
            throw new BladApiException(
                KodBledu.KonfliktWersji,
                szczegoly: new Dictionary<string, object?> { ["aktualny"] = aktualnyZasob() });
        }
    }
}
