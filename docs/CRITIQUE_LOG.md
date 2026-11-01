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
- **Status:** OTWARTA

### C3-02: Filtr danych osobowych zmienia treść matematyczną Rozwiązania, a Uczeń nie może tego poprawić
- **Waga:** ŚREDNIA
- **Lokalizacja:** 10.3.1 p. 3a (a)–(c); 9.4 (Filtr także na `PUT …/transkrypcja`); 13.3 `ROZPOZNAJ_PISMO`; ADR-042; Z-1, Z-3; FR-18, FR-28; G-2/G-6
- **Problem:**
  1. **Usuwanie kroków na podstawie ramek LLM.** Reguła (a) usuwa krok, którego `obszar` pokrywa się w ≥ 50% z ramką danych osobowych. Zarówno `obszar`, jak i ramki pochodzą z Modelu generatywnego, a reguła działa przed zatwierdzeniem i bez śladu w UI. Gdy model błędnie oznaczy linię rozwiązania jako „dane osobowe” albo Uczeń podpisze się w linii wyniku, krok rozwiązania znika z Transkrypcji i Ocena jest liczona bez niego (np. `BRAK_WYMAGANEGO_KROKU`, zerowe punkty za wynik). To sygnał LLM działający w kierunku **niebezpiecznym**, wbrew Z-1, a błąd systemu obniża Ocenę, wbrew FR-28. Uczeń może co najwyżej wstawić krok ponownie jako Poprawkę (limit 2, wlicza się do limitu miękkiego, czyli może wywołać T-7).
  2. **Fałszywe trafienia nazw.** Reguła (b) zastępuje w `tekst` **i `latex`** każdy token zgodny z imieniem lub nazwiskiem (Levenshtein ≤ 1 na rdzeniu ≥ 4 znaków). Polskie nazwiska i imiona pokrywają się ze słowami matematycznymi albo są od nich odległe o jedną literę (np. Pole, Kula, Granica, Suma, Prosta, Koło). „Pole trójkąta” Ucznia o nazwisku Pole staje się „[DANE_OSOBOWE] trójkąta”, a podmiana wewnątrz LaTeX psuje parsowanie SymPy (N-3/N-4, niespójności).
  3. **Brak wyjścia.** Filtr działa także na Poprawkach Ucznia (9.4), więc Uczeń nie może przywrócić usuniętego słowa. Jedyne wyjście, czyli nowe zdjęcia, daje ten sam wynik. Dla takiego Ucznia FR-18 przestaje działać w tej części.
  4. **Luka w drugą stronę.** Imiona i nazwiska krótsze niż 4 znaki (Ewa, Jan, Ola, Iza, Ida, Lis) nie są porównywane, więc kryterium G-2/G-6 „0 wystąpień danych konta” może nie zostać spełnione. Na 50 kartkach to i tak słaby dowód (0/50 daje UB 7%).
- **Oczekiwana poprawka:** (a) zamiast usuwać krok, maskować w nim tylko fragment pokrywający się z ramką, a krok oznaczyć do sprawdzenia przez Ucznia i ewentualnie przez T-3. Usunięcie kroku nie może zmienić Oceny bez Weryfikacji (Z-1). (b) Ograniczyć dopasowanie do `tekst` (nie `latex`) i wykluczyć tokeny ze słownika terminów matematycznych. Dla trafienia niepewnego pokazać Uczniowi znacznik z możliwością przywrócenia słowa: Poprawka przywracająca token nie podlega ponownemu filtrowaniu, ale dostaje flagę do Kontroli. (c) Obsłużyć imiona i nazwiska krótsze niż 4 znaki (dokładne dopasowanie), a w G-2/G-6 dodać metrykę fałszywych zastąpień na ZT-1 i kryterium dla tokenów matematycznych.
- **Status:** OTWARTA

### C3-03: `UZGODNIJ_REJESTR_RODO` unieważnia każde zdarzenie `USUNIECIE_WYKONANE`, przez co rejestr i procedura PITR tracą wykonane usunięcia
- **Waga:** ŚREDNIA
- **Lokalizacja:** 16.4 („Po 30 dniach `USUN_KONTO` wykonuje…”, kroki 0–6; usunięcie konta Nauczyciela p. (1)); 13.3 `UZGODNIJ_REJESTR_RODO` („obiekty … starsze niż 5 min bez wiersza `zdarzenie_rodo` o tym `id` → obiekt kompensujący”); ADR-041 („`api` zapisuje każde Zdarzenie RODO … a kopię do `zdarzenie_rodo`”); 5.1 (krawędź `WRK → REJ`); 7.3 `zdarzenie_rodo.id` („generowany w api”); G-6 (a)
- **Problem:**
  1. `USUNIECIE_WYKONANE` zapisuje do bucketu **worker** (krok 1 `USUN_KONTO`), a nie `api`. Żaden z kroków 0–6 nie wstawia wiersza `zdarzenie_rodo`. Wiersze powstają tylko w transakcjach `api` (16.4 p. 1, przywrócenie, zgoda) i w `ponow_zdarzenia_rodo`.
  2. Po 5 minutach `UZGODNIJ_REJESTR_RODO` uznaje więc zdarzenie za „transakcję nieudaną” i zapisuje `UNIEWAZNIENIE`. Nawet jeśli wiersz byłby wstawiany w kroku 5, kroki 2–4 (aktualizacja wszystkich partycji `decyzja_modelu`, usuwanie obiektów S3) mogą trwać dłużej niż 5 minut, z tym samym skutkiem. Wiersz zapisany przed krokiem 5 i tak przepadłby przy PITR.
  3. Skutki: (a) każde wykonane usunięcie konta kończy się obiektem `UNIEWAZNIENIE` i alertem `ALERT_RODO` (17.3: przyrost `zdarzenia_rodo_uniewaznione_total` > 0), więc alert przestaje cokolwiek znaczyć, a rejestr rozliczalności (art. 5 ust. 2 RODO) mówi, że usunięcia nie wykonano; (b) przy PITR do punktu sprzed wykonania usunięcia `ponow_zdarzenia_rodo` pomija unieważnione `USUNIECIE_WYKONANE`, więc przywrócone dane konta (bez zdjęć) zostają w bazie do najbliższego nocnego `USUN_KONTO`. Odtworzone zostaje tylko `ZADANIE_USUNIECIA` z minionym terminem. Ponowne usunięcie zależy więc od harmonogramu, a nie od procedury odtworzenia. To regresja wobec wersji 2.1, w której rejestr wykonanych usunięć nie miał mechanizmu unieważniania. Test G-6 (a) da poprawny wynik tylko wtedy, gdy odtworzenie nastąpi w ciągu 5 minut od usunięcia.
- **Oczekiwana poprawka:** zdarzenia generowane przez worker (`USUNIECIE_WYKONANE`, także dla Nauczyciela) zapisać w jednej transakcji z krokiem 0 (wiersz `zdarzenie_rodo` + anulowanie procesów) albo oznaczyć w obiekcie `zrodlo = WORKER` i wyłączyć z `UZGODNIJ_REJESTR_RODO` (dla takich zdarzeń stanem docelowym jest wykonanie usunięcia, a nie transakcja). Poprawić ADR-041 („`api` zapisuje każde…”) i komentarz `zdarzenie_rodo.id`. Rozszerzyć G-6 (a) o odtworzenie wykonane później niż 10 minut po usunięciu.
- **Status:** OTWARTA

### C3-04: Globalny ogranicznik ma dwie sprzeczne formuły przydziału, a kubełek jest mniejszy od fali, którą trzeba w nim zarezerwować
- **Waga:** ŚREDNIA
- **Lokalizacja:** 7.3 `limit_szybkosci_modelu` (komentarz: „0.8 × limit_klucza / liczba wierszy”, brak kolumny wagi); 13.2 („`r = 0,8 × limit_klucza / N` … 10 żądań/s na proces”, „lokalny token bucket ma pojemność `r`”); ADR-039 p. 3 i 22 („`waga / Σ wag`, worker 1,0, `api` 0,1”); 17.4 („worker 18.2/s × 2, `api` 1.8/s × 2”); 17.2 S-6 („przydział workerów 36.4/s”, „≤ 1 s oczekiwania”); 13.1 `OgranicznikSzybkosci.zarezerwuj(…, liczba_zadan, …)`; 10.4.3 (fale po ≤ 16, runda 1 = 27 pytań)
- **Problem:**
  1. Dokument podaje dwie różne formuły. Przy podziale po równo (7.3, 13.2) workery dostają łącznie 20 żądań/s, przy wagach (ADR-039, 17.4, 22) — 36,4/s. Różnica ≈ 45% przepustowości Jev w szczycie „koniec lekcji” (do 44/s w trybie pojedynczym). Od wyboru zależy odsetek Ocen kierowanych do modelu zapasowego i ważność S-6. Tabela nie ma kolumny wagi, więc wariant z wagami nie wynika z DDL.
  2. Lokalny token bucket ma pojemność `r`, a `zarezerwuj` przyjmuje liczbę żądań. W trybie pojedynczym jedna fala to do 16 żądań, a runda 1 to 27. Przy `r = 10` (13.2) rezerwacja fali albo rundy **nigdy** nie może się udać. Oceny w trybie pojedynczym czekałyby do `termin_miekki − 11 s` i trafiały do modelu zapasowego, także poza szczytem. Nie zdefiniowano, czy rezerwuje się żądanie, falę czy rundę.
  3. Skutek: NFR-07 i G-4 (d) mierzą zachowanie inne niż opisane, a budżet 17.2 dla trybu pojedynczego (przyjęty jako najgorszy przypadek) zależy od nieustalonego parametru.
- **Oczekiwana poprawka:** jedna formuła we wszystkich miejscach. Jeśli są wagi, to kolumna `waga` w `limit_szybkosci_modelu` albo jawne mapowanie `wykonawca → waga`. Ustalić granulację rezerwacji (np. pojedyncze żądanie z oczekiwaniem albo fala z pojemnością kubełka ≥ maksymalnej fali) i warunek `pojemnosc ≥ 16`. Przeliczyć S-6 dla przyjętej formuły i dodać test jednostkowy rezerwacji fali.
- **Status:** OTWARTA

### C3-05: Poprawki Transkrypcji: kroki wstawione omijają miarę znaków, a Sprawdzenie zgodności nie mieści się w założonym oknie czasowym
- **Waga:** ŚREDNIA
- **Lokalizacja:** 9.4 (tabela limitów: „odsetek zmienionych znaków = Σ Levenshtein(odczyt, poprawka) / Σ długość odczytu”; „Sprawdzenie zwykle kończy się przed zatwierdzeniem”); 12.5 (`tekst`/`latex` ≤ 2000 znaków); 10.3.3 (G-2: tylko Poprawki „zastępujące”); 17.2 („gotowe przed zatwierdzeniem w > 95% przypadków”); 22 (timeout 8 s, oczekiwanie ≤ min(6 s, `termin_miekki − 9 s`)); 17.4 („Dostawca rozpoznawania: 90 żądań/min … limit ≥ 120/min”); 17.7 (T-7 = 3%)
- **Problem:**
  1. Krok wstawiony nie ma odczytu agenta, więc wzór na odsetek zmienionych znaków go nie obejmuje: licznik i mianownik dotyczą par (odczyt, poprawka). Uczeń może więc sfotografować kartkę z kilkoma liniami i wstawić 2 kroki po 2000 + 2000 znaków z kompletnym rozwiązaniem. Limit kroków (≤ 2) i limit miękki (≤ max(2, ⌈0,25 n⌉)) tego nie zatrzymają, więc jedyną barierą jest jedna decyzja LLM `widoczny_na_zdjeciu` dla wstawionego kroku bez `obszar`. G-2 mierzy wykrywalność tylko dla Poprawek „zastępujących”, a dla tego wektora nie mierzy niczego.
  2. Sprawdzenie zgodności to wywołanie multimodalne z kompletem zdjęć tej samej roli co rozpoznanie (p95 9 s dla 3 zdjęć, A-08), z timeoutem 8 s. Nic nie wymusza, żeby PWA wysyłała `PUT …/transkrypcja` wcześniej niż tuż przed `…/zatwierdzenie` (9.4 nie opisuje autozapisu). Okno w Ocenie wynosi najwyżej 6 s, więc założenie „> 95% gotowe przed zatwierdzeniem” jest nieuzasadnione. Brak rezultatu oznacza T-7, czyli Weryfikację. Przy oszacowaniu D-10 (15–25% Rozwiązań z Poprawkami) może to dać kilka razy więcej T-7 niż 3% z 17.7.
  3. Wywołania Sprawdzenia zużywają limit dostawcy rozpoznawania (A-08 ≥ 120/min) i pulę R, która w szczycie obsługuje rozpoznanie (17.4 liczy tylko 90 rozpoznań/min). W szczycie Sprawdzenie ma niższy priorytet (2) i czeka w puli R, co dodatkowo zwiększa liczbę T-7.
- **Oczekiwana poprawka:** wliczyć kroki wstawione do miary znaków (mianownik: znaki odczytu agenta, licznik: znaki zmienione plus wstawione) i ograniczyć długość kroku wstawionego (np. ≤ 200 znaków albo ≤ mediany długości kroków agenta). Rozszerzyć G-2 o syntetyczne „wstawienia rozwiązania”. Opisać w PWA zapis Poprawki natychmiast po edycji (debounce) i zmierzyć `czas_od_put_do_zatwierdzenia`. Uwzględnić Sprawdzenie w 17.4 (limit dostawcy, pula R) i przeliczyć odsetek T-7 w 17.7 z rozkładu czasu, a nie z założenia.
- **Status:** OTWARTA

### C3-06: Odtworzenie bazy z PITR zostawia osierocone kopie treningowe, których nie da się usunąć po odwołaniu zgody
- **Waga:** ŚREDNIA
- **Lokalizacja:** 16.4 („Kopie zapasowe PostgreSQL”, procedura p. 1–3; „Magazyn obiektów”: bucket `zbior-treningowy` z regułą 24 miesięcy); 7.3 `kopia_treningowa` („jedyne powiązanie kopii z kontem”); ADR-035 (odrzucony „pseudonim bez powiązania”); E-02 (sprzątanie osieroconych kluczy tylko dla prefiksu `zdjecia/`)
- **Problem:** kopia treningowa utworzona po punkcie odtworzenia `T` istnieje w buckecie `zbior-treningowy/{pseudonim}/…`, ale jej wiersz `kopia_treningowa` przepada przy PITR. `ponow_zdarzenia_rodo` odtwarza tylko Zdarzenia RODO, a utworzenie kopii nim nie jest. Po odtworzeniu kopia nie ma żadnego powiązania z kontem, więc ani odwołanie zgody, ani `USUN_KONTO` (krok 4a) jej nie usuną. Przetrwa do reguły 24 miesięcy. To dokładnie przypadek, który ADR-035 odrzuca, bo zgoda musi być odwołalna (art. 7 ust. 3 RODO). Zdjęcie maskowane i Transkrypcja pisma Ucznia nadal są danymi osobowymi. Podobnie obiekty `zdjecia-maskowane/` po `T` nie są objęte sprzątaniem z E-02 (prefiks `zdjecia/`), chociaż tu zabezpiecza reguła 91 dni.
- **Oczekiwana poprawka:** sprzątanie osieroconych obiektów w `zbior-treningowy/` i `zdjecia-maskowane/` (porównanie z `kopia_treningowa.klucz_obiektu` i `zdjecie_rozwiazania.klucz_obiektu_maskowany`) jako krok procedury odtworzenia przed startem workerów oraz codzienną pracę w tle. Alternatywnie zapisywać utworzenie kopii jako Zdarzenie RODO (`KOPIA_TRENINGOWA_UTWORZONA`) w rejestrze. Dodać scenariusz (d) do G-6.
- **Status:** OTWARTA

### C3-07: Nowe naruszenia słownika: transliteracja „żądanie” koliduje z zarezerwowanym „Zadanie”, a „wariant” wciąż oznacza grupę eksperymentu
- **Waga:** ŚREDNIA
- **Lokalizacja:** 7.3 `typ_zdarzenia_rodo` (`ZADANIE_USUNIECIA`), 12.3, 16.4, ADR-041; 13.1 `OgranicznikSzybkosci.zarezerwuj(…, liczba_zadan: int, …)`; 17.3 `jev_zadania_http_total{tryb}`; 3.1 p. 1, p. 3 i p. 5; 3.4 (test CI, część 3: lista słów zarezerwowanych bez `zadani*`); 3.3 „Grupa eksperymentu” („Przydział Ucznia do **wariantu** eksperymentu A/B”); 10.4.3 Q-RE-01 („rekomendacja w **wariancie** JEV_CHOICE”); 3.4 B-5; 10.3.1 `PoprawkaDoSprawdzenia.obszar: ObszarDanychOsobowych`
- **Problem:**
  1. Reguła 3.1 p. 1 zapisuje identyfikatory w ASCII bez polskich znaków. Przez to „żądanie usunięcia” staje się `ZADANIE_USUNIECIA`, „liczba żądań” — `liczba_zadan`, a „żądania HTTP” — `jev_zadania_http_total`. Wszystkie trzy identyfikatory, nowe w wersji 2.2, zawierają słowo **zarezerwowane** dla Zadania (3.1 p. 3) w zupełnie innym znaczeniu. `ZADANIE_USUNIECIA` w typie wyliczeniowym czyta się jako „Zadanie usunięcia”. To dokładnie ten rodzaj homonimu, który reguła 3.1 p. 5 miała wykluczyć. Test CI (3.4, część 3) pilnuje `odpowiedz*`, `wariant*`, `krok*`, `wynik*` i `kolejk*`, ale nie `zadani*`, a tokenizacja (część 2) przepuszcza `zadanie` jako token słownikowy, więc naruszenie przejdzie CI.
  2. Mimo zmian z C2-11 słowo „wariant” nadal oznacza grupę eksperymentu w definicji pojęcia **Grupa eksperymentu** w 3.3 (czyli w treści, która ma trafić do słownika przez PZS-1 A) oraz w warunku Q-RE-01 w 10.4.3. To wprost łamie 3.1 p. 5 („grupa eksperymentu … nigdy «wariant»”).
  3. Proponowana definicja B-5 (Pewność oceny: „zgodna … co do liczby punktów i wskazania pierwszego błędnego kroku”) pomija brakujący krok, który jest częścią Zgodności Oceny w 3.3 i ADR-027 (kolumna generowana `zgodna`). Słownik i model danych dostałyby dwie różne definicje.
  4. Drobne: typ `ObszarDanychOsobowych` opisuje ramkę **linii kroku** w `PoprawkaDoSprawdzenia.obszar`. Nazwa „Rejestr usunięć” obejmuje teraz także udzielenie i odwołanie zgody, co nie jest usunięciem.
- **Oczekiwana poprawka:** zmienić nazwy, np. `WNIOSEK_O_USUNIECIE` (albo `ZLECENIE_USUNIECIA_KONTA`), `liczba_wywolan`, `jev_wywolania_http_total`. Dopisać do 3.1 p. 3 regułę transliteracji („żądanie” → „wniosek” albo „wywołanie”), a `zadani*` do listy słów zarezerwowanych testu CI (część 3) z listą dozwolonych użyć. W 3.3 i 10.4.3 zastąpić „wariant” słowami „grupa eksperymentu” / „strategia”. Ujednolicić B-5 z ADR-027. Wprowadzić typ `ObszarKroku` i rozważyć nazwę „Rejestr Zdarzeń RODO”.
- **Status:** OTWARTA

### C3-08: Przywrócenie konta Nauczyciela jest stratne, a rozstrzygnięcie w okresie karencji Ucznia przetwarza dane wbrew ADR-040
- **Waga:** NISKA
- **Lokalizacja:** 16.4 („Usunięcie konta Nauczyciela lub Administratora”: „Natychmiast: blokady … zwalniane (szkice usuwane), flagi … zdejmowane, Weryfikacje `KLASA` eskalowane”); 12.3 `POST /admin/uzytkownicy/{id}/przywrocenie` (tabela przywrócenia opisuje tylko Ucznia); 13.6 `KontoPrzywrocone`; ADR-040 („przywrócenie jest bezstratne”, „dane są przechowywane bez przetwarzania”); 9.6.2 (warunki rozstrzygnięcia bez sprawdzenia statusu konta Ucznia); 16.4 tabela przywrócenia (brak ponownego zlecenia `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`)
- **Problem:**
  1. Dla konta Nauczyciela skutki żądania usunięcia są nieodwracalne: szkice są usunięte, flagi `weryfikator_globalny` i `recenzent_tresci` zdjęte, a Weryfikacje przeniesione do `GLOBALNA`. Przywrócenie w 30 dniach nie przywraca żadnego z nich. Nie wiadomo nawet, czy endpoint przywrócenia obsługuje tę rolę.
  2. SQL podjęcia wymaga konta Ucznia `AKTYWNE`, ale rozstrzygnięcie Weryfikacji podjętej przed żądaniem usunięcia nie ma tego warunku (9.6.2). W karencji powstają więc Ocena Nauczyciela, Wynik zadania i Próbka kalibracyjna, co przeczy deklaracji „bez przetwarzania” z ADR-040. Po przywróceniu kopia treningowa tego Rozwiązania nie powstaje, bo tabela przywrócenia nie zleca ponownie `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`.
- **Oczekiwana poprawka:** tabela przywrócenia dla Nauczyciela i Administratora, albo odroczenie zdjęcia flag i eskalacji do końca karencji z zachowaniem eskalacji jako jedynego skutku natychmiastowego. Warunek konta `AKTYWNE` w rozstrzygnięciu (`409 KONTO_NIEAKTYWNE` z zachowaniem szkicu) albo jawny wyjątek w ADR-040. Ponowne zlecenie kopii przy przywróceniu.
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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

STATUS: NEEDS_REVISION
