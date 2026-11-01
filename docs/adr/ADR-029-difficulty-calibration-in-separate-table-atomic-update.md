# ADR-029: Kalibracja Poziomu trudności w osobnej tabeli z atomową aktualizacją

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2).

## Kontekst

Równoległe Wyniki zadania gubiły aktualizacje `trudnosc_b` i podnosiły `zadanie.version`, powodując `412` w Przeglądzie (C1-23).

## Decyzja

Tabela `kalibracja_trudnosci` (`trudnosc_b`, `liczba_wynikow_kalibracji`, skalibrowany `poziom_trudnosci`), atomowy `UPDATE` z blokadą wiersza (10.1.3), histereza 0,1, audyt zmiany poziomu; `zadanie.poziom_trudnosci_nominalny` zmienia tylko człowiek (Przegląd, import).

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

Brak utraconych aktualizacji (C-24); Poziom trudności w API = wartość skalibrowana (słownik), nominalna dostępna osobno (PZS-1 B-4).

## Uzupełnienia

- **Uzupełnienie (it. 5, wersja 2.3, C3-09):** ten sam wzorzec obejmuje Wstrzymanie w doborze (tabela `wstrzymanie_w_doborze` zamiast kolumny `zadanie.wstrzymane_w_doborze`): zmiana systemowa wywołana zgłoszeniem nie podnosi `zadanie.version`, więc recenzent z `If-Match` nie dostaje `412` po każdym nowym zgłoszeniu (powrót problemu C1-23 wykluczony).
