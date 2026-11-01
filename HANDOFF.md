# HANDOFF — przekazanie projektu Lemat

| Pole | Wartość |
|---|---|
| Data | 2026-11-01 |
| Stan | Dokumentacja architektury zakończona (v2.4); implementacja nie rozpoczęta (`src/` pusty) |
| Źródła | `docs/ARCHITECTURE_DETAILED.md` v2.4, `docs/CRITIQUE_LOG.md` (iteracje 1–4), `docs/requirements.md`, `docs/glossary.md` |
| Plan | `TASKS.md` — 143 zadania w 16 fazach (F0 … F15), macierz pokrycia 775 elementów architektury |
| Reguły | `CLAUDE.md` |

## 1. Stan pętli architekt ↔ krytyk

- Iteracje krytyki 1–4: 29 + 16 + 9 + 6 uwag. Wszystkie uwagi C1, C2 i C3 są zamknięte
  (weryfikacja krytyka w kolejnych iteracjach).
- Uwagi C4-01 … C4-06 (4 średnie, 2 niskie; brak krytycznych i wysokich) zostały zaadresowane
  w wersji 2.4 — mapowanie uwaga → miejsce rozwiązania w sekcji 23.7. Ostatni wpis w
  `CRITIQUE_LOG.md` (werdykt dla v2.3: `NEEDS_REVISION`) poprzedza v2.4; runda zamykająca
  nie zawiera osobnego przeglądu krytyka dla v2.4. Rekomendacja: w TASK-001 potwierdzić zmiany
  z 23.7 przy lekturze, a przy wątpliwości zapisać pytanie w sekcji 8.
- Dwie decyzje dodane w v2.4 czekają na właściciela produktu: D-16 i D-17.

## 2. Podsumowanie architektury

**Cel.** Uczeń rozwiązuje Zadania dobrane do Profilu ucznia, Rozwiązania zadań otwartych
przesyła jako zdjęcia kartki; system tworzy Transkrypcję, ocenia krok po kroku według
Kryteriów oceniania CKE z kalibrowaną Pewnością oceny i automatycznie wystawia Ocenę
ostateczną tylko powyżej Progu pewności. W pozostałych przypadkach Ocena wstępna trafia do
Weryfikacji Nauczyciela.

**Zasady nadrzędne (1.2).** Z-1 „Jev decyduje, LLM pisze” · Z-2 wymagania > architektura
wstępna · Z-3 bezpieczna domyślność (wątpliwość → Weryfikacja; błąd odczytu nie obniża Oceny)
· Z-4 każda decyzja zapisana z rozkładem · Z-5 niezmienność Ocen · Z-6 słownik jest prawem.

**Kontenery (5).** Modularny monolit w jednym obrazie: `api` (FastAPI, 2 repliki, piaskownica
Pillow), `worker` (asyncio, pule R/S/O/P/W, piaskownica SymPy + Pillow), `scheduler`
(1 aktywny, blokada doradcza); PostgreSQL 16 jako źródło prawdy i kolejka prac w tle;
magazyn S3 (UE) na zdjęcia; bucket `rejestr-zdarzen-rodo` z Object Lock; zewnętrznie Jev,
dostawca(y) LLM, SMTP; PWA React.

**Moduły (6.2).** `http`, `konta`, `klasy`, `tresci`, `profil`, `rekomendacje`, `rozwiazania`,
`ocenianie`, `weryfikacja`, `kalibracja`, `modele`, `orkiestrator`, `powiadomienia`, `audyt`
oraz `agenci` (rekomendujący, generujący, rozpoznający, oceniający, wyjaśniający). Orkiestrator
to biblioteka: maszyna stanów w bazie + prace w tle (tryb asynchroniczny) i jedna fasada
synchroniczna dla rekomendacji (ADR-023).

**Przepływ oceny (9.4–9.6).** `POST /rozwiazania` → `ROZPOZNAJ_PISMO` (Agent rozpoznający,
Filtr danych osobowych) → Uczeń poprawia (limity ADR-036) i zatwierdza Transkrypcję →
`SPRAWDZ_POPRAWKI_TRANSKRYPCJI` → `OCEN_ROZWIAZANIE` (Plan pytań Q-OC-00 … Q-OC-06 do Jev lub
modelu zapasowego przez circuit breaker i globalny ogranicznik szybkości, SymPy, reguły 11.1,
pewność 11.2, trasowanie T-1 … T-7) → Ocena `OSTATECZNA` albo `WSTEPNA` + Weryfikacja →
`NAPISZ_WSKAZOWKE`, `AKTUALIZUJ_PROFIL` → Nauczyciel podejmuje, rozstrzyga lub koryguje →
Próbka kalibracyjna → `KALIBRUJ` (kalibrator izotoniczny, próg z górnej granicy Wilsona,
bezpieczniki).

**Dane (7).** 59 tabel (DDL 7.3), 43 typy wyliczeniowe, 6 triggerów (`trg_ocena_niezmienna`,
`trg_weryfikacja_wersja`, `trg_powody_poczatkowe`, `trg_kontekst_proby`,
`trg_suma_kryteriow`, `trg_umiejetnosc_glowna_w_dziale`), funkcja `dodaj_dni_robocze`.
Wersjonowanie Zadań i Ocen (ADR-012), wersja treści Weryfikacji oddzielona od blokady (ADR-037).

**API (12).** 100 operacji REST pod `/api/v1`, RFC 9457, `ETag`/`If-Match`, `Idempotency-Key`,
paginacja kursorem, odpytywanie co 1 s zamiast WebSocket (ADR-014).

**Prace w tle (13.3–13.5).** 28 typów prac + `ZWOLNIJ_BLOKADY_WERYFIKACJI` w schedulerze;
deduplikacja per typ, dzierżawy, `MARTWA` z handlerem po wyczerpaniu.

**NFR (17).** NFR-01 jako czas systemowy ≤ 28 s dla ≥ 99% (A-11, budżet S-1 … S-7);
NFR-02/03/04 przez ZT-1, Kontrolę losową z IPW, kalibrację out-of-fold i bezpieczniki (ADR-046);
NFR-07 przez model zapasowy wykonujący cały plan; NFR-09 przez retencję sterowaną stanami,
okres karencji, Rejestr Zdarzeń RODO i procedurę PITR (`ponow_zdarzenia_rodo`).

**Bramki startu produkcyjnego (17.6).** G-0 akceptacja PO i słownika · G-1 ZT-1 i jakość
oceniania · G-2 jakość rozpoznawania i E2E · G-3 test obciążeniowy · G-4 test chaosu NFR-07 ·
G-5 gotowość treści · G-6 przegląd RODO i test PITR · G-7 pula weryfikatorów globalnych.

## 3. Założenia biznesowe i techniczne z architektury (A-01 … A-15)

| ID | Założenie (skrót — pełna treść w sekcji 2) | Skutek, jeśli fałszywe |
|---|---|---|
| A-01 | Jev: API HTTP/JSON, pytania `noul`/`score`/`choice`, stan ≤ 32 000 znaków, tryb wsadowy ≤ 32 pytania (p95 ≤ 800 ms), limit ≥ 50 wywołań/s z tolerancją chwilowego przekroczenia | tryb `POJEDYNCZY`, `B_całk = 10`, większy budżet czasu (sprawdzane w G-4) |
| A-02 | Jev we wczesnym dostępie — system działa produkcyjnie bez Jev | model zapasowy jest pełnoprawną, osobno skalibrowaną implementacją |
| A-03 | Pilotaż: 1 000 Uczniów, 50 Nauczycieli, 1 500 Rozwiązań otwartych/dobę (20% dowody), szczyt 300/h i „koniec lekcji” 90 przesłań w 60 s | architektura wystarcza do ~20× (17.4) |
| A-04 | Formuła matury od 2023 r., 13 Działów | Działy i Umiejętności są danymi |
| A-05 | Kryteria CKE jako lista kryteriów z całkowitymi punktami w wariantach | pytanie Q-OC-00 wybiera wariant |
| A-06 | Uczniowie mają ≥ 16 lat (oświadczenie przy rejestracji) | przepływ zgody rodzica poza zakresem |
| A-07 | Dostawcy w EOG/SCC, umowa powierzenia, brak trenowania, retencja 0 dni dla zdjęć i ≤ 30 dni dla innych ról | aktywacja konfiguracji modelu zablokowana |
| A-08 | Multimodalny LLM: wyjście JSON, p95 ≤ 9 s dla 3 zdjęć, ≥ 150 wywołań/min, błędy ≤ 0,3% | budżet NFR-01 złamany |
| A-09 | Zbiór ZT-1: ≥ 700 obliczeniowych + ≥ 350 dowodów, 2 Oceny Nauczycieli, ≥ 150 kartek z imieniem i nazwiskiem | start zablokowany (G-1) albo tryb pełnej weryfikacji |
| A-10 | Nowoczesne przeglądarki (2 ostatnie wersje) z `input capture` | komunikat o braku wsparcia |
| A-11 | NFR-01 = czas systemowy (rozpoznanie + ocena), zobowiązanie ≥ 99% ≤ 30 s (do akceptacji w G-0) | 100% wymaga własnego modelu rozpoznawania (poza budżetem) |
| A-12 | ≤ 20% aktywnych Uczniów bez Klasy | cotygodniowe przeliczenie 17.7, kontrola przyjęć |
| A-13 | Czas Weryfikacji: dowód 5 min, obliczeniowe 3 min, Kontrola losowa 2 min, zgłoszenie 3 min; weryfikator globalny ≥ 90 min/dzień roboczy | model 17.7 przeliczany z pomiarów |
| A-14 | Pula weryfikatorów globalnych: 10 Nauczycieli bez Klas + ≥ 8 współpracujących (G-7 = 18) (do akceptacji w G-0) | start z `LEMAT_REJESTRACJA_BEZ_KLASY = false` |
| A-15 | Nauczyciele pracują w dni robocze; SLA 3 dni robocze; 5% eskalacji do kolejki globalnej (do akceptacji w G-0) | wartości mierzone, kontrola przyjęć per Klasa |

## 4. Założenia planisty (P-xx) — przyjęte przy tworzeniu `TASKS.md`

Nie wynikają wprost z architektury; można je zmienić bez ADR, ale zmianę trzeba odnotować tutaj.

| ID | Założenie | Uzasadnienie |
|---|---|---|
| P-01 | Kod PWA w `src/pwa/` | architektura nie określa katalogu frontendu; jeden obraz i jedno repozytorium (ADR-001) |
| P-02 | Migracje Alembic w `src/lemat/migracje/` | spójność z pakietem `lemat` |
| P-03 | `docker-compose.yml` w katalogu głównym, konfiguracje środowisk, pulpity i alerty w `deploy/` | 17.5 nie wskazuje katalogu |
| P-04 | Agenci w `src/lemat/agenci/{rekomendujacy,generujacy,rozpoznajacy,oceniajacy,wyjasniajacy}/` | 10: `lemat.agenci.<nazwa>`; nazwy z pojęć słownika |
| P-05 | Słownik terminów matematycznych w `src/lemat/rozpoznawanie/slownik_matematyczny.txt` jako katalog danych (bez modułu domenowego) | ścieżka podana w ADR-042/3.3, ale 6.2 nie zna modułu `lemat.rozpoznawanie`; Filtr danych osobowych należy do `lemat.rozwiazania` — patrz pytanie Q-1 |
| P-06 | Procedura PITR jako komenda `lemat ponow-zdarzenia-rodo` | 16.4 mówi o „skrypcie `ponow_zdarzenia_rodo`”; jeden punkt wejścia CLI |
| P-07 | Śledzenie wymagań przez marker `@pytest.mark.wymaganie("FR-xx")` i raport w CI | 18 mapuje wymagania, ale nie definiuje mechanizmu testów akceptacyjnych |
| P-08 | Narzędzia: Node.js 20 LTS + npm dla PWA, `pip`/`venv` dla Pythona | architektura nie wskazuje menedżerów pakietów |
| P-09 | Jeden commit na zadanie, Conventional Commits z `TASK-xxx` w treści | czytelna historia i powiązanie z planem |
| P-10 | Zadania F14 (PWA) zależą od gotowego API danego obszaru; atrapy modeli wystarczają do testów E2E | środowisko `lokalne` z 17.5 |

## 5. Decyzje czekające na właściciela produktu (bramka G-0)

Do czasu decyzji implementacja realizuje **opcję domyślną** (23.4). Zmiana opcji dotyczy reguł
w `lemat.ocenianie`, `lemat.weryfikacja`, `lemat.konta` i konfiguracji, bez zmiany struktury
(R-11). Zadanie przygotowania pakietu decyzji: TASK-137 (bramka G-0).

| # | Decyzja | Opcja domyślna | Alternatywa |
|---|---|---|---|
| D-1 | Zmiany słownika PZS-1 (86 nowych pojęć, definicje B-1 … B-9) | treść 3.3/3.4 | inne brzmienie → zmiana nazw w kodzie przed produkcją |
| D-2 | Interpretacja NFR-01 (A-11) | czas systemowy, ≥ 99% ≤ 30 s | 100% ≤ 30 s → własny model rozpoznawania |
| D-3 | Ocena wstępna bez punktów przy podejrzeniu błędu odczytu; zawieszenie Wyniku przy zgłoszeniu; rozstrzyganie na korzyść Ucznia bez zdjęć | ADR-022 | literalne FR-24 (łamie FR-28) |
| D-4 | Pula weryfikatorów globalnych | ≥ 8 Nauczycieli współpracujących albo start bez Uczniów bez Klasy | automatyczna finalizacja po SLA — odrzucona |
| D-5 | Brak automatycznej finalizacji Oceny wstępnej po SLA | ADR-021 | wymagałaby zmiany FR-24/FR-26 |
| D-6 | Tryb pełnej weryfikacji, gdy ZT-1 nie daje progu | 11.5.1 p. 5 | większy ZT-1 |
| D-7 | Brak wpisu ręcznego Rozwiązania | ADR-024 | wpis ręczny z obowiązkową Weryfikacją |
| D-8 | Kopie treningowe za odwołalną zgodą | ADR-035 | brak zbioru treningowego |
| D-9 | Zatwierdzanie kont Nauczycieli; rozszerzone powody Weryfikacji | 16.1, 11.3 | bez Kontroli losowej NFR-03/04 niemierzalne |
| D-10 | Zakres FR-18: zmiana kroków + ≤ 2 wstawienia z limitami i Sprawdzeniem zgodności | ADR-036 | dowolna edycja z obowiązkową Weryfikacją (+15–25% obciążenia) |
| D-11 | Zakres FR-14: Nauczyciel przegląda własne zlecenia, `GLOBALNY` przez recenzenta i Zgłoszenia błędu | ADR-028, ADR-038 | każdy Nauczyciel poprawia `GLOBALNY` (+audyt, +cofanie wersji) |
| D-12 | G-1(a) dla dowodów: zgodność ≥ 90%, LB Wilsona ≥ 85,0% przy n ≥ 350 | 11.5.1 | LB ≥ 87,5% → ≥ 700 dowodów w ZT-1 |
| D-13 | SLA 3 dni robocze, model tygodniowy, G-7 = 18, kontrola przyjęć per Klasa | ADR-043 | SLA 72 h kalendarzowe (praca weekendowa) |
| D-14 | Możliwość pominięcia Testu diagnostycznego | pominięcie dozwolone | test obowiązkowy (`409 TEST_WYMAGANY`) |
| D-15 | Przywrócenie konta Nauczyciela a Klasy przekazane w karencji | Klasy zostają u nowego Nauczyciela | automatyczny zwrot (+kolumna, +krok) |
| D-16 | Bezpiecznik trafności przy progu 0,990 | tryb pełnej weryfikacji do dezaktywacji przez Administratora | zatrzymanie na 0,990 z alertem |
| D-17 | Zapas puli na wariant „aktywny bezpiecznik poprawek” | G-7 = 18 (zapas 1,22) | G-7 = 20 (+2 umowy) |

Pozostałe elementy G-0 (17.6): akceptacja PZS-1, A-11, A-12, A-14, A-15; rozszerzenia powodów
Weryfikacji (`PODEJRZENIE_BLEDU_ODCZYTU`, `ZGLOSZENIE_BLEDU_ODCZYTU`,
`POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`, `KONTROLA_LOSOWA`, `BLAD_PRZETWARZANIA`); kolejka globalna z
pseudonimizacją i kontrolą przyjęć (ADR-021, ADR-031); tryb pełnej weryfikacji przy
niespełnionym G-1(b); start z `LEMAT_REJESTRACJA_BEZ_KLASY = false`, gdy G-7 niespełnione.

**Decyzje blokujące produkcję, nie implementację:** G-0 … G-7 blokują wyłącznie start
produkcyjny; wszystkie zadania F0 … F14 można realizować przed decyzjami.

## 6. Instrukcja startowa

Wymagania: Linux/macOS, Docker + Docker Compose, Python 3.12, Node.js 20 LTS, git.

1. Przeczytaj `CLAUDE.md`, ten plik, a następnie `docs/ARCHITECTURE_DETAILED.md` w częściach
   (sekcje 1–7, potem sekcje wskazane w bieżącym zadaniu). Dokument ma ~7 100 linii —
   nie wczytuj go w całości naraz.
2. Zacznij od TASK-001 (onboarding) i TASK-002 (szkielet repozytorium), potem kolejno wg
   zależności w `TASKS.md`.
3. Po TASK-002 … TASK-005 lokalnie (komendy planowane w zadaniach):
   ```bash
   python3.12 -m venv .venv && . .venv/bin/activate
   pip install -e ".[dev]"
   docker compose up -d                 # PostgreSQL 16, MinIO, SMTP testowy, atrapy modeli
   alembic upgrade head                 # od TASK-007
   pytest && ruff check . && mypy src && lint-imports
   lemat api & lemat worker & lemat scheduler
   ```
4. Środowisko `lokalne` działa bez kluczy dostawców modeli (atrapy, TASK-039). Klucze Jev i
   LLM są potrzebne dopiero w środowisku `testowe` (bramki G-1 … G-4).

## 7. Stan prac

| Faza | Zadania | Status |
|---|---|---|
| F0 Fundament | TASK-001 … TASK-006 | TODO |
| F1 Schemat bazy | TASK-007 … TASK-018 | TODO |
| F2 Rdzeń techniczny | TASK-019 … TASK-035 | TODO |
| F3 Warstwa modeli AI | TASK-036 … TASK-044 | TODO |
| F4 Konta i bezpieczeństwo | TASK-045 … TASK-050 | TODO |
| F5 Treści | TASK-051 … TASK-058 | TODO |
| F6 Klasy | TASK-059 … TASK-061 | TODO |
| F7 Profil i rekomendacje | TASK-062 … TASK-072 | TODO |
| F8 Rozwiązania i rozpoznawanie | TASK-073 … TASK-081 | TODO |
| F9 Ocenianie | TASK-082 … TASK-090 | TODO |
| F10 Weryfikacja | TASK-091 … TASK-099 | TODO |
| F11 Kalibracja | TASK-100 … TASK-105 | TODO |
| F12 Generowanie | TASK-106 … TASK-109 | TODO |
| F13 RODO | TASK-110 … TASK-120 | TODO |
| F14 PWA | TASK-121 … TASK-130 | TODO |
| F15 Obserwowalność, wdrożenie, bramki | TASK-131 … TASK-143 | TODO |

Ostatnio ukończone: — · W toku: — · Zablokowane: —

## 8. Otwarte pytania i znane niespójności

| ID | Pytanie | Gdzie | Propozycja tymczasowa |
|---|---|---|---|
| Q-1 | Ścieżka `lemat/rozpoznawanie/slownik_matematyczny.txt` wskazuje moduł spoza listy 6.2 | 3.3, ADR-042, 6.2 | P-05: katalog danych bez modułu domenowego; Filtr danych osobowych w `lemat.rozwiazania` |
| Q-2 | v2.4 nie ma osobnego przeglądu krytyka (ostatni werdykt dotyczy v2.3) | `CRITIQUE_LOG.md`, 23.7 | traktować 23.7 jako obowiązujące; wątpliwości zgłaszać tutaj |
| Q-3 | Lokalizacja kodu PWA i konfiguracji wdrożeniowych nieokreślona | 5.1, 17.5 | P-01, P-03 |

## 9. Propozycje do słownika (poza PZS-1)

Brak. Każde nowe pojęcie dopisz tutaj (pojęcie, definicja, identyfikator, przyczyna) przed
użyciem w kodzie — zob. `CLAUDE.md` sekcja 5 p. 7.

## 10. Najważniejsze ryzyka na start implementacji

- R-01/R-17 brak Jev lub trybu wsadowego → model zapasowy i tryb `POJEDYNCZY` są pełnoprawne od początku (TASK-040 … TASK-044).
- R-02/R-12/R-15 obciążenie Nauczycieli i pula globalna → model 17.7 i flaga `LEMAT_REJESTRACJA_BEZ_KLASY`.
- R-10 ZT-1 nie da progu → tryb pełnej weryfikacji (D-6); zbiór ZT-1 trzeba zacząć zbierać równolegle z F0 (organizacyjnie, TASK-138).
- R-16 PITR cofa Zdarzenia RODO → kolejność startu procesów po odtworzeniu jest częścią runbooka (TASK-119, TASK-134).
- R-14 obejście kartki przez Poprawki → limity ADR-036 i Sprawdzenie zgodności (TASK-080, TASK-081).

## 11. Gotowy prompt dla kolejnego dewelopera (lub jego agenta)

```text
Jesteś deweloperem projektu Lemat (/home/user/lemat) — platformy do nauki do matury z
matematyki z agentami AI. Pracujesz w Pythonie 3.12 (FastAPI, SQLAlchemy 2, Alembic,
PostgreSQL 16) i React 18 + TypeScript (PWA).

Zanim cokolwiek zmienisz:
1. Przeczytaj CLAUDE.md (reguły, mapa plików, słownik, workflow) i HANDOFF.md (stan,
   założenia A-xx i P-xx, decyzje D-1…D-17 z opcjami domyślnymi, otwarte pytania).
2. Otwórz TASKS.md i wybierz najniższe zadanie TASK-xxx ze statusem TODO, którego wszystkie
   zależności mają status DONE. Ustaw IN_PROGRESS.
3. Przeczytaj w docs/ARCHITECTURE_DETAILED.md (v2.4, ~7 100 linii — czytaj w częściach)
   wszystkie elementy z pola „Realizuje” zadania: sekcje, ADR, DDL, endpointy, prace w tle,
   scenariusze C-xx i błędy E-xx.

Zasady bezwzględne:
- Pliki w docs/ są tylko do odczytu.
- Nazwy w kodzie, bazie i API pochodzą ze słownika (docs/glossary.md + PZS-1, sekcje 3.2–3.4)
  i spełniają reguły ASCII z 3.1; „praca w tle”, nigdy „zadanie/task/job” dla pracy
  asynchronicznej; tests/test_slownik.py musi przechodzić.
- Jev decyduje, LLM pisze (Z-1); przy wątpliwości → Weryfikacja (Z-3); Ocena niezmienna (Z-5).
- Implementuj opcje domyślne decyzji D-x; nie rozstrzygaj sprzeczności po cichu — zapisz je
  w HANDOFF.md → „Otwarte pytania”.

Po implementacji:
- Testy (każdy C-xx/E-xx z zadania ma test nazwany identyfikatorem), potem:
  ruff check . && mypy src && lint-imports && pytest
- Odhacz Definition of Done w TASKS.md, ustaw DONE, zaktualizuj HANDOFF.md („Stan prac”,
  nowe P-xx, pytania, propozycje do słownika).
- Jeden commit na zadanie (Conventional Commits, „TASK-xxx” i „Realizuje: …” w treści).
  Nie pushuj, jeśli nikt o to nie prosił.

Zacznij od TASK-001, następnie TASK-002. Na końcu pracy podaj: zrealizowane zadania,
wynik testów, zmienione pliki i otwarte pytania.
```
