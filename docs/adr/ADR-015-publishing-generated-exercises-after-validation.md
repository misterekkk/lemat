# ADR-015: Publikacja Zadań wygenerowanych po Walidacji; Wkładki i dowody tylko po przeglądzie

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

FR-13, FR-14; skala wymaga automatyzacji, ale dowodów nie da się sprawdzić symbolicznie; dowody bez zlecającego utykały w `DO_PRZEGLADU` (C1-22).

## Decyzja

Obliczeniowe i zamknięte → `OPUBLIKOWANE` (`GLOBALNY`) po pozytywnej Walidacji; dowody i Wkładki → `DO_PRZEGLADU`, publikuje recenzent treści lub Administrator (ADR-028). Generowanie systemowe (`UZUPELNIJ_PULE`) nigdy nie tworzy dowodów (CHECK w `zlecenie_generowania`); szkice Wkładek powstają także automatycznie przy E-20.

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

Każde Zadanie i Wkładka w `DO_PRZEGLADU` ma uprawnionego recenzenta; FR-06 jest pokryte dla obu poziomów (G-5).
