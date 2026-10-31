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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

### C1-13: FR-25 („zawsze”) jest realizowane przez modę jednego pytania, a nie niezależnie od pewności
- **Waga:** ŚREDNIA
- **Lokalizacja:** 11.3 T-2; 11.1; 11.2.1 N-4
- **Problem:** T-2 odpala się tylko wtedy, gdy Q-OC-01 = tak (moda). Przykład: SymPy zwraca `NIEROZSTRZYGNIETE` dla wyniku, a Q-OC-01 = nie z p = 0,92. Jeśli próg wynosi 0,90 i pozostałe p są wysokie, Rozwiązanie z faktycznie poprawnym wynikiem i błędnym krokiem przechodzi automatycznie (ocena z 0 pkt za wynik). FR-25 wymaga Weryfikacji „niezależnie od pewności oceny”, a tutaj decyzja o FR-25 sama zależy od pewności jednej odpowiedzi.
- **Oczekiwana poprawka:** T-2 ma się uruchamiać, gdy `SymPy = ROWNOWAZNE` **lub** `p_tak(Q-OC-01) ≥ τ` (np. 0,2) przy jednoczesnym błędnym lub brakującym kroku. Próg τ należy umieścić w parametrach (22) i uzasadnić.
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

### C1-20: Wygaśnięcie elementu (24 h) i wersjonowanie Zadania niszczą pracę Ucznia na kartce
- **Waga:** ŚREDNIA
- **Lokalizacja:** 8.5; 12.5 (`POST /rozwiazania`); 15.2 C-17; 12.2 `WERSJA_ZADANIA_NIEAKTUALNA`
- **Problem:**
  1. Zadanie otwarte przechodzi do `ROZPOCZETY` dopiero przy utworzeniu Rozwiązania, a samo otwarcie treści tego nie robi. Uczeń, który rozwiązuje na kartce dłużej, niż zostało do `wygasa_at`, dostaje `409 ELEMENT_SCIEZKI_NIEAKTYWNY` i traci pracę.
  2. Jeśli Nauczyciel zapisze Przegląd `POPRAWIONE` w trakcie pisania, `POST /rozwiazania` zwraca `409 WERSJA_ZADANIA_NIEAKTUALNA`, mimo że Uczeń rozwiązywał wersję, którą widział. C-16 zakłada, że ocena idzie według wersji Ucznia, ale walidacja 12.5 na to nie pozwala.
- **Oczekiwana poprawka:**
  - Przejście `PROPONOWANY → ROZPOCZETY` przy otwarciu treści (albo okres karencji).
  - Przyjmowanie poprzedniej wersji Zadania przez określony czas (np. 24 h od publikacji nowej).
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

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
- **Status:** OTWARTA

### C1-29: Wizja „seria zadań z działu aż do poziomu maturalnego” nie ma odwzorowania, a generowanie systemowe nie ma limitu kosztów
- **Waga:** NISKA
- **Lokalizacja:** 10.1.2; 12.7; 21 R-05
- **Problem:**
  1. Wizja: po Wkładce następuje „seria zadań z tego działu o stopniowo rosnącej trudności, aż do poziomu maturalnego”. Polityka przelicza ranking Umiejętności przy każdym kroku, więc po Wkładce może przeskoczyć do innej Umiejętności. Nie ma pojęcia serii ani warunku jej zakończenia (np. osiągnięcie Poziomu trudności 4).
  2. Limit generowania (12.7) dotyczy tylko Nauczycieli. `UZUPELNIJ_PULE` (system) nie ma dziennego budżetu kosztów, a R-05 wspomina tylko o metryce.
- **Oczekiwana poprawka:**
  - Reguła kontynuacji serii w obrębie Umiejętności lub Działu po Wkładce, z warunkiem wyjścia.
  - Dzienny limit lub budżet kosztów dla generowania systemowego.
- **Status:** OTWARTA

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

STATUS: NEEDS_REVISION
