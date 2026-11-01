# Lemat — kontrakty API REST i SSE (ASP.NET Core 10 ↔ Next.js)

| Pole | Wartość |
|---|---|
| Dokument | `docs/spec/api.md` |
| Wersja | 3.2 (pętla 3.x, iteracja 3) |
| Dotyczy | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — sekcja 12 (konwencje, katalog endpointów, kody błędów), 14 (obsługa błędów), 15 (współbieżność); ADR-060 … ADR-064 |
| Pliki w repozytorium | `backend/src/Lemat.Api/Kontrakty/<Modul>/*.cs` (rekordy), `backend/src/Lemat.Api/Endpointy/<Modul>Endpointy.cs`, `backend/openapi/lemat-v1.json` (generowany), `frontend/src/lib/api/schema.d.ts` (generowany), `frontend/src/lib/api/klient.ts`, `frontend/src/lib/sse/strumien.ts` |

Plik zawiera długie listingi, do których odwołuje się sekcja 12 dokumentu
głównego: (1) konwencje serializacji i nagłówki, (2) model błędu, (3)
rekordy C# kontraktów (źródło OpenAPI), (4) przykład grupy endpointów z
filtrami, (5) kontrakty SSE, (6) klient Next.js, (7) mapowanie stron App
Router na endpointy.

## 1. Konwencje serializacji i nagłówki

```csharp
// Lemat.Api/Konfiguracja/JsonApi.cs — te same opcje dla odpowiedzi, żądań i generatora OpenAPI (ADR-061 p. 2)
public static class JsonApi
{
    public static void Konfiguruj(JsonSerializerOptions o)
    {
        o.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        o.NumberHandling = JsonNumberHandling.Strict;                    // integer w OpenAPI bez wariantu string (sprawdzone 10.0.14)
        o.DefaultIgnoreCondition = JsonIgnoreCondition.Never;             // null jest wysyłane: TS `T | null`
        o.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;   // nieznane pole żądania → 422 WALIDACJA
        o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));   // UPPER_SNAKE; wyjątki [JsonStringEnumMemberName("R-SERIA")]
        o.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;          // LaTeX `<`, `>`, `&` bez sekwencji < (ADR-058 p. 4)
    }
}
// Program.cs: builder.Services.ConfigureHttpJsonOptions(o => JsonApi.Konfiguruj(o.SerializerOptions));
```

| Element | Ustalenie |
|---|---|
| Identyfikatory w kontraktach | `Guid` (UUID) i `int` (Działy, Umiejętności); typy silne `XxxId` z domeny są zamieniane w endpointach (OpenAPI nie zna typów `readonly record struct`) |
| Czas | `DateTimeOffset` w UTC (`2026-10-31T18:00:00Z`, `format: date-time`); data bez czasu — `DateOnly` (`format: date`) |
| Liczby dziesiętne | `decimal` jako liczba JSON (`0.95`), nigdy napis |
| Pola opcjonalne żądania | typ dopuszczający `null` z wartością domyślną w rekordzie; TS `pole?: T \| null` |
| Nagłówki żądania | `Authorization: Bearer <JWT>`, `Idempotency-Key: <uuid>` (operacje z listy 12.1), `If-Match: "<version>"` (operacje z kolumną ETag ≠ —), `If-None-Match` (GET zasobów wersjonowanych), `Last-Event-ID` (SSE), `X-Lemat-Mock-Scenariusz` (wyłącznie `Development`/`E2E`, ADR-065) |
| Nagłówki odpowiedzi | `ETag: "<version>"`, `Retry-After: <s>` (429, 503, `409 IDEMPOTENCJA_W_TOKU`), `Idempotent-Replayed: true`, `X-Lemat-Wersja-Api: 1.N.M`, `traceresponse` (W3C), `Deprecation`/`Sunset` (stara wersja główna, ADR-062 p. 1) |
| Odpowiedź stronicowana | `Strona<T> { elementy: T[], nastepny_kursor: string \| null }` (ADR-062 p. 5) |
| `Content-Type` | `application/json; charset=utf-8`; błędy `application/problem+json`; przesłanie zdjęć `multipart/form-data`; eksport danych `application/zip`; strumienie `text/event-stream` |

## 2. Model błędu

```csharp
// Lemat.Api.Kontrakty.Wspolne — schemat ProblemDetailsLemat w OpenAPI (ADR-060 p. 1)
public sealed record ProblemDetailsLemat(
    string Type, string Title, int Status, string? Detail, string Instance,
    KodBledu Kod, string TraceId,
    IReadOnlyList<BladPola>? BledyPol,            // tylko WALIDACJA
    JsonObject? Szczegoly);                       // zależne od kodu (12.2, kolumna „szczegoly”)
public sealed record BladPola(string Pole, string KodReguly, string Komunikat);   // pole: "kroki[3].latex"

// Lemat.Domain.Wspolne.Bledy — kod HTTP z atrybutu członka; pełna lista = 12.2
public enum KodBledu
{
    [Http(422)] Walidacja, [Http(401)] Nieuwierzytelniony, [Http(401)] SesjaUniewazniona, [Http(403)] BrakUprawnien,
    [Http(403)] KontoNieaktywne, [Http(404)] NieZnaleziono, [Http(409)] RozwiazanieNieprawidlowyStan,
    [Http(409)] TranskrypcjaNieaktualna, [Http(412)] KonfliktWersji, [Http(428)] WymaganyIfMatch,
    [Http(428)] WymaganyIdempotencyKey, [Http(409)] IdempotencjaWToku, [Http(422)] KluczIdempotencjiKonflikt,
    [Http(429)] LimitPrzekroczony, [Http(429)] ZaDuzoStrumieni, [Http(500)] BladWewnetrzny,
    [Http(503)] UslugaNiedostepna, [Http(503)] BazaNiedostepna,
    // … pozostałe kody z tabeli 12.2 (generator z katalog-bledow.yaml, test KatalogBledowKompletny)
}
```

```yaml
# backend/src/Lemat.Api/Bledy/katalog-bledow.yaml — jedno źródło KodBledu, OpenAPI x-lemat-kody-bledow i messages/pl-PL.json (ADR-060 p. 4)
- kod: ROZWIAZANIE_NIEPRAWIDLOWY_STAN
  http: 409
  e: [E-13]
  tytul: Nieprawidłowy stan rozwiązania
  akcja: Odśwież widok rozwiązania — mogło zostać zmienione na innym urządzeniu.
  szczegoly: { istniejace_rozwiazanie_id: uuid? }
- kod: KONFLIKT_WERSJI
  http: 412
  e: [E-15]
  tytul: Dane zmieniły się w międzyczasie
  akcja: Pokazujemy aktualną wersję. Sprawdź zmiany i zapisz ponownie.
  szczegoly: { aktualny: object, zmiany_tresci: string[]? }
```

```ts
// frontend/src/lib/api/bledy.ts — typ z OpenAPI + klasa błędu klienta
import type { components } from "./schema";
export type ProblemDetailsLemat = components["schemas"]["ProblemDetailsLemat"];
export type KodBledu = components["schemas"]["KodBledu"];
export class BladApi extends Error {
  constructor(
    public readonly status: number,
    public readonly kod: KodBledu | "BLAD_SIECI" | "PRZERWANO",
    public readonly problem: ProblemDetailsLemat | null,
    public readonly retryAfterS: number | null,
  ) { super(problem?.detail ?? problem?.title ?? kod); }
}
```

## 3. Rekordy C# kontraktów (źródło OpenAPI i typów TypeScript)

Rekordy są w `Lemat.Api.Kontrakty.<Modul>`; enumy (`StatusRozwiazania`,
`RodzajZadania`, `PowodWeryfikacji` …) pochodzą z `Lemat.Domain` (3.5).
Ograniczenia z 12.5 wyrażają walidatory FluentValidation (`<Rekord>Walidator`),
a atrybuty `[StringLength]`/`[Range]` są dodane tam, gdzie mają trafić do
OpenAPI (`maxLength`, `minimum`, `maximum`). Typ TypeScript każdego rekordu
to `components["schemas"]["<NazwaRekordu>"]`; aliasy w `typy.ts`
(`export type RozwiazanieWyjscie = components["schemas"]["RozwiazanieWyjscie"];`).

```csharp
// ===== Wspolne =====
public sealed record Strona<T>(IReadOnlyList<T> Elementy, string? NastepnyKursor);
public sealed record OdwolanieDzialu(int Id, string Nazwa);
public sealed record OdwolanieUmiejetnosci(int Id, string Kod, string Nazwa);
public sealed record OsobaSkrot(Guid Id, string Imie, string Nazwisko);

// ===== Konta =====
public sealed record RejestracjaWejscie(
    [property: StringLength(254)] string Email, [property: StringLength(128, MinimumLength = 10)] string Haslo,
    [property: StringLength(60, MinimumLength = 1)] string Imie, [property: StringLength(80, MinimumLength = 1)] string Nazwisko,
    RolaUzytkownika Rola, bool OswiadczenieWieku16, bool ZgodaRegulamin, string? KodZaproszenia = null);
public sealed record LogowanieWejscie(string Email, string Haslo);
public sealed record FlagiUzytkownika(bool WeryfikatorGlobalny, bool RecenzentTresci);
public sealed record UzytkownikSesji(Guid Id, RolaUzytkownika Rola, string Imie, FlagiUzytkownika Flagi);
public sealed record SesjaWyjscie(string AccessToken, DateTimeOffset WygasaAt, UzytkownikSesji Uzytkownik);
public sealed record RejestracjaNauczycielaWyjscie(StatusKonta Status);                 // 201 bez tokenu (OCZEKUJE_NA_ZATWIERDZENIE)
public sealed record ResetHaslaWejscie(string Email);
public sealed record PotwierdzenieResetuWejscie(string Token, [property: StringLength(128, MinimumLength = 10)] string NoweHaslo);
public sealed record KlasaUcznia(Guid Id, string Nazwa, string Nauczyciel);
public sealed record UczenWyjscie(Guid Id, string Email, string Imie, string Nazwisko, PoziomMatury? PoziomMatury,
    DateOnly? TerminEgzaminu, int? WybranyDzialId, bool ZgodaNaTrening, StanTestuDiagnostycznego TestDiagnostyczny,
    IReadOnlyList<KlasaUcznia> Klasy, int Version);
public enum StanTestuDiagnostycznego { Brak, WToku, Zakonczony, Pominiety, Niedostepny }
public sealed record ZmianaUczniaWejscie(PoziomMatury? PoziomMatury = null, DateOnly? TerminEgzaminu = null,
    int? WybranyDzialId = null, bool? ZgodaNaTrening = null);                          // PATCH: null = bez zmiany
public sealed record UsuniecieKontaWejscie(string Haslo);
public sealed record UzytkownikNaLiscie(Guid Id, string Email, string Imie, string Nazwisko, RolaUzytkownika Rola,
    StatusKonta Status, FlagiUzytkownika Flagi, short DeklarowaneMinutyWeryfikacji, int Version, DateTimeOffset CreatedAt);
public sealed record ZmianaUzytkownikaWejscie(StatusKonta? Status = null, bool? WeryfikatorGlobalny = null,
    bool? RecenzentTresci = null, [property: Range(0, 480)] short? DeklarowaneMinutyWeryfikacji = null);
public sealed record UsuniecieKontaPrzezAdministratoraWejscie([property: StringLength(1000, MinimumLength = 10)] string Powod);

// ===== Profil, Test diagnostyczny =====
public sealed record RezultatPozycji(bool CzyPoprawna, decimal Punkty, decimal PunktyMax);
public sealed record PozycjaTestuWyjscie(Guid Id, short Kolejnosc, ZadanieUczniaWyjscie Zadanie, bool OdpowiedzZapisana, RezultatPozycji? Rezultat);
public sealed record TestDiagnostycznyWyjscie(Guid Id, PoziomMatury PoziomMatury, StatusTestu Status,
    IReadOnlyList<PozycjaTestuWyjscie> Pozycje, IReadOnlyList<OdwolanieDzialu> PominieteDzialy,
    DateTimeOffset CreatedAt, DateTimeOffset? ZakonczonoAt);
public sealed record OdpowiedzTestuWejscie(WyborOdpowiedzi Wybor);
public sealed record ZapisOdpowiedziWyjscie(bool CzyZapisano);
public sealed record OpanowanieUmiejetnosciWyjscie(int Id, string Kod, string Nazwa, decimal PoziomOpanowania,
    int LiczbaWynikow, DateTimeOffset? NastepnaPowtorkaAt);
public sealed record OpanowanieDzialuWyjscie(int Id, string Kod, string Nazwa, decimal PoziomOpanowania,
    IReadOnlyList<OpanowanieUmiejetnosciWyjscie> Umiejetnosci);
public sealed record ProfilUczniaWyjscie(Guid UczenId, PoziomMatury PoziomMatury, DateTimeOffset UpdatedAt,
    int OczekujaceWyniki, IReadOnlyList<OpanowanieDzialuWyjscie> Dzialy);

// ===== Treści =====
public sealed record ZalacznikWyjscie(string Url, string TekstAlternatywny);
public sealed record OpcjaWyjscie(string Id, string TrescMd);
public sealed record UmiejetnoscZadania(int Id, string Nazwa, bool Glowna);
public record ZadanieUczniaWyjscie(Guid Id, Guid ZadanieWersjaId, RodzajZadania Rodzaj, FormaZamknieta? FormaZamknieta,
    string TrescMd, IReadOnlyList<ZalacznikWyjscie> Zalaczniki, IReadOnlyList<OpcjaWyjscie>? Opcje,
    IReadOnlyList<OpcjaWyjscie>? Stwierdzenia, decimal PunktyMax, short PoziomTrudnosci,
    OdwolanieDzialu Dzial, IReadOnlyList<UmiejetnoscZadania> Umiejetnosci);
public sealed record KryteriumWyjscie(short Wariant, short NrKryterium, string Opis, decimal PunktyMax,
    bool CzyWynikKoncowy, short? WymagaNrKryterium);
public sealed record UprawnieniaZadania(bool Przeglad, bool Wycofanie, bool ZgloszenieBledu);
public sealed record WalidacjaZadaniaSkrot(RezultatWalidacji Rezultat, JsonObject Sprawdzenia);
public sealed record ArkuszZadania(string Kod, string NumerWArkuszu);
public sealed record ZadaniePelneWyjscie(Guid Id, Guid ZadanieWersjaId, RodzajZadania Rodzaj, FormaZamknieta? FormaZamknieta,
    string TrescMd, IReadOnlyList<ZalacznikWyjscie> Zalaczniki, IReadOnlyList<OpcjaWyjscie>? Opcje,
    IReadOnlyList<OpcjaWyjscie>? Stwierdzenia, decimal PunktyMax, short PoziomTrudnosci, OdwolanieDzialu Dzial,
    IReadOnlyList<UmiejetnoscZadania> Umiejetnosci,
    PochodzenieZadania Pochodzenie, ZasiegZadania Zasieg, StatusZadania Status, int NrWersji, int Version,
    short PoziomTrudnosciNominalny, int LiczbaWynikowKalibracji, JsonObject? OpcjeZamkniete, JsonObject RozwiazanieWzorcowe,
    JsonObject? WynikKoncowyWzorcowy, IReadOnlyList<KryteriumWyjscie> KryteriaOceniania, ArkuszZadania? ArkuszCke,
    WalidacjaZadaniaSkrot? Walidacja, bool ZweryfikowanePrzezNauczyciela, bool WstrzymaneWDoborze,
    int LiczbaOtwartychZgloszenBledu, UprawnieniaZadania Uprawnienia);
public sealed record ZadanieWejscie(RodzajZadania Rodzaj, FormaZamknieta? FormaZamknieta, int DzialId,
    IReadOnlyList<UmiejetnoscZadania> Umiejetnosci, [property: StringLength(8000)] string TrescMd,
    JsonObject? OpcjeZamkniete, JsonObject RozwiazanieWzorcowe, JsonObject? WynikKoncowyWzorcowy,
    IReadOnlyList<KryteriumWyjscie> KryteriaOceniania, [property: Range(1, 5)] short PoziomTrudnosciNominalny, PoziomMatury PoziomMatury);
public sealed record ZlecenieGenerowaniaWejscie(int UmiejetnoscId, [property: Range(1, 5)] short PoziomTrudnosci,
    PoziomMatury PoziomMatury, RodzajZadania Rodzaj);
public sealed record ZlecenieGenerowaniaPrzyjete(Guid ZlecenieGenerowaniaId);
public sealed record ZlecenieGenerowaniaWyjscie(Guid Id, StatusZlecenia Status, short LiczbaCykli, Guid? ZadanieId,
    WalidacjaZadaniaSkrot? Walidacja, string? Przyczyna, decimal KosztPln, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record PoprawkiZadania(string? TrescMd = null, JsonObject? OpcjeZamkniete = null, JsonObject? RozwiazanieWzorcowe = null,
    JsonObject? WynikKoncowyWzorcowy = null, IReadOnlyList<KryteriumWyjscie>? KryteriaOceniania = null, short? PoziomTrudnosciNominalny = null);
public sealed record PrzegladWejscie(DecyzjaPrzegladu Decyzja, [property: StringLength(1000)] string? Komentarz = null,
    ZasiegZadania? ZasiegPo = null, PoprawkiZadania? Poprawki = null);
public sealed record WycofanieWejscie([property: StringLength(1000, MinimumLength = 10)] string Powod);
public sealed record ZgloszenieBleduZadaniaWejscie([property: StringLength(1000, MinimumLength = 10)] string Opis, Guid ZadanieWersjaId);
public sealed record ZgloszenieBleduZadaniaWyjscie(Guid Id, Guid ZadanieId, Guid ZadanieWersjaId, string Opis,
    StatusZgloszeniaZadania Status, OsobaSkrot? Nauczyciel, string? KomentarzRozpatrzenia, DateTimeOffset CreatedAt, DateTimeOffset? RozpatrzonoAt);
public sealed record RozpatrzenieZgloszeniaWejscie(StatusZgloszeniaZadania Decyzja, [property: StringLength(1000, MinimumLength = 1)] string Komentarz,
    PrzegladWejscie? Przeglad, WycofanieWejscie? Wycofanie, bool RozpatrzWszystkieOtwarte);
public sealed record WkladkaWyjscie(Guid Id, int UmiejetnoscId, string Tytul, string TrescMd, short SzacowanyCzasMin,
    StatusWkladki Status, bool WygenerowanaAutomatycznie, int Version, DateTimeOffset UpdatedAt);
public sealed record WkladkaWejscie([property: StringLength(200, MinimumLength = 3)] string Tytul, [property: StringLength(20000)] string TrescMd,
    [property: Range(1, 60)] short SzacowanyCzasMin, StatusWkladki Status);
public sealed record SzkicWkladkiWejscie(int UmiejetnoscId);
public sealed record DzialWyjscie(int Id, string Kod, string Nazwa, PoziomMatury PoziomMatury);
public sealed record UmiejetnoscWyjscie(int Id, string Kod, string Nazwa, int DzialId, IReadOnlyList<int> WymaganeUmiejetnosci);
public sealed record DiagnostyczneWejscie(bool Diagnostyczne);
public sealed record PulaDiagnostycznaPozycja(int DzialId, PoziomMatury PoziomMatury, short PoziomTrudnosciNominalny, int Liczba);

// ===== Rekomendacje, Ścieżka nauki =====
public sealed record RekomendacjaSkrot(RegulaRekomendacji Regula, string Uzasadnienie, decimal? PrzewidywanePSukcesu);
public sealed record WkladkaSkrot(Guid Id, string Tytul, short SzacowanyCzasMin);
public sealed record PrzypisanieSkrot(Guid Id, DateTimeOffset Termin);
public sealed record UmiejetnoscElementu(int Id, string Kod, string Nazwa, OdwolanieDzialu Dzial);
public sealed record ElementSciezkiWyjscie(Guid Id, TypElementuSciezki Typ, CelElementu Cel, bool WSerii,
    StatusElementuSciezki Status, UmiejetnoscElementu Umiejetnosc, ZadanieUczniaWyjscie? Zadanie,
    WkladkaSkrot? WkladkaTeoretyczna, PrzypisanieSkrot? Przypisanie, RekomendacjaSkrot Rekomendacja,
    DateTimeOffset WygasaAt, DateTimeOffset CreatedAt, DateTimeOffset? RozpoczetoAt);
public sealed record BrakElementu(string Kod, string Komunikat, IReadOnlyList<OdwolanieDzialu> SugerowaneDzialy);   // Kod = "BRAK_ZADAN_W_ZAKRESIE"
public sealed record RekomendacjaWyjscie(ElementSciezkiWyjscie? Element, BrakElementu? BrakElementu);              // dokładnie jedno ≠ null

// ===== Rozwiązania =====
[JsonPolymorphic(TypeDiscriminatorPropertyName = "rodzaj")]
[JsonDerivedType(typeof(WyborOpcji), "OPCJA")] [JsonDerivedType(typeof(WyborStwierdzen), "STWIERDZENIA")]
public abstract record WyborOdpowiedzi;
public sealed record WyborOpcji([property: RegularExpression("^[A-D]$")] string Opcja) : WyborOdpowiedzi;
public sealed record OcenaStwierdzenia(string Id, bool Prawda);
public sealed record WyborStwierdzen([property: MinLength(2), MaxLength(4)] IReadOnlyList<OcenaStwierdzenia> Stwierdzenia) : WyborOdpowiedzi;
public sealed record OdpowiedzWejscie(Guid ZadanieWersjaId, Guid? ElementSciezkiId, Guid? PrzypisanieId, WyborOdpowiedzi Wybor);
public sealed record RezultatOdpowiedzi(bool CzyPoprawna, decimal Punkty, decimal PunktyMax, WyborOdpowiedzi Poprawna);
public sealed record OdpowiedzWyjscie(Guid Id, Guid? ElementSciezkiId, Guid? PrzypisanieId, DateTimeOffset RezultatUjawnionyOd, RezultatOdpowiedzi? Rezultat);
// multipart/form-data: pola tekstowe + pliki "zdjecia" (1..5, ≤ 10 MB, JPEG/PNG/HEIC/WEBP); wiązanie [FromForm]
public sealed record RozwiazanieWejscie(Guid ZadanieWersjaId, Guid? ElementSciezkiId, Guid? PrzypisanieId, IFormFileCollection Zdjecia);
public sealed record NiepewnyFragment(short Zdjecie, IReadOnlyList<int> Ramka, string Odczyt, IReadOnlyList<string> Alternatywy);   // ramka: [x, y, szer, wys]
public sealed record ZastapienieDanychOsobowych(int Od, int Do, RodzajZastapienia Rodzaj);
public enum RodzajZastapienia { Dokladne, Przyblizone }
public sealed record KrokTranskrypcjiWyjscie(short Nr, string Tekst, string Latex, decimal? PewnoscOdczytu,
    IReadOnlyList<NiepewnyFragment> NiepewneFragmenty, bool ZmienionyPrzezUcznia, short? NrKrokuAgenta, bool WymagaSprawdzenia,
    bool PodejrzenieDanychOsobowych, bool TylkoDaneOsobowe, IReadOnlyList<ZastapienieDanychOsobowych> ZastapieniaDanychOsobowych);
public sealed record ZakresPoprawekWyjscie(short Zmienione, short Wstawione, short KrokiAgenta, decimal OdsetekZnakow, bool LimitMiekkiPrzekroczony);
public sealed record ZgodnoscPoprawekWyjscie(IReadOnlyList<short> Niezgodne, DateTimeOffset SprawdzonoAt);
public sealed record TranskrypcjaWyjscie(Guid Id, short Wersja, ZrodloTranskrypcji Zrodlo, short PrzeslanieNr, decimal? ZgodnoscZZadaniem,
    IReadOnlyList<KrokTranskrypcjiWyjscie> Kroki, DateTimeOffset CreatedAt, ZakresPoprawekWyjscie? ZakresPoprawek,
    bool KonfliktFiltra, ZgodnoscPoprawekWyjscie? ZgodnoscPoprawek);                     // ZgodnoscPoprawek tylko N/A
public sealed record KrokTranskrypcjiWejscie(short? NrKrokuAgenta, [property: StringLength(2000)] string Tekst, [property: StringLength(2000)] string Latex);
public sealed record TranskrypcjaWejscie([property: MinLength(1), MaxLength(40)] IReadOnlyList<KrokTranskrypcjiWejscie> Kroki);
public sealed record ZatwierdzenieWejscie(Guid TranskrypcjaId, short Wersja);
public sealed record KryteriumOcenyWyjscie(short NrKryterium, string Opis, decimal? Punkty, decimal PunktyMax, decimal? Prawdopodobienstwo);
public sealed record OcenaWyjscie(Guid Id, short Wersja, StatusOceny Status, ZrodloOceny Zrodlo, WidocznoscDlaUcznia WidocznoscDlaUcznia,
    decimal? Punkty, decimal PunktyMax, short Wariant, IReadOnlyList<KryteriumOcenyWyjscie> Kryteria, short? PierwszyBlednyKrok,
    bool BrakujacyKrok, TypBledu? TypBledu, short? KrokPodejrzanyOdczyt, bool MozliwyBladOdczytu, string? Wskazowka,
    bool WskazowkaWPrzygotowaniu, string Uzasadnienie, decimal? PewnoscOceny, string? Komunikat, DateTimeOffset? MozliweZgloszenieDo,
    DateTimeOffset CreatedAt);
public enum WidocznoscDlaUcznia { Pelna, WstrzymanaBladOdczytu }
public sealed record WeryfikacjaUcznia(StatusWeryfikacji Status, IReadOnlyList<PowodWeryfikacji> Powody, DateTimeOffset TerminAt, bool Opozniona);
public sealed record ZdjecieRozwiazaniaWyjscie(Guid Id, short PrzeslanieNr, short Kolejnosc, bool Dostepne);
public sealed record RozwiazanieWyjscie(Guid Id, Guid ZadanieId, Guid ZadanieWersjaId, StatusRozwiazania Status,
    Guid? ElementSciezkiId, Guid? PrzypisanieId, IReadOnlyList<ZdjecieRozwiazaniaWyjscie> Zdjecia, short LiczbaPrzeslan,
    short LiczbaPonowienRozpoznania, KodBleduRozpoznawania? KodBleduRozpoznawania, TranskrypcjaWyjscie? Transkrypcja,
    OcenaWyjscie? Ocena, WeryfikacjaUcznia? Weryfikacja, int Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    int? NastepneOdpytanieMs);                                                           // 1000 w stanach przejściowych, null w pozostałych
public sealed record AdresZdjeciaWyjscie(string Url, DateTimeOffset WygasaAt);
public enum WersjaZdjecia { Oryginalna, Maskowana }
public sealed record ZgloszenieBleduOdczytuWejscie([property: MinLength(1), MaxLength(5)] IReadOnlyList<short> Kroki, [property: StringLength(500)] string? Opis = null);
public sealed record ZgloszenieBleduOdczytuWyjscie(WeryfikacjaUcznia Weryfikacja);

// ===== Weryfikacje =====
public sealed record ZadanieWeryfikacji(Guid Id, RodzajZadania Rodzaj, string Dzial, string? NumerWArkuszu);
public sealed record OcenaWeryfikowanaSkrot(decimal Punkty, decimal PunktyMax, decimal? PewnoscOceny);
public sealed record KlasaSkrot(Guid Id, string Nazwa, string? Nauczyciel = null);
public sealed record WeryfikacjaNaLiscie(Guid Id, Guid RozwiazanieId, OsobaSkrot? Uczen, string UczenPseudonim, KlasaSkrot? Klasa,
    ZadanieWeryfikacji Zadanie, IReadOnlyList<PowodWeryfikacji> Powody, short Priorytet, TypKolejkiWeryfikacji TypKolejki,
    StatusWeryfikacji Status, bool Dostepna, OsobaSkrot? PodjetaPrzez, DateTimeOffset? ZablokowanaDo, DateTimeOffset TerminAt,
    bool Opozniona, OcenaWeryfikowanaSkrot? OcenaWeryfikowana, DateTimeOffset CreatedAt);
public sealed record ZdjecieWeryfikacji(Guid Id, string Url, DateTimeOffset WygasaAt, bool Maskowane);
public sealed record PrawdopodobienstwoOpcji(string Opcja, decimal P);
public sealed record DecyzjaModeluWyjscie(string KodPytania, TypPytania TypPytania, JsonNode Wybor, IReadOnlyList<PrawdopodobienstwoOpcji> Rozklad);
public sealed record ZgloszenieWeryfikacji(IReadOnlyList<short> Kroki, string? Opis, DateTimeOffset ZgloszonoAt);
public sealed record WeryfikacjaSzczegoly(Guid Id, Guid RozwiazanieId, OsobaSkrot? Uczen, string UczenPseudonim, KlasaSkrot? Klasa,
    ZadanieWeryfikacji Zadanie, IReadOnlyList<PowodWeryfikacji> Powody, short Priorytet, TypKolejkiWeryfikacji TypKolejki,
    StatusWeryfikacji Status, bool Dostepna, OsobaSkrot? PodjetaPrzez, DateTimeOffset? ZablokowanaDo, DateTimeOffset TerminAt,
    bool Opozniona, OcenaWeryfikowanaSkrot? OcenaWeryfikowana, DateTimeOffset CreatedAt,       // pola WeryfikacjaNaLiscie powtórzone jawnie
    IReadOnlyList<PowodWeryfikacji> PowodyPoczatkowe, ZadaniePelneWyjscie ZadaniePelne, IReadOnlyList<ZdjecieWeryfikacji> Zdjecia,
    IReadOnlyList<TranskrypcjaWyjscie> Transkrypcje, OcenaWyjscie? Ocena, IReadOnlyList<DecyzjaModeluWyjscie> DecyzjeModelu,
    JsonObject? OpiniaPomocnicza, ZgloszenieWeryfikacji? Zgloszenie, SzkicRozstrzygnieciaWejscie? Szkic, int Version);
public sealed record PunktyKryterium(short NrKryterium, [property: Range(0, 10)] decimal Punkty);
[JsonPolymorphic(TypeDiscriminatorPropertyName = "tryb")]
[JsonDerivedType(typeof(WskazowkaWlasna), "WLASNA")] [JsonDerivedType(typeof(WskazowkaAutomatyczna), "AUTOMATYCZNA")]
[JsonDerivedType(typeof(WskazowkaZachowaj), "ZACHOWAJ")]
public abstract record WskazowkaRozstrzygniecia;
public sealed record WskazowkaWlasna([property: StringLength(400, MinimumLength = 1)] string Tekst) : WskazowkaRozstrzygniecia;
public sealed record WskazowkaAutomatyczna : WskazowkaRozstrzygniecia;
public sealed record WskazowkaZachowaj : WskazowkaRozstrzygniecia;
public sealed record RozstrzygniecieWejscie(short Wariant, IReadOnlyList<PunktyKryterium> Kryteria, short? PierwszyBlednyKrok,
    bool BrakujacyKrok, TypBledu? TypBledu, WskazowkaRozstrzygniecia? Wskazowka, IReadOnlyList<short> KrokiZBledemOdczytu,
    bool BladOdczytuWplynalNaOcene, [property: StringLength(500)] string? Komentarz = null);
public sealed record SzkicRozstrzygnieciaWejscie(short? Wariant = null, IReadOnlyList<PunktyKryterium>? Kryteria = null,
    short? PierwszyBlednyKrok = null, bool? BrakujacyKrok = null, TypBledu? TypBledu = null, WskazowkaRozstrzygniecia? Wskazowka = null,
    IReadOnlyList<short>? KrokiZBledemOdczytu = null, bool? BladOdczytuWplynalNaOcene = null, string? Komentarz = null);
public sealed record PrzedluzenieWyjscie(DateTimeOffset ZablokowanaDo, int Version);
public sealed record KonfliktTresciWeryfikacji(WeryfikacjaSzczegoly Aktualny, IReadOnlyList<string> ZmianyTresci);   // szczegoly 412 (8.3)

// ===== Klasy, Zestawy zadań, Przypisania =====
public sealed record KlasaWejscie([property: StringLength(80, MinimumLength = 1)] string Nazwa, PoziomMatury PoziomMatury);
public sealed record CzlonekKlasy(Guid UczenId, string Imie, string Nazwisko, DateTimeOffset DolaczylAt);
public sealed record KlasaWyjscie(Guid Id, string Nazwa, PoziomMatury PoziomMatury, bool Zarchiwizowana, string? KodZaproszenia,
    DateTimeOffset? KodWaznyDo, int LiczbaUczniow, IReadOnlyList<CzlonekKlasy>? Czlonkowie, int Version);   // Czlonkowie tylko w GET /klasy/{id}
public sealed record KodZaproszeniaWyjscie(string KodZaproszenia, DateTimeOffset KodWaznyDo, string Link);
public sealed record DolaczenieWejscie([property: RegularExpression("^[A-Z2-9]{8}$")] string KodZaproszenia);
public sealed record PrzekazanieKlasyWejscie(Guid NauczycielId);
public sealed record PozycjaZestawuWyjscie(short Kolejnosc, Guid ZadanieId, StatusZadania Status);
public sealed record ZestawZadanWejscie([property: StringLength(120, MinimumLength = 1)] string Nazwa, [property: MinLength(1), MaxLength(30)] IReadOnlyList<Guid> ZadanieIds);
public sealed record ZestawZadanWyjscie(Guid Id, string Nazwa, IReadOnlyList<PozycjaZestawuWyjscie> Zadania, int LiczbaPrzypisan, int Version);
public sealed record PrzypisanieWejscie(Guid ZestawZadanId, Guid? KlasaId, IReadOnlyList<Guid>? UczenIds, DateTimeOffset Termin);
public sealed record PrzypisanieWyjscie(Guid Id, OdwolanieZestawu Zestaw, KlasaSkrot? Klasa, DateTimeOffset Termin,
    int LiczbaUczniow, int LiczbaUkonczonych, DateTimeOffset CreatedAt);
public sealed record OdwolanieZestawu(Guid Id, string Nazwa);
public sealed record PozycjaPostepu(Guid ZadanieId, short Kolejnosc, StanPozycjiPostepu Stan, decimal? Punkty, decimal PunktyMax, DrogaProby? Droga);
public enum StanPozycjiPostepu { BrakProby, WToku, OczekujeNaWeryfikacje, Ocenione }
public enum DrogaProby { ListaPrzypisan, SciezkaNauki }
public sealed record PostepUcznia(Guid UczenId, string Imie, string Nazwisko, IReadOnlyList<PozycjaPostepu> Pozycje);
public sealed record PostepPrzypisaniaWyjscie(Guid PrzypisanieId, DateTimeOffset Termin, IReadOnlyList<PostepUcznia> Uczniowie);
public sealed record PrzypisanieUczniaWyjscie(Guid Id, string Nazwa, DateTimeOffset Termin, IReadOnlyList<PozycjaPrzypisaniaUcznia> Pozycje);
public sealed record PozycjaPrzypisaniaUcznia(short Kolejnosc, ZadanieUczniaWyjscie Zadanie, StanPozycjiPostepu Stan);
public sealed record OpanowanieDzialuSrednie(int DzialId, decimal PoziomOpanowaniaSredni);
public sealed record OpanowanieDzialuUcznia(int DzialId, decimal PoziomOpanowania);
public sealed record PostepUczniaKlasy(Guid UczenId, string Imie, string Nazwisko, DateTimeOffset? OstatniaAktywnosc,
    int LiczbaWynikow7d, decimal? SredniUlamek7d, IReadOnlyList<OpanowanieDzialuUcznia> Dzialy, int OtwarteWeryfikacje);
public sealed record PostepyKlasyWyjscie(Guid KlasaId, IReadOnlyList<OpanowanieDzialuSrednie> DzialySrednio, IReadOnlyList<PostepUczniaKlasy> Uczniowie);

// ===== Powiadomienia =====
public sealed record PowiadomienieWyjscie(Guid Id, TypPowiadomienia Typ, JsonObject Dane, DateTimeOffset? PrzeczytaneAt, DateTimeOffset CreatedAt);
public sealed record MigawkaPowiadomien(IReadOnlyList<PowiadomienieWyjscie> Nieprzeczytane, int LiczbaNieprzeczytanych);

// ===== Administracja (modele, kalibracja, importy, prace) =====
public sealed record ImportPrzyjety(Guid ImportId);
public sealed record ImportArkuszaWyjscie(Guid Id, StatusImportu Status, string? KodArkusza, JsonObject? Raport, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record KonfiguracjaModeluWejscie(RolaModelu Rola, [property: RegularExpression("^(typesafe-jev|zapasowy-llm|azure-openai|openai-zgodny|mock)$")] string Dostawca,
    [property: StringLength(120)] string Model, [property: StringLength(60)] string WersjaPromptu, JsonObject Parametry);
public sealed record KonfiguracjaModeluWyjscie(Guid Id, RolaModelu Rola, string Dostawca, string Model, string WersjaPromptu,
    JsonObject Parametry, bool Aktywna, DateTimeOffset CreatedAt, DateTimeOffset? AktywowanoAt);
public sealed record ProgPewnosciWejscie(Guid KonfiguracjaModeluId, RodzajZadania RodzajZadania, [property: Range(0.5, 1.0)] decimal Wartosc,
    [property: StringLength(1000, MinimumLength = 10)] string Uzasadnienie);
public sealed record ProgPewnosciWyjscie(Guid Id, Guid KonfiguracjaModeluId, RodzajZadania RodzajZadania, decimal Wartosc, ZrodloProgu Zrodlo,
    bool Aktywny, Guid? KalibratorPewnosciId, string? Uzasadnienie, DateTimeOffset CreatedAt);
public sealed record KoszykNiezawodnosci(IReadOnlyList<decimal> Koszyk, decimal SredniaPewnosc, decimal Trafnosc, int N);
public sealed record PozycjaKalibracji(Guid KonfiguracjaModeluId, RolaModelu Rola, RodzajZadania RodzajZadania, decimal Prog,
    bool TrybPelnejWeryfikacji, decimal? Ece, int LiczbaProbek90d, decimal OdsetekAutomatycznych30d, decimal? BladAutomatycznych30d,
    decimal? GornaGranicaBledu30d, decimal? ZgodnoscZNauczycielem30d, decimal? WykonanieKontroli30d, decimal? BledyOdczytuNiewykryte30d,
    IReadOnlyList<KoszykNiezawodnosci> WykresNiezawodnosci);
public sealed record MetrykiKalibracjiWyjscie(IReadOnlyList<PozycjaKalibracji> Pozycje);
public sealed record StanBezpiecznikaWyjscie(Guid KonfiguracjaModeluId, RodzajZadania RodzajZadania, TypBezpiecznika Typ, bool Aktywny,
    DateTimeOffset OknoOd, decimal? NEff, decimal? BladIpw, decimal? DolnaGranicaBledu, decimal? GornaGranicaBledu,
    DateTimeOffset? OstatnieWyzwolenieAt, DateTimeOffset? WyjscieZBlokadyAt, DateTimeOffset? DezaktywowanoAt,
    string? UzasadnienieDezaktywacji, decimal? ProgAktywny, int Version);
public sealed record DezaktywacjaBezpiecznikaWejscie(Guid KonfiguracjaModeluId, RodzajZadania RodzajZadania, TypBezpiecznika Typ,
    [property: StringLength(1000, MinimumLength = 10)] string Uzasadnienie);
public sealed record ParaKolejek(decimal Klasa, decimal Globalna);
public sealed record KlasaZKontrolaPrzyjec(Guid KlasaId, decimal OdsetekEskalacji7d);
public sealed record ObciazenieWeryfikacjiWyjscie(ParaKolejek PrognozaMinDziennie, ParaKolejek ZalegloscMin, decimal PojemnoscGlobalnaMin,
    int WeryfikatorzyGlobalniAktywni7d, int WymaganiWeryfikatorzyGlobalni, decimal OdsetekEskalacji7d, decimal NaplywEskalacjiMin7d,
    IReadOnlyList<KlasaZKontrolaPrzyjec> KlasyZKontrolaPrzyjec, bool KontrolaPrzyjecAktywna, ParaKolejek PoSla);
public sealed record PracaWTleWyjscie(long Id, TypPracy Typ, PulaPracy Pula, StatusPracy Status, short Proby, short MaksProb,
    short Priorytet, DateTimeOffset DostepnaOd, string? OstatniBlad, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record PrzebiegHarmonogramuWyjscie(string Nazwa, DateTimeOffset OstatnieWystapienie, DateTimeOffset ZleconoAt, string Instancja);
public sealed record PracaPrzyjeta(long PracaId);
```

Rekordy `WeryfikacjaSzczegoly` i `ZadaniePelneWyjscie` powtarzają jawnie
pola rekordów `WeryfikacjaNaLiscie` i `ZadanieUczniaWyjscie` (wersja 2.4:
`extends`), bo dziedziczenie rekordów daje w OpenAPI `allOf`, a wymagane
jest płaskie JSON zgodne z 12.6; test `PolaRekordowSzczegolowZawierajaPolaListy`
pilnuje, że zbiór pól rekordu szczegółów zawiera zbiór pól rekordu
bazowego z tymi samymi typami.

## 4. Endpointy — przykład grupy z filtrami (Rozwiązania)

```csharp
// Lemat.Api/Endpointy/RozwiazaniaEndpointy.cs
public static class RozwiazaniaEndpointy
{
    public static RouteGroupBuilder MapujRozwiazania(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/rozwiazania").WithTags("Rozwiazania").RequireAuthorization("Zalogowany");

        g.MapPost("/", PrzeslijAsync).WithName("PrzeslijRozwiazanie")
         .RequireAuthorization("Uczen").Idempotentny().DisableAntiforgery()           // bez cookie sesji → brak CSRF (ADR-054)
         .WithMetadata(new RequestSizeLimitAttribute(52_428_800))
         .Accepts<RozwiazanieWejscie>("multipart/form-data")
         .Produces<RozwiazanieWyjscie>(202)
         .KodyBledow(KodBledu.Walidacja, KodBledu.ZaDuzoZdjec, KodBledu.ZdjecieZaDuze, KodBledu.NieobslugiwanyFormat,
                     KodBledu.ZdjecieNieprawidlowe, KodBledu.ElementSciezkiNieaktywny, KodBledu.WersjaZadaniaNieaktualna,
                     KodBledu.ZadanieNiedostepne, KodBledu.PrzypisaniePoTerminie, KodBledu.RozwiazanieNieprawidlowyStan,
                     KodBledu.LimitPrzekroczony, KodBledu.UslugaNiedostepna);

        g.MapGet("/{id:guid}", PobierzAsync).WithName("PobierzRozwiazanie")
         .Produces<RozwiazanieWyjscie>().Produces(304).ZwracaETag(ZasobWersjonowany.R).KodyBledow(KodBledu.NieZnaleziono);

        g.MapGet("/{id:guid}/strumien", StrumienAsync).WithName("StrumienRozwiazania")
         .RequireAuthorization("Uczen").Zdarzenia<RozwiazanieWyjscie>("stan", "koniec")                  // x-lemat-zdarzenia (ADR-061 p. 3)
         .KodyBledow(KodBledu.NieZnaleziono, KodBledu.ZaDuzoStrumieni);

        g.MapPut("/{id:guid}/transkrypcja", PoprawTranskrypcjeAsync).WithName("PoprawTranskrypcje")
         .RequireAuthorization("Uczen").WymagaIfMatch(ZasobWersjonowany.R).ZWalidacja<TranskrypcjaWejscie>()
         .Produces<RozwiazanieWyjscie>()
         .KodyBledow(KodBledu.Walidacja, KodBledu.PoprawkiZbytRozlegle, KodBledu.RozwiazanieNieprawidlowyStan, KodBledu.KonfliktWersji);

        g.MapPost("/{id:guid}/zatwierdzenie", ZatwierdzAsync).WithName("ZatwierdzTranskrypcje")
         .RequireAuthorization("Uczen").WymagaIfMatch(ZasobWersjonowany.R).ZWalidacja<ZatwierdzenieWejscie>()
         .Produces<RozwiazanieWyjscie>(202)
         .KodyBledow(KodBledu.RozwiazanieNieprawidlowyStan, KodBledu.TranskrypcjaNieaktualna, KodBledu.KonfliktWersji);
        // … pozostałe operacje grupy według katalogu 12.3
        return g;
    }

    static async Task<Results<Accepted<RozwiazanieWyjscie>, ProblemHttpResult>> PrzeslijAsync(
        [FromForm] RozwiazanieWejscie wejscie, Aktor aktor, IPrzeslijRozwiazanie przypadek, CancellationToken ct)
    {
        var wynik = await przypadek.WykonajAsync(aktor, wejscie.ZaPolecenie(), ct);    // wyjątki domenowe → ObslugaWyjatkowDomenowych
        return TypedResults.Accepted($"/api/v1/rozwiazania/{wynik.Id}", wynik.NaWyjscie(aktor));
    }

    static async Task<IResult> PoprawTranskrypcjeAsync(Guid id, TranskrypcjaWejscie wejscie, OczekiwanaWersja wersja,
        Aktor aktor, IPoprawTranskrypcje przypadek, HttpContext http, CancellationToken ct)
    {
        var r = await przypadek.WykonajAsync(aktor, new RozwiazanieId(id), wersja.Wartosc, wejscie.ZaPolecenie(), ct);
        http.Response.Headers.ETag = $"\"{r.Version}\"";
        return TypedResults.Ok(r);
    }
}
```

`Aktor` (identyfikator, rola, flagi z JWT) i `OczekiwanaWersja` (z
`If-Match`, wypełniana przez `FiltrWersji`) są wiązane przez
`BindAsync` (`IBindableFromHttpContext<T>`); metody rozszerzeń
`Idempotentny()`, `WymagaIfMatch()`, `ZwracaETag()`, `ZWalidacja<T>()`,
`KodyBledow()`, `Zdarzenia<T>()` dodają filtry i metadane czytane przez
transformatory OpenAPI (ADR-061 p. 3). Endpointy nie mają logiki
domenowej: wywołują przypadek użycia z `Lemat.Application` (test
architektury `EndpointyTylkoPrzezAplikacje`).

## 5. Kontrakty SSE (ADR-064)

```text
GET /api/v1/rozwiazania/7c1e…/strumien
Authorization: Bearer eyJ…
Accept: text/event-stream
Last-Event-ID: 3fa1c09e5b2d7714            (opcjonalnie)

HTTP/2 200
content-type: text/event-stream
cache-control: no-cache,no-store
x-accel-buffering: no

event: stan
data: {"id":"7c1e…","status":"ROZPOZNAWANIE", … ,"version":3,"nastepne_odpytanie_ms":1000}
id: 5d0b7e19a4c2f001
retry: 3000

event: ping
data: {}

event: stan
data: {"id":"7c1e…","status":"TRANSKRYPCJA_GOTOWA", … ,"version":4,"nastepne_odpytanie_ms":null}
id: 91e4aa0c6f3b2d58

event: koniec
data: {"id":"7c1e…","status":"BLAD_ROZPOZNAWANIA", … }
id: c0a7…
```

| Strumień | `event:` | `data:` (schemat OpenAPI) | `id:` | Uwagi |
|---|---|---|---|---|
| Rozwiązanie | `stan`, `koniec` | `RozwiazanieWyjscie` (rola U) | skrót SHA-256 danych (16 znaków) | `koniec` w stanach terminalnych (ADR-064 p. 2); `TRANSKRYPCJA_GOTOWA` nie jest terminalny — strumień trwa w trakcie edycji Poprawek |
| Powiadomienia | `migawka` | `MigawkaPowiadomien` | `m` | pierwsze zdarzenie każdego połączenia |
| Powiadomienia | `powiadomienie` | `PowiadomienieWyjscie` | `powiadomienie.id` | klient scala po `id` |
| Zlecenie generowania | `stan`, `koniec` | `ZlecenieGenerowaniaWyjscie` | skrót danych | `koniec` przy `ZAKONCZONE`/`NIEUDANE` |
| Import arkusza | `stan`, `koniec` | `ImportArkuszaWyjscie` | skrót danych | `koniec` przy `ZAKONCZONY`/`ODRZUCONY` |
| każdy | `ping` | `{}` | — | co 15 s |
| każdy | `wznow` | `ZdarzenieWznow { powod: "WYGASNIECIE_TOKENU" \| "LIMIT_CZASU" \| "ZAMYKANIE_SERWERA" }` | — | serwer kończy odpowiedź po zdarzeniu |
| każdy | `blad` | `ProblemDetailsLemat` | — | błąd po rozpoczęciu strumienia; koniec bez ponowień przy `403`/`404` |

```csharp
// Lemat.Api/Strumienie/StrumienRozwiazania.cs — szkielet (TypedResults.ServerSentEvents, sprawdzone na 10.0.14)
static IResult StrumienAsync(Guid id, [FromHeader(Name = "Last-Event-ID")] string? ostatnie, Aktor aktor,
    IOdczytRozwiazan odczyt, KoncentratorZdarzen koncentrator, LimityStrumieni limity, TimeProvider czas, CancellationToken ct)
{
    if (!limity.TryZajmij(aktor.Id, ("ROZWIAZANIE", id), out var zajecie)) throw new LimitPrzekroczonyException(KodBledu.ZaDuzoStrumieni, TimeSpan.FromSeconds(30));
    return TypedResults.ServerSentEvents(Zdarzenia(), eventType: null);

    async IAsyncEnumerable<SseItem<object>> Zdarzenia()
    {
        using var _ = zajecie;
        await using var sub = koncentrator.Subskrybuj(TypZdarzenia.Rozwiazanie, id, pojemnosc: 16, PolitykaPrzepelnienia.OdrzucNajstarsze);
        var koniecPolaczenia = MinCzas(aktor.WygasaAt, czas.GetUtcNow().AddMinutes(10));
        var poprzedniSkrot = ostatnie;
        var pierwsze = true;
        while (!ct.IsCancellationRequested)
        {
            var stan = await odczyt.DlaUczniaAsync(aktor, new RozwiazanieId(id), ct)
                       ?? throw new NieZnalezionoException();                         // przed pierwszym zdarzeniem → 404 ProblemDetails
            var skrot = SkrotSse.Z(stan);
            var terminalny = stan.CzyTerminalnyDlaStrumienia();
            if (skrot != poprzedniSkrot || terminalny)
                yield return new SseItem<object>(stan, terminalny ? "koniec" : "stan")
                    { EventId = skrot, ReconnectionInterval = pierwsze ? TimeSpan.FromSeconds(3) : null };
            if (terminalny) yield break;
            pierwsze = false; poprzedniSkrot = skrot;
            var zdarzenie = await sub.CzekajAsync(okres: TimeSpan.FromSeconds(15), koniec: koniecPolaczenia, ct);
            if (zdarzenie == WynikCzekania.Ping) yield return new SseItem<object>(new { }, "ping");
            else if (zdarzenie == WynikCzekania.KoniecCzasu)
            { yield return new SseItem<object>(new ZdarzenieWznow(PowodWznowienia(aktor, czas)), "wznow"); yield break; }
        }
    }
}
```

## 6. Klient Next.js (`frontend/src/lib`)

### 6.1 `api/klient.ts` — jedyny klient HTTP

```ts
import createClient, { type Middleware } from "openapi-fetch";
import type { paths } from "./schema";
import { BladApi, type ProblemDetailsLemat } from "./bledy";
import { pobierzToken, odswiezToken } from "../auth/odswiezanie";

const naglowkiLemat: Middleware = {
  async onRequest({ request }) {
    const token = await pobierzToken();                       // z pamięci DostawcaSesji; odświeża 60 s przed exp (ADR-057)
    if (token) request.headers.set("Authorization", `Bearer ${token}`);
    request.headers.set("Accept", "application/json");
    return request;
  },
  async onResponse({ response }) {
    if (response.ok || response.status === 304) return response;
    const typ = response.headers.get("Content-Type") ?? "";
    const problem = typ.includes("application/problem+json") ? ((await response.clone().json()) as ProblemDetailsLemat) : null;
    const ra = response.headers.get("Retry-After");
    throw new BladApi(response.status, problem?.kod ?? "BLAD_WEWNETRZNY", problem, ra ? Number(ra) : null);
  },
  onError({ error }) {
    return error instanceof DOMException && error.name === "AbortError"
      ? new BladApi(0, "PRZERWANO", null, null)
      : new BladApi(0, "BLAD_SIECI", null, null);
  },
};

export const api = createClient<paths>({ baseUrl: "/", cache: "no-store", credentials: "same-origin" });
api.use(naglowkiLemat);

/** Wywołanie z jednym ponowieniem po 401 (wygasły token w trakcie). Ciało żądania jest odtwarzane przez ponowne
 *  wywołanie funkcji, więc działa także dla multipart i POST (Request z odczytanym strumieniem nie jest klonowany). */
export async function wywolaj<T>(operacja: () => Promise<{ data?: T }>): Promise<T> {
  try {
    return (await operacja()).data as T;
  } catch (e) {
    if (e instanceof BladApi && e.status === 401 && e.kod === "NIEUWIERZYTELNIONY" && (await odswiezToken())) {
      return (await operacja()).data as T;                   // drugi 401 albo SESJA_UNIEWAZNIONA → wylogowanie (DostawcaSesji)
    }
    throw e;
  }
}

/** Operacje modyfikujące z Idempotency-Key: klucz tworzony raz na intencję użytkownika (kliknięcie) i
 *  przechowywany w stanie formularza do sukcesu albo błędu 4xx — ponowienie po błędzie sieci używa tego samego klucza. */
export function nowyKluczIdempotencji(): string { return crypto.randomUUID(); }
export const ifMatch = (version: number) => `"${version}"`;
```

Przykład użycia (TanStack Query, moduł `features/rozwiazania`):

```ts
const zatwierdz = useMutation({
  mutationFn: (w: { id: string; version: number; transkrypcja_id: string; wersja: number }) =>
    wywolaj(() => api.POST("/api/v1/rozwiazania/{id}/zatwierdzenie", {
      params: { path: { id: w.id }, header: { "If-Match": ifMatch(w.version) } },
      body: { transkrypcja_id: w.transkrypcja_id, wersja: w.wersja },
    })),
  onError: (e) => obsluzBlad(e, { KONFLIKT_WERSJI: () => kolejka.invalidateQueries({ queryKey: ["rozwiazanie", id] }),
                                  TRANSKRYPCJA_NIEAKTUALNA: () => kolejka.invalidateQueries({ queryKey: ["rozwiazanie", id] }) }),
});
```

`obsluzBlad(e, reakcje)` (`lib/api/obsluzBlad.ts`) wywołuje reakcję dla
`e.kod`, a w pozostałych przypadkach pokazuje toast z `bledy.<kod>.tytul`
i `bledy.<kod>.akcja` (next-intl, 14.4); `5xx` i `BLAD_SIECI` w zapytaniach
odczytu przechodzą do najbliższego `error.tsx` (TanStack Query
`throwOnError: (e) => e instanceof BladApi && (e.status >= 500 || e.status === 0)`).

### 6.2 `auth/odswiezanie.ts` — single-flight (ADR-057)

```ts
const kanal = new BroadcastChannel("lemat-sesja");
let token: { wartosc: string; exp: number } | null = null;
let trwajace: Promise<string | null> | null = null;

export async function pobierzToken(): Promise<string | null> {
  if (token && token.exp - Date.now() > 60_000) return token.wartosc;
  return odswiezToken();
}

export function odswiezToken(): Promise<string | null> {
  if (trwajace) return trwajace;
  // lib.dom (TS 5.9) typuje request(...) z async callbackiem jako Promise<Promise<T>>; w czasie wykonania obietnica jest spłaszczona
  const p = navigator.locks.request("lemat-odswiezenie", { mode: "exclusive" }, async (): Promise<string | null> => {
    if (token && token.exp - Date.now() >= 60_000) return token.wartosc;          // dostarczony przez inną kartę w trakcie czekania
    const r = await fetch("/api/v1/auth/odswiezenie", { method: "POST", credentials: "same-origin", cache: "no-store" });
    if (!r.ok) { token = null; kanal.postMessage({ typ: "WYLOGOWANIE" }); return null; }    // 401 SESJA_UNIEWAZNIONA / brak cookie
    const s = (await r.json()) as { access_token: string; wygasa_at: string };
    token = { wartosc: s.access_token, exp: Date.parse(s.wygasa_at) };
    kanal.postMessage({ typ: "TOKEN", access_token: token.wartosc, exp: token.exp });
    return token.wartosc;
  }) as unknown as Promise<string | null>;
  trwajace = p.finally(() => { trwajace = null; });
  return trwajace;
}

kanal.onmessage = (m) => {
  if (m.data?.typ === "TOKEN") token = { wartosc: m.data.access_token, exp: m.data.exp };
  if (m.data?.typ === "WYLOGOWANIE") { token = null; window.location.assign("/logowanie"); }
  if (m.data?.typ === "PROSBA_O_TOKEN" && token) kanal.postMessage({ typ: "TOKEN", access_token: token.wartosc, exp: token.exp });
};
```

Przeglądarka bez `navigator.locks` (spoza A-10) wykonuje odświeżenie bez
blokady (`trwajace` chroni wyłącznie w obrębie karty), a serwer toleruje
ponowne użycie przez okno 30 s (ADR-057 p. 2).

### 6.3 `sse/strumien.ts`

```ts
import { createParser, type EventSourceMessage } from "eventsource-parser";
import { pobierzToken, odswiezToken } from "../auth/odswiezanie";
export type OpcjeStrumienia<Z> = {
  sciezka: string; onZdarzenie: (z: Z) => void; odpytaj: () => Promise<void>; okresOdpytywaniaMs: () => number;
};
export function otworzStrumien<Z extends { event: string }>(o: OpcjeStrumienia<Z>): () => void {
  let przerwij = new AbortController(), ostatnieId: string | undefined, odstepS = 1, porazki: number[] = [], zamkniety = false;
  let trybOdpytywania = false;
  let zegarCiszy: ReturnType<typeof setTimeout> | undefined;
  const odNowa = () => { clearTimeout(zegarCiszy); zegarCiszy = setTimeout(() => przerwij.abort("CISZA"), 45_000); };

  async function polacz(): Promise<void> {
    if (zamkniety) return;
    przerwij = new AbortController();
    let otrzymano = false;
    const token = await pobierzToken();
    const pierwszeZdarzenie = setTimeout(() => przerwij.abort("BUFOROWANIE"), 5_000);
    try {
      const r = await fetch(o.sciezka, { signal: przerwij.signal, cache: "no-store",
        headers: { Accept: "text/event-stream", Authorization: `Bearer ${token}`, ...(ostatnieId ? { "Last-Event-ID": ostatnieId } : {}) } });
      if (r.status === 401 && (await odswiezToken())) return polacz();
      if (r.status === 403 || r.status === 404) return;                             // koniec bez ponowień
      if (r.status === 429 || !r.ok || !r.body) return przejdzNaOdpytywanie();
      const parser = createParser({ onEvent: (m: EventSourceMessage) => {
        clearTimeout(pierwszeZdarzenie); odNowa(); odstepS = 1; otrzymano = true;
        if (m.id) ostatnieId = m.id;
        if (m.event === "ping") return;
        const z = { event: m.event ?? "message", ...JSON.parse(m.data) } as Z;
        o.onZdarzenie(z);
        if (m.event === "koniec") zamkniety = true;
      } });
      const czytnik = r.body.pipeThrough(new TextDecoderStream()).getReader();
      for (;;) { const { value, done } = await czytnik.read(); if (done) break; parser.feed(value); }
    } catch { /* abort, sieć */ } finally { clearTimeout(pierwszeZdarzenie); clearTimeout(zegarCiszy); }
    if (zamkniety) return;
    if (otrzymano && !przerwij.signal.aborted) { porazki = []; setTimeout(polacz, 0); return; }   // `wznow`: natychmiast
    porazki = [...porazki.filter((t) => Date.now() - t < 10_000), Date.now()];
    if (porazki.length >= 2 && przerwij.signal.reason !== "CISZA") return przejdzNaOdpytywanie();
    const opoznienie = odstepS * 1000 * (0.8 + Math.random() * 0.4);
    odstepS = Math.min(30, odstepS * 2);
    setTimeout(polacz, opoznienie);
  }

  async function przejdzNaOdpytywanie(): Promise<void> {
    trybOdpytywania = true;
    const start = Date.now();
    while (!zamkniety && trybOdpytywania) {
      await o.odpytaj();
      if (Date.now() - start > 60_000) { trybOdpytywania = false; return polacz(); }   // próba SSE co 60 s
      await new Promise((r) => setTimeout(r, o.okresOdpytywaniaMs()));
    }
  }

  void polacz();
  return () => { zamkniety = true; przerwij.abort("ZAMKNIECIE"); };
}
```

`pobierzToken`/`odswiezToken` pochodzą z `auth/odswiezanie.ts`. Hook
`useStrumienRozwiazania(id)` (moduł `rozwiazania`) zapisuje każde `stan` do
pamięci TanStack Query (`setQueryData(["rozwiazanie", id], …)`), a
`odpytaj` wywołuje `GET /rozwiazania/{id}`; `okresOdpytywaniaMs` czyta
`nastepne_odpytanie_ms` (null → koniec odpytywania). Karta ukryta > 5 min
zamyka strumień (`visibilitychange`), powrót otwiera go ponownie.

## 7. Mapowanie stron App Router na endpointy

Strony są komponentami serwerowymi bez danych (powłoka, teksty); dane
pobierają komponenty klienckie modułu `features/<modul>` przez `klient.ts`
(ADR-054 p. 4). Kolumna „Mutacje” wymienia operacje wywoływane z danej
strony.

| Trasa (`src/app`) | Moduł | Odczyt (GET / SSE) | Mutacje |
|---|---|---|---|
| `(publiczne)/logowanie` | `konta` | — | `POST /auth/logowanie` |
| `(publiczne)/rejestracja`, `rejestracja/nauczyciel` | `konta` | — | `POST /auth/rejestracja` (`?kod` → `kod_zaproszenia`) |
| `(publiczne)/reset-hasla`, `reset-hasla/potwierdzenie` | `konta` | — | `POST /auth/reset-hasla`, `POST /auth/reset-hasla/potwierdzenie` |
| `(publiczne)/dolacz/[kod]` | `klasy` | `POST /auth/odswiezenie` (sesja) | `POST /klasy/dolaczenie` |
| `(uczen)/start` | `nauka` | `GET /uczniowie/ja`, `GET /uczniowie/ja/rekomendacje/nastepna` | `POST /elementy-sciezki/{id}/rozpoczecie`, `…/pominiecie`, `POST /uczniowie/ja/test-diagnostyczny` |
| `(uczen)/test-diagnostyczny` | `profil` | `GET /testy-diagnostyczne/{id}` | `POST …/pozycje/{pozycja_testu_diagnostycznego_id}/odpowiedz`, `POST …/zakonczenie`, `POST …/pominiecie` |
| `(uczen)/sciezka-nauki` | `nauka` | `GET /uczniowie/ja/sciezka-nauki` (strona) | `POST /elementy-sciezki/{id}/pominiecie` |
| `(uczen)/wkladki-teoretyczne/[wkladkaId]` | `nauka` | `GET /wkladki-teoretyczne/{id}` | `POST /elementy-sciezki/{id}/przeczytanie` |
| `(uczen)/zadania/[zadanieId]` | `rozwiazania` | `GET /zadania/{id}` | `POST /zadania/{id}/odpowiedzi` (Idempotency-Key), `POST /rozwiazania` (multipart, Idempotency-Key) |
| `(uczen)/rozwiazania` | `rozwiazania` | `GET /rozwiazania?status&kursor` | — |
| `(uczen)/rozwiazania/[rozwiazanieId]` | `rozwiazania` | `GET /rozwiazania/{id}/strumien` (SSE), zapasowo `GET /rozwiazania/{id}`, `GET …/zdjecia/{zdjecie_rozwiazania_id}/adres`, `GET …/ocena?historia=true` | `PUT …/transkrypcja` (If-Match, autozapis z debounce 1,5 s), `POST …/zatwierdzenie` (If-Match), `POST …/zdjecia`, `POST …/ponowienie-rozpoznania`, `POST …/porzucenie`, `POST …/zgloszenie-bledu-odczytu` (Idempotency-Key) |
| `(uczen)/profil` | `profil` | `GET /uczniowie/{id}/profil` (`ja`), `GET /dzialy`, `GET /umiejetnosci` | — |
| `(uczen)/przypisania` | `przypisania` | `GET /uczniowie/ja/przypisania` | — |
| `(uczen)/klasy` | `klasy` | `GET /uczniowie/ja` | `POST /klasy/dolaczenie`, `DELETE /uczniowie/ja/klasy/{id}` |
| `(uczen)/ustawienia` | `konta` | `GET /uczniowie/ja` | `PATCH /uczniowie/ja` (If-Match), `GET /uczniowie/ja/eksport-danych`, `DELETE /uczniowie/ja` (Idempotency-Key) |
| `(nauczyciel)/nauczyciel` | `weryfikacje` | `GET /weryfikacje?dostepnosc=DOSTEPNE`, `GET /klasy` | — |
| `nauczyciel/klasy`, `klasy/[klasaId]` | `klasy` | `GET /klasy`, `GET /klasy/{id}`, `GET /klasy/{id}/postepy` | `POST /klasy`, `POST /klasy/{id}/kod-zaproszenia` (If-Match), `POST /klasy/{id}/archiwizacja` (If-Match), `DELETE /klasy/{id}/uczniowie/{uczen_id}` |
| `nauczyciel/klasy/[klasaId]/uczniowie/[uczenId]` | `klasy` | `GET /uczniowie/{id}/profil`, `GET /klasy/{id}/uczniowie/{uczen_id}/rozwiazania` | — |
| `nauczyciel/zestawy-zadan`, `zestawy-zadan/[zestawId]` | `przypisania` | `GET /zestawy-zadan`, `GET /zestawy-zadan/{id}`, `GET /zadania` | `POST /zestawy-zadan`, `PUT /zestawy-zadan/{id}` (If-Match), `DELETE /zestawy-zadan/{id}` (If-Match) |
| `nauczyciel/przypisania` | `przypisania` | `GET /przypisania`, `GET /przypisania/{id}/postep` | `POST /przypisania` (Idempotency-Key) |
| `nauczyciel/weryfikacje` | `weryfikacje` | `GET /weryfikacje?dostepnosc&powod&klasa_id&typ_kolejki&kursor` | `POST /weryfikacje/{id}/podjecie` |
| `nauczyciel/weryfikacje/[weryfikacjaId]` | `weryfikacje` | `GET /weryfikacje/{id}`, `GET /rozwiazania/{id}/zdjecia/{zdjecie_rozwiazania_id}/adres` | `POST …/podjecie`, `POST …/przedluzenie` (co 10 min przy aktywności), `PUT …/szkic` (debounce 3 s), `POST …/zwolnienie`, `POST …/rozstrzygniecie` (If-Match), `POST /rozwiazania/{id}/korekty` (If-Match) |
| `nauczyciel/zadania`, `zadania/nowe` | `tresci` | `GET /zadania` | `POST /zadania`, `POST /zadania/{id}/zgloszenia-bledu` (Idempotency-Key) |
| `nauczyciel/zadania/generowanie` | `tresci` | `GET /zlecenia-generowania/{id}/strumien` (SSE) | `POST /zadania/generowanie` (Idempotency-Key) |
| `nauczyciel/zadania/[zadanieId]/przeglad` | `tresci` | `GET /zadania/{id}` | `POST /zadania/{id}/przeglad` (If-Match), `POST /zadania/{id}/wycofanie` (If-Match) |
| `nauczyciel/zgloszenia-bledu-zadan` | `tresci` | `GET /zgloszenia-bledu-zadan` | `POST /zgloszenia-bledu-zadan/{id}/rozpatrzenie` (If-Match Z) |
| `nauczyciel/wkladki-teoretyczne`, `[wkladkaId]/przeglad` | `tresci` | `GET /wkladki-teoretyczne?status=DO_PRZEGLADU`, `GET /wkladki-teoretyczne/{id}` | `PUT /wkladki-teoretyczne/{id}` (If-Match) |
| `nauczyciel/ustawienia` | `konta` | — | `DELETE /nauczyciele/ja` (Idempotency-Key) |
| `(administrator)/admin/uzytkownicy`, `admin/klasy` | `administracja` | `GET /admin/uzytkownicy` | `PATCH /admin/uzytkownicy/{id}` (If-Match), `POST …/przywrocenie`, `POST …/usuniecie`, `POST /admin/klasy/{id}/przekazanie` (If-Match) |
| `admin/arkusze-cke`, `arkusze-cke/importy/[importId]` | `administracja` | `GET /admin/arkusze-cke/importy/{id}/strumien` (SSE), zapasowo `GET /admin/arkusze-cke/importy/{id}` | `POST /admin/arkusze-cke/importy` (multipart) |
| `admin/konfiguracje-modeli`, `progi-pewnosci`, `kalibracja`, `bezpieczniki` | `administracja` | `GET /admin/konfiguracje-modeli`, `GET /admin/progi-pewnosci`, `GET /admin/kalibracja`, `GET /admin/bezpieczniki` | `POST /admin/konfiguracje-modeli`, `POST …/{id}/aktywacja`, `POST …/{id}/ewaluacja-zt1`, `POST /admin/progi-pewnosci`, `POST /admin/bezpieczniki/dezaktywacja` (If-Match `B`) |
| `admin/obciazenie-weryfikacji`, `pula-diagnostyczna` | `administracja` | `GET /admin/obciazenie-weryfikacji`, `GET /admin/pula-diagnostyczna` | `PUT /admin/zadania/{id}/diagnostyczne` (If-Match) |
| `admin/zadania`, `zadania/[zadanieId]`, `[zadanieId]/przeglad`, `zgloszenia-bledu-zadan`, `zlecenia-generowania/*`, `wkladki-teoretyczne` | `tresci` | jak w grupie Nauczyciela + `GET /zlecenia-generowania/{id}` | jak w grupie Nauczyciela + `POST /admin/wkladki-teoretyczne/szkice` |
| `admin/praca-w-tle` | `administracja` | `GET /admin/praca-w-tle?status&typ&kursor`, `GET /admin/harmonogram` | `POST /admin/praca-w-tle/{id}/ponowienie` |
| każda strona po zalogowaniu (układ roli) | `powiadomienia` | `GET /powiadomienia/strumien` (SSE, karta-lider), zapasowo `GET /powiadomienia?nieprzeczytane=true` co 30 s | `POST /powiadomienia/{id}/przeczytanie`, `POST /auth/wylogowanie` |
