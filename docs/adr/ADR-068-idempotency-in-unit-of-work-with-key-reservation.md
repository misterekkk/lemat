# ADR-068: Idempotencja — rezerwacja klucza z terminem i zapis odpowiedzi w transakcji skutku

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.3, 2026-11-01); uzupełniona w wersji 3.5 (C9-01, C9-07 p. 1) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 4, wersja 3.3) |
| Bramka G-0 | nie |
| Realizuje | C8-03; zastępuje ADR-062 p. 3 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.1, 12.11, 14.5 (E-41, E-47), 15.4 (C-55); [`docs/spec/ddl.md`](../spec/ddl.md) `klucz_idempotencji`, `0007_api.sql` |

## Status

Nowa (it. 4 pętli 3.x). ADR-062 p. 3 (wiersz „w toku” bez terminu, zapis
odpowiedzi poza transakcją przypadku użycia, zapisywanie `4xx`) przestaje
obowiązywać.

## Kontekst

C8-03: (1) wiersz klucza bez odpowiedzi blokował operację na 24 h po
awarii procesu albo przy `503 BAZA_NIEDOSTEPNA` (nieudany `DELETE`), a
klient zgodnie z E-41 ponawiał bez końca; (2) zapis odpowiedzi po `COMMIT`
przypadku użycia pozwalał na drugi skutek po błędzie zapisu odpowiedzi;
(3) zapisane `409 KONFLIKT_WSPOLBIEZNOSCI` wyłączało automatyczne
ponowienie z E-47.

## Decyzja

1. **Rezerwacja (krok 1, krótka transakcja `FiltrIdempotencji`).**
   `SELECT * FROM zarezerwuj_klucz_idempotencji(@uzytkownik_id, @klucz,
   @skrot, @rezerwacja)` z `@rezerwacja = Guid.CreateVersion7()`. Funkcja
   (`ddl.md` `0007_api.sql`) wstawia wiersz z `w_toku_do = now() + 60 s`
   albo przejmuje istniejący wiersz bez odpowiedzi, którego `w_toku_do <
   now()`. Wynik:
   | `wynik` | Odpowiedź filtra |
   |---|---|
   | `WYKONAJ` | wykonanie endpointu z `KontekstIdempotencji(klucz, rezerwacja)` w zakresie żądania |
   | `POWTORKA` | zapisany `kod_statusu_http` i `cialo_http` + `Idempotent-Replayed: true` |
   | `W_TOKU` | `409 IDEMPOTENCJA_W_TOKU`, `Retry-After` = sekundy do `w_toku_do` (≤ 60) |
   | `KONFLIKT_CIALA` | `422 KLUCZ_IDEMPOTENCJI_KONFLIKT` |

   60 s > `RequestTimeouts` 30 s + `ShutdownTimeout` 30 s nie jest
   wymagane: rezerwacja wygasa po 60 s, a ewentualne późne zatwierdzenie
   starego wykonania jest wykluczone przez warunek kroku 2.
2. **Zapis odpowiedzi w transakcji skutku (krok 2).** `IJednostkaPracy`
   (ADR-056) przed `COMMIT`, gdy w zakresie jest `KontekstIdempotencji`,
   wykonuje:
   ```sql
   UPDATE klucz_idempotencji
      SET kod_statusu_http = @kod, cialo_http = @cialo, w_toku_do = NULL, rezerwacja = NULL
    WHERE uzytkownik_id = @u AND klucz = @k AND rezerwacja = @moja_rezerwacja;
   ```
   Ciało odpowiedzi jest serializowane **przed** `COMMIT`: przypadek
   użycia zwraca wynik, endpoint mapuje go na DTO, a `FiltrIdempotencji`
   przekazuje do jednostki pracy funkcję `PrzedZatwierdzeniem(status,
   dto)`; jednostka pracy z kontekstem idempotencji odkłada `COMMIT` do jej
   wywołania (metoda `ZatwierdzAsync` przypadku użycia oznacza transakcję
   jako gotową, a fizyczne `COMMIT` wykonuje filtr po zapisie odpowiedzi).
   `UPDATE` z 0 wierszami (rezerwację przejęło inne wykonanie po upływie
   60 s) → `ROLLBACK` skutku i odpowiedź z kroku 1 ponowionego wywołania
   funkcji (`POWTORKA` albo `W_TOKU`). Skutek i odpowiedź są więc
   zatwierdzane razem albo wcale.
3. **Zapisywane są wyłącznie odpowiedzi `2xx`** (CHECK `kod_statusu_http
   BETWEEN 200 AND 299`). Każda inna odpowiedź (`4xx` domenowe z
   wyjątków, `409 KONFLIKT_WSPOLBIEZNOSCI`, `409 IDEMPOTENCJA_W_TOKU`,
   `429`, `5xx`) oznacza `ROLLBACK` skutku; filtr usuwa wtedy rezerwację
   (`DELETE … WHERE rezerwacja = @moja AND kod_statusu_http IS NULL`) w
   osobnej krótkiej transakcji, a gdy to się nie uda (baza niedostępna),
   rezerwacja wygasa po 60 s. Ponowienie z tym samym kluczem wykonuje
   operację od nowa, co jest poprawne, bo żaden skutek nie został
   zatwierdzony.
4. **Skutki poza bazą przed transakcją.** Obiekty magazynu zapisane przed
   `ROLLBACK` (zdjęcia `POST /rozwiazania`) usuwa sprzątanie
   osieroconych obiektów (ADR-047); Zdarzenie RODO zapisywane do rejestru
   przed transakcją (ADR-041, `DELETE /uczniowie/ja`, `DELETE
   /nauczyciele/ja`) ma w `dane.klucz_idempotencji` klucz żądania, a
   odtwarzanie rejestru (16.4) traktuje zdarzenia z tym samym `(typ,
   uzytkownik_id, klucz_idempotencji)` jako jedno.
5. **Klient.** Klucz jest tworzony raz na intencję i używany ponownie po
   `BLAD_SIECI`, `5xx`, `409 IDEMPOTENCJA_W_TOKU` (po `Retry-After`) i `409
   KONFLIKT_WSPOLBIEZNOSCI` (po `Retry-After: 1`, najwyżej 3 razy, E-47).
   Po innej odpowiedzi `4xx` (Uczeń musi zmienić dane) następne kliknięcie
   tworzy nowy klucz. `wywolaj()` nie ponawia automatycznie `IDEMPOTENCJA_W_TOKU`
   dłużej niż 60 s (dwie próby po `Retry-After`), potem pokazuje „Trwa
   zapisywanie — sprawdź za chwilę” z odświeżeniem listy.

## Alternatywy

- **Klucz wstawiany w transakcji przypadku użycia bez rezerwacji**
  (drugie żądanie czeka na blokadę wiersza unikalnego) — odrzucona:
  `POST /rozwiazania` normalizuje zdjęcia przed transakcją (do kilku
  sekund), więc duplikat wykonałby normalizację i zapis do magazynu, a
  oczekiwanie na blokadę trzymałoby połączenie z puli (ADR-059).
- **Zapisywanie `4xx`** — odrzucona: `4xx` nie ma skutku, a zapis
  blokowałby poprawione ponowienie (p. 3 C8-03).

## Konsekwencje

- Maksymalna blokada klucza po awarii procesu: 60 s (zamiast 24 h).
- Sprawdzone na PostgreSQL 16 (pełne DDL 3.3): rezerwacja → `WYKONAJ`;
  ponowienie w 60 s → `W_TOKU` z `retry_after_s = 60`; po upływie
  `w_toku_do` → `WYKONAJ` (przejęcie); spóźniony `UPDATE` starej
  rezerwacji → 0 wierszy; błąd po zapisie skutku i odpowiedzi przed
  `COMMIT` → 0 skutków i brak odpowiedzi; wykonanie poprawne → 1 skutek,
  powtórka `POWTORKA 201`, inne ciało `KONFLIKT_CIALA`; zapis
  `kod_statusu_http = 409` odrzuca CHECK.
- Testy (`Lemat.Api.Tests/Idempotency`, Testcontainers PostgreSQL 16):
  `KluczBezOdpowiedziPoAwariiProcesuJestPrzejmowany` (rezerwacja bez
  zapisu, `TimeProvider` + 61 s → `WYKONAJ`),
  `Konflikt40P01NieJestZapisywanyWKluczu` (wstrzyknięty `40P01` → `409
  KONFLIKT_WSPOLBIEZNOSCI`, ponowienie tym samym kluczem → `201`),
  `BladZapisuOdpowiedziNieDajeDrugiegoSkutku` (wyjątek w
  `PrzedZatwierdzeniem` po `UPDATE` → brak skutku; ponowienie → dokładnie
  jedno Rozwiązanie), `IdempotencjaPodwojneZadanie` (dwa równoległe
  żądania → jedno `202`, drugie `409 IDEMPOTENCJA_W_TOKU` albo powtórka).

## Uzupełnienie wersji 3.5 (C9-01, C9-07 p. 1)

**Kontekst.** C9-01: `skrot_zapytania` = SHA-256(metoda + ścieżka + ciało)
obejmował losową granicę `multipart` — przeglądarka i undici generują
nową granicę przy każdej serializacji tego samego `FormData`, więc
ponowienie `POST /rozwiazania` (E-02) i podwójne kliknięcie (E-41)
dostawały `422 KLUCZ_IDEMPOTENCJI_KONFLIKT` zamiast powtórki albo `409
IDEMPOTENCJA_W_TOKU`. C9-07 p. 1: przejęcie wygasłej rezerwacji
nadpisywało skrót bez porównania, a wiersz po `wygasa_at` dawał
`POWTORKA`.

**Decyzja.**

1. **Kanoniczny skrót żądania** (`SkrotZapytania`, `api.md` 4.1),
   obliczany przez `FiltrIdempotencji` **po wiązaniu** endpointu, z
   każdym składnikiem zakodowanym jako (długość 4 B big-endian, UTF-8):
   - prefiks wersji formatu `lemat-skrot-zapytania-v1`, metoda,
     `PathBase + Path` (`/api/v1/rozwiazania`), `QueryString`;
   - `application/json` (i `application/merge-patch+json`): `"B"` +
     SHA-256 bajtów ciała. Ciało jest buforowane przed wiązaniem przez
     `BuforowanieCialaIdempotencji` (`EnableBuffering(64 KB, 1 MB)`;
     limit ciała JSON to 1 MB, 12.1), a klient przy ponowieniu wysyła
     ten sam napis `JSON.stringify` (`client.ts` zachowuje obiekt
     żądania w intencji);
   - `multipart/form-data`: pola tekstowe posortowane porządkowo po
     nazwie (`"P"`, nazwa, wartość; wartości jednej nazwy w kolejności
     wysłania), potem pliki pogrupowane po nazwie pola (`"F"`, nazwa,
     kolejność w grupie od 1, długość, SHA-256 treści szesnastkowo).
     **Nie wchodzą:** nagłówek `Content-Type` z granicą, nazwa pliku
     (`filename`), nagłówki części. Formularz jest czytany z
     `IFormFeature` (już związany), treść plików strumieniowo z
     `IFormFile.OpenReadStream()`; pliki > 64 KB są w plikach
     tymczasowych `FormReader`, więc import ZIP do 50 MB nie jest
     buforowany w pamięci procesu.
   - żądanie bez ciała (`DELETE /uczniowie/ja`, `POST …/ponowienie`):
     gałąź JSON ze skrótem pustego ciała.
2. **Funkcja `zarezerwuj_klucz_idempotencji`** (`ddl.md` `0007_api.sql`,
   wersja 3.5): przejęcie rezerwacji po `w_toku_do` wyłącznie przy
   **tym samym** `skrot_zapytania` (inne ciało → `KONFLIKT_CIALA`, wiersz
   bez zmian); wiersz z `wygasa_at <= now()` (jeszcze nieusunięty przez
   sprzątanie) jest traktowany jak brak klucza — nowa rezerwacja, nowy
   skrót, `kod_statusu_http = NULL`, nowe `wygasa_at`; wyścig ze
   sprzątaniem (wiersz usunięty między `INSERT … ON CONFLICT` a `SELECT`)
   powtarza próbę raz.
3. **Klient** (uzupełnienie p. 5): zdjęcia są kompresowane raz na
   intencję, a ponowienie wysyła te same obiekty `Blob` w nowym
   `FormData` — skrót jest równy, choć granica jest inna. `422
   KLUCZ_IDEMPOTENCJI_KONFLIKT` oznacza od wersji 3.5 wyłącznie rzeczywiście
   inną treść pod tym samym kluczem (błąd programistyczny klienta) i jest
   zgłaszane do telemetrii (`klient.idempotencja.konflikt`).

**Konsekwencje.**

- Sprawdzone uruchomieniem (ASP.NET Core 10, `SkrotZapytania` z
  `api.md` 4.1): dwa `curl -F` z tym samym formularzem (różne granice) →
  ten sam skrót; dwa `fetch` w Node 22 z jednym obiektem `FormData`
  (granice różne) → ten sam skrót i równy skrótowi `curl`; inna nazwa
  pliku i inny `Content-Type` części → ten sam skrót; inna kolejność pól
  o różnych nazwach → ten sam skrót; zamiana kolejności zdjęć → inny
  skrót; JSON: to samo ciało → ten sam skrót, inne → inny.
- Sprawdzone na PostgreSQL 16 (pełne DDL 3.5): 8 scenariuszy funkcji
  (`ddl.md` „Zmiany wersji 3.5”).
- Testy (`Lemat.Api.Tests/Idempotency`, Testcontainers PostgreSQL 16,
  `WebApplicationFactory`):
  - `PonowienieMultipartTymSamymKluczemDajePowtorke` — dwa żądania
    `multipart` z tymi samymi polami i plikami, różnymi granicami
    (`MultipartFormDataContent` z jawną granicą `"a1"` i `"b2"`) i tym
    samym `Idempotency-Key`: pierwsze `202`, drugie `202` +
    `Idempotent-Replayed: true`, jedno Rozwiązanie w bazie; trzecie z innym
    plikiem → `422 KLUCZ_IDEMPOTENCJI_KONFLIKT`;
  - `PodwojneKlikniecieMultipartDajeWToku` — drugie żądanie w trakcie
    pierwszego (handler wstrzymany `TaskCompletionSource`) → `409
    IDEMPOTENCJA_W_TOKU`, nie `422`;
  - `PrzejecieRezerwacjiTylkoTymSamymCialem` — rezerwacja bez odpowiedzi,
    `TimeProvider` + 61 s, inne ciało → `422`, to samo ciało → `WYKONAJ`;
  - `KluczPoWygasnieciuJestNowy` — `wygasa_at` w przeszłości, inne ciało →
    `WYKONAJ` (nie `POWTORKA`);
  - `SkrotZapytaniaKanoniczny` (jednostkowy) — przypadki z punktu
    „Sprawdzone uruchomieniem”.
- Playwright `double-click.spec.ts` i `cellular-network.spec.ts`
  (przerwanie odpowiedzi `POST /rozwiazania` przez `route.abort()` po
  odebraniu żądania) mają asercję: brak `422`, jedno Rozwiązanie, ekran
  Transkrypcji po ponowieniu.
