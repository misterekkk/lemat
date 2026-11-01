// @Role: Wartość Punkty kalibratora (11.2.2): monotoniczna funkcja odcinkami liniowa pewność surowa → Pewność oceny; interpolacja liniowa, poza zakresem przycięcie do skrajnych punktów | @DependsOn: NiezmiennikNaruszonyException | @UsedBy: KalibratorPewnosci, konwerter jsonb kalibrator_pewnosci.punkty | @Task: TASK-018
using Lemat.Domain.Common;

namespace Lemat.Domain.Calibration;

public readonly record struct PunktKalibracji(decimal Surowa, decimal Skalibrowana);

public sealed class PunktyKalibratora
{
    /// <summary>Wartość pola <c>typ</c> w jsonb dla kalibratora tożsamościowego.</summary>
    public const string TypTozsamosciowy = "TOZSAMOSCIOWY";

    public const string TypIzotoniczny = "IZOTONICZNY";

    private PunktyKalibratora(string typ, IReadOnlyList<PunktKalibracji> punkty)
    {
        Typ = typ;
        Punkty = punkty;
    }

    public string Typ { get; }

    public IReadOnlyList<PunktKalibracji> Punkty { get; }

    /// <summary>Funkcja tożsamościowa <c>[[0,0],[1,1]]</c> (MVP, przed ewaluacją ZT-1).</summary>
    public static PunktyKalibratora Tozsamosciowe { get; } = new(TypTozsamosciowy, [new(0m, 0m), new(1m, 1m)]);

    /// <summary>
    /// Waliduje punkty: ≥ 2, pewność surowa rosnąco (ściśle), wartości w [0,1], skalibrowane niemalejące (monotoniczność, ADR-006).
    /// Kod błędu <c>PUNKTY_KALIBRATORA_NIEPRAWIDLOWE</c>.
    /// </summary>
    public static PunktyKalibratora Utworz(string typ, IReadOnlyList<PunktKalibracji> punkty)
    {
        ArgumentNullException.ThrowIfNull(punkty);
        if (typ is not (TypTozsamosciowy or TypIzotoniczny))
        {
            throw Blad($"Nieznany typ funkcji kalibratora '{typ}'.");
        }

        if (punkty.Count < 2)
        {
            throw Blad("Funkcja kalibratora wymaga co najmniej dwóch punktów.");
        }

        for (var i = 0; i < punkty.Count; i++)
        {
            var p = punkty[i];
            if (p.Surowa is < 0m or > 1m || p.Skalibrowana is < 0m or > 1m)
            {
                throw Blad("Wartości punktów kalibratora muszą mieścić się w przedziale 0–1.");
            }

            if (i > 0 && (p.Surowa <= punkty[i - 1].Surowa || p.Skalibrowana < punkty[i - 1].Skalibrowana))
            {
                throw Blad("Punkty kalibratora muszą mieć rosnącą pewność surową i niemalejącą skalibrowaną.");
            }
        }

        if (typ == TypTozsamosciowy && !(punkty.Count == 2 && punkty[0] == new PunktKalibracji(0m, 0m) && punkty[1] == new PunktKalibracji(1m, 1m)))
        {
            throw Blad("Kalibrator tożsamościowy ma punkty [[0,0],[1,1]].");
        }

        return new PunktyKalibratora(typ, [.. punkty]);
    }

    /// <summary>Interpolacja liniowa; wejście spoza zakresu punktów jest przycinane do skrajnych punktów. Wynik zaokrąglony do 4 miejsc.</summary>
    public decimal Kalibruj(decimal surowa)
    {
        if (surowa <= Punkty[0].Surowa)
        {
            return Zaokraglij(Punkty[0].Skalibrowana);
        }

        var ostatni = Punkty[^1];
        if (surowa >= ostatni.Surowa)
        {
            return Zaokraglij(ostatni.Skalibrowana);
        }

        for (var i = 1; i < Punkty.Count; i++)
        {
            var prawy = Punkty[i];
            if (surowa <= prawy.Surowa)
            {
                var lewy = Punkty[i - 1];
                var udzial = (surowa - lewy.Surowa) / (prawy.Surowa - lewy.Surowa);
                return Zaokraglij(lewy.Skalibrowana + (udzial * (prawy.Skalibrowana - lewy.Skalibrowana)));
            }
        }

        return Zaokraglij(ostatni.Skalibrowana);
    }

    private static decimal Zaokraglij(decimal wartosc) => Math.Round(wartosc, 4, MidpointRounding.ToEven);

    private static NiezmiennikNaruszonyException Blad(string opis) => new("PUNKTY_KALIBRATORA_NIEPRAWIDLOWE", opis);
}
