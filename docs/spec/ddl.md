# Lemat — pełne DDL PostgreSQL 16 (źródło prawdy schematu)

| Pole | Wartość |
|---|---|
| Dokument | `docs/spec/ddl.md` |
| Wersja | 3.3 (pętla 3.x, iteracja 4) |
| Dotyczy | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — sekcja 7 (decyzje, ERD, konwencje, schematy JSONB), mapowanie EF Core: [`docs/spec/ef-core.md`](ef-core.md) |
| Status | Jedyne źródło schematu (ADR-063): migracje są klasami EF Core wykonującymi wyłącznie pliki SQL wycięte z tego dokumentu; model EF Core nie generuje schematu i jest sprawdzany z katalogiem testem `ModelEfZgodnyZKatalogiem` (`ef-core.md` 8) |

Plik zawiera dwa bloki SQL: **(A) tabele, typy, ograniczenia, indeksy**
(przeniesione z 7.3 wersji 3.0 z poprawkami wersji 3.1) i **(B) triggery,
funkcje i uprawnienia** (w wersji 3.0 opisane komentarzami; od wersji 3.1
pełna treść, z której powstają pliki `Persistence/Migrations/Sql/*.sql`).
Skrypt CI `backend/narzedzia/wyodrebnij-ddl.sh` wycina oba bloki do
`backend/src/Lemat.Infrastructure/Persistence/Schemat/ddl.sql`.

## Zmiany wersji 3.3

| Obiekt | Zmiana | Uwaga krytyka / ADR |
|---|---|---|
| `klucz_idempotencji` | nowe kolumny `rezerwacja uuid`, `w_toku_do timestamptz`; `kod_statusu_http` tylko `2xx`; CHECK `ck_klucz_idempotencji_stan` (wiersz jest albo rezerwacją w toku, albo zapisaną odpowiedzią) | C8-03, ADR-068 |
| `zarezerwuj_klucz_idempotencji` | nowa funkcja (`0007_api.sql`): rezerwacja na 60 s, przejęcie po upływie `w_toku_do`, wynik `WYKONAJ` / `POWTORKA` / `W_TOKU` / `KONFLIKT_CIALA` | C8-03, ADR-068 |
| `przypisanie` | nowe kolumny `version`, `updated_at`, `wycofano_at`, `powod_wycofania` + CHECK spójności wycofania | C8-11 p. 3, ADR-074 |
| `szkic_transkrypcji` | nowa tabela: robocza postać Poprawek Transkrypcji (autozapis bez nowej wersji Transkrypcji) | C8-10, ADR-073 |

Stan katalogu po załadowaniu bloków A i B na czystej bazie PostgreSQL
16 (sprawdzone w wersji 3.3): 61 tabel (+ 4 partycje `decyzja_modelu`),
43 typy wyliczeniowe, 15 triggerów, 0 błędów.

## Zmiany wersji 3.2

| Obiekt | Zmiana | Uwaga krytyka / ADR |
|---|---|---|
| `praca_w_tle` | kolumna `max_prob` → `maks_prob` (ADR-049; konwencja `snake_case` z `MaksProb`) | C7-06 p. 7 |
| `uczen`, `nauczyciel` | klucz obcy `uzytkownik_id → uzytkownik(id)` `DEFERRABLE INITIALLY DEFERRED` | C7-06 p. 1, ADR-063 p. 3 |
| `przebieg_harmonogramu` | nowa tabela (trwały znacznik ostatniego wystąpienia wpisu harmonogramu) | C7-13, ADR-066 p. 5 |
| `0005_uprawnienia.sql` | blok `DO` tworzy role `NOLOGIN`, gdy nie istnieją; `ALTER DEFAULT PRIVILEGES` dla roli wykonującej migrację | C7-15 p. 4 |
| `0006_prace_w_tle.sql` | nowe funkcje `ponow_prace` i `odzyskaj_dzierzawy` (konflikt `ux_praca_dedup_oczekujace` → `ZASTAPIONA_NOWSZA`, odzyskiwanie per wiersz) | C7-03, ADR-066 p. 3 |
| `powiadomienie.id` | bez zmian schematu; wznowienie strumienia nie zależy od kolejności UUIDv7 (migawka nieprzeczytanych) | C7-11, ADR-064 p. 4 |

Stan katalogu po załadowaniu bloków A i B na czystej bazie PostgreSQL
16.15 (sprawdzone w wersji 3.2): 60 tabel (+ 4 partycje `decyzja_modelu`),
43 typy wyliczeniowe, 15 triggerów, 0 błędów (także bez wcześniej
utworzonych ról).

## Zmiany wersji 3.1 względem 7.3 wersji 3.0

| Obiekt | Zmiana | Uwaga krytyka / ADR |
|---|---|---|
| `uzytkownik` | nowa kolumna `status_przed_wnioskiem status_konta` + CHECK `(status = 'USUWANE') = (status_przed_wnioskiem IS NOT NULL)` | C6-06 p. 1 |
| `nauczyciel` | nowa kolumna `umowa_poufnosci_at timestamptz` + CHECK `NOT weryfikator_globalny OR umowa_poufnosci_at IS NOT NULL` | C6-06 p. 3 |
| `token_odswiezania` | nowe kolumny `zastapiony_przez`, `zastapiony_at`, `skrot_klienta_http`, `uzycia_w_oknie_tolerancji`; indeksy `ix_token_rodzina`, `ix_token_uzytkownik` | C6-02, ADR-057 |
| `zlecenie_generowania` | nowa kolumna `wywolania_narzedzi jsonb` (schemat 7.5.5) | C6-03, ADR-058 p. 6 |
| `sciezka_nauki` | nowa kolumna `ostatnia_kolejnosc integer` | C6-15 p. 4 |
| `rozwiazanie` | nowe kolumny `najnowsza_transkrypcja_id`, `najnowsza_wersja_transkrypcji`, `ostatnie_rozpoznanie_od_at`; `przeslano_at` = wyłącznie Przesłanie nowych zdjęć | C6-04 p. 1, 2, 4 |
| `stan_bezpiecznika` | nowa kolumna `version integer` (token współbieżności) | C6-08 |
| `powiadomienie` | `id` bez wartości domyślnej — UUIDv7 z aplikacji | C6-09 p. 3, ADR-057 p. 5 |
| triggery `trg_*_powiadom` | nowe: jedyne źródło `NOTIFY praca_w_tle`, `lemat_zdarzenia`, `konfiguracja_modelu` | C6-01, C6-09 p. 4, ADR-056 |
| triggery i funkcje wersji 2.4 | pełna treść PL/pgSQL zamiast opisu w komentarzu (`trg_suma_kryteriow`, `trg_umiejetnosc_glowna_w_dziale`, `trg_kontekst_proby`, `trg_ocena_niezmienna`, `trg_weryfikacja_wersja`, `trg_powody_poczatkowe`, `dodaj_dni_robocze`, `poziom_z_b`, `utworz_partycje_decyzji`) | — (semantyka bez zmian) |

Komunikat wyjątku zgłaszanego przez trigger (`RAISE EXCEPTION '<KOD>'`,
`SQLSTATE P0001`) jest kodem błędu z 12.2; `Lemat.Infrastructure`
(`TlumaczBledowBazy`) zamienia `PostgresException` z `SqlState = "P0001"`
na `NiezmiennikNaruszonyException(kod)`, a `23505` na indeksach `ux_*` — na
kod przypisany indeksowi w `KatalogIndeksowUnikalnych` (np.
`ux_jeden_proponowany` → `ELEMENT_JUZ_PROPONOWANY`).

## (A) Tabele, typy, ograniczenia, indeksy

```sql
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ===================== TYPY WYLICZENIOWE =====================
CREATE TYPE rola_uzytkownika        AS ENUM ('UCZEN','NAUCZYCIEL','ADMINISTRATOR');
CREATE TYPE status_konta            AS ENUM ('AKTYWNE','OCZEKUJE_NA_ZATWIERDZENIE','ZABLOKOWANE','USUWANE');
CREATE TYPE poziom_matury           AS ENUM ('PODSTAWOWY','ROZSZERZONY');
CREATE TYPE rodzaj_zadania          AS ENUM ('ZAMKNIETE','OTWARTE_OBLICZENIOWE','OTWARTE_Z_DOWODEM');
CREATE TYPE forma_zamknieta         AS ENUM ('WYBOR_JEDNOKROTNY','PRAWDA_FALSZ');
CREATE TYPE pochodzenie_zadania     AS ENUM ('BAZOWE_ARKUSZ_CKE','BAZOWE_NAUCZYCIEL','WYGENEROWANE');
CREATE TYPE zasieg_zadania          AS ENUM ('GLOBALNY','PRYWATNY');
CREATE TYPE status_zadania          AS ENUM ('WALIDACJA','ODRZUCONE_W_WALIDACJI','DO_PRZEGLADU',
                                             'OPUBLIKOWANE','ODRZUCONE','WYCOFANE');
CREATE TYPE status_wkladki          AS ENUM ('SZKIC','DO_PRZEGLADU','OPUBLIKOWANA','WYCOFANA');
CREATE TYPE status_rozwiazania      AS ENUM ('PRZESLANE','ROZPOZNAWANIE','BLAD_ROZPOZNAWANIA',
                                             'TRANSKRYPCJA_GOTOWA','ZATWIERDZONE','OCENIANIE',
                                             'OCZEKUJE_NA_WERYFIKACJE','OCENIONE','PORZUCONE','ANULOWANE');
CREATE TYPE zrodlo_transkrypcji     AS ENUM ('AGENT_ROZPOZNAJACY','POPRAWKA_UCZNIA');
CREATE TYPE status_oceny            AS ENUM ('WSTEPNA','OSTATECZNA','ZASTAPIONA');
CREATE TYPE zrodlo_oceny            AS ENUM ('AUTOMATYCZNA','NAUCZYCIEL');
CREATE TYPE typ_bledu               AS ENUM ('RACHUNKOWY','MERYTORYCZNY','NIEPELNE_ROZWIAZANIE');
CREATE TYPE zrodlo_wskazowki        AS ENUM ('AGENT_WYJASNIAJACY','SZABLON','NAUCZYCIEL');
CREATE TYPE powod_weryfikacji       AS ENUM ('NISKA_PEWNOSC','POPRAWNY_WYNIK_BLEDNE_KROKI','ZADANIE_Z_DOWODEM',
                                             'PODEJRZENIE_BLEDU_ODCZYTU','ZGLOSZENIE_BLEDU_ODCZYTU',
                                             'POPRAWKI_NIEZGODNE_ZE_ZDJECIEM',          -- T-7, ADR-036
                                             'KONTROLA_LOSOWA','BLAD_PRZETWARZANIA');
CREATE TYPE status_weryfikacji      AS ENUM ('OCZEKUJE','W_TRAKCIE','ZAKONCZONA','ANULOWANA');
CREATE TYPE powod_anulowania        AS ENUM ('USUNIECIE_KONTA','PONOWNA_OCENA_AUTOMATYCZNA');
CREATE TYPE typ_kolejki_weryfikacji AS ENUM ('KLASA','GLOBALNA');
CREATE TYPE typ_elementu_sciezki    AS ENUM ('WKLADKA_TEORETYCZNA','ZADANIE');
CREATE TYPE cel_elementu            AS ENUM ('NAUKA','POWTORKA');
CREATE TYPE status_elementu_sciezki AS ENUM ('PROPONOWANY','ROZPOCZETY','UKONCZONY','POMINIETY','WYGASLY');
CREATE TYPE strategia_rekomendacji  AS ENUM ('POLITYKA_ELO','JEV_CHOICE');
CREATE TYPE rola_modelu             AS ENUM ('DECYZYJNY_GLOWNY','DECYZYJNY_ZAPASOWY','ROZPOZNAWANIE_PISMA',
                                             'GENERATYWNY_ZADANIA','GENERATYWNY_ROZWIAZUJACY_NIEZALEZNIE',
                                             'GENERATYWNY_WSKAZOWKI','GENERATYWNY_WKLADKI',
                                             'GENERATYWNY_OPINIA_POMOCNICZA');
CREATE TYPE typ_pytania             AS ENUM ('NOUL','SCORE','CHOICE');
CREATE TYPE kontekst_decyzji        AS ENUM ('OCENA','REKOMENDACJA','WALIDACJA_ZADANIA');
CREATE TYPE zrodlo_wyniku           AS ENUM ('ODPOWIEDZ','OCENA');
CREATE TYPE zrodlo_probki           AS ENUM ('WERYFIKACJA_NISKA_PEWNOSC','KONTROLA_LOSOWA',
                                             'WERYFIKACJA_INNY_POWOD','KOREKTA_NAUCZYCIELA');
CREATE TYPE zrodlo_progu            AS ENUM ('EWALUACJA_ZT1','KALIBRACJA_AUTOMATYCZNA','ADMINISTRATOR','BEZPIECZNIK');
CREATE TYPE zrodlo_kalibratora      AS ENUM ('EWALUACJA_ZT1','KALIBRACJA_AUTOMATYCZNA');   -- rozłączne z zrodlo_progu (3.1 p. 5)
CREATE TYPE powod_uniewaznienia_wyniku AS ENUM ('OCENA_ZASTAPIONA','ZGLOSZENIE_BLEDU_ODCZYTU'); -- 10.1.3, 11.6
CREATE TYPE status_pracy            AS ENUM ('OCZEKUJE','W_TOKU','ZAKONCZONA','MARTWA');
CREATE TYPE status_zlecenia         AS ENUM ('OCZEKUJE','W_TOKU','ZAKONCZONE','NIEUDANE');
CREATE TYPE status_importu          AS ENUM ('OCZEKUJE','PRZETWARZANY','ZAKONCZONY','ODRZUCONY');
CREATE TYPE status_testu            AS ENUM ('W_TOKU','ZAKONCZONY','POMINIETY');
CREATE TYPE decyzja_przegladu       AS ENUM ('ZATWIERDZONE','POPRAWIONE','ODRZUCONE');
CREATE TYPE rezultat_walidacji      AS ENUM ('POZYTYWNY','NEGATYWNY');
CREATE TYPE inicjalizacja_opanowania AS ENUM ('DOMYSLNA','TEST_DIAGNOSTYCZNY','PROPAGACJA_Z_DZIALU');
CREATE TYPE status_zgloszenia_zadania AS ENUM ('OTWARTE','UWZGLEDNIONE','ODRZUCONE');           -- ADR-038
CREATE TYPE typ_zdarzenia_rodo      AS ENUM ('WNIOSEK_O_USUNIECIE','ANULOWANIE_USUNIECIA','USUNIECIE_WYKONANE',
                                             'ODWOLANIE_ZGODY','UDZIELENIE_ZGODY');           -- ADR-041; 2.3: WNIOSEK_O_USUNIECIE (C3-07)
CREATE TYPE zrodlo_zdarzenia_rodo   AS ENUM ('API','WORKER');                                -- ADR-041 (C3-03)
CREATE TYPE typ_bezpiecznika        AS ENUM ('TRAFNOSC','POPRAWKI');                         -- ADR-046 (C3-01)
CREATE TYPE powod_eskalacji         AS ENUM ('PRZEKROCZENIE_SLA','NAUCZYCIEL_DOCELOWY_ZABLOKOWANY',
                                             'KARENCJA_KONTA_NAUCZYCIELA');                   -- 11.4.3 p. 1, 16.4 (C3-08)

-- ===================== KONTA =====================
CREATE TABLE uzytkownik (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email            citext NOT NULL UNIQUE,
    haslo_hash       text   NOT NULL,                 -- argon2id (m=64MiB, t=3, p=4)
    rola             rola_uzytkownika NOT NULL,
    status           status_konta NOT NULL DEFAULT 'AKTYWNE',
    imie             text   NOT NULL CHECK (char_length(imie) BETWEEN 1 AND 60),
    nazwisko         text   NOT NULL CHECK (char_length(nazwisko) BETWEEN 1 AND 80),
    usuniecie_zaplanowane_at timestamptz,             -- RODO: koniec okresu karencji (ADR-040), usunięcie po 30 dniach
    status_przed_wnioskiem   status_konta CHECK (status_przed_wnioskiem IS DISTINCT FROM 'USUWANE'),
                                                      -- 3.1 (C6-06): status odtwarzany przez przywrócenie konta
    ostatnie_logowanie_at    timestamptz,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    version          integer NOT NULL DEFAULT 1,
    CHECK ((status = 'USUWANE') = (usuniecie_zaplanowane_at IS NOT NULL)),
    CHECK ((status = 'USUWANE') = (status_przed_wnioskiem IS NOT NULL))
);

CREATE TABLE uczen (
    uzytkownik_id         uuid PRIMARY KEY REFERENCES uzytkownik(id) ON DELETE CASCADE
                          DEFERRABLE INITIALLY DEFERRED,       -- 3.2 (ADR-063 p. 3): UczenId ≠ UzytkownikId, brak relacji EF
    poziom_matury         poziom_matury,              -- NULL do czasu FR-02
    termin_egzaminu       date,
    wybrany_dzial_id      integer,                    -- FR-09; NULL = wszystkie działy (FK niżej)
    oswiadczenie_wieku_16 boolean NOT NULL CHECK (oswiadczenie_wieku_16),
    zgoda_na_trening      boolean NOT NULL DEFAULT false,   -- zdjęcia do zbioru treningowego (16.4)
    procent_grupy_eksperymentu smallint NOT NULL CHECK (procent_grupy_eksperymentu BETWEEN 0 AND 99),
                                                      -- fnv1a(uzytkownik_id) mod 100; przydział do eksperymentu A/B
    created_at            timestamptz NOT NULL DEFAULT now(),
    version               integer NOT NULL DEFAULT 1
);

CREATE TABLE nauczyciel (
    uzytkownik_id        uuid PRIMARY KEY REFERENCES uzytkownik(id) ON DELETE CASCADE
                         DEFERRABLE INITIALLY DEFERRED,        -- 3.2 (ADR-063 p. 3): NauczycielId ≠ UzytkownikId
    weryfikator_globalny boolean NOT NULL DEFAULT false,  -- obsługuje kolejkę GLOBALNA (ADR-021)
    recenzent_tresci     boolean NOT NULL DEFAULT false,  -- Przegląd Zadań/Wkładek globalnych (ADR-028)
    deklarowane_minuty_weryfikacji smallint NOT NULL DEFAULT 0
                         CHECK (deklarowane_minuty_weryfikacji BETWEEN 0 AND 480),
    umowa_poufnosci_at   timestamptz,                     -- 3.1 (C6-06): data umowy poufności odnotowana przez Administratora
    created_at           timestamptz NOT NULL DEFAULT now(),
    version              integer NOT NULL DEFAULT 1,
    CHECK (NOT weryfikator_globalny OR deklarowane_minuty_weryfikacji >= 90),  -- A-13
    CHECK (NOT weryfikator_globalny OR umowa_poufnosci_at IS NOT NULL)         -- 3.3 „Weryfikator globalny”
);

CREATE TABLE token_odswiezania (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uzytkownik_id   uuid NOT NULL REFERENCES uzytkownik(id) ON DELETE CASCADE,
    rodzina_id      uuid NOT NULL,                    -- wykrywanie ponownego użycia
    token_hash      bytea NOT NULL UNIQUE,            -- SHA-256 tokenu
    wygasa_at       timestamptz NOT NULL,
    uniewazniony_at timestamptz,
    zastapiony_przez uuid REFERENCES token_odswiezania(id) ON DELETE SET NULL,  -- 3.1 (ADR-057): następca po rotacji
    zastapiony_at   timestamptz,                      -- początek 30-sekundowego okna tolerancji
    skrot_klienta_http bytea NOT NULL,                -- SHA-256 nagłówka User-Agent przy wydaniu tokenu
    uzycia_w_oknie_tolerancji smallint NOT NULL DEFAULT 0 CHECK (uzycia_w_oknie_tolerancji BETWEEN 0 AND 5),
    created_at      timestamptz NOT NULL DEFAULT now(),
    CHECK ((zastapiony_at IS NULL) = (zastapiony_przez IS NULL))
);
CREATE INDEX ix_token_rodzina ON token_odswiezania (rodzina_id) WHERE uniewazniony_at IS NULL;
CREATE INDEX ix_token_uzytkownik ON token_odswiezania (uzytkownik_id);

CREATE TABLE proba_logowania (
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email      citext NOT NULL,
    ip         inet   NOT NULL,
    udana      boolean NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_proba_logowania_email ON proba_logowania (email, created_at DESC);

-- Zdarzenia RODO (ADR-041): kopia w bazie dziennika zapisanego wcześniej w buckecie rejestr-zdarzen-rodo
CREATE TABLE zdarzenie_rodo (
    id             uuid PRIMARY KEY,                  -- generowany przez proces zapisujący obiekt do bucketu PRZED zmianą w bazie:
                                                      -- api (gen_random_uuid) albo worker (USUNIECIE_WYKONANE: uuid5(NS_RODO,
                                                      -- 'USUNIECIE_WYKONANE:' || uzytkownik_id) — deterministyczny, ponowienie USUN_KONTO
                                                      -- w dowolnym dniu trafia w ten sam klucz obiektu bez daty; C3-03, C4-06 p. 3)
    uzytkownik_id  uuid NOT NULL,                     -- bez FK: wpis przeżywa usunięcie konta
    typ            typ_zdarzenia_rodo NOT NULL,
    zrodlo         zrodlo_zdarzenia_rodo NOT NULL DEFAULT 'API',
                                                      -- WORKER: stanem docelowym jest wykonanie usunięcia, a nie transakcja;
                                                      -- UZGODNIJ_REJESTR_RODO nigdy go nie unieważnia (13.3)
    dane           jsonb NOT NULL DEFAULT '{}',       -- zawsze {"rola": "UCZEN"|"NAUCZYCIEL"|"ADMINISTRATOR", …} (od 2.4, C4-01),
                                                      -- np. {"rola":"UCZEN","usuniecie_zaplanowane_at":"…"}; bez danych osobowych
    klucz_obiektu  text NOT NULL UNIQUE,              -- API: rejestr-zdarzen-rodo/zdarzenia/{rrrr-mm-dd created_at}/{uzytkownik_id}/{id}.json
                                                      -- WORKER: rejestr-zdarzen-rodo/wykonane/{uzytkownik_id}/{id}.json (bez daty, C4-06 p. 3)
    created_at     timestamptz NOT NULL DEFAULT now(),
    CHECK ((zrodlo = 'WORKER') = (typ = 'USUNIECIE_WYKONANE')),
    CHECK (dane ? 'rola')                             -- rola konta w każdym zdarzeniu (ponowienie wg roli, 16.4, C4-01)
);
CREATE INDEX ix_zdarzenie_rodo_uzytkownik ON zdarzenie_rodo (uzytkownik_id, created_at);

-- Kalendarz dni roboczych (A-15, ADR-043)
CREATE TABLE dzien_wolny (
    data  date PRIMARY KEY,                           -- dzień ustawowo wolny od pracy (Polska)
    nazwa text NOT NULL
);
-- Funkcja dodaj_dni_robocze(od timestamptz, n integer) RETURNS timestamptz (STABLE, strefa Europe/Warsaw):
-- jeśli `od` przypada na sobotę, niedzielę lub dzień z dzien_wolny, liczenie zaczyna się od najbliższego
-- dnia roboczego o 08:00; następnie dodaje n dni roboczych z zachowaniem godziny. Przykłady:
-- piątek 18:00 + 3 → środa 18:00; sobota 10:00 + 3 → czwartek 08:00. Dane: migracja z dniami wolnymi
-- 2026–2030; praca UTRZYMAJ_PARTYCJE alarmuje, gdy brak wpisów na 12 miesięcy naprzód.

-- ===================== DZIAŁY, UMIEJĘTNOŚCI, WKŁADKI =====================
CREATE TABLE dzial (
    id                integer PRIMARY KEY,
    kod               text NOT NULL UNIQUE,           -- np. 'XIII'
    nazwa             text NOT NULL,
    tylko_rozszerzony boolean NOT NULL DEFAULT false,
    kolejnosc         smallint NOT NULL
);
ALTER TABLE uczen ADD CONSTRAINT fk_uczen_dzial FOREIGN KEY (wybrany_dzial_id) REFERENCES dzial(id);

CREATE TABLE umiejetnosc (
    id                 integer PRIMARY KEY,
    dzial_id           integer NOT NULL REFERENCES dzial(id),
    kod                text NOT NULL UNIQUE,          -- np. 'R.XIII.2' (poziom.dział.punkt podstawy)
    nazwa              text NOT NULL,
    tylko_rozszerzony  boolean NOT NULL DEFAULT false,
    waga_egzaminacyjna numeric(4,3) NOT NULL CHECK (waga_egzaminacyjna > 0 AND waga_egzaminacyjna <= 1)
);

CREATE TABLE umiejetnosc_wymagana (
    umiejetnosc_id          integer NOT NULL REFERENCES umiejetnosc(id),
    wymagana_umiejetnosc_id integer NOT NULL REFERENCES umiejetnosc(id),
    PRIMARY KEY (umiejetnosc_id, wymagana_umiejetnosc_id),
    CHECK (umiejetnosc_id <> wymagana_umiejetnosc_id)
);

CREATE TABLE wkladka_teoretyczna (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    umiejetnosc_id     integer NOT NULL REFERENCES umiejetnosc(id),
    tytul              text NOT NULL,
    tresc_md           text NOT NULL CHECK (char_length(tresc_md) <= 12000), -- Markdown + LaTeX ($…$)
    szacowany_czas_min smallint NOT NULL CHECK (szacowany_czas_min BETWEEN 1 AND 15),
    wygenerowana       boolean NOT NULL,              -- true = szkic Agenta generującego
    wygenerowana_automatycznie boolean NOT NULL DEFAULT false, -- true = GENERUJ_SZKIC_WKLADKI (E-20); budżet 12.7
    koszt_generowania_pln numeric(10,4) NOT NULL DEFAULT 0,
    status             status_wkladki NOT NULL DEFAULT 'SZKIC',
    zatwierdzona_przez uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,  -- recenzent treści lub Administrator
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now(),
    version            integer NOT NULL DEFAULT 1
);
CREATE UNIQUE INDEX ux_wkladka_opublikowana ON wkladka_teoretyczna (umiejetnosc_id) WHERE status = 'OPUBLIKOWANA';
CREATE INDEX ix_wkladka_automatyczne_odrzucone ON wkladka_teoretyczna (umiejetnosc_id, updated_at)
    WHERE wygenerowana_automatycznie AND status = 'WYCOFANA';   -- limit 2 odrzuceń / 30 dni (13.3)

-- ===================== ARKUSZE CKE, ZADANIA =====================
CREATE TABLE arkusz_cke (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    kod            text NOT NULL UNIQUE,              -- np. 'MMA-P1-2024-05'
    rok            smallint NOT NULL CHECK (rok BETWEEN 2005 AND 2100),
    sesja          text NOT NULL CHECK (sesja IN ('MAJ','CZERWIEC','SIERPIEN','PROBNA','INFORMATOR')),
    poziom_matury  poziom_matury NOT NULL,
    created_at     timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE import_arkusza (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    administrator_id uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,  -- NULL po usunięciu konta (ADR-044)
    klucz_obiektu    text NOT NULL,                   -- ZIP w magazynie obiektów
    status           status_importu NOT NULL DEFAULT 'OCZEKUJE',
    raport           jsonb,                            -- lista błędów per numer zadania
    arkusz_cke_id    uuid REFERENCES arkusz_cke(id),
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE zlecenie_generowania (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    umiejetnosc_id   integer NOT NULL REFERENCES umiejetnosc(id),
    poziom_trudnosci smallint NOT NULL CHECK (poziom_trudnosci BETWEEN 1 AND 5),  -- nominalny docelowy
    poziom_matury    poziom_matury NOT NULL,
    rodzaj           rodzaj_zadania NOT NULL,
    zlecajacy_id     uuid REFERENCES uzytkownik(id) ON DELETE SET NULL, -- NULL = system (UZUPELNIJ_PULE)
    systemowe        boolean NOT NULL,                -- true ⇔ utworzone przez UZUPELNIJ_PULE (budżet 13.3)
    status           status_zlecenia NOT NULL DEFAULT 'OCZEKUJE',
    liczba_cykli     smallint NOT NULL DEFAULT 0 CHECK (liczba_cykli BETWEEN 0 AND 3),
                     -- cykl = generowanie + Walidacja; ponowienia techniczne pracy w tle się nie liczą (C1-28)
    koszt_pln        numeric(10,4) NOT NULL DEFAULT 0, -- suma kosztów wywołań modeli (13.1, raport adaptera)
    zadanie_id       uuid,                             -- FK niżej
    przyczyna_niepowodzenia text,
    wywolania_narzedzi jsonb NOT NULL DEFAULT '[]',    -- 3.1 (ADR-058 p. 6): ślad wywołań funkcji pluginów, schemat 7.5.5
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    CHECK (jsonb_typeof(wywolania_narzedzi) = 'array'),
    CHECK (NOT systemowe OR (rodzaj <> 'OTWARTE_Z_DOWODEM' AND zlecajacy_id IS NULL))  -- system nie generuje dowodów (ADR-015)
);
CREATE INDEX ix_zlecenie_systemowe_dzien ON zlecenie_generowania (created_at) WHERE systemowe;

CREATE TABLE zadanie (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    pochodzenie            pochodzenie_zadania NOT NULL,
    zasieg                 zasieg_zadania NOT NULL,
    rodzaj                 rodzaj_zadania NOT NULL,
    forma_zamknieta        forma_zamknieta,
    dzial_id               integer NOT NULL REFERENCES dzial(id),
    poziom_matury          poziom_matury NOT NULL,
    arkusz_cke_id          uuid REFERENCES arkusz_cke(id),
    numer_w_arkuszu        text,
    autor_id               uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,
    zlecenie_generowania_id uuid REFERENCES zlecenie_generowania(id),
    status                 status_zadania NOT NULL,
    aktualna_wersja_id     uuid,                       -- FK DEFERRABLE niżej
    poziom_trudnosci_nominalny smallint NOT NULL CHECK (poziom_trudnosci_nominalny BETWEEN 1 AND 5),
    diagnostyczne          boolean NOT NULL DEFAULT false,
    zweryfikowane_przez_nauczyciela boolean NOT NULL DEFAULT false,
    created_at             timestamptz NOT NULL DEFAULT now(),
    updated_at             timestamptz NOT NULL DEFAULT now(),
    version                integer NOT NULL DEFAULT 1,
    CHECK ((rodzaj = 'ZAMKNIETE') = (forma_zamknieta IS NOT NULL)),
    CHECK ((pochodzenie = 'BAZOWE_ARKUSZ_CKE') = (arkusz_cke_id IS NOT NULL)),
    CHECK ((pochodzenie = 'WYGENEROWANE') = (zlecenie_generowania_id IS NOT NULL)),
    CHECK (pochodzenie <> 'BAZOWE_ARKUSZ_CKE' OR zasieg = 'GLOBALNY'),
    CHECK (NOT diagnostyczne OR (rodzaj = 'ZAMKNIETE' AND zasieg = 'GLOBALNY'))
);
ALTER TABLE zlecenie_generowania ADD CONSTRAINT fk_zlecenie_zadanie FOREIGN KEY (zadanie_id) REFERENCES zadanie(id);
CREATE INDEX ix_zadanie_dobor ON zadanie (poziom_matury, status, zasieg, rodzaj);
CREATE INDEX ix_zadanie_autor ON zadanie (autor_id) WHERE zasieg = 'PRYWATNY';
CREATE INDEX ix_zadanie_diagnostyczne ON zadanie (poziom_matury, dzial_id, poziom_trudnosci_nominalny)
    WHERE diagnostyczne AND status = 'OPUBLIKOWANE';      -- pula Testu diagnostycznego (9.1, G-5)

-- Kalibracja Poziomu trudności (ADR-029): statystyka aktualizowana atomowo, poza zadanie.version
CREATE TABLE kalibracja_trudnosci (
    zadanie_id                uuid PRIMARY KEY REFERENCES zadanie(id),
    trudnosc_b                numeric(6,3) NOT NULL,   -- parametr modelu Elo (10.1.3)
    liczba_wynikow_kalibracji integer NOT NULL DEFAULT 0,
    poziom_trudnosci          smallint NOT NULL CHECK (poziom_trudnosci BETWEEN 1 AND 5),
                                                       -- = nominalny, dopóki liczba_wynikow_kalibracji < 30
    updated_at                timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_kalibracja_poziom ON kalibracja_trudnosci (poziom_trudnosci);

CREATE TABLE zadanie_wersja (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_id           uuid NOT NULL REFERENCES zadanie(id),
    nr_wersji            integer NOT NULL CHECK (nr_wersji >= 1),
    tresc_md             text NOT NULL CHECK (char_length(tresc_md) <= 8000),
    opcje_zamkniete jsonb,     -- schemat 7.5.1; NOT NULL dla ZAMKNIETE
    rozwiazanie_wzorcowe jsonb NOT NULL, -- schemat 7.5.2
    wynik_koncowy_wzorcowy jsonb,   -- schemat 7.5.3; NULL wyłącznie dla OTWARTE_Z_DOWODEM i ZAMKNIETE
    punkty_max           smallint NOT NULL CHECK (punkty_max BETWEEN 1 AND 6),
    utworzona_przez      uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,
    zastapiona_at        timestamptz,                  -- chwila, w której przestała być aktualną wersją
    created_at           timestamptz NOT NULL DEFAULT now(),
    UNIQUE (zadanie_id, nr_wersji)
);
ALTER TABLE zadanie ADD CONSTRAINT fk_zadanie_aktualna_wersja
    FOREIGN KEY (aktualna_wersja_id) REFERENCES zadanie_wersja(id) DEFERRABLE INITIALLY DEFERRED;
CREATE INDEX ix_zadanie_wersja_tresc_trgm ON zadanie_wersja USING gin (tresc_md gin_trgm_ops);

CREATE TABLE zadanie_umiejetnosc (
    zadanie_id     uuid    NOT NULL REFERENCES zadanie(id),
    umiejetnosc_id integer NOT NULL REFERENCES umiejetnosc(id),
    glowna         boolean NOT NULL,
    PRIMARY KEY (zadanie_id, umiejetnosc_id)
);
CREATE UNIQUE INDEX ux_zadanie_umiejetnosc_glowna ON zadanie_umiejetnosc (zadanie_id) WHERE glowna;
-- Constraint trigger trg_umiejetnosc_glowna_w_dziale: umiejętność główna należy do zadanie.dzial_id.

CREATE TABLE kryterium_oceniania (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_wersja_id   uuid NOT NULL REFERENCES zadanie_wersja(id),
    wariant             smallint NOT NULL DEFAULT 1 CHECK (wariant BETWEEN 1 AND 4),
    nr_kryterium        smallint NOT NULL CHECK (nr_kryterium BETWEEN 1 AND 12),
    opis                text NOT NULL,
    punkty_max          smallint NOT NULL CHECK (punkty_max BETWEEN 1 AND 3),
    czy_wynik_koncowy   boolean NOT NULL DEFAULT false,
    wymaga_nr_kryterium smallint,                     -- punkt tylko po zaliczeniu wskazanego kryterium
    UNIQUE (zadanie_wersja_id, wariant, nr_kryterium),
    CHECK (wymaga_nr_kryterium IS NULL OR wymaga_nr_kryterium < nr_kryterium)
);
-- Constraint trigger DEFERRABLE trg_suma_kryteriow: dla każdego wariantu
-- SUM(punkty_max) = zadanie_wersja.punkty_max oraz co najwyżej jedno czy_wynik_koncowy;
-- suma liczby kryteriów wszystkich wariantów ≤ 24 (limit planu pytań modelu zapasowego, 13.2).
-- Dla zadań ZAMKNIETE kryteriów nie ma (ocena kluczem).

CREATE TABLE zalacznik_zadania (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_wersja_id uuid NOT NULL REFERENCES zadanie_wersja(id),
    klucz_obiektu     text NOT NULL,
    typ_mime          text NOT NULL CHECK (typ_mime IN ('image/png','image/svg+xml','image/jpeg')),
    tekst_alternatywny text NOT NULL,                 -- opis rysunku, przekazywany też do modeli
    kolejnosc         smallint NOT NULL
);

CREATE TABLE walidacja_zadania (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_wersja_id uuid NOT NULL REFERENCES zadanie_wersja(id),
    nr_cyklu          smallint NOT NULL CHECK (nr_cyklu BETWEEN 1 AND 3),
    rezultat          rezultat_walidacji NOT NULL,
    sprawdzenia       jsonb NOT NULL,                 -- schemat 10.2.3
    created_at        timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE przeglad_zadania (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_id         uuid NOT NULL REFERENCES zadanie(id),
    autor_przegladu_id uuid REFERENCES uzytkownik(id) ON DELETE SET NULL, -- Nauczyciel uprawniony (ADR-028) lub Administrator
    decyzja            decyzja_przegladu NOT NULL,
    komentarz          text,
    nowa_wersja_id     uuid REFERENCES zadanie_wersja(id),
    zasieg_po          zasieg_zadania NOT NULL,       -- zasięg Zadania po Przeglądzie
    created_at         timestamptz NOT NULL DEFAULT now(),
    CHECK ((decyzja = 'POPRAWIONE') = (nowa_wersja_id IS NOT NULL))
);

-- Zgłoszenie błędu w Zadaniu (FR-14, ADR-038): Nauczyciel bez prawa Przeglądu zgłasza błąd Zadania GLOBALNY
CREATE TABLE zgloszenie_bledu_zadania (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zadanie_id             uuid NOT NULL REFERENCES zadanie(id),
    zadanie_wersja_id      uuid NOT NULL REFERENCES zadanie_wersja(id),  -- wersja, której dotyczy zgłoszenie
    nauczyciel_id          uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL,
    opis                   text NOT NULL CHECK (char_length(opis) BETWEEN 10 AND 1000),
    status                 status_zgloszenia_zadania NOT NULL DEFAULT 'OTWARTE',
    rozpatrzone_przez      uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,   -- recenzent treści lub Administrator
    przeglad_zadania_id    uuid REFERENCES przeglad_zadania(id),               -- Przegląd wykonany przy rozpatrzeniu
    komentarz_rozpatrzenia text CHECK (char_length(komentarz_rozpatrzenia) <= 1000),
    created_at             timestamptz NOT NULL DEFAULT now(),
    rozpatrzono_at         timestamptz,
    CHECK ((status = 'OTWARTE') = (rozpatrzono_at IS NULL)),
    CHECK (status <> 'UWZGLEDNIONE' OR przeglad_zadania_id IS NOT NULL)
);
CREATE UNIQUE INDEX ux_zgloszenie_zadania_otwarte ON zgloszenie_bledu_zadania (zadanie_id, nauczyciel_id)
    WHERE status = 'OTWARTE';                              -- 1 otwarte zgłoszenie Nauczyciela na Zadanie
CREATE INDEX ix_zgloszenie_zadania_status ON zgloszenie_bledu_zadania (status, created_at);

-- Wstrzymanie w doborze (ADR-038, C3-09): stan systemowy poza wierszem zadanie, więc nie zmienia zadanie.version
-- (recenzent rozpatrujący zgłoszenie z If-Match nie dostaje 412 po nowym zgłoszeniu; wzorzec ADR-029).
-- Istnienie wiersza = Zadanie wstrzymane; usuwany po rozpatrzeniu wszystkich otwartych zgłoszeń (8.4).
CREATE TABLE wstrzymanie_w_doborze (
    zadanie_id        uuid PRIMARY KEY REFERENCES zadanie(id) ON DELETE CASCADE,
    liczba_zgloszen   smallint NOT NULL CHECK (liczba_zgloszen >= 3),  -- otwarte zgłoszenia od różnych Nauczycieli w chwili wstrzymania
    created_at        timestamptz NOT NULL DEFAULT now()
);

-- ===================== KLASY, ZESTAWY, PRZYPISANIA =====================
CREATE TABLE klasa (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nauczyciel_id   uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL, -- NULL tylko po usunięciu konta bez Przekazania Klasy (ADR-044)
    nazwa           text NOT NULL CHECK (char_length(nazwa) BETWEEN 1 AND 80),
    poziom_matury   poziom_matury NOT NULL,
    kod_zaproszenia char(8) NOT NULL UNIQUE,          -- alfabet [A-HJ-NP-Z2-9]
    kod_wazny_do    timestamptz NOT NULL,
    zarchiwizowana  boolean NOT NULL DEFAULT false,
    zarchiwizowano_at timestamptz,
    created_at      timestamptz NOT NULL DEFAULT now(),
    version         integer NOT NULL DEFAULT 1,
    CHECK (zarchiwizowana = (zarchiwizowano_at IS NOT NULL)),
    CHECK (nauczyciel_id IS NOT NULL OR zarchiwizowana)
);

CREATE TABLE czlonkostwo_klasy (
    klasa_id    uuid NOT NULL REFERENCES klasa(id) ON DELETE CASCADE,
    uczen_id    uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    dolaczono_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (klasa_id, uczen_id)
);
CREATE INDEX ix_czlonkostwo_uczen ON czlonkostwo_klasy (uczen_id);

CREATE TABLE zestaw_zadan (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nauczyciel_id uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL,   -- NULL po usunięciu konta (ADR-044)
    nazwa         text NOT NULL CHECK (char_length(nazwa) BETWEEN 1 AND 120),
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    version       integer NOT NULL DEFAULT 1
);

CREATE TABLE pozycja_zestawu (
    zestaw_zadan_id uuid NOT NULL REFERENCES zestaw_zadan(id) ON DELETE CASCADE,
    kolejnosc       smallint NOT NULL,
    zadanie_id      uuid NOT NULL REFERENCES zadanie(id),
    PRIMARY KEY (zestaw_zadan_id, kolejnosc),
    UNIQUE (zestaw_zadan_id, zadanie_id)
);

CREATE TABLE przypisanie (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    zestaw_zadan_id uuid NOT NULL REFERENCES zestaw_zadan(id),   -- RESTRICT: Zestawu z Przypisaniem nie można usunąć
    nauczyciel_id   uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL, -- NULL po usunięciu konta (ADR-044)
    klasa_id        uuid REFERENCES klasa(id),        -- informacyjnie; adresaci w przypisanie_uczen
    termin          timestamptz NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),          -- 3.3 (C8-11): zmiana terminu albo wycofanie
    version         integer NOT NULL DEFAULT 1,                  -- 3.3: token współbieżności (If-Match P, ADR-074)
    wycofano_at     timestamptz,                                 -- 3.3: wycofanie przez Nauczyciela (ADR-074)
    powod_wycofania text CHECK (powod_wycofania IS NULL OR char_length(powod_wycofania) BETWEEN 10 AND 1000),
    CHECK ((wycofano_at IS NULL) = (powod_wycofania IS NULL))
);
CREATE INDEX ix_przypisanie_nauczyciel ON przypisanie (nauczyciel_id, termin DESC);

CREATE TABLE przypisanie_uczen (
    przypisanie_id uuid NOT NULL REFERENCES przypisanie(id) ON DELETE CASCADE,
    uczen_id       uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    PRIMARY KEY (przypisanie_id, uczen_id)
);
CREATE INDEX ix_przypisanie_uczen_uczen ON przypisanie_uczen (uczen_id);

-- ===================== PROFIL UCZNIA, TEST DIAGNOSTYCZNY =====================
CREATE TABLE profil_ucznia (
    uczen_id   uuid PRIMARY KEY REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    updated_at timestamptz NOT NULL DEFAULT now(),
    version    integer NOT NULL DEFAULT 1
);

CREATE TABLE opanowanie_umiejetnosci (
    uczen_id             uuid    NOT NULL REFERENCES profil_ucznia(uczen_id) ON DELETE CASCADE,
    umiejetnosc_id       integer NOT NULL REFERENCES umiejetnosc(id),
    theta                numeric(6,3) NOT NULL DEFAULT 0,  -- stan DOMYSLNY = 0; odtwarzanie zawsze od 0 (10.1.3)
    poziom_opanowania    numeric(4,3) NOT NULL CHECK (poziom_opanowania BETWEEN 0 AND 1),
    liczba_wynikow       integer NOT NULL DEFAULT 0,
    ostatni_wynik_at     timestamptz,
    licznik_powtorek     smallint NOT NULL DEFAULT 0,
    nastepna_powtorka_at timestamptz,
    inicjalizacja        inicjalizacja_opanowania NOT NULL DEFAULT 'DOMYSLNA',
    PRIMARY KEY (uczen_id, umiejetnosc_id)
);
CREATE INDEX ix_opanowanie_powtorki ON opanowanie_umiejetnosci (uczen_id, nastepna_powtorka_at);

CREATE TABLE test_diagnostyczny (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id      uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    poziom_matury poziom_matury NOT NULL,
    status        status_testu NOT NULL DEFAULT 'W_TOKU',
    pominiete_dzialy integer[] NOT NULL DEFAULT '{}', -- Działy bez Zadań diagnostycznych w chwili utworzenia (9.1)
    created_at    timestamptz NOT NULL DEFAULT now(),
    zakonczono_at timestamptz,
    CHECK ((status = 'W_TOKU') = (zakonczono_at IS NULL))
);
-- Test wykonuje się raz na poziom matury; POMINIETY można później zastąpić jednym testem (C1-11).
CREATE UNIQUE INDEX ux_test_jeden ON test_diagnostyczny (uczen_id, poziom_matury)
    WHERE status IN ('W_TOKU','ZAKONCZONY');

CREATE TABLE pozycja_testu_diagnostycznego (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    test_diagnostyczny_id uuid NOT NULL REFERENCES test_diagnostyczny(id) ON DELETE CASCADE,
    kolejnosc             smallint NOT NULL,
    zadanie_wersja_id     uuid NOT NULL REFERENCES zadanie_wersja(id),
    UNIQUE (test_diagnostyczny_id, kolejnosc)
);

-- ===================== ŚCIEŻKA NAUKI, REKOMENDACJE =====================
CREATE TABLE sciezka_nauki (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id                 uuid NOT NULL UNIQUE REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    seria_umiejetnosc_id     integer REFERENCES umiejetnosc(id),   -- aktywna Seria (ADR-030); NULL = brak
    seria_rozpoczeta_at      timestamptz,
    seria_liczba_elementow   smallint NOT NULL DEFAULT 0 CHECK (seria_liczba_elementow BETWEEN 0 AND 8),
    seria_poziom_trudnosci   smallint CHECK (seria_poziom_trudnosci BETWEEN 1 AND 5),
    seria_kolejne_porazki    smallint NOT NULL DEFAULT 0,
    ostatnia_kolejnosc       integer NOT NULL DEFAULT 0 CHECK (ostatnia_kolejnosc >= 0),
                                                       -- 3.1 (C6-15 p. 4): numer ostatniego elementu; agregat nie ładuje historii
    created_at               timestamptz NOT NULL DEFAULT now(),
    version                  integer NOT NULL DEFAULT 1,
    CHECK ((seria_umiejetnosc_id IS NULL) = (seria_rozpoczeta_at IS NULL))
);

CREATE TABLE element_sciezki (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sciezka_nauki_id       uuid NOT NULL REFERENCES sciezka_nauki(id) ON DELETE CASCADE,
    kolejnosc              integer NOT NULL,
    typ                    typ_elementu_sciezki NOT NULL,
    cel                    cel_elementu NOT NULL DEFAULT 'NAUKA',
    umiejetnosc_id         integer NOT NULL REFERENCES umiejetnosc(id),
    zadanie_id             uuid REFERENCES zadanie(id),
    zadanie_wersja_id      uuid REFERENCES zadanie_wersja(id), -- wersja pokazana Uczniowi; ustawiana przy rozpoczęciu (C1-20)
    wkladka_teoretyczna_id uuid REFERENCES wkladka_teoretyczna(id),
    przypisanie_id         uuid REFERENCES przypisanie(id),  -- tylko reguła R-PRZYPISANIE; dziedziczone przez próbę (ADR-032)
    w_serii                boolean NOT NULL DEFAULT false,
    status                 status_elementu_sciezki NOT NULL DEFAULT 'PROPONOWANY',
    wygasa_at              timestamptz NOT NULL,       -- 8.5: PROPONOWANY +24 h; ROZPOCZETY: Zadanie +7 dni, Wkładka +24 h;
                                                       -- R-PRZYPISANIE z Zadaniem zamkniętym: najpóźniej termin Przypisania
    created_at             timestamptz NOT NULL DEFAULT now(),
    rozpoczeto_at          timestamptz,
    zakonczono_at          timestamptz,
    UNIQUE (sciezka_nauki_id, kolejnosc),
    CHECK ((typ = 'WKLADKA_TEORETYCZNA') = (wkladka_teoretyczna_id IS NOT NULL)),
    CHECK ((typ = 'ZADANIE') = (zadanie_id IS NOT NULL)),
    CHECK (typ = 'ZADANIE' OR cel = 'NAUKA'),
    CHECK (status NOT IN ('ROZPOCZETY','UKONCZONY') OR rozpoczeto_at IS NOT NULL),
    CHECK (typ <> 'ZADANIE' OR status NOT IN ('ROZPOCZETY','UKONCZONY') OR zadanie_wersja_id IS NOT NULL)
);
CREATE UNIQUE INDEX ux_jeden_proponowany ON element_sciezki (sciezka_nauki_id) WHERE status = 'PROPONOWANY';
CREATE INDEX ix_element_wygasanie ON element_sciezki (wygasa_at) WHERE status IN ('PROPONOWANY','ROZPOCZETY');

CREATE TABLE rekomendacja (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id              uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    element_sciezki_id    uuid NOT NULL UNIQUE REFERENCES element_sciezki(id) ON DELETE CASCADE,
    strategia             strategia_rekomendacji NOT NULL,
    regula                text NOT NULL CHECK (regula IN ('R-PRZYPISANIE','R-SERIA','R-POWTORKA','R-WKLADKA',
                                 'R-WKLADKA-DIAG','R-NAJSLABSZA','R-TRUDNOSC_W_GORE','R-UTRWALENIE',
                                 'R-JEV-WKLADKA','R-JEV-ZADANIE_LATWE','R-JEV-ZADANIE_MATURALNE','R-JEV-POWTORKA')),
    cechy                 jsonb NOT NULL,             -- migawka wejścia (10.1.4)
    decyzja               jsonb NOT NULL,             -- kandydaci, wybór, powrot_do_polityki
    przewidywane_p_sukcesu numeric(4,3),
    uzasadnienie          text NOT NULL,              -- FR-10, tekst dla Ucznia
    uzasadnienie_fakty    jsonb NOT NULL,             -- fakty użyte w szablonie
    created_at            timestamptz NOT NULL DEFAULT now()
);

-- ===================== ROZWIĄZANIA, TRANSKRYPCJE, ODPOWIEDZI =====================
CREATE TABLE rozwiazanie (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id                 uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    zadanie_id               uuid NOT NULL REFERENCES zadanie(id),
    zadanie_wersja_id        uuid NOT NULL REFERENCES zadanie_wersja(id),
    element_sciezki_id       uuid REFERENCES element_sciezki(id) ON DELETE SET NULL,
    przypisanie_id           uuid REFERENCES przypisanie(id),  -- także odziedziczone z elementu R-PRZYPISANIE (trg_kontekst_proby)
    status                   status_rozwiazania NOT NULL DEFAULT 'PRZESLANE',
    liczba_przeslan          smallint NOT NULL DEFAULT 1 CHECK (liczba_przeslan BETWEEN 1 AND 3),
                             -- przesłania zdjęć przez Ucznia (nowe zdjęcia), C1-28
    liczba_ponowien_rozpoznania smallint NOT NULL DEFAULT 0 CHECK (liczba_ponowien_rozpoznania BETWEEN 0 AND 5),
                             -- ponowienia rozpoznania tych samych zdjęć zlecone przez Ucznia po błędzie dostawcy
    kod_bledu_rozpoznawania  text CHECK (kod_bledu_rozpoznawania IN
                                 ('NIECZYTELNE','PRZEKROCZONY_CZAS','BLAD_DOSTAWCY','ZBYT_WIELE_KROKOW')),
    zatwierdzona_transkrypcja_id uuid,                 -- FK niżej
    najnowsza_transkrypcja_id uuid,                    -- 3.1 (C6-04): najnowsza wersja Transkrypcji; FK niżej
    najnowsza_wersja_transkrypcji smallint,            -- jej numer wersji (warunek zatwierdzenia, 409 TRANSKRYPCJA_NIEAKTUALNA)
    przeslano_at             timestamptz NOT NULL DEFAULT now(),   -- ostatnie Przesłanie nowych zdjęć (3.1: ponowienie rozpoznania go NIE zmienia, A-11)
    ostatnie_rozpoznanie_od_at timestamptz NOT NULL DEFAULT now(),
                             -- 3.1 (C6-04): początek ostatniego rozpoznania (Przesłanie albo ponowienie); termin pracy ROZPOZNAJ_PISMO, porzucenie po 7 dniach
    transkrypcja_gotowa_at   timestamptz,              -- pierwsza Transkrypcja agenta po ostatnim Przesłaniu
    zatwierdzono_at          timestamptz,
    ocena_wystawiona_at      timestamptz,              -- pierwsza Ocena widoczna dla Ucznia (NFR-01); okno zgłoszenia liczone od Oceny aktualnej (11.6)
    czas_systemowy_ms        integer,                  -- NFR-01: (transkrypcja_gotowa − przeslano) + (ocena_wystawiona − zatwierdzono)
    created_at               timestamptz NOT NULL DEFAULT now(),
    updated_at               timestamptz NOT NULL DEFAULT now(),
    version                  integer NOT NULL DEFAULT 1,
    -- kontekst próby (ADR-032, C1-21): element Ścieżki nauki, pozycja Przypisania albo oba — wtedy
    -- przypisanie_id = element_sciezki.przypisanie_id (element R-PRZYPISANIE), co egzekwuje trg_kontekst_proby
    CHECK (element_sciezki_id IS NOT NULL OR przypisanie_id IS NOT NULL),
    CHECK ((najnowsza_transkrypcja_id IS NULL) = (najnowsza_wersja_transkrypcji IS NULL)),
    CHECK (ostatnie_rozpoznanie_od_at >= przeslano_at)
);
CREATE INDEX ix_rozwiazanie_uczen ON rozwiazanie (uczen_id, created_at DESC);
CREATE INDEX ix_rozwiazanie_status ON rozwiazanie (status, updated_at);
CREATE UNIQUE INDEX ux_rozwiazanie_element ON rozwiazanie (element_sciezki_id)
    WHERE element_sciezki_id IS NOT NULL AND status NOT IN ('PORZUCONE','ANULOWANE');
CREATE UNIQUE INDEX ux_rozwiazanie_przypisanie ON rozwiazanie (uczen_id, przypisanie_id, zadanie_id)
    WHERE przypisanie_id IS NOT NULL AND status NOT IN ('PORZUCONE','ANULOWANE');
-- Trigger trg_kontekst_proby (BEFORE INSERT na rozwiazanie i odpowiedz), C1-21:
--   gdy element_sciezki_id IS NOT NULL: e := element_sciezki(element_sciezki_id);
--     e.przypisanie_id IS NOT NULL i NEW.przypisanie_id IS NULL → NEW.przypisanie_id := e.przypisanie_id (dziedziczenie);
--     NEW.przypisanie_id IS NOT NULL i NEW.przypisanie_id IS DISTINCT FROM e.przypisanie_id → wyjątek 'KONTEKST_PROBY_NIESPOJNY'.
--   Dzięki temu ux_*_przypisanie obejmują obie drogi (element R-PRZYPISANIE i lista Przypisań).

-- 3.3 (C8-10, ADR-073): robocza postać Poprawek — autozapis co 1,5 s bez nowej wersji Transkrypcji i bez
-- Sprawdzenia zgodności poprawek; usuwana przy zatwierdzeniu, porzuceniu, nowym Przesłaniu i zapisie wersji domenowej
CREATE TABLE szkic_transkrypcji (
    rozwiazanie_id      uuid PRIMARY KEY REFERENCES rozwiazanie(id) ON DELETE CASCADE,
    transkrypcja_id     uuid NOT NULL,                    -- wersja Transkrypcji, od której zaczęto edycję (FK niżej)
    kroki               jsonb NOT NULL CHECK (jsonb_typeof(kroki) = 'array' AND jsonb_array_length(kroki) BETWEEN 1 AND 40),
                        -- [{nr_kroku_agenta: int|null, tekst: string ≤ 2000, latex: string ≤ 2000}] (schemat 7.5, TranskrypcjaWejscie)
    numer_zapisu        integer NOT NULL CHECK (numer_zapisu >= 1),   -- monotoniczny numer zapisu z karty (odrzucenie zapisu starszego)
    zapisano_at         timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE zdjecie_rozwiazania (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rozwiazanie_id uuid NOT NULL REFERENCES rozwiazanie(id) ON DELETE CASCADE,
    przeslanie_nr  smallint NOT NULL CHECK (przeslanie_nr BETWEEN 1 AND 3),
    kolejnosc      smallint NOT NULL CHECK (kolejnosc BETWEEN 1 AND 5),
    klucz_obiektu  text NOT NULL UNIQUE,              -- zdjecia/{uczen_id}/{rozwiazanie_id}/{id}.jpg
    klucz_obiektu_maskowany text UNIQUE,              -- zdjecia-maskowane/…; tworzony w ROZPOZNAJ_PISMO (ADR-031)
    obszary_maskowania jsonb NOT NULL DEFAULT '[]',   -- [{"ramka":[x,y,w,h]}] z Agenta rozpoznającego
    typ_mime       text NOT NULL CHECK (typ_mime = 'image/jpeg'), -- po normalizacji
    rozmiar_bajty  integer NOT NULL CHECK (rozmiar_bajty <= 10485760),
    sha256         bytea NOT NULL,
    szerokosc      smallint NOT NULL,
    wysokosc       smallint NOT NULL,
    usun_po        timestamptz NOT NULL,              -- utrzymywane przez przejścia stanów (16.4); ≤ created_at + 90 dni
    usunieto_at    timestamptz,
    created_at     timestamptz NOT NULL DEFAULT now(),
    UNIQUE (rozwiazanie_id, przeslanie_nr, kolejnosc),
    CHECK (usun_po <= created_at + interval '90 days')
);
CREATE INDEX ix_zdjecie_do_usuniecia ON zdjecie_rozwiazania (usun_po) WHERE usunieto_at IS NULL;

-- Kopie w zbiorze treningowym (16.4, ADR-035): jedyne powiązanie kopii z kontem; bez niego odwołanie
-- zgody i usunięcie konta nie mogłyby usunąć kopii. Wiersz żyje dłużej niż Rozwiązanie (brak FK do niego).
CREATE TABLE kopia_treningowa (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id          uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE RESTRICT, -- USUN_KONTO usuwa kopie przed kontem
    pseudonim_ucznia  uuid NOT NULL DEFAULT gen_random_uuid(), -- losowy per kopia; jedyne powiązanie z kontem to ten wiersz
    rozwiazanie_id    uuid,                           -- informacyjnie; NULL po usunięciu Rozwiązania
    klucz_obiektu     text NOT NULL UNIQUE,           -- zbior-treningowy/{pseudonim}/{id}.jpg (+ .json z Transkrypcją)
    ocena_id          uuid,                           -- informacyjnie: Ocena aktualna zapisana w .json; nowsza Ocena → nadpisanie .json
    usun_po           timestamptz NOT NULL,           -- created_at + 24 miesiące; now() po odwołaniu zgody
    usunieto_at       timestamptz,
    created_at        timestamptz NOT NULL DEFAULT now(),
    CHECK (usun_po <= created_at + interval '24 months')
);
CREATE INDEX ix_kopia_do_usuniecia ON kopia_treningowa (usun_po) WHERE usunieto_at IS NULL;
CREATE INDEX ix_kopia_uczen ON kopia_treningowa (uczen_id) WHERE usunieto_at IS NULL;

CREATE TABLE transkrypcja (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rozwiazanie_id         uuid NOT NULL REFERENCES rozwiazanie(id) ON DELETE CASCADE,
    wersja                 smallint NOT NULL CHECK (wersja >= 1),
    zrodlo                 zrodlo_transkrypcji NOT NULL,
    przeslanie_nr          smallint NOT NULL CHECK (przeslanie_nr BETWEEN 1 AND 3), -- zdjęcia, z których pochodzi
    konfiguracja_modelu_id uuid,                       -- dla AGENT_ROZPOZNAJACY (FK niżej)
    zgodnosc_z_zadaniem    numeric(4,3),               -- sygnał agenta: czy kartka dotyczy Zadania
    zakres_poprawek        jsonb,                      -- ADR-036: {"zmienione":2,"wstawione":0,"kroki_agenta":9,"odsetek_znakow":0.08}
    zgodnosc_poprawek      jsonb,                      -- ADR-036: rezultat SPRAWDZ_POPRAWKI_TRANSKRYPCJI (schemat 10.3.3); NULL = brak rezultatu
    dane_osobowe_usuniete  smallint NOT NULL DEFAULT 0,-- liczba zastąpień tokenów przez Filtr danych osobowych (ADR-042); kroków się nie usuwa
    konflikt_filtra        boolean NOT NULL DEFAULT false,
                           -- ≥ 1 krok niezmieniony przez Ucznia z podejrzenie_danych_osobowych i treścią matematyczną
                           -- (tokeny imienia/nazwiska z konta nie są treścią matematyczną, C4-03) → T-3 (11.1 p. 7d, C3-02)
    created_at             timestamptz NOT NULL DEFAULT now(),
    UNIQUE (rozwiazanie_id, wersja),
    CHECK ((zrodlo = 'AGENT_ROZPOZNAJACY') = (konfiguracja_modelu_id IS NOT NULL)),
    CHECK ((zrodlo = 'POPRAWKA_UCZNIA') = (zakres_poprawek IS NOT NULL)),
    CHECK (zrodlo = 'POPRAWKA_UCZNIA' OR zgodnosc_poprawek IS NULL)
);
ALTER TABLE rozwiazanie ADD CONSTRAINT fk_rozwiazanie_transkrypcja
    FOREIGN KEY (zatwierdzona_transkrypcja_id) REFERENCES transkrypcja(id);
ALTER TABLE rozwiazanie ADD CONSTRAINT fk_rozwiazanie_najnowsza_transkrypcja
    FOREIGN KEY (najnowsza_transkrypcja_id) REFERENCES transkrypcja(id) DEFERRABLE INITIALLY DEFERRED;
ALTER TABLE szkic_transkrypcji ADD CONSTRAINT fk_szkic_transkrypcji_transkrypcja
    FOREIGN KEY (transkrypcja_id) REFERENCES transkrypcja(id) ON DELETE CASCADE;

CREATE TABLE krok_transkrypcji (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transkrypcja_id     uuid NOT NULL REFERENCES transkrypcja(id) ON DELETE CASCADE,
    nr                  smallint NOT NULL CHECK (nr BETWEEN 1 AND 40),
    tekst               text NOT NULL CHECK (char_length(tekst) <= 2000),   -- opis słowny
    latex               text NOT NULL CHECK (char_length(latex) <= 2000),   -- wzory
    pewnosc_odczytu     numeric(4,3) CHECK (pewnosc_odczytu BETWEEN 0 AND 1), -- NULL dla kroku zmienionego przez Ucznia
    niepewne_fragmenty  jsonb NOT NULL DEFAULT '[]',   -- schemat 10.3.2
    obszar              jsonb,                         -- {"zdjecie":1,"ramka":[x,y,w,h]} linii kroku (10.3.2); NULL dla wstawionego
    zmieniony_przez_ucznia boolean NOT NULL DEFAULT false,
    nr_kroku_agenta     smallint CHECK (nr_kroku_agenta BETWEEN 1 AND 40),
                        -- numer kroku w Transkrypcji agenta, który ten krok poprawia (ADR-036); NULL = krok wstawiony
    odleglosc_edycyjna  numeric(4,3) CHECK (odleglosc_edycyjna BETWEEN 0 AND 1),
                        -- znormalizowana odległość Levenshteina od kroku agenta; NULL dla kroku agenta i wstawionego
    podejrzenie_danych_osobowych boolean NOT NULL DEFAULT false,
                        -- obszar kroku pokrywa się w ≥ 50% z ramką danych osobowych (Filtr 10.3.1 p. 3a, C3-02)
    tylko_dane_osobowe  boolean NOT NULL DEFAULT false,
                        -- krok bez treści matematycznej w ramce danych osobowych: tekst = '[DANE_OSOBOWE]', latex = ''
    zastapienia_danych_osobowych jsonb NOT NULL DEFAULT '[]',
                        -- [{"od":12,"do":26,"rodzaj":"DOKLADNE"|"PRZYBLIZONE"}] — pozycje znacznika w tekst; bez oryginału
    UNIQUE (transkrypcja_id, nr),
    CHECK (NOT tylko_dane_osobowe OR podejrzenie_danych_osobowych),
    CHECK (zmieniony_przez_ucznia = (pewnosc_odczytu IS NULL)),
    CHECK (nr_kroku_agenta IS NOT NULL OR zmieniony_przez_ucznia)
);

CREATE TABLE odpowiedz (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id           uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    zadanie_id         uuid NOT NULL REFERENCES zadanie(id),
    zadanie_wersja_id  uuid NOT NULL REFERENCES zadanie_wersja(id),
    element_sciezki_id uuid REFERENCES element_sciezki(id) ON DELETE SET NULL,
    przypisanie_id     uuid REFERENCES przypisanie(id),          -- także odziedziczone z elementu R-PRZYPISANIE
    pozycja_testu_diagnostycznego_id uuid REFERENCES pozycja_testu_diagnostycznego(id) ON DELETE CASCADE,
    wybor              jsonb NOT NULL,                -- {"opcja":"B"} | {"stwierdzenia":{"1":true,"2":false}}
    czy_poprawna       boolean NOT NULL,
    punkty             smallint NOT NULL,
    punkty_max         smallint NOT NULL,
    rezultat_ujawniony_od timestamptz,                -- przypisanie_id → greatest(now(), termin); element bez Przypisania → now();
                                                      -- Test diagnostyczny → NULL do …/zakonczenie lub …/pominiecie (C2-16)
    created_at         timestamptz NOT NULL DEFAULT now(),
    CHECK (punkty BETWEEN 0 AND punkty_max),
    -- kontekst próby (C1-21): dozwolone kombinacje {E}, {P}, {E, P} (element R-PRZYPISANIE, trg_kontekst_proby), {T}
    CHECK (num_nonnulls(element_sciezki_id, przypisanie_id, pozycja_testu_diagnostycznego_id) BETWEEN 1 AND 2),
    CHECK (pozycja_testu_diagnostycznego_id IS NULL OR (element_sciezki_id IS NULL AND przypisanie_id IS NULL)),
    CHECK (pozycja_testu_diagnostycznego_id IS NOT NULL OR rezultat_ujawniony_od IS NOT NULL)
);
CREATE UNIQUE INDEX ux_odpowiedz_element ON odpowiedz (element_sciezki_id) WHERE element_sciezki_id IS NOT NULL;
CREATE UNIQUE INDEX ux_odpowiedz_test ON odpowiedz (pozycja_testu_diagnostycznego_id)
    WHERE pozycja_testu_diagnostycznego_id IS NOT NULL;
CREATE UNIQUE INDEX ux_odpowiedz_przypisanie ON odpowiedz (uczen_id, przypisanie_id, zadanie_id)
    WHERE przypisanie_id IS NOT NULL;

-- ===================== MODELE, KALIBRACJA =====================
CREATE TABLE konfiguracja_modelu (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rola             rola_modelu NOT NULL,
    dostawca         text NOT NULL,                   -- klucz adaptera, np. 'typesafe-jev'
    model            text NOT NULL,                   -- identyfikator modelu u dostawcy
    parametry        jsonb NOT NULL DEFAULT '{}',     -- schemat 13.1 (timeout_s, ceny, retencja_dostawcy_dni, …)
    wersja_promptu   text NOT NULL,                   -- np. 'ocena-v3' (plik w repozytorium)
    aktywna          boolean NOT NULL DEFAULT false,
    utworzona_przez  uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,
    created_at       timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_konfiguracja_aktywna ON konfiguracja_modelu (rola) WHERE aktywna;

-- Globalny ogranicznik szybkości (ADR-039): rejestr kubełków korzystających z klucza API modelu;
-- przydział wiersza i = min(0.8 × limit_klucza × waga_i / Σ waga_j, 0.8 × limit_klucza − Σ_{j≠i} przydzial_j)
-- po wierszach z odnowiono_at > now() − 30 s (jedna formuła w 13.2, 17.4, 22 i ADR-039; C3-04, C4-05);
-- pojemność: stała 1 dla api, pozostałe wiersze dzielą B_całk − n_api proporcjonalnie do wagi (13.2), Σ pojemnosc ≤ B_całk
CREATE UNLOGGED TABLE limit_szybkosci_modelu (
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id) ON DELETE CASCADE,
    wykonawca              text NOT NULL,             -- proces, np. 'worker#1', 'api#2'
    pula                   text NOT NULL DEFAULT '*'
                           CHECK (pula IN ('*','ROZPOZNAWANIE','SPRAWDZANIE')),
                                                      -- '*' = cały proces (Jev: pule O i W, api); dla roli ROZPOZNAWANIE_PISMA
                                                      -- osobny wiersz na pulę R i S tej samej repliki (C4-06 p. 8)
    waga                   numeric(3,2) NOT NULL CHECK (waga > 0 AND waga <= 1),
                                                      -- Jev: worker 1.00, api 0.10; rozpoznawanie: R 1.00, S 0.30 (parametr 22)
    pojemnosc              smallint NOT NULL CHECK (pojemnosc BETWEEN 0 AND 34),   -- 0: dołączanie przy pełnym wykorzystaniu i wiersz ponad limit (13.2, C5-03)
                                                      -- opublikowana pojemność kubełka; Jev, 2 workery + 2 api: worker 16, api 1
    przydzial              numeric(6,2) NOT NULL DEFAULT 0 CHECK (przydzial >= 0),
                                                      -- opublikowany przydział (wywołania/s; rozpoznawanie: /min), używany przez proces
    odnowiono_at           timestamptz NOT NULL DEFAULT now(),   -- odnawiane co 10 s (co 2 s w trybie dołączania)
    PRIMARY KEY (konfiguracja_modelu_id, wykonawca, pula)
);
ALTER TABLE transkrypcja ADD CONSTRAINT fk_transkrypcja_konfiguracja
    FOREIGN KEY (konfiguracja_modelu_id) REFERENCES konfiguracja_modelu(id);

-- Kalibrator pewności (Kalibracja, ADR-006/007): funkcja pewnosc_surowa → pewnosc_oceny
CREATE TABLE kalibrator_pewnosci (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id),
    rodzaj_zadania         rodzaj_zadania NOT NULL CHECK (rodzaj_zadania <> 'ZAMKNIETE'),
    metoda                 text NOT NULL CHECK (metoda IN ('TOZSAMOSCIOWY','IZOTONICZNY_CV5')),
    punkty                 jsonb NOT NULL,            -- schemat 11.2.2
    liczba_probek          integer NOT NULL,
    ece                    numeric(5,4),              -- Expected Calibration Error, out-of-fold, 10 koszyków
    okno_od                timestamptz,
    okno_do                timestamptz,
    zrodlo                 zrodlo_kalibratora NOT NULL,
    created_at             timestamptz NOT NULL DEFAULT now()
);

-- Próg pewności: wyłącznie wartość progu i jego uzasadnienie (C1-03)
CREATE TABLE prog_pewnosci (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id),
    rodzaj_zadania         rodzaj_zadania NOT NULL CHECK (rodzaj_zadania <> 'ZAMKNIETE'),
    kalibrator_pewnosci_id uuid NOT NULL REFERENCES kalibrator_pewnosci(id),
    wartosc                numeric(4,3) NOT NULL CHECK (wartosc BETWEEN 0.800 AND 1.000),
    gorna_granica_bledu_auto numeric(5,4),            -- Wilson 95% w chwili wyboru progu
    n_eff                  numeric(8,1),              -- efektywna liczność próby powyżej progu
    zrodlo                 zrodlo_progu NOT NULL,
    uzasadnienie           text NOT NULL,
    aktywny                boolean NOT NULL DEFAULT false,
    utworzony_przez        uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,
    created_at             timestamptz NOT NULL DEFAULT now(),
    CHECK (rodzaj_zadania <> 'OTWARTE_Z_DOWODEM' OR wartosc = 1.000)
);
CREATE UNIQUE INDEX ux_prog_aktywny ON prog_pewnosci (konfiguracja_modelu_id, rodzaj_zadania) WHERE aktywny;

-- Stan bezpieczników (ADR-046, C3-01): jeden wiersz na (konfiguracja decyzyjna, rodzaj, typ); zapis przez KALIBRUJ.
-- Zastępuje flagę LEMAT_POPRAWKI_ZAWSZE_DO_WERYFIKACJI z wersji 2.2.
CREATE TABLE stan_bezpiecznika (
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id),
    rodzaj_zadania         rodzaj_zadania NOT NULL CHECK (rodzaj_zadania = 'OTWARTE_OBLICZENIOWE'),
    typ                    typ_bezpiecznika NOT NULL,
    aktywny                boolean NOT NULL DEFAULT false,      -- POPRAWKI: każda Transkrypcja z Poprawkami → T-7;
                                                                -- TRAFNOSC (od 2.4, C4-02): Blokada obniżania progu po wyzwoleniu
    okno_od                timestamptz NOT NULL DEFAULT now(),  -- początek okna oceny; ustawiany przy dezaktywacji (C4-04)
    n_eff                  numeric(8,1),                        -- ostatnia ocena: efektywna liczność Kontroli w oknie
    blad_ipw               numeric(5,4),                        -- ostatnia ocena: błąd ważony IPW
    dolna_granica_bledu    numeric(5,4),                        -- Wilson 95%, LB
    gorna_granica_bledu    numeric(5,4),                        -- Wilson 95%, UB
    ostatnie_wyzwolenie_at timestamptz,
    wyjscie_z_blokady_at   timestamptz,                         -- TRAFNOSC: automatyczne wyjście (UB ≤ 2% przy n_eff ≥ 189)
    dezaktywowany_przez    uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,   -- nazwy od 2.4 (C4-04)
    dezaktywowano_at       timestamptz,
    uzasadnienie_dezaktywacji text CHECK (char_length(uzasadnienie_dezaktywacji) <= 1000),
    updated_at             timestamptz NOT NULL DEFAULT now(),
    version                integer NOT NULL DEFAULT 1,           -- 3.1 (C6-08): token współbieżności KALIBRUJ ↔ dezaktywacja (ETag)
    PRIMARY KEY (konfiguracja_modelu_id, rodzaj_zadania, typ),
    CHECK ((dezaktywowano_at IS NULL) = (uzasadnienie_dezaktywacji IS NULL))
    -- TRAFNOSC podnosi próg przez prog_pewnosci (zrodlo = BEZPIECZNIK); aktywny = bezpiecznik w stanie aktywnym,
    -- skutek dla TRAFNOSC: Blokada obniżania progu (11.5.3, C4-02), dla POPRAWKI: T-7 (3.1 p. 5, C5-04)
);

CREATE TABLE decyzja_modelu (
    id                     uuid NOT NULL DEFAULT gen_random_uuid(),
    created_at             timestamptz NOT NULL DEFAULT now(),
    kontekst               kontekst_decyzji NOT NULL,
    kontekst_id            uuid NOT NULL,             -- id Oceny w przygotowaniu / Rekomendacji / zadanie_wersja
    uczen_id               uuid,                      -- OCENA i REKOMENDACJA: NOT NULL aplikacyjnie; usuwanie RODO (16.4)
    rozwiazanie_id         uuid,                      -- dla kontekstu OCENA
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id),
    kod_pytania            text NOT NULL,             -- np. 'Q-OC-02.1.K3'
    typ_pytania            typ_pytania NOT NULL,
    opcje                  jsonb,                     -- dla CHOICE: lista opcji, SCORE: {"min":0,"max":2}
    wybor                  jsonb,                     -- wybrana wartość (moda); NULL przy błędzie
    rozklad                jsonb,                     -- {"opcja": p, ...}, suma = 1 ± 0.001
    czas_ms                integer NOT NULL,
    skrot_stanu            bytea NOT NULL,            -- SHA-256 stanu wejściowego
    stan                   text,                      -- pełny stan; zerowany po 180 dniach lub przy usunięciu konta
    blad                   text,
    PRIMARY KEY (id, created_at),
    CHECK (kontekst = 'WALIDACJA_ZADANIA' OR uczen_id IS NOT NULL OR stan IS NULL)
) PARTITION BY RANGE (created_at);
-- Partycje miesięczne tworzone przez pracę w tle UTRZYMAJ_PARTYCJE (3 miesiące naprzód).
CREATE INDEX ix_decyzja_kontekst ON decyzja_modelu (kontekst, kontekst_id);
CREATE INDEX ix_decyzja_uczen ON decyzja_modelu (uczen_id) WHERE uczen_id IS NOT NULL;

-- ===================== OCENY, WERYFIKACJE =====================
CREATE TABLE ocena (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rozwiazanie_id         uuid NOT NULL REFERENCES rozwiazanie(id) ON DELETE CASCADE,
    wersja                 smallint NOT NULL CHECK (wersja >= 1),
    status                 status_oceny NOT NULL,
    zrodlo                 zrodlo_oceny NOT NULL,
    transkrypcja_id        uuid NOT NULL REFERENCES transkrypcja(id),
    wariant                smallint NOT NULL,         -- przy N-5 (ZADEN): wariant o najwyższym p spośród W1…Wn
    punkty_surowe          smallint,                  -- Σ pk przed regułą FR-20 (11.1 p. 4); NULL dla Oceny Nauczyciela
    punkty                 smallint NOT NULL,
    punkty_max             smallint NOT NULL,
    pierwszy_bledny_krok   smallint,                  -- nr kroku Transkrypcji; NULL = brak błędnego kroku Ucznia
    brakujacy_krok         boolean NOT NULL DEFAULT false,
    typ_bledu              typ_bledu,
    krok_podejrzany_odczyt smallint,                  -- krok, w którym model podejrzewa błąd odczytu (ADR-022)
    wskazowka              text CHECK (char_length(wskazowka) <= 400),   -- jednokrotny zapis (Z-5)
    zrodlo_wskazowki       zrodlo_wskazowki,
    uzasadnienie           text NOT NULL,             -- generowane z decyzji (szablon 11.7); widoczne w OcenaWyjscie
    pewnosc_oceny          numeric(5,4) CHECK (pewnosc_oceny BETWEEN 0 AND 1),
    pewnosc_surowa         numeric(5,4),
    prog_pewnosci_id       uuid REFERENCES prog_pewnosci(id),
    kalibrator_pewnosci_id uuid REFERENCES kalibrator_pewnosci(id),
    konfiguracja_modelu_id uuid REFERENCES konfiguracja_modelu(id),
    mozliwy_blad_odczytu   boolean NOT NULL DEFAULT false,
    nauczyciel_id          uuid REFERENCES uzytkownik(id) ON DELETE SET NULL,
    created_at             timestamptz NOT NULL DEFAULT now(),
    UNIQUE (rozwiazanie_id, wersja),
    CHECK (punkty BETWEEN 0 AND punkty_max),
    CHECK ((zrodlo = 'AUTOMATYCZNA') = (pewnosc_oceny IS NOT NULL AND prog_pewnosci_id IS NOT NULL
                                         AND kalibrator_pewnosci_id IS NOT NULL
                                         AND konfiguracja_modelu_id IS NOT NULL AND punkty_surowe IS NOT NULL)),
    CHECK (zrodlo = 'NAUCZYCIEL' OR nauczyciel_id IS NULL),  -- Nauczyciel wymagany aplikacyjnie przy zapisie
    CHECK (zrodlo = 'AUTOMATYCZNA' OR status <> 'WSTEPNA'),
    CHECK (zrodlo = 'AUTOMATYCZNA' OR NOT mozliwy_blad_odczytu),
    CHECK ((typ_bledu IS NULL) = (pierwszy_bledny_krok IS NULL AND NOT brakujacy_krok)),
    CHECK (krok_podejrzany_odczyt IS NULL OR mozliwy_blad_odczytu),
    CHECK ((wskazowka IS NULL) = (zrodlo_wskazowki IS NULL)),
    CHECK (typ_bledu IS NOT NULL OR wskazowka IS NULL)
);
CREATE UNIQUE INDEX ux_ocena_aktualna ON ocena (rozwiazanie_id) WHERE status <> 'ZASTAPIONA';
-- Trigger trg_ocena_niezmienna (BEFORE UPDATE): dozwolone wyłącznie
--   (a) status WSTEPNA|OSTATECZNA → ZASTAPIONA,
--   (b) wskazowka i zrodlo_wskazowki NULL → wartość (jednokrotnie);
-- każda inna zmiana kolumn → wyjątek 'OCENA_NIEZMIENNA'.

CREATE TABLE ocena_kryterium (
    ocena_id            uuid NOT NULL REFERENCES ocena(id) ON DELETE CASCADE,
    kryterium_oceniania_id uuid NOT NULL REFERENCES kryterium_oceniania(id),
    punkty              smallint NOT NULL CHECK (punkty >= 0),
    prawdopodobienstwo  numeric(5,4),                 -- NULL dla Oceny Nauczyciela
    PRIMARY KEY (ocena_id, kryterium_oceniania_id)
);

CREATE TABLE weryfikacja (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rozwiazanie_id          uuid NOT NULL REFERENCES rozwiazanie(id) ON DELETE CASCADE,
    ocena_weryfikowana_id   uuid REFERENCES ocena(id),  -- NULL wyłącznie przy BLAD_PRZETWARZANIA przed PONOW_OCENE
    powody                  powod_weryfikacji[] NOT NULL CHECK (cardinality(powody) >= 1),
    powody_poczatkowe       powod_weryfikacji[] NOT NULL CHECK (cardinality(powody_poczatkowe) >= 1),
                            -- powody w chwili dołączenia Oceny weryfikowanej (INSERT albo PONOW_OCENE); źródło Próbki (11.5.2)
    priorytet               smallint NOT NULL CHECK (priorytet BETWEEN 1 AND 3),
    typ_kolejki             typ_kolejki_weryfikacji NOT NULL,
    klasa_id                uuid REFERENCES klasa(id) ON DELETE SET NULL,
    nauczyciel_docelowy_id  uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL,
    status                  status_weryfikacji NOT NULL DEFAULT 'OCZEKUJE',
    termin_at               timestamptz NOT NULL,     -- SLA: dodaj_dni_robocze(created_at, 3); po eskalacji: dodaj_dni_robocze(eskalowano_at, 3)
    eskalowano_at           timestamptz,
    powod_eskalacji         powod_eskalacji,          -- KARENCJA_KONTA_NAUCZYCIELA jest cofana przy przywróceniu konta (16.4, C3-08)
    podjeta_przez           uuid REFERENCES nauczyciel(uzytkownik_id) ON DELETE SET NULL,
    podjeta_at              timestamptz,              -- początek bieżącej blokady (limit 2 h, 11.4.2)
    zablokowana_do          timestamptz,
    ocena_nauczyciela_id    uuid REFERENCES ocena(id),
    zgloszenie              jsonb,                    -- schemat 7.5.4; warunki 11.6
    ponowna_ocena_at        timestamptz,              -- PONOW_OCENE dołączyła Ocenę wstępną (9.13)
    powod_anulowania        powod_anulowania,
    opinia_pomocnicza       jsonb,                    -- schemat 10.4.5
    created_at              timestamptz NOT NULL DEFAULT now(),
    zakonczono_at           timestamptz,
    version                 integer NOT NULL DEFAULT 1,  -- wersja TREŚCI (ADR-037), ustawiana wyłącznie przez trg_weryfikacja_wersja
    CHECK ((status = 'W_TRAKCIE') = (podjeta_przez IS NOT NULL AND zablokowana_do IS NOT NULL AND podjeta_at IS NOT NULL)),
    CHECK ((status = 'ZAKONCZONA') = (ocena_nauczyciela_id IS NOT NULL)),
    CHECK ((status = 'ANULOWANA') = (powod_anulowania IS NOT NULL)),
    CHECK (ocena_weryfikowana_id IS NOT NULL OR 'BLAD_PRZETWARZANIA' = ANY (powody)),
    CHECK (('ZGLOSZENIE_BLEDU_ODCZYTU' = ANY (powody)) = (zgloszenie IS NOT NULL)),
    CHECK ((status IN ('ZAKONCZONA','ANULOWANA')) = (zakonczono_at IS NOT NULL)),
    CHECK ((eskalowano_at IS NULL) = (powod_eskalacji IS NULL))
);
CREATE UNIQUE INDEX ux_weryfikacja_otwarta ON weryfikacja (rozwiazanie_id) WHERE status IN ('OCZEKUJE','W_TRAKCIE');
CREATE INDEX ix_weryfikacja_kolejka ON weryfikacja (typ_kolejki, nauczyciel_docelowy_id, status, priorytet, termin_at);
CREATE INDEX ix_weryfikacja_blokady ON weryfikacja (zablokowana_do) WHERE status = 'W_TRAKCIE';
-- Trigger trg_weryfikacja_wersja (BEFORE UPDATE), ADR-037:
--   NEW.version := OLD.version + 1, gdy zmienia się którakolwiek kolumna TREŚCI: powody, zgloszenie,
--   ocena_weryfikowana_id, ponowna_ocena_at, ocena_nauczyciela_id, powod_anulowania albo status przechodzi
--   do ZAKONCZONA/ANULOWANA; w przeciwnym razie NEW.version := OLD.version. Kolumny BLOKADY (status
--   OCZEKUJE↔W_TRAKCIE, podjeta_przez, podjeta_at, zablokowana_do), TRASOWANIA (typ_kolejki, eskalowano_at,
--   termin_at, priorytet, klasa_id, nauczyciel_docelowy_id) i opinia_pomocnicza nie zmieniają version.
-- Trigger trg_powody_poczatkowe (BEFORE UPDATE): powody_poczatkowe może zmienić wyłącznie przejście
--   ocena_weryfikowana_id NULL → wartość (PONOW_OCENE, 9.13); każda inna zmiana → wyjątek 'POWODY_NIEZMIENNE'.

-- Autozapis rozstrzygnięcia w toku (C1-06); widoczny tylko dla autora
CREATE TABLE szkic_rozstrzygniecia (
    weryfikacja_id uuid NOT NULL REFERENCES weryfikacja(id) ON DELETE CASCADE,
    nauczyciel_id  uuid NOT NULL REFERENCES nauczyciel(uzytkownik_id) ON DELETE CASCADE,
    dane           jsonb NOT NULL,                    -- kształt RozstrzygniecieWejscie (12.4), pola opcjonalne
    updated_at     timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (weryfikacja_id, nauczyciel_id)
);

CREATE TABLE probka_kalibracyjna (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    rozwiazanie_id         uuid REFERENCES rozwiazanie(id) ON DELETE SET NULL,
    weryfikacja_id         uuid REFERENCES weryfikacja(id) ON DELETE SET NULL,
    pseudonim_ucznia       uuid NOT NULL,             -- losowy, niepowiązany z kontem po usunięciu
    ocena_automatyczna_id  uuid REFERENCES ocena(id) ON DELETE SET NULL,
    ocena_nauczyciela_id   uuid REFERENCES ocena(id) ON DELETE SET NULL,
    konfiguracja_modelu_id uuid NOT NULL REFERENCES konfiguracja_modelu(id),
    rodzaj_zadania         rodzaj_zadania NOT NULL,
    zrodlo_probki          zrodlo_probki NOT NULL,    -- reguła przypisania 11.5.2
    prawdopodobienstwo_doboru numeric(5,4) NOT NULL CHECK (prawdopodobienstwo_doboru > 0 AND prawdopodobienstwo_doboru <= 1),
                                                      -- 1 dla NISKA_PEWNOSC; odsetek Kontroli losowej w chwili Oceny
    pewnosc_surowa         numeric(5,4) NOT NULL,
    pewnosc_oceny          numeric(5,4) NOT NULL,
    punkty_automatyczne    smallint NOT NULL,
    punkty_nauczyciela     smallint NOT NULL,
    zgodny_pierwszy_bledny_krok boolean NOT NULL,     -- oba NULL albo równe
    zgodny_brakujacy_krok  boolean NOT NULL,
    zgodny_typ_bledu       boolean NOT NULL,          -- metryka pomocnicza, poza definicją zgodności
    zgodna                 boolean GENERATED ALWAYS AS
                           (punkty_automatyczne = punkty_nauczyciela AND zgodny_pierwszy_bledny_krok
                            AND zgodny_brakujacy_krok) STORED,       -- Zgodność Oceny (ADR-027)
    uwzgledniana_w_progu   boolean GENERATED ALWAYS AS
                           (zrodlo_probki IN ('WERYFIKACJA_NISKA_PEWNOSC','KONTROLA_LOSOWA')) STORED,
    blad_odczytu_wplynal   boolean NOT NULL DEFAULT false, -- Nauczyciel: błąd odczytu zmienił punkty (metryka T-3)
    kryteria               jsonb NOT NULL,            -- [{"nr_kryterium":1,"auto":1,"nauczyciel":1}, ...]
    created_at             timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_probka_ocena_auto ON probka_kalibracyjna (ocena_automatyczna_id)
    WHERE ocena_automatyczna_id IS NOT NULL;              -- co najwyżej jedna próbka na Ocenę automatyczną
CREATE INDEX ix_probka_kalibracja ON probka_kalibracyjna (konfiguracja_modelu_id, rodzaj_zadania, created_at);

CREATE TABLE wynik_zadania (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    uczen_id           uuid NOT NULL REFERENCES uczen(uzytkownik_id) ON DELETE CASCADE,
    zadanie_id         uuid NOT NULL REFERENCES zadanie(id),
    zadanie_wersja_id  uuid NOT NULL REFERENCES zadanie_wersja(id),
    zrodlo             zrodlo_wyniku NOT NULL,
    odpowiedz_id       uuid REFERENCES odpowiedz(id) ON DELETE CASCADE,
    ocena_id           uuid REFERENCES ocena(id) ON DELETE CASCADE,
    test_diagnostyczny_id uuid REFERENCES test_diagnostyczny(id) ON DELETE CASCADE, -- Wynik z Testu diagnostycznego
    ulamek             numeric(4,3) NOT NULL CHECK (ulamek BETWEEN 0 AND 1), -- punkty / punkty_max
    waga               numeric(3,2) NOT NULL CHECK (waga IN (0.25, 0.50, 1.00)),  -- reguła 10.1.3
    liczy_do_trudnosci boolean NOT NULL,              -- pierwsza próba Ucznia przy tym Zadaniu w 60 dni i waga 1.00
    uniewazniony_at    timestamptz,
    powod_uniewaznienia powod_uniewaznienia_wyniku,   -- ZGLOSZENIE_BLEDU_ODCZYTU = zawieszenie na czas zgłoszenia (11.6)
    zastosowany_w_profilu_at       timestamptz,       -- ustawiane przez AKTUALIZUJ_PROFIL (idempotencja)
    uniewaznienie_przetworzone_at  timestamptz,       -- ustawiane po odtworzeniu Profilu ucznia
    created_at         timestamptz NOT NULL DEFAULT now(),  -- dla Testu diagnostycznego = zakonczono_at testu
    CHECK ((zrodlo = 'ODPOWIEDZ') = (odpowiedz_id IS NOT NULL)),
    CHECK ((zrodlo = 'OCENA') = (ocena_id IS NOT NULL)),
    CHECK (test_diagnostyczny_id IS NULL OR (zrodlo = 'ODPOWIEDZ' AND waga = 1.00)),
    CHECK ((uniewazniony_at IS NULL) = (powod_uniewaznienia IS NULL)),
    CHECK (powod_uniewaznienia IS DISTINCT FROM 'ZGLOSZENIE_BLEDU_ODCZYTU' OR zrodlo = 'OCENA')
);
CREATE INDEX ix_wynik_uczen ON wynik_zadania (uczen_id, created_at) WHERE uniewazniony_at IS NULL;
CREATE INDEX ix_wynik_uczen_zadanie ON wynik_zadania (uczen_id, zadanie_id, created_at) WHERE uniewazniony_at IS NULL;
CREATE UNIQUE INDEX ux_wynik_ocena ON wynik_zadania (ocena_id) WHERE ocena_id IS NOT NULL;
CREATE UNIQUE INDEX ux_wynik_odpowiedz ON wynik_zadania (odpowiedz_id) WHERE odpowiedz_id IS NOT NULL;
CREATE INDEX ix_wynik_do_profilu ON wynik_zadania (uczen_id)
    WHERE zastosowany_w_profilu_at IS NULL
       OR (uniewazniony_at IS NOT NULL AND uniewaznienie_przetworzone_at IS NULL);

-- ===================== ORKIESTRATOR, INFRASTRUKTURA =====================
CREATE TABLE praca_w_tle (
    id                bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    typ               text NOT NULL,                  -- katalog 13.3
    pula              text NOT NULL CHECK (pula IN ('ROZPOZNAWANIE','SPRAWDZANIE','OCENIANIE','POMOCNICZA','WOLNA')),
    klucz_deduplikacji text,
    ladunek           jsonb NOT NULL,
    status            status_pracy NOT NULL DEFAULT 'OCZEKUJE',
    priorytet         smallint NOT NULL DEFAULT 5,    -- 1 = najwyższy (w obrębie puli)
    proby             smallint NOT NULL DEFAULT 0,
    maks_prob         smallint NOT NULL,              -- 3.2 (C7-06 p. 7): dawniej max_prob (ADR-049)
    dostepna_od       timestamptz NOT NULL DEFAULT now(),
    dzierzawa_do      timestamptz,             -- dzierżawa pracy (13.4); do wersji 2.4: zablokowana_do (C5-04)
    wykonawca         text,
    ostatni_blad      text,
    created_at        timestamptz NOT NULL DEFAULT now(),
    updated_at        timestamptz NOT NULL DEFAULT now()
);
-- Deduplikacja (C2-04, C3-05): dla AKTUALIZUJ_PROFIL i SPRAWDZ_POPRAWKI_TRANSKRYPCJI obejmuje tylko prace OCZEKUJE —
-- zlecenie w trakcie wykonania poprzedniej tworzy nową pracę, więc żadne wybudzenie ani nowa wersja Poprawek nie ginie;
-- dla pozostałych typów OCZEKUJE i W_TOKU.
CREATE UNIQUE INDEX ux_praca_dedup ON praca_w_tle (typ, klucz_deduplikacji)
    WHERE status IN ('OCZEKUJE','W_TOKU') AND klucz_deduplikacji IS NOT NULL
      AND typ NOT IN ('AKTUALIZUJ_PROFIL','SPRAWDZ_POPRAWKI_TRANSKRYPCJI');
CREATE UNIQUE INDEX ux_praca_dedup_oczekujace ON praca_w_tle (typ, klucz_deduplikacji)
    WHERE status = 'OCZEKUJE' AND klucz_deduplikacji IS NOT NULL
      AND typ IN ('AKTUALIZUJ_PROFIL','SPRAWDZ_POPRAWKI_TRANSKRYPCJI');
CREATE INDEX ix_praca_pobieranie ON praca_w_tle (pula, priorytet, dostepna_od) WHERE status = 'OCZEKUJE';
CREATE INDEX ix_praca_dzierzawa ON praca_w_tle (dzierzawa_do) WHERE status = 'W_TOKU';
-- NOTIFY praca_w_tle: wyłącznie trigger trg_praca_w_tle_powiadom (część „Triggery i funkcje”, ADR-056)

-- 3.2 (C7-13): trwały znacznik ostatniego zleconego wystąpienia wpisu harmonogramu (13.5); nadrabianie ≤ 24 h
CREATE TABLE przebieg_harmonogramu (
    nazwa                 text PRIMARY KEY,               -- nazwa wpisu tabeli 13.5 (= typ pracy, np. 'KALIBRUJ')
    ostatnie_wystapienie  timestamptz NOT NULL,           -- wystąpienie cron (czas planowy, nie czas wykonania)
    zlecono_at            timestamptz NOT NULL DEFAULT now(),
    instancja             text NOT NULL                   -- identyfikator procesu scheduler, który zlecił
);

CREATE TABLE klucz_idempotencji (
    uzytkownik_id     uuid NOT NULL REFERENCES uzytkownik(id) ON DELETE CASCADE,
    klucz             uuid NOT NULL,                  -- nagłówek Idempotency-Key
    skrot_zapytania   bytea NOT NULL,                 -- SHA-256(metoda + ścieżka + ciało)
    rezerwacja        uuid,                           -- 3.3 (C8-03, ADR-068): identyfikator wykonania, które trzyma klucz
    w_toku_do         timestamptz,                    -- 3.3: koniec rezerwacji (now() + 60 s); po nim ponowienie przejmuje klucz
    kod_statusu_http  smallint CHECK (kod_statusu_http BETWEEN 200 AND 299),   -- 3.3: zapisywane wyłącznie 2xx, w transakcji skutku
    cialo_http        jsonb,                          -- zapisane ciało odpowiedzi HTTP
    wygasa_at         timestamptz NOT NULL,           -- created_at + 24 h
    created_at        timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (uzytkownik_id, klucz),
    CONSTRAINT ck_klucz_idempotencji_stan CHECK (
        (kod_statusu_http IS NULL) = (w_toku_do IS NOT NULL AND rezerwacja IS NOT NULL))
);

CREATE TABLE powiadomienie (
    id            uuid PRIMARY KEY,                   -- 3.1 (ADR-057 p. 5): UUIDv7 z aplikacji (Guid.CreateVersion7), id: zdarzenia SSE
    uzytkownik_id uuid NOT NULL REFERENCES uzytkownik(id) ON DELETE CASCADE,
    typ           text NOT NULL CHECK (typ IN ('OCENA_WYSTAWIONA','OCENA_SKORYGOWANA','NOWE_PRZYPISANIE',
                                              'NOWA_WERYFIKACJA','ZADANIE_DO_PRZEGLADU','WKLADKA_DO_PRZEGLADU',
                                              'TRANSKRYPCJA_GOTOWA','BLAD_ROZPOZNAWANIA','WERYFIKACJA_OPOZNIONA',
                                              'ALERT_KALIBRACJI','ALERT_WERYFIKACJE','ZGLOSZENIE_BLEDU_ZADANIA',
                                              'KLASA_DO_PRZEKAZANIA','ALERT_PULA_DIAGNOSTYCZNA','ALERT_RODO')),
    dane          jsonb NOT NULL,
    przeczytane_at timestamptz,
    created_at    timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_powiadomienie_nieprzeczytane ON powiadomienie (uzytkownik_id, created_at DESC) WHERE przeczytane_at IS NULL;

CREATE TABLE dziennik_audytu (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    uzytkownik_id uuid,                               -- bez FK: wpis przeżywa usunięcie konta; NULL = system
    rola          rola_uzytkownika,                   -- NULL = działanie systemowe
    akcja         text NOT NULL,                      -- np. 'WERYFIKACJA_ROZSTRZYGNIETA', 'POZIOM_TRUDNOSCI_SKALIBROWANY'
    zasob         text NOT NULL,
    zasob_id      text NOT NULL,
    dane          jsonb NOT NULL DEFAULT '{}',
    ip            inet,
    created_at    timestamptz NOT NULL DEFAULT now()
);
```

## (B) Triggery, funkcje i uprawnienia

Pliki migracji SQL (kolejność wykonywania przez `migrationBuilder.Sql`):
`0001_funkcje.sql` (funkcje czyste), `0002_triggery_niezmiennikow.sql`,
`0003_powiadomienia.sql`, `0004_partycje.sql`, `0005_uprawnienia.sql`, `0006_prace_w_tle.sql` (3.2, C7-03).

```sql
-- ===================== 0001_funkcje.sql =====================
-- Kalendarz dni roboczych (A-15, ADR-043). Przykłady: piątek 18:00 + 3 → środa 18:00; sobota 10:00 + 3 → czwartek 08:00.
CREATE FUNCTION dodaj_dni_robocze(od timestamptz, n integer) RETURNS timestamptz
LANGUAGE plpgsql STABLE AS $$
DECLARE
    lokalny timestamp := od AT TIME ZONE 'Europe/Warsaw';
    dzien   date      := lokalny::date;
    godzina time      := lokalny::time;
    dodane  integer   := 0;
BEGIN
    IF n < 0 THEN RAISE EXCEPTION 'DNI_ROBOCZE_UJEMNE'; END IF;
    IF extract(isodow FROM dzien) IN (6, 7) OR EXISTS (SELECT 1 FROM dzien_wolny WHERE data = dzien) THEN
        LOOP
            dzien := dzien + 1;
            EXIT WHEN extract(isodow FROM dzien) NOT IN (6, 7)
                  AND NOT EXISTS (SELECT 1 FROM dzien_wolny WHERE data = dzien);
        END LOOP;
        godzina := time '08:00';
    END IF;
    WHILE dodane < n LOOP
        dzien := dzien + 1;
        IF extract(isodow FROM dzien) NOT IN (6, 7) AND NOT EXISTS (SELECT 1 FROM dzien_wolny WHERE data = dzien) THEN
            dodane := dodane + 1;
        END IF;
    END LOOP;
    RETURN (dzien + godzina) AT TIME ZONE 'Europe/Warsaw';
END $$;

-- Poziom trudności z parametru b modelu Elo z histerezą 0,1 (10.1.3, ADR-029)
CREATE FUNCTION poziom_bez_histerezy(b numeric) RETURNS smallint LANGUAGE sql IMMUTABLE AS $$
    SELECT CASE WHEN b < -1.2 THEN 1 WHEN b < -0.4 THEN 2 WHEN b < 0.4 THEN 3 WHEN b < 1.2 THEN 4 ELSE 5 END::smallint
$$;
CREATE FUNCTION poziom_z_b(b numeric, obecny smallint) RETURNS smallint LANGUAGE sql IMMUTABLE AS $$
    SELECT CASE
        WHEN poziom_bez_histerezy(b - 0.1) > obecny THEN poziom_bez_histerezy(b - 0.1)   -- w górę: granica przekroczona o ≥ 0,1
        WHEN poziom_bez_histerezy(b + 0.1) < obecny THEN poziom_bez_histerezy(b + 0.1)   -- w dół: granica przekroczona o ≥ 0,1
        ELSE obecny END
$$;

-- ===================== 0002_triggery_niezmiennikow.sql =====================
-- Suma kryteriów per wariant = punkty_max wersji, ≤ 1 kryterium wyniku końcowego na wariant, ≤ 24 kryteria (W-1, 13.2)
CREATE FUNCTION f_suma_kryteriow() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    wersja uuid := COALESCE(NEW.zadanie_wersja_id, OLD.zadanie_wersja_id);
    pmax   smallint;
BEGIN
    SELECT punkty_max INTO pmax FROM zadanie_wersja WHERE id = wersja;
    IF EXISTS (SELECT 1 FROM kryterium_oceniania WHERE zadanie_wersja_id = wersja
               GROUP BY wariant
               HAVING sum(punkty_max) <> pmax OR count(*) FILTER (WHERE czy_wynik_koncowy) > 1) THEN
        RAISE EXCEPTION 'KRYTERIA_NIESPOJNE';
    END IF;
    IF (SELECT count(*) FROM kryterium_oceniania WHERE zadanie_wersja_id = wersja) > 24 THEN
        RAISE EXCEPTION 'ZA_DUZO_KRYTERIOW';
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_suma_kryteriow AFTER INSERT OR UPDATE OR DELETE ON kryterium_oceniania
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION f_suma_kryteriow();

-- Umiejętność główna Zadania należy do jego Działu
CREATE FUNCTION f_umiejetnosc_glowna_w_dziale() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.glowna AND NOT EXISTS (
        SELECT 1 FROM zadanie z JOIN umiejetnosc u ON u.id = NEW.umiejetnosc_id
        WHERE z.id = NEW.zadanie_id AND u.dzial_id = z.dzial_id) THEN
        RAISE EXCEPTION 'UMIEJETNOSC_GLOWNA_POZA_DZIALEM';
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_umiejetnosc_glowna_w_dziale AFTER INSERT OR UPDATE ON zadanie_umiejetnosc
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION f_umiejetnosc_glowna_w_dziale();

-- Kontekst próby (ADR-032, C1-21): dziedziczenie przypisanie_id z elementu R-PRZYPISANIE
CREATE FUNCTION f_kontekst_proby() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    przypisanie_elementu uuid;
BEGIN
    IF NEW.element_sciezki_id IS NOT NULL THEN
        SELECT przypisanie_id INTO przypisanie_elementu FROM element_sciezki WHERE id = NEW.element_sciezki_id;
        IF przypisanie_elementu IS NOT NULL AND NEW.przypisanie_id IS NULL THEN
            NEW.przypisanie_id := przypisanie_elementu;
        ELSIF NEW.przypisanie_id IS NOT NULL AND NEW.przypisanie_id IS DISTINCT FROM przypisanie_elementu THEN
            RAISE EXCEPTION 'KONTEKST_PROBY_NIESPOJNY';
        END IF;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_kontekst_proby BEFORE INSERT ON rozwiazanie FOR EACH ROW EXECUTE FUNCTION f_kontekst_proby();
CREATE TRIGGER trg_kontekst_proby BEFORE INSERT ON odpowiedz   FOR EACH ROW EXECUTE FUNCTION f_kontekst_proby();

-- Ocena niezmienna poza WSTEPNA|OSTATECZNA → ZASTAPIONA i jednokrotnym zapisem Wskazówki (Z-5)
CREATE FUNCTION f_ocena_niezmienna() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF (to_jsonb(NEW) - 'status' - 'wskazowka' - 'zrodlo_wskazowki')
       IS DISTINCT FROM (to_jsonb(OLD) - 'status' - 'wskazowka' - 'zrodlo_wskazowki') THEN
        RAISE EXCEPTION 'OCENA_NIEZMIENNA';
    END IF;
    IF NEW.status IS DISTINCT FROM OLD.status
       AND NOT (OLD.status IN ('WSTEPNA', 'OSTATECZNA') AND NEW.status = 'ZASTAPIONA') THEN
        RAISE EXCEPTION 'OCENA_NIEZMIENNA';
    END IF;
    IF (NEW.wskazowka IS DISTINCT FROM OLD.wskazowka OR NEW.zrodlo_wskazowki IS DISTINCT FROM OLD.zrodlo_wskazowki)
       AND OLD.wskazowka IS NOT NULL THEN
        RAISE EXCEPTION 'OCENA_NIEZMIENNA';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_ocena_niezmienna BEFORE UPDATE ON ocena FOR EACH ROW EXECUTE FUNCTION f_ocena_niezmienna();

-- weryfikacja.version = wersja TREŚCI (ADR-037)
CREATE FUNCTION f_weryfikacja_wersja() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.powody IS DISTINCT FROM OLD.powody
       OR NEW.zgloszenie IS DISTINCT FROM OLD.zgloszenie
       OR NEW.ocena_weryfikowana_id IS DISTINCT FROM OLD.ocena_weryfikowana_id
       OR NEW.ponowna_ocena_at IS DISTINCT FROM OLD.ponowna_ocena_at
       OR NEW.ocena_nauczyciela_id IS DISTINCT FROM OLD.ocena_nauczyciela_id
       OR NEW.powod_anulowania IS DISTINCT FROM OLD.powod_anulowania
       OR (NEW.status IN ('ZAKONCZONA', 'ANULOWANA') AND NEW.status IS DISTINCT FROM OLD.status) THEN
        NEW.version := OLD.version + 1;
    ELSE
        NEW.version := OLD.version;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_weryfikacja_wersja BEFORE UPDATE ON weryfikacja FOR EACH ROW EXECUTE FUNCTION f_weryfikacja_wersja();

CREATE FUNCTION f_powody_poczatkowe() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.powody_poczatkowe IS DISTINCT FROM OLD.powody_poczatkowe
       AND NOT (OLD.ocena_weryfikowana_id IS NULL AND NEW.ocena_weryfikowana_id IS NOT NULL) THEN
        RAISE EXCEPTION 'POWODY_NIEZMIENNE';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_powody_poczatkowe BEFORE UPDATE ON weryfikacja FOR EACH ROW EXECUTE FUNCTION f_powody_poczatkowe();

-- ===================== 0003_powiadomienia.sql (ADR-056 p. 3) =====================
-- pg_notify w transakcji jest dostarczane wyłącznie po COMMIT, nigdy po ROLLBACK;
-- identyczne (kanał, ładunek) z jednej transakcji PostgreSQL scala w jedno powiadomienie.
CREATE FUNCTION f_praca_w_tle_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('praca_w_tle', NEW.pula);
    RETURN NULL;
END $$;
CREATE TRIGGER trg_praca_w_tle_powiadom AFTER INSERT OR UPDATE OF status, dostepna_od ON praca_w_tle
    FOR EACH ROW WHEN (NEW.status = 'OCZEKUJE' AND NEW.dostepna_od <= now())
    EXECUTE FUNCTION f_praca_w_tle_powiadom();

CREATE FUNCTION f_zdarzenie_rozwiazania(rozwiazanie_id uuid) RETURNS void LANGUAGE sql AS $$
    SELECT pg_notify('lemat_zdarzenia', json_build_object('typ', 'ROZWIAZANIE', 'id', rozwiazanie_id)::text)
$$;
CREATE FUNCTION f_rozwiazanie_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN PERFORM f_zdarzenie_rozwiazania(NEW.id); RETURN NULL; END $$;
CREATE TRIGGER trg_rozwiazanie_powiadom AFTER UPDATE ON rozwiazanie
    FOR EACH ROW WHEN (OLD.version IS DISTINCT FROM NEW.version OR OLD.status IS DISTINCT FROM NEW.status)
    EXECUTE FUNCTION f_rozwiazanie_powiadom();

CREATE FUNCTION f_ocena_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN PERFORM f_zdarzenie_rozwiazania(NEW.rozwiazanie_id); RETURN NULL; END $$;
CREATE TRIGGER trg_ocena_powiadom AFTER INSERT OR UPDATE OF status, wskazowka ON ocena
    FOR EACH ROW EXECUTE FUNCTION f_ocena_powiadom();

CREATE FUNCTION f_weryfikacja_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN PERFORM f_zdarzenie_rozwiazania(NEW.rozwiazanie_id); RETURN NULL; END $$;
CREATE TRIGGER trg_weryfikacja_powiadom AFTER INSERT OR UPDATE OF status, termin_at, typ_kolejki ON weryfikacja
    FOR EACH ROW EXECUTE FUNCTION f_weryfikacja_powiadom();

CREATE FUNCTION f_powiadomienie_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('lemat_zdarzenia',
        json_build_object('typ', 'POWIADOMIENIE', 'id', NEW.id, 'uzytkownik_id', NEW.uzytkownik_id)::text);
    RETURN NULL;
END $$;
CREATE TRIGGER trg_powiadomienie_powiadom AFTER INSERT ON powiadomienie
    FOR EACH ROW EXECUTE FUNCTION f_powiadomienie_powiadom();

CREATE FUNCTION f_import_arkusza_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('lemat_zdarzenia', json_build_object('typ', 'IMPORT_ARKUSZA', 'id', NEW.id)::text);
    RETURN NULL;
END $$;
CREATE TRIGGER trg_import_arkusza_powiadom AFTER UPDATE OF status, raport ON import_arkusza
    FOR EACH ROW EXECUTE FUNCTION f_import_arkusza_powiadom();

CREATE FUNCTION f_zlecenie_generowania_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('lemat_zdarzenia', json_build_object('typ', 'ZLECENIE_GENEROWANIA', 'id', NEW.id)::text);
    RETURN NULL;
END $$;
CREATE TRIGGER trg_zlecenie_generowania_powiadom AFTER UPDATE OF status, liczba_cykli, zadanie_id ON zlecenie_generowania
    FOR EACH ROW EXECUTE FUNCTION f_zlecenie_generowania_powiadom();

CREATE FUNCTION f_konfiguracja_modelu_powiadom() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('konfiguracja_modelu', NEW.rola::text);
    RETURN NULL;
END $$;
CREATE TRIGGER trg_konfiguracja_modelu_powiadom AFTER INSERT OR UPDATE OF aktywna ON konfiguracja_modelu
    FOR EACH ROW EXECUTE FUNCTION f_konfiguracja_modelu_powiadom();

-- ===================== 0004_partycje.sql =====================
CREATE FUNCTION utworz_partycje_decyzji(miesiac date) RETURNS void LANGUAGE plpgsql AS $$
DECLARE
    od    date := date_trunc('month', miesiac)::date;
    nazwa text := 'decyzja_modelu_' || to_char(od, 'YYYY_MM');
BEGIN
    EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF decyzja_modelu FOR VALUES FROM (%L) TO (%L)',
                   nazwa, od, (od + interval '1 month')::date);
END $$;
-- Migracja początkowa: bieżący miesiąc i 3 kolejne; dalej praca UTRZYMAJ_PARTYCJE (13.3).
SELECT utworz_partycje_decyzji((date_trunc('month', now()) + make_interval(months => m))::date)
FROM generate_series(0, 3) AS m;

-- ===================== 0005_uprawnienia.sql =====================
-- Właścicielem schematu jest lemat_migracje (jedyny użytkownik z DDL, 5.4).
-- 3.2 (C7-15 p. 4): role aplikacji tworzy infrastruktura (Terraform, docker compose: init-db.sh) jako role z LOGIN
-- przed pierwszą migracją; na czystej bazie (Testcontainers, CI) blok DO tworzy je jako role NOLOGIN,
-- więc migracja nie zależy od środowiska. Istniejących ról blok nie zmienia.
DO $$
DECLARE r text;
BEGIN
    FOREACH r IN ARRAY ARRAY['lemat_api','lemat_worker','lemat_scheduler'] LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = r) THEN
            EXECUTE format('CREATE ROLE %I NOLOGIN', r);
        END IF;
    END LOOP;
END $$;
GRANT USAGE ON SCHEMA public TO lemat_api, lemat_worker, lemat_scheduler;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO lemat_api, lemat_worker, lemat_scheduler;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO lemat_api, lemat_worker, lemat_scheduler;
-- Dziennik audytu i Zdarzenia RODO są dopisywane, nie zmieniane (NFR-05, ADR-041):
REVOKE UPDATE, DELETE ON dziennik_audytu FROM lemat_api, lemat_worker, lemat_scheduler;
REVOKE UPDATE, DELETE ON zdarzenie_rodo FROM lemat_api, lemat_scheduler;
REVOKE UPDATE ON zdarzenie_rodo FROM lemat_worker;     -- DELETE po 2 latach wykonuje WYCZYSC_DANE_TECHNICZNE (worker)
ALTER DEFAULT PRIVILEGES IN SCHEMA public                -- 3.2: dla roli wykonującej migrację (lemat_migracje w produkcji)
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO lemat_api, lemat_worker, lemat_scheduler;

-- ===================== 0006_prace_w_tle.sql (3.2, C7-03) =====================
-- Ponowienie i odzyskiwanie dzierżaw bez naruszenia ux_praca_dedup_oczekujace: przy typach TrybDeduplikacji.TylkoOczekujace*
-- może istnieć praca A W_TOKU i praca B OCZEKUJE z tym samym kluczem. Powrót A do OCZEKUJE łamałby indeks, więc
-- A kończy się jako ZAKONCZONA z ostatni_blad = 'ZASTAPIONA_NOWSZA' (B ma nowszy ładunek albo przetworzy wszystkie
-- niezastosowane Wyniki zadania). Każdy wiersz jest zmieniany w osobnym bloku EXCEPTION (podtransakcja), więc konflikt
-- jednej pracy nie wycofuje odzyskania pozostałych; wyścig z równoległym INSERT B (zatwierdzonym po odczycie) też trafia
-- w gałąź unique_violation.
CREATE FUNCTION ponow_prace(p_id bigint, p_wykonawca text, p_blad text, p_odstep interval)
RETURNS status_pracy LANGUAGE plpgsql AS $$
DECLARE v_status status_pracy;
BEGIN
    BEGIN
        UPDATE praca_w_tle p
        SET status = CASE WHEN p.proby >= p.maks_prob THEN 'MARTWA' ELSE 'OCZEKUJE' END::status_pracy,
            dostepna_od = now() + p_odstep, ostatni_blad = p_blad, wykonawca = NULL, dzierzawa_do = NULL, updated_at = now()
        WHERE p.id = p_id AND p.wykonawca = p_wykonawca AND p.status = 'W_TOKU'
        RETURNING p.status INTO v_status;
    EXCEPTION WHEN unique_violation THEN
        UPDATE praca_w_tle p
        SET status = 'ZAKONCZONA', ostatni_blad = 'ZASTAPIONA_NOWSZA', wykonawca = NULL, dzierzawa_do = NULL, updated_at = now()
        WHERE p.id = p_id AND p.wykonawca = p_wykonawca AND p.status = 'W_TOKU'
        RETURNING p.status INTO v_status;
    END;
    RETURN v_status;                                    -- NULL = utrata dzierżawy (ADR-056 p. 4)
END $$;

CREATE FUNCTION odzyskaj_dzierzawy(p_limit integer DEFAULT 500)
RETURNS TABLE (praca_id bigint, praca_typ text, praca_status status_pracy) LANGUAGE plpgsql AS $$
DECLARE r record;
BEGIN
    FOR r IN SELECT p.id FROM praca_w_tle p
             WHERE p.status = 'W_TOKU' AND p.dzierzawa_do < now()
             ORDER BY p.dzierzawa_do LIMIT p_limit
             FOR UPDATE SKIP LOCKED                     -- wiersz potwierdzany przez jednostkę pracy (FOR UPDATE) jest pomijany
    LOOP
        BEGIN
            UPDATE praca_w_tle p
            SET status = CASE WHEN p.proby >= p.maks_prob THEN 'MARTWA' ELSE 'OCZEKUJE' END::status_pracy,
                wykonawca = NULL, dzierzawa_do = NULL, ostatni_blad = 'DZIERZAWA_WYGASLA', updated_at = now()
            WHERE p.id = r.id
            RETURNING p.id, p.typ, p.status INTO praca_id, praca_typ, praca_status;
        EXCEPTION WHEN unique_violation THEN
            UPDATE praca_w_tle p
            SET status = 'ZAKONCZONA', wykonawca = NULL, dzierzawa_do = NULL, ostatni_blad = 'ZASTAPIONA_NOWSZA', updated_at = now()
            WHERE p.id = r.id
            RETURNING p.id, p.typ, p.status INTO praca_id, praca_typ, praca_status;
        END;
        RETURN NEXT;
    END LOOP;
END $$;

-- ===================== 0007_api.sql (3.3, C8-03, ADR-068) =====================
-- Rezerwacja klucza idempotencji w krótkiej transakcji przed wykonaniem; odpowiedź 2xx zapisuje
-- przypadek użycia w SWOJEJ transakcji: UPDATE … SET kod_statusu_http, cialo_http, w_toku_do = NULL,
-- rezerwacja = NULL WHERE (uzytkownik_id, klucz) AND rezerwacja = @moja (0 wierszy → ROLLBACK skutku i powtórka).
CREATE FUNCTION zarezerwuj_klucz_idempotencji(p_uzytkownik_id uuid, p_klucz uuid, p_skrot bytea, p_rezerwacja uuid)
RETURNS TABLE (wynik text, kod_statusu_http smallint, cialo_http jsonb, retry_after_s integer)
LANGUAGE plpgsql AS $$
DECLARE r klucz_idempotencji;
BEGIN
    INSERT INTO klucz_idempotencji AS k (uzytkownik_id, klucz, skrot_zapytania, rezerwacja, w_toku_do, wygasa_at)
    VALUES (p_uzytkownik_id, p_klucz, p_skrot, p_rezerwacja, now() + interval '60 seconds', now() + interval '24 hours')
    ON CONFLICT (uzytkownik_id, klucz) DO UPDATE
        SET rezerwacja = EXCLUDED.rezerwacja, w_toku_do = EXCLUDED.w_toku_do, skrot_zapytania = EXCLUDED.skrot_zapytania
        WHERE k.kod_statusu_http IS NULL AND k.w_toku_do < now()          -- przejęcie klucza po awarii procesu
    RETURNING * INTO r;
    IF FOUND THEN
        RETURN QUERY SELECT 'WYKONAJ'::text, NULL::smallint, NULL::jsonb, NULL::integer; RETURN;
    END IF;
    SELECT * INTO r FROM klucz_idempotencji k WHERE k.uzytkownik_id = p_uzytkownik_id AND k.klucz = p_klucz;
    IF r.skrot_zapytania <> p_skrot THEN
        RETURN QUERY SELECT 'KONFLIKT_CIALA'::text, NULL::smallint, NULL::jsonb, NULL::integer; RETURN;
    END IF;
    IF r.kod_statusu_http IS NOT NULL THEN
        RETURN QUERY SELECT 'POWTORKA'::text, r.kod_statusu_http, r.cialo_http, NULL::integer; RETURN;
    END IF;
    RETURN QUERY SELECT 'W_TOKU'::text, NULL::smallint, NULL::jsonb,
                        GREATEST(1, ceil(extract(epoch FROM r.w_toku_do - now())))::integer;
END $$;
GRANT EXECUTE ON FUNCTION zarezerwuj_klucz_idempotencji(uuid, uuid, bytea, uuid) TO lemat_api;
```
