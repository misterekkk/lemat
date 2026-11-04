# ADR-069: Wersja agregatu w ciele odpowiedzi jako źródło `If-Match`; brak warunkowego `GET` (`304`) i nagłówka `ETag` w odczytach

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.3, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 4, wersja 3.3) |
| Bramka G-0 | nie |
| Realizuje | C8-04; zastępuje ADR-062 p. 4 w części `ETag`/`If-None-Match` |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.1, 12.3, 15.2; [`docs/spec/api.md`](../spec/api.md) 1, 4, 6.1 |

## Status

Nowa (it. 4 pętli 3.x).

## Kontekst

C8-04: `ETag` = `version` jednego agregatu był wysyłany w odpowiedziach
złożonych (`RozwiazanieWyjscie` z Oceną, Wskazówką, Weryfikacją i
dostępnością zdjęć; `WeryfikacjaSzczegoly` z blokadą i podpisanymi URL
ważnymi 10 min), a `If-None-Match` → `304` zwracało stan nieaktualny.
Odpytywanie zapasowe przy `wskazowka_w_przygotowaniu` dostawało `304` bez
końca. Ten sam silny `ETag` był wysyłany rolom U i N mimo różnych
reprezentacji (RFC 9110 §8.8.3).

## Decyzja

1. **Brak warunkowego `GET`.** API nie obsługuje `If-None-Match` i nie
   zwraca `304`; każda odpowiedź `/api/v1/*` ma `Cache-Control: no-store`
   i `Vary: Authorization`. Odpytywanie zapasowe (12.10) dotyczy wyłącznie
   stanów przejściowych (≤ 30 s na Rozwiązanie), a SSE dostarcza zmiany bez
   odpytywania, więc oszczędność transferu z `304` (≤ 4 KB na żądanie) nie
   uzasadnia ryzyka nieaktualnego stanu.
2. **Nagłówek `ETag` nie jest wysyłany w odpowiedziach odczytu ani
   modyfikacji.** Źródłem warunku współbieżności jest pole `version` w
   ciele (każdy rekord wyjściowy zasobu wersjonowanego ma `version`;
   Weryfikacja — wersja treści, ADR-037). Klient wysyła `If-Match:
   "<version>"`. Encja-tag zasobu docelowego operacji modyfikującej jest
   zdefiniowana przez serwer jako `"<version>"` agregatu, którego dotyczy
   operacja (kolumna „ETag” 12.3: `R`, `W`, `Z`, `U`, `S`, `K`, `WT`, `B`,
   `P`); ponieważ reprezentacje nie niosą nagłówka `ETag`, nie ma
   walidatora wspólnego dla różnych reprezentacji ról.
3. **Wersje zasobów nadrzędnych w rekordach, z których wywoływane są
   operacje z `If-Match` innego zasobu** (C8-11 p. 2):
   `WeryfikacjaSzczegoly.rozwiazanie_version` (korekta `R` ze strony
   Weryfikacji), `ZgloszenieBleduZadaniaWyjscie.zadanie_version`
   (rozpatrzenie `Z`), `KlasaAdministratora.version` (Przekazanie Klasy
   `K`), `ZadaniePelneWyjscie.version` (`PUT /admin/zadania/{id}/…`).
   Test `KazdaOperacjaIfMatchMaZrodloWersji` sprawdza dla każdej
   mutacji z `If-Match` w mapowaniu stron (`api.md` 7), że jeden z
   odczytów tej strony zwraca pole `version` albo `<zasob>_version`
   właściwego zasobu.
4. **`412 KONFLIKT_WERSJI`** zwraca aktualny zasób w `szczegoly.aktualny`
   (z polem `version`), bez nagłówka `ETag`.
5. **Identyfikator zdarzenia SSE** (`id:` = skrót SHA-256 reprezentacji
   dla roli, ADR-064 p. 2) pozostaje jedynym skrótem reprezentacji.

## Alternatywy

- **`ETag` = skrót reprezentacji + `304`, `If-Match` = wersja z ciała** —
  odrzucona: ta sama nazwa nagłówka niosłaby dwa różne znaczenia, a
  `If-Match` porównywałby wartość inną niż wysyłany `ETag` (RFC 9110
  §13.1.1).
- **Słaby `ETag` z wersji** — odrzucona: `If-Match` używa porównania
  silnego.

## Konsekwencje

- Metadana `.ZwracaETag()` i `.Produces(304)` znikają z endpointów;
  zostaje `.WymagaIfMatch(ZasobWersjonowany.X)`.
- Test `OdpytywanieWidziWskazowke` (zastępuje `Etag304NieUkrywaZmianyOceny`):
  `GET /rozwiazania/{id}` przy `wskazowka_w_przygotowaniu = true`, zapis
  Wskazówki, kolejny `GET` → `200` z `wskazowka ≠ null` i
  `nastepne_odpytanie_ms = null`; test `BrakNaglowkaEtagWOdczytach`
  (żaden `GET` z katalogu nie zwraca `ETag` ani `304`).
