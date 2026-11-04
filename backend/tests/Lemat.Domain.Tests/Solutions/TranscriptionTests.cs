// @Role: Testy agregatu Transkrypcja i Kroku: fabryka z Agenta rozpoznającego, ciągła numeracja 1-40, zakresy pewności, limity tekstu | @DependsOn: Lemat.Domain.Solutions.Transkrypcja, KrokTranskrypcji, SolutionsTestData | @UsedBy: dotnet test | @Task: TASK-126
using Lemat.Domain.Common;
using Lemat.Domain.Models;
using Lemat.Domain.Solutions;
using static Lemat.Domain.Tests.Solutions.SolutionsTestData;

namespace Lemat.Domain.Tests.Solutions;

public sealed class TranskrypcjaTests
{
    private static readonly RozwiazanieId Rozwiazanie = new(Guid.NewGuid());

    [Fact]
    public void TranskrypcjaAgentaMaWersjeJedenZrodloIKroki()
    {
        var transkrypcja = Transkrypcja(Rozwiazanie, kroki: 3);

        transkrypcja.Wersja.ShouldBe(1);
        transkrypcja.Zrodlo.ShouldBe(ZrodloTranskrypcji.AgentRozpoznajacy);
        transkrypcja.KonfiguracjaModeluId.ShouldNotBeNull();
        transkrypcja.Kroki.Count.ShouldBe(3);
        transkrypcja.Kroki.Select(k => k.Nr).ShouldBe([1, 2, 3]);
        transkrypcja.Kroki.ShouldAllBe(k => !k.ZmienionyPrzezUcznia && k.NrKrokuAgenta == k.Nr && !k.PodejrzenieDanychOsobowych && !k.TylkoDaneOsobowe);
        transkrypcja.CreatedAt.ShouldBe(Teraz);
    }

    [Fact]
    public void TranskrypcjaMozeMiecOdJednegoDoCzterdziestuKrokow()
    {
        Should.NotThrow(() => Transkrypcja(Rozwiazanie, kroki: 1));
        Should.NotThrow(() => Transkrypcja(Rozwiazanie, kroki: 40));
        Should.Throw<NiezmiennikNaruszonyException>(() => Transkrypcja(Rozwiazanie, kroki: 0)).KodBledu.ShouldBe("TRANSKRYPCJA_LICZBA_KROKOW");
        Should.Throw<NiezmiennikNaruszonyException>(() => Transkrypcja(Rozwiazanie, kroki: 41)).KodBledu.ShouldBe("KROK_NR_POZA_ZAKRESEM");
    }

    [Fact]
    public void NumeracjaKrokowMusiBycCiagla()
    {
        var wyjatek = Should.Throw<NiezmiennikNaruszonyException>(
            () => Solutions_Transkrypcja(Rozwiazanie, [Krok(1), Krok(3)]));

        wyjatek.KodBledu.ShouldBe("TRANSKRYPCJA_NUMERACJA");
    }

    [Fact]
    public void WersjaIPrzeslanieMuszaBycWZakresie()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => Lemat.Domain.Solutions.Transkrypcja.ZAgenta(
                new TranskrypcjaId(Guid.NewGuid()), Rozwiazanie, 0, 1, new KonfiguracjaModeluId(Guid.NewGuid()), null, [Krok(1)], Teraz))
            .KodBledu.ShouldBe("TRANSKRYPCJA_WERSJA_NIEPRAWIDLOWA");
        Should.Throw<NiezmiennikNaruszonyException>(() => Lemat.Domain.Solutions.Transkrypcja.ZAgenta(
                new TranskrypcjaId(Guid.NewGuid()), Rozwiazanie, 1, 4, new KonfiguracjaModeluId(Guid.NewGuid()), null, [Krok(1)], Teraz))
            .KodBledu.ShouldBe("TRANSKRYPCJA_PRZESLANIE_POZA_ZAKRESEM");
        Should.Throw<NiezmiennikNaruszonyException>(() => Lemat.Domain.Solutions.Transkrypcja.ZAgenta(
                new TranskrypcjaId(Guid.NewGuid()), Rozwiazanie, 1, 1, new KonfiguracjaModeluId(Guid.NewGuid()), 1.2m, [Krok(1)], Teraz))
            .KodBledu.ShouldBe("TRANSKRYPCJA_ZGODNOSC_POZA_ZAKRESEM");
    }

    [Fact]
    public void KrokOdrzucaPewnoscPozaZakresemITekstPonadLimit()
    {
        Should.Throw<NiezmiennikNaruszonyException>(() => KrokTranskrypcji.OdczytanyPrzezAgenta(1, "t", "l", 1.001m))
            .KodBledu.ShouldBe("KROK_PEWNOSC_POZA_ZAKRESEM");
        Should.Throw<NiezmiennikNaruszonyException>(() => KrokTranskrypcji.OdczytanyPrzezAgenta(1, new string('a', 2001), "l", 0.5m))
            .KodBledu.ShouldBe("KROK_ZA_DLUGI");
        Should.Throw<NiezmiennikNaruszonyException>(() => KrokTranskrypcji.OdczytanyPrzezAgenta(1, "t", new string('a', 2001), 0.5m))
            .KodBledu.ShouldBe("KROK_ZA_DLUGI");
        Should.NotThrow(() => KrokTranskrypcji.OdczytanyPrzezAgenta(1, new string('a', 2000), new string('b', 2000), 0m));
    }

    [Fact]
    public void PewnoscOdczytuJestZaokraglanaDoTrzechMiejsc()
    {
        var krok = KrokTranskrypcji.OdczytanyPrzezAgenta(1, "t", "l", 0.9506m);

        krok.PewnoscOdczytu.ShouldBe(0.951m);
    }
}
