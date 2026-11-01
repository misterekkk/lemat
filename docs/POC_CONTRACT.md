# POC_CONTRACT.md — kontrakt HTTP proof of concept (Lemat)

| Pole | Wartość |
|---|---|
| Wersja kontraktu | **POC-1.0** (2026-11-01) |
| Status | obowiązujący dla ścieżek `[POC-A]` (backend) i `[POC-B]` (frontend) z `TASKS.md` (TASK-121 … TASK-140) |
| Źródła nadrzędne | `docs/spec/api.md` (rekordy C#, konwencje, model błędu), `docs/ARCHITECTURE_DETAILED.md` 12.1 … 12.6 (katalog 12.3), `docs/spec/ddl.md` (wartości enumów), słownik 3.5 |
| Zasada | Niniejszy plik jest **podzbiorem** 12.3 i `api.md`: te same ścieżki, te same nazwy pól `snake_case` i wartości `UPPER_SNAKE`, ten sam model błędu. Odstępstwa są wyłącznie te z sekcji 9 (U-21 … U-31) i są wyraźnie oznaczone jako **[Rozszerzenie POC]** albo **[Uproszczenie POC]**. |
| Dla kogo | Backend implementuje dokładnie ten kontrakt. Frontend pracuje na mockach MSW zbudowanych **wyłącznie** z tego pliku (sekcje 6 i 7 zawierają komplet danych), bez uruchomionego backendu. |
| Zmiany | Tylko przez zmianę tego pliku zaakceptowaną przez obie osoby (sekcja 10). Zmiana niezgodna wstecz = nowa wersja `POC-2.0` i ogłoszenie na obu ścieżkach. |

Poza zakresem POC (nie implementować, nie mockować): Test diagnostyczny, Rekomendacje i Ścieżka nauki (jako ekrany i endpointy), Weryfikacje, Klasy, Nauczyciel, Administrator, SSE (`/strumien`), Poprawki Ucznia (`PUT …/transkrypcja`, szkic), Odpowiedzi na Zadania zamknięte, reset hasła, eksport i usuwanie konta, `Idempotency-Key`, limity szybkości.

---

## 1. Konwencje

| Element | Ustalenie (zgodne z 12.1 i `api.md` 1) |
|---|---|
| Adres bazowy | `/api/v1`. Frontend woła wyłącznie ścieżki względne `/api/v1/...` (ten sam origin); w `next dev` żądania `/api/*` przekierowuje `rewrites` z `next.config.ts` na `LEMAT_API_URL` (domyślnie `http://localhost:5080`). W trybie MSW żądania przechwytuje Service Worker (sekcja 8). |
| Format | JSON UTF-8, `Content-Type: application/json; charset=utf-8`. Nazwy pól `snake_case` (separator przed cyfrą: `oswiadczenie_wieku_16`). Wartości enumów `UPPER_SNAKE`. Pola o wartości `null` **są zawsze wysyłane** (brak „pominiętych” pól); nieznane pole w żądaniu = `422 WALIDACJA` (`POLE_NIEZNANE`). |
| Czas | RFC 3339 w UTC: `2026-11-02T10:00:00Z`. Data bez czasu: `2027-05-05`. |
| Liczby | Dziesiętne jako liczba JSON (`0.995`), nigdy napis. Identyfikatory: UUID (`string`) albo `integer` (Działy, Umiejętności). |
| Uwierzytelnianie | `Authorization: Bearer <access_token>` (JWT ES256, ważny **15 min**, claims `sub`, `rola`, `status`, `exp`, `jti`). Token dostępu klient trzyma **tylko w pamięci**. Refresh token: cookie `lemat_rt` (`HttpOnly; SameSite=Strict; Path=/api/v1/auth`, `Secure` gdy HTTPS, `Max-Age` 14 dni), rotowany przy każdym użyciu. |
| Role w POC | Tylko `UCZEN`. Wszystkie endpointy poza `Anonimowy` wymagają ważnego tokenu **i** roli `UCZEN` ze statusem konta `AKTYWNE`. Brak/zły token: `401 NIEUWIERZYTELNIONY`. Inna rola albo konto nieaktywne: `403 BRAK_UPRAWNIEN` / `403 KONTO_NIEAKTYWNE`. |
| Uprawnienia obiektowe | Cudzy zasób (Rozwiązanie innego Ucznia) = `404 NIE_ZNALEZIONO` (nigdy `403`, nigdy `200`). |
| Optymistyczna blokada | `version` w ciele odpowiedzi zasobu jest źródłem nagłówka `If-Match: "<version>"` (liczba w cudzysłowach) w operacjach oznaczonych **ETag = tak** (`PATCH /uczniowie/ja`, `POST /rozwiazania/{id}/zatwierdzenie`). Brak → `428 WYMAGANY_IF_MATCH`; zła forma → `400 NAGLOWEK_NIEPRAWIDLOWY`; niezgodność → `412 KONFLIKT_WERSJI` z `szczegoly.aktualny`. Brak `ETag`/`304`. |
| Nagłówki odpowiedzi | `Cache-Control: no-store`, `Vary: Authorization`, `X-Lemat-Wersja-Api: 1.0.0`. |
| Stronicowanie | `?limit=1..100` (domyślnie 50), `?kursor=<opaque>`; odpowiedź `{ "elementy": [...], "nastepny_kursor": "..." \| null }`. Kursor jest nieprzezroczysty (klient przekazuje go bez interpretacji); zły kursor = `400 KURSOR_NIEPRAWIDLOWY`. |
| Idempotencja | **[Uproszczenie POC]** `Idempotency-Key` nie jest wymagany i jest ignorowany (U-11). Frontend blokuje podwójne kliknięcie. |
| Nagłówek Mock | `X-Lemat-Mock-Scenariusz` — tylko `POST /rozwiazania`, tylko `Development`/`E2E` (sekcja 7.1). W `Production` ignorowany. |
| Język | `title`, `detail`, `komunikat` po polsku. Frontend wyświetla tekst z `messages/pl-PL.json` wg `kod` (gdy jest), w przeciwnym razie `detail`/`title` z serwera. |

### 1.1 Model błędu (RFC 9457, `application/problem+json`)

Każdy błąd (4xx/5xx, także 404/405/415 z routingu) ma ten kształt:

```json
{
  "type": "https://lemat.pl/bledy/rozwiazanie-nieprawidlowy-stan",
  "title": "Nieprawidłowy stan rozwiązania",
  "status": 409,
  "detail": "Dla tego zadania masz już rozwiązanie w trakcie oceny.",
  "instance": "/api/v1/rozwiazania",
  "kod": "ROZWIAZANIE_NIEPRAWIDLOWY_STAN",
  "trace_id": "937cc0277c3834f55269f5cd2e927c23",
  "szczegoly": { "istniejace_rozwiazanie_id": "d1000000-0000-4000-8000-000000000001", "status": "OCENIANIE" }
}
```

`type` = `https://lemat.pl/bledy/` + `kod` w `kebab-case` małymi literami. `szczegoly` jest `null`, gdy kod nie definiuje szczegółów. Dla `422 WALIDACJA` dodatkowo `bledy_pol`:

```json
{
  "type": "https://lemat.pl/bledy/walidacja",
  "title": "Błędne dane",
  "status": 422,
  "detail": "Popraw zaznaczone pola.",
  "instance": "/api/v1/auth/rejestracja",
  "kod": "WALIDACJA",
  "trace_id": "0c3f5e2b8a1d4e6f9b7a2c1d0e8f4a3b",
  "szczegoly": null,
  "bledy_pol": [
    { "pole": "haslo", "kod_reguly": "ZA_KROTKIE", "komunikat": "Hasło musi mieć co najmniej 10 znaków." },
    { "pole": "oswiadczenie_wieku_16", "kod_reguly": "WYMAGANE_OSWIADCZENIE", "komunikat": "Wymagane oświadczenie o ukończeniu 16 lat." }
  ]
}
```

`pole` to ścieżka w ciele żądania w `snake_case` (np. `haslo`, `zdjecia`, `X-Lemat-Mock-Scenariusz` dla nagłówka). Kody reguł POC (`kod_reguly`): `WYMAGANE`, `ZA_KROTKIE`, `ZA_DLUGIE`, `FORMAT_EMAIL`, `WYMAGANE_OSWIADCZENIE`, `ROLA_NIEOBSLUGIWANA`, `POLE_NIEZNANE`, `TYP_NIEPRAWIDLOWY`, `NIE_MOZNA_WYCZYSCIC`, `DZIAL_NIEZNANY`, `PLIK_POZA_POLEM_ZDJECIA`, `MOCK_SCENARIUSZ_NIEZNANY`, `WIAZANIE` (lista zamknięta w POC-1.0; zob. OQ-13 w `HANDOFF.md`).

### 1.2 Kody błędów używane w POC

| `kod` | HTTP | Gdzie | `szczegoly` | Tytuł (`title`) |
|---|---|---|---|---|
| `ZAPYTANIE_NIEPRAWIDLOWE` | 400 | ucięty/niepoprawny JSON | — | Niepoprawne żądanie |
| `KURSOR_NIEPRAWIDLOWY` | 400 | listy | — | Niepoprawny kursor |
| `NAGLOWEK_NIEPRAWIDLOWY` | 400 | `If-Match` bez cudzysłowów / nie liczba | — | Niepoprawny nagłówek |
| `NIEUWIERZYTELNIONY` | 401 | brak/zły/wygasły token; zły e-mail lub hasło (jeden komunikat) | — | Wymagane logowanie |
| `SESJA_UNIEWAZNIONA` | 401 | `POST /auth/odswiezenie` | — | Sesja wygasła |
| `BRAK_UPRAWNIEN` | 403 | rola ≠ `UCZEN` | — | Brak uprawnień |
| `KONTO_NIEAKTYWNE` | 403 | logowanie / dowolny endpoint | — | Konto nieaktywne |
| `NIE_ZNALEZIONO` | 404 | zasób nie istnieje lub nie jest Twój; wygasły adres magazynu | — | Nie znaleziono |
| `METODA_NIEDOZWOLONA` | 405 | zła metoda | — | Metoda niedozwolona |
| `EMAIL_ZAJETY` | 409 | rejestracja | — | Nie udało się założyć konta |
| `ROZWIAZANIE_NIEPRAWIDLOWY_STAN` | 409 | `POST /rozwiazania` (jest nieukończone Rozwiązanie tego Zadania), `POST …/zatwierdzenie` (zły stan) | `{istniejace_rozwiazanie_id?, status}` | Nieprawidłowy stan rozwiązania |
| `TRANSKRYPCJA_NIEAKTUALNA` | 409 | zatwierdzenie | — | Transkrypcja jest nieaktualna |
| `WERSJA_ZADANIA_NIEAKTUALNA` | 409 | `POST /rozwiazania` | `{aktualna_zadanie_wersja_id}` | Treść zadania się zmieniła |
| `KONFLIKT_WERSJI` | 412 | `If-Match` niezgodny | `{aktualny: <zasób z version>}` | Dane zmieniły się w międzyczasie |
| `ZAPYTANIE_ZA_DUZE` | 413 | ciało > 52 MB (zdjęcia) / > 1 MB (reszta) | — | Żądanie zbyt duże |
| `ZDJECIE_ZA_DUZE` | 413 | plik > 10 MB | — | Zdjęcie jest zbyt duże |
| `NIEOBSLUGIWANY_FORMAT` | 415 | plik inny niż JPEG; zły `Content-Type` żądania | — | Nieobsługiwany format |
| `WALIDACJA` | 422 | ciało/nagłówek niepoprawne | `null` + `bledy_pol` | Błędne dane |
| `ZA_DUZO_ZDJEC` | 422 | 0 lub > 5 plików `zdjecia` | — | Nieprawidłowa liczba zdjęć |
| `ZDJECIE_NIEPRAWIDLOWE` | 422 | plik nie jest czytelnym JPEG | — | Nie można odczytać zdjęcia |
| `WYMAGANY_IF_MATCH` | 428 | brak `If-Match` | — | Wymagany nagłówek If-Match |
| `BLAD_WEWNETRZNY` | 500 | nieobsłużony wyjątek (bez szczegółów technicznych) | — | Błąd wewnętrzny |
| `BAZA_NIEDOSTEPNA` | 503 | brak połączenia z bazą | — | Usługa chwilowo niedostępna |

Kody tylko po stronie klienta (nigdy z serwera): `BLAD_SIECI`, `PRZERWANO`, `BLAD_BRAMY` (odpowiedź bez `application/problem+json`), `SESJA_TYMCZASOWO_NIEDOSTEPNA`.

---

## 2. Katalog endpointów POC

Kolumna „12.3” mówi, czy endpoint istnieje w katalogu architektury (`tak`) czy jest **[Rozszerzenie POC]** (`nie`, sekcja 9).

| # | Metoda | Ścieżka | Auth | ETag | 12.3 | Wejście → wyjście |
|---|---|---|---|---|---|---|
| 1 | POST | `/auth/rejestracja` | anonimowy | — | tak | `RejestracjaWejscie` → `201 RejestracjaWyjscie` + cookie |
| 2 | POST | `/auth/logowanie` | anonimowy | — | tak | `LogowanieWejscie` → `200 SesjaWyjscie` + cookie |
| 3 | POST | `/auth/odswiezenie` | anonimowy (cookie) | — | tak | — → `200 SesjaWyjscie` + nowe cookie |
| 4 | POST | `/auth/wylogowanie` | Uczeń | — | tak | — → `204` + wyczyszczone cookie |
| 5 | GET | `/uczniowie/ja` | Uczeń | — | tak | → `UczenWyjscie` |
| 6 | PATCH | `/uczniowie/ja` | Uczeń | tak | tak | `ZmianaUczniaWejscie` (merge-patch) → `200 UczenWyjscie` |
| 7 | GET | `/uczniowie/ja/profil-uproszczony` | Uczeń | — | **nie** | → `ProfilUproszczonyWyjscie` |
| 8 | GET | `/dzialy` | Uczeń | — | tak | → `DzialWyjscie[]` |
| 9 | GET | `/zadania` | Uczeń | — | **nie** (w 12.3 tylko N/A) | `?dzial_id&limit&kursor` → `Strona<ZadanieUczniaWyjscie>` |
| 10 | GET | `/zadania/{id}` | Uczeń | — | tak (bez wymaganego kontekstu) | → `ZadanieUczniaWyjscie` |
| 11 | POST | `/rozwiazania` | Uczeń | — | tak | multipart → `202 RozwiazanieWyjscie` |
| 12 | GET | `/rozwiazania` | Uczeń | — | tak | `?status&limit&kursor` → `Strona<RozwiazanieWyjscie>` |
| 13 | GET | `/rozwiazania/{id}` | Uczeń (własne) | — | tak | → `RozwiazanieWyjscie` (status, odpytywanie, Transkrypcja) |
| 14 | POST | `/rozwiazania/{id}/zatwierdzenie` | Uczeń (własne) | tak | tak | `ZatwierdzenieWejscie` → `202 RozwiazanieWyjscie` |
| 15 | GET | `/rozwiazania/{id}/ocena` | Uczeń (własne) | — | tak | → `OcenaWyjscie` |
| 16 | GET | `/rozwiazania/{id}/zdjecia/{zdjecie_rozwiazania_id}/adres` | Uczeń (własne) | — | tak | → `AdresZdjeciaWyjscie` |
| 17 | GET | `/magazyn-lokalny/{token}` | token w adresie | — | **nie** (U-03, OQ-05) | → `image/jpeg` |
| 18 | GET | `/zdrowie` | anonimowy | — | tak (TASK-003) | → `{ "status": "OK" }` |

Transkrypcja nie ma osobnego endpointu (jak w 12.3): jest polem `transkrypcja` w `RozwiazanieWyjscie` (od statusu `TRANSKRYPCJA_GOTOWA`). Ocena jest dostępna także jako pole `ocena` w `RozwiazanieWyjscie` (od `OCENIONE`) i pod `GET …/ocena`.

---

## 3. Uwierzytelnianie — szczegóły

Typy (z `api.md` 3):

```text
RejestracjaWejscie { email: string(≤254), haslo: string(10..128), imie: string(1..60), nazwisko: string(1..80),
                     rola: "UCZEN" | "NAUCZYCIEL", oswiadczenie_wieku_16: boolean, zgoda_regulamin: boolean,
                     kod_zaproszenia: string | null }
LogowanieWejscie   { email: string, haslo: string }
SesjaWyjscie       { access_token: string, wygasa_at: string(date-time), uzytkownik: UzytkownikSesji }
UzytkownikSesji    { id: uuid, rola: "UCZEN" | "NAUCZYCIEL" | "ADMINISTRATOR", imie: string, flagi: FlagiUzytkownika }
FlagiUzytkownika   { weryfikator_globalny: boolean, recenzent_tresci: boolean }
RejestracjaWyjscie { status_konta: "AKTYWNE" | "OCZEKUJE_NA_ZATWIERDZENIE" | "ZABLOKOWANE" | "USUWANE", sesja: SesjaWyjscie | null }
```

### 3.1 `POST /auth/rejestracja` (anonimowy)

Reguły POC: `rola = "UCZEN"` (`"NAUCZYCIEL"` → `422 WALIDACJA`, `pole: "rola"`, `kod_reguly: "ROLA_NIEOBSLUGIWANA"`, U-21); `oswiadczenie_wieku_16` i `zgoda_regulamin` muszą być `true`; e-mail poprawny i unikalny bez względu na wielkość liter (`citext`); hasło 10–128 znaków; `kod_zaproszenia` ignorowany. Konto powstaje jako `AKTYWNE`; w tej samej transakcji powstają `uzytkownik`, `uczen`, `profil_ucznia` i `sciezka_nauki` (puste).

Żądanie:

```json
{
  "email": "ola.wisniewska@example.com",
  "haslo": "BardzoTajne#2026",
  "imie": "Ola",
  "nazwisko": "Wiśniewska",
  "rola": "UCZEN",
  "oswiadczenie_wieku_16": true,
  "zgoda_regulamin": true,
  "kod_zaproszenia": null
}
```

Odpowiedź `201` (`Set-Cookie: lemat_rt=<opaque>; HttpOnly; SameSite=Strict; Path=/api/v1/auth; Max-Age=1209600`):

```json
{
  "status_konta": "AKTYWNE",
  "sesja": {
    "access_token": "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJjMTAwMDAwMC0wMDAwLTQwMDAtODAwMC0wMDAwMDAwMDAwMDMiLCJyb2xhIjoiVUNaRU4ifQ.c2lnbmF0dXJh",
    "wygasa_at": "2026-11-02T10:15:00Z",
    "uzytkownik": {
      "id": "c1000000-0000-4000-8000-000000000003",
      "rola": "UCZEN",
      "imie": "Ola",
      "flagi": { "weryfikator_globalny": false, "recenzent_tresci": false }
    }
  }
}
```

Błędy: `409 EMAIL_ZAJETY` (komunikat nie potwierdza istnienia cudzego konta: „Nie udało się założyć konta na ten adres — zaloguj się albo zresetuj hasło.”), `422 WALIDACJA`, `400 ZAPYTANIE_NIEPRAWIDLOWE`, `415 NIEOBSLUGIWANY_FORMAT`.

### 3.2 `POST /auth/logowanie` (anonimowy)

Żądanie: `{ "email": "uczen1@lemat.local", "haslo": "Lemat-Demo-2026!" }`.

Odpowiedź `200` (+ cookie `lemat_rt` jak wyżej):

```json
{
  "access_token": "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJjMTAwMDAwMC0wMDAwLTQwMDAtODAwMC0wMDAwMDAwMDAwMDEiLCJyb2xhIjoiVUNaRU4ifQ.c2lnbmF0dXJh",
  "wygasa_at": "2026-11-02T10:15:00Z",
  "uzytkownik": {
    "id": "c1000000-0000-4000-8000-000000000001",
    "rola": "UCZEN",
    "imie": "Ala",
    "flagi": { "weryfikator_globalny": false, "recenzent_tresci": false }
  }
}
```

Błędy: `401 NIEUWIERZYTELNIONY` (zły e-mail albo hasło — **ten sam** komunikat „Nieprawidłowy e-mail lub hasło.”), `403 KONTO_NIEAKTYWNE`, `422 WALIDACJA`. Brak limitu prób (U-22).

### 3.3 `POST /auth/odswiezenie` (anonimowy, cookie `lemat_rt`)

Bez ciała. Poprawne cookie → `200 SesjaWyjscie` (jak wyżej) i **nowe** `Set-Cookie: lemat_rt` (rotacja; stary token przestaje działać). Brak cookie, token nieznany, wygasły albo użyty ponownie → `401 SESJA_UNIEWAZNIONA` (ponowne użycie unieważnia całą rodzinę tokenów) i wyczyszczone cookie. **[Uproszczenie POC]** Bez 30-sekundowego okna tolerancji i bez synchronizacji kart (U-22): frontend wykonuje odświeżenie jednym żądaniem naraz w obrębie karty (single-flight) i nie wspiera wielu kart jednocześnie.

Klient: przy starcie aplikacji wywołuje `/auth/odswiezenie`; po `401 NIEUWIERZYTELNIONY` na dowolnym żądaniu odświeża raz i ponawia żądanie raz; po `401 SESJA_UNIEWAZNIONA` czyści sesję i przechodzi na `/login`.

### 3.4 `POST /auth/wylogowanie` (Uczeń)

Bez ciała → `204`; unieważnia rodzinę refresh tokenów i wysyła `Set-Cookie: lemat_rt=; Max-Age=0; Path=/api/v1/auth`. Błąd: `401 NIEUWIERZYTELNIONY`.

---

## 4. Uczeń, Działy, uproszczony Profil

### 4.1 `GET /uczniowie/ja` i `PATCH /uczniowie/ja`

```text
UczenWyjscie { id: uuid, email: string, imie: string, nazwisko: string,
               poziom_matury: "PODSTAWOWY" | "ROZSZERZONY" | null, termin_egzaminu: date | null,
               wybrany_dzial_id: integer | null, zgoda_na_trening: boolean,
               test_diagnostyczny: "BRAK" | "W_TOKU" | "ZAKONCZONY" | "POMINIETY" | "NIEDOSTEPNY",
               klasy: KlasaUcznia[], version: integer }
```

W POC `test_diagnostyczny` jest zawsze `"BRAK"` i `klasy` jest zawsze `[]` (U-21).

`GET` → `200`:

```json
{
  "id": "c1000000-0000-4000-8000-000000000001",
  "email": "uczen1@lemat.local",
  "imie": "Ala",
  "nazwisko": "Kowalska",
  "poziom_matury": null,
  "termin_egzaminu": null,
  "wybrany_dzial_id": null,
  "zgoda_na_trening": false,
  "test_diagnostyczny": "BRAK",
  "klasy": [],
  "version": 1
}
```

`PATCH` — JSON Merge Patch (RFC 7396, ADR-072), `Content-Type: application/merge-patch+json`, wymagany `If-Match: "1"` (wartość `version` z ostatniego odczytu). Pola nieobecne = bez zmiany; dozwolone `null` tylko dla `termin_egzaminu` i `wybrany_dzial_id` („wyczyść”); `null` w `poziom_matury` lub `zgoda_na_trening` = `422 WALIDACJA` (`NIE_MOZNA_WYCZYSCIC`); `wybrany_dzial_id` musi istnieć (`DZIAL_NIEZNANY`); `termin_egzaminu` — poprawna data. Żądanie:

```json
{ "poziom_matury": "PODSTAWOWY", "termin_egzaminu": "2027-05-05", "wybrany_dzial_id": 5 }
```

Odpowiedź `200` (`version` rośnie o 1; `UczenWyjscie` z nowymi wartościami):

```json
{
  "id": "c1000000-0000-4000-8000-000000000001",
  "email": "uczen1@lemat.local",
  "imie": "Ala",
  "nazwisko": "Kowalska",
  "poziom_matury": "PODSTAWOWY",
  "termin_egzaminu": "2027-05-05",
  "wybrany_dzial_id": 5,
  "zgoda_na_trening": false,
  "test_diagnostyczny": "BRAK",
  "klasy": [],
  "version": 2
}
```

Błędy: `428 WYMAGANY_IF_MATCH`, `400 NAGLOWEK_NIEPRAWIDLOWY`, `412 KONFLIKT_WERSJI` (`szczegoly.aktualny` = aktualny `UczenWyjscie`), `422 WALIDACJA`, `415 NIEOBSLUGIWANY_FORMAT` (inny `Content-Type`).

### 4.2 `GET /dzialy` (Uczeń)

Zwraca zwykłą tablicę (nie `Strona`) wszystkich 13 Działów wg 7.4, posortowaną po `id`. `poziom_matury` = `ROZSZERZONY` dla Działu z `tylko_rozszerzony = true` (XIII), w pozostałych `PODSTAWOWY` (interpretacja POC, OQ-14).

```json
[
  { "id": 1, "kod": "I", "nazwa": "Liczby rzeczywiste", "poziom_matury": "PODSTAWOWY" },
  { "id": 2, "kod": "II", "nazwa": "Wyrażenia algebraiczne", "poziom_matury": "PODSTAWOWY" },
  { "id": 3, "kod": "III", "nazwa": "Równania i nierówności", "poziom_matury": "PODSTAWOWY" },
  { "id": 4, "kod": "IV", "nazwa": "Układy równań", "poziom_matury": "PODSTAWOWY" },
  { "id": 5, "kod": "V", "nazwa": "Funkcje", "poziom_matury": "PODSTAWOWY" },
  { "id": 6, "kod": "VI", "nazwa": "Ciągi", "poziom_matury": "PODSTAWOWY" },
  { "id": 7, "kod": "VII", "nazwa": "Trygonometria", "poziom_matury": "PODSTAWOWY" },
  { "id": 8, "kod": "VIII", "nazwa": "Planimetria", "poziom_matury": "PODSTAWOWY" },
  { "id": 9, "kod": "IX", "nazwa": "Geometria analityczna na płaszczyźnie kartezjańskiej", "poziom_matury": "PODSTAWOWY" },
  { "id": 10, "kod": "X", "nazwa": "Stereometria", "poziom_matury": "PODSTAWOWY" },
  { "id": 11, "kod": "XI", "nazwa": "Kombinatoryka", "poziom_matury": "PODSTAWOWY" },
  { "id": 12, "kod": "XII", "nazwa": "Rachunek prawdopodobieństwa i statystyka", "poziom_matury": "PODSTAWOWY" },
  { "id": 13, "kod": "XIII", "nazwa": "Optymalizacja i rachunek różniczkowy", "poziom_matury": "ROZSZERZONY" }
]
```

### 4.3 `GET /uczniowie/ja/profil-uproszczony` **[Rozszerzenie POC]** (Uczeń)

Uproszczony widok Profilu ucznia liczony agregacją po Ocenach (U-28). Nie zastępuje `GET /uczniowie/ja/profil` (Elo, TASK-075), którego w POC nie ma. Źródło: Rozwiązania Ucznia w statusie `OCENIONE` (aktualna Ocena `OSTATECZNA`). `sredni_procent_punktow` = `100 · Σ punkty / Σ punkty_max` zaokrąglone do 1 miejsca (`null` bez Ocen); `dzialy` — tylko Działy z co najmniej jedną Oceną, sort po `dzial.id`; `ostatnie_oceny` — do 5 najnowszych (wg `oceniono_at` malejąco).

```text
ProfilUproszczonyWyjscie { uczen_id: uuid, liczba_rozwiazan: integer, liczba_ocenionych: integer,
                           suma_punktow: number, suma_punktow_max: number, sredni_procent_punktow: number | null,
                           dzialy: DzialUproszczony[], ostatnie_oceny: OstatniaOcenaSkrot[] }
DzialUproszczony         { dzial: { id: integer, nazwa: string }, liczba_ocenionych: integer, sredni_procent_punktow: number }
OstatniaOcenaSkrot       { rozwiazanie_id: uuid, zadanie_id: uuid, dzial: { id: integer, nazwa: string },
                           punkty: number, punkty_max: number, oceniono_at: date-time }
```

`liczba_rozwiazan` liczy wszystkie Rozwiązania Ucznia poza `ANULOWANE` i `PORZUCONE`. Przykład po ocenieniu Zadań 1 (2/2), 2 (1/2) i 5 (0/2), a z Zadaniem 3 w trakcie oceny:

```json
{
  "uczen_id": "c1000000-0000-4000-8000-000000000001",
  "liczba_rozwiazan": 4,
  "liczba_ocenionych": 3,
  "suma_punktow": 3,
  "suma_punktow_max": 6,
  "sredni_procent_punktow": 50.0,
  "dzialy": [
    { "dzial": { "id": 3, "nazwa": "Równania i nierówności" }, "liczba_ocenionych": 1, "sredni_procent_punktow": 50.0 },
    { "dzial": { "id": 5, "nazwa": "Funkcje" }, "liczba_ocenionych": 1, "sredni_procent_punktow": 100.0 },
    { "dzial": { "id": 8, "nazwa": "Planimetria" }, "liczba_ocenionych": 1, "sredni_procent_punktow": 0.0 }
  ],
  "ostatnie_oceny": [
    { "rozwiazanie_id": "d1000000-0000-4000-8000-000000000012", "zadanie_id": "a1000000-0000-4000-8000-000000000005", "dzial": { "id": 8, "nazwa": "Planimetria" }, "punkty": 0, "punkty_max": 2, "oceniono_at": "2026-11-02T10:12:00Z" },
    { "rozwiazanie_id": "d1000000-0000-4000-8000-000000000011", "zadanie_id": "a1000000-0000-4000-8000-000000000002", "dzial": { "id": 3, "nazwa": "Równania i nierówności" }, "punkty": 1, "punkty_max": 2, "oceniono_at": "2026-11-02T10:08:00Z" },
    { "rozwiazanie_id": "d1000000-0000-4000-8000-000000000010", "zadanie_id": "a1000000-0000-4000-8000-000000000001", "dzial": { "id": 5, "nazwa": "Funkcje" }, "punkty": 2, "punkty_max": 2, "oceniono_at": "2026-11-02T10:03:00Z" }
  ]
}
```

Nowy Uczeń: `liczba_rozwiazan = 0`, `liczba_ocenionych = 0`, `suma_punktow = 0`, `suma_punktow_max = 0`, `sredni_procent_punktow = null`, `dzialy = []`, `ostatnie_oceny = []`.

---

## 5. Zadania

```text
ZadanieUczniaWyjscie { id: uuid, zadanie_wersja_id: uuid, rodzaj: "OTWARTE_OBLICZENIOWE" | "OTWARTE_Z_DOWODEM" | "ZAMKNIETE",
                       forma_zamknieta: null, tresc_md: string (Markdown + LaTeX w $…$ i $$…$$),
                       zalaczniki: [], opcje: null, stwierdzenia: null, punkty_max: number, poziom_trudnosci: integer(1..5),
                       dzial: { id: integer, nazwa: string }, umiejetnosci: [{ id: integer, nazwa: string, glowna: boolean }] }
```

W POC wszystkie Zadania są `OTWARTE_OBLICZENIOWE`, `zalaczniki = []`, `opcje = null`, `stwierdzenia = null`, `forma_zamknieta = null`. Widoczne są wyłącznie Zadania `OPUBLIKOWANE` i `GLOBALNY`.

### 5.1 `GET /zadania` **[Rozszerzenie POC]**

`?dzial_id=<int>` (opcjonalny filtr), `?limit`, `?kursor`. Sortowanie: `poziom_trudnosci` rosnąco, potem `id`. Odpowiedź `200 Strona<ZadanieUczniaWyjscie>` — dla seeda POC (5 Zadań) pełna treść jest w sekcji 6.3 (to jest dokładna odpowiedź `GET /zadania` bez filtrów). Z filtrem `dzial_id=5` zwracane jest tylko Zadanie 1 (`"nastepny_kursor": null`). Nieistniejący `dzial_id` → pusta lista (`elementy: []`), nie błąd.

### 5.2 `GET /zadania/{id}`

Odpowiedź `200 ZadanieUczniaWyjscie` (jeden element jak w 6.3). Zadanie nieistniejące lub nieopublikowane → `404 NIE_ZNALEZIONO`; `{id}` niebędące UUID → `404 NIE_ZNALEZIONO`. **[Uproszczenie POC]** W 12.3 Uczeń musi podać dokładnie jeden z `?element_sciezki_id`, `?przypisanie_id`, `?rozwiazanie_id`; w POC parametrów kontekstu nie ma (U-23).

Przykład (`GET /api/v1/zadania/a1000000-0000-4000-8000-000000000004`):

```json
{
  "id": "a1000000-0000-4000-8000-000000000004",
  "zadanie_wersja_id": "b1000000-0000-4000-8000-000000000004",
  "rodzaj": "OTWARTE_OBLICZENIOWE",
  "forma_zamknieta": null,
  "tresc_md": "Kąt $\\alpha$ jest ostry i $\\sin\\alpha = \\frac{3}{5}$. Oblicz $\\cos\\alpha$ oraz $\\operatorname{tg}\\alpha$.",
  "zalaczniki": [],
  "opcje": null,
  "stwierdzenia": null,
  "punkty_max": 3,
  "poziom_trudnosci": 3,
  "dzial": { "id": 7, "nazwa": "Trygonometria" },
  "umiejetnosci": [ { "id": 701, "nazwa": "Obliczanie wartości funkcji trygonometrycznych kąta ostrego", "glowna": true } ]
}
```

---

## 6. Dane startowe POC (`Lemat.Tools seed-poc`) i konta demo

Polecenie `seed-poc` (TASK-122) jest idempotentne (`ON CONFLICT DO NOTHING`/porównanie po kodzie lub stałym identyfikatorze), odmawia uruchomienia przy `LEMAT_SRODOWISKO=Production` i wypisuje konta demo w konsoli. Identyfikatory poniżej są **stałe** (frontend i testy na nich polegają).

### 6.1 Konta demo (tylko `Development`/`E2E`)

| Rola | `id` | e-mail | hasło | imię i nazwisko |
|---|---|---|---|---|
| `UCZEN` | `c1000000-0000-4000-8000-000000000001` | `uczen1@lemat.local` | `Lemat-Demo-2026!` | Ala Kowalska |
| `UCZEN` | `c1000000-0000-4000-8000-000000000002` | `uczen2@lemat.local` | `Lemat-Demo-2026!` | Jan Nowak |

Oba konta są `AKTYWNE`, bez poziomu matury, bez Rozwiązań. Hasła są jawne w repozytorium **wyłącznie** dlatego, że to konta lokalne POC (OQ-15); `seed-poc` nie działa w `Production`. Identyfikator trzeciego konta w przykładach (`…0003`) to konto zakładane rejestracją.

### 6.2 Działy i Umiejętności

Działy: wszystkie 13 z 7.4 (`GET /dzialy`, sekcja 4.2), z jawnymi `id` 1 … 13 (późniejsze TASK-025 musi zachować te identyfikatory). Umiejętności (tylko użyte przez Zadania POC; `waga_egzaminacyjna` = 0.100, `tylko_rozszerzony = false`):

| `id` | `kod` | Dział (`id`) | `nazwa` |
|---|---|---|---|
| 101 | `P.I.4` | 1 | Obliczanie procentów i zmian procentowych |
| 301 | `P.III.3` | 3 | Rozwiązywanie równań kwadratowych |
| 502 | `P.V.2` | 5 | Wyznaczanie wierzchołka i wartości największej funkcji kwadratowej |
| 701 | `P.VII.2` | 7 | Obliczanie wartości funkcji trygonometrycznych kąta ostrego |
| 801 | `P.VIII.1` | 8 | Obliczanie długości boków i promienia okręgu opisanego w trójkącie prostokątnym |

### 6.3 Zadania (5 × `OTWARTE_OBLICZENIOWE`, `GLOBALNY`, `OPUBLIKOWANE`, wersja 1, `poziom_matury = PODSTAWOWY`)

Tabela Zadań, Kryteriów oceniania (wariant 1; `czy_wynik_koncowy`, `wymaga_nr_kryterium`) i rozwiązań wzorcowych:

| # | `zadanie_id` / `zadanie_wersja_id` | Dział / Umiejętność | trudność | `punkty_max` | Kryteria (`nr`: opis, pkt) |
|---|---|---|---|---|---|
| 1 | `a1000000-0000-4000-8000-000000000001` / `b1000000-0000-4000-8000-000000000001` | V Funkcje / 502 | 2 | 2 | 1: „Obliczenie odciętej wierzchołka paraboli: $p = 2$.” (1); 2: „Podanie największej wartości funkcji: $f(2) = 1$.” (1; wynik końcowy, wymaga 1) |
| 2 | `a1000000-0000-4000-8000-000000000002` / `b1000000-0000-4000-8000-000000000002` | III Równania i nierówności / 301 | 1 | 2 | 1: „Obliczenie wyróżnika trójmianu: $\\Delta = 1$.” (1); 2: „Obliczenie pierwiastków równania: $x_1 = 2$, $x_2 = 3$.” (1; wynik końcowy, wymaga 1) |
| 3 | `a1000000-0000-4000-8000-000000000003` / `b1000000-0000-4000-8000-000000000003` | I Liczby rzeczywiste / 101 | 2 | 2 | 1: „Zapisanie równania opisującego cenę przed obniżką: $0{,}8x = 640$.” (1); 2: „Obliczenie ceny przed obniżką: $x = 800$ zł.” (1; wynik końcowy, wymaga 1) |
| 4 | `a1000000-0000-4000-8000-000000000004` / `b1000000-0000-4000-8000-000000000004` | VII Trygonometria / 701 | 3 | 3 | 1: „Zastosowanie jedynki trygonometrycznej: $\\cos^2\\alpha = \\frac{16}{25}$.” (1); 2: „Obliczenie $\\cos\\alpha = \\frac{4}{5}$.” (1; wymaga 1); 3: „Obliczenie $\\operatorname{tg}\\alpha = \\frac{3}{4}$.” (1; wynik końcowy, wymaga 2) |
| 5 | `a1000000-0000-4000-8000-000000000005` / `b1000000-0000-4000-8000-000000000005` | VIII Planimetria / 801 | 1 | 2 | 1: „Obliczenie długości przeciwprostokątnej: $c = 10$.” (1); 2: „Obliczenie promienia okręgu opisanego: $R = 5$.” (1; wynik końcowy, wymaga 1) |

(W tabeli opisy Kryteriów zapisano z podwójnym `\\` jak w JSON; w bazie i w `opis` po zdekodowaniu jest pojedynczy `\`.) `rozwiazanie_wzorcowe` — jeden wariant z krokami odpowiadającymi Kryteriom (treść dowolna, niewidoczna dla Ucznia); `wynik_koncowy_wzorcowy` — typ `LICZBA`/`WYRAZENIE` z polami `latex` i `sympy` (np. Zadanie 2: `x_1 = 2,\\ x_2 = 3`, `[2, 3]`). Pola zgodne z 7.5.2 i DDL (suma `punkty_max` Kryteriów = `punkty_max` Zadania — `trg_suma_kryteriow`).

**Dokładna odpowiedź `GET /api/v1/zadania` (bez filtrów) po `seed-poc`:**

```json
{
  "elementy": [
    {
      "id": "a1000000-0000-4000-8000-000000000002",
      "zadanie_wersja_id": "b1000000-0000-4000-8000-000000000002",
      "rodzaj": "OTWARTE_OBLICZENIOWE",
      "forma_zamknieta": null,
      "tresc_md": "Rozwiąż równanie $x^2 - 5x + 6 = 0$.",
      "zalaczniki": [],
      "opcje": null,
      "stwierdzenia": null,
      "punkty_max": 2,
      "poziom_trudnosci": 1,
      "dzial": { "id": 3, "nazwa": "Równania i nierówności" },
      "umiejetnosci": [ { "id": 301, "nazwa": "Rozwiązywanie równań kwadratowych", "glowna": true } ]
    },
    {
      "id": "a1000000-0000-4000-8000-000000000005",
      "zadanie_wersja_id": "b1000000-0000-4000-8000-000000000005",
      "rodzaj": "OTWARTE_OBLICZENIOWE",
      "forma_zamknieta": null,
      "tresc_md": "W trójkącie prostokątnym przyprostokątne mają długości $6$ i $8$. Oblicz długość promienia okręgu opisanego na tym trójkącie.",
      "zalaczniki": [],
      "opcje": null,
      "stwierdzenia": null,
      "punkty_max": 2,
      "poziom_trudnosci": 1,
      "dzial": { "id": 8, "nazwa": "Planimetria" },
      "umiejetnosci": [ { "id": 801, "nazwa": "Obliczanie długości boków i promienia okręgu opisanego w trójkącie prostokątnym", "glowna": true } ]
    },
    {
      "id": "a1000000-0000-4000-8000-000000000001",
      "zadanie_wersja_id": "b1000000-0000-4000-8000-000000000001",
      "rodzaj": "OTWARTE_OBLICZENIOWE",
      "forma_zamknieta": null,
      "tresc_md": "Dana jest funkcja kwadratowa $f(x) = -x^2 + 4x - 3$. Oblicz współrzędne wierzchołka paraboli będącej wykresem funkcji $f$ i podaj największą wartość funkcji $f$.",
      "zalaczniki": [],
      "opcje": null,
      "stwierdzenia": null,
      "punkty_max": 2,
      "poziom_trudnosci": 2,
      "dzial": { "id": 5, "nazwa": "Funkcje" },
      "umiejetnosci": [ { "id": 502, "nazwa": "Wyznaczanie wierzchołka i wartości największej funkcji kwadratowej", "glowna": true } ]
    },
    {
      "id": "a1000000-0000-4000-8000-000000000003",
      "zadanie_wersja_id": "b1000000-0000-4000-8000-000000000003",
      "rodzaj": "OTWARTE_OBLICZENIOWE",
      "forma_zamknieta": null,
      "tresc_md": "Cena roweru po obniżce o $20\\%$ wynosi $640$ zł. Oblicz cenę roweru przed obniżką.",
      "zalaczniki": [],
      "opcje": null,
      "stwierdzenia": null,
      "punkty_max": 2,
      "poziom_trudnosci": 2,
      "dzial": { "id": 1, "nazwa": "Liczby rzeczywiste" },
      "umiejetnosci": [ { "id": 101, "nazwa": "Obliczanie procentów i zmian procentowych", "glowna": true } ]
    },
    {
      "id": "a1000000-0000-4000-8000-000000000004",
      "zadanie_wersja_id": "b1000000-0000-4000-8000-000000000004",
      "rodzaj": "OTWARTE_OBLICZENIOWE",
      "forma_zamknieta": null,
      "tresc_md": "Kąt $\\alpha$ jest ostry i $\\sin\\alpha = \\frac{3}{5}$. Oblicz $\\cos\\alpha$ oraz $\\operatorname{tg}\\alpha$.",
      "zalaczniki": [],
      "opcje": null,
      "stwierdzenia": null,
      "punkty_max": 3,
      "poziom_trudnosci": 3,
      "dzial": { "id": 7, "nazwa": "Trygonometria" },
      "umiejetnosci": [ { "id": 701, "nazwa": "Obliczanie wartości funkcji trygonometrycznych kąta ostrego", "glowna": true } ]
    }
  ],
  "nastepny_kursor": null
}
```

(Kolejność: `poziom_trudnosci` rosnąco, potem `id` rosnąco: Zadania 2, 5 (trudność 1), 1, 3 (trudność 2), 4 (trudność 3).)

### 6.4 Konfiguracja modelu dla Oceny (wymóg DDL)

`ocena` o `zrodlo = AUTOMATYCZNA` wymaga `konfiguracja_modelu_id`, `kalibrator_pewnosci_id`, `prog_pewnosci_id` (CHECK w DDL). `seed-poc` tworzy: aktywną `konfiguracja_modelu` dostawcy `mock` dla ról `ROZPOZNAWANIE_PISMA` i `DECYZYJNY_GLOWNY` (pozostałe role są poza POC), kalibrator tożsamościowy i `prog_pewnosci` 0.95 dla `OTWARTE_OBLICZENIOWE` (zasada U-10).

---

## 7. Rozwiązania

### 7.1 Zachowanie mocka (deterministyczne)

- Rozpoznawanie i Ocena są zmockowane **po stronie serwera** (backend) i **po stronie MSW** (frontend) tak samo. Zawartość Transkrypcji i Oceny zależy wyłącznie od Zadania (tabele 7.2 i 7.3) i od scenariusza.
- **Czasy** (konfigurowalne `Lemat:Poc:OpoznienieRozpoznaniaMs` = **3000**, `Lemat:Poc:OpoznienieOcenyMs` = **2000**): po `POST /rozwiazania` status `PRZESLANE` (do ok. 0,5 s, aż praca w tle zostanie podjęta) → `ROZPOZNAWANIE` na 3 s → `TRANSKRYPCJA_GOTOWA`; po `POST …/zatwierdzenie` status `ZATWIERDZONE` (do ok. 0,5 s) → `OCENIANIE` na 2 s → `OCENIONE`. Całość od zdjęcia do Oceny to ok. 5–6 s plus czas na decyzję Ucznia o zatwierdzeniu. MSW używa tych samych czasów (3000/2000 ms, bez opóźnienia „podjęcia”).
- **Scenariusze** (nagłówek `X-Lemat-Mock-Scenariusz` w `POST /rozwiazania`; nieznana wartość → `422 WALIDACJA`, `pole: "X-Lemat-Mock-Scenariusz"`, `kod_reguly: "MOCK_SCENARIUSZ_NIEZNANY"`; brak nagłówka = `domyslny`):

| Scenariusz | Efekt |
|---|---|
| `domyslny` | Przebieg opisany powyżej; Transkrypcja i Ocena wg tabel 7.2 i 7.3 (wynik zależny od Zadania). |
| `rozpoznanie-nieczytelne` | Po 3 s status `BLAD_ROZPOZNAWANIA`, `kod_bledu_rozpoznawania = "NIECZYTELNE"`, `transkrypcja = null`, `ocena = null`; stan końcowy w POC (Uczeń może przesłać nowe Rozwiązanie, `POST /rozwiazania` nie jest blokowany przez ten status). |

- Ocena jest zawsze „automatyczna”: `status = OSTATECZNA`, `zrodlo = AUTOMATYCZNA`, `pewnosc_oceny = 0.995` (≥ próg 0.95, Z-3: trasa automatyczna), `widocznosc_dla_ucznia = PELNA`, `wariant = 1`, `komunikat = null`, `mozliwe_zgloszenie_do = null`, `krok_podejrzany_odczyt = null`, `mozliwy_blad_odczytu = false`. Brak Weryfikacji (`weryfikacja = null`). Wskazówka (gdy jest błąd) jest zapisana razem z Oceną (`wskazowka_w_przygotowaniu = false`, U-27).
- **Treść zdjęcia nie ma znaczenia** dla mocka (nie jest analizowana); backend sprawdza tylko format, rozmiar i wymiary.

### 7.2 Dane mocka: Transkrypcja per Zadanie (`zrodlo = AGENT_ROZPOZNAJACY`, `wersja = 1`, `przeslanie_nr = 1`, `zgodnosc_z_zadaniem = 0.95`)

Każdy krok: `tekst` (opis słowny), `latex`, `pewnosc_odczytu`; pozostałe pola kroku stałe: `niepewne_fragmenty = []`, `zmieniony_przez_ucznia = false`, `nr_kroku_agenta = null`, `wymaga_sprawdzenia = false`, `podejrzenie_danych_osobowych = false`, `tylko_dane_osobowe = false`, `zastapienia_danych_osobowych = []`. Wartości `latex` są pokazane po zdekodowaniu JSON (w JSON każdy `\` jest zapisany jako `\\`).

| Zadanie | `nr` | `tekst` | `latex` | `pewnosc_odczytu` |
|---|---|---|---|---|
| 1 | 1 | Obliczam odciętą wierzchołka | `p=\frac{-b}{2a}=\frac{-4}{2\cdot(-1)}=2` | 0.97 |
| 1 | 2 | Obliczam wartość funkcji w wierzchołku | `f(2)=-2^2+4\cdot 2-3=1` | 0.95 |
| 1 | 3 | Odpowiedź | `W=(2,1),\ f_{\max}=1` | 0.93 |
| 2 | 1 | Obliczam wyróżnik | `\Delta=(-5)^2-4\cdot 1\cdot 6=1` | 0.96 |
| 2 | 2 | Obliczam pierwiastki | `x_1=\frac{5-1}{2}=2,\quad x_2=\frac{5+1}{2}=4` | 0.91 |
| 3 | 1 | Oznaczam cenę przed obniżką jako x | `0{,}8x=640` | 0.95 |
| 3 | 2 | Obliczam x | `x=\frac{640}{0{,}8}=800` | 0.96 |
| 3 | 3 | Odpowiedź | `800\ \text{zł}` | 0.90 |
| 4 | 1 | Korzystam z jedynki trygonometrycznej | `\cos^2\alpha=1-\sin^2\alpha=1-\frac{9}{25}=\frac{16}{25}` | 0.94 |
| 4 | 2 | Kąt jest ostry, więc cosinus jest dodatni | `\cos\alpha=\frac{4}{5}` | 0.97 |
| 4 | 3 | Obliczam tangens | `\operatorname{tg}\alpha=\frac{\cos\alpha}{\sin\alpha}=\frac{4}{5}:\frac{3}{5}=\frac{4}{3}` | 0.88 |
| 5 | 1 | Obliczam przeciwprostokątną | `c=6+8=14` | 0.95 |
| 5 | 2 | Obliczam promień okręgu opisanego | `R=\frac{c}{2}=7` | 0.94 |

### 7.3 Dane mocka: Ocena per Zadanie

`kryteria[]`: `nr_kryterium`, `opis` (jak w 6.3), `punkty`, `punkty_max` = 1, `prawdopodobienstwo = 0.995` dla każdego Kryterium. `punkty_surowe` = `punkty` (reguła FR-20 nie zmienia wyniku w danych POC).

| Zadanie | `punkty` / `punkty_max` | Punkty Kryteriów (K1, K2, K3) | `pierwszy_bledny_krok` | `typ_bledu` | `wskazowka` |
|---|---|---|---|---|---|
| 1 | 2 / 2 | 1, 1 | `null` | `null` | `null` |
| 2 | 1 / 2 | 1, 0 | 2 | `RACHUNKOWY` | „Sprawdź jeszcze raz krok 2 — policz ponownie drugi pierwiastek.” |
| 3 | 2 / 2 | 1, 1 | `null` | `null` | `null` |
| 4 | 2 / 3 | 1, 1, 0 | 3 | `MERYTORYCZNY` | „Sprawdź jeszcze raz krok 3 — przypomnij sobie, jak definiuje się tangens kąta.” |
| 5 | 0 / 2 | 0, 0 | 1 | `MERYTORYCZNY` | „Sprawdź jeszcze raz krok 1 — jakie twierdzenie łączy długości boków trójkąta prostokątnego?” |

`brakujacy_krok = false` zawsze. `uzasadnienie` składane szablonem (po polsku, Markdown z LaTeX; KaTeX renderuje je razem z `opis`):
`Kryteria: K1 „{opis}” — {p}/{max} pkt; K2 „{opis}” — {p}/{max} pkt. [Pierwszy błędny krok: {n} ({błąd rachunkowy|błąd merytoryczny|niepełne rozwiązanie}). ]Razem {punkty}/{punkty_max} pkt.` — zdanie o błędnym kroku występuje tylko gdy `typ_bledu ≠ null`.

### 7.4 `POST /rozwiazania` (Uczeń) — przesłanie zdjęć

`Content-Type: multipart/form-data`. Pola: `zadanie_wersja_id` (UUID, wymagane), `element_sciezki_id` i `przypisanie_id` (opcjonalne, **ignorowane** w POC, U-24), pliki **wyłącznie** pod nazwą `zdjecia` (1 … 5 plików; `PLIK_POZA_POLEM_ZDJECIA` gdy plik ma inną nazwę pola). Ograniczenia (U-25): tylko **JPEG** (`image/jpeg`, magic bytes `FF D8 FF`; PNG/WEBP/HEIC → `415 NIEOBSLUGIWANY_FORMAT`, frontend konwertuje do JPEG), ≤ 10 MB na plik, ≤ 52 MB łącznie (`413`), poprawnie odczytywalny nagłówek JPEG z wymiarami (inaczej `422 ZDJECIE_NIEPRAWIDLOWE`). Opcjonalny nagłówek `X-Lemat-Mock-Scenariusz`.

Reguły stanu: `zadanie_wersja_id` musi być wersją aktualną opublikowanego Zadania (nieznana → `404 NIE_ZNALEZIONO`; stara → `409 WERSJA_ZADANIA_NIEAKTUALNA`); jeśli Uczeń ma już Rozwiązanie tego Zadania w statusie innym niż `OCENIONE`, `BLAD_ROZPOZNAWANIA`, `PORZUCONE`, `ANULOWANE` → `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN` z `szczegoly.istniejace_rozwiazanie_id` i `szczegoly.status`. Można więc ponownie rozwiązywać to samo Zadanie po `OCENIONE` (nowe Rozwiązanie, nowa Ocena).

Przykład (cURL):

```text
curl -X POST http://localhost:5080/api/v1/rozwiazania \
  -H "Authorization: Bearer <access_token>" \
  -H "X-Lemat-Mock-Scenariusz: domyslny" \
  -F "zadanie_wersja_id=b1000000-0000-4000-8000-000000000002" \
  -F "zdjecia=@kartka-1.jpg;type=image/jpeg"
```

Odpowiedź `202 Accepted` (`Location: /api/v1/rozwiazania/d1000000-0000-4000-8000-000000000011`):

```json
{
  "id": "d1000000-0000-4000-8000-000000000011",
  "zadanie_id": "a1000000-0000-4000-8000-000000000002",
  "zadanie_wersja_id": "b1000000-0000-4000-8000-000000000002",
  "status": "PRZESLANE",
  "element_sciezki_id": "e5000000-0000-4000-8000-000000000011",
  "przypisanie_id": null,
  "zdjecia": [
    { "id": "e1000000-0000-4000-8000-000000000011", "przeslanie_nr": 1, "kolejnosc": 1, "dostepne": true }
  ],
  "liczba_przeslan": 1,
  "liczba_ponowien_rozpoznania": 0,
  "kod_bledu_rozpoznawania": null,
  "transkrypcja": null,
  "ocena": null,
  "weryfikacja": null,
  "version": 1,
  "created_at": "2026-11-02T10:05:00Z",
  "updated_at": "2026-11-02T10:05:00Z",
  "nastepne_odpytanie_ms": 1000,
  "szkic_transkrypcji": null
}
```

`element_sciezki_id` to techniczny element utworzony przez serwer (U-24); frontend go ignoruje. Błędy: `401`, `404 NIE_ZNALEZIONO`, `409 WERSJA_ZADANIA_NIEAKTUALNA`, `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN`, `413 ZDJECIE_ZA_DUZE`/`ZAPYTANIE_ZA_DUZE`, `415 NIEOBSLUGIWANY_FORMAT`, `422 WALIDACJA` (brak/zły `zadanie_wersja_id`, `PLIK_POZA_POLEM_ZDJECIA`, `MOCK_SCENARIUSZ_NIEZNANY`), `422 ZA_DUZO_ZDJEC`, `422 ZDJECIE_NIEPRAWIDLOWE`.

### 7.5 `GET /rozwiazania/{id}` — status (odpytywanie)

Odpowiedź `200 RozwiazanieWyjscie`. **Odpytywanie:** klient powtarza `GET` co `nastepne_odpytanie_ms` ms, dopóki pole nie jest `null`; wartość `1000` występuje w stanach `PRZESLANE`, `ROZPOZNAWANIE`, `ZATWIERDZONE`, `OCENIANIE`; w `TRANSKRYPCJA_GOTOWA` (czeka na Ucznia), `OCENIONE` i `BLAD_ROZPOZNAWANIA` jest `null`. Klient przerywa odpytywanie po 60 s i pokazuje komunikat „Ocena trwa dłużej niż zwykle — odśwież stronę” (przycisk odświeżenia wznawia odpytywanie).

Pola zależne od statusu: `transkrypcja` ≠ `null` od `TRANSKRYPCJA_GOTOWA` (także po `OCENIONE`); `ocena` ≠ `null` tylko w `OCENIONE`; `kod_bledu_rozpoznawania` ≠ `null` tylko w `BLAD_ROZPOZNAWANIA`; `weryfikacja` i `szkic_transkrypcji` zawsze `null`; `zdjecia[]` zawsze zawiera przesłane pliki. Każde przejście stanu zwiększa `version` o 1 (klient czyta wartość z ciała, nie liczy sam).

**Przykład: `TRANSKRYPCJA_GOTOWA`** (Zadanie 2, po ok. 3 s):

```json
{
  "id": "d1000000-0000-4000-8000-000000000011",
  "zadanie_id": "a1000000-0000-4000-8000-000000000002",
  "zadanie_wersja_id": "b1000000-0000-4000-8000-000000000002",
  "status": "TRANSKRYPCJA_GOTOWA",
  "element_sciezki_id": "e5000000-0000-4000-8000-000000000011",
  "przypisanie_id": null,
  "zdjecia": [
    { "id": "e1000000-0000-4000-8000-000000000011", "przeslanie_nr": 1, "kolejnosc": 1, "dostepne": true }
  ],
  "liczba_przeslan": 1,
  "liczba_ponowien_rozpoznania": 0,
  "kod_bledu_rozpoznawania": null,
  "transkrypcja": {
    "id": "f1000000-0000-4000-8000-000000000011",
    "wersja": 1,
    "zrodlo": "AGENT_ROZPOZNAJACY",
    "przeslanie_nr": 1,
    "zgodnosc_z_zadaniem": 0.95,
    "kroki": [
      {
        "nr": 1,
        "tekst": "Obliczam wyróżnik",
        "latex": "\\Delta=(-5)^2-4\\cdot 1\\cdot 6=1",
        "pewnosc_odczytu": 0.96,
        "niepewne_fragmenty": [],
        "zmieniony_przez_ucznia": false,
        "nr_kroku_agenta": null,
        "wymaga_sprawdzenia": false,
        "podejrzenie_danych_osobowych": false,
        "tylko_dane_osobowe": false,
        "zastapienia_danych_osobowych": []
      },
      {
        "nr": 2,
        "tekst": "Obliczam pierwiastki",
        "latex": "x_1=\\frac{5-1}{2}=2,\\quad x_2=\\frac{5+1}{2}=4",
        "pewnosc_odczytu": 0.91,
        "niepewne_fragmenty": [],
        "zmieniony_przez_ucznia": false,
        "nr_kroku_agenta": null,
        "wymaga_sprawdzenia": false,
        "podejrzenie_danych_osobowych": false,
        "tylko_dane_osobowe": false,
        "zastapienia_danych_osobowych": []
      }
    ],
    "created_at": "2026-11-02T10:05:04Z",
    "zakres_poprawek": null,
    "konflikt_filtra": false,
    "zgodnosc_poprawek": null
  },
  "ocena": null,
  "weryfikacja": null,
  "version": 3,
  "created_at": "2026-11-02T10:05:00Z",
  "updated_at": "2026-11-02T10:05:04Z",
  "nastepne_odpytanie_ms": null,
  "szkic_transkrypcji": null
}
```

**Przykład: `OCENIONE`** (Zadanie 2, po zatwierdzeniu i ok. 2 s oceniania; `transkrypcja` jak wyżej, skrócona tu do `…` tylko w tym dokumencie — w odpowiedzi jest pełna):

```json
{
  "id": "d1000000-0000-4000-8000-000000000011",
  "zadanie_id": "a1000000-0000-4000-8000-000000000002",
  "zadanie_wersja_id": "b1000000-0000-4000-8000-000000000002",
  "status": "OCENIONE",
  "element_sciezki_id": "e5000000-0000-4000-8000-000000000011",
  "przypisanie_id": null,
  "zdjecia": [
    { "id": "e1000000-0000-4000-8000-000000000011", "przeslanie_nr": 1, "kolejnosc": 1, "dostepne": true }
  ],
  "liczba_przeslan": 1,
  "liczba_ponowien_rozpoznania": 0,
  "kod_bledu_rozpoznawania": null,
  "transkrypcja": { "id": "f1000000-0000-4000-8000-000000000011", "wersja": 1, "zrodlo": "AGENT_ROZPOZNAJACY", "przeslanie_nr": 1, "zgodnosc_z_zadaniem": 0.95, "kroki": [ "…jak w przykładzie TRANSKRYPCJA_GOTOWA…" ], "created_at": "2026-11-02T10:05:04Z", "zakres_poprawek": null, "konflikt_filtra": false, "zgodnosc_poprawek": null },
  "ocena": {
    "id": "f2000000-0000-4000-8000-000000000011",
    "wersja": 1,
    "status": "OSTATECZNA",
    "zrodlo": "AUTOMATYCZNA",
    "widocznosc_dla_ucznia": "PELNA",
    "punkty": 1,
    "punkty_max": 2,
    "wariant": 1,
    "kryteria": [
      { "nr_kryterium": 1, "opis": "Obliczenie wyróżnika trójmianu: $\\Delta = 1$.", "punkty": 1, "punkty_max": 1, "prawdopodobienstwo": 0.995 },
      { "nr_kryterium": 2, "opis": "Obliczenie pierwiastków równania: $x_1 = 2$, $x_2 = 3$.", "punkty": 0, "punkty_max": 1, "prawdopodobienstwo": 0.995 }
    ],
    "pierwszy_bledny_krok": 2,
    "brakujacy_krok": false,
    "typ_bledu": "RACHUNKOWY",
    "krok_podejrzany_odczyt": null,
    "mozliwy_blad_odczytu": false,
    "wskazowka": "Sprawdź jeszcze raz krok 2 — policz ponownie drugi pierwiastek.",
    "wskazowka_w_przygotowaniu": false,
    "uzasadnienie": "Kryteria: K1 „Obliczenie wyróżnika trójmianu: $\\Delta = 1$.” — 1/1 pkt; K2 „Obliczenie pierwiastków równania: $x_1 = 2$, $x_2 = 3$.” — 0/1 pkt. Pierwszy błędny krok: 2 (błąd rachunkowy). Razem 1/2 pkt.",
    "pewnosc_oceny": 0.995,
    "komunikat": null,
    "mozliwe_zgloszenie_do": null,
    "created_at": "2026-11-02T10:08:00Z"
  },
  "weryfikacja": null,
  "version": 6,
  "created_at": "2026-11-02T10:05:00Z",
  "updated_at": "2026-11-02T10:08:00Z",
  "nastepne_odpytanie_ms": null,
  "szkic_transkrypcji": null
}
```

(W rzeczywistej odpowiedzi tablica `kroki` jest pełna; skrót w przykładzie dotyczy wyłącznie tej dokumentacji.)

**Przykład: `BLAD_ROZPOZNAWANIA`** (scenariusz `rozpoznanie-nieczytelne`): jak `PRZESLANE`, ale `"status": "BLAD_ROZPOZNAWANIA"`, `"kod_bledu_rozpoznawania": "NIECZYTELNE"`, `"transkrypcja": null`, `"ocena": null`, `"version": 3`, `"nastepne_odpytanie_ms": null`.

Błędy: `401`, `404 NIE_ZNALEZIONO` (nieistniejące, cudze lub `{id}` niebędące UUID).

### 7.6 `GET /rozwiazania` (lista własnych)

`?status=<StatusRozwiazania>` (opcjonalny, pojedynczy), `?limit`, `?kursor`. Sortowanie `created_at` malejąco, potem `id`. Odpowiedź `200 Strona<RozwiazanieWyjscie>` (każdy element w pełnym kształcie z 7.5, także z `transkrypcja` i `ocena`). Nieznany `status` → `422 WALIDACJA` (`pole: "status"`, `kod_reguly: "TYP_NIEPRAWIDLOWY"`).

```json
{
  "elementy": [ { "id": "d1000000-0000-4000-8000-000000000011", "status": "OCENIONE", "…": "pełny RozwiazanieWyjscie jak w 7.5" } ],
  "nastepny_kursor": null
}
```

### 7.7 `POST /rozwiazania/{id}/zatwierdzenie` (Uczeń, ETag)

Nagłówek `If-Match: "3"` (wartość `version` z ostatniego odczytu). Żądanie (ciało `application/json`):

```json
{ "transkrypcja_id": "f1000000-0000-4000-8000-000000000011", "wersja": 1 }
```

Wymagany status `TRANSKRYPCJA_GOTOWA`, `transkrypcja_id` równe `najnowsza_transkrypcja_id`, `wersja` równa najnowszej. Odpowiedź `202 RozwiazanieWyjscie` ze statusem `ZATWIERDZONE`, `version` o 1 większym, `transkrypcja` bez zmian, `ocena = null`, `nastepne_odpytanie_ms = 1000`. Błędy: `428 WYMAGANY_IF_MATCH`, `400 NAGLOWEK_NIEPRAWIDLOWY`, `412 KONFLIKT_WERSJI`, `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN` (`szczegoly.status` = aktualny status; np. ponowne zatwierdzenie), `409 TRANSKRYPCJA_NIEAKTUALNA`, `422 WALIDACJA`, `404 NIE_ZNALEZIONO`. **[Uproszczenie POC]** Uczeń nie może poprawić Transkrypcji (brak `PUT …/transkrypcja`, U-26) — może ją tylko zatwierdzić.

### 7.8 `GET /rozwiazania/{id}/ocena`

`200 OcenaWyjscie` (obiekt `ocena` z przykładu 7.5) dla Rozwiązania w statusie `OCENIONE`; w pozostałych statusach `404 NIE_ZNALEZIONO` (szczegóły: „Ocena nie jest jeszcze gotowa.”). Frontend może korzystać z pola `ocena` w `RozwiazanieWyjscie` i nie wołać tego endpointu — oba muszą dawać identyczny obiekt.

### 7.9 `GET /rozwiazania/{id}/zdjecia/{zdjecie_rozwiazania_id}/adres`

`200`:

```json
{ "url": "/api/v1/magazyn-lokalny/MTIzNDU2Nzg5MDEyMzQ1Ng.dGFr_c2lnbmF0dXJh", "wygasa_at": "2026-11-02T10:10:00Z" }
```

`url` jest podpisany (HMAC, ważny 5 min) i otwierany bez nagłówka `Authorization` (`<img src>`). Frontend pobiera adres tuż przed wyświetleniem i po `wygasa_at` pobiera nowy. Nieistniejące/cudze Rozwiązanie lub zdjęcie → `404 NIE_ZNALEZIONO`. W MSW `url` wskazuje statyczny plik `/mock/kartka-przyklad.jpg` (zasób `frontend/public/mock/`).

### 7.10 `GET /magazyn-lokalny/{token}` **[Rozszerzenie POC]**

`200` z `Content-Type: image/jpeg`, `Cache-Control: private, max-age=300`. Token zmieniony lub wygasły → `404 NIE_ZNALEZIONO` (`application/problem+json`). Poza OpenAPI (nazwa tymczasowa, OQ-05).

### 7.11 Statusy Rozwiązania w POC i przejścia

Podzbiór maszyny stanów 8.1 (wartości `status_rozwiazania` z DDL, bez zmian nazw):

```mermaid
stateDiagram-v2
    [*] --> PRZESLANE: POST /rozwiazania
    PRZESLANE --> ROZPOZNAWANIE: worker podejmuje ROZPOZNAJ_PISMO
    ROZPOZNAWANIE --> TRANSKRYPCJA_GOTOWA: po 3 s (Transkrypcja zapisana)
    ROZPOZNAWANIE --> BLAD_ROZPOZNAWANIA: scenariusz rozpoznanie-nieczytelne
    TRANSKRYPCJA_GOTOWA --> ZATWIERDZONE: POST …/zatwierdzenie
    ZATWIERDZONE --> OCENIANIE: worker podejmuje OCEN_ROZWIAZANIE
    OCENIANIE --> OCENIONE: po 2 s (Ocena + Wynik zadania zapisane)
    OCENIONE --> [*]
    BLAD_ROZPOZNAWANIA --> [*]
```

| Status | Kto zmienia | `nastepne_odpytanie_ms` | `transkrypcja` | `ocena` | Co widzi Uczeń |
|---|---|---|---|---|---|
| `PRZESLANE` | API (przy `POST`) | 1000 | `null` | `null` | „Przesłano — czekamy na odczyt” |
| `ROZPOZNAWANIE` | worker | 1000 | `null` | `null` | „Odczytujemy Twoje rozwiązanie…” |
| `TRANSKRYPCJA_GOTOWA` | worker | `null` | tak | `null` | Transkrypcja (kroki z KaTeX) i przycisk „Zatwierdź” |
| `ZATWIERDZONE` | API (przy zatwierdzeniu) | 1000 | tak | `null` | „Oceniamy…” |
| `OCENIANIE` | worker | 1000 | tak | `null` | „Oceniamy…” |
| `OCENIONE` | worker | `null` | tak | tak | Ocena: punkty, Kryteria, uzasadnienie, Wskazówka |
| `BLAD_ROZPOZNAWANIA` | worker | `null` | `null` | `null` | „Nie udało się odczytać zdjęcia” + przycisk „Prześlij ponownie” |

Statusy `PORZUCONE`, `ANULOWANE`, `OCZEKUJE_NA_WERYFIKACJE` występują w typie, ale **nigdy** nie są zwracane w POC.

---

## 8. Wytyczne: MSW (frontend) i testy kontraktu (backend)

**Frontend (MSW).** Handlery MSW (`frontend/src/mocks/`) implementują sekcje 3 … 7 w pamięci (stan per karta): konta demo z 6.1 + rejestracja (`EMAIL_ZAJETY` dla zajętego adresu), tokeny dowolne ciągi, `GET /zadania` i `GET /zadania/{id}` z 6.3, `POST /rozwiazania` (multipart; weryfikuje `zdjecia`), przejścia stanów na timerach 3000/2000 ms wg 7.1 i danych 7.2/7.3, `version` rosnące o 1, błędy w kształcie 1.1 (m.in. `428/412` dla `If-Match`, `409` dla drugiego niezakończonego Rozwiązania tego samego Zadania). Scenariusz z nagłówka `X-Lemat-Mock-Scenariusz` jak w 7.1. Stan sesji odświeżania (odpowiednik cookie `lemat_rt`) wolno trzymać w `localStorage` wyłącznie w trybie MSW (nigdy token dostępu w trybie real). Tryb mock/real przełącza zmienna `NEXT_PUBLIC_LEMAT_API=mock|real` (TASK-138).

**Backend (testy kontraktu).** Przykłady JSON z tego pliku są wzorcami: test integracyjny (TASK-130) porównuje strukturę odpowiedzi (zestaw pól, typy, `null`-e, nazwy `snake_case`, wartości enumów) z przykładami z sekcji 3 … 7 i sprawdza dokładne dane z sekcji 6.3 oraz 7.2 … 7.3.

**Zgodność wartości enumów z DDL:** `status` Rozwiązania, `zrodlo` Transkrypcji (`AGENT_ROZPOZNAJACY`), `status`/`zrodlo` Oceny (`OSTATECZNA`/`AUTOMATYCZNA`), `typ_bledu` (`RACHUNKOWY`, `MERYTORYCZNY`, `NIEPELNE_ROZWIAZANIE`), `kod_bledu_rozpoznawania` (`NIECZYTELNE`, `PRZEKROCZONY_CZAS`, `BLAD_DOSTAWCY`, `ZBYT_WIELE_KROKOW`), `rodzaj` Zadania, `poziom_matury`, `status_konta`, `rola_uzytkownika`.

---

## 9. Odstępstwa od `api.md` / 12.3 (zapisane w `HANDOFF.md` jako U-21 … U-31)

| ID | Odstępstwo | Skutek dla późniejszej pracy |
|---|---|---|
| U-21 | Tylko rola `UCZEN` (rejestracja `NAUCZYCIEL` → `422 ROLA_NIEOBSLUGIWANA`); `UczenWyjscie.test_diagnostyczny` zawsze `BRAK`, `klasy` zawsze `[]` | TASK-072/073/074 rozszerzają bez zmiany kształtu |
| U-22 | Sesja: rotacja refresh tokenu bez okna tolerancji 30 s, bez wielokartowości (Web Locks/BroadcastChannel), bez limitów prób logowania i bez `proba_logowania` | TASK-091, 104 |
| U-23 | **Rozszerzenia kontraktu:** `GET /zadania` dla Ucznia; `GET /zadania/{id}` bez wymaganego `element_sciezki_id`/`przypisanie_id`/`rozwiazanie_id`; `GET /uczniowie/ja/profil-uproszczony`; `GET /magazyn-lokalny/{token}` | po MVP: Uczeń dostaje Zadania ze Ścieżki nauki (TASK-076/083); `profil-uproszczony` zastępuje `GET /uczniowie/ja/profil` (TASK-075/084) |
| U-24 | Kontekst próby: serwer przy `POST /rozwiazania` tworzy techniczny `element_sciezki` (`typ = ZADANIE`, `status = ROZPOCZETY`), bo DDL wymaga `element_sciezki_id` lub `przypisanie_id`; `element_sciezki_id` w odpowiedzi to ten element | TASK-076/077: kontekst z prawdziwej Ścieżki nauki |
| U-25 | Zdjęcia: tylko JPEG (CHECK DDL `typ_mime = 'image/jpeg'`, brak normalizacji, U-02); konwersja do JPEG po stronie klienta; wymiary z nagłówka JPEG | TASK-056 |
| U-26 | Potok: `praca_w_tle` (`ROZPOZNAJ_PISMO`, `OCEN_ROZWIAZANIE`) + prosta pętla odpytywania bazy w `Lemat.Worker` (`FOR UPDATE SKIP LOCKED`, bez `LISTEN/NOTIFY`, bez pul R/S/O/P/W); handlery minimalne z deterministycznym Mockiem zamiast Agentów; brak Filtra danych osobowych, trasowania T-1 … T-7 i Weryfikacji; brak Poprawek Ucznia; odpytywanie zamiast SSE | TASK-024, 043 … 050 zastępują implementacje, zachowując typy prac i kształt danych |
| U-27 | Wskazówka stała (dane mocka 7.3), zapisana razem z Oceną; bez Agenta wyjaśniającego i strażnika wycieku | TASK-046, 050 |
| U-28 | `wynik_zadania` zapisywany (`zrodlo = OCENA`, `waga = 1.00`), ale Profil ucznia (Elo) **nie** jest aktualizowany (brak `AKTUALIZUJ_PROFIL`); „profil uproszczony” to agregat po Ocenach | TASK-050, 075 |
| U-29 | Dane: `seed-poc` zamiast `seed-dev` + TASK-025/027/028 (13 Działów, 5 Umiejętności, 5 Zadań, 2 konta); stałe hasła demo | TASK-025 … 028 zachowują identyfikatory Działów 1 … 13 i Zadań POC |
| U-30 | Frontend: typy TypeScript utrzymywane ręcznie według tego pliku (zamiast `openapi-typescript`/`gen:api`) i cienki klient `fetch` w `lib/api/client.ts` (zamiast `openapi-fetch`); MSW jako `devDependency` (OQ-12) | TASK-081 podmienia typy i klient na generowane |
| U-31 | Kody reguł `bledy_pol` (`kod_reguly`) to lista zamknięta POC-1.0 z 1.1 (12.2 nie definiuje listy) | OQ-13 |

---

## 10. Zmiany kontraktu i wersjonowanie

1. Plik zmienia się **wyłącznie** osobnym commitem/PR „docs(poc-contract): …” zaakceptowanym przez obie osoby; obie ścieżki rebase'ują się na `main` po jego scaleniu, zanim sięgną po zmienione miejsce.
2. Zmiany zgodne wstecz (nowe pole odpowiedzi, nowy endpoint, nowy kod błędu) podnoszą wersję `POC-1.x`; niezgodne (zmiana nazwy/typu/semantyki pola, usunięcie) — `POC-2.0` i wymagają zgody właściciela.
3. Wątpliwość lub błąd w kontrakcie zgłasza się w `HANDOFF.md` → sekcja 12 („Pytania do kontraktu”), a nie rozstrzyga po cichu w kodzie.
4. Rejestr zmian:

| Wersja | Data | Zmiana |
|---|---|---|
| POC-1.0 | 2026-11-01 | wersja początkowa |
