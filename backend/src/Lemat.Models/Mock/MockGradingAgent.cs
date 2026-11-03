// @Role: MockAgentOceniajacy (ADR-065, U-26): deterministyczna Ocena z danych POC 7.3 zależna od Zadania; prawdopodobieństwo = Lemat:Modele:MockMasaModeluDecyzyjnego (0.995), uzasadnienie składane szablonem z kontraktu | @DependsOn: IAgentOceniajacy, MockDane, LematOpcje | @UsedBy: OcenRozwiazanieHandler (przez DI) | @Task: TASK-128
using System.Globalization;
using Lemat.Application.Common;
using Lemat.Application.Models;
using Lemat.Domain.Grading;

namespace Lemat.Models.Mock;

public sealed class MockAgentOceniajacy(MockDane dane, LematOpcje opcje) : IAgentOceniajacy
{
    public Task<WynikOcenyAgenta> OcenAsync(WejscieOceny wejscie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(wejscie);
        var mock = dane.Pobierz(wejscie.Zadanie).Ocena;
        if (mock.PunktyKryteriow.Count != wejscie.Kryteria.Count)
        {
            throw new InvalidOperationException("Liczba Kryteriów Zadania nie zgadza się z danymi Mocka.");
        }

        var pewnosc = (decimal)opcje.Modele.MockMasaModeluDecyzyjnego;
        IReadOnlyList<PunktyKryterium> kryteria =
            [.. wejscie.Kryteria.Select((k, i) => new PunktyKryterium(k.Nr, mock.PunktyKryteriow[i], pewnosc))];
        var typ = mock.TypBleduWartosc;
        return Task.FromResult(new WynikOcenyAgenta(
            kryteria,
            mock.PierwszyBlednyKrok,
            typ,
            mock.Wskazowka,
            Uzasadnienie(wejscie.Kryteria, kryteria, mock.PierwszyBlednyKrok, typ),
            pewnosc));
    }

    // Kryteria: K1 „{opis}” — {p}/{max} pkt; K2 „…” — … pkt. [Pierwszy błędny krok: {n} ({rodzaj}). ]Razem {punkty}/{max} pkt.
    private static string Uzasadnienie(IReadOnlyList<KryteriumDoOceny> kryteria, IReadOnlyList<PunktyKryterium> punkty, int? blednyKrok, TypBledu? typ)
    {
        var czesci = kryteria.Select((k, i) => string.Create(
            CultureInfo.InvariantCulture,
            $"K{k.Nr} „{k.Opis}” — {punkty[i].Punkty}/{k.PunktyMax} pkt"));
        var blad = typ is { } t
            ? string.Create(CultureInfo.InvariantCulture, $"Pierwszy błędny krok: {blednyKrok} ({Opis(t)}). ")
            : string.Empty;
        var suma = punkty.Sum(p => p.Punkty);
        var max = kryteria.Sum(k => k.PunktyMax);
        return string.Create(CultureInfo.InvariantCulture, $"Kryteria: {string.Join("; ", czesci)}. {blad}Razem {suma}/{max} pkt.");
    }

    private static string Opis(TypBledu typ) => typ switch
    {
        TypBledu.Rachunkowy => "błąd rachunkowy",
        TypBledu.Merytoryczny => "błąd merytoryczny",
        _ => "niepełne rozwiązanie",
    };
}
