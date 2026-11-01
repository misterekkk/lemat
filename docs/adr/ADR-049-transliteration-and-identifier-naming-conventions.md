# ADR-049: Transliteracja i konwencje nazw identyfikatorów w C#, PostgreSQL, JSON, URL i TypeScript

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.0, 2026-11-01); uzupełnienie wersji 3.1 — sekcja „Uzupełnienia”; uzupełnienie 3.2 (p. 13–15); zmieniona w wersji 3.4 przez [ADR-075](ADR-075-english-file-and-directory-names.md) (nazwy plików, katalogów i przestrzeni nazw; p. 16–22) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 1, wersja 3.0) |
| Bramka G-0 | nie — rozszerza ADR-016, nie zmienia słownika |
| Rozszerza | ADR-016 (identyfikatory domenowe po polsku), 3.1 dokumentu głównego |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 3.1, 3.4, 3.5 |

## Status

Przyjęta.

## Kontekst

Z-6 i ADR-016 wymagają, aby nazwy w kodzie, bazie i API pochodziły ze
słownika pojęć. Wersja 2.4 ustaliła formy dla Pythona (snake_case w kodzie,
bazie i JSON). Stos .NET + Next.js wprowadza nowe artefakty: przestrzenie
nazw, typy, właściwości i członków enumów C#, nazwy projektów, typy i
zmienne TypeScript, segmenty tras App Router. Bez jednej reguły ten sam
pojęciowy identyfikator przyjąłby niezależne formy w każdej warstwie, a
test słownika (3.4) nie mógłby ich porównać. Krytyk zgłosił też niespójne
nazwy parametrów ścieżek (C5-05 p. 2) i wieloznaczność słowa „blokada”
(C5-04).

## Decyzja

1. **Transliteracja polskich znaków (jedyna dozwolona):** ą→a, ć→c, ę→e,
   ł→l, ń→n, ó→o, ś→s, ź→z, ż→z (wielkie litery analogicznie). Inne znaki
   spoza ASCII nie występują w identyfikatorach; test słownika sprawdza
   ASCII we wszystkich identyfikatorach C#, SQL, JSON i TS. Teksty UI
   (`messages/pl-PL.json`), komunikaty ProblemDetails i komentarze używają
   pełnej polszczyzny.
2. **Forma bazowa:** każdy identyfikator domenowy ma formę bazową
   `snake_case` — identyczną z nazwą tabeli/kolumny/typu w DDL 7.3 albo z
   identyfikatorem z tabel 3.2/3.3 (np. `zgloszenie_bledu_zadania`,
   `wstrzymanie_w_doborze`, `pewnosc_oceny`). Formy w innych artefaktach
   powstają **mechanicznie** z formy bazowej, więc mapowanie jest odwracalne:
   - `PascalCase`: każdy token rozdzielony `_` zaczyna się wielką literą,
     reszta małymi; tokeny jednoliterowe też (`wstrzymanie_w_doborze` →
     `WstrzymanieWDoborze`); cyfry przylegają do poprzedniego tokenu
     (`zt1` → `Zt1`, `sha256` → `Sha256`, `cv5` → `Cv5`); skróty są
     zwykłymi tokenami (`ArkuszCke`, `ZdarzenieRodo`, `JevAdapter`, nigdy
     `ArkuszCKE`).
   - Odwrotnie (`PascalCase` → `snake_case`): algorytm
     `JsonNamingPolicy.SnakeCaseLower` z .NET (granica przed wielką literą
     następującą po małej; przed ostatnią wielką literą ciągu wielkich liter,
     po której stoi mała), używany jednocześnie przez System.Text.Json
     (JSON API), konwencję nazw EF Core (`KonwencjaNazw`) i test słownika.
   - `UPPER_SNAKE` dla wartości wyliczeniowych:
     `JsonNamingPolicy.SnakeCaseUpper` (`NiepelneRozwiazanie` ↔
     `NIEPELNE_ROZWIAZANIE`, `EwaluacjaZt1` ↔ `EWALUACJA_ZT1`, `JevChoice` ↔
     `JEV_CHOICE`); te same nazwy w PostgreSQL (`MapEnum` z tłumaczem nazw
     Npgsql opartym na tym samym algorytmie) i JSON
     (`JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)`).
3. **Formy według artefaktu:**

   | Artefakt | Forma | Przykład |
   |---|---|---|
   | Tabele, kolumny, typy PostgreSQL | `snake_case` (forma bazowa) | `probka_kalibracyjna.zgodny_brakujacy_krok` |
   | Wartości typów wyliczeniowych PostgreSQL | `UPPER_SNAKE` | `'OTWARTE_Z_DOWODEM'` |
   | Indeksy, klucze obce, triggery | prefiksy `ux_`, `ix_`, `fk_`, `trg_` + `snake_case` | `ux_ocena_aktualna` |
   | Przestrzeń nazw C# | `Lemat.<Warstwa>.<Modul>[.<Podmodul>]`, `PascalCase` | `Lemat.Domain.Weryfikacje` |
   | Typ, rekord, enum, właściwość, metoda C# | `PascalCase` | `Weryfikacja.PowodyPoczatkowe` |
   | Członek enumu C# | `PascalCase` | `StatusOceny.Wstepna` |
   | Interfejs C# | `I` + `PascalCase` | `IModelDecyzyjny` |
   | Parametr, zmienna lokalna C# / TS | `camelCase` | `rozwiazanieId`, `terminMiekki` |
   | Pole prywatne C# | `_camelCase` | `_kryteria` |
   | Metoda asynchroniczna C# | przyrostek `Async` | `WykonajPlanAsync` |
   | Identyfikator silnie typowany C# | `<Korzen>Id` (`readonly record struct`) | `ZadanieWersjaId` |
   | Właściwości JSON (API, schematy odpowiedzi modeli, `ladunek` prac, `dane` zdarzeń) | `snake_case` | `pierwszy_bledny_krok` |
   | Wartości wyliczeniowe w JSON | `UPPER_SNAKE` | `"WSTRZYMANA_BLAD_ODCZYTU"` |
   | Nazwy schematów OpenAPI / DTO | `PascalCase` = nazwa rekordu C# | `OcenaWyjscie` |
   | Typy TypeScript | `PascalCase` = nazwa schematu OpenAPI | `type OcenaWyjscie` |
   | Właściwości obiektów TypeScript z API | `snake_case` (lustro JSON, bez warstwy mapowania) | `ocena.pewnosc_oceny` |
   | Komponenty React | `PascalCase`, plik `PascalCase.tsx` | `EdytorTranskrypcji.tsx` |
   | Pozostałe pliki TS | `kebab-case.ts` | `strumien.ts` |
   | Ścieżki URL API i segmenty tras Next.js | `kebab-case` | `/wkladki-teoretyczne`, `app/(uczen)/sciezka-nauki` |
   | Typy prac w tle, kody błędów, powody | `UPPER_SNAKE` (C#: członek enumu `PascalCase`) | `ROZPOZNAJ_PISMO` ↔ `TypPracy.RozpoznajPismo` |
   | Zmienne środowiskowe | `LEMAT_` + `UPPER_SNAKE` (nazwy z 22 bez zmian) | `LEMAT_REJESTRACJA_BEZ_KLASY` |
   | Klucze `appsettings.json` | sekcja `Lemat`, klucze `PascalCase` (wiązanie z `LematOpcje`) | `Lemat:RejestracjaBezKlasy` |
   | Metryki Prometheus | `snake_case` (nazwy z 17.3 bez zmian) | `jev_wywolania_http_total` |

4. **Kody z łącznikiem** (`N-1` … `N-6`, `Q-OC-02`, `R-SERIA`, `T-3`) nie są
   wartościami `UPPER_SNAKE`; w C# są członkami enumu `PascalCase`
   (`KodNiespojnosci.N1`, `RegulaRekomendacji.RSeria`) z atrybutem
   `[JsonStringEnumMemberName("N-1")]` (System.Text.Json, .NET 9+), a w bazie
   tekstem z CHECK (jak `rekomendacja.regula` w 7.3).
5. **Parametry ścieżek URL (C5-05 p. 2):** pierwszy parametr ścieżki, który
   identyfikuje zasób nazwany bezpośrednio poprzedzającym segmentem
   kolekcji, nazywa się `{id}` (`/rozwiazania/{id}/ocena`,
   `/uczniowie/{id}/profil`, `/uczniowie/ja/klasy/{id}`); każdy kolejny
   parametr nazywa się formą bazową klucza obcego zasobu, czyli
   `{<tabela>_id}` (`/klasy/{id}/uczniowie/{uczen_id}`,
   `/rozwiazania/{id}/zdjecia/{zdjecie_rozwiazania_id}`,
   `/testy-diagnostyczne/{id}/pozycje/{pozycja_testu_diagnostycznego_id}/odpowiedz`).
   Parametry zapytań: `snake_case` (`?wersja_zdjecia=`). Segmenty
   dynamiczne Next.js: `[<zasob>Id]` w `camelCase` (są zmiennymi TS).
6. **Przestrzenie nazw a nazwy klas:** przestrzeń nazw modułu nie może
   być równa nazwie klasy w niej zawartej (C# CS0118/niejednoznaczność),
   dlatego moduły mają formę liczby mnogiej albo czynności: `Weryfikacje`
   (klasa `Weryfikacja`), `Rozwiazania`, `Klasy`, `Rekomendacje`,
   `Ocenianie` (klasa `Ocena`), `Orkiestracja` (klasa `Orkiestrator`),
   `Kalibracja` (brak klasy `Kalibracja`), `Tresci`, `Konta`, `Profil`
   (klasa `ProfilUcznia`), `Modele`, `Powiadomienia`, `Audyt`, `Wspolne`.
7. **Nazwy projektów:** `Lemat.<Warstwa>` z technicznymi nazwami warstw po
   angielsku (`Domain`, `Application`, `Infrastructure`, `Api`, `Worker`,
   `Scheduler`, `ImageSandbox`, przyrostek `.Tests`), bo nie są pojęciami
   domenowymi; projekt, którego nazwą jest pojęcie słownika, używa pojęcia
   po polsku: `Lemat.Agenci` (pojęcie „Agent”), `Lemat.Modele` (pojęcia
   „Model System One”, „Model generatywny”, „Model decyzyjny”). Lista słów
   technicznych dopuszczonych w identyfikatorach jest zamknięta (3.1 p. 7).
8. **Konfiguracja:** zmienne `LEMAT_*` mają nazwy z 22; `LematOpcje`
   wiąże je przez `AddEnvironmentVariables(prefix: "LEMAT_")` i atrybuty
   `[ConfigurationKeyName("REJESTRACJA_BEZ_KLASY")]`, więc nazwy wersji 2.4
   działają bez zmian.
9. **Jedno słowo — jedno znaczenie dla „blokada” (C5-04):** dozwolone
   znaczenia domenowe to Blokada weryfikacji i Blokada obniżania progu;
   dzierżawa pracy w tle to `dzierzawa_do`; mechanizm PostgreSQL to
   „blokada doradcza PostgreSQL” wyłącznie w prozie. Token `blokad*` /
   `zablokow*` jest zarezerwowany z listą dozwolonych użyć (3.4, część 3).
10. **Weryfikacja:** test słownika (3.4) stosuje reguły 1–9 do schematu bazy,
    OpenAPI, schematów odpowiedzi modeli, typów C# (refleksja) i tras
    Next.js; tabela 3.5 jest źródłem prawdy dla mapowania pojęcie →
    identyfikator w każdej warstwie.

## Alternatywy

- **`camelCase` w JSON i TypeScript** (konwencja domyślna ASP.NET Core) —
  odrzucone: zmienia ~100 kontraktów 12.3/12.4 i klucze schematów
  odpowiedzi modeli, wymaga warstwy mapowania albo podwójnego słownika
  tokenów w teście, a frontend i tak powstaje od zera, więc nie zyskuje nic
  na konwencji domyślnej.
- **Angielskie nazwy w C# z mapowaniem na polskie kolumny** — odrzucone
  (łamie Z-6 i ADR-016; tłumaczenie między warstwami jest źródłem błędów).
- **Polskie nazwy warstw (`Lemat.Domena`, `Lemat.Aplikacja`,
  `Lemat.Infrastruktura`)** — odrzucone: warstwy nie są pojęciami
  słownika, a nazwy angielskie są rozpoznawalne dla narzędzi i nowych
  członków zespołu; wyjątek dla `Lemat.Agenci`/`Lemat.Modele` wynika z Z-6.
- **Paczka EFCore.NamingConventions** zamiast własnej konwencji —
  odrzucona: zależność od pakietu społeczności, który bywa opóźniony
  względem wydań EF Core; własna konwencja to jedna klasa używająca
  `JsonNamingPolicy.SnakeCaseLower`, co gwarantuje identyczny algorytm w
  JSON i bazie.

## Konsekwencje

- Każde pojęcie ma jednoznaczne formy we wszystkich warstwach (3.5), a test
  słownika porównuje je automatycznie.
- Kontrakty JSON i schemat bazy wersji 2.4 są zachowane bez zmian nazw (poza
  świadomymi zmianami 3.0: `praca_w_tle.dzierzawa_do`, parametry ścieżek).
- Kod C# zawiera polskie identyfikatory bez diakrytyków (np.
  `ZasadyPoprawekTranskrypcji`), co wymaga od zespołu stosowania tabeli 3.5
  zamiast intuicji językowej.

## Uzupełnienia

**Uzupełnienie wersji 3.1 (C6-12 p. 8, C6-13).**

11. **Przesłanianie przestrzeni nazw przez przestrzeń nazw.** Przestrzenie
    `Lemat.Domain.Modele`, `Lemat.Application.Modele`,
    `Lemat.Application.Agenci` mają tę samą ostatnią część co projekty
    `Lemat.Modele` i `Lemat.Agenci`. Kod nigdy nie używa częściowo
    kwalifikowanych nazw zaczynających się od `Modele.` ani `Agenci.`;
    typy z innej przestrzeni są importowane dyrektywą `using` z pełną
    nazwą przestrzeni (`using Lemat.Application.Modele;`), a przy kolizji
    nazwy typu — aliasem (`using PortyModeli = Lemat.Application.Modele;`).
    Test `BrakCzesciowejKwalifikacjiPrzestrzeni` (`Lemat.Architecture.Tests`)
    przegląda pliki `*.cs` wyrażeniem `(?<![\w.])(Modele|Agenci)\.[A-Z]`
    poza dyrektywami `using` i kończy się błędem przy trafieniu.
12. **Trzecie znaczenie „blokady” (zmiana p. 9).** Dozwolone znaczenia
    domenowe słowa „blokada” to: **Blokada weryfikacji**, **Blokada
    obniżania progu** i **blokada konta** (konto zablokowane przez
    Administratora: `status_konta = 'ZABLOKOWANE'`,
    `StatusKonta.Zablokowane`, `PowodEskalacji.NauczycielDocelowyZablokowany`,
    `NAUCZYCIEL_DOCELOWY_ZABLOKOWANY`, akcja audytu `KONTO_ZABLOKOWANE`). W
    prozie trzecie znaczenie ma zawsze kwalifikator „konta”. Przemianowanie
    na „zawieszenie” odrzucono, bo koliduje z pojęciem Zawieszenie Wyniku
    zadania (3.5 #80).

**Uzupełnienie wersji 3.2 (C7-15 p. 5, C7-06 p. 7, ADR-061).**

13. **Aliasy dla kolizji nazw typów z pakietów zewnętrznych.** W plikach,
    które importują jednocześnie `Microsoft.Extensions.AI` i
    `Microsoft.SemanticKernel` (kolizje `FunctionCallContent`,
    `FunctionResultContent`, `ChatMessageContent`) albo `ImageMagick` i
    `Microsoft.SemanticKernel` (`Kernel`), obowiązują aliasy w
    `GlobalUsings.cs` projektu: `global using FunctionCallContent =
    Microsoft.Extensions.AI.FunctionCallContent;`, `global using
    FunctionResultContent = Microsoft.Extensions.AI.FunctionResultContent;`,
    `global using SkKernel = Microsoft.SemanticKernel.Kernel;` (w
    `Lemat.Agenci`; typy SK o tych nazwach nie są używane). W
    `Lemat.ImageSandbox` nie ma Semantic Kernel, więc kolizja `Kernel` tam
    nie występuje. Sprawdzone kompilacją (.NET SDK 10.0.114).
14. **Liczby w nazwach.** Cyfra po słowie jest osobnym segmentem
    `snake_case`: `OswiadczenieWieku16` ↔ `oswiadczenie_wieku_16`; ponieważ
    `EFCore.NamingConventions` daje `oswiadczenie_wieku16`, każda taka
    właściwość ma `HasColumnName` (test `KolumnyZCyframiMajaHasColumnName`).
    Skróty z innych języków (`max`, `min`) są zakazane w identyfikatorach
    (`maks_prob`, nie `max_prob`); wyjątek: nazwy pól protokołów
    zewnętrznych (`max_tokens` w `konfiguracja_modelu.parametry`).
15. **Nazwy w kontraktach HTTP.** Identyfikator operacji OpenAPI
    (`WithName`) = `<Czasownik><Zasob>` w PascalCase po polsku
    (`PobierzRozwiazanie`, `ZatwierdzTranskrypcje`); nazwy schematów =
    nazwy rekordów C#; nazwy zdarzeń SSE — małe litery bez polskich znaków
    (`stan`, `koniec`, `migawka`, `powiadomienie`, `ping`, `wznow`,
    `blad`); nagłówki własne `X-Lemat-*` w PascalCase z łącznikami.

## Uzupełnienie wersji 3.3 (C8-08)

Reguła p. 14 (separator `_` między literą a cyfrą: `oswiadczenie_wieku_16`,
`liczba_wynikow_7d`) obowiązuje także w JSON i nazwach pól formularzy —
realizuje ją polityka `NazwyLemat` (ADR-067 p. 3); test
`NazwyJsonZgodneZKolumnamiDdl`. Pole słownikowe „Próg pewności” w kontraktach
ma nazwę `prog_pewnosci` (`PozycjaKalibracji.prog_pewnosci`,
`StanBezpiecznikaWyjscie.prog_pewnosci_aktywny`), nie `prog` (C8-12 p. 5).

## Uzupełnienie wersji 3.4 (ADR-075)

Wymaganie właściciela projektu „nazwy folderów oraz plików mają być po
angielsku” zmienia tę decyzję wyłącznie w zakresie **nazw plików,
katalogów i przestrzeni nazw**; reguły 1, 2, 4, 5, 8, 9, 12–15 i formy
identyfikatorów z tabeli p. 3 (typy, właściwości, enumy, kolumny, JSON,
ścieżki REST API, kody, zmienne `LEMAT_*`) obowiązują bez zmian.

16. **Tabela p. 3 — wiersze zmienione:**

    | Artefakt | Forma od wersji 3.4 | Przykład |
    |---|---|---|
    | Przestrzeń nazw C# | `Lemat.<Warstwa>.<Module>[.<Submodule>]`, segmenty angielskie z 6.11.1, równe katalogowi | `Lemat.Domain.Verifications` (typ `Weryfikacja`) |
    | Plik `.cs` | nazwa EN pojęcia z 3.5 + przyrostek roli (6.11.2) albo tłumaczenie z `file-names.yaml` | `Verification.cs` z klasą `Weryfikacja` |
    | Komponenty React | identyfikator `PascalCase` ze słownika, plik `PascalCase.tsx` z nazwą EN | `EdytorTranskrypcji` w `TranscriptionEditor.tsx` |
    | Pozostałe pliki TS | `kebab-case.ts`, angielskie | `stream.ts` |
    | Segmenty tras Next.js i adresy stron | `kebab-case`, angielskie; parametry dynamiczne `[<resource>Id]` w `camelCase` po angielsku | `app/(student)/learning-path`, `[solutionId]` |
    | Ścieżki URL API | `kebab-case` po polsku (bez zmian) | `/wkladki-teoretyczne` |

17. **Zmiana p. 5 (ostatnie zdanie).** Segmenty dynamiczne Next.js mają
    nazwy `[<resource>Id]` po angielsku (`[solutionId]`, `[classId]`), bo
    są nazwami katalogów; ich klucze w `params` są destrukturyzowane w
    `page.tsx` do zmiennych o nazwach ze słownika (`const { solutionId:
    rozwiazanieId } = await params;`) i są na zamkniętej liście wyjątków
    testu słownika (6.11.4).
18. **Zmiana p. 6.** Kolizja przestrzeni nazw z nazwą klasy nie może
    wystąpić, bo segmenty przestrzeni są angielskie, a typy polskie; lista
    polskich nazw modułów z p. 6 przestaje być nazwami przestrzeni i
    pozostaje nazwami modułów w prozie (6.2).
19. **Zmiana p. 7.** Wszystkie projekty mają nazwy angielskie:
    `Lemat.Agenci` → `Lemat.Agents`, `Lemat.Modele` → `Lemat.Models`,
    `Lemat.Narzedzia` → `Lemat.Tools`, `Lemat.Symulacje` →
    `Lemat.Simulations` (6.11.1). Wyjątek z p. 7 dla pojęć słownika w
    nazwach projektów przestaje obowiązywać.
20. **Zmiana p. 10.** Test słownika nie traktuje nazw plików, katalogów
    ani segmentów tras jako źródła identyfikatorów; nazwy plików sprawdza
    test `TestNazwPlikow` (ADR-075 p. 9).
21. **Zmiana p. 11.** Przestrzenie `Lemat.Domain.Models`,
    `Lemat.Application.Models`, `Lemat.Application.Agents` mają tę samą
    ostatnią część co projekty `Lemat.Models` i `Lemat.Agents`; zakaz
    częściowej kwalifikacji obowiązuje dla nowych nazw, a test
    `BrakCzesciowejKwalifikacjiPrzestrzeni` używa wyrażenia
    `(?<![\w.])(Models|Agents)\.[A-Z]` (np. `using PortyModeli =
    Lemat.Application.Models;`).
22. **Zakazane synonimy (3.1 p. 4)** nie dotyczą nazw plików i katalogów
    (`Student.cs`, `Grade.cs`, `review/` są dozwolone); dotyczą wyłącznie
    identyfikatorów i tekstów UI.
