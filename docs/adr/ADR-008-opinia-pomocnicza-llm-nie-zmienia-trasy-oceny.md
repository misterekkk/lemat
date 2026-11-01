# ADR-008: Opinia pomocnicza LLM nie zmienia trasy Oceny

| Pole | Wartość |
|---|---|
| Status | Przyjęta |
| Data decyzji | 2026-10-31 (iteracja 1, wersja 1.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Przyjęta.

## Kontekst

Architektura wstępna: „pewność < próg → druga ocena przez mocniejszy LLM”; FR-24: „pewność poniżej progu → Weryfikacja”.

## Decyzja

Zgodnie z Z-2 FR-24 ma pierwszeństwo; druga ocena LLM jest asynchroniczną Opinią pomocniczą dla Nauczyciela.

## Alternatywy

Zgoda Jev + LLM jako podstawa automatycznego zaliczenia — odrzucona do czasu, gdy zespół będzie miał własny kalibrator i ≥ 400 próbek (decyzja do ponownego rozpatrzenia w przyszłym ADR).

## Konsekwencje

Zgodność z FR-24; dane do oceny przyszłej automatyzacji.
