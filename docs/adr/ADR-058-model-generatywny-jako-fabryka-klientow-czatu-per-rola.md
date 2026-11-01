# ADR-058: Port `IModelGeneratywny` jako fabryka klientów czatu per rola; Semantic Kernel tylko do pluginów i szablonów promptów systemowych

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.1, 2026-11-01); uzupełnienie 3.2 (C7-01, C7-02, C7-08, C7-09, C7-10, C7-14, C7-15) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 2, wersja 3.1) |
| Bramka G-0 | nie |
| Realizuje | C6-03, C6-10; zmienia ADR-055 p. 1, p. 2 i p. 4 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 6.4, 6.5, 6.6, 10.0, 13.1; [`docs/spec/prompty.md`](../spec/prompty.md) |

## Status

Przyjęta.

## Kontekst

ADR-055 w wersji 3.0 definiował `IModelGeneratywny.GenerujJsonAsync<T>(Prompt …)`
i jednocześnie wymagał, by Agent generujący wywoływał
`SprawdzanieSymbolicznePlugin` przez `FunctionChoiceBehavior.Auto`. Pętla
wywołań funkcji wymaga klienta czatu roli `GENERATYWNY_ZADANIA`, którego
port nie udostępniał, a `Lemat.Agenci` nie może sięgać do `Lemat.Modele`
(C6-03). Krytyk wskazał też brakujące pakiety Semantic Kernel, brak
mechanizmu limitu wywołań funkcji i ryzyko kodowania znaków `<`, `>`, `&`
w zmiennych szablonów (C6-10).

## Decyzja

1. **Port per rola.** `Lemat.Application.Modele.IModelGeneratywny` jest
   fabryką klientów czatu dla ról generatywnych i roli rozpoznawania:

   ```csharp
   public interface IModelGeneratywny
   {
       /// Aktywna Konfiguracja modelu roli (pamięć podręczna 30 s, unieważniana NOTIFY konfiguracja_modelu).
       Task<KlientCzatuRoli> DlaRoliAsync(RolaModelu rola, CancellationToken ct);
   }

   public sealed record KlientCzatuRoli(
       RolaModelu Rola, KonfiguracjaModeluId KonfiguracjaModeluId, string Dostawca, string Model,
       string WersjaPromptu, ParametryModelu Parametry,
       IChatClient Czat);   // potok zbudowany w Lemat.Modele (p. 3); agent nie buduje własnego
   ```

   Role dozwolone: `GENERATYWNY_ZADANIA`,
   `GENERATYWNY_ROZWIAZUJACY_NIEZALEZNIE`, `GENERATYWNY_WSKAZOWKI`,
   `GENERATYWNY_WKLADKI`, `GENERATYWNY_OPINIA_POMOCNICZA`,
   `ROZPOZNAWANIE_PISMA`, `DECYZYJNY_ZAPASOWY` (ta ostatnia wyłącznie dla
   `ZapasowyLlmAdapter` w `Lemat.Modele`); `DECYZYJNY_GLOWNY` →
   `ArgumentException` (Jev nie jest LLM). Port zastępuje metody
   `GenerujJsonAsync<T>` i `GenerujTekstAsync` z wersji 3.0; ich logika
   (schemat JSON, walidacja, jedna próba naprawy) jest w
   `Lemat.Agenci.Wspolne.WywolanieCzatu` (10.0.3).
2. **Zależności.** `Lemat.Application` dostaje pakiet
   `Microsoft.Extensions.AI.Abstractions` (abstrakcja tej samej klasy co
   `Microsoft.Extensions.Logging.Abstractions`: `IChatClient`,
   `ChatMessage`, `ChatOptions`, `AIFunction`, `AIJsonUtilities`, bez
   implementacji ani SDK dostawców). `BannedSymbols.txt` zabrania w
   `Lemat.Application` i `Lemat.Agenci` konstrukcji
   `ChatClientBuilder`, `OpenAIClient`, `AzureOpenAIClient` i metod
   `AsIChatClient`; NetArchTest `AgentNieRozwiazujeKlientaCzatuZDi`
   sprawdza, że żaden typ w `Lemat.Agenci` nie ma parametru konstruktora
   typu `IChatClient`, `IChatCompletionService` ani `Kernel` — klient
   pochodzi wyłącznie z `IModelGeneratywny.DlaRoliAsync`.
3. **Potok klienta czatu** (`Lemat.Modele.Llm.FabrykaKlientowCzatu :
   IModelGeneratywny`), od zewnątrz:
   `ChatClientBuilder(adapterDostawcy)`
   `.UseOpenTelemetry(sourceName: "Lemat.Modele", configure: c => c.EnableSensitiveData = false)`
   `.Use(new LimitCzasuChatClient(parametry.TimeoutS))`
   `.Use(new KosztIZuzycieChatClient(konfiguracja, metryki))`
   `.Use(new OgranicznikChatClient(ogranicznik, konfiguracja, pula))` (wyłącznie `ROZPOZNAWANIE_PISMA`)
   `.Use(new KlasyfikacjaBledowChatClient())` (mapuje wyjątki SDK na
   `BladModeluPrzejsciowyException` / `BladModeluTrwalyException`, 13.1)
   `.UseFunctionInvocation(configure: f => { f.MaximumIterationsPerRequest = parametry.MaksIteracjiFunkcji ?? 1; f.AllowConcurrentInvocation = false; f.MaximumConsecutiveErrorsPerRequest = 1; })`
   `.Build()`. Ponowienia błędów przejściowych wykonuje warstwa agenta
   według terminów zadania (10.0.4), nie potok — potok nie ma polityki
   Polly, żeby jedno wywołanie nie przekroczyło terminu pracy.
4. **Semantic Kernel** (`Microsoft.SemanticKernel.Core`,
   `Microsoft.SemanticKernel.Yaml`; bez `PromptTemplates.Handlebars`)
   wyłącznie w `Lemat.Agenci`, w dwóch rolach:
   - **pluginy**: klasy z metodami `[KernelFunction]` (10.2.4) tworzone
     przez `KernelPluginFactory.CreateFromObject(...)`; funkcje są
     przekazywane do `ChatOptions.Tools` jako `AIFunction` (`KernelFunction`
     dziedziczy `AIFunction`) po związaniu z lekkim `Kernel` agenta
     (`funkcja.WithKernel(kernel)`); pętlę wywołań wykonuje
     `FunctionInvokingChatClient` z potoku p. 3, a każda funkcja jest
     opakowana `FunkcjaZLimitem : DelegatingAIFunction`, która liczy
     wywołania wspólnym `LicznikWywolanFunkcji` i przy wywołaniu nr
     `limit + 1` rzuca `BladModeluTrwalyException("LIMIT_WYWOLAN_FUNKCJI")`
     (limit Agenta generującego = 8); `MaximumIterationsPerRequest = 8` jest
     drugą linią obrony. `FunctionChoiceBehavior` i `Kernel.InvokeAsync` nie
     są używane;
   - **szablony promptów systemowych**: pliki
     `Prompty/<ROLA_MODELU>/<wersja_promptu>.yaml`
     (`KernelFunctionYaml.ToPromptTemplateConfig`, format
     `semantic-kernel`, składnia `{{$zmienna}}`) renderowane
     `KernelPromptTemplateFactory` do **tekstu wiadomości systemowej**.
     Zmienne szablonu są wyłącznie wartościami systemowymi z zamkniętej
     listy typów (`int`, `decimal`, wartości enumów, nazwy Umiejętności i
     Działów z bazy referencyjnej); treść od Ucznia, Nauczyciela albo
     modelu (Transkrypcja, treść Zadania, Poprawki, Rozwiązanie wzorcowe)
     **nigdy** nie przechodzi przez szablon SK — trafia do wiadomości
     `user` budowanej w C# przez `BudowniczyWiadomosci` (10.0.3) z
     `JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }`,
     więc `<`, `>`, `&` docierają do modelu bajt w bajt. Wynik renderowania
     nie jest parsowany na role (`<message role=…>`), bo nie przechodzi
     przez `Kernel.InvokePromptAsync`.
5. **Stan dla Modelu decyzyjnego** (10.4.2) i stan Q-RE-01 (10.1.6) buduje
   zwykły kod C# (`BudowniczyStanuOceny`, `BudowniczyStanuRekomendacji`) bez
   Semantic Kernel; wersja formatu stanu jest stałą klasy
   (`WersjaStanu = "stan-oceny-v1"`) zapisywaną w
   `konfiguracja_modelu.wersja_promptu` ról decyzyjnych.
6. **Ślad wywołań narzędzi (NFR-05)** — każde wywołanie funkcji pluginu
   (nazwa, argumenty jako SHA-256 i postać jawna ≤ 2 000 znaków, rezultat,
   czas) zwraca agent w `ZadanieWygenerowane.WywolaniaNarzedzi`, a handler
   `GENERUJ_ZADANIE` dopisuje je do `zlecenie_generowania.wywolania_narzedzi`
   (`jsonb`, z numerem cyklu) w tej samej jednostce pracy co wersję
   Zadania.
7. **Testy:** `Lemat.Agenci.Tests/Generowanie/LimitWywolanFunkcjiTests`
   (`MockChatClient` żąda 9 wywołań `sprawdz_rownowaznosc` → 9. kończy się
   `BladModeluTrwalyException`, zlecenie z przyczyną
   `LIMIT_WYWOLAN_FUNKCJI`); `ZgodnoscBajtowaLatexTests` (krok
   `x<2 \land y>3`, `\begin{cases} x & y \end{cases}` — tekst wiadomości
   `user` i `decyzja_modelu.stan` zawierają te same bajty co
   `krok_transkrypcji.latex`); `KazdaWersjaPromptuIstnieje`;
   `SzablonBezZmiennychTresci` (analiza plików YAML: każda zmienna ma typ z
   listy p. 4).

## Alternatywy

- **`IModelGeneratywny.GenerujZNarzedziamiAsync<T>(Prompt, IReadOnlyList<AIFunction>, …)`
  z pętlą w `Lemat.Modele`** — odrzucona: dwa porty dla jednej roli
  (z narzędziami i bez), a agent i tak potrzebuje kontroli nad
  wiadomościami (naprawa odpowiedzi, dopisek strażnika wycieku 10.5.3).
- **`Kernel` z `IChatCompletionService` w agencie i
  `FunctionChoiceBehavior.Auto`** — odrzucona: `FunctionChoiceBehavior` nie
  ma limitu liczby wywołań, a `Kernel.InvokePromptAsync` parsuje znaczniki
  ról i koduje zmienne (C6-10 p. 3).
- **Szablony Handlebars** — odrzucona: dodatkowy pakiet bez potrzeby, bo
  szablony nie mają pętli ani warunków po usunięciu treści użytkownika ze
  zmiennych.

## Konsekwencje

- Wybór dostawcy, modelu, wersji promptu, `timeout_s`, retencji (A-07) i
  zakaz `mock` w `Production` działają dla wszystkich ról generatywnych,
  także dla pętli wywołań funkcji.
- `Lemat.Application` zależy od jednego pakietu abstrakcji Microsoft.Extensions.AI;
  domena pozostaje bez pakietów.
- Treść matematyczna nie jest przetwarzana przez silnik szablonów, więc stan
  w `decyzja_modelu` i wiadomość do modelu odpowiadają temu, co widzi
  Nauczyciel.

## Uzupełnienia

**Uzupełnienie wersji 3.2 (C7-01, C7-02, C7-08, C7-09, C7-10, C7-14,
C7-15 p. 5–7).** Każdy punkt sprawdzono w projekcie .NET 10 z
`Microsoft.Extensions.AI` 10.11.0, `Microsoft.SemanticKernel.Core` 1.81.0
i fałszywym `IChatClient`.

1. **Limit wywołań funkcji (zmienia p. 3 i p. 4).** Obowiązuje wariant
   „jeden strażnik z wyjątkiem”: `FunctionInvokingChatClient` z
   `MaximumIterationsPerRequest = limit + 1` (dla `GENERATYWNY_ZADANIA`:
   9), `MaximumConsecutiveErrorsPerRequest = 0`,
   `AllowConcurrentInvocation = false`, `TerminateOnUnknownCalls = true`;
   `FunkcjaZLimitem` rzuca `BladModeluTrwalyException("LIMIT_WYWOLAN_FUNKCJI")`
   przy wywołaniu nr `limit + 1`. Zmierzono: model żądający wywołań bez
   końca → 8 wykonań, 9. żądanie wywołania kończy pętlę wyjątkiem
   `LIMIT_WYWOLAN_FUNKCJI` (9 żądań do modelu); model żądający dokładnie 8
   wywołań → 8 wykonań i odpowiedź końcowa w 9. żądaniu. Przy
   `MaximumConsecutiveErrorsPerRequest = 1` wyjątek trafiałby do modelu
   jako rezultat funkcji, dlatego wartość 0 jest obowiązkowa (test
   `KonfiguracjaPotokuGenerowania`).
2. **Nazwy narzędzi.** Plugin jest rejestrowany pod nazwą `sympy`, więc
   model widzi `AIFunction` o nazwach **`sympy_sprawdz_rownowaznosc`** i
   **`sympy_postac_liniowa`** (`FullyQualifiedAIFunction`, zmierzone). Te
   nazwy obowiązują wszędzie: prompt `zadanie-v1`, dane Mock, ślad
   `WywolanieNarzedzia.Funkcja`, `zlecenie_generowania.wywolania_narzedzi`,
   metryka `wywolania_funkcji_total{funkcja}`, spany `execute_tool`. Test
   `NazwyNarzedziZgodneZPromptemIMock` porównuje `ChatOptions.Tools[*].Name`
   z listą narzędzi w YAML promptu i z plikami scenariuszy Mock.
   **Nieznane narzędzie:** przy `TerminateOnUnknownCalls = true` pętla
   kończy się odpowiedzią zawierającą `FunctionCallContent` z nieznaną
   nazwą (zmierzono: 0 wykonań, 1 żądanie); `WywolanieCzatu` wykrywa
   `FunctionCallContent` w odpowiedzi końcowej i rzuca
   `BladModeluTrwalyException("NIEZNANE_NARZEDZIE")`.
3. **Potok (zmienia p. 3), od zewnątrz, w postaci kompilowalnej:**
   ```csharp
   new ChatClientBuilder(adapterDostawcy)
       .UseFunctionInvocation(configure: f => { /* p. 1; dla ról bez narzędzi MaximumIterationsPerRequest = 1 */ })
       .UseOpenTelemetry(sourceName: "Lemat.Modele", configure: c => c.EnableSensitiveData = false)
       .Use(w => new KlasyfikacjaBledowChatClient(w))                                  // per żądanie HTTP
       .Use(w => new LimitCzasuChatClient(w, TimeSpan.FromSeconds((double)parametry.TimeoutS)))
       .Use(w => new KosztIZuzycieChatClient(w, konfiguracja, metryki))
       .Use(w => rola == RolaModelu.RozpoznawaniePisma ? new OgranicznikChatClient(w, ogranicznik, konfiguracja, pula) : w)
       .Build();
   ```
   `UseFunctionInvocation` jest najbardziej zewnętrzne (pętla agenta), a
   telemetria, klasyfikacja błędów, limit czasu, koszt i ogranicznik
   działają **per żądanie do dostawcy**. Zmierzono z `ActivityListener`:
   generowanie z 2 wywołaniami daje 3 spany `chat {model}`, 2 spany
   `execute_tool sympy_sprawdz_rownowaznosc` i 1 span `orchestrate_tools`,
   a `KosztIZuzycieChatClient` rejestruje 3 żądania. Termin całego
   wywołania agenta (10.0.4) ustala `WywolanieCzatu` własnym
   `CancellationTokenSource`; `KlasyfikacjaBledowChatClient` rozpoznaje
   przekroczenie limitu per żądanie po tym, że jego token zewnętrzny nie
   jest anulowany (`!ct.IsCancellationRequested`) → `BladModeluPrzejsciowyException("PRZEKROCZONY_CZAS")`;
   anulowanie z zewnątrz (termin agenta, zatrzymanie) przechodzi dalej jako
   `OperationCanceledException` i klasyfikuje je pętla pracy (ADR-066 p. 1).
4. **Budżet czasu generowania (C7-01 p. 4).** `timeout_s` roli
   `GENERATYWNY_ZADANIA` = 45 s **na żądanie**, termin całego generowania =
   150 s (≤ 9 żądań; typowo 3–4: pomiar referencyjny bramki G-3 (e)),
   dzierżawa pracy odnawiana co 15 s, więc długość nie zagraża dzierżawie.
   Po przekroczeniu terminu 150 s → `Ponow`, po 3 próbach zlecenie
   `NIEUDANE` z przyczyną `PRZEKROCZONY_CZAS`.
5. **Model zapasowy dla Walidacji (C7-08).** `ZapasowyLlmAdapter.WykonajPlanAsync(string stan,
   PlanPytan plan, TerminPlanu termin, KontekstDecyzji kontekst, CancellationToken ct)`;
   prompt wybierany po kontekście: `Ocena` → `plan-pytan-v1`, `Walidacja` →
   `plan-pytan-walidacji-v1` (`docs/spec/prompty.md` 4.9, bez Q-OC-04, ze
   stanem `stan-walidacji-v1`); `Rekomendacja` → `ArgumentException`
   (Q-RE-01 nie ma modelu zapasowego, ADR-023). Logika ustrukturyzowanego
   wyjścia (schemat, walidacja, jedna próba naprawy) jest **portem**
   `IWywolanieUstrukturyzowane` (`Lemat.Application.Modele`) z jedną
   implementacją `WywolanieUstrukturyzowane` w `Lemat.Modele.Llm` (pakiet
   `JsonSchema.Net` w `Lemat.Modele`); `Lemat.Agenci.Wspolne.WywolanieCzatu`
   jest cienką nakładką (budowa wiadomości, termin agenta, wykrycie
   nieznanego narzędzia) wywołującą port. Test
   `WalidacjaPrzyObwodzieOtwartym` (Jev niedostępny, obwód otwarty →
   plan Q-WA przez `plan-pytan-walidacji-v1`, Zadanie opublikowane).
6. **Schematy odpowiedzi — jedno źródło (C7-09).** Schematy są
   **generowane** z rekordów C# (`Lemat.Modele.Schematy.<Rekord>`, atrybuty
   `[Range]`, `[StringLength]`, `[MinLength]`, `[MaxLength]`,
   `[RegularExpression]`) przez `AIJsonUtilities.CreateJsonSchema(typ,
   serializerOptions: BudowniczyWiadomosci.Json, inferenceOptions: new() {
   TransformOptions = new() { DisallowAdditionalProperties = true,
   RequireAllProperties = true } })`; wynik (zmierzony: `additionalProperties:
   false` i komplet `required` na każdym obiekcie, `minimum`/`maximum`/`pattern`/`minItems`/`maxItems`
   z atrybutów, typy powtarzalne rozwinięte inline, bez `$defs`) jest
   zapisywany do `Lemat.Modele/Schematy/*.json` (migawka w repozytorium,
   test `SchematyOdpowiedziBezDryfu`). JSON w `docs/spec/prompty.md` 4 jest
   opisem tej postaci. Mapy dynamiczne zastąpiono tablicami par:
   `rozklad: [{ opcja, p }]` (model zapasowy) i `stwierdzenia: [{ id,
   prawda }]` (W-3); komplet opcji i suma prawdopodobieństw są sprawdzane w
   kodzie. **Tryb ścisły:** adaptery `azure-openai` i `openai-zgodny`
   ustawiają `ChatOptions.AdditionalProperties["strict"] = true`
   (sprawdzone na serwerze testowym: żądanie zawiera
   `response_format.type = "json_schema"`, `json_schema.strict = true`,
   `schema.additionalProperties = false`); `mock` waliduje schematem
   lokalnie. Test kontraktu `KontraktWywolaniaOpenAiTests` (WireMock.Net:
   nagrane żądanie każdej roli — `model`, `temperature`,
   `max_completion_tokens`, `response_format`, `tools`) chroni przed zmianą
   zachowania przy aktualizacji `OpenAI` 2.14 / `Azure.AI.OpenAI` 2.1
   (C7-15 p. 6).
7. **Czas życia i współbieżność (C7-10).** `FabrykaKlientowCzatu` i
   `CircuitBreakerModeluDecyzyjnego` są `Singleton`; aktywne Konfiguracje
   modelu trzyma `PamiecKonfiguracjiModeli` (Singleton) odświeżana przez
   `NOTIFY konfiguracja_modelu` i co 30 s w `OdswiezanieKonfiguracjiHostedService`
   (odczyt przez `NpgsqlDataSource`, nie przez `DbContext`); `DlaRoliAsync`
   nie wykonuje zapytań na ścieżce wywołania (pierwsze wypełnienie przy
   starcie procesu, przed gotowością health check). Porty wywoływane z
   agentów (`IPodobienstwoZadan` dla W-9, `IOgranicznikSzybkosci`,
   repozytoria odczytu agentów) używają `NpgsqlDataSource` albo
   `IDbContextFactory<LematDbContext>` z kontekstem na wywołanie. Test
   architektury `AgenciIPortyAgentowNieZalezaOdLematDbContext` (NetArchTest:
   typy `Lemat.Agenci` i implementacje portów z listy `PortyWywolywaneZAgentow`
   nie mają zależności konstruktora od `LematDbContext`) i test
   `WalidacjaRownoleglaBezBledowKontekstu` (100 Walidacji z `Task.WhenAll`
   W-2/W-3/Q-WA/W-9 i wygasłą pamięcią konfiguracji).
8. **Szablony SK bez kodowania HTML (C7-14).** Zmierzono w 1.81.0:
   `PromptTemplateConfig.AllowDangerouslySetContent = true` **nie**
   wyłącza kodowania (`<` → `&lt;`, `ó` → `&#243;`), działa
   `new KernelPromptTemplateFactory { AllowDangerouslySetContent = true }`
   (albo `InputVariable.AllowDangerouslySetContent`). `SzablonyPromptow`
   używa fabryki z tą flagą (bezpieczne: zmienne są wyłącznie systemowe,
   p. 4); test `RenderowanieNazwUmiejetnosci` renderuje każdy szablon dla
   wszystkich nazw z `umiejetnosci.yaml` i porównuje bajty zmiennej w
   tekście wynikowym z nazwą w YAML.
9. **Rejestr adapterów dla modelu zapasowego (C7-15 p. 7).**
   `FabrykaKlientowCzatu.DlaRoliAsync(DecyzyjnyZapasowy)` buduje adapter z
   `parametry.dostawca_llm` (`azure-openai`, `openai-zgodny`, `mock`), a nie
   z `dostawca = zapasowy-llm`; `KlientCzatuRoli.Dostawca` = `dostawca_llm`
   (atrybut `gen_ai.system`), a `lemat.dostawca_konfiguracji = zapasowy-llm`.
10. **Aliasy (C7-15 p. 5)** — ADR-049 uzup. 3.2 p. 13.
