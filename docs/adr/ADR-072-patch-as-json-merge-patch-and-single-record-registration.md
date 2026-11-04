# ADR-072: `PATCH` jako JSON Merge Patch (RFC 7396) z typem `Opcjonalne<T>`; odpowiedź rejestracji jednym rekordem

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.3, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 4, wersja 3.3) |
| Bramka G-0 | nie |
| Realizuje | C8-09 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.3, 12.5; [`docs/spec/api.md`](../spec/api.md) 1, 3 |

## Status

Nowa (it. 4 pętli 3.x).

## Kontekst

C8-09: w `PATCH /uczniowie/ja` i `PATCH /admin/uzytkownicy/{id}` `null`
oznaczało „bez zmiany”, więc Uczeń nie mógł zrezygnować z wybranego Działu
(FR-09) ani usunąć terminu egzaminu (FR-02). `POST /auth/rejestracja`
zwracał `201` w dwóch kształtach bez dyskryminatora.

## Decyzja

1. **Semantyka `PATCH` = JSON Merge Patch (RFC 7396)**,
   `Content-Type: application/merge-patch+json` (akceptowany jest także
   `application/json`): pole nieobecne → bez zmiany; pole z `null` →
   wyczyszczenie (tylko pola, które w DDL dopuszczają `NULL`); pole z
   wartością → ustawienie.
2. **Typ `Opcjonalne<T>`** (`Lemat.Api.Contracts.Common`):
   `readonly record struct Opcjonalne<T>(bool Obecne, T? Wartosc)` z
   konwerterem `OpcjonalneKonwerter : JsonConverterFactory`
   (`HandleNull = true`, więc `null` w JSON daje `Obecne = true, Wartosc =
   null`; brak pola — `default`, czyli `Obecne = false`). Rekordy `PATCH`
   są rekordami z właściwościami `init` (bez konstruktora pozycyjnego —
   generator OpenAPI 10.0.14 nie serializuje domyślnej wartości
   parametru typu `Opcjonalne<T>` i kończy dokument błędem, sprawdzone).
   Transformator schematu OpenAPI zastępuje schemat `Opcjonalne<T>`
   schematem `T | null`; właściwość nie jest w `required`, więc w TS jest
   `pole?: T | null`.
3. **Pola niedopuszczające wyczyszczenia** (`poziom_matury`,
   `zgoda_na_trening`, `status`, flagi, `deklarowane_minuty_weryfikacji`)
   z `null` → `422 WALIDACJA` (`kod_reguly = NIE_MOZNA_WYCZYSCIC`).
   `wybrany_dzial_id: null` → rekomendacja ze wszystkich Działów poziomu
   matury (10.1.2 krok 2); `termin_egzaminu: null` → brak terminu
   (rekomendacja bez członu terminu, 10.1.2).
4. **Rejestracja:** `201 RejestracjaWyjscie { status_konta: StatusKonta,
   sesja: SesjaWyjscie | null }` — Uczeń: `status_konta = AKTYWNE` i
   `sesja ≠ null` (+ cookie `lemat_rt`); Nauczyciel: `status_konta =
   OCZEKUJE_NA_ZATWIERDZENIE`, `sesja = null`, bez cookie. Rekord
   `RejestracjaNauczycielaWyjscie` jest usunięty.

## Alternatywy

- **Jawne pola czyszczące (`wyczysc_wybrany_dzial: true`)** — odrzucona:
  dwa pola na jedną informację i możliwa sprzeczność
  (`wybrany_dzial_id: 3` + `wyczysc_wybrany_dzial: true`).
- **JSON Patch (RFC 6902)** — odrzucona: zbędna złożoność dla płaskich
  rekordów.

## Konsekwencje

- Sprawdzone uruchomieniem (ASP.NET Core 10.0.14): `{}` → oba pola
  `Obecne = false`; `{"wybrany_dzial_id": null, "termin_egzaminu": null}` →
  `Obecne = true, Wartosc = null`; wartości → ustawione; schemat OpenAPI
  `wybrany_dzial_id: ["null","integer"]` poza `required`.
- Testy: `ZmianaUczniaWyczyszczenieDzialu` (`null` → `uczen.wybrany_dzial_id
  IS NULL`), `ZmianaUczniaBrakPolaBezZmiany`, `PoziomMaturyNullOdrzucony`
  (`422`), `RejestracjaUczniaZwracaSesje`, `RejestracjaNauczycielaBezSesji`.
