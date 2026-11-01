// @Role: Encja techniczna Próba logowania (bigint identity): e-mail, adres IP, wynik; podstawa limitów logowania (ADR-070) | @DependsOn: System.Net.IPAddress | @UsedBy: repozytorium prób logowania, uwierzytelnianie (TASK-072) | @Task: TASK-016
using System.Net;
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public sealed class ProbaLogowania
{
    private ProbaLogowania()
    {
    }

    public long Id { get; private init; }

    public string Email { get; private init; } = string.Empty;

    public IPAddress Ip { get; private init; } = IPAddress.None;

    public bool Udana { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static ProbaLogowania Zapisz(string email, IPAddress ip, bool udana, DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(ip);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new NiezmiennikNaruszonyException("EMAIL_NIEPRAWIDLOWY", "Próba logowania wymaga adresu e-mail.");
        }

        return new ProbaLogowania { Email = email.Trim().ToLowerInvariant(), Ip = ip, Udana = udana, CreatedAt = teraz };
    }
}
