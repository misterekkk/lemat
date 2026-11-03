// @Role: Encja wewnętrzna Rozwiązania: Zdjęcie rozwiązania (fotografia kartki) z kluczem obiektu w magazynie, skrótem sha256, wymiarami i terminem usunięcia (≤ 90 dni, ADR-019) | @DependsOn: Lemat.Domain.Common.NiezmiennikNaruszonyException | @UsedBy: Rozwiazanie, konfiguracja EF, PrzeslijRozwiazanieHandler (planowane TASK-127) | @Task: TASK-126
using Lemat.Domain.Common;

namespace Lemat.Domain.Solutions;

public sealed class ZdjecieRozwiazania
{
    public const string TypMimeJpeg = "image/jpeg";

    public const int MaksRozmiarBajty = 10_485_760;

    public const int MaksLiczbaZdjec = 5;

    public const int MaksPrzeslan = 3;

    public static readonly TimeSpan MaksOkresPrzechowywania = TimeSpan.FromDays(90);

    private ZdjecieRozwiazania()
    {
    }

    public Guid Id { get; private init; }

    public int PrzeslanieNr { get; private init; }

    public int Kolejnosc { get; private init; }

    /// <summary>Klucz obiektu: <c>zdjecia/{uczen_id}/{rozwiazanie_id}/{id}.jpg</c>.</summary>
    public string KluczObiektu { get; private init; } = string.Empty;

    public string? KluczObiektuMaskowany { get; private set; }

    public string TypMime { get; private init; } = TypMimeJpeg;

    public int RozmiarBajty { get; private init; }

    public byte[] Sha256 { get; private init; } = [];

    public int Szerokosc { get; private init; }

    public int Wysokosc { get; private init; }

    public DateTimeOffset UsunPo { get; private set; }

    public DateTimeOffset? UsunietoAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static ZdjecieRozwiazania Utworz(
        Guid id,
        int przeslanieNr,
        int kolejnosc,
        string kluczObiektu,
        int rozmiarBajty,
        byte[] sha256,
        int szerokosc,
        int wysokosc,
        DateTimeOffset usunPo,
        DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(sha256);
        if (id == Guid.Empty)
        {
            throw new NiezmiennikNaruszonyException("ZDJECIE_ID_WYMAGANE", "Zdjęcie wymaga identyfikatora.");
        }

        if (przeslanieNr is < 1 or > MaksPrzeslan)
        {
            throw new NiezmiennikNaruszonyException("ZDJECIE_PRZESLANIE_POZA_ZAKRESEM", "Numer przesłania musi mieścić się w zakresie 1-3.");
        }

        if (kolejnosc is < 1 or > MaksLiczbaZdjec)
        {
            throw new NiezmiennikNaruszonyException("ZDJECIE_KOLEJNOSC_POZA_ZAKRESEM", "Kolejność zdjęcia musi mieścić się w zakresie 1-5.");
        }

        if (string.IsNullOrWhiteSpace(kluczObiektu))
        {
            throw new NiezmiennikNaruszonyException("ZDJECIE_KLUCZ_WYMAGANY", "Zdjęcie wymaga klucza obiektu.");
        }

        if (rozmiarBajty is < 1 or > MaksRozmiarBajty)
        {
            throw new NiezmiennikNaruszonyException("ZDJECIE_ZA_DUZE", "Rozmiar zdjęcia musi mieścić się w zakresie 1 bajt - 10 MB.");
        }

        if (sha256.Length != 32)
        {
            throw new NiezmiennikNaruszonyException("ZDJECIE_SKROT_NIEPRAWIDLOWY", "Skrót sha256 ma 32 bajty.");
        }

        if (szerokosc is < 1 or > short.MaxValue || wysokosc is < 1 or > short.MaxValue)
        {
            throw new NiezmiennikNaruszonyException("ZDJECIE_WYMIARY_NIEPRAWIDLOWE", "Wymiary zdjęcia muszą mieścić się w zakresie 1-32767.");
        }

        if (usunPo > teraz + MaksOkresPrzechowywania)
        {
            throw new NiezmiennikNaruszonyException("ZDJECIE_OKRES_PRZECHOWYWANIA", "Zdjęcie można przechowywać najwyżej 90 dni.");
        }

        return new ZdjecieRozwiazania
        {
            Id = id,
            PrzeslanieNr = przeslanieNr,
            Kolejnosc = kolejnosc,
            KluczObiektu = kluczObiektu,
            RozmiarBajty = rozmiarBajty,
            Sha256 = sha256,
            Szerokosc = szerokosc,
            Wysokosc = wysokosc,
            UsunPo = usunPo,
            CreatedAt = teraz,
        };
    }
}
