# ADR-033: Weryfikacja: SLA, zwalnianie wygasłych blokad, przedłużanie i szkice

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2).

## Kontekst

W wersji 1 wygasła blokada zostawiała Weryfikację na zawsze w `W_TRAKCIE`, nie było przedłużenia ani autozapisu (C1-06); Kontrola losowa nie miała terminu (C1-10).

## Decyzja

Reguła odczytu „dostępna” + `ZWOLNIJ_BLOKADY_WERYFIKACJI` co minutę; `POST …/przedluzenie` (łącznie ≤ 2 h) i idempotentne podjęcie przez właściciela; `szkic_rozstrzygniecia` per (Weryfikacja, Nauczyciel); `termin_at` dla wszystkich powodów (wersja 2.1: 72 h; od 2.2: 3 dni robocze, ADR-043); eskalacja co godzinę; poprawiony SQL podjęcia (9.6.1).

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

Porzucona Weryfikacja wraca do kolejki w ≤ 1 min; Nauczyciel nie traci pracy przy przejęciu; każda Weryfikacja ma mierzalny termin.

## Uzupełnienia

- **Uzupełnienie (it. 4, wersja 2.2, C2-02, C2-16):** przedłużenie i podjęcie nie zmieniają `version` (ADR-037); ponowne podjęcie przez właściciela nie wydłuża blokady poza `podjeta_at + 2 h`.
