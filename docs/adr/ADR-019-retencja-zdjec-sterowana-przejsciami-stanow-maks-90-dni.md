# ADR-019: Retencja zdjęć sterowana przejściami stanów, maks. 90 dni

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

NFR-09 vs. potrzeba porównania zdjęcia przy Weryfikacji, Kontroli losowej i zgłoszeniu (C1-10, C1-12).

## Decyzja

`usun_po` jako jedyne źródło prawdy, ustawiane przez przejścia z 16.4 (30 dni po ocenie bez otwartej Weryfikacji, przedłużenie do 90 dni przy otwartej, natychmiast po porzuceniu/anulowaniu); twardy limit `CHECK`; buckety bez wersjonowania + reguła cyklu życia 91 dni.

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

Weryfikacja dłuższa niż 90 dni odbywa się bez zdjęcia (SLA 3 dni robocze i alert po 80 dniach czynią to wyjątkiem).
