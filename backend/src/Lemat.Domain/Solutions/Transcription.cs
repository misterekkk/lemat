// @Role: Agregat Transkrypcja (niezmienny zapis odczytu pisma w wersjach): wersja, źródło, kroki 1-40 z ciągłą numeracją, niezmienniki CHECK transkrypcja; w POC wyłącznie wersja 1 z Agenta rozpoznającego | @DependsOn: Lemat.Domain.Common.Agregat, KrokTranskrypcji, RozwiazanieId, KonfiguracjaModeluId | @UsedBy: ITranskrypcjaRepozytorium, Rozwiazanie, handler ROZPOZNAJ_PISMO (planowane TASK-128) | @Task: TASK-126
using Lemat.Domain.Common;
using Lemat.Domain.Models;

namespace Lemat.Domain.Solutions;

public sealed class Transkrypcja : Agregat<TranskrypcjaId>
{
    private readonly List<KrokTranskrypcji> _kroki = [];

    private Transkrypcja()
    {
    }

    public RozwiazanieId RozwiazanieId { get; private init; }

    /// <summary>Numer wersji (≥ 1), unikalny w Rozwiązaniu.</summary>
    public int Wersja { get; private init; }

    public ZrodloTranskrypcji Zrodlo { get; private init; }

    /// <summary>Numer Przesłania zdjęć, z których pochodzi Transkrypcja.</summary>
    public int PrzeslanieNr { get; private init; }

    /// <summary>Niepusty wyłącznie dla <see cref="ZrodloTranskrypcji.AgentRozpoznajacy"/>.</summary>
    public KonfiguracjaModeluId? KonfiguracjaModeluId { get; private init; }

    /// <summary>Sygnał Agenta: czy kartka dotyczy Zadania (0-1); dane wejściowe, nie decyzja (Z-1).</summary>
    public decimal? ZgodnoscZZadaniem { get; private init; }

    public int DaneOsoboweUsuniete { get; private init; }

    public bool KonfliktFiltra { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public IReadOnlyList<KrokTranskrypcji> Kroki => _kroki;

    /// <summary>Transkrypcja odczytana przez Agenta rozpoznającego (wersja 1 przy pierwszym rozpoznaniu).</summary>
    public static Transkrypcja ZAgenta(
        TranskrypcjaId id,
        RozwiazanieId rozwiazanie,
        int wersja,
        int przeslanieNr,
        KonfiguracjaModeluId konfiguracja,
        decimal? zgodnoscZZadaniem,
        IReadOnlyList<KrokTranskrypcji> kroki,
        DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(kroki);
        if (wersja < 1)
        {
            throw new NiezmiennikNaruszonyException("TRANSKRYPCJA_WERSJA_NIEPRAWIDLOWA", "Wersja Transkrypcji musi być większa od zera.");
        }

        if (przeslanieNr is < 1 or > ZdjecieRozwiazania.MaksPrzeslan)
        {
            throw new NiezmiennikNaruszonyException("TRANSKRYPCJA_PRZESLANIE_POZA_ZAKRESEM", "Numer przesłania musi mieścić się w zakresie 1-3.");
        }

        if (zgodnoscZZadaniem is < 0m or > 1m)
        {
            throw new NiezmiennikNaruszonyException("TRANSKRYPCJA_ZGODNOSC_POZA_ZAKRESEM", "Zgodność z Zadaniem musi mieścić się w zakresie 0-1.");
        }

        if (kroki.Count is < 1 or > KrokTranskrypcji.MaksNr)
        {
            throw new NiezmiennikNaruszonyException("TRANSKRYPCJA_LICZBA_KROKOW", "Transkrypcja ma od 1 do 40 kroków.");
        }

        for (var i = 0; i < kroki.Count; i++)
        {
            if (kroki[i].Nr != i + 1)
            {
                throw new NiezmiennikNaruszonyException("TRANSKRYPCJA_NUMERACJA", "Numeracja kroków musi być ciągła od 1.");
            }
        }

        var transkrypcja = new Transkrypcja
        {
            Id = id,
            RozwiazanieId = rozwiazanie,
            Wersja = wersja,
            Zrodlo = ZrodloTranskrypcji.AgentRozpoznajacy,
            PrzeslanieNr = przeslanieNr,
            KonfiguracjaModeluId = konfiguracja,
            ZgodnoscZZadaniem = zgodnoscZZadaniem is { } z ? Math.Round(z, 3, MidpointRounding.ToEven) : null,
            CreatedAt = teraz,
        };
        transkrypcja._kroki.AddRange(kroki);
        return transkrypcja;
    }
}
