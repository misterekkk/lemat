// @Role: Testy integracyjne zmockowanego potoku POC (TASK-128) na prawdziwej bazie: PRZESLANE→ROZPOZNAWANIE→TRANSKRYPCJA_GOTOWA→ZATWIERDZONE→OCENIANIE→OCENIONE dla wszystkich 5 Zadań dokładnie wg tabel 7.2 i 7.3 kontraktu, zegar testowy, version +1 na przejście, idempotencja handlerów, scenariusz nieczytelny, błędy zatwierdzenia (428/400/412/409/422/404), wyścig zatwierdzeń, GET …/ocena | @DependsOn: ApiFactory, WorkerTestowy, ZegarTestowy, KontraktPoc, PocSeed, TestowyJpeg | @UsedBy: dotnet test | @Task: TASK-128
using System.Globalization;
using System.Net;
using System.Text.Json;
using Lemat.Api.Tests.Support;
using Lemat.Application.Common.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lemat.Api.Tests.Solutions;

[Collection(KolekcjeTestowe.DanePoc)]
public sealed class SolutionsFlowTests(ApiFactory fabryka, BazaTestowaFixture baza) : IClassFixture<ApiFactory>
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string WersjaZadania(int numer) => $"b1000000-0000-4000-8000-00000000000{numer}";

    private async Task<KlientZalogowany> KlientAsync()
    {
        await PocSeed.ZapewnijAsync(baza.Polaczenie, Token);

        // Prace zostawione przez inne testy (np. nieprzetworzone ROZPOZNAJ_PISMO) nie mogą zakłócać potoku tego testu.
        await WykonajAsync("UPDATE praca_w_tle SET status = 'ZAKONCZONA', wykonawca = NULL, dzierzawa_do = NULL WHERE status IN ('OCZEKUJE','W_TOKU')");
        return await KlientZalogowany.ZarejestrujAsync(fabryka);
    }

    private static WorkerTestowy NowyWorker() => new(new ZegarTestowy(DateTimeOffset.UtcNow));

    private static Task<OdpowiedzTestowa> PrzeslijAsync(KlientZalogowany klient, int zadanie, string? scenariusz = null)
    {
        var tresc = new MultipartFormDataContent { { new StringContent(WersjaZadania(zadanie)), "zadanie_wersja_id" } };
        var plik = new ByteArrayContent(TestowyJpeg.Utworz());
        plik.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        tresc.Add(plik, "zdjecia", "kartka.jpg");
        return klient.WyslijFormularzAsync("/api/v1/rozwiazania", tresc, scenariusz);
    }

    private static Task<OdpowiedzTestowa> ZatwierdzAsync(KlientZalogowany klient, Guid rozwiazanie, string? ifMatch, string json) =>
        klient.WyslijAsync(HttpMethod.Post, $"/api/v1/rozwiazania/{rozwiazanie}/zatwierdzenie", json, ifMatch);

    private static string CialoZatwierdzenia(JsonElement transkrypcja) =>
        $$"""{ "transkrypcja_id": "{{transkrypcja.GetProperty("id").GetGuid()}}", "wersja": {{transkrypcja.GetProperty("wersja").GetInt32()}} }""";

    private async Task<T> SkalarAsync<T>(string sql, params (string Nazwa, object Wartosc)[] parametry)
    {
        await using var zrodlo = NpgsqlDataSource.Create(baza.Polaczenie);
        await using var polecenie = zrodlo.CreateCommand(sql);
        foreach (var (nazwa, wartosc) in parametry)
        {
            polecenie.Parameters.AddWithValue(nazwa, wartosc);
        }

        return (T)Convert.ChangeType((await polecenie.ExecuteScalarAsync(Token))!, typeof(T), CultureInfo.InvariantCulture);
    }

    private async Task WykonajAsync(string sql)
    {
        await using var zrodlo = NpgsqlDataSource.Create(baza.Polaczenie);
        await using var polecenie = zrodlo.CreateCommand(sql);
        await polecenie.ExecuteNonQueryAsync(Token);
    }

    // ---------- pełny przepływ dla wszystkich 5 Zadań ----------

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task PelnyPrzeplywDajeTranskrypcjeIOceneDokladnieWgTabelKontraktu(int zadanie)
    {
        using var klient = await KlientAsync();

        // PRZESLANE (version 1) → worker: ROZPOZNAWANIE (2) → TRANSKRYPCJA_GOTOWA (3)
        var przeslane = await PrzeslijAsync(klient, zadanie);
        await using var worker = NowyWorker();
        przeslane.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var id = przeslane.Tresc.GetProperty("id").GetGuid();
        przeslane.Tresc.GetProperty("version").GetInt32().ShouldBe(1);
        (await worker.PrzetworzWszystkoAsync(Token)).ShouldBe(1);

        var gotowe = (await klient.PobierzAsync($"/api/v1/rozwiazania/{id}")).Tresc;
        gotowe.GetProperty("status").GetString().ShouldBe("TRANSKRYPCJA_GOTOWA");
        gotowe.GetProperty("version").GetInt32().ShouldBe(3);
        gotowe.GetProperty("nastepne_odpytanie_ms").ValueKind.ShouldBe(JsonValueKind.Null);
        gotowe.GetProperty("ocena").ValueKind.ShouldBe(JsonValueKind.Null);
        var transkrypcja = gotowe.GetProperty("transkrypcja");
        transkrypcja.GetProperty("wersja").GetInt32().ShouldBe(1);
        transkrypcja.GetProperty("zrodlo").GetString().ShouldBe("AGENT_ROZPOZNAJACY");
        transkrypcja.GetProperty("przeslanie_nr").GetInt32().ShouldBe(1);
        transkrypcja.GetProperty("zgodnosc_z_zadaniem").GetDecimal().ShouldBe(0.95m);
        var kroki = transkrypcja.GetProperty("kroki").EnumerateArray().ToList();
        var oczekiwaneKroki = KontraktPoc.Kroki(zadanie);
        kroki.Count.ShouldBe(oczekiwaneKroki.Count);
        for (var i = 0; i < kroki.Count; i++)
        {
            kroki[i].GetProperty("nr").GetInt32().ShouldBe(oczekiwaneKroki[i].Nr);
            kroki[i].GetProperty("tekst").GetString().ShouldBe(oczekiwaneKroki[i].Tekst);
            kroki[i].GetProperty("latex").GetString().ShouldBe(oczekiwaneKroki[i].Latex);
            kroki[i].GetProperty("pewnosc_odczytu").GetDecimal().ShouldBe(oczekiwaneKroki[i].Pewnosc);
        }

        // Przed OCENIONE: GET …/ocena → 404.
        var brak = await klient.PobierzAsync($"/api/v1/rozwiazania/{id}/ocena");
        brak.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        brak.Tresc.GetProperty("kod").GetString().ShouldBe("NIE_ZNALEZIONO");

        // ZATWIERDZONE (version 4) → worker: OCENIANIE (5) → OCENIONE (6)
        var zatwierdzone = await ZatwierdzAsync(klient, id, "\"3\"", CialoZatwierdzenia(transkrypcja));
        zatwierdzone.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        zatwierdzone.Tresc.GetProperty("status").GetString().ShouldBe("ZATWIERDZONE");
        zatwierdzone.Tresc.GetProperty("version").GetInt32().ShouldBe(4);
        zatwierdzone.Tresc.GetProperty("ocena").ValueKind.ShouldBe(JsonValueKind.Null);
        zatwierdzone.Tresc.GetProperty("nastepne_odpytanie_ms").GetInt32().ShouldBe(1000);
        (await klient.PobierzAsync($"/api/v1/rozwiazania/{id}/ocena")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await worker.PrzetworzWszystkoAsync(Token)).ShouldBe(1);

        var ocenione = (await klient.PobierzAsync($"/api/v1/rozwiazania/{id}")).Tresc;
        ocenione.GetProperty("status").GetString().ShouldBe("OCENIONE");
        ocenione.GetProperty("version").GetInt32().ShouldBe(6);
        ocenione.GetProperty("nastepne_odpytanie_ms").ValueKind.ShouldBe(JsonValueKind.Null);
        ocenione.GetProperty("transkrypcja").GetProperty("id").GetGuid().ShouldBe(transkrypcja.GetProperty("id").GetGuid());

        var ocena = ocenione.GetProperty("ocena");
        var oczekiwana = KontraktPoc.Ocena(zadanie);
        ocena.GetProperty("status").GetString().ShouldBe("OSTATECZNA");
        ocena.GetProperty("zrodlo").GetString().ShouldBe("AUTOMATYCZNA");
        ocena.GetProperty("widocznosc_dla_ucznia").GetString().ShouldBe("PELNA");
        ocena.GetProperty("wersja").GetInt32().ShouldBe(1);
        ocena.GetProperty("wariant").GetInt32().ShouldBe(1);
        ocena.GetProperty("punkty").GetInt32().ShouldBe(oczekiwana.Punkty);
        ocena.GetProperty("punkty_max").GetInt32().ShouldBe(oczekiwana.PunktyMax);
        ocena.GetProperty("pewnosc_oceny").GetDecimal().ShouldBe(0.995m);
        ocena.GetProperty("brakujacy_krok").GetBoolean().ShouldBeFalse();
        ocena.GetProperty("mozliwy_blad_odczytu").GetBoolean().ShouldBeFalse();
        ocena.GetProperty("krok_podejrzany_odczyt").ValueKind.ShouldBe(JsonValueKind.Null);
        ocena.GetProperty("wskazowka_w_przygotowaniu").GetBoolean().ShouldBeFalse();
        ocena.GetProperty("komunikat").ValueKind.ShouldBe(JsonValueKind.Null);
        ocena.GetProperty("mozliwe_zgloszenie_do").ValueKind.ShouldBe(JsonValueKind.Null);

        var kryteria = ocena.GetProperty("kryteria").EnumerateArray().ToList();
        kryteria.Count.ShouldBe(oczekiwana.PunktyKryteriow.Length);
        for (var i = 0; i < kryteria.Count; i++)
        {
            kryteria[i].GetProperty("nr_kryterium").GetInt32().ShouldBe(i + 1);
            kryteria[i].GetProperty("opis").GetString().ShouldNotBeNullOrWhiteSpace();
            kryteria[i].GetProperty("punkty").GetInt32().ShouldBe(oczekiwana.PunktyKryteriow[i]);
            kryteria[i].GetProperty("punkty_max").GetInt32().ShouldBe(1);
            kryteria[i].GetProperty("prawdopodobienstwo").GetDecimal().ShouldBe(0.995m);
        }

        PoleLiczbowe(ocena, "pierwszy_bledny_krok").ShouldBe(oczekiwana.PierwszyBlednyKrok);
        PoleTekstowe(ocena, "typ_bledu").ShouldBe(oczekiwana.TypBledu);
        PoleTekstowe(ocena, "wskazowka").ShouldBe(oczekiwana.Wskazowka);
        ocena.GetProperty("uzasadnienie").GetString().ShouldBe(Uzasadnienie(kryteria, oczekiwana));

        // GET …/ocena i pole ocena w GET /rozwiazania/{id} są identyczne.
        var osobno = await klient.PobierzAsync($"/api/v1/rozwiazania/{id}/ocena");
        osobno.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        osobno.Tresc.GetRawText().ShouldBe(ocena.GetRawText());

        await SprawdzSkutkiWBazieAsync(id, oczekiwana);
    }

    private async Task SprawdzSkutkiWBazieAsync(Guid id, KontraktOcena oczekiwana)
    {
        (await SkalarAsync<long>("SELECT count(*) FROM transkrypcja WHERE rozwiazanie_id = @id", ("id", id))).ShouldBe(1);
        (await SkalarAsync<long>("SELECT count(*) FROM ocena WHERE rozwiazanie_id = @id", ("id", id))).ShouldBe(1);
        (await SkalarAsync<long>("SELECT count(*) FROM ocena_kryterium ok JOIN ocena o ON o.id = ok.ocena_id WHERE o.rozwiazanie_id = @id", ("id", id)))
            .ShouldBe(oczekiwana.PunktyKryteriow.Length);

        // Z-4: po jednym wierszu decyzja_modelu na Kryterium, z rozkładem sumującym się do 1 i czasem wywołania z zegara testowego.
        (await SkalarAsync<long>("SELECT count(*) FROM decyzja_modelu WHERE rozwiazanie_id = @id AND kontekst = 'OCENA'", ("id", id)))
            .ShouldBe(oczekiwana.PunktyKryteriow.Length);
        (await SkalarAsync<int>("SELECT max(czas_ms) FROM decyzja_modelu WHERE rozwiazanie_id = @id", ("id", id))).ShouldBe(2000);

        // Wynik zadania (FR-29): źródło OCENA, ułamek = punkty/punkty_max, waga 1,00, liczy się do trudności (pierwsza próba).
        (await SkalarAsync<long>("SELECT count(*) FROM wynik_zadania w JOIN ocena o ON o.id = w.ocena_id WHERE o.rozwiazanie_id = @id", ("id", id))).ShouldBe(1);
        (await SkalarAsync<string>("SELECT w.zrodlo::text FROM wynik_zadania w JOIN ocena o ON o.id = w.ocena_id WHERE o.rozwiazanie_id = @id", ("id", id))).ShouldBe("OCENA");
        (await SkalarAsync<decimal>("SELECT w.ulamek FROM wynik_zadania w JOIN ocena o ON o.id = w.ocena_id WHERE o.rozwiazanie_id = @id", ("id", id)))
            .ShouldBe(Math.Round((decimal)oczekiwana.Punkty / oczekiwana.PunktyMax, 3));
        (await SkalarAsync<decimal>("SELECT w.waga FROM wynik_zadania w JOIN ocena o ON o.id = w.ocena_id WHERE o.rozwiazanie_id = @id", ("id", id))).ShouldBe(1.00m);
        (await SkalarAsync<bool>("SELECT w.liczy_do_trudnosci FROM wynik_zadania w JOIN ocena o ON o.id = w.ocena_id WHERE o.rozwiazanie_id = @id", ("id", id))).ShouldBeTrue();

        // Znaczniki czasu i czas systemowy (NFR-01): zegar testowy jest „przed” rzeczywistym o czas opóźnień Mocka.
        (await SkalarAsync<DateTime>("SELECT transkrypcja_gotowa_at - przeslano_at + timestamp '2000-01-01' FROM rozwiazanie WHERE id = @id", ("id", id)))
            .ShouldBeGreaterThanOrEqualTo(new DateTime(2000, 1, 1, 0, 0, 3, DateTimeKind.Unspecified));
        (await SkalarAsync<DateTime>("SELECT ocena_wystawiona_at - zatwierdzono_at + timestamp '2000-01-01' FROM rozwiazanie WHERE id = @id", ("id", id)))
            .ShouldBeGreaterThanOrEqualTo(new DateTime(2000, 1, 1, 0, 0, 2, DateTimeKind.Unspecified));
        (await SkalarAsync<int>("SELECT czas_systemowy_ms FROM rozwiazanie WHERE id = @id", ("id", id))).ShouldBeGreaterThanOrEqualTo(5000);

        // Rozstrzygnięcie punktu z TASK-127: techniczny element Ścieżki jest zamykany po Ocenie.
        (await SkalarAsync<string>("SELECT e.status::text FROM rozwiazanie r JOIN element_sciezki e ON e.id = r.element_sciezki_id WHERE r.id = @id", ("id", id)))
            .ShouldBe("UKONCZONY");
        (await SkalarAsync<long>("SELECT count(*) FROM praca_w_tle WHERE status = 'ZAKONCZONA' AND ladunek->>'rozwiazanie_id' = @id", ("id", id.ToString()))).ShouldBe(2);
    }

    private static int? PoleLiczbowe(JsonElement element, string nazwa) =>
        element.GetProperty(nazwa).ValueKind == JsonValueKind.Null ? null : element.GetProperty(nazwa).GetInt32();

    private static string? PoleTekstowe(JsonElement element, string nazwa) =>
        element.GetProperty(nazwa).ValueKind == JsonValueKind.Null ? null : element.GetProperty(nazwa).GetString();

    // Szablon uzasadnienia z kontraktu 7.3.
    private static string Uzasadnienie(List<JsonElement> kryteria, KontraktOcena oczekiwana)
    {
        var czesci = kryteria.Select(k => string.Create(
            CultureInfo.InvariantCulture,
            $"K{k.GetProperty("nr_kryterium").GetInt32()} „{k.GetProperty("opis").GetString()}” — {k.GetProperty("punkty").GetInt32()}/{k.GetProperty("punkty_max").GetInt32()} pkt"));
        var blad = oczekiwana.TypBledu switch
        {
            null => string.Empty,
            "RACHUNKOWY" => $"Pierwszy błędny krok: {oczekiwana.PierwszyBlednyKrok} (błąd rachunkowy). ",
            "MERYTORYCZNY" => $"Pierwszy błędny krok: {oczekiwana.PierwszyBlednyKrok} (błąd merytoryczny). ",
            _ => $"Pierwszy błędny krok: {oczekiwana.PierwszyBlednyKrok} (niepełne rozwiązanie). ",
        };
        return $"Kryteria: {string.Join("; ", czesci)}. {blad}Razem {oczekiwana.Punkty}/{oczekiwana.PunktyMax} pkt.";
    }

    // ---------- idempotencja handlerów ----------

    [Fact]
    public async Task HandleryUruchomioneDrugiRazNieDuplikujaTranskrypcjiOcenyAniWyniku()
    {
        using var klient = await KlientAsync();
        await using var worker = NowyWorker();
        var id = (await PrzeslijAsync(klient, 2)).Tresc.GetProperty("id").GetGuid();
        await worker.PrzetworzWszystkoAsync(Token);
        var transkrypcja = (await klient.PobierzAsync($"/api/v1/rozwiazania/{id}")).Tresc.GetProperty("transkrypcja");
        (await ZatwierdzAsync(klient, id, "\"3\"", CialoZatwierdzenia(transkrypcja))).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await worker.PrzetworzWszystkoAsync(Token);
        var wersja = await SkalarAsync<long>("SELECT version FROM rozwiazanie WHERE id = @id", ("id", id));

        using var zakres = worker.Uslugi.CreateScope();
        var rozpoznaj = zakres.ServiceProvider.GetServices<IHandlerPracy>().Single(h => h.Typ == TypPracy.RozpoznajPismo);
        var ocen = zakres.ServiceProvider.GetServices<IHandlerPracy>().Single(h => h.Typ == TypPracy.OcenRozwiazanie);
        await rozpoznaj.WykonajAsync(new PracaWTle(0, TypPracy.RozpoznajPismo, $$"""{"rozwiazanie_id":"{{id}}","przeslanie_nr":1,"ponowienie_nr":0,"mock_scenariusz":"domyslny"}""", 2, 3), Token);
        await ocen.WykonajAsync(new PracaWTle(0, TypPracy.OcenRozwiazanie, $$"""{"rozwiazanie_id":"{{id}}","transkrypcja_wersja":1,"mock_scenariusz":"domyslny"}""", 2, 3), Token);

        (await SkalarAsync<long>("SELECT count(*) FROM transkrypcja WHERE rozwiazanie_id = @id", ("id", id))).ShouldBe(1);
        (await SkalarAsync<long>("SELECT count(*) FROM ocena WHERE rozwiazanie_id = @id", ("id", id))).ShouldBe(1);
        (await SkalarAsync<long>("SELECT count(*) FROM wynik_zadania w JOIN ocena o ON o.id = w.ocena_id WHERE o.rozwiazanie_id = @id", ("id", id))).ShouldBe(1);
        (await SkalarAsync<long>("SELECT count(*) FROM decyzja_modelu WHERE rozwiazanie_id = @id", ("id", id))).ShouldBe(2);
        (await SkalarAsync<long>("SELECT version FROM rozwiazanie WHERE id = @id", ("id", id))).ShouldBe(wersja);
    }

    [Fact]
    public async Task PonowienieOcenyPoAwariiPoZapisieOcenyNieTworzyDrugiejOcenyIDokanczaPrzeplyw()
    {
        using var klient = await KlientAsync();
        await using var worker = NowyWorker();
        var id = (await PrzeslijAsync(klient, 4)).Tresc.GetProperty("id").GetGuid();
        await worker.PrzetworzWszystkoAsync(Token);
        var transkrypcja = (await klient.PobierzAsync($"/api/v1/rozwiazania/{id}")).Tresc.GetProperty("transkrypcja");
        await ZatwierdzAsync(klient, id, "\"3\"", CialoZatwierdzenia(transkrypcja));
        await worker.PrzetworzWszystkoAsync(Token);

        // Symulacja awarii między zapisem Oceny a OCENIONE: status wraca do OCENIANIE, Ocena i Wynik zostają.
        await WykonajAsync($"UPDATE rozwiazanie SET status = 'OCENIANIE', ocena_wystawiona_at = NULL, czas_systemowy_ms = NULL WHERE id = '{id}'");
        await WykonajAsync($"DELETE FROM wynik_zadania WHERE ocena_id IN (SELECT id FROM ocena WHERE rozwiazanie_id = '{id}')");
        await WykonajAsync($"UPDATE element_sciezki SET status = 'ROZPOCZETY', zakonczono_at = NULL WHERE id = (SELECT element_sciezki_id FROM rozwiazanie WHERE id = '{id}')");
        using var zakres = worker.Uslugi.CreateScope();
        var ocen = zakres.ServiceProvider.GetServices<IHandlerPracy>().Single(h => h.Typ == TypPracy.OcenRozwiazanie);
        await ocen.WykonajAsync(new PracaWTle(0, TypPracy.OcenRozwiazanie, $$"""{"rozwiazanie_id":"{{id}}","transkrypcja_wersja":1,"mock_scenariusz":"domyslny"}""", 2, 3), Token);

        (await SkalarAsync<long>("SELECT count(*) FROM ocena WHERE rozwiazanie_id = @id", ("id", id))).ShouldBe(1);
        (await SkalarAsync<string>("SELECT status::text FROM rozwiazanie WHERE id = @id", ("id", id))).ShouldBe("OCENIONE");
        (await SkalarAsync<long>("SELECT count(*) FROM wynik_zadania w JOIN ocena o ON o.id = w.ocena_id WHERE o.rozwiazanie_id = @id", ("id", id))).ShouldBe(1);
    }

    // ---------- scenariusz nieczytelny ----------

    [Fact]
    public async Task ScenariuszNieczytelneKonczySieBleduRozpoznawaniaIPozwalaPrzeslacNoweRozwiazanie()
    {
        using var klient = await KlientAsync();
        await using var worker = NowyWorker();
        var id = (await PrzeslijAsync(klient, 3, "rozpoznanie-nieczytelne")).Tresc.GetProperty("id").GetGuid();

        await worker.PrzetworzWszystkoAsync(Token);

        var rozwiazanie = (await klient.PobierzAsync($"/api/v1/rozwiazania/{id}")).Tresc;
        rozwiazanie.GetProperty("status").GetString().ShouldBe("BLAD_ROZPOZNAWANIA");
        rozwiazanie.GetProperty("kod_bledu_rozpoznawania").GetString().ShouldBe("NIECZYTELNE");
        rozwiazanie.GetProperty("transkrypcja").ValueKind.ShouldBe(JsonValueKind.Null);
        rozwiazanie.GetProperty("ocena").ValueKind.ShouldBe(JsonValueKind.Null);
        rozwiazanie.GetProperty("version").GetInt32().ShouldBe(3);
        rozwiazanie.GetProperty("nastepne_odpytanie_ms").ValueKind.ShouldBe(JsonValueKind.Null);
        (await SkalarAsync<long>("SELECT count(*) FROM transkrypcja WHERE rozwiazanie_id = @id", ("id", id))).ShouldBe(0);

        (await PrzeslijAsync(klient, 3)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    // ---------- zatwierdzenie: błędy ----------

    [Fact]
    public async Task ZatwierdzenieZBledamiZwracaKodyZKontraktu()
    {
        using var klient = await KlientAsync();
        using var obcy = await KlientZalogowany.ZarejestrujAsync(fabryka);
        await using var worker = NowyWorker();
        var id = (await PrzeslijAsync(klient, 5)).Tresc.GetProperty("id").GetGuid();
        var cialo = $$"""{ "transkrypcja_id": "{{Guid.NewGuid()}}", "wersja": 1 }""";

        // Status PRZESLANE: 409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN ze statusem w szczegolach.
        var zaWczesnie = await ZatwierdzAsync(klient, id, "\"1\"", cialo);
        zaWczesnie.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        zaWczesnie.Tresc.GetProperty("kod").GetString().ShouldBe("ROZWIAZANIE_NIEPRAWIDLOWY_STAN");
        zaWczesnie.Tresc.GetProperty("szczegoly").GetProperty("status").GetString().ShouldBe("PRZESLANE");

        await worker.PrzetworzWszystkoAsync(Token);
        var transkrypcja = (await klient.PobierzAsync($"/api/v1/rozwiazania/{id}")).Tresc.GetProperty("transkrypcja");
        var dobre = CialoZatwierdzenia(transkrypcja);

        (await ZatwierdzAsync(klient, id, null, dobre)).Odpowiedz.StatusCode.ShouldBe((HttpStatusCode)428);
        var zlyNaglowek = await ZatwierdzAsync(klient, id, "3", dobre);
        zlyNaglowek.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        zlyNaglowek.Tresc.GetProperty("kod").GetString().ShouldBe("NAGLOWEK_NIEPRAWIDLOWY");

        var wersja = await ZatwierdzAsync(klient, id, "\"1\"", dobre);
        wersja.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        wersja.Tresc.GetProperty("kod").GetString().ShouldBe("KONFLIKT_WERSJI");
        wersja.Tresc.GetProperty("szczegoly").GetProperty("aktualny").GetProperty("version").GetInt32().ShouldBe(3);

        var staraWersja = await ZatwierdzAsync(klient, id, "\"3\"", $$"""{ "transkrypcja_id": "{{transkrypcja.GetProperty("id").GetGuid()}}", "wersja": 2 }""");
        staraWersja.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        staraWersja.Tresc.GetProperty("kod").GetString().ShouldBe("TRANSKRYPCJA_NIEAKTUALNA");
        var innaTranskrypcja = await ZatwierdzAsync(klient, id, "\"3\"", cialo);
        innaTranskrypcja.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        innaTranskrypcja.Tresc.GetProperty("kod").GetString().ShouldBe("TRANSKRYPCJA_NIEAKTUALNA");

        var brakPol = await ZatwierdzAsync(klient, id, "\"3\"", "{}");
        brakPol.Odpowiedz.StatusCode.ShouldBe((HttpStatusCode)422);
        brakPol.Tresc.GetProperty("kod").GetString().ShouldBe("WALIDACJA");
        brakPol.Tresc.GetProperty("bledy_pol").EnumerateArray().Select(b => b.GetProperty("pole").GetString()).ShouldBe(["transkrypcja_id", "wersja"], ignoreOrder: true);

        // Cudze Rozwiązanie to 404 (ADR-071).
        (await ZatwierdzAsync(obcy, id, "\"3\"", dobre)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await obcy.PobierzAsync($"/api/v1/rozwiazania/{id}/ocena")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Żadna nieudana próba nie zleciła oceny ani nie zmieniła stanu.
        (await SkalarAsync<long>("SELECT count(*) FROM praca_w_tle WHERE typ = 'OCEN_ROZWIAZANIE' AND ladunek->>'rozwiazanie_id' = @id", ("id", id.ToString()))).ShouldBe(0);
        (await SkalarAsync<string>("SELECT status::text FROM rozwiazanie WHERE id = @id", ("id", id))).ShouldBe("TRANSKRYPCJA_GOTOWA");

        // Poprawne zatwierdzenie, a potem ponowne → 409 stan (także ze starym If-Match).
        (await ZatwierdzAsync(klient, id, "\"3\"", dobre)).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        foreach (var ifMatch in new[] { "\"3\"", "\"4\"" })
        {
            var ponowne = await ZatwierdzAsync(klient, id, ifMatch, dobre);
            ponowne.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            ponowne.Tresc.GetProperty("kod").GetString().ShouldBe("ROZWIAZANIE_NIEPRAWIDLOWY_STAN");
            ponowne.Tresc.GetProperty("szczegoly").GetProperty("status").GetString().ShouldBe("ZATWIERDZONE");
        }

        (await SkalarAsync<long>("SELECT count(*) FROM praca_w_tle WHERE typ = 'OCEN_ROZWIAZANIE' AND ladunek->>'rozwiazanie_id' = @id", ("id", id.ToString()))).ShouldBe(1);
    }

    [Fact]
    public async Task RownoleglaDwaZatwierdzeniaTegoSamegoRozwiazaniaZlecajDokladnieJednaOcene()
    {
        using var klient = await KlientAsync();
        await using var worker = NowyWorker();
        var id = (await PrzeslijAsync(klient, 1)).Tresc.GetProperty("id").GetGuid();
        await worker.PrzetworzWszystkoAsync(Token);
        var cialo = CialoZatwierdzenia((await klient.PobierzAsync($"/api/v1/rozwiazania/{id}")).Tresc.GetProperty("transkrypcja"));

        var wyniki = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => ZatwierdzAsync(klient, id, "\"3\"", cialo)));

        wyniki.Count(w => w.Odpowiedz.StatusCode == HttpStatusCode.Accepted).ShouldBe(1);
        wyniki.Single(w => w.Odpowiedz.StatusCode != HttpStatusCode.Accepted).Odpowiedz.StatusCode
            .ShouldBeOneOf(HttpStatusCode.Conflict, HttpStatusCode.PreconditionFailed);
        (await SkalarAsync<long>("SELECT count(*) FROM praca_w_tle WHERE typ = 'OCEN_ROZWIAZANIE' AND ladunek->>'rozwiazanie_id' = @id", ("id", id.ToString()))).ShouldBe(1);
    }

    [Fact]
    public async Task EndpointyZatwierdzeniaIOcenyWymagajaUwierzytelnienia()
    {
        await PocSeed.ZapewnijAsync(baza.Polaczenie, Token);
        using var anonim = KlientZalogowany.Anonimowy(fabryka);
        var id = Guid.NewGuid();

        (await ZatwierdzAsync(anonim, id, "\"1\"", "{}")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonim.PobierzAsync($"/api/v1/rozwiazania/{id}/ocena")).Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
