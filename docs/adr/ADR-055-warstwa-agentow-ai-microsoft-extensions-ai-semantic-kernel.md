# ADR-055: Warstwa agentów AI w .NET — Microsoft.Extensions.AI, Semantic Kernel i implementacje Mock

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.0, 2026-11-01); uzupełnienie wersji 3.1 — sekcja „Uzupełnienia”; uzupełnienie 3.2 (ADR-065) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 1, wersja 3.0) |
| Bramka G-0 | nie |
| Realizuje | ADR-048 p. 4; realizacja ADR-003, ADR-009, ADR-026 w .NET |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 6.3, 6.6, 10, 13.1, 13.2 |

## Status

Przyjęta. Kontrakty C# agentów, prompty systemowe i adaptery opisuje
szczegółowo iteracja 2.

## Kontekst

NFR-06 wymaga wymienności modeli bez zmian logiki; NFR-07 — modelu
zapasowego za circuit breakerem; Z-1 — Jev decyduje, LLM pisze; NFR-05 —
zapis każdej decyzji z rozkładem. Właściciel projektu wskazał
Microsoft.Extensions.AI (`IChatClient`) i Semantic Kernel (pluginy
`KernelFunction`) jako bazę warstwy agentów, z dostawcami za interfejsami i
implementacją Mock do pracy offline. Jev nie jest modelem językowym
(zwraca rozkłady dla pytań `NOUL`/`SCORE`/`CHOICE`).

## Decyzja

1. **Porty** (interfejsy w `Lemat.Application.Modele`):
   `IModelDecyzyjny.WykonajPlanAsync(string stan, PlanPytan plan,
   DateTimeOffset termin, CancellationToken ct) → Task<RezultatPytan>`;
   `IModelGeneratywny.GenerujJsonAsync<T>(Prompt prompt,
   IReadOnlyList<ReadOnlyMemory<byte>> obrazy, TimeSpan timeout,
   CancellationToken ct)` i `GenerujTekstAsync(Prompt prompt, int
   maxZnakow, TimeSpan timeout, CancellationToken ct)`;
   `IRozpoznawaniePisma.RozpoznajAsync` / `SprawdzPoprawkiAsync`;
   `ISprawdzanieSymboliczne`; `IPredyktorSukcesu`; `IOgranicznikSzybkosci`
   — odpowiedniki 1:1 portów 13.1. Wyjątki: `BladModeluPrzejsciowyException`,
   `BladModeluTrwalyException`, `ModelNiedostepnyException`.
2. **Modele generatywne** wyłącznie przez `Microsoft.Extensions.AI.IChatClient`
   budowany w `Lemat.Modele` (`RejestrAdapterow`) z potoku
   `ChatClientBuilder`: `UseOpenTelemetry()` (bez zapisu treści
   wiadomości, `EnableSensitiveData = false`), `UseLogging()` (metadane
   bez treści), adapter dostawcy. Ustrukturyzowane wyjście:
   `GetResponseAsync<T>` ze schematem JSON generowanym z rekordu C#
   (`AIJsonUtilities.CreateJsonSchema`); obrazy jako `DataContent`
   (`image/jpeg`). Pakiety dostawców (`Microsoft.Extensions.AI.OpenAI`)
   wyłącznie w `Lemat.Modele`. Klucze dostawców: `azure-openai`,
   `openai-zgodny`, `mock` (wybór konkretnego dostawcy dla ról —
   konfiguracja, G-6, A-07).
3. **Jev:** `JevAdapter` jako typowany `HttpClient` (`IHttpClientFactory`)
   z kontraktem A-01 i trybami `WSADOWY`/`POJEDYNCZY` (13.2), bez
   Microsoft.Extensions.AI i Semantic Kernel.
4. **Semantic Kernel** (`Microsoft.SemanticKernel.Core`) wyłącznie w
   `Lemat.Agenci`: (a) szablony promptów w formacie YAML
   (`PromptTemplateConfig`, składnia Handlebars) w
   `Prompty/<ROLA_MODELU>/<wersja_promptu>.yaml`, renderowane do `Prompt`
   przekazywanego do portu; (b) plugin `SprawdzanieSymbolicznePlugin`
   (`[KernelFunction] sprawdz_rownowaznosc`, `postac_liniowa`) używany
   wyłącznie przez Agenta generującego przy tworzeniu rozwiązania
   wzorcowego, z `FunctionChoiceBehavior.Auto` ograniczonym do tych dwóch
   funkcji i ≤ 8 wywołaniami; `Kernel` dostaje `IChatCompletionService`
   uzyskany z `IChatClient` (`AsChatCompletionService()`), więc
   wymienność dostawcy jest zachowana. Model decyzyjny (Jev i model
   zapasowy), Agent rozpoznający i Agent wyjaśniający nie używają
   wywoływania funkcji.
5. **Orkiestracja** nie korzysta z Semantic Kernel Agent Framework ani
   Process Framework: przepływy są maszynami stanów w bazie (6.3), bo
   muszą przetrwać restart procesu, być idempotentne i audytowalne
   (`decyzja_modelu`, NFR-05).
6. **Mock:** `MockModelDecyzyjny`, `MockChatClient : IChatClient`,
   `MockRozpoznawaniePisma` w `Lemat.Modele.Mock` z danymi
   `Mock/Dane/**/*.json` (dopasowanie po kodzie pytania lub nazwie
   szablonu i SHA-256 stanu; brak dopasowania — deterministyczny rozkład z
   ziarnem = SHA-256 stanu), dostawca `mock` w `konfiguracja_modelu`;
   aktywacja `mock` w środowisku `Production` → `409
   DOSTAWCA_NIEDOZWOLONY`, a proces `Production` z aktywną konfiguracją
   `mock` nie startuje. Mock pozwala uruchomić cały przepływ (rozpoznanie
   → ocena → Wskazówka → Profil) offline, w testach i w E2E Playwright.
7. **Circuit breaker i model zapasowy:** własny
   `CircuitBreakerModeluDecyzyjnego` (reguły 13.2: 3 kolejne błędy albo ≥
   50% w 20 s przy ≥ 6 wywołaniach, półotwarty po 60 s; `429` i brak
   przydziału nie liczą się), bo reguła „3 kolejne błędy” i wybór modelu
   dla całej Oceny (ADR-009) nie są dostępne w Polly v8; Polly
   (`Microsoft.Extensions.Http.Resilience`) tylko dla ponowień S3 i SMTP.

## Alternatywy

- **Semantic Kernel jako jedyna abstrakcja (bez `IChatClient`)** —
  odrzucona: `IChatClient` jest abstrakcją niższego poziomu, którą
  wspierają dostawcy i którą Semantic Kernel sam konsumuje; porty
  generatywne nie potrzebują jądra SK.
- **Agent Framework / Process Framework SK do orkiestracji** — odrzucone
  (pkt 5).
- **Wywoływanie funkcji przez model zapasowy** — odrzucone: model
  zapasowy pełni rolę modelu decyzyjnego i musi dawać deterministyczny,
  skalibrowany rozkład z jednego wywołania (ADR-009, ADR-026).
- **SDK dostawców bezpośrednio w agentach** — odrzucone (ADR-003, NFR-06).

## Konsekwencje

- Zmiana dostawcy LLM = nowa konfiguracja + adapter w `Lemat.Modele`
  (często tylko inny klucz `dostawca` dla tego samego pakietu), bez zmian
  w `Lemat.Agenci` i `Lemat.Application`.
- Telemetria GenAI z OpenTelemetry bez treści promptów (RODO, 16.3).
- Prompty są wersjonowane jak w 2.4 (`wersja_promptu`), ale w formacie
  szablonów SK; test sprawdza istnienie pliku dla każdej wersji.

## Uzupełnienia

**Uzupełnienie wersji 3.1 (C6-03, C6-10; ADR-058).** Zmiany:
p. 1 — `IModelGeneratywny` jest fabryką klientów czatu per rola
(`DlaRoliAsync(RolaModelu, CancellationToken) → KlientCzatuRoli`), a
metody `GenerujJsonAsync<T>`/`GenerujTekstAsync` przechodzą do pomocnika
`Lemat.Agenci.Wspolne.WywolanieCzatu`; p. 2 — potok `ChatClientBuilder` z
ADR-058 p. 3 (w tym `UseFunctionInvocation`); p. 4 — Semantic Kernel
(`Microsoft.SemanticKernel.Core`, `Microsoft.SemanticKernel.Yaml`) służy
wyłącznie do pluginów `[KernelFunction]` przekazywanych jako `AIFunction` i
do szablonów promptów **systemowych** w składni `{{$zmienna}}` (bez
Handlebars); limit wywołań funkcji realizuje `FunkcjaZLimitem :
DelegatingAIFunction`; treść od użytkownika nie przechodzi przez szablony;
stan Modelu decyzyjnego buduje kod C# bez Semantic Kernel. Pełne prompty i
schematy — `docs/spec/prompty.md`.

**Uzupełnienie wersji 3.2 (C7-07, C7-12).** Implementacje Mock wybierają
odpowiedź po jawnym scenariuszu (`X-Lemat-Mock-Scenariusz`, poza
`Production`), domyślne odpowiedzi prowadzą przepływ trasą automatyczną,
tryb ścisły w E2E, a konfiguracje `mock` wstawia polecenie `seed-dev`, nie
migracja — ADR-065.
