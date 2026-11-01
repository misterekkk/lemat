# ADR-061: Kontrakt OpenAPI generowany z kodu (`Microsoft.AspNetCore.OpenApi`) i klient TypeScript generowany przez `openapi-typescript`

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.2, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 3, wersja 3.2) |
| Bramka G-0 | nie |
| Realizuje | ADR-054 p. 5 (doprecyzowanie), ADR-049 (nazwy JSON) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.1, 12.4, 12.11; [`docs/spec/api.md`](../spec/api.md) sekcje 1, 3, 5 |

## Status

Nowa (it. 3 pętli 3.x).

## Kontekst

ADR-054 przyjął `openapi-typescript` + `openapi-fetch` i plik
`backend/openapi/lemat-v1.json`, ale nie ustalił, jak powstaje plik, jak
wyglądają typy liczbowe, wyliczenia i strumienie SSE ani jak wykrywany jest
dryf kontraktu. Sprawdzenie na ASP.NET Core 10.0.14 wykazało, że przy
domyślnych `JsonSerializerDefaults.Web` (`NumberHandling = AllowReadingFromString`)
generator opisuje każdą liczbę całkowitą jako `"type": ["integer", "string"]`
z `pattern`, co daje w TypeScript `number | string`.

## Decyzja

1. **Źródło kontraktu = kod.** Rekordy C# żądań i odpowiedzi
   (`Lemat.Api.Kontrakty.<Modul>`, sufiksy `Wejscie`/`Wyjscie`, ADR-049) i
   endpointy Minimal API (`MapGroup("/api/v1/<zasob>")`, `WithName`
   = identyfikator operacji `PascalCase`, `Produces<T>`,
   `ProducesProblem(kod)`) są jedynym źródłem. `Microsoft.AspNetCore.OpenApi`
   10.0 (`AddOpenApi("v1", …)`) generuje dokument OpenAPI 3.1.1 przy
   budowie (`Microsoft.Extensions.ApiDescription.Server`,
   `<OpenApiGenerateDocumentsOnBuild>true`) do
   `backend/openapi/lemat-v1.json`; plik jest w repozytorium.
2. **Serializacja (wspólna dla API i generatora):**
   `JsonSerializerOptions` z `PropertyNamingPolicy = SnakeCaseLower`,
   `NumberHandling = Strict` (sprawdzone: `version` →
   `{"type": "integer", "format": "int32"}`), `JsonStringEnumConverter(SnakeCaseUpper)`
   (wartości enumów `UPPER_SNAKE`, w OpenAPI jako `enum`),
   `DefaultIgnoreCondition = Never` (pola `null` są wysyłane — typ TS
   `T | null`, nie `T?`), `UnmappedMemberHandling = Disallow` dla żądań
   (nieznane pole → `422 WALIDACJA`, ochrona przed literówkami klienta).
   Czas `DateTimeOffset` w UTC (`format: date-time`), data `DateOnly`
   (`format: date`), `decimal` jako liczba JSON.
3. **Transformatory dokumentu** (`IOpenApiDocumentTransformer`,
   `IOpenApiOperationTransformer`): `info.version` = wersja semantyczna API
   (`1.N.M`), schemat `ProblemDetailsLemat` (ADR-060) zamiast
   `ProblemDetails`, nagłówki `ETag`, `Idempotency-Key`, `If-Match`,
   `Retry-After` jako parametry/nagłówki operacji według metadanych
   endpointu (`.WymagaIfMatch()`, `.Idempotentny()`), `x-lemat-kody-bledow`
   (lista `kod` możliwych dla operacji), a dla operacji SSE
   `x-lemat-zdarzenia: [{ event, schemat }]` i schematy danych zdarzeń w
   `components.schemas` (generator sam opisuje SSE jako `SseItemOfObject`,
   co nie daje typów).
4. **Klient TypeScript:** `openapi-typescript` 7.13 generuje
   `frontend/src/lib/api/schema.d.ts` (`npm run api:typy`), a
   `openapi-fetch` 0.14 jest jedynym klientem HTTP (`klient.ts`, 12.11).
   Typy zdarzeń SSE generuje skrypt `frontend/narzedzia/generuj-zdarzenia.ts`
   z `x-lemat-zdarzenia` do `src/lib/sse/zdarzenia.ts` (unia
   rozróżnialna po `event`). Sprawdzone: wygenerowany `schema.d.ts` i
   klient z middleware `onRequest`/`onResponse` przechodzą `tsc --strict`.
5. **Dryf kontraktu:** CI (a) buduje `backend` i porównuje wygenerowany
   `lemat-v1.json` z plikiem w repozytorium (różnica = błąd z instrukcją
   „uruchom `dotnet build` i zatwierdź plik”); (b) generuje `schema.d.ts` i
   porównuje z repozytorium; (c) `oasdiff breaking` porównuje plik z gałęzi
   z plikiem z `main` — zmiana niekompatybilna w `v1` blokuje PR (ADR-062
   p. 1).

## Alternatywy

- **Swashbuckle / NSwag** — odrzucona: `Microsoft.AspNetCore.OpenApi` jest
  wbudowany w .NET 10, obsługuje OpenAPI 3.1 i generowanie przy budowie bez
  dodatkowych pakietów.
- **Kontrakt ręczny (plik YAML) i generowanie serwera** — odrzucona: dwa
  źródła prawdy (rekordy C# i YAML) przy jednym zespole.
- **NSwag/Orval z generowaniem klas klienta** — odrzucona: `openapi-fetch`
  daje typowanie bez kodu wykonywalnego (rozmiar pakietu ≈ 6 kB).

## Konsekwencje

- Zmiana rekordu C# bez regeneracji kontraktu psuje CI, a zmiana
  niekompatybilna wymaga `v2` albo pola opcjonalnego.
- Frontend nie definiuje DTO ręcznie; aliasy `typy.ts` (3.5) wskazują na
  `components["schemas"][...]`.

## Uzupełnienie wersji 3.3 (C8-01, C8-08)

Formularze `multipart/form-data` są klasami `[AsParameters]` z
`[FromForm(Name)]`, a ich schemat OpenAPI buduje `TransformatorFormularzy`
(ADR-067 p. 1–2). Polityka nazw JSON to `NazwyLemat` (snake_case z
separacją cyfr, ADR-067 p. 3), `AllowOutOfOrderMetadataProperties = true`
(p. 5). Typ `Opcjonalne<T>` rekordów `PATCH` ma w OpenAPI schemat `T | null`
poza `required` (ADR-072).
