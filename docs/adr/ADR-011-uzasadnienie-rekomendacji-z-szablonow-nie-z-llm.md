# ADR-011: Uzasadnienie rekomendacji z szablonów, nie z LLM

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

FR-10, NFR-05; ryzyko halucynacji liczb.

## Decyzja

Szablony 10.1.5 wypełniane faktami zapisanymi w `uzasadnienie_fakty`, dla **każdej** wartości `regula` (także `R-JEV-*`, `R-SERIA`, `R-UTRWALENIE`, `R-WKLADKA-DIAG`) i dla braku elementu; test pokrycia.

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

Uzasadnienie zawsze prawdziwe i audytowalne; mniej „naturalny” język.
