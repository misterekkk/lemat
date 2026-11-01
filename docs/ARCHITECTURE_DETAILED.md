# Lemat — Architektura szczegółowa

| Pole | Wartość |
|---|---|
| Dokument | `docs/ARCHITECTURE_DETAILED.md` |
| Wersja | 2.4 (iteracja 6 pętli architekt → krytyk: zamknięcie uwag C4-01 … C4-06) |
| Data | 2026-11-01 |
| Dokumenty źródłowe | `docs/vision.md`, `docs/glossary.md`, `docs/requirements.md`, `docs/architecture.md` |
| Zaadresowane uwagi | `docs/CRITIQUE_LOG.md`: iteracja 1 — C1-01 … C1-29 (wersje 2.0 i 2.1); iteracja 2 — ponownie otwarta C1-21 oraz C2-01 … C2-16, wszystkie 17 (wersja 2.2); iteracja 3 — C3-01 … C3-09, wszystkie 9 (wersja 2.3); iteracja 4 — C4-01 … C4-06 (w tym 10 podpunktów C4-06), wszystkie 6 (wersja 2.4); mapowanie uwaga → miejsce rozwiązania: 23.3 (C1), 23.5 (C1-21, C2), 23.6 (C3) i 23.7 (C4) |
| Decyzje właściciela produktu | zebrane w bramce G-0 (17.6), lista D-1 … D-17 w 23.4 |
| Status | Do przeglądu przez krytyka (`docs/CRITIQUE_LOG.md`) |

Dokument rozwija `docs/architecture.md` (wstępną) do poziomu, na którym można
rozpocząć implementację. Tam, gdzie wstępna architektura jest sprzeczna z
wymaganiami, **pierwszeństwo mają wymagania** (np. ADR-008). Każda decyzja ma
numer ADR (sekcja 19), a każde założenie — numer A-xx (sekcja 2). Decyzje,
które zmieniają lub doprecyzowują wymagania albo słownik pojęć, są zebrane w
bramce **G-0** (sekcja 17.6) i wymagają akceptacji właściciela produktu przed
startem produkcyjnym; do tego czasu obowiązują w brzmieniu z tego dokumentu.

## Spis treści

1. Cel, zakres i zasady nadrzędne
2. Założenia
3. Język wszechobecny (mapowanie słownika pojęć na kod, propozycja zmian słownika PZS-1)
4. Widok kontekstu systemu
5. Widok kontenerów i stos technologiczny
6. Moduły, agenci i porty
7. Model danych (ERD + pełne DDL)
8. Maszyny stanów
9. Przepływy (diagramy sekwencji)
10. Agenci — kontrakty, algorytmy, pytania do Jev
11. Reguły decyzyjne oceniania i kierowania do Weryfikacji
12. Kontrakty API (REST)
13. Kontrakty wewnętrzne (porty) i prace w tle
14. Obsługa błędów
15. Współbieżność i spójność
16. Bezpieczeństwo i RODO
17. Wymagania niefunkcjonalne — realizacja, budżety, obciążenie, obserwowalność, wdrożenie
18. Mapowanie wymagań na komponenty, przepływy i API
19. Rejestr decyzji architektonicznych (ADR)
20. Odpowiedzi na otwarte pytania z architektury wstępnej
21. Ryzyka
22. Parametry systemu (wartości domyślne)
23. Changelog iteracji (23.3, 23.5, 23.6 i 23.7: mapowanie uwag krytyki → miejsca rozwiązania; 23.4: decyzje właściciela produktu)

---

## 1. Cel, zakres i zasady nadrzędne

### 1.1 Cel systemu

Lemat to platforma przeglądarkowa (telefon i komputer), w której:

- **Uczeń** rozwiązuje **Zadania** dobrane przez **Agenta rekomendującego** do
  jego **Profilu ucznia**, czyta **Wkładki teoretyczne**, a **Rozwiązania**
  zadań otwartych przesyła jako zdjęcia kartki;
- **Agent rozpoznający** tworzy **Transkrypcję**, **Agent oceniający** (silnik
  decyzyjny: **Jev**) wystawia **Ocenę** z **Pewnością oceny**, a **Agent
  wyjaśniający** pisze wskazówkę;
- **Nauczyciel** prowadzi klasę, przypisuje zadania i wykonuje **Weryfikację**;
- **Administrator** importuje **Arkusze CKE**, zarządza kontami i konfiguracją
  modeli.

### 1.2 Zasady nadrzędne

| ID | Zasada | Konsekwencja |
|---|---|---|
| Z-1 | **Jev decyduje, LLM pisze.** | Każda decyzja, która **przyznaje punkty albo pozwala zaliczyć Rozwiązanie automatycznie**, pochodzi z **Modelu decyzyjnego** (Jev — Model System One — albo model zapasowy w tej samej roli, NFR-07) i ma zapisany rozkład prawdopodobieństwa. Sygnały pochodzące z **Modelu generatywnego (LLM)** — `pewnosc_odczytu`, `czytelne`, `zgodnosc_z_zadaniem` Agenta rozpoznającego, Sprawdzenie zgodności poprawek (`zgodnosc_poprawek`, ADR-036), Opinia pomocnicza — są wyłącznie **danymi wejściowymi** i mogą działać tylko w kierunku bezpiecznym: dodać powód Weryfikacji albo zatrzymać przetwarzanie (Z-3). Nigdy nie podnoszą punktów ani nie zezwalają na trasę automatyczną. LLM produkuje tekst (zadania, wskazówki, wkładki, transkrypcje). |
| Z-2 | **Wymagania > architektura wstępna.** | Np. druga opinia LLM nie może samodzielnie zaliczyć rozwiązania poniżej progu (FR-24, ADR-008). |
| Z-3 | **Bezpieczna domyślność.** | W razie wątpliwości lub awarii rozwiązanie trafia do Weryfikacji, nigdy nie jest automatycznie zaliczane. Bezpieczna domyślność nigdy nie obniża Oceny Ucznia z powodu błędu odczytu (FR-28, ADR-022). |
| Z-4 | **Każda decyzja jest zapisana.** | Tabela `decyzja_modelu` przechowuje pytanie, wybraną wartość i pełny rozkład prawdopodobieństw (NFR-05). |
| Z-5 | **Niezmienność ocen.** | Ocena nie jest nadpisywana; korekta tworzy nową wersję, poprzednia otrzymuje status `ZASTAPIONA` (ADR-012). Jedyny wyjątek: kolumny `wskazowka` i `zrodlo_wskazowki` są **jednokrotnego zapisu** (`NULL` → wartość), bo Wskazówka powstaje asynchronicznie (ADR-025); egzekwuje to trigger `trg_ocena_niezmienna`. |
| Z-6 | **Słownik pojęć jest prawem.** | Nazwy encji, tabel, pól, endpointów i zdarzeń pochodzą ze słownika (sekcja 3). Każde pojęcie spoza słownika i każda zmiana znaczenia pojęcia słownikowego jest ujęta w formalnej propozycji zmiany słownika **PZS-1** (3.4), której akceptacja jest warunkiem bramki G-0. |

### 1.3 Zakres i poza zakresem

W zakresie: FR-01 … FR-36, NFR-01 … NFR-10.

Poza zakresem (zgodnie z `requirements.md` i decyzjami tego dokumentu):

- przedmioty inne niż matematyka; aplikacja natywna;
- automatyczne zaliczanie zadań z dowodem;
- generowanie Wkładek teoretycznych „na żądanie” w trakcie nauki ucznia —
  wkładki są przygotowywane i zatwierdzane przed udostępnieniem (ADR-015);
- automatyczna konwersja PDF Arkusza CKE do formatu importu (import przyjmuje
  ustrukturyzowany pakiet ZIP, sekcja 12.9);
- powiadomienia push i e-mail o ocenach (wyłącznie powiadomienia w aplikacji;
  e-mail służy tylko do resetu hasła);
- wpisywanie Rozwiązania zadania otwartego z klawiatury — Rozwiązanie to
  zawsze zdjęcie kartki (ADR-024); Poprawki Transkrypcji (FR-18) obejmują
  wyłącznie fragmenty błędnie odczytane: zmianę istniejących kroków i
  wstawienie najwyżej 2 kroków pominiętych przez Agenta rozpoznającego, w
  limitach ADR-036;
- usuwanie kont Nauczycieli i Administratorów jest **w zakresie** (RODO
  dotyczy wszystkich ról, ADR-044), mimo że NFR-09 wymienia wprost Uczniów.

---

## 2. Założenia

| ID | Założenie | Skutek, jeśli fałszywe |
|---|---|---|
| A-01 | Jev udostępnia API HTTP/JSON z trzema typami pytań: `noul` (tak/nie z prawdopodobieństwem), `score` (rozkład na liczbach całkowitych w zadanym zakresie), `choice` (rozkład na zamkniętej liście opcji); stan wejściowy to tekst ≤ 32 000 znaków; **żądanie wsadowe** (`questions[]`, do 32 pytań o jednym stanie) z p95 ≤ 800 ms na żądanie; limit szybkości ≥ 50 żądań HTTP/s na klucz API, liczony jako średnia w oknie ≥ 5 s **albo** z tolerancją chwilowego przekroczenia do 75 żądań w oknie 1 s (wersja 2.3, C3-04; od wersji 2.4, C4-05: łączna pojemność kubełków wszystkich procesów `B_całk = 34` niezależnie od liczby replik, więc w dowolnym oknie 1 s ≤ 34 + 40 = 74 wywołania przy 2, 3 i 4 workerach — 13.2). Wymagania dla dostawcy: tryb wsadowy i tolerancja przekroczenia (obie sprawdzane w G-4); przy ścisłym oknie 1 s bez tolerancji `B_całk = 10` (kubełek workera 4 przy 2 workerach, 13.2), a budżet trybu pojedynczego rośnie o 1,1 s (17.2). | Bez trybu wsadowego `JevAdapter` (13.2) działa w **trybie pojedynczym**: jedno pytanie na żądanie, ≤ 16 równolegle na Ocenę, plan w 3 falach (p95 3,6 s zamiast 1,6 s — budżet 17.2 i scenariusze S-1 … S-7 są liczone dla trybu pojedynczego jako najgorszego), szczyt „koniec lekcji” do 44 żądań/s (17.4). Globalny ogranicznik szybkości (13.2, ADR-039) utrzymuje ruch ≤ 80% limitu klucza; Ocena, dla której zabrakło przydziału, idzie do modelu zapasowego bez otwierania obwodu. Przy braku dostępu do Jev cały ruch obsługuje model zapasowy (ADR-009). |
| A-02 | Jev jest we wczesnym dostępie — system musi działać produkcyjnie **bez** Jev. | Model zapasowy jest pełnoprawną, osobno skalibrowaną implementacją portu `ModelDecyzyjny`. |
| A-03 | Skala pilotażu: 1 000 Uczniów, 50 Nauczycieli, 1 500 Rozwiązań zadań otwartych na dobę (20% z dowodem), szczyt 300 Rozwiązań/h wieczorem oraz szczyt „koniec lekcji”: 90 przesłań w ciągu 60 s (3 klasy jednocześnie), 30 jednoczesnych ocen. | Architektura (modularny monolit, prace w tle w PostgreSQL) wystarcza do ok. 20× tej skali (17.4); model obciążenia Nauczycieli w 17.7. |
| A-04 | Obowiązuje formuła matury od 2023 r. (podstawa programowa 2018) z 13 Działami (sekcja 7.4). | Działy i Umiejętności są danymi (tabele `dzial`, `umiejetnosc`), zmiana nie wymaga zmian kodu. |
| A-05 | Kryteria oceniania CKE da się zapisać jako uporządkowaną listę kryteriów z całkowitą liczbą punktów, pogrupowaną w warianty (sposoby rozwiązania). | Kryterium ma pole `wariant`; Model decyzyjny najpierw wybiera wariant (pytanie Q-OC-00). |
| A-06 | Uczniowie mają ukończone 16 lat (wiek zgody cyfrowej w Polsce). Rejestracja wymaga oświadczenia. | Dla osób < 16 lat potrzebny byłby przepływ zgody rodzica — poza zakresem. |
| A-07 | Dostawcy modeli generatywnych i Jev przetwarzają dane w EOG lub na podstawie SCC, z umową powierzenia, wyłączonym trenowaniem na danych oraz **retencją danych wejściowych**: zero (tryb „zero data retention”) dla roli `ROZPOZNAWANIE_PISMA` (zdjęcia) i ≤ 30 dni (wyłącznie monitorowanie nadużyć) dla pozostałych ról. | Bez tego nie wolno wysyłać Transkrypcji ani zdjęć do dostawcy (sekcja 16); konfiguracja modelu z dostawcą bez potwierdzonej retencji nie może zostać aktywowana (pole `parametry.retencja_dostawcy_dni`, 13.1, G-6). |
| A-08 | Wybrany multimodalny LLM obsługuje wejście obrazowe, wyjście zgodne ze schematem JSON, p95 ≤ 9 s dla 3 zdjęć, p95 ≤ 4 s dla wywołania Sprawdzenia zgodności poprawek (≤ 6 wycinków kroków + zdjęcie poglądowe z dłuższym bokiem 1024 px, 10.3.3), limit ≥ 150 żądań/min (wersja 2.2: ≥ 120; w szczycie „koniec lekcji” rozpoznanie 90/min + Sprawdzenie zgodności poprawek ≤ 27/min, 17.4; C3-05) oraz **odsetek wywołań zakończonych błędem lub timeoutem 14 s ≤ 0,3%** (każde takie wywołanie oznacza naruszenie NFR-01, scenariusz S-7 w 17.2; razem z S-3 i T-6 mieści się w 1% SLO). | Wymagane przy wyborze dostawcy dla roli `ROZPOZNAWANIE_PISMA`; niższa wydajność łamie budżet NFR-01 (17.2). |
| A-09 | Przed startem produkcyjnym powstaje zbiór testowy **ZT-1**: ≥ 1 050 odręcznych Rozwiązań z co najmniej 10 Działów — **≥ 700 zadań otwartych obliczeniowych i ≥ 350 zadań otwartych z dowodem** (oba rodzaje zadań otwartych) — każde ocenione niezależnie przez 2 Nauczycieli, rozbieżności rozstrzyga trzeci; ≥ 150 kartek z wpisanym imieniem i nazwiskiem (wersja 2.2: ≥ 50; 0 wystąpień na 50 kartkach daje górną granicę Wilsona 95% równą 7,1%, na 150 — 2,5%), w tym ≥ 30 kartek z imieniem lub nazwiskiem krótszym niż 4 znaki (np. Ewa, Jan, Lis) i ≥ 30 kartek z nazwiskiem równym terminowi matematycznemu (np. Pole, Kula, Suma, Prosta) — kartki są pisane na kontach testowych o zadanym imieniu i nazwisku (test Filtra danych osobowych, ADR-042, C3-02). Liczności wynikają z dwóch analiz mocy w 11.5.1: kryterium progu NFR-03 (tabela 1, zadania obliczeniowe) i kryterium zgodności G-1(a) (tabela 2, oba rodzaje). | Bez ZT-1 nie da się zmierzyć NFR-02 ani wyznaczyć początkowego Progu pewności — start produkcyjny jest zablokowany (bramka G-1). Gdy ZT-1 nie pozwala wyznaczyć progu spełniającego NFR-03, para (model, rodzaj) startuje w trybie pełnej weryfikacji (11.5.1). |
| A-10 | Klient to nowoczesna przeglądarka (Chrome/Safari/Firefox/Edge — dwie ostatnie wersje główne), obsługująca `input capture`. | Starsze przeglądarki dostają komunikat o braku wsparcia. |
| A-11 | **Interpretacja NFR-01 (do akceptacji w G-0):** „ocena rozwiązania” = czas pracy systemu nad Rozwiązaniem: rozpoznanie pisma (od ostatniego przesłania zdjęć do Transkrypcji gotowej) **plus** ocena (od zatwierdzenia Transkrypcji do Oceny widocznej dla Ucznia). Z pomiaru wyłączony jest wyłącznie czas, w którym Uczeń sprawdza i poprawia Transkrypcję (FR-18) — tego czasu system nie kontroluje. Zobowiązanie mierzalne: ≥ 99% Rozwiązań w miesiącu z czasem widocznym dla Ucznia ≤ 30 s (metryka serwerowa `czas_systemowy_ms` ≤ 28 s plus odpytywanie klienta co 1 s), p95 ≤ 20 s (≤ 23 s przy pracy na modelu zapasowym); skierowanie do Weryfikacji bez Oceny (T-6) i błąd rozpoznawania z winy dostawcy liczą się jako **naruszenie**, nie jako sukces (17.1, ADR-025). Wartość 100% nie jest gwarantowana architektonicznie, bo zależy od zewnętrznych dostawców modeli; każde przekroczenie i tak kończy się Oceną (termin twardy 120 s), a nie brakiem Oceny. | Jeśli właściciel produktu wymaga 100% ≤ 30 s, konieczna jest rezygnacja z zewnętrznego rozpoznawania pisma na rzecz modelu hostowanego we własnej infrastrukturze z gwarantowaną przepustowością (poza budżetem pilotażu) — decyzja w G-0. |
| A-12 | W pilotażu co najwyżej 20% aktywnych Uczniów nie należy do żadnej Klasy (rekrutacja pilotażu przez szkoły). | Model obciążenia kolejki globalnej (17.7) i bramka G-7 są przeliczane co tydzień z rzeczywistego udziału; przekroczenie uruchamia kontrolę przyjęć (11.4.3). |
| A-13 | Średni czas Weryfikacji: dowód 5 min, zadanie obliczeniowe 3 min, Kontrola losowa 2 min, zgłoszenie błędu odczytu 3 min (pomiar w pilotażu: `weryfikacja_czas_pracy_sekundy`). Weryfikator globalny deklaruje co najmniej 90 min pracy w każdym dniu roboczym (`nauczyciel.deklarowane_minuty_weryfikacji`, A-15). | Model 17.7 jest przeliczany z wartości zmierzonych; G-7 używa wartości zmierzonej, gdy jest dostępna. |
| A-14 | **Pula weryfikatorów globalnych (do akceptacji w G-0, ADR-034, ADR-043):** 50 Nauczycieli z A-03 to 40 prowadzących Klasy i 10 bez Klas. 10 Nauczycieli bez Klas nie wystarcza na G-7 (start: 18 weryfikatorów, model tygodniowy z eskalacjami 17.7), więc pulę tworzą: (a) Nauczyciele bez Klas z A-03 oraz (b) Nauczyciele współpracujący z platformą na podstawie umowy (rekrutowani i wynagradzani przez operatora platformy), w liczbie uzupełniającej do wartości z 17.7. Nauczyciel prowadzący Klasę może być weryfikatorem globalnym tylko wtedy, gdy prognoza jego kolejki `KLASA` na dzień roboczy (17.7) + `deklarowane_minuty_weryfikacji` ≤ 180 min (walidacja w `PATCH /admin/uzytkownicy/{id}`, `422`). Na start: 10 Nauczycieli bez Klas + ≥ 8 Nauczycieli współpracujących. | Bez rekrutacji Nauczycieli współpracujących bramka G-7 nie jest spełniona i start produkcyjny dla Uczniów bez Klasy jest zablokowany: rejestracja Ucznia bez kodu zaproszenia jest wtedy wyłączona flagą `LEMAT_REJESTRACJA_BEZ_KLASY = false` (Uczeń bez Klasy nie powstaje, więc kolejka `GLOBALNA` obsługuje tylko eskalacje z Klas; wtedy wymagani są weryfikatorzy wyłącznie dla eskalacji: start 4, 17.7). |
| A-15 | **Rytm pracy Nauczycieli (do akceptacji w G-0, ADR-043):** Nauczyciele pracują w dni robocze (poniedziałek–piątek z wyłączeniem dni ustawowo wolnych z tabeli `dzien_wolny`); deklarowane minuty dotyczą dnia roboczego; SLA Weryfikacji = 3 dni robocze (`dodaj_dni_robocze`, 7.3); 5% Weryfikacji kolejki `KLASA` jest eskalowanych do `GLOBALNA` (`odsetek_eskalacji_7d`). Rozwiązania przesyłane są przez 7 dni w tygodniu z jednakowym wolumenem dobowym (A-03). | Odsetek eskalacji > 10% w 7 dniach: model 17.7 i G-7 używają wartości zmierzonej, włącza się kontrola przyjęć dla Klas Nauczycieli z odsetkiem eskalacji ≥ 20% (11.4.3) i alert `ALERT_WERYFIKACJE`. Weryfikatorzy pracujący także w weekendy zwiększają pojemność ponad model (model zakłada najgorszy przypadek 5/7). |

---

## 3. Język wszechobecny

### 3.1 Reguły nazewnictwa

1. Nazwy domenowe w kodzie, bazie i API to **pojęcia ze słownika** zapisane
   ASCII bez polskich znaków: `snake_case` w bazie/JSON/Pythonie,
   `PascalCase` dla klas, `kebab-case` w ścieżkach URL, `UPPER_SNAKE` dla
   wartości wyliczeniowych. Reguła obejmuje także klucze JSON schematów
   odpowiedzi modeli, parametry zapytań i nazwy prac w tle.
2. Kolumny czysto techniczne mają nazwy angielskie, ujednolicone w całym
   schemacie: `id`, `created_at`, `updated_at`, `version` (licznik
   optymistycznej blokady). Nie są pojęciami domenowymi.
3. Słowo **„zadanie” jest zarezerwowane** dla pojęcia *Zadanie* ze słownika.
   Jednostka pracy asynchronicznej nazywa się **praca w tle** (`praca_w_tle`),
   nigdy „zadanie”, „task” ani „job” w kodzie domenowym. **Transliteracja
   „żądanie” (C3-07):** zapis ASCII z p. 1 zamienia „żądanie” na `zadanie`,
   dlatego słowo „żądanie” nie występuje w identyfikatorach: wniosek
   użytkownika o czynność (np. o usunięcie konta) = **wniosek**
   (`WNIOSEK_O_USUNIECIE`); żądanie HTTP do modelu lub dostawcy =
   **wywołanie** (`jev_wywolania_http_total`). W prozie dokumentu i w
   tekstach UI słowo „żądanie” jest dozwolone. Test CI (3.4, część 3) pilnuje
   tokenów `zadan*`.
4. Zakazane synonimy: „student”, „task”, „submission”, „grade”, „score”
   (poza nazwą typu pytania Jev `SCORE`), „difficulty”, „topic”, „skill”,
   „lesson”, „review” (w znaczeniu weryfikacji).
5. **Jedno słowo — jedno znaczenie.** Słowa o znaczeniu domenowym (Odpowiedź,
   Wariant, Krok, Wynik, Kolejka) są używane wyłącznie w znaczeniu z 3.2/3.3.
   Dla pojęć technicznych o podobnym sensie stosuje się nazwy rozłączne:
   odpowiedź modelu = **decyzja składowa** (`DecyzjaSkladowa`); rezultat
   operacji technicznej = **rezultat** (`RezultatOceny`,
   `RezultatRozpoznawania`, `rezultat_walidacji`); wynik porównania SymPy =
   **równoważność** (`Rownowaznosc`); grupa eksperymentu A/B = **grupa
   eksperymentu** (`procent_grupy_eksperymentu`), nigdy „wariant”; pozycja w
   Kryteriach oceniania = **kryterium** (`nr_kryterium`), nigdy „krok”; rodzaj
   kolejki = **typ kolejki** (`typ_kolejki`); odpowiedź HTTP = **status i
   ciało HTTP** (`kod_statusu_http`, `cialo_http`); opcje i klucz Zadania
   zamkniętego = **opcje zamknięte** (`opcje_zamkniete`); postać pliku
   zdjęcia = **wersja zdjęcia** (`?wersja_zdjecia=`); mechanizm pracy
   asynchronicznej = **prace w tle** (nigdy „kolejka”); punkty i klucz
   pokazywane po Odpowiedzi = **rezultat** (`OdpowiedzWyjscie.rezultat`,
   `rezultat_ujawniony_od`); odrzucony zapis przestarzałego obliczenia =
   **rezultat przestarzały** (`rezultat_przestarzaly`); możliwość wyboru w
   decyzji właściciela produktu = **opcja** (23.4), nigdy „wariant”.
   **Zdjęcie** (wersja 2.4, C4-04) oznacza wyłącznie fotografię kartki
   (`zdjecie_rozwiazania`, `?wersja_zdjecia=`, `ObszarNaZdjeciu`); wyłączenie
   mechanizmu ochronnego przez Administratora (bezpiecznika) =
   **dezaktywacja** (`POST /admin/bezpieczniki/dezaktywacja`,
   `dezaktywowany_przez`, `dezaktywowano_at`, `uzasadnienie_dezaktywacji`);
   odebranie uprawnienia (flagi) = **odebranie** (np. „flagi
   `weryfikator_globalny` są odbierane w `USUN_KONTO`”); wyłączenie flagi
   danych (np. `diagnostyczne`) = **wyłączenie**. W prozie dokumentu
   czasownik „zdjąć” i rzeczownik „zdjęcie” w innym znaczeniu niż fotografia
   nie występują. **Wstrzymanie** ma jedno znaczenie: czasowe zatrzymanie
   skutku do zdarzenia rozstrzygającego, bez zmiany stanu obiektu i
   odwracalne bez strat; identyfikator zawsze nazywa obiekt i skutek:
   `wstrzymanie_w_doborze` (Zadanie poza doborem do rozpatrzenia Zgłoszeń),
   `WSTRZYMANA_BLAD_ODCZYTU` (punkty Oceny wstępnej niewidoczne do
   Weryfikacji, ADR-022), procesy konta wstrzymane w okresie karencji
   (ADR-040; tylko proza, bez identyfikatora). Zatrzymanie innego rodzaju
   (np. blokada obniżania progu) nie jest nazywane wstrzymaniem.
6. **Dopuszczone terminy techniczne** (jedyne wyjątki od reguły 1 poza
   kolumnami z reguły 2): `theta` (θ — parametr umiejętności w modelu Elo),
   `trudnosc_b` (b — parametr trudności Zadania w modelu Elo), `ipw`
   (inverse probability weighting — waga odwrotności prawdopodobieństwa
   doboru), `ece` (Expected Calibration Error), `n_eff` (efektywna liczność
   próby), `cv5` (5-krotna walidacja krzyżowa), `sha256`, `latex`, `sympy`,
   `jwt`, `url`, `mime`, `trace_id`, `hmac`, `fnv1a`, `http`, `rodo`; prefiksy obiektów bazy
   `ux_` (indeks unikalny), `ix_` (indeks), `fk_` (klucz obcy), `trg_`
   (trigger). Każdy z nich ma definicję w PZS-1 (3.4, część C).

### 3.2 Pojęcia słownika → identyfikatory

| Pojęcie (słownik) | Tabela / typ | Klasa domenowa | Zasób API |
|---|---|---|---|
| Uczeń | `uczen` | `Uczen` | `/uczniowie` |
| Nauczyciel | `nauczyciel` | `Nauczyciel` | — (zasoby Nauczyciela: `/klasy`, `/weryfikacje`, `/przypisania`) |
| Zadanie | `zadanie` + `zadanie_wersja` | `Zadanie`, `ZadanieWersja` | `/zadania` |
| Zadanie bazowe | `zadanie.pochodzenie IN ('BAZOWE_ARKUSZ_CKE','BAZOWE_NAUCZYCIEL')`; `BAZOWE_ARKUSZ_CKE` tworzy wyłącznie import Arkusza CKE (Administrator), `BAZOWE_NAUCZYCIEL` — wyłącznie `POST /zadania` (rola Nauczyciel) | `Zadanie.czy_bazowe` | filtr `pochodzenie` |
| Zadanie wygenerowane | `zadanie.pochodzenie = 'WYGENEROWANE'` | `Zadanie.czy_wygenerowane` | filtr `pochodzenie` |
| Dział | `dzial` | `Dzial` | `/dzialy` |
| Umiejętność | `umiejetnosc` | `Umiejetnosc` | `/umiejetnosci` |
| Profil ucznia | `profil_ucznia` + `opanowanie_umiejetnosci` | `ProfilUcznia` | `/uczniowie/{id}/profil` |
| Poziom trudności | `kalibracja_trudnosci.poziom_trudnosci` (1–5, skalibrowany na wynikach Uczniów); wartość startowa `zadanie.poziom_trudnosci_nominalny` (PZS-1 B-4) | `PoziomTrudnosci` | pola `poziom_trudnosci`, `poziom_trudnosci_nominalny` |
| Ścieżka nauki | `sciezka_nauki` + `element_sciezki` (każdy element zaproponował Agent rekomendujący; PZS-1 B-3) | `SciezkaNauki` | `/uczniowie/ja/sciezka-nauki` |
| Wkładka teoretyczna | `wkladka_teoretyczna` | `WkladkaTeoretyczna` | `/wkladki-teoretyczne` |
| Rozwiązanie | `rozwiazanie` + `zdjecie_rozwiazania` | `Rozwiazanie` | `/rozwiazania` |
| Transkrypcja | `transkrypcja` + `krok_transkrypcji` (źródła: `AGENT_ROZPOZNAJACY`, `POPRAWKA_UCZNIA` — PZS-1 B-1) | `Transkrypcja` | `/rozwiazania/{id}/transkrypcja` |
| Kryteria oceniania | `kryterium_oceniania` (wiersz = jedno kryterium z listy, numer `nr_kryterium`) | `KryteriumOceniania` | pole `kryteria_oceniania` |
| Ocena | `ocena` + `ocena_kryterium`; uzasadnienie: `ocena.uzasadnienie` (eksponowane w `OcenaWyjscie`) | `Ocena` | `/rozwiazania/{id}/ocena` |
| Pewność oceny | `ocena.pewnosc_oceny` (prawdopodobieństwo zgodności Oceny w rozumieniu ADR-027) | `PewnoscOceny` | pole `pewnosc_oceny` |
| Próg pewności | `prog_pewnosci` (wyłącznie wartość progu, jego źródło i uzasadnienie) | `ProgPewnosci` | `/admin/progi-pewnosci` |
| Weryfikacja | `weryfikacja` (PZS-1 B-2) | `Weryfikacja` | `/weryfikacje` |
| Agent | moduł `lemat.agenci.*` | `Agent*` | — |
| Orkiestrator | moduł `lemat.orkiestrator` (fasada wywoływana w procesach `worker` i `api`, ADR-023) | `Orkiestrator` | — |
| Model System One | adapter `JevAdapter`, rola `DECYZYJNY_GLOWNY` (Jev jest jedynym Modelem System One w systemie) | `JevAdapter` | — |
| Jev | adapter `JevAdapter`, `konfiguracja_modelu.dostawca = 'typesafe-jev'` | `JevAdapter` | — |
| Kalibracja | moduł `lemat.kalibracja`; tabele `kalibrator_pewnosci` (funkcja kalibrująca Pewność oceny), `kalibracja_trudnosci` (kalibracja Poziomu trudności), `probka_kalibracyjna` | `KalibratorPewnosci`, `KalibracjaTrudnosci` | `/admin/kalibracja` |
| Model generatywny (LLM) | port `ModelGeneratywny` | `ModelGeneratywny` | — |
| Arkusz CKE | `arkusz_cke` | `ArkuszCke` | `/admin/arkusze-cke` |

Port `ModelDecyzyjny` odpowiada pojęciu uzupełniającemu **Model decyzyjny**
(3.3) — roli, którą pełni Model System One albo model zapasowy. Pojęcie
słownikowe „Model System One” nie jest mapowane na port, bo model zapasowy
(LLM) nim nie jest.

### 3.3 Pojęcia uzupełniające

Poniższe pojęcia występują w wymaganiach lub są konieczne do modelowania. Są
używane wyłącznie w podanym znaczeniu. Każde z nich jest objęte propozycją
dopisania do słownika (PZS-1, część A).

| Pojęcie | Źródło | Definicja | Identyfikator |
|---|---|---|---|
| Administrator | requirements: Aktorzy | Osoba zarządzająca kontami, bazą zadań i konfiguracją modeli. | `ADMINISTRATOR` |
| Klasa | FR-30 | Grupa Uczniów prowadzona przez jednego Nauczyciela. | `klasa` |
| Rodzaj zadania | requirements: Rodzaje zadań | `ZAMKNIETE`, `OTWARTE_OBLICZENIOWE`, `OTWARTE_Z_DOWODEM`. | `rodzaj_zadania` |
| Odpowiedź | FR-15 | Wybór zaznaczony przez Ucznia w Zadaniu zamkniętym (nie jest Rozwiązaniem). Słowo nie jest używane dla odpowiedzi modelu (patrz Decyzja składowa). | `odpowiedz` |
| Decyzja składowa | architecture.md | Odpowiedź Modelu decyzyjnego na jedno pytanie: wybrana wartość (moda) i rozkład prawdopodobieństw. | `DecyzjaSkladowa`, `decyzja_modelu` |
| Poziom matury | FR-02 | `PODSTAWOWY` lub `ROZSZERZONY`. | `poziom_matury` |
| Termin egzaminu | FR-02 | Data egzaminu maturalnego Ucznia. | `termin_egzaminu` |
| Test diagnostyczny | FR-03 | Jednorazowy (per poziom matury) zestaw Zadań zamkniętych inicjujący Profil ucznia. | `test_diagnostyczny` |
| Zestaw zadań | FR-32 | Uporządkowana lista Zadań utworzona przez Nauczyciela. | `zestaw_zadan` |
| Przypisanie | FR-32 | Zlecenie Zestawu zadań wskazanym Uczniom z terminem. | `przypisanie` |
| Rozwiązanie wzorcowe | słownik (def. Zadania) | Poprawne rozwiązanie Zadania rozpisane na kroki, w wariantach. | `rozwiazanie_wzorcowe` |
| Wariant rozwiązania | A-05 | Jeden z dopuszczonych przez Kryteria oceniania sposobów rozwiązania. Słowo „wariant” nie oznacza grupy eksperymentu. | `wariant` |
| Krok rozwiązania | FR-17 | Jeden logiczny etap w Transkrypcji (linia przekształcenia). Numer: `krok_transkrypcji.nr`. Pozycja Kryteriów oceniania to **kryterium**, nie krok. | `krok_transkrypcji` |
| Kryterium | słownik (def. Kryteriów oceniania) | Jedna pozycja Kryteriów oceniania (opis + punkty); numer `nr_kryterium`. | `kryterium_oceniania` |
| Wynik końcowy | FR-20, FR-25 | Ostateczna odpowiedź w Rozwiązaniu Ucznia (np. zbiór rozwiązań równania). | `wynik_koncowy` (tylko w stanie dla modelu) |
| Wynik końcowy wzorcowy | słownik (def. Zadania) | Wynik końcowy Rozwiązania wzorcowego, zapisany także w postaci SymPy. | `zadanie_wersja.wynik_koncowy_wzorcowy` |
| Typ błędu | FR-27 | `RACHUNKOWY`, `MERYTORYCZNY`, `NIEPELNE_ROZWIAZANIE`. Błąd odczytu nie jest typem błędu Ucznia (FR-28). | `typ_bledu` |
| Wskazówka | FR-27, NFR-05 | Krótki tekst Agenta wyjaśniającego (albo Nauczyciela), bez gotowego rozwiązania. | `wskazowka` |
| Ocena wstępna | FR-24 | Ocena o statusie `WSTEPNA`, widoczna dla Ucznia do czasu Weryfikacji; przy podejrzeniu błędu odczytu jej punkty są wstrzymane (ADR-022). | `status_oceny.WSTEPNA` |
| Ocena ostateczna | FR-23 | Ocena o statusie `OSTATECZNA` (automatyczna albo po Weryfikacji). | `status_oceny.OSTATECZNA` |
| Kolejka weryfikacji | FR-33 | Lista Weryfikacji oczekujących na Nauczyciela. Typ kolejki: `KLASA` albo `GLOBALNA`. | `GET /weryfikacje`, `typ_kolejki_weryfikacji` |
| Powód weryfikacji | FR-33 | Przyczyna skierowania do Weryfikacji (11.3, 11.6). | `powod_weryfikacji` |
| Kontrola losowa | NFR-03, NFR-04 | Weryfikacja losowej próby ocen wystawionych automatycznie w celu pomiaru trafności i Kalibracji. | `KONTROLA_LOSOWA` |
| Zgłoszenie błędu odczytu | FR-28 | Wniosek Ucznia o Weryfikację, bo Ocena opiera się na błędnie odczytanym fragmencie pisma. | `ZGLOSZENIE_BLEDU_ODCZYTU` |
| Próbka kalibracyjna | FR-34 | Para (Ocena automatyczna, Ocena Nauczyciela) używana do Kalibracji. | `probka_kalibracyjna` |
| Zgodność Oceny | NFR-02, słownik (Pewność oceny) | Ocena automatyczna jest zgodna z Oceną Nauczyciela, gdy ma tę samą liczbę punktów, ten sam pierwszy błędny krok (lub oba brak) i tę samą wartość `brakujacy_krok` (ADR-027). | `probka_kalibracyjna.zgodna` |
| Kalibrator pewności | słownik (Kalibracja) | Monotoniczna funkcja przekształcająca pewność surową w Pewność oceny, dopasowana na Próbkach kalibracyjnych. | `kalibrator_pewnosci` |
| Opinia pomocnicza | architecture.md | Niezależna ocena mocniejszego LLM dołączona do Weryfikacji jako materiał dla Nauczyciela; nie zmienia Oceny. | `opinia_pomocnicza` |
| Model decyzyjny | architecture.md | Rola pełniona przez Model System One (Jev) albo model zapasowy (NFR-07). | port `ModelDecyzyjny` |
| Model zapasowy | NFR-07 | LLM ze strukturyzowaną odpowiedzią implementujący port `ModelDecyzyjny`. | rola `DECYZYJNY_ZAPASOWY` |
| Plan pytań | architecture.md (pytania do Jev) | Zbiór pytań do Modelu decyzyjnego z jawnymi zależnościami między nimi (ADR-026). | `PlanPytan` |
| Rekomendacja | FR-05, FR-10 | Decyzja Agenta rekomendującego o kolejnym elemencie Ścieżki nauki wraz z uzasadnieniem. | `rekomendacja` |
| Powtórka | FR-08 | Cel elementu Ścieżki nauki: Zadanie z Umiejętności już opanowanej. Powtórka nie jest osobnym typem elementu. | `cel_elementu.POWTORKA` |
| Seria | vision.md | Ciąg kolejnych elementów Ścieżki nauki z jednej Umiejętności o niemalejącym Poziomie trudności, rozpoczynany po Wkładce teoretycznej i kończony po rozwiązaniu Zadania na poziomie maturalnym (ADR-030). | `sciezka_nauki.seria_*` |
| Poziom opanowania | słownik (def. Profilu ucznia) | Liczba z [0, 1]: szacowane prawdopodobieństwo poprawnego rozwiązania Zadania o Poziomie trudności 4 (maturalnym) z danej Umiejętności. | `poziom_opanowania` |
| Wynik zadania | FR-29 | Znormalizowany rezultat (punkty / punkty maksymalne) Odpowiedzi lub Oceny, zasilający Profil ucznia. | `wynik_zadania` |
| Walidacja zadania | FR-13 | Automatyczne sprawdzenie Zadania wygenerowanego przed udostępnieniem. | `walidacja_zadania` |
| Przegląd zadania | FR-14 | Decyzja Nauczyciela uprawnionego (ADR-028) lub Administratora o Zadaniu (zatwierdzenie, poprawka, odrzucenie). | `przeglad_zadania` |
| Recenzent treści | FR-14, ADR-028 | Nauczyciel z uprawnieniem nadanym przez Administratora do Przeglądu Zadań i Wkładek o zasięgu globalnym. | `nauczyciel.recenzent_tresci` |
| Zasięg zadania | ADR-028 | `GLOBALNY` (w doborze rekomendacji dla wszystkich) albo `PRYWATNY` (tylko w Przypisaniach autora). | `zasieg_zadania` |
| Konfiguracja modelu | requirements: Administrator | Przypisanie dostawcy, modelu i wersji promptu do roli modelu. | `konfiguracja_modelu` |
| Praca w tle | technika | Jednostka pracy asynchronicznej Orkiestratora. | `praca_w_tle` |
| Element Ścieżki nauki | słownik (def. Ścieżki nauki) | Jedna pozycja Ścieżki nauki: Wkładka teoretyczna albo Zadanie zaproponowane przez Agenta rekomendującego, ze statusem i terminem ważności (8.5). | `element_sciezki` |
| Cel elementu | FR-08 | `NAUKA` albo `POWTORKA` — po co Agent rekomendujący zaproponował element. | `cel_elementu` |
| Przesłanie | FR-16 | Jednorazowe wysłanie kompletu 1–5 zdjęć do Rozwiązania; Rozwiązanie ma ≤ 3 przesłania. | `przeslanie_nr`, `liczba_przeslan` |
| Zdjęcie maskowane | NFR-09, ADR-031 | Kopia zdjęcia kartki z zamalowanymi obszarami danych osobowych, pokazywana weryfikatorowi globalnemu spoza Klasy Ucznia. | `klucz_obiektu_maskowany` |
| Kopia treningowa | NFR-09, 16.4 | Kopia zdjęcia i Transkrypcji w zbiorze treningowym, wykonana wyłącznie za zgodą Ucznia, usuwana po odwołaniu zgody lub usunięciu konta. | `kopia_treningowa` |
| Weryfikator globalny | ADR-021, ADR-034 | Nauczyciel z uprawnieniem do obsługi kolejki `GLOBALNA` (Uczniowie bez Klasy), z deklarowanym czasem pracy i umową poufności. | `nauczyciel.weryfikator_globalny` |
| Nauczyciel docelowy | 11.4.1 | Nauczyciel, któremu Weryfikacja została przydzielona przy utworzeniu (Nauczyciel Przypisania albo Klasy Ucznia). | `weryfikacja.nauczyciel_docelowy_id` |
| Blokada weryfikacji | FR-33, ADR-033 | Wyłączne prawo jednego Nauczyciela do rozstrzygnięcia Weryfikacji przez 15 min (przedłużane, łącznie ≤ 2 h). | `weryfikacja.zablokowana_do` |
| Szkic rozstrzygnięcia | ADR-033 | Autozapisany, niezatwierdzony stan rozstrzygnięcia jednego Nauczyciela dla jednej Weryfikacji. | `szkic_rozstrzygniecia` |
| Pseudonim Ucznia | ADR-031 | Stały, nieodwracalny bez sekretu identyfikator Ucznia pokazywany weryfikatorowi globalnemu spoza jego Klasy. | `uczen_pseudonim` |
| Kontrola przyjęć | 11.4.3 | Ograniczenie udziału zadań otwartych w rekomendacjach: dla Uczniów bez Klasy, gdy zaległość kolejki globalnej przekracza jej pojemność, oraz dla Uczniów Klasy, której Nauczyciel ma `odsetek_eskalacji_7d ≥ 0,20` (bez eskalacji z powodu karencji) albo konto w okresie karencji (wersja 2.4, C4-06 p. 6). | `kontrola_przyjec_aktywna`, `klasy_z_kontrola_przyjec` |
| Tryb pełnej weryfikacji | 11.5.1 | Stan pary (model decyzyjny, rodzaj zadania), w którym Próg pewności = 1.000, więc każda Ocena trafia do Weryfikacji. | `prog_pewnosci.wartosc = 1.000` |
| Zawieszenie Wyniku zadania | FR-28, ADR-022 | Wyłączenie Wyniku zadania z Profilu ucznia na czas rozpatrywania Zgłoszenia błędu odczytu. | `wynik_zadania.powod_uniewaznienia = 'ZGLOSZENIE_BLEDU_ODCZYTU'` |
| Czas systemowy | NFR-01, A-11 | Czas pracy systemu nad Rozwiązaniem: rozpoznanie (przesłanie → Transkrypcja gotowa) plus ocena (zatwierdzenie → Ocena widoczna). | `rozwiazanie.czas_systemowy_ms` |
| Termin miękki / termin twardy | NFR-01, ADR-025 | Termin miękki: chwila, do której Ocena powinna powstać, by spełnić NFR-01 (skraca timeouty). Termin twardy: zatwierdzenie + 120 s; po nim Rozwiązanie trafia do Weryfikacji bez Oceny (T-6). | `termin_miekki`, `termin_twardy` |
| Zlecenie generowania | FR-11 | Żądanie wygenerowania Zadania dla (Umiejętność, Poziom trudności, poziom matury, rodzaj), od Nauczyciela, Administratora albo systemu. | `zlecenie_generowania` |
| Grupa eksperymentu | 10.1.6 | Przydział Ucznia do jednej z dwóch grup eksperymentu A/B rekomendacji — grupy kontrolnej (strategia `POLITYKA_ELO`) albo grupy strategii eksperymentalnej (`JEV_CHOICE`) — na podstawie skrótu identyfikatora Ucznia. | `procent_grupy_eksperymentu` |
| Zbiór testowy ZT-1 | A-09 | Zbiór odręcznych Rozwiązań ocenionych przez Nauczycieli, używany do bramek G-1/G-2 i początkowej Kalibracji. | `zbiory-testowe/zt1/` |
| Rejestr Zdarzeń RODO | NFR-09, 16.4 | Niezmienialny (Object Lock) zapis Zdarzeń RODO poza bazą danych, używany do ponowienia wniosków o usunięcie, wykonanych usunięć, przywróceń i zmian zgody na trening po odtworzeniu bazy z kopii (ADR-041). Nazwa do wersji 2.2: „Rejestr usunięć” (C3-07). | bucket `rejestr-zdarzen-rodo` |
| Pewność surowa | FR-22, 11.2.1 | Minimum prawdopodobieństw decyzji składowych Oceny, ograniczone do 0.50 przy niespójności, przed Kalibracją. | `pewnosc_surowa` |
| Punkty surowe | FR-20, 11.1 | Suma punktów za kryteria przed zastosowaniem reguły FR-20. | `punkty_surowe` |
| Niespójność | 11.2.1 | Sprzeczność między decyzjami składowymi jednej Oceny (kody N-1 … N-6), która zawsze kieruje do Weryfikacji. | `niespojnosci`, `N-x` |
| Rozstrzygnięcie | FR-34 | Ocena Nauczyciela kończąca Weryfikację podjętą z Kolejki weryfikacji. | `/rozstrzygniecie`, `RozstrzygniecieWejscie` |
| Korekta | FR-34 | Ocena Nauczyciela wystawiona z poziomu Rozwiązania `OCENIONE`, zastępująca aktualną Ocenę (9.6.3). | `/korekty`, `KOREKTA_NAUCZYCIELA` |
| Eskalacja | 11.4.3 | Przeniesienie Weryfikacji z kolejki `KLASA` do `GLOBALNA` po terminie SLA albo po utracie Nauczyciela docelowego. | `eskalowano_at`, `odsetek_eskalacji_7d` |
| Poprawka Transkrypcji | FR-18, ADR-036 | Zmiana treści istniejącego kroku Transkrypcji albo wstawienie kroku pominiętego przez Agenta rozpoznającego, wykonana przez Ucznia przed zatwierdzeniem; usunięcie kroku nie jest Poprawką. | `POPRAWKA_UCZNIA`, `nr_kroku_agenta` |
| Zakres poprawek | ADR-036 | Miary rozległości Poprawek Transkrypcji względem odczytu Agenta rozpoznającego: liczba zmienionych i wstawionych kroków, odsetek zmienionych znaków. | `transkrypcja.zakres_poprawek` |
| Sprawdzenie zgodności poprawek | ADR-036 | Porównanie zmienionych kroków ze zdjęciem kartki przez Model generatywny w roli `ROZPOZNAWANIE_PISMA`; działa tylko w kierunku bezpiecznym (Z-1). | `transkrypcja.zgodnosc_poprawek`, `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` |
| Powody początkowe | 11.5.2 | Powody Weryfikacji w chwili dołączenia Oceny weryfikowanej; wyznaczają źródło Próbki kalibracyjnej i nie zmieniają się po dopisaniu zgłoszenia. | `weryfikacja.powody_poczatkowe` |
| Zgłoszenie błędu w Zadaniu | FR-14, ADR-038 | Wniosek Nauczyciela o Przegląd Zadania o zasięgu globalnym z opisem błędu, rozpatrywany przez recenzenta treści albo Administratora. | `zgloszenie_bledu_zadania` |
| Wstrzymanie w doborze | ADR-038 | Wyłączenie Zadania z doboru rekomendacji do czasu rozpatrzenia Zgłoszeń błędu w Zadaniu; stan systemowy przechowywany poza wierszem `zadanie` (bez zmiany `zadanie.version`, ADR-029, C3-09). | tabela `wstrzymanie_w_doborze` |
| Dzień roboczy | A-15 | Dzień od poniedziałku do piątku, który nie jest dniem ustawowo wolnym; jednostka SLA Weryfikacji. | `dzien_wolny`, `dodaj_dni_robocze` |
| Zdarzenie RODO | NFR-09, ADR-041 | Wniosek o usunięcie konta, anulowanie wniosku (przywrócenie), wykonanie usunięcia, odwołanie albo udzielenie zgody na trening; zapisywane w Rejestrze Zdarzeń RODO przed zmianą w bazie — przez `api` (zdarzenia z inicjatywy użytkownika) albo przez `worker` (wykonanie usunięcia, `zrodlo = WORKER`). | `zdarzenie_rodo`, `typ_zdarzenia_rodo`, `zrodlo_zdarzenia_rodo` |
| Okres karencji | NFR-09, ADR-040 | 30 dni między żądaniem usunięcia konta a usunięciem danych; procesy konta są w tym czasie wstrzymane, a nie anulowane. | `uzytkownik.usuniecie_zaplanowane_at` |
| Filtr danych osobowych | NFR-09, ADR-042 | Przetwarzanie Transkrypcji (odczytu i Poprawek) zastępujące dane identyfikacyjne Ucznia znacznikiem przed zapisem w bazie; nie usuwa kroków i nie zmienia LaTeX (wersja 2.3, C3-02); imię i nazwisko z konta nigdy nie są traktowane jako treść matematyczna, także gdy są terminem ze Słownika terminów matematycznych (wersja 2.4, C4-03). | `FiltrDanychOsobowych`, `[DANE_OSOBOWE]` |
| Przekazanie Klasy | FR-30, ADR-044 | Zmiana Nauczyciela prowadzącego Klasę przez Administratora, wymagana przed usunięciem konta Nauczyciela. | `POST /admin/klasy/{id}/przekazanie` |
| Bezpiecznik | NFR-03, ADR-046 | Automatyczna reakcja nadzoru na **dowód** przekroczenia 2% błędu ocen automatycznych w Kontroli losowej (dolna granica Wilsona 95% > 2% przy `n_eff ≥ 50`): bezpiecznik trafności podnosi Próg pewności, bezpiecznik poprawek kieruje każdą Transkrypcję z Poprawkami do Weryfikacji (T-7). | `stan_bezpiecznika`, `typ_bezpiecznika` |
| Wniosek o usunięcie konta | NFR-09, ADR-040 | Zdarzenie RODO rozpoczynające okres karencji usunięcia konta (`DELETE /uczniowie/ja`, `DELETE /nauczyciele/ja`, `POST /admin/uzytkownicy/{id}/usuniecie`). | `WNIOSEK_O_USUNIECIE` |
| Słownik terminów matematycznych | ADR-042 | Zamknięta lista form wyrazowych terminów matematycznych (z odmianą), których Filtr danych osobowych nie zastępuje, także gdy są równe imieniu lub nazwisku z konta — z dwoma wyjątkami (wersja 2.4, C4-03): token sąsiaduje z drugim członem imienia i nazwiska (lub inicjałem) albo leży w kroku agenta z podejrzeniem danych osobowych. | `lemat/rozpoznawanie/slownik_matematyczny.txt` |
| Obszar kroku | 10.3.2 | Ramka linii kroku Transkrypcji na znormalizowanym zdjęciu, używana przez Filtr danych osobowych i Sprawdzenie zgodności poprawek. | `krok_transkrypcji.obszar`, `ObszarKroku` |
| Przyjęcie planu | ADR-039 | Decyzja globalnego ogranicznika szybkości przed pierwszą falą Oceny: plan pytań Jev zostanie wykonany przed terminem miękkim albo Ocena od razu przechodzi do modelu zapasowego. | `PrzyjeciePlanu` |
| Dezaktywacja bezpiecznika | ADR-046, C4-04 | Decyzja Administratora z uzasadnieniem kończąca stan aktywny bezpiecznika (poprawek: koniec T-7 dla każdej Poprawki; trafności: koniec blokady obniżania progu) i ustawiająca nowy początek okna oceny `okno_od`. | `POST /admin/bezpieczniki/dezaktywacja`, `dezaktywowano_at` |
| Blokada obniżania progu | 11.5.3, C4-02 | Stan pary (konfiguracja, rodzaj zadania) po wyzwoleniu bezpiecznika trafności, w którym KALIBRUJ nie obniża Progu pewności, dopóki próba Kontroli losowych powyżej bieżącego progu nie wykaże UB Wilsona ≤ 2% przy `n_eff ≥ 189`. | `stan_bezpiecznika.aktywny` (typ `TRAFNOSC`) |
| Wstrzymanie | 3.1 p. 5, C4-04 | Czasowe zatrzymanie skutku (doboru Zadania, punktów Oceny wstępnej, procesów konta w karencji) do zdarzenia rozstrzygającego, bez zmiany stanu obiektu i odwracalne bez strat. | `wstrzymanie_w_doborze`, `WSTRZYMANA_BLAD_ODCZYTU` |

### 3.4 Propozycja zmiany słownika PZS-1

Status: **do akceptacji przez właścicieli słownika i właściciela produktu
(bramka G-0, 17.6)**. Po akceptacji treść części A i B jest dopisywana do
`docs/glossary.md` jednym commitem; do tego czasu implementacja może
powstawać, ale nie może zostać wdrożona produkcyjnie. Dokument nie edytuje
`docs/glossary.md` samodzielnie, bo słownik jest dokumentem źródłowym
właściciela produktu.

**Część A — nowe pojęcia.** Wszystkie wiersze tabeli 3.3 w podanym tam
brzmieniu (86 pojęć: 42 z wersji 2.0, 19 dopisanych w wersji 2.1 — od
„Element Ścieżki nauki” do „Rejestr Zdarzeń RODO” (do wersji 2.2 „Rejestr
usunięć”) —, 17 dopisanych w wersji 2.2 — od „Pewność surowa” do
„Przekazanie Klasy” —, 5 dopisanych w wersji 2.3 — od „Bezpiecznik” do
„Przyjęcie planu” — i 3 dopisane w wersji 2.4 — „Dezaktywacja
bezpiecznika”, „Blokada obniżania progu”, „Wstrzymanie”; razem 86 pojęć).

**Test w CI `tests/test_slownik.py`** (wersja 2.2, C2-11) ma cztery części,
każda z zamkniętą listą wyjątków w `tests/slownik_wyjatki.yaml`:

1. **Tabele:** nazwy tabel z DDL 7.3 (odczyt `information_schema.tables` w
   bazie testowej po migracjach) muszą mieć wpis w 3.2/3.3. Lista
   wyłączeń: tabele techniczne (`uzytkownik` — konto logowania wspólne dla
   ról, `token_odswiezania`, `proba_logowania`, `klucz_idempotencji`,
   `praca_w_tle`, `powiadomienie`, `dziennik_audytu`, `import_arkusza`,
   `limit_szybkosci_modelu`) oraz tabele łączące, których nazwa składa się
   wyłącznie z pojęć słownikowych (`czlonkostwo_klasy`, `pozycja_zestawu`,
   `przypisanie_uczen`, `pozycja_testu_diagnostycznego`,
   `umiejetnosc_wymagana`, `zadanie_umiejetnosc`, `zalacznik_zadania`,
   `ocena_kryterium`).
2. **Tokeny identyfikatorów:** kolumny (`information_schema.columns`), typy
   wyliczeniowe i ich wartości (`pg_enum`), ścieżki, parametry i właściwości
   schematów OpenAPI (`/api/v1/openapi.json`), klucze schematów JSON
   odpowiedzi modeli (`lemat/modele/schematy/*.json`) i typy prac w tle
   (13.3) są dzielone na tokeny (`_`, `-`, granice `PascalCase`); każdy token
   musi należeć do słownika tokenów `tests/slownik_tokeny.yaml`, generowanego
   z identyfikatorów 3.2/3.3, terminów technicznych 3.1 p. 2 i p. 6 oraz
   zamkniętej listy słów ogólnych (np. `nr`, `opis`, `status`, `liczba`,
   `termin`, `tresc`, `koszt`; od wersji 2.4: `dezaktywacja`,
   `dezaktywowany`, `dezaktywowano`, `dezaktywacji`, `blokada`,
   `obnizania`, `przydzial`, `laczna`, `wykonane`, `podpisu`).
3. **Słowa zarezerwowane (3.1 p. 3 i p. 5):** identyfikator zawierający token
   `odpowiedz*`, `wariant*`, `krok*`, `wynik*`, `kolejk*`, (od wersji 2.3,
   C3-07) `zadan*` albo (od wersji 2.4, C4-04) `zdjec*`, `zdjet*`, `zdje*`
   (formy „zdjęcie”, „zdjęto”, „zdjęty”) i `wstrzym*` musi być na liście
   dozwolonych użyć tego słowa (dla `zdje*` — wyłącznie znaczenie
   „fotografia kartki”: `zdjecie_rozwiazania`, `zdjecie_rozwiazania_id`,
   `zdjecia` w ścieżkach `/rozwiazania/{id}/zdjecia` i polach DTO,
   `zdjecie` w `ObszarNaZdjeciu`/`NiepewnyFragment`/`obszar`,
   `wersja_zdjecia`, `ZdjecieRef`, `ObszarNaZdjeciu`, `widoczny_na_zdjeciu`,
   `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`, `ZDJECIE_NIEPRAWIDLOWE`, `USUN_ZDJECIA`,
   `zdjecia_zalegle`, prefiksy `ux_zdjecie_*`, `ix_zdjecie_*`; tokeny
   `zdjeto`, `zdjety`, `zdjecia` w znaczeniu dezaktywacji nie mają
   dozwolonych użyć; dla `wstrzym*`: `wstrzymanie_w_doborze`,
   `WSTRZYMANA_BLAD_ODCZYTU`, pole DTO `wstrzymane_w_doborze` (wyliczane z
   tabeli `wstrzymanie_w_doborze`); dla
   `zadan*`
   `zadan*`: identyfikatory odnoszące się do pojęcia Zadanie — `zadanie`,
   `zadanie_id`, `zadanie_ids`, `zadanie_wersja`, `zadanie_wersja_id`,
   `zadanie_umiejetnosc`, `zadania` w ścieżkach `/zadania`, `rodzaj_zadania`,
   `status_zadania`, `pochodzenie_zadania`, `zasieg_zadania`, `wynik_zadania`,
   `walidacja_zadania`, `przeglad_zadania`, `zgloszenie_bledu_zadania`,
   `zestaw_zadan`, `udzial_zadan_otwartych`, `tresc_zadania_md`,
   `zgodnosc_z_zadaniem`, wartości `ZADANIE`, `ZADANIE_LATWE`,
   `ZADANIE_MATURALNE`, `ZADANIE_Z_DOWODEM`, `ZADANIE_DO_PRZEGLADU`, klasy
   `Zadanie*`, `*Zadania*` i prefiksy `ux_zadanie_*`, `ix_zadanie_*`; pełna lista
   w `tests/slownik_wyjatki.yaml`). Przypadek kontrolny testu (wersja 2.4):
   migracja testowa z kolumną `stan_bezpiecznika.zdjeto_at` i ścieżka
   `/admin/bezpieczniki/zdjecie` muszą kończyć test błędem. Przykłady list
   dozwolonych użyć (np. dla `wynik*`: `wynik_zadania`,
   `wynik_zadania_id`, `wynik_koncowy`, `wynik_koncowy_wzorcowy`,
   `ix_wynik_*`, `ux_wynik_*`, `WynikZadaniaSkrot`; dla `odpowiedz*`:
   `odpowiedz`, `odpowiedz_id`, `OdpowiedzWejscie`, `OdpowiedzWyjscie`,
   `OdpowiedzZapisana`, `ux_odpowiedz_*`); każde inne użycie kończy test
   błędem.
4. **Zakazane synonimy (3.1 p. 4)** w identyfikatorach i w plikach
   tłumaczeń UI.

**Część B — zmiany definicji istniejących pojęć.**

| ID | Pojęcie | Obecna definicja | Proponowana definicja | Przyczyna |
|---|---|---|---|---|
| B-1 | Transkrypcja | Tekstowa postać rozwiązania (tekst + LaTeX) uzyskana ze zdjęcia przez moduł rozpoznawania pisma. | Tekstowa postać rozwiązania (tekst + LaTeX) uzyskana ze zdjęcia przez moduł rozpoznawania pisma, **ewentualnie poprawiona przez Ucznia przed oceną (FR-18) w zakresie fragmentów odczytanych błędnie (Poprawka Transkrypcji: zmiana istniejących kroków, wstawienie ≤ 2 kroków, bez usuwania; ADR-036)**. | FR-18 wymaga poprawek Ucznia; limity Poprawek i Sprawdzenie zgodności poprawek ze zdjęciem gwarantują, że Transkrypcja nadal pochodzi ze zdjęcia. Wpis ręczny bez zdjęcia nie istnieje (ADR-024). |
| B-2 | Weryfikacja | Sprawdzenie oceny przez nauczyciela, gdy pewność systemu jest zbyt niska. | Sprawdzenie oceny przez nauczyciela, gdy pewność systemu jest zbyt niska **albo gdy wymagają tego reguły: poprawny wynik przy błędnych krokach (FR-25), zadanie z dowodem (FR-26), podejrzenie lub zgłoszenie błędu odczytu (FR-28), Poprawki Transkrypcji niezgodne ze zdjęciem lub zbyt rozległe (ADR-036), Kontrola losowa (NFR-03), błąd przetwarzania**. | Wymagania FR-25, FR-26 i NFR-03 wymuszają Weryfikację niezależnie od pewności; ADR-036 chroni FR-18 przed obejściem kartki. |
| B-3 | Ścieżka nauki | Kolejność wkładek teoretycznych i zadań zaproponowana uczniowi przez agenta rekomendującego. | Kolejność wkładek teoretycznych i zadań zaproponowana uczniowi przez agenta rekomendującego, **z uwzględnieniem Zadań przypisanych przez nauczyciela (agent decyduje, kiedy je zaproponować) oraz powtórek**. | Element z Przypisania jest proponowany przez agenta (reguła R-PRZYPISANIE), więc definicja się utrzymuje; dopisek usuwa niejednoznaczność. Zadania rozwiązywane bezpośrednio z listy Przypisań **nie są** elementami Ścieżki nauki (ADR-032). |
| B-4 | Poziom trudności | Skala trudności zadania (np. 1–5), kalibrowana na podstawie wyników uczniów. | Skala trudności zadania 1–5, kalibrowana na podstawie wyników uczniów; **wartość startowa (nominalna) pochodzi ze wskaźnika łatwości CKE, od autora albo z zlecenia generowania**. | Rozdzielenie wartości nominalnej (tag, FR-36) od skalibrowanej (ADR-029). |
| B-5 | Pewność oceny | Oszacowane przez system prawdopodobieństwo, że ocena jest poprawna. | Oszacowane przez system prawdopodobieństwo, że ocena jest poprawna, **tj. zgodna z oceną nauczyciela co do liczby punktów, wskazania pierwszego błędnego kroku i stwierdzenia braku wymaganego kroku (Zgodność Oceny, ADR-027)**. | Mierzalność NFR-02 i NFR-04 (ADR-027). |
| B-6 | Rozwiązanie | Praca ucznia nad zadaniem przesłana jako zdjęcie kartki. | Praca ucznia nad **zadaniem otwartym** przesłana jako zdjęcie kartki (jedno lub kilka). **Wybór w zadaniu zamkniętym to Odpowiedź.** | FR-15/FR-16 rozróżniają oba przypadki. |
| B-7 | Model System One | (bez zmiany treści) | Dopisek: „W systemie jedynym Modelem System One jest Jev; rolę modelu decyzyjnego może pełnić także model zapasowy (Model decyzyjny), który Modelem System One nie jest.” | Usunięcie niejednoznaczności mapowania (3.2). |
| B-8 | Agent | Komponent AI realizujący jedno zadanie w systemie (rekomendacja, generowanie, rozpoznawanie, ocenianie). | Komponent AI realizujący jedno zadanie w systemie: rekomendację, generowanie, rozpoznawanie, ocenianie **albo wyjaśnianie (Agent wyjaśniający pisze Wskazówkę na podstawie decyzji Agenta oceniającego)**. | `architecture.md` i FR-27 przewidują Agenta wyjaśniającego; obecna definicja wylicza tylko cztery zadania. |
| B-9 | Jev | Model typu System One firmy TypeSafe; w projekcie używany jako silnik decyzyjny agenta oceniającego. | Model typu System One firmy TypeSafe; w projekcie silnik decyzyjny Agenta oceniającego, **a także Walidacji zadania (pytania Q-WA-*) i eksperymentalnej strategii Agenta rekomendującego (Q-RE-01)**. | `architecture.md` używa Jev w Walidacji i w rekomendacji; definicja musi to obejmować. |

**Część C — terminy techniczne dopuszczone w identyfikatorach** (3.1 p. 6):
θ/`theta`, `trudnosc_b`, `ipw`, `ece`, `n_eff`, `cv5`, `sha256`, `latex`,
`sympy`, `jwt`, `url`, `mime`, `trace_id`, `hmac`, `fnv1a`, `http` (protokół
odpowiedzi API), `rodo` (RODO — rozporządzenie 2016/679), prefiksy `ux_`,
`ix_`, `fk_`, `trg_` — z definicjami jak w 3.1 p. 6.

---

## 4. Widok kontekstu systemu

```mermaid
flowchart LR
    U["Uczeń<br/>(przeglądarka: telefon / komputer)"]
    N["Nauczyciel<br/>(przeglądarka)"]
    A["Administrator<br/>(przeglądarka)"]
    L["Lemat<br/>(platforma)"]
    J["Jev — Model System One<br/>(TypeSafe, API HTTP)"]
    G["Dostawca LLM<br/>(Model generatywny, multimodalny)"]
    S["Serwer SMTP<br/>(reset hasła)"]
    U -- "zdjęcia, Odpowiedzi, poprawki Transkrypcji" --> L
    L -- "Zadania, Oceny, Wskazówki, Wkładki teoretyczne, Profil ucznia" --> U
    N -- "Klasy, Przypisania, Weryfikacje, Przeglądy zadań" --> L
    A -- "import Arkuszy CKE, Konfiguracja modeli, konta" --> L
    L -- "stan + pytania noul/score/choice" --> J
    J -- "decyzje z prawdopodobieństwem" --> L
    L -- "prompty, obrazy" --> G
    G -- "tekst / JSON" --> L
    L -- "e-mail" --> S
```

---

## 5. Widok kontenerów i stos technologiczny

### 5.1 Diagram kontenerów

```mermaid
flowchart TB
    subgraph Klient
        PWA["Aplikacja PWA<br/>React 18 + TypeScript + Vite<br/>KaTeX, i18n pl-PL"]
    end
    subgraph Lemat["Lemat (jedno repozytorium, jeden obraz kontenera)"]
        RP["Reverse proxy<br/>Caddy 2 (TLS, statyki PWA)"]
        API["api<br/>Python 3.12, FastAPI, Uvicorn<br/>2 repliki"]
        WRK["worker<br/>Python 3.12, asyncio<br/>2 repliki × 4 pule korutyn (R15/O15/P6/W4)"]
        SCH["scheduler<br/>1 aktywna instancja<br/>(blokada doradcza PG)"]
        SBX["piaskownica SymPy + Pillow<br/>pula 4 procesów na replikę workera<br/>RLIMIT CPU/RAM"]
        SBXA["piaskownica Pillow api<br/>pula 2 procesów na replikę api<br/>RLIMIT CPU/RAM (ADR-045)"]
    end
    PG[("PostgreSQL 16<br/>dane + prace w tle (praca_w_tle)")]
    OBJ[("Magazyn obiektów S3 (UE)<br/>zdjęcia, zdjęcia maskowane, importy")]
    REJ[("Bucket rejestr-zdarzen-rodo<br/>Object Lock COMPLIANCE 60 dni")]
    JEV["Jev API"]
    LLM["Dostawca(y) LLM"]
    SMTP["SMTP"]
    PWA -->|HTTPS JSON| RP --> API
    API --> PG
    API --> OBJ
    API --> SBXA
    API --> REJ
    API -->|"Q-RE-01, timeout 1,5 s (ADR-023)"| JEV
    WRK --> PG
    WRK --> OBJ
    WRK --> REJ
    WRK --> SBX
    WRK --> JEV
    WRK --> LLM
    WRK --> SMTP
    SCH --> PG
    PG -. "LISTEN/NOTIFY praca_w_tle" .-> WRK
```

Wysyłka e-mail odbywa się wyłącznie przez pracę w tle `WYSLIJ_EMAIL`
(worker), więc proces `api` nie łączy się z SMTP. Proces `api` zapisuje do
bucketu `rejestr-zdarzen-rodo` Zdarzenia RODO z inicjatywy użytkownika (wniosek o
usunięcie, przywrócenie, odwołanie i udzielenie zgody) **przed** zatwierdzeniem
transakcji; proces `worker` zapisuje tam zdarzenie `USUNIECIE_WYKONANE`
(`zrodlo = WORKER`) przed pierwszą zmianą w bazie wykonywaną przez
`USUN_KONTO` (ADR-041, C3-03).

### 5.2 Stos technologiczny

| Obszar | Wybór | Uzasadnienie |
|---|---|---|
| Frontend | React 18 + TypeScript 5, Vite, PWA (manifest + service worker do cache statyk), KaTeX do renderowania LaTeX, `react-i18next` z jednym językiem `pl-PL` | NFR-08, NFR-10; jedna baza kodu dla telefonu i komputera. |
| Zdjęcia w kliencie | `<input type="file" accept="image/*" capture="environment" multiple>`; kompresja w kliencie (canvas → JPEG q=0.85, dłuższy bok ≤ 2560 px); ekran aparatu z instrukcją „nie podpisuj kartki imieniem i nazwiskiem” | NFR-08; zmniejsza transfer, koszt rozpoznawania i ilość danych osobowych na zdjęciu (ADR-031). |
| Backend | Python 3.12, FastAPI, Pydantic v2, SQLAlchemy 2 (Core + ORM), Alembic | Ekosystem SymPy i SDK modeli AI w Pythonie. |
| Baza danych | PostgreSQL 16 (rozszerzenia `citext`, `pg_trgm`) | Jedno źródło prawdy + prace w tle (ADR-002). |
| Prace w tle | Tabela `praca_w_tle` z kolumną `pula` + `SELECT … FOR UPDATE SKIP LOCKED` + `LISTEN/NOTIFY` | Transakcyjne zlecanie prac razem ze zmianą stanu (ADR-002); osobne pule dla rozpoznawania i oceniania (13.4). |
| Magazyn obiektów | S3-kompatybilny w regionie UE (lokalnie MinIO), szyfrowanie SSE, **wersjonowanie wyłączone** na bucketach ze zdjęciami (16.4) | Zdjęcia poza bazą; NFR-09. |
| Obróbka obrazu | Pillow ≥ 10 + `pillow-heif`; normalizacja w piaskownicy procesowej `api` (2 procesy na replikę, ADR-045), maskowanie w piaskownicy `worker` | Usuwanie EXIF, maskowanie danych osobowych (ADR-031); dekodowanie obrazów nigdy w procesie obsługującym pętlę zdarzeń. |
| Obliczenia symboliczne | SymPy ≥ 1.13 (`parse_latex` z backendem `lark`) w puli procesów z limitami | FR-21, walidacja FR-13 (ADR-013). |
| Obserwowalność | OpenTelemetry (ślady), Prometheus (metryki), Grafana, logi JSON | NFR-01, NFR-03, NFR-04. |
| Wdrożenie | Docker Compose na 2 maszynach wirtualnych w UE + zarządzany PostgreSQL z PITR | Skala A-03 (ADR-001). |
| Kontrola architektury | `import-linter` w CI: SDK dostawców modeli importowane wyłącznie w `lemat.modele.adaptery.*` | NFR-06. |

---

## 6. Moduły, agenci i porty

### 6.1 Diagram komponentów

```mermaid
flowchart LR
    subgraph api["Proces api"]
        HTTP["lemat.http<br/>(routery FastAPI, auth, DTO,<br/>idempotencja)"]
    end
    subgraph domena["Moduły domenowe (wspólne dla api i worker)"]
        KON["lemat.konta"]
        KLA["lemat.klasy"]
        TRE["lemat.tresci<br/>Zadania, Kryteria oceniania,<br/>Wkładki, Działy, Umiejętności,<br/>Arkusze CKE, Przeglądy"]
        PRO["lemat.profil<br/>Profil ucznia, Wynik zadania,<br/>Test diagnostyczny"]
        REKM["lemat.rekomendacje<br/>Ścieżka nauki, Seria"]
        ROZ["lemat.rozwiazania<br/>Rozwiązanie, zdjęcia, Transkrypcja,<br/>Odpowiedzi"]
        OCE["lemat.ocenianie<br/>Ocena, reguły trasowania"]
        WER["lemat.weryfikacja<br/>Kolejka, blokady, szkice,<br/>zgłoszenia"]
        KAL["lemat.kalibracja<br/>Kalibrator pewności, Próg pewności,<br/>Kalibracja trudności"]
        POW["lemat.powiadomienia"]
        AUD["lemat.audyt"]
        ORK["lemat.orkiestrator<br/>maszyny stanów + praca_w_tle<br/>+ fasada synchroniczna"]
    end
    subgraph agenci["lemat.agenci"]
        AREK["Agent rekomendujący"]
        AGEN["Agent generujący"]
        AROZ["Agent rozpoznający"]
        AOCE["Agent oceniający"]
        AWYJ["Agent wyjaśniający"]
    end
    subgraph porty["lemat.modele (porty + adaptery)"]
        PMD["ModelDecyzyjny"]
        PMG["ModelGeneratywny"]
        PRP["RozpoznawaniePisma"]
        PSY["SprawdzanieSymboliczne"]
        PPS["PredyktorSukcesu"]
        AJEV["JevAdapter"]
        AZAP["ZapasowyLlmAdapter"]
        ALLM["LlmAdapter (per dostawca)"]
        ACB["CircuitBreakerModeluDecyzyjnego"]
    end
    HTTP --> KON & KLA & TRE & PRO & REKM & ROZ & OCE & WER & KAL & POW & AUD & ORK
    ORK --> AREK & AGEN & AROZ & AOCE & AWYJ
    REKM --> ORK
    AOCE --> PMD & PSY
    AREK --> PPS & PMD
    AGEN --> PMG & PMD & PSY
    AROZ --> PRP
    AWYJ --> PMG
    PMD --> ACB --> AJEV & AZAP
    PMG --> ALLM
    PRP --> ALLM
    AZAP --> ALLM
```

### 6.2 Odpowiedzialności modułów

| Moduł | Odpowiedzialność | Właściciel danych (tabele) |
|---|---|---|
| `lemat.http` | Routery, uwierzytelnianie, DTO, mapowanie błędów RFC 9457, idempotencja żądań, globalny ogranicznik szybkości modeli dla procesu `api` (13.2) | `klucz_idempotencji`, `limit_szybkosci_modelu` (wspólnie z `lemat.modele`) |
| `lemat.konta` | Rejestracja, logowanie, tokeny, role, flagi Nauczyciela (`weryfikator_globalny`, `recenzent_tresci`), statusy kont, usuwanie i przywracanie kont wszystkich ról (RODO, ADR-040, ADR-044), Zdarzenia RODO i ich rejestr poza bazą (ADR-041), kalendarz dni roboczych | `uzytkownik`, `uczen`, `nauczyciel`, `token_odswiezania`, `proba_logowania`, `zdarzenie_rodo`, `dzien_wolny` |
| `lemat.klasy` | Klasy, kody zaproszeń, członkostwo, archiwizacja, Zestawy zadań, Przypisania | `klasa`, `czlonkostwo_klasy`, `zestaw_zadan`, `pozycja_zestawu`, `przypisanie`, `przypisanie_uczen` |
| `lemat.tresci` | Zadania i ich wersje, zasięg, Kryteria oceniania, Wkładki teoretyczne, Działy, Umiejętności, import Arkuszy CKE, Walidacja i Przegląd zadań, Zgłoszenia błędu w Zadaniu i wstrzymanie w doborze, pula Zadań diagnostycznych, zlecenia generowania i budżet generowania systemowego | `dzial`, `umiejetnosc`, `umiejetnosc_wymagana`, `arkusz_cke`, `import_arkusza`, `zadanie`, `zadanie_wersja`, `zadanie_umiejetnosc`, `kryterium_oceniania`, `zalacznik_zadania`, `wkladka_teoretyczna`, `walidacja_zadania`, `przeglad_zadania`, `zlecenie_generowania`, `zgloszenie_bledu_zadania`, `wstrzymanie_w_doborze` |
| `lemat.profil` | Profil ucznia, Wynik zadania, Test diagnostyczny, powtórki, odtwarzanie Profilu | `profil_ucznia`, `opanowanie_umiejetnosci`, `wynik_zadania`, `test_diagnostyczny`, `pozycja_testu_diagnostycznego` |
| `lemat.rekomendacje` | Ścieżka nauki, Seria, Rekomendacje (korzysta z Agenta rekomendującego przez fasadę Orkiestratora) | `sciezka_nauki`, `element_sciezki`, `rekomendacja` |
| `lemat.rozwiazania` | Przyjęcie i normalizacja zdjęć (piaskownica `api`), maskowanie, Transkrypcja, Poprawki Transkrypcji z limitami i Sprawdzeniem zgodności poprawek (ADR-036), Filtr danych osobowych (ADR-042), Odpowiedzi i kontekst próby (ADR-032), zgłoszenia błędu odczytu (przyjęcie wniosku), kopie treningowe (za zgodą) | `rozwiazanie`, `zdjecie_rozwiazania`, `transkrypcja`, `krok_transkrypcji`, `odpowiedz`, `kopia_treningowa` |
| `lemat.ocenianie` | Ocena, reguły FR-20/21/25/26/28, trasowanie do Weryfikacji, Wskazówki | `ocena`, `ocena_kryterium` |
| `lemat.weryfikacja` | Kolejka weryfikacji, blokady i ich zwalnianie, szkice rozstrzygnięć, rozstrzygnięcia, korekty, eskalacja, kontrola przyjęć | `weryfikacja`, `szkic_rozstrzygniecia` |
| `lemat.kalibracja` | Próbki kalibracyjne, kalibratory pewności, Próg pewności, bezpieczniki trafności i poprawek (ADR-046), kalibracja Poziomu trudności, metryki NFR-02/03/04 | `probka_kalibracyjna`, `kalibrator_pewnosci`, `prog_pewnosci`, `stan_bezpiecznika`, `kalibracja_trudnosci` |
| `lemat.modele` | Porty, adaptery, Konfiguracja modeli, dziennik decyzji, circuit breaker, globalny ogranicznik szybkości (ADR-039) | `konfiguracja_modelu`, `decyzja_modelu`, `limit_szybkosci_modelu` |
| `lemat.orkiestrator` | Maszyny stanów, zlecanie i wykonywanie prac w tle, pule workera, harmonogram, fasada synchroniczna dla rekomendacji (ADR-023) | `praca_w_tle` |
| `lemat.powiadomienia` | Powiadomienia w aplikacji | `powiadomienie` |
| `lemat.audyt` | Dziennik działań Nauczycieli, Administratorów i zmian systemowych (np. zmiana skalibrowanego Poziomu trudności) | `dziennik_audytu` |

Reguła zależności: moduły domenowe nie importują siebie nawzajem poza
publicznymi fasadami `lemat.<modul>.api` (kontrakt `import-linter`
„layers + independence”). Agenci nie znają FastAPI ani SQLAlchemy — dostają i
zwracają obiekty Pydantic (sekcja 10).

### 6.3 Orkiestrator

Orkiestrator nie jest osobnym serwisem, lecz modułem (biblioteką)
wykonywanym w dwóch trybach:

- **Tryb asynchroniczny (proces `worker`)** — wszystkie przepływy
  wieloagentowe: rozpoznanie → ocena → wskazówka → aktualizacja Profilu,
  generowanie → walidacja, kalibracja. Realizuje wzorzec **maszyna stanów w
  bazie + prace w tle**:
  1. Każda zmiana stanu encji (np. `rozwiazanie.status`) i zlecenie kolejnej
     pracy w tle odbywają się **w jednej transakcji** PostgreSQL.
  2. Worker pobiera pracę ze swojej puli (`FOR UPDATE SKIP LOCKED`), ustawia
     dzierżawę (`zablokowana_do = now() + 60 s`), odnawia ją co 15 s.
  3. Wywołania modeli odbywają się **poza transakcją**; wynik zapisywany jest
     warunkowo (`UPDATE … WHERE status = :oczekiwany AND version = :v`). Jeśli
     warunek nie jest spełniony, wynik jest odrzucany i logowany jako
     `rezultat_przestarzaly` (sekcja 15).
  4. Każdy handler jest idempotentny (sekcja 13.3).
- **Tryb synchroniczny (proces `api`)** — jeden przepływ:
  `Orkiestrator.rekomenduj_synchronicznie(uczen_id)` wywoływany przez
  `lemat.rekomendacje` przy `GET /uczniowie/ja/rekomendacje/nastepna`. Jest to
  wywołanie jednego agenta (rekomendującego) bez przekazywania danych do
  innych agentów, więc nie wymaga maszyny stanów. Uzasadnienie wyjątku:
  Uczeń czeka na następny element po kliknięciu „Dalej” (cel p95 ≤ 500 ms),
  a praca w tle z odpytywaniem dodałaby ≥ 1 s. Ograniczenia: jedyne
  wywołanie modelu to `Q-RE-01` (tylko grupa eksperymentu `JEV_CHOICE`) z
  timeoutem 1,5 s, bez ponowień i bez modelu zapasowego; przy błędzie albo
  otwartym obwodzie polityka `POLITYKA_ELO` (10.1.6). Zapotrzebowanie na
  uzupełnienie puli Zadań jest zlecane jako praca w tle w tej samej
  transakcji co zapis elementu (ADR-023).

---

## 7. Model danych

### 7.1 Diagram ERD (encje rdzenia domeny)

Diagram pokazuje relacje; pełne kolumny, typy i ograniczenia są w DDL (7.3).

```mermaid
erDiagram
    UZYTKOWNIK ||--o| UCZEN : "jest"
    UZYTKOWNIK ||--o| NAUCZYCIEL : "jest"
    NAUCZYCIEL ||--o{ KLASA : "prowadzi"
    KLASA ||--o{ CZLONKOSTWO_KLASY : "ma"
    UCZEN ||--o{ CZLONKOSTWO_KLASY : "należy"
    NAUCZYCIEL ||--o{ ZESTAW_ZADAN : "tworzy"
    ZESTAW_ZADAN ||--o{ PRZYPISANIE : "przypisany"
    PRZYPISANIE ||--o{ PRZYPISANIE_UCZEN : "adresaci"
    DZIAL ||--o{ UMIEJETNOSC : "zawiera"
    UMIEJETNOSC ||--o{ WKLADKA_TEORETYCZNA : "wyjaśnia"
    ARKUSZ_CKE ||--o{ ZADANIE : "źródło zadań bazowych"
    DZIAL ||--o{ ZADANIE : "przypisany dział"
    ZADANIE ||--|{ ZADANIE_WERSJA : "wersje"
    ZADANIE ||--|| KALIBRACJA_TRUDNOSCI : "poziom trudności"
    ZADANIE ||--|{ ZADANIE_UMIEJETNOSC : "otagowane"
    UMIEJETNOSC ||--o{ ZADANIE_UMIEJETNOSC : "w zadaniach"
    ZADANIE_WERSJA ||--o{ KRYTERIUM_OCENIANIA : "kryteria oceniania"
    UCZEN ||--|| PROFIL_UCZNIA : "ma"
    PROFIL_UCZNIA ||--o{ OPANOWANIE_UMIEJETNOSCI : "poziomy opanowania"
    UCZEN ||--o{ TEST_DIAGNOSTYCZNY : "wykonuje"
    UCZEN ||--|| SCIEZKA_NAUKI : "ma"
    SCIEZKA_NAUKI ||--o{ ELEMENT_SCIEZKI : "kolejne elementy"
    ELEMENT_SCIEZKI ||--|| REKOMENDACJA : "uzasadniony przez"
    UCZEN ||--o{ ROZWIAZANIE : "przesyła"
    ZADANIE_WERSJA ||--o{ ROZWIAZANIE : "dotyczy"
    ELEMENT_SCIEZKI ||--o| ROZWIAZANIE : "kontekst próby"
    PRZYPISANIE ||--o{ ROZWIAZANIE : "kontekst próby"
    ROZWIAZANIE ||--|{ ZDJECIE_ROZWIAZANIA : "zdjęcia"
    ROZWIAZANIE ||--o{ TRANSKRYPCJA : "wersje transkrypcji"
    TRANSKRYPCJA ||--|{ KROK_TRANSKRYPCJI : "kroki"
    ROZWIAZANIE ||--o{ OCENA : "wersje oceny"
    OCENA ||--|{ OCENA_KRYTERIUM : "punkty za kryteria"
    KRYTERIUM_OCENIANIA ||--o{ OCENA_KRYTERIUM : "oceniane"
    ROZWIAZANIE ||--o{ WERYFIKACJA : "weryfikacje"
    WERYFIKACJA ||--o{ SZKIC_ROZSTRZYGNIECIA : "szkice"
    UCZEN ||--o{ ODPOWIEDZ : "zaznacza"
    UCZEN ||--o{ WYNIK_ZADANIA : "wyniki"
    OCENA ||--o| WYNIK_ZADANIA : "rejestruje"
    ODPOWIEDZ ||--o| WYNIK_ZADANIA : "rejestruje"
    WERYFIKACJA ||--o| PROBKA_KALIBRACYJNA : "tworzy"
    KONFIGURACJA_MODELU ||--o{ KALIBRATOR_PEWNOSCI : "kalibratory"
    KALIBRATOR_PEWNOSCI ||--o{ PROG_PEWNOSCI : "progi"
    KONFIGURACJA_MODELU ||--o{ DECYZJA_MODELU : "decyzje"
    UCZEN ||--o{ KOPIA_TRENINGOWA : "kopie za zgodą (ADR-035)"
    ZADANIE ||--o{ ZGLOSZENIE_BLEDU_ZADANIA : "zgłoszenia błędu (ADR-038)"
    NAUCZYCIEL ||--o{ ZGLOSZENIE_BLEDU_ZADANIA : "zgłasza"
    UZYTKOWNIK ||--o{ ZDARZENIE_RODO : "zdarzenia RODO (bez FK, ADR-041)"
```

### 7.2 Konwencje schematu

- Klucze główne: `uuid` generowany przez `gen_random_uuid()`; wyjątki:
  `dzial.id`, `umiejetnosc.id` (`integer`, stabilne dane referencyjne) i
  `praca_w_tle.id` (`bigint identity`).
- Czas: `timestamptz`, zawsze UTC; `date` dla `termin_egzaminu`.
- Optymistyczna blokada: kolumna `version integer NOT NULL DEFAULT 1`,
  zwiększana przy każdej modyfikacji **wykonanej przez człowieka albo przez
  przejście maszyny stanów**; API eksponuje ją jako nagłówek `ETag`.
  Wyjątek: `weryfikacja.version` jest **wersją treści** — zmienia ją wyłącznie
  trigger `trg_weryfikacja_wersja` przy zmianie kolumn treści (powody,
  zgłoszenie, Ocena weryfikowana, zamknięcie); podjęcie, przedłużenie,
  zwolnienie blokady i eskalacja jej nie zmieniają (ADR-037).
  Statystyki i stany systemowe aktualizowane automatycznie (kalibracja
  Poziomu trudności, liczniki, Wstrzymanie w doborze — tabela
  `wstrzymanie_w_doborze` od wersji 2.3, C3-09) są w osobnych tabelach i nie
  zmieniają `version` encji, do której należą (ADR-029). Blokada wiersza
  (`SELECT … FOR UPDATE`) używana do serializacji takich zmian nie modyfikuje
  wiersza, więc też nie zmienia `version`.
- Usuwanie: twarde usuwanie danych osobowych (RODO, sekcja 16.4); dane
  referencyjne treści mają statusy zamiast usuwania.

### 7.3 Pełne DDL (PostgreSQL 16)

```sql
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ===================== TYPY WYLICZENIOWE =====================
CREATE TYPE rola_uzytkownika        AS ENUM ('UCZEN','NAUCZYCIEL','ADMINISTRATOR');
CREATE TYPE status_konta            AS ENUM ('AKTYWNE','OCZEKUJE_NA_ZATWIERDZENIE','ZABLOKOWANE','USUWANE');
CREATE TYPE poziom_matury           AS ENUM ('PODSTAWOWY','ROZSZERZONY');
CREATE TYPE rodzaj_zadania          AS ENUM ('ZAMKNIETE','OTWARTE_OBLICZENIOWE','OTWARTE_Z_DOWODEM');
CREATE TYPE forma_zamknieta         AS ENUM ('WYBOR_JEDNOKROTNY','PRAWDA_FALSZ');
CREATE TYPE pochodzenie_zadania     AS ENUM ('BAZOWE_ARKUSZ_CKE','BAZOWE_NAUCZYCIEL','WYGENEROWANE');
CREATE TYPE zasieg_zadania          AS ENUM ('GLOBALNY','PRYWATNY');
CREATE TYPE status_zadania          AS ENUM ('WALIDACJA','ODRZUCONE_W_WALIDACJI','DO_PRZEGLADU',
                                             'OPUBLIKOWANE','ODRZUCONE','WYCOFANE');
CREATE TYPE status_wkladki          AS ENUM ('SZKIC','DO_PRZEGLADU','OPUBLIKOWANA','WYCOFANA');
CREATE TYPE status_rozwiazania      AS ENUM ('PRZESLANE','ROZPOZNAWANIE','BLAD_ROZPOZNAWANIA',
                                             'TRANSKRYPCJA_GOTOWA','ZATWIERDZONE','OCENIANIE',
                                             'OCZEKUJE_NA_WERYFIKACJE','OCENIONE','PORZUCONE','ANULOWANE');
CREATE TYPE zrodlo_transkrypcji     AS ENUM ('AGENT_ROZPOZNAJACY','POPRAWKA_UCZNIA');
CREATE TYPE status_oceny            AS ENUM ('WSTEPNA','OSTATECZNA','ZASTAPIONA');
CREATE TYPE zrodlo_oceny            AS ENUM ('AUTOMATYCZNA','NAUCZYCIEL');
CREATE TYPE typ_bledu               AS ENUM ('RACHUNKOWY','MERYTORYCZNY','NIEPELNE_ROZWIAZANIE');
CREATE TYPE zrodlo_wskazowki        AS ENUM ('AGENT_WYJASNIAJACY','SZABLON','NAUCZYCIEL');
CREATE TYPE powod_weryfikacji       AS ENUM ('NISKA_PEWNOSC','POPRAWNY_WYNIK_BLEDNE_KROKI','ZADANIE_Z_DOWODEM',
                                             'PODEJRZENIE_BLEDU_ODCZYTU','ZGLOSZENIE_BLEDU_ODCZYTU',
                                             'POPRAWKI_NIEZGODNE_ZE_ZDJECIEM',          -- T-7, ADR-036
                                             'KONTROLA_LOSOWA','BLAD_PRZETWARZANIA');
CREATE TYPE status_weryfikacji      AS ENUM ('OCZEKUJE','W_TRAKCIE','ZAKONCZONA','ANULOWANA');
CREATE TYPE powod_anulowania        AS ENUM ('USUNIECIE_KONTA','PONOWNA_OCENA_AUTOMATYCZNA');
CREATE TYPE typ_kolejki_weryfikacji AS ENUM ('KLASA','GLOBALNA');
CREATE TYPE typ_elementu_sciezki    AS ENUM ('WKLADKA_TEORETYCZNA','ZADANIE');
CREATE TYPE cel_elementu            AS ENUM ('NAUKA','POWTORKA');
CREATE TYPE status_elementu_sciezki AS ENUM ('PROPONOWANY','ROZPOCZETY','UKONCZONY','POMINIETY','WYGASLY');
CREATE TYPE strategia_rekomendacji  AS ENUM ('POLITYKA_ELO','JEV_CHOICE');
CREATE TYPE rola_modelu             AS ENUM ('DECYZYJNY_GLOWNY','DECYZYJNY_ZAPASOWY','ROZPOZNAWANIE_PISMA',
                                             'GENERATYWNY_ZADANIA','GENERATYWNY_ROZWIAZUJACY_NIEZALEZNIE',
                                             'GENERATYWNY_WSKAZOWKI','GENERATYWNY_WKLADKI',
                                             'GENERATYWNY_OPINIA_POMOCNICZA');
CREATE TYPE typ_pytania             AS ENUM ('NOUL','SCORE','CHOICE');
CREATE TYPE kontekst_decyzji        AS ENUM ('OCENA','REKOMENDACJA','WALIDACJA_ZADANIA');
CREATE TYPE zrodlo_wyniku           AS ENUM ('ODPOWIEDZ','OCENA');
CREATE TYPE zrodlo_probki           AS ENUM ('WERYFIKACJA_NISKA_PEWNOSC','KONTROLA_LOSOWA',
                                             'WERYFIKACJA_INNY_POWOD','KOREKTA_NAUCZYCIELA');
CREATE TYPE zrodlo_progu            AS ENUM ('EWALUACJA_ZT1','KALIBRACJA_AUTOMATYCZNA','ADMINISTRATOR','BEZPIECZNIK');
CREATE TYPE zrodlo_kalibratora      AS ENUM ('EWALUACJA_ZT1','KALIBRACJA_AUTOMATYCZNA');   -- rozłączne z zrodlo_progu (3.1 p. 5)
CREATE TYPE powod_uniewaznienia_wyniku AS ENUM ('OCENA_ZASTAPIONA','ZGLOSZENIE_BLEDU_ODCZYTU'); -- 10.1.3, 11.6
CREATE TYPE status_pracy            AS ENUM ('OCZEKUJE','W_TOKU','ZAKONCZONA','MARTWA');
CREATE TYPE status_zlecenia         AS ENUM ('OCZEKUJE','W_TOKU','ZAKONCZONE','NIEUDANE');
CREATE TYPE status_importu          AS ENUM ('OCZEKUJE','PRZETWARZANY','ZAKONCZONY','ODRZUCONY');
CREATE TYPE status_testu            AS ENUM ('W_TOKU','ZAKONCZONY','POMINIETY');
CREATE TYPE decyzja_przegladu       AS ENUM ('ZATWIERDZONE','POPRAWIONE','ODRZUCONE');
CREATE TYPE rezultat_walidacji      AS ENUM ('POZYTYWNY','NEGATYWNY');
CREATE TYPE inicjalizacja_opanowania AS ENUM ('DOMYSLNA','TEST_DIAGNOSTYCZNY','PROPAGACJA_Z_DZIALU');
CREATE TYPE status_zgloszenia_zadania AS ENUM ('OTWARTE','UWZGLEDNIONE','ODRZUCONE');           -- ADR-038
CREATE TYPE typ_zdarzenia_rodo      AS ENUM ('WNIOSEK_O_USUNIECIE','ANULOWANIE_USUNIECIA','USUNIECIE_WYKONANE',
                                             'ODWOLANIE_ZGODY','UDZIELENIE_ZGODY');           -- ADR-041; 2.3: WNIOSEK_O_USUNIECIE (C3-07)
CREATE TYPE zrodlo_zdarzenia_rodo   AS ENUM ('API','WORKER');                                -- ADR-041 (C3-03)
CREATE TYPE typ_bezpiecznika        AS ENUM ('TRAFNOSC','POPRAWKI');                         -- ADR-046 (C3-01)
CREATE TYPE powod_eskalacji         AS ENUM ('PRZEKROCZENIE_SLA','NAUCZYCIEL_DOCELOWY_ZABLOKOWANY',
                                             'KARENCJA_KONTA_NAUCZYCIELA');                   -- 11.4.3 p. 1, 16.4 (C3-08)

-- ===================== KONTA =====================
CREATE TABLE uzytkownik (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email            citext NOT NULL UNIQUE,
    haslo_hash       text   NOT NULL,                 -- argon2id (m=64MiB, t=3, p=4)
    rola             rola_uzytkownika NOT NULL,
    status           status_konta NOT NULL DEFAULT 'AKTYWNE',
    imie             text   NOT NULL CHECK (char_length(imie) BETWEEN 1 AND 60),
    nazwisko         text   NOT NULL CHECK (char_length(nazwisko) BETWEEN 1 AND 80),
    usuniecie_zaplanowane_at timestamptz,             -- RODO: koniec okresu karencji (ADR-040), usunięcie po 30 dniach
    ostatnie_logowanie_at    timestamptz,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    version          integer NOT NULL DEFAULT 1,
    CHECK ((status = 'USUWANE') = (usuniecie_zaplanowane_at IS NOT NULL))
);

CREATE TABLE uczen (
    uzytkownik_id         uuid PRIMARY KEY REFERENCES uzytkownik(id) ON DELETE CASCADE,
    poziom_matury         poziom_matury,              -- NULL do czasu FR-02
    termin_egzaminu       date,
    wybrany_dzial_id      integer,                    -- FR-09; NULL = wszystkie działy (FK niżej)
    oswiadczenie_wieku_16 boolean NOT NULL CHECK (oswiadczenie_wieku_16),
    zgoda_na_trening      boolean NOT NULL DEFAULT false,   -- zdjęcia do zbioru treningowego (16.4)
    procent_grupy_eksperymentu smallint NOT NULL CHECK (procent_grupy_eksperymentu BETWEEN 0 AND 99),
                                                      -- fnv1a(uzytkownik_id) mod 100; przydział do eksperymentu A/B
    created_at            timestamptz NOT NULL DEFAULT now(),
    version               integer NOT NULL DEFAULT 1
);

CREATE TABLE nauczyciel (
    uzytkownik_id        uuid PRIMARY KEY REFERENCES uzytkownik(id) ON DELETE CASCADE,
    weryfikator_globalny boolean NOT NULL DEFAULT false,  -- obsługuje kolejkę GLOBALNA (ADR-021)
    recenzent_tresci     boolean NOT NULL DEFAULT false,  -- Przegląd Zadań/Wkładek globalnych (ADR-028)
    deklarowane_minuty_weryfikacji smallint NOT NULL DEFAULT 0
                         CHECK (deklarowane_minuty_weryfikacji BETWEEN 0 AND 480),
    created_at           timestamptz NOT NULL DEFAULT now(),
    version              integer NOT NULL DEFAULT 1,
    CHECK (NOT weryfikator_globalny OR deklarowane_minuty_weryfikacji >= 90)  -- A-13
);

CREATE TABLE token_odswiezania (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uzytkownik_id   uuid NOT NULL REFERENCES uzytkownik(id) ON DELETE CASCADE,
    rodzina_id      uuid NOT NULL,                    -- wykrywanie ponownego użycia
    token_hash      bytea NOT NULL UNIQUE,            -- SHA-256 tokenu
    wygasa_at       timestamptz NOT NULL,
    uniewazniony_at timestamptz,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE proba_logowania (
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email      citext NOT NULL,
    ip         inet   NOT NULL,
    udana      boolean NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_proba_logowania_email ON proba_logowania (email, created_at DESC);

-- Zdarzenia RODO (ADR-041): kopia w bazie dziennika zapisanego wcześniej w buckecie rejestr-zdarzen-rodo
CREATE TABLE zdarzenie_rodo (
    id             uuid PRIMARY KEY,                  -- generowany przez proces zapisujący obiekt do bucketu PRZED zmianą w bazie:
                                                      -- api (gen_random_uuid) albo worker (USUNIECIE_WYKONANE: uuid5(NS_RODO,
                                                      -- 'USUNIECIE_WYKONANE:' || uzytkownik_id) — deterministyczny, ponowienie USUN_KONTO
                                                      -- w dowolnym dniu trafia w ten sam klucz obiektu bez daty; C3-03, C4-06 p. 3)
    uzytkownik_id  uuid NOT NULL,                     -- bez FK: wpis przeżywa usunięcie konta
    typ            typ_zdarzenia_rodo NOT NULL,
    zrodlo         zrodlo_zdarzenia_rodo NOT NULL DEFAULT 'API',
                                                      -- WORKER: stanem docelowym jest wykonanie usunięcia, a nie transakcja;
                                                      -- UZGODNIJ_REJESTR_RODO nigdy go nie unieważnia (13.3)
    dane           jsonb NOT NULL DEFAULT '{}',       -- zawsze {"rola": "UCZEN"|"NAUCZYCIEL"|"ADMINISTRATOR", …} (od 2.4, C4-01),
                                                      -- np. {"rola":"UCZEN","usuniecie_zaplanowane_at":"…"}; bez danych osobowych
    klucz_obiektu  text NOT NULL UNIQUE,              -- API: rejestr-zdarzen-rodo/zdarzenia/{rrrr-mm-dd created_at}/{uzytkownik_id}/{id}.json
                                                      -- WORKER: rejestr-zdarzen-rodo/wykonane/{uzytkownik_id}/{id}.json (bez daty, C4-06 p. 3)
    created_at     timestamptz NOT NULL DEFAULT now(),
    CHECK ((zrodlo = 'WORKER') = (typ = 'USUNIECIE_WYKONANE')),
    CHECK (dane ? 'rola')                             -- rola konta w każdym zdarzeniu (ponowienie wg roli, 16.4, C4-01)
);
CREATE INDEX ix_zdarzenie_rodo_uzytkownik ON zdarzenie_rodo (uzytkownik_id, created_at);

-- Kalendarz dni roboczych (A-15, ADR-043)
CREATE TABLE dzien_wolny (
    data  date PRIMARY KEY,                           -- dzień ustawowo wolny od pracy (Polska)
    nazwa text NOT NULL
);
-- Funkcja dodaj_dni_robocze(od timestamptz, n integer) RETURNS timestamptz (STABLE, strefa Europe/Warsaw):
-- jeśli `od` przypada na sobotę, niedzielę lub dzień z dzien_wolny, liczenie zaczyna się od najbliższego
-- dnia roboczego o 08:00; następnie dodaje n dni roboczych z zachowaniem godziny. Przykłady:
-- piątek 18:00 + 3 → środa 18:00; sobota 10:00 + 3 → czwartek 08:00. Dane: migracja z dniami wolnymi
-- 2026–2030; praca UTRZYMAJ_PARTYCJE alarmuje, gdy brak wpisów na 12 miesięcy naprzód.

-- ===================== DZIAŁY, UMIEJĘTNOŚCI, WKŁADKI =====================
CREATE TABLE dzial (
    id                integer PRIMARY KEY,
    kod               text NOT NULL UNIQUE,           -- np. 'XIII'
    nazwa             text NOT NULL,
    tylko_rozszerzony boolean NOT NULL DEFAULT false,
    kolejnosc         smallint NOT NULL
);
ALTER TABLE uczen ADD CONSTRAINT fk_uczen_dzial FOREIGN KEY (wybrany_dzial_id) REFERENCES dzial(id);

CREATE TABLE umiejetnosc (
    id                 integer PRIMARY KEY,
    dzial_id           integer NOT NULL REFERENCES dzial(id),
    kod                text NOT NULL UNIQUE,          -- np. 'R.XIII.2' (poziom.dział.punkt podstawy)
    nazwa              text NOT NULL,
    tylko_rozszerzony  boolean NOT NULL DEFAULT false,
    waga_egzaminacyjna numeric(4,3) NOT NULL CHECK (waga_egzaminacyjna > 0 AND waga_egzaminacyjna <= 1)
);

CREATE TABLE umiejetnosc_wymagana (
    umiejetnosc_id          integer NOT NULL REFERENCES umiejetnosc(id),
    wymagana_umiejetnosc_id integer NOT NULL REFERENCES umiejetnosc(id),
    PRIMARY KEY (umiejetnosc_id, wymagana_umiejetnosc_id),
    CHECK (umiejetnosc_id <> wymagana_umiejetnosc_id)
);

CREATE TABLE wkladka_teoretyczna (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    umiejetnosc_id     integer NOT NULL REFERENCES umiejetnosc(id),
    tytul              text NOT NULL,
    tresc_md           text NOT NULL CHECK (char_length(tresc_md) <= 12000), -- Markdown + LaTeX ($…$)
    szacowany_czas_min smallint NOT NULL CHECK (szacowany_czas_min BETWEEN 1 AND 15),
    wygenerowana       boolean NOT NULL,              -- true = szkic Agenta generującego
    wygenerowana_automatycznie boolean NOT NULL DEFAULT false, -- true = GENERUJ_SZKIC_WKLADKI (E-20); budżet 12.7
    koszt_generowania_pln numeric(10,4) NOT NULL DEFAULT 0,
    status             status_wkladki NOT NULL DEFAULT 'SZKIC',
    zatwierdzona_przez uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,  -- recenzent treści lub Administrator
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now(),
    version            integer NOT NULL DEFAULT 1
);
CREATE UNIQUE INDEX ux_wkladka_opublikowana ON wkladka_teoretyczna (umiejetnosc_id) WHERE status = 'OPUBLIKOWANA';
CREATE INDEX ix_wkladka_automatyczne_odrzucone ON wkladka_teoretyczna (umiejetnosc_id, updated_at)
    WHERE wygenerowana_automatycznie AND status = 'WYCOFANA';   -- limit 2 odrzuceń / 30 dni (13.3)

-- ===================== ARKUSZE CKE, ZADANIA =====================
CREATE TABLE arkusz_cke (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    kod            text NOT NULL UNIQUE,              -- np. 'MMA-P1-2024-05'
    rok            smallint NOT NULL CHECK (rok BETWEEN 2005 AND 2100),
    sesja          text NOT NULL CHECK (sesja IN ('MAJ','CZERWIEC','SIERPIEN','PROBNA','INFORMATOR')),
    poziom_matury  poziom_matury NOT NULL,
    created_at     timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE import_arkusza (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    administrator_id uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,  -- NULL po usunięciu konta (ADR-044)
    klucz_obiektu    text NOT NULL,                   -- ZIP w magazynie obiektów
    status           status_importu NOT NULL DEFAULT 'OCZEKUJE',
    raport           jsonb,                            -- lista błędów per numer zadania
    arkusz_cke_id    uuid REFERENCES arkusz_cke(id),
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE zlecenie_generowania (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    umiejetnosc_id   integer NOT NULL REFERENCES umiejetnosc(id),
    poziom_trudnosci smallint NOT NULL CHECK (poziom_trudnosci BETWEEN 1 AND 5),  -- nominalny docelowy
    poziom_matury    poziom_matury NOT NULL,
    rodzaj           rodzaj_zadania NOT NULL,
    zlecajacy_id     uuid REFERENCES uzytkownik(id) ON DELETE SET NULL, -- NULL = system (UZUPELNIJ_PULE)
    systemowe        boolean NOT NULL,                -- true ⇔ utworzone przez UZUPELNIJ_PULE (budżet 13.3)
    status           status_zlecenia NOT NULL DEFAULT 'OCZEKUJE',
    liczba_cykli     smallint NOT NULL DEFAULT 0 CHECK (liczba_cykli BETWEEN 0 AND 3),
                     -- cykl = generowanie + Walidacja; ponowienia techniczne pracy w tle się nie liczą (C1-28)
    koszt_pln        numeric(10,4) NOT NULL DEFAULT 0, -- suma kosztów wywołań modeli (13.1, raport adaptera)
    zadanie_id       uuid,                             -- FK niżej
    przyczyna_niepowodzenia text,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    CHECK (NOT systemowe OR (rodzaj <> 'OTWARTE_Z_DOWODEM' AND zlecajacy_id IS NULL))  -- system nie generuje dowodów (ADR-015)
);
CREATE INDEX ix_zlecenie_systemowe_dzien ON zlecenie_generowania (created_at) WHERE systemowe;

CREATE TABLE zadanie (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    pochodzenie            pochodzenie_zadania NOT NULL,
    zasieg                 zasieg_zadania NOT NULL,
    rodzaj                 rodzaj_zadania NOT NULL,
    forma_zamknieta        forma_zamknieta,
    dzial_id               integer NOT NULL REFERENCES dzial(id),
    poziom_matury          poziom_matury NOT NULL,
    arkusz_cke_id          uuid REFERENCES arkusz_cke(id),
    numer_w_arkuszu        text,
    autor_id               uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,
    zlecenie_generowania_id uuid REFERENCES zlecenie_generowania(id),
    status                 status_zadania NOT NULL,
    aktualna_wersja_id     uuid,                       -- FK DEFERRABLE niżej
    poziom_trudnosci_nominalny smallint NOT NULL CHECK (poziom_trudnosci_nominalny BETWEEN 1 AND 5),
    diagnostyczne          boolean NOT NULL DEFAULT false,
    zweryfikowane_przez_nauczyciela boolean NOT NULL DEFAULT false,
    created_at             timestamptz NOT NULL DEFAULT now(),
    updated_at             timestamptz NOT NULL DEFAULT now(),
    version                integer NOT NULL DEFAULT 1,
    CHECK ((rodzaj = 'ZAMKNIETE') = (forma_zamknieta IS NOT NULL)),
    CHECK ((pochodzenie = 'BAZOWE_ARKUSZ_CKE') = (arkusz_cke_id IS NOT NULL)),
    CHECK ((pochodzenie = 'WYGENEROWANE') = (zlecenie_generowania_id IS NOT NULL)),
    CHECK (pochodzenie <> 'BAZOWE_ARKUSZ_CKE' OR zasieg = 'GLOBALNY'),
    CHECK (NOT diagnostyczne OR (rodzaj = 'ZAMKNIETE' AND zasieg = 'GLOBALNY'))
);
ALTER TABLE zlecenie_generowania ADD CONSTRAINT fk_zlecenie_zadanie FOREIGN KEY (zadanie_id) REFERENCES zadanie(id);
CREATE INDEX ix_zadanie_dobor ON zadanie (poziom_matury, status, zasieg, rodzaj);
CREATE INDEX ix_zadanie_autor ON zadanie (autor_id) WHERE zasieg = 'PRYWATNY';
CREATE INDEX ix_zadanie_diagnostyczne ON zadanie (poziom_matury, dzial_id, poziom_trudnosci_nominalny)
    WHERE diagnostyczne AND status = 'OPUBLIKOWANE';      -- pula Testu diagnostycznego (9.1, G-5)

-- Kalibracja Poziomu trudności (ADR-029): statystyka aktualizowana atomowo, poza zadanie.version
CREATE TABLE kalibracja_trudnosci (
    zadanie_id                uuid PRIMARY KEY REFERENCES zadanie(id),
    trudnosc_b                numeric(6,3) NOT NULL,   -- parametr modelu Elo (10.1.3)
    liczba_wynikow_kalibracji integer NOT NULL DEFAULT 0,
    poziom_trudnosci          smallint NOT NULL CHECK (poziom_trudnosci BETWEEN 1 AND 5),
                                                       -- = nominalny, dopóki liczba_wynikow_kalibracji < 30
    updated_at                timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_kalibracja_poziom ON kalibracja_trudnosci (poziom_trudnosci);

CREATE TABLE zadanie_wersja (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_id           uuid NOT NULL REFERENCES zadanie(id),
    nr_wersji            integer NOT NULL CHECK (nr_wersji >= 1),
    tresc_md             text NOT NULL CHECK (char_length(tresc_md) <= 8000),
    opcje_zamkniete jsonb,     -- schemat 7.5.1; NOT NULL dla ZAMKNIETE
    rozwiazanie_wzorcowe jsonb NOT NULL, -- schemat 7.5.2
    wynik_koncowy_wzorcowy jsonb,   -- schemat 7.5.3; NULL wyłącznie dla OTWARTE_Z_DOWODEM i ZAMKNIETE
    punkty_max           smallint NOT NULL CHECK (punkty_max BETWEEN 1 AND 6),
    utworzona_przez      uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,
    zastapiona_at        timestamptz,                  -- chwila, w której przestała być aktualną wersją
    created_at           timestamptz NOT NULL DEFAULT now(),
    UNIQUE (zadanie_id, nr_wersji)
);
ALTER TABLE zadanie ADD CONSTRAINT fk_zadanie_aktualna_wersja
    FOREIGN KEY (aktualna_wersja_id) REFERENCES zadanie_wersja(id) DEFERRABLE INITIALLY DEFERRED;
CREATE INDEX ix_zadanie_wersja_tresc_trgm ON zadanie_wersja USING gin (tresc_md gin_trgm_ops);

CREATE TABLE zadanie_umiejetnosc (
    zadanie_id     uuid    NOT NULL REFERENCES zadanie(id),
    umiejetnosc_id integer NOT NULL REFERENCES umiejetnosc(id),
    glowna         boolean NOT NULL,
    PRIMARY KEY (zadanie_id, umiejetnosc_id)
);
CREATE UNIQUE INDEX ux_zadanie_umiejetnosc_glowna ON zadanie_umiejetnosc (zadanie_id) WHERE glowna;
-- Constraint trigger trg_umiejetnosc_glowna_w_dziale: umiejętność główna należy do zadanie.dzial_id.

CREATE TABLE kryterium_oceniania (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_wersja_id   uuid NOT NULL REFERENCES zadanie_wersja(id),
    wariant             smallint NOT NULL DEFAULT 1 CHECK (wariant BETWEEN 1 AND 4),
    nr_kryterium        smallint NOT NULL CHECK (nr_kryterium BETWEEN 1 AND 12),
    opis                text NOT NULL,
    punkty_max          smallint NOT NULL CHECK (punkty_max BETWEEN 1 AND 3),
    czy_wynik_koncowy   boolean NOT NULL DEFAULT false,
    wymaga_nr_kryterium smallint,                     -- punkt tylko po zaliczeniu wskazanego kryterium
    UNIQUE (zadanie_wersja_id, wariant, nr_kryterium),
    CHECK (wymaga_nr_kryterium IS NULL OR wymaga_nr_kryterium < nr_kryterium)
);
-- Constraint trigger DEFERRABLE trg_suma_kryteriow: dla każdego wariantu
-- SUM(punkty_max) = zadanie_wersja.punkty_max oraz co najwyżej jedno czy_wynik_koncowy;
-- suma liczby kryteriów wszystkich wariantów ≤ 24 (limit planu pytań modelu zapasowego, 13.2).
-- Dla zadań ZAMKNIETE kryteriów nie ma (ocena kluczem).

CREATE TABLE zalacznik_zadania (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_wersja_id uuid NOT NULL REFERENCES zadanie_wersja(id),
    klucz_obiektu     text NOT NULL,
    typ_mime          text NOT NULL CHECK (typ_mime IN ('image/png','image/svg+xml','image/jpeg')),
    tekst_alternatywny text NOT NULL,                 -- opis rysunku, przekazywany też do modeli
    kolejnosc         smallint NOT NULL
);

CREATE TABLE walidacja_zadania (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_wersja_id uuid NOT NULL REFERENCES zadanie_wersja(id),
    nr_cyklu          smallint NOT NULL CHECK (nr_cyklu BETWEEN 1 AND 3),
    rezultat          rezultat_walidacji NOT NULL,
    sprawdzenia       jsonb NOT NULL,                 -- schemat 10.2.3
    created_at        timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE przeglad_zadania (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_id         uuid NOT NULL REFERENCES zadanie(id),
    autor_przegladu_id uuid REFERENCES uzytkownik(id) ON DELETE SET NULL, -- Nauczyciel uprawniony (ADR-028) lub Administrator
    decyzja            decyzja_przegladu NOT NULL,
    komentarz          text,
    nowa_wersja_id     uuid REFERENCES zadanie_wersja(id),
    zasieg_po          zasieg_zadania NOT NULL,       -- zasięg Zadania po Przeglądzie
    created_at         timestamptz NOT NULL DEFAULT now(),
    CHECK ((decyzja = 'POPRAWIONE') = (nowa_wersja_id IS NOT NULL))
);

-- Zgłoszenie błędu w Zadaniu (FR-14, ADR-038): Nauczyciel bez prawa Przeglądu zgłasza błąd Zadania GLOBALNY
CREATE TABLE zgloszenie_bledu_zadania (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_id             uuid NOT NULL REFERENCES zadanie(id),
    zadanie_wersja_id      uuid NOT NULL REFERENCES zadanie_wersja(id),  -- wersja, której dotyczy zgłoszenie
    nauczyciel_id          uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL,
    opis                   text NOT NULL CHECK (char_length(opis) BETWEEN 10 AND 1000),
    status                 status_zgloszenia_zadania NOT NULL DEFAULT 'OTWARTE',
    rozpatrzone_przez      uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,   -- recenzent treści lub Administrator
    przeglad_zadania_id    uuid REFERENCES przeglad_zadania(id),               -- Przegląd wykonany przy rozpatrzeniu
    komentarz_rozpatrzenia text CHECK (char_length(komentarz_rozpatrzenia) <= 1000),
    created_at             timestamptz NOT NULL DEFAULT now(),
    rozpatrzono_at         timestamptz,
    CHECK ((status = 'OTWARTE') = (rozpatrzono_at IS NULL)),
    CHECK (status <> 'UWZGLEDNIONE' OR przeglad_zadania_id IS NOT NULL)
);
CREATE UNIQUE INDEX ux_zgloszenie_zadania_otwarte ON zgloszenie_bledu_zadania (zadanie_id, nauczyciel_id)
    WHERE status = 'OTWARTE';                              -- 1 otwarte zgłoszenie Nauczyciela na Zadanie
CREATE INDEX ix_zgloszenie_zadania_status ON zgloszenie_bledu_zadania (status, created_at);

-- Wstrzymanie w doborze (ADR-038, C3-09): stan systemowy poza wierszem zadanie, więc nie zmienia zadanie.version
-- (recenzent rozpatrujący zgłoszenie z If-Match nie dostaje 412 po nowym zgłoszeniu; wzorzec ADR-029).
-- Istnienie wiersza = Zadanie wstrzymane; usuwany po rozpatrzeniu wszystkich otwartych zgłoszeń (8.4).
CREATE TABLE wstrzymanie_w_doborze (
    zadanie_id        uuid PRIMARY KEY REFERENCES zadanie(id) ON DELETE CASCADE,
    liczba_zgloszen   smallint NOT NULL CHECK (liczba_zgloszen >= 3),  -- otwarte zgłoszenia od różnych Nauczycieli w chwili wstrzymania
    created_at        timestamptz NOT NULL DEFAULT now()
);

-- ===================== KLASY, ZESTAWY, PRZYPISANIA =====================
CREATE TABLE klasa (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nauczyciel_id   uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL, -- NULL tylko po usunięciu konta bez Przekazania Klasy (ADR-044)
    nazwa           text NOT NULL CHECK (char_length(nazwa) BETWEEN 1 AND 80),
    poziom_matury   poziom_matury NOT NULL,
    kod_zaproszenia char(8) NOT NULL UNIQUE,          -- alfabet [A-HJ-NP-Z2-9]
    kod_wazny_do    timestamptz NOT NULL,
    zarchiwizowana  boolean NOT NULL DEFAULT false,
    zarchiwizowano_at timestamptz,
    created_at      timestamptz NOT NULL DEFAULT now(),
    version         integer NOT NULL DEFAULT 1,
    CHECK (zarchiwizowana = (zarchiwizowano_at IS NOT NULL)),
    CHECK (nauczyciel_id IS NOT NULL OR zarchiwizowana)
);

CREATE TABLE czlonkostwo_klasy (
    klasa_id    uuid NOT NULL REFERENCES klasa(id) ON DELETE CASCADE,
    uczen_id    uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    dolaczono_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (klasa_id, uczen_id)
);
CREATE INDEX ix_czlonkostwo_uczen ON czlonkostwo_klasy (uczen_id);

CREATE TABLE zestaw_zadan (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nauczyciel_id uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL,   -- NULL po usunięciu konta (ADR-044)
    nazwa         text NOT NULL CHECK (char_length(nazwa) BETWEEN 1 AND 120),
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    version       integer NOT NULL DEFAULT 1
);

CREATE TABLE pozycja_zestawu (
    zestaw_zadan_id uuid NOT NULL REFERENCES zestaw_zadan(id) ON DELETE CASCADE,
    kolejnosc       smallint NOT NULL,
    zadanie_id      uuid NOT NULL REFERENCES zadanie(id),
    PRIMARY KEY (zestaw_zadan_id, kolejnosc),
    UNIQUE (zestaw_zadan_id, zadanie_id)
);

CREATE TABLE przypisanie (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zestaw_zadan_id uuid NOT NULL REFERENCES zestaw_zadan(id),   -- RESTRICT: Zestawu z Przypisaniem nie można usunąć
    nauczyciel_id   uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL, -- NULL po usunięciu konta (ADR-044)
    klasa_id        uuid REFERENCES klasa(id),        -- informacyjnie; adresaci w przypisanie_uczen
    termin          timestamptz NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_przypisanie_nauczyciel ON przypisanie (nauczyciel_id, termin DESC);

CREATE TABLE przypisanie_uczen (
    przypisanie_id uuid NOT NULL REFERENCES przypisanie(id) ON DELETE CASCADE,
    uczen_id       uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    PRIMARY KEY (przypisanie_id, uczen_id)
);
CREATE INDEX ix_przypisanie_uczen_uczen ON przypisanie_uczen (uczen_id);

-- ===================== PROFIL UCZNIA, TEST DIAGNOSTYCZNY =====================
CREATE TABLE profil_ucznia (
    uczen_id   uuid PRIMARY KEY REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    updated_at timestamptz NOT NULL DEFAULT now(),
    version    integer NOT NULL DEFAULT 1
);

CREATE TABLE opanowanie_umiejetnosci (
    uczen_id             uuid    NOT NULL REFERENCES profil_ucznia(uczen_id) ON DELETE CASCADE,
    umiejetnosc_id       integer NOT NULL REFERENCES umiejetnosc(id),
    theta                numeric(6,3) NOT NULL DEFAULT 0,  -- stan DOMYSLNY = 0; odtwarzanie zawsze od 0 (10.1.3)
    poziom_opanowania    numeric(4,3) NOT NULL CHECK (poziom_opanowania BETWEEN 0 AND 1),
    liczba_wynikow       integer NOT NULL DEFAULT 0,
    ostatni_wynik_at     timestamptz,
    licznik_powtorek     smallint NOT NULL DEFAULT 0,
    nastepna_powtorka_at timestamptz,
    inicjalizacja        inicjalizacja_opanowania NOT NULL DEFAULT 'DOMYSLNA',
    PRIMARY KEY (uczen_id, umiejetnosc_id)
);
CREATE INDEX ix_opanowanie_powtorki ON opanowanie_umiejetnosci (uczen_id, nastepna_powtorka_at);

CREATE TABLE test_diagnostyczny (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id      uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    poziom_matury poziom_matury NOT NULL,
    status        status_testu NOT NULL DEFAULT 'W_TOKU',
    pominiete_dzialy integer[] NOT NULL DEFAULT '{}', -- Działy bez Zadań diagnostycznych w chwili utworzenia (9.1)
    created_at    timestamptz NOT NULL DEFAULT now(),
    zakonczono_at timestamptz,
    CHECK ((status = 'W_TOKU') = (zakonczono_at IS NULL))
);
-- Test wykonuje się raz na poziom matury; POMINIETY można później zastąpić jednym testem (C1-11).
CREATE UNIQUE INDEX ux_test_jeden ON test_diagnostyczny (uczen_id, poziom_matury)
    WHERE status IN ('W_TOKU','ZAKONCZONY');

CREATE TABLE pozycja_testu_diagnostycznego (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    test_diagnostyczny_id uuid NOT NULL REFERENCES test_diagnostyczny(id) ON DELETE CASCADE,
    kolejnosc             smallint NOT NULL,
    zadanie_wersja_id     uuid NOT NULL REFERENCES zadanie_wersja(id),
    UNIQUE (test_diagnostyczny_id, kolejnosc)
);

-- ===================== ŚCIEŻKA NAUKI, REKOMENDACJE =====================
CREATE TABLE sciezka_nauki (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id                 uuid NOT NULL UNIQUE REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    seria_umiejetnosc_id     integer REFERENCES umiejetnosc(id),   -- aktywna Seria (ADR-030); NULL = brak
    seria_rozpoczeta_at      timestamptz,
    seria_liczba_elementow   smallint NOT NULL DEFAULT 0 CHECK (seria_liczba_elementow BETWEEN 0 AND 8),
    seria_poziom_trudnosci   smallint CHECK (seria_poziom_trudnosci BETWEEN 1 AND 5),
    seria_kolejne_porazki    smallint NOT NULL DEFAULT 0,
    created_at               timestamptz NOT NULL DEFAULT now(),
    version                  integer NOT NULL DEFAULT 1,
    CHECK ((seria_umiejetnosc_id IS NULL) = (seria_rozpoczeta_at IS NULL))
);

CREATE TABLE element_sciezki (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sciezka_nauki_id       uuid NOT NULL REFERENCES sciezka_nauki(id) ON DELETE CASCADE,
    kolejnosc              integer NOT NULL,
    typ                    typ_elementu_sciezki NOT NULL,
    cel                    cel_elementu NOT NULL DEFAULT 'NAUKA',
    umiejetnosc_id         integer NOT NULL REFERENCES umiejetnosc(id),
    zadanie_id             uuid REFERENCES zadanie(id),
    zadanie_wersja_id      uuid REFERENCES zadanie_wersja(id), -- wersja pokazana Uczniowi; ustawiana przy rozpoczęciu (C1-20)
    wkladka_teoretyczna_id uuid REFERENCES wkladka_teoretyczna(id),
    przypisanie_id         uuid REFERENCES przypisanie(id),  -- tylko reguła R-PRZYPISANIE; dziedziczone przez próbę (ADR-032)
    w_serii                boolean NOT NULL DEFAULT false,
    status                 status_elementu_sciezki NOT NULL DEFAULT 'PROPONOWANY',
    wygasa_at              timestamptz NOT NULL,       -- 8.5: PROPONOWANY +24 h; ROZPOCZETY: Zadanie +7 dni, Wkładka +24 h;
                                                       -- R-PRZYPISANIE z Zadaniem zamkniętym: najpóźniej termin Przypisania
    created_at             timestamptz NOT NULL DEFAULT now(),
    rozpoczeto_at          timestamptz,
    zakonczono_at          timestamptz,
    UNIQUE (sciezka_nauki_id, kolejnosc),
    CHECK ((typ = 'WKLADKA_TEORETYCZNA') = (wkladka_teoretyczna_id IS NOT NULL)),
    CHECK ((typ = 'ZADANIE') = (zadanie_id IS NOT NULL)),
    CHECK (typ = 'ZADANIE' OR cel = 'NAUKA'),
    CHECK (status NOT IN ('ROZPOCZETY','UKONCZONY') OR rozpoczeto_at IS NOT NULL),
    CHECK (typ <> 'ZADANIE' OR status NOT IN ('ROZPOCZETY','UKONCZONY') OR zadanie_wersja_id IS NOT NULL)
);
CREATE UNIQUE INDEX ux_jeden_proponowany ON element_sciezki (sciezka_nauki_id) WHERE status = 'PROPONOWANY';
CREATE INDEX ix_element_wygasanie ON element_sciezki (wygasa_at) WHERE status IN ('PROPONOWANY','ROZPOCZETY');

CREATE TABLE rekomendacja (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id              uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    element_sciezki_id    uuid NOT NULL UNIQUE REFERENCES element_sciezki(id) ON DELETE CASCADE,
    strategia             strategia_rekomendacji NOT NULL,
    regula                text NOT NULL CHECK (regula IN ('R-PRZYPISANIE','R-SERIA','R-POWTORKA','R-WKLADKA',
                                 'R-WKLADKA-DIAG','R-NAJSLABSZA','R-TRUDNOSC_W_GORE','R-UTRWALENIE',
                                 'R-JEV-WKLADKA','R-JEV-ZADANIE_LATWE','R-JEV-ZADANIE_MATURALNE','R-JEV-POWTORKA')),
    cechy                 jsonb NOT NULL,             -- migawka wejścia (10.1.4)
    decyzja               jsonb NOT NULL,             -- kandydaci, wybór, powrot_do_polityki
    przewidywane_p_sukcesu numeric(4,3),
    uzasadnienie          text NOT NULL,              -- FR-10, tekst dla Ucznia
    uzasadnienie_fakty    jsonb NOT NULL,             -- fakty użyte w szablonie
    created_at            timestamptz NOT NULL DEFAULT now()
);

-- ===================== ROZWIĄZANIA, TRANSKRYPCJE, ODPOWIEDZI =====================
CREATE TABLE rozwiazanie (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id                 uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    zadanie_id               uuid NOT NULL REFERENCES zadanie(id),
    zadanie_wersja_id        uuid NOT NULL REFERENCES zadanie_wersja(id),
    element_sciezki_id       uuid REFERENCES element_sciezki(id) ON DELETE SET NULL,
    przypisanie_id           uuid REFERENCES przypisanie(id),  -- także odziedziczone z elementu R-PRZYPISANIE (trg_kontekst_proby)
    status                   status_rozwiazania NOT NULL DEFAULT 'PRZESLANE',
    liczba_przeslan          smallint NOT NULL DEFAULT 1 CHECK (liczba_przeslan BETWEEN 1 AND 3),
                             -- przesłania zdjęć przez Ucznia (nowe zdjęcia), C1-28
    liczba_ponowien_rozpoznania smallint NOT NULL DEFAULT 0 CHECK (liczba_ponowien_rozpoznania BETWEEN 0 AND 5),
                             -- ponowienia rozpoznania tych samych zdjęć zlecone przez Ucznia po błędzie dostawcy
    kod_bledu_rozpoznawania  text CHECK (kod_bledu_rozpoznawania IN
                                 ('NIECZYTELNE','PRZEKROCZONY_CZAS','BLAD_DOSTAWCY','ZBYT_WIELE_KROKOW')),
    zatwierdzona_transkrypcja_id uuid,                 -- FK niżej
    przeslano_at             timestamptz NOT NULL DEFAULT now(),   -- ostatnie przesłanie lub ponowienie
    transkrypcja_gotowa_at   timestamptz,              -- pierwsza Transkrypcja po ostatnim przesłaniu
    zatwierdzono_at          timestamptz,
    ocena_wystawiona_at      timestamptz,              -- pierwsza Ocena widoczna dla Ucznia (NFR-01); okno zgłoszenia liczone od Oceny aktualnej (11.6)
    czas_systemowy_ms        integer,                  -- NFR-01: (transkrypcja_gotowa − przeslano) + (ocena_wystawiona − zatwierdzono)
    created_at               timestamptz NOT NULL DEFAULT now(),
    updated_at               timestamptz NOT NULL DEFAULT now(),
    version                  integer NOT NULL DEFAULT 1,
    -- kontekst próby (ADR-032, C1-21): element Ścieżki nauki, pozycja Przypisania albo oba — wtedy
    -- przypisanie_id = element_sciezki.przypisanie_id (element R-PRZYPISANIE), co egzekwuje trg_kontekst_proby
    CHECK (element_sciezki_id IS NOT NULL OR przypisanie_id IS NOT NULL)
);
CREATE INDEX ix_rozwiazanie_uczen ON rozwiazanie (uczen_id, created_at DESC);
CREATE INDEX ix_rozwiazanie_status ON rozwiazanie (status, updated_at);
CREATE UNIQUE INDEX ux_rozwiazanie_element ON rozwiazanie (element_sciezki_id)
    WHERE element_sciezki_id IS NOT NULL AND status NOT IN ('PORZUCONE','ANULOWANE');
CREATE UNIQUE INDEX ux_rozwiazanie_przypisanie ON rozwiazanie (uczen_id, przypisanie_id, zadanie_id)
    WHERE przypisanie_id IS NOT NULL AND status NOT IN ('PORZUCONE','ANULOWANE');
-- Trigger trg_kontekst_proby (BEFORE INSERT na rozwiazanie i odpowiedz), C1-21:
--   gdy element_sciezki_id IS NOT NULL: e := element_sciezki(element_sciezki_id);
--     e.przypisanie_id IS NOT NULL i NEW.przypisanie_id IS NULL → NEW.przypisanie_id := e.przypisanie_id (dziedziczenie);
--     NEW.przypisanie_id IS NOT NULL i NEW.przypisanie_id IS DISTINCT FROM e.przypisanie_id → wyjątek 'KONTEKST_PROBY_NIESPOJNY'.
--   Dzięki temu ux_*_przypisanie obejmują obie drogi (element R-PRZYPISANIE i lista Przypisań).

CREATE TABLE zdjecie_rozwiazania (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rozwiazanie_id uuid NOT NULL REFERENCES rozwiazanie(id) ON DELETE CASCADE,
    przeslanie_nr  smallint NOT NULL CHECK (przeslanie_nr BETWEEN 1 AND 3),
    kolejnosc      smallint NOT NULL CHECK (kolejnosc BETWEEN 1 AND 5),
    klucz_obiektu  text NOT NULL UNIQUE,              -- zdjecia/{uczen_id}/{rozwiazanie_id}/{id}.jpg
    klucz_obiektu_maskowany text UNIQUE,              -- zdjecia-maskowane/…; tworzony w ROZPOZNAJ_PISMO (ADR-031)
    obszary_maskowania jsonb NOT NULL DEFAULT '[]',   -- [{"ramka":[x,y,w,h]}] z Agenta rozpoznającego
    typ_mime       text NOT NULL CHECK (typ_mime = 'image/jpeg'), -- po normalizacji
    rozmiar_bajty  integer NOT NULL CHECK (rozmiar_bajty <= 10485760),
    sha256         bytea NOT NULL,
    szerokosc      smallint NOT NULL,
    wysokosc       smallint NOT NULL,
    usun_po        timestamptz NOT NULL,              -- utrzymywane przez przejścia stanów (16.4); ≤ created_at + 90 dni
    usunieto_at    timestamptz,
    created_at     timestamptz NOT NULL DEFAULT now(),
    UNIQUE (rozwiazanie_id, przeslanie_nr, kolejnosc),
    CHECK (usun_po <= created_at + interval '90 days')
);
CREATE INDEX ix_zdjecie_do_usuniecia ON zdjecie_rozwiazania (usun_po) WHERE usunieto_at IS NULL;

-- Kopie w zbiorze treningowym (16.4, ADR-035): jedyne powiązanie kopii z kontem; bez niego odwołanie
-- zgody i usunięcie konta nie mogłyby usunąć kopii. Wiersz żyje dłużej niż Rozwiązanie (brak FK do niego).
CREATE TABLE kopia_treningowa (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id          uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE RESTRICT, -- USUN_KONTO usuwa kopie przed kontem
    pseudonim_ucznia  uuid NOT NULL DEFAULT gen_random_uuid(), -- losowy per kopia; jedyne powiązanie z kontem to ten wiersz
    rozwiazanie_id    uuid,                           -- informacyjnie; NULL po usunięciu Rozwiązania
    klucz_obiektu     text NOT NULL UNIQUE,           -- zbior-treningowy/{pseudonim}/{id}.jpg (+ .json z Transkrypcją)
    ocena_id          uuid,                           -- informacyjnie: Ocena aktualna zapisana w .json; nowsza Ocena → nadpisanie .json
    usun_po           timestamptz NOT NULL,           -- created_at + 24 miesiące; now() po odwołaniu zgody
    usunieto_at       timestamptz,
    created_at        timestamptz NOT NULL DEFAULT now(),
    CHECK (usun_po <= created_at + interval '24 months')
);
CREATE INDEX ix_kopia_do_usuniecia ON kopia_treningowa (usun_po) WHERE usunieto_at IS NULL;
CREATE INDEX ix_kopia_uczen ON kopia_treningowa (uczen_id) WHERE usunieto_at IS NULL;

CREATE TABLE transkrypcja (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rozwiazanie_id         uuid NOT NULL REFERENCES rozwiazanie(id) ON DELETE CASCADE,
    wersja                 smallint NOT NULL CHECK (wersja >= 1),
    zrodlo                 zrodlo_transkrypcji NOT NULL,
    przeslanie_nr          smallint NOT NULL CHECK (przeslanie_nr BETWEEN 1 AND 3), -- zdjęcia, z których pochodzi
    konfiguracja_modelu_id uuid,                       -- dla AGENT_ROZPOZNAJACY (FK niżej)
    zgodnosc_z_zadaniem    numeric(4,3),               -- sygnał agenta: czy kartka dotyczy Zadania
    zakres_poprawek        jsonb,                      -- ADR-036: {"zmienione":2,"wstawione":0,"kroki_agenta":9,"odsetek_znakow":0.08}
    zgodnosc_poprawek      jsonb,                      -- ADR-036: rezultat SPRAWDZ_POPRAWKI_TRANSKRYPCJI (schemat 10.3.3); NULL = brak rezultatu
    dane_osobowe_usuniete  smallint NOT NULL DEFAULT 0,-- liczba zastąpień tokenów przez Filtr danych osobowych (ADR-042); kroków się nie usuwa
    konflikt_filtra        boolean NOT NULL DEFAULT false,
                           -- ≥ 1 krok niezmieniony przez Ucznia z podejrzenie_danych_osobowych i treścią matematyczną
                           -- (tokeny imienia/nazwiska z konta nie są treścią matematyczną, C4-03) → T-3 (11.1 p. 7d, C3-02)
    created_at             timestamptz NOT NULL DEFAULT now(),
    UNIQUE (rozwiazanie_id, wersja),
    CHECK ((zrodlo = 'AGENT_ROZPOZNAJACY') = (konfiguracja_modelu_id IS NOT NULL)),
    CHECK ((zrodlo = 'POPRAWKA_UCZNIA') = (zakres_poprawek IS NOT NULL)),
    CHECK (zrodlo = 'POPRAWKA_UCZNIA' OR zgodnosc_poprawek IS NULL)
);
ALTER TABLE rozwiazanie ADD CONSTRAINT fk_rozwiazanie_transkrypcja
    FOREIGN KEY (zatwierdzona_transkrypcja_id) REFERENCES transkrypcja(id);

CREATE TABLE krok_transkrypcji (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transkrypcja_id     uuid NOT NULL REFERENCES transkrypcja(id) ON DELETE CASCADE,
    nr                  smallint NOT NULL CHECK (nr BETWEEN 1 AND 40),
    tekst               text NOT NULL CHECK (char_length(tekst) <= 2000),   -- opis słowny
    latex               text NOT NULL CHECK (char_length(latex) <= 2000),   -- wzory
    pewnosc_odczytu     numeric(4,3) CHECK (pewnosc_odczytu BETWEEN 0 AND 1), -- NULL dla kroku zmienionego przez Ucznia
    niepewne_fragmenty  jsonb NOT NULL DEFAULT '[]',   -- schemat 10.3.2
    obszar              jsonb,                         -- {"zdjecie":1,"ramka":[x,y,w,h]} linii kroku (10.3.2); NULL dla wstawionego
    zmieniony_przez_ucznia boolean NOT NULL DEFAULT false,
    nr_kroku_agenta     smallint CHECK (nr_kroku_agenta BETWEEN 1 AND 40),
                        -- numer kroku w Transkrypcji agenta, który ten krok poprawia (ADR-036); NULL = krok wstawiony
    odleglosc_edycyjna  numeric(4,3) CHECK (odleglosc_edycyjna BETWEEN 0 AND 1),
                        -- znormalizowana odległość Levenshteina od kroku agenta; NULL dla kroku agenta i wstawionego
    podejrzenie_danych_osobowych boolean NOT NULL DEFAULT false,
                        -- obszar kroku pokrywa się w ≥ 50% z ramką danych osobowych (Filtr 10.3.1 p. 3a, C3-02)
    tylko_dane_osobowe  boolean NOT NULL DEFAULT false,
                        -- krok bez treści matematycznej w ramce danych osobowych: tekst = '[DANE_OSOBOWE]', latex = ''
    zastapienia_danych_osobowych jsonb NOT NULL DEFAULT '[]',
                        -- [{"od":12,"do":26,"rodzaj":"DOKLADNE"|"PRZYBLIZONE"}] — pozycje znacznika w tekst; bez oryginału
    UNIQUE (transkrypcja_id, nr),
    CHECK (NOT tylko_dane_osobowe OR podejrzenie_danych_osobowych),
    CHECK (zmieniony_przez_ucznia = (pewnosc_odczytu IS NULL)),
    CHECK (nr_kroku_agenta IS NOT NULL OR zmieniony_przez_ucznia)
);

CREATE TABLE odpowiedz (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id           uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    zadanie_id         uuid NOT NULL REFERENCES zadanie(id),
    zadanie_wersja_id  uuid NOT NULL REFERENCES zadanie_wersja(id),
    element_sciezki_id uuid REFERENCES element_sciezki(id) ON DELETE SET NULL,
    przypisanie_id     uuid REFERENCES przypisanie(id),          -- także odziedziczone z elementu R-PRZYPISANIE
    pozycja_testu_diagnostycznego_id uuid REFERENCES pozycja_testu_diagnostycznego(id) ON DELETE CASCADE,
    wybor              jsonb NOT NULL,                -- {"opcja":"B"} | {"stwierdzenia":{"1":true,"2":false}}
    czy_poprawna       boolean NOT NULL,
    punkty             smallint NOT NULL,
    punkty_max         smallint NOT NULL,
    rezultat_ujawniony_od timestamptz,                -- przypisanie_id → greatest(now(), termin); element bez Przypisania → now();
                                                      -- Test diagnostyczny → NULL do …/zakonczenie lub …/pominiecie (C2-16)
    created_at         timestamptz NOT NULL DEFAULT now(),
    CHECK (punkty BETWEEN 0 AND punkty_max),
    -- kontekst próby (C1-21): dozwolone kombinacje {E}, {P}, {E, P} (element R-PRZYPISANIE, trg_kontekst_proby), {T}
    CHECK (num_nonnulls(element_sciezki_id, przypisanie_id, pozycja_testu_diagnostycznego_id) BETWEEN 1 AND 2),
    CHECK (pozycja_testu_diagnostycznego_id IS NULL OR (element_sciezki_id IS NULL AND przypisanie_id IS NULL)),
    CHECK (pozycja_testu_diagnostycznego_id IS NOT NULL OR rezultat_ujawniony_od IS NOT NULL)
);
CREATE UNIQUE INDEX ux_odpowiedz_element ON odpowiedz (element_sciezki_id) WHERE element_sciezki_id IS NOT NULL;
CREATE UNIQUE INDEX ux_odpowiedz_test ON odpowiedz (pozycja_testu_diagnostycznego_id)
    WHERE pozycja_testu_diagnostycznego_id IS NOT NULL;
CREATE UNIQUE INDEX ux_odpowiedz_przypisanie ON odpowiedz (uczen_id, przypisanie_id, zadanie_id)
    WHERE przypisanie_id IS NOT NULL;

-- ===================== MODELE, KALIBRACJA =====================
CREATE TABLE konfiguracja_modelu (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rola             rola_modelu NOT NULL,
    dostawca         text NOT NULL,                   -- klucz adaptera, np. 'typesafe-jev'
    model            text NOT NULL,                   -- identyfikator modelu u dostawcy
    parametry        jsonb NOT NULL DEFAULT '{}',     -- schemat 13.1 (timeout_s, ceny, retencja_dostawcy_dni, …)
    wersja_promptu   text NOT NULL,                   -- np. 'ocena-v3' (plik w repozytorium)
    aktywna          boolean NOT NULL DEFAULT false,
    utworzona_przez  uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,
    created_at       timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_konfiguracja_aktywna ON konfiguracja_modelu (rola) WHERE aktywna;

-- Globalny ogranicznik szybkości (ADR-039): rejestr kubełków korzystających z klucza API modelu;
-- przydział wiersza i = min(0.8 × limit_klucza × waga_i / Σ waga_j, 0.8 × limit_klucza − Σ_{j≠i} przydzial_j)
-- po wierszach z odnowiono_at > now() − 30 s (jedna formuła w 13.2, 17.4, 22 i ADR-039; C3-04, C4-05);
-- pojemność: stała 1 dla api, pozostałe wiersze dzielą B_całk − n_api proporcjonalnie do wagi (13.2), Σ pojemnosc ≤ B_całk
CREATE UNLOGGED TABLE limit_szybkosci_modelu (
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id) ON DELETE CASCADE,
    wykonawca              text NOT NULL,             -- proces, np. 'worker#1', 'api#2'
    pula                   text NOT NULL DEFAULT '*'
                           CHECK (pula IN ('*','ROZPOZNAWANIE','SPRAWDZANIE')),
                                                      -- '*' = cały proces (Jev: pule O i W, api); dla roli ROZPOZNAWANIE_PISMA
                                                      -- osobny wiersz na pulę R i S tej samej repliki (C4-06 p. 8)
    waga                   numeric(3,2) NOT NULL CHECK (waga > 0 AND waga <= 1),
                                                      -- Jev: worker 1.00, api 0.10; rozpoznawanie: R 1.00, S 0.30 (parametr 22)
    pojemnosc              smallint NOT NULL CHECK (pojemnosc BETWEEN 1 AND 34),
                                                      -- opublikowana pojemność kubełka; Jev, 2 workery + 2 api: worker 16, api 1
    przydzial              numeric(6,2) NOT NULL DEFAULT 0 CHECK (przydzial >= 0),
                                                      -- opublikowany przydział (wywołania/s; rozpoznawanie: /min), używany przez proces
    odnowiono_at           timestamptz NOT NULL DEFAULT now(),   -- odnawiane co 10 s (co 2 s w trybie dołączania)
    PRIMARY KEY (konfiguracja_modelu_id, wykonawca, pula)
);
ALTER TABLE transkrypcja ADD CONSTRAINT fk_transkrypcja_konfiguracja
    FOREIGN KEY (konfiguracja_modelu_id) REFERENCES konfiguracja_modelu(id);

-- Kalibrator pewności (Kalibracja, ADR-006/007): funkcja pewnosc_surowa → pewnosc_oceny
CREATE TABLE kalibrator_pewnosci (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id),
    rodzaj_zadania         rodzaj_zadania NOT NULL CHECK (rodzaj_zadania <> 'ZAMKNIETE'),
    metoda                 text NOT NULL CHECK (metoda IN ('TOZSAMOSCIOWY','IZOTONICZNY_CV5')),
    punkty                 jsonb NOT NULL,            -- schemat 11.2.2
    liczba_probek          integer NOT NULL,
    ece                    numeric(5,4),              -- Expected Calibration Error, out-of-fold, 10 koszyków
    okno_od                timestamptz,
    okno_do                timestamptz,
    zrodlo                 zrodlo_kalibratora NOT NULL,
    created_at             timestamptz NOT NULL DEFAULT now()
);

-- Próg pewności: wyłącznie wartość progu i jego uzasadnienie (C1-03)
CREATE TABLE prog_pewnosci (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id),
    rodzaj_zadania         rodzaj_zadania NOT NULL CHECK (rodzaj_zadania <> 'ZAMKNIETE'),
    kalibrator_pewnosci_id uuid NOT NULL REFERENCES kalibrator_pewnosci(id),
    wartosc                numeric(4,3) NOT NULL CHECK (wartosc BETWEEN 0.800 AND 1.000),
    gorna_granica_bledu_auto numeric(5,4),            -- Wilson 95% w chwili wyboru progu
    n_eff                  numeric(8,1),              -- efektywna liczność próby powyżej progu
    zrodlo                 zrodlo_progu NOT NULL,
    uzasadnienie           text NOT NULL,
    aktywny                boolean NOT NULL DEFAULT false,
    utworzony_przez        uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,
    created_at             timestamptz NOT NULL DEFAULT now(),
    CHECK (rodzaj_zadania <> 'OTWARTE_Z_DOWODEM' OR wartosc = 1.000)
);
CREATE UNIQUE INDEX ux_prog_aktywny ON prog_pewnosci (konfiguracja_modelu_id, rodzaj_zadania) WHERE aktywny;

-- Stan bezpieczników (ADR-046, C3-01): jeden wiersz na (konfiguracja decyzyjna, rodzaj, typ); zapis przez KALIBRUJ.
-- Zastępuje flagę LEMAT_POPRAWKI_ZAWSZE_DO_WERYFIKACJI z wersji 2.2.
CREATE TABLE stan_bezpiecznika (
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id),
    rodzaj_zadania         rodzaj_zadania NOT NULL CHECK (rodzaj_zadania = 'OTWARTE_OBLICZENIOWE'),
    typ                    typ_bezpiecznika NOT NULL,
    aktywny                boolean NOT NULL DEFAULT false,      -- POPRAWKI: każda Transkrypcja z Poprawkami → T-7;
                                                                -- TRAFNOSC (od 2.4, C4-02): Blokada obniżania progu po wyzwoleniu
    okno_od                timestamptz NOT NULL DEFAULT now(),  -- początek okna oceny; ustawiany przy dezaktywacji (C4-04)
    n_eff                  numeric(8,1),                        -- ostatnia ocena: efektywna liczność Kontroli w oknie
    blad_ipw               numeric(5,4),                        -- ostatnia ocena: błąd ważony IPW
    dolna_granica_bledu    numeric(5,4),                        -- Wilson 95%, LB
    gorna_granica_bledu    numeric(5,4),                        -- Wilson 95%, UB
    ostatnie_wyzwolenie_at timestamptz,
    wyjscie_z_blokady_at   timestamptz,                         -- TRAFNOSC: automatyczne wyjście (UB ≤ 2% przy n_eff ≥ 189)
    dezaktywowany_przez    uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,   -- nazwy od 2.4 (C4-04)
    dezaktywowano_at       timestamptz,
    uzasadnienie_dezaktywacji text CHECK (char_length(uzasadnienie_dezaktywacji) <= 1000),
    updated_at             timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (konfiguracja_modelu_id, rodzaj_zadania, typ),
    CHECK ((dezaktywowano_at IS NULL) = (uzasadnienie_dezaktywacji IS NULL))
    -- TRAFNOSC podnosi próg przez prog_pewnosci (zrodlo = BEZPIECZNIK); aktywny = blokada obniżania (11.5.3, C4-02)
);

CREATE TABLE decyzja_modelu (
    id                     uuid NOT NULL DEFAULT gen_random_uuid(),
    created_at             timestamptz NOT NULL DEFAULT now(),
    kontekst               kontekst_decyzji NOT NULL,
    kontekst_id            uuid NOT NULL,             -- id Oceny w przygotowaniu / Rekomendacji / zadanie_wersja
    uczen_id               uuid,                      -- OCENA i REKOMENDACJA: NOT NULL aplikacyjnie; usuwanie RODO (16.4)
    rozwiazanie_id         uuid,                      -- dla kontekstu OCENA
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id),
    kod_pytania            text NOT NULL,             -- np. 'Q-OC-02.1.K3'
    typ_pytania            typ_pytania NOT NULL,
    opcje                  jsonb,                     -- dla CHOICE: lista opcji, SCORE: {"min":0,"max":2}
    wybor                  jsonb,                     -- wybrana wartość (moda); NULL przy błędzie
    rozklad                jsonb,                     -- {"opcja": p, ...}, suma = 1 ± 0.001
    czas_ms                integer NOT NULL,
    skrot_stanu            bytea NOT NULL,            -- SHA-256 stanu wejściowego
    stan                   text,                      -- pełny stan; zerowany po 180 dniach lub przy usunięciu konta
    blad                   text,
    PRIMARY KEY (id, created_at),
    CHECK (kontekst = 'WALIDACJA_ZADANIA' OR uczen_id IS NOT NULL OR stan IS NULL)
) PARTITION BY RANGE (created_at);
-- Partycje miesięczne tworzone przez pracę w tle UTRZYMAJ_PARTYCJE (3 miesiące naprzód).
CREATE INDEX ix_decyzja_kontekst ON decyzja_modelu (kontekst, kontekst_id);
CREATE INDEX ix_decyzja_uczen ON decyzja_modelu (uczen_id) WHERE uczen_id IS NOT NULL;

-- ===================== OCENY, WERYFIKACJE =====================
CREATE TABLE ocena (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rozwiazanie_id         uuid NOT NULL REFERENCES rozwiazanie(id) ON DELETE CASCADE,
    wersja                 smallint NOT NULL CHECK (wersja >= 1),
    status                 status_oceny NOT NULL,
    zrodlo                 zrodlo_oceny NOT NULL,
    transkrypcja_id        uuid NOT NULL REFERENCES transkrypcja(id),
    wariant                smallint NOT NULL,         -- przy N-5 (ZADEN): wariant o najwyższym p spośród W1…Wn
    punkty_surowe          smallint,                  -- Σ pk przed regułą FR-20 (11.1 p. 4); NULL dla Oceny Nauczyciela
    punkty                 smallint NOT NULL,
    punkty_max             smallint NOT NULL,
    pierwszy_bledny_krok   smallint,                  -- nr kroku Transkrypcji; NULL = brak błędnego kroku Ucznia
    brakujacy_krok         boolean NOT NULL DEFAULT false,
    typ_bledu              typ_bledu,
    krok_podejrzany_odczyt smallint,                  -- krok, w którym model podejrzewa błąd odczytu (ADR-022)
    wskazowka              text CHECK (char_length(wskazowka) <= 400),   -- jednokrotny zapis (Z-5)
    zrodlo_wskazowki       zrodlo_wskazowki,
    uzasadnienie           text NOT NULL,             -- generowane z decyzji (szablon 11.7); widoczne w OcenaWyjscie
    pewnosc_oceny          numeric(5,4) CHECK (pewnosc_oceny BETWEEN 0 AND 1),
    pewnosc_surowa         numeric(5,4),
    prog_pewnosci_id       uuid REFERENCES prog_pewnosci(id),
    kalibrator_pewnosci_id uuid REFERENCES kalibrator_pewnosci(id),
    konfiguracja_modelu_id uuid REFERENCES konfiguracja_modelu(id),
    mozliwy_blad_odczytu   boolean NOT NULL DEFAULT false,
    nauczyciel_id          uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,
    created_at             timestamptz NOT NULL DEFAULT now(),
    UNIQUE (rozwiazanie_id, wersja),
    CHECK (punkty BETWEEN 0 AND punkty_max),
    CHECK ((zrodlo = 'AUTOMATYCZNA') = (pewnosc_oceny IS NOT NULL AND prog_pewnosci_id IS NOT NULL
                                         AND kalibrator_pewnosci_id IS NOT NULL
                                         AND konfiguracja_modelu_id IS NOT NULL AND punkty_surowe IS NOT NULL)),
    CHECK (zrodlo = 'NAUCZYCIEL' OR nauczyciel_id IS NULL),  -- Nauczyciel wymagany aplikacyjnie przy zapisie
    CHECK (zrodlo = 'AUTOMATYCZNA' OR status <> 'WSTEPNA'),
    CHECK (zrodlo = 'AUTOMATYCZNA' OR NOT mozliwy_blad_odczytu),
    CHECK ((typ_bledu IS NULL) = (pierwszy_bledny_krok IS NULL AND NOT brakujacy_krok)),
    CHECK (krok_podejrzany_odczyt IS NULL OR mozliwy_blad_odczytu),
    CHECK ((wskazowka IS NULL) = (zrodlo_wskazowki IS NULL)),
    CHECK (typ_bledu IS NOT NULL OR wskazowka IS NULL)
);
CREATE UNIQUE INDEX ux_ocena_aktualna ON ocena (rozwiazanie_id) WHERE status <> 'ZASTAPIONA';
-- Trigger trg_ocena_niezmienna (BEFORE UPDATE): dozwolone wyłącznie
--   (a) status WSTEPNA|OSTATECZNA → ZASTAPIONA,
--   (b) wskazowka i zrodlo_wskazowki NULL → wartość (jednokrotnie);
-- każda inna zmiana kolumn → wyjątek 'OCENA_NIEZMIENNA'.

CREATE TABLE ocena_kryterium (
    ocena_id            uuid NOT NULL REFERENCES ocena(id) ON DELETE CASCADE,
    kryterium_oceniania_id uuid NOT NULL REFERENCES kryterium_oceniania(id),
    punkty              smallint NOT NULL CHECK (punkty >= 0),
    prawdopodobienstwo  numeric(5,4),                 -- NULL dla Oceny Nauczyciela
    PRIMARY KEY (ocena_id, kryterium_oceniania_id)
);

CREATE TABLE weryfikacja (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rozwiazanie_id          uuid NOT NULL REFERENCES rozwiazanie(id) ON DELETE CASCADE,
    ocena_weryfikowana_id   uuid REFERENCES ocena(id),  -- NULL wyłącznie przy BLAD_PRZETWARZANIA przed PONOW_OCENE
    powody                  powod_weryfikacji[] NOT NULL CHECK (cardinality(powody) >= 1),
    powody_poczatkowe       powod_weryfikacji[] NOT NULL CHECK (cardinality(powody_poczatkowe) >= 1),
                            -- powody w chwili dołączenia Oceny weryfikowanej (INSERT albo PONOW_OCENE); źródło Próbki (11.5.2)
    priorytet               smallint NOT NULL CHECK (priorytet BETWEEN 1 AND 3),
    typ_kolejki             typ_kolejki_weryfikacji NOT NULL,
    klasa_id                uuid REFERENCES klasa(id) ON DELETE SET NULL,
    nauczyciel_docelowy_id  uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL,
    status                  status_weryfikacji NOT NULL DEFAULT 'OCZEKUJE',
    termin_at               timestamptz NOT NULL,     -- SLA: dodaj_dni_robocze(created_at, 3); po eskalacji: dodaj_dni_robocze(eskalowano_at, 3)
    eskalowano_at           timestamptz,
    powod_eskalacji         powod_eskalacji,          -- KARENCJA_KONTA_NAUCZYCIELA jest cofana przy przywróceniu konta (16.4, C3-08)
    podjeta_przez           uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL,
    podjeta_at              timestamptz,              -- początek bieżącej blokady (limit 2 h, 11.4.2)
    zablokowana_do          timestamptz,
    ocena_nauczyciela_id    uuid REFERENCES ocena(id),
    zgloszenie              jsonb,                    -- schemat 7.5.4; warunki 11.6
    ponowna_ocena_at        timestamptz,              -- PONOW_OCENE dołączyła Ocenę wstępną (9.13)
    powod_anulowania        powod_anulowania,
    opinia_pomocnicza       jsonb,                    -- schemat 10.4.5
    created_at              timestamptz NOT NULL DEFAULT now(),
    zakonczono_at           timestamptz,
    version                 integer NOT NULL DEFAULT 1,  -- wersja TREŚCI (ADR-037), ustawiana wyłącznie przez trg_weryfikacja_wersja
    CHECK ((status = 'W_TRAKCIE') = (podjeta_przez IS NOT NULL AND zablokowana_do IS NOT NULL AND podjeta_at IS NOT NULL)),
    CHECK ((status = 'ZAKONCZONA') = (ocena_nauczyciela_id IS NOT NULL)),
    CHECK ((status = 'ANULOWANA') = (powod_anulowania IS NOT NULL)),
    CHECK (ocena_weryfikowana_id IS NOT NULL OR 'BLAD_PRZETWARZANIA' = ANY (powody)),
    CHECK (('ZGLOSZENIE_BLEDU_ODCZYTU' = ANY (powody)) = (zgloszenie IS NOT NULL)),
    CHECK ((status IN ('ZAKONCZONA','ANULOWANA')) = (zakonczono_at IS NOT NULL)),
    CHECK ((eskalowano_at IS NULL) = (powod_eskalacji IS NULL))
);
CREATE UNIQUE INDEX ux_weryfikacja_otwarta ON weryfikacja (rozwiazanie_id) WHERE status IN ('OCZEKUJE','W_TRAKCIE');
CREATE INDEX ix_weryfikacja_kolejka ON weryfikacja (typ_kolejki, nauczyciel_docelowy_id, status, priorytet, termin_at);
CREATE INDEX ix_weryfikacja_blokady ON weryfikacja (zablokowana_do) WHERE status = 'W_TRAKCIE';
-- Trigger trg_weryfikacja_wersja (BEFORE UPDATE), ADR-037:
--   NEW.version := OLD.version + 1, gdy zmienia się którakolwiek kolumna TREŚCI: powody, zgloszenie,
--   ocena_weryfikowana_id, ponowna_ocena_at, ocena_nauczyciela_id, powod_anulowania albo status przechodzi
--   do ZAKONCZONA/ANULOWANA; w przeciwnym razie NEW.version := OLD.version. Kolumny BLOKADY (status
--   OCZEKUJE↔W_TRAKCIE, podjeta_przez, podjeta_at, zablokowana_do), TRASOWANIA (typ_kolejki, eskalowano_at,
--   termin_at, priorytet, klasa_id, nauczyciel_docelowy_id) i opinia_pomocnicza nie zmieniają version.
-- Trigger trg_powody_poczatkowe (BEFORE UPDATE): powody_poczatkowe może zmienić wyłącznie przejście
--   ocena_weryfikowana_id NULL → wartość (PONOW_OCENE, 9.13); każda inna zmiana → wyjątek 'POWODY_NIEZMIENNE'.

-- Autozapis rozstrzygnięcia w toku (C1-06); widoczny tylko dla autora
CREATE TABLE szkic_rozstrzygniecia (
    weryfikacja_id uuid NOT NULL REFERENCES weryfikacja(id) ON DELETE CASCADE,
    nauczyciel_id  uuid NOT NULL REFERENCES nauczyciel(uzytkownik_id) ON DELETE CASCADE,
    dane           jsonb NOT NULL,                    -- kształt RozstrzygniecieWejscie (12.4), pola opcjonalne
    updated_at     timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (weryfikacja_id, nauczyciel_id)
);

CREATE TABLE probka_kalibracyjna (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rozwiazanie_id         uuid REFERENCES rozwiazanie(id) ON DELETE SET NULL,
    weryfikacja_id         uuid REFERENCES weryfikacja(id) ON DELETE SET NULL,
    pseudonim_ucznia       uuid NOT NULL,             -- losowy, niepowiązany z kontem po usunięciu
    ocena_automatyczna_id  uuid REFERENCES ocena(id) ON DELETE SET NULL,
    ocena_nauczyciela_id   uuid REFERENCES ocena(id) ON DELETE SET NULL,
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id),
    rodzaj_zadania         rodzaj_zadania NOT NULL,
    zrodlo_probki          zrodlo_probki NOT NULL,    -- reguła przypisania 11.5.2
    prawdopodobienstwo_doboru numeric(5,4) NOT NULL CHECK (prawdopodobienstwo_doboru > 0 AND prawdopodobienstwo_doboru <= 1),
                                                      -- 1 dla NISKA_PEWNOSC; odsetek Kontroli losowej w chwili Oceny
    pewnosc_surowa         numeric(5,4) NOT NULL,
    pewnosc_oceny          numeric(5,4) NOT NULL,
    punkty_automatyczne    smallint NOT NULL,
    punkty_nauczyciela     smallint NOT NULL,
    zgodny_pierwszy_bledny_krok boolean NOT NULL,     -- oba NULL albo równe
    zgodny_brakujacy_krok  boolean NOT NULL,
    zgodny_typ_bledu       boolean NOT NULL,          -- metryka pomocnicza, poza definicją zgodności
    zgodna                 boolean GENERATED ALWAYS AS
                           (punkty_automatyczne = punkty_nauczyciela AND zgodny_pierwszy_bledny_krok
                            AND zgodny_brakujacy_krok) STORED,       -- Zgodność Oceny (ADR-027)
    uwzgledniana_w_progu   boolean GENERATED ALWAYS AS
                           (zrodlo_probki IN ('WERYFIKACJA_NISKA_PEWNOSC','KONTROLA_LOSOWA')) STORED,
    blad_odczytu_wplynal   boolean NOT NULL DEFAULT false, -- Nauczyciel: błąd odczytu zmienił punkty (metryka T-3)
    kryteria               jsonb NOT NULL,            -- [{"nr_kryterium":1,"auto":1,"nauczyciel":1}, ...]
    created_at             timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_probka_ocena_auto ON probka_kalibracyjna (ocena_automatyczna_id)
    WHERE ocena_automatyczna_id IS NOT NULL;              -- co najwyżej jedna próbka na Ocenę automatyczną
CREATE INDEX ix_probka_kalibracja ON probka_kalibracyjna (konfiguracja_modelu_id, rodzaj_zadania, created_at);

CREATE TABLE wynik_zadania (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id           uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    zadanie_id         uuid NOT NULL REFERENCES zadanie(id),
    zadanie_wersja_id  uuid NOT NULL REFERENCES zadanie_wersja(id),
    zrodlo             zrodlo_wyniku NOT NULL,
    odpowiedz_id       uuid REFERENCES odpowiedz(id) ON DELETE CASCADE,
    ocena_id           uuid REFERENCES ocena(id) ON DELETE CASCADE,
    test_diagnostyczny_id uuid REFERENCES test_diagnostyczny(id) ON DELETE CASCADE, -- Wynik z Testu diagnostycznego
    ulamek             numeric(4,3) NOT NULL CHECK (ulamek BETWEEN 0 AND 1), -- punkty / punkty_max
    waga               numeric(3,2) NOT NULL CHECK (waga IN (0.25, 0.50, 1.00)),  -- reguła 10.1.3
    liczy_do_trudnosci boolean NOT NULL,              -- pierwsza próba Ucznia przy tym Zadaniu w 60 dni i waga 1.00
    uniewazniony_at    timestamptz,
    powod_uniewaznienia powod_uniewaznienia_wyniku,   -- ZGLOSZENIE_BLEDU_ODCZYTU = zawieszenie na czas zgłoszenia (11.6)
    zastosowany_w_profilu_at       timestamptz,       -- ustawiane przez AKTUALIZUJ_PROFIL (idempotencja)
    uniewaznienie_przetworzone_at  timestamptz,       -- ustawiane po odtworzeniu Profilu ucznia
    created_at         timestamptz NOT NULL DEFAULT now(),  -- dla Testu diagnostycznego = zakonczono_at testu
    CHECK ((zrodlo = 'ODPOWIEDZ') = (odpowiedz_id IS NOT NULL)),
    CHECK ((zrodlo = 'OCENA') = (ocena_id IS NOT NULL)),
    CHECK (test_diagnostyczny_id IS NULL OR (zrodlo = 'ODPOWIEDZ' AND waga = 1.00)),
    CHECK ((uniewazniony_at IS NULL) = (powod_uniewaznienia IS NULL)),
    CHECK (powod_uniewaznienia IS DISTINCT FROM 'ZGLOSZENIE_BLEDU_ODCZYTU' OR zrodlo = 'OCENA')
);
CREATE INDEX ix_wynik_uczen ON wynik_zadania (uczen_id, created_at) WHERE uniewazniony_at IS NULL;
CREATE INDEX ix_wynik_uczen_zadanie ON wynik_zadania (uczen_id, zadanie_id, created_at) WHERE uniewazniony_at IS NULL;
CREATE UNIQUE INDEX ux_wynik_ocena ON wynik_zadania (ocena_id) WHERE ocena_id IS NOT NULL;
CREATE UNIQUE INDEX ux_wynik_odpowiedz ON wynik_zadania (odpowiedz_id) WHERE odpowiedz_id IS NOT NULL;
CREATE INDEX ix_wynik_do_profilu ON wynik_zadania (uczen_id)
    WHERE zastosowany_w_profilu_at IS NULL
       OR (uniewazniony_at IS NOT NULL AND uniewaznienie_przetworzone_at IS NULL);

-- ===================== ORKIESTRATOR, INFRASTRUKTURA =====================
CREATE TABLE praca_w_tle (
    id                bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    typ               text NOT NULL,                  -- katalog 13.3
    pula              text NOT NULL CHECK (pula IN ('ROZPOZNAWANIE','SPRAWDZANIE','OCENIANIE','POMOCNICZA','WOLNA')),
    klucz_deduplikacji text,
    ladunek           jsonb NOT NULL,
    status            status_pracy NOT NULL DEFAULT 'OCZEKUJE',
    priorytet         smallint NOT NULL DEFAULT 5,    -- 1 = najwyższy (w obrębie puli)
    proby             smallint NOT NULL DEFAULT 0,
    max_prob          smallint NOT NULL,
    dostepna_od       timestamptz NOT NULL DEFAULT now(),
    zablokowana_do    timestamptz,
    wykonawca         text,
    ostatni_blad      text,
    created_at        timestamptz NOT NULL DEFAULT now(),
    updated_at        timestamptz NOT NULL DEFAULT now()
);
-- Deduplikacja (C2-04, C3-05): dla AKTUALIZUJ_PROFIL i SPRAWDZ_POPRAWKI_TRANSKRYPCJI obejmuje tylko prace OCZEKUJE —
-- zlecenie w trakcie wykonania poprzedniej tworzy nową pracę, więc żadne wybudzenie ani nowa wersja Poprawek nie ginie;
-- dla pozostałych typów OCZEKUJE i W_TOKU.
CREATE UNIQUE INDEX ux_praca_dedup ON praca_w_tle (typ, klucz_deduplikacji)
    WHERE status IN ('OCZEKUJE','W_TOKU') AND klucz_deduplikacji IS NOT NULL
      AND typ NOT IN ('AKTUALIZUJ_PROFIL','SPRAWDZ_POPRAWKI_TRANSKRYPCJI');
CREATE UNIQUE INDEX ux_praca_dedup_oczekujace ON praca_w_tle (typ, klucz_deduplikacji)
    WHERE status = 'OCZEKUJE' AND klucz_deduplikacji IS NOT NULL
      AND typ IN ('AKTUALIZUJ_PROFIL','SPRAWDZ_POPRAWKI_TRANSKRYPCJI');
CREATE INDEX ix_praca_pobieranie ON praca_w_tle (pula, priorytet, dostepna_od) WHERE status = 'OCZEKUJE';
CREATE INDEX ix_praca_dzierzawa ON praca_w_tle (zablokowana_do) WHERE status = 'W_TOKU';

CREATE TABLE klucz_idempotencji (
    uzytkownik_id     uuid NOT NULL REFERENCES uzytkownik(id) ON DELETE CASCADE,
    klucz             uuid NOT NULL,                  -- nagłówek Idempotency-Key
    skrot_zapytania   bytea NOT NULL,                 -- SHA-256(metoda + ścieżka + ciało)
    kod_statusu_http  smallint,                       -- zapisany status HTTP odpowiedzi (3.1 p. 5)
    cialo_http        jsonb,                          -- zapisane ciało odpowiedzi HTTP
    wygasa_at         timestamptz NOT NULL,           -- created_at + 24 h
    created_at        timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (uzytkownik_id, klucz)
);

CREATE TABLE powiadomienie (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uzytkownik_id uuid NOT NULL REFERENCES uzytkownik(id) ON DELETE CASCADE,
    typ           text NOT NULL CHECK (typ IN ('OCENA_WYSTAWIONA','OCENA_SKORYGOWANA','NOWE_PRZYPISANIE',
                                              'NOWA_WERYFIKACJA','ZADANIE_DO_PRZEGLADU','WKLADKA_DO_PRZEGLADU',
                                              'TRANSKRYPCJA_GOTOWA','BLAD_ROZPOZNAWANIA','WERYFIKACJA_OPOZNIONA',
                                              'ALERT_KALIBRACJI','ALERT_WERYFIKACJE','ZGLOSZENIE_BLEDU_ZADANIA',
                                              'KLASA_DO_PRZEKAZANIA','ALERT_PULA_DIAGNOSTYCZNA','ALERT_RODO')),
    dane          jsonb NOT NULL,
    przeczytane_at timestamptz,
    created_at    timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_powiadomienie_nieprzeczytane ON powiadomienie (uzytkownik_id, created_at DESC) WHERE przeczytane_at IS NULL;

CREATE TABLE dziennik_audytu (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    uzytkownik_id uuid,                               -- bez FK: wpis przeżywa usunięcie konta; NULL = system
    rola          rola_uzytkownika,                   -- NULL = działanie systemowe
    akcja         text NOT NULL,                      -- np. 'WERYFIKACJA_ROZSTRZYGNIETA', 'POZIOM_TRUDNOSCI_SKALIBROWANY'
    zasob         text NOT NULL,
    zasob_id      text NOT NULL,
    dane          jsonb NOT NULL DEFAULT '{}',
    ip            inet,
    created_at    timestamptz NOT NULL DEFAULT now()
);
```

### 7.4 Dane referencyjne: Działy (A-04)

| id | kod | nazwa | tylko_rozszerzony |
|---|---|---|---|
| 1 | I | Liczby rzeczywiste | nie |
| 2 | II | Wyrażenia algebraiczne | nie |
| 3 | III | Równania i nierówności | nie |
| 4 | IV | Układy równań | nie |
| 5 | V | Funkcje | nie |
| 6 | VI | Ciągi | nie |
| 7 | VII | Trygonometria | nie |
| 8 | VIII | Planimetria | nie |
| 9 | IX | Geometria analityczna na płaszczyźnie kartezjańskiej | nie |
| 10 | X | Stereometria | nie |
| 11 | XI | Kombinatoryka | nie |
| 12 | XII | Rachunek prawdopodobieństwa i statystyka | nie |
| 13 | XIII | Optymalizacja i rachunek różniczkowy | tak |

Umiejętności są ładowane migracją danych z pliku
`src/lemat/tresci/dane/umiejetnosci.yaml` (jeden wpis = jeden punkt wymagań
szczegółowych podstawy programowej; kod `P.V.9` / `R.XIII.2`). Wagi
egzaminacyjne = udział punktów danej Umiejętności w Arkuszach CKE 2023–2026
(wyliczane skryptem przy imporcie, normalizowane w obrębie poziomu matury).

### 7.5 Schematy pól JSONB

#### 7.5.1 `zadanie_wersja.opcje_zamkniete`

```json
{
  "forma": "WYBOR_JEDNOKROTNY",
  "opcje": [ {"id": "A", "tresc_md": "$x = 2$"}, {"id": "B", "tresc_md": "$x = -2$"},
             {"id": "C", "tresc_md": "$x = 4$"}, {"id": "D", "tresc_md": "$x = 0$"} ],
  "poprawna": "B"
}
```
```json
{
  "forma": "PRAWDA_FALSZ",
  "stwierdzenia": [ {"id": "1", "tresc_md": "Funkcja jest rosnąca.", "poprawna": true},
                    {"id": "2", "tresc_md": "Miejscem zerowym jest $3$.", "poprawna": false} ]
}
```
Punktacja: `WYBOR_JEDNOKROTNY` — `punkty_max = 1`; `PRAWDA_FALSZ` —
`punkty_max = 1`, punkt tylko gdy wszystkie stwierdzenia ocenione poprawnie
(zasada CKE).

#### 7.5.2 `zadanie_wersja.rozwiazanie_wzorcowe`

```json
{
  "warianty": [
    { "wariant": 1, "opis": "Wyznaczenie wierzchołka paraboli",
      "kroki": [ {"nr": 1, "latex": "f(x) = -2x^2 + 8x - 3", "komentarz": "postać ogólna"},
                 {"nr": 2, "latex": "p = -\\frac{b}{2a} = 2", "komentarz": "odcięta wierzchołka"},
                 {"nr": 3, "latex": "f(2) = 5", "komentarz": "wartość największa"} ] }
  ]
}
```
Każdy `wariant` ma odpowiadający mu zbiór wierszy `kryterium_oceniania` o tym
samym numerze wariantu. `kroki[].nr` to numer kroku Rozwiązania wzorcowego
(nie kryterium).

#### 7.5.3 `zadanie_wersja.wynik_koncowy_wzorcowy`

```json
{ "typ": "ZBIOR", "latex": "x \\in \\{-1, 3\\}", "sympy": "FiniteSet(-1, 3)" }
```
`typ` ∈ `WYRAZENIE` | `ROWNANIE` | `ZBIOR` | `PRZEDZIAL` | `WARTOSC_LOGICZNA` |
`TEKST`; `sympy` jest wymagane dla wszystkich typów poza `TEKST`. Dla
`OTWARTE_Z_DOWODEM` i `ZAMKNIETE` pole ma wartość `NULL`.

#### 7.5.4 `weryfikacja.zgloszenie`

```json
{ "kroki": [2], "opis": "W kroku 2 napisałem 5, a nie 6.", "zgloszono_at": "2026-11-02T17:40:00Z",
  "ocena_zglaszana_id": "f1a2…" }
```
`kroki` — 1–5 numerów kroków zatwierdzonej Transkrypcji; `opis` ≤ 500 znaków
(opcjonalny).

---

## 8. Maszyny stanów

### 8.1 Rozwiązanie

```mermaid
stateDiagram-v2
    [*] --> PRZESLANE: POST /rozwiazania
    PRZESLANE --> ROZPOZNAWANIE: worker pobiera ROZPOZNAJ_PISMO
    ROZPOZNAWANIE --> TRANSKRYPCJA_GOTOWA: Transkrypcja zapisana
    ROZPOZNAWANIE --> BLAD_ROZPOZNAWANIA: NIECZYTELNE, ZBYT_WIELE_KROKOW lub wyczerpane ponowienia pracy
    BLAD_ROZPOZNAWANIA --> PRZESLANE: nowe zdjęcia (liczba_przeslan < 3)
    BLAD_ROZPOZNAWANIA --> PRZESLANE: ponowienie rozpoznania tych samych zdjęć (błąd dostawcy, ≤ 5)
    TRANSKRYPCJA_GOTOWA --> TRANSKRYPCJA_GOTOWA: Poprawka Transkrypcji w limitach ADR-036 (nowa wersja)
    TRANSKRYPCJA_GOTOWA --> ZATWIERDZONE: zatwierdzenie Ucznia
    ZATWIERDZONE --> OCENIANIE: worker pobiera OCEN_ROZWIAZANIE
    OCENIANIE --> OCENIONE: Ocena OSTATECZNA (automatyczna)
    OCENIANIE --> OCZEKUJE_NA_WERYFIKACJE: Ocena WSTEPNA lub T-6 (BLAD_PRZETWARZANIA)
    OCZEKUJE_NA_WERYFIKACJE --> OCZEKUJE_NA_WERYFIKACJE: PONOW_OCENE dołącza Ocenę WSTEPNA
    OCZEKUJE_NA_WERYFIKACJE --> OCENIONE: rozstrzygnięcie Weryfikacji lub PONOW_OCENE z trasą automatyczną
    OCENIONE --> OCENIONE: korekta, Kontrola losowa, zgłoszenie błędu odczytu
    PRZESLANE --> PORZUCONE: 7 dni bez postępu
    TRANSKRYPCJA_GOTOWA --> PORZUCONE: 7 dni bez zatwierdzenia lub POST porzucenie
    BLAD_ROZPOZNAWANIA --> PORZUCONE: 7 dni bez akcji lub POST porzucenie
    PRZESLANE --> ANULOWANE: USUN_KONTO po okresie karencji (krok 0)
    ROZPOZNAWANIE --> ANULOWANE: USUN_KONTO po okresie karencji (krok 0)
    BLAD_ROZPOZNAWANIA --> ANULOWANE: USUN_KONTO po okresie karencji (krok 0)
    TRANSKRYPCJA_GOTOWA --> ANULOWANE: USUN_KONTO po okresie karencji (krok 0)
    ZATWIERDZONE --> ANULOWANE: USUN_KONTO po okresie karencji (krok 0)
    OCENIANIE --> ANULOWANE: USUN_KONTO po okresie karencji (krok 0)
    OCZEKUJE_NA_WERYFIKACJE --> ANULOWANE: USUN_KONTO po okresie karencji (krok 0)
    OCENIONE --> [*]
    PORZUCONE --> [*]
    ANULOWANE --> [*]
```

**Okres karencji (ADR-040, C2-08):** żądanie usunięcia konta **nie zmienia**
stanów Rozwiązań ani Weryfikacji. Przez 30 dni konto ma status `USUWANE`, a
procesy są **wstrzymane**: handlery prac w tle kończą się bez efektu
(warunek „konto `AKTYWNE`”, rezultat `KONTO_USUWANE`), zapytania Kolejki
weryfikacji pomijają Weryfikacje Uczniów `USUWANE`, `PORZUC_NIEZATWIERDZONE`
i `WYGAS_ELEMENTY` ich nie dotykają. Przywrócenie konta wznawia procesy
(16.4, C-33). Przejścia do `ANULOWANE` wykonuje dopiero `USUN_KONTO` w
kroku 0 (pierwszy zapis w bazie, 16.4), aby worker, który pobrał pracę przed
usunięciem, zastał stan terminalny; Rozwiązanie `OCENIONE` z otwartą
Weryfikacją pozostaje `OCENIONE`, a Weryfikacja przechodzi do `ANULOWANA`
(`USUNIECIE_KONTA`). Zdjęcia Rozwiązań `PORZUCONE` dostają `usun_po :=
now()` w tej samej transakcji (16.4); zdjęcia Rozwiązań `ANULOWANE` usuwa
krok 4 `USUN_KONTO`.

Dozwolone operacje API w stanach (wszystkie inne → `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN`):

| Operacja | Dozwolona w stanach | Zasób `If-Match` |
|---|---|---|
| `PUT /rozwiazania/{id}/transkrypcja` | `TRANSKRYPCJA_GOTOWA` (limity Poprawek ADR-036, 12.5) | Rozwiązanie |
| `POST /rozwiazania/{id}/zatwierdzenie` | `TRANSKRYPCJA_GOTOWA` | Rozwiązanie |
| `POST /rozwiazania/{id}/zdjecia` | `BLAD_ROZPOZNAWANIA` (gdy `liczba_przeslan < 3`) | Rozwiązanie |
| `POST /rozwiazania/{id}/ponowienie-rozpoznania` | `BLAD_ROZPOZNAWANIA` z kodem `BLAD_DOSTAWCY` lub `PRZEKROCZONY_CZAS` (gdy `liczba_ponowien_rozpoznania < 5`) | Rozwiązanie |
| `POST /rozwiazania/{id}/porzucenie` | `TRANSKRYPCJA_GOTOWA`, `BLAD_ROZPOZNAWANIA` | Rozwiązanie |
| `POST /rozwiazania/{id}/zgloszenie-bledu-odczytu` | `OCENIONE`, `OCZEKUJE_NA_WERYFIKACJE` (warunki 11.6) | Rozwiązanie |
| `POST /rozwiazania/{id}/korekty` | `OCENIONE` (warunki 9.6.3) | Rozwiązanie |
| `POST /weryfikacje/{id}/rozstrzygniecie` | Rozwiązanie w `OCZEKUJE_NA_WERYFIKACJE` lub `OCENIONE` (Kontrola losowa, zgłoszenie) | Weryfikacja (wersja treści, ADR-037) |

### 8.2 Ocena

```mermaid
stateDiagram-v2
    [*] --> WSTEPNA: Agent oceniający, trasa = Weryfikacja
    [*] --> OSTATECZNA: Agent oceniający, trasa = automatyczna
    [*] --> OSTATECZNA: Nauczyciel (rozstrzygnięcie lub korekta)
    WSTEPNA --> ZASTAPIONA: Ocena Nauczyciela zapisana
    OSTATECZNA --> ZASTAPIONA: korekta lub rozstrzygnięcie Kontroli losowej albo zgłoszenia
    ZASTAPIONA --> [*]
```

Jeśli Nauczyciel w Kontroli losowej **potwierdza** Ocenę bez zmian, i tak
powstaje nowa wersja Oceny (`zrodlo = NAUCZYCIEL`), aby każda Próbka
kalibracyjna wskazywała Ocenę Nauczyciela; Uczeń nie dostaje powiadomienia,
gdy punkty i błędny krok są identyczne. Poza przejściem do `ZASTAPIONA`
jedyną dozwoloną modyfikacją wiersza `ocena` jest jednokrotne uzupełnienie
Wskazówki (Z-5, trigger `trg_ocena_niezmienna`).

Widoczność dla Ucznia (`OcenaWyjscie.widocznosc_dla_ucznia`, ADR-022):
`PELNA` — wszystkie pola; `WSTRZYMANA_BLAD_ODCZYTU` — Ocena `WSTEPNA` z
`mozliwy_blad_odczytu = true`: Uczeń widzi status, komunikat i numer kroku
podejrzanego o błąd odczytu, ale nie widzi punktów, typu błędu ani
Wskazówki do czasu Weryfikacji.

### 8.3 Weryfikacja

```mermaid
stateDiagram-v2
    [*] --> OCZEKUJE: Agent oceniający, T-6 lub zgłoszenie Ucznia
    OCZEKUJE --> OCZEKUJE: eskalacja do GLOBALNA (bez zmiany version), PONOW_OCENE, dopisanie zgłoszenia (version + 1)
    OCZEKUJE --> W_TRAKCIE: POST podjecie (blokada 15 min, bez zmiany version)
    W_TRAKCIE --> W_TRAKCIE: POST przedluzenie lub ponowne podjęcie przez właściciela (łącznie ≤ 2 h, bez zmiany version)
    W_TRAKCIE --> W_TRAKCIE: dopisanie zgłoszenia błędu odczytu (version + 1)
    W_TRAKCIE --> OCZEKUJE: POST zwolnienie lub ZWOLNIJ_BLOKADY po wygaśnięciu
    W_TRAKCIE --> ZAKONCZONA: POST rozstrzygniecie lub korekta przez właściciela blokady
    OCZEKUJE --> ZAKONCZONA: korekta Nauczyciela z poziomu Rozwiązania (9.6.3)
    OCZEKUJE --> ANULOWANA: USUN_KONTO po okresie karencji lub PONOW_OCENE z trasą automatyczną
    W_TRAKCIE --> ANULOWANA: USUN_KONTO po okresie karencji
    ZAKONCZONA --> [*]
    ANULOWANA --> [*]
```

Wykonawcy przejść czasowych: `W_TRAKCIE → OCZEKUJE` po wygaśnięciu blokady
wykonuje scheduler co 1 min (`ZWOLNIJ_BLOKADY_WERYFIKACJI`, 13.5);
dodatkowo każde zapytanie kolejki traktuje `W_TRAKCIE` z
`zablokowana_do < now()` jak `OCZEKUJE` (reguła odczytu, 11.4.2), więc
opóźnienie schedulera nie ukrywa Weryfikacji. Eskalację wykonuje
`ESKALUJ_WERYFIKACJE` co godzinę (11.4.3).

**Wersja treści (ADR-037, C2-02):** `weryfikacja.version` (ETag `W`) zmienia
się wyłącznie przy zmianie treści, którą Nauczyciel musi zobaczyć przed
rozstrzygnięciem: dopisanie powodu lub zgłoszenia błędu odczytu, dołączenie
Oceny weryfikowanej przez `PONOW_OCENE`, zamknięcie lub anulowanie.
Podjęcie, ponowne podjęcie, przedłużenie, zwolnienie, `ZWOLNIJ_BLOKADY` i
eskalacja wersji nie zmieniają (trigger `trg_weryfikacja_wersja`, 7.3).
Dlatego ETag otrzymany przy podjęciu pozostaje ważny przez cały czas pracy
Nauczyciela (C-31), a `412 KONFLIKT_WERSJI` przy rozstrzygnięciu zawsze
oznacza zmianę treści; ciało `412` zawiera aktualny zasób i pole
`zmiany_tresci` (lista: `POWODY`, `ZGLOSZENIE`, `OCENA_WERYFIKOWANA`,
`ZAMKNIETA`), które PWA wyróżnia przed ponownym wysłaniem.

### 8.4 Zadanie

```mermaid
stateDiagram-v2
    [*] --> WALIDACJA: Agent generujący (Zadanie wygenerowane)
    [*] --> OPUBLIKOWANE: import Arkusza CKE (GLOBALNY) lub POST /zadania po W-1, W-2, W-8 (PRYWATNY)
    WALIDACJA --> ODRZUCONE_W_WALIDACJI: walidacja negatywna
    WALIDACJA --> OPUBLIKOWANE: walidacja pozytywna, zlecenie Administratora lub systemu, rodzaj inny niż z dowodem (GLOBALNY)
    WALIDACJA --> DO_PRZEGLADU: walidacja pozytywna, zlecenie Nauczyciela (każdy rodzaj) lub OTWARTE_Z_DOWODEM
    DO_PRZEGLADU --> OPUBLIKOWANE: Przegląd ZATWIERDZONE lub POPRAWIONE (zlecający → PRYWATNY, NR/A → GLOBALNY)
    DO_PRZEGLADU --> ODRZUCONE: Przegląd ODRZUCONE
    OPUBLIKOWANE --> OPUBLIKOWANE: Przegląd POPRAWIONE (nowa wersja), ZATWIERDZONE (PRYWATNY → GLOBALNY), rozpatrzenie Zgłoszenia błędu w Zadaniu
    OPUBLIKOWANE --> WYCOFANE: POST wycofanie
    ODRZUCONE_W_WALIDACJI --> [*]
    ODRZUCONE --> [*]
    WYCOFANE --> [*]
```

Uprawnienia do przejść (ADR-028):

| Przejście | Kto |
|---|---|
| Przegląd Zadania `GLOBALNY` (`POPRAWIONE`), wycofanie Zadania `GLOBALNY`, rozpatrzenie Zgłoszenia błędu w Zadaniu | recenzent treści, Administrator |
| Zgłoszenie błędu w Zadaniu `GLOBALNY` (`POST /zadania/{id}/zgloszenia-bledu`) | każdy Nauczyciel (FR-14, ADR-038) |
| Przegląd `DO_PRZEGLADU` Zadania wygenerowanego na zlecenie Nauczyciela | zlecający Nauczyciel (wynik: `PRYWATNY`, natychmiast do użycia w jego Przypisaniach), recenzent treści lub Administrator (wynik: `GLOBALNY`) |
| Przegląd `DO_PRZEGLADU` dowodu ze zlecenia Administratora | recenzent treści, Administrator (wynik: `GLOBALNY`) |
| Przegląd i wycofanie Zadania `PRYWATNY` | autor (dla Zadań wygenerowanych: zlecający), recenzent treści, Administrator |
| Promocja `PRYWATNY → GLOBALNY` (Przegląd `ZATWIERDZONE`) | recenzent treści, Administrator |

**FR-14 a zasięg (ADR-038, C2-03):** Zadanie wygenerowane na zlecenie
Nauczyciela (`zlecenie_generowania.zlecajacy_id` z rolą `NAUCZYCIEL`) po
pozytywnej Walidacji **zawsze** trafia do `DO_PRZEGLADU`, a zlecający
Nauczyciel może je przejrzeć, poprawić albo odrzucić (FR-14 w pełni).
Zatwierdzone przez niego jest `PRYWATNY` i może trafić do doboru dla
wszystkich dopiero po promocji przez recenzenta treści lub Administratora.
Po 14 dniach bez Przeglądu zlecającego Zadanie jest widoczne także w
kolejce Przeglądu recenzentów (`GET /zadania?status=DO_PRZEGLADU`).
Zadanie `GLOBALNY` (z Arkusza CKE, systemowe, promowane) wpływa na
wszystkich Uczniów, więc Nauczyciel bez flagi `recenzent_tresci` nie
poprawia go bezpośrednio, tylko składa **Zgłoszenie błędu w Zadaniu**:
1 otwarte zgłoszenie na (Zadanie, Nauczyciel), limit 10 zgłoszeń na dobę;
powiadomienie `ZGLOSZENIE_BLEDU_ZADANIA` dla recenzentów i Administratorów;
SLA rozpatrzenia 5 dni roboczych (metryka `zgloszenia_zadan_po_sla`). Przy
**3 otwartych zgłoszeniach od różnych Nauczycieli** Zadanie dostaje wiersz
`wstrzymanie_w_doborze` (nie jest rekomendowane i nie może być dodane
do nowego Zestawu zadań), aż recenzent rozpatrzy zgłoszenia: `UWZGLEDNIONE`
wymaga Przeglądu (`POPRAWIONE` albo wycofanie), `ODRZUCONE` wymaga
komentarza; po rozpatrzeniu wszystkich otwartych zgłoszeń wstrzymanie jest
usuwane — `DELETE FROM wstrzymanie_w_doborze` w transakcji rozpatrzenia
ostatniego otwartego zgłoszenia (o ile Zadanie nie zostało wycofane). Ani
wstrzymanie, ani jego usunięcie nie zmienia `zadanie.version` (7.2, C3-09). Zgłaszający Nauczyciel
może do czasu rozpatrzenia usunąć Zadanie ze swoich Zestawów. Zawężenie
prawa bezpośredniej poprawki Zadań `GLOBALNY` do recenzentów jest
decyzją D-11 (23.4, G-0).

Wycofanie Zadania nie przerywa trwających Rozwiązań — są one oceniane według
swojej `zadanie_wersja` (ADR-012); Zadanie znika z doboru rekomendacji i z
otwartych Przypisań (pozycja oznaczona w UI jako „wycofane”). Elementy
`PROPONOWANY` z tym Zadaniem przechodzą do `WYGASLY`; elementy `ROZPOCZETY`
pozostają aktywne do `wygasa_at` (Uczeń mógł już pisać na kartce).

### 8.5 Element Ścieżki nauki

```mermaid
stateDiagram-v2
    [*] --> PROPONOWANY: Agent rekomendujący (wygasa_at = +24 h, R-PRZYPISANIE z Zadaniem zamkniętym — najpóźniej termin)
    PROPONOWANY --> ROZPOCZETY: POST rozpoczecie — otwarcie treści (Zadanie +7 dni, Wkładka +24 h)
    PROPONOWANY --> POMINIETY: POST pominiecie
    PROPONOWANY --> WYGASLY: 24 h bez otwarcia lub wycofanie Zadania
    ROZPOCZETY --> UKONCZONY: Wynik zadania zarejestrowany lub POST przeczytanie (Wkładka)
    ROZPOCZETY --> UKONCZONY: Wkładka otwarta ≥ 60 s, a Uczeń prosi o kolejny element
    ROZPOCZETY --> POMINIETY: POST pominiecie lub wygasa_at bez Rozwiązania w toku
    ROZPOCZETY --> POMINIETY: Wkładka otwarta < 60 s, a Uczeń prosi o kolejny element
    ROZPOCZETY --> POMINIETY: Wkładka bez przeczytania i bez kolejnej prośby do wygasa_at (WYGAS_ELEMENTY)
    UKONCZONY --> [*]
    POMINIETY --> [*]
    WYGASLY --> [*]
```

Reguły:

- `POST /elementy-sciezki/{id}/rozpoczecie` wykonuje PWA przy wyświetleniu
  treści Zadania lub Wkładki (idempotentne); zapisuje
  `zadanie_wersja_id` = aktualna wersja Zadania w tej chwili. Od tego
  momentu Rozwiązanie lub Odpowiedź dla tego elementu jest przyjmowana dla
  tej wersji, nawet jeśli w międzyczasie powstała nowsza (C1-20, 12.5).
- Rozwiązanie `PORZUCONE` nie kończy elementu: Uczeń może przesłać nowe
  Rozwiązanie do `wygasa_at`. `WYGAS_ELEMENTY` przenosi element
  `ROZPOCZETY` do `POMINIETY` po `wygasa_at` tylko wtedy, gdy nie istnieje
  Rozwiązanie w stanie nieterminalnym; w przeciwnym razie element kończy się
  wraz z Wynikiem zadania.
- Odpowiedź na Zadanie zamknięte w elemencie `PROPONOWANY` wykonuje
  rozpoczęcie i ukończenie w jednej transakcji.
- **Ukończenie Wkładki (C2-16 p. 6)** wymaga dowodu przeczytania: wywołania
  `POST …/przeczytanie` albo prośby o kolejny element po ≥ 60 s od
  `rozpoczeto_at`. `WYGAS_ELEMENTY` przenosi Wkładkę `ROZPOCZETY` po
  `wygasa_at` do `POMINIETY` (brak dowodu), więc 8.5 i 13.3 stosują tę samą
  regułę, a reguła FR-06 („Wkładka nieprzeczytana w 14 dniach”) może ją
  zaproponować ponownie.
- **Element `R-PRZYPISANIE` (C1-21):** ma `przypisanie_id`; każda próba z
  niego dziedziczy `przypisanie_id` (trigger `trg_kontekst_proby`), więc
  podlega tym samym regułom co próba z listy Przypisań (unikalność,
  ujawnianie klucza od terminu, kolejka Nauczyciela Przypisania, postęp
  Przypisania). Dla Zadania zamkniętego `wygasa_at = least(created_at +
  24 h, termin)`, a po terminie element przechodzi do `WYGASLY`; dla
  Zadania otwartego obowiązuje okno Przypisania „termin + 7 dni” (12.5).

### 8.6 Wynik zadania (wersja 2.1)

```mermaid
stateDiagram-v2
    [*] --> OCZEKUJE_NA_PROFIL: INSERT (Odpowiedź, Ocena ostateczna, Ocena wstępna bez mozliwy_blad_odczytu, Test diagnostyczny)
    OCZEKUJE_NA_PROFIL --> ZASTOSOWANY: AKTUALIZUJ_PROFIL (zastosowany_w_profilu_at)
    OCZEKUJE_NA_PROFIL --> UNIEWAZNIONY: Ocena zastąpiona przed zastosowaniem
    ZASTOSOWANY --> UNIEWAZNIONY: Ocena zastąpiona (powod OCENA_ZASTAPIONA)
    ZASTOSOWANY --> ZAWIESZONY: Zgłoszenie błędu odczytu (powod ZGLOSZENIE_BLEDU_ODCZYTU)
    OCZEKUJE_NA_PROFIL --> ZAWIESZONY: Zgłoszenie błędu odczytu
    ZAWIESZONY --> [*]: rozstrzygnięcie — nowy Wynik z Oceny Nauczyciela
    UNIEWAZNIONY --> [*]: odtworzenie Profilu (uniewaznienie_przetworzone_at)
```

Stany są wyliczane z kolumn (`zastosowany_w_profilu_at`, `uniewazniony_at`,
`powod_uniewaznienia`), a nie przechowywane w osobnej kolumnie. Zarówno
`UNIEWAZNIONY`, jak i `ZAWIESZONY` wywołują odtworzenie Profilu ucznia
(10.1.3) i nie są w nim stosowane. Ocena wstępna z
`mozliwy_blad_odczytu = true` nie tworzy Wyniku zadania wcale (ADR-022).

---

## 9. Przepływy

Oznaczenia uczestników: `PWA` — aplikacja kliencka, `API` — proces api,
`DB` — PostgreSQL, `S3` — magazyn obiektów, `WRK` — worker (Orkiestrator),
`AR*` — agenci, `JEV` — Jev, `LLM` — Model generatywny.

### 9.1 Rejestracja, poziom matury, Test diagnostyczny (FR-01, FR-02, FR-03)

```mermaid
sequenceDiagram
    actor U as Uczeń
    participant PWA
    participant API
    participant DB
    participant WRK
    U->>PWA: e-mail, hasło, imię, nazwisko, oświadczenie 16+
    PWA->>API: POST /auth/rejestracja
    API->>DB: INSERT uzytkownik, uczen, profil_ucznia, sciezka_nauki (1 transakcja)
    API-->>PWA: 201 + access token + cookie refresh
    U->>PWA: poziom matury, termin egzaminu
    PWA->>API: PATCH /uczniowie/ja
    API->>DB: UPDATE uczen, INSERT opanowanie_umiejetnosci (theta=0, DOMYSLNA) dla brakujących Umiejętności poziomu
    PWA->>API: POST /uczniowie/ja/test-diagnostyczny
    API->>DB: dobór z puli (ix_zadanie_diagnostyczne): na Dział 1 Zadanie z poziomem nominalnym 2 i 1 z poziomem 4
    alt pula pokrywa < 50% planowanych pozycji
        API-->>PWA: 409 TEST_NIEDOSTEPNY (UczenWyjscie.test_diagnostyczny = NIEDOSTEPNY), alert ALERT_PULA_DIAGNOSTYCZNA
    else
        API->>DB: INSERT test (ux_test_jeden, pominiete_dzialy), pozycje
        API-->>PWA: 201 TestDiagnostycznyWyjscie (bez kluczy odpowiedzi, z pominiete_dzialy)
    end
    loop każda pozycja
        U->>PWA: zaznacza Odpowiedź
        PWA->>API: POST /testy-diagnostyczne/{id}/pozycje/{pid}/odpowiedz
        API->>DB: INSERT odpowiedz (porównanie z kluczem, rezultat_ujawniony_od = NULL)
    end
    PWA->>API: POST /testy-diagnostyczne/{id}/zakonczenie
    API->>DB: BEGIN, status ZAKONCZONY, zakonczono_at = now()
    API->>DB: UPDATE odpowiedz SET rezultat_ujawniony_od = zakonczono_at (pozycje testu)
    API->>DB: INSERT wynik_zadania per Odpowiedź (test_diagnostyczny_id, created_at = zakonczono_at)
    API->>DB: INSERT praca_w_tle AKTUALIZUJ_PROFIL (dedup uczen_id), COMMIT
    API-->>PWA: 202 TestDiagnostycznyWyjscie
    WRK->>DB: AKTUALIZUJ_PROFIL: FOR UPDATE profil_ucznia, K=0.8, propagacja w Dziale (10.1.3)
    loop co 1 s, maks. 15 s
        PWA->>API: GET /uczniowie/ja/profil
        API-->>PWA: ProfilUczniaWyjscie (oczekujace_wyniki)
    end
```

Test diagnostyczny aktualizuje Profil ucznia wyłącznie przez Wyniki zadania i
pracę `AKTUALIZUJ_PROFIL` (ta sama blokada `FOR UPDATE` co pozostałe
aktualizacje, C-11). Uczeń może pominąć test (`POST …/pominiecie`): status
`POMINIETY`, Umiejętności zostają z `theta = 0` i `inicjalizacja = DOMYSLNA`;
Odpowiedzi udzielone przed pominięciem dostają `rezultat_ujawniony_od =
now()` i **nie** tworzą Wyników zadania. Możliwość pominięcia testu jest
rozszerzeniem FR-03 („nowy uczeń rozwiązuje…”) — decyzja D-14 (23.4, G-0);
opcja domyślna: pominięcie dozwolone.

**Pula i dobór (C2-13):** dobór używa **nominalnego** Poziomu trudności
(`zadanie.poziom_trudnosci_nominalny`), bo pula jest kuratorowana przez
Administratora (`PUT /admin/zadania/{id}/diagnostyczne`) i nie może zmieniać
składu wraz z kalibracją; aktualizacja Profilu po teście używa bieżącego
`kalibracja_trudnosci.trudnosc_b` (10.1.3). Planowane pozycje: Działy
poziomu matury Ucznia (12 dla `PODSTAWOWY`, 13 dla `ROZSZERZONY`) × 2
Poziomy trudności (2 i 4) = 24 / 26 pozycji; z ≥ 3 kandydatów losowany jest
jeden (różnorodność między Uczniami). Brak kandydata na poziomie 2 (4) →
kandydat z poziomem nominalnym 1 lub 3 (3 lub 5); brak i takiego → Dział
trafia do `pominiete_dzialy`, a jego Umiejętności pozostają `DOMYSLNA`
(propagacja 10.1.3 działa tylko w obrębie przetestowanego Działu). Gdy dobrane pozycje stanowią < 50%
planowanych, test nie powstaje (`409 TEST_NIEDOSTEPNY`), Uczeń zaczyna z
Profilem `DOMYSLNA`, a Administratorzy dostają `ALERT_PULA_DIAGNOSTYCZNA`;
test można wykonać później, gdy pula zostanie uzupełniona. Bramka G-5
wymaga ≥ 3 Zadań diagnostycznych na każdą trójkę (Dział, poziom matury,
nominalny Poziom trudności ∈ {2, 4}); `SPRAWDZ_PULE_DIAGNOSTYCZNA` (13.3)
sprawdza to codziennie, a `PUT …/diagnostyczne` z `diagnostyczne = false`,
które obniżyłoby pulę poniżej 3, kończy się `409
PULA_DIAGNOSTYCZNA_ZA_MALA` (chyba że `?wymus=true` — Zadanie z błędem —
wtedy alert).
Pominięty test można później wykonać **raz** (indeks `ux_test_jeden`
obejmuje `W_TOKU` i `ZAKONCZONY`); ponowna próba → `409 TEST_JUZ_WYKONANY`.
Ponieważ Wyniki zadania z testu są zdarzeniami w historii (z datą
zakończenia testu), Wyniki sprzed testu i po nim stosują się w kolejności
chronologicznej, bez podwójnego liczenia (10.1.3).

**Zmiana poziomu matury** (`PATCH /uczniowie/ja` z nowym `poziom_matury`):
w jednej transakcji: (1) wstawiane są brakujące wiersze
`opanowanie_umiejetnosci` dla Umiejętności nowego poziomu (`DOMYSLNA`);
istniejące `theta` są zachowane (Umiejętność jest wspólna dla obu
poziomów); wiersze Umiejętności `tylko_rozszerzony` po zmianie na
`PODSTAWOWY` zostają, ale nie są pokazywane w Profilu ani rekomendowane;
(2) element `PROPONOWANY` przechodzi do `WYGASLY`; (3) aktywna Seria jest
kończona; (4) Uczeń może wykonać jeden Test diagnostyczny dla nowego
poziomu (indeks per poziom). Nie powstają nowe Wyniki zadania, więc nie ma
odtwarzania Profilu.

### 9.2 Rekomendacja kolejnego elementu Ścieżki nauki (FR-05 … FR-10)

```mermaid
sequenceDiagram
    actor U as Uczeń
    participant PWA
    participant API
    participant ORK as Orkiestrator (fasada w api)
    participant REK as Agent rekomendujący
    participant DB
    participant JEV
    U->>PWA: „Dalej”
    PWA->>API: GET /uczniowie/ja/rekomendacje/nastepna
    API->>DB: domknij Wkładkę ROZPOCZETY (≥ 60 s → UKONCZONY, inaczej POMINIETY)
    API->>DB: SELECT element_sciezki WHERE status = PROPONOWANY AND wygasa_at > now()
    alt istnieje
        API-->>PWA: 200 RekomendacjaWyjscie z istniejącym elementem
    else brak
        API->>ORK: rekomenduj_synchronicznie(uczen_id)
        ORK->>DB: migawka Profilu ucznia, historia, Przypisania, Seria
        ORK->>REK: rekomenduj(WejscieRekomendacji)
        opt grupa eksperymentu JEV_CHOICE i brak aktywnej Serii
            REK->>JEV: CHOICE Q-RE-01 (timeout 1,5 s, bez ponowień)
            JEV-->>REK: rozkład prawdopodobieństw lub błąd → POLITYKA_ELO
        end
        REK-->>ORK: DecyzjaRekomendacji (typ, cel, Umiejętność, Zadanie lub Wkładka lub brak, reguła, fakty)
        ORK->>DB: BEGIN, INSERT element_sciezki (PROPONOWANY) + rekomendacja + decyzja_modelu
        ORK->>DB: UPDATE sciezka_nauki (Seria), praca_w_tle UZUPELNIJ_PULE lub GENERUJ_SZKIC_WKLADKI (dedup), COMMIT
        alt naruszenie ux_jeden_proponowany (równoległe żądanie)
            ORK->>DB: ROLLBACK, SELECT istniejący PROPONOWANY
        end
        ORK-->>API: element lub brak elementu
        API-->>PWA: 200 RekomendacjaWyjscie (element + uzasadnienie FR-10 albo brak_elementu)
    end
```

### 9.3 Zadanie zamknięte (FR-15, FR-29)

```mermaid
sequenceDiagram
    actor U as Uczeń
    participant PWA
    participant API
    participant DB
    participant WRK
    U->>PWA: zaznacza Odpowiedź
    PWA->>API: POST /zadania/{id}/odpowiedzi (Idempotency-Key, element_sciezki_id albo przypisanie_id)
    API->>DB: BEGIN
    API->>DB: sprawdź kontekst (12.5), element R-PRZYPISANIE → przypisanie_id dziedziczone (trg_kontekst_proby)
    API->>DB: INSERT odpowiedz (ux_odpowiedz_element / ux_odpowiedz_przypisanie, rezultat_ujawniony_od = greatest(now(), termin) przy przypisanie_id, inaczej now())
    API->>DB: INSERT wynik_zadania (waga wg 10.1.3), element_sciezki ROZPOCZETY→UKONCZONY
    API->>DB: INSERT praca_w_tle AKTUALIZUJ_PROFIL (dedup uczen_id), COMMIT
    alt rezultat ujawniony
        API-->>PWA: 201 OdpowiedzWyjscie.rezultat (czy_poprawna, punkty, klucz)
    else próba w kontekście Przypisania (także z elementu R-PRZYPISANIE) przed terminem
        API-->>PWA: 201 rezultat = null, rezultat_ujawniony_od = termin
    end
    WRK->>DB: AKTUALIZUJ_PROFIL: SELECT profil_ucznia FOR UPDATE
    WRK->>DB: aktualizacja theta, atomowa aktualizacja kalibracja_trudnosci, powtórki, Seria
```

### 9.4 Przesłanie Rozwiązania i Transkrypcja (FR-16, FR-17, FR-18)

```mermaid
sequenceDiagram
    actor U as Uczeń
    participant PWA
    participant API
    participant S3
    participant DB
    participant WRK
    participant ROZ as Agent rozpoznający
    participant LLM
    U->>PWA: robi 1–5 zdjęć kartki
    PWA->>PWA: kompresja JPEG q=0.85, dłuższy bok ≤ 2560 px
    PWA->>API: POST /rozwiazania multipart (zadanie_wersja_id, element_sciezki_id lub przypisanie_id, zdjecia[]) + Idempotency-Key
    API->>API: walidacja (typ, rozmiar, limit 30/h, kontekst i wersja 12.5)
    API->>API: piaskownica Pillow api (ADR-045): nagłówek → limit pikseli, potem HEIC→JPEG, obrót EXIF, usunięcie EXIF/GPS
    API->>S3: PUT zdjęcia (klucz z uuid)
    API->>DB: BEGIN, INSERT rozwiazanie PRZESLANE (przeslano_at), zdjecie_rozwiazania, praca_w_tle ROZPOZNAJ_PISMO (pula R), COMMIT
    API-->>PWA: 202 {id, status: PRZESLANE, nastepne_odpytanie_ms: 1000}
    WRK->>DB: pobiera ROZPOZNAJ_PISMO, status := ROZPOZNAWANIE
    WRK->>ROZ: rozpoznaj(zdjęcia, treść Zadania), termin = początek próby + 14 s
    ROZ->>S3: GET zdjęcia
    ROZ->>LLM: obrazy + prompt (schemat JSON 10.3.2)
    LLM-->>ROZ: kroki, LaTeX, pewność odczytu, niepewne fragmenty, obszary danych osobowych
    ROZ->>ROZ: post-processing (parsowanie SymPy, oznaczanie niepewnych), Filtr danych osobowych (ADR-042)
    WRK->>WRK: maskowanie obszarów danych osobowych (Pillow) → zdjęcia maskowane
    WRK->>S3: PUT zdjecia-maskowane/…
    WRK->>DB: INSERT transkrypcja v1 + kroki, transkrypcja_gotowa_at, status := TRANSKRYPCJA_GOTOWA, powiadomienie
    loop odpytywanie co 1 s w stanach PRZESLANE/ROZPOZNAWANIE
        PWA->>API: GET /rozwiazania/{id}
    end
    U->>PWA: poprawia błędnie odczytane kroki (edycja istniejących, wstawienie ≤ 2)
    PWA->>API: PUT /rozwiazania/{id}/transkrypcja (If-Match: version Rozwiązania) — autozapis 2 s po ostatniej edycji i przy opuszczeniu pola (C3-05)
    API->>API: limity twarde ADR-036 (bez usuwania kroków, zakres poprawek ze wstawionymi, długość kroku wstawionego), Filtr danych osobowych (dokładne trafienia)
    alt limit twardy przekroczony
        API-->>PWA: 422 POPRAWKI_ZBYT_ROZLEGLE (UI: „Wyślij nowe zdjęcia”)
    else
        API->>DB: INSERT transkrypcja vN (POPRAWKA_UCZNIA, zakres_poprawek), kroki (nr_kroku_agenta, odleglosc_edycyjna)
        API->>DB: praca_w_tle SPRAWDZ_POPRAWKI_TRANSKRYPCJI (pula S, klucz rozwiazanie_id, oczekująca praca dostaje nowy transkrypcja_id), COMMIT
        API-->>PWA: 200 RozwiazanieWyjscie + nowy ETag
    end
    WRK->>LLM: wycinki zmienionych i wstawionych kroków + zdjęcie poglądowe 1024 px + pary (odczyt agenta, Poprawka) (rola ROZPOZNAWANIE_PISMA, prompt sprawdzenie-poprawek-v2)
    WRK->>DB: UPDATE transkrypcja SET zgodnosc_poprawek (tylko gdy wersja nadal najnowsza)
    U->>PWA: „Zatwierdzam i oceniam”
    PWA->>API: (gdy jest niezapisana edycja) PUT …/transkrypcja natychmiast, potem POST /rozwiazania/{id}/zatwierdzenie (If-Match z ostatniego PUT)
    API->>DB: status := ZATWIERDZONE, zatwierdzona_transkrypcja_id, zatwierdzono_at, praca_w_tle OCEN_ROZWIAZANIE (pula O)
    API-->>PWA: 202
```

**Poprawki Transkrypcji (ADR-036, C2-01).** Uczeń poprawia **fragmenty
odczytu**, a nie pisze rozwiązania od nowa:

| Limit | Wartość | Skutek przekroczenia |
|---|---|---|
| usunięcie kroku Transkrypcji agenta | zabronione (każdy krok agenta występuje dokładnie raz, w tej samej kolejności) | `422 POPRAWKI_ZBYT_ROZLEGLE` |
| kroki wstawione (`nr_kroku_agenta = NULL`) | ≤ 2 | `422` |
| długość kroku wstawionego (`tekst` + `latex` po normalizacji LaTeX) — limit twardy (C3-05) | ≤ min(200, max(60, ⌈1,5 × mediana długości kroków agenta⌉)) znaków | `422` |
| kroki zmienione + wstawione — limit twardy | ≤ max(3, ⌈0,5 × n⌉), `n` = liczba kroków agenta | `422` |
| odsetek zmienionych znaków = (Σ po krokach zmienionych Levenshtein(odczyt, poprawka) + Σ po krokach wstawionych długość kroku) / Σ długość **wszystkich** kroków agenta bieżącego przesłania (`tekst` + `latex` po normalizacji LaTeX; od wersji 2.3 kroki wstawione są w liczniku, C3-05) — limit twardy | ≤ 0,50 | `422` |
| kroki zmienione + wstawione — limit miękki | ≤ max(2, ⌈0,25 × n⌉) | powód `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM` (T-7) |
| odsetek zmienionych znaków — limit miękki | ≤ 0,25 | T-7 |
| Sprawdzenie zgodności poprawek (10.3.3) | każdy zmieniony krok z `p_zgodna_z_poprawka ≥ 0,50`; rezultat dostępny przed zapisem Oceny | T-7 |

Przykład (C3-05): Transkrypcja agenta ma 4 kroki o łącznej długości 120
znaków (mediana 30). Krok wstawiony może mieć ≤ min(200, max(60, 45)) = 60
znaków; dwa wstawione kroki po 60 znaków dają odsetek (0 + 120) / 120 =
1,00 > 0,50 → `422`. Wstawienie kompletnego rozwiązania (2 × 2000 znaków,
możliwe w wersji 2.2) jest odrzucane już limitem długości. Jeden wstawiony
krok 25 znaków (pominięta linia) daje 25 / 120 = 0,21 — mieści się w limicie
miękkim 0,25.

Zakres poprawek jest zawsze liczony względem Transkrypcji agenta
bieżącego przesłania (kolejne wersje `POPRAWKA_UCZNIA` nie „przesuwają”
punktu odniesienia). Przy `422` UI proponuje „Wyślij nowe zdjęcia” (nowe
przesłanie, ≤ 3) — jedyną drogę zmiany rozwiązania w większym zakresie.
`OCEN_ROZWIAZANIE` czeka na rezultat sprawdzenia najwyżej do
`zatwierdzono_at + 6 s` (wersja 2.4, C4-06 p. 10 — okno stałe, niezależne
od `t_R`; wersja 2.3 skracała je do `termin_miekki − 9 s`, czyli do 1 s przy
`t_R ≥ 18 s`), równolegle z SymPy i Modelem decyzyjnym; brak rezultatu →
T-7 (Z-3). Okno stałe nie narusza terminu miękkiego: termin miękki ≥ 10 s,
a zapis Oceny po oknie trwa ≤ 0,2 s (6,2 s < 10 s); rezerwa 9 s na model
zapasowy nie jest potrzebna, bo model zapasowy startuje równolegle ze
Sprawdzeniem (po błędzie Jev, najpóźniej w 4,2 s), a nie po nim.

**Autozapis i czas Sprawdzenia (C3-05).** PWA wysyła `PUT …/transkrypcja`
2 s po ostatniej zmianie kroku i przy opuszczeniu pola edycji (debounce), a
przycisk „Zatwierdzam i oceniam” najpierw zapisuje niezapisaną zmianę. Każdy
`PUT` podmienia `transkrypcja_id` w oczekującej pracy
`SPRAWDZ_POPRAWKI_TRANSKRYPCJI` (klucz deduplikacji `rozwiazanie_id`);
praca w toku dla starszej wersji kończy się bez zapisu (`rezultat
przestarzały`), a nowa jest zlecana po niej. Sprawdzenie działa w osobnej
puli `S` (13.4) i wysyła tylko wycinki zmienionych i wstawionych kroków
oraz zdjęcie poglądowe 1024 px (A-08: p95 ≤ 4 s). Okno na rezultat liczone
od ostatniego `PUT`: `G + 6 s`, gdzie `G` = czas od ostatniego `PUT` do
zatwierdzenia. Najgorszy przypadek to `G ≈ 0` (Uczeń zatwierdza od razu po
edycji): przy rozkładzie log-normalnym czasu Sprawdzenia (p50 2,5 s, p95
4,0 s, σ = 0,286) P(czas > 6 s) = P(Z > 3,06) ≈ 0,1%; z timeoutem 8 s i
drugą próbą w puli `S` bez kolejki (17.4) brak rezultatu ≤ 0,5% Rozwiązań
z Poprawkami (przyjęte w 17.7). Rachunek obowiązuje dla każdego `t_R`, bo
okno wynosi zawsze 6 s (C4-06 p. 10); przy oknie z wersji 2.3 (1 s przy
`t_R ≥ 18 s`) wynosiłby P(czas > 1 s) = P(Z > −3,20) ≈ 99,9% dla `G ≈ 0`. Metryki `czas_od_put_do_zatwierdzenia_sekundy`
i `sprawdzenie_poprawek_gotowe_przed_zatwierdzeniem` (odsetek) weryfikują
założenie w pilotażu; alert przy odsetku braku rezultatu > 1% w 24 h.

Błędy rozpoznawania (14.1 E-03 … E-06): Uczeń może przesłać nowe zdjęcia
(łącznie ≤ 3 przesłania), ponowić rozpoznanie tych samych zdjęć po błędzie
dostawcy (≤ 5 razy) albo porzucić Rozwiązanie i zacząć nowe dla tego samego
elementu. Wpis ręczny nie istnieje (ADR-024).

### 9.5 Ocena Rozwiązania (FR-19 … FR-29, NFR-01, NFR-05)

```mermaid
sequenceDiagram
    participant WRK as Orkiestrator (WRK)
    participant DB
    participant OCE as Agent oceniający
    participant SYM as SprawdzanieSymboliczne
    participant MD as ModelDecyzyjny (CB → Jev lub zapasowy)
    participant WYJ as Agent wyjaśniający
    participant LLM
    WRK->>DB: pobiera OCEN_ROZWIAZANIE, ZATWIERDZONE → OCENIANIE
    WRK->>WRK: termin_miekki = zatwierdzono_at + max(10 s, 28 s − t_R), termin_twardy = zatwierdzono_at + 120 s
    WRK->>DB: wczytaj zadanie_wersja, kryteria, transkrypcję, aktywną konfigurację, Próg pewności i kalibrator
    WRK->>OCE: ocen(KontekstOceny)
    OCE->>SYM: równoważność par kroków + wynik końcowy vs wzorcowy (limit 3 s)
    SYM-->>OCE: ROWNOWAZNE / NIEROWNOWAZNE / NIEROZSTRZYGNIETE
    OCE->>OCE: zbuduj PlanPytan (10.4.3) i stan (10.4.2)
    OCE->>MD: wykonaj_plan(stan, plan, termin_miekki)
    par oczekiwanie na Sprawdzenie zgodności poprawek (tylko Transkrypcja POPRAWKA_UCZNIA, ≤ 6 s)
        WRK->>DB: odczyt transkrypcja.zgodnosc_poprawek
    end
    alt Jev (obwód ZAMKNIETY, przydział ogranicznika dostępny)
        MD->>MD: runda 1: Q-OC-00, Q-OC-01, Q-OC-02.* (gdy 1 wariant), Q-OC-03, Q-OC-05.*, Q-OC-06 (wsad: 1 żądanie, pojedynczo: ≤ 2 fale po 16)
        MD->>MD: runda 2: Q-OC-02.* wybranego wariantu (gdy > 1 wariant), Q-OC-04.j dla mody Q-OC-03 (1 żądanie / 1 fala)
    else model zapasowy
        MD->>MD: jedno wywołanie LLM z całym planem (pytania warunkowe zwinięte, 13.2)
    end
    MD-->>OCE: RezultatPytan (decyzje składowe + konfiguracja_modelu_id)
    OCE->>OCE: punkty (11.1), pewność surowa i kalibrowana (11.2), trasa (11.3, w tym T-7), uzasadnienie (11.7)
    OCE-->>WRK: RezultatOceny
    WRK->>DB: BEGIN
    WRK->>DB: UPDATE rozwiazanie WHERE status = OCENIANIE AND version = v (ocena_wystawiona_at, czas_systemowy_ms)
    WRK->>DB: INSERT ocena (wskazowka NULL), ocena_kryterium, decyzja_modelu[] (z uczen_id)
    alt trasa automatyczna
        WRK->>DB: ocena OSTATECZNA, rozwiazanie OCENIONE, wynik_zadania (waga 1.00 lub 0.50 przy powtórzonej próbie), usun_po zdjęć
        alt reguła T-5
            WRK->>DB: INSERT weryfikacja KONTROLA_LOSOWA (powody_poczatkowe = powody, termin_at = dodaj_dni_robocze(now, 3)), usun_po zdjęć = created_at + 90 dni
        else bez Kontroli losowej i zgoda_na_trening
            WRK->>DB: praca_w_tle KOPIUJ_DO_ZBIORU_TRENINGOWEGO
        end
    else trasa Weryfikacja
        WRK->>DB: ocena WSTEPNA, rozwiazanie OCZEKUJE_NA_WERYFIKACJE, INSERT weryfikacja (11.4, powody_poczatkowe = powody)
        opt brak mozliwy_blad_odczytu
            WRK->>DB: wynik_zadania (waga 0.50 lub 0.25)
        end
        WRK->>DB: praca_w_tle OPINIA_POMOCNICZA (gdy NISKA_PEWNOSC)
    end
    opt typ_bledu ≠ NULL i brak mozliwy_blad_odczytu
        WRK->>DB: praca_w_tle NAPISZ_WSKAZOWKE (pula P, prio 1)
    end
    WRK->>DB: praca_w_tle AKTUALIZUJ_PROFIL, element_sciezki UKONCZONY, powiadomienie, COMMIT
    WRK->>WYJ: NAPISZ_WSKAZOWKE: napisz wskazówkę (timeout 8 s)
    WYJ->>LLM: prompt wskazówki
    LLM-->>WYJ: tekst
    WYJ->>WYJ: strażnik wycieku rozwiązania (10.5.3)
    WYJ-->>WRK: Wskazówka lub szablon
    WRK->>DB: UPDATE ocena SET wskazowka WHERE id = :id AND wskazowka IS NULL
```

Ocena jest widoczna dla Ucznia zaraz po pierwszym `COMMIT` (to jest moment
`ocena_wystawiona_at` mierzony w NFR-01). Wskazówka dochodzi asynchronicznie
(cel p95 ≤ 10 s po Ocenie); do tego czasu `OcenaWyjscie.wskazowka_w_przygotowaniu
= true`, a PWA odpytuje co 1 s, maks. 15 s (ADR-025). Gdy termin twardy
minie przed zapisem Oceny, handler wykonuje T-6 (14.2). `t_R` = czas
rozpoznania (`transkrypcja_gotowa_at − przeslano_at`). Termin miękki
`max(10 s, 28 s − t_R)` jest zgodny z SLO serwerowym 28 s (17.1); wersja
2.1 liczyła go od 30 s, przez co scenariusze z ponowieniem Jev mieściły się
w 30 s, ale nie w 28 s (C2-07).

### 9.6 Weryfikacja przez Nauczyciela (FR-33, FR-34)

#### 9.6.1 Kolejka, podjęcie, przedłużenie, szkic

```mermaid
sequenceDiagram
    actor N as Nauczyciel
    participant PWA
    participant API
    participant DB
    PWA->>API: GET /weryfikacje?dostepnosc=DOSTEPNE
    API->>DB: SELECT … WHERE uprawniony AND (status = OCZEKUJE OR (status = W_TRAKCIE AND zablokowana_do < now())) ORDER BY priorytet, termin_at
    API-->>PWA: lista (pseudonim zamiast imienia dla obcego weryfikatora globalnego, 11.4.4)
    N->>PWA: otwiera pozycję
    PWA->>API: POST /weryfikacje/{id}/podjecie
    API->>DB: UPDATE weryfikacja … RETURNING (SQL poniżej)
    alt 0 wierszy
        API-->>PWA: 409 WERYFIKACJA_ZAJETA
    else
        API-->>PWA: 200 WeryfikacjaSzczegoly + ETag (zdjęcia lub zdjęcia maskowane, Transkrypcje, decyzje, Opinia pomocnicza, szkic)
    end
    loop co 20 s przy zmianach
        PWA->>API: PUT /weryfikacje/{id}/szkic
        API->>DB: UPSERT szkic_rozstrzygniecia (weryfikacja_id, nauczyciel_id)
    end
    loop co 5 min, gdy karta jest aktywna
        PWA->>API: POST /weryfikacje/{id}/przedluzenie
        API->>DB: zablokowana_do = least(now() + 15 min, podjeta_at + 2 h) — version bez zmian (ADR-037)
        API-->>PWA: 200 {zablokowana_do, version} + ETag (ten sam, o ile nie zmieniła się treść)
    end
    N->>PWA: zatwierdza rozstrzygnięcie (po np. 40 min, 8 przedłużeniach)
    PWA->>API: POST /weryfikacje/{id}/rozstrzygniecie (If-Match = ETag z podjęcia lub ostatniego przedłużenia)
    API-->>PWA: 200 (412 wyłącznie po zmianie treści, z polem zmiany_tresci)
```

```sql
-- Podjęcie (także idempotentne ponowne podjęcie przez właściciela ważnej blokady);
-- version NIE jest zmieniana (trg_weryfikacja_wersja, ADR-037)
UPDATE weryfikacja w
SET status = 'W_TRAKCIE',
    podjeta_przez = :ja,
    podjeta_at = CASE WHEN w.status = 'W_TRAKCIE' AND w.podjeta_przez = :ja AND w.zablokowana_do >= now()
                      THEN w.podjeta_at ELSE now() END,
    zablokowana_do = CASE WHEN w.status = 'W_TRAKCIE' AND w.podjeta_przez = :ja AND w.zablokowana_do >= now()
                          THEN least(now() + interval '15 minutes', w.podjeta_at + interval '2 hours')
                          ELSE now() + interval '15 minutes' END
WHERE w.id = :id
  AND (w.status = 'OCZEKUJE'
       OR (w.status = 'W_TRAKCIE' AND w.zablokowana_do < now())
       OR (w.status = 'W_TRAKCIE' AND w.podjeta_przez = :ja
           AND w.podjeta_at + interval '2 hours' > now()))                 -- limit 2 h także przy ponownym podjęciu (C2-16)
  AND (w.nauczyciel_docelowy_id = :ja
       OR (w.typ_kolejki = 'GLOBALNA' AND :ja_jest_weryfikatorem_globalnym)
       OR w.klasa_id IN (SELECT id FROM klasa WHERE nauczyciel_id = :ja))
  AND EXISTS (SELECT 1 FROM rozwiazanie r JOIN uzytkownik u ON u.id = r.uczen_id
              WHERE r.id = w.rozwiazanie_id AND u.status = 'AKTYWNE')     -- okres karencji (ADR-040)
RETURNING *;
-- 0 wierszy: inny właściciel ważnej blokady → 409 WERYFIKACJA_ZAJETA.
-- Ponowne podjęcie przez właściciela nie wydłuża blokady poza podjeta_at + 2 h (least), więc po 2 h blokada
-- wygasa, `ZWOLNIJ_BLOKADY` zwraca Weryfikację do kolejki w ≤ 1 min, a kolejne podjęcie (przez dowolnego
-- uprawnionego, także dawnego właściciela) zaczyna nową sesję z nowym podjeta_at; szkic jest zachowany.

-- Przedłużenie (tylko właściciel; łączny czas blokady ≤ 2 h od podjeta_at); version bez zmian
UPDATE weryfikacja
SET zablokowana_do = least(now() + interval '15 minutes', podjeta_at + interval '2 hours')
WHERE id = :id AND status = 'W_TRAKCIE' AND podjeta_przez = :ja
  AND zablokowana_do >= now() AND podjeta_at + interval '2 hours' > now()
RETURNING zablokowana_do, version;
-- 0 wierszy: blokada wygasła/przejęta → 409 WERYFIKACJA_NIE_PODJETA; limit 2 h → 409 BLOKADA_MAKS_CZAS
```

Szkic jest przypisany do pary (Weryfikacja, Nauczyciel), więc przejęcie
Weryfikacji przez innego Nauczyciela nie usuwa pracy pierwszego: gdy pierwszy
podejmie ją ponownie, odzyskuje swój szkic. Szkic jest usuwany przy
`ZAKONCZONA`/`ANULOWANA`.

#### 9.6.2 Rozstrzygnięcie

```mermaid
sequenceDiagram
    actor N as Nauczyciel
    participant PWA
    participant API
    participant DB
    participant WRK
    N->>PWA: punkty per kryterium, pierwszy błędny krok, typ błędu, Wskazówka (tryb), kroki z błędem odczytu
    PWA->>API: POST /weryfikacje/{id}/rozstrzygniecie (If-Match: version Weryfikacji)
    API->>DB: BEGIN, SELECT rozwiazanie FOR UPDATE, SELECT weryfikacja FOR UPDATE
    API->>DB: sprawdź W_TRAKCIE, podjeta_przez = ja, zablokowana_do > now(), version (treści, ADR-037), Ocena weryfikowana ≠ ZASTAPIONA
    API->>DB: SELECT status FROM uzytkownik WHERE id = :uczen_id FOR SHARE — konto Ucznia AKTYWNE (karencja ADR-040, C3-08, C4-06 p. 4)
    alt konto Ucznia USUWANE
        API-->>PWA: 409 KONTO_UCZNIA_NIEAKTYWNE (ROLLBACK, szkic zachowany, blokada wygasa normalnie)
    end
    API->>DB: UPDATE poprzednia ocena → ZASTAPIONA (najpierw, ux_ocena_aktualna)
    API->>DB: INSERT ocena (NAUCZYCIEL, OSTATECZNA, wersja = max + 1), ocena_kryterium
    API->>DB: UPDATE weryfikacja ZAKONCZONA, DELETE szkic, rozwiazanie OCENIONE (version + 1), usun_po zdjęć
    API->>DB: UPDATE wynik_zadania stary: uniewazniony_at, powod OCENA_ZASTAPIONA (zawieszony przez zgłoszenie — bez zmian), INSERT nowy (waga 1.00 lub 0.50)
    API->>DB: INSERT probka_kalibracyjna (reguła 11.5.2 na powody_poczatkowe, ON CONFLICT DO NOTHING), dziennik_audytu
    API->>DB: praca_w_tle AKTUALIZUJ_PROFIL, NAPISZ_WSKAZOWKE (tryb AUTOMATYCZNA), KOPIUJ_DO_ZBIORU_TRENINGOWEGO (gdy brak innej otwartej Weryfikacji i zgoda), powiadomienie Ucznia, COMMIT
    API-->>PWA: 200 OcenaWyjscie
    WRK->>DB: AKTUALIZUJ_PROFIL: odtworzenie theta dla Umiejętności Zadania
```

**Blokada konta Ucznia (wersja 2.4, C4-06 p. 4).** Kolejność blokad w
rozstrzygnięciu i korekcie: `rozwiazanie` (`FOR UPDATE`) → `weryfikacja`
(`FOR UPDATE`) → `uzytkownik` Ucznia (`FOR SHARE`). Transakcja wniosku o
usunięcie konta wykonuje `UPDATE uzytkownik SET status = 'USUWANE' …`, które
wymaga blokady wiersza sprzecznej z `FOR SHARE`, więc obie transakcje są
serializowane: wniosek zatwierdzony przed rozstrzygnięciem — rozstrzygnięcie
odczytuje `USUWANE` i kończy się `409 KONTO_UCZNIA_NIEAKTYWNE`; rozstrzygnięcie,
które pierwsze zablokowało wiersz — wniosek czeka na jego `COMMIT`, więc
Ocena Nauczyciela powstaje przed wnioskiem. Transakcja wniosku Ucznia nie
blokuje `rozwiazanie` ani `weryfikacja`, więc cykl blokad (zakleszczenie)
nie powstaje.

Reguły Wskazówki przy rozstrzygnięciu (FR-27, C1-14): pole
`wskazowka.tryb` ∈ {`WLASNA` (tekst Nauczyciela, ≤ 400 znaków),
`AUTOMATYCZNA` (praca `NAPISZ_WSKAZOWKE` na decyzjach Nauczyciela),
`ZACHOWAJ` (kopiuje Wskazówkę Oceny weryfikowanej)}. `ZACHOWAJ` jest
dozwolone tylko wtedy, gdy `pierwszy_bledny_krok`, `brakujacy_krok` i
`typ_bledu` są identyczne jak w Ocenie weryfikowanej i ta Ocena ma
Wskazówkę; w przeciwnym razie `422 WALIDACJA`. Gdy Ocena Nauczyciela nie ma
błędu, `wskazowka` musi być `null`. Dzięki temu każda Ocena ostateczna z
błędem ma Wskazówkę odnoszącą się do aktualnego błędnego kroku.

#### 9.6.3 Korekta Oceny z poziomu Rozwiązania (FR-34)

`POST /rozwiazania/{id}/korekty` (If-Match: `version` Rozwiązania), stan
`OCENIONE`, konto Ucznia `AKTYWNE` (inaczej `409 KONTO_UCZNIA_NIEAKTYWNE`,
C3-08) — sprawdzane `SELECT status FROM uzytkownik … FOR SHARE` po blokadzie
`rozwiazanie`, jak w 9.6.2 (C4-06 p. 4). W jednej transakcji z `SELECT
rozwiazanie FOR UPDATE`:

1. Jeśli istnieje otwarta Weryfikacja tego Rozwiązania (Kontrola losowa lub
   zgłoszenie błędu odczytu):
   - `W_TRAKCIE` z ważną blokadą innego Nauczyciela → `409
     WERYFIKACJA_ZAJETA` (nic nie zapisano);
   - w pozostałych przypadkach korekta **rozstrzyga** tę Weryfikację:
     `status = ZAKONCZONA`, `ocena_nauczyciela_id` = nowa Ocena; Próbka
     kalibracyjna dostaje źródło wynikające z powodów tej Weryfikacji
     (11.5.2), np. `KONTROLA_LOSOWA`.
2. Bez otwartej Weryfikacji: Próbka kalibracyjna `KOREKTA_NAUCZYCIELA`
   (tylko gdy aktualna Ocena jest automatyczna i nie ma jeszcze próbki —
   `ux_probka_ocena_auto`).
3. Dalej jak w 9.6.2 (zastąpienie Oceny, Wynik zadania, Wskazówka,
   powiadomienie `OCENA_SKORYGOWANA`).

Korekta i rozstrzygnięcie są serializowane blokadą wiersza `rozwiazanie`;
operacja wykonana jako druga dostaje `412 KONFLIKT_WERSJI` (zmieniona
`version`) albo `409 OCENA_ZMIENIONA` (Ocena weryfikowana została zastąpiona)
— scenariusz C-09.

### 9.7 Generowanie i Walidacja Zadania (FR-11 … FR-14)

```mermaid
sequenceDiagram
    participant SRC as Źródło zlecenia (Nauczyciel, Administrator, UZUPELNIJ_PULE)
    participant API
    participant DB
    participant WRK
    participant GEN as Agent generujący
    participant LLM
    participant LLM2 as LLM rozwiązujący niezależnie
    participant SYM as SprawdzanieSymboliczne
    participant MD as ModelDecyzyjny
    SRC->>API: POST /zadania/generowanie {umiejetnosc_id, poziom_trudnosci, poziom_matury, rodzaj}
    API->>DB: INSERT zlecenie_generowania, praca_w_tle GENERUJ_ZADANIE
    API-->>SRC: 202 {zlecenie_generowania_id}
    WRK->>DB: liczba_cykli += 1 (WHERE liczba_cykli < 3)
    WRK->>GEN: generuj(Umiejętność, Poziom trudności, 3 Zadania bazowe tej Umiejętności, poprzednie niepowodzenia)
    GEN->>LLM: prompt + schemat JSON
    LLM-->>GEN: treść, Rozwiązanie wzorcowe, Kryteria oceniania
    WRK->>DB: INSERT zadanie (WYGENEROWANE, WALIDACJA), zadanie_wersja v1, kryteria, kalibracja_trudnosci, praca_w_tle WALIDUJ_ZADANIE
    WRK->>GEN: waliduj(zadanie_wersja)
    GEN->>LLM2: sama treść → wynik w notacji SymPy
    GEN->>SYM: zgodność wyników, ciągłość Rozwiązania wzorcowego, unikalność dystraktorów
    GEN->>MD: NOUL Q-WA-01 jednoznaczność, NOUL Q-WA-02 zgodność z podstawą, SCORE Q-WA-03 Poziom trudności
    GEN->>DB: podobieństwo pg_trgm do opublikowanych Zadań
    alt walidacja pozytywna, zlecenie Nauczyciela
        WRK->>DB: INSERT walidacja_zadania POZYTYWNY, status DO_PRZEGLADU, powiadomienie ZADANIE_DO_PRZEGLADU dla zlecającego (FR-14)
    else walidacja pozytywna, zlecenie Administratora lub systemu
        WRK->>DB: INSERT walidacja_zadania POZYTYWNY, status OPUBLIKOWANE (GLOBALNY) lub DO_PRZEGLADU (dowód)
    else negatywna i liczba_cykli < 3
        WRK->>DB: ODRZUCONE_W_WALIDACJI, nowa praca GENERUJ_ZADANIE z listą niespełnionych sprawdzeń
    else negatywna i liczba_cykli = 3
        WRK->>DB: zlecenie NIEUDANE
    end
    Note over API,DB: FR-14: zlecający Nauczyciel, recenzent treści lub Administrator → POST /zadania/{id}/przeglad, Zadanie GLOBALNY → POST /zadania/{id}/zgloszenia-bledu (8.4)
```

`liczba_cykli` liczy pełne cykle „generowanie + Walidacja” (≤ 3);
ponowienia techniczne pracy w tle (`praca_w_tle.proby`, np. timeout
dostawcy) nie zmieniają tego licznika. Inkrementacja jest warunkowa
(`UPDATE … SET liczba_cykli = liczba_cykli + 1 WHERE id = :id AND
liczba_cykli < 3 RETURNING`), więc CHECK nie może zostać naruszony; 0 wierszy
→ zlecenie `NIEUDANE`.

### 9.8 Kalibracja i Próg pewności (FR-34, NFR-03, NFR-04)

```mermaid
sequenceDiagram
    participant SCH as scheduler
    participant WRK
    participant DB
    participant ADM as Administrator
    SCH->>DB: 03:00 UTC INSERT praca_w_tle KALIBRUJ (dedup dzienny)
    WRK->>DB: Próbki 90 dni per (konfiguracja, rodzaj), uwzgledniana_w_progu, do now() − 5 min
    WRK->>WRK: wagi IPW z korektą wykonania Kontroli (11.5.3)
    WRK->>WRK: regresja izotoniczna ważona, 5-krotna walidacja krzyżowa (pewność surowa → zgodność)
    WRK->>WRK: krok 1 — wybór progu p_wyb: min t z UB Wilsona ≤ 2% i n_eff ≥ 189 (wartości out-of-fold, okno 90 dni)
    WRK->>WRK: krok 2 — ocena bezpieczników na stanie sprzed przebiegu (próg p0, kalibrator aktywny): wyzwolenie przy LB > 2% i n_eff ≥ 50 (ADR-046)
    WRK->>WRK: krok 3 — Blokada obniżania progu: wejście przy wyzwoleniu, wyjście przy UB ≤ 2% i n_eff ≥ 189 powyżej p0 (C4-02)
    WRK->>WRK: krok 4 — dozwolone obniżenie (≤ 0,02/dobę, test 30 dni powyżej docelowego progu, blokady)
    WRK->>WRK: krok 5 — wynik dnia p_nowy = max(p_dol, p0 + 0,02) przy wyzwoleniu (p0 = 0,990 → 1,000), inaczej p_dol
    WRK->>DB: UPSERT stan_bezpiecznika (n_eff, blad_ipw, LB, UB, aktywny) dla TRAFNOSC i POPRAWKI
    alt p_nowy ≠ p0 lub nowy kalibrator
        WRK->>DB: INSERT kalibrator_pewnosci, jeden INSERT prog_pewnosci aktywny (zrodlo wg kroku 5), poprzedni aktywny := false (1 transakcja)
    end
    opt bezpiecznik, ECE > 0.05 lub wykonanie Kontroli < 80%
        WRK->>DB: powiadomienie ALERT_KALIBRACJI do Administratorów
    end
    ADM->>DB: GET /admin/kalibracja (wykres niezawodności, ECE, odsetek automatycznych, wykonanie Kontroli)
```

### 9.9 Przełączenie na model zapasowy (NFR-07)

```mermaid
sequenceDiagram
    participant OCE as Agent oceniający
    participant CB as CircuitBreakerModeluDecyzyjnego
    participant JEV as JevAdapter
    participant ZAP as ZapasowyLlmAdapter
    OCE->>CB: wykonaj_plan(stan, plan, termin_miekki)
    alt obwód ZAMKNIETY
        CB->>CB: OgranicznikSzybkosci.przyjmij_plan (przewidywany koniec planu Jev ≤ termin miękki? ADR-039)
        alt plan nieprzyjęty albo brak przydziału dla fali przed jej terminem (13.2)
            CB->>ZAP: cały plan jednym wywołaniem (OGRANICZENIE_LOKALNE, obwód bez zmian)
        end
        CB->>JEV: plan w ≤ 2 rundach (wsad: 1 żądanie na rundę, pojedynczo: ≤ 3 fale, timeout 2 s na żądanie)
        alt sukces
            JEV-->>CB: decyzje składowe
        else 429
            CB->>CB: przydział × 0,5 na 30 s (bez licznika błędów obwodu), ponowienie lub model zapasowy jak niżej
        else błąd / timeout / 5xx / niezgodność ze schematem
            CB->>CB: licznik błędów++ (3 kolejne lub ≥ 50% w 20 s przy ≥ 6 wywołaniach → OTWARTY)
            opt do terminu miękkiego zostało ≥ 13 s (ponowienie 2 s + zapasowy 9 s + zapas 2 s)
                CB->>JEV: 1 ponowienie nieudanych pytań
            end
            CB->>ZAP: cały plan jednym wywołaniem (timeout 9 s), odpowiedzi Jev odrzucone
            ZAP-->>CB: decyzje składowe + rozkłady
        end
    else obwód OTWARTY (60 s)
        CB->>ZAP: cały plan jednym wywołaniem
    else PÓŁOTWARTY
        CB->>JEV: 1 próbny plan, pozostałe Oceny → ZAP
    end
    CB-->>OCE: RezultatPytan + konfiguracja_modelu_id użytego modelu
    Note over OCE: Pewność oceny liczona kalibratorem i Progiem pewności właściwym dla użytego modelu
```

Zasada: w ramach jednej Oceny wszystkie decyzje składowe pochodzą z
**jednego** modelu decyzyjnego. Jeśli Jev zawiedzie w dowolnej rundzie,
cały plan jest wykonywany ponownie modelem zapasowym w **jednym**
wywołaniu (rozkłady obu modeli nie są mieszane, bo mają różne kalibratory).
Najgorsze przypadki czasowe są policzone w 17.2.

### 9.10 Retencja zdjęć (NFR-09)

```mermaid
sequenceDiagram
    participant SCH as scheduler
    participant WRK
    participant DB
    participant S3
    SCH->>DB: co godzinę praca_w_tle USUN_ZDJECIA
    WRK->>DB: SELECT zdjęcia WHERE usunieto_at IS NULL AND usun_po < now() LIMIT 500
    loop każde zdjęcie
        WRK->>S3: DELETE obiekt i obiekt maskowany (idempotentne)
        WRK->>DB: UPDATE usunieto_at = now()
    end
    WRK->>DB: metryka zdjecia_zalegle (usun_po < now() − 24 h) → alert
```

`usun_po` jest jedynym źródłem prawdy o terminie usunięcia; ustawiają je
przejścia stanów opisane w 16.4 (m.in. `PORZUCONE`/`ANULOWANE` → `now()`,
otwarcie Weryfikacji na Rozwiązaniu `OCENIONE` → przedłużenie do limitu 90
dni). Reguła ograniczenia `CHECK (usun_po <= created_at + 90 dni)` gwarantuje
twardy limit.

### 9.11 Import Arkusza CKE (FR-35, FR-36)

```mermaid
sequenceDiagram
    actor A as Administrator
    participant API
    participant S3
    participant DB
    participant WRK
    A->>API: POST /admin/arkusze-cke/importy (ZIP: arkusz.json + rysunki)
    API->>S3: PUT importy/{id}.zip
    API->>DB: INSERT import_arkusza OCZEKUJE, praca_w_tle IMPORTUJ_ARKUSZ
    API-->>A: 202 {import_id}
    WRK->>S3: GET ZIP
    WRK->>WRK: walidacja schematu ArkuszImportV1 (12.9), sum punktów, kodów Umiejętności, SymPy wynik_koncowy_wzorcowy
    alt wszystkie Zadania poprawne
        WRK->>DB: 1 transakcja: arkusz_cke, zadanie (BAZOWE_ARKUSZ_CKE, GLOBALNY, OPUBLIKOWANE), wersje, kryteria, tagi, kalibracja_trudnosci
        WRK->>DB: import ZAKONCZONY + raport
    else błąd w dowolnym Zadaniu
        WRK->>DB: import ODRZUCONY + raport błędów per numer Zadania (nic nie zapisano)
    end
```

### 9.12 Zgłoszenie błędu odczytu przez Ucznia (FR-28)

```mermaid
sequenceDiagram
    actor U as Uczeń
    participant PWA
    participant API
    participant DB
    U->>PWA: „Zgłoś błąd odczytu” przy Ocenie (wybiera kroki, opis)
    PWA->>API: POST /rozwiazania/{id}/zgloszenie-bledu-odczytu (If-Match)
    API->>DB: BEGIN, SELECT rozwiazanie FOR UPDATE
    API->>DB: sprawdź warunki 11.6 (okno 14 dni od Oceny aktualnej, limity, zdjęcia dostępne, brak wcześniejszego zgłoszenia)
    alt otwarta Weryfikacja istnieje (OCZEKUJE lub W_TRAKCIE)
        API->>DB: UPDATE weryfikacja: powody += ZGLOSZENIE_BLEDU_ODCZYTU, zgloszenie, priorytet = min(priorytet, 2) (trigger: version + 1, powody_poczatkowe bez zmian)
    else brak otwartej Weryfikacji (OCENIONE)
        API->>DB: INSERT weryfikacja (ZGLOSZENIE_BLEDU_ODCZYTU, powody_poczatkowe = {ZGLOSZENIE_BLEDU_ODCZYTU}, ocena_weryfikowana = aktualna, priorytet 2, Kolejka weryfikacji 11.4)
        API->>DB: usun_po zdjęć = created_at + 90 dni
    end
    API->>DB: UPDATE wynik_zadania aktualnej Oceny: uniewazniony_at = now(), powod_uniewaznienia = ZGLOSZENIE_BLEDU_ODCZYTU
    API->>DB: praca_w_tle AKTUALIZUJ_PROFIL (odtworzenie bez zawieszonego Wyniku), powiadomienie NOWA_WERYFIKACJA, COMMIT
    API-->>PWA: 201 {weryfikacja: {status, powody, termin_at}}
```

Skutki zgłoszenia dla Ucznia (FR-28, uzupełnienie 2.1): od chwili zgłoszenia
do rozstrzygnięcia Wynik zadania tej Oceny jest **zawieszony** (wyłączony z
Profilu ucznia przez mechanizm unieważnienia z powodem
`ZGLOSZENIE_BLEDU_ODCZYTU`; Profil jest odtwarzany, 10.1.3), więc ewentualny
błąd odczytu nie obniża Profilu nawet przez czas trwania Weryfikacji. Ocena
pozostaje widoczna z komunikatem „Zgłoszenie błędu odczytu jest sprawdzane”
(11.3). Rozstrzygnięcie zawsze tworzy nową Ocenę Nauczyciela i nowy Wynik
zadania (9.6.2), więc zawieszony Wynik nigdy nie wraca do Profilu — zastępuje
go Wynik z Oceny Nauczyciela. W okresie karencji usunięcia konta
Weryfikacja zgłoszenia nie jest anulowana, tylko wstrzymana (ADR-040); po
przywróceniu konta wraca do kolejki z nowym terminem SLA i jest
rozstrzygana normalnie, więc zawieszony Wynik zostaje zastąpiony (C-33).
Gdy konto zostanie faktycznie usunięte, Wynik znika wraz z kontem. Nadużycie (zgłaszanie każdej
słabej Oceny, by usunąć ją z Profilu) ogranicza limit 1 zgłoszenia na
Rozwiązanie i 5 na 30 dni, a metryka `zgloszenia_bledu_odczytu_uznane_30d`
(11.8) per Uczeń < 20% przy ≥ 5 zgłoszeniach oznacza konto w raporcie
Administratora.

Zgłoszenie do Weryfikacji `W_TRAKCIE` zmienia treść, więc trigger
`trg_weryfikacja_wersja` podnosi `weryfikacja.version` (ADR-037), a
rozstrzygnięcie Nauczyciela wysłane z poprzednim `If-Match` dostaje `412
KONFLIKT_WERSJI` z aktualnym zasobem (w tym `zgloszenie`). To zamierzone:
Nauczyciel nie może rozstrzygnąć Weryfikacji bez zobaczenia zgłoszenia.
Blokada i szkic pozostają bez zmian, więc Nauczyciel wysyła rozstrzygnięcie
ponownie po przejrzeniu zgłoszenia (scenariusz C-28).

### 9.13 Ponowna ocena po błędzie przetwarzania (`PONOW_OCENE`)

```mermaid
sequenceDiagram
    participant WRK
    participant DB
    participant OCE as Agent oceniający
    WRK->>DB: pobiera PONOW_OCENE, czyta weryfikacja (version = v) i rozwiazanie (version = r)
    WRK->>WRK: warunek: weryfikacja OCZEKUJE, BLAD_PRZETWARZANIA, ocena_weryfikowana_id IS NULL
    WRK->>OCE: ocen(KontekstOceny), termin_miekki = now() + 60 s
    OCE-->>WRK: RezultatOceny
    WRK->>DB: BEGIN
    WRK->>DB: UPDATE weryfikacja … WHERE id = :id AND status = OCZEKUJE AND version = v
    alt 0 wierszy (Nauczyciel podjął lub rozstrzygnął)
        WRK->>DB: ROLLBACK, log rezultat_przestarzaly
    else trasa Weryfikacja (powody T-1…T-4)
        WRK->>DB: INSERT ocena WSTEPNA, weryfikacja: ocena_weryfikowana_id, powody := powody z 11.3, powody_poczatkowe := te same powody, priorytet, ponowna_ocena_at
        WRK->>DB: rozwiazanie (version = r) ocena_wystawiona_at, wynik_zadania (o ile brak mozliwy_blad_odczytu)
        WRK->>DB: NAPISZ_WSKAZOWKE (gdy błąd i brak mozliwy_blad_odczytu), OPINIA_POMOCNICZA (NISKA_PEWNOSC)
        WRK->>DB: AKTUALIZUJ_PROFIL (gdy powstał Wynik zadania), powiadomienie OCENA_WYSTAWIONA, COMMIT
    else trasa automatyczna (brak powodów)
        WRK->>DB: najpierw weryfikacja ANULOWANA (PONOWNA_OCENA_AUTOMATYCZNA), potem INSERT ocena OSTATECZNA
        WRK->>DB: rozwiazanie OCENIONE, wynik_zadania (1.00 lub 0.50), usun_po zdjęć
        WRK->>DB: T-5: INSERT weryfikacja KONTROLA_LOSOWA po anulowaniu (ux_weryfikacja_otwarta, 15.1 p. 3), usun_po = created_at + 90 dni
        WRK->>DB: NAPISZ_WSKAZOWKE (gdy błąd), AKTUALIZUJ_PROFIL, KOPIUJ_DO_ZBIORU_TRENINGOWEGO (bez Kontroli, za zgodą), powiadomienie OCENA_WYSTAWIONA, COMMIT
    end
```

`PONOW_OCENE` nie podnosi wcześniejszego czasu: `czas_systemowy_ms` jest już
zapisany jako naruszenie NFR-01 (T-6). Ocena powstała w `PONOW_OCENE`
podlega tym samym regułom 11.1–11.3 co pierwsza ocena. Nauczyciel, który
podjął Weryfikację (`W_TRAKCIE`), zawsze ma pierwszeństwo: warunek na
`status = OCZEKUJE AND version = v` gwarantuje, że rezultat `PONOW_OCENE`
nie nadpisze jego pracy (C-23). Od wersji 2.2 podjęcie nie zmienia
`version` (ADR-037), więc ochronę daje warunek `status = 'OCZEKUJE'`; warunek
`version = v` chroni przed równoległą zmianą treści (np. zgłoszeniem).

---

## 10. Agenci — kontrakty, algorytmy, pytania do Jev

Wszyscy agenci są klasami Pythona w `lemat.agenci.<nazwa>`, przyjmują i
zwracają modele Pydantic, nie wykonują operacji na bazie (Orkiestrator
dostarcza dane i zapisuje wyniki). Dzięki temu są testowalni bez bazy i bez
sieci (porty mają implementacje testowe).

### 10.1 Agent rekomendujący

#### 10.1.1 Kontrakt

```python
class SeriaSkrot(BaseModel):
    umiejetnosc_id: int
    rozpoczeta_at: datetime
    liczba_elementow: int                             # 0..8
    poziom_trudnosci: int                             # docelowy Poziom trudności kolejnego Zadania
    kolejne_porazki: int

class WejscieRekomendacji(BaseModel):
    uczen_id: UUID
    poziom_matury: PoziomMatury
    termin_egzaminu: date | None
    wybrany_dzial_id: int | None                      # FR-09
    opanowanie: list[OpanowanieUmiejetnosci]          # migawka Profilu ucznia
    ostatnie_wyniki: list[WynikZadaniaSkrot]          # 50 ostatnich nieunieważnionych, malejąco po czasie
    ostatnie_elementy: list[ElementSciezkiSkrot]      # 10 ostatnich elementów Ścieżki nauki
    otwarte_przypisania: list[PrzypisanieSkrot]       # pozycje bez próby w kontekście Przypisania (ux_*_przypisanie), z termin
    seria: SeriaSkrot | None                          # ADR-030
    wkladki_przeczytane: dict[int, datetime]          # umiejetnosc_id → ostatnie przeczytanie
    udzial_zadan_otwartych: Literal[3, 5]             # co który element ZADANIE jest otwarty (11.4.3, 11.5.1)
    dowody_dozwolone: bool                            # false przy kontroli przyjęć kolejki GLOBALNA (11.4.3)
    strategia: StrategiaRekomendacji                  # POLITYKA_ELO | JEV_CHOICE
    teraz: datetime

class BrakElementu(BaseModel):
    kod: Literal["BRAK_ZADAN_W_ZAKRESIE"]
    komunikat: str                                    # tekst dla Ucznia (10.1.5)
    sugerowane_dzialy: list[int]                      # Działy z dostępnymi Zadaniami

class DecyzjaRekomendacji(BaseModel):
    typ: TypElementuSciezki | None                    # WKLADKA_TEORETYCZNA | ZADANIE; None ⇔ brak_elementu
    cel: CelElementu                                  # NAUKA | POWTORKA
    umiejetnosc_id: int | None
    zadanie_id: UUID | None
    wkladka_teoretyczna_id: UUID | None
    przypisanie_id: UUID | None                       # tylko R-PRZYPISANIE
    przewidywane_p_sukcesu: float | None
    regula: Literal["R-PRZYPISANIE","R-SERIA","R-POWTORKA","R-WKLADKA","R-WKLADKA-DIAG","R-NAJSLABSZA",
                    "R-TRUDNOSC_W_GORE","R-UTRWALENIE","R-JEV-WKLADKA","R-JEV-ZADANIE_LATWE",
                    "R-JEV-ZADANIE_MATURALNE","R-JEV-POWTORKA"] | None
    uzasadnienie: str                                 # FR-10, z szablonu 10.1.5 (także dla brak_elementu)
    uzasadnienie_fakty: dict[str, Any]
    zmiana_serii: Literal["BEZ_ZMIAN","ROZPOCZNIJ","ZAKONCZ"]
    w_serii: bool
    potrzebne_uzupelnienie_puli: list[ZapotrzebowaniePuli]   # klucz 10.1.2 „Zapotrzebowanie puli”
    potrzebne_szkice_wkladek: list[int]               # umiejetnosc_id bez opublikowanej Wkładki (E-20)
    decyzje_modelu: list[DecyzjaSkladowa]             # pusta dla POLITYKA_ELO
    brak_elementu: BrakElementu | None
```

#### 10.1.2 Polityka POLITYKA_ELO (domyślna)

Kroki wykonywane w kolejności; pierwszy, który zwraca decyzję, kończy algorytm.
„Poziom trudności” oznacza skalibrowany `kalibracja_trudnosci.poziom_trudnosci`.

1. **Przypisania (FR-32).** Jeśli istnieje otwarte Przypisanie — wybierz
   pozycję z najbliższym terminem, dla której nie istnieje żadna próba w
   kontekście tego Przypisania (Odpowiedź albo nieporzucone Rozwiązanie z
   tym `przypisanie_id`, z dowolnej drogi — ten sam warunek co indeksy
   `ux_odpowiedz_przypisanie`, `ux_rozwiazanie_przypisanie`); Zadanie
   `WYCOFANE` jest pomijane; pozycja z Zadaniem zamkniętym jest pomijana po
   terminie (klucz jest już ujawniony Klasie, 12.5); `R-PRZYPISANIE`,
   element z `przypisanie_id`, z którego każda próba dziedziczy
   `przypisanie_id` (trigger `trg_kontekst_proby`, ADR-032). Przypisanie ma
   pierwszeństwo przed Serią (Seria nie jest kończona, tylko wstrzymana).
2. **Zakres.** Kandydackie Umiejętności = Umiejętności poziomu matury Ucznia
   (`tylko_rozszerzony = false` dla `PODSTAWOWY`), zawężone do
   `wybrany_dzial_id`, jeśli ustawiony (FR-09).
3. **Seria (wizja, ADR-030).** Jeśli Seria jest aktywna i jej Umiejętność
   należy do zakresu z kroku 2 — wybierz Zadanie tej Umiejętności (główna) o
   Poziomie trudności = `seria.poziom_trudnosci`, bez Wyniku zadania w 60
   dniach, zgodnie z regułą rodzaju z kroku 7; brak na tym poziomie →
   najbliższy wyższy poziom, potem najbliższy niższy; brak w ogóle →
   `zmiana_serii = ZAKONCZ` (powód `BRAK_ZADAN`), zapotrzebowanie puli i
   przejście do kroku 4. `R-SERIA`, `w_serii = true`. Seria wyklucza krok 4
   (powtórka), bo realizuje wizję „serii aż do poziomu maturalnego”.
   Seria spoza zakresu (Uczeń zmienił `wybrany_dzial_id`) → `ZAKONCZ`.
4. **Powtórka (FR-08).** Jeśli wśród kandydatów jest Umiejętność z
   `nastepna_powtorka_at ≤ teraz` i w 4 ostatnich elementach nie było
   elementu z `cel = POWTORKA` — wybierz najdawniej zaległą; Zadanie dobierz
   jak w kroku 7 z docelowym P = 0.80; `R-POWTORKA`, `cel = POWTORKA`.
5. **Wybór Umiejętności.** Dla każdego kandydata z `poziom_opanowania < 0.80`:
   `priorytet = (1 − poziom_opanowania) × waga_egzaminacyjna × pilnosc`,
   gdzie `pilnosc = 1 + max(0, (90 − dni_do_egzaminu) / 90)` (1, gdy brak
   terminu). Jeśli którakolwiek Umiejętność wymagana (`umiejetnosc_wymagana`)
   ma `poziom_opanowania < 0.50`, zamiast kandydata rozpatrywana jest
   najsłabsza z wymaganych (rekurencyjnie, maks. głębokość 3). Wybór:
   maksymalny priorytet; remis → mniejszy `id`.
   **5b. Wszystko opanowane** (brak kandydata z `poziom_opanowania < 0.80`):
   wybierz Umiejętność o najwcześniejszym `nastepna_powtorka_at` (powtórka
   przyspieszona) i Zadanie o Poziomie trudności ≥ 4 z P najbliższym 0.675;
   `R-UTRWALENIE`, `cel = POWTORKA`. Gdy `wybrany_dzial_id` jest ustawiony,
   uzasadnienie dopisuje „Dział {dzial} jest opanowany — możesz wybrać inny
   dział w ustawieniach.”. Gdy ta Umiejętność nie ma Zadania na Poziomie
   trudności ≥ 4 (warunki kandydatów jak w kroku 7): kolejne Umiejętności
   według `nastepna_powtorka_at` (maks. 5), potem Zadanie o najwyższym
   Poziomie trudności w całym zakresie z kroku 2 (z tą samą regułą
   `R-UTRWALENIE`) i zapotrzebowanie puli dla pierwszej Umiejętności na
   Poziomie trudności 4; brak jakiegokolwiek Zadania → krok 8.
6. **Wkładka teoretyczna (FR-06).** Jeśli dla wybranej Umiejętności
   (`liczba_wynikow ≥ 2` lub `inicjalizacja = TEST_DIAGNOSTYCZNY`) oraz
   (`poziom_opanowania < 0.40` lub 2 ostatnie Wyniki zadania z tej
   Umiejętności mają `ulamek < 0.5`) oraz Wkładka nie była przeczytana w
   ostatnich 14 dniach:
   - istnieje opublikowana Wkładka → zwróć ją; `R-WKLADKA`, a gdy wszystkie
     Wyniki zadania tej Umiejętności pochodzą z Testu diagnostycznego —
     `R-WKLADKA-DIAG`;
   - brak opublikowanej Wkładki → `potrzebne_szkice_wkladek += [U]` (praca
     `GENERUJ_SZKIC_WKLADKI`, E-20) i przejście do kroku 7;
   - w obu przypadkach `zmiana_serii = ROZPOCZNIJ` dla Umiejętności U z
     `seria.poziom_trudnosci` = Poziom trudności (≤ 3) Zadania o P
     najbliższym 0.675.
7. **Zadanie (FR-05, FR-07).** Kandydaci: Zadania `OPUBLIKOWANE`,
   `zasieg = GLOBALNY`, bez wiersza `wstrzymanie_w_doborze` (ADR-038; warunek
   obowiązuje w każdym kroku doboru 3–8), z tą Umiejętnością główną, poziomu matury Ucznia,
   bez Wyniku zadania tego Ucznia w ostatnich 60 dniach i nieobecne w jego
   otwartych Przypisaniach. Rodzaj: co `udzial_zadan_otwartych`-ty element
   typu `ZADANIE` w Ścieżce nauki jest otwarty (`OTWARTE_OBLICZENIOWE` lub —
   gdy `dowody_dozwolone` — `OTWARTE_Z_DOWODEM`), pozostałe `ZAMKNIETE`;
   gdy brak kandydatów danego rodzaju — dowolny dozwolony rodzaj. Wybór:
   Zadanie z `P` (10.1.3) najbliższym 0.675 w przedziale [0.60, 0.75]; remis
   → Zadanie bazowe przed wygenerowanym, potem mniejsza
   `liczba_wynikow_kalibracji`. Brak w przedziale → najbliższe z
   `|P − 0.675| ≤ 0.20` i zapotrzebowanie puli; brak i takiego → kolejna
   Umiejętność z rankingu kroku 5 (maks. 3), a na końcu Zadanie o najbliższym
   P bez ograniczenia w całym zakresie z kroku 2. Reguła: `R-TRUDNOSC_W_GORE`,
   jeśli wybrany Poziom trudności jest wyższy niż w poprzednim elemencie tej
   Umiejętności, w przeciwnym razie `R-NAJSLABSZA`.
8. **Brak Zadań.** Gdy w całym zakresie nie ma żadnego Zadania spełniającego
   warunki kroku 7 (np. start platformy, nowa Umiejętność rozszerzona):
   `typ = None`, `brak_elementu = {kod: BRAK_ZADAN_W_ZAKRESIE, …}`,
   zapotrzebowanie puli dla Umiejętności z kroku 5 na Poziomach trudności
   2–4 (dla `ZAMKNIETE` i `OTWARTE_OBLICZENIOWE`). Endpoint zwraca `200` z
   `element = null` i `brak_elementu` (12.4); UI pokazuje komunikat i listę
   `sugerowane_dzialy`. Element Ścieżki nauki nie powstaje.

**Zapotrzebowanie puli** — klucz jednolity w całym systemie (10.1, 13.3,
`zlecenie_generowania`): `(umiejetnosc_id, poziom_trudnosci, poziom_matury,
rodzaj)`, gdzie `rodzaj ∈ {ZAMKNIETE, OTWARTE_OBLICZENIOWE}`. Warunek:
mniej niż 3 Zadania `OPUBLIKOWANE` i `GLOBALNY` dla klucza → praca
`UZUPELNIJ_PULE` z tym kluczem. Zadania z dowodem nie są uzupełniane
automatycznie (wymagają Przeglądu, ADR-015); powstają na zlecenie
Nauczyciela lub Administratora.

**Reguły Serii** (stosowane w `AKTUALIZUJ_PROFIL` po każdym Wyniku zadania z
elementu `w_serii`, ADR-030):

| Zdarzenie | Skutek |
|---|---|
| `ulamek ≥ 0.7` i Poziom trudności Zadania ≥ 4 | Seria zakończona sukcesem (cel wizji: poziom maturalny) |
| `ulamek ≥ 0.7` i Poziom trudności < 4 | `poziom_trudnosci := min(5, poziom + 1)`, `kolejne_porazki := 0` |
| `0.5 ≤ ulamek < 0.7` | poziom bez zmian, `kolejne_porazki := 0` |
| `ulamek < 0.5` | `kolejne_porazki += 1`; przy 2 kolejnych `poziom := max(1, poziom − 1)` |
| `kolejne_porazki = 3` lub `liczba_elementow = 8` lub 7 dni od rozpoczęcia | Seria zakończona; następna rekomendacja wraca do kroku 5 (zwykle Umiejętność wymagana) |

Poziom trudności w Serii jest niemalejący, dopóki Uczeń odnosi sukcesy, co
realizuje „serię zadań o stopniowo rosnącej trudności, aż do poziomu
maturalnego” z wizji.

#### 10.1.3 Model Profilu ucznia (Elo per Umiejętność) — ADR-010

- `σ(x) = 1 / (1 + e^(−x))`.
- Parametr Zadania: `trudnosc_b` (tabela `kalibracja_trudnosci`), inicjalnie
  `0.8 × (poziom_trudnosci_nominalny − 3)` (Poziom trudności 1…5 →
  −1.6…1.6).
- Umiejętność efektywna: `θ̄ = 0.7·θ_główna + 0.3·średnia(θ_dodatkowe)`
  (gdy brak dodatkowych: `θ̄ = θ_główna`).
- Prawdopodobieństwo sukcesu: `P = c + (1 − c)·σ(θ̄ − b)`, gdzie `c = 0.25`
  dla `WYBOR_JEDNOKROTNY` (4 opcje), `c = 0.5^n` dla `PRAWDA_FALSZ` z `n`
  stwierdzeniami, `c = 0` dla zadań otwartych.
- **Waga Wyniku zadania** `waga = w_status × w_proba`:
  `w_status = 1.0` dla Odpowiedzi i Oceny ostatecznej, `0.5` dla Oceny
  wstępnej **bez** `mozliwy_blad_odczytu`; Ocena wstępna z
  `mozliwy_blad_odczytu = true` **nie tworzy** Wyniku zadania (FR-28,
  ADR-022), a Wynik zadania Oceny objętej Zgłoszeniem błędu odczytu jest
  zawieszony do rozstrzygnięcia (`powod_uniewaznienia =
  'ZGLOSZENIE_BLEDU_ODCZYTU'`, 11.6) i — jak każdy unieważniony — pomijany
  przy odtwarzaniu. `w_proba = 1.0` dla pierwszej próby Ucznia przy danym Zadaniu w
  60 dniach, `0.5` dla każdej kolejnej (np. to samo Zadanie w Przypisaniu i
  wcześniej w Ścieżce nauki). Wartości: {1.00, 0.50, 0.25}.
  `liczy_do_trudnosci = (waga = 1.00)`.
- Aktualizacja po Wyniku zadania `s = ulamek` z wagą `w`:
  `Δ = s − P`; `θ_główna += w·K_u·Δ`; `θ_dodatkowa += w·0.5·K_u·Δ`;
  `K_u = max(0.2, 0.8 / (1 + n/20))`, `n` = `liczba_wynikow` Umiejętności.
  Dla Wyników z Testu diagnostycznego `K_u = 0.8`.
- **Kalibracja Poziomu trudności** (słownik: „kalibrowana na podstawie
  wyników uczniów”, ADR-029) — tylko gdy `liczy_do_trudnosci`, jednym
  atomowym poleceniem (bez odczytu–modyfikacji–zapisu w ORM):

  ```sql
  WITH stary AS (
      SELECT poziom_trudnosci FROM kalibracja_trudnosci WHERE zadanie_id = :zadanie_id FOR UPDATE)
  UPDATE kalibracja_trudnosci k
  SET trudnosc_b = k.trudnosc_b - greatest(0.05, 0.4 / (1 + k.liczba_wynikow_kalibracji / 30.0)) * :delta,
      liczba_wynikow_kalibracji = k.liczba_wynikow_kalibracji + 1,
      poziom_trudnosci = CASE WHEN k.liczba_wynikow_kalibracji + 1 < 30 THEN k.poziom_trudnosci
                              ELSE poziom_z_b(k.trudnosc_b - greatest(0.05, 0.4 / (1 + k.liczba_wynikow_kalibracji / 30.0)) * :delta,
                                              k.poziom_trudnosci) END,
      updated_at = now()
  WHERE k.zadanie_id = :zadanie_id
  RETURNING k.poziom_trudnosci AS nowy, (SELECT poziom_trudnosci FROM stary) AS stary;
  ```

  `poziom_z_b(b, obecny)` (funkcja SQL `IMMUTABLE`): progi `b < −1.2 → 1`,
  `[−1.2, −0.4) → 2`, `[−0.4, 0.4) → 3`, `[0.4, 1.2) → 4`, `≥ 1.2 → 5`, z
  histerezą 0.1 (poziom zmienia się dopiero, gdy `b` przekroczy granicę o ≥
  0.1), aby uniknąć oscylacji. Gdy `nowy ≠ stary`, ta sama transakcja
  zapisuje `dziennik_audytu` (`POZIOM_TRUDNOSCI_SKALIBROWANY`, `rola =
  NULL`). `Δ` jest liczone z `b` odczytanego przed aktualizacją; przy
  równoległych Wynikach zadania to przybliżenie (opóźnienie o ≤ kilka
  wyników) jest akceptowane, natomiast żadna aktualizacja nie ginie, bo
  odejmowanie wykonuje baza. `zadanie.version` nie zmienia się (C1-23).
- `poziom_opanowania = σ(θ − 0.8)` — prawdopodobieństwo sukcesu przy Zadaniu
  o Poziomie trudności 4 (maturalnym).
- Poziom opanowania Działu (FR-04) = średnia ważona `waga_egzaminacyjna`
  poziomów opanowania jego Umiejętności.
- **Test diagnostyczny**: Wyniki zadania z testu mają `created_at =
  zakonczono_at` testu i są stosowane razem (w kolejności pozycji); po
  zastosowaniu ostatniego z nich Umiejętności Działu z `liczba_wynikow = 0`
  w tej chwili dostają średnią `θ` przetestowanych Umiejętności tego Działu
  (`PROPAGACJA_Z_DZIALU`), a przetestowane — `inicjalizacja =
  TEST_DIAGNOSTYCZNY`.
- Powtórki: po Wyniku zadania z `ulamek ≥ 0.7` dla Umiejętności z
  `poziom_opanowania ≥ 0.80`: `licznik_powtorek += 1`,
  `nastepna_powtorka_at = teraz + [3, 7, 14, 30, 60][min(licznik−1, 4)] dni`;
  przy `ulamek < 0.5`: `licznik_powtorek = 0`, `nastepna_powtorka_at = teraz + 1 dzień`.
- **Odtwarzanie** (po unieważnieniu Wyniku zadania): **cały Profil ucznia**
  wraca do stanu DOMYŚLNEGO (`θ = 0`, `liczba_wynikow = 0`,
  `inicjalizacja = DOMYSLNA`, liczniki powtórek zerowane), następnie
  wszystkie nieunieważnione Wyniki zadania Ucznia — **łącznie z Wynikami z
  Testu diagnostycznego** — są stosowane chronologicznie (`created_at`,
  potem `id`) według reguł powyżej, z bieżącymi `trudnosc_b`. Nie istnieje
  „punkt startowy po teście”, więc żaden Wynik nie jest liczony podwójnie
  (C1-11). Operacja jest deterministyczna i idempotentna; dla ≤ 5 000
  Wyników trwa < 50 ms. `trudnosc_b` nie jest odtwarzane (korekta wpływa na
  `b` tylko przez nowy Wynik o wadze 1.00).

#### 10.1.4 Migawka cech (`rekomendacja.cechy`)

```json
{
  "poziom_matury": "PODSTAWOWY", "dni_do_egzaminu": 142, "wybrany_dzial_id": null,
  "seria": {"umiejetnosc_id": 512, "liczba_elementow": 3, "poziom_trudnosci": 3},
  "kandydaci": [ {"umiejetnosc_id": 512, "poziom_opanowania": 0.31, "waga": 0.042, "priorytet": 0.029} ],
  "ostatnie_wyniki_umiejetnosci": [ {"zadanie_id": "…", "ulamek": 0.0, "kiedy": "2026-10-30T18:01:00Z",
                                     "z_testu_diagnostycznego": false} ],
  "zadania_w_strefie": 4, "udzial_zadan_otwartych": 3, "dowody_dozwolone": true
}
```

#### 10.1.5 Szablony uzasadnień (FR-10, NFR-05)

Uzasadnienie jest generowane deterministycznie z faktów (ADR-011); liczebniki
odmieniane funkcją `odmien(n, "zadanie", "zadania", "zadań")`. Każda
wartość `regula` (także strategii JEV_CHOICE) i stan „brak elementu” mają
szablon — test jednostkowy sprawdza pokrycie wszystkich wartości `Literal`.

| Reguła | Szablon | Fakty |
|---|---|---|
| R-PRZYPISANIE | „Zadanie z zestawu „{zestaw}” od nauczyciela {nauczyciel}, termin: {termin}.” | `zestaw`, `nauczyciel`, `termin` |
| R-SERIA | „Kontynuujesz serię z „{umiejetnosc}”: zadanie {k} z serii, poziom trudności {poziom} (cel: poziom maturalny 4).” | `umiejetnosc`, `k`, `poziom` |
| R-POWTORKA | „Powtórka: ostatnie ćwiczenie z „{umiejetnosc}” było {dni} dni temu.” | `umiejetnosc`, `dni` |
| R-WKLADKA | „{bledne} z ostatnich {n} {odmiana} z „{umiejetnosc}” rozwiązano błędnie — zacznij od krótkiej teorii.” | `bledne`, `n` (≥ 2), `umiejetnosc` |
| R-WKLADKA-DIAG | „W teście diagnostycznym {bledne} z {n} {odmiana} z „{umiejetnosc}” rozwiązano błędnie — zacznij od krótkiej teorii.” | `bledne`, `n` (≥ 1), `umiejetnosc` |
| R-NAJSLABSZA | „„{umiejetnosc}” to obecnie najsłabsza umiejętność w dziale {dzial} (opanowanie {procent}%). Zadanie dobrano tak, aby szansa na poprawne rozwiązanie wynosiła ok. {p}%.” | `umiejetnosc`, `dzial`, `procent`, `p` |
| R-TRUDNOSC_W_GORE | „{k} ostatnie {odmiana} z „{umiejetnosc}” rozwiązano poprawnie — poziom trudności rośnie do {poziom}.” | `k`, `umiejetnosc`, `poziom` |
| R-UTRWALENIE | „Wszystkie umiejętności w zakresie masz opanowane (co najmniej 80%). Utrwalenie na poziomie maturalnym: „{umiejetnosc}”.” | `umiejetnosc` |
| R-JEV-WKLADKA | „Model oceny postępów wskazał z prawdopodobieństwem {p_modelu}%, że przed kolejnymi zadaniami z „{umiejetnosc}” warto przeczytać krótką teorię (opanowanie {procent}%).” | `p_modelu`, `umiejetnosc`, `procent` |
| R-JEV-ZADANIE_LATWE | „Model oceny postępów wskazał z prawdopodobieństwem {p_modelu}%, że teraz najlepsze będzie łatwiejsze zadanie z „{umiejetnosc}” (szansa na sukces ok. {p}%).” | `p_modelu`, `umiejetnosc`, `p` |
| R-JEV-ZADANIE_MATURALNE | „Model oceny postępów wskazał z prawdopodobieństwem {p_modelu}%, że możesz spróbować zadania na poziomie maturalnym z „{umiejetnosc}”.” | `p_modelu`, `umiejetnosc` |
| R-JEV-POWTORKA | „Model oceny postępów wskazał z prawdopodobieństwem {p_modelu}%, że teraz warto powtórzyć „{umiejetnosc}” (ostatnie ćwiczenie {dni} dni temu).” | `p_modelu`, `umiejetnosc`, `dni` |
| brak elementu | „Nie mamy teraz zadań dopasowanych do Twojego zakresu — przygotowujemy nowe. Możesz w tym czasie wybrać inny dział: {dzialy}.” | `dzialy` |

Gdy ustawiony jest `wybrany_dzial_id`, uzasadnienie zaczyna się od
„Ćwiczysz wybrany dział: {dzial}.”. Gdy Seria została rozpoczęta, do
uzasadnienia R-WKLADKA/R-WKLADKA-DIAG/R-NAJSLABSZA dopisywane jest „Po tej
części zaczniesz serię zadań z rosnącym poziomem trudności.”.

#### 10.1.6 Strategia eksperymentalna JEV_CHOICE (grupa eksperymentu)

- Przydział: `uczen.procent_grupy_eksperymentu < LEMAT_REKOMENDACJA_PROCENT_JEV`
  (domyślnie 0; plan pilotażu: 20 od 4. tygodnia).
- Nie dotyczy kroków 1 (Przypisania) i 3 (Seria) ani kroku 8. Zastępuje
  krok 6 i wybór poziomu w kroku 7: pytanie `Q-RE-01` (CHOICE) o opcjach
  `WKLADKA` (→ `R-JEV-WKLADKA`, rozpoczęcie Serii), `ZADANIE_LATWE` (docelowe
  P = 0.85, `R-JEV-ZADANIE_LATWE`), `ZADANIE_MATURALNE` (Poziom trudności 4,
  `R-JEV-ZADANIE_MATURALNE`), `POWTORKA` (Umiejętność o najwcześniejszym
  `nastepna_powtorka_at` wśród opanowanych, `R-JEV-POWTORKA`, `cel =
  POWTORKA`). Stan: opis sytuacji Ucznia wygenerowany z migawki cech
  (szablon tekstowy, bez danych osobowych). `p_modelu` w uzasadnieniu = masa
  rozkładu w modzie.
- Wywołanie w procesie `api` z timeoutem 1,5 s, bez ponowień, przez ten sam
  circuit breaker co Jev w workerze (stan obwodu w pamięci procesu `api`).
  Gdy `max(rozkład) < 0.5`, błąd, timeout lub obwód otwarty → polityka
  POLITYKA_ELO (fakt zapisany w `rekomendacja.decyzja.powrot_do_polityki` z
  wartością `NISKA_PEWNOSC_MODELU` lub `MODEL_NIEDOSTEPNY`). Model zapasowy
  nie jest używany w rekomendacji (latencja LLM ≫ 1,5 s).
- Metryka porównawcza: przyrost średniego `poziom_opanowania` po 14 dniach
  i odsetek Wyników zadania z `ulamek ≥ 0.5`.

#### 10.1.7 Port PredyktorSukcesu i model drzewiasty

`PredyktorSukcesu.p_sukcesu(uczen, zadanie) -> float` ma implementację
`EloPredyktor` (domyślną). `GbmPredyktor` (LightGBM; cechy: θ Umiejętności,
`trudnosc_b`, rodzaj, liczba Wyników, dni od ostatniej próby, średni `ulamek`
z 5 ostatnich) jest trenowany offline, gdy liczba Wyników zadania ≥ 20 000;
zastępuje `EloPredyktor` tylko, jeśli na zbiorze z ostatnich 30 dni ma
AUC wyższe o ≥ 0.02 i log-loss niższy (decyzja Administratora przez
Konfigurację modelu).

### 10.2 Agent generujący

#### 10.2.1 Kontrakt

```python
class ZlecenieGenerowaniaDto(BaseModel):
    umiejetnosc: UmiejetnoscDto                       # kod, nazwa, Dział
    poziom_trudnosci: conint(ge=1, le=5)              # nominalny docelowy
    poziom_matury: PoziomMatury
    rodzaj: RodzajZadania
    przyklady: list[ZadanieBazoweDto]                 # 3 Zadania bazowe tej Umiejętności (najbliższy Poziom trudności)
    poprzednie_niepowodzenia: list[str]               # kody sprawdzeń W-x z poprzednich cykli

class ZadanieWygenerowaneDto(BaseModel):
    tresc_md: constr(max_length=8000)
    rodzaj: RodzajZadania
    forma_zamknieta: FormaZamknieta | None
    opcje_zamkniete: OpcjeZamkniete | None  # 7.5.1
    rozwiazanie_wzorcowe: RozwiazanieWzorcowe         # 7.5.2
    wynik_koncowy_wzorcowy: WynikKoncowyWzorcowy | None   # 7.5.3
    kryteria_oceniania: list[KryteriumOcenianiaDto]   # wariant, nr_kryterium, opis, punkty_max, czy_wynik_koncowy, wymaga_nr_kryterium
    punkty_max: conint(ge=1, le=6)
    umiejetnosci_dodatkowe: list[str]                 # kody Umiejętności
```

Szkice Wkładek teoretycznych (`GENERATYWNY_WKLADKI`) powstają przez to samo
API agenta (`generuj_wkladke(umiejetnosc) -> WkladkaDto`) w dwóch
przypadkach: na żądanie Administratora (`POST /admin/wkladki-teoretyczne/szkice`)
albo automatycznie, gdy rekomendacja natrafi na brak opublikowanej Wkładki
(praca `GENERUJ_SZKIC_WKLADKI`, E-20, z limitami C2-16: najwyżej 2
automatyczne szkice tej Umiejętności odrzucone (`WYCOFANA`) w 30 dniach — po
drugim odrzuceniu automatyczne szkice są wstrzymane na 30 dni, a
Administratorzy dostają alert „Wkładka wymaga autora”; koszt szkicu
automatycznego liczy się do dziennego budżetu generowania systemowego
12.7). Szkic zawsze kończy w statusie
`DO_PRZEGLADU` i jest publikowany wyłącznie przez recenzenta treści lub
Administratora (ADR-015, ADR-028); powiadomienie `WKLADKA_DO_PRZEGLADU`
otrzymują recenzenci treści i Administratorzy.

#### 10.2.2 Sprawdzenia Walidacji (FR-13)

| Kod | Sprawdzenie | Kryterium pozytywne | Dotyczy |
|---|---|---|---|
| W-1 | Schemat i spójność (Pydantic, suma kryteriów = `punkty_max` per wariant, ≤ 1 kryterium wyniku końcowego, łącznie ≤ 24 kryteria) | brak błędów | wszystkie |
| W-2 | `wynik_koncowy_wzorcowy.sympy` i opcje zamknięte parsują się w SymPy (`sympify(strict=True)` na danych z modelu, nie od Ucznia) | parsowanie OK | poza dowodem |
| W-3 | Niezależne rozwiązanie: LLM w roli `GENERATYWNY_ROZWIAZUJACY_NIEZALEZNIE` dostaje tylko treść (i opcje) | wynik równoważny `wynik_koncowy_wzorcowy` (SymPy) lub ta sama opcja | poza dowodem |
| W-4 | Ciągłość Rozwiązania wzorcowego: pary kroków `NIEROWNOWAZNE`/`NIEROZSTRZYGNIETE` sprawdzane NOUL `Q-WA-04` „Czy krok k+1 wynika z kroku k?” | każde `p_tak ≥ 0.80` | wszystkie otwarte |
| W-5 | Jednoznaczność: NOUL `Q-WA-01` „Czy treść ma dokładnie jedną poprawną odpowiedź / jednoznaczną tezę?” | `p_tak ≥ 0.85` | wszystkie |
| W-6 | Zgodność z podstawą programową: NOUL `Q-WA-02` (stan: treść + opis Umiejętności + poziom matury) | `p_tak ≥ 0.85` | wszystkie |
| W-7 | Poziom trudności: SCORE `Q-WA-03` (1–5) | wartość oczekiwana w odległości ≤ 1 od żądanego | wszystkie |
| W-8 | Dystraktory: opcje parami nierównoważne (SymPy), dokładnie jedna poprawna | spełnione | zamknięte |
| W-9 | Duplikat: `similarity(tresc_md, …)` z opublikowanymi Zadaniami tej Umiejętności | `< 0.80` | wszystkie |
| W-10 | Poprawność dowodu wzorcowego: NOUL `Q-WA-05` | `p_tak ≥ 0.90`, a następnie obowiązkowy Przegląd zadania | dowód |

#### 10.2.3 Schemat `walidacja_zadania.sprawdzenia`

```json
{ "W-1": {"ok": true}, "W-3": {"ok": false, "wynik_niezalezny": "FiniteSet(2)", "wynik_wzorcowy": "FiniteSet(2, -2)"},
  "W-5": {"ok": true, "p_tak": 0.93}, "W-9": {"ok": true, "max_podobienstwo": 0.41, "zadanie_id": "…"} }
```

### 10.3 Agent rozpoznający

#### 10.3.1 Kontrakt i przetwarzanie

```python
class WejscieRozpoznawania(BaseModel):
    rozwiazanie_id: UUID
    przeslanie_nr: int
    zdjecia: list[ZdjecieRef]           # klucze obiektów po normalizacji, kolejność stron
    tresc_zadania_md: str               # kontekst nazw zmiennych; BEZ Rozwiązania wzorcowego
    tekst_alternatywny_rysunkow: list[str]
    termin: datetime                    # początek bieżącej próby pracy ROZPOZNAJ_PISMO + 14 s (timeout wywołania, 17.2);
                                        # liczony per próba, bo przy ponowieniu przeslano_at + 14 s już minęło (S-7)

class ObszarNaZdjeciu(BaseModel):
    zdjecie: int                        # numer zdjęcia (kolejnosc)
    ramka: tuple[int, int, int, int]    # [x, y, szerokość, wysokość] w pikselach znormalizowanego zdjęcia

class ObszarDanychOsobowych(ObszarNaZdjeciu): ...   # ramka danych osobowych (imię, nazwisko, podpis, szkoła)
class ObszarKroku(ObszarNaZdjeciu): ...             # ramka linii kroku Transkrypcji (10.3.2, C3-07)

class RezultatRozpoznawania(BaseModel):
    czytelne: bool
    zgodnosc_z_zadaniem: confloat(ge=0, le=1)
    kroki: conlist(KrokTranskrypcjiDto, max_length=40)
    obszary_danych_osobowych: list[ObszarDanychOsobowych]   # imię, nazwisko, podpis, nazwa szkoły

class PoprawkaDoSprawdzenia(BaseModel):  # ADR-036
    nr: int                             # nr kroku w Transkrypcji z Poprawkami
    nr_kroku_agenta: int | None         # None = krok wstawiony
    odczyt_agenta: KrokTranskrypcjiDto | None
    poprawka: KrokTranskrypcjiDto
    obszar: ObszarKroku                 # ramka linii kroku agenta; dla wstawionego: obszar między krokiem
                                        # agenta poprzedzającym i następnym (suma ich ramek z przerwą, C3-05)

class WejscieSprawdzeniaPoprawek(BaseModel):
    rozwiazanie_id: UUID
    transkrypcja_id: UUID
    zdjecia: list[ZdjecieRef]
    poprawki: conlist(PoprawkaDoSprawdzenia, min_length=1, max_length=22)
    termin: datetime                    # początek próby + 8 s

class RezultatSprawdzeniaPoprawek(BaseModel):   # schemat 10.3.3; sygnał LLM — tylko kierunek bezpieczny (Z-1)
    kroki: list[ZgodnoscKroku]          # dla każdej poprawki
    konfiguracja_modelu_id: UUID

class ZgodnoscKroku(BaseModel):
    nr: int
    p_zgodna_z_poprawka: confloat(ge=0, le=1)   # zapis na zdjęciu odpowiada Poprawce
    p_zgodna_z_odczytem: confloat(ge=0, le=1)   # zapis odpowiada odczytowi agenta
    widoczny_na_zdjeciu: bool                   # dla wstawionego: czy linia istnieje na kartce
```

Przetwarzanie:

1. Normalizacja (w piaskownicy procesowej `api` przed zapisem, ADR-045):
   najpierw odczyt samego nagłówka (`Image.open` bez dekodowania) i
   odrzucenie pliku z szerokością lub wysokością > 12 000 px albo liczbą
   pikseli > 40 000 000 (`Image.MAX_IMAGE_PIXELS = 40_000_000`, ostrzeżenie
   `DecompressionBombWarning` traktowane jako błąd) → `422
   ZDJECIE_NIEPRAWIDLOWE`; potem HEIC/PNG/WEBP → JPEG q=0.85, zastosowanie
   orientacji EXIF, **usunięcie wszystkich metadanych EXIF** (w tym GPS),
   dłuższy bok ≤ 2560 px, odrzucenie duplikatów (SHA-256) w obrębie
   Rozwiązania.
2. Wywołanie `RozpoznawaniePisma.rozpoznaj()` (rola `ROZPOZNAWANIE_PISMA`) z
   promptem wymagającym: przepisania dosłownego (bez poprawiania błędów
   Ucznia), podziału na kroki (krok = jedna linia przekształcenia albo jedno
   zdanie wniosku), LaTeX dla wzorów, oznaczenia niepewnych fragmentów z
   alternatywnymi odczytami, ramki linii każdego kroku (`obszar`) oraz ramek
   danych osobowych. Prompt zawiera regułę: „Nie przepisuj do kroków treści
   z obszarów danych osobowych (imię, nazwisko, podpis, nazwa szkoły,
   klasa, numer PESEL, adres e-mail); zgłoś je wyłącznie jako ramki.”. Tekst
   zapisany na kartce jest danymi, nie instrukcją (ochrona przed
   wstrzykiwaniem poleceń).
3. Post-processing: krok z wzorem, którego LaTeX nie parsuje się w SymPy,
   dostaje `pewnosc_odczytu := min(pewnosc_odczytu, 0.79)`; kroki z
   `pewnosc_odczytu < 0.80` są oznaczane w UI jako „sprawdź odczyt”.
3a. **Filtr danych osobowych (ADR-042, C2-10)** — kod Lemat, deterministyczny,
   wykonywany przed zapisem każdej Transkrypcji (odczytu agenta i Poprawek
   Ucznia, 9.4) oraz ponownie przed utworzeniem kopii treningowej:
   wersja 2.3 (C3-02) — **filtr nigdy nie usuwa kroku i nie zmienia
   `latex`**, więc nie może zmienić treści matematycznej ani numeracji kroków:
   (a) krok, którego `obszar` pokrywa się z ramką danych osobowych w ≥ 50%
   powierzchni obszaru kroku, dostaje `podejrzenie_danych_osobowych = true`
   i jest pokazywany Uczniowi jako „sprawdź odczyt”. Jeżeli krok **nie ma
   treści matematycznej** (`latex` pusty po normalizacji, `tekst` bez cyfr,
   bez znaków `= + − · : / ^ < > ≤ ≥` i bez tokenu ze Słownika terminów
   matematycznych, ≤ 6 tokenów — typowa linia podpisu; **od wersji 2.4
   (C4-03) przed tym sprawdzeniem z `tekst` są wyłączane tokeny równe
   imieniu lub nazwisku z konta (dopasowanie jak w regule b, także gdy token
   jest w Słowniku), inicjały imienia i nazwiska (`J.`, `P.`) oraz oznaczenie
   klasy szkolnej (wyrażenie `(kl\.?|klasa)\s*[1-4]\s*[a-z]{0,3}` i token
   `[1-4][a-z]{1,3}` bezpośrednio po nim)** — linia „Jan Pole, kl. 3b” nie
   ma więc treści matematycznej), jego `tekst` jest
   zastępowany w całości znacznikiem `[DANE_OSOBOWE]` (`tylko_dane_osobowe =
   true`); taki krok pozostaje w Transkrypcji pod swoim numerem, a stan dla
   Modelu decyzyjnego oznacza go „linia z danymi osobowymi — nie jest krokiem
   rozwiązania”. Jeżeli krok **ma** treść matematyczną, jego treść zostaje
   (poza tokenami z reguł b–c), a Transkrypcja dostaje `konflikt_filtra =
   true` → Ocena trafia do Weryfikacji z powodem `PODEJRZENIE_BLEDU_ODCZYTU`
   (11.1 p. 7d), bo sygnał LLM (ramki) nie może obniżyć Oceny bez
   Weryfikacji (Z-1, FR-28); (b) **wyłącznie w `tekst`** tokeny równe imieniu,
   nazwisku albo części lokalnej e-maila Ucznia z konta są zastępowane
   znacznikiem `[DANE_OSOBOWE]` po normalizacji (małe litery, usunięte
   diakrytyki): dla rdzeni ≥ 4 znaków — dopasowanie dokładne albo odległość
   Levenshteina ≤ 1 (rodzaj `PRZYBLIZONE`); dla imion i nazwisk krótszych niż
   4 znaki (Ewa, Jan, Ola, Iza, Ida, Lis) — dopasowanie dokładne całego tokenu
   (rodzaj `DOKLADNE`). **Token ze Słownika terminów matematycznych** (np.
   „pole”, „kula”, „granica”, „suma”, „prosta”, „koło”, „kąt” i ich formy
   odmiany) równy imieniu lub nazwisku Ucznia **jest zastępowany wyłącznie,
   gdy** (wersja 2.4, C4-03): (b1) sąsiaduje (najbliższy token z pominięciem
   znaków interpunkcyjnych, przed nim albo po nim) z tokenem dopasowanym do
   drugiego członu imienia i nazwiska z konta albo z jego inicjałem („Jan
   Pole”, „Pole Jan”, „J. Pole”) — dopasowanie `DOKLADNE`; albo (b2) leży w
   kroku Transkrypcji agenta z `podejrzenie_danych_osobowych = true`
   (wtedy krok ma i tak `konflikt_filtra` albo `tylko_dane_osobowe`, więc
   Ocena idzie do Weryfikacji albo krok nie jest krokiem rozwiązania). W
   pozostałych przypadkach (np. „Pole trójkąta jest równe 12” poza ramką
   danych osobowych) token Słownika nie jest zastępowany, także gdy jest
   równy nazwisku Ucznia; słownik `lemat/rozpoznawanie/slownik_matematyczny.txt` zawiera ≈ 350
   lematów terminów z podstawy programowej i nazw funkcji (sin, cos, log)
   rozwiniętych do wszystkich form odmiany (słownik fleksyjny generowany przy
   budowie obrazu, ≈ 4 000 form); (c) ciągi 11 cyfr (PESEL) i adresy e-mail
   (wyrażenie RFC 5322 uproszczone) w `tekst` są zastępowane tym samym
   znacznikiem; ciąg 11 cyfr w `latex` (np. wynik obliczeń) nie jest
   zmieniany. Pozycje zastąpień trafiają do
   `krok_transkrypcji.zastapienia_danych_osobowych` (bez oryginalnego
   tokenu), liczba — do `transkrypcja.dane_osobowe_usuniete` i metryki
   `dane_osobowe_w_transkrypcji_total{zrodlo,rodzaj}`. **Przywrócenie słowa
   (FR-18):** UI pokazuje znacznik `[DANE_OSOBOWE]` jako element z akcją
   „To nie są moje dane — przywróć”; Uczeń wpisuje słowo, co jest zwykłą
   Poprawką Transkrypcji (wlicza się do limitów 9.4, podlega Sprawdzeniu
   zgodności poprawek ze zdjęciem i podwójnemu odsetkowi Kontroli losowej,
   T-5). W Poprawkach Ucznia filtr stosuje tylko dopasowania `DOKLADNE`;
   dopasowania `PRZYBLIZONE` (Levenshtein = 1) nie są ponownie zastępowane,
   bo Uczeń sam przywrócił słowo. Imię i nazwisko z konta są porównywane po
   stronie Lemat i nigdy nie są wysyłane do modelu (16.3).
   **Działanie filtra na Poprawkach (wersja 2.4, C4-03 p. 3)** — dla każdego
   kroku Transkrypcji `POPRAWKA_UCZNIA`:

   | Krok | Reguła (a) — ramka | Reguła (b) | Reguła (c) | `konflikt_filtra` |
   |---|---|---|---|---|
   | niezmieniony (`zmieniony_przez_ucznia = false`) | nie jest liczona ponownie: `podejrzenie_danych_osobowych`, `tylko_dane_osobowe` i treść kopiowane z kroku agenta | — (treść jak w kroku agenta) | — | liczony jak dla agenta |
   | zmieniony, w tym przywrócony krok `tylko_dane_osobowe` | nie jest stosowana: `podejrzenie_danych_osobowych` kopiowane z kroku agenta, `tylko_dane_osobowe := false`, tekst nie jest ponownie maskowany w całości | `DOKLADNE` z warunkiem (b1); (b2) nie dotyczy kroku zmienionego, więc przywrócone słowo „Pole” zostaje, a „Jan Pole” jest zastępowane | stosowana | krok nie wlicza się (kontrolę przejmuje Sprawdzenie zgodności poprawek i T-7) |
   | wstawiony | brak ramki (`obszar` NULL) | `DOKLADNE` z warunkiem (b1) | stosowana | nie wlicza się |

   Krok przywrócony z `tylko_dane_osobowe` jest zwykłą Poprawką: wlicza się
   do limitów 9.4 (odległość edycyjna liczona od zapisanego tekstu
   `[DANE_OSOBOWE]`, więc cała przywrócona treść jest „zmienionymi znakami”),
   trafia do Sprawdzenia zgodności poprawek z wycinkiem `obszar` i do
   podwójnego odsetka Kontroli losowej (T-5). FR-18 działa więc także dla
   kroku zamaskowanego w całości, a dowolny tekst wpisany w krok w ramce
   przechodzi tę samą kontrolę co każda Poprawka.
4. Maskowanie (ADR-031): dla każdego zdjęcia worker tworzy kopię z
   zamalowanymi na czarno ramkami `obszary_danych_osobowych` (margines 8 px)
   i zapisuje ją pod `klucz_obiektu_maskowany`. Kopię maskowaną widzą
   weryfikatorzy globalni spoza Klasy Ucznia (11.4.4).
5. `czytelne = false` → `BLAD_ROZPOZNAWANIA` z kodem `NIECZYTELNE` (bez
   ponowień). `zgodnosc_z_zadaniem < 0.5` → ostrzeżenie w UI („Kartka może
   dotyczyć innego zadania”), bez blokowania. Oba sygnały pochodzą z LLM i
   działają wyłącznie w kierunku bezpiecznym (Z-1): zatrzymują przetwarzanie
   albo ostrzegają, nie wpływają na punkty.

#### 10.3.2 Schemat kroku (`krok_transkrypcji`, wyjście modelu)

```json
{
  "nr": 2,
  "tekst": "Obliczam wyróżnik",
  "latex": "\\Delta = 8^2 - 4 \\cdot (-2) \\cdot (-3) = 40",
  "pewnosc_odczytu": 0.72,
  "niepewne_fragmenty": [
    { "zdjecie": 1, "ramka": [412, 880, 96, 54], "odczyt": "40", "alternatywy": ["46"] }
  ]
}
```
`ramka` = `[x, y, szerokość, wysokość]` w pikselach znormalizowanego zdjęcia;
UI rysuje ją na podglądzie zdjęcia obok kroku. Wyjście modelu zawiera dla
każdego kroku także `"obszar": {"zdjecie": 1, "ramka": [380, 860, 1420, 96]}`
— ramkę całej linii kroku (typ `ObszarKroku`; filtr 3a, Sprawdzenie zgodności poprawek).

#### 10.3.3 Sprawdzenie zgodności poprawek (ADR-036)

Praca `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` (pula S — od wersji 2.3, C3-05;
poprawka redakcyjna 2.4, C4-06 p. 1; zlecana przez `PUT …/transkrypcja`) wywołuje `RozpoznawaniePisma.sprawdz_poprawki()` z
aktywną konfiguracją roli `ROZPOZNAWANIE_PISMA` i promptem
`sprawdzenie-poprawek-v2` (wersja 2.3, C3-05): model dostaje zdjęcie
poglądowe każdej strony (dłuższy bok 1024 px) oraz dla każdego zmienionego
kroku wycinek `obszar` powiększony o 40 px, odczyt agenta i Poprawkę Ucznia,
a dla każdego kroku wstawionego wycinek obszaru między krokiem agenta
poprzedzającym i następnym (od dolnej krawędzi ramki poprzedniego do górnej
krawędzi ramki następnego, pełna szerokość obu ramek, + 40 px) i treść
wstawioną (bez Rozwiązania wzorcowego i bez informacji, która wersja jest
poprawna matematycznie). Maksymalnie 6 wycinków na wywołanie; więcej
zmienionych kroków przekracza i tak limit miękki (T-7). Rezultat zapisywany w `transkrypcja.zgodnosc_poprawek`:

```json
{ "konfiguracja_modelu_id": "…", "sprawdzono_at": "2026-11-02T17:41:03Z",
  "kroki": [ {"nr": 2, "p_zgodna_z_poprawka": 0.93, "p_zgodna_z_odczytem": 0.05, "widoczny_na_zdjeciu": true},
             {"nr": 5, "p_zgodna_z_poprawka": 0.12, "p_zgodna_z_odczytem": 0.71, "widoczny_na_zdjeciu": true} ],
  "niezgodne": [5] }
```

`niezgodne` = kroki z `p_zgodna_z_poprawka < 0.50` albo (dla wstawionych)
`widoczny_na_zdjeciu = false`. Niepusta lista `niezgodne`, brak rezultatu
przed zapisem Oceny albo przekroczenie limitu miękkiego (9.4) dodaje powód
`POPRAWKI_NIEZGODNE_ZE_ZDJECIEM` (T-7). Rezultat nigdy nie zmienia punktów
ani nie dopuszcza trasy automatycznej (Z-1). Skuteczność mierzy G-2:
na podzbiorze 200 Rozwiązań ZT-1 z syntetycznymi Poprawkami „zastępującymi”
(krok przepisany na poprawny krok Rozwiązania wzorcowego) wykrywalność
≥ 95%; na 200 Rozwiązaniach ZT-1 z syntetycznymi **„wstawieniami
rozwiązania”** (1–2 kroki Rozwiązania wzorcowego nieobecne na kartce,
wstawione w granicach limitów twardych) wykrywalność łącznie z limitami
(T-7 albo `422`) ≥ 95% (C3-05); na 200 rzeczywistych Poprawkach błędów
odczytu odsetek fałszywych alarmów ≤ 5% (wersja 2.2: ≤ 10%; zaostrzenie
wynika z modelu T-7 w 17.7).

### 10.4 Agent oceniający

#### 10.4.1 Kontrakt

```python
class KontekstOceny(BaseModel):
    rozwiazanie_id: UUID
    uczen_id: UUID                                    # tylko do zapisu decyzja_modelu (RODO); nie trafia do stanu
    rodzaj: RodzajZadania                             # OTWARTE_OBLICZENIOWE | OTWARTE_Z_DOWODEM
    tresc_md: str
    rozwiazanie_wzorcowe: RozwiazanieWzorcowe
    wynik_koncowy_wzorcowy: WynikKoncowyWzorcowy | None
    kryteria: list[KryteriumOcenianiaDto]
    kroki: list[KrokTranskrypcjiDto]                  # zatwierdzona Transkrypcja
    kroki_zmienione_przez_ucznia: set[int]
    prog: ProgPewnosciDto                             # migawka: id, wartosc
    kalibrator: KalibratorPewnosciDto                 # migawka: id, punkty
    odsetek_kontroli_losowej: float
    termin_miekki: datetime                           # 9.5
    termin_twardy: datetime                           # zatwierdzono_at + 120 s

class RezultatOceny(BaseModel):
    wariant: int
    punkty_kryteriow: dict[int, int]                  # nr_kryterium → punkty
    prawdopodobienstwa_kryteriow: dict[int, float]
    punkty_surowe: int                                # Σ pk przed regułą FR-20
    punkty: int
    punkty_max: int
    pierwszy_bledny_krok: int | None                  # nr kroku Transkrypcji
    brakujacy_krok: bool
    typ_bledu: TypBledu | None
    krok_podejrzany_odczyt: int | None
    mozliwy_blad_odczytu: bool
    pewnosc_surowa: float
    pewnosc_oceny: float
    niespojnosci: list[Literal["N-1","N-2","N-3","N-4","N-5","N-6"]]
    trasa: Literal["AUTOMATYCZNA","WERYFIKACJA"]
    powody: list[PowodWeryfikacji]                    # pusta dla czysto automatycznej
    uzasadnienie: str                                 # szablon 11.7
    konfiguracja_modelu_id: UUID
    decyzje: list[DecyzjaSkladowa]
```

#### 10.4.2 Stan przekazywany do modelu decyzyjnego

Stan to tekst o stałej strukturze sekcji (wersja szablonu w
`konfiguracja_modelu.wersja_promptu`). Wzory podawane są w dwóch postaciach:
LaTeX i postać liniowa SymPy (gdy parsowanie się udało) — odpowiedź na
otwarte pytanie o LaTeX w Jev (sekcja 20, eksperyment E-01). Sekcja
`[NIEPEWNE_ODCZYTY]` przekazuje sygnały Agenta rozpoznającego jako dane do
pytań Q-OC-04 i Q-OC-06 (Z-1: decyzję podejmuje Model decyzyjny).

```text
[ZADANIE]
rodzaj: OTWARTE_OBLICZENIOWE
tresc: Wyznacz największą wartość funkcji $f(x)=-2x^2+8x-3$.
[ROZWIAZANIE_WZORCOWE wariant=1]
1. f(x) = -2x^2 + 8x - 3 | -2*x**2 + 8*x - 3
2. p = -b/(2a) = 2 | Eq(p, 2)
3. f(2) = 5 | Eq(f(2), 5)
[KRYTERIA_OCENIANIA wariant=1]
K1 (1 pkt): Obliczenie odciętej wierzchołka paraboli.
K2 (1 pkt, wynik końcowy): Podanie największej wartości funkcji.
[ROZWIAZANIE_UCZNIA]
krok 1: tekst="" latex="p=\frac{-8}{-4}=2" | Eq(p, 2)
krok 2: tekst="wartość największa" latex="f(2)=-8+16-3=6" | Eq(f(2), 6)
[NIEPEWNE_ODCZYTY]
krok 2: pewnosc_odczytu=0.74, fragment "6", alternatywy: "5"; niezmieniony przez Ucznia
[SPRAWDZENIE_SYMBOLICZNE]
krok 1->2: NIEROZSTRZYGNIETE
wynik_koncowy_ucznia vs wzorcowy: NIEROWNOWAZNE
```

#### 10.4.3 Katalog pytań do modelu decyzyjnego

| Kod | Typ | Kiedy zadawane (warunek w planie) | Runda Jev | Pytanie | Odpowiedzi |
|---|---|---|---|---|---|
| Q-OC-00 | CHOICE | więcej niż 1 wariant | 1 | „Którym sposobem (wariantem) Uczeń rozwiązuje zadanie?” | `W1` … `Wn`, `ZADEN` |
| Q-OC-01 | NOUL | `OTWARTE_OBLICZENIOWE` | 1 | „Czy wynik końcowy Ucznia jest równoważny wynikowi wzorcowemu?” | tak / nie |
| Q-OC-02.v.Kk | SCORE | każde kryterium wariantu v; warunek: wariant wybrany = v (gdy 1 wariant — bezwarunkowe, runda 1) | 2 | „Ile punktów przyznać za kryterium K{k}: „{opis}”?” | 0 … `punkty_max` kryterium |
| Q-OC-03 | CHOICE | zawsze | 1 | „W którym kroku Rozwiązania Ucznia występuje pierwszy błąd?” | `BRAK_BLEDU`, `KROK_1` … `KROK_n`, `BRAK_WYMAGANEGO_KROKU` |
| Q-OC-04.j | CHOICE | warunek: moda Q-OC-03 = `KROK_j` | 2 | „Jaki jest typ błędu w kroku j?” | `RACHUNKOWY`, `MERYTORYCZNY`, `NIEPELNE_ROZWIAZANIE`, `BLAD_ODCZYTU` |
| Q-OC-05.i | NOUL | para (i, i+1) z wynikiem SymPy `NIEROZSTRZYGNIETE` lub `NIEROWNOWAZNE`; maks. 12 par, od początku; więcej par → niespójność N-6 | 1 | „Czy krok i+1 wynika poprawnie z kroku i?” | tak / nie |
| Q-OC-06 | NOUL | zawsze | 1 | „Czy w Rozwiązaniu Ucznia występuje zapis, który najpewniej jest błędem odczytu pisma (niezgodny z sąsiednimi krokami w sposób typowy dla pomyłki odczytu, np. 5↔6, 1↔7, + ↔ −), a nie błędem Ucznia?” | tak / nie |
| Q-RE-01 | CHOICE | rekomendacja dla Ucznia z grupy eksperymentu strategii `JEV_CHOICE` (10.1.6) | — | „Jaki następny krok nauki jest najlepszy dla Ucznia?” | `WKLADKA`, `ZADANIE_LATWE`, `ZADANIE_MATURALNE`, `POWTORKA` |
| Q-WA-01…05 | NOUL/SCORE | Walidacja zadania | 1 | 10.2.2 | 10.2.2 |

Wybraną wartością każdego pytania jest **moda rozkładu**; prawdopodobieństwo
decyzji to masa rozkładu w modzie. Wyjątek: „wariant wybrany” =
`argmax` rozkładu Q-OC-00 po opcjach `W1…Wn` (z pominięciem `ZADEN`); gdy
moda to `ZADEN`, rejestrowana jest niespójność N-5. Liczba pytań do Jev na
Ocenę: ≤ 1 + 1 + 12 + 1 + 1 + 12 + 1 = **29**, w **2 rundach** (runda 1:
pytania bezwarunkowe; runda 2: pytania, których warunek jest spełniony).
Skład rund (C2-07):

| Przypadek | Runda 1 (pytania) | Runda 2 (pytania) | Tryb wsadowy (A-01): żądania HTTP | Tryb pojedynczy: fale po ≤ 16 |
|---|---|---|---|---|
| 1 wariant, 12 kryteriów, 12 par | Q-OC-01, 12 × Q-OC-02, Q-OC-03, 12 × Q-OC-05, Q-OC-06 = **27** (+ Q-OC-00 nie występuje) | Q-OC-04.j = **1** | 2 | ⌈27/16⌉ + 1 = **3** |
| > 1 wariant (łącznie ≤ 24 kryteria, wariant ≤ 12), 12 par | Q-OC-00, Q-OC-01, Q-OC-03, 12 × Q-OC-05, Q-OC-06 = **16** | ≤ 12 × Q-OC-02 + Q-OC-04.j = **13** | 2 | 1 + 1 = **2** |
| Najgorszy przypadek budżetu | — | — | 2 żądania | **3 fale** (p95 fali 1,2 s = max z 16 wywołań) |

W trybie wsadowym Jev dostaje jedno żądanie na rundę (≤ 32 pytania); w
trybie pojedynczym — jedno żądanie na pytanie, z limitem 16 równoległych
wywołań na Ocenę.

#### 10.4.4 Plan pytań Oceny (ADR-026)

```python
plan = PlanPytan(pytania=[
    PytanieWPlanie(pytanie=Q_OC_00, warunek=None),                       # gdy > 1 wariant
    PytanieWPlanie(pytanie=Q_OC_01, warunek=None),                       # gdy obliczeniowe
    *[PytanieWPlanie(pytanie=q_oc_02(v, k),
                     warunek=WarunekPytania(kod_nadrzedny="Q-OC-00", wartosci=[f"W{v}"], pomin_opcje=["ZADEN"])
                             if liczba_wariantow > 1 else None)
      for v, k in kryteria_wszystkich_wariantow],
    PytanieWPlanie(pytanie=Q_OC_03, warunek=None),
    *[PytanieWPlanie(pytanie=q_oc_04(j),
                     warunek=WarunekPytania(kod_nadrzedny="Q-OC-03", wartosci=[f"KROK_{j}"], pomin_opcje=[]))
      for j in range(1, n + 1)],
    *[PytanieWPlanie(pytanie=q_oc_05(i), warunek=None) for i in pary_do_sprawdzenia[:12]],
    PytanieWPlanie(pytanie=Q_OC_06, warunek=None),
])
```

Plan jest jedynym kontraktem między Agentem oceniającym a Modelem
decyzyjnym: Agent nie wie, czy plan zostanie wykonany w dwóch rundach (Jev),
czy jednym wywołaniem (model zapasowy, 13.2). Zależności mają głębokość 1.

#### 10.4.5 Opinia pomocnicza (architecture.md: „druga ocena przez mocniejszy LLM”)

Praca `OPINIA_POMOCNICZA` (tylko gdy wśród powodów jest `NISKA_PEWNOSC`)
wywołuje rolę `GENERATYWNY_OPINIA_POMOCNICZA` z tym samym stanem i
schematem odpowiedzi. Wynik trafia do `weryfikacja.opinia_pomocnicza`:

```json
{
  "konfiguracja_modelu_id": "…",
  "punkty": 1,
  "kryteria": [ {"nr_kryterium": 1, "punkty": 1}, {"nr_kryterium": 2, "punkty": 0} ],
  "pierwszy_bledny_krok": 2,
  "typ_bledu": "RACHUNKOWY",
  "komentarz": "Odcięta wierzchołka poprawna. W kroku 2 błąd rachunkowy przy obliczaniu f(2).",
  "zgodna_z_ocena_weryfikowana": false,
  "created_at": "2026-10-31T19:22:04Z"
}
```
Opinia pomocnicza **nie zmienia** Oceny ani trasy (ADR-008, Z-1). Zgodność
opinii z Oceną Nauczyciela jest mierzona, aby w przyszłości ocenić, czy
zespół „Jev + opinia pomocnicza” może dostać własny kalibrator.

### 10.5 Agent wyjaśniający

#### 10.5.1 Kontrakt

```python
class WejscieWskazowki(BaseModel):
    tresc_md: str
    rozwiazanie_wzorcowe: RozwiazanieWzorcowe
    wynik_koncowy_wzorcowy: WynikKoncowyWzorcowy | None
    kroki: list[KrokTranskrypcjiDto]
    pierwszy_bledny_krok: int | None
    brakujacy_krok: bool
    typ_bledu: TypBledu
    kryteria_bez_punktow: list[str]                   # opisy kryteriów z pk < punkty_max (dla N-1 i brakującego kroku)
    umiejetnosc_nazwa: str
    punkty_kryteriow: dict[int, int]                  # nr_kryterium → punkty

class Wskazowka(BaseModel):
    tekst: constr(max_length=400)
    zrodlo: Literal["AGENT_WYJASNIAJACY","SZABLON"]
```

Agent działa w pracy w tle `NAPISZ_WSKAZOWKE` **po** zapisaniu Oceny, poza
ścieżką krytyczną NFR-01 (ADR-025). Wskazówka jest tworzona dla każdej Oceny
z `typ_bledu ≠ NULL`, także dla Oceny Nauczyciela w trybie
`AUTOMATYCZNA` (9.6.2) i dla Oceny z niespójnością N-1 (11.1 p. 6). Nie jest
tworzona dla Oceny wstępnej z `mozliwy_blad_odczytu = true` — wskazówka do
błędu, który może nie istnieć, byłaby myląca (ADR-022); powstaje po
Weryfikacji. Dla Rozwiązania bez błędu pole `wskazowka` ma wartość `NULL`, a
UI wyświetla komunikat „Rozwiązanie kompletne”. Po wyczerpaniu prób pracy
zapisywany jest szablon (10.5.3 p. 4), więc każda Ocena z błędem dostaje
Wskazówkę.

#### 10.5.2 Zasady promptu

- Odnosi się do numeru kroku (`pierwszy_bledny_krok`) albo — przy brakującym
  kroku — do opisu kryterium bez punktów, nazywa typ błędu, sugeruje, co
  sprawdzić; maks. 400 znaków; język polski; forma bezosobowa lub 2. osoba
  liczby pojedynczej bez form rodzajowych.
- Nie podaje wyniku końcowego ani kolejnych kroków Rozwiązania wzorcowego.

#### 10.5.3 Strażnik wycieku rozwiązania (FR-27)

1. Z tekstu wyodrębniane są fragmenty `$…$` i liczby.
2. Każdy fragment parsowany SymPy jest porównywany z
   `wynik_koncowy_wzorcowy` (oraz każdym elementem zbioru rozwiązań) i z
   dwoma ostatnimi krokami Rozwiązania wzorcowego wybranego wariantu.
   Równoważność = wyciek.
3. Wyciek lub tekst > 400 znaków → jedno ponowienie z dopiskiem „Nie podawaj
   żadnych wartości liczbowych wyniku” → przy kolejnym niepowodzeniu szablon.
4. Szablony (`zrodlo = SZABLON`):
   - `RACHUNKOWY`: „Sprawdź jeszcze raz obliczenia w kroku {n}.”
   - `MERYTORYCZNY`: „W kroku {n} zastosowano niewłaściwą metodę lub własność. Przypomnij sobie: {umiejetnosc}.”
   - `NIEPELNE_ROZWIAZANIE` (brakujący krok): „Rozwiązanie jest niepełne — brakuje etapu: „{kryterium}”. Sprawdź, czy ten etap jest zapisany na kartce ({umiejetnosc}).”

---

## 11. Reguły decyzyjne oceniania i kierowania do Weryfikacji

Reguły 11.1–11.3 są wykonywane w podanej kolejności przez czystą funkcję
`lemat.ocenianie.reguly.ocen_decyzje(decyzje, kontekst) -> RezultatOceny`
(testy jednostkowe dla każdej reguły i każdej niespójności).

### 11.1 Punkty i błędy (FR-19, FR-20, FR-21, FR-27, FR-28)

1. **Wariant** = 1, gdy Kryteria oceniania mają jeden wariant; w przeciwnym
   razie wariant wybrany = `argmax` rozkładu Q-OC-00 po `W1…Wn` (bez
   `ZADEN`). Moda Q-OC-00 = `ZADEN` → niespójność **N-5**; punkty są liczone
   dla wariantu wybranego, a `ocena.wariant` przyjmuje jego numer (kolumna
   pozostaje `NOT NULL`). Nauczyciel w Weryfikacji może zmienić wariant.
2. Dla kryterium `k` wybranego wariantu: `pk = moda(Q-OC-02.v.Kk)`.
3. **Zależność:** jeśli `wymaga_nr_kryterium = r` i `pr = 0`, to `pk = 0`.
4. `punkty_surowe = Σ pk` po kroku 3 (zapisywane w `ocena.punkty_surowe`).
   Niespójności N-1 i N-2 są liczone **na `punkty_surowe`**, czyli przed
   regułą FR-20 z p. 5 (C1-15).
5. **FR-20:** dla kryterium z `czy_wynik_koncowy = true`: `pk = 0`, jeżeli
   (a) moda Q-OC-03 = `BRAK_WYMAGANEGO_KROKU`, lub (b) moda Q-OC-03 =
   `KROK_j` i moda Q-OC-04.j ≠ `BLAD_ODCZYTU`, lub (c) moda Q-OC-01 = nie.
   Punkty za wynik przyznawane są wyłącznie, gdy wynik wynika z rozumowania
   bez błędu Ucznia. Podejrzany błąd odczytu (moda Q-OC-04.j =
   `BLAD_ODCZYTU`) **nie** zeruje punktów za wynik (FR-28).
6. **Błędy** (`punkty = Σ pk` po p. 5):

   | Moda Q-OC-03 | Moda Q-OC-04.j | `pierwszy_bledny_krok` | `brakujacy_krok` | `typ_bledu` | `krok_podejrzany_odczyt` |
   |---|---|---|---|---|---|
   | `BRAK_BLEDU`, `punkty_surowe = punkty_max` | — | NULL | false | NULL | NULL |
   | `BRAK_BLEDU`, `punkty_surowe < punkty_max` (N-1) | — | NULL | **true** | `NIEPELNE_ROZWIAZANIE` | NULL |
   | `BRAK_WYMAGANEGO_KROKU` | — | NULL | true | `NIEPELNE_ROZWIAZANIE` | NULL |
   | `KROK_j` | `RACHUNKOWY` / `MERYTORYCZNY` / `NIEPELNE_ROZWIAZANIE` | j | false | moda Q-OC-04.j | j, gdy p(`BLAD_ODCZYTU`) ≥ 0.20, inaczej NULL |
   | `KROK_j` | `BLAD_ODCZYTU` | NULL | false | NULL | j |

   Przy N-1 system nie zgaduje kroku: wskazuje, że brakuje etapu wymaganego
   przez nieprzyznane kryterium (Wskazówka cytuje opis kryterium, 10.5.3), co
   spełnia FR-27 (wskazanie, typ błędu, wskazówka).
7. **Podejrzenie błędu odczytu** (FR-28, ADR-022):
   `mozliwy_blad_odczytu = true`, gdy zachodzi którykolwiek warunek:
   (a) Q-OC-04.j zadane i p(`BLAD_ODCZYTU`) ≥ τ_odczyt = 0.20;
   (b) p_tak(Q-OC-06) ≥ τ_odczyt;
   (c) krok `pierwszy_bledny_krok` lub `krok_podejrzany_odczyt` ma
   `pewnosc_odczytu < 0.80` i nie był zmieniony przez Ucznia;
   (d) (wersja 2.3, C3-02) zatwierdzona Transkrypcja ma `konflikt_filtra =
   true` — wtedy `krok_podejrzany_odczyt` = najmniejszy numer kroku
   niezmienionego przez Ucznia z `podejrzenie_danych_osobowych` i treścią
   matematyczną (po wyłączeniu tokenów imienia, nazwiska, inicjałów i
   oznaczenia klasy, 10.3.1 p. 3a, C4-03).
   Warunki (a) i (b) to decyzje Modelu decyzyjnego; (c) i (d) to sygnały LLM
   (pewność odczytu, ramki danych osobowych), które wyłącznie dodają powód
   Weryfikacji (Z-1). Kroki zmienione przez Ucznia są
   wyłączone z (c), bo nie mają `pewnosc_odczytu`; ich zgodność ze zdjęciem
   sprawdza reguła T-7 (11.3, ADR-036), więc Poprawka nie może ominąć
   kontroli odczytu. Uzasadnienie τ_odczyt = 0.20:
   Próg pewności ≥ 0.80, więc decyzja z prawdopodobieństwem przeciwnej opcji
   ≥ 0.20 i tak nie mogłaby przejść automatycznie; reguła jedynie nadaje jej
   właściwy powód i priorytet oraz chroni Ucznia przed obniżeniem Oceny.
8. **FR-21 (wynikanie kroków):** wynik SymPy i Q-OC-05.i trafiają do stanu
   Q-OC-03 (`[SPRAWDZENIE_SYMBOLICZNE]`) oraz do niespójności N-3. Każda para
   kroków jest sprawdzona: symbolicznie (`ROWNOWAZNE`) albo pytaniem
   Q-OC-05.i; gdy par wymagających pytania jest więcej niż 12, niesprawdzone
   pary dają niespójność **N-6**, która wymusza Weryfikację (11.3 T-4). Brak
   sprawdzenia nigdy nie przechodzi trasą automatyczną.

### 11.2 Pewność oceny (FR-22, NFR-04)

#### 11.2.1 Pewność surowa i niespójności

`pewnosc_surowa = min(p(Q-OC-00), p(Q-OC-01), min_k p(Q-OC-02.v.Kk), p(Q-OC-03), p(Q-OC-04.j))`
(tylko pytania faktycznie zadane; dla Q-OC-00 — masa wariantu wybranego).
Każda wykryta niespójność ustawia `pewnosc_surowa := min(pewnosc_surowa, 0.50)`
**i niezależnie od kalibratora** dodaje powód `NISKA_PEWNOSC` (11.3 T-4), bo
kalibrator mógłby przekształcić 0.50 w wartość powyżej progu:

| Kod | Niespójność |
|---|---|
| N-1 | moda Q-OC-03 = `BRAK_BLEDU`, a `punkty_surowe < punkty_max` |
| N-2 | moda Q-OC-03 ∈ {`KROK_j` z typem ≠ `BLAD_ODCZYTU`, `BRAK_WYMAGANEGO_KROKU`}, a `punkty_surowe = punkty_max` |
| N-3 | moda Q-OC-03 = `BRAK_BLEDU`, a któreś Q-OC-05.i = nie (`p_nie ≥ 0.5`) |
| N-4 | SymPy rozstrzygnął równoważność wyniku końcowego odwrotnie niż moda Q-OC-01 |
| N-5 | moda Q-OC-00 = `ZADEN` (Uczeń użył sposobu spoza Kryteriów oceniania) |
| N-6 | więcej niż 12 par kroków wymagało pytania Q-OC-05 (wynikanie części kroków niesprawdzone) |

#### 11.2.2 Kalibracja

`pewnosc_oceny = kalibrator(pewnosc_surowa)` — funkcja monotoniczna
odcinkami liniowa z regresji izotonicznej, osobna dla każdej pary
(`konfiguracja_modelu_id`, `rodzaj_zadania`), przechowywana w
`kalibrator_pewnosci.punkty`:

```json
{ "typ": "IZOTONICZNY", "punkty": [[0.00, 0.12], [0.50, 0.41], [0.80, 0.86], [0.95, 0.985], [1.00, 0.995]] }
```

Interpolacja liniowa między punktami, wartości poza zakresem — przycięte do
skrajnych punktów. `pewnosc_oceny` estymuje prawdopodobieństwo **Zgodności
Oceny** (ADR-027). Przed pierwszą kalibracją z ZT-1 (A-09) obowiązuje
kalibrator `TOZSAMOSCIOWY` `[[0,0],[1,1]]` i progi 0.95 (Jev) / 0.98 (model
zapasowy) — wyłącznie w środowisku testowym; start produkcyjny wymaga
kalibratora z ZT-1 (bramka G-1).

### 11.3 Trasowanie (FR-23 … FR-26, FR-28, FR-33)

Reguły są oceniane wszystkie; powody są sumowane.

| Reguła | Warunek | Powód | Wymaganie |
|---|---|---|---|
| T-1 | `rodzaj = OTWARTE_Z_DOWODEM` | `ZADANIE_Z_DOWODEM` | FR-26 |
| T-2 | `rodzaj = OTWARTE_OBLICZENIOWE` **i** (SymPy: wynik końcowy `ROWNOWAZNE` **lub** p_tak(Q-OC-01) ≥ τ_FR25) **i** (moda Q-OC-03 ≠ `BRAK_BLEDU` **lub** p(Q-OC-03 ≠ `BRAK_BLEDU`) ≥ τ_FR25 **lub** istnieje kryterium nie-końcowe z `pk < punkty_max`) | `POPRAWNY_WYNIK_BLEDNE_KROKI` | FR-25 |
| T-3 | `mozliwy_blad_odczytu` (11.1 p. 7) | `PODEJRZENIE_BLEDU_ODCZYTU` | FR-28 |
| T-4 | `rodzaj = OTWARTE_OBLICZENIOWE` **i** (`pewnosc_oceny ≤ prog.wartosc` **lub** `niespojnosci ≠ ∅`) — automatyczna tylko, gdy pewność **przekracza** próg i nie ma niespójności | `NISKA_PEWNOSC` | FR-23, FR-24 |
| T-7 | Transkrypcja zatwierdzona ma `zrodlo = POPRAWKA_UCZNIA` **i** (Sprawdzenie zgodności poprawek ma niepustą listę `niezgodne` **lub** brak jego rezultatu przed zapisem Oceny **lub** przekroczony limit miękki zakresu poprawek, 9.4, **lub** aktywny bezpiecznik poprawek dla użytej konfiguracji decyzyjnej — `stan_bezpiecznika.aktywny`, typ `POPRAWKI`, ADR-046) | `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM` | FR-18, FR-23 (ADR-036) |
| T-5 | brak powodów T-1…T-4 i T-7 **i** `(fnv1a(rozwiazanie_id) mod 10000) < odsetek_kontroli_efektywny × 10000`, gdzie `odsetek_kontroli_efektywny = min(1, 2 × odsetek_kontroli)` dla Transkrypcji z Poprawkami i `odsetek_kontroli` w pozostałych przypadkach | `KONTROLA_LOSOWA` (Ocena `OSTATECZNA`) | NFR-03, NFR-04 |
| T-6 | termin twardy (`zatwierdzono_at + 120 s`) minął lub wyczerpano ponowienia (14.2) | `BLAD_PRZETWARZANIA` (bez Oceny wstępnej) | Z-3 |

**τ_FR25 = 0.20** (parametr 22). Uzasadnienie: FR-25 wymaga Weryfikacji
„niezależnie od pewności oceny”, więc decyzja o FR-25 nie może zależeć od
tego, czy jedna odpowiedź modelu przekroczyła 0.5. Warunek T-2 jest
spełniony, gdy **którykolwiek** sygnał (SymPy albo co najmniej 20%
prawdopodobieństwa modelu) wskazuje poprawny wynik i którykolwiek wskazuje
błędny lub brakujący krok. Ponieważ Próg pewności ≥ 0.80, przypadki z
prawdopodobieństwem przeciwnej odpowiedzi ≥ 0.20 i tak nie byłyby zaliczone
automatycznie (T-4) — T-2 nie zwiększa więc liczby Weryfikacji, tylko
gwarantuje, że rozwiązanie z poprawnym wynikiem i błędnym krokiem zawsze
trafi do Nauczyciela z właściwym powodem i priorytetem 2, także gdy
kalibrator lub próg zmienią się w przyszłości.

Rezultat trasowania: jeśli są powody z T-1…T-4 lub T-7 → Ocena `WSTEPNA`, Rozwiązanie
`OCZEKUJE_NA_WERYFIKACJE`, Uczeń widzi Ocenę wstępną z adnotacją powodu
(FR-24; przy T-3 z wstrzymanymi punktami, ADR-022). T-6 → Rozwiązanie
`OCZEKUJE_NA_WERYFIKACJE` bez Oceny. W przeciwnym razie Ocena `OSTATECZNA`,
Rozwiązanie `OCENIONE` (FR-23); przy T-5 dodatkowo Weryfikacja typu Kontrola
losowa.

**Priorytet** Weryfikacji = minimum z: `BLAD_PRZETWARZANIA` → 1;
`POPRAWNY_WYNIK_BLEDNE_KROKI`, `PODEJRZENIE_BLEDU_ODCZYTU`,
`ZGLOSZENIE_BLEDU_ODCZYTU`, `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM` → 2; `NISKA_PEWNOSC`, `ZADANIE_Z_DOWODEM`,
`KONTROLA_LOSOWA` → 3. Kolejność w Kolejce weryfikacji: `priorytet`, potem
`termin_at` (najbliższy SLA pierwszy). Kontrola losowa ma ten sam priorytet
co zwykłe Weryfikacje i własny termin (C1-10).

**Komunikaty dla Ucznia** (`OcenaWyjscie.komunikat`):

| Sytuacja | Komunikat |
|---|---|
| Ocena wstępna, `widocznosc_dla_ucznia = PELNA` | „Ocena wstępna — rozwiązanie czeka na sprawdzenie przez nauczyciela.” |
| Ocena wstępna, `WSTRZYMANA_BLAD_ODCZYTU` | „Ocena wstrzymana — możliwy błąd odczytu pisma{ w kroku {n}}. Nauczyciel porówna rozwiązanie ze zdjęciem. Błąd odczytu nie obniży Twojej oceny.” |
| T-6 (brak Oceny) | „Rozwiązanie zostanie ocenione przez nauczyciela.” |
| Ocena wstępna z powodem T-7 | „Ocena wstępna — nauczyciel porówna Twoje poprawki odczytu ze zdjęciem kartki.” |
| otwarte Zgłoszenie błędu odczytu (11.6) | „Zgłoszenie błędu odczytu jest sprawdzane przez nauczyciela. Do tego czasu ta ocena nie wpływa na Twój profil.” |
| Weryfikacja po terminie SLA (`opozniona`) | dopisek: „Sprawdzenie trwa dłużej niż zwykle — przepraszamy za opóźnienie.” |

### 11.4 Kolejka weryfikacji

#### 11.4.1 Przydział

1. Rozwiązanie z `przypisanie_id` (także odziedziczonym z elementu
   `R-PRZYPISANIE`, ADR-032) → `typ_kolejki = KLASA`, Nauczyciel
   Przypisania, `klasa_id` Przypisania; gdy Przypisanie nie ma już
   Nauczyciela (`nauczyciel_id = NULL`, ADR-044) — reguła p. 2.
2. W przeciwnym razie, jeśli Uczeń należy do niezarchiwizowanej Klasy →
   Klasa z najpóźniejszym `dolaczono_at`, jej Nauczyciel.
3. W przeciwnym razie → `typ_kolejki = GLOBALNA` (Nauczyciele z
   `weryfikator_globalny = true`).
4. `termin_at = dodaj_dni_robocze(created_at, 3)` — SLA Weryfikacji: 3 dni
   robocze, wszystkie powody (A-15, ADR-043); np. Weryfikacja z piątku 18:00
   ma termin w środę 18:00, a nie w poniedziałek.

#### 11.4.2 Dostępność i blokady

- Weryfikacja jest **dostępna**, gdy `status = OCZEKUJE` albo `status =
  W_TRAKCIE AND zablokowana_do < now()` (reguła odczytu w każdym zapytaniu
  kolejki i w SQL podjęcia, 9.6.1).
- Blokada: 15 min od podjęcia lub przedłużenia, łącznie ≤ 2 h od
  `podjeta_at` (także przy ponownym podjęciu przez właściciela, 9.6.1); PWA
  przedłuża ją co 5 min, gdy karta jest aktywna. Żadna operacja na blokadzie
  nie zmienia `version` (ADR-037).
- `ZWOLNIJ_BLOKADY_WERYFIKACJI` (scheduler co 1 min): `UPDATE weryfikacja
  SET status = 'OCZEKUJE', podjeta_przez = NULL, podjeta_at = NULL,
  zablokowana_do = NULL WHERE status = 'W_TRAKCIE' AND zablokowana_do <
  now()` (bez zmiany `version`).
- Weryfikacje Uczniów z kontem `USUWANE` (okres karencji, ADR-040) nie są
  dostępne: zapytania kolejki i SQL podjęcia wymagają `uzytkownik.status =
  'AKTYWNE'` Ucznia.
- Uprawnieni do podjęcia: Nauczyciel docelowy, Nauczyciel prowadzący Klasę
  z `klasa_id` oraz — dla `GLOBALNA` — każdy `weryfikator_globalny`.

#### 11.4.3 Eskalacja, SLA i kontrola przyjęć (ADR-021, ADR-033)

`ESKALUJ_WERYFIKACJE` (co godzinę) dla Weryfikacji dostępnych (11.4.2):

1. `typ_kolejki = KLASA` i `now() > termin_at` albo Nauczyciel docelowy
   zablokowany lub w okresie karencji usunięcia konta (`USUWANE`) →
   `typ_kolejki = GLOBALNA`, `eskalowano_at = now()`, `powod_eskalacji` ∈
   {`PRZEKROCZENIE_SLA`, `NAUCZYCIEL_DOCELOWY_ZABLOKOWANY`,
   `KARENCJA_KONTA_NAUCZYCIELA`} (C3-08), `termin_at =
   dodaj_dni_robocze(now(), 3)` (`nauczyciel_docelowy_id` pozostaje, by
   Nauczyciel nadal ją widział; po usunięciu konta przyjmuje `NULL`).
   Eskalacja nie zmienia `version` (ADR-037).
2. `typ_kolejki = GLOBALNA` i `now() > termin_at` (naruszenie SLA) →
   Weryfikacja jest oznaczana jako `opozniona` (wartość wyliczana:
   `now() > termin_at`), Uczeń dostaje jednorazowo powiadomienie
   `WERYFIKACJA_OPOZNIONA`, Administratorzy — `ALERT_WERYFIKACJE`, metryka
   `weryfikacje_po_sla{typ_kolejki}`. **Ocena wstępna nie staje się
   ostateczna automatycznie** — wymagałoby to złamania FR-24/FR-26 („zawsze
   do weryfikacji”); decyzja ta nie jest delegowana na właściciela produktu
   jako opcja (C1-02).
3. Weryfikacja otwarta, gdy najstarsze zdjęcie Rozwiązania ma ≥ 80 dni
   (zdjęcia zostaną usunięte po 90 dniach) → `priorytet := 1`, alert
   krytyczny `ALERT_WERYFIKACJE` dla Administratorów. Gdy zdjęcia zostaną
   usunięte przed rozstrzygnięciem Weryfikacji z powodem
   `PODEJRZENIE_BLEDU_ODCZYTU` lub `ZGLOSZENIE_BLEDU_ODCZYTU`, Nauczyciel
   rozstrzyga na podstawie Transkrypcji, a kroki wskazane jako podejrzane
   (`krok_podejrzany_odczyt`, `zgloszenie.kroki`) są oceniane na korzyść
   Ucznia: dla kryteriów zależnych od tych kroków PWA wstępnie wypełnia
   punkty z interpretacji korzystniejszej dla Ucznia (alternatywny odczyt z
   `niepewne_fragmenty` albo poprawka zgłoszona przez Ucznia); Nauczyciel może
   to zmienić, ale `komentarz` jest wtedy wymagany w każdym rozstrzygnięciu
   (12.5) (FR-28: brak dowodu, że to błąd Ucznia, nie obniża Oceny).

**Kontrola przyjęć kolejki globalnej:** `zaleglosc_globalna_min` = Σ
szacowanego czasu (A-13) dostępnych Weryfikacji `GLOBALNA` — **łącznie z
eskalowanymi z Klas** (`eskalowano_at IS NOT NULL`), co pokazuje osobno
metryka `zaleglosc_globalna_min{zrodlo="ESKALACJA"|"BEZ_KLASY"}`;
`pojemnosc_globalna_min` = Σ `deklarowane_minuty_weryfikacji` weryfikatorów
globalnych aktywnych w ostatnich 7 dniach, liczona **na dzień roboczy**
(w sobotę, niedzielę i dzień wolny pojemność = 0, a zaległość nie
uruchamia kontroli przyjęć, jeśli `zaleglosc_globalna_min ≤ 2 ×
pojemnosc_globalna_min` następnego dnia roboczego). Gdy
`zaleglosc_globalna_min > 2 × pojemnosc_globalna_min`, dla Uczniów bez Klasy
Agent rekomendujący dostaje `dowody_dozwolone = false` i
`udzial_zadan_otwartych = 5` (co piąty element zadaniowy otwarty), a
uzasadnienie dopisuje „Teraz proponujemy więcej zadań zamkniętych, bo
sprawdzanie rozwiązań przez nauczycieli trwa dłużej.”. Ograniczenie
znika, gdy `zaleglosc_globalna_min < pojemnosc_globalna_min`. Uczeń nadal
może rozwiązać dowolne Zadanie z Przypisania.

**Kontrola przyjęć per Klasa (napływ eskalacji, C2-12):** dla każdego
Nauczyciela prowadzącego Klasę `odsetek_eskalacji_7d` = Weryfikacje
`KLASA` z jego kolejki eskalowane w ostatnich 7 dniach z `powod_eskalacji ≠
KARENCJA_KONTA_NAUCZYCIELA` / Weryfikacje `KLASA` utworzone w jego kolejce w
ostatnich 7 dniach (przy ≥ 10 utworzonych). Eskalacje z powodu karencji są
wyłączone z licznika (wersja 2.4, C4-06 p. 6), także w odsetku globalnym:
nie mierzą przepustowości Nauczyciela, a po przywróceniu konta ich wliczenie
trzymałoby Klasy pod kontrolą przyjęć do spadku odsetka < 0,10 (do 7 dni) i
fałszywie uruchamiało przeliczenie 17.7. W okresie karencji Klasy
Nauczyciela `USUWANE` są objęte kontrolą przyjęć **wprost** (warunek „konto
Nauczyciela prowadzącego `USUWANE`” w `klasy_z_kontrola_przyjec`), do
Przekazania Klasy albo przywrócenia konta — przywrócenie kończy ją w tej
samej transakcji. Gdy
`odsetek_eskalacji_7d ≥ 0,20`, Uczniowie jego Klas dostają w rekomendacjach
(nie w Przypisaniach) `dowody_dozwolone = false` i `udzial_zadan_otwartych =
5`, Nauczyciel dostaje powiadomienie `ALERT_WERYFIKACJE` z liczbą
zaległych Weryfikacji, a Administratorzy — wpis w raporcie obciążenia;
ograniczenie znika, gdy odsetek spadnie < 0,10. Globalny
`odsetek_eskalacji_7d > 0,10` (A-15; licznik bez `KARENCJA_KONTA_NAUCZYCIELA`)
uruchamia przeliczenie modelu 17.7 i `ALERT_WERYFIKACJE`. Dzięki temu napływ eskalacji jest hamowany u źródła, a
nie tylko obsługiwany w puli globalnej.

#### 11.4.4 Pseudonimizacja w kolejce globalnej (ADR-031)

Nauczyciel, który **nie** jest Nauczycielem docelowym ani Nauczycielem
prowadzącym którąkolwiek Klasę Ucznia, widzi w `WeryfikacjaNaLiscie` i
`WeryfikacjaSzczegoly`:

- `uczen = null`, `uczen_pseudonim = "U-" + upper(hex(hmac_sha256(sekret_pseudonimow, uczen_id)))[0:6]`
  (stały dla Ucznia, nieodwracalny bez sekretu);
- `klasa = null`;
- zdjęcia wyłącznie w wersji maskowanej (`klucz_obiektu_maskowany`); gdy
  maskowana wersja nie istnieje, zdjęcia nie są udostępniane (Weryfikacja na
  podstawie Transkrypcji);
- brak dostępu do Profilu ucznia i innych Rozwiązań Ucznia (12.8).

### 11.5 Wybór Progu pewności (FR-34, NFR-03, NFR-04) — ADR-007

#### 11.5.1 Analiza mocy i ZT-1

Kryterium progu: górna granica Wilsona 95% (UB) dla błędu ocen
automatycznych ≤ 2%. Minimalna efektywna liczność próby powyżej progu
(`n_eff`), przy której kryterium może być spełnione, zależy od
rzeczywistego odsetka błędów `e`:

| `e` (odsetek niezgodnych powyżej progu) | minimalne `n_eff` dla UB ≤ 2% |
|---|---|
| 0% | 189 |
| 0,5% | 335 |
| 0,75% | 482 |
| 1,0% | 753 |

Wnioski i decyzje:

1. Warunek `n_eff ≥ 189` jest wpisany wprost w regułę wyboru progu (dolna
   granica wynikająca z rachunku dla `e = 0`).
2. ZT-1 musi pozwolić na `n_eff ≥ 335` powyżej progu przy założeniu `e =
   0,5%` i udziale ocen automatycznych ≈ 50% zadań obliczeniowych → **≥ 700
   zadań obliczeniowych** (A-09). Próg jest wyznaczany na wartościach
   kalibratora **out-of-fold** z 5-krotnej walidacji krzyżowej, więc
   wszystkie 700 próbek służą jednocześnie do kalibracji i wyboru progu (bez
   utraty 20% na zbiór odłożony).
3. Zadania z dowodem nie mają progu (`wartosc = 1.000`); 350 próbek służy do
   pomiaru NFR-02 (półszerokość przedziału ufności 95% ≈ ±3,1 pp przy 90%
   zgodności) i do kalibratora prezentowanego Nauczycielowi.
4. Ten sam ZT-1 jest oceniany oboma modelami decyzyjnymi (Jev i zapasowy),
   więc G-1 dla modelu zapasowego nie wymaga dodatkowych danych.
**Analiza mocy kryterium G-1(a) (C2-06).** Kryterium zgodności mierzy
dolną granicę Wilsona 95% (LB) Zgodności Oceny na ZT-1, osobno dla każdego
rodzaju. Tabela podaje LB przy obserwowanej zgodności 90% oraz moc
(prawdopodobieństwo spełnienia kryterium) przy rzeczywistej zgodności 92%
(założenie projektowe: model o 2 pp lepszy niż NFR-02):

| Rodzaj | n | Kryterium | LB przy obserwowanych 90% | Minimalna obserwowana zgodność spełniająca kryterium | Moc przy rzeczywistych 92% |
|---|---|---|---|---|---|
| `OTWARTE_OBLICZENIOWE` | 700 | zgodność ≥ 90% **i** LB ≥ 87,5% | 87,5% | 90,0% | 97% |
| `OTWARTE_Z_DOWODEM` (wersja 2.1) | 250 | zgodność ≥ 90% i LB ≥ 87,5% | 85,7% | 91,6% | 65% |
| `OTWARTE_Z_DOWODEM` (wersja 2.2) | 350 | zgodność ≥ 90% **i** LB ≥ 85,0% | 86,4% | 90,0% (samo LB wymagałoby 88,9%) | 93% |
| `OTWARTE_Z_DOWODEM` (opcja D-12) | 700 | zgodność ≥ 90% i LB ≥ 87,5% | 87,5% | 90,0% | 97% |

Decyzja: dla dowodów **n ≥ 350 i LB ≥ 85,0%** (A-09). Uzasadnienie
łagodniejszego LB: Ocena dowodu nigdy nie jest wystawiana automatycznie
(FR-26, próg 1.000), więc błąd modelu nie trafia do Ucznia jako Ocena
ostateczna; kryterium punktowe 90% (NFR-02) pozostaje bez zmian. Kryterium
zostaje przekazane do akceptacji w G-0 (D-12); opcja alternatywna — 700
dowodów z LB ≥ 87,5% — kosztuje dodatkowo ≈ 350 × 2 oceny × 5 min ≈ 58 h
pracy Nauczycieli. Obie analizy (próg i zgodność) są liczone dla obu modeli
decyzyjnych na tym samym ZT-1.

5. **Gdy ZT-1 nie daje progu** (żaden `t` nie spełnia UB ≤ 2% i `n_eff ≥
   189`) dla pary (model, `OTWARTE_OBLICZENIOWE`): para startuje w **trybie
   pełnej weryfikacji** — `prog_pewnosci.wartosc = 1.000`, `zrodlo =
   ADMINISTRATOR`, `uzasadnienie = 'TRYB_PELNEJ_WERYFIKACJI: ZT-1 n_eff=…'`.
   Wszystkie Rozwiązania obliczeniowe idą do Weryfikacji z powodem
   `NISKA_PEWNOSC` (waga IPW 1), więc produkcja sama zbiera próbki; próg
   obniży się automatycznie, gdy reguła 11.5.3 znajdzie `t`. W tym trybie
   `udzial_zadan_otwartych = 5` dla wszystkich Uczniów (ograniczenie
   obciążenia, 17.7). Uruchomienie trybu wymaga akceptacji w G-0.

#### 11.5.2 Populacja i źródło Próbki kalibracyjnej

Próbka kalibracyjna powstaje przy każdym rozstrzygnięciu lub korekcie, w
której Ocena zastępowana jest automatyczna, z deduplikacją
`ux_probka_ocena_auto` (jedna próbka na Ocenę automatyczną). **Źródło
wynika z `weryfikacja.powody_poczatkowe`** — powodów w chwili dołączenia
Oceny weryfikowanej (INSERT albo `PONOW_OCENE`), a nie z bieżących `powody`
(C2-05). Powody dopisane później (Zgłoszenie błędu odczytu) nie zmieniają
źródła, bo dobór Rozwiązania do Weryfikacji odbył się wcześniej i nie
zależał od zgłoszenia; wykluczenie takich próbek wprowadzałoby nielosowe
braki (Uczniowie zgłaszają głównie niskie Oceny), które zaniżałyby
`blad(t)` pod progiem i obniżały próg.

| `powody_poczatkowe` (zbiór) | `zrodlo_probki` | W kalibratorze i progu |
|---|---|---|
| dokładnie {`NISKA_PEWNOSC`} (bez względu na powody dopisane później) | `WERYFIKACJA_NISKA_PEWNOSC` (prawdopodobieństwo doboru 1) | tak |
| dokładnie {`KONTROLA_LOSOWA`} (bez względu na powody dopisane później) | `KONTROLA_LOSOWA` (prawdopodobieństwo doboru = `odsetek_kontroli_efektywny` w chwili Oceny, 11.3 T-5) | tak |
| każdy inny zbiór (np. {`NISKA_PEWNOSC`, `POPRAWNY_WYNIK_BLEDNE_KROKI`}, {`POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`}, {`ZGLOSZENIE_BLEDU_ODCZYTU`}) | `WERYFIKACJA_INNY_POWOD` | nie (te Rozwiązania nie byłyby zaliczone automatycznie albo dobór nie był losowy) |
| korekta bez otwartej Weryfikacji | `KOREKTA_NAUCZYCIELA` | nie (silna selekcja); tylko raporty |

Test jednostkowy `tests/kalibracja/test_zrodlo_probki.py` sprawdza każdy
wiersz tabeli, w tym przypadki „{`NISKA_PEWNOSC`} + dopisane
`ZGLOSZENIE_BLEDU_ODCZYTU`” → `WERYFIKACJA_NISKA_PEWNOSC`, „{`KONTROLA_LOSOWA`}
+ dopisane zgłoszenie” → `KONTROLA_LOSOWA` oraz „`PONOW_OCENE` z trasą
Weryfikacja i powodem {`NISKA_PEWNOSC`}” → `WERYFIKACJA_NISKA_PEWNOSC`.

**Poprawki Transkrypcji a populacja progu (C2-01):** Oceny z powodem T-7
mają źródło `WERYFIKACJA_INNY_POWOD` i nie wchodzą do populacji progu.
Oceny automatyczne z Poprawkami, które przeszły T-7, pozostają w populacji
(są częścią populacji ocen automatycznych, której dotyczy NFR-03) i są
dobierane do Kontroli losowej z podwójnym odsetkiem (T-5), co zapewnia im
osobny nadzór: metryka `blad_automatycznych_z_poprawkami_30d` (IPW, tylko
Kontrola losowa) z **bezpiecznikiem poprawek** (ADR-046, reguła i rachunek
w 11.5.3): wiersz `stan_bezpiecznika` typu `POPRAWKI` staje się aktywny, gdy
dolna granica Wilsona 95% błędu Kontroli losowych Ocen automatycznych z
Poprawkami przekracza 2% przy `n_eff ≥ 50`; aktywny bezpiecznik dodaje
powód T-7 każdej Transkrypcji z Poprawkami, a dezaktywuje go wyłącznie
Administrator po analizie (`POST /admin/bezpieczniki/dezaktywacja`, C4-04) (wersja 2.2: flaga
`LEMAT_POPRAWKI_ZAWSZE_DO_WERYFIKACJI` ustawiana przy `n ≥ 50` i UB > 2%,
czyli zawsze dla n < 189 — C3-01). Odsetek Rozwiązań z Poprawkami mierzy
`odsetek_rozwiazan_z_poprawkami_30d`.

Wszystkie źródła zasilają metrykę zgodności NFR-02 (17.1), a
`uwzgledniana_w_progu` (kolumna generowana) wyznacza populację 11.5.3.

#### 11.5.3 Algorytm

Populacja dla pary (konfiguracja, `OTWARTE_OBLICZENIOWE`), okno 90 dni,
`uwzgledniana_w_progu = true`. Waga IPW z korektą braków odpowiedzi:

```text
π_dobor        = probka.prawdopodobienstwo_doboru
π_wykonanie(k) = (Kontrole losowe zakończone z próbką) / (Kontrole losowe utworzone, bez ANULOWANA z USUNIECIE_KONTA
                 i bez Kontroli Uczniów z kontem USUWANE w chwili obliczenia — okres karencji, ADR-040)
                 w koszyku k pewności surowej {[0.80,0.90), [0.90,0.95), [0.95,1.00]} w oknie 90 dni
w              = 1 / π_dobor                                   dla WERYFIKACJA_NISKA_PEWNOSC
w              = 1 / (π_dobor × π_wykonanie(k))                dla KONTROLA_LOSOWA

kalibrator_nowy = izotoniczna regresja ważona w (pewnosc_surowa → zgodna), 5-krotna CV;
                  dla każdej próbki p_oof = kalibrator dopasowany bez jej foldu
dla t w {0.800, 0.805, …, 0.990}:
    S_t      = próbki z p_oof > t
    blad(t)  = Σ w·[niezgodna] / Σ w            (po S_t)
    n_eff(t) = (Σ w)² / Σ w²
    UB(t)    = górna granica Wilsona 95% dla blad(t) i n_eff(t)
prog_nowy = min { t : UB(t) ≤ 0.02 i n_eff(t) ≥ 189 }
jeśli brak takiego t: prog_nowy = prog_obecny (bez obniżania)
```

ECE (10 koszyków równej szerokości) jest liczone na `p_oof`. Nowy kalibrator
i próg wchodzą w życie tylko, gdy liczba próbek ≥ 400; zmiana progu w dół
jest ograniczona do 0.02 na dobę i podlega blokadom z kroku 4 algorytmu
„Wynik dnia KALIBRUJ” niżej (C4-02); `prog_nowy` z powyższego pseudokodu
jest w nim oznaczony `p_wyb`.

**Reguły nadzoru (braki danych, C1-10):**

| Warunek (okno 30 dni) | Skutek |
|---|---|
| `wykonanie_kontroli_30d` = Kontrole zakończone do `termin_at` (3 dni robocze) / utworzone (bez kont `USUWANE`) ≥ 0.90 | normalna praca |
| 0.80 ≤ `wykonanie_kontroli_30d` < 0.90 | alert `ALERT_KALIBRACJI` (ostrzeżenie) |
| 0.60 ≤ `wykonanie_kontroli_30d` < 0.80 | próg **nie może spadać** (KALIBRUJ nie obniża), alert |
| `wykonanie_kontroli_30d` < 0.60 lub liczba Kontroli zakończonych < 50 przy ≥ 100 utworzonych | **bezpiecznik braku nadzoru:** `prog := min(0.990, prog + 0.02)` na dobę, `zrodlo = BEZPIECZNIK`, alert |

**Bezpieczniki trafności i poprawek (ADR-046, wersja 2.3, C3-01).** W
wersji 2.2 bezpiecznik reagował na **brak dowodu spełnienia** („`n ≥ 50` i
UB > 2%”). Górna granica Wilsona 95% przy zerze błędów wynosi z²/(n + z²):
7,1% dla n = 50, 3,7% dla n = 100, 2,5% dla n = 150 i spada poniżej 2%
dopiero od n = 189 — warunek był więc spełniony z konstrukcji w całym
przedziale 50 ≤ n < 189, także przy bezbłędnym modelu. Od wersji 2.3
bezpiecznik reaguje na **dowód przekroczenia**:

```text
Populacja P(typ, para) — liczona codziennie w KALIBRUJ dla pary (konfiguracja, OTWARTE_OBLICZENIOWE),
NIE dla rekordu prog_pewnosci (zmiana progu nie zeruje próby):
  TRAFNOSC: Próbki KONTROLA_LOSOWA z Kontroli utworzonych w [max(now() − 30 dni, okno_od), now()],
            których kalibrator AKTYWNY w chwili obliczenia daje p(pewnosc_surowa) > prog_aktywny
            (próba warunkowa „powyżej bieżącego progu”: podniesienie progu zmniejsza próbę, ale jej nie zeruje,
            więc nie działa jak zapadka)
  POPRAWKI: jak TRAFNOSC, ale tylko Oceny z zatwierdzoną Transkrypcją POPRAWKA_UCZNIA
wagi:  w = 1 / (π_dobor × π_wykonanie(k))       (11.5.3, ten sam wzór co przy wyborze progu)
blad   = Σ w·[niezgodna] / Σ w;   n_eff = (Σ w)² / Σ w²
LB, UB = granice przedziału Wilsona 95% (z = 1,96) dla (blad, n_eff)

Klasyfikacja oceny (codziennie, krok 2 algorytmu „Wynik dnia KALIBRUJ”; P liczona dla progu p0 i
kalibratora aktywnego PRZED przebiegiem — wybór progu tego samego dnia nie zmienia próby):
  1. n_eff < 50                    → brak działania (za mało danych; metryki i panel /admin/kalibracja)
  2. n_eff ≥ 50 i LB > 0,02        → WYZWOLENIE:
        TRAFNOSC: podniesienie progu w kroku 5 (zrodlo = BEZPIECZNIK) i Blokada obniżania progu
                  (stan_bezpiecznika.aktywny := true, ostatnie_wyzwolenie_at := now()), ALERT_KALIBRACJI
        POPRAWKI: stan_bezpiecznika.aktywny := true (każda Transkrypcja z Poprawkami → T-7), ALERT_KALIBRACJI
  3. n_eff ≥ 189 i UB > 0,02 i LB ≤ 0,02 → OSTRZEŻENIE: KALIBRUJ nie obniża progu tej pary (jak przy
        0,60 ≤ wykonanie_kontroli_30d < 0,80), alert informacyjny; progu nie podnosi
  4. pozostałe                     → brak działania
```

**Wynik dnia KALIBRUJ dla pary (wersja 2.4, C4-02) — jedna, jawna kolejność.**
W wersji 2.3 bezpiecznik działał „po wyborze progu” bez określenia, czy
podnosi próg wybrany tego dnia, czy poprzedni, a wybór progu mógł codziennie
cofać podniesienie (oscylacja 0,930 ↔ 0,950 przy trwałym LB > 2%). Od wersji
2.4 każdy przebieg wykonuje kroki 1–6 w tej kolejności i zapisuje **jeden**
rekord `prog_pewnosci`:

```text
Dane: p0 = wartość aktywnego progu przed przebiegiem; K0 = aktywny kalibrator; K1 = kalibrator_nowy
      (gdy ≥ 400 próbek, inaczej K1 = K0); BT = stan_bezpiecznika (para, TRAFNOSC)
Krok 1 (wybór):      p_wyb = min{ t : UB90(t) ≤ 0,02 ∧ n_eff90(t) ≥ 189 } na p_oof(K1), okno 90 dni;
                     brak takiego t → p_wyb = p0
Krok 2 (bezpieczniki, stan sprzed przebiegu): P(TRAFNOSC), P(POPRAWKI) powyżej p0 przy K0, okno
                     [max(now − 30 dni, okno_od), now] → wyzwolenie_T, ostrzezenie_T, wyzwolenie_P (reguły 1–4)
Krok 3 (Blokada obniżania progu):
                     wyzwolenie_T                                  → BT.aktywny := true
                     BT.aktywny ∧ ¬wyzwolenie_T ∧ n_eff ≥ 189 ∧ UB ≤ 0,02 (próba kroku 2)
                                                                   → BT.aktywny := false, wyjscie_z_blokady_at := now()
                     BT.aktywny ∧ p0 = 1,000                       → bez zmian (wyjście wyłącznie przez dezaktywację)
Krok 4 (dozwolone obniżenie):
                     zablokowane = BT.aktywny ∨ ostrzezenie_T ∨ wykonanie_kontroli_30d < 0,80
                                   ∨ bezpiecznik braku nadzoru (tabela reguł nadzoru)
                     p_dol = max(p_wyb, p0 − 0,02)
                     p_dol < p0 ∧ ¬zablokowane → test obniżenia na oknie 30 dni: populacja wyboru progu
                         (Próbki uwzgledniana_w_progu, wagi w z 11.5.3) z p(K1) > p_dol:
                         wymagane n_eff ≥ 50 ∧ LB ≤ 0,02 ∧ (n_eff < 189 ∨ UB ≤ 0,02); niespełnione → p_dol := p0
                     zablokowane → p_dol := max(p_dol, p0)
Krok 5 (łączny wynik):
                     wyzwolenie_T:               p_nowy = 1,000                          gdy p0 ≥ 0,990
                                                 p_nowy = min(0,990, max(p_dol, p0 + 0,02)) w przeciwnym razie
                     bezpiecznik braku nadzoru:  p_nowy = min(0,990, max(p_dol, p0 + 0,02))
                     pozostałe:                  p_nowy = p_dol   (p_wyb > p0 podnosi próg bez limitu kroku)
Krok 6 (zapis, 1 transakcja): gdy p_nowy ≠ p0 lub K1 ≠ K0 — INSERT kalibrator_pewnosci (gdy K1 ≠ K0),
                     INSERT prog_pewnosci (wartosc = p_nowy, zrodlo = BEZPIECZNIK gdy p_nowy > p_dol,
                     inaczej KALIBRACJA_AUTOMATYCZNA), poprzedni aktywny := false;
                     UPSERT stan_bezpiecznika (TRAFNOSC, POPRAWKI)
```

Skutki: (1) dopóki trwa Blokada obniżania progu, próg pary jest
**niemalejący** — wybór progu na oknie 90 dni (rozcieńczonym danymi sprzed
degradacji) nie może go cofnąć, bo krok 4 ustawia `p_dol ≥ p0`; (2) kolejne
wyzwolenia podnoszą próg o 0,02 dziennie od `p0`, a nie od progu wybranego
tego dnia; (3) wyzwolenie przy `p0 = 0,990` przełącza parę w **tryb pełnej
weryfikacji** (`wartosc = 1.000`, CHECK 7.3 dopuszcza 1.000; alert krytyczny
`ALERT_KALIBRACJI`; `udzial_zadan_otwartych = 5` jak w 10.1.2) — decyzja D-16;
(4) **reguła wyjścia**: blokada kończy się automatycznie dopiero przy dowodzie
spełnienia NFR-03 powyżej bieżącego progu (UB ≤ 2% przy `n_eff ≥ 189` na
oknie 30 dni); przy LB ≤ 2% < UB albo `n_eff < 189` (mniejsza próba powyżej
wyższego progu) blokada trwa, próg się nie zmienia, a nowy wzrost LB > 2%
podnosi go dalej; w trybie pełnej weryfikacji (`p0 = 1,000`) nie ma Ocen
automatycznych, więc nie ma Kontroli losowych powyżej progu i wyjście
następuje wyłącznie przez dezaktywację bezpiecznika trafności przez
Administratora (`POST /admin/bezpieczniki/dezaktywacja`, typ `TRAFNOSC`),
po której próg obniża się o ≤ 0,02 na dobę przez test kroku 4 (populacja
wyboru progu zawiera wtedy Próbki `WERYFIKACJA_NISKA_PEWNOSC` wszystkich
Ocen, `π_dobor = 1`); (5) po wyjściu z blokady obniżanie wymaga testu 30
dni powyżej progu docelowego, więc błędy z okresu degradacji (do 30 dni)
blokują powrót do niższego progu.

Rachunek dla reguły 2 (rozkład dwumianowy przy wagach równych, `n_eff = n`;
przy podwójnym odsetku Kontroli dla Poprawek `n_eff` jest o 3–6% mniejsze niż
`n`). `k_min` = najmniejsza liczba błędów, przy której LB > 2%;
„P(wyzwolenie)” = prawdopodobieństwo wyzwolenia w jednej ocenie przy
rzeczywistym błędzie `e`:

| `n_eff` | UB przy 0 błędów (reguła 2.2 wyzwalała?) | `k_min` (obserwowany błąd) | LB przy `k_min − 1` / `k_min` | P(wyzwolenie), e = 1% | e = 2% | e = 3% | e = 5% | e = 8% |
|---|---|---|---|---|---|---|---|---|
| 50 | 7,13% (tak) | 3 (6,0%) | 1,10% / 2,06% | 1,4% | 7,8% | 18,9% | 45,9% | 77,4% |
| 100 | 3,70% (tak) | 5 (5,0%) | 1,57% / 2,15% | 0,34% | 5,1% | 18,2% | 56,4% | 91,0% |
| 150 | 2,50% (tak) | 7 (4,7%) | 1,85% / 2,28% | 0,08% | 3,2% | 16,6% | 62,7% | 96,0% |
| 188 | 2,00% (tak) | 8 (4,3%) | 1,82% / 2,17% | 0,07% | 3,7% | 20,5% | 72,7% | 98,6% |
| 189 | 1,99% (nie) | 8 (4,2%) | 1,81% / 2,16% | 0,07% | 3,7% | 20,9% | 73,3% | 98,6% |
| 335 | 1,13% (nie) | 12 (3,6%) | 1,84% / 2,06% | 0,02% | 3,9% | 30,7% | 91,2% | ≈ 100% |
| 500 | 0,76% (nie) | 17 (3,4%) | 1,98% / 2,13% | < 0,01% | 2,6% | 33,4% | 96,6% | ≈ 100% |
| 1 000 | 0,38% (nie) | 29 (2,9%) | 1,94% / 2,03% | < 0,01% | 3,3% | 59,9% | ≈ 100% | ≈ 100% |

Przy zerze błędów LB = 0, więc reguła 2 **nigdy** nie wyzwala się przy
bezbłędnej próbie, niezależnie od `n` (w szczególności dla 50 ≤ n < 189).
**Rozmiar testu (wersja 2.4, C4-06 p. 5):** reguła 2 jest jednostronnym
testem H0: e ≤ 2% o nominalnym poziomie 2,5%, ale granica Wilsona jest przy
małym `n` antykonserwatywna, więc rzeczywisty rozmiar pojedynczej oceny przy
e = 2% wynosi **2,6–7,8%** (kolumna „e = 2%” tabeli), a nie ≤ 2,5%; przy e =
1% — ≤ 1,4%. Przy codziennej ocenie przez 90 dni P(≥ 1 wyzwolenie) przy e =
2% wynosi 31–36% (tabela symulacji niżej). Jest to świadomie przyjęty koszt
(D-16): na granicy NFR-03 bezpiecznik nie odróżnia e = 2% od e nieco
wyższego. Oczekiwana liczba **fałszywych podniesień progu na kwartał** przy
e = 2% (symulacja dynamiki niżej): **1,1**; przy e ≤ 1%: ≤ 0,01.

Ocena codzienna na kroczącym oknie 30 dni (symulacja 2 000 przebiegów po 90
dni, napływ zakończonych Kontroli: start 100/dobę, faza ustalona 35/dobę,
model zapasowy podczas awarii Jev 10/dobę; `tests/kalibracja/symulacja_bezpiecznika.py`):

| Rzeczywisty błąd `e` | Start (100/dobę): P(≥ 1 wyzwolenie w 90 dni) / mediana dnia wyzwolenia | Faza ustalona (35/dobę) | Model zapasowy (10/dobę) |
|---|---|---|---|
| 0% | 0% / — | 0% / — | 0% / — |
| 0,5% (założenie ZT-1, 11.5.1) | 0% / — | 0,2% / dzień 2 | 0,1% / dzień 5 |
| 1,0% | 0,7% / dzień 1 | 1,0% / dzień 2 | 2,2% / dzień 5 |
| 2,0% (granica NFR-03) | 31% / dzień 19 | 34% / dzień 32 | 36% / dzień 32 |
| 4,0% (degradacja) | 100% / dzień 2 | 100% / dzień 4 | 99,4% / dzień 12 |
| 6,0% | 100% / dzień 1 | 100% / dzień 2 | 100% / dzień 5 |

Wnioski: (1) bezpiecznik nie wyłącza ocen automatycznych na starcie
produkcji ani po awarii Jev przy modelu spełniającym założenie ZT-1 —
model obciążenia 17.7 („Start: auto 47%”) pozostaje ważny (scenariusz
startowy w 17.7); (2) model, którego błąd **od początku okna** wynosi ≥ 4%,
jest wykrywany w 2 dni na starcie i w 4 dni w fazie ustalonej; **nagła**
degradacja z 0,5% do 4% w fazie ustalonej jest wykrywana później, bo okno 30
dni rozcieńcza ją danymi sprzed degradacji — mediana 13 dni od początku
degradacji do pierwszego wyzwolenia (symulacja dynamiki niżej, C4-02);
(3) błąd w przedziale (2%, 3%) nie jest
wykrywany szybko przez bezpiecznik — chroni przed nim reguła wyboru progu
(UB ≤ 2% przy `n_eff ≥ 189` na oknie 90 dni, KALIBRUJ nie obniża progu,
reguła 3 blokuje obniżanie) oraz alert `gorna_granica_bledu_30d > 0,02`.

**Symulacja dynamiki progu (wersja 2.4, C4-02;
`tests/kalibracja/symulacja_bezpiecznika.py --dynamika`).** Założenia: faza
ustalona, 1 200 Ocen obliczeniowych na dobę, pewność skalibrowana z rozkładem
`1 − p ~ Exp(0,0631)` (auto 67% przy progu 0,930; 54,7% przy 0,950; 37,8% przy
0,970; 14,7% przy 0,990), Kontrola 5%, wykonanie 0,9 (≈ 36 Kontroli/dobę przy
0,930), Próbki `WERYFIKACJA_NISKA_PEWNOSC` dla wszystkich Ocen poniżej progu,
okno 30 dni wypełnione przed dniem 1, wybór progu pesymistyczny: `p_wyb =
0,930` każdego dnia (krok 1 zawsze chce wrócić do progu sprzed degradacji),
600 przebiegów po 90 dni; reguły kroków 1–6.

| Scenariusz | P(wyzwolenie) | Mediana dni od degradacji do 1. wyzwolenia | Próg — mediana (p10–p90): dzień 70 / 75 / 80 / 90 | Udział auto — mediana: dzień 75 / 80 / 90 | P(tryb pełnej weryfikacji w dniu 90) | Spadek progu podczas blokady |
|---|---|---|---|---|---|---|
| e = 0,5% przez 90 dni | 0,0% | — | 0,930 / 0,930 / 0,930 / 0,930 | 67% / 67% / 67% | 0% | 0 |
| degradacja jednorodna: e = 0,5% w dniach 1–60, 4% w dniach 61–90 (test (g)) | 98,7% | 13 | 0,930 (0,930–0,970) / 0,970 (0,930–1,000) / 0,990 (0,950–1,000) / 1,000 (0,990–1,000) | 38% / 15% / 0% | 61% | 0 |
| degradacja skupiona: 8,5% dla p ∈ (0,930; 0,970], 0,5% powyżej, od dnia 61 (łącznie ≈ 4% przy 0,930) | 98,3% | 13 | 0,930 (0,930–0,950) / 0,950 (0,930–0,970) / 0,950 (0,950–0,970) / 0,970 (0,950–0,970) | 55% / 55% / 38% | 0% | 0 |
| e = 2% przez 90 dni (granica NFR-03) | 44,7% | — | — | średnio 57,5% (zamiast 67%) | 11% | — (wyjście z blokady, potem obniżanie) |

Wnioski: (1) we wszystkich przebiegach z degradacją próg jest
niemalejący od pierwszego wyzwolenia (100%), więc reakcja jest skuteczna, a
nie tylko alarmowa; (2) degradacja jednorodna (niezależna od pewności) nie
maleje po podniesieniu progu, więc w ≤ 4 kolejnych wyzwoleniach para
przechodzi w tryb pełnej weryfikacji (61% przebiegów do dnia 90); degradacja
skupiona w niskiej pewności zatrzymuje się na progu 0,950–0,970, przy którym
LB ≤ 2%; (3) przy e = 2% P(≥ 1 wyzwolenie w 90 dniach) = 44,7% przy oknie
pełnym od dnia 1 (31–36% w tabeli wyżej, gdzie okno napełnia się od zera) —
oba wyniki opisują ten sam koszt: średnio 1,1 fałszywego podniesienia na
kwartał i 11% kwartałów z trybem pełnej weryfikacji na granicy NFR-03
(D-16, R-22); (4) **odrzucona alternatywa** — dodatkowe okno 7 dni (wyzwolenie
przy LB > 2% w oknie 30 **lub** 7 dni) skraca medianę wykrycia nagłej
degradacji z 13 do 7 dni, ale przy e = 2% podnosi P(≥ 1 wyzwolenie) do 72,5%,
średnią liczbę podniesień do 2,0 na kwartał i P(trybu pełnej weryfikacji) do
28,5%, a przy e = 1% daje 2,3% kwartałów z wyzwoleniem (okno 30 dni: 0%).

**Dezaktywacja bezpiecznika (wersja 2.4, C4-04 — dawniej „zdjęcie”):**
wyłącznie Administrator (`POST /admin/bezpieczniki/dezaktywacja`,
uzasadnienie wymagane), dla obu typów: ustawia `aktywny = false`, `okno_od =
now()`, `dezaktywowano_at`, `dezaktywowany_przez`,
`uzasadnienie_dezaktywacji`, więc ponowne wyzwolenie wymaga nowego dowodu z
Kontroli utworzonych po dezaktywacji (stary materiał nie wyzwala go
ponownie). Dla typu `POPRAWKI` kończy T-7 dla każdej Poprawki; dla typu
`TRAFNOSC` kończy Blokadę obniżania progu (jedyne wyjście z trybu pełnej
weryfikacji wywołanego bezpiecznikiem) — próg obniża potem krok 4 (o ≤ 0,02
na dobę, z testem 30 dni). Bezpiecznik trafności nie zmienia trasy Oceny
bezpośrednio: działa przez rekord `prog_pewnosci` (`zrodlo = BEZPIECZNIK`).

**Testy jednostkowe** `tests/kalibracja/test_bezpiecznik.py`: (a) 0 błędów
dla każdego n = 50 … 188 oraz n ∈ {189, 335, 500, 1 000, 2 000} → brak
wyzwolenia (oba typy); (b) dla n ∈ {50, 100, 189, 500, 1 000}: `k_min − 1`
błędów → brak, `k_min` → wyzwolenie (wartości z tabeli wyżej); (c)
podniesienie progu z 0,930 do 0,950 przy 300 Próbkach powyżej 0,930 i 120
powyżej 0,950 → ocena następnego dnia używa 120 Próbek, a nie 0 (brak
zapadki); (d) `n_eff < 50` przy 5/40 błędów → brak wyzwolenia; (e)
bezpiecznik poprawek po dezaktywacji: Kontrole sprzed `okno_od` nie są liczone;
(f) przypadek ostrzeżenia (n = 300, k = 3: UB 2,90%, LB 0,34%) → blokada
obniżania bez podnoszenia progu; od wersji 2.4 (C4-02): (g) degradacja 4%
przez 30 dni (dni 61–90) na tle 60 dni z błędem 0,5% (dane deterministyczne:
co 25. Kontrola błędna w dniach 61–90, co 200. w dniach 1–60), `p_wyb =
0,930` każdego dnia → próg niemalejący w każdym dniu od pierwszego
wyzwolenia, przyrosty wyłącznie o 0,02 (albo 0,990 → 1,000) i tylko w dniach
z LB > 2%; (h) dzień N: wyzwolenie przy p0 = 0,930 → p_nowy = 0,950; dzień N
+ 1: `p_wyb = 0,930`, LB ≤ 2% < UB → p_nowy = 0,950 (bez oscylacji, krok 4);
(i) wyjście z blokady: próba powyżej 0,950 z n = 400, k = 2 (UB 1,80%) →
`aktywny = false`, następnego dnia obniżenie dopuszczone tylko przy
spełnionym teście 30 dni powyżej progu docelowego; (j) wyzwolenie przy p0 =
0,990 → `wartosc = 1.000`, a przy `p0 = 1.000` brak automatycznego wyjścia;
(k) dezaktywacja typu `TRAFNOSC` ustawia `okno_od` i umożliwia obniżenie o
0,02 na dobę.

**Odsetek Kontroli losowej:** 0.20, dopóki para (konfiguracja, rodzaj) ma
mniej niż 400 próbek `KONTROLA_LOSOWA` w oknie 90 dni; potem 0.05.

**Termin Kontroli losowej:** `termin_at = dodaj_dni_robocze(created_at, 3)`
(jak każda Weryfikacja); zdjęcia Rozwiązania z otwartą Kontrolą są przechowywane do
jej zamknięcia (maks. 90 dni, 16.4), więc Kontrola zawsze odbywa się z
dostępem do zdjęcia w obrębie twardego limitu.

Dla `OTWARTE_Z_DOWODEM` istnieje rekord `prog_pewnosci` z `wartosc = 1.000`
(automatyczne zaliczenie niemożliwe, CHECK w DDL); jego kalibrator służy
wyłącznie do pokazania Nauczycielowi Pewności oceny.

### 11.6 Zgłoszenie błędu odczytu przez Ucznia (FR-28, ADR-022)

`POST /rozwiazania/{id}/zgloszenie-bledu-odczytu` jest przyjmowane, gdy:

| Warunek | Błąd przy niespełnieniu |
|---|---|
| Rozwiązanie w `OCENIONE` lub `OCZEKUJE_NA_WERYFIKACJE` i ma aktualną Ocenę | `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN` |
| `now() ≤ ocena_aktualna.created_at + 14 dni` (okno liczone od Oceny aktualnej, także Oceny Nauczyciela wystawionej po Kontroli losowej lub korekcie, C2-16) | `409 ZGLOSZENIE_NIEDOSTEPNE` |
| zdjęcia Rozwiązania nie zostały usunięte | `409 ZGLOSZENIE_NIEDOSTEPNE` |
| brak wcześniejszego zgłoszenia dla tego Rozwiązania (1 na Rozwiązanie) | `409 ZGLOSZENIE_NIEDOSTEPNE` |
| ≤ 5 zgłoszeń Ucznia w ostatnich 30 dniach | `429 LIMIT_PRZEKROCZONY` |
| `kroki` ⊆ numery kroków zatwierdzonej Transkrypcji, 1–5 pozycji | `422 WALIDACJA` |

Skutek: dopisanie powodu `ZGLOSZENIE_BLEDU_ODCZYTU` do otwartej Weryfikacji
albo nowa Weryfikacja wskazująca aktualną Ocenę (9.12), priorytet 2, SLA
3 dni robocze. Ocena pozostaje widoczna do rozstrzygnięcia, ale jej Wynik zadania
jest **zawieszony** (`powod_uniewaznienia = 'ZGLOSZENIE_BLEDU_ODCZYTU'`) i
nie wpływa na Profil ucznia; po rozstrzygnięciu Ocena Nauczyciela zastępuje
Ocenę zgłoszoną, powstaje nowy Wynik zadania, a Profil ucznia jest
odtwarzany. Nauczyciel oznacza `kroki_z_bledem_odczytu` i
`blad_odczytu_wplynal_na_ocene`, co zasila metryki 11.8.

### 11.7 Uzasadnienie Oceny (słownik: „Ocena … wraz z uzasadnieniem”)

`ocena.uzasadnienie` jest generowane deterministycznie z decyzji (bez LLM)
i pokazywane wszystkim rolom w `OcenaWyjscie.uzasadnienie`. Szablon:

```text
Kryteria: K1 „{opis}” — {pk}/{max}; K2 „{opis}” — {pk}/{max}.
{Pierwszy błąd: krok {j} ({typ_bledu_pl}). | Brak wymaganego etapu: „{opis kryterium bez punktów}”. | Nie wskazano błędów.}
{Punkty za wynik końcowy nieprzyznane, bo wynik nie wynika z poprawnego rozumowania (FR-20).}
{Możliwy błąd odczytu pisma w kroku {k} — punkty zostaną potwierdzone przez nauczyciela.}
```

Dla roli Nauczyciel i Administrator do uzasadnienia dopisywane są
prawdopodobieństwa decyzji (np. „K1 — 1/1 (p = 0,96)”), niespójności
(N-x) i pewność oceny; Uczeń widzi wersję bez prawdopodobieństw. Dla Oceny
Nauczyciela uzasadnienie jest generowane z jego decyzji tym samym szablonem,
z dopiskiem komentarza Nauczyciela (≤ 500 znaków, opcjonalny).

### 11.8 Skuteczność wykrywania błędów odczytu (FR-28)

| Metryka | Definicja | Cel / alert | Bramka |
|---|---|---|---|
| `bledy_odczytu_niewykryte_30d` | odsetek Próbek kalibracyjnych z `blad_odczytu_wplynal = true`, w których Ocena automatyczna miała `mozliwy_blad_odczytu = false` (populacja: wszystkie Weryfikacje i Kontrole z Oceną automatyczną) | ≤ 1%; alert > 2% | G-2 |
| `czulosc_t3` | na ZT-1 end-to-end: odsetek Rozwiązań, w których Transkrypcja bez poprawek zawiera błąd odczytu zmieniający punkty, oznaczonych `mozliwy_blad_odczytu` | ≥ 90% | G-2 |
| `zgloszenia_bledu_odczytu_uznane_30d` | odsetek zgłoszeń Ucznia, po których Ocena Nauczyciela ma więcej punktów niż Ocena zgłaszana | raport; > 50% przy ≥ 30 zgłoszeniach → przegląd promptu Agenta rozpoznającego | — |

---

## 12. Kontrakty API (REST)

### 12.1 Konwencje

| Element | Ustalenie |
|---|---|
| Bazowa ścieżka | `/api/v1`; zmiana niekompatybilna → `/api/v2`, `v1` utrzymywane 6 miesięcy |
| Format | JSON UTF-8; czasy ISO-8601 w UTC (`2026-10-31T18:00:00Z`); identyfikatory UUID v4 |
| Specyfikacja | OpenAPI 3.1 generowane z FastAPI, publikowane pod `/api/v1/openapi.json`; test kontraktowy w CI porównuje z wersją w repozytorium |
| Uwierzytelnianie | `Authorization: Bearer <JWT>` (EdDSA, 15 min, claims: `sub`, `rola`, `exp`, `jti`); refresh token w cookie `lemat_rt` (`HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth`) |
| Optymistyczna blokada | Odpowiedź z zasobem wersjonowanym zawiera `ETag: "<version>"` (dla Weryfikacji — wersja treści, ADR-037); operacje modyfikujące wymagają `If-Match` (brak → `428 WYMAGANY_IF_MATCH`, niezgodność → `412 KONFLIKT_WERSJI`). **Zasób `If-Match`** jest podany w kolumnie „ETag” katalogu 12.3: `R` — Rozwiązanie, `W` — Weryfikacja, `Z` — Zadanie, `U` — Uczeń/Użytkownik, `S` — Zestaw zadań, `K` — Klasa, `WT` — Wkładka teoretyczna |
| Idempotencja | `Idempotency-Key: <uuid>` wymagany dla `POST /rozwiazania`, `POST /zadania/{id}/odpowiedzi`, `POST /zadania/generowanie`, `POST /przypisania`, `POST /rozwiazania/{id}/zgloszenie-bledu-odczytu`, `POST /zadania/{id}/zgloszenia-bledu`, `DELETE /uczniowie/ja`, `DELETE /nauczyciele/ja`; zapisane są `kod_statusu_http` i `cialo_http`; powtórka z tym samym kluczem i ciałem zwraca zapisaną odpowiedź; inne ciało → `422 KLUCZ_IDEMPOTENCJI_KONFLIKT`; ważność 24 h; właściciel: `lemat.http` |
| Stronicowanie | `?limit=1..100` (domyślnie 50) i `?kursor=<nieprzezroczysty>`; odpowiedź `{ "elementy": [...], "nastepny_kursor": string \| null }` |
| Język | Komunikaty `title`/`detail` po polsku (NFR-10) |
| Odpytywanie | `GET /rozwiazania/{id}` zwraca `nastepne_odpytanie_ms`: 1000 w stanach `PRZESLANE`, `ROZPOZNAWANIE`, `ZATWIERDZONE`, `OCENIANIE` oraz gdy `ocena.wskazowka_w_przygotowaniu`; `null` w pozostałych (ADR-014) |

### 12.2 Model błędu (RFC 9457)

```json
{
  "type": "https://lemat.pl/bledy/rozwiazanie-nieprawidlowy-stan",
  "title": "Nieprawidłowy stan rozwiązania",
  "status": 409,
  "detail": "Transkrypcji nie można zmienić po zatwierdzeniu.",
  "kod": "ROZWIAZANIE_NIEPRAWIDLOWY_STAN",
  "instance": "/api/v1/rozwiazania/7c1e…/transkrypcja",
  "bledy_pol": [ { "pole": "kroki[3].latex", "komunikat": "Maksymalnie 2000 znaków." } ]
}
```

| Kod | HTTP | Znaczenie |
|---|---|---|
| `WALIDACJA` | 422 | Błędne dane wejściowe (`bledy_pol`) |
| `NIEUWIERZYTELNIONY` | 401 | Brak/nieważny token |
| `BRAK_UPRAWNIEN` | 403 | Rola lub relacja (np. Uczeń spoza Klasy, Nauczyciel bez uprawnienia Przeglądu) nie pozwala |
| `KONTO_NIEAKTYWNE` | 403 | Konto `OCZEKUJE_NA_ZATWIERDZENIE`/`ZABLOKOWANE`/`USUWANE` |
| `NIE_ZNALEZIONO` | 404 | Zasób nie istnieje lub nie jest widoczny dla użytkownika |
| `ROZWIAZANIE_NIEPRAWIDLOWY_STAN` | 409 | Operacja niedozwolona w stanie Rozwiązania (8.1) |
| `ELEMENT_SCIEZKI_NIEAKTYWNY` | 409 | Element nie jest `PROPONOWANY`/`ROZPOCZETY` lub dotyczy innego Zadania |
| `WERSJA_ZADANIA_NIEAKTUALNA` | 409 | `zadanie_wersja_id` nie jest wersją akceptowaną (12.5) — Uczeń musi odświeżyć treść |
| `ZADANIE_NIEDOSTEPNE` | 409 | Zadanie `WYCOFANE`/`ODRZUCONE` i element nie jest `ROZPOCZETY` |
| `WERYFIKACJA_ZAJETA` | 409 | Weryfikację z ważną blokadą trzyma inny Nauczyciel |
| `WERYFIKACJA_NIE_PODJETA` | 409 | Rozstrzygnięcie, przedłużenie lub szkic bez aktualnej blokady |
| `BLOKADA_MAKS_CZAS` | 409 | Przedłużenie przekroczyłoby 2 h od podjęcia |
| `OCENA_ZMIENIONA` | 409 | Ocena weryfikowana została zastąpiona w międzyczasie (korekta) |
| `PRZYPISANIE_PO_TERMINIE` | 409 | Odpowiedź w kontekście Przypisania (także z elementu `R-PRZYPISANIE`) po terminie albo Rozwiązanie po terminie + 7 dni (12.5) |
| `TEST_NIEDOSTEPNY` | 409 | Pula Zadań diagnostycznych pokrywa < 50% planowanych pozycji Testu diagnostycznego (9.1) |
| `PULA_DIAGNOSTYCZNA_ZA_MALA` | 409 | Wyłączenie flagi `diagnostyczne` obniżyłoby pulę (Dział, poziom matury, Poziom trudności) poniżej 3 (bez `?wymus=true`) |
| `ZGLOSZENIE_ZADANIA_ISTNIEJE` | 409 | Nauczyciel ma już otwarte Zgłoszenie błędu w tym Zadaniu |
| `OSTATNI_ADMINISTRATOR` | 409 | Usunięcie lub blokada ostatniego aktywnego konta Administratora |
| `LIMIT_PRZESLAN` | 409 | Wyczerpane 3 przesłania zdjęć |
| `LIMIT_PONOWIEN_ROZPOZNANIA` | 409 | Wyczerpane 5 ponowień rozpoznania |
| `ZGLOSZENIE_NIEDOSTEPNE` | 409 | Zgłoszenie błędu odczytu poza warunkami 11.6 |
| `TEST_ZAKONCZONY` | 409 | Odpowiedź do zakończonego Testu diagnostycznego |
| `TEST_JUZ_WYKONANY` | 409 | Test diagnostyczny dla tego poziomu matury już wykonano |
| `ZESTAW_UZYWANY` | 409 | Usunięcie Zestawu zadań użytego w Przypisaniu |
| `KONTO_NIE_DO_PRZYWROCENIA` | 409 | Konto nie jest `USUWANE` albo minęło 30 dni |
| `KONTO_UCZNIA_NIEAKTYWNE` | 409 | Rozstrzygnięcie lub korekta dla Ucznia w okresie karencji usunięcia konta (ADR-040, C3-08); szkic rozstrzygnięcia pozostaje zapisany |
| `BEZPIECZNIK_NIEAKTYWNY` | 409 | Dezaktywacja bezpiecznika, który nie jest aktywny (ADR-046, C4-04) |
| `BRAK_PROGU_DLA_KONFIGURACJI` | 409 | Aktywacja konfiguracji decyzyjnej bez Progu pewności z ZT-1 |
| `BRAK_RETENCJI_DOSTAWCY` | 409 | Aktywacja konfiguracji bez potwierdzonej retencji dostawcy (A-07) |
| `KONFLIKT_WERSJI` | 412 | `If-Match` niezgodny |
| `ZDJECIE_ZA_DUZE` | 413 | Plik > 10 MB |
| `NIEOBSLUGIWANY_FORMAT` | 415 | Inny niż JPEG/PNG/HEIC/WEBP |
| `KLUCZ_IDEMPOTENCJI_KONFLIKT` | 422 | Ten sam klucz, inne ciało |
| `ZA_DUZO_ZDJEC` | 422 | Więcej niż 5 zdjęć w jednym przesłaniu lub 0 zdjęć |
| `ZDJECIE_NIEPRAWIDLOWE` | 422 | Plik nie dekoduje się albo przekracza 12 000 px w jednym wymiarze lub 40 mln pikseli (ADR-045) |
| `POPRAWKI_ZBYT_ROZLEGLE` | 422 | Poprawka Transkrypcji przekracza limit twardy ADR-036 (usunięcie kroku, > 2 kroki wstawione, krok wstawiony dłuższy niż limit 9.4, zbyt wiele zmienionych kroków lub znaków — kroki wstawione wliczone); `detail` podaje limit i wartość |
| `WYMAGANY_KOD_ZAPROSZENIA` | 422 | Rejestracja Ucznia bez kodu zaproszenia, gdy `LEMAT_REJESTRACJA_BEZ_KLASY = false` (A-14) |
| `LIMIT_OBCIAZENIA_WERYFIKATORA` | 422 | Nadanie flagi `weryfikator_globalny` Nauczycielowi, którego prognoza kolejki `KLASA` na dzień roboczy (17.7, 7/5 obciążenia dobowego) + `deklarowane_minuty_weryfikacji` > 180 min na dzień roboczy (A-14) |
| `WYMAGANY_IF_MATCH` | 428 | Brak `If-Match` |
| `LIMIT_PRZEKROCZONY` | 429 | Limit szybkości (nagłówek `Retry-After`) |
| `BLAD_WEWNETRZNY` | 500 | Nieobsłużony wyjątek; `instance` zawiera `trace_id`, bez szczegółów technicznych |
| `USLUGA_NIEDOSTEPNA` | 503 | Baza, magazyn obiektów lub bucket `rejestr-zdarzen-rodo` niedostępne, albo piaskownica normalizacji zdjęć zajęta > 5 s (`Retry-After: 5`) |

### 12.3 Katalog endpointów

Kolumna „Rola”: `U` — Uczeń, `N` — Nauczyciel, `NR` — Nauczyciel z
uprawnieniem `recenzent_tresci`, `A` — Administrator, `*` — każdy
zalogowany, `—` — publiczny. Kolumna „ETag” — zasób, którego `version`
sprawdza `If-Match` (12.1); `—` = bez `If-Match`.

| Metoda | Ścieżka | Rola | ETag | Wymagania | Wejście → Wyjście |
|---|---|---|---|---|---|
| POST | `/auth/rejestracja` | — | — | FR-01 | `RejestracjaWejscie` → `201 SesjaWyjscie` (Nauczyciel: konto `OCZEKUJE_NA_ZATWIERDZENIE`, `201` bez tokenu) |
| POST | `/auth/logowanie` | — | — | FR-01 | `{email, haslo}` → `200 SesjaWyjscie` |
| POST | `/auth/odswiezenie` | cookie | — | FR-01 | — → `200 SesjaWyjscie` (rotacja refresh tokenu) |
| POST | `/auth/wylogowanie` | * | — | FR-01 | — → `204` |
| POST | `/auth/reset-hasla` | — | — | FR-01 | `{email}` → `202` (zawsze, bez ujawniania istnienia konta) |
| POST | `/auth/reset-hasla/potwierdzenie` | — | — | FR-01 | `{token, nowe_haslo}` → `204` |
| GET | `/uczniowie/ja` | U | — | FR-02 | → `UczenWyjscie` + ETag |
| PATCH | `/uczniowie/ja` | U | U | FR-02, FR-09, NFR-09 | `{poziom_matury?, termin_egzaminu?, wybrany_dzial_id?, zgoda_na_trening?}` → `UczenWyjscie` (skutki zmiany poziomu: 9.1; odwołanie zgody `zgoda_na_trening = false` → `kopia_treningowa.usun_po := now()` w tej samej transakcji, 16.4) |
| DELETE | `/uczniowie/ja` | U | — | NFR-09 | `{haslo}` + Idempotency-Key → `202` (Zdarzenie RODO `WNIOSEK_O_USUNIECIE` w rejestrze przed zmianą w bazie; okres karencji 30 dni, ADR-040, ADR-041, 16.4) |
| DELETE | `/nauczyciele/ja` | N | — | NFR-09 | `{haslo}` + Idempotency-Key → `202` (usunięcie konta Nauczyciela po 30 dniach, ADR-044; Administratorzy dostają `KLASA_DO_PRZEKAZANIA` dla każdej niezarchiwizowanej Klasy) |
| GET | `/uczniowie/ja/eksport-danych` | U | — | NFR-09 | → `200 application/zip` (dane osobowe, Rozwiązania, Oceny) |
| DELETE | `/uczniowie/ja/klasy/{klasaId}` | U | — | FR-30 | → `204` (opuszczenie Klasy; otwarte Weryfikacje tej Klasy pozostają u Nauczyciela docelowego) |
| POST | `/uczniowie/ja/test-diagnostyczny` | U | — | FR-03 | — → `201 TestDiagnostycznyWyjscie` (lub `200` istniejący `W_TOKU`; `409 TEST_JUZ_WYKONANY`; `409 TEST_NIEDOSTEPNY`, 9.1) |
| GET | `/testy-diagnostyczne/{id}` | U | — | FR-03 | → `TestDiagnostycznyWyjscie` |
| POST | `/testy-diagnostyczne/{id}/pozycje/{pozycjaId}/odpowiedz` | U | — | FR-03 | `{wybor}` → `201 {czy_zapisano: true}` (bez ujawniania poprawności do końca testu) |
| POST | `/testy-diagnostyczne/{id}/zakonczenie` | U | — | FR-03 | — → `202 TestDiagnostycznyWyjscie` (Profil aktualizuje `AKTUALIZUJ_PROFIL`, 9.1) |
| POST | `/testy-diagnostyczne/{id}/pominiecie` | U | — | FR-03 | — → `204` (D-14; Odpowiedzi bez Wyników zadania) |
| GET | `/uczniowie/{uczenId}/profil` | U (`ja`), N (Uczeń swojej Klasy) | — | FR-04, FR-31 | → `ProfilUczniaWyjscie` |
| GET | `/uczniowie/ja/rekomendacje/nastepna` | U | — | FR-05…FR-10 | → `200 RekomendacjaWyjscie` |
| GET | `/uczniowie/ja/sciezka-nauki` | U | — | FR-05 | `?kursor` → strona `ElementSciezkiWyjscie` |
| POST | `/elementy-sciezki/{id}/rozpoczecie` | U | — | FR-05, FR-16 | — → `200 ElementSciezkiWyjscie` (idempotentne; ustala `zadanie_wersja_id`) |
| POST | `/elementy-sciezki/{id}/pominiecie` | U | — | FR-05 | — → `204` |
| POST | `/elementy-sciezki/{id}/przeczytanie` | U | — | FR-06 | — → `204` (tylko `WKLADKA_TEORETYCZNA`) |
| GET | `/wkladki-teoretyczne` | NR, A | — | FR-06 | `?status=DO_PRZEGLADU&umiejetnosc_id` → strona |
| GET | `/wkladki-teoretyczne/{id}` | * | — | FR-06 | → `WkladkaWyjscie` + ETag (U: tylko `OPUBLIKOWANA`) |
| PUT | `/wkladki-teoretyczne/{id}` | NR, A | WT | FR-06 | `{tytul, tresc_md, szacowany_czas_min, status}` → `200` |
| GET | `/dzialy`, `/umiejetnosci` | * | — | FR-04, FR-36 | → listy referencyjne |
| GET | `/zadania/{id}` | U, N, A | — | FR-15, FR-16 | U → `ZadanieUczniaWyjscie` (bez Rozwiązania wzorcowego i klucza); N/A → `ZadaniePelneWyjscie` + ETag |
| POST | `/zadania/{id}/odpowiedzi` | U | — | FR-15, FR-29 | `OdpowiedzWejscie` + Idempotency-Key → `201 OdpowiedzWyjscie` |
| POST | `/rozwiazania` | U | — | FR-16 | multipart `RozwiazanieWejscie` + Idempotency-Key → `202 RozwiazanieWyjscie` |
| GET | `/rozwiazania` | U | — | FR-24 | `?status&kursor` → strona `RozwiazanieWyjscie` (własne) |
| GET | `/rozwiazania/{id}` | U (własne), N (Uczeń Klasy / Weryfikacja) | — | FR-17, FR-24 | → `RozwiazanieWyjscie` + ETag |
| GET | `/rozwiazania/{id}/zdjecia/{zdjecieId}` | U (własne), N | — | FR-16, FR-33 | `?wersja_zdjecia=ORYGINAL\|MASKOWANE` (domyślnie `ORYGINAL`) → `302` na podpisany URL (ważny 10 min); `410` po usunięciu; obcy weryfikator globalny dostaje wyłącznie wersję maskowaną (11.4.4) |
| PUT | `/rozwiazania/{id}/transkrypcja` | U | R | FR-18 | `TranskrypcjaWejscie` → `200 RozwiazanieWyjscie` (z `transkrypcja.zakres_poprawek`) / `422 POPRAWKI_ZBYT_ROZLEGLE` (ADR-036) |
| POST | `/rozwiazania/{id}/zatwierdzenie` | U | R | FR-18, FR-19 | — → `202 RozwiazanieWyjscie` |
| POST | `/rozwiazania/{id}/zdjecia` | U | R | FR-16 | multipart `zdjecia[]` → `202` (nowe przesłanie; `409 LIMIT_PRZESLAN`) |
| POST | `/rozwiazania/{id}/ponowienie-rozpoznania` | U | R | FR-17 | — → `202` (te same zdjęcia; `409 LIMIT_PONOWIEN_ROZPOZNANIA`) |
| POST | `/rozwiazania/{id}/porzucenie` | U | R | FR-16 | — → `204` (element pozostaje aktywny do `wygasa_at`) |
| POST | `/rozwiazania/{id}/zgloszenie-bledu-odczytu` | U | R | FR-28 | `ZgloszenieBleduOdczytuWejscie` + Idempotency-Key → `201 {weryfikacja: WeryfikacjaUcznia}` |
| GET | `/rozwiazania/{id}/ocena` | U, N | — | FR-24, FR-27 | `?historia=true` → `OcenaWyjscie` lub lista wersji |
| POST | `/rozwiazania/{id}/korekty` | N | R | FR-34 | `RozstrzygniecieWejscie` → `201 OcenaWyjscie` (9.6.3) / `409 KONTO_UCZNIA_NIEAKTYWNE` |
| POST | `/klasy` | N | — | FR-30 | `{nazwa, poziom_matury}` → `201 KlasaWyjscie` (z `kod_zaproszenia`) |
| GET | `/klasy` | N | — | FR-30 | `?zarchiwizowane=false` → lista `KlasaWyjscie` |
| GET | `/klasy/{id}` | N | — | FR-30 | → `KlasaWyjscie` + członkowie + ETag |
| POST | `/klasy/{id}/kod-zaproszenia` | N | K | FR-30 | — → `200 {kod_zaproszenia, kod_wazny_do, link}` (stary kod unieważniony) |
| POST | `/klasy/{id}/archiwizacja` | N | K | FR-30 | — → `200 KlasaWyjscie` (kod zaproszenia unieważniony; Klasa nie uczestniczy w przydziale 11.4.1 p. 2; Przypisania i Weryfikacje pozostają) |
| POST | `/klasy/dolaczenie` | U | — | FR-30 | `{kod_zaproszenia}` → `200 KlasaSkrot` (Klasa niezarchiwizowana, kod ważny) |
| DELETE | `/klasy/{id}/uczniowie/{uczenId}` | N | — | FR-30 | → `204` |
| GET | `/klasy/{id}/postepy` | N | — | FR-31 | → `PostepyKlasyWyjscie` |
| GET | `/klasy/{id}/uczniowie/{uczenId}/rozwiazania` | N | — | FR-31 | `?kursor` → strona `RozwiazanieWyjscie` |
| POST | `/zestawy-zadan` | N | — | FR-32 | `{nazwa, zadanie_ids[1..30]}` → `201 ZestawZadanWyjscie` |
| GET | `/zestawy-zadan` | N | — | FR-32 | → lista `ZestawZadanWyjscie` (własne) |
| GET | `/zestawy-zadan/{id}` | N | — | FR-32 | → `ZestawZadanWyjscie` + ETag |
| PUT | `/zestawy-zadan/{id}` | N | S | FR-32 | `{nazwa, zadanie_ids[1..30]}` → `200` (zmiana nie wpływa na istniejące Przypisania — pozycje są czytane w chwili rekomendacji; usunięte pozycje znikają z otwartych Przypisań) |
| DELETE | `/zestawy-zadan/{id}` | N | S | FR-32 | → `204` / `409 ZESTAW_UZYWANY` |
| POST | `/przypisania` | N | — | FR-32 | `{zestaw_zadan_id, klasa_id?, uczen_ids?, termin}` + Idempotency-Key → `201` |
| GET | `/przypisania` | N | — | FR-32 | `?klasa_id&aktywne=true&kursor` → strona `PrzypisanieWyjscie` (własne) |
| GET | `/przypisania/{id}/postep` | N | — | FR-31, FR-32 | → `PostepPrzypisaniaWyjscie` |
| GET | `/uczniowie/ja/przypisania` | U | — | FR-32 | → lista otwartych Przypisań z pozycjami |
| GET | `/zadania` | N, A | — | FR-14, FR-32, FR-36 | `?dzial_id&umiejetnosc_id&rodzaj&poziom_trudnosci&pochodzenie&zasieg&status&kursor` → strona `ZadaniePelneWyjscie` (widoczność 12.8) |
| POST | `/zadania` | N | — | FR-36 | `ZadanieWejscie` → `201` (Zadanie bazowe `BAZOWE_NAUCZYCIEL`, `PRYWATNY`, `OPUBLIKOWANE` po walidacji W-1, W-2, W-8) |
| POST | `/zadania/generowanie` | N, A | — | FR-11 | `{umiejetnosc_id, poziom_trudnosci, poziom_matury, rodzaj}` + Idempotency-Key → `202 {zlecenie_generowania_id}` |
| GET | `/zlecenia-generowania/{id}` | N, A | — | FR-11 | → `{status, liczba_cykli, zadanie_id, walidacja}` |
| POST | `/zadania/{id}/przeglad` | N (zlecający lub autor wg 8.4), NR, A | Z | FR-14 | `PrzegladWejscie` → `200 ZadaniePelneWyjscie` |
| POST | `/zadania/{id}/wycofanie` | N (autor lub zlecający, `PRYWATNY`), NR, A | Z | FR-14 | `{powod}` → `200 ZadaniePelneWyjscie` |
| POST | `/zadania/{id}/zgloszenia-bledu` | N | — | FR-14 | `ZgloszenieBleduZadaniaWejscie` + Idempotency-Key → `201 ZgloszenieBleduZadaniaWyjscie` (tylko `GLOBALNY` i `OPUBLIKOWANE`; `409 ZGLOSZENIE_ZADANIA_ISTNIEJE`; 3 otwarte od różnych Nauczycieli → wiersz `wstrzymanie_w_doborze`, bez zmiany `zadanie.version`, ADR-038) |
| GET | `/zgloszenia-bledu-zadan` | N (własne), NR, A | — | FR-14 | `?status=OTWARTE&zadanie_id&kursor` → strona `ZgloszenieBleduZadaniaWyjscie` (sortowanie: `created_at`) |
| POST | `/zgloszenia-bledu-zadan/{id}/rozpatrzenie` | NR, A | Z | FR-14 | `RozpatrzenieZgloszeniaWejscie` → `200 ZgloszenieBleduZadaniaWyjscie` (`UWZGLEDNIONE` wymaga `przeglad` albo `wycofanie` w tej samej transakcji; powiadomienie zgłaszającego) |
| GET | `/weryfikacje` | N | — | FR-33 | `?dostepnosc=DOSTEPNE\|MOJE_W_TRAKCIE\|WSZYSTKIE&powod&klasa_id&typ_kolejki&kursor` → strona `WeryfikacjaNaLiscie` (sortowanie: priorytet, termin_at) |
| GET | `/weryfikacje/{id}` | N | — | FR-33 | → `WeryfikacjaSzczegoly` + ETag |
| POST | `/weryfikacje/{id}/podjecie` | N | — | FR-33 | — → `200 WeryfikacjaSzczegoly` + ETag / `409 WERYFIKACJA_ZAJETA` |
| POST | `/weryfikacje/{id}/przedluzenie` | N | — | FR-33 | — → `200 {zablokowana_do, version}` + ETag (wersja treści, niezmieniona przez przedłużenie, ADR-037) / `409 WERYFIKACJA_NIE_PODJETA` / `409 BLOKADA_MAKS_CZAS` |
| PUT | `/weryfikacje/{id}/szkic` | N | — | FR-33 | `SzkicRozstrzygnieciaWejscie` → `204` (tylko właściciel blokady) |
| POST | `/weryfikacje/{id}/zwolnienie` | N | — | FR-33 | — → `204` (szkic zachowany) |
| POST | `/weryfikacje/{id}/rozstrzygniecie` | N | W | FR-34 | `RozstrzygniecieWejscie` → `200 OcenaWyjscie` / `412 KONFLIKT_WERSJI` tylko po zmianie treści (ciało: aktualny zasób + `zmiany_tresci`, 8.3) / `409 KONTO_UCZNIA_NIEAKTYWNE` (karencja, C3-08) |
| GET | `/powiadomienia` | * | — | FR-24, FR-33 | `?nieprzeczytane=true` → lista |
| POST | `/powiadomienia/{id}/przeczytanie` | * | — | — | → `204` |
| POST | `/admin/arkusze-cke/importy` | A | — | FR-35 | multipart ZIP (`ArkuszImportV1`) → `202 {import_id}` |
| GET | `/admin/arkusze-cke/importy/{id}` | A | — | FR-35 | → `{status, raport}` |
| PUT | `/admin/zadania/{id}/diagnostyczne` | A | Z | FR-03 | `{diagnostyczne: boolean}`, `?wymus=true` → `200` (tylko `ZAMKNIETE`, `GLOBALNY`, `OPUBLIKOWANE`; pula 9.1; `409 PULA_DIAGNOSTYCZNA_ZA_MALA`) |
| GET | `/admin/pula-diagnostyczna` | A | — | FR-03 | → `[{dzial_id, poziom_matury, poziom_trudnosci_nominalny, liczba}]` (G-5) |
| GET | `/admin/uzytkownicy` | A | — | — | `?rola&status&kursor` → strona |
| PATCH | `/admin/uzytkownicy/{id}` | A | U | — | `{status?, weryfikator_globalny?, recenzent_tresci?, deklarowane_minuty_weryfikacji?}` → `200` / `422 LIMIT_OBCIAZENIA_WERYFIKATORA` (A-14) |
| POST | `/admin/uzytkownicy/{id}/przywrocenie` | A | U | NFR-09 | — → `200` (konto dowolnej roli w stanie `USUWANE`, < 30 dni; Zdarzenie RODO `ANULOWANIE_USUNIECIA`; wznowienie procesów wg tabel przywrócenia 16.4 — osobno dla Ucznia oraz dla Nauczyciela i Administratora, C3-08; `409 KONTO_NIE_DO_PRZYWROCENIA`) |
| POST | `/admin/uzytkownicy/{id}/usuniecie` | A | U | NFR-09 | `{powod}` → `202` (konto Nauczyciela lub innego Administratora; okres karencji 30 dni, ADR-044; `409 OSTATNI_ADMINISTRATOR`) |
| POST | `/admin/klasy/{id}/przekazanie` | A | K | FR-30 | `{nauczyciel_id}` → `200 KlasaWyjscie` (nowy Nauczyciel prowadzący: Klasa, jej otwarte Przypisania, Zadania `PRYWATNY` użyte w tych Przypisaniach, Weryfikacje `KLASA` z `nauczyciel_docelowy_id` = poprzedni; ADR-044) |
| GET | `/admin/konfiguracje-modeli` | A | — | NFR-06 | → lista |
| POST | `/admin/konfiguracje-modeli` | A | — | NFR-06 | `KonfiguracjaModeluWejscie` → `201` (nieaktywna) |
| POST | `/admin/konfiguracje-modeli/{id}/aktywacja` | A | — | NFR-06 | — → `200` / `409 BRAK_PROGU_DLA_KONFIGURACJI` / `409 BRAK_RETENCJI_DOSTAWCY` |
| POST | `/admin/konfiguracje-modeli/{id}/ewaluacja-zt1` | A | — | NFR-02, NFR-04 | — → `202` (praca `EWALUUJ_ZT1`: kalibrator + `prog_pewnosci` `EWALUACJA_ZT1`, raport G-1/G-2) |
| GET | `/admin/progi-pewnosci` | A | — | FR-34 | → lista z historią (z kalibratorem) |
| POST | `/admin/progi-pewnosci` | A | — | FR-34 | `{konfiguracja_modelu_id, rodzaj_zadania, wartosc, uzasadnienie}` → `201` (ręczne ustawienie, zawsze ≥ wartości wyznaczonej przez 11.5.3 i bezpiecznik; audyt) |
| GET | `/admin/kalibracja` | A | — | NFR-02…NFR-04 | → `MetrykiKalibracjiWyjscie` |
| GET | `/admin/bezpieczniki` | A | — | NFR-03 | → `StanBezpiecznikaWyjscie[]` (ADR-046) |
| POST | `/admin/bezpieczniki/dezaktywacja` | A | — | NFR-03 | `DezaktywacjaBezpiecznikaWejscie {konfiguracja_modelu_id, rodzaj_zadania, typ: "POPRAWKI" \| "TRAFNOSC", uzasadnienie (10–1000 znaków)}` → `200 StanBezpiecznikaWyjscie` (`aktywny = false`, `okno_od = dezaktywowano_at = now()`; audyt `BEZPIECZNIK_DEZAKTYWOWANY`) / `409 BEZPIECZNIK_NIEAKTYWNY` / `422 WALIDACJA` (C4-04; ścieżka `/admin/bezpieczniki/zdjecie` z wersji 2.3 nie istnieje) |
| GET | `/admin/obciazenie-weryfikacji` | A | — | FR-33 | → `ObciazenieWeryfikacjiWyjscie` (model 17.7, G-7) |
| POST | `/admin/wkladki-teoretyczne/szkice` | A | — | FR-06 | `{umiejetnosc_id}` → `202` (szkic Agenta generującego) |
| GET | `/admin/praca-w-tle` | A | — | — | `?status=MARTWA&typ` → strona |
| POST | `/admin/praca-w-tle/{id}/ponowienie` | A | — | — | — → `202` |

### 12.4 Typy DTO (TypeScript, źródło: modele Pydantic)

```ts
type Uuid = string;
type Czas = string; // ISO-8601 UTC
type PoziomMatury = "PODSTAWOWY" | "ROZSZERZONY";
type RodzajZadania = "ZAMKNIETE" | "OTWARTE_OBLICZENIOWE" | "OTWARTE_Z_DOWODEM";
type StatusRozwiazania = "PRZESLANE" | "ROZPOZNAWANIE" | "BLAD_ROZPOZNAWANIA" | "TRANSKRYPCJA_GOTOWA"
  | "ZATWIERDZONE" | "OCENIANIE" | "OCZEKUJE_NA_WERYFIKACJE" | "OCENIONE" | "PORZUCONE" | "ANULOWANE";
type StatusOceny = "WSTEPNA" | "OSTATECZNA" | "ZASTAPIONA";
type TypBledu = "RACHUNKOWY" | "MERYTORYCZNY" | "NIEPELNE_ROZWIAZANIE";
type PowodWeryfikacji = "NISKA_PEWNOSC" | "POPRAWNY_WYNIK_BLEDNE_KROKI" | "ZADANIE_Z_DOWODEM"
  | "PODEJRZENIE_BLEDU_ODCZYTU" | "ZGLOSZENIE_BLEDU_ODCZYTU" | "POPRAWKI_NIEZGODNE_ZE_ZDJECIEM"
  | "KONTROLA_LOSOWA" | "BLAD_PRZETWARZANIA";
type Regula = "R-PRZYPISANIE" | "R-SERIA" | "R-POWTORKA" | "R-WKLADKA" | "R-WKLADKA-DIAG" | "R-NAJSLABSZA"
  | "R-TRUDNOSC_W_GORE" | "R-UTRWALENIE" | "R-JEV-WKLADKA" | "R-JEV-ZADANIE_LATWE"
  | "R-JEV-ZADANIE_MATURALNE" | "R-JEV-POWTORKA";

interface RejestracjaWejscie {
  email: string;                 // RFC 5322, ≤ 254
  haslo: string;                 // 10–128 znaków, sprawdzane z listą 100k najczęstszych haseł
  imie: string; nazwisko: string;
  rola: "UCZEN" | "NAUCZYCIEL";
  oswiadczenie_wieku_16: true;   // wymagane dla UCZEN
  zgoda_regulamin: true;         // regulamin zawiera informację o kolejce globalnej i pseudonimizacji (16.3)
  kod_zaproszenia?: string;      // UCZEN: wymagany, gdy LEMAT_REJESTRACJA_BEZ_KLASY = false (A-14);
                                 // brak → 422 WYMAGANY_KOD_ZAPROSZENIA; poprawny → dołączenie do Klasy w tej samej transakcji
}
interface SesjaWyjscie { access_token: string; wygasa_at: Czas; uzytkownik: { id: Uuid; rola: string; imie: string } }

interface UczenWyjscie {
  id: Uuid; email: string; imie: string; nazwisko: string;
  poziom_matury: PoziomMatury | null; termin_egzaminu: string | null; // YYYY-MM-DD
  wybrany_dzial_id: number | null; zgoda_na_trening: boolean;
  test_diagnostyczny: "BRAK" | "W_TOKU" | "ZAKONCZONY" | "POMINIETY" | "NIEDOSTEPNY";  // dla bieżącego poziomu matury;
                                                    // NIEDOSTEPNY: brak testu i pula < 50% (9.1)
  klasy: { id: Uuid; nazwa: string; nauczyciel: string }[];
  version: number;
}

interface TestDiagnostycznyWyjscie {
  id: Uuid; poziom_matury: PoziomMatury; status: "W_TOKU" | "ZAKONCZONY" | "POMINIETY";
  pozycje: { id: Uuid; kolejnosc: number; zadanie: ZadanieUczniaWyjscie; odpowiedz_zapisana: boolean;
             rezultat: { czy_poprawna: boolean; punkty: number; punkty_max: number } | null }[];  // rezultat po zakończeniu
  pominiete_dzialy: { id: number; nazwa: string }[];          // brak Zadań diagnostycznych (9.1)
  created_at: Czas; zakonczono_at: Czas | null;
}

interface ProfilUczniaWyjscie {
  uczen_id: Uuid; poziom_matury: PoziomMatury; updated_at: Czas;
  oczekujace_wyniki: number;      // Wyniki zadania jeszcze niezastosowane (np. tuż po teście)
  dzialy: {
    id: number; kod: string; nazwa: string; poziom_opanowania: number; // 0..1
    umiejetnosci: { id: number; kod: string; nazwa: string; poziom_opanowania: number;
                    liczba_wynikow: number; nastepna_powtorka_at: Czas | null }[];
  }[];
}

interface ZadanieUczniaWyjscie {
  id: Uuid; zadanie_wersja_id: Uuid; rodzaj: RodzajZadania;
  forma_zamknieta: "WYBOR_JEDNOKROTNY" | "PRAWDA_FALSZ" | null;
  tresc_md: string; zalaczniki: { url: string; tekst_alternatywny: string }[];
  opcje: { id: string; tresc_md: string }[] | null;          // WYBOR_JEDNOKROTNY
  stwierdzenia: { id: string; tresc_md: string }[] | null;   // PRAWDA_FALSZ
  punkty_max: number; poziom_trudnosci: number;              // skalibrowany
  dzial: { id: number; nazwa: string }; umiejetnosci: { id: number; nazwa: string; glowna: boolean }[];
}
interface ZadaniePelneWyjscie extends ZadanieUczniaWyjscie {
  pochodzenie: "BAZOWE_ARKUSZ_CKE" | "BAZOWE_NAUCZYCIEL" | "WYGENEROWANE";
  zasieg: "GLOBALNY" | "PRYWATNY";
  status: "WALIDACJA" | "ODRZUCONE_W_WALIDACJI" | "DO_PRZEGLADU" | "OPUBLIKOWANE" | "ODRZUCONE" | "WYCOFANE";
  nr_wersji: number; version: number;
  poziom_trudnosci_nominalny: number; liczba_wynikow_kalibracji: number;
  opcje_zamkniete: object | null; rozwiazanie_wzorcowe: object; wynik_koncowy_wzorcowy: object | null;
  kryteria_oceniania: { wariant: number; nr_kryterium: number; opis: string; punkty_max: number;
                        czy_wynik_koncowy: boolean; wymaga_nr_kryterium: number | null }[];
  arkusz_cke: { kod: string; numer_w_arkuszu: string } | null;
  walidacja: { rezultat: "POZYTYWNY" | "NEGATYWNY"; sprawdzenia: object } | null;
  zweryfikowane_przez_nauczyciela: boolean;
  wstrzymane_w_doborze: boolean;        // wyliczane: istnieje wiersz wstrzymanie_w_doborze (ADR-038, C3-09)
  liczba_otwartych_zgloszen_bledu: number;
  uprawnienia: { przeglad: boolean; wycofanie: boolean; zgloszenie_bledu: boolean };   // dla wywołującego (ADR-028, ADR-038)
}

interface ZgloszenieBleduZadaniaWejscie { opis: string; zadanie_wersja_id: Uuid }   // opis 10–1000 znaków
interface ZgloszenieBleduZadaniaWyjscie {
  id: Uuid; zadanie_id: Uuid; zadanie_wersja_id: Uuid; opis: string;
  status: "OTWARTE" | "UWZGLEDNIONE" | "ODRZUCONE";
  nauczyciel: { id: Uuid; imie: string; nazwisko: string } | null;   // null po usunięciu konta
  komentarz_rozpatrzenia: string | null; created_at: Czas; rozpatrzono_at: Czas | null;
}
interface RozpatrzenieZgloszeniaWejscie {
  decyzja: "UWZGLEDNIONE" | "ODRZUCONE";
  komentarz: string;                                         // wymagany, ≤ 1000
  przeglad?: PrzegladWejscie;                                // UWZGLEDNIONE: Przegląd POPRAWIONE …
  wycofanie?: { powod: string };                             // … albo wycofanie Zadania (dokładnie jedno)
  rozpatrz_wszystkie_otwarte: boolean;                       // true: ta decyzja zamyka wszystkie otwarte zgłoszenia Zadania
}

interface ElementSciezkiWyjscie {
  id: Uuid; typ: "WKLADKA_TEORETYCZNA" | "ZADANIE"; cel: "NAUKA" | "POWTORKA"; w_serii: boolean;
  status: "PROPONOWANY" | "ROZPOCZETY" | "UKONCZONY" | "POMINIETY" | "WYGASLY";
  umiejetnosc: { id: number; kod: string; nazwa: string; dzial: { id: number; nazwa: string } };
  zadanie: ZadanieUczniaWyjscie | null;                       // w wersji element.zadanie_wersja_id, gdy ustalona
  wkladka_teoretyczna: { id: Uuid; tytul: string; szacowany_czas_min: number } | null;
  przypisanie: { id: Uuid; termin: Czas } | null;
  rekomendacja: { regula: Regula; uzasadnienie: string; przewidywane_p_sukcesu: number | null };
  wygasa_at: Czas; created_at: Czas; rozpoczeto_at: Czas | null;
}
interface RekomendacjaWyjscie {
  element: ElementSciezkiWyjscie | null;
  brak_elementu: { kod: "BRAK_ZADAN_W_ZAKRESIE"; komunikat: string;
                   sugerowane_dzialy: { id: number; nazwa: string }[] } | null;   // dokładnie jedno z pól ≠ null
}

interface OdpowiedzWejscie {
  zadanie_wersja_id: Uuid;
  element_sciezki_id?: Uuid; przypisanie_id?: Uuid;           // klient podaje dokładnie jedno (ADR-032); przypisanie_id
                                                              // elementu R-PRZYPISANIE serwer dziedziczy (trg_kontekst_proby)
  wybor: { opcja: string } | { stwierdzenia: Record<string, boolean> };
}
interface OdpowiedzWyjscie {
  id: Uuid; element_sciezki_id: Uuid | null; przypisanie_id: Uuid | null;   // przypisanie_id także odziedziczone
  rezultat_ujawniony_od: Czas;
  rezultat: { czy_poprawna: boolean; punkty: number; punkty_max: number;
              poprawna: { opcja: string } | { stwierdzenia: Record<string, boolean> } } | null;  // null przed terminem Przypisania
}

// multipart/form-data
interface RozwiazanieWejscie {
  zadanie_wersja_id: Uuid;
  element_sciezki_id?: Uuid; przypisanie_id?: Uuid;           // dokładnie jedno wymagane (ADR-032)
  zdjecia: File[];                                            // 1..5, każde ≤ 10 MB, JPEG/PNG/HEIC/WEBP
}

interface NiepewnyFragment { zdjecie: number; ramka: [number, number, number, number]; odczyt: string; alternatywy: string[] }
interface KrokTranskrypcji {
  nr: number; tekst: string; latex: string; pewnosc_odczytu: number | null;   // null: krok zmieniony przez Ucznia
  niepewne_fragmenty: NiepewnyFragment[]; zmieniony_przez_ucznia: boolean;
  nr_kroku_agenta: number | null;       // ADR-036: krok agenta, który ten krok poprawia; null = wstawiony
  wymaga_sprawdzenia: boolean;          // pewnosc_odczytu < 0.80 lub podejrzenie_danych_osobowych
  podejrzenie_danych_osobowych: boolean; tylko_dane_osobowe: boolean;          // Filtr 10.3.1 p. 3a (C3-02)
  zastapienia_danych_osobowych: { od: number; do: number; rodzaj: "DOKLADNE" | "PRZYBLIZONE" }[];
                                        // pozycje znacznika [DANE_OSOBOWE] w tekst — UI: „przywróć słowo”
}
interface StanBezpiecznikaWyjscie {     // ADR-046
  konfiguracja_modelu_id: Uuid; rodzaj_zadania: RodzajZadania; typ: "TRAFNOSC" | "POPRAWKI";
  aktywny: boolean; okno_od: Czas; n_eff: number | null; blad_ipw: number | null;
  dolna_granica_bledu: number | null; gorna_granica_bledu: number | null;
  ostatnie_wyzwolenie_at: Czas | null; wyjscie_z_blokady_at: Czas | null;   // TRAFNOSC: aktywny = Blokada obniżania progu (C4-02)
  dezaktywowano_at: Czas | null; uzasadnienie_dezaktywacji: string | null;   // nazwy od 2.4 (C4-04)
  prog_aktywny: number | null;                                              // wartość aktywnego progu pary (1.000 = tryb pełnej weryfikacji)
}
interface DezaktywacjaBezpiecznikaWejscie {   // POST /admin/bezpieczniki/dezaktywacja (C4-04)
  konfiguracja_modelu_id: Uuid; rodzaj_zadania: "OTWARTE_OBLICZENIOWE"; typ: "TRAFNOSC" | "POPRAWKI";
  uzasadnienie: string;                       // 10..1000 znaków, zapis w stan_bezpiecznika.uzasadnienie_dezaktywacji i dziennik_audytu
}
interface ZakresPoprawek { zmienione: number; wstawione: number; kroki_agenta: number; odsetek_znakow: number;
                           limit_miekki_przekroczony: boolean }
interface TranskrypcjaWyjscie {
  id: Uuid; wersja: number; zrodlo: "AGENT_ROZPOZNAJACY" | "POPRAWKA_UCZNIA"; przeslanie_nr: number;
  zgodnosc_z_zadaniem: number | null; kroki: KrokTranskrypcji[]; created_at: Czas;
  zakres_poprawek: ZakresPoprawek | null;                 // tylko POPRAWKA_UCZNIA
  konflikt_filtra: boolean;                               // C3-02: krok z treścią matematyczną w ramce danych osobowych
  zgodnosc_poprawek: { niezgodne: number[]; sprawdzono_at: Czas } | null;   // tylko dla N/A (10.3.3)
}
interface TranskrypcjaWejscie {
  // ADR-036: lista kroków po Poprawkach; każdy krok agenta bieżącego przesłania występuje dokładnie raz,
  // w kolejności rosnących nr_kroku_agenta; kroki wstawione mają nr_kroku_agenta = null (≤ 2); n ≤ 40
  kroki: { nr_kroku_agenta: number | null; tekst: string; latex: string }[];
}

interface OcenaWyjscie {
  id: Uuid; wersja: number; status: StatusOceny; zrodlo: "AUTOMATYCZNA" | "NAUCZYCIEL";
  widocznosc_dla_ucznia: "PELNA" | "WSTRZYMANA_BLAD_ODCZYTU";
  punkty: number | null; punkty_max: number; wariant: number;  // punkty null tylko dla U przy WSTRZYMANA
  kryteria: { nr_kryterium: number; opis: string; punkty: number | null; punkty_max: number;
              prawdopodobienstwo: number | null }[];          // prawdopodobienstwo: tylko dla N/A
  pierwszy_bledny_krok: number | null; brakujacy_krok: boolean; typ_bledu: TypBledu | null;
  krok_podejrzany_odczyt: number | null; mozliwy_blad_odczytu: boolean;
  wskazowka: string | null; wskazowka_w_przygotowaniu: boolean;
  uzasadnienie: string;                                       // 11.7; dla U bez prawdopodobieństw
  pewnosc_oceny: number | null;                               // null dla roli U
  komunikat: string | null;                                   // 11.3
  mozliwe_zgloszenie_do: Czas | null;                         // koniec okna zgłoszenia (11.6), tylko U
  created_at: Czas;
}

interface WeryfikacjaUcznia {
  status: "OCZEKUJE" | "W_TRAKCIE" | "ZAKONCZONA" | "ANULOWANA";
  powody: PowodWeryfikacji[]; termin_at: Czas; opozniona: boolean;
}

interface RozwiazanieWyjscie {
  id: Uuid; zadanie_id: Uuid; zadanie_wersja_id: Uuid; status: StatusRozwiazania;
  element_sciezki_id: Uuid | null; przypisanie_id: Uuid | null;
  zdjecia: { id: Uuid; przeslanie_nr: number; kolejnosc: number; dostepne: boolean }[];
  liczba_przeslan: number; liczba_ponowien_rozpoznania: number;
  kod_bledu_rozpoznawania: "NIECZYTELNE" | "PRZEKROCZONY_CZAS" | "BLAD_DOSTAWCY" | "ZBYT_WIELE_KROKOW" | null;
  transkrypcja: TranskrypcjaWyjscie | null;               // najnowsza wersja
  ocena: OcenaWyjscie | null;                             // aktualna (status ≠ ZASTAPIONA)
  weryfikacja: WeryfikacjaUcznia | null;                  // otwarta lub ostatnia
  version: number; created_at: Czas; updated_at: Czas;
  nastepne_odpytanie_ms: number | null;
}

interface ZgloszenieBleduOdczytuWejscie { kroki: number[]; opis?: string }   // 1..5 kroków, opis ≤ 500

interface WeryfikacjaNaLiscie {
  id: Uuid; rozwiazanie_id: Uuid;
  uczen: { id: Uuid; imie: string; nazwisko: string } | null;   // null dla obcego weryfikatora globalnego
  uczen_pseudonim: string;                                       // zawsze, np. "U-4F2A9C"
  klasa: { id: Uuid; nazwa: string } | null;
  zadanie: { id: Uuid; rodzaj: RodzajZadania; dzial: string; numer_w_arkuszu: string | null };
  powody: PowodWeryfikacji[]; priorytet: 1 | 2 | 3; typ_kolejki: "KLASA" | "GLOBALNA";
  status: "OCZEKUJE" | "W_TRAKCIE" | "ZAKONCZONA" | "ANULOWANA";
  dostepna: boolean;                                             // 11.4.2
  podjeta_przez: { id: Uuid; imie: string; nazwisko: string } | null; zablokowana_do: Czas | null;
  termin_at: Czas; opozniona: boolean;
  ocena_weryfikowana: { punkty: number; punkty_max: number; pewnosc_oceny: number | null } | null;
  created_at: Czas;
}
interface WeryfikacjaSzczegoly extends WeryfikacjaNaLiscie {
  powody_poczatkowe: PowodWeryfikacji[];                  // 11.5.2
  zadanie_pelne: ZadaniePelneWyjscie;
  zdjecia: { id: Uuid; url: string; wygasa_at: Czas; maskowane: boolean }[];   // puste po usunięciu (NFR-09)
  transkrypcje: TranskrypcjaWyjscie[];                    // wszystkie wersje: agent + poprawki Ucznia
  ocena: OcenaWyjscie | null;                             // Ocena weryfikowana (null przy T-6 przed PONOW_OCENE)
  decyzje_modelu: { kod_pytania: string; typ_pytania: "NOUL" | "SCORE" | "CHOICE";
                    wybor: unknown; rozklad: Record<string, number> }[];
  opinia_pomocnicza: object | null;                       // 10.4.5
  zgloszenie: { kroki: number[]; opis: string | null; zgloszono_at: Czas } | null;
  szkic: Partial<RozstrzygniecieWejscie> | null;          // tylko szkic wywołującego
  version: number;                                        // wersja treści (ADR-037) = ETag
}
interface RozstrzygniecieWejscie {
  wariant: number;
  kryteria: { nr_kryterium: number; punkty: number }[];   // wszystkie kryteria wariantu
  pierwszy_bledny_krok: number | null;                    // nr kroku Transkrypcji
  brakujacy_krok: boolean;
  typ_bledu: TypBledu | null;                             // null ⇔ brak błędu
  wskazowka: { tryb: "WLASNA"; tekst: string }            // ≤ 400 znaków
           | { tryb: "AUTOMATYCZNA" }                     // Agent wyjaśniający na decyzjach Nauczyciela
           | { tryb: "ZACHOWAJ" }                         // tylko przy identycznym błędzie (9.6.2)
           | null;                                        // wymagane null ⇔ typ_bledu = null
  kroki_z_bledem_odczytu: number[];                       // do metryk Agenta rozpoznającego
  blad_odczytu_wplynal_na_ocene: boolean;                 // metryka 11.8
  komentarz?: string;                                     // ≤ 500, dopisywany do uzasadnienia
}
type SzkicRozstrzygnieciaWejscie = Partial<RozstrzygniecieWejscie>;

interface PrzegladWejscie {
  decyzja: "ZATWIERDZONE" | "POPRAWIONE" | "ODRZUCONE";
  komentarz?: string;
  zasieg_po?: "GLOBALNY" | "PRYWATNY";                   // GLOBALNY tylko NR/A (ADR-028)
  poprawki?: { tresc_md?: string; opcje_zamkniete?: object; rozwiazanie_wzorcowe?: object;
               wynik_koncowy_wzorcowy?: object | null; kryteria_oceniania?: object[];
               poziom_trudnosci_nominalny?: number };
}

interface ZestawZadanWyjscie { id: Uuid; nazwa: string; zadania: { kolejnosc: number; zadanie_id: Uuid; status: string }[];
                               liczba_przypisan: number; version: number }
interface PrzypisanieWyjscie { id: Uuid; zestaw: { id: Uuid; nazwa: string }; klasa: { id: Uuid; nazwa: string } | null;
                               termin: Czas; liczba_uczniow: number; liczba_ukonczonych: number; created_at: Czas }
interface PostepPrzypisaniaWyjscie {
  przypisanie_id: Uuid; termin: Czas;
  uczniowie: { uczen_id: Uuid; imie: string; nazwisko: string;
               pozycje: { zadanie_id: Uuid; kolejnosc: number;
                          stan: "BRAK_PROBY" | "W_TOKU" | "OCZEKUJE_NA_WERYFIKACJE" | "OCENIONE";
                          punkty: number | null; punkty_max: number;
                          droga: "LISTA_PRZYPISAN" | "SCIEZKA_NAUKI" | null }[] }[];
  // próba liczy się do postępu, gdy ma przypisanie_id tego Przypisania — z listy Przypisań albo odziedziczone
  // z elementu R-PRZYPISANIE (droga = SCIEZKA_NAUKI); jedna próba na pozycję (ux_*_przypisanie, C1-21)
}

interface PostepyKlasyWyjscie {
  klasa_id: Uuid;
  dzialy_srednio: { dzial_id: number; poziom_opanowania_sredni: number }[];
  uczniowie: { uczen_id: Uuid; imie: string; nazwisko: string; ostatnia_aktywnosc: Czas | null;
               liczba_wynikow_7d: number; sredni_ulamek_7d: number | null;
               dzialy: { dzial_id: number; poziom_opanowania: number }[];
               otwarte_weryfikacje: number }[];
}

interface MetrykiKalibracjiWyjscie {
  pozycje: { konfiguracja_modelu_id: Uuid; rola: string; rodzaj_zadania: RodzajZadania;
             prog: number; tryb_pelnej_weryfikacji: boolean; ece: number | null; liczba_probek_90d: number;
             odsetek_automatycznych_30d: number; blad_automatycznych_30d: number | null;
             gorna_granica_bledu_30d: number | null; zgodnosc_z_nauczycielem_30d: number | null;
             wykonanie_kontroli_30d: number | null; bledy_odczytu_niewykryte_30d: number | null;
             wykres_niezawodnosci: { koszyk: [number, number]; srednia_pewnosc: number; trafnosc: number; n: number }[] }[];
}

interface ObciazenieWeryfikacjiWyjscie {
  prognoza_min_dziennie: { klasa: number; globalna: number };         // 17.7
  zaleglosc_min: { klasa: number; globalna: number };
  pojemnosc_globalna_min: number; weryfikatorzy_globalni_aktywni_7d: number;
  wymagani_weryfikatorzy_globalni: number;                           // G-7, model tygodniowy 17.7
  odsetek_eskalacji_7d: number; naplyw_eskalacji_min_7d: number;     // A-15, 11.4.3
  klasy_z_kontrola_przyjec: { klasa_id: Uuid; odsetek_eskalacji_7d: number }[];
  kontrola_przyjec_aktywna: boolean;                                 // 11.4.3
  po_sla: { klasa: number; globalna: number };
}
```

### 12.5 Reguły walidacji wejścia (wybrane)

| Endpoint | Reguła | Błąd |
|---|---|---|
| `POST /rozwiazania`, `POST /zadania/{id}/odpowiedzi` | **Klient podaje dokładnie jeden kontekst**: `element_sciezki_id` (należy do Ucznia, `PROPONOWANY` lub `ROZPOCZETY`, dotyczy tego Zadania) albo `przypisanie_id` (obejmuje Ucznia i Zadanie). **Kontekst efektywny** (C1-21): dla elementu `R-PRZYPISANIE` serwer dziedziczy jego `przypisanie_id` (`trg_kontekst_proby`), więc obie drogi podlegają tym samym regułom Przypisania: Odpowiedź tylko przed terminem (po terminie klucz jest ujawniany Klasie); Rozwiązanie zadania otwartego przed terminem lub do 7 dni po nim (klucza się nie ujawnia); jedna próba na (Uczeń, Przypisanie, Zadanie) | `422 WALIDACJA` / `409 ELEMENT_SCIEZKI_NIEAKTYWNY` / `409 PRZYPISANIE_PO_TERMINIE` / `403` |
| jw. | **Wersja akceptowana**: dla elementu — `zadanie_wersja_id = element.zadanie_wersja_id` (ustalona przy rozpoczęciu; element `PROPONOWANY` jest rozpoczynany w tej samej transakcji z wersją aktualną); dla Przypisania — wersja aktualna albo wersja z `zastapiona_at > now() − 72 h` | `409 WERSJA_ZADANIA_NIEAKTUALNA` |
| jw. | Zadanie `OPUBLIKOWANE`; Zadanie `WYCOFANE` przyjmowane tylko dla elementu `ROZPOCZETY` | `409 ZADANIE_NIEDOSTEPNE` |
| `POST /rozwiazania` | Zadanie otwarte; brak innego nieporzuconego Rozwiązania dla tego elementu (`ux_rozwiazanie_element`) lub tej pozycji Przypisania — z dowolnej drogi (`ux_rozwiazanie_przypisanie` na kontekście efektywnym) | `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN` z `id` istniejącego w `detail` |
| `POST /zadania/{id}/odpowiedzi` | Zadanie `ZAMKNIETE`; dla `PRAWDA_FALSZ` wszystkie stwierdzenia ocenione; brak wcześniejszej Odpowiedzi w tym kontekście efektywnym (`ux_odpowiedz_element`, `ux_odpowiedz_przypisanie`) | `422` / `409 ELEMENT_SCIEZKI_NIEAKTYWNY` |
| `POST /rozwiazania` | ≤ 30 Rozwiązań/h na Ucznia | `429` |
| `PUT …/transkrypcja` | 1–40 kroków, `tekst`/`latex` ≤ 2000 znaków, co najmniej jedno z nich niepuste w każdym kroku; każdy krok agenta bieżącego przesłania dokładnie raz, w kolejności (bez usuwania); ≤ 2 kroki wstawione, każdy ≤ min(200, max(60, ⌈1,5 × mediana długości kroków agenta⌉)) znaków; limity twarde zakresu poprawek z 9.4 (zmienione + wstawione ≤ max(3, ⌈0,5 n⌉), odsetek znaków z krokami wstawionymi ≤ 0,50) | `422 WALIDACJA` / `422 POPRAWKI_ZBYT_ROZLEGLE` |
| `POST …/zatwierdzenie` | Stan `TRANSKRYPCJA_GOTOWA`, If-Match zgodny | `409` / `412` |
| `POST …/zgloszenie-bledu-odczytu` | warunki 11.6 | 11.6 |
| `…/rozstrzygniecie`, `…/korekty` | Konto Ucznia `AKTYWNE` (`409 KONTO_UCZNIA_NIEAKTYWNE`, C3-08); blokada należy do wywołującego i nie wygasła (rozstrzygnięcie); kryteria kompletne dla `wariant`; `0 ≤ punkty ≤ punkty_max`; `typ_bledu` null ⇔ `pierwszy_bledny_krok` null i `brakujacy_krok = false`; reguły Wskazówki 9.6.2; Ocena weryfikowana nadal aktualna; niepusty `komentarz`, gdy zdjęcia Rozwiązania są usunięte, a powody zawierają `PODEJRZENIE_BLEDU_ODCZYTU` lub `ZGLOSZENIE_BLEDU_ODCZYTU` (11.4.3 p. 3) | `409 WERYFIKACJA_NIE_PODJETA` / `422` / `409 OCENA_ZMIENIONA` |
| `POST /przypisania` | Dokładnie jedno z `klasa_id` / `uczen_ids`; Uczniowie należą do niezarchiwizowanych Klas Nauczyciela; `termin > now()`; Zadania Zestawu `OPUBLIKOWANE` i (`GLOBALNY` lub `PRYWATNY` autorstwa Nauczyciela); Zadania dodawane do Zestawu (`POST`/`PUT /zestawy-zadan`) nie mają wiersza `wstrzymanie_w_doborze` (ADR-038) | `422` / `403` |
| `POST /zadania/{id}/przeglad` | Uprawnienie wg 8.4; `zasieg_po = GLOBALNY` tylko NR/A | `403 BRAK_UPRAWNIEN` |

### 12.6 Przykład: odpowiedź `GET /rozwiazania/{id}` z Oceną wstępną wstrzymaną (rola U)

```json
{
  "id": "7c1e4b1a-3d2f-4c55-9a51-0f7d8b0e2a11",
  "zadanie_id": "0e4f…", "zadanie_wersja_id": "a91c…",
  "status": "OCZEKUJE_NA_WERYFIKACJE",
  "element_sciezki_id": "5b2d…", "przypisanie_id": null,
  "zdjecia": [ { "id": "c3d1…", "przeslanie_nr": 1, "kolejnosc": 1, "dostepne": true } ],
  "liczba_przeslan": 1, "liczba_ponowien_rozpoznania": 0, "kod_bledu_rozpoznawania": null,
  "transkrypcja": { "id": "e8f0…", "wersja": 1, "zrodlo": "AGENT_ROZPOZNAJACY", "przeslanie_nr": 1,
    "zgodnosc_z_zadaniem": 0.94,
    "kroki": [
      { "nr": 1, "tekst": "", "latex": "p=\\frac{-8}{-4}=2", "pewnosc_odczytu": 0.98,
        "niepewne_fragmenty": [], "zmieniony_przez_ucznia": false, "wymaga_sprawdzenia": false },
      { "nr": 2, "tekst": "wartość największa", "latex": "f(2)=-8+16-3=6", "pewnosc_odczytu": 0.74,
        "niepewne_fragmenty": [ { "zdjecie": 1, "ramka": [512, 940, 40, 52], "odczyt": "6", "alternatywy": ["5"] } ],
        "zmieniony_przez_ucznia": false, "wymaga_sprawdzenia": true } ],
    "created_at": "2026-10-31T18:01:40Z" },
  "ocena": { "id": "f1a2…", "wersja": 1, "status": "WSTEPNA", "zrodlo": "AUTOMATYCZNA",
    "widocznosc_dla_ucznia": "WSTRZYMANA_BLAD_ODCZYTU",
    "punkty": null, "punkty_max": 2, "wariant": 1,
    "kryteria": [ { "nr_kryterium": 1, "opis": "Obliczenie odciętej wierzchołka paraboli.", "punkty": null, "punkty_max": 1, "prawdopodobienstwo": null },
                  { "nr_kryterium": 2, "opis": "Podanie największej wartości funkcji.", "punkty": null, "punkty_max": 1, "prawdopodobienstwo": null } ],
    "pierwszy_bledny_krok": null, "brakujacy_krok": false, "typ_bledu": null,
    "krok_podejrzany_odczyt": 2, "mozliwy_blad_odczytu": true,
    "wskazowka": null, "wskazowka_w_przygotowaniu": false,
    "uzasadnienie": "Kryteria: K1 „Obliczenie odciętej wierzchołka paraboli.” — ocena w toku; K2 „Podanie największej wartości funkcji.” — ocena w toku. Możliwy błąd odczytu pisma w kroku 2 — punkty zostaną potwierdzone przez nauczyciela.",
    "pewnosc_oceny": null,
    "komunikat": "Ocena wstrzymana — możliwy błąd odczytu pisma w kroku 2. Nauczyciel porówna rozwiązanie ze zdjęciem. Błąd odczytu nie obniży Twojej oceny.",
    "mozliwe_zgloszenie_do": "2026-11-14T18:01:52Z",
    "created_at": "2026-10-31T18:01:52Z" },
  "weryfikacja": { "status": "OCZEKUJE", "powody": ["PODEJRZENIE_BLEDU_ODCZYTU"],
                   "termin_at": "2026-11-05T07:00:00Z", "opozniona": false },
  "version": 7, "created_at": "2026-10-31T18:01:02Z", "updated_at": "2026-10-31T18:01:52Z",
  "nastepne_odpytanie_ms": null
}
```

`termin_at` (C3-09): Ocena powstała w sobotę 2026-10-31 o 19:01:52 czasu
warszawskiego (CET, UTC+1), więc `dodaj_dni_robocze` zaczyna liczenie od
najbliższego dnia roboczego o 08:00 — poniedziałku 2026-11-02 (niedziela
1.11 jest dniem wolnym) — i dodaje 3 dni robocze: czwartek 2026-11-05
08:00 CET = 07:00Z.

Dla roli Uczeń przy `widocznosc_dla_ucznia = WSTRZYMANA_BLAD_ODCZYTU`
serializator zastępuje w uzasadnieniu punkty kryteriów tekstem „ocena w
toku”; Nauczyciel widzi tę samą Ocenę z punktami (np. 1/2) i
prawdopodobieństwami.

### 12.7 Limity szybkości

| Zakres | Limit | Mechanizm |
|---|---|---|
| Nieudane logowania | 5 / 15 min na e-mail, 20 / 15 min na IP | zapytanie do `proba_logowania` |
| `POST /rozwiazania` | 30 / h na Ucznia | `count(*)` po `ix_rozwiazanie_uczen` |
| `POST …/zgloszenie-bledu-odczytu` | 5 / 30 dni na Ucznia, 1 na Rozwiązanie | `count(*)` po `weryfikacja.zgloszenie` |
| `POST /zadania/generowanie` | 20 / dobę na Nauczyciela | `count(*)` po `zlecenie_generowania` |
| `POST /zadania/{id}/zgloszenia-bledu` | 10 / dobę na Nauczyciela | `count(*)` po `zgloszenie_bledu_zadania` |
| Generowanie systemowe (`UZUPELNIJ_PULE` i `GENERUJ_SZKIC_WKLADKI`) | 200 zleceń i szkiców / dobę i 150 PLN / dobę (`Σ koszt_pln` zleceń `systemowe` + `Σ koszt_generowania_pln` Wkładek `wygenerowana_automatycznie`), w tym ≤ 20 szkiców Wkładek | sprawdzenie w handlerze `UZUPELNIJ_PULE` (13.3); po przekroczeniu praca przesunięta na 00:00 UTC następnego dnia, alert przy 80% |
| `POST /auth/reset-hasla` | 3 / h na e-mail | `count(*)` po `praca_w_tle` typu `WYSLIJ_EMAIL` |
| Globalnie na IP | 600 żądań / min | Caddy `rate_limit` |

### 12.8 Uprawnienia na poziomie obiektów

| Zasób | Uczeń | Nauczyciel | Administrator |
|---|---|---|---|
| Profil ucznia, Rozwiązania, Oceny | tylko własne | Uczniowie jego Klas; Rozwiązania z Weryfikacji, które może podjąć (obcy weryfikator globalny: tylko to Rozwiązanie, z pseudonimem, 11.4.4) | wszystkie (odczyt, wpis w `dziennik_audytu`) |
| Zdjęcia Rozwiązania | własne | Uczniowie jego Klas i Nauczyciel docelowy: oryginał; obcy weryfikator globalny: wyłącznie wersja maskowana | brak dostępu (zasada minimalizacji) |
| Weryfikacje | własne (`WeryfikacjaUcznia` w Rozwiązaniu) | `nauczyciel_docelowy_id = ja`, `klasa_id` jego Klasy, lub (`typ_kolejki = GLOBALNA` i `weryfikator_globalny`) | odczyt |
| Klasy, Przypisania, Zestawy zadań | swoje członkostwa i Przypisania | własne | odczyt |
| Zadania (pełne) | brak | `GLOBALNY` + własne `PRYWATNY` + `DO_PRZEGLADU` ze swoich zleceń; NR: wszystkie | wszystkie |
| Przegląd i wycofanie Zadań | brak | wg 8.4 (ADR-028, ADR-038) | wszystkie |
| Zgłoszenia błędu w Zadaniu | brak | tworzenie dla Zadań `GLOBALNY`; odczyt własnych; NR: odczyt i rozpatrzenie wszystkich | wszystkie |
| Wkładki `DO_PRZEGLADU` | brak | NR | wszystkie |
| Konfiguracja modeli, progi, importy, obciążenie | brak | brak | pełny |

Realizacja: każde zapytanie repozytorium przyjmuje obiekt `Aktor` i dokłada
warunek widoczności w SQL (nie filtrowanie po pobraniu). Brak widoczności →
`404 NIE_ZNALEZIONO` (nie `403`), aby nie ujawniać istnienia zasobu.

### 12.9 Format importu `ArkuszImportV1` (FR-35)

Plik ZIP: `arkusz.json` + katalog `rys/` (PNG/SVG/JPEG). `arkusz.json`:

```json
{
  "wersja_formatu": 1,
  "arkusz": { "kod": "MMA-P1-2024-05", "rok": 2024, "sesja": "MAJ", "poziom_matury": "PODSTAWOWY" },
  "zadania": [
    {
      "numer": "7",
      "rodzaj": "ZAMKNIETE", "forma_zamknieta": "WYBOR_JEDNOKROTNY",
      "dzial_kod": "V",
      "umiejetnosci": [ { "kod": "P.V.9", "glowna": true } ],
      "latwosc_cke": 0.54,
      "tresc_md": "Funkcja $f$ określona jest wzorem …",
      "rysunki": [ { "plik": "rys/7.png", "tekst_alternatywny": "Wykres paraboli o wierzchołku (2, 5)" } ],
      "opcje_zamkniete": { "forma": "WYBOR_JEDNOKROTNY", "opcje": [ … ], "poprawna": "B" },
      "rozwiazanie_wzorcowe": { "warianty": [ { "wariant": 1, "opis": "…", "kroki": [ … ] } ] },
      "wynik_koncowy_wzorcowy": null,
      "punkty_max": 1,
      "kryteria_oceniania": []
    }
  ]
}
```

Kryterium w `kryteria_oceniania`: `{wariant, nr_kryterium, opis, punkty_max,
czy_wynik_koncowy, wymaga_nr_kryterium}`. Poziom trudności nominalny Zadań
bazowych z Arkusza CKE wynika ze wskaźnika łatwości ze sprawozdania CKE
(`latwosc_cke`): `≥ 0.8 → 1`, `[0.6, 0.8) → 2`, `[0.4, 0.6) → 3`,
`[0.2, 0.4) → 4`, `< 0.2 → 5`; gdy brak wskaźnika — pole
`poziom_trudnosci_nominalny` (1–5) jest wymagane. Import tworzy też wiersz
`kalibracja_trudnosci` (`trudnosc_b = 0.8 × (poziom − 3)`,
`poziom_trudnosci` = nominalny). Import jest transakcyjny per arkusz
(wszystko albo nic); ponowny import tego samego `kod` → `422` z raportem.

---

## 13. Kontrakty wewnętrzne (porty) i prace w tle

### 13.1 Porty modeli (NFR-06)

```python
# lemat/modele/porty.py
class PytanieNoul(BaseModel):   kod: str; tresc: str
class PytanieScore(BaseModel):  kod: str; tresc: str; min: int; max: int          # max - min ≤ 10
class PytanieChoice(BaseModel): kod: str; tresc: str; opcje: conlist(str, min_length=2, max_length=42)
Pytanie = PytanieNoul | PytanieScore | PytanieChoice

class WarunekPytania(BaseModel):
    kod_nadrzedny: str                       # pytanie bezwarunkowe z tego samego planu
    wartosci: list[str]                      # pytanie jest zadawane, gdy argmax rozkładu nadrzędnego ∈ wartosci
    pomin_opcje: list[str] = []              # opcje pomijane przy argmax (np. "ZADEN" dla Q-OC-00)

class PytanieWPlanie(BaseModel):
    pytanie: Pytanie
    warunek: WarunekPytania | None           # None = bezwarunkowe; głębokość zależności = 1

class PlanPytan(BaseModel):
    pytania: list[PytanieWPlanie]            # kody unikalne; walidator odrzuca cykle i głębokość > 1

class DecyzjaSkladowa(BaseModel):            # odpowiedź modelu na jedno pytanie (nie mylić z Odpowiedzią Ucznia)
    kod: str
    typ: Literal["NOUL", "SCORE", "CHOICE"]
    wybor: str                               # "tak"/"nie" | "0".."n" | opcja (moda)
    rozklad: dict[str, float]                # suma 1 ± 0.001 (normalizowane w adapterze)
    p_wyboru: float                          # = rozklad[wybor]

class RezultatPytan(BaseModel):
    konfiguracja_modelu_id: UUID
    decyzje: list[DecyzjaSkladowa]           # tylko pytania zadane (warunek spełniony)
    pominiete: list[str]                     # kody pytań warunkowych, których warunek nie zaszedł
    czas_ms: int
    koszt_pln: Decimal                       # wg parametry.cena_* konfiguracji

class ModelDecyzyjny(Protocol):
    async def wykonaj_plan(self, stan: str, plan: PlanPytan, *, termin: datetime) -> RezultatPytan: ...

class ModelGeneratywny(Protocol):
    konfiguracja_modelu_id: UUID
    async def generuj_json(self, prompt: Prompt, schemat: type[T], *,
                           obrazy: Sequence[bytes] = (), timeout_s: float) -> T: ...
    async def generuj_tekst(self, prompt: Prompt, *, max_znakow: int, timeout_s: float) -> str: ...

class RozpoznawaniePisma(Protocol):
    async def rozpoznaj(self, wejscie: WejscieRozpoznawania, obrazy: Sequence[bytes]) -> RezultatRozpoznawania: ...
    async def sprawdz_poprawki(self, wejscie: WejscieSprawdzeniaPoprawek,
                               obrazy: Sequence[bytes]) -> RezultatSprawdzeniaPoprawek: ...   # ADR-036, 10.3.3

class PrzyjeciePlanu(BaseModel):              # ADR-039 (C3-04)
    konfiguracja_modelu_id: UUID
    liczby_wywolan_fal: list[int]            # np. [16, 11, 1] w trybie pojedynczym, [1, 1] w wsadowym
    terminy_fal: list[datetime]               # najpóźniejszy przydział tokenu dla wywołań fali k (13.2)
    przewidywany_koniec: datetime

class OgranicznikSzybkosci(Protocol):        # ADR-039; implementacja: lokalny token bucket + przydział z limit_szybkosci_modelu
    def przyjmij_plan(self, konfiguracja_modelu_id: UUID, liczby_wywolan_fal: Sequence[int], *,
                      termin_miekki: datetime) -> PrzyjeciePlanu | None: ...
                                             # None = przewidywany koniec planu Jev > termin miękki → model zapasowy od razu
    async def zarezerwuj_wywolanie(self, przyjecie: PrzyjeciePlanu, nr_fali: int) -> bool: ...
                                             # 1 token = 1 wywołanie HTTP; fale ≥ 2 mają pierwszeństwo;
                                             # False = brak tokenu przed terminy_fal[nr_fali] (nie jest błędem modelu)
    def zwolnij(self, przyjecie: PrzyjeciePlanu) -> None: ...   # usuwa niewykorzystane zapotrzebowanie (pytania warunkowe niezadane)
    def zmniejsz_przydzial(self, konfiguracja_modelu_id: UUID) -> None: ...   # po 429 dostawcy: × 0.5 na 30 s
    async def odnow(self, konfiguracja_modelu_id: UUID, pula: str = "*") -> tuple[float, int]: ...
                                             # co 10 s (co 2 s w trybie dołączania): pod pg_advisory_xact_lock liczy
                                             # (przydzial, pojemnosc) wg 13.2 i publikuje w limit_szybkosci_modelu (C4-05)

class Rownowaznosc(StrEnum):
    ROWNOWAZNE = "ROWNOWAZNE"; NIEROWNOWAZNE = "NIEROWNOWAZNE"; NIEROZSTRZYGNIETE = "NIEROZSTRZYGNIETE"

class SprawdzanieSymboliczne(Protocol):
    def postac_liniowa(self, latex: str) -> str | None: ...                     # str() wyrażenia SymPy
    def rownowaznosc_krokow(self, latex_a: str, latex_b: str) -> Rownowaznosc: ...
    def rownowaznosc_wyniku(self, latex: str, wynik: WynikKoncowyWzorcowy) -> Rownowaznosc: ...

class PredyktorSukcesu(Protocol):
    def p_sukcesu(self, opanowanie: Mapping[int, float], zadanie: ZadanieDoDoboru) -> float: ...

class MagazynObiektow(Protocol):
    async def zapisz(self, klucz: str, dane: bytes, typ_mime: str) -> None: ...
    async def pobierz(self, klucz: str) -> bytes: ...
    async def usun(self, klucz: str) -> None: ...                              # idempotentne
    def podpisany_url(self, klucz: str, waznosc_s: int = 600) -> str: ...

# Wyjątki wspólne dla adapterów:
class BladModeluPrzejsciowy(Exception): ...   # timeout, 429, 5xx, zerwane połączenie → ponawiane
class BladModeluTrwaly(Exception): ...        # 4xx, odpowiedź niezgodna ze schematem → nie ponawiane po 1 próbie naprawy
class ModelNiedostepny(Exception): ...        # obwód OTWARTY i brak modelu zapasowego
```

Schemat `konfiguracja_modelu.parametry` (walidowany Pydantic przy zapisie):
`{timeout_s: float, max_tokens: int, temperatura: float, cena_wejscie_pln_1k: decimal,
cena_wyjscie_pln_1k: decimal, retencja_dostawcy_dni: int, reprezentacja_wzorow:
"LATEX"|"LINIOWA_SYMPY"|"OBA"}`. Aktywacja odrzuca konfigurację, której
`retencja_dostawcy_dni` przekracza A-07 (0 dla `ROZPOZNAWANIE_PISMA`, ≤ 30
dla pozostałych) → `409 BRAK_RETENCJI_DOSTAWCY`.

Rejestr adapterów: `lemat.modele.rejestr` mapuje `konfiguracja_modelu.dostawca`
na klasę adaptera; aktywna konfiguracja roli jest czytana z bazy (cache 30 s,
unieważniany `NOTIFY konfiguracja_modelu`). Zmiana dostawcy = nowa
konfiguracja + (dla ról decyzyjnych) ewaluacja ZT-1 + aktywacja — bez zmian
w kodzie domenowym (NFR-06). Agent oceniający zna wyłącznie `PlanPytan` i
`RezultatPytan`, więc nie wie, który model wykonał plan (ADR-026).

### 13.2 Kontrakty adapterów decyzyjnych

**`JevAdapter`** (zgodnie z A-01) wykonuje plan w ≤ 2 rundach: runda 1 —
pytania bezwarunkowe; runda 2 — pytania warunkowe, których warunek zachodzi
dla rozkładów z rundy 1. Dwa tryby (parametr konfiguracji
`parametry.tryb_jev ∈ {WSADOWY, POJEDYNCZY}`, ustalany w G-4):

- **`WSADOWY`** (domyślny, wymaganie A-01): jedno żądanie HTTP na rundę z
  `questions[]` (≤ 32 pytania), czyli ≤ 2 żądania na Ocenę;
- **`POJEDYNCZY`**: jedno żądanie na pytanie, ≤ 16 równolegle na Ocenę,
  plan w ≤ 3 falach (10.4.3).

Timeout żądania: `min(2 s, termin − teraz)`. Każde żądanie przechodzi przez
**globalny ogranicznik szybkości** (ADR-039, C2-07; jedna formuła od wersji
2.3, C3-04; pojemność i dołączanie procesów od wersji 2.4, C4-05). Proces
rejestruje się w `limit_szybkosci_modelu` (wiersz `pula = '*'`) z wagą
swojego typu (worker 1,00, `api` 0,10), odnawia wiersz co 10 s i przy każdym
odnowieniu wylicza **przydział i pojemność** w jednej transakcji z
`pg_advisory_xact_lock(hashtext('ogranicznik:' || konfiguracja_modelu_id))`
(odnowienia jednego klucza są serializowane):

```text
w_i      = waga wiersza i;  j: wiersze z odnowiono_at > now() − 30 s
r_i      = min( 0,8 × limit × w_i / Σ w_j ,  0,8 × limit − Σ_{j≠i} przydzial_j )      (≥ 0)
B_całk   = 34  (tolerancja A-01: 75 − 0,8 × 50 × 1 s − 1 wywołanie zapasu)   albo 10 (ścisłe okno 1 s: 50 − 40), parametr 22
B_i      = 1                                                                  dla wiersza api
B_i      = min( ⌊(B_całk − n_api) × w_i / Σ_{j ≠ api} w_j⌋ ,  B_całk − Σ_{j≠i} pojemnosc_j )   dla pozostałych
publikacja: przydzial := r_i, pojemnosc := B_i; zmniejszenie stosowane lokalnie PRZED COMMIT,
            zwiększenie — PO COMMIT (używana wartość ≤ opublikowanej w każdej chwili)
niezmienniki: Σ przydzial_j ≤ 0,8 × limit;  Σ pojemnosc_j ≤ B_całk   (każde odnowienie je zachowuje, bo odczytuje
            aktualne wartości pozostałych wierszy pod blokadą i ogranicza własną wartość do reszty)
2 workery + 2 api, limit 50/s:  Σ w = 2,2;  worker r = 18,18/s, B = ⌊32 / 2⌋ = 16;  api r = 1,82/s, B = 1
```

Lokalny token bucket procesu ma pojemność `B_i` (z wiersza) i uzupełnia
się z szybkością `r_i`; zmniejszenie `B_i` obcina bieżącą liczbę tokenów do
nowej pojemności. **Dołączenie procesu** (start repliki): nowy wiersz
startuje z pustym kubełkiem (0 tokenów), `r_nowy = min(formuła, 0,8 × limit
− Σ przydziałów pozostałych)` i `B_nowy = min(formuła, B_całk − Σ
pojemności pozostałych)` — przy pełnym wykorzystaniu przez dotychczasowe
procesy oba wynoszą 0 — i odnawia wiersz co 2 s (tryb dołączania), aż
osiągnie wartości z formuły; dotychczasowe procesy zmniejszają przydział i
pojemność przy najbliższym odnowieniu (≤ 10 s), więc nowy proces osiąga
pełny przydział w ≤ 12 s. W wersji 2.3 istniejące procesy do odnowienia
miały stary przydział, a nowy — pełny (3. worker: 2 × 18,18 + 12,5 + 2 ×
1,82 ≈ 52,5/s > 50/s); od wersji 2.4 suma opublikowanych przydziałów nigdy
nie przekracza 40/s. Proces kończący pracę usuwa swój wiersz (`DELETE`);
proces, który zniknął bez tego, wypada z sum po 30 s. Liczba wierszy innych
niż `api` nie może przekroczyć `B_całk − n_api` (32): wiersz ponad tę liczbę
dostaje `B_i = 0`, `r_i = 0` i alert operacyjny
`ogranicznik_wiersze_ponad_limit > 0` (17.3; granica 8× powyżej 4 replik z
17.4).

**Granica chwilowa dla N workerów (C4-05).** W dowolnym oknie `T` s procesy
wysyłają ≤ Σ B_i + Σ r_i × T ≤ B_całk + 0,8 × limit × T wywołań —
**niezależnie od liczby procesów**, także w trakcie dołączania:

| Konfiguracja (2 × `api`) | Σ w | `r` worker / `api` | `B` worker / `api` | Σ B | Granica 1 s / 5 s (`B_całk = 34`) | Wersja 2.3 (B stałe 16 / 1): granica 1 s | Oczekiwanie fali 16 przy pełnym kubełku | Ścisłe okno (`B_całk = 10`): `B` worker, granica 1 s |
|---|---|---|---|---|---|---|---|---|
| 2 workery | 2,2 | 18,18 / 1,82 | 16 / 1 | 34 | 74 / 234 | 74 | 0 s | 4, 50 |
| 3 workery | 3,2 | 12,50 / 1,25 | 10 / 1 | 32 | 72 / 232 | 90 (> 75) | (16 − 10) / 12,5 = 0,48 s | 2, 48 |
| 4 workery | 4,2 | 9,52 / 0,95 | 8 / 1 | 34 | 74 / 234 | 106 (> 75) | (16 − 8) / 9,52 = 0,84 s | 2, 50 |

Granica 74 w oknie 1 s jest więc spełniona przy 2, 3 i 4 workerach (A-01:
tolerancja 75), a w oknie 5 s ≤ 234 ≤ 250. Koszt skalowania: fala 16
wywołań poza szczytem czeka do 0,48 s (3 repliki) albo 0,84 s (4 repliki)
na uzupełnienie kubełka; w szczycie kubełki są puste niezależnie od `B`, więc
oczekiwanie wynika z przydziału (17.4). Fala trybu pojedynczego (≤ 16
wywołań) przy 2 workerach mieści się w kubełku (`B = 16 ≥ maks. fala`). **Granulacja: jeden token = jedno wywołanie HTTP**
(tryb wsadowy: żądanie rundy; tryb pojedynczy: każde wywołanie fali). Fala
trybu pojedynczego (≤ 16 wywołań) mieści się w kubełku workera (`B = 16 ≥
maks. fala`), więc poza szczytem startuje bez czekania; przy pustym kubełku
jej wywołania startują w miarę uzupełniania (16 / 18,18 ≈ 0,9 s). Gdy G-4 wykaże ścisłe okno 1 s bez tolerancji, parametr
`pojemnosc_laczna_kubelkow = 10` daje przy 2 workerach Σ (B_i + r_i) = 2 ×
(4 + 18,18) + 2 × (1 + 1,82) = 50,0 ≤ 50 (wtedy fala czeka do 0,66 s na
tokeny, budżet 17.2 +1,1 s), a przy 3 i 4 workerach ≤ 50 (tabela wyżej).

Kolejność przydziału w procesie: **najpierw wywołania fal ≥ 2 Ocen już
przyjętych**, potem fale 1 (FIFO w obrębie klasy) — Ocena rozpoczęta nie
traci tokenów na rzecz nowych. **Przyjęcie planu** (przed falą 1, metoda
`przyjmij_plan`):

```text
Z      = wywołania oczekujące w procesie + pozostałe wywołania planów przyjętych (fale ≥ 2, górne oszacowanie)
czekanie_1 = max(0, (Z + n_1 − tokeny) / r)
koniec  = teraz + czekanie_1 + F × t_fali + max(0, n_2 − B) / r + 0,2 s
          (F = liczba fal planu; t_fali = 1,2 s w trybie pojedynczym, 0,8 s w wsadowym; n_1, n_2 = liczności fal 1 i 2)
koniec ≤ termin_miekki → plan przyjęty; termin fali 1 = teraz + czekanie_1 + 0,5 s
koniec > termin_miekki → plan nieprzyjęty: Ocena od razu modelem zapasowym (przyczyna OGRANICZENIE_LOKALNE)
termin fali k ≥ 2 = termin_miekki − t_fali × (F − k + 1) − 0,2 s
brak tokenu dla wywołania przed terminem jego fali → Ocena modelem zapasowym (OGRANICZENIE_LOKALNE),
          z timeoutem jak niżej (min(9 s, termin − teraz), a gdy pozostało < 6 s — 9 s)
```

Reguła zastępuje „oczekiwanie do `termin_miekki − 11 s`” z wersji 2.2, które
przy jednym tokenie na żądanie i kubełku pojemności `r` uniemożliwiało
rezerwację fali (C3-04). Brak przydziału i odrzucenie planu są liczone w
`jev_ograniczenie_lokalne_total{przyczyna}` (`PLAN_NIEPRZYJETY`,
`TERMIN_FALI`) — **bez** zliczania do circuit breakera. Odpowiedź `429` od
Jev zmniejsza przydział procesu o połowę na 30 s (`zmniejsz_przydzial`), jest
ponawiana jak inne błędy przejściowe, ale również **nie** jest liczona do
otwarcia obwodu; obwód otwierają wyłącznie timeouty, `5xx`, błędy połączenia
i odpowiedzi niezgodne ze schematem. Test jednostkowy
`tests/modele/test_ogranicznik.py`: (a) pusty system — fala 16 wywołań
dostaje 16 tokenów w chwili 0; (b) przy pustym kubełku 16 wywołań kończy się
w 0,88 s ± 0,01 s; (c) fale ≥ 2 wyprzedzają fale 1 złożone wcześniej; (d) plan
z przewidywanym końcem > termin miękki jest odrzucany bez zużycia tokenów;
(e) suma przydziałów = 0,8 × limit przy 2/3/4 workerach i 2 `api`; od
wersji 2.4 (C4-05): (f) **granica chwilowa** — symulowany zegar, N ∈ {2, 3,
4} workerów i 2 `api`, każdy proces wysyła z maksymalną dozwoloną szybkością,
losowe chwile dołączenia i odejścia procesów (`hypothesis`, 500 przypadków):
maksimum wywołań w dowolnym przesuwnym oknie 1 s ≤ 74 przy `B_całk = 34` i ≤
50 przy `B_całk = 10`, w oknie 5 s ≤ 234; (g) dołączenie 3. workera przy
pełnym wykorzystaniu: przydział nowego procesu = 0 do odnowienia
pozostałych, Σ opublikowanych przydziałów ≤ 40/s w każdej chwili, pełny
przydział 12,5/s po ≤ 12 s; (h) dwa jednoczesne odnowienia (dwa wątki,
blokada doradcza) nie przekraczają niezmienników.

```http
POST {JEV_URL}/v1/decisions
Authorization: Bearer {JEV_API_KEY}
Content-Type: application/json

{ "state": "<stan 10.4.2>",
  "questions": [
    { "id": "Q-OC-01",      "type": "noul",   "text": "Czy wynik końcowy Ucznia jest równoważny wynikowi wzorcowemu?" },
    { "id": "Q-OC-02.1.K1", "type": "score",  "text": "Ile punktów przyznać za kryterium K1: …?", "min": 0, "max": 1 },
    { "id": "Q-OC-03",      "type": "choice", "text": "W którym kroku …?", "options": ["BRAK_BLEDU","KROK_1","KROK_2","BRAK_WYMAGANEGO_KROKU"] } ] }

200 OK
{ "answers": [
    { "id": "Q-OC-01",      "distribution": { "yes": 0.97, "no": 0.03 } },
    { "id": "Q-OC-02.1.K1", "distribution": { "0": 0.04, "1": 0.96 } },
    { "id": "Q-OC-03",      "distribution": { "BRAK_BLEDU": 0.02, "KROK_1": 0.01, "KROK_2": 0.95, "BRAK_WYMAGANEGO_KROKU": 0.02 } } ],
  "model": "jev-…" }
```

W trybie `POJEDYNCZY` `questions[]` zawiera jedno pytanie. Mapowanie
`yes/no → tak/nie` odbywa się w adapterze.

**`ZapasowyLlmAdapter`** (rola `DECYZYJNY_ZAPASOWY`) wykonuje **cały plan
jednym wywołaniem** LLM (timeout `min(9 s, termin − teraz)`, a gdy pozostało
< 6 s — 9 s mimo przekroczenia terminu miękkiego; liczy się wtedy naruszenie
NFR-01, ale Ocena powstaje). Protokół pytań zależnych (C1-07):

1. **Rodzina pytań warunkowych o wspólnym rodzicu i jednym szablonie**
   (Q-OC-04.1 … Q-OC-04.n) jest zwijana do jednego pola schematu:
   `"Q-OC-04": {"krok": int, "rozklad": {...}}` z instrukcją „typ błędu w
   kroku, który w Q-OC-03 ma najwyższe prawdopodobieństwo”. Adapter
   sprawdza, że `krok` = argmax Q-OC-03 tej samej odpowiedzi; niezgodność →
   `BladModeluTrwaly` (1 próba naprawy). Gdy argmax Q-OC-03 nie jest
   `KROK_j`, pole jest ignorowane i trafia do `pominiete`.
2. **Pytania warunkowe zależne od wariantu** (Q-OC-02.v.Kk) są zadawane dla
   **wszystkich** wariantów (łącznie ≤ 24 kryteria — limit W-1 i triggera
   `trg_suma_kryteriow`); adapter zwraca decyzje tylko dla wariantu wybranego
   (argmax Q-OC-00 bez `ZADEN`), pozostałe trafiają do `pominiete`.
3. Schemat odpowiedzi: `{"decyzje_skladowe":[{"kod": str, "rozklad":
   {opcja: float}}], "Q-OC-04": {"krok": int, "rozklad": {…}} | null}`
   (klucz `decyzje_skladowe` zgodnie z 3.1 p. 5 — „odpowiedź” oznacza
   wyłącznie wybór Ucznia). Walidacja:
   komplet kodów bezwarunkowych, klucze rozkładu = dozwolone opcje, wartości
   w [0, 1]; suma normalizowana do 1 (odchylenie > 0.05 →
   `BladModeluTrwaly`).

**`CircuitBreakerModeluDecyzyjnego`**: stan obwodu per proces; otwarcie po
3 kolejnych błędach albo przy ≥ 50% błędów w oknie 20 s przy ≥ 6
wywołaniach (błędy = timeout, `5xx`, błąd połączenia, niezgodność ze
schematem; `429` i brak przydziału ogranicznika nie są błędami obwodu); stan `OTWARTY` 60 s, potem `POLOTWARTY` (1 próbny plan, reszta
do modelu zapasowego). Ponowienie nieudanych pytań Jev następuje tylko,
gdy do terminu miękkiego (`max(10 s, 28 s − t_R)`) zostało ≥ 13 s
(ponowienie 2 s + model zapasowy 9 s + zapas 2 s; 9.9, 17.2).

### 13.3 Katalog prac w tle

Pule (13.4): `R` — ROZPOZNAWANIE, `S` — SPRAWDZANIE (od wersji 2.3, C3-05), `O` — OCENIANIE, `P` — POMOCNICZA, `W` — WOLNA.

| Typ | Pula | Ładunek | Klucz deduplikacji | Prio | Maks. prób | Odstęp ponowień | Warunek wykonania (idempotencja) | Po wyczerpaniu prób |
|---|---|---|---|---|---|---|---|---|
| `ROZPOZNAJ_PISMO` | R | `{rozwiazanie_id, przeslanie_nr, ponowienie_nr}` | `rozwiazanie_id:przeslanie_nr:ponowienie_nr` | 1 | 3 | 1 s, 3 s | konto Ucznia `AKTYWNE`, status ∈ {`PRZESLANE`, `ROZPOZNAWANIE`} i brak Transkrypcji dla `przeslanie_nr` po `przeslano_at`; zapis po Filtrze danych osobowych (ADR-042) | `BLAD_ROZPOZNAWANIA` (`BLAD_DOSTAWCY`/`PRZEKROCZONY_CZAS`), powiadomienie |
| `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` | S | `{transkrypcja_id}` | `rozwiazanie_id` (oczekująca praca dostaje `transkrypcja_id` najnowszego `PUT`; C3-05) | 1 | 2 | 1 s | Transkrypcja `POPRAWKA_UCZNIA` z ≥ 1 krokiem zmienionym lub wstawionym, będąca najnowszą wersją Rozwiązania (inaczej koniec z rezultatem przestarzałym), `zgodnosc_poprawek IS NULL`, Rozwiązanie w `TRANSKRYPCJA_GOTOWA`/`ZATWIERDZONE`/`OCENIANIE`; zapis `UPDATE … WHERE id = :transkrypcja_id AND zgodnosc_poprawek IS NULL` (10.3.3, ADR-036) | brak rezultatu → T-7 w Ocenie (Z-3) |
| `OCEN_ROZWIAZANIE` | O | `{rozwiazanie_id, transkrypcja_id}` | `rozwiazanie_id` | 1 | 4 | 0,5 s, 2 s, 5 s (tylko przed terminem twardym 120 s) | konto Ucznia `AKTYWNE`, status ∈ {`ZATWIERDZONE`, `OCENIANIE`} i `zatwierdzona_transkrypcja_id = transkrypcja_id` i brak Oceny; po przywróceniu konta z minionym terminem twardym — od razu T-6 (16.4) | T-6: Weryfikacja `BLAD_PRZETWARZANIA` + `PONOW_OCENE` |
| `PONOW_OCENE` | O | `{weryfikacja_id}` | `weryfikacja_id` | 4 | 3 | 15 min, 1 h | Weryfikacja `OCZEKUJE`, `BLAD_PRZETWARZANIA` ∈ powody, `ocena_weryfikowana_id IS NULL` (9.13) | brak akcji (Nauczyciel ocenia od zera) |
| `NAPISZ_WSKAZOWKE` | P | `{ocena_id}` | `ocena_id` | 1 | 2 | 1 s | `ocena.wskazowka IS NULL`, `typ_bledu IS NOT NULL`, Ocena ≠ `ZASTAPIONA` | zapis szablonu (10.5.3 p. 4) |
| `AKTUALIZUJ_PROFIL` | P | `{uczen_id}` | `uczen_id` (deduplikacja tylko wśród `OCZEKUJE`, `ux_praca_dedup_oczekujace`, C2-04) | 2 | 5 | 1 s, 5 s, 30 s, 2 min | w jednej transakcji z `SELECT … FROM profil_ucznia FOR UPDATE`: pętla „pobierz Wyniki z `zastosowany_w_profilu_at IS NULL` lub nieprzetworzonym unieważnieniem → zastosuj/odtwórz → oznacz” powtarzana do pustego wyniku (maks. 10 iteracji, potem nowa praca); reguły Serii | alert operacyjny |
| `OPINIA_POMOCNICZA` | W | `{weryfikacja_id}` | `weryfikacja_id` | 6 | 3 | 1 min, 5 min | Weryfikacja otwarta i `opinia_pomocnicza IS NULL` | brak opinii |
| `UZUPELNIJ_PULE` | W | `{umiejetnosc_id, poziom_trudnosci, poziom_matury, rodzaj}` | `umiejetnosc_id:poziom_trudnosci:poziom_matury:rodzaj` | 7 | 1 | — | `rodzaj ∈ {ZAMKNIETE, OTWARTE_OBLICZENIOWE}`, liczba `OPUBLIKOWANE`+`GLOBALNY` < 3 dla klucza, brak otwartego zlecenia dla klucza, budżet dzienny (12.7) nieprzekroczony; inaczej `dostepna_od` = 00:00 UTC następnego dnia | — |
| `GENERUJ_ZADANIE` | W | `{zlecenie_generowania_id}` | id | 7 | 3 | 30 s, 2 min | zlecenie `OCZEKUJE`/`W_TOKU`, `liczba_cykli < 3` (inkrementacja warunkowa 9.7) | zlecenie `NIEUDANE` |
| `WALIDUJ_ZADANIE` | W | `{zadanie_wersja_id, nr_cyklu}` | `zadanie_wersja_id` | 7 | 3 | 30 s, 2 min | Zadanie `WALIDACJA` | Zadanie `ODRZUCONE_W_WALIDACJI`, nowy cykl lub zlecenie `NIEUDANE` |
| `GENERUJ_SZKIC_WKLADKI` | W | `{umiejetnosc_id}` | `umiejetnosc_id` | 7 | 3 | 1 min, 10 min | brak Wkładki `SZKIC`/`DO_PRZEGLADU`/`OPUBLIKOWANA` dla Umiejętności; < 2 automatycznych szkiców tej Umiejętności `WYCOFANA` w 30 dniach (`ix_wkladka_automatyczne_odrzucone`); budżet generowania systemowego 12.7 nieprzekroczony (inaczej `dostepna_od` = 00:00 UTC następnego dnia); zapis `wygenerowana_automatycznie = true`, `koszt_generowania_pln` | alert dla Administratorów („Wkładka wymaga autora” po 2 odrzuceniach) |
| `IMPORTUJ_ARKUSZ` | W | `{import_id}` | id | 5 | 1 | — | import `OCZEKUJE` | import `ODRZUCONY` |
| `EWALUUJ_ZT1` | W | `{konfiguracja_modelu_id}` | id | 8 | 1 | — | brak `prog_pewnosci` `EWALUACJA_ZT1` dla konfiguracji | alert |
| `KALIBRUJ` | W | `{data}` | data | 8 | 3 | 10 min | brak kalibratora `KALIBRACJA_AUTOMATYCZNA` z tą datą; kroki 1–6 „Wynik dnia KALIBRUJ” (11.5.3, C4-02): wybór progu, ocena bezpieczników na stanie sprzed przebiegu, Blokada obniżania progu, dozwolone obniżenie, łączny wynik, jeden `INSERT prog_pewnosci` i `UPSERT stan_bezpiecznika` (ADR-046) | alert |
| `USUN_ZDJECIA` | W | `{}` | `cykl` | 3 | 5 | 1 min | selekcja 9.10 | alert RODO |
| `ESKALUJ_WERYFIKACJE` | W | `{godzina}` | godzina | 5 | 3 | 5 min | reguły 11.4.3 (terminy w dniach roboczych) + kontrola przyjęć globalna i per Klasa (`odsetek_eskalacji_7d`) + dzienne podsumowanie dla Nauczycieli (05:00 UTC, tylko w dni robocze) | alert |
| `ZAMIATAJ_PROFILE` | W | `{}` | `cykl` | 2 | 3 | 1 min | `SELECT DISTINCT uczen_id FROM wynik_zadania` po `ix_wynik_do_profilu` z `created_at` lub `uniewazniony_at` < `now() − 2 min`, konto `AKTYWNE`, `LIMIT 500` → zlecenie `AKTUALIZUJ_PROFIL`; metryka `wynik_najstarszy_niezastosowany_sekundy` (C2-04) | alert |
| `SPRAWDZ_PULE_DIAGNOSTYCZNA` | W | `{data}` | data | 8 | 3 | 10 min | dla każdej trójki (Dział, poziom matury, nominalny Poziom trudności ∈ {2, 4}) liczba Zadań diagnostycznych `OPUBLIKOWANE` ≥ 3 (9.1, G-5) | `ALERT_PULA_DIAGNOSTYCZNA` |
| `UZGODNIJ_REJESTR_RODO` | W | `{}` | `cykl` | 3 | 5 | 1 min | (1) obiekty `rejestr-zdarzen-rodo/zdarzenia/` z ostatnich 24 h, starsze niż 5 min, z `zrodlo = API` i bez wiersza `zdarzenie_rodo` o tym `id` (zatwierdzenie transakcji `api` nie powiodło się) → zapis obiektu kompensującego `{typ: "UNIEWAZNIENIE", zdarzenie_id}`; przy cyklu co 5 min (13.5) zdarzenie bez transakcji jest unieważniane najpóźniej **10 min** po zapisie obiektu (5 min wieku + ≤ 5 min do następnego przebiegu; wersja 2.3: cykl 10 min, czyli do 15 min — C4-06 p. 2); (2) obiekty `zrodlo = WORKER` (`USUNIECIE_WYKONANE`, prefiks `rejestr-zdarzen-rodo/wykonane/` bez daty) **nigdy nie są unieważniane** — stanem docelowym jest usunięcie konta: gdy 2 h po zapisie obiektu `uzytkownik` o tym `uzytkownik_id` nadal istnieje, praca zleca ponownie `USUN_KONTO` (klucz `uzytkownik_id`), a po 26 h — `ALERT_RODO` (ADR-041, C3-03) | `ALERT_RODO` |
| `PORZUC_NIEZATWIERDZONE` | W | `{data}` | data | 5 | 3 | 10 min | Rozwiązania 7 dni w `PRZESLANE`/`TRANSKRYPCJA_GOTOWA`/`BLAD_ROZPOZNAWANIA` → `PORZUCONE`, `usun_po := now()` | alert |
| `WYGAS_ELEMENTY` | W | `{}` | `cykl` | 5 | 3 | 1 min | konto `AKTYWNE`; `PROPONOWANY` z `wygasa_at < now()` → `WYGASLY`; `ROZPOCZETY` z `wygasa_at < now()` → `POMINIETY` (Wkładka bez przeczytania, 8.5; Zadanie bez Rozwiązania nieterminalnego); Seria nieaktywna 7 dni → zakończona | — |
| `USUN_KONTO` | W | `{uzytkownik_id}` | id | 3 | 5 | 1 h | `status = USUWANE` i `usuniecie_zaplanowane_at ≤ now()` **albo** istnieje obiekt `USUNIECIE_WYKONANE` dla konta (ponowienie po awarii, PITR); kroki wg `uzytkownik.rola`: 0–6 z 16.4 (`UCZEN`) albo (1)–(5) z 16.4 „Usunięcie konta Nauczyciela lub Administratora” (`NAUCZYCIEL`, `ADMINISTRATOR`); krok 0 = zapis obiektu `USUNIECIE_WYKONANE` (`zrodlo = WORKER`, deterministyczne `id`, `dane.rola`, klucz bez daty `rejestr-zdarzen-rodo/wykonane/{uzytkownik_id}/{id}.json`, `If-None-Match: *` — istniejący obiekt oznacza ponowienie w dowolnym dniu i jest akceptowany, C4-06 p. 3); każdy krok jest idempotentny, więc ponowienie po przerwaniu w dowolnym kroku (awaria, PITR) kończy usunięcie (C4-01) | `ALERT_RODO` |
| `UTRZYMAJ_PARTYCJE` | W | `{}` | miesiąc | 8 | 3 | 1 h | brak partycji na 3 miesiące naprzód | alert |
| `WYCZYSC_DANE_TECHNICZNE` | W | `{}` | data | 8 | 3 | 1 h | `decyzja_modelu.stan := NULL` po 180 dniach; klucze idempotencji po `wygasa_at`; `proba_logowania` po 30 dniach; zakończone prace po 7 dniach; `DELETE FROM zdarzenie_rodo WHERE created_at < now() − interval '2 years'` (16.4, C3-09) | alert |
| `WYSLIJ_EMAIL` | W | `{szablon, adresat, dane}` | — | 4 | 5 | 1, 5, 15, 60 min | — | wpis w logu |
| `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` | W | `{rozwiazanie_id}` | `rozwiazanie_id` | 8 | 3 | 10 min, 1 h | zlecana w każdej transakcji, po której Rozwiązanie jest `OCENIONE` bez otwartej Weryfikacji (zapis Oceny automatycznej bez Kontroli, rozstrzygnięcie, korekta, zamknięcie ostatniej Weryfikacji, `PONOW_OCENE`; C2-16); warunki: `uczen.zgoda_na_trening = true`, konto `AKTYWNE`, zdjęcia nieusunięte, brak otwartej Weryfikacji, brak `kopia_treningowa` dla Rozwiązania **albo** `kopia_treningowa.ocena_id ≠` id Oceny aktualnej (wtedy nadpisanie `.json`); kopia: zdjęcie maskowane (ADR-031; brak maskowanego → brak kopii) + Transkrypcja zatwierdzona po ponownym Filtrze danych osobowych (ADR-042) + Ocena aktualna | brak kopii (alert informacyjny) |
| `USUN_KOPIE_TRENINGOWE` | W | `{}` | `cykl` | 3 | 5 | 1 min | `kopia_treningowa.usun_po < now() AND usunieto_at IS NULL`: `DELETE` obiektów, `usunieto_at = now()` | alert RODO |
| `SPRZATAJ_OSIEROCONE_OBIEKTY` | W | `{tryb: "CODZIENNY" \| "ODTWORZENIE", od?}` | data | 3 | 3 | 10 min | (ADR-047, C3-06) lista obiektów w `zbior-treningowy/`, `zdjecia/`, `zdjecia-maskowane/` (paginacja po 1 000 kluczy); obiekt jest osierocony, gdy jego klucz nie występuje w `kopia_treningowa.klucz_obiektu` (także dla `.json` — porównanie po kluczu bez rozszerzenia) albo w `zdjecie_rozwiazania.klucz_obiektu` / `klucz_obiektu_maskowany`; tryb `CODZIENNY`: tylko obiekty z `LastModified < now() − 1 h` (ochrona zapisów w toku przed zatwierdzeniem transakcji); tryb `ODTWORZENIE`: obiekty z `LastModified > od` (= punkt odtworzenia `T` − 5 min), bez okna 1 h, bo `worker` nie działa; osierocone → `DELETE`, metryka `obiekty_osierocone_usuniete_total{bucket,tryb}` | `ALERT_RODO` |

Każdy handler przetwarzający dane Ucznia (oznaczone „konto `AKTYWNE`”, a
także `NAPISZ_WSKAZOWKE`, `OPINIA_POMOCNICZA`, `PONOW_OCENE`) w okresie
karencji kończy się bez efektu z rezultatem `KONTO_USUWANE`; przywrócenie
konta zleca ponownie przerwane prace (16.4, ADR-040).

Ponowienie rozpoznania przez Ucznia (`POST …/ponowienie-rozpoznania`)
zleca nową pracę `ROZPOZNAJ_PISMO` z `ponowienie_nr + 1` (inny klucz
deduplikacji), ustawia `przeslano_at = now()` i nie zwiększa
`liczba_przeslan`. `liczba_przeslan` liczy wyłącznie przesłania nowych
zdjęć; `praca_w_tle.proby` liczy wyłącznie próby techniczne jednej pracy
(C1-28).

### 13.4 Mechanika prac w tle i pule workera

```sql
-- Pobranie (worker, korutyna danej puli)
WITH kandydat AS (
    SELECT id FROM praca_w_tle
    WHERE status = 'OCZEKUJE' AND pula = :pula AND dostepna_od <= now()
    ORDER BY priorytet, dostepna_od
    FOR UPDATE SKIP LOCKED
    LIMIT 1)
UPDATE praca_w_tle p
SET status = 'W_TOKU', proby = p.proby + 1, wykonawca = :wykonawca,
    zablokowana_do = now() + interval '60 seconds', updated_at = now()
FROM kandydat WHERE p.id = kandydat.id
RETURNING p.*;

-- Odnowienie dzierżawy co 15 s
UPDATE praca_w_tle SET zablokowana_do = now() + interval '60 seconds'
WHERE id = :id AND wykonawca = :wykonawca AND status = 'W_TOKU';

-- Sukces
UPDATE praca_w_tle SET status = 'ZAKONCZONA', zablokowana_do = NULL, updated_at = now()
WHERE id = :id AND wykonawca = :wykonawca;

-- Błąd przejściowy
UPDATE praca_w_tle
SET status = CASE WHEN proby >= max_prob THEN 'MARTWA' ELSE 'OCZEKUJE' END,
    dostepna_od = now() + :odstep, ostatni_blad = :blad, wykonawca = NULL, zablokowana_do = NULL
WHERE id = :id AND wykonawca = :wykonawca;

-- Odzyskiwanie porzuconych dzierżaw (scheduler co 30 s)
UPDATE praca_w_tle
SET status = CASE WHEN proby >= max_prob THEN 'MARTWA' ELSE 'OCZEKUJE' END,
    wykonawca = NULL, zablokowana_do = NULL, ostatni_blad = 'DZIERZAWA_WYGASLA'
WHERE status = 'W_TOKU' AND zablokowana_do < now()
RETURNING id, typ, status;
```

- Przejście do `MARTWA` wywołuje handler `po_wyczerpaniu(praca)` danego typu
  (kolumna „Po wyczerpaniu prób”) w tej samej transakcji.
- Trigger `AFTER INSERT` wykonuje `pg_notify('praca_w_tle', pula)`; worker
  nasłuchuje i dodatkowo odpytuje co 1 s.
- Zlecanie z deduplikacją: `INSERT … ON CONFLICT (typ, klucz_deduplikacji)
  WHERE status IN ('OCZEKUJE','W_TOKU') AND klucz_deduplikacji IS NOT NULL
  AND typ NOT IN ('AKTUALIZUJ_PROFIL','SPRAWDZ_POPRAWKI_TRANSKRYPCJI') DO
  NOTHING`; dla `AKTUALIZUJ_PROFIL` predykat `WHERE status = 'OCZEKUJE' AND
  klucz_deduplikacji IS NOT NULL AND typ IN ('AKTUALIZUJ_PROFIL',
  'SPRAWDZ_POPRAWKI_TRANSKRYPCJI')` z `DO NOTHING` (indeks
  `ux_praca_dedup_oczekujace`); dla `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` ten sam
  predykat z `DO UPDATE SET ladunek = EXCLUDED.ladunek, updated_at = now()`
  (oczekująca praca dostaje `transkrypcja_id` najnowszego `PUT`, C3-05). Funkcja
  `zlec(typ, …)` wybiera predykat po typie. Skutek (C2-04): Wynik zadania
  zatwierdzony w trakcie wykonywania `AKTUALIZUJ_PROFIL` zawsze ma
  oczekującą pracę, która wykona się po zwolnieniu blokady `profil_ucznia`;
  pętla „do pustego wyniku” ogranicza liczbę takich prac, a `ZAMIATAJ_PROFILE`
  jest trzecią linią obrony.
- **Semantyka slotu (C1-25):** proces `worker` to jedna pętla `asyncio`;
  **slot = korutyna** pobierająca prace jednej puli. Wywołania modeli i
  magazynu obiektów są nieblokujące (I/O), więc jedna replika obsługuje
  dziesiątki slotów. Praca CPU (SymPy, Pillow) jest delegowana do
  `ProcessPoolExecutor` z 4 procesami na replikę (limity 16.2).
- **Pule na replikę:** `ROZPOZNAWANIE` 15 korutyn (`ROZPOZNAJ_PISMO`),
  `SPRAWDZANIE` 4 (`SPRAWDZ_POPRAWKI_TRANSKRYPCJI`; wersja 2.2 — pula R z
  priorytetem 2, przez co w szczycie Sprawdzenie czekało za rozpoznaniami,
  C3-05), `OCENIANIE` 15 (`OCEN_ROZWIAZANIE`, `PONOW_OCENE`), `POMOCNICZA` 6
  (`NAPISZ_WSKAZOWKE`, `AKTUALIZUJ_PROFIL`), `WOLNA` 4 (pozostałe). Dwie
  repliki: 30 / 8 / 30 / 12 / 8. Pule R i S korzystają z jednego limitu
  dostawcy rozpoznawania (A-08 ≥ 150/min), pilnowanego ogranicznikiem o tej
  samej konstrukcji co 13.2 (przydział 0,8 × 150 = 120/min, okno 60 s,
  `B_całk = 150 − 120 = 30`, wagi R 1,0 i S 0,3). Klucz rejestru
  `(konfiguracja_modelu_id, wykonawca, pula)` (C4-06 p. 8): każda replika ma
  dwa wiersze konfiguracji roli `ROZPOZNAWANIE_PISMA` — `('worker#1',
  'ROZPOZNAWANIE')` i `('worker#1', 'SPRAWDZANIE')`; przy 2 replikach Σ w =
  2,6, R: 46,2/min i `B = ⌊30 × 1,0 / 2,6⌋ = 11`, S: 13,8/min i `B = ⌊30 ×
  0,3 / 2,6⌋ = 3` na replikę (Σ B = 28 ≤ 30; w oknie 60 s ≤ 28 + 120 = 148 ≤
  150). Wersja 2.3 miała klucz `(konfiguracja_modelu_id, wykonawca)`, w którym
  dwie pule jednej repliki nie mieściły się jako osobne wiersze. Pule są rozłączne, więc szczyt rozpoznawania nie
  blokuje oceniania, a generowanie Zadań nie zagłodzi żadnej z nich.
- Zakończone prace są usuwane po 7 dniach, `MARTWA` — po 30 dniach.

### 13.5 Harmonogram (scheduler)

Jedna aktywna instancja dzięki `pg_try_advisory_lock(7_420_001)` na
dedykowanym połączeniu; druga instancja czeka w trybie gotowości.

| Cykl | Praca |
|---|---|
| co 30 s | odzyskiwanie dzierżaw (13.4) |
| co 1 min | `ZWOLNIJ_BLOKADY_WERYFIKACJI` (SQL 11.4.2, wykonywany bezpośrednio przez scheduler), `WYGAS_ELEMENTY` |
| co 5 min | `ZAMIATAJ_PROFILE` (C2-04) |
| co 5 min | `UZGODNIJ_REJESTR_RODO` (ADR-041; od wersji 2.4 co 5 min — okno unieważniania ≤ 10 min, C4-06 p. 2) |
| co 1 h | `USUN_ZDJECIA`, `USUN_KOPIE_TRENINGOWE`, `ESKALUJ_WERYFIKACJE` |
| codziennie 02:00 UTC | `PORZUC_NIEZATWIERDZONE`, `USUN_KONTO` (dla zaplanowanych), `WYCZYSC_DANE_TECHNICZNE` |
| codziennie 03:00 UTC | `KALIBRUJ` |
| codziennie 04:00 UTC | `SPRAWDZ_PULE_DIAGNOSTYCZNA` |
| codziennie 04:30 UTC | `SPRZATAJ_OSIEROCONE_OBIEKTY` (tryb `CODZIENNY`, ADR-047) |
| codziennie 05:00 UTC | podsumowanie dla Nauczycieli (w ramach `ESKALUJ_WERYFIKACJE`: liczba oczekujących, najbliższe terminy SLA) |
| 1. dnia miesiąca | `UTRZYMAJ_PARTYCJE` |

### 13.6 Zdarzenia domenowe (w procesie, zapis w tej samej transakcji)

Zdarzenia są obiektami Pythona publikowanymi przez moduły domenowe do
Orkiestratora wewnątrz transakcji; Orkiestrator zamienia je na prace w tle
lub powiadomienia. Nie ma zewnętrznego brokera.

| Zdarzenie | Pola | Reakcje |
|---|---|---|
| `RozwiazaniePrzeslane` | `rozwiazanie_id, przeslanie_nr, ponowienie_nr` | `ROZPOZNAJ_PISMO` |
| `TranskrypcjaGotowa` | `rozwiazanie_id, transkrypcja_id` | powiadomienie `TRANSKRYPCJA_GOTOWA` |
| `TranskrypcjaPoprawiona` | `rozwiazanie_id, transkrypcja_id, zakres_poprawek` | `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` (ADR-036) |
| `TranskrypcjaZatwierdzona` | `rozwiazanie_id, transkrypcja_id` | `OCEN_ROZWIAZANIE` |
| `OcenaWystawiona` | `ocena_id, status, powody[], mozliwy_blad_odczytu` | `AKTUALIZUJ_PROFIL` (gdy powstał Wynik zadania), `NAPISZ_WSKAZOWKE` (gdy błąd i brak podejrzenia odczytu), powiadomienie Ucznia, (`WSTEPNA`) powiadomienie `NOWA_WERYFIKACJA` dla Nauczyciela, `OPINIA_POMOCNICZA` |
| `OcenaSkorygowana` | `ocena_id, poprzednia_ocena_id` | `AKTUALIZUJ_PROFIL`, `NAPISZ_WSKAZOWKE` (tryb `AUTOMATYCZNA`), powiadomienie `OCENA_SKORYGOWANA` (gdy zmiana punktów lub błędnego kroku) |
| `BladOdczytuZgloszony` | `rozwiazanie_id, weryfikacja_id` | powiadomienie `NOWA_WERYFIKACJA` |
| `OdpowiedzZapisana` | `odpowiedz_id` | `AKTUALIZUJ_PROFIL` |
| `TestDiagnostycznyZakonczony` | `test_diagnostyczny_id` | `AKTUALIZUJ_PROFIL` |
| `ZadanieOpublikowane` | `zadanie_id, zasieg` | — (dostępne w doborze, gdy `GLOBALNY`) |
| `ZadanieDoPrzegladu` | `zadanie_id` | powiadomienie `ZADANIE_DO_PRZEGLADU` dla zlecającego Nauczyciela, recenzentów treści i Administratorów |
| `WkladkaDoPrzegladu` | `wkladka_teoretyczna_id` | powiadomienie `WKLADKA_DO_PRZEGLADU` dla recenzentów treści i Administratorów |
| `PrzypisanieUtworzone` | `przypisanie_id` | powiadomienia `NOWE_PRZYPISANIE` |
| `ZgloszenieBleduZadaniaUtworzone` | `zgloszenie_id, zadanie_id` | powiadomienie `ZGLOSZENIE_BLEDU_ZADANIA` dla recenzentów i Administratorów; przy 3 otwartych od różnych Nauczycieli `INSERT INTO wstrzymanie_w_doborze … ON CONFLICT DO NOTHING` (ADR-038) |
| `KontoUsuwane` | `uzytkownik_id, rola` | publikowane przez funkcję domenową `SluzbaKont.zastosuj_wniosek_o_usuniecie` — w transakcji wniosku `api` **i** w ponowieniu `WNIOSEK_O_USUNIECIE` przez `ponow_zdarzenia_rodo` (16.4, C4-01); reakcje wykonywane w tej samej transakcji (UPDATE Weryfikacji, INSERT powiadomień), prace w tle wstawiane do `praca_w_tle`; (Nauczyciel) eskalacja jego Weryfikacji `KLASA` z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA`, zwolnienie jego blokad `W_TRAKCIE` **z zachowaniem szkiców**; flagi `weryfikator_globalny` i `recenzent_tresci` pozostają do `USUN_KONTO` (konto `USUWANE` nie loguje się i nie jest liczone w puli G-7); `KLASA_DO_PRZEKAZANIA` dla Administratorów (ADR-044, C3-08) |
| `KontoPrzywrocone` | `uzytkownik_id, rola` | publikowane przez `SluzbaKont.zastosuj_anulowanie_usuniecia` — w `api` i w `ponow_zdarzenia_rodo` (C4-01); wznowienie procesów wg tabeli przywrócenia 16.4 właściwej dla `rola` (ADR-040); (Nauczyciel) także koniec kontroli przyjęć Klas z warunku „konto `USUWANE`” (11.4.3): (Uczeń) ponowne zlecenie przerwanych prac, nowe `termin_at` otwartych Weryfikacji, `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` dla Rozwiązań bez aktualnej kopii; (Nauczyciel) powrót Weryfikacji eskalowanych z powodu karencji do kolejki `KLASA`, oznaczenie nieprzeczytanych `KLASA_DO_PRZEKAZANIA` jako nieaktualne (16.4) |

---

## 14. Obsługa błędów

### 14.1 Macierz błędów

| # | Awaria | Wykrycie | Reakcja systemu | Skutek dla użytkownika |
|---|---|---|---|---|
| E-01 | Zdjęcie > 10 MB, > 5 zdjęć, zły format | walidacja `api` przed zapisem | `413` / `422 ZA_DUZO_ZDJEC` / `415`; nic nie zapisano | komunikat i możliwość ponowienia; kompresja w kliencie zapobiega większości przypadków |
| E-02 | Magazyn obiektów niedostępny przy przesyłaniu | wyjątek klienta S3 | rollback transakcji; obiekty już zapisane usuwa `SPRZATAJ_OSIEROCONE_OBIEKTY` (codziennie, buckety `zdjecia/`, `zdjecia-maskowane/`, `zbior-treningowy/` porównywane z `zdjecie_rozwiazania` i `kopia_treningowa`, ADR-047) | `503 USLUGA_NIEDOSTEPNA`, `Retry-After: 5`; klient ponawia z tym samym `Idempotency-Key` |
| E-03 | Dostawca rozpoznawania: timeout (14 s), 429, 5xx | `BladModeluPrzejsciowy` | ponowienia pracy po 1 s i 3 s; po 3. próbie `BLAD_ROZPOZNAWANIA` (`PRZEKROCZONY_CZAS`/`BLAD_DOSTAWCY`); naruszenie NFR-01 liczone w metryce | „Nie udało się odczytać zdjęcia z powodu awarii usługi” + akcje: „Spróbuj ponownie” (te same zdjęcia, ≤ 5 razy, bez zużycia przesłania) lub „Porzuć i zacznij od nowa” |
| E-04 | Zdjęcie nieczytelne | `czytelne = false` | `BLAD_ROZPOZNAWANIA` (`NIECZYTELNE`) bez ponowień | wskazówki fotograficzne (światło, kadr, czytelne pismo) + „Wyślij nowe zdjęcia” (łącznie ≤ 3 przesłania) lub „Porzuć i zacznij od nowa” (nowe Rozwiązanie dla tego samego elementu do `wygasa_at`) |
| E-05 | Odpowiedź modelu niezgodna ze schematem | walidacja Pydantic | 1 ponowienie z komunikatem błędu walidacji w promptcie; potem jak E-03 | jak E-03 |
| E-06 | > 40 kroków | walidacja wyniku | `BLAD_ROZPOZNAWANIA` (`ZBYT_WIELE_KROKOW`) | prośba o zwięźlejsze zapisanie rozwiązania i nowe zdjęcia (wpis ręczny nie istnieje, ADR-024) |
| E-07 | Kartka dotyczy innego Zadania | `zgodnosc_z_zadaniem < 0.5` | ostrzeżenie, bez blokady | baner „Kartka może dotyczyć innego zadania” na ekranie Transkrypcji |
| E-08 | Jev: timeout (2 s), 429, 5xx, błędny format | adapter + circuit breaker | ponowienie tylko przy zapasie czasu (9.9), potem model zapasowy dla całego planu jednym wywołaniem; metryka `jev_bledy_total` | brak (dłuższy czas oceny; budżet 17.2) |
| E-09 | Model zapasowy niedostępny lub błędny | wyjątek po ponowieniu | ponowienia pracy `OCEN_ROZWIAZANIE` do terminu twardego 120 s; po nim T-6: Weryfikacja `BLAD_PRZETWARZANIA` + `PONOW_OCENE` | „Rozwiązanie zostanie ocenione przez nauczyciela” (brak Oceny wstępnej); przy udanym `PONOW_OCENE` Ocena pojawia się później |
| E-10 | SymPy: błąd parsowania, przekroczenie 2 s CPU / 512 MB | wyjątek / sygnał w procesie potomnym | wynik `NIEROZSTRZYGNIETE`; proces potomny zastępowany | brak |
| E-11 | Agent wyjaśniający: błąd, timeout 8 s, wyciek rozwiązania | wyjątek / strażnik 10.5.3 | ponowienie pracy `NAPISZ_WSKAZOWKE` (raz), potem szablon | Ocena widoczna od razu; Wskazówka dochodzi (z szablonu) w ≤ 20 s |
| E-12 | Awaria workera w trakcie pracy | wygaśnięcie dzierżawy (60 s) | odzyskanie pracy; handler idempotentny (warunki 13.3) | opóźnienie ≤ 60 s; dla `OCEN_ROZWIAZANIE` ponowienie do terminu twardego, po nim T-6 |
| E-13 | Rezultat obliczeń przestarzały (stan zmienił się w trakcie) | `UPDATE … WHERE status = :s AND version = :v` zwraca 0 wierszy | rezultat odrzucony, log `rezultat_przestarzaly` | brak |
| E-14 | PostgreSQL niedostępny / przełączenie | błąd połączenia | `api`: `503`; worker: wstrzymanie pobierania, ponawianie połączenia co 2 s (maks. 60 s backoff) | komunikat „Spróbuj za chwilę”; PWA ponawia `GET` automatycznie |
| E-15 | Konflikt edycji (dwa urządzenia) | `If-Match` | `412 KONFLIKT_WERSJI` + aktualny zasób w ciele | UI pokazuje aktualną wersję i pyta, czy nadpisać |
| E-16 | Weryfikacja podjęta przez innego Nauczyciela | `UPDATE … RETURNING` = 0 | `409 WERYFIKACJA_ZAJETA` z danymi blokującego | komunikat „Sprawdza: {imię nazwisko} do {godzina}” |
| E-17 | Walidacja Zadania wygenerowanego negatywna | sprawdzenia W-x | do 3 cykli generowania z listą niespełnionych sprawdzeń; potem zlecenie `NIEUDANE` | Nauczyciel (zlecający) widzi `NIEUDANE` i przyczynę; dla puli — metryka |
| E-18 | Import arkusza z błędami | walidacja schematu | import `ODRZUCONY`, raport per numer Zadania, nic nie zapisano | Administrator poprawia ZIP i ponawia |
| E-19 | Brak Zadań w strefie rozwoju | dobór 10.1.2 krok 7 | Zadanie najbliższe + `UZUPELNIJ_PULE` | uzasadnienie z faktycznym P(sukces) |
| E-20 | Brak opublikowanej Wkładki teoretycznej | dobór 10.1.2 krok 6 | Zadanie zamiast Wkładki, Seria rozpoczęta, praca `GENERUJ_SZKIC_WKLADKI` (szkic → `DO_PRZEGLADU` → powiadomienie recenzentów), metryka `brak_wkladki_total{umiejetnosc}` | Zadanie zamiast Wkładki do czasu publikacji szkicu |
| E-21 | Trwałe pogorszenie trafności auto-ocen | bezpiecznik trafności 11.5.3: dolna granica Wilsona 95% błędu Kontroli losowych powyżej bieżącego progu > 2% przy `n_eff ≥ 50` (ADR-046; przy 0 błędach nigdy) | podniesienie progu o 0.02 względem progu sprzed przebiegu i Blokada obniżania progu do UB ≤ 2% przy `n_eff ≥ 189` (kroki 1–6 11.5.3, C4-02); wyzwolenie przy progu 0,990 → tryb pełnej weryfikacji (1.000, D-16), wyjście przez dezaktywację; przy UB > 2% i LB ≤ 2% (`n_eff ≥ 189`) — tylko blokada obniżania | więcej Ocen wstępnych |
| E-22 | Zaległe usuwanie zdjęć > 24 h | metryka `zdjecia_zalegle` | alert krytyczny dla Administratora | brak |
| E-23 | Niepowodzenie wysyłki e-mail | błąd SMTP | 5 ponowień (1–60 min) | e-mail resetu dociera z opóźnieniem |
| E-24 | Nieznany wyjątek w handlerze | catch-all w pętli workera | log z `trace_id`, traktowany jak błąd przejściowy (ponowienia), potem `po_wyczerpaniu` | jak dla danego typu pracy |
| E-25 | Nieznany wyjątek w `api` | handler globalny FastAPI | `500` z `kod = BLAD_WEWNETRZNY` i `instance` = `trace_id`, bez szczegółów | komunikat ogólny |
| E-26 | Brak jakiegokolwiek Zadania w zakresie Ucznia | dobór 10.1.2 krok 8 | `200` z `brak_elementu`, `UZUPELNIJ_PULE` dla Poziomów trudności 2–4 | komunikat 10.1.5 i sugerowane Działy |
| E-27 | Q-RE-01 (rekomendacja): timeout 1,5 s, błąd, obwód otwarty | wyjątek w fasadzie | polityka `POLITYKA_ELO`, `powrot_do_polityki = MODEL_NIEDOSTEPNY` | brak (rekomendacja bez strategii eksperymentalnej) |
| E-28 | Pusta lub niewystarczająca pula weryfikatorów globalnych | `weryfikatorzy_globalni_aktywni_7d < wymagani` (17.7) lub `zaleglosc_globalna_min > 2 × pojemnosc` | alert `ALERT_WERYFIKACJE`; kontrola przyjęć 11.4.3 | Uczniowie bez Klasy dostają więcej zadań zamkniętych; po SLA status „opóźniona” |
| E-29 | Kontrole losowe niewykonywane | `wykonanie_kontroli_30d < 0.90` | reguły nadzoru 11.5.3 (alert → blokada obniżania progu → bezpiecznik) | więcej Ocen wstępnych |
| E-30 | Budżet generowania systemowego wyczerpany | licznik 12.7 | `UZUPELNIJ_PULE` i `GENERUJ_SZKIC_WKLADKI` przesunięte na następną dobę, alert przy 80% | dobór Zadań spoza strefy rozwoju (E-19); Zadanie zamiast Wkładki (E-20) |
| E-31 | Poprawka Transkrypcji przekracza limit twardy | walidacja `PUT …/transkrypcja` (9.4) | `422 POPRAWKI_ZBYT_ROZLEGLE`, nic nie zapisano | komunikat „Zmieniono zbyt dużą część odczytu — wyślij nowe zdjęcia kartki” + akcja „Wyślij nowe zdjęcia” |
| E-32 | Sprawdzenie zgodności poprawek: błąd, timeout 8 s lub brak rezultatu przed zapisem Oceny | `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` po wyczerpaniu prób / brak `zgodnosc_poprawek` | T-7: Ocena wstępna, Weryfikacja z powodem `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM` (Z-3); metryka `sprawdzenie_poprawek_brak_total` | Ocena wstępna z komunikatem T-7 |
| E-33 | Plan Jev nieprzyjęty przez globalny ogranicznik, brak tokenu przed terminem fali (13.2) lub `429` od Jev | `przyjmij_plan` = `None` / `zarezerwuj_wywolanie` = `False` / HTTP 429 | Ocena modelem zapasowym (`OGRANICZENIE_LOKALNE`), przydział × 0,5 na 30 s po 429; obwód nie jest otwierany | dłuższy czas oceny w granicach budżetu S-4 |
| E-34 | Pula Zadań diagnostycznych niepełna | `SPRAWDZ_PULE_DIAGNOSTYCZNA`, dobór 9.1 | test częściowy (`pominiete_dzialy`) albo `409 TEST_NIEDOSTEPNY` przy < 50%; `ALERT_PULA_DIAGNOSTYCZNA` | Uczeń zaczyna z Profilem `DOMYSLNA` i może wykonać test później |
| E-35 | Zdjęcie o nadmiernych wymiarach (bomba dekompresyjna) lub nieprawidłowy plik | odczyt nagłówka w piaskownicy `api`, `MAX_IMAGE_PIXELS`, `RLIMIT_AS` | `422 ZDJECIE_NIEPRAWIDLOWE`; proces piaskownicy zastępowany po przekroczeniu limitu | komunikat „Nie udało się odczytać zdjęcia — zrób je ponownie aparatem w aplikacji” |
| E-36 | Bucket `rejestr-zdarzen-rodo` niedostępny przy Zdarzeniu RODO | wyjątek klienta S3 przed transakcją | `503 USLUGA_NIEDOSTEPNA`, nic nie zapisano w bazie (ADR-041); w `USUN_KONTO` — błąd przejściowy pracy, ponowienie co 1 h, żaden krok w bazie nie startuje przed zapisem obiektu | „Spróbuj za chwilę”; PWA ponawia z tym samym `Idempotency-Key` |
| E-37 | Filtr danych osobowych: krok z treścią matematyczną (po wyłączeniu tokenów imienia, nazwiska, inicjałów i oznaczenia klasy, C4-03) w ramce danych osobowych (`konflikt_filtra`) | Filtr 10.3.1 p. 3a | treść kroku zachowana (poza zastąpieniami b1/b2/c), T-3 (`PODEJRZENIE_BLEDU_ODCZYTU`) — Ocena wstępna wstrzymana do Weryfikacji (11.1 p. 7d); metryka `konflikt_filtra_total` | „Ocena wstrzymana — możliwy błąd odczytu…” (komunikat T-3) |
| E-38 | Obiekty magazynu bez wiersza w bazie (awaria między zapisem obiektu a transakcją albo odtworzenie PITR) | `SPRZATAJ_OSIEROCONE_OBIEKTY` | usunięcie obiektu (tryb `CODZIENNY` po 1 h; tryb `ODTWORZENIE` przed startem workerów), metryka; przyrost > 100 obiektów na dobę w trybie `CODZIENNY` → `ALERT_RODO` (ADR-047) | brak |

### 14.2 Terminy oceny i reguła T-6

- **Termin miękki** (NFR-01): `termin_miekki = zatwierdzono_at + max(10 s,
  28 s − t_R)`, gdzie `t_R = transkrypcja_gotowa_at − przeslano_at` (28 s =
  SLO serwerowe, 17.1; C2-07). Handler
  `OCEN_ROZWIAZANIE` przekazuje go do wszystkich wywołań: timeouty są
  skracane do `min(timeout_roli, termin_miekki − teraz)`, a ponowienie Jev
  następuje tylko przy zapasie czasu (9.9). Przekroczenie terminu miękkiego
  **nie** przerywa oceniania — jest rejestrowane jako naruszenie NFR-01.
- **Termin twardy:** `termin_twardy = zatwierdzono_at + 120 s`. Jeżeli minie
  przed zapisaniem Oceny (np. oba modele decyzyjne niedostępne), handler w
  jednej transakcji: ustawia Rozwiązanie na `OCZEKUJE_NA_WERYFIKACJE`,
  zapisuje `czas_systemowy_ms` (naruszenie), tworzy Weryfikację
  `BLAD_PRZETWARZANIA` (priorytet 1, `ocena_weryfikowana_id = NULL`) i zleca
  `PONOW_OCENE`.
- **`PONOW_OCENE`** ma pełną semantykę w 9.13: działa tylko na Weryfikacji
  `OCZEKUJE` (Nauczyciel, który podjął Weryfikację, ma pierwszeństwo), zapis
  jest warunkowy na `weryfikacja.version`; wynik trasy Weryfikacja dołącza
  Ocenę `WSTEPNA` i **zastępuje** powód `BLAD_PRZETWARZANIA` powodami z 11.3
  (CHECK `ocena_weryfikowana_id IS NOT NULL OR 'BLAD_PRZETWARZANIA' = ANY
  (powody)` pozostaje spełniony), a wynik trasy automatycznej wystawia Ocenę
  `OSTATECZNA` i anuluje Weryfikację (`PONOWNA_OCENA_AUTOMATYCZNA`). W obu
  przypadkach powstaje Wynik zadania według 10.1.3, ustawiane jest
  `ocena_wystawiona_at` i wysyłane powiadomienie `OCENA_WYSTAWIONA`.

---

## 15. Współbieżność i spójność

### 15.1 Zasady

1. **Jedna transakcja = zmiana stanu + zlecenie kolejnych prac** (brak
   podwójnego zapisu baza/kolejka).
2. **Długie operacje poza transakcją**, zapis warunkowy z `status` i
   `version`.
3. **Unikalne indeksy częściowe** egzekwują niezmienniki „co najwyżej jeden”.
   Ponieważ PostgreSQL nie pozwala odroczyć indeksu częściowego, kolejność
   operacji jest stała: **najpierw** zmiana statusu starego wiersza (np.
   Ocena → `ZASTAPIONA`, Weryfikacja → `ANULOWANA`), **potem** wstawienie
   nowego.
4. **Optymistyczna blokada** (`version` + `If-Match`) dla edycji przez ludzi
   (zasób wskazany w 12.3); dla Weryfikacji `version` jest wersją treści,
   niezmienianą przez operacje na blokadzie (ADR-037); **pesymistyczna** (`FOR UPDATE`) tylko dla
   krótkich sekcji: Profil ucznia, wiersz `rozwiazanie` przy rozstrzygnięciu,
   korekcie i zgłoszeniu, wiersz `kalibracja_trudnosci`.
5. Statystyki aktualizowane automatycznie (kalibracja Poziomu trudności)
   są aktualizowane atomowo w SQL i nie zmieniają `version` encji edytowanych
   przez ludzi (ADR-029).
6. Poziom izolacji: `READ COMMITTED` (domyślny); niezmienniki wymagające
   więcej są wyrażone indeksami unikalnymi lub warunkami `UPDATE … WHERE`.

### 15.2 Scenariusze wyścigów

| # | Scenariusz | Mechanizm | Rezultat |
|---|---|---|---|
| C-01 | Podwójne kliknięcie „Wyślij” przy Rozwiązaniu | `Idempotency-Key` (`klucz_idempotencji`) + `ux_rozwiazanie_element` / `ux_rozwiazanie_przypisanie` | jedno Rozwiązanie; drugie żądanie dostaje tę samą odpowiedź |
| C-02 | Dwa urządzenia wysyłają Rozwiązanie do tego samego elementu Ścieżki nauki lub tej samej pozycji Przypisania | indeksy jw. | drugie: `409` z `id` istniejącego Rozwiązania |
| C-03 | Edycja Transkrypcji na dwóch urządzeniach | `If-Match` na `rozwiazanie.version` | drugie: `412` |
| C-04 | Edycja Transkrypcji równolegle z zatwierdzeniem | oba wymagają `If-Match` (Rozwiązanie); zatwierdzenie zmienia stan | przegrany dostaje `412` lub `409` |
| C-05 | Ponowne dostarczenie `OCEN_ROZWIAZANIE` (awaria workera po zapisie, przed `ZAKONCZONA`) | warunek „brak Oceny” + `UNIQUE (rozwiazanie_id, wersja)` | druga próba kończy się bez efektu |
| C-06 | Dwa workery oceniają to samo Rozwiązanie (dzierżawa wygasła, pierwszy wciąż liczy) | zapis warunkowy `WHERE status = 'OCENIANIE' AND version = :v`; drugi worker podnosi `version` przy przejęciu | wynik pierwszego odrzucony (E-13) |
| C-07 | Dwóch Nauczycieli podejmuje tę samą Weryfikację | SQL podjęcia 9.6.1 w wersji 2.2+ (`WHERE id = :id AND (status = 'OCZEKUJE' OR (status = 'W_TRAKCIE' AND zablokowana_do < now()) OR (status = 'W_TRAKCIE' AND podjeta_przez = :ja AND podjeta_at + interval '2 hours' > now())) AND <uprawnienie> AND <konto Ucznia AKTYWNE>`); jedna instrukcja `UPDATE … RETURNING` | jeden wygrywa, drugi `409 WERYFIKACJA_ZAJETA`; właściciel nie przedłuża blokady poza `podjeta_at + 2 h` (C2-16, C3-09) |
| C-08 | Blokada Weryfikacji wygasła, inny Nauczyciel ją przejął, pierwszy wysyła rozstrzygnięcie | rozstrzygnięcie wymaga `podjeta_przez = ja AND zablokowana_do > now()` oraz `If-Match` (Weryfikacja); szkic pierwszego zachowany w `szkic_rozstrzygniecia` | pierwszy: `409 WERYFIKACJA_NIE_PODJETA`, po ponownym podjęciu odzyskuje szkic |
| C-09 | Rozstrzygnięcie Weryfikacji (Kontrola losowa) równolegle z korektą tego samego Rozwiązania | obie operacje: `SELECT rozwiazanie FOR UPDATE`; rozstrzygnięcie: `If-Match` Weryfikacji + warunek „Ocena weryfikowana ≠ ZASTAPIONA”; korekta: `If-Match` Rozwiązania i zamknięcie otwartej Weryfikacji (9.6.3); `ux_ocena_aktualna` | pierwsza operacja wygrywa; druga dostaje `412 KONFLIKT_WERSJI`, `409 OCENA_ZMIENIONA` albo `409 WERYFIKACJA_ZAJETA`; powstaje dokładnie jedna Próbka kalibracyjna (`ux_probka_ocena_auto`) |
| C-10 | Opinia pomocnicza dociera po rozstrzygnięciu | zapis `UPDATE weryfikacja SET opinia_pomocnicza = … WHERE id = :id AND opinia_pomocnicza IS NULL` bez warunku na status (pole wyłącznie informacyjne, nie zmienia `version`) | opinia zapisana do metryk, bez wpływu na Ocenę |
| C-11 | Równoległe aktualizacje Profilu ucznia (Odpowiedź + Ocena + Test diagnostyczny) | deduplikacja `AKTUALIZUJ_PROFIL` tylko wśród prac `OCZEKUJE` (`ux_praca_dedup_oczekujace`) + `SELECT … FROM profil_ucznia FOR UPDATE` + pętla do pustego wyniku + znaczniki `zastosowany_w_profilu_at` + `ZAMIATAJ_PROFILE` co 5 min | każdy Wynik zadania zastosowany **co najmniej raz** (zawsze istnieje praca oczekująca albo zamiatacz) i **co najwyżej raz** (znacznik), w kolejności `created_at` (C-32) |
| C-12 | Dwa równoległe `GET …/rekomendacje/nastepna` | `ux_jeden_proponowany` | oba zwracają ten sam element |
| C-13 | Rekomendacja czyta Profil w trakcie aktualizacji | `READ COMMITTED` — migawka zatwierdzona | dopuszczalne opóźnienie ≤ 1 Wyniku zadania |
| C-14 | Zmiana Progu pewności w trakcie Oceny | Ocena używa migawki progu i kalibratora wczytanej na starcie i zapisuje `prog_pewnosci_id`, `kalibrator_pewnosci_id` | spójna decyzja, audytowalna |
| C-15 | Zmiana aktywnej Konfiguracji modelu w trakcie Oceny | jak C-14 (`konfiguracja_modelu_id`) | jw. |
| C-16 | Nauczyciel poprawia Zadanie, gdy Rozwiązania są w toku | Rozwiązanie wskazuje `zadanie_wersja_id` (niezmienne); element wskazuje wersję z chwili rozpoczęcia | ocena według wersji, którą Uczeń widział |
| C-17 | Uczeń wysyła Rozwiązanie lub Odpowiedź do wersji Zadania, która przestała być aktualna | reguła wersji akceptowanej 12.5 (wersja elementu albo 72 h dla Przypisania) | przyjęte, ocenione według tej wersji; wersja starsza niż reguła → `409 WERSJA_ZADANIA_NIEAKTUALNA` |
| C-18 | Dwa zlecenia uzupełnienia tej samej puli | `ux_praca_dedup` z kluczem `umiejetnosc:poziom:poziom_matury:rodzaj` + warunek „brak otwartego zlecenia dla klucza” | jedna praca, jedno zlecenie |
| C-19 | Dwie instancje schedulera | `pg_try_advisory_lock` | jedna aktywna |
| C-20 | Żądanie usunięcia konta w trakcie oceniania | żądanie ustawia tylko `uzytkownik.status = USUWANE` (okres karencji, ADR-040); handlery sprawdzają status konta przed zapisem i kończą się z `KONTO_USUWANE`; `USUN_KONTO` po 30 dniach w kroku 0 anuluje otwarte Weryfikacje (`ANULOWANA`, `USUNIECIE_KONTA`) i Rozwiązania nieterminalne (`ANULOWANE`) przed usunięciem obiektów | brak zapisu do danych konta w okresie karencji; stan wstrzymany, odwracalny przywróceniem (C-33) |
| C-21 | Kalibracja równolegle z zapisem nowych próbek | kalibracja czyta okno do `now() − 5 min` | deterministyczny zbiór próbek |
| C-22 | Ponowny import tego samego Arkusza CKE | `arkusz_cke.kod UNIQUE` | drugi import `ODRZUCONY` |
| C-23 | `PONOW_OCENE` kończy się, gdy Nauczyciel właśnie podjął Weryfikację `BLAD_PRZETWARZANIA` | podjęcie zmienia `status` na `W_TRAKCIE` (bez zmiany `version`, ADR-037); zapis `PONOW_OCENE` jest warunkowy `WHERE status = 'OCZEKUJE' AND version = :v`; warunek wykonania pracy wymaga `OCZEKUJE` | wynik `PONOW_OCENE` odrzucony (E-13); Nauczyciel ocenia bez zakłóceń; brak kolizji na `UNIQUE (rozwiazanie_id, wersja)` |
| C-24 | Równoległe Wyniki zadania wielu Uczniów dla tego samego Zadania (kalibracja Poziomu trudności) | atomowy `UPDATE kalibracja_trudnosci SET trudnosc_b = trudnosc_b − …` z `FOR UPDATE` w CTE (10.1.3); `zadanie.version` niezmieniane | brak utraconych aktualizacji; Przegląd Nauczyciela (`If-Match` Zadania) nie dostaje `412` z powodu kalibracji |
| C-25 | Zgłoszenie błędu odczytu równolegle z rozstrzygnięciem Kontroli losowej | `SELECT rozwiazanie FOR UPDATE` w obu; zgłoszenie dopisuje powód do otwartej Weryfikacji albo — po jej zamknięciu — tworzy nową wskazującą aktualną (nową) Ocenę | brak utraty zgłoszenia; zgłoszenie dotyczy Oceny aktualnej w chwili zapisu |
| C-26 | Korekta, gdy otwarta Kontrola losowa jest `W_TRAKCIE` u innego Nauczyciela | 9.6.3 p. 1 | `409 WERYFIKACJA_ZAJETA`; korekta możliwa po zwolnieniu lub wygaśnięciu blokady |
| C-27 | Dwa żądania „Dalej” przy aktywnej Serii | `ux_jeden_proponowany` + `UPDATE sciezka_nauki … WHERE version = :v` w tej samej transakcji co INSERT elementu | jeden element, jeden przyrost `seria_liczba_elementow` |
| C-28 | Zgłoszenie błędu odczytu do Weryfikacji `W_TRAKCIE`, gdy Nauczyciel właśnie wysyła rozstrzygnięcie | oba: `SELECT rozwiazanie FOR UPDATE`; zgłoszenie zmienia treść, więc trigger podnosi `weryfikacja.version` (ADR-037); rozstrzygnięcie wymaga `If-Match` Weryfikacji | gdy pierwsze jest zgłoszenie: rozstrzygnięcie `412` z aktualnym zasobem, blokada i szkic zachowane, Nauczyciel rozstrzyga po przejrzeniu zgłoszenia; gdy pierwsze jest rozstrzygnięcie: zgłoszenie dotyczy nowej Oceny Nauczyciela i tworzy nową Weryfikację (11.6 — limit 1 zgłoszenia na Rozwiązanie nie został jeszcze zużyty) |
| C-29 | Zgłoszenie błędu odczytu równolegle z `AKTUALIZUJ_PROFIL` stosującym ten sam Wynik zadania | zawieszenie ustawia `uniewazniony_at` w transakcji z `FOR UPDATE rozwiazanie`; `AKTUALIZUJ_PROFIL` bierze `FOR UPDATE profil_ucznia` i przetwarza Wyniki z nieprzetworzonym unieważnieniem (`ix_wynik_do_profilu`) | Wynik zastosowany przed zawieszeniem zostaje cofnięty przy kolejnym przebiegu (odtworzenie); żaden Wynik nie zostaje w Profilu po zawieszeniu |
| C-30 | Ta sama pozycja Przypisania rozwiązywana z elementu `R-PRZYPISANIE` i z listy `GET /uczniowie/ja/przypisania` (dwa urządzenia) | `trg_kontekst_proby` dziedziczy `przypisanie_id` z elementu; `ux_rozwiazanie_przypisanie` / `ux_odpowiedz_przypisanie` na (Uczeń, Przypisanie, Zadanie) | drugie żądanie: `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN` z `id` istniejącego; jedna Weryfikacja u Nauczyciela Przypisania; klucz Zadania zamkniętego ukryty do terminu w obu drogach (C1-21) |
| C-31 | Rozstrzygnięcie po 40 min pracy i 8 przedłużeniach blokady | przedłużenie i podjęcie nie zmieniają `version` (`trg_weryfikacja_wersja`, ADR-037); rozstrzygnięcie z `If-Match` = ETag z podjęcia | `200`; `412` wyłącznie, gdy w międzyczasie zmieniła się treść (np. zgłoszenie — C-28), z polem `zmiany_tresci` (C2-02) |
| C-32 | Wynik zadania zatwierdzony, gdy `AKTUALIZUJ_PROFIL` tego Ucznia jest `W_TOKU` i już odczytał Wyniki | zlecenie nowej pracy nie koliduje z pracą `W_TOKU` (deduplikacja tylko `OCZEKUJE`); nowa praca czeka na `FOR UPDATE profil_ucznia`; pętla do pustego wyniku; `ZAMIATAJ_PROFILE` | Wynik zastosowany najpóźniej w kolejnym przebiegu (≤ 5 min także przy awarii); `wynik_najstarszy_niezastosowany_sekundy` > 300 → alert (C2-04) |
| C-33 | Żądanie usunięcia konta, a następnie przywrócenie w okresie karencji | żądanie nie zmienia stanów (ADR-040); przywrócenie w jednej transakcji: `status = AKTYWNE`, Zdarzenie RODO `ANULOWANIE_USUNIECIA`, `termin_at := dodaj_dni_robocze(now(), 3)` otwartych Weryfikacji, ponowne zlecenie przerwanych prac (16.4) | zawieszone Wyniki i otwarte zgłoszenia są rozstrzygane normalnie; Kontrole losowe wracają do `π_wykonanie`; Rozwiązania z minionym terminem twardym trafiają do T-6 (C2-08) |
| C-34 | Odtworzenie bazy z PITR do punktu sprzed żądania usunięcia konta lub odwołania zgody | Zdarzenia RODO w buckecie Object Lock zapisane **przed** transakcją; `ponow_zdarzenia_rodo` stosuje zdarzenia późniejsze niż punkt odtworzenia, zanim wystartują `worker` i `scheduler` (16.4, ADR-041) | konto wraca do `USUWANE` z pierwotnym `usuniecie_zaplanowane_at`, zgoda do `false`, kopie treningowe z `usun_po = now()`; żadna kopia nie powstaje wbrew odwołanej zgodzie (C2-09) |
| C-35 | `PUT …/transkrypcja` (nowa wersja Poprawek) równolegle z zakończeniem `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` dla poprzedniej wersji | rezultat zapisywany `WHERE id = :transkrypcja_id` (wersja, którą sprawdzano); `OCEN_ROZWIAZANIE` czyta `zgodnosc_poprawek` **zatwierdzonej** wersji | rezultat starej wersji nie dotyczy nowej; brak rezultatu dla wersji zatwierdzonej → T-7 (C2-01) |
| C-36 | Trzech Nauczycieli zgłasza błąd tego samego Zadania `GLOBALNY` równolegle | `ux_zgloszenie_zadania_otwarte`; każda transakcja zgłoszenia najpierw wykonuje `SELECT 1 FROM zadanie WHERE id = :id FOR UPDATE` (blokada bez modyfikacji wiersza — serializuje zgłoszenia), potem `INSERT` zgłoszenia i `INSERT INTO wstrzymanie_w_doborze (zadanie_id, liczba_zgloszen) SELECT :id, count(DISTINCT nauczyciel_id) FROM zgloszenie_bledu_zadania WHERE zadanie_id = :id AND status = 'OTWARTE' HAVING count(DISTINCT nauczyciel_id) >= 3 ON CONFLICT (zadanie_id) DO NOTHING` | wstrzymanie ustawione dokładnie raz; dobór pomija Zadanie od następnej rekomendacji (C2-03); `zadanie.version` bez zmian, więc recenzent z `If-Match` Z nie dostaje `412` po nowym zgłoszeniu (C3-09) |
| C-37 | Nauczyciel rozstrzyga Weryfikację, a Uczeń w tym czasie składa wniosek o usunięcie konta (C3-08) | rozstrzygnięcie i korekta w transakcji z `SELECT rozwiazanie FOR UPDATE` odczytują stan konta Ucznia przez `SELECT status FROM uzytkownik WHERE id = :uczen_id FOR SHARE` (wersja 2.4, C4-06 p. 4 — w wersji 2.3 odczyt bez blokady wiersza `uzytkownik`, a transakcja wniosku nie blokowała `rozwiazanie`, więc wniosek zatwierdzony między sprawdzeniem a `COMMIT` dawał Ocenę Nauczyciela po wniosku); `UPDATE uzytkownik SET status = 'USUWANE'` wniosku czeka na zwolnienie `FOR SHARE` albo `FOR SHARE` czeka na `COMMIT` wniosku; kolejność blokad 9.6.2 wyklucza zakleszczenie | rozstrzygnięcie zatwierdzone przed wnioskiem — Ocena Nauczyciela istnieje (przetwarzanie przed wnioskiem); zatwierdzane po wniosku — `409 KONTO_UCZNIA_NIEAKTYWNE`, szkic zachowany, po przywróceniu konta Nauczyciel podejmuje Weryfikację ponownie |
| C-38 | Autozapis Poprawki (`PUT`) w trakcie wykonywania `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` dla poprzedniej wersji (C3-05) | deduplikacja po `rozwiazanie_id` tylko wśród `OCZEKUJE` (nowa praca zlecana obok pracy `W_TOKU`); zapis rezultatu `UPDATE transkrypcja … WHERE id = :transkrypcja_id AND zgodnosc_poprawek IS NULL` i warunek „najnowsza wersja” | rezultat dla starszej wersji jest odrzucany (rezultat przestarzały); Ocena czeka na rezultat wersji zatwierdzonej |
| C-39 | Odtworzenie bazy z PITR do punktu `T` w trakcie `USUN_KONTO` (po kroku 1, przed krokiem 5) albo w trakcie usunięcia konta Nauczyciela (C4-01) | dla `USUNIECIE_WYKONANE` kryterium zastosowania = „wiersz `uzytkownik` nie istnieje”, a nie „wiersz `zdarzenie_rodo` istnieje”; `ponow_zdarzenia_rodo` przegląda cały prefiks `wykonane/` i wykonuje idempotentne kroki właściwe dla `dane.rola` tymi samymi funkcjami domenowymi co `USUN_KONTO` (16.4) | konto usunięte przed startem workerów, dane Nauczyciela spseudonimizowane w uzasadnieniach rekomendacji; G-6 (a3), (a4) |
| C-40 | Start 3. repliki workera w szczycie „koniec lekcji”, gdy 2 workery i 2 `api` wykorzystują pełny przydział (C4-05) | odnowienia wierszy `limit_szybkosci_modelu` pod `pg_advisory_xact_lock` klucza; nowy wiersz: pusty kubełek, `r = min(formuła, 0,8 × limit − Σ przydziałów pozostałych)` i `B = min(formuła, B_całk − Σ pojemności pozostałych)`; zmniejszenia stosowane przed `COMMIT`, zwiększenia po | Σ przydziałów ≤ 40/s i Σ pojemności ≤ 34 w każdej chwili, ≤ 74 wywołania w dowolnym oknie 1 s; nowa replika osiąga 12,5/s po ≤ 12 s; test 13.2 (f)–(h), G-3 (c) |

---

## 16. Bezpieczeństwo i RODO

### 16.1 Uwierzytelnianie i sesje (ADR-020)

- Hasła: argon2id (m = 64 MiB, t = 3, p = 4), minimum 10 znaków, odrzucanie
  haseł z listy 100 000 najczęstszych.
- Access token JWT (EdDSA Ed25519, 15 min) w pamięci aplikacji (nie w
  `localStorage`); refresh token 256-bit losowy, 30 dni, w cookie
  `HttpOnly; Secure; SameSite=Strict`, rotowany przy każdym użyciu;
  ponowne użycie unieważnionego tokenu unieważnia całą `rodzina_id`.
- Klucze podpisu rotowane co 90 dni (`kid` w nagłówku JWT, 2 klucze aktywne).
- Konto Nauczyciela wymaga zatwierdzenia przez Administratora
  (`OCZEKUJE_NA_ZATWIERDZENIE`), bo Nauczyciel widzi dane Uczniów. Flagi
  `weryfikator_globalny` i `recenzent_tresci` nadaje wyłącznie
  Administrator (audyt).

### 16.2 Ochrona aplikacji

- TLS 1.2+ (Caddy, HSTS 1 rok), CSP `default-src 'self'; img-src 'self' blob: https://<magazyn>`.
- Zdjęcia serwowane wyłącznie przez podpisane URL-e (10 min), bucket prywatny.
  Obcy weryfikator globalny dostaje podpisany URL wyłącznie do obiektu
  maskowanego (ADR-031).
- Wstrzykiwanie poleceń w modelach: treść od Ucznia (Transkrypcja) jest
  umieszczana w wydzielonej sekcji stanu/promptu; prompty systemowe zabraniają
  wykonywania instrukcji z danych; o punktach decyduje model decyzyjny z
  zamkniętą listą odpowiedzi, a niespójności (11.2.1) kierują do Weryfikacji.
- Piaskownica SymPy i Pillow: osobne procesy, `RLIMIT_AS = 512 MiB`,
  `RLIMIT_CPU = 2 s` (SymPy) / 5 s (Pillow), brak dostępu do sieci (procesy
  uruchamiane bez zmiennych z sekretami), `parse_latex` zamiast
  `eval`/`sympify` na danych Ucznia. Piaskownica istnieje w **obu**
  procesach przetwarzających obrazy (ADR-045): w `worker` (4 procesy:
  SymPy, maskowanie) i w `api` (`ProcessPoolExecutor` z 2 procesami na
  replikę: normalizacja przesłanych zdjęć, limit czasu ściennego 8 s na
  plik, `Image.MAX_IMAGE_PIXELS = 40 000 000`, odczyt nagłówka przed
  dekodowaniem, limit 12 000 px na wymiar). Pętla zdarzeń `api` czeka na
  wynik przez `run_in_executor`, więc dekodowanie nie blokuje obsługi
  innych żądań; oczekiwanie na wolny proces > 5 s → `503`.
- Sekrety (klucze API modeli, JWT, S3, `sekret_pseudonimow` dla HMAC
  pseudonimów 11.4.4) w zmiennych środowiskowych z menedżera sekretów
  dostawcy chmury; nigdy w bazie. Rotacja `sekret_pseudonimow` zmienia
  pseudonimy (akceptowalne — pseudonim służy tylko do rozróżniania
  Uczniów w kolejce).

### 16.3 Kategorie danych osobowych

| Dane | Kategoria | Podstawa przetwarzania | Odbiorcy |
|---|---|---|---|
| e-mail, imię, nazwisko | identyfikacyjne | umowa (regulamin, art. 6 ust. 1 lit. b RODO) | SMTP; Nauczyciele Klas Ucznia i Nauczyciel docelowy Weryfikacji |
| Zdjęcia kartek | treść tworzona przez Ucznia (może zawierać pismo, imię) | umowa | dostawca roli `ROZPOZNAWANIE_PISMA` (A-07, retencja zero); Nauczyciele jak wyżej (oryginał); obcy weryfikator globalny — wyłącznie wersja maskowana |
| Transkrypcje, Oceny, Profil ucznia | wyniki edukacyjne | umowa | dostawcy ról decyzyjnych i generatywnych (bez danych identyfikacyjnych w stanie — gwarantuje to Filtr danych osobowych przed zapisem Transkrypcji, ADR-042); obcy weryfikator globalny — tylko Rozwiązanie objęte Weryfikacją, z pseudonimem |
| e-mail, imię, nazwisko Nauczyciela i Administratora | identyfikacyjne | umowa | Uczniowie Klas Nauczyciela (imię i nazwisko), Administratorzy; usuwane wg 16.4 (ADR-044) |
| Zdjęcia w zbiorze treningowym | jw. | zgoda (`zgoda_na_trening`), odwołalna | brak |

**Kolejka globalna (C1-02):** sprawdzenie Rozwiązania przez Nauczyciela
spoza Klasy jest częścią usługi oceniania (FR-24…FR-26) i odbywa się na
podstawie umowy. Zasada minimalizacji jest realizowana pseudonimizacją
(11.4.4): obcy weryfikator globalny nie poznaje imienia, nazwiska, Klasy ani
innych Rozwiązań Ucznia, a zdjęcia widzi z zamaskowanymi danymi osobowymi.
Formularz rejestracji (krok „Regulamin”) zawiera informację: „Jeśli nie
należysz do klasy nauczyciela w Lemat, Twoje rozwiązania może sprawdzić
nauczyciel współpracujący z platformą. Nie zobaczy on Twojego imienia i
nazwiska.”. Weryfikatorzy globalni podpisują umowę o zachowaniu poufności
(warunek nadania flagi, G-6).

Do modeli nigdy nie są wysyłane: e-mail, imię, nazwisko, identyfikatory
kont. Stan Jev i prompty zawierają wyłącznie treść Zadania i Transkrypcję,
a Transkrypcja jest zapisywana dopiero po Filtrze danych osobowych (10.3.1
p. 3a), więc imię i nazwisko z kartki nie trafiają do stanu Jev, promptów
LLM, `WeryfikacjaSzczegoly.transkrypcje` obcego weryfikatora ani kopii
treningowej. Jedynym wyjątkiem jest wejście roli `ROZPOZNAWANIE_PISMA`
(zdjęcie, na którym dane mogą być widoczne) — dostawca z retencją zero
(A-07). Skuteczność filtra mierzy G-2/G-6 (≥ 50 kartek ZT-1 z imieniem i
nazwiskiem: 0 wystąpień danych konta w zapisanych Transkrypcjach; ≥ 98%
obszarów danych osobowych zamaskowanych na zdjęciach).

### 16.4 Retencja i usuwanie (NFR-09)

| Dane | Retencja | Mechanizm |
|---|---|---|
| Zdjęcia Rozwiązań (oryginał i maskowane) | `usun_po` utrzymywane przez przejścia stanów (poniżej); twardy limit 90 dni od przesłania (`CHECK`), także gdy Weryfikacja trwa — Nauczyciel widzi wtedy tylko Transkrypcję | `USUN_ZDJECIA` (9.10) |
| Zdjęcia w zbiorze treningowym (kopie treningowe) | 24 miesiące lub do odwołania zgody albo usunięcia konta (usunięcie obiektów w ≤ 1 h, deklaracja w polityce prywatności: ≤ 7 dni) | praca `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` tworzy kopię (obiekt pod pseudonimem) i wiersz `kopia_treningowa` (jedyne powiązanie z kontem); `USUN_KOPIE_TRENINGOWE` usuwa obiekty z `usun_po < now()`; obiekty bez wiersza (awaria między zapisem obiektu a transakcją, odtworzenie PITR) usuwa `SPRZATAJ_OSIEROCONE_OBIEKTY` (ADR-047, C3-06) |
| Transkrypcje, Oceny, Profil ucznia, Rozwiązania (bez zdjęć) | do usunięcia konta | `USUN_KONTO` |
| `decyzja_modelu.stan` | 180 dni (potem tylko liczby i skrót stanu); przy usunięciu konta natychmiast | `WYCZYSC_DANE_TECHNICZNE`, `USUN_KONTO` |
| `decyzja_modelu.uczen_id`, `rozwiazanie_id` | do usunięcia konta (potem `NULL`) | `USUN_KONTO` |
| Rekomendacje (`cechy`, `uzasadnienie_fakty`) | do usunięcia konta | kaskada `uczen` |
| Próbki kalibracyjne | bezterminowo, bez danych osobowych (`pseudonim_ucznia`, liczby) | `rozwiazanie_id`, `weryfikacja_id` → `NULL` przy usunięciu konta |
| Szkice rozstrzygnięć | do zamknięcia Weryfikacji | kaskada / usunięcie przy `ZAKONCZONA`/`ANULOWANA` |
| Logi aplikacyjne | 30 dni; bez treści zdjęć, Transkrypcji i haseł | konfiguracja loggera (maskowanie pól) |
| `dziennik_audytu` | 2 lata | partycje roczne |
| Rejestr Zdarzeń RODO (bucket `rejestr-zdarzen-rodo`): Zdarzenia RODO | 60 dni (≥ okres karencji 30 dni + okno PITR 14 dni + zapas 16 dni) | S3 Object Lock w trybie COMPLIANCE; kopia w tabeli `zdarzenie_rodo` |
| `zdarzenie_rodo` (baza) | 2 lata (rozliczalność, art. 5 ust. 2 RODO); bez danych osobowych | `WYCZYSC_DANE_TECHNICZNE` |
| Dane u dostawców modeli | rozpoznawanie: 0 dni; pozostałe role: ≤ 30 dni (monitorowanie nadużyć) | umowy powierzenia (A-07), weryfikowane w G-6 i przy aktywacji konfiguracji (13.1) |
| Konta nieaktywne | 24 miesiące od `termin_egzaminu` (lub od ostatniego logowania, jeśli brak terminu) → e-mail z ostrzeżeniem → usunięcie po 30 dniach | `USUN_KONTO` |

**Przejścia ustawiające `zdjecie_rozwiazania.usun_po`** (jedyne źródło
prawdy, C1-10, C1-12):

| Zdarzenie | `usun_po` |
|---|---|
| przesłanie zdjęcia | `created_at + 90 dni` |
| każda transakcja, po której Rozwiązanie jest `OCENIONE` bez otwartej Weryfikacji (Ocena automatyczna bez Kontroli, rozstrzygnięcie, korekta, zamknięcie ostatniej Weryfikacji, `PONOW_OCENE`), gdy `uczen.zgoda_na_trening = true` | jak wiersze „→ `OCENIONE`” i „zamknięcie ostatniej otwartej Weryfikacji” poniżej; dodatkowo w tej samej transakcji praca `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` (kopia musi powstać przed `usun_po`; C2-16) |
| Rozwiązanie → `PORZUCONE` lub `ANULOWANE` | `now()` |
| Rozwiązanie → `OCENIONE` bez otwartej Weryfikacji | `least(created_at + 90 dni, now() + 30 dni)` |
| otwarcie Weryfikacji na Rozwiązaniu `OCENIONE` (Kontrola losowa, zgłoszenie) | `created_at + 90 dni` |
| zamknięcie ostatniej otwartej Weryfikacji Rozwiązania `OCENIONE` | `least(created_at + 90 dni, now() + 30 dni)` |

**Usunięcie konta** na wniosek Ucznia (`DELETE /uczniowie/ja`) — okres
karencji (ADR-040, C2-08):

1. `api` zapisuje Zdarzenie RODO `WNIOSEK_O_USUNIECIE` do bucketu
   `rejestr-zdarzen-rodo/zdarzenia/{rrrr-mm-dd}/{uzytkownik_id}/{id}.json`
   (Object Lock COMPLIANCE, 60 dni; `{id, uzytkownik_id, typ, zrodlo: "API",
   created_at, dane: {rola, usuniecie_zaplanowane_at}}` — `rola` w każdym
   zdarzeniu od wersji 2.4, C4-01), a **potem** w jednej transakcji:
   `status = USUWANE`, `usuniecie_zaplanowane_at = now() + 30 dni`, wiersz
   `zdarzenie_rodo`, unieważnienie refresh tokenów (brak logowania).
   Niedostępny bucket → `503`, nic nie zapisano (E-36).
2. Procesy **nie są anulowane**, tylko wstrzymane: Rozwiązania i
   Weryfikacje zachowują stany, handlery prac w tle kończą się z
   `KONTO_USUWANE`, Weryfikacje znikają z Kolejki weryfikacji, kopie
   treningowe nie powstają, zdjęcia zachowują swoje `usun_po` (≤ 90 dni od
   przesłania; usunięcie wszystkich w kroku 4 `USUN_KONTO`). Kontrole
   losowe kont `USUWANE` są pomijane w liczniku i mianowniku
   `wykonanie_kontroli_30d` i `π_wykonanie` (11.5.3).

**Przywrócenie** w okresie karencji (`POST
/admin/uzytkownicy/{id}/przywrocenie`): zapis Zdarzenia RODO
`ANULOWANIE_USUNIECIA` do bucketu, potem jedna transakcja:

| Element | Działanie przy przywróceniu |
|---|---|
| konto | `status = AKTYWNE`, `usuniecie_zaplanowane_at = NULL`, wiersz `zdarzenie_rodo` |
| otwarte Weryfikacje (`OCZEKUJE`; `W_TRAKCIE` z wygasłą blokadą) | wracają do Kolejki weryfikacji, `termin_at := dodaj_dni_robocze(now(), 3)` (SLA wstrzymany przez karencję); zgłoszenia błędu odczytu i zawieszone Wyniki zadania pozostają i są rozstrzygane normalnie (limit „1 zgłoszenie na Rozwiązanie” nie jest zwracany, bo zgłoszenie nadal trwa) |
| Rozwiązania `PRZESLANE`/`ROZPOZNAWANIE` | nowa praca `ROZPOZNAJ_PISMO` (`ponowienie_nr + 1`, `przeslano_at := now()`), o ile zdjęcia istnieją; w przeciwnym razie `PORZUCONE` |
| Rozwiązania `TRANSKRYPCJA_GOTOWA`/`BLAD_ROZPOZNAWANIA` | `updated_at := now()` (pełne 7 dni do `PORZUC_NIEZATWIERDZONE`) |
| Rozwiązania `ZATWIERDZONE`/`OCENIANIE` | nowa praca `OCEN_ROZWIAZANIE`; termin twardy (120 s od `zatwierdzono_at`) jest już miniony, więc handler od razu wykonuje T-6 (Weryfikacja `BLAD_PRZETWARZANIA` + `PONOW_OCENE`) |
| Rozwiązania `OCZEKUJE_NA_WERYFIKACJE` z Oceną wstępną | bez zmian; Wynik zadania z wagą 0,5 pozostaje w Profilu do rozstrzygnięcia |
| Wyniki zadania | bez zmian; praca `AKTUALIZUJ_PROFIL` dla Ucznia |
| Kontrole losowe | wracają do `π_wykonanie` z nowym `termin_at` |
| Rozwiązania `OCENIONE` bez otwartej Weryfikacji (C3-08) | gdy `zgoda_na_trening = true`, zdjęcia istnieją i brak `kopia_treningowa` z `ocena_id` = Ocena aktualna — praca `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` (kopia, która nie powstała w karencji) |
| Weryfikacje rozstrzygnięte w karencji | nie występują: rozstrzygnięcie i korekta wymagają konta Ucznia `AKTYWNE` (`409 KONTO_UCZNIA_NIEAKTYWNE`, 9.6.2, 9.6.3), więc w karencji nie powstaje Ocena Nauczyciela, Wynik zadania ani Próbka kalibracyjna (ADR-040, C3-08) |

Po 30 dniach `USUN_KONTO` wykonuje w kolejności (wersja 2.3, C3-03 —
zdarzenie wykonawcze zapisuje `worker`, a jego wiersz w bazie powstaje w
pierwszej transakcji):

0. zapis Zdarzenia RODO `USUNIECIE_WYKONANE` (`{id, uzytkownik_id, typ,
   zrodlo: "WORKER", created_at, dane: {rola, zaplanowano_at}}`, `id` =
   `uuid5(NS_RODO, 'USUNIECIE_WYKONANE:' || uzytkownik_id)`) do bucketu
   `rejestr-zdarzen-rodo/wykonane/{uzytkownik_id}/{id}.json` — klucz **bez
   daty** (wersja 2.4, C4-06 p. 3: w wersji 2.3 klucz zawierał datę zapisu,
   więc ponowienie w innym dniu UTC — przez `UZGODNIJ_REJESTR_RODO` po 2 h
   albo po PITR — tworzyło drugi obiekt z tym samym `id` mimo `If-None-Match:
   *`) (Object Lock COMPLIANCE, 60 dni, `If-None-Match: *` — istniejący obiekt
   oznacza ponowienie pracy w dowolnym dniu i jest akceptowany, a jego
   `created_at` pozostaje pierwotne) — **przed** jakąkolwiek
   zmianą w bazie; `UZGODNIJ_REJESTR_RODO` nigdy nie unieważnia obiektów
   `zrodlo = WORKER` (13.3);
1. jedna transakcja: `INSERT INTO zdarzenie_rodo (id, uzytkownik_id, typ,
   zrodlo, dane, klucz_obiektu) … ON CONFLICT (id) DO NOTHING` oraz
   anulowanie procesów — Weryfikacje otwarte → `ANULOWANA`
   (`USUNIECIE_KONTA`), Rozwiązania nieterminalne → `ANULOWANE` (worker,
   który pobrał pracę wcześniej, zastaje stan terminalny, C-20); wiersz
   `zdarzenie_rodo` nie ma klucza obcego, więc przeżywa krok 5;
2. `UPDATE decyzja_modelu SET stan = NULL, uczen_id = NULL, rozwiazanie_id
   = NULL WHERE uczen_id = :id` (wszystkie partycje; indeks
   `ix_decyzja_uczen`);
3. `UPDATE probka_kalibracyjna SET rozwiazanie_id = NULL, weryfikacja_id =
   NULL` dla Rozwiązań Ucznia;
4. usunięcie obiektów zdjęć (oryginał i maskowane) Ucznia z magazynu
   (prefiks `zdjecia/{uczen_id}/`, `zdjecia-maskowane/{uczen_id}/`);
4a. usunięcie obiektów kopii treningowych (`kopia_treningowa.klucz_obiektu`
   Ucznia, także już wygasłych) i `DELETE FROM kopia_treningowa WHERE
   uczen_id = :id` — `ON DELETE RESTRICT` gwarantuje, że krok 5 nie
   powiedzie się, jeśli 4a pominięto;
5. `DELETE FROM uzytkownik WHERE id = :id` (kaskady);
6. wpis `dziennik_audytu` (`KONTO_USUNIETE`, `uzytkownik_id` bez danych
   osobowych).

**Odwołanie i udzielenie zgody na trening** (`PATCH /uczniowie/ja` z
`zgoda_na_trening`): przed transakcją `api` zapisuje do bucketu Zdarzenie
RODO `ODWOLANIE_ZGODY` albo `UDZIELENIE_ZGODY` (`dane: {rola: "UCZEN"}`);
transakcja zmienia
`uczen.zgoda_na_trening`, zapisuje `zdarzenie_rodo` i (odwołanie) ustawia
`kopia_treningowa.usun_po := now()`.

**Kopie zapasowe PostgreSQL** (PITR 14 dni) zawierają usunięte dane do czasu
rotacji — opisane w polityce prywatności. Rejestr Zdarzeń RODO zawiera **wszystkie
Zdarzenia RODO** (ADR-041, C2-09), nie tylko wykonane usunięcia. Procedura
odtworzenia (runbook, testowana w G-6):

1. odtworzenie bazy do punktu `T`; procesy `worker` i `scheduler` **nie są
   uruchamiane** (żadna praca w tle, w tym `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`,
   nie może działać na stanie sprzed zdarzeń), `api` działa w trybie
   konserwacji (`503`);
2. skrypt `ponow_zdarzenia_rodo` czyta z bucketu `rejestr-zdarzen-rodo` (poza
   odtwarzaną bazą) dwa zbiory obiektów: (i) zdarzenia `zrodlo = API` z
   prefiksów `zdarzenia/{rrrr-mm-dd}/` od daty `T − 5 min` do dziś, z
   `created_at > T − 5 min` (zakładka na rozbieżność zegarów i czas
   transakcji po zapisie obiektu); (ii) **wszystkie** obiekty prefiksu
   `wykonane/` (`USUNIECIE_WYKONANE`, `zrodlo = WORKER`) bez filtra czasu —
   retencja 60 dni ≥ okno PITR 14 dni, a obiekt zapisany przed `T − 5 min`
   może dotyczyć usunięcia, którego kroki 2–5 trwały jeszcze po `T` (przy
   górnym oszacowaniu 30 usunięć kont na dobę zbiór ma ≤ 1 800 obiektów, 2
   wywołania LIST).
   Zdarzenia są stosowane chronologicznie po `created_at`, a dla jednego
   konta `USUNIECIE_WYKONANE` zawsze na końcu. **Kryterium „skutek już
   zastosowany” zależy od źródła (wersja 2.4, C4-01):**

   | Źródło zdarzenia | Zdarzenie pomijane, gdy | Uzasadnienie |
   |---|---|---|
   | `API` | unieważnione obiektem `UNIEWAZNIENIE` **albo** jego `id` jest w `zdarzenie_rodo` | wiersz powstaje w tej samej transakcji co skutek, więc jego istnienie oznacza zatwierdzony skutek |
   | `WORKER` (`USUNIECIE_WYKONANE`) | wiersz `uzytkownik` o tym `uzytkownik_id` **nie istnieje** | wiersz `zdarzenie_rodo` powstaje w kroku 1, na **początku** usunięcia; punkt `T` między krokiem 1 a krokiem 5 (aktualizacja partycji `decyzja_modelu`, usuwanie obiektów, `DELETE`) daje bazę z wierszem zdarzenia **i** pełnymi danymi konta, więc wersja 2.3 (pomijanie po `id`) zostawiała konto `USUWANE` z danymi do `UZGODNIJ_REJESTR_RODO` (≥ 2 h) albo nocnego `USUN_KONTO` |

   Zdarzenie `API` dla konta, którego wiersza `uzytkownik` nie ma w
   odtworzonej bazie (konto założone po `T`), nie ma czego zmienić: skrypt
   zapisuje tylko wiersz `zdarzenie_rodo` (rozliczalność) i metrykę
   `ponowienie_rodo_brak_konta_total`. **Ponowienie zależy od roli konta**
   (`dane.rola` obiektu; od wersji 2.4 każde Zdarzenie RODO ma `rola`, CHECK
   7.3) i wywołuje **te same funkcje domenowe** co `api` i `USUN_KONTO`, więc
   skutki i zdarzenia domenowe (13.6) są identyczne:

   | Zdarzenie | `UCZEN` | `NAUCZYCIEL` | `ADMINISTRATOR` |
   |---|---|---|---|
   | `WNIOSEK_O_USUNIECIE` | `SluzbaKont.zastosuj_wniosek_o_usuniecie`: `status = USUWANE`, `usuniecie_zaplanowane_at` z danych zdarzenia (pierwotny termin, nie przesunięty), unieważnienie refresh tokenów | to samo **oraz** skutki `KontoUsuwane` w tej samej transakcji: zwolnienie blokad `W_TRAKCIE` (szkice zachowane), eskalacja jego Weryfikacji `KLASA` z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA`, powiadomienia `KLASA_DO_PRZEKAZANIA`, kontrola przyjęć jego Klas (11.4.3) | jak `UCZEN` (bez procesów; ochrona ostatniego Administratora nie jest sprawdzana — zdarzenie zostało już przyjęte) |
   | `ANULOWANIE_USUNIECIA` | `SluzbaKont.zastosuj_anulowanie_usuniecia`: tabela przywrócenia **Ucznia** (16.4, „Przywrócenie w okresie karencji”) i `KontoPrzywrocone` | tabela przywrócenia **Nauczyciela** (16.4, „Przywrócenie konta Nauczyciela lub Administratora”) i `KontoPrzywrocone` | wiersz konta |
   | `USUNIECIE_WYKONANE` | `SluzbaKont.wykonaj_usuniecie`, kroki 1–6 `USUN_KONTO` (krok 0 jest już zapisany) | kroki (1)–(5) usunięcia konta Nauczyciela (archiwizacja nieprzekazanych Klas i zamknięcie Przypisań, `WYCOFANE` dla Zadań `PRYWATNY`, pseudonimizacja `rekomendacja.uzasadnienie` i `uzasadnienie_fakty` reguły `R-PRZYPISANIE`, `DELETE`, audyt) | kroki (1), (4), (5) |
   | `ODWOLANIE_ZGODY` / `UDZIELENIE_ZGODY` | `zgoda_na_trening = false` i `usun_po := now()` kopii / `zgoda_na_trening = true` | — (zdarzenia tylko dla Ucznia) | — |

   Kroki usunięcia są idempotentne (`ON CONFLICT DO NOTHING`, `UPDATE …
   WHERE`, usuwanie obiektów po prefiksie, pseudonimizacja tekstu imienia i
   nazwiska odczytanego z wiersza `uzytkownik`, który istnieje aż do
   `DELETE`), więc skrypt wykonuje je w całości bez względu na to, w którym
   kroku przerwano oryginalne `USUN_KONTO`, i niezależnie od tego, ile czasu
   minęło od usunięcia (wersja 2.2 unieważniała to zdarzenie po 5 min, C3-03).
   Prace w tle zlecone przez funkcje domenowe (np. ponowne `ROZPOZNAJ_PISMO`
   po przywróceniu) są tylko wstawiane do `praca_w_tle` i wykonują się po
   kroku 3. Każde zastosowanie zdarzenia `API` zapisuje wiersz `zdarzenie_rodo`
   z tym samym `id` i `zrodlo`; dla `WORKER` wiersz zapisuje krok 1
   (`ON CONFLICT DO NOTHING`), a brakujący wpis audytu `KONTO_USUNIETE` —
   krok 6;
2a. (ADR-047, C3-06) praca `SPRZATAJ_OSIEROCONE_OBIEKTY` w trybie
   `ODTWORZENIE` z `od = T − 5 min`, wykonywana przez skrypt odtworzenia:
   usuwa obiekty `zbior-treningowy/`, `zdjecia/` i `zdjecia-maskowane/`
   utworzone po punkcie odtworzenia, których wiersze (`kopia_treningowa`,
   `zdjecie_rozwiazania`) przepadły przy PITR — kopia treningowa bez
   wiersza nie mogłaby zostać usunięta po odwołaniu zgody ani przez
   `USUN_KONTO` (ADR-035); Uczeń, którego zdjęcia zostały w ten sposób
   usunięte, ma Rozwiązanie z przesłaniem sprzed `T` albo żadnego (wiersze
   po `T` przepadły), więc żadne istniejące Rozwiązanie nie traci zdjęć;
3. dopiero po zakończeniu skryptu startują `worker` i `scheduler`.

Zdarzenie `zrodlo = API` zapisane w buckecie, którego transakcja się nie
powiodła (np. awaria bazy po zapisie obiektu), jest w ciągu 10 min
(cykl co 5 min, obiekty starsze niż 5 min — 13.3, C4-06 p. 2)
unieważniane przez `UZGODNIJ_REJESTR_RODO` obiektem kompensującym; zdarzenie
`zrodlo = WORKER` nie jest unieważniane, tylko doprowadzane do stanu
docelowego (ponowne `USUN_KONTO`, 13.3). Gdy odtworzenie nastąpi w
oknie przed unieważnieniem, takie zdarzenie zostanie zastosowane — w
kierunku ochrony danych (usunięcie wstrzymane z karencją 30 dni i
możliwością przywrócenia, odwołanie zgody), co jest akceptowanym skutkiem
ubocznym (Z-3).

**Usunięcie konta Nauczyciela lub Administratora (ADR-044, C2-14):**
żądanie `DELETE /nauczyciele/ja` albo `POST
/admin/uzytkownicy/{id}/usuniecie` (Administrator; ostatniego aktywnego
Administratora nie można usunąć — `409 OSTATNI_ADMINISTRATOR`) zapisuje
Zdarzenie RODO `WNIOSEK_O_USUNIECIE` i ustawia `USUWANE` z karencją 30 dni.
Wersja 2.3 (C3-08) — w karencji jedynym natychmiastowym skutkiem dla
procesów jest to, co chroni Uczniów przed utratą SLA: blokady `W_TRAKCIE`
tego Nauczyciela są zwalniane (`zablokowana_do := now()`; **szkice
zachowane**), jego Weryfikacje `KLASA` eskalowane do `GLOBALNA` z
`powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA` (11.4.3 p. 1), a
Administratorzy dostają `KLASA_DO_PRZEKAZANIA` dla każdej
niezarchiwizowanej Klasy. Flagi `weryfikator_globalny` i
`recenzent_tresci` **nie są odbierane** (konto `USUWANE` nie może się
zalogować, a pula G-7 i przydział liczą tylko konta `AKTYWNE`); znikają
razem z wierszem `nauczyciel` w `USUN_KONTO`.

**Przywrócenie konta Nauczyciela lub Administratora** (`POST
/admin/uzytkownicy/{id}/przywrocenie`, Zdarzenie RODO
`ANULOWANIE_USUNIECIA`, jedna transakcja):

| Element | Działanie przy przywróceniu |
|---|---|
| konto | `status = AKTYWNE`, `usuniecie_zaplanowane_at = NULL`, wiersz `zdarzenie_rodo`; flagi `weryfikator_globalny`, `recenzent_tresci` i `deklarowane_minuty_weryfikacji` bez zmian (nie były odbierane) |
| szkice rozstrzygnięć | bez zmian (zachowane przy zwolnieniu blokad); Nauczyciel odzyskuje je przy ponownym podjęciu (9.6.1) |
| Weryfikacje z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA` i `status = OCZEKUJE` (także `W_TRAKCIE` z wygasłą blokadą) | `typ_kolejki := KLASA`, `eskalowano_at := NULL`, `powod_eskalacji := NULL`, `termin_at := dodaj_dni_robocze(now(), 3)`; Weryfikacje podjęte w międzyczasie przez weryfikatora globalnego (ważna blokada) lub zakończone — bez zmian |
| Klasy nieprzekazane | pozostają przy Nauczycielu; nieprzeczytane powiadomienia `KLASA_DO_PRZEKAZANIA` dla tych Klas dostają `przeczytane_at := now()` i `dane = dane || '{"nieaktualne": true}'` |
| Klasy przekazane w karencji (`POST /admin/klasy/{id}/przekazanie`) | **nie wracają** automatycznie (decyzja Administratora; D-15); Administrator może przekazać je ponownie |
| Administrator | wyłącznie wiersz konta; brak procesów do wznowienia |

Przywrócenie konta Nauczyciela jest więc bezstratne poza Klasami, które
Administrator świadomie przekazał (ADR-040, ADR-044). Przekazanie
Klasy (`POST /admin/klasy/{id}/przekazanie`) przenosi na nowego
Nauczyciela Klasę, jej otwarte Przypisania i ich Zestawy zadań, Zadania
`PRYWATNY` użyte w tych Przypisaniach (`autor_id`) oraz otwarte Weryfikacje
`KLASA` (`nauczyciel_docelowy_id`). Po 30 dniach `USUN_KONTO`:
(1) Zdarzenie RODO `USUNIECIE_WYKONANE` zapisane przez `worker` (`zrodlo =
WORKER`, `dane.rola = NAUCZYCIEL` albo `ADMINISTRATOR`, klucz bez daty)
przed pierwszą zmianą w bazie i wiersz `zdarzenie_rodo` w pierwszej
transakcji, jak w krokach 0–1 dla Ucznia (C3-03, C4-06 p. 3); kroki
(1)–(5) są idempotentne i ponawiane w całości przez `USUN_KONTO` oraz
`ponow_zdarzenia_rodo`, dopóki istnieje wiersz `uzytkownik` (C4-01); (2) Klasy nieprzekazane →
`zarchiwizowana = true`, a ich Przypisania z `termin > now()` → `termin :=
now()` (zamknięte); Zadania `PRYWATNY` autora nieprzekazane → `WYCOFANE`;
(3) pseudonimizacja tekstów z danymi Nauczyciela: `rekomendacja.uzasadnienie`
i `uzasadnienie_fakty` reguły `R-PRZYPISANIE` — imię i nazwisko zastępowane
tekstem „nauczyciel (konto usunięte)”; (4) `DELETE FROM uzytkownik` —
klucze obce `klasa.nauczyciel_id`, `zestaw_zadan.nauczyciel_id`,
`przypisanie.nauczyciel_id`, `ocena.nauczyciel_id`,
`weryfikacja.nauczyciel_docelowy_id`/`podjeta_przez`,
`zgloszenie_bledu_zadania.nauczyciel_id`, `zadanie.autor_id`,
`import_arkusza.administrator_id`, `przeglad_zadania.autor_przegladu_id`,
`konfiguracja_modelu.utworzona_przez`, `prog_pewnosci.utworzony_przez`
przyjmują `NULL` (`ON DELETE SET NULL`), `nauczyciel` i `szkic_rozstrzygniecia`
— kaskada; Oceny Nauczyciela zachowują `zrodlo = NAUCZYCIEL` bez wskazania
osoby; (5) `dziennik_audytu` zachowuje `uzytkownik_id` (UUID bez danych
osobowych, niepowiązany już z kontem — pseudonim) przez 2 lata ze względu na
rozliczalność.

**Magazyn obiektów:** buckety `zdjecia` i `zdjecia-maskowane` mają
**wyłączone wersjonowanie**, więc `DELETE` usuwa obiekt trwale; reguła
cyklu życia: usuwanie niedokończonych wysyłek multipart po 1 dniu oraz
reguła zabezpieczająca „wygaśnięcie obiektów starszych niż 91 dni” (druga
linia obrony dla twardego limitu 90 dni). Zdjęcia nie są objęte kopiami
zapasowymi ani replikacją (utrata zdjęć wpływa wyłącznie na Weryfikację —
ryzyko zaakceptowane). Bucket `zbior-treningowy` ma tę samą konfigurację z
regułą 24 miesięcy. Spójność magazynu z bazą zapewnia
`SPRZATAJ_OSIEROCONE_OBIEKTY` (codziennie i jako krok 2a procedury
odtworzenia, ADR-047): przy 1 500 Rozwiązaniach/dobę i ≈ 3 zdjęciach
lista obejmuje ≈ 0,8 mln obiektów w `zdjecia/` i `zdjecia-maskowane/`
(90 dni) oraz ≤ 1,1 mln w `zbior-treningowy/` (24 miesiące, 50% zgód) —
≈ 1 900 wywołań LIST i ≈ 1 900 zapytań `SELECT … WHERE klucz_obiektu = ANY
(:partia_1000)` na przebieg, ≈ 6 min.

Eksport danych (`GET /uczniowie/ja/eksport-danych`) realizuje prawo do
przenoszenia danych.

---

## 17. Wymagania niefunkcjonalne — realizacja, budżety, obciążenie, obserwowalność, wdrożenie

### 17.1 Realizacja NFR

| NFR | Mechanizm | Miara / weryfikacja |
|---|---|---|
| NFR-01 (≤ 30 s) | Interpretacja A-11 (do akceptacji w G-0): **czas systemowy** `czas_systemowy_ms = (transkrypcja_gotowa_at − przeslano_at) + (ocena_wystawiona_at − zatwierdzono_at)`; budżet 17.2; osobne pule workera; termin miękki `max(10 s, 28 s − t_R)` skracający timeouty; globalny ogranicznik szybkości Jev (ADR-039); Wskazówka poza ścieżką krytyczną (ADR-025); termin twardy 120 s | histogram `ocena_czas_systemowy_sekundy{model}`; SLO miesięczne: ≥ 99% Rozwiązań z `czas_systemowy_ms ≤ 28 000` (czas widoczny z odpytywaniem ≤ 30 s), p95 ≤ 20 s (Jev) / ≤ 23 s (model zapasowy); **T-6 i `BLAD_ROZPOZNAWANIA` z kodem `BLAD_DOSTAWCY`/`PRZEKROCZONY_CZAS` liczą się jako naruszenie**; `odsetek_t6 ≤ 0,2%`; metryka informacyjna end-to-end `czas_od_przeslania_do_oceny_widocznej_sekundy` (z czasem Ucznia na sprawdzenie Transkrypcji) |
| NFR-02 (≥ 90% zgodności) | Zgodność Oceny (ADR-027: punkty + pierwszy błędny krok + brakujący krok); bramka G-1 na Transkrypcjach referencyjnych ZT-1 i G-2 end-to-end; online: Próbki kalibracyjne z produkcji (z definicji end-to-end: zdjęcie → Transkrypcja zatwierdzona → Ocena), ważone IPW dla populacji progu oraz nieważone dla wszystkich źródeł | `zgodnosc_z_nauczycielem_30d{model,rodzaj}`; alert < 0,90; raport `/admin/kalibracja` |
| NFR-03 (≤ 2% błędnych auto) | Próg z górną granicą Wilsona ≤ 2% przy `n_eff ≥ 189` (11.5.3), kalibracja out-of-fold, Kontrola losowa z terminem 3 dni roboczych, reguły nadzoru braków danych, bezpieczniki reagujące na dowód przekroczenia (LB Wilsona > 2%, ADR-046) | `blad_automatycznych_30d`, `gorna_granica_bledu_30d`, `wykonanie_kontroli_30d` |
| NFR-04 (kalibracja) | Kalibrator izotoniczny per (model, rodzaj) dopasowany 5-krotną walidacją krzyżową; cel: prawdopodobieństwo Zgodności Oceny; ECE na wartościach out-of-fold; wykres niezawodności | ECE ≤ 0.03 (alert > 0.05) |
| NFR-05 (wyjaśnialność) | `decyzja_modelu` dla każdego pytania (rozkład), `ocena_kryterium.prawdopodobienstwo`, `ocena.uzasadnienie` (11.7, eksponowane w `OcenaWyjscie`), Wskazówka generowana z decyzji; `rekomendacja.cechy/decyzja/uzasadnienie_fakty` i szablon dla każdej reguły | test: każda Ocena automatyczna ma ≥ 1 rekord `decyzja_modelu` i niepuste `uzasadnienie`; każda Rekomendacja ma niepuste `uzasadnienie`; test pokrycia szablonów wszystkich wartości `regula` |
| NFR-06 (wymienność modeli) | Porty 13.1 (`PlanPytan` niezależny od modelu), rejestr adapterów, `konfiguracja_modelu`, `import-linter` | zmiana dostawcy = konfiguracja + ewaluacja ZT-1 |
| NFR-07 (model zapasowy) | Circuit breaker + `ZapasowyLlmAdapter` wykonujący cały plan jednym wywołaniem, z osobnym kalibratorem i progiem; reguła ponowienia Jev zależna od zapasu czasu | G-4 (trzy scenariusze awarii) |
| NFR-08 (telefon i komputer, aparat) | PWA responsywna (breakpointy 360/768/1280 px), `capture="environment"`, kompresja w kliencie | testy E2E Playwright na profilach iPhone 13, Pixel 7, Desktop Chrome |
| NFR-09 (RODO, usuwanie zdjęć) | 16.3, 16.4: `usun_po` jako jedyne źródło prawdy, `decyzja_modelu.uczen_id`, rejestr Zdarzeń RODO poza bazą (ADR-041), okres karencji z wstrzymaniem procesów (ADR-040), Filtr danych osobowych w Transkrypcji (ADR-042), usuwanie kont wszystkich ról (ADR-044), wyłączone wersjonowanie bucketów, retencja u dostawców | metryki `zdjecia_zalegle = 0`, `dane_osobowe_w_transkrypcji_total`; test `ponow_zdarzenia_rodo` (G-6, trzy scenariusze) |
| NFR-10 (język polski) | i18n `pl-PL` jako jedyny język, komunikaty błędów API po polsku, format dat `dd.MM.yyyy`, liczby z przecinkiem | lint: brak literałów UI poza plikami tłumaczeń |

### 17.2 Budżet czasowy (NFR-01)

**Faza R — rozpoznanie** (`t_R`, od `przeslano_at` do Transkrypcji
widocznej):

| Etap | Limit | Cel p95 |
|---|---|---|
| Normalizacja zdjęć (piaskownica `api`, 2 procesy na replikę; ≈ 80 ms na zdjęcie JPEG z kompresji klienta, ≈ 400 ms na HEIC 12 Mpx), zapis S3, transakcja (`przeslano_at` = przyjęcie żądania) | 8 s na plik | 1.5 s |
| Oczekiwanie w puli `ROZPOZNAWANIE` (poza szczytem) | — | 0.5 s |
| Wywołanie modelu rozpoznającego | 14 s | 9 s |
| Post-processing, maskowanie, zapis | — | 0.5 s |
| Odpytywanie klienta | interwał 1 s | +1 s |
| **Razem `t_R`** (serwerowo / widocznie dla Ucznia) | | **≈ 11.5 s / ≈ 12.5 s** |

**Faza O — ocena** (`t_O`, od `zatwierdzono_at` do Oceny widocznej;
termin miękki = `max(10 s, 28 s − t_R)`):

| Etap | Limit | Cel p95 |
|---|---|---|
| Zapis zatwierdzenia + oczekiwanie w puli `OCENIANIE` | — | 0.6 s |
| Wczytanie danych | — | 0.1 s |
| SymPy (wszystkie pary + wynik) | 3 s łącznie, 2 s CPU na parę | 1.5 s |
| Model decyzyjny — Jev, tryb wsadowy (2 rundy = 2 żądania, A-01) | 2 s na żądanie | 1.6 s |
| Model decyzyjny — Jev, tryb pojedynczy (2 rundy = ≤ 3 fale po ≤ 16 wywołań, 10.4.3; p95 fali 1.2 s) — **wartość przyjęta w budżecie** | 2 s na wywołanie | 3.6 s |
| Oczekiwanie na przydział globalnego ogranicznika (13.2: przyjęcie planu, fale ≥ 2 z pierwszeństwem; wersja 2.3, C3-04) | plan przyjmowany tylko, gdy przewidywany koniec ≤ termin miękki; termin fali k: `termin_miekki − t_fali × (F − k + 1) − 0,2 s` | 0 s poza szczytem przy 2 workerach (fala 16 mieści się w kubełku 16); przy skalowaniu do 3 / 4 workerów poza szczytem ≤ 0,48 s / ≤ 0,84 s (`B_całk = 34` dzielone między repliki, 13.2, C4-05); w szczycie A-03 łącznie ≤ 3,7 s na Ocenę (symulacja S-6) |
| Sprawdzenie zgodności poprawek (tylko Transkrypcje z Poprawkami; startuje przy autozapisie `PUT …/transkrypcja`, pula `S`, równolegle z SymPy i Jev) | ≤ 6 s oczekiwania po zatwierdzeniu, niezależnie od `t_R` (wersja 2.4, C4-06 p. 10) | 0 s na ścieżce krytycznej: p95 Sprawdzenia 4 s (A-08) < SymPy + Jev ≥ 3,1 s + odstęp od ostatniego `PUT`; brak rezultatu ≤ 0,5% Rozwiązań z Poprawkami (9.4, rachunek log-normalny) |
| Model decyzyjny — zapasowy (1 wywołanie) | 9 s | 6.5 s |
| Reguły, zapis wyniku | — | 0.2 s |
| Odpytywanie klienta | interwał 1 s | +1 s |
| **Razem `t_O` serwerowo (Jev pojedynczy / Jev wsadowy / zapasowy)** | | **≈ 6.0 s / ≈ 4.0 s / ≈ 8.9 s** |
| **Razem `t_O` widocznie (Jev pojedynczy / Jev wsadowy / zapasowy)** | | **≈ 7.0 s / ≈ 5.0 s / ≈ 9.9 s** |
| Agent wyjaśniający (asynchronicznie, poza `t_O`) | 8 s | 5 s |

**Czas systemowy (serwerowy, `czas_systemowy_ms`) p95 ≈ 17.5 s (Jev, tryb
pojedynczy; 15.5 s w trybie wsadowym) / ≈ 20.4 s (model zapasowy); czas
widoczny dla Ucznia jest dłuższy o ≤ 2 s
(odpytywanie co 1 s w każdej fazie).** Suma p95 faz jest oszacowaniem
górnym. Dlatego SLO dla metryki serwerowej ma 2 s zapasu: ≥ 99% Rozwiązań z
`czas_systemowy_ms ≤ 28 000` oznacza czas widoczny ≤ 30 s.

**Najgorsze przypadki z przełączeniem w trakcie Oceny (C1-07, przeliczone
w wersji 2.2 dla C2-07)** — reguła: ponowienie nieudanych pytań Jev tylko,
gdy do terminu miękkiego zostało ≥ 13 s (ponowienie 2 s + model zapasowy
9 s + zapas 2 s); Jev w trybie pojedynczym (3 fale, najgorszy przypadek
A-01); model zapasowy z timeoutem `min(9 s, termin − teraz)`, a gdy
pozostało < 6 s — 9 s (13.2).

Czasy serwerowe (bez odpytywania klienta); termin miękki fazy O =
`max(10 s, 28 s − t_R)`.

| # | Scenariusz | Przebieg fazy O (s) | `t_O` | `t_R` | Czas systemowy | Ocena względem SLO (≤ 28 s) |
|---|---|---|---|---|---|---|
| S-1 | Obwód zamknięty, Jev: timeout od pierwszej fali (`t_R` = 11.5 s, termin miękki 16.5 s) | 0.6 + 0.1 + 1.5 + 2 (timeout; zostało 16.5 − 4.2 = 12.3 s < 13 s → bez ponowienia) + 9 (zapasowy, pełny timeout) + 0.2 | 13.4 s | 11.5 s | 24.9 s | spełnione |
| S-1b | Jak S-1, ale szybkie rozpoznanie (`t_R` = 9 s, termin miękki 19 s) | 0.6 + 0.1 + 1.5 + 2 (timeout; zostało 14.8 s ≥ 13 s) + 2 (ponowienie, timeout) + 9 + 0.2 | 15.4 s | 9 s | 24.4 s | spełnione |
| S-2 | Jev (tryb pojedynczy): runda 1 OK w 2 falach, timeout w rundzie 2 | 0.6 + 0.1 + 1.5 + 2.4 (2 fale) + 2 (timeout; zostało 9.9 s → bez ponowienia) + 9 + 0.2 | 15.8 s | 11.5 s | 27.3 s | spełnione |
| S-2b | Jev (tryb pojedynczy): timeout w 2. fali rundy 1 | 0.6 + 0.1 + 1.5 + 1.2 + 2 (timeout; zostało 11.1 s → bez ponowienia) + 9 + 0.2 | 14.6 s | 11.5 s | 26.1 s | spełnione |
| S-3 | Jak S-1, rozpoznanie trwające pełny timeout 14 s (`t_R` = 16.5 s, termin miękki 11.5 s) | po timeoutcie Jev zostało 7.3 s → model zapasowy z timeoutem 7.3 s; przebieg p95: 0.6 + 0.1 + 1.5 + 2 + 6.5 + 0.2 = 10.9 s; gdy model zapasowy przekroczy 7.3 s, praca jest ponawiana (0.5 s): Jev ponownie 2 s (timeout), model zapasowy 9 s (pozostało < 6 s) → t_O ≈ 25 s | 10.9 s (≈ 25 s) | 16.5 s | 27.4 s (≈ 41.5 s) | p95 spełnione; wariant z przekroczeniem — naruszenie, łączne prawdopodobieństwo < 0,05% (w granicy 1% SLO) |
| S-4 | Obwód otwarty (praca ciągła na modelu zapasowym) | 0.6 + 0.1 + 1.5 + 6.5 + 0.2 | 8.9 s | 11.5 s | 20.4 s | spełnione |
| S-5 | Oba modele decyzyjne niedostępne | ponowienia do 120 s → T-6 | — | — | — | naruszenie; Ocena później przez `PONOW_OCENE` |
| S-6 | „Koniec lekcji” (wersja 2.3, C3-04; popyt ujednolicony w wersji 2.4, C4-06 p. 9): 90 zatwierdzeń w 120 s, z czego 30 w pierwszych 20 s (1,5 Oceny/s × 28 wywołań planu budżetowego 16 + 11 + 1 = 42 wywołania/s), Jev w trybie pojedynczym, przydział workerów 2 × 18,18 = 36,36/s, kubełek 16, fale ≥ 2 z pierwszeństwem | Jev: 0.6 + 0.1 + 1.5 + ≤ 3,7 (oczekiwanie łącznie, maksimum z symulacji) + 3.6 + 0.2; `t_R` ≤ 16,5 s | ≤ 9.7 s | ≤ 16,5 s | ≤ 26,2 s | spełnione: 0/90 do modelu zapasowego, 0/90 naruszeń (symulacja `tests/obciazenie/symulacja_ogranicznika.py`, tabela 17.4) |
| S-6b | Jak S-6, ale fala zatwierdzeń zbiega się z nasyceniem puli R (`t_R` = 18,5 s, termin miękki 10 s) | plany z przewidywanym końcem > 10 s idą od razu do modelu zapasowego (0.6 + 0.1 + 1.5 + 6.5 + 0.2 = 8,9 s); pozostałe Jev | ≤ 9,7 s | 18,5 s | ≤ 28,2 s | 3/90 naruszeń (o ≤ 0,2 s) w takim szczycie; przy ≤ 1 zbiegu na dobę ≤ 3/1 500 = 0,2% dobowego wolumenu — pozycja w budżecie 1% SLO (S-7) |
| S-7 | Rozpoznanie: 1. próba kończy się timeoutem 14 s, 2. próba udana (p95 9 s) | faza R: 1.5 + 0.5 + 14 + 1 (odstęp) + 9 + 0.5 = 26.5 s; faza O z terminem miękkim `max(10, 28 − 26.5)` = 10 s: 0.6 + 0.1 + 1.5 + 3.6 + 0.2 | 6.0 s | 26.5 s | 32.5 s | **naruszenie** (liczone w SLO); dopuszczalne wyłącznie dzięki wymaganiu A-08 (≤ 0,3% wywołań rozpoznawania z błędem lub timeoutem). Budżet 1% SLO (wersja 2.3): S-7 ≤ 0,3% + S-3 < 0,05% + T-6 ≤ 0,2% + S-6b ≤ 0,2% + ogon rozkładu czasu normalnej pracy ≤ 0,25% |

### 17.3 Obserwowalność

- **Ślady**: OpenTelemetry; `trace_id` przenoszony z żądania HTTP do
  `praca_w_tle.ladunek._trace` i dalej do wywołań modeli.
- **Metryki** (Prometheus): `ocena_czas_systemowy_sekundy{model}`,
  `rozpoznawanie_czas_sekundy`, `ocena_faza_o_sekundy{model,trasa}`,
  `czas_od_przeslania_do_oceny_widocznej_sekundy`, `odsetek_t6`,
  `jev_bledy_total{rodzaj}`, `circuit_breaker_stan{model}`,
  `oceny_total{trasa,powod}`, `odsetek_automatycznych`,
  `praca_w_tle_oczekujace{pula,typ}`, `praca_w_tle_martwe_total{typ}`,
  `pula_zajete_sloty{pula}`, `weryfikacje_oczekujace{typ_kolejki}`,
  `weryfikacje_po_sla{typ_kolejki}`, `weryfikacja_czas_pracy_sekundy{powod}`,
  `weryfikacja_czas_do_rozstrzygniecia_godziny{powod}`,
  `wykonanie_kontroli_30d{model}`, `bledy_odczytu_niewykryte_30d`,
  `zgloszenia_bledu_odczytu_total{wynik}`, `zaleglosc_globalna_min`,
  `pojemnosc_globalna_min`, `zdjecia_zalegle`,
  `wskazowka_szablon_total{powod}`, `wskazowka_opoznienie_sekundy`,
  `generowanie_walidacja_total{rezultat,sprawdzenie}`,
  `koszt_modeli_pln_total{rola,systemowe}`, `brak_wkladki_total{umiejetnosc}`;
  od wersji 2.2: `odsetek_rozwiazan_z_poprawkami_30d`,
  `poprawki_niezgodne_total`, `sprawdzenie_poprawek_brak_total`,
  `blad_automatycznych_z_poprawkami_30d`, `jev_wywolania_http_total{tryb}`,
  `jev_ograniczenie_lokalne_total`, `jev_429_total`,
  `przydzial_ogranicznika{proces}`, `wynik_najstarszy_niezastosowany_sekundy`,
  `odsetek_eskalacji_7d{nauczyciel}`, `naplyw_eskalacji_min_7d`,
  `zaleglosc_globalna_min{zrodlo}`, `zgloszenia_zadan_otwarte`,
  `zgloszenia_zadan_po_sla`, `dane_osobowe_w_transkrypcji_total{zrodlo}`,
  `zdarzenia_rodo_total{typ}`, `zdarzenia_rodo_uniewaznione_total`,
  `normalizacja_oczekiwanie_sekundy`, `pula_diagnostyczna_braki`; od wersji
  2.3: `bezpiecznik_ocena{typ,model}` (n_eff, blad_ipw, LB, UB),
  `bezpiecznik_wyzwolenia_total{typ,model}`, `gorna_granica_bledu_30d{model}`,
  `jev_ograniczenie_lokalne_total{przyczyna}` (`PLAN_NIEPRZYJETY`,
  `TERMIN_FALI`), `ogranicznik_oczekiwanie_sekundy{fala}`,
  `czas_od_put_do_zatwierdzenia_sekundy`,
  `sprawdzenie_poprawek_gotowe_przed_zatwierdzeniem`,
  `sprawdzenie_poprawek_czas_sekundy`, `konflikt_filtra_total`,
  `dane_osobowe_w_transkrypcji_total{zrodlo,rodzaj}`,
  `obiekty_osierocone_usuniete_total{bucket,tryb}`,
  `usuniecie_wykonane_niedokonczone` (konta z obiektem `USUNIECIE_WYKONANE`
  starszym niż 2 h); od wersji 2.4: `prog_pewnosci_wartosc{model,rodzaj}`,
  `blokada_obnizania_progu{model}` (0/1, C4-02),
  `uczniowie_z_konfliktem_filtra_udzial` (odsetek Uczniów z ≥ 5
  Rozwiązaniami obliczeniowymi w 30 dniach, u których ≥ 50% Transkrypcji ma
  `konflikt_filtra`, C4-03), `ogranicznik_suma_przydzialow{konfiguracja}`,
  `ogranicznik_suma_pojemnosci{konfiguracja}`,
  `ogranicznik_wiersze_ponad_limit`, `ponowienie_rodo_zastosowane_total{typ,rola}`,
  `ponowienie_rodo_brak_konta_total` (C4-01, C4-05).
- **Alerty**: p95 `ocena_czas_systemowy_sekundy` > 20 s (10 min), odsetek
  > 28 s powyżej 1% (1 h), `odsetek_t6 > 0,2%` (24 h), obwód Jev otwarty
  > 15 min, `praca_w_tle_martwe_total` przyrost > 0 dla `OCEN_ROZWIAZANIE`,
  `zdjecia_zalegle > 0`, `ALERT_KALIBRACJI`, `ALERT_WERYFIKACJE`
  (Weryfikacje po SLA, pusta pula globalna, kontrola przyjęć aktywna),
  `wykonanie_kontroli_30d < 0,90`, `bledy_odczytu_niewykryte_30d > 2%`,
  koszt generowania systemowego ≥ 80% budżetu dziennego,
  `wynik_najstarszy_niezastosowany_sekundy > 300`, `odsetek_eskalacji_7d >
  0,10` (globalnie), `jev_ograniczenie_lokalne_total` > 5% Ocen w 15 min,
  `zdarzenia_rodo_uniewaznione_total` przyrost > 0 (`ALERT_RODO`; od wersji
  2.3 dotyczy wyłącznie zdarzeń `zrodlo = API`, więc wykonane usunięcia go
  nie wyzwalają, C3-03), `usuniecie_wykonane_niedokonczone > 0` przez 26 h
  (`ALERT_RODO`), `obiekty_osierocone_usuniete_total{tryb="CODZIENNY"}` > 100
  na dobę, `pula_diagnostyczna_braki > 0`, `bezpiecznik_wyzwolenia_total`
  przyrost > 0 (`ALERT_KALIBRACJI`), odsetek braku rezultatu Sprawdzenia
  zgodności poprawek > 1% w 24 h, `konflikt_filtra_total` > 1% Transkrypcji
  w 7 dniach; od wersji 2.4: `uczniowie_z_konfliktem_filtra_udzial > 1%`
  (`ALERT_RODO`, przegląd słownika i reguły b1), `prog_pewnosci_wartosc =
  1.000` ustawione przez bezpiecznik (alert krytyczny `ALERT_KALIBRACJI`),
  `ogranicznik_suma_przydzialow > 0,8 × limit` lub
  `ogranicznik_suma_pojemnosci > B_całk` (naruszenie niezmiennika 13.2),
  `ogranicznik_wiersze_ponad_limit > 0`.
- **Logi**: JSON, pola `trace_id`, `uzytkownik_id` (UUID, nie e-mail),
  `rozwiazanie_id`; maskowanie pól `haslo`, `tresc_md`, `latex`, `tekst`.

### 17.4 Pojemność (A-03)

| Zasób | Szacunek | Zapas |
|---|---|---|
| Żądania Jev — szczyt wieczorny | tryb pojedynczy: 300 Ocen/h × 28 (plan budżetowy 16 + 11 + 1, C4-06 p. 9) = 8 400/h ≈ 2.3/s; tryb wsadowy: 600/h ≈ 0.2/s | globalny ogranicznik: 0,8 × 50 = 40/s (ADR-039) |
| Żądania Jev — „koniec lekcji” | tryb pojedynczy: 90 Ocen w ~120 s × 28 = 21/s, chwilowo do 42/s; tryb wsadowy: ≤ 3/s | jedna formuła `r_i = min(0,8 × limit × waga_i / Σ wag, 0,8 × limit − Σ_{j≠i} r_j)` (13.2, ADR-039): worker 18,18/s × 2, `api` 1,82/s × 2 (wagi 1,0 / 0,1); łączna pojemność kubełków `B_całk = 34` (worker 16 przy 2 replikach, 10 przy 3, 8 przy 4); nadmiar ponad 36,36/s czeka w kolejce procesu z pierwszeństwem fal ≥ 2; plan nieprzyjęty → model zapasowy bez otwierania obwodu (E-33) |
| Piaskownica normalizacji w `api` | szczyt: 90 przesłań × ≤ 3 zdjęcia w 60 s ≈ 4.5 zdjęcia/s × 80 ms ≈ 0.36 s CPU/s | 4 procesy (2 repliki × 2) — wykorzystanie ≈ 9%; HEIC bez kompresji klienta (400 ms) — ≈ 45% |
| Pula `ROZPOZNAWANIE` | 30 korutyn; 90 przesłań/60 s przy średnim czasie 7 s → średnio 10.5 zajętych | 2.9× w szczycie lekcji; skalowanie: 3. replika workera — nie zwiększa ruchu do Jev ani do dostawcy rozpoznawania ponad 0,8 × limit (przydział i `B_całk` dzielone między repliki, granica chwilowa Jev ≤ 72 w 1 s, 13.2, C4-05) |
| Pula `OCENIANIE` | 30 korutyn; ≈ 4 s na Ocenę (Jev) → ≈ 7.5 Ocen/s | ≥ 30 jednoczesnych ocen (A-03) |
| Dostawca rozpoznawania | szczyt lekcji: 90 rozpoznań/min + Sprawdzenie zgodności poprawek 90 × 0,8 (obliczeniowe) × 0,25 (Poprawki, górna granica D-10) × 1,5 wywołania na Rozwiązanie (autozapis) = 27/min → 117/min | limit A-08 ≥ 150/min; przydział procesu 0,8 × 150 = 120/min (wagi R 1,0, S 0,3) |
| Pula `SPRAWDZANIE` | 8 korutyn; 27 Sprawdzeń/min × p95 4 s → średnio 1,8 zajętej | 4,4× w szczycie; Sprawdzenie nie czeka za rozpoznaniami (C3-05) |
| Zdjęcia | 300 × 3 × 1.2 MB ≈ 1.1 GB/h szczytowo; ≈ 5 GB/dobę; przy retencji ≈ 30–90 dni realnie ~200 GB (oryginał + maskowane) | magazyn obiektów |
| `decyzja_modelu` | ≤ 43 500 wierszy/dobę (1 500 × 29 — maksimum pytań planu, 10.4.3; liczba wierszy = liczba pytań, a nie wywołań) → ≤ 16 mln/rok | partycje miesięczne |
| PostgreSQL | 2 vCPU / 8 GB RAM / 200 GB SSD wystarcza do 20× A-03 | skalowanie pionowe |

**Symulacja globalnego ogranicznika (C3-04)** — 2 workery, tryb pojedynczy,
plan 16 + 11 + 1 wywołań, fala 1,2 s (p95), SymPy i wczytanie 2,2 s przed
falą 1; reguły 13.2 (przyjęcie planu, pierwszeństwo fal ≥ 2); zapasowy
p95 6,5 s. Profile zatwierdzeń: A — 90 w 120 s równomiernie (≈ 22
wywołania/s); B — 30 w 20 s, potem 60 w 100 s (szczyt 43,5/s, profil S-6);
C — 45 w 10 s, potem 45 w 110 s; D — 90 w 10 s (≈ 4× A-03).

| Profil | `t_R` | Przydział ważony, kubełek 16 (2.3): do zapasowego / maks. oczekiwanie / maks. `t_O` / naruszenia | Kubełek 4 (ścisłe okno 1 s): naruszenia | Podział po równo `r = 10/s` (formuła 7.3/13.2 z wersji 2.2): do zapasowego / naruszenia |
|---|---|---|---|---|
| A | 11,5 s | 0 / 0,0 s / 6,0 s / 0 | 0 (oczekiwanie 1,1 s) | 0 / 0 (oczekiwanie do 7,9 s) |
| B | 11,5 s | 0 / 3,7 s / 9,7 s / 0 | 0 | 16 / 6 |
| B | 16,5 s | 0 / 3,7 s / 9,7 s / 0 | 0 | — |
| B | 18,5 s | 0 / 3,7 s / 9,7 s / 3 | 4 | 23 / 14 |
| C | 11,5 s | 21 / 10,4 s / 20,6 s / 3 | 0 | 36 / 4 |
| D | 11,5 s | 64 / 10,4 s / 20,6 s / 2 | 2 | 78 / 2 |

**Ujednolicenie popytu (wersja 2.4, C4-06 p. 9).** Budżet, S-6, 17.4 i
symulacja używają jednej wartości: **28 wywołań na Ocenę** — plan budżetowy
„1 wariant, 12 kryteriów, 12 par” (10.4.3) w 3 falach 16 + 11 + 1, najgorszy
przypadek liczby fal. Plan wielowariantowy ma 29 wywołań (16 + 13), ale 2
fale; w profilu B, gdyby wszystkie plany miały 29 wywołań, nadwyżka popytu
nad przydziałem w 20-sekundowym szczycie rośnie z 42 − 36,36 = 5,64/s do
43,5 − 36,36 = 7,14/s (×1,27), więc oszacowanie maksymalnego oczekiwania
skalowane tym współczynnikiem wynosi 3,7 × 1,27 = 4,7 s, a `t_O` = 0,6 + 0,1
+ 1,5 + 4,7 + 2,4 (2 fale) + 0,2 = 9,5 s ≤ 9,7 s — S-6 pozostaje spełnione.
Liczba 29 występuje wyłącznie jako maksimum pytań (rozmiar `decyzja_modelu`,
ADR-039).

Wnioski: przy profilu A-03 (A, B) i `t_R` ≤ 16,5 s żadna Ocena nie trafia
do modelu zapasowego i nie narusza NFR-01; podział po równo z wersji 2.2
kierowałby w profilu B 18% Ocen do modelu zapasowego i dawał 7% naruszeń
w szczycie. Profile C i D przekraczają A-03 (90 przesłań w 60 s) — nadmiar
degraduje do modelu zapasowego z ≤ 3,3% naruszeń szczytu. G-3 (b) mierzy
profil B na środowisku testowym; rozbieżność z symulacją > 1 pp naruszeń
blokuje start (17.6). G-3 (c) powtarza profil B przy 3 replikach workera
(C4-05): 0 odpowiedzi `429`, maksimum wywołań w oknie 1 s ≤ 72 (pomiar po
stronie zaślepki Jev).

### 17.5 Wdrożenie

```mermaid
flowchart LR
    subgraph VM1["VM-1 (UE, 4 vCPU, 8 GB)"]
        C1["Caddy"]
        API1["api #1"]
        W1["worker #1"]
        S1["scheduler (aktywny)"]
    end
    subgraph VM2["VM-2 (UE, 4 vCPU, 8 GB)"]
        C2["Caddy"]
        API2["api #2"]
        W2["worker #2"]
        S2["scheduler (rezerwa)"]
    end
    LB["Load balancer dostawcy (TLS passthrough)"] --> C1 & C2
    C1 --> API1
    C2 --> API2
    PGM[("Zarządzany PostgreSQL 16<br/>HA, PITR 14 dni")]
    S3[("Magazyn obiektów UE<br/>bez wersjonowania")]
    REJ[("Bucket rejestr-zdarzen-rodo<br/>Object Lock 60 dni")]
    API1 & API2 & W1 & W2 & S1 & S2 --> PGM
    API1 & API2 & W1 & W2 --> S3
    W1 & W2 --> REJ
    MON["Prometheus + Grafana + Loki"] -.-> VM1 & VM2
```

- Jeden obraz kontenera `lemat:<git-sha>`, procesy uruchamiane komendą
  (`lemat api`, `lemat worker`, `lemat scheduler`).
- Migracje Alembic uruchamiane jako krok wdrożenia przed nowymi procesami;
  migracje muszą być kompatybilne wstecz o jedną wersję (rozszerz → migruj →
  zawęź).
- Wdrożenie kroczące: VM-2, kontrola zdrowia (`/api/v1/zdrowie` sprawdza
  bazę i magazyn), VM-1. Worker kończy bieżące prace (SIGTERM, 30 s).
- Środowiska: `lokalne` (Docker Compose + MinIO + atrapy modeli),
  `testowe` (prawdziwe modele, dane syntetyczne), `produkcyjne`.

### 17.6 Bramki jakości przed startem produkcyjnym

| Bramka | Warunek |
|---|---|
| G-0 | **Akceptacja właściciela produktu i właścicieli słownika:** PZS-1 (3.4); interpretacja NFR-01 (A-11); założenie A-12; rozszerzenia względem wymagań: powody Weryfikacji `PODEJRZENIE_BLEDU_ODCZYTU`, `ZGLOSZENIE_BLEDU_ODCZYTU`, `KONTROLA_LOSOWA`, `BLAD_PRZETWARZANIA`; Ocena wstępna wstrzymana przy podejrzeniu błędu odczytu (ADR-022); kolejka globalna z pseudonimizacją i kontrolą przyjęć (ADR-021, ADR-031); brak wpisu ręcznego (ADR-024); tryb pełnej weryfikacji, jeśli G-1(b) nie jest spełnione; zatwierdzanie kont Nauczycieli przez Administratora; **(2.1)** rekrutacja i finansowanie Nauczycieli współpracujących albo start bez rejestracji Uczniów bez Klasy (A-14, ADR-034); zawieszenie Wyniku zadania na czas Zgłoszenia błędu odczytu (11.6); rozstrzyganie na korzyść Ucznia po usunięciu zdjęć (11.4.3 p. 3); kopie treningowe za zgodą (ADR-035); **(2.2)** limity Poprawek Transkrypcji i powód `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM` (ADR-036, D-10); Przegląd Zadań wygenerowanych przez zlecającego Nauczyciela i Zgłoszenia błędu w Zadaniu zamiast bezpośredniej poprawki Zadań `GLOBALNY` (ADR-028, ADR-038, D-11); kryterium G-1(a) dla dowodów LB ≥ 85% przy n ≥ 350 (11.5.1, D-12); SLA w dniach roboczych, pula 18 weryfikatorów globalnych i kontrola przyjęć per Klasa (A-15, ADR-043, D-13); pominięcie Testu diagnostycznego (D-14); **(2.3)** Klasy przekazane w karencji nie wracają do przywróconego Nauczyciela (D-15); **(2.4)** automatyczne przejście pary w tryb pełnej weryfikacji przez bezpiecznik trafności przy progu 0,990 i koszt na granicy NFR-03 (D-16); zapas puli globalnej przy aktywnym bezpieczniku poprawek w fazie startowej (D-17). Akceptacja zapisana jako podpisany wpis w `docs/` (decyzja produktowa z datą). |
| G-1 | ZT-1 (A-09: ≥ 700 obliczeniowych, ≥ 350 z dowodem), Transkrypcje referencyjne: (a) Zgodność Oceny (ADR-027) ≥ 90% — dla Jev **i** modelu zapasowego, osobno dla każdego rodzaju zadania otwartego (NFR-02) — oraz dolna granica Wilsona 95% ≥ 87,5% dla `OTWARTE_OBLICZENIOWE` i ≥ 85,0% dla `OTWARTE_Z_DOWODEM` (analiza mocy 11.5.1, tabela 2; D-12); (b) próg z 11.5.3 (wartości out-of-fold) z UB błędu auto ≤ 2% i `n_eff ≥ 189` — albo tryb pełnej weryfikacji zaakceptowany w G-0; (c) ECE ≤ 0.05 |
| G-2 | Agent rozpoznający na zdjęciach ZT-1: ≥ 85% kroków z Transkrypcją identyczną z referencyjną po normalizacji LaTeX, CER ≤ 5%; czułość T-3 (`czulosc_t3`, 11.8) ≥ 90%; **end-to-end** (zdjęcie → Transkrypcja bez poprawek Ucznia → Ocena, praca `EWALUUJ_ZT1` w trybie `E2E`) Zgodność Oceny ≥ 85% dla każdego modelu decyzyjnego (dolne oszacowanie produkcji, w której Uczeń poprawia Transkrypcję); **(2.2)** Sprawdzenie zgodności poprawek (10.3.3): wykrywalność Poprawek „zastępujących” ≥ 95% na 200 syntetycznych przypadkach, **(2.3)** wykrywalność „wstawień rozwiązania” (T-7 albo `422`) ≥ 95% na 200 syntetycznych przypadkach i odsetek fałszywych alarmów ≤ 5% na 200 rzeczywistych Poprawkach błędów odczytu (wersja 2.2: ≤ 10%), p95 czasu Sprawdzenia ≤ 4 s; Filtr danych osobowych (2.3, C3-02): 0 wystąpień imienia i nazwiska z konta w zapisanych Transkrypcjach ≥ 150 kartek ZT-1 z danymi (w tym ≥ 30 z imionami lub nazwiskami < 4 znaków; górna granica Wilsona 95% odsetka kartek z wyciekiem 2,5%), ≥ 98% obszarów danych osobowych zamaskowanych, **0 zastąpień tokenów ze Słownika terminów matematycznych** i ≤ 0,5% kroków z fałszywym zastąpieniem innych tokenów na wszystkich 1 050 Transkrypcjach referencyjnych ZT-1 przefiltrowanych z syntetycznymi danymi konta (200 par imię–nazwisko z listy najczęstszych w Polsce, w tym nazwiska Pole, Kula, Granica, Suma, Prosta, Koło), `konflikt_filtra` ≤ 1% Transkrypcji, 0 zmian w `latex`; **(2.4, C4-03)** osobne kryterium dla ≥ 30 kartek z nazwiskiem równym terminowi matematycznemu (A-09): 0 wystąpień nazwiska w zapisanych Transkrypcjach, gdy stoi obok imienia lub inicjału albo w kroku w ramce danych osobowych (reguły a, b1, b2), i ≤ 1 kartka z `konflikt_filtra` wywołanym linią podpisu (≤ 3,3%); na 1 050 Transkrypcjach z syntetycznymi danymi konta `uczniowie_z_konfliktem_filtra_udzial` ≤ 1% (200 syntetycznych kont po ≥ 5 Transkrypcji); test Poprawek: przywrócenie kroku `tylko_dane_osobowe` na 50 przypadkach nie jest ponownie maskowane i trafia do Sprawdzenia zgodności poprawek (100%) |
| G-3 | Test obciążeniowy: (a) 2× szczyt wieczorny A-03 (600 Rozwiązań/h) przez 30 min; (b) „koniec lekcji”: 90 przesłań w 60 s, następnie 90 zatwierdzeń w 120 s; kryterium w obu: p95 czasu systemowego ≤ 20 s, ≥ 99% ≤ 28 s (czas widoczny ≤ 30 s), 0 przypadków T-6; **(2.4, C4-05)** (c) scenariusz (b) przy 3 replikach workera i dołączeniu 4. repliki w trakcie szczytu: 0 odpowiedzi `429`, maksimum wywołań Jev w dowolnym oknie 1 s ≤ 74 (pomiar po stronie zaślepki), Σ opublikowanych przydziałów ≤ 40/s w każdej próbce co 1 s |
| G-4 | Test chaosu NFR-07 przy 300 Rozwiązaniach/h, trzy scenariusze: (a) Jev zaczyna zwracać timeout na 100% wywołań przy obwodzie ZAMKNIĘTYM (pomiar 10 min od wstrzyknięcia awarii, obejmuje przejście obwodu); (b) Jev zwraca natychmiast 503; (c) Jev degraduje do p95 2.5 s; (d) Jev zwraca `429` dla 30% żądań przez 10 min — obwód nie może się otworzyć, a przydział ogranicznika spada i wraca (ADR-039); kryterium w każdym: ≥ 99% Ocen z czasem systemowym ≤ 28 s (czas widoczny ≤ 30 s) i 0 przypadków T-6. Przed G-4 ustalany jest `parametry.tryb_jev` (test wsadu 32 pytań; brak wsadu → `POJEDYNCZY` i G-3/G-4 w tym trybie) oraz **(2.3)** tolerancja limitu (A-01): seria 74 wywołań w 1 s po 10 s bezczynności bez `429` → łączna pojemność kubełków `B_całk = 34` (worker 16 przy 2 replikach), inaczej `B_całk = 10` (13.2, C4-05) |
| G-5 | Wszystkie Umiejętności **obu poziomów matury** mają opublikowaną Wkładkę teoretyczną i ≥ 3 Zadania `GLOBALNY` na każdy z Poziomów trudności 2–4 (FR-06, FR-07); **(2.2)** pula Testu diagnostycznego: ≥ 3 Zadania `diagnostyczne = true` (`ZAMKNIETE`, `GLOBALNY`, `OPUBLIKOWANE`) na każdą trójkę (Dział poziomu matury, poziom matury, nominalny Poziom trudności ∈ {2, 4}) — 12 × 2 × 3 = 72 dla `PODSTAWOWY` i 13 × 2 × 3 = 78 dla `ROZSZERZONY` (raport `GET /admin/pula-diagnostyczna`, FR-03, C2-13) |
| G-6 | Przegląd RODO: umowy powierzenia z dostawcami z retencją zgodną z A-07, polityka prywatności (w tym kolejka globalna i pseudonimizacja), umowy poufności weryfikatorów globalnych, rejestr czynności przetwarzania, test procedury `ponow_zdarzenia_rodo` (odtworzenie PITR w środowisku testowym z rejestrem w buckecie Object Lock) w czterech scenariuszach: (a) wykonane usunięcie po punkcie odtworzenia — **(2.3, C3-03)** w dwóch wariantach: odtworzenie 3 min i 60 min po usunięciu (dłużej niż okno unieważniania 10 min); **(2.4, C4-01)** (a3) punkt odtworzenia `T` **w trakcie** `USUN_KONTO` Ucznia — po kroku 1, 2, 3 i 4, a przed krokiem 5 (cztery przebiegi; zatrzymanie przez zmienną testową `LEMAT_TEST_ZATRZYMAJ_USUN_KONTO_PO_KROKU ∈ {1, 2, 3, 4}`, dostępną wyłącznie w środowisku testowym): odtworzona baza ma wiersz `zdarzenie_rodo` i pełne dane konta, a po skrypcie brak wiersza `uzytkownik`, 0 wierszy `decyzja_modelu` z `uczen_id` konta, 0 obiektów z prefiksem konta w `zdjecia/`, `zdjecia-maskowane/`, `zbior-treningowy/`; (a4) konto **Nauczyciela**: wniosek po `T` (po skrypcie Weryfikacje `KLASA` eskalowane z `KARENCJA_KONTA_NAUCZYCIELA`, blokady zwolnione, szkice zachowane, `KLASA_DO_PRZEKAZANIA` wysłane), usunięcie po `T` i w trakcie kroków (2)–(4) (Klasy zarchiwizowane, Zadania `PRYWATNY` `WYCOFANE`, 0 wystąpień imienia i nazwiska w `rekomendacja.uzasadnienie` i `uzasadnienie_fakty`), anulowanie po `T` (tabela przywrócenia Nauczyciela); we wszystkich wariantach konto jest w stanie docelowym przed startem workerów, a `zdarzenia_rodo_uniewaznione_total` się nie zmienia, (b) **wniosek** o usunięcie po punkcie odtworzenia (konto wraca do `USUWANE` z pierwotnym terminem), (c) **odwołanie zgody** po punkcie odtworzenia (zgoda `false`, brak nowych kopii, istniejące kopie usunięte), (d) **(2.3, C3-06)** kopia treningowa i zdjęcia utworzone po punkcie odtworzenia — po kroku 2a w `zbior-treningowy/`, `zdjecia/` i `zdjecia-maskowane/` nie ma obiektów bez wiersza w bazie oraz test przywrócenia konta w karencji (C-33), usunięcia konta Nauczyciela z Przekazaniem Klasy i bez niego (ADR-044) oraz **(2.3)** przywrócenia konta Nauczyciela w karencji (Weryfikacje z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA` wracają do `KLASA`, szkice i flagi zachowane) i rozstrzygnięcia dla Ucznia w karencji (`409 KONTO_UCZNIA_NIEAKTYWNE`, C-37) |
| G-7 | Pula weryfikatorów globalnych: liczba aktywnych weryfikatorów globalnych ≥ `wymagani_weryfikatorzy_globalni` z modelu tygodniowego 17.7 (na start: **18** = 10 Nauczycieli bez Klas + ≥ 8 współpracujących, A-14) i 5 × Σ `deklarowane_minuty_weryfikacji` ≥ 1,3 × tygodniowa prognoza kolejki `GLOBALNA` **łącznie z eskalacjami** (A-15); test eskalacji, alertu pustej puli i kontroli przyjęć; **(2.4, C4-06 p. 7)** wariant z aktywnym bezpiecznikiem poprawek w fazie startowej wymaga 20 weryfikatorów — przy opcji domyślnej D-17 (18) współczynnik zapasu w tym wariancie wynosi 1,22 i nadwyżkę zatrzymuje kontrola przyjęć kolejki globalnej (17.7). Gdy G-7 nie jest spełnione, a pozostałe bramki tak: start produkcyjny z `LEMAT_REJESTRACJA_BEZ_KLASY = false` (A-14) — dopuszczalny wyłącznie za akceptacją w G-0 |

ZT-1 jest przechowywany w buckecie `zbiory-testowe/zt1/` (manifest JSON:
Zadanie, zdjęcia, Transkrypcja referencyjna, Oceny 2–3 Nauczycieli). Praca
`EWALUUJ_ZT1` ma dwa tryby: `REFERENCYJNY` (Agent oceniający na
Transkrypcjach referencyjnych — izoluje jakość oceniania, G-1) i `E2E`
(zdjęcie → Agent rozpoznający → Agent oceniający bez poprawek — G-2).

### 17.7 Model obciążenia Nauczycieli (FR-33, R-02, C1-02, C1-10, C2-12)

Założenia: A-03 (1 500 Rozwiązań zadań otwartych/dobę przez 7 dni w
tygodniu, 20% z dowodem), A-12 (20% Uczniów bez Klasy), A-13 (czasy
Weryfikacji), A-15 (praca Nauczycieli 5 dni roboczych w tygodniu, 5%
Weryfikacji `KLASA` eskalowanych), zgłoszenia błędu odczytu 1% Rozwiązań,
T-6 ≤ 0,2%, powód T-7 dla 3% Rozwiązań obliczeniowych (wliczone w kolumnę
obliczeniowych). **Wyprowadzenie T-7 (wersja 2.3, C3-05)** z rozkładu
zamiast założenia: T-7 = p_P × (p_miekki + p_FA + p_brak + p_nz), gdzie
p_P = 0,20 — odsetek Rozwiązań obliczeniowych z Poprawkami (środek D-10,
`odsetek_rozwiazan_z_poprawkami_30d`), p_miekki = 0,04 — przekroczenie
limitu miękkiego (do pomiaru), p_FA ≤ 0,05 — fałszywe alarmy Sprawdzenia
(kryterium G-2), p_brak ≤ 0,005 — brak rezultatu przed zapisem Oceny
(rachunek log-normalny 9.4, osobna pula `S`), p_nz = 0,03 — rzeczywiste
Poprawki niezgodne ze zdjęciem (do pomiaru). T-7 = 0,20 × 0,125 = 2,5%; przy
górnej granicy D-10 (p_P = 0,25) — 3,1%. Model przyjmuje 3%; wartość
zmierzona zastępuje oszacowanie w cotygodniowym przeliczeniu (punkt 4
wniosków).

**Napływ dobowy** (średnia z 7 dni kalendarzowych):

| Faza | Weryfikacje dowodów | Weryfikacje obliczeniowych (w tym T-7) | Kontrole losowe | Zgłoszenia + T-6 | Razem / dobę | Minuty / dobę `M` |
|---|---|---|---|---|---|---|
| Start (po G-1; auto 47% obliczeniowych, Kontrola 20%, × 2 dla Poprawek) | 300 × 5 min | 636 × 3 min | 120 × 2 min | 18 × 3 min | 1 074 | 3 702 |
| Ustalona (auto 67%, Kontrola 5%) | 300 × 5 min | 396 × 3 min | 42 × 2 min | 18 × 3 min | 756 | 2 826 |
| Tryb pełnej weryfikacji (auto 0%, `udzial_zadan_otwartych = 5`, wolumen × 3/5) | 180 × 5 min | 720 × 3 min | 0 | 11 × 3 min | 911 | 3 093 |

**Podział i rytm tygodniowy.** Kolejka `KLASA` = 80% `M`, kolejka
`GLOBALNA` = 20% `M` (Uczniowie bez Klasy) + 5% kolejki `KLASA`
(eskalacje). Nauczyciele pracują 5 dni, więc tygodniowy napływ `7 × M`
jest obsługiwany w 5 dniach roboczych: **obciążenie dnia roboczego = 7/5 ×
obciążenie dobowe** (Rozwiązania z piątku i weekendu mają termin SLA w
dniach roboczych, więc nie eskalują masowo w poniedziałek, A-15).

| Faza | `KLASA` / dobę (80%, po odjęciu 5% eskalacji) | Na Nauczyciela prowadzącego (40) — dzień roboczy | `GLOBALNA` / dobę (20% + eskalacje) | Wymagani weryfikatorzy globalni = ⌈1,3 × 7 × `GLOBALNA` / (5 × 90)⌉ |
|---|---|---|---|---|
| Start | 2 814 min | 2 814 × 7/5 / 40 = **98 min** (≈ 29 Weryfikacji na dzień roboczy, łącznie z Kontrolą losową) | 740 + 148 = 888 min | ⌈1,3 × 6 216 / 450⌉ = **18** |
| Ustalona | 2 148 min | **75 min** (≈ 20 Weryfikacji) | 565 + 113 = 678 min | ⌈1,3 × 4 746 / 450⌉ = **14** |
| Tryb pełnej weryfikacji | 2 351 min | **82 min** (≈ 24 Weryfikacje) | 619 + 124 = 742 min (po zaokrągleniu składników: 618,6 + 123,7) | ⌈1,3 × 5 194 / 450⌉ = ⌈15,01⌉ = **16** |
| Start bez Uczniów bez Klasy (`LEMAT_REJESTRACJA_BEZ_KLASY = false`, A-14) | 3 517 min | **123 min** | 0 + 185 = 185 min | ⌈1,3 × 1 295 / 450⌉ = **4** |

Współczynnik 1,3 (wcześniej 1,5) obejmuje zmienność dzienną; eskalacje są
teraz modelowane jawnie, więc nie są już ukryte w zapasie.

Skład puli weryfikatorów globalnych (A-14, ADR-034, ADR-043):

| Faza | Wymagani | Nauczyciele bez Klas (A-03) | Nauczyciele prowadzący Klasy (≤ 180 min na dzień roboczy łącznie) | Nauczyciele współpracujący (umowa) |
|---|---|---|---|---|
| Start | 18 | 10 × 90 min | 0 (prognoza Klasy 98 min + 90 min = 188 min — niedozwolone, `422 LIMIT_OBCIAZENIA_WERYFIKATORA`) | ≥ 8 × 90 min |
| Ustalona | 14 | 10 × 90 min | 0 (75 + 90 = 165 min — dozwolone, nieplanowane) | ≥ 4 × 90 min |
| Tryb pełnej weryfikacji | 16 | 10 × 90 min | 0 | ≥ 6 × 90 min |
| Start bez Uczniów bez Klasy | 4 | 4 z 10 × 90 min | 0 | 0 |

Wnioski: (1) Kontrola losowa stanowi 6% obciążenia na starcie i 3% w fazie
ustalonej — jest wliczona; (2) obciążenie startowe 98 min na dzień roboczy
na Nauczyciela prowadzącego Klasę jest wysokie (R-02) — mitygacje: Opinia
pomocnicza, szkice i przedłużanie blokad bez utraty ETag (ADR-037),
automatyczne obniżanie progu (11.5.3), parametr `udzial_zadan_otwartych`,
kontrola przyjęć per Klasa przy `odsetek_eskalacji_7d ≥ 0,20` (11.4.3);
(3) eskalacje z Klas zasilają kolejkę `GLOBALNA` i są wliczone w
`zaleglosc_globalna_min` oraz w G-7; (4) model jest przeliczany co tydzień z
wartości zmierzonych (`weryfikacja_czas_pracy_sekundy`, rzeczywisty udział
Uczniów bez Klasy, `odsetek_eskalacji_7d`) i publikowany w `GET
/admin/obciazenie-weryfikacji`; spadek `weryfikatorzy_globalni_aktywni_7d`
poniżej wymaganej liczby uruchamia `ALERT_WERYFIKACJE` (E-28); (5) model
zakłada najgorszy przypadek pracy wyłącznie w dni robocze — weryfikatorzy
współpracujący pracujący w weekendy zmniejszają liczbę wymaganych osób
(liczona z deklaracji).

**Scenariusz startowy a bezpiecznik trafności (C3-01).** Założenia: próg z
ZT-1 dla Jev 0,930 (przykład), rzeczywisty błąd ocen automatycznych 0,5%
(11.5.1), ≈ 110 zakończonych Kontroli losowych na dobę kalendarzową
(120 utworzonych × wykonanie 0,9), pierwsze zakończone w dniu 2.

| Dzień | `n` w oknie (Kontrole powyżej progu) | błędy `k` | UB / LB Wilsona 95% | Reguła wersji 2.2 („n ≥ 50 i UB > 2%”) | Reguła wersji 2.3 (LB > 2% przy `n_eff ≥ 50`; ostrzeżenie: UB > 2% przy `n_eff ≥ 189`) |
|---|---|---|---|---|---|
| 2 | 110 | 0 | 3,37% / 0,00% | próg 0,930 → 0,950 | brak działania |
| 3 | 220 (2.2: przy oknie „rekordu progu” 110) | 1 | 2,53% / 0,08% | → 0,970 | ostrzeżenie: brak obniżania progu |
| 4 | 330 | 2 | 2,18% / 0,17% | → 0,990 (maksimum) | ostrzeżenie |
| 5 | 440 | 2 | 1,64% / 0,13% | zatrzymanie (okno pary) albo dalsze wyzwalanie przy n = 110 (okno rekordu — zapadka) | brak działania; KALIBRUJ może obniżać próg |
| 8 | 770 | 4 | 1,33% / 0,20% | próg 0,990 do ≈ 2 tygodni (obniżanie ≤ 0,02/dobę dopiero po ≥ 400 próbkach) | brak działania |

Skutek dla obciążenia w fazie „Start” (dwa pierwsze tygodnie):

| Wariant | Auto (obliczeniowe) | Weryfikacje obliczeniowych | Kontrole | `M` (min/dobę) | Na Nauczyciela prowadzącego (dzień roboczy) | Wymagani weryfikatorzy globalni |
|---|---|---|---|---|---|---|
| Reguła 2.3 (bez fałszywego wyzwolenia) | 47% | 636 × 3 min | 120 × 2 min | 3 702 | 98 min | 18 (G-7 bez zmian) |
| Reguła 2.2 (próg 0,990, auto ≈ 0%) | ≈ 0% | 1 200 × 3 min | 0 | 1 500 + 3 600 + 54 = 5 154 | 0,76 × 5 154 × 7/5 / 40 = 137 min | ⌈1,3 × 7 × (0,24 × 5 154) / 450⌉ = ⌈25,01⌉ = 26 |

Reguła z wersji 2.2 unieważniałaby więc G-7 (18 zamiast 26 weryfikatorów)
i limit 180 min (A-14) dokładnie w pierwszych dwóch tygodniach. Reguła 2.3
nie wyzwala się przy błędzie 0,5% z prawdopodobieństwem ≥ 99,8% w 90
dniach (symulacja 11.5.3), więc model „Start: auto 47%” i G-7 = 18
pozostają ważne. Prawdziwa degradacja do ≥ 4% podnosi próg w dniu 2 (błąd od początku
okna) albo — przy nagłej degradacji w fazie ustalonej — w medianie 13 dni
(11.5.3); kolejne wyzwolenia prowadzą przy degradacji jednorodnej do trybu
pełnej weryfikacji ustawianego automatycznie przez bezpiecznik (D-16), z
`udzial_zadan_otwartych = 5` (wiersz „Tryb pełnej weryfikacji”, 16
weryfikatorów).

**Wariant: aktywny bezpiecznik poprawek (wersja 2.4, C4-06 p. 7).** Aktywny
bezpiecznik kieruje do T-7 każdą Transkrypcję z Poprawkami, w tym te, które
inaczej byłyby Ocenami automatycznymi: Rozwiązania obliczeniowe z Poprawkami
ocenione automatycznie = p_P × auto × 1 200 = 0,20 × 0,47 × 1 200 ≈ 113 na
dobę na starcie (0,20 × 0,67 × 1 200 ≈ 161 w fazie ustalonej). Każde z nich
staje się Weryfikacją obliczeniową (3 min), a znikają Kontrole losowe, które
by na nie przypadły (podwójny odsetek: 0,40 na starcie, 0,10 w fazie
ustalonej; 2 min):

| Faza | Dodatkowe Weryfikacje (3 min) | Kontrole, które znikają (2 min) | Δ`M` (min/dobę) | `M` | Na Nauczyciela prowadzącego (dzień roboczy) | `GLOBALNA` / dobę | Wymagani weryfikatorzy globalni |
|---|---|---|---|---|---|---|---|
| Start | 113 | 45 | 339 − 90 = +249 (+6,7%) | 3 951 | 0,8 × 3 951 × 0,95 × 7/5 / 40 = **105 min** | 790 + 158 = 948 min | ⌈1,3 × 7 × 948 / 450⌉ = ⌈19,17⌉ = **20** |
| Ustalona | 161 | 16 | 483 − 32 = +451 (+16,0%) | 3 277 | 0,8 × 3 277 × 0,95 × 7/5 / 40 = **87 min** | 655 + 131 = 786 min | ⌈1,3 × 7 × 786 / 450⌉ = ⌈15,9⌉ = **16** |

Wariant mieści się w G-7 = 18 w fazie ustalonej; w fazie startowej wymaga
20 weryfikatorów. Opcja domyślna D-17: G-7 pozostaje 18, bo przy 18
weryfikatorach pojemność tygodniowa 18 × 90 × 5 = 8 100 min pokrywa
prognozę 7 × 948 = 6 636 min ze współczynnikiem 1,22 (zamiast 1,3), a
nadwyżkę dzienną zatrzymuje kontrola przyjęć kolejki globalnej (11.4.3);
cotygodniowe przeliczenie (punkt 4 wniosków) używa wariantu z bezpiecznikiem,
gdy `stan_bezpiecznika.aktywny` typu `POPRAWKI` jest prawdą.

**Wariant: koszt bezpiecznika trafności na granicy NFR-03 (C4-06 p. 5).**
Przy rzeczywistym błędzie e = 2% (fazie ustalonej) symulacja dynamiki
11.5.3 daje średnio 1,1 fałszywego podniesienia progu na kwartał, średni
udział Ocen automatycznych 57,5% zamiast 67% (Weryfikacje obliczeniowe +
0,095 × 1 200 ≈ 114 na dobę, `M` ≈ 2 826 + 114 × 3 − 114 × 0,05 × 2 ≈ 3 157
min, 84 min na Nauczyciela prowadzącego) i 11% kwartałów kończących się
trybem pełnej weryfikacji (wiersz „Tryb pełnej weryfikacji”, 16
weryfikatorów). Wartości mieszczą się w G-7 = 18.

**Konflikt Filtra danych osobowych per Uczeń (C4-03 p. 2).** Model zakłada
`konflikt_filtra` ≤ 1% Transkrypcji (kryterium G-2) rozłożone na Uczniów, a
nie skupione: Uczeń z nazwiskiem równym terminowi matematycznemu nie dostaje
od wersji 2.4 konfliktu z linii podpisu (10.3.1 p. 3a). Metryka
`uczniowie_z_konfliktem_filtra_udzial` > 1% (alert) oznacza systematyczną
Weryfikację wąskiej grupy Uczniów i uruchamia przegląd reguły b1 i
Słownika.

---

## 18. Mapowanie wymagań na komponenty, przepływy i API

### 18.1 Wymagania funkcjonalne

| Wymaganie | Komponenty | Przepływ / reguły | API | Dane |
|---|---|---|---|---|
| FR-01 konto, logowanie | `lemat.konta` | 9.1 | `/auth/*` | `uzytkownik`, `uczen`, `token_odswiezania` |
| FR-02 poziom matury, termin | `lemat.konta`, `lemat.profil` | 9.1 (w tym zmiana poziomu) | `PATCH /uczniowie/ja` | `uczen.poziom_matury`, `termin_egzaminu` |
| FR-03 test diagnostyczny | `lemat.profil`, `lemat.tresci` (pula), Orkiestrator (`AKTUALIZUJ_PROFIL`, `SPRAWDZ_PULE_DIAGNOSTYCZNA`) | 9.1 (pula, `TEST_NIEDOSTEPNY`, pominięcie D-14), 10.1.3, G-5 | `/uczniowie/ja/test-diagnostyczny`, `/testy-diagnostyczne/*`, `PUT /admin/zadania/{id}/diagnostyczne`, `GET /admin/pula-diagnostyczna` | `test_diagnostyczny` (`ux_test_jeden`, `pominiete_dzialy`), `odpowiedz`, `wynik_zadania.test_diagnostyczny_id`, `opanowanie_umiejetnosci` |
| FR-04 widok profilu | `lemat.profil` | 10.1.3 | `GET /uczniowie/{id}/profil` | `opanowanie_umiejetnosci` |
| FR-05 kolejne zadanie | Agent rekomendujący, `lemat.rekomendacje`, fasada Orkiestratora | 9.2, 10.1.2 kroki 1–8 | `GET …/rekomendacje/nastepna`, `POST /elementy-sciezki/{id}/rozpoczecie` | `element_sciezki`, `rekomendacja` |
| FR-06 wkładka przed zadaniami | Agent rekomendujący, Agent generujący (szkice), `lemat.tresci` | 10.1.2 krok 6, E-20, G-5 (oba poziomy) | `…/nastepna`, `/wkladki-teoretyczne/*`, `/elementy-sciezki/{id}/przeczytanie` | `wkladka_teoretyczna` |
| FR-07 rosnąca trudność | Agent rekomendujący (Elo, Seria) | 10.1.2 kroki 3 i 7, 10.1.3, reguły Serii | `…/nastepna` | `theta`, `kalibracja_trudnosci`, `sciezka_nauki.seria_*` |
| FR-08 powtórki | Agent rekomendujący | 10.1.2 kroki 4 i 5b, 10.1.3 | `…/nastepna` | `nastepna_powtorka_at`, `licznik_powtorek`, `element_sciezki.cel` |
| FR-09 wybór działu | `lemat.konta`, Agent rekomendujący | 10.1.2 krok 2 | `PATCH /uczniowie/ja` | `uczen.wybrany_dzial_id` |
| FR-10 uzasadnienie | Agent rekomendujący | 10.1.5 (szablon dla każdej reguły i braku elementu) | `RekomendacjaWyjscie` | `rekomendacja.uzasadnienie`, `uzasadnienie_fakty`, `regula` |
| FR-11 generowanie | Agent generujący | 9.7, 13.3 (`UZUPELNIJ_PULE` z budżetem) | `POST /zadania/generowanie` | `zlecenie_generowania`, `zadanie` |
| FR-12 treść + wzorcowe + kryteria | Agent generujący | 10.2.1 | `GET /zadania/{id}` | `zadanie_wersja`, `kryterium_oceniania` |
| FR-13 walidacja | Agent generujący, SymPy, model decyzyjny | 9.7, 10.2.2 | `GET /zlecenia-generowania/{id}` | `walidacja_zadania` |
| FR-14 przegląd przez Nauczyciela | `lemat.tresci` | 8.4 (uprawnienia ADR-028, ADR-038; Zadania zlecone przez Nauczyciela zawsze `DO_PRZEGLADU`), 9.7, D-11 | `POST /zadania/{id}/przeglad`, `POST /zadania/{id}/wycofanie`, `POST /zadania/{id}/zgloszenia-bledu`, `/zgloszenia-bledu-zadan/*` | `przeglad_zadania`, `zadanie_wersja`, `zadanie.zasieg`, `zgloszenie_bledu_zadania`, `wstrzymanie_w_doborze` |
| FR-15 zadanie zamknięte | `lemat.rozwiazania` | 9.3 | `POST /zadania/{id}/odpowiedzi` | `odpowiedz` |
| FR-16 zdjęcia rozwiązania | `lemat.rozwiazania`, PWA | 9.4, 8.5 (rozpoczęcie) | `POST /rozwiazania`, `…/zdjecia`, `…/porzucenie` | `rozwiazanie`, `zdjecie_rozwiazania` |
| FR-17 odczyt pisma, kroki | Agent rozpoznający | 9.4, 10.3 | `GET /rozwiazania/{id}`, `…/ponowienie-rozpoznania` | `transkrypcja`, `krok_transkrypcji` |
| FR-18 poprawa transkrypcji | `lemat.rozwiazania`, Agent rozpoznający (Sprawdzenie zgodności poprawek) | 9.4 (limity), 8.1, 10.3.3, 11.3 T-7, ADR-036, D-10 | `PUT …/transkrypcja`, `POST …/zatwierdzenie` | `transkrypcja` (wersje, `zakres_poprawek`, `zgodnosc_poprawek`), `krok_transkrypcji.nr_kroku_agenta`, `zmieniony_przez_ucznia` |
| FR-19 ocena krokowa, punkty cząstkowe | Agent oceniający | 9.5, 11.1 | `GET …/ocena` | `ocena`, `ocena_kryterium` |
| FR-20 wynik tylko z rozumowania | Agent oceniający | 11.1 p. 5 | — | `ocena_kryterium`, `ocena.punkty_surowe` |
| FR-21 wynikanie kroków | SymPy, Q-OC-05, N-3, N-6 | 10.4.3, 11.1 p. 8 | — | `decyzja_modelu` |
| FR-22 pewność oceny | Agent oceniający, `lemat.kalibracja` | 11.2, ADR-027 | pole `pewnosc_oceny` (N) | `ocena.pewnosc_oceny`, `kalibrator_pewnosci` |
| FR-23 auto powyżej progu | Agent oceniający | 11.3 T-4 | — | `ocena.status = OSTATECZNA`, `prog_pewnosci` |
| FR-24 poniżej progu → weryfikacja, ocena wstępna | Agent oceniający, `lemat.weryfikacja` | 9.5, 11.3, 8.2 (widoczność) | `GET /rozwiazania/{id}` (`ocena.status = WSTEPNA`) | `weryfikacja` |
| FR-25 poprawny wynik, błędne kroki | Agent oceniający | 11.3 T-2 (τ_FR25) | — | `powody` |
| FR-26 dowód zawsze do weryfikacji | Agent oceniający | 11.3 T-1, CHECK progu 1.000 | — | `powody`, `prog_pewnosci` |
| FR-27 błędny krok, typ, wskazówka | Agent oceniający, Agent wyjaśniający | 9.5, 9.6.2 (reguły Wskazówki), 10.5, 11.1 p. 6 (N-1), 11.7 | `OcenaWyjscie` | `pierwszy_bledny_krok`, `typ_bledu`, `wskazowka`, `uzasadnienie` |
| FR-28 błędy odczytu nie obniżają oceny | Agent rozpoznający, Agent oceniający, `lemat.weryfikacja` | 8.6, 9.4, 9.12 (zawieszenie Wyniku), 11.1 p. 5–7, 11.3 T-3, 11.4.3 p. 3, 11.6, 11.8, ADR-022 | `…/zgloszenie-bledu-odczytu`, `OcenaWyjscie.widocznosc_dla_ucznia`, `RozstrzygniecieWejscie.kroki_z_bledem_odczytu` | `mozliwy_blad_odczytu`, `krok_podejrzany_odczyt`, `weryfikacja.zgloszenie` |
| FR-29 ocena aktualizuje profil | `lemat.profil` | 9.3, 9.5, 9.6, 10.1.3 (wagi) | — | `wynik_zadania`, `opanowanie_umiejetnosci` |
| FR-30 klasy, zaproszenia | `lemat.klasy` | 16.4 (Przekazanie Klasy przy usunięciu konta Nauczyciela) | `/klasy`, `/klasy/dolaczenie`, `…/archiwizacja`, `DELETE /uczniowie/ja/klasy/{id}`, `POST /admin/klasy/{id}/przekazanie` | `klasa`, `czlonkostwo_klasy` |
| FR-31 postępy | `lemat.klasy`, `lemat.profil` | — | `/klasy/{id}/postepy`, `/uczniowie/{id}/profil`, `/przypisania/{id}/postep` | widok agregujący |
| FR-32 przypisania | `lemat.klasy`, Agent rekomendujący (krok 1) | 10.1.2, 8.5 (element `R-PRZYPISANIE`), ADR-032, C-30 | `/zestawy-zadan` (CRUD), `/przypisania`, `GET /przypisania/{id}/postep` (`PostepPrzypisaniaWyjscie`) | `zestaw_zadan`, `przypisanie`, `element_sciezki.przypisanie_id`, `trg_kontekst_proby` |
| FR-33 kolejka z powodem | `lemat.weryfikacja` | 9.6.1, 11.3, 11.4, 17.7 | `GET /weryfikacje`, `…/podjecie`, `…/przedluzenie`, `…/szkic` | `weryfikacja.powody`, `priorytet`, `termin_at` |
| FR-34 korekty → kalibracja | `lemat.weryfikacja`, `lemat.kalibracja` | 9.6.2, 9.6.3, 9.8, 11.5 | `…/rozstrzygniecie`, `…/korekty` | `probka_kalibracyjna`, `kalibrator_pewnosci`, `prog_pewnosci` |
| FR-35 import CKE | `lemat.tresci` | 9.11 | `/admin/arkusze-cke/importy` | `arkusz_cke`, `import_arkusza` |
| FR-36 tagowanie | `lemat.tresci` | 9.11 | `POST /zadania`, `GET /zadania?…` | `zadanie.rodzaj`, `dzial_id`, `zadanie_umiejetnosc`, `poziom_trudnosci_nominalny`, `kalibracja_trudnosci.poziom_trudnosci` |

### 18.2 Wymagania niefunkcjonalne

Pełne mapowanie w 17.1; budżety w 17.2; bramki weryfikujące w 17.6.

### 18.3 Rodzaje zadań

| Rodzaj | Przepływ | Ocena | Weryfikacja |
|---|---|---|---|
| Zamknięte | 9.3 | porównanie z kluczem (`odpowiedz.czy_poprawna`), bez zdjęcia | nigdy |
| Otwarte obliczeniowe | 9.4 → 9.5 | Agent oceniający krok po kroku | wg 11.3 (T-2…T-5) |
| Otwarte z dowodem | 9.4 → 9.5 | Ocena wstępna (bez Q-OC-01) | zawsze (T-1) |

---

## 19. Rejestr decyzji architektonicznych (ADR)

Statusy: **Przyjęta** (iteracja 1, bez zmian), **Zmieniona (it. 2)**,
**Nowa (it. 2)**, **Nowa (it. 3)**, **Nowa (it. 4)** (wersja 2.2), **Nowa (it. 5)**
(wersja 2.3: ADR-046, ADR-047); uzupełnienia wprowadzone w wersjach 2.1, 2.2
i 2.3 do istniejących decyzji są oznaczone akapitem „Uzupełnienie (it. 3)”,
„Uzupełnienie (it. 4)” albo „Uzupełnienie (it. 5)”. Decyzje
wymagające akceptacji właściciela produktu są oznaczone „G-0”.

### ADR-001 Modularny monolit z osobnymi procesami `api`, `worker`, `scheduler` — Przyjęta
- **Kontekst:** skala A-03, mały zespół, wiele ściśle powiązanych przepływów.
- **Decyzja:** jedno repozytorium i jeden obraz; granice modułów egzekwowane `import-linter`; procesy skalowane niezależnie.
- **Alternatywy:** mikroserwisy per agent — odrzucone (koszt operacyjny, transakcje rozproszone).
- **Konsekwencje:** prostsze wdrożenie i transakcje; ewentualny podział na serwisy wzdłuż modułów jest możliwy bez zmiany kontraktów portów.

### ADR-002 PostgreSQL jako jedyne źródło prawdy i mechanizm prac w tle — Zmieniona (it. 2)
- **Kontekst:** każda zmiana stanu Rozwiązania musi atomowo zlecić kolejny krok; szczyt rozpoznawania nie może blokować oceniania (C1-25).
- **Decyzja:** tabela `praca_w_tle` z kolumną `pula` + `SKIP LOCKED` + `LISTEN/NOTIFY`; rozłączne pule korutyn (13.4).
- **Alternatywy:** Redis + Celery (problem podwójnego zapisu, wymaga wzorca outbox), Kafka (nadmiarowa).
- **Konsekwencje:** brak dodatkowego komponentu; przepustowość wystarcza do ~1 000 prac/s, dużo powyżej A-03.
- **Uzupełnienie (it. 4, wersja 2.2, C2-04):** deduplikacja prac obejmuje dla typów-wybudzeń (`AKTUALIZUJ_PROFIL`) wyłącznie prace `OCZEKUJE` (`ux_praca_dedup_oczekujace`), bo deduplikacja z `W_TOKU` gubiła wybudzenie dla Wyniku zatwierdzonego po odczycie przez handler; handler działa w pętli do pustego wyniku, a `ZAMIATAJ_PROFILE` (co 5 min) jest zabezpieczeniem. Alternatywa „tylko pętla w handlerze” — odrzucona: okno między ostatnim odczytem a `ZAKONCZONA` nadal gubi wybudzenie.

### ADR-003 Porty i adaptery dla modeli AI, konfiguracja w bazie — Przyjęta
- **Kontekst:** NFR-06, NFR-07, Jev we wczesnym dostępie.
- **Decyzja:** porty z 13.1, rejestr adapterów, tabela `konfiguracja_modelu` z rolami; SDK dostawców tylko w `lemat.modele.adaptery`.
- **Alternatywy:** bezpośrednie użycie SDK w agentach — odrzucone.
- **Konsekwencje:** zmiana dostawcy bez zmian logiki; konieczna ewaluacja ZT-1 dla ról decyzyjnych.

### ADR-004 Agent rozpoznający oparty na multimodalnym LLM ze strukturyzowanym wyjściem — Zmieniona (it. 2)
- **Kontekst:** potrzebne jednocześnie: LaTeX, podział na kroki, oznaczenie niepewności, ramki danych osobowych.
- **Decyzja:** rola `ROZPOZNAWANIE_PISMA` = multimodalny LLM z wymuszonym schematem JSON; post-processing SymPy obniża pewność niepoprawnego LaTeX. Sygnały LLM (`pewnosc_odczytu`, `czytelne`) działają wyłącznie w kierunku bezpiecznym (Z-1). Decyzję o podejrzeniu błędu odczytu wpływającym na Ocenę podejmuje Model decyzyjny (Q-OC-04 `BLAD_ODCZYTU`, Q-OC-06), a nie samoocena LLM.
- **Alternatywy:** wyspecjalizowany OCR wzorów — daje LaTeX, ale bez podziału na kroki i bez znaczników niepewności; pozostaje kandydatem do porównania na ZT-1 (G-2) jako alternatywny adapter.
- **Konsekwencje:** nieskalibrowana samoocena LLM nie jest jedyną ochroną FR-28: działają trzy niezależne mechanizmy — poprawki Ucznia (FR-18), decyzje Modelu decyzyjnego (T-3) i zgłoszenie Ucznia po Ocenie (11.6); skuteczność mierzy 11.8.

### ADR-005 Ocena per kryterium osobnymi pytaniami — Zmieniona (it. 2)
- **Kontekst:** otwarte pytanie „całość naraz czy krok po kroku”; FR-21 wymaga sprawdzenia każdego kroku (C1-15).
- **Decyzja:** SCORE per kryterium + CHOICE pierwszego błędu + CHOICE typu błędu + NOUL wyniku + NOUL wynikania kroków (≤ 12 par) + NOUL podejrzenia błędu odczytu; ≤ 29 pytań, 2 rundy (Jev) lub 1 wywołanie (model zapasowy). Więcej niesprawdzonych par → niespójność N-6 i Weryfikacja.
- **Alternatywy:** jedno pytanie SCORE o sumę punktów — brak lokalizacji błędu (FR-27) i słabsza diagnostyka niespójności; limit 40 par — przekracza budżet 17.2.
- **Konsekwencje:** więcej wywołań (tanich), reguły niespójności N-1…N-6; żadne niesprawdzone wynikanie nie przechodzi automatycznie.

### ADR-006 Pewność oceny = minimum prawdopodobieństw + kalibracja izotoniczna — Zmieniona (it. 2)
- **Kontekst:** FR-22, NFR-04; wiele decyzji składowych.
- **Decyzja:** `pewnosc_surowa = min(p)` z ograniczeniem przy niespójności, kalibrator izotoniczny per (konfiguracja, rodzaj) w tabeli `kalibrator_pewnosci`; każda niespójność **niezależnie od kalibratora** wymusza powód `NISKA_PEWNOSC`.
- **Alternatywy:** iloczyn prawdopodobieństw (zaniżony, zależny od liczby kryteriów), średnia (maskuje najsłabszą decyzję).
- **Konsekwencje:** pewność porównywalna między Zadaniami o różnej liczbie kryteriów; kalibrator naprawia nadmierną pewność min, ale nie może „wyprowadzić” niespójnej Oceny na trasę automatyczną.

### ADR-007 Próg pewności z górnej granicy Wilsona, Kontroli losowej z wagami IPW i analizy mocy — Zmieniona (it. 2)
- **Kontekst:** NFR-03 wymaga mierzenia błędów ocen automatycznych, które z definicji nie trafiają do Nauczyciela; ZT-1 w wersji 1 był za mały (C1-09); Kontrola mogła nie być wykonywana (C1-10).
- **Decyzja:** Kontrola losowa 20% → 5% (próg 400 próbek), termin 3 dni robocze (wersja 2.1: 72 h); wybór progu 11.5.3 z `n_eff ≥ 189`, kalibracją out-of-fold (5-krotna CV) i wagami IPW skorygowanymi o odsetek wykonania Kontroli; reguły nadzoru (blokada obniżania progu, bezpiecznik braku nadzoru); ZT-1 ≥ 700 obliczeniowych z analizy mocy (11.5.1); tryb pełnej weryfikacji, gdy ZT-1 nie daje progu (G-0).
- **Alternatywy:** stały próg 0.9 (niemierzalny NFR-03); kalibracja tylko na Weryfikacjach z niskiej pewności (obciążenie selekcją); traktowanie niewykonanych Kontroli jako błędów (zbyt konserwatywne przy 90% wykonania — zawyża błąd o do 10 pp).
- **Konsekwencje:** obciążenie Kontrolą wliczone w model 17.7 (6% na starcie, 3% później); brak nadzoru zawsze podnosi próg, nigdy go nie obniża.
- **Uzupełnienie (it. 4, wersja 2.2, C2-05, C2-06):** źródło Próbki kalibracyjnej wynika z `weryfikacja.powody_poczatkowe` (powody dopisane później nie wykluczają próbki — usunięcie nielosowych braków); analiza mocy G-1(a) per rodzaj: 700 obliczeniowych z LB ≥ 87,5% i 350 dowodów z LB ≥ 85,0% (D-12); Transkrypcje z Poprawkami mają podwójny odsetek Kontroli losowej i własny bezpiecznik (11.5.2).
- **Uzupełnienie (it. 5, wersja 2.3, C3-01):** bezpieczniki trafności i poprawek reagują na dowód przekroczenia (LB Wilsona 95% > 2% przy `n_eff ≥ 50`), a nie na brak dowodu spełnienia; próba liczona dla pary (konfiguracja, rodzaj) powyżej bieżącego progu, nie dla rekordu progu — ADR-046.

### ADR-008 Opinia pomocnicza LLM nie zmienia trasy Oceny — Przyjęta
- **Kontekst:** architektura wstępna: „pewność < próg → druga ocena przez mocniejszy LLM”; FR-24: „pewność poniżej progu → Weryfikacja”.
- **Decyzja:** zgodnie z Z-2 FR-24 ma pierwszeństwo; druga ocena LLM jest asynchroniczną Opinią pomocniczą dla Nauczyciela.
- **Alternatywy:** zgoda Jev + LLM jako podstawa automatycznego zaliczenia — odrzucona do czasu, gdy zespół będzie miał własny kalibrator i ≥ 400 próbek (decyzja do ponownego rozpatrzenia w przyszłym ADR).
- **Konsekwencje:** zgodność z FR-24; dane do oceny przyszłej automatyzacji.

### ADR-009 Model zapasowy: LLM ze strukturyzowaną odpowiedzią + circuit breaker + osobna kalibracja — Zmieniona (it. 2)
- **Kontekst:** NFR-07, A-02; pytania Agenta oceniającego są zależne (C1-07).
- **Decyzja:** 9.9; jedna Ocena = jeden model decyzyjny; model zapasowy wykonuje cały `PlanPytan` jednym wywołaniem (pytania warunkowe zwinięte, 13.2); własny kalibrator i próg; ponowienie Jev tylko przy zapasie ≥ 13 s do terminu miękkiego; obwód otwiera się po 3 kolejnych błędach.
- **Alternatywy:** przełączanie per pytanie (mieszanie rozkładów o różnej kalibracji) — odrzucone; model zapasowy w rundach jak Jev — odrzucony (2–3 wywołania LLM po 6 s przekraczają budżet).
- **Konsekwencje:** najgorszy przypadek z przełączeniem w trakcie Oceny mieści się w SLO (17.2, S-1…S-3); przy awarii Jev rośnie odsetek Weryfikacji, ale NFR-03 pozostaje spełnione.

### ADR-010 Profil ucznia jako model Elo per Umiejętność; model drzewiasty jako eksperyment — Zmieniona (it. 2)
- **Kontekst:** potrzeba działania od pierwszego dnia (brak danych do GBM), kalibracji Poziomu trudności i deterministycznego odtwarzania po korektach (C1-11).
- **Decyzja:** 10.1.3; Wyniki z Testu diagnostycznego są zwykłymi zdarzeniami w historii; odtwarzanie całego Profilu ucznia od stanu DOMYŚLNEGO; GBM przez port `PredyktorSukcesu` po zebraniu 20 000 Wyników zadania.
- **Alternatywy:** BKT (gorzej obsługuje punkty cząstkowe), IRT offline (wymaga dużych danych na starcie); odtwarzanie od „punktu po teście” — odrzucone (podwójne liczenie).
- **Konsekwencje:** prosta, deterministyczna aktualizacja; koszt odtworzenia < 50 ms dla ≤ 5 000 Wyników.

### ADR-011 Uzasadnienie rekomendacji z szablonów, nie z LLM — Zmieniona (it. 2)
- **Kontekst:** FR-10, NFR-05; ryzyko halucynacji liczb.
- **Decyzja:** szablony 10.1.5 wypełniane faktami zapisanymi w `uzasadnienie_fakty`, dla **każdej** wartości `regula` (także `R-JEV-*`, `R-SERIA`, `R-UTRWALENIE`, `R-WKLADKA-DIAG`) i dla braku elementu; test pokrycia.
- **Konsekwencje:** uzasadnienie zawsze prawdziwe i audytowalne; mniej „naturalny” język.

### ADR-012 Wersjonowanie Zadań i niezmienność Ocen — Zmieniona (it. 2)
- **Kontekst:** FR-14 (poprawki Zadań), FR-34 (korekty Ocen), NFR-05, asynchroniczna Wskazówka.
- **Decyzja:** `zadanie_wersja` niezmienna (z `zastapiona_at`); Rozwiązanie, Odpowiedź i rozpoczęty element wskazują wersję; Ocena tylko dopisywana z `wersja`; jedyny wyjątek — jednokrotny zapis Wskazówki (trigger `trg_ocena_niezmienna`).
- **Konsekwencje:** pełna historia; nieco bardziej złożone zapytania (indeks `ux_ocena_aktualna`, stała kolejność „najpierw `ZASTAPIONA`, potem `INSERT`”).

### ADR-013 SymPy w piaskownicy procesowej — Przyjęta
- **Kontekst:** FR-21, FR-13; ryzyko zawieszenia i nadużycia parsera.
- **Decyzja:** pula procesów z limitami 16.2, `parse_latex` (backend `lark`), wynik trójwartościowy.
- **Konsekwencje:** SymPy jest sygnałem, nie werdyktem — nierównoważne przekształcenia bywają poprawne (np. przejście do zbioru rozwiązań).

### ADR-014 Odpytywanie (polling) zamiast WebSocket/SSE — Zmieniona (it. 2)
- **Kontekst:** NFR-08 (sieci komórkowe), prostota infrastruktury, budżet NFR-01.
- **Decyzja:** `GET /rozwiazania/{id}` co 1 s w stanach przejściowych i przy Wskazówce w przygotowaniu, z podpowiedzią `nastepne_odpytanie_ms`; powiadomienia co 60 s.
- **Konsekwencje:** ≤ 1 s dodatkowego opóźnienia na fazę (ujęte w 17.2 jako różnica 28 s / 30 s); w szczycie lekcji ≤ 90 żądań/s — pomijalne dla `api`.

### ADR-015 Publikacja Zadań wygenerowanych po Walidacji; Wkładki i dowody tylko po przeglądzie — Zmieniona (it. 2)
- **Kontekst:** FR-13, FR-14; skala wymaga automatyzacji, ale dowodów nie da się sprawdzić symbolicznie; dowody bez zlecającego utykały w `DO_PRZEGLADU` (C1-22).
- **Decyzja:** obliczeniowe i zamknięte → `OPUBLIKOWANE` (`GLOBALNY`) po pozytywnej Walidacji; dowody i Wkładki → `DO_PRZEGLADU`, publikuje recenzent treści lub Administrator (ADR-028). Generowanie systemowe (`UZUPELNIJ_PULE`) nigdy nie tworzy dowodów (CHECK w `zlecenie_generowania`); szkice Wkładek powstają także automatycznie przy E-20.
- **Konsekwencje:** każde Zadanie i Wkładka w `DO_PRZEGLADU` ma uprawnionego recenzenta; FR-06 jest pokryte dla obu poziomów (G-5).

### ADR-016 Identyfikatory domenowe po polsku, zgodne ze słownikiem — Zmieniona (it. 2)
- **Kontekst:** wymóg zgodności ze słownikiem pojęć (Z-6); homonimy w wersji 1 (C1-03, C1-04).
- **Decyzja:** reguły 3.1 (w tym „jedno słowo — jedno znaczenie” i lista dopuszczonych terminów technicznych); zmiany nazw: `DecyzjaSkladowa`, `RezultatPytan`, `RezultatOceny`, `RezultatRozpoznawania`, `Rownowaznosc`, `rezultat_walidacji`, `procent_grupy_eksperymentu`, `nr_kryterium`, `wymaga_nr_kryterium`, `wynik_koncowy_wzorcowy`, `typ_kolejki_weryfikacji`, `decyzja_modelu.wybor`; propozycja zmiany słownika PZS-1 (3.4).
- **Konsekwencje:** brak tłumaczeń między warstwami; zmiany słownika wymagają akceptacji (G-0).

- **Uzupełnienie (it. 6, wersja 2.4, C4-04):** „zdjęcie” = wyłącznie fotografia kartki; wyłączenie bezpiecznika = „dezaktywacja” (`POST /admin/bezpieczniki/dezaktywacja`, `dezaktywowany_przez`, `dezaktywowano_at`, `uzasadnienie_dezaktywacji`), odebranie flagi = „odebranie”, wyłączenie flagi danych = „wyłączenie”; „wstrzymanie” ma jedno znaczenie (3.1 p. 5); test CI, część 3, pilnuje tokenów `zdje*` i `wstrzym*` z listami dozwolonych użyć. Odrzucona alternatywa: „wyłączenie bezpiecznika” — słowo „wyłączenie” jest już używane dla flag danych i reguł 11.5.2 („wyłączenie powodów nielosowych”), więc łamałoby 3.1 p. 5 w ten sam sposób.

### ADR-017 Obowiązkowe zatwierdzenie Transkrypcji przez Ucznia — Zmieniona (it. 2)
- **Kontekst:** FR-18, FR-28; FR-28 nie może zależeć wyłącznie od uważności Ucznia (C1-01).
- **Decyzja:** brak automatycznego zatwierdzenia; po 7 dniach Rozwiązanie `PORZUCONE`. Zatwierdzenie jest pierwszą, a nie jedyną linią obrony FR-28: dochodzą T-3 (ADR-022, decyzje Modelu decyzyjnego) i zgłoszenie po Ocenie (11.6).
- **Konsekwencje:** Uczeń nie ponosi skutków błędu odczytu, którego nie zauważył: Ocena z podejrzeniem nie obniża punktów ani Profilu, a niezauważony błąd może zgłosić przez 14 dni.

### ADR-018 Wynik zadania z Oceny wstępnej z wagą 0,5; bez Wyniku przy podejrzeniu błędu odczytu — Zmieniona (it. 2)
- **Kontekst:** FR-29 vs. czas Weryfikacji (dni) — Profil nie może stać w miejscu; FR-28 — błąd odczytu nie może obniżyć Profilu (C1-01); wielokrotne próby nie mogą zawyżać Profilu (C1-21).
- **Decyzja:** `waga = w_status × w_proba` (10.1.3): Ocena wstępna 0,5 (bez wpływu na `trudnosc_b`), Ocena wstępna z `mozliwy_blad_odczytu` — **brak** Wyniku zadania; kolejna próba tego samego Zadania w 60 dniach — × 0,5. Wynik wstępny jest unieważniany i zastępowany po Ocenie ostatecznej; odtworzenie 10.1.3.
- **Konsekwencje:** rekomendacje reagują od razu; korekty są w pełni uwzględniane; Profil nie jest obniżany przez podejrzany odczyt.

### ADR-019 Retencja zdjęć sterowana przejściami stanów, maks. 90 dni — Zmieniona (it. 2)
- **Kontekst:** NFR-09 vs. potrzeba porównania zdjęcia przy Weryfikacji, Kontroli losowej i zgłoszeniu (C1-10, C1-12).
- **Decyzja:** `usun_po` jako jedyne źródło prawdy, ustawiane przez przejścia z 16.4 (30 dni po ocenie bez otwartej Weryfikacji, przedłużenie do 90 dni przy otwartej, natychmiast po porzuceniu/anulowaniu); twardy limit `CHECK`; buckety bez wersjonowania + reguła cyklu życia 91 dni.
- **Konsekwencje:** Weryfikacja dłuższa niż 90 dni odbywa się bez zdjęcia (SLA 3 dni robocze i alert po 80 dniach czynią to wyjątkiem).

### ADR-020 JWT krótkotrwały + rotowany refresh token w cookie — Przyjęta
- **Kontekst:** PWA na telefonach, bezpieczeństwo sesji.
- **Decyzja:** 16.1.
- **Alternatywy:** sesje serwerowe w bazie (dodatkowe zapytanie na każde żądanie), długożyjące JWT (brak unieważniania).
- **Konsekwencje:** unieważnienie dostępu w ≤ 15 min (blokada konta unieważnia refresh tokeny natychmiast).

### ADR-021 Kolejka weryfikacji globalna dla Uczniów bez Klasy — Zmieniona (it. 2), G-0
- **Kontekst:** Uczeń może korzystać z platformy bez Nauczyciela (FR-01), a Weryfikacja jest obowiązkowa (FR-24…FR-26); w wersji 1 brakowało gwarancji istnienia weryfikatorów, SLA i ochrony danych (C1-02).
- **Decyzja:** 11.4; weryfikatorzy globalni z deklarowanym czasem ≥ 90 min na dzień roboczy; bramka G-7 z liczbą wyliczoną z tygodniowego modelu 17.7 (start: 18, wersja 2.2 — ADR-043); SLA 3 dni robocze (w wersji 2.1: 72 h); eskalacja KLASA → GLOBALNA → alert Administratora (nie do tej samej kolejki); kontrola przyjęć; pseudonimizacja (ADR-031).
- **Alternatywy:** automatyczne nadanie Ocenie wstępnej statusu ostatecznej po SLA — **odrzucone**, bo łamie FR-24 i FR-26 („zawsze”); blokada zadań otwartych dla Uczniów bez Klasy — odrzucona (sprzeczna z wizją „jak na maturze”), zastąpiona ograniczeniem udziału zadań otwartych tylko przy przeciążeniu.
- **Konsekwencje:** przy przeciążeniu Uczeń bez Klasy dostaje więcej zadań zamkniętych i jawny status opóźnienia, ale nigdy nie traci Weryfikacji.

### ADR-022 Ocena wstępna wstrzymana przy podejrzeniu błędu odczytu i zgłoszenie Ucznia — Nowa (it. 2), G-0
- **Kontekst:** FR-28 („błędy odczytu nie obniżają oceny”) vs. FR-24 („uczeń widzi ocenę wstępną”); w wersji 1 podejrzany błąd odczytu zerował punkty za wynik i trafiał do Profilu (C1-01).
- **Decyzja:** (1) moda Q-OC-04 = `BLAD_ODCZYTU` nie jest błędem Ucznia: nie zeruje punktów (FR-20), nie ustala `typ_bledu`; (2) Ocena wstępna z `mozliwy_blad_odczytu` jest pokazywana Uczniowi jako „wstrzymana”: status, krok podejrzany, komunikat — bez punktów, typu błędu i Wskazówki; (3) taka Ocena nie tworzy Wyniku zadania; (4) Uczeń może zgłosić błąd odczytu do 14 dni po Ocenie (11.6), co tworzy Weryfikację z priorytetem 2.
- **Alternatywy:** „ocena w najlepszej wierze” (maksimum punktów dla kryteriów, które mogły być dotknięte błędem) — odrzucona, bo pokazuje Uczniowi zawyżone punkty, które Nauczyciel potem obniża (gorsze doświadczenie niż wstrzymanie); pokazanie punktów modelu — odrzucone (łamie FR-28).
- **Konsekwencje:** w tym jednym przypadku FR-28 ma pierwszeństwo przed literalnym brzmieniem FR-24 (Uczeń widzi Ocenę wstępną bez punktów); wymaga akceptacji w G-0. Skuteczność mierzona w 11.8.
- **Uzupełnienie (it. 3, wersja 2.1):** (5) zgłoszenie błędu odczytu **zawiesza** Wynik zadania zgłoszonej Oceny do rozstrzygnięcia (unieważnienie z powodem `ZGLOSZENIE_BLEDU_ODCZYTU`, odtworzenie Profilu, 9.12), więc niezauważony przez Ucznia i przez T-3 błąd odczytu nie obniża Profilu także w czasie Weryfikacji; (6) po usunięciu zdjęć przed rozstrzygnięciem kroki podejrzane są oceniane na korzyść Ucznia, a odstępstwo wymaga komentarza (11.4.3 p. 3). Alternatywa „Wynik w mocy do rozstrzygnięcia” (wersja 2.0) — odrzucona, bo przez ≤ 72 h (SLA) Profil i rekomendacje opierałyby się na Ocenie, którą Uczeń zakwestionował z powodu odczytu.

### ADR-023 Rekomendacja synchroniczna w procesie `api` przez fasadę Orkiestratora — Nowa (it. 2)
- **Kontekst:** słownik: Orkiestrator koordynuje agentów; wersja 1 wywoływała Agenta rekomendującego z `api` z pominięciem Orkiestratora i bez timeoutu (C1-17).
- **Decyzja:** 6.3: `Orkiestrator.rekomenduj_synchronicznie()` w procesie `api`; jedyne wywołanie modelu — Q-RE-01 z timeoutem 1,5 s, bez ponowień, z circuit breakerem; przy błędzie POLITYKA_ELO; zlecenia prac (`UZUPELNIJ_PULE`, `GENERUJ_SZKIC_WKLADKI`) w tej samej transakcji.
- **Alternatywy:** praca w tle + odpytywanie — odrzucona (+ ≥ 1 s na każde „Dalej”, a przepływ nie angażuje innych agentów).
- **Konsekwencje:** p95 `GET …/nastepna` ≤ 500 ms (bez JEV_CHOICE ≤ 150 ms); krawędź `api → Jev` w 5.1.

### ADR-024 Brak wpisu ręcznego Rozwiązania — Nowa (it. 2), G-0
- **Kontekst:** wizja i słownik: Rozwiązanie to zdjęcie kartki („jak na prawdziwej maturze”); wpis ręczny w wersji 1 był rozszerzeniem spoza wymagań, omijał T-3 i zmieniał rozkład danych kalibracyjnych (C1-26).
- **Decyzja:** usunięto `WPIS_RECZNY_UCZNIA`. Po błędzie rozpoznawania Uczeń ma: nowe zdjęcia (≤ 3 przesłania), ponowienie rozpoznania tych samych zdjęć po awarii dostawcy (≤ 5, bez zużycia przesłania), porzucenie i nowe Rozwiązanie dla tego samego elementu do `wygasa_at`.
- **Alternatywy:** wpis ręczny z obowiązkową Weryfikacją — odrzucony (obciąża Nauczycieli i nadal pozwala ominąć kartkę).
- **Konsekwencje:** Transkrypcja zawsze pochodzi ze zdjęcia (PZS-1 B-1) — od wersji 2.2 gwarantują to limity i Sprawdzenie zgodności Poprawek Transkrypcji (ADR-036), bo bez nich nieograniczona edycja Transkrypcji była wpisem ręcznym „tylnymi drzwiami” (C2-01); dane kalibracyjne jednorodne.

### ADR-025 NFR-01 jako czas systemowy; termin miękki i twardy; Wskazówka asynchroniczna — Nowa (it. 2), G-0
- **Kontekst:** wersja 1 mierzyła NFR-01 od zatwierdzenia i liczyła T-6 jako sukces, przez co NFR-01 było spełnione z definicji (C1-08); Agent wyjaśniający był na ścieżce krytycznej.
- **Decyzja:** A-11 i 17.1: pomiar obejmuje rozpoznanie i ocenę; T-6 i błędy dostawcy są naruszeniami; termin miękki skraca timeouty, termin twardy 120 s kończy próby Oceną albo T-6; Wskazówka powstaje w pracy `NAPISZ_WSKAZOWKE` po zapisie Oceny.
- **Alternatywy:** pomiar od przesłania z czasem Ucznia na sprawdzenie Transkrypcji — odrzucony (system nie kontroluje czasu Ucznia, a FR-18 go wymaga); twardy termin 28 s z T-6 — odrzucony (zamieniał spóźnione Oceny na brak Oceny).
- **Konsekwencje:** NFR-01 mierzalne i niespełnialne „z definicji” tylko przez poprawne działanie; Wskazówka pojawia się do ~10 s po Ocenie.

### ADR-026 Plan pytań z zależnościami jako kontrakt portu `ModelDecyzyjny` — Nowa (it. 2)
- **Kontekst:** pytania Agenta oceniającego są zależne (wariant → kryteria, pierwszy błąd → typ błędu); model zapasowy nie znał wyników wcześniejszych rund (C1-07).
- **Decyzja:** `PlanPytan` z `WarunekPytania` (głębokość 1); Jev wykonuje plan w 2 rundach, model zapasowy jednym wywołaniem z pytaniami warunkowymi zwiniętymi do pól schematu (13.2).
- **Alternatywy:** zadawanie Q-OC-04 dla każdego kroku — odrzucone (do 40 pytań więcej dla Jev); rundy dla modelu zapasowego — odrzucone (budżet).
- **Konsekwencje:** Agent oceniający niezależny od modelu (NFR-06); pytania warunkowe niespełnione są jawnie zwracane w `pominiete`.

### ADR-027 Zgodność Oceny = punkty + pierwszy błędny krok + brakujący krok — Nowa (it. 2), G-0
- **Kontekst:** słownik: Ocena obejmuje wskazanie błędnego kroku, a Pewność oceny to prawdopodobieństwo, że Ocena jest poprawna; wersja 1 kalibrowała tylko sumę punktów (C1-16).
- **Decyzja:** `probka_kalibracyjna.zgodna` = równe punkty ∧ równy `pierwszy_bledny_krok` (lub oba brak) ∧ równe `brakujacy_krok`. Typ błędu jest mierzony osobno (`zgodny_typ_bledu`), bo jest klasyfikacją pomocniczą dla Wskazówki, a nie elementem definicji Oceny w słowniku. Ta sama definicja służy NFR-02 (PZS-1 B-5).
- **Alternatywy:** tylko punkty — odrzucone (Ocena z dobrą sumą i złym krokiem byłaby „poprawna”); punkty + krok + typ — odrzucone (zaniżałoby zgodność z powodu rozbieżności klasyfikacji, które nie zmieniają Oceny).
- **Konsekwencje:** NFR-02 i NFR-04 są trudniejsze do spełnienia, ale zgodne ze słownikiem; ZT-1 musi mieć oznaczony pierwszy błędny krok przez Nauczycieli.

### ADR-028 Zasięg Zadań i recenzent treści — Nowa (it. 2), G-0
- **Kontekst:** w wersji 1 każdy Nauczyciel mógł poprawić lub wycofać dowolne Zadanie (także z Arkusza CKE), a Zadania Nauczycieli trafiały do globalnego doboru bez przeglądu (C1-22).
- **Decyzja:** `zadanie.zasieg` (`GLOBALNY`/`PRYWATNY`); Zadania `BAZOWE_NAUCZYCIEL` są `PRYWATNY` (tylko Przypisania autora), dopóki recenzent treści lub Administrator nie zatwierdzi ich do `GLOBALNY`; flaga `nauczyciel.recenzent_tresci`; uprawnienia 8.4; endpointy Przeglądu i wycofania dla Administratora.
- **Alternatywy:** wszystkie Zadania globalne z obowiązkowym przeglądem — odrzucone (blokuje Nauczyciela, który chce użyć własnego zadania w swojej Klasie od razu).
- **Konsekwencje:** zmiany treści wpływające na wszystkich Uczniów wykonują tylko osoby uprawnione; `POST /zadania` tylko dla roli Nauczyciel (Administrator importuje Arkusze CKE), co zachowuje definicję Zadania bazowego.
- **Uzupełnienie (it. 4, wersja 2.2, C2-03):** ograniczenie bezpośredniej poprawki Zadań `GLOBALNY` do recenzentów treści zawęża FR-14 („Nauczyciel może przejrzeć, poprawić lub odrzucić”), więc decyzja jest oznaczona G-0 (D-11); FR-14 dla Zadań zleconych przez Nauczyciela jest spełnione w pełni przez ADR-038, a dla Zadań `GLOBALNY` — przez Zgłoszenie błędu w Zadaniu.

### ADR-029 Kalibracja Poziomu trudności w osobnej tabeli z atomową aktualizacją — Nowa (it. 2)
- **Kontekst:** równoległe Wyniki zadania gubiły aktualizacje `trudnosc_b` i podnosiły `zadanie.version`, powodując `412` w Przeglądzie (C1-23).
- **Decyzja:** tabela `kalibracja_trudnosci` (`trudnosc_b`, `liczba_wynikow_kalibracji`, skalibrowany `poziom_trudnosci`), atomowy `UPDATE` z blokadą wiersza (10.1.3), histereza 0,1, audyt zmiany poziomu; `zadanie.poziom_trudnosci_nominalny` zmienia tylko człowiek (Przegląd, import).
- **Konsekwencje:** brak utraconych aktualizacji (C-24); Poziom trudności w API = wartość skalibrowana (słownik), nominalna dostępna osobno (PZS-1 B-4).
- **Uzupełnienie (it. 5, wersja 2.3, C3-09):** ten sam wzorzec obejmuje Wstrzymanie w doborze (tabela `wstrzymanie_w_doborze` zamiast kolumny `zadanie.wstrzymane_w_doborze`): zmiana systemowa wywołana zgłoszeniem nie podnosi `zadanie.version`, więc recenzent z `If-Match` nie dostaje `412` po każdym nowym zgłoszeniu (powrót problemu C1-23 wykluczony).

### ADR-030 Seria nauki po Wkładce teoretycznej — Nowa (it. 2)
- **Kontekst:** wizja: po Wkładce „seria zadań z tego działu o stopniowo rosnącej trudności, aż do poziomu maturalnego”; polityka wersji 1 mogła po Wkładce przeskoczyć do innej Umiejętności (C1-29).
- **Decyzja:** stan Serii w `sciezka_nauki.seria_*`; reguła R-SERIA (10.1.2 krok 3) z poziomem niemalejącym przy sukcesach; zakończenie sukcesem po `ulamek ≥ 0,7` na Poziomie trudności ≥ 4, a porażką po 3 kolejnych błędach, 8 elementach lub 7 dniach.
- **Alternatywy:** brak Serii (czysty ranking) — odrzucone (niezgodne z wizją).
- **Konsekwencje:** przewidywalna ścieżka po Wkładce; Przypisania mają pierwszeństwo, powtórki są wstrzymane na czas Serii.

### ADR-031 Pseudonimizacja i maskowanie zdjęć w kolejce globalnej — Nowa (it. 2), G-0
- **Kontekst:** w wersji 1 Nauczyciel spoza Klasy widział imię, nazwisko i niezamaskowane zdjęcia Ucznia (C1-02).
- **Decyzja:** 11.4.4: pseudonim HMAC, brak Klasy i innych Rozwiązań, zdjęcia maskowane na podstawie ramek danych osobowych z Agenta rozpoznającego; informacja przy rejestracji; umowy poufności weryfikatorów (16.3).
- **Alternatywy:** brak kolejki globalnej — odrzucone (FR-24…FR-26 dla Uczniów bez Klasy). Twierdzenie wersji 2.1, że anonimizacja Transkrypcji jest zbędna, bo „Transkrypcja zawiera wyłącznie rozwiązanie”, było niczym niezagwarantowane (C2-10) — zastąpione Filtrem danych osobowych (ADR-042).
- **Konsekwencje:** minimalizacja danych zgodna z NFR-09; koszt: jedno maskowanie zdjęcia (Pillow, ~50 ms) na przesłanie.

### ADR-032 Jeden kontekst na próbę, wielokrotne próby i ujawnianie klucza po terminie — Nowa (it. 2)
- **Kontekst:** Odpowiedź bez kontekstu pozwalała powtarzać Zadanie zamknięte i zawyżać Profil; brak unikalności w Przypisaniach; klucz ujawniany od razu w Przypisaniu (C1-21).
- **Decyzja:** każda Odpowiedź i każde Rozwiązanie ma dokładnie jeden kontekst: element Ścieżki nauki albo pozycję Przypisania (CHECK w DDL, Test diagnostyczny osobno); indeksy unikalne per (Uczeń, Przypisanie, Zadanie); kolejna próba tego samego Zadania w 60 dniach ma wagę × 0,5 (ADR-018); w Przypisaniu wynik i klucz Odpowiedzi są ujawniane od terminu (`rezultat_ujawniony_od`). Rozwiązania z listy Przypisań nie tworzą elementów Ścieżki nauki (Ścieżka zawiera wyłącznie propozycje agenta — PZS-1 B-3).
- **Konsekwencje:** brak duplikatów Weryfikacji dla Nauczyciela; Profil odporny na powtarzanie.
- **Uzupełnienie (it. 4, wersja 2.2, C1-21):** element Ścieżki nauki z reguły `R-PRZYPISANIE` był obejściem niezmiennika (próba miała tylko `element_sciezki_id`). Decyzja: klient podaje jeden kontekst, ale **kontekst efektywny** próby z elementu `R-PRZYPISANIE` zawiera także `przypisanie_id` elementu — dziedziczone w bazie przez `trg_kontekst_proby` (para dozwolona wyłącznie przy `element.przypisanie_id = przypisanie_id`). Skutki: `ux_odpowiedz_przypisanie`/`ux_rozwiazanie_przypisanie` obejmują obie drogi (C-30); `rezultat_ujawniony_od = greatest(now(), termin)` także z elementu; przydział 11.4.1 p. 1 kieruje Weryfikację do Nauczyciela Przypisania; próba liczy się do `PostepPrzypisaniaWyjscie`; CHECK w 7.3 jawnie wylicza kombinacje {E}, {P}, {E, P}, {T}. Alternatywa „rezygnacja z elementu `R-PRZYPISANIE` i kierowanie Ucznia na listę Przypisań” — odrzucona, bo Przypisania przestałyby być częścią Ścieżki nauki proponowanej przez agenta (PZS-1 B-3) i rekomendacja nie mogłaby wybierać, kiedy je zaproponować.

### ADR-033 Weryfikacja: SLA, zwalnianie wygasłych blokad, przedłużanie i szkice — Nowa (it. 2)
- **Kontekst:** w wersji 1 wygasła blokada zostawiała Weryfikację na zawsze w `W_TRAKCIE`, nie było przedłużenia ani autozapisu (C1-06); Kontrola losowa nie miała terminu (C1-10).
- **Decyzja:** reguła odczytu „dostępna” + `ZWOLNIJ_BLOKADY_WERYFIKACJI` co minutę; `POST …/przedluzenie` (łącznie ≤ 2 h) i idempotentne podjęcie przez właściciela; `szkic_rozstrzygniecia` per (Weryfikacja, Nauczyciel); `termin_at` dla wszystkich powodów (wersja 2.1: 72 h; od 2.2: 3 dni robocze, ADR-043); eskalacja co godzinę; poprawiony SQL podjęcia (9.6.1).
- **Konsekwencje:** porzucona Weryfikacja wraca do kolejki w ≤ 1 min; Nauczyciel nie traci pracy przy przejęciu; każda Weryfikacja ma mierzalny termin.
- **Uzupełnienie (it. 4, wersja 2.2, C2-02, C2-16):** przedłużenie i podjęcie nie zmieniają `version` (ADR-037); ponowne podjęcie przez właściciela nie wydłuża blokady poza `podjeta_at + 2 h`.

### ADR-034 Pula weryfikatorów globalnych: Nauczyciele bez Klas + Nauczyciele współpracujący — Nowa (it. 3), G-0
- **Kontekst:** G-7 wymagało w wersji 2.1 na starcie 12 weryfikatorów globalnych (17.7; od wersji 2.2 — 18, ADR-043), a A-03 przewiduje 50 Nauczycieli, z których 40 prowadzi Klasy — bez Klas jest tylko 10. Wersja 2.0 nie mówiła, skąd pochodzi brakująca pojemność, więc G-7 było niespełnialne przy założeniach A-03 (uzupełnienie C1-02).
- **Decyzja:** A-14: pula = 10 Nauczycieli bez Klas + ≥ 8 Nauczycieli współpracujących na umowę (wersja 2.1: ≥ 2; zmiana wynika z modelu tygodniowego z eskalacjami, ADR-043); Nauczyciel prowadzący Klasę może dołączyć tylko przy łącznym obciążeniu ≤ 180 min na dzień roboczy (walidacja `422 LIMIT_OBCIAZENIA_WERYFIKATORA`). Plan awaryjny bez rekrutacji: start z `LEMAT_REJESTRACJA_BEZ_KLASY = false` (rejestracja Ucznia tylko z kodem zaproszenia), wtedy kolejka `GLOBALNA` obsługuje wyłącznie eskalacje z Klas i Uczniów, którzy opuścili Klasę.
- **Alternatywy:** obowiązkowy przydział każdego Nauczyciela prowadzącego Klasę do puli globalnej — odrzucony (72 + 90 = 162 min/dobę na start, czyli ponad 2,5 h pracy dziennie dla każdego, R-02); Ocena wstępna jako ostateczna po SLA — odrzucona w ADR-021 (łamie FR-24/FR-26).
- **Konsekwencje:** koszt operacyjny (umowy z ≥ 8 Nauczycielami na czas fazy startowej, ≥ 4 w fazie ustalonej) albo ograniczenie zakresu pilotażu do Uczniów z Klas — **decyzja właściciela produktu w G-0**; architektura obsługuje oba warianty bez zmian kodu (flaga konfiguracji).

### ADR-035 Kopie treningowe z rejestrem powiązań w bazie — Nowa (it. 3), G-0
- **Kontekst:** 16.4 (wersja 2.0) przewidywało kopie zdjęć do zbioru treningowego „z pseudonimem zamiast `uczen_id`” i usunięcie po odwołaniu zgody, ale nie było żadnego powiązania kopii z kontem — odwołania zgody ani usunięcia konta nie dało się wykonać (uzupełnienie C1-12).
- **Decyzja:** tabela `kopia_treningowa` (7.3) jako jedyne powiązanie kopii z Uczniem; kopia zawiera zdjęcie maskowane, Transkrypcję i Ocenę ostateczną; tworzy ją `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` przy przejściu do `OCENIONE` (przed upływem `usun_po` zdjęcia); odwołanie zgody (`PATCH /uczniowie/ja`) i `USUN_KONTO` (krok 4a) ustawiają usunięcie; `ON DELETE RESTRICT` wymusza kolejność kroków.
- **Alternatywy:** pseudonim bez powiązania (anonimizacja) — odrzucony, bo zgoda jest odwołalna (art. 7 ust. 3 RODO) i musi dać się wykonać; kopie oryginałów zdjęć — odrzucone (minimalizacja: oryginał może zawierać imię i nazwisko).
- **Konsekwencje:** prawo do odwołania zgody i do usunięcia jest wykonalne w ≤ 1 h; zbiór treningowy nie zawiera danych identyfikacyjnych (od wersji 2.2 gwarantuje to Filtr danych osobowych, ADR-042).
- **Uzupełnienie (it. 4, wersja 2.2, C2-16):** kopia jest zlecana przy każdym przejściu do stanu „`OCENIONE` bez otwartej Weryfikacji” (także po zamknięciu Kontroli losowej lub zgłoszenia) i nadpisywana, gdy Ocena aktualna jest nowsza niż zapisana (`kopia_treningowa.ocena_id`).

### ADR-036 Poprawki Transkrypcji ograniczone i sprawdzane ze zdjęciem — Nowa (it. 4), G-0
- **Kontekst:** `PUT …/transkrypcja` przyjmował dowolną listę 1–40 kroków, a kroki zmienione były wyłączone z podejrzenia odczytu (11.1 p. 7c); Uczeń mógł sfotografować dowolną kartkę, wpisać gotowe rozwiązanie i przejść trasą automatyczną — sprzeczność z ADR-024 i PZS-1 B-1 (C2-01). FR-18 mówi o poprawie „błędnie odczytanego **fragmentu**”.
- **Decyzja:** (1) `TranskrypcjaWejscie` wiąże każdy krok z krokiem agenta (`nr_kroku_agenta`): bez usuwania, ≤ 2 kroki wstawione; (2) limity twarde (`422 POPRAWKI_ZBYT_ROZLEGLE`): zmienione + wstawione ≤ max(3, ⌈0,5 n⌉), odsetek zmienionych znaków ≤ 0,50; (3) limity miękkie i **Sprawdzenie zgodności poprawek** (rola `ROZPOZNAWANIE_PISMA`, praca `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` startująca przy `PUT`) — niezgodność, brak rezultatu albo przekroczenie limitu miękkiego dodaje powód `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM` (T-7, priorytet 2), wyłącznie w kierunku bezpiecznym (Z-1); (4) Oceny z T-7 są poza populacją progu (`WERYFIKACJA_INNY_POWOD`), Oceny automatyczne z Poprawkami mają podwójny odsetek Kontroli losowej i własny bezpiecznik (11.5.2); (5) metryki `odsetek_rozwiazan_z_poprawkami_30d`, `blad_automatycznych_z_poprawkami_30d`; G-2 mierzy wykrywalność (≥ 95%) i fałszywe alarmy (≤ 10%).
- **Alternatywy:** każda Poprawka → Weryfikacja — odrzucona (FR-18 jest codzienną funkcją; podwoiłoby obciążenie Nauczycieli z 17.7); pełne wyłączenie Rozwiązań z Poprawkami z populacji progu — odrzucone, bo NFR-03 dotyczy wszystkich ocen automatycznych, więc te Rozwiązania muszą być mierzone (podwójna Kontrola losowa zamiast wyłączenia); edycja tylko kroków z `pewnosc_odczytu < 0.80` — odrzucona (agent bywa pewny błędnego odczytu, a to właśnie chroni FR-28).
- **Konsekwencje:** Transkrypcja pochodzi ze zdjęcia także po Poprawkach; ≈ 3% Rozwiązań obliczeniowych więcej w Weryfikacji (17.7); Uczeń z rozległymi zmianami wysyła nowe zdjęcia. Limity zawężają literalne FR-18 — decyzja D-10 (G-0).
- **Uzupełnienie (it. 5, wersja 2.3, C3-05):** kroki wstawione wchodzą do licznika odsetka zmienionych znaków (mianownik: wszystkie kroki agenta), krok wstawiony ≤ min(200, max(60, ⌈1,5 × mediana⌉)) znaków; autozapis Poprawek w PWA (debounce 2 s), deduplikacja Sprawdzenia po `rozwiazanie_id` tylko wśród `OCZEKUJE` z podmianą ładunku; osobna pula `S` (4 korutyny na replikę), wejście z wycinków i zdjęcia poglądowego 1024 px (A-08: p95 ≤ 4 s, limit ≥ 150/min); G-2: „wstawienia rozwiązania” ≥ 95%, fałszywe alarmy ≤ 5%; T-7 wyprowadzone z rozkładu w 17.7 (2,5–3,1%).

- **Uzupełnienie (it. 6, wersja 2.4, C4-06 p. 10):** okno oczekiwania Oceny na rezultat Sprawdzenia zgodności poprawek jest stałe — `zatwierdzono_at + 6 s` — zamiast `min(zatwierdzono_at + 6 s, termin_miekki − 9 s)`, które przy `t_R ≥ 18 s` malało do 1 s i dawało brak rezultatu dla prawie każdej Poprawki zatwierdzonej od razu po edycji; rachunek p_brak ≤ 0,5% obowiązuje dla każdego `t_R` (9.4), a termin miękki (≥ 10 s) nie jest naruszany, bo model zapasowy startuje równolegle ze Sprawdzeniem.

### ADR-037 Wersja treści Weryfikacji oddzielona od blokady — Nowa (it. 4)
- **Kontekst:** podjęcie, ponowne podjęcie, przedłużenie (co 5 min) i `ZWOLNIJ_BLOKADY` podnosiły `weryfikacja.version`, a odpowiedź przedłużenia nie zwracała ETag; rozstrzygnięcie Weryfikacji trwającej > 5 min kończyło się `412`, a sygnał „pojawiło się zgłoszenie” tracił znaczenie (C2-02).
- **Decyzja:** `weryfikacja.version` = wersja **treści**; zmienia ją wyłącznie trigger `trg_weryfikacja_wersja` przy zmianie powodów, zgłoszenia, Oceny weryfikowanej, `ponowna_ocena_at`, Oceny Nauczyciela, anulowania lub zamknięcia; operacje blokady i trasowania (eskalacja, `termin_at`, priorytet po 80 dniach) oraz `opinia_pomocnicza` jej nie zmieniają. Przedłużenie zwraca `{zablokowana_do, version}` i ETag; `412` przy rozstrzygnięciu zawiera `zmiany_tresci`. Scenariusz C-31.
- **Alternatywy:** zwracanie nowego ETag po każdym przedłużeniu i aktualizacja w PWA — odrzucona (wyścig między odpowiedzią przedłużenia a wysyłką rozstrzygnięcia z drugiej karty nadal daje fałszywe `412`, a `412` nie odróżnia przyczyny); osobna kolumna `wersja_blokady` z `If-Match` na obie — odrzucona (blokadę i tak chroni warunek `podjeta_przez = ja AND zablokowana_do > now()`).
- **Konsekwencje:** `412` zawsze oznacza zmianę, którą Nauczyciel musi zobaczyć (C-28); C-23 opiera się na warunku `status = 'OCZEKUJE'`; konwencja 7.2 ma jawny wyjątek.

### ADR-038 Przegląd Zadań zleconych przez Nauczyciela i Zgłoszenia błędu w Zadaniu — Nowa (it. 4), G-0
- **Kontekst:** Zadania wygenerowane (obliczeniowe i zamknięte), także zlecone przez Nauczyciela, po Walidacji trafiały od razu do `OPUBLIKOWANE`/`GLOBALNY`, a poprawić je mogli tylko recenzent treści i Administrator; FR-14 przyznaje to prawo Nauczycielowi; zawężenie nie było w G-0 (C2-03).
- **Decyzja:** (1) Zadanie wygenerowane na zlecenie Nauczyciela zawsze przechodzi `WALIDACJA → DO_PRZEGLADU`; zlecający przegląda, poprawia albo odrzuca (FR-14 w pełni), wynik `PRYWATNY`; promocja do `GLOBALNY` — recenzent treści lub Administrator; po 14 dniach Zadanie trafia także do kolejki recenzentów; (2) Zadania `GLOBALNY`: każdy Nauczyciel składa **Zgłoszenie błędu w Zadaniu** (`zgloszenie_bledu_zadania`, `POST /zadania/{id}/zgloszenia-bledu`), rozpatrywane przez recenzentów i Administratorów z SLA 5 dni roboczych; 3 otwarte zgłoszenia od różnych Nauczycieli tworzą wiersz `wstrzymanie_w_doborze` (od wersji 2.3 poza `zadanie.version`, C3-09); (3) zlecenia Administratora i systemu — bez zmian (ADR-015).
- **Alternatywy:** bezpośrednia poprawka Zadania `GLOBALNY` przez każdego Nauczyciela — odrzucona jako opcja domyślna (jedna osoba zmienia treść dla wszystkich Uczniów bez przeglądu; nowa wersja Zadania wpływa na kalibrację Poziomu trudności); prywatna kopia Zadania `GLOBALNY` do poprawy — odrzucona (kopia nie jest Zadaniem bazowym ani wygenerowanym w rozumieniu słownika).
- **Konsekwencje:** Nauczyciel ma pełne FR-14 dla Zadań, które sam zlecił, i ścieżkę zgłoszenia dla pozostałych; pula `GLOBALNY` rośnie z zleceń Nauczycieli wolniej (wymaga promocji). Zawężenie dla Zadań `GLOBALNY` — decyzja D-11 (G-0).

### ADR-039 Tryb wsadowy Jev i globalny ogranicznik szybkości — Nowa (it. 4)
- **Kontekst:** plan pytań ma do 29 pytań (28 w rundzie 1 przy jednym wariancie), a budżet 17.2 zakładał ≤ 2 fale po 8 wywołań; limit A-01 jest per klucz, a pilnował go semafor per proces; `429` otwierał obwód i przełączał ruch na model zapasowy przy działającym Jev (C2-07).
- **Decyzja:** (1) wymaganie A-01: żądanie wsadowe (≤ 32 pytania na rundę); bez niego tryb `POJEDYNCZY` z ≤ 16 wywołaniami na Ocenę i 3 falami — budżet i S-1…S-7 liczone dla tego trybu; (2) termin miękki `max(10 s, 28 s − t_R)` zgodny z SLO 28 s; (3) globalny ogranicznik: przydział `0,8 × limit × waga / Σ wag` z rejestru `limit_szybkosci_modelu` (wagi worker 1,0, `api` 0,1), lokalny token bucket, oczekiwanie do `termin_miekki − 11 s` (od wersji 2.3 zastąpione przyjęciem planu i terminami fal — uzupełnienie it. 5), potem model zapasowy z przyczyną `OGRANICZENIE_LOKALNE`; (4) `429` i brak przydziału nie liczą się do circuit breakera; `429` zmniejsza przydział o połowę na 30 s.
- **Alternatywy:** token bucket w PostgreSQL (wiersz na klucz, `UPDATE … RETURNING` na każde żądanie) — odrzucony (≈ 44 dodatkowe zapisy/s w szczycie i zależność ścieżki krytycznej od bazy); Redis jako licznik — odrzucony (nowy komponent, ADR-002).
- **Konsekwencje:** szczyt „koniec lekcji” mieści się w limicie klucza; nadmiar degraduje do modelu zapasowego bez fałszywego otwarcia obwodu; G-4 (d) testuje `429`.
- **Uzupełnienie (it. 5, wersja 2.3, C3-04):** jedna formuła przydziału `0,8 × limit × waga_i / Σ wag` z kolumną `waga` w `limit_szybkosci_modelu` (DDL, 13.2, 17.4, 22 zgodne); jeden token = jedno wywołanie HTTP; pojemność kubełka workera 16 ≥ maks. fala (kolumna `pojemnosc`; 4 przy ścisłym oknie 1 s po G-4); przyjęcie planu przed falą 1 z przewidywanym końcem ≤ termin miękki, terminy fal ≥ 2 i pierwszeństwo fal ≥ 2 zastępują „oczekiwanie do `termin_miekki − 11 s`”; symulacja profili A–D (17.4) i S-6/S-6b przeliczone.
- **Uzupełnienie (it. 6, wersja 2.4, C4-05, C4-06 p. 8 i 9):** łączna pojemność kubełków `B_całk` (34 przy tolerancji A-01, 10 przy ścisłym oknie) dzielona między wiersze — `api` 1, pozostałe proporcjonalnie do wagi z podłogą — zamiast pojemności stałej per proces; przydział `r_i = min(formuła, 0,8 × limit − Σ_{j≠i} r_j)` i pojemność ograniczona resztą do `B_całk`, publikowane w kolumnach `przydzial` i `pojemnosc` i liczone pod blokadą doradczą klucza; nowy proces z pustym kubełkiem i odnowieniem co 2 s; granica chwilowa ≤ `B_całk` + 40 T niezależnie od liczby replik (2 / 3 / 4 workery: 74 / 72 / 74 w 1 s; wersja 2.3: 74 / 90 / 106); klucz rejestru `(konfiguracja_modelu_id, wykonawca, pula)` z wierszami R i S dla roli `ROZPOZNAWANIE_PISMA`; popyt 28 wywołań na Ocenę w budżecie, S-6 i 17.4 (29 = maksimum pytań). Odrzucona alternatywa: pojemność proporcjonalna do wagi bez wydzielenia `api` (`B_i = B_całk × w_i / Σ w`) — przy 2 workerach daje kubełek 15 < fala 16 i zmienia wyniki S-6 bez zysku dla granicy.

### ADR-040 Okres karencji usunięcia konta: procesy wstrzymane, nie anulowane — Nowa (it. 4)
- **Kontekst:** żądanie usunięcia natychmiast anulowało Weryfikacje i Rozwiązania, a przywrócenie w 30 dniach zostawiało trwale zawieszone Wyniki, zużyty limit zgłoszeń, Rozwiązania `ANULOWANE` z Wynikami wstępnymi i Kontrole pominięte w `π_wykonanie` (C2-08).
- **Decyzja:** w okresie karencji stany się nie zmieniają; handlery kończą się z `KONTO_USUWANE`, Kolejka weryfikacji pomija konta `USUWANE`, Kontrole tych kont są poza licznikiem i mianownikiem nadzoru; przywrócenie wznawia procesy (tabela 16.4: nowe `termin_at`, ponowne zlecenie rozpoznania, T-6 dla przerwanych ocen); anulowanie wykonuje `USUN_KONTO` w kroku 0. Scenariusz C-33.
- **Alternatywy:** anulowanie przy żądaniu i odtwarzanie procesów przy przywróceniu — odrzucone (odtworzenie Weryfikacji wymagałoby zachowania pełnego stanu sprzed anulowania, czyli i tak wstrzymania danych).
- **Konsekwencje:** przywrócenie jest bezstratne; przez 30 dni dane są przechowywane bez przetwarzania (art. 12 ust. 3 RODO — realizacja żądania w ciągu miesiąca).
- **Uzupełnienie (it. 5, wersja 2.3, C3-08):** rozstrzygnięcie i korekta wymagają konta Ucznia `AKTYWNE` (`409 KONTO_UCZNIA_NIEAKTYWNE`, szkic zachowany), więc w karencji nie powstaje Ocena Nauczyciela, Wynik zadania ani Próbka kalibracyjna — deklaracja „bez przetwarzania” nie ma wyjątków; przywrócenie zleca `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` dla Rozwiązań bez aktualnej kopii; dla kont Nauczycieli i Administratorów — osobna tabela przywrócenia (16.4) i odroczenie odebrania flag oraz usunięcia szkiców do `USUN_KONTO`.

### ADR-041 Rejestr wszystkich Zdarzeń RODO w Object Lock przed transakcją — Nowa (it. 4)
- **Kontekst:** do wersji 2.1 rejestr zawierał tylko wykonane usunięcia; odtworzenie PITR do punktu sprzed wniosku o usunięcie lub odwołania zgody przywracało konto `AKTYWNE` i zgodę `true` (C2-09).
- **Decyzja:** `api` zapisuje każde Zdarzenie RODO (`WNIOSEK_O_USUNIECIE`, `ANULOWANIE_USUNIECIA`, `USUNIECIE_WYKONANE`, `ODWOLANIE_ZGODY`, `UDZIELENIE_ZGODY`) do bucketu `rejestr-zdarzen-rodo` **przed** transakcją, a kopię do `zdarzenie_rodo`; `UZGODNIJ_REJESTR_RODO` unieważnia obiekty bez zatwierdzonej transakcji; `ponow_zdarzenia_rodo` stosuje zdarzenia po punkcie odtworzenia przed startem workerów. G-6 testuje trzy scenariusze.
- **Alternatywy:** wzorzec outbox (zapis w bazie, potem praca w tle do bucketu) — odrzucony, bo odtworzenie do chwili między zatwierdzeniem a wysyłką gubi zdarzenie; zapis do bucketu po transakcji — odrzucony z tego samego powodu.
- **Konsekwencje:** żądania RODO przeżywają odtworzenie bazy; niejednoznaczność (zdarzenie bez transakcji w oknie 10 min) jest rozstrzygana w kierunku ochrony danych.
- **Uzupełnienie (it. 5, wersja 2.3, C3-03, C3-07):** zdarzenia z inicjatywy użytkownika zapisuje `api` (`zrodlo = API`), a `USUNIECIE_WYKONANE` — `worker` (`zrodlo = WORKER`, deterministyczne `id`, obiekt przed pierwszą zmianą w bazie, wiersz `zdarzenie_rodo` w pierwszej transakcji `USUN_KONTO`); `UZGODNIJ_REJESTR_RODO` unieważnia wyłącznie zdarzenia `API` bez wiersza, a dla `WORKER` doprowadza konto do usunięcia (ponowne `USUN_KONTO` po 2 h, alert po 26 h); `ponow_zdarzenia_rodo` wykonuje kroki 1–6 synchronicznie niezależnie od czasu od usunięcia. Nazwy: `WNIOSEK_O_USUNIECIE` (zamiast `ZADANIE_USUNIECIA`), bucket `rejestr-zdarzen-rodo`, pojęcie „Rejestr Zdarzeń RODO”. Odrzucona alternatywa: unieważnianie także zdarzeń `WORKER` z wydłużonym oknem (np. 24 h) — każde okno skończone zostawia przypadek PITR wykonanego po oknie (C3-03 p. 3b).
- **Uzupełnienie (it. 6, wersja 2.4, C4-01, C4-06 p. 2 i 3):** w `ponow_zdarzenia_rodo` kryterium zastosowania zależy od źródła — `API`: wiersz `zdarzenie_rodo` o tym `id`; `WORKER`: brak wiersza `uzytkownik` (wiersz zdarzenia powstaje na początku usunięcia, więc PITR w trakcie `USUN_KONTO` dawał bazę z wierszem zdarzenia i danymi konta); prefiks `wykonane/` przeglądany w całości; ponowienie każdego zdarzenia przez te same funkcje domenowe co `api`/`USUN_KONTO`, według `dane.rola` (kroki Nauczyciela, skutki `KontoUsuwane` / `KontoPrzywrocone`); `rola` w każdym zdarzeniu (CHECK); klucz `USUNIECIE_WYKONANE` bez daty (`wykonane/{uzytkownik_id}/{id}.json`), więc `If-None-Match: *` działa przy ponowieniu w innym dniu; `UZGODNIJ_REJESTR_RODO` co 5 min — okno unieważniania ≤ 10 min zgodne z 13.3, 16.4 i G-6; G-6 (a3), (a4), scenariusz C-39. Odrzucona alternatywa: wiersz `zdarzenie_rodo` dla `USUNIECIE_WYKONANE` zapisywany w ostatniej transakcji (krok 5) — wtedy PITR po kroku 4 traci informację, że usunięcie się rozpoczęło, a obiekty zdjęć są już skasowane; kryterium „konto nie istnieje” jest poprawne przy każdym punkcie `T`.

### ADR-042 Filtr danych osobowych w Transkrypcji — Nowa (it. 4)
- **Kontekst:** Agent rozpoznający oznaczał ramki danych osobowych tylko do maskowania zdjęcia; nic nie zabraniało przepisania imienia i nazwiska do kroków, skąd trafiały do stanu Jev, promptów, obcego weryfikatora i kopii treningowej (C2-10).
- **Decyzja:** reguła promptu „nie przepisuj obszarów danych osobowych”; deterministyczny filtr w Lemat przed zapisem każdej Transkrypcji i przed kopią treningową: usunięcie kroków pokrywających się z ramkami w ≥ 50%, zastąpienie imienia, nazwiska i części lokalnej e-maila z konta (porównanie po stronie Lemat, odległość Levenshteina ≤ 1 na rdzeniu ≥ 4 znaków) oraz PESEL i e-maili znacznikiem `[DANE_OSOBOWE]`; metryka i test G-2/G-6 (0 wystąpień na ≥ 50 kartkach z danymi).
- **Alternatywy:** drugi model wykrywający dane osobowe w tekście — odrzucony (koszt, opóźnienie fazy R, niedeterministyczność); poleganie wyłącznie na prompcie — odrzucone (brak gwarancji).
- **Konsekwencje:** deklaracja 16.3 jest egzekwowana kodem; ryzyko pozostaje dla danych osób trzecich niewystępujących na koncie (np. imię kolegi) — pokrywa je reguła promptu i ramki maskowania.
- **Uzupełnienie (it. 5, wersja 2.3, C3-02):** filtr nie usuwa kroków i nie zmienia `latex`; krok w ramce danych osobowych bez treści matematycznej jest maskowany w całości (`tylko_dane_osobowe`), z treścią matematyczną — zachowany z `konflikt_filtra` → T-3 (Z-1); dopasowanie nazw tylko w `tekst`, z wykluczeniem Słownika terminów matematycznych; nazwy < 4 znaków — dopasowanie dokładne; pozycje zastąpień w `zastapienia_danych_osobowych` i przywracanie słowa Poprawką (dopasowania przybliżone nie są ponownie filtrowane); G-2: 150 kartek, 0 zastąpień terminów matematycznych, ≤ 0,5% fałszywych zastąpień. Odrzucona alternatywa: usuwanie kroku z możliwością przywrócenia — usunięcie przed zatwierdzeniem zmienia Ocenę bez Weryfikacji, gdy Uczeń nie zauważy braku.
- **Uzupełnienie (it. 6, wersja 2.4, C4-03):** w regule (a) tokeny imienia i nazwiska z konta, inicjały i oznaczenie klasy nie są treścią matematyczną, więc linia „Jan Pole” jest krokiem `tylko_dane_osobowe`, a nie `konflikt_filtra`; w regule (b) token Słownika równy nazwisku jest zastępowany przy sąsiedztwie z drugim członem imienia i nazwiska lub inicjałem (b1) albo w kroku agenta z podejrzeniem danych osobowych (b2); na Poprawkach reguła (a) nie jest liczona ponownie — krok przywrócony z `tylko_dane_osobowe` nie jest maskowany, podlega limitom 9.4, Sprawdzeniu zgodności poprawek i T-7; `konflikt_filtra` liczony wyłącznie z kroków niezmienionych; G-2: osobne kryterium dla kartek z nazwiskiem-terminem i metryka `uczniowie_z_konfliktem_filtra_udzial`. Odrzucona alternatywa: wyłączenie nazwisk-terminów ze Słownika dla konkretnego Ucznia (lista per konto) — zastępowałoby „Pole trójkąta” w każdym kroku tego Ucznia i zmieniało treść matematyczną w `tekst` poza ramką. Ryzyko resztkowe: samo nazwisko-termin bez imienia i poza ramką danych osobowych (np. podpis „Pole” niewykryty jako ramka) nie jest zastępowane — chroni je reguła promptu (R-19).

### ADR-043 SLA w dniach roboczych i tygodniowy model obciążenia z eskalacjami — Nowa (it. 4), G-0
- **Kontekst:** model 17.7 liczył obciążenie na 7 dni przy pracy Nauczycieli 5 dni i pomijał napływ eskalacji z Klas do kolejki `GLOBALNA`; kontrola przyjęć nie hamowała eskalacji; G-7 = 12 było spełnialne „na papierze” (C2-12).
- **Decyzja:** SLA = 3 dni robocze (`dodaj_dni_robocze`, tabela `dzien_wolny`); model tygodniowy (obciążenie dnia roboczego = 7/5 dobowego) z jawnym odsetkiem eskalacji 5% (A-15) i współczynnikiem 1,3; G-7 start = 18 (10 bez Klas + ≥ 8 współpracujących); `zaleglosc_globalna_min` z podziałem na źródło; kontrola przyjęć per Klasa przy `odsetek_eskalacji_7d ≥ 0,20`.
- **Alternatywy:** SLA 72 h kalendarzowe z akceptacją ryzyka poniedziałkowych eskalacji — odrzucona jako opcja domyślna (pierwszy tydzień pilotażu dałby lawinę `opozniona`); obowiązkowa praca weekendowa weryfikatorów — odrzucona (koszt, brak podstaw w umowach).
- **Konsekwencje:** termin dla Ucznia może wynosić do 5 dni kalendarzowych (Weryfikacja z piątku → środa); wyższy koszt puli globalnej; decyzja D-13 (G-0).
- **Uzupełnienie (it. 6, wersja 2.4, C4-06 p. 6 i 7):** eskalacje z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA` nie wliczają się do `odsetek_eskalacji_7d` (per Klasa i globalnie); Klasy Nauczyciela w karencji są objęte kontrolą przyjęć wprost do Przekazania Klasy albo przywrócenia; wariant 17.7 z aktywnym bezpiecznikiem poprawek (start: `M` = 3 951 min, 20 weryfikatorów; faza ustalona: 16) i decyzja D-17 o zapasie puli.

### ADR-044 Usuwanie kont Nauczycieli i Administratorów — Nowa (it. 4)
- **Kontekst:** brak przepływu i endpointu usunięcia konta Nauczyciela, a klucze obce `RESTRICT` blokowały `DELETE FROM uzytkownik`; dane Nauczyciela są danymi osobowymi (C2-14).
- **Decyzja:** `DELETE /nauczyciele/ja`, `POST /admin/uzytkownicy/{id}/usuniecie` (ochrona ostatniego Administratora); okres karencji 30 dni; natychmiastowe zwolnienie blokad, odebranie flag (od wersji 2.3 odroczone do `USUN_KONTO`), eskalacja Weryfikacji `KLASA`; Przekazanie Klasy przez Administratora; po karencji archiwizacja nieprzekazanych Klas i zamknięcie ich Przypisań, `WYCOFANE` dla nieprzekazanych Zadań `PRYWATNY`, pseudonimizacja uzasadnień `R-PRZYPISANIE`, klucze obce `ON DELETE SET NULL` (DDL 7.3), Oceny Nauczyciela bez wskazania osoby.
- **Alternatywy:** wyłączenie z zakresu (NFR-09 wymienia Uczniów) — odrzucone (RODO dotyczy wszystkich osób); usuwanie miękkie z zachowaniem danych — odrzucone (brak podstawy przetwarzania po ustaniu umowy).
- **Konsekwencje:** Uczniowie nie tracą historii Ocen; Klasa bez Nauczyciela istnieje tylko jako zarchiwizowana (CHECK w DDL).
- **Uzupełnienie (it. 5, wersja 2.3, C3-08):** natychmiastowe skutki karencji ograniczone do zwolnienia blokad (szkice zachowane) i eskalacji z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA`; flagi odbiera dopiero `USUN_KONTO`; przywrócenie cofa eskalacje oczekujące, Klasy przekazane pozostają u nowego Nauczyciela (D-15).

### ADR-045 Normalizacja zdjęć w piaskownicy procesowej `api` — Nowa (it. 4)
- **Kontekst:** dekodowanie HEIC/PNG/WEBP i zmiana rozmiaru do 5 × 10 MB odbywały się w procesie `api` bez limitów, wbrew deklaracji 16.2 (bomba dekompresyjna, blokowanie pętli zdarzeń w szczycie „koniec lekcji”, C2-15).
- **Decyzja:** `ProcessPoolExecutor` z 2 procesami na replikę `api`, `RLIMIT_AS` 512 MiB, `RLIMIT_CPU` 5 s, 8 s czasu ściennego na plik; odczyt nagłówka przed dekodowaniem, ≤ 12 000 px na wymiar i ≤ 40 Mpx (`MAX_IMAGE_PIXELS`), `422 ZDJECIE_NIEPRAWIDLOWE`; oczekiwanie na proces > 5 s → `503`; `run_in_executor` w pętli zdarzeń.
- **Alternatywy:** normalizacja w workerze przed `ROZPOZNAJ_PISMO` — odrzucona (zapis nieznormalizowanego oryginału z EXIF/GPS do S3 przed usunięciem metadanych, +0,4 s w fazie R na ścieżce krytycznej, osobna obsługa błędów formatu po `202`).
- **Konsekwencje:** wykorzystanie piaskownicy ≈ 9% w szczycie (17.4); błędne pliki odrzucane synchronicznie przed zapisem.


### ADR-046 Bezpieczniki trafności i poprawek reagujące na dowód przekroczenia — Nowa (it. 5)
- **Kontekst:** reguła „`n ≥ 50` i UB Wilsona > 2%” z wersji 2.2 była spełniona z konstrukcji dla 50 ≤ n < 189 przy zerze błędów (UB = z²/(n + z²)), więc na starcie produkcji i po awariach Jev podnosiła próg do 0,990, a bezpiecznik poprawek kierował każdą Poprawkę do Weryfikacji — sprzecznie z minimum `n_eff ≥ 189` z 11.5.1 i z modelem obciążenia 17.7 (C3-01).
- **Decyzja:** wyzwolenie wyłącznie przy dowodzie przekroczenia: dolna granica Wilsona 95% błędu (wagi IPW) > 2% przy `n_eff ≥ 50`; próba = Kontrole losowe z 30 dni dla pary (konfiguracja, rodzaj) z pewnością (bieżący kalibrator) powyżej bieżącego progu — nie dla rekordu progu; UB > 2% przy `n_eff ≥ 189` tylko blokuje obniżanie progu; stan w tabeli `stan_bezpiecznika` (zastępuje flagę środowiskową), dezaktywacja bezpiecznika poprawek przez Administratora (nazwa od wersji 2.4; w 2.3 „zdjęcie”) ustawia `okno_od`; testy jednostkowe dla n = 50 … 188 bez błędów i dla progów `k_min` (11.5.3).
- **Alternatywy:** SPRT (H0: e ≤ 1%, H1: e ≥ 3%, α = β = 0,05) — odrzucony jako opcja domyślna: wymaga stanu sekwencyjnego per para i resetu po każdej zmianie progu (powrót problemu zapadki), a przy oknie kroczącym traci własności testu; samo minimum `n_eff ≥ 189` przed UB — odrzucone: przy e = 0,5% UB > 2% utrzymuje się do n ≈ 335 (11.5.1, tabela 1), więc w przedziale 189–335 bezpiecznik nadal wyzwalałby się bez dowodu.
- **Konsekwencje:** przy e ≤ 0,5% fałszywe wyzwolenie w 90 dniach ≤ 0,2%, przy e = 1% ≤ 2,2% (symulacja 11.5.3); model z błędem 4% od początku okna wykrywany w 2–4 dni (model zapasowy: 12 dni), nagła degradacja 0,5% → 4% w fazie ustalonej — w medianie 13 dni (doprecyzowanie wersji 2.4: „wykrywanie” oznacza pierwsze podniesienie progu, a skuteczną reakcję gwarantuje Blokada obniżania progu z uzupełnienia it. 6); błąd 2–3% nie jest szybko wykrywany przez bezpiecznik — chroni przed nim reguła wyboru progu (UB ≤ 2% przy `n_eff ≥ 189`) i blokada obniżania; model 17.7 i G-7 = 18 pozostają ważne (scenariusz startowy 17.7).

- **Uzupełnienie (it. 6, wersja 2.4, C4-02, C4-04, C4-06 p. 5):** jawna kolejność dnia KALIBRUJ (kroki 1–6, 11.5.3): bezpieczniki oceniane na progu i kalibratorze sprzed przebiegu, wynik `p_nowy = max(p_dol, p0 + 0,02)` przy wyzwoleniu, jeden rekord `prog_pewnosci`; Blokada obniżania progu (`stan_bezpiecznika.aktywny` dla `TRAFNOSC`) od wyzwolenia do UB ≤ 2% przy `n_eff ≥ 189` powyżej bieżącego progu; obniżenie tylko po teście 30 dni powyżej progu docelowego (LB ≤ 2% i UB ≤ 2% przy `n_eff ≥ 189`); wyzwolenie przy 0,990 → tryb pełnej weryfikacji (1.000) z wyjściem przez dezaktywację (D-16); symulacja dynamiki (próg niemalejący w 100% przebiegów z degradacją, 61% przebiegów z degradacją jednorodną kończy w trybie pełnej weryfikacji w 30 dni); rozmiar testu przy e = 2% nazwany wprost (2,6–7,8% na ocenę, 1,1 fałszywego podniesienia na kwartał); „zdjęcie” → „dezaktywacja” dla obu typów. Odrzucone alternatywy: okno 7 dni obok 30 dni (wykrycie 7 zamiast 13 dni, ale 2,0 fałszywego podniesienia na kwartał i 28,5% kwartałów w trybie pełnej weryfikacji przy e = 2%); zatrzymanie na 0,990 bez trybu pełnej weryfikacji — przy degradacji jednorodnej błąd ocen automatycznych pozostaje > 2% bez końca (naruszenie NFR-03).

### ADR-047 Sprzątanie osieroconych obiektów magazynu — Nowa (it. 5)
- **Kontekst:** po odtworzeniu bazy z PITR obiekty kopii treningowych i zdjęć utworzone po punkcie odtworzenia zostawały w magazynie bez wierszy `kopia_treningowa` / `zdjecie_rozwiazania`, więc odwołanie zgody i `USUN_KONTO` nie mogły ich usunąć (do reguły 24 miesięcy) — przypadek odrzucony w ADR-035; sprzątanie z E-02 obejmowało tylko prefiks `zdjecia/` (C3-06).
- **Decyzja:** praca `SPRZATAJ_OSIEROCONE_OBIEKTY` dla bucketów `zbior-treningowy/`, `zdjecia/`, `zdjecia-maskowane/`: tryb `CODZIENNY` (04:30 UTC, obiekty starsze niż 1 h bez wiersza → `DELETE`) i tryb `ODTWORZENIE` (krok 2a procedury PITR przed startem workerów, obiekty nowsze niż `T − 5 min` bez wiersza → `DELETE`); metryka i alert; G-6 (d).
- **Alternatywy:** Zdarzenie RODO `KOPIA_TRENINGOWA_UTWORZONA` w Object Lock — odrzucone: ≈ 750 obiektów dziennie w rejestrze rozliczalności dla zdarzenia, które nie jest decyzją osoby, a i tak nie obejmuje zdjęć Rozwiązań; S3 Inventory zamiast listowania — odrzucone dla pilotażu (raport z opóźnieniem do 48 h nie spełnia kroku 2a), do ponownej oceny przy 20× A-03.
- **Konsekwencje:** każdy obiekt danych osobowych w magazynie ma wiersz w bazie albo zostaje usunięty w ≤ 25 h (tryb codzienny) lub przed wznowieniem przetwarzania (odtworzenie); koszt ≈ 1 900 wywołań LIST i ≈ 6 min pracy dziennie (16.4).

---

## 20. Odpowiedzi na otwarte pytania z architektury wstępnej

| Pytanie (architecture.md) | Odpowiedź | Gdzie |
|---|---|---|
| Jaki próg pewności przyjąć i jak go kalibrować? | Początkowo z ewaluacji ZT-1 (≥ 700 zadań obliczeniowych, analiza mocy 11.5.1); potem automatycznie: minimalny próg, przy którym górna granica Wilsona błędu ocen automatycznych ≤ 2% przy `n_eff ≥ 189`, z kalibracją out-of-fold, Kontrolą losową (termin 3 dni robocze, korekta wykonania w wagach IPW, podwójny odsetek dla Transkrypcji z Poprawkami), regułami nadzoru i bezpiecznikami reagującymi na dowód przekroczenia (dolna granica Wilsona > 2%, ADR-046). Gdy ZT-1 nie wystarcza — tryb pełnej weryfikacji. | 11.5, ADR-007, ADR-046 |
| Czy Jev radzi sobie z LaTeX w stanie? | Eksperyment **E-01** na ZT-1: trzy reprezentacje stanu (`LATEX`, `LINIOWA_SYMPY`, `OBA`); wybór reprezentacji z najwyższą Zgodnością Oceny i najniższym ECE; parametr `parametry.reprezentacja_wzorow` w Konfiguracji modelu. Domyślnie `OBA`. | 10.4.2, 13.1 |
| Całe rozwiązanie naraz czy każdy krok osobno? | Każde kryterium osobno + pierwszy błąd + typ błędu + wynikanie kroków + podejrzenie błędu odczytu, jako `PlanPytan` z zależnościami. | ADR-005, ADR-026 |
| Jak potraktować błędy odczytu? | Trzy linie obrony: zatwierdzenie i poprawki Transkrypcji przez Ucznia; decyzje Modelu decyzyjnego (Q-OC-04 `BLAD_ODCZYTU`, Q-OC-06) → T-3 z Oceną wstępną wstrzymaną i bez wpływu na Profil; zgłoszenie błędu odczytu przez Ucznia do 14 dni po Ocenie. Nauczyciel oznacza kroki z błędem odczytu; skuteczność mierzy 11.8. | 10.3, 11.1, 11.3, 11.6, 11.8, ADR-017, ADR-022 |
| Plan B dla Jev? | `ZapasowyLlmAdapter` za circuit breakerem, wykonujący cały plan pytań jednym wywołaniem, z osobną kalibracją; globalny ogranicznik szybkości kieruje nadmiar ruchu do modelu zapasowego bez otwierania obwodu (ADR-039); bramki G-1 i G-4 dla obu modeli. | ADR-009, ADR-026, 9.9 |
| Skąd zbiór odręcznych rozwiązań? | ZT-1 (A-09): klasy pilotażowe z pisemną zgodą (opiekunów prawnych dla niepełnoletnich), rozwiązania Arkuszy CKE pisane przez Uczniów, ocenione przez 2–3 Nauczycieli z oznaczeniem pierwszego błędnego kroku; uzupełnienie: rozwiązania z celowo wprowadzonymi błędami (także błędami trudnymi do odczytu — do pomiaru czułości T-3) pisane ręcznie przez wolontariuszy (≤ 30% zbioru). Dalsze dane: zgoda `zgoda_na_trening`. | 2, 11.5.1, 17.6 |

---

## 21. Ryzyka

| ID | Ryzyko | Prawdopodobieństwo / wpływ | Mitygacja |
|---|---|---|---|
| R-01 | Brak dostępu do Jev lub niska trafność na zapisie matematycznym | średnie / wysoki | model zapasowy jako pełnoprawna ścieżka; E-01; G-1 i G-4 dla obu modeli |
| R-02 | Zbyt duży odsetek Weryfikacji przeciąża Nauczycieli: model tygodniowy 17.7 — start 98 min na dzień roboczy na Nauczyciela prowadzącego Klasę (≈ 29 Weryfikacji, w tym Kontrola losowa), faza ustalona 75 min (≈ 20), kolejka globalna 888 min/dobę na starcie (z eskalacjami) | wysokie / średni | model obciążenia przeliczany co tydzień; G-7 (18 weryfikatorów globalnych); kontrola przyjęć per Klasa; priorytety i SLA 3 dni robocze (A-15, ADR-043); Opinia pomocnicza, szkice i przedłużanie blokad; automatyczne obniżanie progu; `udzial_zadan_otwartych`; kontrola przyjęć kolejki globalnej |
| R-03 | Jakość rozpoznawania pisma | średnie / wysoki | zatwierdzanie Transkrypcji, ponowienia i nowe przesłania, G-2 (w tym czułość T-3 i pomiar end-to-end), zgłoszenie błędu odczytu, alternatywny adapter OCR |
| R-04 | Pracochłonne przygotowanie Kryteriów oceniania, Umiejętności i Wkładek dla obu poziomów | wysokie / średni | format importu, szkice Wkładek i Zadań generowane przez Agenta generującego (także automatycznie przy E-20), recenzenci treści, G-5 |
| R-05 | Koszt modeli generatywnych | średnie / średni | limity 12.7 (w tym budżet dzienny generowania systemowego 150 PLN i 200 zleceń), metryka `koszt_modeli_pln_total{rola,systemowe}`, ograniczenie Opinii pomocniczej do `NISKA_PEWNOSC` |
| R-06 | Wstrzykiwanie poleceń przez treść kartki | niskie / wysoki | 16.2; decyzje z zamkniętą listą odpowiedzi; niespójności → Weryfikacja |
| R-07 | Obciążenie selekcją i braki odpowiedzi w kalibracji | średnie / wysoki | wagi IPW z korektą wykonania Kontroli, reguły źródła próbki 11.5.2, wyłączenie powodów nielosowych z populacji, reguły nadzoru 11.5.3 |
| R-08 | Przetwarzanie danych przez dostawców spoza EOG lub z długą retencją | średnie / wysoki | A-07 (retencja zero dla zdjęć), blokada aktywacji konfiguracji (`BRAK_RETENCJI_DOSTAWCY`), G-6, brak danych identyfikacyjnych w promptach |
| R-09 | Zimny start Profilu ucznia | wysokie / niski | Test diagnostyczny, propagacja w Dziale, ostrożna reguła Wkładki (wymaga dowodów słabości) |
| R-10 | ZT-1 nie pozwala wyznaczyć progu spełniającego NFR-03 | średnie / wysoki | analiza mocy 11.5.1 (≥ 700 zadań obliczeniowych); tryb pełnej weryfikacji z akceptacją w G-0 i automatycznym obniżaniem progu z danych produkcyjnych |
| R-11 | Brak akceptacji decyzji G-0 (PZS-1, A-11, ADR-022) przez właściciela produktu | niskie / wysoki | decyzje mają opisane alternatywy w ADR; zmiana dotyczy wyłącznie reguł domenowych (moduły `lemat.ocenianie`, `lemat.weryfikacja`), nie struktury systemu |
| R-12 | Brak Nauczycieli współpracujących do puli globalnej (A-14) | średnie / wysoki | plan awaryjny `LEMAT_REJESTRACJA_BEZ_KLASY = false` (ADR-034); kontrola przyjęć; cotygodniowe przeliczenie 17.7 — 10 Nauczycieli bez Klas (4 500 min tygodniowo) wystarcza przy udziale Uczniów bez Klasy `s` spełniającym 1,3 × 7 × M × (0,95 s + 0,05) ≤ 4 500: w fazie startowej s ≤ 8,8% (M = 3 702), w fazie ustalonej s ≤ 13,2% (M = 2 826) |
| R-13 | Nadużywanie Zgłoszeń błędu odczytu do czasowego usuwania słabych Ocen z Profilu | niskie / niski | limity 1 na Rozwiązanie i 5 na 30 dni; raport uznawalności per Uczeń (9.12); zawieszenie trwa najwyżej do rozstrzygnięcia (SLA 3 dni robocze) |
| R-14 | Uczeń zastępuje Transkrypcję gotowym rozwiązaniem (obejście kartki przez Poprawki) | średnie / wysoki | limity twarde i miękkie Poprawek, Sprawdzenie zgodności poprawek ze zdjęciem, T-7, podwójny odsetek Kontroli losowej i bezpiecznik poprawek (ADR-036, ADR-046); krok wstawiony ≤ 200 znaków i wliczony w odsetek znaków; G-2 (wykrywalność Poprawek „zastępujących” i „wstawień rozwiązania” ≥ 95%) |
| R-15 | Rytm tygodniowy i eskalacje przeciążają pulę globalną w poniedziałki | średnie / średni | SLA w dniach roboczych, model tygodniowy z eskalacjami, kontrola przyjęć per Klasa, G-7 = 18 (ADR-043) |
| R-16 | Odtworzenie bazy cofa Zdarzenia RODO | niskie / wysoki | rejestr wszystkich Zdarzeń RODO w Object Lock zapisywany przed transakcją, `ponow_zdarzenia_rodo` przed startem workerów, uzgadnianie co 5 min (okno ≤ 10 min, ADR-041); `USUNIECIE_WYKONANE` zapisywane przez `worker` i nigdy nieunieważniane, `SPRZATAJ_OSIEROCONE_OBIEKTY` w kroku 2a (ADR-041, ADR-047); od wersji 2.4 kryterium „konto nie istnieje” dla zdarzeń `WORKER` i ponowienie według roli konta (C-39); G-6 (4 scenariusze, w tym (a3) PITR w trakcie `USUN_KONTO` i (a4) konto Nauczyciela) |
| R-17 | Dostawca Jev bez trybu wsadowego lub z niższym limitem szybkości | średnie / średni | tryb `POJEDYNCZY` z budżetem 3 fal, globalny ogranicznik bez otwierania obwodu, model zapasowy dla nadmiaru (ADR-039); G-4 (d) |
| R-18 | Zgłoszenia błędu w Zadaniu nierozpatrywane przez recenzentów | średnie / niski | SLA 5 dni roboczych z metryką, wstrzymanie w doborze po 3 zgłoszeniach, alert dla Administratorów (ADR-038) |
| R-19 | Filtr danych osobowych zastępuje tokeny matematyczne albo przepuszcza krótkie imiona lub nazwiska równe terminom matematycznym | średnie / średni | Słownik terminów matematycznych, filtr tylko w `tekst`, dopasowanie dokładne dla nazw < 4 znaków, `konflikt_filtra` → T-3 zamiast usuwania kroku, przywracanie słowa Poprawką; od wersji 2.4 imię i nazwisko nigdy nie są treścią matematyczną, reguły b1/b2 dla nazwisk-terminów, metryka `uczniowie_z_konfliktem_filtra_udzial`; G-2 na 150 kartkach (w tym ≥ 30 z nazwiskiem-terminem, osobne kryterium) i 1 050 Transkrypcjach z syntetycznymi danymi konta (ADR-042); ryzyko resztkowe: samo nazwisko-termin bez imienia poza ramką — reguła promptu |
| R-20 | Bezpiecznik nie wykrywa umiarkowanego wzrostu błędu (2–3%) ani szybko nagłej degradacji (mediana 13 dni dla 0,5% → 4% w fazie ustalonej) | średnie / średni | reguła wyboru progu (UB ≤ 2% przy `n_eff ≥ 189`, okno 90 dni), ostrzeżenie z blokadą obniżania progu przy UB > 2%, Blokada obniżania progu po wyzwoleniu i test obniżenia na 30 dniach (wersja 2.4, C4-02), alert `gorna_granica_bledu_30d > 0,02`, cotygodniowy przegląd `/admin/kalibracja` (ADR-046); okno 7 dni odrzucone z powodu kosztu na granicy NFR-03 (11.5.3) |
| R-21 | Dostawca Jev liczy limit w ścisłym oknie 1 s i odrzuca chwilowe przekroczenia; skalowanie liczby replik zwiększa ruch chwilowy | średnie / niski | test G-4 (seria 74 wywołań), `B_całk = 10` zamiast 34 (kubełek workera 4 przy 2 replikach, +1,1 s w trybie pojedynczym, 17.2), `429` poza circuit breakerem (ADR-039); od wersji 2.4 łączna pojemność i przydział dzielone między repliki z niezmiennikami pod blokadą doradczą — granica ≤ 74 w 1 s przy 2/3/4 workerach, także przy dołączaniu (13.2, C-40, G-3 c) |
| R-22 | Bezpiecznik trafności na granicy NFR-03 (e ≈ 2%) fałszywie podnosi próg i przełącza parę w tryb pełnej weryfikacji | średnie / średni | koszt nazwany i policzony (1,1 podniesienia na kwartał, 11% kwartałów w trybie pełnej weryfikacji, auto 57,5% zamiast 67%, 11.5.3, 17.7); decyzja D-16 w G-0; wyjście z trybu pełnej weryfikacji przez dezaktywację Administratora po analizie `/admin/kalibracja`; plan obciążenia „Tryb pełnej weryfikacji” (16 weryfikatorów ≤ G-7) |

---

## 22. Parametry systemu (wartości domyślne)

Parametry przechowywane w kodzie (`lemat/konfiguracja.py`, nadpisywalne
zmiennymi środowiskowymi `LEMAT_*`), poza progami, kalibratorami i
konfiguracją modeli (baza danych).

| Parametr | Wartość | Sekcja |
|---|---|---|
| Limit zdjęć na przesłanie / rozmiar | 5 / 10 MB; JPEG, PNG, HEIC, WEBP | 12.5 |
| Maks. przesłań zdjęć / ponowień rozpoznania (Uczeń) | 3 / 5 | 8.1 |
| Próby techniczne pracy `ROZPOZNAJ_PISMO` | 3 (odstęp 1 s, 3 s) | 13.3 |
| Timeout rozpoznawania | 14 s na wywołanie; cel p95 9 s | 17.2 |
| Maks. kroków Transkrypcji | 40 | 10.3 |
| Próg „sprawdź odczyt” | `pewnosc_odczytu < 0.80` | 10.3, 11.1 |
| τ_odczyt (podejrzenie błędu odczytu) | 0.20 | 11.1 p. 7 |
| τ_FR25 (reguła T-2) | 0.20 | 11.3 |
| Maks. par wynikania sprawdzanych pytaniem Q-OC-05 | 12 (więcej → N-6) | 10.4.3 |
| Termin miękki / twardy oceny | `max(10 s, 28 s − t_R)` / 120 s od zatwierdzenia | 14.2 |
| SLO NFR-01 | ≥ 99% `czas_systemowy_ms ≤ 28 000`; p95 ≤ 20 s (Jev) / ≤ 23 s (zapasowy); T-6 ≤ 0,2% | 17.1 |
| Jev timeout / ponowienie / współbieżność | 2 s na żądanie / 1 tylko przy ≥ 13 s do terminu miękkiego / tryb wsadowy: 1 żądanie na rundę; tryb pojedynczy: 16 na Ocenę, limit procesu z globalnego ogranicznika (ADR-039) | 13.2 |
| Circuit breaker | otwarcie: 3 kolejne błędy lub ≥ 50% błędów w 20 s przy ≥ 6 wywołaniach; półotwarty po 60 s | 13.2 |
| Model zapasowy timeout | 9 s | 13.2 |
| Q-RE-01 timeout | 1,5 s, bez ponowień | 10.1.6 |
| Agent wyjaśniający | 8 s, ≤ 400 znaków, asynchronicznie | 10.5 |
| SymPy | 2 s CPU / para, 3 s / Rozwiązanie, 512 MiB | 13.1, 17.2 |
| Pule workera (na replikę) | R 15 / S 4 / O 15 / P 6 / W 4 korutyn; 4 procesy CPU | 13.4 |
| Odpytywanie klienta | 1 s w stanach przejściowych | 12.1 |
| Próg początkowy Jev / zapasowy (tylko środowisko testowe, przed ZT-1) | 0.95 / 0.98 | 11.2.2 |
| Zakres progu | 0.800–0.990 (dowód i tryb pełnej weryfikacji: 1.000) | 11.5 |
| Odsetek Kontroli losowej | 0.20 do 400 próbek, potem 0.05 | 11.5.3 |
| Min. próbek do kalibracji / n_eff powyżej progu | 400 / 189 | 11.5.3 |
| Cel błędu auto (górna granica Wilsona 95%) | 0.02 | 11.5.3 |
| Walidacja krzyżowa kalibratora | 5-krotna, wartości out-of-fold | 11.5.3 |
| Bezpieczniki trafności i poprawek (ADR-046) | wyzwolenie: LB Wilsona 95% błędu > 0,02 przy `n_eff ≥ 50`; ostrzeżenie (blokada obniżania progu): UB > 0,02 przy `n_eff ≥ 189`; okno 30 dni dla pary (konfiguracja, rodzaj) powyżej progu sprzed przebiegu; krok +0.02 na dobę od progu sprzed przebiegu (maks. 0.990; wyzwolenie przy 0.990 → 1.000, D-16); Blokada obniżania progu: wejście przy wyzwoleniu, wyjście przy UB ≤ 0,02 i `n_eff ≥ 189` albo przez dezaktywację; test obniżenia: okno 30 dni powyżej progu docelowego, `n_eff ≥ 50`, LB ≤ 0,02 i (n_eff < 189 lub UB ≤ 0,02) | 11.5.3 |
| Progi wykonania Kontroli (alert / blokada obniżania / bezpiecznik) | 0.90 / 0.80 / 0.60 | 11.5.3 |
| Strefa rozwoju (P sukcesu) | [0.60, 0.75], cel 0.675 | 10.1.2 |
| Próg Wkładki / opanowania / wymagań | 0.40 / 0.80 / 0.50 | 10.1.2 |
| Seria: maks. elementów / porażek / dni; poziom sukcesu | 8 / 3 / 7; `ulamek ≥ 0.7` przy Poziomie trudności ≥ 4 | 10.1.2 |
| Udział zadań otwartych | co 3. element zadaniowy; co 5. przy kontroli przyjęć lub trybie pełnej weryfikacji | 10.1.2, 11.4.3 |
| Interwały powtórek | 3, 7, 14, 30, 60 dni | 10.1.3 |
| Wagi Wyniku zadania | `w_status` 1.0 / 0.5 (wstępna) / brak (wstrzymana); `w_proba` 1.0 / 0.5 | 10.1.3 |
| Kalibracja Poziomu trudności | `K_z = max(0.05, 0.4/(1+m/30))`, przeliczanie poziomu od 30 wyników, histereza 0.1 | 10.1.3 |
| Minimalna pula Zadań | 3 na (Umiejętność, Poziom trudności, poziom matury, rodzaj) | 10.1.2 |
| Budżet generowania systemowego | 200 zleceń i 150 PLN na dobę; alert przy 80% | 12.7 |
| Procent grupy eksperymentu JEV_CHOICE | 0 (pilotaż: 20 od 4. tygodnia) | 10.1.6 |
| Ważność elementu `PROPONOWANY` / `ROZPOCZETY` | 24 h / Zadanie 7 dni, Wkładka 24 h | 8.5 |
| Porzucenie niezatwierdzonego Rozwiązania | 7 dni | 8.1 |
| Blokada Weryfikacji / przedłużenie / maks. łącznie | 15 min / co 5 min przez PWA / 2 h | 11.4.2 |
| SLA Weryfikacji / eskalacja / alert przed usunięciem zdjęć | 3 dni robocze (`dodaj_dni_robocze`) / co 1 h / 80 dni | 11.4.3, A-15 |
| Kontrola przyjęć per Klasa (eskalacje) | włączenie: `odsetek_eskalacji_7d ≥ 0,20` przy ≥ 10 Weryfikacjach (licznik bez `KARENCJA_KONTA_NAUCZYCIELA`) albo konto Nauczyciela `USUWANE`; wyłączenie: < 0,10 albo przywrócenie / Przekazanie Klasy; alert globalny > 0,10 | 11.4.3 |
| Model obciążenia: współczynnik zapasu / dni pracy / odsetek eskalacji | 1,3 / 5 dni roboczych / 5% | 17.7, A-15 |
| Poprawki Transkrypcji — limity twarde | bez usuwania; ≤ 2 wstawione, każdy ≤ min(200, max(60, ⌈1,5 × mediana długości kroków agenta⌉)) znaków; zmienione + wstawione ≤ max(3, ⌈0,5 n⌉); odsetek znaków (z krokami wstawionymi) ≤ 0,50 | 9.4, ADR-036 |
| Poprawki Transkrypcji — limity miękkie (T-7) | zmienione + wstawione ≤ max(2, ⌈0,25 n⌉); odsetek znaków ≤ 0,25; `p_zgodna_z_poprawka ≥ 0,50` | 9.4, 10.3.3 |
| Sprawdzenie zgodności poprawek | pula `S` (4 korutyny na replikę); timeout 8 s, 2 próby; ≤ 6 wycinków + zdjęcie poglądowe 1024 px; autozapis PWA 2 s po edycji; oczekiwanie w Ocenie do `zatwierdzono_at + 6 s` niezależnie od `t_R` (C4-06 p. 10) | 9.4, 10.3.3 |
| Odsetek Kontroli losowej dla Transkrypcji z Poprawkami | 2 × odsetek bazowy (maks. 1) | 11.3 T-5 |
| Jev: tryb / wsad / równoległość w trybie pojedynczym | `WSADOWY` (wymaganie A-01) albo `POJEDYNCZY` / ≤ 32 pytania / 16 na Ocenę | 13.2 |
| Globalny ogranicznik szybkości modelu | `r_i = min(0,8 × limit × waga_i / Σ wag, 0,8 × limit − Σ_{j≠i} r_j)`; wagi: worker 1,0, `api` 0,1; łączna pojemność kubełków `B_całk` (`LEMAT_POJEMNOSC_LACZNA_KUBELKOW`): 34 (tolerancja A-01), 10 (ścisłe okno 1 s, G-4); pojemność `api` 1, pozostałe ⌊(B_całk − n_api) × w_i / Σ w⌋ (worker 16 / 10 / 8 przy 2 / 3 / 4 replikach); nowy wiersz z pustym kubełkiem, odnowienie co 2 s do osiągnięcia formuły; 1 token = 1 wywołanie; fale ≥ 2 z pierwszeństwem; `t_fali` 1,2 s (pojedynczy) / 0,8 s (wsadowy); odnowienie 10 s pod `pg_advisory_xact_lock`, wygasanie 30 s; po `429` przydział × 0,5 na 30 s; rozpoznawanie: 0,8 × 150 = 120/min, `B_całk = 30`, wiersze R (1,0) i S (0,3) na replikę | 13.2, 13.4, ADR-039 |
| Popyt Jev na Ocenę (tryb pojedynczy) | 28 wywołań (plan budżetowy 16 + 11 + 1, 3 fale); maksimum pytań 29 (16 + 13, 2 fale) | 10.4.3, 17.2, 17.4 |
| Normalizacja zdjęć w `api` | 2 procesy na replikę; `RLIMIT_AS` 512 MiB, `RLIMIT_CPU` 5 s, 8 s czasu ściennego; ≤ 40 Mpx, ≤ 12 000 px na wymiar; oczekiwanie > 5 s → `503` | 16.2, ADR-045 |
| Pula Testu diagnostycznego | ≥ 3 Zadania na (Dział, poziom matury, nominalny Poziom trudności 2/4); test niedostępny < 50% pozycji | 9.1, G-5 |
| Zgłoszenia błędu w Zadaniu | 10 / dobę na Nauczyciela; wstrzymanie w doborze przy 3 otwartych od różnych Nauczycieli; SLA 5 dni roboczych; Przegląd recenzentów po 14 dniach bez Przeglądu zlecającego | 8.4, ADR-038 |
| Automatyczne szkice Wkładek | ≤ 20 / dobę w budżecie 12.7; wstrzymanie na 30 dni po 2 odrzuceniach | 10.2.1, 13.3 |
| Okres karencji usunięcia konta | 30 dni, procesy wstrzymane | 16.4, ADR-040 |
| Zamiatacz Profili | co 5 min, Wyniki starsze niż 2 min, 500 Uczniów na przebieg; alert > 300 s | 13.3 |
| Kontrola przyjęć kolejki globalnej | włączenie: zaległość > 2× pojemność; wyłączenie: < 1× | 11.4.3 |
| Weryfikator globalny: minimalny deklarowany czas | 90 min na dzień roboczy | A-13 |
| Priorytet Weryfikacji przy zdjęciach ≥ 80 dni | 1 | 11.4.3 |
| Zgłoszenie błędu odczytu | okno 14 dni; 1 na Rozwiązanie; 5 na 30 dni | 11.6 |
| Kod zaproszenia | 8 znaków `[A-HJ-NP-Z2-9]`, ważny 14 dni | 7.3 |
| Retencja zdjęć | 30 dni po `OCENIONE` bez otwartej Weryfikacji, maks. 90 dni | 16.4 |
| Rejestr Zdarzeń RODO (Object Lock) | 60 dni | 16.4 |
| Kopie treningowe | maks. 24 miesiące; usunięcie obiektów ≤ 1 h po odwołaniu zgody lub w `USUN_KONTO` | 16.4 |
| Rozpoznawanie: dopuszczalny odsetek błędów/timeoutów dostawcy | ≤ 0,3% wywołań | A-08, 17.2 S-7 |
| Weryfikator globalny prowadzący Klasę: maks. łączne obciążenie | 180 min na dzień roboczy (prognoza Klasy na dzień roboczy, 7/5 dobowej + deklaracja) | A-14 |
| Filtr danych osobowych | tylko `tekst`; rdzeń ≥ 4 znaki: dokładnie lub Levenshtein ≤ 1; < 4 znaki: dokładnie; Słownik terminów matematycznych (≈ 4 000 form) niezastępowany poza regułami b1 (sąsiedztwo drugiego członu lub inicjału) i b2 (krok agenta w ramce); krok bez treści matematycznej (po wyłączeniu imienia, nazwiska, inicjałów i oznaczenia klasy) ≤ 6 tokenów → `[DANE_OSOBOWE]` w całości; na Poprawkach reguła (a) nie jest liczona ponownie | 10.3.1, ADR-042 |
| Sprzątanie osieroconych obiektów | codziennie 04:30 UTC, obiekty starsze niż 1 h; przy odtworzeniu: od `T − 5 min`; alert > 100 obiektów na dobę | 13.3, 16.4, ADR-047 |
| Uzgadnianie Rejestru Zdarzeń RODO | cykl co 5 min; unieważnianie zdarzeń `API` bez wiersza starszych niż 5 min (okno ≤ 10 min); `USUNIECIE_WYKONANE` (klucz bez daty `wykonane/…`): ponowne `USUN_KONTO` po 2 h, alert po 26 h | 13.3, 13.5, ADR-041 |
| Rejestracja Ucznia bez kodu zaproszenia | `LEMAT_REJESTRACJA_BEZ_KLASY` = true (false, gdy G-7 niespełnione) | A-14 |
| Raport nadużyć zgłoszeń błędu odczytu | uznane < 20% przy ≥ 5 zgłoszeniach Ucznia | 9.12 |
| Dzierżawa pracy / odnowienie | 60 s / 15 s | 13.4 |
| JWT / refresh | 15 min / 30 dni | 16.1 |

---

## 23. Changelog iteracji

### 23.1 Iteracja 1 (2026-10-31)

Utworzenie dokumentu na podstawie `vision.md`, `glossary.md`,
`requirements.md`, `architecture.md`. `docs/CRITIQUE_LOG.md` nie istniał —
brak punktów krytyki do zaadresowania.

- Dodano: język wszechobecny z mapowaniem słownika na identyfikatory oraz
  listę pojęć uzupełniających do dopisania do słownika (sekcja 3).
- Dodano: widoki kontekstu, kontenerów, komponentów i wdrożenia (Mermaid).
- Dodano: pełne DDL PostgreSQL, typy wyliczeniowe, ograniczenia, indeksy
  częściowe, schematy JSONB, ERD.
- Dodano: maszyny stanów Rozwiązania, Oceny, Weryfikacji, Zadania, elementu
  Ścieżki nauki.
- Dodano: 11 diagramów sekwencji dla wszystkich głównych przepływów.
- Dodano: kontrakty agentów, katalog pytań do Jev, reguły punktacji,
  pewności, niespójności i trasowania (T-1…T-6), algorytm progu (Wilson + IPW).
- Dodano: katalog endpointów REST, typy DTO, model błędów, idempotencję,
  uprawnienia na poziomie obiektów, format importu Arkuszy CKE.
- Dodano: porty modeli, kontrakt adaptera Jev, katalog prac w tle,
  mechanikę kolejki, harmonogram, zdarzenia domenowe.
- Dodano: macierz błędów (25 pozycji), scenariusze współbieżności (22),
  bezpieczeństwo, retencję RODO, budżet NFR-01, obserwowalność, bramki G-1…G-6.
- Decyzje sprzeczne z architekturą wstępną: druga ocena LLM nie może
  zaliczyć rozwiązania poniżej progu (ADR-008, wymóg FR-24).
- Rozszerzenia względem wymagań (do potwierdzenia przez właściciela
  produktu): powody Weryfikacji `PODEJRZENIE_BLEDU_ODCZYTU`,
  `KONTROLA_LOSOWA`, `BLAD_PRZETWARZANIA`; kolejka globalna (ADR-021);
  zatwierdzanie kont Nauczycieli przez Administratora.

### 23.2 Iteracja 2 (2026-10-31, wersja 2.0)

Zaadresowano wszystkie 29 uwag z `docs/CRITIQUE_LOG.md`, iteracja 1
(2 krytyczne, 10 wysokich, 14 średnich, 3 niskie). Żadna uwaga nie została
odrzucona w całości; odstępstwa od proponowanej poprawki są opisane w
kolumnie „Zmiany” i w ADR (C1-02: brak automatycznej finalizacji po SLA;
C1-08: zobowiązanie 99% zamiast 100%; C1-17: wyjątek synchroniczny zamiast
pracy w tle; C1-20: wersja zapisana przy rozpoczęciu zamiast okna 24 h).

| ID uwagi | Zmiany | Sekcje dokumentu |
|---|---|---|
| C1-01 (KRYTYCZNA) | Q-OC-04 = `BLAD_ODCZYTU` nie zeruje punktów i nie ustala `typ_bledu` (nowa kolumna `krok_podejrzany_odczyt`); Ocena wstępna z podejrzeniem odczytu jest „wstrzymana” dla Ucznia (bez punktów, typu i Wskazówki) i nie tworzy Wyniku zadania; T-3 opiera się na decyzjach Modelu decyzyjnego (Q-OC-04 z τ_odczyt = 0.20, nowe Q-OC-06), a `pewnosc_odczytu` tylko dodaje powód; endpoint `POST /rozwiazania/{id}/zgloszenie-bledu-odczytu` z powodem `ZGLOSZENIE_BLEDU_ODCZYTU`, oknem 14 dni i limitami; metryki `bledy_odczytu_niewykryte_30d`, `czulosc_t3` powiązane z G-2; Z-1 przeformułowana (sygnały LLM działają tylko w kierunku bezpiecznym) | 1.2, 3.3, 7.3, 8.2, 9.5, 9.12, 10.3, 10.4, 11.1, 11.3, 11.6, 11.8, 12.3, 12.4, 12.6, 17.6, ADR-004, ADR-017, ADR-018, ADR-022 |
| C1-02 (KRYTYCZNA) | Bramka G-7 (min. 12 weryfikatorów globalnych z modelu obciążenia); `deklarowane_minuty_weryfikacji`; SLA 72 h (`termin_at`), eskalacja KLASA → GLOBALNA → alert Administratora; status „opóźniona” dla Ucznia; kontrola przyjęć (mniej zadań otwartych dla Uczniów bez Klasy przy przeciążeniu); pseudonim HMAC, brak Klasy i zdjęcia maskowane dla obcego weryfikatora; podstawa prawna i informacja przy rejestracji; pełny model obciążenia z Kontrolą losową. Odrzucono automatyczną finalizację Oceny wstępnej po SLA (łamie FR-24/FR-26) — uzasadnienie w ADR-021 | 2 (A-12, A-13), 7.3, 10.3, 11.4, 12.3, 12.4, 12.8, 14.1 (E-28), 16.3, 17.6, 17.7, 21, ADR-021, ADR-031 |
| C1-03 (WYSOKA) | Formalna propozycja zmiany słownika PZS-1 (części A–C) z bramką G-0; Transkrypcja tylko ze zdjęcia (+ poprawki Ucznia); Weryfikacja — zmiana definicji; „Model System One” mapowany na `JevAdapter`, port = „Model decyzyjny”; `kalibrator` wydzielony z `prog_pewnosci` do `kalibrator_pewnosci`; `uzasadnienie` w `OcenaWyjscie`; `POST /zadania` tylko dla Nauczyciela; Powtórka jako `cel_elementu`, nie typ; elementy z Przypisań wyłącznie z reguły agenta | 1.2 (Z-6), 3.2, 3.3, 3.4, 7.3, 11.7, 12.3, 12.4, ADR-016, ADR-028, ADR-032 |
| C1-04 (WYSOKA) | Reguła „jedno słowo — jedno znaczenie” i lista terminów technicznych; zmiany nazw: `DecyzjaSkladowa`, `RezultatPytan`, `decyzja_modelu.wybor`, `procent_grupy_eksperymentu`, `nr_kryterium`/`wymaga_nr_kryterium`, `RezultatOceny`, `RezultatRozpoznawania`, `Rownowaznosc`, `rezultat_walidacji`, `wynik_koncowy_wzorcowy`, `typ_kolejki_weryfikacji`/`typ_kolejki` | 3.1, 3.3, 7.3, 7.5, 10, 12.4, 12.9, 13.1, ADR-016 |
| C1-05 (WYSOKA) | CHECK zmieniony na `ocena_weryfikowana_id IS NOT NULL OR 'BLAD_PRZETWARZANIA' = ANY(powody)`; pełna semantyka `PONOW_OCENE` (dwie trasy, Wynik zadania, powiadomienia, `ponowna_ocena_at`, `powod_anulowania`); warunek `OCZEKUJE` + zapis warunkowy na `weryfikacja.version`; scenariusz C-23 | 7.3, 8.1, 8.3, 9.13, 13.3, 14.2, 15.2 |
| C1-06 (WYSOKA) | Reguła odczytu „dostępna” + praca schedulera `ZWOLNIJ_BLOKADY_WERYFIKACJI`; eskalacja obejmuje wygasłe blokady; `POST /weryfikacje/{id}/przedluzenie` i idempotentne podjęcie przez właściciela (≤ 2 h, `podjeta_at`); tabela `szkic_rozstrzygniecia` i `PUT …/szkic`; poprawiony SQL podjęcia z `id = :id` i nawiasami | 7.3, 8.3, 9.6.1, 11.4.2, 12.3, 13.5, 15.2 (C-07, C-08), ADR-033 |
| C1-07 (WYSOKA) | `PlanPytan` z zależnościami; model zapasowy wykonuje cały plan jednym wywołaniem (pytania warunkowe zwinięte); Jev w 2 rundach; reguła ponowienia zależna od zapasu do terminu; skrócone timeouty (Jev 2 s, zapasowy 9 s); Wskazówka poza ścieżką krytyczną; tabela najgorszych przypadków S-1…S-6; G-4 z trzema scenariuszami (w tym timeouty przy zamkniętym obwodzie) | 9.5, 9.9, 10.4.3, 10.4.4, 13.1, 13.2, 17.2, 17.6, 22, ADR-009, ADR-026 |
| C1-08 (WYSOKA) | NFR-01 mierzone jako czas systemowy (rozpoznanie + ocena); T-6 i błędy dostawcy liczone jako naruszenia; SLO `odsetek_t6 ≤ 0,2%`, metryka end-to-end; termin twardy 120 s zamiast 28 s; A-11 przekazane do akceptacji w G-0. Odstępstwo: zobowiązanie 99% (nie 100%) z uzasadnieniem w A-11/ADR-025 | 2 (A-11), 7.3, 9.5, 14.2, 17.1, 17.2, 17.6, ADR-025 |
| C1-09 (WYSOKA) | Analiza mocy (tabela n_eff dla e = 0…1%); ZT-1 ≥ 700 obliczeniowych + ≥ 250 dowodów; `n_eff ≥ 189` w regule; kalibracja 5-krotną CV (out-of-fold zamiast 80/20); tryb pełnej weryfikacji jako jawne kryterium startowe; G-2 z pomiarem end-to-end; poprawka A-09 („oba rodzaje”) | 2 (A-09), 11.5.1, 11.5.3, 17.1, 17.6, 21 (R-10), ADR-007 |
| C1-10 (WYSOKA) | Kontrola losowa: priorytet 3 (jak zwykłe Weryfikacje), `termin_at` 72 h, zdjęcia zachowane do zamknięcia; metryka `wykonanie_kontroli_30d` i alert; reguły nadzoru (blokada obniżania progu, bezpiecznik braku nadzoru); wagi IPW skorygowane o wykonanie per koszyk; Kontrola wliczona w model 17.7 | 11.3, 11.5.3, 14.1 (E-29), 16.4, 17.7, ADR-007, ADR-033 |
| C1-11 (WYSOKA) | Usunięto `theta_poczatkowa`; odtwarzanie całego Profilu od stanu DOMYŚLNEGO z Wynikami Testu diagnostycznego jako zdarzeniami (`wynik_zadania.test_diagnostyczny_id`); test raz na poziom matury (`ux_test_jeden`, `TEST_JUZ_WYKONANY`); zakończenie testu przez `AKTUALIZUJ_PROFIL` (202 + `oczekujace_wyniki`); zdefiniowany skutek zmiany poziomu matury | 7.3, 9.1, 10.1.3, 12.3, 12.4, 13.6, ADR-010 |
| C1-12 (WYSOKA) | `decyzja_modelu.uczen_id` + indeks; `USUN_KONTO` zeruje `stan` we wszystkich kontekstach; rejestr usunięć w buckecie z Object Lock (poza bazą); wyłączone wersjonowanie i reguła cyklu życia bucketów; retencja u dostawców w A-07, walidacja przy aktywacji i G-6; C-20 ustawia `usun_po := now()` | 2 (A-07), 7.3, 13.1, 15.2 (C-20), 16.4, 17.6, ADR-019 |
| C1-13 (ŚREDNIA) | T-2: (SymPy `ROWNOWAZNE` lub p_tak(Q-OC-01) ≥ τ_FR25) i (błędny/brakujący krok lub p(błąd) ≥ τ_FR25); τ_FR25 = 0.20 z uzasadnieniem i w parametrach | 11.3, 22 |
| C1-14 (ŚREDNIA) | Tryby Wskazówki przy rozstrzygnięciu (`WLASNA`/`AUTOMATYCZNA`/`ZACHOWAJ` tylko przy identycznym błędzie); Wskazówka automatyczna także przy `BLAD_PRZETWARZANIA`; reguła N-1 (brakujący etap + `NIEPELNE_ROZWIAZANIE` + Wskazówka cytująca kryterium); `uzasadnienie` w DTO z szablonem | 9.6.2, 10.5, 11.1 p. 6, 11.7, 12.4 |
| C1-15 (ŚREDNIA) | Limit 12 par Q-OC-05, nadmiar → N-6 i Weryfikacja; zachowanie przy `ZADEN` (wariant o najwyższym p, `wariant` NOT NULL); N-1/N-2 liczone na `punkty_surowe` przed FR-20 (kolejność reguł) | 7.3, 10.4.3, 11.1, 11.2.1, ADR-005 |
| C1-16 (ŚREDNIA) | Zgodność Oceny = punkty + pierwszy błędny krok + brakujący krok (kolumna generowana); reguły przypisania `zrodlo_probki`; `KOREKTA_NAUCZYCIELA` wykluczona z progu; deduplikacja `ux_probka_ocena_auto` | 3.3, 3.4 (B-5), 7.3, 11.5.2, 17.1, ADR-027 |
| C1-17 (ŚREDNIA) | Jawny wyjątek synchroniczny: fasada Orkiestratora w `api`, timeout Q-RE-01 1,5 s, circuit breaker, powrót do POLITYKA_ELO; krawędź `api → Jev` w 5.1; moduły `lemat.rekomendacje`, `lemat.powiadomienia`, `lemat.audyt`, `lemat.http` w 6.1/6.2; właściciel `klucz_idempotencji` | 5.1, 6.1, 6.2, 6.3, 9.2, 10.1.6, 14.1 (E-27), ADR-023 |
| C1-18 (ŚREDNIA) | Krok 5b „wszystko opanowane” (`R-UTRWALENIE`); krok 8 „brak Zadań” (`RekomendacjaWyjscie.brak_elementu`, UI, zlecenie puli); reguły i szablony `R-JEV-*`; szablon `R-WKLADKA-DIAG` (Wyniki z testu są Wynikami zadania); jednolity klucz puli z rodzajem | 10.1.1, 10.1.2, 10.1.5, 10.1.6, 12.4, 13.3, 14.1 (E-26), ADR-011 |
| C1-19 (ŚREDNIA) | G-5 obejmuje oba poziomy matury; automatyczne zlecenie szkicu Wkładki (`GENERUJ_SZKIC_WKLADKI`) przy E-20 z przeglądem przez recenzentów; domknięcie elementu Wkładki `ROZPOCZETY` (≥ 60 s → `UKONCZONY`, inaczej `POMINIETY`; po 24 h `UKONCZONY`) | 8.5, 9.2, 10.2.1, 13.3, 14.1 (E-20), 17.6, ADR-015 |
| C1-20 (ŚREDNIA) | `POST /elementy-sciezki/{id}/rozpoczecie` przy otwarciu treści (ważność 7 dni dla Zadania); `element_sciezki.zadanie_wersja_id` zapisywana przy rozpoczęciu i akceptowana mimo nowszej wersji; dla Przypisań okno 72 h (`zastapiona_at`). Odstępstwo od propozycji „24 h od publikacji”: wersja z chwili otwarcia jest dokładniejsza | 7.3, 8.5, 12.3, 12.5, 15.2 (C-16, C-17) |
| C1-21 (ŚREDNIA) | Dokładnie jeden kontekst próby (CHECK, `422`); indeksy `ux_odpowiedz_przypisanie`, `ux_rozwiazanie_przypisanie`; waga × 0,5 dla kolejnej próby w 60 dniach; wynik i klucz w Przypisaniu ujawniane od terminu (`wynik_ujawniony_od`) | 7.3, 9.3, 10.1.3, 12.4, 12.5, ADR-018, ADR-032 |
| C1-22 (ŚREDNIA) | Zasięg Zadań `GLOBALNY`/`PRYWATNY`; rola recenzenta treści; uprawnienia Przeglądu/wycofania (8.4); endpointy Przeglądu i `POST /zadania/{id}/wycofanie` dla Administratora; powiadomienia recenzentów dla Zadań bez zlecającego; system nie generuje dowodów | 3.3, 7.3, 8.4, 12.3, 12.8, 13.6, ADR-015, ADR-028 |
| C1-23 (ŚREDNIA) | Tabela `kalibracja_trudnosci` z atomowym `UPDATE` i blokadą wiersza; `zadanie.version` niezmieniane; `poziom_trudnosci_nominalny` vs skalibrowany z histerezą i audytem; scenariusz C-24 | 3.2, 7.2, 7.3, 10.1.3, 15.1, 15.2, ADR-029 |
| C1-24 (ŚREDNIA) | Korekta zamyka otwartą Weryfikację (Kontrola, zgłoszenie) jako `ZAKONCZONA` albo zwraca `409 WERYFIKACJA_ZAJETA`; kolumna „ETag” dla każdego endpointu; blokada wiersza `rozwiazanie` i `409 OCENA_ZMIENIONA`; poprawione C-09, nowe C-26 | 8.1, 8.3, 9.6.3, 12.1, 12.3, 15.2 |
| C1-25 (ŚREDNIA) | Rozłączne pule (R/O/P/W) z kolumną `pula`; slot = korutyna asyncio, CPU w puli procesów; model szczytu „koniec lekcji” (A-03, 17.2 S-6, 17.4); G-3 ze scenariuszem „koniec lekcji” | 2 (A-03), 5.1, 7.3, 13.3, 13.4, 17.2, 17.4, 17.6, ADR-002 |
| C1-26 (ŚREDNIA) | Usunięto wpis ręczny (`WPIS_RECZNY_UCZNIA`, przejście w 8.1, akcje E-03/E-04/E-06); zastąpiono ponowieniem rozpoznania, nowymi zdjęciami i porzuceniem; wpływ na słownik w PZS-1 B-1 | 1.3, 7.3, 8.1, 9.4, 12.3, 14.1, ADR-024 |
| C1-27 (NISKA) | Dodano endpointy: przedłużenie blokady, `GET /przypisania`, `GET/PUT/DELETE /zestawy-zadan/{id}`, archiwizacja Klasy, opuszczenie Klasy przez Ucznia, `PUT /admin/zadania/{id}/diagnostyczne`, przywrócenie konta, wycofanie Zadania (oraz szkic, rozpoczęcie elementu, ponowienie rozpoznania, porzucenie, zgłoszenie, obciążenie) | 12.3, 12.4 |
| C1-28 (NISKA) | Kolejność „najpierw `ZASTAPIONA`, potem `INSERT`” (9.6.2, 15.1); usunięto `SZKIC` ze statusów Zadania (8.4 = 9.7); liczniki `liczba_przeslan`, `liczba_ponowien_rozpoznania`, `przeslanie_nr` vs `praca_w_tle.proby`; `liczba_cykli` z inkrementacją warunkową; CHECK kontekstu Odpowiedzi | 7.3, 8.4, 9.6.2, 9.7, 13.3, 15.1 |
| C1-29 (NISKA) | Seria po Wkładce (`R-SERIA`, stan w `sciezka_nauki`, warunki zakończenia z poziomem maturalnym); dzienny budżet generowania systemowego (200 zleceń, 150 PLN, `zlecenie_generowania.systemowe`, `koszt_pln`) | 7.3, 10.1.2, 10.1.5, 12.7, 13.3, 21 (R-05), ADR-030 |

Zmiany przekrojowe: wersja 2.0; nowe sekcje 3.4 (PZS-1), 9.12, 9.13,
11.4.2–11.4.4, 11.5.1–11.5.3, 11.6–11.8, 17.7; nowe bramki G-0 i G-7; nowe
tabele `kalibracja_trudnosci`, `kalibrator_pewnosci`, `szkic_rozstrzygniecia`;
ADR-022 … ADR-033; macierz błędów rozszerzona do E-30, scenariusze
współbieżności do C-27.

### 23.3 Iteracja 3 (2026-11-01, wersja 2.1) — weryfikacja zamknięcia uwag C1

Każda uwaga iteracji 1 (`docs/CRITIQUE_LOG.md`, status OTWARTA — krytyk
nie przeglądał jeszcze wersji 2.0) została ponownie sprawdzona punkt po
punkcie (każdy podpunkt „Problem” i każdy element „Oczekiwanej poprawki”)
względem treści wersji 2.0. Kolumna „Stan w 2.0” podaje wynik weryfikacji;
„Uzupełnienie 2.1” — zmiany tej iteracji dla luk wykrytych przy weryfikacji;
„Miejsce rozwiązania” — pełna lista sekcji, w których uwaga jest rozwiązana
(stan po wersji 2.1).

| ID uwagi | Stan w 2.0 | Uzupełnienie 2.1 | Miejsce rozwiązania |
|---|---|---|---|
| C1-01 (KRYTYCZNA) | pp. 1–5 rozwiązane (Q-OC-04 `BLAD_ODCZYTU` nie zeruje punktów, Ocena wstrzymana, brak Wyniku zadania, T-3 z decyzji Modelu decyzyjnego, endpoint zgłoszenia, metryki z G-2, Z-1); luka: po zgłoszeniu Wynik zadania zakwestionowanej Oceny zostawał w Profilu do rozstrzygnięcia (do 72 h), a po usunięciu zdjęć nie było reguły rozstrzygania | zawieszenie Wyniku zadania na czas zgłoszenia (`powod_uniewaznienia`, odtworzenie Profilu); maszyna stanów Wyniku zadania; komunikat dla Ucznia; reguła „na korzyść Ucznia” po usunięciu zdjęć z wymaganym komentarzem; priorytet 1 po 80 dniach; scenariusze C-28, C-29; R-13 | 1.2 (Z-1, Z-3), 7.3 (`ocena`, `wynik_zadania.powod_uniewaznienia`), 8.2, 8.6, 9.5, 9.12, 10.1.3, 10.3, 10.4.3, 11.1 pp. 5–7, 11.3 (T-3, komunikaty), 11.4.3 p. 3, 11.6, 11.8, 12.3, 12.4, 12.5, 12.6, 15.2 (C-28, C-29), 17.6 (G-2), 21 (R-13), ADR-004, ADR-017, ADR-018, ADR-022 |
| C1-02 (KRYTYCZNA) | rozwiązane: G-7, SLA 72 h, eskalacja, status „opóźniona”, alert pustej puli, kontrola przyjęć, pseudonimizacja, zdjęcia maskowane, podstawa prawna, model obciążenia z Kontrolą; luka: G-7 = 12 weryfikatorów, a A-03 daje tylko 10 Nauczycieli bez Klas — źródło brakującej pojemności nieokreślone | A-14 i ADR-034: skład puli (10 bez Klas + ≥ 2 współpracujących), limit 180 min/dobę dla prowadzących Klasy (`422 LIMIT_OBCIAZENIA_WERYFIKATORA`), plan awaryjny `LEMAT_REJESTRACJA_BEZ_KLASY = false` z `kod_zaproszenia` w rejestracji; tabela składu puli w 17.7; R-12 | 2 (A-12, A-13, A-14), 7.3, 10.3, 11.4, 12.2, 12.3, 12.4, 12.8, 14.1 (E-28), 16.3, 17.6 (G-0, G-7), 17.7, 21 (R-02, R-12), 22, ADR-021, ADR-031, ADR-034 |
| C1-03 (WYSOKA) | pp. 1–7 rozwiązane (PZS-1 B-1…B-7, `kalibrator_pewnosci`, `uzasadnienie` w DTO, `POST /zadania` tylko N, `JevAdapter`); luka w p. 8: 19 nazw używanych w DDL/API (m.in. `element_sciezki`, `zlecenie_generowania`, weryfikator globalny, szkic rozstrzygnięcia) nie miało wpisu w 3.3, a `kalibrator_pewnosci.zrodlo` używał typu `zrodlo_progu` | 19 nowych pojęć w 3.3 (PZS-1 A: 61 pojęć) i test CI z zamkniętą listą wyłączeń; typ `zrodlo_kalibratora` | 1.2 (Z-6), 3.1, 3.2, 3.3, 3.4, 7.3, 11.7, 12.3, 12.4, ADR-016, ADR-028, ADR-032 |
| C1-04 (WYSOKA) | rozwiązane w całości (sprawdzono brak nazw `Odpowiedz` modelu, `procent_wariantu`, `nr_kroku`, `WynikRozpoznawania`, `wynik_walidacji`, `kolejka_weryfikacji` jako typu) | `zrodlo_kalibratora` rozdziela źródło kalibratora od źródła progu (3.1 p. 5) | 3.1 pp. 5–6, 3.3, 3.4 C, 7.3, 7.5, 10, 12.4, 12.9, 13.1, ADR-016 |
| C1-05 (WYSOKA) | rozwiązane (CHECK, semantyka, warunek `OCZEKUJE` + `version`, C-23); luka: trasa Weryfikacja w `PONOW_OCENE` nie zlecała `AKTUALIZUJ_PROFIL` mimo zapisu Wyniku zadania, a kolejność „anulowanie → Kontrola losowa” nie była jawna wobec `ux_weryfikacja_otwarta` | `AKTUALIZUJ_PROFIL` w trasie Weryfikacja; jawna kolejność anulowania przed `INSERT` Kontroli losowej | 7.3 (`weryfikacja`), 8.1, 8.3, 9.13, 13.3, 14.2, 15.1 p. 3, 15.2 (C-23) |
| C1-06 (WYSOKA) | rozwiązane w całości (reguła odczytu, `ZWOLNIJ_BLOKADY_WERYFIKACJI`, eskalacja dostępnych, `…/przedluzenie`, idempotentne podjęcie, szkice, poprawiony SQL) | — | 7.3, 8.3, 9.6.1, 11.4.2, 12.3, 13.5, 15.2 (C-07, C-08), ADR-033 |
| C1-07 (WYSOKA) | rozwiązane (`PlanPytan`, zwijanie pytań warunkowych, 2 rundy Jev, reguła ponowienia, Wskazówka asynchroniczna, S-1…S-6, G-4 z trzema scenariuszami) | — | 9.5, 9.9, 10.4.3, 10.4.4, 13.1, 13.2, 17.2, 17.6 (G-4), 22, ADR-009, ADR-026 |
| C1-08 (WYSOKA) | rozwiązane (czas systemowy z rozpoznaniem, T-6 jako naruszenie, SLO T-6, metryka end-to-end, A-11 w G-0); luka: termin wywołania rozpoznawania liczony od `przeslano_at` był już miniony przy ponowieniu, a scenariusz „timeout rozpoznania + ponowienie” nie był policzony | termin per próba (10.3.1, 9.4); scenariusz S-7 i rozpisanie budżetu 1% SLO; wymaganie A-08 ≤ 0,3% błędów/timeoutów dostawcy | 2 (A-08, A-11), 7.3, 9.4, 9.5, 10.3.1, 14.2, 17.1, 17.2 (S-1…S-7), 17.6, 22, ADR-025 |
| C1-09 (WYSOKA) | rozwiązane (analiza mocy — tabela `n_eff` sprawdzona obliczeniowo: 189 / 335 / 482 / 753; ZT-1 ≥ 700 + ≥ 250; CV5; tryb pełnej weryfikacji; G-2 end-to-end; A-09) | — | 2 (A-09), 11.5.1, 11.5.3, 17.1, 17.6 (G-1, G-2), 21 (R-10), ADR-007 |
| C1-10 (WYSOKA) | rozwiązane (priorytet 3 i SLA 72 h dla Kontroli, zdjęcia do zamknięcia, `wykonanie_kontroli_30d`, reguły nadzoru, IPW z korektą wykonania per koszyk, Kontrola w modelu 17.7) | — | 11.3, 11.5.3, 14.1 (E-29), 16.4, 17.7, ADR-007, ADR-033 |
| C1-11 (WYSOKA) | rozwiązane w całości (brak `theta_poczatkowa`, odtwarzanie od stanu DOMYŚLNEGO z Wynikami testu jako zdarzeniami, test raz na poziom, `AKTUALIZUJ_PROFIL`, zmiana poziomu matury) | — | 7.3, 9.1, 10.1.3, 12.3, 12.4, 13.6, ADR-010 |
| C1-12 (WYSOKA) | pp. 1–5 rozwiązane (`decyzja_modelu.uczen_id`, rejestr usunięć w Object Lock, wersjonowanie bucketów, retencja dostawców, C-20); luka: kopie zdjęć w zbiorze treningowym nie miały powiązania z kontem, więc odwołania zgody i usunięcia konta nie dało się dla nich wykonać | tabela `kopia_treningowa` (`ON DELETE RESTRICT`), prace `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` i `USUN_KOPIE_TRENINGOWE`, krok 4a w `USUN_KONTO`, `zgoda_na_trening` w `PATCH /uczniowie/ja`, ADR-035 | 2 (A-07), 6.2, 7.1, 7.3, 12.3, 13.1, 13.3, 13.5, 15.2 (C-20), 16.4, 17.6 (G-6), 22, ADR-019, ADR-035 |
| C1-13 (ŚREDNIA) | rozwiązane w całości (T-2 z τ_FR25 = 0.20, uzasadnienie, parametr) | — | 11.3, 22 |
| C1-14 (ŚREDNIA) | rozwiązane w całości (tryby Wskazówki, Wskazówka przy `BLAD_PRZETWARZANIA`, reguła N-1, `uzasadnienie` w DTO) | — | 9.6.2, 10.5, 11.1 p. 6, 11.7, 12.4 |
| C1-15 (ŚREDNIA) | rozwiązane w całości (12 par i N-6, `ZADEN` → wariant o najwyższym p, kolejność reguł na `punkty_surowe`) | — | 7.3, 10.4.3, 11.1, 11.2.1, ADR-005 |
| C1-16 (ŚREDNIA) | rozwiązane w całości (Zgodność Oceny, reguły źródła próbki, wykluczenie `KOREKTA_NAUCZYCIELA`, `ux_probka_ocena_auto`) | — | 3.3, 3.4 (B-5), 7.3, 11.5.2, 17.1, ADR-027 |
| C1-17 (ŚREDNIA) | rozwiązane w całości (jawny wyjątek synchroniczny przez fasadę Orkiestratora, timeout 1,5 s, krawędź `api → Jev`, moduły w 6.1/6.2, właściciel `klucz_idempotencji`) | — | 5.1, 6.1, 6.2, 6.3, 9.2, 10.1.6, 14.1 (E-27), ADR-023 |
| C1-18 (ŚREDNIA) | pp. 1–5 rozwiązane; luka w p. 1: krok 5b nie definiował zachowania, gdy wybrana Umiejętność nie ma Zadania na Poziomie trudności ≥ 4 | ścieżka zastępcza kroku 5b (kolejne Umiejętności, potem najwyższy Poziom trudności w zakresie, zapotrzebowanie puli, krok 8) | 10.1.1, 10.1.2 (kroki 5b, 8), 10.1.5, 10.1.6, 12.4, 13.3, 14.1 (E-26), ADR-011 |
| C1-19 (ŚREDNIA) | rozwiązane w całości (G-5 dla obu poziomów, `GENERUJ_SZKIC_WKLADKI`, domknięcie elementu Wkładki) | — | 8.5, 9.2, 10.2.1, 13.3, 14.1 (E-20), 17.6 (G-5), ADR-015 |
| C1-20 (ŚREDNIA) | rozwiązane w całości (rozpoczęcie przy otwarciu treści, wersja ustalana przy rozpoczęciu, okno 72 h dla Przypisań) | — | 7.3, 8.5, 12.3, 12.5, 15.2 (C-16, C-17) |
| C1-21 (ŚREDNIA) | rozwiązane w całości (dokładnie jeden kontekst, indeksy per Przypisanie, waga kolejnej próby, ujawnianie po terminie) | — | 7.3, 9.3, 10.1.3, 12.4, 12.5, ADR-018, ADR-032 |
| C1-22 (ŚREDNIA) | rozwiązane w całości (zasięg, recenzent treści, uprawnienia 8.4, endpointy Administratora, brak dowodów systemowych) | — | 3.3, 7.3, 8.4, 12.3, 12.8, 13.6, ADR-015, ADR-028 |
| C1-23 (ŚREDNIA) | rozwiązane w całości (`kalibracja_trudnosci`, atomowy `UPDATE`, histereza i audyt, C-24) | — | 3.2, 7.2, 7.3, 10.1.3, 15.1, 15.2 (C-24), ADR-029 |
| C1-24 (ŚREDNIA) | rozwiązane w całości (korekta zamyka otwartą Weryfikację albo `409`, kolumna „ETag”, C-09, C-26) | — | 8.1, 8.3, 9.6.3, 12.1, 12.3, 15.2 (C-09, C-26) |
| C1-25 (ŚREDNIA) | rozwiązane w całości (rozłączne pule, slot = korutyna, model „koniec lekcji”, G-3 b) | — | 2 (A-03), 5.1, 7.3, 13.3, 13.4, 17.2 (S-6), 17.4, 17.6 (G-3), ADR-002 |
| C1-26 (ŚREDNIA) | rozwiązane w całości (brak wpisu ręcznego, ADR-024, PZS-1 B-1) | — | 1.3, 7.3, 8.1, 9.4, 12.3, 14.1, ADR-024 |
| C1-27 (NISKA) | rozwiązane w całości (wszystkie 8 brakujących operacji ma endpointy) | — | 12.3, 12.4 |
| C1-28 (NISKA) | rozwiązane w całości (kolejność `ZASTAPIONA` → `INSERT`, statusy Zadania, liczniki przesłań i prób, inkrementacja warunkowa cykli, CHECK kontekstu Odpowiedzi) | — | 7.3, 8.4, 9.6.2, 9.7, 13.3, 15.1 |
| C1-29 (NISKA) | rozwiązane w całości (Seria z warunkiem wyjścia na poziomie maturalnym, budżet generowania systemowego) | — | 7.3, 10.1.2, 10.1.5, 12.7, 13.3, 21 (R-05), ADR-030 |

Zmiany przekrojowe wersji 2.1: nowe założenie A-14; nowa sekcja 8.6;
nowe tabele i typy `kopia_treningowa`, `zrodlo_kalibratora`,
`powod_uniewaznienia_wyniku`; nowe prace w tle `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`,
`USUN_KOPIE_TRENINGOWE`; nowe kody błędów `WYMAGANY_KOD_ZAPROSZENIA`,
`LIMIT_OBCIAZENIA_WERYFIKATORA`; scenariusz S-7; scenariusze współbieżności
C-28, C-29; ryzyka R-12, R-13; ADR-034, ADR-035; uzupełnienie ADR-022.

### 23.4 Decyzje, które wymagają właściciela produktu (bramka G-0)

Poniższych punktów nie da się zamknąć wyłącznie decyzją architektoniczną,
bo zmieniają albo doprecyzowują wymagania, słownik lub budżet operacyjny.
Dla każdego dokument wybiera opcję domyślną (obowiązującą do decyzji) i
opisuje opcję alternatywną, tak aby implementacja nie była blokowana
(od wersji 2.2 słowo „wariant” jest zarezerwowane dla Wariantu rozwiązania,
3.1 p. 5).

| # | Decyzja | Opcja domyślna (w dokumencie) | Alternatywa i jej koszt | Uwagi źródłowe |
|---|---|---|---|---|
| D-1 | Zmiany słownika PZS-1 (86 nowych pojęć w części A, definicje B-1…B-9; wersja 2.3: 5 nowych pojęć, B-5 ujednolicone z ADR-027, „Rejestr Zdarzeń RODO” zamiast „Rejestr usunięć”; wersja 2.4: „Dezaktywacja bezpiecznika”, „Blokada obniżania progu”, „Wstrzymanie”, zawężenie wyjątku Słownika terminów matematycznych) | treść 3.3/3.4 | inne brzmienie → zmiana nazw w kodzie przed wdrożeniem produkcyjnym | C1-03, C1-04 |
| D-2 | Interpretacja NFR-01 (A-11): czas systemowy, zobowiązanie ≥ 99% | A-11, 17.1, 17.2 (S-1…S-7) | 100% ≤ 30 s → model rozpoznawania hostowany we własnej infrastrukturze, poza budżetem pilotażu | C1-08 |
| D-3 | Ocena wstępna wstrzymana (bez punktów) przy podejrzeniu błędu odczytu; zawieszenie Wyniku przy zgłoszeniu; rozstrzyganie na korzyść Ucznia bez zdjęć | ADR-022 z uzupełnieniem 2.1 | literalne FR-24 (pokazanie punktów modelu) → łamie FR-28 | C1-01 |
| D-4 | Pula weryfikatorów globalnych: rekrutacja i finansowanie ≥ 8 Nauczycieli współpracujących (wersja 2.1: ≥ 2; model tygodniowy z eskalacjami, ADR-043) albo start bez rejestracji Uczniów bez Klasy (wtedy 4 weryfikatorów spośród 10 Nauczycieli bez Klas) | A-14, ADR-034 (obie opcje wspierane flagą) | automatyczna finalizacja po SLA — odrzucona, bo łamie FR-24/FR-26 | C1-02 |
| D-5 | Brak automatycznej finalizacji Oceny wstępnej po przekroczeniu SLA | ADR-021 | krytyk dopuścił ją „za zgodą właściciela produktu”; architektura jej nie implementuje, bo FR-26 mówi „zawsze” — zmiana wymagałaby zmiany FR-24/FR-26 | C1-02 |
| D-6 | Tryb pełnej weryfikacji, gdy ZT-1 nie daje progu | 11.5.1 p. 5 | powiększenie ZT-1 ponad 700 zadań obliczeniowych (koszt przygotowania zbioru) | C1-09 |
| D-7 | Brak wpisu ręcznego Rozwiązania | ADR-024 | wpis ręczny z obowiązkową Weryfikacją — obciąża Nauczycieli, omija kartkę | C1-26 |
| D-8 | Kopie treningowe za odwołalną zgodą | ADR-035 | brak zbioru treningowego — wolniejsza poprawa modeli (FR-34) | C1-12 |
| D-9 | Zatwierdzanie kont Nauczycieli przez Administratora; rozszerzenia powodów Weryfikacji (`PODEJRZENIE_BLEDU_ODCZYTU`, `ZGLOSZENIE_BLEDU_ODCZYTU`, `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`, `KONTROLA_LOSOWA`, `BLAD_PRZETWARZANIA`) | 16.1, 11.3, PZS-1 B-2 | bez Kontroli losowej NFR-03/NFR-04 są niemierzalne | C1-03, C1-10, C2-01 |
| D-10 | Zakres FR-18: Poprawki Transkrypcji tylko jako zmiana istniejących kroków i wstawienie ≤ 2 kroków, z limitami twardymi (`422`) i miękkimi (T-7) oraz Sprawdzeniem zgodności poprawek ze zdjęciem | ADR-036, 9.4, 10.3.3, 11.3 T-7 | dowolna edycja z obowiązkową Weryfikacją każdego Rozwiązania z Poprawkami — wzrost obciążenia Nauczycieli o szacunkowo 15–25% Rozwiązań obliczeniowych (do pomiaru `odsetek_rozwiazan_z_poprawkami_30d`), bez ochrony kalibracji przed zmienionym rozkładem; dowolna edycja bez kontroli — łamie ADR-024 i NFR-03 | C2-01 |
| D-11 | Zakres FR-14: Nauczyciel przegląda, poprawia i odrzuca Zadania wygenerowane na swoje zlecenie (zawsze `DO_PRZEGLADU`, wynik `PRYWATNY`); Zadania `GLOBALNY` poprawia recenzent treści lub Administrator, a Nauczyciel składa Zgłoszenie błędu w Zadaniu | ADR-028 (uzupełnienie it. 4), ADR-038, 8.4 | każdy Nauczyciel poprawia Zadania `GLOBALNY` bezpośrednio — nowa wersja dla wszystkich Uczniów bez przeglądu; wymagałoby audytu zmian i mechanizmu cofania wersji (+1 endpoint, +1 stan) | C2-03 |
| D-12 | Kryterium G-1(a) dla dowodów: zgodność ≥ 90% i LB Wilsona ≥ 85,0% przy n ≥ 350 | 11.5.1 (tabela 2), A-09, G-1 | LB ≥ 87,5% jak dla obliczeniowych — ZT-1 z ≥ 700 dowodami, +350 podwójnie ocenionych dowodów (≈ 58 h pracy Nauczycieli) | C2-06 |
| D-13 | SLA Weryfikacji 3 dni robocze, tygodniowy model obciążenia z 5% eskalacji, G-7 = 18 weryfikatorów globalnych na start, kontrola przyjęć per Klasa | A-15, ADR-043, 11.4.3, 17.7 | SLA 72 h kalendarzowe — wymaga pracy weekendowej weryfikatorów (≈ +40% pojemności w dniu roboczym) albo akceptacji poniedziałkowych przekroczeń SLA | C2-12 |
| D-14 | Możliwość pominięcia Testu diagnostycznego (FR-03: „nowy uczeń rozwiązuje…”) | 9.1 (pominięcie dozwolone, Profil `DOMYSLNA`, test możliwy później) | test obowiązkowy przed pierwszą rekomendacją — `GET …/rekomendacje/nastepna` zwraca `409 TEST_WYMAGANY`, gdy brak testu `ZAKONCZONY` dla poziomu matury, a pula pozwala go utworzyć | C2-13 |
| D-15 | Skutki przywrócenia konta Nauczyciela w okresie karencji dla Klas przekazanych przez Administratora | Klasy przekazane w karencji pozostają u nowego Nauczyciela; wracają tylko Weryfikacje eskalowane z powodu karencji i jeszcze niepodjęte; flagi i szkice zachowane (16.4, ADR-044) | automatyczny zwrot przekazanych Klas — wymaga zachowania historii przekazań (`klasa.poprzedni_nauczyciel_id`), powiadomienia nowego Nauczyciela i ponownego przypisania otwartych Weryfikacji `KLASA` oraz Przypisań (+1 kolumna, +1 krok przywrócenia), a Uczniowie i nowy Nauczyciel tracą ciągłość w trakcie roku | C3-08 |
| D-16 | Reakcja bezpiecznika trafności na trwały dowód przekroczenia NFR-03 przy progu 0,990 i jej koszt na granicy NFR-03 | automatyczne przejście pary w tryb pełnej weryfikacji (`wartosc = 1.000`) z wyjściem wyłącznie przez dezaktywację Administratora; przy e = 2% koszt: 1,1 fałszywego podniesienia progu na kwartał, 11% kwartałów w trybie pełnej weryfikacji, auto 57,5% zamiast 67% (11.5.3, 17.7) | zatrzymanie na 0,990 z alertem — brak kosztu obciążenia, ale przy degradacji jednorodnej błąd ocen automatycznych > 2% trwa do decyzji Administratora (naruszenie NFR-03 przez ≥ 1 dobę) | C4-02, C4-06 p. 5 |
| D-17 | Zapas puli weryfikatorów globalnych na wariant „aktywny bezpiecznik poprawek w fazie startowej” (20 wymaganych) | G-7 = 18; w tym wariancie współczynnik zapasu 1,22 zamiast 1,3, nadwyżkę zatrzymuje kontrola przyjęć kolejki globalnej (17.7) | G-7 = 20 (≥ 10 Nauczycieli współpracujących zamiast ≥ 8, koszt 2 umów) | C4-06 p. 7 |

Do czasu akceptacji implementacja realizuje opcje domyślne; zmiana
opcji dotyczy reguł w modułach `lemat.ocenianie`, `lemat.weryfikacja`,
`lemat.konta` i konfiguracji (22), bez zmiany struktury kontenerów, modułów
ani portów (R-11).

### 23.5 Iteracja 4 (2026-11-01, wersja 2.2) — zamknięcie uwag C1-21 i C2-01 … C2-16

Zaadresowano wszystkie 17 otwartych uwag z `docs/CRITIQUE_LOG.md`: ponownie
otwartą C1-21 oraz C2-01 … C2-16 z iteracji 2 (3 wysokie, 10 średnich, 3
niskie). Decyzje zmieniające lub zawężające wymagania przekazano do G-0
jako D-10 … D-14 z opcją domyślną opisaną w całości w dokumencie.
Odstępstwa od proponowanych poprawek: C2-01 — Oceny automatyczne z
Poprawkami nie są wyłączane z populacji progu, tylko objęte podwójną
Kontrolą losową i bezpiecznikiem (wyłączenie uczyniłoby NFR-03
niemierzalnym dla tej grupy; wyłączone są Oceny z T-7); C2-07 — globalny
ogranicznik jest przydziałem lokalnym z rejestru procesów, a nie token
bucketem w PostgreSQL (koszt zapisu na żądanie); C2-08 — wybrano wariant
„odroczenie anulowania do `USUN_KONTO`” zaproponowany przez krytyka.

| ID uwagi | Rozwiązanie | Miejsce rozwiązania |
|---|---|---|
| C1-21 (ŚREDNIA) | Kontekst efektywny próby: element `R-PRZYPISANIE` przekazuje `przypisanie_id` każdej próbie (trigger `trg_kontekst_proby`), więc jedna unikalność (Uczeń, Przypisanie, Zadanie) obejmuje obie drogi; klucz Zadania zamkniętego ujawniany od terminu także z elementu; Weryfikacja trafia do Nauczyciela Przypisania; próba liczy się do `PostepPrzypisaniaWyjscie` (nowy DTO, pole `droga`); jednoznaczny CHECK z kombinacjami {E}, {P}, {E, P}, {T}; `wygasa_at` elementu z Zadaniem zamkniętym ≤ termin; błąd `PRZYPISANIE_PO_TERMINIE` | 7.3 (`element_sciezki`, `rozwiazanie`, `odpowiedz`, `trg_kontekst_proby`), 8.5, 9.3, 10.1.1, 10.1.2 krok 1, 11.4.1 p. 1, 12.2, 12.4 (`OdpowiedzWejscie`, `OdpowiedzWyjscie`, `PostepPrzypisaniaWyjscie`), 12.5, 15.2 (C-30), 18.1 (FR-32), ADR-032 (uzupełnienie it. 4) |
| C2-01 (WYSOKA) | Poprawki Transkrypcji związane z krokami agenta (`nr_kroku_agenta`), bez usuwania, ≤ 2 wstawione; limity twarde (`422 POPRAWKI_ZBYT_ROZLEGLE`) i miękkie; Sprawdzenie zgodności poprawek ze zdjęciem (`SPRAWDZ_POPRAWKI_TRANSKRYPCJI`, rola `ROZPOZNAWANIE_PISMA`, Z-1); reguła T-7 i powód `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`; Oceny z T-7 poza populacją progu; podwójny odsetek Kontroli losowej i bezpiecznik poprawek; metryki; G-2; PZS-1 B-1/B-2 | 1.3, 3.3, 3.4 (B-1, B-2), 7.3 (`powod_weryfikacji`, `transkrypcja`, `krok_transkrypcji`), 8.1, 9.4, 9.5, 10.3.1, 10.3.3, 11.1 p. 7, 11.3 (T-7, T-5, priorytet, komunikat), 11.5.2, 12.2, 12.3, 12.4, 12.5, 13.1, 13.3, 13.6, 14.1 (E-31, E-32), 15.2 (C-35), 17.2, 17.3, 17.6 (G-0, G-2), 17.7, 18.1 (FR-18), 21 (R-14), 22, ADR-024, ADR-036, D-10 |
| C2-02 (WYSOKA) | `weryfikacja.version` = wersja treści ustawiana triggerem `trg_weryfikacja_wersja`; podjęcie, przedłużenie, zwolnienie, eskalacja bez zmiany `version`; przedłużenie zwraca ETag i `version`; `412` z polem `zmiany_tresci`; scenariusz rozstrzygnięcia po przedłużeniach | 7.2, 7.3 (`weryfikacja`, trigger), 8.3, 9.6.1 (SQL i diagram), 9.12, 9.13, 11.4.2, 11.4.3, 12.3, 12.4, 15.1 p. 4, 15.2 (C-23, C-28, C-31), ADR-033 (uzupełnienie), ADR-037 |
| C2-03 (WYSOKA) | Zadania wygenerowane na zlecenie Nauczyciela zawsze `DO_PRZEGLADU` z Przeglądem zlecającego (wynik `PRYWATNY`, promocja przez recenzenta); Zgłoszenie błędu w Zadaniu dla Zadań `GLOBALNY` (tabela, endpointy, powiadomienie, SLA 5 dni roboczych), wstrzymanie w doborze po 3 zgłoszeniach; ADR-028 oznaczony G-0; decyzja D-11 | 3.3, 6.2, 7.3 (`zadanie.wstrzymane_w_doborze`, `zgloszenie_bledu_zadania`), 8.4, 9.7, 10.1.2 krok 7, 12.2, 12.3, 12.4, 12.7, 12.8, 13.6, 15.2 (C-36), 17.6 (G-0), 18.1 (FR-14), 21 (R-18), 22, ADR-028, ADR-038, D-11 |
| C2-04 (ŚREDNIA) | Deduplikacja `AKTUALIZUJ_PROFIL` tylko wśród prac `OCZEKUJE` (`ux_praca_dedup_oczekujace`), pętla handlera do pustego wyniku, zamiatacz `ZAMIATAJ_PROFILE` co 5 min, metryka i alert wieku najstarszego niezastosowanego Wyniku | 7.3 (`praca_w_tle`), 13.3, 13.4, 13.5, 15.2 (C-11, C-32), 17.3, 22, ADR-002 (uzupełnienie) |
| C2-05 (ŚREDNIA) | Kolumna `weryfikacja.powody_poczatkowe` (niezmienna poza dołączeniem Oceny przez `PONOW_OCENE`, trigger `trg_powody_poczatkowe`); źródło Próbki z powodów początkowych, zgłoszenie dopisane później nie wyklucza próbki; test jednostkowy | 3.3, 7.3, 9.5, 9.6.2, 9.12, 9.13, 11.5.2, 12.4 (`WeryfikacjaSzczegoly`), ADR-007 (uzupełnienie) |
| C2-06 (ŚREDNIA) | Analiza mocy G-1(a) per rodzaj (tabela 2: LB, minimalna obserwowana zgodność, moc); ZT-1 ≥ 350 dowodów z LB ≥ 85,0% (≥ 700 obliczeniowych z LB ≥ 87,5%); poprawiona półszerokość ±3,1 pp; D-12 | 2 (A-09), 11.5.1, 17.6 (G-0, G-1), ADR-007 (uzupełnienie), D-12 |
| C2-07 (ŚREDNIA) | Skład rund i fal (27 + 1 albo 16 + 13 pytań); tryby Jev `WSADOWY`/`POJEDYNCZY` (A-01); budżet 17.2 i S-1 … S-7 przeliczone dla 3 fal i terminu miękkiego `max(10 s, 28 s − t_R)`; globalny ogranicznik szybkości (`limit_szybkosci_modelu`, przydział ważony, token bucket); `429` i brak przydziału poza circuit breakerem; G-4 (d) | 2 (A-01), 3.1 p. 6, 6.2, 7.3 (`limit_szybkosci_modelu`), 9.5, 10.4.3, 13.1 (`OgranicznikSzybkosci`), 13.2, 14.1 (E-33), 14.2, 17.1, 17.2, 17.4, 17.6 (G-4), 21 (R-17), 22, ADR-039 |
| C2-08 (ŚREDNIA) | Okres karencji: żądanie usunięcia wstrzymuje procesy zamiast je anulować; przywrócenie wznawia procesy (tabela 16.4: Weryfikacje z nowym SLA, zgłoszenia i zawieszone Wyniki rozstrzygane normalnie, ponowne rozpoznanie, T-6 dla przerwanych ocen); anulowanie w kroku 0 `USUN_KONTO`; Kontrole kont `USUWANE` poza `π_wykonanie`; scenariusz C-33 | 3.3, 8.1, 8.3, 9.6.1, 9.12, 11.4.2, 11.5.3, 12.3, 13.3, 13.6, 15.2 (C-20, C-33), 16.4, 22, ADR-040 |
| C2-09 (ŚREDNIA) | Rejestr wszystkich Zdarzeń RODO (żądanie, anulowanie, wykonanie usunięcia, odwołanie i udzielenie zgody) zapisywany w Object Lock **przed** transakcją, kopia w `zdarzenie_rodo`; `UZGODNIJ_REJESTR_RODO`; procedura `ponow_zdarzenia_rodo` przed startem workerów; G-6 z trzema scenariuszami | 3.3, 5.1, 6.2, 7.3 (`zdarzenie_rodo`, `typ_zdarzenia_rodo`), 12.2, 12.3, 13.3, 13.5, 14.1 (E-36), 15.2 (C-34), 16.4, 17.1, 17.6 (G-6), 21 (R-16), ADR-041 |
| C2-10 (ŚREDNIA) | Reguła promptu i deterministyczny Filtr danych osobowych przed zapisem Transkrypcji, Poprawek i kopii treningowej (ramki kroków, dane z konta, PESEL, e-mail); kolumna `dane_osobowe_usuniete`, metryka; test na ZT-1 (G-2, G-6); poprawione ADR-031 i 16.3 | 2 (A-09), 3.3, 7.3 (`transkrypcja`, `krok_transkrypcji.obszar`), 9.4, 10.3.1 p. 2 i 3a, 10.3.2, 13.3, 16.3, 17.3, 17.6 (G-2), ADR-031, ADR-035, ADR-042 |
| C2-11 (ŚREDNIA) | Zmiany nazw: `decyzje_skladowe` (schemat adaptera zapasowego), `kod_statusu_http`/`cialo_http`, `opcje_zamkniete`, `?wersja_zdjecia=`, `rezultat_przestarzaly`, `rezultat_ujawniony_od`, `OdpowiedzWyjscie.rezultat`, „prace w tle” zamiast „kolejka”, „Strategia eksperymentalna” zamiast „Wariant eksperymentalny”, „opcja” zamiast „wariant” w 23.4; 3.1 p. 5 rozszerzony; 17 nowych pojęć w 3.3 (w tym Pewność surowa, Punkty surowe, Niespójność, Rozstrzygnięcie, Korekta, Eskalacja); PZS-1 B-8 (Agent), B-9 (Jev); test CI na tabele, tokeny kolumn, typów, OpenAPI, schematów modeli i słowa zarezerwowane | 3.1, 3.3, 3.4 (A, B-8, B-9, C), 5.1, 5.2, 6.3, 7.3, 7.5.1, 9.3, 10.1.6, 10.2.1, 12.3, 12.4, 13.2, 13.4, 14.1 (E-13), 15.2, 23.4 |
| C2-12 (ŚREDNIA) | SLA 3 dni robocze (`dzien_wolny`, `dodaj_dni_robocze`); model tygodniowy 7/5 z jawnymi eskalacjami 5% (A-15) i współczynnikiem 1,3; G-7 = 18 na start; `zaleglosc_globalna_min` z eskalacjami; kontrola przyjęć per Klasa; D-13 | 2 (A-13, A-14, A-15), 3.3, 7.3, 11.4.1 p. 4, 11.4.3, 11.5.3, 12.4 (`ObciazenieWeryfikacjiWyjscie`), 13.3, 17.3, 17.6 (G-0, G-7), 17.7, 21 (R-02, R-12, R-15), 22, ADR-021, ADR-034, ADR-043, D-4, D-13 |
| C2-13 (ŚREDNIA) | G-5 wymaga ≥ 3 Zadań diagnostycznych na (Dział, poziom matury, nominalny Poziom trudności 2/4); dobór po poziomie nominalnym; test częściowy (`pominiete_dzialy`) albo `409 TEST_NIEDOSTEPNY` < 50%; `SPRAWDZ_PULE_DIAGNOSTYCZNA`, `409 PULA_DIAGNOSTYCZNA_ZA_MALA`, `GET /admin/pula-diagnostyczna`; DTO `TestDiagnostycznyWyjscie`; pominięcie testu jako D-14 | 7.3 (`ix_zadanie_diagnostyczne`, `pominiete_dzialy`), 9.1, 12.2, 12.3, 12.4, 13.3, 13.5, 14.1 (E-34), 17.6 (G-0, G-5), 18.1 (FR-03), 22, D-14 |
| C2-14 (NISKA) | Przepływ usunięcia konta Nauczyciela i Administratora (endpointy, karencja, Przekazanie Klasy, archiwizacja nieprzekazanych Klas, pseudonimizacja uzasadnień, ochrona ostatniego Administratora); DDL z `ON DELETE SET NULL` i CHECK „Klasa bez Nauczyciela tylko zarchiwizowana” | 1.3, 3.3, 6.2, 7.3 (`klasa`, `zestaw_zadan`, `przypisanie`, `import_arkusza`), 11.4.1 p. 1, 11.4.3 p. 1, 12.2, 12.3, 13.3, 13.6, 16.3, 16.4, 17.6 (G-6), 18.1 (FR-30), ADR-044 |
| C2-15 (NISKA) | Piaskownica procesowa Pillow w `api` (2 procesy na replikę, RLIMIT, limit czasu), limit pikseli i wymiarów przed dekodowaniem, `422 ZDJECIE_NIEPRAWIDLOWE`, `503` przy zajętej piaskownicy; budżet i pojemność | 5.1, 5.2, 9.4, 10.3.1 p. 1, 12.2, 14.1 (E-35), 16.2, 17.2, 17.4, 22, ADR-045 |
| C2-16 (NISKA) | (1) `rezultat_ujawniony_od` NULL dla Odpowiedzi testu do `…/zakonczenie` lub `…/pominiecie`; (2) warunek 2 h przy ponownym podjęciu przez właściciela; (3) `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` przy każdym przejściu do „`OCENIONE` bez otwartej Weryfikacji” i nadpisanie przy nowszej Ocenie; (4) okno zgłoszenia od Oceny aktualnej; (5) limit 2 odrzuceń automatycznych szkiców Wkładek na 30 dni i szkice w budżecie generowania systemowego; (6) Wkładka bez dowodu przeczytania po `wygasa_at` → `POMINIETY` w 8.5 i 13.3 | (1) 7.3, 9.1; (2) 9.6.1, 11.4.2, ADR-033; (3) 7.3 (`kopia_treningowa.ocena_id`), 9.5, 9.6.2, 9.13, 13.3, 16.4, ADR-035; (4) 11.6, 7.3; (5) 7.3 (`wkladka_teoretyczna`), 10.2.1, 12.7, 13.3, 14.1 (E-30), 22; (6) 8.5, 13.3 |

Zmiany przekrojowe wersji 2.2: nowe założenie A-15; nowe tabele
`zgloszenie_bledu_zadania`, `zdarzenie_rodo`, `dzien_wolny`,
`limit_szybkosci_modelu`; nowe typy `status_zgloszenia_zadania`,
`typ_zdarzenia_rodo` i wartość `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`; nowe
triggery `trg_kontekst_proby`, `trg_weryfikacja_wersja`,
`trg_powody_poczatkowe`; nowa sekcja 10.3.3; reguła trasowania T-7; nowe
prace w tle `SPRAWDZ_POPRAWKI_TRANSKRYPCJI`, `ZAMIATAJ_PROFILE`,
`SPRAWDZ_PULE_DIAGNOSTYCZNA`, `UZGODNIJ_REJESTR_RODO`; nowe kody błędów
`PRZYPISANIE_PO_TERMINIE`, `TEST_NIEDOSTEPNY`, `PULA_DIAGNOSTYCZNA_ZA_MALA`,
`ZGLOSZENIE_ZADANIA_ISTNIEJE`, `OSTATNI_ADMINISTRATOR`,
`ZDJECIE_NIEPRAWIDLOWE`, `POPRAWKI_ZBYT_ROZLEGLE`; macierz błędów do E-36;
scenariusze współbieżności do C-36; scenariusze budżetu S-1b, S-2b; ryzyka
R-14 … R-18; ADR-036 … ADR-045; decyzje G-0 D-10 … D-14; PZS-1: 78 pojęć w
części A, B-8 i B-9 w części B.


### 23.6 Iteracja 5 (2026-11-01, wersja 2.3) — zamknięcie uwag C3-01 … C3-09

Zaadresowano wszystkie 9 otwartych uwag z `docs/CRITIQUE_LOG.md`, iteracja 3
(1 wysoka, 6 średnich, 2 niskie). Jedna decyzja wymaga właściciela produktu
(D-15, z opcją domyślną opisaną w 16.4). Odstępstwa od sugerowanych
poprawek: C3-01 — wybrano dolną granicę Wilsona (LB > 2% przy `n_eff ≥ 50`)
zamiast SPRT, z uzasadnieniem w ADR-046; C3-02 — zamiast maskowania
fragmentu kroku wg ramki (ramka pikselowa nie wskazuje znaków w tekście) krok
z treścią matematyczną pozostaje nienaruszony i kieruje Ocenę do Weryfikacji
(T-3), a maskowany w całości jest tylko krok bez treści matematycznej; C3-03
— zastosowano oba warianty krytyka jednocześnie (wiersz w pierwszej
transakcji **i** `zrodlo = WORKER` wyłączone z unieważniania); C3-06 —
sprzątanie zamiast Zdarzenia RODO dla kopii (ADR-047).

| ID uwagi | Rozwiązanie | Miejsce rozwiązania |
|---|---|---|
| C3-01 (WYSOKA) | Bezpieczniki trafności i poprawek wyzwalane wyłącznie dowodem przekroczenia: LB Wilsona 95% błędu (IPW) > 2% przy `n_eff ≥ 50`; ostrzeżenie (blokada obniżania progu) przy UB > 2% i `n_eff ≥ 189`; próba dla pary (konfiguracja, rodzaj) powyżej bieżącego progu, nie dla rekordu progu (brak zapadki); rachunek: tabela `k_min` i P(wyzwolenie) dla n = 50 … 1 000 (przy 0 błędach LB = 0 — nigdy) i symulacja 90 dni dla startu, fazy ustalonej i modelu zapasowego; tabela `stan_bezpiecznika` zamiast flagi środowiskowej, dezaktywacja (w wersji 2.3: „zdjęcie”) przez Administratora z `okno_od`; testy jednostkowe (a)–(f), w tym „0 błędów przy n = 50 … 188 nie wyzwala”; scenariusz startowy w 17.7: reguła 2.2 dawałaby auto ≈ 0%, `M` = 5 154 min, 137 min na Nauczyciela i 26 weryfikatorów globalnych zamiast 18 — reguła 2.3 zachowuje „Start: auto 47%” i G-7 = 18 | 3.3 (Bezpiecznik), 7.3 (`typ_bezpiecznika`, `stan_bezpiecznika`), 9.8, 11.5.2, 11.5.3, 12.3 (`/admin/bezpieczniki`, `/admin/bezpieczniki/zdjecie` — od wersji 2.4 `/admin/bezpieczniki/dezaktywacja`), 12.2 (`BEZPIECZNIK_NIEAKTYWNY`), 12.4 (`StanBezpiecznikaWyjscie`), 13.3 (`KALIBRUJ`), 14.1 (E-21), 17.1 (NFR-03), 17.3, 17.7, 20, 21 (R-20), 22, ADR-007 (uzupełnienie), ADR-046 |
| C3-02 (ŚREDNIA) | Filtr danych osobowych nie usuwa kroków i nie zmienia `latex`; krok w ramce bez treści matematycznej → `[DANE_OSOBOWE]` w całości, z treścią matematyczną → zachowany, `konflikt_filtra` → T-3 (11.1 p. 7d, Z-1); dopasowanie nazw tylko w `tekst`, Słownik terminów matematycznych (≈ 350 lematów, ≈ 4 000 form) nigdy niezastępowany, nazwy < 4 znaków — dopasowanie dokładne; pozycje zastąpień w `zastapienia_danych_osobowych`, przywracanie słowa Poprawką (dopasowania przybliżone nie są ponownie filtrowane, Poprawka podlega Sprawdzeniu i podwójnej Kontroli); G-2: 150 kartek (UB 2,5%), w tym krótkie imiona i nazwiska-terminy, 0 zastąpień terminów matematycznych i ≤ 0,5% fałszywych zastąpień na 1 050 Transkrypcjach, `konflikt_filtra` ≤ 1% | 2 (A-09), 3.3 (Filtr, Słownik terminów matematycznych, Obszar kroku), 7.3 (`krok_transkrypcji`, `transkrypcja.konflikt_filtra`), 10.3.1 p. 3a, 11.1 p. 7d, 12.4 (`KrokTranskrypcji`, `TranskrypcjaWyjscie`), 14.1 (E-37), 17.3, 17.6 (G-2), 21 (R-19), 22, ADR-042 (uzupełnienie) |
| C3-03 (ŚREDNIA) | `USUNIECIE_WYKONANE` zapisuje `worker` (`zrodlo = WORKER`, deterministyczne `id` = uuid5, `If-None-Match: *`) przed pierwszą zmianą w bazie, wiersz `zdarzenie_rodo` w pierwszej transakcji `USUN_KONTO` (krok 1, razem z anulowaniem procesów); `UZGODNIJ_REJESTR_RODO` unieważnia tylko zdarzenia `API`, a dla `WORKER` ponawia `USUN_KONTO` po 2 h i alarmuje po 26 h; `ponow_zdarzenia_rodo` wykonuje kroki 1–6 synchronicznie bez względu na czas od usunięcia; poprawione ADR-041 i komentarz `zdarzenie_rodo.id`; G-6 (a) z odtworzeniem 60 min po usunięciu | 3.3 (Zdarzenie RODO), 5.1, 7.3 (`zrodlo_zdarzenia_rodo`, `zdarzenie_rodo`), 13.3 (`UZGODNIJ_REJESTR_RODO`, `USUN_KONTO`), 14.1 (E-36), 16.4 (kroki 0–1, procedura odtworzenia, konto Nauczyciela p. 1), 17.3, 17.6 (G-6 a), 21 (R-16), 22, ADR-041 (uzupełnienie) |
| C3-04 (ŚREDNIA) | Jedna formuła `0,8 × limit × waga_i / Σ wag` (kolumny `waga` i `pojemnosc` w `limit_szybkosci_modelu`); 1 token = 1 wywołanie HTTP; kubełek workera 16 ≥ maks. fala (4 przy ścisłym oknie 1 s, test G-4); przyjęcie planu przed falą 1 (przewidywany koniec ≤ termin miękki), terminy fal ≥ 2, pierwszeństwo fal ≥ 2; porty `przyjmij_plan`, `zarezerwuj_wywolanie`, `zwolnij`; testy jednostkowe (a)–(e); symulacja profili A–D: przy A-03 0 Ocen do modelu zapasowego (wersja 2.2: 18%), S-6 ≤ 26,2 s, S-6b w budżecie SLO; A-01 z tolerancją 75 wywołań w 1 s | 2 (A-01), 3.3 (Przyjęcie planu), 7.3 (`limit_szybkosci_modelu`), 9.9, 13.1 (`PrzyjeciePlanu`, `OgranicznikSzybkosci`), 13.2, 14.1 (E-33), 17.2 (S-6, S-6b, budżet SLO), 17.3, 17.4 (symulacja), 17.6 (G-4), 21 (R-21), 22, ADR-039 (uzupełnienie) |
| C3-05 (ŚREDNIA) | Kroki wstawione w liczniku odsetka znaków (mianownik: wszystkie kroki agenta), limit długości kroku wstawionego ≤ min(200, max(60, ⌈1,5 × mediana⌉)) z przykładem; obszar kroku wstawionego między sąsiednimi krokami; autozapis PWA (debounce 2 s, zapis przed zatwierdzeniem), deduplikacja Sprawdzenia tylko wśród `OCZEKUJE` z podmianą ładunku (C-38); osobna pula `S`, wejście z wycinków i zdjęcia 1024 px, A-08: p95 ≤ 4 s i limit ≥ 150/min; rachunek braku rezultatu (≈ 0,1% przy `G ≈ 0`, przyjęte ≤ 0,5%); pojemność dostawcy w szczycie 117/min ≤ 120/min; T-7 wyprowadzone z rozkładu (2,5–3,1%, model 3%); G-2: „wstawienia rozwiązania” ≥ 95%, fałszywe alarmy ≤ 5% | 2 (A-08), 7.3 (`praca_w_tle.pula`, indeksy deduplikacji), 9.4, 10.3.1 (`PoprawkaDoSprawdzenia.obszar`), 10.3.3, 12.2, 12.5, 13.3, 13.4, 15.2 (C-38), 17.2, 17.3, 17.4, 17.6 (G-2), 17.7, 21 (R-14), 22, ADR-036 (uzupełnienie) |
| C3-06 (ŚREDNIA) | Praca `SPRZATAJ_OSIEROCONE_OBIEKTY` dla `zbior-treningowy/`, `zdjecia/`, `zdjecia-maskowane/`: tryb `CODZIENNY` (04:30 UTC, okno 1 h) i `ODTWORZENIE` (krok 2a procedury PITR przed startem workerów); koszt przebiegu; metryka i alert; E-02 rozszerzone; G-6 (d) | 13.3, 13.5, 14.1 (E-02, E-38), 16.4 (retencja, procedura odtworzenia p. 2a, magazyn obiektów), 17.3, 17.6 (G-6 d), 21 (R-16), 22, ADR-047 |
| C3-07 (ŚREDNIA) | Reguła transliteracji „żądanie” → „wniosek” / „wywołanie” (3.1 p. 3); `WNIOSEK_O_USUNIECIE` zamiast `ZADANIE_USUNIECIA`, `jev_wywolania_http_total`, parametr `liczba_zadan` usunięty (rezerwacja per wywołanie); test CI z tokenem `zadan*` i listą dozwolonych użyć; „Grupa eksperymentu” i Q-RE-01 bez słowa „wariant”; B-5 ujednolicone z ADR-027 (brakujący krok); typy `ObszarNaZdjeciu` / `ObszarKroku`; „Rejestr Zdarzeń RODO” i bucket `rejestr-zdarzen-rodo` | 3.1 p. 3, 3.3, 3.4 (część A, test CI część 3, B-5), 5.1, 7.3 (`typ_zdarzenia_rodo`), 10.3.1, 10.3.2, 10.4.3, 12.3, 13.1, 16.4, 17.3, ADR-041 (uzupełnienie), D-1 |
| C3-08 (NISKA) | Karencja konta Nauczyciela: natychmiast tylko zwolnienie blokad (szkice zachowane) i eskalacja z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA`, flagi do `USUN_KONTO`; tabela przywrócenia dla Nauczyciela i Administratora (cofnięcie eskalacji oczekujących, Klasy przekazane bez zwrotu — D-15); rozstrzygnięcie i korekta wymagają konta Ucznia `AKTYWNE` (`409 KONTO_UCZNIA_NIEAKTYWNE`, C-37); przywrócenie zleca `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` | 7.3 (`powod_eskalacji`, `weryfikacja.powod_eskalacji`), 9.6.2, 9.6.3, 11.4.3 p. 1, 12.2, 12.3, 12.5, 13.6, 15.2 (C-37), 16.4, 17.6 (G-0, G-6), ADR-040 i ADR-044 (uzupełnienia), D-15 |
| C3-09 (NISKA) | (1) R-02: SLA 3 dni robocze; (2) 12.6: `termin_at` = 2026-11-05T07:00:00Z z rachunkiem `dodaj_dni_robocze`; (3) `LIMIT_OBCIAZENIA_WERYFIKATORA` i parametry 22: minuty na dzień roboczy; (4) `WYCZYSC_DANE_TECHNICZNE` usuwa `zdarzenie_rodo` po 2 latach; (5) C-07 z SQL podjęcia 2.2+ (`podjeta_at + 2 h`, konto `AKTYWNE`); (6) Wstrzymanie w doborze w tabeli `wstrzymanie_w_doborze` poza `zadanie.version` (ADR-029), C-36 przepisany | (1) 21; (2) 12.6; (3) 12.2, 22; (4) 13.3; (5) 15.2; (6) 3.3, 7.2, 7.3, 8.4, 10.1.2 krok 7, 12.3, 12.4, 12.5, 13.6, 15.2 (C-36), 18.1, ADR-029 (uzupełnienie), ADR-038 |

Zmiany przekrojowe wersji 2.3: nowe typy `zrodlo_zdarzenia_rodo`,
`typ_bezpiecznika`, `powod_eskalacji`; nowe tabele `stan_bezpiecznika`,
`wstrzymanie_w_doborze`; nowe kolumny `zdarzenie_rodo.zrodlo`,
`weryfikacja.powod_eskalacji`, `krok_transkrypcji.podejrzenie_danych_osobowych`,
`tylko_dane_osobowe`, `zastapienia_danych_osobowych`,
`transkrypcja.konflikt_filtra`, `limit_szybkosci_modelu.waga`, `pojemnosc`;
usunięta kolumna `zadanie.wstrzymane_w_doborze`; zmieniona wartość
`ZADANIE_USUNIECIA` → `WNIOSEK_O_USUNIECIE`; nowa pula `SPRAWDZANIE`; nowa
praca w tle `SPRZATAJ_OSIEROCONE_OBIEKTY`; nowe endpointy `GET
/admin/bezpieczniki`, `POST /admin/bezpieczniki/zdjecie` (od wersji 2.4 `POST /admin/bezpieczniki/dezaktywacja`, C4-04); nowe kody błędów
`KONTO_UCZNIA_NIEAKTYWNE`, `BEZPIECZNIK_NIEAKTYWNY`; macierz błędów do
E-38; scenariusze współbieżności do C-38; scenariusz budżetu S-6b; ryzyka
R-19 … R-21; ADR-046, ADR-047 oraz uzupełnienia ADR-007, ADR-029, ADR-036,
ADR-039 … ADR-042, ADR-044; decyzja G-0 D-15; PZS-1: 83 pojęcia w części A;
słowa ogólne dopisane do `tests/slownik_tokeny.yaml`: `sprzataj`,
`osierocone`, `obiekty`, `wniosek`, `wywolania`, `pojemnosc`, `waga`,
`okno`.

### 23.7 Iteracja 6 (2026-11-01, wersja 2.4) — zamknięcie uwag C4-01 … C4-06

Zaadresowano wszystkie 6 otwartych uwag z `docs/CRITIQUE_LOG.md`, iteracja 4
(4 średnie, 2 niskie), w tym wszystkie 10 podpunktów C4-06. Dwie decyzje
wymagają właściciela produktu (D-16, D-17; opcje domyślne w 11.5.3 i 17.7).
Odstępstwa od sugerowanych poprawek: C4-02 — oprócz `prog_nowy = max(wybór,
prog_obecny + 0,02)` i zakazu obniżania dodano tryb pełnej weryfikacji przy
wyzwoleniu na 0,990 (bez niego reakcja na degradację jednorodną kończy się na
0,990 z błędem > 2%), a symulacja wykazała, że okno 30 dni wykrywa nagłą
degradację w medianie 13 dni (nie 2–4) — skorygowano ADR-046 i R-20, a okno
7 dni odrzucono z rachunkiem; C4-04 — `dezaktywowany_przez` zamiast
`dezaktywowal_id` (konwencja `utworzony_przez`, `podjeta_przez`), a
„wstrzymanie” dostało jedną definicję zamiast zmiany nazw; C4-05 — pojemność
`api` stała (1), a workery dzielą resztę `B_całk` proporcjonalnie do wagi,
co przy 2 workerach zachowuje kubełek 16 i wyniki S-6.

| ID uwagi | Rozwiązanie | Miejsce rozwiązania |
|---|---|---|
| C4-01 (ŚREDNIA) | `ponow_zdarzenia_rodo`: kryterium zastosowania zależne od źródła — `API`: wiersz `zdarzenie_rodo`; `WORKER`: brak wiersza `uzytkownik` (PITR w trakcie `USUN_KONTO` daje wiersz zdarzenia i pełne dane konta); cały prefiks `wykonane/` bez filtra czasu; ponowienie każdego zdarzenia według `dane.rola` tymi samymi funkcjami domenowymi co `api` i `USUN_KONTO` (kroki (1)–(5) Nauczyciela, skutki `KontoUsuwane` / `KontoPrzywrocone`, tabela przywrócenia właściwa dla roli); `rola` w każdym Zdarzeniu RODO (CHECK); G-6 (a3) PITR po krokach 1–4 i (a4) konto Nauczyciela; scenariusz C-39 | 7.3 (`zdarzenie_rodo.dane`, CHECK), 13.3 (`USUN_KONTO`), 13.6 (`KontoUsuwane`, `KontoPrzywrocone`), 15.2 (C-39), 16.4 (wniosek p. 1, krok 0, procedura odtworzenia p. 2, konto Nauczyciela p. (1)), 17.3, 17.6 (G-6), 21 (R-16), ADR-041 (uzupełnienie it. 6) |
| C4-02 (ŚREDNIA) | „Wynik dnia KALIBRUJ”: kroki 1–6 w jawnej kolejności — wybór progu, ocena bezpieczników na progu i kalibratorze sprzed przebiegu, Blokada obniżania progu (wejście przy wyzwoleniu, wyjście przy UB ≤ 2% i `n_eff ≥ 189` powyżej bieżącego progu), dozwolone obniżenie z testem 30 dni powyżej progu docelowego, `p_nowy = max(p_dol, p0 + 0,02)` przy wyzwoleniu, 0,990 → 1,000, jeden rekord `prog_pewnosci`; `stan_bezpiecznika.aktywny` dla `TRAFNOSC`, `wyjscie_z_blokady_at`; testy (g)–(k), w tym „degradacja 4% przez 30 dni na tle 60 dni z błędem 0,5% → próg niemalejący”; symulacja dynamiki (trajektoria progu i udział auto: 67% → 38% → 15% → 0% przy degradacji jednorodnej, próg niemalejący w 100% przebiegów) | 3.3 (Blokada obniżania progu), 7.3 (`stan_bezpiecznika`), 9.8, 11.5.3, 12.4 (`StanBezpiecznikaWyjscie`), 13.3 (`KALIBRUJ`), 14.1 (E-21), 17.3, 17.7, 21 (R-20, R-22), 22, 23.4 (D-16), ADR-046 (konsekwencje, uzupełnienie it. 6) |
| C4-03 (ŚREDNIA) | Reguła (a): imię i nazwisko z konta, inicjały i oznaczenie klasy nie są treścią matematyczną („Jan Pole” → `tylko_dane_osobowe`, bez `konflikt_filtra`); reguła (b): token Słownika równy nazwisku zastępowany przy sąsiedztwie z drugim członem lub inicjałem (b1) albo w kroku agenta w ramce (b2); Poprawki: reguła (a) nie jest liczona ponownie, krok przywrócony z `tylko_dane_osobowe` nie jest maskowany i podlega limitom 9.4, Sprawdzeniu zgodności i T-7, `konflikt_filtra` tylko z kroków niezmienionych; G-2: osobne kryterium dla ≥ 30 kartek z nazwiskiem-terminem; metryka `uczniowie_z_konfliktem_filtra_udzial` z alertem | 3.3 (Filtr, Słownik terminów matematycznych), 7.3 (`transkrypcja.konflikt_filtra`), 10.3.1 p. 3a, 11.1 p. 7d, 14.1 (E-37), 17.3, 17.6 (G-2), 17.7, 21 (R-19), 22, ADR-042 (uzupełnienie it. 6) |
| C4-04 (ŚREDNIA) | „Dezaktywacja” zamiast „zdjęcie” dla bezpiecznika: `POST /admin/bezpieczniki/dezaktywacja` (oba typy, `DezaktywacjaBezpiecznikaWejscie`), kolumny `dezaktywowany_przez`, `dezaktywowano_at`, `uzasadnienie_dezaktywacji`, DTO `dezaktywowano_at`; „odebranie” flag i „wyłączenie” flagi `diagnostyczne` w prozie; definicja „wstrzymania” (jedno znaczenie) w 3.1 p. 5; nowe pojęcia PZS-1 (86); test CI: słowa zarezerwowane `zdje*`, `wstrzym*` z listami dozwolonych użyć i przypadek kontrolny `zdjeto_at` | 3.1 p. 5, 3.3, 3.4 (część A, test CI części 2 i 3), 7.3 (`stan_bezpiecznika`), 8.4, 11.5.2, 11.5.3, 12.2 (`BEZPIECZNIK_NIEAKTYWNY`, `PULA_DIAGNOSTYCZNA_ZA_MALA`), 12.3, 12.4, 16.4, 23.4 (D-1), ADR-016 (uzupełnienie it. 6), ADR-040, ADR-044 |
| C4-05 (NISKA) | Łączna pojemność kubełków `B_całk` (34 / 10) dzielona między wiersze (`api` 1, pozostałe proporcjonalnie do wagi), przydział `min(formuła, 0,8 × limit − Σ pozostałych)`, publikacja `przydzial`/`pojemnosc` pod blokadą doradczą, zmniejszenie przed i zwiększenie po `COMMIT`, nowy proces z pustym kubełkiem i odnowieniem co 2 s; rachunek granicy dla 2 / 3 / 4 workerów: 74 / 72 / 74 w 1 s (wersja 2.3: 74 / 90 / 106) i ≤ 234 w 5 s; testy (f)–(h), G-3 (c), scenariusz C-40 | 2 (A-01), 7.3 (`limit_szybkosci_modelu`), 13.2, 15.2 (C-40), 17.2, 17.3, 17.4, 17.6 (G-3, G-4), 21 (R-21), 22, ADR-039 (uzupełnienie it. 6) |
| C4-06 p. 1 | `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` w puli S | 10.3.3 |
| C4-06 p. 2 | `UZGODNIJ_REJESTR_RODO` co 5 min z wiekiem > 5 min → okno unieważniania ≤ 10 min, zgodne z 16.4, ADR-041 i G-6 | 13.3, 13.5, 16.4, 21 (R-16), 22, ADR-041 |
| C4-06 p. 3 | Klucz `USUNIECIE_WYKONANE` bez daty: `rejestr-zdarzen-rodo/wykonane/{uzytkownik_id}/{id}.json`; `If-None-Match: *` działa przy ponowieniu w dowolnym dniu | 7.3 (`zdarzenie_rodo.klucz_obiektu`), 13.3, 16.4 (krok 0, procedura p. 2), 22, ADR-041 |
| C4-06 p. 4 | `SELECT status FROM uzytkownik WHERE id = :uczen_id FOR SHARE` po blokadzie `rozwiazanie` i `weryfikacja` w rozstrzygnięciu i korekcie; kolejność blokad bez zakleszczenia | 9.6.2, 9.6.3, 15.2 (C-37) |
| C4-06 p. 5 | Rozmiar testu nazwany wprost: 2,6–7,8% na ocenę przy e = 2% (antykonserwatywna granica Wilsona przy małym `n`), P(≥ 1 wyzwolenie w 90 dniach) 31–45% zależnie od napełnienia okna, 1,1 fałszywego podniesienia progu na kwartał, 11% kwartałów w trybie pełnej weryfikacji | 11.5.3, 17.7, 21 (R-22), 23.4 (D-16), ADR-046 |
| C4-06 p. 6 | Eskalacje `KARENCJA_KONTA_NAUCZYCIELA` poza licznikiem `odsetek_eskalacji_7d` (per Klasa i globalnie); Klasy Nauczyciela w karencji pod kontrolą przyjęć wprost do przywrócenia albo Przekazania Klasy | 3.3 (Kontrola przyjęć), 11.4.3, 13.6, 22, ADR-043 |
| C4-06 p. 7 | Wariant 17.7 „aktywny bezpiecznik poprawek”: start +249 min/dobę (`M` = 3 951, 105 min na Nauczyciela, 20 weryfikatorów), faza ustalona +451 min (`M` = 3 277, 16 weryfikatorów); G-7 = 18 z zapasem 1,22 i kontrolą przyjęć (D-17) | 17.6 (G-7), 17.7, 23.4 (D-17), ADR-043 |
| C4-06 p. 8 | Klucz `limit_szybkosci_modelu (konfiguracja_modelu_id, wykonawca, pula)` z wierszami R (waga 1,0) i S (0,3) dla roli `ROZPOZNAWANIE_PISMA`; `B_całk = 30` dla rozpoznawania | 7.3, 13.4, 22, ADR-039 |
| C4-06 p. 9 | Popyt 28 wywołań na Ocenę (plan 16 + 11 + 1) w S-6, 17.4 i symulacji; 29 wyłącznie jako maksimum pytań; oszacowanie S-6 dla planów 29 (t_O 9,5 s) | 17.2 (S-6), 17.4, 22, ADR-039 |
| C4-06 p. 10 | Okno na rezultat Sprawdzenia stałe `zatwierdzono_at + 6 s`, niezależne od `t_R`; rachunek p_brak ≤ 0,5% dla każdego `t_R` | 9.4, 17.2, 22, ADR-036 (uzupełnienie it. 6) |

Zmiany przekrojowe wersji 2.4: nowe kolumny `limit_szybkosci_modelu.pula`,
`przydzial`, `stan_bezpiecznika.wyjscie_z_blokady_at`; zmienione nazwy
kolumn `stan_bezpiecznika.zdjety_przez` → `dezaktywowany_przez`, `zdjeto_at`
→ `dezaktywowano_at`, `uzasadnienie_zdjecia` → `uzasadnienie_dezaktywacji`;
zmieniony klucz główny `limit_szybkosci_modelu`; usunięty CHECK `typ =
'POPRAWKI' OR NOT aktywny`; nowy CHECK `zdarzenie_rodo.dane ? 'rola'`; nowy
prefiks `rejestr-zdarzen-rodo/wykonane/`; endpoint `POST
/admin/bezpieczniki/zdjecie` zastąpiony przez `POST
/admin/bezpieczniki/dezaktywacja` (typ `DezaktywacjaBezpiecznikaWejscie`);
cykl `UZGODNIJ_REJESTR_RODO` 5 min; scenariusze współbieżności do C-40;
ryzyko R-22; uzupełnienia ADR-016, ADR-036, ADR-039, ADR-041, ADR-042,
ADR-043, ADR-046; decyzje G-0 D-16, D-17; PZS-1: 86 pojęć w części A; słowa
ogólne dopisane do `tests/slownik_tokeny.yaml`: `dezaktywacja`,
`dezaktywowany`, `dezaktywowano`, `dezaktywacji`, `blokada`, `obnizania`,
`przydzial`, `laczna`, `wykonane`, `podpisu`; nowy plik symulacji
`tests/kalibracja/symulacja_bezpiecznika.py --dynamika` (założenia w 11.5.3).
