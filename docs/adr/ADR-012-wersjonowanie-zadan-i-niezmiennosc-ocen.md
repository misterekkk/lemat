# ADR-012: Wersjonowanie Zadań i niezmienność Ocen

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

FR-14 (poprawki Zadań), FR-34 (korekty Ocen), NFR-05, asynchroniczna Wskazówka.

## Decyzja

`zadanie_wersja` niezmienna (z `zastapiona_at`); Rozwiązanie, Odpowiedź i rozpoczęty element wskazują wersję; Ocena tylko dopisywana z `wersja`; jedyny wyjątek — jednokrotny zapis Wskazówki (trigger `trg_ocena_niezmienna`).

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

Pełna historia; nieco bardziej złożone zapytania (indeks `ux_ocena_aktualna`, stała kolejność „najpierw `ZASTAPIONA`, potem `INSERT`”).
