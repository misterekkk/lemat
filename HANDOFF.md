# HANDOFF.md — przekazanie projektu Lemat

| Pole | Wartość |
|---|---|
| Data | 2026-11-01 |
| Etap | **Implementacja w toku** — ukończono TASK-001 (1 z 120 pozycji `TASKS.md`; 1 z 60 `[MVP]`) |
| Cel właściciela | **Działające MVP jak najszybciej**, kompletne end-to-end (Uczeń i Nauczyciel), całość uruchamialna lokalnie bez kluczy API (Mock) |
| Architektura | `docs/ARCHITECTURE_DETAILED.md` v3.5; spec: `docs/spec/{ddl,ef-core,api,prompts}.md`; ADR-001 … ADR-075 w `docs/adr/` |
| Stos | .NET 10 (`backend/`), Next.js App Router + TypeScript (`frontend/`), PostgreSQL 16, REST + SSE, Microsoft.Extensions.AI + Semantic Kernel |
| Kod | szkielet solucji `backend/` (TASK-001); `frontend/` nie istnieje; dokumentacja i definicje agentów w `.claude/agents/` |
| Następne TASK | **TASK-002** (Faza 1) |
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

## 5. Jak uruchomić (stan na dziś: tylko szkielet backendu .NET, frontend nie istnieje)

Dziś działa wyłącznie (SDK .NET 10.0.100, `global.json`, `rollForward: latestPatch`; brak zmiennych środowiskowych, bazy i testów):

```bash
dotnet build backend/Lemat.slnx -warnaserror
dotnet format backend/Lemat.slnx --verify-no-changes
dotnet run --project backend/src/Lemat.Tools     # wypisuje "brak poleceń (szkielet)"
```

`Lemat.Api`/`Worker`/`Scheduler` budują się i startują jako puste hosty (bez endpointów i `/zdrowie` do TASK-003). Docelowo (po TASK-005, 013, 026, 112):

```bash
# 1. Baza (Docker; domyślnie port 5433, żeby nie kolidować z lokalnym PostgreSQL na 5432)
cp .env.example .env && docker compose up -d postgres        # albo: scripts/db-local-setup.sh dla PostgreSQL na 5432
# 2. Schemat i dane startowe
dotnet run --project backend/src/Lemat.Tools -- migrate
dotnet run --project backend/src/Lemat.Tools -- seed-dev      # konta demo, konfiguracje Mock, Zadania, Wkładki
# 3. Procesy
dotnet run --project backend/src/Lemat.Api                    # http://localhost:5080/api/v1/zdrowie
dotnet run --project backend/src/Lemat.Worker
npm --prefix frontend install && npm --prefix frontend run dev   # http://localhost:3000
# albo wszystko naraz: scripts/dev-up.sh (TASK-112)
```

Weryfikacja jakości: `scripts/verify-all.sh` (build `-warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `npm run typecheck|lint|test|build`).

---

## 6. ⚠️ Wymagane akcje od człowieka / Konfiguracja

**Do wykonania przez właściciela / człowieka (agent nie może tego zrobić sam):**

0. **Po TASK-001:** brak akcji obowiązkowych; zalecane: zainstalować SDK .NET 10.0.100+ (zgodnie z `backend/global.json`) i zdecydować, czy pakiety z podbitymi wersjami (NU1901–1903) akceptujemy do czasu TASK-004.
1. **Klucze modeli AI (opcjonalne — MVP działa bez nich na Mockach).** Aby użyć prawdziwego LLM do rozpoznawania pisma, Wskazówek i oceny (`ZapasowyLlmAdapter`), ustaw w `.env`/user-secrets:
   `LEMAT_MODELE_DOSTAWCA` (`openai-zgodny` | `azure-openai` | `anthropic`), `LEMAT_MODELE_KLUCZ_API`, `LEMAT_MODELE_ENDPOINT` (dla Azure i punktów zgodnych z OpenAI), nazwy modeli
   (wielomodalny dla `ROZPOZNAWANIE_PISMA`) i **jawne potwierdzenie retencji** `LEMAT_POTWIERDZONA_RETENCJA_DOSTAWCY=true` (A-07: zero-retention dla zdjęć, ≤ 30 dni dla reszty; umowa powierzenia).
   Bez potwierdzenia aplikacja z dostawcą ≠ `mock` nie wystartuje. Nigdy nie commituj kluczy. (Nazwy zmiennych są propozycją P-01; ostateczne zapisuje TASK-004.)
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

**Proponowane zmienne środowiskowe (P-01)**, ostateczne nazwy zapisze TASK-004: `ASPNETCORE_ENVIRONMENT` (`Development`/`E2E`/`Production`), `LEMAT_SRODOWISKO`, `LEMAT_BAZA`, `LEMAT_BAZA_MIGRACJE`, `LEMAT_TEST_BAZA`,
`LEMAT_PG_PORT` (compose), `LEMAT_API_URL` (frontend), `LEMAT_MODELE_DOSTAWCA`, `LEMAT_MODELE_KLUCZ_API`, `LEMAT_MODELE_ENDPOINT`, `LEMAT_POTWIERDZONA_RETENCJA_DOSTAWCY`, `LEMAT_MAGAZYN_SCIEZKA`, `LEMAT_KURSOR_KLUCZ`,
`LEMAT_JWT_KLUCZ` (PEM ES256; w Development generowany), `LEMAT_CZAS_TESTOWY`, `LEMAT_MOCK_OPOZNIENIE_MS`, `LEMAT_KONTA_AUTOZATWIERDZANIE_NAUCZYCIELI` (tylko Development).

---

## 7. Stan prac

| Faza | `[MVP]` ukończone | `[POST-MVP]` ukończone | Następne TASK |
|---|---|---|---|
| 1. Szkielet i środowisko (001 … 012) | 1 / 7 | 0 / 5 | **TASK-002** |
| 2. Model domenowy i baza (013 … 037) | 0 / 16 | 0 / 9 | po TASK-001 … 007 |
| 3. Agenci AI i prace w tle (038 … 070) | 0 / 14 | 0 / 19 | po TASK-013 … 028 |
| 4. API i integracja z Next.js (071 … 108) | 0 / 19 | 0 / 19 | po fazie 3 (dla części API wystarcza TASK-015, 016) |
| 5. Testy i dopracowanie (109 … 120) | 0 / 4 | 0 / 8 | po TASK-089 |

Ścieżka krytyczna MVP: 001 → 002 → 003/004 → 013 → 014 → 015 → 016 → 017 → 018 → 019 → 020 → 021 → 022 → 023 → 024 → 038 → 039/040/041 → 042 → 043 … 047 → 048 → 049 → 050 →
071 → 072 → 073 … 080 → 081 → 082 … 089 → 109 → 110 → 111 → 112. Zadania frontendu 006 oraz 005 można wykonywać równolegle z backendem; 081 … 089 równolegle po 071/072.

**Co działa:** kompilująca się solucja `backend/Lemat.slnx` z 9 pustymi projektami `src` i grafem referencji z 6.4 (Domain bez pakietów; Application bez referencji do Infrastructure/Agents/Models); analizatory
(`TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild`), Central Package Management, `.gitignore`. Brak testów, hostów z endpointami, bazy, frontendu.

**Ostatnia zmiana: TASK-001** (pliki: `TASKS.md` pod TASK-001). Decyzje developera:
- `backend/BannedSymbols.txt` (zakaz `DateTime.Now/UtcNow`, `DateTimeOffset.Now/UtcNow`) dotyczy całego `backend/src`; `backend/BannedSymbols.Sdk.txt` (typy SDK dostawców modeli: `OpenAI.*`, `Azure.AI.*`, `Anthropic.*`) dołączany do wszystkich projektów `src` poza `Lemat.Models` (przez `backend/src/Directory.Build.props`, który dodaje też `Microsoft.CodeAnalysis.BannedApiAnalyzers` z `PrivateAssets=all`).
- Konsekwencja: `Lemat.Domain` ma w grafie ukryty `PackageReference` analizatora (`PrivateAssets=all`, nie trafia do zależności wynikowych) — test `ProjektDomenyBezPakietow` (TASK-002) ma to uwzględnić (ignorować pakiety analizatorów / sprawdzać zależności assembly, nie `PackageReference` w pliku).
- Podniesione wersje pakietów z powodu ostrzeżeń NU1901–NU1903 (przy `-warnaserror`): MailKit 4.18.2, AWSSDK.S3 4.0.104.8, OpenTelemetry.Extensions.Hosting 1.20.1, FluentValidation 12.2.0, Microsoft.IdentityModel.JsonWebTokens 8.23.0; ponadto Cronos 0.13.0, Microsoft.ML/LightGbm 4.0.2 (wszystkie w `backend/Directory.Packages.props`).
- Nagłówki Living Documentation sprawdzone w 4 plikach `Program.cs` — poprawne.

**Wytyczne dla TASK-002:** testy architektury oprzeć na referencjach assembly i przestrzeniach nazw (biblioteki są puste — nie na typach); pakiety testowe (xUnit v3, Shouldly, NSubstitute, FsCheck.Xunit.v3, Testcontainers.PostgreSql, WireMock.Net, Mvc.Testing, NetArchTest.Rules, Microsoft.CodeAnalysis.CSharp) dopisać do `backend/Directory.Packages.props` (sprawdzić NU1901–1903); katalog `backend/tests` poza `backend/src/Directory.Build.props` (analizator banned nie obejmuje testów); `NaglowekLivingDocumentation` zadziała na obecnych `Program.cs`. Nie edytuj kodu `src` poza koniecznością.
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
| OQ-06 | Nazwy zmiennych/opcji z P-01 i klucze konfiguracji (`Lemat:Konta:AutozatwierdzanieNauczycieli`, `Lemat:Poprawki:ZawszeDoWeryfikacji`, `Lemat:Magazyn:*`) nie występują w sekcji 22 | Wprowadza je TASK-004 jako rozszerzenie; zatwierdzić |
| OQ-07 | Czy kolejka `GLOBALNA` ma istnieć w MVP bez pseudonimizacji (U-07)? Alternatywa: Uczeń bez Klasy w MVP w ogóle nie może przesyłać zdjęć (`LEMAT_REJESTRACJA_BEZ_KLASY=false`) | Domyślnie dozwolone na danych demo; do decyzji przed pilotażem |
| OQ-08 | Wagi egzaminacyjne Umiejętności (7.4) w MVP równe — czy wystarczy do testów rekomendacji? | Tak (U-19) |

## 9. Propozycje do słownika

Brak nowych pojęć domenowych. Nazwy tymczasowe poza słownikiem: ścieżka lokalnego magazynu (OQ-05), klucze opcji z OQ-06. Wszystkie nowe typy techniczne (`Mock*`, `Fake*`, `Prosta*`, `*Lokalny`)
mieszczą się w słowach technicznych 3.1 p. 7. Dopisuj tu kolejne propozycje w formacie: pojęcie, definicja, identyfikator, przyczyna.

---

## 10. Gotowy prompt dla kolejnego dewelopera

### 10.1 Pierwsze TASK (kopiuj do agenta `developer`)

```text
Jesteś Senior Developerem .NET i Next.js w projekcie Lemat (repozytorium: /home/user/lemat).
Zrealizuj dokładnie jedno zadanie planu: TASK-001 z TASKS.md ("Solucja .NET: pliki globalne i projekty src").

Najpierw przeczytaj: CLAUDE.md (reguły: słownik pojęć, nagłówki Living Documentation, mocki, komendy weryfikacji),
HANDOFF.md (uproszczenia MVP U-xx, otwarte pytania), docs/CODEBASE_MAP.md oraz opis TASK-001 w TASKS.md.
Architekturę czytaj WYŁĄCZNIE we wskazanych sekcjach (docs/ARCHITECTURE_DETAILED.md ma ~10 000 linii): 5.2, 6.4, 6.5, 6.7, 6.11.3.

Wymagania:
- Kod w backend/ (nazwy plików i katalogów po angielsku, identyfikatory ze słownika po polsku ASCII).
- Na samej górze KAŻDEGO pliku .cs/.ts/.tsx:
  // @Role: ... | @DependsOn: ... | @UsedBy: ... | @Task: TASK-001
- Zewnętrzne zależności (AI, magazyn, e-mail) tylko za interfejsem; domyślnie Mock — aplikacja ma działać bez kluczy.
- Weryfikacja przed zakończeniem: dotnet build backend/Lemat.slnx -warnaserror oraz dotnet test; napraw wszystkie błędy, nie wyłączaj testów.
- Nie edytuj TASKS.md, HANDOFF.md, docs/ (robi to handoff-guardian). Nie commituj.
- Nie wychodź poza zakres TASK-001 (bez kodu domenowego).
Zwróć raport: lista utworzonych/zmienionych plików, wyniki build/test, decyzje i odstępstwa, mocki, wskazówki dla TASK-002.
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
