// @Role: Agregat Ocena (niezmienny, Z-5): punkty, Kryteria, błąd, pewność, próg i kalibrator; dozwolone tylko przejście do ZASTAPIONA i jednokrotny zapis Wskazówki; niezmienniki CHECK ocena | @DependsOn: Lemat.Domain.Common.Agregat, StatusOceny, ZrodloOceny, OcenaKryterium, Wskazowka, RozwiazanieId, TranskrypcjaId | @UsedBy: IOcenaRepozytorium, GradingFacade, handler OCEN_ROZWIAZANIE (planowane TASK-128), moduł Weryfikacje | @Task: TASK-126
using Lemat.Domain.Accounts;
using Lemat.Domain.Calibration;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Models;
using Lemat.Domain.Solutions;

namespace Lemat.Domain.Grading;

public sealed class Ocena : Agregat<OcenaId>
{
    public const string KodNiezmienna = "OCENA_NIEZMIENNA";

    private readonly List<OcenaKryterium> _kryteria = [];

    private Ocena()
    {
    }

    public RozwiazanieId RozwiazanieId { get; private init; }

    public int Wersja { get; private init; }

    public StatusOceny Status { get; private set; }

    public ZrodloOceny Zrodlo { get; private init; }

    public TranskrypcjaId TranskrypcjaId { get; private init; }

    public WariantRozwiazania Wariant { get; private init; }

    /// <summary>Suma punktów Kryteriów przed regułą FR-20; niepusta wyłącznie dla Oceny automatycznej.</summary>
    public int? PunktySurowe { get; private init; }

    public int Punkty { get; private init; }

    public int PunktyMax { get; private init; }

    public int? PierwszyBlednyKrok { get; private init; }

    public bool BrakujacyKrok { get; private init; }

    public TypBledu? TypBledu { get; private init; }

    public bool MozliwyBladOdczytu { get; private init; }

    public int? KrokPodejrzanyOdczyt { get; private init; }

    public PewnoscOceny? PewnoscOceny { get; private init; }

    public PewnoscSurowa? PewnoscSurowa { get; private init; }

    public ProgPewnosciId? ProgPewnosciId { get; private init; }

    public KalibratorPewnosciId? KalibratorPewnosciId { get; private init; }

    public KonfiguracjaModeluId? KonfiguracjaModeluId { get; private init; }

    public NauczycielId? NauczycielId { get; private init; }

    public string Uzasadnienie { get; private init; } = string.Empty;

    /// <summary>Zapis jednokrotny (<c>NULL</c> → wartość), bo Wskazówka może powstać po Ocenie (ADR-025).</summary>
    public Wskazowka? Wskazowka { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public IReadOnlyList<OcenaKryterium> Kryteria => _kryteria;

    public WidocznoscOcenyDlaUcznia Widocznosc =>
        Status == StatusOceny.Wstepna && MozliwyBladOdczytu
            ? WidocznoscOcenyDlaUcznia.WstrzymanaBladOdczytu
            : WidocznoscOcenyDlaUcznia.Pelna;

    /// <summary>Ocena automatyczna (Model decyzyjny); status <c>WSTEPNA</c> albo <c>OSTATECZNA</c>.</summary>
    public static Ocena Automatyczna(OcenaId id, DaneOcenyAutomatycznej dane, DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(dane);
        if (dane.Wersja < 1)
        {
            throw new NiezmiennikNaruszonyException("OCENA_WERSJA_NIEPRAWIDLOWA", "Wersja Oceny musi być większa od zera.");
        }

        if (dane.Status == StatusOceny.Zastapiona)
        {
            throw new NiezmiennikNaruszonyException("OCENA_STATUS_NIEPRAWIDLOWY", "Nowa Ocena nie może być zastąpiona.");
        }

        if (dane.PunktyMax < 0 || dane.Punkty < 0 || dane.Punkty > dane.PunktyMax)
        {
            throw new NiezmiennikNaruszonyException("OCENA_PUNKTY_POZA_ZAKRESEM", "Punkty muszą mieścić się w zakresie 0-punkty_max.");
        }

        var bezBledu = dane.PierwszyBlednyKrok is null && !dane.BrakujacyKrok;
        if ((dane.TypBledu is null) != bezBledu)
        {
            throw new NiezmiennikNaruszonyException("OCENA_TYP_BLEDU_NIESPOJNY", "Typ błędu występuje wtedy i tylko wtedy, gdy jest błędny albo brakujący krok.");
        }

        if (dane.KrokPodejrzanyOdczyt is not null && !dane.MozliwyBladOdczytu)
        {
            throw new NiezmiennikNaruszonyException("OCENA_KROK_PODEJRZANY_NIESPOJNY", "Krok podejrzany o błąd odczytu wymaga flagi możliwego błędu odczytu.");
        }

        if (dane.Wskazowka is not null && dane.TypBledu is null)
        {
            throw new NiezmiennikNaruszonyException("WSKAZOWKA_BEZ_BLEDU", "Wskazówka wymaga typu błędu.");
        }

        if (string.IsNullOrWhiteSpace(dane.Uzasadnienie))
        {
            throw new NiezmiennikNaruszonyException("OCENA_UZASADNIENIE_WYMAGANE", "Ocena wymaga uzasadnienia.");
        }

        if (dane.Kryteria is not { Count: > 0 } kryteria)
        {
            throw new NiezmiennikNaruszonyException("OCENA_KRYTERIA_WYMAGANE", "Ocena wymaga punktów za co najmniej jedno Kryterium.");
        }

        if (kryteria.Select(k => k.KryteriumOcenianiaId).Distinct().Count() != kryteria.Count)
        {
            throw new NiezmiennikNaruszonyException("OCENA_KRYTERIA_DUPLIKAT", "Kryterium oceniania może wystąpić w Ocenie tylko raz.");
        }

        if (kryteria.Sum(k => k.Punkty) != dane.PunktySurowe)
        {
            throw new NiezmiennikNaruszonyException("OCENA_PUNKTY_SUROWE_NIESPOJNE", "Punkty surowe muszą być sumą punktów Kryteriów.");
        }

        var ocena = new Ocena
        {
            Id = id,
            RozwiazanieId = dane.RozwiazanieId,
            Wersja = dane.Wersja,
            Status = dane.Status,
            Zrodlo = ZrodloOceny.Automatyczna,
            TranskrypcjaId = dane.TranskrypcjaId,
            Wariant = dane.Wariant,
            PunktySurowe = dane.PunktySurowe,
            Punkty = dane.Punkty,
            PunktyMax = dane.PunktyMax,
            PierwszyBlednyKrok = dane.PierwszyBlednyKrok,
            BrakujacyKrok = dane.BrakujacyKrok,
            TypBledu = dane.TypBledu,
            MozliwyBladOdczytu = dane.MozliwyBladOdczytu,
            KrokPodejrzanyOdczyt = dane.KrokPodejrzanyOdczyt,
            PewnoscOceny = dane.PewnoscOceny,
            PewnoscSurowa = dane.PewnoscSurowa,
            ProgPewnosciId = dane.ProgPewnosciId,
            KalibratorPewnosciId = dane.KalibratorPewnosciId,
            KonfiguracjaModeluId = dane.KonfiguracjaModeluId,
            Uzasadnienie = dane.Uzasadnienie,
            Wskazowka = dane.Wskazowka,
            CreatedAt = teraz,
        };
        ocena._kryteria.AddRange(kryteria);
        return ocena;
    }

    /// <summary>Jednokrotne uzupełnienie Wskazówki (<c>trg_ocena_niezmienna</c>); wymaga typu błędu i Oceny niezastąpionej.</summary>
    public void UzupelnijWskazowke(Wskazowka wskazowka)
    {
        ArgumentNullException.ThrowIfNull(wskazowka);
        if (Wskazowka is not null)
        {
            throw new NiezmiennikNaruszonyException(KodNiezmienna, "Wskazówkę zapisuje się jednokrotnie.");
        }

        if (TypBledu is null)
        {
            throw new NiezmiennikNaruszonyException("WSKAZOWKA_BEZ_BLEDU", "Wskazówka wymaga typu błędu.");
        }

        if (Status == StatusOceny.Zastapiona)
        {
            throw new NiezmiennikNaruszonyException("OCENA_ZASTAPIONA", "Ocena została zastąpiona.");
        }

        Wskazowka = wskazowka;
    }

    /// <summary>WSTEPNA | OSTATECZNA → ZASTAPIONA; nowa wersja Oceny powstaje jako osobny agregat (Z-5).</summary>
    public void Zastap()
    {
        if (Status == StatusOceny.Zastapiona)
        {
            throw new NiezmiennikNaruszonyException(KodNiezmienna, "Ocena została już zastąpiona.");
        }

        Status = StatusOceny.Zastapiona;
    }
}
