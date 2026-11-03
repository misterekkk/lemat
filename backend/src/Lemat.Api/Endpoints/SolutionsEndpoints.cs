// @Role: Grupa endpointów Rozwiązań Ucznia: POST /rozwiazania (multipart, 202 + Location), GET /rozwiazania (status, kursor), GET /rozwiazania/{id} (odpytywanie), GET …/zdjecia/{id}/adres oraz GET /magazyn-lokalny/{token} (kontrakt POC 7.4 … 7.6, 7.9, 7.10) | @DependsOn: PrzeslijRozwiazanieHandler, IRozwiazaniaFasada, MapperRozwiazan, IOdczytPodpisanegoAdresu, IMagazynObiektow, FiltrScenariuszaMock, KodekKursora | @UsedBy: ApiHost (IGrupaEndpointow) | @Task: TASK-127
using System.Globalization;
using Lemat.Api.Contracts.Solutions;
using Lemat.Api.Errors;
using Lemat.Api.Http;
using Lemat.Api.Pagination;
using Lemat.Api.Security;
using Lemat.Application.Models;
using Lemat.Application.Solutions;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Solutions;

namespace Lemat.Api.Endpoints;

public sealed class EndpointyRozwiazan : IGrupaEndpointow
{
    public const string PolePlikow = "zdjecia";

    /// <summary>Ważność podpisanego adresu zdjęcia (kontrakt POC 7.9).</summary>
    public static readonly TimeSpan WaznoscAdresu = TimeSpan.FromMinutes(5);

    public void Mapuj(IEndpointRouteBuilder trasy)
    {
        ArgumentNullException.ThrowIfNull(trasy);

        trasy.MapPost("/api/v1/rozwiazania", PrzeslijAsync)
            .WithName("PrzeslijRozwiazanie")
            .RequireAuthorization(PolitykiAutoryzacji.Uczen)
            .DisableAntiforgery()
            .AddEndpointFilter<FiltrScenariuszaMock>()
            .Accepts<RozwiazanieWejscie>("multipart/form-data")
            .Produces<RozwiazanieWyjscie>(StatusCodes.Status202Accepted);

        trasy.MapGet("/api/v1/rozwiazania", ListaAsync)
            .WithName("ListaRozwiazan")
            .RequireAuthorization(PolitykiAutoryzacji.Uczen)
            .Produces<Strona<RozwiazanieWyjscie>>();

        trasy.MapGet("/api/v1/rozwiazania/{id}", PobierzAsync)
            .WithName("PobierzRozwiazanie")
            .RequireAuthorization(PolitykiAutoryzacji.Uczen)
            .Produces<RozwiazanieWyjscie>();

        trasy.MapGet("/api/v1/rozwiazania/{id}/zdjecia/{zdjecieRozwiazaniaId}/adres", AdresZdjeciaAsync)
            .WithName("AdresZdjeciaRozwiazania")
            .RequireAuthorization(PolitykiAutoryzacji.Uczen)
            .Produces<AdresZdjeciaWyjscie>();

        // [Rozszerzenie POC] (U-03, OQ-05): bez Authorization; dostęp daje podpisany, wygasający token w adresie.
        trasy.MapGet("/api/v1/magazyn-lokalny/{token}", PobierzZMagazynuAsync)
            .WithName("PobierzZMagazynuLokalnego")
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    /// <summary>Ładunek kursora listy Rozwiązań: ostatnia zwrócona pozycja (created_at, id).</summary>
    private sealed record KursorRozwiazan(DateTimeOffset CreatedAt, Guid Id);

    private static async Task<IResult> PrzeslijAsync(
        [AsParameters] RozwiazanieWejscie wejscie,
        PrzeslijRozwiazanieHandler handler,
        MapperRozwiazan mapper,
        HttpContext kontekst,
        CancellationToken ct)
    {
        if (wejscie.ZadanieWersjaId is not { } wersja || wersja == Guid.Empty)
        {
            throw BladApiException.Walidacja("zadanie_wersja_id", "WYMAGANE", ErrorCatalog.KomunikatReguly("WYMAGANE"));
        }

        var pliki = wejscie.Pliki ?? throw new BladApiException(KodBledu.ZaDuzoZdjec);
        if (pliki.FirstOrDefault(p => !string.Equals(p.Name, PolePlikow, StringComparison.Ordinal)) is { } obcy)
        {
            throw BladApiException.Walidacja(obcy.Name, "PLIK_POZA_POLEM_ZDJECIA", ErrorCatalog.KomunikatReguly("PLIK_POZA_POLEM_ZDJECIA"));
        }

        var zawartosc = new List<ReadOnlyMemory<byte>>(pliki.Count);
        foreach (var plik in pliki)
        {
            await using var strumien = plik.OpenReadStream();
            using var bufor = new MemoryStream(capacity: (int)Math.Min(plik.Length, int.MaxValue));
            await strumien.CopyToAsync(bufor, ct);
            zawartosc.Add(bufor.ToArray());
        }

        var wynik = await handler.WykonajAsync(
            new PrzeslijRozwiazanieWejscie(
                UczenId.Z(BiezacyUzytkownik.Id(kontekst.User)),
                new Domain.Content.ZadanieWersjaId(wersja),
                zawartosc,
                FiltrScenariuszaMock.Pobierz(kontekst)),
            ct);

        switch (wynik.Rodzaj)
        {
            case RodzajWynikuPrzeslania.Przyjete:
                var wyjscie = await mapper.ZlozAsync(wynik.Rozwiazanie!, ct);
                return Results.Accepted($"/api/v1/rozwiazania/{wyjscie.Id}", wyjscie);
            case RodzajWynikuPrzeslania.ZadanieNieznane:
                throw new BladApiException(KodBledu.NieZnaleziono);
            case RodzajWynikuPrzeslania.ZadanieZamkniete:
                throw BladApiException.Walidacja("zadanie_wersja_id", "TYP_NIEPRAWIDLOWY", "Zadanie zamknięte rozwiązuje się odpowiedzią, a nie zdjęciem.");
            case RodzajWynikuPrzeslania.WersjaNieaktualna:
                throw new BladApiException(
                    KodBledu.WersjaZadaniaNieaktualna,
                    szczegoly: new Dictionary<string, object?> { ["aktualna_zadanie_wersja_id"] = wynik.AktualnaWersja!.Value.Wartosc });
            case RodzajWynikuPrzeslania.StanNieprawidlowy:
                throw new BladApiException(
                    KodBledu.RozwiazanieNieprawidlowyStan,
                    "Dla tego zadania masz już rozwiązanie w trakcie oceny.",
                    new Dictionary<string, object?>
                    {
                        ["istniejace_rozwiazanie_id"] = wynik.Istniejace!.Value.Id.Wartosc,
                        ["status"] = KonwencjaNazw.PascalNaUpperSnake(wynik.Istniejace.Value.Status.ToString()),
                    });
            case RodzajWynikuPrzeslania.ZaDuzoZdjec:
                throw new BladApiException(KodBledu.ZaDuzoZdjec);
            case RodzajWynikuPrzeslania.ZdjecieZaDuze:
                throw new BladApiException(KodBledu.ZdjecieZaDuze);
            case RodzajWynikuPrzeslania.NieobslugiwanyFormat:
                throw new BladApiException(KodBledu.NieobslugiwanyFormat, "Obsługiwany jest wyłącznie format JPEG.");
            default:
                throw new BladApiException(KodBledu.ZdjecieNieprawidlowe);
        }
    }

    private static async Task<IResult> ListaAsync(
        HttpRequest zadanie,
        IRozwiazaniaFasada rozwiazania,
        MapperRozwiazan mapper,
        KodekKursora kodek,
        HttpContext kontekst,
        CancellationToken ct)
    {
        var limit = zadanie.Query["limit"].ToString();
        var zapytanie = ZapytanieStrony.Z(
            string.IsNullOrEmpty(limit) ? null : int.TryParse(limit, NumberStyles.None, CultureInfo.InvariantCulture, out var l)
                ? l
                : throw BladApiException.Walidacja("limit", "TYP_NIEPRAWIDLOWY", "Limit musi być liczbą całkowitą."),
            zadanie.Query["kursor"].ToString());
        var status = OdczytajStatus(zadanie.Query["status"].ToString());

        PozycjaListyRozwiazan? po = null;
        if (zapytanie.Kursor is not null)
        {
            var kursor = kodek.Odkoduj<KursorRozwiazan>(zapytanie.Kursor);
            po = new PozycjaListyRozwiazan(kursor.CreatedAt, new RozwiazanieId(kursor.Id));
        }

        var strona = await rozwiazania.ListaDlaUczniaAsync(UczenId.Z(BiezacyUzytkownik.Id(kontekst.User)), status, po, zapytanie.Limit, ct);
        var elementy = new List<RozwiazanieWyjscie>(strona.Elementy.Count);
        foreach (var element in strona.Elementy)
        {
            elementy.Add(await mapper.ZlozAsync(element, ct));
        }

        var nastepny = strona.MaWiecej && strona.Elementy.Count > 0
            ? kodek.Zakoduj(new KursorRozwiazan(strona.Elementy[^1].CreatedAt, strona.Elementy[^1].Id.Wartosc))
            : null;
        return Results.Ok(new Strona<RozwiazanieWyjscie>(elementy, nastepny));
    }

    private static async Task<IResult> PobierzAsync(string id, IRozwiazaniaFasada rozwiazania, MapperRozwiazan mapper, HttpContext kontekst, CancellationToken ct)
    {
        var skrot = await ZnajdzWlasneAsync(id, rozwiazania, kontekst, ct);
        return Results.Ok(await mapper.ZlozAsync(skrot, ct));
    }

    private static async Task<IResult> AdresZdjeciaAsync(
        string id,
        string zdjecieRozwiazaniaId,
        IRozwiazaniaFasada rozwiazania,
        IMagazynObiektow magazyn,
        HttpContext kontekst,
        CancellationToken ct)
    {
        var skrot = await ZnajdzWlasneAsync(id, rozwiazania, kontekst, ct);
        var zdjecie = Guid.TryParse(zdjecieRozwiazaniaId, out var zdjecieId)
            ? skrot.Zdjecia.FirstOrDefault(z => z.Id == zdjecieId && z.Dostepne)
            : null;
        if (zdjecie is null)
        {
            throw new BladApiException(KodBledu.NieZnaleziono);
        }

        var adres = magazyn.PodpisanyUrl(zdjecie.KluczObiektu, WaznoscAdresu);
        return Results.Ok(new AdresZdjeciaWyjscie(adres.Url.OriginalString, adres.WygasaAt));
    }

    private static async Task<IResult> PobierzZMagazynuAsync(string token, IOdczytPodpisanegoAdresu magazyn, HttpContext kontekst, CancellationToken ct)
    {
        var dane = await magazyn.OtworzPodpisanyAsync(token, ct) ?? throw new BladApiException(KodBledu.NieZnaleziono);
        kontekst.Response.Headers.CacheControl = "private, max-age=300";
        return Results.File(dane, "image/jpeg");
    }

    // Cudze, nieistniejące i niebędące UUID Rozwiązanie to zawsze 404 NIE_ZNALEZIONO (uprawnienia obiektowe, ADR-071).
    private static async Task<RozwiazanieSkrot> ZnajdzWlasneAsync(string id, IRozwiazaniaFasada rozwiazania, HttpContext kontekst, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var rozwiazanieId))
        {
            throw new BladApiException(KodBledu.NieZnaleziono);
        }

        return await rozwiazania.PobierzDlaUczniaAsync(new RozwiazanieId(rozwiazanieId), UczenId.Z(BiezacyUzytkownik.Id(kontekst.User)), ct)
            ?? throw new BladApiException(KodBledu.NieZnaleziono);
    }

    private static StatusRozwiazania? OdczytajStatus(string tekst)
    {
        if (string.IsNullOrEmpty(tekst))
        {
            return null;
        }

        if (tekst.All(c => char.IsAsciiLetterUpper(c) || c == '_')
            && Enum.TryParse<StatusRozwiazania>(KonwencjaNazw.UpperSnakeNaPascal(tekst), ignoreCase: false, out var status)
            && Enum.IsDefined(status))
        {
            return status;
        }

        throw BladApiException.Walidacja("status", "TYP_NIEPRAWIDLOWY", "Nieznany status rozwiązania.");
    }
}
