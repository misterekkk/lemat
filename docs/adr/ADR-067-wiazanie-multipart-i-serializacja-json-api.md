# ADR-067: Wiązanie `multipart/form-data` przez `[AsParameters]`, polityka nazw JSON `NazwyLemat` i błędy wiązania jako ProblemDetails

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.3, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 4, wersja 3.3) |
| Bramka G-0 | nie |
| Realizuje | C8-01, C8-08; uzupełnia ADR-049 p. 14, ADR-060 i ADR-061 p. 2 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.1, 12.2, 12.12; [`docs/spec/api.md`](../spec/api.md) sekcje 1, 2, 3, 4 |

## Status

Nowa (it. 4 pętli 3.x). Zastępuje zdanie „wiązanie `[FromForm]` rekordu
`RozwiazanieWejscie`” w ADR-061 p. 2 i regułę „nieznane pole → 422” z
`api.md` 1 wersji 3.2, która w ASP.NET Core 10.0.14 dawała `400` z pustym
ciałem.

## Kontekst

Krytyk uruchomił kontrakty wersji 3.2 na ASP.NET Core 10.0.14 (C8-01,
C8-08) i wykazał: (a) rekord pozycyjny z `IFormFileCollection` i
`[FromForm]` odrzuca każde żądanie (`400`, `FormDataMappingException`),
a binder dopasowuje pola formularza po nazwach CLR, podczas gdy schemat
OpenAPI ma nazwy `snake_case` — klient wygenerowany z OpenAPI wysłałby
Rozwiązanie bez Zadania; (b) `JsonNamingPolicy.SnakeCaseLower` daje
`oswiadczenie_wieku16`, a DDL i listing 12.4 mają `oswiadczenie_wieku_16`;
(c) unia z dyskryminatorem nie na pierwszej pozycji daje `500`; (d)
nieznane pole, zły typ i błędy wiązania dają `400` bez ciała ProblemDetails,
a `404`/`405` trasy — bez `kod`.

Architekt odtworzył każdy przypadek i sprawdził rozwiązanie w projekcie
weryfikacyjnym ASP.NET Core 10.0.14 + `Microsoft.AspNetCore.OpenApi`
10.0.14 (wyniki w „Konsekwencjach”).

## Decyzja

1. **Formularze `multipart/form-data` są klasami wiązanymi przez
   `[AsParameters]`**, z `[FromForm(Name = "<snake_case>")]` na każdej
   właściwości tekstowej i pojedynczym pliku, właściwościami
   `{ get; set; }` (bez `required` i `init`) i wartością `null` dla pól
   opcjonalnych:
   ```csharp
   public sealed class RozwiazanieWejscie
   {
       [FromForm(Name = "zadanie_wersja_id")]  public Guid ZadanieWersjaId { get; set; }
       [FromForm(Name = "element_sciezki_id")] public Guid? ElementSciezkiId { get; set; }
       [FromForm(Name = "przypisanie_id")]     public Guid? PrzypisanieId { get; set; }
       public IFormFileCollection Pliki { get; set; } = null!;          // wszystkie pliki formularza
       public IReadOnlyList<IFormFile> Zdjecia => Pliki.GetFiles("zdjecia");
   }
   // endpoint: ([AsParameters] RozwiazanieWejscie wejscie, …)
   ```
   Ograniczenia sprawdzone uruchomieniem: `IFormFileCollection` nie
   przyjmuje `FromForm(Name)` (`NotSupportedException` przy budowie
   endpointu), a `IReadOnlyList<IFormFile>` w klasie `[AsParameters]`
   kończy się błędem kompilacji wyrażenia (`variable 'Zdjecia_local' …
   not defined`). Dlatego pliki są wiązane jako cała kolekcja, a walidator
   `RozwiazanieWejscieWalidator` odrzuca plik pod inną nazwą niż
   `zdjecia` (`422 WALIDACJA`, `kod_reguly = PLIK_POZA_POLEM_ZDJECIA`).
   Ten sam wzorzec mają `ZdjeciaRozwiazaniaWejscie` (`POST
   /rozwiazania/{id}/zdjecia`) i `ImportArkuszaWejscie`
   (`[FromForm(Name = "plik")] IFormFile Plik` — pojedynczy plik przyjmuje
   `Name`, sprawdzone).
2. **Schemat OpenAPI formularza** dodaje `TransformatorFormularzy`
   (`IOpenApiDocumentTransformer` + `IOpenApiOperationTransformer`):
   dla endpointu z metadaną `SchematFormularza("<Nazwa>")` zastępuje
   wygenerowany schemat `multipart/form-data` odwołaniem
   `#/components/schemas/<Nazwa>` i buduje schemat z właściwości klasy:
   nazwy z `FromForm.Name`, `Guid` → `string/uuid`, `Guid?` →
   `["null","string"]/uuid`, pliki → `zdjecia: array (1..5) of string
   (format: binary)`, `required` = pola niedopuszczające `null`. Schematy
   `IFormFile` i `IFormFileCollection` są usuwane z `components`. Test
   `SchematFormularzaZgodnyZWiazaniem` porównuje nazwy pól schematu z
   `FromForm.Name` i z polityką `NazwyLemat` (dla właściwości tekstowych
   nazwa formularza = `NazwyLemat.ConvertName(nazwa CLR)`).
3. **Polityka nazw `NazwyLemat`** (właściwości JSON, wartości enumów,
   nazwy pól formularza, parametry zapytania):
   `JsonNamingPolicy.SnakeCaseLower` (dla enumów `SnakeCaseUpper`) z
   dodatkowym separatorem między literą a cyfrą — wyrażenie
   `(?<=[A-Za-z])(?=\d)` → `_`. Wynik: `OswiadczenieWieku16` →
   `oswiadczenie_wieku_16`, `LiczbaWynikow7d` → `liczba_wynikow_7d`,
   `WeryfikatorzyGlobalniAktywni7d` → `weryfikatorzy_globalni_aktywni_7d`,
   `NEff` → `n_eff` (bez zmian). Cyfra i następująca po niej mała litera
   tworzą jeden człon (`7d`, `30d`). Reguła jest tą samą regułą, którą
   ADR-049 p. 14 stosuje do kolumn, więc test
   `NazwyJsonZgodneZKolumnamiDdl` porównuje — dla każdego rekordu
   wyjściowego mapowanego 1:1 na tabelę (lista w teście) — nazwy JSON z
   nazwami kolumn katalogu PostgreSQL.
4. **Enumy.** `JsonStringEnumConverter(NazwyLemat.Upper,
   allowIntegerValues: false)`; enum, którego wartość w słowniku zawiera
   znak spoza `[A-Z0-9_]`, ma `[JsonStringEnumMemberName]` na **każdym**
   członku — dotyczy `RegulaRekomendacji` (12 reguł: `R-PRZYPISANIE`,
   `R-SERIA`, `R-TRUDNOSC_W_GORE` …). Test `EnumyZMyslnikiemMajaAtrybutNaKazdymCzlonku`.
5. **Unie z dyskryminatorem:** `AllowOutOfOrderMetadataProperties = true`
   (dyskryminator `rodzaj`/`tryb` w dowolnym miejscu obiektu, sprawdzone:
   `{"opcja":"B","rodzaj":"OPCJA"}` → `200`); brak dyskryminatora →
   `422 WALIDACJA` (`kod_reguly = BRAK_DYSKRYMINATORA`, `pole = wybor`),
   nieznana wartość → `422` (`TYP_NIEPRAWIDLOWY`).
6. **Błędy wiązania jako ProblemDetails.**
   `RouteHandlerOptions.ThrowOnBadRequest = true` (także poza
   `Development`), a `ObslugaBledowWiazania : IExceptionHandler`
   (zarejestrowany przed `ObslugaWyjatkowDomenowych`) tłumaczy:
   | Wyjątek | Odpowiedź |
   |---|---|
   | `BadHttpRequestException` z `JsonException`, którego przyczyną jest błąd czytnika (`JsonReaderException`: składnia, ciało przerwane, E-43) | `400 ZAPYTANIE_NIEPRAWIDLOWE` |
   | `BadHttpRequestException` z `JsonException` „could not be mapped” (`UnmappedMemberHandling.Disallow`) | `422 WALIDACJA`, `bledy_pol[0] = {pole: "<ścieżka>.<nazwa>", kod_reguly: "POLE_NIEZNANE"}` |
   | `BadHttpRequestException` z innym `JsonException` (zły typ, zła wartość enumu) | `422 WALIDACJA`, `pole` = `JsonException.Path` bez `$.`, `kod_reguly: "TYP_NIEPRAWIDLOWY"` |
   | `BadHttpRequestException` bez `JsonException` (brak/zły typ pola formularza, nagłówka, parametru zapytania) | `422 WALIDACJA`, `pole` = `NazwyLemat(nazwa parametru)`, `kod_reguly: "WIAZANIE"` |
   | `BadHttpRequestException` `413` | `413 ZAPYTANIE_ZA_DUZE` |
   | `NotSupportedException` z „Path: $…” (brak dyskryminatora) | `422 WALIDACJA`, `kod_reguly: "BRAK_DYSKRYMINATORA"` |
7. **Odpowiedzi bez wyjątku** (`404` trasy, `405`, `415`) dostają ciało
   `application/problem+json` przez `UseStatusCodePages()` +
   `AddProblemDetails(o => o.CustomizeProblemDetails = …)`, które dopisuje
   `kod` (`404 NIE_ZNALEZIONO`, `405 METODA_NIEDOZWOLONA`, `415
   NIEOBSLUGIWANY_FORMAT`, `400 ZAPYTANIE_NIEPRAWIDLOWE`) i `trace_id`.
   Klient (`klient.ts`) dla odpowiedzi `!ok` bez `application/problem+json`
   (np. `502` z Caddy) ustawia `kod = "BLAD_BRAMY"` zamiast
   `BLAD_WEWNETRZNY`, aby rozróżnić awarię bramy od błędu aplikacji.

## Alternatywy

- **Parametry `[FromForm(Name)]` w sygnaturze handlera** (wariant
  działający w teście krytyka) — odrzucona: brak nazwanego schematu
  `RozwiazanieWejscie` w OpenAPI i długie sygnatury; `[AsParameters]`
  daje ten sam mechanizm wiązania z jedną klasą.
- **`[JsonPropertyName]` na każdym polu z cyfrą** — odrzucona: ≥ 14 pól
  i ryzyko pominięcia nowego pola; reguła w polityce obejmuje wszystkie.
- **Nazwy formularza PascalCase** — odrzucona: łamie ADR-049 (JSON i
  formularze `snake_case`).

## Konsekwencje

- Sprawdzone uruchomieniem (ASP.NET Core 10.0.14): formularz z
  `zadanie_wersja_id` + `element_sciezki_id` albo `przypisanie_id` +
  `zdjecia` → `202`; brak kontekstu → `422` (`DOKLADNIE_JEDEN_KONTEKST`);
  brak `zadanie_wersja_id`, zły UUID, nazwy PascalCase → `422`
  (`pole = zadanie_wersja_id`, `WIAZANIE`); plik pod inną nazwą → `422`;
  0 zdjęć → `422 ZA_DUZO_ZDJEC`; `GET` rekordu z polami
  `OswiadczenieWieku16`, `LiczbaWynikow7d`, `SredniUlamek7d`,
  `LiczbaProbek90d`, `OdsetekAutomatycznych30d`, `OdsetekEskalacji7d`,
  `WeryfikatorzyGlobalniAktywni7d`, `NEff` → nazwy z separatorem cyfr w
  JSON i w `required` OpenAPI; dyskryminator na końcu → `200`; brak
  dyskryminatora, nieznane pole, zły typ → `422` z `bledy_pol`; ucięty
  JSON → `400 ZAPYTANIE_NIEPRAWIDLOWE`; `404`/`405`/`415` →
  `application/problem+json` z `kod`; schemat OpenAPI `RozwiazanieWejscie`
  ma pola `zadanie_wersja_id`, `element_sciezki_id`, `przypisanie_id`,
  `zdjecia` (`format: binary`, 1..5).
- Test `Lemat.Api.Tests/Rozwiazania/PrzeslanieMultipartZKlientaOpenApi`
  buduje formularz według schematu z `lemat-v1.json` (nazwy pól z
  `components.schemas.RozwiazanieWejscie.properties`) i sprawdza trzy
  przypadki z poprzedniego punktu (element, Przypisanie, brak kontekstu →
  `422`); test `KazdaOdpowiedzBleduMaKod` wysyła niepoprawny JSON, nieznane
  pole, brak dyskryminatora, złą ścieżkę, złą metodę i zły
  `Content-Type` i wymaga `application/problem+json` z `kod` z katalogu.
- Kody `ZAPYTANIE_NIEPRAWIDLOWE` (400) i `ZASOB_USUNIETY` (410) oraz
  `BLAD_BRAMY` (tylko klient) są w katalogu 12.2.
