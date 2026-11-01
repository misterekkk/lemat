// @Role: Implementacja IKlasyFasada nad IKlasaRepozytorium i IJednostkaPracy: przypadki użycia Klasy z uprawnieniami obiektowymi (ADR-071), losowaniem Kodu zaproszenia i zapisem w jednej transakcji | @DependsOn: IKlasaRepozytorium, IJednostkaPracy, TimeProvider, Lemat.Domain.Classes | @UsedBy: DI (ClassesRegistration), moduły przez IKlasyFasada | @Task: TASK-018
using Lemat.Application.Common;
using Lemat.Domain.Accounts;
using Lemat.Domain.Classes;
using Lemat.Domain.Common;

namespace Lemat.Application.Classes;

public sealed class KlasyFasada(IKlasaRepozytorium klasy, IJednostkaPracy jednostkaPracy, TimeProvider czas) : IKlasyFasada
{
    private const int MaksProbLosowaniaKodu = 5;

    public async Task<KlasaSkrot> ZalozAsync(NauczycielId nauczyciel, string nazwa, PoziomMatury poziom, CancellationToken ct)
    {
        var klasa = await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var teraz = czas.GetUtcNow();
                var kod = await LosujKodAsync(teraz, token);
                var nowa = Klasa.Zaloz(new KlasaId(GeneratorUuid.Nowy(czas)), nauczyciel, nazwa, poziom, kod, teraz);
                klasy.Dodaj(nowa);
                return nowa;
            },
            ct);
        return Skrot(klasa, pokazKod: true);
    }

    public async Task<KlasaSkrot?> PobierzAsync(KlasaId id, CancellationToken ct)
    {
        var klasa = await klasy.PobierzDoOdczytuAsync(id, ct);
        return klasa is null ? null : Skrot(klasa, pokazKod: false);
    }

    public async Task<KlasaSkrot> PobierzDlaNauczycielaAsync(NauczycielId nauczyciel, KlasaId id, CancellationToken ct)
    {
        var klasa = await klasy.PobierzDoOdczytuAsync(id, ct);
        return Skrot(WymagajProwadzacego(klasa, nauczyciel), pokazKod: true);
    }

    public async Task<IReadOnlyList<KlasaSkrot>> ListaKlasNauczycielaAsync(NauczycielId nauczyciel, CancellationToken ct) =>
        [.. (await klasy.ListaNauczycielaAsync(nauczyciel, ct)).Select(k => Skrot(k, pokazKod: true))];

    public async Task<IReadOnlyList<KlasaSkrot>> ListaKlasUczniaAsync(UczenId uczen, CancellationToken ct) =>
        [.. (await klasy.ListaUczniaAsync(uczen, ct)).Select(k => Skrot(k, pokazKod: false))];

    public async Task<KlasaSkrot?> PobierzNajpozniejDolaczonaKlaseAsync(UczenId uczen, CancellationToken ct)
    {
        var lista = await klasy.ListaUczniaAsync(uczen, ct);
        var klasa = lista.FirstOrDefault(k => !k.Zarchiwizowana);
        return klasa is null ? null : Skrot(klasa, pokazKod: false);
    }

    public async Task<KlasaSkrot> DolaczAsync(UczenId uczen, string kod, CancellationToken ct)
    {
        var klasa = await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var znaleziona = await klasy.PobierzPoKodzieAsync(KodZaproszenia.Normalizuj(kod), token)
                    ?? throw new NiezmiennikNaruszonyException("KOD_ZAPROSZENIA_NIEWAZNY", "Kod zaproszenia jest nieważny albo wygasł.");
                znaleziona.Dolacz(uczen, kod, czas.GetUtcNow());
                return znaleziona;
            },
            ct);
        return Skrot(klasa, pokazKod: false);
    }

    public Task OpuscAsync(UczenId uczen, KlasaId id, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var klasa = await klasy.PobierzAsync(id, null, token);
                if (klasa is null || !klasa.CzyCzlonek(uczen))
                {
                    throw BrakKlasy();
                }

                klasa.Opusc(uczen);
            },
            ct);

    public Task<KodZaproszeniaSkrot> OdnowKodAsync(NauczycielId nauczyciel, KlasaId id, int? oczekiwanaWersja, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var klasa = WymagajProwadzacego(await klasy.PobierzAsync(id, oczekiwanaWersja, token), nauczyciel);
                var nowy = await LosujKodAsync(czas.GetUtcNow(), token);
                klasa.OdnowKod(nowy);
                return new KodZaproszeniaSkrot(nowy.Wartosc, nowy.WaznyDo);
            },
            ct);

    public Task UniewaznKodAsync(NauczycielId nauczyciel, KlasaId id, int? oczekiwanaWersja, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var klasa = WymagajProwadzacego(await klasy.PobierzAsync(id, oczekiwanaWersja, token), nauczyciel);
                klasa.UniewaznKod(czas.GetUtcNow());
            },
            ct);

    public Task ArchiwizujAsync(NauczycielId nauczyciel, KlasaId id, int? oczekiwanaWersja, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var klasa = WymagajProwadzacego(await klasy.PobierzAsync(id, oczekiwanaWersja, token), nauczyciel);
                klasa.Archiwizuj(czas.GetUtcNow());
            },
            ct);

    public async Task<KlasaSkrot> PrzekazAsync(KlasaId id, NauczycielId nowy, int? oczekiwanaWersja, CancellationToken ct)
    {
        var klasa = await jednostkaPracy.WykonajAsync(
            async token =>
            {
                var znaleziona = await klasy.PobierzAsync(id, oczekiwanaWersja, token) ?? throw BrakKlasy();
                znaleziona.Przekaz(nowy, czas.GetUtcNow());
                return znaleziona;
            },
            ct);
        return Skrot(klasa, pokazKod: false);
    }

    private static Klasa WymagajProwadzacego(Klasa? klasa, NauczycielId nauczyciel) =>
        klasa is { NauczycielId: { } prowadzacy } && prowadzacy == nauczyciel ? klasa : throw BrakKlasy();

    private static NiezmiennikNaruszonyException BrakKlasy() => new("KLASA_NIE_ZNALEZIONA", "Nie znaleziono Klasy.");

    private async Task<KodZaproszenia> LosujKodAsync(DateTimeOffset teraz, CancellationToken ct)
    {
        for (var proba = 0; proba < MaksProbLosowaniaKodu; proba++)
        {
            var kod = GeneratorKoduZaproszenia.Nowy(teraz);
            if (!await klasy.KodZajetyAsync(kod.Wartosc, ct))
            {
                return kod;
            }
        }

        throw new NiezmiennikNaruszonyException("KOD_ZAPROSZENIA_ZAJETY", "Nie udało się wylosować wolnego Kodu zaproszenia.");
    }

    private static KlasaSkrot Skrot(Klasa klasa, bool pokazKod) => new(
        klasa.Id,
        klasa.Nazwa,
        klasa.PoziomMatury,
        klasa.NauczycielId,
        klasa.Zarchiwizowana,
        klasa.Czlonkowie.Count,
        pokazKod ? new KodZaproszeniaSkrot(klasa.KodZaproszenia.Wartosc, klasa.KodZaproszenia.WaznyDo) : null,
        klasa.Version);
}
