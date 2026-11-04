// @Role: Przypadek użycia zmiany własnego Ucznia (PATCH /uczniowie/ja, JSON Merge Patch): kontrola wersji (If-Match), istnienie Działu, zapis w jednej transakcji, konflikt równoległy jako wynik | @DependsOn: IUczenRepozytorium, IUzytkownikRepozytorium, ITresciFasada, IJednostkaPracy, PobierzUczniaHandler | @UsedBy: Lemat.Api (EndpointyUcznia) | @Task: TASK-124
using Lemat.Application.Common;
using Lemat.Application.Content;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Application.Accounts;

/// <summary>Pole opcjonalnego patcha: <see cref="Podano"/> = false oznacza „bez zmiany”.</summary>
public readonly record struct ZmianaPola<T>(bool Podano, T Wartosc);

public sealed record ZmienUczniaWejscie(
    UzytkownikId Id,
    int OczekiwanaWersja,
    ZmianaPola<PoziomMatury> PoziomMatury,
    ZmianaPola<DateOnly?> TerminEgzaminu,
    ZmianaPola<int?> WybranyDzialId,
    ZmianaPola<bool> ZgodaNaTrening);

public enum RodzajWynikuZmianyUcznia
{
    Zmieniono,
    KonfliktWersji,
    DzialNieznany,
}

/// <summary><see cref="Widok"/> to stan po zmianie (<c>Zmieniono</c>) albo aktualny stan zasobu (pozostałe rodzaje).</summary>
public sealed record WynikZmianyUcznia(RodzajWynikuZmianyUcznia Rodzaj, UczenWidok Widok);

public sealed class ZmienUczniaHandler(
    IUzytkownikRepozytorium uzytkownicy,
    IUczenRepozytorium uczniowie,
    ITresciFasada tresci,
    IJednostkaPracy jednostkaPracy)
{
    public async Task<WynikZmianyUcznia> WykonajAsync(ZmienUczniaWejscie wejscie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(wejscie);

        // Odczyt poza transakcją: lista Działów (13 wierszy) do sprawdzenia istnienia.
        var dzialy = wejscie.WybranyDzialId is { Podano: true, Wartosc: not null }
            ? await tresci.ListaDzialowAsync(ct)
            : null;

        Uzytkownik? konto = null;
        Uczen? uczen = null;
        try
        {
            var rodzaj = await jednostkaPracy.WykonajAsync(
                async token =>
                {
                    konto = await uzytkownicy.PobierzDoOdczytuAsync(wejscie.Id, token);
                    uczen = await uczniowie.PobierzDoOdczytuAsync(UczenId.Z(wejscie.Id), token);
                    if (konto is null || uczen is null)
                    {
                        throw new NiezmiennikNaruszonyException("NIE_ZNALEZIONO", "Nie znaleziono Ucznia.");
                    }

                    if (uczen.Version != wejscie.OczekiwanaWersja)
                    {
                        return RodzajWynikuZmianyUcznia.KonfliktWersji;
                    }

                    if (wejscie.WybranyDzialId is { Podano: true, Wartosc: { } dzialId } && dzialy!.All(d => d.Id.Wartosc != dzialId))
                    {
                        return RodzajWynikuZmianyUcznia.DzialNieznany;
                    }

                    // Wersja zgodna: agregat do zmiany ze śledzeniem i oczekiwaną wersją (równoległy zapis przegra na UPDATE ... WHERE version).
                    uczen = await uczniowie.PobierzAsync(UczenId.Z(wejscie.Id), wejscie.OczekiwanaWersja, token)
                        ?? throw new NiezmiennikNaruszonyException("NIE_ZNALEZIONO", "Nie znaleziono Ucznia.");

                    if (wejscie.PoziomMatury.Podano)
                    {
                        uczen.UstawPoziom(wejscie.PoziomMatury.Wartosc);
                    }

                    if (wejscie.TerminEgzaminu.Podano)
                    {
                        uczen.UstawTermin(wejscie.TerminEgzaminu.Wartosc);
                    }

                    if (wejscie.WybranyDzialId.Podano)
                    {
                        uczen.WybierzDzial(wejscie.WybranyDzialId.Wartosc is { } id ? new DzialId(id) : null);
                    }

                    if (wejscie.ZgodaNaTrening.Podano)
                    {
                        uczen.UstawZgodeNaTrening(wejscie.ZgodaNaTrening.Wartosc);
                    }

                    return RodzajWynikuZmianyUcznia.Zmieniono;
                },
                ct);

            // Widok po zatwierdzeniu transakcji: Version zwiększa zapis (AggregateVersionInterceptor).
            return new WynikZmianyUcznia(rodzaj, PobierzUczniaHandler.Zloz(konto!, uczen!));
        }
        catch (KonfliktWspolbieznosciException)
        {
            // Równoległy zapis z tym samym If-Match przegrał: zwracamy aktualny stan (412 szczegoly.aktualny).
            var aktualneKonto = await uzytkownicy.PobierzDoOdczytuAsync(wejscie.Id, ct);
            var aktualnyUczen = await uczniowie.PobierzDoOdczytuAsync(UczenId.Z(wejscie.Id), ct);
            if (aktualneKonto is null || aktualnyUczen is null)
            {
                throw new NiezmiennikNaruszonyException("NIE_ZNALEZIONO", "Nie znaleziono Ucznia.");
            }

            return new WynikZmianyUcznia(RodzajWynikuZmianyUcznia.KonfliktWersji, PobierzUczniaHandler.Zloz(aktualneKonto, aktualnyUczen));
        }
    }
}
