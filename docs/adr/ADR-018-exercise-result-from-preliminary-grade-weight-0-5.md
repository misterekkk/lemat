# ADR-018: Wynik zadania z Oceny wstępnej z wagą 0,5; bez Wyniku przy podejrzeniu błędu odczytu

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

FR-29 vs. czas Weryfikacji (dni) — Profil nie może stać w miejscu; FR-28 — błąd odczytu nie może obniżyć Profilu (C1-01); wielokrotne próby nie mogą zawyżać Profilu (C1-21).

## Decyzja

`waga = w_status × w_proba` (10.1.3): Ocena wstępna 0,5 (bez wpływu na `trudnosc_b`), Ocena wstępna z `mozliwy_blad_odczytu` — **brak** Wyniku zadania; kolejna próba tego samego Zadania w 60 dniach — × 0,5. Wynik wstępny jest unieważniany i zastępowany po Ocenie ostatecznej; odtworzenie 10.1.3.

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

Rekomendacje reagują od razu; korekty są w pełni uwzględniane; Profil nie jest obniżany przez podejrzany odczyt.
