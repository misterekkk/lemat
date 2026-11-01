# ADR-026: Plan pytań z zależnościami jako kontrakt portu `ModelDecyzyjny`

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2).

## Kontekst

Pytania Agenta oceniającego są zależne (wariant → kryteria, pierwszy błąd → typ błędu); model zapasowy nie znał wyników wcześniejszych rund (C1-07).

## Decyzja

`PlanPytan` z `WarunekPytania` (głębokość 1); Jev wykonuje plan w 2 rundach, model zapasowy jednym wywołaniem z pytaniami warunkowymi zwiniętymi do pól schematu (13.2).

## Alternatywy

Zadawanie Q-OC-04 dla każdego kroku — odrzucone (do 40 pytań więcej dla Jev); rundy dla modelu zapasowego — odrzucone (budżet).

## Konsekwencje

Agent oceniający niezależny od modelu (NFR-06); pytania warunkowe niespełnione są jawnie zwracane w `pominiete`.
