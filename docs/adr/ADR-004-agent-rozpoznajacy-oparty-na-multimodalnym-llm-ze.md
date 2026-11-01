# ADR-004: Agent rozpoznający oparty na multimodalnym LLM ze strukturyzowanym wyjściem

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

Potrzebne jednocześnie: LaTeX, podział na kroki, oznaczenie niepewności, ramki danych osobowych.

## Decyzja

Rola `ROZPOZNAWANIE_PISMA` = multimodalny LLM z wymuszonym schematem JSON; post-processing SymPy obniża pewność niepoprawnego LaTeX. Sygnały LLM (`pewnosc_odczytu`, `czytelne`) działają wyłącznie w kierunku bezpiecznym (Z-1). Decyzję o podejrzeniu błędu odczytu wpływającym na Ocenę podejmuje Model decyzyjny (Q-OC-04 `BLAD_ODCZYTU`, Q-OC-06), a nie samoocena LLM.

## Alternatywy

Wyspecjalizowany OCR wzorów — daje LaTeX, ale bez podziału na kroki i bez znaczników niepewności; pozostaje kandydatem do porównania na ZT-1 (G-2) jako alternatywny adapter.

## Konsekwencje

Nieskalibrowana samoocena LLM nie jest jedyną ochroną FR-28: działają trzy niezależne mechanizmy — poprawki Ucznia (FR-18), decyzje Modelu decyzyjnego (T-3) i zgłoszenie Ucznia po Ocenie (11.6); skuteczność mierzy 11.8.
