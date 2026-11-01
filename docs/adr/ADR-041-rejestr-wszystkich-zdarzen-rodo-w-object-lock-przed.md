# ADR-041: Rejestr wszystkich Zdarzeń RODO w Object Lock przed transakcją

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 4) |
| Data decyzji | 2026-11-01 (iteracja 4, wersja 2.2) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 4).

## Kontekst

Do wersji 2.1 rejestr zawierał tylko wykonane usunięcia; odtworzenie PITR do punktu sprzed wniosku o usunięcie lub odwołania zgody przywracało konto `AKTYWNE` i zgodę `true` (C2-09).

## Decyzja

`api` zapisuje każde Zdarzenie RODO (`WNIOSEK_O_USUNIECIE`, `ANULOWANIE_USUNIECIA`, `USUNIECIE_WYKONANE`, `ODWOLANIE_ZGODY`, `UDZIELENIE_ZGODY`) do bucketu `rejestr-zdarzen-rodo` **przed** transakcją, a kopię do `zdarzenie_rodo`; `UZGODNIJ_REJESTR_RODO` unieważnia obiekty bez zatwierdzonej transakcji; `ponow_zdarzenia_rodo` stosuje zdarzenia po punkcie odtworzenia przed startem workerów. G-6 testuje trzy scenariusze.

## Alternatywy

Wzorzec outbox (zapis w bazie, potem praca w tle do bucketu) — odrzucony, bo odtworzenie do chwili między zatwierdzeniem a wysyłką gubi zdarzenie; zapis do bucketu po transakcji — odrzucony z tego samego powodu.

## Konsekwencje

Żądania RODO przeżywają odtworzenie bazy; niejednoznaczność (zdarzenie bez transakcji w oknie 10 min) jest rozstrzygana w kierunku ochrony danych.

## Uzupełnienia

- **Uzupełnienie (it. 5, wersja 2.3, C3-03, C3-07):** zdarzenia z inicjatywy użytkownika zapisuje `api` (`zrodlo = API`), a `USUNIECIE_WYKONANE` — `worker` (`zrodlo = WORKER`, deterministyczne `id`, obiekt przed pierwszą zmianą w bazie, wiersz `zdarzenie_rodo` w pierwszej transakcji `USUN_KONTO`); `UZGODNIJ_REJESTR_RODO` unieważnia wyłącznie zdarzenia `API` bez wiersza, a dla `WORKER` doprowadza konto do usunięcia (ponowne `USUN_KONTO` po 2 h, alert po 26 h); `ponow_zdarzenia_rodo` wykonuje kroki 1–6 synchronicznie niezależnie od czasu od usunięcia. Nazwy: `WNIOSEK_O_USUNIECIE` (zamiast `ZADANIE_USUNIECIA`), bucket `rejestr-zdarzen-rodo`, pojęcie „Rejestr Zdarzeń RODO”. Odrzucona alternatywa: unieważnianie także zdarzeń `WORKER` z wydłużonym oknem (np. 24 h) — każde okno skończone zostawia przypadek PITR wykonanego po oknie (C3-03 p. 3b).

- **Uzupełnienie (it. 6, wersja 2.4, C4-01, C4-06 p. 2 i 3):** w `ponow_zdarzenia_rodo` kryterium zastosowania zależy od źródła — `API`: wiersz `zdarzenie_rodo` o tym `id`; `WORKER`: brak wiersza `uzytkownik` (wiersz zdarzenia powstaje na początku usunięcia, więc PITR w trakcie `USUN_KONTO` dawał bazę z wierszem zdarzenia i danymi konta); prefiks `wykonane/` przeglądany w całości; ponowienie każdego zdarzenia przez te same funkcje domenowe co `api`/`USUN_KONTO`, według `dane.rola` (kroki Nauczyciela, skutki `KontoUsuwane` / `KontoPrzywrocone`); `rola` w każdym zdarzeniu (CHECK); klucz `USUNIECIE_WYKONANE` bez daty (`wykonane/{uzytkownik_id}/{id}.json`), więc `If-None-Match: *` działa przy ponowieniu w innym dniu; `UZGODNIJ_REJESTR_RODO` co 5 min — okno unieważniania ≤ 10 min zgodne z 13.3, 16.4 i G-6; G-6 (a3), (a4), scenariusz C-39. Odrzucona alternatywa: wiersz `zdarzenie_rodo` dla `USUNIECIE_WYKONANE` zapisywany w ostatniej transakcji (krok 5) — wtedy PITR po kroku 4 traci informację, że usunięcie się rozpoczęło, a obiekty zdjęć są już skasowane; kryterium „konto nie istnieje” jest poprawne przy każdym punkcie `T`.

- **Uzupełnienie (pętla 3.x, it. 1, wersja 3.0, C5-05 p. 4):** `UZGODNIJ_REJESTR_RODO` przegląda obiekty `zdarzenia/` od `max(ostatni_udany_przebieg − 5 min, now() − 60 dni)` zamiast „ostatnich 24 h”, więc przerwa harmonogramu dłuższa niż dobę nie zostawia zdarzeń `API` bez transakcji; przerwa > 24 h podnosi `ALERT_RODO`. Odrzucona alternatywa: stałe okno 60 dni przy każdym przebiegu — ≈ 288 pełnych przeglądów prefiksu na dobę bez potrzeby.
