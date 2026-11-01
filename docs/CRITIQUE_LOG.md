# Dziennik krytyki architektury (CRITIQUE_LOG)

Dokument przeglądany: `docs/ARCHITECTURE_DETAILED.md`.
Punkty odniesienia: `docs/glossary.md`, `docs/requirements.md`, `docs/vision.md`, `docs/architecture.md`.

Każda uwaga ma identyfikator `C<iteracja>-<nr>`, wagę, lokalizację, opis problemu, oczekiwaną poprawkę i status.
Skala wag:
- **KRYTYCZNA**: wymaganie biznesowe nie jest spełnione albo przepływ kończy się w martwym punkcie.
- **WYSOKA**: wewnętrzna sprzeczność, przez którą implementacja się nie uda albo NFR stanie się niemierzalny.
- **ŚREDNIA**: luka w modelowaniu lub nieobsłużony przypadek brzegowy z realnym skutkiem.
- **NISKA**: niespójność redakcyjna lub brakujący element pomocniczy.

---

## Iteracja 1

Data przeglądu: 2026-10-31. Wersja dokumentu: 1.0.

### C1-01: FR-28 nie jest spełnione, bo błąd odczytu nadal obniża Ocenę i Profil ucznia
- **Waga:** KRYTYCZNA
- **Lokalizacja:** 11.1 p. 4 i p. 6; 11.3 T-3; 9.5; ADR-017, ADR-018; 12.3 (brak ścieżki dla Ucznia)
- **Problem:**
  1. Gdy Q-OC-04 = `BLAD_ODCZYTU`, 11.1 p. 6 podmienia `typ_bledu` na „najbardziej prawdopodobny z pozostałych trzech”. Dzieje się tak mimo że model właśnie stwierdził, że to nie jest błąd Ucznia. Jednocześnie Q-OC-03 ≠ `BRAK_BLEDU`, więc 11.1 p. 4 zeruje punkty za wynik końcowy. Ocena wstępna pokazana Uczniowi (FR-24) jest więc obniżona przez błąd odczytu, a Uczeń dostaje fałszywy typ błędu i wskazówkę do nieistniejącego błędu.
  2. Taki wynik trafia do Profilu ucznia z wagą 0,5 (ADR-018, 9.5). FR-28 mówi wprost, że błędy odczytu nie obniżają oceny ucznia, a Profil jest pochodną oceny (FR-29).
  3. T-3 opiera się na `pewnosc_odczytu`, czyli na samoocenie multimodalnego LLM. ADR-004 sam przyznaje, że ta wartość jest nieskalibrowana. Błąd odczytu z wysoką deklarowaną pewnością, którego Uczeń nie zauważył, przechodzi trasą automatyczną do Oceny `OSTATECZNA`.
  4. Po wystawieniu Oceny Uczeń nie ma żadnego endpointu, którym mógłby zgłosić błąd odczytu („odwołanie”). FR-28 zależy więc wyłącznie od tego, czy Uczeń zauważy błąd przed zatwierdzeniem. ADR-017 przerzuca odpowiedzialność na Ucznia, a FR-28 tego nie dopuszcza.
  5. Z-1 („każda decyzja wpływająca na trasę pochodzi z Modelu System One”) jest naruszona przez decyzje LLM: `czytelne`, `pewnosc_odczytu` i trasowanie T-3.
- **Oczekiwana poprawka:**
  - Q-OC-04 = `BLAD_ODCZYTU` nie może zmniejszać punktów. Należy oznaczyć krok, wymusić Weryfikację i nie obniżać Oceny wstępnej (np. ocenić „w najlepszej wierze” albo pokazać tylko „ocena w toku”).
  - Wynik zadania z Oceny wstępnej z `mozliwy_blad_odczytu` nie może trafiać do Profilu ucznia.
  - Trzeba dodać endpoint zgłoszenia przez Ucznia (np. `POST /rozwiazania/{id}/zgloszenie-bledu-odczytu`) z nowym powodem Weryfikacji, limitem i oknem czasowym.
  - Trzeba zdefiniować metrykę skuteczności T-3 (odsetek błędów odczytu wykrytych dopiero przez Nauczyciela) i powiązać ją z bramką G-2.
  - Z-1 należy przeformułować albo zdecydowanie przenieść na Model decyzyjny.
- **Status:** ZAMKNIĘTA (iteracja 2). Moda Q-OC-04 = `BLAD_ODCZYTU` nie zeruje punktów za wynik i nie ustala `typ_bledu` (11.1 p. 5–7, `krok_podejrzany_odczyt`). Ocena wstępna z `mozliwy_blad_odczytu` jest pokazywana jako `WSTRZYMANA_BLAD_ODCZYTU` (8.2, 12.6) i nie tworzy Wyniku zadania (10.1.3, 8.6). T-3 wynika z decyzji Modelu decyzyjnego (Q-OC-04, Q-OC-06), a `pewnosc_odczytu` tylko dodaje powód (Z-1, 1.2). Jest endpoint zgłoszenia z oknem 14 dni, limitami i zawieszeniem Wyniku (9.12, 11.6). Metryki `bledy_odczytu_niewykryte_30d` i `czulosc_t3` są powiązane z G-2 (11.8). Odstępstwo od literalnego FR-24 przekazano do G-0 (D-3).

### C1-02: Kolejka GLOBALNA to martwy punkt dla Uczniów bez Klasy i wyciek danych osobowych
- **Waga:** KRYTYCZNA
- **Lokalizacja:** 11.4; ADR-021; 12.4 (`WeryfikacjaNaLiscie.uczen`); 12.8; 17.6; R-02
- **Problem:**
  1. Uczeń bez Klasy zawsze trafia do kolejki `GLOBALNA`, a każde jego Zadanie z dowodem trafia tam obowiązkowo (FR-26). Dokument nie gwarantuje, że istnieje choć jeden `weryfikator_globalny`: ADR-021 nazywa to tylko „operacyjnym”, a w G-1…G-6 nie ma bramki. Nie ma też SLA, limitu czasu ani reakcji na pustą pulę. Eskalacja (11.4 p. 4) przenosi pracę do tej samej kolejki `GLOBALNA`, więc niczego nie rozwiązuje. Rozwiązanie może zostać w `OCZEKUJE_NA_WERYFIKACJE` na zawsze, a po 90 dniach znika zdjęcie (ADR-019).
  2. R-02 szacuje 12–18 Weryfikacji na Nauczyciela dziennie. Nie uwzględnia przy tym Kontroli losowej (20% ocen automatycznych na starcie) ani kolejki globalnej obsługiwanej przez nieokreśloną grupę. Model obciążenia nie jest zamknięty.
  3. `WeryfikacjaNaLiscie` zwraca `imie` i `nazwisko` Ucznia, a `WeryfikacjaSzczegoly` dodaje zdjęcia (które według 16.3 „mogą zawierać imię”) Nauczycielowi spoza Klasy Ucznia. Uczeń nie zgadzał się na udostępnienie danych obcemu Nauczycielowi. Dokument nie podaje podstawy przetwarzania ani pseudonimizacji, co narusza zasadę minimalizacji (NFR-09, 16.3).
- **Oczekiwana poprawka:**
  - Bramka startowa: minimalna liczba weryfikatorów globalnych wyliczona z modelu obciążenia.
  - SLA kolejki globalnej, metryka i alert, gdy pula weryfikatorów jest pusta.
  - Zdefiniowane zachowanie po przekroczeniu SLA, np. jawny status dla Ucznia i decyzja, czy Ocena wstępna staje się ostateczna, za zgodą właściciela produktu.
  - Pseudonimizacja Ucznia w kolejce `GLOBALNA` (bez imienia i nazwiska, zdjęcia z maskowaniem), podstawa prawna w 16.3 i informacja dla Ucznia przy rejestracji.
  - Pełny model obciążenia Nauczycieli z Kontrolą losową.
- **Status:** ZAMKNIĘTA (iteracja 2). Zakres poprawki: bramka G-7 z liczbą wyliczoną z 17.7, skład puli (A-14, ADR-034) z planem awaryjnym `LEMAT_REJESTRACJA_BEZ_KLASY = false`, SLA 72 h, eskalacja KLASA → GLOBALNA → alert, status „opóźniona”, kontrola przyjęć (11.4.3), pseudonim HMAC i zdjęcia maskowane (11.4.4, ADR-031), podstawa prawna i informacja przy rejestracji (16.3). Rezygnacja z finalizacji po SLA jest uzasadniona (FR-26 „zawsze”) i przekazana do G-0 (D-4, D-5). Pozostałe wątpliwości dotyczące modelu obciążenia opisuje C2-12.

### C1-03: Encje i definicje łamią słownik pojęć (Z-6 „Słownik pojęć jest prawem”)
- **Waga:** WYSOKA
- **Lokalizacja:** 3.2, 3.3, 7.3 (`transkrypcja.zrodlo`, `prog_pewnosci`, `ocena.uzasadnienie`, `pochodzenie_zadania`, `typ_elementu_sciezki`), 12.4
- **Problem:** lista naruszeń:
  1. **Transkrypcja.** Słownik: „uzyskana ze zdjęcia przez moduł rozpoznawania pisma”. Dokument tworzy Transkrypcje o źródle `POPRAWKA_UCZNIA` i `WPIS_RECZNY_UCZNIA`, czyli w ogóle nie pochodzące ze zdjęcia.
  2. **Weryfikacja.** Słownik: sprawdzenie „gdy pewność systemu jest zbyt niska”. Dokument używa tej samej encji dla `KONTROLA_LOSOWA` (pewność wysoka), `BLAD_PRZETWARZANIA` (brak Oceny) i FR-25/FR-26. Wymagania częściowo to wymuszają, ale dokument nie zgłasza, że definicja słownikowa musi się zmienić.
  3. **Model System One → port `ModelDecyzyjny`** (3.2). Według 3.3 ten port implementuje również LLM zapasowy, czyli coś, co z definicji nie jest Modelem System One. Pojęcie słownikowe zostało zmapowane na abstrakcję o innym znaczeniu.
  4. **Próg pewności → tabela `prog_pewnosci`**, która przechowuje też `kalibrator`, `ece` i `gorna_granica_bledu_auto`. To agregat Kalibracji, a nie Próg pewności. Nazwa zakrywa drugą odpowiedzialność.
  5. **Ocena.** Słownik: „liczba punktów wraz z uzasadnieniem i wskazaniem błędnego kroku”. Kolumna `ocena.uzasadnienie` istnieje, ale nie ma jej w żadnym DTO (`OcenaWyjscie`, `WeryfikacjaSzczegoly`), więc uzasadnienia nikt nie widzi.
  6. **Zadanie bazowe.** Słownik: „z arkuszy CKE lub dodane ręcznie przez nauczyciela”. `POST /zadania` jest dostępne także dla roli A, a Zadanie Administratora dostaje `BAZOWE_NAUCZYCIEL`. Pochodzenie jest fałszywe.
  7. **Ścieżka nauki.** Słownik: „kolejność wkładek teoretycznych i zadań zaproponowana przez agenta rekomendującego”. Dokument dodaje typ `POWTORKA` jako osobny byt oraz pozycje Przypisań (`przypisanie_id`), które proponuje Nauczyciel, a nie agent.
  8. 33 pojęcia z 3.3 (m.in. Klasa, Odpowiedź, Przypisanie, Kontrola losowa, Opinia pomocnicza) są nazwami tabel i API. Nie ma ich w słowniku, a dokument jedynie „rekomenduje” ich dopisanie. Przy Z-6 to niespójność blokująca.
- **Oczekiwana poprawka:** dla każdego punktu albo zmienić model, albo przygotować formalną propozycję zmiany `docs/glossary.md`, zaakceptowaną przed implementacją. Konkretnie:
  - Wydzielić `kalibrator` z `prog_pewnosci`.
  - Udostępnić `uzasadnienie` w DTO.
  - Dodać pochodzenie `BAZOWE_ADMINISTRATOR` albo zabronić roli A tworzenia Zadań.
  - Przemapować „Model System One” na adapter `JevAdapter`, a nie na port.
- **Status:** ZAMKNIĘTA (iteracja 2). Powstała formalna propozycja PZS-1 (3.4, B-1…B-7, 61 pojęć w 3.3) z bramką G-0 (D-1). `kalibrator_pewnosci` wydzielono z `prog_pewnosci`. `uzasadnienie` jest w `OcenaWyjscie` (11.7). `POST /zadania` jest dostępne tylko dla roli N (ADR-028). „Model System One” → `JevAdapter` (3.2, B-7). Powtórka to `cel_elementu`. Nowe, pojedyncze niezgodności ze słownikiem opisuje C2-11.

### C1-04: Homonimy w języku wszechobecnym, czyli ten sam termin w różnych znaczeniach
- **Waga:** WYSOKA
- **Lokalizacja:** 13.1 (`class Odpowiedz`), 7.3 (`decyzja_modelu.odpowiedz`, `uczen.procent_wariantu`, `kryterium_oceniania.nr_kroku`, `kolejka_weryfikacji`), 10.4.1, 10.3.1, 7.5.3, 12.4 (`RozstrzygniecieWejscie`)
- **Problem:**
  1. **Odpowiedź.** 3.3 definiuje ją jako wybór Ucznia w Zadaniu zamkniętym (tabela `odpowiedz`). Tymczasem port modeli ma `class Odpowiedz` (odpowiedź modelu), a `decyzja_modelu.odpowiedz` to wynik pytania do Jev. W kodzie będą dwie różne klasy `Odpowiedz`, co jest wprost sprzeczne z regułą 3.1.
  2. **Wariant.** 3.3: „Wariant rozwiązania”. `uczen.procent_wariantu` to wariant eksperymentu A/B (JEV_CHOICE).
  3. **Krok.** 3.3: „Krok rozwiązania = krok Transkrypcji”. `kryterium_oceniania.nr_kroku` to numer kryterium. `RozstrzygniecieWejscie.kryteria[].nr_kroku` (kryterium) leży obok `pierwszy_bledny_krok` (krok Transkrypcji) w jednym DTO. Nauczyciel i kod będą je mylić.
  4. **Wynik.** „Wynik zadania” (3.3) to znormalizowany rezultat. `WynikOceny`, `WynikRozpoznawania`, `WynikRownowaznosci` i `wynik_walidacji` mają zupełnie inne znaczenia. „Wynik końcowy” jest zdefiniowany jako odpowiedź w Rozwiązaniu Ucznia, a kolumna `zadanie_wersja.wynik_koncowy` przechowuje wynik wzorcowy.
  5. **Kolejka weryfikacji.** W 3.3 to lista oczekujących Weryfikacji, a enum `kolejka_weryfikacji` oznacza typ kolejki (KLASA/GLOBALNA).
  6. Domenowe parametry modelu (`theta`, `trudnosc_b`, `waga_ipw`, `ece`) mają nazwy nie-polskie lub nie-słownikowe, mimo że według 3.1 p. 2 wyjątkami mogą być tylko `id`, `created_at`, `updated_at` i `version`.
- **Oczekiwana poprawka:**
  - Zmienić nazwy: `OdpowiedzModelu`/`DecyzjaSkladowa`, `procent_grupy_eksperymentu`, `nr_kryterium`, `WynikRozpoznawaniaPisma` → np. `RezultatRozpoznawania` itd., `zadanie_wersja.wynik_koncowy_wzorcowy`, enum `typ_kolejki_weryfikacji`.
  - Dodać do 3.1 jawną listę dopuszczonych terminów technicznych (θ, b, IPW, ECE).
- **Status:** ZAMKNIĘTA (iteracja 2). Wszystkie wskazane homonimy zmieniono: `DecyzjaSkladowa`, `procent_grupy_eksperymentu`, `nr_kryterium`, `Rezultat*`, `wynik_koncowy_wzorcowy`, `typ_kolejki_weryfikacji`. Dodano regułę 3.1 p. 5 i listę terminów technicznych 3.1 p. 6 / PZS-1 C. Nowe homonimy (`odpowiedzi`, `?wariant=`) opisuje C2-11.

### C1-05: `PONOW_OCENE` jest sprzeczne z ograniczeniem CHECK tabeli `weryfikacja`, a jego semantyka jest niezdefiniowana
- **Waga:** WYSOKA
- **Lokalizacja:** 14.2; 7.3 (`weryfikacja` CHECK `(ocena_weryfikowana_id IS NULL) = ('BLAD_PRZETWARZANIA' = ANY (powody))`); 13.3 `PONOW_OCENE`
- **Problem:**
  1. Według 14.2 udane `PONOW_OCENE` „dołącza Ocenę `WSTEPNA` do istniejącej Weryfikacji (powód `BLAD_PRZETWARZANIA` pozostaje)”. Ustawienie `ocena_weryfikowana_id` przy `BLAD_PRZETWARZANIA` w `powody` łamie ten CHECK. Opisany przepływ odzyskiwania jest więc niewykonalny.
  2. Nie wiadomo, co się dzieje, gdy ponowna ocena nie ma powodów T-1…T-4. Czy Ocena zostaje `OSTATECZNA` i Weryfikacja jest anulowana?
  3. Nie wiadomo, czy powstaje Wynik zadania z wagą 0,5, czy ustawiane jest `ocena_wystawiona_at` i czy idzie powiadomienie Ucznia.
  4. Wyścig: Nauczyciel podjął Weryfikację (`W_TRAKCIE`) i ocenia od zera, a w tym czasie `PONOW_OCENE` zapisuje Ocenę. Warunek wykonania w 13.3 nie sprawdza statusu `W_TRAKCIE`. Nie ma też zapisu warunkowego na `rozwiazanie.version`, więc oba zapisy mogą kolidować na `UNIQUE (rozwiazanie_id, wersja)` albo nadpisać kontekst Nauczyciela.
- **Oczekiwana poprawka:**
  - Przeprojektować CHECK, np. osobna kolumna `brak_oceny_wstepnej` albo CHECK tylko w momencie utworzenia.
  - Spisać pełną semantykę `PONOW_OCENE` (wynik zadania, powiadomienia, możliwa trasa automatyczna).
  - Dodać warunek „Weryfikacja w stanie `OCZEKUJE`” i zapis warunkowy na `version`, a także scenariusz C-xx w 15.2.
- **Status:** ZAMKNIĘTA (iteracja 2). CHECK `ocena_weryfikowana_id IS NOT NULL OR 'BLAD_PRZETWARZANIA' = ANY(powody)` (7.3) jest zgodny z przepływem. Pełna semantyka obu tras jest w 9.13 i 14.2 (Wynik zadania, `AKTUALIZUJ_PROFIL`, powiadomienie, anulowanie przed `INSERT` Kontroli). Jest warunek `OCZEKUJE` i zapis warunkowy na `version`, a także scenariusz C-23.

### C1-06: Wygasła blokada Weryfikacji zostawia ją na zawsze w `W_TRAKCIE`, a przedłużenie blokady jest niemożliwe
- **Waga:** WYSOKA
- **Lokalizacja:** 8.3; 9.6 (SQL podjęcia); 11.4 p. 4; 13.3, 13.5; 12.3
- **Problem:**
  1. Przejście `W_TRAKCIE → OCZEKUJE: wygaśnięcie blokady` (8.3) nie ma wykonawcy: w 13.3/13.5 nie ma pracy w tle, która zmienia status. Stan zmienia się tylko leniwie, przy kolejnym `podjecie`.
  2. Kolejka w 9.6 jest pobierana z filtrem `?status=OCZEKUJE`, więc porzucona Weryfikacja (Nauczyciel zamknął kartę) znika z kolejki dla wszystkich.
  3. `ESKALUJ_WERYFIKACJE` też patrzy tylko na `OCZEKUJE`, więc taka Weryfikacja nigdy nie jest eskalowana.
  4. 8.3 deklaruje „przedłużenie blokady przez tego samego Nauczyciela”, ale nie ma na to endpointu. Ponowne `POST /podjecie` przez właściciela zwraca 0 wierszy (`WHERE status = OCZEKUJE OR zablokowana_do < now()`), czyli `409` dla samego siebie.
  5. Przy 15-minutowej blokadzie i ocenianiu dowodu Nauczyciel traci pracę (C-08), jeśli ktoś przejmie Weryfikację.
  6. W SQL podjęcia brakuje warunku `id = :id` i nawiasów, przez co zapis jest niejednoznaczny.
- **Oczekiwana poprawka:**
  - Praca w tle (lub reguła odczytu) zwalniająca wygasłe blokady.
  - Kolejka wyświetlająca `W_TRAKCIE` z `zablokowana_do < now()` jako dostępne.
  - Eskalacja obejmująca takie przypadki.
  - Endpoint `POST /weryfikacje/{id}/przedluzenie` (albo idempotentne podjęcie przez właściciela).
  - Autozapis szkicu rozstrzygnięcia.
  - Poprawiony SQL.
- **Status:** ZAMKNIĘTA (iteracja 2). Dodano regułę odczytu „dostępna” (11.4.2) i `ZWOLNIJ_BLOKADY_WERYFIKACJI` co 1 min (13.5). Eskalacja obejmuje Weryfikacje dostępne. Są endpoint `…/przedluzenie`, idempotentne podjęcie, `szkic_rozstrzygniecia` i poprawiony SQL z `id = :id` (9.6.1). Skutek uboczny (podnoszenie `version` przy przedłużeniu łamie `If-Match` rozstrzygnięcia) opisuje C2-02.

### C1-07: Model zapasowy nie odpowie na pytania zależne w jednym wywołaniu, a najgorszy przypadek przełączenia przekracza 28 s
- **Waga:** WYSOKA
- **Lokalizacja:** 9.9; 13.2 (`ZapasowyLlmAdapter`); 10.4.3 (rundy); 17.2; 17.6 G-4; 22
- **Problem:**
  1. `ZapasowyLlmAdapter` wysyła „te same pytania jednym wywołaniem”. Pytania Agenta oceniającego są jednak zależne: Q-OC-02.Kk zależy od wariantu z Q-OC-00, a Q-OC-04 jest zadawane tylko dla `KROK_j` z Q-OC-03, które jest nieznane w chwili wywołania. Kontrakt nie mówi, jak model zapasowy ma odpowiedzieć na Q-OC-04 o nieznanym kroku ani na Q-OC-02 dla nieznanego wariantu.
  2. Budżet 17.2 (≈ 21 s dla modelu zapasowego) zakłada bezpośrednie użycie modelu zapasowego. Najgorszy przypadek przy zamkniętym obwodzie wygląda tak: Jev zawodzi w rundzie 3 po rundach 1–2 (3 × (3 s + 1 ponowienie 3 s) = do 18 s). Potem cała Ocena jest liczona od nowa modelem zapasowym (do 15 s), a dochodzi Agent wyjaśniający (do 8 s). Razem do 41 s, czyli ponad termin 28 s, co kończy się T-6 i brakiem Oceny.
  3. Nawet awaria Jev już w rundzie 1 daje 6 s + 15 s + 8 s + kolejka ≈ 30 s.
  4. G-4 („Jev niedostępny, 100% ocen w ≤ 30 s”) jest więc nieosiągalne, dopóki circuit breaker nie przejdzie w stan OTWARTY. Według 22 następuje to dopiero po 5 błędach lub ≥ 50% błędów w 30 s.
- **Oczekiwana poprawka:**
  - Wieloetapowy protokół dla modelu zapasowego (rundy albo pytania warunkowe w schemacie) albo wariant „Q-OC-04 dla każdego kroku”.
  - Budżet najgorszego przypadku z przełączeniem w trakcie Oceny.
  - Skrócone timeouty Jev przy zbliżającym się terminie.
  - Agent wyjaśniający poza ścieżką krytyczną terminu (wskazówka dopisywana asynchronicznie).
  - Doprecyzowana G-4 (scenariusz: obwód zamknięty, a Jev zaczyna odpowiadać timeoutem).
- **Status:** ZAMKNIĘTA (iteracja 2). Wprowadzono `PlanPytan` z warunkami głębokości 1 (10.4.4, 13.1) i zwijanie pytań warunkowych dla modelu zapasowego (13.2 p. 1–3). Ponowienie Jev zależy od zapasu do terminu miękkiego. Wskazówka jest poza ścieżką krytyczną (ADR-025). Najgorsze przypadki S-1…S-7 są policzone (17.2), a G-4 ma scenariusz timeoutów przy zamkniętym obwodzie. Niespójność liczby fal Jev opisuje C2-07.

### C1-08: NFR-01 zostało przedefiniowane tak, że jest spełnione z definicji
- **Waga:** WYSOKA
- **Lokalizacja:** A-11; 17.1 (NFR-01); 11.3 T-6; 14.2
- **Problem:**
  1. NFR-01: „Ocena rozwiązania trwa nie dłużej niż 30 sekund”. Dokument mierzy od zatwierdzenia Transkrypcji, czyli wyłącza rozpoznawanie pisma (p95 20 s, timeout 25 s × 3 próby).
  2. Do „sukcesu” zalicza „skierowanie do Weryfikacji”, także T-6, w którym nie ma żadnej Oceny. Dowolna awaria lub przeciążenie (np. wszystkie oceny po terminie) daje 100% zgodności z NFR-01, chociaż żaden Uczeń nie dostał Oceny.
  3. A-11 jest jednostronnym założeniem architekta, bez akceptacji właściciela produktu.
- **Oczekiwana poprawka:**
  - Uzyskać i zapisać akceptację interpretacji NFR-01 albo mierzyć od przesłania zdjęcia (wtedy przebudować budżet).
  - Wyłączyć T-6 z liczenia zgodności z NFR-01.
  - Dodać SLO „odsetek T-6 ≤ X%” i metrykę end-to-end (przesłanie → Ocena widoczna dla Ucznia).
- **Status:** ZAMKNIĘTA (iteracja 2). Pomiar obejmuje rozpoznanie i ocenę (A-11, `czas_systemowy_ms`). T-6 i błędy dostawcy liczą się jako naruszenie (17.1). Dodano SLO `odsetek_t6 ≤ 0,2%`, metrykę end-to-end i termin twardy 120 s zamiast T-6 po 28 s. Interpretacja i zobowiązanie 99% są przekazane do G-0 (D-2).

### C1-09: ZT-1 jest statystycznie za mały dla kryteriów G-1 i 11.5, a NFR-02 nie jest mierzone end-to-end
- **Waga:** WYSOKA
- **Lokalizacja:** A-09; 11.5; 17.6 G-1, G-2; 20
- **Problem:**
  1. ZT-1 ma ≥ 600 Rozwiązań, w tym 70% obliczeniowych, czyli ≈ 420. 80% idzie na dopasowanie kalibratora, a 20% na ECE. Kryterium progu wymaga górnej granicy Wilsona 95% ≤ 2% przy `n_eff ≥ 100` w zbiorze powyżej progu.
  2. Rachunek: przy 0 błędach granica Wilsona ≤ 2% wymaga n ≥ 189. Przy 1,5 błędu na 300 próbek (0,5%) granica wynosi ≈ 2,14%, czyli warunek nie jest spełniony.
  3. W praktyce ZT-1 może dać próg ≥ 0,99 (prawie zero ocen automatycznych, co pogłębia R-02) albo zablokować start (G-1). Trzeba to policzyć osobno dla modelu zapasowego. Dokument nigdzie nie pokazuje, że wielkość ZT-1 wystarcza.
  4. G-1 mierzy ocenianie na **Transkrypcjach referencyjnych**, a G-2 osobno mierzy rozpoznawanie. Zgodność end-to-end (zdjęcie → Transkrypcja zatwierdzona przez Ucznia → Ocena) z Oceną Nauczyciela nie jest nigdzie zmierzona, chociaż tak działa produkcja.
  5. A-09 mówi o „wszystkich trzech rodzajach zadań otwartych”, a są dwa.
- **Oczekiwana poprawka:**
  - Analiza mocy statystycznej: wymagana liczność ZT-1 per (model, rodzaj) przy zakładanej stopie błędów.
  - Zwiększenie ZT-1 albo zmiana kryterium startowego (np. próg początkowy z jawnie zaakceptowanym wyższym odsetkiem Weryfikacji).
  - Metryka i bramka end-to-end.
  - Korekta A-09.
- **Status:** ZAMKNIĘTA (iteracja 2). Tabelę `n_eff` w 11.5.1 sprawdzono niezależnie (Wilson 95%: 189 / 335 / 482 / 753). ZT-1 ma ≥ 700 zadań obliczeniowych i ≥ 250 z dowodem (A-09). Kalibracja korzysta z wartości out-of-fold (CV5). Jest tryb pełnej weryfikacji (D-6) i bramka E2E w G-2. A-09 poprawiono. Niedoszacowaną liczność dla G-1(a) przy dowodach opisuje C2-06.

### C1-10: Pomiar NFR-03 i NFR-04 zależy od Kontroli losowej, której nikt nie musi wykonać
- **Waga:** WYSOKA
- **Lokalizacja:** 11.3 T-5 (priorytet 4); 11.4; 11.5; ADR-007; 16.4; R-02, R-07
- **Problem:**
  1. Kontrola losowa ma najniższy priorytet (4), nie ma terminu ani SLA i podlega tej samej eskalacji. Nauczyciele przeciążeni według R-02 (12–18 Weryfikacji dziennie) będą ją systematycznie pomijać.
  2. Próbka kalibracyjna powstaje tylko po rozstrzygnięciu. Nierozstrzygnięte i `ANULOWANA` kontrole wypadają bez śladu, więc to brak danych zależny od trudności (not-missing-at-random), a wagi IPW tego nie korygują.
  3. Zdjęcia są usuwane 30 dni po `OCENIONE` (16.4). Kontrola wykonana później odbywa się bez zdjęcia, czyli z inną jakością niż wcześniejsze.
  4. Bezpiecznik wymaga `n ≥ 50` w 30 dni. Jeśli kontrole nie są wykonywane, bezpiecznik nigdy się nie uruchamia, a próg pozostaje bez nadzoru.
- **Oczekiwana poprawka:**
  - SLA i termin Kontroli losowej (np. 72 h, przed usunięciem zdjęcia).
  - Metryka odsetka wykonanych kontroli i alert.
  - Reguła „brak wystarczającej liczby kontroli → próg nie może spadać / rośnie”.
  - Dedykowana pula kontrolerów albo wliczenie kontroli do obciążenia w R-02.
  - Jawne uwzględnienie braków odpowiedzi w estymacji.
- **Status:** ZAMKNIĘTA (iteracja 2). Kontrola losowa ma priorytet 3 i termin 72 h (11.3, 11.4.1). Zdjęcia są zachowane do zamknięcia (16.4). Dodano `wykonanie_kontroli_30d` z regułami nadzoru: alert, blokada obniżania progu, bezpiecznik (11.5.3). `π_wykonanie` per koszyk w IPW koryguje braki, a Kontrola jest wliczona w 17.7.

### C1-11: Test diagnostyczny i odtwarzanie Profilu ucznia liczą wyniki podwójnie
- **Waga:** WYSOKA
- **Lokalizacja:** 9.1; 10.1.3 („Odtwarzanie”, „Test diagnostyczny”); 7.3 (`test_diagnostyczny`, `ux_test_w_toku`, `opanowanie_umiejetnosci.theta_poczatkowa`); 15.2 C-11
- **Problem:**
  1. Po teście `theta_poczatkowa := theta`. Jeśli Uczeń miał już Wyniki zadania przed testem (test pominięty i wykonany później albo powtórzony), `theta_poczatkowa` zawiera ich wpływ. Odtwarzanie stosuje je ponownie, „wszystkie nieunieważnione Wyniki zadania spoza Testu diagnostycznego”, więc są liczone podwójnie.
  2. Powtórzenie testu jest możliwe, bo `ux_test_w_toku` blokuje tylko równoległy `W_TOKU`. Każde powtórzenie nadpisuje `theta_poczatkowa` bez reguły.
  3. Zakończenie testu aktualizuje `theta` bezpośrednio w `api` (9.1), z pominięciem `AKTUALIZUJ_PROFIL` i `FOR UPDATE` z C-11. To wyścig z równoległą aktualizacją Profilu.
  4. Odpowiedzi testu nie tworzą `wynik_zadania`. Reguła R-WKLADKA dla `inicjalizacja = TEST_DIAGNOSTYCZNY` ma więc puste fakty (patrz C1-18).
  5. Zmiana `poziom_matury` po teście (PATCH) nie ma zdefiniowanego skutku dla `opanowanie_umiejetnosci` ani dla testu.
- **Oczekiwana poprawka:**
  - Odtwarzanie od stanu DOMYŚLNEGO z chronologicznym zastosowaniem odpowiedzi testu jako zdarzeń (Wynik zadania typu diagnostycznego).
  - Reguła „test można wykonać raz albo powtórzenie resetuje…”.
  - Aktualizacja Profilu z testu przez tę samą pracę w tle i blokadę.
  - Zdefiniowany skutek zmiany poziomu matury.
- **Status:** ZAMKNIĘTA (iteracja 2). Usunięto `theta_poczatkowa`. Odtwarzanie biegnie od stanu DOMYŚLNEGO z Wynikami testu jako zdarzeniami (`wynik_zadania.test_diagnostyczny_id`, 10.1.3). Test jest jeden na poziom matury (`ux_test_jeden`). Zakończenie testu przechodzi przez `AKTUALIZUJ_PROFIL` (9.1). Skutek zmiany poziomu matury jest zdefiniowany w 9.1.

### C1-12: RODO (NFR-09) nie obejmuje wszystkich miejsc z danymi Ucznia, a odtwarzanie z kopii zapasowej jest logicznie błędne
- **Waga:** WYSOKA
- **Lokalizacja:** 16.4; 7.3 (`decyzja_modelu`, partycjonowana, bez FK); 9.10; 15.2 C-20; A-07
- **Problem:**
  1. `decyzja_modelu.stan` przechowuje pełną Transkrypcję (treść pracy Ucznia) przez 180 dni. Usunięcie konta (`USUN_KONTO`) jej nie obejmuje: tabela 16.4 nie wymienia `decyzja_modelu`, nie ma FK, a dla kontekstu `REKOMENDACJA` nie ma nawet `rozwiazanie_id` ani `uczen_id`, po którym można by usuwać.
  2. Skrypt `ponow_usuniecia` po odtworzeniu z PITR czyta rejestr usunięć z `dziennik_audytu`. Ta tabela jest w tej samej bazie, więc odtworzenie do chwili sprzed usunięcia kasuje też wpis o usunięciu. Mechanizm nie działa dokładnie w scenariuszu, dla którego powstał.
  3. Dokument nie opisuje wersjonowania i kopii magazynu obiektów, czyli czy `DELETE` w S3 rzeczywiście usuwa zdjęcie.
  4. Retencja zdjęć u dostawcy LLM (A-07 mówi tylko o „wyłączonym trenowaniu”, a nie o czasie przechowywania) nie jest określona. Wymóg „zdjęcia są usuwane po określonym czasie” obejmuje też podmiot przetwarzający.
  5. 16.4 każe usuwać zdjęcia `ANULOWANE` natychmiast, ale C-20 nie ustawia `usun_po := now()` (robi to tylko `PORZUC_NIEZATWIERDZONE`), więc 9.10 usunie je po 30 dniach.
- **Oczekiwana poprawka:**
  - Dodać `uczen_id` (lub pseudonim) do `decyzja_modelu`, a `USUN_KONTO` ma zerować `stan` dla wszystkich kontekstów.
  - Rejestr usunięć poza bazą odtwarzaną (np. magazyn obiektów z blokadą zapisu albo osobna baza).
  - Polityka wersjonowania i cyklu życia bucketu.
  - Wymagana retencja po stronie dostawców w A-07 i G-6.
  - Poprawione C-20.
- **Status:** ZAMKNIĘTA (iteracja 2). `decyzja_modelu.uczen_id` i krok 2 `USUN_KONTO` zerują `stan`. Rejestr usunięć jest w buckecie z Object Lock poza bazą. Buckety ze zdjęciami nie mają wersjonowania i mają regułę cyklu życia. Retencję u dostawców określają A-07, walidacja aktywacji i G-6. C-20 ustawia `usun_po := now()`, a ADR-035 wprowadza `kopia_treningowa`. Nową lukę (utrata żądań usunięcia przy PITR) opisuje C2-09.

### C1-13: FR-25 („zawsze”) jest realizowane przez modę jednego pytania, a nie niezależnie od pewności
- **Waga:** ŚREDNIA
- **Lokalizacja:** 11.3 T-2; 11.1; 11.2.1 N-4
- **Problem:** T-2 odpala się tylko wtedy, gdy Q-OC-01 = tak (moda). Przykład: SymPy zwraca `NIEROZSTRZYGNIETE` dla wyniku, a Q-OC-01 = nie z p = 0,92. Jeśli próg wynosi 0,90 i pozostałe p są wysokie, Rozwiązanie z faktycznie poprawnym wynikiem i błędnym krokiem przechodzi automatycznie (ocena z 0 pkt za wynik). FR-25 wymaga Weryfikacji „niezależnie od pewności oceny”, a tutaj decyzja o FR-25 sama zależy od pewności jednej odpowiedzi.
- **Oczekiwana poprawka:** T-2 ma się uruchamiać, gdy `SymPy = ROWNOWAZNE` **lub** `p_tak(Q-OC-01) ≥ τ` (np. 0,2) przy jednoczesnym błędnym lub brakującym kroku. Próg τ należy umieścić w parametrach (22) i uzasadnić.
- **Status:** ZAMKNIĘTA (iteracja 2). T-2 w 11.3 uruchamia się przy SymPy `ROWNOWAZNE` lub p_tak(Q-OC-01) ≥ τ_FR25 oraz przy błędnym lub brakującym kroku z τ_FR25 (lub kryterium nie-końcowym < max). τ_FR25 = 0.20 jest uzasadnione i ujęte w 22.

### C1-14: Luki w FR-27: wskazówka i uzasadnienie bywają nieaktualne albo nie ma ich wcale
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.5.1; 11.2.1 (N-1); 12.4 `RozstrzygniecieWejscie.wskazowka`; 9.6; 14.2
- **Problem:**
  1. Przy rozstrzygnięciu `wskazowka = null` oznacza „zachowaj wskazówkę z Oceny weryfikowanej”. Jeśli Nauczyciel zmienił `pierwszy_bledny_krok` lub `typ_bledu`, Uczeń dostaje wskazówkę o innym kroku niż w Ocenie.
  2. Przy `BLAD_PRZETWARZANIA` nie ma Oceny weryfikowanej, a wskazówka jest opcjonalna. Ocena ostateczna z błędem może więc nie mieć wskazówki, mimo że FR-27 jej wymaga.
  3. Przy N-1 (Q-OC-03 = `BRAK_BLEDU`, a punkty niższe od maksimum) Ocena wstępna odejmuje punkty bez wskazania kroku, typu i wskazówki.
  4. `ocena.uzasadnienie` nie jest eksponowane (patrz C1-03).
- **Oczekiwana poprawka:**
  - Po zmianie błędnego kroku lub typu błędu przez Nauczyciela: wskazówka obowiązkowa albo automatycznie generowana przez Agenta wyjaśniającego na nowych decyzjach.
  - Reguła dla N-1 (komunikat lub wymuszenie decyzji).
  - `uzasadnienie` w DTO.
- **Status:** ZAMKNIĘTA (iteracja 2). Rozstrzygnięcie ma tryby Wskazówki `WLASNA`, `AUTOMATYCZNA` i `ZACHOWAJ`, przy czym `ZACHOWAJ` tylko przy identycznym błędzie (9.6.2, 12.5). Przy `BLAD_PRZETWARZANIA` nie da się pominąć Wskazówki, jeśli jest błąd. Reguła N-1 prowadzi do brakującego etapu i Wskazówki cytującej kryterium (11.1 p. 6, 10.5.3). `uzasadnienie` jest w DTO.

### C1-15: FR-21 jest pokryte częściowo, a reguły punktacji mają niezdefiniowane przypadki
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.4.3 (Q-OC-05.i „maks. 10 par, od początku”); 11.1; 11.2.1 N-2, N-5
- **Problem:**
  1. Transkrypcja może mieć 40 kroków, a Q-OC-05 sprawdza najwyżej 10 par. Wynikanie kroków 11…40, gdy SymPy nie rozstrzyga, nie jest sprawdzane. FR-21 („każdy krok”) nie jest spełnione, a brak sprawdzenia nie obniża pewności.
  2. Przy N-5 (Q-OC-00 = `ZADEN`) nie wiadomo, które kryteria ocenić w Q-OC-02 i jaką wartość wpisać do `ocena.wariant` (NOT NULL).
  3. Nie jest ustalone, czy N-2 liczy się przed czy po wyzerowaniu punktów za wynik (11.1 p. 4). Po wyzerowaniu N-2 praktycznie nie wystąpi.
- **Oczekiwana poprawka:**
  - Niesprawdzone pary mają wymuszać `NISKA_PEWNOSC` albo trzeba zwiększyć limit (z rachunkiem budżetu).
  - Zdefiniować zachowanie przy `ZADEN` (np. Weryfikacja bez punktów i `wariant` nullable).
  - Ustalić kolejność reguł.
- **Status:** ZAMKNIĘTA (iteracja 2). Dodano limit 12 par i N-6 wymuszające Weryfikację (11.1 p. 8). Przy `ZADEN` przyjmowany jest wariant o najwyższym p, a `ocena.wariant` jest wypełnione (11.1 p. 1). N-1 i N-2 są liczone na `punkty_surowe` przed FR-20 (11.1 p. 4).

### C1-16: Pewność oceny jest kalibrowana tylko na sumie punktów, a populacja próbek jest niedookreślona
- **Waga:** ŚREDNIA
- **Lokalizacja:** 7.3 `probka_kalibracyjna.zgodna`; 11.5; zrodlo_probki; 17.1 NFR-02
- **Problem:**
  1. Słownik definiuje Pewność oceny jako prawdopodobieństwo, że **Ocena** jest poprawna, a Ocena obejmuje też wskazanie błędnego kroku (słownik) i typ błędu (FR-27). Kalibrator uczy się jednak tylko `punkty_automatyczne = punkty_nauczyciela`. Ocena z dobrą sumą i złym krokiem lub złą wskazówką liczy się jako „poprawna”. Pole `zgodny_pierwszy_bledny_krok` jest zapisywane, ale nigdzie nie używane.
  2. Nie ma reguły, jak przypisać `zrodlo_probki`, gdy powody są złożone (np. `NISKA_PEWNOSC` + `POPRAWNY_WYNIK_BLEDNE_KROKI`).
  3. Nie wiadomo, czy próbki `KOREKTA_NAUCZYCIELA` (spontaniczne, silnie obciążone selekcją) wchodzą do populacji 11.5. Nie ma też ochrony przed dwiema próbkami dla jednej Oceny (korekta + Kontrola losowa).
- **Oczekiwana poprawka:**
  - Zdefiniować „zgodność Oceny” (punkty + pierwszy błędny krok, ewentualnie typ błędu) albo jawnie zawęzić definicję słownikową.
  - Dodać reguły przypisania źródła próbki, wykluczenia i deduplikacji.
- **Status:** ZAMKNIĘTA (iteracja 2). Zgodność Oceny obejmuje punkty, pierwszy błędny krok i brakujący krok (ADR-027, kolumna generowana). Są reguły źródła próbki (11.5.2), wykluczenie `KOREKTA_NAUCZYCIELA` z progu i deduplikacja `ux_probka_ocena_auto`. Nowe obciążenie selekcją przez zgłoszenia opisuje C2-05.

### C1-17: Rekomendacja działa synchronicznie w `api` z pominięciem Orkiestratora, niezgodnie z widokami architektury
- **Waga:** ŚREDNIA
- **Lokalizacja:** 9.2; 6.1; 6.3; 5.1; 10 (wstęp)
- **Problem:**
  1. Słownik: Orkiestrator „koordynuje współpracę agentów”, a 6.3 umieszcza go w `worker`. W 9.2 `API->>REK` wywołuje Agenta rekomendującego bezpośrednio w procesie `api`, łącznie z wywołaniem Jev (Q-RE-01).
  2. Diagram kontenerów 5.1 nie ma krawędzi `API → JEV`, a diagram 6.1 nie pokazuje modułów `lemat.rekomendacje`, `lemat.powiadomienia` ani `lemat.audyt`.
  3. Nie zdefiniowano timeoutu ani circuit breakera dla Q-RE-01 w ścieżce HTTP, więc żądanie „Dalej” może wisieć do timeoutu Jev.
  4. Tabela `klucz_idempotencji` nie ma właściciela w 6.2.
- **Oczekiwana poprawka:**
  - Ujednolicić: albo rekomendacja jako praca w tle Orkiestratora z odpytywaniem, albo jawny wyjątek w 6.3 z uzasadnieniem.
  - Uzupełnić 5.1 i 6.1.
  - Dodać timeout Q-RE-01 i ścieżkę awaryjną.
  - Przypisać właściciela `klucz_idempotencji`.
- **Status:** ZAMKNIĘTA (iteracja 2). Wyjątek synchroniczny jest jawny i uzasadniony (6.3, ADR-023). Q-RE-01 ma timeout 1,5 s, circuit breaker i powrót do `POLITYKA_ELO`. Krawędź `api → Jev` jest w 5.1, moduły w 6.1 i 6.2, a właściciel `klucz_idempotencji` to `lemat.http`.

### C1-18: Algorytm rekomendacji ma niezdefiniowane przypadki, a uzasadnienia FR-10 nie pokrywają wszystkich ścieżek
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.1.2 kroki 4 i 6; 10.1.5; 10.1.6; 13.3 `UZUPELNIJ_PULE`
- **Problem:**
  1. Gdy wszyscy kandydaci mają `poziom_opanowania ≥ 0,80` (np. opanowany `wybrany_dzial_id`), krok 4 nie wybiera Umiejętności. Kroki 5–6 nie mają wejścia, a odpowiedź `GET …/nastepna` jest niezdefiniowana.
  2. Gdy nie ma żadnego opublikowanego Zadania (start, Umiejętności rozszerzone), krok 6 może nic nie zwrócić. Brakuje kodu błędu i zachowania UI.
  3. Wariant JEV_CHOICE zwraca decyzje (`ZADANIE_LATWE`, `ZADANIE_MATURALNE`), dla których `regula` (Literal) nie ma wartości ani szablonu uzasadnienia. FR-10 jest niepokryte dla tego wariantu.
  4. R-WKLADKA przy `inicjalizacja = TEST_DIAGNOSTYCZNY` i braku Wyników zadania daje „0 z ostatnich 0 zadań…”, bo odpowiedzi testu nie są Wynikami zadania.
  5. Zapotrzebowanie puli w 10.1.2 to (Umiejętność, Poziom trudności, poziom matury), a ładunek i klucz `UZUPELNIJ_PULE` zawierają też `rodzaj`. To niespójność, która powoduje duplikaty lub braki.
- **Oczekiwana poprawka:**
  - Gałęzie „wszystko opanowane” (np. powtórka lub podniesienie trudności) i „brak Zadań” (kod błędu + UI + zlecenie generowania).
  - Reguły i szablony dla JEV_CHOICE.
  - Szablon R-WKLADKA dla przypadku diagnostycznego.
  - Ujednolicony klucz puli.
- **Status:** ZAMKNIĘTA (iteracja 2). Dodano krok 5b z drogą zastępczą i krok 8 (`brak_elementu`, UI, zapotrzebowanie puli). Są reguły i szablony `R-JEV-*` oraz `R-WKLADKA-DIAG` (10.1.5). Klucz puli z rodzajem jest jednolity (10.1.2, 13.3).

### C1-19: FR-06 nie jest zagwarantowane dla poziomu rozszerzonego
- **Waga:** ŚREDNIA
- **Lokalizacja:** 17.6 G-5; 10.2.1 (Wkładki tylko na żądanie Administratora); 14.1 E-20; 8.5
- **Problem:**
  1. G-5 wymaga Wkładek tylko dla Umiejętności poziomu podstawowego. Przy braku Wkładki E-20 po cichu pomija krok 5, więc Uczeń poziomu rozszerzonego może nigdy nie dostać Wkładki teoretycznej, mimo że FR-06 tego wymaga.
  2. Nic automatycznie nie zgłasza braku Wkładki do przygotowania. Jest tylko metryka.
  3. Element `WKLADKA_TEORETYCZNA` w stanie `ROZPOCZETY` nie ma wyjścia, jeśli Uczeń nie kliknie „przeczytane”. Nie ma wygaśnięcia, a element zostaje na zawsze.
- **Oczekiwana poprawka:**
  - G-5 obejmujące poziom rozszerzony (albo jawna akceptacja wyjątku).
  - Automatyczne zlecenie szkicu Wkładki przy E-20.
  - Wygaśnięcie lub zakończenie `ROZPOCZETY` dla Wkładek.
- **Status:** ZAMKNIĘTA (iteracja 2). G-5 obejmuje oba poziomy matury. `GENERUJ_SZKIC_WKLADKI` jest zlecane automatycznie przy E-20. Wkładka w stanie `ROZPOCZETY` ma wyjście (8.5, `WYGAS_ELEMENTY`).

### C1-20: Wygaśnięcie elementu (24 h) i wersjonowanie Zadania niszczą pracę Ucznia na kartce
- **Waga:** ŚREDNIA
- **Lokalizacja:** 8.5; 12.5 (`POST /rozwiazania`); 15.2 C-17; 12.2 `WERSJA_ZADANIA_NIEAKTUALNA`
- **Problem:**
  1. Zadanie otwarte przechodzi do `ROZPOCZETY` dopiero przy utworzeniu Rozwiązania, a samo otwarcie treści tego nie robi. Uczeń, który rozwiązuje na kartce dłużej, niż zostało do `wygasa_at`, dostaje `409 ELEMENT_SCIEZKI_NIEAKTYWNY` i traci pracę.
  2. Jeśli Nauczyciel zapisze Przegląd `POPRAWIONE` w trakcie pisania, `POST /rozwiazania` zwraca `409 WERSJA_ZADANIA_NIEAKTUALNA`, mimo że Uczeń rozwiązywał wersję, którą widział. C-16 zakłada, że ocena idzie według wersji Ucznia, ale walidacja 12.5 na to nie pozwala.
- **Oczekiwana poprawka:**
  - Przejście `PROPONOWANY → ROZPOCZETY` przy otwarciu treści (albo okres karencji).
  - Przyjmowanie poprzedniej wersji Zadania przez określony czas (np. 24 h od publikacji nowej).
- **Status:** ZAMKNIĘTA (iteracja 2). `POST /elementy-sciezki/{id}/rozpoczecie` przy otwarciu treści (+7 dni) ustala `zadanie_wersja_id` (8.5). Reguła wersji akceptowanej obejmuje także okno 72 h dla Przypisań (12.5, C-17).

### C1-21: Brak niezmiennika jednej próby, który pozwala zawyżać Profil i zalewać Nauczyciela duplikatami
- **Waga:** ŚREDNIA
- **Lokalizacja:** 7.3 (`odpowiedz`, `rozwiazanie`: unikalność tylko po `element_sciezki_id`); 12.4 `OdpowiedzWejscie` (oba konteksty opcjonalne); 9.3 (odpowiedź ujawnia klucz)
- **Problem:**
  1. `OdpowiedzWejscie` pozwala odpowiedzieć bez `element_sciezki_id` i bez `przypisanie_id`, bez limitu, a odpowiedź zwraca poprawny klucz. Uczeń może powtarzać Zadanie zamknięte, każda Odpowiedź aktualizuje `theta` (tylko `liczy_do_trudnosci` jest ograniczone do pierwszej próby), więc Profil ucznia jest łatwo zawyżyć.
  2. Dla Przypisań nie ma unikalności (Uczeń, Przypisanie, Zadanie) ani dla `odpowiedz`, ani dla `rozwiazanie`. Wielokrotne Rozwiązania tego samego zadania z dowodem tworzą wiele Weryfikacji dla Nauczyciela.
  3. Natychmiastowe ujawnianie klucza w Przypisaniu pozwala go rozpowszechnić w Klasie.
- **Oczekiwana poprawka:**
  - Wymagany kontekst Odpowiedzi.
  - Indeksy unikalne per (Uczeń, Przypisanie, Zadanie).
  - Reguła wielu prób (np. tylko pierwsza do Profilu albo malejąca waga).
  - Ujawnianie klucza w Przypisaniu dopiero po terminie.
- **Status:** ZAMKNIĘTA (iteracja 3). Sprawdzono w treści: trigger `trg_kontekst_proby` (7.3, BEFORE INSERT na `rozwiazanie` i `odpowiedz`) dziedziczy `przypisanie_id` z elementu `R-PRZYPISANIE` i odrzuca parę niespójną (`KONTEKST_PROBY_NIESPOJNY`), więc `ux_rozwiazanie_przypisanie` i `ux_odpowiedz_przypisanie` obejmują obie drogi (C-30). `rezultat_ujawniony_od = greatest(now(), termin)` dotyczy także elementu (7.3, 9.3, 12.5), a pozycja z Zadaniem zamkniętym jest pomijana po terminie (10.1.2 krok 1, 8.5: `wygasa_at = least(created_at + 24 h, termin)`). Przydział do Nauczyciela Przypisania obejmuje `przypisanie_id` odziedziczone (11.4.1 p. 1). Postęp ma pole `droga` (12.4 `PostepPrzypisaniaWyjscie`). CHECK w `odpowiedz` wylicza kombinacje {E}, {P}, {E, P}, {T}, a 12.5 rozróżnia kontekst podany przez klienta (dokładnie jeden) od kontekstu efektywnego. Wszystkie pięć skutków jest usuniętych.

### C1-22: Nadzór nad treścią: uprawnienia Przeglądu są globalne, a Zadania z dowodem utykają w `DO_PRZEGLADU`
- **Waga:** ŚREDNIA
- **Lokalizacja:** 8.4; 12.3 (`POST /zadania/{id}/przeglad` tylko N; `POST /zadania` N, A); 13.6 `ZadanieDoPrzegladu`
- **Problem:**
  1. Każdy Nauczyciel może poprawić lub wycofać **dowolne** Zadanie z globalnej bazy (także Zadanie bazowe z Arkusza CKE), a skutek dotyczy wszystkich Uczniów platformy.
  2. Zadania `BAZOWE_NAUCZYCIEL` dodane przez jednego Nauczyciela trafiają do globalnego doboru rekomendacji bez przeglądu.
  3. Zadania z dowodem wygenerowane przez system (`UZUPELNIJ_PULE`, `zlecajacy_id = NULL`) powiadamiają Administratorów (13.6), ale endpoint Przeglądu jest dostępny tylko dla roli N. Takie Zadania utykają w `DO_PRZEGLADU`.
  4. 8.4 przewiduje „WYCOFANE: … Administrator”, ale nie ma na to endpointu.
- **Oczekiwana poprawka:**
  - Model własności i zasięgu Zadań (prywatne dla Klasy vs globalne).
  - Rola recenzenta treści.
  - Endpoint Przeglądu i wycofania dla Administratora.
  - Kolejka przeglądu dla Zadań bez zlecającego.
- **Status:** ZAMKNIĘTA (iteracja 2). Dodano zasięg `GLOBALNY`/`PRYWATNY`, rolę recenzenta treści, macierz uprawnień 8.4, endpointy Przeglądu i wycofania dla Administratora oraz powiadomienia dla Zadań bez zlecającego. System nie generuje dowodów (CHECK). Nadmierne zawężenie FR-14 dla zwykłego Nauczyciela opisuje C2-03.

### C1-23: Współbieżna kalibracja Poziomu trudności traci aktualizacje i koliduje z `If-Match` Nauczyciela
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.1.3 (aktualizacja `trudnosc_b`, przeliczanie `poziom_trudnosci`); 7.2 (`version` przy każdej modyfikacji); 13.3 `AKTUALIZUJ_PROFIL` (deduplikacja per Uczeń); 15.2
- **Problem:**
  1. `AKTUALIZUJ_PROFIL` jest deduplikowane per Uczeń, więc wielu workerów równolegle aktualizuje `zadanie.trudnosc_b`, `liczba_wynikow_kalibracji` i `poziom_trudnosci` tego samego Zadania. Przy wzorcu odczyt–modyfikacja–zapis (ORM) aktualizacje giną, a 15.2 nie ma takiego scenariusza.
  2. Według 7.2 każda modyfikacja podnosi `version`, czyli ETag Zadania. Przegląd Nauczyciela (`If-Match`) dostaje wtedy `412` przy każdym nowym wyniku dowolnego Ucznia.
  3. Zmiana `poziom_trudnosci` nie jest wersjonowana (pole jest w `zadanie`, a nie w `zadanie_wersja`) i nie trafia do audytu.
- **Oczekiwana poprawka:**
  - Statystyki kalibracji w osobnej tabeli z atomowym `UPDATE … SET b = b - :d`, bez podnoszenia `zadanie.version`.
  - Nowy scenariusz C-xx w 15.2.
  - Polityka zmiany `poziom_trudnosci`.
- **Status:** ZAMKNIĘTA (iteracja 2). Dodano tabelę `kalibracja_trudnosci` z atomowym `UPDATE` i blokadą wiersza (10.1.3). `zadanie.version` się nie zmienia (7.2). Rozdzielono poziom nominalny i skalibrowany, z histerezą i audytem. Jest scenariusz C-24.

### C1-24: Korekta Oceny równolegle z otwartą Weryfikacją
- **Waga:** ŚREDNIA
- **Lokalizacja:** 8.1 (korekty w `OCENIONE`); 9.6; 12.3 (`If-Match` dla rozstrzygnięcia); 15.2 C-09
- **Problem:**
  1. Korekta (`POST /rozwiazania/{id}/korekty`) jest dozwolona w `OCENIONE` także wtedy, gdy istnieje otwarta Weryfikacja `KONTROLA_LOSOWA`. Korekta nie zamyka ani nie anuluje tej Weryfikacji, więc jej `ocena_weryfikowana_id` wskazuje Ocenę `ZASTAPIONA`.
  2. Późniejsze rozstrzygnięcie Kontroli nadpisze korektę innego Nauczyciela i wygeneruje drugą próbkę dla tej samej Oceny automatycznej.
  3. C-09 zakłada, że obie operacje używają `If-Match` na `rozwiazanie.version`, a 12.3 i 9.6 sugerują ETag Weryfikacji (`WeryfikacjaSzczegoly.version`). Nie wiadomo, który zasób jest blokowany.
- **Oczekiwana poprawka:**
  - Korekta zamyka lub anuluje otwarte Weryfikacje dla Rozwiązania (albo jest zabroniona, gdy Weryfikacja jest otwarta).
  - Jednoznaczne wskazanie zasobu dla `If-Match` w każdym endpoincie.
- **Status:** ZAMKNIĘTA (iteracja 2). Korekta rozstrzyga otwartą Weryfikację albo zwraca `409 WERYFIKACJA_ZAJETA` (9.6.3). Kolumna „ETag” w 12.3 jednoznacznie wskazuje zasób `If-Match`. Są scenariusze C-09 i C-26.

### C1-25: Pojemność workera nie pasuje do A-03, a szczyt po lekcji zamieni się w falę T-6
- **Waga:** ŚREDNIA
- **Lokalizacja:** A-03 (20 jednoczesnych ocen); 5.1; 13.4 (6 slotów szybkich na replikę = 12); 13.3 (`ROZPOZNAJ_PISMO` prio 1, do 25 s × 3); 17.2; 17.4
- **Problem:**
  1. Sloty szybkie są wspólne dla `ROZPOZNAJ_PISMO` (do 25 s), `OCEN_ROZWIAZANIE` i `AKTUALIZUJ_PROFIL`. Jest ich 12, a A-03 zakłada 20 jednoczesnych ocen.
  2. Klasa z 30 Uczniami wysyłającymi zdjęcia naraz zajmuje wszystkie sloty na rozpoznawaniu. Czas w kolejce wlicza się do terminu 28 s (liczonego od zatwierdzenia), więc Rozwiązania trafiają do T-6, czyli do Nauczycieli bez Oceny.
  3. 17.4 liczy tylko przepustowość średnią (≈ 7 200 ocen/h), bez analizy szczytu ani kontroli przyjęć.
  4. Nie jest jasne, czy „slot” oznacza proces, wątek czy korutynę asynchroniczną.
- **Oczekiwana poprawka:**
  - Osobna pula dla `OCEN_ROZWIAZANIE`.
  - Model kolejkowy szczytu (np. 30 Rozwiązań w 60 s).
  - Doprecyzowana semantyka slotu.
  - Bramka G-3 obejmująca scenariusz „koniec lekcji”.
- **Status:** ZAMKNIĘTA (iteracja 2). Pule R/O/P/W są rozłączne, a slot to korutyna z CPU w puli procesów (13.4). Model „koniec lekcji” jest w A-03, 17.2 S-6 i 17.4, a G-3(b) go testuje.

### C1-26: Wpis ręczny Rozwiązania to decyzja produktowa spoza wymagań
- **Waga:** ŚREDNIA
- **Lokalizacja:** 8.1 (`BLAD_ROZPOZNAWANIA → TRANSKRYPCJA_GOTOWA: wpis ręczny Ucznia`); 14.1 E-03, E-04, E-06; 7.3 `zrodlo_transkrypcji.WPIS_RECZNY_UCZNIA`; 23
- **Problem:**
  1. Wizja i słownik: Rozwiązanie to zdjęcie kartki („jak na prawdziwej maturze”).
  2. Wpis ręczny pozwala ominąć kartkę (np. wkleić gotowe rozwiązanie), zmienia charakter danych kalibracyjnych (inny rozkład błędów) i omija T-3 (`pewnosc_odczytu = NULL`).
  3. Changelog nie wymienia go wśród rozszerzeń do potwierdzenia przez właściciela produktu.
- **Oczekiwana poprawka:**
  - Usunąć wpis ręczny albo zgłosić go jako rozszerzenie do akceptacji.
  - Oznaczyć takie Rozwiązania (np. zawsze Weryfikacja lub wykluczenie z Próbek kalibracyjnych).
  - Opisać wpływ na słownik (C1-03).
- **Status:** ZAMKNIĘTA (iteracja 2). Wpis ręczny usunięto (ADR-024, PZS-1 B-1) i przekazano do G-0 (D-7). Uwaga: ten sam efekt można uzyskać przez nieograniczoną edycję Transkrypcji, co opisuje C2-01.

### C1-27: Brakujące endpointy dla operacji przewidzianych w modelu
- **Waga:** NISKA
- **Lokalizacja:** 12.3
- **Problem:** model danych lub maszyny stanów przewidują operacje, które nie mają API:
  - przedłużenie blokady Weryfikacji (8.3);
  - lista Przypisań Nauczyciela (`GET /przypisania`);
  - edycja i usuwanie Zestawu zadań;
  - archiwizacja Klasy (`klasa.zarchiwizowana`);
  - opuszczenie Klasy przez Ucznia;
  - oznaczanie Zadania jako `diagnostyczne` (bez tego 9.1 nie ma puli do losowania);
  - przywrócenie konta w okresie 30 dni (16.4);
  - wycofanie Zadania przez Administratora (8.4).
- **Oczekiwana poprawka:** uzupełnić katalog endpointów albo usunąć nieobsługiwane przejścia i pola.
- **Status:** ZAMKNIĘTA (iteracja 2). Wszystkie 8 wskazanych operacji ma endpointy w 12.3.

### C1-28: Drobne niespójności wewnętrzne
- **Waga:** NISKA
- **Lokalizacja:** 9.6; 7.3; 8.4 vs 9.7; 8.1 vs 13.3; 7.3 `zlecenie_generowania`
- **Problem:**
  1. W 9.6 `INSERT ocena` następuje przed oznaczeniem poprzedniej jako `ZASTAPIONA`. `ux_ocena_aktualna` to częściowy indeks unikalny, którego PostgreSQL nie pozwala odroczyć, więc ta kolejność łamie ograniczenie.
  2. 8.4: `SZKIC → WALIDACJA`, a 9.7 wstawia Zadanie od razu w `WALIDACJA`.
  3. `liczba_prob_rozpoznania` i `proba_nr` nie mają jednoznacznej semantyki: ponowienia pracy w tle to nie to samo co ponowne przesłanie zdjęć (8.1 „3 nieudane próby” vs E-03 „maks. 3 próby łącznie”).
  4. `zlecenie_generowania.liczba_prob` ma `CHECK ≤ 3`, a nie zdefiniowano, kiedy jest inkrementowane względem `max_prob` pracy `GENERUJ_ZADANIE` (ryzyko naruszenia CHECK).
  5. Odpowiedzi Testu diagnostycznego (`odpowiedz.pozycja_testu_diagnostycznego_id ON DELETE CASCADE`) nie mają `element_sciezki_id` ani `przypisanie_id`, a reguła „co najmniej jeden kontekst” nie jest zapisana w CHECK.
- **Oczekiwana poprawka:** poprawić kolejność operacji w 9.6, ujednolicić 8.4 i 9.7, doprecyzować liczniki prób i dodać CHECK kontekstu Odpowiedzi.
- **Status:** ZAMKNIĘTA (iteracja 2). Kolejność „najpierw `ZASTAPIONA`, potem `INSERT`” jest zapisana w 9.6.2 i 15.1 p. 3. 8.4 jest spójne z 9.7. Rozdzielono `liczba_przeslan`, `liczba_ponowien_rozpoznania` i `proby`. Dodano warunkową inkrementację `liczba_cykli` i CHECK kontekstu Odpowiedzi.

### C1-29: Wizja „seria zadań z działu aż do poziomu maturalnego” nie ma odwzorowania, a generowanie systemowe nie ma limitu kosztów
- **Waga:** NISKA
- **Lokalizacja:** 10.1.2; 12.7; 21 R-05
- **Problem:**
  1. Wizja: po Wkładce następuje „seria zadań z tego działu o stopniowo rosnącej trudności, aż do poziomu maturalnego”. Polityka przelicza ranking Umiejętności przy każdym kroku, więc po Wkładce może przeskoczyć do innej Umiejętności. Nie ma pojęcia serii ani warunku jej zakończenia (np. osiągnięcie Poziomu trudności 4).
  2. Limit generowania (12.7) dotyczy tylko Nauczycieli. `UZUPELNIJ_PULE` (system) nie ma dziennego budżetu kosztów, a R-05 wspomina tylko o metryce.
- **Oczekiwana poprawka:**
  - Reguła kontynuacji serii w obrębie Umiejętności lub Działu po Wkładce, z warunkiem wyjścia.
  - Dzienny limit lub budżet kosztów dla generowania systemowego.
- **Status:** ZAMKNIĘTA (iteracja 2). Dodano Serię z warunkiem wyjścia na poziomie maturalnym (ADR-030, 10.1.2 krok 3) i dzienny budżet generowania systemowego: 200 zleceń, 150 PLN (12.7, 13.3).

### Podsumowanie iteracji 1

| Waga | Liczba uwag | Identyfikatory |
|---|---|---|
| KRYTYCZNA | 2 | C1-01, C1-02 |
| WYSOKA | 10 | C1-03 … C1-12 |
| ŚREDNIA | 14 | C1-13 … C1-26 |
| NISKA | 3 | C1-27, C1-28, C1-29 |
| **Razem** | **29** | |

Werdykt: dokument jest obszerny i w wielu miejscach dopracowany (idempotencja, maszyny stanów, porty modeli). Nie jest jednak kompletny na poziomie produkcyjnym:
- dwa wymagania biznesowe (FR-28 oraz obowiązkowa Weryfikacja dla Uczniów bez Klasy) kończą się w martwym punkcie albo nie są spełnione;
- część przepływów odzyskiwania (C1-05, C1-06) jest sprzeczna z własnym DDL lub nie ma wykonawcy;
- pomiar kluczowych NFR (NFR-01, NFR-02, NFR-03) jest nieważny statystycznie albo spełniony z definicji;
- nazwy encji łamią słownik pojęć.

---

## Iteracja 2

Data przeglądu: 2026-11-01. Wersja dokumentu: 2.1. Uwagi C1 zweryfikowano w treści dokumentu (nie tylko w tabeli 23.3). Statusy są wpisane bezpośrednio przy uwagach iteracji 1. Decyzje przekazane do G-0 (D-1…D-9) z wariantem domyślnym nie są traktowane jako błędy. Uwagi poniżej dotyczą miejsc, których 23.4 nie obejmuje.

### C2-01: Nieograniczona edycja Transkrypcji przywraca „wpis ręczny” i pozwala ominąć kartkę
- **Waga:** WYSOKA
- **Lokalizacja:** 12.4 `TranskrypcjaWejscie` („pełna lista kroków po edycji”, n ≤ 40); 9.4; 7.3 `krok_transkrypcji.zmieniony_przez_ucznia`; 11.1 p. 7(c); 11.3; ADR-024; PZS-1 B-1
- **Problem:**
  1. FR-18 pozwala Uczniowi „poprawić błędnie odczytany **fragment**”. `PUT /rozwiazania/{id}/transkrypcja` przyjmuje jednak dowolną nową listę 1–40 kroków. Nie ma limitu liczby zmienionych kroków, porównania z odczytem agenta ani sprawdzenia zgodności ze zdjęciem.
  2. Uczeń może więc sfotografować dowolną kartkę, zastąpić Transkrypcję gotowym, poprawnym rozwiązaniem i zatwierdzić. Kroki zmienione mają `pewnosc_odczytu = NULL`, co 11.1 p. 7(c) jawnie **wyłącza** z podejrzenia odczytu. Q-OC-06 widzi tylko tekst, a nie zdjęcie. Ocena przechodzi trasą automatyczną (FR-23).
  3. To dokładnie ten scenariusz, dla którego ADR-024 usunął wpis ręczny („pozwala ominąć kartkę”, „zmienia rozkład danych kalibracyjnych”). Dokument jest wewnętrznie sprzeczny: B-1 i ADR-024 twierdzą, że Transkrypcja „zawsze pochodzi ze zdjęcia”, a API na to nie pozwala.
  4. Skutki: zawyżone Profile ucznia (FR-29), skażenie Próbek kalibracyjnych (Kontrola losowa obejmuje tylko 5–20%) i rozjechanie się z wizją („jak na prawdziwej maturze”).
- **Oczekiwana poprawka:**
  - Ograniczyć poprawki: zmiana tylko istniejących kroków i fragmentów, limit odsetka zmienionych kroków oraz odległości edycyjnej od odczytu agenta.
  - Dodać sygnał zgodności „Transkrypcja po poprawkach ↔ zdjęcie”, np. drugie wywołanie roli `ROZPOZNAWANIE_PISMA` porównujące zmienione kroki z obrazem. Zgodnie z Z-1 sygnał działa tylko w kierunku bezpiecznym, przez nowy powód Weryfikacji, np. `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`, albo przez wymuszenie Weryfikacji powyżej progu zmian.
  - Wyłączyć takie Oceny z populacji progu (11.5.2) i dodać metrykę odsetka Rozwiązań z poprawkami.
- **Status:** ZAMKNIĘTA (iteracja 3). Poprawka jest związana z krokiem agenta (`nr_kroku_agenta`, 7.3, 12.4), bez usuwania kroków, z limitami twardymi `422 POPRAWKI_ZBYT_ROZLEGLE` liczonymi zawsze względem odczytu agenta bieżącego przesłania (9.4, 12.5) oraz limitami miękkimi i Sprawdzeniem zgodności poprawek ze zdjęciem (10.3.3, `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` w 13.3), które przez T-7 działają wyłącznie w kierunku bezpiecznym (11.3, Z-1). 11.1 p. 7 kieruje kroki zmienione do T-7, więc wyłączenie z (c) nie jest już luką. B-1, ADR-024 i ADR-036 są spójne, a zawężenie FR-18 przekazano do G-0 (D-10). **Ocena odstępstwa:** pozostawienie Ocen automatycznych z Poprawkami w populacji progu przy Kontroli losowej z podwójnym odsetkiem jest poprawne statystycznie, bo `prawdopodobienstwo_doboru` = odsetek efektywny (11.5.2), więc waga IPW koryguje nadreprezentację. Wyłączone są tylko Oceny z T-7, czyli te, które i tak nie byłyby automatyczne. Alternatywa rozwiązuje problem. Nowe luki w limitach (kroki wstawione), czasie Sprawdzenia i bezpieczniku poprawek opisują C3-01 i C3-05.

### C2-02: Przedłużenie i ponowne podjęcie blokady podnoszą `weryfikacja.version`, więc rozstrzygnięcie z `If-Match` kończy się `412`
- **Waga:** WYSOKA
- **Lokalizacja:** 9.6.1 (SQL podjęcia i przedłużenia: `version = version + 1`; pętla „co 5 min” w PWA); 12.3 (`POST …/przedluzenie` → `200 {zablokowana_do}` bez ETag; `…/rozstrzygniecie` z ETag `W`); 9.12 (ostatni akapit); 15.2 C-28; A-13
- **Problem:**
  1. PWA przedłuża blokadę co 5 min, a każde przedłużenie podnosi `version`. Odpowiedź przedłużenia nie zwraca nowego `ETag` ani `version`.
  2. Rozstrzygnięcie wysłane z ETag otrzymanym przy podjęciu dostaje `412 KONFLIKT_WERSJI` w każdej Weryfikacji trwającej dłużej niż 5 min. Średni czas dowodu według A-13 to 5 min, więc dotyczy to większości dowodów i części zadań obliczeniowych. Główny przepływ FR-33/FR-34 kończy się błędem.
  3. Sygnał `412` ma według 9.12 i C-28 oznaczać „pojawiło się zgłoszenie, przejrzyj je”. Przy zmianie wersji przez przedłużenie, ponowne podjęcie i `ZWOLNIJ_BLOKADY` ten sygnał traci znaczenie, a PWA nie ma jak odróżnić przyczyny.
- **Oczekiwana poprawka:**
  - Rozdzielić wersję treści Weryfikacji (powody, zgłoszenie, Ocena weryfikowana, opinia), sprawdzaną przez `If-Match`, od pól blokady, które nie zmieniają `version`. Alternatywnie zwracać `ETag` w odpowiedzi przedłużenia i podjęcia oraz opisać to w PWA.
  - Dodać scenariusz C-xx „rozstrzygnięcie po przedłużeniu”.
- **Status:** ZAMKNIĘTA (iteracja 3). `weryfikacja.version` jest wersją treści ustawianą wyłącznie przez `trg_weryfikacja_wersja` (7.3, wyjątek w 7.2). SQL podjęcia i przedłużenia nie dotyka `version` (9.6.1). Przedłużenie zwraca `{zablokowana_do, version}` i ETag (12.3), a `412` niesie `zmiany_tresci` (8.3). Eskalacja, `termin_at`, priorytet i `opinia_pomocnicza` nie zmieniają wersji (11.4.3). Scenariusz C-31. C-23 opiera się teraz na warunku `status = 'OCZEKUJE'` (9.13). Główny przepływ FR-33/FR-34 nie kończy się już fałszywym `412`.

### C2-03: FR-14 zawężone do recenzentów treści bez decyzji właściciela produktu
- **Waga:** WYSOKA
- **Lokalizacja:** 8.4 (przejście `WALIDACJA → OPUBLIKOWANE (GLOBALNY)` i tabela uprawnień); 12.3 `POST /zadania/{id}/przeglad`, `…/wycofanie`; 12.8; ADR-015, ADR-028; 17.6 G-0; 23.4
- **Problem:**
  1. FR-14: „**Nauczyciel** może przejrzeć, poprawić lub odrzucić wygenerowane zadanie”. Zadanie wygenerowane obliczeniowe lub zamknięte, także zlecone przez Nauczyciela przez `POST /zadania/generowanie`, po Walidacji trafia od razu do `OPUBLIKOWANE` z zasięgiem `GLOBALNY`. Od tej chwili poprawić lub wycofać je może tylko `NR` albo `A`.
  2. Nauczyciel bez flagi `recenzent_tresci` nie może więc poprawić ani odrzucić żadnego Zadania wygenerowanego innego niż dowód, nawet takiego, które sam zlecił i widzi błąd przed użyciem w Klasie. Nie ma też endpointu „zgłoś błąd w Zadaniu” dla zwykłego Nauczyciela.
  3. To zmiana wymagania (prawo przysługuje podzbiorowi ról), której nie ma w G-0 ani w 23.4 (D-1…D-9). ADR-028 nie jest oznaczony „G-0”.
- **Oczekiwana poprawka:**
  - Zadanie zlecone przez Nauczyciela: po Walidacji `DO_PRZEGLADU` dla zlecającego (wynik `PRYWATNY`, promocja do `GLOBALNY` przez NR/A), a dla Zadań `GLOBALNY` endpoint zgłoszenia błędu w Zadaniu z kolejką dla recenzentów.
  - Alternatywnie: jawna pozycja w 23.4 z wariantem domyślnym i akceptacją w G-0.
- **Status:** ZAMKNIĘTA (iteracja 3). Zadanie wygenerowane na zlecenie Nauczyciela zawsze przechodzi `WALIDACJA → DO_PRZEGLADU` z Przeglądem zlecającego (8.4, 9.7, wynik `PRYWATNY`). Dla Zadań `GLOBALNY` jest Zgłoszenie błędu w Zadaniu: tabela `zgloszenie_bledu_zadania` (7.3), endpointy (12.3), uprawnienia (12.8), powiadomienie (13.6), C-36, SLA 5 dni roboczych i wstrzymanie w doborze. ADR-028 jest oznaczony G-0, a zawężenie dla Zadań `GLOBALNY` ma pozycję D-11 z pełną opcją domyślną.

### C2-04: Utracone wybudzenie `AKTUALIZUJ_PROFIL`, przez które Wynik zadania może nie trafić do Profilu ucznia
- **Waga:** ŚREDNIA
- **Lokalizacja:** 7.3 `ux_praca_dedup` (`WHERE status IN ('OCZEKUJE','W_TOKU')`); 13.3 `AKTUALIZUJ_PROFIL` (dedup `uczen_id`); 13.4 (`ON CONFLICT … DO NOTHING`); 15.2 C-11; 13.5 (brak zamiatacza)
- **Problem:** handler `AKTUALIZUJ_PROFIL` w stanie `W_TOKU` wybiera Wyniki z `zastosowany_w_profilu_at IS NULL`. Wynik zatwierdzony przez inną transakcję po tym `SELECT` (READ COMMITTED) nie zostaje przetworzony. Jednocześnie zlecenie nowej pracy przez tę transakcję kończy się `DO NOTHING`, bo istnieje praca `W_TOKU` z tym samym kluczem. Wynik zostaje niezastosowany do czasu następnego zdarzenia Ucznia, a gdy to był ostatni Wynik (np. przed egzaminem), nigdy. Typowy przypadek: zakończenie testu, a zaraz potem Odpowiedź albo Ocena. C-11 obiecuje „każdy Wynik zastosowany dokładnie raz”, ale nie gwarantuje, że Wynik zostanie zastosowany w ogóle (FR-29).
- **Oczekiwana poprawka:**
  - Handler ma działać w pętli do opróżnienia (ponowny `SELECT` po zapisie, w tej samej blokadzie `FOR UPDATE profil_ucznia`) albo deduplikacja ma obejmować tylko `OCZEKUJE`.
  - Dodać okresowy zamiatacz `ix_wynik_do_profilu` w schedulerze i metrykę wieku najstarszego niezastosowanego Wyniku.
- **Status:** ZAMKNIĘTA (iteracja 3). Deduplikacja `AKTUALIZUJ_PROFIL` obejmuje tylko prace `OCZEKUJE` (`ux_praca_dedup_oczekujace`, 7.3; predykat `ON CONFLICT` w 13.4). Handler działa w pętli do pustego wyniku w blokadzie `profil_ucznia` (13.3). Zamiatacz `ZAMIATAJ_PROFILE` działa co 5 min (13.3, 13.5), jest metryka i alert `wynik_najstarszy_niezastosowany_sekundy` (17.3), a scenariusze C-11 i C-32 są opisane. Utracone wybudzenie jest wykluczone na trzech poziomach.

### C2-05: Zgłoszenie błędu odczytu wprowadza nielosowe braki do populacji progu
- **Waga:** ŚREDNIA
- **Lokalizacja:** 11.5.2 (tabela: tylko „dokładnie {`NISKA_PEWNOSC`}” → populacja progu; {`NISKA_PEWNOSC`, `ZGLOSZENIE_BLEDU_ODCZYTU`} → `WERYFIKACJA_INNY_POWOD`); 9.12 (dopisanie powodu do otwartej Weryfikacji); 11.5.3
- **Problem:** Uczeń może dopisać zgłoszenie do otwartej Weryfikacji `NISKA_PEWNOSC` w stanie `OCZEKUJE_NA_WERYFIKACJE`. Takie Rozwiązanie wypada z populacji `WERYFIKACJA_NISKA_PEWNOSC` (π_dobor = 1). Zgłoszenia nie są losowe: Uczniowie zgłaszają Oceny wstępne o niskich punktach, a te częściej są niezgodne z Oceną Nauczyciela. Usunięcie ich z populacji zaniża `blad(t)` w przedziale pod progiem. Przy obniżaniu progu (11.5.3 szuka `t` < bieżącego progu w tych właśnie próbkach) prowadzi to do zbyt niskiego progu, a NFR-03 jest zagrożone. Asymetria jest widoczna w tej samej tabeli: dla Kontroli losowej zgłoszenie dopisane później **nie** wyklucza próbki.
- **Oczekiwana poprawka:** źródło próbki ma wynikać z powodów **w chwili utworzenia** Weryfikacji, na wzór reguły dla Kontroli losowej, czyli {`NISKA_PEWNOSC`, `ZGLOSZENIE_BLEDU_ODCZYTU` dopisane później} → `WERYFIKACJA_NISKA_PEWNOSC`. Trzeba też zapisać w `weryfikacja` pole `powody_poczatkowe` i dodać test jednostkowy reguły.
- **Status:** ZAMKNIĘTA (iteracja 3). Kolumna `weryfikacja.powody_poczatkowe` jest chroniona triggerem `trg_powody_poczatkowe` (7.3) i jest jedyną podstawą źródła Próbki (11.5.2). Zgłoszenie dopisane później nie zmienia źródła ani dla `NISKA_PEWNOSC`, ani dla `KONTROLA_LOSOWA`. Opisano też przypadek `PONOW_OCENE` i test `test_zrodlo_probki.py`.

### C2-06: Liczność ZT-1 dla dowodów nie pozwala spełnić kryterium G-1(a) przy zgodności równej NFR-02
- **Waga:** ŚREDNIA
- **Lokalizacja:** A-09 (≥ 250 dowodów, „liczności wynikają z analizy mocy”); 11.5.1 p. 3 („±3,7 pp przy 90%”); 17.6 G-1(a) (dolna granica Wilsona 95% ≥ 87,5% osobno dla każdego rodzaju)
- **Problem:** przy n = 250 i obserwowanej zgodności 90% dolna granica Wilsona 95% wynosi ≈ 85,7%, czyli mniej niż 87,5% (rachunek krytyka). Spełnienie G-1(a) dla dowodów wymaga obserwowanej zgodności ≥ ≈ 91,6%, czyli realnie modelu lepszego niż NFR-02. Dla 700 zadań obliczeniowych granica 87,5% jest osiągana dokładnie przy 90,0%. Analiza mocy 11.5.1 dotyczy wyłącznie progu (NFR-03), a nie G-1(a). A-09 twierdzi jednak, że obie liczności z niej wynikają. G-1 dla dowodów jest więc nieprzewidywalnym blokerem startu (dotyczy obu modeli decyzyjnych).
- **Oczekiwana poprawka:** analiza mocy dla G-1(a) per rodzaj: albo ≈ 700 dowodów przy kryterium LB ≥ 87,5% przy 90%, albo kryterium „LB ≥ 85%” dla dowodów, uzasadnione tym, że dowody zawsze przechodzą Weryfikację (FR-26). W drugim przypadku decyzja trafia do G-0.
- **Status:** ZAMKNIĘTA (iteracja 3). Tabela 2 w 11.5.1 została sprawdzona niezależnie (Wilson 95%). Przy n = 350 i 90% LB wynosi 86,4%, półszerokość ±3,2 pp (dokument podaje ±3,1, różnica pomijalna). Moc przy rzeczywistych 92% wynosi ≈ 93% (n = 350) i ≈ 97–98% (n = 700). A-09 (≥ 350 dowodów), G-1(a) i D-12 są spójne. Łagodniejsze LB dla dowodów jest uzasadnione FR-26 i przekazane do G-0.

### C2-07: Budżet NFR-01 dla Jev zakłada ≤ 16 wywołań, a plan pytań ma ich do 29
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.4.3 („≤ 29 wywołań w 2 rundach”); 13.2 („w rundzie maks. 8 wywołań równolegle”, semafor 64 **na proces**); 17.2 („Jev (2 rundy, ≤ 2 fale po 8 wywołań)”, p95 3,2 s, S-2 „runda 1 OK 1,6 s”); 17.4; A-01
- **Problem:**
  1. Przy jednym wariancie runda 1 zawiera Q-OC-00/01, 12 × Q-OC-02, Q-OC-03, 12 × Q-OC-05 i Q-OC-06, czyli do 28 pytań, a więc 4 fale po 8. Runda 2 to do 13 pytań, czyli 2 fale. Jeśli API Jev nie przyjmuje wielu pytań w jednym żądaniu (13.2 dopuszcza ten wariant), p95 rośnie z 3,2 s do ≈ 4,8 s (6 fal × 0,8 s). Scenariusze S-1/S-2 (czas do decyzji o ponowieniu) są wtedy liczone dla złej liczby fal.
  2. Limit A-01 (≥ 50 żądań/s **na klucz**) jest pilnowany semaforem per proces (2 repliki × 64). Nie ma globalnego ogranicznika, a `429` liczy się jako błąd circuit breakera. Szczyt „koniec lekcji” (do 44/s według 17.4, przy 6 falach więcej) może więc otwierać obwód i przełączać cały ruch na model zapasowy, chociaż Jev działa.
- **Oczekiwana poprawka:**
  - Przeliczyć 17.2 i S-1…S-3 dla rzeczywistej liczby fal (lub zapisać w A-01 wymaganie wsadowych żądań).
  - Dodać globalny ogranicznik szybkości (np. token bucket w PostgreSQL lub w adapterze z przydziałem per replika) i nie liczyć `429` z własnego ogranicznika jako awarii Jev.
- **Status:** ZAMKNIĘTA (iteracja 3). Skład rund i fal jest policzony jawnie (10.4.3: 27 + 1 albo 16 + 13), są tryby `WSADOWY` i `POJEDYNCZY` (A-01, 13.2), a 17.2 i S-1…S-7 przeliczono dla 3 fal (S-1 sprawdzony: 24,9 s). `429` i brak przydziału nie liczą się do circuit breakera (13.2, 9.9), jest też G-4 (d). **Ocena odstępstwa:** przydział lokalny z rejestru procesów (`limit_szybkosci_modelu`) zamiast token bucketa w PostgreSQL rozwiązuje problem. Suma przydziałów jest ograniczona przez 0,8 × limit, a przy zmianie składu procesów chwilowo przez ≤ 0,96 × limit (N = 4). Nie ma zapisu do bazy na każde żądanie. Sprzeczne formuły przydziału i pojemność kubełka mniejszą od fali opisuje C3-04.

### C2-08: Przywrócenie konta w okresie 30 dni zostawia trwale zawieszone Wyniki i nierozpatrzone zgłoszenia
- **Waga:** ŚREDNIA
- **Lokalizacja:** 16.4 („Usunięcie konta”, `POST /admin/uzytkownicy/{id}/przywrocenie`: „Rozwiązania `ANULOWANE` pozostają anulowane”); 15.2 C-20; 9.12 („zawieszenia nie trzeba cofać”); 11.5.3 (`π_wykonanie` bez `USUNIECIE_KONTA`); 8.1
- **Problem:** dokument zakłada, że anulowanie przy usunięciu konta jest nieodwracalne, a jednocześnie pozwala przywrócić konto. Po przywróceniu:
  1. Wyniki zadania zawieszone przez zgłoszenie (`powod_uniewaznienia = ZGLOSZENIE_BLEDU_ODCZYTU`) pozostają zawieszone na zawsze, bo Weryfikacja jest `ANULOWANA`, a limit „1 zgłoszenie na Rozwiązanie” jest zużyty. Ocena nigdy nie wraca do Profilu (FR-29).
  2. Rozwiązania z `OCZEKUJE_NA_WERYFIKACJE` są `ANULOWANE`, ale mają Ocenę `WSTEPNA` i Wynik z wagą 0,5. Nie wiadomo, czy ten Wynik zostaje w Profilu, czy znika.
  3. Kontrole losowe anulowane z powodem `USUNIECIE_KONTA` są pomijane w `π_wykonanie`, mimo że po przywróceniu konto jest aktywne.
- **Oczekiwana poprawka:** zdefiniować przywrócenie, np. odtworzenie otwartych Weryfikacji (jeśli zdjęcia jeszcze istnieją) albo cofnięcie zawieszenia i zwrot limitu zgłoszenia. Trzeba też ustalić los Wyników z Ocen wstępnych Rozwiązań `ANULOWANE` i dodać scenariusz C-xx „usunięcie → przywrócenie”. Alternatywnie anulowanie procesów można odroczyć do faktycznego `USUN_KONTO`.
- **Status:** ZAMKNIĘTA (iteracja 3). Wybrano wariant zaproponowany przez krytyka (ADR-040): żądanie usunięcia nie zmienia stanów (8.1, 16.4 p. 2), handlery kończą się z `KONTO_USUWANE` (13.3), Kolejka i SQL podjęcia wymagają konta `AKTYWNE` (9.6.1, 11.4.2), a Kontrole kont `USUWANE` są poza `π_wykonanie` (11.5.3). Przywrócenie opisuje tabela 16.4: nowe `termin_at`, ponowne rozpoznanie, T-6, zawieszone Wyniki rozstrzygane normalnie, a anulowanie przeniesiono do kroku 0 `USUN_KONTO`. Scenariusz C-33. Wszystkie trzy skutki z opisu są usunięte. Drobne luki (konto Nauczyciela, rozstrzygnięcie w karencji) opisuje C3-08.

### C2-09: Odtworzenie bazy z PITR cofa żądania usunięcia konta i odwołania zgody
- **Waga:** ŚREDNIA
- **Lokalizacja:** 16.4 („Kopie zapasowe PostgreSQL”, skrypt `ponow_usuniecia`); rejestr `rejestr-usuniec` (wpis dopiero w kroku 1 `USUN_KONTO`); `PATCH /uczniowie/ja` (`zgoda_na_trening = false`); G-6
- **Problem:** rejestr usunięć zapisuje wyłącznie **wykonane** usunięcia (po 30 dniach). Żądanie `DELETE /uczniowie/ja` (stan `USUWANE`) i odwołanie `zgoda_na_trening` istnieją tylko w bazie. Odtworzenie do punktu sprzed takiego żądania przywraca konto jako `AKTYWNE` i zgodę jako `true`. Konto nie zostanie usunięte, a `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` może tworzyć kopie wbrew odwołanej zgodzie (art. 7 ust. 3 i art. 17 RODO). Test G-6 sprawdza tylko scenariusz wykonanych usunięć.
- **Oczekiwana poprawka:** zapisywać do bucketu Object Lock także **żądania** usunięcia, ich anulowanie (przywrócenie) i odwołania zgody. `ponow_usuniecia` ma odtwarzać te zdarzenia. Rozszerzyć G-6 o oba scenariusze.
- **Status:** ZAMKNIĘTA (iteracja 3). Wszystkie Zdarzenia RODO (`typ_zdarzenia_rodo`) są zapisywane do bucketu Object Lock przed transakcją, z kopią w `zdarzenie_rodo` (7.3, 16.4). Procedura `ponow_zdarzenia_rodo` stosuje je przed startem `worker` i `scheduler` (16.4 p. 1–3, C-34). G-6 obejmuje scenariusze (a)–(c). Żądanie usunięcia i odwołanie zgody przeżywają odtworzenie. Regresję dla wykonanych usunięć (brak wiersza `zdarzenie_rodo` dla `USUNIECIE_WYKONANE`, przez co `UZGODNIJ_REJESTR_RODO` unieważnia zdarzenie) opisuje C3-03, a osierocone kopie treningowe po PITR opisuje C3-06.

### C2-10: Dane osobowe z kartki mogą trafić do Transkrypcji, modeli, obcego weryfikatora i zbioru treningowego
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.3.1 p. 2 („przepisanie dosłowne”, ramki danych osobowych tylko dla maskowania zdjęcia); 16.3 („Do modeli nigdy nie są wysyłane: … imię, nazwisko”); 11.4.4; ADR-031 („Transkrypcja zawiera wyłącznie rozwiązanie”); 13.3 `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`
- **Problem:** Agent rozpoznający oznacza obszary danych osobowych tylko po to, żeby zamaskować zdjęcie. Nic nie zabrania mu przepisać imienia, nazwiska ani podpisu do `kroki` w trybie „dosłownym”. Taka Transkrypcja trafia do stanu Jev i promptów LLM (wbrew deklaracji 16.3), do `WeryfikacjaSzczegoly.transkrypcje` obcego weryfikatora globalnego (wbrew 11.4.4) i do kopii treningowej opisanej jako „bez danych identyfikacyjnych”. Twierdzenie z ADR-031 nie jest niczym zagwarantowane.
- **Oczekiwana poprawka:** instrukcja promptu „nie przepisuj treści z obszarów danych osobowych”, post-filtr usuwający z kroków tekst pokrywający się z ramkami (oraz imię i nazwisko z konta Ucznia, porównanie po stronie Lemat), metryka i test na ZT-1 (G-2/G-6).
- **Status:** ZAMKNIĘTA (iteracja 3). Prompt zawiera regułę „nie przepisuj obszarów danych osobowych” (10.3.1 p. 2). Deterministyczny Filtr danych osobowych działa przed zapisem każdej Transkrypcji i Poprawki oraz ponownie przed kopią treningową (10.3.1 p. 3a, 9.4, 13.3). Są kolumna `dane_osobowe_usuniete`, metryka, test G-2/G-6, a deklaracje w 16.3 i ADR-031 są poprawione. Pierwotny wyciek jest zablokowany. Skutki uboczne filtra dla treści matematycznej (FR-28, FR-18) opisuje C3-02.

### C2-11: Nowe naruszenia słownika i reguły „jedno słowo — jedno znaczenie”
- **Waga:** ŚREDNIA
- **Lokalizacja:** 3.1 p. 5; 3.3; 3.4 (część A, test CI); 7.3 (`klucz_idempotencji.kod_odpowiedzi`, `cialo_odpowiedzi`; `zadanie_wersja.odpowiedzi_zamkniete`); 13.2 (schemat modelu zapasowego `{"odpowiedzi":[…]}`); 12.3 (`GET /rozwiazania/{id}/zdjecia/{zdjecieId}?wariant=MASKOWANE`); 6.3, 13.4, ADR-002 („kolejka prac w tle”); E-13 (`wynik_przestarzaly`); glossary: Agent, Jev
- **Problem:**
  1. **Odpowiedź** (3.3: wybór Ucznia w Zadaniu zamkniętym, „nie jest używane dla odpowiedzi modelu”) występuje jako odpowiedź modelu w kluczu schematu `odpowiedzi` adaptera zapasowego (dokładnie homonim z C1-04 p. 1), jako odpowiedź HTTP w `kod_odpowiedzi` i `cialo_odpowiedzi`, a jako opcje i klucz Zadania w `odpowiedzi_zamkniete`.
  2. **Wariant** (zarezerwowany dla Wariantu rozwiązania) jest parametrem API `?wariant=MASKOWANE`.
  3. **Kolejka** (zarezerwowana dla Kolejki weryfikacji) opisuje mechanizm `praca_w_tle`. **Wynik** pojawia się w `wynik_przestarzaly` (rezultat techniczny, wbrew 3.1 p. 5) i w `OdpowiedzWyjscie.wynik`.
  4. W 3.3 brakuje pojęć użytych w identyfikatorach: Pewność surowa (`pewnosc_surowa`), Punkty surowe (`punkty_surowe`), Niespójność (`niespojnosci`, N-x), Rozstrzygnięcie (`/rozstrzygniecie`, `RozstrzygniecieWejscie`), Korekta (`/korekty`, `KOREKTA_NAUCZYCIELA`), Eskalacja (`eskalowano_at`). 3.4 twierdzi, że przejrzano „wszystkie nazwy tabel, kolumn, endpointów i parametrów”, ale test CI porównuje tylko nazwy **tabel**.
  5. Definicje słownikowe, których PZS-1 nie zmienia, mimo że model je przekracza: „Agent” wylicza cztery zadania (bez Agenta wyjaśniającego), a „Jev” jest „silnikiem decyzyjnym agenta oceniającego”, podczas gdy dokument używa go także w rekomendacji (Q-RE-01) i Walidacji (Q-WA-*).
- **Oczekiwana poprawka:**
  - Zmienić nazwy: `odpowiedzi` → `decyzje_skladowe` w schemacie adaptera, `kod_odpowiedzi_http`/`cialo_odpowiedzi_http` (albo uznać je za kolumny techniczne w 3.1 p. 2), `odpowiedzi_zamkniete` → `opcje_zamkniete`, `?wariant=` → `?wersja_zdjecia=`, `wynik_przestarzaly` → `rezultat_przestarzaly`.
  - Dopisać brakujące pojęcia do 3.3 i PZS-1 A, a definicje Agenta i Jev do PZS-1 B.
  - Rozszerzyć test CI o kolumny, typy wyliczeniowe i ścieżki OpenAPI.
- **Status:** ZAMKNIĘTA (iteracja 3). Sprawdzono wszystkie wskazane zmiany: `decyzje_skladowe` (13.2), `kod_statusu_http`/`cialo_http` (7.3), `opcje_zamkniete` (7.3, 7.5.1), `?wersja_zdjecia=` (12.3), `rezultat_przestarzaly` (6.3, E-13), `rezultat_ujawniony_od` i `OdpowiedzWyjscie.rezultat` (7.3, 12.4), „prace w tle” (5.1, 6.3, 13.4), 17 nowych pojęć w 3.3, B-8/B-9 w PZS-1 oraz test CI z tokenami kolumn, typów, OpenAPI i słowami zarezerwowanymi (3.4). Nowe naruszenia wprowadzone w wersji 2.2 i pozostałości opisuje C3-07.

### C2-12: Model obciążenia Nauczycieli pomija napływ eskalacji i tygodniowy rytm pracy
- **Waga:** ŚREDNIA
- **Lokalizacja:** 17.7; 11.4.3 p. 1 (eskalacja KLASA → GLOBALNA po 72 h); 17.6 G-7; A-13; R-02
- **Problem:**
  1. Kolejka `GLOBALNA` jest wymiarowana wyłącznie na 20% wolumenu Uczniów bez Klasy (719 min/dobę). Weryfikacje z Klas eskalowane po SLA trafiają do tej samej puli, a model ich nie uwzględnia. Wystarczy, że kilku z 40 Nauczycieli (72 min/dobę na starcie) nie nadąża, i pula 12 weryfikatorów zostaje przeciążona: kontrola przyjęć działa tylko wobec Uczniów bez Klasy i nie hamuje napływu eskalacji.
  2. Model liczy obciążenie na 7 dni, a Nauczyciele pracują zwykle 5 dni. Rozwiązania z piątku i weekendu przekraczają SLA 72 h i masowo eskalują w poniedziałek. Realne obciążenie dnia roboczego to ≈ 7/5 wartości z tabeli (≈ 100 min zamiast 72 min na Nauczyciela).
  3. G-7 jest więc spełnialne „na papierze”, a pierwszy tydzień pilotażu może dać lawinę `opozniona` i `ALERT_WERYFIKACJE`.
- **Oczekiwana poprawka:** model z rozkładem tygodniowym (5/7) i założonym odsetkiem eskalacji z Klas, uwzględnienie eskalacji w `zaleglosc_globalna_min` i w progu G-7, SLA liczony w dniach roboczych albo jawna akceptacja ryzyka w G-0.
- **Status:** ZAMKNIĘTA (iteracja 3). SLA wynosi 3 dni robocze (`dodaj_dni_robocze`, `dzien_wolny` w 7.3; 11.4.1 p. 4). Model tygodniowy 7/5 ma jawne 5% eskalacji (A-15, 17.7). Rachunki sprawdzono: M = 3 702 / 2 826 / 3 093 min, 98 / 75 / 82 min na Nauczyciela, a G-7 = 18 / 14 / 16 / 4. `zaleglosc_globalna_min` obejmuje eskalacje (11.4.3), jest kontrola przyjęć per Klasa, a decyzja trafiła do D-13.

### C2-13: Test diagnostyczny (FR-03) nie ma gwarancji puli Zadań, ścieżki błędu ani akceptacji pominięcia
- **Waga:** ŚREDNIA
- **Lokalizacja:** 9.1 („losuj 2 Zadania diagnostyczne na Dział, Poziom trudności 2 i 4”); 12.3 `PUT /admin/zadania/{id}/diagnostyczne`, `POST …/pominiecie`; 17.6 G-5; 12.2; 23.4
- **Problem:**
  1. Żadna bramka nie wymaga, by dla każdego Działu i poziomu matury istniały Zadania `diagnostyczne = true` na Poziomach trudności 2 i 4. G-5 sprawdza tylko Wkładki i pulę ogólną.
  2. Nie zdefiniowano odpowiedzi `POST /uczniowie/ja/test-diagnostyczny`, gdy puli brakuje (kod błędu, test częściowy, czy pominięcie Działu). Przepływ FR-03 może skończyć się w martwym punkcie od pierwszego dnia.
  3. Nie wiadomo też, czy losowanie używa poziomu nominalnego, czy skalibrowanego, który zmienia się w czasie.
  4. Pominięcie testu (FR-03: „nowy uczeń rozwiązuje…”) jest rozszerzeniem wymagań, którego nie ma w G-0.
- **Oczekiwana poprawka:** bramka (np. w G-5) „≥ 2 Zadania diagnostyczne na (Dział, poziom matury, Poziom trudności 2/4)”, zdefiniowane zachowanie przy niepełnej puli, jednoznaczny wybór poziomu i pozycja w 23.4 dla pominięcia testu.
- **Status:** ZAMKNIĘTA (iteracja 3). G-5 wymaga ≥ 3 Zadań diagnostycznych na trójkę (72 / 78, 17.6). Dobór działa po poziomie nominalnym, z poziomami zastępczymi i `pominiete_dzialy` (9.1, 7.3). Przy pokryciu < 50% zwracany jest `409 TEST_NIEDOSTEPNY` ze stanem `NIEDOSTEPNY` w `UczenWyjscie` (12.2, 12.4). Są też `SPRAWDZ_PULE_DIAGNOSTYCZNA` (13.3), `409 PULA_DIAGNOSTYCZNA_ZA_MALA` i `GET /admin/pula-diagnostyczna` (12.3), a pominięcie testu przekazano do D-14.

### C2-14: Brak ścieżki usunięcia konta Nauczyciela i Administratora, a DDL ją blokuje
- **Waga:** NISKA
- **Lokalizacja:** 12.3 (tylko `DELETE /uczniowie/ja`); 7.3 (`klasa.nauczyciel_id`, `zestaw_zadan.nauczyciel_id`, `przypisanie.nauczyciel_id` bez `ON DELETE`); 11.4.3 p. 1 („Nauczyciel docelowy zablokowany/usunięty”); 16.3
- **Problem:** dokument wspomina „usuniętego” Nauczyciela, ale nie ma ani przepływu, ani endpointu usunięcia, a klucze obce `RESTRICT` uniemożliwiają `DELETE FROM uzytkownik` dla Nauczyciela z Klasą. Imię, nazwisko i e-mail Nauczyciela to dane osobowe (RODO), mimo że NFR-09 mówi wprost tylko o Uczniach.
- **Oczekiwana poprawka:** przepływ dezaktywacji i usunięcia konta Nauczyciela (przekazanie albo archiwizacja Klas, pseudonimizacja `nauczyciel_id` w Ocenach i audycie) albo jawne wyłączenie z zakresu.
- **Status:** ZAMKNIĘTA (iteracja 3). Opisano przepływ usunięcia konta Nauczyciela i Administratora (ADR-044, 16.4) z endpointami `DELETE /nauczyciele/ja` i `POST /admin/uzytkownicy/{id}/usuniecie` (ochrona ostatniego Administratora), Przekazaniem Klasy, archiwizacją, pseudonimizacją uzasadnień `R-PRZYPISANIE`, `ON DELETE SET NULL` i CHECK `nauczyciel_id IS NOT NULL OR zarchiwizowana` (7.3). Brak definicji przywrócenia takiego konta opisuje C3-08.

### C2-15: Normalizacja zdjęć w procesie `api` jest sprzeczna z deklarowaną piaskownicą Pillow
- **Waga:** NISKA
- **Lokalizacja:** 5.1 (piaskownica tylko na replikach workera); 5.2 („normalizacja w `api`”); 9.4; 10.3.1 p. 1; 16.2 („Piaskownica SymPy i Pillow: osobne procesy, RLIMIT”)
- **Problem:** dekodowanie HEIC/PNG/WEBP i zmiana rozmiaru do 5 × 10 MB odbywa się w procesie `api`, w którym nie ma puli procesów z limitami. 16.2 deklaruje izolację Pillow, a 5.1 jej w `api` nie przewiduje. Grozi to bombą dekompresyjną i blokowaniem pętli zdarzeń w szczycie „koniec lekcji” (90 przesłań w 60 s na 2 repliki).
- **Oczekiwana poprawka:** pula procesów z RLIMIT w `api` (albo normalizacja w workerze przed `ROZPOZNAJ_PISMO`, z uwzględnieniem w budżecie 17.2), `Image.MAX_IMAGE_PIXELS` i limit wymiarów przed dekodowaniem.
- **Status:** ZAMKNIĘTA (iteracja 3). `ProcessPoolExecutor` w `api` (2 procesy na replikę, `RLIMIT_AS`/`RLIMIT_CPU`, 8 s czasu ściennego), odczyt nagłówka i limit pikseli przed dekodowaniem, `422 ZDJECIE_NIEPRAWIDLOWE`, `503` przy zajętej piaskownicy: ADR-045, 5.1, 10.3.1 p. 1, 16.2, E-35. Pojemność (≈ 9% w szczycie) jest policzona w 17.4.

### C2-16: Drobne niespójności wewnętrzne
- **Waga:** NISKA
- **Lokalizacja:** 9.1 / 7.3 `odpowiedz.wynik_ujawniony_od`; 9.6.1 SQL podjęcia; 16.4 (tabela `usun_po`) / 13.3 `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`; 11.6; 13.3 `GENERUJ_SZKIC_WKLADKI`; 8.5 vs 13.3 `WYGAS_ELEMENTY`
- **Problem:**
  1. `wynik_ujawniony_od` (NOT NULL) dla Odpowiedzi testu ma być „koniec testu”, który nie jest znany przy `INSERT`, a `odpowiedz` nie ma przepływu aktualizacji.
  2. Ponowne podjęcie przez właściciela ustawia `zablokowana_do = now() + 15 min` bez warunku `podjeta_at + 2 h`, co obchodzi limit 2 h zapowiedziany w 8.3.
  3. `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` jest zlecane tylko przy przejściu do `OCENIONE` bez otwartej Weryfikacji. Po zamknięciu Kontroli losowej lub zgłoszenia kopia nie powstaje.
  4. Okno zgłoszenia liczone od `ocena_wystawiona_at` (pierwszej Oceny) skraca albo usuwa możliwość zgłoszenia dla Oceny Nauczyciela wystawionej później.
  5. Wkładka odrzucona przez recenzenta (status `WYCOFANA`) ponownie spełnia warunek `GENERUJ_SZKIC_WKLADKI` przy każdej rekomendacji, a szkice Wkładek nie podlegają budżetowi 12.7.
  6. 8.5 kończy Wkładkę `UKONCZONY` tylko po ≥ 60 s otwarcia, a `WYGAS_ELEMENTY` ustawia `UKONCZONY` bez tego warunku.
- **Oczekiwana poprawka:** ujednolicić wskazane miejsca (np. `wynik_ujawniony_od` ustawiane w `…/zakonczenie` albo nullable dla testu; warunek 2 h w podjęciu; zlecanie kopii przy zamknięciu ostatniej Weryfikacji; okno od Oceny aktualnej; licznik odrzuceń szkicu i budżet).
- **Status:** ZAMKNIĘTA (iteracja 3). Wszystkie sześć punktów usunięto: (1) `rezultat_ujawniony_od` jest nullable dla testu i ustawiany w `…/zakonczenie` lub `…/pominiecie` (7.3, 9.1); (2) warunek `podjeta_at + 2 h` obowiązuje przy ponownym podjęciu (9.6.1); (3) kopia jest zlecana przy każdym przejściu do „`OCENIONE` bez otwartej Weryfikacji” i nadpisywana przy nowszej Ocenie (13.3, 16.4, ADR-035); (4) okno zgłoszenia liczy się od Oceny aktualnej (11.6); (5) obowiązuje limit 2 odrzuceń w 30 dniach i budżet 12.7 dla szkiców (10.2.1, 13.3); (6) Wkładka bez dowodu przeczytania przechodzi do `POMINIETY` w 8.5 i 13.3.

### Podsumowanie iteracji 2

Weryfikacja uwag iteracji 1:

| Stan | Liczba | Identyfikatory |
|---|---|---|
| ZAMKNIĘTA | 28 | C1-01 … C1-20, C1-22 … C1-29 |
| OTWARTA | 1 | C1-21 (ŚREDNIA, obejście przez element `R-PRZYPISANIE`) |

Nowe uwagi iteracji 2:

| Waga | Liczba uwag | Identyfikatory |
|---|---|---|
| KRYTYCZNA | 0 | — |
| WYSOKA | 3 | C2-01, C2-02, C2-03 |
| ŚREDNIA | 10 | C2-04 … C2-13 |
| NISKA | 3 | C2-14, C2-15, C2-16 |
| **Razem** | **16** | |

Werdykt: wersja 2.1 zamyka oba problemy krytyczne iteracji 1 i niemal wszystkie wysokie. FR-28, kolejka globalna, PONOW_OCENE, blokady, model zapasowy, pomiar NFR-01/02/03 i RODO mają teraz spójne, mierzalne rozwiązania. Decyzje produktowe są uczciwie zebrane w G-0. Dokument nie jest jednak jeszcze gotowy do produkcji, bo otwarte pozostają trzy uwagi wysokie:
- nieograniczona edycja Transkrypcji podważa ADR-024 i integralność oceny automatycznej (C2-01);
- wersjonowanie Weryfikacji sprawia, że typowe rozstrzygnięcie trwające ponad 5 min kończy się `412` (C2-02);
- FR-14 zostało zawężone bez decyzji właściciela produktu (C2-03).

Do tego dochodzą luki średnie w spójności Profilu ucznia (C2-04), w Kalibracji (C2-05, C2-06), w budżecie NFR-01 (C2-07) i w RODO (C2-08 … C2-10).

---

## Iteracja 3

Data przeglądu: 2026-11-01. Wersja dokumentu: 2.2. Uwagi C1-21 i C2-01 … C2-16 zweryfikowano w treści dokumentu, a nie tylko w tabeli 23.5. Statusy są wpisane przy uwagach iteracji 1 i 2. Odstępstwa od sugerowanych poprawek (C2-01, C2-07, C2-08) oceniono pod kątem skuteczności i wszystkie trzy uznano za rozwiązujące problem. Decyzje D-1 … D-14 z pełną opcją domyślną nie są traktowane jako błędy. Uwagi poniżej dotyczą głównie fragmentów nowych w wersji 2.2.

### C3-01: Bezpiecznik trafności i bezpiecznik poprawek uruchamiają się zawsze, niezależnie od rzeczywistego błędu
- **Waga:** WYSOKA
- **Lokalizacja:** 11.5.3 („Bezpiecznik trafności: … jeśli `n ≥ 50` i UB błędu > 0.02 → `prog := min(0.990, prog + 0.02)`”); 11.5.2 („bezpiecznik poprawek — przy `n ≥ 50` i UB > 2%” → `LEMAT_POPRAWKI_ZAWSZE_DO_WERYFIKACJI`); 11.5.1 p. 1 (`n_eff ≥ 189`); 17.7 (faza „Start”: auto 47%); E-21
- **Problem:**
  1. Górna granica Wilsona 95% dla zera błędów wynosi z²/(n + z²). Dla n = 50 to 7,1%, dla n = 100 to 3,7%, dla n = 150 to 2,5%. Poniżej 2% spada dopiero przy n ≥ 189, i to wyłącznie przy zerze błędów. Przy rzeczywistym błędzie 0,5% potrzeba ≈ 335 próbek (tabela 11.5.1). Warunek „n ≥ 50 i UB > 2%” jest więc spełniony **z konstrukcji** w całym przedziale 50 ≤ n < 189, nawet gdy model nie popełnia żadnych błędów. Dokument sam przyjmuje 189 jako minimalną liczność, przy której kryterium w ogóle da się spełnić (11.5.1 p. 1). Bezpiecznik jest z tym sprzeczny.
  2. Skutek na starcie produkcji: przy Kontroli 20% (≈ 110–120 Kontroli dziennie dla Jev i zadań obliczeniowych, 17.7) n przekracza 50 w ciągu 1–2 dni. Bezpiecznik podnosi wtedy próg o 0,02 dziennie aż do 0,990, mimo że próg z ZT-1 spełniał UB ≤ 2% przy `n_eff ≥ 335`. Obniżanie próg może cofać tylko o 0,02 na dobę, i to dopiero gdy 11.5.3 znajdzie `t`. Przez pierwsze ≈ 2 tygodnie odsetek ocen automatycznych spada więc praktycznie do zera, a model obciążenia 17.7 („Start: auto 47%”) i bramka G-7 tracą ważność dokładnie w okresie, w którym pula weryfikatorów jest najciaśniejsza. Jeśli „dla aktywnego progu” oznacza Kontrole wykonane przy bieżącym rekordzie `prog_pewnosci`, każde podniesienie progu zeruje n i mechanizm działa jak zapadka.
  3. To samo dotyczy modelu zapasowego. Używany jest tylko w czasie awarii Jev, więc jego n z 30 dni zwykle mieści się w przedziale 50–189. Po każdej dłuższej awarii jego próg rośnie do 0,990, a NFR-07 w praktyce oznacza pełną weryfikację.
  4. Bezpiecznik poprawek ma ten sam błąd. Flaga ustawia się automatycznie przy n ≥ 50 i wraca po każdym ręcznym zdjęciu, dopóki n < 189. Każda Poprawka trafia wtedy do T-7, co podważa uzasadnienie odrzuconej alternatywy w ADR-036 („każda Poprawka → Weryfikacja — odrzucona”).
- **Oczekiwana poprawka:** bezpiecznik ma reagować na **dowód** przekroczenia, a nie na brak dowodu spełnienia: np. dolna granica Wilsona (LB) błędu > 2%, sekwencyjny test SPRT (H0: e ≤ 1%, H1: e ≥ 3%) albo próg minimalny `n_eff ≥ 189`, zanim UB może cokolwiek uruchomić. Okno należy liczyć dla pary (konfiguracja, rodzaj), a nie dla rekordu progu. Ta sama poprawka dotyczy bezpiecznika poprawek. Trzeba dodać test jednostkowy „0 błędów przy n = 50…188 nie uruchamia bezpiecznika” i scenariusz startowy w 17.7.
- **Status:** ZAMKNIĘTA (iteracja 4). Bezpiecznik wyzwala się wyłącznie przy dolnej granicy Wilsona 95% błędu > 2% przy `n_eff ≥ 50` (11.5.3, ADR-046, E-21, parametr 22). Próba to Kontrole losowe z 30 dni dla pary (konfiguracja, rodzaj) powyżej bieżącego progu, więc mechanizm nie działa jak zapadka. UB > 2% przy `n_eff ≥ 189` tylko blokuje obniżanie. Flagę środowiskową zastąpiła tabela `stan_bezpiecznika` z CHECK i `okno_od` (7.3), są też endpointy 12.3 i testy (a)–(f). **Rachunki przeliczone niezależnie:** UB przy 0 błędach wynosi 7,13 / 3,70 / 2,50 / 2,00 / 1,99% dla n = 50 / 100 / 150 / 188 / 189. `k_min` = 3, 5, 7, 8, 8, 12, 17, 29 dla n = 50, 100, 150, 188, 189, 335, 500, 1 000. LB przy `k_min − 1` / `k_min` i P(wyzwolenie) dla e = 1 … 8% zgadzają się z tabelą co do 0,01 pp, a minima `n_eff` (189 / 335 / 482 / 753) z tabelą 11.5.1. Symulację 90 dni odtworzono (2 000 przebiegów, okno kroczące 30 dni, stały napływ dzienny, ocena od dnia 1, licznik do pierwszego wyzwolenia). Dla e = 0 / 0,5 / 1 / 2 / 4 / 6% wyniki to: start 0 / 0,1 / 0,5 / 32 / 100 / 100%, faza ustalona 0 / 0 / 0,4 / 35 / 100 / 100%, model zapasowy 0 / 0 / 1,4 / 34 / 99,5 / 100%. Różnice względem tabeli dokumentu nie przekraczają 0,8 pp (błąd Monte Carlo), a mediany dnia wyzwolenia różnią się o 1–5 dni. Scenariusz startowy 17.7 też się zgadza: UB/LB dla (110, 0), (220, 1), (330, 2), (440, 2) i (770, 4), przy czym LB 0,12% zamiast 0,13% to kwestia zaokrąglenia. Zgadzają się też liczby wariantu 2.2: `M` = 5 154 min, 137 min i 26 weryfikatorów. Opis `tests/kalibracja/symulacja_bezpiecznika.py` (napływy, okno, liczba przebiegów, reguła, metryki wyniku) wystarcza do zbudowania skryptu. Nie rozstrzyga tylko, czy napływ dzienny jest stały, czy z rozkładu Poissona, i czy przebieg kończy się na pierwszym wyzwoleniu (wpływ < 1 pp). Pozostałości opisują C4-02 (interakcja z wyborem progu) i C4-06 p. 5.

### C3-02: Filtr danych osobowych zmienia treść matematyczną Rozwiązania, a Uczeń nie może tego poprawić
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.3.1 p. 3a (a)–(c); 9.4 (Filtr także na `PUT …/transkrypcja`); 13.3 `ROZPOZNAJ_PISMO`; ADR-042; Z-1, Z-3; FR-18, FR-28; G-2/G-6
- **Problem:**
  1. **Usuwanie kroków na podstawie ramek LLM.** Reguła (a) usuwa krok, którego `obszar` pokrywa się w ≥ 50% z ramką danych osobowych. Zarówno `obszar`, jak i ramki pochodzą z Modelu generatywnego, a reguła działa przed zatwierdzeniem i bez śladu w UI. Gdy model błędnie oznaczy linię rozwiązania jako „dane osobowe” albo Uczeń podpisze się w linii wyniku, krok rozwiązania znika z Transkrypcji i Ocena jest liczona bez niego (np. `BRAK_WYMAGANEGO_KROKU`, zerowe punkty za wynik). To sygnał LLM działający w kierunku **niebezpiecznym**, wbrew Z-1, a błąd systemu obniża Ocenę, wbrew FR-28. Uczeń może co najwyżej wstawić krok ponownie jako Poprawkę (limit 2, wlicza się do limitu miękkiego, czyli może wywołać T-7).
  2. **Fałszywe trafienia nazw.** Reguła (b) zastępuje w `tekst` **i `latex`** każdy token zgodny z imieniem lub nazwiskiem (Levenshtein ≤ 1 na rdzeniu ≥ 4 znaków). Polskie nazwiska i imiona pokrywają się ze słowami matematycznymi albo są od nich odległe o jedną literę (np. Pole, Kula, Granica, Suma, Prosta, Koło). „Pole trójkąta” Ucznia o nazwisku Pole staje się „[DANE_OSOBOWE] trójkąta”, a podmiana wewnątrz LaTeX psuje parsowanie SymPy (N-3/N-4, niespójności).
  3. **Brak wyjścia.** Filtr działa także na Poprawkach Ucznia (9.4), więc Uczeń nie może przywrócić usuniętego słowa. Jedyne wyjście, czyli nowe zdjęcia, daje ten sam wynik. Dla takiego Ucznia FR-18 przestaje działać w tej części.
  4. **Luka w drugą stronę.** Imiona i nazwiska krótsze niż 4 znaki (Ewa, Jan, Ola, Iza, Ida, Lis) nie są porównywane, więc kryterium G-2/G-6 „0 wystąpień danych konta” może nie zostać spełnione. Na 50 kartkach to i tak słaby dowód (0/50 daje UB 7%).
- **Oczekiwana poprawka:** (a) zamiast usuwać krok, maskować w nim tylko fragment pokrywający się z ramką, a krok oznaczyć do sprawdzenia przez Ucznia i ewentualnie przez T-3. Usunięcie kroku nie może zmienić Oceny bez Weryfikacji (Z-1). (b) Ograniczyć dopasowanie do `tekst` (nie `latex`) i wykluczyć tokeny ze słownika terminów matematycznych. Dla trafienia niepewnego pokazać Uczniowi znacznik z możliwością przywrócenia słowa: Poprawka przywracająca token nie podlega ponownemu filtrowaniu, ale dostaje flagę do Kontroli. (c) Obsłużyć imiona i nazwiska krótsze niż 4 znaki (dokładne dopasowanie), a w G-2/G-6 dodać metrykę fałszywych zastąpień na ZT-1 i kryterium dla tokenów matematycznych.
- **Status:** ZAMKNIĘTA (iteracja 4). Filtr nie usuwa kroków i nie zmienia `latex` (10.3.1 p. 3a, 3.3, uzupełnienie ADR-042, G-2 „0 zmian w `latex`”). Krok bez treści matematycznej jest maskowany w całości (`tylko_dane_osobowe` z CHECK w 7.3). Krok z treścią matematyczną zostaje i ustawia `konflikt_filtra` → T-3 (11.1 p. 7d, E-37), więc sygnał LLM działa tylko w kierunku bezpiecznym (Z-1, FR-28). Dopasowanie obejmuje tylko `tekst` i wyklucza Słownik terminów matematycznych. Nazwy krótsze niż 4 znaki są dopasowywane dokładnie. Pozycje zastąpień trafiają do `zastapienia_danych_osobowych`. Słowo można przywrócić Poprawką, a dopasowania `PRZYBLIZONE` nie są ponownie filtrowane. G-2 obejmuje 150 kartek (UB 2,5% sprawdzone), w tym przypadki krótkich imion i nazwisk-terminów, oraz kryteria fałszywych zastąpień. Pozostałe luki (nazwisko równe terminowi matematycznemu, działanie reguły (a) na Poprawkach) opisuje C4-03.

### C3-03: `UZGODNIJ_REJESTR_RODO` unieważnia każde zdarzenie `USUNIECIE_WYKONANE`, przez co rejestr i procedura PITR tracą wykonane usunięcia
- **Waga:** ŚREDNIA
- **Lokalizacja:** 16.4 („Po 30 dniach `USUN_KONTO` wykonuje…”, kroki 0–6; usunięcie konta Nauczyciela p. (1)); 13.3 `UZGODNIJ_REJESTR_RODO` („obiekty … starsze niż 5 min bez wiersza `zdarzenie_rodo` o tym `id` → obiekt kompensujący”); ADR-041 („`api` zapisuje każde Zdarzenie RODO … a kopię do `zdarzenie_rodo`”); 5.1 (krawędź `WRK → REJ`); 7.3 `zdarzenie_rodo.id` („generowany w api”); G-6 (a)
- **Problem:**
  1. `USUNIECIE_WYKONANE` zapisuje do bucketu **worker** (krok 1 `USUN_KONTO`), a nie `api`. Żaden z kroków 0–6 nie wstawia wiersza `zdarzenie_rodo`. Wiersze powstają tylko w transakcjach `api` (16.4 p. 1, przywrócenie, zgoda) i w `ponow_zdarzenia_rodo`.
  2. Po 5 minutach `UZGODNIJ_REJESTR_RODO` uznaje więc zdarzenie za „transakcję nieudaną” i zapisuje `UNIEWAZNIENIE`. Nawet jeśli wiersz byłby wstawiany w kroku 5, kroki 2–4 (aktualizacja wszystkich partycji `decyzja_modelu`, usuwanie obiektów S3) mogą trwać dłużej niż 5 minut, z tym samym skutkiem. Wiersz zapisany przed krokiem 5 i tak przepadłby przy PITR.
  3. Skutki: (a) każde wykonane usunięcie konta kończy się obiektem `UNIEWAZNIENIE` i alertem `ALERT_RODO` (17.3: przyrost `zdarzenia_rodo_uniewaznione_total` > 0), więc alert przestaje cokolwiek znaczyć, a rejestr rozliczalności (art. 5 ust. 2 RODO) mówi, że usunięcia nie wykonano; (b) przy PITR do punktu sprzed wykonania usunięcia `ponow_zdarzenia_rodo` pomija unieważnione `USUNIECIE_WYKONANE`, więc przywrócone dane konta (bez zdjęć) zostają w bazie do najbliższego nocnego `USUN_KONTO`. Odtworzone zostaje tylko `ZADANIE_USUNIECIA` z minionym terminem. Ponowne usunięcie zależy więc od harmonogramu, a nie od procedury odtworzenia. To regresja wobec wersji 2.1, w której rejestr wykonanych usunięć nie miał mechanizmu unieważniania. Test G-6 (a) da poprawny wynik tylko wtedy, gdy odtworzenie nastąpi w ciągu 5 minut od usunięcia.
- **Oczekiwana poprawka:** zdarzenia generowane przez worker (`USUNIECIE_WYKONANE`, także dla Nauczyciela) zapisać w jednej transakcji z krokiem 0 (wiersz `zdarzenie_rodo` + anulowanie procesów) albo oznaczyć w obiekcie `zrodlo = WORKER` i wyłączyć z `UZGODNIJ_REJESTR_RODO` (dla takich zdarzeń stanem docelowym jest wykonanie usunięcia, a nie transakcja). Poprawić ADR-041 („`api` zapisuje każde…”) i komentarz `zdarzenie_rodo.id`. Rozszerzyć G-6 (a) o odtworzenie wykonane później niż 10 minut po usunięciu.
- **Status:** ZAMKNIĘTA (iteracja 4). `USUNIECIE_WYKONANE` zapisuje `worker` z `zrodlo = WORKER`, deterministycznym `id` (uuid5) i `If-None-Match: *`, przed pierwszą zmianą w bazie (16.4 krok 0). Wiersz `zdarzenie_rodo` powstaje w pierwszej transakcji (krok 1), a typ `zrodlo_zdarzenia_rodo` jest w 7.3. `UZGODNIJ_REJESTR_RODO` unieważnia tylko zdarzenia `API`, a dla `WORKER` ponawia `USUN_KONTO` po 2 h i alarmuje po 26 h (13.3). `ponow_zdarzenia_rodo` wykonuje kroki 1–6 synchronicznie niezależnie od czasu od usunięcia. `ALERT_RODO` dotyczy tylko `API` (17.3). Jest uzupełnienie ADR-041, a G-6 (a) ma warianty 3 min i 60 min. Nową lukę w regule pomijania zdarzeń po `id` (odtworzenie do punktu w trakcie `USUN_KONTO`) i brak rozróżnienia ról opisuje C4-01.

### C3-04: Globalny ogranicznik ma dwie sprzeczne formuły przydziału, a kubełek jest mniejszy od fali, którą trzeba w nim zarezerwować
- **Waga:** ŚREDNIA
- **Lokalizacja:** 7.3 `limit_szybkosci_modelu` (komentarz: „0.8 × limit_klucza / liczba wierszy”, brak kolumny wagi); 13.2 („`r = 0,8 × limit_klucza / N` … 10 żądań/s na proces”, „lokalny token bucket ma pojemność `r`”); ADR-039 p. 3 i 22 („`waga / Σ wag`, worker 1,0, `api` 0,1”); 17.4 („worker 18.2/s × 2, `api` 1.8/s × 2”); 17.2 S-6 („przydział workerów 36.4/s”, „≤ 1 s oczekiwania”); 13.1 `OgranicznikSzybkosci.zarezerwuj(…, liczba_zadan, …)`; 10.4.3 (fale po ≤ 16, runda 1 = 27 pytań)
- **Problem:**
  1. Dokument podaje dwie różne formuły. Przy podziale po równo (7.3, 13.2) workery dostają łącznie 20 żądań/s, przy wagach (ADR-039, 17.4, 22) — 36,4/s. Różnica ≈ 45% przepustowości Jev w szczycie „koniec lekcji” (do 44/s w trybie pojedynczym). Od wyboru zależy odsetek Ocen kierowanych do modelu zapasowego i ważność S-6. Tabela nie ma kolumny wagi, więc wariant z wagami nie wynika z DDL.
  2. Lokalny token bucket ma pojemność `r`, a `zarezerwuj` przyjmuje liczbę żądań. W trybie pojedynczym jedna fala to do 16 żądań, a runda 1 to 27. Przy `r = 10` (13.2) rezerwacja fali albo rundy **nigdy** nie może się udać. Oceny w trybie pojedynczym czekałyby do `termin_miekki − 11 s` i trafiały do modelu zapasowego, także poza szczytem. Nie zdefiniowano, czy rezerwuje się żądanie, falę czy rundę.
  3. Skutek: NFR-07 i G-4 (d) mierzą zachowanie inne niż opisane, a budżet 17.2 dla trybu pojedynczego (przyjęty jako najgorszy przypadek) zależy od nieustalonego parametru.
- **Oczekiwana poprawka:** jedna formuła we wszystkich miejscach. Jeśli są wagi, to kolumna `waga` w `limit_szybkosci_modelu` albo jawne mapowanie `wykonawca → waga`. Ustalić granulację rezerwacji (np. pojedyncze żądanie z oczekiwaniem albo fala z pojemnością kubełka ≥ maksymalnej fali) i warunek `pojemnosc ≥ 16`. Przeliczyć S-6 dla przyjętej formuły i dodać test jednostkowy rezerwacji fali.
- **Status:** ZAMKNIĘTA (iteracja 4). Jedna formuła obowiązuje w 7.3 (komentarz oraz kolumny `waga` i `pojemnosc` z CHECK), 13.2, 17.4, 22 i ADR-039. Jeden token odpowiada jednemu wywołaniu, a `B = 16` mieści maksymalną falę. Są przyjęcie planu, terminy fal, pierwszeństwo fal ≥ 2, porty w 13.1 (`PrzyjeciePlanu`, `przyjmij_plan`, `zarezerwuj_wywolanie`, `zwolnij`) i testy (a)–(e). **Rachunki przeliczone niezależnie:** Σ wag = 2,2, r_worker = 18,18/s, r_api = 1,82/s, łącznie 40/s. 16 / 18,18 = 0,88 s. Granica chwilowa 34 + 40 T daje 74 przy T = 1 s i 234 przy T = 5 s. Wariant B = 4: 2 × (4 + 18,18) + 2 × (1 + 1,82) = 50,0, oczekiwanie 12 / 18,18 = 0,66 s. S-6: popyt 43,5/s wobec 36,36/s, zaległość po 20 s ≈ 80–110 wywołań, czyli ≈ 2,2–3 s kolejki, co zgadza się z maksimum 3,7 s z symulacji; t_O = 9,7 s, czas systemowy 26,2 s. S-6b: 28,2 s wynika z dolnego ograniczenia terminu miękkiego do 10 s. Opis `tests/obciazenie/symulacja_ogranicznika.py` (procesy, plan 16 + 11 + 1, czas fali, SymPy przed falą 1, profile A–D, `t_R`, reguły 13.2, warianty kubełka i podziału po równo) wystarcza do implementacji. Nie rozstrzyga tylko, czy czas fali jest stały (p95), czy losowany; wariant stały jest pesymistyczny, więc to dopuszczalne. Pozostałości opisują C4-05 (zależność granicy chwilowej od liczby procesów) i C4-06 p. 8–9.

### C3-05: Poprawki Transkrypcji: kroki wstawione omijają miarę znaków, a Sprawdzenie zgodności nie mieści się w założonym oknie czasowym
- **Waga:** ŚREDNIA
- **Lokalizacja:** 9.4 (tabela limitów: „odsetek zmienionych znaków = Σ Levenshtein(odczyt, poprawka) / Σ długość odczytu”; „Sprawdzenie zwykle kończy się przed zatwierdzeniem”); 12.5 (`tekst`/`latex` ≤ 2000 znaków); 10.3.3 (G-2: tylko Poprawki „zastępujące”); 17.2 („gotowe przed zatwierdzeniem w > 95% przypadków”); 22 (timeout 8 s, oczekiwanie ≤ min(6 s, `termin_miekki − 9 s`)); 17.4 („Dostawca rozpoznawania: 90 żądań/min … limit ≥ 120/min”); 17.7 (T-7 = 3%)
- **Problem:**
  1. Krok wstawiony nie ma odczytu agenta, więc wzór na odsetek zmienionych znaków go nie obejmuje: licznik i mianownik dotyczą par (odczyt, poprawka). Uczeń może więc sfotografować kartkę z kilkoma liniami i wstawić 2 kroki po 2000 + 2000 znaków z kompletnym rozwiązaniem. Limit kroków (≤ 2) i limit miękki (≤ max(2, ⌈0,25 n⌉)) tego nie zatrzymają, więc jedyną barierą jest jedna decyzja LLM `widoczny_na_zdjeciu` dla wstawionego kroku bez `obszar`. G-2 mierzy wykrywalność tylko dla Poprawek „zastępujących”, a dla tego wektora nie mierzy niczego.
  2. Sprawdzenie zgodności to wywołanie multimodalne z kompletem zdjęć tej samej roli co rozpoznanie (p95 9 s dla 3 zdjęć, A-08), z timeoutem 8 s. Nic nie wymusza, żeby PWA wysyłała `PUT …/transkrypcja` wcześniej niż tuż przed `…/zatwierdzenie` (9.4 nie opisuje autozapisu). Okno w Ocenie wynosi najwyżej 6 s, więc założenie „> 95% gotowe przed zatwierdzeniem” jest nieuzasadnione. Brak rezultatu oznacza T-7, czyli Weryfikację. Przy oszacowaniu D-10 (15–25% Rozwiązań z Poprawkami) może to dać kilka razy więcej T-7 niż 3% z 17.7.
  3. Wywołania Sprawdzenia zużywają limit dostawcy rozpoznawania (A-08 ≥ 120/min) i pulę R, która w szczycie obsługuje rozpoznanie (17.4 liczy tylko 90 rozpoznań/min). W szczycie Sprawdzenie ma niższy priorytet (2) i czeka w puli R, co dodatkowo zwiększa liczbę T-7.
- **Oczekiwana poprawka:** wliczyć kroki wstawione do miary znaków (mianownik: znaki odczytu agenta, licznik: znaki zmienione plus wstawione) i ograniczyć długość kroku wstawionego (np. ≤ 200 znaków albo ≤ mediany długości kroków agenta). Rozszerzyć G-2 o syntetyczne „wstawienia rozwiązania”. Opisać w PWA zapis Poprawki natychmiast po edycji (debounce) i zmierzyć `czas_od_put_do_zatwierdzenia`. Uwzględnić Sprawdzenie w 17.4 (limit dostawcy, pula R) i przeliczyć odsetek T-7 w 17.7 z rozkładu czasu, a nie z założenia.
- **Status:** ZAMKNIĘTA (iteracja 4). Kroki wstawione są w liczniku odsetka znaków, a ich długość ma limit (9.4; przykład sprawdzony: limit 60 znaków, 120 / 120 = 1,00 > 0,50 → `422`, 25 / 120 = 0,21). Dodano obszar kroku wstawionego (10.3.1, 10.3.3), autozapis PWA, deduplikację z podmianą ładunku (13.4, C-38), osobną pulę S (13.3, 13.4, 22) i A-08 ≥ 150/min. Rachunek 17.4 się zgadza: 117/min ≤ 120/min, przy wagach R 92,3/min ≥ 90 i S 27,7/min ≥ 27. Rachunek log-normalny sprawdzono (σ = 0,286, P(> 6 s) = 0,11%). T-7 = 0,20 × 0,125 = 2,5% (3,1% przy p_P = 0,25). G-2 obejmuje „wstawienia rozwiązania” i fałszywe alarmy ≤ 5%, są też metryki `czas_od_put_do_zatwierdzenia_sekundy` i `sprawdzenie_poprawek_gotowe_przed_zatwierdzeniem`. Drobne pozostałości opisuje C4-06 p. 1 i p. 10.

### C3-06: Odtworzenie bazy z PITR zostawia osierocone kopie treningowe, których nie da się usunąć po odwołaniu zgody
- **Waga:** ŚREDNIA
- **Lokalizacja:** 16.4 („Kopie zapasowe PostgreSQL”, procedura p. 1–3; „Magazyn obiektów”: bucket `zbior-treningowy` z regułą 24 miesięcy); 7.3 `kopia_treningowa` („jedyne powiązanie kopii z kontem”); ADR-035 (odrzucony „pseudonim bez powiązania”); E-02 (sprzątanie osieroconych kluczy tylko dla prefiksu `zdjecia/`)
- **Problem:** kopia treningowa utworzona po punkcie odtworzenia `T` istnieje w buckecie `zbior-treningowy/{pseudonim}/…`, ale jej wiersz `kopia_treningowa` przepada przy PITR. `ponow_zdarzenia_rodo` odtwarza tylko Zdarzenia RODO, a utworzenie kopii nim nie jest. Po odtworzeniu kopia nie ma żadnego powiązania z kontem, więc ani odwołanie zgody, ani `USUN_KONTO` (krok 4a) jej nie usuną. Przetrwa do reguły 24 miesięcy. To dokładnie przypadek, który ADR-035 odrzuca, bo zgoda musi być odwołalna (art. 7 ust. 3 RODO). Zdjęcie maskowane i Transkrypcja pisma Ucznia nadal są danymi osobowymi. Podobnie obiekty `zdjecia-maskowane/` po `T` nie są objęte sprzątaniem z E-02 (prefiks `zdjecia/`), chociaż tu zabezpiecza reguła 91 dni.
- **Oczekiwana poprawka:** sprzątanie osieroconych obiektów w `zbior-treningowy/` i `zdjecia-maskowane/` (porównanie z `kopia_treningowa.klucz_obiektu` i `zdjecie_rozwiazania.klucz_obiektu_maskowany`) jako krok procedury odtworzenia przed startem workerów oraz codzienną pracę w tle. Alternatywnie zapisywać utworzenie kopii jako Zdarzenie RODO (`KOPIA_TRENINGOWA_UTWORZONA`) w rejestrze. Dodać scenariusz (d) do G-6.
- **Status:** ZAMKNIĘTA (iteracja 4). Praca `SPRZATAJ_OSIEROCONE_OBIEKTY` (13.3) obejmuje trzy buckety i porównuje klucze z `kopia_treningowa` i `zdjecie_rozwiazania`. Tryb `CODZIENNY` pomija obiekty młodsze niż 1 h, co chroni zapisy przed zatwierdzeniem transakcji. Tryb `ODTWORZENIE` działa od `T − 5 min` jako krok 2a przed startem workerów (16.4). Są też harmonogram (13.5), E-02 i E-38, metryka i alert, G-6 (d), koszt przebiegu oraz ADR-047 z uzasadnionym odrzuceniem alternatyw.

### C3-07: Nowe naruszenia słownika: transliteracja „żądanie” koliduje z zarezerwowanym „Zadanie”, a „wariant” wciąż oznacza grupę eksperymentu
- **Waga:** ŚREDNIA
- **Lokalizacja:** 7.3 `typ_zdarzenia_rodo` (`ZADANIE_USUNIECIA`), 12.3, 16.4, ADR-041; 13.1 `OgranicznikSzybkosci.zarezerwuj(…, liczba_zadan: int, …)`; 17.3 `jev_zadania_http_total{tryb}`; 3.1 p. 1, p. 3 i p. 5; 3.4 (test CI, część 3: lista słów zarezerwowanych bez `zadani*`); 3.3 „Grupa eksperymentu” („Przydział Ucznia do **wariantu** eksperymentu A/B”); 10.4.3 Q-RE-01 („rekomendacja w **wariancie** JEV_CHOICE”); 3.4 B-5; 10.3.1 `PoprawkaDoSprawdzenia.obszar: ObszarDanychOsobowych`
- **Problem:**
  1. Reguła 3.1 p. 1 zapisuje identyfikatory w ASCII bez polskich znaków. Przez to „żądanie usunięcia” staje się `ZADANIE_USUNIECIA`, „liczba żądań” — `liczba_zadan`, a „żądania HTTP” — `jev_zadania_http_total`. Wszystkie trzy identyfikatory, nowe w wersji 2.2, zawierają słowo **zarezerwowane** dla Zadania (3.1 p. 3) w zupełnie innym znaczeniu. `ZADANIE_USUNIECIA` w typie wyliczeniowym czyta się jako „Zadanie usunięcia”. To dokładnie ten rodzaj homonimu, który reguła 3.1 p. 5 miała wykluczyć. Test CI (3.4, część 3) pilnuje `odpowiedz*`, `wariant*`, `krok*`, `wynik*` i `kolejk*`, ale nie `zadani*`, a tokenizacja (część 2) przepuszcza `zadanie` jako token słownikowy, więc naruszenie przejdzie CI.
  2. Mimo zmian z C2-11 słowo „wariant” nadal oznacza grupę eksperymentu w definicji pojęcia **Grupa eksperymentu** w 3.3 (czyli w treści, która ma trafić do słownika przez PZS-1 A) oraz w warunku Q-RE-01 w 10.4.3. To wprost łamie 3.1 p. 5 („grupa eksperymentu … nigdy «wariant»”).
  3. Proponowana definicja B-5 (Pewność oceny: „zgodna … co do liczby punktów i wskazania pierwszego błędnego kroku”) pomija brakujący krok, który jest częścią Zgodności Oceny w 3.3 i ADR-027 (kolumna generowana `zgodna`). Słownik i model danych dostałyby dwie różne definicje.
  4. Drobne: typ `ObszarDanychOsobowych` opisuje ramkę **linii kroku** w `PoprawkaDoSprawdzenia.obszar`. Nazwa „Rejestr usunięć” obejmuje teraz także udzielenie i odwołanie zgody, co nie jest usunięciem.
- **Oczekiwana poprawka:** zmienić nazwy, np. `WNIOSEK_O_USUNIECIE` (albo `ZLECENIE_USUNIECIA_KONTA`), `liczba_wywolan`, `jev_wywolania_http_total`. Dopisać do 3.1 p. 3 regułę transliteracji („żądanie” → „wniosek” albo „wywołanie”), a `zadani*` do listy słów zarezerwowanych testu CI (część 3) z listą dozwolonych użyć. W 3.3 i 10.4.3 zastąpić „wariant” słowami „grupa eksperymentu” / „strategia”. Ujednolicić B-5 z ADR-027. Wprowadzić typ `ObszarKroku` i rozważyć nazwę „Rejestr Zdarzeń RODO”.
- **Status:** ZAMKNIĘTA (iteracja 4). 3.1 p. 3 zawiera regułę transliteracji. Są `WNIOSEK_O_USUNIECIE` (7.3), `jev_wywolania_http_total` (17.3), usunięty parametr `liczba_zadan` (13.1), test CI (część 3) z `zadan*` i zamkniętą listą dozwolonych użyć, „Grupa eksperymentu” i Q-RE-01 bez słowa „wariant” (3.3, 10.4.3), B-5 z brakującym krokiem zgodne z ADR-027, typy `ObszarNaZdjeciu` / `ObszarKroku` i „Rejestr Zdarzeń RODO”. Przeszukanie całego dokumentu nie znalazło starych identyfikatorów poza changelogiem i adnotacjami historycznymi. Nowe naruszenie w identyfikatorach wersji 2.3 opisuje C4-04.

### C3-08: Przywrócenie konta Nauczyciela jest stratne, a rozstrzygnięcie w okresie karencji Ucznia przetwarza dane wbrew ADR-040
- **Waga:** NISKA
- **Lokalizacja:** 16.4 („Usunięcie konta Nauczyciela lub Administratora”: „Natychmiast: blokady … zwalniane (szkice usuwane), flagi … zdejmowane, Weryfikacje `KLASA` eskalowane”); 12.3 `POST /admin/uzytkownicy/{id}/przywrocenie` (tabela przywrócenia opisuje tylko Ucznia); 13.6 `KontoPrzywrocone`; ADR-040 („przywrócenie jest bezstratne”, „dane są przechowywane bez przetwarzania”); 9.6.2 (warunki rozstrzygnięcia bez sprawdzenia statusu konta Ucznia); 16.4 tabela przywrócenia (brak ponownego zlecenia `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`)
- **Problem:**
  1. Dla konta Nauczyciela skutki żądania usunięcia są nieodwracalne: szkice są usunięte, flagi `weryfikator_globalny` i `recenzent_tresci` zdjęte, a Weryfikacje przeniesione do `GLOBALNA`. Przywrócenie w 30 dniach nie przywraca żadnego z nich. Nie wiadomo nawet, czy endpoint przywrócenia obsługuje tę rolę.
  2. SQL podjęcia wymaga konta Ucznia `AKTYWNE`, ale rozstrzygnięcie Weryfikacji podjętej przed żądaniem usunięcia nie ma tego warunku (9.6.2). W karencji powstają więc Ocena Nauczyciela, Wynik zadania i Próbka kalibracyjna, co przeczy deklaracji „bez przetwarzania” z ADR-040. Po przywróceniu kopia treningowa tego Rozwiązania nie powstaje, bo tabela przywrócenia nie zleca ponownie `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`.
- **Oczekiwana poprawka:** tabela przywrócenia dla Nauczyciela i Administratora, albo odroczenie zdjęcia flag i eskalacji do końca karencji z zachowaniem eskalacji jako jedynego skutku natychmiastowego. Warunek konta `AKTYWNE` w rozstrzygnięciu (`409 KONTO_NIEAKTYWNE` z zachowaniem szkicu) albo jawny wyjątek w ADR-040. Ponowne zlecenie kopii przy przywróceniu.
- **Status:** ZAMKNIĘTA (iteracja 4). W karencji konta Nauczyciela natychmiast zwalniane są tylko blokady (szkice zachowane) i wykonywana jest eskalacja z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA` (typ i kolumna w 7.3, 11.4.3 p. 1, 13.6). Flagi zostają do `USUN_KONTO`. Jest tabela przywrócenia dla Nauczyciela i Administratora (16.4), a rozstrzygnięcie i korekta wymagają konta Ucznia `AKTYWNE` (`409 KONTO_UCZNIA_NIEAKTYWNE` w 9.6.2, 9.6.3, 12.2, 12.5 i C-37). Przywrócenie zleca `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`, a D-15 ma opcję domyślną (nie jest to błąd). Drobny wyścig w C-37 opisuje C4-06 p. 4.

### C3-09: Drobne niespójności wersji 2.2
- **Waga:** NISKA
- **Lokalizacja:** 21 R-02; 12.6; 12.2 `LIMIT_OBCIAZENIA_WERYFIKATORA`, 22 (dwa wiersze), A-13, A-14; 13.3 `WYCZYSC_DANE_TECHNICZNE` vs 16.4; 15.2 C-07; 15.2 C-36 / 7.2
- **Problem:**
  1. R-02 wymienia w mitygacji „SLA 72 h”, a od wersji 2.2 obowiązują 3 dni robocze.
  2. W przykładzie 12.6 Ocena powstała w sobotę 2026-10-31 o 18:01Z, a `termin_at` = 2026-11-03T18:01:52Z (72 h kalendarzowe). Według `dodaj_dni_robocze` (7.3) powinno być 2026-11-05T07:00:00Z: start w poniedziałek 08:00 czasu warszawskiego, bo 1.11 to niedziela i święto, plus 3 dni robocze.
  3. Kod `LIMIT_OBCIAZENIA_WERYFIKATORA` i parametry w 22 mówią o „180 min/dobę” i „90 min/dobę”, a A-13, A-14 i 17.7 o dniu roboczym. Różnica 7/5 zmienia wynik walidacji `422`.
  4. 16.4 przypisuje retencję `zdarzenie_rodo` (2 lata) pracy `WYCZYSC_DANE_TECHNICZNE`, ale jej warunek w 13.3 tej tabeli nie wymienia.
  5. C-07 opisuje SQL podjęcia sprzed wersji 2.2, bez warunku `podjeta_at + 2 h`.
  6. `UPDATE zadanie SET wstrzymane_w_doborze` (C-36) jest zmianą systemową. Według 7.2 nie wiadomo, czy podnosi `zadanie.version`. Jeśli tak, recenzent rozpatrujący zgłoszenie z `If-Match` Z dostaje `412` po każdym nowym zgłoszeniu, czyli wraca problem z C1-23.
- **Oczekiwana poprawka:** ujednolicić wskazane miejsca. Wstrzymanie w doborze przenieść poza `zadanie.version`, na wzór ADR-029.
- **Status:** ZAMKNIĘTA (iteracja 4). (1) R-02 nie wspomina już 72 h. (2) 12.6 podaje `termin_at` = 2026-11-05T07:00:00Z — sprawdzone: 31.10.2026 to sobota, 1.11 to niedziela i święto, więc termin liczy się od poniedziałku 2.11, 08:00 CET; po 3 dniach roboczych wypada w czwartek 5.11, 08:00 CET, czyli 07:00Z. (3) 12.2 i 22 mówią o dniu roboczym. (4) `WYCZYSC_DANE_TECHNICZNE` usuwa `zdarzenie_rodo` po 2 latach (13.3). (5) C-07 ma warunek `podjeta_at + 2 h` i konto `AKTYWNE`. (6) Tabela `wstrzymanie_w_doborze` jest poza `zadanie.version` (7.3, 8.4, 10.1.2, 12.3, wyliczane pole w 12.4, 13.6, C-36, ADR-029); C-36 sprawdzono pod kątem wyścigu z rozpatrzeniem ostatniego zgłoszenia — wynik jest spójny z regułą „≥ 3 otwarte”.

### Podsumowanie iteracji 3

Weryfikacja uwag otwartych po iteracji 2:

| Stan | Liczba | Identyfikatory |
|---|---|---|
| ZAMKNIĘTA | 17 | C1-21, C2-01 … C2-16 |
| OTWARTA | 0 | — |

Odstępstwa architekta od sugerowanych środków oceniono jako skuteczne: C2-01 (IPW z podwójnym odsetkiem Kontroli zamiast wyłączenia z populacji), C2-07 (przydział z rejestru procesów zamiast token bucketa w PostgreSQL) i C2-08 (wariant krytyka: odroczenie anulowania). Skutki uboczne tych rozwiązań opisują nowe uwagi C3-01, C3-04 i C3-08.

Nowe uwagi iteracji 3:

| Waga | Liczba uwag | Identyfikatory |
|---|---|---|
| KRYTYCZNA | 0 | — |
| WYSOKA | 1 | C3-01 |
| ŚREDNIA | 6 | C3-02 … C3-07 |
| NISKA | 2 | C3-08, C3-09 |
| **Razem** | **9** | |

Werdykt: wersja 2.2 zamyka wszystkie 17 uwag z poprzedniej iteracji. Zmiany sprawdzono w DDL, przepływach, API i rachunkach (analiza mocy D-12, model tygodniowy 17.7, budżet S-1…S-7). Wymagania funkcjonalne FR-01 … FR-36 i niefunkcjonalne NFR-01 … NFR-10 mają pokrycie w przepływach i API, a zawężenia wymagań są uczciwie przekazane do G-0 (D-10 … D-14). Dokument nie jest jednak jeszcze gotowy do produkcji:
- reguła bezpiecznika trafności (i bezpiecznika poprawek) jest statystycznie sprzeczna z własnym minimum `n_eff ≥ 189`, więc na starcie i po awariach Jev wyłącza ocenianie automatyczne i unieważnia model obciążenia (C3-01, WYSOKA);
- nowe mechanizmy RODO i filtr danych osobowych mają skutki uboczne: zmianę treści matematycznej bez Weryfikacji (C3-02), fałszywe unieważnianie wykonanych usunięć (C3-03) i osierocone kopie po PITR (C3-06);
- globalny ogranicznik Jev i Sprawdzenie zgodności poprawek mają niespójne lub niezweryfikowane parametry wpływające na NFR-01 i obciążenie Nauczycieli (C3-04, C3-05);
- w nowych identyfikatorach pojawiły się naruszenia słownika (C3-07).

## Iteracja 4

Data przeglądu: 2026-11-01. Wersja dokumentu: 2.3. Uwagi C3-01 … C3-09 sprawdzono w treści dokumentu (DDL, przepływy, API, prace w tle, rachunki), a nie tylko w tabeli 23.6. Statusy są wpisane przy uwagach iteracji 3. Rachunki C3-01 (granice Wilsona, `k_min`, P(wyzwolenie), symulacja 90 dni, scenariusz startowy) i C3-04 (przydziały, granica chwilowa, kubełek 4, S-6) przeliczono niezależnie. Odstępstwa architekta oceniono jako skuteczne: C3-01 (LB Wilsona zamiast SPRT, uzasadnione w ADR-046), C3-02 (krok z treścią matematyczną zostaje i kieruje do T-3 zamiast maskowania fragmentu), C3-03 (oba warianty jednocześnie) i C3-06 (sprzątanie zamiast Zdarzenia RODO). Pliki symulacji (`tests/kalibracja/symulacja_bezpiecznika.py`, `tests/obciazenie/symulacja_ogranicznika.py`) oceniono jako elementy do wytworzenia: opisy wystarczają do ich zbudowania. Decyzje G-0 (D-1 … D-15) z pełną opcją domyślną nie są traktowane jako błędy. Uwagi poniżej dotyczą fragmentów nowych lub zmienionych w wersji 2.3.

### C4-01: `ponow_zdarzenia_rodo` pomija wykonane usunięcie, gdy punkt odtworzenia wypada w trakcie `USUN_KONTO`, i nie rozróżnia ról kont
- **Waga:** ŚREDNIA
- **Lokalizacja:** 16.4 procedura odtworzenia p. 2 („pomija … zdarzenia, których `id` już jest w `zdarzenie_rodo`”); 16.4 kroki 0–6 `USUN_KONTO` (wiersz `zdarzenie_rodo` w kroku 1, kroki 2–5 jako osobne operacje); 16.4 „Usunięcie konta Nauczyciela lub Administratora” p. (1)–(5); 13.3 `UZGODNIJ_REJESTR_RODO` p. (2); 13.6 `KontoUsuwane` / `KontoPrzywrocone`; 17.6 G-6 (a); uzupełnienie ADR-041
- **Problem:**
  1. **Reguła pomijania po `id` jest poprawna dla zdarzeń `API`, ale nie dla `WORKER`.** Dla zdarzenia `API` istnienie wiersza oznacza zatwierdzoną transakcję, czyli zastosowany skutek. Od wersji 2.3 wiersz `USUNIECIE_WYKONANE` powstaje w kroku 1, czyli na **początku** usunięcia. Kroki 2–5 to aktualizacja wszystkich partycji `decyzja_modelu`, usuwanie obiektów S3 i `DELETE FROM uzytkownik`. Trwają minuty, a `USUN_KONTO` dla wszystkich zaplanowanych kont startuje razem o 02:00 UTC. Jeśli punkt odtworzenia `T` wypada po kroku 1, a przed krokiem 5, odtworzona baza ma wiersz zdarzenia **i** pełne dane konta. Skrypt pomija zdarzenie, workery startują, a konto `USUWANE` z Transkrypcjami, Ocenami i Profilem istnieje dalej. Usunie je dopiero `UZGODNIJ_REJESTR_RODO` (2) po ≥ 2 h od zapisu obiektu albo nocne `USUN_KONTO`. G-6 (a) deklaruje „w obu wariantach konto jest usunięte przed startem workerów”, ale testuje tylko odtworzenie 3 min i 60 min **po** usunięciu. Test przejdzie, a gwarancja nie jest spełniona.
  2. **Ponowienie nie zależy od roli konta.** Dla `USUNIECIE_WYKONANE` skrypt wykonuje „kroki 1–6 `USUN_KONTO`”, czyli kroki Ucznia. Usunięcie konta Nauczyciela ma inne kroki: archiwizację nieprzekazanych Klas i zamknięcie Przypisań, `WYCOFANE` dla Zadań `PRYWATNY` oraz pseudonimizację imienia i nazwiska w `rekomendacja.uzasadnienie` (`R-PRZYPISANIE`). Odtworzenie wykonane krokami Ucznia zostawia dane osobowe Nauczyciela w uzasadnieniach rekomendacji Uczniów. Ponowienie `WNIOSEK_O_USUNIECIE` ustawia tylko `status` i termin, bez skutków `KontoUsuwane` (zwolnienie blokad, eskalacja `KARENCJA_KONTA_NAUCZYCIELA`, `KLASA_DO_PRZEKAZANIA`). Weryfikacje `KLASA` takiego Nauczyciela czekają więc do eskalacji po SLA (3 dni robocze). `ANULOWANIE_USUNIECIA` odwołuje się do „tabeli wyżej”, czyli do tabeli przywrócenia Ucznia.
- **Oczekiwana poprawka:** dla zdarzeń `zrodlo = WORKER` kryterium zastosowania ma brzmieć „wiersz `uzytkownik` nie istnieje”, a nie „istnieje wiersz `zdarzenie_rodo`”: skrypt wykonuje idempotentne kroki usunięcia, gdy konto istnieje. Ponowienie każdego zdarzenia ma zależeć od roli konta (rola zapisana w `dane` zdarzenia albo odczytana z wiersza), z krokami (1)–(5) dla Nauczyciela oraz skutkami `KontoUsuwane` / `KontoPrzywrocone`. G-6 (a) należy rozszerzyć o wariant „`T` po kroku 1, przed krokiem 5” oraz o konto Nauczyciela.
- **Status:** ZAMKNIĘTA (iteracja 5). Tabela kryterium zastosowania w 16.4 p. 2 zależy od źródła: `API` — wiersz `zdarzenie_rodo` albo `UNIEWAZNIENIE`; `WORKER` — brak wiersza `uzytkownik`. Skrypt przegląda cały prefiks `wykonane/` bez filtra czasu (≤ 1 800 obiektów). Każde Zdarzenie RODO ma `dane.rola` (CHECK `dane ? 'rola'` w 7.3), a ponowienie wywołuje te same funkcje domenowe co `api` i `USUN_KONTO` (`SluzbaKont.*`). Dla Nauczyciela są kroki (1)–(5) i skutki `KontoUsuwane` / `KontoPrzywrocone` (13.6), a tabela przywrócenia jest właściwa dla roli. G-6 ma warianty (a3) (zatrzymanie po krokach 1–4 zmienną testową) i (a4) (konto Nauczyciela: wniosek, usunięcie w trakcie, anulowanie). Jest też scenariusz C-39. Sprawdzono, że przywrócenie konta po kroku 0 nie jest możliwe: `POST …/przywrocenie` wymaga karencji < 30 dni, a `USUN_KONTO` startuje po `usuniecie_zaplanowane_at`, więc obiekt `USUNIECIE_WYKONANE` nie może dotyczyć konta przywróconego.

### C4-02: Kolejność wyboru progu i bezpiecznika trafności jest nieokreślona, więc zwykła reguła KALIBRUJ może codziennie cofać podniesienie progu
- **Waga:** ŚREDNIA
- **Lokalizacja:** 11.5.3 (pseudokod wyboru progu z obniżaniem ≤ 0,02 na dobę; „Reguła (codziennie, po wyborze progu)”; reguła 2: `prog := min(0,990, prog + 0,02)`; reguła 3 tylko przy `LB ≤ 0,02`; tabela reguł nadzoru); 9.8 (jeden `INSERT prog_pewnosci` „gdy reguły nadzoru pozwalają”); symulacja 11.5.3 (tylko pierwsze wyzwolenie); testy (a)–(f); E-21; ADR-046 („degradacja do 4% wykrywana w 2–4 dni”)
- **Problem:**
  1. Bezpiecznik działa „po wyborze progu”, a reguła 2 podnosi `prog` o 0,02. Dokument nie mówi, czy `prog` to wartość z poprzedniego dnia, czy wartość wybrana tego dnia, ani czy wyzwolenie blokuje obniżanie. Blokada obniżania (reguła 3) obejmuje wyłącznie przypadek `LB ≤ 0,02`, więc w stanie wyzwolenia (`LB > 0,02`) obniżanie nie jest zablokowane wprost. Bezpieczniki braku nadzoru w tabeli reguł mają blokadę zapisaną jawnie, a bezpiecznik trafności nie.
  2. Przykładowy przebieg. Dzień N: bezpiecznik podnosi próg z 0,930 do 0,950. Dzień N + 1: wybór progu na oknie 90 dni (60 dni danych z błędem 0,5% rozcieńcza 30 dni degradacji) daje `min t` = 0,930 i obniża próg o dozwolone 0,02. Bezpiecznik, liczony „powyżej bieżącego progu”, znów podnosi go do 0,950. Próg oscyluje i nie rośnie, choć LB > 2% trwa. Przy takiej interpretacji bezpiecznik daje najwyżej +0,02 ponad próg zwykłej reguły. Deklaracja „degradacja do ≥ 4% wykrywana w 2–4 dni” opisuje więc wykrycie (alert), a nie skuteczną reakcję. Symulacja zlicza tylko pierwsze wyzwolenie i nie modeluje dynamiki progu ani odsetka Ocen automatycznych po nim.
  3. Brak reguły wyjścia: nie wiadomo, kiedy kończy się stan „po wyzwoleniu”, gdy po podniesieniu LB spada poniżej 2% (próba powyżej wyższego progu jest mniejsza), a UB pozostaje > 2%.
- **Oczekiwana poprawka:** jawna kolejność i łączny wynik dnia, np. `prog_nowy = max(wybór_progu, prog_obecny + 0,02)` przy wyzwoleniu. Zakaz obniżania progu pary, dopóki LB > 2% albo UB > 2% na oknie 30 dni powyżej progu, do którego reguła chce obniżyć. Test jednostkowy: „degradacja 4% przez 30 dni na tle 60 dni z błędem 0,5% → próg rośnie monotonicznie, aż LB ≤ 2%”. Rozszerzenie symulacji o trajektorię progu i odsetek automatycznych po wyzwoleniu.
- **Status:** ZAMKNIĘTA (iteracja 5). „Wynik dnia KALIBRUJ” (11.5.3) ma kroki 1–6 w jawnej kolejności. Bezpieczniki są oceniane na `p0` i `K0` sprzed przebiegu. Blokada obniżania progu ma wejście (wyzwolenie) i wyjście (UB ≤ 2% przy `n_eff ≥ 189`), a krok 4 przy blokadzie ustawia `p_dol ≥ p0`. Przy wyzwoleniu `p_nowy = min(0,990, max(p_dol, p0 + 0,02))`, a przy 0,990 następuje przejście do 1,000. Zapis to jeden rekord `prog_pewnosci`. Przebieg z C4-02 p. 2 sprawdzono krokami: dzień N + 1, `p_wyb = 0,930`, BT aktywny → `p_dol = 0,950` → brak oscylacji (test (h)). Niezależnie przeliczono testy (f) (n = 300, k = 3: UB 2,90%, LB 0,34%) i (i) (n = 400, k = 2: UB 1,80%) oraz rozkład `1 − p ~ Exp(0,0631)` (udział auto 67,0 / 54,7 / 37,8 / 14,7% przy 0,930 / 0,950 / 0,970 / 0,990) i wariant 17.7 dla e = 2% (`M` ≈ 3 157, 84 min). Wszystko się zgadza. Odstępstwo (tryb pełnej weryfikacji przy 0,990, mediana wykrycia 13 dni zamiast 2–4) oceniono jako uczciwe, bo koszt jest policzony i przekazany do D-16. Skutek uboczny nowego kroku 4 opisuje C5-02.

### C4-03: Filtr danych osobowych nie chroni Uczniów, których imię lub nazwisko jest terminem matematycznym, a działanie reguły (a) na Poprawkach jest nieokreślone
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.3.1 p. 3a: (a) „bez tokenu ze Słownika terminów matematycznych”, (b) „Token ze Słownika … nigdy nie jest zastępowany …, także gdy jest równy nazwisku Ucznia”, „W Poprawkach Ucznia filtr stosuje tylko dopasowania `DOKLADNE`”; A-09 (≥ 30 kartek z nazwiskiem równym terminowi); 17.6 G-2 („0 wystąpień imienia i nazwiska z konta”, `konflikt_filtra ≤ 1%`); 11.1 p. 7d; E-37; 17.7
- **Problem:**
  1. Linia podpisu „Jan Pole” zawiera token „pole” ze Słownika, więc reguła (a) uznaje ją za krok **z** treścią matematyczną. Tekst zostaje, ustawia się `konflikt_filtra = true`, a reguła (b) z definicji nie zastępuje „pole”. Nazwisko trafia do Transkrypcji, stanu Jev, Opinii pomocniczej, kolejki weryfikatora globalnego i kopii treningowej. Chroni je tylko reguła promptu, czyli brak gwarancji, który ADR-042 miał usunąć. Kryterium G-2 „0 wystąpień” dla ≥ 30 kartek z nazwiskiem-terminem jest niespełnialne przez kod deterministyczny: bramka albo nie przejdzie, albo przejdzie wyłącznie dzięki LLM, bez gwarancji na produkcji.
  2. Taki Uczeń przy każdym podpisanym Rozwiązaniu dostaje `konflikt_filtra` → T-3 → Ocenę wstrzymaną i Weryfikację. Systematyczna Weryfikacja wszystkich Rozwiązań wąskiej grupy Uczniów nie jest ujęta w 17.7, a średnie kryterium `konflikt_filtra ≤ 1%` ją ukrywa. Uczeń nie może tego naprawić Poprawką, bo flaga wynika z ramki, nie z treści.
  3. Dokument nie określa, czy reguła (a) (ramka) działa ponownie na Poprawkach, a ramka i `obszar` kroku się nie zmieniają. Jeśli działa, Uczeń nie przywróci kroku zamaskowanego w całości jako `tylko_dane_osobowe` (np. zdania wniosku bez cyfr, ≤ 6 tokenów, zapisanego obok podpisu), więc FR-18 dla tego kroku przestaje działać, a Model decyzyjny dostaje „linia z danymi osobowymi — nie jest krokiem rozwiązania” zamiast wniosku. Jeśli nie działa, Poprawka wpisuje do kroku w ramce dowolny tekst, który przechodzi tylko przez filtr `DOKLADNE`.
- **Oczekiwana poprawka:** w regule (a) tokeny równe imieniu lub nazwisku z konta nie liczą się jako treść matematyczna. W regule (b) token ze Słownika równy nazwisku jest zastępowany, gdy sąsiaduje z drugim członem imienia i nazwiska albo leży w kroku z `podejrzenie_danych_osobowych`. Należy określić działanie reguły (a) dla Poprawek, np. krok `tylko_dane_osobowe` przywrócony Poprawką nie jest ponownie maskowany, ale podlega Sprawdzeniu zgodności i T-7. W G-2 potrzebne jest osobne kryterium dla kartek z nazwiskiem-terminem i metryka `konflikt_filtra` per Uczeń (np. odsetek Uczniów z ≥ 50% Rozwiązań z konfliktem).
- **Status:** ZAMKNIĘTA (iteracja 5). W regule (a) tokeny imienia i nazwiska z konta, inicjały i oznaczenie klasy są wyłączane przed oceną treści matematycznej, więc „Jan Pole, kl. 3b” daje `tylko_dane_osobowe` bez `konflikt_filtra`. Reguły (b1) i (b2) zastępują token Słownika równy nazwisku. Tabela działania filtra na Poprawkach (10.3.1 p. 3a) rozstrzyga C4-03 p. 3: reguła (a) nie jest liczona ponownie, krok przywrócony nie jest maskowany i podlega limitom 9.4, Sprawdzeniu zgodności i T-5/T-7. G-2 ma osobne kryterium dla ≥ 30 kartek z nazwiskiem-terminem, a metryka `uczniowie_z_konfliktem_filtra_udzial` ma alert (17.3, 17.7). Oba problemy z C4-03 są usunięte. Nowy skutek uboczny warunku (b1) z inicjałem opisuje C5-01.

### C4-04: Nowe identyfikatory używają słowa „zdjęcie” w znaczeniu „dezaktywacja bezpiecznika”
- **Waga:** ŚREDNIA
- **Lokalizacja:** 7.3 `stan_bezpiecznika` (`zdjety_przez`, `zdjeto_at`, `uzasadnienie_zdjecia`); 12.3 `POST /admin/bezpieczniki/zdjecie`; 12.4 `StanBezpiecznikaWyjscie.zdjeto_at`; 11.5.3 („Zdjęcie bezpiecznika poprawek”); 3.1 p. 5; 3.2 (Rozwiązanie → `zdjecie_rozwiazania`); 3.4 test CI, części 2 i 3
- **Problem:** „zdjęcie” w modelu oznacza fotografię kartki. To element definicji Rozwiązania w słowniku pojęć („przesłana jako zdjęcie kartki”) i identyfikatorów `zdjecie_rozwiazania`, buckety `zdjecia/` i `zdjecia-maskowane/`, `?wersja_zdjecia=`, `ObszarNaZdjeciu`, `widoczny_na_zdjeciu`, `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`. Wersja 2.3 dodaje `uzasadnienie_zdjecia` (czytane „uzasadnienie zdjęcia”), `zdjeto_at`, `zdjety_przez` i ścieżkę `/admin/bezpieczniki/zdjecie` w znaczeniu „zdjęcie (dezaktywacja) bezpiecznika”. To homonim tego samego rodzaju co `ZADANIE_USUNIECIA` w C3-07 i łamie 3.1 p. 5 („jedno słowo — jedno znaczenie”). Test CI go nie wykryje: część 2 uzna `zdjecie` za token słownikowy, a lista słów zarezerwowanych w części 3 nie zawiera `zdjec*`. Pokrewna, łagodniejsza niejednoznaczność: „wstrzymanie” ma w identyfikatorach dwa znaczenia (`wstrzymanie_w_doborze` oraz Ocena wstępna `WSTRZYMANA_BLAD_ODCZYTU`), a w prozie trzecie (procesy „wstrzymane” w karencji).
- **Oczekiwana poprawka:** zastąpić słowem „dezaktywacja” (`POST /admin/bezpieczniki/dezaktywacja`, `dezaktywowal_id`, `dezaktywowano_at`, `uzasadnienie_dezaktywacji`) i dopisać `zdjec*` do słów zarezerwowanych testu CI z listą dozwolonych użyć (znaczenie: fotografia). Rozważyć ujednolicenie „wstrzymanie” albo jego definicję w 3.1 p. 5.
- **Status:** ZAMKNIĘTA (iteracja 5). 3.1 p. 5 definiuje „zdjęcie” wyłącznie jako fotografię i wprowadza „dezaktywację”, „odebranie” i „wyłączenie”. Kolumny `dezaktywowany_przez`, `dezaktywowano_at`, `uzasadnienie_dezaktywacji` (7.3), endpoint `POST /admin/bezpieczniki/dezaktywacja` (12.3) i DTO są przemianowane. W całym dokumencie `zdjet*` / `zdjeto` / `bezpieczniki/zdjecie` występuje tylko w regule testu CI, przypadku kontrolnym i changelogu (sprawdzone grep). Test CI ma słowa zarezerwowane `zdje*` i `wstrzym*` z listami dozwolonych użyć, a „wstrzymanie” ma jedną definicję (3.1 p. 5, 3.3). Pokrewną niejednoznaczność słowa „blokada” opisuje C5-04.

### C4-05: Granica chwilowa globalnego ogranicznika zależy od liczby procesów
- **Waga:** NISKA
- **Lokalizacja:** 13.2 („≤ Σ B_i + 0,8 × limit × T = 34 + 40 T”, „`pojemnosc_kubelka_workera = 4` daje Σ (B_i + r_i) = 50,0”, test (e)); 7.3 `limit_szybkosci_modelu.pojemnosc` (stała dla typu procesu); 17.4 („skalowanie: 3. replika workera”); A-01 (tolerancja 75 wywołań w 1 s); G-4 (seria 74 wywołań)
- **Problem:** przydziały r_i zawsze sumują się do 40/s, ale Σ B_i rośnie z liczbą procesów, bo pojemność jest stała dla procesu (worker 16, `api` 1). Przy 3 workerach i 2 `api` Σ B = 50, więc w oknie 1 s procesy mogą wysłać do 90 wywołań, a tolerancja A-01 to 75. W wariancie ścisłym (B = 4) wychodzi 3 × 4 + 2 + 40 = 54 > 50. Także chwilowo, przy dołączeniu procesu, istniejące procesy do najbliższego odnowienia (≤ 10 s) mają stary przydział: 2 × 18,18 + 12,5 + 2 × 1,82 ≈ 52,5/s > 50/s. Skutek: odpowiedzi `429`, przydział zmniejszony o połowę i Oceny przekierowane do modelu zapasowego w szczycie, i to właśnie przy skalowaniu, które 17.4 podaje jako mitygację. Test (e) sprawdza tylko sumę przydziałów, a G-4 kalibruje 74 wywołania pod dokładnie 2 workery.
- **Oczekiwana poprawka:** pojemność proporcjonalna do wagi (`B_i = B_całk × waga_i / Σ wag`, z ograniczeniem Σ B ≤ tolerancja − 0,8 × limit × 1 s). Nowy proces startuje z pustym kubełkiem i przydziałem min(r_nowy, 0,8 × limit − Σ r_pozostałych) do najbliższego odnowienia. Dodać test „granica chwilowa przy 2 / 3 / 4 workerach ≤ tolerancja A-01”.
- **Status:** ZAMKNIĘTA (iteracja 5). Przeliczono niezależnie (13.2): `B_i = ⌊(B_całk − n_api) × w_i / Σ w⌋`. Przy 2 / 3 / 4 workerach Σ B wynosi 34 / 32 / 34, granica 1 s 74 / 72 / 74 ≤ 75, a w oknie 5 s ≤ 234. W wariancie ścisłym (B_całk = 10) granica wynosi 50 / 48 / 50. Ograniczenie `min(formuła, 0,8 × limit − Σ pozostałych)`, zmniejszanie przed `COMMIT` i zwiększanie po `COMMIT` pod `pg_advisory_xact_lock` zachowują oba niezmienniki także przy dołączaniu procesu (pusty kubełek, odnowienie co 2 s). Dodano testy (f)–(h) i C-40. Rozpoznawanie: ogranicznik R / S z `B_całk = 30` daje Σ B = 28, czyli ≤ 148 w oknie 60 s (sprawdzone). Sprzeczność z CHECK w DDL opisuje C5-03.

### C4-06: Drobne niespójności wersji 2.3
- **Waga:** NISKA
- **Lokalizacja:** 10.3.3; 13.3 / 13.5 / 16.4 / ADR-041 / G-6; 16.4 krok 0; 15.2 C-37 i 9.6.2; 11.5.3; 11.4.3 (`odsetek_eskalacji_7d`); 17.7; 13.4 / 17.4 / 7.3 `limit_szybkosci_modelu`; 17.2 S-6 / 17.4; 9.4
- **Problem:**
  1. 10.3.3: „Praca `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` (pula R, …)”, a od wersji 2.3 jest to pula S (13.3, 13.4, 22, 9.4).
  2. Okno unieważniania zdarzeń `API`: warunek 13.3 „starsze niż 5 min” przy cyklu co 10 min (13.5) daje do 15 min, a 16.4, ADR-041 i G-6 (a) mówią o 10 min.
  3. Klucz obiektu `USUNIECIE_WYKONANE` zawiera datę `{rrrr-mm-dd}`. Ponowienie w innym dniu UTC (np. przez `UZGODNIJ_REJESTR_RODO` po 2 h albo po PITR) zapisuje drugi obiekt z tym samym `id` mimo `If-None-Match: *`. Skutek jest nieszkodliwy (deduplikacja po `id`), ale opisany mechanizm idempotencji nie działa. Klucz nie powinien zależeć od daty albo data powinna wynikać z `usuniecie_zaplanowane_at`.
  4. C-37 / 9.6.2: stan konta jest sprawdzany po `SELECT rozwiazanie FOR UPDATE`, ale bez blokady wiersza `uzytkownik`, a transakcja wniosku nie blokuje `rozwiazanie`. Wniosek zatwierdzony między sprawdzeniem a COMMIT rozstrzygnięcia oznacza, że Ocena Nauczyciela powstaje po wniosku. Wystarczy `SELECT status FROM uzytkownik … FOR SHARE`.
  5. 11.5.3: sformułowanie „test jednostronny na poziomie 2,5% z dyskretnością rozkładu” jest nieścisłe. Rzeczywisty rozmiar przy e = 2% wynosi 2,6–7,8%, bo granica Wilsona jest antykonserwatywna przy małym n. Przy codziennej ocenie przez 90 dni P(≥ 1 wyzwolenie) przy e = 2% wynosi 31–36% (tabela symulacji, potwierdzone). To akceptowalny kompromis, ale należy go nazwać wprost jako oczekiwaną liczbę fałszywych podniesień progu na kwartał na granicy NFR-03.
  6. Eskalacje z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA` wliczają się do `odsetek_eskalacji_7d` (11.4.3). Po przywróceniu konta Klasy Nauczyciela zostają więc pod kontrolą przyjęć (wyzwolenie przy ≥ 0,20, zdjęcie dopiero przy < 0,10) przez kilka dni, a odsetek globalny uruchamia przeliczenie 17.7 i alert. Ten powód należy wyłączyć z miary.
  7. Aktywny bezpiecznik poprawek kieruje do T-7 każdą Transkrypcję z Poprawkami: ≈ 0,20 × 0,47 × 1 200 ≈ 110 Weryfikacji/dobę, ≈ 340 min/dobę, czyli ≈ +9% `M` na starcie. 17.7 nie ma tego wariantu, a G-7 = 18 nie ma na niego zapasu.
  8. Ogranicznik dostawcy rozpoznawania z wagami pul R 1,0 / S 0,3 (13.4, 17.4) nie mieści się w kluczu `limit_szybkosci_modelu (konfiguracja_modelu_id, wykonawca)`, bo jedna replika ma obie pule tej samej konfiguracji. Należy określić `wykonawca` per pula (np. `worker#1/S`) albo dodać kolumnę puli do klucza.
  9. 17.2 S-6 i 17.4 liczą popyt jako 29 wywołań na Ocenę, a symulacja używa planu 16 + 11 + 1 = 28. Wartości trzeba ujednolicić (różnica ≈ 3,5%).
  10. Okno na rezultat Sprawdzenia `min(zatwierdzono_at + 6 s, termin_miekki − 9 s)` (9.4) maleje do 1 s przy `t_R` ≥ 18 s (termin miękki 10 s), a rachunek log-normalny zakłada 6 s. Przy wolnym rozpoznaniu p_brak przekracza więc 0,5%. Należy dopisać odsetek warunkowy albo nie skracać okna poniżej p95 Sprawdzenia.
- **Oczekiwana poprawka:** ujednolicić wskazane miejsca: p. 1, 2 i 9 redakcyjnie, p. 3, 4, 6 i 8 drobną zmianą mechanizmu, p. 5, 7 i 10 uzupełnieniem rachunku.
- **Status:** ZAMKNIĘTA (iteracja 5). Sprawdzono każdy z 10 podpunktów w treści: (1) 10.3.3 — pula S; (2) 13.3 / 13.5 — cykl 5 min, wiek > 5 min, okno ≤ 10 min, zgodne z 16.4, ADR-041 i G-6; (3) klucz `wykonane/{uzytkownik_id}/{id}.json` bez daty w 7.3, 13.3 i 16.4; (4) `SELECT status FROM uzytkownik … FOR SHARE` po blokadach `rozwiazanie` → `weryfikacja` (9.6.2, 9.6.3, C-37); `UPDATE` wniosku bierze `FOR NO KEY UPDATE`, które jest sprzeczne z `FOR SHARE`, więc transakcje są serializowane bez cyklu blokad; (5) rozmiar testu nazwany wprost (2,6–7,8%, 1,1 fałszywego podniesienia na kwartał, D-16, R-22); (6) `KARENCJA_KONTA_NAUCZYCIELA` poza `odsetek_eskalacji_7d` (3.3, 11.4.3); (7) wariant 17.7 przeliczony: start +249 min, `M` = 3 951, 105 min, 20 weryfikatorów; faza ustalona +451 min, `M` = 3 277, 87 min, 16 weryfikatorów; 8 100 / 6 636 = 1,22 (D-17); (8) klucz `(konfiguracja_modelu_id, wykonawca, pula)`; (9) 28 wywołań w S-6, 17.4 i 22, 29 jako maksimum; (10) okno stałe `zatwierdzono_at + 6 s`, 6,2 s < 10 s. Drobne rozbieżności redakcyjne nowego tekstu opisuje C5-05.

### Podsumowanie iteracji 4

Weryfikacja uwag otwartych po iteracji 3:

| Stan | Liczba | Identyfikatory |
|---|---|---|
| ZAMKNIĘTA | 9 | C3-01 … C3-09 |
| OTWARTA | 0 | — |

Niezależne przeliczenia potwierdziły rachunki wersji 2.3. Dla C3-01: UB przy zerze błędów, `k_min`, LB, P(wyzwolenie), minima `n_eff` i scenariusz startowy 17.7 są zgodne co do zaokrągleń, a odtworzona symulacja 90 dni mieści się w błędzie Monte Carlo (≤ 0,8 pp). Dla C3-04: przydziały 18,18 / 1,82 / 40 na sekundę, granica chwilowa 74 / 234, wariant B = 4 (50,0, 0,66 s) i S-6 (26,2 s) są zgodne. Oba opisy symulacji wystarczają do zbudowania skryptów.

Nowe uwagi iteracji 4:

| Waga | Liczba uwag | Identyfikatory |
|---|---|---|
| KRYTYCZNA | 0 | — |
| WYSOKA | 0 | — |
| ŚREDNIA | 4 | C4-01, C4-02, C4-03, C4-04 |
| NISKA | 2 | C4-05, C4-06 |
| **Razem** | **6** | |

Werdykt: wersja 2.3 zamyka wszystkie 9 uwag iteracji 3, w tym jedyną wysoką (C3-01). Wymagania FR-01 … FR-36 i NFR-01 … NFR-10 mają pokrycie w przepływach i API, a zawężenia są przekazane do G-0 z opcjami domyślnymi (D-1 … D-15). Nie ma otwartych uwag krytycznych ani wysokich. Dokument nie jest jednak jeszcze na poziomie produkcyjnym w czterech miejscach, które dotyczą mechanizmów ochronnych dla Ucznia i RODO:
- procedura odtworzenia PITR nie gwarantuje usunięcia konta przed startem workerów, gdy punkt odtworzenia wypada w trakcie `USUN_KONTO`, i nie odtwarza kroków właściwych dla konta Nauczyciela (C4-01);
- skuteczność bezpiecznika trafności zależy od nieokreślonej kolejności względem wyboru progu, który może codziennie cofać podniesienie (C4-02);
- Filtr danych osobowych z definicji nie chroni nazwisk równych terminom matematycznym, a bramka G-2 jest dla nich niespełnialna przez kod deterministyczny (C4-03);
- w nowych identyfikatorach pojawił się homonim słowa „zdjęcie” (C4-04).

Wszystkie cztery poprawki są lokalne i nie wymagają zmian architektury.

## Iteracja 5

Data przeglądu: 2026-11-01. Wersja dokumentu: 2.4. Uwagi C4-01 … C4-06, w tym 10 podpunktów C4-06, sprawdzono w treści dokumentu (DDL 7.3, przepływy 9.x, 10.3.1, 11.5.3, 13.2–13.5, 16.4, 17.6–17.7), a nie tylko w tabeli 23.7. Statusy są wpisane przy uwagach iteracji 4. Rachunki przeliczono niezależnie: granice Wilsona w testach (f) i (i), rozkład pewności w symulacji dynamiki, granicę chwilową ogranicznika dla 2 / 3 / 4 workerów i ogranicznika rozpoznawania oraz warianty 17.7. Wszystkie się zgadzają. Odstępstwa architekta (C4-02: tryb pełnej weryfikacji i skorygowana mediana wykrycia; C4-04: `dezaktywowany_przez`; C4-05: stała pojemność `api`) oceniono jako skuteczne. Uwagi poniżej dotyczą fragmentów nowych w wersji 2.4 (kroki KALIBRUJ, `B_całk`, reguły b1/b2, procedura odtworzenia) oraz planu `TASKS.md`.

### C5-01: Warunek (b1) z inicjałem zastępuje termin matematyczny w zwykłym kroku rozwiązania, a Poprawka nie może go przywrócić
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.3.1 p. 3a reguła (b1) („sąsiaduje … z tokenem dopasowanym do drugiego członu imienia i nazwiska z konta albo z jego inicjałem … dopasowanie `DOKLADNE`”, „z pominięciem znaków interpunkcyjnych”, normalizacja: małe litery, bez diakrytyków); tabela „Działanie filtra na Poprawkach” (krok zmieniony: „`DOKLADNE` z warunkiem (b1)”); 17.6 G-2 („0 zastąpień tokenów ze Słownika terminów matematycznych … 200 par … w tym nazwiska Pole, Kula, …”); ADR-042 (uzupełnienie it. 6)
- **Problem:**
  1. Inicjał po normalizacji i pominięciu kropki to pojedyncza litera. W tekście matematycznym jednoliterowe oznaczenia stoją obok terminów bardzo często: „Pole P”, „prosta k”, „prosta l”, „suma S”, „koło o”. Przykład: Uczeń Piotr Pole pisze „Pole P trójkąta wynosi” poza ramką danych osobowych. Token „pole” jest równy nazwisku i sąsiaduje z „p”, czyli inicjałem imienia, więc (b1) zastępuje go znacznikiem `[DANE_OSOBOWE]`. Krok nie ma ramki, więc nie dostaje ani `podejrzenie_danych_osobowych`, ani `konflikt_filtra`. Model decyzyjny ocenia „[DANE_OSOBOWE] P trójkąta wynosi”, a Ocena może być wystawiona automatycznie. Sygnał filtra zmienia więc treść kroku bez Weryfikacji, czyli to, czego zabraniają C3-02 i Z-1.
  2. Gdy Uczeń przywraca słowo („To nie są moje dane — przywróć”), krok jest „zmieniony”, a dla takiego kroku tabela Poprawek stosuje „`DOKLADNE` z warunkiem (b1)”. „Pole P” jest zastępowane ponownie, więc FR-18 dla tego kroku nie działa. Przykład z tabeli („przywrócone słowo „Pole” zostaje”) zakłada brak sąsiedztwa.
  3. Kryterium G-2 „0 zastąpień tokenów ze Słownika” na 1 050 Transkrypcjach z syntetycznymi danymi konta (w tym nazwisko Pole) jest sprzeczne z regułą (b1), która z założenia zastępuje token Słownika. Przy parze „Piotr Pole” i Transkrypcji z „pole P” bramka nie przejdzie. Gdyby przeszła, to tylko dlatego, że losowe pary nie trafiły na kolizję inicjału z oznaczeniem, czyli bez sprawdzenia tego przypadku.
- **Oczekiwana poprawka:** w (b1) inicjał liczy się tylko z kropką w oryginalnym tekście (przed pominięciem interpunkcji) i tylko wtedy, gdy krok nie ma treści matematycznej po wyłączeniach z reguły (a). Prościej: (b1) stosować wyłącznie w krokach bez treści matematycznej, a w krokach z treścią (b1) z inicjałem ustawia `konflikt_filtra` zamiast zastępować. Dla kroku zmienionego przez Ucznia (b1) nie powinna zastępować tokenu, który Uczeń sam przywrócił w miejscu znacznika. G-2: „0 zastąpień tokenów Słownika w krokach z treścią matematyczną” oraz celowy zestaw przypadków „nazwisko-termin + oznaczenie równe inicjałowi” (np. 20 kartek „Pole P”, „prosta k”).
- **Status:** ZAMKNIĘTA (iteracja 6). 10.3.1 p. 3a rozdziela (b1) — sąsiedztwo z **pełnym** drugim członem (≥ 2 litery) — od (b1-i): inicjał to litera z kropką w oryginalnym tekście i działa wyłącznie w kroku bez treści matematycznej; w kroku z treścią daje `konflikt_filtra` (T-3) bez zastąpienia, a litera bez kropki nie jest inicjałem. Tabela Poprawek wyłącza (b1-i)/(b2) dla kroku zmienionego i zakazuje ponownego zastąpienia tokenu wpisanego w miejscu znacznika (przykład „Pole P trójkąta”). G-2 (17.6) ma kryterium „0 zastąpień w krokach z treścią matematyczną” z b1 raportowaną osobno oraz celowy zestaw ≥ 20 kartek i test przywrócenia. Resztkowy przypadek „… Pole P.” na końcu zdania daje `konflikt_filtra`, czyli kierunek bezpieczny (Z-1) — akceptowalne.

### C5-02: Reguły nadzoru w kroku 4 KALIBRUJ nie mają określonej populacji ani wartości przy braku Kontroli, co może zablokować wyjście z trybu pełnej weryfikacji
- **Waga:** ŚREDNIA
- **Lokalizacja:** 11.5.3 tabela reguł nadzoru (`wykonanie_kontroli_30d` = „Kontrole zakończone do `termin_at` / utworzone”, bezpiecznik braku nadzoru „< 50 zakończonych przy ≥ 100 utworzonych”); krok 4 („`zablokowane = BT.aktywny ∨ ostrzezenie_T ∨ wykonanie_kontroli_30d < 0,80 ∨ bezpiecznik braku nadzoru`”); skutek (4) (wyjście z trybu pełnej weryfikacji przez dezaktywację, „próg obniża się o ≤ 0,02 na dobę przez test kroku 4”); 11.5.1 p. 5 (start w trybie pełnej weryfikacji, „próg obniży się automatycznie”); 12.4 `wykonanie_kontroli_30d: number | null`
- **Problem:**
  1. W trybie pełnej weryfikacji (`p0 = 1,000`, start D-6 albo po wyzwoleniu przy 0,990) nie ma Ocen automatycznych, więc nie ma Kontroli losowych, a `wykonanie_kontroli_30d` = 0/0. API przewiduje `null`, ale krok 4 nie określa, czy `null < 0,80`. W implementacji, która traktuje brak danych zachowawczo (`null` → zablokowane), para nigdy nie obniży progu z 1,000. Tryb pełnej weryfikacji (`M` = 5 194 min/dobę, 17.7) staje się wtedy trwały, mimo dezaktywacji przez Administratora.
  2. Mianownik „utworzone w oknie 30 dni” obejmuje Kontrole, których `termin_at` jeszcze nie minął (≈ 3 dni robocze, czyli do ≈ 14% okna). Po obniżeniu progu z 1,000 do 0,980 pojawiają się pierwsze Oceny automatyczne z odsetkiem Kontroli 0,20 (mniej niż 400 próbek). To ≈ 0,20 × 0,25 × 1 200 ≈ 60 Kontroli na dobę. Po 2 dniach jest ≥ 100 utworzonych i < 50 zakończonych, choć żadna nie jest spóźniona. Wyzwala się bezpiecznik braku nadzoru (+0,02), a krok 5 przywraca 0,990–1,000. Para oscyluje przy wyjściu z trybu pełnej weryfikacji. Ten sam efekt w łagodniejszej postaci zaniża `wykonanie_kontroli_30d` w normalnej pracy (alert przy < 0,90 przy pełnym wykonaniu w terminie).
- **Oczekiwana poprawka:** zdefiniować populację: „Kontrole z `termin_at ≤ now()` utworzone w oknie 30 dni”, analogicznie dla progu „≥ 100 utworzonych”. Zdefiniować wartość przy pustej populacji: `null` = brak reguły nadzoru (bez blokady), skoro brak Kontroli wynika z braku Ocen automatycznych. Dodać test: „dezaktywacja przy `p0 = 1,000` → próg spada do 0,990, 0,970, … przy bezbłędnych Próbkach, bez wyzwolenia bezpiecznika braku nadzoru w pierwszych 3 dniach roboczych”.
- **Status:** ZAMKNIĘTA (iteracja 6). 11.5.3: populacja reguł nadzoru = Kontrole wymagalne (`termin_at ≤ now()`) utworzone w oknie 30 dni, także dla progu „≥ 100”; Kontrole przed terminem są poza licznikiem i mianownikiem; `null` przy pustej populacji = brak reguły nadzoru, a krok 4 ma jawny warunek `≠ null ∧ < 0,80`. Testy (l) (wyjście z 1,000 bez bezpiecznika braku nadzoru) i (m) (Kontrole przed terminem) dopisane. Przeliczenie scenariusza z uwagi: po obniżeniu do 0,980 przez pierwsze 3 dni robocze populacja wymagalna jest pusta, więc bezpiecznik braku nadzoru nie może się wyzwolić.

### C5-03: CHECK `pojemnosc BETWEEN 1 AND 34` jest sprzeczny z pojemnością 0 przy dołączaniu procesu i ponad limit wierszy
- **Waga:** NISKA
- **Lokalizacja:** 7.3 `limit_szybkosci_modelu.pojemnosc smallint NOT NULL CHECK (pojemnosc BETWEEN 1 AND 34)`; 13.2 („`B_nowy = min(formuła, B_całk − Σ pojemności pozostałych)` — przy pełnym wykorzystaniu … oba wynoszą 0”; „wiersz ponad tę liczbę dostaje `B_i = 0`”); test (g); C-40
- **Problem:** nowy proces rejestruje się przy pełnym wykorzystaniu z `pojemnosc = 0`, czego CHECK nie dopuszcza. `INSERT` kończy się błędem, więc replika uruchomiona w szczycie (C-40, mitygacja z 17.4) nie zarejestruje się w ogóle albo implementacja obejdzie to wartością 1 i naruszy niezmiennik `Σ pojemnosc ≤ B_całk`. To samo dotyczy wiersza ponad limit 32. Dla ogranicznika rozpoznawania (`B_całk = 30`) górna granica 34 jest luźna, ale poprawna.
- **Oczekiwana poprawka:** `CHECK (pojemnosc BETWEEN 0 AND 34)` (albo `≤ B_całk` z parametru 22), test (g) z asercją `pojemnosc = 0` po `INSERT`.
- **Status:** ZAMKNIĘTA (iteracja 6). 7.3: `CHECK (pojemnosc BETWEEN 0 AND 34)` z komentarzem; test (g) z asercją `pojemnosc = 0` (13.2, ADR-039, wymaganie DoD w 18.4).

### C5-04: „Blokada” ma w modelu trzy znaczenia, a test CI tego nie wykrywa
- **Waga:** NISKA
- **Lokalizacja:** 3.3 („Blokada weryfikacji”, „Blokada obniżania progu”); 13.2 („blokada doradcza”); 11.5.3 skutki (1) i (4) oraz komentarz DDL `stan_bezpiecznika` („aktywny = blokada obniżania”), proza bez kwalifikatora („blokada kończy się automatycznie”, „blokady `W_TRAKCIE`”); 3.4 część 2 (`blokada` dopisana do słów ogólnych `tests/slownik_tokeny.yaml`)
- **Problem:** od wersji 2.4 to samo słowo oznacza wyłączne prawo do rozstrzygnięcia Weryfikacji, stan pary po wyzwoleniu bezpiecznika i mechanizm PostgreSQL. Oba pojęcia domenowe mają kwalifikatory, ale proza i komentarze często ich nie mają. „Blokada” trafiła na listę słów ogólnych, więc test CI przepuści np. `blokada_aktywna` w obu znaczeniach. To ten sam rodzaj ryzyka co C4-04, w łagodniejszej postaci. Kolumna `stan_bezpiecznika.aktywny` ma przy tym dwa znaczenia zależne od `typ` (POPRAWKI: T-7 dla Poprawek; TRAFNOSC: Blokada obniżania progu).
- **Oczekiwana poprawka:** dopisać `blokad*` do słów zarezerwowanych z listą dozwolonych użyć (`zablokowana_do`, `BlokadaWeryfikacji…`, `blokada_obnizania…`) i ujednolicić prozę 11.5.3 do pełnej nazwy. Rozważyć osobną kolumnę `blokada_obnizania_progu` zamiast przeciążonego `aktywny` dla `TRAFNOSC`.
- **Status:** ZAMKNIĘTA (iteracja 6). 3.1 p. 5 i ADR-049 p. 9: dwa znaczenia domenowe z kwalifikatorem, `praca_w_tle.dzierzawa_do`, blokada doradcza PostgreSQL wyłącznie w prozie; `blokad*`/`zablokow*` są słowami zarezerwowanymi z listą dozwolonych użyć i dwoma przypadkami kontrolnymi; `stan_bezpiecznika.aktywny` ma jedno znaczenie, a skutki są właściwościami `BlokadaObnizaniaProgu` / `KazdaPoprawkaDoWeryfikacji` (3.6). Lista dozwolonych użyć zawiera jednak trzecie, niezdefiniowane znaczenie (konto zablokowane) — opisuje to C6-13.

### C5-05: Drobne niespójności wersji 2.4
- **Waga:** NISKA
- **Lokalizacja:** 3.4 część 3; 3.2 / 18 / 12.3; 23.7 / 11.5.3; 13.3 `UZGODNIJ_REJESTR_RODO` (1)
- **Problem:**
  1. 3.4 część 3: zdublowany fragment „dla `zadan*` `zadan*`:”, a w liście dozwolonych użyć `zdje*` jest pozycja „`zdjecie` w `ObszarNaZdjeciu`/`NiepewnyFragment`/`obszar`”, której nie da się przełożyć na wyjątek tokenowy.
  2. Ścieżki z różnymi nazwami parametrów: 3.2 i 18 — `/uczniowie/{id}/profil`, `DELETE /uczniowie/ja/klasy/{id}`; 12.3 — `{uczenId}`, `{klasaId}`. Test OpenAPI z 3.4 porównuje identyfikatory, więc warto ujednolicić.
  3. 23.7 (C4-06 p. 5) podaje „P(≥ 1 wyzwolenie w 90 dniach) 31–45%”, a akapit „Rozmiar testu” w 11.5.3 — „31–36%”. Rozbieżność wyjaśnia dopiero wniosek (3) symulacji dynamiki (okno pełne od dnia 1). Akapit warto doprecyzować.
  4. `UZGODNIJ_REJESTR_RODO` (1) przegląda obiekty „z ostatnich 24 h”. Przerwa schedulera > 24 h (np. awaria i odtworzenie) zostawia zdarzenia `API` bez transakcji, których żadna praca nie unieważni. Należy dopisać przebieg nadrabiający od ostatniego udanego przebiegu albo alert.
- **Oczekiwana poprawka:** p. 1–3 redakcyjnie; p. 4 — okno od `max(ostatni_udany_przebieg − 5 min, now() − 60 dni)`.
- **Status:** ZAMKNIĘTA (iteracja 6). (1) 3.4 część 3 bez duplikatu, wyjątek `zdje*` zapisany tokenowo (`zdjecie`, `nr_zdjecia`, `{zdjecie_rozwiazania_id}`); (2) ADR-049 p. 5 — jedna reguła parametrów ścieżek, 3.2 i 12.3 ujednolicone (sprawdzone grep: brak `{uczenId}`/`{klasaId}` poza segmentami Next.js); (3) akapit „Rozmiar testu” wyjaśnia 31–36% / do 45%; (4) `UZGODNIJ_REJESTR_RODO` przegląda od `max(ostatni_udany_przebieg − 5 min, now() − 60 dni)` z `ALERT_RODO` przy przerwie > 24 h (13.3, ADR-041).

### Podsumowanie iteracji 5

Weryfikacja uwag otwartych po iteracji 4:

| Stan | Liczba | Identyfikatory |
|---|---|---|
| ZAMKNIĘTA | 6 | C4-01 … C4-06 (w tym 10/10 podpunktów C4-06) |
| OTWARTA | 0 | — |

Nowe uwagi iteracji 5 dotyczące architektury (C5-01 … C5-05) oraz planu (C5-06 … C5-09, sekcja niżej):

| Waga | Liczba uwag | Identyfikatory |
|---|---|---|
| KRYTYCZNA | 0 | — |
| WYSOKA | 0 | — |
| ŚREDNIA | 4 | C5-01, C5-02, C5-06, C5-07 |
| NISKA | 5 | C5-03, C5-04, C5-05, C5-08, C5-09 |
| **Razem** | **9** | |

## Weryfikacja pokrycia TASKS.md

**Metoda.** Uniwersum elementów wyodrębniono niezależnie od macierzy plannera, skryptem w scratchpadzie, z `docs/ARCHITECTURE_DETAILED.md` v2.4 i `docs/requirements.md`. Pokrycie sprawdzono wyłącznie na polach „Realizuje” 143 zadań (bez macierzy na końcu `TASKS.md`), dokładnym dopasowaniem identyfikatora. Sekcje dopasowano do listy „Sekcje:”, a endpointy jako `METODA ścieżka`. Zależności sprawdzono pod kątem nieistniejących identyfikatorów, cykli (DFS) i odwołań w przód, a dla DDL dodatkowo pod kątem kolejności kluczy obcych (zadanie tworzące tabelę wskazywaną musi być przodkiem zadania tworzącego tabelę wskazującą). DoD przejrzano pod kątem kryteriów niesprawdzalnych („zgodnie z sekcją X”) i zgodności z testami zdefiniowanymi w architekturze.

| Kategoria | Elementów | Pokryte | Brak |
|---|---|---|---|
| Sekcje numerowane (`##`–`####`) | 149 | 149 | 0 |
| ADR-001 … ADR-047 | 47 | 47 | 0 |
| DDL — tabele | 59 | 59 | 0 |
| DDL — typy wyliczeniowe | 43 | 43 | 0 |
| DDL — triggery `trg_*` | 6 | 6 | 0 |
| DDL — funkcje (`dodaj_dni_robocze`) | 1 | 1 | 0 |
| DDL — indeksy | 50 | 50 | 0 |
| DDL — rozszerzenia | 2 | 2 | 0 |
| Endpointy API (tabele 12.3; `/dzialy` i `/umiejetnosci` osobno) | 100 | 100 | 0 |
| Agenci (5) i Orkiestrator (6.3 → TASK-020) | 6 | 6 | 0 |
| Prace w tle (13.3 + `ZWOLNIJ_BLOKADY_WERYFIKACJI` z 13.5) | 29 | 29 | 0 |
| Zdarzenia domenowe 13.6 | 16 | 16 | 0 |
| Bramki G-0 … G-7 | 8 | 8 | 0 |
| Scenariusze C-01 … C-40 | 40 | 40 | 0 |
| Błędy E-01 … E-38 | 38 | 38 | 0 |
| Ryzyka R-01 … R-22 | 22 | 22 | 0 |
| Decyzje D-1 … D-17 | 17 | 17 | 0 |
| Pliki testowe / symulacyjne / danych | 12 | 12 | 0 |
| FR-01 … FR-36 | 36 | 36 | 0 |
| NFR-01 … NFR-10 | 10 | 10 | 0 |
| Założenia A, zasady Z, scenariusze S, reguły T | 15 + 6 + 7 + 7 | 35 | 0 |
| Niespójności N, sprawdzenia W, pytania Q, reguły `R-*`, PZS-1 B | 6 + 10 + 13 + 12 + 9 | 50 | 0 |
| **Razem** | **776** | **776** | **0** |

Wynik zgadza się z macierzą plannera (775). Różnica 1 to Orkiestrator, liczony tu osobno i pokryty przez sekcję 6.3 w TASK-020. Fałszywe trafienia skryptu odrzucono ręcznie: `CODZIENNY` to tryb pracy `SPRZATAJ_OSIEROCONE_OBIEKTY`, a nie praca w tle. Ścieżki z prozy 3.2 / 18 o innych nazwach parametrów opisuje C5-05 p. 2. Zależności: 0 nieistniejących identyfikatorów, 0 cykli, 0 odwołań do zadań późniejszych, każde zadanie poza TASK-001 ma zależność. Słownik: w `TASKS.md` i `CLAUDE.md` nie ma zakazanych synonimów poza celowymi przypadkami kontrolnymi i listą zakazów (`TASK-xxx` jest zadeklarowanym identyfikatorem planu); naruszenie w `HANDOFF.md` opisuje C5-09.

**Pokrycie formalne wynosi 100%** (776/776, lista braków pusta). Wpisanie elementu do „Realizuje” nie gwarantuje jednak, że opis i DoD odpowiadają architekturze v2.4. Rozbieżności opisują uwagi niżej.

### C5-06: TASK-078 błędnie opisuje regułę (a) Filtra danych osobowych, a DoD nie obejmuje Poprawek
- **Waga:** ŚREDNIA
- **Lokalizacja:** `TASKS.md` TASK-078 („reguła (a) imię, nazwisko, inicjały i oznaczenie klasy z konta → `[DANE_OSOBOWE]`”), DoD TASK-078 i TASK-080; architektura 10.3.1 p. 3a reguły (a)/(b), tabela Poprawek, G-2 („test Poprawek: przywrócenie kroku `tylko_dane_osobowe` na 50 przypadkach”)
- **Problem:** w architekturze reguła (a) jest regułą ramki. Inicjały i oznaczenie klasy są tylko **wyłączane** przed oceną, czy krok ma treść matematyczną. Tokenowo zastępowane są wyłącznie imię, nazwisko, część lokalna e-maila (b) oraz PESEL i e-mail (c). Opis zadania każe zastępować inicjały i oznaczenie klasy znacznikiem. Wykonawca, który go dosłownie wdroży, zamieni na `[DANE_OSOBOWE]` jednoliterowe tokeny („P.”, „k”) i ciągi typu „3b” w tekście matematycznym. Złamie tym kryterium G-2 (≤ 0,5% fałszywych zastąpień) i inwariant C3-02. DoD nie wymaga też testów tabeli „Działanie filtra na Poprawkach” (kroki niezmienione, zmienione, wstawione, przywrócony `tylko_dane_osobowe`), choć to główna treść poprawki C4-03 p. 3.
- **Oczekiwana poprawka:** opis zgodny z 10.3.1 p. 3a: (a) ramka i wyłączenia przed oceną treści, (b) imię, nazwisko i e-mail z regułami (b1)/(b2), (c) PESEL i e-mail. DoD: testy każdego wiersza tabeli Poprawek oraz test G-2 „50 przywróceń bez ponownego maskowania i ze Sprawdzeniem zgodności”, a po decyzji w C5-01 także przypadek „Pole P”.
- **Status:** ZAMKNIĘTA (iteracja 6). 18.4 jest wiążącym wymaganiem dla nowego `TASKS.md`: reguły `a`, `b1`, `b1-i`, `b2`, `c` dokładnie jak 10.3.1 p. 3a (z wyłączeniami reguły a), DoD z 4 wierszami tabeli Poprawek, testem G-2 (50 przywróceń), zestawem C5-01 i testem przywrócenia, plik testów wskazany; kryterium sprawdzenia przez krytyka jest mechaniczne.

### C5-07: DoD nie wymagają testów dodanych w v2.4 dla bezpiecznika, ogranicznika i odtworzenia konta Nauczyciela
- **Waga:** ŚREDNIA
- **Lokalizacja:** TASK-104 DoD („`test_bezpiecznik.py` (a)–(e)”), TASK-103 DoD; TASK-041 DoD („`test_ogranicznik.py` (a)–(f)”); TASK-119 DoD („Testy C-34 i C-39 (PITR w trakcie `USUN_KONTO` po krokach 1–4)”); architektura 11.5.3 testy (a)–(k), 13.2 testy (a)–(h), G-6 (a3), (a4), (b)–(d)
- **Problem:** testy, które v2.4 dodała jako dowód zamknięcia C4-02 i C4-05, nie występują w żadnym DoD: (f) ostrzeżenie, (g) degradacja 4% z niemalejącym progiem, (h) brak oscylacji, (i) wyjście z blokady, (j) 0,990 → 1,000 i brak wyjścia, (k) dezaktywacja `TRAFNOSC`; w ograniczniku (g) dołączenie 3. workera z przydziałem 0 i (h) dwa jednoczesne odnowienia. DoD TASK-103 („bezpiecznik nie jest cofany”) jest ogólne i nie wskazuje danych testu (g)/(h). DoD TASK-119 zawęża C-39 do kroków Ucznia 1–4. Wariant (a4), czyli konto Nauczyciela (pseudonimizacja `R-PRZYPISANIE`, skutki `KontoUsuwane`, tabela przywrócenia Nauczyciela), i scenariusze (b)–(d) G-6 są tylko w opisie TASK-142, którego DoD to „protokół podpisany”. Zadanie można więc uznać za `DONE` bez dowodu zamknięcia uwag C4.
- **Oczekiwana poprawka:** TASK-104 DoD — (a)–(k) z 11.5.3, w tym dane deterministyczne testu (g). TASK-041 DoD — (a)–(h) z 13.2. TASK-119 DoD — G-6 (a1)–(a4) i (b)–(d) jako testy automatyczne w środowisku testowym, w tym konto Nauczyciela (0 wystąpień imienia i nazwiska w `rekomendacja.uzasadnienie`).
- **Status:** ZAMKNIĘTA (iteracja 6). 18.4 wymaga w DoD testów (a)–(m) bezpiecznika, (a)–(h) ogranicznika (z `pojemnosc = 0` w (g)) i G-6 (a1)–(a4), (b)–(d) jako testów automatycznych w Testcontainers, w tym konta Nauczyciela; protokół G-6 jest osobnym DoD; kryterium „każda litera w DoD dokładnie jednego zadania” jest sprawdzalne.

### C5-08: Zależności niezgodne z kolejnością implementacji
- **Waga:** NISKA
- **Lokalizacja:** TASK-103 (zależności: TASK-022, TASK-029, TASK-102) i TASK-104 (zależność: TASK-103); TASK-018 (zależność: TASK-008); 7.3 `klucz_idempotencji.uzytkownik_id`, `powiadomienie.uzytkownik_id` → `uzytkownik`
- **Problem:**
  1. Kroki 2–3 „Wyniku dnia KALIBRUJ” (TASK-103) wymagają oceny bezpieczników i Blokady obniżania progu, które implementuje TASK-104, a TASK-104 zależy od TASK-103. DoD TASK-103 („bezpiecznik nie jest cofany przez wybór progu”) jest niewykonalne przed TASK-104. Pętla jest logiczna, nie formalna, więc test acykliczności jej nie wykrywa.
  2. TASK-018 tworzy tabele z kluczem obcym do `uzytkownik` (TASK-009), ale nie zależy od TASK-009 ani pośrednio, ani bezpośrednio. Migracja wykonana w kolejności zależności się nie powiedzie.
- **Oczekiwana poprawka:** wydzielić ocenę bezpieczników (populacja, LB/UB, klasyfikacja 1–4) do TASK-104 zależnego od TASK-102, a TASK-103 (kroki 1–6) uzależnić od TASK-104; dodać TASK-009 do zależności TASK-018.
- **Status:** ZAMKNIĘTA (iteracja 6). 18.4: ocena bezpieczników jako osobne zadanie przed „Wynikiem dnia KALIBRUJ”, reguła „FK → przodek” z przykładami `klucz_idempotencji`/`powiadomienie` → `uzytkownik`, kolejność migracji EF Core = kolejność zadań oraz test logiczny DoD → przodek.

### C5-09: Pozostałości redakcyjne w TASKS.md, CLAUDE.md i HANDOFF.md
- **Waga:** NISKA
- **Lokalizacja:** `TASKS.md` TASK-006 DoD („TASK „slownik-baza””), TASK-048 („TASK „rodo-kopie””), TASK-049 DoD („TASK „wer-obciazenie””); TASK-043 DoD („zgodnie z 14.1”); `HANDOFF.md` linia 36 („kolejka prac w tle”), linie 15–18 i Q-2 („v2.4 nie ma osobnego przeglądu krytyka”)
- **Problem:** (1) trzy odwołania do zadań po roboczych nazwach zamiast identyfikatorów (powinny być TASK-024, TASK-115, TASK-099); (2) DoD TASK-043 odwołuje się do sekcji zamiast podać sprawdzalne kryterium (które błędy → `ModelNiedostepny`, które → przejściowe); (3) „kolejka prac w tle” łamie 3.1 p. 5 („prace w tle, nigdy „kolejka””); (4) po tym przeglądzie stwierdzenia HANDOFF o braku przeglądu v2.4 są nieaktualne.
- **Oczekiwana poprawka:** zamienić nazwy robocze na identyfikatory, rozpisać DoD TASK-043 według wierszy E-09 / 14.1, poprawić sformułowanie w HANDOFF i zaktualizować Q-2 oraz linie 15–18 wynikiem iteracji 5.
- **Status:** ZAMKNIĘTA (iteracja 6). 18.4: wyłącznie identyfikatory `TASK-NNN`, DoD bez „zgodnie z sekcją X” (przykład dla E-09), słownictwo „prace w tle”/„dzierżawa”, aktualizacja `HANDOFF.md` po pętli 3.x; test słownika obejmie `TASKS.md`, `HANDOFF.md` i `CLAUDE.md`.

### Werdykt

Wersja 2.4 zamyka wszystkie 6 uwag iteracji 4 (w tym 10/10 podpunktów C4-06). Nie ma otwartych uwag krytycznych ani wysokich. Pokrycie formalne planu wynosi 100% (776/776), zależności są acykliczne, a nazwy w planie są zgodne ze słownikiem. Dokument i plan nie są jednak jeszcze na poziomie produkcyjnym:
- reguła (b1) z inicjałem zmienia treść kroku matematycznego bez Weryfikacji i blokuje FR-18 dla tego kroku, a G-2 jest z nią sprzeczna (C5-01);
- reguły nadzoru w nowym kroku 4 KALIBRUJ mają nieokreśloną populację i wartość `null`, przez co wyjście z trybu pełnej weryfikacji może się zablokować albo oscylować (C5-02);
- opis TASK-078 sprzeczny z 10.3.1 i DoD bez testów v2.4 pozwalają oznaczyć zadania jako `DONE` bez dowodu zamknięcia uwag C4 (C5-06, C5-07).

Wszystkie poprawki są lokalne: 3 akapity architektury, 1 CHECK w DDL i redakcja kilku zadań planu.

## Iteracja 6 (v3.0 — moduły i słownik)

Data przeglądu: 2026-11-01. Wersja dokumentu: 3.0 (iteracja 1 pętli „.NET 10 + Next.js”). Zakres: podział na moduły (Backend .NET, warstwa agentów AI, Frontend Next.js), mapowanie słownika na model domenowy (3.5), szkielety agregatów (3.6), sekcje 1–6, 18.4, 19, 22, 23.4 oraz ADR-048 … ADR-055 i ADR-y ze zmienionym statusem (001, 002, 003, 013, 014, 016, 020, 031, 045). Kod pythonowy w sekcjach 7, 10, 12–15 nie jest w tej rundzie oceniany; ich treść domenowa jest użyta tylko jako odniesienie przy sprawdzaniu sprzeczności z sekcjami nowymi.

**Weryfikacja uwag iteracji 5.** C5-01 … C5-05 sprawdzono w treści (10.3.1 p. 3a, 11.5.3, 7.3, 3.1, 3.4, 13.3, 17.6 G-2), a C5-06 … C5-09 — w 18.4. Wszystkie 9 są ZAMKNIĘTE; statusy są wpisane przy uwagach iteracji 5.

**Sprawdzenie mapowania słownika (skrypt w scratchpadzie, niezależnie od tabel architekta).**

| Sprawdzenie | Wynik |
|---|---|
| Pojęcia `glossary.md` (25) w 3.5 część I | 25 / 25, ta sama pisownia, kolejność zgodna |
| Pojęcia PZS-1 część A (tabela 3.3, 86) w 3.5 część II | 86 / 86, kolejność zgodna; razem 111 wierszy, 0 pustych komórek |
| Enumy C# z 3.6 (30) ↔ typy wyliczeniowe DDL 7.3 (`JsonNamingPolicy.SnakeCaseUpper`) | 28 zgodnych wartość po wartości (w tym `PowodWeryfikacji` 8, `StatusRozwiazania` 10, `StatusZadania` 6); `KodBleduRozpoznawania` ↔ CHECK tekstowy 7.3 — zgodny; `WidocznoscOcenyDlaUcznia` — wyliczany, bez kolumny (zgodnie z 3.5) |
| Typy wyliczeniowe DDL bez wpisu w 3.5/3.6 | `kontekst_decyzji`, `status_importu`, `status_testu` są w 3.5 tylko pośrednio (`StatusImportu`, `StatusTestu`); `kontekst_decyzji` — brak (C6-12) |
| Wartości `UPPER_SNAKE` z 3.5 nieobecne w sekcjach 7–23 | `MASKOWANA` (C6-12) |
| Typy TS z 3.5 bez definicji w 12.4 i spoza listy „nowe nazwy” | `ObszarNaZdjeciu`, `WynikZadaniaSkrot`, `ZdjecieRef` (C6-12) |
| Reguła transliteracji ADR-049 na przypadkach brzegowych (`OtwarteZDowodem`, `WstrzymanieWDoborze`, `EwaluacjaZt1`, `IzotonicznyCv5`, `NEff`, `Sha256`) | odwracalna, zgodna z `SnakeCaseLower`/`SnakeCaseUpper` |

Pokrycie formalne słownika wynosi 100%. Poniższe uwagi dotyczą poprawności i wykonalności, a nie kompletności list.

### C6-01: Kontrakt zlecania prac w tle i `NOTIFY` jest niewykonalny w EF Core w opisanej postaci
- **Waga:** WYSOKA
- **Lokalizacja:** ADR-053 p. 3 („`IZlecaniePrac.Zlec(...)` dodaje encję `PracaWTle` do bieżącego `LematDbContext`; … konflikt indeksu deduplikacji … jest obsługiwany przez `INSERT … ON CONFLICT DO NOTHING` (dla `SPRAWDZ_POPRAWKI_TRANSKRYPCJI`: `DO UPDATE SET ladunek = EXCLUDED.ladunek`)”); 6.3 p. 1 („`NOTIFY` … wysyłane w tej samej transakcji przez `ZlecaniePracInterceptor` (`SaveChangesInterceptor`), więc dociera do workerów dopiero po zatwierdzeniu”); ADR-051 p. 2 (to samo dla `lemat_zdarzenia`)
- **Problem:**
  1. EF Core (także 10) nie generuje `INSERT … ON CONFLICT` dla encji dodanej do `DbContext` — `Add` + `SaveChanges` wysyła zwykły `INSERT`. Konflikt `ux_praca_dedup` / `ux_praca_dedup_oczekujace` jest przewidzianą ścieżką (ponowne `PUT /rozwiazania/{id}/transkrypcja` → `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` z nowym ładunkiem, ponowione żądanie z innym kluczem idempotencji, dwa handlery zlecające tę samą pracę). Zgodnie z opisem kończy się `DbUpdateException` (`23505`). Cały przypadek użycia jest wtedy wycofywany: Uczeń dostaje `500` przy drugiej Poprawce, a handler pracy wpada w ponowienia aż do `MARTWA`. Semantyki `DO UPDATE SET ladunek` nie da się w ogóle wyrazić przez `Add`.
  2. `SaveChangesInterceptor.SavingChangesAsync` działa **przed** otwarciem transakcji niejawnej `SaveChanges`. Polecenie `pg_notify` wykonane tam bez jawnej transakcji idzie w trybie autocommit, więc powiadomienie dociera, zanim wiersz `praca_w_tle` albo nowy stan Rozwiązania jest widoczny. Worker budzi się, nic nie znajduje i czeka 5 s (zabezpieczenie z ADR-053 p. 2a), co zjada budżet NFR-01 (17.2). `KoncentratorZdarzen` czyta stary stan i wysyła go jako `stan`, a kolejne powiadomienie może nie przyjść. Przy wycofaniu `SaveChanges` powiadomienie i tak jest wysłane. `SavedChangesAsync` działa z kolei **po** zatwierdzeniu transakcji niejawnej, czyli też poza nią. Gwarancja „po zatwierdzeniu” istnieje wyłącznie przy jawnej transakcji jednostki pracy, której dokument nie wymaga.
- **Oczekiwana poprawka:** `IZlecaniePrac` jako operacja SQL (`NpgsqlCommand`/`ExecuteSqlAsync` z `INSERT … ON CONFLICT …`) na połączeniu i transakcji bieżącej jednostki pracy, a nie `Add` encji. `IJednostkaPracy` zawsze otwiera jawną transakcję (`BeginTransactionAsync`), a `pg_notify` jest wykonywane w niej przed `CommitAsync` (np. `DbTransactionInterceptor.TransactionCommittingAsync` albo jawne wywołanie w `IJednostkaPracy.ZatwierdzAsync`). Testy w `Lemat.Infrastructure.Tests`: (a) podwójne zlecenie z tym samym kluczem nie wycofuje przypadku użycia, (b) `DO UPDATE` ładunku `SPRAWDZ_POPRAWKI_TRANSKRYPCJI`, (c) `LISTEN` nie dostaje powiadomienia przed `COMMIT` ani po `ROLLBACK`.
- **Status:** ZAMKNIĘTA (iteracja 7). ADR-056: jawna transakcja `IJednostkaPracy`, `ZlecaniePracSql` z `INSERT … ON CONFLICT … RETURNING id, (xmax = 0)`, `NOTIFY` wyłącznie z triggerów `trg_*_powiadom`. Sprawdzone na PostgreSQL 16 z DDL `docs/spec/ddl.md`: oba predykaty `ON CONFLICT` trafiają w indeksy częściowe `ux_praca_dedup` / `ux_praca_dedup_oczekujace`, `DO UPDATE` podmienia ładunek, a `Database.SqlQuery<T>(…).ToListAsync()` zwraca `(id, wstawiona)`. Nowe problemy tego mechanizmu opisują C7-03 (konflikt indeksu przy ponowieniu) i C7-06 p. 6 (kompozycja `SqlQuery`).

### C6-02: Rotacja refresh tokenu z wykrywaniem ponownego użycia + access token tylko w pamięci karty = wylogowanie przy kilku kartach i wznowieniu PWA
- **Waga:** WYSOKA
- **Lokalizacja:** 16.1 („refresh token … rotowany przy każdym użyciu; ponowne użycie unieważnionego tokenu unieważnia całą `rodzina_id`”); ADR-054 p. 2 (bez BFF, access token w pamięci `DostawcaSesji`, odświeżanie `POST /api/v1/auth/odswiezenie`); 6.8 `lib/auth/`; ADR-051 p. 3 (strumień zamykany przy `exp`, klient łączy się ponownie „z odświeżonym tokenem”)
- **Problem:** bez BFF każda karta, każde przeładowanie, każde wznowienie PWA z tła i każde ponowne połączenie SSE po `exp` wywołuje odświeżenie, bo token żyje tylko w pamięci karty. Cookie `lemat_rt` jest wspólne dla kart. Dwie karty (np. Nauczyciel z kolejką weryfikacji i Rozwiązaniem w drugiej karcie; Uczeń z Zadaniem i Profilem) wysyłają odświeżenie z tym samym tokenem w odstępie milisekund. Pierwsze rotuje token, drugie jest „ponownym użyciem unieważnionego tokenu”, więc serwer unieważnia rodzinę i obie karty są wylogowane. Ten sam wyścig daje React 19 Strict Mode w dev (podwójny efekt) i ponowienia TanStack Query. Dla Nauczyciela utrata sesji w trakcie Weryfikacji oznacza utratę Blokady weryfikacji po 15 min (szkic jest autozapisywany, ale doświadczenie jest nie do przyjęcia), a dla Ucznia na telefonie — logowanie po każdym powrocie do aplikacji z dwiema kartami. Dokument nie opisuje tego scenariusza ani w 6.8, ani w 15.2.
- **Oczekiwana poprawka:** (1) w kliencie odświeżanie typu single-flight między kartami: `navigator.locks.request("lemat-odswiezenie", …)` (Web Locks API) i `BroadcastChannel` rozgłaszający nowy access token kartom tego samego origin; (2) po stronie serwera okno tolerancji (np. 30 s): ponowne użycie tokenu rotowanego w tym oknie zwraca tego samego następcę zamiast unieważniać rodzinę (zapis `zastapiony_przez`, `zastapiony_at` w `token_odswiezania`); unieważnienie rodziny dopiero poza oknem albo przy innym User-Agent / IP (do decyzji); (3) test Playwright „2 karty, równoczesne odświeżenie, obie zalogowane” i test integracyjny okna tolerancji; (4) scenariusz w 15.2.
- **Status:** ZAMKNIĘTA (iteracja 7). ADR-057: single-flight z Web Locks i `BroadcastChannel`, okno tolerancji 30 s z `skrot_klienta_http` i limitem 5 użyć, kolumny `token_odswiezania` są w DDL (sprawdzone w katalogu), testy Playwright i integracyjne, scenariusz C-41.

### C6-03: Granica „agent ↔ port modelu generatywnego” jest sprzeczna: Semantic Kernel w `Lemat.Agenci` potrzebuje `IChatClient`, którego port nie udostępnia
- **Waga:** WYSOKA
- **Lokalizacja:** ADR-055 p. 1 (`IModelGeneratywny.GenerujJsonAsync<T>(Prompt …)`, `GenerujTekstAsync(Prompt …)`), p. 2 (`IChatClient` budowany wyłącznie w `Lemat.Modele` przez `RejestrAdapterow`), p. 4b (`Kernel` dostaje `IChatCompletionService` uzyskany z `IChatClient`); 6.4 (`Lemat.Agenci` → `Lemat.Application`, `Lemat.Domain`; pakiety `Microsoft.Extensions.AI.Abstractions`, `Microsoft.SemanticKernel.Core`; `Lemat.Application` bez `Microsoft.Extensions.AI.Abstractions`); 6.5 (`Lemat.Agenci` nie może zależeć od `Lemat.Modele`); 3.5 #24 (`A.Modele.IModelGeneratywny` „nad `IChatClient`”); ADR-003 (konfiguracja modeli w bazie per rola)
- **Problem:** Agent generujący ma wywoływać `SprawdzanieSymbolicznePlugin` przez `FunctionChoiceBehavior.Auto`. To wymaga `Kernel` z usługą czatu, czyli `IChatClient` skonfigurowanego dla roli `GENERATYWNY_ZADANIA`. Port `IModelGeneratywny` przyjmuje wyłącznie wyrenderowany `Prompt`, więc agent nie może przez niego wykonać pętli wywołań funkcji. `Lemat.Agenci` nie może sięgnąć do `RejestrAdapterow` (`Lemat.Modele`), a `Lemat.Application` nie ma `Microsoft.Extensions.AI.Abstractions`, więc nie może zdefiniować portu zwracającego `IChatClient`. Implementacja zostaje z dwiema złymi drogami:
  1. wstrzyknąć „jakiś” `IChatClient` z DI do agenta — z pominięciem `konfiguracja_modelu` (wybór dostawcy i modelu dla roli, `wersja_promptu`, `parametry.timeout_s`, `retencja_dostawcy_dni` z A-07, zakaz `mock` w `Production`, metryki per rola), czyli ze złamaniem ADR-003 i NFR-06;
  2. przenieść pętlę wywołań funkcji do adaptera w `Lemat.Modele`, który wtedy musi znać plugin agenta, co odwraca kierunek zależności.
  
  Nie wiadomo też, gdzie jest zapisywany ślad wywołań funkcji (NFR-05), skoro `decyzja_modelu` dotyczy Modelu decyzyjnego.
- **Oczekiwana poprawka:** jawny port w `Lemat.Application.Modele`, np. `IFabrykaKlientowCzatu.PobierzAsync(RolaModelu rola, CancellationToken ct) → IKlientCzatuRoli` (opakowanie z `IChatClient`, konfiguracją roli i limitami), z jednoznaczną decyzją, czy `Microsoft.Extensions.AI.Abstractions` jest dozwolony w `Lemat.Application` (6.4, 6.5, BannedSymbols). Alternatywnie: port `IModelGeneratywny.GenerujZNarzedziamiAsync<T>(Prompt, IReadOnlyList<AIFunction> narzedzia, int maksWywolan, …)` z implementacją pętli w `Lemat.Modele`, a funkcje dostarcza agent jako `AIFunction`. Dopisać, gdzie są logowane wywołania narzędzi (np. `zlecenie_generowania` / `walidacja_zadania.sprawdzenia`) i test architektury „agent nie rozwiązuje `IChatClient` z DI”.
- **Status:** ZAMKNIĘTA (iteracja 7). ADR-058: `IModelGeneratywny.DlaRoliAsync → KlientCzatuRoli` z konfiguracją roli i potokiem budowanym w `Lemat.Modele`, `Microsoft.Extensions.AI.Abstractions` w `Lemat.Application`, ślad narzędzi w `zlecenie_generowania.wywolania_narzedzi`, NetArchTest `AgentNieRozwiazujeKlientaCzatuZDi`. Wady samego potoku opisują C7-01 i C7-02.

### C6-04: Szkielet `Rozwiazanie` gubi pracę po ponowieniu rozpoznania, rzuca wyjątek przy pierwszej Ocenie i nie ma połowy przejść maszyny stanów 8.1
- **Waga:** WYSOKA
- **Lokalizacja:** 3.6 `Lemat.Domain.Rozwiazania.Rozwiazanie` (`PonowRozpoznanie`, `ZarejestrujPierwszaOcene`, `Zatwierdz`, `Przeslij`); 8.1; A-11; 3.5 #81
- **Problem:**
  1. `PonowRozpoznanie` ustawia `Status = Przeslane`, ale w przeciwieństwie do `Przeslij` i `PrzeslijPonownie` nie zgłasza `RozwiazaniePrzeslane`. Komentarze mówią, że to handler tego zdarzenia zleca `ROZPOZNAJ_PISMO`, więc po „Ponów” (E-xx, C1-28) nikt nie zleca rozpoznania i Rozwiązanie zostaje w `PRZESLANE` na zawsze (żadna praca w tle 13.3 nie zbiera osieroconych `PRZESLANE`).
  2. `PonowRozpoznanie` nadpisuje `PrzeslanoAt`, więc `CzasSystemowy` liczy rozpoznanie od ponowienia. A-11 każe liczyć błąd rozpoznawania z winy dostawcy jako **naruszenie** NFR-01; szkielet ukrywa ten czas, więc metryka `czas_systemowy_ms` i zobowiązanie 99% są zawyżane.
  3. `ZarejestrujPierwszaOcene` wymaga stanu `Ocenianie` i używa `TranskrypcjaGotowaAt!.Value`, ale żadna metoda nie ustawia ani `Ocenianie`, ani `TranskrypcjaGotowaAt` (brak przejść `Przeslane → Rozpoznawanie → TranskrypcjaGotowa | BladRozpoznawania`, `Zatwierdzone → Ocenianie`). Brakuje też T-6 (`OczekujeNaWeryfikacje` bez Oceny po terminie twardym), `Porzuc`, `Anuluj` (usunięcie konta) i przejścia `OczekujeNaWeryfikacje → Ocenione` po Rozstrzygnięciu. Wykonawca, który przepisze szkielet, dostanie `InvalidOperationException` przy pierwszej Ocenie.
  4. `Zatwierdz(TranskrypcjaId)` nie sprawdza, że Transkrypcja należy do tego Rozwiązania i jest jego **najnowszą** wersją. Wyścig: Uczeń zapisuje Poprawki w karcie A (`PUT …/transkrypcja` tworzy wersję 2), a w karcie B zatwierdza widoczną wersję 1. Oceniona zostaje Transkrypcja inna niż ta, którą Uczeń poprawił (FR-18), a Sprawdzenie zgodności poprawek dotyczy wersji, która nie jest oceniana. W 3.6 brakuje też `ETag`/`version` jako warunku zatwierdzenia.
  5. Niezmiennik „para (`ElementSciezkiId`, `PrzypisanieId`) tylko dla `R-PRZYPISANIE`” z komentarza nie jest sprawdzany, a `rodzaj` jest parametrem od wywołującego, a nie cechą `ZadanieWersja`.
- **Oczekiwana poprawka:** komplet przejść 8.1 jako metod z niezmiennikami (każda metoda, która ustawia `Przeslane`, zgłasza zdarzenie zlecające rozpoznanie); `PrzeslanoAt` ustawiane wyłącznie przy przesłaniu, a ponowienie z `OstatnieRozpoznanieOdAt` niezależnym od pomiaru A-11; `Zatwierdz(TranskrypcjaId id, int wersjaTranskrypcji, int oczekiwanaWersjaRozwiazania)` z `409 TRANSKRYPCJA_NIEAKTUALNA`; test domeny dla każdego wiersza 8.1 i test wyścigu „Poprawka vs zatwierdzenie” (15.2).
- **Status:** ZAMKNIĘTA (iteracja 7). 3.6 ma komplet przejść 8.1 (`RozpocznijRozpoznawanie` … `Anuluj`). `PonowRozpoznanie` zgłasza `RozwiazaniePrzeslane` i nie zmienia `PrzeslanoAt`. `Zatwierdz` sprawdza `Version` i najnowszą wersję Transkrypcji (409/412). Kolumny `najnowsza_transkrypcja_id`, `najnowsza_wersja_transkrypcji`, `ostatnie_rozpoznanie_od_at` z CHECK-ami są w DDL (sprawdzone).

### C6-05: Szkielet `Weryfikacja` nie ma fabryki, `KlasaId`, `Anuluj`, `Zwolnij`, `DolaczOcene` i omija limit 2 h
- **Waga:** ŚREDNIA
- **Lokalizacja:** 3.6 `Lemat.Domain.Weryfikacje.Weryfikacja`; 11.4.1–11.4.2; ADR-033 (uzupełnienie it. 4: „ponowne podjęcie przez właściciela nie wydłuża blokady poza `podjeta_at + 2 h`”); 9.13 (`PONOW_OCENE`)
- **Problem:**
  1. Brak fabryki: `Priorytet` ma wartość domyślną 0, więc `DopiszZgloszenie` (`Math.Min(Priorytet, 2)`) zostawia 0 wbrew zakresowi 1–3; `PowodyPoczatkowe` nie ma miejsca ustalenia.
  2. Brak `KlasaId`, choć 11.4.2 daje prawo podjęcia „Nauczycielowi prowadzącemu Klasę z `klasa_id`”. `Podejmij` nie sprawdza żadnego uprawnienia (docelowy / Klasa / `weryfikator_globalny`).
  3. `Podejmij` przy wygasłej, ale jeszcze niezwolnionej blokadzie **własnej** (`W_TRAKCIE`, `ZablokowanaDo < teraz`, przed przebiegiem `ZWOLNIJ_BLOKADY_WERYFIKACJI`) idzie gałęzią `Nowa` i zeruje `PodjetaAt`, czyli przedłuża blokadę poza 2 h wbrew ADR-033.
  4. Brak `Anuluj(PowodAnulowania)` (enum jest zadeklarowany), `Zwolnij` (endpoint `…/zwolnienie`, 3.5 #75) i `DolaczOcene` (`OcenaWeryfikowanaId` po `PONOW_OCENE`); `DopiszZgloszenie` nie sprawdza stanu, więc dopisuje Zgłoszenie do Weryfikacji `Zakonczona`/`Anulowana`.
  5. `Eskaluj` w stanie `W_TRAKCIE` zeruje `NauczycielDocelowyId`, ale zostawia Blokadę weryfikacji dotychczasowego Nauczyciela, więc nie wiadomo, czy może on rozstrzygnąć Weryfikację już w kolejce `GLOBALNA` (np. bez uprawnienia `weryfikator_globalny`).
- **Oczekiwana poprawka:** fabryka `Weryfikacja.Utworz(...)` z `PowodyPoczatkowe`, `Priorytet` i przydziałem 11.4.1; `KlasaId`; `Podejmij(nauczyciel, UprawnienieDoPodjecia, teraz)` z zachowaniem `PodjetaAt` przy ponownym podjęciu przez właściciela; metody `Anuluj`, `Zwolnij`, `DolaczOcene` ze strażnikami stanu; jawna reguła dla `Eskaluj` w stanie `W_TRAKCIE`.
- **Status:** ZAMKNIĘTA (iteracja 7). Są: fabryka `Weryfikacja.Utworz`, `KlasaId`, `Podejmij` z `UprawnienieDoPodjecia` i limitem 2 h liczonym od `PodjetaAt` także po wygaśnięciu, `Zwolnij`/`ZwolnijWygasla`/`DolaczOcene`/`Anuluj`/`CofnijEskalacjeKarencji`, strażnik w `DopiszZgloszenie` i reguła `Eskaluj` w `W_TRAKCIE`. Realizację SQL podjęcia opisuje C7-06 p. 5.

### C6-06: Szkielety kont: przywrócenie konta omija zatwierdzenie Nauczyciela, a limit 180 min dotyczy wszystkich weryfikatorów
- **Waga:** ŚREDNIA
- **Lokalizacja:** 3.6 `Uzytkownik.Przywroc`, `Uzytkownik.ZlozWniosekOUsuniecie`, `Nauczyciel.NadajWeryfikatoraGlobalnego`; A-14; 16.1 (konto Nauczyciela `OCZEKUJE_NA_ZATWIERDZENIE`, „bo Nauczyciel widzi dane Uczniów”); 3.3 „Weryfikator globalny” (umowa poufności)
- **Problem:**
  1. `ZlozWniosekOUsuniecie` nadpisuje dowolny status (`OczekujeNaZatwierdzenie`, `Zablokowane`) wartością `Usuwane`, a `Przywroc` ustawia zawsze `Aktywne`. Nauczyciel niezatwierdzony przez Administratora może złożyć wniosek o usunięcie i od razu go anulować, a wtedy jest `AKTYWNE` bez zatwierdzenia i widzi dane Uczniów. To samo dotyczy konta zablokowanego przez Administratora (odblokowanie przez użytkownika).
  2. `NadajWeryfikatoraGlobalnego` stosuje warunek `prognoza + deklarowane ≤ 180` bezwarunkowo, a A-14 ogranicza go do Nauczyciela **prowadzącego Klasę**. Nauczyciel bez Klas albo współpracujący z umową, który deklaruje 240 min (CHECK dopuszcza do 480), dostaje `LIMIT_OBCIAZENIA_WERYFIKATORA`. To podważa pulę weryfikatorów z 17.7 i bramkę G-7.
  3. Brak warunku umowy poufności (definicja Weryfikatora globalnego w 3.3), statusu `AKTYWNE` i metody odebrania flagi (3.1 p. 5: „flagi są odbierane w `USUN_KONTO`”).
- **Oczekiwana poprawka:** `OkresKarencji` przechowuje `StatusPrzedWnioskiem`, a `Przywroc` go odtwarza (kolumna w 7.3); `NadajWeryfikatoraGlobalnego(int deklarowane, bool prowadziKlase, int prognozaKolejkiKlasy, bool umowaPoufnosci)` z limitem 180 wyłącznie przy `prowadziKlase`; metody `OdbierzWeryfikatoraGlobalnego` / `OdbierzRecenzentaTresci`; testy domeny dla wszystkich par (status przed wnioskiem, przywrócenie).
- **Status:** ZAMKNIĘTA (iteracja 7). `OkresKarencji.StatusPrzedWnioskiem` z kolumną i CHECK-ami `status_przed_wnioskiem` (sprawdzone w katalogu), `Zatwierdz`/`Zablokuj`/`Odblokuj`, limit 180 min tylko dla prowadzącego Klasę, `umowa_poufnosci_at` z CHECK, metody odbierania flag.

### C6-07: Cykl zależności w czasie wykonania `Rekomendacje ↔ Orkiestracja`
- **Waga:** ŚREDNIA
- **Lokalizacja:** 6.2 reguła 3 („Orkiestracja może korzystać z fasad wszystkich modułów; żaden moduł nie korzysta z Orkiestracji poza `IZlecaniePrac` … i `IOrkiestrator.RekomendujSynchronicznieAsync` (wyłącznie Rekomendacje)”); 6.3 tryb synchroniczny; 6.1 (`REKM --> ORK`); 6.5 (NetArchTest bez reguły acykliczności)
- **Problem:** `Orkiestrator` jest jedną klasą, która korzysta z fasad wszystkich modułów, w tym `IRekomendacjeFasada` (zapis elementu Ścieżki nauki i Rekomendacji po decyzji agenta), a fasada Rekomendacji zależy od `IOrkiestrator`. Przy wstrzykiwaniu przez konstruktor `Microsoft.Extensions.DependencyInjection` zgłasza „circular dependency” przy pierwszym rozwiązaniu usługi. Przy obejściu (`Lazy<T>`, lokator usług) przypadek użycia „następny element” wchodzi ponownie w ten sam moduł w tej samej transakcji, a reguła 6.2 p. 4 (transakcja = zmiany jednego przypadku użycia) staje się niejasna: kto zapisuje element i zlecenie uzupełnienia puli, Rekomendacje czy Orkiestrator? Reguły 6.5 sprawdzają kierunek „przez fasadę”, ale nie acykliczność między modułami.
- **Oczekiwana poprawka:** wydzielić `RekomendacjaSynchroniczna` (osobna klasa w `Lemat.Application.Orkiestracja`, zależna wyłącznie od `IAgentRekomendujacy` i fasad odczytu Profil/Tresci), która zwraca decyzję, a zapis wykonuje przypadek użycia Rekomendacji; albo przenieść tryb synchroniczny do `Lemat.Application.Rekomendacje` bez udziału `IOrkiestrator`. Dodać test architektury „brak cykli między przestrzeniami nazw modułów `Lemat.Application.*`” (poza jawnie wymienionym `IZlecaniePrac`) i test rozwiązania kontenera DI w `Lemat.Api.Tests` (`ValidateOnBuild = true`, `ValidateScopes = true`).
- **Status:** ZAMKNIĘTA (iteracja 7). `IRekomendacjaSynchroniczna` w `Orkiestracja.Synchroniczna` zależy tylko od agenta i fasad odczytu. Zapis wykonuje `NastepnyElementHandler` Rekomendacji. `IZlecaniePrac` jest w `Wspolne.PraceWTle`. Są testy `BrakCykliMiedzyModulamiAplikacji` i `KontenerDiTests`.

### C6-08: `StanBezpiecznika` bez tokenu współbieżności — `KALIBRUJ` nadpisuje równoległą dezaktywację
- **Waga:** ŚREDNIA
- **Lokalizacja:** 3.6 `StanBezpiecznika` (nie dziedziczy `Agregat<>`, brak `Version`), 3.5 #104 (rodzaj „AG”); 7.3 `stan_bezpiecznika` (brak kolumny `version`); 13.3 `KALIBRUJ` („ocena bezpieczników na stanie sprzed przebiegu … `UPSERT stan_bezpiecznika`”, czas do 10 min); `POST /admin/bezpieczniki/dezaktywacja`; 15.2 (brak scenariusza)
- **Problem:** `KALIBRUJ` czyta stan na początku przebiegu, liczy kroki 1–6 przez kilka minut i wykonuje `UPSERT`. Administrator, który w tym czasie dezaktywuje bezpiecznik (`aktywny = false`, `okno_od = teraz`, uzasadnienie), traci tę decyzję po cichu: `UPSERT` przywraca `aktywny = true` i stare `okno_od`, a próg przy wyzwoleniu może zostać ponownie podniesiony. Odwrotny wyścig — dezaktywacja po ocenie, a przed zapisem — daje stan, którego żaden z aktorów nie zamierzał. Szkielet nie ustawia też `ostatnie_wyzwolenie_at` w `Wyzwol`, nie modeluje `wyjscie_z_blokady_at`, dopuszcza `Dezaktywuj` na nieaktywnym bezpieczniku i nie sprawdza `RodzajZadania == OtwarteObliczeniowe` (CHECK 7.3).
- **Oczekiwana poprawka:** `StanBezpiecznika : Agregat<…>` z kolumną `version` (token współbieżności) albo `SELECT … FOR UPDATE` w obu ścieżkach i ponowna ocena w `KALIBRUJ` po konflikcie; dezaktywacja w trakcie przebiegu wygrywa (przebieg z `rezultat_przestarzaly`); metody `Wyzwol(teraz)` z `OstatnieWyzwolenieAt`, `WyjdzZBlokady(teraz)`; strażnik stanu w `Dezaktywuj`; scenariusz w 15.2 i test integracyjny.
- **Status:** ZAMKNIĘTA (iteracja 7). `StanBezpiecznika : Agregat<KluczBezpiecznika>` z `version` w DDL, `Wyzwol`/`WyjdzZBlokady`, strażnik `Dezaktywuj`, `DezaktywowanyPo`, `KALIBRUJ` z tokenem wersji i ponowną oceną, scenariusz C-44. Mapowanie klucza złożonego jest jednak niewykonalne w opisanej postaci (C7-06 p. 3).

### C6-09: Limity strumieni SSE są liczone per replika i kolidują ze strumieniem powiadomień w każdym układzie
- **Waga:** ŚREDNIA
- **Lokalizacja:** ADR-051 p. 1 (strumień `powiadomienia/strumien`, `id:` = `created_at` powiadomienia), p. 4 („≤ 3 strumienie na użytkownika i ≤ 1 na (użytkownik, Rozwiązanie) — nadmiarowe połączenie zamyka najstarsze”); 6.9 (`powiadomienia` — „układy wszystkich ról”); ADR-054 p. 6 (Serwist, `NetworkOnly` dla `/api/*`)
- **Problem:**
  1. Strumień powiadomień jest otwierany w układzie każdej roli, czyli w każdej karcie. Uczeń z dwiema kartami Rozwiązań ma 4 strumienie (2 powiadomień + 2 Rozwiązań) przy limicie 3. Serwer zamyka najstarszy, klient łączy się ponownie (ADR-051 p. 5), co zamyka kolejny najstarszy — pętla ponownych połączeń, a po dwóch nieudanych próbach w 10 s klient przechodzi na odpytywanie, czyli traci zysk z SSE.
  2. Licznik jest w `KoncentratorZdarzen` jednej repliki `api`. Przy 2 replikach za Caddy limit faktyczny wynosi 6, a „zamknięcie najstarszego” nie działa między replikami.
  3. `id:` powiadomienia = `created_at` nie jest unikalne (kilka powiadomień w tej samej mikrosekundzie z jednej transakcji), więc `Last-Event-ID` nie identyfikuje miejsca wznowienia.
  4. Zapis Wskazówki zmienia `ocena`, a nie `rozwiazanie.version`; nie jest określone, jak interceptor mapuje zmianę encji `Ocena` (Wskazówka, Ocena po Rozstrzygnięciu) na `NOTIFY {"typ":"ROZWIAZANIE","id":…}`.
  5. Service worker Serwist przechwytujący `fetch` strumienia (`NetworkOnly`) może buforować albo przerywać długie odpowiedzi w części przeglądarek; bezpieczniej wyłączyć `/api/*` z obsługi `fetch` w `sw.ts`.
- **Oczekiwana poprawka:** jeden strumień powiadomień na przeglądarkę (wybór karty-lidera przez Web Locks / `BroadcastChannel`, jak w C6-02) albo powiadomienia multipleksowane w jednym strumieniu na kartę; limit liczony globalnie (wiersz w tabeli `UNLOGGED` albo licznik per użytkownik w PostgreSQL) albo limit tylko per (użytkownik, zasób); `id:` powiadomienia = `id` (UUIDv7) powiadomienia; jawna lista encji, których zmiana wysyła `NOTIFY ROZWIAZANIE`; `/api/*` poza `fetch` handlerem service workera; test Playwright „2 karty, brak pętli ponownych połączeń”.
- **Status:** ZAMKNIĘTA (iteracja 7). Jest jeden strumień powiadomień na przeglądarkę (karta-lider), `429 ZA_DUZO_STRUMIENI` bez zamykania istniejących strumieni, zamknięta lista triggerów `NOTIFY` (8 triggerów obecnych w katalogu po załadowaniu DDL), `/api/*` poza service workerem i scenariusz C-42. Wznawianie po `Last-Event-ID` z UUIDv7 ma nową lukę (C7-11).

### C6-10: Wykonalność Semantic Kernel: brakujące pakiety, limit wywołań funkcji i kodowanie LaTeX w szablonach
- **Waga:** ŚREDNIA
- **Lokalizacja:** 6.4 (`Lemat.Agenci`: wyłącznie `Microsoft.SemanticKernel.Core`, `Microsoft.Extensions.AI.Abstractions`); 6.6 i ADR-055 p. 4 (szablony YAML `PromptTemplateConfig` w składni Handlebars, `FunctionChoiceBehavior.Auto` „≤ 8 wywołaniami”, szablon stanu Agenta oceniającego jako szablon SK); 10.4.2 (stan zawiera Transkrypcję z LaTeX)
- **Problem:**
  1. Składnia Handlebars wymaga pakietu `Microsoft.SemanticKernel.PromptTemplates.Handlebars`, a ładowanie YAML — `Microsoft.SemanticKernel.Yaml`; żadnego nie ma na liście 6.4 (Central Package Management nie przepuści budowy).
  2. `FunctionChoiceBehavior.Auto` nie ma parametru „maksymalnie N wywołań”; limit wymaga `IAutoFunctionInvocationFilter` przerywającego pętlę (`context.Terminate = true`) albo `FunctionInvokingChatClient.MaximumIterationsPerRequest` w Microsoft.Extensions.AI. Mechanizm nie jest wskazany, więc limit ≤ 8 (koszt i czas `GENERUJ_ZADANIE`) nie ma realizacji.
  3. Silniki szablonów SK domyślnie kodują wartości zmiennych wejściowych (ochrona przed wstrzyknięciem znaczników `<message>`), chyba że ustawiono `AllowDangerouslySetContent`. Transkrypcja i treść Zadania zawierają `<`, `>`, `&` (nierówności, `\begin{cases} … & …`). Bez jawnej polityki i testu model dostaje `&lt;` zamiast `<`, co zmienia treść kroku przed oceną (FR-19, FR-21), a `decyzja_modelu.stan` (NFR-05) nie odpowiada temu, co widział Nauczyciel.
  4. Szablon stanu Modelu decyzyjnego (10.4.2) renderowany przez SK „bez wywołania LLM” wiąże deterministyczny kontrakt Jev (A-01, limit 32 000 znaków) z silnikiem szablonów i jego kodowaniem; prostsze i testowalne jest zwykłe budowanie tekstu w C#.
- **Oczekiwana poprawka:** dopisać pakiety do 6.4/5.2; wskazać mechanizm limitu wywołań i test „9. wywołanie przerywa generowanie z `BladModeluTrwalyException`”; ustalić politykę kodowania (zmienne treści matematycznej z `AllowDangerouslySetContent = true` w szablonach bez znaczników ról, albo wyłącznie format tekstowy bez parsowania ról) i test zgodności bajtowej „LaTeX z `<`, `&` w stanie i w prompcie”; stan Jev budować bez SK.
- **Status:** ZAMKNIĘTA (iteracja 7) w zakresie p. 1, 3 i 4: są pakiety `Microsoft.SemanticKernel.Yaml` i `JsonSchema.Net` (bez Handlebars), treść użytkownika trafia do wiadomości `user` z `UnsafeRelaxedJsonEscaping`, a stan Jev buduje C#. Punkt 2 (limit wywołań funkcji) ma wskazany mechanizm, ale ten mechanizm nie działa tak, jak opisano (zweryfikowano kompilacją i uruchomieniem). Problem jest kontynuowany jako C7-01.

### C6-11: Sidecar Magick.NET: system plików tylko do odczytu, polityka ImageMagick i `FailFast` jako wektor DoS
- **Waga:** ŚREDNIA
- **Lokalizacja:** ADR-052 p. 2–3; 5.3 (`read_only: true`, `pids_limit: 64`, `restart: always`); 5.4 (oczekiwanie na slot > 5 s → `503`)
- **Problem:**
  1. ImageMagick przy przekroczeniu `ResourceLimits.Memory` przenosi bufor pikseli na dysk (`ResourceLimits.Disk` nieustawiony), a dekodery delegatów korzystają z katalogu tymczasowego. Przy `read_only: true` bez `tmpfs` dekodowanie dużych HEIC kończy się błędem, który klient zinterpretuje jako `422 ZDJECIE_NIEPRAWIDLOWE` dla poprawnego zdjęcia. Trzeba też potwierdzić, że wariant `Magick.NET-Q8-AnyCPU` ładuje bibliotekę natywną z `runtimes/<rid>/native`, a nie z katalogu pamięci podręcznej w `/tmp`.
  2. Magick.NET nie wczytuje systemowego `policy.xml`; polityka musi być ustawiona w kodzie (`MagickNET.SetSecurityPolicy` / `MagickNET.Initialize` z `ConfigurationFiles`). Bez tego wyłączenie koderów z p. 3 nie działa, a jedyną obroną pozostaje sprawdzenie formatu przez `MagickImageInfo`.
  3. `Environment.FailFast` po 8 s jednego żądania zabija wszystkie równoległe operacje sidecara (2 w `obrazy-api`), czyli przesłania innych Uczniów, a przy powtarzaniu restartów Docker wydłuża opóźnienie kolejnych startów. Jeden spreparowany plik (np. HEIC z kosztownym dekodowaniem w granicach 40 Mpx) wysyłany co kilka sekund wyłącza przesyłanie zdjęć na replice `api` (`503`) — limit szybkości 12.7 nie jest liczony po kosztach dekodowania.
- **Oczekiwana poprawka:** `tmpfs: /tmp` (np. 512 MiB) i `ResourceLimits.Disk` jawnie; polityka bezpieczeństwa ustawiana w `Program.cs` sidecara z testem „koder spoza listy jest odrzucany”; izolacja operacji per proces potomny albo `ResourceLimits.Time` + `ResourceLimits.Thread = 1` zamiast `FailFast` całego sidecara; metryka i limit przesłań na Ucznia po przekroczeniach czasu normalizacji (np. 3 przekroczenia / 10 min → `429`).
- **Status:** ZAMKNIĘTA (iteracja 7). Nadzorca i procesy robocze zamiast `FailFast`, `tmpfs`, `ResourceLimits.Disk`/`Thread`/`Time`, polityka w kodzie. Zweryfikowano na Magick.NET-Q8-AnyCPU 15.0.0: `MagickNET.Initialize(IConfigurationFiles, string)` istnieje, polityka z `ConfigurationFiles.Default.Policy.Data` odrzuca SVG i TXT (`MagickPolicyErrorException`), a biblioteka natywna ładuje się z `runtimes/linux-x64/native` bez zapisu do `/tmp`.

### C6-12: Niespójności mapowania 3.2 / 3.3 / 3.5 z resztą dokumentu
- **Waga:** NISKA
- **Lokalizacja:** 3.2, 3.3, 3.5, 6.2, 6.4, 12.3, 16.1
- **Problem:**
  1. `WersjaZdjecia { Oryginalna, Maskowana }` i `?wersja_zdjecia=MASKOWANA` (3.5 #71) są sprzeczne z 12.3 (`?wersja_zdjecia=ORYGINAL|MASKOWANE`). `MASKOWANA` nie występuje nigdzie poza 3.5, a 3.5 jest według ADR-049 p. 10 źródłem prawdy.
  2. 3.2: Kalibracja to „moduł Kalibracja (`Lemat.Domain.Kalibracja`); tabele … `kalibracja_trudnosci`”, podczas gdy 3.5 #23 i 6.2 umieszczają `KalibracjaTrudnosci` i tabelę w `Tresci`. 3.2 podaje dla Nauczyciela „— (zasoby: `/klasy`, …)”, a 3.5 #2 — zasób `/nauczyciele`. Kolumna „Klasa domenowa” 3.2 ma `Zadanie.czy_bazowe` w `snake_case`.
  3. 3.3 („Model decyzyjny”: port `ModelDecyzyjny`; „Model zapasowy”: „implementujący port `ModelDecyzyjny`”) nie ma nazwy `IModelDecyzyjny` z 3.5 #54.
  4. 3.5 wymienia jako typy TS `WynikZadaniaSkrot` i `ZdjecieRef`, które są typami wewnętrznymi kontraktów agentów (10.1.1, 10.3.1), oraz `ObszarNaZdjeciu`, który nie ma definicji w 12.4 ani na liście „nowych nazw”.
  5. Typ wyliczeniowy `kontekst_decyzji` (7.3) nie ma odpowiednika C# w 3.5 (powinien być przy #30 `DecyzjaModelu`).
  6. 16.1 nadal podaje „JWT (EdDSA Ed25519)”, a 5.2 i ADR-048 — ES256; sekcja 16 nie jest na liście sekcji do przepisania.
  7. `Lemat.Symulacje` (6.4) ma referencje tylko do `Lemat.Domain` i `Lemat.Application`, choć symuluje `OgranicznikSzybkosci` z `Lemat.Infrastructure` (6.7: `OgranicznikSymulacja.cs`); `PredyktorSukcesuGbm` w `Lemat.Modele` (ML.NET LightGBM) nie ma pakietów `Microsoft.ML*` w 6.4.
  8. Przestrzeń nazw `Lemat.Domain.Modele` / `Lemat.Application.Modele` przesłania projekt `Lemat.Modele` w kodzie wewnątrz `Lemat.Domain.*` / `Lemat.Application.*` (odwołanie `Modele.X` rozwiązuje się do przestrzeni warstwy). ADR-049 p. 6 rozstrzyga kolizję przestrzeni z klasą, ale nie przestrzeni z przestrzenią; to samo dotyczy `Lemat.Application.Agenci` i projektu `Lemat.Agenci`.
- **Oczekiwana poprawka:** ujednolicić p. 1–6 w 3.2/3.3/3.5/16.1 (dla p. 1 wybrać jedną formę i dopisać wartości do 3.5 i 12.3); p. 7 — referencje i pakiety w 6.4; p. 8 — zapis w ADR-049 i reguła analizatora (pełne kwalifikowanie albo aliasy `using`).
- **Status:** ZAMKNIĘTA (iteracja 7). Wszystkie 8 punktów jest ujednoliconych: `ORYGINALNA|MASKOWANA` w 3.5 i 12.3, 3.2, 3.3 (`IModelDecyzyjny`), typy TS, `KontekstDecyzji`, ES256 w 16.1, referencje i pakiety `Lemat.Symulacje`/ML.NET oraz test `BrakCzesciowejKwalifikacjiPrzestrzeni` w ADR-049.

### C6-13: „Blokada” ma trzecie znaczenie (konto zablokowane) mimo deklaracji „dokładnie dwa znaczenia”
- **Waga:** NISKA
- **Lokalizacja:** 3.1 p. 5 („**Blokada** … ma dokładnie dwa znaczenia domenowe”); 3.4 część 3 (dozwolone użycia `ZABLOKOWANE` — wartość `status_konta`, `NAUCZYCIEL_DOCELOWY_ZABLOKOWANY`); 3.6 `StatusKonta.Zablokowane`, `PowodEskalacji.NauczycielDocelowyZablokowany`
- **Problem:** konto zablokowane przez Administratora to trzecie znaczenie słowa — ani Blokada weryfikacji, ani Blokada obniżania progu. Lista dozwolonych użyć je przepuszcza, ale 3.1 p. 5 i ADR-049 p. 9 go nie definiują. To ta sama klasa ryzyka co C4-04 / C5-04: test CI akceptuje `zablokow*` w znaczeniu, którego słownik nie zna.
- **Oczekiwana poprawka:** dopisać trzecie znaczenie z kwalifikatorem („blokada konta”: `status_konta = 'ZABLOKOWANE'`, `NAUCZYCIEL_DOCELOWY_ZABLOKOWANY`) do 3.1 p. 5, ADR-049 p. 9 i PZS-1 albo przemianować (np. `ZAWIESZONE`, `NAUCZYCIEL_DOCELOWY_ZAWIESZONY` — z kolizją z „Zawieszeniem Wyniku zadania” do rozstrzygnięcia).
- **Status:** ZAMKNIĘTA (iteracja 7). Trzecie znaczenie „blokada konta” z kwalifikatorem jest w 3.1 p. 5, 3.4 i ADR-049 p. 12.

### C6-14: Struktura App Router nie pokrywa ról Administratora i recenzenta treści ani wyświetlania zdjęć bez BFF
- **Waga:** ŚREDNIA
- **Lokalizacja:** 6.8 (drzewo `src/app/`), 6.9; 6.2 (grupy tras API `/admin/zadania`, `/zgloszenia-bledu-zadan`, `/zlecenia-generowania`); 3.3 („Recenzent treści … do Przeglądu Zadań i Wkładek o zasięgu globalnym”, „Zgłoszenie błędu w Zadaniu … rozpatrywany przez recenzenta treści albo Administratora”, „Przegląd zadania … Nauczyciela uprawnionego lub Administratora”); 12.3 (`GET /rozwiazania/{id}/zdjecia/{…}` → `302` na podpisany URL); 16.2 (CSP `img-src 'self' blob: https://<magazyn>`); ADR-054 p. 5 (`openapi-fetch` jedynym klientem); 5.2 (Serwist)
- **Problem:**
  1. Administrator nie ma ekranów Przeglądu zadań, Zgłoszeń błędu w Zadaniu, `/admin/zadania` ani zleceń generowania (FR-11, FR-14, ADR-028, ADR-038), choć API je udostępnia. Recenzent treści (Nauczyciel) nie ma ekranu przeglądu Wkładek teoretycznych — `wkladki-teoretyczne` jest tylko w `(administrator)`, a moduł `tresci` wskazuje ten ekran jako jedyny. Uczeń zalogowany nie ma ekranu członkostwa w Klasach (dołączenie kodem z konta, `DELETE /uczniowie/ja/klasy/{id}`), a `dolacz/[kod]` jest w `(publiczne)`, mimo że wymaga sesji Ucznia albo rejestracji z kodem — przepływ nie jest opisany.
  2. Zdjęcia kartek (Uczeń, Nauczyciel w Weryfikacji) są pobierane z endpointu wymagającego `Authorization`. `<img src="/api/v1/…">` nie wyśle nagłówka (token jest tylko w pamięci), więc dostanie `401`. Potrzebny jest jawny wzorzec: `fetch` z nagłówkiem → `302` na S3 → odczyt `blob` (wymaga CORS na buckecie) → `URL.createObjectURL`, albo endpoint zwracający JSON z podpisanym URL. Reguła 6.9 p. 4 (`fetch` wyłącznie w `klient.ts`) musi go objąć; trzeba też zwalniać `blob:` URL (pamięć telefonu przy 5 zdjęciach × 2560 px).
  3. Next.js 16 buduje domyślnie Turbopackiem, a `@serwist/next` 9 wymaga webpacka; potrzebne `next build --webpack` albo wariant Serwist dla Turbopacka — wymaganie nie jest zapisane w 5.2 / ADR-054.
- **Oczekiwana poprawka:** dopisać trasy `(administrator)/admin/zadania/**`, `zgloszenia-bledu-zadan`, `zlecenia-generowania`; `(nauczyciel)/nauczyciel/wkladki-teoretyczne/**` dla recenzenta (strażnik flagi `recenzent_tresci`); `(uczen)/klasy` i opis przepływu `dolacz/[kod]` (zalogowany / niezalogowany); wzorzec pobierania zdjęć w `lib/api` z testem Vitest i E2E; decyzję o buildzie Serwist w ADR-054.
- **Status:** ZAMKNIĘTA (iteracja 7). Są trasy Administratora i recenzenta (`StraznikFlagi`), `(uczen)/klasy`, przepływ `dolacz/[kod]`, `GET …/zdjecia/{id}/adres` (JSON z podpisanym URL, bez `blob:` i CORS) oraz `next build --webpack` dla Serwist.

### C6-15: Drobne luki wykonalności procesów w tle i infrastruktury
- **Waga:** NISKA
- **Lokalizacja:** ADR-053 p. 2e; 5.4 (pule połączeń); 3.6 `ProfilUcznia.Zastosuj`, `SciezkaNauki.Zaproponuj`, `Ocena.Automatyczna(RezultatOceny r, Trasa t)`
- **Problem:**
  1. ADR-053 p. 2d chroni odnowienie dzierżawy warunkiem `wykonawca = @ja`, ale zapis wyniku (p. 2e: `ZAKONCZONA`, ponowienie, `MARTWA`) nie ma tego warunku. Proces, który stracił dzierżawę (pauza GC, utrata połączenia), może oznaczyć jako `ZAKONCZONA` pracę wykonywaną już przez innego workera albo przenieść ją do `MARTWA`.
  2. Suma pul połączeń: `api` 2 × (50 + 1), `worker` 2 × (60 + 1), `scheduler` 2 × 5, `migracje` 1 = 235 połączeń. Brak wymagania `max_connections` dla zarządzanego PostgreSQL (wiele planów ma 100–200) i decyzji o PgBouncerze (z ograniczeniem: `LISTEN` i blokady doradcze sesji wymagają trybu sesji).
  3. `ProfilUcznia.Zastosuj` nie pomija Wyniku z `UniewaznionyAt != null` (zawieszonego przez Zgłoszenie błędu odczytu albo unieważnionego) — gdy `AKTUALIZUJ_PROFIL` wykona się po zawieszeniu, wynik trafi do Profilu wbrew ADR-022.
  4. `SciezkaNauki` ładuje wszystkie elementy (`Kolejnosc = _elementy.Count + 1`), czyli setki do tysięcy wierszy na Ucznia w ciągu roku, przy każdym `GET …/rekomendacje/nastepna` (cel p95 ≤ 500 ms). Element `PROPONOWANY` po `WygasaAt`, którego praca w tle jeszcze nie oznaczyła `WYGASLY`, blokuje `Zaproponuj` błędem `ELEMENT_JUZ_PROPONOWANY`. Limit Serii ≤ 8 jest tylko w komentarzu.
  5. Fabryka `Ocena.Automatyczna(RezultatOceny r, Trasa t)` używa `RezultatOceny` (rezultat Agenta oceniającego), którego przestrzeń nazw nie jest określona. Jeśli to typ z `Lemat.Application`, domena zależy od aplikacji (cykl warstw z 6.5).
- **Oczekiwana poprawka:** (1) `WHERE id = @id AND wykonawca = @ja AND status = 'W_TOKU'` przy zapisie wyniku i test; (2) budżet połączeń w 17.4 / 22 z minimalnym `max_connections` i decyzją o puli; (3) strażnik w `Zastosuj` + test; (4) agregat z aktywnym oknem elementów (np. tylko `PROPONOWANY`/`ROZPOCZETY`) i `Kolejnosc` z sekwencji, wygaszanie w `Zaproponuj` przed sprawdzeniem niezmiennika, limit Serii w metodzie; (5) `RezultatOceny` w `Lemat.Domain.Ocenianie` albo fabryka przyjmująca typy domenowe.
- **Status:** ZAMKNIĘTA (iteracja 7). p. 1: warunek `wykonawca`/`W_TOKU` i `FOR UPDATE`. p. 2: ADR-059 (155 połączeń, `max_connections ≥ 200`). p. 3: strażnik `UniewaznionyAt`. p. 4: `_aktywne` i `ostatnia_kolejnosc` (kolumna w DDL). p. 5: `RezultatOceny` w domenie. Składnię filtrowanego `Include` opisuje C7-06 p. 4.

### Podsumowanie iteracji 6

Weryfikacja uwag otwartych po iteracji 5:

| Stan | Liczba | Identyfikatory |
|---|---|---|
| ZAMKNIĘTA | 9 | C5-01 … C5-09 (C5-06 … C5-09 jako wymagania 18.4 dla nowego `TASKS.md`) |
| OTWARTA | 0 | — |

Nowe uwagi iteracji 6:

| Waga | Liczba uwag | Identyfikatory |
|---|---|---|
| KRYTYCZNA | 0 | — |
| WYSOKA | 4 | C6-01, C6-02, C6-03, C6-04 |
| ŚREDNIA | 8 | C6-05, C6-06, C6-07, C6-08, C6-09, C6-10, C6-11, C6-14 |
| NISKA | 3 | C6-12, C6-13, C6-15 |
| **Razem** | **15** | |

Werdykt: wersja 3.0 zamyka wszystkie uwagi iteracji 5. Mapowanie słownika jest formalnie kompletne: 111/111 pojęć, a 28 z 30 enumów C# zgadza się z DDL wartość po wartości. Reguła transliteracji ADR-049 jest odwracalna i spójna z `JsonNamingPolicy`. Graf referencji projektów .NET jest acykliczny i ma właściwy kierunek (domena bez pakietów, adaptery i infrastruktura zależą od aplikacji). Wybór SymPy jako sidecara, SSE z `LISTEN/NOTIFY`, ES256 i Next.js bez BFF jest co do zasady wykonalny. Dokument nie jest jednak na poziomie produkcyjnym:
- dwa mechanizmy, na których opiera się idempotencja i opóźnienie (zlecanie prac z `ON CONFLICT` przez `Add` encji, `NOTIFY` w `SaveChangesInterceptor`), nie działają w EF Core tak, jak je opisano (C6-01);
- model sesji bez BFF z rotacją i wykrywaniem ponownego użycia refresh tokenu wylogowuje użytkownika przy dwóch kartach (C6-02);
- warstwa agentów nie ma portu, przez który Semantic Kernel dostałby `IChatClient` roli bez obejścia `konfiguracja_modelu` (C6-03);
- szkielet głównego agregatu `Rozwiazanie` zostawia Rozwiązanie w `PRZESLANE` po ponowieniu, zawyża metrykę NFR-01 i nie chroni przed oceną nieaktualnej Transkrypcji (C6-04).

Poprawki C6-01 … C6-04 są lokalne (kontrakt `IZlecaniePrac`/`IJednostkaPracy`, okno tolerancji refresh tokenu + single-flight w kliencie, jeden port w `Lemat.Application.Modele`, uzupełnienie metod agregatu). Większość pozostałych uwag mieści się w planowanych iteracjach 2 i 3, ale powinna zostać wpisana do ich zakresu.

## Iteracja 7 (v3.1 — agenci AI i dane)

Data przeglądu: 2026-11-01. Wersja dokumentu: 3.1 (iteracja 2 pętli „.NET 10 + Next.js”). Zakres: 3.6, 7, 10, 13, `docs/spec/ddl.md`, `docs/spec/ef-core.md`, `docs/spec/prompts.md`, ADR-056 … ADR-059, uzupełnienia 3.1 ADR-020/023/049/051/052/053/054/055 i mapowanie C6 w `ARCHITECTURE_CHANGELOG.md`. Sekcje 12, 14 i 15 mają notkę „do przepisania w iteracji 3”, więc ich kod pythonowy nie jest w tej rundzie oceniany.

**Weryfikacja uwag iteracji 6.** Wszystkie 15 uwag (C6-01 … C6-15) są ZAMKNIĘTE. Statusy z uzasadnieniem są wpisane przy uwagach. C6-10 jest zamknięta w p. 1, 3 i 4, a niedziałający mechanizm limitu wywołań z p. 2 przechodzi do C7-01.

**Metoda (skrypty i projekt w scratchpadzie, niezależnie od tabel architekta).**

1. Bloki A i B `docs/spec/ddl.md` załadowano do PostgreSQL 16.
   - Blok A działa bez błędów.
   - Blok B zgłasza błędy wyłącznie w `0005_uprawnienia.sql`, bo DDL nie tworzy ról `lemat_api`, `lemat_worker` i `lemat_scheduler`.
   - Katalog po załadowaniu zawiera:
     - 58 tabel (+ 4 partycje `decyzja_modelu`);
     - 43 typy wyliczeniowe;
     - 15 triggerów;
     - 142 CHECK-i, 124 klucze obce i 21 ograniczeń UNIQUE.
2. Skrypty porównały z katalogiem:
   - nazwy kolumn, CHECK-ów, indeksów i triggerów użyte w `ef-core.md`;
   - listę `MapowanieTypowWyliczeniowych`;
   - wartości enumów C# z 3.6, 10 i 13 po transliteracji `UPPER_SNAKE`;
   - właściwości szkieletów agregatów 3.6 po konwencji `snake_case`.
3. Projekt `dotnet new console` (.NET SDK 10.0.114) z aktualnymi stabilnymi pakietami:
   - `Microsoft.SemanticKernel.Core` i `Microsoft.SemanticKernel.Yaml` 1.81.0;
   - `Microsoft.Extensions.AI` i `Microsoft.Extensions.AI.OpenAI` 10.11.0;
   - `Magick.NET-Q8-AnyCPU` 15.0.0;
   - `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 i `EFCore.NamingConventions` 10.0.1;
   - `Azure.AI.OpenAI` 2.1.0, `JsonSchema.Net` 9.6.0 i `Cronos` 0.13.0.

   Kluczowe API z sekcji 10 i ADR-052/056/058 sprawdzono kompilacją, refleksją i uruchomieniem: potok `FunctionInvokingChatClient` z fałszywym `IChatClient`, budowa modelu EF Core, polecenia SQL na PostgreSQL 16 i polityka Magick.NET.

**Wynik weryfikacji API (ryzyko R-27).**

| API wskazane w dokumencie | Wynik | Uwaga |
|---|---|---|
| `KernelFunction.WithKernel(Kernel)` | istnieje (kompiluje się); `KernelFunction : FullyQualifiedAIFunction : AIFunction` | nazwa narzędzia widziana przez model = `sympy_sprawdz_rownowaznosc` (C7-01) |
| `DelegatingAIFunction` (`InvokeCoreAsync`) | istnieje (`Microsoft.Extensions.AI.Abstractions` 10.11) | — |
| `FunctionInvokingChatClient.MaximumIterationsPerRequest`, `AllowConcurrentInvocation`, `MaximumConsecutiveErrorsPerRequest` | istnieją | semantyka inna niż w ADR-058 (C7-01) |
| `ChatOptions.AllowMultipleToolCalls`, `ChatResponseFormat.ForJsonSchema`, `ChatToolMode.Auto`, `DataContent`, `AIJsonUtilities.CreateJsonSchema` | istnieją | schemat generowany różni się od schematów `prompts.md` (C7-09) |
| `ChatClientBuilder.UseOpenTelemetry(sourceName:, configure: c => c.EnableSensitiveData = false)`, `UseFunctionInvocation(configure:)` | istnieją | kolejność potoku gubi spany `execute_tool` (C7-02) |
| `ChatClientBuilder.Use(new LimitCzasuChatClient(…))` (ADR-058 p. 3) | **nie kompiluje się** (CS1503) | `Use` przyjmuje fabrykę `Func<IChatClient, IChatClient>`; poprawnie `.Use(wewn => new LimitCzasuChatClient(wewn, …))` (C7-02) |
| `KernelPluginFactory.CreateFromObject`, `KernelFunctionYaml.ToPromptTemplateConfig`, `KernelPromptTemplateFactory.Create(…).RenderAsync` | istnieją | renderowanie koduje wartości zmiennych HTML-em (C7-14) |
| `MagickNET.Initialize(IConfigurationFiles, string)`, `ConfigurationFiles.Default.Policy.Data`, `MagickNET.SetTempDirectory`, `ResourceLimits.{Memory,Disk,Thread,Time,Area,Width,Height}` | istnieją; polityka działa (SVG/TXT → `MagickPolicyErrorException`); `ConfigurationFiles` jest w przestrzeni `ImageMagick.Configuration`; `ResourceLimits.Time` to `ulong` w sekundach | `MagickNET.SetSecurityPolicy` **nie istnieje** w 15.0.0 (dokument go nie używa; alternatywa: `ConfigurationFiles.Policy.Data`, jak w ADR-052 uzup. 3.1) |
| `NpgsqlDataSourceBuilder.MapEnum(Type, string, INpgsqlNameTranslator)`, `EnableDynamicJson()`, `UseNpgsql(…, o => o.MapEnum(Type, …).MigrationsHistoryTable(…).CommandTimeout(…))`, `UseSnakeCaseNamingConvention()` | istnieją | — |
| `Database.SqlQuery<T>($"INSERT … ON CONFLICT … RETURNING …")` | działa z `ToListAsync()`; z `FirstOrDefaultAsync()` → `InvalidOperationException` (SQL niekomponowalny) | C7-06 p. 6 |
| `ExecuteUpdateAsync` z „`UPDATE … RETURNING`” (ef-core 6.4) | `ExecuteUpdateAsync` zwraca `int` (liczba wierszy), nie ma `RETURNING` | C7-06 p. 5 |
| filtrowany `.Include("_aktywne", e => …)` (ef-core 6.5) | przeciążenie nie istnieje | działa `Include(s => EF.Property<List<ElementSciezki>>(s, "_aktywne").Where(…))` (C7-06 p. 4) |
| `Property<uint>("xmin").HasColumnType("xid")…IsConcurrencyToken()` | działa; skrypt migracji nie tworzy kolumny `xmin` | — |
| `AzureOpenAIClient(…).GetChatClient(m).AsIChatClient()`, `OpenAIClient(…, new OpenAIClientOptions { Endpoint })` | działają; `Azure.AI.OpenAI` 2.1.0 (najnowsza stabilna) jest budowana na `OpenAI` 2.1.0, a `Microsoft.Extensions.AI.OpenAI` 10.11 wymaga `OpenAI` 2.14.0 | C7-15 p. 6 |

### C7-01: Limit wywołań funkcji Agenta generującego nie działa tak, jak opisano, a nazwy narzędzi w prompcie, danych Mock i teście są błędne
- **Waga:** WYSOKA
- **Lokalizacja:** ADR-058 p. 3 (`MaximumIterationsPerRequest = parametry.MaksIteracjiFunkcji ?? 1`, `MaximumConsecutiveErrorsPerRequest = 1`), p. 4 (`FunkcjaZLimitem`, „wywołanie nr `limit + 1` rzuca `BladModeluTrwalyException`”, `MaximumIterationsPerRequest = 8` jako „druga linia obrony”), p. 7 (`LimitWywolanFunkcjiTests`); 10.2.4 (`KernelPluginFactory.CreateFromObject(…, "sympy")`); 10.2.5 (test „9 żądań wywołań → zlecenie z przyczyną `LIMIT_WYWOLAN_FUNKCJI`”); `prompts.md` 4.3 i 6 (`"nazwa": "sprawdz_rownowaznosc"`); 10.0.4 (termin 60 s)
- **Problem (sprawdzone uruchomieniem z fałszywym `IChatClient`, który zawsze żąda wywołania funkcji):**
  1. Przy `MaximumIterationsPerRequest = 8` wykonuje się dokładnie 8 wywołań funkcji. Potem `FunctionInvokingChatClient` wysyła **9. żądanie bez narzędzi** (`Tools = 0`, `ToolMode = null`) i zwraca jego odpowiedź jako zwykły wynik, bez wyjątku. `FunkcjaZLimitem` nigdy nie dostaje 9. wywołania. Opisany test (`BladModeluTrwalyException("LIMIT_WYWOLAN_FUNKCJI")`, przyczyna w zleceniu) więc nie przejdzie, a „pierwsza linia obrony” jest martwym kodem.
  2. Przy `MaximumConsecutiveErrorsPerRequest = 1` (wartość z ADR-058) wyjątek pierwszego nadmiarowego wywołania jest połykany i wysyłany do modelu jako rezultat funkcji. Na zewnątrz wychodzi dopiero przy kolejnym błędzie, czyli przy **10.** wywołaniu (zmierzono: licznik 10, wykonane 8). Natychmiastowe przerwanie wymaga wartości `0`.
  3. `KernelFunction` z pluginu `"sympy"` jest widoczna jako `AIFunction` o nazwie **`sympy_sprawdz_rownowaznosc`** (`FullyQualifiedAIFunction`). Prompt `zadanie-v1` każe wołać `sprawdz_rownowaznosc` / `postac_liniowa`, dane Mock (`prompts.md` 6) zawierają `"nazwa": "sprawdz_rownowaznosc"`, a test ADR-058 p. 7 żąda `sprawdz_rownowaznosc`. Takie wywołanie trafia w gałąź „nieznana funkcja”: model dostaje komunikat o braku narzędzia, licznik się nie zmienia i nic się nie wykonuje (zmierzono: 0 wykonań przy 8 żądaniach). Scenariusze Mock „3 Zadania wygenerowane z wywołaniami narzędzi” nie wywołają więc żadnego narzędzia, a `WywolanieNarzedzia.Funkcja` w śladzie NFR-05 będzie miało inną nazwę niż w prompcie.
  4. Termin 60 s obejmuje cały potok, czyli do 9 kolejnych żądań do modelu. Każde z nich ponownie wysyła rosnący kontekst, a ostatnie generuje odpowiedź do `max_tokens` 6 000. Dokument nie uzasadnia, że 60 s wystarcza. Przekroczenie terminu po 3 próbach daje zlecenie `NIEUDANE`.
- **Oczekiwana poprawka:**
  - Wybrać jedną semantykę limitu:
    - (a) `MaximumIterationsPerRequest = limit + 1`, `MaximumConsecutiveErrorsPerRequest = 0` i `FunkcjaZLimitem` jako jedyny strażnik z wyjątkiem;
    - albo (b) świadomie użyć mechanizmu wbudowanego: po `limit` iteracjach ostatnie żądanie idzie bez narzędzi, a zlecenie dostaje adnotację `LIMIT_WYWOLAN_FUNKCJI` wyliczaną z licznika, bez wyjątku.
  - Ustalić nazwy narzędzi: plugin bez prefiksu albo jawne nazwy `sympy_*` wszędzie (prompt, Mock, test, ślad, metryka `wywolania_funkcji_total{funkcja}`). Do tego test „nazwa w `ChatOptions.Tools` = nazwa w prompcie i w danych Mock”.
  - Wybrać `TerminateOnUnknownCalls` (np. `true` + `BladModeluTrwalyException("NIEZNANE_NARZEDZIE")`).
  - Dodać budżet czasu generowania z liczbą żądań (np. termin per żądanie i łączny 120 s) albo uzasadnić 60 s pomiarem.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-058 uzup. 3.2 p. 1–2, 4. Sprawdzone uruchomieniem: `MaximumIterationsPerRequest = 9` i `MaximumConsecutiveErrorsPerRequest = 0` z `FunkcjaZLimitem` (limit 8) dają 8 wykonań i wyjątek `LIMIT_WYWOLAN_FUNKCJI` przy 9. żądaniu (9 żądań do modelu). Nazwy `sympy_*` są w prompcie, Mock i teście. Budżet to 45 s na żądanie i 150 s na wywołanie agenta.

### C7-02: Kolejność i konstrukcja potoku `ChatClientBuilder` (ADR-058 p. 3) — kod się nie kompiluje, a spany `execute_tool` i koszt per iteracja nie powstają
- **Waga:** ŚREDNIA
- **Lokalizacja:** ADR-058 p. 3; 10.0.5 (tabela telemetrii: spany `execute_tool {funkcja}`); 10.2.5 („spany `execute_tool sprawdz_rownowaznosc` … z `Microsoft.Extensions.AI`”); 10.0.4
- **Problem:**
  1. `.Use(new LimitCzasuChatClient(parametry.TimeoutS))`, `.Use(new KosztIZuzycieChatClient(…))` itd. nie kompilują się (CS1503). `ChatClientBuilder.Use` przyjmuje fabrykę `Func<IChatClient, IChatClient>`, a `DelegatingChatClient` wymaga klienta wewnętrznego w konstruktorze.
  2. `UseOpenTelemetry` jest pierwszym (najbardziej zewnętrznym) elementem, a `UseFunctionInvocation` ostatnim (najbliżej dostawcy). Uruchomienie takiego potoku z `ActivityListener` daje **jeden span `chat`** na całą pętlę i **zero spanów `execute_tool`**. W odwrotnej kolejności (`UseFunctionInvocation().UseOpenTelemetry()`) powstają `chat` per iteracja, `execute_tool sympy_sprawdz_rownowaznosc` i `orchestrate_tools`. Telemetria z 10.0.5 i ślad wywołań w spanach (NFR-05) są więc nieosiągalne w kolejności z ADR-058.
  3. Z tego samego powodu `LimitCzasuChatClient`, `KosztIZuzycieChatClient`, `OgranicznikChatClient` i `KlasyfikacjaBledowChatClient` obejmują całą pętlę wywołań funkcji, a nie pojedyncze żądanie do dostawcy. Ogranicznik dla `ROZPOZNAWANIE_PISMA` jest bez narzędzi, więc tam to nie szkodzi. Dla `GENERATYWNY_ZADANIA` nie wiadomo jednak, czy limit czasu, koszt i ogranicznik działają per żądanie HTTP, czy per wywołanie agenta.
  4. `KlasyfikacjaBledowChatClient` dostaje `OperationCanceledException` z limitu czasu potoku i z anulowania dzierżawy tym samym tokenem. Nie ma reguły, jak je rozróżnia — por. C7-04.
- **Oczekiwana poprawka:** zapis potoku w kompilowalnej postaci (`.Use(w => new LimitCzasuChatClient(w, …))`) z jawną kolejnością i uzasadnieniem każdej warstwy (per żądanie vs per wywołanie agenta). Zalecana kolejność: `UseFunctionInvocation` na zewnątrz, `UseOpenTelemetry` wewnątrz niego, a koszt, ogranicznik i klasyfikacja per żądanie do dostawcy. Test `Lemat.Modele.Tests` z `ActivityListener`: „generowanie z 2 wywołaniami → 3 spany `chat` i 2 spany `execute_tool`”.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-058 uzup. 3.2 p. 3: potok `.Use(w => new …(w, …))`, `UseFunctionInvocation` najbardziej zewnętrzne, a telemetria, klasyfikacja, limit czasu i koszt działają per żądanie. Reguła rozróżniania anulowania jest zapisana (`!ct.IsCancellationRequested`).

### C7-03: Ponowienie i odzyskiwanie dzierżawy łamią indeks `ux_praca_dedup_oczekujace` — jeden konflikt blokuje odzyskiwanie wszystkich prac
- **Waga:** WYSOKA
- **Lokalizacja:** 13.4.2 („Błąd przejściowy”: `SET status = … 'OCZEKUJE'` z warunkiem dzierżawy; „Odzyskiwanie porzuconych dzierżaw”: jedno `UPDATE … WHERE status = 'W_TOKU' AND dzierzawa_do < now()`); 13.3 `AKTUALIZUJ_PROFIL` i `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` (deduplikacja wyłącznie wśród `OCZEKUJE`); `ddl.md` `ux_praca_dedup_oczekujace`; ADR-056 p. 2
- **Problem:**
  - Przy `TrybDeduplikacji.TylkoOczekujace*` może istnieć jednocześnie praca A `W_TOKU` i praca B `OCZEKUJE` z tym samym kluczem. To przypadek zamierzony:
    - nowy Wynik zadania Ucznia w trakcie `AKTUALIZUJ_PROFIL`;
    - nowy `PUT …/transkrypcja` w trakcie Sprawdzenia zgodności poprawek.
  - Gdy A kończy się błędem przejściowym albo traci dzierżawę, oba polecenia z 13.4.2 próbują ustawić A na `OCZEKUJE`. Sprawdzono na PostgreSQL 16 z DDL:
    - `PonowAsync` kończy się `23505 duplicate key … ux_praca_dedup_oczekujace`;
    - polecenie odzyskiwania dzierżaw (jedno `UPDATE` dla wszystkich wygasłych dzierżaw) **kończy się tym samym błędem w całości**, więc nie odzyskuje także prac innych typów. W teście nie odzyskała się praca `OCEN_ROZWIAZANIE` z wygasłą dzierżawą.
  - Stan trwa, dopóki B nie zostanie pobrana. Przy nasyconej puli `POMOCNICZA` albo `dostepna_od` B w przyszłości (odstępy 1 s … 2 min) żadna porzucona praca w systemie nie wraca do kolejki. Dotyczy to też `OCEN_ROZWIAZANIE` z terminem twardym 120 s, a skutkiem jest masowe T-6.
  - Wyjątek z `PonowAsync` nie jest obsłużony w `PetlaAsync` (C7-04).
- **Oczekiwana poprawka:**
  - Przy ponowieniu i odzyskiwaniu typu `TylkoOczekujace*`: gdy istnieje praca `OCZEKUJE` z tym samym kluczem, A przechodzi do `ZAKONCZONA` z `ostatni_blad = 'ZASTAPIONA_NOWSZA'`. Dla `AKTUALIZUJ_PROFIL` B i tak przetworzy wszystkie niezastosowane Wyniki. Dla `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` B ma nowszy ładunek.
  - Alternatywnie scalić A z B (`DELETE` B i przejęcie ładunku) w jednym poleceniu z `NOT EXISTS`.
  - Odzyskiwanie dzierżaw wykonywać per wiersz albo z `ON CONFLICT`-bezpiecznym predykatem `NOT EXISTS (SELECT 1 … status = 'OCZEKUJE' AND typ = p.typ AND klucz_deduplikacji = p.klucz_deduplikacji)`.
  - Testy (g) „ponowienie przy istniejącej pracy `OCZEKUJE` z tym samym kluczem” i (h) „odzyskiwanie dzierżaw z jednym konfliktem odzyskuje pozostałe prace”.
- **Status:** ZAMKNIĘTA (iteracja 8). `ponow_prace` i `odzyskaj_dzierzawy` w `0006_prace_w_tle.sql`. Sprawdzone na PostgreSQL 16 z pełnym DDL: A (`AKTUALIZUJ_PROFIL`, `W_TOKU`, dzierżawa wygasła) + B (`OCZEKUJE`, ten sam klucz) + C (`OCEN_ROZWIAZANIE`, dzierżawa wygasła) → A `ZAKONCZONA`/`ZASTAPIONA_NOWSZA`, C `OCZEKUJE`, bez błędu. `ponow_prace` przy konflikcie zwraca `ZAKONCZONA`, a dla pracy już odzyskanej `NULL`.

### C7-04: `PulaPracHostedService` klasyfikuje każde anulowanie jako utratę dzierżawy i nie obsługuje pozostałych wyjątków — zatrzymane Rozwiązania i zatrzymanie procesu `worker`
- **Waga:** WYSOKA
- **Lokalizacja:** 13.4.1 `PetlaAsync` (`catch (OperationCanceledException) when (!zatrzymanie.IsCancellationRequested) → RezultatPrzestarzaly("DZIERZAWA_UTRACONA")` → `ZakonczAsync`), `ZapiszWynikAsync`, `PobierzAsync`; 10.0.3 (`CancellationTokenSource.CancelAfter(min(timeout_s, termin − teraz))`); 10.4.6 („całość ograniczona terminem twardym 120 s (`CancelAfter`)”, „po terminie twardym → handler wykonuje T-6”); 13.2 `JevAdapter` (`HttpClient.Timeout = Infinite`, `CancelAfter`); 10.3.4 („przekroczenie terminu → `BladModeluPrzejsciowyException`”)
- **Problem:**
  1. `OperationCanceledException` powstaje w czterech źródłach:
     - przy utracie dzierżawy;
     - w `CancelAfter` w `WywolanieCzatu`;
     - przy terminie twardym 120 s Agenta oceniającego;
     - przy `TaskCanceledException` z `HttpClient` (S3, Jev, sidecary).

     Pętla traktuje każde z nich jako `DZIERZAWA_UTRACONA` i wykonuje `ZakonczAsync`. Przy przekroczeniu czasu (dzierżawa wciąż ważna) to polecenie **się udaje**, więc praca kończy się jako `ZAKONCZONA` bez efektu i bez ścieżki „po wyczerpaniu prób”:
     - Rozwiązanie zostaje w `OCENIANIE` bez T-6;
     - Rozwiązanie zostaje w `ROZPOZNAWANIE` bez `BLAD_ROZPOZNAWANIA`.

     Żadna praca z 13.3 nie zbiera takich stanów (`PORZUC_NIEZATWIERDZONE` nie obejmuje `ROZPOZNAWANIE` ani `OCENIANIE`). Mapowanie timeoutu na `BladModeluPrzejsciowyException` jest opisane tylko dla Agenta rozpoznającego, a dla terminu twardego Oceny wprost przerzucone na „handler”, bez kontraktu.
  2. `PetlaAsync` nie ma `catch (Exception)`. Każdy inny wyjątek kończy zadanie pętli, a przez `Task.WhenAll` cały `ExecuteAsync` usługi:
     - `DbUpdateException`, `NpgsqlException`;
     - `23505` z C7-03;
     - `NiezmiennikNaruszonyException` z handlera;
     - błąd w `ZapiszWynikAsync` albo `PobierzAsync`.

     W .NET 10 domyślnie `HostOptions.BackgroundServiceExceptionBehavior = StopHost`, więc proces `worker` zatrzymuje się w całości (wszystkie pule obu typów). Chwilowa niedostępność bazy przy `PobierzAsync` wyłącza więc całą replikę.
  3. `CancelAfter(min(timeout_s, termin − teraz))` z ujemnym czasem (termin minął przed wywołaniem) rzuca `ArgumentOutOfRangeException`, którego klasyfikacja 10.0.4 nie obejmuje.
- **Oczekiwana poprawka:**
  - Jawna reguła: utratę dzierżawy rozpoznaje wyłącznie `dzierzawa.Token.IsCancellationRequested` przy niezanulowanym `zatrzymanie` **i** wyniku `OdnowDzierzaweAsync = false`. Każde inne anulowanie wewnątrz handlera jest przekroczeniem czasu: `Ponow`, a po terminie twardym `Trwaly` → `PoWyczerpaniuAsync` (T-6, `BLAD_ROZPOZNAWANIA` z `PRZEKROCZONY_CZAS`).
  - `catch (Exception)` w pętli z `Ponow` i metryką. `ZapiszWynikAsync` i `PobierzAsync` z ponowieniem i odstępem, bez zakończenia pętli.
  - `BackgroundServiceExceptionBehavior` ustawione świadomie (`Ignore` + health check albo `StopHost` z restartem kontenera i alertem).
  - Praca kontrolna harmonogramu dla Rozwiązań w `ROZPOZNAWANIE`/`OCENIANIE` bez aktywnej pracy dłużej niż 5 min.
  - Testy: „timeout modelu → `Ponow`, a nie `ZAKONCZONA`”, „wyjątek bazy w `PobierzAsync` nie zatrzymuje hosta”.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-066 p. 1, 2, 4, 6: klasyfikacja po `UtraconaAt`, `catch (Exception)` z wynikiem, `PonawiajInfrastrukture`, świadome `StopHost` z restartem i health check, `TerminMinalException`, praca `SPRAWDZ_ZAWIESZONE_ROZWIAZANIA` (E-46, C-52).

### C7-05: Strategia migracji i test `SchematZgodnyZDdlTest` są niewykonalne — dwa źródła schematu dają różne nazwy, typy i rodzaje ograniczeń
- **Waga:** WYSOKA
- **Lokalizacja:** 7.2 („prefiksy `ux_`, `ix_`, `fk_`, `trg_`, `ck_` (CHECK nazwany w konfiguracji EF Core)”); 7.7 p. 2, 6; `ef-core.md` 6 (`HasCheckConstraint("ck_…")`, `HasMaxLength(60)`), 8 p. 1–2, 6
- **Problem:** `0001_SchematPoczatkowy` jest generowana z modelu EF Core i uzupełniana plikami SQL, a test wymaga, by `pg_dump --schema-only` i katalogi `pg_constraint`/`pg_index` były równe bazie z `ddl.sql`. Stan po załadowaniu DDL:
  - **142 z 142 CHECK-ów** ma nazwy automatyczne (`uzytkownik_check`, `zadanie_check3`, …), a `ef-core.md` nadaje 12 z nich nazwy `ck_*`, których w DDL nie ma (skrypt: 12 × „CHECK BRAK”). Pozostałe 130 CHECK-ów (np. `char_length(imie) BETWEEN 1 AND 60`, zakresy `smallint`) nie występuje w konfiguracji EF, więc migracja z modelu ich nie utworzy. Lista „obiektów spoza modelu EF” w 7.7 p. 2 obejmuje tylko CHECK-i na tablicach.
  - **118 z 124 kluczy obcych** ma nazwy `*_fkey`, a EF z `UseSnakeCaseNamingConvention` tworzy `fk_<tabela>_<tabela>_<kolumna>`.
  - **21 ograniczeń UNIQUE** (`*_key`) w DDL odpowiada w EF `HasIndex(…).IsUnique()`, czyli indeksom unikalnym bez ograniczenia. `pg_constraint` się różni.
  - `HasMaxLength(60)` / `HasMaxLength(8000)` daje w Npgsql `character varying(n)`, a DDL ma `text` + CHECK `char_length`.

  Test zakończy się więc setkami różnic, a 7.2 opisuje konwencję nazw, której DDL nie stosuje.
- **Oczekiwana poprawka:** jedno źródło schematu.
  - **Wariant A:** migracja `0001` = `migrationBuilder.Sql(ddl.sql)` + migawka modelu EF bez operacji. Test porównuje model EF z katalogiem (tabela, kolumna, typ, `NULL`, klucze, długości), np. przez `IModel`/`IRelationalModel` vs `information_schema`, zamiast porównania `pg_dump` dwóch baz.
  - **Wariant B:** DDL generowany z migracji (`dotnet ef migrations script`), a `ddl.md` jako artefakt.

  W obu wariantach DDL powinien nadać jawne nazwy wszystkim CHECK-om/FK/UNIQUE (zgodnie z 7.2). `HasMaxLength` należy zastąpić `HasColumnType("text")` + CHECK albo przyjąć `varchar(n)` w DDL.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-063 (wariant A): migracje wyłącznie SQL, model EF bez `HasCheckConstraint`/`HasMaxLength`/`HasTrigger`, test `ModelEfZgodnyZKatalogiem`. Sprawdzone: bloki A i B ładują się na czystej bazie PostgreSQL 16 bez błędów. Katalog: 60 tabel + 4 partycje, 43 enumy, 15 triggerów, 142 CHECK, 124 FK, 21 UNIQUE, role `lemat_*` utworzone.

### C7-06: Konfiguracje EF Core z `ef-core.md` nie budują modelu albo nie odpowiadają DDL (sprawdzone kompilacją i budową modelu)
- **Waga:** WYSOKA
- **Lokalizacja:** `ef-core.md` 6.1 (`Nauczyciel`), 6.5 (`ProfilUcznia`, `SciezkaNauki`, `StanBezpiecznika`), 6.4 (podjęcie Weryfikacji), 4 (`WersjaAgregatuInterceptor`), 7 (`PracaWTle`, `Uczen`); ADR-056 p. 2; 3.6 (`ProfilUcznia._opanowanie`, `StanBezpiecznika`, `PracaWTle.MaksProb`, `Uczen`)
- **Problem:**
  1. `HasOne<Uzytkownik>().WithOne().HasForeignKey<Nauczyciel>(n => n.Id)` przy `Nauczyciel : Agregat<NauczycielId>` i `Uzytkownik : Agregat<UzytkownikId>` → `InvalidOperationException: … foreign key properties {'Id' : NauczycielId} cannot target the primary key {'Id' : UzytkownikId} because it is not compatible`. To samo dotyczy `Uczen` (`UczenId`) i każdego FK z `UczenId`/`NauczycielId` do `uzytkownik`.
  2. `OwnsMany<OpanowanieUmiejetnosci>("_opanowanie", …)` przy polu `Dictionary<UmiejetnoscId, OpanowanieUmiejetnosci>` (3.6) → `The collection navigation '_opanowanie' … does not implement 'IEnumerable<OpanowanieUmiejetnosci>'`. Model się nie buduje.
  3. `StanBezpiecznika` ma właściwości cienia `konfiguracja_modelu_id`/`rodzaj_zadania`/`typ` i konstruktor `(konfiguracjaModeluId, rodzajZadania, typ)`. EF nie wiąże parametrów z cieniami o nazwach `snake_case`: z samym tym konstruktorem → `No suitable constructor was found`. Z niejawnym konstruktorem bezparametrowym (3.6 tworzy obiekt inicjalizatorem) EF materializuje encję przez niego, a `Id` (ignorowane) zostaje `default`. Wtedy `BlokadaObnizaniaProgu`/`KazdaPoprawkaDoWeryfikacji` liczą się dla `Typ = Trafnosc` i pustej konfiguracji — błąd cichy. Działa wariant z cieniami o nazwach PascalCase (`KonfiguracjaModeluId`, `RodzajZadania`, `Typ`) + `HasColumnName(…)` (sprawdzone).
  4. `.Include("_aktywne", e => …)` nie istnieje. Działa `Include(s => EF.Property<List<ElementSciezki>>(s, "_aktywne").Where(…))` (sprawdzone).
  5. Podjęcie, przedłużenie i zwolnienie Weryfikacji: „`ExecuteUpdateAsync` … jedno polecenie `UPDATE … RETURNING`, metody agregatu wykonywane na wierszu zwróconym przez `RETURNING`”. `ExecuteUpdateAsync` zwraca liczbę wierszy, bez `RETURNING`. Potrzebne `SqlQuery<T>`/`FromSql` z `UPDATE … RETURNING` (niekomponowalne, p. 6) albo `UPDATE` + odczyt. Brakuje też reguły odświeżenia śledzonej encji `Weryfikacja` po poleceniu SQL w tej samej jednostce pracy (inaczej `SaveChanges` wyśle nieaktualny `version`).
  6. `Database.SqlQuery<T>($"INSERT … RETURNING …")` (ADR-056 p. 2) działa tylko bez kompozycji (`ToListAsync`). `FirstOrDefaultAsync`/`SingleAsync` → `InvalidOperationException: 'FromSql' or 'SqlQuery' was called with non-composable SQL`. Kontrakt `ZlecaniePracSql` tego nie zapisuje.
  7. Nazwy kolumn z konwencji nie zgadzają się z DDL (skrypt + `GenerateCreateScript`):
     - `PracaWTle.MaksProb` → `maks_prob`, a DDL ma `max_prob` (dodatkowo `max` łamie ADR-049);
     - `Uczen.OswiadczenieWieku16` → `oswiadczenie_wieku16`, a DDL ma `oswiadczenie_wieku_16`, bez `HasColumnName` w tabeli pokrycia.

     Rekord `PracaWTle` (13.4.1) nie ma też `Status`, `DostepnaOd`, `Priorytet`, `OstatniBlad`, potrzebnych panelowi `/admin/praca-w-tle`.
  8. `WersjaAgregatuInterceptor` zwiększa `Version` agregatu w stanie `Modified`. Zmiana wyłącznie encji wewnętrznych (`OwnsMany`/`HasMany`: `opanowanie_umiejetnosci`, `ocena_kryterium`, `zdjecie_rozwiazania`, `kryterium_oceniania`) zostawia korzeń `Unchanged`, więc token współbieżności nie chroni niezmienników agregatu. Dla `ProfilUcznia` chroni go `FOR UPDATE` z 13.3, ale reguła ogólna jest błędna.
- **Oczekiwana poprawka:**
  - Wspólny typ identyfikatora dla kont: `UczenId`/`NauczycielId` jako `UzytkownikId` albo FK przez właściwość cienia `uzytkownik_id` typu `UzytkownikId`.
  - `_opanowanie` jako `List<OpanowanieUmiejetnosci>` ze słownikiem budowanym w pamięci.
  - Cienie PascalCase + `HasColumnName` dla klucza `StanBezpiecznika` i brak konstruktora bezparametrowego.
  - Poprawne składnie p. 4–6.
  - `HasColumnName` dla p. 7 albo zmiana DDL na `maks_prob`.
  - Interceptor oznaczający korzeń jako `Modified`, gdy zmienia się dowolna encja jego agregatu (np. przez `IAgregatWlasciciel`).
  - Test `ModelEfBudujeSieIZgadzaZKatalogiem` (budowa `IModel` + porównanie z katalogiem, C7-05) uruchamiany przy każdym PR.
- **Status:** ZAMKNIĘTA (iteracja 8). `ef-core.md` 4, 6.1, 6.4, 6.5, 7; DDL `maks_prob`, FK kont `DEFERRABLE INITIALLY DEFERRED` (potwierdzone w `pg_constraint`). Sprawdzone na PostgreSQL 16 i EF Core 10.0.3: `OwnsMany` z `List<record>` i podmianą elementu przez `with` zapisuje zmianę `theta`. Pozostałość redakcyjna (szkielet 3.6 nadal ma `Dictionary`) przechodzi do C8-12.

### C7-07: Implementacje Mock nie są wystarczające do deterministycznych testów E2E, a domyślny przepływ offline nie dochodzi do Oceny automatycznej
- **Waga:** WYSOKA
- **Lokalizacja:** 10.0.6; `prompts.md` 4.1 (wiadomość `user` rozpoznania), 6 (klucz pliku = 16 znaków SHA-256 tekstu wiadomości `user`, fallback `domyslna.json`, potem generator ze schematu); 10.1.8 (Q-RE-01), `prompts.md` 3.3; 11.2 (Pewność = minimum prawdopodobieństw), próg migracji danych 0,95
- **Problem:**
  1. **Dobór scenariusza.**
     - Wiadomość `user` rozpoznania zawiera wyłącznie `tresc_zadania_md`, `tekst_alternatywny_rysunkow` i `liczba_zdjec`. Skrót nie zależy od zdjęć, więc każda para (Zadanie, liczba zdjęć) ma w Mock **jedną** Transkrypcję. Testy E2E różnych Rozwiązań tego samego Zadania (poprawne / z błędem / z podejrzeniem błędu odczytu / `NIECZYTELNE`) są niemożliwe bez osobnych Zadań, a ograniczenie nie jest zapisane.
     - Każda zmiana serializacji, promptu, normalizacji SymPy albo formatu stanu zmienia skrót. Brak pliku przechodzi **po cichu** w `domyslna.json` albo w generator, więc test może przejść na innych danych niż zamierzone.
  2. **Generator ze schematu jest nieokreślony i niebezpieczny dla przepływu.**
     - Reguła „`boolean` = `true`” ustawia `przekroczono_limit_krokow = true`, co według `prompts.md` 4.1 daje `BLAD_ROZPOZNAWANIA` (`ZBYT_WIELE_KROKOW`). Domyślne rozpoznanie offline kończy się więc błędem, wbrew 10.0.6 („cały przepływ … działa offline”).
     - „Środek przedziału zaokrąglony w dół” jest nieokreślony dla pól bez `maximum` (`Ramka`: tylko `minimum: 0`), dla map `additionalProperties` (`rozklad`, `stwierdzenia`), typów unii `["object","null"]` i `pattern` (`umiejetnosci_dodatkowe`, `kod`).
     - Dla `zgodnosc_z_zadaniem` w [0, 1] daje 0 albo 0,5, czyli baner E-07 przy każdym przesłaniu.
  3. **`MockModelDecyzyjny`** daje masę 0,90 na losowej opcji. Przy kalibratorze tożsamościowym i progu 0,95 z migracji danych Pewność oceny = 0,90 < 0,95, więc **każda** Ocena offline idzie do Weryfikacji, z losowymi punktami i losowym `Q-OC-06`. Trasy automatycznej (FR-23), Kontroli losowej i Wskazówki po Ocenie ostatecznej nie da się przetestować bez plików rozkładów kluczowanych skrótem stanu, który zmienia się przy każdej zmianie formatu `stan-oceny-v1`.
  4. Stan Q-RE-01 zawiera `dni_do_egzaminu` i „dni temu”, więc ziarno `MockModelDecyzyjny` zależy od daty systemowej. Rekomendacje E2E zmieniają się z dnia na dzień, bo proces `api` w E2E nie ma `FakeTimeProvider`.
- **Oczekiwana poprawka:**
  - Jawny selektor scenariusza Mock niezależny od treści, np. pole `mock_scenariusz` w danych Zadania testowego albo nagłówek `X-Lemat-Mock-Scenariusz` przekazywany do kontekstu pracy, tylko poza `Production`.
  - Tryb ścisły w E2E: brak pliku scenariusza = błąd testu, nie fallback.
  - Generator domyślny z wartościami per rola dla pól sterujących (`czytelne = true`, `przekroczono_limit_krokow = false`, `zgodnosc_z_zadaniem = 0.95`, ramki = całe zdjęcie) i zdefiniowanym zachowaniem dla map, unii i `pattern`.
  - `MockModelDecyzyjny` z domyślną masą ≥ 0,99 na opcji „zgodnej z rozwiązaniem wzorcowym” (np. „tak” dla Q-OC-01, `BRAK_BLEDU`, maks. punkty), żeby domyślny przepływ szedł trasą automatyczną.
  - Czas `api`/`worker` w E2E ustawiany przez `LEMAT_CZAS_TESTOWY` (`TimeProvider` z przesunięciem).
  - Macierz scenariuszy E2E (trasa automatyczna, każdy powód Weryfikacji, T-6, T-7, błąd przejściowy, timeout) z przypisanymi plikami danych.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-065: nagłówek `X-Lemat-Mock-Scenariusz` (tylko `Development`/`E2E`) w ładunku prac, tryb ścisły, odpowiedzi domyślne trasy automatycznej (masa 0,995), `PrzesunietyTimeProvider`, macierz 15 scenariuszy.

### C7-08: Model zapasowy dla Walidacji Zadania (Q-WA) nie ma promptu ani stanu, a `ZapasowyLlmAdapter` nie zna kontekstu decyzji
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.0.4 (Walidacja: „Q-WA przez ten sam circuit breaker”); 10.2.5; 13.2 `ZapasowyLlmAdapter.WykonajPlanAsync(string stan, PlanPytan plan, TerminPlanu termin, CancellationToken ct)` (bez `KontekstDecyzji`); `prompts.md` 2 (jedna wersja `plan-pytan-v1`), 4.8 („Jesteś modelem decyzyjnym systemu oceniania”, wymagane pole `Q-OC-04`, wiadomość `"[STAN]\n" + stan-oceny-v1`), 3.2 (`stan-walidacji-v1`); 6.4 (pakiety `Lemat.Modele` bez `JsonSchema.Net`)
- **Problem:**
  - `IModelDecyzyjny.WykonajPlanAsync` przyjmuje `KontekstDecyzji`, ale `CircuitBreakerModeluDecyzyjnego` przekazuje do `ZapasowyLlmAdapter` wywołanie bez niego.
  - Jedyny prompt zapasowy opisuje Ocenę Rozwiązania (stan oceny, Q-OC-04, zasady CKE punktowania ucznia). Dla planu Q-WA-01…05 ze stanem `stan-walidacji-v1` model dostaje sprzeczne instrukcje, a wymagane pole `Q-OC-04` nie ma sensu.
  - Walidacja przy otwartym obwodzie (NFR-07) albo publikuje Zadania na podstawie odpowiedzi do niewłaściwego promptu, albo kończy się `BladModeluTrwalyException` → `ODRZUCONE_W_WALIDACJI` dla każdego Zadania.
  - `ZapasowyLlmAdapter` (`Lemat.Modele`) potrzebuje walidacji schematu i jednej próby naprawy, czyli logiki `WywolanieCzatu` z `Lemat.Agenci`, do której nie ma dostępu (6.5). Nie wiadomo, czy logika jest zduplikowana, ani skąd `Lemat.Modele` ma `JsonSchema.Net`.
- **Oczekiwana poprawka:**
  - `KontekstDecyzji` w `ZapasowyLlmAdapter`.
  - Osobny prompt `plan-pytan-walidacji-v1` (bez Q-OC-04) albo wersja promptu wybierana per kontekst z `konfiguracja_modelu`.
  - Test „Walidacja przy obwodzie otwartym”.
  - Port `IWywolanieUstrukturyzowane` (`Lemat.Application.Modele`) z implementacją w jednym miejscu albo przeniesienie `WywolanieCzatu` do `Lemat.Modele` (i dopisanie `JsonSchema.Net` do pakietów).
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-058 uzup. 3.2 p. 5: `KontekstDecyzji` w `ZapasowyLlmAdapter`, prompt `plan-pytan-walidacji-v1`, port `IWywolanieUstrukturyzowane` z jedną implementacją w `Lemat.Modele`.

### C7-09: Schematy odpowiedzi — dwa źródła prawdy, test zgodności niewykonalny literalnie, a mapy rozkładów są sprzeczne z „trybem ścisłym”
- **Waga:** ŚREDNIA
- **Lokalizacja:** `prompts.md` 1 p. 3 („wszystkie właściwości wymagane, `additionalProperties: false` … zgodność z trybem ścisłym dostawców”), 4.4 (`stwierdzenia`: `additionalProperties: {boolean}`), 4.8 (`rozklad`: `additionalProperties: {number}`); 6.6 („test porównuje zapisane pliki z wygenerowanymi i ze schematami w `docs/spec/prompts.md`”); 10.0.3 (`AIJsonUtilities.CreateJsonSchema(typeof(T))`)
- **Problem:**
  1. Wygenerowano schemat `AIJsonUtilities.CreateJsonSchema` dla rekordu z `[StringLength]`, `[Range]`, `[MaxLength]`, `[RegularExpression]`, nullowalnym enumem i słownikiem. Wynik:
     - zawiera `minLength`/`maxLength`/`minimum`/`maximum`/`maxItems`/`pattern` i komplet `required`;
     - **nie zawiera** `additionalProperties: false`, `$schema`, `title` ani `$defs`/`$ref` (typy powtarzalne są rozwijane inline).

     Schematy z `prompts.md` (`$defs.Ramka`, `$defs.ObszarNaZdjeciu`, `title`) nie będą więc równe wygenerowanym, a test „dryfu” musi porównywać semantycznie i użyć `AIJsonSchemaCreateOptions.TransformOptions { DisallowAdditionalProperties = true, RequireAllProperties = true }` — dokument tego nie określa. Rekordy C# z atrybutami ograniczeń nie są nigdzie pokazane.
  2. `rozklad` i `stwierdzenia` jako mapy dynamiczne (`additionalProperties: {…}`) są niezgodne z trybem ścisłym (strict), który wymaga `additionalProperties: false` na każdym obiekcie. Albo `prompts.md` 1 p. 3 jest nieprawdziwe dla modelu zapasowego i W-3, albo dostawca odrzuci schemat (`400` → `BladModeluTrwalyException` → przy otwartym obwodzie każda Ocena kończy się T-6). Nie wiadomo też, czy `ForJsonSchema` w adapterze OpenAI jest wysyłane jako `strict: true`.
- **Oczekiwana poprawka:**
  - Jedno źródło: schematy wyłącznie generowane z rekordów (z `TransformOptions`), a `prompts.md` jako wygenerowany artefakt; albo schematy ręczne jako zasoby i rekordy bez generacji.
  - Mapy zastąpić tablicami `[{opcja, p}]` (zgodne z trybem ścisłym), z walidacją kompletu opcji w kodzie.
  - Jawne ustawienie trybu ścisłego per dostawca i test kontraktu na schemacie każdej roli.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-058 uzup. 3.2 p. 6: schematy generowane z `TransformOptions` i zapisane jako migawki, mapy zamienione na tablice par, `strict = true`, test kontraktu WireMock.

### C7-10: Równoległe wywołania w agentach (`Task.WhenAll`) a `DbContext` o zasięgu `Scoped`
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.2.5 („W-2, W-3 i plan Q-WA … równolegle (`Task.WhenAll`)”; W-9 `similarity` „przez port”); 10.0.7 (agenci `Scoped`); ADR-058 p. 1 (`DlaRoliAsync` — aktywna Konfiguracja modelu z bazy, pamięć podręczna 30 s); 10.3.4 (równoległe pobranie zdjęć)
- **Problem:** w `WalidujAsync` równolegle działają:
  - W-3 (`DlaRoliAsync(GENERATYWNY_ROZWIAZUJACY_NIEZALEZNIE)` — odczyt `konfiguracja_modelu` po wygaśnięciu pamięci podręcznej);
  - plan Q-WA (przy przejściu na model zapasowy `DlaRoliAsync(DECYZYJNY_ZAPASOWY)`);
  - W-9 (zapytanie `pg_trgm` przez port).

  Jeśli te porty używają `LematDbContext` z zakresu pracy (jedyny kontekst opisany w `ef-core.md`), drugie równoległe zapytanie kończy się `InvalidOperationException: A second operation was started on this context instance`, co daje losowe błędy Walidacji zależne od czasu wygaśnięcia pamięci podręcznej. Dokument nie określa czasu życia `FabrykaKlientowCzatu` i źródła jej odczytów ani reguły „porty używane współbieżnie nie korzystają z `DbContext` zakresu”.
- **Oczekiwana poprawka:** odczyty portów wywoływanych przez agentów przez `IDbContextFactory<LematDbContext>` albo `NpgsqlDataSource` (nowe połączenie na zapytanie); `FabrykaKlientowCzatu` jako `Singleton` z pamięcią podręczną odświeżaną poza ścieżką żądania; test architektury „typy `Lemat.Agenci` i porty wywoływane z agentów nie zależą od `LematDbContext`” i test współbieżny Walidacji.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-058 uzup. 3.2 p. 7: `PamiecKonfiguracjiModeli` jako Singleton, porty agentów przez `NpgsqlDataSource`/`IDbContextFactory`, test architektury i test współbieżny (C-51).

### C7-11: Wznawianie strumienia powiadomień po `Last-Event-ID` z UUIDv7 gubi zdarzenia
- **Waga:** ŚREDNIA
- **Lokalizacja:** ADR-057 p. 5 („`powiadomienie.id` (UUIDv7 … `Guid.CreateVersion7()`, monotoniczny w obrębie procesu); po `Last-Event-ID` serwer wysyła powiadomienia z `id > Last-Event-ID`”); 7.2
- **Problem:**
  1. `Guid.CreateVersion7()` w .NET ma 48 bitów czasu w ms i bity losowe, bez licznika, więc nie jest monotoniczny w obrębie milisekundy, także w jednym procesie.
  2. Powiadomienia tworzą 2 repliki `api` i 2 repliki `worker` z niezależnymi zegarami.
  3. `id` jest nadawane przed `COMMIT`. Powiadomienie Y z transakcji, która zaczęła się wcześniej (mniejszy `id`), a zatwierdziła się po wysłaniu X, ma `id < X`. Klient wznawiający z `Last-Event-ID = X` nigdy go nie dostanie.

  Dotyczy to m.in. `NOWA_WERYFIKACJA`, `OCENA_SKORYGOWANA` i `TRANSKRYPCJA_GOTOWA`.
- **Oczekiwana poprawka:** kursor wznowienia niezależny od kolejności nadawania `id`:
  - albo `created_at` z marginesem (np. ponowne wysłanie zdarzeń z ostatnich 30 s przed kursorem i deduplikacja po `id` w kliencie);
  - albo kolumna `nr_zdarzenia bigint` nadawana w triggerze `AFTER INSERT` z sekwencji połączonej z oknem bezpieczeństwa.

  Do tego test „dwa powiadomienia zatwierdzone w odwróconej kolejności nie giną po ponownym połączeniu”.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-064 p. 2–4: strumień powiadomień zaczyna każde połączenie migawką nieprzeczytanych i nie używa kursora UUIDv7; test odwróconej kolejności zatwierdzeń (C-49). Wada wznawiania strumieni stanu po skrócie jest nową uwagą C8-02.

### C7-12: Konfiguracje `mock` jako migracja danych warunkowana `ASPNETCORE_ENVIRONMENT` — ryzyko aktywnego Mock w produkcji i nieudanego startu
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.0.6 („migracja danych `Dane/0001_konfiguracje_mock.sql`, wyłącznie gdy `ASPNETCORE_ENVIRONMENT != Production`”); 7.7 p. 5 (`efbundle` w kontenerze `migracje`); 6.6 („start procesu z aktywną konfiguracją `mock` w `Production` kończy się błędem”)
- **Problem:**
  - `efbundle` w kontenerze `migracje` nie jest aplikacją ASP.NET Core. Zmienna `ASPNETCORE_ENVIRONMENT` nie musi być tam ustawiona (domyślnie brak ≠ `Production`), więc migracja wstawi aktywne konfiguracje `mock` dla 8 ról w produkcji.
  - Pierwsze wdrożenie kończy się wtedy odmową startu `api`/`worker` (6.6). Bez tego zabezpieczenia aktywny Mock oceniałby Rozwiązania.
  - Migracja warunkowa daje też w `__historia_migracji` wpis „zastosowana” niezależnie od tego, czy dane wstawiono, więc późniejsza zmiana środowiska jej nie powtórzy.
- **Oczekiwana poprawka:** dane Mock poza migracjami — osobne polecenie `Lemat.Narzedzia seed-dev` (uruchamiane przez `docker compose` dla `Development`) albo `HostedService` startowy aktywny wyłącznie przy `IHostEnvironment.IsDevelopment()`; test „migracje na pustej bazie nie tworzą konfiguracji `mock`”.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-065 p. 6: migracje bez danych `mock`, polecenie `seed-dev` z odmową w `Production`, test `MigracjeNaPustejBazieNieTworzaKonfiguracjiMock`.

### C7-13: Harmonogram nie nadrabia wystąpień pominiętych przy przełączeniu instancji, restarcie albo wdrożeniu
- **Waga:** ŚREDNIA
- **Lokalizacja:** 13.5 (`Cron.GetNextOccurrence(ostatnie, Strefa) ≤ teraz` — `ostatnie` w pamięci; przejęcie roli w ≤ 10 s; tabela cykli 02:00 / 03:00 / 04:00 / 04:30 / 05:00 / 1. dnia miesiąca)
- **Problem:**
  - Dokument nie określa, czym jest `ostatnie` po starcie albo przejęciu roli. Jeśli to czas startu, wystąpienia z okna przerwy przepadają: wdrożenie, restart kontenera albo utrata połączenia blokady (do 10 s, a przy awarii hosta dłużej).
  - Przykłady skutków:
    - pominięte `KALIBRUJ` 03:00 odkłada kalibrację i bezpiecznik o dobę;
    - `USUN_KONTO` 02:00 przesuwa usunięcia RODO o dobę;
    - `UTRZYMAJ_PARTYCJE` 1. dnia miesiąca przesuwa utworzenie partycji o miesiąc (bufor 3 miesiące maleje bez alertu).
  - Deduplikacja chroni przed duplikatami tylko wśród `OCZEKUJE`/`W_TOKU`, więc nie zastąpi nadrabiania.
- **Oczekiwana poprawka:** trwały znacznik ostatniego wykonania per wpis (tabela `przebieg_harmonogramu(nazwa, ostatnie_wystapienie)` aktualizowana w jednostce pracy zlecenia) i nadrabianie wystąpień z ostatnich 24 h przy starcie albo przejęciu roli (warunki wykonania 13.3 są idempotentne); test „przejęcie roli o 03:00:05 zleca `KALIBRUJ` dla bieżącej daty”.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-066 p. 5: tabela `przebieg_harmonogramu` (jest w katalogu po załadowaniu DDL), nadrabianie z 24 h zapisywane w tej samej transakcji, klucze z datą wystąpienia, test (l) (C-53).

### C7-14: Szablony SK kodują wartości zmiennych HTML-em — założenie ADR-058 p. 4 nie jest sprawdzane dla nazw z bazy referencyjnej
- **Waga:** NISKA
- **Lokalizacja:** ADR-058 p. 4 (zmienne: `int`, `decimal`, enumy, „nazwy Umiejętności i Działów z bazy referencyjnej”); `prompts.md` 4.3, 4.5, 4.6 (`{{$umiejetnosc}}`, `{{$dzial}}`); test `SzablonBezZmiennychTresci` (sprawdza tylko typy)
- **Problem:** `KernelPromptTemplateFactory` 1.81 renderuje `x<2 & y>3 "q"` jako `x&lt;2 &amp; y&gt;3 &quot;q&quot;` (sprawdzone). Nazwa Umiejętności albo Działu z cudzysłowem, `<`, `>` lub `&` (np. „Nierówności typu „f(x) < g(x)”” albo nazwy z LaTeX) trafi do promptu systemowego w postaci zakodowanej, a test tego nie wykryje.
- **Oczekiwana poprawka:** `AllowDangerouslySetContent = true` w `PromptTemplateConfig` (bezpieczne, bo zmienne są wyłącznie systemowe), albo CHECK/test w danych referencyjnych zabraniający tych znaków w nazwach; test renderowania każdego szablonu dla wszystkich nazw z `umiejetnosci.yaml`.
- **Status:** ZAMKNIĘTA (iteracja 8). ADR-058 uzup. 3.2 p. 8: `new KernelPromptTemplateFactory { AllowDangerouslySetContent = true }` (pomiar architekta: flaga w `PromptTemplateConfig` nie działa) i test `RenderowanieNazwUmiejetnosci`.

### C7-15: Drobne niespójności wersji 3.1
- **Waga:** NISKA
- **Lokalizacja:** 7.3, 7.6, 7.7 p. 6, `ef-core.md` 3.2, 3.3, 6.2, 7; `ddl.md` blok B `0005_uprawnienia.sql`; 3.6/3.5; ADR-058 p. 1; 13.1; 10.0.3
- **Problem:**
  1. Dokument podaje „42 typy wyliczeniowe” (7.3, 7.6, 7.7, `ef-core.md` 3.2: „42 = 42”), a DDL i lista `MapowanieTypowWyliczeniowych` mają **43** (zgodne ze sobą nazwa po nazwie).
  2. 31 enumów C# z 3.6/10/13 zgadza się z DDL wartość po wartości. 12 typów z listy mapowania nie ma jednak definicji C# w dokumencie, w tym `RolaModelu` — kluczowa dla ADR-058: `StatusWkladki`, `RolaModelu`, `ZrodloProbki`, `ZrodloKalibratora`, `StatusZlecenia`, `StatusImportu`, `StatusTestu`, `DecyzjaPrzegladu`, `RezultatWalidacji`, `StatusZgloszeniaZadania`, `TypZdarzeniaRodo`, `ZrodloZdarzeniaRodo`.
  3. Triggery w modelu EF:
     - `HasTrigger("trg_suma_kryteriow")` jest na tabeli `zadanie_wersja`, a trigger jest na `kryterium_oceniania`;
     - 6 triggerów nie ma `HasTrigger` (`trg_umiejetnosc_glowna_w_dziale`, `trg_praca_w_tle_powiadom`, `trg_powiadomienie_powiadom`, `trg_import_arkusza_powiadom`, `trg_zlecenie_generowania_powiadom`, `trg_konfiguracja_modelu_powiadom`), choć 7.6 deklaruje triggery w modelu „dokumentacja w modelu, testy”.
  4. `0005_uprawnienia.sql` nadaje uprawnienia rolom, których DDL nie tworzy. Na czystej bazie (Testcontainers w `SchematZgodnyZDdlTest`) migracja kończy się błędem `role "lemat_api" does not exist`. Brakuje kroku tworzenia ról albo warunku.
  5. Kolizje nazw w `Lemat.Agenci` (sprawdzone kompilacją): `FunctionCallContent` (SK i MEAI) i `Kernel` (SK i `ImageMagick` w sidecarze) są niejednoznaczne. Potrzebna reguła aliasów `using` (ADR-049 p. 11 dotyczy tylko przestrzeni `Lemat.*`).
  6. Wersje pakietów:
     - `Azure.AI.OpenAI` 2.1.0 (najnowsza stabilna) jest zbudowana na `OpenAI` 2.1.0, a `Microsoft.Extensions.AI.OpenAI` 10.11 wymusza `OpenAI` 2.14.0. Konstrukcja klienta działa, ale potrzebny jest test kontraktu wywołania na WireMock.
     - Metapakiet `Microsoft.SemanticKernel` wciąga `Azure.AI.OpenAI` 2.9.0-beta.1 (NU1605 przy jawnej 2.1.0), więc w `Directory.Packages.props` dopuszczalne są wyłącznie `Microsoft.SemanticKernel.Core` i `.Yaml` — warto to zapisać w 6.4.
  7. `FabrykaKlientowCzatu` dla `DECYZYJNY_ZAPASOWY` musi zbudować klienta z `parametry.dostawca_llm`, a nie z `dostawca = zapasowy-llm`. Rejestr adapterów 13.1 tego nie opisuje, a `KlientCzatuRoli.Dostawca` będzie wtedy niejednoznaczny w telemetrii (`gen_ai.system`).
  8. Tekst 10.0.3 („bez zamiany na <”) i `ef-core.md` 3.3 („zapisuje `<`, `>`, `&` jako `<`, `>`, `&`”) zgubił sekwencje `<` itd. Uzasadnienie `UnsafeRelaxedJsonEscaping` dla `jsonb` jest też nieścisłe: `jsonb` dekoduje sekwencje `\uXXXX` przy zapisie, więc równość bajtowa dotyczy tekstu wiadomości do modelu i kolumn `text`, a nie zawartości `jsonb`.
- **Oczekiwana poprawka:** p. 1 — 43; p. 2 — definicje enumów w 3.6/13 albo odnośnik do generatora; p. 3 — `HasTrigger` na właściwych tabelach dla wszystkich 15 triggerów; p. 4 — `CREATE ROLE … IF NOT EXISTS` (blok `DO`) albo tworzenie ról poza migracją z warunkiem; p. 5 — reguła aliasów w ADR-049; p. 6 — zapis w 6.4 i test kontraktu; p. 7 — opis w 13.1; p. 8 — poprawka tekstu.
- **Status:** ZAMKNIĘTA (iteracja 8). p. 1: 43 typy (potwierdzone w katalogu). p. 2: enumy w `ef-core.md` 3.2. p. 3: `KatalogTriggerow` z 15 pozycjami (potwierdzone w `pg_trigger`). p. 4: role tworzone blokiem `DO` (potwierdzone). p. 5–7: ADR-049 p. 13, 6.4, 13.1. p. 8: poprawione w 10.0.3 i `ef-core.md` 3.3, ale ten sam błąd wrócił w `api.md` 1 (C8-12).

### Podsumowanie iteracji 7

Weryfikacja uwag otwartych po iteracji 6:

| Stan | Liczba | Identyfikatory |
|---|---|---|
| ZAMKNIĘTA | 15 | C6-01 … C6-15 (C6-10 w p. 1, 3, 4; p. 2 kontynuowany w C7-01) |
| OTWARTA | 0 | — |

Nowe uwagi iteracji 7:

| Waga | Liczba uwag | Identyfikatory |
|---|---|---|
| KRYTYCZNA | 0 | — |
| WYSOKA | 6 | C7-01, C7-03, C7-04, C7-05, C7-06, C7-07 |
| ŚREDNIA | 7 | C7-02, C7-08, C7-09, C7-10, C7-11, C7-12, C7-13 |
| NISKA | 2 | C7-14, C7-15 |
| **Razem** | **15** | |

Werdykt: wersja 3.1 zamyka wszystkie 15 uwag iteracji 6.

Co działa (sprawdzone na PostgreSQL 16 i w projekcie .NET 10):
- DDL ładuje się bez błędów (poza rolami w `0005`); nazwy tabel, kolumn, indeksów i triggerów użyte w `ef-core.md` istnieją w katalogu; 31 enumów C# zgadza się z DDL wartość po wartości;
- `INSERT … ON CONFLICT` z predykatami indeksów częściowych i `DO UPDATE` ładunku działa;
- Magick.NET 15 ma wszystkie użyte API, a polityka bezpieczeństwa w kodzie odrzuca SVG/TXT;
- port per rola (ADR-058), jednostka pracy z jawną transakcją (ADR-056) i budżet połączeń (ADR-059) są spójne.

Dokument nie jest jednak na poziomie produkcyjnym:
- limit wywołań funkcji i nazwy narzędzi Agenta generującego nie działają tak, jak opisano, a potok `ChatClientBuilder` w zapisanej postaci się nie kompiluje i nie emituje spanów narzędzi (C7-01, C7-02);
- mechanika prac w tle ma dwa błędy wykonania: konflikt indeksu deduplikacji blokuje odzyskiwanie wszystkich dzierżaw, a pętla puli zamyka przekroczenia czasu jako sukces i zatrzymuje proces przy pierwszym nieobsłużonym wyjątku (C7-03, C7-04);
- warstwa danych nie przejdzie własnych testów: migracja z modelu EF i DDL dają różne schematy, a cztery konfiguracje EF nie budują modelu (C7-05, C7-06);
- Mock nie pozwala deterministycznie sterować scenariuszami E2E, a domyślny przepływ offline kończy się błędem rozpoznania albo Weryfikacją (C7-07).

Poprawki C7-01 … C7-07 są lokalne. Wymagają zmiany:
- konfiguracji `FunctionInvokingChatClient` i nazewnictwa narzędzi;
- dwóch poleceń SQL i bloku `try/catch` w 13.4;
- wyboru jednego źródła schematu;
- typów identyfikatorów kont i kolekcji `ProfilUcznia`;
- selektora scenariuszy Mock.

Iteracja 3 (sekcje 12, 14, 15) powinna je uwzględnić razem z mapowaniem macierzy błędów.

---

## Iteracja 8 (v3.2 — kontrakty REST/SSE i błędy)

Data przeglądu: 2026-11-01. Wersja dokumentu: 3.2 (iteracja 3 pętli „.NET 10 + Next.js”). Zakres przeglądu:
- sekcje 12, 14 i 15 (przepisane);
- `docs/spec/api.md` (nowy), `ddl.md`, `ef-core.md`, `prompts.md`;
- ADR-060 … ADR-066 i mapowanie C7 w `ARCHITECTURE_CHANGELOG.md`.

**Weryfikacja uwag iteracji 7.** Wszystkie 15 uwag (C7-01 … C7-15) są ZAMKNIĘTE. Uzasadnienia są wpisane przy uwagach. Dwie pozostałości redakcyjne, czyli szkielet `ProfilUcznia` w 3.6 i sekwencja `<` w `api.md` 1, przechodzą do C8-12.

**Metoda (niezależnie od tabel architekta, artefakty w scratchpadzie).**
1. Bloki A i B `ddl.md` załadowano do PostgreSQL 16 na czystej bazie, bez błędów. Katalog po załadowaniu:
   - 60 tabel + 4 partycje, 43 enumy, 15 triggerów;
   - 142 CHECK, 124 FK, 21 UNIQUE;
   - role `lemat_api`, `lemat_worker`, `lemat_scheduler`;
   - FK kont `DEFERRABLE`.

   Scenariusze C-47/C-48 odtworzono SQL-em na `ponow_prace` i `odzyskaj_dzierzawy`.
2. Projekt ASP.NET Core 10.0.14 (`Microsoft.AspNetCore.OpenApi` 10.0.14) uruchomiono z opcjami `JsonApi.Konfiguruj` z `api.md` 1. Sprawdzono:
   - nazwy JSON pól z cyframi;
   - polimorficzne `WyborOdpowiedzi`, nieznane pole i `PATCH` z `null`;
   - wiązanie `[FromForm]` rekordu `RozwiazanieWejscie` w postaci z `api.md` 3 i w trzech wariantach;
   - schemat OpenAPI;
   - `TypedResults.ServerSentEvents` z wyjątkiem przed pierwszym zdarzeniem.
3. `FunctionInvokingChatClient` (MEAI 10.11, SK 1.81) i `OwnsMany` EF Core 10.0.3 sprawdzono na PostgreSQL 16 (C7-01, C7-06).
4. Kod `klient.ts`, `odswiezanie.ts` i `strumien.ts` z `api.md` 6 przeanalizowano ścieżka po ścieżce wobec ADR-064 p. 2 i p. 8.
5. Pokrycie: każdy FR/NFR porównano z katalogiem 12.3, mapowaniem stron `api.md` 7 i macierzą 12.8.

**Co działa (potwierdzone):**
- wyjątek z iteratora SSE przed pierwszym `yield` daje `404 application/problem+json` (ADR-064 p. 6);
- `JsonStringEnumMemberName("R-SERIA")` trafia do `enum` w OpenAPI;
- unia `WyborOdpowiedzi` daje `anyOf` z `discriminator`;
- limit wywołań funkcji działa zgodnie z ADR-058 uzup. 3.2 p. 1;
- funkcje prac w tle są odporne na konflikt deduplikacji;
- pokrycie FR-01 … FR-36 endpointami jest kompletne co do istnienia operacji (wyjątki: C8-07, C8-11);
- macierz E-01 … E-47 jest kompletna co do numeracji.

### C8-01: `POST /rozwiazania` (multipart) nie wiąże się w zapisanej postaci, a nazwy pól z OpenAPI nie trafiają do rekordu (sprawdzone uruchomieniem)
- **Waga:** WYSOKA
- **Lokalizacja:** `api.md` 3 (`public sealed record RozwiazanieWejscie(Guid ZadanieWersjaId, Guid? ElementSciezkiId, Guid? PrzypisanieId, IFormFileCollection Zdjecia)`), 4 (`[FromForm] RozwiazanieWejscie wejscie`, `.Accepts<RozwiazanieWejscie>("multipart/form-data")`); 12.3 `POST /rozwiazania`, `POST /rozwiazania/{id}/zdjecia`, `POST /admin/arkusze-cke/importy`; 12.4 `RozwiazanieWejscie`; 12.12 (`kontrakt-api.spec.ts`); ADR-061 p. 2
- **Problem (ASP.NET Core 10.0.14, `curl -F`):**
  1. Rekord z konstruktorem pozycyjnym i polem `IFormFileCollection` (postać z `api.md`) odrzuca **każde** żądanie kodem `400` z pustym ciałem. Log: `FormDataMappingException: An error occurred while trying to map a value from form data`. Dotyczy nazw pól w obu konwencjach. Wariant bez wartości domyślnych `Guid? … = null` kończy się `BadHttpRequestException: Missing required value for constructor parameter 'ElementSciezkiId'`, a reguła 12.5 („dokładnie jeden kontekst”) wymaga, by jedno z pól było nieobecne.
  2. Binder formularza dopasowuje pola po nazwach CLR (`ZadanieWersjaId`), a nie po `JsonNamingPolicy`. Schemat OpenAPI rekordu formularza (to, co trafi do `schema.d.ts`) ma jednak nazwy `zadanie_wersja_id`, `element_sciezki_id`, `przypisanie_id`. W wariancie klasy z setterami pola z nazwami ze schematu wiążą się **po cichu** jako `Guid.Empty`/`null`, a przy nazwach PascalCase wartości są poprawne. Klient generowany z OpenAPI wysłałby więc Rozwiązanie bez Zadania i kontekstu. Skutek: `422` albo, przy słabszej walidacji, `404`/`409` mylące Ucznia.
  3. Przypadek działający bez zastrzeżeń to parametry `[FromForm(Name = "zadanie_wersja_id")] Guid`, `[FromForm(Name = "element_sciezki_id")] Guid?` i `IFormFileCollection zdjecia`, ale wtedy OpenAPI nie zawiera rekordu `RozwiazanieWejscie`, do którego odwołują się 12.3 i 12.4.

  FR-16, czyli główny przepływ systemu, nie przejdzie więc testu `kontrakt-api.spec.ts`. To samo ryzyko dotyczy `POST /rozwiazania/{id}/zdjecia` i importu ZIP. Dokument nie zawiera żadnego testu wiązania multipart przez klienta wygenerowanego z OpenAPI.
- **Oczekiwana poprawka:**
  - Ustalić kompilowalną i działającą postać wiązania: parametry z `[FromForm(Name = "…")]` albo klasa z `[FromForm(Name = "…")]` na każdej właściwości i wartościami domyślnymi dla pól opcjonalnych.
  - Transformator OpenAPI dający schemat `multipart/form-data` z nazwami `snake_case` i `zdjecia: string[] (format: binary)`.
  - Test `Lemat.Api.Tests/Rozwiazania/PrzeslanieMultipartZKlientaOpenApi`: formularz zbudowany według wygenerowanego schematu, oba warianty kontekstu i brak kontekstu → `422 WALIDACJA` z `bledy_pol`, a nie `400` z pustym ciałem.

### C8-02: Wznowienie strumienia stanu bez zmiany stanu nie wysyła pierwszego zdarzenia, więc klient przechodzi na odpytywanie po każdym `wznow`, a odpytywanie przy `nastepne_odpytanie_ms = null` kręci się bez przerwy
- **Waga:** WYSOKA
- **Lokalizacja:** ADR-064 p. 2 c („wysyła `stan`, chyba że skrót równa się `Last-Event-ID`”), p. 6, p. 8 („brak pierwszego zdarzenia w ciągu 5 s → odpytywanie”); `api.md` 5 (`if (skrot != poprzedniSkrot || terminalny)` przy `poprzedniSkrot = ostatnie`), 6.3 (`pierwszeZdarzenie = setTimeout(…abort("BUFOROWANIE"), 5_000)`, `porazki.length >= 2`, `przejdzNaOdpytywanie`, `setTimeout(r, o.okresOdpytywaniaMs())`, `if (r.status === 401 && (await odswiezToken())) return polacz();`); 12.10; 14.7 („Zegar urządzenia niezgodny”); E-42
- **Problem (analiza ścieżek kodu z `api.md` przy semantyce ADR-064):**
  1. Serwer zamyka każdy strumień `wznow` najpóźniej po 10 min albo przy `exp` tokenu (15 min). Klient łączy się natychmiast z `Last-Event-ID` = skrót ostatniego stanu. W typowych stanach długotrwałych stan się nie zmienił:
     - `TRANSKRYPCJA_GOTOWA` w trakcie edycji;
     - `OCZEKUJE_NA_WERYFIKACJE`, które dla strumienia nie jest terminalne, a trwa do 3 dni roboczych.

     Serwer nie wysyła więc nic do pierwszego `ping` po 15 s. Klient przerywa po 5 s (`BUFOROWANIE`), dopisuje porażkę i po ok. 1 s łączy się ponownie z tym samym skutkiem. Druga porażka w 10 s daje `przejdzNaOdpytywanie()`. Każde wznowienie kończy się więc trybem odpytywania na 60 s, a „brak pierwszego zdarzenia” myli poprawny serwer z proxy buforującym. Metryka E-42 i cel ADR-051 p. 6 (dostarczenie SSE p99 ≤ 300 ms) są nieosiągalne w długich sesjach.
  2. W trybie odpytywania `okresOdpytywaniaMs()` czyta `nastepne_odpytanie_ms`, które w stanach niezmiennych jest `null` (12.1). Kod wywołuje `setTimeout(r, null)`, czyli opóźnienie 0 ms. Pętla `GET /rozwiazania/{id}` przez 60 s ogranicza tylko czas odpowiedzi: przy 50 ms to ok. 1 200 żądań na kartę, ponad limit `NaUzytkownika` (20/s), więc pojawiają się `429` i toasty. Opis z `api.md` 6.3 („null → koniec odpytywania”) nie jest zaimplementowany.
  3. `401` → `odswiezToken()` → `polacz()` jest rekurencją bez limitu. 12.10 i ADR-064 p. 8 mówią o „jednym ponowieniu”.
  4. `pobierzToken()` porównuje `wygasa_at` z `Date.now()` bez `roznicaZegara`, choć 14.7 deklaruje przeliczenie względem nagłówka `Date`. Przy zegarze urządzenia +10 min każdy `pobierzToken()` odświeża token, a każde odświeżenie rotuje refresh token. Przy zegarze −20 min token jest używany po `exp`, więc każde żądanie przechodzi przez `401` i ponowienie.
- **Oczekiwana poprawka:**
  - Serwer zawsze wysyła pierwsze zdarzenie po połączeniu: `stan` niezależnie od `Last-Event-ID` albo `ping` natychmiast. Regułę 5 s liczyć od dowolnego zdarzenia.
  - Odpytywanie z `null` → zatrzymanie i powrót do SSE.
  - Licznik ponowień `401` (maks. 1).
  - `roznicaZegara` w `odswiezanie.ts`.
  - Testy Vitest `strumien.test.ts`: „wznow przy niezmienionym stanie nie przełącza na odpytywanie”, „null zatrzymuje odpytywanie”, „drugi 401 kończy strumień”.

### C8-03: Idempotencja — wiersz klucza bez odpowiedzi blokuje operację na 24 h, a zapis poza transakcją przypadku użycia dopuszcza podwójny skutek
- **Waga:** WYSOKA
- **Lokalizacja:** ADR-062 p. 3 („Filtr w osobnej krótkiej transakcji wstawia `klucz_idempotencji` … wykonuje endpoint, zapisuje `kod_statusu_http` i `cialo_http` (tylko `2xx` i `4xx` poza `409 IDEMPOTENCJA_W_TOKU`, `429`; dla `5xx` usuwa wiersz)”); `ddl.md` `klucz_idempotencji` (brak kolumny terminu „w toku”); 12.1; E-41, E-43, E-47 („klient ponawia z tym samym `Idempotency-Key`”); 14.4 („ponowienia automatyczne … `KONFLIKT_WSPOLBIEZNOSCI`”); 15.3 (C-01)
- **Problem:**
  1. **Zawieszony klucz.** Wiersz bez odpowiedzi oznacza „w toku” bez terminu. Zostaje na 24 h, gdy:
     - proces `api` kończy się w trakcie żądania (wdrożenie z `ShutdownTimeout` 30 s, OOM, restart kontenera);
     - zapis odpowiedzi albo `DELETE` po `5xx` się nie udaje.

     Drugi przypadek jest typowy, bo najczęstszym `5xx` jest `503 BAZA_NIEDOSTEPNA` i właśnie wtedy `DELETE` nie dojdzie do bazy. Każde ponowienie z tym kluczem dostaje `409 IDEMPOTENCJA_W_TOKU`, a klient zgodnie z E-41 „czeka i ponawia tym samym kluczem” bez końca. E-02 i E-36 („ponawia z tym samym `Idempotency-Key`”) dają tę samą pętlę dla `POST /rozwiazania` i `DELETE /uczniowie/ja`.
  2. **Podwójny skutek.** Klucz i odpowiedź są w innych transakcjach niż jednostka pracy przypadku użycia (ADR-056 p. 1). Ciąg zdarzeń: `COMMIT` przypadku użycia → błąd zapisu odpowiedzi → `5xx` → wiersz usunięty → ponowienie wykonuje operację drugi raz. Bez naturalnego indeksu unikalnego daje to duplikat:
     - `POST /klasy`, `/zestawy-zadan`, `/przypisania`, `/zadania`;
     - `/zadania/generowanie` (koszt modelu i limit 20/dobę);
     - `/admin/konfiguracje-modeli`, `/admin/progi-pewnosci`.
  3. **Zapisane odpowiedzi przejściowe.** Zapisywane są wszystkie `4xx` poza `409 IDEMPOTENCJA_W_TOKU` i `429`, więc także `409 KONFLIKT_WSPOLBIEZNOSCI` (E-47). Automatyczne ponowienie z tym samym kluczem (14.4, E-47) zwraca zapisane `409` z `Idempotent-Replayed: true` zamiast wykonać operację. Mechanizm z E-47 nie działa. 12.11 („klucz trzymany do odpowiedzi `2xx`/`4xx`”) i 14.4 się wykluczają.
- **Oczekiwana poprawka:**
  - Wstawienie klucza i zapis odpowiedzi w **tej samej** jednostce pracy co przypadek użycia (`IJednostkaPracy`, ADR-056), a wiersz „w toku” tylko jako krótka rezerwacja z kolumną `w_toku_do` (np. `now() + 60 s`). Po jej upływie ponowienie przejmuje klucz, bo skutek bez zapisanej odpowiedzi nie został zatwierdzony.
  - Lista kodów niezapisywanych: `409 KONFLIKT_WSPOLBIEZNOSCI`, `409 IDEMPOTENCJA_W_TOKU`, `429`, `5xx`.
  - Testy:
    - `KluczBezOdpowiedziPoAwariiProcesuJestPrzejmowany`;
    - `Konflikt40P01NieJestZapisywanyWKluczu`;
    - `BladZapisuOdpowiedziNieDajeDrugiegoSkutku` (wstrzyknięty wyjątek po `COMMIT`).

### C8-04: `ETag`/`If-None-Match → 304` dla zasobów złożonych zwraca nieaktualny stan (Wskazówka, Weryfikacja, blokada, podpisane URL)
- **Waga:** WYSOKA
- **Lokalizacja:** 12.1 („`If-None-Match` na `GET` → `304`”), ADR-062 p. 4 („oszczędność transferu w sieci komórkowej”); `api.md` 4 (`MapGet("/{id:guid}") … .Produces(304).ZwracaETag(ZasobWersjonowany.R)`); 12.3 `GET /weryfikacje/{id}` (`ETag` = wersja treści, ADR-037), `GET /zadania/{id}`; 15.2 (tokeny `ocena`, `transkrypcja` = `xmin` „bez ETag”, `weryfikacja.version` z triggera)
- **Problem:** `ETag` jest wersją **jednego** agregatu, a odpowiedź zawiera dane innych agregatów albo danych zależnych od czasu:
  - **`GET /rozwiazania/{id}` (`ETag` = `rozwiazanie.version`)** — `ocena` (Wskazówka dopisywana `UPDATE ocena`, `wskazowka_w_przygotowaniu`), `weryfikacja` (status `OCZEKUJE` → `W_TRAKCIE` → `ZAKONCZONA` bez zmiany Rozwiązania przy rozstrzygnięciu Kontroli losowej, `termin_at` przy przywróceniu konta, `opozniona` zależne od czasu), `zdjecia[].dostepne` (retencja), `transkrypcja.zgodnosc_poprawek`. W trybie zapasowym (odpytywanie co 1 s przy `wskazowka_w_przygotowaniu`) klient z `If-None-Match` dostaje `304` w nieskończoność: Wskazówka nigdy się nie pojawia, a odpytywanie się nie kończy.
  - **`GET /weryfikacje/{id}` (`ETag` = wersja treści)** — `status`, `podjeta_przez`, `zablokowana_do`, `dostepna` zmieniają się bez zmiany `version` (ADR-037, C-31), a `zdjecia[].url` wygasają po 10 min. `304` oddaje Nauczycielowi nieaktualną blokadę i martwe adresy zdjęć przy blokadzie trwającej do 2 h.
  - Ten sam silny `ETag` jest wysyłany rolom U i N, choć reprezentacje się różnią (12.6), co narusza RFC 9110 §8.8.3.
- **Oczekiwana poprawka:**
  - Rozdzielić dwa znaczenia: `If-Match` (współbieżność) = `version` agregatu, a `ETag` dla `If-None-Match` = skrót reprezentacji, ten sam co `id:` SSE (ADR-064 p. 2), obejmujący rolę.
  - Wersję dla `If-Match` przekazywać polem `version` w ciele, jak już robi UI w 12.11.
  - Albo zrezygnować z `304` dla zasobów złożonych.
  - Test `Etag304NieUkrywaZmianyOceny` (Wskazówka dopisana → `200` z nowym `ETag`).

### C8-05: Limity szybkości per IP i obsługa nieudanego odświeżenia odcinają klasy szkolne za NAT i wylogowują wszystkie karty przy chwilowej awarii
- **Waga:** WYSOKA
- **Lokalizacja:** 12.1 („5/min logowań na IP, 600/min na IP w Caddy”); ADR-062 p. 6 (`Logowanie` 5/min na IP); 12.7 (nieudane logowania 20/15 min na IP); A-03 („koniec lekcji: 90 przesłań w ciągu 60 s, 3 klasy”); `api.md` 6.2 (`if (!r.ok) { token = null; kanal.postMessage({ typ: "WYLOGOWANIE" }); … }`); ADR-057; E-40, E-42
- **Problem:**
  1. Uczniowie jednej szkoły wychodzą do internetu przez jeden adres IP (NAT). Klasa logująca się na początku lekcji (25–30 osób w 1–2 min) przekracza `Logowanie` 5/min na IP: 20–25 Uczniów dostaje `429` przez kilka minut. Polityka liczy wszystkie logowania, nie tylko nieudane.
  2. Caddy 600 żądań/min na IP przy scenariuszu A-03 (3 klasy, 90 Uczniów) daje ok. 6,7 żądania/min na Ucznia. Samo przesłanie, otwarcie SSE, `GET` po `koniec`, autozapis Poprawek (debounce 1,5 s), powiadomienia i podpisane adresy zdjęć to kilkanaście żądań na Ucznia. Sieci szkolne z proxy buforującym (E-42, cel testu `sse-zapasowe-odpytywanie.spec.ts`) przechodzą na odpytywanie co 1 s, czyli 60 żądań/min na Ucznia, i cała szkoła dostaje `429`.
  3. `odswiezToken()` traktuje **każdą** odpowiedź `!ok` jako koniec sesji: `429` (limit `NaUzytkownika` dla anonimowych po IP!), `503 BAZA_NIEDOSTEPNA`, `502` z Caddy przy wdrożeniu. Wylogowuje wtedy wszystkie karty (`WYLOGOWANIE`) i niszczy niezapisane szkice. Błąd sieci (`fetch` rzuca) zostawia natomiast `trwajace` w stanie odrzuconym bez obsługi w `pobierzToken`.
- **Oczekiwana poprawka:**
  - Limity per IP tylko dla żądań nieuwierzytelnionych i nieudanych, z kluczem (IP, e-mail).
  - Limit Caddy z kluczem po `sub` tokenu albo wyższy (np. 6 000/min) z uzasadnieniem z modelu A-03 i scenariuszem obciążenia „3 klasy za jednym NAT”.
  - Odświeżenie: `WYLOGOWANIE` wyłącznie przy `401 SESJA_UNIEWAZNIONA`; `429`/`5xx`/sieć → ponowienie z odstępem i `BladApi` bez wylogowania.
  - Test Playwright `klasa-za-nat.spec.ts` (30 logowań z jednego IP w 60 s).

### C8-06: Autoryzacja obiektowa niepełna — widoczność Zadania dla Ucznia nieokreślona, brak reguł dla kilku zasobów, rejestracja z dowolną rolą
- **Waga:** WYSOKA
- **Lokalizacja:** 12.3 `GET /zadania/{id}` (`Zalogowany`, „U → `ZadanieUczniaWyjscie`”), `GET /zlecenia-generowania/{id}` (bez warunku „zlecający”, choć strumień go ma), `POST /powiadomienia/{id}/przeczytanie`, `/testy-diagnostyczne/{id}/pozycje/{pozycja_testu_diagnostycznego_id}/…`, `/rozwiazania/{id}/zdjecia/{zdjecie_rozwiazania_id}/adres`, `/klasy/{id}/uczniowie/{uczen_id}/…`; 12.8; `api.md` 3 (`RejestracjaWejscie(… RolaUzytkownika Rola …)`), 4 (grupa `RequireAuthorization("Zalogowany")`, `GET /{id:guid}` bez własnej polityki); 12.4 (`rola: "UCZEN" | "NAUCZYCIEL"`)
- **Problem:**
  1. **`GET /zadania/{id}` dla Ucznia.** 12.8 określa tylko „Zadania (pełne): brak”. Nie ma reguły, **które** Zadania Uczeń może czytać w wersji uczniowskiej. Dosłowna implementacja zwraca każde Zadanie po identyfikatorze, w tym:
     - Zadania `PRYWATNY` innych Nauczycieli;
     - Zadania `WALIDACJA`, `ODRZUCONE_W_WALIDACJI`, `DO_PRZEGLADU`, `ODRZUCONE`;
     - pozycje Testu diagnostycznego przed jego rozpoczęciem.

     Identyfikatory trafiają do Uczniów przez Przypisania i Ścieżki nauki innych osób (UUIDv7 ujawnia czas utworzenia). Reguła powinna dopuszczać tylko Zadania z własnego elementu Ścieżki nauki, własnego Przypisania, własnego Rozwiązania lub Odpowiedzi i własnego Testu diagnostycznego, w wersji `zadanie_wersja_id` tego kontekstu.
  2. **Brak wierszy w 12.8** dla Powiadomień (właściciel), Zleceń generowania (zlecający albo A — `GET` i `/strumien` mają różne reguły), Testów diagnostycznych i ich pozycji, Wkładek dla N bez NR oraz Importów. Brak też reguły dla identyfikatorów zagnieżdżonych, która wymaga spójności z rodzicem:
     - `zdjecie_rozwiazania.rozwiazanie_id = {id}`;
     - `pozycja.test_diagnostyczny_id = {id}`;
     - `uczen_id` członkiem Klasy `{id}`.

     Bez tej reguły Nauczyciel z dostępem do dwóch Rozwiązań może łączyć identyfikatory dowolnie, a test „brak widoczności → 404” nie ma specyfikacji wejściowej.
  3. **Rejestracja z rolą `ADMINISTRATOR`.** Rekord C# ma typ `RolaUzytkownika` (z `ADMINISTRATOR`), więc wygenerowany `schema.d.ts` i OpenAPI dopuszczą tę wartość. Ograniczenie do `UCZEN | NAUCZYCIEL` istnieje tylko w „referencyjnym” listingu 12.4. 12.5 nie ma reguły walidatora, a przepływ 9.1 nie mówi, co się stanie z `rola = ADMINISTRATOR` (Nauczyciel → `OCZEKUJE_NA_ZATWIERDZENIE`, Administrator — nieokreślone). To potencjalna eskalacja uprawnień przez anonimowy endpoint.
  4. **Administrator.** 12.8 przyznaje Administratorowi odczyt wszystkich Rozwiązań, Ocen, Weryfikacji, Klas i Przypisań, ale:
     - polityki tych endpointów (`UczenLubNauczyciel`, `Nauczyciel`) go wykluczają, a osobnych endpointów `admin/*` nie ma;
     - przykład `api.md` 4 (grupa `Zalogowany`, `GET /{id:guid}` bez polityki) wpuszcza na `GET /rozwiazania/{id}` każdą rolę.

     Trzy źródła mówią trzy różne rzeczy, a test `KazdyEndpointMaPolitykeIKody` nie wie, które jest wzorcem.
- **Oczekiwana poprawka:**
  - Reguła widoczności `ZadanieUczniaWyjscie` w 12.8.
  - Kompletna macierz 12.8 dla każdego zasobu z 12.3 i reguła zagnieżdżonych identyfikatorów.
  - Osobny enum `RolaRejestracji { Uczen, Nauczyciel }` w `RejestracjaWejscie`.
  - Jedna decyzja o odczycie przez Administratora (endpointy `admin/*` z audytem albo usunięcie z 12.8).
  - Test `KazdyEndpointZIdMaTestIdor` (dla każdej ścieżki z `{id}`: zasób innego użytkownika tej samej roli → `404`).

### C8-07: Przepływy Administratora niewykonalne — Przekazanie Klasy wymaga `If-Match` zasobu, którego Administrator nie może odczytać; brak odczytu flagi „diagnostyczne” i zarządzania bazą Zadań
- **Waga:** WYSOKA
- **Lokalizacja:** 12.3 `POST /admin/klasy/{id}/przekazanie` (ETag `K`), `GET /klasy`, `GET /klasy/{id}` (`Nauczyciel`, „własne”), `PUT /admin/zadania/{id}/diagnostyczne` (ETag `Z`), `GET /zadania` (filtry bez `diagnostyczne`), `POST /zadania` (`Nauczyciel`); `api.md` 3 (`ZadaniePelneWyjscie` bez pola `diagnostyczne`, `PoprawkiZadania` bez `DzialId`/`Umiejetnosci`/`Rodzaj`), 7 (strona `admin/klasy` czyta wyłącznie `GET /admin/uzytkownicy`; `admin/zadania` = „jak w grupie Nauczyciela”, czyli `POST /zadania` i `POST /zadania/{id}/zgloszenia-bledu`, zakazane dla A); ADR-044; 16.4; FR-35, FR-36; wymagania „Administrator – zarządza … bazą zadań”
- **Problem:**
  1. Przekazanie Klasy jest warunkiem usunięcia konta Nauczyciela (ADR-044, NFR-09). Administrator nie ma jednak endpointu zwracającego Klasę, jej `version` ani listy Klas danego Nauczyciela, więc `If-Match: K` jest niemożliwe do spełnienia (`428`/`412`). Powiadomienie `KLASA_DO_PRZEKAZANIA` ma co najwyżej identyfikator. Przepływ 16.4 kończy się w martwym punkcie.
  2. Strona `admin/pula-diagnostyczna` nie może pokazać, które Zadania mają flagę `diagnostyczne`: pola nie ma w `ZadaniePelneWyjscie`, a `GET /zadania` nie ma takiego filtru. Administrator zmienia więc flagę na ślepo.
  3. Administrator „zarządza bazą zadań” (aktorzy, FR-36), ale:
     - nie może utworzyć Zadania (`POST /zadania` tylko `Nauczyciel`);
     - nie może poprawić otagowania: `PoprawkiZadania` nie obejmuje Działu, Umiejętności ani rodzaju, a import tego samego `kod` jest odrzucany (C-22).

     Błąd tagu w Zadaniu z arkusza nie ma więc ścieżki naprawy poza wycofaniem.
- **Oczekiwana poprawka:**
  - `GET /admin/klasy?nauczyciel_id&zarchiwizowane&kursor` i `GET /admin/klasy/{id}` (z `ETag`, audyt).
  - Pole `diagnostyczne` w `ZadaniePelneWyjscie` i filtr `?diagnostyczne=` w `GET /zadania`.
  - `PoprawkiZadania` z `dzial_id`, `umiejetnosci` (walidacja `trg_umiejetnosc_glowna_w_dziale`) albo osobne `PUT /admin/zadania/{id}/otagowanie` (nowa wersja Zadania).
  - Polityka `POST /zadania` = `NauczycielLubAdministrator` albo poprawione mapowanie stron `api.md` 7.

### C8-08: Serializacja i błędy deserializacji niezgodne z kontraktem (sprawdzone uruchomieniem): nazwy pól z cyframi, unia z dyskryminatorem, nieznane pole i błędy bez ProblemDetails
- **Waga:** ŚREDNIA
- **Lokalizacja:** `api.md` 1 (`JsonApi.Konfiguruj`, „nieznane pole żądania → 422 WALIDACJA”), 2 (`BladApi` z `problem?.kod ?? "BLAD_WEWNETRZNY"`), 3; ADR-061 p. 2; ADR-049 p. 14 (cyfry w nazwach, por. `oswiadczenie_wieku_16` w DDL); 12.2; 12.3 (`410` dla `…/zdjecia/…/adres`); 12.4; 14.2 (`BadHttpRequestException` → `400`)
- **Problem (ASP.NET Core 10.0.14 z `JsonApi.Konfiguruj`):**
  1. `JsonNamingPolicy.SnakeCaseLower` nie oddziela cyfr od liter:
     - `OswiadczenieWieku16` → `oswiadczenie_wieku16`;
     - `LiczbaWynikow7d` → `liczba_wynikow7d`, `SredniUlamek7d` → `sredni_ulamek7d`;
     - `LiczbaProbek90d` → `liczba_probek90d`, `OdsetekAutomatycznych30d` → `odsetek_automatycznych30d`.

     Tak samo w schemacie OpenAPI (`required: ["oswiadczenie_wieku16", …]`). Listing 12.4 i DDL używają `oswiadczenie_wieku_16`, `liczba_wynikow_7d`, `sredni_ulamek_7d`, `liczba_probek_90d`, `odsetek_automatycznych_30d`, `odsetek_eskalacji_7d`, `weryfikatorzy_globalni_aktywni_7d`… (≥ 14 pól). Test `TypyReferencyjneZgodneZSchematem` nie przejdzie, a JSON łamie ADR-049 p. 14, który EF Core realizuje przez `HasColumnName("oswiadczenie_wieku_16")`.
  2. Wartość enumu bez atrybutu: `RTrudnoscWGore` → `R_TRUDNOSC_W_GORE`, a listing ma `R-TRUDNOSC_W_GORE`. Atrybut `[JsonStringEnumMemberName]` jest potrzebny na **każdej** z 12 reguł, nie tylko jako „wyjątki”.
  3. Unia `WyborOdpowiedzi`: `{"opcja":"B","rodzaj":"OPCJA"}`, czyli dyskryminator nie jako pierwszy, kończy się `NotSupportedException: … must specify a type discriminator` → **`500`**. Kolejność kluczy w obiekcie JS nie jest kontrolowana przez typ TS. Potrzebne `JsonSerializerOptions.AllowOutOfOrderMetadataProperties = true` i mapowanie na `422`. To samo dotyczy `WskazowkaRozstrzygniecia`.
  4. Nieznane pole przy `UnmappedMemberHandling.Disallow` daje **`400` z pustym ciałem**, a nie `422 WALIDACJA` z `bledy_pol` (ADR-061 p. 2, `api.md` 1). Błędy wiązania i routingu (`400`, `404` trasy, `405`) też nie mają ciała `application/problem+json`. Klient mapuje je wtedy na `kod = "BLAD_WEWNETRZNY"` (toast „Coś poszło nie tak”) zamiast na kod domenowy.
  5. W katalogu 12.2 brakuje kodów `400` (np. `ZAPYTANIE_NIEPRAWIDLOWE`: JSON niepoprawny, przerwane ciało E-43) i `410` (zdjęcie usunięte, `…/zdjecia/…/adres`). Test `KatalogBledowKompletny` ich nie wykryje, bo porównuje tylko zbiory deklarowane.
- **Oczekiwana poprawka:**
  - Własna `JsonNamingPolicy` (snake_case z separacją cyfr) albo `[JsonPropertyName]` dla pól z cyframi i test „nazwy JSON = nazwy kolumn DDL dla pól mapowanych 1:1”.
  - `[JsonStringEnumMemberName]` na każdej regule.
  - `AllowOutOfOrderMetadataProperties = true`.
  - `IExceptionHandler` dla `BadHttpRequestException`/`JsonException`/`NotSupportedException` z wiązania → `422 WALIDACJA` (pole z `Path`).
  - `UseStatusCodePages` z ProblemDetails dla `404`/`405`.
  - Kody `ZAPYTANIE_NIEPRAWIDLOWE` (400) i `ZASOB_USUNIETY` (410) w `katalog-bledow.yaml`.
  - Test `KazdaOdpowiedzBleduMaKod` (fuzz: niepoprawny JSON, nieznane pole, zła ścieżka, zła metoda).

### C8-09: Semantyka `PATCH` i odpowiedzi wielowariantowe — nie da się wyczyścić pola, a `201` rejestracji ma dwa niezróżnicowane kształty
- **Waga:** ŚREDNIA
- **Lokalizacja:** 12.3 `PATCH /uczniowie/ja`, `PATCH /admin/uzytkownicy/{id}`, `POST /auth/rejestracja` („`201 SesjaWyjscie` (Nauczyciel: … `201` bez tokenu)”); `api.md` 3 (`ZmianaUczniaWejscie(… int? WybranyDzialId = null …) // PATCH: null = bez zmiany`, `RejestracjaNauczycielaWyjscie(StatusKonta Status)`); FR-09, FR-02
- **Problem:**
  1. `null` znaczy „bez zmiany”. Sprawdzone: `{"wybrany_dzial_id": null}` i `{}` dają ten sam rekord. Uczeń, który wybrał Dział (FR-09), nie może wrócić do rekomendacji wszystkich Działów, a `termin_egzaminu` nie da się usunąć. Dokument nie opisuje innej operacji, która to umożliwia.
  2. `POST /auth/rejestracja` zwraca `201` z `SesjaWyjscie` (Uczeń) albo `RejestracjaNauczycielaWyjscie` (Nauczyciel) bez dyskryminatora. OpenAPI dla jednego kodu statusu da `oneOf` bez `discriminator`, więc klient TS musi zgadywać po obecności `access_token`. To nie jest zapisane i nie jest testowane.
- **Oczekiwana poprawka:**
  - JSON Merge Patch (RFC 7396) z typem „pole obecne/nieobecne” (`Opcjonalne<T>` z konwerterem STJ) albo jawne pola czyszczące (`wyczysc_wybrany_dzial: true`).
  - Rejestracja: jeden rekord `RejestracjaWyjscie { status_konta, sesja: SesjaWyjscie | null }` albo `202` dla Nauczyciela.
  - Testy obu przypadków.

### C8-10: Autozapis Poprawek Transkrypcji — wyścig z własnym żądaniem, nadpisywanie edytora przez SSE i nieograniczona liczba wersji
- **Waga:** ŚREDNIA
- **Lokalizacja:** 9.4 (autozapis „2 s po ostatniej edycji”, „INSERT transkrypcja vN” i praca `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` przy każdym `PUT`); 12.11, 14.7, `api.md` 7 („debounce 1,5 s”); 22 („autozapis PWA 2 s”); `api.md` 6.3 (`useStrumienRozwiazania` → `setQueryData(["rozwiazanie", id], …)` dla każdego `stan`); 12.4 `WeryfikacjaSzczegoly.transkrypcje` („wszystkie wersje”); C-03, C-43; 17.4 (1,5 wywołania sprawdzenia na Rozwiązanie)
- **Problem:**
  1. `PUT …/transkrypcja` wymaga `If-Match` = `rozwiazanie.version` i zmienia `version`. Jeśli kolejny autozapis (po debounce albo „przy opuszczeniu pola”) wyruszy, zanim przyjdzie odpowiedź poprzedniego (sieć komórkowa, 9.4: „przy opuszczeniu pola” + debounce), wysyła stary `ETag` i dostaje `412`. Uczeń widzi wtedy dialog „Dane zmieniły się w międzyczasie” na własnej edycji (C-03 opisuje tylko dwa urządzenia). Kolejka autozapisów per Rozwiązanie nie jest opisana.
  2. Każdy `PUT` zmienia skrót stanu, więc strumień tej samej karty dostaje `stan`. `setQueryData` nadpisuje wtedy stan edytora wersją z serwera, starszą niż tekst wpisany od wysłania `PUT`, i wpisane znaki giną. Brakuje reguły „edytor z niezapisanymi zmianami ignoruje `stan`, poza zmianą `status`”.
  3. Każdy autozapis tworzy nową wersję Transkrypcji i (dedup tylko wśród `OCZEKUJE`) kolejne wywołanie multimodalne. 10 min edycji z debounce 1,5 s daje do ~400 wersji. Weryfikacja ładuje „wszystkie wersje” (do 40 kroków każda), a model kosztów 17.4 zakłada 1,5 wywołania na Rozwiązanie bez uzasadnienia i limitu.
  4. Okres debounce jest niespójny: 1,5 s (12.11, 14.7, `api.md` 7) i 2 s (9.4, 22).
- **Oczekiwana poprawka:**
  - Kolejka autozapisów (jeden `PUT` w locie, następny z `ETag` z odpowiedzi poprzedniego).
  - Reguła edytora wobec `stan`.
  - Ograniczenie wersji: wersja domenowa tylko przy zatwierdzeniu albo „przy opuszczeniu pola” po co najmniej N s, szkic w kolumnie roboczej albo limit wersji na przesłanie. Wywołania sprawdzenia uzależnione od stabilności edycji (np. ≥ 10 s bez zmian).
  - Jedna wartość debounce w 22.
  - Test Playwright `autozapis-wolna-siec.spec.ts`.

### C8-11: Luki stron i odczytów dla FR-32/FR-34 oraz operacji z `If-Match` — Nauczyciel nie skoryguje Oceny poza Weryfikacją, brak edycji Przypisania
- **Waga:** ŚREDNIA
- **Lokalizacja:** `api.md` 7: `nauczyciel/klasy/[klasaId]/uczniowie/[uczenId]` (bez mutacji), `nauczyciel/weryfikacje/[weryfikacjaId]` (`POST /rozwiazania/{id}/korekty` (If-Match `R`), a strona czyta tylko `GET /weryfikacje/{id}` z ETag `W`), `nauczyciel/zgloszenia-bledu-zadan` (`rozpatrzenie` z If-Match `Z`, strona czyta tylko listę zgłoszeń); 12.3 (`/przypisania` tylko `POST`/`GET`); FR-32, FR-34; wizja („Nauczyciel … może … poprawiać oceny wystawione przez system”)
- **Problem:**
  1. Korekta Oceny ostatecznej wystawionej automatycznie (FR-34, 9.6.3) jest dostępna tylko ze strony Weryfikacji. Takie Rozwiązanie nie ma jednak otwartej Weryfikacji. Nie ma strony Rozwiązania Ucznia dla Nauczyciela (zdjęcia, Transkrypcja, Ocena, historia, akcja „Skoryguj”), a strona Ucznia w Klasie nie ma mutacji. Główny mechanizm uczenia systemu z wizji („poprawiać oceny … dzięki czemu z czasem system ocenia coraz trafniej”) nie ma interfejsu.
  2. Operacje z `If-Match` bez źródła wersji na stronie:
     - `korekty` (`R`) — `WeryfikacjaSzczegoly` nie zawiera `rozwiazanie.version`;
     - `rozpatrzenie` zgłoszenia (`Z`) — `ZgloszenieBleduZadaniaWyjscie` nie zawiera `zadanie.version`.

     Każde takie wywołanie kończy się `428` albo wymaga nieopisanego dodatkowego `GET`.
  3. FR-32: Przypisania nie da się zmienić (termin) ani wycofać (brak `PATCH`/`DELETE /przypisania/{id}`). Błędny termin oznacza, że do jego upływu Uczniowie widzą pozycje, a po nim następuje ujawnienie klucza (12.5).
  4. `POST /testy-diagnostyczne/{id}/pozycje/{…}/odpowiedz`: nie wiadomo, czy ponowna odpowiedź na tę samą pozycję nadpisuje poprzednią (`201` za każdym razem), czy ją odrzuca, i jaki jest wtedy kod.
- **Oczekiwana poprawka:**
  - Strona `nauczyciel/rozwiazania/[rozwiazanieId]` (`GET /rozwiazania/{id}` N, korekta z `If-Match` R).
  - Pola `rozwiazanie_version` w `WeryfikacjaSzczegoly` i `zadanie_version` w `ZgloszenieBleduZadaniaWyjscie` albo jawny odczyt w mapowaniu stron.
  - `PATCH /przypisania/{id}` (`termin`, ETag) i `POST /przypisania/{id}/wycofanie` z regułami dla prób w toku.
  - Semantyka ponownej odpowiedzi w Teście diagnostycznym.

### C8-12: Drobne niespójności wersji 3.2
- **Waga:** NISKA
- **Lokalizacja:** 3.6 (`ProfilUcznia`), `api.md` 1, 15.4 C-42, 12.6, 12.4/`api.md` 3, 12.1, 12.3, ADR-064 p. 3
- **Problem:**
  1. Szkielet 3.6 nadal ma `Dictionary<UmiejetnoscId, OpanowanieUmiejetnosci> _opanowanie` i indeksator, a `ef-core.md` 6.5 deklaruje `List<…>` (C7-06 p. 2). Wariant listy z rekordem i podmianą `with` działa w EF Core 10.0.3 (sprawdzone), więc wystarczy poprawić szkielet.
  2. `api.md` 1: komentarz „bez sekwencji <”, czyli ponownie zgubione `<` (C7-15 p. 8).
  3. C-42 kończy się „wersja 3.1: `Last-Event-ID` UUIDv7, C7-11”, choć mechanizm został zastąpiony migawką.
  4. Przykład 12.6 pomija pola wymagane rekordów (`nr_kroku_agenta`, `podejrzenie_danych_osobowych`, `tylko_dane_osobowe`, `zastapienia_danych_osobowych`, `zakres_poprawek`, `konflikt_filtra`, `zgodnosc_poprawek`), choć 12.1 mówi „pola `null` wysyłane jawnie”.
  5. Słownik: pole `prog` (`PozycjaKalibracji`) i `prog_aktywny` (`StanBezpiecznikaWyjscie`) zamiast `prog_pewnosci`/`prog_pewnosci_aktywny` („Próg pewności”, ADR-016).
  6. Polityki `RecenzentTresci` i `WeryfikatorGlobalny` (12.1, ADR-062 p. 2) nie są przypisane żadnemu endpointowi 12.3.
  7. Typy referencyjne węższe niż rekordy: `DezaktywacjaBezpiecznikaWejscie.rodzaj_zadania` (TS tylko `OTWARTE_OBLICZENIOWE`, C# `RodzajZadania`), krotki `[number, number]` (`ramka`, `koszyk`) wobec `IReadOnlyList<…>`.
  8. `GET /uczniowie/{id}/profil` z wartością `ja` (`api.md` 7) przy trasach `{id:guid}` (`api.md` 4) — brak trasy albo ograniczenia dla `ja`.
  9. Listy bez stronicowania, które rosną bez ograniczenia: `GET /admin/progi-pewnosci` („z historią”), `GET /rozwiazania/{id}/ocena?historia=true`, `GET /klasy`, `GET /zestawy-zadan`.
  10. ADR-064 p. 3: migawka zawiera ≤ 100 najnowszych nieprzeczytanych. Przy > 100 nieprzeczytanych starsze powiadomienie zatwierdzone w odwróconej kolejności (C-49) nie trafia ani do migawki, ani do strumienia. Warto to zapisać jako ograniczenie albo użyć limitu z kursorem.
- **Oczekiwana poprawka:** poprawki redakcyjne w podanych miejscach; dla p. 6 — usunięcie polityk albo przypisanie; dla p. 8 — osobna trasa `/uczniowie/ja/profil`.

### Podsumowanie iteracji 8

Weryfikacja uwag otwartych po iteracji 7:

| Stan | Liczba | Identyfikatory |
|---|---|---|
| ZAMKNIĘTA | 15 | C7-01 … C7-15 (pozostałości redakcyjne C7-06 p. 2 i C7-15 p. 8 przeniesione do C8-12) |
| OTWARTA | 0 | — |

Nowe uwagi iteracji 8:

| Waga | Liczba uwag | Identyfikatory |
|---|---|---|
| KRYTYCZNA | 0 | — |
| WYSOKA | 7 | C8-01, C8-02, C8-03, C8-04, C8-05, C8-06, C8-07 |
| ŚREDNIA | 4 | C8-08, C8-09, C8-10, C8-11 |
| NISKA | 1 | C8-12 |
| **Razem** | **12** | |

Ocena scenariuszy współbieżności C-47 … C-54:
- C-47 i C-48 potwierdzone na PostgreSQL 16;
- C-49 … C-53 są spójne z ADR-064/066 i `ef-core.md` (z ograniczeniem C8-12 p. 10);
- C-54 jest poprawny.

Brakuje scenariuszy:
- równoległego autozapisu z jednej karty i nadpisania edytora przez SSE (C8-10);
- zawieszonego klucza idempotencji (C8-03).

Werdykt:
- Wersja 3.2 zamyka wszystkie uwagi iteracji 7. Warstwa danych, prace w tle i potok agentów są wykonalne i w kluczowych miejscach sprawdzone.
- Kontrakt REST/SSE nie jest jeszcze na poziomie produkcyjnym:
  - główne żądanie FR-16 w zapisanej postaci się nie wiąże (C8-01);
  - wznawianie SSE przy niezmienionym stanie przełącza klienta na odpytywanie, które przy `null` kręci się bez przerwy (C8-02);
  - idempotencja może zablokować operację na 24 h albo wykonać ją dwa razy (C8-03);
  - `304` ukrywa zmiany zasobów złożonych (C8-04);
  - limity per IP odcinają klasy za NAT, a chwilowa awaria odświeżenia wylogowuje wszystkie karty (C8-05);
  - autoryzacja obiektowa i przepływy Administratora mają luki (C8-06, C8-07).
- Poprawki są lokalne:
  - wiązanie formularza;
  - pierwsze zdarzenie SSE;
  - przeniesienie klucza idempotencji do jednostki pracy;
  - rozdzielenie `ETag` reprezentacji i wersji;
  - klucze limitów;
  - uzupełnienie macierzy 12.8 i endpointów `admin/*`.

---

## Iteracja 9 (v3.4 — audyt końcowy i RTM)

Data przeglądu: 2026-11-01. Wersja dokumentu: 3.4 (iteracja 4 pętli „.NET 10 + Next.js”, audyt końcowy). Zakres:
- zamknięcie uwag C8-01 … C8-12 (wersja 3.3) i ADR-067 … ADR-075;
- sekcja 18 (RTM) — audyt niezależny od `check_rtm.py`;
- ADR-075 (angielskie nazwy plików i katalogów), 3.5 (kolumna EN), 6.4, 6.7, 6.8, 6.11;
- `docs/spec/api.md`, `ddl.md`, `ef-core.md`, `prompts.md` w wersji 3.4.

**Metoda (artefakty krytyka w scratchpadzie, katalog `c9/`, niezależne od projektów architekta).**
1. Oba bloki SQL `ddl.md` załadowane na czystej bazie PostgreSQL 16: 0 błędów, 61 tabel + 4 partycje, 43 enumy, 15 triggerów. Funkcję `zarezerwuj_klucz_idempotencji` sprawdzono 9 scenariuszami (pierwsze wykonanie, `W_TOKU`, `KONFLIKT_CIALA`, przejęcie po 60 s, spóźnione zatwierdzenie starego wykonania → `UPDATE 0`, `POWTORKA`, wiersz po `wygasa_at`, rola `lemat_api`).
2. Własna aplikacja ASP.NET Core 10.0.14 z rekordami i politykami z `api.md` 1 i 3:
   - wiązanie `[AsParameters] RozwiazanieWejscie` przez `curl -F`: pola `snake_case`, `GetFiles("zdjecia")` przy dodatkowym pliku `inne`, brak pola, zły GUID, nazwy PascalCase;
   - `NazwyLemat` dla właściwości i enumów z cyframi;
   - `Opcjonalne<T>` (`{}` vs `null`, `application/merge-patch+json`);
   - dyskryminator poza kolejnością.
3. `backend/.editorconfig` z 6.11.3 w projekcie klasy z `EnforceCodeStyleInBuild`: IDE0130 i IDE1006 zgłaszane jako błędy `dotnet build` (.NET SDK 10.0.114).
4. Moduły `errors.ts`, `refresh.ts`, `stream.ts`, `save-queue.ts` wyodrębnione z `api.md` 6 i skompilowane `tsc --strict` 5.9.3: 0 błędów. Serializacja `FormData` sprawdzona w Node 22 (undici).
5. RTM — własny skrypt `c9/rtm9.py`. Poza testami `check_rtm.py` sprawdza:
   - istnienie projektów i przestrzeni nazw w specyfikacji;
   - interfejsy Agentów i pluginy w 6.6/10;
   - typy prac w tle w 13.3 i identyfikatory promptów w `prompts.md`;
   - rozwiązanie każdej strony do drzewa 6.8 (`page.tsx`);
   - wpis każdego `*.spec.ts` w 6.11.4 i wystąpienie każdego testu C# poza RTM;
   - zgodność dwukierunkową z kolumną „Wymagania” katalogu 12.3;
   - użycie endpointu przez stronę z wiersza (mapowanie `api.md` 7).
6. Reguły `TestNazwPlikow` (6.11.2 p. 4 c, e) zasymulowane skryptami `c9/t611b.py`, `c9/t_e.py` na nazwach plików z drzew 6.7, 6.8 i 6.11.

### Weryfikacja uwag iteracji 8

| Uwaga | Status | Uzasadnienie |
|---|---|---|
| C8-01 | ZAMKNIĘTA | `[AsParameters]` z `[FromForm(Name = "…")]`, `IFormFileCollection Pliki` i `GetFiles("zdjecia")` wiąże się poprawnie (sprawdzone: element → wartości, `zdjecia: 2` przy 3 plikach). Brak `zadanie_wersja_id`, zły GUID i nazwy PascalCase → `BadHttpRequestException` → `422` przez `ObslugaBledowWiazania`. Domyślny schemat OpenAPI ma `Pliki` i `allOf`, więc `TransformatorFormularzy` (ADR-067 p. 2) jest konieczny, a dokument go przewiduje. Nowy problem idempotencji multipart opisuje C9-01. |
| C8-02 | ZAMKNIĘTA | `stream.ts` (`tsc --strict` bez błędów) czeka na pierwsze zdarzenie z każdego połączenia (serwer zawsze wysyła `stan`). `null` kończy odpytywanie z odstępem ≥ 1 000 ms albo spoczynkiem 30 s. Ponowienie po `401` jest jedno (`ponowienia401`). `terazSerwer()` uwzględnia `roznicaZegara`. Pozostałość ADR-075 (`/logowanie`) — C9-05. |
| C8-03 | ZAMKNIĘTA | Sprawdzone na PostgreSQL 16: rezerwacja 60 s, przejęcie po wygaśnięciu, spóźnione zatwierdzenie starego wykonania (`UPDATE 0` → `ROLLBACK`), `POWTORKA`, CHECK `2xx`. Odpowiedź jest zatwierdzana w transakcji skutku (ADR-068 p. 2). Drobne odstępstwa — C9-07 p. 1. |
| C8-04 | ZAMKNIĘTA | Brak `If-None-Match`/`304`/`ETag` w odczytach (grep: 0 wystąpień poza Object Lock S3), `Cache-Control: no-store`, `version` w ciele, pola `rozwiazanie_version` i `zadanie_version`. |
| C8-05 | ZAMKNIĘTA | ADR-070: limity kluczowane kontem, (IP, e-mail) i rodziną tokenu, Caddy 12 000/min z modelem A-03. W `refresh.ts` `WYLOGOWANIE` następuje wyłącznie po `401`, a `429`/`5xx`/sieć dają ponowienia i `SESJA_TYMCZASOWO_NIEDOSTEPNA`. |
| C8-06 | ZAMKNIĘTA | Macierz 12.8 ma wiersz dla każdej ścieżki z `{id}` i regułę identyfikatorów zagnieżdżonych. Widoczność `ZadanieUczniaWyjscie` zależy od kontekstu. `RolaRejestracji { Uczen, Nauczyciel }`. Polityka jest na każdym endpoincie. |
| C8-07 | ZAMKNIĘTA | `GET /admin/klasy`, `GET /admin/klasy/{id}` (`KlasaAdministratora.version`), pole i filtr `diagnostyczne`, `PUT /admin/zadania/{id}/otagowanie`; mapowanie stron `admin/*` poprawione. |
| C8-08 | ZAMKNIĘTA | Sprawdzone: `oswiadczenie_wieku_16`, `liczba_wynikow_7d`, `n_eff`. Dyskryminator poza kolejnością działa, a błędy wiązania dają `422`. Separacja cyfr wprowadziła jednak niezgodność z DDL dla terminów technicznych i wartości enumów — nowa uwaga C9-03. |
| C8-09 | ZAMKNIĘTA | Sprawdzone: `{"wybrany_dzial_id": null}` → `Obecne = true, Wartosc = null`, `{}` → `Obecne = false`; `application/merge-patch+json` przyjmowany. `RejestracjaWyjscie { status_konta, sesja }`. |
| C8-10 | ZAMKNIĘTA (projekt) | Szkic roboczy, wersja domenowa po 10 s stabilnej edycji, limit 20, `KolejkaZapisowRozwiazania` i reguła edytora wobec `stan` usuwają wszystkie cztery problemy C8-10. Wybrany sposób numerowania zapisów jest jednak niewykonalny — nowa uwaga C9-02. |
| C8-11 | ZAMKNIĘTA | Strona `teacher/solutions/[solutionId]` z korektą, `rozwiazanie_version`, `zadanie_version`, ADR-074 (`PATCH`/`wycofanie` Przypisania, ponowna odpowiedź nadpisuje). |
| C8-12 | ZAMKNIĘTA | Sprawdzone wyrywkowo: p. 1 (`List<OpanowanieUmiejetnosci>`), p. 2 (`<`), p. 5 (`prog_pewnosci*`), p. 8 (`/uczniowie/ja/profil`), p. 9 (stronicowanie), p. 10 (`nastepny_kursor`). |

**Audyt RTM — wynik.**
- `check_rtm.py`: 0 błędów (46/46 wymagań, 140 odwołań do endpointów, 114 do tabel, 80 do stron, 96 do ADR).
- Własny skrypt potwierdza:
  - 46 kompletnych wierszy (żadna kolumna pusta poza świadomym „—” dla Agenta i dla tabel/endpointów NFR-10);
  - wszystkie projekty .NET, interfejsy Agentów, pluginy, typy prac w tle, identyfikatory promptów i testy C# istnieją poza RTM;
  - każda strona rozwiązuje się jednoznacznie do pliku `page.tsx` drzewa 6.8 (59 stron);
  - każdy `*.spec.ts` ma wpis w 6.11.4.
- Słabości macierzy (zgodność dwukierunkowa z 12.3, notacja stron) — C9-06.

**Analiza end-to-end (14 wymagań: strona → endpoint → przypadek użycia → Agent → tabela).** Bez luk blokujących:
- FR-03, FR-09, FR-11/FR-13, FR-15, FR-24, FR-28, FR-31, FR-32, FR-34;
- NFR-07 (circuit breaker → `ZapasowyLlmAdapter` → `decyzja_modelu`);
- NFR-09 (`DELETE /uczniowie/ja` → Zdarzenie RODO przed transakcją → `USUN_KONTO`; `szkic_transkrypcji` z `ON DELETE CASCADE`).

Defekty znaleziono w trzech wymaganiach:
- FR-16 i NFR-08 — idempotencja multipart (C9-01);
- FR-18 — autozapis szkicu (C9-02);
- FR-06 — źródło `element_sciezki_id` na stronie Wkładki (C9-06 p. 4).

### C9-01: Skrót ciała dla idempotencji obejmuje losową granicę `multipart`, więc każde ponowienie przesłania zdjęć dostaje `422 KLUCZ_IDEMPOTENCJI_KONFLIKT` (sprawdzone uruchomieniem)
- **Waga:** WYSOKA
- **Lokalizacja:** `ddl.md` `klucz_idempotencji.skrot_zapytania` („SHA-256(metoda + ścieżka + ciało)”), `0007_api.sql` (`IF r.skrot_zapytania <> p_skrot THEN 'KONFLIKT_CIALA'`, sprawdzane przed `W_TOKU`); ADR-062 p. 3; ADR-068 p. 1 i p. 5; 12.1; 12.3 `POST /rozwiazania`, `POST /rozwiazania/{id}/zdjecia`, `POST /admin/arkusze-cke/importy`; E-02, E-41, E-47; testy `double-click.spec.ts`, `cellular-network.spec.ts`; FR-16, NFR-08
- **Problem:**
  1. Przeglądarka (i undici w Node) generuje nową granicę (`boundary`) przy każdej serializacji tego samego `FormData`. Sprawdzone w Node 22: dwa `Request` z tym samym obiektem `FormData` mają różne nagłówki `Content-Type` i różne ciała. „Ten sam klucz, to samo żądanie” daje więc zawsze inny `skrot_zapytania`.
  2. Funkcja `zarezerwuj_klucz_idempotencji` porównuje skrót przed sprawdzeniem stanu. Sprawdzone: przy zapisanym `2xx` i przy rezerwacji w toku inny skrót daje `KONFLIKT_CIALA`.
  3. W efekcie dla `POST /rozwiazania` (główny przepływ FR-16, sieć komórkowa NFR-08):
     - ponowienie po utraconej odpowiedzi (E-02, „klient ponawia z tym samym `Idempotency-Key`”) dostaje `422` zamiast `202` z `Idempotent-Replayed`;
     - podwójne kliknięcie (E-41) dostaje `422` zamiast `409 IDEMPOTENCJA_W_TOKU`.
  4. Według ADR-068 p. 5 klient traktuje `422` jako „inne `4xx`” i przy następnym kliknięciu tworzy nowy klucz. Nowe żądanie trafia w `ux_rozwiazanie_element`/`ux_rozwiazanie_przypisanie` i dostaje `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN`, choć zdjęcia zostały przyjęte. Uczeń widzi błąd po udanym przesłaniu.
  5. To samo dotyczy `POST /rozwiazania/{id}/zdjecia` i importu ZIP (≤ 50 MB). Przy imporcie skrót całego ciała oznacza dodatkowo buforowanie 50 MB w filtrze, a dokument nie opisuje, jak filtr czyta strumień po wiązaniu formularza.
- **Oczekiwana poprawka:**
  - Kanoniczny skrót żądania: metoda, ścieżka, dla JSON — ciało, dla `multipart/form-data` — posortowane pary (nazwa pola, wartość) pól tekstowych oraz (nazwa pola, kolejność, długość, SHA-256 zawartości) każdego pliku, bez nagłówka `Content-Type` i granicy.
  - Opis w ADR-068 p. 1 i 12.1.
  - Test `PonowienieMultipartTymSamymKluczemDajePowtorke`: dwa formularze o tej samej treści i różnych granicach → `POWTORKA`; inny plik → `KONFLIKT_CIALA`.

### C9-02: `numer_zapisu` szkicu startuje od `Date.now()`, które przekracza `int` w C# i `integer` w DDL — każdy autozapis kończy się `422`
- **Waga:** WYSOKA
- **Lokalizacja:** ADR-073 p. 1 („`numer_zapisu` jest licznikiem karty (`sessionStorage`, start od `Date.now()` przy otwarciu edytora)”); `api.md` 3 (`SzkicTranskrypcjiWejscie(…, [property: Range(1, int.MaxValue)] int NumerZapisu, …)`, `SzkicTranskrypcjiWyjscie(…, int NumerZapisu, …)`); `ddl.md` `szkic_transkrypcji.numer_zapisu integer`; 12.3 `PUT /rozwiazania/{id}/transkrypcja/szkic`; C-56; `autosave-slow-network.spec.ts`; FR-18
- **Problem:**
  1. `Date.now()` w dniu przeglądu = 1 793 527 047 367, a `int.MaxValue` = 2 147 483 647. System.Text.Json z `NumberHandling.Strict` odrzuca wartość dla `int`. `ObslugaBledowWiazania` zamienia to na `422 WALIDACJA` (`TYP_NIEPRAWIDLOWY`). Nawet po zmianie typu w C# kolumna `integer` w PostgreSQL odrzuci wartość (`integer out of range`).
  2. Każdy `PUT …/szkic` (co 1,5 s) kończy się więc błędem i toastem (`onBlad` w `KolejkaZapisowRozwiazania`). Szkic nigdy nie powstaje, a odtworzenie edytora po przeładowaniu, czyli cel ADR-073 p. 1, nie działa. Wersje domenowe po 10 s nadal się zapisują, więc FR-18 działa w trybie zdegradowanym.
  3. Przy poprawionym typie zostaje błąd semantyczny. Karta otwarta wcześniej ma zawsze niższe numery niż karta otwarta później, więc jej szkice są **po cichu** pomijane (`204` w obu przypadkach). Uczeń w starszej karcie uważa tekst za zapisany, a po przeładowaniu odtwarza się szkic z drugiej karty. C-56 opisuje jedną kartę, a reguła „nowa karta ma wyższe numery” nie rozstrzyga edycji przeplatanej.
- **Oczekiwana poprawka:**
  - `numer_zapisu` jako `long`/`bigint` albo licznik nadawany przez serwer (np. `numer_zapisu` z odpowiedzi poprzedniego `PUT`).
  - Pominięty zapis zgłaszany jawnie (`409 SZKIC_NIEAKTUALNY` z aktualnym szkicem albo pole `zapisano: false` w odpowiedzi), żeby edytor w starszej karcie mógł pokazać konflikt.
  - Test `SzkicZNumeremZDateNowJestPrzyjety` i scenariusz C-56 dla dwóch kart.

### C9-03: `NazwyLemat` rozdziela cyfry także w terminach technicznych i wartościach enumów — API zwraca `EWALUACJA_ZT_1`, a DDL i EF Core mają `EWALUACJA_ZT1` (sprawdzone uruchomieniem)
- **Waga:** ŚREDNIA
- **Lokalizacja:** `api.md` 1 (`NazwyEnum = new NazwyLemat(JsonNamingPolicy.SnakeCaseUpper)`, wyrażenie `(?<=[A-Za-z])(?=\d)`); ADR-067 p. 3 („Reguła jest tą samą regułą, którą ADR-049 p. 14 stosuje do kolumn”); `ef-core.md` 4 (`EwaluacjaZt1` → `EWALUACJA_ZT1`); `ddl.md` `zrodlo_progu`, `zrodlo_kalibratora`, CHECK `metoda IN ('TOZSAMOSCIOWY','IZOTONICZNY_CV5')`, kolumna `sha256`; 3.1 p. 6 (terminy `cv5`, `sha256`, `fnv1a`); `ProgPewnosciWyjscie.Zrodlo`
- **Problem:** wynik uruchomienia na ASP.NET Core 10.0.14 z opcjami `JsonApi.Konfiguruj`:

  | Członek / właściwość C# | Wynik w API | Wartość w DDL / EF Core / słowniku |
  |---|---|---|
  | `ZrodloProgu.EwaluacjaZt1` | `EWALUACJA_ZT_1` | `EWALUACJA_ZT1` |
  | `MetodaKalibratora.IzotonicznyCv5` | `IZOTONICZNY_CV_5` | `IZOTONICZNY_CV5` |
  | `PowodPorzucenia.BrakPostepu7Dni` | `BRAK_POSTEPU_7_DNI` | — |
  | właściwość `Sha256` | `sha_256` | `sha256` |
  | właściwość `Cv5` | `cv_5` | `cv5` |
  | właściwość `Fnv1a` | `fnv_1a` | `fnv1a` |

  Skutki:
  - `GET /admin/progi-pewnosci` zwraca `zrodlo: "EWALUACJA_ZT_1"`, czyli wartość spoza `pg_enum`, a typ TS z OpenAPI ma literał niezgodny z 3.5;
  - test słownika (3.4 p. 2) dostaje tokeny `zt`, `cv`, `sha` spoza słownika tokenów;
  - deklaracja ADR-067 p. 3 o „tej samej regule” jest fałszywa: EFCore.NamingConventions i `TlumaczNazwUpperSnake` nie rozdzielają cyfr.

  Test `NazwyJsonZgodneZKolumnamiDdl` obejmuje tylko rekordy mapowane 1:1 na tabele i wartości enumów nie sprawdza.
- **Oczekiwana poprawka:**
  - Wyjątki `NazwyLemat` dla terminów technicznych 3.1 p. 6 i oznaczeń (`zt1`, `cv5`, `sha256`, `fnv1a`) albo reguła „separator tylko przed liczbą z jednostką (`7d`, `30d`, `16`)” z jawną listą.
  - Test `WartosciEnumowApiRowneDdl`: każdy enum z OpenAPI porównany z `pg_enum` albo CHECK.

### C9-04: Reguły `TestNazwPlikow` (6.11.2 p. 1, p. 4c, p. 4e) nie przechodzą na drzewach z 6.7/6.8, a kolumna EN 3.5 nie jest „jedyną nazwą” pojęcia
- **Waga:** ŚREDNIA
- **Lokalizacja:** 3.5 (akapit „Kolumna Nazwa pliku/folderu (EN)” — „jedyną angielską nazwę pojęcia”), 6.7, 6.8, 6.11.1, 6.11.2 p. 1–4, 6.11.4; ADR-075 p. 3, p. 5, p. 9
- **Problem (symulacja reguł skryptami na nazwach z dokumentu):**
  1. **Reguła 4c** („plik typu z 3.5 = nazwa EN pojęcia + przyrostek z listy”). Drzewo 6.7 ma pliki typów z kolumny C# tabeli 3.5, które jej nie spełniają:
     - `JobType.cs` (`TypPracy`), `JobHandler.cs` (`IHandlerPracy`), `JobPool.cs` (`PulaPracy`), `JobEnqueuing.cs` (`IZlecaniePrac`) — pojęcie „Praca w tle” ma nazwę `BackgroundJob`;
     - `ChatClientFactory.cs` (`FabrykaKlientowCzatu`, wiersz „Model generatywny” → `GenerativeModel`);
     - `S3GdprEventRegistry.cs` (przedrostek `S3`).

     Przykład z samej reguły, `SafeguardState.cs` (`StanBezpiecznika`), używa przyrostka `State`, którego nie ma na liście. `ISolutionsFacade.cs` i `SolutionsEndpoints.cs` mają liczbę mnogą i przedrostek `I`, których reguła nie przewiduje.
  2. **Reguła 1** („jeden plik — jeden typ najwyższego poziomu”) wobec 6.7:
     - `Common/Identifiers.cs` (wszystkie typy `*Id`) przeczy przykładowi `RozwiazanieId` → `SolutionId.cs` z 6.11.2;
     - `JobHandler.cs (IHandlerPracy, RezultatPracy)`, `JobEnqueuing.cs (IZlecaniePrac, ZleceniePracy)` i `GenerativeModel.cs (IModelGeneratywny, KlientCzatuRoli)` deklarują po dwa typy.
  3. **Reguła 4e** (brak polskich tokenów: rdzenie ≥ 4 znaków z kolumny PostgreSQL 3.5 minus `wspolne_tokeny`). Kolumna daje 222 tokeny, w tym angielskie słowa `status`, `limit`, `element`, `administrator`, `version`, `total`, `date`, `numeric`. Zgodnie z regułą odrzucone zostałyby:
     - przy dopasowaniu całych tokenów — `SolutionStatus.cs`, `(administrator)`;
     - przy dopasowaniu rdzenia jako prefiksu — także `Profile/` (`profil`), `Program.cs` (`prog`), `RateLimiter.cs` (`limit`).

     Lista `wspolne_tokeny` (6 słów) nie wystarcza, a sposób dopasowania (cały token czy prefiks) nie jest określony.
  4. **Jednoznaczność kolumny EN.** Akapit 3.5 mówi o jedynej nazwie pojęcia w nazwach plików i katalogów. Drzewa używają jednak innych nazw:

     | Pojęcie | Kolumna EN (3.5) | Użycie w drzewach |
     |---|---|---|
     | Klasa | `SchoolClass` | katalog `Classes/`, `features/classes`, trasy `classes`, `ClassTransfer` (Przekazanie Klasy) |
     | Arkusz CKE | `CkeExamSheet` | `cke-sheets` |
     | Profil ucznia | `StudentProfile` | `Profile/`, `profile` |
     | Dział | `CurriculumSection` | `fr09-section-choice` |

     Deweloper zakładający szkielet musi zgadywać, która nazwa jest wiążąca. Test `NazwyPlikowPojecUnikalne` sprawdza unikalność kolumny, nie zgodność drzew z kolumną.
- **Oczekiwana poprawka:**
  - Zakres reguły 4c: wyłącznie główny typ pojęcia albo lista typów pojęcia w manifeście; dopuszczalne `I`-przedrostek, liczba mnoga dla `Facade`/`Endpoints`, przyrostek `State`.
  - Drzewo 6.7 zgodne z regułą (`BackgroundJobType.cs` … albo przeniesienie typów do manifestu jako technicznych; `Identifiers.cs` rozbity albo wyjątek).
  - Dopasowanie w regule 4e jako całych tokenów i lista `wspolne_tokeny` uzupełniona o słowa angielskie z kolumny PostgreSQL.
  - Jedna nazwa EN dla Klasy (`Class` / `classes`) i Arkusza CKE (`CkeSheet`) albo dopuszczenie formy skróconej w 6.11.
  - Test „drzewa dokumentu przechodzą `TestNazwPlikow`” na manifeście z 6.7.

### C9-05: Pozostałość polskiego adresu strony po ADR-075 — wylogowanie w innych kartach przechodzi na nieistniejące `/logowanie`
- **Waga:** ŚREDNIA
- **Lokalizacja:** `api.md` 6.2 (`if (m.data?.typ === "WYLOGOWANIE") { token = null; window.location.assign("/logowanie"); }`); ADR-057 p. 1 („każda karta czyści pamięć i przechodzi do `/logowanie`”); 6.8 (`/login`), 6.11.4; `check_rtm.py` (10)
- **Problem:**
  - Po ADR-075 strona logowania ma adres `/login`, a przekierowania ze starych adresów świadomie nie powstają (6.11.4).
  - Wylogowanie w jednej karcie i `401` z `/auth/odswiezenie` rozgłaszają `WYLOGOWANIE`, więc pozostałe karty przechodzą na `/logowanie`, czyli stronę 404 Next.js.
  - Sprawdzenie (9)–(10) w `check_rtm.py` obejmuje tylko RTM i odnośniki Markdown, a nie adresy stron w kodzie listingów.
  - Przykład ADR-049 p. 3 (`app/(uczen)/sciezka-nauki`) jest nadpisany przez p. 16, więc można go pozostawić.
- **Oczekiwana poprawka:**
  - `/login` w `api.md` 6.2 i ADR-057.
  - Rozszerzenie skryptu o literały adresów stron (`"/…"` w blokach `ts`/`tsx`) porównywane z tabelą 6.11.4.
  - Test Playwright `session-two-tabs.spec.ts` z asercją adresu po wylogowaniu.

### C9-06: RTM — niezgodność dwukierunkowa z katalogiem 12.3, niejednolita notacja stron i drobne luki śladu
- **Waga:** NISKA
- **Lokalizacja:** 18.1, 18.2; 12.3 (kolumna „Wymagania”); `api.md` 7; `docs/spec/tools/check_rtm.py`
- **Problem:**
  1. **Zgodność z 12.3.** Około 25 par (wymaganie, endpoint) z RTM nie ma tego wymagania w kolumnie „Wymagania” 12.3, na przykład:
     - `GET /rozwiazania/{id}/ocena` w wierszach FR-19 … FR-22 i NFR-05 (12.3: FR-24, FR-27);
     - `GET /weryfikacje` w FR-25 i FR-26 (12.3: FR-33);
     - `POST /weryfikacje/{id}/rozstrzygniecie` w FR-28;
     - `GET /admin/kalibracja` w FR-22, NFR-03 i NFR-07;
     - `POST /rozwiazania` w NFR-08.

     Śledzenie wymaganie → endpoint i endpoint → wymaganie daje więc różne odpowiedzi.
  2. **Notacja stron.** RTM łączy pełne ścieżki (`(public)/login`, `(administrator)/admin/users`) ze skrótami (`register/teacher`, `teacher/...`, `admin/...`). `check_rtm.py` akceptuje skróty `admin/…` specjalną regułą. Własny skrypt rozwiązał wszystkie strony jednoznacznie do 6.8, ale konwencja nie jest zapisana.
  3. **Endpointy katalogu bez wiersza RTM:** `GET /przypisania/{id}`, `GET /zestawy-zadan/{id}` (FR-32), `POST /admin/uzytkownicy`, `GET /powiadomienia/strumien`, `POST /powiadomienia/{id}/przeczytanie`, `GET /admin/praca-w-tle`, `GET /admin/harmonogram`, `POST /admin/praca-w-tle/{id}/ponowienie`.
  4. **FR-06:** strona `(student)/theory-inserts/[theoryInsertId]` wywołuje `POST /elementy-sciezki/{id}/przeczytanie`, ale mapowanie nie mówi, skąd strona zna `element_sciezki_id`. Dla Zadań jest `?element=`, dla Wkładki brak parametru.
  5. Wiersz `(student)/settings` w `api.md` 7 wymienia `GET /uczniowie/ja/eksport-danych` w kolumnie „Mutacje”.
  6. `check_rtm.py` nie sprawdza kolumn „Moduł”, „Agent” i „Weryfikacja”. Własny skrypt potwierdził je w całości, ale CI tego nie zrobi.
- **Oczekiwana poprawka:**
  - Uzgodnić kolumnę „Wymagania” 12.3 z RTM (albo regułę „12.3 wymienia wymaganie główne”) i dopisać sprawdzenie dwukierunkowe do skryptu.
  - Zapisać notację stron (zawsze pełna ścieżka z grupą).
  - Uzupełnić p. 3–5.
  - Dodać do `check_rtm.py` sprawdzenia z metody p. 5.

### C9-07: Drobne niespójności wersji 3.4
- **Waga:** NISKA
- **Lokalizacja:** `ddl.md` `0007_api.sql`; 6.11.3; `ef-core.md` 2; 13.2, 7.1 (3.6); `api.md` 6.4; 18.5
- **Problem:**
  1. `zarezerwuj_klucz_idempotencji` (sprawdzone na PostgreSQL 16):
     - przejęcie wygasłej rezerwacji nadpisuje `skrot_zapytania` bez porównania — inne ciało z tym samym kluczem dostaje `WYKONAJ`, a pierwotne ciało potem `KONFLIKT_CIALA`;
     - funkcja pomija `wygasa_at` — wiersz po 24 h, jeszcze nieusunięty przez sprzątanie, daje `POWTORKA`.
  2. Reguła `pola_prywatne` (`_camelCase`, IDE1006 `error`) obejmuje też `private const` i `private static readonly`. Sprawdzone `dotnet build`: `private const int MaksPunktow` daje błąd IDE1006. `ef-core.md` ma `private static readonly INpgsqlNameTranslator T` — szkielet się nie zbuduje. Potrzebna osobna reguła dla stałych albo poprawione listingi.
  3. Odwołania do testów w postaci `Projekt/Katalog/KlasaTestowa` z polską nazwą klasy (13.2: `Lemat.Infrastructure.Tests/Models/OgranicznikSzybkosciTests`, `Lemat.Simulations/OgranicznikSymulacja`; 3.6: `Lemat.Domain.Tests/Solutions/PrzejsciaRozwiazaniaTests`; ADR-057, ADR-070, ADR-073) obok plików `RateLimiterTests.cs` i `RateLimiterSimulation.cs` w 6.7. Należy zapisać, że taka notacja wskazuje klasę, a plik wynika z manifestu.
  4. `KolejkaZapisowRozwiazania.dodaj` przy scalaniu szkiców usuwa starszy `SZKIC` sprzed oczekującej `WERSJA`, więc nowszy szkic trafia za `WERSJA`. Jeśli ładunek szkicu (z `transkrypcja_id`) powstaje w chwili `dodaj`, a nie w `wykonaj`, dostaje `409 TRANSKRYPCJA_NIEAKTUALNA` i toast. Należy zapisać, że `wykonaj` buduje ładunek z bieżącego stanu edytora.
  5. Warunek startu implementacji: `CLAUDE.md`, `TASKS.md` i `HANDOFF.md` nadal opisują stos 2.4 (Python/FastAPI, `src/lemat/`). 18.5 wymaga ich regeneracji przed pierwszym zadaniem implementacyjnym. Nie jest to wada architektury, ale deweloper czytający `CLAUDE.md` zbudowałby niewłaściwy szkielet.
- **Oczekiwana poprawka:** poprawki w podanych miejscach; dla p. 5 — regeneracja dokumentów planu przez planistę przed startem implementacji.

### Podsumowanie iteracji 9

Weryfikacja uwag otwartych po iteracji 8:

| Stan | Liczba | Identyfikatory |
|---|---|---|
| ZAMKNIĘTA | 12 | C8-01 … C8-12 (skutki uboczne poprawek C8-08 i C8-10 zapisane jako C9-03 i C9-02) |
| OTWARTA | 0 | — |

Nowe uwagi iteracji 9:

| Waga | Liczba uwag | Identyfikatory |
|---|---|---|
| KRYTYCZNA | 0 | — |
| WYSOKA | 2 | C9-01, C9-02 |
| ŚREDNIA | 3 | C9-03, C9-04, C9-05 |
| NISKA | 2 | C9-06, C9-07 |
| **Razem** | **7** | |

**ADR-075 — ocena:**
- Zasada (pliki i katalogi angielskie, identyfikatory według słownika) jest spójna z Z-6.
- Przestrzenie nazw = katalog: IDE0130 i IDE1006 działają w `dotnet build` (sprawdzone).
- W drzewach planowanych nie ma polskich nazw plików ani katalogów: skan 6.7, 6.8, 6.11, `prompts.md` i ADR-040 … 075 nie znalazł żadnej poza historycznymi z dopiskiem.
- Słabości:
  - reguły testu nazw nie przechodzą na samych drzewach dokumentu, a kolumna EN nie jest stosowana konsekwentnie (C9-04);
  - jeden polski adres strony w kodzie (C9-05).

**Gotowość do implementacji.**
- Szkielet solucji .NET jest wykonalny bez rozstrzygania otwartych kwestii architektury:
  - projekty, referencje i pakiety z 6.4;
  - `.editorconfig`, DDL ładowane bez błędów, EF Core;
  - kontrakty multipart, JSON Merge Patch i ProblemDetails sprawdzone uruchomieniem.
- Szkielet Next.js też jest wykonalny: drzewo 6.8, granice 6.9, moduły `lib` kompilują się `tsc --strict`.
- Przed rozpoczęciem:
  - poprawić C9-01 i C9-02 (kontrakty głównego przepływu FR-16/FR-18);
  - ujednolicić reguły nazw plików (C9-04), bo inaczej `TestNazwPlikow` zablokuje CI od pierwszego commita;
  - zregenerować `CLAUDE.md`/`TASKS.md`/`HANDOFF.md` (C9-07 p. 5).
- Decyzje G-0 (D-1 … D-17) mają opcje domyślne i nie blokują szkieletu.

### Podsumowanie pętli „.NET 10 + Next.js” (iteracje 6–9)

| Iteracja | Wersja | Temat | Uwagi nowe (K/W/Ś/N) | Zamknięte w następnej wersji |
|---|---|---|---|---|
| 6 | 3.0 | stos, moduły, słownik → model domenowy | 15 (0/4/8/3) | 15/15 w 3.1 |
| 7 | 3.1 | agenci AI (SK/MEAI), EF Core, porty, prace w tle | 15 (0/6/7/2) | 15/15 w 3.2 |
| 8 | 3.2 | kontrakty REST/SSE, błędy, współbieżność | 12 (0/7/4/1) | 12/12 w 3.3–3.4 |
| 9 | 3.4 | audyt końcowy, RTM, ADR-075 | 7 (0/2/3/2) | — |

Trend:
- liczba uwag WYSOKA: 4 → 6 → 7 → 2;
- obszary ryzyka przesunęły się z wykonalności stosu (iteracje 6–7) przez kontrakty HTTP (8) do pojedynczych defektów kontraktowych na styku przeglądarka ↔ API (9);
- warstwa danych, prace w tle, potok agentów, autoryzacja obiektowa i RTM są na poziomie produkcyjnym.

Werdykt:
- obie uwagi WYSOKA są lokalne (kanoniczny skrót żądania, typ i semantyka `numer_zapisu`), ale dotyczą głównego przepływu FR-16/FR-18 i obie potwierdzono uruchomieniem;
- zatwierdzenie wymaga ich zamknięcia — po poprawce C9-01 i C9-02 dokument spełnia kryterium APPROVED, a uwagi ŚREDNIA i NISKA mogą zostać zamknięte w tej samej wersji albo przeniesione do planu jako zadania z DoD.

STATUS: NEEDS_REVISION
