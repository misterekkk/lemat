# ADR-025: NFR-01 jako czas systemowy; termin miękki i twardy; Wskazówka asynchroniczna

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2), G-0 |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2), G-0.

## Kontekst

Wersja 1 mierzyła NFR-01 od zatwierdzenia i liczyła T-6 jako sukces, przez co NFR-01 było spełnione z definicji (C1-08); Agent wyjaśniający był na ścieżce krytycznej.

## Decyzja

A-11 i 17.1: pomiar obejmuje rozpoznanie i ocenę; T-6 i błędy dostawcy są naruszeniami; termin miękki skraca timeouty, termin twardy 120 s kończy próby Oceną albo T-6; Wskazówka powstaje w pracy `NAPISZ_WSKAZOWKE` po zapisie Oceny.

## Alternatywy

Pomiar od przesłania z czasem Ucznia na sprawdzenie Transkrypcji — odrzucony (system nie kontroluje czasu Ucznia, a FR-18 go wymaga); twardy termin 28 s z T-6 — odrzucony (zamieniał spóźnione Oceny na brak Oceny).

## Konsekwencje

NFR-01 mierzalne i niespełnialne „z definicji” tylko przez poprawne działanie; Wskazówka pojawia się do ~10 s po Ocenie.
