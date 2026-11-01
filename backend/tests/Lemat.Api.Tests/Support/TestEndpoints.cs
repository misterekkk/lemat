// @Role: Endpointy pomocnicze wyłącznie dla testów infrastruktury HTTP (błędy domenowe, wiązanie, kursor, If-Match, Mock, baza) | @DependsOn: IGrupaEndpointow, Lemat.Api.Errors, KodekKursora, NaglowekIfMatch, FiltrScenariuszaMock | @UsedBy: Lemat.Api.Tests | @Task: TASK-121
using Lemat.Api.Endpoints;
using Lemat.Api.Http;
using Lemat.Api.Pagination;
using Lemat.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Npgsql;

namespace Lemat.Api.Tests.Support;

public enum StatusTestowy
{
    Wstepna,
    OczekujeNaDecyzje,
}

public sealed record WejscieTestowe(string Haslo, bool OswiadczenieWieku16, int LiczbaWynikow7d, StatusTestowy Status);

public sealed record ZasobTestowy(Guid Id, long Version, string Nazwa);

public sealed record PozycjaKursora(int Pozycja);

public sealed class TestoweEndpointy : IGrupaEndpointow
{
    public static readonly ZasobTestowy Zasob = new(Guid.Parse("d1000000-0000-4000-8000-000000000001"), 3, "Test");

    public void Mapuj(IEndpointRouteBuilder trasy)
    {
        trasy.MapGet("/api/v1/test/domena", () => Results.Ok(Fail(new NiezmiennikNaruszonyException("ROZWIAZANIE_NIEPRAWIDLOWY_STAN"))));
        trasy.MapGet("/api/v1/test/nieznany-kod-domeny", () => Results.Ok(Fail(new NiezmiennikNaruszonyException("KOD_SPOZA_KATALOGU"))));
        trasy.MapGet("/api/v1/test/wyjatek", () => Results.Ok(Fail(new InvalidOperationException("tajny szczegol techniczny"))));
        trasy.MapPost("/api/v1/test/wejscie", (WejscieTestowe wejscie) => Results.Ok(wejscie));
        trasy.MapGet("/api/v1/test/strona", (int? limit, string? kursor, KodekKursora kodek) =>
        {
            var zapytanie = ZapytanieStrony.Z(limit, kursor);
            var start = zapytanie.Kursor is null ? 0 : kodek.Odkoduj<PozycjaKursora>(zapytanie.Kursor).Pozycja;
            var elementy = Enumerable.Range(start, zapytanie.Limit).ToList();
            return Results.Ok(new Strona<int>(elementy, kodek.Zakoduj(new PozycjaKursora(start + zapytanie.Limit))));
        });
        trasy.MapPatch("/api/v1/test/zasob", (HttpRequest zadanie) =>
        {
            NaglowekIfMatch.SprawdzZgodnosc(NaglowekIfMatch.Wymagaj(zadanie), Zasob.Version, () => Zasob);
            return Results.Ok(Zasob);
        });
        trasy.MapPost("/api/v1/test/mock", (HttpContext kontekst) => Results.Ok(new { scenariusz = FiltrScenariuszaMock.Pobierz(kontekst) }))
            .AddEndpointFilter<FiltrScenariuszaMock>();
        trasy.MapGet("/api/v1/test/baza", async (NpgsqlDataSource zrodlo, CancellationToken token) =>
        {
            await using var polecenie = zrodlo.CreateCommand("SELECT count(*) FROM dzial");
            return Results.Ok(new { liczba = (long)(await polecenie.ExecuteScalarAsync(token))! });
        });
    }

    private static object Fail(Exception wyjatek) => throw wyjatek;
}
