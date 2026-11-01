// @Role: Agregat Użytkownik (konto każdej roli): e-mail normalizowany (citext), status konta, karencja usunięcia, przejścia 8.x (zatwierdzenie, blokada, wniosek o usunięcie, przywrócenie) | @DependsOn: Lemat.Domain.Common.Agregat, OkresKarencji, StatusKonta, RolaUzytkownika | @UsedBy: repozytoria Kont, IKontaFasada, uwierzytelnianie (TASK-072), RODO (TASK-033) | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public sealed class Uzytkownik : Agregat<UzytkownikId>
{
    public const int MaksDlugoscEmail = 254;
    public const int MaksDlugoscImie = 60;
    public const int MaksDlugoscNazwisko = 80;

    private Uzytkownik()
    {
    }

    public string Email { get; private set; } = string.Empty;

    public RolaUzytkownika Rola { get; private init; }

    public StatusKonta Status { get; private set; }

    public string Imie { get; private set; } = string.Empty;

    public string Nazwisko { get; private set; } = string.Empty;

    /// <summary>Niezmiennik: różne od <c>null</c> wtedy i tylko wtedy, gdy <see cref="Status"/> to <see cref="StatusKonta.Usuwane"/>.</summary>
    public OkresKarencji? OkresKarencji { get; private set; }

    /// <summary>Wstrzymanie procesów konta w okresie karencji (ADR-040).</summary>
    public bool CzyProcesyWstrzymane => Status == StatusKonta.Usuwane;

    [BezZmianyWersji]
    public DateTimeOffset? OstatnieLogowanieAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Tworzy konto: Nauczyciel startuje jako <see cref="StatusKonta.OczekujeNaZatwierdzenie"/>, pozostałe role jako
    /// <see cref="StatusKonta.Aktywne"/>. Hasło (argon2id) jest poza API domeny — zapisuje je repozytorium.
    /// </summary>
    public static Uzytkownik Zaloz(UzytkownikId id, RolaUzytkownika rola, string email, string imie, string nazwisko, DateTimeOffset teraz)
    {
        if (!Enum.IsDefined(rola))
        {
            throw new NiezmiennikNaruszonyException("ROLA_NIEZNANA", "Nieznana rola użytkownika.");
        }

        return new Uzytkownik
        {
            Id = id,
            Rola = rola,
            Email = NormalizujEmail(email),
            Imie = WalidujNazwe(imie, MaksDlugoscImie, "IMIE_NIEPRAWIDLOWE", "Imię ma mieć 1-60 znaków."),
            Nazwisko = WalidujNazwe(nazwisko, MaksDlugoscNazwisko, "NAZWISKO_NIEPRAWIDLOWE", "Nazwisko ma mieć 1-80 znaków."),
            Status = rola == RolaUzytkownika.Nauczyciel ? StatusKonta.OczekujeNaZatwierdzenie : StatusKonta.Aktywne,
            CreatedAt = teraz,
            UpdatedAt = teraz,
        };
    }

    /// <summary>Normalizacja zgodna z kolumną citext: obcięcie białych znaków i małe litery (kultura niezmienna).</summary>
    public static string NormalizujEmail(string? email)
    {
        var znormalizowany = (email ?? string.Empty).Trim().ToLowerInvariant();
        var at = znormalizowany.IndexOf('@', StringComparison.Ordinal);
        var poprawny = znormalizowany.Length is > 0 and <= MaksDlugoscEmail
            && at > 0
            && at == znormalizowany.LastIndexOf('@')
            && at < znormalizowany.Length - 1
            && znormalizowany.IndexOf('.', at) > at + 1
            && !znormalizowany.EndsWith('.')
            && !znormalizowany.Any(char.IsWhiteSpace);
        return poprawny
            ? znormalizowany
            : throw new NiezmiennikNaruszonyException("EMAIL_NIEPRAWIDLOWY", "Adres e-mail ma nieprawidłowy format.");
    }

    public void ZarejestrujLogowanie(DateTimeOffset teraz) => OstatnieLogowanieAt = teraz;

    /// <summary>Wniosek o usunięcie konta z dowolnego statusu poza USUWANE (C6-06); start 30-dniowej karencji.</summary>
    public void ZlozWniosekOUsuniecie(DateTimeOffset teraz)
    {
        if (Status == StatusKonta.Usuwane)
        {
            throw new NiezmiennikNaruszonyException("KONTO_JUZ_USUWANE", "Wniosek o usunięcie konta został już złożony.");
        }

        OkresKarencji = OkresKarencji.Rozpocznij(teraz, Status);
        Status = StatusKonta.Usuwane;
        Zglos(new KontoUsuwane(Id, Rola, teraz));
    }

    /// <summary>Przywrócenie w okresie karencji odtwarza status sprzed wniosku (C6-06 p. 1).</summary>
    public void Przywroc(DateTimeOffset teraz)
    {
        if (OkresKarencji is null || teraz >= OkresKarencji.UsuniecieZaplanowaneAt)
        {
            throw new NiezmiennikNaruszonyException("PRZYWROCENIE_NIEMOZLIWE", "Okres karencji minął lub nie trwa.");
        }

        Status = OkresKarencji.StatusPrzedWnioskiem;
        OkresKarencji = null;
        Zglos(new KontoPrzywrocone(Id, Rola, Status, teraz));
    }

    /// <summary>Zatwierdzenie konta Nauczyciela oczekującego (Administrator; w Development autozatwierdzenie, U-04).</summary>
    public void Zatwierdz()
    {
        if (Rola != RolaUzytkownika.Nauczyciel || Status != StatusKonta.OczekujeNaZatwierdzenie)
        {
            throw new NiezmiennikNaruszonyException("KONTO_NIEPRAWIDLOWY_STAN", "Zatwierdzenie dotyczy tylko konta Nauczyciela oczekującego na zatwierdzenie.");
        }

        Status = StatusKonta.Aktywne;
    }

    public void Zablokuj(DateTimeOffset teraz)
    {
        if (Status is StatusKonta.Usuwane or StatusKonta.Zablokowane)
        {
            throw new NiezmiennikNaruszonyException("KONTO_NIEPRAWIDLOWY_STAN", "Konto jest usuwane albo już zablokowane.");
        }

        Status = StatusKonta.Zablokowane;
        Zglos(new KontoZablokowane(Id, Rola, teraz));
    }

    public void Odblokuj()
    {
        if (Status != StatusKonta.Zablokowane)
        {
            throw new NiezmiennikNaruszonyException("KONTO_NIEPRAWIDLOWY_STAN", "Konto nie jest zablokowane.");
        }

        Status = StatusKonta.Aktywne;
    }

    private static string WalidujNazwe(string? wartosc, int maks, string kod, string opis)
    {
        var przyciete = (wartosc ?? string.Empty).Trim();
        return przyciete.Length is >= 1 && przyciete.Length <= maks
            ? przyciete
            : throw new NiezmiennikNaruszonyException(kod, opis);
    }
}
