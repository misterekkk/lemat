// @Role: Implementacja IProfilFasada: Profil ucznia, rejestracja Wyniku zadania z wagą, AKTUALIZUJ_PROFIL (Elo + atomowa kalibracja trudności) lub odtworzenie po unieważnieniu, Test diagnostyczny z propagacją — każdy przypadek w jednej transakcji | @DependsOn: IProfilRepozytorium, IWynikZadaniaRepozytorium, ITestDiagnostycznyRepozytorium, IParametryZadaniaDlaProfilu, IAktualizacjaKalibracjiTrudnosci, ITresciFasada, IJednostkaPracy | @UsedBy: DI (ProfileRegistration), moduły przez IProfilFasada | @Task: TASK-019
using Lemat.Application.Common;
using Lemat.Application.Content;
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;
using Lemat.Domain.Profile;

namespace Lemat.Application.Profile;

public sealed class ProfilFasada(
    IProfilRepozytorium profile,
    IWynikZadaniaRepozytorium wyniki,
    ITestDiagnostycznyRepozytorium testy,
    IParametryZadaniaDlaProfilu parametry,
    IAktualizacjaKalibracjiTrudnosci kalibracja,
    ITresciFasada tresci,
    IJednostkaPracy jednostkaPracy,
    TimeProvider czas) : IProfilFasada
{
    /// <summary>Okno reguły <c>w_proba</c>: pierwsza próba Ucznia przy Zadaniu w 60 dniach ma wagę 1,0.</summary>
    public static readonly TimeSpan OknoProb = TimeSpan.FromDays(60);

    public Task ZalozProfilAsync(UczenId uczen, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(async token => await PobierzLubZalozAsync(uczen, token), ct);

    public async Task<ProfilSkrot?> PobierzProfilAsync(UczenId uczen, CancellationToken ct)
    {
        var profil = await profile.PobierzDoOdczytuAsync(uczen, ct);
        return profil is null ? null : Skrot(profil);
    }

    public Task<int> InicjalizujOpanowanieAsync(UczenId uczen, IReadOnlyList<UmiejetnoscId> umiejetnosci, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var profil = await PobierzLubZalozAsync(uczen, token);
                return profil.InicjalizujBrakujace(umiejetnosci);
            },
            ct);

    public Task<WynikSkrot?> ZarejestrujWynikAsync(NowyWynikZadania wejscie, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(wejscie);
        return jednostkaPracy.WykonajAsync(
            async token =>
            {
                var istniejace = await wyniki.PobierzPoZrodleAsync(wejscie.OcenaId, wejscie.OdpowiedzId, token);
                var duplikat = istniejace.FirstOrDefault(w => w.Zrodlo == wejscie.Zrodlo && w.UniewaznionyAt is null);
                if (duplikat is not null)
                {
                    return Skrot(duplikat);
                }

                var teraz = czas.GetUtcNow();
                var pierwszaProba = await wyniki.LiczbaWaznychOdAsync(wejscie.UczenId, wejscie.ZadanieId, teraz - OknoProb, token) == 0;
                var id = new WynikZadaniaId(GeneratorUuid.Nowy(czas));
                var wynik = wejscie.Zrodlo == ZrodloWyniku.Odpowiedz
                    ? WynikZadania.ZOdpowiedzi(
                        id,
                        wejscie.UczenId,
                        wejscie.ZadanieId,
                        wejscie.ZadanieWersjaId,
                        wejscie.OdpowiedzId ?? throw new NiezmiennikNaruszonyException("WYNIK_BEZ_ODPOWIEDZI", "Wynik z Odpowiedzi wymaga identyfikatora Odpowiedzi."),
                        wejscie.Ulamek,
                        pierwszaProba,
                        teraz)
                    : WynikZadania.ZOceny(
                        id,
                        wejscie.UczenId,
                        wejscie.ZadanieId,
                        wejscie.ZadanieWersjaId,
                        wejscie.OcenaId ?? throw new NiezmiennikNaruszonyException("WYNIK_BEZ_OCENY", "Wynik z Oceny wymaga identyfikatora Oceny."),
                        wejscie.Ulamek,
                        wejscie.OcenaWstepna,
                        wejscie.MozliwyBladOdczytu,
                        pierwszaProba,
                        teraz);
                if (wynik is null)
                {
                    return null;
                }

                wyniki.Dodaj(wynik);
                return Skrot(wynik);
            },
            ct);
    }

    public Task<int> ZastosujOczekujaceWynikiAsync(UczenId uczen, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var teraz = czas.GetUtcNow();
                var profil = await PobierzLubZalozAsync(uczen, token);
                var uniewaznione = await wyniki.PobierzUniewaznioneNieprzetworzoneAsync(uczen, token);
                var dzialy = await UmiejetnosciDzialowAsync(token);
                if (uniewaznione.Count > 0)
                {
                    return await OdtworzAsync(profil, uczen, uniewaznione, dzialy, teraz, token);
                }

                var oczekujace = await wyniki.PobierzOczekujaceNaProfilAsync(uczen, token);
                if (oczekujace.Count == 0)
                {
                    return 0;
                }

                var zParametrami = await ZParametramiAsync(oczekujace, token);
                var zastosowania = new List<ZastosowanieWyniku>();
                foreach (var grupa in zParametrami.GroupBy(p => p.Wynik.TestDiagnostycznyId))
                {
                    if (grupa.Key is null)
                    {
                        foreach (var (wynik, parametryZadania) in grupa)
                        {
                            var zastosowanie = profil.Zastosuj(wynik, parametryZadania, teraz);
                            if (zastosowanie is not null)
                            {
                                zastosowania.Add(zastosowanie);
                            }
                        }
                    }
                    else
                    {
                        zastosowania.AddRange(profil.ZastosujTestDiagnostyczny([.. grupa], dzialy, teraz));
                    }
                }

                await KalibrujAsync(zastosowania, oczekujace, token);
                return zastosowania.Count;
            },
            ct);

    public Task<int> UniewaznWynikiOcenyAsync(Guid ocenaId, PowodUniewaznieniaWyniku powod, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var teraz = czas.GetUtcNow();
                var znalezione = await wyniki.PobierzPoZrodleAsync(ocenaId, null, token);
                var doUniewaznienia = znalezione.Where(w => w.UniewaznionyAt is null).ToList();
                foreach (var wynik in doUniewaznienia)
                {
                    wynik.Uniewaznij(powod, teraz);
                }

                return doUniewaznienia.Count;
            },
            ct);

    public Task<TestSkrot> RozpocznijTestAsync(
        UczenId uczen,
        PoziomMatury poziom,
        IReadOnlyList<ZadanieWersjaId> wersjeZadan,
        IReadOnlyList<DzialId> pominieteDzialy,
        CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                if (await testy.PobierzAktywnyLubZakonczonyAsync(uczen, poziom, token) is not null)
                {
                    throw new NiezmiennikNaruszonyException("TEST_JUZ_WYKONANY", "Test diagnostyczny na ten poziom matury został już rozpoczęty lub wykonany.");
                }

                var test = TestDiagnostyczny.Rozpocznij(
                    new TestDiagnostycznyId(GeneratorUuid.Nowy(czas)),
                    uczen,
                    poziom,
                    wersjeZadan,
                    pominieteDzialy,
                    czas.GetUtcNow());
                testy.Dodaj(test);
                return Skrot(test);
            },
            ct);

    public async Task<TestSkrot?> PobierzTestAsync(UczenId uczen, TestDiagnostycznyId id, CancellationToken ct)
    {
        var test = await testy.PobierzDoOdczytuAsync(id, ct);
        return test is not null && test.UczenId == uczen ? Skrot(test) : null;
    }

    public Task ZakonczTestAsync(UczenId uczen, TestDiagnostycznyId id, IReadOnlyList<OdpowiedzNaPozycje> odpowiedzi, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(odpowiedzi);
        return jednostkaPracy.WykonajAsync(
            async token =>
            {
                var teraz = czas.GetUtcNow();
                var test = WymagajTestuUcznia(await testy.PobierzAsync(id, token), uczen);
                test.Zakoncz(teraz);

                var wersjePozycji = test.Pozycje.ToDictionary(p => p.Id, p => p);
                var nowe = odpowiedzi
                    .OrderBy(o => wersjePozycji.TryGetValue(o.PozycjaId, out var p) ? p.Kolejnosc : int.MaxValue)
                    .Select(o =>
                    {
                        if (!wersjePozycji.TryGetValue(o.PozycjaId, out var pozycja))
                        {
                            throw new NiezmiennikNaruszonyException("POZYCJA_TESTU_NIEZNANA", "Odpowiedź dotyczy pozycji spoza tego Testu diagnostycznego.");
                        }

                        return WynikZadania.ZTestuDiagnostycznego(
                            new WynikZadaniaId(GeneratorUuid.Nowy(czas)),
                            uczen,
                            o.ZadanieId,
                            pozycja.ZadanieWersjaId,
                            o.OdpowiedzId,
                            test.Id,
                            o.Ulamek,
                            teraz);
                    })
                    .ToList();
                foreach (var wynik in nowe)
                {
                    wyniki.Dodaj(wynik);
                }

                var profil = await PobierzLubZalozAsync(uczen, token);
                var dzialy = await UmiejetnosciDzialowAsync(token);
                var zParametrami = await ZParametramiAsync(nowe, token);
                var zastosowania = profil.ZastosujTestDiagnostyczny(zParametrami, dzialy, teraz);
                await KalibrujAsync(zastosowania, nowe, token);
            },
            ct);
    }

    public Task PominTestAsync(UczenId uczen, TestDiagnostycznyId id, CancellationToken ct) =>
        jednostkaPracy.WykonajAsync(
            async token =>
            {
                var test = WymagajTestuUcznia(await testy.PobierzAsync(id, token), uczen);
                test.Pomin(czas.GetUtcNow());
            },
            ct);

    private static TestDiagnostyczny WymagajTestuUcznia(TestDiagnostyczny? test, UczenId uczen) =>
        test is not null && test.UczenId == uczen
            ? test
            : throw new NiezmiennikNaruszonyException("TEST_NIE_ZNALEZIONY", "Nie znaleziono Testu diagnostycznego.");

    private static ProfilSkrot Skrot(ProfilUcznia profil) => new(
        profil.Id,
        profil.Version,
        [.. profil.Opanowanie.Select(o => new OpanowanieSkrot(
            o.UmiejetnoscId,
            o.Theta,
            o.PoziomOpanowania.Wartosc,
            o.LiczbaWynikow,
            o.OstatniWynikAt,
            o.LicznikPowtorek,
            o.NastepnaPowtorkaAt,
            o.Inicjalizacja))]);

    private static WynikSkrot Skrot(WynikZadania wynik) => new(wynik.Id, wynik.Ulamek, wynik.Waga, wynik.LiczyDoTrudnosci);

    private static TestSkrot Skrot(TestDiagnostyczny test) => new(
        test.Id,
        test.PoziomMatury,
        test.Status,
        test.PominieteDzialy,
        test.ZakonczonoAt,
        [.. test.Pozycje.OrderBy(p => p.Kolejnosc).Select(p => new PozycjaTestuSkrot(p.Id, p.Kolejnosc, p.ZadanieWersjaId))]);

    private async Task<ProfilUcznia> PobierzLubZalozAsync(UczenId uczen, CancellationToken ct)
    {
        var profil = await profile.PobierzAsync(uczen, null, ct);
        if (profil is not null)
        {
            return profil;
        }

        profil = ProfilUcznia.Zaloz(uczen, czas.GetUtcNow());
        profile.Dodaj(profil);
        return profil;
    }

    private async Task<IReadOnlyDictionary<DzialId, IReadOnlyList<UmiejetnoscId>>> UmiejetnosciDzialowAsync(CancellationToken ct) =>
        (await tresci.ListaUmiejetnosciAsync(null, ct))
            .GroupBy(u => u.DzialId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<UmiejetnoscId>)[.. g.Select(u => u.Id)]);

    private async Task<List<(WynikZadania Wynik, ParametryEloZadania Parametry)>> ZParametramiAsync(
        IReadOnlyList<WynikZadania> lista,
        CancellationToken ct)
    {
        var parametryZadan = await parametry.PobierzAsync([.. lista.Select(w => w.ZadanieId).Distinct()], ct);
        return
        [
            .. lista.Select(w => (
                w,
                parametryZadan.TryGetValue(w.ZadanieId, out var p)
                    ? p
                    : throw new NiezmiennikNaruszonyException("ZADANIE_NIE_ZNALEZIONE", "Nie znaleziono Zadania dla Wyniku zadania."))),
        ];
    }

    private async Task<int> OdtworzAsync(
        ProfilUcznia profil,
        UczenId uczen,
        IReadOnlyList<WynikZadania> uniewaznione,
        IReadOnlyDictionary<DzialId, IReadOnlyList<UmiejetnoscId>> dzialy,
        DateTimeOffset teraz,
        CancellationToken ct)
    {
        var wazne = await wyniki.PobierzWazneAsync(uczen, ct);
        var oczekujace = wazne.Where(w => w.ZastosowanyWProfiluAt is null).ToList();
        var zParametrami = await ZParametramiAsync(wazne, ct);
        var zastosowania = profil.Odtworz(zParametrami, uniewaznione, dzialy, teraz);

        // Kalibracja trudności dotyczy wyłącznie Wyników, których nie zastosowano wcześniej (b nie jest odtwarzane, 10.1.3).
        var identyfikatoryOczekujacych = oczekujace.Select(w => w.Id).ToHashSet();
        await KalibrujAsync([.. zastosowania.Where(z => identyfikatoryOczekujacych.Contains(z.WynikId))], oczekujace, ct);
        return wazne.Count;
    }

    private async Task KalibrujAsync(IReadOnlyList<ZastosowanieWyniku> zastosowania, IReadOnlyList<WynikZadania> zrodlowe, CancellationToken ct)
    {
        var poId = zrodlowe.ToDictionary(w => w.Id);
        foreach (var zastosowanie in zastosowania)
        {
            if (poId.TryGetValue(zastosowanie.WynikId, out var wynik) && wynik.LiczyDoTrudnosci)
            {
                await kalibracja.ZastosujAsync(wynik.ZadanieId, zastosowanie.Delta, ct);
            }
        }
    }
}
