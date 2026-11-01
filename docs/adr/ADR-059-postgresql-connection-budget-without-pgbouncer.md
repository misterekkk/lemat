# ADR-059: Budżet połączeń PostgreSQL bez PgBouncera w pilotażu

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.1, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 2, wersja 3.1) |
| Bramka G-0 | nie |
| Realizuje | C6-15 p. 2 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 5.4, 17.4, 22 |

## Status

Przyjęta.

## Kontekst

Wersja 3.0 przyjęła pule: `api` 2 × (50 + 1 `LISTEN`), `worker` 2 × (60 + 1),
`scheduler` 2 × 5, `migracje` 1 — razem 235 połączeń, bez wymagania
`max_connections` dla zarządzanego PostgreSQL (plany 2 vCPU / 8 GB mają
zwykle 100–200) i bez decyzji o PgBouncerze. `LISTEN` i blokady doradcze
sesji (`pg_try_advisory_lock` schedulera) wymagają połączeń w trybie sesji.

## Decyzja

1. **Pule** (`Maximum Pool Size` w ciągu połączenia, `Timeout = 5` s
   oczekiwania na połączenie):

   | Proces | Pula główna | Połączenia dedykowane | Na replikę | Repliki | Razem |
   |---|---|---|---|---|---|
   | `api` | 30 | 1 (`NasluchPowiadomien`, `LISTEN lemat_zdarzenia`, `konfiguracja_modelu`) | 31 | 2 | 62 |
   | `worker` | 40 | 1 (`LISTEN praca_w_tle`, `konfiguracja_modelu`) | 41 | 2 | 82 |
   | `scheduler` | 3 | 1 (sesja blokady doradczej) | 4 | 2 | 8 |
   | `migracje` | 1 | — | 1 | 1 | 1 |
   | monitoring (eksporter Collectora) | 2 | — | 2 | 1 | 2 |
   | **Razem** | | | | | **155** |

   Uzasadnienie: w `api` czas bazy p95 ≤ 20 ms na żądanie przy ≤ 150
   żądaniach/s na replikę w szczycie (17.4) daje średnio ≤ 3 zajęte
   połączenia (prawo Little'a), a 30 to zapas 10×; w `worker` wywołania
   modeli odbywają się poza transakcją (6.3), więc z 44 pętli puli
   jednocześnie połączenie trzyma tylko krótka jednostka pracy (≤ 50 ms) —
   40 połączeń wystarcza przy wszystkich pętlach zajętych.
2. **Wymaganie wobec bazy:** `max_connections ≥ 200` (155 + 3
   zarezerwowane dla superużytkownika + 42 zapasu na trzecią replikę
   `worker` i ręczne sesje administracyjne). Plan zarządzanego PostgreSQL
   jest wybierany z tym parametrem; sprawdza go bramka G-3 (d) i test
   startowy `api` (`SHOW max_connections` < 200 → ostrzeżenie w logu i
   metryka `baza_max_connections`).
3. **PgBouncer nie jest wdrażany w pilotażu.** Jeżeli liczba replik
   przekroczy budżet (np. 4 repliki `worker` = 237 połączeń), wdrażany jest
   PgBouncer w trybie `transaction` wyłącznie dla pul głównych `api` i
   `worker` (ciąg `LEMAT_BAZA`), a połączenia dedykowane (`LISTEN`, blokada
   doradcza schedulera) i `migracje` łączą się bezpośrednio (ciąg
   `LEMAT_BAZA_BEZPOSREDNIA`); Npgsql działa wtedy z `No Reset On Close =
   true` i bez przygotowanych poleceń (`Max Auto Prepare = 0`). Kod nie
   używa blokad doradczych sesji na pulach głównych — wyłącznie
   `pg_advisory_xact_lock` (ogranicznik 13.2), zgodny z trybem
   `transaction` — więc przejście nie zmienia kodu.

## Alternatywy

- **PgBouncer od startu** — odrzucona: dodatkowy komponent i dwa ciągi
  połączenia bez potrzeby przy 155 połączeniach.
- **Pozostawienie pul 50/60** — odrzucona: 235 połączeń przekracza limit
  wielu planów i nie wynika z obciążenia.

## Konsekwencje

- Budżet połączeń jest jawny i sprawdzany; skalowanie ponad 3 repliki
  `worker` wymaga PgBouncera według p. 3 albo większego planu bazy.
- Kod jest od początku zgodny z trybem `transaction` PgBouncera.
