# HANDOFF.md — przekazanie projektu Lemat

| Pole | Wartość |
|---|---|
| Data | 2026-11-01 |
| Etap | **Implementacja w toku** — ukończono TASK-001 … TASK-007, TASK-013 … TASK-016 (11 z 120 pozycji `TASKS.md`; 11 z 60 `[MVP]`) |
| Cel właściciela | **Działające MVP jak najszybciej**, kompletne end-to-end (Uczeń i Nauczyciel), całość uruchamialna lokalnie bez kluczy API (Mock) |
| Architektura | `docs/ARCHITECTURE_DETAILED.md` v3.5; spec: `docs/spec/{ddl,ef-core,api,prompts}.md`; ADR-001 … ADR-075 w `docs/adr/` |
| Stos | .NET 10 (`backend/`), Next.js App Router + TypeScript (`frontend/`), PostgreSQL 16, REST + SSE, Microsoft.Extensions.AI + Semantic Kernel |
| Kod | moduł Konta: `Lemat.Domain.Accounts`, `Lemat.Application.Accounts`, konfiguracje EF i repozytoria (TASK-016) + warstwa trwałości EF Core: konwencje, `IJednostkaPracy`, `TlumaczBledowBazy`, `DodajPersystencje` (TASK-015) + `Lemat.Domain.Common` (TASK-014) + szkielet solucji `backend/` (TASK-001) + 7 projektów testowych (TASK-002) + hosty z `/zdrowie` i OpenAPI (TASK-003) + `LematOpcje` (TASK-004) + PostgreSQL 16 w `docker-compose.yml` i skrypty `scripts/db-*.sh` (TASK-005); szkielet `frontend/` (TASK-006: Next.js, next-intl pl-PL, KaTeX, `src/ui`); skrypty `verify-*` i CI (TASK-007); schemat bazy: `extract-ddl.sh`, migracja SQL, `Lemat.Tools migrate`, `PostgresFixture` (TASK-013); dokumentacja i definicje agentów w `.claude/agents/` |
| Następne TASK | **TASK-017** (moduł Treści; zależności TASK-015, TASK-016 spełnione) |
| Jak pracować | `CLAUDE.md` (reguły), `TASKS.md` (backlog + macierz pokrycia), `docs/CODEBASE_MAP.md` (żywa mapa), ten plik (stan, założenia, decyzje) |

Poprzednia wersja `CLAUDE.md`/`TASKS.md`/`HANDOFF.md` (Python/FastAPI/React, ścieżki `src/lemat/…`) była nieaktualna i została
zastąpiona (18.5 l). Nie ma w repozytorium kodu do migracji.

---

## 1. Podsumowanie architektury

**Produkt.** Lemat to platforma przeglądarkowa (telefon i komputer) do nauki do matury z matematyki. Uczeń: rejestracja → poziom matury → opcjonalny
Test diagnostyczny → Profil ucznia → Rekomendacja kolejnego elementu Ścieżki nauki (Wkładka teoretyczna lub Zadanie) → rozwiązanie na kartce, zdjęcie →
Transkrypcja (podgląd i Poprawki Ucznia) → Ocena z Pewnością oceny i Wskazówką → aktualizacja Profilu. Nauczyciel: Klasa z kodem zaproszenia, Profil Ucznia,
Kolejka weryfikacji (Oceny o niskiej pewności, poprawny wynik przy błędnych krokach, dowody, Poprawki Ucznia) i rozstrzygnięcie. Administrator (po MVP): konta,
Arkusze CKE, konfiguracja modeli, kalibracja.

**Zasady nadrzędne.** Z-1 *Jev decyduje, LLM pisze* (punkty i automatyczne zaliczenie tylko z Modelu decyzyjnego z zapisanym rozkładem; sygnały LLM tylko w
kierunku bezpiecznym); Z-2 wymagania > architektura; Z-3 przy wątpliwości lub awarii → Weryfikacja, nigdy automatyczne zaliczenie, błąd odczytu nie obniża Oceny;
Z-4 każda decyzja modelu w `decyzja_modelu`; Z-5 Ocena niezmienna (korekta = nowa wersja); Z-6 słownik jest prawem.

**Moduły** (6.2): Konta, Klasy, Treści, Profil, Rekomendacje, Rozwiązania, Ocenianie, Weryfikacje, Kalibracja, Modele (porty), Orkiestracja, Powiadomienia, Audyt,
Wspólne. Moduły komunikują się wyłącznie przez fasady `I<Module>Facade`; Agenci (`Lemat.Agents`) nie dotykają bazy; SDK dostawców modeli są tylko w `Lemat.Models`.

**Procesy** (jeden obraz, trzy punkty wejścia): `Lemat.Api` (REST + SSE), `Lemat.Worker` (pule prac w tle R/S/O/P/W na `FOR UPDATE SKIP LOCKED` + `LISTEN/NOTIFY`),
`Lemat.Scheduler` (POST-MVP). PostgreSQL 16 jest jedynym źródłem prawdy i mechanizmem prac w tle (ADR-002, ADR-053); zmiana stanu i zlecenie kolejnej pracy
w jednej transakcji (ADR-056); wywołania modeli poza transakcją.

**Agenci** (6.6): rekomendujący (Elo + szablony, bez LLM), rozpoznający (multimodalny LLM → Transkrypcja), oceniający (plan pytań do Modelu decyzyjnego + czyste reguły
`ZasadyOceniania` z Domain), wyjaśniający (Wskazówka + strażnik wycieku), generujący (POST-MVP). Porty: `IModelGeneratywny` (fabryka klientów czatu per rola),
`IModelDecyzyjny`, `ISprawdzanieSymboliczne`, `IMagazynObiektow`, `IObrobkaObrazow`, `IPredyktorSukcesu`.

**Maszyna stanów Rozwiązania** (8.1): `PRZESLANE → ROZPOZNAWANIE → TRANSKRYPCJA_GOTOWA → ZATWIERDZONE → OCENIANIE → OCENIONE | OCZEKUJE_NA_WERYFIKACJE`
(+ `BLAD_ROZPOZNAWANIA`, `PORZUCONE`, `ANULOWANE`). **Trasowanie** (11.3): T-1 dowód, T-2 poprawny wynik przy błędnych krokach, T-3 podejrzenie błędu odczytu,
T-4 niska pewność, T-5 Kontrola losowa, T-6 awaria (termin twardy 120 s), T-7 Poprawki Ucznia.

**Schemat.** `docs/spec/ddl.md` (ok. 61 tabel, typy enum, 15 triggerów, funkcje, partycje `decyzja_modelu`) jest jedynym źródłem; migracje EF Core wykonują pliki SQL (ADR-063).
**W MVP stosujemy pełne DDL jedną migracją** (U-01), a encje EF mapujemy tylko dla tabel używanych przez MVP.

**Mapa wymagań:** `TASKS.md` → „Macierz pokrycia”; pełna RTM architektury: sekcja 18.

---

## 2. Założenia biznesowe i techniczne

### 2.1 Biznesowe (z architektury: A-01 … A-15, skrót)

- Jev (Model System One) jest we wczesnym dostępie (A-02) — system działa bez niego: model zapasowy (LLM ze schematem JSON) jest pełnoprawnym Modelem decyzyjnym. **W MVP Jev nie jest integrowany.**
- Uczniowie mają ≥ 16 lat (A-06, oświadczenie przy rejestracji). Dostawcy modeli: UE/SCC, umowa powierzenia, brak trenowania, retencja zero dla zdjęć i ≤ 30 dni dla pozostałych ról (A-07).
- NFR-01 interpretowany jako czas systemowy (A-11, D-2): ≥ 99% Rozwiązań ≤ 30 s widocznego czasu, bez czasu poprawek Ucznia.
- Matura od 2023 (13 Działów, A-04); Kryteria CKE jako lista kryteriów z wariantami (A-05).
- Pilotaż: 1 000 Uczniów, 50 Nauczycieli, 1 500 Rozwiązań/dobę (A-03); Nauczyciele pracują w dni robocze, SLA Weryfikacji 3 dni robocze (A-15) — **w MVP SLA nie jest egzekwowane (U-15)**.
- Dowody (`OTWARTE_Z_DOWODEM`) zawsze trafiają do Weryfikacji (FR-26); brak wpisu ręcznego Rozwiązania (ADR-024); brak automatycznej finalizacji po SLA (D-5).

### 2.2 Techniczne

- Dane modelu: tylko przez porty; konfiguracje modeli, progi i kalibratory w bazie (`konfiguracja_modelu`, `prog_pewnosci`, `kalibrator_pewnosci`), parametry w `LematOpcje`/`appsettings` + `LEMAT_*`.
- JWT ES256 (15 min, w pamięci klienta) + refresh token w cookie `lemat_rt` (rotacja); argon2id; uprawnienia obiektowe (ADR-071) — cudze zasoby → `404`/`403`.
- API: ProblemDetails RFC 9457 z kodami domenowymi; JSON `snake_case`, enumy `UPPER_SNAKE`; `version` w ciele jako źródło `If-Match` (ADR-069); paginacja kursorowa; OpenAPI 3.x generowane do `backend/openapi/lemat-v1.json` i typy TS generowane z niego.
- Scenariusze Mock sterowane nagłówkiem `X-Lemat-Mock-Scenariusz` (tylko `Development`/`E2E`); domyślna odpowiedź Mock prowadzi trasą automatyczną (pewność 0.995 ≥ próg 0.95).
- Wersje: .NET SDK `10.0.114` zainstalowany lokalnie; Node `22.x` + npm `10.x` (architektura wskazuje Node 24 LTS — 22 wystarcza, Next.js wymaga ≥ 20.9); PostgreSQL 16.15 lokalnie na **5432** (zajęty — compose używa domyślnie 5433), Docker 29.

---

## 3. Decyzje właściciela produktu D-1 … D-17 (23.4) — opcje domyślne obowiązujące w implementacji

Do czasu akceptacji w bramce **G-0** implementacja realizuje opcje domyślne. Zmiana decyzji ma dotyczyć wyłącznie reguł w `Lemat.Domain.Grading/Verifications/Accounts` i konfiguracji.

| # | Decyzja | Opcja domyślna (implementowana) | Status w MVP |
|---|---|---|---|
| D-1 | Zmiany słownika PZS-1 (86 pojęć) | treść 3.3/3.4 + mapowanie 3.5 | stosowane; **do akceptacji w G-0** (wymaga zgody właścicieli słownika) |
| D-2 | Interpretacja NFR-01 (A-11) | czas systemowy, ≥ 99% ≤ 30 s | stosowane (pomiar POST-MVP) |
| D-3 | Ocena wstępna wstrzymana przy podejrzeniu błędu odczytu | ADR-022 (`WSTRZYMANA_BLAD_ODCZYTU`) | TASK-021, 022 |
| D-4 | Pula weryfikatorów globalnych (≥ 8 współpracujących albo start bez Uczniów bez Klasy) | A-14, flaga `LEMAT_REJESTRACJA_BEZ_KLASY` | **MVP: demo — jeden weryfikator globalny z seed-dev** (U-07, U-15) |
| D-5 | Brak automatycznej finalizacji po SLA | ADR-021 | stosowane |
| D-6 | Tryb pełnej weryfikacji gdy ZT-1 nie daje progu | 11.5.1 p. 5 | POST-MVP (TASK-063) |
| D-7 | Brak wpisu ręcznego | ADR-024 | stosowane |
| D-8 | Kopie treningowe za odwołalną zgodą | ADR-035 | POST-MVP (TASK-070) |
| D-9 | Zatwierdzanie kont Nauczycieli; dodatkowe powody Weryfikacji | 16.1, 11.3 | **MVP: autozatwierdzenie w Development (U-04)**; powody w DDL/domenie |
| D-10 | Zakres FR-18 (Poprawki: zmiana + ≤ 2 wstawione kroki, limity, Sprawdzenie zgodności ze zdjęciem) | ADR-036 | **MVP: limity twarde + każda Poprawka → Weryfikacja (U-06)**; reszta TASK-059 |
| D-11 | Zakres FR-14 (Przegląd Zadań) | ADR-028, ADR-038 | POST-MVP (TASK-097) |
| D-12 | G-1(a) dla dowodów: ≥ 90%, LB Wilsona ≥ 85,0% | 11.5.1 | POST-MVP (TASK-116) |
| D-13 | SLA 3 dni robocze, 18 weryfikatorów globalnych | A-15, ADR-043 | POST-MVP (TASK-035, 065) |
| D-14 | Możliwość pominięcia Testu diagnostycznego | pominięcie dozwolone | **stosowane w MVP** (TASK-075) |
| D-15 | Przywrócenie konta Nauczyciela po przekazaniu Klas | Klasy zostają u nowego Nauczyciela | POST-MVP (TASK-033, 094) |
| D-16 | Reakcja bezpiecznika trafności | automatyczny tryb pełnej weryfikacji | POST-MVP (TASK-032, 063) |
| D-17 | Zapas puli weryfikatorów (G-7 = 18) | współczynnik 1,22 | POST-MVP |

---

## 4. Uproszczenia MVP (odstępstwa tymczasowe od architektury; „U-xx”)

Uproszczenia **nie zmieniają kontraktów** (porty, endpointy, schemat DDL, nazwy ze słownika) — dlatego późniejsza zamiana implementacji nie powoduje zmian w modułach.
Kolumna „Blokuje produkcję” oznacza, że do startu produkcyjnego z prawdziwymi Uczniami konieczne jest wykonanie wskazanego TASK.

| ID | Uproszczenie | Powód | Blokuje produkcję | Usuwa |
|---|---|---|---|---|
| U-01 | Pełne DDL jedną migracją (wszystkie tabele), encje EF tylko dla tabel MVP; test zgodności modelu z katalogiem po MVP | najszybsza zgodność z ADR-063 | nie | TASK-037 |
| U-02 | `ProstaObrobkaObrazow`: tylko walidacja typu (magic bytes JPEG/PNG/WEBP) i rozmiaru; **bez usuwania EXIF**, bez normalizacji i bez HEIC po stronie serwera (HEIC konwertuje klient) | brak sandboxu Magick.NET (ADR-045/052) | **tak** (NFR-09, EXIF) | TASK-056 |
| U-03 | Magazyn lokalny na dysku (`MagazynObiektowLokalny`), adresy podpisane HMAC przez `GET /magazyn-lokalny/{token}`; bez S3/MinIO, bez Object Lock | brak infrastruktury S3 | tak | TASK-057, 066 |
| U-04 | Konta Nauczycieli autozatwierdzane w `Development` (flaga konfiguracji); konto Administratora tylko z `seed-dev`; brak panelu Administratora | brak panelu zatwierdzania | **tak** (D-9) | TASK-094 |
| U-05 | `ProsteSprawdzanieSymboliczne` (normalizacja LaTeX; nigdy fałszywe `NIEROWNOWAZNE`, w razie wątpliwości `NIEROZSTRZYGNIETE`) zamiast sidecara SymPy | brak sidecara Python | częściowo (jakość FR-21) | TASK-055 |
| U-06 | T-7 uproszczone: każda Transkrypcja z Poprawkami Ucznia → Weryfikacja (`POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`); limity twarde ADR-036 aktywne; brak Sprawdzenia zgodności ze zdjęciem, limitów miękkich i bezpiecznika poprawek | kierunek bezpieczny (Z-3) zamiast agenta kontrolnego | nie (zwiększa obciążenie Nauczycieli) | TASK-059 |
| U-07 | Brak Filtra danych osobowych (ADR-042), maskowania zdjęć i pseudonimizacji (ADR-031); kolejka `GLOBALNA` pokazuje oryginalne zdjęcia weryfikatorowi globalnemu | poza zakresem MVP | **tak** (RODO, G-0/G-6) | TASK-058, 065 |
| U-08 | Brak Schedulera: partycje `decyzja_modelu` tworzy `Lemat.Tools migrate` na 12 miesięcy naprzód (**po tym czasie wymagany ponowny `migrate`**), wygasanie elementów Ścieżki leniwie przy `nastepna`; brak `PORZUC_NIEZATWIERDZONE`, `ZAMIATAJ_PROFILE`, `SPRAWDZ_ZAWIESZONE_ROZWIAZANIA` | uproszczenie hostingu | tak (po MVP) | TASK-064 |
| U-09 | Test diagnostyczny: 1 Zadanie `ZAMKNIETE` na Dział (poziom nominalny 2, zapas 1/3), nie 2 poziomy; pokrycie < 50% → `409 TEST_NIEDOSTEPNY` | mała pula seed | nie | TASK-100 (pula, G-5) |
| U-10 | Kalibrator tożsamościowy, stały Próg pewności z `seed-dev` (0.95/0.98, dowód 1.000), brak Kontroli losowej (odsetek 0), brak bezpieczników | brak ZT-1 | **tak** (NFR-02 … NFR-04, G-1) | TASK-032, 063, 116 |
| U-11 | Brak `Idempotency-Key`, limitów szybkości 12.7 i ogranicznika wywołań modelu; UI blokuje podwójne kliknięcie | uproszczenie | tak | TASK-090, 091, 054 |
| U-12 | Frontend: bez PWA/Serwist/offline, bez Web Workera kompresji (canvas w wątku głównym), bez synchronizacji wielu kart, bez autozapisu szkicu Transkrypcji | szybkość | nie | TASK-102, 104, 105 |
| U-13 | Dostawca `anthropic` w `FabrykaKlientowCzatu` jest **dodatkiem spoza ADR-058** (`azure-openai`, `openai-zgodny`, `mock`) | wymaganie właściciela MVP | nie, ale wymaga ADR | OQ-01 |
| U-14 | Brak Opinii pomocniczej i `PONOW_OCENE`; T-6 → Weryfikacja bez Oceny, Nauczyciel ocenia od zera | uproszczenie | nie | TASK-062 |
| U-15 | Weryfikacja bez szkicu, przedłużania Blokady, eskalacji, SLA, kontroli przyjęć | uproszczenie | tak | TASK-035, 065, 103 |
| U-16 | Obserwowalność: logi JSON na konsoli; bez OpenTelemetry Collector, Prometheus, Grafana, alertów | uproszczenie | tak | TASK-114 |
| U-17 | Testy architektury lekkie (warstwy, zakazane synonimy, nagłówki, ASCII); brak manifestu `file-names.yaml` i pełnego testu słownika | szybkość | nie | TASK-008, 009, 113 |
| U-18 | Brak powiadomień w aplikacji: Uczeń widzi status przez SSE/odświeżenie listy, Nauczyciel przez odświeżenie Kolejki | uproszczenie | nie | TASK-096 |
| U-19 | **Dane startowe są autorskie, „w stylu CKE”** (nie import Arkuszy CKE); wagi Umiejętności równe | brak importu | **tak** (jakość merytoryczna) | TASK-068, 098 |
| U-20 | Brak Dockerfile, Caddy i wdrożenia; uruchomienie: PostgreSQL (Docker lub lokalny) + `dotnet run` + `npm run dev` | szybkość | tak | TASK-010, 119 |

---

## 5. Jak uruchomić (stan na dziś: szkielet backendu .NET + LematOpcje + baza w Dockerze + szkielet frontendu Next.js)

Dziś działa wyłącznie (SDK .NET 10.0.100, `global.json`, `rollForward: latestPatch`; brak zmiennych środowiskowych i bazy; testy nie wymagają Dockera):

```bash
dotnet build backend/Lemat.slnx -warnaserror
dotnet test backend/Lemat.slnx                       # Architecture.Tests 104 (warstwy, słownik, nagłówki, ASCII, KontenerDiTests, LematOpcjeHostyTests), Application.Tests 14, Api.Tests 3, testy dymne
dotnet format backend/Lemat.slnx --verify-no-changes
dotnet run --project backend/src/Lemat.Tools -- --help   # dispatcher; polecenie `migrate` od TASK-013 (nieznane polecenie -> kod 2)
dotnet run --project backend/src/Lemat.Api         # http://localhost:5080/api/v1/zdrowie -> 200 {"status":"OK"}; /api/v1/openapi.json
dotnet run --project backend/src/Lemat.Worker      # GET http://localhost:8081/zdrowie
dotnet run --project backend/src/Lemat.Scheduler   # GET http://localhost:8082/zdrowie
```

`Lemat.Api` ma tylko `GET /api/v1/zdrowie` (Development: adres 5080; poza Development adres z konfiguracji ASP.NET Core); `Worker`/`Scheduler` to hosty bez logiki z `/zdrowie` na 8081/8082. Logi JSON na konsoli. Zmienne środowiskowe: opcjonalne `LEMAT_*` (tabela w sekcji 6; w Development nic nie jest wymagane — hosty startują na Mockach/lokalnych). Baza działa już dziś (TASK-005). Schemat bazy działa od TASK-013 (`migrate`); docelowo (po TASK-026, 112 — `seed-dev`, `dev-up.sh`):

```bash
# 1. Baza (DZIAŁA od TASK-005; Docker, port 5433, żeby nie kolidować z lokalnym PostgreSQL na 5432)
cp .env.example .env && docker compose up -d postgres        # albo scripts/db-up.sh; reset pustej bazy: scripts/db-reset.sh
# alternatywa bez Dockera: scripts/db-local-setup.sh (PostgreSQL 16 na 5432; PGADMIN="sudo -u postgres" gdy nie root/postgres; wypisuje LEMAT_BAZA do ustawienia)
# psql: psql "host=localhost port=5433 dbname=lemat_dev user=lemat password=lemat_dev"
# 2. Schemat (DZIAŁA od TASK-013; idempotentne; połączenie: --polaczenie=..., LEMAT_BAZA_MIGRACJE, LEMAT_BAZA, domyślnie dev 5433) i dane startowe
dotnet run --project backend/src/Lemat.Tools -- migrate       # 61 tabel, partycje decyzja_modelu na 12 miesięcy
dotnet run --project backend/src/Lemat.Tools -- seed-dev      # konta demo, konfiguracje Mock, Zadania, Wkładki
# 3. Procesy
dotnet run --project backend/src/Lemat.Api                    # http://localhost:5080/api/v1/zdrowie
dotnet run --project backend/src/Lemat.Worker
npm --prefix frontend install && npm --prefix frontend run dev   # http://localhost:3000
# albo wszystko naraz: scripts/dev-up.sh (TASK-112)
```

Frontend (DZIAŁA od TASK-006; Node >= 22, bez zmiennych środowiskowych; opcjonalne `LEMAT_API_URL`, domyślnie `http://localhost:5080` — cel `rewrites` `/api/:path*`):

```bash
npm --prefix frontend install        # albo npm ci
npm --prefix frontend run dev        # http://localhost:3000 (polska strona główna, <html lang="pl">)
npm --prefix frontend run typecheck && npm --prefix frontend run lint && npm --prefix frontend run test && npm --prefix frontend run build
npm --prefix frontend run check:headers   # nagłówki Living Documentation w .ts/.tsx
# gen:api i e2e to placeholdery (TASK-081 / TASK-110)
```

Weryfikacja jakości (DZIAŁA od TASK-007): `scripts/verify-all.sh` (opcje `--backend-only`/`--frontend-only`) = `scripts/verify-backend.sh` (restore, build `-warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`) + `scripts/verify-frontend.sh` (`npm ci`, typecheck, lint, test, build, `check:headers`). Skrypty są idempotentne; CI: `.github/workflows/ci.yml` (job backend z usługą postgres:16 na 5433 + Testcontainers, job frontend Node 22).

---

## 6. ⚠️ Wymagane akcje od człowieka / Konfiguracja

**Do wykonania przez właściciela / człowieka (agent nie może tego zrobić sam):**

0g. **Po TASK-016:** brak akcji obowiązkowych; brak mocków i nowych zmiennych `LEMAT_*`. Testy bazy: `LEMAT_TEST_BAZA` (np. `Host=localhost;Port=5432;Database=lemat_test;Username=lemat;Password=lemat_dev`) albo Docker. Do potwierdzenia: (a) definicja fnv1a (przyjęto 32-bit nad 16 bajtami UUID big-endian, mod 100; ten sam `Fnv1a.Skrot32` posłuży T-5 w TASK-021), OQ-09; (b) kody błędów domeny spoza katalogu 12.2 do zmapowania w TASK-071 (m.in. `EMAIL_NIEPRAWIDLOWY`, `KONTO_NIEPRAWIDLOWA_ROLA`, `OSWIADCZENIE_WIEKU_WYMAGANE`, `HASLO_HASH_WYMAGANY`, `TOKEN_*`, `KONTO_JUZ_USUWANE`, `PRZYWROCENIE_NIEMOZLIWE`, `UMOWA_POUFNOSCI_WYMAGANA`, `DEKLAROWANY_CZAS_ZA_KROTKI`); (c) hasher argon2id i rotacja tokenów z oknem tolerancji (ADR-057) — TASK-072; (d) hosty jeszcze nie wołają `DodajPersystencje`/`DodajModulKonta` (podpiąć przy pierwszym endpoincie, TASK-071/072).
0f. **Po TASK-015:** brak akcji obowiązkowych; brak mocków i nowych zmiennych `LEMAT_*`. Testy bazy wymagają Dockera (Testcontainers) albo `LEMAT_TEST_BAZA` (np. `Host=localhost;Port=5432;Database=lemat_test;Username=lemat;Password=lemat_dev`). Do potwierdzenia (informacyjnie): (a) kody błędów `ux_rozwiazanie_przypisanie` i `ux_test_jeden` w `KatalogIndeksowUnikalnych` to interpretacja 12.2 developera — zweryfikować w TASK modułów; (b) hosty (`Program.cs`/`ApiHost` itd.) NIE wołają jeszcze `DodajPersystencje` — podpiąć przy pierwszym endpoincie/handlerze potrzebującym bazy (wtedy `KontenerDiTests` będą wymagać bazy lub podmiany); (c) nazwa `DodajPersystencje` (wg TASKS.md), `ef-core.md` ma `DodajPersistence`.
0e. **Po TASK-014:** brak akcji obowiązkowych (czysty kod domeny, bez mocków, zmiennych środowiskowych ani konfiguracji). Do zatwierdzenia (informacyjnie): `default(PoziomTrudnosci)` ma `Wartosc == 0` (struct) — używać konstruktora; `CA1710` wyciszony na `WyjatekDomenowy` (nazwa wymagana przez zadanie).
0d. **Po TASK-013:** brak akcji obowiązkowych. Testy integracyjne wymagają Dockera (Testcontainers, postgres:16) albo `LEMAT_TEST_BAZA` (np. `Host=localhost;Port=5432;Database=lemat_test;Username=lemat;Password=lemat_dev`); `extract-ddl.sh` wymaga `bash`. **Decyzja do podjęcia:** rola `lemat_migracje` (rekomendacja z 0a: tak) NIE została utworzona — migracja 0005 działa na dowolnej roli właściciela, a role `lemat_api/worker/scheduler` tworzy jako NOLOGIN, gdy brak; rozstrzygnąć przed TASK-112/produkcją. Uwaga: granice partycji `decyzja_modelu` zależą od strefy czasowej sesji (`timestamptz`); po 12 miesiącach wymagany ponowny `migrate` (U-08). Mocki: brak (`konfiguracja_modelu` pusta do `seed-dev`, TASK-026).
0c. **Po TASK-007:** brak akcji obowiązkowych. Do lokalnego `verify-all.sh` potrzebne: .NET SDK 10.0.100+, Node >= 22, Docker (Testcontainers od TASK-013). Zalecane: po pierwszym pushu sprawdzić, że workflow `.github/workflows/ci.yml` przechodzi na GitHub Actions (lokalnie zweryfikowano tylko składnię — `yamllint`, `actionlint` niedostępny); rozważyć `packages.lock.json` dla cache NuGet. Mocki: nie dotyczy.
0b. **Po TASK-006:** brak akcji obowiązkowych. Wymagany Node >= 22 (+ npm); port 3000 musi być wolny. Decyzje do potwierdzenia: (a) Next 16.4 / React 19 / TS 5.9 (przypięte, bo `npm latest` to TS 7; 5.2 mówi o TS 5) — akceptacja wersji; (b) OQ-04 (Node 22, `next build --webpack` użyty w skrypcie `build`). Mocki: nie dotyczy (brak wywołań API do TASK-081). `LEMAT_API_URL` jest już czytane przez frontend.
0a. **Po TASK-005:** brak akcji obowiązkowych. Do uruchomienia bazy potrzebny Docker (`docker compose`) albo lokalny PostgreSQL 16 + `scripts/db-local-setup.sh`; port 5433 (Docker) lub 5432 (lokalnie) musi być wolny. Hasło `lemat_dev` jest wyłącznie deweloperskie (w compose rola `lemat` to superuser) — w środowiskach poza maszyną developerską ustawić własne `LEMAT_PG_HASLO`/`LEMAT_BAZA` i nie commitować `.env`. Zdecydować, czy rola `lemat_migracje` (osobna od ról aplikacji) ma powstać już w TASK-013 (rekomendacja: tak).
0. **Po TASK-004:** brak akcji obowiązkowych (Development startuje na Mockach bez zmiennych). W Production wymagane: `LEMAT_JWT_KLUCZ`, `LEMAT_KURSOR_KLUCZ`, dostawca ≠ mock wraz z `LEMAT_MODELE_KLUCZ_API`/`LEMAT_MODELE_ENDPOINT` i `LEMAT_POTWIERDZONA_RETENCJA_DOSTAWCY=true`; mock i autozatwierdzanie nauczycieli są w Production odrzucane. Zatwierdzić rozszerzenia kluczy z OQ-06 (wprowadzone, patrz niżej).
   Wcześniej (po TASK-003): brak akcji obowiązkowych; porty 5080/8081/8082 muszą być wolne; `backend/openapi/lemat-v1.json` jest generowany przy buildzie i ma być commitowany (zmiana kontraktu = zmiana pliku w diffie).
   Wcześniej (po TASK-002): brak akcji obowiązkowych; `Testcontainers.PostgreSql` jest już w pakietach, ale żaden test go jeszcze nie używa (Docker potrzebny dopiero od testów integracyjnych, TASK-014+); `git` musi być w PATH (testy architektury wołają `git ls-files`). Zalecane: ocenić odstępstwo OQ-03 (lekki test słownika zamiast pełnego).
   Wcześniej (po TASK-001): brak akcji obowiązkowych; zalecane: zainstalować SDK .NET 10.0.100+ (zgodnie z `backend/global.json`) i zdecydować, czy pakiety z podbitymi wersjami (NU1901–1903) akceptujemy do czasu TASK-004.
1. **Klucze modeli AI (opcjonalne — MVP działa bez nich na Mockach).** Aby użyć prawdziwego LLM do rozpoznawania pisma, Wskazówek i oceny (`ZapasowyLlmAdapter`), ustaw w `.env`/user-secrets:
   `LEMAT_MODELE_DOSTAWCA` (`openai-zgodny` | `azure-openai` | `anthropic`), `LEMAT_MODELE_KLUCZ_API`, `LEMAT_MODELE_ENDPOINT` (dla Azure i punktów zgodnych z OpenAI), nazwy modeli
   (wielomodalny dla `ROZPOZNAWANIE_PISMA`) i **jawne potwierdzenie retencji** `LEMAT_POTWIERDZONA_RETENCJA_DOSTAWCY=true` (A-07: zero-retention dla zdjęć, ≤ 30 dni dla reszty; umowa powierzenia).
   Bez potwierdzenia aplikacja z dostawcą ≠ `mock` nie wystartuje. Nigdy nie commituj kluczy. (Nazwy zmiennych zatwierdzone w TASK-004.)
2. **Bramka G-0** — akceptacja właściciela produktu i właścicieli słownika: PZS-1 (86 pojęć, D-1), interpretacja NFR-01 (D-2), pozostałe D-x z tabeli 3. Bez G-0 nie ma startu produkcyjnego;
   dla MVP obowiązują opcje domyślne. Po akceptacji: commit dopisujący PZS-1 do `docs/glossary.md` (jedyny dozwolony wyjątek od zasady „docs tylko do odczytu”).
3. **Przegląd merytoryczny danych startowych (U-19)** — Zadania, Kryteria oceniania i Wkładki z TASK-027/028 są autorskie i mogą zawierać błędy matematyczne; wymagany przegląd przez nauczyciela
   matematyki przed pokazaniem Uczniom. Zlecić zespołowi/nauczycielowi; wynik odnotować w „Stanie prac”.
4. **Decyzja o dostawcy `anthropic` (OQ-01)** — potwierdzić, czy dodać go do ADR-058 (nowy ADR-076) i czy jest dostępny jako `IChatClient` w wersjach pakietów z 6.4.
5. **Przed jakimkolwiek użyciem z prawdziwymi Uczniami** muszą zniknąć uproszczenia oznaczone w tabeli 4 jako „Blokuje produkcję” (U-02, U-03, U-04, U-07, U-10, U-19, U-20 i pozostałe wg tabeli),
   plus bramki G-1 … G-7 z 17.6 (ZT-1, rekrutacja weryfikatorów, przegląd RODO).
6. **Środowisko:** jeśli lokalny PostgreSQL ma być używany zamiast Dockera — utworzyć rolę/bazy skryptem `scripts/db-local-setup.sh` (TASK-005); Testcontainers wymaga działającego Dockera
   (albo `LEMAT_TEST_BAZA`).
7. **Konta demo** po `seed-dev` — hasła wypisywane w konsoli; zmienić przed jakimkolwiek wdrożeniem poza maszyną developerską.

**Mocki aktywne domyślnie** (nic nie trzeba konfigurować): `MockChatClient`, `MockModelDecyzyjny`, `MagazynObiektowLokalny`, `ProstaObrobkaObrazow`, `ProsteSprawdzanieSymboliczne`, `NadawcaEmailLog`.

**Zmienne środowiskowe (P-01, wdrożone w TASK-004 w `LematKonfiguracja.MapowanieZmiennych`; `LEMAT_TEST_BAZA`, `LEMAT_PG_PORT`, `LEMAT_API_URL` opcje nie czytają; `LEMAT_MOCK_OPOZNIENIE_MS` -> `Lemat:Modele:MockOpoznienieMs`; `LEMAT_REJESTRACJA_BEZ_KLASY` -> `Lemat:Konta:RejestracjaBezKlasy`):** `ASPNETCORE_ENVIRONMENT` (`Development`/`E2E`/`Production`), `LEMAT_SRODOWISKO`, `LEMAT_BAZA`, `LEMAT_BAZA_MIGRACJE`, `LEMAT_TEST_BAZA`,
`LEMAT_PG_PORT` (compose), `LEMAT_API_URL` (frontend), `LEMAT_MODELE_DOSTAWCA`, `LEMAT_MODELE_KLUCZ_API`, `LEMAT_MODELE_ENDPOINT`, `LEMAT_POTWIERDZONA_RETENCJA_DOSTAWCY`, `LEMAT_MAGAZYN_SCIEZKA`, `LEMAT_KURSOR_KLUCZ`,
`LEMAT_JWT_KLUCZ` (PEM ES256; w Development generowany), `LEMAT_CZAS_TESTOWY`, `LEMAT_MOCK_OPOZNIENIE_MS`, `LEMAT_KONTA_AUTOZATWIERDZANIE_NAUCZYCIELI` (tylko Development).

---

## 7. Stan prac

| Faza | `[MVP]` ukończone | `[POST-MVP]` ukończone | Następne TASK |
|---|---|---|---|
| 1. Szkielet i środowisko (001 … 012) | 7 / 7 | 0 / 5 | faza MVP ukończona; dalej **TASK-017** (faza 2) |
| 2. Model domenowy i baza (013 … 037) | 4 / 16 | 0 / 9 | **TASK-017** (TASK-013 … 016 gotowe) |
| 3. Agenci AI i prace w tle (038 … 070) | 0 / 14 | 0 / 19 | po TASK-013 … 028 |
| 4. API i integracja z Next.js (071 … 108) | 0 / 19 | 0 / 19 | po fazie 3 (dla części API wystarcza TASK-015, 016) |
| 5. Testy i dopracowanie (109 … 120) | 0 / 4 | 0 / 8 | po TASK-089 |

Ścieżka krytyczna MVP: 001 → 002 → 003/004 → 013 → 014 → 015 → 016 → 017 → 018 → 019 → 020 → 021 → 022 → 023 → 024 → 038 → 039/040/041 → 042 → 043 … 047 → 048 → 049 → 050 →
071 → 072 → 073 … 080 → 081 → 082 … 089 → 109 → 110 → 111 → 112. Zadania frontendu 006 oraz 005 można wykonywać równolegle z backendem; 081 … 089 równolegle po 071/072.

**Co działa (TASK-003):** `GET /api/v1/zdrowie` -> 200 `{"status":"OK"}` (Api, `application/json`), `AddProblemDetails`, OpenAPI 3.1.1 generowane przy buildzie do `backend/openapi/lemat-v1.json` (deterministyczne) i serwowane pod `/api/v1/openapi.json`, `/zdrowie` w Worker (8081) i Scheduler (8082), dispatcher `Lemat.Tools` (`--help`), logi JSON, `KontenerDiTests`. Poza tym: kompilująca się solucja `backend/Lemat.slnx` z 9 pustymi projektami `src` i grafem referencji z 6.4 (Domain bez pakietów; Application bez referencji do Infrastructure/Agents/Models); analizatory
(`TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild`), Central Package Management, `.gitignore`; 7 projektów testowych w `backend/tests` (xUnit v3) z zielonymi testami: reguły warstw 6.5 (referencje projektów + NetArchTest, testy negatywne na syntetycznym grafie, reguły dodatkowe `ModeleBezInfrastruktury`, `InfrastrukturaBezWarstwWyzszych`, `HostyNieZalezaOdSiebie`, referencje = tabela 6.4), Roslyn-owy strażnik zakazanych synonimów w deklaracjach, nagłówek Living Documentation w każdym `.cs`, ASCII nazw plików (`git ls-files`). Brak hostów z endpointami, bazy, frontendu.

**Co działa (TASK-004):** `Lemat.Application.Common.LematOpcje` (sekcja `Lemat`, wartości domyślne = tabela 22), `LematOpcjeWalidator` (`IValidateOptions` + `ValidateOnStart`), `LematKonfiguracja` (`MapowanieZmiennych` `LEMAT_*` -> `Lemat:*`, `DodajZmienneLemat`, `AddLematOpcje`), podpięte w ApiHost/WorkerHost/SchedulerHost; `appsettings.json` + `appsettings.Development.json` w trzech hostach. Walidacja startu odrzuca: mock w Production; dostawcę ≠ mock bez `PotwierdzonaRetencjaDostawcy` (A-07) lub bez klucza/endpointu; pustą `Magazyn:Sciezka`; nieznane typy/zakresy; dodatkowo w Production autozatwierdzanie nauczycieli i brak kluczy JWT/kursora. Development: autozatwierdzanie włączone, `Magazyn:Sciezka=../../storage/magazyn`. Konsumenci wstrzykują `LematOpcje` albo `IOptions<LematOpcje>`.

**Co działa (TASK-006):** `frontend/` (npm, Node 22): Next.js 16.4 App Router + React 19 + TypeScript strict 5.9, Tailwind 4, Radix UI (`radix-ui`), ESLint 9 flat config, Prettier, Vitest 3 + Testing Library (9 testów), next-intl 4 (tylko `pl-PL`, bez prefiksu w URL, `messages/pl-PL.json`), TanStack Query (`QueryProvider`), KaTeX (`Wzor`, `TrescMarkdown`; `trust: false`, brak wykonywania HTML), `src/ui` (`Button`, `Dialog`, `List`, `Form`), drzewo grup tras `(public)`, `(student)`, `(teacher)/teacher`, `(administrator)/admin` z pustymi układami, strona `/`, `scripts/check-headers.mjs`. `typecheck`, `lint`, `test`, `build` (`next build --webpack`) zielone; backend nietknięty.

**Co działa (TASK-014):** `Lemat.Domain.Common` (`backend/src/Lemat.Domain/Common/`, bez pakietów): `Agregat<TId>` (`Version` start 1, zdarzenia przez `Zglos()`, `PotwierdzZapis()`), `IZdarzenieDomenowe`, `IIdentyfikator<TWartosc>`, `WyjatekDomenowy`, `NiezmiennikNaruszonyException(kod, opis=null)`, `PoziomMatury`, `RodzajZadania`, `PoziomTrudnosci` (1–5), `GeneratorUuid.Nowy(TimeProvider)` (UUIDv7), `KonwencjaNazw` (oparta na `JsonNamingPolicy`). Testy w `backend/tests/Lemat.Domain.Tests/Common` (Domain 56 zielone). Uruchamianie bez zmian (sekcja 5); ten TASK nie dodaje procesów ani zmiennych.

**Co działa (TASK-013):** `backend/tools/extract-ddl.sh` (wycina bloki A i B z `docs/spec/ddl.md` -> `Persistence/Schema/ddl.sql` + `Persistence/Migrations/Sql/0000_schema.sql … 0007_api.sql`, zasoby osadzone, deterministyczny, `--check`), `LematDbContext` (Npgsql, snake_case, bez encji), migracja `SchematPoczatkowy` (`0001_SchematPoczatkowy`, tylko `migrationBuilder.Sql`, bez `ModelSnapshot`), `MigratorBazy` i `Lemat.Tools migrate` (idempotentne; partycje `decyzja_modelu` bieżący miesiąc + 12), `PostgresFixture` + `SchematBazyTests`. Rzeczywiste liczby w bazie: 61 tabel, 43 typy enum, 15 triggerów, 13 partycji, `konfiguracja_modelu` = 0 wierszy.

**Co działa (TASK-015):** warstwa trwałości w `backend/src/Lemat.Infrastructure/Persistence`: `PersistenceRegistration.DodajPersystencje` (źródło danych Npgsql z enumami i dynamicznym jsonb, `LematDbContext` + fabryka, interceptory, `IJednostkaPracy`; pule/timeouty wg ADR-059 przez `BazaOpcje.CiagPolaczenia(RodzajProcesu)`), konwencje (`Conventions/`: rejestr 43 typów enum PG `MapowanieTypowWyliczeniowych` z translatorem UPPER_SNAKE — mapowanie CLR tylko dla `PoziomMatury`, `RodzajZadania`; konwertery identyfikatorów dla każdego `IIdentyfikator<T>`; `jsonb`; `UpperSnakeEnumConverter` dla kolumn text), interceptory (`ZnacznikiCzasuInterceptor` -> `updated_at`; `WersjaAgregatuInterceptor` -> `PotwierdzZapis()` korzenia, pomija `[BezZmianyWersji]`), `JednostkaPracyEf` (`WykonajAsync` x2, `TransakcjaOtwarta`, `WymagajTransakcji` -> `ZLECENIE_POZA_JEDNOSTKA_PRACY`, `UtworzPolecenie(sql)` w tej samej transakcji), `TlumaczBledowBazy` (P0001 -> `NiezmiennikNaruszonyException`, 23505 -> kod z katalogu, 40001/40P01 i `DbUpdateConcurrencyException` -> `KonfliktWspolbieznosciException`). W `Lemat.Application.Common`: `IJednostkaPracy`, `RodzajProcesu`, `KonfliktWspolbieznosciException`; w Domain: `IKorzenAgregatu`, `BezZmianyWersjiAttribute`. Testy: Infrastructure 50 (łącznie Domain 56, Application 14, Architecture 104, Api 3) zielone.

**Co działa (TASK-016):** moduł Konta. Domena `backend/src/Lemat.Domain/Accounts/` (`Uzytkownik.Zaloz` — Nauczyciel startuje jako `OCZEKUJE_NA_ZATWIERDZENIE`, reszta `AKTYWNE`; przejścia 8.x: zatwierdzenie, blokada, wniosek o usunięcie, przywrócenie; `Uczen.Utworz` wymaga oświadczenia 16+; `Nauczyciel` z flagami; `TokenOdswiezania` z rodziną; `ProbaLogowania`; `GrupaEksperymentu` z `Fnv1a`), aplikacja `backend/src/Lemat.Application/Accounts/` (5 portów repozytoriów, `IKontaFasada`/`KontaFasada`, `DodajModulKonta`), trwałość `Persistence/Configurations/Accounts` i `Persistence/Repositories/Accounts` (If-Match przez `PobierzAsync(id, wersja)`; skrót hasła jako shadow property `haslo_hash`). Testy: 20 testów bazy na PostgreSQL (EMAIL_ZAJETY, CHECK weryfikatora, uzytkownik+uczen w jednej transakcji).

**Ostatnia zmiana: TASK-016** (pliki: `TASKS.md` pod TASK-016). Decyzje: fnv1a 32-bit/UUID big-endian mod 100; skrót hasła poza agregatem; `OstatnieLogowanieAt` z `[BezZmianyWersji]`; klasa testowa `UczenTests` w `StudentTests.cs` (strażnik synonimów); dodano `DzialId`. Weryfikacja: build `-warnaserror` 0/0; testy zielone; `dotnet format` czysto.

**Wytyczne dla TASK-017 (następne MVP):** `Lemat.Domain.Content` — `DzialId` już istnieje w `Lemat.Domain.Content/CurriculumSectionId.cs` (nie duplikuj, dopisz resztę typów); nowe enumy rejestruj w `MapowanieTypowWyliczeniowych.Poczatkowe`; jsonb przez `JsonbSupport` (7.5.1–7.5.3); konfiguracje w `Persistence/Configurations/Content/`, repozytoria w `Persistence/Repositories/Content/`; wzorzec fasady/rejestracji jak `AccountsFacade`/`AccountsRegistration`; testy bazy na `LEMAT_TEST_BAZA`.

**Poprzednia zmiana: TASK-015** (pliki: `TASKS.md` pod TASK-015). Zrobiono: j.w. Weryfikacja: build `-warnaserror` OK; `dotnet test` zielone; `dotnet format` czysto. Decyzje: (1) rejestr enumów: `NazwyTypowDdl` = wszystkie 43 nazwy PG (test porównuje z `pg_type`); kolejne moduły wołają `MapowanieTypowWyliczeniowych.Zarejestruj(typeof(X), "nazwa_pg")` przed zbudowaniem źródła danych/modelu; (2) konwerter `IdentyfikatorConverter<TId,TWartosc>` automatycznie dla value types z `IIdentyfikator<T>` w `Lemat.Domain`; (3) interceptor wersji pomija korzenie Added/Deleted, korzenie z `Version` generowanym przez bazę (Weryfikacja/xmin) i zmiany wyłącznie `[BezZmianyWersji]`/`UpdatedAt`; encje wewnętrzne niebędące owned (FK, `IEncjaWewnetrzna` z ef-core.md 4) NIE są obsługiwane — dodać w TASK-016+ przy pierwszym takim agregacie; (4) konfiguracje agregatów same ustawiają `Version.IsConcurrencyToken()` (ef-core.md 6); (5) nie zaimplementowano (poza zakresem): rozsyłanie zdarzeń domenowych (`IObslugaZdarzenia`, TASK-024/13.6), `IKontekstPracy` (potwierdzenie dzierżawy), `AddInterceptors` NOTIFY i `ZlecaniePracSql` (TASK-024); (6) katalog `ux_*` zawiera tylko pary jednoznaczne z architektury — nieznane 23505 przechodzą bez tłumaczenia, uzupełniać w TASK modułów. Nagłówki 19 plików `.cs` sprawdzone, poprawne. Brak mocków, brak nowych U-xx.

**Wytyczne dla TASK-016 (zrealizowane):** konfiguracje `IEntityTypeConfiguration<T>` w `Persistence/Configurations/Accounts/` (`ApplyConfigurationsFromAssembly` już wpięte); identyfikatory `UzytkownikId/UczenId/NauczycielId : IIdentyfikator<Guid>` (jeden plik na typ), generowane `GeneratorUuid.Nowy(TimeProvider)`; zarejestruj enumy modułu przez `MapowanieTypowWyliczeniowych.Zarejestruj` (walidowane względem listy 43); repozytoria wczytują agregaty przez `AsTracking()` (domyślnie NoTracking); If-Match: `entry.Property(Version).OriginalValue`; ustaw `Version.IsConcurrencyToken()`; dopisz kody `uzytkownik_email_key` -> `EMAIL_ZAJETY` itp. do `KatalogIndeksowUnikalnych`; zapis `uzytkownik` + `uczen` w jednej transakcji przez `IJednostkaPracy` (FK odroczony). Testy na `PostgresFixture`/`LEMAT_TEST_BAZA`.

**Poprzednia zmiana: TASK-014** (pliki: `TASKS.md` pod TASK-014). Zrobiono: j.w. Weryfikacja: build `-warnaserror` OK; testy zielone (Domain 56, Architecture 104, Infrastructure 15); `dotnet format` czysto. Decyzje: (1) `Agregat.Version` rośnie przez publiczne `PotwierdzZapis()` wołane przez `LematDbContext` przy zapisie (3.6 p. 3) — TASK-015 musi to wywołać; `Zglos()` wersji nie zmienia; (2) `NiezmiennikNaruszonyException(kod, opis=null)`, opis domyślnie = kod; (3) `CA1710` wyciszony na `WyjatekDomenowy`; (4) `default(PoziomTrudnosci).Wartosc == 0`; (5) `IIdentyfikator<TWartosc>` to baza dla `*Id`, `Agregat<TId>` ma tylko ograniczenie `struct`. Brak mocków, brak nowych U-xx. Nagłówki 12 plików `.cs` sprawdzone, poprawne.

**Poprzednia zmiana: TASK-013** (pliki: `TASKS.md` pod TASK-013). Zrobiono: j.w. Weryfikacja: build `-warnaserror` 0/0; testy zielone (Infrastructure.Tests 15, Architecture.Tests 104) z Testcontainers i z `LEMAT_TEST_BAZA`; `dotnet format` czysto; `extract-ddl.sh` deterministyczny. Decyzje: tabela historii `__historia_migracji`; rola `lemat_migracje` nie utworzona (sekcja 6, pkt 0d); testy zgodności modelu EF z katalogiem (`ModelEfZgodnyZKatalogiem`, `KazdyTriggerWKatalogu`, `ZadnaMigracjaNieGenerujeSchematu`) dojdą z TASK-037/encjami. Zmiana DDL: edycja `docs/spec/ddl.md` -> `backend/tools/extract-ddl.sh` -> nowa migracja (kolejne pliki SQL); test `ExtractDdlJestDeterministycznyIZgodnyZRepozytorium` pilnuje zgodności. Kolejne TASK z encjami: `IEntityTypeConfiguration` w `Lemat.Infrastructure` (`ApplyConfigurationsFromAssembly` podpięte), w testach `PostgresFixture` + `[Collection(PostgresKolekcjaTestow.Nazwa)]`. Nagłówki 9 plików `.cs` sprawdzone, poprawne. Brak mocków i nowych U-xx.

**Poprzednia zmiana: TASK-007** (pliki: `TASKS.md` pod TASK-007). Skrypty `scripts/verify-*.sh` i `.github/workflows/ci.yml` (szczegóły: TASKS.md).

**Poprzednia zmiana: TASK-006** (pliki: `TASKS.md` pod TASK-006). Zrobiono: j.w.; `next.config.ts` (`output: standalone`, `rewrites` `/api/:path*` -> `LEMAT_API_URL`); reguła ESLint `react/jsx-no-literals` (teksty tylko z `messages/pl-PL.json`; komponenty `src/ui` nie znają i18n — etykiety przez props); `gen:api` i `e2e` to placeholdery; brak `proxy.ts`, PWA/Serwist i dependency-cruiser (TASK-012/POST-MVP, U-12); bez `@vitejs/plugin-react` (konflikt typów vite 7/8 w vitest 3 — JSX przez esbuild + `jsx: react-jsx`); fonty KaTeX przez `katex.min.css` w `globals.css`. Nagłówki sprawdzone: wszystkie 25 plików `.ts/.tsx/.mjs` mają poprawne (`check-headers` OK). `node_modules`/`.next` ignorowane przez git (root `.gitignore` + `frontend/.gitignore`).

**Szczegóły TASK-007:** (pliki: `TASKS.md` pod TASK-007). Zrobiono: `scripts/verify-backend.sh`, `scripts/verify-frontend.sh`, `scripts/verify-all.sh` (wykonywalne), `.github/workflows/ci.yml` (job backend: postgres:16 na 5433 jako zapas dla `LEMAT_TEST_BAZA` + Testcontainers, cache NuGet przez `actions/cache` po hash csproj/`Directory.Packages.props` — brak `packages.lock.json`, więc `setup-dotnet` bez cache; job frontend: Node 22 + cache npm), `README.md` — sekcja „Szybki start”. Weryfikacja: `scripts/verify-all.sh` -> kod 0, powtórny przebieg frontendu OK; `ci.yml`: `yaml.safe_load` + `yamllint` OK (`actionlint` niedostępny), `bash -n` OK. Zmienne środowiskowe: bez zmian (`LEMAT_TEST_BAZA` w CI nieustawiane — testy używają Testcontainers). Brak mocków i nowych U-xx.

**Poprzednia zmiana: TASK-005** (pliki: `TASKS.md` pod TASK-005). Zrobiono: `docker-compose.yml` (postgres:16, wolumen, healthcheck, port `${LEMAT_PG_PORT:-5433}`, zmienne `LEMAT_PG_UZYTKOWNIK`/`LEMAT_PG_HASLO`), `docker/postgres/init-db.sh` (baza `lemat_test`, role `lemat_api/worker/scheduler` z LOGIN wg ddl.md 0005, rozszerzenia `citext`/`pg_trgm`), `.env.example` (wszystkie 14 nazw z `MapowanieZmiennych` + `LEMAT_PG_*`, `LEMAT_TEST_BAZA`, `LEMAT_API_URL`; bez sekretów), `scripts/db-up.sh`, `db-reset.sh`, `db-local-setup.sh` (idempotentny), `Lemat:Baza:Polaczenie` w `appsettings.Development.json` trzech hostów. Weryfikacja: kontener `healthy`, `psql` na 5433 do `lemat_dev`, rozszerzenia obecne, `db-reset.sh` -> 0 tabel, `db-local-setup.sh` OK; build `-warnaserror` i testy zielone (Architecture 104, Application 14, Api 3). Brak mocków, kod `.cs` bez zmian (nagłówki: nie dotyczy).

**Poprzednia zmiana: TASK-004** (pliki: `TASKS.md` pod TASK-004). Zrobiono: j.w.; testy `LematOpcjeTests` (Application 14) i `LematOpcjeHostyTests` (Architecture 104 łącznie). Weryfikacja: build `-warnaserror`, testy zielone, format OK, `lemat-v1.json` bez zmian. Odstępstwa: pakiety Microsoft.Extensions.Options/DI.Abstractions/Logging.Abstractions -> 10.0.14 + Options.ConfigurationExtensions 10.0.14 (NU1605); `ApiHost` pomija `ValidateOnStart`, gdy entry assembly = `GetDocument.Insider` (generator OpenAPI); `Zdjecia:DozwoloneTypyMime` nie jest w JSON (binder dopisywałby do domyślnych); `Obrazy/Symbolika/Email/Magazyn:Typ` akceptują na razie tylko `prosta/prosta/log/lokalny`. Nagłówki 17 plików sprawdzone, poprawne. Rozstrzygnięte: OQ-06/P-01 (nazwy kluczy i zmiennych).

**Poprzednia zmiana: TASK-003** (pliki: `TASKS.md` pod TASK-003). Zrobiono: hosty przez klasy `ApiHost` (`CreateBuilder`, `MapEndpoints`), `WorkerHost`/`SchedulerHost` (`CreateBuilder`, `MapHealth`) w `Configuration/`; endpointy w `Endpoints/` mapowane z `ApiHost.MapEndpoints` (grupy `MapGroup("/api/v1/...")`), DI w `Configuration`; DTO w `Contracts/`; `CommandDispatcher` + `IPolecenie`; OpenAPI (`Microsoft.AspNetCore.OpenApi`) w `backend/openapi/lemat-v1.json`; Worker/Scheduler z `FrameworkReference Microsoft.AspNetCore.App`; testy `HealthEndpointTests`, `KontenerDiTests` (ValidateOnBuild/ValidateScopes). Weryfikacja: build `-warnaserror`, testy (Architecture 96, Api 3), format OK, ręcznie `dotnet run` Api 200. Nagłówki wszystkich 11 plików `.cs` sprawdzone, poprawne.

**Poprzednia zmiana: TASK-002.** Zrobiono: `backend/tests` z 7 projektami (xUnit v3 3.2.2, Shouldly, NSubstitute, FsCheck.Xunit.v3, Testcontainers.PostgreSql 4.16.0, WireMock.Net 2.18.0 z `NoWarn NU1608` w Models.Tests, Mvc.Testing, NetArchTest.Rules 1.3.2, Roslyn 5.0.0) w `Directory.Packages.props` i `Lemat.slnx`; `backend/tests/Directory.Build.props` (OutputType Exe, CA1707 wyłączone). Weryfikacja: build `-warnaserror` OK, `dotnet test` zielone, format OK. Decyzje: testy architektury opierają się na referencjach/zespołach, więc działają na pustych projektach; testy negatywne używają syntetycznego grafu/typu (`Layers/Synthetic`); `RepositoryLocator` woła `git ls-files`.

**Poprzednia zmiana: TASK-001.** Decyzje developera:
- `backend/BannedSymbols.txt` (zakaz `DateTime.Now/UtcNow`, `DateTimeOffset.Now/UtcNow`) dotyczy całego `backend/src`; `backend/BannedSymbols.Sdk.txt` (typy SDK dostawców modeli: `OpenAI.*`, `Azure.AI.*`, `Anthropic.*`) dołączany do wszystkich projektów `src` poza `Lemat.Models` (przez `backend/src/Directory.Build.props`, który dodaje też `Microsoft.CodeAnalysis.BannedApiAnalyzers` z `PrivateAssets=all`).
- Konsekwencja: `Lemat.Domain` ma w grafie ukryty `PackageReference` analizatora (`PrivateAssets=all`, nie trafia do zależności wynikowych) — test `ProjektDomenyBezPakietow` (TASK-002) ma to uwzględnić (ignorować pakiety analizatorów / sprawdzać zależności assembly, nie `PackageReference` w pliku).
- Podniesione wersje pakietów z powodu ostrzeżeń NU1901–NU1903 (przy `-warnaserror`): MailKit 4.18.2, AWSSDK.S3 4.0.104.8, OpenTelemetry.Extensions.Hosting 1.20.1, FluentValidation 12.2.0, Microsoft.IdentityModel.JsonWebTokens 8.23.0; ponadto Cronos 0.13.0, Microsoft.ML/LightGbm 4.0.2 (wszystkie w `backend/Directory.Packages.props`).
- Nagłówki Living Documentation sprawdzone w 4 plikach `Program.cs` — poprawne.

`handoff-guardian` po każdym TASK dopisuje tu: co zostało zrobione, jak uruchomić, zmienione zmienne środowiskowe, nowe U-xx, otwarte pytania i wskazówki dla następnego TASK.

---

## 8. Otwarte pytania (do rozstrzygnięcia przez właściciela / architekta)

| ID | Pytanie | Tymczasowe postępowanie |
|---|---|---|
| OQ-01 | Dostawca `anthropic` nie występuje w ADR-058 — dodać ADR-076 (rozszerzenie rejestru adapterów)? Czy używać oficjalnego SDK Anthropic z adapterem `IChatClient`, czy endpointu zgodnego z OpenAI? | TASK-042 implementuje za interfejsem; jeśli pakiet niedostępny — test `Skip` z uzasadnieniem w raporcie |
| OQ-02 | Czy uproszczenia U-01 … U-20 zapisać jako ADR „Profil MVP” (docs są tylko do odczytu dla agentów implementacyjnych)? | Obowiązuje ta lista w `HANDOFF.md` |
| OQ-03 | Wymaganie 18.5 (c)/(m) każe tworzyć pełny test słownika i manifest nazw plików przed kodem domeny; plan MVP przesuwa je na POST-MVP (TASK-008, 009) i zastępuje lekkimi testami (TASK-002). Akceptacja odstępstwa? | Reguły nazewnictwa obowiązują ręcznie; `handoff-guardian` sprawdza nazwy w każdym TASK |
| OQ-04 | Architektura wskazuje Node 24 LTS i `next build --webpack` (Serwist); lokalnie jest Node 22, a PWA jest POST-MVP — w MVP używamy domyślnego `next build`. Potwierdzić. | Node 22, `next build` bez `--webpack` do TASK-105 |
| OQ-05 | Nazwa tymczasowa ścieżki lokalnego magazynu `GET /magazyn-lokalny/{token}` (U-03) jest spoza słownika | `// SLOWNIK-PROPOZYCJA`; zastąpiona podpisanym URL S3 w TASK-057 |
| OQ-06 | Nazwy zmiennych/opcji z P-01 i klucze konfiguracji (`Lemat:Konta:AutozatwierdzanieNauczycieli`, `Lemat:Poprawki:ZawszeDoWeryfikacji`, `Lemat:Magazyn:*`) nie występują w sekcji 22 | Wprowadzone w TASK-004 (`LematOpcje`, `MapowanieZmiennych`) jako rozszerzenie; nazwy ustalone — czeka na formalne zatwierdzenie |
| OQ-07 | Czy kolejka `GLOBALNA` ma istnieć w MVP bez pseudonimizacji (U-07)? Alternatywa: Uczeń bez Klasy w MVP w ogóle nie może przesyłać zdjęć (`LEMAT_REJESTRACJA_BEZ_KLASY=false`) | Domyślnie dozwolone na danych demo; do decyzji przed pilotażem |
| OQ-08 | Wagi egzaminacyjne Umiejętności (7.4) w MVP równe — czy wystarczy do testów rekomendacji? | Tak (U-19) |
| OQ-09 | Definicja fnv1a (szerokość, wejście) nie jest podana w dokumentach (`procent_grupy_eksperymentu`, T-5) | TASK-016: 32-bit nad 16 bajtami UUID big-endian, mod 100 (`Fnv1a.Skrot32`); wektory policzone niezależnie; potwierdzić z PO |

## 9. Propozycje do słownika

Brak nowych pojęć domenowych. Nazwy tymczasowe poza słownikiem: ścieżka lokalnego magazynu (OQ-05), klucze opcji z OQ-06. Wszystkie nowe typy techniczne (`Mock*`, `Fake*`, `Prosta*`, `*Lokalny`)
mieszczą się w słowach technicznych 3.1 p. 7. Dopisuj tu kolejne propozycje w formacie: pojęcie, definicja, identyfikator, przyczyna.

---

## 10. Gotowy prompt dla kolejnego dewelopera

### 10.1 Następne TASK: TASK-017 (kopiuj do agenta `developer`)

```text
Jesteś Senior Developerem .NET i Next.js w projekcie Lemat (repozytorium: /home/user/lemat).
Zrealizuj dokładnie jedno zadanie planu: TASK-017 z TASKS.md ("Moduł Treści: Działy, Umiejętności, Zadania z wersjami i Kryteriami, Wkładki").

Najpierw przeczytaj: CLAUDE.md, HANDOFF.md (sekcje 5, 6, 7: wytyczne dla TASK-017), docs/CODEBASE_MAP.md oraz opis TASK-017 w TASKS.md.
Architekturę czytaj WYŁĄCZNIE we wskazanych sekcjach (3.5 wiersze 3, 6, 7, 11, 14, 36, 37, 39, 41; 7.4, 7.5.1–7.5.3; 8.4; 6.2; ADR-012, ADR-063; docs/spec/ef-core.md); docs/ jest tylko do odczytu.

Wymagania:
- Lemat.Domain.Content (Dzial, Umiejetnosc, Zadanie + ZadanieWersja, KryteriumOceniania, OpcjeZamkniete, WkladkaTeoretyczna itd.; DzialId już istnieje), Lemat.Application.Content (ITresciFasada, repozytoria), konfiguracje EF (jsonb) i repozytoria w Lemat.Infrastructure/Persistence wg wzorca z modułu Konta (TASK-016).
- Testy wg DoD z TASKS.md (triggery trg_suma_kryteriow, trg_umiejetnosc_glowna_w_dziale, round-trip jsonb, OpcjeZamkniete.Ocen, wersjonowanie Zadania); testy bazy na LEMAT_TEST_BAZA lub Docker.
- Nagłówki @Role/@DependsOn/@UsedBy/@Task w każdym nowym .cs; uruchom scripts/verify-all.sh (kod 0).
- Nie edytuj TASKS.md, HANDOFF.md, docs/. Nie commituj. Nie wychodź poza zakres TASK-017.
Zwróć raport: lista plików, wyniki weryfikacji, decyzje i odstępstwa, wskazówki dla kolejnego TASK.
```

### 10.2 Szablon pętli dla orkiestratora (jedno TASK = jedna iteracja)

```text
1. Wybierz najniższy TASK-XXX z TASKS.md ze statusem [ ], którego zależności są [x] (najpierw wszystkie [MVP]).
2. Uruchom agenta `developer` z promptem: "Zrealizuj TASK-XXX z TASKS.md. Przeczytaj CLAUDE.md, HANDOFF.md, docs/CODEBASE_MAP.md i sekcje z pola 'Realizuje'. Nagłówki @Role/@DependsOn/@UsedBy/@Task w każdym pliku .cs/.ts/.tsx. Zweryfikuj build/test/lint. Nie edytuj TASKS.md/HANDOFF.md/docs. Nie commituj. Zwróć raport."
3. Po sukcesie uruchom agenta `handoff-guardian`: "Zaktualizuj docs/CODEBASE_MAP.md, TASKS.md ([x] + ścieżki plików + wynik weryfikacji) i HANDOFF.md dla TASK-XXX; sprawdź nagłówki Living Documentation."
4. Zweryfikuj samodzielnie: scripts/verify-all.sh (po TASK-007) lub komendy z CLAUDE.md pkt 10.
5. Jeden commit: "feat(TASK-XXX): <opis> + updated codebase map & handoff" (+ stopka atrybucji środowiska). Bez push.
6. Gdy developer zgłasza blokadę (brak/sprzeczność w architekturze) — dopisz do HANDOFF.md → "Otwarte pytania" i przejdź do następnego TASK niezależnego od zablokowanego.
```

---

## 11. Mapa dokumentów

| Dokument | Rola |
|---|---|
| `CLAUDE.md` | reguły pracy, słownik, protokół Living Documentation, komendy |
| `TASKS.md` | backlog 120 pozycji (60 MVP) + macierz pokrycia FR/NFR |
| `docs/CODEBASE_MAP.md` | żywa mapa: strona ↔ endpoint ↔ przypadek użycia ↔ Agent ↔ tabela, diagramy, rejestr plików |
| `docs/ARCHITECTURE_DETAILED.md` v3.5 | architektura (czytać wybiórczo) |
| `docs/spec/*` | DDL, EF Core, API/TS, prompty |
| `docs/adr/*` | uzasadnienia decyzji (ADR-001 … ADR-075) |
| `docs/CRITIQUE_LOG.md`, `docs/ARCHITECTURE_CHANGELOG.md` | historia przeglądów (ostatnia iteracja: C9, wszystkie uwagi zaadresowane w v3.5) |
| `.claude/agents/*` | `architect`, `critic`, `planner`, `developer`, `handoff-guardian` |
