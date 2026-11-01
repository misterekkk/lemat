# ADR-062: Semantyka HTTP API — wersjonowanie, polityki autoryzacji, `Idempotency-Key`, `ETag`/`If-Match`, stronicowanie kursorem i limity szybkości

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.2, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 3, wersja 3.2) |
| Bramka G-0 | nie |
| Realizuje | 12.1 wersji 2.4 w realizacji ASP.NET Core 10; ADR-020, ADR-037, ADR-057 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.1, 12.3, 12.7, 15.3; [`docs/spec/api.md`](../spec/api.md) sekcje 1, 2, 4 |

## Status

Nowa (it. 3 pętli 3.x).

## Kontekst

Reguły 12.1 wersji 2.4 (idempotencja, `If-Match`, kursor, limity) były
realizowane middleware FastAPI. W .NET muszą być zrealizowane filtrami
endpointów i middleware ASP.NET Core tak, by nie zależały od każdego
handlera z osobna i były widoczne w OpenAPI (ADR-061).

## Decyzja

1. **Wersjonowanie.** Ścieżka `/api/v1`; jedna wersja główna na raz
   w produkcji, poprzednia utrzymywana 6 miesięcy po wydaniu następnej
   (nagłówek `Deprecation: @<unix>` i `Sunset: <data HTTP>` na każdej
   odpowiedzi starej wersji). W obrębie `v1` dozwolone są wyłącznie zmiany
   zgodne wstecz: nowy endpoint, nowe pole odpowiedzi, nowe pole
   opcjonalne żądania, nowa wartość enumu **tylko** w polach oznaczonych
   w OpenAPI `x-lemat-enum-otwarty: true` (klient obsługuje wartość
   nieznaną jako „inna”). Frontend i API są wdrażane razem (jeden tag
   obrazów), więc okno niezgodności to czas wdrożenia; `api` zwraca
   `X-Lemat-Wersja-Api: 1.N.M`, a frontend po wykryciu wyższej wersji
   głównej niż zbudowana wyświetla „Dostępna nowa wersja — odśwież stronę”.
2. **Uwierzytelnianie i polityki.** `AddAuthentication().AddJwtBearer`
   (ES256, ADR-020, `ClockSkew = 30 s`, `MapInboundClaims = false`);
   polityki `AddAuthorizationBuilder()`:
   `Uczen` (claim `rola = UCZEN`), `Nauczyciel` (`NAUCZYCIEL`),
   `RecenzentTresci` (`NAUCZYCIEL` + claim `recenzent_tresci = true`; zmiana
   flagi przez `PATCH /admin/uzytkownicy/{id}` unieważnia rodziny refresh
   tokenów konta, więc nowy claim obowiązuje najpóźniej po 15 min), `WeryfikatorGlobalny` (`NAUCZYCIEL` +
   `weryfikator_globalny = true`), `RecenzentLubAdministrator`,
   `NauczycielLubAdministrator`, `UczenLubNauczyciel`, `Administrator`, `Zalogowany`
   (`RequireAuthenticatedUser` + `status = AKTYWNE` z claimu). Polityka
   jest przypisana grupie endpointów (`RequireAuthorization("Uczen")`);
   uprawnienia obiektowe (12.8) sprawdza przypadek użycia (`Aktor`).
3. **Idempotencja** (`FiltrIdempotencji : IEndpointFilter`, metadane
   `.Idempotentny()`): nagłówek `Idempotency-Key` (UUID) wymagany dla
   16 operacji z kolumną „Idem.” katalogu 12.3 (każde `POST` tworzące zasób
   Ucznia albo Nauczyciela i `DELETE` kont) (brak → `428 WYMAGANY_IDEMPOTENCY_KEY`). Filtr w
   osobnej krótkiej transakcji wstawia `klucz_idempotencji (uzytkownik_id,
   klucz, skrot_zapytania = SHA-256(metoda ‖ ścieżka ‖ ciało), wygasa_at = now() + 24 h)`
   z `ON CONFLICT DO NOTHING`:
   - wstawiono → wykonuje endpoint, zapisuje `kod_statusu_http` i
     `cialo_http` (tylko odpowiedzi `2xx` i `4xx` poza `409
     IDEMPOTENCJA_W_TOKU`, `429`; dla `5xx` usuwa wiersz, żeby ponowienie
     mogło się wykonać);
   - istnieje z tym samym skrótem i zapisaną odpowiedzią → zwraca ją z
     nagłówkiem `Idempotent-Replayed: true`;
   - istnieje z tym samym skrótem bez odpowiedzi (żądanie w toku) → `409
     IDEMPOTENCJA_W_TOKU` z `Retry-After: 1`;
   - istnieje z innym skrótem → `422 KLUCZ_IDEMPOTENCJI_KONFLIKT`.
   Dla `multipart/form-data` skrót obejmuje pola tekstowe i SHA-256 każdego
   pliku (strumieniowo, bez buforowania całości w pamięci).
4. **`ETag` / `If-Match`** (`FiltrWersji`, metadane `.WymagaIfMatch(ZasobWersjonowany.R)`):
   odpowiedź zasobu wersjonowanego ma `ETag: "<version>"` (silny ETag,
   liczba całkowita w cudzysłowie; dla Weryfikacji — wersja treści,
   ADR-037). Operacja modyfikująca bez `If-Match` → `428 WYMAGANY_IF_MATCH`,
   z wartością niebędącą liczbą → `400 NAGLOWEK_NIEPRAWIDLOWY`. Wartość
   jest przekazywana do przypadku użycia jako `OczekiwanaWersja` i
   ustawiana jako `OriginalValue` tokenu współbieżności (15.3); konflikt →
   `412 KONFLIKT_WERSJI` z aktualnym zasobem w `szczegoly.aktualny` i
   nagłówkiem `ETag` aktualnej wersji. `If-None-Match` dla `GET` zasobów
   wersjonowanych → `304` bez ciała (oszczędność transferu w sieci
   komórkowej).
5. **Stronicowanie kursorem.** `?limit=1..100` (domyślnie 50) i
   `?kursor=<base64url>`; kursor = `base64url(JSON {"k": [wartości klucza
   sortowania], "v": 1})` podpisany HMAC-SHA256 (klucz `LEMAT_KURSOR_KLUCZ`,
   pierwsze 16 B), sortowanie zawsze z `id` jako ostatnim kluczem
   (stabilność); zapytanie `WHERE (klucz, id) > (@k, @id) ORDER BY klucz,
   id LIMIT @limit + 1`. Odpowiedź `Strona<T> { elementy, nastepny_kursor }`.
   Zmieniony albo sfałszowany kursor → `400 KURSOR_NIEPRAWIDLOWY`.
6. **Limity szybkości.** Limity biznesowe (12.7) są liczone w bazie
   (`count(*)` w transakcji przypadku użycia, spójne między replikami) →
   `429 LIMIT_PRZEKROCZONY` z `Retry-After` wyliczonym z okna. Ochrona
   techniczna repliki: `AddRateLimiter` z polityką `NaUzytkownika`
   (token bucket 20 żądań/s, pojemność 40, klucz `sub` albo IP dla
   anonimowych) i `Logowanie` (5/min na IP); globalnie 600/min na IP w
   Caddy. Odrzucenie przez `RateLimiter` ma to samo ciało ProblemDetails
   (`OnRejected`).
7. **Rozmiary i czasy.** `Kestrel.Limits.MaxRequestBodySize` 52 MB dla
   `POST /rozwiazania` i `POST /rozwiazania/{id}/zdjecia` (5 × 10 MB +
   narzut multipart), 1 MB dla pozostałych; `FormOptions.MultipartBodyLengthLimit`
   10 MB na plik; `RequestTimeouts` 30 s dla żądań zwykłych, bez limitu dla
   `*/strumien` (SSE ma własny limit 10 min, ADR-064).

## Alternatywy

- **Wersjonowanie nagłówkiem `Api-Version` (`Asp.Versioning`)** —
  odrzucona: wersja w ścieżce jest widoczna w logach Caddy i nie wymaga
  dodatkowego pakietu.
- **Słaby ETag `W/"…"`** — odrzucona: `If-Match` wymaga porównania
  silnego (RFC 9110 §13.1.1); `ef-core.md` 4 w wersji 3.1 podawał słaby
  ETag — poprawione na silny.
- **Stronicowanie `offset`/`page`** — odrzucona: przesunięcia przy
  wstawianiu wierszy (kolejka weryfikacji) i koszt `OFFSET` przy dużych
  tabelach.
- **Idempotencja w pamięci repliki** — odrzucona: dwie repliki `api`.

## Konsekwencje

- Każdy endpoint deklaruje politykę, idempotencję i wersjonowanie
  metadanymi; test `KazdyEndpointMaPolitykeIKody` porównuje katalog 12.3 z
  `EndpointDataSource`.
- `klucz_idempotencji` rośnie o ≤ 1 wiersz na operację modyfikującą
  z listy; czyści go `WYCZYSC_DANE_TECHNICZNE` po `wygasa_at`.
