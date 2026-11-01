// @Role: Fasada modułu Rekomendacje (6.2): Ścieżka nauki, propozycja Elementu z Rekomendacją w jednej transakcji, przejścia stanów 8.5 (rozpoczęcie, ukończenie, pominięcie, przeczytanie, wygaszenie), Seria; bez zależności od Orchestration | @DependsOn: Lemat.Domain.Recommendations, SciezkaSkrot, NowaRekomendacja | @UsedBy: Agent rekomendujący (TASK-040), Rozwiązania (TASK-020), Orkiestracja (WYGAS_ELEMENTY), Api (planowane TASK-073) | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;

namespace Lemat.Application.Recommendations;

public interface IRekomendacjeFasada
{
    /// <summary>Zakłada Ścieżkę nauki Ucznia (rejestracja, 9.1); idempotentne.</summary>
    Task<SciezkaSkrot> ZalozSciezkeAsync(UczenId uczen, CancellationToken ct);

    Task<SciezkaSkrot?> PobierzSciezkeAsync(UczenId uczen, CancellationToken ct);

    /// <summary>Aktualny element PROPONOWANY z Rekomendacją albo <c>null</c>.</summary>
    Task<NastepnyElementSkrot?> PobierzNastepnyAsync(UczenId uczen, CancellationToken ct);

    /// <summary>10 ostatnich (domyślnie) elementów Ścieżki, najnowsze pierwsze.</summary>
    Task<IReadOnlyList<ElementSkrot>> OstatnieElementyAsync(UczenId uczen, int liczba, CancellationToken ct);

    /// <summary>
    /// Dodaje element PROPONOWANY wraz z niezmienną Rekomendacją; drugi PROPONOWANY kończy się <c>ELEMENT_JUZ_PROPONOWANY</c>.
    /// Zakłada Ścieżkę, gdy jej brak.
    /// </summary>
    Task<NastepnyElementSkrot> ZaproponujAsync(UczenId uczen, PropozycjaElementu propozycja, NowaRekomendacja rekomendacja, CancellationToken ct);

    /// <summary>POST rozpoczecie (idempotentne): PROPONOWANY -> ROZPOCZETY; Zadanie wymaga wersji pokazanej Uczniowi.</summary>
    Task<ElementSkrot> RozpocznijAsync(UczenId uczen, ElementSciezkiId element, ZadanieWersjaId? wersja, CancellationToken ct);

    /// <summary>ROZPOCZETY -> UKONCZONY (Wynik zadania zarejestrowany); PROPONOWANY z Zadaniem zamkniętym: rozpoczęcie i ukończenie razem.</summary>
    Task<ElementSkrot> UkonczAsync(UczenId uczen, ElementSciezkiId element, ZadanieWersjaId? wersja, CancellationToken ct);

    /// <summary>POST przeczytanie Wkładki (idempotentne).</summary>
    Task<ElementSkrot> PrzeczytajAsync(UczenId uczen, ElementSciezkiId element, CancellationToken ct);

    /// <summary>Uczeń prosi o kolejny element: otwarta Wkładka &gt;= 60 s jest ukończona, krócej pominięta.</summary>
    Task<ElementSkrot> ZakonczWkladkePoProsbieOKolejnyAsync(UczenId uczen, ElementSciezkiId element, CancellationToken ct);

    /// <summary>POST pominiecie: PROPONOWANY albo ROZPOCZETY -> POMINIETY.</summary>
    Task<ElementSkrot> PominAsync(UczenId uczen, ElementSciezkiId element, CancellationToken ct);

    /// <summary>PROPONOWANY -> WYGASLY po <c>wygasa_at</c> albo przy wycofaniu Zadania.</summary>
    Task<ElementSkrot> WygasAsync(UczenId uczen, ElementSciezkiId element, bool wycofanieZadania, CancellationToken ct);

    Task RozpocznijSerieAsync(UczenId uczen, UmiejetnoscId umiejetnosc, PoziomTrudnosci poziom, CancellationToken ct);

    Task ZarejestrujWynikSeriiAsync(UczenId uczen, bool sukces, PoziomTrudnosci? nowyPoziom, CancellationToken ct);

    Task ZakonczSerieAsync(UczenId uczen, CancellationToken ct);
}
