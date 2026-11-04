// @Role: Grupa endpointów Treści: GET /api/v1/dzialy (kontrakt POC 4.2), GET /api/v1/zadania i GET /api/v1/zadania/{id} dla Ucznia (kontrakt POC 5) | @DependsOn: ITresciFasada, PolitykiAutoryzacji, KodekKursora, ZapytanieStrony, ZadanieUczniaWyjscie | @UsedBy: ApiHost (IGrupaEndpointow) | @Task: TASK-124, TASK-125
using System.Globalization;
using Lemat.Api.Contracts.Common;
using Lemat.Api.Contracts.Content;
using Lemat.Api.Errors;
using Lemat.Api.Pagination;
using Lemat.Api.Security;
using Lemat.Application.Content;
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Api.Endpoints;

public sealed class EndpointyTresci : IGrupaEndpointow
{
    public void Mapuj(IEndpointRouteBuilder trasy)
    {
        ArgumentNullException.ThrowIfNull(trasy);

        trasy.MapGet("/api/v1/dzialy", ListaDzialowAsync)
            .WithName("ListaDzialow")
            .RequireAuthorization(PolitykiAutoryzacji.Uczen)
            .Produces<IReadOnlyList<DzialWyjscie>>();

        trasy.MapGet("/api/v1/zadania", ListaZadanAsync)
            .WithName("ListaZadan")
            .RequireAuthorization(PolitykiAutoryzacji.Uczen)
            .Produces<Strona<ZadanieUczniaWyjscie>>();

        trasy.MapGet("/api/v1/zadania/{id}", PobierzZadanieAsync)
            .WithName("PobierzZadanie")
            .RequireAuthorization(PolitykiAutoryzacji.Uczen)
            .Produces<ZadanieUczniaWyjscie>();
    }

    /// <summary>Ładunek kursora listy Zadań: ostatnia zwrócona pozycja (poziom trudności, id).</summary>
    private sealed record KursorZadan(int PoziomTrudnosci, Guid Id);

    private static async Task<IResult> ListaZadanAsync(
        HttpRequest zadanie,
        ITresciFasada tresci,
        KodekKursora kodek,
        CancellationToken ct)
    {
        var zapytanie = ZapytanieStrony.Z(
            LiczbaCalkowita(zadanie, "limit", "TYP_NIEPRAWIDLOWY"),
            zadanie.Query["kursor"].ToString());
        var dzialId = LiczbaCalkowita(zadanie, "dzial_id", "TYP_NIEPRAWIDLOWY");

        PozycjaListyZadan? po = null;
        if (zapytanie.Kursor is not null)
        {
            var kursor = kodek.Odkoduj<KursorZadan>(zapytanie.Kursor);
            if (kursor.PoziomTrudnosci is < PoziomTrudnosci.Minimum or > PoziomTrudnosci.Maksimum)
            {
                throw new BladApiException(KodBledu.KursorNieprawidlowy);
            }

            po = new PozycjaListyZadan(new PoziomTrudnosci(kursor.PoziomTrudnosci), new ZadanieId(kursor.Id));
        }

        var strona = await tresci.ListaZadanUczniaAsync(dzialId is { } d ? new DzialId(d) : null, po, zapytanie.Limit, ct);
        var wyjscie = await ZnazwamiAsync(tresci, strona.Elementy, ct);
        var nastepny = strona.MaWiecej && strona.Elementy.Count > 0
            ? kodek.Zakoduj(new KursorZadan(strona.Elementy[^1].PoziomTrudnosci.Wartosc, strona.Elementy[^1].ZadanieId.Wartosc))
            : null;
        return Results.Ok(new Strona<ZadanieUczniaWyjscie>(wyjscie, nastepny));
    }

    // Nieistniejące, nieopublikowane, nieglobalne i niepoprawne UUID -> 404 NIE_ZNALEZIONO (U-23: bez kontekstu ścieżki/przypisania).
    private static async Task<IResult> PobierzZadanieAsync(string id, ITresciFasada tresci, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var zadanieId))
        {
            throw new BladApiException(KodBledu.NieZnaleziono);
        }

        var zadanie = await tresci.PobierzZadanieUczniaAsync(new ZadanieId(zadanieId), ct);
        if (zadanie is not { Status: StatusZadania.Opublikowane, Zasieg: ZasiegZadania.Globalny })
        {
            throw new BladApiException(KodBledu.NieZnaleziono);
        }

        return Results.Ok((await ZnazwamiAsync(tresci, [zadanie], ct))[0]);
    }

    private static int? LiczbaCalkowita(HttpRequest zadanie, string nazwa, string kodReguly)
    {
        var tekst = zadanie.Query[nazwa].ToString();
        if (string.IsNullOrEmpty(tekst))
        {
            return null;
        }

        return int.TryParse(tekst, NumberStyles.None, CultureInfo.InvariantCulture, out var wartosc)
            ? wartosc
            : throw BladApiException.Walidacja(nazwa, kodReguly, "Wartość musi być liczbą całkowitą.");
    }

    private static async Task<IReadOnlyList<ZadanieUczniaWyjscie>> ZnazwamiAsync(ITresciFasada tresci, IReadOnlyList<ZadanieUcznia> zadania, CancellationToken ct)
    {
        var dzialy = (await tresci.ListaDzialowAsync(ct)).ToDictionary(d => d.Id);
        var umiejetnosci = (await tresci.ListaUmiejetnosciAsync(null, ct)).ToDictionary(u => u.Id);
        return
        [
            .. zadania.Select(z => new ZadanieUczniaWyjscie(
                z.ZadanieId.Wartosc,
                z.ZadanieWersjaId.Wartosc,
                z.Rodzaj,
                z.Opcje?.Forma,
                z.TrescMd,
                [],
                null,
                null,
                z.PunktyMax,
                z.PoziomTrudnosci.Wartosc,
                new DzialZadaniaWyjscie(z.DzialId.Wartosc, dzialy[z.DzialId].Nazwa),
                [.. z.Umiejetnosci
                    .OrderByDescending(u => u.Glowna)
                    .ThenBy(u => u.UmiejetnoscId.Wartosc)
                    .Select(u => new UmiejetnoscZadaniaWyjscie(u.UmiejetnoscId.Wartosc, umiejetnosci[u.UmiejetnoscId].Nazwa, u.Glowna))])),
        ];
    }

    // Zwykła tablica, sort po id; poziom_matury = ROZSZERZONY dla Działu tylko_rozszerzony, inaczej PODSTAWOWY (OQ-14).
    private static async Task<IResult> ListaDzialowAsync(ITresciFasada tresci, CancellationToken ct)
    {
        var dzialy = await tresci.ListaDzialowAsync(ct);
        return Results.Ok(
            dzialy
                .OrderBy(d => d.Id.Wartosc)
                .Select(d => new DzialWyjscie(d.Id.Wartosc, d.Kod, d.Nazwa, d.TylkoRozszerzony ? PoziomMatury.Rozszerzony : PoziomMatury.Podstawowy))
                .ToList());
    }
}
