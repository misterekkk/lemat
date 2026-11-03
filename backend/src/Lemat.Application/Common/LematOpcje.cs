// @Role: Parametry systemu MVP (sekcja 22) wiązane z sekcją Lemat; wartości domyślne = tabela 22; wybór implementacji Mock/lokalnych | @DependsOn: Microsoft.Extensions.Options | @UsedBy: Lemat.Api.Configuration.ApiHost, Lemat.Worker.Configuration.WorkerHost, Lemat.Scheduler.Configuration.SchedulerHost, LematOpcjeWalidator | @Task: TASK-004, TASK-123, TASK-127
namespace Lemat.Application.Common;

/// <summary>
/// Klasa opcji wiązana z sekcją <see cref="NazwaSekcji"/> konfiguracji (ADR-049 p. 8). Wartości domyślne odpowiadają
/// tabeli parametrów (22); nadpisanie: <c>appsettings*.json</c> albo zmienne <c>LEMAT_*</c> (<see cref="LematKonfiguracja"/>).
/// </summary>
public sealed class LematOpcje
{
    public const string NazwaSekcji = "Lemat";

    /// <summary>Środowisko logiczne (<c>Development</c>, <c>E2E</c>, <c>Production</c>); gdy puste, host ustawia nazwę środowiska ASP.NET Core.</summary>
    public string Srodowisko { get; set; } = string.Empty;

    /// <summary>Stały czas dla testów (<c>LEMAT_CZAS_TESTOWY</c>, ISO 8601); null = zegar systemowy.</summary>
    public DateTimeOffset? CzasTestowy { get; set; }

    public BazaOpcje Baza { get; set; } = new();
    public ZdjeciaOpcje Zdjecia { get; set; } = new();
    public TranskrypcjaOpcje Transkrypcja { get; set; } = new();
    public OcenianieOpcje Ocenianie { get; set; } = new();
    public StrumienOpcje Strumien { get; set; } = new();
    public ProgiOpcje Progi { get; set; } = new();
    public PoprawkiOpcje Poprawki { get; set; } = new();
    public KontaOpcje Konta { get; set; } = new();
    public ModeleOpcje Modele { get; set; } = new();
    public MagazynOpcje Magazyn { get; set; } = new();
    public ObrazyOpcje Obrazy { get; set; } = new();
    public SymbolikaOpcje Symbolika { get; set; } = new();
    public EmailOpcje Email { get; set; } = new();
    public JwtOpcje Jwt { get; set; } = new();
    public KursorOpcje Kursor { get; set; } = new();
}

public sealed class BazaOpcje
{
    /// <summary><c>LEMAT_BAZA</c>: connection string aplikacji (pusty w MVP do TASK-005/013).</summary>
    public string Polaczenie { get; set; } = string.Empty;

    /// <summary><c>LEMAT_BAZA_MIGRACJE</c>: connection string roli migracji.</summary>
    public string PolaczenieMigracji { get; set; } = string.Empty;
}

/// <summary>Limity zdjęć (12.5, 8.1).</summary>
public sealed class ZdjeciaOpcje
{
    public int MaksNaPrzeslanie { get; set; } = 5;
    public int MaksRozmiarMb { get; set; } = 10;
    public IList<string> DozwoloneTypyMime { get; set; } = ["image/jpeg", "image/png", "image/webp"];
    public int MaksPrzeslanNaRozwiazanie { get; set; } = 3;
    public int MaksPonowienRozpoznania { get; set; } = 5;
}

/// <summary>Transkrypcja i odczyt (10.3, 11.1, 11.3, 10.4.3).</summary>
public sealed class TranskrypcjaOpcje
{
    public int MaksKrokow { get; set; } = 40;
    public double ProgSprawdzOdczyt { get; set; } = 0.80;
    public double TauOdczyt { get; set; } = 0.20;
    public double TauFr25 { get; set; } = 0.20;
}

/// <summary>Terminy i limity oceniania (14.2, 10.4.3, 10.5).</summary>
public sealed class OcenianieOpcje
{
    public int MaksParQoc05 { get; set; } = 12;
    public int TerminMiekkiMinimumSekund { get; set; } = 10;
    public int TerminMiekkiBazaSekund { get; set; } = 28;
    public int TerminTwardySekund { get; set; } = 120;
    public int TimeoutAgentaWyjasniajacegoSekund { get; set; } = 8;
}

public sealed class StrumienOpcje
{
    public int HeartbeatSekund { get; set; } = 15;
}

/// <summary>Progi pewności i Kontrola losowa (11.2.2, 11.5.3); właściwe progi są w bazie (<c>prog_pewnosci</c>).</summary>
public sealed class ProgiOpcje
{
    public double PoczatkowyJev { get; set; } = 0.95;
    public double PoczatkowyZapasowy { get; set; } = 0.98;
    public double OdsetekKontroliLosowej { get; set; }
}

public sealed class PoprawkiOpcje
{
    /// <summary>MVP (U-xx): każda Transkrypcja z Poprawkami trafia do Weryfikacji.</summary>
    public bool ZawszeDoWeryfikacji { get; set; } = true;
}

public sealed class KontaOpcje
{
    /// <summary>Tylko Development (HANDOFF U-04); w Production walidacja startu to odrzuca.</summary>
    public bool AutozatwierdzanieNauczycieli { get; set; }

    public bool RejestracjaBezKlasy { get; set; } = true;
}

/// <summary>Wybór dostawcy modeli (ADR-058, ADR-065, A-07).</summary>
public sealed class ModeleOpcje
{
    public const string Mock = "mock";
    public const string OpenAiZgodny = "openai-zgodny";
    public const string AzureOpenAi = "azure-openai";
    public const string Anthropic = "anthropic";

    public string Dostawca { get; set; } = Mock;
    public string KluczApi { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>A-07: potwierdzona retencja po stronie dostawcy; wymagana dla dostawcy innego niż <c>mock</c>.</summary>
    public bool PotwierdzonaRetencjaDostawcy { get; set; }

    public int MockOpoznienieMs { get; set; }
    public double MockMasaModeluDecyzyjnego { get; set; } = 0.995;
}

public sealed class MagazynOpcje
{
    public const string Lokalny = "lokalny";

    public string Typ { get; set; } = Lokalny;

    /// <summary>Katalog magazynu lokalnego (<c>LEMAT_MAGAZYN_SCIEZKA</c>); ścieżka względna jest liczona od katalogu głównego aplikacji.</summary>
    public string Sciezka { get; set; } = string.Empty;

    /// <summary><c>LEMAT_MAGAZYN_KLUCZ</c>: klucz HMAC podpisu adresów magazynu lokalnego (U-03); w Development/E2E domyślny klucz deweloperski.</summary>
    public string Klucz { get; set; } = string.Empty;
}

public sealed class ObrazyOpcje
{
    public const string Prosta = "prosta";

    public string Typ { get; set; } = Prosta;
}

public sealed class SymbolikaOpcje
{
    public const string Prosta = "prosta";

    public string Typ { get; set; } = Prosta;
}

public sealed class EmailOpcje
{
    public const string Log = "log";

    public string Typ { get; set; } = Log;
}

public sealed class JwtOpcje
{
    /// <summary><c>LEMAT_JWT_KLUCZ</c>: PEM ES256; pusty poza Production = klucz generowany i zapisywany lokalnie (TASK-123).</summary>
    public string Klucz { get; set; } = string.Empty;

    public string Wystawca { get; set; } = "lemat";
    public string Odbiorca { get; set; } = "lemat-api";
    public int WaznoscMinut { get; set; } = 15;
    public int OdswiezenieDni { get; set; } = 14;
}

public sealed class KursorOpcje
{
    /// <summary><c>LEMAT_KURSOR_KLUCZ</c>: klucz HMAC podpisu kursora stronicowania (ADR-062 p. 5).</summary>
    public string Klucz { get; set; } = string.Empty;
}
