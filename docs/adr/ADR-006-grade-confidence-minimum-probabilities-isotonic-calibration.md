# ADR-006: Pewność oceny = minimum prawdopodobieństw + kalibracja izotoniczna

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

FR-22, NFR-04; wiele decyzji składowych.

## Decyzja

`pewnosc_surowa = min(p)` z ograniczeniem przy niespójności, kalibrator izotoniczny per (konfiguracja, rodzaj) w tabeli `kalibrator_pewnosci`; każda niespójność **niezależnie od kalibratora** wymusza powód `NISKA_PEWNOSC`.

## Alternatywy

Iloczyn prawdopodobieństw (zaniżony, zależny od liczby kryteriów), średnia (maskuje najsłabszą decyzję).

## Konsekwencje

Pewność porównywalna między Zadaniami o różnej liczbie kryteriów; kalibrator naprawia nadmierną pewność min, ale nie może „wyprowadzić” niespójnej Oceny na trasę automatyczną.
