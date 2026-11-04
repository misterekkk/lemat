// @Role: Agregat Konfiguracja modelu (ADR-003, FR-33): przypisanie dostawcy, modelu, parametrów i wersji promptu do Roli modelu; ≤ 1 aktywna na rolę; aktywacja sprawdza retencję (A-07) i zakaz Mocka w Production | @DependsOn: Lemat.Domain.Common.Agregat, RolaModelu, ParametryModelu, DostawcyModeli | @UsedBy: IKonfiguracjaModeluRepozytorium, ModeleFasada, moduł Kalibracja | @Task: TASK-018
using Lemat.Domain.Accounts;
using Lemat.Domain.Common;

namespace Lemat.Domain.Models;

public sealed class KonfiguracjaModelu : Agregat<KonfiguracjaModeluId>
{
    /// <summary>A-07: dopuszczalna retencja dostawcy dla roli rozpoznawania pisma (zdjęcia).</summary>
    public const int MaksRetencjaRozpoznawaniaDni = 0;

    /// <summary>A-07: dopuszczalna retencja dostawcy dla pozostałych ról.</summary>
    public const int MaksRetencjaPozostalychDni = 30;

    private KonfiguracjaModelu()
    {
    }

    public RolaModelu Rola { get; private init; }

    public string Dostawca { get; private init; } = string.Empty;

    public string Model { get; private init; } = string.Empty;

    public ParametryModelu Parametry { get; private init; } = ParametryModelu.Domyslne;

    public string WersjaPromptu { get; private init; } = string.Empty;

    public bool Aktywna { get; private set; }

    public UzytkownikId? UtworzonaPrzez { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static KonfiguracjaModelu Utworz(
        KonfiguracjaModeluId id,
        RolaModelu rola,
        string dostawca,
        string model,
        ParametryModelu parametry,
        string wersjaPromptu,
        UzytkownikId? utworzonaPrzez,
        DateTimeOffset teraz)
    {
        ArgumentNullException.ThrowIfNull(parametry);
        if (!DostawcyModeli.Wszyscy.Contains(dostawca, StringComparer.Ordinal))
        {
            throw new NiezmiennikNaruszonyException("DOSTAWCA_NIEZNANY", $"Nieznany dostawca modelu '{dostawca}'.");
        }

        if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(wersjaPromptu))
        {
            throw new NiezmiennikNaruszonyException("KONFIGURACJA_MODELU_NIEPRAWIDLOWA", "Model i wersja promptu są wymagane.");
        }

        parametry.Waliduj();
        if (parametry.TrybJev is not null && dostawca != DostawcyModeli.TypesafeJev)
        {
            throw new NiezmiennikNaruszonyException("PARAMETRY_MODELU_NIEPRAWIDLOWE", "tryb_jev dotyczy wyłącznie dostawcy typesafe-jev.");
        }

        if (parametry.DostawcaLlm is not null && dostawca != DostawcyModeli.ZapasowyLlm)
        {
            throw new NiezmiennikNaruszonyException("PARAMETRY_MODELU_NIEPRAWIDLOWE", "dostawca_llm dotyczy wyłącznie dostawcy zapasowy-llm.");
        }

        if (parametry.MaksIteracjiFunkcji is not null && rola != RolaModelu.GeneratywnyZadania)
        {
            throw new NiezmiennikNaruszonyException("PARAMETRY_MODELU_NIEPRAWIDLOWE", "maks_iteracji_funkcji dotyczy wyłącznie roli GENERATYWNY_ZADANIA.");
        }

        return new KonfiguracjaModelu
        {
            Id = id,
            Rola = rola,
            Dostawca = dostawca,
            Model = model.Trim(),
            Parametry = parametry,
            WersjaPromptu = wersjaPromptu.Trim(),
            UtworzonaPrzez = utworzonaPrzez,
            CreatedAt = teraz,
        };
    }

    public bool JestRoleDecyzyjna => Rola is RolaModelu.DecyzyjnyGlowny or RolaModelu.DecyzyjnyZapasowy;

    /// <summary>
    /// Aktywacja odrzuca konfigurację z retencją dostawcy ponad A-07 (<c>BRAK_RETENCJI_DOSTAWCY</c>) oraz Mocka w Production
    /// (<c>DOSTAWCA_NIEDOZWOLONY</c>). Zamknięcie poprzedniej aktywnej konfiguracji roli to zadanie repozytorium (jedna transakcja).
    /// </summary>
    public void Aktywuj(bool srodowiskoProdukcyjne)
    {
        if (srodowiskoProdukcyjne && Dostawca == DostawcyModeli.Mock)
        {
            throw new NiezmiennikNaruszonyException("DOSTAWCA_NIEDOZWOLONY", "Dostawca mock jest niedozwolony w środowisku Production.");
        }

        var limit = Rola == RolaModelu.RozpoznawaniePisma ? MaksRetencjaRozpoznawaniaDni : MaksRetencjaPozostalychDni;
        if (Parametry.RetencjaDostawcyDni > limit)
        {
            throw new NiezmiennikNaruszonyException(
                "BRAK_RETENCJI_DOSTAWCY",
                $"Retencja dostawcy {Parametry.RetencjaDostawcyDni} dni przekracza dopuszczalne {limit} dni (A-07).");
        }

        Aktywna = true;
    }

    public void Dezaktywuj() => Aktywna = false;
}
