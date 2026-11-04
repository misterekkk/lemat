# ADR-039: Tryb wsadowy Jev i globalny ogranicznik szybkości

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 4) |
| Data decyzji | 2026-11-01 (iteracja 4, wersja 2.2) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 4).

## Kontekst

Plan pytań ma do 29 pytań (28 w rundzie 1 przy jednym wariancie), a budżet 17.2 zakładał ≤ 2 fale po 8 wywołań; limit A-01 jest per klucz, a pilnował go semafor per proces; `429` otwierał obwód i przełączał ruch na model zapasowy przy działającym Jev (C2-07).

## Decyzja

(1) wymaganie A-01: żądanie wsadowe (≤ 32 pytania na rundę); bez niego tryb `POJEDYNCZY` z ≤ 16 wywołaniami na Ocenę i 3 falami — budżet i S-1…S-7 liczone dla tego trybu; (2) termin miękki `max(10 s, 28 s − t_R)` zgodny z SLO 28 s; (3) globalny ogranicznik: przydział `0,8 × limit × waga / Σ wag` z rejestru `limit_szybkosci_modelu` (wagi worker 1,0, `api` 0,1), lokalny token bucket, oczekiwanie do `termin_miekki − 11 s` (od wersji 2.3 zastąpione przyjęciem planu i terminami fal — uzupełnienie it. 5), potem model zapasowy z przyczyną `OGRANICZENIE_LOKALNE`; (4) `429` i brak przydziału nie liczą się do circuit breakera; `429` zmniejsza przydział o połowę na 30 s.

## Alternatywy

Token bucket w PostgreSQL (wiersz na klucz, `UPDATE … RETURNING` na każde żądanie) — odrzucony (≈ 44 dodatkowe zapisy/s w szczycie i zależność ścieżki krytycznej od bazy); Redis jako licznik — odrzucony (nowy komponent, ADR-002).

## Konsekwencje

Szczyt „koniec lekcji” mieści się w limicie klucza; nadmiar degraduje do modelu zapasowego bez fałszywego otwarcia obwodu; G-4 (d) testuje `429`.

## Uzupełnienia

- **Uzupełnienie (it. 5, wersja 2.3, C3-04):** jedna formuła przydziału `0,8 × limit × waga_i / Σ wag` z kolumną `waga` w `limit_szybkosci_modelu` (DDL, 13.2, 17.4, 22 zgodne); jeden token = jedno wywołanie HTTP; pojemność kubełka workera 16 ≥ maks. fala (kolumna `pojemnosc`; 4 przy ścisłym oknie 1 s po G-4); przyjęcie planu przed falą 1 z przewidywanym końcem ≤ termin miękki, terminy fal ≥ 2 i pierwszeństwo fal ≥ 2 zastępują „oczekiwanie do `termin_miekki − 11 s`”; symulacja profili A–D (17.4) i S-6/S-6b przeliczone.

- **Uzupełnienie (it. 6, wersja 2.4, C4-05, C4-06 p. 8 i 9):** łączna pojemność kubełków `B_całk` (34 przy tolerancji A-01, 10 przy ścisłym oknie) dzielona między wiersze — `api` 1, pozostałe proporcjonalnie do wagi z podłogą — zamiast pojemności stałej per proces; przydział `r_i = min(formuła, 0,8 × limit − Σ_{j≠i} r_j)` i pojemność ograniczona resztą do `B_całk`, publikowane w kolumnach `przydzial` i `pojemnosc` i liczone pod blokadą doradczą klucza; nowy proces z pustym kubełkiem i odnowieniem co 2 s; granica chwilowa ≤ `B_całk` + 40 T niezależnie od liczby replik (2 / 3 / 4 workery: 74 / 72 / 74 w 1 s; wersja 2.3: 74 / 90 / 106); klucz rejestru `(konfiguracja_modelu_id, wykonawca, pula)` z wierszami R i S dla roli `ROZPOZNAWANIE_PISMA`; popyt 28 wywołań na Ocenę w budżecie, S-6 i 17.4 (29 = maksimum pytań). Odrzucona alternatywa: pojemność proporcjonalna do wagi bez wydzielenia `api` (`B_i = B_całk × w_i / Σ w`) — przy 2 workerach daje kubełek 15 < fala 16 i zmienia wyniki S-6 bez zysku dla granicy.

- **Uzupełnienie (pętla 3.x, it. 1, wersja 3.0, C5-03):** CHECK `limit_szybkosci_modelu.pojemnosc BETWEEN 0 AND 34` (w 2.4: 1–34), bo wiersz procesu dołączającego przy pełnym wykorzystaniu i wiersz ponad limit `B_całk − n_api` mają pojemność 0; test (g) w 13.2 sprawdza `pojemnosc = 0` po `INSERT`. Odrzucona alternatywa: pojemność minimalna 1 — narusza niezmiennik `Σ pojemnosc ≤ B_całk` przy dołączaniu w szczycie.
