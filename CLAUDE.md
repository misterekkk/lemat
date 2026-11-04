# CLAUDE.md — reguły projektu Lemat

Ten plik obowiązuje każdego dewelopera i każdego agenta (`developer`, `handoff-guardian`,
`architect`, `critic`, `planner` z `.claude/agents/`) w tym repozytorium. Przed pierwszą
zmianą przeczytaj także `HANDOFF.md` (stan, założenia, uproszczenia MVP, otwarte pytania),
`TASKS.md` (backlog) i `docs/CODEBASE_MAP.md` (żywa mapa kodu).

Planowanie architektury jest zakończone (`docs/ARCHITECTURE_DETAILED.md` v3.5). Teraz
**implementujemy: cel właściciela to działające MVP jak najszybciej**. Zakres MVP to zadania
z tagiem `[MVP]` w `TASKS.md`; zadania `[POST-MVP]` nie są wykonywane, dopóki MVP nie jest
kompletne end-to-end (chyba że właściciel zdecyduje inaczej).

**Priorytet właściciela: POC.** Do ukończenia POC obowiązuje wyłącznie sekcja „POC (priorytet właściciela)” w `TASKS.md` (TASK-121 … 140, ścieżki `[POC-A]` backend i `[POC-B]`
frontend, pracowane równolegle przez dwie osoby) oraz kontrakt HTTP `docs/POC_CONTRACT.md` (jedyny punkt styku ścieżek; zmiana kontraktu tylko procedurą z jego sekcji 10). Agent pracuje tylko w katalogach
swojej ścieżki, a reguły edycji `TASKS.md`/`HANDOFF.md`/`docs/CODEBASE_MAP.md` w POC opisuje `HANDOFF.md` sekcja 12 (zawężenie pkt 5.2). Uproszczenia POC to U-21 … U-31; pozostałe zadania `[MVP]` czekają na koniec POC.

## 1. Czym jest Lemat

Platforma przeglądarkowa (telefon i komputer) do nauki do matury z matematyki. **Uczeń**
dostaje od **Agenta rekomendującego** Zadania dobrane do swojego **Profilu ucznia**, a
**Rozwiązania** zadań otwartych przesyła jako zdjęcia kartek. **Agent rozpoznający** tworzy
**Transkrypcję** (Uczeń może ją poprawić), **Agent oceniający** (Model decyzyjny) wystawia
**Ocenę** z **Pewnością oceny**, a **Agent wyjaśniający** pisze **Wskazówkę**.
**Nauczyciel** prowadzi **Klasy** i rozstrzyga **Weryfikacje** Ocen o niskiej pewności.
Zasada nadrzędna Z-1: **„Jev decyduje, LLM pisze”** — punkty i automatyczne zaliczenie
pochodzą wyłącznie z Modelu decyzyjnego z zapisanym rozkładem; sygnały LLM działają tylko w
kierunku bezpiecznym (Z-3: przy wątpliwości lub awarii → Weryfikacja, nigdy automatyczne
zaliczenie; błąd odczytu nie obniża Oceny, FR-28).

## 2. Stos technologiczny (ADR-048 … ADR-075; nie zmieniaj bez nowego ADR)

| Obszar | Wybór |
|---|---|
| Backend | **.NET 10** (C# 14), ASP.NET Core Minimal APIs, EF Core 10 + Npgsql, FluentValidation, w `backend/` (solucja `backend/Lemat.slnx`) |
| Warstwa AI | Microsoft.Extensions.AI (`IChatClient`) + Semantic Kernel (`Microsoft.SemanticKernel.Core`/`.Yaml`, bez metapakietu `Microsoft.SemanticKernel`); Jev przez typowany `HttpClient` |
| Frontend | **Next.js (App Router) + React + TypeScript `strict`** w `frontend/`; Tailwind CSS, Radix UI, next-intl (`pl-PL`), TanStack Query, `openapi-fetch` + `openapi-typescript`, react-hook-form + zod, KaTeX; Vitest, Playwright, ESLint 9, Prettier |
| Baza | **PostgreSQL 16** (`citext`, `pg_trgm`); jedyne źródło prawdy **i** mechanizm prac w tle (`praca_w_tle`, `FOR UPDATE SKIP LOCKED`, `LISTEN/NOTIFY`) |
| Komunikacja | REST (`/api/v1`) + SSE dla postępu (ADR-051) z ścieżką zapasową: odpytywanie |
| Procesy | `Lemat.Api`, `Lemat.Worker`, `Lemat.Scheduler` (jeden obraz, wiele punktów wejścia) |
| Testy | xUnit v3, Shouldly, NSubstitute, FsCheck, Testcontainers (PostgreSQL 16), WireMock.Net, `WebApplicationFactory`; Vitest; Playwright |
| Schemat | DDL z `docs/spec/ddl.md` jest **jedynym źródłem schematu** (ADR-063): migracje EF Core wykonują wyłącznie pliki SQL; model EF Core nie generuje schematu |

Wersje: .NET SDK `10.0.x` (`backend/global.json`, `rollForward: latestPatch`), Node.js ≥ 22
(dokumentacja zakłada 24 LTS, lokalnie jest 22 — to wystarcza), npm (lockfile
`package-lock.json`; nie mieszaj z pnpm/yarn). Dokładne wersje pakietów przypina Central
Package Management (`backend/Directory.Packages.props`) i lockfile frontendu; zmiana wersji
głównej wymaga ADR. Środowisko lokalne: PostgreSQL 16 na porcie 5432, Docker.

## 3. Hierarchia dokumentów (od najważniejszego)

1. `docs/requirements.md` — FR-01 … FR-36, NFR-01 … NFR-10 (Z-2: wymagania > architektura).
2. `docs/glossary.md` + mapowanie 3.5 w `docs/ARCHITECTURE_DETAILED.md` — słownik pojęć (Z-6).
3. `docs/ARCHITECTURE_DETAILED.md` v3.5 — architektura. Plik ma ~10 000 linii: **czytaj tylko
   sekcje wskazane w polu „Realizuje” zadania** (`sed -n 'A,Bp'`). Przydatne punkty: 3.5
   (mapowanie słownika), 5 (kontenery, stos), 6.2/6.4/6.7/6.8/6.11 (moduły, projekty, drzewa
   plików, reguły nazw), 8 (maszyny stanów), 9 (przepływy), 10 (agenci), 11 (reguły oceniania),
   12.3 (katalog endpointów), 13 (porty i prace w tle), 15 (współbieżność), 18 (RTM),
   22 (parametry), 23.4 (decyzje D-1 … D-17).
4. `docs/spec/{ddl,ef-core,api,prompts}.md` — DDL, mapowanie EF Core, rekordy kontraktów i
   klient Next.js, prompty i schematy odpowiedzi modeli.
5. `docs/adr/ADR-001 … ADR-075` — uzasadnienia decyzji; obowiązujący stos: ADR-048,
   nazewnictwo: ADR-049 i **ADR-075**.
6. `docs/CRITIQUE_LOG.md`, `docs/ARCHITECTURE_CHANGELOG.md`, `docs/architecture.md`,
   `docs/vision.md` — kontekst; w razie sprzeczności wygrywa dokument wyżej.

**Pliki w `docs/` są tylko do odczytu** — jedyny wyjątek: `docs/CODEBASE_MAP.md` (żywa
mapa, utrzymuje ją `handoff-guardian`). Niejasność lub sprzeczność w architekturze zapisz w
`HANDOFF.md` → „Otwarte pytania”; nie rozstrzygaj jej po cichu w kodzie. Odstępstwa od
architektury dozwolone dla MVP są wyłącznie te z `HANDOFF.md` → „Uproszczenia MVP” (U-xx).

## 4. Słownik pojęć — TWARDY NAKAZ (ADR-016, ADR-049, ADR-075)

Słownik jest prawem (Z-6). Nazwy encji, tabel, kolumn, pól DTO, ścieżek REST, wartości
wyliczeniowych, typów prac w tle, zdarzeń, kodów błędów i kluczy schematów JSON modeli
**pochodzą ze słownika** (sekcja 3.5 architektury mapuje wszystkie 111 pojęć).

**A. Identyfikatory w kodzie — polskie, ASCII, wg słownika (ADR-049):**

| Forma | Użycie | Przykład (`Zgłoszenie błędu odczytu`) |
|---|---|---|
| `PascalCase` | typy, właściwości, metody C#; typy TS | `ZgloszenieBleduOdczytu` |
| `camelCase` | zmienne i parametry C#/TS | `zgloszenieBleduOdczytu` |
| `snake_case` | baza (tabele, kolumny), JSON API, klucze schematów modeli, parametry zapytań | `zgloszenie_bledu_odczytu` |
| `UPPER_SNAKE` | wartości wyliczeniowe (baza, JSON), typy prac w tle, kody błędów (C#: członek enumu `PascalCase`, np. `StatusOceny.Wstepna` ↔ `'WSTEPNA'`) | `ZGLOSZENIE_BLEDU_ODCZYTU` |
| `kebab-case` | segmenty URL i tras Next.js, nazwy plików TS niebędących komponentami | `zgloszenie-bledu-odczytu` |

Transliteracja: `ą→a ć→c ę→e ł→l ń→n ó→o ś→s ź→z ż→z` (`Uczeń` → `Uczen`). Teksty UI, komunikaty
błędów, prompty i proza dokumentów używają polskich znaków; **tylko identyfikatory są ASCII**.

**B. Nazwy plików i katalogów — angielskie, ASCII (ADR-075, 6.11):**

- Plik `.cs` z typem pojęcia ma nazwę `[I]<EN><Przyrostek>.cs`, gdzie `<EN>` to forma
  `PascalCase` z kolumny „Nazwa pliku/folderu (EN)” tabeli 3.5, a typ w środku zachowuje
  nazwę polską: `Rozwiazanie` → `Solution.cs`, `StatusRozwiazania` → `SolutionStatus.cs`,
  `RozwiazanieId` → `SolutionId.cs`, `OcenaWyjscie` → `GradeOutput.cs`, `IModelDecyzyjny` →
  `IDecisionModel.cs`, `AgentOceniajacy` → `GradingAgent.cs`. Jeden plik = jeden typ najwyższego
  poziomu; interfejs zawsze z `I`.
- Fasady i grupy endpointów biorą nazwę z modułu (nie z pojęcia): `ISolutionsFacade.cs`,
  `SolutionsEndpoints.cs`. Typ bez pojęcia w 3.5 dostaje angielskie tłumaczenie nazwy
  (`OgranicznikSzybkosci` → `RateLimiter.cs`, `KoncentratorZdarzen` → `EventHub.cs`).
- Przestrzeń nazw = ścieżka katalogu (IDE0130 jako błąd); segmenty angielskie wg tabeli 6.11.1:
  `Lemat.Domain.Grading.Ocena`, `Lemat.Application.Solutions.ISolutionsFacade`.
- Frontend: segmenty tras App Router i katalogi `src/features/*` angielskie `kebab-case`
  (`(student)/solutions/[solutionId]`, `(teacher)/teacher/verifications`); parametry dynamiczne
  `[solutionId]` mapuj od razu na zmienne ze słownika (`const { solutionId: rozwiazanieId }`).
  Komponenty React: plik `PascalCase.tsx` z angielską nazwą pojęcia (`TranscriptionEditor.tsx`),
  eksportowany komponent może mieć nazwę ze słownika (`EdytorTranskrypcji`).
- Pełne drzewa: 6.7 (`backend/`) i 6.8 (`frontend/`). Nie wymyślaj własnych katalogów —
  jeśli zadanie wymaga pliku spoza drzewa, dopisz go zgodnie z regułami i odnotuj w raporcie.

**C. Reguły znaczeniowe (3.1):**

1. **„zadanie” jest zarezerwowane dla pojęcia Zadanie.** Jednostka pracy asynchronicznej to
   **praca w tle** (`praca_w_tle`, `PracaWTle`, `TypPracy`) — nigdy „task”, „job”, „zadanie”
   w identyfikatorach domenowych. W prozie planu pozycje backlogu nazywamy **TASK-XXX**.
   Identyfikator `TASK-xxx` to numer planu — **nie używaj go w identyfikatorach, bazie ani API**
   (tylko w nagłówku `@Task` i w commitach).
2. „Żądanie” nie występuje w identyfikatorach: wniosek użytkownika = `wniosek`
   (`WNIOSEK_O_USUNIECIE`), żądanie HTTP do modelu = `wywolanie`.
3. **Zakazane synonimy w identyfikatorach:** `student`, `task`, `submission`, `grade`, `score`
   (poza typem pytania Jev `SCORE`), `difficulty`, `topic`, `skill`, `lesson`, `review`
   (jako weryfikacja). Nazwy **plików** mogą zawierać angielskie słowa (`Grade.cs`), klasa w
   środku nazywa się `Ocena`.
4. **Jedno słowo — jedno znaczenie:** odpowiedź modelu = `DecyzjaSkladowa`; rezultat operacji
   = `rezultat`; wynik porównania SymPy = `Rownowaznosc`; grupa A/B = `grupa eksperymentu`
   (nigdy „wariant”); pozycja Kryteriów = `kryterium` (`nr_kryterium`, nigdy „krok”);
   „zdjęcie” wyłącznie jako fotografia kartki; wyłączenie bezpiecznika = `dezaktywacja`;
   „wstrzymanie” tylko w znaczeniu z 3.1 p. 5; „Blokada” z kwalifikatorem (weryfikacji /
   obniżania progu / konta); worker ma **dzierżawę** pracy w tle, nie „blokadę”.
5. **Kolumny techniczne po angielsku i tylko te:** `id`, `created_at`, `updated_at`, `version`.
6. **Dozwolone terminy techniczne:** `theta`, `trudnosc_b`, `ipw`, `ece`, `n_eff`, `cv5`, `sha256`,
   `latex`, `sympy`, `jwt`, `url`, `mime`, `trace_id`, `hmac`, `fnv1a`, `http`, `rodo`; prefiksy
   `ux_`, `ix_`, `fk_`, `trg_`. Słowa techniczne stosu (`Handler`, `Adapter`, `Plugin`, `Mock`,
   `Fake`, `Tests`, `Exception`, `DbContext`, `Async` …) — 3.1 p. 7.
7. **Nowe pojęcie?** Nie wymyślaj nazwy. Sprawdź 3.2/3.3/3.5; jeśli pojęcia brak, zapisz
   propozycję (pojęcie, definicja, identyfikator, przyczyna) w `HANDOFF.md` → „Propozycje do
   słownika” i użyj nazwy tymczasowej tylko w obrębie jednego pliku, oznaczonej komentarzem
   `// SLOWNIK-PROPOZYCJA`.
8. **Strażnik:** `Lemat.Architecture.Tests/Glossary` (od TASK-002 lekki test zakazanych
   synonimów w identyfikatorach; pełny test słownika i manifest nazw plików — POST-MVP,
   TASK-008, TASK-009) musi przechodzić w `dotnet test`.

## 5. Protokół „Living Documentation” (OBOWIĄZKOWY)

### 5.1 Nagłówek w KAŻDYM pliku `.cs`, `.ts`, `.tsx`

Pierwsza linia pliku (przed `using`/`import`/`namespace`) to jednowierszowy komentarz:

```text
// @Role: <jedno zdanie po polsku: za co odpowiada plik> | @DependsOn: <typy/moduły/pliki, od których zależy> | @UsedBy: <kto go używa> | @Task: TASK-012
```

Przykłady:

```csharp
// @Role: Reguły trasowania T-1…T-6 do Weryfikacji (11.3) | @DependsOn: Ocena, PowodWeryfikacji, ProgPewnosci | @UsedBy: OcenRozwiazanieHandler, ZasadyOceniania | @Task: TASK-022
namespace Lemat.Domain.Grading;
```

```tsx
// @Role: Edytor kroków Transkrypcji z Poprawkami Ucznia | @DependsOn: lib/api/client, KaTeX Formula | @UsedBy: (student)/solutions/[solutionId]/page | @Task: TASK-087
"use client";
```

Reguły:

- Komentarz przed dyrektywą `"use client"` / `"use server"` jest dozwolony i wymagany.
- Dotyczy też testów, `Program.cs`, plików konfiguracyjnych `.ts` (`next.config.ts`,
  `vitest.config.ts`, `playwright.config.ts`). Wyjątek: pliki **generowane** (`schema.d.ts`,
  `*.g.cs`, `obj/`, `bin/`, `.next/`) — nie edytuj ich ręcznie i nie dodawaj nagłówka.
- Tworzysz plik → wpisz swój `TASK-XXX`. **Modyfikujesz** plik → dopisz kolejny identyfikator
  po przecinku (`@Task: TASK-012, TASK-045`) i uaktualnij `@Role/@DependsOn/@UsedBy`, jeśli się zmieniły.
- `@DependsOn`/`@UsedBy`: nazwy typów, modułów lub ścieżek względem projektu (maks. ~6 pozycji;
  dla długich list użyj nazwy modułu). `@UsedBy: (planowane TASK-XXX)` jest dozwolone, gdy
  konsument powstanie w późniejszym zadaniu.
- Jedna linia, bez łamania. `handoff-guardian` weryfikuje i poprawia nagłówki po każdym zadaniu.

### 5.2 Aktualizacja dokumentów po każdym TASK

Po zakończeniu zadania (w tym samym commicie):

| Dokument | Co aktualizować | Kto |
|---|---|---|
| `docs/CODEBASE_MAP.md` | wiersze tabel: strona/komponent Next.js ↔ endpoint .NET ↔ przypadek użycia ↔ Agent/plugin AI ↔ encja/tabela; rejestr plików; diagram Mermaid; status `planowane` → `gotowe` | `handoff-guardian` |
| `TASKS.md` | `[ ]` → `[x]`, pod zadaniem dokładne ścieżki utworzonych/zmodyfikowanych plików, wynik weryfikacji | `handoff-guardian` |
| `HANDOFF.md` | „Stan prac”, jak uruchomić, zmienne środowiskowe, odstępstwa/uproszczenia (U-xx), otwarte pytania, propozycje do słownika, sekcja „⚠️ Wymagane akcje od człowieka / Konfiguracja” | `handoff-guardian` |

`developer` **nie edytuje** tych trzech plików ani `docs/` — wpisuje wszystko do raportu
(lista plików, wyniki komend, decyzje, odstępstwa, mocki, wskazówki dla następnego TASK).
Gdy pracujesz samodzielnie (bez podziału na agentów), wykonaj obie role po kolei.

## 6. Mapa repozytorium (stan docelowy; aktualny stan: `docs/CODEBASE_MAP.md`)

```text
lemat/
├── CLAUDE.md  TASKS.md  HANDOFF.md  README.md
├── docker-compose.yml  .env.example        # PostgreSQL 16 lokalnie (TASK-005)
├── scripts/                                 # verify-backend.sh, verify-frontend.sh, verify-all.sh, dev-up.sh
├── .github/workflows/ci.yml
├── .claude/agents/                          # architect, critic, planner, developer, handoff-guardian
├── docs/                                    # TYLKO DO ODCZYTU (poza CODEBASE_MAP.md)
├── backend/                                 # 6.4, 6.7
│   ├── Lemat.slnx  global.json  Directory.Build.props  Directory.Packages.props  BannedSymbols.txt  .editorconfig
│   ├── openapi/lemat-v1.json                # generowany; źródło typów frontendu
│   ├── src/  Lemat.Domain  Lemat.Application  Lemat.Agents  Lemat.Models  Lemat.Infrastructure
│   │         Lemat.Api  Lemat.Worker  Lemat.Scheduler  Lemat.Tools
│   └── tests/ Lemat.Domain.Tests  Lemat.Application.Tests  Lemat.Agents.Tests  Lemat.Models.Tests
│              Lemat.Infrastructure.Tests  Lemat.Api.Tests  Lemat.Architecture.Tests
├── frontend/                                # 6.8, 6.9
│   └── src/ app/ ((public) (student) (teacher)/teacher (administrator)/admin)  features/  lib/  ui/
└── (POST-MVP) backend/src/Lemat.ImageSandbox, backend/sandbox/sympy, deploy/
```

Granice warstw (6.5), egzekwowane referencjami projektów i testami architektury:
`Lemat.Domain` — tylko BCL (czas jako parametr `DateTimeOffset teraz`, bez `DateTime.Now`);
`Lemat.Application` — Domain + abstrakcje `Microsoft.Extensions.*` (w tym
`Microsoft.Extensions.AI.Abstractions`), **bez** EF Core/ASP.NET/SK/SDK dostawców;
`Lemat.Agents` — przyjmują i zwracają rekordy, nie znają EF Core ani ASP.NET i **nie dotykają
bazy** (dane przez porty), bez `IChatClient` w konstruktorach (wyłącznie `IModelGeneratywny`);
SDK dostawców modeli (`OpenAI.*`, `Azure.AI.*`, `Anthropic.*`) **wyłącznie** w `Lemat.Models`;
`Lemat.Api` — endpointy wywołują handlery przypadków użycia i fasady, **nie** `LematDbContext`;
moduły rozmawiają tylko przez fasady `I<Module>Facade` (6.2). Frontend (6.9): `src/app` importuje
z `features/<x>` wyłącznie `index.ts`; wyłącznie `lib/api/client.ts` woła `/api/*`; wyłącznie
`lib/sse/stream.ts` otwiera strumienie SSE; `schema.d.ts` jest generowany.

## 7. Zasada mocków i interfejsów (aplikacja ma działać end-to-end bez żadnych kluczy)

Każda zależność zewnętrzna jest **za interfejsem (portem)**, a implementację wybiera
**konfiguracja** (`appsettings*.json` / zmienne `LEMAT_*`). Domyślnie (Development, brak
kluczy) działają implementacje Mock/lokalne — aplikacja przechodzi cały przepływ offline.

| Zależność | Port | Domyślnie (MVP) | Realna (wybór konfiguracją) |
|---|---|---|---|
| Model generatywny (rozpoznawanie, Wskazówka) | `IModelGeneratywny` → `KlientCzatuRoli` | `MockChatClient` (dostawca `mock`) | Microsoft.Extensions.AI: `openai-zgodny`, `azure-openai`, `anthropic` (zmienna `LEMAT_MODELE_DOSTAWCA` + klucze) |
| Model decyzyjny (Ocena) | `IModelDecyzyjny` | `MockModelDecyzyjny` | MVP: `ZapasowyLlmAdapter` (LLM ze schematem JSON); POST-MVP: `JevAdapter` + obwód |
| Magazyn zdjęć | `IMagazynObiektow` | `MagazynObiektowLokalny` (katalog na dysku, adresy podpisane HMAC) | POST-MVP: `MagazynObiektowS3` (MinIO/S3 UE) |
| Obróbka obrazów | `IObrobkaObrazow` | `ProstaObrobkaObrazow` (walidacja typu/rozmiaru, bez przetwarzania) | POST-MVP: `ImageSandboxKlient` (Magick.NET) |
| Obliczenia symboliczne | `ISprawdzanieSymboliczne` | `ProsteSprawdzanieSymboliczne` (normalizacja LaTeX; nigdy fałszywe `NIEROWNOWAZNE` — przy wątpliwości `NIEROZSTRZYGNIETE`) | POST-MVP: `SympySidecarKlient` |
| E-mail | `INadawcaEmail` | `NadawcaEmailLog` (zapis do logu) | POST-MVP: MailKit |
| Rejestr Zdarzeń RODO | `IRejestrZdarzenRodo` | POST-MVP | `RejestrZdarzenRodoS3` |

Zasady: (1) kod domenowy, aplikacyjny i agenci nie wiedzą, która implementacja jest aktywna;
(2) `mock` w środowisku `Production` kończy start procesu błędem (ADR-055 p. 6); (3) dane nie
trafiają do dostawcy modelu bez potwierdzonej retencji (A-07, `409 BRAK_RETENCJI_DOSTAWCY`) —
dla dostawców innych niż `mock` wymagana jawna zmienna potwierdzająca; (4) scenariusze Mock
wybiera nagłówek `X-Lemat-Mock-Scenariusz` (tylko `Development`/`E2E`, ADR-065); (5) testy
jednostkowe używają `Fake*`/`Mock*`, testy integracyjne — prawdziwego PostgreSQL 16
(Testcontainers; awaryjnie `LEMAT_TEST_BAZA`) i Mocków modeli; (6) nigdy nie wpisuj kluczy API do
repozytorium — `.env`/user-secrets, a wymagane klucze wypisz w `HANDOFF.md` → „⚠️ Wymagane akcje od człowieka”.

## 8. Reguły techniczne (skrót; szczegóły w podanych sekcjach)

- **C#:** `Nullable=enable`, `TreatWarningsAsErrors=true`, `ImplicitUsings`, file-scoped namespaces,
  `TimeProvider` zamiast `DateTime.Now` (BannedApiAnalyzers), rekordy dla DTO i wartości, brak
  wyjątków do sterowania przepływem w domenie. Komunikaty błędów i walidacji po polsku (NFR-10).
- **Transakcje (15.1, ADR-056):** zmiana stanu agregatu i zlecenie kolejnej pracy w tle w **jednej**
  transakcji `IJednostkaPracy` (`IZlecaniePrac.ZlecAsync`); `NOTIFY` wysyłają triggery; wywołania
  modeli **poza** transakcją; zapis wyniku warunkowy (`version`, `status`); najpierw zmiana starego
  wiersza, potem INSERT nowego.
- **Handlery prac w tle są idempotentne** i sprawdzają warunek wykonania z 13.3 (w tym konto `AKTYWNE`).
- **Ocena jest niezmienna** (`trg_ocena_niezmienna`); korekta/rozstrzygnięcie = nowa wersja (Z-5).
  Każda decyzja modelu trafia do `decyzja_modelu` (Z-4, NFR-05).
- **API:** ProblemDetails RFC 9457 z kodami domenowymi (12.2), JSON `snake_case`, enumy `UPPER_SNAKE`,
  `version` w ciele jako źródło `If-Match` (ADR-069), paginacja kursorowa, uprawnienia obiektowe
  (12.8, ADR-071 — każdy endpoint z identyfikatorem sprawdza własność zasobu), ścieżki REST po
  polsku wg słownika (`/rozwiazania/{id}/ocena`), typy wejścia/wyjścia = rekordy z `docs/spec/api.md` 3.
- **EF Core:** mapowania wg `docs/spec/ef-core.md`; typy PostgreSQL enum przez `MapEnum`; `jsonb` wg 7.5;
  kolumny `NOT NULL` bez `DEFAULT` muszą być w modelu.
- **Frontend:** wszystkie teksty UI w `frontend/messages/pl-PL.json` (nigdy w JSX na sztywno);
  dane użytkownika pobierają komponenty klienckie przez TanStack Query + `client.ts`; token dostępu tylko
  w pamięci; zdjęcia kartek wyłącznie przez podpisany URL; wzory przez KaTeX (`throwOnError: false`).
- **Opcje domyślne decyzji D-1 … D-17** (23.4) są implementowane tak, by zmiana decyzji dotyczyła
  wyłącznie reguł w `Lemat.Domain.Grading`/`Verifications`/`Accounts` i konfiguracji.

## 9. Workflow pracy: jedno TASK = jedna sesja = jeden commit

1. **Wybierz** najniższy `TASK-XXX` ze statusem `[ ]`, którego wszystkie zależności są `[x]`;
   kolejność: wszystkie `[MVP]` danej fazy przed `[POST-MVP]`. Nie zaczynaj zadania `[POST-MVP]`, dopóki
   `[MVP]` całego planu nie jest ukończone, chyba że właściciel zdecyduje inaczej.
2. **Przeczytaj** `HANDOFF.md`, `docs/CODEBASE_MAP.md`, opis TASK i **tylko** sekcje/ADR/tabele DDL
   z pola „Realizuje”. Sprawdź słownik (3.5) dla każdej nowej nazwy.
3. **Zaimplementuj dokładnie zakres TASK** — nie rozszerzaj go, nie refaktoryzuj poza zakresem. Brak lub
   sprzeczność w architekturze → „Otwarte pytania” w `HANDOFF.md` (przez raport); jeśli bez odpowiedzi nie
   da się spełnić DoD, oznacz TASK jako zablokowany i zakończ raportem.
4. **Nagłówki** `// @Role … | @Task: TASK-XXX` w każdym utworzonym/zmienionym pliku `.cs/.ts/.tsx` (pkt 5.1).
5. **Zweryfikuj** (pkt 10); napraw wszystkie błędy; nie wyłączaj testów, nie osłabiaj analizatorów.
6. **Raport developera:** utworzone/zmienione pliki, wyniki build/test/lint, decyzje i odstępstwa (U-xx),
   użyte mocki, wskazówki dla następnego TASK.
7. **`handoff-guardian`:** aktualizuje `docs/CODEBASE_MAP.md`, `TASKS.md`, `HANDOFF.md`, poprawia nagłówki.
8. **Commit** (jeden na TASK, nie łącz zadań, migracje bazy nie mieszają się z innym kodem poza tym TASK):

   ```text
   feat(TASK-022): reguły trasowania T-1…T-6 + updated codebase map & handoff

   Realizuje: 11.3, ADR-008, ADR-022, FR-23…FR-26
   ```

   Format: `<typ>(TASK-XXX): <opis w trybie rozkazującym> + updated codebase map & handoff`; typ domyślnie
   `feat`, dla szkieletu/konfiguracji `chore`, dla samych testów `test`, dla poprawek `fix`. Dodaj stopkę
   atrybucji wymaganą przez środowisko. Commituje orkiestrator (lub Ty, gdy pracujesz samodzielnie);
   `developer` i `handoff-guardian` nie commitują. Bez `git push`, jeśli nikt o to nie prosił.

## 10. Komendy weryfikacji (uruchamiaj to, czego dotyczy TASK; docelowo `scripts/verify-all.sh`)

```bash
# Backend (katalog backend/)
dotnet build backend/Lemat.slnx -warnaserror
dotnet test backend/Lemat.slnx --no-build
dotnet format backend/Lemat.slnx --verify-no-changes          # od TASK-007

# Frontend (katalog frontend/)
npm --prefix frontend run typecheck       # tsc --noEmit
npm --prefix frontend run lint
npm --prefix frontend run test            # Vitest
npm --prefix frontend run build

# Po zmianie kontraktu API (od TASK-071): regeneracja klienta i brak różnic
dotnet build backend/src/Lemat.Api && npm --prefix frontend run gen:api && git diff --exit-code backend/openapi frontend/src/lib/api/schema.d.ts

# E2E (od TASK-110): Playwright przeciw lokalnemu API + Next.js z Mockami
npm --prefix frontend run e2e
```

Testy integracyjne bazy wymagają Dockera (Testcontainers) albo lokalnego PostgreSQL 16
(`LEMAT_TEST_BAZA`). Zadanie nie jest ukończone, jeśli build/test/lint/typecheck nie przechodzą albo test
słownika zgłasza naruszenie.

## 11. Czego nie robić

- Nie edytuj `docs/` (poza `docs/CODEBASE_MAP.md` jako `handoff-guardian`), nie zmieniaj stosu ani
  struktury kontenerów bez nowego ADR uzgodnionego z właścicielem produktu.
- Nie dodawaj zewnętrznego brokera, WebSocketów, cache poza PostgreSQL, Hangfire/Quartz, MediatR-podobnych
  frameworków, ani nowych zależności infrastrukturalnych bez ADR. Pakiet spoza 5.2/6.4 → najpierw pytanie
  w „Otwarte pytania”.
- Nie generuj schematu z modelu EF Core (żadnych `dotnet ef migrations add` z modelu) — tylko SQL z DDL (ADR-063).
- Nie implementuj automatycznego zaliczania dowodów ani automatycznej finalizacji Oceny wstępnej po SLA
  (D-5, FR-26); nie twórz wpisu ręcznego Rozwiązania (ADR-024).
- Nie loguj danych osobowych ani treści promptów; nie wysyłaj danych do dostawcy modelu bez potwierdzonej retencji.
- Nie wykonuj zadań `[POST-MVP]` „przy okazji” — dopisz pomysł do `HANDOFF.md` → „Otwarte pytania”.
- Nie commituj sekretów, `.env`, katalogów `bin/obj/.next/node_modules`, plików magazynu lokalnego.
