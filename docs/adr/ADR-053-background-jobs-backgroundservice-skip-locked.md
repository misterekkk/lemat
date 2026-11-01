# ADR-053: Prace w tle w .NET — `BackgroundService` na pulę i `FOR UPDATE SKIP LOCKED`

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.0, 2026-11-01); uzupełnienie wersji 3.1 — sekcja „Uzupełnienia”; uzupełnienie 3.2 (ADR-066) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 1, wersja 3.0) |
| Bramka G-0 | nie |
| Realizuje | ADR-048 p. 4; realizacja ADR-002 w .NET |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 6.3, 13.3, 13.4, 13.5 |

## Status

Przyjęta. Pełne kontrakty C# handlerów i sklepu prac dopisuje iteracja 2
(13.3–13.5).

## Kontekst

ADR-002 ustala PostgreSQL jako jedyny mechanizm prac w tle: tabela
`praca_w_tle` z kolumną `pula`, pobieranie `FOR UPDATE SKIP LOCKED`,
wybudzanie `LISTEN/NOTIFY`, zlecanie pracy w tej samej transakcji co zmiana
stanu, deduplikacja indeksami częściowymi, dzierżawa 60 s odnawiana co
15 s, rozłączne pule R/S/O/P/W o liczności 15/4/15/6/4 na replikę (13.4).
W wersji 2.4 pętle były korutynami `asyncio`.

## Decyzja

1. `Lemat.Worker` rejestruje jeden `PulaPracHostedService : BackgroundService`
   na pulę (`PulaPracy.Rozpoznawanie`, `Sprawdzanie`, `Ocenianie`,
   `Pomocnicza`, `Wolna`); każdy uruchamia N pętli asynchronicznych (`Task`)
   z liczbą z 13.4 (`LematOpcje.PulePracy`).
2. Pętla: (a) czeka na `SemaphoreSlim` puli sygnalizowany przez
   `NasluchPowiadomien` (`LISTEN praca_w_tle`, ładunek = nazwa puli) albo
   na upływ 5 s (zabezpieczenie przed zgubionym powiadomieniem);
   (b) wykonuje SQL pobrania z 13.4 przez `NpgsqlDataSource` (nie EF Core),
   ustawiając `status = 'W_TOKU'`, `wykonawca = '<host>#<pula>#<nr>'`,
   `dzierzawa_do = now() + 60 s`; (c) uruchamia handler
   `IHandlerPracy` dopasowany po `TypPracy` z
   `CancellationTokenSource` powiązanym z zamknięciem procesu i utratą
   dzierżawy; (d) odnawia dzierżawę co 15 s (`PeriodicTimer`; `UPDATE …
   WHERE id = @id AND wykonawca = @ja` — 0 zmienionych wierszy = utrata
   dzierżawy → anulowanie handlera); (e) zapisuje wynik (`ZAKONCZONA`,
   ponowienie z odstępem z 13.3 albo `MARTWA`).
3. Zlecanie: `IZlecaniePrac.Zlec(TypPracy, ladunek, kluczDeduplikacji,
   priorytet, dostepnaOd)` dodaje encję `PracaWTle` do bieżącego
   `LematDbContext`; `ZlecaniePracInterceptor` (`SaveChangesInterceptor`)
   wykonuje `SELECT pg_notify('praca_w_tle', @pula)` w tej samej
   transakcji; konflikt indeksu deduplikacji (`ux_praca_dedup`,
   `ux_praca_dedup_oczekujace`) jest obsługiwany przez `INSERT … ON CONFLICT
   DO NOTHING` (dla `SPRAWDZ_POPRAWKI_TRANSKRYPCJI`: `DO UPDATE SET ladunek
   = EXCLUDED.ladunek` wśród `OCZEKUJE`, C3-05).
4. Zamknięcie procesu: `HostOptions.ShutdownTimeout = 30 s`; po `SIGTERM`
   pętle przestają pobierać prace, uruchomione handlery dostają
   anulowanie po 25 s; niezakończone dzierżawy wygasają po 60 s i są
   przejmowane według 13.4.
5. Harmonogram 13.5: `Lemat.Scheduler` (`HarmonogramHostedService`)
   utrzymuje jedną sesję z `pg_try_advisory_lock(<stała>)`; tylko
   instancja z blokadą doradczą PostgreSQL zleca prace cykliczne
   (wyrażenia cron parsowane przez Cronos 0.11; strefy czasowe jak w 13.5);
   utrata sesji kończy rolę aktywnej instancji, a druga przejmuje ją w
   ≤ 10 s.
6. Obserwowalność: metryki 17.3 (`prace_w_tle_oczekujace{pula}`,
   `praca_w_tle_czas_sekundy{typ}` i pozostałe nazwy z 17.3) przez
   `System.Diagnostics.Metrics` i OpenTelemetry.

## Alternatywy

- **Hangfire (z PostgreSQL)** — odrzucony: własny schemat i serializacja
  zadań, zlecenie nie jest częścią transakcji EF Core zmieniającej stan
  agregatu bez dodatkowej integracji, brak semantyki rozłącznych pul i
  deduplikacji z 13.3; słowo „job” w kodzie domenowym łamałoby 3.1 p. 3.
- **Quartz.NET** — odrzucony jako mechanizm prac (jest harmonogramem, nie
  transakcyjnym magazynem prac); jako harmonogram 13.5 — zbędny, bo
  wystarcza parser cron i blokada doradcza.
- **MassTransit / RabbitMQ / Azure Service Bus** — odrzucone: nowy
  komponent i wzorzec outbox (ADR-002).
- **EF Core do pobierania prac** — odrzucone: `FOR UPDATE SKIP LOCKED`
  z `UPDATE … RETURNING` jest jednym poleceniem SQL; EF Core dodawałby
  śledzenie zmian bez korzyści.

## Konsekwencje

- Semantyka 13.3–13.5 bez zmian, w tym idempotencja handlerów i reguły
  deduplikacji.
- Kolumna dzierżawy nazywa się od wersji 3.0 `dzierzawa_do` (C5-04).
- Kod sklepu prac (≈ 300 linii) jest własny i testowany w
  `Lemat.Infrastructure.Tests` na PostgreSQL 16 (Testcontainers):
  współbieżne pobieranie bez duplikatów, wygasanie dzierżawy, deduplikacja,
  `NOTIFY` dopiero po zatwierdzeniu.

## Uzupełnienia

**Uzupełnienie wersji 3.1 (C6-01, C6-15 p. 1; ADR-056).** Zmiany:
p. 2e — zapis wyniku ma warunek `WHERE id = @id AND wykonawca = @ja AND
status = 'W_TOKU'`, a jednostka pracy handlera potwierdza dzierżawę
`SELECT … FOR UPDATE` przed `COMMIT`; p. 3 — `IZlecaniePrac.ZlecAsync`
wykonuje `INSERT … ON CONFLICT … RETURNING` poleceniem SQL na transakcji
bieżącej jednostki pracy (nie `Add` encji), a `NOTIFY praca_w_tle` wysyła
trigger `trg_praca_w_tle_powiadom`; `ZlecaniePracInterceptor` nie
istnieje. Kontrakt `IZlecaniePrac` jest w `Lemat.Application.Common.BackgroundJobs`.

**Uzupełnienie wersji 3.2 (C7-03, C7-04, C7-13, ADR-066).** Klasyfikacja
anulowania (utrata dzierżawy wyłącznie przez pętlę odnawiania; każde inne
anulowanie = przekroczenie czasu → `Ponow`/`Trwaly`), obsługa każdego
wyjątku w pętli bez jej kończenia, `BackgroundServiceExceptionBehavior =
StopHost` z restartem kontenera, funkcje SQL `ponow_prace` i
`odzyskaj_dzierzawy` (konflikt deduplikacji → `ZASTAPIONA_NOWSZA`) oraz
nadrabianie wystąpień harmonogramu z tabeli `przebieg_harmonogramu` —
ADR-066, kod w 13.4.1.
