// @Role: Agregat Próg pewności (ADR-006, FR-24): wartość progu 0,800–1,000 (1,000 = Tryb pełnej weryfikacji, dla dowodu wymuszone) z źródłem i uzasadnieniem; niezmienny poza flagą aktywności, zmiana progu = nowy rekord | @DependsOn: Lemat.Domain.Common.Agregat, KonfiguracjaModeluId, KalibratorPewnosciId, RodzajZadania, ZrodloProgu | @UsedBy: IProgPewnosciRepozytorium, KalibracjaFasada, Agent oceniający (planowane TASK-022) | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Domain.Calibration;

public sealed class ProgPewnosci : Agregat<ProgPewnosciId>
{
    public const decimal MinWartosc = 0.800m;

    public const decimal MaksWartoscAutomatyczna = 0.990m;

    /// <summary>Próg 1,000 = Tryb pełnej weryfikacji: każda Ocena trafia do Weryfikacji.</summary>
    public const decimal WartoscTrybuPelnejWeryfikacji = 1.000m;

    private ProgPewnosci()
    {
    }

    public KonfiguracjaModeluId KonfiguracjaModeluId { get; private init; }

    public RodzajZadania RodzajZadania { get; private init; }

    public KalibratorPewnosciId KalibratorPewnosciId { get; private init; }

    public decimal Wartosc { get; private init; }

    /// <summary>Górna granica Wilsona 95% błędu ocen automatycznych w chwili wyboru progu.</summary>
    public decimal? GornaGranicaBleduAuto { get; private init; }

    /// <summary>Efektywna liczność próby powyżej progu.</summary>
    public decimal? NEff { get; private init; }

    public ZrodloProgu Zrodlo { get; private init; }

    public string Uzasadnienie { get; private init; } = string.Empty;

    /// <summary>≤ 1 aktywny na (Konfiguracja modelu, Rodzaj zadania).</summary>
    public bool Aktywny { get; private set; }

    public UzytkownikId? UtworzonyPrzez { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Próg 1,000: Tryb pełnej weryfikacji (3.5 #79).</summary>
    public bool TrybPelnejWeryfikacji => Wartosc == WartoscTrybuPelnejWeryfikacji;

    public static ProgPewnosci Utworz(
        ProgPewnosciId id,
        KonfiguracjaModeluId konfiguracja,
        RodzajZadania rodzaj,
        KalibratorPewnosciId kalibrator,
        decimal wartosc,
        ZrodloProgu zrodlo,
        string uzasadnienie,
        decimal? gornaGranicaBleduAuto,
        decimal? nEff,
        UzytkownikId? utworzonyPrzez,
        DateTimeOffset teraz)
    {
        if (rodzaj == RodzajZadania.Zamkniete)
        {
            throw new NiezmiennikNaruszonyException("PROG_DLA_ZAMKNIETYCH", "Zadania zamknięte nie mają Progu pewności.");
        }

        if (wartosc is < MinWartosc or > WartoscTrybuPelnejWeryfikacji || (wartosc > MaksWartoscAutomatyczna && wartosc != WartoscTrybuPelnejWeryfikacji))
        {
            throw new NiezmiennikNaruszonyException("PROG_POZA_ZAKRESEM", "Próg pewności mieści się w 0,800–0,990 albo wynosi 1,000 (Tryb pełnej weryfikacji).");
        }

        if (rodzaj == RodzajZadania.OtwarteZDowodem && wartosc != WartoscTrybuPelnejWeryfikacji)
        {
            throw new NiezmiennikNaruszonyException("PROG_DOWODU", "Dla Zadań z dowodem Próg pewności wynosi 1,000 (FR-26).");
        }

        if (string.IsNullOrWhiteSpace(uzasadnienie))
        {
            throw new NiezmiennikNaruszonyException("UZASADNIENIE_WYMAGANE", "Próg pewności wymaga uzasadnienia.");
        }

        return new ProgPewnosci
        {
            Id = id,
            KonfiguracjaModeluId = konfiguracja,
            RodzajZadania = rodzaj,
            KalibratorPewnosciId = kalibrator,
            Wartosc = decimal.Round(wartosc, 3),
            GornaGranicaBleduAuto = gornaGranicaBleduAuto,
            NEff = nEff,
            Zrodlo = zrodlo,
            Uzasadnienie = uzasadnienie.Trim(),
            UtworzonyPrzez = utworzonyPrzez,
            CreatedAt = teraz,
        };
    }

    public void Aktywuj() => Aktywny = true;

    public void Dezaktywuj() => Aktywny = false;
}
