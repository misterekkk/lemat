# ADR-005: Ocena per kryterium osobnymi pytaniami

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

Otwarte pytanie „całość naraz czy krok po kroku”; FR-21 wymaga sprawdzenia każdego kroku (C1-15).

## Decyzja

SCORE per kryterium + CHOICE pierwszego błędu + CHOICE typu błędu + NOUL wyniku + NOUL wynikania kroków (≤ 12 par) + NOUL podejrzenia błędu odczytu; ≤ 29 pytań, 2 rundy (Jev) lub 1 wywołanie (model zapasowy). Więcej niesprawdzonych par → niespójność N-6 i Weryfikacja.

## Alternatywy

Jedno pytanie SCORE o sumę punktów — brak lokalizacji błędu (FR-27) i słabsza diagnostyka niespójności; limit 40 par — przekracza budżet 17.2.

## Konsekwencje

Więcej wywołań (tanich), reguły niespójności N-1…N-6; żadne niesprawdzone wynikanie nie przechodzi automatycznie.
