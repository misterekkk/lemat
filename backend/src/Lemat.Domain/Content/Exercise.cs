// @Role: Agregat Zadanie: pochodzenie, zasięg, rodzaj, Dział, Umiejętności (główna + dodatkowe), Poziom trudności nominalny, status (8.4), wersjonowanie (ADR-012: nowa wersja zamyka starą) | @DependsOn: Lemat.Domain.Common.Agregat, ZadanieWersja, TrescWersji, UmiejetnoscId, DzialId, UzytkownikId | @UsedBy: repozytoria Treści, ITresciFasada, moduły Rozwiązania, Rekomendacje, Klasy, Agent generujący | @Task: TASK-017
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Content;

/// <summary>Powiązanie Zadania z Umiejętnością (tabela <c>zadanie_umiejetnosc</c>); dokładnie jedno jest główne.</summary>
public sealed record PowiazanieUmiejetnosci(UmiejetnoscId UmiejetnoscId, bool Glowna);

/// <summary>Dane założenia Zadania (wersja 1 powstaje razem z Zadaniem).</summary>
public sealed record NoweZadanie(
    ZadanieId Id,
    PochodzenieZadania Pochodzenie,
    ZasiegZadania Zasieg,
    RodzajZadania Rodzaj,
    FormaZamknieta? FormaZamknieta,
    DzialId DzialId,
    PoziomMatury PoziomMatury,
    ArkuszCkeId? ArkuszCkeId,
    string? NumerWArkuszu,
    UzytkownikId? AutorId,
    ZlecenieGenerowaniaId? ZlecenieGenerowaniaId,
    StatusZadania Status,
    PoziomTrudnosci PoziomTrudnosciNominalny,
    bool Diagnostyczne,
    UmiejetnoscId UmiejetnoscGlowna,
    IReadOnlyList<UmiejetnoscId> UmiejetnosciDodatkowe,
    TrescWersji Tresc);

public sealed class Zadanie : Agregat<ZadanieId>
{
    private readonly List<ZadanieWersja> _wersje = [];
    private readonly List<PowiazanieUmiejetnosci> _umiejetnosci = [];

    private Zadanie()
    {
    }

    public PochodzenieZadania Pochodzenie { get; private init; }

    public ZasiegZadania Zasieg { get; private set; }

    public RodzajZadania Rodzaj { get; private init; }

    /// <summary>Różne od <c>null</c> wtedy i tylko wtedy, gdy <see cref="Rodzaj"/> to Zamkniete.</summary>
    public FormaZamknieta? FormaZamknieta { get; private init; }

    public DzialId DzialId { get; private init; }

    public PoziomMatury PoziomMatury { get; private init; }

    /// <summary>Różne od <c>null</c> wtedy i tylko wtedy, gdy pochodzenie to BazoweArkuszCke.</summary>
    public ArkuszCkeId? ArkuszCkeId { get; private init; }

    public string? NumerWArkuszu { get; private init; }

    public UzytkownikId? AutorId { get; private set; }

    /// <summary>Różne od <c>null</c> wtedy i tylko wtedy, gdy pochodzenie to Wygenerowane.</summary>
    public ZlecenieGenerowaniaId? ZlecenieGenerowaniaId { get; private init; }

    public StatusZadania Status { get; private set; }

    public ZadanieWersjaId AktualnaWersjaId { get; private set; }

    /// <summary>Zmienia wyłącznie człowiek (ADR-029); skalibrowany poziom leży w <see cref="KalibracjaTrudnosci"/>.</summary>
    public PoziomTrudnosci PoziomTrudnosciNominalny { get; private set; }

    /// <summary>Niezmiennik: tylko Zadanie zamknięte o zasięgu GLOBALNY.</summary>
    public bool Diagnostyczne { get; private set; }

    public bool ZweryfikowanePrzezNauczyciela { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ZadanieWersja> Wersje => _wersje;

    public IReadOnlyList<PowiazanieUmiejetnosci> Umiejetnosci => _umiejetnosci;

    public ZadanieWersja AktualnaWersja => _wersje.Single(w => w.Id == AktualnaWersjaId);

    public UmiejetnoscId UmiejetnoscGlowna => _umiejetnosci.Single(u => u.Glowna).UmiejetnoscId;

    public IReadOnlyList<UmiejetnoscId> UmiejetnosciDodatkowe => [.. _umiejetnosci.Where(u => !u.Glowna).Select(u => u.UmiejetnoscId)];

    public bool CzyBazowe => Pochodzenie is PochodzenieZadania.BazoweArkuszCke or PochodzenieZadania.BazoweNauczyciel;

    public bool CzyWygenerowane => Pochodzenie == PochodzenieZadania.Wygenerowane;

    public static Zadanie Utworz(NoweZadanie dane, DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(dane);
        SprawdzSpojnosc(dane);

        var zadanie = new Zadanie
        {
            Id = dane.Id,
            Pochodzenie = dane.Pochodzenie,
            Zasieg = dane.Zasieg,
            Rodzaj = dane.Rodzaj,
            FormaZamknieta = dane.FormaZamknieta,
            DzialId = dane.DzialId,
            PoziomMatury = dane.PoziomMatury,
            ArkuszCkeId = dane.ArkuszCkeId,
            NumerWArkuszu = dane.NumerWArkuszu,
            AutorId = dane.AutorId,
            ZlecenieGenerowaniaId = dane.ZlecenieGenerowaniaId,
            Status = dane.Status,
            PoziomTrudnosciNominalny = dane.PoziomTrudnosciNominalny,
            Diagnostyczne = dane.Diagnostyczne,
            CreatedAt = teraz,
            UpdatedAt = teraz,
        };
        zadanie._umiejetnosci.Add(new PowiazanieUmiejetnosci(dane.UmiejetnoscGlowna, true));
        foreach (var dodatkowa in dane.UmiejetnosciDodatkowe.Distinct().Where(u => u != dane.UmiejetnoscGlowna))
        {
            zadanie._umiejetnosci.Add(new PowiazanieUmiejetnosci(dodatkowa, false));
        }

        var pierwsza = ZadanieWersja.Utworz(
            new ZadanieWersjaId(Guid.CreateVersion7(teraz)), 1, dane.Rodzaj, dane.FormaZamknieta, dane.Tresc, dane.AutorId, teraz);
        zadanie._wersje.Add(pierwsza);
        zadanie.AktualnaWersjaId = pierwsza.Id;
        return zadanie;
    }

    /// <summary>ADR-012: nowa wersja zamyka starą (<c>zastapiona_at</c>), stara pozostaje niezmienna.</summary>
    public ZadanieWersja Popraw(TrescWersji tresc, UzytkownikId autor, DateTimeOffset teraz)
    {
        if (Status is StatusZadania.Wycofane or StatusZadania.Odrzucone or StatusZadania.OdrzuconeWWalidacji)
        {
            throw new NiezmiennikNaruszonyException("ZADANIE_NIEPRAWIDLOWY_STAN", "Zadanie wycofane lub odrzucone nie przyjmuje poprawek.");
        }

        var nowa = ZadanieWersja.Utworz(
            new ZadanieWersjaId(Guid.CreateVersion7(teraz)),
            _wersje.Max(w => w.NrWersji) + 1,
            Rodzaj,
            FormaZamknieta,
            tresc,
            autor,
            teraz);
        AktualnaWersja.Zastap(teraz);
        _wersje.Add(nowa);
        AktualnaWersjaId = nowa.Id;
        return nowa;
    }

    /// <summary>Przegląd: PRYWATNY -> GLOBALNY (ADR-028, ADR-038); Zadanie z Arkusza CKE jest zawsze GLOBALNY.</summary>
    public void Promuj(ZasiegZadania zasiegPo)
    {
        if (Pochodzenie == PochodzenieZadania.BazoweArkuszCke && zasiegPo != ZasiegZadania.Globalny)
        {
            throw new NiezmiennikNaruszonyException("ZASIEG_NIEDOZWOLONY", "Zadanie z Arkusza CKE jest zawsze GLOBALNY.");
        }

        if (zasiegPo == ZasiegZadania.Prywatny && Diagnostyczne)
        {
            throw new NiezmiennikNaruszonyException("ZASIEG_NIEDOZWOLONY", "Zadanie diagnostyczne musi być GLOBALNY.");
        }

        Zasieg = zasiegPo;
    }

    public void UstawPoziomNominalny(PoziomTrudnosci poziom) => PoziomTrudnosciNominalny = poziom;

    public void UstawDiagnostyczne(bool diagnostyczne)
    {
        if (diagnostyczne && (Rodzaj != RodzajZadania.Zamkniete || Zasieg != ZasiegZadania.Globalny))
        {
            throw new NiezmiennikNaruszonyException("ZADANIE_DIAGNOSTYCZNE_NIEDOZWOLONE", "Diagnostyczne może być tylko Zadanie zamknięte o zasięgu GLOBALNY.");
        }

        Diagnostyczne = diagnostyczne;
    }

    public void OznaczJakoZweryfikowane() => ZweryfikowanePrzezNauczyciela = true;

    // ---- maszyna stanów 8.4 ----

    /// <summary>WALIDACJA -> ODRZUCONE_W_WALIDACJI.</summary>
    public void OdrzucWWalidacji() => Przejdz(StatusZadania.OdrzuconeWWalidacji, StatusZadania.Walidacja);

    /// <summary>WALIDACJA -> DO_PRZEGLADU.</summary>
    public void PrzekazDoPrzegladu() => Przejdz(StatusZadania.DoPrzegladu, StatusZadania.Walidacja);

    /// <summary>WALIDACJA albo DO_PRZEGLADU -> OPUBLIKOWANE; opcjonalnie zmienia zasięg (Przegląd: zlecający -> PRYWATNY, recenzent -> GLOBALNY).</summary>
    public void Opublikuj(ZasiegZadania? zasieg = null)
    {
        Przejdz(StatusZadania.Opublikowane, StatusZadania.Walidacja, StatusZadania.DoPrzegladu);
        if (zasieg is { } docelowy)
        {
            Promuj(docelowy);
        }
    }

    /// <summary>DO_PRZEGLADU -> ODRZUCONE.</summary>
    public void Odrzuc() => Przejdz(StatusZadania.Odrzucone, StatusZadania.DoPrzegladu);

    /// <summary>OPUBLIKOWANE -> WYCOFANE.</summary>
    public void Wycofaj() => Przejdz(StatusZadania.Wycofane, StatusZadania.Opublikowane);

    private void Przejdz(StatusZadania docelowy, params StatusZadania[] dozwoloneZrodla)
    {
        if (!dozwoloneZrodla.Contains(Status))
        {
            throw new NiezmiennikNaruszonyException("ZADANIE_NIEPRAWIDLOWY_STAN", $"Przejście {Status} -> {docelowy} jest niedozwolone.");
        }

        Status = docelowy;
    }

    private static void SprawdzSpojnosc(NoweZadanie dane)
    {
        if ((dane.Rodzaj == RodzajZadania.Zamkniete) != dane.FormaZamknieta.HasValue)
        {
            throw new NiezmiennikNaruszonyException("ZADANIE_FORMA_NIESPOJNA", "Forma zamknięta jest ustawiona wtedy i tylko wtedy, gdy Zadanie jest zamknięte.");
        }

        if ((dane.Pochodzenie == PochodzenieZadania.BazoweArkuszCke) != dane.ArkuszCkeId.HasValue)
        {
            throw new NiezmiennikNaruszonyException("ZADANIE_ARKUSZ_NIESPOJNY", "Arkusz CKE jest ustawiony wtedy i tylko wtedy, gdy pochodzenie to BAZOWE_ARKUSZ_CKE.");
        }

        if ((dane.Pochodzenie == PochodzenieZadania.Wygenerowane) != dane.ZlecenieGenerowaniaId.HasValue)
        {
            throw new NiezmiennikNaruszonyException("ZADANIE_ZLECENIE_NIESPOJNE", "Zlecenie generowania jest ustawione wtedy i tylko wtedy, gdy Zadanie jest wygenerowane.");
        }

        if (dane.Pochodzenie == PochodzenieZadania.BazoweArkuszCke && dane.Zasieg != ZasiegZadania.Globalny)
        {
            throw new NiezmiennikNaruszonyException("ZASIEG_NIEDOZWOLONY", "Zadanie z Arkusza CKE jest zawsze GLOBALNY.");
        }

        if (dane.Diagnostyczne && (dane.Rodzaj != RodzajZadania.Zamkniete || dane.Zasieg != ZasiegZadania.Globalny))
        {
            throw new NiezmiennikNaruszonyException("ZADANIE_DIAGNOSTYCZNE_NIEDOZWOLONE", "Diagnostyczne może być tylko Zadanie zamknięte o zasięgu GLOBALNY.");
        }

        if (dane.Status is not (StatusZadania.Walidacja or StatusZadania.Opublikowane))
        {
            throw new NiezmiennikNaruszonyException("ZADANIE_NIEPRAWIDLOWY_STAN", "Zadanie powstaje w stanie WALIDACJA albo OPUBLIKOWANE.");
        }
    }
}
