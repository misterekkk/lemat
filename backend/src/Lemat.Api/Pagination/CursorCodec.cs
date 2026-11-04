// @Role: Kodowanie i weryfikacja nieprzezroczystego kursora stronicowania z podpisem HMAC-SHA256 (zły/podrobiony kursor -> 400 KURSOR_NIEPRAWIDLOWY) | @DependsOn: JsonApi, BladApiException, LematOpcje.Kursor | @UsedBy: ApiHost (singleton), endpointy list | @Task: TASK-121
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lemat.Api.Configuration;
using Lemat.Api.Errors;
using Microsoft.AspNetCore.WebUtilities;

namespace Lemat.Api.Pagination;

/// <summary>Kursor = base64url(ładunek JSON) + "." + base64url(HMAC). Klient przekazuje go bez interpretacji.</summary>
public sealed class KodekKursora
{
    /// <summary>Klucz tylko dla Development/E2E, gdy nie ustawiono <c>LEMAT_KURSOR_KLUCZ</c>.</summary>
    public const string KluczDeweloperski = "lemat-dev-kursor-klucz-nie-uzywac-w-produkcji";

    private readonly byte[] _klucz;

    public KodekKursora(string klucz)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(klucz);
        _klucz = Encoding.UTF8.GetBytes(klucz);
    }

    public string Zakoduj<T>(T pozycja)
    {
        var ladunek = JsonSerializer.SerializeToUtf8Bytes(pozycja, JsonApi.Opcje);
        return WebEncoders.Base64UrlEncode(ladunek) + "." + WebEncoders.Base64UrlEncode(HMACSHA256.HashData(_klucz, ladunek));
    }

    /// <summary>Odczytuje pozycję z kursora; każdy błąd (forma, podpis, JSON) to <c>400 KURSOR_NIEPRAWIDLOWY</c>.</summary>
    public T Odkoduj<T>(string kursor)
    {
        try
        {
            var czesci = kursor.Split('.');
            if (czesci.Length != 2)
            {
                throw Nieprawidlowy();
            }

            var ladunek = WebEncoders.Base64UrlDecode(czesci[0]);
            var podpis = WebEncoders.Base64UrlDecode(czesci[1]);
            if (!CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(_klucz, ladunek), podpis))
            {
                throw Nieprawidlowy();
            }

            return JsonSerializer.Deserialize<T>(ladunek, JsonApi.Opcje) ?? throw Nieprawidlowy();
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            throw Nieprawidlowy();
        }
    }

    private static BladApiException Nieprawidlowy() => new(KodBledu.KursorNieprawidlowy);
}
