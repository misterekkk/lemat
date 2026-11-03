// @Role: Fasada modułu Treści (jedyne wejście z innych modułów): migawki Zadania do oceny i dla Ucznia, kandydaci doboru, Działy, Umiejętności, opublikowana Wkładka | @DependsOn: Lemat.Domain.Content, ZadanieDoOceny, ZadanieUcznia, ZadanieDoDoboru | @UsedBy: moduły Ocenianie, Rozwiązania, Rekomendacje (IDaneDoboru), Profil, Klasy, Orkiestracja, Api | @Task: TASK-017, TASK-125
using Lemat.Domain.Content;

namespace Lemat.Application.Content;

public interface ITresciFasada
{
    /// <summary>Kryteria oceniania i wzorce wskazanej wersji (także zastąpionej, bo Rozwiązanie wskazuje wersję, którą widział Uczeń); <c>null</c>, gdy wersji brak.</summary>
    Task<ZadanieDoOceny?> PobierzZadanieDoOcenyAsync(ZadanieWersjaId wersja, CancellationToken ct);

    /// <summary>Aktualna wersja Zadania w widoku Ucznia (bez wzorców i klucza); <c>null</c>, gdy Zadania brak.</summary>
    Task<ZadanieUcznia?> PobierzZadanieUczniaAsync(ZadanieId zadanie, CancellationToken ct);

    /// <summary>
    /// Opublikowane Zadania globalne w widoku Ucznia, porządek (Poziom trudności, id) rosnąco, strona po <paramref name="po"/> (wyłącznie).
    /// </summary>
    Task<StronaZadanUcznia> ListaZadanUczniaAsync(DzialId? dzial, PozycjaListyZadan? po, int limit, CancellationToken ct);

    /// <summary>Kandydaci doboru (10.1.2 krok 7) bez filtrów zależnych od Ucznia (Wyniki z 60 dni, Przypisania) — te stosuje wywołujący.</summary>
    Task<IReadOnlyList<ZadanieDoDoboru>> PobierzKandydatowDoboruAsync(FiltrKandydatow filtr, CancellationToken ct);

    Task<IReadOnlyList<DzialSkrot>> ListaDzialowAsync(CancellationToken ct);

    /// <summary>Umiejętności Działu albo wszystkie (<paramref name="dzial"/> = <c>null</c>).</summary>
    Task<IReadOnlyList<UmiejetnoscSkrot>> ListaUmiejetnosciAsync(DzialId? dzial, CancellationToken ct);

    Task<WkladkaDoDoboru?> PobierzOpublikowanaWkladkeAsync(UmiejetnoscId umiejetnosc, CancellationToken ct);
}
