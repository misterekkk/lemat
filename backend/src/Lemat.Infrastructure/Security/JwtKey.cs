// @Role: Klucz podpisu JWT ES256: z LEMAT_JWT_KLUCZ (PEM) albo — poza Production — generowany i zapisywany lokalnie poza repozytorium (profil użytkownika), z awaryjnym kluczem w pamięci | @DependsOn: Microsoft.IdentityModel.Tokens, JwtOpcje | @UsedBy: WystawcaTokenow, uwierzytelnianie JWT Bearer (ApiHost), testy | @Task: TASK-123
using System.Security.Cryptography;
using Lemat.Application.Common;
using Microsoft.IdentityModel.Tokens;

namespace Lemat.Infrastructure.Security;

public sealed class KluczJwt
{
    public const string NazwaPlikuDev = "jwt-dev-es256.pem";

    private KluczJwt(ECDsa ecdsa)
    {
        Klucz = new ECDsaSecurityKey(ecdsa) { KeyId = IdentyfikatorKlucza(ecdsa) };
    }

    public ECDsaSecurityKey Klucz { get; }

    public static string KatalogDomyslnyDev { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
        "lemat");

    /// <summary>
    /// Ładuje klucz: PEM z konfiguracji (znaki <c>\n</c> w zmiennej środowiskowej są zamieniane na nowe linie) albo, gdy go brak i środowisko
    /// nie jest Production, klucz deweloperski z <paramref name="katalogDev"/> (tworzony przy pierwszym użyciu). W Production brak klucza to błąd.
    /// </summary>
    public static KluczJwt Zaladuj(JwtOpcje opcje, string srodowisko, string? katalogDev = null)
    {
        ArgumentNullException.ThrowIfNull(opcje);
        if (!string.IsNullOrWhiteSpace(opcje.Klucz))
        {
            return new KluczJwt(ZPem(opcje.Klucz));
        }

        if (string.Equals(srodowisko, "Production", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Production wymaga Lemat:Jwt:Klucz (LEMAT_JWT_KLUCZ).");
        }

        return new KluczJwt(ZPlikuDev(Path.Combine(katalogDev ?? KatalogDomyslnyDev, NazwaPlikuDev)));
    }

    private static ECDsa ZPem(string pem)
    {
        var ecdsa = ECDsa.Create();
        try
        {
            ecdsa.ImportFromPem(pem.Replace("\\n", "\n", StringComparison.Ordinal));
        }
        catch (Exception e) when (e is ArgumentException or CryptographicException)
        {
            ecdsa.Dispose();
            throw new InvalidOperationException("Lemat:Jwt:Klucz nie jest poprawnym kluczem prywatnym EC (PEM).", e);
        }

        if (ecdsa.KeySize != 256)
        {
            ecdsa.Dispose();
            throw new InvalidOperationException("Lemat:Jwt:Klucz musi być kluczem EC P-256 (ES256).");
        }

        return ecdsa;
    }

    private static ECDsa ZPlikuDev(string sciezka)
    {
        try
        {
            if (File.Exists(sciezka))
            {
                return ZPem(File.ReadAllText(sciezka));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(sciezka)!);
            var nowy = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            try
            {
                var opcjeStrumienia = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
                if (!OperatingSystem.IsWindows())
                {
                    opcjeStrumienia.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                }

                using var strumien = new FileStream(sciezka, opcjeStrumienia);
                using var pisarz = new StreamWriter(strumien);
                pisarz.Write(nowy.ExportECPrivateKeyPem());
            }
            catch (IOException) when (File.Exists(sciezka))
            {
                // Równoległy start innego procesu zdążył zapisać klucz — używamy jego klucza.
                nowy.Dispose();
                return ZPem(File.ReadAllText(sciezka));
            }

            return nowy;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // Brak możliwości zapisu (np. katalog tylko do odczytu): klucz w pamięci, sesje przestają działać po restarcie.
            return ECDsa.Create(ECCurve.NamedCurves.nistP256);
        }
    }

    private static string IdentyfikatorKlucza(ECDsa ecdsa) =>
        Convert.ToHexString(SHA256.HashData(ecdsa.ExportSubjectPublicKeyInfo()))[..16].ToLowerInvariant();
}
