# ADR-073: Autozapis Poprawek Transkrypcji jako szkic roboczy; wersja Transkrypcji tylko po ustabilizowaniu edycji i przy zatwierdzeniu

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.3, 2026-11-01); p. 1 zmieniony w wersji 3.5 (C9-02) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 4, wersja 3.3) |
| Bramka G-0 | nie |
| Realizuje | C8-10; uzupełnia ADR-036 i 9.4 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 9.4, 12.3, 12.11, 15.4 (C-56), 17.4, 22; [`docs/spec/ddl.md`](../spec/ddl.md) `szkic_transkrypcji`; [`docs/spec/api.md`](../spec/api.md) 6.4, 7 |

## Status

Nowa (it. 4 pętli 3.x).

## Kontekst

C8-10: każdy autozapis (`PUT …/transkrypcja`, debounce 1,5 s albo 2 s —
niespójnie) tworzył nową wersję Transkrypcji i pracę
`SPRAWDZ_POPRAWKI_TRANSKRYPCJI`, zmieniał `rozwiazanie.version` (kolejny
autozapis wysłany przed odpowiedzią poprzedniego dostawał `412`) i skrót
stanu SSE (zdarzenie `stan` nadpisywało edytor starszą wersją). 10 minut
edycji dawało do ~400 wersji.

## Decyzja

1. **Szkic roboczy.** Autozapis wysyła `PUT
   /rozwiazania/{id}/transkrypcja/szkic` (`SzkicTranskrypcjiWejscie {
   transkrypcja_id, poprzedni_numer_zapisu, kroki }` — od wersji 3.5;
   w 3.3 `numer_zapisu` z karty) 1,5 s po ostatniej zmianie
   kroku i przy opuszczeniu pola. Serwer wykonuje zapis warunkowy z
   licznikiem nadawanym przez serwer i odpowiada `200
   SzkicTranskrypcjiWyjscie` albo `409 SZKIC_NIEAKTUALNY` (uzupełnienie
   3.5 p. 1; w 3.3: `WHERE numer_zapisu < EXCLUDED.numer_zapisu` i `204`),
   bez `If-Match`, bez zmiany
   `rozwiazanie.version`, bez nowej wersji Transkrypcji i bez
   `NOTIFY`. Walidacja: stan `TRANSKRYPCJA_GOTOWA`, `transkrypcja_id` =
   najnowsza wersja (inaczej `409 TRANSKRYPCJA_NIEAKTUALNA`), reguły
   strukturalne `TranskrypcjaWejscie` (1–40 kroków, ≤ 2000 znaków) i
   limity twarde ADR-036 (`422 POPRAWKI_ZBYT_ROZLEGLE` — Uczeń dowiaduje
   się o przekroczeniu w trakcie edycji). `numer_zapisu` (`long`) nadaje
   serwer, a klient odsyła numer z ostatniej odpowiedzi (uzupełnienie 3.5
   p. 2; licznik karty od `Date.now()` z wersji 3.3 przepełniał `int` —
   C9-02). `GET /rozwiazania/{id}` dla roli U
   zwraca `szkic_transkrypcji` (odtworzenie edytora po przeładowaniu).
2. **Wersja domenowa Transkrypcji** (`PUT …/transkrypcja`, `If-Match: R`,
   nowa wersja `POPRAWKA_UCZNIA` i praca `SPRAWDZ_POPRAWKI_TRANSKRYPCJI`)
   jest wysyłana: (a) gdy edycja jest stabilna — 10 s bez zmiany kroku —
   i szkic różni się od najnowszej wersji; (b) przed zatwierdzeniem
   („Zatwierdzam i oceniam”), jeżeli szkic różni się od najnowszej wersji.
   Zapis wersji usuwa szkic w tej samej transakcji.
3. **Limit wersji:** ≤ 20 wersji `POPRAWKA_UCZNIA` na przesłanie;
   przekroczenie → `409 LIMIT_WERSJI_TRANSKRYPCJI`. Klient nie wysyła
   wersji pośredniej (p. 2a), gdy najnowsza ma numer ≥ 19 wersji
   `POPRAWKA_UCZNIA` przesłania, więc wersja przed zatwierdzeniem (p. 2b)
   jest zawsze możliwa. Model kosztów 17.4: średnio 1,5 Sprawdzenia na
   Rozwiązanie z Poprawkami (jedna pauza ≥ 10 s + zatwierdzenie), górna
   granica 20.
4. **Jedno żądanie w locie na Rozwiązanie.** `KolejkaZapisowRozwiazania`
   (moduł `features/solutions`, `api.md` 6.4) szereguje `PUT …/szkic`,
   `PUT …/transkrypcja` i `POST …/zatwierdzenie`: kolejne żądanie startuje
   po odpowiedzi poprzedniego, `If-Match` bierze z `version` ostatniej
   odpowiedzi, a oczekujące autozapisy szkicu są scalane (wysyłany jest
   tylko najnowszy).
5. **Edytor wobec zdarzeń `stan`.** Gdy edytor ma zmiany niezapisane w
   wersji domenowej, zdarzenie `stan` z tym samym `status` aktualizuje
   wyłącznie pola spoza edytora (`ocena`, `transkrypcja.zgodnosc_poprawek`,
   `zdjecia`, `version`); `transkrypcja.kroki` z serwera nie zastępuje
   tekstu w edytorze. Zmiana `status` (np. `BLAD_ROZPOZNAWANIA` po nowym
   Przesłaniu z innego urządzenia) zamyka edytor z komunikatem „Rozwiązanie
   zmieniło się na innym urządzeniu”, a szkic zostaje w pamięci karty do
   skopiowania.
6. **Debounce** autozapisu szkicu: 1,5 s w każdym miejscu dokumentu (22).

## Alternatywy

- **Wersja Transkrypcji przy każdym autozapisie z limitem** — odrzucona:
  koszt Sprawdzeń i rozrost `WeryfikacjaSzczegoly.transkrypcje`.
- **Szkic wyłącznie w `localStorage`** — odrzucona: utrata przy zmianie
  urządzenia i brak odtworzenia po wylogowaniu.

## Konsekwencje

- Wersji Transkrypcji na przesłanie ≤ 20 (typowo 1–2); `WeryfikacjaSzczegoly.transkrypcje`
  zawiera wersje agenta i `POPRAWKA_UCZNIA` (≤ 21 na przesłanie).
- Szkic jest daną osobową Ucznia: usuwany przy zatwierdzeniu, porzuceniu,
  nowym Przesłaniu, zapisie wersji domenowej i kaskadowo z kontem; nie
  jest pokazywany Nauczycielowi.
- Testy: Playwright `autosave-slow-network.spec.ts` (opóźnienie 3 s na
  odpowiedź, pisanie przez 20 s → 0 dialogów `412`, tekst w edytorze
  niezmieniony po zdarzeniach `stan`, ≤ 2 wersje Transkrypcji), Vitest
  `save-queue.test.ts` (scalanie szkiców, `If-Match` z poprzedniej
  odpowiedzi), `Lemat.Api.Tests/Solutions/SzkicStarszyPomijany`,
  `LimitWersjiTranskrypcji` (21. wersja → `409`). Od wersji 3.5
  `SzkicStarszyPomijany` zastępują testy z uzupełnienia 3.5.

## Uzupełnienie wersji 3.5 (C9-02, C9-07 p. 4)

**Kontekst.** C9-02: `numer_zapisu` startujący od `Date.now()`
(1 793 527 047 367 w dniu przeglądu) przekraczał `int` w C# i `integer` w
DDL, więc każdy autozapis kończył się `422`; po zmianie typu karta
otwarta wcześniej zawsze przegrywałaby z późniejszą, a jej zapisy byłyby
**po cichu** pomijane (`204`). C9-07 p. 4: ładunek szkicu zbudowany w
chwili `dodaj` mógł nieść nieaktualny `transkrypcja_id`.

**Decyzja (zastępuje zdania p. 1 o `numer_zapisu` i odpowiedzi `204`,
test `SzkicStarszyPomijany` przestaje obowiązywać).**

1. **Licznik nadaje serwer.** `SzkicTranskrypcjiWejscie {
   transkrypcja_id, poprzedni_numer_zapisu: long | null, kroki }`;
   odpowiedź `200 SzkicTranskrypcjiWyjscie { transkrypcja_id, numer_zapisu:
   long, kroki, zapisano_at }`. Kolumna `szkic_transkrypcji.numer_zapisu
   bigint` (`ddl.md`). Zapis w jednej instrukcji:
   ```sql
   INSERT INTO szkic_transkrypcji AS s (rozwiazanie_id, transkrypcja_id, kroki, numer_zapisu)
   VALUES (@rozwiazanie_id, @transkrypcja_id, @kroki, 1)
   ON CONFLICT (rozwiazanie_id) DO UPDATE
      SET transkrypcja_id = EXCLUDED.transkrypcja_id, kroki = EXCLUDED.kroki,
          numer_zapisu = s.numer_zapisu + 1, zapisano_at = now()
    WHERE s.numer_zapisu = @poprzedni_numer_zapisu        -- NULL nigdy nie spełnia warunku przy istniejącym szkicu
   RETURNING numer_zapisu, zapisano_at;
   ```
   Brak szkicu → wstawienie z `numer_zapisu = 1` (niezależnie od
   `poprzedni_numer_zapisu`; szkic mógł zostać usunięty przez zapis
   wersji domenowej). 0 wierszy → odczyt szkicu: gdy `kroki` i
   `transkrypcja_id` są równe wysłanym (ponowienie po utraconej
   odpowiedzi albo druga karta z identycznym tekstem) → `200` z zapisanym
   szkicem; w przeciwnym razie → `409 SZKIC_NIEAKTUALNY` z
   `szczegoly.aktualny = SzkicTranskrypcjiWyjscie`. Walidacja stanu,
   `transkrypcja_id` i limitów ADR-036 — bez zmian, przed zapisem.
2. **Źródło `poprzedni_numer_zapisu` w kliencie:** `numer_zapisu` z
   ostatniej odpowiedzi `200` (także z `szczegoly.aktualny` po wyborze
   użytkownika), a przy otwarciu edytora — z `GET /rozwiazania/{id}` →
   `szkic_transkrypcji.numer_zapisu` (`null`, gdy szkicu nie ma). Po
   zapisie wersji domenowej (`PUT …/transkrypcja`, szkic usunięty w tej
   samej transakcji) edytor ustawia `null`.
3. **Konflikt jest jawny.** Edytor po `409 SZKIC_NIEAKTUALNY` wstrzymuje
   autozapis szkicu i wersji pośredniej (p. 2a) i pokazuje pasek „Tekst
   tego Rozwiązania zmieniono w innej karcie lub na innym urządzeniu” z
   przyciskami „Wczytaj zapisany tekst” (edytor przyjmuje
   `aktualny.kroki`, `poprzedni_numer_zapisu = aktualny.numer_zapisu`) i
   „Zachowaj mój tekst” (`PUT …/szkic` z `poprzedni_numer_zapisu =
   aktualny.numer_zapisu` i tekstem z edytora). Zatwierdzenie jest
   zablokowane do wyboru. Edycja przeplatana w dwóch kartach daje więc w
   każdej karcie albo zapis, albo widoczny konflikt — nigdy cichą utratę.
4. **Ładunek szkicu powstaje w `wykonaj`, nie w `dodaj` (C9-07 p. 4).**
   `Zapis.wykonaj` dla `SZKIC` odczytuje w chwili wysłania bieżący stan
   edytora: `kroki`, `transkrypcja_id` najnowszej wersji (po ewentualnym
   `WERSJA` wykonanym wcześniej w kolejce) i `poprzedni_numer_zapisu`
   (`api.md` 6.4). Scalenie szkiców w kolejce nie może więc wysłać
   nieaktualnego `transkrypcja_id`.

**Konsekwencje.**

- `numer_zapisu` rośnie o 1 na zapis (≤ 400 na 10 minut edycji), typ
  `long` jest zapasem; w TS `number` (OpenAPI `int64`, wartości < 2^53).
- Sprawdzone na PostgreSQL 16 (`ddl.md` „Zmiany wersji 3.5”): 1 → 2,
  druga karta z `poprzedni = 1` → 0 wierszy, ponowienie tej samej treści
  → 0 wierszy i równa treść, wartość 1 793 527 047 367 przyjęta.
- Testy: `Lemat.Api.Tests/Solutions/SzkicZNumeremZDateNowJestPrzyjety`
  (szkic z `numer_zapisu = 1 793 527 047 367`, `PUT` z tym
  `poprzedni_numer_zapisu` → `200`, `numer_zapisu = 1 793 527 047 368`),
  `SzkicZDwochKartDajeKonflikt` (A: `null` → 1, A: 1 → 2, B: 1 → `409
  SZKIC_NIEAKTUALNY` z `aktualny.numer_zapisu = 2`),
  `SzkicPonowionyPoUtracieOdpowiedziJestPrzyjety` (A: 1 → 2, A ponawia 1
  z tą samą treścią → `200`, `numer_zapisu = 2`),
  `SzkicBezNumeruPrzyIstniejacymDajeKonflikt`; Vitest `save-queue.test.ts`
  (ładunek z `wykonaj`: `WERSJA` przed `SZKIC` → szkic z nowym
  `transkrypcja_id` i `poprzedni_numer_zapisu = null`); Playwright
  `autosave-slow-network.spec.ts` — scenariusz C-56 „dwie karty”: karta B
  pisze po zapisie w karcie A → pasek konfliktu w karcie B, „Wczytaj
  zapisany tekst” → tekst z karty A, 0 dialogów `412`.
