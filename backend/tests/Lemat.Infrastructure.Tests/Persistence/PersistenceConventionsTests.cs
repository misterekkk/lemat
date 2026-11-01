// @Role: Testy konwencji trwałości: typy wyliczeniowe PostgreSQL (rejestr 43 typów, translator UPPER_SNAKE), identyfikatory silnie typowane, jsonb, znaczniki czasu | @DependsOn: BazaTestowa, MapowanieTypowWyliczeniowych, KonwersjeIdentyfikatorow, JsonbConverter | @UsedBy: dotnet test | @Task: TASK-015
using System.Text.Json;
using Lemat.Domain.Common;
using Lemat.Infrastructure.Persistence.Conventions;
using Lemat.Infrastructure.Tests.Persistence.Support;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lemat.Infrastructure.Tests.Persistence;

public enum PrzykladowyKodTestowy
{
    OtwarteZDowodem,
    EwaluacjaZt1,
    IzotonicznyCv5,
    Wstepna,
}

[Collection(PostgresKolekcjaTestow.Nazwa)]
public sealed class PersistenceConventionsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private BazaTestowa _baza = null!;

    public async ValueTask InitializeAsync() => _baza = await BazaTestowa.UtworzAsync(postgres);

    public async ValueTask DisposeAsync() => await _baza.DisposeAsync();

    [Fact]
    public async Task ZapisIOdczytEnumowIIdentyfikatoraPrzezBaze()
    {
        var agregat = TestowyAgregat.Nowy("pierwszy");
        agregat.Poziom = PoziomMatury.Rozszerzony;
        agregat.Rodzaj = RodzajZadania.OtwarteZDowodem;
        await using (var zapis = _baza.Kontekst())
        {
            zapis.Add(agregat);
            await zapis.SaveChangesAsync(Ct);
        }

        (await _baza.SkalarAsync<string>($"SELECT rodzaj::text FROM test_agregat WHERE id = '{agregat.Id.Wartosc}'")).ShouldBe("OTWARTE_Z_DOWODEM");
        (await _baza.SkalarAsync<string>($"SELECT poziom::text FROM test_agregat WHERE id = '{agregat.Id.Wartosc}'")).ShouldBe("ROZSZERZONY");
        await using var odczyt = _baza.Kontekst();
        var wczytany = await odczyt.Set<TestowyAgregat>().SingleAsync(a => a.Id == agregat.Id, Ct);
        wczytany.Id.ShouldBe(agregat.Id);
        wczytany.Poziom.ShouldBe(PoziomMatury.Rozszerzony);
        wczytany.Rodzaj.ShouldBe(RodzajZadania.OtwarteZDowodem);
        wczytany.Version.ShouldBe(1);
    }

    [Fact]
    public async Task RejestrTypowWyliczeniowychZgodnyZKatalogiemBazy()
    {
        MapowanieTypowWyliczeniowych.NazwyTypowDdl.Count.ShouldBe(43);
        MapowanieTypowWyliczeniowych.NazwyTypowDdl.Distinct().Count().ShouldBe(43);

        await using var polecenie = _baza.Zrodlo.CreateCommand(
            "SELECT t.typname FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typtype = 'e'");
        var wBazie = new List<string>();
        await using (var czytnik = await polecenie.ExecuteReaderAsync(Ct))
        {
            while (await czytnik.ReadAsync(Ct))
            {
                wBazie.Add(czytnik.GetString(0));
            }
        }

        wBazie.Order(StringComparer.Ordinal).ShouldBe(MapowanieTypowWyliczeniowych.NazwyTypowDdl.Order(StringComparer.Ordinal));

        MapowanieTypowWyliczeniowych.Lista.Count.ShouldBeGreaterThanOrEqualTo(2);
        foreach (var (typ, nazwaPg) in MapowanieTypowWyliczeniowych.Lista)
        {
            var wartosciBazy = await _baza.SkalarAsync<string>($"SELECT string_agg(e::text, ',') FROM unnest(enum_range(NULL::{nazwaPg})) AS e");
            var wartosciClr = Enum.GetNames(typ).Select(n => TlumaczNazwUpperSnake.Instancja.TranslateMemberName(n));
            wartosciBazy.Split(',').ShouldBe(wartosciClr, $"enum {typ.Name} vs {nazwaPg}");
        }
    }

    [Fact]
    public void ZarejestrujOdrzucaNieznanyTypPostgresIPodwojneMapowanie()
    {
        Should.Throw<ArgumentException>(() => MapowanieTypowWyliczeniowych.Zarejestruj(typeof(PoziomMatury), "nie_istnieje"));
        Should.Throw<ArgumentException>(() => MapowanieTypowWyliczeniowych.Zarejestruj(typeof(string), "status_testu"));
        Should.Throw<InvalidOperationException>(() => MapowanieTypowWyliczeniowych.Zarejestruj(typeof(PoziomMatury), "rodzaj_zadania"));
        Should.NotThrow(() => MapowanieTypowWyliczeniowych.Zarejestruj(typeof(PoziomMatury), "poziom_matury"));
    }

    [Theory]
    [InlineData("OtwarteZDowodem", "OTWARTE_Z_DOWODEM")]
    [InlineData("EwaluacjaZt1", "EWALUACJA_ZT1")]
    [InlineData("IzotonicznyCv5", "IZOTONICZNY_CV5")]
    [InlineData("WniosekOUsuniecie", "WNIOSEK_O_USUNIECIE")]
    public void TlumaczNazwDajeWartosciUpperSnake(string clr, string oczekiwana)
    {
        TlumaczNazwUpperSnake.Instancja.TranslateMemberName(clr).ShouldBe(oczekiwana);
        TlumaczNazwUpperSnake.Instancja.TranslateTypeName("StatusRozwiazania").ShouldBe("status_rozwiazania");
    }

    [Theory]
    [InlineData(PrzykladowyKodTestowy.OtwarteZDowodem, "OTWARTE_Z_DOWODEM")]
    [InlineData(PrzykladowyKodTestowy.EwaluacjaZt1, "EWALUACJA_ZT1")]
    [InlineData(PrzykladowyKodTestowy.IzotonicznyCv5, "IZOTONICZNY_CV5")]
    [InlineData(PrzykladowyKodTestowy.Wstepna, "WSTEPNA")]
    public void KonwerterEnumaTekstowegoRoundTrip(PrzykladowyKodTestowy wartosc, string tekst)
    {
        var konwerter = new UpperSnakeEnumConverter<PrzykladowyKodTestowy>();

        konwerter.ConvertToProvider(wartosc).ShouldBe(tekst);
        konwerter.ConvertFromProvider(tekst).ShouldBe(wartosc);
    }

    [Fact]
    public void KazdyIdentyfikatorMaKonwerterIZnajdujeIdentyfikatoryDomenyITestowe()
    {
        var znalezione = KonwersjeIdentyfikatorow.ZnajdzIdentyfikatory();

        znalezione.ShouldContain(p => p.TypId == typeof(TestowyAgregatId) && p.TypWartosci == typeof(Guid));
        using var kontekst = _baza.Kontekst();
        var wlasciwosc = kontekst.Model.FindEntityType(typeof(TestowyAgregat))!.FindProperty(nameof(TestowyAgregat.Id))!;
        wlasciwosc.GetValueConverter().ShouldNotBeNull();
        wlasciwosc.GetColumnType().ShouldBe("uuid");
    }

    [Fact]
    public void KonwerterIdentyfikatoraRoundTrip()
    {
        var konwerter = new IdentyfikatorConverter<TestowyAgregatId, Guid>();
        var id = new TestowyAgregatId(Guid.CreateVersion7());

        konwerter.ConvertToProvider(id).ShouldBe(id.Wartosc);
        konwerter.ConvertFromProvider(id.Wartosc).ShouldBe(id);
    }

    [Fact]
    public async Task JsonbRekordIDokumentRoundTripZSnakeCaseIBezEscapowania()
    {
        var agregat = TestowyAgregat.Nowy("json");
        agregat.Dane = new DaneTestowe("a < b && c > d", 7);
        agregat.Dokument = JsonDocument.Parse("""{"dowolne":[1,2,{"x":"y"}]}""");
        agregat.Wpisy.Add(new WpisTestowy { Tresc = "wpis 1" });
        await using (var zapis = _baza.Kontekst())
        {
            zapis.Add(agregat);
            await zapis.SaveChangesAsync(Ct);
        }

        (await _baza.SkalarAsync<string>($"SELECT dane->>'pole_dlugie_ze_znakami' FROM test_agregat WHERE id = '{agregat.Id.Wartosc}'")).ShouldBe("a < b && c > d");
        (await _baza.SkalarAsync<string>($"SELECT dokument->'dowolne'->2->>'x' FROM test_agregat WHERE id = '{agregat.Id.Wartosc}'")).ShouldBe("y");
        await using var odczyt = _baza.Kontekst();
        var wczytany = await odczyt.Set<TestowyAgregat>().SingleAsync(a => a.Id == agregat.Id, Ct);
        wczytany.Dane.ShouldBe(new DaneTestowe("a < b && c > d", 7));
        wczytany.Dokument!.RootElement.GetProperty("dowolne").GetArrayLength().ShouldBe(3);
        wczytany.Wpisy.Select(w => w.Tresc).ShouldBe(["wpis 1"]);
    }

    [Fact]
    public async Task ZnacznikCzasuUstawiaUpdatedAtTylkoPrzyModyfikacji()
    {
        var agregat = TestowyAgregat.Nowy("czas");
        await using var kontekst = _baza.Kontekst();
        kontekst.Add(agregat);
        await kontekst.SaveChangesAsync(Ct);
        agregat.UpdatedAt.ShouldBe(default); // Added: wartość z bazy (DEFAULT now()) — interceptor nie dotyka

        _baza.Zegar.Teraz = new DateTimeOffset(2030, 5, 5, 10, 0, 0, TimeSpan.Zero);
        agregat.Nazwa = "czas2";
        await kontekst.SaveChangesAsync(Ct);

        (await _baza.SkalarAsync<DateTime>($"SELECT updated_at FROM test_agregat WHERE id = '{agregat.Id.Wartosc}'"))
            .ShouldBe(new DateTime(2030, 5, 5, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ZrodloDanychNpgsqlCzytaEnumJakoTypCSharp()
    {
        await using var polecenie = _baza.Zrodlo.CreateCommand("SELECT 'ROZSZERZONY'::poziom_matury");

        (await polecenie.ExecuteScalarAsync(Ct)).ShouldBe(PoziomMatury.Rozszerzony);
    }
}
