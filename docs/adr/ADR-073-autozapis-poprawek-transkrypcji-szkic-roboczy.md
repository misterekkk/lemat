# ADR-073: Autozapis Poprawek Transkrypcji jako szkic roboczy; wersja Transkrypcji tylko po ustabilizowaniu edycji i przy zatwierdzeniu

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.3, 2026-11-01) |
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
   transkrypcja_id, numer_zapisu, kroki }`) 1,5 s po ostatniej zmianie
   kroku i przy opuszczeniu pola. Serwer wykonuje `INSERT … ON CONFLICT
   (rozwiazanie_id) DO UPDATE … WHERE szkic_transkrypcji.numer_zapisu <
   EXCLUDED.numer_zapisu` (zapis starszy od zapisanego jest pomijany,
   odpowiedź `204` w obu przypadkach), bez `If-Match`, bez zmiany
   `rozwiazanie.version`, bez nowej wersji Transkrypcji i bez
   `NOTIFY`. Walidacja: stan `TRANSKRYPCJA_GOTOWA`, `transkrypcja_id` =
   najnowsza wersja (inaczej `409 TRANSKRYPCJA_NIEAKTUALNA`), reguły
   strukturalne `TranskrypcjaWejscie` (1–40 kroków, ≤ 2000 znaków) i
   limity twarde ADR-036 (`422 POPRAWKI_ZBYT_ROZLEGLE` — Uczeń dowiaduje
   się o przekroczeniu w trakcie edycji). `numer_zapisu` jest licznikiem
   karty (`sessionStorage`, start od `Date.now()` przy otwarciu edytora,
   więc nowa karta ma wyższe numery). `GET /rozwiazania/{id}` dla roli U
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
   (moduł `features/rozwiazania`, `api.md` 6.4) szereguje `PUT …/szkic`,
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
- Testy: Playwright `autozapis-wolna-siec.spec.ts` (opóźnienie 3 s na
  odpowiedź, pisanie przez 20 s → 0 dialogów `412`, tekst w edytorze
  niezmieniony po zdarzeniach `stan`, ≤ 2 wersje Transkrypcji), Vitest
  `kolejkaZapisow.test.ts` (scalanie szkiców, `If-Match` z poprzedniej
  odpowiedzi), `Lemat.Api.Tests/Rozwiazania/SzkicStarszyPomijany`,
  `LimitWersjiTranskrypcji` (21. wersja → `409`).
