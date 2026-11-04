// @Role: Budowniczy danych testowych modułu Treści (Zadania zamknięte/obliczeniowe/dowód, Kryteria, wzorce) | @DependsOn: Lemat.Domain.Content | @UsedBy: testy domeny Treści | @Task: TASK-017
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Tests.Content;

internal static class ContentTestData
{
    public static readonly DateTimeOffset Teraz = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    public static RozwiazanieWzorcowe Wzorzec(int wariantow = 1) => new(
        [.. Enumerable.Range(1, wariantow).Select(w => new WariantWzorca(w, $"Wariant {w}", [new KrokWzorcowy(1, "x = 2", "wynik")]))]);

    public static WynikKoncowyWzorcowy Wynik() => new(TypWynikuKoncowego.Zbior, @"x \in \{-1, 3\}", "FiniteSet(-1, 3)");

    public static OpcjeZamkniete WyborA() => OpcjeZamkniete.WyborJednokrotny(
        [new OpcjaWyboru("A", "$x=2$"), new OpcjaWyboru("B", "$x=-2$"), new OpcjaWyboru("C", "$x=4$")], "B");

    public static OpcjeZamkniete PrawdaFalszDwa() => OpcjeZamkniete.PrawdaFalsz(
        [new StwierdzeniePrawdaFalsz("1", "Funkcja jest rosnąca.", true), new StwierdzeniePrawdaFalsz("2", "Miejscem zerowym jest $3$.", false)]);

    public static KryteriumOceniania Kryterium(int wariant, int nr, int punkty, bool wynik = false, int? wymaga = null) =>
        new(new WariantRozwiazania(wariant), nr, $"Kryterium {nr}", punkty, wynik, wymaga);

    public static TrescWersji TrescZamknieta(OpcjeZamkniete? opcje = null) => new("Ile wynosi $x$?", opcje ?? WyborA(), Wzorzec(), null, 1, []);

    public static TrescWersji TrescObliczeniowa(string tresc = "Oblicz $x$.", int punkty = 2) => new(
        tresc, null, Wzorzec(), Wynik(), punkty, [Kryterium(1, 1, punkty - 1), Kryterium(1, 2, 1, wynik: true, wymaga: 1)]);

    public static TrescWersji TrescDowodu() => new("Udowodnij, że ...", null, Wzorzec(), null, 2, [Kryterium(1, 1, 1), Kryterium(1, 2, 1)]);

    public static NoweZadanie Nowe(
        RodzajZadania rodzaj = RodzajZadania.OtwarteObliczeniowe,
        PochodzenieZadania pochodzenie = PochodzenieZadania.BazoweNauczyciel,
        ZasiegZadania zasieg = ZasiegZadania.Globalny,
        StatusZadania status = StatusZadania.Opublikowane,
        bool diagnostyczne = false,
        TrescWersji? tresc = null,
        ArkuszCkeId? arkusz = null,
        ZlecenieGenerowaniaId? zlecenie = null,
        FormaZamknieta? forma = null,
        UmiejetnoscId? glowna = null,
        IReadOnlyList<UmiejetnoscId>? dodatkowe = null)
    {
        forma ??= rodzaj == RodzajZadania.Zamkniete ? FormaZamknieta.WyborJednokrotny : null;
        tresc ??= rodzaj switch
        {
            RodzajZadania.Zamkniete => TrescZamknieta(),
            RodzajZadania.OtwarteZDowodem => TrescDowodu(),
            _ => TrescObliczeniowa(),
        };
        return new NoweZadanie(
            new ZadanieId(Guid.CreateVersion7(Teraz)),
            pochodzenie,
            zasieg,
            rodzaj,
            forma,
            new DzialId(5),
            PoziomMatury.Podstawowy,
            arkusz,
            null,
            null,
            zlecenie,
            status,
            new PoziomTrudnosci(3),
            diagnostyczne,
            glowna ?? new UmiejetnoscId(501),
            dodatkowe ?? [],
            tresc);
    }

    public static Zadanie Zadanie(RodzajZadania rodzaj = RodzajZadania.OtwarteObliczeniowe, StatusZadania status = StatusZadania.Opublikowane) =>
        Utworz(Nowe(rodzaj, status: status));

    public static Zadanie Utworz(NoweZadanie dane) => Lemat.Domain.Content.Zadanie.Utworz(dane, Teraz);

    public static UzytkownikId Autor() => new(Guid.CreateVersion7(Teraz));
}
