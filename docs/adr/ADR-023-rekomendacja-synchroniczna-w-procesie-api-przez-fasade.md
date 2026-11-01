# ADR-023: Rekomendacja synchroniczna w procesie `api` przez fasadę Orkiestratora

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2).

## Kontekst

Słownik: Orkiestrator koordynuje agentów; wersja 1 wywoływała Agenta rekomendującego z `api` z pominięciem Orkiestratora i bez timeoutu (C1-17).

## Decyzja

6.3: `Orkiestrator.rekomenduj_synchronicznie()` w procesie `api`; jedyne wywołanie modelu — Q-RE-01 z timeoutem 1,5 s, bez ponowień, z circuit breakerem; przy błędzie POLITYKA_ELO; zlecenia prac (`UZUPELNIJ_PULE`, `GENERUJ_SZKIC_WKLADKI`) w tej samej transakcji.

## Alternatywy

Praca w tle + odpytywanie — odrzucona (+ ≥ 1 s na każde „Dalej”, a przepływ nie angażuje innych agentów).

## Konsekwencje

P95 `GET …/nastepna` ≤ 500 ms (bez JEV_CHOICE ≤ 150 ms); krawędź `api → Jev` w 5.1.
