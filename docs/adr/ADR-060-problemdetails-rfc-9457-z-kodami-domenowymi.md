# ADR-060: Błędy API jako ProblemDetails (RFC 9457) z kodem domenowym; hierarchia wyjątków C# i obsługa w Next.js

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.2, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 3, wersja 3.2) |
| Bramka G-0 | nie |
| Realizuje | ADR-048 p. 4, NFR-10; zastępuje model błędu 12.2 wersji 2.4 (FastAPI) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.2, 14.1–14.4; [`docs/spec/api.md`](../spec/api.md) sekcje 2 i 6 |

## Status

Nowa (it. 3 pętli 3.x).

## Kontekst

Wersja 2.4 opisywała błąd jako JSON RFC 9457 z polem `kod` generowany przez
globalny handler FastAPI. Stos .NET 10 ma natywne `ProblemDetails`
(`AddProblemDetails`, `IExceptionHandler`, `IProblemDetailsService`),
a frontend Next.js musi rozróżniać błędy po stabilnym kodzie, nie po tekście.
Macierz błędów 14.1 (E-01 … E-38) wymaga komunikatów po polsku (NFR-10)
w jednym miejscu, a nie w kodzie komponentów (ADR-054 p. 7).

## Decyzja

1. **Format odpowiedzi błędu** — `Content-Type: application/problem+json`,
   pola RFC 9457 `type`, `title`, `status`, `detail`, `instance` oraz
   rozszerzenia (JSON `snake_case`, ADR-049):
   - `kod` (wymagane) — stały identyfikator `UPPER_SNAKE` z katalogu
     `KodBledu` (12.2); jedyne pole, po którym klient podejmuje decyzję;
   - `trace_id` (wymagane) — identyfikator śladu W3C (`Activity.TraceId`),
     także w nagłówku `traceresponse`;
   - `bledy_pol` (dla `WALIDACJA`) — `[{ pole, kod_reguly, komunikat }]`,
     `pole` w notacji ścieżki JSON (`kroki[3].latex`);
   - `szczegoly` (opcjonalne, obiekt zależny od kodu, np. dla
     `WERYFIKACJA_ZAJETA` `{ podjeta_przez: { imie, nazwisko }, zablokowana_do }`,
     dla `ROZWIAZANIE_NIEPRAWIDLOWY_STAN` `{ istniejace_rozwiazanie_id }`,
     dla `KONFLIKT_WERSJI` `{ aktualny: <zasób>, zmiany_tresci: [...] }`).
   `type` = `https://lemat.pl/bledy/<kod w kebab-case>`; `title` — krótki
   tekst pl-PL z katalogu (stały dla kodu); `detail` — tekst pl-PL dla
   konkretnego przypadku bez danych osobowych innych osób niż wywołujący.
2. **Hierarchia wyjątków** (`Lemat.Domain.Wspolne.Bledy`, bez zależności od
   ASP.NET Core): abstrakcyjny `WyjatekDomenowy(KodBledu kod, string? szczegolyTekst, object? szczegoly)`
   i podklasy według rodzaju: `NiezmiennikNaruszonyException` (409),
   `NieprawidlowyStanException` (409), `KonfliktWersjiException` (412),
   `BrakUprawnienException` (403), `NieZnalezionoException` (404),
   `WalidacjaException` (422, lista `BladPola`), `LimitPrzekroczonyException`
   (429, `RetryAfter`), `UslugaNiedostepnaException` (503, `RetryAfter`).
   Kod HTTP wynika z `KodBledu` (atrybut `[Http(409)]` na członku enumu),
   nie z klasy — klasa służy wyłącznie do przechwytywania w kodzie.
   Wyjątki infrastruktury (`DbUpdateConcurrencyException`,
   `PostgresException`, `BrokenCircuitException`, `TimeoutRejectedException`,
   `NpgsqlException`) tłumaczy `TlumaczBledowInfrastruktury`
   (`Lemat.Infrastructure`) na wyjątki domenowe w jednym miejscu (14.2).
3. **Potok ASP.NET Core:** `AddProblemDetails(o => o.CustomizeProblemDetails = DodajTraceId)`,
   `AddExceptionHandler<ObslugaWyjatkowDomenowych>()` (pierwszy),
   `AddExceptionHandler<ObslugaWyjatkowNieoczekiwanych>()` (ostatni: `500
   BLAD_WEWNETRZNY`, log `Error` z `trace_id`, bez treści wyjątku w
   odpowiedzi), `UseExceptionHandler()`, `UseStatusCodePages()` (puste
   `401`/`403`/`404`/`405` z middleware autoryzacji i routingu dostają ciało
   ProblemDetails z kodem `NIEUWIERZYTELNIONY`/`BRAK_UPRAWNIEN`/`NIE_ZNALEZIONO`/`METODA_NIEDOZWOLONA`),
   walidacja wejścia przez `IEndpointFilter` (`FiltrWalidacji<T>`,
   FluentValidation 12) → `422 WALIDACJA`. Sprawdzone na ASP.NET Core
   10.0.14: odpowiedź `409` ma `Content-Type: application/problem+json` i
   pola `type`, `title`, `status`, `detail`, `instance`, `kod`, `trace_id`.
4. **Katalog komunikatów:** `KodBledu` (C#) i `messages/pl-PL.json`
   (`bledy.<KOD>.tytul`, `bledy.<KOD>.akcja`) są generowane z jednego pliku
   `backend/src/Lemat.Api/Bledy/katalog-bledow.yaml` (kod, HTTP, E-xx,
   tytuł, akcja UI) przez generator źródeł `Lemat.Api.Generatory`
   (C#) i skrypt `frontend/narzedzia/generuj-bledy.ts`; test CI
   `KatalogBledowKompletny` porównuje enum, YAML i plik komunikatów.
5. **Next.js:** `klient.ts` zamienia każdą odpowiedź `!ok` na `BladApi
   { status, kod, problem, retryAfterS }`; komponenty reagują wyłącznie na
   `kod`. Błędy zapytań TanStack Query trafiają do `error.tsx` segmentu
   (granica błędu App Router) tylko dla `5xx` i błędów sieci; `4xx` są
   obsługiwane lokalnie w formularzu albo komunikacie inline. Komunikat
   pochodzi z `bledy.<kod>` (next-intl), a `detail` z serwera jest
   pokazywany tylko pod nim jako uzupełnienie.

## Alternatywy

- **Własny format `{ blad: { kod, komunikat } }`** — odrzucona: RFC 9457
  jest standardem obsługiwanym natywnie przez ASP.NET Core i generator
  OpenAPI.
- **Kod HTTP z klasy wyjątku** — odrzucona: ten sam rodzaj błędu (np.
  stan) ma różne kody HTTP w historycznym katalogu (409 vs 422), a katalog
  12.2 jest kontraktem wiążącym.
- **Komunikaty tylko po stronie serwera (`title`, `detail`)** — odrzucona:
  frontend potrzebuje akcji (przycisk, przekierowanie) i komunikatu także
  dla błędów sieci, gdy odpowiedzi nie ma.

## Konsekwencje

- Jeden kontrakt błędu dla wszystkich endpointów; nowy kod wymaga wpisu w
  `katalog-bledow.yaml`, co wymusza komunikat pl-PL.
- Klient nie parsuje tekstów; zmiana tytułu nie psuje logiki UI.
- `trace_id` w każdej odpowiedzi błędu ułatwia zgłoszenia od użytkowników.

## Uzupełnienie wersji 3.3 (C8-08)

Odpowiedzi bez wyjątku domenowego też mają ciało ProblemDetails z `kod`:
błędy wiązania i deserializacji (`ObslugaBledowWiazania`, ADR-067 p. 6),
`404` trasy, `405`, `415` (`UseStatusCodePages`, ADR-067 p. 7). Nowe kody:
`ZAPYTANIE_NIEPRAWIDLOWE` (400), `ZASOB_USUNIETY` (410),
`LIMIT_WERSJI_TRANSKRYPCJI` (409), `PRZYPISANIE_WYCOFANE` (409); po stronie
klienta `BLAD_BRAMY` (odpowiedź bez `application/problem+json`) i
`SESJA_TYMCZASOWO_NIEDOSTEPNA` (ADR-070). Test `KazdaOdpowiedzBleduMaKod`.
