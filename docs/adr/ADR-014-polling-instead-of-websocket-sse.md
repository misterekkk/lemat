# ADR-014: Odpytywanie (polling) zamiast WebSocket/SSE

| Pole | Wartość |
|---|---|
| Status | Zastąpiony przez ADR-048 (wersja 3.0, 2026-11-01); poprzedni status: Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zastąpiony przez ADR-048 (wersja 3.0, 2026-11-01); poprzedni status: Zmieniona (it. 2).

**Wyjaśnienie zastąpienia (wersja 3.0).** Odpytywanie co 1 s przestaje być mechanizmem podstawowym. Postęp Rozwiązania, Wskazówka w przygotowaniu, powiadomienia oraz długotrwałe operacje Nauczyciela i Administratora są przekazywane strumieniem SSE (`text/event-stream`) zasilanym przez `LISTEN/NOTIFY` PostgreSQL — ADR-051. `GET /rozwiazania/{id}` z polem `nastepne_odpytanie_ms` pozostaje ścieżką zapasową, gdy strumień nie działa (sieć komórkowa, proxy); budżet NFR-01 z A-11 (28 s serwera + 2 s dostarczenia) pozostaje bez zmian.

## Kontekst

NFR-08 (sieci komórkowe), prostota infrastruktury, budżet NFR-01.

## Decyzja

`GET /rozwiazania/{id}` co 1 s w stanach przejściowych i przy Wskazówce w przygotowaniu, z podpowiedzią `nastepne_odpytanie_ms`; powiadomienia co 60 s.

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

≤ 1 s dodatkowego opóźnienia na fazę (ujęte w 17.2 jako różnica 28 s / 30 s); w szczycie lekcji ≤ 90 żądań/s — pomijalne dla `api`.
