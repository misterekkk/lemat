// @Role: Rejestr typów wyliczeniowych PostgreSQL (43 z ddl.md) i mapowanie na enumy C# dla źródła danych Npgsql oraz modelu EF Core (ef-core.md 3.2) | @DependsOn: TlumaczNazwUpperSnake, Npgsql, Lemat.Domain.Common | @UsedBy: PersistenceRegistration, LematDbContext, testy | @Task: TASK-015, TASK-016, TASK-017, TASK-018
using System.Collections.Concurrent;
using Lemat.Domain.Accounts;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Models;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Lemat.Infrastructure.Persistence.Conventions;

/// <summary>
/// Jedno źródło prawdy o mapowaniu typów wyliczeniowych. <see cref="NazwyTypowDdl"/> to wszystkie 43 typy z <c>ddl.md</c>;
/// <see cref="Lista"/> zawiera te z nich, których enum C# już istnieje. Moduł dodaje enum zadaniem własnego TASK przez
/// <see cref="Zarejestruj"/> (jedna linia w statycznym konstruktorze modułu) albo wpisem w <see cref="Poczatkowe"/>.
/// </summary>
public static class MapowanieTypowWyliczeniowych
{
    /// <summary>43 typy <c>CREATE TYPE … AS ENUM</c> z <c>docs/spec/ddl.md</c> (kolejność jak w DDL).</summary>
    public static readonly IReadOnlyList<string> NazwyTypowDdl =
    [
        "rola_uzytkownika", "status_konta", "poziom_matury", "rodzaj_zadania", "forma_zamknieta",
        "pochodzenie_zadania", "zasieg_zadania", "status_zadania", "status_wkladki", "status_rozwiazania",
        "zrodlo_transkrypcji", "status_oceny", "zrodlo_oceny", "typ_bledu", "zrodlo_wskazowki",
        "powod_weryfikacji", "status_weryfikacji", "powod_anulowania", "typ_kolejki_weryfikacji", "typ_elementu_sciezki",
        "cel_elementu", "status_elementu_sciezki", "strategia_rekomendacji", "rola_modelu", "typ_pytania",
        "kontekst_decyzji", "zrodlo_wyniku", "zrodlo_probki", "zrodlo_progu", "zrodlo_kalibratora",
        "powod_uniewaznienia_wyniku", "status_pracy", "status_zlecenia", "status_importu", "status_testu",
        "decyzja_przegladu", "rezultat_walidacji", "inicjalizacja_opanowania", "status_zgloszenia_zadania", "typ_zdarzenia_rodo",
        "zrodlo_zdarzenia_rodo", "typ_bezpiecznika", "powod_eskalacji",
    ];

    /// <summary>Enumy C# zarejestrowane przez TASK-015/016/017/018 (reszta dochodzi z TASK modułów).</summary>
    public static readonly IReadOnlyList<(Type Typ, string NazwaPg)> Poczatkowe =
    [
        (typeof(PoziomMatury), "poziom_matury"),
        (typeof(RodzajZadania), "rodzaj_zadania"),
        (typeof(RolaUzytkownika), "rola_uzytkownika"),
        (typeof(StatusKonta), "status_konta"),
        (typeof(PochodzenieZadania), "pochodzenie_zadania"),
        (typeof(ZasiegZadania), "zasieg_zadania"),
        (typeof(StatusZadania), "status_zadania"),
        (typeof(FormaZamknieta), "forma_zamknieta"),
        (typeof(StatusWkladki), "status_wkladki"),
        (typeof(RolaModelu), "rola_modelu"),
        (typeof(TypPytania), "typ_pytania"),
        (typeof(KontekstDecyzji), "kontekst_decyzji"),
        (typeof(ZrodloProgu), "zrodlo_progu"),
        (typeof(ZrodloKalibratora), "zrodlo_kalibratora"),
    ];

    private static readonly ConcurrentDictionary<Type, string> Rejestr = new(
        Poczatkowe.ToDictionary(p => p.Typ, p => p.NazwaPg));

    public static IReadOnlyList<(Type Typ, string NazwaPg)> Lista =>
        [.. Rejestr.Select(p => (p.Key, p.Value)).OrderBy(p => p.Value, StringComparer.Ordinal)];

    /// <summary>Rejestruje enum C# dla typu PostgreSQL z <see cref="NazwyTypowDdl"/>; powtórzenie tego samego mapowania jest dozwolone.</summary>
    public static void Zarejestruj(Type typEnum, string nazwaPg)
    {
        ArgumentNullException.ThrowIfNull(typEnum);
        ArgumentException.ThrowIfNullOrWhiteSpace(nazwaPg);
        if (!typEnum.IsEnum)
        {
            throw new ArgumentException($"Typ {typEnum.Name} nie jest enumem.", nameof(typEnum));
        }

        if (!NazwyTypowDdl.Contains(nazwaPg, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Typ PostgreSQL '{nazwaPg}' nie występuje w ddl.md.", nameof(nazwaPg));
        }

        var zapisany = Rejestr.GetOrAdd(typEnum, nazwaPg);
        if (!string.Equals(zapisany, nazwaPg, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Enum {typEnum.Name} jest już zmapowany na '{zapisany}'.");
        }
    }

    /// <summary>Źródło danych Npgsql (ADO.NET): <c>MapEnum</c> dla każdego zarejestrowanego enuma.</summary>
    public static NpgsqlDataSourceBuilder MapujTypyWyliczeniowe(this NpgsqlDataSourceBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var (typ, nazwaPg) in Lista)
        {
            builder.MapEnum(typ, nazwaPg, nameTranslator: TlumaczNazwUpperSnake.Instancja);
        }

        return builder;
    }

    /// <summary>Model EF Core: <c>MapEnum</c> w opcjach Npgsql.</summary>
    public static NpgsqlDbContextOptionsBuilder MapujTypyWyliczenioweEf(this NpgsqlDbContextOptionsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var (typ, nazwaPg) in Lista)
        {
            builder.MapEnum(typ, nazwaPg, nameTranslator: TlumaczNazwUpperSnake.Instancja);
        }

        return builder;
    }
}
