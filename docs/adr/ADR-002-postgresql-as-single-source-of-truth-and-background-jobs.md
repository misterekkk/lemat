# ADR-002: PostgreSQL jako jedyne źródło prawdy i mechanizm prac w tle

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2); realizacja techniczna zaktualizowana przez ADR-048 (wersja 3.0) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2); realizacja techniczna zaktualizowana przez ADR-048 (wersja 3.0).

**Realizacja w stosie .NET 10 + Next.js (wersja 3.0).** Decyzja obowiązuje. Realizacja w .NET: `BackgroundService` na pulę z pętlami asynchronicznymi (zamiast korutyn `asyncio`), pobieranie `FOR UPDATE SKIP LOCKED` przez Npgsql, wybudzanie `LISTEN/NOTIFY`, zlecanie pracy w tej samej transakcji EF Core co zmiana stanu — ADR-053. Kolumna dzierżawy `praca_w_tle.zablokowana_do` ma od wersji 3.0 nazwę `dzierzawa_do` (C5-04).

## Kontekst

Każda zmiana stanu Rozwiązania musi atomowo zlecić kolejny krok; szczyt rozpoznawania nie może blokować oceniania (C1-25).

## Decyzja

Tabela `praca_w_tle` z kolumną `pula` + `SKIP LOCKED` + `LISTEN/NOTIFY`; rozłączne pule korutyn (13.4).

## Alternatywy

Redis + Celery (problem podwójnego zapisu, wymaga wzorca outbox), Kafka (nadmiarowa).

## Konsekwencje

Brak dodatkowego komponentu; przepustowość wystarcza do ~1 000 prac/s, dużo powyżej A-03.

## Uzupełnienia

- **Uzupełnienie (it. 4, wersja 2.2, C2-04):** deduplikacja prac obejmuje dla typów-wybudzeń (`AKTUALIZUJ_PROFIL`) wyłącznie prace `OCZEKUJE` (`ux_praca_dedup_oczekujace`), bo deduplikacja z `W_TOKU` gubiła wybudzenie dla Wyniku zatwierdzonego po odczycie przez handler; handler działa w pętli do pustego wyniku, a `ZAMIATAJ_PROFILE` (co 5 min) jest zabezpieczeniem. Alternatywa „tylko pętla w handlerze” — odrzucona: okno między ostatnim odczytem a `ZAKONCZONA` nadal gubi wybudzenie.
