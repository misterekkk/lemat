// @Role: Agregat Kalibrator pewności (ADR-006/007, niezmienny): funkcja pewność surowa → Pewność oceny dla pary (Konfiguracja modelu, Rodzaj zadania); MVP: kalibrator tożsamościowy | @DependsOn: Lemat.Domain.Common.Agregat, PunktyKalibratora, MetodaKalibratora, KonfiguracjaModeluId, RodzajZadania | @UsedBy: IKalibratorRepozytorium, KalibracjaFasada, Agent oceniający (planowane TASK-022), KALIBRUJ (planowane TASK-032) | @Task: TASK-018
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Domain.Calibration;

public sealed class KalibratorPewnosci : Agregat<KalibratorPewnosciId>
{
    private KalibratorPewnosci()
    {
    }

    public KonfiguracjaModeluId KonfiguracjaModeluId { get; private init; }

    public RodzajZadania RodzajZadania { get; private init; }

    public MetodaKalibratora Metoda { get; private init; }

    public PunktyKalibratora Punkty { get; private init; } = PunktyKalibratora.Tozsamosciowe;

    public int LiczbaProbek { get; private init; }

    /// <summary>Expected Calibration Error (out-of-fold, 10 koszyków); <c>null</c> dla kalibratora tożsamościowego.</summary>
    public decimal? Ece { get; private init; }

    public DateTimeOffset? OknoOd { get; private init; }

    public DateTimeOffset? OknoDo { get; private init; }

    public ZrodloKalibratora Zrodlo { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Kalibrator tożsamościowy (MVP, U-10): <c>Kalibruj(x) == x</c> (po przycięciu do 0–1). Dla Zadań zamkniętych nie istnieje
    /// (kod <c>KALIBRATOR_DLA_ZAMKNIETYCH</c>).
    /// </summary>
    public static KalibratorPewnosci Tozsamosciowy(
        KalibratorPewnosciId id,
        KonfiguracjaModeluId konfiguracja,
        RodzajZadania rodzaj,
        DateTimeOffset teraz,
        ZrodloKalibratora zrodlo = ZrodloKalibratora.KalibracjaAutomatyczna)
    {
        WymagajRodzajuOtwartego(rodzaj);
        return new KalibratorPewnosci
        {
            Id = id,
            KonfiguracjaModeluId = konfiguracja,
            RodzajZadania = rodzaj,
            Metoda = MetodaKalibratora.Tozsamosciowy,
            Punkty = PunktyKalibratora.Tozsamosciowe,
            LiczbaProbek = 0,
            Zrodlo = zrodlo,
            CreatedAt = teraz,
        };
    }

    /// <summary>Kalibrator izotoniczny z ewaluacji ZT-1 albo przebiegu KALIBRUJ (POST-MVP, TASK-032).</summary>
    public static KalibratorPewnosci Izotoniczny(
        KalibratorPewnosciId id,
        KonfiguracjaModeluId konfiguracja,
        RodzajZadania rodzaj,
        PunktyKalibratora punkty,
        int liczbaProbek,
        decimal? ece,
        DateTimeOffset? oknoOd,
        DateTimeOffset? oknoDo,
        ZrodloKalibratora zrodlo,
        DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(punkty);
        WymagajRodzajuOtwartego(rodzaj);
        if (punkty.Typ != PunktyKalibratora.TypIzotoniczny)
        {
            throw new NiezmiennikNaruszonyException("PUNKTY_KALIBRATORA_NIEPRAWIDLOWE", "Kalibrator izotoniczny wymaga funkcji typu IZOTONICZNY.");
        }

        if (liczbaProbek < 0 || ece is < 0m or > 1m || (oknoOd > oknoDo))
        {
            throw new NiezmiennikNaruszonyException("KALIBRATOR_NIEPRAWIDLOWY", "Liczba próbek, ECE i okno muszą być prawidłowe.");
        }

        return new KalibratorPewnosci
        {
            Id = id,
            KonfiguracjaModeluId = konfiguracja,
            RodzajZadania = rodzaj,
            Metoda = MetodaKalibratora.IzotonicznyCv5,
            Punkty = punkty,
            LiczbaProbek = liczbaProbek,
            Ece = ece,
            OknoOd = oknoOd,
            OknoDo = oknoDo,
            Zrodlo = zrodlo,
            CreatedAt = teraz,
        };
    }

    /// <summary>Pewność oceny dla pewności surowej modelu (przycięta do przedziału 0–1).</summary>
    public decimal Kalibruj(decimal pewnoscSurowa) => Punkty.Kalibruj(Math.Clamp(pewnoscSurowa, 0m, 1m));

    private static void WymagajRodzajuOtwartego(RodzajZadania rodzaj)
    {
        if (rodzaj == RodzajZadania.Zamkniete)
        {
            throw new NiezmiennikNaruszonyException("KALIBRATOR_DLA_ZAMKNIETYCH", "Zadania zamknięte nie mają kalibratora pewności.");
        }
    }
}
