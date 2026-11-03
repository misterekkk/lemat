// @Role: Dane testowe domeny Rozwiązań: fabryki Zdjęcia, Rozwiązania w zadanym stanie, kroków i Transkrypcji agenta oraz ustawianie stanu przez refleksję dla stanów nieosiągalnych w POC | @DependsOn: Lemat.Domain.Solutions, UczenId, ZadanieId | @UsedBy: Lemat.Domain.Tests.Solutions, Lemat.Domain.Tests.Grading | @Task: TASK-126
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Models;
using Lemat.Domain.Recommendations;
using Lemat.Domain.Solutions;

namespace Lemat.Domain.Tests.Solutions;

internal static class SolutionsTestData
{
    public static readonly DateTimeOffset Teraz = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    public static ZdjecieRozwiazania Zdjecie(int kolejnosc = 1, int przeslanie = 1, DateTimeOffset? usunPo = null) =>
        ZdjecieRozwiazania.Utworz(
            Guid.NewGuid(),
            przeslanie,
            kolejnosc,
            $"zdjecia/u/r/{Guid.NewGuid()}.jpg",
            2048,
            new byte[32],
            1200,
            1600,
            usunPo ?? Teraz.AddDays(90),
            Teraz);

    public static Rozwiazanie Nowe(IReadOnlyList<ZdjecieRozwiazania>? zdjecia = null) =>
        Rozwiazanie.Przeslij(
            new RozwiazanieId(Guid.NewGuid()),
            new UczenId(Guid.NewGuid()),
            new ZadanieId(Guid.NewGuid()),
            new ZadanieWersjaId(Guid.NewGuid()),
            RodzajZadania.OtwarteObliczeniowe,
            new ElementSciezkiId(Guid.NewGuid()),
            null,
            zdjecia ?? [Zdjecie()],
            Teraz);

    public static KrokTranskrypcji Krok(int nr, decimal pewnosc = 0.95m) =>
        KrokTranskrypcji.OdczytanyPrzezAgenta(nr, $"Krok {nr}", $"x_{nr}=1", pewnosc);

    public static Transkrypcja Transkrypcja(RozwiazanieId rozwiazanie, int kroki = 3) =>
        Solutions_Transkrypcja(rozwiazanie, [.. Enumerable.Range(1, kroki).Select(n => Krok(n))]);

    public static Transkrypcja Solutions_Transkrypcja(RozwiazanieId rozwiazanie, IReadOnlyList<KrokTranskrypcji> kroki) =>
        Lemat.Domain.Solutions.Transkrypcja.ZAgenta(
            new TranskrypcjaId(Guid.NewGuid()),
            rozwiazanie,
            1,
            1,
            new KonfiguracjaModeluId(Guid.NewGuid()),
            0.95m,
            kroki,
            Teraz);

    /// <summary>Doprowadza Rozwiązanie do stanu docelowego legalną ścieżką POC; zwraca Transkrypcję (gdy powstała).</summary>
    public static (Rozwiazanie Rozwiazanie, Transkrypcja? Transkrypcja) WStanie(StatusRozwiazania status)
    {
        var r = Nowe();
        if (status == StatusRozwiazania.Przeslane)
        {
            return (r, null);
        }

        r.RozpocznijRozpoznawanie();
        if (status == StatusRozwiazania.Rozpoznawanie)
        {
            return (r, null);
        }

        if (status == StatusRozwiazania.BladRozpoznawania)
        {
            r.ZapiszBladRozpoznawania(KodBleduRozpoznawania.Nieczytelne);
            return (r, null);
        }

        var t = Transkrypcja(r.Id);
        r.ZapiszTranskrypcjeAgenta(t.Id, t.Wersja, Teraz.AddSeconds(3));
        if (status == StatusRozwiazania.TranskrypcjaGotowa)
        {
            return (r, t);
        }

        r.Zatwierdz(t.Id, t.Wersja, Teraz.AddSeconds(10));
        if (status == StatusRozwiazania.Zatwierdzone)
        {
            return (r, t);
        }

        r.RozpocznijOcenianie();
        if (status == StatusRozwiazania.Ocenianie)
        {
            return (r, t);
        }

        r.ZarejestrujOcene(Teraz.AddSeconds(12));
        if (status == StatusRozwiazania.Ocenione)
        {
            return (r, t);
        }

        // OCZEKUJE_NA_WERYFIKACJE, PORZUCONE, ANULOWANE: poza zakresem POC — stan ustawiany bezpośrednio.
        UstawStatus(r, status);
        return (r, t);
    }

    public static void UstawStatus(Rozwiazanie rozwiazanie, StatusRozwiazania status) =>
        typeof(Rozwiazanie).GetProperty(nameof(Rozwiazanie.Status))!.SetValue(rozwiazanie, status);
}
