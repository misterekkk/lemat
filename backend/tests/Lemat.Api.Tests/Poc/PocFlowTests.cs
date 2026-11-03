// @Role: Test integracyjny pełnego przepływu POC na prawdziwej bazie i prawdziwej pętli PocPracaHostedService: rejestracja → PATCH /uczniowie/ja → lista Zadań → przesłanie JPEG → odpytywanie do TRANSKRYPCJA_GOTOWA → zatwierdzenie → odpytywanie do OCENIONE → Ocena → profil uproszczony; izolacja Rozwiązań między Uczniami | @DependsOn: ApiFactory, WorkerHost, PocPracaHostedService, KlientZalogowany, PocSeed, TestowyJpeg, KontraktPoc | @UsedBy: dotnet test | @Task: TASK-130
extern alias WorkerAsm;

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Lemat.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using WorkerAsm::Lemat.Worker.Configuration;
using WorkerAsm::Lemat.Worker.Jobs;

namespace Lemat.Api.Tests.Poc;

[Collection(KolekcjeTestowe.DanePoc)]
public sealed class PocFlowTests(ApiFactory fabryka, BazaTestowaFixture baza) : IClassFixture<ApiFactory>
{
    private const int Zadanie = 2;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string WersjaZadania(int numer) => $"b1000000-0000-4000-8000-00000000000{numer}";

    private static string IdZadania(int numer) => $"a1000000-0000-4000-8000-00000000000{numer}";

    /// <summary>Worker z prawdziwą pętlą odpytywania (bez Kestrela), skróconymi opóźnieniami Mocka i szybkim odpytywaniem.</summary>
    private static async Task<IHost> UruchomWorkeraAsync()
    {
        var builder = WorkerHost.CreateBuilder(
        [
            "--environment=Development",
            "--Lemat:Poc:OpoznienieRozpoznaniaMs=60",
            "--Lemat:Poc:OpoznienieOcenyMs=60",
            "--Lemat:Poc:OdpytanieMs=25",
        ]);
        var host = builder.Build();
        foreach (var usluga in host.Services.GetServices<IHostedService>().OfType<PocPracaHostedService>())
        {
            await usluga.StartAsync(Token);
        }

        return host;
    }

    private static async Task ZatrzymajWorkeraAsync(IHost host)
    {
        foreach (var usluga in host.Services.GetServices<IHostedService>().OfType<PocPracaHostedService>())
        {
            await usluga.StopAsync(CancellationToken.None);
        }

        await host.StopAsync(CancellationToken.None);
        host.Dispose();
    }

    private async Task<KlientZalogowany> NowyUczenAsync()
    {
        await PocSeed.ZapewnijAsync(baza.Polaczenie, Token);
        await using var zrodlo = NpgsqlDataSource.Create(baza.Polaczenie);
        await using var polecenie = zrodlo.CreateCommand("UPDATE praca_w_tle SET status = 'ZAKONCZONA', wykonawca = NULL, dzierzawa_do = NULL WHERE status IN ('OCZEKUJE','W_TOKU')");
        await polecenie.ExecuteNonQueryAsync(Token);
        return await KlientZalogowany.ZarejestrujAsync(fabryka);
    }

    private static async Task<JsonElement> CzekajNaStatusAsync(KlientZalogowany klient, Guid id, string oczekiwany)
    {
        var stoper = Stopwatch.StartNew();
        var ostatni = string.Empty;
        while (stoper.Elapsed < TimeSpan.FromSeconds(30))
        {
            var odpowiedz = await klient.PobierzAsync($"/api/v1/rozwiazania/{id}");
            odpowiedz.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
            ostatni = odpowiedz.Tresc.GetProperty("status").GetString()!;
            if (ostatni == oczekiwany)
            {
                return odpowiedz.Tresc;
            }

            ostatni.ShouldNotBe("BLAD_ROZPOZNAWANIA");
            await Task.Delay(50, Token);
        }

        throw new TimeoutException($"Rozwiązanie {id} nie osiągnęło statusu {oczekiwany} (ostatni: {ostatni}).");
    }

    private static Task<OdpowiedzTestowa> PrzeslijAsync(KlientZalogowany klient, int zadanie)
    {
        var tresc = new MultipartFormDataContent { { new StringContent(WersjaZadania(zadanie)), "zadanie_wersja_id" } };
        var plik = new ByteArrayContent(TestowyJpeg.Utworz());
        plik.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        tresc.Add(plik, "zdjecia", "kartka.jpg");
        return klient.WyslijFormularzAsync("/api/v1/rozwiazania", tresc);
    }

    [Fact]
    public async Task PelnyPrzeplywPocOdRejestracjiDoProfiluIIzolacjaMiedzyUczniami()
    {
        using var uczen = await NowyUczenAsync();
        using var drugi = await KlientZalogowany.ZarejestrujAsync(fabryka);
        var host = await UruchomWorkeraAsync();
        try
        {
            // Ustawienia Ucznia (If-Match = version z GET).
            var ja = await uczen.PobierzAsync("/api/v1/uczniowie/ja");
            var zmiana = await uczen.ZmienAsync(
                """{ "poziom_matury": "PODSTAWOWY", "termin_egzaminu": "2027-05-05", "wybrany_dzial_id": 3 }""",
                $"\"{ja.Tresc.GetProperty("version").GetInt32()}\"");
            zmiana.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
            zmiana.Tresc.GetProperty("poziom_matury").GetString().ShouldBe("PODSTAWOWY");

            // Lista Zadań (kolejność kontraktu, 5 Zadań).
            var lista = await uczen.PobierzAsync("/api/v1/zadania");
            lista.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
            lista.Tresc.GetProperty("elementy").EnumerateArray().Select(e => e.GetProperty("id").GetString()).ShouldContain(IdZadania(Zadanie));

            // Przesłanie JPEG → 202 PRZESLANE.
            var przeslane = await PrzeslijAsync(uczen, Zadanie);
            przeslane.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            var id = przeslane.Tresc.GetProperty("id").GetGuid();
            przeslane.Tresc.GetProperty("status").GetString().ShouldBe("PRZESLANE");

            // Odpytywanie do TRANSKRYPCJA_GOTOWA; Transkrypcja z tabeli 7.2.
            var gotowe = await CzekajNaStatusAsync(uczen, id, "TRANSKRYPCJA_GOTOWA");
            var transkrypcja = gotowe.GetProperty("transkrypcja");
            transkrypcja.GetProperty("kroki").GetArrayLength().ShouldBe(KontraktPoc.Kroki(Zadanie).Count);

            // Zatwierdzenie → 202 ZATWIERDZONE.
            var cialo = $$"""{ "transkrypcja_id": "{{transkrypcja.GetProperty("id").GetGuid()}}", "wersja": {{transkrypcja.GetProperty("wersja").GetInt32()}} }""";
            var zatwierdzone = await uczen.WyslijAsync(
                HttpMethod.Post, $"/api/v1/rozwiazania/{id}/zatwierdzenie", cialo, $"\"{gotowe.GetProperty("version").GetInt32()}\"");
            zatwierdzone.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            zatwierdzone.Tresc.GetProperty("status").GetString().ShouldBe("ZATWIERDZONE");

            // Odpytywanie do OCENIONE; Ocena z tabeli 7.3.
            var ocenione = await CzekajNaStatusAsync(uczen, id, "OCENIONE");
            var oczekiwana = KontraktPoc.Ocena(Zadanie);
            ocenione.GetProperty("ocena").GetProperty("punkty").GetInt32().ShouldBe(oczekiwana.Punkty);

            var ocena = await uczen.PobierzAsync($"/api/v1/rozwiazania/{id}/ocena");
            ocena.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
            ocena.Tresc.GetProperty("status").GetString().ShouldBe("OSTATECZNA");
            ocena.Tresc.GetProperty("punkty").GetInt32().ShouldBe(oczekiwana.Punkty);
            ocena.Tresc.GetProperty("punkty_max").GetInt32().ShouldBe(oczekiwana.PunktyMax);

            // Profil uproszczony widzi tę Ocenę.
            var profil = await uczen.PobierzAsync("/api/v1/uczniowie/ja/profil-uproszczony");
            profil.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.OK);
            profil.Tresc.GetProperty("uczen_id").GetGuid().ShouldBe(uczen.Id);
            profil.Tresc.GetProperty("liczba_ocenionych").GetInt32().ShouldBe(1);
            profil.Tresc.GetProperty("suma_punktow").GetDecimal().ShouldBe(oczekiwana.Punkty);
            profil.Tresc.GetProperty("ostatnie_oceny")[0].GetProperty("rozwiazanie_id").GetGuid().ShouldBe(id);

            // Drugi Uczeń nie widzi cudzych Rozwiązań ani Oceny (404) i ma pusty profil.
            foreach (var sciezka in new[] { $"/api/v1/rozwiazania/{id}", $"/api/v1/rozwiazania/{id}/ocena" })
            {
                var cudze = await drugi.PobierzAsync(sciezka);
                cudze.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
                cudze.Tresc.GetProperty("kod").GetString().ShouldBe("NIE_ZNALEZIONO");
            }

            var cudzeZatwierdzenie = await drugi.WyslijAsync(HttpMethod.Post, $"/api/v1/rozwiazania/{id}/zatwierdzenie", cialo, "\"6\"");
            cudzeZatwierdzenie.Odpowiedz.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await drugi.PobierzAsync("/api/v1/uczniowie/ja/profil-uproszczony")).Tresc.GetProperty("liczba_rozwiazan").GetInt32().ShouldBe(0);
        }
        finally
        {
            await ZatrzymajWorkeraAsync(host);
        }
    }
}
