// @Role: Agregat Decyzja modelu (Z-4, NFR-05): niezmienny zapis odpowiedzi modelu na Pytanie (rozkład, skrót stanu, czas, błąd) w partycjonowanej tabeli decyzja_modelu | @DependsOn: Lemat.Domain.Common.Agregat, DecyzjaSkladowa, Pytanie, KontekstDecyzji, KonfiguracjaModeluId | @UsedBy: IDecyzjaModeluRepozytorium, ModeleFasada, Agent oceniający (planowane TASK-022) | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Models;

public sealed class DecyzjaModelu : Agregat<DecyzjaModeluId>
{
    public const int DlugoscSkrotuStanu = 32;

    private DecyzjaModelu()
    {
    }

    /// <summary>Część klucza głównego (partycjonowanie miesięczne po <c>created_at</c>).</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    public KontekstDecyzji Kontekst { get; private init; }

    /// <summary>Id Oceny w przygotowaniu / Rekomendacji / wersji Zadania (zależnie od kontekstu).</summary>
    public Guid KontekstId { get; private init; }

    public UczenId? UczenId { get; private init; }

    /// <summary>Dla kontekstu OCENA (typ RozwiazanieId dojdzie z modułem Rozwiązania, TASK-020).</summary>
    public Guid? RozwiazanieId { get; private init; }

    public KonfiguracjaModeluId KonfiguracjaModeluId { get; private init; }

    public string KodPytania { get; private init; } = string.Empty;

    public TypPytania TypPytania { get; private init; }

    public OpcjePytania? Opcje { get; private init; }

    /// <summary>Wybrana wartość (moda); <c>null</c> przy błędzie.</summary>
    public string? Wybor { get; private init; }

    public IReadOnlyDictionary<string, decimal>? Rozklad { get; private init; }

    public int CzasMs { get; private init; }

    /// <summary>SHA-256 stanu wejściowego.</summary>
    public byte[] SkrotStanu { get; private init; } = [];

    /// <summary>Pełny stan; zerowany po 180 dniach lub przy usunięciu konta.</summary>
    public string? Stan { get; private set; }

    public string? Blad { get; private init; }

    public static DecyzjaModelu Zapisz(
        DecyzjaModeluId id,
        DateTimeOffset teraz,
        KontekstDecyzji kontekst,
        Guid kontekstId,
        UczenId? uczen,
        Guid? rozwiazanie,
        KonfiguracjaModeluId konfiguracja,
        Pytanie pytanie,
        DecyzjaSkladowa decyzja,
        int czasMs,
        byte[] skrotStanu,
        string? stan)
    {
        ArgumentNullException.ThrowIfNull(pytanie);
        ArgumentNullException.ThrowIfNull(decyzja);
        if (decyzja.Kod != pytanie.Kod || decyzja.Typ != pytanie.Typ)
        {
            throw new NiezmiennikNaruszonyException("DECYZJA_NIEZGODNA_Z_PYTANIEM", "Kod i typ Decyzji składowej muszą zgadzać się z Pytaniem.");
        }

        DecyzjaSkladowa.WalidujRozklad(decyzja.Rozklad);
        if (!decyzja.Rozklad.ContainsKey(decyzja.Wybor))
        {
            throw new NiezmiennikNaruszonyException("ROZKLAD_NIEPRAWIDLOWY", "Wybór musi należeć do rozkładu.");
        }

        return Zbuduj(id, teraz, kontekst, kontekstId, uczen, rozwiazanie, konfiguracja, pytanie, decyzja.Wybor, decyzja.Rozklad, czasMs, skrotStanu, stan, null);
    }

    /// <summary>Zapis nieudanego wywołania (Z-3): brak wyboru i rozkładu, kod błędu w <see cref="Blad"/>.</summary>
    public static DecyzjaModelu ZapiszBlad(
        DecyzjaModeluId id,
        DateTimeOffset teraz,
        KontekstDecyzji kontekst,
        Guid kontekstId,
        UczenId? uczen,
        Guid? rozwiazanie,
        KonfiguracjaModeluId konfiguracja,
        Pytanie pytanie,
        string blad,
        int czasMs,
        byte[] skrotStanu,
        string? stan)
    {
        ArgumentNullException.ThrowIfNull(pytanie);
        ArgumentException.ThrowIfNullOrWhiteSpace(blad);
        return Zbuduj(id, teraz, kontekst, kontekstId, uczen, rozwiazanie, konfiguracja, pytanie, null, null, czasMs, skrotStanu, stan, blad);
    }

    /// <summary>Zerowanie pełnego stanu po 180 dniach albo przy usunięciu konta (RODO) — jedyna dozwolona zmiana.</summary>
    public void WyzerujStan() => Stan = null;

    private static DecyzjaModelu Zbuduj(
        DecyzjaModeluId id,
        DateTimeOffset teraz,
        KontekstDecyzji kontekst,
        Guid kontekstId,
        UczenId? uczen,
        Guid? rozwiazanie,
        KonfiguracjaModeluId konfiguracja,
        Pytanie pytanie,
        string? wybor,
        IReadOnlyDictionary<string, decimal>? rozklad,
        int czasMs,
        byte[] skrotStanu,
        string? stan,
        string? blad)
    {
        ArgumentNullException.ThrowIfNull(skrotStanu);
        if (czasMs < 0)
        {
            throw new NiezmiennikNaruszonyException("DECYZJA_NIEPRAWIDLOWA", "Czas wywołania nie może być ujemny.");
        }

        if (skrotStanu.Length != DlugoscSkrotuStanu)
        {
            throw new NiezmiennikNaruszonyException("DECYZJA_NIEPRAWIDLOWA", "Skrót stanu to SHA-256 (32 bajty).");
        }

        if (kontekst != KontekstDecyzji.WalidacjaZadania && uczen is null && stan is not null)
        {
            throw new NiezmiennikNaruszonyException("DECYZJA_NIEPRAWIDLOWA", "Pełny stan bez Ucznia jest dozwolony tylko dla walidacji Zadania.");
        }

        return new DecyzjaModelu
        {
            Id = id,
            CreatedAt = teraz,
            Kontekst = kontekst,
            KontekstId = kontekstId,
            UczenId = uczen,
            RozwiazanieId = rozwiazanie,
            KonfiguracjaModeluId = konfiguracja,
            KodPytania = pytanie.Kod,
            TypPytania = pytanie.Typ,
            Opcje = OpcjePytania.Z(pytanie),
            Wybor = wybor,
            Rozklad = rozklad is null ? null : new Dictionary<string, decimal>(rozklad, StringComparer.Ordinal),
            CzasMs = czasMs,
            SkrotStanu = [.. skrotStanu],
            Stan = stan,
            Blad = blad,
        };
    }
}
