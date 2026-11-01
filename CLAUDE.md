# CLAUDE.md — reguły projektu Lemat

Ten plik obowiązuje każdego dewelopera i każdego agenta pracującego w tym repozytorium.
Przed pierwszą zmianą przeczytaj także `HANDOFF.md` (stan projektu, założenia, decyzje)
i `TASKS.md` (plan pracy).

## 1. Czym jest Lemat

Platforma przeglądarkowa do nauki do matury z matematyki. Agenci AI dobierają Zadania do
Profilu ucznia, generują nowe Zadania, odczytują Rozwiązania ze zdjęć kartek (Transkrypcja)
i oceniają je według Kryteriów oceniania; Nauczyciel weryfikuje Oceny o niskiej Pewności
oceny. Zasada nadrzędna: **Jev decyduje, LLM pisze** (Z-1).

## 2. Hierarchia dokumentów (od najważniejszego)

1. `docs/requirements.md` — wymagania FR-01 … FR-36, NFR-01 … NFR-10 (Z-2: wymagania mają pierwszeństwo).
2. `docs/glossary.md` — słownik pojęć (Z-6: słownik jest prawem) + propozycja PZS-1 z sekcji 3.4 architektury (obowiązuje do decyzji G-0).
3. `docs/ARCHITECTURE_DETAILED.md` v2.4 — architektura do implementacji (ADR-001 … ADR-047, DDL 7.3, API 12.3, prace w tle 13.3, parametry 22).
4. `docs/CRITIQUE_LOG.md` — historia uwag krytyka (C1 … C4) i ich zamknięcia.
5. `docs/architecture.md`, `docs/vision.md` — kontekst; w razie sprzeczności wygrywa dokument wyżej.

**Pliki w `docs/` są tylko do odczytu.** Nie edytuj ich w ramach zadań implementacyjnych.
Wyjątek: commit dopisujący PZS-1 do `docs/glossary.md` po akceptacji w bramce G-0 (zadanie
w `TASKS.md`), wykonany wyłącznie za zgodą właściciela produktu i właścicieli słownika.
Niejasność lub sprzeczność w architekturze zapisz w `HANDOFF.md` → „Otwarte pytania”;
nie rozstrzygaj jej po cichu w kodzie.

## 3. Stos technologiczny (5.2 — nie zmieniaj bez nowego ADR)

| Obszar | Wybór |
|---|---|
| Backend | Python 3.12, FastAPI, Uvicorn, Pydantic v2, SQLAlchemy 2 (Core + ORM), Alembic |
| Baza | PostgreSQL 16 (`citext`, `pg_trgm`) — jedyne źródło prawdy i mechanizm prac w tle (ADR-002) |
| Prace w tle | tabela `praca_w_tle` + `FOR UPDATE SKIP LOCKED` + `LISTEN/NOTIFY`; procesy `api`, `worker`, `scheduler` z jednego obrazu (ADR-001) |
| Magazyn obiektów | S3 w UE (lokalnie MinIO), SSE, bez wersjonowania na bucketach zdjęć; bucket `rejestr-zdarzen-rodo` z Object Lock |
| Obraz i obliczenia | Pillow ≥ 10 + `pillow-heif` (piaskownice procesowe), SymPy ≥ 1.13 (`parse_latex`/`lark`) w piaskownicy |
| Modele AI | porty `lemat.modele.porty`, adaptery `lemat.modele.adaptery.*` (Jev, model zapasowy LLM, LLM per dostawca) |
| Frontend | React 18 + TypeScript 5 + Vite, PWA, KaTeX, `react-i18next` (wyłącznie `pl-PL`), odpytywanie zamiast WebSocket (ADR-014) |
| Obserwowalność | OpenTelemetry, Prometheus, Grafana, Loki, logi JSON |
| Wdrożenie | Docker Compose, 2 VM w UE, Caddy 2, zarządzany PostgreSQL z PITR 14 dni |
| Testy | pytest, Playwright (iPhone 13, Pixel 7, Desktop Chrome), `import-linter` |

## 4. Mapa plików i katalogów

Stan obecny (2026-11-01): istnieje wyłącznie dokumentacja; `src/` jest pusty. Struktura
poniżej to plan — ścieżki oznaczone *(P)* są propozycją planisty (HANDOFF.md, P-xx),
pozostałe pochodzą wprost z architektury.

```
lemat/
├── CLAUDE.md                      # ten plik — reguły
├── TASKS.md                       # plan pracy: TASK-001 … TASK-143 + macierz pokrycia
├── HANDOFF.md                     # stan, założenia, decyzje PO, instrukcja startowa
├── README.md
├── docker-compose.yml        (P)  # środowisko „lokalne”
├── deploy/                   (P)  # Caddyfile, konfiguracje środowisk testowe/produkcyjne, pulpity, alerty
├── .claude/agents/                # agenci pętli dokumentacyjnej: architect, critic, planner
├── docs/                          # TYLKO DO ODCZYTU
│   ├── vision.md, glossary.md, requirements.md, architecture.md
│   ├── ARCHITECTURE_DETAILED.md   # v2.4 — specyfikacja implementacji
│   └── CRITIQUE_LOG.md
├── src/
│   ├── lemat/                     # pakiet Pythona (modularny monolit, 6.1–6.2)
│   │   ├── konfiguracja.py        # wszystkie parametry z sekcji 22 (LEMAT_*)
│   │   ├── migracje/         (P)  # Alembic
│   │   ├── http/                  # routery FastAPI, auth, DTO, RFC 9457, idempotencja
│   │   ├── konta/                 # konta, role, flagi, RODO, Zdarzenia RODO, dni robocze
│   │   ├── klasy/                 # Klasy, Zestawy zadań, Przypisania
│   │   ├── tresci/                # Zadania, Kryteria, Wkładki, Działy, Umiejętności, Arkusze CKE
│   │   │   └── dane/umiejetnosci.yaml
│   │   ├── profil/                # Profil ucznia, Wynik zadania, Test diagnostyczny
│   │   ├── rekomendacje/          # Ścieżka nauki, Seria, Rekomendacje
│   │   ├── rozwiazania/           # Rozwiązania, zdjęcia, Transkrypcja, Odpowiedzi, Filtr danych osobowych
│   │   ├── rozpoznawanie/         # slownik_matematyczny.txt (ścieżka z ADR-042, patrz HANDOFF P-05)
│   │   ├── ocenianie/             # Ocena, reguły 11.1–11.3, Wskazówki
│   │   ├── weryfikacja/           # Kolejka weryfikacji, blokady, szkice, eskalacja
│   │   ├── kalibracja/            # Próbki, Kalibrator pewności, Próg pewności, bezpieczniki
│   │   ├── powiadomienia/
│   │   ├── audyt/
│   │   ├── orkiestrator/          # maszyny stanów, praca_w_tle, scheduler, fasada synchroniczna
│   │   ├── agenci/                # rekomendujacy/, generujacy/, rozpoznajacy/, oceniajacy/, wyjasniajacy/
│   │   └── modele/                # porty.py, rejestr.py, adaptery/, schematy/*.json
│   └── pwa/                  (P)  # React 18 + TS + Vite
└── tests/
    ├── test_slownik.py, slownik_tokeny.yaml, slownik_wyjatki.yaml
    ├── kalibracja/                # test_bezpiecznik.py, test_zrodlo_probki.py, symulacja_bezpiecznika.py
    ├── modele/                    # test_ogranicznik.py
    ├── obciazenie/                # symulacja_ogranicznika.py
    └── <moduł>/                   # testy pozostałych modułów (P)
```

Każdy moduł domenowy ma publiczną fasadę `lemat/<moduł>/api.py`; tylko przez nią inne moduły
mogą go używać.

## 5. Słownik pojęć — obowiązkowe reguły nazewnictwa (3.1)

Słownik (`docs/glossary.md`) i pojęcia uzupełniające PZS-1 (sekcja 3.3–3.4 architektury, 86
pojęć) są **jedynym źródłem nazw** encji, tabel, kolumn, pól DTO, ścieżek API, wartości
wyliczeniowych, typów prac w tle, zdarzeń i kluczy schematów JSON modeli.

1. **ASCII bez polskich znaków:** `snake_case` (baza, JSON, Python), `PascalCase` (klasy),
   `kebab-case` (ścieżki URL), `UPPER_SNAKE` (wartości wyliczeniowe). Przykłady:
   `Uczeń` → `uczen`/`Uczen`/`/uczniowie`; `Zgłoszenie błędu odczytu` → `ZGLOSZENIE_BLEDU_ODCZYTU`.
2. **Kolumny techniczne po angielsku i tylko te:** `id`, `created_at`, `updated_at`, `version`.
3. **„zadanie” jest zarezerwowane dla pojęcia Zadanie.** Jednostka pracy asynchronicznej to
   **praca w tle** (`praca_w_tle`) — nigdy „zadanie”, „task”, „job” w kodzie domenowym.
   „Żądanie” nie występuje w identyfikatorach: wniosek użytkownika = `wniosek`
   (`WNIOSEK_O_USUNIECIE`), żądanie HTTP do modelu = `wywolanie`.
4. **Zakazane synonimy:** `student`, `task`, `submission`, `grade`, `score` (poza typem
   pytania Jev `SCORE`), `difficulty`, `topic`, `skill`, `lesson`, `review` (jako weryfikacja).
5. **Jedno słowo — jedno znaczenie:** odpowiedź modelu = `DecyzjaSkladowa`; rezultat operacji
   = `rezultat`; wynik SymPy = `Rownowaznosc`; grupa A/B = `grupa eksperymentu` (nigdy „wariant”);
   pozycja Kryteriów = `kryterium` (`nr_kryterium`, nigdy „krok”); „zdjęcie” wyłącznie jako
   fotografia kartki; wyłączenie bezpiecznika = `dezaktywacja`; „wstrzymanie” tylko w znaczeniu
   z 3.1 p. 5. Słowa `odpowiedz*`, `wariant*`, `krok*`, `wynik*`, `kolejk*`, `zadan*`, `zdje*`,
   `wstrzym*` wolno użyć tylko z listy dozwolonych użyć w `tests/slownik_wyjatki.yaml`.
6. **Dopuszczone terminy techniczne:** `theta`, `trudnosc_b`, `ipw`, `ece`, `n_eff`, `cv5`,
   `sha256`, `latex`, `sympy`, `jwt`, `url`, `mime`, `trace_id`, `hmac`, `fnv1a`, `http`,
   `rodo`; prefiksy `ux_`, `ix_`, `fk_`, `trg_`.
7. **Nowe pojęcie?** Nie wymyślaj nazwy. Sprawdź 3.2/3.3; jeśli pojęcia brak — zapisz propozycję
   (pojęcie, definicja, identyfikator, przyczyna) w `HANDOFF.md` → „Propozycje do słownika”
   i dopisz token do `tests/slownik_tokeny.yaml` dopiero po akceptacji (tak jak PZS-1).
8. **Strażnik:** `tests/test_slownik.py` (cztery części z 3.4) musi przechodzić w CI.
   Identyfikator `TASK-xxx` jest numerem planu — nie używaj go w kodzie, bazie ani API.

Teksty interfejsu i proza dokumentów używają polskich znaków; tylko identyfikatory są ASCII.

## 6. Reguły architektoniczne (skrót — szczegóły w podanych sekcjach)

- **Z-1:** punkty i automatyczne zaliczenie pochodzą wyłącznie z Modelu decyzyjnego (Jev albo
  model zapasowy) z zapisanym rozkładem; sygnały LLM działają tylko w kierunku bezpiecznym.
- **Z-3:** przy wątpliwości lub awarii → Weryfikacja, nigdy automatyczne zaliczenie; błąd odczytu
  nigdy nie obniża Oceny (FR-28).
- **Z-4:** każda decyzja modelu w `decyzja_modelu` (NFR-05). **Z-5:** Ocena niezmienna
  (`trg_ocena_niezmienna`); korekta = nowa wersja.
- **Zależności modułów:** tylko przez `lemat.<moduł>.api` (kontrakt `import-linter`). SDK
  dostawców modeli wyłącznie w `lemat.modele.adaptery.*` (NFR-06). Agenci (`lemat.agenci.*`)
  przyjmują i zwracają Pydantic, nie znają FastAPI/SQLAlchemy i nie dotykają bazy.
- **Transakcje (15.1):** zmiana stanu i zlecenie kolejnej pracy w tle w jednej transakcji;
  wywołania modeli poza transakcją; zapis warunkowy `WHERE status = … AND version = …`;
  najpierw zmiana starego wiersza, potem INSERT nowego; `FOR UPDATE` tylko w krótkich sekcjach,
  w kolejności `rozwiazanie` → `weryfikacja` → `uzytkownik FOR SHARE`.
- **Handlery prac w tle są idempotentne** i sprawdzają warunek wykonania z 13.3 (w tym konto
  `AKTYWNE`; w okresie karencji koniec z `KONTO_USUWANE`).
- **Parametry** tylko w `lemat/konfiguracja.py` (sekcja 22); progi, kalibratory i Konfiguracja
  modeli w bazie.
- **RODO:** Zdarzenie RODO zapisywane do bucketu `rejestr-zdarzen-rodo` **przed** transakcją
  (ADR-041); brak danych osobowych w logach i promptach; `usun_po` jedynym źródłem prawdy
  o usuwaniu zdjęć; Filtr danych osobowych przed zapisem Transkrypcji (ADR-042).
- **API:** RFC 9457, `ETag`/`If-Match` na zasobie z kolumny „ETag” w 12.3, `Idempotency-Key`
  tam, gdzie wskazuje 12.3; komunikaty po polsku.
- **Opcje domyślne decyzji D-1 … D-17** (23.4) są implementowane tak, by zmiana decyzji
  dotyczyła wyłącznie reguł w `lemat.ocenianie`, `lemat.weryfikacja`, `lemat.konta` i
  konfiguracji (R-11).

## 7. Workflow pracy (jedno zadanie = jeden commit)

1. **Wybierz zadanie** z `TASKS.md`: najniższy `TASK-xxx` ze statusem `TODO`, którego wszystkie
   zależności mają `DONE`. Zmień status na `IN_PROGRESS`.
2. **Przeczytaj źródła:** każdą sekcję, ADR, tabelę DDL, endpoint i scenariusz C-xx/E-xx z pola
   „Realizuje” (dokument architektury czytaj w częściach — ma ponad 7 000 linii). Sprawdź
   słownik dla każdej nowej nazwy.
3. **Implementuj** w module wskazanym w 6.2; nie wychodź poza zakres zadania. Brakujące lub
   sprzeczne miejsce w architekturze → „Otwarte pytania” w `HANDOFF.md`, a zadanie `BLOCKED`,
   jeśli bez odpowiedzi nie da się spełnić DoD.
4. **Testuj:** testy jednostkowe i integracyjne; każdy C-xx i E-xx z „Realizuje” ma test
   nazwany identyfikatorem (`test_c07_…`, `test_e35_…`). Uruchom lokalnie:
   ```bash
   ruff check . && mypy src && lint-imports && pytest
   # gdy dotyczy PWA:
   npm --prefix src/pwa run lint && npm --prefix src/pwa run test
   ```
5. **Zaktualizuj dokumenty planu:** w `TASKS.md` odhacz DoD i ustaw `DONE (<sha>)` po commicie
   (albo w tym samym commicie `DONE`, a sha w następnym); w `HANDOFF.md` uzupełnij „Stan prac”,
   nowe założenia techniczne (P-xx), otwarte pytania i propozycje do słownika.
6. **Commit** (bez pushowania, jeśli nikt o to nie prosił), format Conventional Commits z
   numerem zadania w treści:
   ```
   feat(ocenianie): reguły trasowania T-1…T-7 (TASK-085)

   Realizuje: 11.3, ADR-008, ADR-022, FR-23…FR-26
   ```
   Jeden commit na zadanie; nie łącz zadań. Migracje w osobnym pliku na zadanie.

Wspólne Definition of Done jest na początku `TASKS.md`. Zadanie nie jest `DONE`, jeśli CI nie
jest zielone albo `tests/test_slownik.py` zgłasza naruszenie.

## 8. Czego nie robić

- Nie zmieniaj plików w `docs/`, stosu technologicznego ani struktury kontenerów bez nowego ADR
  uzgodnionego z właścicielem produktu.
- Nie dodawaj zewnętrznego brokera, WebSocketów, cache poza PostgreSQL ani nowych zależności
  infrastrukturalnych.
- Nie wysyłaj danych do dostawcy modelu bez potwierdzonej retencji (A-07,
  `409 BRAK_RETENCJI_DOSTAWCY`).
- Nie implementuj automatycznego zaliczania dowodów ani automatycznej finalizacji Oceny
  wstępnej po SLA (D-5, FR-26).
- Nie twórz wpisu ręcznego Rozwiązania (ADR-024).
