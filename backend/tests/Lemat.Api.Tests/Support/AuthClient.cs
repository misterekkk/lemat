// @Role: Pomocnik testów kont: wywołania /auth/* bez automatycznej obsługi ciasteczek, odczyt Set-Cookie i ciała JSON, unikalne dane rejestracji | @DependsOn: ApiFactory, HttpClient | @UsedBy: Accounts/AuthenticationTests | @Task: TASK-123
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lemat.Api.Tests.Support;

public sealed record OdpowiedzTestowa(HttpResponseMessage Odpowiedz, JsonElement Tresc)
{
    public string? Ciasteczko => PobierzCiasteczko(Odpowiedz);

    public string? NaglowekCiasteczka =>
        Odpowiedz.Headers.TryGetValues("Set-Cookie", out var wartosci) ? wartosci.FirstOrDefault(w => w.StartsWith("lemat_rt=", StringComparison.Ordinal)) : null;

    /// <summary>Wartość ciasteczka <c>lemat_rt</c> z nagłówka Set-Cookie (puste = ciasteczko wyczyszczone).</summary>
    public static string? PobierzCiasteczko(HttpResponseMessage odpowiedz)
    {
        if (!odpowiedz.Headers.TryGetValues("Set-Cookie", out var wartosci))
        {
            return null;
        }

        var naglowek = wartosci.FirstOrDefault(w => w.StartsWith("lemat_rt=", StringComparison.Ordinal));
        return naglowek?.Split(';')[0]["lemat_rt=".Length..];
    }
}

public sealed class KlientAuth(ApiFactory fabryka) : IDisposable
{
    public const string Haslo = "BardzoTajne#2026";

    private readonly HttpClient _klient = fabryka.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });

    public static string NowyEmail() => $"uczen.{Guid.NewGuid():N}@example.com";

    public static object Rejestracja(string? email = null, string haslo = Haslo, string rola = "UCZEN", bool oswiadczenie = true, bool regulamin = true) => new
    {
        email = email ?? NowyEmail(),
        haslo,
        imie = "Ola",
        nazwisko = "Wiśniewska",
        rola,
        oswiadczenie_wieku_16 = oswiadczenie,
        zgoda_regulamin = regulamin,
        kod_zaproszenia = (string?)null,
    };

    public Task<OdpowiedzTestowa> ZarejestrujAsync(object? tresc = null) => WyslijAsync("/api/v1/auth/rejestracja", tresc ?? Rejestracja());

    public Task<OdpowiedzTestowa> ZalogujAsync(string email, string haslo) => WyslijAsync("/api/v1/auth/logowanie", new { email, haslo });

    public Task<OdpowiedzTestowa> OdswiezAsync(string? ciasteczko) => WyslijAsync("/api/v1/auth/odswiezenie", null, ciasteczko);

    public Task<OdpowiedzTestowa> WylogujAsync(string? token, string? ciasteczko = null) =>
        WyslijAsync("/api/v1/auth/wylogowanie", null, ciasteczko, token);

    public async Task<OdpowiedzTestowa> WyslijAsync(string sciezka, object? tresc, string? ciasteczko = null, string? tokenDostepu = null)
    {
        using var zadanie = new HttpRequestMessage(HttpMethod.Post, sciezka);
        if (tresc is not null)
        {
            zadanie.Content = JsonContent.Create(tresc);
        }

        if (ciasteczko is not null)
        {
            zadanie.Headers.Add("Cookie", $"lemat_rt={ciasteczko}");
        }

        if (tokenDostepu is not null)
        {
            zadanie.Headers.Authorization = new("Bearer", tokenDostepu);
        }

        var odpowiedz = await _klient.SendAsync(zadanie, TestContext.Current.CancellationToken);
        var tekst = await odpowiedz.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var json = string.IsNullOrWhiteSpace(tekst) ? default : JsonDocument.Parse(tekst).RootElement.Clone();
        return new OdpowiedzTestowa(odpowiedz, json);
    }

    public void Dispose() => _klient.Dispose();
}
