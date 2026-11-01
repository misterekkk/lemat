// @Role: Implementacja ITresciFasada nad repozytoriami Treści (tylko odczyt; mapowanie agregatów na migawki) | @DependsOn: IZadanieRepozytorium, IWkladkaTeoretycznaRepozytorium, IDzialRepozytorium, IUmiejetnoscRepozytorium | @UsedBy: DI (ContentRegistration), moduły przez ITresciFasada | @Task: TASK-017
using Lemat.Domain.Content;

namespace Lemat.Application.Content;

public sealed class TresciFasada(
    IZadanieRepozytorium zadania,
    IWkladkaTeoretycznaRepozytorium wkladki,
    IDzialRepozytorium dzialy,
    IUmiejetnoscRepozytorium umiejetnosci) : ITresciFasada
{
    public async Task<ZadanieDoOceny?> PobierzZadanieDoOcenyAsync(ZadanieWersjaId wersja, CancellationToken ct)
    {
        var zadanie = await zadania.PobierzPoWersjiAsync(wersja, ct);
        var tresc = zadanie?.Wersje.FirstOrDefault(w => w.Id == wersja);
        if (zadanie is null || tresc is null)
        {
            return null;
        }

        return new ZadanieDoOceny(
            zadanie.Id,
            tresc.Id,
            tresc.NrWersji,
            zadanie.Rodzaj,
            zadanie.FormaZamknieta,
            zadanie.PoziomMatury,
            zadanie.DzialId,
            zadanie.UmiejetnoscGlowna,
            tresc.TrescMd,
            tresc.PunktyMax,
            tresc.OpcjeZamkniete,
            tresc.RozwiazanieWzorcowe,
            tresc.WynikKoncowyWzorcowy,
            [.. tresc.KryteriaOceniania.OrderBy(k => k.Wariant.Wartosc).ThenBy(k => k.NrKryterium)]);
    }

    public async Task<ZadanieUcznia?> PobierzZadanieUczniaAsync(ZadanieId zadanie, CancellationToken ct)
    {
        var agregat = await zadania.PobierzDoOdczytuAsync(zadanie, ct);
        if (agregat is null)
        {
            return null;
        }

        var wersja = agregat.AktualnaWersja;
        return new ZadanieUcznia(
            agregat.Id,
            wersja.Id,
            wersja.NrWersji,
            agregat.Status,
            agregat.Zasieg,
            agregat.AutorId,
            agregat.Rodzaj,
            agregat.PoziomMatury,
            agregat.DzialId,
            agregat.UmiejetnoscGlowna,
            wersja.TrescMd,
            wersja.PunktyMax,
            wersja.OpcjeZamkniete is { } opcje
                ? new OpcjeZamknieteUcznia(opcje.Forma, opcje.Opcje, [.. opcje.Stwierdzenia.Select(s => new StwierdzenieDlaUcznia(s.Id, s.TrescMd))])
                : null);
    }

    public Task<IReadOnlyList<ZadanieDoDoboru>> PobierzKandydatowDoboruAsync(FiltrKandydatow filtr, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filtr);
        return filtr.Rodzaje.Count == 0
            ? Task.FromResult<IReadOnlyList<ZadanieDoDoboru>>([])
            : zadania.PobierzKandydatowAsync(filtr, ct);
    }

    public async Task<IReadOnlyList<DzialSkrot>> ListaDzialowAsync(CancellationToken ct) =>
        [.. (await dzialy.ListaAsync(ct)).Select(d => new DzialSkrot(d.Id, d.Kod, d.Nazwa, d.TylkoRozszerzony, d.Kolejnosc))];

    public async Task<IReadOnlyList<UmiejetnoscSkrot>> ListaUmiejetnosciAsync(DzialId? dzial, CancellationToken ct) =>
        [.. (await umiejetnosci.ListaAsync(dzial, ct)).Select(u => new UmiejetnoscSkrot(
            u.Id, u.DzialId, u.Kod, u.Nazwa, u.TylkoRozszerzony, u.WagaEgzaminacyjna, u.WymaganeUmiejetnosci))];

    public async Task<WkladkaDoDoboru?> PobierzOpublikowanaWkladkeAsync(UmiejetnoscId umiejetnosc, CancellationToken ct)
    {
        var wkladka = await wkladki.PobierzOpublikowanaAsync(umiejetnosc, ct);
        return wkladka is null ? null : new WkladkaDoDoboru(wkladka.Id, wkladka.UmiejetnoscId, wkladka.SzacowanyCzasMin);
    }
}
