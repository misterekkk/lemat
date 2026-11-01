# ADR-021: Kolejka weryfikacji globalna dla Uczniów bez Klasy

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2), G-0 |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2), G-0.

## Kontekst

Uczeń może korzystać z platformy bez Nauczyciela (FR-01), a Weryfikacja jest obowiązkowa (FR-24…FR-26); w wersji 1 brakowało gwarancji istnienia weryfikatorów, SLA i ochrony danych (C1-02).

## Decyzja

11.4; weryfikatorzy globalni z deklarowanym czasem ≥ 90 min na dzień roboczy; bramka G-7 z liczbą wyliczoną z tygodniowego modelu 17.7 (start: 18, wersja 2.2 — ADR-043); SLA 3 dni robocze (w wersji 2.1: 72 h); eskalacja KLASA → GLOBALNA → alert Administratora (nie do tej samej kolejki); kontrola przyjęć; pseudonimizacja (ADR-031).

## Alternatywy

Automatyczne nadanie Ocenie wstępnej statusu ostatecznej po SLA — **odrzucone**, bo łamie FR-24 i FR-26 („zawsze”); blokada zadań otwartych dla Uczniów bez Klasy — odrzucona (sprzeczna z wizją „jak na maturze”), zastąpiona ograniczeniem udziału zadań otwartych tylko przy przeciążeniu.

## Konsekwencje

Przy przeciążeniu Uczeń bez Klasy dostaje więcej zadań zamkniętych i jawny status opóźnienia, ale nigdy nie traci Weryfikacji.
