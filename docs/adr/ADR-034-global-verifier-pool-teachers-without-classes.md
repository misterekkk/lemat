# ADR-034: Pula weryfikatorów globalnych: Nauczyciele bez Klas + Nauczyciele współpracujący

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 3), G-0 |
| Data decyzji | 2026-11-01 (iteracja 3, wersja 2.1) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 3), G-0.

## Kontekst

G-7 wymagało w wersji 2.1 na starcie 12 weryfikatorów globalnych (17.7; od wersji 2.2 — 18, ADR-043), a A-03 przewiduje 50 Nauczycieli, z których 40 prowadzi Klasy — bez Klas jest tylko 10. Wersja 2.0 nie mówiła, skąd pochodzi brakująca pojemność, więc G-7 było niespełnialne przy założeniach A-03 (uzupełnienie C1-02).

## Decyzja

A-14: pula = 10 Nauczycieli bez Klas + ≥ 8 Nauczycieli współpracujących na umowę (wersja 2.1: ≥ 2; zmiana wynika z modelu tygodniowego z eskalacjami, ADR-043); Nauczyciel prowadzący Klasę może dołączyć tylko przy łącznym obciążeniu ≤ 180 min na dzień roboczy (walidacja `422 LIMIT_OBCIAZENIA_WERYFIKATORA`). Plan awaryjny bez rekrutacji: start z `LEMAT_REJESTRACJA_BEZ_KLASY = false` (rejestracja Ucznia tylko z kodem zaproszenia), wtedy kolejka `GLOBALNA` obsługuje wyłącznie eskalacje z Klas i Uczniów, którzy opuścili Klasę.

## Alternatywy

Obowiązkowy przydział każdego Nauczyciela prowadzącego Klasę do puli globalnej — odrzucony (72 + 90 = 162 min/dobę na start, czyli ponad 2,5 h pracy dziennie dla każdego, R-02); Ocena wstępna jako ostateczna po SLA — odrzucona w ADR-021 (łamie FR-24/FR-26).

## Konsekwencje

Koszt operacyjny (umowy z ≥ 8 Nauczycielami na czas fazy startowej, ≥ 4 w fazie ustalonej) albo ograniczenie zakresu pilotażu do Uczniów z Klas — **decyzja właściciela produktu w G-0**; architektura obsługuje oba warianty bez zmian kodu (flaga konfiguracji).
