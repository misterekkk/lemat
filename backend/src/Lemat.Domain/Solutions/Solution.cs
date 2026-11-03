// @Role: Agregat Rozwiązanie (8.1, podzbiór POC 7.11): Zdjęcia, kontekst próby, przejścia PRZESLANE→ROZPOZNAWANIE→TRANSKRYPCJA_GOTOWA|BLAD_ROZPOZNAWANIA, ZATWIERDZONE→OCENIANIE→OCENIONE, znaczniki czasu i czas systemowy (NFR-01) | @DependsOn: Lemat.Domain.Common.Agregat, StatusRozwiazania, ZdjecieRozwiazania, TranskrypcjaId, UczenId, ZadanieWersjaId, ElementSciezkiId | @UsedBy: IRozwiazanieRepozytorium, SolutionsFacade, handlery prac w tle (planowane TASK-127, TASK-128) | @Task: TASK-126
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;

namespace Lemat.Domain.Solutions;

public sealed class Rozwiazanie : Agregat<RozwiazanieId>
{
    public const string KodNieprawidlowyStan = "ROZWIAZANIE_NIEPRAWIDLOWY_STAN";

    public const string KodTranskrypcjaNieaktualna = "TRANSKRYPCJA_NIEAKTUALNA";

    private readonly List<ZdjecieRozwiazania> _zdjecia = [];

    private Rozwiazanie()
    {
    }

    public UczenId UczenId { get; private init; }

    public ZadanieId ZadanieId { get; private init; }

    public ZadanieWersjaId ZadanieWersjaId { get; private init; }

    public ElementSciezkiId? ElementSciezkiId { get; private init; }

    /// <summary>Identyfikator Przypisania (moduł Klasy, TASK-034).</summary>
    public Guid? PrzypisanieId { get; private init; }

    public StatusRozwiazania Status { get; private set; }

    public int LiczbaPrzeslan { get; private set; } = 1;

    public int LiczbaPonowienRozpoznania { get; private set; }

    public KodBleduRozpoznawania? KodBleduRozpoznawania { get; private set; }

    public TranskrypcjaId? NajnowszaTranskrypcjaId { get; private set; }

    public int? NajnowszaWersjaTranskrypcji { get; private set; }

    public TranskrypcjaId? ZatwierdzonaTranskrypcjaId { get; private set; }

    /// <summary>Ostatnie Przesłanie nowych zdjęć (A-11).</summary>
    public DateTimeOffset PrzeslanoAt { get; private set; }

    public DateTimeOffset OstatnieRozpoznanieOdAt { get; private set; }

    public DateTimeOffset? TranskrypcjaGotowaAt { get; private set; }

    public DateTimeOffset? ZatwierdzonoAt { get; private set; }

    public DateTimeOffset? OcenaWystawionaAt { get; private set; }

    /// <summary>NFR-01: (transkrypcja gotowa − Przesłanie) + (Ocena − zatwierdzenie).</summary>
    public TimeSpan? CzasSystemowy { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ZdjecieRozwiazania> Zdjecia => _zdjecia;

    public bool CzyTerminalne => Status is StatusRozwiazania.Ocenione or StatusRozwiazania.Porzucone or StatusRozwiazania.Anulowane;

    /// <summary>Nowe Rozwiązanie w stanie PRZESLANE; kontekst próby wymaga elementu Ścieżki nauki albo Przypisania (ADR-032).</summary>
    public static Rozwiazanie Przeslij(
        RozwiazanieId id,
        UczenId uczen,
        ZadanieId zadanie,
        ZadanieWersjaId wersja,
        RodzajZadania rodzaj,
        ElementSciezkiId? element,
        Guid? przypisanie,
        IReadOnlyList<ZdjecieRozwiazania> zdjecia,
        DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(zdjecia);
        if (rodzaj == RodzajZadania.Zamkniete)
        {
            throw new NiezmiennikNaruszonyException("ZADANIE_ZAMKNIETE_BEZ_ROZWIAZANIA", "Zadanie zamknięte rozwiązuje się Odpowiedzią.");
        }

        if (element is null && przypisanie is null)
        {
            throw new NiezmiennikNaruszonyException("KONTEKST_PROBY_WYMAGANY", "Wymagany element Ścieżki nauki albo Przypisanie.");
        }

        if (zdjecia.Count is < 1 or > ZdjecieRozwiazania.MaksLiczbaZdjec || zdjecia.Any(z => z.PrzeslanieNr != 1))
        {
            throw new NiezmiennikNaruszonyException("PRZESLANIE_NIEPRAWIDLOWE", "Przesłanie: 1-5 zdjęć, numer 1.");
        }

        var rozwiazanie = new Rozwiazanie
        {
            Id = id,
            UczenId = uczen,
            ZadanieId = zadanie,
            ZadanieWersjaId = wersja,
            ElementSciezkiId = element,
            PrzypisanieId = przypisanie,
            Status = StatusRozwiazania.Przeslane,
            PrzeslanoAt = teraz,
            OstatnieRozpoznanieOdAt = teraz,
            CreatedAt = teraz,
            UpdatedAt = teraz,
        };
        rozwiazanie._zdjecia.AddRange(zdjecia);
        return rozwiazanie;
    }

    /// <summary>PRZESLANE → ROZPOZNAWANIE; idempotentne przy ponowieniu pracy.</summary>
    public void RozpocznijRozpoznawanie()
    {
        if (Status == StatusRozwiazania.Rozpoznawanie)
        {
            return;
        }

        WymagajStanu(StatusRozwiazania.Przeslane);
        Status = StatusRozwiazania.Rozpoznawanie;
    }

    /// <summary>ROZPOZNAWANIE → TRANSKRYPCJA_GOTOWA; zapisuje najnowszą Transkrypcję i pierwszy znacznik gotowości.</summary>
    public void ZapiszTranskrypcjeAgenta(TranskrypcjaId transkrypcja, int wersja, DateTimeOffset teraz)
    {
        WymagajStanu(StatusRozwiazania.Rozpoznawanie);
        if (wersja < 1)
        {
            throw new NiezmiennikNaruszonyException("TRANSKRYPCJA_WERSJA_NIEPRAWIDLOWA", "Wersja Transkrypcji musi być większa od zera.");
        }

        NajnowszaTranskrypcjaId = transkrypcja;
        NajnowszaWersjaTranskrypcji = wersja;
        TranskrypcjaGotowaAt ??= teraz;
        Status = StatusRozwiazania.TranskrypcjaGotowa;
    }

    /// <summary>PRZESLANE | ROZPOZNAWANIE → BLAD_ROZPOZNAWANIA (stan końcowy w POC).</summary>
    public void ZapiszBladRozpoznawania(KodBleduRozpoznawania kod)
    {
        WymagajStanu(StatusRozwiazania.Przeslane, StatusRozwiazania.Rozpoznawanie);
        KodBleduRozpoznawania = kod;
        Status = StatusRozwiazania.BladRozpoznawania;
    }

    /// <summary>TRANSKRYPCJA_GOTOWA → ZATWIERDZONE; zatwierdzana musi być najnowsza wersja Transkrypcji (409 TRANSKRYPCJA_NIEAKTUALNA).</summary>
    public void Zatwierdz(TranskrypcjaId transkrypcja, int wersjaTranskrypcji, DateTimeOffset teraz)
    {
        WymagajStanu(StatusRozwiazania.TranskrypcjaGotowa);
        if (NajnowszaTranskrypcjaId != transkrypcja || NajnowszaWersjaTranskrypcji != wersjaTranskrypcji)
        {
            throw new NiezmiennikNaruszonyException(KodTranskrypcjaNieaktualna, "Zatwierdzana wersja Transkrypcji nie jest najnowsza.");
        }

        ZatwierdzonaTranskrypcjaId = transkrypcja;
        ZatwierdzonoAt = teraz;
        Status = StatusRozwiazania.Zatwierdzone;
    }

    /// <summary>ZATWIERDZONE → OCENIANIE; idempotentne przy ponowieniu pracy.</summary>
    public void RozpocznijOcenianie()
    {
        if (Status == StatusRozwiazania.Ocenianie)
        {
            return;
        }

        WymagajStanu(StatusRozwiazania.Zatwierdzone);
        Status = StatusRozwiazania.Ocenianie;
    }

    /// <summary>OCENIANIE → OCENIONE (trasa automatyczna); ustawia znacznik Oceny i czas systemowy (NFR-01).</summary>
    public void ZarejestrujOcene(DateTimeOffset teraz)
    {
        WymagajStanu(StatusRozwiazania.Ocenianie);
        OcenaWystawionaAt ??= teraz;
        CzasSystemowy ??= (TranskrypcjaGotowaAt!.Value - PrzeslanoAt) + (teraz - ZatwierdzonoAt!.Value);
        Status = StatusRozwiazania.Ocenione;
    }

    private void WymagajStanu(params StatusRozwiazania[] dozwolone)
    {
        if (!dozwolone.Contains(Status))
        {
            throw new NiezmiennikNaruszonyException(KodNieprawidlowyStan, $"Operacja niedozwolona w stanie {Status}.");
        }
    }
}
