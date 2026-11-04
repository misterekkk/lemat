// @Role: Agregat Test diagnostyczny (FR-03, 9.1): pozycje z puli diagnostycznej, Działy pominięte, statusy W_TOKU/ZAKONCZONY/POMINIETY (zakończenie i pominięcie tylko z W_TOKU) | @DependsOn: Lemat.Domain.Common.Agregat, UczenId, PoziomMatury, PozycjaTestuDiagnostycznego, StatusTestu | @UsedBy: ITestDiagnostycznyRepozytorium, ProfilFasada, konfiguracja EF | @Task: TASK-019
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;
using Lemat.Domain.Content;

namespace Lemat.Domain.Profile;

public sealed class TestDiagnostyczny : Agregat<TestDiagnostycznyId>
{
    private readonly List<PozycjaTestuDiagnostycznego> _pozycje = [];

    private TestDiagnostyczny()
    {
    }

    public UczenId UczenId { get; private init; }

    public PoziomMatury PoziomMatury { get; private init; }

    public StatusTestu Status { get; private set; }

    /// <summary>Wartości <see cref="DzialId"/> Działów bez Zadań diagnostycznych w chwili utworzenia (kolumna <c>integer[]</c>).</summary>
    public IReadOnlyList<int> PominieteDzialy => PominieteDzialyKolumna;

    public DateTimeOffset CreatedAt { get; private init; }

    private int[] PominieteDzialyKolumna { get; init; } = [];

    public DateTimeOffset? ZakonczonoAt { get; private set; }

    public IReadOnlyList<PozycjaTestuDiagnostycznego> Pozycje => _pozycje;

    /// <summary>Tworzy test <c>W_TOKU</c> z pozycjami w podanej kolejności (numerowanymi od 1); wymaga co najmniej jednej pozycji.</summary>
    public static TestDiagnostyczny Rozpocznij(
        TestDiagnostycznyId id,
        UczenId uczen,
        PoziomMatury poziom,
        IReadOnlyList<ZadanieWersjaId> wersjeZadan,
        IReadOnlyList<DzialId> pominieteDzialy,
        DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(wersjeZadan);
        ArgumentNullException.ThrowIfNull(pominieteDzialy);
        if (wersjeZadan.Count == 0)
        {
            throw new NiezmiennikNaruszonyException("TEST_NIEDOSTEPNY", "Test diagnostyczny wymaga co najmniej jednej pozycji.");
        }

        var test = new TestDiagnostyczny
        {
            Id = id,
            UczenId = uczen,
            PoziomMatury = poziom,
            Status = StatusTestu.WToku,
            PominieteDzialyKolumna = [.. pominieteDzialy.Select(d => d.Wartosc).Distinct()],
            CreatedAt = teraz,
        };
        for (var i = 0; i < wersjeZadan.Count; i++)
        {
            test._pozycje.Add(PozycjaTestuDiagnostycznego.Utworz(new PozycjaTestuDiagnostycznegoId(Guid.CreateVersion7()), i + 1, wersjeZadan[i]));
        }

        return test;
    }

    public void Zakoncz(DateTimeOffset teraz) => Zamknij(StatusTestu.Zakonczony, teraz);

    public void Pomin(DateTimeOffset teraz) => Zamknij(StatusTestu.Pominiety, teraz);

    private void Zamknij(StatusTestu docelowy, DateTimeOffset teraz)
    {
        if (Status != StatusTestu.WToku)
        {
            throw new NiezmiennikNaruszonyException("TEST_NIEPRAWIDLOWY_STAN", "Test diagnostyczny nie jest w toku.");
        }

        Status = docelowy;
        ZakonczonoAt = teraz;
    }
}
