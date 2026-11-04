// @Role: Encja techniczna Token odświeżania (rodzina tokenów, ADR-057): skrót SHA-256, wygaśnięcie, unieważnienie, następca po rotacji i okno tolerancji 30 s | @DependsOn: TokenOdswiezaniaId, UzytkownikId, Lemat.Domain.Common.NiezmiennikNaruszonyException | @UsedBy: repozytorium tokenów, uwierzytelnianie (TASK-072, TASK-081) | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public sealed class TokenOdswiezania
{
    public const int OknoToleranciSekundy = 30;
    public const int MaksUzycWOknieTolerancji = 5;
    public const int DlugoscSkrotuBajty = 32;

    private TokenOdswiezania()
    {
    }

    public TokenOdswiezaniaId Id { get; private init; }

    public UzytkownikId UzytkownikId { get; private init; }

    /// <summary>Rodzina tokenów: wspólna dla kolejnych rotacji, służy do wykrywania ponownego użycia.</summary>
    public Guid RodzinaId { get; private init; }

    public byte[] TokenHash { get; private init; } = [];

    public DateTimeOffset WygasaAt { get; private init; }

    public DateTimeOffset? UniewaznionyAt { get; private set; }

    public TokenOdswiezaniaId? ZastapionyPrzez { get; private set; }

    /// <summary>Początek 30-sekundowego okna tolerancji po rotacji.</summary>
    public DateTimeOffset? ZastapionyAt { get; private set; }

    public byte[] SkrotKlientaHttp { get; private init; } = [];

    public int UzyciaWOknieTolerancji { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static TokenOdswiezania Wydaj(
        TokenOdswiezaniaId id,
        UzytkownikId uzytkownik,
        Guid rodzinaId,
        byte[] tokenHash,
        byte[] skrotKlientaHttp,
        DateTimeOffset teraz,
        TimeSpan waznosc)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);
        ArgumentNullException.ThrowIfNull(skrotKlientaHttp);
        if (tokenHash.Length != DlugoscSkrotuBajty || skrotKlientaHttp.Length != DlugoscSkrotuBajty)
        {
            throw new NiezmiennikNaruszonyException("TOKEN_SKROT_NIEPRAWIDLOWY", "Skrót SHA-256 ma 32 bajty.");
        }

        if (waznosc <= TimeSpan.Zero)
        {
            throw new NiezmiennikNaruszonyException("TOKEN_WAZNOSC_NIEPRAWIDLOWA", "Ważność tokenu musi być dodatnia.");
        }

        return new TokenOdswiezania
        {
            Id = id,
            UzytkownikId = uzytkownik,
            RodzinaId = rodzinaId,
            TokenHash = tokenHash,
            SkrotKlientaHttp = skrotKlientaHttp,
            WygasaAt = teraz.Add(waznosc),
            CreatedAt = teraz,
        };
    }

    public bool CzyWazny(DateTimeOffset teraz) => UniewaznionyAt is null && ZastapionyAt is null && teraz < WygasaAt;

    /// <summary>Token zastąpiony, ale jeszcze w oknie tolerancji (równoległe odświeżenia), z limitem użyć.</summary>
    public bool CzyWOknieTolerancji(DateTimeOffset teraz) =>
        UniewaznionyAt is null
        && ZastapionyAt is { } zastapiony
        && teraz < WygasaAt
        && teraz - zastapiony <= TimeSpan.FromSeconds(OknoToleranciSekundy)
        && UzyciaWOknieTolerancji < MaksUzycWOknieTolerancji;

    public void Zastap(TokenOdswiezaniaId nastepca, DateTimeOffset teraz)
    {
        if (UniewaznionyAt is not null || ZastapionyAt is not null)
        {
            throw new NiezmiennikNaruszonyException("TOKEN_NIEPRAWIDLOWY_STAN", "Token został już zastąpiony albo unieważniony.");
        }

        ZastapionyPrzez = nastepca;
        ZastapionyAt = teraz;
    }

    public void ZarejestrujUzycieWOknieTolerancji(DateTimeOffset teraz)
    {
        if (!CzyWOknieTolerancji(teraz))
        {
            throw new NiezmiennikNaruszonyException("TOKEN_POZA_OKNEM_TOLERANCJI", "Token jest poza oknem tolerancji.");
        }

        UzyciaWOknieTolerancji++;
    }

    public void Uniewaznij(DateTimeOffset teraz) => UniewaznionyAt ??= teraz;
}
