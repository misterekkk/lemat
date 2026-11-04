// @Role: Agregat Nauczyciel: flagi WeryfikatorGlobalny i RecenzentTresci, deklarowane minuty Weryfikacji, umowa poufności (A-13, A-14) | @DependsOn: Lemat.Domain.Common.Agregat, UzytkownikId, StatusKonta, UprawnienieOdebrane | @UsedBy: repozytoria Kont, IKontaFasada, moduły Klasy, Weryfikacje, Treści | @Task: TASK-016
using Lemat.Domain.Common;

namespace Lemat.Domain.Accounts;

public sealed class Nauczyciel : Agregat<NauczycielId>
{
    public const int MinMinutWeryfikatoraGlobalnego = 90;
    public const int MaksMinutWeryfikacji = 480;
    public const int MaksLaczneObciazenieMinut = 180;

    private Nauczyciel()
    {
    }

    public bool WeryfikatorGlobalny { get; private set; }

    public bool RecenzentTresci { get; private set; }

    /// <summary>0-480; dla Weryfikatora globalnego co najmniej 90 (A-13).</summary>
    public int DeklarowaneMinutyWeryfikacji { get; private set; }

    /// <summary>Niezmiennik: różne od <c>null</c>, gdy <see cref="WeryfikatorGlobalny"/> (CHECK 7.3).</summary>
    public DateTimeOffset? UmowaPoufnosciAt { get; private set; }

    public static Nauczyciel Utworz(Uzytkownik konto)
    {
        ArgumentNullException.ThrowIfNull(konto);
        return konto.Rola == RolaUzytkownika.Nauczyciel
            ? new Nauczyciel { Id = NauczycielId.Z(konto.Id) }
            : throw new NiezmiennikNaruszonyException("KONTO_NIEPRAWIDLOWA_ROLA", "Nauczyciel wymaga konta o roli NAUCZYCIEL.");
    }

    public void NadajWeryfikatoraGlobalnego(
        int deklarowaneMinuty,
        StatusKonta statusKonta,
        bool prowadziKlase,
        int prognozaKolejkiKlasyMinuty,
        DateTimeOffset? umowaPoufnosciAt)
    {
        if (statusKonta != StatusKonta.Aktywne)
        {
            throw new NiezmiennikNaruszonyException("KONTO_NIEAKTYWNE", "Uprawnienie tylko dla konta AKTYWNE.");
        }

        if (umowaPoufnosciAt is null)
        {
            throw new NiezmiennikNaruszonyException("UMOWA_POUFNOSCI_WYMAGANA", "Weryfikator globalny wymaga umowy poufności.");
        }

        if (deklarowaneMinuty is < MinMinutWeryfikatoraGlobalnego or > MaksMinutWeryfikacji)
        {
            throw new NiezmiennikNaruszonyException("DEKLAROWANY_CZAS_ZA_KROTKI", "Weryfikator globalny deklaruje 90-480 min.");
        }

        if (prowadziKlase && prognozaKolejkiKlasyMinuty + deklarowaneMinuty > MaksLaczneObciazenieMinut)
        {
            throw new NiezmiennikNaruszonyException("LIMIT_OBCIAZENIA_WERYFIKATORA", "Łączne obciążenie przekracza 180 min.");
        }

        WeryfikatorGlobalny = true;
        DeklarowaneMinutyWeryfikacji = deklarowaneMinuty;
        UmowaPoufnosciAt = umowaPoufnosciAt;
    }

    public void OdbierzWeryfikatoraGlobalnego(DateTimeOffset teraz)
    {
        if (!WeryfikatorGlobalny)
        {
            return;
        }

        WeryfikatorGlobalny = false;
        DeklarowaneMinutyWeryfikacji = 0;
        Zglos(new UprawnienieOdebrane(Id, UprawnienieNauczyciela.WeryfikatorGlobalny, teraz));
    }

    public void NadajRecenzentaTresci(StatusKonta statusKonta)
    {
        if (statusKonta != StatusKonta.Aktywne)
        {
            throw new NiezmiennikNaruszonyException("KONTO_NIEAKTYWNE", "Uprawnienie tylko dla konta AKTYWNE.");
        }

        RecenzentTresci = true;
    }

    public void OdbierzRecenzentaTresci() => RecenzentTresci = false;
}
