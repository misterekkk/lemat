# ADR-064: Kontrakt strumieni SSE — zdarzenia, migawka stanu przy każdym połączeniu zamiast kursora UUIDv7, heartbeat, zamykanie i ścieżka zapasowa

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.2, 2026-11-01); uzupełnienie 3.3 (C8-02) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 3, wersja 3.2) |
| Bramka G-0 | nie |
| Realizuje | C7-11; zmienia ADR-051 p. 1, p. 3–5 i ADR-057 p. 5 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.10, 15.4; [`docs/spec/api.md`](../spec/api.md) sekcja 5 |

## Status

Nowa (it. 3 pętli 3.x). Zastępuje w ADR-057 p. 5 regułę „po
`Last-Event-ID` serwer wysyła powiadomienia z `id > Last-Event-ID`”.

## Kontekst

ADR-057 p. 5 wznawiał strumień powiadomień od `Last-Event-ID` = UUIDv7.
Krytyk (C7-11) wykazał, że `Guid.CreateVersion7()` nie jest monotoniczny w
obrębie milisekundy, powiadomienia tworzą cztery procesy z niezależnymi
zegarami, a `id` jest nadawane przed `COMMIT` — powiadomienie z
transakcji zatwierdzonej później, ale z mniejszym `id`, ginie po
wznowieniu. Każdy kursor nadawany przed zatwierdzeniem (sekwencja,
`created_at`) ma tę samą wadę; poprawny kursor wymagałby horyzontu
`pg_snapshot_xmin` i okna bezpieczeństwa.

## Decyzja

1. **Strumienie** (wszystkie `GET`, `Content-Type: text/event-stream`,
   `Cache-Control: no-cache, no-store`, `X-Accel-Buffering: no`,
   implementacja `TypedResults.ServerSentEvents(IAsyncEnumerable<SseItem<T>>)`):

   | Ścieżka | Polityka i uprawnienie | Zdarzenia (`event:`) | Zamknięcie przez serwer |
   |---|---|---|---|
   | `/api/v1/rozwiazania/{id}/strumien` | `Uczen`, właściciel Rozwiązania | `stan`, `koniec`, `ping`, `wznow`, `blad` | po `koniec` |
   | `/api/v1/powiadomienia/strumien` | `Zalogowany` | `migawka`, `powiadomienie`, `ping`, `wznow`, `blad` | po `wznow` |
   | `/api/v1/zlecenia-generowania/{id}/strumien` | `NauczycielLubAdministrator`, zlecający albo Administrator | `stan`, `koniec`, `ping`, `wznow`, `blad` | po `koniec` |
   | `/api/v1/admin/arkusze-cke/importy/{id}/strumien` | `Administrator` | `stan`, `koniec`, `ping`, `wznow`, `blad` | po `koniec` |

   Zdarzenie `wskazowka` z ADR-051 jest scalone ze `stan` (zapis
   Wskazówki zmienia `RozwiazanieWyjscie.ocena`, a trigger
   `trg_ocena_powiadom` wysyła `NOTIFY` dla Rozwiązania).
2. **Strumienie stanu zasobu** (`stan`): każde zdarzenie niesie pełny DTO
   tego samego typu co `GET` zasobu; `id:` = pierwsze 16 znaków
   szesnastkowych SHA-256 zserializowanego `data`. Po połączeniu serwer
   (a) rejestruje subskrypcję w `KoncentratorZdarzen`, (b) czyta stan z
   bazy, (c) wysyła `stan`, chyba że skrót równa się `Last-Event-ID`.
   Kolejność a → b wyklucza lukę między odczytem a subskrypcją. Każde
   `NOTIFY` zasobu powoduje odczyt i wysłanie `stan`, jeśli skrót się
   zmienił. Stan terminalny (8.1: `OCENIONE` bez Wskazówki w przygotowaniu,
   `PORZUCONE`, `ANULOWANE`, `BLAD_ROZPOZNAWANIA`; zlecenie `ZAKONCZONE`/`NIEUDANE`;
   import `ZAKONCZONY`/`ODRZUCONY`) → `koniec` z tym samym `data` i
   zamknięcie. `BLAD_ROZPOZNAWANIA` jest terminalny dla strumienia, bo
   kolejny krok wymaga akcji Ucznia (nowe przesłanie otwiera strumień
   ponownie).
3. **Strumień powiadomień** (`migawka` + `powiadomienie`): pierwsze
   zdarzenie po **każdym** połączeniu to `migawka` z `{ nieprzeczytane:
   PowiadomienieWyjscie[] (≤ 100 najnowszych), liczba_nieprzeczytanych }`,
   odczytana po rejestracji subskrypcji; potem `powiadomienie` dla każdego
   `NOTIFY` typu `POWIADOMIENIE` z `uzytkownik_id` wywołującego. `id:`
   zdarzenia `powiadomienie` = `powiadomienie.id`; `id:` migawki = `m`.
   **Serwer nie używa `Last-Event-ID` tego strumienia**; klient scala
   migawkę i zdarzenia po `id` (zbiór ostatnich 500 identyfikatorów w
   pamięci karty-lidera). Powiadomienie zatwierdzone w dowolnej kolejności
   względem innych jest albo nieprzeczytane — wtedy jest w migawce przy
   wznowieniu — albo przeczytane na innym urządzeniu, wtedy jego
   pominięcie w strumieniu jest zamierzone (lista `GET /powiadomienia`
   pokazuje je dalej).
4. **Kursor wznowienia nie zależy od kolejności nadawania `id`**
   (C7-11): strumienie stanu porównują skrót stanu, strumień powiadomień
   wysyła migawkę. UUIDv7 `powiadomienie.id` pozostaje kluczem i porządkiem
   wyświetlania (sortowanie po `created_at, id`), ale nie kursorem.
5. **Heartbeat i wykrywanie zerwania:** co 15 s zdarzenie `ping` z `data:
   {}` (`SseItem` nie obsługuje komentarzy; klient je ignoruje).
   Klient bez żadnego zdarzenia przez 45 s zamyka połączenie i łączy się
   ponownie (sieć komórkowa po zmianie stacji często zostawia półotwarte
   TCP).
6. **Zamykanie przez serwer:** po `min(exp tokenu, now() + 10 min)` albo
   przy zatrzymaniu procesu (`IHostApplicationLifetime.ApplicationStopping`)
   serwer wysyła `wznow` z `{ powod: "WYGASNIECIE_TOKENU" | "LIMIT_CZASU" |
   "ZAMYKANIE_SERWERA" }` i kończy odpowiedź; klient łączy się ponownie
   natychmiast (po odświeżeniu tokenu, gdy `powod = WYGASNIECIE_TOKENU`).
   Pierwsze zdarzenie ma `retry: 3000`. Błąd po rozpoczęciu strumienia
   (np. utrata uprawnień przy zmianie Klasy) → `blad` z ProblemDetails
   (ADR-060) i zamknięcie; przed rozpoczęciem — zwykła odpowiedź
   ProblemDetails (`401`, `404`, `429 ZA_DUZO_STRUMIENI`).
7. **Przeciążenie subskrybenta:** kanał subskrybenta ma pojemność 16;
   dla strumieni stanu przy przepełnieniu najstarsze `NOTIFY` jest
   odrzucane (stan i tak jest czytany na nowo); dla strumienia powiadomień
   przepełnienie kończy strumień zdarzeniem `wznow` (`LIMIT_CZASU`), a
   ponowne połączenie dostaje migawkę — żadne powiadomienie nie ginie.
8. **Ponowne łączenie i ścieżka zapasowa (klient `stream.ts`):**
   odstęp 1, 2, 4, 8, 16, 30 s (maks.) z losowym rozrzutem ±20%, zerowany
   po pierwszym zdarzeniu; `401` → odświeżenie tokenu (ADR-057) i jedno
   ponowienie; `403`/`404` → koniec bez ponowień; `429` albo dwie nieudane
   próby w ciągu 10 s albo brak pierwszego zdarzenia w ciągu 5 s od
   odpowiedzi `200` (proxy buforujące) → **odpytywanie** (`GET` zasobu co
   `nastepne_odpytanie_ms`, domyślnie 1 000 ms; powiadomienia `GET
   /powiadomienia?nieprzeczytane=true` co 30 s) i próba SSE co 60 s.
   Karta ukryta (`document.visibilityState = "hidden"`) dłużej niż 5 min
   zamyka strumienie; po powrocie otwiera je ponownie (migawka/stan).
9. **Testy** (`Lemat.Api.Tests/Streams`, `WebApplicationFactory` +
   PostgreSQL 16): `DwaPowiadomieniaWOdwroconejKolejnosciNieGinaPoWznowieniu`
   (T1 wstawia powiadomienie A i czeka, T2 wstawia B i zatwierdza, klient
   odbiera B i rozłącza się, T1 zatwierdza A, klient łączy się z
   `Last-Event-ID = B.id` → migawka zawiera A i B);
   `StanZTymSamymSkrotemNieJestWysylanyPonownie`; `KoniecZamykaStrumien`;
   `WygasniecieTokenuWysylaWznow`; `PrzepelnienieKanaluPowiadomienKonczyWznow`;
   Playwright `sse-fallback-polling.spec.ts` (Caddy z buforowaniem →
   przejście na odpytywanie w ≤ 7 s).

## Alternatywy

- **Kursor `(xid8, id)` z horyzontem `pg_snapshot_xmin(pg_current_snapshot())`**
  — odrzucona: poprawny, ale wymaga kolumny `xid8`, zapytania z horyzontem
  i okna na powiadomienia już wysłane na żywo; migawka nieprzeczytanych jest
  prostsza i wystarcza, bo strumień niesie wyłącznie powiadomienia do
  przeczytania.
- **`created_at` z marginesem 30 s i deduplikacją** — odrzucona: długość
  transakcji nie jest w PostgreSQL 16 twardo ograniczona (`transaction_timeout`
  jest od wersji 17), więc margines nie daje gwarancji.
- **Komentarze SSE (`: ping`) jako heartbeat** — odrzucona w realizacji:
  `System.Net.ServerSentEvents.SseItem<T>` nie ma komentarzy, a ręczny
  zapis obok formatera komplikuje kod bez zysku.

## Konsekwencje

- Każde połączenie strumienia powiadomień kosztuje jedno zapytanie po
  `ix_powiadomienie_nieprzeczytane` (≤ 100 wierszy).
- Zmiana kolejności zatwierdzeń transakcji nie gubi powiadomień; test p. 9
  jest bramką CI.
- Kontrakty zdarzeń są w OpenAPI (`x-lemat-zdarzenia`, ADR-061 p. 3).

## Uzupełnienie wersji 3.3 (C8-02, C8-12 p. 10)

1. **Pierwsze zdarzenie po każdym połączeniu (zmienia p. 2 c).** Strumień
   stanu wysyła `stan` (albo `koniec`) natychmiast po rejestracji
   subskrypcji i odczycie stanu **niezależnie od `Last-Event-ID`**;
   porównanie skrótu z `Last-Event-ID` dotyczy wyłącznie kolejnych zdarzeń
   tego samego połączenia. Reguła „brak pierwszego zdarzenia w ciągu 5 s →
   odpytywanie” (p. 8) jest więc spełniana przez każdy poprawny serwer, a
   wznowienie po `wznow` w stanie niezmienionym (`TRANSKRYPCJA_GOTOWA`,
   `OCZEKUJE_NA_WERYFIKACJE`) nie przełącza klienta na odpytywanie. Koszt:
   jedno zdarzenie (≤ 4 KB) na połączenie, czyli ≤ 6 na godzinę na kartę.
   Test `StanZTymSamymSkrotemNieJestWysylanyPonownie` dotyczy kolejnych
   zdarzeń, a nowy `WznowienieZawszeWysylaPierwszyStan` — połączenia z
   `Last-Event-ID` = skrót bieżącego stanu (oczekiwane `stan` w ≤ 1 s).
2. **Odpytywanie zapasowe kończy się przy `nastepne_odpytanie_ms = null`
   (zmienia p. 8).** `null` oznacza stan bez zmian samoczynnych; klient
   przerywa pętlę odpytywania i wraca do SSE (`polacz()`), a gdy SSE nadal
   nie działa — odpytuje co 30 s (stała `OKRES_ODPYTYWANIA_SPOCZYNKU_MS`),
   nigdy z opóźnieniem 0 ms. Opóźnienie odpytywania jest zawsze ≥ 1 000 ms.
3. **`401` przy otwarciu strumienia:** najwyżej jedno odświeżenie tokenu i
   jedno ponowienie na połączenie (licznik `ponowienia401`); drugi `401`
   kończy strumień i przekazuje sterowanie `DostawcaSesji` (wylogowanie
   wyłącznie przy `401 SESJA_UNIEWAZNIONA` z odświeżenia, ADR-070 p. 4).
4. **Różnica zegara.** `refresh.ts` liczy `roznicaZegara =
   Date.parse(naglowek Date) − Date.now()` z każdej odpowiedzi
   `/auth/odswiezenie` i `/auth/logowanie`, a termin odświeżenia
   proaktywnego = `wygasa_at − 60 s` porównuje z `Date.now() +
   roznicaZegara`.
5. **Migawka powiadomień > 100 nieprzeczytanych (C8-12 p. 10).**
   `MigawkaPowiadomien` ma pole `nastepny_kursor` (kursor ADR-062 p. 5 dla
   `GET /powiadomienia?nieprzeczytane=true`, sortowanie `created_at DESC,
   id`); gdy `liczba_nieprzeczytanych > 100`, klient dociąga pozostałe
   strony tym kursorem, więc powiadomienie starsze od 100 najnowszych nie
   ginie.
6. **Testy Vitest `stream.test.ts`:** „wznow przy niezmienionym stanie nie
   przełącza na odpytywanie”, „null zatrzymuje odpytywanie i wraca do SSE”,
   „drugi 401 kończy strumień”, „opóźnienie odpytywania ≥ 1000 ms”.
