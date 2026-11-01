// @Role: Agregat Profil ucznia (10.1.3, ADR-010): Opanowanie Umiejętności (theta, poziom, powtórki), stosowanie Wyników zadania modelem Elo, Test diagnostyczny z propagacją w Dziale i deterministyczne odtwarzanie od stanu DOMYŚLNEGO | @DependsOn: Lemat.Domain.Common.Agregat, UczenId, OpanowanieUmiejetnosci, WynikZadania, ModelElo | @UsedBy: IProfilRepozytorium, ProfilFasada, konfiguracja EF | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Profile;

/// <summary>Rezultat zastosowania Wyniku zadania w Profilu: dotknięte Umiejętności oraz <c>delta = s - P</c> (wejście kalibracji trudności).</summary>
public sealed record ZastosowanieWyniku(WynikZadaniaId WynikId, IReadOnlyList<UmiejetnoscId> Dotkniete, decimal Delta);

public sealed class ProfilUcznia : Agregat<UczenId>
{
    private readonly List<OpanowanieUmiejetnosci> _opanowanie = [];

    private ProfilUcznia()
    {
    }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<OpanowanieUmiejetnosci> Opanowanie => _opanowanie;

    public static ProfilUcznia Zaloz(UczenId uczen, DateTimeOffset teraz) => new() { Id = uczen, UpdatedAt = teraz };

    public OpanowanieUmiejetnosci? Znajdz(UmiejetnoscId umiejetnosc) => _opanowanie.Find(o => o.UmiejetnoscId == umiejetnosc);

    /// <summary>Dodaje brakujące Umiejętności w stanie DOMYŚLNYM (9.1, po wyborze poziomu matury); zwraca liczbę dodanych.</summary>
    public int InicjalizujBrakujace(IEnumerable<UmiejetnoscId> umiejetnosci)
    {
        ArgumentNullException.ThrowIfNull(umiejetnosci);
        var dodane = 0;
        foreach (var umiejetnosc in umiejetnosci.Distinct())
        {
            if (Znajdz(umiejetnosc) is null)
            {
                _opanowanie.Add(OpanowanieUmiejetnosci.Domyslne(umiejetnosc));
                dodane++;
            }
        }

        return dodane;
    }

    /// <summary>
    /// AKTUALIZUJ_PROFIL dla jednego Wyniku zadania (10.1.3). Idempotentne: Wynik już zastosowany, unieważniony albo zawieszony
    /// jest pomijany (zwraca <c>null</c>) — takie Wyniki wchodzą do Profilu wyłącznie przez <see cref="Odtworz"/>.
    /// </summary>
    public ZastosowanieWyniku? Zastosuj(WynikZadania wynik, ParametryEloZadania parametry, DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(wynik);
        ArgumentNullException.ThrowIfNull(parametry);
        WymagajWlasciciela(wynik);
        if (wynik.ZastosowanyWProfiluAt is not null || wynik.UniewaznionyAt is not null)
        {
            return null;
        }

        var zastosowanie = ZastosujWynik(wynik, parametry);
        wynik.OznaczZastosowany(teraz);
        UpdatedAt = teraz;
        return zastosowanie;
    }

    /// <summary>
    /// Stosuje razem Wyniki zadania jednego Testu diagnostycznego (w kolejności pozycji) ze współczynnikiem K = 0,8, a następnie
    /// ustawia <c>TEST_DIAGNOSTYCZNY</c> dla przetestowanych Umiejętności i propaguje średnią <c>theta</c> na Umiejętności Działu
    /// bez Wyników (<c>PROPAGACJA_Z_DZIALU</c>).
    /// </summary>
    public IReadOnlyList<ZastosowanieWyniku> ZastosujTestDiagnostyczny(
        IReadOnlyList<(WynikZadania Wynik, ParametryEloZadania Parametry)> wyniki,
        IReadOnlyDictionary<DzialId, IReadOnlyList<UmiejetnoscId>> umiejetnosciDzialow,
        DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(wyniki);
        ArgumentNullException.ThrowIfNull(umiejetnosciDzialow);
        var przetestowane = new HashSet<UmiejetnoscId>();
        var zastosowania = new List<ZastosowanieWyniku>();
        foreach (var (wynik, parametry) in wyniki)
        {
            WymagajWlasciciela(wynik);
            if (wynik.TestDiagnostycznyId is null)
            {
                throw new NiezmiennikNaruszonyException("WYNIK_NIE_Z_TESTU", "Wynik zadania nie pochodzi z Testu diagnostycznego.");
            }

            if (wynik.ZastosowanyWProfiluAt is not null || wynik.UniewaznionyAt is not null)
            {
                continue;
            }

            var zastosowanie = ZastosujWynik(wynik, parametry);
            przetestowane.UnionWith(zastosowanie.Dotkniete);
            zastosowania.Add(zastosowanie);
            wynik.OznaczZastosowany(teraz);
        }

        PropagujPoTestach(przetestowane, umiejetnosciDzialow);
        UpdatedAt = teraz;
        return zastosowania;
    }

    /// <summary>
    /// Odtwarzanie Profilu (po unieważnieniu): cały Profil wraca do stanu DOMYŚLNEGO, a ważne Wyniki (także z Testów diagnostycznych)
    /// są stosowane chronologicznie (<c>created_at</c>, potem <c>id</c>) z bieżącymi <c>trudnosc_b</c>. Deterministyczne i idempotentne.
    /// </summary>
    public IReadOnlyList<ZastosowanieWyniku> Odtworz(
        IEnumerable<(WynikZadania Wynik, ParametryEloZadania Parametry)> wazne,
        IEnumerable<WynikZadania> uniewaznione,
        IReadOnlyDictionary<DzialId, IReadOnlyList<UmiejetnoscId>> umiejetnosciDzialow,
        DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(wazne);
        ArgumentNullException.ThrowIfNull(uniewaznione);
        ArgumentNullException.ThrowIfNull(umiejetnosciDzialow);
        foreach (var opanowanie in _opanowanie)
        {
            opanowanie.Resetuj();
        }

        var uporzadkowane = wazne
            .OrderBy(p => p.Wynik.CreatedAt)
            .ThenBy(p => p.Wynik.Id.Wartosc)
            .ToList();
        var pozostaleWTescie = uporzadkowane
            .Where(p => p.Wynik.TestDiagnostycznyId is not null)
            .GroupBy(p => p.Wynik.TestDiagnostycznyId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
        var przetestowaneWTescie = new Dictionary<TestDiagnostycznyId, HashSet<UmiejetnoscId>>();
        var zastosowania = new List<ZastosowanieWyniku>();
        foreach (var (wynik, parametry) in uporzadkowane)
        {
            WymagajWlasciciela(wynik);
            var zastosowanie = ZastosujWynik(wynik, parametry);
            zastosowania.Add(zastosowanie);
            var dotkniete = zastosowanie.Dotkniete;
            wynik.OznaczZastosowany(teraz);
            if (wynik.TestDiagnostycznyId is not { } test)
            {
                continue;
            }

            if (!przetestowaneWTescie.TryGetValue(test, out var zbior))
            {
                przetestowaneWTescie[test] = zbior = [];
            }

            zbior.UnionWith(dotkniete);
            if (--pozostaleWTescie[test] == 0)
            {
                PropagujPoTestach(zbior, umiejetnosciDzialow);
            }
        }

        foreach (var wynik in uniewaznione)
        {
            WymagajWlasciciela(wynik);
            wynik.OznaczUniewaznieniePrzetworzone(teraz);
        }

        UpdatedAt = teraz;
        return zastosowania;
    }

    private ZastosowanieWyniku ZastosujWynik(WynikZadania wynik, ParametryEloZadania parametry)
    {
        var glowna = Pobierz(parametry.UmiejetnoscGlowna);
        var dodatkowe = parametry.UmiejetnosciDodatkowe
            .Where(u => u != parametry.UmiejetnoscGlowna)
            .Distinct()
            .Select(Pobierz)
            .ToList();
        var thetaEfektywna = ModelElo.ThetaEfektywna(glowna.Theta, [.. dodatkowe.Select(d => d.Theta)]);
        var p = ModelElo.PrawdopodobienstwoSukcesu(thetaEfektywna, parametry.TrudnoscB, parametry.SzansaOdgadniecia);
        var zTestu = wynik.TestDiagnostycznyId is not null;

        // Przyrosty liczone ze stanu sprzed aktualizacji (K zależy od liczby Wyników przed tym Wynikiem).
        var przyrostGlownej = ModelElo.PrzyrostTheta(wynik.Waga, 1m, ModelElo.WspolczynnikK(glowna.LiczbaWynikow, zTestu), wynik.Ulamek, p);
        var przyrostyDodatkowych = dodatkowe
            .Select(d => ModelElo.PrzyrostTheta(wynik.Waga, ModelElo.MnoznikDodatkowej, ModelElo.WspolczynnikK(d.LiczbaWynikow, zTestu), wynik.Ulamek, p))
            .ToList();

        glowna.ZastosujWynik(przyrostGlownej, wynik.Ulamek, wynik.CreatedAt);
        for (var i = 0; i < dodatkowe.Count; i++)
        {
            dodatkowe[i].ZastosujWynik(przyrostyDodatkowych[i], wynik.Ulamek, wynik.CreatedAt);
        }

        return new ZastosowanieWyniku(
            wynik.Id,
            [parametry.UmiejetnoscGlowna, .. dodatkowe.Select(d => d.UmiejetnoscId)],
            wynik.Ulamek - (decimal)p);
    }

    private void PropagujPoTestach(HashSet<UmiejetnoscId> przetestowane, IReadOnlyDictionary<DzialId, IReadOnlyList<UmiejetnoscId>> umiejetnosciDzialow)
    {
        foreach (var umiejetnosc in przetestowane)
        {
            Pobierz(umiejetnosc).OznaczJakoPrzetestowane();
        }

        foreach (var umiejetnosciDzialu in umiejetnosciDzialow.Values)
        {
            var przetestowaneDzialu = umiejetnosciDzialu.Where(przetestowane.Contains).Select(Pobierz).ToList();
            if (przetestowaneDzialu.Count == 0)
            {
                continue;
            }

            var srednia = przetestowaneDzialu.Average(o => o.Theta);
            foreach (var umiejetnosc in umiejetnosciDzialu.Where(u => !przetestowane.Contains(u)))
            {
                var opanowanie = Pobierz(umiejetnosc);
                if (opanowanie.LiczbaWynikow == 0)
                {
                    opanowanie.UstawZPropagacji(srednia);
                }
            }
        }
    }

    private OpanowanieUmiejetnosci Pobierz(UmiejetnoscId umiejetnosc)
    {
        var istniejace = Znajdz(umiejetnosc);
        if (istniejace is not null)
        {
            return istniejace;
        }

        var nowe = OpanowanieUmiejetnosci.Domyslne(umiejetnosc);
        _opanowanie.Add(nowe);
        return nowe;
    }

    private void WymagajWlasciciela(WynikZadania wynik)
    {
        if (wynik.UczenId != Id)
        {
            throw new NiezmiennikNaruszonyException("WYNIK_CUDZEGO_UCZNIA", "Wynik zadania należy do innego Ucznia.");
        }
    }
}
