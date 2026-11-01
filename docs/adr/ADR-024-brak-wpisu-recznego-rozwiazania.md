# ADR-024: Brak wpisu ręcznego Rozwiązania

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2), G-0 |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2), G-0.

## Kontekst

Wizja i słownik: Rozwiązanie to zdjęcie kartki („jak na prawdziwej maturze”); wpis ręczny w wersji 1 był rozszerzeniem spoza wymagań, omijał T-3 i zmieniał rozkład danych kalibracyjnych (C1-26).

## Decyzja

Usunięto `WPIS_RECZNY_UCZNIA`. Po błędzie rozpoznawania Uczeń ma: nowe zdjęcia (≤ 3 przesłania), ponowienie rozpoznania tych samych zdjęć po awarii dostawcy (≤ 5, bez zużycia przesłania), porzucenie i nowe Rozwiązanie dla tego samego elementu do `wygasa_at`.

## Alternatywy

Wpis ręczny z obowiązkową Weryfikacją — odrzucony (obciąża Nauczycieli i nadal pozwala ominąć kartkę).

## Konsekwencje

Transkrypcja zawsze pochodzi ze zdjęcia (PZS-1 B-1) — od wersji 2.2 gwarantują to limity i Sprawdzenie zgodności Poprawek Transkrypcji (ADR-036), bo bez nich nieograniczona edycja Transkrypcji była wpisem ręcznym „tylnymi drzwiami” (C2-01); dane kalibracyjne jednorodne.
