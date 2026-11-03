// @Role: Testy integracyjne POST/GET /rozwiazania, adresu zdjęcia i GET /magazyn-lokalny/{token} (kontrakt POC 7.4 … 7.6, 7.9, 7.10): 202 + Location + praca ROZPOZNAJ_PISMO w jednej transakcji (wycofanie), błędy zdjęć, wersja Zadania, stan, własność zasobu, adres podpisany bez Authorization i wygasanie, lista z filtrem i kursorem, Mock | @DependsOn: ApiFactory, KlientZalogowany, PocSeed, TestowyJpeg, MagazynObiektowLokalny | @UsedBy: dotnet test | @Task: TASK-127
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lemat.Api.Tests.Support;
using Lemat.Application.Common.BackgroundJobs;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Grading;
using Lemat.Domain.Models;
using Lemat.Domain.Solutions;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.BackgroundJobs;
using Lemat.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Lemat.Api.Tests.Solutions;

[Collection(KolekcjeTestowe.DanePoc)]
public sealed class SolutionsEndpointsTests(ApiFactory fabryka, BazaTestowaFixture baza) : IClassFixture<ApiFactory>
{
    private const string Wersja2 = "b1000000-0000-4000-8000-000000000002";
    private const string Zadanie2 = "a1000000-0000-4000-8000-000000000002";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string WersjaZadania(int numer) => $"b1000000-0000-4000-8000-00000000000{numer}";

    private async Task<KlientZalogowany> KlientAsync()
    {
        await PocSeed.ZapewnijAsync(baza.Polaczenie, Token);
        return await KlientZalogowany.ZarejestrujAsync(fabryka);
    }

    private static MultipartFormDataContent Formularz(string? wersja, params (string Pole, byte[] Bajty)[] pliki)
    {
        var tresc = new MultipartFormDataContent();
        if (wersja is not null)
        {
            tresc.Add(new StringContent(wersja), "zadanie_wersja_id");
        }

        for (var i = 0; i < pliki.Length; i++)
        {
            var plik = new ByteArrayContent(pliki[i].Bajty);
            plik.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            tresc.Add(plik, pliki[i].Pole, $"kartka-{i + 1}.jpg");
        }

        return tresc;
    }

    private static Task<OdpowiedzTestowa> PrzeslijAsync(KlientZalogowany klient, string wersja = Wersja2, int liczbaPlikow = 1, string? scenariusz = null)
    {
        var pliki = Enumerable.Range(0, liczbaPlikow).Select(i => ("zdjecia", TestowyJpeg.Utworz(ziarno: (byte)i))).ToArray();
        return klient.WyslijFormularzAsync("/api/v1/rozwiazania", Formularz(wersja, pliki), scenariusz);
    }

    private async Task<T> SkalarAsync<T>(string sql, params (string Nazwa, object Wartosc)[] parametry)
    {
        await using var zrodlo = NpgsqlDataSource.Create(baza.Polaczenie);
        await using var polecenie = zrodlo.CreateCommand(sql);
        foreach (var (nazwa, wartosc) in parametry)
        {
            polecenie.Parameters.AddWithValue(nazwa, wartosc);
        }

        return (T)(await polecenie.ExecuteScalarAsync(Token))!;
    }

    private async Task WykonajAsync(string sql)
    {
        await using var zrodlo = NpgsqlDataSource.Create(baza.Polaczenie);
        await using var polecenie = zrodlo.CreateCommand(sql);
        await polecenie.ExecuteNonQueryAsync(Token);
    }

    private static string[] Pola(JsonElement element) => [.. element.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)];

    // ---------- POST /rozwiazania: sukces ----------

    [Fact]
    public async Task PoprawnePrzeslanieZwraca202ZLocationIKsztaltemZKontraktu()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await PrzeslijAsync(klient);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var tresc = odpowiedz.Tresc;
        var id = tresc.GetProperty("id").GetGuid();
        odpowiedz.Odpowiedz.Headers.Location!.OriginalString.ShouldBe($"/api/v1/rozwiazania/{id}");
        Pola(tresc).ShouldBe(
            [
                "created_at", "element_sciezki_id", "id", "kod_bledu_rozpoznawania", "liczba_ponowien_rozpoznania", "liczba_przeslan",
                "nastepne_odpytanie_ms", "ocena", "przypisanie_id", "status", "szkic_transkrypcji", "transkrypcja", "updated_at", "version",
                "weryfikacja", "zadanie_id", "zadanie_wersja_id", "zdjecia",
            ]);
        tresc.GetProperty("zadanie_id").GetString().ShouldBe(Zadanie2);
        tresc.GetProperty("zadanie_wersja_id").GetString().ShouldBe(Wersja2);
        tresc.GetProperty("status").GetString().ShouldBe("PRZESLANE");
        tresc.GetProperty("element_sciezki_id").ValueKind.ShouldBe(JsonValueKind.String);
        tresc.GetProperty("przypisanie_id").ValueKind.ShouldBe(JsonValueKind.Null);
        tresc.GetProperty("liczba_przeslan").GetInt32().ShouldBe(1);
        tresc.GetProperty("liczba_ponowien_rozpoznania").GetInt32().ShouldBe(0);
        tresc.GetProperty("version").GetInt32().ShouldBe(1);
        tresc.GetProperty("nastepne_odpytanie_ms").GetInt32().ShouldBe(1000);
        foreach (var pustePole in new[] { "kod_bledu_rozpoznawania", "transkrypcja", "ocena", "weryfikacja", "szkic_transkrypcji" })
        {
            tresc.GetProperty(pustePole).ValueKind.ShouldBe(JsonValueKind.Null, pustePole);
        }

        var zdjecia = tresc.GetProperty("zdjecia").EnumerateArray().ToList();
        zdjecia.Count.ShouldBe(1);
        Pola(zdjecia[0]).ShouldBe(["dostepne", "id", "kolejnosc", "przeslanie_nr"]);
        zdjecia[0].GetProperty("przeslanie_nr").GetInt32().ShouldBe(1);
        zdjecia[0].GetProperty("kolejnosc").GetInt32().ShouldBe(1);
        zdjecia[0].GetProperty("dostepne").GetBoolean().ShouldBeTrue();
        tresc.GetProperty("created_at").GetString()!.ShouldEndWith("Z");
    }

    [Fact]
    public async Task PrzeslanieTworzyZdjecieNaDyskuIPraceRozpoznajPismoWTejSamejTransakcji()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await PrzeslijAsync(klient, liczbaPlikow: 2, scenariusz: "rozpoznanie-nieczytelne");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var id = odpowiedz.Tresc.GetProperty("id").GetGuid();
        (await SkalarAsync<long>("SELECT count(*) FROM zdjecie_rozwiazania WHERE rozwiazanie_id = @id", ("id", id))).ShouldBe(2);
        var klucz = await SkalarAsync<string>("SELECT klucz_obiektu FROM zdjecie_rozwiazania WHERE rozwiazanie_id = @id AND kolejnosc = 1", ("id", id));
        klucz.ShouldBe($"zdjecia/{klient.Id}/{id}/{odpowiedz.Tresc.GetProperty("zdjecia")[0].GetProperty("id").GetGuid()}.jpg");
        File.Exists(Path.Combine(fabryka.KatalogMagazynu, klucz)).ShouldBeTrue();
        var usunPo = await SkalarAsync<DateTime>("SELECT usun_po FROM zdjecie_rozwiazania WHERE rozwiazanie_id = @id AND kolejnosc = 1", ("id", id));
        var utworzono = await SkalarAsync<DateTime>("SELECT created_at FROM zdjecie_rozwiazania WHERE rozwiazanie_id = @id AND kolejnosc = 1", ("id", id));
        (usunPo - utworzono).ShouldBe(TimeSpan.FromDays(90));

        (await SkalarAsync<long>("SELECT count(*) FROM praca_w_tle WHERE typ = 'ROZPOZNAJ_PISMO' AND klucz_deduplikacji = @k", ("k", $"{id}:1:0"))).ShouldBe(1);
        (await SkalarAsync<string>("SELECT pula FROM praca_w_tle WHERE klucz_deduplikacji = @k", ("k", $"{id}:1:0"))).ShouldBe("ROZPOZNAWANIE");
        (await SkalarAsync<string>("SELECT status::text FROM praca_w_tle WHERE klucz_deduplikacji = @k", ("k", $"{id}:1:0"))).ShouldBe("OCZEKUJE");
        (await SkalarAsync<short>("SELECT maks_prob FROM praca_w_tle WHERE klucz_deduplikacji = @k", ("k", $"{id}:1:0"))).ShouldBe((short)3);
        var ladunek = JsonDocument.Parse(await SkalarAsync<string>("SELECT ladunek::text FROM praca_w_tle WHERE klucz_deduplikacji = @k", ("k", $"{id}:1:0"))).RootElement;
        ladunek.GetProperty("rozwiazanie_id").GetGuid().ShouldBe(id);
        ladunek.GetProperty("przeslanie_nr").GetInt32().ShouldBe(1);
        ladunek.GetProperty("ponowienie_nr").GetInt32().ShouldBe(0);
        ladunek.GetProperty("mock_scenariusz").GetString().ShouldBe("rozpoznanie-nieczytelne");

        // Techniczny element Ścieżki (U-24): ROZPOCZETY, wskazany przez Rozwiązanie.
        (await SkalarAsync<string>(
            "SELECT e.status::text FROM rozwiazanie r JOIN element_sciezki e ON e.id = r.element_sciezki_id WHERE r.id = @id", ("id", id))).ShouldBe("ROZPOCZETY");
    }

    private sealed class ZlecaniePoCzymBlad(IZlecaniePrac wlasciwe) : IZlecaniePrac
    {
        public async Task<RezultatZlecenia> ZlecAsync(ZleceniePracy zlecenie, CancellationToken ct)
        {
            await wlasciwe.ZlecAsync(zlecenie, ct);
            throw new InvalidOperationException("Błąd po zleceniu pracy (test wycofania transakcji).");
        }
    }

    [Fact]
    public async Task BladPoZleceniuPracyWycofujeRozwiazanieIPraceIUsuwaZapisaneObiekty()
    {
        await PocSeed.ZapewnijAsync(baza.Polaczenie, Token);
        var prace = await SkalarAsync<long>("SELECT count(*) FROM praca_w_tle");
        var rozwiazania = await SkalarAsync<long>("SELECT count(*) FROM rozwiazanie");
        var katalog = Path.Combine(fabryka.KatalogMagazynu, "zdjecia");
        var plikiPrzed = Directory.Exists(katalog) ? Directory.GetFiles(katalog, "*", SearchOption.AllDirectories).Length : 0;

        using var wadliwa = fabryka.WithWebHostBuilder(b => b.ConfigureServices(uslugi =>
        {
            uslugi.AddScoped<ZlecaniePracSql>();
            uslugi.RemoveAll<IZlecaniePrac>();
            uslugi.AddScoped<IZlecaniePrac>(sp => new ZlecaniePoCzymBlad(sp.GetRequiredService<ZlecaniePracSql>()));
        }));

        // Rejestracja i przesłanie idą do hosta z wadliwym zleceniem pracy.
        using var http = wadliwa.CreateClient();
        var rejestracja = await http.PostAsJsonAsync("/api/v1/auth/rejestracja", KlientAuth.Rejestracja(), Token);
        var token = JsonDocument.Parse(await rejestracja.Content.ReadAsStringAsync(Token)).RootElement.GetProperty("sesja").GetProperty("access_token").GetString();
        using var zadanie = new HttpRequestMessage(HttpMethod.Post, "/api/v1/rozwiazania") { Content = Formularz(Wersja2, ("zdjecia", TestowyJpeg.Utworz())) };
        zadanie.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var odpowiedz = await http.SendAsync(zadanie, Token);

        odpowiedz.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await SkalarAsync<long>("SELECT count(*) FROM praca_w_tle")).ShouldBe(prace);
        (await SkalarAsync<long>("SELECT count(*) FROM rozwiazanie")).ShouldBe(rozwiazania);
        (Directory.Exists(katalog) ? Directory.GetFiles(katalog, "*", SearchOption.AllDirectories).Length : 0).ShouldBe(plikiPrzed);
    }

    // ---------- POST /rozwiazania: błędy ----------

    [Fact]
    public async Task PlikPngZwraca415()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.WyslijFormularzAsync("/api/v1/rozwiazania", Formularz(Wersja2, ("zdjecia", TestowyJpeg.Png())));

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("NIEOBSLUGIWANY_FORMAT");
    }

    [Theory]
    [InlineData(6)]
    [InlineData(0)]
    public async Task NiepoprawnaLiczbaPlikowZwraca422ZaDuzoZdjec(int liczba)
    {
        using var klient = await KlientAsync();

        var odpowiedz = await PrzeslijAsync(klient, liczbaPlikow: liczba);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe((HttpStatusCode)422);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("ZA_DUZO_ZDJEC");
    }

    [Fact]
    public async Task PlikPowyzej10MbZwraca413()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.WyslijFormularzAsync(
            "/api/v1/rozwiazania",
            Formularz(Wersja2, ("zdjecia", TestowyJpeg.Utworz(dopelnienieBajty: 10 * 1024 * 1024 + 1))));

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("ZDJECIE_ZA_DUZE");
    }

    [Fact]
    public async Task PlikNieczytelnyJakoJpegZwraca422()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.WyslijFormularzAsync("/api/v1/rozwiazania", Formularz(Wersja2, ("zdjecia", TestowyJpeg.Uszkodzony())));

        odpowiedz.Odpowiedz.StatusCode.ShouldBe((HttpStatusCode)422);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("ZDJECIE_NIEPRAWIDLOWE");
    }

    [Fact]
    public async Task PlikPodInnaNazwaPolaZwraca422ZBledemPola()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.WyslijFormularzAsync("/api/v1/rozwiazania", Formularz(Wersja2, ("plik", TestowyJpeg.Utworz())));

        odpowiedz.Odpowiedz.StatusCode.ShouldBe((HttpStatusCode)422);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("WALIDACJA");
        var blad = odpowiedz.Tresc.GetProperty("bledy_pol")[0];
        blad.GetProperty("pole").GetString().ShouldBe("plik");
        blad.GetProperty("kod_reguly").GetString().ShouldBe("PLIK_POZA_POLEM_ZDJECIA");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("to-nie-uuid")]
    public async Task BrakLubZlyIdentyfikatorWersjiZwraca422(string? wersja)
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.WyslijFormularzAsync("/api/v1/rozwiazania", Formularz(wersja, ("zdjecia", TestowyJpeg.Utworz())));

        odpowiedz.Odpowiedz.StatusCode.ShouldBe((HttpStatusCode)422);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("WALIDACJA");
        odpowiedz.Tresc.GetProperty("bledy_pol")[0].GetProperty("pole").GetString().ShouldBe("zadanie_wersja_id");
    }

    [Fact]
    public async Task NieznanaWersjaZadaniaZwraca404()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await PrzeslijAsync(klient, wersja: "b1000000-0000-4000-8000-0000000000ee");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("NIE_ZNALEZIONO");
    }

    [Fact]
    public async Task StaraWersjaZadaniaZwraca409ZAktualnaWersja()
    {
        using var klient = await KlientAsync();
        const string nowaWersja = "b1000000-0000-4000-8000-0000000000f5";
        await WykonajAsync($"""
            INSERT INTO zadanie_wersja (id, zadanie_id, nr_wersji, tresc_md, opcje_zamkniete, rozwiazanie_wzorcowe, wynik_koncowy_wzorcowy, punkty_max)
            SELECT '{nowaWersja}', zadanie_id, 2, tresc_md, opcje_zamkniete, rozwiazanie_wzorcowe, wynik_koncowy_wzorcowy, punkty_max
            FROM zadanie_wersja WHERE id = '{WersjaZadania(5)}';
            UPDATE zadanie SET aktualna_wersja_id = '{nowaWersja}' WHERE id = 'a1000000-0000-4000-8000-000000000005';
            """);
        try
        {
            var odpowiedz = await PrzeslijAsync(klient, wersja: WersjaZadania(5));

            odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            odpowiedz.Tresc.GetProperty("kod").GetString().ShouldBe("WERSJA_ZADANIA_NIEAKTUALNA");
            odpowiedz.Tresc.GetProperty("szczegoly").GetProperty("aktualna_zadanie_wersja_id").GetString().ShouldBe(nowaWersja);
        }
        finally
        {
            await WykonajAsync($"""
                UPDATE zadanie SET aktualna_wersja_id = '{WersjaZadania(5)}' WHERE id = 'a1000000-0000-4000-8000-000000000005';
                DELETE FROM zadanie_wersja WHERE id = '{nowaWersja}';
                """);
        }
    }

    [Fact]
    public async Task DrugieRozwiazanieTegoSamegoZadaniaWTrakcieZwraca409ZIstniejacymId()
    {
        using var klient = await KlientAsync();
        var pierwsze = await PrzeslijAsync(klient);
        pierwsze.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var prace = await SkalarAsync<long>("SELECT count(*) FROM praca_w_tle");

        var drugie = await PrzeslijAsync(klient);

        drugie.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        drugie.Tresc.GetProperty("kod").GetString().ShouldBe("ROZWIAZANIE_NIEPRAWIDLOWY_STAN");
        var szczegoly = drugie.Tresc.GetProperty("szczegoly");
        szczegoly.GetProperty("istniejace_rozwiazanie_id").GetGuid().ShouldBe(pierwsze.Tresc.GetProperty("id").GetGuid());
        szczegoly.GetProperty("status").GetString().ShouldBe("PRZESLANE");
        (await SkalarAsync<long>("SELECT count(*) FROM praca_w_tle")).ShouldBe(prace);

        // Inne Zadanie tego samego Ucznia nie jest blokowane.
        (await PrzeslijAsync(klient, wersja: WersjaZadania(1))).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task RozwiazanieZeStanemBladRozpoznawaniaNieBlokujeNowegoPrzeslania()
    {
        using var klient = await KlientAsync();
        var pierwsze = await PrzeslijAsync(klient);
        await WykonajAsync($"UPDATE rozwiazanie SET status = 'BLAD_ROZPOZNAWANIA', kod_bledu_rozpoznawania = 'NIECZYTELNE' WHERE id = '{pierwsze.Tresc.GetProperty("id").GetGuid()}'");

        (await PrzeslijAsync(klient)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task NieznanyScenariuszMockZwraca422()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await PrzeslijAsync(klient, scenariusz: "nieznany");

        odpowiedz.Odpowiedz.StatusCode.ShouldBe((HttpStatusCode)422);
        var blad = odpowiedz.Tresc.GetProperty("bledy_pol")[0];
        blad.GetProperty("pole").GetString().ShouldBe("X-Lemat-Mock-Scenariusz");
        blad.GetProperty("kod_reguly").GetString().ShouldBe("MOCK_SCENARIUSZ_NIEZNANY");
    }

    [Fact]
    public async Task BezTokenuZwraca401()
    {
        using var anonim = KlientZalogowany.Anonimowy(fabryka);

        var odpowiedz = await anonim.WyslijFormularzAsync("/api/v1/rozwiazania", Formularz(Wersja2, ("zdjecia", TestowyJpeg.Utworz())));

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ---------- GET /rozwiazania/{id}, własność zasobu ----------

    [Fact]
    public async Task GetZwracaToSamoCoPostIRozwiazanieInnegoUczniaToNotFound()
    {
        using var wlasciciel = await KlientAsync();
        using var obcy = await KlientAsync();
        var utworzone = await PrzeslijAsync(wlasciciel);
        var id = utworzone.Tresc.GetProperty("id").GetGuid();

        var wlasne = await wlasciciel.PobierzAsync($"/api/v1/rozwiazania/{id}");
        var cudze = await obcy.PobierzAsync($"/api/v1/rozwiazania/{id}");

        wlasne.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        wlasne.Tresc.GetRawText().ShouldBe(utworzone.Tresc.GetRawText());
        cudze.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        cudze.Tresc.GetProperty("kod").GetString().ShouldBe("NIE_ZNALEZIONO");
        (await wlasciciel.PobierzAsync("/api/v1/rozwiazania/to-nie-uuid")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await wlasciciel.PobierzAsync($"/api/v1/rozwiazania/{Guid.NewGuid()}")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---------- adres zdjęcia i magazyn lokalny ----------

    [Fact]
    public async Task AdresZdjeciaDzialaBezAuthorizationAZmienionyTokenToNotFound()
    {
        using var klient = await KlientAsync();
        using var obcy = await KlientAsync();
        var jpeg = TestowyJpeg.Utworz(800, 600, dopelnienieBajty: 100);
        var utworzone = await klient.WyslijFormularzAsync("/api/v1/rozwiazania", Formularz(Wersja2, ("zdjecia", jpeg)));
        var id = utworzone.Tresc.GetProperty("id").GetGuid();
        var zdjecieId = utworzone.Tresc.GetProperty("zdjecia")[0].GetProperty("id").GetGuid();
        var sciezka = $"/api/v1/rozwiazania/{id}/zdjecia/{zdjecieId}/adres";

        var adres = await klient.PobierzAsync(sciezka);

        adres.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        Pola(adres.Tresc).ShouldBe(["url", "wygasa_at"]);
        var url = adres.Tresc.GetProperty("url").GetString()!;
        url.ShouldStartWith("/api/v1/magazyn-lokalny/");
        var wygasa = adres.Tresc.GetProperty("wygasa_at").GetDateTimeOffset();
        (wygasa - DateTimeOffset.UtcNow).ShouldBeInRange(TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(5.1));

        using var anonim = KlientZalogowany.Anonimowy(fabryka);
        using var obraz = await anonim.PobierzSurowoAsync(url);
        obraz.StatusCode.ShouldBe(HttpStatusCode.OK);
        obraz.Content.Headers.ContentType!.MediaType.ShouldBe("image/jpeg");
        obraz.Headers.CacheControl!.Private.ShouldBeTrue();
        obraz.Headers.CacheControl.MaxAge.ShouldBe(TimeSpan.FromSeconds(300));
        (await obraz.Content.ReadAsByteArrayAsync(Token)).ShouldBe(jpeg);

        var zmieniony = url[..^2] + (url[^2] == 'A' ? "B" : "A") + url[^1];
        using var odrzucony = await anonim.PobierzSurowoAsync(zmieniony);
        odrzucony.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var smieci = await anonim.PobierzSurowoAsync("/api/v1/magazyn-lokalny/nie-token");
        smieci.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await obcy.PobierzAsync(sciezka)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await klient.PobierzAsync($"/api/v1/rozwiazania/{id}/zdjecia/{Guid.NewGuid()}/adres")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await klient.PobierzAsync($"/api/v1/rozwiazania/{Guid.NewGuid()}/zdjecia/{zdjecieId}/adres")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var bezTokenu = KlientZalogowany.Anonimowy(fabryka);
        (await bezTokenu.PobierzAsync(sciezka)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Treść zdjęcia, ścieżka obiektu i token nie trafiają do logów.
        fabryka.Logi.Calosc.ShouldNotContain(url);
        fabryka.Logi.Calosc.ShouldNotContain(zdjecieId.ToString());
        fabryka.Logi.Calosc.ShouldNotContain("kartka-1.jpg");
    }

    private sealed class CzasRecznie(DateTimeOffset teraz) : TimeProvider
    {
        public DateTimeOffset Teraz { get; set; } = teraz;

        public override DateTimeOffset GetUtcNow() => Teraz;
    }

    [Fact]
    public async Task TokenPo5MinutachToNotFound()
    {
        using var klient = await KlientAsync();
        var jpeg = TestowyJpeg.Utworz();
        var utworzone = await klient.WyslijFormularzAsync("/api/v1/rozwiazania", Formularz(Wersja2, ("zdjecia", jpeg)));
        var id = utworzone.Tresc.GetProperty("id").GetGuid();
        var zdjecieId = utworzone.Tresc.GetProperty("zdjecia")[0].GetProperty("id").GetGuid();
        var klucz = await SkalarAsync<string>("SELECT klucz_obiektu FROM zdjecie_rozwiazania WHERE id = @id", ("id", zdjecieId));

        // Magazyn z zegarem sprzed 5 minut i 1 sekundy wystawia adres, który względem zegara hosta jest już wygasły.
        var czas = new CzasRecznie(DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(1));
        var wygasly = new MagazynObiektowLokalny(fabryka.KatalogMagazynu, MagazynObiektowLokalny.KluczDeweloperski, czas).PodpisanyUrl(klucz, TimeSpan.FromMinutes(5));
        var swiezy = new MagazynObiektowLokalny(fabryka.KatalogMagazynu, MagazynObiektowLokalny.KluczDeweloperski, TimeProvider.System).PodpisanyUrl(klucz, TimeSpan.FromMinutes(5));
        using var anonim = KlientZalogowany.Anonimowy(fabryka);

        using var poCzasie = await anonim.PobierzSurowoAsync(wygasly.Url.OriginalString);
        using var wCzasie = await anonim.PobierzSurowoAsync(swiezy.Url.OriginalString);

        poCzasie.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        wCzasie.StatusCode.ShouldBe(HttpStatusCode.OK);
        id.ShouldNotBe(Guid.Empty);
    }

    // ---------- Transkrypcja i Ocena w RozwiazanieWyjscie (dane zapisuje dopiero TASK-128; tu wstawiane przez domenę) ----------

    private async Task ZmienAgregatAsync(Guid id, Action<Rozwiazanie, LematDbContext> zmiana)
    {
        await using var zakres = fabryka.Services.CreateAsyncScope();
        var kontekst = zakres.ServiceProvider.GetRequiredService<LematDbContext>();
        var rozwiazanie = await kontekst.Set<Rozwiazanie>().AsTracking().FirstAsync(r => r.Id == new RozwiazanieId(id), Token);
        zmiana(rozwiazanie, kontekst);
        await kontekst.SaveChangesAsync(Token);
    }

    [Fact]
    public async Task TranskrypcjaIOcenaSaWOdpowiedziZgodneZPrzykladamiKontraktu()
    {
        using var klient = await KlientAsync();
        var id = (await PrzeslijAsync(klient)).Tresc.GetProperty("id").GetGuid();
        var teraz = DateTimeOffset.UtcNow;
        var transkrypcjaId = new TranskrypcjaId(Guid.CreateVersion7());
        var konfiguracjaRozpoznawania = new KonfiguracjaModeluId(await SkalarAsync<Guid>("SELECT id FROM konfiguracja_modelu WHERE rola = 'ROZPOZNAWANIE_PISMA'"));
        await ZmienAgregatAsync(id, (r, kontekst) =>
        {
            r.RozpocznijRozpoznawanie();
            kontekst.Add(Transkrypcja.ZAgenta(
                transkrypcjaId,
                r.Id,
                1,
                1,
                konfiguracjaRozpoznawania,
                0.95m,
                [
                    KrokTranskrypcji.OdczytanyPrzezAgenta(1, "Obliczam wyróżnik", @"\Delta=(-5)^2-4\cdot 1\cdot 6=1", 0.96m),
                    KrokTranskrypcji.OdczytanyPrzezAgenta(2, "Obliczam pierwiastki", @"x_1=\frac{5-1}{2}=2,\quad x_2=\frac{5+1}{2}=4", 0.91m),
                ],
                teraz));
            r.ZapiszTranskrypcjeAgenta(transkrypcjaId, 1, teraz);
        });

        var gotowa = await klient.PobierzAsync($"/api/v1/rozwiazania/{id}");

        gotowa.Tresc.GetProperty("status").GetString().ShouldBe("TRANSKRYPCJA_GOTOWA");
        gotowa.Tresc.GetProperty("nastepne_odpytanie_ms").ValueKind.ShouldBe(JsonValueKind.Null);
        gotowa.Tresc.GetProperty("version").GetInt32().ShouldBeGreaterThan(1);
        var oczekiwanaTranskrypcja = System.Text.Json.Nodes.JsonNode.Parse("""
            {
              "id": "__ID__", "wersja": 1, "zrodlo": "AGENT_ROZPOZNAJACY", "przeslanie_nr": 1, "zgodnosc_z_zadaniem": 0.95,
              "kroki": [
                { "nr": 1, "tekst": "Obliczam wyróżnik", "latex": "\\Delta=(-5)^2-4\\cdot 1\\cdot 6=1", "pewnosc_odczytu": 0.96, "niepewne_fragmenty": [],
                  "zmieniony_przez_ucznia": false, "nr_kroku_agenta": null, "wymaga_sprawdzenia": false, "podejrzenie_danych_osobowych": false,
                  "tylko_dane_osobowe": false, "zastapienia_danych_osobowych": [] },
                { "nr": 2, "tekst": "Obliczam pierwiastki", "latex": "x_1=\\frac{5-1}{2}=2,\\quad x_2=\\frac{5+1}{2}=4", "pewnosc_odczytu": 0.91, "niepewne_fragmenty": [],
                  "zmieniony_przez_ucznia": false, "nr_kroku_agenta": null, "wymaga_sprawdzenia": false, "podejrzenie_danych_osobowych": false,
                  "tylko_dane_osobowe": false, "zastapienia_danych_osobowych": [] }
              ],
              "created_at": "__CZAS__", "zakres_poprawek": null, "konflikt_filtra": false, "zgodnosc_poprawek": null
            }
            """.Replace("__ID__", transkrypcjaId.Wartosc.ToString()).Replace("__CZAS__", gotowa.Tresc.GetProperty("transkrypcja").GetProperty("created_at").GetString()))!;
        System.Text.Json.Nodes.JsonNode.DeepEquals(System.Text.Json.Nodes.JsonNode.Parse(gotowa.Tresc.GetProperty("transkrypcja").GetRawText()), oczekiwanaTranskrypcja)
            .ShouldBeTrue(gotowa.Tresc.GetProperty("transkrypcja").GetRawText());
        gotowa.Tresc.GetProperty("ocena").ValueKind.ShouldBe(JsonValueKind.Null);

        var kryteria = await SkalarAsync<Guid[]>(
            "SELECT array_agg(id ORDER BY nr_kryterium) FROM kryterium_oceniania WHERE zadanie_wersja_id = @w AND wariant = 1", ("w", Guid.Parse(Wersja2)));
        var ocenaId = new OcenaId(Guid.CreateVersion7());
        var dane = new DaneOcenyAutomatycznej(
            new RozwiazanieId(id),
            1,
            transkrypcjaId,
            StatusOceny.Ostateczna,
            new WariantRozwiazania(1),
            1,
            1,
            2,
            2,
            false,
            TypBledu.Rachunkowy,
            false,
            null,
            new PewnoscOceny(0.995m),
            new PewnoscSurowa(0.995m),
            new ProgPewnosciId(await SkalarAsync<Guid>("SELECT id FROM prog_pewnosci WHERE aktywny")),
            new KalibratorPewnosciId(await SkalarAsync<Guid>("SELECT id FROM kalibrator_pewnosci LIMIT 1")),
            new KonfiguracjaModeluId(await SkalarAsync<Guid>("SELECT id FROM konfiguracja_modelu WHERE rola = 'DECYZYJNY_GLOWNY'")),
            "Kryteria: K1 „Obliczenie wyróżnika trójmianu: $\\Delta = 1$.” — 1/1 pkt; K2 „Obliczenie pierwiastków równania: $x_1 = 2$, $x_2 = 3$.” — 0/1 pkt. Pierwszy błędny krok: 2 (błąd rachunkowy). Razem 1/2 pkt.",
            new Wskazowka("Sprawdź jeszcze raz krok 2 — policz ponownie drugi pierwiastek.", ZrodloWskazowki.Szablon),
            [new OcenaKryterium(kryteria[0], 1, 0.995m), new OcenaKryterium(kryteria[1], 0, 0.995m)]);
        await ZmienAgregatAsync(id, (r, kontekst) =>
        {
            r.Zatwierdz(transkrypcjaId, 1, teraz);
            r.RozpocznijOcenianie();
            kontekst.Add(Ocena.Automatyczna(ocenaId, dane, teraz));
            r.ZarejestrujOcene(teraz);
        });

        var oceniona = await klient.PobierzAsync($"/api/v1/rozwiazania/{id}");

        oceniona.Tresc.GetProperty("status").GetString().ShouldBe("OCENIONE");
        oceniona.Tresc.GetProperty("nastepne_odpytanie_ms").ValueKind.ShouldBe(JsonValueKind.Null);
        var czasOceny = oceniona.Tresc.GetProperty("ocena").GetProperty("created_at").GetString();
        var oczekiwanaOcena = System.Text.Json.Nodes.JsonNode.Parse("""
            {
              "id": "__ID__", "wersja": 1, "status": "OSTATECZNA", "zrodlo": "AUTOMATYCZNA", "widocznosc_dla_ucznia": "PELNA", "punkty": 1, "punkty_max": 2, "wariant": 1,
              "kryteria": [
                { "nr_kryterium": 1, "opis": "Obliczenie wyróżnika trójmianu: $\\Delta = 1$.", "punkty": 1, "punkty_max": 1, "prawdopodobienstwo": 0.995 },
                { "nr_kryterium": 2, "opis": "Obliczenie pierwiastków równania: $x_1 = 2$, $x_2 = 3$.", "punkty": 0, "punkty_max": 1, "prawdopodobienstwo": 0.995 }
              ],
              "pierwszy_bledny_krok": 2, "brakujacy_krok": false, "typ_bledu": "RACHUNKOWY", "krok_podejrzany_odczyt": null, "mozliwy_blad_odczytu": false,
              "wskazowka": "Sprawdź jeszcze raz krok 2 — policz ponownie drugi pierwiastek.", "wskazowka_w_przygotowaniu": false,
              "uzasadnienie": "Kryteria: K1 „Obliczenie wyróżnika trójmianu: $\\Delta = 1$.” — 1/1 pkt; K2 „Obliczenie pierwiastków równania: $x_1 = 2$, $x_2 = 3$.” — 0/1 pkt. Pierwszy błędny krok: 2 (błąd rachunkowy). Razem 1/2 pkt.",
              "pewnosc_oceny": 0.995, "komunikat": null, "mozliwe_zgloszenie_do": null, "created_at": "__CZAS__"
            }
            """.Replace("__ID__", ocenaId.Wartosc.ToString()).Replace("__CZAS__", czasOceny))!;
        System.Text.Json.Nodes.JsonNode.DeepEquals(System.Text.Json.Nodes.JsonNode.Parse(oceniona.Tresc.GetProperty("ocena").GetRawText()), oczekiwanaOcena)
            .ShouldBeTrue(oceniona.Tresc.GetProperty("ocena").GetRawText());
        oceniona.Tresc.GetProperty("transkrypcja").GetProperty("id").GetGuid().ShouldBe(transkrypcjaId.Wartosc);
    }

    // ---------- GET /rozwiazania ----------

    [Fact]
    public async Task ListaZwracaWlasneRozwiazaniaMalejacoZFiltremStatusuIKursorem()
    {
        using var klient = await KlientAsync();
        using var obcy = await KlientAsync();
        var a = (await PrzeslijAsync(klient, wersja: WersjaZadania(1))).Tresc.GetProperty("id").GetString();
        await Task.Delay(20, Token);
        var b = (await PrzeslijAsync(klient, wersja: WersjaZadania(2))).Tresc.GetProperty("id").GetString();
        await Task.Delay(20, Token);
        var c = (await PrzeslijAsync(klient, wersja: WersjaZadania(3))).Tresc.GetProperty("id").GetString();
        await PrzeslijAsync(obcy, wersja: WersjaZadania(1));
        await WykonajAsync($"UPDATE rozwiazanie SET status = 'BLAD_ROZPOZNAWANIA', kod_bledu_rozpoznawania = 'NIECZYTELNE' WHERE id = '{b}'");

        var wszystkie = await klient.PobierzAsync("/api/v1/rozwiazania");
        var przeslane = await klient.PobierzAsync("/api/v1/rozwiazania?status=PRZESLANE");
        var ocenione = await klient.PobierzAsync("/api/v1/rozwiazania?status=OCENIONE");

        Identyfikatory(wszystkie.Tresc).ShouldBe([c, b, a]);
        Identyfikatory(przeslane.Tresc).ShouldBe([c, a]);
        Identyfikatory(ocenione.Tresc).ShouldBeEmpty();
        wszystkie.Tresc.GetProperty("elementy")[0].GetProperty("nastepne_odpytanie_ms").GetInt32().ShouldBe(1000);
        wszystkie.Tresc.GetProperty("elementy")[1].GetProperty("nastepne_odpytanie_ms").ValueKind.ShouldBe(JsonValueKind.Null);

        var strona1 = await klient.PobierzAsync("/api/v1/rozwiazania?limit=2");
        var kursor = strona1.Tresc.GetProperty("nastepny_kursor").GetString()!;
        var strona2 = await klient.PobierzAsync("/api/v1/rozwiazania?limit=2&kursor=" + Uri.EscapeDataString(kursor));
        Identyfikatory(strona1.Tresc).ShouldBe([c, b]);
        Identyfikatory(strona2.Tresc).ShouldBe([a]);
        strona2.Tresc.GetProperty("nastepny_kursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Theory]
    [InlineData("status=NIEZNANY", 422)]
    [InlineData("status=przeslane", 422)]
    [InlineData("limit=0", 422)]
    [InlineData("kursor=cos.zepsute", 400)]
    public async Task NiepoprawneParametryListyZwracajaBlad(string zapytanie, int oczekiwany)
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.PobierzAsync("/api/v1/rozwiazania?" + zapytanie);

        ((int)odpowiedz.Odpowiedz.StatusCode).ShouldBe(oczekiwany);
    }

    [Fact]
    public async Task ListaBezTokenuZwraca401()
    {
        using var anonim = KlientZalogowany.Anonimowy(fabryka);

        (await anonim.PobierzAsync("/api/v1/rozwiazania")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static string?[] Identyfikatory(JsonElement strona) =>
        [.. strona.GetProperty("elementy").EnumerateArray().Select(e => e.GetProperty("id").GetString())];
}
