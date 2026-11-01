# ADR-058: Port `IModelGeneratywny` jako fabryka klientów czatu per rola; Semantic Kernel tylko do pluginów i szablonów promptów systemowych

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.1, 2026-11-01) |
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
