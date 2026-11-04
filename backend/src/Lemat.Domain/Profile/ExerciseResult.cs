// @Role: Agregat Wynik zadania (8.6, 10.1.3): ułamek punktów, waga (1,00/0,50/0,25), flaga kalibracji, zastosowanie w Profilu, unieważnienie i zawieszenie (FR-28) | @DependsOn: Lemat.Domain.Common.Agregat, UczenId, ZadanieId, ZadanieWersjaId, ZrodloWyniku | @UsedBy: ProfilUcznia, IWynikZadaniaRepozytorium, ProfilFasada | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Profile;

public sealed class WynikZadania : Agregat<WynikZadaniaId>
{
    public const decimal WagaPelna = 1.00m;

    public const decimal WagaOcenyWstepnej = 0.50m;

    public const decimal WagaKolejnejProby = 0.50m;

    private WynikZadania()
    {
    }

    public UczenId UczenId { get; private init; }

    public ZadanieId ZadanieId { get; private init; }

    public ZadanieWersjaId ZadanieWersjaId { get; private init; }

    public ZrodloWyniku Zrodlo { get; private init; }

    /// <summary>Identyfikator Odpowiedzi (moduł Rozwiązania, TASK-020); niepusty wyłącznie dla <see cref="ZrodloWyniku.Odpowiedz"/>.</summary>
    public Guid? OdpowiedzId { get; private init; }

    /// <summary>Identyfikator Oceny (moduł Ocenianie, TASK-021); niepusty wyłącznie dla <see cref="ZrodloWyniku.Ocena"/>.</summary>
    public Guid? OcenaId { get; private init; }

    public TestDiagnostycznyId? TestDiagnostycznyId { get; private init; }

    /// <summary>Punkty / punkty_max, 0-1.</summary>
    public decimal Ulamek { get; private init; }

    public decimal Waga { get; private init; }

    public bool LiczyDoTrudnosci { get; private init; }

    public DateTimeOffset? UniewaznionyAt { get; private set; }

    public PowodUniewaznieniaWyniku? PowodUniewaznienia { get; private set; }

    public DateTimeOffset? ZastosowanyWProfiluAt { get; private set; }

    public DateTimeOffset? UniewaznieniePrzetworzoneAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Wagi dozwolone przez CHECK <c>wynik_zadania.waga</c>: <c>w_status · w_proba</c>.</summary>
    public static decimal ObliczWage(bool ocenaWstepna, bool pierwszaProba) =>
        (ocenaWstepna ? WagaOcenyWstepnej : WagaPelna) * (pierwszaProba ? WagaPelna : WagaKolejnejProby);

    /// <summary>Wynik z Odpowiedzi na Zadanie zamknięte (waga statusu 1,0).</summary>
    public static WynikZadania ZOdpowiedzi(
        WynikZadaniaId id,
        UczenId uczen,
        ZadanieId zadanie,
        ZadanieWersjaId wersja,
        Guid odpowiedzId,
        decimal ulamek,
        bool pierwszaProba,
        DateTimeOffset teraz) =>
        Utworz(id, uczen, zadanie, wersja, ZrodloWyniku.Odpowiedz, odpowiedzId, null, null, ulamek, ObliczWage(false, pierwszaProba), teraz);

    /// <summary>
    /// Wynik z Oceny ostatecznej albo wstępnej (waga statusu 0,5). Ocena wstępna z <paramref name="mozliwyBladOdczytu"/> nie tworzy
    /// Wyniku zadania (FR-28, ADR-022) — zwracane jest <c>null</c>.
    /// </summary>
    public static WynikZadania? ZOceny(
        WynikZadaniaId id,
        UczenId uczen,
        ZadanieId zadanie,
        ZadanieWersjaId wersja,
        Guid ocenaId,
        decimal ulamek,
        bool ocenaWstepna,
        bool mozliwyBladOdczytu,
        bool pierwszaProba,
        DateTimeOffset teraz)
    {
        if (ocenaWstepna && mozliwyBladOdczytu)
        {
            return null;
        }

        return Utworz(id, uczen, zadanie, wersja, ZrodloWyniku.Ocena, null, ocenaId, null, ulamek, ObliczWage(ocenaWstepna, pierwszaProba), teraz);
    }

    /// <summary>Wynik z Testu diagnostycznego: zawsze waga 1,00; <c>created_at</c> = zakończenie testu.</summary>
    public static WynikZadania ZTestuDiagnostycznego(
        WynikZadaniaId id,
        UczenId uczen,
        ZadanieId zadanie,
        ZadanieWersjaId wersja,
        Guid odpowiedzId,
        TestDiagnostycznyId test,
        decimal ulamek,
        DateTimeOffset zakonczonoTestAt) =>
        Utworz(id, uczen, zadanie, wersja, ZrodloWyniku.Odpowiedz, odpowiedzId, null, test, ulamek, WagaPelna, zakonczonoTestAt);

    /// <summary>Unieważnienie (np. Ocena zastąpiona); idempotentne — pierwszy powód zostaje.</summary>
    public void Uniewaznij(PowodUniewaznieniaWyniku powod, DateTimeOffset teraz)
    {
        if (powod == PowodUniewaznieniaWyniku.ZgloszenieBleduOdczytu)
        {
            Zawies(teraz);
            return;
        }

        UniewaznionyAt ??= teraz;
        PowodUniewaznienia ??= powod;
    }

    /// <summary>Zawieszenie Wyniku z Oceny objętej Zgłoszeniem błędu odczytu (FR-28, ADR-022).</summary>
    public void Zawies(DateTimeOffset teraz)
    {
        if (Zrodlo != ZrodloWyniku.Ocena)
        {
            throw new NiezmiennikNaruszonyException("ZAWIESZENIE_NIEDOZWOLONE", "Zawiesić można wyłącznie Wynik zadania pochodzący z Oceny.");
        }

        UniewaznionyAt ??= teraz;
        PowodUniewaznienia ??= PowodUniewaznieniaWyniku.ZgloszenieBleduOdczytu;
    }

    /// <summary>Wynik zastosowany w Profilu (AKTUALIZUJ_PROFIL); idempotentne.</summary>
    public void OznaczZastosowany(DateTimeOffset teraz) => ZastosowanyWProfiluAt ??= teraz;

    /// <summary>Unieważnienie uwzględnione w odtworzonym Profilu.</summary>
    public void OznaczUniewaznieniePrzetworzone(DateTimeOffset teraz)
    {
        if (UniewaznionyAt is null)
        {
            throw new NiezmiennikNaruszonyException("WYNIK_NIEUNIEWAZNIONY", "Wynik zadania nie jest unieważniony.");
        }

        UniewaznieniePrzetworzoneAt ??= teraz;
    }

    private static WynikZadania Utworz(
        WynikZadaniaId id,
        UczenId uczen,
        ZadanieId zadanie,
        ZadanieWersjaId wersja,
        ZrodloWyniku zrodlo,
        Guid? odpowiedzId,
        Guid? ocenaId,
        TestDiagnostycznyId? test,
        decimal ulamek,
        decimal waga,
        DateTimeOffset teraz)
    {
        if (ulamek is < 0m or > 1m)
        {
            throw new NiezmiennikNaruszonyException("ULAMEK_POZA_ZAKRESEM", "Ułamek punktów musi mieścić się w zakresie 0-1.");
        }

        return new WynikZadania
        {
            Id = id,
            UczenId = uczen,
            ZadanieId = zadanie,
            ZadanieWersjaId = wersja,
            Zrodlo = zrodlo,
            OdpowiedzId = odpowiedzId,
            OcenaId = ocenaId,
            TestDiagnostycznyId = test,
            Ulamek = ModelElo.Zaokraglij(ulamek),
            Waga = waga,
            LiczyDoTrudnosci = waga == WagaPelna,
            CreatedAt = teraz,
        };
    }
}
