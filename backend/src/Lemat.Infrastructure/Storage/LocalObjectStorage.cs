// @Role: Adapter MagazynObiektowLokalny (U-03): obiekty jako pliki w katalogu z LEMAT_MAGAZYN_SCIEZKA, adresy podpisane HMAC-SHA256 (token base64url(payload).base64url(podpis)) ważne do wygaśnięcia; odczyt po tokenie dla GET /magazyn-lokalny/{token} | @DependsOn: IMagazynObiektow, IOdczytPodpisanegoAdresu, PodpisanyAdres, TimeProvider | @UsedBy: ObjectStorageRegistration, PrzeslijRozwiazanieHandler, EndpointyRozwiazan | @Task: TASK-127
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Lemat.Application.Models;

namespace Lemat.Infrastructure.Storage;

/// <summary>
/// Magazyn na dysku. Konstruktor jest tani i nie dotyka systemu plików (host generatora OpenAPI nie ma ścieżki);
/// katalog powstaje przy pierwszym zapisie. Klucz obiektu: segmenty <c>[A-Za-z0-9._-]</c> rozdzielone <c>/</c>, bez <c>..</c>.
/// </summary>
public sealed partial class MagazynObiektowLokalny(string katalog, string klucz, TimeProvider czas) : IMagazynObiektow, IOdczytPodpisanegoAdresu
{
    /// <summary>Klucz podpisu tylko dla Development/E2E, gdy nie ustawiono <c>LEMAT_MAGAZYN_KLUCZ</c>.</summary>
    public const string KluczDeweloperski = "lemat-dev-magazyn-klucz-nie-uzywac-w-produkcji";

    /// <summary>Prefiks adresu (względny, ten sam origin); endpoint <c>GET /api/v1/magazyn-lokalny/{token}</c>.</summary>
    public const string PrefiksAdresu = "/api/v1/magazyn-lokalny/";

    private readonly byte[] _klucz = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(klucz) ? KluczDeweloperski : klucz);

    [GeneratedRegex("^[A-Za-z0-9._-]+(/[A-Za-z0-9._-]+)*$")]
    private static partial Regex PoprawnyKlucz();

    public async Task ZapiszAsync(string klucz, ReadOnlyMemory<byte> dane, string typMime, CancellationToken ct)
    {
        var sciezka = Sciezka(klucz);
        Directory.CreateDirectory(Path.GetDirectoryName(sciezka)!);
        var tymczasowy = sciezka + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(tymczasowy, dane.ToArray(), ct);
            File.Move(tymczasowy, sciezka, overwrite: true);
        }
        finally
        {
            if (File.Exists(tymczasowy))
            {
                File.Delete(tymczasowy);
            }
        }
    }

    public Task<byte[]> PobierzAsync(string klucz, CancellationToken ct) => File.ReadAllBytesAsync(Sciezka(klucz), ct);

    public Task UsunAsync(string klucz, CancellationToken ct)
    {
        var sciezka = Sciezka(klucz);
        if (File.Exists(sciezka))
        {
            File.Delete(sciezka);
        }

        return Task.CompletedTask;
    }

    public PodpisanyAdres PodpisanyUrl(string klucz, TimeSpan waznosc)
    {
        Sciezka(klucz);
        if (waznosc <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(waznosc), "Ważność adresu musi być dodatnia.");
        }

        // Wygaśnięcie z dokładnością do sekundy: ta sama wartość trafia do tokenu i do wygasa_at odpowiedzi.
        var wygasa = DateTimeOffset.FromUnixTimeSeconds((czas.GetUtcNow() + waznosc).ToUnixTimeSeconds());
        var ladunek = Encoding.UTF8.GetBytes(klucz + "\n" + wygasa.ToUnixTimeSeconds());
        var token = Base64Url.EncodeToString(ladunek) + "." + Base64Url.EncodeToString(HMACSHA256.HashData(_klucz, ladunek));
        return new PodpisanyAdres(new Uri(PrefiksAdresu + token, UriKind.Relative), wygasa);
    }

    /// <summary>
    /// Odczytuje obiekt wskazany podpisanym tokenem; <c>null</c> dla tokenu zmienionego, wygasłego, o złej formie albo
    /// wskazującego nieistniejący obiekt (endpoint zwraca wtedy <c>404</c>).
    /// </summary>
    public async Task<byte[]?> OtworzPodpisanyAsync(string token, CancellationToken ct)
    {
        var klucz = Zweryfikuj(token);
        if (klucz is null)
        {
            return null;
        }

        var sciezka = Sciezka(klucz);
        return File.Exists(sciezka) ? await File.ReadAllBytesAsync(sciezka, ct) : null;
    }

    private string? Zweryfikuj(string token)
    {
        try
        {
            var czesci = token.Split('.');
            if (czesci.Length != 2)
            {
                return null;
            }

            var ladunek = Base64Url.DecodeFromChars(czesci[0]);
            var podpis = Base64Url.DecodeFromChars(czesci[1]);
            if (!CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(_klucz, ladunek), podpis))
            {
                return null;
            }

            var tekst = Encoding.UTF8.GetString(ladunek).Split('\n');
            if (tekst.Length != 2 || !long.TryParse(tekst[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var wygasa))
            {
                return null;
            }

            return czas.GetUtcNow().ToUnixTimeSeconds() >= wygasa || !PoprawnyKlucz().IsMatch(tekst[0]) ? null : tekst[0];
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            return null;
        }
    }

    private string Sciezka(string klucz)
    {
        if (string.IsNullOrWhiteSpace(katalog))
        {
            throw new InvalidOperationException("Brak Lemat:Magazyn:Sciezka (LEMAT_MAGAZYN_SCIEZKA) dla magazynu lokalnego.");
        }

        if (string.IsNullOrEmpty(klucz) || klucz.Contains("..", StringComparison.Ordinal) || !PoprawnyKlucz().IsMatch(klucz))
        {
            throw new ArgumentException("Niepoprawny klucz obiektu magazynu.", nameof(klucz));
        }

        var korzen = Path.GetFullPath(katalog);
        var pelna = Path.GetFullPath(Path.Combine(korzen, klucz));
        return pelna.StartsWith(korzen + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? pelna
            : throw new ArgumentException("Klucz obiektu wychodzi poza katalog magazynu.", nameof(klucz));
    }
}
