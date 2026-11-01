// @Role: Testy integracyjne schematu: obiekty z ddl.md na pustej bazie, brak konfiguracji Mock, idempotencja migrate, smoke funkcji, deterministyczność extract-ddl | @DependsOn: PostgresFixture, MigratorBazy, MigrujBazeCommand, ZasobSql, backend/tools/extract-ddl.sh | @UsedBy: dotnet test | @Task: TASK-013
using System.Diagnostics;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Migrations;
using Lemat.Tools.Commands;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class SchematBazyTests(PostgresFixture postgres)
{
    // Liczby z nagłówka docs/spec/ddl.md (wersja 3.5): 61 tabel, 43 typy wyliczeniowe, 15 triggerów.
    private const int OczekiwanaLiczbaTabel = 61;
    private const int OczekiwanaLiczbaTypowWyliczeniowych = 43;
    private const int OczekiwanaLiczbaTriggerow = 15;
    private const int OczekiwanaLiczbaPartycji = MigratorBazy.MiesiecyNaprzod + 1;

    private static readonly TimeProvider Zegar = TimeProvider.System;

    private async Task<string> MigrowanaBazaAsync()
    {
        var polaczenie = await postgres.UtworzPustaBazeAsync();
        await MigratorBazy.MigrujAsync(polaczenie, Zegar);
        return polaczenie;
    }

    private static async Task<T> SkalarAsync<T>(string polaczenie, string sql)
    {
        await using var zrodlo = NpgsqlDataSource.Create(polaczenie);
        await using var polecenie = zrodlo.CreateCommand(sql);
        return (T)(await polecenie.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task WszystkieObiektyZDdlIstniejaNaPustejBazie()
    {
        var polaczenie = await MigrowanaBazaAsync();

        (await SkalarAsync<long>(polaczenie,
            "SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace " +
            "WHERE n.nspname = 'public' AND c.relkind IN ('r','p') AND NOT c.relispartition AND c.relname <> '__historia_migracji'"))
            .ShouldBe(OczekiwanaLiczbaTabel);
        (await SkalarAsync<long>(polaczenie,
            "SELECT count(*) FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typtype = 'e'"))
            .ShouldBe(OczekiwanaLiczbaTypowWyliczeniowych);
        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM pg_trigger WHERE NOT tgisinternal AND tgparentid = 0"))
            .ShouldBe(OczekiwanaLiczbaTriggerow);
        (await SkalarAsync<long>(polaczenie,
            "SELECT count(*) FROM pg_inherits i JOIN pg_class p ON p.oid = i.inhparent WHERE p.relname = 'decyzja_modelu'"))
            .ShouldBe(OczekiwanaLiczbaPartycji);
        (await SkalarAsync<long>(polaczenie,
            "SELECT count(*) FROM pg_extension WHERE extname IN ('citext','pg_trgm')")).ShouldBe(2);
        (await SkalarAsync<long>(polaczenie,
            "SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace WHERE n.nspname = 'public' AND p.proname IN " +
            "('dodaj_dni_robocze','poziom_bez_histerezy','poziom_z_b','utworz_partycje_decyzji','ponow_prace','odzyskaj_dzierzawy','zarezerwuj_klucz_idempotencji')"))
            .ShouldBe(7);
        (await SkalarAsync<long>(polaczenie,
            "SELECT count(*) FROM pg_roles WHERE rolname IN ('lemat_api','lemat_worker','lemat_scheduler')")).ShouldBe(3);
    }

    [Fact]
    public async Task MigracjeNaPustejBazieNieTworzaKonfiguracjiMock()
    {
        var polaczenie = await MigrowanaBazaAsync();

        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM konfiguracja_modelu")).ShouldBe(0);
    }

    [Fact]
    public async Task MigracjaZapisujeIdentyfikatorWHistoriiMigracji()
    {
        var polaczenie = await MigrowanaBazaAsync();

        (await SkalarAsync<string>(polaczenie, "SELECT string_agg(migration_id, ',') FROM __historia_migracji"))
            .ShouldBe("0001_SchematPoczatkowy");
    }

    [Fact]
    public async Task PolecenieMigrateUruchomioneDwukrotnieNieZmieniaBazy()
    {
        var polaczenie = await postgres.UtworzPustaBazeAsync();
        var polecenie = new MigrujBazeCommand();
        using var wyjscie = new StringWriter();

        (await polecenie.WykonajAsync([$"--polaczenie={polaczenie}"], wyjscie, CancellationToken.None)).ShouldBe(0);
        var po1 = await MigawkaKataloguAsync(polaczenie);
        (await polecenie.WykonajAsync([$"--polaczenie={polaczenie}"], wyjscie, CancellationToken.None)).ShouldBe(0);
        var po2 = await MigawkaKataloguAsync(polaczenie);

        po2.ShouldBe(po1);
        wyjscie.ToString().ShouldContain("zastosowano 1");
        wyjscie.ToString().ShouldContain("zastosowano 0");
    }

    [Fact]
    public async Task PolecenieMigrateWybieraPolaczenieZeZmiennychSrodowiskowych()
    {
        var polaczenie = await postgres.UtworzPustaBazeAsync();
        var zmienne = new System.Collections.Hashtable { ["LEMAT_BAZA_MIGRACJE"] = polaczenie, ["LEMAT_BAZA"] = "Host=nieistnieje" };
        using var wyjscie = new StringWriter();

        (await new MigrujBazeCommand(zmienne).WykonajAsync([], wyjscie, CancellationToken.None)).ShouldBe(0);

        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM __historia_migracji")).ShouldBe(1);
    }

    [Fact]
    public async Task PartycjeDecyzjiIstniejaNaDwanascieMiesiecyNaprzod()
    {
        var polaczenie = await MigrowanaBazaAsync();
        var teraz = Zegar.GetUtcNow();
        var pierwszy = new DateOnly(teraz.Year, teraz.Month, 1);

        for (var m = 0; m <= MigratorBazy.MiesiecyNaprzod; m++)
        {
            var nazwa = $"decyzja_modelu_{pierwszy.AddMonths(m):yyyy_MM}";
            (await SkalarAsync<long>(polaczenie, $"SELECT count(*) FROM pg_class WHERE relname = '{nazwa}' AND relispartition"))
                .ShouldBe(1, nazwa);
        }
    }

    [Fact]
    public async Task UtworzPartycjeDecyzjiJestIdempotentna()
    {
        var polaczenie = await MigrowanaBazaAsync();

        await SkalarAsync<object?>(polaczenie, "SELECT utworz_partycje_decyzji(date '2031-05-17')::text");
        await SkalarAsync<object?>(polaczenie, "SELECT utworz_partycje_decyzji(date '2031-05-17')::text");

        var granice = await SkalarAsync<string>(polaczenie,
            "SELECT pg_get_expr(c.relpartbound, c.oid) FROM pg_class c WHERE c.relname = 'decyzja_modelu_2031_05'");
        granice.ShouldContain("'2031-05-01");
        granice.ShouldContain("'2031-06-01");
    }

    [Theory]
    [InlineData("2026-03-06 18:00+01", 3, "2026-03-11 18:00+01")] // piątek 18:00 + 3 -> środa 18:00
    [InlineData("2026-03-07 10:00+01", 3, "2026-03-12 08:00+01")] // sobota 10:00 + 3 -> czwartek 08:00
    [InlineData("2026-03-09 09:30+01", 0, "2026-03-09 09:30+01")] // dzień roboczy + 0 -> bez zmian
    public async Task DodajDniRoboczeLiczyWgKalendarza(string od, int dni, string oczekiwane)
    {
        var polaczenie = await MigrowanaBazaAsync();

        var wynik = await SkalarAsync<DateTime>(polaczenie,
            $"SELECT dodaj_dni_robocze(timestamptz '{od}', {dni}) AT TIME ZONE 'UTC'");

        var oczekiwany = DateTimeOffset.Parse(oczekiwane, System.Globalization.CultureInfo.InvariantCulture).UtcDateTime;
        wynik.ShouldBe(oczekiwany);
    }

    [Fact]
    public async Task DodajDniRoboczePomijaDzienWolnyZTabeli()
    {
        var polaczenie = await MigrowanaBazaAsync();
        await SkalarAsync<object?>(polaczenie,
            "WITH w AS (INSERT INTO dzien_wolny (data, nazwa) VALUES (date '2026-03-10', 'test') RETURNING 1) SELECT count(*) FROM w");

        // piątek 2026-03-06 18:00 + 3 dni robocze, wtorek 03-10 wolny -> czwartek 03-12 18:00
        var wynik = await SkalarAsync<DateTime>(polaczenie,
            "SELECT dodaj_dni_robocze(timestamptz '2026-03-06 18:00+01', 3) AT TIME ZONE 'UTC'");

        wynik.ShouldBe(new DateTime(2026, 3, 12, 17, 0, 0, DateTimeKind.Unspecified));
    }

    [Fact]
    public async Task DodajDniRoboczeOdrzucaUjemnaLiczbeDni()
    {
        var polaczenie = await MigrowanaBazaAsync();

        var blad = await Should.ThrowAsync<PostgresException>(() =>
            SkalarAsync<object?>(polaczenie, "SELECT dodaj_dni_robocze(now(), -1)::text"));

        blad.MessageText.ShouldBe("DNI_ROBOCZE_UJEMNE");
    }

    [Fact]
    public void WszystkiePlikiSqlSaOsadzoneIMigracjaIchUzywa()
    {
        ZasobSql.Nazwy().ShouldBe(SchematPoczatkowy.PlikiSql);
        foreach (var plik in SchematPoczatkowy.PlikiSql)
        {
            ZasobSql.Wczytaj(plik).ShouldNotBeNullOrWhiteSpace();
        }

        var polaczone = string.Concat(SchematPoczatkowy.PlikiSql.Select(ZasobSql.Wczytaj));
        polaczone.ShouldBe(WczytajOsadzonyDdl());
    }

    [Fact]
    public void ExtractDdlJestDeterministycznyIZgodnyZRepozytorium()
    {
        var skrypt = Path.Combine(KatalogBackend(), "tools", "extract-ddl.sh");
        File.Exists(skrypt).ShouldBeTrue();

        var wynik = Uruchom("bash", skrypt, "--check");

        wynik.Kod.ShouldBe(0, wynik.Wyjscie);
    }

    private static string WczytajOsadzonyDdl()
    {
        var zespol = typeof(ZasobSql).Assembly;
        using var strumien = zespol.GetManifestResourceStream("Lemat.Infrastructure.Persistence.Schema.ddl.sql")!;
        using var czytnik = new StreamReader(strumien);
        return czytnik.ReadToEnd();
    }

    private static async Task<string> MigawkaKataloguAsync(string polaczenie) =>
        await SkalarAsync<string>(polaczenie,
            "SELECT string_agg(x, '|' ORDER BY x) FROM (" +
            " SELECT 'rel:' || c.relname::text || ':' || c.relkind::text AS x FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public'" +
            " UNION ALL SELECT 'con:' || conname::text FROM pg_constraint" +
            " UNION ALL SELECT 'proc:' || proname::text FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace WHERE n.nspname = 'public'" +
            " UNION ALL SELECT 'hist:' || migration_id FROM __historia_migracji) s");

    private static string KatalogBackend()
    {
        var katalog = new DirectoryInfo(AppContext.BaseDirectory);
        while (katalog is not null && !File.Exists(Path.Combine(katalog.FullName, "Lemat.slnx")))
        {
            katalog = katalog.Parent;
        }

        return katalog?.FullName ?? throw new InvalidOperationException("Nie znaleziono katalogu backend (Lemat.slnx).");
    }

    private static (int Kod, string Wyjscie) Uruchom(string program, params string[] argumenty)
    {
        var info = new ProcessStartInfo(program) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in argumenty)
        {
            info.ArgumentList.Add(argument);
        }

        using var proces = Process.Start(info)!;
        var wyjscie = proces.StandardOutput.ReadToEnd() + proces.StandardError.ReadToEnd();
        proces.WaitForExit();
        return (proces.ExitCode, wyjscie);
    }
}
