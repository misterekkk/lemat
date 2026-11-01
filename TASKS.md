# TASKS — plan implementacji Lemat

> Źródło: `docs/ARCHITECTURE_DETAILED.md` v2.4 i `docs/CRITIQUE_LOG.md` (iteracje 1–4; uwagi C4 zaadresowane w v2.4, mapowanie w 23.7).
> Plik jest jedynym rejestrem pracy. Status zmieniaj wyłącznie w tym pliku, zgodnie z workflow z `CLAUDE.md`.
> Identyfikator `TASK-xxx` jest wyłącznie identyfikatorem planu — nigdy nie trafia do kodu, bazy ani API (słowo „task” jest zakazanym synonimem, 3.1 p. 4).

## Jak czytać ten plik

- **Faza** — grupa zadań; faza N+1 zaczyna się, gdy zależności jej zadań są `DONE` (fazy mogą się nakładać, jeśli pozwalają na to zależności).
- **Zależności** — identyfikatory zadań, które muszą mieć status `DONE` przed rozpoczęciem.
- **Realizuje** — elementy architektury, za które zadanie odpowiada (sekcje, ADR, DDL, API, prace w tle, bramki, scenariusze wyścigów C-xx, błędy E-xx, FR/NFR, założenia A-xx, decyzje D-x itd.). Szczegóły zawsze w dokumencie architektury — zadanie nie powtarza specyfikacji.
- **Status:** `TODO` → `IN_PROGRESS` → `DONE (<sha commita>)`; `BLOCKED (<powód>)` gdy zależność zewnętrzna (np. decyzja G-0).

## Definition of Done wspólne dla wszystkich zadań

Każde zadanie, oprócz własnych kryteriów, spełnia:

1. Kod zgodny z regułami nazewnictwa 3.1 i słownikiem pojęć (`docs/glossary.md` + PZS-1); `tests/test_slownik.py` przechodzi (od TASK, który go wprowadza).
2. Testy jednostkowe i integracyjne dla nowej logiki; scenariusze wyścigów C-xx i błędy E-xx wymienione w „Realizuje” mają testy nazwane identyfikatorem (np. `test_c07_…`, `test_e35_…`).
3. CI zielone: ruff, mypy, pytest, `import-linter`; migracje odwracalne i kompatybilne wstecz o jedną wersję.
4. Nowe metryki z 17.3 zarejestrowane, nowe parametry tylko w `lemat/konfiguracja.py` (sekcja 22).
5. Komunikaty dla użytkownika po polsku (NFR-10); brak danych osobowych w logach.
6. Status zadania i `HANDOFF.md` (sekcja „Stan prac”) zaktualizowane w tym samym commicie; pliki w `docs/` niezmienione (chyba że zadanie jawnie tego wymaga po decyzji G-0).

## Fazy

| Faza | Nazwa | Zadania | Liczba |
|---|---|---|---|
| F0 | Fundament repozytorium i narzędzi | TASK-001 … TASK-006 | 6 |
| F1 | Schemat bazy danych (DDL 7.3) | TASK-007 … TASK-018 | 12 |
| F2 | Rdzeń techniczny: Orkiestrator, prace w tle, HTTP, piaskownice | TASK-019 … TASK-035 | 17 |
| F3 | Warstwa modeli AI (porty i adaptery) | TASK-036 … TASK-044 | 9 |
| F4 | Konta i bezpieczeństwo | TASK-045 … TASK-050 | 6 |
| F5 | Treści: Zadania, Kryteria oceniania, Wkładki teoretyczne, Arkusze CKE | TASK-051 … TASK-058 | 8 |
| F6 | Klasy, Zestawy zadań, Przypisania | TASK-059 … TASK-061 | 3 |
| F7 | Profil ucznia, Test diagnostyczny, Agent rekomendujący | TASK-062 … TASK-072 | 11 |
| F8 | Rozwiązania, Odpowiedzi, Agent rozpoznający, Transkrypcja | TASK-073 … TASK-081 | 9 |
| F9 | Agent oceniający, Agent wyjaśniający, trasowanie do Weryfikacji | TASK-082 … TASK-090 | 9 |
| F10 | Weryfikacja i Kolejka weryfikacji | TASK-091 … TASK-099 | 9 |
| F11 | Kalibracja, Próg pewności, bezpieczniki | TASK-100 … TASK-105 | 6 |
| F12 | Agent generujący i Walidacja zadania | TASK-106 … TASK-109 | 4 |
| F13 | RODO: retencja, usuwanie kont, Rejestr Zdarzeń RODO | TASK-110 … TASK-120 | 11 |
| F14 | Aplikacja PWA | TASK-121 … TASK-130 | 10 |
| F15 | Obserwowalność, wdrożenie, bramki G-0 … G-7 | TASK-131 … TASK-143 | 13 |
| **Razem** | | | **143** |

Ścieżka krytyczna do pierwszej Oceny end-to-end na atrapach modeli (z rozstrzygnięciem Weryfikacji): TASK-002 → TASK-007 → TASK-017 → TASK-018 → TASK-019 → TASK-020 → TASK-023 → TASK-036 → TASK-039 → TASK-074 → TASK-077 → TASK-080 → TASK-082 → TASK-085 → TASK-086 → TASK-091 → TASK-093 → TASK-094. Zadania poza ścieżką można realizować równolegle, gdy ich zależności są `DONE`.

## F0 — Fundament repozytorium i narzędzi

### TASK-001 — Onboarding: przegląd dokumentacji i potwierdzenie założeń

- **Faza:** F0
- **Zależności:** —
- **Realizuje:** Sekcje: 1, 1.1, 1.3, 2, 4, 19, 20, 23, 23.1, 23.2, 23.3, 23.5, 23.6, 23.7 · Założenia: A-03, A-10 · Zasady nadrzędne: Z-2
- **Opis:** Przeczytanie `docs/ARCHITECTURE_DETAILED.md` (sekcje 1–6, 19, 20, 23), `docs/CRITIQUE_LOG.md` (podsumowania iteracji) i `HANDOFF.md`. Celem jest wspólne rozumienie zakresu (1.3), założeń A-xx (2), kontekstu (4) i historii decyzji (23).
- **Definition of Done:**
  - [ ] Pytania i niejasności dopisane do sekcji „Otwarte pytania” w `HANDOFF.md`.
  - [ ] Potwierdzono, że lista ADR (19) i odpowiedzi na otwarte pytania (20) nie zawierają decyzji sprzecznych z `docs/requirements.md` (Z-2: wymagania mają pierwszeństwo).
  - [ ] Brak zmian w `docs/`.
- **Status:** TODO

### TASK-002 — Szkielet repozytorium, pakiet `lemat` i CLI procesów

- **Faza:** F0
- **Zależności:** TASK-001
- **Realizuje:** Sekcje: 5, 6, 6.1 · ADR: ADR-001
- **Opis:** Struktura `src/lemat/<moduł>/` zgodna z 6.1/6.2 (moduły domenowe z publiczną fasadą `api.py`), `tests/`, `pyproject.toml` (Python 3.12, FastAPI, Pydantic v2, SQLAlchemy 2, Alembic), komenda `lemat api|worker|scheduler`.
- **Definition of Done:**
  - [ ] `pip install -e .[dev]` i `lemat --help` działają; `lemat api`, `lemat worker`, `lemat scheduler` startują puste procesy.
  - [ ] Katalogi modułów z 6.2 istnieją z plikiem `api.py` (fasada) i `__init__.py`; `pytest` przechodzi (test dymny).
  - [ ] ruff + mypy (strict dla `lemat.*`) przechodzą lokalnie.
- **Status:** TODO

### TASK-003 — Parametry systemu w `lemat/konfiguracja.py`

- **Faza:** F0
- **Zależności:** TASK-002
- **Realizuje:** Sekcje: 22 · Pliki testowe/danych: `lemat/konfiguracja.py`
- **Opis:** Wszystkie parametry z tabeli 22 jako pola ustawień (Pydantic Settings) z wartościami domyślnymi, nadpisywalne zmiennymi `LEMAT_*`. Progi, kalibratory i Konfiguracja modeli NIE są tu (baza danych).
- **Definition of Done:**
  - [ ] Każdy wiersz tabeli 22 ma pole z wartością domyślną i komentarzem z numerem sekcji (test parametryczny porównuje listę pól z listą w teście).
  - [ ] Flagi `LEMAT_REJESTRACJA_BEZ_KLASY`, `LEMAT_POJEMNOSC_LACZNA_KUBELKOW` i parametry środowiska testowego są dostępne.
  - [ ] Brak literałów liczbowych z tabeli 22 w kodzie domenowym (kontrola w code review).
- **Status:** TODO

### TASK-004 — CI: lint, typy, testy, kontrakty `import-linter`

- **Faza:** F0
- **Zależności:** TASK-002
- **Realizuje:** Sekcje: 5.2, 6.2 · ADR: ADR-003 · NFR: NFR-06
- **Opis:** Pipeline CI (ruff, mypy, pytest z PostgreSQL 16 w kontenerze) oraz kontrakty `import-linter`: „layers + independence” dla modułów domenowych (import tylko przez `lemat.<moduł>.api`), SDK dostawców modeli wyłącznie w `lemat.modele.adaptery.*`, agenci bez FastAPI/SQLAlchemy.
- **Definition of Done:**
  - [ ] CI uruchamia się na każdym PR i blokuje merge przy błędzie.
  - [ ] Celowo błędny import (np. `openai` w `lemat.ocenianie`) powoduje błąd kontraktu (test kontrolny w repo).
  - [ ] Kontrakty opisane w `CLAUDE.md` (sekcja reguł zależności).
- **Status:** TODO

### TASK-005 — Środowisko lokalne: Docker Compose, MinIO, atrapy modeli, obraz kontenera

- **Faza:** F0
- **Zależności:** TASK-002
- **Realizuje:** Sekcje: 5.1, 17.5
- **Opis:** Jeden obraz `lemat:<git-sha>`; `docker-compose.yml` dla środowiska `lokalne`: PostgreSQL 16, MinIO (buckety `zdjecia`, `zdjecia-maskowane`, `zbior-treningowy`, `importy`, `zbiory-testowe`, `rejestr-zdarzen-rodo` z Object Lock, bez wersjonowania na bucketach zdjęć), Caddy, `api`, `worker`, `scheduler`, serwer SMTP testowy.
- **Definition of Done:**
  - [ ] `docker compose up` uruchamia wszystkie kontenery; `GET /api/v1/zdrowie` zwraca 200 (gdy powstanie endpoint).
  - [ ] Bucket `rejestr-zdarzen-rodo` ma Object Lock COMPLIANCE (lokalnie retencja skrócona, konfigurowalna).
  - [ ] README sekcja „Uruchomienie lokalne” zaktualizowana.
- **Status:** TODO

### TASK-006 — Test słownika `tests/test_slownik.py` — ramy, słownik tokenów, wyjątki

- **Faza:** F0
- **Zależności:** TASK-002
- **Realizuje:** Sekcje: 3, 3.1, 3.2, 3.3, 3.4 · ADR: ADR-016 · Pliki testowe/danych: `tests/test_slownik.py`, `tests/slownik_tokeny.yaml`, `tests/slownik_wyjatki.yaml` · Zasady nadrzędne: Z-6
- **Opis:** Generator `tests/slownik_tokeny.yaml` z identyfikatorów 3.2/3.3, terminów technicznych 3.1 p. 2 i 6 oraz zamkniętej listy słów ogólnych (z dopiskami wersji 2.4). Implementacja części 3 (słowa zarezerwowane: `odpowiedz*`, `wariant*`, `krok*`, `wynik*`, `kolejk*`, `zadan*`, `zdje*`, `wstrzym*`) i 4 (zakazane synonimy) na dowolnym zbiorze identyfikatorów.
- **Definition of Done:**
  - [ ] Przypadki kontrolne z 3.4 kończą test błędem: kolumna `stan_bezpiecznika.zdjeto_at`, ścieżka `/admin/bezpieczniki/zdjecie`, identyfikator ze słowem `task`/`student`.
  - [ ] `tests/slownik_wyjatki.yaml` zawiera zamknięte listy z 3.4 (tabele techniczne, tabele łączące, dozwolone użycia słów zarezerwowanych).
  - [ ] Test działa w CI na identyfikatorach z kodu Python (AST) — części 1–2 dołącza TASK „slownik-baza”.
- **Status:** TODO

## F1 — Schemat bazy danych (DDL 7.3)

### TASK-007 — Alembic, konwencje schematu i rozszerzenia PostgreSQL

- **Faza:** F1
- **Zależności:** TASK-002, TASK-005
- **Realizuje:** Sekcje: 7, 7.2, 7.3 · DDL — rozszerzenia: `citext`, `pg_trgm`
- **Opis:** Konfiguracja Alembic w `src/lemat/migracje/` (propozycja ścieżki), konwencje 7.2: `uuid` z `gen_random_uuid()`, `timestamptz` UTC, `version integer NOT NULL DEFAULT 1`, nazewnictwo `ux_`/`ix_`/`fk_`/`trg_`. Reguła migracji „rozszerz → migruj → zawęź” (17.5).
- **Definition of Done:**
  - [ ] `alembic upgrade head` i `downgrade base` działają na pustej bazie w CI.
  - [ ] Rozszerzenia `citext` i `pg_trgm` tworzone w pierwszej migracji.
  - [ ] Szablon migracji z checklistą kompatybilności wstecz o jedną wersję.
- **Status:** TODO

### TASK-008 — Typy wyliczeniowe (wszystkie `CREATE TYPE` z 7.3)

- **Faza:** F1
- **Zależności:** TASK-007
- **Realizuje:** DDL — typy wyliczeniowe: `rola_uzytkownika`, `status_konta`, `poziom_matury`, `rodzaj_zadania`, `forma_zamknieta`, `pochodzenie_zadania`, `zasieg_zadania`, `status_zadania`, `status_wkladki`, `status_rozwiazania`, `zrodlo_transkrypcji`, `status_oceny`, `zrodlo_oceny`, `typ_bledu`, `zrodlo_wskazowki`, `powod_weryfikacji`, `status_weryfikacji`, `powod_anulowania`, `typ_kolejki_weryfikacji`, `typ_elementu_sciezki`, `cel_elementu`, `status_elementu_sciezki`, `strategia_rekomendacji`, `rola_modelu`, `typ_pytania`, `kontekst_decyzji`, `zrodlo_wyniku`, `zrodlo_probki`, `zrodlo_progu`, `zrodlo_kalibratora`, `powod_uniewaznienia_wyniku`, `status_pracy`, `status_zlecenia`, `status_importu`, `status_testu`, `decyzja_przegladu`, `rezultat_walidacji`, `inicjalizacja_opanowania`, `status_zgloszenia_zadania`, `typ_zdarzenia_rodo`, `zrodlo_zdarzenia_rodo`, `typ_bezpiecznika`, `powod_eskalacji`
- **Opis:** Jedna migracja ze wszystkimi 43 typami ENUM dokładnie w brzmieniu 7.3 oraz odpowiadające im `StrEnum` w Pythonie (`lemat.<moduł>.typy`).
- **Definition of Done:**
  - [ ] Test porównuje `pg_enum` po migracji z listą wartości w `StrEnum` (identyczne zbiory).
  - [ ] Wartości przechodzą części 2–3 testu słownika.
- **Status:** TODO

### TASK-009 — Tabele kont, Zdarzeń RODO i kalendarza

- **Faza:** F1
- **Zależności:** TASK-008
- **Realizuje:** DDL — tabele: `uzytkownik`, `uczen`, `nauczyciel`, `token_odswiezania`, `proba_logowania`, `zdarzenie_rodo`, `dzien_wolny` · DDL — indeksy: `ix_proba_logowania_email`, `ix_zdarzenie_rodo_uzytkownik`
- **Opis:** DDL sekcji „KONTA” z 7.3 wraz z CHECK (m.in. `zdarzenie_rodo`: `(zrodlo = 'WORKER') = (typ = 'USUNIECIE_WYKONANE')`, `dane ? 'rola'`).
- **Definition of Done:**
  - [ ] Migracja odtwarza kolumny, CHECK i indeksy 1:1 z 7.3 (test schematu porównuje `information_schema`).
  - [ ] Testy negatywne CHECK `zdarzenie_rodo` (brak `rola`, niezgodne `zrodlo`/`typ`) kończą się błędem.
- **Status:** TODO

### TASK-010 — Funkcja `dodaj_dni_robocze` i dane dni ustawowo wolnych 2026–2030

- **Faza:** F1
- **Zależności:** TASK-009
- **Realizuje:** ADR: ADR-043 · DDL — funkcje: `dodaj_dni_robocze` · Założenia: A-15
- **Opis:** Funkcja SQL `dodaj_dni_robocze(od timestamptz, n integer) RETURNS timestamptz` (STABLE, strefa Europe/Warsaw) wg opisu w 7.3 i migracja danych `dzien_wolny` 2026–2030.
- **Definition of Done:**
  - [ ] Testy przykładów z 7.3: piątek 18:00 + 3 → środa 18:00; sobota 10:00 + 3 → czwartek 08:00; przypadek z dniem wolnym w środku.
  - [ ] Dane dni wolnych dla Polski 2026–2030 w migracji danych.
- **Status:** TODO

### TASK-011 — Tabele treści: Działy, Umiejętności, Wkładki, Arkusze CKE, Zadania, Kryteria

- **Faza:** F1
- **Zależności:** TASK-008, TASK-009
- **Realizuje:** Sekcje: 7.1, 7.5, 7.5.1, 7.5.2, 7.5.3 · DDL — tabele: `dzial`, `umiejetnosc`, `umiejetnosc_wymagana`, `wkladka_teoretyczna`, `arkusz_cke`, `import_arkusza`, `zlecenie_generowania`, `zadanie`, `kalibracja_trudnosci`, `zadanie_wersja`, `zadanie_umiejetnosc`, `kryterium_oceniania`, `zalacznik_zadania`, `walidacja_zadania`, `przeglad_zadania`, `zgloszenie_bledu_zadania`, `wstrzymanie_w_doborze` · DDL — triggery: `trg_umiejetnosc_glowna_w_dziale`, `trg_suma_kryteriow` · DDL — indeksy: `ux_wkladka_opublikowana`, `ix_wkladka_automatyczne_odrzucone`, `ix_zlecenie_systemowe_dzien`, `ix_zadanie_dobor`, `ix_zadanie_autor`, `ix_zadanie_diagnostyczne`, `ix_kalibracja_poziom`, `ix_zadanie_wersja_tresc_trgm`, `ux_zadanie_umiejetnosc_glowna`, `ux_zgloszenie_zadania_otwarte`, `ix_zgloszenie_zadania_status`
- **Opis:** DDL sekcji „DZIAŁY, UMIEJĘTNOŚCI, WKŁADKI” i „ARKUSZE CKE, ZADANIA” z 7.3, constraint triggery `trg_umiejetnosc_glowna_w_dziale` i `trg_suma_kryteriow` (DEFERRABLE: suma `punkty_max` per wariant, ≤ 1 kryterium wyniku końcowego, ≤ 24 kryteria łącznie) oraz modele Pydantic schematów JSONB 7.5.1–7.5.3.
- **Definition of Done:**
  - [ ] Test schematu zgodny z 7.3; ERD 7.1 odtworzone (FK).
  - [ ] Testy triggerów: niezgodna suma kryteriów i umiejętność główna spoza Działu → wyjątek przy COMMIT.
  - [ ] Walidatory Pydantic `opcje_zamkniete`, `rozwiazanie_wzorcowe`, `wynik_koncowy_wzorcowy` z przykładami z 7.5.
- **Status:** TODO

### TASK-012 — Dane referencyjne: 13 Działów i Umiejętności z pliku YAML

- **Faza:** F1
- **Zależności:** TASK-011
- **Realizuje:** Sekcje: 7.4 · Pliki testowe/danych: `src/lemat/tresci/dane/umiejetnosci.yaml` · Założenia: A-04
- **Opis:** Migracja danych z 13 Działami (7.4) oraz Umiejętnościami z `src/lemat/tresci/dane/umiejetnosci.yaml` (kody `P.V.9` / `R.XIII.2`, jeden wpis = punkt podstawy programowej 2018). Skrypt wag egzaminacyjnych (udział punktów w Arkuszach CKE 2023–2026, normalizacja w obrębie poziomu matury).
- **Definition of Done:**
  - [ ] Po migracji: 13 Działów, `XIII` tylko rozszerzony; każda Umiejętność ma Dział i poziom.
  - [ ] Plik YAML ma schemat walidowany testem; zmiana Działów/Umiejętności nie wymaga zmian kodu (A-04).
- **Status:** TODO

### TASK-013 — Tabele Klas, Zestawów zadań i Przypisań

- **Faza:** F1
- **Zależności:** TASK-009, TASK-011
- **Realizuje:** DDL — tabele: `klasa`, `czlonkostwo_klasy`, `zestaw_zadan`, `pozycja_zestawu`, `przypisanie`, `przypisanie_uczen` · DDL — indeksy: `ix_czlonkostwo_uczen`, `ix_przypisanie_nauczyciel`, `ix_przypisanie_uczen_uczen`
- **Opis:** DDL sekcji „KLASY, ZESTAWY, PRZYPISANIA” z 7.3 (kod zaproszenia 8 znaków `[A-HJ-NP-Z2-9]`, ważny 14 dni).
- **Definition of Done:**
  - [ ] Test schematu zgodny z 7.3.
  - [ ] CHECK formatu kodu zaproszenia przetestowany.
- **Status:** TODO

### TASK-014 — Tabele Profilu ucznia, Testu diagnostycznego, Ścieżki nauki i Rekomendacji

- **Faza:** F1
- **Zależności:** TASK-009, TASK-011, TASK-013
- **Realizuje:** DDL — tabele: `profil_ucznia`, `opanowanie_umiejetnosci`, `test_diagnostyczny`, `pozycja_testu_diagnostycznego`, `sciezka_nauki`, `element_sciezki`, `rekomendacja` · DDL — indeksy: `ix_opanowanie_powtorki`, `ux_test_jeden`, `ux_jeden_proponowany`, `ix_element_wygasanie`
- **Opis:** DDL sekcji „PROFIL UCZNIA, TEST DIAGNOSTYCZNY” i „ŚCIEŻKA NAUKI, REKOMENDACJE” z 7.3 (w tym kolumny Serii `sciezka_nauki.seria_*`, CHECK reguł rekomendacji).
- **Definition of Done:**
  - [ ] Test schematu zgodny z 7.3.
  - [ ] `ux_jeden_proponowany` i `ux_test_jeden` mają testy naruszenia.
- **Status:** TODO

### TASK-015 — Tabele Rozwiązań, zdjęć, Transkrypcji, Odpowiedzi i trigger kontekstu próby

- **Faza:** F1
- **Zależności:** TASK-013, TASK-014
- **Realizuje:** DDL — tabele: `rozwiazanie`, `zdjecie_rozwiazania`, `kopia_treningowa`, `transkrypcja`, `krok_transkrypcji`, `odpowiedz` · DDL — triggery: `trg_kontekst_proby` · DDL — indeksy: `ix_rozwiazanie_uczen`, `ix_rozwiazanie_status`, `ux_rozwiazanie_element`, `ux_rozwiazanie_przypisanie`, `ix_zdjecie_do_usuniecia`, `ix_kopia_do_usuniecia`, `ix_kopia_uczen`, `ux_odpowiedz_element`, `ux_odpowiedz_test`, `ux_odpowiedz_przypisanie`
- **Opis:** DDL sekcji „ROZWIĄZANIA, TRANSKRYPCJE, ODPOWIEDZI” z 7.3 i trigger `trg_kontekst_proby` (BEFORE INSERT na `rozwiazanie` i `odpowiedz`: dziedziczenie `przypisanie_id` z elementu `R-PRZYPISANIE`, wyjątek `KONTEKST_PROBY_NIESPOJNY`).
- **Definition of Done:**
  - [ ] Test schematu zgodny z 7.3 (w tym CHECK `usun_po` ≤ 90 dni od przesłania).
  - [ ] Testy triggera: dziedziczenie, niespójny kontekst → wyjątek, dozwolone kombinacje {E}, {P}, {E,P}, {T}.
- **Status:** TODO

### TASK-016 — Tabele Konfiguracji modeli, ogranicznika, kalibracji, bezpieczników i dziennika decyzji

- **Faza:** F1
- **Zależności:** TASK-008, TASK-011
- **Realizuje:** DDL — tabele: `konfiguracja_modelu`, `limit_szybkosci_modelu`, `kalibrator_pewnosci`, `prog_pewnosci`, `stan_bezpiecznika`, `decyzja_modelu` · DDL — indeksy: `ux_konfiguracja_aktywna`, `ux_prog_aktywny`, `ix_decyzja_kontekst`, `ix_decyzja_uczen`
- **Opis:** DDL sekcji „MODELE, KALIBRACJA” z 7.3: `limit_szybkosci_modelu` jako `UNLOGGED` z kluczem `(konfiguracja_modelu_id, wykonawca, pula)` (wersja 2.4), `stan_bezpiecznika` z kolumnami `dezaktywowany_przez`, `dezaktywowano_at`, `uzasadnienie_dezaktywacji`, `wyjscie_z_blokady_at`, `decyzja_modelu` partycjonowana `PARTITION BY RANGE (created_at)`.
- **Definition of Done:**
  - [ ] Test schematu zgodny z 7.3; partycje `decyzja_modelu` na 3 miesiące naprzód tworzone w migracji.
  - [ ] CHECK progu (zakres 0.800–0.990 lub 1.000; dowód zawsze 1.000) przetestowany.
- **Status:** TODO

### TASK-017 — Tabele Ocen, Weryfikacji, szkiców, Próbek kalibracyjnych, Wyników zadania i triggery niezmienności

- **Faza:** F1
- **Zależności:** TASK-015, TASK-016
- **Realizuje:** Sekcje: 7.5.4 · DDL — tabele: `ocena`, `ocena_kryterium`, `weryfikacja`, `szkic_rozstrzygniecia`, `probka_kalibracyjna`, `wynik_zadania` · DDL — triggery: `trg_ocena_niezmienna`, `trg_weryfikacja_wersja`, `trg_powody_poczatkowe` · DDL — indeksy: `ux_ocena_aktualna`, `ux_weryfikacja_otwarta`, `ix_weryfikacja_kolejka`, `ix_weryfikacja_blokady`, `ux_probka_ocena_auto`, `ix_probka_kalibracja`, `ix_wynik_uczen`, `ix_wynik_uczen_zadanie`, `ux_wynik_ocena`, `ux_wynik_odpowiedz`, `ix_wynik_do_profilu` · Zasady nadrzędne: Z-5
- **Opis:** DDL sekcji „OCENY, WERYFIKACJE” z 7.3 i triggery: `trg_ocena_niezmienna` (tylko → `ZASTAPIONA` oraz jednokrotny zapis Wskazówki), `trg_weryfikacja_wersja` (wersja treści, ADR-037), `trg_powody_poczatkowe` (`POWODY_NIEZMIENNE`). Schemat `weryfikacja.zgloszenie` (7.5.4).
- **Definition of Done:**
  - [ ] Testy triggerów: każda niedozwolona zmiana `ocena` → `OCENA_NIEZMIENNA`; zmiana kolumn blokady/trasowania nie zmienia `weryfikacja.version`, zmiana treści podnosi ją o 1.
  - [ ] Wszystkie CHECK `weryfikacja` mają testy negatywne.
  - [ ] Model Pydantic `zgloszenie` (1–5 kroków, opis ≤ 500).
- **Status:** TODO

### TASK-018 — Tabele infrastruktury: prace w tle, idempotencja, powiadomienia, audyt

- **Faza:** F1
- **Zależności:** TASK-008
- **Realizuje:** DDL — tabele: `praca_w_tle`, `klucz_idempotencji`, `powiadomienie`, `dziennik_audytu` · DDL — indeksy: `ux_praca_dedup`, `ux_praca_dedup_oczekujace`, `ix_praca_pobieranie`, `ix_praca_dzierzawa`, `ix_powiadomienie_nieprzeczytane`
- **Opis:** DDL sekcji „ORKIESTRATOR, INFRASTRUKTURA” z 7.3 z triggerem `AFTER INSERT` wykonującym `pg_notify('praca_w_tle', pula)` (13.4) i partycjami rocznymi `dziennik_audytu`.
- **Definition of Done:**
  - [ ] Test schematu zgodny z 7.3; dwa indeksy deduplikacji z predykatami z 13.4.
  - [ ] Test: INSERT pracy wysyła NOTIFY z nazwą puli.
- **Status:** TODO

## F2 — Rdzeń techniczny: Orkiestrator, prace w tle, HTTP, piaskownice

### TASK-019 — Fundament maszyn stanów i zapisów warunkowych

- **Faza:** F2
- **Zależności:** TASK-018
- **Realizuje:** Sekcje: 8, 15, 15.1, 15.2 · Macierz błędów: E-13
- **Opis:** Biblioteka w `lemat.orkiestrator`: deklaracja dozwolonych przejść, zapis warunkowy `UPDATE … WHERE status = :oczekiwany AND version = :v`, odrzucenie i log `rezultat_przestarzaly`, zasady 15.1 (najpierw zmiana starego wiersza, potem INSERT; READ COMMITTED; `FOR UPDATE` tylko w krótkich sekcjach).
- **Definition of Done:**
  - [ ] Test: równoległe dwa zapisy warunkowe — jeden wygrywa, drugi zwraca rezultat przestarzały i metrykę.
  - [ ] Niedozwolone przejście → wyjątek domenowy mapowany na `409 …_NIEPRAWIDLOWY_STAN`.
- **Status:** TODO

### TASK-020 — Prace w tle: pobieranie, dzierżawa, ponowienia, deduplikacja, pule workera

- **Faza:** F2
- **Zależności:** TASK-018, TASK-019
- **Realizuje:** Sekcje: 6.3, 13, 13.3, 13.4 · ADR: ADR-002 · Scenariusze wyścigów: C-05, C-06 · Macierz błędów: E-12, E-24
- **Opis:** Mechanika 13.4: pobranie `FOR UPDATE SKIP LOCKED`, dzierżawa 60 s odnawiana co 15 s, sukces / błąd przejściowy / `MARTWA` z `po_wyczerpaniu` w tej samej transakcji, `zlec(typ, …)` z predykatami deduplikacji per typ (`DO NOTHING`, `DO UPDATE` dla `SPRAWDZ_POPRAWKI_TRANSKRYPCJI`), LISTEN/NOTIFY + odpytywanie 1 s, pule R 15 / S 4 / O 15 / P 6 / W 4 korutyn i `ProcessPoolExecutor` (4 procesy), SIGTERM z 30 s na dokończenie.
- **Definition of Done:**
  - [ ] Testy integracyjne: dwie korutyny nie pobierają tej samej pracy; wygasła dzierżawa wraca do `OCZEKUJE`; po `max_prob` → `MARTWA` i wywołany `po_wyczerpaniu`.
  - [ ] Test C-05/C-06: ponowne dostarczenie pracy po awarii kończy się bez efektu.
  - [ ] Nieznany wyjątek handlera (E-24) → ponowienie z logiem i śladem, bez utraty pracy.
- **Status:** TODO

### TASK-021 — Zdarzenia domenowe w procesie (publikacja w transakcji)

- **Faza:** F2
- **Zależności:** TASK-020
- **Realizuje:** Sekcje: 13.6
- **Opis:** Szyna zdarzeń w procesie: moduły publikują obiekty zdarzeń wewnątrz transakcji, Orkiestrator zamienia je na prace w tle lub powiadomienia w tej samej transakcji (bez zewnętrznego brokera). Konkretne reakcje implementują zadania domenowe.
- **Definition of Done:**
  - [ ] Test: rollback transakcji usuwa zarówno zmianę stanu, jak i zleconą pracę.
  - [ ] Rejestr zdarzeń z 13.6 jako typy Pydantic; brak reakcji dla zdarzenia → błąd w teście kontraktu.
- **Status:** TODO

### TASK-022 — Scheduler z blokadą doradczą i odzyskiwaniem dzierżaw

- **Faza:** F2
- **Zależności:** TASK-020
- **Realizuje:** Sekcje: 13.5 · Scenariusze wyścigów: C-19
- **Opis:** Proces `lemat scheduler`: `pg_try_advisory_lock(7_420_001)` na dedykowanym połączeniu (druga instancja w gotowości), tabela cykli 13.5, odzyskiwanie porzuconych dzierżaw co 30 s. Konkretne prace rejestrują swoje cykle w zadaniach domenowych.
- **Definition of Done:**
  - [ ] Test C-19: dwie instancje — dokładnie jedna aktywna; po zabiciu aktywnej druga przejmuje w ≤ 10 s.
  - [ ] Cykle z 13.5 zarejestrowane deklaratywnie (jedna tabela w kodzie, test porównuje z dokumentem).
- **Status:** TODO

### TASK-023 — Aplikacja FastAPI: konwencje API, model błędu RFC 9457, OpenAPI, zdrowie

- **Faza:** F2
- **Zależności:** TASK-002, TASK-007
- **Realizuje:** Sekcje: 12, 12.1, 12.2, 12.3, 12.4, 14.1 · Macierz błędów: E-14, E-15, E-25
- **Opis:** `lemat.http`: prefiks `/api/v1`, paginacja kursorem, `ETag`/`If-Match` (412 `KONFLIKT_WERSJI`), model błędu RFC 9457 z kodami domenowymi (12.2), `/api/v1/openapi.json`, `/api/v1/zdrowie` (baza + magazyn), generowanie typów TS z modeli Pydantic (12.4), komunikaty błędów po polsku.
- **Definition of Done:**
  - [ ] Kontrakt błędu RFC 9457 przetestowany dla 400/401/403/404/409/412/422/429/503.
  - [ ] Niedostępny PostgreSQL (E-14) → `503 USLUGA_NIEDOSTEPNA`; nieznany wyjątek (E-25) → `500` z `trace_id`, bez szczegółów.
  - [ ] Typy TS generowane w CI i zgodne z 12.4.
- **Status:** TODO

### TASK-024 — Test słownika — części 1–2 na bazie, OpenAPI i schematach JSON modeli

- **Faza:** F2
- **Zależności:** TASK-006, TASK-010, TASK-012, TASK-017, TASK-018, TASK-023
- **Realizuje:** Sekcje: 3.4 · Pliki testowe/danych: `lemat/modele/schematy/*.json`
- **Opis:** Rozszerzenie `tests/test_slownik.py`: część 1 (tabele z `information_schema.tables` mają wpis w 3.2/3.3 lub na liście wyłączeń), część 2 (tokeny kolumn, `pg_enum`, ścieżek i właściwości `/api/v1/openapi.json`, kluczy `lemat/modele/schematy/*.json` i typów prac w tle).
- **Definition of Done:**
  - [ ] Test przechodzi na bazie po `alembic upgrade head`.
  - [ ] Każdy nowy endpoint/kolumna jest automatycznie objęty testem (brak ręcznych list poza `slownik_wyjatki.yaml`).
- **Status:** TODO

### TASK-025 — Idempotency-Key i tabela `klucz_idempotencji`

- **Faza:** F2
- **Zależności:** TASK-018, TASK-023
- **Realizuje:** Scenariusze wyścigów: C-01
- **Opis:** Middleware/zależność dla endpointów oznaczonych „+ Idempotency-Key” w 12.3: zapis klucza, odtwarzanie tej samej odpowiedzi, wygaśnięcie `wygasa_at` (czyszczenie w `WYCZYSC_DANE_TECHNICZNE`).
- **Definition of Done:**
  - [ ] Test C-01: dwa identyczne żądania → jedna zmiana stanu, ta sama odpowiedź.
  - [ ] Ten sam klucz z innym ciałem → `422`.
- **Status:** TODO

### TASK-026 — Limity szybkości żądań użytkowników (12.7)

- **Faza:** F2
- **Zależności:** TASK-023
- **Realizuje:** Sekcje: 12.7
- **Opis:** Wspólny mechanizm limitów opartych o `count(*)` po indeksach (12.7) oraz konfiguracja Caddy `rate_limit` 600/min na IP. Konkretne limity włączają zadania endpointów.
- **Definition of Done:**
  - [ ] Przekroczenie limitu → `429` z `Retry-After` i kodem domenowym.
  - [ ] Tabela limitów 12.7 w jednym miejscu kodu, test porównuje z dokumentem.
- **Status:** TODO

### TASK-027 — Uwierzytelnianie w HTTP i uprawnienia na poziomie obiektów (12.8)

- **Faza:** F2
- **Zależności:** TASK-023
- **Realizuje:** Sekcje: 12.8
- **Opis:** Zależności FastAPI ról (`U`, `N`, `NR`, `A`, `*`) i polityki obiektowe z 12.8 (własne zasoby, Uczniowie Klasy, weryfikator globalny, NR) jako funkcje w `lemat.<moduł>.api`.
- **Definition of Done:**
  - [ ] Macierz testów 12.8: każda komórka tabeli ma test (dozwolone/zabronione).
  - [ ] Brak dostępu Administratora do zdjęć (minimalizacja) przetestowany.
- **Status:** TODO

### TASK-028 — Port `MagazynObiektow` i adapter S3/MinIO

- **Faza:** F2
- **Zależności:** TASK-002, TASK-005
- **Realizuje:** Macierz błędów: E-02
- **Opis:** Port `MagazynObiektow` (13.1: `zapisz`, `pobierz`, `usun` idempotentne, `podpisany_url` 10 min) i adapter S3 (SSE, region UE). Klucze obiektów wg 16.4 (`zdjecia/{uczen_id}/…`).
- **Definition of Done:**
  - [ ] Testy integracyjne z MinIO.
  - [ ] Niedostępny magazyn przy zapisie → wyjątek mapowany na `503` i rollback (E-02).
- **Status:** TODO

### TASK-029 — Moduł `lemat.audyt`: dziennik działań

- **Faza:** F2
- **Zależności:** TASK-018
- **Realizuje:** DDL — tabele: `dziennik_audytu`
- **Opis:** Funkcja `zapisz_audyt(akcja, zasob, zasob_id, dane)` wywoływana w transakcji operacji (Nauczyciel, Administrator, zmiany systemowe). Bez FK do użytkownika (wpis przeżywa usunięcie konta).
- **Definition of Done:**
  - [ ] Każda akcja administracyjna i rozstrzygnięcie Weryfikacji ma wpis audytu (test kontraktowy na liście akcji).
  - [ ] Partycje roczne, retencja 2 lata (16.4).
- **Status:** TODO

### TASK-030 — Powiadomienia w aplikacji i ich API

- **Faza:** F2
- **Zależności:** TASK-021, TASK-027
- **Realizuje:** API: `GET /powiadomienia`, `POST /powiadomienia/{id}/przeczytanie`
- **Opis:** Moduł `lemat.powiadomienia`: tworzenie powiadomień z reakcji na zdarzenia (typy z 13.6), `GET /powiadomienia?nieprzeczytane=true`, oznaczanie przeczytania. Brak push i e-mail o ocenach (1.3).
- **Definition of Done:**
  - [ ] Testy API obu endpointów (tylko własne powiadomienia).
  - [ ] Typy powiadomień jako `StrEnum` przechodzą test słownika.
- **Status:** TODO

### TASK-031 — Praca `WYSLIJ_EMAIL` i adapter SMTP

- **Faza:** F2
- **Zależności:** TASK-020
- **Realizuje:** Prace w tle: `WYSLIJ_EMAIL` · Macierz błędów: E-23
- **Opis:** Wysyłka e-mail wyłącznie z workera (`api` nie łączy się z SMTP), szablony po polsku, ponowienia 1, 5, 15, 60 min.
- **Definition of Done:**
  - [ ] Test z serwerem SMTP testowym; po wyczerpaniu prób wpis w logu (E-23), bez danych wrażliwych.
- **Status:** TODO

### TASK-032 — Piaskownica SymPy i port `SprawdzanieSymboliczne`

- **Faza:** F2
- **Zależności:** TASK-020
- **Realizuje:** ADR: ADR-013 · Macierz błędów: E-10
- **Opis:** Pula procesów z `RLIMIT` (2 s CPU na parę, 3 s na Rozwiązanie, 512 MiB), `parse_latex` z backendem `lark`, `postac_liniowa`, `rownowaznosc_krokow`, `rownowaznosc_wyniku` → `Rownowaznosc` (`ROWNOWAZNE`/`NIEROWNOWAZNE`/`NIEROZSTRZYGNIETE`).
- **Definition of Done:**
  - [ ] Testy: przekroczenie CPU/RAM → `NIEROZSTRZYGNIETE` bez awarii workera (E-10).
  - [ ] Wejście od Ucznia nigdy nie trafia do `sympify` bez `parse_latex` (test bezpieczeństwa).
- **Status:** TODO

### TASK-033 — Normalizacja zdjęć w piaskownicy procesowej `api`

- **Faza:** F2
- **Zależności:** TASK-023
- **Realizuje:** ADR: ADR-045 · Macierz błędów: E-35
- **Opis:** Pula 2 procesów na replikę `api` (`RLIMIT_AS` 512 MiB, `RLIMIT_CPU` 5 s, 8 s czasu ściennego), limit 40 Mpx i 12 000 px na wymiar, usunięcie EXIF, obsługa HEIC (`pillow-heif`), oczekiwanie > 5 s → `503`; metryka `normalizacja_oczekiwanie_sekundy`.
- **Definition of Done:**
  - [ ] Test bomby dekompresyjnej (E-35) → `422 ZDJECIE_NIEPRAWIDLOWE`, proces `api` nie traci pętli zdarzeń.
  - [ ] Znormalizowany JPEG bez EXIF (test).
- **Status:** TODO

### TASK-034 — Prace utrzymaniowe: partycje i czyszczenie danych technicznych

- **Faza:** F2
- **Zależności:** TASK-016, TASK-020, TASK-022
- **Realizuje:** Prace w tle: `UTRZYMAJ_PARTYCJE`, `WYCZYSC_DANE_TECHNICZNE`
- **Opis:** `UTRZYMAJ_PARTYCJE` (partycje 3 miesiące naprzód, alarm braku dni wolnych na 12 miesięcy) i `WYCZYSC_DANE_TECHNICZNE` (`decyzja_modelu.stan := NULL` po 180 dniach, klucze idempotencji, `proba_logowania` po 30 dniach, zakończone prace po 7 dniach, `MARTWA` po 30, `zdarzenie_rodo` po 2 latach).
- **Definition of Done:**
  - [ ] Testy z zegarem testowym dla każdej reguły czyszczenia.
  - [ ] Cykle w schedulerze zgodne z 13.5.
- **Status:** TODO

### TASK-035 — Administracja pracami w tle

- **Faza:** F2
- **Zależności:** TASK-020, TASK-027
- **Realizuje:** API: `GET /admin/praca-w-tle`, `POST /admin/praca-w-tle/{id}/ponowienie`
- **Opis:** Lista prac (`?status=MARTWA&typ`) i ręczne ponowienie pracy `MARTWA` przez Administratora (audyt).
- **Definition of Done:**
  - [ ] Testy API (tylko A); ponowienie ustawia `OCZEKUJE`, `proby = 0` i wpis audytu.
- **Status:** TODO

## F3 — Warstwa modeli AI (porty i adaptery)

### TASK-036 — Porty modeli AI `lemat/modele/porty.py`

- **Faza:** F3
- **Zależności:** TASK-002
- **Realizuje:** Sekcje: 13.1 · ADR: ADR-026 · NFR: NFR-06
- **Opis:** Typy i protokoły z 13.1: `PytanieNoul/Score/Choice`, `WarunekPytania`, `PytanieWPlanie`, `PlanPytan` z jawnymi zależnościami (ADR-026), `DecyzjaSkladowa`, `RezultatPytan`, `ModelDecyzyjny`, `ModelGeneratywny`, `RozpoznawaniePisma`, `PrzyjeciePlanu`, `OgranicznikSzybkosci`, `SprawdzanieSymboliczne`, `PredyktorSukcesu`, `MagazynObiektow`, wyjątki `BladModeluPrzejsciowy`, `BladModeluTrwaly`, `ModelNiedostepny`.
- **Definition of Done:**
  - [ ] Plik `src/lemat/modele/porty.py` 1:1 z 13.1, mypy strict.
  - [ ] Walidacja `PlanPytan`: cykl zależności i `max − min > 10` w SCORE → błąd (testy).
- **Status:** TODO

### TASK-037 — Konfiguracja modeli, rejestr adapterów i aktywacja

- **Faza:** F3
- **Zależności:** TASK-016, TASK-027, TASK-029, TASK-036
- **Realizuje:** API: `GET /admin/konfiguracje-modeli`, `POST /admin/konfiguracje-modeli`, `POST /admin/konfiguracje-modeli/{id}/aktywacja` · Założenia: A-07 · Ryzyka: R-08
- **Opis:** `lemat.modele.rejestr` (dostawca → adapter), schemat `konfiguracja_modelu.parametry` (m.in. `retencja_dostawcy_dni`, `tryb_jev`), endpointy administracyjne. Aktywacja blokowana `409 BRAK_PROGU_DLA_KONFIGURACJI` i `409 BRAK_RETENCJI_DOSTAWCY` (A-07).
- **Definition of Done:**
  - [ ] Testy API: utworzenie nieaktywnej, aktywacja, oba kody 409, `ux_konfiguracja_aktywna` (jedna aktywna na rolę).
  - [ ] Zmiana dostawcy wymaga wyłącznie nowej konfiguracji (NFR-06) — test z dwoma atrapami.
- **Status:** TODO

### TASK-038 — Dziennik decyzji modelu `decyzja_modelu`

- **Faza:** F3
- **Zależności:** TASK-016, TASK-036
- **Realizuje:** NFR: NFR-05 · Zasady nadrzędne: Z-4
- **Opis:** Zapis każdej Decyzji składowej: pytanie, wartość (moda), pełny rozkład, konfiguracja, kontekst, `uczen_id`, `rozwiazanie_id`, stan (do 180 dni).
- **Definition of Done:**
  - [ ] Każde wywołanie `ModelDecyzyjny.wykonaj_plan` zapisuje wiersz na każde pytanie (test).
  - [ ] Rozkład sumuje się do 1 ± 1e-6 (walidacja).
- **Status:** TODO

### TASK-039 — Atrapy modeli i zaślepka HTTP Jev

- **Faza:** F3
- **Zależności:** TASK-036
- **Realizuje:** Założenia: A-02
- **Opis:** Deterministyczne atrapy wszystkich portów (konfigurowalne odpowiedzi, opóźnienia, błędy 429/5xx/timeout) dla środowiska `lokalne` i testów oraz zaślepka HTTP API Jev mierząca liczbę wywołań w oknach 1 s (wymagana w G-3 c i G-4). System musi działać bez Jev (A-02).
- **Definition of Done:**
  - [ ] `docker compose up` działa bez kluczy dostawców.
  - [ ] Zaślepka udostępnia metrykę maksimum wywołań w oknie 1 s.
- **Status:** TODO

### TASK-040 — `JevAdapter` — tryb wsadowy i pojedynczy

- **Faza:** F3
- **Zależności:** TASK-036, TASK-038, TASK-039
- **Realizuje:** Sekcje: 13.2 · ADR: ADR-026 · Macierz błędów: E-08 · Pliki testowe/danych: `lemat/modele/schematy/*.json` · NFR: NFR-06 · Założenia: A-01
- **Opis:** Wykonanie planu w ≤ 2 rundach; `WSADOWY` (≤ 32 pytania o jednym stanie na żądanie) i `POJEDYNCZY` (≤ 16 równolegle na Ocenę, 3 fale); timeout 2 s, ponowienie tylko przy ≥ 13 s do terminu miękkiego; mapowanie błędów (E-08) na wyjątki portu; schematy JSON odpowiedzi w `lemat/modele/schematy/`.
- **Definition of Done:**
  - [ ] Testy kontraktowe na zaślepce dla obu trybów i wszystkich typów pytań (noul/score/choice).
  - [ ] Pytania warunkowe planu niezadane, gdy warunek niespełniony (test).
- **Status:** TODO

### TASK-041 — Globalny ogranicznik szybkości modeli (`OgranicznikSzybkosci`)

- **Faza:** F3
- **Zależności:** TASK-016, TASK-040
- **Realizuje:** ADR: ADR-039 · Scenariusze wyścigów: C-40 · Macierz błędów: E-33 · Pliki testowe/danych: `tests/modele/test_ogranicznik.py`, `tests/obciazenie/symulacja_ogranicznika.py` · Ryzyka: R-17, R-21
- **Opis:** Lokalny token bucket + przydział z `limit_szybkosci_modelu`: `r_i = min(0,8 × limit × waga_i / Σ wag, 0,8 × limit − Σ_{j≠i} r_j)`, wagi worker 1,0 / `api` 0,1, łączna pojemność kubełków `B_całk` 34 albo 10, odnowienia pod `pg_advisory_xact_lock`, wiersze per pula (R 1,0 / S 0,3 dla `ROZPOZNAWANIE_PISMA`), `przyjmij_plan` przed pierwszą falą, pierwszeństwo fal ≥ 2, `zmniejsz_przydzial` po 429 (× 0,5 na 30 s) bez otwierania obwodu.
- **Definition of Done:**
  - [ ] `tests/modele/test_ogranicznik.py` (a)–(f) z 13.2 przechodzi, w tym granica chwilowa ≤ 74 wywołań w oknie 1 s przy 2/3/4 workerach.
  - [ ] `tests/obciazenie/symulacja_ogranicznika.py` odtwarza S-6 (0/90 do modelu zapasowego) i tabelę 17.4.
  - [ ] Test C-40: dołączenie 3. repliki w szczycie nie przekracza limitu klucza.
- **Status:** TODO

### TASK-042 — `LlmAdapter` per dostawca: generowanie i rozpoznawanie pisma

- **Faza:** F3
- **Zależności:** TASK-036, TASK-038
- **Realizuje:** ADR: ADR-004 · Macierz błędów: E-05 · Ryzyka: R-05
- **Opis:** Adaptery `ModelGeneratywny` (`generuj_json`, `generuj_tekst`) i `RozpoznawaniePisma` (`rozpoznaj`, `sprawdz_poprawki`) dla wybranego dostawcy multimodalnego; jedna próba naprawy odpowiedzi niezgodnej ze schematem (E-05), metryka `koszt_modeli_pln_total{rola,systemowe}`.
- **Definition of Done:**
  - [ ] Testy kontraktowe na atrapie i (środowisko `testowe`) na prawdziwym dostawcy.
  - [ ] SDK dostawcy importowane wyłącznie w `lemat.modele.adaptery.*` (kontrakt CI).
- **Status:** TODO

### TASK-043 — `ZapasowyLlmAdapter` — model zapasowy wykonujący cały plan

- **Faza:** F3
- **Zależności:** TASK-036, TASK-038, TASK-042
- **Realizuje:** ADR: ADR-009 · Macierz błędów: E-09 · Ryzyka: R-01
- **Opis:** Implementacja `ModelDecyzyjny` na LLM ze strukturyzowaną odpowiedzią: cały plan w jednym wywołaniu (rodziny pytań warunkowych, pytania zależne od wariantu Q-OC-02.v.Kk), timeout 9 s, ≤ 24 kryteria, rozkłady z odpowiedzi modelu; osobny kalibrator i próg (rola `DECYZYJNY_ZAPASOWY`).
- **Definition of Done:**
  - [ ] Test kontraktowy: ten sam `PlanPytan` daje `RezultatPytan` o identycznej strukturze co `JevAdapter`.
  - [ ] Błąd/timeout (E-09) → `ModelNiedostepny` lub błąd przejściowy zgodnie z 14.1.
- **Status:** TODO

### TASK-044 — `CircuitBreakerModeluDecyzyjnego` i przełączanie na model zapasowy

- **Faza:** F3
- **Zależności:** TASK-040, TASK-043
- **Realizuje:** Sekcje: 9.9 · NFR: NFR-07 · Scenariusze NFR-01: S-4, S-5
- **Opis:** Obwód per proces: otwarcie po 3 kolejnych błędach lub ≥ 50% błędów w 20 s przy ≥ 6 wywołaniach, półotwarty po 60 s; przy otwartym obwodzie plan idzie do modelu zapasowego; `429` od Jev nie otwiera obwodu (ADR-039).
- **Definition of Done:**
  - [ ] Testy stanów obwodu z zegarem testowym.
  - [ ] Scenariusz S-4 (praca ciągła na zapasowym) i S-5 (oba niedostępne → T-6/Weryfikacja) w testach integracyjnych.
- **Status:** TODO

## F4 — Konta i bezpieczeństwo

### TASK-045 — Logowanie, odświeżanie, wylogowanie: JWT + rotowany refresh token

- **Faza:** F4
- **Zależności:** TASK-009, TASK-023, TASK-027
- **Realizuje:** Sekcje: 16.1 · ADR: ADR-020 · API: `POST /auth/logowanie`, `POST /auth/odswiezenie`, `POST /auth/wylogowanie`
- **Opis:** JWT 15 min, refresh token 30 dni w cookie `HttpOnly; Secure; SameSite=Strict` z rotacją i wykrywaniem ponownego użycia, haszowanie haseł (Argon2id), limity nieudanych logowań (5/15 min na e-mail, 20/15 min na IP) w `proba_logowania`.
- **Definition of Done:**
  - [ ] Testy: rotacja, ponowne użycie starego refresh tokenu unieważnia rodzinę, limity → `429`.
  - [ ] Konto `ZABLOKOWANE`/`OCZEKUJE_NA_ZATWIERDZENIE` nie loguje się (kody błędów).
- **Status:** TODO

### TASK-046 — Rejestracja Ucznia i Nauczyciela

- **Faza:** F4
- **Zależności:** TASK-013, TASK-045
- **Realizuje:** Sekcje: 9.1 · API: `POST /auth/rejestracja` · FR: FR-01 · Założenia: A-06, A-14 · Decyzje PO: D-9
- **Opis:** `RejestracjaWejscie`: Uczeń (oświadczenie ukończenia 16 lat — A-06, opcjonalny kod zaproszenia; bez kodu tylko przy `LEMAT_REJESTRACJA_BEZ_KLASY = true`), Nauczyciel (konto `OCZEKUJE_NA_ZATWIERDZENIE`, `201` bez tokenu, D-9). Tworzy `uzytkownik`, `uczen`/`nauczyciel`, `profil_ucznia`, `sciezka_nauki`.
- **Definition of Done:**
  - [ ] Testy przepływu 9.1 (rejestracja z kodem, bez kodu przy fladze true/false, Nauczyciel bez tokenu).
  - [ ] E-mail `citext` unikalny; komunikaty po polsku.
- **Status:** TODO

### TASK-047 — Reset hasła

- **Faza:** F4
- **Zależności:** TASK-031, TASK-045
- **Realizuje:** API: `POST /auth/reset-hasla`, `POST /auth/reset-hasla/potwierdzenie`
- **Opis:** Zawsze `202` bez ujawniania istnienia konta, limit 3/h na e-mail (po `praca_w_tle` typu `WYSLIJ_EMAIL`), jednorazowy token z terminem, unieważnienie refresh tokenów po zmianie.
- **Definition of Done:**
  - [ ] Testy: nieistniejący e-mail → `202`; token jednorazowy; limit → `429`.
- **Status:** TODO

### TASK-048 — Dane Ucznia: poziom matury, termin egzaminu, wybrany Dział

- **Faza:** F4
- **Zależności:** TASK-046
- **Realizuje:** API: `GET /uczniowie/ja`, `PATCH /uczniowie/ja` · FR: FR-02, FR-09
- **Opis:** `GET/PATCH /uczniowie/ja` z `If-Match` (U): `poziom_matury` (skutki zmiany poziomu wg 9.1), `termin_egzaminu`, `wybrany_dzial_id` (FR-09). Pole `zgoda_na_trening` obsługuje TASK „rodo-kopie”.
- **Definition of Done:**
  - [ ] Testy: zmiana poziomu matury działa wg 9.1; `412` przy nieaktualnym ETag.
  - [ ] Dział tylko-rozszerzony niedostępny dla poziomu podstawowego (`422`).
- **Status:** TODO

### TASK-049 — Administracja kontami i flagami Nauczycieli

- **Faza:** F4
- **Zależności:** TASK-029, TASK-046
- **Realizuje:** ADR: ADR-034 · API: `GET /admin/uzytkownicy`, `PATCH /admin/uzytkownicy/{id}`
- **Opis:** Lista kont i `PATCH` (`status` — zatwierdzenie Nauczyciela, `weryfikator_globalny`, `recenzent_tresci`, `deklarowane_minuty_weryfikacji`); walidacja A-14: prowadzący Klasę może być weryfikatorem globalnym tylko gdy prognoza dnia roboczego + deklaracja ≤ 180 min (`422 LIMIT_OBCIAZENIA_WERYFIKATORA`), minimum 90 min dla weryfikatora (A-13).
- **Definition of Done:**
  - [ ] Testy API, audyt każdej zmiany, oba warunki 422.
  - [ ] Prognoza Klasy liczona funkcją z TASK „wer-obciazenie” (do tego czasu atrapa).
- **Status:** TODO

### TASK-050 — Ochrona aplikacji i kategorie danych osobowych

- **Faza:** F4
- **Zależności:** TASK-023
- **Realizuje:** Sekcje: 16, 16.2, 16.3 · Ryzyka: R-06
- **Opis:** Nagłówki bezpieczeństwa i CSP, walidacja wejścia (12.5), maskowanie pól w logach (bez treści zdjęć, Transkrypcji, haseł; retencja 30 dni), ochrona przed wstrzykiwaniem poleceń (R-06: zamknięte listy odpowiedzi, treść kartki tylko jako dane), brak danych identyfikacyjnych w promptach, inwentarz kategorii danych 16.3.
- **Definition of Done:**
  - [ ] Test: logi nie zawierają pól z listy maskowanych.
  - [ ] Testy CSP i nagłówków; checklist 16.2 odhaczona w PR.
- **Status:** TODO

## F5 — Treści: Zadania, Kryteria oceniania, Wkładki teoretyczne, Arkusze CKE

### TASK-051 — API danych referencyjnych: Działy i Umiejętności

- **Faza:** F5
- **Zależności:** TASK-012, TASK-027
- **Realizuje:** API: `GET /dzialy`, `GET /umiejetnosci`
- **Opis:** Listy referencyjne dla wszystkich zalogowanych (cache HTTP).
- **Definition of Done:**
  - [ ] Testy API; odpowiedź zgodna z DTO 12.4.
- **Status:** TODO

### TASK-052 — Zadania: wersjonowanie, Zadanie bazowe Nauczyciela, wyszukiwanie i odczyt

- **Faza:** F5
- **Zależności:** TASK-011, TASK-027, TASK-032
- **Realizuje:** Sekcje: 12.5, 7.5.1, 7.5.2, 7.5.3 · ADR: ADR-012 · API: `GET /zadania`, `POST /zadania`, `GET /zadania/{id}` · Scenariusze wyścigów: C-16, C-17 · FR: FR-12, FR-36
- **Opis:** Domena `Zadanie`/`ZadanieWersja`/`KryteriumOceniania` w `lemat.tresci`: nowa wersja przy każdej zmianie treści, Rozwiązanie wskazuje niezmienną `zadanie_wersja_id`; `POST /zadania` (Zadanie bazowe `BAZOWE_NAUCZYCIEL`, `PRYWATNY`, `OPUBLIKOWANE` po W-1, W-2, W-8), `GET /zadania` z filtrami i widocznością 12.8, `GET /zadania/{id}` (U bez Rozwiązania wzorcowego i klucza; N/A pełne + ETag); reguła wersji akceptowanej (12.5).
- **Definition of Done:**
  - [ ] Testy C-16/C-17: Ocena według wersji widzianej przez Ucznia; wersja starsza niż reguła → `409`.
  - [ ] Uczeń nigdy nie dostaje `rozwiazanie_wzorcowe`, `opcje_zamkniete.poprawna`, kryteriów (test).
  - [ ] Każde Zadanie ma rodzaj, Dział, Umiejętności (dokładnie jedna główna) i nominalny Poziom trudności (FR-36).
- **Status:** TODO

### TASK-053 — Import Arkusza CKE (`ArkuszImportV1`)

- **Faza:** F5
- **Zależności:** TASK-020, TASK-028, TASK-052
- **Realizuje:** Sekcje: 9.11, 12.9 · API: `POST /admin/arkusze-cke/importy`, `GET /admin/arkusze-cke/importy/{id}` · Prace w tle: `IMPORTUJ_ARKUSZ` · Scenariusze wyścigów: C-22 · Macierz błędów: E-18 · FR: FR-35 · Ryzyka: R-04
- **Opis:** Przyjęcie ZIP (`arkusz.json` + `rys/`) do bucketu `importy`, praca `IMPORTUJ_ARKUSZ`: walidacja schematu 12.9, tworzenie Zadań `BAZOWE_ARKUSZ_CKE` (`GLOBALNY`, `OPUBLIKOWANE`) z Kryteriami i załącznikami, raport błędów.
- **Definition of Done:**
  - [ ] Test na przykładowym pakiecie (fikstura) → Zadania z kryteriami; błędny pakiet → `ODRZUCONY` z raportem (E-18).
  - [ ] C-22: ponowny import tego samego `arkusz_cke.kod` → `ODRZUCONY`.
- **Status:** TODO

### TASK-054 — Przegląd i wycofanie Zadań, zasięg i uprawnienia

- **Faza:** F5
- **Zależności:** TASK-021, TASK-030, TASK-052
- **Realizuje:** Sekcje: 8.4 · ADR: ADR-015, ADR-028 · API: `POST /zadania/{id}/przeglad`, `POST /zadania/{id}/wycofanie` · Zdarzenia domenowe: `ZadanieOpublikowane`, `ZadanieDoPrzegladu` · FR: FR-14 · Decyzje PO: D-11 · PZS-1 (część B): B-3
- **Opis:** Maszyna stanów Zadania 8.4 i tabela uprawnień przejść (ADR-028): zlecający Nauczyciel → `PRYWATNY`, recenzent treści / Administrator → `GLOBALNY`, promocja `PRYWATNY → GLOBALNY`, widoczność w kolejce recenzentów po 14 dniach, Zadania z dowodem zawsze przez Przegląd (ADR-015). Zdarzenia `ZadanieOpublikowane`, `ZadanieDoPrzegladu`.
- **Definition of Done:**
  - [ ] Testy każdej komórki tabeli uprawnień 8.4.
  - [ ] `POPRAWIONE` tworzy nową wersję (ADR-012); `If-Match` Zadania.
- **Status:** TODO

### TASK-055 — Zgłoszenia błędu w Zadaniu i Wstrzymanie w doborze

- **Faza:** F5
- **Zależności:** TASK-026, TASK-054
- **Realizuje:** ADR: ADR-038 · API: `POST /zadania/{id}/zgloszenia-bledu`, `GET /zgloszenia-bledu-zadan`, `POST /zgloszenia-bledu-zadan/{id}/rozpatrzenie` · Zdarzenia domenowe: `ZgloszenieBleduZadaniaUtworzone` · Scenariusze wyścigów: C-36 · Ryzyka: R-18
- **Opis:** Zgłoszenie tylko dla `GLOBALNY` + `OPUBLIKOWANE`, 1 otwarte na (Zadanie, Nauczyciel), limit 10/dobę, przy 3 otwartych od różnych Nauczycieli `INSERT INTO wstrzymanie_w_doborze … ON CONFLICT DO NOTHING` (bez zmiany `zadanie.version`), rozpatrzenie przez NR/A (`UWZGLEDNIONE` wymaga przeglądu lub wycofania w tej samej transakcji), SLA 5 dni roboczych z metrykami `zgloszenia_zadan_otwarte`, `zgloszenia_zadan_po_sla`.
- **Definition of Done:**
  - [ ] Test C-36: trzy równoległe zgłoszenia → dokładnie jedno wstrzymanie, `zadanie.version` bez zmian.
  - [ ] Testy API z kodem `409 ZGLOSZENIE_ZADANIA_ISTNIEJE` i limitem.
- **Status:** TODO

### TASK-056 — Wkładki teoretyczne: odczyt, edycja i publikacja

- **Faza:** F5
- **Zależności:** TASK-011, TASK-021, TASK-027, TASK-030
- **Realizuje:** API: `GET /wkladki-teoretyczne`, `GET /wkladki-teoretyczne/{id}`, `PUT /wkladki-teoretyczne/{id}` · Zdarzenia domenowe: `WkladkaDoPrzegladu` · FR: FR-06
- **Opis:** Statusy `SZKIC → DO_PRZEGLADU → OPUBLIKOWANA → WYCOFANA`, jedna opublikowana na Umiejętność (`ux_wkladka_opublikowana`), Uczeń widzi tylko `OPUBLIKOWANA`, edycja NR/A z `If-Match`, zdarzenie `WkladkaDoPrzegladu`. Brak generowania „na żądanie” (1.3).
- **Definition of Done:**
  - [ ] Testy API i uprawnień; publikacja drugiej Wkładki tej Umiejętności wycofuje poprzednią w jednej transakcji.
- **Status:** TODO

### TASK-057 — Pula Zadań Testu diagnostycznego

- **Faza:** F5
- **Zależności:** TASK-022, TASK-052
- **Realizuje:** API: `PUT /admin/zadania/{id}/diagnostyczne`, `GET /admin/pula-diagnostyczna` · Prace w tle: `SPRAWDZ_PULE_DIAGNOSTYCZNA` · Macierz błędów: E-34
- **Opis:** Oznaczanie `diagnostyczne` (tylko `ZAMKNIETE`, `GLOBALNY`, `OPUBLIKOWANE`; `409 PULA_DIAGNOSTYCZNA_ZA_MALA` bez `?wymus=true`), raport puli per (Dział, poziom matury, nominalny Poziom trudności ∈ {2, 4}), praca dzienna `SPRAWDZ_PULE_DIAGNOSTYCZNA` z alertem `ALERT_PULA_DIAGNOSTYCZNA`.
- **Definition of Done:**
  - [ ] Testy API i pracy; metryka `pula_diagnostyczna_braki`.
- **Status:** TODO

### TASK-058 — Kalibracja Poziomu trudności (atomowa, poza wierszem Zadania)

- **Faza:** F5
- **Zależności:** TASK-011, TASK-029
- **Realizuje:** ADR: ADR-029 · Scenariusze wyścigów: C-24 · PZS-1 (część B): B-4
- **Opis:** Atomowy `UPDATE kalibracja_trudnosci SET trudnosc_b = trudnosc_b − …` z `FOR UPDATE` w CTE (10.1.3), `K_z = max(0.05, 0.4/(1+m/30))`, przeliczanie poziomu od 30 wyników z histerezą 0,1, audyt `POZIOM_TRUDNOSCI_SKALIBROWANY`; `zadanie.version` niezmieniane.
- **Definition of Done:**
  - [ ] Test C-24: 50 równoległych Wyników → brak utraconych aktualizacji.
  - [ ] Nauczyciel edytujący Zadanie nie dostaje `412` z powodu kalibracji (test).
- **Status:** TODO

## F6 — Klasy, Zestawy zadań, Przypisania

### TASK-059 — Klasy, kody zaproszeń, członkostwo i archiwizacja

- **Faza:** F6
- **Zależności:** TASK-013, TASK-027, TASK-046
- **Realizuje:** API: `POST /klasy`, `GET /klasy`, `GET /klasy/{id}`, `POST /klasy/{id}/kod-zaproszenia`, `POST /klasy/{id}/archiwizacja`, `POST /klasy/dolaczenie`, `DELETE /klasy/{id}/uczniowie/{uczenId}`, `DELETE /uczniowie/ja/klasy/{klasaId}` · FR: FR-30
- **Opis:** `lemat.klasy`: tworzenie Klasy z kodem zaproszenia (8 znaków, 14 dni), nowy kod unieważnia stary, archiwizacja (kod unieważniony, Klasa poza przydziałem 11.4.1 p. 2), dołączenie Ucznia kodem, usunięcie/opuszczenie (otwarte Weryfikacje zostają u Nauczyciela docelowego).
- **Definition of Done:**
  - [ ] Testy API wszystkich 8 endpointów z uprawnieniami 12.8.
  - [ ] Kod wygasły lub Klasa zarchiwizowana → `409`.
- **Status:** TODO

### TASK-060 — Zestawy zadań (CRUD)

- **Faza:** F6
- **Zależności:** TASK-052, TASK-059
- **Realizuje:** API: `POST /zestawy-zadan`, `GET /zestawy-zadan`, `GET /zestawy-zadan/{id}`, `PUT /zestawy-zadan/{id}`, `DELETE /zestawy-zadan/{id}`
- **Opis:** 1–30 Zadań widocznych dla Nauczyciela; zmiana nie wpływa wstecz na istniejące Przypisania (pozycje czytane w chwili rekomendacji, usunięte znikają z otwartych Przypisań), `409 ZESTAW_UZYWANY` przy usuwaniu.
- **Definition of Done:**
  - [ ] Testy API z `If-Match` (S).
- **Status:** TODO

### TASK-061 — Przypisania i ich postęp

- **Faza:** F6
- **Zależności:** TASK-021, TASK-025, TASK-030, TASK-060
- **Realizuje:** ADR: ADR-032 · API: `POST /przypisania`, `GET /przypisania`, `GET /przypisania/{id}/postep`, `GET /uczniowie/ja/przypisania` · Zdarzenia domenowe: `PrzypisanieUtworzone` · FR: FR-32
- **Opis:** Przypisanie Zestawu Klasie lub Uczniom z terminem (Idempotency-Key), powiadomienia `NOWE_PRZYPISANIE`, lista otwartych Przypisań Ucznia z pozycjami, postęp (`PostepPrzypisaniaWyjscie`), ujawnianie klucza Zadań zamkniętych po terminie (ADR-032).
- **Definition of Done:**
  - [ ] Testy API; Uczeń widzi tylko własne Przypisania; po terminie `rezultat_ujawniony_od` ustawione.
- **Status:** TODO

## F7 — Profil ucznia, Test diagnostyczny, Agent rekomendujący

### TASK-062 — Wynik zadania: rejestracja, wagi i unieważnienia

- **Faza:** F7
- **Zależności:** TASK-017
- **Realizuje:** Sekcje: 8.6 · ADR: ADR-018
- **Opis:** Maszyna stanów 8.6, rejestracja Wyniku z Odpowiedzi i Ocen (`w_status` 1,0 / 0,5 dla Oceny wstępnej / brak przy wstrzymaniu; `w_proba` 1,0 / 0,5), unieważnianie przy zastąpieniu Oceny i przy Zgłoszeniu błędu odczytu (`powod_uniewaznienia`).
- **Definition of Done:**
  - [ ] Testy każdego przejścia 8.6; brak Wyniku przy `mozliwy_blad_odczytu` (ADR-018).
- **Status:** TODO

### TASK-063 — Profil ucznia: model Elo per Umiejętność i praca `AKTUALIZUJ_PROFIL`

- **Faza:** F7
- **Zależności:** TASK-020, TASK-022, TASK-058, TASK-062
- **Realizuje:** Sekcje: 10.1.3 · ADR: ADR-010 · Prace w tle: `AKTUALIZUJ_PROFIL`, `ZAMIATAJ_PROFILE` · Scenariusze wyścigów: C-11, C-13, C-32 · FR: FR-29, FR-07, FR-08
- **Opis:** Aktualizacja `theta` i `poziom_opanowania` (P sukcesu na Poziomie trudności 4), propagacja w Dziale, interwały powtórek 3/7/14/30/60 dni, odtwarzanie Profilu; `AKTUALIZUJ_PROFIL` w transakcji z `SELECT … FROM profil_ucznia FOR UPDATE` i pętlą „do pustego wyniku”, deduplikacja tylko wśród `OCZEKUJE`; `ZAMIATAJ_PROFILE` co 5 min z metryką `wynik_najstarszy_niezastosowany_sekundy`.
- **Definition of Done:**
  - [ ] Testy C-11 i C-32 (Wynik zatwierdzony w trakcie `W_TOKU` zawsze trafia do Profilu).
  - [ ] Test odtworzenia Profilu = wynik przyrostowy (brak podwójnego liczenia, C1-11).
  - [ ] Alert, gdy najstarszy niezastosowany Wynik > 300 s.
- **Status:** TODO

### TASK-064 — Postępy Klasy i Uczniów dla Nauczyciela

- **Faza:** F7
- **Zależności:** TASK-059, TASK-063
- **Realizuje:** API: `GET /klasy/{id}/postepy`, `GET /klasy/{id}/uczniowie/{uczenId}/rozwiazania` · FR: FR-31
- **Opis:** Widok agregujący Profile ucznia Klasy i lista Rozwiązań Ucznia (paginacja kursorem).
- **Definition of Done:**
  - [ ] Testy API i wydajności (Klasa 35 Uczniów < 300 ms p95 lokalnie).
- **Status:** TODO

### TASK-065 — Widok Profilu ucznia

- **Faza:** F7
- **Zależności:** TASK-027, TASK-063
- **Realizuje:** API: `GET /uczniowie/{uczenId}/profil` · FR: FR-04
- **Opis:** `ProfilUczniaWyjscie` z poziomem opanowania Działów i Umiejętności; U (`ja`) i N (Uczeń swojej Klasy).
- **Definition of Done:**
  - [ ] Testy API i uprawnień.
- **Status:** TODO

### TASK-066 — Test diagnostyczny: utworzenie, Odpowiedzi, zakończenie, pominięcie

- **Faza:** F7
- **Zależności:** TASK-021, TASK-057, TASK-063
- **Realizuje:** API: `POST /uczniowie/ja/test-diagnostyczny`, `GET /testy-diagnostyczne/{id}`, `POST /testy-diagnostyczne/{id}/pozycje/{pozycjaId}/odpowiedz`, `POST /testy-diagnostyczne/{id}/zakonczenie`, `POST /testy-diagnostyczne/{id}/pominiecie` · Zdarzenia domenowe: `TestDiagnostycznyZakonczony` · FR: FR-03 · Decyzje PO: D-14 · Ryzyka: R-09
- **Opis:** Przepływ 9.1: dobór pozycji z puli (Dział × Poziom trudności 2/4), `409 TEST_JUZ_WYKONANY`, `409 TEST_NIEDOSTEPNY` (< 50% pozycji), bez ujawniania poprawności do końca, zakończenie → `AKTUALIZUJ_PROFIL`, pominięcie (opcja domyślna D-14: Profil `DOMYSLNA`, test możliwy później).
- **Definition of Done:**
  - [ ] Testy API i przepływu 9.1 dla obu poziomów matury.
  - [ ] Odpowiedzi z pominiętego testu nie tworzą Wyników zadania.
- **Status:** TODO

### TASK-067 — Agent rekomendujący — polityka `POLITYKA_ELO`

- **Faza:** F7
- **Zależności:** TASK-052, TASK-056, TASK-061, TASK-063
- **Realizuje:** Sekcje: 10, 10.1, 10.1.1, 10.1.2, 10.1.4, 10.1.5, 10.1.3 · ADR: ADR-011 · Agenci: Agent rekomendujący · Macierz błędów: E-19, E-20, E-26 · FR: FR-05, FR-06, FR-07, FR-08, FR-10 · Reguły rekomendacji: `R-PRZYPISANIE`, `R-SERIA`, `R-POWTORKA`, `R-WKLADKA`, `R-WKLADKA-DIAG`, `R-NAJSLABSZA`, `R-TRUDNOSC_W_GORE`, `R-UTRWALENIE`
- **Opis:** `lemat.agenci.rekomendujacy`: kontrakt 10.1.1 (obiekty Pydantic, bez FastAPI/SQLAlchemy), kroki 1–8 polityki 10.1.2 (Przypisania, Seria, powtórki, Wkładka przy opanowaniu < 0,40, strefa rozwoju P ∈ [0,60; 0,75] z celem 0,675, udział zadań otwartych co 3./co 5. element, zapotrzebowanie puli), migawka cech 10.1.4, szablony uzasadnień 10.1.5 dla każdej reguły i braku elementu (FR-10).
- **Definition of Done:**
  - [ ] Testy jednostkowe każdej reguły `R-*` i przypadków E-19, E-20, E-26.
  - [ ] Każda Rekomendacja ma `regula`, `uzasadnienie` i `uzasadnienie_fakty` (test własności na losowych Profilach).
- **Status:** TODO

### TASK-068 — Seria nauki po Wkładce teoretycznej

- **Faza:** F7
- **Zależności:** TASK-067
- **Realizuje:** ADR: ADR-030 · Scenariusze wyścigów: C-27
- **Opis:** Seria: elementy jednej Umiejętności o niemalejącym Poziomie trudności, maks. 8 elementów / 3 porażki / 7 dni, koniec po sukcesie (ułamek ≥ 0,7) na Poziomie trudności ≥ 4; `UPDATE sciezka_nauki … WHERE version = :v` w transakcji INSERT elementu.
- **Definition of Done:**
  - [ ] Test C-27: dwa „Dalej” → jeden element, jeden przyrost `seria_liczba_elementow`.
- **Status:** TODO

### TASK-069 — Fasada synchroniczna rekomendacji i Ścieżka nauki w API

- **Faza:** F7
- **Zależności:** TASK-023, TASK-067, TASK-068
- **Realizuje:** Sekcje: 9.2 · ADR: ADR-023 · API: `GET /uczniowie/ja/rekomendacje/nastepna`, `GET /uczniowie/ja/sciezka-nauki` · Scenariusze wyścigów: C-12
- **Opis:** `Orkiestrator.rekomenduj_synchronicznie(uczen_id)` w procesie `api` (p95 ≤ 500 ms), zapis elementu `PROPONOWANY` z `ux_jeden_proponowany`, zlecenie `UZUPELNIJ_PULE` w tej samej transakcji, strona Ścieżki nauki kursorem.
- **Definition of Done:**
  - [ ] Test C-12: dwa równoległe żądania zwracają ten sam element.
  - [ ] Test wydajnościowy p95 ≤ 500 ms przy 1 000 Uczniów danych syntetycznych.
- **Status:** TODO

### TASK-070 — Elementy Ścieżki nauki: rozpoczęcie, pominięcie, przeczytanie, wygasanie

- **Faza:** F7
- **Zależności:** TASK-022, TASK-069
- **Realizuje:** Sekcje: 8.5 · API: `POST /elementy-sciezki/{id}/rozpoczecie`, `POST /elementy-sciezki/{id}/pominiecie`, `POST /elementy-sciezki/{id}/przeczytanie` · Prace w tle: `WYGAS_ELEMENTY`
- **Opis:** Maszyna stanów 8.5 (`PROPONOWANY` 24 h, `ROZPOCZETY`: Zadanie 7 dni, Wkładka 24 h), rozpoczęcie idempotentne ustala `zadanie_wersja_id`, przeczytanie tylko dla Wkładki, praca `WYGAS_ELEMENTY` co 1 min (konto `AKTYWNE`; Seria nieaktywna 7 dni kończona).
- **Definition of Done:**
  - [ ] Testy przejść 8.5 z zegarem testowym.
  - [ ] Element z aktywnym Rozwiązaniem nieterminalnym nie wygasa (C1-20).
- **Status:** TODO

### TASK-071 — Strategia eksperymentalna `JEV_CHOICE` (grupa eksperymentu)

- **Faza:** F7
- **Zależności:** TASK-044, TASK-069
- **Realizuje:** Sekcje: 10.1.6 · Macierz błędów: E-27 · Pytania do modelu decyzyjnego: Q-RE-01 · Reguły rekomendacji: `R-JEV-WKLADKA`, `R-JEV-ZADANIE_LATWE`, `R-JEV-ZADANIE_MATURALNE`, `R-JEV-POWTORKA`
- **Opis:** Przydział do grupy eksperymentu skrótem `fnv1a(uczen_id)` i `procent_grupy_eksperymentu` (domyślnie 0), pytanie Q-RE-01 z timeoutem 1,5 s bez ponowień i bez modelu zapasowego; błąd lub otwarty obwód → `POLITYKA_ELO` (E-27).
- **Definition of Done:**
  - [ ] Testy: timeout → wynik `POLITYKA_ELO` w ≤ 1,6 s; przydział do grupy stabilny dla Ucznia.
- **Status:** TODO

### TASK-072 — Port `PredyktorSukcesu`: Elo domyślnie, model drzewiasty jako eksperyment

- **Faza:** F7
- **Zależności:** TASK-067
- **Realizuje:** Sekcje: 10.1.7
- **Opis:** Implementacja `PredyktorSukcesu` na Elo (domyślna) oraz offline’owy eksperyment z modelem drzewiastym (gradient boosting) na migawkach `rekomendacja.cechy`, z raportem porównawczym; brak wpływu na produkcję bez decyzji.
- **Definition of Done:**
  - [ ] Notebook/skrypt porównania (log-loss, kalibracja) na danych syntetycznych; przełączenie predyktora konfiguracją.
- **Status:** TODO

## F8 — Rozwiązania, Odpowiedzi, Agent rozpoznający, Transkrypcja

### TASK-073 — Zadania zamknięte: Odpowiedzi Ucznia

- **Faza:** F8
- **Zależności:** TASK-021, TASK-025, TASK-052, TASK-062
- **Realizuje:** Sekcje: 9.3 · ADR: ADR-032 · API: `POST /zadania/{id}/odpowiedzi` · Zdarzenia domenowe: `OdpowiedzZapisana` · Scenariusze wyścigów: C-30 · FR: FR-15, FR-29
- **Opis:** `OdpowiedzWejscie` + Idempotency-Key: jeden kontekst na próbę (element, Przypisanie albo Test — `trg_kontekst_proby`), ocena kluczem (PRAWDA_FALSZ: punkt tylko za komplet), Wynik zadania, `OdpowiedzZapisana` → `AKTUALIZUJ_PROFIL`, wielokrotne próby z `w_proba` (ADR-032).
- **Definition of Done:**
  - [ ] Test C-30: ta sama pozycja Przypisania z elementu i z listy → jedna Odpowiedź (`409`).
  - [ ] Klucz niewidoczny przed terminem Przypisania (test).
- **Status:** TODO

### TASK-074 — Przesłanie Rozwiązania i jego odczyt

- **Faza:** F8
- **Zależności:** TASK-015, TASK-021, TASK-025, TASK-026, TASK-028, TASK-033
- **Realizuje:** Sekcje: 8.1, 9.4, 12.5 · ADR: ADR-024 · API: `POST /rozwiazania`, `GET /rozwiazania`, `GET /rozwiazania/{id}` · Zdarzenia domenowe: `RozwiazaniePrzeslane` · Scenariusze wyścigów: C-02 · Macierz błędów: E-01 · FR: FR-16 · NFR: NFR-08
- **Opis:** Multipart `RozwiazanieWejscie` (1–5 zdjęć, ≤ 10 MB, JPEG/PNG/HEIC/WEBP, limit 30/h), normalizacja w piaskownicy `api`, zapis obiektów i wierszy `zdjecie_rozwiazania` (`usun_po = created_at + 90 dni`), `RozwiazaniePrzeslane` → `ROZPOZNAJ_PISMO`; `GET` własnych Rozwiązań z odpytywaniem co 1 s w stanach przejściowych. Brak wpisu ręcznego (ADR-024).
- **Definition of Done:**
  - [ ] Testy E-01 (413/415/`422 ZA_DUZO_ZDJEC`) i C-02 (`409` z `id` istniejącego).
  - [ ] Tabela dozwolonych operacji 8.1 zaimplementowana jako jedna definicja (test każdej pary operacja × stan).
- **Status:** TODO

### TASK-075 — Nowe przesłanie, ponowienie rozpoznania, porzucenie

- **Faza:** F8
- **Zależności:** TASK-022, TASK-074
- **Realizuje:** API: `POST /rozwiazania/{id}/zdjecia`, `POST /rozwiazania/{id}/ponowienie-rozpoznania`, `POST /rozwiazania/{id}/porzucenie` · Prace w tle: `PORZUC_NIEZATWIERDZONE`
- **Opis:** Nowe przesłanie (`liczba_przeslan < 3`, `409 LIMIT_PRZESLAN`), ponowienie tych samych zdjęć przy `BLAD_DOSTAWCY`/`PRZEKROCZONY_CZAS` (`ponowienie_nr + 1`, ≤ 5, `409 LIMIT_PONOWIEN_ROZPOZNANIA`), porzucenie (element aktywny do `wygasa_at`), praca `PORZUC_NIEZATWIERDZONE` po 7 dniach (`usun_po := now()`).
- **Definition of Done:**
  - [ ] Testy API z `If-Match` (R) i limitami.
  - [ ] Różnica `liczba_przeslan` vs `proby` (C1-28) przetestowana.
- **Status:** TODO

### TASK-076 — Dostęp do zdjęć Rozwiązania (podpisane URL, wersja maskowana)

- **Faza:** F8
- **Zależności:** TASK-027, TASK-074
- **Realizuje:** API: `GET /rozwiazania/{id}/zdjecia/{zdjecieId}`
- **Opis:** `302` na podpisany URL ważny 10 min, `?wersja_zdjecia=ORYGINAL|MASKOWANE`, `410` po usunięciu, obcy weryfikator globalny wyłącznie wersja maskowana.
- **Definition of Done:**
  - [ ] Testy uprawnień 12.8 (oryginał vs maskowane) i `410`.
- **Status:** TODO

### TASK-077 — Agent rozpoznający i praca `ROZPOZNAJ_PISMO`

- **Faza:** F8
- **Zależności:** TASK-020, TASK-041, TASK-042, TASK-074
- **Realizuje:** Sekcje: 10.3, 10.3.1, 10.3.2 · Agenci: Agent rozpoznający · Prace w tle: `ROZPOZNAJ_PISMO` · Zdarzenia domenowe: `TranskrypcjaGotowa` · Macierz błędów: E-03, E-04, E-06, E-07 · FR: FR-17 · NFR: NFR-01 · Założenia: A-08 · Scenariusze NFR-01: S-7
- **Opis:** `lemat.agenci.rozpoznajacy`: kontrakt 10.3.1 (zdjęcia + treść Zadania → kroki ≤ 40 w tekście i LaTeX, `pewnosc_odczytu`, `czytelne`, `zgodnosc_z_zadaniem`, `obszar` kroku 10.3.2), timeout 14 s, próby 1 s/3 s, `BLAD_ROZPOZNAWANIA` (`NIECZYTELNE`, `ZBYT_WIELE_KROKOW`, `PRZEKROCZONY_CZAS`, `BLAD_DOSTAWCY`, inne Zadanie), zapis Transkrypcji po Filtrze danych osobowych, `TranskrypcjaGotowa`.
- **Definition of Done:**
  - [ ] Testy E-03, E-04, E-06, E-07 na atrapie; metryka `rozpoznawanie_czas_sekundy`.
  - [ ] Scenariusz S-7 (1. próba timeout, 2. udana) mieści się w budżecie 17.2 (test z zegarem).
- **Status:** TODO

### TASK-078 — Filtr danych osobowych i Słownik terminów matematycznych

- **Faza:** F8
- **Zależności:** TASK-077
- **Realizuje:** ADR: ADR-042 · Macierz błędów: E-37 · Pliki testowe/danych: `lemat/rozpoznawanie/slownik_matematyczny.txt` · NFR: NFR-09 · Założenia: A-09 · Ryzyka: R-19
- **Opis:** `FiltrDanychOsobowych` na polu `tekst` odczytu i Poprawek: reguła (a) imię, nazwisko, inicjały i oznaczenie klasy z konta → `[DANE_OSOBOWE]`; rdzeń ≥ 4 znaki: dokładnie lub Levenshtein ≤ 1, < 4 znaki: dokładnie; Słownik terminów matematycznych (≈ 4 000 form) niezastępowany poza regułami b1 (sąsiedztwo drugiego członu/inicjału) i b2 (krok z podejrzeniem danych osobowych); `konflikt_filtra` (E-37); LaTeX nigdy nie zmieniany.
- **Definition of Done:**
  - [ ] Testy z przypadkami „Jan Pole”, „Ewa”, „Lis”, „Suma” z 10.3/C4-03; metryki `konflikt_filtra_total`, `uczniowie_z_konfliktem_filtra_udzial`.
  - [ ] Plik słownika w repo z testem formatu (bez duplikatów, małe litery).
- **Status:** TODO

### TASK-079 — Zdjęcia maskowane (piaskownica Pillow w workerze)

- **Faza:** F8
- **Zależności:** TASK-028, TASK-077
- **Realizuje:** ADR: ADR-031
- **Opis:** Tworzenie kopii z zamalowanymi obszarami danych osobowych (obszary z rozpoznania) w piaskownicy procesowej workera, zapis `klucz_obiektu_maskowany`; pokazywana weryfikatorowi globalnemu spoza Klasy.
- **Definition of Done:**
  - [ ] Test: maskowane obszary pokrywają ramki kroków z danymi osobowymi; brak oryginału w odpowiedziach dla obcego weryfikatora.
- **Status:** TODO

### TASK-080 — Poprawki Transkrypcji i zatwierdzenie

- **Faza:** F8
- **Zależności:** TASK-021, TASK-078
- **Realizuje:** Sekcje: 12.5, 9.4, 8.1 · ADR: ADR-017, ADR-036 · API: `PUT /rozwiazania/{id}/transkrypcja`, `POST /rozwiazania/{id}/zatwierdzenie` · Zdarzenia domenowe: `TranskrypcjaPoprawiona`, `TranskrypcjaZatwierdzona` · Scenariusze wyścigów: C-03, C-04 · Macierz błędów: E-31 · FR: FR-18 · Decyzje PO: D-10 · PZS-1 (część B): B-1
- **Opis:** `PUT …/transkrypcja` w `TRANSKRYPCJA_GOTOWA`: nowa wersja `POPRAWKA_UCZNIA`, limity twarde (bez usuwania, ≤ 2 wstawione kroki z limitem długości, zmienione + wstawione ≤ max(3, ⌈0,5 n⌉), odsetek znaków ≤ 0,50 → `422 POPRAWKI_ZBYT_ROZLEGLE`), `zakres_poprawek`, Filtr danych osobowych, `TranskrypcjaPoprawiona` → `SPRAWDZ_POPRAWKI_TRANSKRYPCJI`; obowiązkowe zatwierdzenie (ADR-017) → `TranskrypcjaZatwierdzona` → `OCEN_ROZWIAZANIE`.
- **Definition of Done:**
  - [ ] Testy C-03, C-04 (`412`/`409`) i każdego limitu twardego (E-31).
  - [ ] Metryki `odsetek_rozwiazan_z_poprawkami_30d`, `czas_od_put_do_zatwierdzenia_sekundy`.
- **Status:** TODO

### TASK-081 — Sprawdzenie zgodności poprawek ze zdjęciem

- **Faza:** F8
- **Zależności:** TASK-041, TASK-042, TASK-080
- **Realizuje:** Sekcje: 10.3.3 · Prace w tle: `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` · Scenariusze wyścigów: C-35, C-38 · Macierz błędów: E-32 · Ryzyka: R-14
- **Opis:** Praca w puli S: ≤ 6 wycinków zmienionych kroków + zdjęcie poglądowe 1024 px do roli `ROZPOZNAWANIE_PISMA`, timeout 8 s, 2 próby, rezultat `zgodnosc_poprawek` zapisywany `WHERE id = :transkrypcja_id`, deduplikacja po `rozwiazanie_id` z `DO UPDATE` ładunku; działa tylko w kierunku bezpiecznym (Z-1).
- **Definition of Done:**
  - [ ] Testy C-35 i C-38 (rezultat dla starej wersji nie nadpisuje nowej).
  - [ ] Metryki `sprawdzenie_poprawek_czas_sekundy`, `sprawdzenie_poprawek_brak_total`, `poprawki_niezgodne_total`.
- **Status:** TODO

## F9 — Agent oceniający, Agent wyjaśniający, trasowanie do Weryfikacji

### TASK-082 — Agent oceniający: kontrakt, stan i Plan pytań

- **Faza:** F9
- **Zależności:** TASK-036, TASK-052
- **Realizuje:** Sekcje: 10.4, 10.4.1, 10.4.2, 10.4.3, 10.4.4 · ADR: ADR-005 · Agenci: Agent oceniający · Założenia: A-05 · Pytania do modelu decyzyjnego: Q-OC-00, Q-OC-01, Q-OC-02, Q-OC-03, Q-OC-04, Q-OC-05, Q-OC-06
- **Opis:** `lemat.agenci.oceniajacy`: budowa stanu tekstowego ≤ 32 000 znaków (treść, Rozwiązanie wzorcowe, Kryteria, Transkrypcja, wynik końcowy), katalog pytań Q-OC-00 … Q-OC-06 z warunkami i rundami, Plan pytań z zależnościami (wariant → kryteria wariantu, pierwszy błąd → typ błędu, pary wynikania ≤ 12).
- **Definition of Done:**
  - [ ] Testy złotych planów dla zadań 1- i 2-wariantowych, obliczeniowych i z dowodem (bez Q-OC-01).
  - [ ] Liczba wywołań planu budżetowego = 28 w trybie pojedynczym (16 + 11 + 1), maksimum 29 (test).
- **Status:** TODO

### TASK-083 — Reguły punktów, błędów i niespójności

- **Faza:** F9
- **Zależności:** TASK-032, TASK-082
- **Realizuje:** Sekcje: 11, 11.1, 11.2.1 · FR: FR-19, FR-20, FR-21, FR-27, FR-28 · Niespójności: N-1, N-2, N-3, N-4, N-5, N-6
- **Opis:** Reguły 11.1 (punkty surowe, FR-20: punkty za wynik tylko z rozumowania, `wymaga_nr_kryterium`, pierwszy błędny krok i typ błędu, `BLAD_ODCZYTU` nie jest typem błędu Ucznia, `mozliwy_blad_odczytu` przy τ_odczyt 0,20 i `pewnosc_odczytu < 0,80`), sprawdzenie wynikania SymPy + Q-OC-05, niespójności N-1 … N-6 (11.2.1).
- **Definition of Done:**
  - [ ] Testy jednostkowe każdej reguły 11.1 i każdego kodu N-x na tablicach decyzji.
  - [ ] Błąd odczytu nigdy nie obniża punktów (test własności, FR-28).
- **Status:** TODO

### TASK-084 — Pewność oceny: pewność surowa i zastosowanie Kalibratora

- **Faza:** F9
- **Zależności:** TASK-016, TASK-083
- **Realizuje:** Sekcje: 11.2 · ADR: ADR-006 · Scenariusze wyścigów: C-14, C-15 · FR: FR-22
- **Opis:** Pewność surowa = minimum prawdopodobieństw decyzji składowych (≤ 0,50 przy niespójności), przekształcenie aktywnym Kalibratorem pewności pary (konfiguracja, rodzaj); migawka progu, kalibratora i konfiguracji wczytana na starcie Oceny i zapisana w `ocena`.
- **Definition of Done:**
  - [ ] Testy C-14/C-15: zmiana progu/konfiguracji w trakcie Oceny nie zmienia decyzji; `prog_pewnosci_id`, `kalibrator_pewnosci_id`, `konfiguracja_modelu_id` zapisane.
- **Status:** TODO

### TASK-085 — Trasowanie do Weryfikacji (T-1 … T-7) i Ocena wstępna

- **Faza:** F9
- **Zależności:** TASK-017, TASK-084
- **Realizuje:** Sekcje: 11.3, 11.4.1 · ADR: ADR-008, ADR-022 · Zdarzenia domenowe: `OcenaWystawiona` · FR: FR-23, FR-24, FR-25, FR-26, FR-33 · NFR: NFR-03 · Zasady nadrzędne: Z-1, Z-3 · Reguły trasowania: T-1, T-2, T-3, T-4, T-5, T-6, T-7 · PZS-1 (część B): B-2
- **Opis:** Reguły T-1 … T-7 (dowód zawsze, FR-25 z τ_FR25 0,20, podejrzenie błędu odczytu, niska pewność lub niespójność, Kontrola losowa `fnv1a(rozwiazanie_id)` z podwójnym odsetkiem dla Poprawek, T-6 bez Oceny, T-7 Poprawki niezgodne/brak rezultatu/limit miękki), utworzenie Weryfikacji z `powody`, `powody_poczatkowe`, priorytetem, typem kolejki i Nauczycielem docelowym (11.4.1), widoczność `WSTRZYMANA_BLAD_ODCZYTU` (ADR-022), zdarzenie `OcenaWystawiona`. Sygnały LLM działają tylko w kierunku bezpiecznym (Z-1, Z-3).
- **Definition of Done:**
  - [ ] Testy tablicy decyzji 11.3 (każda reguła i ich kombinacje); dowód nigdy `OSTATECZNA` automatycznie.
  - [ ] Test własności Z-1: żaden sygnał LLM nie podnosi punktów ani nie zezwala na trasę automatyczną.
- **Status:** TODO

### TASK-086 — Praca `OCEN_ROZWIAZANIE`: terminy miękki i twardy, NFR-01

- **Faza:** F9
- **Zależności:** TASK-041, TASK-044, TASK-062, TASK-081, TASK-085
- **Realizuje:** Sekcje: 9.5, 14, 14.2, 13.2 · ADR: ADR-025, ADR-039 · Prace w tle: `OCEN_ROZWIAZANIE` · NFR: NFR-01 · Założenia: A-11 · Zasady nadrzędne: Z-3 · Scenariusze NFR-01: S-1, S-2, S-3, S-6
- **Opis:** Handler: warunki idempotencji z 13.3, przyjęcie planu przez ogranicznik (lub od razu model zapasowy), termin miękki `max(10 s, 28 s − t_R)` skracający timeouty, termin twardy 120 s → T-6 (`BLAD_PRZETWARZANIA`, bez Oceny), oczekiwanie na rezultat Sprawdzenia do `zatwierdzono_at + 6 s`, zapis Oceny, Wyniku zadania, `czas_systemowy_ms`, Wskazówka asynchronicznie (ADR-025).
- **Definition of Done:**
  - [ ] Testy scenariuszy S-1, S-1b, S-2, S-2b, S-3 i S-6 z atrapami i zegarem (wyniki zgodne z tabelą 17.2).
  - [ ] Metryki `ocena_czas_systemowy_sekundy`, `czas_od_przeslania_do_oceny_widocznej_sekundy`.
- **Status:** TODO

### TASK-087 — Uzasadnienie Oceny

- **Faza:** F9
- **Zależności:** TASK-085
- **Realizuje:** Sekcje: 11.7
- **Opis:** Generowanie `ocena.uzasadnienie` z decyzji (szablony 11.7) i ekspozycja w `OcenaWyjscie`.
- **Definition of Done:**
  - [ ] Każda Ocena ma niepuste uzasadnienie zgodne z decyzjami (test).
- **Status:** TODO

### TASK-088 — Odczyt Oceny i historia wersji

- **Faza:** F9
- **Zależności:** TASK-027, TASK-085
- **Realizuje:** Sekcje: 8.2, 12.6 · API: `GET /rozwiazania/{id}/ocena`
- **Opis:** `OcenaWyjscie` (z `widocznosc_dla_ucznia` `PELNA`/`WSTRZYMANA_BLAD_ODCZYTU`) i `?historia=true`; przykład odpowiedzi 12.6 jako test kontraktowy.
- **Definition of Done:**
  - [ ] Test kontraktowy z przykładem 12.6.
  - [ ] Uczeń nie widzi punktów, typu błędu ani Wskazówki przy wstrzymaniu.
- **Status:** TODO

### TASK-089 — Agent wyjaśniający i praca `NAPISZ_WSKAZOWKE`

- **Faza:** F9
- **Zależności:** TASK-042, TASK-085
- **Realizuje:** Sekcje: 10.5, 10.5.1, 10.5.2, 10.5.3 · Agenci: Agent wyjaśniający · Prace w tle: `NAPISZ_WSKAZOWKE` · Macierz błędów: E-11
- **Opis:** `lemat.agenci.wyjasniajacy`: Wskazówka ≤ 400 znaków z decyzji (błędny krok, typ błędu) bez gotowego rozwiązania, zasady promptu 10.5.2, strażnik wycieku rozwiązania 10.5.3 (porównanie z Rozwiązaniem wzorcowym), timeout 8 s, fallback szablon (`SZABLON`), jednokrotny zapis (Z-5).
- **Definition of Done:**
  - [ ] Testy strażnika na przypadkach wycieku (np. podany wynik końcowy) → szablon.
  - [ ] Metryka `wskazowka_opoznienie_sekundy`.
- **Status:** TODO

### TASK-090 — Opinia pomocnicza mocniejszego LLM

- **Faza:** F9
- **Zależności:** TASK-042, TASK-085
- **Realizuje:** Sekcje: 10.4.5 · Prace w tle: `OPINIA_POMOCNICZA` · Scenariusze wyścigów: C-10
- **Opis:** Praca w puli W (tylko `NISKA_PEWNOSC`, R-05), zapis `UPDATE weryfikacja SET opinia_pomocnicza = … WHERE opinia_pomocnicza IS NULL` bez warunku na status i bez zmiany `version`; nigdy nie zmienia Oceny (ADR-008).
- **Definition of Done:**
  - [ ] Test C-10: opinia po rozstrzygnięciu zapisana do metryk, bez zmiany Oceny.
- **Status:** TODO

## F10 — Weryfikacja i Kolejka weryfikacji

### TASK-091 — Kolejka weryfikacji: przydział, odczyt i filtry

- **Faza:** F10
- **Zależności:** TASK-027, TASK-059, TASK-085
- **Realizuje:** Sekcje: 9.6, 11.4, 11.4.2, 11.4.1 · API: `GET /weryfikacje`, `GET /weryfikacje/{id}` · FR: FR-33
- **Opis:** Zapytania kolejki z `?dostepnosc=DOSTEPNE|MOJE_W_TRAKCIE|WSZYSTKIE&powod&klasa_id&typ_kolejki`, sortowanie (priorytet, `termin_at`), reguła odczytu (`W_TRAKCIE` z wygasłą blokadą = `OCZEKUJE`), pomijanie Uczniów `USUWANE`, uprawnienia (Nauczyciel docelowy, Klasa, weryfikator globalny).
- **Definition of Done:**
  - [ ] Testy API i uprawnień; zapytanie korzysta z `ix_weryfikacja_kolejka` (EXPLAIN w teście).
- **Status:** TODO

### TASK-092 — Ponowna ocena po błędzie przetwarzania (`PONOW_OCENE`)

- **Faza:** F10
- **Zależności:** TASK-086, TASK-091
- **Realizuje:** Sekcje: 9.13 · Prace w tle: `PONOW_OCENE` · Scenariusze wyścigów: C-23
- **Opis:** Dla Weryfikacji z `BLAD_PRZETWARZANIA` i bez Oceny: ponowna ocena po 15 min / 1 h, dołączenie Oceny wstępnej (ustawia `powody_poczatkowe`) albo trasa automatyczna (Weryfikacja `ANULOWANA`, `PONOWNA_OCENA_AUTOMATYCZNA`); zapis warunkowy `WHERE status = 'OCZEKUJE' AND version = :v`.
- **Definition of Done:**
  - [ ] Test C-23: podjęcie przez Nauczyciela w trakcie → wynik `PONOW_OCENE` odrzucony.
- **Status:** TODO

### TASK-093 — Podjęcie, przedłużenie, zwolnienie blokady i szkic rozstrzygnięcia

- **Faza:** F10
- **Zależności:** TASK-022, TASK-091
- **Realizuje:** Sekcje: 9.6.1, 8.3 · ADR: ADR-033, ADR-037 · API: `POST /weryfikacje/{id}/podjecie`, `POST /weryfikacje/{id}/przedluzenie`, `PUT /weryfikacje/{id}/szkic`, `POST /weryfikacje/{id}/zwolnienie` · Prace w tle: `ZWOLNIJ_BLOKADY_WERYFIKACJI` · Scenariusze wyścigów: C-07, C-08, C-31 · Macierz błędów: E-16
- **Opis:** Blokada 15 min, przedłużanie (łącznie ≤ 2 h, `409 BLOKADA_MAKS_CZAS`), ponowne podjęcie przez właściciela, zwolnienie z zachowaniem szkicu, autozapis szkicu (tylko właściciel), `ZWOLNIJ_BLOKADY_WERYFIKACJI` co 1 min w schedulerze; operacje blokady nie zmieniają `version` (ADR-037).
- **Definition of Done:**
  - [ ] Testy C-07, C-08, C-31 (rozstrzygnięcie po 40 min i 8 przedłużeniach → `200`).
  - [ ] `409 WERYFIKACJA_ZAJETA` (E-16) i `409 WERYFIKACJA_NIE_PODJETA`.
- **Status:** TODO

### TASK-094 — Rozstrzygnięcie Weryfikacji

- **Faza:** F10
- **Zależności:** TASK-062, TASK-089, TASK-093
- **Realizuje:** Sekcje: 9.6.2, 8.2, 8.3 · ADR: ADR-037 · API: `POST /weryfikacje/{id}/rozstrzygniecie` · Scenariusze wyścigów: C-09, C-28, C-37 · FR: FR-34
- **Opis:** `RozstrzygniecieWejscie` z `If-Match` (wersja treści): `SELECT rozwiazanie FOR UPDATE`, potem `weryfikacja`, potem `SELECT status FROM uzytkownik … FOR SHARE` (`409 KONTO_UCZNIA_NIEAKTYWNE`), nowa Ocena `OSTATECZNA` (`NAUCZYCIEL`) zastępująca poprzednią, reguły Wskazówki, Wynik zadania, Próbka kalibracyjna, `412` z `zmiany_tresci`, powiadomienie (bez powiadomienia przy potwierdzeniu bez zmian).
- **Definition of Done:**
  - [ ] Testy C-09, C-28, C-37 (kolejność blokad bez zakleszczenia).
  - [ ] Potwierdzenie bez zmian w Kontroli losowej tworzy nową wersję Oceny (8.2).
- **Status:** TODO

### TASK-095 — Korekta Oceny z poziomu Rozwiązania

- **Faza:** F10
- **Zależności:** TASK-094
- **Realizuje:** Sekcje: 9.6.3 · API: `POST /rozwiazania/{id}/korekty` · Zdarzenia domenowe: `OcenaSkorygowana` · Scenariusze wyścigów: C-26
- **Opis:** Korekta Rozwiązania `OCENIONE` (Nauczyciel Klasy/docelowy): zamknięcie otwartej Weryfikacji `OCZEKUJE` jako `ZAKONCZONA`, `409 WERYFIKACJA_ZAJETA` gdy `W_TRAKCIE` u innego, `OcenaSkorygowana` → `AKTUALIZUJ_PROFIL`, `NAPISZ_WSKAZOWKE`, powiadomienie `OCENA_SKORYGOWANA`.
- **Definition of Done:**
  - [ ] Test C-26 i scenariuszy 9.6.3; korekta zapisuje Próbkę kalibracyjną (FR-34).
- **Status:** TODO

### TASK-096 — Eskalacja, SLA w dniach roboczych i kontrola przyjęć

- **Faza:** F10
- **Zależności:** TASK-010, TASK-022, TASK-030, TASK-091
- **Realizuje:** Sekcje: 11.4.3, 8.3 · ADR: ADR-021, ADR-043 · Prace w tle: `ESKALUJ_WERYFIKACJE` · Macierz błędów: E-28 · Założenia: A-12, A-15 · Decyzje PO: D-5, D-13 · Ryzyka: R-15
- **Opis:** Praca co godzinę: SLA `dodaj_dni_robocze(created_at, 3)`, eskalacja `KLASA → GLOBALNA` (po SLA, utracie Nauczyciela docelowego, karencji konta Nauczyciela), priorytet 1 przy zdjęciach ≥ 80 dni, rozstrzyganie na korzyść Ucznia po usunięciu zdjęć (11.4.3 p. 3), kontrola przyjęć globalna (zaległość > 2× pojemność) i per Klasa (`odsetek_eskalacji_7d ≥ 0,20` bez `KARENCJA_KONTA_NAUCZYCIELA`), podsumowanie dla Nauczycieli 05:00 UTC w dni robocze, alert `ALERT_WERYFIKACJE` (E-28). Brak automatycznej finalizacji Oceny wstępnej (D-5).
- **Definition of Done:**
  - [ ] Testy z zegarem i kalendarzem (weekend, dzień wolny).
  - [ ] Metryki `zaleglosc_globalna_min`, `pojemnosc_globalna_min`, `naplyw_eskalacji_min_7d`.
- **Status:** TODO

### TASK-097 — Pseudonimizacja w kolejce globalnej

- **Faza:** F10
- **Zależności:** TASK-079, TASK-091
- **Realizuje:** Sekcje: 11.4.4, 16.3 · ADR: ADR-031 · NFR: NFR-09
- **Opis:** `uczen_pseudonim` (HMAC z sekretem, stały, nieodwracalny bez sekretu) zamiast danych Ucznia dla weryfikatora globalnego spoza Klasy; wyłącznie zdjęcie maskowane i to jedno Rozwiązanie.
- **Definition of Done:**
  - [ ] Test: odpowiedzi API dla obcego weryfikatora nie zawierają imienia, nazwiska, e-maila ani `uczen_id`.
- **Status:** TODO

### TASK-098 — Zgłoszenie błędu odczytu przez Ucznia

- **Faza:** F10
- **Zależności:** TASK-026, TASK-094
- **Realizuje:** Sekcje: 9.12, 11.6, 11.8, 7.5.4 · ADR: ADR-022 · API: `POST /rozwiazania/{id}/zgloszenie-bledu-odczytu` · Zdarzenia domenowe: `BladOdczytuZgloszony` · Scenariusze wyścigów: C-25, C-29 · FR: FR-28 · Ryzyka: R-13
- **Opis:** Warunki 11.6 (okno 14 dni, 1 na Rozwiązanie, 5 na 30 dni), dopisanie powodu do otwartej Weryfikacji (podnosi `version`) albo nowa Weryfikacja, zawieszenie Wyniku zadania (`uniewazniony_at`), raport nadużyć (uznane < 20% przy ≥ 5 zgłoszeniach), skuteczność wykrywania błędów odczytu i czułość T-3 (11.8, `bledy_odczytu_niewykryte_30d`).
- **Definition of Done:**
  - [ ] Testy C-25 i C-29.
  - [ ] Odpowiedź `201 {weryfikacja: WeryfikacjaUcznia}` i powiadomienie `NOWA_WERYFIKACJA`.
- **Status:** TODO

### TASK-099 — Model obciążenia Nauczycieli i raport obciążenia

- **Faza:** F10
- **Zależności:** TASK-049, TASK-096
- **Realizuje:** Sekcje: 17.7 · API: `GET /admin/obciazenie-weryfikacji` · Założenia: A-13, A-14 · Decyzje PO: D-4, D-17 · Ryzyka: R-02, R-12
- **Opis:** Implementacja modelu tygodniowego 17.7 (współczynnik zapasu 1,3, 5 dni roboczych, odsetek eskalacji zmierzony lub 5%, wariant z aktywnym bezpiecznikiem poprawek), cotygodniowe przeliczenie, wyliczenie `wymagani_weryfikatorzy_globalni`, endpoint `ObciazenieWeryfikacjiWyjscie`; funkcja prognozy dnia roboczego Klasy dla walidacji A-14.
- **Definition of Done:**
  - [ ] Test: dla danych A-03/A-13/A-15 wynik = 18 weryfikatorów na start (wariant bezpiecznika: 20).
  - [ ] Wartości zmierzone (`weryfikacja_czas_pracy_sekundy`) zastępują założenia, gdy dostępne.
- **Status:** TODO

## F11 — Kalibracja, Próg pewności, bezpieczniki

### TASK-100 — Próbki kalibracyjne, źródło próbki i wagi IPW

- **Faza:** F11
- **Zależności:** TASK-094, TASK-095
- **Realizuje:** Sekcje: 9.8, 11.5, 11.5.2 · ADR: ADR-027 · Pliki testowe/danych: `tests/kalibracja/test_zrodlo_probki.py` · Ryzyka: R-07 · PZS-1 (część B): B-5
- **Opis:** Tworzenie `probka_kalibracyjna` przy rozstrzygnięciu i korekcie: Zgodność Oceny (punkty + pierwszy błędny krok + `brakujacy_krok`, ADR-027), źródło próbki z `powody_poczatkowe` (11.5.2), wagi IPW z korektą wykonania Kontroli, wyłączenie powodów nielosowych z populacji progu, Uczniowie `USUWANE` pominięci.
- **Definition of Done:**
  - [ ] `tests/kalibracja/test_zrodlo_probki.py` sprawdza każdy wiersz tabeli 11.5.2 (w tym przypadki z dopisanym zgłoszeniem i `PONOW_OCENE`).
  - [ ] `ux_probka_ocena_auto`: jedna próbka na Ocenę automatyczną.
- **Status:** TODO

### TASK-101 — Kalibrator pewności (izotoniczny, 5-krotna walidacja krzyżowa)

- **Faza:** F11
- **Zależności:** TASK-100
- **Realizuje:** Sekcje: 11.2.2 · NFR: NFR-04
- **Opis:** Dopasowanie kalibratora izotonicznego per (konfiguracja, rodzaj) na Próbkach z wagami IPW, wartości out-of-fold (cv5), ECE i wykres niezawodności; progi początkowe środowiska testowego 0,95 / 0,98 przed ZT-1.
- **Definition of Done:**
  - [ ] Test na danych syntetycznych: ECE ≤ 0,03 dla dobrze skalibrowanego generatora; monotoniczność.
- **Status:** TODO

### TASK-102 — Algorytm Progu pewności (Wilson, `n_eff`, tryb pełnej weryfikacji)

- **Faza:** F11
- **Zależności:** TASK-101
- **Realizuje:** Sekcje: 11.5.1, 11.5.3 · ADR: ADR-007 · Założenia: A-09 · Decyzje PO: D-6 · Ryzyka: R-10
- **Opis:** Wybór progu z zakresu 0,800–0,990: najniższy próg, dla którego górna granica Wilsona 95% błędu ocen automatycznych ≤ 2% przy `n_eff ≥ 189` (out-of-fold); brak takiego progu → tryb pełnej weryfikacji (`1.000`); dowód zawsze 1.000; odsetek Kontroli losowej 0,20 do 400 próbek, potem 0,05; analiza mocy 11.5.1 jako test regresji liczb.
- **Definition of Done:**
  - [ ] Testy na tablicach z 11.5.1 (tabele 1 i 2) i 11.5.3.
  - [ ] Próg ręczny Administratora nigdy niższy niż wyznaczony (test).
- **Status:** TODO

### TASK-103 — Praca `KALIBRUJ` i API progów oraz metryk kalibracji

- **Faza:** F11
- **Zależności:** TASK-022, TASK-029, TASK-102
- **Realizuje:** Sekcje: 9.8, 11.5.3 · API: `GET /admin/progi-pewnosci`, `POST /admin/progi-pewnosci`, `GET /admin/kalibracja` · Prace w tle: `KALIBRUJ` · Scenariusze wyścigów: C-21 · Macierz błędów: E-21, E-29 · FR: FR-34 · NFR: NFR-02, NFR-03, NFR-04
- **Opis:** Codziennie 03:00 UTC „Wynik dnia KALIBRUJ” kroki 1–6 (C4-02): wybór progu, ocena bezpieczników na stanie sprzed przebiegu, Blokada obniżania progu, dozwolone obniżenie, łączny wynik, jeden `INSERT prog_pewnosci` i `UPSERT stan_bezpiecznika`; okno próbek do `now() − 5 min` (C-21); reguły nadzoru wykonania Kontroli (0,90 / 0,80 / 0,60, E-29), alert `ALERT_KALIBRACJI`; endpointy progów (ręczne ustawienie z audytem) i `MetrykiKalibracjiWyjscie` (NFR-02/03/04).
- **Definition of Done:**
  - [ ] Testy kolejności kroków 1–6 (bezpiecznik nie jest cofany przez wybór progu tego samego dnia).
  - [ ] Metryki `odsetek_automatycznych`, ECE, zgodność 30 dni dostępne w `/admin/kalibracja`.
- **Status:** TODO

### TASK-104 — Bezpieczniki trafności i poprawek

- **Faza:** F11
- **Zależności:** TASK-103
- **Realizuje:** ADR: ADR-046 · API: `GET /admin/bezpieczniki`, `POST /admin/bezpieczniki/dezaktywacja` · Pliki testowe/danych: `tests/kalibracja/test_bezpiecznik.py`, `tests/kalibracja/symulacja_bezpiecznika.py` · NFR: NFR-03 · Decyzje PO: D-16 · Ryzyka: R-20, R-22 · Reguły trasowania: T-7
- **Opis:** Wyzwolenie przy dolnej granicy Wilsona 95% błędu > 2% przy `n_eff ≥ 50` (okno 30 dni powyżej progu sprzed przebiegu): bezpiecznik trafności podnosi próg (`max(wybór, prog + 0,02)`, przy 0,990 tryb pełnej weryfikacji — opcja domyślna D-16), bezpiecznik poprawek kieruje każdą Transkrypcję z Poprawkami do T-7; Blokada obniżania progu; dezaktywacja przez Administratora z uzasadnieniem (10–1000 znaków) i nowym `okno_od`; metryka `bezpiecznik_wyzwolenia_total`.
- **Definition of Done:**
  - [ ] `tests/kalibracja/test_bezpiecznik.py` (a)–(e) z 11.5.3 przechodzi.
  - [ ] `tests/kalibracja/symulacja_bezpiecznika.py` (także `--dynamika`) odtwarza tabelę P(wyzwolenia) z 11.5.3 w granicy błędu Monte Carlo.
- **Status:** TODO

### TASK-105 — Ewaluacja na ZT-1 (`EWALUUJ_ZT1`, tryby REFERENCYJNY i E2E)

- **Faza:** F11
- **Zależności:** TASK-077, TASK-086, TASK-101, TASK-102
- **Realizuje:** Sekcje: 17.6 · API: `POST /admin/konfiguracje-modeli/{id}/ewaluacja-zt1` · Prace w tle: `EWALUUJ_ZT1`
- **Opis:** Wczytanie manifestu `zbiory-testowe/zt1/` (Zadanie, zdjęcia, Transkrypcja referencyjna, Oceny 2–3 Nauczycieli), tryb `REFERENCYJNY` (G-1: zgodność, LB Wilsona, próg out-of-fold, ECE) i `E2E` (G-2: CER, kroki identyczne, czułość T-3, zgodność end-to-end, Filtr danych osobowych), zapis kalibratora i `prog_pewnosci` `EWALUACJA_ZT1`, raport bramek.
- **Definition of Done:**
  - [ ] Raport G-1/G-2 generowany na fikstury (mini ZT-1) z poprawnymi formułami.
  - [ ] Aktywacja konfiguracji bez progu `EWALUACJA_ZT1` zablokowana (`409 BRAK_PROGU_DLA_KONFIGURACJI`).
- **Status:** TODO

## F12 — Agent generujący i Walidacja zadania

### TASK-106 — Agent generujący i zlecenia generowania

- **Faza:** F12
- **Zależności:** TASK-020, TASK-026, TASK-042, TASK-052
- **Realizuje:** Sekcje: 9.7, 10.2, 10.2.1 · API: `POST /zadania/generowanie`, `GET /zlecenia-generowania/{id}` · Agenci: Agent generujący · Prace w tle: `GENERUJ_ZADANIE` · FR: FR-11
- **Opis:** `lemat.agenci.generujacy`: wejście (Umiejętność, Poziom trudności, poziom matury, rodzaj, przykładowe Zadania bazowe), wyjście (treść, Rozwiązanie wzorcowe w wariantach, Kryteria oceniania, opcje zamknięte), zlecenie (limit 20/dobę na Nauczyciela, Idempotency-Key), `GENERUJ_ZADANIE` z warunkową inkrementacją `liczba_cykli < 3`.
- **Definition of Done:**
  - [ ] Test przepływu 9.7 na atrapach do stanu `WALIDACJA`.
  - [ ] Wyjście przechodzi W-1 (schemat) w 100% przypadków atrapy.
- **Status:** TODO

### TASK-107 — Walidacja zadania (W-1 … W-10)

- **Faza:** F12
- **Zależności:** TASK-032, TASK-040, TASK-106
- **Realizuje:** Sekcje: 10.2.2, 10.2.3 · Prace w tle: `WALIDUJ_ZADANIE` · Macierz błędów: E-17 · FR: FR-13 · Sprawdzenia Walidacji: W-1, W-2, W-3, W-4, W-5, W-6, W-7, W-8, W-9, W-10 · Pytania do modelu decyzyjnego: Q-WA-01, Q-WA-02, Q-WA-03, Q-WA-04, Q-WA-05 · PZS-1 (część B): B-9
- **Opis:** Praca `WALIDUJ_ZADANIE`: sprawdzenia W-1 … W-10 (schemat, SymPy, niezależne rozwiązanie przez LLM `GENERATYWNY_ROZWIAZUJACY_NIEZALEZNIE`, ciągłość kroków Q-WA-04, jednoznaczność Q-WA-01, podstawa programowa Q-WA-02, Poziom trudności Q-WA-03, dystraktory, duplikat `similarity < 0,80`, dowód wzorcowy Q-WA-05), zapis `walidacja_zadania.sprawdzenia` (10.2.3), przejście do `OPUBLIKOWANE`/`DO_PRZEGLADU`/`ODRZUCONE_W_WALIDACJI` lub nowy cykl.
- **Definition of Done:**
  - [ ] Testy każdego sprawdzenia W-x na przypadkach pozytywnych i negatywnych (E-17).
  - [ ] Trasa po Walidacji zgodna z 8.4 (zlecenie Nauczyciela zawsze `DO_PRZEGLADU`).
- **Status:** TODO

### TASK-108 — Uzupełnianie puli Zadań i budżet generowania systemowego

- **Faza:** F12
- **Zależności:** TASK-069, TASK-107
- **Realizuje:** Prace w tle: `UZUPELNIJ_PULE` · Scenariusze wyścigów: C-18 · Macierz błędów: E-30
- **Opis:** `UZUPELNIJ_PULE` dla `ZAMKNIETE`/`OTWARTE_OBLICZENIOWE`, gdy pula `OPUBLIKOWANE`+`GLOBALNY` < 3 dla klucza i brak otwartego zlecenia; budżet dzienny 200 zleceń i 150 PLN (alert przy 80%, E-30).
- **Definition of Done:**
  - [ ] Test C-18: dwa zlecenia tej samej puli → jedna praca, jedno zlecenie.
  - [ ] Wyczerpany budżet → brak nowych zleceń i alert.
- **Status:** TODO

### TASK-109 — Automatyczne szkice Wkładek teoretycznych

- **Faza:** F12
- **Zależności:** TASK-056, TASK-106
- **Realizuje:** API: `POST /admin/wkladki-teoretyczne/szkice` · Prace w tle: `GENERUJ_SZKIC_WKLADKI`
- **Opis:** Szkic Wkładki z Agenta generującego (ręcznie przez Administratora lub automatycznie przy E-20), ≤ 20/dobę w budżecie 12.7, wstrzymanie na 30 dni po 2 odrzuceniach automatycznych szkiców (`ix_wkladka_automatyczne_odrzucone`); szkic zawsze przez Przegląd.
- **Definition of Done:**
  - [ ] Testy warunków wykonania pracy i budżetu; zdarzenie `WkladkaDoPrzegladu` po utworzeniu szkicu.
- **Status:** TODO

## F13 — RODO: retencja, usuwanie kont, Rejestr Zdarzeń RODO

### TASK-110 — Rejestr Zdarzeń RODO w buckecie Object Lock

- **Faza:** F13
- **Zależności:** TASK-009, TASK-028
- **Realizuje:** ADR: ADR-041 · Macierz błędów: E-36
- **Opis:** Zapis Zdarzeń RODO z inicjatywy użytkownika przez `api` **przed** zatwierdzeniem transakcji (`rejestr-zdarzen-rodo/zdarzenia/{rrrr-mm-dd}/{uzytkownik_id}/{id}.json`, `If-None-Match: *`) oraz `USUNIECIE_WYKONANE` przez `worker` pod kluczem bez daty `wykonane/{uzytkownik_id}/{id}.json` (`id = uuid5(NS_RODO, …)`); `dane.rola` w każdym zdarzeniu; wiersz `zdarzenie_rodo` w transakcji.
- **Definition of Done:**
  - [ ] Niedostępny bucket → `503`, nic nie zapisano (E-36).
  - [ ] Test idempotencji ponownego zapisu `USUNIECIE_WYKONANE` w innym dniu.
- **Status:** TODO

### TASK-111 — Wniosek o usunięcie konta Ucznia i okres karencji

- **Faza:** F13
- **Zależności:** TASK-021, TASK-045, TASK-110
- **Realizuje:** Sekcje: 16.4 · ADR: ADR-040 · API: `DELETE /uczniowie/ja` · Zdarzenia domenowe: `KontoUsuwane` · Scenariusze wyścigów: C-20 · NFR: NFR-09
- **Opis:** `DELETE /uczniowie/ja` (`{haslo}` + Idempotency-Key → `202`): `SluzbaKont.zastosuj_wniosek_o_usuniecie` (status `USUWANE`, `usuniecie_zaplanowane_at = now() + 30 dni`), procesy wstrzymane, nie anulowane: handlery kończą się z `KONTO_USUWANE`, kolejka i Kontrole losowe pomijają konto; zdarzenie `KontoUsuwane`.
- **Definition of Done:**
  - [ ] Test C-20 (wniosek w trakcie oceniania).
  - [ ] Test kontraktowy: każdy handler z adnotacją „konto `AKTYWNE`” (13.3) kończy się bez efektu w karencji.
- **Status:** TODO

### TASK-112 — Przywrócenie konta w okresie karencji

- **Faza:** F13
- **Zależności:** TASK-096, TASK-111
- **Realizuje:** Sekcje: 16.4 · ADR: ADR-040 · API: `POST /admin/uzytkownicy/{id}/przywrocenie` · Zdarzenia domenowe: `KontoPrzywrocone` · Scenariusze wyścigów: C-33 · Decyzje PO: D-15
- **Opis:** Zdarzenie `ANULOWANIE_USUNIECIA` w rejestrze, potem jedna transakcja wg tabel przywrócenia 16.4 (osobno Uczeń oraz Nauczyciel/Administrator): powrót Weryfikacji z nowym `termin_at`, ponowne prace `ROZPOZNAJ_PISMO`/`OCEN_ROZWIAZANIE`, `AKTUALIZUJ_PROFIL`, kopie treningowe zaległe, cofnięcie eskalacji `KARENCJA_KONTA_NAUCZYCIELA` niepodjętych, Klasy przekazane zostają u nowego Nauczyciela (D-15).
- **Definition of Done:**
  - [ ] Test C-33 i każdego wiersza tabel przywrócenia 16.4.
- **Status:** TODO

### TASK-113 — Usuwanie kont Nauczycieli i Administratorów, Przekazanie Klasy

- **Faza:** F13
- **Zależności:** TASK-059, TASK-111
- **Realizuje:** Sekcje: 16.4 · ADR: ADR-044 · API: `DELETE /nauczyciele/ja`, `POST /admin/uzytkownicy/{id}/usuniecie`, `POST /admin/klasy/{id}/przekazanie` · NFR: NFR-09
- **Opis:** Wniosek Nauczyciela (`KLASA_DO_PRZEKAZANIA` dla Administratorów), usunięcie konta przez Administratora (`409 OSTATNI_ADMINISTRATOR`), Przekazanie Klasy (Klasa, otwarte Przypisania, Zadania `PRYWATNY` z tych Przypisań, Weryfikacje `KLASA`), kroki (1)–(5) usunięcia konta Nauczyciela/Administratora, odebranie flag.
- **Definition of Done:**
  - [ ] Testy API i kroków usunięcia; Klasa nie zostaje bez Nauczyciela prowadzącego.
- **Status:** TODO

### TASK-114 — Retencja zdjęć: `usun_po` i praca `USUN_ZDJECIA`

- **Faza:** F13
- **Zależności:** TASK-022, TASK-074
- **Realizuje:** Sekcje: 9.10, 16.4 · ADR: ADR-019 · Prace w tle: `USUN_ZDJECIA` · Macierz błędów: E-22 · NFR: NFR-09
- **Opis:** Tabela przejść ustawiających `zdjecie_rozwiazania.usun_po` (16.4) jako jedyne źródło prawdy, praca co godzinę usuwająca obiekty (oryginał i maskowane), alert przed usunięciem przy otwartej Weryfikacji (80 dni), `ALERT_RODO` przy zaległości > 24 h (E-22), metryka `zdjecia_zalegle`.
- **Definition of Done:**
  - [ ] Test każdego wiersza tabeli `usun_po`; twardy limit 90 dni (CHECK) nigdy nie przekroczony.
- **Status:** TODO

### TASK-115 — Kopie treningowe za zgodą i zgoda na trening

- **Faza:** F13
- **Zależności:** TASK-048, TASK-110, TASK-114
- **Realizuje:** Sekcje: 16.4 · ADR: ADR-035 · Prace w tle: `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`, `USUN_KOPIE_TRENINGOWE` · NFR: NFR-09 · Decyzje PO: D-8
- **Opis:** `PATCH /uczniowie/ja` z `zgoda_na_trening` (Zdarzenia RODO `ODWOLANIE_ZGODY`/`UDZIELENIE_ZGODY`, odwołanie ustawia `kopia_treningowa.usun_po := now()`), `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` po każdej transakcji kończącej się `OCENIONE` bez otwartej Weryfikacji, `USUN_KOPIE_TRENINGOWE` co godzinę (usunięcie ≤ 1 h), maks. 24 miesiące.
- **Definition of Done:**
  - [ ] Testy: brak zgody → brak kopii; odwołanie → obiekty usunięte w ≤ 1 h (zegar testowy).
- **Status:** TODO

### TASK-116 — Praca `USUN_KONTO` dla Ucznia (kroki 0–6)

- **Faza:** F13
- **Zależności:** TASK-111, TASK-114, TASK-115
- **Realizuje:** Sekcje: 16.4 · Prace w tle: `USUN_KONTO`
- **Opis:** Kroki 0–6 z 16.4: (0) obiekt `USUNIECIE_WYKONANE`, (1) `INSERT zdarzenie_rodo` + anulowanie procesów (Rozwiązania → `ANULOWANE`, Weryfikacje → `ANULOWANA`), (2) `decyzja_modelu` bez danych, (3) Próbki bez powiązań, (4) obiekty zdjęć, (4a) kopie treningowe, (5) `DELETE FROM uzytkownik`, (6) audyt `KONTO_USUNIETE`; konta nieaktywne 24 miesiące (ostrzeżenie e-mail, usunięcie po 30 dniach); zmienna testowa `LEMAT_TEST_ZATRZYMAJ_USUN_KONTO_PO_KROKU` tylko w środowisku testowym.
- **Definition of Done:**
  - [ ] Testy idempotencji każdego kroku i ponowienia po awarii.
  - [ ] Po usunięciu brak danych osobowych Ucznia w bazie i magazynie (test przeszukujący).
- **Status:** TODO

### TASK-117 — Eksport danych Ucznia

- **Faza:** F13
- **Zależności:** TASK-065, TASK-074, TASK-088
- **Realizuje:** API: `GET /uczniowie/ja/eksport-danych`
- **Opis:** ZIP z danymi osobowymi, Rozwiązaniami (bez zdjęć usuniętych), Transkrypcjami, Ocenami, Profilem w formacie JSON + README po polsku.
- **Definition of Done:**
  - [ ] Test zawartości ZIP na fiksturze konta.
- **Status:** TODO

### TASK-118 — Uzgadnianie Rejestru Zdarzeń RODO (`UZGODNIJ_REJESTR_RODO`)

- **Faza:** F13
- **Zależności:** TASK-022, TASK-110
- **Realizuje:** Sekcje: 13.5 · ADR: ADR-041 · Prace w tle: `UZGODNIJ_REJESTR_RODO`
- **Opis:** Co 5 min: obiekty `zrodlo = API` z ostatnich 24 h, starsze niż 5 min i bez wiersza `zdarzenie_rodo` → obiekt kompensujący `UNIEWAZNIENIE` (okno ≤ 10 min); `USUNIECIE_WYKONANE` nigdy nie unieważniane, tylko doprowadzane do końca (ponowne `USUN_KONTO` po 2 h, alert po 26 h); metryki `zdarzenia_rodo_uniewaznione_total`, `usuniecie_wykonane_niedokonczone`.
- **Definition of Done:**
  - [ ] Testy obu gałęzi z zegarem testowym.
- **Status:** TODO

### TASK-119 — Procedura odtworzenia PITR: `ponow_zdarzenia_rodo`

- **Faza:** F13
- **Zależności:** TASK-112, TASK-113, TASK-115, TASK-116, TASK-118
- **Realizuje:** Sekcje: 16.4 · ADR: ADR-041 · Scenariusze wyścigów: C-34, C-39 · Ryzyka: R-16
- **Opis:** Skrypt `lemat ponow-zdarzenia-rodo` uruchamiany po odtworzeniu bazy do `T` przed startem `worker` i `scheduler` (api w trybie tylko do odczytu): odczyt prefiksów `zdarzenia/` od `T − 5 min` i całego `wykonane/`, stosowanie chronologiczne, kryterium „zastosowany” zależne od źródła (`API`: wiersz `zdarzenie_rodo`; `WORKER`: brak wiersza `uzytkownik`), ponowienie wg roli konta tymi samymi funkcjami domenowymi co `api` i `USUN_KONTO`, metryka `ponowienie_rodo_brak_konta_total`.
- **Definition of Done:**
  - [ ] Testy C-34 i C-39 (PITR w trakcie `USUN_KONTO` po krokach 1–4).
  - [ ] Instrukcja operacyjna w `docs/` NIE jest edytowana — runbook w `HANDOFF.md`/README.
- **Status:** TODO

### TASK-120 — Sprzątanie osieroconych obiektów magazynu

- **Faza:** F13
- **Zależności:** TASK-022, TASK-114, TASK-115
- **Realizuje:** Sekcje: 16.4 · ADR: ADR-047 · Prace w tle: `SPRZATAJ_OSIEROCONE_OBIEKTY` · Macierz błędów: E-38
- **Opis:** Tryb `CODZIENNY` (04:30 UTC, obiekty starsze niż 1 h) i `ODTWORZENIE` (od `T − 5 min`): porównanie kluczy `zdjecia/`, `zdjecia-maskowane/`, `zbior-treningowy/` z `zdjecie_rozwiazania` i `kopia_treningowa` (paginacja po 1 000), usunięcie osieroconych, alert > 100 obiektów na dobę.
- **Definition of Done:**
  - [ ] Test E-38 (obiekt zapisany, transakcja wycofana) i scenariusz po PITR.
- **Status:** TODO

## F14 — Aplikacja PWA

### TASK-121 — Szkielet PWA: React 18 + TypeScript + Vite, i18n pl-PL, KaTeX, klient API

- **Faza:** F14
- **Zależności:** TASK-023
- **Realizuje:** ADR: ADR-014 · NFR: NFR-08, NFR-10
- **Opis:** Aplikacja w `src/pwa/` (propozycja ścieżki): manifest i service worker (cache statyk), `react-i18next` z jedynym językiem `pl-PL`, format dat `dd.MM.yyyy` i liczb z przecinkiem, KaTeX, klient API generowany z OpenAPI, odpytywanie co 1 s w stanach przejściowych (ADR-014), breakpointy 360/768/1280 px, komunikat o niewspieranej przeglądarce (A-10), lint „brak literałów UI poza plikami tłumaczeń”.
- **Definition of Done:**
  - [ ] `npm run build` i lint w CI; statyki serwowane przez Caddy.
  - [ ] Lint wykrywa literał UI w komponencie (test kontrolny).
- **Status:** TODO

### TASK-122 — PWA: rejestracja, logowanie, ustawienia konta, Test diagnostyczny

- **Faza:** F14
- **Zależności:** TASK-046, TASK-047, TASK-048, TASK-066, TASK-121
- **Realizuje:** FR: FR-01, FR-02, FR-03
- **Opis:** Ekrany rejestracji (oświadczenie 16+, kod zaproszenia), logowania, resetu hasła, ustawień (poziom matury, termin, Dział), Testu diagnostycznego z możliwością pominięcia.
- **Definition of Done:**
  - [ ] Testy komponentów i E2E ścieżki „rejestracja → test → pierwsza rekomendacja”.
- **Status:** TODO

### TASK-123 — PWA: rekomendacja, Ścieżka nauki, Wkładka, Zadanie zamknięte, Profil

- **Faza:** F14
- **Zależności:** TASK-065, TASK-070, TASK-073, TASK-121
- **Realizuje:** FR: FR-04, FR-05, FR-09, FR-10, FR-15
- **Opis:** Ekran „Dalej” z uzasadnieniem rekomendacji, historia Ścieżki nauki, Wkładka (KaTeX) z „przeczytane”, Zadanie zamknięte z wynikiem, Profil ucznia (Działy i Umiejętności), lista Przypisań.
- **Definition of Done:**
  - [ ] E2E: element proponowany → rozpoczęcie → Odpowiedź → kolejna rekomendacja.
- **Status:** TODO

### TASK-124 — PWA: zdjęcia, Transkrypcja, zatwierdzenie, Ocena, zgłoszenie błędu odczytu

- **Faza:** F14
- **Zależności:** TASK-075, TASK-076, TASK-080, TASK-088, TASK-098, TASK-121
- **Realizuje:** FR: FR-16, FR-18, FR-24, FR-27, FR-28
- **Opis:** `<input type="file" accept="image/*" capture="environment" multiple>`, kompresja w kliencie (JPEG q=0,85, dłuższy bok ≤ 2560 px), instrukcja „nie podpisuj kartki imieniem i nazwiskiem”, odpytywanie statusu, edycja Transkrypcji z autozapisem 2 s i komunikatami limitów, zatwierdzenie, widok Oceny (także wstrzymanej), akcje przy `BLAD_ROZPOZNAWANIA`, zgłoszenie błędu odczytu.
- **Definition of Done:**
  - [ ] E2E na profilach mobilnych: zdjęcie → Transkrypcja → poprawka → zatwierdzenie → Ocena (atrapy modeli).
- **Status:** TODO

### TASK-125 — PWA Nauczyciela: Klasy, Zestawy, Przypisania, postępy

- **Faza:** F14
- **Zależności:** TASK-061, TASK-064, TASK-121
- **Realizuje:** FR: FR-30, FR-31, FR-32
- **Opis:** Tworzenie Klasy i zaproszeń (link + kod), lista Uczniów i postępów, Zestawy zadań, Przypisania z terminem i postępem.
- **Definition of Done:**
  - [ ] E2E: Klasa → zaproszenie → dołączenie Ucznia → Przypisanie → postęp.
- **Status:** TODO

### TASK-126 — PWA Nauczyciela: Kolejka weryfikacji i rozstrzyganie

- **Faza:** F14
- **Zależności:** TASK-095, TASK-097, TASK-121
- **Realizuje:** FR: FR-33, FR-34
- **Opis:** Kolejka z powodami i filtrami, podjęcie, automatyczne przedłużanie co 5 min, autozapis szkicu, rozstrzygnięcie (punkty za kryteria, pierwszy błędny krok, typ błędu, Wskazówka), obsługa `412` z wyróżnieniem `zmiany_tresci`, korekta z Rozwiązania, Opinia pomocnicza, zdjęcia maskowane dla obcych.
- **Definition of Done:**
  - [ ] E2E: podjęcie → 2 przedłużenia → rozstrzygnięcie; scenariusz `412` po zgłoszeniu błędu odczytu.
- **Status:** TODO

### TASK-127 — PWA treści: generowanie, Przegląd Zadań, Zgłoszenia błędu, Wkładki

- **Faza:** F14
- **Zależności:** TASK-055, TASK-056, TASK-107, TASK-121
- **Realizuje:** FR: FR-11, FR-14
- **Opis:** Zlecenie generowania i podgląd statusu, Przegląd (zatwierdź/popraw/odrzuć) z edytorem Kryteriów, Zgłoszenia błędu w Zadaniu i ich rozpatrzenie (NR/A), edycja i publikacja Wkładek.
- **Definition of Done:**
  - [ ] E2E: zlecenie → `DO_PRZEGLADU` → zatwierdzenie `PRYWATNY` → użycie w Zestawie.
- **Status:** TODO

### TASK-128 — PWA Administratora

- **Faza:** F14
- **Zależności:** TASK-035, TASK-037, TASK-049, TASK-053, TASK-057, TASK-099, TASK-104, TASK-113, TASK-121
- **Realizuje:** FR: FR-35 · NFR: NFR-06
- **Opis:** Użytkownicy i flagi, zatwierdzanie Nauczycieli, import Arkuszy CKE z raportem, Konfiguracje modeli (aktywacja, ewaluacja ZT-1), progi i kalibracja, bezpieczniki z dezaktywacją, pula diagnostyczna, obciążenie weryfikacji, prace w tle, Przekazanie Klasy, przywracanie i usuwanie kont.
- **Definition of Done:**
  - [ ] E2E smoke każdego ekranu z kontem Administratora.
- **Status:** TODO

### TASK-129 — PWA: powiadomienia, usunięcie konta, eksport danych, zgoda na trening

- **Faza:** F14
- **Zależności:** TASK-030, TASK-111, TASK-115, TASK-117, TASK-121
- **Realizuje:** NFR: NFR-09
- **Opis:** Centrum powiadomień (licznik nieprzeczytanych), ekran prywatności: eksport ZIP, zgoda na trening, wniosek o usunięcie konta z informacją o 30-dniowej karencji.
- **Definition of Done:**
  - [ ] E2E: wniosek o usunięcie → wylogowanie → komunikat karencji przy logowaniu.
- **Status:** TODO

### TASK-130 — Testy E2E Playwright na profilach urządzeń

- **Faza:** F14
- **Zależności:** TASK-123, TASK-124, TASK-126
- **Realizuje:** Sekcje: 17.1
- **Opis:** Zestaw Playwright na profilach iPhone 13, Pixel 7, Desktop Chrome uruchamiany w CI na środowisku `lokalne` z atrapami modeli.
- **Definition of Done:**
  - [ ] Wszystkie ścieżki E2E z zadań F14 zielone na 3 profilach; raport w artefaktach CI.
- **Status:** TODO

## F15 — Obserwowalność, wdrożenie, bramki G-0 … G-7

### TASK-131 — Obserwowalność: ślady, metryki, logi, alerty, pulpity

- **Faza:** F15
- **Zależności:** TASK-020, TASK-023
- **Realizuje:** Sekcje: 17.3 · NFR: NFR-05
- **Opis:** OpenTelemetry (ślady `trace_id` przez api → praca w tle → wywołania modeli), Prometheus (wszystkie metryki z 17.3 — w tym te dodawane w zadaniach domenowych), logi JSON z maskowaniem, reguły alertów (`ALERT_KALIBRACJI`, `ALERT_PULA_DIAGNOSTYCZNA`, `ALERT_RODO`, `ALERT_WERYFIKACJE`, martwe prace), pulpity Grafana (NFR-01, NFR-03, NFR-04, kolejki).
- **Definition of Done:**
  - [ ] Test kontraktowy: każda metryka z 17.3 jest zarejestrowana w `/metrics`.
  - [ ] Pulpity i reguły alertów jako kod w repo.
- **Status:** TODO

### TASK-132 — Weryfikacja budżetu czasowego NFR-01 (scenariusze S-1 … S-7)

- **Faza:** F15
- **Zależności:** TASK-077, TASK-086, TASK-131
- **Realizuje:** Sekcje: 17.2 · NFR: NFR-01 · Decyzje PO: D-2 · Scenariusze NFR-01: S-1, S-2, S-3, S-4, S-5, S-6, S-7
- **Opis:** Zestaw testów symulacyjnych odtwarzających tabelę 17.2 (S-1 … S-7, w tym warianty b) na atrapach z rozkładami opóźnień; raport zgodności z SLO (≥ 99% `czas_systemowy_ms ≤ 28 000`, p95 ≤ 20 s / ≤ 23 s, T-6 ≤ 0,2%).
- **Definition of Done:**
  - [ ] Raport porównujący wynik symulacji z kolumnami tabeli 17.2 (odchyłka ≤ 5%).
- **Status:** TODO

### TASK-133 — Pojemność i skalowanie (17.4)

- **Faza:** F15
- **Zależności:** TASK-020, TASK-041
- **Realizuje:** Sekcje: 17.4 · Założenia: A-03
- **Opis:** Weryfikacja rachunku pojemności 17.4 (pule, połączenia PostgreSQL, limity dostawców, szczyt „koniec lekcji”) i konfiguracja liczby replik; dokumentacja progów skalowania (≈ 20× A-03).
- **Definition of Done:**
  - [ ] Tabela 17.4 odtworzona skryptem z parametrów `lemat/konfiguracja.py`.
- **Status:** TODO

### TASK-134 — Wdrożenie produkcyjne: 2 VM, Caddy, zarządzany PostgreSQL, wdrożenie kroczące

- **Faza:** F15
- **Zależności:** TASK-005, TASK-119, TASK-131
- **Realizuje:** Sekcje: 17, 17.5
- **Opis:** Konfiguracja środowisk `testowe` i `produkcyjne` (2 VM w UE, load balancer z TLS passthrough, Caddy, zarządzany PostgreSQL 16 z HA i PITR 14 dni, magazyn UE bez wersjonowania, bucket Object Lock 60 dni), migracje przed procesami, wdrożenie kroczące VM-2 → zdrowie → VM-1, runbook PITR z `ponow_zdarzenia_rodo`.
- **Definition of Done:**
  - [ ] Próbne wdrożenie na `testowe` z rollbackiem; runbook przetestowany (G-6).
- **Status:** TODO

### TASK-135 — Śledzenie wymagań: testy akceptacyjne FR/NFR i rodzaje zadań

- **Faza:** F15
- **Zależności:** TASK-103, TASK-130
- **Realizuje:** Sekcje: 9, 18, 18.1, 18.2, 18.3
- **Opis:** Tabela testów akceptacyjnych: każde FR-01 … FR-36 i NFR-01 … NFR-10 wskazuje test (pytest/Playwright/bramka); trzy rodzaje zadań (18.3) mają test przepływu end-to-end.
- **Definition of Done:**
  - [ ] Plik `tests/akceptacja/MACIERZ.md` (lub marker pytest `@pytest.mark.wymaganie('FR-xx')`) i raport w CI bez luk.
- **Status:** TODO

### TASK-136 — Monitorowanie ryzyk i decyzji w trakcie implementacji

- **Faza:** F15
- **Zależności:** TASK-001
- **Realizuje:** Sekcje: 21 · Decyzje PO: D-1, D-3, D-7 · Ryzyka: R-03, R-11
- **Opis:** Przegląd rejestru ryzyk R-01 … R-22 na koniec każdej fazy: status mitygacji (zadania), nowe ryzyka do `HANDOFF.md`; potwierdzenie, że opcje domyślne D-x są zaimplementowane w sposób przełączalny (zmiana dotyczy reguł w `lemat.ocenianie`, `lemat.weryfikacja`, `lemat.konta` i konfiguracji — R-11).
- **Definition of Done:**
  - [ ] Sekcja „Ryzyka” w `HANDOFF.md` aktualna po każdej fazie.
- **Status:** TODO

### TASK-137 — Bramka G-0: akceptacja właściciela produktu i słownika (PZS-1)

- **Faza:** F15
- **Zależności:** TASK-001
- **Realizuje:** Sekcje: 1.2, 23.4, 17.6 · Bramki: G-0 · Założenia: A-11, A-12, A-14, A-15 · Decyzje PO: D-1, D-2, D-3, D-4, D-5, D-6, D-7, D-8, D-9, D-10, D-11, D-12, D-13, D-14, D-15, D-16, D-17 · Zasady nadrzędne: Z-6 · PZS-1 (część B): B-1, B-2, B-3, B-4, B-5, B-6, B-7, B-8, B-9
- **Opis:** Przygotowanie pakietu decyzyjnego dla właściciela produktu: D-1 … D-17 z opcjami domyślnymi i alternatywami (23.4), PZS-1 (86 pojęć części A, B-1 … B-9, część C), A-11, A-12, A-14, A-15. Po akceptacji: jeden commit dopisujący PZS-1 do `docs/glossary.md` (wyłącznie za zgodą właściciela słownika) i ewentualne zmiany nazw w kodzie.
- **Definition of Done:**
  - [ ] Protokół decyzji (data, osoba, wybrana opcja dla każdego D-x) dołączony do `HANDOFF.md`.
  - [ ] Każda decyzja inna niż domyślna ma utworzone zadanie zmian w `TASKS.md`.
- **Status:** TODO

### TASK-138 — Bramki G-1 i G-2: ZT-1 i jakość oceniania/rozpoznawania

- **Faza:** F15
- **Zależności:** TASK-078, TASK-081, TASK-105
- **Realizuje:** Sekcje: 17.6, 11.5.1, 11.8 · Bramki: G-1, G-2 · NFR: NFR-02, NFR-04 · Założenia: A-09
- **Opis:** Przygotowanie zbioru ZT-1 (≥ 700 obliczeniowych i ≥ 350 dowodów, 2 niezależne Oceny Nauczycieli + trzecia przy rozbieżności, ≥ 150 kartek z imieniem i nazwiskiem, w tym przypadki krótkich nazw i nazw-terminów), Transkrypcje referencyjne, uruchomienie `EWALUUJ_ZT1` dla Jev i modelu zapasowego, testy syntetyczne Sprawdzenia zgodności poprawek (200 + 200 przypadków).
- **Definition of Done:**
  - [ ] Raport G-1 (a)–(c) i G-2 dla obu modeli decyzyjnych z decyzją: próg albo tryb pełnej weryfikacji (D-6).
- **Status:** TODO

### TASK-139 — Bramka G-3: test obciążeniowy

- **Faza:** F15
- **Zależności:** TASK-132, TASK-133, TASK-134
- **Realizuje:** Sekcje: 17.6, 17.4 · Bramki: G-3
- **Opis:** Scenariusze (a) 600 Rozwiązań/h przez 30 min, (b) „koniec lekcji” 90 przesłań w 60 s + 90 zatwierdzeń w 120 s, (c) 3 repliki workera + dołączenie 4. w szczycie (0 × `429`, ≤ 74 wywołań Jev w oknie 1 s, Σ przydziałów ≤ 40/s).
- **Definition of Done:**
  - [ ] Raport z p95 czasu systemowego ≤ 20 s, ≥ 99% ≤ 28 s, 0 przypadków T-6.
- **Status:** TODO

### TASK-140 — Bramka G-4: test chaosu NFR-07

- **Faza:** F15
- **Zależności:** TASK-044, TASK-139
- **Realizuje:** Sekcje: 17.6, 13.2 · Bramki: G-4 · NFR: NFR-07
- **Opis:** Przy 300 Rozwiązaniach/h: (a) Jev timeout 100% przy obwodzie zamkniętym, (b) natychmiastowe 503, (c) degradacja p95 2,5 s, (d) 30% `429` przez 10 min bez otwarcia obwodu; wcześniej ustalenie `parametry.tryb_jev` (test wsadu 32 pytań) i tolerancji limitu (seria 74 wywołań w 1 s → `B_całk` 34 albo 10).
- **Definition of Done:**
  - [ ] Raport: w każdym scenariuszu ≥ 99% Ocen ≤ 28 s i 0 × T-6; zapisane `tryb_jev` i `B_całk`.
- **Status:** TODO

### TASK-141 — Bramka G-5: gotowość treści

- **Faza:** F15
- **Zależności:** TASK-053, TASK-057, TASK-109
- **Realizuje:** Sekcje: 17.6 · Bramki: G-5 · FR: FR-06, FR-07
- **Opis:** Wszystkie Umiejętności obu poziomów mają opublikowaną Wkładkę i ≥ 3 Zadania `GLOBALNY` na Poziomy trudności 2–4; pula diagnostyczna 72 (`PODSTAWOWY`) i 78 (`ROZSZERZONY`) Zadań wg `GET /admin/pula-diagnostyczna`.
- **Definition of Done:**
  - [ ] Raport z zapytania kontrolnego bez braków.
- **Status:** TODO

### TASK-142 — Bramka G-6: przegląd RODO i test procedury PITR

- **Faza:** F15
- **Zależności:** TASK-119, TASK-134
- **Realizuje:** Sekcje: 17.6, 16.4 · Bramki: G-6 · NFR: NFR-09 · Założenia: A-07
- **Opis:** Umowy powierzenia z retencją A-07, polityka prywatności (kolejka globalna, pseudonimizacja), umowy poufności weryfikatorów, rejestr czynności przetwarzania, test `ponow_zdarzenia_rodo` w środowisku testowym we wszystkich scenariuszach G-6 (w tym (a3) z `LEMAT_TEST_ZATRZYMAJ_USUN_KONTO_PO_KROKU ∈ {1, 2, 3, 4}`).
- **Definition of Done:**
  - [ ] Protokół G-6 podpisany przez IOD/właściciela produktu; raport testów PITR.
- **Status:** TODO

### TASK-143 — Bramka G-7: pula weryfikatorów globalnych

- **Faza:** F15
- **Zależności:** TASK-096, TASK-099
- **Realizuje:** Sekcje: 17.6, 17.7 · Bramki: G-7 · Założenia: A-14
- **Opis:** Liczba aktywnych weryfikatorów globalnych ≥ `wymagani_weryfikatorzy_globalni` (start 18), 5 × Σ deklarowanych minut ≥ 1,3 × tygodniowa prognoza kolejki `GLOBALNA` z eskalacjami; testy eskalacji, alertu pustej puli i kontroli przyjęć; w przeciwnym razie start z `LEMAT_REJESTRACJA_BEZ_KLASY = false` za akceptacją G-0.
- **Definition of Done:**
  - [ ] Raport z `GET /admin/obciazenie-weryfikacji` i wynik testów eskalacji.
- **Status:** TODO

## Macierz pokrycia (element architektury → TASK-ID)

Macierz jest kompletna: każdy element z poniższych kategorii, wyodrębniony automatycznie z `docs/ARCHITECTURE_DETAILED.md` v2.4 i `docs/requirements.md`, ma co najmniej jedno zadanie. Metoda sprawdzenia — na końcu pliku.

| Kategoria | Liczba elementów | Pokryte |
|---|---|---|
| Sekcje | 149 | 149 |
| ADR | 47 | 47 |
| DDL — tabele | 59 | 59 |
| DDL — triggery | 6 | 6 |
| DDL — funkcje | 1 | 1 |
| DDL — rozszerzenia | 2 | 2 |
| DDL — typy wyliczeniowe | 43 | 43 |
| DDL — indeksy | 50 | 50 |
| API | 100 | 100 |
| Agenci | 5 | 5 |
| Prace w tle | 29 | 29 |
| Zdarzenia domenowe | 16 | 16 |
| Bramki | 8 | 8 |
| Scenariusze wyścigów | 40 | 40 |
| Macierz błędów | 38 | 38 |
| Pliki testowe/danych | 12 | 12 |
| FR | 36 | 36 |
| NFR | 10 | 10 |
| Założenia | 15 | 15 |
| Decyzje PO | 17 | 17 |
| Ryzyka | 22 | 22 |
| Zasady nadrzędne | 6 | 6 |
| Scenariusze NFR-01 | 7 | 7 |
| Reguły trasowania | 7 | 7 |
| Niespójności | 6 | 6 |
| Sprawdzenia Walidacji | 10 | 10 |
| Pytania do modelu decyzyjnego | 13 | 13 |
| Reguły rekomendacji | 12 | 12 |
| PZS-1 (część B) | 9 | 9 |
| **Razem** | **775** | **775** |

### Sekcje

| Sekcja | Tytuł | Zadania |
|---|---|---|
| 1 | Cel, zakres i zasady nadrzędne | TASK-001 |
| 1.1 | Cel systemu | TASK-001 |
| 1.2 | Zasady nadrzędne | TASK-137 |
| 1.3 | Zakres i poza zakresem | TASK-001 |
| 2 | Założenia | TASK-001 |
| 3 | Język wszechobecny | TASK-006 |
| 3.1 | Reguły nazewnictwa | TASK-006 |
| 3.2 | Pojęcia słownika → identyfikatory | TASK-006 |
| 3.3 | Pojęcia uzupełniające | TASK-006 |
| 3.4 | Propozycja zmiany słownika PZS-1 | TASK-006, TASK-024 |
| 4 | Widok kontekstu systemu | TASK-001 |
| 5 | Widok kontenerów i stos technologiczny | TASK-002 |
| 5.1 | Diagram kontenerów | TASK-005 |
| 5.2 | Stos technologiczny | TASK-004 |
| 6 | Moduły, agenci i porty | TASK-002 |
| 6.1 | Diagram komponentów | TASK-002 |
| 6.2 | Odpowiedzialności modułów | TASK-004 |
| 6.3 | Orkiestrator | TASK-020 |
| 7 | Model danych | TASK-007 |
| 7.1 | Diagram ERD (encje rdzenia domeny) | TASK-011 |
| 7.2 | Konwencje schematu | TASK-007 |
| 7.3 | Pełne DDL (PostgreSQL 16) | TASK-007 |
| 7.4 | Dane referencyjne: Działy (A-04) | TASK-012 |
| 7.5 | Schematy pól JSONB | TASK-011 |
| 7.5.1 | `zadanie_wersja.opcje_zamkniete` | TASK-011, TASK-052 |
| 7.5.2 | `zadanie_wersja.rozwiazanie_wzorcowe` | TASK-011, TASK-052 |
| 7.5.3 | `zadanie_wersja.wynik_koncowy_wzorcowy` | TASK-011, TASK-052 |
| 7.5.4 | `weryfikacja.zgloszenie` | TASK-017, TASK-098 |
| 8 | Maszyny stanów | TASK-019 |
| 8.1 | Rozwiązanie | TASK-074, TASK-080 |
| 8.2 | Ocena | TASK-088, TASK-094 |
| 8.3 | Weryfikacja | TASK-093, TASK-094, TASK-096 |
| 8.4 | Zadanie | TASK-054 |
| 8.5 | Element Ścieżki nauki | TASK-070 |
| 8.6 | Wynik zadania (wersja 2.1) | TASK-062 |
| 9 | Przepływy | TASK-135 |
| 9.1 | Rejestracja, poziom matury, Test diagnostyczny (FR-01, FR-02, FR-03) | TASK-046 |
| 9.2 | Rekomendacja kolejnego elementu Ścieżki nauki (FR-05 … FR-10) | TASK-069 |
| 9.3 | Zadanie zamknięte (FR-15, FR-29) | TASK-073 |
| 9.4 | Przesłanie Rozwiązania i Transkrypcja (FR-16, FR-17, FR-18) | TASK-074, TASK-080 |
| 9.5 | Ocena Rozwiązania (FR-19 … FR-29, NFR-01, NFR-05) | TASK-086 |
| 9.6 | Weryfikacja przez Nauczyciela (FR-33, FR-34) | TASK-091 |
| 9.6.1 | Kolejka, podjęcie, przedłużenie, szkic | TASK-093 |
| 9.6.2 | Rozstrzygnięcie | TASK-094 |
| 9.6.3 | Korekta Oceny z poziomu Rozwiązania (FR-34) | TASK-095 |
| 9.7 | Generowanie i Walidacja Zadania (FR-11 … FR-14) | TASK-106 |
| 9.8 | Kalibracja i Próg pewności (FR-34, NFR-03, NFR-04) | TASK-100, TASK-103 |
| 9.9 | Przełączenie na model zapasowy (NFR-07) | TASK-044 |
| 9.10 | Retencja zdjęć (NFR-09) | TASK-114 |
| 9.11 | Import Arkusza CKE (FR-35, FR-36) | TASK-053 |
| 9.12 | Zgłoszenie błędu odczytu przez Ucznia (FR-28) | TASK-098 |
| 9.13 | Ponowna ocena po błędzie przetwarzania (`PONOW_OCENE`) | TASK-092 |
| 10 | Agenci — kontrakty, algorytmy, pytania do Jev | TASK-067 |
| 10.1 | Agent rekomendujący | TASK-067 |
| 10.1.1 | Kontrakt | TASK-067 |
| 10.1.2 | Polityka POLITYKA_ELO (domyślna) | TASK-067 |
| 10.1.3 | Model Profilu ucznia (Elo per Umiejętność) — ADR-010 | TASK-063, TASK-067 |
| 10.1.4 | Migawka cech (`rekomendacja.cechy`) | TASK-067 |
| 10.1.5 | Szablony uzasadnień (FR-10, NFR-05) | TASK-067 |
| 10.1.6 | Strategia eksperymentalna JEV_CHOICE (grupa eksperymentu) | TASK-071 |
| 10.1.7 | Port PredyktorSukcesu i model drzewiasty | TASK-072 |
| 10.2 | Agent generujący | TASK-106 |
| 10.2.1 | Kontrakt | TASK-106 |
| 10.2.2 | Sprawdzenia Walidacji (FR-13) | TASK-107 |
| 10.2.3 | Schemat `walidacja_zadania.sprawdzenia` | TASK-107 |
| 10.3 | Agent rozpoznający | TASK-077 |
| 10.3.1 | Kontrakt i przetwarzanie | TASK-077 |
| 10.3.2 | Schemat kroku (`krok_transkrypcji`, wyjście modelu) | TASK-077 |
| 10.3.3 | Sprawdzenie zgodności poprawek (ADR-036) | TASK-081 |
| 10.4 | Agent oceniający | TASK-082 |
| 10.4.1 | Kontrakt | TASK-082 |
| 10.4.2 | Stan przekazywany do modelu decyzyjnego | TASK-082 |
| 10.4.3 | Katalog pytań do modelu decyzyjnego | TASK-082 |
| 10.4.4 | Plan pytań Oceny (ADR-026) | TASK-082 |
| 10.4.5 | Opinia pomocnicza (architecture.md: „druga ocena przez mocniejszy LLM”) | TASK-090 |
| 10.5 | Agent wyjaśniający | TASK-089 |
| 10.5.1 | Kontrakt | TASK-089 |
| 10.5.2 | Zasady promptu | TASK-089 |
| 10.5.3 | Strażnik wycieku rozwiązania (FR-27) | TASK-089 |
| 11 | Reguły decyzyjne oceniania i kierowania do Weryfikacji | TASK-083 |
| 11.1 | Punkty i błędy (FR-19, FR-20, FR-21, FR-27, FR-28) | TASK-083 |
| 11.2 | Pewność oceny (FR-22, NFR-04) | TASK-084 |
| 11.2.1 | Pewność surowa i niespójności | TASK-083 |
| 11.2.2 | Kalibracja | TASK-101 |
| 11.3 | Trasowanie (FR-23 … FR-26, FR-28, FR-33) | TASK-085 |
| 11.4 | Kolejka weryfikacji | TASK-091 |
| 11.4.1 | Przydział | TASK-085, TASK-091 |
| 11.4.2 | Dostępność i blokady | TASK-091 |
| 11.4.3 | Eskalacja, SLA i kontrola przyjęć (ADR-021, ADR-033) | TASK-096 |
| 11.4.4 | Pseudonimizacja w kolejce globalnej (ADR-031) | TASK-097 |
| 11.5 | Wybór Progu pewności (FR-34, NFR-03, NFR-04) — ADR-007 | TASK-100 |
| 11.5.1 | Analiza mocy i ZT-1 | TASK-102, TASK-138 |
| 11.5.2 | Populacja i źródło Próbki kalibracyjnej | TASK-100 |
| 11.5.3 | Algorytm | TASK-102, TASK-103 |
| 11.6 | Zgłoszenie błędu odczytu przez Ucznia (FR-28, ADR-022) | TASK-098 |
| 11.7 | Uzasadnienie Oceny (słownik: „Ocena … wraz z uzasadnieniem”) | TASK-087 |
| 11.8 | Skuteczność wykrywania błędów odczytu (FR-28) | TASK-098, TASK-138 |
| 12 | Kontrakty API (REST) | TASK-023 |
| 12.1 | Konwencje | TASK-023 |
| 12.2 | Model błędu (RFC 9457) | TASK-023 |
| 12.3 | Katalog endpointów | TASK-023 |
| 12.4 | Typy DTO (TypeScript, źródło: modele Pydantic) | TASK-023 |
| 12.5 | Reguły walidacji wejścia (wybrane) | TASK-052, TASK-074, TASK-080 |
| 12.6 | Przykład: odpowiedź `GET /rozwiazania/{id}` z Oceną wstępną wstrzymaną (rola U) | TASK-088 |
| 12.7 | Limity szybkości | TASK-026 |
| 12.8 | Uprawnienia na poziomie obiektów | TASK-027 |
| 12.9 | Format importu `ArkuszImportV1` (FR-35) | TASK-053 |
| 13 | Kontrakty wewnętrzne (porty) i prace w tle | TASK-020 |
| 13.1 | Porty modeli (NFR-06) | TASK-036 |
| 13.2 | Kontrakty adapterów decyzyjnych | TASK-040, TASK-086, TASK-140 |
| 13.3 | Katalog prac w tle | TASK-020 |
| 13.4 | Mechanika prac w tle i pule workera | TASK-020 |
| 13.5 | Harmonogram (scheduler) | TASK-022, TASK-118 |
| 13.6 | Zdarzenia domenowe (w procesie, zapis w tej samej transakcji) | TASK-021 |
| 14 | Obsługa błędów | TASK-086 |
| 14.1 | Macierz błędów | TASK-023 |
| 14.2 | Terminy oceny i reguła T-6 | TASK-086 |
| 15 | Współbieżność i spójność | TASK-019 |
| 15.1 | Zasady | TASK-019 |
| 15.2 | Scenariusze wyścigów | TASK-019 |
| 16 | Bezpieczeństwo i RODO | TASK-050 |
| 16.1 | Uwierzytelnianie i sesje (ADR-020) | TASK-045 |
| 16.2 | Ochrona aplikacji | TASK-050 |
| 16.3 | Kategorie danych osobowych | TASK-050, TASK-097 |
| 16.4 | Retencja i usuwanie (NFR-09) | TASK-111, TASK-112, TASK-113, TASK-114, TASK-115, TASK-116, TASK-119, TASK-120, TASK-142 |
| 17 | Wymagania niefunkcjonalne — realizacja, budżety, obciążenie, obserwowalność, wdrożenie | TASK-134 |
| 17.1 | Realizacja NFR | TASK-130 |
| 17.2 | Budżet czasowy (NFR-01) | TASK-132 |
| 17.3 | Obserwowalność | TASK-131 |
| 17.4 | Pojemność (A-03) | TASK-133, TASK-139 |
| 17.5 | Wdrożenie | TASK-005, TASK-134 |
| 17.6 | Bramki jakości przed startem produkcyjnym | TASK-105, TASK-137, TASK-138, TASK-139, TASK-140, TASK-141, TASK-142, TASK-143 |
| 17.7 | Model obciążenia Nauczycieli (FR-33, R-02, C1-02, C1-10, C2-12) | TASK-099, TASK-143 |
| 18 | Mapowanie wymagań na komponenty, przepływy i API | TASK-135 |
| 18.1 | Wymagania funkcjonalne | TASK-135 |
| 18.2 | Wymagania niefunkcjonalne | TASK-135 |
| 18.3 | Rodzaje zadań | TASK-135 |
| 19 | Rejestr decyzji architektonicznych (ADR) | TASK-001 |
| 20 | Odpowiedzi na otwarte pytania z architektury wstępnej | TASK-001 |
| 21 | Ryzyka | TASK-136 |
| 22 | Parametry systemu (wartości domyślne) | TASK-003 |
| 23 | Changelog iteracji | TASK-001 |
| 23.1 | Iteracja 1 (2026-10-31) | TASK-001 |
| 23.2 | Iteracja 2 (2026-10-31, wersja 2.0) | TASK-001 |
| 23.3 | Iteracja 3 (2026-11-01, wersja 2.1) — weryfikacja zamknięcia uwag C1 | TASK-001 |
| 23.4 | Decyzje, które wymagają właściciela produktu (bramka G-0) | TASK-137 |
| 23.5 | Iteracja 4 (2026-11-01, wersja 2.2) — zamknięcie uwag C1-21 i C2-01 … C2-16 | TASK-001 |
| 23.6 | Iteracja 5 (2026-11-01, wersja 2.3) — zamknięcie uwag C3-01 … C3-09 | TASK-001 |
| 23.7 | Iteracja 6 (2026-11-01, wersja 2.4) — zamknięcie uwag C4-01 … C4-06 | TASK-001 |

### ADR

| ADR | Tytuł | Zadania |
|---|---|---|
| ADR-001 | Modularny monolit z osobnymi procesami `api`, `worker`, `scheduler` — Przyjęta | TASK-002 |
| ADR-002 | PostgreSQL jako jedyne źródło prawdy i mechanizm prac w tle — Zmieniona (it. 2) | TASK-020 |
| ADR-003 | Porty i adaptery dla modeli AI, konfiguracja w bazie — Przyjęta | TASK-004 |
| ADR-004 | Agent rozpoznający oparty na multimodalnym LLM ze strukturyzowanym wyjściem — Zmieniona (… | TASK-042 |
| ADR-005 | Ocena per kryterium osobnymi pytaniami — Zmieniona (it. 2) | TASK-082 |
| ADR-006 | Pewność oceny = minimum prawdopodobieństw + kalibracja izotoniczna — Zmieniona (it. 2) | TASK-084 |
| ADR-007 | Próg pewności z górnej granicy Wilsona, Kontroli losowej z wagami IPW i analizy mocy — Zm… | TASK-102 |
| ADR-008 | Opinia pomocnicza LLM nie zmienia trasy Oceny — Przyjęta | TASK-085 |
| ADR-009 | Model zapasowy: LLM ze strukturyzowaną odpowiedzią + circuit breaker + osobna kalibracja… | TASK-043 |
| ADR-010 | Profil ucznia jako model Elo per Umiejętność; model drzewiasty jako eksperyment — Zmienio… | TASK-063 |
| ADR-011 | Uzasadnienie rekomendacji z szablonów, nie z LLM — Zmieniona (it. 2) | TASK-067 |
| ADR-012 | Wersjonowanie Zadań i niezmienność Ocen — Zmieniona (it. 2) | TASK-052 |
| ADR-013 | SymPy w piaskownicy procesowej — Przyjęta | TASK-032 |
| ADR-014 | Odpytywanie (polling) zamiast WebSocket/SSE — Zmieniona (it. 2) | TASK-121 |
| ADR-015 | Publikacja Zadań wygenerowanych po Walidacji; Wkładki i dowody tylko po przeglądzie — Zmi… | TASK-054 |
| ADR-016 | Identyfikatory domenowe po polsku, zgodne ze słownikiem — Zmieniona (it. 2) | TASK-006 |
| ADR-017 | Obowiązkowe zatwierdzenie Transkrypcji przez Ucznia — Zmieniona (it. 2) | TASK-080 |
| ADR-018 | Wynik zadania z Oceny wstępnej z wagą 0,5; bez Wyniku przy podejrzeniu błędu odczytu — Zm… | TASK-062 |
| ADR-019 | Retencja zdjęć sterowana przejściami stanów, maks. 90 dni — Zmieniona (it. 2) | TASK-114 |
| ADR-020 | JWT krótkotrwały + rotowany refresh token w cookie — Przyjęta | TASK-045 |
| ADR-021 | Kolejka weryfikacji globalna dla Uczniów bez Klasy — Zmieniona (it. 2), G-0 | TASK-096 |
| ADR-022 | Ocena wstępna wstrzymana przy podejrzeniu błędu odczytu i zgłoszenie Ucznia — Nowa (it. 2… | TASK-085, TASK-098 |
| ADR-023 | Rekomendacja synchroniczna w procesie `api` przez fasadę Orkiestratora — Nowa (it. 2) | TASK-069 |
| ADR-024 | Brak wpisu ręcznego Rozwiązania — Nowa (it. 2), G-0 | TASK-074 |
| ADR-025 | NFR-01 jako czas systemowy; termin miękki i twardy; Wskazówka asynchroniczna — Nowa (it.… | TASK-086 |
| ADR-026 | Plan pytań z zależnościami jako kontrakt portu `ModelDecyzyjny` — Nowa (it. 2) | TASK-036, TASK-040 |
| ADR-027 | Zgodność Oceny = punkty + pierwszy błędny krok + brakujący krok — Nowa (it. 2), G-0 | TASK-100 |
| ADR-028 | Zasięg Zadań i recenzent treści — Nowa (it. 2), G-0 | TASK-054 |
| ADR-029 | Kalibracja Poziomu trudności w osobnej tabeli z atomową aktualizacją — Nowa (it. 2) | TASK-058 |
| ADR-030 | Seria nauki po Wkładce teoretycznej — Nowa (it. 2) | TASK-068 |
| ADR-031 | Pseudonimizacja i maskowanie zdjęć w kolejce globalnej — Nowa (it. 2), G-0 | TASK-079, TASK-097 |
| ADR-032 | Jeden kontekst na próbę, wielokrotne próby i ujawnianie klucza po terminie — Nowa (it. 2) | TASK-061, TASK-073 |
| ADR-033 | Weryfikacja: SLA, zwalnianie wygasłych blokad, przedłużanie i szkice — Nowa (it. 2) | TASK-093 |
| ADR-034 | Pula weryfikatorów globalnych: Nauczyciele bez Klas + Nauczyciele współpracujący — Nowa (… | TASK-049 |
| ADR-035 | Kopie treningowe z rejestrem powiązań w bazie — Nowa (it. 3), G-0 | TASK-115 |
| ADR-036 | Poprawki Transkrypcji ograniczone i sprawdzane ze zdjęciem — Nowa (it. 4), G-0 | TASK-080 |
| ADR-037 | Wersja treści Weryfikacji oddzielona od blokady — Nowa (it. 4) | TASK-093, TASK-094 |
| ADR-038 | Przegląd Zadań zleconych przez Nauczyciela i Zgłoszenia błędu w Zadaniu — Nowa (it. 4), G… | TASK-055 |
| ADR-039 | Tryb wsadowy Jev i globalny ogranicznik szybkości — Nowa (it. 4) | TASK-041, TASK-086 |
| ADR-040 | Okres karencji usunięcia konta: procesy wstrzymane, nie anulowane — Nowa (it. 4) | TASK-111, TASK-112 |
| ADR-041 | Rejestr wszystkich Zdarzeń RODO w Object Lock przed transakcją — Nowa (it. 4) | TASK-110, TASK-118, TASK-119 |
| ADR-042 | Filtr danych osobowych w Transkrypcji — Nowa (it. 4) | TASK-078 |
| ADR-043 | SLA w dniach roboczych i tygodniowy model obciążenia z eskalacjami — Nowa (it. 4), G-0 | TASK-010, TASK-096 |
| ADR-044 | Usuwanie kont Nauczycieli i Administratorów — Nowa (it. 4) | TASK-113 |
| ADR-045 | Normalizacja zdjęć w piaskownicy procesowej `api` — Nowa (it. 4) | TASK-033 |
| ADR-046 | Bezpieczniki trafności i poprawek reagujące na dowód przekroczenia — Nowa (it. 5) | TASK-104 |
| ADR-047 | Sprzątanie osieroconych obiektów magazynu — Nowa (it. 5) | TASK-120 |

### DDL — tabele

| Element | Zadania |
|---|---|
| `uzytkownik` | TASK-009 |
| `uczen` | TASK-009 |
| `nauczyciel` | TASK-009 |
| `token_odswiezania` | TASK-009 |
| `proba_logowania` | TASK-009 |
| `zdarzenie_rodo` | TASK-009 |
| `dzien_wolny` | TASK-009 |
| `dzial` | TASK-011 |
| `umiejetnosc` | TASK-011 |
| `umiejetnosc_wymagana` | TASK-011 |
| `wkladka_teoretyczna` | TASK-011 |
| `arkusz_cke` | TASK-011 |
| `import_arkusza` | TASK-011 |
| `zlecenie_generowania` | TASK-011 |
| `zadanie` | TASK-011 |
| `kalibracja_trudnosci` | TASK-011 |
| `zadanie_wersja` | TASK-011 |
| `zadanie_umiejetnosc` | TASK-011 |
| `kryterium_oceniania` | TASK-011 |
| `zalacznik_zadania` | TASK-011 |
| `walidacja_zadania` | TASK-011 |
| `przeglad_zadania` | TASK-011 |
| `zgloszenie_bledu_zadania` | TASK-011 |
| `wstrzymanie_w_doborze` | TASK-011 |
| `klasa` | TASK-013 |
| `czlonkostwo_klasy` | TASK-013 |
| `zestaw_zadan` | TASK-013 |
| `pozycja_zestawu` | TASK-013 |
| `przypisanie` | TASK-013 |
| `przypisanie_uczen` | TASK-013 |
| `profil_ucznia` | TASK-014 |
| `opanowanie_umiejetnosci` | TASK-014 |
| `test_diagnostyczny` | TASK-014 |
| `pozycja_testu_diagnostycznego` | TASK-014 |
| `sciezka_nauki` | TASK-014 |
| `element_sciezki` | TASK-014 |
| `rekomendacja` | TASK-014 |
| `rozwiazanie` | TASK-015 |
| `zdjecie_rozwiazania` | TASK-015 |
| `kopia_treningowa` | TASK-015 |
| `transkrypcja` | TASK-015 |
| `krok_transkrypcji` | TASK-015 |
| `odpowiedz` | TASK-015 |
| `konfiguracja_modelu` | TASK-016 |
| `limit_szybkosci_modelu` | TASK-016 |
| `kalibrator_pewnosci` | TASK-016 |
| `prog_pewnosci` | TASK-016 |
| `stan_bezpiecznika` | TASK-016 |
| `decyzja_modelu` | TASK-016 |
| `ocena` | TASK-017 |
| `ocena_kryterium` | TASK-017 |
| `weryfikacja` | TASK-017 |
| `szkic_rozstrzygniecia` | TASK-017 |
| `probka_kalibracyjna` | TASK-017 |
| `wynik_zadania` | TASK-017 |
| `praca_w_tle` | TASK-018 |
| `klucz_idempotencji` | TASK-018 |
| `powiadomienie` | TASK-018 |
| `dziennik_audytu` | TASK-018, TASK-029 |

### DDL — triggery

| Element | Zadania |
|---|---|
| `trg_ocena_niezmienna` | TASK-017 |
| `trg_weryfikacja_wersja` | TASK-017 |
| `trg_umiejetnosc_glowna_w_dziale` | TASK-011 |
| `trg_suma_kryteriow` | TASK-011 |
| `trg_kontekst_proby` | TASK-015 |
| `trg_powody_poczatkowe` | TASK-017 |

### DDL — funkcje

| Element | Zadania |
|---|---|
| `dodaj_dni_robocze` | TASK-010 |

### DDL — rozszerzenia

| Element | Zadania |
|---|---|
| `citext` | TASK-007 |
| `pg_trgm` | TASK-007 |

### DDL — typy wyliczeniowe

| Element | Zadania |
|---|---|
| `rola_uzytkownika` | TASK-008 |
| `status_konta` | TASK-008 |
| `poziom_matury` | TASK-008 |
| `rodzaj_zadania` | TASK-008 |
| `forma_zamknieta` | TASK-008 |
| `pochodzenie_zadania` | TASK-008 |
| `zasieg_zadania` | TASK-008 |
| `status_zadania` | TASK-008 |
| `status_wkladki` | TASK-008 |
| `status_rozwiazania` | TASK-008 |
| `zrodlo_transkrypcji` | TASK-008 |
| `status_oceny` | TASK-008 |
| `zrodlo_oceny` | TASK-008 |
| `typ_bledu` | TASK-008 |
| `zrodlo_wskazowki` | TASK-008 |
| `powod_weryfikacji` | TASK-008 |
| `status_weryfikacji` | TASK-008 |
| `powod_anulowania` | TASK-008 |
| `typ_kolejki_weryfikacji` | TASK-008 |
| `typ_elementu_sciezki` | TASK-008 |
| `cel_elementu` | TASK-008 |
| `status_elementu_sciezki` | TASK-008 |
| `strategia_rekomendacji` | TASK-008 |
| `rola_modelu` | TASK-008 |
| `typ_pytania` | TASK-008 |
| `kontekst_decyzji` | TASK-008 |
| `zrodlo_wyniku` | TASK-008 |
| `zrodlo_probki` | TASK-008 |
| `zrodlo_progu` | TASK-008 |
| `zrodlo_kalibratora` | TASK-008 |
| `powod_uniewaznienia_wyniku` | TASK-008 |
| `status_pracy` | TASK-008 |
| `status_zlecenia` | TASK-008 |
| `status_importu` | TASK-008 |
| `status_testu` | TASK-008 |
| `decyzja_przegladu` | TASK-008 |
| `rezultat_walidacji` | TASK-008 |
| `inicjalizacja_opanowania` | TASK-008 |
| `status_zgloszenia_zadania` | TASK-008 |
| `typ_zdarzenia_rodo` | TASK-008 |
| `zrodlo_zdarzenia_rodo` | TASK-008 |
| `typ_bezpiecznika` | TASK-008 |
| `powod_eskalacji` | TASK-008 |

### DDL — indeksy

| Element | Zadania |
|---|---|
| `ix_proba_logowania_email` | TASK-009 |
| `ix_zdarzenie_rodo_uzytkownik` | TASK-009 |
| `ux_wkladka_opublikowana` | TASK-011 |
| `ix_wkladka_automatyczne_odrzucone` | TASK-011 |
| `ix_zlecenie_systemowe_dzien` | TASK-011 |
| `ix_zadanie_dobor` | TASK-011 |
| `ix_zadanie_autor` | TASK-011 |
| `ix_zadanie_diagnostyczne` | TASK-011 |
| `ix_kalibracja_poziom` | TASK-011 |
| `ix_zadanie_wersja_tresc_trgm` | TASK-011 |
| `ux_zadanie_umiejetnosc_glowna` | TASK-011 |
| `ux_zgloszenie_zadania_otwarte` | TASK-011 |
| `ix_zgloszenie_zadania_status` | TASK-011 |
| `ix_czlonkostwo_uczen` | TASK-013 |
| `ix_przypisanie_nauczyciel` | TASK-013 |
| `ix_przypisanie_uczen_uczen` | TASK-013 |
| `ix_opanowanie_powtorki` | TASK-014 |
| `ux_test_jeden` | TASK-014 |
| `ux_jeden_proponowany` | TASK-014 |
| `ix_element_wygasanie` | TASK-014 |
| `ix_rozwiazanie_uczen` | TASK-015 |
| `ix_rozwiazanie_status` | TASK-015 |
| `ux_rozwiazanie_element` | TASK-015 |
| `ux_rozwiazanie_przypisanie` | TASK-015 |
| `ix_zdjecie_do_usuniecia` | TASK-015 |
| `ix_kopia_do_usuniecia` | TASK-015 |
| `ix_kopia_uczen` | TASK-015 |
| `ux_odpowiedz_element` | TASK-015 |
| `ux_odpowiedz_test` | TASK-015 |
| `ux_odpowiedz_przypisanie` | TASK-015 |
| `ux_konfiguracja_aktywna` | TASK-016 |
| `ux_prog_aktywny` | TASK-016 |
| `ix_decyzja_kontekst` | TASK-016 |
| `ix_decyzja_uczen` | TASK-016 |
| `ux_ocena_aktualna` | TASK-017 |
| `ux_weryfikacja_otwarta` | TASK-017 |
| `ix_weryfikacja_kolejka` | TASK-017 |
| `ix_weryfikacja_blokady` | TASK-017 |
| `ux_probka_ocena_auto` | TASK-017 |
| `ix_probka_kalibracja` | TASK-017 |
| `ix_wynik_uczen` | TASK-017 |
| `ix_wynik_uczen_zadanie` | TASK-017 |
| `ux_wynik_ocena` | TASK-017 |
| `ux_wynik_odpowiedz` | TASK-017 |
| `ix_wynik_do_profilu` | TASK-017 |
| `ux_praca_dedup` | TASK-018 |
| `ux_praca_dedup_oczekujace` | TASK-018 |
| `ix_praca_pobieranie` | TASK-018 |
| `ix_praca_dzierzawa` | TASK-018 |
| `ix_powiadomienie_nieprzeczytane` | TASK-018 |

### API

| Element | Zadania |
|---|---|
| `POST /auth/rejestracja` | TASK-046 |
| `POST /auth/logowanie` | TASK-045 |
| `POST /auth/odswiezenie` | TASK-045 |
| `POST /auth/wylogowanie` | TASK-045 |
| `POST /auth/reset-hasla` | TASK-047 |
| `POST /auth/reset-hasla/potwierdzenie` | TASK-047 |
| `GET /uczniowie/ja` | TASK-048 |
| `PATCH /uczniowie/ja` | TASK-048 |
| `DELETE /uczniowie/ja` | TASK-111 |
| `DELETE /nauczyciele/ja` | TASK-113 |
| `GET /uczniowie/ja/eksport-danych` | TASK-117 |
| `DELETE /uczniowie/ja/klasy/{klasaId}` | TASK-059 |
| `POST /uczniowie/ja/test-diagnostyczny` | TASK-066 |
| `GET /testy-diagnostyczne/{id}` | TASK-066 |
| `POST /testy-diagnostyczne/{id}/pozycje/{pozycjaId}/odpowiedz` | TASK-066 |
| `POST /testy-diagnostyczne/{id}/zakonczenie` | TASK-066 |
| `POST /testy-diagnostyczne/{id}/pominiecie` | TASK-066 |
| `GET /uczniowie/{uczenId}/profil` | TASK-065 |
| `GET /uczniowie/ja/rekomendacje/nastepna` | TASK-069 |
| `GET /uczniowie/ja/sciezka-nauki` | TASK-069 |
| `POST /elementy-sciezki/{id}/rozpoczecie` | TASK-070 |
| `POST /elementy-sciezki/{id}/pominiecie` | TASK-070 |
| `POST /elementy-sciezki/{id}/przeczytanie` | TASK-070 |
| `GET /wkladki-teoretyczne` | TASK-056 |
| `GET /wkladki-teoretyczne/{id}` | TASK-056 |
| `PUT /wkladki-teoretyczne/{id}` | TASK-056 |
| `GET /dzialy` | TASK-051 |
| `GET /umiejetnosci` | TASK-051 |
| `GET /zadania/{id}` | TASK-052 |
| `POST /zadania/{id}/odpowiedzi` | TASK-073 |
| `POST /rozwiazania` | TASK-074 |
| `GET /rozwiazania` | TASK-074 |
| `GET /rozwiazania/{id}` | TASK-074 |
| `GET /rozwiazania/{id}/zdjecia/{zdjecieId}` | TASK-076 |
| `PUT /rozwiazania/{id}/transkrypcja` | TASK-080 |
| `POST /rozwiazania/{id}/zatwierdzenie` | TASK-080 |
| `POST /rozwiazania/{id}/zdjecia` | TASK-075 |
| `POST /rozwiazania/{id}/ponowienie-rozpoznania` | TASK-075 |
| `POST /rozwiazania/{id}/porzucenie` | TASK-075 |
| `POST /rozwiazania/{id}/zgloszenie-bledu-odczytu` | TASK-098 |
| `GET /rozwiazania/{id}/ocena` | TASK-088 |
| `POST /rozwiazania/{id}/korekty` | TASK-095 |
| `POST /klasy` | TASK-059 |
| `GET /klasy` | TASK-059 |
| `GET /klasy/{id}` | TASK-059 |
| `POST /klasy/{id}/kod-zaproszenia` | TASK-059 |
| `POST /klasy/{id}/archiwizacja` | TASK-059 |
| `POST /klasy/dolaczenie` | TASK-059 |
| `DELETE /klasy/{id}/uczniowie/{uczenId}` | TASK-059 |
| `GET /klasy/{id}/postepy` | TASK-064 |
| `GET /klasy/{id}/uczniowie/{uczenId}/rozwiazania` | TASK-064 |
| `POST /zestawy-zadan` | TASK-060 |
| `GET /zestawy-zadan` | TASK-060 |
| `GET /zestawy-zadan/{id}` | TASK-060 |
| `PUT /zestawy-zadan/{id}` | TASK-060 |
| `DELETE /zestawy-zadan/{id}` | TASK-060 |
| `POST /przypisania` | TASK-061 |
| `GET /przypisania` | TASK-061 |
| `GET /przypisania/{id}/postep` | TASK-061 |
| `GET /uczniowie/ja/przypisania` | TASK-061 |
| `GET /zadania` | TASK-052 |
| `POST /zadania` | TASK-052 |
| `POST /zadania/generowanie` | TASK-106 |
| `GET /zlecenia-generowania/{id}` | TASK-106 |
| `POST /zadania/{id}/przeglad` | TASK-054 |
| `POST /zadania/{id}/wycofanie` | TASK-054 |
| `POST /zadania/{id}/zgloszenia-bledu` | TASK-055 |
| `GET /zgloszenia-bledu-zadan` | TASK-055 |
| `POST /zgloszenia-bledu-zadan/{id}/rozpatrzenie` | TASK-055 |
| `GET /weryfikacje` | TASK-091 |
| `GET /weryfikacje/{id}` | TASK-091 |
| `POST /weryfikacje/{id}/podjecie` | TASK-093 |
| `POST /weryfikacje/{id}/przedluzenie` | TASK-093 |
| `PUT /weryfikacje/{id}/szkic` | TASK-093 |
| `POST /weryfikacje/{id}/zwolnienie` | TASK-093 |
| `POST /weryfikacje/{id}/rozstrzygniecie` | TASK-094 |
| `GET /powiadomienia` | TASK-030 |
| `POST /powiadomienia/{id}/przeczytanie` | TASK-030 |
| `POST /admin/arkusze-cke/importy` | TASK-053 |
| `GET /admin/arkusze-cke/importy/{id}` | TASK-053 |
| `PUT /admin/zadania/{id}/diagnostyczne` | TASK-057 |
| `GET /admin/pula-diagnostyczna` | TASK-057 |
| `GET /admin/uzytkownicy` | TASK-049 |
| `PATCH /admin/uzytkownicy/{id}` | TASK-049 |
| `POST /admin/uzytkownicy/{id}/przywrocenie` | TASK-112 |
| `POST /admin/uzytkownicy/{id}/usuniecie` | TASK-113 |
| `POST /admin/klasy/{id}/przekazanie` | TASK-113 |
| `GET /admin/konfiguracje-modeli` | TASK-037 |
| `POST /admin/konfiguracje-modeli` | TASK-037 |
| `POST /admin/konfiguracje-modeli/{id}/aktywacja` | TASK-037 |
| `POST /admin/konfiguracje-modeli/{id}/ewaluacja-zt1` | TASK-105 |
| `GET /admin/progi-pewnosci` | TASK-103 |
| `POST /admin/progi-pewnosci` | TASK-103 |
| `GET /admin/kalibracja` | TASK-103 |
| `GET /admin/bezpieczniki` | TASK-104 |
| `POST /admin/bezpieczniki/dezaktywacja` | TASK-104 |
| `GET /admin/obciazenie-weryfikacji` | TASK-099 |
| `POST /admin/wkladki-teoretyczne/szkice` | TASK-109 |
| `GET /admin/praca-w-tle` | TASK-035 |
| `POST /admin/praca-w-tle/{id}/ponowienie` | TASK-035 |

### Agenci

| Element | Zadania |
|---|---|
| Agent rekomendujący | TASK-067 |
| Agent generujący | TASK-106 |
| Agent rozpoznający | TASK-077 |
| Agent oceniający | TASK-082 |
| Agent wyjaśniający | TASK-089 |

### Prace w tle

| Element | Zadania |
|---|---|
| `ROZPOZNAJ_PISMO` | TASK-077 |
| `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` | TASK-081 |
| `OCEN_ROZWIAZANIE` | TASK-086 |
| `PONOW_OCENE` | TASK-092 |
| `NAPISZ_WSKAZOWKE` | TASK-089 |
| `AKTUALIZUJ_PROFIL` | TASK-063 |
| `OPINIA_POMOCNICZA` | TASK-090 |
| `UZUPELNIJ_PULE` | TASK-108 |
| `GENERUJ_ZADANIE` | TASK-106 |
| `WALIDUJ_ZADANIE` | TASK-107 |
| `GENERUJ_SZKIC_WKLADKI` | TASK-109 |
| `IMPORTUJ_ARKUSZ` | TASK-053 |
| `EWALUUJ_ZT1` | TASK-105 |
| `KALIBRUJ` | TASK-103 |
| `USUN_ZDJECIA` | TASK-114 |
| `ESKALUJ_WERYFIKACJE` | TASK-096 |
| `ZAMIATAJ_PROFILE` | TASK-063 |
| `SPRAWDZ_PULE_DIAGNOSTYCZNA` | TASK-057 |
| `UZGODNIJ_REJESTR_RODO` | TASK-118 |
| `PORZUC_NIEZATWIERDZONE` | TASK-075 |
| `WYGAS_ELEMENTY` | TASK-070 |
| `USUN_KONTO` | TASK-116 |
| `UTRZYMAJ_PARTYCJE` | TASK-034 |
| `WYCZYSC_DANE_TECHNICZNE` | TASK-034 |
| `WYSLIJ_EMAIL` | TASK-031 |
| `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` | TASK-115 |
| `USUN_KOPIE_TRENINGOWE` | TASK-115 |
| `SPRZATAJ_OSIEROCONE_OBIEKTY` | TASK-120 |
| `ZWOLNIJ_BLOKADY_WERYFIKACJI` | TASK-093 |

### Zdarzenia domenowe

| Element | Zadania |
|---|---|
| `RozwiazaniePrzeslane` | TASK-074 |
| `TranskrypcjaGotowa` | TASK-077 |
| `TranskrypcjaPoprawiona` | TASK-080 |
| `TranskrypcjaZatwierdzona` | TASK-080 |
| `OcenaWystawiona` | TASK-085 |
| `OcenaSkorygowana` | TASK-095 |
| `BladOdczytuZgloszony` | TASK-098 |
| `OdpowiedzZapisana` | TASK-073 |
| `TestDiagnostycznyZakonczony` | TASK-066 |
| `ZadanieOpublikowane` | TASK-054 |
| `ZadanieDoPrzegladu` | TASK-054 |
| `WkladkaDoPrzegladu` | TASK-056 |
| `PrzypisanieUtworzone` | TASK-061 |
| `ZgloszenieBleduZadaniaUtworzone` | TASK-055 |
| `KontoUsuwane` | TASK-111 |
| `KontoPrzywrocone` | TASK-112 |

### Bramki

| Element | Zadania |
|---|---|
| G-0 | TASK-137 |
| G-1 | TASK-138 |
| G-2 | TASK-138 |
| G-3 | TASK-139 |
| G-4 | TASK-140 |
| G-5 | TASK-141 |
| G-6 | TASK-142 |
| G-7 | TASK-143 |

### Scenariusze wyścigów

| Element | Zadania |
|---|---|
| C-01 | TASK-025 |
| C-02 | TASK-074 |
| C-03 | TASK-080 |
| C-04 | TASK-080 |
| C-05 | TASK-020 |
| C-06 | TASK-020 |
| C-07 | TASK-093 |
| C-08 | TASK-093 |
| C-09 | TASK-094 |
| C-10 | TASK-090 |
| C-11 | TASK-063 |
| C-12 | TASK-069 |
| C-13 | TASK-063 |
| C-14 | TASK-084 |
| C-15 | TASK-084 |
| C-16 | TASK-052 |
| C-17 | TASK-052 |
| C-18 | TASK-108 |
| C-19 | TASK-022 |
| C-20 | TASK-111 |
| C-21 | TASK-103 |
| C-22 | TASK-053 |
| C-23 | TASK-092 |
| C-24 | TASK-058 |
| C-25 | TASK-098 |
| C-26 | TASK-095 |
| C-27 | TASK-068 |
| C-28 | TASK-094 |
| C-29 | TASK-098 |
| C-30 | TASK-073 |
| C-31 | TASK-093 |
| C-32 | TASK-063 |
| C-33 | TASK-112 |
| C-34 | TASK-119 |
| C-35 | TASK-081 |
| C-36 | TASK-055 |
| C-37 | TASK-094 |
| C-38 | TASK-081 |
| C-39 | TASK-119 |
| C-40 | TASK-041 |

### Macierz błędów

| Element | Zadania |
|---|---|
| E-01 | TASK-074 |
| E-02 | TASK-028 |
| E-03 | TASK-077 |
| E-04 | TASK-077 |
| E-05 | TASK-042 |
| E-06 | TASK-077 |
| E-07 | TASK-077 |
| E-08 | TASK-040 |
| E-09 | TASK-043 |
| E-10 | TASK-032 |
| E-11 | TASK-089 |
| E-12 | TASK-020 |
| E-13 | TASK-019 |
| E-14 | TASK-023 |
| E-15 | TASK-023 |
| E-16 | TASK-093 |
| E-17 | TASK-107 |
| E-18 | TASK-053 |
| E-19 | TASK-067 |
| E-20 | TASK-067 |
| E-21 | TASK-103 |
| E-22 | TASK-114 |
| E-23 | TASK-031 |
| E-24 | TASK-020 |
| E-25 | TASK-023 |
| E-26 | TASK-067 |
| E-27 | TASK-071 |
| E-28 | TASK-096 |
| E-29 | TASK-103 |
| E-30 | TASK-108 |
| E-31 | TASK-080 |
| E-32 | TASK-081 |
| E-33 | TASK-041 |
| E-34 | TASK-057 |
| E-35 | TASK-033 |
| E-36 | TASK-110 |
| E-37 | TASK-078 |
| E-38 | TASK-120 |

### Pliki testowe/danych

| Element | Zadania |
|---|---|
| `lemat/rozpoznawanie/slownik_matematyczny.txt` | TASK-078 |
| `tests/test_slownik.py` | TASK-006 |
| `tests/slownik_wyjatki.yaml` | TASK-006 |
| `lemat/modele/schematy/*.json` | TASK-024, TASK-040 |
| `tests/slownik_tokeny.yaml` | TASK-006 |
| `src/lemat/tresci/dane/umiejetnosci.yaml` | TASK-012 |
| `tests/kalibracja/test_zrodlo_probki.py` | TASK-100 |
| `tests/kalibracja/symulacja_bezpiecznika.py` | TASK-104 |
| `tests/kalibracja/test_bezpiecznik.py` | TASK-104 |
| `tests/modele/test_ogranicznik.py` | TASK-041 |
| `tests/obciazenie/symulacja_ogranicznika.py` | TASK-041 |
| `lemat/konfiguracja.py` | TASK-003 |

### FR

| FR | Treść | Zadania |
|---|---|---|
| FR-01 | Uczeń może założyć konto i zalogować się. | TASK-046, TASK-122 |
| FR-02 | Uczeń wybiera poziom matury (podstawowy / rozszerzony) i termin egzaminu. | TASK-048, TASK-122 |
| FR-03 | Nowy uczeń rozwiązuje krótki test diagnostyczny, na podstawie którego powstaje początkowy… | TASK-066, TASK-122 |
| FR-04 | Uczeń widzi swój profil: poziom opanowania każdego działu i umiejętności. | TASK-065, TASK-123 |
| FR-05 | System proponuje uczniowi kolejne zadanie na podstawie jego profilu. | TASK-067, TASK-123 |
| FR-06 | Gdy umiejętność jest słabo opanowana, system proponuje wkładkę teoretyczną przed zadaniam… | TASK-056, TASK-067, TASK-141 |
| FR-07 | Trudność kolejnych zadań rośnie wraz z postępami ucznia. | TASK-063, TASK-067, TASK-141 |
| FR-08 | System okresowo przypomina zadania z działów już opanowanych (powtórki). | TASK-063, TASK-067 |
| FR-09 | Uczeń może samodzielnie wybrać dział, z którego chce ćwiczyć. | TASK-048, TASK-123 |
| FR-10 | System uzasadnia rekomendację (np. „3 z ostatnich 4 zadań z optymalizacji były błędne”). | TASK-067, TASK-123 |
| FR-11 | System generuje nowe zadanie dla wskazanej umiejętności i poziomu trudności. | TASK-106, TASK-127 |
| FR-12 | Wygenerowane zadanie zawiera treść, rozwiązanie wzorcowe i kryteria oceniania rozpisane n… | TASK-052 |
| FR-13 | Przed udostępnieniem zadanie jest walidowane (poprawność matematyczna, jednoznaczność, zg… | TASK-107 |
| FR-14 | Nauczyciel może przejrzeć, poprawić lub odrzucić wygenerowane zadanie. | TASK-054, TASK-127 |
| FR-15 | Uczeń rozwiązuje zadanie zamknięte, zaznaczając odpowiedź bezpośrednio na platformie. | TASK-073, TASK-123 |
| FR-16 | Rozwiązanie zadania otwartego uczeń przesyła jako zdjęcie (jedno lub kilka) kartki; treść… | TASK-074, TASK-124 |
| FR-17 | System odczytuje pismo odręczne, w tym wzory matematyczne, i dzieli transkrypcję na kolej… | TASK-077 |
| FR-18 | Uczeń widzi transkrypcję i może poprawić błędnie odczytany fragment przed oceną. | TASK-080, TASK-124 |
| FR-19 | System ocenia rozwiązanie zadania otwartego krok po kroku według kryteriów oceniania i pr… | TASK-083 |
| FR-20 | Poprawny wynik końcowy nie wystarcza do zaliczenia zadania – punkty za wynik są przyznawa… | TASK-083 |
| FR-21 | System sprawdza, czy każdy krok rozwiązania wynika z poprzedniego (np. czy przekształceni… | TASK-083 |
| FR-22 | System wyznacza pewność oceny. | TASK-084 |
| FR-23 | Gdy pewność przekracza próg, ocena jest wystawiana automatycznie. | TASK-085 |
| FR-24 | Gdy pewność jest poniżej progu, rozwiązanie trafia do weryfikacji nauczyciela, a uczeń wi… | TASK-085, TASK-124 |
| FR-25 | Gdy wynik końcowy jest poprawny, ale co najmniej jeden krok jest błędny lub go brakuje, r… | TASK-085 |
| FR-26 | Dla zadań z dowodem system wystawia ocenę wstępną i zawsze przekazuje rozwiązanie do wery… | TASK-085 |
| FR-27 | Ocena zawiera wskazanie pierwszego błędnego kroku, typ błędu (rachunkowy, merytoryczny, n… | TASK-083, TASK-124 |
| FR-28 | Błędy wynikające z niepoprawnego odczytu pisma nie obniżają oceny ucznia. | TASK-083, TASK-098, TASK-124 |
| FR-29 | Wynik oceny aktualizuje profil ucznia. | TASK-063, TASK-073 |
| FR-30 | Nauczyciel tworzy klasę i zaprasza do niej uczniów. | TASK-059, TASK-125 |
| FR-31 | Nauczyciel widzi postępy klasy i poszczególnych uczniów. | TASK-064, TASK-125 |
| FR-32 | Nauczyciel przypisuje zadania lub zestawy zadań. | TASK-061, TASK-125 |
| FR-33 | Nauczyciel ma kolejkę rozwiązań do weryfikacji, z oznaczeniem powodu (niska pewność, popr… | TASK-085, TASK-091, TASK-126 |
| FR-34 | Nauczyciel weryfikuje i koryguje oceny systemu; korekty są zapisywane do kalibracji progu… | TASK-094, TASK-103, TASK-126 |
| FR-35 | Administrator importuje zadania z arkuszy CKE wraz z kryteriami oceniania. | TASK-053, TASK-128 |
| FR-36 | Każde zadanie jest otagowane rodzajem, działem, umiejętnościami i poziomem trudności. | TASK-052 |

### NFR

| NFR | Treść | Zadania |
|---|---|---|
| NFR-01 | Ocena rozwiązania trwa nie dłużej niż 30 sekund. | TASK-077, TASK-086, TASK-132 |
| NFR-02 | Ocena automatyczna jest zgodna z oceną nauczyciela w co najmniej 90% przypadków (mierzone… | TASK-103, TASK-138 |
| NFR-03 | Odsetek ocen automatycznie zaliczonych, które okazały się błędne, nie przekracza 2%. | TASK-085, TASK-103, TASK-104 |
| NFR-04 | Pewność oceny jest skalibrowana – system mierzy, czy deklarowana pewność odpowiada rzeczy… | TASK-101, TASK-103, TASK-138 |
| NFR-05 | Każda ocena i rekomendacja jest możliwa do wyjaśnienia: zapisywane są decyzje modelu z pr… | TASK-038, TASK-131 |
| NFR-06 | Modele AI są wymienne – zmiana dostawcy modelu nie wymaga zmian w logice platformy. | TASK-004, TASK-036, TASK-040, TASK-128 |
| NFR-07 | Gdy model decyzyjny jest niedostępny, system przełącza się na model zapasowy (LLM ze stru… | TASK-044, TASK-140 |
| NFR-08 | Platforma działa w przeglądarce na telefonie i komputerze; zdjęcie można zrobić bezpośred… | TASK-074, TASK-121 |
| NFR-09 | Dane uczniów są przetwarzane zgodnie z RODO; zdjęcia są usuwane po określonym czasie. | TASK-078, TASK-097, TASK-111, TASK-113, TASK-114, TASK-115, TASK-129, TASK-142 |
| NFR-10 | Interfejs jest w języku polskim. | TASK-121 |

### Założenia

| Element | Zadania |
|---|---|
| A-01 | TASK-040 |
| A-02 | TASK-039 |
| A-03 | TASK-001, TASK-133 |
| A-04 | TASK-012 |
| A-05 | TASK-082 |
| A-06 | TASK-046 |
| A-07 | TASK-037, TASK-142 |
| A-08 | TASK-077 |
| A-09 | TASK-078, TASK-102, TASK-138 |
| A-10 | TASK-001 |
| A-11 | TASK-086, TASK-137 |
| A-12 | TASK-096, TASK-137 |
| A-13 | TASK-099 |
| A-14 | TASK-046, TASK-099, TASK-137, TASK-143 |
| A-15 | TASK-010, TASK-096, TASK-137 |

### Decyzje PO

| Element | Zadania |
|---|---|
| D-1 | TASK-136, TASK-137 |
| D-2 | TASK-132, TASK-137 |
| D-3 | TASK-136, TASK-137 |
| D-4 | TASK-099, TASK-137 |
| D-5 | TASK-096, TASK-137 |
| D-6 | TASK-102, TASK-137 |
| D-7 | TASK-136, TASK-137 |
| D-8 | TASK-115, TASK-137 |
| D-9 | TASK-046, TASK-137 |
| D-10 | TASK-080, TASK-137 |
| D-11 | TASK-054, TASK-137 |
| D-12 | TASK-137 |
| D-13 | TASK-096, TASK-137 |
| D-14 | TASK-066, TASK-137 |
| D-15 | TASK-112, TASK-137 |
| D-16 | TASK-104, TASK-137 |
| D-17 | TASK-099, TASK-137 |

### Ryzyka

| Element | Zadania |
|---|---|
| R-01 | TASK-043 |
| R-02 | TASK-099 |
| R-03 | TASK-136 |
| R-04 | TASK-053 |
| R-05 | TASK-042 |
| R-06 | TASK-050 |
| R-07 | TASK-100 |
| R-08 | TASK-037 |
| R-09 | TASK-066 |
| R-10 | TASK-102 |
| R-11 | TASK-136 |
| R-12 | TASK-099 |
| R-13 | TASK-098 |
| R-14 | TASK-081 |
| R-15 | TASK-096 |
| R-16 | TASK-119 |
| R-17 | TASK-041 |
| R-18 | TASK-055 |
| R-19 | TASK-078 |
| R-20 | TASK-104 |
| R-21 | TASK-041 |
| R-22 | TASK-104 |

### Zasady nadrzędne

| Element | Zadania |
|---|---|
| Z-1 | TASK-085 |
| Z-2 | TASK-001 |
| Z-3 | TASK-085, TASK-086 |
| Z-4 | TASK-038 |
| Z-5 | TASK-017 |
| Z-6 | TASK-006, TASK-137 |

### Scenariusze NFR-01

| Element | Zadania |
|---|---|
| S-1 | TASK-086, TASK-132 |
| S-2 | TASK-086, TASK-132 |
| S-3 | TASK-086, TASK-132 |
| S-4 | TASK-044, TASK-132 |
| S-5 | TASK-044, TASK-132 |
| S-6 | TASK-086, TASK-132 |
| S-7 | TASK-077, TASK-132 |

### Reguły trasowania

| Element | Zadania |
|---|---|
| T-1 | TASK-085 |
| T-2 | TASK-085 |
| T-3 | TASK-085 |
| T-4 | TASK-085 |
| T-5 | TASK-085 |
| T-6 | TASK-085 |
| T-7 | TASK-085, TASK-104 |

### Niespójności

| Element | Zadania |
|---|---|
| N-1 | TASK-083 |
| N-2 | TASK-083 |
| N-3 | TASK-083 |
| N-4 | TASK-083 |
| N-5 | TASK-083 |
| N-6 | TASK-083 |

### Sprawdzenia Walidacji

| Element | Zadania |
|---|---|
| W-1 | TASK-107 |
| W-2 | TASK-107 |
| W-3 | TASK-107 |
| W-4 | TASK-107 |
| W-5 | TASK-107 |
| W-6 | TASK-107 |
| W-7 | TASK-107 |
| W-8 | TASK-107 |
| W-9 | TASK-107 |
| W-10 | TASK-107 |

### Pytania do modelu decyzyjnego

| Element | Zadania |
|---|---|
| Q-OC-00 | TASK-082 |
| Q-OC-01 | TASK-082 |
| Q-OC-02 | TASK-082 |
| Q-OC-03 | TASK-082 |
| Q-OC-04 | TASK-082 |
| Q-OC-05 | TASK-082 |
| Q-OC-06 | TASK-082 |
| Q-RE-01 | TASK-071 |
| Q-WA-01 | TASK-107 |
| Q-WA-02 | TASK-107 |
| Q-WA-03 | TASK-107 |
| Q-WA-04 | TASK-107 |
| Q-WA-05 | TASK-107 |

### Reguły rekomendacji

| Element | Zadania |
|---|---|
| `R-JEV-POWTORKA` | TASK-071 |
| `R-JEV-WKLADKA` | TASK-071 |
| `R-JEV-ZADANIE_LATWE` | TASK-071 |
| `R-JEV-ZADANIE_MATURALNE` | TASK-071 |
| `R-NAJSLABSZA` | TASK-067 |
| `R-POWTORKA` | TASK-067 |
| `R-PRZYPISANIE` | TASK-067 |
| `R-SERIA` | TASK-067 |
| `R-TRUDNOSC_W_GORE` | TASK-067 |
| `R-UTRWALENIE` | TASK-067 |
| `R-WKLADKA` | TASK-067 |
| `R-WKLADKA-DIAG` | TASK-067 |

### PZS-1 (część B)

| Element | Zadania |
|---|---|
| B-1 | TASK-080, TASK-137 |
| B-2 | TASK-085, TASK-137 |
| B-3 | TASK-054, TASK-137 |
| B-4 | TASK-058, TASK-137 |
| B-5 | TASK-100, TASK-137 |
| B-6 | TASK-137 |
| B-7 | TASK-137 |
| B-8 | TASK-137 |
| B-9 | TASK-107, TASK-137 |

### Metoda sprawdzenia kompletności

Uniwersum elementów wyodrębniono skryptem z tekstu dokumentów (nie ręcznie):

- sekcje: nagłówki `##`–`####` z numerem, poza blokami kodu (149);
- ADR: nagłówki `### ADR-xxx` (47); FR/NFR: pogrubione identyfikatory w `docs/requirements.md` (36 + 10);
- DDL: `CREATE [UNLOGGED] TABLE`, `CREATE TYPE`, `CREATE [UNIQUE] INDEX`, `CREATE EXTENSION` w sekcji 7.3, wszystkie identyfikatory `trg_*` oraz funkcja `dodaj_dni_robocze`;
- API: każdy wiersz tabeli 12.3 (ścieżki rozdzielone, gdy wiersz ma dwie); prace w tle: tabela 13.3 + `ZWOLNIJ_BLOKADY_WERYFIKACJI` (13.5); zdarzenia: tabela 13.6;
- identyfikatory G-x, C-xx, E-xx, S-x, T-x, N-x, W-x, Q-xx-xx, `R-*` (reguły rekomendacji), A-xx, D-x, R-xx, Z-x, B-x oraz ścieżki plików `tests/…`, `lemat/…`, `src/lemat/…`.

Każde zadanie deklaruje listę „Realizuje”; skrypt porównał uniwersum z sumą tych list (dokładne dopasowanie napisów) i potwierdził: 0 elementów bez zadania, 0 odwołań do nieistniejących elementów, 0 zależności wskazujących na zadanie późniejsze lub z późniejszej fazy. Przy zmianie architektury należy powtórzyć tę kontrolę.
