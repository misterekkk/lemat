// @Role: Grupa endpointów własnego Ucznia: GET /api/v1/uczniowie/ja i PATCH /api/v1/uczniowie/ja (JSON Merge Patch, If-Match z version; kontrakt POC 4.1) | @DependsOn: PobierzUczniaHandler, ZmienUczniaHandler, NaglowekIfMatch, Opcjonalne<T>, BladApiException | @UsedBy: ApiHost (IGrupaEndpointow) | @Task: TASK-124
using Lemat.Api.Contracts.Accounts;
using Lemat.Api.Errors;
using Lemat.Api.Http;
using Lemat.Api.Security;
using Lemat.Application.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Api.Endpoints;

public sealed class EndpointyUcznia : IGrupaEndpointow
{
    public void Mapuj(IEndpointRouteBuilder trasy)
    {
        ArgumentNullException.ThrowIfNull(trasy);
        var grupa = trasy.MapGroup("/api/v1/uczniowie/ja").RequireAuthorization(PolitykiAutoryzacji.Uczen);

        grupa.MapGet(string.Empty, PobierzAsync)
            .WithName("PobierzUcznia")
            .Produces<UczenWyjscie>();

        grupa.MapPatch(string.Empty, ZmienAsync)
            .WithName("ZmienUcznia")
            .Accepts<ZmianaUczniaWejscie>("application/merge-patch+json", "application/json")
            .Produces<UczenWyjscie>();
    }

    private static async Task<IResult> PobierzAsync(PobierzUczniaHandler handler, HttpContext kontekst, CancellationToken ct) =>
        Results.Ok(DoWyjscia(await handler.WykonajAsync(BiezacyUzytkownik.Id(kontekst.User), ct)));

    private static async Task<IResult> ZmienAsync(ZmianaUczniaWejscie wejscie, ZmienUczniaHandler handler, HttpContext kontekst, CancellationToken ct)
    {
        var oczekiwana = NaglowekIfMatch.Wymagaj(kontekst.Request);
        WymagajMozliwosciZmiany(wejscie);

        var wynik = await handler.WykonajAsync(
            new ZmienUczniaWejscie(
                BiezacyUzytkownik.Id(kontekst.User),
                (int)Math.Min(oczekiwana, int.MaxValue),
                wejscie.PoziomMatury.Obecne ? new ZmianaPola<PoziomMatury>(true, wejscie.PoziomMatury.Wartosc!.Value) : default,
                wejscie.TerminEgzaminu.Obecne ? new ZmianaPola<DateOnly?>(true, wejscie.TerminEgzaminu.Wartosc) : default,
                wejscie.WybranyDzialId.Obecne ? new ZmianaPola<int?>(true, wejscie.WybranyDzialId.Wartosc) : default,
                wejscie.ZgodaNaTrening.Obecne ? new ZmianaPola<bool>(true, wejscie.ZgodaNaTrening.Wartosc!.Value) : default),
            ct);

        return wynik.Rodzaj switch
        {
            RodzajWynikuZmianyUcznia.Zmieniono => Results.Ok(DoWyjscia(wynik.Widok)),
            RodzajWynikuZmianyUcznia.DzialNieznany => throw BladApiException.Walidacja("wybrany_dzial_id", "DZIAL_NIEZNANY", ErrorCatalog.KomunikatReguly("DZIAL_NIEZNANY")),
            _ => throw new BladApiException(
                KodBledu.KonfliktWersji,
                szczegoly: new Dictionary<string, object?> { ["aktualny"] = DoWyjscia(wynik.Widok) }),
        };
    }

    /// <summary>Merge Patch: <c>null</c> dozwolone tylko dla <c>termin_egzaminu</c> i <c>wybrany_dzial_id</c> (ADR-072 p. 3).</summary>
    private static void WymagajMozliwosciZmiany(ZmianaUczniaWejscie wejscie)
    {
        var bledy = new List<BladPola>();
        if (wejscie.PoziomMatury is { Obecne: true, Wartosc: null })
        {
            bledy.Add(new BladPola("poziom_matury", "NIE_MOZNA_WYCZYSCIC", ErrorCatalog.KomunikatReguly("NIE_MOZNA_WYCZYSCIC")));
        }

        if (wejscie.ZgodaNaTrening is { Obecne: true, Wartosc: null })
        {
            bledy.Add(new BladPola("zgoda_na_trening", "NIE_MOZNA_WYCZYSCIC", ErrorCatalog.KomunikatReguly("NIE_MOZNA_WYCZYSCIC")));
        }

        if (bledy.Count > 0)
        {
            throw new BladApiException(KodBledu.Walidacja, null, null, bledy);
        }
    }

    // POC (U-21): test diagnostyczny zawsze BRAK, Klasy zawsze puste.
    private static UczenWyjscie DoWyjscia(UczenWidok widok) =>
        new(
            widok.Id,
            widok.Email,
            widok.Imie,
            widok.Nazwisko,
            widok.PoziomMatury,
            widok.TerminEgzaminu,
            widok.WybranyDzialId,
            widok.ZgodaNaTrening,
            StanTestuDiagnostycznego.Brak,
            [],
            widok.Version);
}
