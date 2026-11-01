# ADR-057: Sesja w wielu kartach — odświeżanie typu single-flight, okno tolerancji rotacji refresh tokenu i jeden strumień powiadomień na przeglądarkę

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.1, 2026-11-01); p. 5 zastąpiony przez ADR-064 (wersja 3.2) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 2, wersja 3.1) |
| Bramka G-0 | nie |
| Realizuje | C6-02, C6-09 p. 1–3, p. 5; uzupełnia ADR-020, ADR-051, ADR-054 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 6.8, 6.9, 15.2 (C-41, C-42), 16.1, 22; [`docs/spec/ddl.md`](../spec/ddl.md) (`token_odswiezania`) |

## Status

Przyjęta.

## Kontekst

ADR-020 rotuje refresh token przy każdym użyciu i unieważnia całą
`rodzina_id` przy ponownym użyciu tokenu unieważnionego. ADR-054 trzyma
access token wyłącznie w pamięci karty (bez BFF), więc każda karta,
przeładowanie, wznowienie PWA i ponowne połączenie SSE po `exp` wywołuje
`POST /api/v1/auth/odswiezenie` z tym samym cookie `lemat_rt`. Dwie karty
odświeżające w odstępie milisekund dawały „ponowne użycie” i wylogowanie
obu (C6-02). Strumień powiadomień otwierany w każdej karcie przekraczał
limit 3 strumieni i wpadał w pętlę „zamknij najstarszy” (C6-09).

## Decyzja

1. **Klient — single-flight między kartami** (`frontend/src/lib/auth/odswiezanie.ts`):
   - odświeżenie wykonuje się wyłącznie wewnątrz
     `navigator.locks.request("lemat-odswiezenie", { mode: "exclusive" }, …)`
     (Web Locks API, dostępne we wszystkich przeglądarkach A-10);
   - po wejściu do sekcji karta sprawdza, czy podczas oczekiwania nie
     dostała przez `BroadcastChannel("lemat-sesja")` tokenu z `exp − teraz
     ≥ 60 s`; jeśli tak — używa go bez żądania;
   - w przeciwnym razie wysyła `POST /auth/odswiezenie`, a nowy access token
     rozgłasza komunikatem `{ typ: "TOKEN", access_token, exp }`;
   - karta otwierana pierwszy raz wysyła `{ typ: "PROSBA_O_TOKEN" }` i czeka
     150 ms na odpowiedź innej karty, zanim zacznie odświeżanie;
   - wylogowanie rozgłasza `{ typ: "WYLOGOWANIE" }`, a każda karta czyści
     pamięć i przechodzi do `/logowanie`;
   - odświeżenie proaktywne: 60 s przed `exp`; w React 19 Strict Mode
     drugi efekt trafia w tę samą sekcję krytyczną.
   Brak Web Locks (przeglądarka spoza A-10) → odświeżanie bez sekcji
   krytycznej; chroni je wtedy okno tolerancji z p. 2.
2. **Serwer — okno tolerancji rotacji (30 s).** Tabela `token_odswiezania`
   dostaje kolumny `zastapiony_przez`, `zastapiony_at`,
   `skrot_klienta_http` (SHA-256 nagłówka `User-Agent`) i
   `uzycia_w_oknie_tolerancji`. Odświeżenie tokenu T (jedno polecenie
   `UPDATE … WHERE id = @t AND zastapiony_at IS NULL AND uniewazniony_at IS
   NULL RETURNING …` w transakcji):
   - T aktywny → nowy token S tej samej rodziny, `T.zastapiony_przez = S`,
     `T.zastapiony_at = now()`; odpowiedź z nowym access tokenem i
     `Set-Cookie: lemat_rt=<S>`;
   - T zastąpiony, `now() − T.zastapiony_at ≤ 30 s`, ten sam
     `skrot_klienta_http`, następca S aktywny i
     `uzycia_w_oknie_tolerancji < 5` → odpowiedź `200` z **nowym access
     tokenem bez `Set-Cookie`** (przeglądarka zachowuje cookie S ustawione
     odpowiedzią na pierwsze żądanie), `uzycia_w_oknie_tolerancji += 1`;
     serwer nie przechowuje tokenów jawnie, więc nie musi zwracać S;
   - T zastąpiony poza oknem, z innym `skrot_klienta_http`, po 5 użyciach w
     oknie albo T unieważniony → unieważnienie całej rodziny
     (`uniewazniony_at = now()`), `401 SESJA_UNIEWAZNIONA`, wpis
     `dziennik_audytu` `RODZINA_TOKENOW_UNIEWAZNIONA`, metryka
     `tokeny_ponowne_uzycie_total{wynik}`.
   Adres IP nie jest kryterium (sieci komórkowe zmieniają adres przy
   każdym wznowieniu).
3. **Jeden strumień powiadomień na przeglądarkę.** Karta-lider jest
   wybierana przez `navigator.locks.request("lemat-powiadomienia-lider", …)`
   trzymane przez cały czas życia karty; tylko lider otwiera
   `GET /api/v1/powiadomienia/strumien` i przekazuje zdarzenia przez
   `BroadcastChannel("lemat-powiadomienia")`. Zamknięcie karty-lidera zwalnia
   blokadę Web Locks i kolejna karta przejmuje strumień, wznawiając go z
   `Last-Event-ID` ostatniego rozgłoszonego zdarzenia.
4. **Limity strumieni po stronie serwera** (zastępują ADR-051 p. 4
   „≤ 3 na użytkownika, zamykane najstarsze”): na replikę `api` ≤ 4
   strumienie na (użytkownik, zasób) i ≤ 12 na użytkownika; połączenie
   ponad limit dostaje **`429 ZA_DUZO_STRUMIENI`** (z `Retry-After: 30`),
   a istniejące strumienie nie są zamykane — pętla ponownych połączeń nie
   może powstać. Klient po `429` przechodzi dla tego zasobu na odpytywanie
   (ADR-051 p. 5). Limit jest ochroną zasobów repliki, nie regułą
   funkcjonalną, więc nie jest liczony globalnie.
5. **`id:` zdarzenia powiadomienia** = `powiadomienie.id` (UUIDv7
   generowany w aplikacji, `Guid.CreateVersion7()`, monotoniczny w obrębie
   procesu); po `Last-Event-ID` serwer wysyła powiadomienia z `id >
   Last-Event-ID` (porównanie UUIDv7 = porównanie czasu utworzenia).
6. **Service worker** (`app/sw.ts`) nie obsługuje żądań `/api/*`: brak
   trasy Serwist dla tej ścieżki, więc przeglądarka wykonuje je natywnie,
   bez pośrednictwa `fetch` handlera (strumienie SSE nie są buforowane ani
   przerywane przez service worker).
7. **Testy:** Playwright `sesja-dwie-karty.spec.ts` — dwie karty jednej
   sesji, równoczesne wymuszone odświeżenie (zegar przesunięty do `exp −
   30 s`), obie karty zalogowane, jedno żądanie `POST /auth/odswiezenie` przy
   Web Locks; wariant bez Web Locks — dwa żądania, obie karty zalogowane;
   `powiadomienia-dwie-karty.spec.ts` — jeden strumień na przeglądarkę,
   0 ponownych połączeń w 60 s, przejęcie po zamknięciu lidera;
   `Lemat.Api.Tests/Konta/OknoTolerancjiTests` — ponowne użycie w 30 s z tym
   samym `User-Agent` (200, brak `Set-Cookie`), po 31 s (401 i rodzina
   unieważniona), z innym `User-Agent` (401), szóste użycie w oknie (401).

## Alternatywy

- **BFF w Next.js** — odrzucona w ADR-054 (drugi serwer z poświadczeniami).
- **Brak wykrywania ponownego użycia** — odrzucona: kradzież refresh tokenu
  pozostałaby niewykryta przez 30 dni.
- **Access token w `sessionStorage` współdzielony przez karty** — odrzucona:
  `sessionStorage` nie jest współdzielony między kartami, a `localStorage`
  narusza ADR-020 (token dostępny dla skryptów po XSS na stałe).
- **Zamykanie najstarszego strumienia (ADR-051 p. 4 w wersji 3.0)** —
  odrzucona: przy dwóch kartach powoduje pętlę ponownych połączeń.

## Konsekwencje

- Kilka kart i wznowienia PWA nie wylogowują użytkownika; kradzież
  tokenu poza 30-sekundowym oknem albo z innego klienta HTTP nadal
  unieważnia rodzinę.
- Okno tolerancji osłabia wykrywanie ponownego użycia wyłącznie dla
  napastnika z identycznym `User-Agent` w ciągu 30 s od rotacji i najwyżej
  5 razy; ryzyko opisane w 21 (R-26).
- Jeden strumień powiadomień na przeglądarkę zmniejsza liczbę połączeń
  `api` (17.4).

## Uzupełnienia

**Uzupełnienie wersji 3.2 (C7-11).** Punkt 5 („po `Last-Event-ID` serwer
wysyła powiadomienia z `id > Last-Event-ID`”) jest zastąpiony przez ADR-064
p. 3–4: UUIDv7 nie jest monotoniczny w obrębie milisekundy ani między
procesami i jest nadawany przed `COMMIT`, więc nie może być kursorem.
Karta-lider przejmująca strumień (p. 3) dostaje migawkę nieprzeczytanych
powiadomień i scala ją z rozgłoszonymi zdarzeniami po `id`.

## Uzupełnienie wersji 3.3 (C8-05)

Nieudane odświeżenie nie wylogowuje kart, chyba że serwer odpowiedział
`401`; `429`, `5xx` i błąd sieci są ponawiane z odstępem i kończą się
błędem `SESJA_TYMCZASOWO_NIEDOSTEPNA` bez komunikatu `WYLOGOWANIE`
(ADR-070 p. 4). Termin odświeżenia proaktywnego uwzględnia różnicę zegara
urządzenia (ADR-064 uzup. 3.3 p. 4).
