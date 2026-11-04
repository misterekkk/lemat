// @Role: Pomocnik testów endpointów Ucznia: rejestruje konto, trzyma token dostępu i wysyła GET/PATCH z nagłówkami (If-Match, Content-Type) oraz odczytem JSON | @DependsOn: ApiFactory, KlientAuth, OdpowiedzTestowa | @UsedBy: Accounts/UczenEndpointsTests, Content/SectionsEndpointsTests | @Task: TASK-124, TASK-127
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lemat.Api.Tests.Support;

public sealed class KlientZalogowany : IDisposable
{
    private readonly HttpClient _klient;

    private KlientZalogowany(HttpClient klient, string? token, string email, Guid id)
    {
        _klient = klient;
        Token = token;
        Email = email;
        Id = id;
    }

    public string? Token { get; }

    public string Email { get; }

    public Guid Id { get; }

    /// <summary>Rejestruje nowego Ucznia (token dostępu z odpowiedzi rejestracji).</summary>
    public static async Task<KlientZalogowany> ZarejestrujAsync(ApiFactory fabryka)
    {
        using var auth = new KlientAuth(fabryka);
        var email = KlientAuth.NowyEmail();
        var odpowiedz = await auth.ZarejestrujAsync(KlientAuth.Rejestracja(email));
        var sesja = odpowiedz.Tresc.GetProperty("sesja");
        var klient = fabryka.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        return new KlientZalogowany(klient, sesja.GetProperty("access_token").GetString(), email, sesja.GetProperty("uzytkownik").GetProperty("id").GetGuid());
    }

    /// <summary>Klient bez tokenu (do sprawdzenia 401).</summary>
    public static KlientZalogowany Anonimowy(ApiFactory fabryka) =>
        new(fabryka.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false }), null, string.Empty, Guid.Empty);

    public Task<OdpowiedzTestowa> PobierzAsync(string sciezka) => WyslijAsync(HttpMethod.Get, sciezka);

    public Task<OdpowiedzTestowa> ZmienAsync(string json, string? ifMatch, string typTresci = "application/merge-patch+json") =>
        WyslijAsync(HttpMethod.Patch, "/api/v1/uczniowie/ja", json, ifMatch, typTresci);

    public async Task<OdpowiedzTestowa> WyslijAsync(HttpMethod metoda, string sciezka, string? json = null, string? ifMatch = null, string typTresci = "application/json")
    {
        using var zadanie = new HttpRequestMessage(metoda, sciezka);
        if (Token is not null)
        {
            zadanie.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        if (ifMatch is not null)
        {
            zadanie.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        if (json is not null)
        {
            zadanie.Content = new StringContent(json, Encoding.UTF8);
            zadanie.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(typTresci);
        }

        var odpowiedz = await _klient.SendAsync(zadanie, TestContext.Current.CancellationToken);
        var tekst = await odpowiedz.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var tresc = string.IsNullOrWhiteSpace(tekst) ? default : JsonDocument.Parse(tekst).RootElement.Clone();
        return new OdpowiedzTestowa(odpowiedz, tresc);
    }

    /// <summary>Wysyła multipart/form-data (np. POST /rozwiazania) z tokenem i opcjonalnym nagłówkiem Mock; ciało odpowiedzi czytane jako JSON.</summary>
    public async Task<OdpowiedzTestowa> WyslijFormularzAsync(string sciezka, MultipartFormDataContent tresc, string? scenariuszMock = null)
    {
        using var zadanie = new HttpRequestMessage(HttpMethod.Post, sciezka) { Content = tresc };
        if (Token is not null)
        {
            zadanie.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        if (scenariuszMock is not null)
        {
            zadanie.Headers.TryAddWithoutValidation("X-Lemat-Mock-Scenariusz", scenariuszMock);
        }

        var odpowiedz = await _klient.SendAsync(zadanie, TestContext.Current.CancellationToken);
        var tekst = await odpowiedz.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var json = string.IsNullOrWhiteSpace(tekst) || !odpowiedz.Content.Headers.ContentType!.MediaType!.Contains("json", StringComparison.Ordinal)
            ? default
            : JsonDocument.Parse(tekst).RootElement.Clone();
        return new OdpowiedzTestowa(odpowiedz, json);
    }

    /// <summary>Surowe GET (bez parsowania JSON), np. obraz z magazynu lokalnego; token dostępu dołączany tylko, gdy <paramref name="zTokenem"/>.</summary>
    public async Task<HttpResponseMessage> PobierzSurowoAsync(string sciezka, bool zTokenem = false)
    {
        using var zadanie = new HttpRequestMessage(HttpMethod.Get, sciezka);
        if (zTokenem && Token is not null)
        {
            zadanie.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        return await _klient.SendAsync(zadanie, TestContext.Current.CancellationToken);
    }

    public void Dispose() => _klient.Dispose();
}
