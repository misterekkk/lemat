# ADR-063: DDL jako jedyne źródło schematu (migracje SQL); model EF Core jest mapowaniem bez generowania schematu

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.2, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 3, wersja 3.2) |
| Bramka G-0 | nie |
| Realizuje | C7-05, C7-06 p. 1, C7-15 p. 3–4; zmienia 7.2, 7.6, 7.7 i `ef-core.md` 8 wersji 3.1 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 7.2, 7.6, 7.7; [`docs/spec/ddl.md`](../spec/ddl.md); [`docs/spec/ef-core.md`](../spec/ef-core.md) 2, 6, 8 |

## Status

Nowa (it. 3 pętli 3.x).

## Kontekst

Wersja 3.1 generowała migrację `0001_SchematPoczatkowy` z modelu EF Core,
uzupełniała ją plikami SQL i wymagała, by wynik był równy `ddl.sql`
(`pg_dump`). Krytyk (C7-05) wykazał, że oba źródła dają różne nazwy
ograniczeń (142 CHECK-i z nazwami automatycznymi w DDL vs 12 `ck_*` w EF,
`*_fkey` vs `fk_*`), różne rodzaje (UNIQUE vs indeks unikalny) i typy
(`varchar(n)` z `HasMaxLength` vs `text` + CHECK), a większość CHECK-ów w
ogóle nie ma odpowiednika w EF. Dodatkowo relacja EF `Nauczyciel → Uzytkownik`
nie buduje się przy różnych typach identyfikatorów (C7-06 p. 1).

## Decyzja

1. **Jedno źródło.** Schemat definiuje wyłącznie `docs/spec/ddl.md` (bloki
   A i B), wycinany skryptem do `Persistence/Migrations/Sql/`. Migracje są
   klasami EF Core `Migration` zawierającymi **wyłącznie**
   `migrationBuilder.Sql(ZasobSql.Wczytaj("<plik>.sql"))` (np.
   `0001_SchematPoczatkowy` = blok A + pliki `0001`…`0006` bloku B); `efbundle`
   pozostaje narzędziem wykonania (historia w `__historia_migracji`). Model
   EF Core **nie generuje** schematu: `dotnet ef migrations add` nie jest
   używane, migawka modelu (`LematDbContextModelSnapshot`) nie jest
   utrzymywana, a CI zabrania plików `*.Designer.cs` z operacjami
   `CreateTable` (`ZadnaMigracjaNieGenerujeSchematu`).
2. **Konfiguracje EF Core opisują mapowanie, nie schemat.** Z
   `IEntityTypeConfiguration<T>` usunięto `HasCheckConstraint`,
   `HasMaxLength`, `HasTrigger`, `HasDatabaseName` indeksów niebędących
   celem `ON CONFLICT` i `HasFilter` — nie mają wpływu na zapytania ani
   `SaveChanges` w Npgsql. Pozostają: nazwy tabel i kolumn
   (`ToTable`, `HasColumnName` tam, gdzie konwencja `snake_case` nie daje
   nazwy z DDL), typy kolumn różne od domyślnych (`citext`, `jsonb`,
   `smallint`, `numeric(p,s)`), klucze, tokeny współbieżności, konwersje,
   relacje i encje własne. Długości tekstów i reguły wartości pilnują
   walidatory wejścia (12.5) i CHECK-i w bazie.
3. **Relacje EF bez nawigacji jako porządek zapisu.** EF Core porządkuje
   polecenia `INSERT`/`DELETE` wyłącznie według relacji znanych modelowi
   (sprawdzone: bez relacji `INSERT profil_ucznia` przed `INSERT uczen`
   kończy się `23503`). Dlatego każdy klucz obcy z DDL ma w modelu relację
   bez nawigacji (`HasOne<T>().WithMany().HasForeignKey(x => x.TId)`), z
   wyjątkiem kluczy, których typy identyfikatorów się różnią (`uczen.uzytkownik_id`
   i `nauczyciel.uzytkownik_id` → `uzytkownik.id`: `UczenId`/`NauczycielId`
   vs `UzytkownikId`); te dwa klucze są w DDL `DEFERRABLE INITIALLY
   DEFERRED`, więc kolejność w transakcji rejestracji nie ma znaczenia.
   Test `KazdyKluczObcyMaRelacjeAlboJestOdroczony` porównuje
   `pg_constraint (contype = 'f')` z `IForeignKey` modelu.
4. **Nazwy ograniczeń.** Jawne nazwy mają: indeksy (`ux_*`, `ix_*`),
   triggery (`trg_*`) i funkcje (ADR-049). Klucze główne, obce, UNIQUE i
   CHECK mają **nazwy domyślne PostgreSQL** (`<tabela>_pkey`,
   `<tabela>_<kolumna>_fkey`, `<tabela>_<kolumny>_key`,
   `<tabela>_<kolumna>_check`, `<tabela>_check[N]`) — są deterministyczne
   dla danego DDL; wyjątkiem są dwa klucze obce dodawane `ALTER TABLE`
   (`fk_zadanie_aktualna_wersja`, `fk_rozwiazanie_najnowsza_transkrypcja`),
   nazwane jawnie, bo powstają po obu tabelach. Stan po migracjach
   (sprawdzone): 142 CHECK, 124 FK, 21 UNIQUE, 64 klucze główne (z
   partycjami). Plik `Persistence/Schema/constraints.txt`
   (posortowana lista `tabela | nazwa | definicja` z `pg_constraint`) jest
   generowany w CI po migracjach i porównywany z wersją w repozytorium,
   więc każda zmiana nazwy albo definicji jest widoczna w przeglądzie PR.
   `TlumaczBledowInfrastruktury` mapuje `23505` po nazwie indeksu `ux_*`
   albo ograniczenia `*_key` (katalog `KatalogOgraniczenUnikalnych`),
   `23514` (CHECK) i `23503` (FK) — na `500 BLAD_WEWNETRZNY` z logiem
   `NARUSZENIE_SCHEMATU {ograniczenie}` (walidacja wejścia powinna je
   wykluczyć), `P0001` — na kod z komunikatu triggera.
5. **Triggery w modelu.** `HasTrigger` nie jest używane (Npgsql nie
   zmienia SQL zapisu z powodu triggerów, a `RETURNING` działa z
   triggerami). Lista 15 triggerów jest stałą `KatalogTriggerow` (tabela,
   nazwa, plik SQL) porównywaną z `pg_trigger` testem `KazdyTriggerWKatalogu`.
6. **Test zgodności `ModelEfZgodnyZKatalogiem`** (zastępuje
   `SchematZgodnyZDdlTest`; Testcontainers PostgreSQL 16, uruchamiany w
   każdym PR): po migracjach porównuje `IModel.GetRelationalModel()` z
   `information_schema.columns` i `pg_index`: (a) każda tabela i kolumna
   modelu istnieje; (b) typ magazynu (`StoreType`) = typ kolumny po
   normalizacji (`character varying` niedozwolone); (c) `IsNullable` =
   `is_nullable`; (d) klucz główny modelu = klucz główny tabeli; (e) każdy
   indeks wskazany jako cel `ON CONFLICT` w SQL (`KatalogPrac`, C-36)
   istnieje z tym predykatem; (f) kolumny `NOT NULL` bez `DEFAULT` w tabeli
   mapowanej do zapisu są w modelu. Kolumny tylko w bazie (np.
   `created_at` z `DEFAULT`) są dozwolone, jeśli nie są `NOT NULL` bez
   `DEFAULT`. Sprawdzone na PostgreSQL 16.15 i EF Core 10.0.3 na
   fragmencie schematu: test wykrył `element_sciezki.sciezka_nauki_id`
   nullowalne w modelu i `NOT NULL` w DDL; po `IsRequired()` 0 różnic.
7. **Role aplikacji** (C7-15 p. 4): `0005_permissions.sql` tworzy role
   `lemat_api`, `lemat_worker`, `lemat_scheduler` jako `NOLOGIN`, jeśli nie
   istnieją (blok `DO`); w produkcji tworzy je wcześniej infrastruktura z
   `LOGIN`. Sprawdzone: bloki A i B ładują się bez błędów na czystej bazie
   PostgreSQL 16.15.

## Alternatywy

- **Wariant B — DDL generowany z migracji EF (`dotnet ef migrations
  script`)** — odrzucona: 130 CHECK-ów, triggery, funkcje PL/pgSQL,
  partycje, indeksy częściowe i GIN oraz FK `DEFERRABLE` wymagałyby
  `migrationBuilder.Sql` i tak, a model EF stałby się nieczytelny; DDL
  jest czytany przez przeglądających (ADR-002).
- **Jawne nazwy wszystkich 287 ograniczeń CHECK/FK/UNIQUE w DDL** — odrzucona: nazwy
  domyślne są deterministyczne, a plik `constraints.txt` daje tę samą
  kontrolę zmian bez ręcznego nazywania; kod nie odwołuje się do nazw
  CHECK ani FK.
- **Wspólny typ `UzytkownikId` dla Ucznia i Nauczyciela** — odrzucona:
  traci rozróżnienie typów w sygnaturach domeny (`UczenId` w 60 miejscach);
  dwa odroczone klucze obce są tańsze.

## Konsekwencje

- Zmiana schematu = edycja `ddl.md` + nowy plik SQL + nowa klasa migracji
  z `Sql(...)` + ewentualna zmiana konfiguracji EF; test zgodności
  wychwytuje brak tej ostatniej.
- EF Core nie może „po cichu” utworzyć innego schematu, bo nie tworzy go
  wcale.
- Odroczone klucze obce kont są sprawdzane przy `COMMIT` (błąd `23503`
  pojawia się przy `CommitAsync`, nie przy `SaveChangesAsync`) —
  `TlumaczBledowInfrastruktury` obsługuje oba miejsca.
