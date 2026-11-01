// @Role: Tłumaczy błędy PostgreSQL na wyjątki domenowe/aplikacyjne: P0001 -> NiezmiennikNaruszonyException(kod), 23505 -> kod z katalogu indeksów unikalnych, 40001/40P01 -> konflikt współbieżności | @DependsOn: Npgsql, Lemat.Domain.Common.NiezmiennikNaruszonyException, Lemat.Application.Common.KonfliktWspolbieznosciException | @UsedBy: JednostkaPracyEf, repozytoria (TASK-016+) | @Task: TASK-015, TASK-017, TASK-018
using Lemat.Application.Common;
using Lemat.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lemat.Infrastructure.Persistence;

/// <summary>Kody błędów 12.2 przypisane ograniczeniom unikalnym (nazwa indeksu/ograniczenia z DDL -> kod).</summary>
public static class KatalogIndeksowUnikalnych
{
    private static readonly Dictionary<string, string> Kody = new(StringComparer.Ordinal)
    {
        ["ux_jeden_proponowany"] = "ELEMENT_JUZ_PROPONOWANY",
        ["ux_rozwiazanie_element"] = "ROZWIAZANIE_NIEPRAWIDLOWY_STAN",
        ["ux_rozwiazanie_przypisanie"] = "ROZWIAZANIE_NIEPRAWIDLOWY_STAN",
        ["ux_zgloszenie_zadania_otwarte"] = "ZGLOSZENIE_ZADANIA_ISTNIEJE",
        ["ux_test_jeden"] = "TEST_JUZ_WYKONANY",
        ["uzytkownik_email_key"] = "EMAIL_ZAJETY",
        ["ux_wkladka_opublikowana"] = "WKLADKA_OPUBLIKOWANA_ISTNIEJE",
        ["klasa_kod_zaproszenia_key"] = "KOD_ZAPROSZENIA_ZAJETY",
        ["ux_konfiguracja_aktywna"] = "KONFIGURACJA_MODELU_AKTYWNA_ISTNIEJE",
        ["ux_prog_aktywny"] = "PROG_AKTYWNY_ISTNIEJE",
    };

    public static IReadOnlyDictionary<string, string> Wszystkie => Kody;

    public static string? Kod(string? nazwaOgraniczenia) =>
        nazwaOgraniczenia is not null && Kody.TryGetValue(nazwaOgraniczenia, out var kod) ? kod : null;
}

public static class TlumaczBledowBazy
{
    private const string NiezmiennikSqlState = "P0001";
    private const string NaruszenieUnikalnosciSqlState = "23505";
    private const string KonfliktSerializacjiSqlState = "40001";
    private const string ZakleszczenieSqlState = "40P01";

    /// <summary>
    /// Zwraca wyjątek docelowy albo <c>null</c>, gdy błąd nie podlega tłumaczeniu (wtedy wyjątek leci dalej bez zmian).
    /// </summary>
    public static Exception? Przetlumacz(Exception wyjatek)
    {
        ArgumentNullException.ThrowIfNull(wyjatek);
        if (wyjatek is DbUpdateConcurrencyException)
        {
            return new KonfliktWspolbieznosciException(wyjatek);
        }

        var pg = ZnajdzBladPostgres(wyjatek);
        if (pg is null)
        {
            return null;
        }

        return pg.SqlState switch
        {
            NiezmiennikSqlState when !string.IsNullOrWhiteSpace(pg.MessageText) => new NiezmiennikNaruszonyException(pg.MessageText.Trim()),
            NaruszenieUnikalnosciSqlState => KatalogIndeksowUnikalnych.Kod(pg.ConstraintName) is { } kod ? new NiezmiennikNaruszonyException(kod) : null,
            KonfliktSerializacjiSqlState or ZakleszczenieSqlState => new KonfliktWspolbieznosciException(wyjatek),
            _ => null,
        };
    }

    private static PostgresException? ZnajdzBladPostgres(Exception wyjatek)
    {
        for (var biezacy = wyjatek; biezacy is not null; biezacy = biezacy.InnerException)
        {
            if (biezacy is PostgresException pg)
            {
                return pg;
            }
        }

        return null;
    }
}
