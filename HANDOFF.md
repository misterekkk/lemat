# HANDOFF.md — przekazanie projektu Lemat

| Pole | Wartość |
|---|---|
| Data | 2026-11-01 |
| Etap | **Implementacja w toku** — ukończono TASK-001 … TASK-007, TASK-013 … TASK-019 (14 z 120 pozycji `TASKS.md`; 14 z 60 `[MVP]`). Zakres zawężony do POC (decyzja właściciela) |
| Cel właściciela | **Działające MVP jak najszybciej**, kompletne end-to-end (Uczeń i Nauczyciel), całość uruchamialna lokalnie bez kluczy API (Mock) |
| Architektura | `docs/ARCHITECTURE_DETAILED.md` v3.5; spec: `docs/spec/{ddl,ef-core,api,prompts}.md`; ADR-001 … ADR-075 w `docs/adr/` |
| Stos | .NET 10 (`backend/`), Next.js App Router + TypeScript (`frontend/`), PostgreSQL 16, REST + SSE, Microsoft.Extensions.AI + Semantic Kernel |
| Kod | moduły Profil i Rekomendacje: `Lemat.Domain.{Profile,Recommendations}`, `Lemat.Application.{Profile,Recommendations}`, konfiguracje EF i repozytoria (TASK-019) + moduły Klasy, Modele, Kalibracja: `Lemat.Domain.{Classes,Models,Calibration}`, `Lemat.Application.{Classes,Models,Calibration}`, konfiguracje EF i repozytoria (TASK-018) + moduł Treści (TASK-017) + moduł Konta: `Lemat.Domain.Accounts`, `Lemat.Application.Accounts`, konfiguracje EF i repozytoria (TASK-016) + warstwa trwałości EF Core: konwencje, `IJednostkaPracy`, `TlumaczBledowBazy`, `DodajPersystencje` (TASK-015) + `Lemat.Domain.Common` (TASK-014) + szkielet solucji `backend/` (TASK-001) + 7 projektów testowych (TASK-002) + hosty z `/zdrowie` i OpenAPI (TASK-003) + `LematOpcje` (TASK-004) + PostgreSQL 16 w `docker-compose.yml` i skrypty `scripts/db-*.sh` (TASK-005); szkielet `frontend/` (TASK-006: Next.js, next-intl pl-PL, KaTeX, `src/ui`); skrypty `verify-*` i CI (TASK-007); schemat bazy: `extract-ddl.sh`, migracja SQL, `Lemat.Tools migrate`, `PostgresFixture` (TASK-013); dokumentacja i definicje agentów w `.claude/agents/` |
| Następne TASK | **POC, dwie ścieżki równolegle** (sekcja 12): `[POC-A]` backend TASK-121 → 130, `[POC-B]` frontend TASK-131 → 139, potem `[POC-INT]` TASK-140. Kontrakt: `docs/POC_CONTRACT.md` (POC-1.0). Pozostałe `[MVP]` po POC |
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
| U-21 | **POC:** tylko rola `UCZEN` (rejestracja `NAUCZYCIEL` → `422 ROLA_NIEOBSLUGIWANA`); `test_diagnostyczny` zawsze `BRAK`, `klasy` zawsze `[]` | zakres POC (decyzja właściciela) | nie | TASK-072, 073, 074 |
| U-22 | **POC:** sesja bez okna tolerancji rotacji refresh tokenu (30 s), bez wielokartowości i bez limitów prób logowania | szybkość | tak | TASK-091, 104 |
| U-23 | **POC:** rozszerzenia kontraktu spoza 12.3: `GET /zadania` dla Ucznia, `GET /zadania/{id}` bez wymaganego kontekstu, `GET /uczniowie/ja/profil-uproszczony`, `GET /magazyn-lokalny/{token}` | brak Ścieżki nauki i Profilu Elo w POC | nie | TASK-075, 076, 083, 084 |
| U-24 | **POC:** serwer tworzy techniczny `element_sciezki` (`ROZPOCZETY`) dla każdego Rozwiązania (CHECK DDL wymaga kontekstu próby) | brak Rekomendacji | nie | TASK-076, 077 |
| U-25 | **POC:** zdjęcia wyłącznie JPEG (CHECK DDL `typ_mime = 'image/jpeg'`), konwersja do JPEG po stronie klienta; wymiary z nagłówka JPEG (rozszerza U-02) | brak normalizacji obrazów | tak | TASK-056 |
| U-26 | **POC:** potok `ROZPOZNAJ_PISMO`/`OCEN_ROZWIAZANIE` jako jedna pętla odpytywania `praca_w_tle` w `Lemat.Worker` (bez `LISTEN/NOTIFY`, bez pul), handlery minimalne z deterministycznym Mockiem zamiast Agentów, brak Filtra danych osobowych, trasowania T-1 … T-7, Weryfikacji i Poprawek Ucznia; odpytywanie zamiast SSE | najprostsze sensowne uproszczenie | tak | TASK-024, 043 … 050, 079 |
| U-27 | **POC:** Wskazówka stała (dane mocka), zapisana razem z Oceną; bez Agenta wyjaśniającego i strażnika wycieku | brak Agenta | tak | TASK-046, 050 |
| U-28 | **POC:** `wynik_zadania` zapisywany, ale Profil ucznia (Elo) nie jest aktualizowany; „profil uproszczony” to agregat po Ocenach | brak `AKTUALIZUJ_PROFIL` | nie | TASK-050, 075 |
| U-29 | **POC:** `Lemat.Tools seed-poc` (13 Działów, 5 Umiejętności, 5 Zadań, 2 konta, stałe hasła demo) zamiast `seed-dev` i TASK-025/027/028 | szybkość | tak | TASK-025 … 028 |
| U-30 | **POC (frontend):** typy TS utrzymywane ręcznie według kontraktu i cienki klient `fetch` zamiast `openapi-typescript`/`openapi-fetch`; MSW jako `devDependency` (OQ-12) | brak generowanego OpenAPI | nie | TASK-081 |
| U-31 | **POC:** `kod_reguly` w `bledy_pol` to lista zamknięta z `docs/POC_CONTRACT.md` 1.1 (OQ-13) | 12.2 nie definiuje listy | nie | TASK-071 |

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

0k. **Po TASK-128 (POC-A):** brak kluczy ani nowych zmiennych obowiązkowych. Mocki: `MockAgentRozpoznajacy`/`MockAgentOceniajacy` (dane z tabel 7.2/7.3 kontraktu POC, stała `pewnosc_oceny = 0.995`, Wskazówka z szablonu); dostawca ≠ `mock` przerywa start workera. Do uruchomienia pełnego potoku lokalnie potrzebne dwa procesy z tą samą `LEMAT_BAZA`: `dotnet run --project backend/src/Lemat.Api` i `dotnet run --project backend/src/Lemat.Worker` (bez workera Rozwiązanie zostaje w PRZESLANE/ZATWIERDZONE). Opcjonalnie `Lemat__Poc__*` (opóźnienia mocka 3 s + 2 s). Do wiedzy: praca MARTWA nie zmienia statusu Rozwiązania; partycje `decyzja_modelu` obejmują 4 miesiące od migracji (długo żyjąca baza dev: odtworzyć bazę lub czekać na `UTRZYMAJ_PARTYCJE`). Testy bazy: `LEMAT_TEST_BAZA` (np. `Host=localhost;Port=5432;Database=lemat_test;Username=lemat;Password=lemat_dev`, `scripts/db-local-setup.sh`).
0j. **Po TASK-122 (POC-A):** brak kluczy ani nowych zmiennych obowiązkowych. Mocki: konfiguracje modelu `mock` (wersja_promptu `mock-v1`), kalibrator tożsamościowy, próg 0.95 (źródło ADMINISTRATOR). Hasła kont demo są wypisywane wyłącznie w komunikacie `seed-poc` (w bazie tylko skrót). Do potwierdzenia: (a) `NoweZadanie` dostało opcjonalne `WersjaId` (stałe `zadanie_wersja_id` z kontraktu); (b) typy wyniku wzorcowego WYRAZENIE/ZBIOR (enum domeny nie ma LICZBA); (c) zadania seedu: pochodzenie BAZOWE_NAUCZYCIEL, autor_id NULL; (d) Production wykrywane po `LEMAT_SRODOWISKO`; (e) seed tworzy też `uczen`/`profil_ucznia`/`sciezka_nauki` dla kont demo.
0i. **Po TASK-018:** brak akcji obowiązkowych; brak mocków i nowych zmiennych `LEMAT_*`. Testy bazy: `LEMAT_TEST_BAZA` (np. `Host=localhost;Port=5432;Database=lemat_test;Username=lemat;Password=lemat_dev`; lokalnie `pg_ctlcluster 16 main start` + `scripts/db-local-setup.sh`) albo Docker. Do potwierdzenia (informacyjnie): (a) nowe kody błędów domeny spoza katalogu 12.2 do zmapowania w TASK-071 (`KLASA_NIE_ZNALEZIONA`, `KLASA_NAZWA_NIEPRAWIDLOWA`, `KOD_ZAPROSZENIA_NIEPRAWIDLOWY/ZAJETY/NIEWAZNY`, `PLAN_PYTAN_NIEPRAWIDLOWY`, `PYTANIE_NIEPRAWIDLOWE`, `ROZKLAD_NIEPRAWIDLOWY`, `DECYZJA_*`, `DOSTAWCA_NIEZNANY`, `PARAMETRY_MODELU_NIEPRAWIDLOWE`, `KONFIGURACJA_MODELU_*`, `PUNKTY_KALIBRATORA_NIEPRAWIDLOWE`, `KALIBRATOR_*`, `PROG_*`); (b) wąski wyjątek strażnika słownika dla `PytanieScore` / `TypPytania.Score` (typ pytania Jev SCORE, CLAUDE.md 4.C.3); (c) fasady `IKlasyFasada`/`IModeleFasada`/`IKalibracjaFasada` w `Lemat.Application.<Moduł>` (nie w Domain, jak sugerował opis TASK); (d) dostawca `anthropic` dopuszczony w `ModelProviders` (U-13); (e) hosty nie wołają jeszcze `DodajModulKlasy/Modele/Kalibracji`; (f) brak aktywnego progu => `PobierzAktywnyProgAsync` zwraca `null` (trasa do Weryfikacji, Z-3) — seed-dev ma ustawić próg 0.95/0.98, dowód 1.000 (TASK-026).
0h. **Po TASK-017:** brak akcji obowiązkowych; brak mocków i nowych zmiennych `LEMAT_*`. Testy bazy: `LEMAT_TEST_BAZA` albo Docker. Do potwierdzenia (informacyjnie): (a) nowe kody błędów domeny spoza katalogu 12.2 (`KRYTERIA_NIESPOJNE`, `ZA_DUZO_KRYTERIOW`, `UMIEJETNOSC_GLOWNA_POZA_DZIALEM`, `ZADANIE_*`, `OPCJE_ZAMKNIETE_*`, `WKLADKA_*`, `WKLADKA_OPUBLIKOWANA_ISTNIEJE`, `WYNIK_WZORCOWY_*`) do zmapowania w TASK-071; (b) jsonb zapisywany przez DTO `ContentJson` (nie `OwnsOne.ToJson`) — akceptacja odstępstwa; (c) dane referencyjne (13 Działów, Umiejętności) NIE są ładowane — to TASK-025; (d) hosty nie wołają jeszcze `DodajModulTresci`.
0g. **Po TASK-016:** brak akcji obowiązkowych; brak mocków i nowych zmiennych `LEMAT_*`. Testy bazy: `LEMAT_TEST_BAZA` (np. `Host=localhost;Port=5432;Database=lemat_test;Username=lemat;Password=lemat_dev`) albo Docker. Do potwierdzenia: (a) definicja fnv1a (przyjęto 32-bit nad 16 bajtami UUID big-endian, mod 100; ten sam `Fnv1a.Skrot32` posłuży T-5 w TASK-021), OQ-09; (b) kody błędów domeny spoza katalogu 12.2 do zmapowania w TASK-071 (m.in. `EMAIL_NIEPRAWIDLOWY`, `KONTO_NIEPRAWIDLOWA_ROLA`, `OSWIADCZENIE_WIEKU_WYMAGANE`, `HASLO_HASH_WYMAGANY`, `TOKEN_*`, `KONTO_JUZ_USUWANE`, `PRZYWROCENIE_NIEMOZLIWE`, `UMOWA_POUFNOSCI_WYMAGANA`, `DEKLAROWANY_CZAS_ZA_KROTKI`); (c) hasher argon2id i rotacja tokenów z oknem tolerancji (ADR-057) — TASK-072; (d) hosty jeszcze nie wołają `DodajPersystencje`/`DodajModulKonta` (podpiąć przy pierwszym endpoincie, TASK-071/072).
0h. **Po TASK-019:** brak akcji obowiązkowych; brak mocków i nowych zmiennych `LEMAT_*`. Testy bazy: `LEMAT_TEST_BAZA` albo Docker. Do potwierdzenia: OQ-10 (poziom DOMYŚLNY 0.310 wg wzoru vs 0.5 w przykładzie), OQ-11 (kody błędów spoza 12.2).
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
| 1. Szkielet i środowisko (001 … 012) | 7 / 7 | 0 / 5 | faza MVP ukończona |
| 2. Model domenowy i baza (013 … 037) | 7 / 16 | 0 / 9 | TASK-013 … 019 gotowe; dalej POC (TASK-121 … 130, `[POC-A]`) |
| 3. Agenci AI i prace w tle (038 … 070) | 0 / 14 | 0 / 19 | po POC (TASK-121 … 139) |
| 4. API i integracja z Next.js (071 … 108) | 0 / 19 | 0 / 19 | po fazie 3 (dla części API wystarcza TASK-015, 016) |
| 5. Testy i dopracowanie (109 … 120) | 0 / 4 | 0 / 8 | po TASK-089 |

**Zakres i kolejność:** właściciel zawęził zakres do POC (rejestracja, logowanie, profil, Zadania z przesłaniem zdjęcia i zmockowaną Oceną; bez Testu diagnostycznego, rekomendacji, Weryfikacji, Klas). Plan: `TASKS.md` sekcja „POC (priorytet właściciela)” (TASK-121 … 140), kontrakt `docs/POC_CONTRACT.md`, podział pracy na 2 osoby — sekcja 12 poniżej. Zadania POC nie wchodzą do liczników tabeli i są częściową realizacją zadań MVP (mapowanie w `TASKS.md`); pozostałe `[MVP]` po POC. Tabela „Stan prac” aktualizowana jest po scaleniu obu ścieżek (TASK-140); do tego czasu stan ścieżek jest w 12.4.

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

**Co działa (TASK-017):** moduł Treści. Domena `backend/src/Lemat.Domain/Content/` (`Zadanie` + `ZadanieWersja`/`TrescWersji` z wersjonowaniem ADR-012, `KryteriumOceniania`, `RozwiazanieWzorcowe`, `WynikKoncowyWzorcowy`, `OpcjeZamkniete.Ocen` dla `WYBOR_JEDNOKROTNY`/`PRAWDA_FALSZ`, `ZasadyKryteriow`, `WkladkaTeoretyczna`, `Dzial`, `Umiejetnosc`, `KalibracjaTrudnosci`), aplikacja `backend/src/Lemat.Application/Content/` (`ITresciFasada`: `PobierzZadanieDoOcenyAsync`, `PobierzZadanieUczniaAsync`, `PobierzKandydatowDoboruAsync`, `ListaDzialowAsync`, `ListaUmiejetnosciAsync`, `PobierzOpublikowanaWkladkeAsync`; 3 porty repozytoriów; `DodajModulTresci`), trwałość `Persistence/Configurations/Content/` + `Persistence/Repositories/Content/`. Testy: Domain 179, Application 29, Infrastructure 103 (33 testy bazy Treści), Architecture 104.

**Co działa (TASK-018):** moduły Klasy, Modele, Kalibracja. Domena `backend/src/Lemat.Domain/Classes/` (`Klasa` z kodem zaproszenia i ważnością, archiwizacją, przekazaniem; `CzlonkostwoKlasy` z unikalnością aktywnego członkostwa; `KodZaproszenia` + generator), `Models/` (`KonfiguracjaModelu` z `RolaModelu` x8, `ParametryModelu`, `DecyzjaModelu` + `DecyzjaSkladowa`, `PlanPytan`/`PytanieWPlanie` z walidacją zależności ADR-026, `Pytanie` Noul/Score/Choice, `KontekstDecyzji`, `RezultatPytan`), `Calibration/` (`KalibratorPewnosci` tożsamościowy, `ProgPewnosci` 0.800–0.990 lub 1.000, dowód wymusza 1.000, `MigawkaProgu`, `MigawkaKalibratora`); aplikacja `backend/src/Lemat.Application/{Classes,Models,Calibration}/` (fasady `IKlasyFasada`, `IModeleFasada`, `IKalibracjaFasada`, porty repozytoriów, `TerminPlanu`, rejestracje `DodajModulKlasy/Modele/Kalibracji`); trwałość w `Persistence/Configurations|Repositories/{Classes,Models,Calibration}` (6 konfiguracji, jsonb przez `ModelsJson`/`CalibrationJson`, 5 repozytoriów); aktywacja konfiguracji sprawdza A-07 (`BRAK_RETENCJI_DOSTAWCY`) i mock w Production. Testy: Domain 269, Application 52, Architecture 106, Infrastructure 140 (w tym zapis `DecyzjaModelu` do partycji miesięcznej i odtworzenie rozkładu).

**Co działa (TASK-019):** moduły Profil i Rekomendacje. Domena `backend/src/Lemat.Domain/Profile/` (`ProfilUcznia` + `ZastosowanieWyniku`, `OpanowanieUmiejetnosci` z inicjalizacją `DOMYSLNA`, `WynikZadania` z źródłem, wagą 0.5 dla Oceny wstępnej i unieważnieniem, `TestDiagnostyczny` `W_TOKU`/`ZAKONCZONY`/`POMINIETY` + `PozycjaTestuDiagnostycznego`, czysty `ModelElo` 10.1.3: aktualizacja `theta`, `trudnosc_b`, propagacja w Dziale, K = 0.8, `poziom_z_b`) oraz `backend/src/Lemat.Domain/Recommendations/` (`SciezkaNauki` + propozycje, `ElementSciezki` z maszyną stanów 8.5, `Rekomendacja` z `RegulaRekomendacji` ↔ kody R-…, `Seria`); aplikacja `Lemat.Application.Profile`/`Recommendations` (porty repozytoriów, `IExerciseEloParameters`, `IDifficultyCalibrationUpdater`, `IProfilFasada`/`ProfilFasada`, `IRekomendacjeFasada`/`RekomendacjeFasada`, `DodajModulProfil`/`DodajModulRekomendacje`); trwałość w `Persistence/Configurations/{Profile,Recommendations}` i `Persistence/Repositories/{Profile,Recommendations}` (kalibracja trudności atomowym SQL + wpis audytu `POZIOM_TRUDNOSCI_SKALIBROWANY`, wymaga otwartej transakcji); `AggregateVersionInterceptor` podnosi `version` korzenia także dla encji wewnętrznych z FK 1:N (`ElementSciezki`). Testy: Domain 394, Infrastructure 167 (Profil 15, Rekomendacje 12) zielone. Uruchamianie bez zmian (sekcja 5); brak nowych procesów i zmiennych środowiskowych; hosty jeszcze nie wołają `DodajModulProfil`/`DodajModulRekomendacje`.

**Ostatnia zmiana: TASK-019** (pliki: `TASKS.md` pod TASK-019). Decyzje: fasady w `Lemat.Application.<Moduł>`; `theta`/ułamek/poziom zaokrąglane do 3 miejsc; poziom DOMYŚLNY = sigma(-0.8) = 0.310 wg wzoru (OQ-10); K liczone z `liczba_wynikow` każdej Umiejętności osobno; `OdpowiedzId`/`OcenaId`/`PrzypisanieId` jako `Guid?` do TASK-020/021/034 (w testach bazy zdjęte FK `wynik_zadania` -> `odpowiedz`/`ocena`; po wprowadzeniu typów rozważyć zamianę na silne typy i przywrócenie FK); `Seria` z kolumn `seria_*` (nie `OwnsOne`); `Rekomendacja.cechy/decyzja/uzasadnienie_fakty` jako JSON w jsonb; odtworzenie profilu kalibruje trudność tylko dla Wyników dotąd niezastosowanych. Wskazówki dla TASK-020: Rozwiązanie ma wołać `IRekomendacjeFasada.UkonczAsync/RozpocznijAsync` i `IProfilFasada.ZarejestrujWynikAsync` (+ `ZastosujOczekujaceWynikiAsync` w pracy `AKTUALIZUJ_PROFIL`); wywołanie `ZalozProfilAsync`/`ZalozSciezkeAsync` przy rejestracji Ucznia dojdzie w endpoincie rejestracji (TASK-071/072). Nie zrobiono (poza zakresem): handlery `AKTUALIZUJ_PROFIL`/`WYGAS_ELEMENTY`, Agent rekomendujący, adapter `IDaneDoboru`, endpointy. Weryfikacja: build `-warnaserror` 0/0; testy zielone; `dotnet format` czysto.

**Poprzednia zmiana: TASK-018** (pliki: `TASKS.md` pod TASK-018). Decyzje: fasady w `Lemat.Application.<Moduł>`; `PytanieScore`/`TypPytania.Score` jako wyjątek strażnika słownika; kod zaproszenia składany z dwóch prywatnych właściwości (`kod_zaproszenia`/`kod_wazny_do`); `KonfiguracjaModelu` z tokenem `xmin`, pozostałe agregaty bez `version`; aktywność progu/konfiguracji przez `ExecuteUpdate` przed INSERT (`ux_prog_aktywny`, `ux_konfiguracja_aktywna`); `DecyzjaModelu.RozwiazanieId` jako `Guid?` (TASK-020 wprowadzi `RozwiazanieId`); wybór zapisywany jako JSON string, rozkład jako obiekt jsonb. Weryfikacja: build `-warnaserror` 0/0; testy zielone; `dotnet format` czysto. Nie zrobiono (poza zakresem): `StanBezpiecznika`, `ProbkaKalibracyjna` (032), `IModelDecyzyjny`/`IModelGeneratywny` (038, 054), `RezultatPytan` -> `Domain.Grading` (022), zestawy/przypisania (034).

**Wytyczne dla TASK-019 (zrealizowane):** `Lemat.Domain.Profile` (`ProfilUcznia`, `OpanowanieUmiejetnosci`, `WynikZadania`, `TestDiagnostyczny`, czysty model Elo 10.1.3) i `Lemat.Domain.Recommendations` (`SciezkaNauki`, `ElementSciezki` z maszyną stanów 8.5, `Rekomendacja`); wzorzec jak Konta/Treści/Klasy: enumy przez `MapowanieTypowWyliczeniowych.Poczatkowe`, konfiguracje w `Persistence/Configurations/{Profile,Recommendations}/`, repozytoria w `Persistence/Repositories/...`, fasady `IProfilFasada`/`IRekomendacjeFasada` w `Lemat.Application.<Moduł>`; `Uczen`/`Zadanie`/`Umiejetnosc` przez identyfikatory z Accounts/Content. Testy bazy na `LEMAT_TEST_BAZA`.

**Poprzednia zmiana: TASK-017** (pliki: `TASKS.md` pod TASK-017). Decyzje: jsonb przez `ContentJson` + ValueConverter (klucze snake_case, enumy UPPER_SNAKE); `Dzial`/`Umiejetnosc`/`KalibracjaTrudnosci` bez `Version`; `Zadanie.AktualnaWersjaId` jako zapisana właściwość; `IZadanieRepozytorium.Dodaj` dodaje też wiersz początkowy kalibracji (b = 0.8*(poziom-3)); `ZasadyKryteriow` wymaga także ≥1 Kryterium dla Zadań otwartych i unikalności (wariant, nr); Zadanie zamknięte = 1 punkt bez wyniku wzorcowego; limit 24 punktów nieosiągalny przez bazę (test tylko domeny). Weryfikacja: build `-warnaserror` 0/0; testy zielone; `dotnet format` czysto.

**Wytyczne dla TASK-018 (zrealizowane):** `Lemat.Domain.Classes` (`Klasa`, `CzlonkostwoKlasy`, `IKlasyFasada`), `Lemat.Domain.Models` (`KonfiguracjaModelu`, `DecyzjaModelu` z partycją miesięczną, `PlanPytan`…), `Lemat.Domain.Calibration` (`KalibratorPewnosci`, `ProgPewnosci`, `IKalibracjaFasada`) — wzorzec jak Konta/Treści: enumy przez `MapowanieTypowWyliczeniowych.Poczatkowe`, konfiguracje w `Persistence/Configurations/<Moduł>/`, repozytoria w `Persistence/Repositories/<Moduł>/`, `Dodaj<Moduł>` w DI, testy bazy na `LEMAT_TEST_BAZA`. `DzialId` i `UmiejetnoscId` są już w `Lemat.Domain.Content`. Kalibrację trudności (`kalibracja_trudnosci`) aktualizuj wyłącznie atomowym SQL 10.1.3. TASK-019/040: adapter `IDaneDoboru` nad `ITresciFasada.PobierzKandydatowDoboruAsync` (filtry zależne od Ucznia — Wyniki 60 dni, Przypisania — stosuje wywołujący). TASK-025/026: zadania przez `Zadanie.Utworz(NoweZadanie, ...)` + `IZadanieRepozytorium.Dodaj` w `IJednostkaPracy` (triggery sum kryteriów i umiejętności głównej działają na COMMIT).

**Poprzednia zmiana: TASK-016** (pliki: `TASKS.md` pod TASK-016). Decyzje: fnv1a 32-bit/UUID big-endian mod 100; skrót hasła poza agregatem; `OstatnieLogowanieAt` z `[BezZmianyWersji]`; klasa testowa `UczenTests` w `StudentTests.cs` (strażnik synonimów); dodano `DzialId`. Weryfikacja: build `-warnaserror` 0/0; testy zielone; `dotnet format` czysto.

**Wytyczne dla TASK-017 (zrealizowane):** `Lemat.Domain.Content` — `DzialId` już istnieje w `Lemat.Domain.Content/CurriculumSectionId.cs` (nie duplikuj, dopisz resztę typów); nowe enumy rejestruj w `MapowanieTypowWyliczeniowych.Poczatkowe`; jsonb przez `JsonbSupport` (7.5.1–7.5.3); konfiguracje w `Persistence/Configurations/Content/`, repozytoria w `Persistence/Repositories/Content/`; wzorzec fasady/rejestracji jak `AccountsFacade`/`AccountsRegistration`; testy bazy na `LEMAT_TEST_BAZA`.

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
| OQ-10 | Poziom DOMYŚLNY opanowania: wzór `sigma(-0.8)` daje 0.310, a przykładowy kod w architekturze zakładał 0.5 | TASK-019: 0.310 wg wzoru; potwierdzić z PO |
| OQ-11 | Kody błędów domeny Profilu/Rekomendacji spoza 12.2 (`POZIOM_OPANOWANIA_POZA_ZAKRESEM`, `WYNIK_CUDZEGO_UCZNIA`, `TEST_NIEPRAWIDLOWY_STAN`, `ELEMENT_NIEPRAWIDLOWY_STAN`, `SERIA_PELNA`, `UZASADNIENIE_WYMAGANE` i in.) | Do zmapowania na ProblemDetails w TASK-071 (zakres POC wg planu) |
| OQ-12 | Pakiet `msw` (mocki frontendu) nie występuje w 5.2/6.4; `zod`, `react-hook-form`, `@hookform/resolvers`, `@playwright/test` są w stosie z CLAUDE.md pkt 2, ale jeszcze nie w `package.json` | Właściciel zażądał MSW w POC: dodać jako `devDependency` (TASK-132), bez wpływu na build produkcyjny; potwierdzić |
| OQ-13 | `kod_reguly` w `bledy_pol` nie ma w 12.2 zamkniętej listy | Lista POC-1.0 z `docs/POC_CONTRACT.md` 1.1 (U-31); uzgodnić przy TASK-071 |
| OQ-14 | `DzialWyjscie.poziom_matury` (jeden poziom na Dział) vs DDL `dzial.tylko_rozszerzony` | POC: `ROZSZERZONY` gdy `tylko_rozszerzony`, w pozostałych `PODSTAWOWY`; potwierdzić z PO |
| OQ-15 | Stałe hasła kont demo POC (`Lemat-Demo-2026!`) jawne w `docs/POC_CONTRACT.md`, wbrew sugestii TASK-026 (wypisywać w konsoli) | Dopuszczone wyłącznie dla `Development`/`E2E`; `seed-poc` odmawia w `Production`; potwierdzić |

## 9. Propozycje do słownika

Brak nowych pojęć domenowych. Nazwy tymczasowe poza słownikiem: ścieżka lokalnego magazynu (OQ-05), klucze opcji z OQ-06. Wszystkie nowe typy techniczne (`Mock*`, `Fake*`, `Prosta*`, `*Lokalny`)
mieszczą się w słowach technicznych 3.1 p. 7. Dopisuj tu kolejne propozycje w formacie: pojęcie, definicja, identyfikator, przyczyna.

---

## 10. Gotowy prompt dla kolejnego dewelopera

### 10.1 Następne TASK

Dwie osoby pracują równolegle według sekcji 12: ścieżka A (backend) startuje od **TASK-121**, ścieżka B (frontend) od **TASK-131**. Gotowe prompty startowe agentów: 12.3. Po obu ścieżkach — TASK-140. Prompt generyczny poniżej (10.2) stosuj z ograniczeniem do własnej ścieżki.

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
| `docs/POC_CONTRACT.md` | kontrakt HTTP POC (POC-1.0): endpointy, przykłady JSON, dane seed, zachowanie mocka — jedyny punkt styku ścieżek A i B |
| `docs/CRITIQUE_LOG.md`, `docs/ARCHITECTURE_CHANGELOG.md` | historia przeglądów (ostatnia iteracja: C9, wszystkie uwagi zaadresowane w v3.5) |
| `.claude/agents/*` | `architect`, `critic`, `planner`, `developer`, `handoff-guardian` |

---

## 12. POC — podział pracy na 2 osoby

**Decyzja właściciela (2026-11-01):** zakres zawężony do proof of concept — rejestracja i logowanie Ucznia (JWT), dane Ucznia i uproszczony Profil ucznia, lista Zadań z seeda ze szczegółami (KaTeX),
przesłanie zdjęcia kartki, po chwili zmockowana Transkrypcja (z zatwierdzeniem) i Ocena (punkty per Kryterium, uzasadnienie, Wskazówka). Poza zakresem: Test diagnostyczny, Rekomendacje, Weryfikacje, Klasy, Nauczyciel, Administrator, SSE.
Pracują **dwie osoby równolegle, każda ze swoimi agentami**: osoba A = backend (`[POC-A]`, TASK-121 … 130), osoba B = frontend (`[POC-B]`, TASK-131 … 139). Łączy je wyłącznie kontrakt `docs/POC_CONTRACT.md`
(POC-1.0); frontend pracuje na mockach MSW, backend nie czeka na frontend. Integrację robi TASK-140 (`[POC-INT]`). Zadania: `TASKS.md` → „POC (priorytet właściciela)”.

### 12.1 Zasady współpracy

1. **Branche.** Osoba A: `poc/backend`; osoba B: `poc/frontend` (oba z aktualnego `main`). Praca na własnym branchu, zadanie po zadaniu (jedno TASK = jeden commit, `feat(TASK-12x): … + updated codebase map & handoff`, stopka atrybucji wymagana przez środowisko).
   Zmiana kontraktu: osobny krótki branch `poc/contract-<n>` z jednym commitem `docs(poc-contract): …`.
2. **Kto merguje.** Każda osoba scala **własne** zmiany do `main` (pull request z `poc/backend` / `poc/frontend`) po zielonym `scripts/verify-backend.sh` (A) lub `scripts/verify-frontend.sh` (B), nie rzadziej niż po każdych 2–3 ukończonych zadaniach.
   Przed scaleniem: `git fetch && git rebase origin/main` (bez merge-commitów, jedno zadanie = jeden commit zostaje widoczny). Zmianę kontraktu scala dopiero druga osoba (akceptacja obu); po jej scaleniu obie ścieżki robią rebase. Agenci nie wykonują `git push` bez prośby swojej osoby.
3. **Rozdzielne katalogi.** A edytuje wyłącznie `backend/`, `scripts/`, `.env.example`, `README.md` (sekcja „Szybki start POC”, TASK-130), `.logs/` w `.gitignore`. B edytuje wyłącznie `frontend/` (w tym `frontend/.env.example`) oraz własne linie w `.gitignore` (`playwright-report/`, `test-results/`).
   Nikt nie edytuje `docs/` (poza `docs/CODEBASE_MAP.md` jak niżej i kontraktem wg procedury), `CLAUDE.md` ani cudzego katalogu. Brak lub sprzeczność w kontrakcie → wpis w 12.6, nie „cicha” decyzja w kodzie; odstępstwo zgodne z kontraktem nie wymaga pytania.
4. **Kontrakt jest jedynym łączem.** A implementuje dokładnie ten kontrakt (testy porównują z przykładami), B buduje mocki z danych kontraktu (sekcje 6 i 7), nigdy z kodu backendu. Zmiany addytywne podnoszą wersję `POC-1.x`; niezgodne — `POC-2.0` po zgodzie właściciela.
5. **Punkty synchronizacji.** (1) Po TASK-122 A publikuje `seed-poc`; po TASK-132 B ma pełne mocki — żadna ścieżka na nikogo nie czeka. (2) Po TASK-130 A ogłasza „backend gotowy” (`scripts/dev-up.sh`, `scripts/poc-smoke.sh`); po TASK-139 B uruchamia `LEMAT_E2E_REAL=1`.
   (3) TASK-140 wykonuje osoba B (domyślnie), z pomocą A przy poprawkach backendu; na czas integracji ścieżki nie dokładają nowych zadań.

### 12.2 Jak nie robić konfliktów w `TASKS.md`, `HANDOFF.md`, `docs/CODEBASE_MAP.md`

W POC `handoff-guardian` każdej ścieżki **zawęża** swoje obowiązki z CLAUDE.md pkt 5.2:

| Dokument | Co wolno ścieżce | Czego nie wolno |
|---|---|---|
| `TASKS.md` | Zmienić `[ ]` → `[x]` i dopisać `Pliki:`/`Weryfikacja:` **tylko pod własnymi zadaniami** (A: 121 … 130, B: 131 … 139); nic więcej w pliku | Zmieniać cudze zadania, tabelę mapowania, sekcje MVP/POST-MVP, formatowanie |
| `HANDOFF.md` | Pisać **tylko** w 12.4 (podsekcja własnej ścieżki: stan, jak uruchomić, zmienne, decyzje, nowe U-xx/OQ jako propozycje) i w 12.6/12.7 z prefiksem `[A]`/`[B]`; własne wiersze dopisywać na końcu tabel | Edytować sekcje 1 … 11 (tabela „Stan prac”, U-xx, OQ, „Wymagane akcje”) — scala je TASK-140 |
| `docs/CODEBASE_MAP.md` | Dodawać/zmieniać wiersze swoich plików (backend lub frontend) i status `planowane` → `gotowe`; nowe wiersze na końcu właściwej sekcji | Usuwać lub przepisywać cudze wiersze, zmieniać diagramy cudzej warstwy; przy konflikcie rebase zachować **obie** strony |

Konflikt tekstowy w tych trzech plikach rozwiązuje się zawsze „zachowaj obie strony” (dwie osoby nie mają prawa zmieniać tych samych linii). Opcjonalnie (do decyzji właściciela, nie dodane): `.gitattributes` z `docs/CODEBASE_MAP.md merge=union`.
Gdy zadanie wymaga nowego U-xx lub OQ, ścieżka proponuje je w 12.4/12.6; numery U-32 … i OQ-16 … nadaje TASK-140.

### 12.3 Gotowe prompty startowe dla agentów

**Osoba A — backend (`[POC-A]`).** Wklej do agenta (`developer`, potem `handoff-guardian`; gdy pracujesz bez podziału na agentów, wykonaj obie role po kolei):

```text
Jesteś agentem osoby A (backend) w repozytorium Lemat (/home/user/lemat), ścieżka [POC-A]. Właściciel zawęził zakres do POC: rejestracja/logowanie Ucznia (JWT), /uczniowie/ja i uproszczony
Profil, lista i szczegóły Zadań z seeda, przesłanie zdjęcia kartki, zmockowana Transkrypcja i Ocena. Drugą ścieżkę (frontend, [POC-B]) prowadzi inna osoba równolegle — łączy Was tylko kontrakt.
1. Pracuj na branchu poc/backend (git switch -c poc/backend z aktualnego main, jeśli go nie ma). Zmieniaj WYŁĄCZNIE backend/, scripts/, .env.example, README.md (sekcja "Szybki start POC", TASK-130) i .gitignore (linia .logs/). Nie dotykaj frontend/, docs/ ani CLAUDE.md.
2. Przeczytaj: CLAUDE.md w całości; docs/POC_CONTRACT.md w całości (to Twój kontrakt — implementujesz go dokładnie: ścieżki, nazwy pól snake_case, kody błędów, dane seed, zachowanie mocka); HANDOFF.md sekcje 4, 8 i 12; TASKS.md sekcję "POC (priorytet właściciela)";
   docs/CODEBASE_MAP.md. Z docs/ARCHITECTURE_DETAILED.md i docs/spec/* czytaj tylko sekcje z pola "Realizuje" zadania (sed -n 'A,Bp').
3. Wybierz najniższe TASK z ścieżki [POC-A] (TASK-121 … 130) ze statusem [ ], którego zależności są [x]. Pierwsze: TASK-121. Realizuj dokładnie jego zakres i DoD; nie wykonuj zadań spoza POC i spoza swojej ścieżki.
4. Obowiązują: słownik pojęć (identyfikatory polskie ASCII, nazwy plików angielskie), nagłówek "// @Role … | @Task: TASK-XXX" w każdym .cs, mocki za portami (aplikacja działa bez kluczy), DDL z docs/spec/ddl.md jedynym źródłem schematu (żadnych migracji z modelu EF),
   jedna transakcja = zmiana stanu + zlecenie pracy, wywołania modeli poza transakcją, testy na PostgreSQL 16 (Testcontainers lub LEMAT_TEST_BAZA).
5. Zweryfikuj: dotnet build backend/Lemat.slnx -warnaserror && dotnet test backend/Lemat.slnx && dotnet format backend/Lemat.slnx --verify-no-changes (lub scripts/verify-backend.sh). Odpowiedzi API muszą zgadzać się z przykładami JSON w kontrakcie.
6. Sprzeczność lub luka w kontrakcie: NIE zgaduj — zapisz w HANDOFF.md 12.6 (prefiks [A]) i zatrzymaj zadanie, jeśli blokuje DoD. Zmiana kontraktu wyłącznie procedurą z docs/POC_CONTRACT.md sekcja 10 (osobny branch, zgoda osoby B).
7. Dokumentacja po zadaniu (zasady z HANDOFF.md 12.2): TASKS.md ([x], Pliki, Weryfikacja — tylko Twoje zadanie), HANDOFF.md 12.4.A, docs/CODEBASE_MAP.md (Twoje wiersze). Jeden commit: "feat(TASK-12x): <opis> + updated codebase map & handoff" ze stopką atrybucji. Bez git push, chyba że osoba poprosi.
8. Raport: utworzone/zmienione pliki, wyniki komend, decyzje i odstępstwa, mocki, wskazówki dla następnego zadania. Potem weź kolejne zadanie ścieżki A.
```

**Osoba B — frontend (`[POC-B]`).** Wklej do agenta (`developer`, potem `handoff-guardian`):

```text
Jesteś agentem osoby B (frontend) w repozytorium Lemat (/home/user/lemat), ścieżka [POC-B]. Właściciel zawęził zakres do POC: rejestracja/logowanie Ucznia, ustawienia i uproszczony Profil, lista i szczegóły Zadań (KaTeX),
przesłanie zdjęcia kartki, ekran oczekiwania z odpytywaniem, Transkrypcja z zatwierdzeniem i Ocena z Wskazówką. Backend ([POC-A]) pisze inna osoba równolegle — łączy Was tylko kontrakt; Ty NIE potrzebujesz działającego backendu.
1. Pracuj na branchu poc/frontend (git switch -c poc/frontend z aktualnego main, jeśli go nie ma). Zmieniaj WYŁĄCZNIE frontend/ oraz własne linie w .gitignore (playwright-report/, test-results/). Nie dotykaj backend/, scripts/, docs/ ani CLAUDE.md.
2. Przeczytaj: CLAUDE.md w całości; docs/POC_CONTRACT.md w całości (sekcje 6 i 7 zawierają komplet danych do mocków: Działy, 5 Zadań, Transkrypcje, Oceny, czasy 3 s + 2 s, przykłady JSON, kody błędów); HANDOFF.md sekcje 4, 8 i 12;
   TASKS.md sekcję "POC (priorytet właściciela)"; docs/CODEBASE_MAP.md. Z docs/ARCHITECTURE_DETAILED.md i docs/spec/api.md czytaj tylko to, co wskazuje pole "Realizuje" (np. api.md 6, sekcje 6.8/6.9 architektury dla drzewa frontendu).
3. Wybierz najniższe TASK z ścieżki [POC-B] (TASK-131 … 139) ze statusem [ ], którego zależności są [x]. Pierwsze: TASK-131. Realizuj dokładnie jego zakres i DoD; nie wykonuj zadań spoza POC i spoza swojej ścieżki.
4. Obowiązują: wszystkie teksty UI w frontend/messages/pl-PL.json (nigdy w JSX), tylko lib/api/client.ts woła /api/*, token dostępu tylko w pamięci, KaTeX z throwOnError:false, trasy i katalogi features/* angielskie kebab-case ze słownika
   (np. (student)/solutions/[solutionId], zmienna const { solutionId: rozwiazanieId }), nagłówek "// @Role … | @Task: TASK-XXX" w każdym .ts/.tsx, układ działa od 375 px, nowe zależności tylko z listy w TASK-131/132 (msw — OQ-12).
5. Zweryfikuj: npm --prefix frontend run typecheck && lint && test && build oraz npm --prefix frontend run check:headers (lub scripts/verify-frontend.sh). Mocki MSW muszą zwracać dokładnie dane i kształty z kontraktu.
6. Sprzeczność lub luka w kontrakcie: NIE zgaduj — zapisz w HANDOFF.md 12.6 (prefiks [B]) i zatrzymaj zadanie, jeśli blokuje DoD. Zmiana kontraktu wyłącznie procedurą z docs/POC_CONTRACT.md sekcja 10 (osobny branch, zgoda osoby A).
7. Dokumentacja po zadaniu (zasady z HANDOFF.md 12.2): TASKS.md ([x], Pliki, Weryfikacja — tylko Twoje zadanie), HANDOFF.md 12.4.B, docs/CODEBASE_MAP.md (Twoje wiersze). Jeden commit: "feat(TASK-13x): <opis> + updated codebase map & handoff" ze stopką atrybucji. Bez git push, chyba że osoba poprosi.
8. Raport: utworzone/zmienione pliki, wyniki komend, decyzje i odstępstwa, użyte mocki, wskazówki dla następnego zadania. Potem weź kolejne zadanie ścieżki B.
```

**TASK-140 (integracja, osoba B z pomocą A):** „Wykonaj TASK-140 z TASKS.md: uruchom scripts/dev-up.sh i frontend w trybie dev:real, przejdź scenariusz z HANDOFF.md 12.5, uruchom LEMAT_E2E_REAL=1 npm --prefix frontend run e2e i scripts/poc-smoke.sh;
rozbieżności rozstrzygaj zmianą kontraktu (procedura w POC_CONTRACT.md 10) i poprawką właściwej ścieżki; scal HANDOFF.md (sekcje 4, 6, 7, 8, 9: U-32 …, OQ-16 …) i docs/CODEBASE_MAP.md; zaktualizuj 'Stan prac' i 'Następny krok'.”

### 12.4 Dziennik ścieżek (wypełnia `handoff-guardian` własnej ścieżki)

#### 12.4.A Ścieżka A — backend (`poc/backend`)

| Pole | Wartość |
|---|---|
| Stan | TASK-121 … 130: 10 / 10 ukończonych — **backend POC kompletny** (TASK-130 gotowe: `PocFlowTests` (`Lemat.Api.Tests/Poc`, prawdziwa baza + `PocPracaHostedService`, ~7 s; rejestracja → PATCH → lista Zadań → JPEG → TRANSKRYPCJA_GOTOWA → zatwierdzenie → OCENIONE → Ocena → profil uproszczony; izolacja drugiego Ucznia 404), `scripts/dev-up.sh` (start/`--stop`/`--reset`), `scripts/poc-smoke.sh`, klucze `Lemat:Poc:*` w `.env.example`, „Szybki start POC” w `README.md`, `.logs/` w `.gitignore`; `.env` i `.logs/` nieśledzone; `verify-backend.sh` zielone; `--reset` niezweryfikowane). Wcześniej TASK-129 gotowe: `GET /uczniowie/ja/profil-uproszczony` (`EndpointyProfilu` → `PobierzProfilUproszczonyHandler` → port `IProfilUproszczonyCzytnik` / `ProfilUproszczonyCzytnikEf`; agregat po aktualnych Ocenach OSTATECZNA Rozwiązań OCENIONE, `liczba_rozwiazan` bez ANULOWANE/PORZUCONE, `sredni_procent_punktow` 1 miejsce, Działy z `ITresciFasada`, 5 ostatnich Ocen: `oceniono_at` = `ocena.created_at` malejąco, potem `rozwiazanie_id`; testy: nowy Uczeń, przykład 4.3, izolacja, 401; bez Elo, U-28; bez nowych zmiennych i odstępstw). Wcześniej TASK-128 gotowe: zmockowany potok end-to-end. Pętla `PocPracaHostedService` (Lemat.Worker) odpytuje `praca_w_tle` co 500 ms przez `ProcesorPrac` + `KolejkaPracSql` (jedno `UPDATE … FOR UPDATE SKIP LOCKED`, dzierżawa 60 s, ponowienia do 3 prób, potem `MARTWA`); handlery idempotentne `RozpoznajPismoHandler` (PRZESLANE → ROZPOZNAWANIE → TRANSKRYPCJA_GOTOWA albo BLAD_ROZPOZNAWANIA) i `OcenRozwiazanieHandler` (ZATWIERDZONE → OCENIANIE → OCENIONE; Ocena OSTATECZNA, `decyzja_modelu` na Kryterium, `wynik_zadania`, `UkonczAsync` Elementu Ścieżki); porty `IAgentRozpoznajacy`/`IAgentOceniajacy` (Lemat.Application.Models) z Mockami `MockAgentRozpoznajacy`/`MockAgentOceniajacy` (Lemat.Models.Mock, dane osadzone `poc-mock.json` z tabel 7.2/7.3); `POST /api/v1/rozwiazania/{id}/zatwierdzenie` (`If-Match`, zlecenie `OCEN_ROZWIAZANIE` w tej samej transakcji) i `GET /api/v1/rozwiazania/{id}/ocena` (404 do `OCENIONE`); testy: Api 135, Infrastructure 242, Application 70, Models 12, Architecture 106, Domain 500; TASK-127 gotowe: porty `IMagazynObiektow`/`IObrobkaObrazow`/`IOdczytPodpisanegoAdresu`/`IZlecaniePrac` (Application), adaptery `MagazynObiektowLokalny` (adresy HMAC 5 min, `GET /api/v1/magazyn-lokalny/{token}` bez Authorization), `ProstaObrobkaObrazow` (JPEG), `ZlecaniePracSql`; `PrzeslijRozwiazanieHandler`; `POST /api/v1/rozwiazania` (multipart, 202 + Location, `ROZPOZNAJ_PISMO` w tej samej transakcji), `GET /rozwiazania` (status, kursor), `GET /rozwiazania/{id}` (pełne `RozwiazanieWyjscie` z `nastepne_odpytanie_ms`), `GET /rozwiazania/{id}/zdjecia/{id}/adres`; daty JSON w UTC z `Z`; Api 124, Infrastructure 235, Application 70 testów; TASK-126 gotowe: domena `Lemat.Domain.Solutions` (`Rozwiazanie` z przejściami POC wg 7.11, `ZdjecieRozwiazania`, `Transkrypcja`/`KrokTranskrypcji`) i `Lemat.Domain.Grading` (`Ocena.Automatyczna`, `OcenaKryterium`, VO `Wskazowka`/`PewnoscOceny`/`PewnoscSurowa`), konfiguracje EF, repozytoria `IRozwiazanieRepozytorium`/`ITranskrypcjaRepozytorium`/`IOcenaRepozytorium`, fasady odczytowe `IRozwiazaniaFasada`/`IOcenianieFasada`, `DodajModulRozwiazan`/`DodajModulOceniania` w Api i Worker; +6 enumów PG w `EnumTypeMapping`; 129 nowych testów (Domain 500, Infrastructure 212); TASK-125 gotowe: `GET /api/v1/zadania` (filtr `dzial_id`, `limit` 422 przy złym, kursor HMAC `{poziom_trudnosci,id}`, sort po poziomie nominalnym i id) i `GET /api/v1/zadania/{id}` (ZadanieUczniaWyjscie bez pól wzorcowych; 404 NIE_ZNALEZIONO dla nieopublikowanych/nieznanych); test zgodny strukturalnie z kontraktem 6.3 przez `PocSeed`; TASK-124 gotowe: `GET`/`PATCH /api/v1/uczniowie/ja` (`UczenWyjscie`, `test_diagnostyczny = BRAK`, `klasy = []`; JSON Merge Patch przez `Opcjonalne<T>` + `OpcjonalneKonwerter`, `application/merge-patch+json`, `If-Match` z `version`, 422 `NIE_MOZNA_WYCZYSCIC`/`DZIAL_NIEZNANY`, 412 z `szczegoly.aktualny`, 415/428) i `GET /api/v1/dzialy` (13 pozycji, polityka Uczeń); 18 + 2 grupy nowych testów Api; TASK-123 gotowe: `POST /api/v1/auth/{rejestracja,logowanie,odswiezenie,wylogowanie}` — JWT ES256 15 min (claims sub/rola/status/jti/exp), refresh token w cookie `lemat_rt` (HttpOnly, SameSite=Strict, Path=/api/v1/auth, 14 dni) z rotacją i unieważnieniem rodziny (U-22), polityki `Anonimowy`/`Zalogowany`/`Uczen`, FluentValidation, 26 nowych testów Api; TASK-122 gotowe: `Lemat.Tools seed-poc` — 13 Działów, 5 Umiejętności, 5 Zadań z 11 Kryteriami, 2 konta demo argon2id (`IHasherHasel`/`HaszowanieHasel`), konfiguracje `mock` (ROZPOZNAWANIE_PISMA, DECYZYJNY_GLOWNY), kalibrator tożsamościowy, próg 0.95; idempotentne, odmowa w Production; TASK-121 gotowe: infrastruktura HTTP `/api/v1` — JSON snake_case, ProblemDetails z `kod`/`trace_id`/`bledy_pol`, `Strona<T>` + kursor HMAC, `If-Match` 428/400/412, `FiltrScenariuszaMock`, nagłówki; trwałość wpięta w Api i Worker) |
| Jak uruchomić | Baza: `docker compose up -d` (lub `scripts/db-local-setup.sh`); `dotnet run --project backend/src/Lemat.Tools -- migrate`; `dotnet run --project backend/src/Lemat.Api` (`http://localhost:5080`, `/api/v1/zdrowie`); Worker `dotnet run --project backend/src/Lemat.Worker` (`:8081`). Testy Api: `LEMAT_TEST_BAZA='Host=localhost;Port=5432;Database=postgres;Username=lemat;Password=lemat_dev' dotnet test backend/Lemat.slnx`. Seed: `dotnet run --project backend/src/Lemat.Tools -- seed-poc` (po `migrate`; opcje `--polaczenie=`, LEMAT_BAZA_MIGRACJE/LEMAT_BAZA; wypisuje konta demo). **Jednym poleceniem:** `scripts/dev-up.sh` (PostgreSQL z `.env`/Docker :5433 albo lokalny 5432 → `migrate` → `seed-poc` → Api `http://localhost:5080` + Worker w tle; logi `.logs/api.log`, `.logs/worker.log`, PID-y w `.logs/`; wypisuje adresy i konta demo; idempotentny); `scripts/poc-smoke.sh` (curl, pełny przepływ z kontraktu na działającym API, kod 0 + punkty Oceny; używa go TASK-140); `scripts/dev-up.sh --stop` (zatrzymuje Api i Worker, baza zostaje); `scripts/dev-up.sh --reset` (pusta baza + magazyn; **niezweryfikowane** — `docker compose down -v` było zablokowane, sprawdź ręcznie). |
| Zmienne środowiskowe / konfiguracja | `LEMAT_BAZA` (connection string; bez niej hosty używają połączenia zastępczego), `LEMAT_KURSOR_KLUCZ` (HMAC kursora; w Development klucz domyślny, w Production wymagany), `LEMAT_TEST_BAZA` (testy); `LEMAT_JWT_KLUCZ` (PEM klucza ES256; w Development brak = generowany i zapisywany w `~/.local/share/lemat/jwt-dev-es256.pem`, poza repo; w Production wymagany), `Jwt:OdswiezenieDni` = 14 (appsettings Api/Worker/Scheduler); `LEMAT_MAGAZYN_KLUCZ` (`Lemat:Magazyn:Klucz`, klucz HMAC adresów zdjęć; Development/E2E ma domyślny, Production wymaga; nie commituj), `LEMAT_MAGAZYN_SCIEZKA` (`Lemat:Magazyn:Sciezka`; domyślnie Development `backend/storage/magazyn`, w .gitignore); `Lemat:Poc:{OpoznienieRozpoznaniaMs=3000, OpoznienieOcenyMs=2000, OdpytanieMs=500, DzierzawaSekund=60, OdstepPonowieniaMs=2000}` (appsettings/zmienne `Lemat__Poc__*`; walidowane w `LematOpcjeWalidator`); Worker potrzebuje tej samej `LEMAT_BAZA` co API (+ `LEMAT_MAGAZYN_KLUCZ`/ścieżki jak API, bo wspólny walidator opcji); `Lemat:Modele:Dostawca` ≠ `mock` przerywa start workera (InvalidOperationException) |
| Decyzje, odstępstwa, propozycje U-xx/OQ | TASK-126: [A] `Zatwierdz` w domenie nie sprawdza `If-Match` (robi to handler/endpoint przez `Version`, 412); domena rzuca ROZWIAZANIE_NIEPRAWIDLOWY_STAN albo TRANSKRYPCJA_NIEAKTUALNA. [A] Przejścia poza zakresem POC (7.11) nie istnieją, choć enum ma wszystkie 10 wartości DDL. [A] Brak zdarzeń domenowych; prace w tle zleca handler w tej samej transakcji (U-26). [A] Kolumny jsonb Transkrypcji/Kroku i `obszary_maskowania` poza modelem EF (DEFAULT/NULL w DDL) — dodaje TASK-020. [A] `Ocena` tylko fabryką `Ocena.Automatyczna` (waliduje CHECK-i DDL); `Ocena.Nauczyciela`/RezultatOceny — TASK-021. [A] Zastąpienie Oceny: najpierw zmiana starego wiersza, osobna jednostka pracy (ux_ocena_aktualna). [A] `IRozwiazanieRepozytorium.ZnajdzNieukonczoneAsync` blokuje PRZESLANE…OCENIANIE i OCZEKUJE_NA_WERYFIKACJE; BLAD_ROZPOZNAWANIA/OCENIONE nie blokują. [A] `Rozwiazanie.element_sciezki_id` bez nawigacji EF. [A] `KodBledu` to podzbiór POC — kolejne zadania dopisują członka + wpis w `ErrorCatalog` (test pilnuje kompletności). [A] Grupy endpointów przez `IGrupaEndpointow` rejestrowane w DI. [A] `KonfliktWspolbieznosciException` -> 412 `KONFLIKT_WERSJI`, błędy łączności z bazą -> 503 `BAZA_NIEDOSTEPNA`. [A] Nie zaimplementowano `Opcjonalne<T>` (merge-patch) ani limitów ciała Kestrel (1 MB / 52 MB) — TASK-123/124/127. Brak nowych pakietów i zmian DDL. TASK-123: [A] `Jwt:OdswiezenieDni` 30 -> 14 (kontrakt POC 3.1; odstępstwo od tabeli 22). [A] Wylogowanie bez/z cudzym cookie unieważnia wszystkie tokeny konta (U-22); access token ważny do wygaśnięcia (bezstanowy JWT). [A] 403 `KONTO_NIEAKTYWNE` tylko po poprawnym haśle (zły e-mail i złe hasło -> identyczne 401); atrapa weryfikacji hasła wyrównuje czas; brak limitu prób (U-22). [A] `NAUCZYCIEL` -> 422 `ROLA_NIEOBSLUGIWANA` (U-21). [A] W modelu EF dodano relację `TokenOdswiezania` -> `Uzytkownik` (kolejność INSERT; DDL bez zmian). [A] `ProblemWriter.ZapiszAsync(…, przedZapisem, token)` — `token` jest ostatnim parametrem (CA1068). [A] `IConfigureOptions<JwtBearerOptions>` rejestrować jako `IConfigureOptions`. TASK-124: [A] `Opcjonalne<T>` w `Lemat.Api.Contracts.Common` (ADR-072), warstwa Application ma własne `ZmianaPola<T>` (bez zależności od Api); `OptionalOpenApiTransformer` mapuje `Opcjonalne<T>` na `T|null` w OpenAPI (bez niego schematy były puste — wymóg ADR-072 p. 2 dla TASK-081). [A] Fasada Treści w kodzie to `ITresciFasada` (`IContentFacade`); `GET /dzialy` i walidacja `DZIAL_NIEZNANY` idą przez nią. [A] PATCH zwraca rezultat (`Zmieniono`/`KonfliktWersji`/`DzialNieznany`), nie wyjątek; wyścig dwóch PATCH: `UPDATE … WHERE version` -> `KonfliktWspolbieznosciException` -> ponowny odczyt -> 412. [A] Kolejność błędów PATCH: wiązanie/JSON (422/415) -> If-Match (428/400) -> `NIE_MOZNA_WYCZYSCIC` -> wersja (412) -> `DZIAL_NIEZNANY`. [A] Termin egzaminu w przeszłości nie jest odrzucany (kontrakt: „poprawna data”). [A] PATCH bez realnej zmiany nie podnosi `version`. [A] Testy Api wstawiają 13 Działów SQL-em (bez zależności od `Lemat.Tools`); klasa testowa `UczenEndpointsTests` (strażnik słownika blokuje `student`). TASK-127: [A] Porty magazynu/obróbki w `Lemat.Application.Models`, `IZlecaniePrac` w `Common.BackgroundJobs`; `IObrobkaObrazow` tylko `NormalizujAsync` (Maskuj/Wytnij — TASK-056), `TypPracy` tylko `ROZPOZNAJ_PISMO` (`OCEN_ROZWIAZANIE` dopisze TASK-128; 128 mówi `maks_prob` 3, 13.3 — 4), brak `TrybDeduplikacji`. [A] Nowy port `IOdczytPodpisanegoAdresu` (reguła architektury zabrania endpointom zależności od Infrastructure) oraz nowa opcja `Lemat:Magazyn:Klucz`/`LEMAT_MAGAZYN_KLUCZ` (walidator wymaga w Production). [A] Kolejność kontroli w `PrzeslijRozwiazanieHandler`: liczba zdjęć (0/>5 -> 422 `ZA_DUZO_ZDJEC`) -> Zadanie/wersja (404/409) -> rodzaj (zamknięte -> 422 `WALIDACJA`) -> nieukończone Rozwiązanie (409) -> obróbka zdjęć (413/415/422); plik w innym polu niż `zdjecia` lub zły `zadanie_wersja_id` -> 422 `WALIDACJA` z `bledy_pol` (przed handlerem). [A] Plik bez sygnatury JPEG, niebędący znanym innym obrazem (PNG/GIF/BMP/TIFF/WEBP/HEIC) -> 422 `ZDJECIE_NIEPRAWIDLOWE`; 415 tylko dla rozpoznanych innych obrazów (zob. 12.6 K-A-01). [A] Limit ciała 52 MB tylko dla `POST /rozwiazania` (middleware `UseWhen`, `IHttpMaxRequestBodySizeFeature`); TestServer go nie egzekwuje, więc 413 `ZAPYTANIE_ZA_DUZE` bez testu integracyjnego; limit 1 MB z kontraktu 1.2 nadal nieegzekwowany dla pozostałych endpointów. [A] `IRekomendacjeFasada.RozpocznijProbeAsync` (U-24, techniczny element Ścieżki bez Rekomendacji); element zostaje `ROZPOCZETY` po `OCENIONE` — TASK-128 woła `UkonczAsync` albo akceptuje wzrost liczby aktywnych elementów. [A] Obiekty magazynu zapisywane poza transakcją, po wycofaniu/konflikcie usuwane best-effort (sprzątanie okresowe — POST-MVP). [A] `RozwiazanieWyjscie` składane w pełni już teraz (Transkrypcja, Ocena z opisami Kryteriów); Weryfikacja/szkic/Poprawki zawsze `null`. [A] Daty JSON zawsze UTC z `Z` (`UtcDateTimeOffsetConverter`). [A] Lista Rozwiązań: kursor (created_at malejąco, id rosnąco). [A] Testy `ExercisesEndpointsTests` i `SolutionsEndpointsTests` w jednej kolekcji xUnit „Dane POC” (test wersji Zadania 5 chwilowo zmienia `aktualna_wersja_id`). Brak nowych pakietów i zmian DDL. TASK-128: [A] Porty agentów w `Lemat.Application.Models` (Application nie może zależeć od Agents), Mocki w `Lemat.Models/Mock`. [A] Rozstrzygnięto punkt z TASK-127: `OcenRozwiazanieHandler` w jednej jednostce pracy z OCENIONE woła `IRekomendacjeFasada.UkonczAsync` (Element Ścieżki → UKONCZONY); po BLAD_ROZPOZNAWANIA Element zostaje ROZPOCZETY (ew. Pomin w kolejnym zadaniu). [A] Dane mocka kluczowane Id Zadania; test integracyjny parsuje oczekiwane wartości wprost z `docs/POC_CONTRACT.md` (niezależnie od `poc-mock.json`). [A] Każde przejście stanu = osobny SaveChanges (`version` 1→…→6); Transkrypcja + TRANSKRYPCJA_GOTOWA w jednej tx; Ocena + Decyzje w tx A, a OCENIONE + Wynik zadania + UkonczAsync w tx B (FK `wynik_zadania.ocena_id` nieodroczony). [A] Kolejność kontroli zatwierdzenia: 404 → stan (409 `ROZWIAZANIE_NIEPRAWIDLOWY_STAN`, także przy starym If-Match) → `version` (412 z `szczegoly.aktualny`) → Transkrypcja (409 `TRANSKRYPCJA_NIEAKTUALNA`); 428/400 i walidacja 422 przed handlerem; zatwierdzenie nie używa `FiltrScenariuszaMock` (scenariusz „domyslny”). [A] `maks_prob` = 3 dla obu typów (kontrakt POC; 13.3 mówi 4); Wskazówka ma źródło SZABLON. [A] Praca `MARTWA` nie zmienia statusu Rozwiązania (zostaje ROZPOZNAWANIE/OCENIANIE; klient po 60 s pokazuje komunikat) — do rozważenia poza POC (Z-3). [A] Partycje `decyzja_modelu`: migracja tworzy bieżący miesiąc + 3; w długo żyjącej bazie dev bez `UTRZYMAJ_PARTYCJE` zapis decyzji padnie po ok. 3 miesiącach (praca MARTWA) — wtedy ponownie `migrate` na świeżej bazie. [A] Odzyskiwanie dzierżaw co 20 iteracji pętli. Brak nowych pakietów i zmian DDL. |
| Następne zadanie | Ścieżka A zakończona (brak kolejnych zadań POC-A). Dalej: ścieżka B (TASK-131 … 139) po stronie osoby B oraz **TASK-140** (integracja obu ścieżek, `[POC]`; zależy od TASK-130 `[x]` i TASK-139). Dla TASK-140 backend gotowy: `scripts/dev-up.sh` + `scripts/poc-smoke.sh`; frontend `dev:real` z proxy `/api` → `http://localhost:5080` (`LEMAT_API_URL`); konta demo wypisuje `dev-up.sh`/`seed-poc`; opóźnienia mocka: rozpoznanie ~3 s, ocena ~2 s. Odnotowane luki: limit ciała 1 MB nieegzekwowany poza `POST /rozwiazania`, praca `MARTWA` nie zmienia statusu Rozwiązania, bez SSE (U-27). |

**Ścieżka POC-A zakończona.** Wznowienie: tylko na prośbę właściciela (poprawki kontraktu wg `docs/POC_CONTRACT.md` sekcja 10) albo w ramach TASK-140 (integracja; uruchom `scripts/dev-up.sh`, potem `scripts/poc-smoke.sh`).

#### 12.4.B Ścieżka B — frontend (`poc/frontend`)

| Pole | Wartość |
|---|---|
| Stan | TASK-131 … 139: 1 / 9 ukończonych (TASK-131 gotowe: `frontend/src/lib/api/{types,errors,client}.ts` — typy kontraktu POC 3–7 jako unie literałów, `BladApi` + `kluczKomunikatuBledu(kod)` → `bledy.<KOD>`, klient z `skonfigurujKlienta({pobierzToken, odswiezSesje, fetchImpl?, bazowyAdres?})`, 16 funkcji endpointów; jedno odświeżenie i jedno ponowienie po 401; `check:headers` zielone; Vitest 60 testów) |
| Jak uruchomić | (po TASK-138) `npm --prefix frontend install`; `npm --prefix frontend run dev:mock` (MSW, bez backendu) lub `dev:real` (proxy `/api` → `LEMAT_API_URL`, domyślnie `http://localhost:5080`); `npm --prefix frontend run e2e` |
| Zmienne środowiskowe / konfiguracja | TASK-131: brak nowych zmiennych (nowe zależności npm: zod, react-hook-form, @hookform/resolvers). (uzupełnia ścieżka B: `NEXT_PUBLIC_LEMAT_API=mock\|real`, `LEMAT_API_URL`, `NEXT_PUBLIC_LEMAT_MOCK_SCENARIUSZ`, `LEMAT_E2E_REAL`) |
| Decyzje, odstępstwa, propozycje U-xx/OQ | TASK-131: [B] konfiguracja klienta globalna w module (`skonfigurujKlienta`); token i `odswiezSesje` wstrzykuje TASK-133, single-flight odświeżania po stronie wstrzykiwanej funkcji; gdy `odswiezSesje` rzuci `BladApi` (np. `SESJA_UNIEWAZNIONA`) — propagacja, przy `null`/innym błędzie oryginalny 401. [B] Zdjęcia jako `kartka-N.jpg` w polu `zdjecia`; `If-Match: "<version>"`; PATCH `application/merge-patch+json`. [B] Brak `frontend/.env.example` (TASK-132/138). Wskazówki dla TASK-132+: MSW przechwytuje `/api/v1/...`; w testach jsdom ustaw `bazowyAdres` na pełny URL (np. `http://localhost/api/v1`); komunikaty przez `kluczKomunikatuBledu` z fallbackiem `bledy.NIEZNANY`/`problem.detail`; WALIDACJA → `bledyPol[].komunikat` przy polach; `version` z ostatnio odczytanego zasobu, po 412 z `blad.aktualny`. |

### 12.5 Definicja „POC gotowe” i scenariusz demo

**POC jest gotowe, gdy** spełnione są wszystkie warunki: (a) TASK-121 … 140 mają status `[x]`; (b) scenariusz demo poniżej przechodzi na czystym klonie bez ręcznych obejść, a uruchomienie to `scripts/dev-up.sh` + `npm --prefix frontend run dev:real` (+ `npm install`);
(c) zielone: `scripts/verify-all.sh`, `PocFlowTests` (backend), `npm --prefix frontend run e2e` w trybie MSW oraz z `LEMAT_E2E_REAL=1`; (d) odpowiedzi API zgadzają się z `docs/POC_CONTRACT.md` (`scripts/poc-smoke.sh` kod 0), brak otwartych pozycji w 12.6;
(e) HANDOFF.md zawiera aktualny „Stan prac”, „Jak uruchomić” i wpisy U-21 … U-31 oraz dalsze; (f) nic spoza zakresu POC nie zostało dodane „przy okazji”.

**Scenariusz demo (kolejne kroki):**

1. Uruchom `scripts/dev-up.sh` (PostgreSQL, migracja, `seed-poc`, API :5080, Worker) i `npm --prefix frontend run dev:real` → `http://localhost:3000`. Konta demo wypisane w konsoli (`uczen1@lemat.local`, `uczen2@lemat.local`, hasło z `docs/POC_CONTRACT.md` 6.1).
2. Strona główna → „Załóż konto” → wypełnij rejestrację (Ola Wiśniewska, hasło ≥ 10 znaków, oba oświadczenia) → po sukcesie jesteś zalogowana(y) na liście Zadań. Spróbuj tego samego e-maila ponownie → czytelny komunikat o zajętym adresie.
3. Ustawienia: poziom matury „podstawowy”, termin egzaminu 2027-05-05, Dział „Funkcje” → zapisz → odśwież stronę → wartości zostały.
4. Zadania: 5 pozycji (kolejność wg trudności); filtr Działu „Funkcje” zostawia jedno; otwórz „Rozwiąż równanie $x^2 - 5x + 6 = 0$” — wzór wyrenderowany przez KaTeX, widać punkty i Umiejętność.
5. „Prześlij rozwiązanie” → wybierz zdjęcie (plik JPEG/PNG albo aparat na telefonie) → wyślij → ekran Rozwiązania „Odczytujemy…” (ok. 3 s, status odświeża się samo).
6. Pojawia się Transkrypcja (2 kroki z wzorami) → „Zatwierdź” → „Oceniamy…” (ok. 2 s).
7. Ocena 1/2: Kryterium 1 — 1/1, Kryterium 2 — 0/1, błędny krok 2 (błąd rachunkowy), uzasadnienie i Wskazówka („Sprawdź jeszcze raz krok 2 …”), miniatura przesłanego zdjęcia.
8. Wróć do Zadań, rozwiąż Zadanie z funkcją kwadratową (Zadanie 1) → Ocena 2/2 bez Wskazówki.
9. Profil: 2 rozwiązania, średnio 75,0% (3/4 pkt), rozbicie na Działy „Funkcje” i „Równania i nierówności”, dwie ostatnie Oceny z linkami do Rozwiązań.
10. Wyloguj się → zaloguj jako `uczen1@lemat.local` → Profil pusty; wklejenie adresu cudzego Rozwiązania daje „Nie znaleziono”.
11. Scenariusz błędu: z `NEXT_PUBLIC_LEMAT_MOCK_SCENARIUSZ=rozpoznanie-nieczytelne` kolejne przesłanie kończy się komunikatem „Nie udało się odczytać zdjęcia” i przyciskiem „Prześlij ponownie”.
12. Ten sam przebieg (kroki 2–9) bez backendu: `npm --prefix frontend run dev:mock` (dane przykładowe z MSW).

### 12.6 Pytania i rozbieżności dotyczące kontraktu

| ID | Kto zgłasza | Pytanie / rozbieżność | Status / rozstrzygnięcie |
|---|---|---|---|
| K-A-01 | [A] (TASK-127) | Kontrakt 7.4: „plik inny niż JPEG → 415”. Zaimplementowano: 415 tylko dla rozpoznanych innych formatów obrazów (PNG/GIF/BMP/TIFF/WEBP/HEIC); plik bez sygnatury JPEG i bez rozpoznanego innego formatu → 422 `ZDJECIE_NIEPRAWIDLOWE`. Prośba o doprecyzowanie zapisu w kontrakcie (sekcja 10 kontraktu). | otwarte |
| K-B-01 | [B] (TASK-131) | Kontrakt nie wylicza wartości `widocznosc_dla_ucznia` (tylko `PELNA`) — typ ma jedną wartość. | otwarte |
| K-B-02 | [B] (TASK-131) | Kontrakt nie opisuje `NiepewnyFragment` ani `ZastapienieDanychOsobowych` (w POC zawsze puste) — typy przyjęte z `docs/spec/api.md`. | otwarte |
| K-B-03 | [B] (TASK-131) | Kontrakt nie rozstrzyga, czy 412 dla `POST …/zatwierdzenie` zwraca `szczegoly.aktualny` jako `RozwiazanieWyjscie`; założono tak, `BladApi.aktualny` typowany `unknown`. | otwarte |
| K-B-04 | [B] (TASK-131) | `kod_reguly` to zamknięta lista (U-31) — nieznane wartości UI pokazuje komunikatem z serwera. | otwarte |

Zgłoszenia mają prefiks `[A]` lub `[B]` i numer (np. `K-A-01`); rozstrzygnięcie zapisuje się jako zmiana `docs/POC_CONTRACT.md` (wersja `POC-1.x`) albo odpowiedź „bez zmian — kontrakt jest jednoznaczny”.

### 12.7 Wymagane akcje od człowieka (POC)

- **Narzędzia lokalne:** .NET SDK 10.0.x, Node.js ≥ 22 + npm, Docker (PostgreSQL przez `docker-compose.yml`) albo lokalny PostgreSQL 16 (`scripts/db-local-setup.sh`); dla Playwright jednorazowo `npx playwright install` (przeglądarki).
- **Potwierdzenia (OQ-12 … OQ-15):** dodanie `msw` jako `devDependency`; lista `kod_reguly`; interpretacja `DzialWyjscie.poziom_matury`; jawne hasła kont demo wyłącznie lokalnie.
- **Klucze AI:** niepotrzebne — POC działa wyłącznie na Mockach (`Lemat:Modele:Dostawca = mock`); nie ustawiaj kluczy ani `LEMAT_POTWIERDZONA_RETENCJA_DOSTAWCY` w POC.
- **Przegląd merytoryczny 5 Zadań i Kryteriów (U-19):** autorskie dane „w stylu CKE”; przed pokazaniem prawdziwym Uczniom wymagają przeglądu nauczyciela matematyki.
- **Do decyzji właściciela przed MVP:** kolejność zadań MVP po POC (zadania `[MVP]` z częściową realizacją przez POC pozostają `[ ]`, zob. mapowanie w `TASKS.md`).
- **[A] (TASK-121):** do uruchomienia testów Api potrzebny Docker albo lokalny PostgreSQL 16 z `LEMAT_TEST_BAZA`; w Production ustaw `LEMAT_KURSOR_KLUCZ` (sekret, nie commituj).
- **[A] (TASK-123):** `LEMAT_JWT_KLUCZ` (PEM ES256) — w Production wymagany sekret (nie commituj); w Development klucz generuje się sam do `~/.local/share/lemat/jwt-dev-es256.pem`. Cookie `lemat_rt` ma `Secure` tylko przy HTTPS (za reverse proxy ustaw przekazywanie schematu). Brak limitu prób logowania w POC (U-22).
- **[A] (TASK-130):** `scripts/dev-up.sh --reset` (kasuje wolumen Dockera/bazę `lemat_dev` i magazyn zdjęć) nie był uruchamiany — sprawdź ręcznie przed poleganiem na nim; `.env` kopiuj z `.env.example` (nieśledzony), `.logs/` ignorowane przez git. Do demo potrzebne: Docker albo lokalny PostgreSQL 16, .NET SDK 10, `curl`; brak kluczy AI.
- **[A] (TASK-128):** Worker musi działać obok API (ta sama `LEMAT_BAZA`), inaczej Rozwiązania nie przejdą do TRANSKRYPCJA_GOTOWA/OCENIONE; opóźnienia mocka (`Lemat:Poc:*`) dla UI: rozpoznanie ~3 s, ocena ~2 s. Brak kluczy AI (Mock). Decyzja do potwierdzenia: praca MARTWA nie zmienia statusu Rozwiązania (komunikat po 60 s po stronie klienta).
- **[A] (TASK-127):** `LEMAT_MAGAZYN_KLUCZ` (klucz HMAC adresów zdjęć) — w Production wymagany sekret (nie commituj); w Development/E2E działa klucz domyślny. Zdjęcia kartek trafiają do `backend/storage/magazyn` (w `.gitignore`, nie commituj; zawierają dane Uczniów). Mocki: `MagazynObiektowLokalny`, `ProstaObrobkaObrazow` (bez przetwarzania — walidacja JPEG ≤ 10 MB); brak kluczy AI. Do potwierdzenia: K-A-01 (415 vs 422 dla nie-JPEG, 12.6) oraz brak egzekwowania limitu ciała 1 MB dla pozostałych endpointów (poza zakresem).
