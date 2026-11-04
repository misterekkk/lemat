// @Role: Implementacja IRekomendacjeFasada nad repozytoriami i IJednostkaPracy: propozycja Elementu z Rekomendacją w jednej transakcji, przejścia stanów 8.5, Seria; uprawnienia obiektowe przez Ścieżkę Ucznia (ADR-071) | @DependsOn: ISciezkaNaukiRepozytorium, IRekomendacjaRepozytorium, IJednostkaPracy, TimeProvider, Lemat.Domain.Recommendations | @UsedBy: DI (RecommendationsRegistration), moduły przez IRekomendacjeFasada | @Task: TASK-019, TASK-127
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Recommendations;

namespace Lemat.Application.Recommendations;

public sealed class RekomendacjeFasada(
    ISciezkaNaukiRepozytorium sciezki,
    IRekomendacjaRepozytorium rekomendacje,
    IJednostkaPracy jednostkaPracy,
    TimeProvider czas) : IRekomendacjeFasada
{
    public Task<SciezkaSkrot> ZalozSciezkeAsync(UczenId uczen, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(async token => Skrot(await PobierzLubZalozAsync(uczen, null, token)), ct);

    public async Task<SciezkaSkrot?> PobierzSciezkeAsync(UczenId uczen, CancellationToken ct)
    {
        var sciezka = await sciezki.PobierzDoOdczytuAsync(uczen, ct);
        return sciezka is null ? null : Skrot(sciezka);
    }

    public async Task<NastepnyElementSkrot?> PobierzNastepnyAsync(UczenId uczen, CancellationToken ct)
    {
        var sciezka = await sciezki.PobierzDoOdczytuAsync(uczen, ct);
        if (sciezka?.Proponowany is not { } element)
        {
            return null;
        }

        var rekomendacja = await rekomendacje.PobierzPoElemencieAsync(element.Id, ct);
        return new NastepnyElementSkrot(Skrot(element), rekomendacja is null ? null : Skrot(rekomendacja));
    }

    public async Task<IReadOnlyList<ElementSkrot>> OstatnieElementyAsync(UczenId uczen, int liczba, CancellationToken ct) =>
        [.. (await sciezki.OstatnieElementyAsync(uczen, liczba, ct)).Select(Skrot)];

    public Task<NastepnyElementSkrot> ZaproponujAsync(UczenId uczen, PropozycjaElementu propozycja, NowaRekomendacja rekomendacja, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(propozycja);
        ArgumentNullException.ThrowIfNull(rekomendacja);
        return jednostkaPracy.WykonajAsync(
            async token =>
            {
                var teraz = czas.GetUtcNow();
                var sciezka = await PobierzLubZalozAsync(uczen, null, token);
                var element = sciezka.Zaproponuj(propozycja, teraz);
                var nowa = Rekomendacja.Utworz(
                    new RekomendacjaId(GeneratorUuid.Nowy(czas)),
                    uczen,
                    element.Id,
                    rekomendacja.Strategia,
                    rekomendacja.Regula,
                    rekomendacja.CechyJson,
                    rekomendacja.DecyzjaJson,
                    rekomendacja.PrzewidywanePSukcesu,
                    rekomendacja.Uzasadnienie,
                    rekomendacja.UzasadnienieFaktyJson,
                    teraz);
                rekomendacje.Dodaj(nowa);
                return new NastepnyElementSkrot(Skrot(element), Skrot(nowa));
            },
            ct);
    }

    public Task<ElementSkrot> RozpocznijProbeAsync(UczenId uczen, ZadanieId zadanie, UmiejetnoscId umiejetnosc, ZadanieWersjaId wersja, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var teraz = czas.GetUtcNow();
                var sciezka = await PobierzLubZalozAsync(uczen, null, token);
                var element = sciezka.Zaproponuj(
                    new PropozycjaElementu(TypElementuSciezki.Zadanie, CelElementu.Nauka, umiejetnosc, zadanie, null, null, false, null),
                    teraz);
                element.Rozpocznij(wersja, teraz);
                return Skrot(element);
            },
            ct);

    public Task<ElementSkrot> RozpocznijAsync(UczenId uczen, ElementSciezkiId element, ZadanieWersjaId? wersja, CancellationToken ct) =>
        ZmienElementAsync(
            uczen,
            element,
            (e, teraz) =>
            {
                if (e.Status != StatusElementuSciezki.Rozpoczety)
                {
                    e.Rozpocznij(wersja, teraz);
                }
            },
            ct);

    public Task<ElementSkrot> UkonczAsync(UczenId uczen, ElementSciezkiId element, ZadanieWersjaId? wersja, CancellationToken ct) =>
        ZmienElementAsync(
            uczen,
            element,
            (e, teraz) =>
            {
                if (e.Status == StatusElementuSciezki.Proponowany)
                {
                    e.RozpocznijIUkoncz(wersja ?? throw new NiezmiennikNaruszonyException("WERSJA_ZADANIA_WYMAGANA", "Ukończenie PROPONOWANEGO Zadania wymaga wersji."), teraz);
                }
                else
                {
                    e.Ukoncz(teraz);
                }
            },
            ct);

    public Task<ElementSkrot> PrzeczytajAsync(UczenId uczen, ElementSciezkiId element, CancellationToken ct) =>
        ZmienElementAsync(uczen, element, (e, teraz) => e.Przeczytaj(teraz), ct);

    public Task<ElementSkrot> ZakonczWkladkePoProsbieOKolejnyAsync(UczenId uczen, ElementSciezkiId element, CancellationToken ct) =>
        ZmienElementAsync(uczen, element, (e, teraz) => e.ZakonczWkladkePoProsbieOKolejny(teraz), ct);

    public Task<ElementSkrot> PominAsync(UczenId uczen, ElementSciezkiId element, CancellationToken ct) =>
        ZmienElementAsync(uczen, element, (e, teraz) => e.Pomin(teraz), ct);

    public Task<ElementSkrot> WygasAsync(UczenId uczen, ElementSciezkiId element, bool wycofanieZadania, CancellationToken ct) =>
        ZmienElementAsync(uczen, element, (e, teraz) => e.Wygas(teraz, wycofanieZadania), ct);

    public Task RozpocznijSerieAsync(UczenId uczen, UmiejetnoscId umiejetnosc, PoziomTrudnosci poziom, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token => (await PobierzLubZalozAsync(uczen, null, token)).RozpocznijSerie(umiejetnosc, poziom, czas.GetUtcNow()),
            ct);

    public Task ZarejestrujWynikSeriiAsync(UczenId uczen, bool sukces, PoziomTrudnosci? nowyPoziom, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token => (await PobierzLubZalozAsync(uczen, null, token)).ZarejestrujWynikSerii(sukces, nowyPoziom),
            ct);

    public Task ZakonczSerieAsync(UczenId uczen, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(async token => (await PobierzLubZalozAsync(uczen, null, token)).ZakonczSerie(), ct);

    private static ElementSkrot Skrot(ElementSciezki e) => new(
        e.Id,
        e.Kolejnosc,
        e.Typ,
        e.Cel,
        e.UmiejetnoscId,
        e.ZadanieId,
        e.ZadanieWersjaId,
        e.WkladkaTeoretycznaId,
        e.PrzypisanieId,
        e.WSerii,
        e.Status,
        e.WygasaAt,
        e.RozpoczetoAt,
        e.ZakonczonoAt);

    private static RekomendacjaSkrot Skrot(Rekomendacja r) => new(
        r.Id,
        r.ElementSciezkiId,
        r.Strategia,
        r.Regula,
        r.PrzewidywanePSukcesu,
        r.Uzasadnienie,
        r.UzasadnienieFaktyJson,
        r.CreatedAt);

    private static SciezkaSkrot Skrot(SciezkaNauki s) => new(
        s.Id,
        s.UczenId,
        s.Version,
        s.Seria is { } seria ? new SeriaSkrot(seria.UmiejetnoscId, seria.RozpoczetaAt, seria.LiczbaElementow, seria.PoziomTrudnosci, seria.KolejnePorazki) : null,
        [.. s.AktywneElementy.Where(e => e.CzyAktywny).OrderBy(e => e.Kolejnosc).Select(Skrot)]);

    private async Task<SciezkaNauki> PobierzLubZalozAsync(UczenId uczen, ElementSciezkiId? dodatkowyElement, CancellationToken ct)
    {
        var sciezka = await sciezki.PobierzAsync(uczen, dodatkowyElement, null, ct);
        if (sciezka is not null)
        {
            return sciezka;
        }

        sciezka = SciezkaNauki.Zaloz(new SciezkaNaukiId(GeneratorUuid.Nowy(czas)), uczen, czas.GetUtcNow());
        sciezki.Dodaj(sciezka);
        return sciezka;
    }

    private Task<ElementSkrot> ZmienElementAsync(UczenId uczen, ElementSciezkiId id, Action<ElementSciezki, DateTimeOffset> zmiana, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var sciezka = await sciezki.PobierzAsync(uczen, id, null, token);
                var element = sciezka?.Znajdz(id)
                    ?? throw new NiezmiennikNaruszonyException("ELEMENT_SCIEZKI_NIE_ZNALEZIONY", "Nie znaleziono Elementu Ścieżki nauki.");
                zmiana(element, czas.GetUtcNow());
                return Skrot(element);
            },
            ct);
}
