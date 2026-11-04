// @Role: Przypadek użycia POST /rozwiazania (kontrakt POC 7.4): kontrola Zadania i wersji, brak nieukończonego Rozwiązania, normalizacja i zapis zdjęć w magazynie, a w JEDNEJ transakcji Rozwiązanie PRZESLANE + Zdjęcia (usun_po = +90 dni) + techniczny element Ścieżki (U-24) + zlecenie ROZPOZNAJ_PISMO | @DependsOn: ITresciFasada, IRekomendacjeFasada, IRozwiazanieRepozytorium, IRozwiazaniaFasada, IMagazynObiektow, IObrobkaObrazow, IZlecaniePrac, IJednostkaPracy, TimeProvider | @UsedBy: Lemat.Api (EndpointyRozwiazan) | @Task: TASK-127
using Lemat.Application.Common;
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Application.Content;
using Lemat.Application.Models;
using Lemat.Application.Recommendations;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Solutions;

namespace Lemat.Application.Solutions;

/// <param name="Zdjecia">Zawartość plików z pola <c>zdjecia</c> w kolejności przesłania.</param>
/// <param name="MockScenariusz">Scenariusz Mocka przenoszony w ładunku pracy (<c>mock_scenariusz</c>, ADR-065); <c>domyslny</c>, gdy brak.</param>
public sealed record PrzeslijRozwiazanieWejscie(
    UczenId Uczen,
    ZadanieWersjaId ZadanieWersjaId,
    IReadOnlyList<ReadOnlyMemory<byte>> Zdjecia,
    string MockScenariusz);

public enum RodzajWynikuPrzeslania
{
    Przyjete,
    ZadanieNieznane,
    ZadanieZamkniete,
    WersjaNieaktualna,
    StanNieprawidlowy,
    ZaDuzoZdjec,
    ZdjecieZaDuze,
    NieobslugiwanyFormat,
    ZdjecieNieprawidlowe,
}

/// <summary>
/// <see cref="Rozwiazanie"/> dla <c>Przyjete</c>; <see cref="AktualnaWersja"/> dla <c>WersjaNieaktualna</c>;
/// <see cref="Istniejace"/> (identyfikator i status Rozwiązania w trakcie) dla <c>StanNieprawidlowy</c>.
/// </summary>
public sealed record WynikPrzeslania(
    RodzajWynikuPrzeslania Rodzaj,
    RozwiazanieSkrot? Rozwiazanie = null,
    ZadanieWersjaId? AktualnaWersja = null,
    (RozwiazanieId Id, StatusRozwiazania Status)? Istniejace = null);

public sealed class PrzeslijRozwiazanieHandler(
    ITresciFasada tresci,
    IRekomendacjeFasada rekomendacje,
    IRozwiazanieRepozytorium rozwiazania,
    IRozwiazaniaFasada fasada,
    IMagazynObiektow magazyn,
    IObrobkaObrazow obrobka,
    IZlecaniePrac zlecanie,
    IJednostkaPracy jednostkaPracy,
    TimeProvider czas)
{
    public async Task<WynikPrzeslania> WykonajAsync(PrzeslijRozwiazanieWejscie wejscie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(wejscie);

        if (wejscie.Zdjecia.Count is < 1 or > ZdjecieRozwiazania.MaksLiczbaZdjec)
        {
            return new WynikPrzeslania(RodzajWynikuPrzeslania.ZaDuzoZdjec);
        }

        // Odczyty poza transakcją: Zadanie (wersja aktualna, opublikowane, globalne) i szybka kontrola nieukończonego Rozwiązania.
        var zadanie = await tresci.PobierzZadanieDoOcenyAsync(wejscie.ZadanieWersjaId, ct);
        var widok = zadanie is null ? null : await tresci.PobierzZadanieUczniaAsync(zadanie.ZadanieId, ct);
        if (zadanie is null || widok is not { Status: StatusZadania.Opublikowane, Zasieg: ZasiegZadania.Globalny })
        {
            return new WynikPrzeslania(RodzajWynikuPrzeslania.ZadanieNieznane);
        }

        if (widok.ZadanieWersjaId != wejscie.ZadanieWersjaId)
        {
            return new WynikPrzeslania(RodzajWynikuPrzeslania.WersjaNieaktualna, AktualnaWersja: widok.ZadanieWersjaId);
        }

        if (zadanie.Rodzaj == RodzajZadania.Zamkniete)
        {
            return new WynikPrzeslania(RodzajWynikuPrzeslania.ZadanieZamkniete);
        }

        if (await IstniejaceWTrakcieAsync(wejscie.Uczen, zadanie.ZadanieId, ct) is { } istniejace)
        {
            return new WynikPrzeslania(RodzajWynikuPrzeslania.StanNieprawidlowy, Istniejace: istniejace);
        }

        var obrazy = new List<ObrazZnormalizowany>(wejscie.Zdjecia.Count);
        foreach (var plik in wejscie.Zdjecia)
        {
            try
            {
                obrazy.Add(await obrobka.NormalizujAsync(plik, ct));
            }
            catch (ZdjecieNieprawidloweException e)
            {
                return new WynikPrzeslania(e.Kod switch
                {
                    ZdjecieNieprawidloweException.KodZaDuze => RodzajWynikuPrzeslania.ZdjecieZaDuze,
                    ZdjecieNieprawidloweException.KodNieobslugiwanyFormat => RodzajWynikuPrzeslania.NieobslugiwanyFormat,
                    _ => RodzajWynikuPrzeslania.ZdjecieNieprawidlowe,
                });
            }
        }

        var teraz = czas.GetUtcNow();
        var rozwiazanieId = new RozwiazanieId(GeneratorUuid.Nowy(czas));
        var zdjecia = new List<ZdjecieRozwiazania>(obrazy.Count);
        for (var i = 0; i < obrazy.Count; i++)
        {
            var id = GeneratorUuid.Nowy(czas);
            zdjecia.Add(ZdjecieRozwiazania.Utworz(
                id,
                przeslanieNr: 1,
                kolejnosc: i + 1,
                $"zdjecia/{wejscie.Uczen.Wartosc}/{rozwiazanieId.Wartosc}/{id}.jpg",
                obrazy[i].Jpeg.Length,
                obrazy[i].Sha256,
                obrazy[i].Szerokosc,
                obrazy[i].Wysokosc,
                teraz + ZdjecieRozwiazania.MaksOkresPrzechowywania,
                teraz));
        }

        // Zapis obiektów poza transakcją; osierocone obiekty po wycofaniu usuwamy od razu (sprzątanie okresowe — POST-MVP).
        var zapisane = new List<string>(zdjecia.Count);
        try
        {
            for (var i = 0; i < zdjecia.Count; i++)
            {
                await magazyn.ZapiszAsync(zdjecia[i].KluczObiektu, obrazy[i].Jpeg, ZdjecieRozwiazania.TypMimeJpeg, ct);
                zapisane.Add(zdjecia[i].KluczObiektu);
            }

            var konflikt = await jednostkaPracy.WykonajAsync(
                async token =>
                {
                    if (await IstniejaceWTrakcieAsync(wejscie.Uczen, zadanie.ZadanieId, token) is { } wTrakcie)
                    {
                        return (RozwiazanieId: (RozwiazanieId?)null, Istniejace: ((RozwiazanieId, StatusRozwiazania)?)wTrakcie);
                    }

                    var element = await rekomendacje.RozpocznijProbeAsync(
                        wejscie.Uczen, zadanie.ZadanieId, zadanie.UmiejetnoscGlowna, zadanie.ZadanieWersjaId, token);
                    var rozwiazanie = Rozwiazanie.Przeslij(
                        rozwiazanieId, wejscie.Uczen, zadanie.ZadanieId, zadanie.ZadanieWersjaId, zadanie.Rodzaj, element.Id, null, zdjecia, teraz);
                    rozwiazania.Dodaj(rozwiazanie);
                    await zlecanie.ZlecAsync(
                        new ZleceniePracy(
                            TypPracy.RozpoznajPismo,
                            new { RozwiazanieId = rozwiazanieId.Wartosc, PrzeslanieNr = 1, PonowienieNr = 0, wejscie.MockScenariusz },
                            $"{rozwiazanieId.Wartosc}:1:0"),
                        token);
                    return (RozwiazanieId: (RozwiazanieId?)rozwiazanieId, Istniejace: ((RozwiazanieId, StatusRozwiazania)?)null);
                },
                ct);

            if (konflikt.Istniejace is { } blokujace)
            {
                await UsunObiektyAsync(zapisane);
                return new WynikPrzeslania(RodzajWynikuPrzeslania.StanNieprawidlowy, Istniejace: blokujace);
            }
        }
        catch
        {
            await UsunObiektyAsync(zapisane);
            throw;
        }

        var skrot = await fasada.PobierzDlaUczniaAsync(rozwiazanieId, wejscie.Uczen, ct)
            ?? throw new InvalidOperationException("Rozwiązanie zapisane w transakcji nie jest widoczne po jej zatwierdzeniu.");
        return new WynikPrzeslania(RodzajWynikuPrzeslania.Przyjete, skrot);
    }

    private async Task<(RozwiazanieId Id, StatusRozwiazania Status)?> IstniejaceWTrakcieAsync(UczenId uczen, ZadanieId zadanie, CancellationToken ct)
    {
        var id = await rozwiazania.ZnajdzNieukonczoneAsync(uczen, zadanie, ct);
        if (id is not { } znalezione)
        {
            return null;
        }

        var istniejace = await rozwiazania.PobierzDoOdczytuAsync(znalezione, ct);
        return istniejace is null ? null : (znalezione, istniejace.Status);
    }

    private async Task UsunObiektyAsync(IEnumerable<string> klucze)
    {
        foreach (var klucz in klucze)
        {
            try
            {
                await magazyn.UsunAsync(klucz, CancellationToken.None);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Sprzątanie best-effort: osierocony obiekt zbierze okresowe sprzątanie (SPRZATAJ_OSIEROCONE_OBIEKTY, POST-MVP).
            }
        }
    }
}
