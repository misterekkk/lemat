# ADR-056: Jednostka pracy z jawną transakcją, zlecanie prac poleceniem SQL `ON CONFLICT` i `NOTIFY` z triggerów

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.1, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 2, wersja 3.1) |
| Bramka G-0 | nie |
| Realizuje | C6-01, C6-09 p. 4, C6-15 p. 1; doprecyzowuje ADR-051 p. 2 i ADR-053 p. 2e, p. 3 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 6.3, 13.4, 13.6, 15.1; [`docs/spec/ddl.md`](../spec/ddl.md) (triggery `trg_*_powiadom`) |

## Status

Przyjęta. Zastępuje opis „`ZlecaniePracInterceptor` (`SaveChangesInterceptor`)
wysyła `NOTIFY` w tej samej transakcji” z ADR-053 p. 3 i ADR-051 p. 2.

## Kontekst

Wersja 3.0 zakładała, że (a) `IZlecaniePrac.Zlec(...)` dodaje encję
`PracaWTle` do `LematDbContext`, a konflikt indeksu deduplikacji jest
obsługiwany przez `INSERT … ON CONFLICT`, (b) `SaveChangesInterceptor`
wysyła `pg_notify` „w tej samej transakcji”. Krytyk (C6-01) wykazał, że EF
Core nie generuje `ON CONFLICT` dla `Add` (konflikt kończy się
`DbUpdateException` 23505 i wycofaniem całego przypadku użycia), a
`SavingChangesAsync` działa przed otwarciem transakcji niejawnej
`SaveChanges` — powiadomienie w trybie autocommit dociera przed
zatwierdzeniem albo mimo wycofania. Ponadto zapis wyniku pracy w tle nie
był chroniony warunkiem dzierżawy (C6-15 p. 1), a lista encji wysyłających
`NOTIFY lemat_zdarzenia` nie była określona (C6-09 p. 4).

## Decyzja

1. **Jednostka pracy.** Każdy przypadek użycia zmieniający stan (handler
   komendy w `api`, handler pracy w tle w `worker`, praca harmonogramu)
   wykonuje się w `IJednostkaPracy.WykonajAsync(...)`
   (`Lemat.Application.Wspolne`), której implementacja `JednostkaPracyEf`
   (`Lemat.Infrastructure.Persistence`):
   1. otwiera **jawną** transakcję `Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)`;
   2. uruchamia delegat przypadku użycia;
   3. rozsyła zdarzenia domenowe zgłoszone przez agregaty śledzone w
      `ChangeTracker` do `IObslugaZdarzenia<T>` (13.6) — w pętli, aż żadna
      obsługa nie zgłosi nowych zdarzeń (maks. 5 przebiegów, potem
      `InvalidOperationException`);
   4. w kontekście pracy w tle wykonuje potwierdzenie dzierżawy (p. 5);
   5. wykonuje `SaveChangesAsync` i `CommitAsync`;
   6. przy wyjątku wykonuje `RollbackAsync` i czyści `ChangeTracker`.
   `EnableRetryOnFailure` (strategia wykonania z ponowieniami) **nie jest
   włączana**: błąd przejściowy połączenia kończy przypadek użycia
   (`api`: `503 BAZA_NIEDOSTEPNA` z `Retry-After: 2`; `worker`: ponowienie
   pracy według 13.3).
2. **Zlecanie prac poleceniem SQL.** `IZlecaniePrac.ZlecAsync(ZleceniePracy z,
   CancellationToken ct) → Task<RezultatZlecenia>` wykonuje
   `Database.SqlQuery<…>($"INSERT INTO praca_w_tle … ON CONFLICT … RETURNING id, (xmax = 0) AS wstawiona")`
   na połączeniu i w transakcji bieżącego `LematDbContext`. Wymaga
   aktywnej transakcji jednostki pracy (`Database.CurrentTransaction is
   null` → `InvalidOperationException("ZLECENIE_POZA_JEDNOSTKA_PRACY")`).
   Predykat konfliktu wybiera `KatalogPrac` według typu (13.4): `DO
   NOTHING` dla wszystkich typów poza `SPRAWDZ_POPRAWKI_TRANSKRYPCJI`, które
   ma `DO UPDATE SET ladunek = EXCLUDED.ladunek, updated_at = now()`.
   Rezultat: `Wstawiona`, `Zaktualizowana` (dla `DO UPDATE`) albo
   `Zdeduplikowana` (`DO NOTHING` bez wiersza — wtedy drugie zapytanie
   zwraca `id` istniejącej pracy). Konflikt nigdy nie wycofuje przypadku
   użycia. Encja `PracaWTle` jest w EF Core mapowana wyłącznie do
   odczytu (`AsNoTracking`, panel `/admin/praca-w-tle`); zapisy wykonuje
   `SklepPracWTle` poleceniami SQL (13.4).
3. **`NOTIFY` wyłącznie z triggerów.** Powiadomienia są wysyłane przez
   triggery `AFTER … FOR EACH ROW` utrzymywane w migracjach SQL
   (`Persistence/Migrations/Sql/0003_powiadomienia.sql`, pełna treść w
   `docs/spec/ddl.md`). PostgreSQL dostarcza `pg_notify` wywołane w
   transakcji **wyłącznie po `COMMIT`** i nigdy po `ROLLBACK`, a identyczne
   pary (kanał, ładunek) z jednej transakcji scala w jedno powiadomienie.
   Lista triggerów jest zamknięta:

   | Trigger | Tabela, zdarzenie | Kanał | Ładunek |
   |---|---|---|---|
   | `trg_praca_w_tle_powiadom` | `praca_w_tle` `AFTER INSERT OR UPDATE OF status, dostepna_od` gdy `NEW.status = 'OCZEKUJE' AND NEW.dostepna_od <= now()` | `praca_w_tle` | nazwa puli |
   | `trg_rozwiazanie_powiadom` | `rozwiazanie` `AFTER UPDATE` gdy zmienia się `version` albo `status` | `lemat_zdarzenia` | `{"typ":"ROZWIAZANIE","id":…}` |
   | `trg_ocena_powiadom` | `ocena` `AFTER INSERT OR UPDATE OF status, wskazowka` | `lemat_zdarzenia` | `{"typ":"ROZWIAZANIE","id":<rozwiazanie_id>}` |
   | `trg_weryfikacja_powiadom` | `weryfikacja` `AFTER INSERT OR UPDATE OF status, termin_at, typ_kolejki` | `lemat_zdarzenia` | `{"typ":"ROZWIAZANIE","id":<rozwiazanie_id>}` (stan „opóźniona” w widoku Ucznia) |
   | `trg_powiadomienie_powiadom` | `powiadomienie` `AFTER INSERT` | `lemat_zdarzenia` | `{"typ":"POWIADOMIENIE","id":…,"uzytkownik_id":…}` |
   | `trg_import_arkusza_powiadom` | `import_arkusza` `AFTER UPDATE OF status, raport` | `lemat_zdarzenia` | `{"typ":"IMPORT_ARKUSZA","id":…}` |
   | `trg_zlecenie_generowania_powiadom` | `zlecenie_generowania` `AFTER UPDATE OF status, liczba_cykli, zadanie_id` | `lemat_zdarzenia` | `{"typ":"ZLECENIE_GENEROWANIA","id":…}` |
   | `trg_konfiguracja_modelu_powiadom` | `konfiguracja_modelu` `AFTER INSERT OR UPDATE OF aktywna` | `konfiguracja_modelu` | `rola` (unieważnienie pamięci podręcznej 30 s, 13.1) |

   `ZlecaniePracInterceptor` nie istnieje. Zmiana encji, której nie ma w
   tabeli (np. `transkrypcja.zgodnosc_poprawek`), nie wysyła zdarzenia, bo
   nie zmienia widoku żadnego strumienia SSE.
4. **Wynik pracy chroniony dzierżawą.** Każde polecenie zapisu wyniku
   pracy (`ZAKONCZONA`, ponowienie, `MARTWA`) ma warunek `WHERE id = @id AND
   wykonawca = @ja AND status = 'W_TOKU'`; 0 zmienionych wierszy oznacza
   utratę dzierżawy — wynik jest odrzucany i liczony w
   `praca_zapis_po_utracie_dzierzawy_total{typ}`. Dodatkowo jednostka pracy
   w kontekście pracy w tle (`KontekstPracy` ustawiony przez
   `PulaPracHostedService`) wykonuje przed `SaveChanges` `SELECT 1 FROM
   praca_w_tle WHERE id = @id AND wykonawca = @ja AND status = 'W_TOKU' FOR
   UPDATE`; brak wiersza → `UtrataDzierzawyException` i `ROLLBACK`, więc
   proces po pauzie GC nie zapisze zmian domenowych pracy przejętej przez
   inny worker. Blokada wiersza pracy trwa do `COMMIT` i serializuje
   zapis z odzyskiwaniem dzierżaw (13.4).
5. **Testy** (`Lemat.Infrastructure.Tests/PraceWTle`, PostgreSQL 16 w
   Testcontainers): (a) `PodwojneZlecenieTegoSamegoKluczaNieWycofujePrzypadkuUzycia`;
   (b) `SprawdzPoprawkiDoUpdateLadunku` — druga `ZlecAsync` z innym
   `transkrypcja_id` zostawia jedną pracę `OCZEKUJE` z nowym ładunkiem;
   (c) `ListenNieDostajePowiadomieniaPrzedCommit` i
   `ListenNieDostajePowiadomieniaPoRollback` (osobne połączenie `LISTEN`,
   oczekiwanie 500 ms); (d) `ZapisWynikuPoUtracieDzierzawyJestOdrzucany`;
   (e) `JednostkaPracyBezTransakcjiOdrzucaZlecenie`; (f) dla każdego
   triggera z p. 3 — powiadomienie z oczekiwanym ładunkiem po `COMMIT`.

## Alternatywy

- **`Add` encji + przechwycenie `DbUpdateException` 23505** — odrzucona:
  wyjątek unieważnia transakcję PostgreSQL (stan „aborted”), więc reszty
  przypadku użycia nie da się zapisać bez punktu zapisu; `DO UPDATE` nie
  ma odpowiednika.
- **`pg_notify` w `DbTransactionInterceptor.TransactionCommittingAsync`** —
  odrzucona na rzecz triggerów: wymaga ręcznego śledzenia, które encje i
  pule zmieniono, a pominięcie jednej ścieżki zapisu (np. polecenie SQL
  poza `ChangeTracker`, praca harmonogramu) gubi zdarzenie; trigger działa
  dla każdej drogi zapisu, także dla `ExecuteUpdate` i SQL ręcznego.
- **Wzorzec outbox z osobnym procesem publikującym** — odrzucona: tabela
  `praca_w_tle` jest już outboxem (ADR-002), a `LISTEN/NOTIFY` dostarcza
  po zatwierdzeniu bez dodatkowego procesu.
- **`EnableRetryOnFailure`** — odrzucona: z jawnymi transakcjami wymaga
  opakowania całej jednostki pracy w `ExecutionStrategy.ExecuteAsync` i
  powtarzalności delegatu (zdarzenia domenowe, wywołania zewnętrzne), a
  ponowienia są już zapewnione na poziomie prac w tle i klienta.

## Konsekwencje

- Zlecenie pracy i zmiana stanu są atomowe, a deduplikacja jest ścieżką
  normalną, nie błędem.
- Powiadomienia docierają wyłącznie o stanie zatwierdzonym; worker nie
  budzi się „na pusto” przed `COMMIT`, więc nie traci 5 s zabezpieczenia
  z ADR-053 p. 2a.
- Każda nowa tabela, której zmiana ma być widoczna w SSE, wymaga triggera
  z listy p. 3 i testu (f); test architektury `KazdyStrumienMaTrigger`
  porównuje typy zdarzeń SSE z ADR-051 z listą triggerów w migracjach.
- Zapis wyników prac w tle kosztuje jedno dodatkowe `SELECT … FOR UPDATE`
  na transakcję (< 1 ms).
