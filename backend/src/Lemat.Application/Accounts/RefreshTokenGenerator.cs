// @Role: Generator i skrót tokenów odświeżania (256 bitów losowych, Base64Url; w bazie SHA-256) | @DependsOn: System.Security.Cryptography | @UsedBy: SessionFactory, RefreshSessionHandler, LogoutHandler, endpointy /auth/*, testy | @Task: TASK-123
using System.Security.Cryptography;
using System.Text;

namespace Lemat.Application.Accounts;

public static class GeneratorTokenowOdswiezania
{
    public const int DlugoscTokenuBajty = 32;

    /// <summary>Nowy losowy token w postaci jawnej (do ciasteczka).</summary>
    public static string Nowy() => Base64Url(RandomNumberGenerator.GetBytes(DlugoscTokenuBajty));

    public static byte[] Skrot(string tokenJawny)
    {
        ArgumentNullException.ThrowIfNull(tokenJawny);
        return SHA256.HashData(Encoding.UTF8.GetBytes(tokenJawny));
    }

    private static string Base64Url(byte[] bajty) => Convert.ToBase64String(bajty).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
