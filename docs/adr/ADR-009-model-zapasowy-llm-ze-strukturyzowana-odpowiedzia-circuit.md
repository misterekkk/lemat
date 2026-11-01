# ADR-009: Model zapasowy: LLM ze strukturyzowaną odpowiedzią + circuit breaker + osobna kalibracja

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

NFR-07, A-02; pytania Agenta oceniającego są zależne (C1-07).

## Decyzja

9.9; jedna Ocena = jeden model decyzyjny; model zapasowy wykonuje cały `PlanPytan` jednym wywołaniem (pytania warunkowe zwinięte, 13.2); własny kalibrator i próg; ponowienie Jev tylko przy zapasie ≥ 13 s do terminu miękkiego; obwód otwiera się po 3 kolejnych błędach.

## Alternatywy

Przełączanie per pytanie (mieszanie rozkładów o różnej kalibracji) — odrzucone; model zapasowy w rundach jak Jev — odrzucony (2–3 wywołania LLM po 6 s przekraczają budżet).

## Konsekwencje

Najgorszy przypadek z przełączeniem w trakcie Oceny mieści się w SLO (17.2, S-1…S-3); przy awarii Jev rośnie odsetek Weryfikacji, ale NFR-03 pozostaje spełnione.
