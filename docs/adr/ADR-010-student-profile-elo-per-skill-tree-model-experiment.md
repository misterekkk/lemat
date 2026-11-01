# ADR-010: Profil ucznia jako model Elo per Umiejętność; model drzewiasty jako eksperyment

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

Potrzeba działania od pierwszego dnia (brak danych do GBM), kalibracji Poziomu trudności i deterministycznego odtwarzania po korektach (C1-11).

## Decyzja

10.1.3; Wyniki z Testu diagnostycznego są zwykłymi zdarzeniami w historii; odtwarzanie całego Profilu ucznia od stanu DOMYŚLNEGO; GBM przez port `PredyktorSukcesu` po zebraniu 20 000 Wyników zadania.

## Alternatywy

BKT (gorzej obsługuje punkty cząstkowe), IRT offline (wymaga dużych danych na starcie); odtwarzanie od „punktu po teście” — odrzucone (podwójne liczenie).

## Konsekwencje

Prosta, deterministyczna aktualizacja; koszt odtworzenia < 50 ms dla ≤ 5 000 Wyników.
