// @Role: Rejestracja LematOpcje (Options + ValidateOnStart) i mapowanie zmiennych środowiskowych LEMAT_* na klucze sekcji Lemat | @DependsOn: LematOpcje, LematOpcjeWalidator, Microsoft.Extensions.Configuration | @UsedBy: Lemat.Api.Configuration.ApiHost, Lemat.Worker.Configuration.WorkerHost, Lemat.Scheduler.Configuration.SchedulerHost | @Task: TASK-004, TASK-127
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lemat.Application.Common;

/// <summary>Podpięcie konfiguracji aplikacji (sekcja <c>Lemat</c>) w hostach <c>api</c>, <c>worker</c>, <c>scheduler</c>.</summary>
public static class LematKonfiguracja
{
    /// <summary>Zmienna środowiskowa → klucz konfiguracji. Zmiennych spoza tej listy (np. <c>LEMAT_PG_PORT</c>) opcje nie czytają.</summary>
    public static readonly IReadOnlyDictionary<string, string> MapowanieZmiennych = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["LEMAT_SRODOWISKO"] = "Lemat:Srodowisko",
        ["LEMAT_CZAS_TESTOWY"] = "Lemat:CzasTestowy",
        ["LEMAT_BAZA"] = "Lemat:Baza:Polaczenie",
        ["LEMAT_BAZA_MIGRACJE"] = "Lemat:Baza:PolaczenieMigracji",
        ["LEMAT_MODELE_DOSTAWCA"] = "Lemat:Modele:Dostawca",
        ["LEMAT_MODELE_KLUCZ_API"] = "Lemat:Modele:KluczApi",
        ["LEMAT_MODELE_ENDPOINT"] = "Lemat:Modele:Endpoint",
        ["LEMAT_POTWIERDZONA_RETENCJA_DOSTAWCY"] = "Lemat:Modele:PotwierdzonaRetencjaDostawcy",
        ["LEMAT_MOCK_OPOZNIENIE_MS"] = "Lemat:Modele:MockOpoznienieMs",
        ["LEMAT_MAGAZYN_SCIEZKA"] = "Lemat:Magazyn:Sciezka",
        ["LEMAT_MAGAZYN_KLUCZ"] = "Lemat:Magazyn:Klucz",
        ["LEMAT_KURSOR_KLUCZ"] = "Lemat:Kursor:Klucz",
        ["LEMAT_JWT_KLUCZ"] = "Lemat:Jwt:Klucz",
        ["LEMAT_KONTA_AUTOZATWIERDZANIE_NAUCZYCIELI"] = "Lemat:Konta:AutozatwierdzanieNauczycieli",
        ["LEMAT_REJESTRACJA_BEZ_KLASY"] = "Lemat:Konta:RejestracjaBezKlasy",
    };

    /// <summary>Tłumaczy zmienne <c>LEMAT_*</c> na klucze konfiguracji; puste wartości są pomijane.</summary>
    public static IReadOnlyDictionary<string, string?> ZmienneNaKlucze(IDictionary zmienneSrodowiskowe)
    {
        ArgumentNullException.ThrowIfNull(zmienneSrodowiskowe);
        var wynik = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (zmienna, klucz) in MapowanieZmiennych)
        {
            if (zmienneSrodowiskowe[zmienna] is string wartosc && !string.IsNullOrWhiteSpace(wartosc))
            {
                wynik[klucz] = wartosc;
            }
        }

        return wynik;
    }

    /// <summary>Dodaje zmienne <c>LEMAT_*</c> jako źródło o najwyższym priorytecie (po plikach i standardowych zmiennych).</summary>
    public static IConfigurationBuilder DodajZmienneLemat(this IConfigurationBuilder konfiguracja, IDictionary? zmienneSrodowiskowe = null)
    {
        ArgumentNullException.ThrowIfNull(konfiguracja);
        return konfiguracja.AddInMemoryCollection(ZmienneNaKlucze(zmienneSrodowiskowe ?? Environment.GetEnvironmentVariables()));
    }

    /// <summary>
    /// Rejestruje <see cref="LematOpcje"/> (IOptions + singleton) z walidacją przy starcie. Gdy <c>Lemat:Srodowisko</c> jest puste,
    /// przyjmuje nazwę środowiska hosta. <paramref name="walidujPrzyStarcie"/> = false tylko dla generatora dokumentu OpenAPI przy budowie.
    /// </summary>
    public static IServiceCollection AddLematOpcje(this IServiceCollection uslugi, IConfiguration konfiguracja, string nazwaSrodowiskaHosta, bool walidujPrzyStarcie = true)
    {
        ArgumentNullException.ThrowIfNull(uslugi);
        ArgumentNullException.ThrowIfNull(konfiguracja);

        var budowniczy = uslugi.AddOptions<LematOpcje>()
            .Bind(konfiguracja.GetSection(LematOpcje.NazwaSekcji))
            .PostConfigure(opcje =>
            {
                if (string.IsNullOrWhiteSpace(opcje.Srodowisko))
                {
                    opcje.Srodowisko = nazwaSrodowiskaHosta;
                }
            });
        if (walidujPrzyStarcie)
        {
            budowniczy.ValidateOnStart();
        }

        uslugi.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<LematOpcje>, LematOpcjeWalidator>());
        uslugi.TryAddSingleton(dostawca => dostawca.GetRequiredService<IOptions<LematOpcje>>().Value);
        return uslugi;
    }
}
