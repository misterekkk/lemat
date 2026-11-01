# ADR-001: Modularny monolit z osobnymi procesami `api`, `worker`, `scheduler`

| Pole | Wartość |
|---|---|
| Status | Zastąpiony przez ADR-048 (wersja 3.0, 2026-11-01); poprzedni status: Przyjęta |
| Data decyzji | 2026-10-31 (iteracja 1, wersja 1.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zastąpiony przez ADR-048 (wersja 3.0, 2026-11-01); poprzedni status: Przyjęta.

**Wyjaśnienie zastąpienia (wersja 3.0).** ADR-048 przejmuje decyzję o modularnym monolicie z trzema procesami (`Lemat.Api`, `Lemat.Worker`, `Lemat.Scheduler` z jednej solucji .NET) bez zmian merytorycznych: brak mikroserwisów per agent, transakcje lokalne w PostgreSQL, niezależne skalowanie procesów. Zmienia się realizacja: (1) zamiast jednego obrazu Pythona powstają obrazy `lemat-backend` (trzy procesy .NET i `Lemat.ImageSandbox`), `lemat-frontend` (Next.js) i `lemat-sympy` (sidecar SymPy, ADR-050); (2) granice modułów egzekwują referencje projektów, test architektury NetArchTest i analizator BannedApiAnalyzers zamiast `import-linter` (sekcja 6).

## Kontekst

Skala A-03, mały zespół, wiele ściśle powiązanych przepływów.

## Decyzja

Jedno repozytorium i jeden obraz; granice modułów egzekwowane `import-linter`; procesy skalowane niezależnie.

## Alternatywy

Mikroserwisy per agent — odrzucone (koszt operacyjny, transakcje rozproszone).

## Konsekwencje

Prostsze wdrożenie i transakcje; ewentualny podział na serwisy wzdłuż modułów jest możliwy bez zmiany kontraktów portów.
