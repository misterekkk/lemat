// @Role: Pomocnik testów bazy: tworzy i zapisuje opublikowane Zadanie (otwarte obliczeniowe lub zamknięte WYBOR_JEDNOKROTNY) z Umiejętnością główną i dodatkowymi w Dziale 5 | @DependsOn: BazaTestowa, Zadanie, ZadanieRepozytoriumEf, JednostkaPracyEf | @UsedBy: ProfilePersistenceTests, RecommendationsPersistenceTests | @Task: TASK-019
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Infrastructure.Persistence;
using Lemat.Infrastructure.Persistence.Repositories.Content;

namespace Lemat.Infrastructure.Tests.Persistence.Support;

internal static class ZadaniaTestowe
{
    internal static RozwiazanieWzorcowe Wzorzec() => new(
    [
        new WariantWzorca(1, "Wyznaczenie wierzchołka", [new KrokWzorcowy(1, @"f(x) = -2x^2 + 8x - 3", "postać ogólna")]),
    ]);

    internal static TrescWersji Obliczeniowa() => new(
        "Wyznacz wartość największą funkcji $f(x) = -2x^2 + 8x - 3$.",
        null,
        Wzorzec(),
        new WynikKoncowyWzorcowy(TypWynikuKoncowego.Zbior, @"x \in \{-1, 3\}", "FiniteSet(-1, 3)"),
        2,
        [
            new KryteriumOceniania(new WariantRozwiazania(1), 1, "Wierzchołek", 1, false, null),
            new KryteriumOceniania(new WariantRozwiazania(1), 2, "Wynik końcowy", 1, true, 1),
        ]);

    internal static TrescWersji Zamknieta() => new(
        "Wskaż poprawną odpowiedź.",
        OpcjeZamkniete.WyborJednokrotny(
            [new OpcjaWyboru("A", "$x = 2$"), new OpcjaWyboru("B", "$x = -2$"), new OpcjaWyboru("C", "$x = 4$"), new OpcjaWyboru("D", "$x = 0$")], "B"),
        Wzorzec(),
        null,
        1,
        []);

    internal static async Task<Zadanie> UtworzAsync(
        BazaTestowa baza,
        RodzajZadania rodzaj,
        int poziom,
        UmiejetnoscId glowna,
        IReadOnlyList<UmiejetnoscId> dodatkowe,
        CancellationToken ct)
    {
        var dane = new NoweZadanie(
            new ZadanieId(GeneratorUuid.Nowy(baza.Zegar)),
            PochodzenieZadania.BazoweNauczyciel,
            ZasiegZadania.Globalny,
            rodzaj,
            rodzaj == RodzajZadania.Zamkniete ? FormaZamknieta.WyborJednokrotny : null,
            new DzialId(5),
            PoziomMatury.Podstawowy,
            null,
            null,
            null,
            null,
            StatusZadania.Opublikowane,
            new PoziomTrudnosci(poziom),
            rodzaj == RodzajZadania.Zamkniete,
            glowna,
            dodatkowe,
            rodzaj == RodzajZadania.Zamkniete ? Zamknieta() : Obliczeniowa());
        var zadanie = Zadanie.Utworz(dane, baza.Zegar.GetUtcNow());
        await using var kontekst = baza.Kontekst();
        await new JednostkaPracyEf(kontekst).WykonajAsync(
            _ =>
            {
                new ZadanieRepozytoriumEf(kontekst).Dodaj(zadanie);
                return Task.CompletedTask;
            },
            ct);
        return zadanie;
    }
}
