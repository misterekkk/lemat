# ADR-066: Odporność pętli prac w tle i harmonogramu — klasyfikacja anulowania, obsługa każdego wyjątku, ponowienie bez konfliktu deduplikacji, nadrabianie wystąpień

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.2, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 3, wersja 3.2) |
| Bramka G-0 | nie |
| Realizuje | C7-03, C7-04, C7-13; uzupełnia ADR-053 i ADR-056 p. 4 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 10.0.4, 13.3, 13.4, 13.5, 14.3; [`docs/spec/ddl.md`](../spec/ddl.md) `0006_prace_w_tle.sql`, `przebieg_harmonogramu` |

## Status

Nowa (it. 3 pętli 3.x).

## Kontekst

Krytyk (C7-03, C7-04, C7-13) wykazał w wersji 3.1: (a) ponowienie i
odzyskiwanie dzierżaw ustawiają pracę `W_TOKU` na `OCZEKUJE`, co narusza
`ux_praca_dedup_oczekujace`, gdy istnieje już praca `OCZEKUJE` z tym samym
kluczem — jedno `UPDATE` odzyskiwania kończy się `23505` i nie odzyskuje
żadnej pracy; (b) pętla puli traktuje każde `OperationCanceledException`
jako utratę dzierżawy i kończy pracę jako `ZAKONCZONA` (Rozwiązanie zostaje
w `OCENIANIE`/`ROZPOZNAWANIE`), a każdy inny wyjątek zatrzymuje proces
(`BackgroundServiceExceptionBehavior.StopHost`); (c) harmonogram pomija
wystąpienia z okna przerwy.

## Decyzja

1. **Klasyfikacja anulowania** (`PulaPracHostedService.Klasyfikuj`):
   - `zatrzymanie.IsCancellationRequested` → wynik nie jest zapisywany
     (praca wraca przez odzyskanie dzierżawy po 60 s, ADR-053);
   - `dzierzawa.UtraconaAt is not null` (ustawiane wyłącznie przez
     pętlę odnawiania, gdy `OdnowDzierzaweAsync` zwróci `false`) →
     `RezultatPrzestarzaly("DZIERZAWA_UTRACONA")` bez zapisu (wiersz nie
     należy już do wykonawcy);
   - każde inne `OperationCanceledException`/`TaskCanceledException`/`TimeoutException`
     → `Ponow("PRZEKROCZONY_CZAS")`, a gdy minął termin twardy pracy
     (`IHandlerPracy.TerminTwardy(praca)`, np. `zatwierdzono_at + 120 s`
     dla `OCEN_ROZWIAZANIE`) albo wyczerpano próby → `Trwaly("PRZEKROCZONY_CZAS")`
     → `PoWyczerpaniuAsync` (T-6, `BLAD_ROZPOZNAWANIA` `PRZEKROCZONY_CZAS`,
     `NIEUDANE` zlecenie).
   Token dzierżawy jest anulowany wyłącznie przez pętlę odnawiania i
   zatrzymanie; limity czasu wywołań mają własne `CancellationTokenSource`
   (10.0.3), a `CancelAfter` dostaje `max(TimeSpan.Zero, …)` — termin, który
   już minął, rzuca `TerminMinalException` przed wywołaniem (klasyfikowane
   jak przekroczenie czasu).
   Sprawdzone kompilacją i uruchomieniem (.NET 10): `TaskCanceledException`
   przy ważnej dzierżawie → `Ponow("PRZEKROCZONY_CZAS")`; anulowanie po
   `Utracona()` → `RezultatPrzestarzaly("DZIERZAWA_UTRACONA")`; przekroczenie
   po terminie twardym → `Trwaly("PRZEKROCZONY_CZAS")`; anulowanie przy
   zatrzymaniu → `Przerwana`; `CancelAfter` z ujemnym czasem rzuca
   `ArgumentOutOfRangeException` (stąd `TerminMinalException` przed
   wywołaniem).
2. **Każdy wyjątek ma wynik.** `catch (Exception)` w pętli: `NpgsqlException`
   z `IsTransient`, `DbUpdateException` z błędem przejściowym, `TimeoutException`
   → `Ponow("BLAD_INFRASTRUKTURY")`; `NiezmiennikNaruszonyException` i
   pozostałe → `Ponow("BLAD_NIEOCZEKIWANY")` (E-24) z logiem `Error` i
   `trace_id`; po wyczerpaniu prób → `MARTWA` i `PoWyczerpaniuAsync`.
   `PobierzAsync`, `ZapiszWynikAsync` i `OdnowDzierzaweAsync` są wywoływane
   w `PonawiajInfrastrukture` (odstęp 0,5 s, 1, 2, 4, … 30 s, bez limitu
   prób, metryka `praca_petla_blad_infrastruktury_total{pula}`); pętla nie
   kończy się nigdy poza zatrzymaniem procesu.
3. **Ponowienie i odzyskiwanie bez konfliktu deduplikacji:** funkcje SQL
   `ponow_prace` i `odzyskaj_dzierzawy` (`0006_prace_w_tle.sql`) zmieniają
   każdy wiersz w osobnym bloku `EXCEPTION`; naruszenie
   `ux_praca_dedup_oczekujace` kończy pracę A statusem `ZAKONCZONA` z
   `ostatni_blad = 'ZASTAPIONA_NOWSZA'` (praca B `OCZEKUJE` z tym samym
   kluczem ma nowszy ładunek albo przetworzy wszystkie niezastosowane
   Wyniki zadania). Sprawdzone na PostgreSQL 16.15: przy pracy A
   (`AKTUALIZUJ_PROFIL`, `W_TOKU`, dzierżawa wygasła) i B (`OCZEKUJE`, ten
   sam klucz) oraz C (`OCEN_ROZWIAZANIE`, dzierżawa wygasła)
   `odzyskaj_dzierzawy()` zwraca A → `ZAKONCZONA`, C → `OCZEKUJE`, bez
   błędu; `ponow_prace` przy konflikcie zwraca `ZAKONCZONA`, a po utracie
   dzierżawy `NULL`.
4. **Zachowanie hosta:** `HostOptions.BackgroundServiceExceptionBehavior =
   StopHost` (świadomie): wyjątek, który mimo p. 2 opuści `ExecuteAsync`,
   jest błędem programu — proces kończy się kodem ≠ 0, Docker restartuje
   kontener (`restart: unless-stopped`), alert `WORKER_ZATRZYMANY`.
   Health check `/zdrowie/gotowosc` procesu `worker` zwraca `503`, gdy
   którakolwiek pula nie wykonała iteracji pętli od 60 s
   (`OstatniaIteracjaPetli`).
5. **Harmonogram z nadrabianiem (C7-13):** tabela
   `przebieg_harmonogramu (nazwa, ostatnie_wystapienie, zlecono_at, instancja)`.
   Przy objęciu roli aktywnej i co 1 s dla każdego wpisu: wystąpienia cron
   z przedziału `(max(ostatnie_wystapienie, teraz − 24 h), teraz]` są
   zlecane po kolei w jednostce pracy, która w tej samej transakcji
   zapisuje `ostatnie_wystapienie` (`INSERT … ON CONFLICT (nazwa) DO UPDATE
   … WHERE przebieg_harmonogramu.ostatnie_wystapienie < EXCLUDED.ostatnie_wystapienie`);
   dla cykli krótszych niż 1 h nadrabiane jest tylko ostatnie wystąpienie
   (odzyskiwanie dzierżaw, `ZWOLNIJ_BLOKADY_WERYFIKACJI`, `WYGAS_ELEMENTY`,
   `ZAMIATAJ_PROFILE` są stanowe). Brak wiersza → `ostatnie_wystapienie =
   teraz − 24 h`. Prace harmonogramu mają klucz deduplikacji z datą
   wystąpienia (13.3), a warunki wykonania są idempotentne, więc
   nadrobienie po przejęciu roli nie duplikuje skutków.
6. **Praca kontrolna `SPRAWDZ_ZAWIESZONE_ROZWIAZANIA`** (harmonogram co 5
   min): Rozwiązania w `ROZPOZNAWANIE` albo `OCENIANIE` od > 5 min bez pracy
   `OCZEKUJE`/`W_TOKU` dla tego Rozwiązania → ponowne zlecenie właściwej
   pracy (gdy termin twardy nie minął) albo ścieżka „po wyczerpaniu” (T-6,
   `BLAD_ROZPOZNAWANIA`); metryka `rozwiazania_zawieszone_total` i alert
   przy > 0 przez 15 min.
7. **Testy** (`Lemat.Infrastructure.Tests/PraceWTle`, Testcontainers):
   (g) `PonowienieGdyIstniejePracaOczekujacaZTymSamymKluczem`;
   (h) `OdzyskiwanieZJednymKonfliktemOdzyskujePozostale`;
   (i) `TimeoutModeluToPonowANieZakonczona`;
   (j) `TerminTwardyMinalToT6`;
   (k) `WyjatekBazyWPobierzAsyncNieZatrzymujeHosta` (zatrzymany kontener
   PostgreSQL na 10 s, potem pętla wznawia pobieranie);
   (l) `PrzejecieRoliO030005ZlecaKalibrujDlaBiezacejDaty`
   (`FakeTimeProvider` 03:00:05, brak wiersza `KALIBRUJ` z dnia → praca
   `KALIBRUJ` z kluczem daty).

## Alternatywy

- **Scalanie A z B (`DELETE` B i przejęcie ładunku)** — odrzucona:
  zmienia identyfikator pracy oczekującej, na który mogą wskazywać logi i
  metryki; zakończenie A jest prostsze i równoważne skutkiem.
- **`BackgroundServiceExceptionBehavior.Ignore`** — odrzucona: zatrzymana
  pętla bez zatrzymania procesu jest niewidoczna dla orkiestratora;
  restart kontenera jest bezpieczny dzięki dzierżawom.
- **Nadrabianie bez limitu wstecz** — odrzucona: po długiej przerwie (np.
  odtworzenie z kopii) prace dzienne z wielu dni nie mają sensu poza
  ostatnią; 24 h pokrywa każde wdrożenie i awarię hosta.

## Konsekwencje

- Odzyskiwanie dzierżaw kosztuje jedną podtransakcję na wiersz (≤ 500 na
  przebieg co 30 s).
- Proces `worker` nie przestaje działać z powodu chwilowej niedostępności
  bazy, a przekroczenia czasu zawsze kończą się ścieżką z 13.3.
- Każdy wpis harmonogramu ma trwały znacznik, widoczny w panelu
  `/admin/praca-w-tle`.
