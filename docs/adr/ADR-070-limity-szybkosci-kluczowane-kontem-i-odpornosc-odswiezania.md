# ADR-070: Limity szybkości kluczowane kontem (IP tylko dla żądań anonimowych i nieudanych) i odświeżanie sesji odporne na chwilowe awarie

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.3, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 4, wersja 3.3) |
| Bramka G-0 | nie |
| Realizuje | C8-05; zastępuje ADR-062 p. 6; uzupełnia ADR-057 p. 1 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.1, 12.7, 14.5 (E-40), 17.4; [`docs/spec/api.md`](../spec/api.md) 6.2 |

## Status

Nowa (it. 4 pętli 3.x).

## Kontekst

C8-05: Uczniowie jednej szkoły wychodzą do internetu przez jeden adres IP
(NAT). Limit `Logowanie` 5/min na IP liczył wszystkie logowania, więc
klasa logująca się na początku lekcji dostawała `429`; limit Caddy
600/min na IP dawał ≈ 6,7 żądania/min na Ucznia przy trzech klasach
(A-03); `odswiezToken()` wylogowywał wszystkie karty przy każdej
odpowiedzi `!ok` (także `429`, `503`, `502`).

## Decyzja

1. **Klucze i progi limitów technicznych** (`AddRateLimiter`, polityki
   przypisane grupom endpointów; odrzucenie →
   `429 LIMIT_PRZEKROCZONY`, `szczegoly.zakres = "TECHNICZNY"`,
   `Retry-After`):
   | Polityka | Endpointy | Klucz | Limit |
   |---|---|---|---|
   | `NaUzytkownika` | wszystkie z polityką inną niż `Anonimowy` | `sub` z JWT | token bucket 20/s, pojemność 40 |
   | `Logowanie` | `POST /auth/logowanie` | (IP, e-mail po `lower()` i `trim()`) | 10/min |
   | `Odswiezenie` | `POST /auth/odswiezenie` | identyfikator rodziny refresh tokenu z cookie `lemat_rt` (bez cookie → `401` przed limiterem) | 30/min |
   | `Anonimowe` | `POST /auth/rejestracja`, `/auth/reset-hasla*`, `GET /api/v1/openapi.json` | IP | 300/min |
2. **Limity biznesowe nieudanych logowań** (12.7, zapytanie do
   `proba_logowania` w przypadku użycia): 5 nieudanych / 15 min na e-mail
   (blokada konta na 15 min z `429`) i 100 nieudanych / 15 min na IP.
   Udane logowania nie są liczone. Próg 100 na IP: klasa 30 Uczniów × 3
   pomyłki = 90 nieudanych prób w ciągu lekcji mieści się w limicie, a
   atak słownikowy z jednego IP jest ograniczony do 400 prób/h.
3. **Caddy** — ochrona przed zalewem, nie limit użytkowy: `rate_limit`
   12 000 żądań/min na IP. Uzasadnienie z modelu A-03: trzy klasy (90
   Uczniów) za jednym NAT, wszyscy w trybie odpytywania zapasowego (60
   żądań/min) + 20 żądań/min pozostałego ruchu = 7 200/min; zapas 1,5 →
   10 800, zaokrąglone do 12 000. Scenariusz obciążenia k6
   `trzy-klasy-za-nat.js` (17.4): 90 wirtualnych Uczniów z jednego IP, 30
   logowań w 60 s, 90 przesłań w 60 s, odpytywanie zapasowe co 1 s przez
   30 s → 0 odpowiedzi `429`.
4. **Odświeżenie tokenu w kliencie** (`odswiezanie.ts`):
   - `401` (`SESJA_UNIEWAZNIONA`, brak cookie) → `token = null`,
     komunikat `WYLOGOWANIE` do wszystkich kart (jedyny przypadek
     wylogowania);
   - `429` → oczekiwanie `Retry-After` (≤ 30 s) i jedno ponowienie;
   - `5xx`, `502`/`504` z Caddy, błąd sieci (`fetch` rzuca) → ponowienia
     po 1, 2 i 4 s (z rozrzutem ±20%); po trzech nieudanych zwracany jest
     błąd `BladApi(kod = "SESJA_TYMCZASOWO_NIEDOSTEPNA")` bez
     wylogowania, a dotychczasowy token zostaje (jeżeli jeszcze ważny,
     żądanie idzie z nim);
   - `trwajace` jest zerowane w `finally` także po wyjątku, więc kolejne
     wywołanie zaczyna nową próbę.
   UI pokazuje pasek „Problem z połączeniem — ponawiamy” i zachowuje szkice
   (Poprawki Transkrypcji, szkic rozstrzygnięcia).

## Alternatywy

- **Limity na IP z białą listą adresów szkół** — odrzucona: wymaga
  rejestru adresów, a szkoły zmieniają operatorów.
- **Brak limitu logowań na IP** — odrzucona: atak rozproszony po e-mailach
  z jednego IP nie byłby ograniczony.

## Konsekwencje

- Testy: Playwright `klasa-za-nat.spec.ts` (30 logowań różnych kont z
  jednego IP w 60 s → 30 × `200`; 6. nieudane logowanie na to samo konto
  → `429`), Vitest `odswiezanie.test.ts` (`503` → bez `WYLOGOWANIE`,
  token zachowany, 3 ponowienia; `429` z `Retry-After: 2` → ponowienie po
  2 s; `401` → `WYLOGOWANIE`; wyjątek sieci → `trwajace = null`),
  `Lemat.Api.Tests/Limity/LimityKluczowaneKontem` (dwa konta z jednego IP
  nie dzielą `NaUzytkownika`).
