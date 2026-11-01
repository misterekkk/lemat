# ADR-051: SSE dla postępu długotrwałych operacji; odpytywanie jako ścieżka zapasowa

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.0, 2026-11-01); uzupełnienie wersji 3.1 — sekcja „Uzupełnienia”; uzupełnienie 3.2 (ADR-064) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 1, wersja 3.0) |
| Bramka G-0 | nie |
| Realizuje | ADR-048 p. 4; zastępuje ADR-014 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 5.1, 5.2, 6.8, 12.1, 17.2, 22 |

## Status

Przyjęta. Kontrakty zdarzeń (pełne schematy) dopisuje iteracja 3 w 12.x.

## Kontekst

ADR-014 wybrał odpytywanie `GET /rozwiazania/{id}` co 1 s w stanach
przejściowych i powiadomień co 60 s, aby uprościć infrastrukturę. Koszt:
do 1 s opóźnienia na fazę (budżet A-11: 28 s serwera + odpytywanie), do
90 żądań/s w szczycie „koniec lekcji” i opóźnienie powiadomień o
Weryfikacjach do 60 s. Nowy stos (ASP.NET Core 10:
`TypedResults.ServerSentEvents`, Caddy z HTTP/2) wspiera SSE bez
dodatkowych komponentów, a właściciel projektu wskazał REST + SSE jako
docelową komunikację.

## Decyzja

1. **Strumienie SSE** (`Content-Type: text/event-stream`, HTTP/2 przez
   Caddy, `flush_interval -1`):

   | Strumień | Zdarzenia (`event:`) | Dane (`data:`, JSON snake_case) | Zamknięcie przez serwer |
   |---|---|---|---|
   | `GET /api/v1/rozwiazania/{id}/strumien` (właściciel Rozwiązania) | `stan` — każda zmiana `rozwiazanie.version`; `wskazowka` — zapis Wskazówki do Oceny aktualnej; `koniec` — stan terminalny bez Wskazówki w przygotowaniu | `RozwiazanieWyjscie` (ten sam DTO co `GET /rozwiazania/{id}`) | po `koniec` |
   | `GET /api/v1/powiadomienia/strumien` (każda rola) | `powiadomienie` | `PowiadomienieWyjscie` | po 10 min (klient wznawia) |
   | `GET /api/v1/admin/arkusze-cke/importy/{id}/strumien` (Administrator) | `stan`, `koniec` | `ImportArkuszaWyjscie` | po stanie `ZAKONCZONY`/`ODRZUCONY` |
   | `GET /api/v1/zlecenia-generowania/{id}/strumien` (zlecający, Administrator) | `stan`, `koniec` | `ZlecenieGenerowaniaWyjscie` | po `ZAKONCZONE`/`NIEUDANE` |

   Pierwsze zdarzenie po połączeniu to zawsze bieżący stan (`stan` albo
   lista nieprzeczytanych powiadomień), więc klient nie potrzebuje
   osobnego `GET`. `id:` zdarzenia = `"<version>.<0|1>"` (wersja zasobu i
   znacznik Wskazówki) albo `created_at` powiadomienia; po ponownym
   połączeniu z `Last-Event-ID` serwer wysyła stan bieżący, jeśli różni się
   od wskazanego.
2. **Źródło zdarzeń:** transakcja zmieniająca stan (worker albo `api`)
   wysyła `NOTIFY lemat_zdarzenia, '{"typ":"ROZWIAZANIE","id":"<uuid>"}'`
   (typy: `ROZWIAZANIE`, `POWIADOMIENIE`, `IMPORT_ARKUSZA`,
   `ZLECENIE_GENEROWANIA`; ładunek wyłącznie typ i identyfikator, ≤ 200 B)
   w tej samej transakcji przez `SaveChangesInterceptor`; PostgreSQL
   dostarcza powiadomienie po zatwierdzeniu. Każda replika `api` ma jedno
   dedykowane połączenie Npgsql `LISTEN lemat_zdarzenia`
   (`NasluchPowiadomien`) i koncentrator `KoncentratorZdarzen`
   (`Channel<T>` na subskrybenta, pojemność 16, przy przepełnieniu
   najstarsze zdarzenie `stan` jest zastępowane najnowszym). Po
   powiadomieniu `api` odczytuje aktualny stan zasobu z bazy (uprawnienia
   sprawdzone przy otwarciu strumienia, 12.8) i wysyła zdarzenie.
3. **Uwierzytelnianie:** nagłówek `Authorization: Bearer` (ADR-020 bez
   zmian). Klient używa `fetch` + `eventsource-parser`, bo `EventSource`
   nie wysyła nagłówków; token w URL i cookie z access tokenem są
   zakazane. Serwer zamyka strumień najpóźniej w chwili `exp` tokenu albo
   po 10 min (wcześniejsze), a klient łączy się ponownie z odświeżonym
   tokenem.
4. **Odporność:** heartbeat (komentarz `: ping`) co 15 s; limity ≤ 3
   strumienie na użytkownika i ≤ 1 na (użytkownik, Rozwiązanie) —
   nadmiarowe połączenie zamyka najstarsze; po zerwaniu połączenia
   `NasluchPowiadomien` łączy się ponownie z wykładniczym odstępem (0,5 s …
   10 s) i po odtworzeniu wysyła wszystkim subskrybentom bieżący stan.
5. **Ścieżka zapasowa:** klient, który dwa razy w ciągu 10 s nie
   nawiąże strumienia (proxy buforujące, sieć komórkowa), przechodzi na
   odpytywanie `GET /rozwiazania/{id}` co `nastepne_odpytanie_ms` (1 000 ms,
   jak w 12.1) i co 60 s próbuje ponownie SSE. Pole
   `nastepne_odpytanie_ms` pozostaje w `RozwiazanieWyjscie`.
6. **NFR-01:** pomiar A-11 bez zmian (≥ 99% z `czas_systemowy_ms ≤ 28 000`
   po stronie serwera + ≤ 2 s dostarczenia); dostarczenie przez SSE ma cel
   p99 ≤ 300 ms od zatwierdzenia transakcji, a ścieżka zapasowa — ≤ 1 s,
   więc margines 2 s z A-11 obejmuje oba tryby.

## Alternatywy

- **Pozostanie przy odpytywaniu (ADR-014)** — odrzucone: do 1 s
  opóźnienia na fazę i ≈ 90 żądań/s w szczycie bez potrzeby; powiadomienia
  o Weryfikacjach opóźnione do 60 s.
- **WebSocket / SignalR** — odrzucone: komunikacja jest jednokierunkowa
  (serwer → klient), SignalR wymaga backplane'u albo sticky sessions przy
  2 replikach `api`, a SSE przechodzi przez zwykłe HTTP/2 i Caddy bez
  zmian.
- **`EventSource` z tokenem w parametrze zapytania** — odrzucone: token
  trafiałby do logów proxy i historii (naruszenie ADR-020).
- **Cookie z access tokenem dla SSE** — odrzucone: wymagałoby ochrony CSRF
  i zmiany modelu sesji ADR-020.
- **Redis Pub/Sub jako źródło zdarzeń** — odrzucone: nowy komponent;
  `LISTEN/NOTIFY` wystarcza (≤ 300 zdarzeń/s przy 20× A-03).

## Konsekwencje

- Niższe opóźnienie widoczne dla Ucznia i mniej żądań w szczycie; jedno
  połączenie `LISTEN` na replikę `api` (5.4).
- Proces `api` utrzymuje długie połączenia: przy A-03 ≤ 150 jednoczesnych
  strumieni na replikę (Rozwiązania w toku + powiadomienia aktywnych
  użytkowników), co mieści się w domyślnych limitach Kestrel.
- Caddy musi przepuszczać strumienie bez buforowania (`flush_interval -1`)
  i bez limitu czasu odczytu dla ścieżek `*/strumien`.
- Kontrakty zdarzeń są częścią OpenAPI (iteracja 3) i testów
  `Lemat.Api.Tests` (otwarcie, `Last-Event-ID`, zamknięcie po `koniec`,
  limit strumieni).

## Uzupełnienia

**Uzupełnienie wersji 3.1 (C6-01, C6-09; ADR-056, ADR-057).**
(1) `NOTIFY lemat_zdarzenia` wysyłają wyłącznie triggery z listy ADR-056
p. 3 (rozwiązanie, ocena, weryfikacja, powiadomienie, import arkusza,
zlecenie generowania), nie `SaveChangesInterceptor`; powiadomienie dociera
po `COMMIT`, nigdy po `ROLLBACK`. (2) Limit strumieni z p. 4 zastąpiony:
≤ 4 strumienie na (użytkownik, zasób) i ≤ 12 na użytkownika na replikę,
nadmiarowe połączenie dostaje `429 ZA_DUZO_STRUMIENI` (istniejące nie są
zamykane); strumień powiadomień otwiera wyłącznie karta-lider przeglądarki
(Web Locks `lemat-powiadomienia-lider`, `BroadcastChannel`). (3) `id:`
zdarzenia `powiadomienie` = `powiadomienie.id` (UUIDv7), nie `created_at`.
(4) Service worker nie obsługuje `/api/*` (ADR-057 p. 6).

**Uzupełnienie wersji 3.2 (C7-11, ADR-064).** Pełny kontrakt strumieni
(zdarzenia `stan`, `koniec`, `migawka`, `powiadomienie`, `ping`, `wznow`,
`blad`, schematy `data:`, `id:`, zamykanie, heartbeat, ścieżka zapasowa) jest
w ADR-064 i `docs/spec/api.md` 5. Zmiany względem p. 1–5: zdarzenie
`wskazowka` scalone ze `stan`; `id:` zdarzeń stanu = skrót SHA-256 danych
(zamiast `"<version>.<0|1>"`, bo stan „opóźniona” Weryfikacji nie zmienia
`rozwiazanie.version`); heartbeat jako zdarzenie `ping` co 15 s; strumień
powiadomień zaczyna każde połączenie migawką nieprzeczytanych i nie używa
`Last-Event-ID`.
