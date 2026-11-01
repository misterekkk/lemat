# Lemat — Changelog architektury

| Pole | Wartość |
|---|---|
| Dokument | `docs/ARCHITECTURE_CHANGELOG.md` |
| Dotyczy | [`docs/ARCHITECTURE_DETAILED.md`](ARCHITECTURE_DETAILED.md) |
| Zawartość | historia iteracji pętli architekt → krytyk (wersje 1.0 … 3.1) i mapowanie uwag krytyka na miejsca rozwiązania; przeniesione z sekcji 23 dokumentu głównego w wersji 3.0 |

Numery sekcji w tabelach mapowania odnoszą się do wersji dokumentu
głównego obowiązującej w danej iteracji. Od wersji 3.0 rejestr ADR jest w
`docs/adr/`, a decyzje właściciela produktu D-1 … D-17 pozostają w sekcji
23.4 dokumentu głównego.

## Pętla 3.x, iteracja 2 (2026-11-01, wersja 3.1) — agenci AI w .NET, model danych EF Core, porty i prace w tle; zamknięcie uwag C6

Kontekst: iteracja 2 z 4 pętli „.NET 10 + Next.js” (ADR-048). Zakres:
dokładna architektura agentów AI w .NET (Microsoft.Extensions.AI, Semantic
Kernel), schemat danych z mapowaniem EF Core, porty C# i prace w tle oraz
wszystkie 15 uwag iteracji 6 krytyka. Reguły domenowe wersji 2.4 i zasada
Z-1 bez zmian.

**Zmiany struktury dokumentów:**

- Nowy katalog `docs/spec/`: `ddl.md` (pełne DDL przeniesione z 7.3 z
  poprawkami 3.1 i pełną treścią PL/pgSQL triggerów i funkcji),
  `ef-core.md` (`LematDbContext`, konwencje, konwersje, współbieżność,
  konfiguracje agregatów, pokrycie 58 tabel, strategia migracji),
  `prompty.md` (8 promptów systemowych, 3 szablony stanów, schematy JSON
  odpowiedzi, teksty pytań, dane Mock).
- Nowe ADR: ADR-056 (jednostka pracy, zlecanie SQL `ON CONFLICT`,
  `NOTIFY` z triggerów), ADR-057 (sesja w wielu kartach, okno tolerancji
  rotacji, jeden strumień powiadomień), ADR-058 (`IModelGeneratywny` jako
  fabryka klientów czatu per rola, SK tylko do pluginów i promptów
  systemowych), ADR-059 (budżet połączeń PostgreSQL bez PgBouncera).
- Uzupełnienia wersji 3.1: ADR-020, ADR-023, ADR-049 (p. 11, 12), ADR-051,
  ADR-052, ADR-053, ADR-054, ADR-055.

**Zmiany treści dokumentu głównego:**

- Sekcja 7 przepisana: ERD, konwencje (UUIDv7, `version`/`xmin`, nazwy),
  7.3 jako lista zmian i odnośnik do `docs/spec/ddl.md`, 7.5.5
  (`wywolania_narzedzi`), nowe 7.6 (decyzje mapowania EF Core) i 7.7
  (strategia migracji); notka „do przepisania” usunięta.
- Sekcja 10 przepisana: nowe 10.0 (komponenty, kontrakty wspólne,
  `WywolanieCzatu`/`BudowniczyWiadomosci`/`SzablonyPromptow`, czas,
  ponowienia, błędy i model zapasowy per agent, telemetria OpenTelemetry
  GenAI, implementacje Mock, rejestracja i testy); kontrakty agentów jako
  interfejsy i rekordy C#; nowe 10.1.8, 10.2.4 (plugin
  `SprawdzanieSymbolicznePlugin` z `[KernelFunction]`), 10.2.5
  (orkiestracja generowania i Walidacji), 10.3.4, 10.4.6, 10.5.4
  (realizacja .NET każdego agenta); plan pytań 10.4.4 w C#; notka
  usunięta.
- Sekcja 13 przepisana: porty C# 13.1 (w tym `IModelGeneratywny`,
  `IObrobkaObrazow`, wyjątki), klasy adapterów 13.2, kontrakty prac w tle
  13.4.1 (`IZlecaniePrac`, `IJednostkaPracy`, `IHandlerPracy`,
  `PulaPracHostedService`), SQL sklepu prac 13.4.2 (warunek dzierżawy przy
  zapisie wyniku, `ON CONFLICT`), harmonogram 13.5 (`HarmonogramHostedService`,
  wyrażenia cron), zdarzenia 13.6 (`IObslugaZdarzenia<T>`); notka usunięta.
- 3.6: pełne szkielety `Uzytkownik`, `Nauczyciel`, `Rozwiazanie`,
  `Weryfikacja`, `SciezkaNauki`, `StanBezpiecznika`; strażnik w
  `ProfilUcznia.Zastosuj`; fabryki `Ocena` z typem domenowym `RezultatOceny`.
- 5.2–5.4, 6.1–6.9, 9.2, 12.3 (zatwierdzenie, adres zdjęcia), 12.4
  (`SesjaWyjscie.flagi`), 15.2 (C-41 … C-46), 16.1, 17.4, 18.4 (e)–(k),
  19 (indeks), 21 (R-26, R-27), 22 (parametry 3.1), nagłówek i konwencja
  odczytu.

**Mapowanie uwag iteracji 6 → miejsce rozwiązania:**

| Uwaga | Rozwiązanie | Miejsca |
|---|---|---|
| C6-01 (WYSOKA) | `IJednostkaPracy` z jawną transakcją (`BeginTransactionAsync` … `CommitAsync`, bez `EnableRetryOnFailure`); `IZlecaniePrac.ZlecAsync` = `INSERT … ON CONFLICT … DO NOTHING / DO UPDATE SET ladunek … RETURNING id, (xmax = 0)` na transakcji jednostki pracy (konflikt nie wycofuje przypadku użycia); `NOTIFY` wyłącznie z triggerów `trg_*_powiadom` (dostarczane po `COMMIT`, nigdy po `ROLLBACK`); `ZlecaniePracInterceptor` usunięty; testy (a)–(f) w `Lemat.Infrastructure.Tests` | ADR-056, ADR-053 (uzup. 3.1), ADR-051 (uzup. 3.1), 6.3, 13.4, 13.6, 15.2 (C-46), `docs/spec/ddl.md` blok B, `docs/spec/ef-core.md` 1, 5 |
| C6-02 (WYSOKA) | Klient: odświeżanie single-flight w sekcji krytycznej Web Locks `lemat-odswiezenie`, rozgłaszanie tokenu `BroadcastChannel("lemat-sesja")`, prośba o token nowej karty (150 ms), odświeżenie proaktywne 60 s przed `exp`; serwer: okno tolerancji 30 s dla tokenu zastąpionego (ten sam `skrot_klienta_http`, ≤ 5 użyć) — nowy access token bez `Set-Cookie`, poza oknem unieważnienie rodziny; kolumny `zastapiony_przez`, `zastapiony_at`, `skrot_klienta_http`, `uzycia_w_oknie_tolerancji`; testy Playwright i integracyjne; scenariusz C-41 | ADR-057, ADR-020 (uzup. 3.1), 6.8, 15.2 (C-41), 16.1, 21 (R-26), 22, `docs/spec/ddl.md` (`token_odswiezania`), `docs/spec/ef-core.md` 6.1 |
| C6-03 (WYSOKA) | Port `IModelGeneratywny.DlaRoliAsync(RolaModelu) → KlientCzatuRoli` (fabryka klientów czatu per rola z konfiguracją, wersją promptu, parametrami i potokiem `ChatClientBuilder`); `Microsoft.Extensions.AI.Abstractions` dozwolony w `Lemat.Application`; pętla wywołań funkcji w `FunctionInvokingChatClient` potoku; ślad wywołań narzędzi w `zlecenie_generowania.wywolania_narzedzi`; test `AgentNieRozwiazujeKlientaCzatuZDi` | ADR-058, ADR-055 (uzup. 3.1), 6.4, 6.5, 6.6, 7.5.5, 10.0, 10.2.4, 13.1, 3.5 #24 |
| C6-04 (WYSOKA) | Komplet przejść 8.1 w `Rozwiazanie` (`RozpocznijRozpoznawanie`, `ZapiszTranskrypcjeAgenta`, `ZapiszBladRozpoznawania`, `ZapiszPoprawke`, `RozpocznijOcenianie`, `ZarejestrujT6`, `ZakonczPoWeryfikacji`, `Porzuc`, `Anuluj`); `PonowRozpoznanie` zgłasza `RozwiazaniePrzeslane`; `PrzeslanoAt` wyłącznie przy Przesłaniu, nowe `OstatnieRozpoznanieOdAt` (A-11 liczy błąd dostawcy jako naruszenie); `Zatwierdz(transkrypcja, wersjaTranskrypcji, oczekiwanaWersjaRozwiazania)` z `409 TRANSKRYPCJA_NIEAKTUALNA` i `412`; `KontekstProby` i `ZadanieDoProby` (rodzaj z wersji Zadania); kolumny `najnowsza_transkrypcja_id`, `najnowsza_wersja_transkrypcji`, `ostatnie_rozpoznanie_od_at`; test przejść (34 przypadki) i scenariusz C-43 | 3.6, 7.3, 12.3, 12.5, 13.3, 15.2 (C-43), 18.4 (j), `docs/spec/ddl.md` |
| C6-05 (ŚREDNIA) | Fabryka `Weryfikacja.Utworz` (powody początkowe, priorytet z 11.3, przydział 11.4.1, termin SLA); `KlasaId`; `Podejmij(nauczyciel, UprawnienieDoPodjecia, teraz)` z regułą uprawnień 11.4.2 i zachowaniem `PodjetaAt` przy ponownym podjęciu przez właściciela także po wygaśnięciu (limit 2 h); `Zwolnij`, `ZwolnijWygasla`, `DolaczOcene`, `Anuluj`, `CofnijEskalacjeKarencji`; strażnik stanu w `DopiszZgloszenie`; `Eskaluj` w `W_TRAKCIE`: przy ważnej blokadzie odroczenie (chyba że eskalacja dotyczy właściciela blokady), przy wygasłej — zwolnienie i eskalacja; `NauczycielDocelowyId` pozostaje po eskalacji | 3.6, `docs/spec/ef-core.md` 6.4 |
| C6-06 (ŚREDNIA) | `OkresKarencji.StatusPrzedWnioskiem` (kolumna `status_przed_wnioskiem`) odtwarzany przez `Przywroc`; `Zatwierdz`, `Zablokuj`, `Odblokuj` w `Uzytkownik`; `NadajWeryfikatoraGlobalnego(deklarowane, statusKonta, prowadziKlase, prognoza, umowaPoufnosciAt)` z limitem 180 min wyłącznie dla prowadzącego Klasę; kolumna i CHECK `umowa_poufnosci_at`; `OdbierzWeryfikatoraGlobalnego`, `NadajRecenzentaTresci`, `OdbierzRecenzentaTresci` | 3.5 #73, #101, 3.6, `docs/spec/ddl.md` |
| C6-07 (ŚREDNIA) | Tryb synchroniczny w `IRekomendacjaSynchroniczna` (`Lemat.Application.Orkiestracja.Synchroniczna`), zależnej tylko od `IAgentRekomendujacy` i fasad odczytu; zapis w `NastepnyElementHandler` modułu Rekomendacje; `IZlecaniePrac` i typy prac przeniesione do `Lemat.Application.Wspolne.PraceWTle`; testy `BrakCykliMiedzyModulamiAplikacji` i `KontenerDiTests` (`ValidateOnBuild`, `ValidateScopes`) | ADR-023 (uzup. 3.1), 6.1, 6.2 reguła 3, 6.3, 6.4, 6.5, 6.7, 9.2, 3.5 #67, #95 |
| C6-08 (ŚREDNIA) | `StanBezpiecznika : Agregat<KluczBezpiecznika>` z kolumną `version`; `Utworz` (tylko `OTWARTE_OBLICZENIOWE`), `Wyzwol` z `OstatnieWyzwolenieAt`, `WyjdzZBlokady`, strażnik w `Dezaktywuj`, `DezaktywowanyPo`; `KALIBRUJ` zapisuje z tokenem wersji, przy konflikcie ponowna ocena (maks. 3), dezaktywacja w trakcie przebiegu wygrywa; dezaktywacja z `If-Match`; scenariusz C-44 | 3.5 #104, #110, 3.6, 11.5.3 (krok 6), 13.3 (`KALIBRUJ`), 15.2 (C-44), `docs/spec/ddl.md`, `docs/spec/ef-core.md` 6.5 |
| C6-09 (ŚREDNIA) | Jeden strumień powiadomień na przeglądarkę (karta-lider Web Locks `lemat-powiadomienia-lider` + `BroadcastChannel`); limity per replika ≤ 4 na (użytkownik, zasób) i ≤ 12 na użytkownika z `429 ZA_DUZO_STRUMIENI` bez zamykania istniejących; `id:` powiadomienia = UUIDv7 `powiadomienie.id`; jawna lista triggerów `NOTIFY lemat_zdarzenia` (rozwiązanie, ocena, weryfikacja, powiadomienie, import, zlecenie); `/api/*` poza service workerem; scenariusz C-42 | ADR-057, ADR-056 p. 3, ADR-051 (uzup. 3.1), ADR-054 (uzup. 3.1), 5.2, 6.8, 6.9, 15.2 (C-42), 22 |
| C6-10 (ŚREDNIA) | Pakiety `Microsoft.SemanticKernel.Yaml` (bez Handlebars) i `JsonSchema.Net`; limit wywołań funkcji przez `FunkcjaZLimitem : DelegatingAIFunction` (9. wywołanie → `BladModeluTrwalyException("LIMIT_WYWOLAN_FUNKCJI")`) + `MaximumIterationsPerRequest = 8`; szablony SK wyłącznie dla promptów systemowych ze zmiennymi systemowymi, treść użytkownika w wiadomości `user` z `UnsafeRelaxedJsonEscaping`; stan Modelu decyzyjnego budowany w C# (`BudowniczyStanuOceny`); testy `LimitWywolanFunkcjiTests`, `ZgodnoscBajtowaLatexTests`, `StanOcenyZgodnyBajtowo` | ADR-058 p. 4–5, 7, 5.2, 6.4, 6.6, 10.0.3, 10.2.4, `docs/spec/prompty.md` 1, 3 |
| C6-11 (ŚREDNIA) | Nadzorca + procesy robocze w `Lemat.ImageSandbox` zamiast `Environment.FailFast` (zabijany tylko proces przekraczający 8 s, `504`); `tmpfs /tmp`, `ResourceLimits.Disk`, `Memory = 384 MiB`, `Thread = 1`, `SetTempDirectory`; limity kontenerów 1,5 / 3 GiB; polityka bezpieczeństwa ustawiana w kodzie (`MagickNET.Initialize` z `ConfigurationFiles`), test `KoderSpozaListyJestOdrzucany`; ładowanie biblioteki natywnej z `runtimes/` i test startowy z `read_only`; `429 LIMIT_NORMALIZACJI` po 3 przekroczeniach w 10 min | ADR-052 (uzup. 3.1), 5.3, 5.4, 6.7, 13.1 (`IObrobkaObrazow`), 22 |
| C6-12 (NISKA) | p. 1: `ORYGINALNA`/`MASKOWANA` w 3.5 i 12.3; p. 2: 3.2 — Kalibracja w dwóch modułach, zasób `/nauczyciele`, `Zadanie.CzyBazowe`/`CzyWygenerowane`; p. 3: `IModelDecyzyjny` w 3.3 i A-02; p. 4: `WynikZadaniaSkrot`, `ZdjecieRef` jako typy wewnętrzne C#, TS `WynikZadaniaWyjscie`, `ZdjecieRozwiazaniaWyjscie`, `AdresZdjeciaWyjscie`, `ObszarNaZdjeciu` na liście nowych nazw; p. 5: `KontekstDecyzji` przy #30; p. 6: 16.1 — ES256; p. 7: `Lemat.Symulacje` → `Lemat.Infrastructure`, pakiety `Microsoft.ML`, `Microsoft.ML.LightGbm`; p. 8: ADR-049 p. 11 i test `BrakCzesciowejKwalifikacjiPrzestrzeni` | 3.2, 3.3, 3.5, 6.4, 12.3, 16.1, ADR-049 (uzup. 3.1) |
| C6-13 (NISKA) | Trzecie znaczenie „blokady” — blokada konta (`status_konta = 'ZABLOKOWANE'`, `StatusKonta.Zablokowane`, `Uzytkownik.Zablokuj`, `KontoZablokowane`, `NAUCZYCIEL_DOCELOWY_ZABLOKOWANY`) z kwalifikatorem w prozie; przemianowanie odrzucone z powodu kolizji z Zawieszeniem Wyniku zadania | 3.1 p. 5, 3.4 część 3, 3.6, ADR-049 p. 12 |
| C6-14 (ŚREDNIA) | Trasy `(administrator)/admin/zadania/**`, `zgloszenia-bledu-zadan`, `zlecenia-generowania/**`; `(nauczyciel)/nauczyciel/wkladki-teoretyczne/**` ze `StraznikFlagi` (`SesjaWyjscie.uzytkownik.flagi`); `(uczen)/klasy`; przepływ `dolacz/[kod]` dla zalogowanego, niezalogowanego, innej roli i kodu nieważnego; zdjęcia przez `GET …/zdjecia/{id}/adres` (JSON z podpisanym URL) i `<img src>` bez `blob:` i CORS, testy Vitest i E2E; `next build --webpack` dla Serwist | ADR-054 (uzup. 3.1), 5.2, 6.8, 6.9, 12.3, 12.4 |
| C6-15 (NISKA) | p. 1: zapis wyniku pracy z `wykonawca = @ja AND status = 'W_TOKU'` + potwierdzenie dzierżawy `FOR UPDATE` w jednostce pracy (C-45); p. 2: budżet 155 połączeń, `max_connections ≥ 200`, PgBouncer dopiero powyżej 3 replik `worker` (tryb `transaction`, połączenia `LISTEN`/blokady doradczej bezpośrednio); p. 3: strażnik `UniewaznionyAt` w `ProfilUcznia.Zastosuj`; p. 4: `SciezkaNauki` z aktywnymi elementami, `OstatniaKolejnosc` (kolumna `ostatnia_kolejnosc`), wygaszanie w `Zaproponuj`, limit Serii w metodzie; p. 5: `RezultatOceny` w `Lemat.Domain.Ocenianie` | ADR-056 p. 4, ADR-059, 3.6, 5.4, 10.4.1, 13.4, 15.2 (C-45), 17.4, 22, `docs/spec/ef-core.md` 6.5 |

`TASKS.md`, `HANDOFF.md`, `CLAUDE.md`, `docs/CRITIQUE_LOG.md`, wymagania,
słownik i wizja nie były edytowane.

## Pętla 3.x, iteracja 1 (2026-11-01, wersja 3.0) — stos .NET 10 + Next.js, moduły, mapowanie słownika, zamknięcie uwag C5

Kontekst: właściciel projektu zmienił stos na .NET 10 (backend, warstwa
agentów AI) i Next.js (frontend) z komunikacją REST + SSE. Reguły domenowe,
liczby i decyzje biznesowe wersji 2.4 obowiązują bez zmian. Iteracja 1 z 4
nowej pętli: podział na moduły i 100% mapowanie słownika na model domenowy.

**Zmiany struktury dokumentów:**

- Rejestr ADR-001 … ADR-047 przeniesiony bez skrótów do `docs/adr/` (47
  plików `ADR-NNN-<slug>.md` z sekcjami Status, Kontekst, Decyzja,
  Alternatywy, Konsekwencje, Uzupełnienia); sekcja 19 jest indeksem.
- Changelog iteracji 1–6 (dawne 23.1–23.3, 23.5–23.7) przeniesiony do tego
  pliku; w dokumencie głównym zostaje 23.4 (decyzje D-1 … D-17).
- Nowe ADR: ADR-048 (zmiana stosu), ADR-049 (transliteracja i konwencje
  nazw), ADR-050 (sidecar SymPy), ADR-051 (SSE), ADR-052 (Magick.NET w
  `Lemat.ImageSandbox`), ADR-053 (prace w tle w .NET), ADR-054 (frontend
  Next.js), ADR-055 (warstwa agentów AI).
- Statusy: ADR-001, ADR-013, ADR-014, ADR-045 — „Zastąpiony przez ADR-048”
  z wyjaśnieniem, która treść domenowa przechodzi dalej; ADR-002, ADR-003,
  ADR-016, ADR-020, ADR-031 — decyzja obowiązuje, realizacja techniczna
  zaktualizowana przez ADR-048.

**Zmiany treści dokumentu głównego:**

- Sekcja 5 przepisana: diagram kontenerów (Caddy, `web` Next.js, `api`,
  `worker`, `scheduler`, sidecary `obrazy-api`, `obrazy-worker`, `sympy`,
  `migracje`), stos 5.2, obrazy i repliki 5.3, komunikacja 5.4,
  odpowiedniki zależności 2.4 → 3.0 (5.5).
- Sekcja 6 przepisana: komponenty 6.1, moduły i fasady 6.2, Orkiestrator
  w .NET 6.3, solucja i projekty 6.4 (diagram zależności), reguły warstw i
  egzekwowanie 6.5, warstwa agentów AI 6.6, katalogi `backend/` 6.7 i
  `frontend/` z grupami tras 6.8, granice modułów frontendu 6.9,
  mapowanie modułów `lemat.*` → .NET 6.10.
- Nowe 3.5 (111 pojęć: 25 ze słownika + 86 z PZS-1 część A → typ C#,
  tabela/kolumna, typ TS, nazwa JSON) i 3.6 (szkielety agregatów C# z
  niezmiennikami: `Uzytkownik`, `Uczen`, `Nauczyciel`, `Klasa`, `Zadanie` z
  `ZadanieWersja`, `Rozwiazanie`, `Transkrypcja`, `Ocena`, `Weryfikacja`,
  `ProfilUcznia`, `WynikZadania`, `SciezkaNauki` z `ElementSciezki`,
  `ProgPewnosci`, `StanBezpiecznika`).
- 3.1 p. 1 (formy identyfikatorów według ADR-049), p. 5 (znaczenia
  „blokady”, C5-04), nowy p. 7 (słowa techniczne stosu .NET/Next.js); 3.2
  (nazwy .NET); 3.4 (test słownika w `Lemat.Architecture.Tests`, nowe
  źródła identyfikatorów, tokeny zarezerwowane `blokad*`).
- Notki „Do przepisania na .NET w iteracji 2/3 — treść domenowa obowiązuje”
  na początku sekcji 7, 10, 12, 13, 14, 15.
- 18.4: wymagania dla nowego `TASKS.md` (C5-06 … C5-09); 21: R-23 … R-25;
  22: parametry SSE, sidecarów, `LEMAT_HEIC_PO_STRONIE_SERWERA`,
  `LematOpcje`.

**Mapowanie uwag iteracji 5 → miejsce rozwiązania:**

| Uwaga | Rozwiązanie | Miejsca |
|---|---|---|
| C5-01 (ŚREDNIA) | (b1) zastępuje token Słownika tylko przy pełnym drugim członie imienia i nazwiska; inicjał (b1-i) wymaga kropki w oryginale i działa tylko w kroku bez treści matematycznej, w kroku z treścią daje `konflikt_filtra` (T-3) bez zastąpienia; pojedyncza litera bez kropki nie jest inicjałem; token przywrócony przez Ucznia w miejscu znacznika nie jest zastępowany ponownie; G-2: „0 zastąpień w krokach z treścią matematyczną” + zestaw ≥ 20 kartek „Pole P”, „prosta k”, „suma S” i test przywrócenia | 3.3 (Słownik terminów matematycznych), 10.3.1 p. 3a (reguła b, tabela Poprawek), 17.6 (G-2), 21 (R-19), 22 (Filtr), ADR-042 (uzupełnienie 3.0) |
| C5-02 (ŚREDNIA) | Populacja reguł nadzoru = Kontrole wymagalne (`termin_at ≤ now()`) w oknie 30 dni, także dla progu „≥ 100”; `π_wykonanie` bez Kontroli przed terminem; `wykonanie_kontroli_30d = null` przy pustej populacji = brak reguły nadzoru (krok 4 jawnie: `≠ null ∧ < 0,80`); testy (l) wyjście z trybu pełnej weryfikacji 1,000 → 0,980 → … bez bezpiecznika braku nadzoru i (m) Kontrole przed terminem poza mianownikiem | 11.5.3 (tabela reguł nadzoru, `π_wykonanie`, krok 4, testy), ADR-046 (uzupełnienie 3.0) |
| C5-03 (NISKA) | `CHECK (pojemnosc BETWEEN 0 AND 34)`; test (g) z asercją `pojemnosc = 0` po `INSERT` i wierszem ponad limit | 7.3 (`limit_szybkosci_modelu`), 13.2 (test (g)), ADR-039 (uzupełnienie 3.0) |
| C5-04 (NISKA) | „Blokada” — dwa znaczenia domenowe z kwalifikatorem; dzierżawa pracy `praca_w_tle.dzierzawa_do` (DDL, indeks, SQL 13.4, 6.3); `blokad*`/`zablokow*` zarezerwowane z listą dozwolonych użyć i przypadkami kontrolnymi; `blokada` usunięta ze słów ogólnych; proza 11.5.3 z pełną nazwą „Blokada obniżania progu”; `stan_bezpiecznika.aktywny` = stan aktywny bezpiecznika, skutki jako właściwości `BlokadaObnizaniaProgu` / `KazdaPoprawkaDoWeryfikacji` | 3.1 p. 5, 3.4 część 2 i 3, 3.5 (#75, #110), 3.6, 6.3, 7.3, 11.5.3, 13.4, ADR-016 (uzupełnienie 3.0), ADR-049 p. 9, ADR-053 |
| C5-05 p. 1 (NISKA) | Usunięty zdublowany fragment „dla `zadan*` `zadan*`”; wyjątek `zdjecie` zapisany tokenowo (klucz JSON `zdjecie`, `nr_zdjecia`, `{zdjecie_rozwiazania_id}`) | 3.4 część 3 |
| C5-05 p. 2 (NISKA) | Jedna reguła parametrów ścieżek: pierwszy parametr `{id}`, kolejne `{<tabela>_id}`; ujednolicone 12.3 (`{uczen_id}`, `{pozycja_testu_diagnostycznego_id}`, `{zdjecie_rozwiazania_id}`, `/uczniowie/{id}/profil`, `/uczniowie/ja/klasy/{id}`) | ADR-049 p. 5, 12.3, 3.2, 18 |
| C5-05 p. 3 (NISKA) | Akapit „Rozmiar testu”: 31–36% przy oknie napełnianym od zera, do 45% przy pełnym oknie od dnia 1; zakres 31–45% obejmuje oba przypadki | 11.5.3 |
| C5-05 p. 4 (NISKA) | `UZGODNIJ_REJESTR_RODO` (1) przegląda od `max(ostatni_udany_przebieg − 5 min, now() − 60 dni)`, alert przy przerwie > 24 h | 13.3, ADR-041 (uzupełnienie 3.0) |
| C5-06 (ŚREDNIA) | Wymagania dla zadania Filtra danych osobowych w nowym planie (reguły a/b1/b1-i/b2/c, testy tabeli Poprawek, G-2, zestaw C5-01) | 18.4 |
| C5-07 (ŚREDNIA) | Wymagania DoD: testy (a)–(m) bezpiecznika, (a)–(h) ogranicznika, G-6 (a1)–(a4) i (b)–(d) jako testy automatyczne | 18.4 |
| C5-08 (NISKA) | Wymagania kolejności: ocena bezpieczników przed „Wynikiem dnia KALIBRUJ”; FK → przodek (`klucz_idempotencji`, `powiadomienie` po `uzytkownik`) | 18.4 |
| C5-09 (NISKA) | Wymagania redakcyjne: identyfikatory `TASK-NNN`, DoD bez „zgodnie z sekcją”, słownictwo „prace w tle”/„dzierżawa”, aktualizacja `HANDOFF.md` po pętli 3.x | 18.4 |

`TASKS.md`, `HANDOFF.md` i `CLAUDE.md` nie były edytowane (powstaną od nowa
po pętli 3.x zgodnie z 18.4).


## 23.1 Iteracja 1 (2026-10-31)

Utworzenie dokumentu na podstawie `vision.md`, `glossary.md`,
`requirements.md`, `architecture.md`. `docs/CRITIQUE_LOG.md` nie istniał —
brak punktów krytyki do zaadresowania.

- Dodano: język wszechobecny z mapowaniem słownika na identyfikatory oraz
  listę pojęć uzupełniających do dopisania do słownika (sekcja 3).
- Dodano: widoki kontekstu, kontenerów, komponentów i wdrożenia (Mermaid).
- Dodano: pełne DDL PostgreSQL, typy wyliczeniowe, ograniczenia, indeksy
  częściowe, schematy JSONB, ERD.
- Dodano: maszyny stanów Rozwiązania, Oceny, Weryfikacji, Zadania, elementu
  Ścieżki nauki.
- Dodano: 11 diagramów sekwencji dla wszystkich głównych przepływów.
- Dodano: kontrakty agentów, katalog pytań do Jev, reguły punktacji,
  pewności, niespójności i trasowania (T-1…T-6), algorytm progu (Wilson + IPW).
- Dodano: katalog endpointów REST, typy DTO, model błędów, idempotencję,
  uprawnienia na poziomie obiektów, format importu Arkuszy CKE.
- Dodano: porty modeli, kontrakt adaptera Jev, katalog prac w tle,
  mechanikę kolejki, harmonogram, zdarzenia domenowe.
- Dodano: macierz błędów (25 pozycji), scenariusze współbieżności (22),
  bezpieczeństwo, retencję RODO, budżet NFR-01, obserwowalność, bramki G-1…G-6.
- Decyzje sprzeczne z architekturą wstępną: druga ocena LLM nie może
  zaliczyć rozwiązania poniżej progu (ADR-008, wymóg FR-24).
- Rozszerzenia względem wymagań (do potwierdzenia przez właściciela
  produktu): powody Weryfikacji `PODEJRZENIE_BLEDU_ODCZYTU`,
  `KONTROLA_LOSOWA`, `BLAD_PRZETWARZANIA`; kolejka globalna (ADR-021);
  zatwierdzanie kont Nauczycieli przez Administratora.

## 23.2 Iteracja 2 (2026-10-31, wersja 2.0)

Zaadresowano wszystkie 29 uwag z `docs/CRITIQUE_LOG.md`, iteracja 1
(2 krytyczne, 10 wysokich, 14 średnich, 3 niskie). Żadna uwaga nie została
odrzucona w całości; odstępstwa od proponowanej poprawki są opisane w
kolumnie „Zmiany” i w ADR (C1-02: brak automatycznej finalizacji po SLA;
C1-08: zobowiązanie 99% zamiast 100%; C1-17: wyjątek synchroniczny zamiast
pracy w tle; C1-20: wersja zapisana przy rozpoczęciu zamiast okna 24 h).

| ID uwagi | Zmiany | Sekcje dokumentu |
|---|---|---|
| C1-01 (KRYTYCZNA) | Q-OC-04 = `BLAD_ODCZYTU` nie zeruje punktów i nie ustala `typ_bledu` (nowa kolumna `krok_podejrzany_odczyt`); Ocena wstępna z podejrzeniem odczytu jest „wstrzymana” dla Ucznia (bez punktów, typu i Wskazówki) i nie tworzy Wyniku zadania; T-3 opiera się na decyzjach Modelu decyzyjnego (Q-OC-04 z τ_odczyt = 0.20, nowe Q-OC-06), a `pewnosc_odczytu` tylko dodaje powód; endpoint `POST /rozwiazania/{id}/zgloszenie-bledu-odczytu` z powodem `ZGLOSZENIE_BLEDU_ODCZYTU`, oknem 14 dni i limitami; metryki `bledy_odczytu_niewykryte_30d`, `czulosc_t3` powiązane z G-2; Z-1 przeformułowana (sygnały LLM działają tylko w kierunku bezpiecznym) | 1.2, 3.3, 7.3, 8.2, 9.5, 9.12, 10.3, 10.4, 11.1, 11.3, 11.6, 11.8, 12.3, 12.4, 12.6, 17.6, ADR-004, ADR-017, ADR-018, ADR-022 |
| C1-02 (KRYTYCZNA) | Bramka G-7 (min. 12 weryfikatorów globalnych z modelu obciążenia); `deklarowane_minuty_weryfikacji`; SLA 72 h (`termin_at`), eskalacja KLASA → GLOBALNA → alert Administratora; status „opóźniona” dla Ucznia; kontrola przyjęć (mniej zadań otwartych dla Uczniów bez Klasy przy przeciążeniu); pseudonim HMAC, brak Klasy i zdjęcia maskowane dla obcego weryfikatora; podstawa prawna i informacja przy rejestracji; pełny model obciążenia z Kontrolą losową. Odrzucono automatyczną finalizację Oceny wstępnej po SLA (łamie FR-24/FR-26) — uzasadnienie w ADR-021 | 2 (A-12, A-13), 7.3, 10.3, 11.4, 12.3, 12.4, 12.8, 14.1 (E-28), 16.3, 17.6, 17.7, 21, ADR-021, ADR-031 |
| C1-03 (WYSOKA) | Formalna propozycja zmiany słownika PZS-1 (części A–C) z bramką G-0; Transkrypcja tylko ze zdjęcia (+ poprawki Ucznia); Weryfikacja — zmiana definicji; „Model System One” mapowany na `JevAdapter`, port = „Model decyzyjny”; `kalibrator` wydzielony z `prog_pewnosci` do `kalibrator_pewnosci`; `uzasadnienie` w `OcenaWyjscie`; `POST /zadania` tylko dla Nauczyciela; Powtórka jako `cel_elementu`, nie typ; elementy z Przypisań wyłącznie z reguły agenta | 1.2 (Z-6), 3.2, 3.3, 3.4, 7.3, 11.7, 12.3, 12.4, ADR-016, ADR-028, ADR-032 |
| C1-04 (WYSOKA) | Reguła „jedno słowo — jedno znaczenie” i lista terminów technicznych; zmiany nazw: `DecyzjaSkladowa`, `RezultatPytan`, `decyzja_modelu.wybor`, `procent_grupy_eksperymentu`, `nr_kryterium`/`wymaga_nr_kryterium`, `RezultatOceny`, `RezultatRozpoznawania`, `Rownowaznosc`, `rezultat_walidacji`, `wynik_koncowy_wzorcowy`, `typ_kolejki_weryfikacji`/`typ_kolejki` | 3.1, 3.3, 7.3, 7.5, 10, 12.4, 12.9, 13.1, ADR-016 |
| C1-05 (WYSOKA) | CHECK zmieniony na `ocena_weryfikowana_id IS NOT NULL OR 'BLAD_PRZETWARZANIA' = ANY(powody)`; pełna semantyka `PONOW_OCENE` (dwie trasy, Wynik zadania, powiadomienia, `ponowna_ocena_at`, `powod_anulowania`); warunek `OCZEKUJE` + zapis warunkowy na `weryfikacja.version`; scenariusz C-23 | 7.3, 8.1, 8.3, 9.13, 13.3, 14.2, 15.2 |
| C1-06 (WYSOKA) | Reguła odczytu „dostępna” + praca schedulera `ZWOLNIJ_BLOKADY_WERYFIKACJI`; eskalacja obejmuje wygasłe blokady; `POST /weryfikacje/{id}/przedluzenie` i idempotentne podjęcie przez właściciela (≤ 2 h, `podjeta_at`); tabela `szkic_rozstrzygniecia` i `PUT …/szkic`; poprawiony SQL podjęcia z `id = :id` i nawiasami | 7.3, 8.3, 9.6.1, 11.4.2, 12.3, 13.5, 15.2 (C-07, C-08), ADR-033 |
| C1-07 (WYSOKA) | `PlanPytan` z zależnościami; model zapasowy wykonuje cały plan jednym wywołaniem (pytania warunkowe zwinięte); Jev w 2 rundach; reguła ponowienia zależna od zapasu do terminu; skrócone timeouty (Jev 2 s, zapasowy 9 s); Wskazówka poza ścieżką krytyczną; tabela najgorszych przypadków S-1…S-6; G-4 z trzema scenariuszami (w tym timeouty przy zamkniętym obwodzie) | 9.5, 9.9, 10.4.3, 10.4.4, 13.1, 13.2, 17.2, 17.6, 22, ADR-009, ADR-026 |
| C1-08 (WYSOKA) | NFR-01 mierzone jako czas systemowy (rozpoznanie + ocena); T-6 i błędy dostawcy liczone jako naruszenia; SLO `odsetek_t6 ≤ 0,2%`, metryka end-to-end; termin twardy 120 s zamiast 28 s; A-11 przekazane do akceptacji w G-0. Odstępstwo: zobowiązanie 99% (nie 100%) z uzasadnieniem w A-11/ADR-025 | 2 (A-11), 7.3, 9.5, 14.2, 17.1, 17.2, 17.6, ADR-025 |
| C1-09 (WYSOKA) | Analiza mocy (tabela n_eff dla e = 0…1%); ZT-1 ≥ 700 obliczeniowych + ≥ 250 dowodów; `n_eff ≥ 189` w regule; kalibracja 5-krotną CV (out-of-fold zamiast 80/20); tryb pełnej weryfikacji jako jawne kryterium startowe; G-2 z pomiarem end-to-end; poprawka A-09 („oba rodzaje”) | 2 (A-09), 11.5.1, 11.5.3, 17.1, 17.6, 21 (R-10), ADR-007 |
| C1-10 (WYSOKA) | Kontrola losowa: priorytet 3 (jak zwykłe Weryfikacje), `termin_at` 72 h, zdjęcia zachowane do zamknięcia; metryka `wykonanie_kontroli_30d` i alert; reguły nadzoru (blokada obniżania progu, bezpiecznik braku nadzoru); wagi IPW skorygowane o wykonanie per koszyk; Kontrola wliczona w model 17.7 | 11.3, 11.5.3, 14.1 (E-29), 16.4, 17.7, ADR-007, ADR-033 |
| C1-11 (WYSOKA) | Usunięto `theta_poczatkowa`; odtwarzanie całego Profilu od stanu DOMYŚLNEGO z Wynikami Testu diagnostycznego jako zdarzeniami (`wynik_zadania.test_diagnostyczny_id`); test raz na poziom matury (`ux_test_jeden`, `TEST_JUZ_WYKONANY`); zakończenie testu przez `AKTUALIZUJ_PROFIL` (202 + `oczekujace_wyniki`); zdefiniowany skutek zmiany poziomu matury | 7.3, 9.1, 10.1.3, 12.3, 12.4, 13.6, ADR-010 |
| C1-12 (WYSOKA) | `decyzja_modelu.uczen_id` + indeks; `USUN_KONTO` zeruje `stan` we wszystkich kontekstach; rejestr usunięć w buckecie z Object Lock (poza bazą); wyłączone wersjonowanie i reguła cyklu życia bucketów; retencja u dostawców w A-07, walidacja przy aktywacji i G-6; C-20 ustawia `usun_po := now()` | 2 (A-07), 7.3, 13.1, 15.2 (C-20), 16.4, 17.6, ADR-019 |
| C1-13 (ŚREDNIA) | T-2: (SymPy `ROWNOWAZNE` lub p_tak(Q-OC-01) ≥ τ_FR25) i (błędny/brakujący krok lub p(błąd) ≥ τ_FR25); τ_FR25 = 0.20 z uzasadnieniem i w parametrach | 11.3, 22 |
| C1-14 (ŚREDNIA) | Tryby Wskazówki przy rozstrzygnięciu (`WLASNA`/`AUTOMATYCZNA`/`ZACHOWAJ` tylko przy identycznym błędzie); Wskazówka automatyczna także przy `BLAD_PRZETWARZANIA`; reguła N-1 (brakujący etap + `NIEPELNE_ROZWIAZANIE` + Wskazówka cytująca kryterium); `uzasadnienie` w DTO z szablonem | 9.6.2, 10.5, 11.1 p. 6, 11.7, 12.4 |
| C1-15 (ŚREDNIA) | Limit 12 par Q-OC-05, nadmiar → N-6 i Weryfikacja; zachowanie przy `ZADEN` (wariant o najwyższym p, `wariant` NOT NULL); N-1/N-2 liczone na `punkty_surowe` przed FR-20 (kolejność reguł) | 7.3, 10.4.3, 11.1, 11.2.1, ADR-005 |
| C1-16 (ŚREDNIA) | Zgodność Oceny = punkty + pierwszy błędny krok + brakujący krok (kolumna generowana); reguły przypisania `zrodlo_probki`; `KOREKTA_NAUCZYCIELA` wykluczona z progu; deduplikacja `ux_probka_ocena_auto` | 3.3, 3.4 (B-5), 7.3, 11.5.2, 17.1, ADR-027 |
| C1-17 (ŚREDNIA) | Jawny wyjątek synchroniczny: fasada Orkiestratora w `api`, timeout Q-RE-01 1,5 s, circuit breaker, powrót do POLITYKA_ELO; krawędź `api → Jev` w 5.1; moduły `lemat.rekomendacje`, `lemat.powiadomienia`, `lemat.audyt`, `lemat.http` w 6.1/6.2; właściciel `klucz_idempotencji` | 5.1, 6.1, 6.2, 6.3, 9.2, 10.1.6, 14.1 (E-27), ADR-023 |
| C1-18 (ŚREDNIA) | Krok 5b „wszystko opanowane” (`R-UTRWALENIE`); krok 8 „brak Zadań” (`RekomendacjaWyjscie.brak_elementu`, UI, zlecenie puli); reguły i szablony `R-JEV-*`; szablon `R-WKLADKA-DIAG` (Wyniki z testu są Wynikami zadania); jednolity klucz puli z rodzajem | 10.1.1, 10.1.2, 10.1.5, 10.1.6, 12.4, 13.3, 14.1 (E-26), ADR-011 |
| C1-19 (ŚREDNIA) | G-5 obejmuje oba poziomy matury; automatyczne zlecenie szkicu Wkładki (`GENERUJ_SZKIC_WKLADKI`) przy E-20 z przeglądem przez recenzentów; domknięcie elementu Wkładki `ROZPOCZETY` (≥ 60 s → `UKONCZONY`, inaczej `POMINIETY`; po 24 h `UKONCZONY`) | 8.5, 9.2, 10.2.1, 13.3, 14.1 (E-20), 17.6, ADR-015 |
| C1-20 (ŚREDNIA) | `POST /elementy-sciezki/{id}/rozpoczecie` przy otwarciu treści (ważność 7 dni dla Zadania); `element_sciezki.zadanie_wersja_id` zapisywana przy rozpoczęciu i akceptowana mimo nowszej wersji; dla Przypisań okno 72 h (`zastapiona_at`). Odstępstwo od propozycji „24 h od publikacji”: wersja z chwili otwarcia jest dokładniejsza | 7.3, 8.5, 12.3, 12.5, 15.2 (C-16, C-17) |
| C1-21 (ŚREDNIA) | Dokładnie jeden kontekst próby (CHECK, `422`); indeksy `ux_odpowiedz_przypisanie`, `ux_rozwiazanie_przypisanie`; waga × 0,5 dla kolejnej próby w 60 dniach; wynik i klucz w Przypisaniu ujawniane od terminu (`wynik_ujawniony_od`) | 7.3, 9.3, 10.1.3, 12.4, 12.5, ADR-018, ADR-032 |
| C1-22 (ŚREDNIA) | Zasięg Zadań `GLOBALNY`/`PRYWATNY`; rola recenzenta treści; uprawnienia Przeglądu/wycofania (8.4); endpointy Przeglądu i `POST /zadania/{id}/wycofanie` dla Administratora; powiadomienia recenzentów dla Zadań bez zlecającego; system nie generuje dowodów | 3.3, 7.3, 8.4, 12.3, 12.8, 13.6, ADR-015, ADR-028 |
| C1-23 (ŚREDNIA) | Tabela `kalibracja_trudnosci` z atomowym `UPDATE` i blokadą wiersza; `zadanie.version` niezmieniane; `poziom_trudnosci_nominalny` vs skalibrowany z histerezą i audytem; scenariusz C-24 | 3.2, 7.2, 7.3, 10.1.3, 15.1, 15.2, ADR-029 |
| C1-24 (ŚREDNIA) | Korekta zamyka otwartą Weryfikację (Kontrola, zgłoszenie) jako `ZAKONCZONA` albo zwraca `409 WERYFIKACJA_ZAJETA`; kolumna „ETag” dla każdego endpointu; blokada wiersza `rozwiazanie` i `409 OCENA_ZMIENIONA`; poprawione C-09, nowe C-26 | 8.1, 8.3, 9.6.3, 12.1, 12.3, 15.2 |
| C1-25 (ŚREDNIA) | Rozłączne pule (R/O/P/W) z kolumną `pula`; slot = korutyna asyncio, CPU w puli procesów; model szczytu „koniec lekcji” (A-03, 17.2 S-6, 17.4); G-3 ze scenariuszem „koniec lekcji” | 2 (A-03), 5.1, 7.3, 13.3, 13.4, 17.2, 17.4, 17.6, ADR-002 |
| C1-26 (ŚREDNIA) | Usunięto wpis ręczny (`WPIS_RECZNY_UCZNIA`, przejście w 8.1, akcje E-03/E-04/E-06); zastąpiono ponowieniem rozpoznania, nowymi zdjęciami i porzuceniem; wpływ na słownik w PZS-1 B-1 | 1.3, 7.3, 8.1, 9.4, 12.3, 14.1, ADR-024 |
| C1-27 (NISKA) | Dodano endpointy: przedłużenie blokady, `GET /przypisania`, `GET/PUT/DELETE /zestawy-zadan/{id}`, archiwizacja Klasy, opuszczenie Klasy przez Ucznia, `PUT /admin/zadania/{id}/diagnostyczne`, przywrócenie konta, wycofanie Zadania (oraz szkic, rozpoczęcie elementu, ponowienie rozpoznania, porzucenie, zgłoszenie, obciążenie) | 12.3, 12.4 |
| C1-28 (NISKA) | Kolejność „najpierw `ZASTAPIONA`, potem `INSERT`” (9.6.2, 15.1); usunięto `SZKIC` ze statusów Zadania (8.4 = 9.7); liczniki `liczba_przeslan`, `liczba_ponowien_rozpoznania`, `przeslanie_nr` vs `praca_w_tle.proby`; `liczba_cykli` z inkrementacją warunkową; CHECK kontekstu Odpowiedzi | 7.3, 8.4, 9.6.2, 9.7, 13.3, 15.1 |
| C1-29 (NISKA) | Seria po Wkładce (`R-SERIA`, stan w `sciezka_nauki`, warunki zakończenia z poziomem maturalnym); dzienny budżet generowania systemowego (200 zleceń, 150 PLN, `zlecenie_generowania.systemowe`, `koszt_pln`) | 7.3, 10.1.2, 10.1.5, 12.7, 13.3, 21 (R-05), ADR-030 |

Zmiany przekrojowe: wersja 2.0; nowe sekcje 3.4 (PZS-1), 9.12, 9.13,
11.4.2–11.4.4, 11.5.1–11.5.3, 11.6–11.8, 17.7; nowe bramki G-0 i G-7; nowe
tabele `kalibracja_trudnosci`, `kalibrator_pewnosci`, `szkic_rozstrzygniecia`;
ADR-022 … ADR-033; macierz błędów rozszerzona do E-30, scenariusze
współbieżności do C-27.

## 23.3 Iteracja 3 (2026-11-01, wersja 2.1) — weryfikacja zamknięcia uwag C1

Każda uwaga iteracji 1 (`docs/CRITIQUE_LOG.md`, status OTWARTA — krytyk
nie przeglądał jeszcze wersji 2.0) została ponownie sprawdzona punkt po
punkcie (każdy podpunkt „Problem” i każdy element „Oczekiwanej poprawki”)
względem treści wersji 2.0. Kolumna „Stan w 2.0” podaje wynik weryfikacji;
„Uzupełnienie 2.1” — zmiany tej iteracji dla luk wykrytych przy weryfikacji;
„Miejsce rozwiązania” — pełna lista sekcji, w których uwaga jest rozwiązana
(stan po wersji 2.1).

| ID uwagi | Stan w 2.0 | Uzupełnienie 2.1 | Miejsce rozwiązania |
|---|---|---|---|
| C1-01 (KRYTYCZNA) | pp. 1–5 rozwiązane (Q-OC-04 `BLAD_ODCZYTU` nie zeruje punktów, Ocena wstrzymana, brak Wyniku zadania, T-3 z decyzji Modelu decyzyjnego, endpoint zgłoszenia, metryki z G-2, Z-1); luka: po zgłoszeniu Wynik zadania zakwestionowanej Oceny zostawał w Profilu do rozstrzygnięcia (do 72 h), a po usunięciu zdjęć nie było reguły rozstrzygania | zawieszenie Wyniku zadania na czas zgłoszenia (`powod_uniewaznienia`, odtworzenie Profilu); maszyna stanów Wyniku zadania; komunikat dla Ucznia; reguła „na korzyść Ucznia” po usunięciu zdjęć z wymaganym komentarzem; priorytet 1 po 80 dniach; scenariusze C-28, C-29; R-13 | 1.2 (Z-1, Z-3), 7.3 (`ocena`, `wynik_zadania.powod_uniewaznienia`), 8.2, 8.6, 9.5, 9.12, 10.1.3, 10.3, 10.4.3, 11.1 pp. 5–7, 11.3 (T-3, komunikaty), 11.4.3 p. 3, 11.6, 11.8, 12.3, 12.4, 12.5, 12.6, 15.2 (C-28, C-29), 17.6 (G-2), 21 (R-13), ADR-004, ADR-017, ADR-018, ADR-022 |
| C1-02 (KRYTYCZNA) | rozwiązane: G-7, SLA 72 h, eskalacja, status „opóźniona”, alert pustej puli, kontrola przyjęć, pseudonimizacja, zdjęcia maskowane, podstawa prawna, model obciążenia z Kontrolą; luka: G-7 = 12 weryfikatorów, a A-03 daje tylko 10 Nauczycieli bez Klas — źródło brakującej pojemności nieokreślone | A-14 i ADR-034: skład puli (10 bez Klas + ≥ 2 współpracujących), limit 180 min/dobę dla prowadzących Klasy (`422 LIMIT_OBCIAZENIA_WERYFIKATORA`), plan awaryjny `LEMAT_REJESTRACJA_BEZ_KLASY = false` z `kod_zaproszenia` w rejestracji; tabela składu puli w 17.7; R-12 | 2 (A-12, A-13, A-14), 7.3, 10.3, 11.4, 12.2, 12.3, 12.4, 12.8, 14.1 (E-28), 16.3, 17.6 (G-0, G-7), 17.7, 21 (R-02, R-12), 22, ADR-021, ADR-031, ADR-034 |
| C1-03 (WYSOKA) | pp. 1–7 rozwiązane (PZS-1 B-1…B-7, `kalibrator_pewnosci`, `uzasadnienie` w DTO, `POST /zadania` tylko N, `JevAdapter`); luka w p. 8: 19 nazw używanych w DDL/API (m.in. `element_sciezki`, `zlecenie_generowania`, weryfikator globalny, szkic rozstrzygnięcia) nie miało wpisu w 3.3, a `kalibrator_pewnosci.zrodlo` używał typu `zrodlo_progu` | 19 nowych pojęć w 3.3 (PZS-1 A: 61 pojęć) i test CI z zamkniętą listą wyłączeń; typ `zrodlo_kalibratora` | 1.2 (Z-6), 3.1, 3.2, 3.3, 3.4, 7.3, 11.7, 12.3, 12.4, ADR-016, ADR-028, ADR-032 |
| C1-04 (WYSOKA) | rozwiązane w całości (sprawdzono brak nazw `Odpowiedz` modelu, `procent_wariantu`, `nr_kroku`, `WynikRozpoznawania`, `wynik_walidacji`, `kolejka_weryfikacji` jako typu) | `zrodlo_kalibratora` rozdziela źródło kalibratora od źródła progu (3.1 p. 5) | 3.1 pp. 5–6, 3.3, 3.4 C, 7.3, 7.5, 10, 12.4, 12.9, 13.1, ADR-016 |
| C1-05 (WYSOKA) | rozwiązane (CHECK, semantyka, warunek `OCZEKUJE` + `version`, C-23); luka: trasa Weryfikacja w `PONOW_OCENE` nie zlecała `AKTUALIZUJ_PROFIL` mimo zapisu Wyniku zadania, a kolejność „anulowanie → Kontrola losowa” nie była jawna wobec `ux_weryfikacja_otwarta` | `AKTUALIZUJ_PROFIL` w trasie Weryfikacja; jawna kolejność anulowania przed `INSERT` Kontroli losowej | 7.3 (`weryfikacja`), 8.1, 8.3, 9.13, 13.3, 14.2, 15.1 p. 3, 15.2 (C-23) |
| C1-06 (WYSOKA) | rozwiązane w całości (reguła odczytu, `ZWOLNIJ_BLOKADY_WERYFIKACJI`, eskalacja dostępnych, `…/przedluzenie`, idempotentne podjęcie, szkice, poprawiony SQL) | — | 7.3, 8.3, 9.6.1, 11.4.2, 12.3, 13.5, 15.2 (C-07, C-08), ADR-033 |
| C1-07 (WYSOKA) | rozwiązane (`PlanPytan`, zwijanie pytań warunkowych, 2 rundy Jev, reguła ponowienia, Wskazówka asynchroniczna, S-1…S-6, G-4 z trzema scenariuszami) | — | 9.5, 9.9, 10.4.3, 10.4.4, 13.1, 13.2, 17.2, 17.6 (G-4), 22, ADR-009, ADR-026 |
| C1-08 (WYSOKA) | rozwiązane (czas systemowy z rozpoznaniem, T-6 jako naruszenie, SLO T-6, metryka end-to-end, A-11 w G-0); luka: termin wywołania rozpoznawania liczony od `przeslano_at` był już miniony przy ponowieniu, a scenariusz „timeout rozpoznania + ponowienie” nie był policzony | termin per próba (10.3.1, 9.4); scenariusz S-7 i rozpisanie budżetu 1% SLO; wymaganie A-08 ≤ 0,3% błędów/timeoutów dostawcy | 2 (A-08, A-11), 7.3, 9.4, 9.5, 10.3.1, 14.2, 17.1, 17.2 (S-1…S-7), 17.6, 22, ADR-025 |
| C1-09 (WYSOKA) | rozwiązane (analiza mocy — tabela `n_eff` sprawdzona obliczeniowo: 189 / 335 / 482 / 753; ZT-1 ≥ 700 + ≥ 250; CV5; tryb pełnej weryfikacji; G-2 end-to-end; A-09) | — | 2 (A-09), 11.5.1, 11.5.3, 17.1, 17.6 (G-1, G-2), 21 (R-10), ADR-007 |
| C1-10 (WYSOKA) | rozwiązane (priorytet 3 i SLA 72 h dla Kontroli, zdjęcia do zamknięcia, `wykonanie_kontroli_30d`, reguły nadzoru, IPW z korektą wykonania per koszyk, Kontrola w modelu 17.7) | — | 11.3, 11.5.3, 14.1 (E-29), 16.4, 17.7, ADR-007, ADR-033 |
| C1-11 (WYSOKA) | rozwiązane w całości (brak `theta_poczatkowa`, odtwarzanie od stanu DOMYŚLNEGO z Wynikami testu jako zdarzeniami, test raz na poziom, `AKTUALIZUJ_PROFIL`, zmiana poziomu matury) | — | 7.3, 9.1, 10.1.3, 12.3, 12.4, 13.6, ADR-010 |
| C1-12 (WYSOKA) | pp. 1–5 rozwiązane (`decyzja_modelu.uczen_id`, rejestr usunięć w Object Lock, wersjonowanie bucketów, retencja dostawców, C-20); luka: kopie zdjęć w zbiorze treningowym nie miały powiązania z kontem, więc odwołania zgody i usunięcia konta nie dało się dla nich wykonać | tabela `kopia_treningowa` (`ON DELETE RESTRICT`), prace `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` i `USUN_KOPIE_TRENINGOWE`, krok 4a w `USUN_KONTO`, `zgoda_na_trening` w `PATCH /uczniowie/ja`, ADR-035 | 2 (A-07), 6.2, 7.1, 7.3, 12.3, 13.1, 13.3, 13.5, 15.2 (C-20), 16.4, 17.6 (G-6), 22, ADR-019, ADR-035 |
| C1-13 (ŚREDNIA) | rozwiązane w całości (T-2 z τ_FR25 = 0.20, uzasadnienie, parametr) | — | 11.3, 22 |
| C1-14 (ŚREDNIA) | rozwiązane w całości (tryby Wskazówki, Wskazówka przy `BLAD_PRZETWARZANIA`, reguła N-1, `uzasadnienie` w DTO) | — | 9.6.2, 10.5, 11.1 p. 6, 11.7, 12.4 |
| C1-15 (ŚREDNIA) | rozwiązane w całości (12 par i N-6, `ZADEN` → wariant o najwyższym p, kolejność reguł na `punkty_surowe`) | — | 7.3, 10.4.3, 11.1, 11.2.1, ADR-005 |
| C1-16 (ŚREDNIA) | rozwiązane w całości (Zgodność Oceny, reguły źródła próbki, wykluczenie `KOREKTA_NAUCZYCIELA`, `ux_probka_ocena_auto`) | — | 3.3, 3.4 (B-5), 7.3, 11.5.2, 17.1, ADR-027 |
| C1-17 (ŚREDNIA) | rozwiązane w całości (jawny wyjątek synchroniczny przez fasadę Orkiestratora, timeout 1,5 s, krawędź `api → Jev`, moduły w 6.1/6.2, właściciel `klucz_idempotencji`) | — | 5.1, 6.1, 6.2, 6.3, 9.2, 10.1.6, 14.1 (E-27), ADR-023 |
| C1-18 (ŚREDNIA) | pp. 1–5 rozwiązane; luka w p. 1: krok 5b nie definiował zachowania, gdy wybrana Umiejętność nie ma Zadania na Poziomie trudności ≥ 4 | ścieżka zastępcza kroku 5b (kolejne Umiejętności, potem najwyższy Poziom trudności w zakresie, zapotrzebowanie puli, krok 8) | 10.1.1, 10.1.2 (kroki 5b, 8), 10.1.5, 10.1.6, 12.4, 13.3, 14.1 (E-26), ADR-011 |
| C1-19 (ŚREDNIA) | rozwiązane w całości (G-5 dla obu poziomów, `GENERUJ_SZKIC_WKLADKI`, domknięcie elementu Wkładki) | — | 8.5, 9.2, 10.2.1, 13.3, 14.1 (E-20), 17.6 (G-5), ADR-015 |
| C1-20 (ŚREDNIA) | rozwiązane w całości (rozpoczęcie przy otwarciu treści, wersja ustalana przy rozpoczęciu, okno 72 h dla Przypisań) | — | 7.3, 8.5, 12.3, 12.5, 15.2 (C-16, C-17) |
| C1-21 (ŚREDNIA) | rozwiązane w całości (dokładnie jeden kontekst, indeksy per Przypisanie, waga kolejnej próby, ujawnianie po terminie) | — | 7.3, 9.3, 10.1.3, 12.4, 12.5, ADR-018, ADR-032 |
| C1-22 (ŚREDNIA) | rozwiązane w całości (zasięg, recenzent treści, uprawnienia 8.4, endpointy Administratora, brak dowodów systemowych) | — | 3.3, 7.3, 8.4, 12.3, 12.8, 13.6, ADR-015, ADR-028 |
| C1-23 (ŚREDNIA) | rozwiązane w całości (`kalibracja_trudnosci`, atomowy `UPDATE`, histereza i audyt, C-24) | — | 3.2, 7.2, 7.3, 10.1.3, 15.1, 15.2 (C-24), ADR-029 |
| C1-24 (ŚREDNIA) | rozwiązane w całości (korekta zamyka otwartą Weryfikację albo `409`, kolumna „ETag”, C-09, C-26) | — | 8.1, 8.3, 9.6.3, 12.1, 12.3, 15.2 (C-09, C-26) |
| C1-25 (ŚREDNIA) | rozwiązane w całości (rozłączne pule, slot = korutyna, model „koniec lekcji”, G-3 b) | — | 2 (A-03), 5.1, 7.3, 13.3, 13.4, 17.2 (S-6), 17.4, 17.6 (G-3), ADR-002 |
| C1-26 (ŚREDNIA) | rozwiązane w całości (brak wpisu ręcznego, ADR-024, PZS-1 B-1) | — | 1.3, 7.3, 8.1, 9.4, 12.3, 14.1, ADR-024 |
| C1-27 (NISKA) | rozwiązane w całości (wszystkie 8 brakujących operacji ma endpointy) | — | 12.3, 12.4 |
| C1-28 (NISKA) | rozwiązane w całości (kolejność `ZASTAPIONA` → `INSERT`, statusy Zadania, liczniki przesłań i prób, inkrementacja warunkowa cykli, CHECK kontekstu Odpowiedzi) | — | 7.3, 8.4, 9.6.2, 9.7, 13.3, 15.1 |
| C1-29 (NISKA) | rozwiązane w całości (Seria z warunkiem wyjścia na poziomie maturalnym, budżet generowania systemowego) | — | 7.3, 10.1.2, 10.1.5, 12.7, 13.3, 21 (R-05), ADR-030 |

Zmiany przekrojowe wersji 2.1: nowe założenie A-14; nowa sekcja 8.6;
nowe tabele i typy `kopia_treningowa`, `zrodlo_kalibratora`,
`powod_uniewaznienia_wyniku`; nowe prace w tle `KOPIUJ_DO_ZBIORU_TRENINGOWEGO`,
`USUN_KOPIE_TRENINGOWE`; nowe kody błędów `WYMAGANY_KOD_ZAPROSZENIA`,
`LIMIT_OBCIAZENIA_WERYFIKATORA`; scenariusz S-7; scenariusze współbieżności
C-28, C-29; ryzyka R-12, R-13; ADR-034, ADR-035; uzupełnienie ADR-022.

## 23.5 Iteracja 4 (2026-11-01, wersja 2.2) — zamknięcie uwag C1-21 i C2-01 … C2-16

Zaadresowano wszystkie 17 otwartych uwag z `docs/CRITIQUE_LOG.md`: ponownie
otwartą C1-21 oraz C2-01 … C2-16 z iteracji 2 (3 wysokie, 10 średnich, 3
niskie). Decyzje zmieniające lub zawężające wymagania przekazano do G-0
jako D-10 … D-14 z opcją domyślną opisaną w całości w dokumencie.
Odstępstwa od proponowanych poprawek: C2-01 — Oceny automatyczne z
Poprawkami nie są wyłączane z populacji progu, tylko objęte podwójną
Kontrolą losową i bezpiecznikiem (wyłączenie uczyniłoby NFR-03
niemierzalnym dla tej grupy; wyłączone są Oceny z T-7); C2-07 — globalny
ogranicznik jest przydziałem lokalnym z rejestru procesów, a nie token
bucketem w PostgreSQL (koszt zapisu na żądanie); C2-08 — wybrano wariant
„odroczenie anulowania do `USUN_KONTO`” zaproponowany przez krytyka.

| ID uwagi | Rozwiązanie | Miejsce rozwiązania |
|---|---|---|
| C1-21 (ŚREDNIA) | Kontekst efektywny próby: element `R-PRZYPISANIE` przekazuje `przypisanie_id` każdej próbie (trigger `trg_kontekst_proby`), więc jedna unikalność (Uczeń, Przypisanie, Zadanie) obejmuje obie drogi; klucz Zadania zamkniętego ujawniany od terminu także z elementu; Weryfikacja trafia do Nauczyciela Przypisania; próba liczy się do `PostepPrzypisaniaWyjscie` (nowy DTO, pole `droga`); jednoznaczny CHECK z kombinacjami {E}, {P}, {E, P}, {T}; `wygasa_at` elementu z Zadaniem zamkniętym ≤ termin; błąd `PRZYPISANIE_PO_TERMINIE` | 7.3 (`element_sciezki`, `rozwiazanie`, `odpowiedz`, `trg_kontekst_proby`), 8.5, 9.3, 10.1.1, 10.1.2 krok 1, 11.4.1 p. 1, 12.2, 12.4 (`OdpowiedzWejscie`, `OdpowiedzWyjscie`, `PostepPrzypisaniaWyjscie`), 12.5, 15.2 (C-30), 18.1 (FR-32), ADR-032 (uzupełnienie it. 4) |
| C2-01 (WYSOKA) | Poprawki Transkrypcji związane z krokami agenta (`nr_kroku_agenta`), bez usuwania, ≤ 2 wstawione; limity twarde (`422 POPRAWKI_ZBYT_ROZLEGLE`) i miękkie; Sprawdzenie zgodności poprawek ze zdjęciem (`SPRAWDZ_POPRAWKI_TRANSKRYPCJI`, rola `ROZPOZNAWANIE_PISMA`, Z-1); reguła T-7 i powód `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`; Oceny z T-7 poza populacją progu; podwójny odsetek Kontroli losowej i bezpiecznik poprawek; metryki; G-2; PZS-1 B-1/B-2 | 1.3, 3.3, 3.4 (B-1, B-2), 7.3 (`powod_weryfikacji`, `transkrypcja`, `krok_transkrypcji`), 8.1, 9.4, 9.5, 10.3.1, 10.3.3, 11.1 p. 7, 11.3 (T-7, T-5, priorytet, komunikat), 11.5.2, 12.2, 12.3, 12.4, 12.5, 13.1, 13.3, 13.6, 14.1 (E-31, E-32), 15.2 (C-35), 17.2, 17.3, 17.6 (G-0, G-2), 17.7, 18.1 (FR-18), 21 (R-14), 22, ADR-024, ADR-036, D-10 |
| C2-02 (WYSOKA) | `weryfikacja.version` = wersja treści ustawiana triggerem `trg_weryfikacja_wersja`; podjęcie, przedłużenie, zwolnienie, eskalacja bez zmiany `version`; przedłużenie zwraca ETag i `version`; `412` z polem `zmiany_tresci`; scenariusz rozstrzygnięcia po przedłużeniach | 7.2, 7.3 (`weryfikacja`, trigger), 8.3, 9.6.1 (SQL i diagram), 9.12, 9.13, 11.4.2, 11.4.3, 12.3, 12.4, 15.1 p. 4, 15.2 (C-23, C-28, C-31), ADR-033 (uzupełnienie), ADR-037 |
| C2-03 (WYSOKA) | Zadania wygenerowane na zlecenie Nauczyciela zawsze `DO_PRZEGLADU` z Przeglądem zlecającego (wynik `PRYWATNY`, promocja przez recenzenta); Zgłoszenie błędu w Zadaniu dla Zadań `GLOBALNY` (tabela, endpointy, powiadomienie, SLA 5 dni roboczych), wstrzymanie w doborze po 3 zgłoszeniach; ADR-028 oznaczony G-0; decyzja D-11 | 3.3, 6.2, 7.3 (`zadanie.wstrzymane_w_doborze`, `zgloszenie_bledu_zadania`), 8.4, 9.7, 10.1.2 krok 7, 12.2, 12.3, 12.4, 12.7, 12.8, 13.6, 15.2 (C-36), 17.6 (G-0), 18.1 (FR-14), 21 (R-18), 22, ADR-028, ADR-038, D-11 |
| C2-04 (ŚREDNIA) | Deduplikacja `AKTUALIZUJ_PROFIL` tylko wśród prac `OCZEKUJE` (`ux_praca_dedup_oczekujace`), pętla handlera do pustego wyniku, zamiatacz `ZAMIATAJ_PROFILE` co 5 min, metryka i alert wieku najstarszego niezastosowanego Wyniku | 7.3 (`praca_w_tle`), 13.3, 13.4, 13.5, 15.2 (C-11, C-32), 17.3, 22, ADR-002 (uzupełnienie) |
| C2-05 (ŚREDNIA) | Kolumna `weryfikacja.powody_poczatkowe` (niezmienna poza dołączeniem Oceny przez `PONOW_OCENE`, trigger `trg_powody_poczatkowe`); źródło Próbki z powodów początkowych, zgłoszenie dopisane później nie wyklucza próbki; test jednostkowy | 3.3, 7.3, 9.5, 9.6.2, 9.12, 9.13, 11.5.2, 12.4 (`WeryfikacjaSzczegoly`), ADR-007 (uzupełnienie) |
| C2-06 (ŚREDNIA) | Analiza mocy G-1(a) per rodzaj (tabela 2: LB, minimalna obserwowana zgodność, moc); ZT-1 ≥ 350 dowodów z LB ≥ 85,0% (≥ 700 obliczeniowych z LB ≥ 87,5%); poprawiona półszerokość ±3,1 pp; D-12 | 2 (A-09), 11.5.1, 17.6 (G-0, G-1), ADR-007 (uzupełnienie), D-12 |
| C2-07 (ŚREDNIA) | Skład rund i fal (27 + 1 albo 16 + 13 pytań); tryby Jev `WSADOWY`/`POJEDYNCZY` (A-01); budżet 17.2 i S-1 … S-7 przeliczone dla 3 fal i terminu miękkiego `max(10 s, 28 s − t_R)`; globalny ogranicznik szybkości (`limit_szybkosci_modelu`, przydział ważony, token bucket); `429` i brak przydziału poza circuit breakerem; G-4 (d) | 2 (A-01), 3.1 p. 6, 6.2, 7.3 (`limit_szybkosci_modelu`), 9.5, 10.4.3, 13.1 (`OgranicznikSzybkosci`), 13.2, 14.1 (E-33), 14.2, 17.1, 17.2, 17.4, 17.6 (G-4), 21 (R-17), 22, ADR-039 |
| C2-08 (ŚREDNIA) | Okres karencji: żądanie usunięcia wstrzymuje procesy zamiast je anulować; przywrócenie wznawia procesy (tabela 16.4: Weryfikacje z nowym SLA, zgłoszenia i zawieszone Wyniki rozstrzygane normalnie, ponowne rozpoznanie, T-6 dla przerwanych ocen); anulowanie w kroku 0 `USUN_KONTO`; Kontrole kont `USUWANE` poza `π_wykonanie`; scenariusz C-33 | 3.3, 8.1, 8.3, 9.6.1, 9.12, 11.4.2, 11.5.3, 12.3, 13.3, 13.6, 15.2 (C-20, C-33), 16.4, 22, ADR-040 |
| C2-09 (ŚREDNIA) | Rejestr wszystkich Zdarzeń RODO (żądanie, anulowanie, wykonanie usunięcia, odwołanie i udzielenie zgody) zapisywany w Object Lock **przed** transakcją, kopia w `zdarzenie_rodo`; `UZGODNIJ_REJESTR_RODO`; procedura `ponow_zdarzenia_rodo` przed startem workerów; G-6 z trzema scenariuszami | 3.3, 5.1, 6.2, 7.3 (`zdarzenie_rodo`, `typ_zdarzenia_rodo`), 12.2, 12.3, 13.3, 13.5, 14.1 (E-36), 15.2 (C-34), 16.4, 17.1, 17.6 (G-6), 21 (R-16), ADR-041 |
| C2-10 (ŚREDNIA) | Reguła promptu i deterministyczny Filtr danych osobowych przed zapisem Transkrypcji, Poprawek i kopii treningowej (ramki kroków, dane z konta, PESEL, e-mail); kolumna `dane_osobowe_usuniete`, metryka; test na ZT-1 (G-2, G-6); poprawione ADR-031 i 16.3 | 2 (A-09), 3.3, 7.3 (`transkrypcja`, `krok_transkrypcji.obszar`), 9.4, 10.3.1 p. 2 i 3a, 10.3.2, 13.3, 16.3, 17.3, 17.6 (G-2), ADR-031, ADR-035, ADR-042 |
| C2-11 (ŚREDNIA) | Zmiany nazw: `decyzje_skladowe` (schemat adaptera zapasowego), `kod_statusu_http`/`cialo_http`, `opcje_zamkniete`, `?wersja_zdjecia=`, `rezultat_przestarzaly`, `rezultat_ujawniony_od`, `OdpowiedzWyjscie.rezultat`, „prace w tle” zamiast „kolejka”, „Strategia eksperymentalna” zamiast „Wariant eksperymentalny”, „opcja” zamiast „wariant” w 23.4; 3.1 p. 5 rozszerzony; 17 nowych pojęć w 3.3 (w tym Pewność surowa, Punkty surowe, Niespójność, Rozstrzygnięcie, Korekta, Eskalacja); PZS-1 B-8 (Agent), B-9 (Jev); test CI na tabele, tokeny kolumn, typów, OpenAPI, schematów modeli i słowa zarezerwowane | 3.1, 3.3, 3.4 (A, B-8, B-9, C), 5.1, 5.2, 6.3, 7.3, 7.5.1, 9.3, 10.1.6, 10.2.1, 12.3, 12.4, 13.2, 13.4, 14.1 (E-13), 15.2, 23.4 |
| C2-12 (ŚREDNIA) | SLA 3 dni robocze (`dzien_wolny`, `dodaj_dni_robocze`); model tygodniowy 7/5 z jawnymi eskalacjami 5% (A-15) i współczynnikiem 1,3; G-7 = 18 na start; `zaleglosc_globalna_min` z eskalacjami; kontrola przyjęć per Klasa; D-13 | 2 (A-13, A-14, A-15), 3.3, 7.3, 11.4.1 p. 4, 11.4.3, 11.5.3, 12.4 (`ObciazenieWeryfikacjiWyjscie`), 13.3, 17.3, 17.6 (G-0, G-7), 17.7, 21 (R-02, R-12, R-15), 22, ADR-021, ADR-034, ADR-043, D-4, D-13 |
| C2-13 (ŚREDNIA) | G-5 wymaga ≥ 3 Zadań diagnostycznych na (Dział, poziom matury, nominalny Poziom trudności 2/4); dobór po poziomie nominalnym; test częściowy (`pominiete_dzialy`) albo `409 TEST_NIEDOSTEPNY` < 50%; `SPRAWDZ_PULE_DIAGNOSTYCZNA`, `409 PULA_DIAGNOSTYCZNA_ZA_MALA`, `GET /admin/pula-diagnostyczna`; DTO `TestDiagnostycznyWyjscie`; pominięcie testu jako D-14 | 7.3 (`ix_zadanie_diagnostyczne`, `pominiete_dzialy`), 9.1, 12.2, 12.3, 12.4, 13.3, 13.5, 14.1 (E-34), 17.6 (G-0, G-5), 18.1 (FR-03), 22, D-14 |
| C2-14 (NISKA) | Przepływ usunięcia konta Nauczyciela i Administratora (endpointy, karencja, Przekazanie Klasy, archiwizacja nieprzekazanych Klas, pseudonimizacja uzasadnień, ochrona ostatniego Administratora); DDL z `ON DELETE SET NULL` i CHECK „Klasa bez Nauczyciela tylko zarchiwizowana” | 1.3, 3.3, 6.2, 7.3 (`klasa`, `zestaw_zadan`, `przypisanie`, `import_arkusza`), 11.4.1 p. 1, 11.4.3 p. 1, 12.2, 12.3, 13.3, 13.6, 16.3, 16.4, 17.6 (G-6), 18.1 (FR-30), ADR-044 |
| C2-15 (NISKA) | Piaskownica procesowa Pillow w `api` (2 procesy na replikę, RLIMIT, limit czasu), limit pikseli i wymiarów przed dekodowaniem, `422 ZDJECIE_NIEPRAWIDLOWE`, `503` przy zajętej piaskownicy; budżet i pojemność | 5.1, 5.2, 9.4, 10.3.1 p. 1, 12.2, 14.1 (E-35), 16.2, 17.2, 17.4, 22, ADR-045 |
| C2-16 (NISKA) | (1) `rezultat_ujawniony_od` NULL dla Odpowiedzi testu do `…/zakonczenie` lub `…/pominiecie`; (2) warunek 2 h przy ponownym podjęciu przez właściciela; (3) `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` przy każdym przejściu do „`OCENIONE` bez otwartej Weryfikacji” i nadpisanie przy nowszej Ocenie; (4) okno zgłoszenia od Oceny aktualnej; (5) limit 2 odrzuceń automatycznych szkiców Wkładek na 30 dni i szkice w budżecie generowania systemowego; (6) Wkładka bez dowodu przeczytania po `wygasa_at` → `POMINIETY` w 8.5 i 13.3 | (1) 7.3, 9.1; (2) 9.6.1, 11.4.2, ADR-033; (3) 7.3 (`kopia_treningowa.ocena_id`), 9.5, 9.6.2, 9.13, 13.3, 16.4, ADR-035; (4) 11.6, 7.3; (5) 7.3 (`wkladka_teoretyczna`), 10.2.1, 12.7, 13.3, 14.1 (E-30), 22; (6) 8.5, 13.3 |

Zmiany przekrojowe wersji 2.2: nowe założenie A-15; nowe tabele
`zgloszenie_bledu_zadania`, `zdarzenie_rodo`, `dzien_wolny`,
`limit_szybkosci_modelu`; nowe typy `status_zgloszenia_zadania`,
`typ_zdarzenia_rodo` i wartość `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`; nowe
triggery `trg_kontekst_proby`, `trg_weryfikacja_wersja`,
`trg_powody_poczatkowe`; nowa sekcja 10.3.3; reguła trasowania T-7; nowe
prace w tle `SPRAWDZ_POPRAWKI_TRANSKRYPCJI`, `ZAMIATAJ_PROFILE`,
`SPRAWDZ_PULE_DIAGNOSTYCZNA`, `UZGODNIJ_REJESTR_RODO`; nowe kody błędów
`PRZYPISANIE_PO_TERMINIE`, `TEST_NIEDOSTEPNY`, `PULA_DIAGNOSTYCZNA_ZA_MALA`,
`ZGLOSZENIE_ZADANIA_ISTNIEJE`, `OSTATNI_ADMINISTRATOR`,
`ZDJECIE_NIEPRAWIDLOWE`, `POPRAWKI_ZBYT_ROZLEGLE`; macierz błędów do E-36;
scenariusze współbieżności do C-36; scenariusze budżetu S-1b, S-2b; ryzyka
R-14 … R-18; ADR-036 … ADR-045; decyzje G-0 D-10 … D-14; PZS-1: 78 pojęć w
części A, B-8 i B-9 w części B.


## 23.6 Iteracja 5 (2026-11-01, wersja 2.3) — zamknięcie uwag C3-01 … C3-09

Zaadresowano wszystkie 9 otwartych uwag z `docs/CRITIQUE_LOG.md`, iteracja 3
(1 wysoka, 6 średnich, 2 niskie). Jedna decyzja wymaga właściciela produktu
(D-15, z opcją domyślną opisaną w 16.4). Odstępstwa od sugerowanych
poprawek: C3-01 — wybrano dolną granicę Wilsona (LB > 2% przy `n_eff ≥ 50`)
zamiast SPRT, z uzasadnieniem w ADR-046; C3-02 — zamiast maskowania
fragmentu kroku wg ramki (ramka pikselowa nie wskazuje znaków w tekście) krok
z treścią matematyczną pozostaje nienaruszony i kieruje Ocenę do Weryfikacji
(T-3), a maskowany w całości jest tylko krok bez treści matematycznej; C3-03
— zastosowano oba warianty krytyka jednocześnie (wiersz w pierwszej
transakcji **i** `zrodlo = WORKER` wyłączone z unieważniania); C3-06 —
sprzątanie zamiast Zdarzenia RODO dla kopii (ADR-047).

| ID uwagi | Rozwiązanie | Miejsce rozwiązania |
|---|---|---|
| C3-01 (WYSOKA) | Bezpieczniki trafności i poprawek wyzwalane wyłącznie dowodem przekroczenia: LB Wilsona 95% błędu (IPW) > 2% przy `n_eff ≥ 50`; ostrzeżenie (blokada obniżania progu) przy UB > 2% i `n_eff ≥ 189`; próba dla pary (konfiguracja, rodzaj) powyżej bieżącego progu, nie dla rekordu progu (brak zapadki); rachunek: tabela `k_min` i P(wyzwolenie) dla n = 50 … 1 000 (przy 0 błędach LB = 0 — nigdy) i symulacja 90 dni dla startu, fazy ustalonej i modelu zapasowego; tabela `stan_bezpiecznika` zamiast flagi środowiskowej, dezaktywacja (w wersji 2.3: „zdjęcie”) przez Administratora z `okno_od`; testy jednostkowe (a)–(f), w tym „0 błędów przy n = 50 … 188 nie wyzwala”; scenariusz startowy w 17.7: reguła 2.2 dawałaby auto ≈ 0%, `M` = 5 154 min, 137 min na Nauczyciela i 26 weryfikatorów globalnych zamiast 18 — reguła 2.3 zachowuje „Start: auto 47%” i G-7 = 18 | 3.3 (Bezpiecznik), 7.3 (`typ_bezpiecznika`, `stan_bezpiecznika`), 9.8, 11.5.2, 11.5.3, 12.3 (`/admin/bezpieczniki`, `/admin/bezpieczniki/zdjecie` — od wersji 2.4 `/admin/bezpieczniki/dezaktywacja`), 12.2 (`BEZPIECZNIK_NIEAKTYWNY`), 12.4 (`StanBezpiecznikaWyjscie`), 13.3 (`KALIBRUJ`), 14.1 (E-21), 17.1 (NFR-03), 17.3, 17.7, 20, 21 (R-20), 22, ADR-007 (uzupełnienie), ADR-046 |
| C3-02 (ŚREDNIA) | Filtr danych osobowych nie usuwa kroków i nie zmienia `latex`; krok w ramce bez treści matematycznej → `[DANE_OSOBOWE]` w całości, z treścią matematyczną → zachowany, `konflikt_filtra` → T-3 (11.1 p. 7d, Z-1); dopasowanie nazw tylko w `tekst`, Słownik terminów matematycznych (≈ 350 lematów, ≈ 4 000 form) nigdy niezastępowany, nazwy < 4 znaków — dopasowanie dokładne; pozycje zastąpień w `zastapienia_danych_osobowych`, przywracanie słowa Poprawką (dopasowania przybliżone nie są ponownie filtrowane, Poprawka podlega Sprawdzeniu i podwójnej Kontroli); G-2: 150 kartek (UB 2,5%), w tym krótkie imiona i nazwiska-terminy, 0 zastąpień terminów matematycznych i ≤ 0,5% fałszywych zastąpień na 1 050 Transkrypcjach, `konflikt_filtra` ≤ 1% | 2 (A-09), 3.3 (Filtr, Słownik terminów matematycznych, Obszar kroku), 7.3 (`krok_transkrypcji`, `transkrypcja.konflikt_filtra`), 10.3.1 p. 3a, 11.1 p. 7d, 12.4 (`KrokTranskrypcji`, `TranskrypcjaWyjscie`), 14.1 (E-37), 17.3, 17.6 (G-2), 21 (R-19), 22, ADR-042 (uzupełnienie) |
| C3-03 (ŚREDNIA) | `USUNIECIE_WYKONANE` zapisuje `worker` (`zrodlo = WORKER`, deterministyczne `id` = uuid5, `If-None-Match: *`) przed pierwszą zmianą w bazie, wiersz `zdarzenie_rodo` w pierwszej transakcji `USUN_KONTO` (krok 1, razem z anulowaniem procesów); `UZGODNIJ_REJESTR_RODO` unieważnia tylko zdarzenia `API`, a dla `WORKER` ponawia `USUN_KONTO` po 2 h i alarmuje po 26 h; `ponow_zdarzenia_rodo` wykonuje kroki 1–6 synchronicznie bez względu na czas od usunięcia; poprawione ADR-041 i komentarz `zdarzenie_rodo.id`; G-6 (a) z odtworzeniem 60 min po usunięciu | 3.3 (Zdarzenie RODO), 5.1, 7.3 (`zrodlo_zdarzenia_rodo`, `zdarzenie_rodo`), 13.3 (`UZGODNIJ_REJESTR_RODO`, `USUN_KONTO`), 14.1 (E-36), 16.4 (kroki 0–1, procedura odtworzenia, konto Nauczyciela p. 1), 17.3, 17.6 (G-6 a), 21 (R-16), 22, ADR-041 (uzupełnienie) |
| C3-04 (ŚREDNIA) | Jedna formuła `0,8 × limit × waga_i / Σ wag` (kolumny `waga` i `pojemnosc` w `limit_szybkosci_modelu`); 1 token = 1 wywołanie HTTP; kubełek workera 16 ≥ maks. fala (4 przy ścisłym oknie 1 s, test G-4); przyjęcie planu przed falą 1 (przewidywany koniec ≤ termin miękki), terminy fal ≥ 2, pierwszeństwo fal ≥ 2; porty `przyjmij_plan`, `zarezerwuj_wywolanie`, `zwolnij`; testy jednostkowe (a)–(e); symulacja profili A–D: przy A-03 0 Ocen do modelu zapasowego (wersja 2.2: 18%), S-6 ≤ 26,2 s, S-6b w budżecie SLO; A-01 z tolerancją 75 wywołań w 1 s | 2 (A-01), 3.3 (Przyjęcie planu), 7.3 (`limit_szybkosci_modelu`), 9.9, 13.1 (`PrzyjeciePlanu`, `OgranicznikSzybkosci`), 13.2, 14.1 (E-33), 17.2 (S-6, S-6b, budżet SLO), 17.3, 17.4 (symulacja), 17.6 (G-4), 21 (R-21), 22, ADR-039 (uzupełnienie) |
| C3-05 (ŚREDNIA) | Kroki wstawione w liczniku odsetka znaków (mianownik: wszystkie kroki agenta), limit długości kroku wstawionego ≤ min(200, max(60, ⌈1,5 × mediana⌉)) z przykładem; obszar kroku wstawionego między sąsiednimi krokami; autozapis PWA (debounce 2 s, zapis przed zatwierdzeniem), deduplikacja Sprawdzenia tylko wśród `OCZEKUJE` z podmianą ładunku (C-38); osobna pula `S`, wejście z wycinków i zdjęcia 1024 px, A-08: p95 ≤ 4 s i limit ≥ 150/min; rachunek braku rezultatu (≈ 0,1% przy `G ≈ 0`, przyjęte ≤ 0,5%); pojemność dostawcy w szczycie 117/min ≤ 120/min; T-7 wyprowadzone z rozkładu (2,5–3,1%, model 3%); G-2: „wstawienia rozwiązania” ≥ 95%, fałszywe alarmy ≤ 5% | 2 (A-08), 7.3 (`praca_w_tle.pula`, indeksy deduplikacji), 9.4, 10.3.1 (`PoprawkaDoSprawdzenia.obszar`), 10.3.3, 12.2, 12.5, 13.3, 13.4, 15.2 (C-38), 17.2, 17.3, 17.4, 17.6 (G-2), 17.7, 21 (R-14), 22, ADR-036 (uzupełnienie) |
| C3-06 (ŚREDNIA) | Praca `SPRZATAJ_OSIEROCONE_OBIEKTY` dla `zbior-treningowy/`, `zdjecia/`, `zdjecia-maskowane/`: tryb `CODZIENNY` (04:30 UTC, okno 1 h) i `ODTWORZENIE` (krok 2a procedury PITR przed startem workerów); koszt przebiegu; metryka i alert; E-02 rozszerzone; G-6 (d) | 13.3, 13.5, 14.1 (E-02, E-38), 16.4 (retencja, procedura odtworzenia p. 2a, magazyn obiektów), 17.3, 17.6 (G-6 d), 21 (R-16), 22, ADR-047 |
| C3-07 (ŚREDNIA) | Reguła transliteracji „żądanie” → „wniosek” / „wywołanie” (3.1 p. 3); `WNIOSEK_O_USUNIECIE` zamiast `ZADANIE_USUNIECIA`, `jev_wywolania_http_total`, parametr `liczba_zadan` usunięty (rezerwacja per wywołanie); test CI z tokenem `zadan*` i listą dozwolonych użyć; „Grupa eksperymentu” i Q-RE-01 bez słowa „wariant”; B-5 ujednolicone z ADR-027 (brakujący krok); typy `ObszarNaZdjeciu` / `ObszarKroku`; „Rejestr Zdarzeń RODO” i bucket `rejestr-zdarzen-rodo` | 3.1 p. 3, 3.3, 3.4 (część A, test CI część 3, B-5), 5.1, 7.3 (`typ_zdarzenia_rodo`), 10.3.1, 10.3.2, 10.4.3, 12.3, 13.1, 16.4, 17.3, ADR-041 (uzupełnienie), D-1 |
| C3-08 (NISKA) | Karencja konta Nauczyciela: natychmiast tylko zwolnienie blokad (szkice zachowane) i eskalacja z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA`, flagi do `USUN_KONTO`; tabela przywrócenia dla Nauczyciela i Administratora (cofnięcie eskalacji oczekujących, Klasy przekazane bez zwrotu — D-15); rozstrzygnięcie i korekta wymagają konta Ucznia `AKTYWNE` (`409 KONTO_UCZNIA_NIEAKTYWNE`, C-37); przywrócenie zleca `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` | 7.3 (`powod_eskalacji`, `weryfikacja.powod_eskalacji`), 9.6.2, 9.6.3, 11.4.3 p. 1, 12.2, 12.3, 12.5, 13.6, 15.2 (C-37), 16.4, 17.6 (G-0, G-6), ADR-040 i ADR-044 (uzupełnienia), D-15 |
| C3-09 (NISKA) | (1) R-02: SLA 3 dni robocze; (2) 12.6: `termin_at` = 2026-11-05T07:00:00Z z rachunkiem `dodaj_dni_robocze`; (3) `LIMIT_OBCIAZENIA_WERYFIKATORA` i parametry 22: minuty na dzień roboczy; (4) `WYCZYSC_DANE_TECHNICZNE` usuwa `zdarzenie_rodo` po 2 latach; (5) C-07 z SQL podjęcia 2.2+ (`podjeta_at + 2 h`, konto `AKTYWNE`); (6) Wstrzymanie w doborze w tabeli `wstrzymanie_w_doborze` poza `zadanie.version` (ADR-029), C-36 przepisany | (1) 21; (2) 12.6; (3) 12.2, 22; (4) 13.3; (5) 15.2; (6) 3.3, 7.2, 7.3, 8.4, 10.1.2 krok 7, 12.3, 12.4, 12.5, 13.6, 15.2 (C-36), 18.1, ADR-029 (uzupełnienie), ADR-038 |

Zmiany przekrojowe wersji 2.3: nowe typy `zrodlo_zdarzenia_rodo`,
`typ_bezpiecznika`, `powod_eskalacji`; nowe tabele `stan_bezpiecznika`,
`wstrzymanie_w_doborze`; nowe kolumny `zdarzenie_rodo.zrodlo`,
`weryfikacja.powod_eskalacji`, `krok_transkrypcji.podejrzenie_danych_osobowych`,
`tylko_dane_osobowe`, `zastapienia_danych_osobowych`,
`transkrypcja.konflikt_filtra`, `limit_szybkosci_modelu.waga`, `pojemnosc`;
usunięta kolumna `zadanie.wstrzymane_w_doborze`; zmieniona wartość
`ZADANIE_USUNIECIA` → `WNIOSEK_O_USUNIECIE`; nowa pula `SPRAWDZANIE`; nowa
praca w tle `SPRZATAJ_OSIEROCONE_OBIEKTY`; nowe endpointy `GET
/admin/bezpieczniki`, `POST /admin/bezpieczniki/zdjecie` (od wersji 2.4 `POST /admin/bezpieczniki/dezaktywacja`, C4-04); nowe kody błędów
`KONTO_UCZNIA_NIEAKTYWNE`, `BEZPIECZNIK_NIEAKTYWNY`; macierz błędów do
E-38; scenariusze współbieżności do C-38; scenariusz budżetu S-6b; ryzyka
R-19 … R-21; ADR-046, ADR-047 oraz uzupełnienia ADR-007, ADR-029, ADR-036,
ADR-039 … ADR-042, ADR-044; decyzja G-0 D-15; PZS-1: 83 pojęcia w części A;
słowa ogólne dopisane do `tests/slownik_tokeny.yaml`: `sprzataj`,
`osierocone`, `obiekty`, `wniosek`, `wywolania`, `pojemnosc`, `waga`,
`okno`.

## 23.7 Iteracja 6 (2026-11-01, wersja 2.4) — zamknięcie uwag C4-01 … C4-06

Zaadresowano wszystkie 6 otwartych uwag z `docs/CRITIQUE_LOG.md`, iteracja 4
(4 średnie, 2 niskie), w tym wszystkie 10 podpunktów C4-06. Dwie decyzje
wymagają właściciela produktu (D-16, D-17; opcje domyślne w 11.5.3 i 17.7).
Odstępstwa od sugerowanych poprawek: C4-02 — oprócz `prog_nowy = max(wybór,
prog_obecny + 0,02)` i zakazu obniżania dodano tryb pełnej weryfikacji przy
wyzwoleniu na 0,990 (bez niego reakcja na degradację jednorodną kończy się na
0,990 z błędem > 2%), a symulacja wykazała, że okno 30 dni wykrywa nagłą
degradację w medianie 13 dni (nie 2–4) — skorygowano ADR-046 i R-20, a okno
7 dni odrzucono z rachunkiem; C4-04 — `dezaktywowany_przez` zamiast
`dezaktywowal_id` (konwencja `utworzony_przez`, `podjeta_przez`), a
„wstrzymanie” dostało jedną definicję zamiast zmiany nazw; C4-05 — pojemność
`api` stała (1), a workery dzielą resztę `B_całk` proporcjonalnie do wagi,
co przy 2 workerach zachowuje kubełek 16 i wyniki S-6.

| ID uwagi | Rozwiązanie | Miejsce rozwiązania |
|---|---|---|
| C4-01 (ŚREDNIA) | `ponow_zdarzenia_rodo`: kryterium zastosowania zależne od źródła — `API`: wiersz `zdarzenie_rodo`; `WORKER`: brak wiersza `uzytkownik` (PITR w trakcie `USUN_KONTO` daje wiersz zdarzenia i pełne dane konta); cały prefiks `wykonane/` bez filtra czasu; ponowienie każdego zdarzenia według `dane.rola` tymi samymi funkcjami domenowymi co `api` i `USUN_KONTO` (kroki (1)–(5) Nauczyciela, skutki `KontoUsuwane` / `KontoPrzywrocone`, tabela przywrócenia właściwa dla roli); `rola` w każdym Zdarzeniu RODO (CHECK); G-6 (a3) PITR po krokach 1–4 i (a4) konto Nauczyciela; scenariusz C-39 | 7.3 (`zdarzenie_rodo.dane`, CHECK), 13.3 (`USUN_KONTO`), 13.6 (`KontoUsuwane`, `KontoPrzywrocone`), 15.2 (C-39), 16.4 (wniosek p. 1, krok 0, procedura odtworzenia p. 2, konto Nauczyciela p. (1)), 17.3, 17.6 (G-6), 21 (R-16), ADR-041 (uzupełnienie it. 6) |
| C4-02 (ŚREDNIA) | „Wynik dnia KALIBRUJ”: kroki 1–6 w jawnej kolejności — wybór progu, ocena bezpieczników na progu i kalibratorze sprzed przebiegu, Blokada obniżania progu (wejście przy wyzwoleniu, wyjście przy UB ≤ 2% i `n_eff ≥ 189` powyżej bieżącego progu), dozwolone obniżenie z testem 30 dni powyżej progu docelowego, `p_nowy = max(p_dol, p0 + 0,02)` przy wyzwoleniu, 0,990 → 1,000, jeden rekord `prog_pewnosci`; `stan_bezpiecznika.aktywny` dla `TRAFNOSC`, `wyjscie_z_blokady_at`; testy (g)–(k), w tym „degradacja 4% przez 30 dni na tle 60 dni z błędem 0,5% → próg niemalejący”; symulacja dynamiki (trajektoria progu i udział auto: 67% → 38% → 15% → 0% przy degradacji jednorodnej, próg niemalejący w 100% przebiegów) | 3.3 (Blokada obniżania progu), 7.3 (`stan_bezpiecznika`), 9.8, 11.5.3, 12.4 (`StanBezpiecznikaWyjscie`), 13.3 (`KALIBRUJ`), 14.1 (E-21), 17.3, 17.7, 21 (R-20, R-22), 22, 23.4 (D-16), ADR-046 (konsekwencje, uzupełnienie it. 6) |
| C4-03 (ŚREDNIA) | Reguła (a): imię i nazwisko z konta, inicjały i oznaczenie klasy nie są treścią matematyczną („Jan Pole” → `tylko_dane_osobowe`, bez `konflikt_filtra`); reguła (b): token Słownika równy nazwisku zastępowany przy sąsiedztwie z drugim członem lub inicjałem (b1) albo w kroku agenta w ramce (b2); Poprawki: reguła (a) nie jest liczona ponownie, krok przywrócony z `tylko_dane_osobowe` nie jest maskowany i podlega limitom 9.4, Sprawdzeniu zgodności i T-7, `konflikt_filtra` tylko z kroków niezmienionych; G-2: osobne kryterium dla ≥ 30 kartek z nazwiskiem-terminem; metryka `uczniowie_z_konfliktem_filtra_udzial` z alertem | 3.3 (Filtr, Słownik terminów matematycznych), 7.3 (`transkrypcja.konflikt_filtra`), 10.3.1 p. 3a, 11.1 p. 7d, 14.1 (E-37), 17.3, 17.6 (G-2), 17.7, 21 (R-19), 22, ADR-042 (uzupełnienie it. 6) |
| C4-04 (ŚREDNIA) | „Dezaktywacja” zamiast „zdjęcie” dla bezpiecznika: `POST /admin/bezpieczniki/dezaktywacja` (oba typy, `DezaktywacjaBezpiecznikaWejscie`), kolumny `dezaktywowany_przez`, `dezaktywowano_at`, `uzasadnienie_dezaktywacji`, DTO `dezaktywowano_at`; „odebranie” flag i „wyłączenie” flagi `diagnostyczne` w prozie; definicja „wstrzymania” (jedno znaczenie) w 3.1 p. 5; nowe pojęcia PZS-1 (86); test CI: słowa zarezerwowane `zdje*`, `wstrzym*` z listami dozwolonych użyć i przypadek kontrolny `zdjeto_at` | 3.1 p. 5, 3.3, 3.4 (część A, test CI części 2 i 3), 7.3 (`stan_bezpiecznika`), 8.4, 11.5.2, 11.5.3, 12.2 (`BEZPIECZNIK_NIEAKTYWNY`, `PULA_DIAGNOSTYCZNA_ZA_MALA`), 12.3, 12.4, 16.4, 23.4 (D-1), ADR-016 (uzupełnienie it. 6), ADR-040, ADR-044 |
| C4-05 (NISKA) | Łączna pojemność kubełków `B_całk` (34 / 10) dzielona między wiersze (`api` 1, pozostałe proporcjonalnie do wagi), przydział `min(formuła, 0,8 × limit − Σ pozostałych)`, publikacja `przydzial`/`pojemnosc` pod blokadą doradczą, zmniejszenie przed i zwiększenie po `COMMIT`, nowy proces z pustym kubełkiem i odnowieniem co 2 s; rachunek granicy dla 2 / 3 / 4 workerów: 74 / 72 / 74 w 1 s (wersja 2.3: 74 / 90 / 106) i ≤ 234 w 5 s; testy (f)–(h), G-3 (c), scenariusz C-40 | 2 (A-01), 7.3 (`limit_szybkosci_modelu`), 13.2, 15.2 (C-40), 17.2, 17.3, 17.4, 17.6 (G-3, G-4), 21 (R-21), 22, ADR-039 (uzupełnienie it. 6) |
| C4-06 p. 1 | `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` w puli S | 10.3.3 |
| C4-06 p. 2 | `UZGODNIJ_REJESTR_RODO` co 5 min z wiekiem > 5 min → okno unieważniania ≤ 10 min, zgodne z 16.4, ADR-041 i G-6 | 13.3, 13.5, 16.4, 21 (R-16), 22, ADR-041 |
| C4-06 p. 3 | Klucz `USUNIECIE_WYKONANE` bez daty: `rejestr-zdarzen-rodo/wykonane/{uzytkownik_id}/{id}.json`; `If-None-Match: *` działa przy ponowieniu w dowolnym dniu | 7.3 (`zdarzenie_rodo.klucz_obiektu`), 13.3, 16.4 (krok 0, procedura p. 2), 22, ADR-041 |
| C4-06 p. 4 | `SELECT status FROM uzytkownik WHERE id = :uczen_id FOR SHARE` po blokadzie `rozwiazanie` i `weryfikacja` w rozstrzygnięciu i korekcie; kolejność blokad bez zakleszczenia | 9.6.2, 9.6.3, 15.2 (C-37) |
| C4-06 p. 5 | Rozmiar testu nazwany wprost: 2,6–7,8% na ocenę przy e = 2% (antykonserwatywna granica Wilsona przy małym `n`), P(≥ 1 wyzwolenie w 90 dniach) 31–45% zależnie od napełnienia okna, 1,1 fałszywego podniesienia progu na kwartał, 11% kwartałów w trybie pełnej weryfikacji | 11.5.3, 17.7, 21 (R-22), 23.4 (D-16), ADR-046 |
| C4-06 p. 6 | Eskalacje `KARENCJA_KONTA_NAUCZYCIELA` poza licznikiem `odsetek_eskalacji_7d` (per Klasa i globalnie); Klasy Nauczyciela w karencji pod kontrolą przyjęć wprost do przywrócenia albo Przekazania Klasy | 3.3 (Kontrola przyjęć), 11.4.3, 13.6, 22, ADR-043 |
| C4-06 p. 7 | Wariant 17.7 „aktywny bezpiecznik poprawek”: start +249 min/dobę (`M` = 3 951, 105 min na Nauczyciela, 20 weryfikatorów), faza ustalona +451 min (`M` = 3 277, 16 weryfikatorów); G-7 = 18 z zapasem 1,22 i kontrolą przyjęć (D-17) | 17.6 (G-7), 17.7, 23.4 (D-17), ADR-043 |
| C4-06 p. 8 | Klucz `limit_szybkosci_modelu (konfiguracja_modelu_id, wykonawca, pula)` z wierszami R (waga 1,0) i S (0,3) dla roli `ROZPOZNAWANIE_PISMA`; `B_całk = 30` dla rozpoznawania | 7.3, 13.4, 22, ADR-039 |
| C4-06 p. 9 | Popyt 28 wywołań na Ocenę (plan 16 + 11 + 1) w S-6, 17.4 i symulacji; 29 wyłącznie jako maksimum pytań; oszacowanie S-6 dla planów 29 (t_O 9,5 s) | 17.2 (S-6), 17.4, 22, ADR-039 |
| C4-06 p. 10 | Okno na rezultat Sprawdzenia stałe `zatwierdzono_at + 6 s`, niezależne od `t_R`; rachunek p_brak ≤ 0,5% dla każdego `t_R` | 9.4, 17.2, 22, ADR-036 (uzupełnienie it. 6) |

Zmiany przekrojowe wersji 2.4: nowe kolumny `limit_szybkosci_modelu.pula`,
`przydzial`, `stan_bezpiecznika.wyjscie_z_blokady_at`; zmienione nazwy
kolumn `stan_bezpiecznika.zdjety_przez` → `dezaktywowany_przez`, `zdjeto_at`
→ `dezaktywowano_at`, `uzasadnienie_zdjecia` → `uzasadnienie_dezaktywacji`;
zmieniony klucz główny `limit_szybkosci_modelu`; usunięty CHECK `typ =
'POPRAWKI' OR NOT aktywny`; nowy CHECK `zdarzenie_rodo.dane ? 'rola'`; nowy
prefiks `rejestr-zdarzen-rodo/wykonane/`; endpoint `POST
/admin/bezpieczniki/zdjecie` zastąpiony przez `POST
/admin/bezpieczniki/dezaktywacja` (typ `DezaktywacjaBezpiecznikaWejscie`);
cykl `UZGODNIJ_REJESTR_RODO` 5 min; scenariusze współbieżności do C-40;
ryzyko R-22; uzupełnienia ADR-016, ADR-036, ADR-039, ADR-041, ADR-042,
ADR-043, ADR-046; decyzje G-0 D-16, D-17; PZS-1: 86 pojęć w części A; słowa
ogólne dopisane do `tests/slownik_tokeny.yaml`: `dezaktywacja`,
`dezaktywowany`, `dezaktywowano`, `dezaktywacji`, `blokada`, `obnizania`,
`przydzial`, `laczna`, `wykonane`, `podpisu`; nowy plik symulacji
`tests/kalibracja/symulacja_bezpiecznika.py --dynamika` (założenia w 11.5.3).
