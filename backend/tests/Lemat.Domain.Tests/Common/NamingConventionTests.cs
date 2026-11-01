// @Role: Testy KonwencjiNazw: round-trip PascalCase, snake_case, UPPER_SNAKE, kebab-case (ADR-049) | @DependsOn: Lemat.Domain.Common.KonwencjaNazw | @UsedBy: dotnet test | @Task: TASK-014
using Lemat.Domain.Common;

namespace Lemat.Domain.Tests.Common;

public sealed class KonwencjaNazwTests
{
    public static TheoryData<string, string> Nazwy => new()
    {
        { "Wstepna", "wstepna" },
        { "Ostateczna", "ostateczna" },
        { "Zastapiona", "zastapiona" },
        { "OtwarteZDowodem", "otwarte_z_dowodem" },
        { "OtwarteObliczeniowe", "otwarte_obliczeniowe" },
        { "Zamkniete", "zamkniete" },
        { "ZgloszenieBleduOdczytu", "zgloszenie_bledu_odczytu" },
        { "WstrzymanieWDoborze", "wstrzymanie_w_doborze" },
        { "NiepelneRozwiazanie", "niepelne_rozwiazanie" },
        { "EwaluacjaZt1", "ewaluacja_zt1" },
        { "JevChoice", "jev_choice" },
        { "ArkuszCke", "arkusz_cke" },
        { "ZdarzenieRodo", "zdarzenie_rodo" },
        { "JevAdapter", "jev_adapter" },
        { "RozpoznajPismo", "rozpoznaj_pismo" },
        { "OcenRozwiazanie", "ocen_rozwiazanie" },
        { "AktualizujProfil", "aktualizuj_profil" },
        { "KonfiguracjaModelu", "konfiguracja_modelu" },
        { "PoziomMatury", "poziom_matury" },
        { "RodzajZadania", "rodzaj_zadania" },
        { "PoziomTrudnosci", "poziom_trudnosci" },
        { "PewnoscOceny", "pewnosc_oceny" },
        { "ProbkaKalibracyjna", "probka_kalibracyjna" },
        { "StanBezpiecznika", "stan_bezpiecznika" },
        { "WkladkaTeoretyczna", "wkladka_teoretyczna" },
        { "ElementSciezki", "element_sciezki" },
        { "ZadanieWersja", "zadanie_wersja" },
        { "KrokTranskrypcji", "krok_transkrypcji" },
        { "DecyzjaModelu", "decyzja_modelu" },
        { "OczekujeNaZatwierdzenie", "oczekuje_na_zatwierdzenie" },
        { "WstrzymanaBladOdczytu", "wstrzymana_blad_odczytu" },
        { "Sha256", "sha256" },
        { "Cv5", "cv5" },
        { "TrudnoscB", "trudnosc_b" },
    };

    [Theory]
    [MemberData(nameof(Nazwy))]
    public void PascalSnakeUpperKebab_RoundTrip(string pascal, string snake)
    {
        KonwencjaNazw.PascalNaSnake(pascal).ShouldBe(snake);
        KonwencjaNazw.SnakeNaPascal(snake).ShouldBe(pascal);

        var upper = snake.ToUpperInvariant();
        KonwencjaNazw.PascalNaUpperSnake(pascal).ShouldBe(upper);
        KonwencjaNazw.UpperSnakeNaPascal(upper).ShouldBe(pascal);
        KonwencjaNazw.SnakeNaUpperSnake(snake).ShouldBe(upper);
        KonwencjaNazw.UpperSnakeNaSnake(upper).ShouldBe(snake);

        var kebab = snake.Replace('_', '-');
        KonwencjaNazw.PascalNaKebab(pascal).ShouldBe(kebab);
        KonwencjaNazw.KebabNaPascal(kebab).ShouldBe(pascal);
        KonwencjaNazw.SnakeNaKebab(snake).ShouldBe(kebab);
        KonwencjaNazw.KebabNaSnake(kebab).ShouldBe(snake);
    }

    [Fact]
    public void ZestawNazwMaCoNajmniej30Pozycji() => Nazwy.Count.ShouldBeGreaterThanOrEqualTo(30);

    [Fact]
    public void PustaNazwaJestOdrzucana()
    {
        Should.Throw<ArgumentException>(() => KonwencjaNazw.PascalNaSnake(""));
        Should.Throw<ArgumentNullException>(() => KonwencjaNazw.SnakeNaPascal(null!));
    }
}
