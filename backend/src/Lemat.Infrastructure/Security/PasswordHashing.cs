// @Role: Haszowanie haseł argon2id (m = 64 MiB, t = 3, p = 4) w formacie PHC; implementacja portu IHasherHasel | @DependsOn: Konscious.Security.Cryptography.Argon2, IHasherHasel | @UsedBy: PocSeeder, uwierzytelnianie (planowane TASK-123) | @Task: TASK-122
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Lemat.Application.Accounts;

namespace Lemat.Infrastructure.Security;

/// <summary>Skrót w formacie <c>$argon2id$v=19$m=65536,t=3,p=4$sól$skrót</c> (Base64 bez dopełnienia).</summary>
public sealed class HaszowanieHasel(int pamiecKiB = HaszowanieHasel.DomyslnaPamiecKiB, int iteracje = HaszowanieHasel.DomyslneIteracje, int rownoleglosc = HaszowanieHasel.DomyslnaRownoleglosc) : IHasherHasel
{
    public const int DomyslnaPamiecKiB = 65536;
    public const int DomyslneIteracje = 3;
    public const int DomyslnaRownoleglosc = 4;

    private const int DlugoscSoli = 16;
    private const int DlugoscSkrotu = 32;
    private const int WersjaArgon2 = 19;

    public string Haszuj(string haslo)
    {
        ArgumentException.ThrowIfNullOrEmpty(haslo);
        var sol = RandomNumberGenerator.GetBytes(DlugoscSoli);
        var skrot = Oblicz(haslo, sol, pamiecKiB, iteracje, rownoleglosc, DlugoscSkrotu);
        return $"$argon2id$v={WersjaArgon2}$m={pamiecKiB},t={iteracje},p={rownoleglosc}${Base64(sol)}${Base64(skrot)}";
    }

    public bool Weryfikuj(string haslo, string skrot)
    {
        if (string.IsNullOrEmpty(haslo) || string.IsNullOrEmpty(skrot))
        {
            return false;
        }

        var czesci = skrot.Split('$');
        if (czesci.Length != 6 || czesci[0].Length != 0 || czesci[1] != "argon2id" || czesci[2] != $"v={WersjaArgon2}")
        {
            return false;
        }

        var parametry = czesci[3].Split(',');
        if (parametry.Length != 3
            || !TryParametr(parametry[0], "m=", out var pamiec)
            || !TryParametr(parametry[1], "t=", out var iter)
            || !TryParametr(parametry[2], "p=", out var rownolegle))
        {
            return false;
        }

        try
        {
            var sol = Convert.FromBase64String(Dopelnij(czesci[4]));
            var oczekiwany = Convert.FromBase64String(Dopelnij(czesci[5]));
            var obliczony = Oblicz(haslo, sol, pamiec, iter, rownolegle, oczekiwany.Length);
            return CryptographicOperations.FixedTimeEquals(obliczony, oczekiwany);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Oblicz(string haslo, byte[] sol, int pamiecKiB, int iteracje, int rownoleglosc, int dlugosc)
    {
        using var argon = new Argon2id(Encoding.UTF8.GetBytes(haslo))
        {
            Salt = sol,
            MemorySize = pamiecKiB,
            Iterations = iteracje,
            DegreeOfParallelism = rownoleglosc,
        };
        return argon.GetBytes(dlugosc);
    }

    private static bool TryParametr(string tekst, string przedrostek, out int wartosc)
    {
        wartosc = 0;
        return tekst.StartsWith(przedrostek, StringComparison.Ordinal)
            && int.TryParse(tekst.AsSpan(przedrostek.Length), out wartosc)
            && wartosc is > 0 and <= 1_048_576;
    }

    private static string Base64(byte[] bajty) => Convert.ToBase64String(bajty).TrimEnd('=');

    private static string Dopelnij(string base64) => base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
}
