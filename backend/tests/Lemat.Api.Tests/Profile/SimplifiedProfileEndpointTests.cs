// @Role: Testy integracyjne GET /uczniowie/ja/profil-uproszczony (kontrakt POC 4.3) na prawdziwej bazie: nowy Uczeń, przykład z kontraktu (Zadania 1, 2, 5 ocenione + Zadanie 3 w trakcie), izolacja Uczniów, 401 | @DependsOn: ApiFactory, WorkerTestowy, ZegarTestowy, KlientZalogowany, PocSeed, TestowyJpeg | @UsedBy: dotnet test | @Task: TASK-129
using System.Net;
using System.Text.Json;
using Lemat.Api.Tests.Support;
using Npgsql;

namespace Lemat.Api.Tests.Profile;

[Collection(KolekcjeTestowe.DanePoc)]
public sealed class SimplifiedProfileEndpointTests(ApiFactory fabryka, BazaTestowaFixture baza) : IClassFixture<ApiFactory>
{
    private const string Sciezka = "/api/v1/uczniowie/ja/profil-uproszczony";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string WersjaZadania(int numer) => $"b1000000-0000-4000-8000-00000000000{numer}";

    private async Task<KlientZalogowany> KlientAsync()
    {
        await PocSeed.ZapewnijAsync(baza.Polaczenie, Token);

        // Prace zostawione przez inne testy nie mogą zakłócać potoku tego testu.
        await using var zrodlo = NpgsqlDataSource.Create(baza.Polaczenie);
        await using var polecenie = zrodlo.CreateCommand("UPDATE praca_w_tle SET status = 'ZAKONCZONA', wykonawca = NULL, dzierzawa_do = NULL WHERE status IN ('OCZEKUJE','W_TOKU')");
        await polecenie.ExecuteNonQueryAsync(Token);
        return await KlientZalogowany.ZarejestrujAsync(fabryka);
    }

    private static async Task<Guid> PrzeslijAsync(KlientZalogowany klient, int zadanie)
    {
        var tresc = new MultipartFormDataContent { { new StringContent(WersjaZadania(zadanie)), "zadanie_wersja_id" } };
        var plik = new ByteArrayContent(TestowyJpeg.Utworz());
        plik.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        tresc.Add(plik, "zdjecia", "kartka.jpg");
        var odpowiedz = await klient.WyslijFormularzAsync("/api/v1/rozwiazania", tresc);
        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        return odpowiedz.Tresc.GetProperty("id").GetGuid();
    }

    /// <summary>Przesyła, zatwierdza i oddaje do oceny jedno Zadanie, a worker doprowadza je do OCENIONE.</summary>
    private static async Task<Guid> OcenZadanieAsync(KlientZalogowany klient, WorkerTestowy worker, int zadanie)
    {
        var id = await PrzeslijAsync(klient, zadanie);
        await worker.PrzetworzWszystkoAsync(Token);
        var gotowe = (await klient.PobierzAsync($"/api/v1/rozwiazania/{id}")).Tresc;
        var transkrypcja = gotowe.GetProperty("transkrypcja");
        var cialo = $$"""{ "transkrypcja_id": "{{transkrypcja.GetProperty("id").GetGuid()}}", "wersja": {{transkrypcja.GetProperty("wersja").GetInt32()}} }""";
        var zatwierdzone = await klient.WyslijAsync(HttpMethod.Post, $"/api/v1/rozwiazania/{id}/zatwierdzenie", cialo, $"\"{gotowe.GetProperty("version").GetInt32()}\"");
        zatwierdzone.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await worker.PrzetworzWszystkoAsync(Token);
        (await klient.PobierzAsync($"/api/v1/rozwiazania/{id}")).Tresc.GetProperty("status").GetString().ShouldBe("OCENIONE");
        return id;
    }

    [Fact]
    public async Task NowyUczenMaPustyProfil()
    {
        using var klient = await KlientAsync();

        var odpowiedz = await klient.PobierzAsync(Sciezka);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profil = odpowiedz.Tresc;
        profil.GetProperty("uczen_id").GetGuid().ShouldBe(klient.Id);
        profil.GetProperty("liczba_rozwiazan").GetInt32().ShouldBe(0);
        profil.GetProperty("liczba_ocenionych").GetInt32().ShouldBe(0);
        profil.GetProperty("suma_punktow").GetDecimal().ShouldBe(0);
        profil.GetProperty("suma_punktow_max").GetDecimal().ShouldBe(0);
        profil.GetProperty("sredni_procent_punktow").ValueKind.ShouldBe(JsonValueKind.Null);
        profil.GetProperty("dzialy").GetArrayLength().ShouldBe(0);
        profil.GetProperty("ostatnie_oceny").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task ProfilPoOcenieniuZadanWgPrzykladuKontraktu()
    {
        using var klient = await KlientAsync();
        using var inny = await KlientZalogowany.ZarejestrujAsync(fabryka);
        await using var worker = new WorkerTestowy(new ZegarTestowy(DateTimeOffset.UtcNow));

        // Dane innego Ucznia nie mogą wpływać na wynik.
        await OcenZadanieAsync(inny, worker, 1);

        var r1 = await OcenZadanieAsync(klient, worker, 1);
        var r2 = await OcenZadanieAsync(klient, worker, 2);
        var r5 = await OcenZadanieAsync(klient, worker, 5);
        await PrzeslijAsync(klient, 3); // w trakcie (PRZESLANE): liczy się do liczba_rozwiazan, nie do liczba_ocenionych

        var odpowiedz = await klient.PobierzAsync(Sciezka);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profil = odpowiedz.Tresc;
        profil.GetProperty("uczen_id").GetGuid().ShouldBe(klient.Id);
        profil.GetProperty("liczba_rozwiazan").GetInt32().ShouldBe(4);
        profil.GetProperty("liczba_ocenionych").GetInt32().ShouldBe(3);
        profil.GetProperty("suma_punktow").GetDecimal().ShouldBe(3);
        profil.GetProperty("suma_punktow_max").GetDecimal().ShouldBe(6);
        profil.GetProperty("sredni_procent_punktow").GetDecimal().ShouldBe(50.0m);

        var dzialy = profil.GetProperty("dzialy").EnumerateArray().ToList();
        dzialy.Select(d => d.GetProperty("dzial").GetProperty("id").GetInt32()).ShouldBe([3, 5, 8]);
        dzialy.Select(d => d.GetProperty("dzial").GetProperty("nazwa").GetString()).ShouldBe(["Równania i nierówności", "Funkcje", "Planimetria"]);
        dzialy.Select(d => d.GetProperty("liczba_ocenionych").GetInt32()).ShouldBe([1, 1, 1]);
        dzialy.Select(d => d.GetProperty("sredni_procent_punktow").GetDecimal()).ShouldBe([50.0m, 100.0m, 0.0m]);

        var ostatnie = profil.GetProperty("ostatnie_oceny").EnumerateArray().ToList();
        ostatnie.Select(o => o.GetProperty("rozwiazanie_id").GetGuid()).ShouldBe([r5, r2, r1]);
        ostatnie.Select(o => o.GetProperty("zadanie_id").GetString()).ShouldBe(
            ["a1000000-0000-4000-8000-000000000005", "a1000000-0000-4000-8000-000000000002", "a1000000-0000-4000-8000-000000000001"]);
        ostatnie.Select(o => o.GetProperty("punkty").GetInt32()).ShouldBe([0, 1, 2]);
        ostatnie.Select(o => o.GetProperty("punkty_max").GetInt32()).ShouldBe([2, 2, 2]);
        ostatnie.Select(o => o.GetProperty("dzial").GetProperty("id").GetInt32()).ShouldBe([8, 3, 5]);
        ostatnie[0].GetProperty("oceniono_at").GetDateTimeOffset().ShouldBeGreaterThan(ostatnie[2].GetProperty("oceniono_at").GetDateTimeOffset());
    }

    [Fact]
    public async Task BezTokenuZwracaNieuwierzytelniony()
    {
        using var anonim = KlientZalogowany.Anonimowy(fabryka);

        var odpowiedz = await anonim.PobierzAsync(Sciezka);

        odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
