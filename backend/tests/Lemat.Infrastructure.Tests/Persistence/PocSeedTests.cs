// @Role: Testy polecenia seed-poc: 13 Działów, 5 Umiejętności, 5 Zadań z identyfikatorami kontraktu, suma punktów Kryteriów, 2 konta AKTYWNE, idempotencja, odmowa w Production, zbalansowanie $…$ i nawiasów | @DependsOn: PostgresFixture, SeedPocCommand, DaneStartowePoc, MigratorBazy, HaszowanieHasel | @UsedBy: dotnet test | @Task: TASK-122
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Seeding;
using Lemat.Infrastructure.Security;
using Lemat.Tools.Commands;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class PocSeedTests(PostgresFixture postgres)
{
    private static readonly string[] OczekiwaneIdZadan =
    [
        "a1000000-0000-4000-8000-000000000001",
        "a1000000-0000-4000-8000-000000000002",
        "a1000000-0000-4000-8000-000000000003",
        "a1000000-0000-4000-8000-000000000004",
        "a1000000-0000-4000-8000-000000000005",
    ];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<string> MigrowanaBazaAsync()
    {
        var polaczenie = await postgres.UtworzPustaBazeAsync();
        await MigratorBazy.MigrujAsync(polaczenie, TimeProvider.System, Ct);
        return polaczenie;
    }

    private static async Task<T> SkalarAsync<T>(string polaczenie, string sql)
    {
        await using var zrodlo = NpgsqlDataSource.Create(polaczenie);
        await using var polecenie = zrodlo.CreateCommand(sql);
        return (T)(await polecenie.ExecuteScalarAsync(Ct))!;
    }

    private static async Task<int> UruchomAsync(string polaczenie, StringWriter wyjscie, System.Collections.IDictionary? zmienne = null) =>
        await new SeedPocCommand(zmienne ?? new System.Collections.Hashtable()).WykonajAsync([$"--polaczenie={polaczenie}"], wyjscie, Ct);

    [Fact]
    public async Task SeedPocNaPustejBazieTworzyDaneZKontraktu()
    {
        var polaczenie = await MigrowanaBazaAsync();
        using var wyjscie = new StringWriter();

        (await UruchomAsync(polaczenie, wyjscie)).ShouldBe(0);

        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM dzial")).ShouldBe(13);
        (await SkalarAsync<string>(polaczenie, "SELECT string_agg(id::text, ',' ORDER BY id) FROM dzial")).ShouldBe("1,2,3,4,5,6,7,8,9,10,11,12,13");
        (await SkalarAsync<bool>(polaczenie, "SELECT tylko_rozszerzony FROM dzial WHERE id = 13")).ShouldBeTrue();
        (await SkalarAsync<string>(polaczenie, "SELECT string_agg(id::text, ',' ORDER BY id) FROM umiejetnosc")).ShouldBe("101,301,502,701,801");
        (await SkalarAsync<string>(polaczenie, "SELECT string_agg(id::text, ',' ORDER BY id) FROM zadanie WHERE status = 'OPUBLIKOWANE' AND zasieg = 'GLOBALNY' AND rodzaj = 'OTWARTE_OBLICZENIOWE'"))
            .ShouldBe(string.Join(',', OczekiwaneIdZadan));
        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM zadanie z JOIN zadanie_wersja w ON w.id = z.aktualna_wersja_id AND w.zadanie_id = z.id AND w.nr_wersji = 1 AND w.id::text LIKE 'b1000000-0000-4000-8000-00000000000%'"))
            .ShouldBe(5);
        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM zadanie_wersja w WHERE w.punkty_max <> (SELECT sum(k.punkty_max) FROM kryterium_oceniania k WHERE k.zadanie_wersja_id = w.id)"))
            .ShouldBe(0);
        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM uzytkownik WHERE rola = 'UCZEN' AND status = 'AKTYWNE' AND email IN ('uczen1@lemat.local', 'uczen2@lemat.local')"))
            .ShouldBe(2);
        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM uczen u JOIN profil_ucznia p ON p.uczen_id = u.uzytkownik_id JOIN sciezka_nauki s ON s.uczen_id = u.uzytkownik_id"))
            .ShouldBe(2);
        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM konfiguracja_modelu WHERE aktywna AND dostawca = 'mock' AND rola IN ('ROZPOZNAWANIE_PISMA', 'DECYZYJNY_GLOWNY')"))
            .ShouldBe(2);
        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM kalibrator_pewnosci WHERE metoda = 'TOZSAMOSCIOWY' AND rodzaj_zadania = 'OTWARTE_OBLICZENIOWE'")).ShouldBe(1);
        (await SkalarAsync<decimal>(polaczenie, "SELECT wartosc FROM prog_pewnosci WHERE aktywny AND rodzaj_zadania = 'OTWARTE_OBLICZENIOWE'")).ShouldBe(0.95m);

        var tekst = wyjscie.ToString();
        tekst.ShouldContain("uczen1@lemat.local");
        tekst.ShouldContain("uczen2@lemat.local");
    }

    [Fact]
    public async Task SeedPocUruchomionyDwukrotnieNieDuplikujeDanych()
    {
        var polaczenie = await MigrowanaBazaAsync();
        using var pierwsze = new StringWriter();
        using var drugie = new StringWriter();
        const string Migawka = "SELECT (SELECT count(*) FROM dzial) || '/' || (SELECT count(*) FROM umiejetnosc) || '/' || (SELECT count(*) FROM zadanie) || '/' || (SELECT count(*) FROM zadanie_wersja) || '/' || (SELECT count(*) FROM kryterium_oceniania) || '/' || (SELECT count(*) FROM uzytkownik) || '/' || (SELECT count(*) FROM konfiguracja_modelu) || '/' || (SELECT count(*) FROM kalibrator_pewnosci) || '/' || (SELECT count(*) FROM prog_pewnosci)";

        (await UruchomAsync(polaczenie, pierwsze)).ShouldBe(0);
        var po1 = await SkalarAsync<string>(polaczenie, Migawka);
        (await UruchomAsync(polaczenie, drugie)).ShouldBe(0);
        var po2 = await SkalarAsync<string>(polaczenie, Migawka);

        po2.ShouldBe(po1);
        po1.ShouldBe("13/5/5/5/11/2/2/1/1");
        drugie.ToString().ShouldContain("dodano Działy 0, Umiejętności 0, Zadania 0, konta 0, konfiguracje i kalibrację 0");
    }

    [Fact]
    public async Task SeedPocOdmawiaWProductionIniczegoNieZapisuje()
    {
        var polaczenie = await MigrowanaBazaAsync();
        using var wyjscie = new StringWriter();
        var zmienne = new System.Collections.Hashtable { ["LEMAT_SRODOWISKO"] = "Production" };

        (await UruchomAsync(polaczenie, wyjscie, zmienne)).ShouldBe(1);

        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM dzial")).ShouldBe(0);
        (await SkalarAsync<long>(polaczenie, "SELECT count(*) FROM uzytkownik")).ShouldBe(0);
        wyjscie.ToString().ShouldContain("Production");
        wyjscie.ToString().ShouldNotContain("Lemat-Demo-2026!");
    }

    [Fact]
    public async Task HasloDemoJestZapisaneWylacznieJakoSkrotArgon2id()
    {
        var polaczenie = await MigrowanaBazaAsync();
        using var wyjscie = new StringWriter();
        await UruchomAsync(polaczenie, wyjscie);

        var skrot = await SkalarAsync<string>(polaczenie, "SELECT haslo_hash FROM uzytkownik WHERE email = 'uczen1@lemat.local'");

        skrot.ShouldStartWith("$argon2id$");
        skrot.ShouldNotContain("Lemat-Demo-2026!");
        new HaszowanieHasel().Weryfikuj("Lemat-Demo-2026!", skrot).ShouldBeTrue();
        new HaszowanieHasel().Weryfikuj("inne-haslo-123", skrot).ShouldBeFalse();
    }

    [Fact]
    public void TrescZadanIOpisyKryteriowMajaZbalansowaneDolaryINawiasy()
    {
        var dane = DaneStartowePoc.Wczytaj();

        dane.Zadania.Count.ShouldBe(5);
        foreach (var zadanie in dane.Zadania)
        {
            Zbalansowany(zadanie.TrescMd).ShouldBeTrue(zadanie.TrescMd);
            zadanie.Kryteria.Sum(k => k.PunktyMax).ShouldBe(zadanie.PunktyMax);
            foreach (var kryterium in zadanie.Kryteria)
            {
                Zbalansowany(kryterium.Opis).ShouldBeTrue(kryterium.Opis);
            }
        }
    }

    [Fact]
    public void DaneStartoweZawieraTrzynascieDzialowIIdentyfikatoryZKontraktu()
    {
        var dane = DaneStartowePoc.Wczytaj();

        dane.Dzialy.Select(d => d.Id).ShouldBe(Enumerable.Range(1, 13));
        dane.Zadania.Select(z => z.ZadanieId.ToString()).ShouldBe(OczekiwaneIdZadan);
        dane.Konta.Select(k => k.Id.ToString()).ShouldBe(["c1000000-0000-4000-8000-000000000001", "c1000000-0000-4000-8000-000000000002"]);
    }

    /// <summary>Parzysta liczba nieucieczkowanych <c>$</c> oraz zbalansowane <c>()</c>, <c>[]</c>, <c>{}</c> (nawiasy <c>\{</c> i <c>\}</c> pomijane).</summary>
    private static bool Zbalansowany(string tekst)
    {
        var stos = new Stack<char>();
        var dolary = 0;
        for (var i = 0; i < tekst.Length; i++)
        {
            var znak = tekst[i];
            if (znak == '\\')
            {
                i++;
                continue;
            }

            switch (znak)
            {
                case '$':
                    dolary++;
                    break;
                case '(' or '[' or '{':
                    stos.Push(znak);
                    break;
                case ')' or ']' or '}':
                    var otwierajacy = znak switch { ')' => '(', ']' => '[', _ => '{' };
                    if (stos.Count == 0 || stos.Pop() != otwierajacy)
                    {
                        return false;
                    }

                    break;
                default:
                    break;
            }
        }

        return dolary % 2 == 0 && stos.Count == 0;
    }
}
