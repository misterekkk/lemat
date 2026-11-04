# TASKS.md — backlog implementacji Lemat (cel: działające MVP jak najszybciej)

Backlog został wygenerowany od zera dla stosu 3.x (`backend/` .NET 10, `frontend/` Next.js, ADR-048 … ADR-075;
18.5 l, m). Poprzedni plan (Python/FastAPI) jest nieaktualny i został usunięty.

**Zasady korzystania.** Pozycje to `TASK-XXX` (słowo „zadanie” jest zarezerwowane dla pojęcia Zadanie).
Jedno TASK = jedna sesja agenta `developer` = jeden commit (`CLAUDE.md` pkt 9). Kolejność: w każdej fazie
najpierw wszystkie `[MVP]`, potem `[POST-MVP]`; numery rosną w kolejności dokumentu. Wybierz najniższe `[ ]`,
którego zależności są `[x]`. Pod ukończonym TASK `handoff-guardian` dopisuje: `Pliki:` (dokładne ścieżki),
`Weryfikacja:` (wyniki komend) i zmienia `[ ]` na `[x]`. Pozycje `[POST-MVP]` startują dopiero po ukończeniu
wszystkich `[MVP]` (chyba że właściciel zdecyduje inaczej). Uproszczenia MVP (U-xx) opisuje `HANDOFF.md`.
**Priorytet właściciela: najpierw POC** (sekcja „POC (priorytet właściciela)”, TASK-121 … 140, kontrakt `docs/POC_CONTRACT.md`); pozostałe zadania MVP po POC.

**Tagi:** `[MVP]` — wchodzi do pierwszego działającego end-to-end produktu; `[POST-MVP]` — reszta architektury.

**Podsumowanie:** 120 pozycji, w tym **60 `[MVP]`** i 60 `[POST-MVP]`; ponadto **20 pozycji POC** (TASK-121 … 140; sekcja „POC (priorytet właściciela)” poniżej — **wykonywane w pierwszej kolejności**, nie wchodzą do liczników).

| Faza | `[MVP]` | `[POST-MVP]` | Zakres |
|---|---|---|---|
| 1. Szkielet solucji .NET i aplikacji Next.js, środowisko | 7 (TASK-001 … 007) | 5 (008 … 012) | solucja, konfiguracja, compose, CI-skrypty |
| 2. Model domenowy i baza | 16 (013 … 028) | 9 (029 … 037) | DDL jako migracje, encje, reguły, dane startowe |
| 3. Agenci AI w .NET i narzędzia (Mock domyślnie) | 14 (038 … 051) | 19 (052 … 070) | porty, Mock, Agenci, prace w tle |
| 4. Endpointy API i integracja z Next.js | 19 (071 … 089) | 19 (090 … 108) | REST, SSE, strony |
| 5. Testy i dopracowanie | 4 (109 … 112) | 8 (113 … 120) | integracja, E2E, jakość, wdrożenie |

## Wspólne Definition of Done (obowiązuje KAŻDE TASK)

1. **Nagłówki Living Documentation** (`// @Role … | @DependsOn … | @UsedBy … | @Task: TASK-XXX`) w każdym utworzonym
   lub zmienionym pliku `.cs/.ts/.tsx` (poza generowanymi); weryfikuje test `NaglowekLivingDocumentation` (od TASK-002)
   i `scripts/check-headers.mjs` (od TASK-006).
2. **Weryfikacja zielona** dla dotkniętych części: `dotnet build backend/Lemat.slnx -warnaserror`, `dotnet test`,
   `npm --prefix frontend run typecheck && lint && test && build` (CLAUDE.md pkt 10). Testów nie wyłączamy.
3. **Słownik**: identyfikatory polskie ASCII wg 3.5, nazwy plików angielskie (CLAUDE.md pkt 4); test zakazanych synonimów przechodzi.
4. **Mocki za interfejsem**: zależności zewnętrzne (AI, S3, e-mail) tylko przez porty, domyślnie Mock/lokalne; brak kluczy
   i sekretów w repozytorium; aplikacja działa bez internetu.
5. **Zakres**: nie wykraczaj poza TASK; nie edytuj `docs/` ani `TASKS.md`/`HANDOFF.md`/`docs/CODEBASE_MAP.md` (robi to `handoff-guardian`).
6. **Raport developera** (pliki, wyniki komend, decyzje/odstępstwa, mocki, wskazówki) i aktualizacja dokumentów przez `handoff-guardian`; jeden commit
   `feat(TASK-XXX): … + updated codebase map & handoff`.
7. Każdy przypadek użycia i reguła domenowa ma test jednostkowy (przypadki graniczne); testy bazy — na PostgreSQL 16 (Testcontainers).

---

# POC (priorytet właściciela) — TASK-121 … TASK-140

**Decyzja właściciela (2026-11-01): zakres zawężony do proof of concept.** Do czasu ukończenia POC wykonujemy **wyłącznie** zadania z tej sekcji;
pozostałe `[MVP]` (poza już ukończonymi TASK-001 … 019) czekają. Kontrakt HTTP obu ścieżek: **`docs/POC_CONTRACT.md`** (wersja POC-1.0) — jedyne miejsce, które łączy ścieżki.
Podział pracy, branche, prompty i scenariusz demo: `HANDOFF.md` → sekcja 12.

**Zakres POC:** (1) rejestracja i logowanie Ucznia (JWT), (2) dane Ucznia `/uczniowie/ja` i uproszczony Profil ucznia, (3) lista kilku Zadań z seeda, szczegóły Zadania z KaTeX,
przesłanie zdjęcia kartki, po chwili zmockowana Transkrypcja (z zatwierdzeniem) i Ocena (punkty per Kryterium, uzasadnienie, Wskazówka).
**Poza zakresem:** Test diagnostyczny, Rekomendacje i Ścieżka nauki (ekrany), Weryfikacje, Klasy, Nauczyciel, Administrator, SSE (odpytywanie statusu), Poprawki Ucznia, Zadania zamknięte,
reset hasła, RODO, `Idempotency-Key`, limity szybkości.

**Ścieżki (dwie osoby równolegle, niezależne; zależności tylko wewnątrz ścieżki + kontrakt):**
`[POC-A]` backend = TASK-121 … 130 (katalog `backend/`, `scripts/`, `.env.example`); `[POC-B]` frontend = TASK-131 … 139 (katalog `frontend/`);
`[POC-INT]` = TASK-140 (po obu ścieżkach). Zadania POC **nie wchodzą** do liczników `[MVP]`/`[POST-MVP]` z tabeli „Podsumowanie”; są *częściową realizacją* wskazanych zadań MVP
(tabela niżej) — te zadania MVP pozostają `[ ]` i wykonuje się je później, już na gotowym szkielecie POC (uproszczenia POC to U-21 … U-31 w `HANDOFF.md`).
Numer zadania nie narzuca kolejności między ścieżkami; w obrębie ścieżki numery rosną w kolejności wykonania. Jeden TASK = jedna sesja agenta = jeden commit
(`feat(TASK-121): … + updated codebase map & handoff`); wspólne Definition of Done z początku pliku obowiązuje, a rozdzielenie edycji dokumentów między ścieżki opisuje `HANDOFF.md` 12.2.

| Zadanie POC | Częściowo realizuje (zadania MVP, które po POC zostają do dokończenia) |
|---|---|
| TASK-121 | TASK-071 (infrastruktura HTTP), TASK-003/015 (wpięcie trwałości w hosty) |
| TASK-122 | TASK-025 (Działy), TASK-026 (konta, konfiguracja modelu, próg), TASK-028 (Zadania otwarte) |
| TASK-123 | TASK-072 (rejestracja, logowanie, odświeżanie, wylogowanie) |
| TASK-124 | TASK-073 (`/uczniowie/ja`, `/dzialy`) |
| TASK-125 | TASK-073 (`/zadania`, `/zadania/{id}`) |
| TASK-126 | TASK-020 (Rozwiązanie, Transkrypcja), TASK-021 (Ocena) — część domenowa i EF |
| TASK-127 | TASK-040 (magazyn lokalny, obróbka obrazów), TASK-077 (przesłanie zdjęć, lista, adresy) |
| TASK-128 | TASK-024 (zlecanie i pobieranie prac), TASK-039, 043, 045, 048, 049 (Mock, rozpoznanie, ocena, pula, handlery), TASK-078 (zatwierdzenie, odczyt Oceny) |
| TASK-129 | (rozszerzenie POC; zastąpione przez TASK-075 `GET /uczniowie/ja/profil`) |
| TASK-130 | TASK-109 (test potoku), TASK-112 (`dev-up`) |
| TASK-131 | TASK-081 (klient API, błędy) |
| TASK-132 | (mocki MSW — rozszerzenie POC; w MVP rolę mocków pełni backend na Mockach) |
| TASK-133 | TASK-081 (sesja), TASK-082 (logowanie, rejestracja, układ Ucznia) |
| TASK-134 | TASK-083 (ustawienia Ucznia), TASK-084 (Profil ucznia) |
| TASK-135 | TASK-083/085 (Zadanie z KaTeX) |
| TASK-136 | TASK-085 (przesłanie zdjęć, kompresja) |
| TASK-137 | TASK-086 (postęp, Ocena i Wskazówka), TASK-087 (Transkrypcja — tylko podgląd i zatwierdzenie) |
| TASK-138 | TASK-081/082 (tryby, układ, błędy) |
| TASK-139 | TASK-110 (E2E Playwright) |
| TASK-140 | TASK-110/112 (integracja i scenariusz demo) |

## Ścieżka A — backend `[POC-A]` (TASK-121 … 130)

- [x] **TASK-121 [POC-A] Infrastruktura HTTP (`/api/v1`) i wpięcie trwałości w hosty Api i Worker**
  - **Opis:** W `Lemat.Api`: `Configuration/JsonApi.cs` (`System.Text.Json`: `snake_case` z separacją cyfr przez `KonwencjaNazw`, enumy `UPPER_SNAKE`, `UnmappedMemberHandling.Disallow`, `null` zawsze wysyłane, `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` dla LaTeX; `api.md` 1),
    `Errors/` (podzbiór `KodBledu` z `docs/POC_CONTRACT.md` 1.2, `ProblemDetailsLemat` z `kod`, `trace_id`, `szczegoly`, `bledy_pol`, `type` = `https://lemat.pl/bledy/<kod-kebab>`; `ObslugaBledowWiazania`, `ObslugaWyjatkowDomenowych` mapujący `WyjatekDomenowy`/`TlumaczBledowBazy`; komunikaty po polsku),
    `Strona<T>` + kursor z podpisem HMAC (`LEMAT_KURSOR_KLUCZ`, w Development klucz domyślny), pomocnik `If-Match`/`version` (428/400/412 z `szczegoly.aktualny`), `FiltrScenariuszaMock` (nagłówek `X-Lemat-Mock-Scenariusz` honorowany tylko w `Development`/`E2E`),
    nagłówki `Cache-Control: no-store`, `Vary: Authorization`, `X-Lemat-Wersja-Api`. Wpięcie: `DodajPersystencje` + `DodajModulKonta/Tresci` w `ApiHost`, `DodajPersystencje` w `WorkerHost`, connection string z `LEMAT_BAZA`/`appsettings.Development.json`; dostosowanie `KontenerDiTests` do hostów z bazą (podmiana źródła danych lub `LEMAT_TEST_BAZA`).
    Regeneracja `backend/openapi/lemat-v1.json` (operacje `WithName` wg ADR-049 p. 15).
  - **Zależności:** TASK-003, TASK-004, TASK-014, TASK-015 (ukończone). Kontrakt: `docs/POC_CONTRACT.md` 1.
  - **Realizuje:** 12.1, 12.2, `docs/spec/api.md` 1–2; ADR-060, ADR-062, ADR-067, ADR-069, ADR-065; kontrakt POC 1.1, 1.2.
  - **DoD:** testy `Lemat.Api.Tests` (WebApplicationFactory + PostgreSQL): wyjątek domenowy → ProblemDetails (`type`, `kod`, `status`, `trace_id`, `instance`); nieznane pole JSON → `422 WALIDACJA` z `bledy_pol[0].kod_reguly = POLE_NIEZNANE`; ucięty JSON → `400 ZAPYTANIE_NIEPRAWIDLOWE`;
    podrobiony kursor → `400 KURSOR_NIEPRAWIDLOWY`; `If-Match` brak → `428`, zła forma → `400`, niezgodny → `412` z `szczegoly.aktualny`; rekord testowy serializuje się z `oswiadczenie_wieku_16`, `liczba_wynikow_7d` i enumem `UPPER_SNAKE`; 404/405 z routingu mają `kod`;
    nagłówek Mock ignorowany poza `Development`/`E2E`; `GET /api/v1/zdrowie` nadal `200`; `dotnet run --project backend/src/Lemat.Api` startuje z bazą po `migrate`; build `-warnaserror`, `dotnet test`, `dotnet format --verify-no-changes` zielone.

  - **Pliki (TASK-121):** `backend/src/Lemat.Api/Configuration/{JsonApi,NazwyLemat,ApiHost}.cs`, `backend/src/Lemat.Api/Errors/{ErrorCode,FieldError,ProblemDetailsLemat,ApiException,ErrorCatalog,ProblemWriter,BindingErrorHandling,DomainExceptionHandling}.cs`,
    `backend/src/Lemat.Api/Pagination/{Page,CursorCodec,PageRequest}.cs`, `backend/src/Lemat.Api/Http/{IfMatchHeader,MockScenarioFilter,ResponseHeadersMiddleware}.cs`, `backend/src/Lemat.Api/Endpoints/IEndpointGroup.cs`,
    `backend/src/Lemat.Worker/Configuration/WorkerHost.cs`, `backend/src/Lemat.Domain/Common/NamingConvention.cs`, `backend/openapi/lemat-v1.json`,
    `backend/tests/Lemat.Api.Tests/{HttpInfrastructureTests.cs,Support/ApiFactory.cs,Support/ApiTestDatabaseFixture.cs,Support/TestEndpoints.cs}`.
  - **Weryfikacja (TASK-121):** build `-warnaserror` 0 warn/0 err; `dotnet test` zielone (Api 35, Infrastructure 167, Architecture 106, Domain 394, Application 52); `dotnet format --verify-no-changes` OK; ręcznie `dotnet run Lemat.Api` po `migrate`: `/zdrowie` 200, 404/405 jako problem+json.

- [x] **TASK-122 [POC-A] `Lemat.Tools seed-poc`: Działy, Umiejętności, 5 Zadań z Kryteriami, konta demo, konfiguracja modelu i próg**
  - **Opis:** Polecenie `seed-poc` (idempotentne; odmawia przy `LEMAT_SRODOWISKO=Production`; wypisuje konta demo): 13 Działów z 7.4 (jawne `id` 1 … 13), 5 Umiejętności z kontraktu 6.2, 5 Zadań `OTWARTE_OBLICZENIOWE` `GLOBALNY` `OPUBLIKOWANE` ze stałymi identyfikatorami, treścią (Markdown + LaTeX), `rozwiazanie_wzorcowe`,
    `wynik_koncowy_wzorcowy` i Kryteriami oceniania z `docs/POC_CONTRACT.md` 6.3 (przez agregaty `Zadanie` i repozytoria z TASK-017), 2 konta Uczniów z 6.1 (argon2id; hasher z TASK-123 — do czasu jego powstania prosty `IHasherHasel` w `Lemat.Application.Accounts`, ten sam port), aktywna `konfiguracja_modelu` `mock`
    (role `ROZPOZNAWANIE_PISMA`, `DECYZYJNY_GLOWNY`), kalibrator tożsamościowy i `prog_pewnosci` 0.95 dla `OTWARTE_OBLICZENIOWE` (wymóg CHECK `ocena`, U-10). Dane w plikach osadzonych (`Persistence/Data/poc-exercises.yaml` lub JSON), nie w kodzie.
  - **Zależności:** TASK-121; ukończone TASK-013, 016, 017, 018.
  - **Realizuje:** 7.4, 7.5, `ddl.md` (`dzial`, `umiejetnosc`, `zadanie`, `zadanie_wersja`, `kryterium_oceniania`, `konfiguracja_modelu`, `kalibrator_pewnosci`, `prog_pewnosci`); kontrakt POC 6; FR-12, FR-36 (część).
  - **DoD:** po `migrate` + `seed-poc` na pustej bazie: 13 Działów, 5 Umiejętności, 5 Zadań z identyfikatorami z kontraktu, suma `punkty_max` Kryteriów = `punkty_max` Zadania (`trg_suma_kryteriow` przechodzi), 2 konta `AKTYWNE`; drugie uruchomienie nie duplikuje (test); odmowa w `Production` (test);
    test zbalansowania `$…$` i nawiasów w `tresc_md` i `opis` Kryteriów; hasła demo nie są logowane poza komunikatem wyjściowym polecenia; build/test/format zielone.
  - **Pliki (TASK-122):** `backend/src/Lemat.Application/Accounts/IPasswordHasher.cs`, `backend/src/Lemat.Infrastructure/Security/PasswordHashing.cs`, `backend/src/Lemat.Infrastructure/Persistence/Data/poc-seed.json`,
    `backend/src/Lemat.Infrastructure/Persistence/Seeding/{PocSeedData,PocSeeder}.cs`, `backend/src/Lemat.Tools/Commands/{SeedPocCommand,DatabaseConnection}.cs`,
    `backend/tests/Lemat.Infrastructure.Tests/Persistence/PocSeedTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Security/PasswordHashingTests.cs`;
    zmienione: `backend/src/Lemat.Domain/Content/Exercise.cs`, `backend/src/Lemat.Infrastructure/Lemat.Infrastructure.csproj`, `backend/src/Lemat.Tools/{Program.cs,Commands/CommandDispatcher.cs,Commands/MigrujBazeCommand.cs}`.
  - **Weryfikacja (TASK-122):** build `-warnaserror` 0 warn/0 err; `dotnet test` zielone (Architecture 106, Domain 394, Models 1, Api 35, Infrastructure 181 w tym 14 nowych); `dotnet format --verify-no-changes` OK; ręcznie `migrate` + `seed-poc` x2 na pustej bazie (13/5/5/11 kryteriów/2 konta; 2. uruchomienie zera), `LEMAT_SRODOWISKO=Production` -> kod 1.
- [x] **TASK-123 [POC-A] Konta: rejestracja, logowanie, odświeżenie, wylogowanie (JWT ES256, refresh w cookie)**
  - **Opis:** `HaszowanieHasel` (argon2id, pakiet `Konscious.Security.Cryptography.Argon2` z `Directory.Packages.props`), `WystawcaTokenow` (JWT ES256, 15 min, claims `sub`, `rola`, `status`, `jti`, `exp`; klucz z `LEMAT_JWT_KLUCZ`, w Development generowany i zapisywany lokalnie poza repozytorium),
    refresh token w cookie `lemat_rt` (`HttpOnly; SameSite=Strict; Path=/api/v1/auth`, `Secure` przy HTTPS) z rotacją i unieważnieniem rodziny przy ponownym użyciu (tabela `token_odswiezania`, bez okna tolerancji — U-22), uwierzytelnianie JWT Bearer, polityki `Anonimowy`, `Zalogowany`, `Uczen` (status konta `AKTYWNE`),
    handlery `ZarejestrujUzytkownikaHandler` (Uczeń: `uzytkownik`, `uczen`, `profil_ucznia`, `sciezka_nauki` w jednej transakcji; `NAUCZYCIEL` → `422 ROLA_NIEOBSLUGIWANA`), `ZalogujHandler`, `OdswiezSesjeHandler`, `WylogujHandler`, `AccountsEndpoints` (`/auth/*`), `FluentValidation` dla `RejestracjaWejscie`/`LogowanieWejscie`.
  - **Zależności:** TASK-121; ukończone TASK-016.
  - **Realizuje:** 12.3 (`/auth/*`), 16.1, `api.md` 3 (Konta); ADR-020, ADR-057 (część), ADR-071 p. 4, ADR-072; FR-01; kontrakt POC 3.
  - **DoD:** testy integracyjne zgodne z `docs/POC_CONTRACT.md` 3: rejestracja → `201` + `Set-Cookie`, kształt JSON identyczny z przykładem; zajęty e-mail (także w innej wielkości liter) → `409 EMAIL_ZAJETY`; hasło < 10 znaków → `422` z `bledy_pol`; brak oświadczenia wieku → `422 WYMAGANE_OSWIADCZENIE`;
    logowanie poprawne `200`, zły e-mail i złe hasło dają **identyczną** odpowiedź `401 NIEUWIERZYTELNIONY`; odświeżenie rotuje cookie; użycie starego cookie → `401 SESJA_UNIEWAZNIONA` i unieważnienie rodziny (nowe cookie też przestaje działać); wylogowanie `204` czyści cookie; endpoint chroniony bez tokenu `401`, z tokenem wygasłym `401`;
    hasła i tokeny nie trafiają do logów (test); testy jednostkowe hashera i wystawcy; build/test/format zielone.
  - **Pliki (TASK-123):** `backend/src/Lemat.Application/Accounts/{ITokenIssuer,IssuedAccessToken,AuthenticationFailedException,SessionResult,RefreshTokenGenerator,SessionFactory,RegisterUserInput,RegisterUserHandler,LoginInput,LoginHandler,RefreshSessionHandler,LogoutHandler}.cs`, `backend/src/Lemat.Infrastructure/Security/{JwtKey,TokenIssuer,SecurityRegistration}.cs`,
    `backend/src/Lemat.Api/Contracts/Accounts/{RegistrationRole,RegistrationInput,LoginInput,UserFlags,SessionUser,SessionOutput,RegistrationOutput}.cs`, `backend/src/Lemat.Api/Validation/{InputValidation,RegistrationInputValidator,LoginInputValidator}.cs`, `backend/src/Lemat.Api/Security/{AuthorizationPolicies,AuthorizationResultHandler}.cs`, `backend/src/Lemat.Api/Http/CurrentUser.cs`, `backend/src/Lemat.Api/Endpoints/AccountsEndpoints.cs`,
    `backend/tests/Lemat.Api.Tests/Accounts/AuthenticationTests.cs`, `backend/tests/Lemat.Api.Tests/Support/{AuthClient,LogCollector}.cs`, `backend/tests/Lemat.Infrastructure.Tests/Security/{TokenIssuerTests,JwtKeyTests}.cs`, `backend/tests/Lemat.Application.Tests/Accounts/RefreshTokenGeneratorTests.cs`;
    zmienione: `backend/src/Lemat.Domain/Accounts/User.cs`, `backend/src/Lemat.Application/Accounts/{AccountsRegistration,IRefreshTokenRepository,IPasswordHasher}.cs`, `backend/src/Lemat.Application/Common/LematOpcje.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Accounts/RefreshTokenEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Accounts/RefreshTokenRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Security/PasswordHashing.cs`,
    `backend/src/Lemat.Api/Configuration/ApiHost.cs`, `backend/src/Lemat.Api/Errors/{ProblemWriter,DomainExceptionHandling,ErrorCatalog}.cs`, `backend/src/{Lemat.Api,Lemat.Worker,Lemat.Scheduler}/appsettings.json`, `backend/openapi/lemat-v1.json`, `backend/tests/Lemat.Api.Tests/Support/ApiFactory.cs`.
  - **Weryfikacja (TASK-123):** build `-warnaserror` 0 warn/0 err; `dotnet test` zielone (Agents 1, Application 54, Architecture 106, Domain 394, Models 1, Api 61 w tym 26 nowych, Infrastructure 189 w tym 8 nowych; `LEMAT_TEST_BAZA`, PostgreSQL 16); `dotnet format --verify-no-changes` OK.

- [x] **TASK-124 [POC-A] Uczeń (`/uczniowie/ja` GET i PATCH) i `GET /dzialy`**
  - **Opis:** `StudentsEndpoints`/`AccountsEndpoints`: `GET /uczniowie/ja` (`UczenWyjscie`, `test_diagnostyczny = BRAK`, `klasy = []`), `PATCH /uczniowie/ja` (JSON Merge Patch przez `Opcjonalne<T>` i `OpcjonalneKonwerter`, `Content-Type: application/merge-patch+json`, `If-Match` z `version`, walidacja `null` tylko dla `termin_egzaminu` i `wybrany_dzial_id`, `DZIAL_NIEZNANY`),
    przypadki użycia w `Lemat.Application.Accounts`/`Content` (`PobierzUczniaHandler`, `ZmienUczniaHandler`, odczyt Działów przez `IContentFacade`), `GET /dzialy` (tablica 13 pozycji, `poziom_matury` wg kontraktu 4.2).
  - **Zależności:** TASK-122, TASK-123.
  - **Realizuje:** 12.3 (`/uczniowie/ja`, `/dzialy`), 12.5 (PATCH), `api.md` 3; ADR-069, ADR-072; FR-02 (część), FR-09 (część); kontrakt POC 4.1, 4.2.
  - **DoD:** testy integracyjne: `GET` zgodny z przykładem; `PATCH` ustawia `poziom_matury`, `termin_egzaminu`, `wybrany_dzial_id` i zwiększa `version` o 1; pole nieobecne nie zmienia wartości; `termin_egzaminu: null` i `wybrany_dzial_id: null` czyszczą; `poziom_matury: null` → `422 NIE_MOZNA_WYCZYSCIC`; nieznany Dział → `422 DZIAL_NIEZNANY`;
    brak `If-Match` → `428`, zły → `412` z `szczegoly.aktualny`; zły `Content-Type` → `415`; dwa równoległe `PATCH` z tym samym `If-Match` — jeden `200`, drugi `412`; `GET /dzialy` zwraca 13 pozycji dokładnie jak w kontrakcie 4.2; bez tokenu `401`; build/test/format zielone.
  - **Pliki (TASK-124):** `backend/src/Lemat.Application/Accounts/{StudentView,GetStudentHandler,ChangeStudentHandler}.cs`, `backend/src/Lemat.Api/Contracts/Common/{Optional,OptionalConverter,CurriculumSectionOutput}.cs`, `backend/src/Lemat.Api/Contracts/Accounts/{StudentOutput,StudentChangeInput}.cs`,
    `backend/src/Lemat.Api/Configuration/OptionalOpenApiTransformer.cs`, `backend/src/Lemat.Api/Endpoints/{StudentsEndpoints,ContentEndpoints}.cs`, `backend/tests/Lemat.Api.Tests/Accounts/StudentEndpointsTests.cs`, `backend/tests/Lemat.Api.Tests/Content/SectionsEndpointsTests.cs`, `backend/tests/Lemat.Api.Tests/Support/{ReferenceData,AuthorizedClient}.cs`;
    zmienione: `backend/src/Lemat.Domain/Accounts/Student.cs`, `backend/src/Lemat.Application/Accounts/AccountsRegistration.cs`, `backend/src/Lemat.Api/Configuration/ApiHost.cs`, `backend/openapi/lemat-v1.json` (regenerowany).
  - **Weryfikacja (TASK-124):** build `-warnaserror` 0 warn/0 err; `dotnet test` zielone (Agents 1, Application 54, Architecture 106, Domain 394, Models 1, Api 81, Infrastructure 189; `LEMAT_TEST_BAZA`, PostgreSQL 16); `dotnet format --verify-no-changes` OK.

- [x] **TASK-125 [POC-A] Zadania: `GET /zadania` i `GET /zadania/{id}` dla Ucznia**
  - **Opis:** `ContentEndpoints`: `GET /zadania` **[Rozszerzenie POC]** (filtr `dzial_id`, `limit`, `kursor`, sortowanie `poziom_trudnosci`, `id`) i `GET /zadania/{id}` (bez wymaganego kontekstu — U-23) zwracające `ZadanieUczniaWyjscie` z danych Zadania wersji aktualnej (bez Rozwiązania wzorcowego i klucza) przez `IContentFacade`
    (`ExerciseForStudent` z TASK-017); tylko Zadania `OPUBLIKOWANE` i `GLOBALNY`.
  - **Zależności:** TASK-122, TASK-123.
  - **Realizuje:** 12.3 (`/zadania/{id}`), 7.5, `api.md` 3 (Treści); ADR-012, ADR-071 p. 3 (uproszczony); FR-12; kontrakt POC 5, 6.3.
  - **DoD:** test integracyjny: po `seed-poc` odpowiedź `GET /zadania` jest **identyczna** z JSON z `docs/POC_CONTRACT.md` 6.3 (porównanie strukturalne, kolejność 2, 5, 1, 3, 4); `?dzial_id=5` → tylko Zadanie 1; `?dzial_id=999` → pusta lista; `GET /zadania/{id}` zgodny z przykładem z 5.2; nieistniejące, nieopublikowane i niepoprawne UUID → `404 NIE_ZNALEZIONO`;
    odpowiedź nie zawiera pól `rozwiazanie_wzorcowe`, `kryteria_oceniania`, `wynik_koncowy_wzorcowy` (test refleksyjny po nazwach pól); paginacja z `limit=2` i kursorem przechodzi całą listę bez duplikatów; bez tokenu `401`; build/test/format zielone.

  - **Pliki (TASK-125):** `backend/src/Lemat.Api/Contracts/Content/ExerciseForStudentOutput.cs`, `backend/tests/Lemat.Api.Tests/Content/{ExercisesEndpointsTests.cs,zadania-poc.json}`, `backend/tests/Lemat.Api.Tests/Support/PocSeed.cs`;
    zmienione: `backend/src/Lemat.Api/Endpoints/ContentEndpoints.cs`, `backend/src/Lemat.Application/Content/{ExerciseForStudent,IExerciseRepository,IContentFacade,ContentFacade}.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Content/ExerciseRepositoryEf.cs`, `backend/tests/Lemat.Api.Tests/Lemat.Api.Tests.csproj`, `backend/tests/Lemat.Application.Tests/Content/ContentFacadeTests.cs`, `backend/openapi/lemat-v1.json` (regenerowany).
  - **Weryfikacja (TASK-125):** build `-warnaserror` OK; `dotnet test` zielone (Api 97, Infrastructure 189, Domain 394, Architecture 106, Application ok); `dotnet format --verify-no-changes` OK.
- [x] **TASK-126 [POC-A] Domena i trwałość Rozwiązań i Ocen (zakres POC): Rozwiązanie, Zdjęcie, Transkrypcja, Ocena**
  - **Opis:** `Lemat.Domain.Solutions`: agregat `Rozwiazanie` ze stanami POC (`PRZESLANE`, `ROZPOZNAWANIE`, `BLAD_ROZPOZNAWANIA`, `TRANSKRYPCJA_GOTOWA`, `ZATWIERDZONE`, `OCENIANIE`, `OCENIONE`) i przejściami z 7.11 kontraktu (niedozwolone przejście → `WyjatekDomenowy` `ROZWIAZANIE_NIEPRAWIDLOWY_STAN`; `Version` rośnie przy każdym przejściu; znaczniki `transkrypcja_gotowa_at`, `zatwierdzono_at`, `ocena_wystawiona_at`, `czas_systemowy_ms`),
    `ZdjecieRozwiazania`, `Transkrypcja` z `KrokTranskrypcji` (wersja 1 `AGENT_ROZPOZNAJACY`), `Lemat.Domain.Grading`: `Ocena` z `OcenaKryterium` (niezmienna, Z-5) i `WynikZadania` z `Lemat.Domain.Profile` (zapis wyniku z `zrodlo = OCENA`); konfiguracje EF (`Persistence/Configurations/Solutions|Grading`) wg `ef-core.md` i DDL (`rozwiazanie`, `zdjecie_rozwiazania`, `transkrypcja`, `krok_transkrypcji`, `ocena`, `ocena_kryterium`, `decyzja_modelu` przez istniejący port),
    repozytoria i `IRozwiazaniaFasada` w `Lemat.Application.Solutions`. Typy i kolumny zgodne z DDL, tak by TASK-020/021 jedynie rozszerzały (reszta stanów, Poprawki, Weryfikacja).
  - **Zależności:** TASK-121, TASK-122; ukończone TASK-014 … 019.
  - **Realizuje:** 8.1 (podzbiór), 6.7, `ddl.md` (tabele wyżej, `trg_ocena_niezmienna`), `ef-core.md`; Z-4, Z-5; FR-17, FR-19, FR-27 (dane); kontrakt POC 7.11.
  - **DoD:** testy jednostkowe domeny: każde dozwolone przejście z diagramu i **każde niedozwolone** (parametrycznie), `Version`, brak `DateTime.Now`; testy bazy (PostgreSQL 16): zapis i odczyt agregatu z zdjęciami i Transkrypcją, CHECK-i DDL (np. `CHECK ocena AUTOMATYCZNA` wymaga progu i kalibratora), `trg_ocena_niezmienna` blokuje `UPDATE` Oceny, `UNIQUE (rozwiazanie_id, wersja)`;
    testy architektury (warstwy, słownik, nagłówki) zielone; `DodajModulRozwiazan`/`DodajModulOceniania` zarejestrowane w hostach; build/test/format zielone.
  - **Pliki (TASK-126):** `backend/src/Lemat.Domain/Solutions/{SolutionId,TranscriptionId,SolutionStatus,RecognitionErrorCode,TranscriptionSource,SolutionPhoto,Solution,TranscriptionStep,Transcription}.cs`, `backend/src/Lemat.Domain/Grading/{GradeId,GradeStatus,GradeSource,ErrorType,HintSource,GradeVisibility,Hint,GradeConfidence,RawConfidence,GradeCriterion,AutomaticGradeData,Grade}.cs`,
    `backend/src/Lemat.Application/Solutions/{ISolutionRepository,ITranscriptionRepository,SolutionSummaries,ISolutionsFacade,SolutionsFacade,SolutionsRegistration}.cs`, `backend/src/Lemat.Application/Grading/{IGradeRepository,GradeSummaries,IGradingFacade,GradingFacade,GradingRegistration}.cs`,
    `backend/src/Lemat.Infrastructure/Persistence/Configurations/Solutions/{SolutionEntityTypeConfiguration,TranscriptionEntityTypeConfiguration}.cs`, `.../Configurations/Grading/GradeEntityTypeConfiguration.cs`, `.../Repositories/Solutions/{SolutionRepositoryEf,TranscriptionRepositoryEf}.cs`, `.../Repositories/Grading/GradeRepositoryEf.cs`,
    `backend/tests/Lemat.Domain.Tests/Solutions/{SolutionsTestData,SolutionTests,TranscriptionTests}.cs`, `backend/tests/Lemat.Domain.Tests/Grading/GradeTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/SolutionsPersistenceTests.cs`;
    zmienione: `backend/src/Lemat.Infrastructure/Persistence/Conventions/EnumTypeMapping.cs`, `backend/src/Lemat.Infrastructure/Persistence/PersistenceRegistration.cs`, `backend/src/Lemat.Api/Configuration/ApiHost.cs`, `backend/src/Lemat.Worker/Configuration/WorkerHost.cs`.
  - **Weryfikacja (TASK-126):** build `-warnaserror` 0 warn / 0 err; `dotnet test` zielone (Agents 1, Application 55, Architecture 106, Domain 500, Models 1, Api 97, Infrastructure 212); `dotnet format --verify-no-changes` OK. Bez zmian DDL i pakietów.

- [x] **TASK-127 [POC-A] Magazyn lokalny i obróbka obrazów oraz `POST /rozwiazania`, `GET /rozwiazania`, `GET /rozwiazania/{id}`, adres zdjęcia**
  - **Opis:** Porty `IMagazynObiektow` i `IObrobkaObrazow` (`Lemat.Application.Common`/moduł Rozwiązania) i adaptery `MagazynObiektowLokalny` (katalog z `LEMAT_MAGAZYN_SCIEZKA`/`Lemat:Magazyn:Sciezka`, klucze `zdjecia/{uczen_id}/{rozwiazanie_id}/{id}.jpg`, adresy podpisane HMAC ważne 5 min) i `ProstaObrobkaObrazow` (JPEG: magic bytes, ≤ 10 MB, wymiary z nagłówka SOF, `sha256`; U-02, U-25),
    `PrzeslijRozwiazanieHandler` (reguły z kontraktu 7.4: wersja aktualna Zadania, brak nieukończonego Rozwiązania tego Zadania, techniczny `element_sciezki` — U-24; w jednej transakcji `rozwiazanie`, `zdjecie_rozwiazania` (`usun_po` = +90 dni) i zlecenie pracy `ROZPOZNAJ_PISMO` w `praca_w_tle` przez minimalny `IZlecaniePrac`, scenariusz Mock w `ladunek.mock_scenariusz`),
    `SolutionsEndpoints`: `POST /rozwiazania` (multipart `[AsParameters]`, pola `zadanie_wersja_id`, `zdjecia`), `GET /rozwiazania` (filtr `status`, kursor), `GET /rozwiazania/{id}` (`RozwiazanieWyjscie` z `nastepne_odpytanie_ms`), `GET /rozwiazania/{id}/zdjecia/{id}/adres`, `GET /magazyn-lokalny/{token}`; własność zasobu (cudze → `404`).
  - **Zależności:** TASK-123, TASK-125, TASK-126.
  - **Realizuje:** 9.4, 12.3 (`/rozwiazania`), 12.5, `api.md` 3 i ADR-067 (multipart), 6.6 (porty); ADR-024, ADR-052 (część), ADR-056; FR-16; kontrakt POC 7.4 … 7.6, 7.9, 7.10.
  - **DoD:** testy integracyjne: poprawne przesłanie → `202` + `Location`, JSON zgodny z przykładem 7.4 (`PRZESLANE`, `nastepne_odpytanie_ms = 1000`) i wiersz `praca_w_tle` `ROZPOZNAJ_PISMO` w tej samej transakcji (test wycofania: błąd zapisu → brak Rozwiązania i brak pracy); PNG → `415`; 6 plików i 0 plików → `422 ZA_DUZO_ZDJEC`; plik > 10 MB → `413 ZDJECIE_ZA_DUZE`;
    plik nieczytelny jako JPEG → `422 ZDJECIE_NIEPRAWIDLOWE`; plik pod inną nazwą pola → `422 PLIK_POZA_POLEM_ZDJECIA`; stara `zadanie_wersja_id` → `409 WERSJA_ZADANIA_NIEAKTUALNA`; drugie Rozwiązanie tego samego Zadania w trakcie → `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN` z `szczegoly.istniejace_rozwiazanie_id`; Rozwiązanie innego Ucznia → `404` (GET, adres);
    adres zdjęcia działa bez nagłówka `Authorization`, zmieniony token i token po 5 min → `404`; lista: sortowanie, `status`, paginacja kursorem; nagłówek Mock `nieznany` → `422 MOCK_SCENARIUSZ_NIEZNANY`; zdjęcia nie są logowane; build/test/format zielone.
  - **Pliki (TASK-127):** `backend/src/Lemat.Application/Common/BackgroundJobs/{BackgroundJobType,BackgroundJobPool,BackgroundJobCatalog,BackgroundJobRequest,BackgroundJobEnqueueOutcome,BackgroundJobEnqueueResult,IBackgroundJobEnqueuing}.cs`, `backend/src/Lemat.Application/Models/{IObjectStorage,SignedAddress,IImageProcessing,NormalizedImage,InvalidImageException,ISignedAddressReader}.cs`, `backend/src/Lemat.Application/Solutions/SubmitSolutionHandler.cs`,
    `backend/src/Lemat.Infrastructure/Storage/{LocalObjectStorage,ObjectStorageRegistration}.cs`, `backend/src/Lemat.Infrastructure/Sandboxes/SimpleImageProcessing.cs`, `backend/src/Lemat.Infrastructure/BackgroundJobs/SqlBackgroundJobEnqueuing.cs`, `backend/src/Lemat.Api/Endpoints/SolutionsEndpoints.cs`, `backend/src/Lemat.Api/Contracts/Solutions/{SolutionInput,SolutionOutput,SolutionPhotoOutput,TranscriptionOutput,TranscriptionStepOutput,GradeOutput,GradeCriterionOutput,PhotoAddressOutput,SolutionOutputMapper}.cs`, `backend/src/Lemat.Api/Configuration/UtcDateTimeOffsetConverter.cs`,
    `backend/tests/Lemat.Api.Tests/Solutions/SolutionsEndpointsTests.cs`, `backend/tests/Lemat.Api.Tests/Support/{TestJpeg,TestCollections}.cs`, `backend/tests/Lemat.Application.Tests/Solutions/SubmitSolutionHandlerTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Storage/StorageAdaptersTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/BackgroundJobs/EnqueuingTests.cs`;
    zmienione: `.env.example` (`LEMAT_MAGAZYN_KLUCZ`), `backend/openapi/lemat-v1.json` (regenerowany), `backend/src/Lemat.Application/Common/{LematOpcje,LematKonfiguracja,LematOpcjeWalidator}.cs`, `backend/src/Lemat.Application/Grading/{GradeSummaries,IGradingFacade,GradingFacade}.cs`, `backend/src/Lemat.Application/Recommendations/{IRecommendationsFacade,RecommendationsFacade}.cs`, `backend/src/Lemat.Application/Solutions/{ISolutionRepository,ISolutionsFacade,SolutionSummaries,SolutionsFacade,SolutionsRegistration}.cs`,
    `backend/src/Lemat.Infrastructure/Persistence/PersistenceRegistration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Solutions/SolutionRepositoryEf.cs`, `backend/src/Lemat.Api/Configuration/{ApiHost,JsonApi}.cs`, `backend/src/Lemat.Api/Http/ResponseHeadersMiddleware.cs`, `backend/tests/Lemat.Api.Tests/Content/ExercisesEndpointsTests.cs`, `backend/tests/Lemat.Api.Tests/Support/{ApiFactory,AuthorizedClient}.cs`, `backend/tests/Lemat.Application.Tests/Common/LematOpcjeTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/SolutionsPersistenceTests.cs`.
  - **Weryfikacja (TASK-127):** build `-warnaserror` 0 warn / 0 err; `dotnet test` zielone (Agents 1, Application 70, Architecture 106, Domain 500, Models 1, Api 124, Infrastructure 235); `dotnet format --verify-no-changes` OK; `lemat-v1.json` zregenerowany. Bez zmian DDL i pakietów. 413 `ZAPYTANIE_ZA_DUZE` nieprzetestowane integracyjnie (TestServer nie egzekwuje limitu).

- [x] **TASK-128 [POC-A] Zmockowany potok: praca w tle, rozpoznanie i ocena, zatwierdzenie, odczyt Oceny, Wynik zadania**
  - **Opis:** Wybrane najprostsze sensowne uproszczenie (U-26): **jedna pętla** `PocPracaHostedService` w `Lemat.Worker` odpytująca `praca_w_tle` co 500 ms (`FOR UPDATE SKIP LOCKED`, dzierżawa `dzierzawa_do`/`wykonawca`, ponowienia do `maks_prob = 3`, potem `MARTWA`; bez `LISTEN/NOTIFY` i pul R/S/O/P/W), z handlerami idempotentnymi sprawdzającymi stan:
    `RozpoznajPismoHandler` (`PRZESLANE` → `ROZPOZNAWANIE`, opóźnienie `Lemat:Poc:OpoznienieRozpoznaniaMs` = 3000 poza transakcją, zapis Transkrypcji wersja 1 z danych 7.2 kontraktu → `TRANSKRYPCJA_GOTOWA`; scenariusz `rozpoznanie-nieczytelne` → `BLAD_ROZPOZNAWANIA`) i `OcenRozwiazanieHandler` (`ZATWIERDZONE` → `OCENIANIE`, opóźnienie `OpoznienieOcenyMs` = 2000, Ocena i `ocena_kryterium` wg 7.3 z `pewnosc_oceny = 0.995`, po jednym wierszu `decyzja_modelu` na Kryterium (Z-4), `wynik_zadania` (`zrodlo = OCENA`, `ulamek = punkty/punkty_max`, `waga = 1.00`, `liczy_do_trudnosci` przy pierwszej próbie), `OCENIONE`, `ocena_wystawiona_at`, `czas_systemowy_ms`).
    Dane mocka w pliku osadzonym za portem (Mock deterministyczny w `Lemat.Models.Mock`/`Lemat.Agents`, bez wiedzy domeny o implementacji; w `Production` mock kończy start błędem — ADR-055 p. 6). `POST /rozwiazania/{id}/zatwierdzenie` (`If-Match`, `TRANSKRYPCJA_NIEAKTUALNA`, zlecenie `OCEN_ROZWIAZANIE` w tej samej transakcji) i `GET /rozwiazania/{id}/ocena` (404 do czasu `OCENIONE`). Brak Poprawek Ucznia, Filtra, trasowania T-1 … T-7, Weryfikacji, Wskazówki z Agenta (U-26, U-27).
  - **Zależności:** TASK-122, TASK-126, TASK-127.
  - **Realizuje:** 9.4, 9.5, 11.1 (podzbiór), 13.3–13.4 (uproszczone), 15.1, 10.0.6 (Mock); ADR-053 (część), ADR-056, ADR-065, ADR-055 p. 6; FR-17, FR-19, FR-22 (stała pewność), FR-23, FR-27, FR-29 (tylko zapis Wyniku); kontrakt POC 7.1 … 7.3, 7.7, 7.8.
  - **DoD:** test parametryczny dla wszystkich 5 Zadań: po `POST` → ... → `OCENIONE` Transkrypcja (`kroki[].latex`, `pewnosc_odczytu`) i Ocena (`punkty`, `kryteria[]`, `pierwszy_bledny_krok`, `typ_bledu`, `wskazowka`, `uzasadnienie`) są **dokładnie** takie jak w tabelach 7.2 i 7.3; czasy z `TimeProvider`/fałszywego zegara (test nie czeka realnie 5 s); `version` rośnie o 1 przy każdym przejściu;
    handler uruchomiony dwa razy nie duplikuje Transkrypcji, Oceny ani `wynik_zadania`; dwa workery na jednej bazie nie wykonują tej samej pracy (test współbieżności); scenariusz nieczytelny → `BLAD_ROZPOZNAWANIA` z `kod_bledu_rozpoznawania`; ponowne zatwierdzenie → `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN`; zła `wersja` → `409 TRANSKRYPCJA_NIEAKTUALNA`; `GET …/ocena` przed `OCENIONE` → `404`;
    `ocena` w `GET /rozwiazania/{id}` i w `GET …/ocena` identyczne; praca z błędem jest ponawiana, a po wyczerpaniu prób `MARTWA` (test); `Lemat.Worker` startuje (`/zdrowie`); build/test/format zielone.
  - **Pliki (TASK-128):** `backend/src/Lemat.Application/Common/BackgroundJobs/{BackgroundJob,JobStatus,IBackgroundJobHandler,IBackgroundJobQueue,BackgroundJobProcessor,BackgroundJobsRegistration,JobPayloadJson}.cs`, `backend/src/Lemat.Application/Models/{IRecognitionAgent,RecognitionResult,IGradingAgent,GradingAgentResult}.cs`, `backend/src/Lemat.Application/Solutions/{RecognizeHandwritingHandler,ApproveSolutionHandler}.cs`, `backend/src/Lemat.Application/Grading/GradeSolutionHandler.cs`,
    `backend/src/Lemat.Infrastructure/BackgroundJobs/SqlBackgroundJobQueue.cs`, `backend/src/Lemat.Models/Mock/{MockData,MockRecognitionAgent,MockGradingAgent,MockModelsRegistration}.cs`, `backend/src/Lemat.Models/Mock/Data/poc-mock.json`, `backend/src/Lemat.Worker/Jobs/PocJobHostedService.cs`,
    `backend/src/Lemat.Api/Contracts/Solutions/SolutionApprovalInput.cs`, `backend/src/Lemat.Api/Validation/SolutionApprovalInputValidator.cs`,
    `backend/tests/Lemat.Api.Tests/Solutions/SolutionsFlowTests.cs`, `backend/tests/Lemat.Api.Tests/Support/{FakeClock,PocContract,WorkerTestowy}.cs`, `backend/tests/Lemat.Infrastructure.Tests/BackgroundJobs/QueueTests.cs`, `backend/tests/Lemat.Models.Tests/Mock/MockAgentsTests.cs`;
    zmienione: `backend/openapi/lemat-v1.json` (regenerowany, +2 endpointy), `backend/src/Lemat.Application/Common/{LematOpcje,LematOpcjeWalidator}.cs`, `backend/src/Lemat.Application/Common/BackgroundJobs/{BackgroundJobType,BackgroundJobCatalog}.cs`, `backend/src/Lemat.Application/Solutions/SolutionsRegistration.cs`, `backend/src/Lemat.Infrastructure/Persistence/PersistenceRegistration.cs`, `backend/src/Lemat.Models/Lemat.Models.csproj`, `backend/src/Lemat.Worker/Configuration/WorkerHost.cs`, `backend/src/Lemat.Api/Endpoints/SolutionsEndpoints.cs`, `backend/src/Lemat.Api/Contracts/Solutions/SolutionOutputMapper.cs`, `backend/src/Lemat.Api/Configuration/ApiHost.cs`, `backend/tests/Lemat.Api.Tests/Lemat.Api.Tests.csproj`
  - **Weryfikacja (TASK-128):** build `-warnaserror` 0 warn / 0 err; `dotnet test` zielone (Agents 1, Application 70, Architecture 106, Domain 500, Models 12, Api 135, Infrastructure 242; `LEMAT_TEST_BAZA` lokalny PG16); `dotnet format --verify-no-changes` OK; `lemat-v1.json` zregenerowany. Bez zmian DDL i pakietów.

- [x] **TASK-129 [POC-A] Uproszczony Profil ucznia: `GET /uczniowie/ja/profil-uproszczony`**
  - **Opis:** **[Rozszerzenie POC]** Zapytanie w `Lemat.Application.Profile` (`PobierzProfilUproszczonyHandler`) agregujące po Ocenach `OSTATECZNA` Rozwiązań w statusie `OCENIONE` Ucznia: `liczba_rozwiazan` (bez `ANULOWANE`, `PORZUCONE`), `liczba_ocenionych`, sumy punktów, `sredni_procent_punktow` (1 miejsce po przecinku), rozbicie na Działy i 5 ostatnich Ocen; endpoint w `ProfileEndpoints`. Bez aktualizacji Elo (U-28).
  - **Zależności:** TASK-124, TASK-128.
  - **Realizuje:** kontrakt POC 4.3; FR-04 (uproszczone), FR-29 (odczyt).
  - **DoD:** testy: nowy Uczeń → `liczba_rozwiazan = 0`, `sredni_procent_punktow = null`, puste listy; po ocenieniu Zadań 1 (2/2), 2 (1/2), 5 (0/2) i Zadania 3 w trakcie wynik **równy** przykładowi z 4.3 (3/6, 50.0, trzy Działy, kolejność `ostatnie_oceny`); Rozwiązanie w trakcie liczy się do `liczba_rozwiazan`, ale nie do `liczba_ocenionych`; dane innego Ucznia nie wpływają na wynik; bez tokenu `401`; build/test/format zielone.
  - **Pliki (TASK-129):** `backend/src/Lemat.Application/Profile/{ISimplifiedProfileReader,GetSimplifiedProfileHandler}.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Profile/SimplifiedProfileReaderEf.cs`, `backend/src/Lemat.Api/Contracts/Profile/SimplifiedProfileOutput.cs`, `backend/src/Lemat.Api/Endpoints/ProfileEndpoints.cs`, `backend/tests/Lemat.Api.Tests/Profile/SimplifiedProfileEndpointTests.cs`;
    zmienione: `backend/openapi/lemat-v1.json` (regenerowany, +1 endpoint), `backend/src/Lemat.Application/Profile/ProfileRegistration.cs`, `backend/src/Lemat.Infrastructure/Persistence/PersistenceRegistration.cs`, `backend/src/Lemat.Api/Configuration/ApiHost.cs`.
  - **Weryfikacja (TASK-129):** build `-warnaserror` 0 warn / 0 err; `dotnet test` zielone (Agents 1, Application 70, Architecture 106, Domain 500, Models 12, Api 138, Infrastructure 242); `dotnet format --verify-no-changes` OK. Bez zmian DDL i pakietów.

- [x] **TASK-130 [POC-A] Test integracyjny przepływu POC i skrypt `scripts/dev-up.sh`**
  - **Opis:** `Lemat.Api.Tests/Poc/PocFlowTests`: `WebApplicationFactory` + `PocPracaHostedService` w procesie testu na tej samej bazie (PostgreSQL 16), sterowany zegar i skrócone opóźnienia; scenariusz: rejestracja → `PATCH /uczniowie/ja` → `GET /zadania` → `POST /rozwiazania` (JPEG z zasobu testowego) → odpytywanie `GET /rozwiazania/{id}` do `TRANSKRYPCJA_GOTOWA` → zatwierdzenie → odpytywanie do `OCENIONE` → `GET …/ocena` → `GET /uczniowie/ja/profil-uproszczony`; oraz drugi Uczeń nie widzi cudzych Rozwiązań (`404`).
    `scripts/dev-up.sh` (z opcjami `--stop`, `--reset`): PostgreSQL (`db-up.sh` albo lokalny — wg `.env`), `Lemat.Tools migrate`, `seed-poc`, `Lemat.Api` (5080) i `Lemat.Worker` w tle (logi w `.logs/`, ignorowane przez git; PID-y do `--stop`), czeka na `GET /api/v1/zdrowie`, wypisuje adresy i konta demo; `scripts/poc-smoke.sh` (curl: pełny przepływ z kontraktu na działającym API — używany przez TASK-140). Aktualizacja `.env.example` (nowe klucze `Lemat:Poc:*`, `LEMAT_JWT_KLUCZ`, `LEMAT_MAGAZYN_SCIEZKA`), sekcja „Szybki start POC” w `README.md`.
  - **Zależności:** TASK-128, TASK-129.
  - **Realizuje:** kontrakt POC (całość), 17.5 (część), CLAUDE.md pkt 7, 10; TASK-109 i TASK-112 (część).
  - **DoD:** `PocFlowTests` zielony w < 60 s; wszystkie kształty odpowiedzi zgodne z przykładami kontraktu; na czystym środowisku `scripts/dev-up.sh` uruchamia całość jednym poleceniem, `scripts/poc-smoke.sh` kończy się kodem 0 i wypisuje punkty z Oceny, `--stop` zatrzymuje procesy; instrukcje z README wykonane krok po kroku (zapis w raporcie);
    `scripts/verify-backend.sh` zielone; **backend POC kompletny** — `handoff-guardian` zapisuje w `HANDOFF.md` 12.4 stan ścieżki A.
  - **Pliki (TASK-130):** `backend/tests/Lemat.Api.Tests/Poc/PocFlowTests.cs`, `scripts/dev-up.sh`, `scripts/poc-smoke.sh`, `.env.example`, `README.md` (Szybki start POC), `.gitignore` (`.logs/`);
  - **Weryfikacja (TASK-130):** `scripts/verify-backend.sh` zielone (Domain 500, Application 70, Models 12, Agents 1, Architecture 106, Api 139, Infrastructure 242; format OK); `PocFlowTests` ~7 s; `dev-up.sh` + `poc-smoke.sh` (kod 0, Ocena 0/2 pkt dla Zadania 5, izolacja `uczen2` 404) + `--stop` zweryfikowane ręcznie; `--reset` NIEZWERYFIKOWANE (`docker compose down -v` zablokowane przez klasyfikator uprawnień). Bez zmian DDL i pakietów.

## Ścieżka B — frontend `[POC-B]` (TASK-131 … 139)

Reguły wspólne: wszystkie teksty UI w `frontend/messages/pl-PL.json` (reguła `react/jsx-no-literals`), tylko `lib/api/client.ts` woła `/api/*`, token dostępu tylko w pamięci, wzory przez `MarkdownContent`/`Formula` (KaTeX, `throwOnError: false`),
segmenty tras i katalogi `features/*` angielskie wg CLAUDE.md pkt 4.B (`(public)/login`, `(public)/register`, `(student)/exercises`, `(student)/solutions`, `(student)/profile`, `(student)/settings`), nagłówki Living Documentation w każdym `.ts/.tsx`, `npm run check:headers`. Praca wyłącznie na kontrakcie `docs/POC_CONTRACT.md` i mockach MSW — backend nie jest potrzebny aż do TASK-140.

- [x] **TASK-131 [POC-B] Typy kontraktu, klient API i obsługa błędów (`lib/api`)**
  - **Opis:** `src/lib/api/types.ts` (ręcznie utrzymywane typy z `docs/POC_CONTRACT.md` 3 … 7: `SesjaWyjscie`, `UczenWyjscie`, `ZmianaUczniaWejscie`, `ProfilUproszczonyWyjscie`, `DzialWyjscie`, `ZadanieUczniaWyjscie`, `RozwiazanieWyjscie`, `TranskrypcjaWyjscie`, `OcenaWyjscie`, `AdresZdjeciaWyjscie`, `Strona<T>`, enumy `UPPER_SNAKE` jako unie literałów; nazwy i pola ze słownika, U-30),
    `errors.ts` (`ProblemDetailsLemat`, `BladApi` z `status`, `kod` — także kody klienta `BLAD_SIECI`, `PRZERWANO`, `BLAD_BRAMY` — `problem`, `bledyPol`), `client.ts` (jedyne miejsce `fetch` do `/api/v1`: wstrzykiwany dostawca tokenu, `Authorization: Bearer`, `cache: "no-store"`, jednokrotne odświeżenie i ponowienie po `401 NIEUWIERZYTELNIONY` przez wstrzyknięty `odswiezSesje`, nagłówek `If-Match: "<version>"`, `multipart/form-data`, `application/merge-patch+json`, opcjonalny `X-Lemat-Mock-Scenariusz`, mapowanie odpowiedzi bez `application/problem+json` na `BLAD_BRAMY`),
    typowane funkcje endpointów (`zarejestruj`, `zaloguj`, `odswiez`, `wyloguj`, `pobierzUcznia`, `zmienUcznia`, `pobierzProfilUproszczony`, `pobierzDzialy`, `pobierzZadania`, `pobierzZadanie`, `przeslijRozwiazanie`, `pobierzRozwiazanie`, `pobierzRozwiazania`, `zatwierdzRozwiazanie`, `pobierzOcene`, `pobierzAdresZdjecia`), klucze komunikatów `bledy.<KOD>` w `messages/pl-PL.json` dla kodów z 1.2, zależności `zod`, `react-hook-form`, `@hookform/resolvers` (stos z CLAUDE.md pkt 2).
  - **Zależności:** TASK-006 (ukończone). Kontrakt: `docs/POC_CONTRACT.md` 1 … 7.
  - **Realizuje:** 12.11, `api.md` 6.1 (uproszczone), ADR-054, ADR-061 (część); NFR-10; kontrakt POC 1.
  - **DoD:** testy Vitest klienta (wstrzyknięty `fetch`): dodaje `Authorization` i `If-Match`; po `401` odświeża raz i ponawia raz (drugi `401` → błąd, bez pętli); ProblemDetails → `BladApi` z `kod` i `bledyPol`; odpowiedź HTML 502 → `BLAD_BRAMY`; błąd sieci → `BLAD_SIECI`; `multipart` bez ręcznego `Content-Type`; `412` niesie `szczegoly.aktualny`; typy kompilują się z przykładami JSON wklejonymi z kontraktu (test typów na stałych);
    reguła ESLint lub test blokuje `fetch('/api` poza `client.ts`; komunikaty dla każdego kodu z 1.2 istnieją w `pl-PL.json` (test kompletności); `typecheck`, `lint`, `test`, `build`, `check:headers` zielone.
  - **Pliki (TASK-131):** `frontend/src/lib/api/types.ts`, `frontend/src/lib/api/errors.ts`, `frontend/src/lib/api/client.ts`, `frontend/src/lib/api/client.test.ts`, `frontend/src/lib/api/errors.test.ts`, `frontend/src/lib/api/contract-examples.test.ts`, `frontend/src/lib/api/fetch-guard.test.ts`, `frontend/messages/pl-PL.json` (sekcja `bledy`: 23 kody serwera, 4 klienta, `NIEZNANY`), `frontend/eslint.config.mjs` (`no-restricted-syntax`: `fetch` tylko w `client.ts`, testach i `src/mocks/`), `frontend/package.json`, `frontend/package-lock.json` (zod ^4.6.7, react-hook-form ^7.91.0, @hookform/resolvers ^5.9.2);
  - **Weryfikacja (TASK-131):** `typecheck`, `lint`, `test` (8 plików, 60 testów), `build`, `check:headers` zielone. Brak mocków (MSW w TASK-132).

- [x] **TASK-132 [POC-B] Mocki MSW według kontraktu (stanowe, z przejściami czasowymi)**
  - **Opis:** Dodaj `msw` jako `devDependency` (OQ-12) i `src/mocks/`: `handlers.ts`, `db.ts` (stan w pamięci: konta, sesje, Rozwiązania), `fixtures/` (Działy 4.2, Zadania 6.3, Transkrypcje 7.2, Oceny 7.3 — dane **skopiowane z kontraktu**, nie wymyślone), `browser.ts`, `server.ts` (Vitest/Playwright), `public/mockServiceWorker.js` (`msw init`), `public/mock/kartka-przyklad.jpg` (mała kartka JPEG), komponent `MockProvider` uruchamiający worker tylko gdy `NEXT_PUBLIC_LEMAT_API=mock` (import dynamiczny — kod MSW nie trafia do buildu produkcyjnego `real`).
    Handlery: wszystkie endpointy 1–17 z katalogu kontraktu: konta demo 6.1 i rejestracja (`EMAIL_ZAJETY`, walidacja z `bledy_pol`), logowanie/odświeżenie/wylogowanie (stan odświeżania wolno trzymać w `localStorage` tylko w trybie MSW), `PATCH /uczniowie/ja` z `If-Match` (428/412), `GET /zadania` (filtr `dzial_id`, paginacja), `POST /rozwiazania` (multipart, walidacje 7.4, scenariusze z nagłówka), przejścia stanów na timerach 3000/2000 ms z `version` +1 i `nastepne_odpytanie_ms` wg 7.11, `409` dla drugiego nieukończonego Rozwiązania, `profil-uproszczony` liczony z bieżącego stanu.
  - **Zależności:** TASK-131.
  - **Realizuje:** kontrakt POC 2 … 8 (zachowanie mocka 7.1); NFR-10.
  - **DoD:** testy Vitest na `setupServer`: pełny przepływ (rejestracja → logowanie → lista Zadań → `POST` → status po 3 s `TRANSKRYPCJA_GOTOWA` → zatwierdzenie → po 2 s `OCENIONE`) z fałszywymi timerami; dla każdego z 5 Zadań Transkrypcja i Ocena równe tabelom 7.2/7.3; scenariusz `rozpoznanie-nieczytelne`; błędy w kształcie 1.1 (`kod`, `bledy_pol`, `szczegoly`); `profil-uproszczony` równy przykładowi 4.3 po odtworzeniu sekwencji; odpowiedź `GET /zadania` równa 6.3;
    `NEXT_PUBLIC_LEMAT_API=mock npm run dev` startuje z aktywnym Service Workerem (weryfikacja ręczna w raporcie); `typecheck`, `lint`, `test`, `build`, `check:headers` zielone.
  - **Pliki (TASK-132):** utworzone: `frontend/src/mocks/db.ts`, `frontend/src/mocks/http.ts`, `frontend/src/mocks/handlers.ts`, `frontend/src/mocks/browser.ts`, `frontend/src/mocks/server.ts`, `frontend/src/mocks/MockProvider.tsx`, `frontend/src/mocks/fixtures/{dzialy,zadania,transkrypcje,oceny,konta}.ts`, `frontend/src/mocks/__expected__/*.json` (5 wycinków JSON kontraktu jako wzorce testów), `frontend/src/mocks/handlers.test.ts`, `frontend/src/mocks/MockProvider.test.tsx`, `frontend/public/mockServiceWorker.js` (generowany `msw init`, msw 2.15.0), `frontend/public/mock/kartka-przyklad.jpg`, `frontend/.env.example`; zmodyfikowane: `frontend/package.json`, `frontend/package-lock.json` (msw ^2.15.0, devDependency), `frontend/src/app/layout.tsx` (MockProvider owija QueryProvider), `frontend/eslint.config.mjs` (ignoruje `public/mockServiceWorker.js`), `frontend/.prettierignore`, `frontend/vitest.setup.ts` (zapasowy localStorage/sessionStorage w jsdom).
  - **Weryfikacja (TASK-132):** `typecheck`, `lint`, `test` (10 plików, 91 testów), `build`, `check:headers`, `prettier --check` zielone; build `real` bez kodu MSW (grep pusty). Niezweryfikowane ręcznie: działanie Service Workera w przeglądarce (`NEXT_PUBLIC_LEMAT_API=mock next dev` startuje, `/` 200, serwowane `/mockServiceWorker.js` i `/mock/kartka-przyklad.jpg`).

- [x] **TASK-133 [POC-B] Sesja i uwierzytelnianie: `SessionProvider`, strony `/login` i `/register`, strażnik układu Ucznia**
  - **Opis:** `src/lib/auth/SessionProvider.tsx` (+ `useSession`): token dostępu tylko w pamięci, przy starcie jedno `POST /auth/odswiezenie` (single-flight w obrębie karty), proaktywne odświeżenie 60 s przed `wygasa_at`, wstrzyknięcie dostawcy tokenu i `odswiezSesje` do `client.ts`, `wyloguj`; `src/features/accounts` (formularze `react-hook-form` + `zod` odwzorowujące reguły serwera: e-mail, hasło 10–128, imię 1–60, nazwisko 1–80, dwa oświadczenia; mapowanie `bledy_pol` na pola; komunikaty `EMAIL_ZAJETY`, `NIEUWIERZYTELNIONY`),
    strony `(public)/login/page.tsx`, `(public)/register/page.tsx`, `(student)/layout.tsx` (strażnik: bez sesji → `/login`; nawigacja: Zadania, Rozwiązania, Profil, Ustawienia, „Wyloguj”), przekierowanie po logowaniu na `/exercises`, `proxy.ts`/middleware niewymagane.
  - **Zależności:** TASK-131, TASK-132.
  - **Realizuje:** 12.3 (`/auth/*`), 14 (UX), `api.md` 6.2 (uproszczone), ADR-054, ADR-057 (część), ADR-020; FR-01; NFR-10; kontrakt POC 3.
  - **DoD:** testy Vitest/Testing Library (MSW): walidacja pól po polsku, błędy serwera na polach (`bledy_pol`), `EMAIL_ZAJETY` i zły login jako czytelne komunikaty, udane logowanie → przekierowanie, rejestracja → sesja i przekierowanie, strażnik przekierowuje niezalogowanego, wylogowanie czyści stan i wywołuje endpoint, odświeżenie przy starcie przywraca sesję, nieudane odświeżenie (`SESJA_UNIEWAZNIONA`) → `/login`;
    test, że token nie trafia do `localStorage`/`sessionStorage`/`document.cookie`; podwójne kliknięcie nie wysyła dwóch żądań; nawigacja dostępna na szerokości 375 px (test układu lub zapis ręcznej weryfikacji); `typecheck`, `lint`, `test`, `build`, `check:headers` zielone.
  - **Pliki (TASK-133):** utworzone: `frontend/src/lib/auth/SessionProvider.tsx` (+ `useSession`), `frontend/src/lib/auth/test-support.tsx`, `frontend/src/lib/auth/SessionProvider.test.tsx`, `frontend/src/lib/api/use-error-message.ts` (`useKomunikatBledu`), `frontend/src/features/accounts/{schemas.ts,field-errors.ts,use-single-submit.ts,LoginForm.tsx,RegisterForm.tsx,StudentShell.tsx,index.ts,accounts.test.tsx}`, `frontend/src/app/(public)/login/page.tsx`, `frontend/src/app/(public)/register/page.tsx`; zmodyfikowane: `frontend/src/app/layout.tsx` (SessionProvider wewnątrz QueryProvider), `frontend/src/app/(student)/layout.tsx` (StudentShell), `frontend/messages/pl-PL.json` (sekcja `Konto`).
  - **Weryfikacja (TASK-133):** `typecheck`, `lint`, `test` (12 plików, 109 testów), `build`, `check:headers` zielone. Niezweryfikowane ręcznie: przeglądarka i układ 375 px (tylko test klas CSS).

- [x] **TASK-134 [POC-B] Dane Ucznia (`/settings`) i uproszczony Profil ucznia (`/profile`)**
  - **Opis:** `(student)/settings/page.tsx`: formularz poziomu matury, terminu egzaminu i wybranego Działu (lista z `GET /dzialy`, opcja „Wszystkie Działy” = `null`), `PATCH /uczniowie/ja` z `If-Match` z `version`, obsługa `412 KONFLIKT_WERSJI` (komunikat i przeładowanie danych), `422` na polach; `(student)/profile/page.tsx`: karty „Liczba rozwiązań”, „Średni wynik”, rozbicie na Działy, lista ostatnich Ocen (link do `/solutions/[solutionId]`), stan pusty dla nowego Ucznia; `features/accounts`, `features/profile` (publiczne API przez `index.ts`), TanStack Query z unieważnianiem po `PATCH`.
  - **Zależności:** TASK-133.
  - **Realizuje:** 12.3 (`/uczniowie/ja`, `/dzialy`), kontrakt POC 4.1 … 4.3; FR-02 (część), FR-04 (uproszczone), FR-09 (część); NFR-10.
  - **DoD:** testy (MSW): formularz wypełnia się danymi z `GET`, zapis wysyła merge-patch tylko ze zmienionymi polami i poprawnym `If-Match`, wyczyszczenie terminu wysyła `null`, `412` pokazuje komunikat i odświeża dane, `422` oznacza pola; strona Profilu dla Ucznia bez Rozwiązań pokazuje stan pusty, a po sekwencji z przykładu 4.3 — 50,0% i trzy Działy; wartości procentowe i liczby w formacie polskim (`Intl`, `pl-PL`);
    `typecheck`, `lint`, `test`, `build`, `check:headers` zielone.
  - **Pliki (TASK-134):** utworzone: `frontend/src/features/accounts/SettingsForm.tsx`, `frontend/src/features/accounts/settings-schema.ts`, `frontend/src/features/accounts/settings.test.tsx`, `frontend/src/features/profile/ProfileView.tsx`, `frontend/src/features/profile/index.ts`, `frontend/src/features/profile/profile.test.tsx`, `frontend/src/app/(student)/settings/page.tsx`, `frontend/src/app/(student)/profile/page.tsx`, `frontend/src/lib/api/queries.ts` (`useUczen`, `useDzialy`, `useProfilUproszczony`, `kluczeZapytan`), `frontend/src/lib/i18n/format.ts`, `frontend/src/lib/i18n/format.test.ts`, `frontend/src/ui/Select.tsx`; zmodyfikowane: `frontend/src/features/accounts/index.ts` (eksport `SettingsForm`), `frontend/src/features/accounts/use-single-submit.ts` (`useWysylkaPojedyncza` — blokada zwalniana także po sukcesie), `frontend/messages/pl-PL.json` (sekcje `Ustawienia`, `Profil`). Odstępstwo od opisu: zapytania we wspólnym `lib/api/queries.ts`, nie w `features/*` (TASK-135 użyje `useDzialy` bez importu między features).
  - **Weryfikacja (TASK-134):** `typecheck`, `lint`, `test` (15 plików, 123 testy, +14), `build`, `check:headers`, `prettier --check` zielone. Niezweryfikowane ręcznie: przeglądarka (`next dev` + Service Worker) i układ 375 px.

- [x] **TASK-135 [POC-B] Zadania: lista (`/exercises`) i szczegóły (`/exercises/[exerciseId]`) z KaTeX**
  - **Opis:** `features/content` (kafelki Zadań: Dział, poziom trudności, `punkty_max`, początek `tresc_md` renderowany przez `MarkdownContent`; filtr Działu z `GET /dzialy`; stany ładowania, pusty, błąd z możliwością ponowienia), `(student)/exercises/page.tsx`, `(student)/exercises/[exerciseId]/page.tsx` (pełna treść z KaTeX, Umiejętności, punkty, przycisk „Prześlij rozwiązanie” otwierający sekcję przesyłania z TASK-136 — w tym zadaniu pusty slot), mapowanie `const { exerciseId: zadanieId }`, `404 NIE_ZNALEZIONO` → strona „Nie znaleziono zadania”.
  - **Zależności:** TASK-133.
  - **Realizuje:** 12.3 (`/zadania`, `/zadania/{id}`), kontrakt POC 5; FR-12 (odczyt); NFR-08, NFR-10.
  - **DoD:** testy (MSW): lista pokazuje 5 Zadań w kolejności z kontraktu 6.3, filtr Działu zawęża listę, wzór `$20\%$`, `\frac`, `\operatorname{tg}` renderuje się (element `.katex`, brak surowego `$`), niepoprawny LaTeX nie wywala strony (`throwOnError: false`), szczegóły pokazują `punkty_max` i Umiejętność, nieistniejący `exerciseId` → komunikat 404; układ czytelny na 375 px (test lub ręczny zapis);
    `typecheck`, `lint`, `test`, `build`, `check:headers` zielone.
  - **Pliki (TASK-135):** utworzone: `frontend/src/features/content/ExerciseList.tsx`, `frontend/src/features/content/ExerciseDetail.tsx`, `frontend/src/features/content/index.ts`, `frontend/src/features/content/content.test.tsx`, `frontend/src/app/(student)/exercises/page.tsx`, `frontend/src/app/(student)/exercises/[exerciseId]/page.tsx`; zmodyfikowane: `frontend/src/lib/api/queries.ts` (`useZadania`, `useZadanie`, `kluczeZapytan.zadania/zadanie`), `frontend/messages/pl-PL.json` (sekcja `Zadania`). Odstępstwa od opisu: zapytania w `lib/api/queries.ts` (jak TASK-134); komponent `MarkdownContent` w kodzie to `TrescMarkdown` (`lib/katex/MarkdownContent.tsx`); początek treści w kafelku = pełny `tresc_md` ucięty CSS `line-clamp-3` (cięcie tekstu przecinałoby wzory); „Prześlij rozwiązanie” otwiera pusty slot (`ExerciseDetail` prop `przeslanie?: ReactNode`).
  - **Weryfikacja (TASK-135):** `typecheck`, `lint`, `test` (16 plików, 134 testy, +11), `build` (trasy `/exercises`, `/exercises/[exerciseId]`), `check:headers` zielone. Niezweryfikowane ręcznie: układ 375 px (klasy Tailwind `sm:grid-cols-2`, `break-words`, min. 44 px) i przeglądarka.

- [x] **TASK-136 [POC-B] Przesłanie zdjęcia kartki (aparat/plik, konwersja do JPEG, `POST /rozwiazania`)**
  - **Opis:** `features/solutions/PhotoUpload` (`input type=file accept="image/*" capture="environment" multiple`, 1 … 5 plików, miniatury z możliwością usunięcia, licznik), `src/lib/photos/to-jpeg.ts` (przeskalowanie do ≤ 2000 px dłuższego boku i kodowanie JPEG jakość 0.85 przez `canvas`, EXIF z orientacją respektowany przez przeglądarkę; HEIC/nieczytelny plik → komunikat; funkcja wstrzykiwalna dla testów), walidacje klienta (≤ 10 MB po konwersji, ≤ 5 plików), wysyłka `multipart` przez `client.ts` (pola `zadanie_wersja_id`, `zdjecia`), blokada przycisku w trakcie (U-11), po `202` przejście do `/solutions/[solutionId]`;
    obsługa błędów: `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN` (link do istniejącego Rozwiązania z `szczegoly.istniejace_rozwiazanie_id`), `409 WERSJA_ZADANIA_NIEAKTUALNA` (odświeżenie Zadania), `413/415/422` jako komunikaty; podpięcie w `exercises/[exerciseId]`.
  - **Zależności:** TASK-135.
  - **Realizuje:** 12.3 (`POST /rozwiazania`), `api.md` ADR-067 (multipart), 9.4; FR-16; NFR-08; kontrakt POC 7.4.
  - **DoD:** testy (MSW + wstrzyknięty konwerter): wybór 1 … 5 plików, 6. plik odrzucony komunikatem, usunięcie miniatury, wysyłka tworzy żądanie `multipart` z polami `zadanie_wersja_id` i `zdjecia`, po `202` nawigacja na `/solutions/<id>` z odpowiedzi, `409` pokazuje link do istniejącego Rozwiązania, `415`/`422` mają polskie komunikaty, przycisk zablokowany w trakcie wysyłki (jedno żądanie przy podwójnym kliknięciu);
    nagłówek `X-Lemat-Mock-Scenariusz` wysyłany tylko gdy ustawiono `NEXT_PUBLIC_LEMAT_MOCK_SCENARIUSZ` (tryb dev/E2E); `typecheck`, `lint`, `test`, `build`, `check:headers` zielone.
  - **Pliki (TASK-136):** utworzone: `frontend/src/lib/photos/to-jpeg.ts`, `frontend/src/lib/photos/to-jpeg.test.ts`, `frontend/src/features/solutions/PhotoUpload.tsx`, `frontend/src/features/solutions/index.ts`, `frontend/src/features/solutions/solutions.test.tsx`; zmodyfikowane: `frontend/src/app/(student)/exercises/[exerciseId]/page.tsx` (`<PhotoUpload zadanieId={zadanieId} />` w slocie `przeslanie`), `frontend/messages/pl-PL.json` (sekcja `Przeslanie`). Odstępstwa od opisu: brak nowych zależności npm (`createImageBitmap` z `imageOrientation: "from-image"` + `canvas`); konwersja przez canvas zawsze, także dla JPEG; blokada podwójnego kliku lokalnym `ref` (bez importu `useWysylkaPojedyncza` z `features/accounts`); po `202` brak invalidacji (lista Rozwiązań — TASK-137); `X-Lemat-Mock-Scenariusz` bez filtrowania wartości (literówka → `422 MOCK_SCENARIUSZ_NIEZNANY`); `415 NIEOBSLUGIWANY_FORMAT` i `422 ZDJECIE_NIEPRAWIDLOWE` → jeden komunikat (K-B-06).
  - **Weryfikacja (TASK-136):** `typecheck`, `lint`, `check:headers`, `prettier`, `test` (18 plików, 153 testy, +19), `build` zielone. Niezweryfikowane ręcznie: prawdziwy canvas (`naJpeg`) w przeglądarce i aparat na telefonie; brak fallbacku `HTMLImageElement` dla przeglądarek bez `imageOrientation` w `createImageBitmap` (sprawdzić na iOS Safari).

- [ ] **TASK-137 [POC-B] Ekran Rozwiązania: odpytywanie statusu, Transkrypcja, zatwierdzenie, Ocena i Wskazówka; lista Rozwiązań**
  - **Opis:** `(student)/solutions/[solutionId]/page.tsx` (`const { solutionId: rozwiazanieId }`) i `(student)/solutions/page.tsx`: hook `useRozwiazanie` na TanStack Query z `refetchInterval` = `nastepne_odpytanie_ms` (zatrzymanie przy `null`, limit 60 s z komunikatem i przyciskiem „Odśwież”), widoki według tabeli 7.11 kontraktu: postęp („Odczytujemy…”, „Oceniamy…” z paskiem/animacją respektującą `prefers-reduced-motion`), Transkrypcja (kroki: `tekst`, `latex` przez KaTeX, wskazanie niskiej pewności) z przyciskiem „Zatwierdź” (`POST …/zatwierdzenie`, `If-Match` z `version`, obsługa `409`/`412`),
    Ocena (punkty `x/y`, tabela Kryteriów z punktami i opisem w KaTeX, `uzasadnienie` jako Markdown, wyróżniony pierwszy błędny krok, Wskazówka w ramce, podgląd przesłanego zdjęcia przez `GET …/adres`), `BLAD_ROZPOZNAWANIA` (komunikat i „Prześlij ponownie” → Zadanie), lista Rozwiązań (`GET /rozwiazania`, status, punkty, link), `features/solutions` z `index.ts`.
  - **Zależności:** TASK-136.
  - **Realizuje:** 12.3 (`/rozwiazania*`), 12.1 (Postęp operacji — odpytywanie), 9.4, 9.5, kontrakt POC 7.5 … 7.11; FR-17, FR-18 (podgląd i zatwierdzenie), FR-19, FR-27; NFR-01 (widoczny postęp), NFR-08.
  - **DoD:** testy (MSW, fałszywe timery): sekwencja `PRZESLANE` → `ROZPOZNAWANIE` → `TRANSKRYPCJA_GOTOWA` (odpytywanie zatrzymuje się, pokazana Transkrypcja Zadania 2 z KaTeX) → klik „Zatwierdź” → `OCENIANIE` → `OCENIONE` z Oceną 1/2, Kryteriami, błędnym krokiem 2 i Wskazówką; dla Zadania 1 (2/2) brak bloku Wskazówki; `BLAD_ROZPOZNAWANIA` pokazuje komunikat i akcję; ponowne zatwierdzenie (`409`) pokazuje komunikat i odświeża; limit 60 s zatrzymuje odpytywanie;
    zdjęcie ładowane z adresu z `GET …/adres` (nie z `Authorization`); lista pokazuje statusy po polsku; `typecheck`, `lint`, `test`, `build`, `check:headers` zielone.

- [ ] **TASK-138 [POC-B] Przełączanie mock/real, stany ogólne i dopracowanie nawigacji**
  - **Opis:** Zmienna `NEXT_PUBLIC_LEMAT_API=mock|real` (domyślnie `real`; `.env.example` frontendu), skrypty `dev:mock` i `dev:real` w `package.json`, `next.config.ts`: `rewrites` `/api/:path*` → `LEMAT_API_URL` tylko w trybie `real`, brak kodu MSW w buildzie `real` (weryfikacja przez wyszukanie w `.next`), `app/error.tsx`, `(student)/error.tsx`, `loading.tsx`, `not-found.tsx`, strona główna `(public)/page.tsx` z linkami „Zaloguj się”/„Załóż konto” (przekierowanie zalogowanego na `/exercises`), baner w trybie mock („Dane przykładowe”), dostępność (fokus, etykiety, kontrast, `aria-live` dla postępu), przegląd widoków na 375 px i 1280 px.
  - **Zależności:** TASK-137.
  - **Realizuje:** 5.2 (konfiguracja frontendu), 14 (UX), `api.md` 6.1; NFR-08, NFR-10; kontrakt POC 8.
  - **DoD:** `npm run dev:mock` — cały scenariusz demo (rejestracja → Zadanie → zdjęcie → Transkrypcja → Ocena → Profil) przechodzi ręcznie bez backendu (zapis kroków w raporcie); `npm run dev:real` wysyła żądania na `/api/v1/...` (potwierdzone w narzędziach sieci lub teście `rewrites`); build `real` nie zawiera `msw` ani `mockServiceWorker`;
    błąd sieci i `BLAD_BRAMY` pokazują czytelny komunikat z ponowieniem; brak surowych kluczy tłumaczeń ani tekstów w JSX (lint); `scripts/verify-frontend.sh` zielone.

- [ ] **TASK-139 [POC-B] Test Playwright smoke (tryb MSW; opcjonalnie przeciw prawdziwemu API)**
  - **Opis:** `@playwright/test`, `frontend/playwright.config.ts` (projekty Desktop Chrome i Pixel 7; `webServer`: `npm run dev:mock` na wolnym porcie albo `dev:real` gdy `LEMAT_E2E_REAL=1`), skrypt `e2e` (zastępuje placeholder), `e2e/poc-smoke.spec.ts`: rejestracja nowego Ucznia → ustawienia (poziom matury) → lista 5 Zadań → szczegóły Zadania 2 (KaTeX widoczny) → przesłanie zdjęcia (`e2e/fixtures/kartka.jpg`) → ekran oczekiwania → Transkrypcja → „Zatwierdź” → Ocena 1/2 z Wskazówką → Profil (1 rozwiązanie, 50,0%) → wylogowanie i ponowne logowanie kontem demo; w trybie `real` czas oczekiwania liczony z 3 s + 2 s mocka serwera.
  - **Zależności:** TASK-138.
  - **Realizuje:** 17.x (E2E), ADR-065 p. 7 (część); TASK-110 (część); FR-01, FR-16, FR-17, FR-19, FR-27; NFR-08.
  - **DoD:** `npm --prefix frontend run e2e` zielone lokalnie na obu projektach w trybie MSW (< 90 s); test `rozpoznanie-nieczytelne` (nagłówek scenariusza przez `NEXT_PUBLIC_LEMAT_MOCK_SCENARIUSZ` lub `page.route`) pokazuje błąd rozpoznania; w trybie `real` ten sam smoke zielony po `scripts/dev-up.sh` (zapis wyniku w raporcie; jeśli backend niegotowy — oznaczone jako oczekujące na TASK-140); katalog `e2e/` i wyniki Playwright w `.gitignore` (raporty), nagłówki w `.ts`; `scripts/verify-frontend.sh` zielone.

## Integracja `[POC-INT]` (po ukończeniu obu ścieżek)

- [ ] **TASK-140 [POC-INT] Integracja frontendu z prawdziwym backendem i scenariusz demo**
  - **Opis:** Uruchom `scripts/dev-up.sh`, a frontend w trybie `real`; wykonaj pełny scenariusz demo z `HANDOFF.md` 12.5 ręcznie i testem `LEMAT_E2E_REAL=1 npm --prefix frontend run e2e`; porównaj odpowiedzi API z kontraktem (`scripts/poc-smoke.sh`); rozbieżności rozstrzygaj zmianą **kontraktu** (procedura `docs/POC_CONTRACT.md` 10) i poprawką właściwej ścieżki, nie obejściem po stronie klienta;
    uzgodnij `HANDOFF.md` (stan prac, jak uruchomić, U-21 … U-31, otwarte pytania z obu ścieżek) i `docs/CODEBASE_MAP.md` (scalenie wierszy), oznacz w `TASKS.md` zadania MVP częściowo zrealizowane przez POC (notatka „częściowo: POC”).
  - **Zależności:** TASK-130 oraz TASK-139 (obie ścieżki).
  - **Realizuje:** kontrakt POC (całość); TASK-109/110/112 (część).
  - **DoD:** scenariusz demo 12.5 przechodzi bez ręcznych obejść na czystym klonie (`scripts/dev-up.sh` + `npm --prefix frontend run dev:real`); `LEMAT_E2E_REAL=1 npm --prefix frontend run e2e` zielone; `scripts/verify-all.sh` zielone; lista rozbieżności kontraktu i ich rozwiązań w raporcie; **POC gotowe** wg definicji z `HANDOFF.md` 12.5; wybór dalszej kolejności MVP (zadania nadal `[ ]`) jest decyzją właściciela.

---

# Faza 1: Szkielet solucji .NET i aplikacji Next.js + konfiguracja środowiska

- [x] **TASK-001 [MVP] Solucja .NET: pliki globalne i projekty `src`**
  - **Opis:** Utwórz `backend/Lemat.slnx`, `global.json` (SDK `10.0.100`, `rollForward: latestPatch`), `Directory.Build.props` (`Nullable=enable`,
    `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `ImplicitUsings=enable`, `EnforceCodeStyleInBuild=true`), `Directory.Packages.props`
    (Central Package Management; pakiety z 5.2/6.4; **zakaz** metapakietu `Microsoft.SemanticKernel`), `BannedSymbols.txt` (`DateTime.Now`,
    `DateTimeOffset.Now/UtcNow` poza `TimeProvider`; typy `OpenAI.*`/`Azure.AI.*`/`Anthropic.*` poza `Lemat.Models`), `.editorconfig` (6.11.3: IDE0130 błąd,
    nazwy IDE1006, wyłączone reguły „nazwa pliku = nazwa typu”), `.gitignore` (bin, obj, .next, node_modules, `.env`, `storage/`). Projekty `backend/src`:
    `Lemat.Domain`, `Lemat.Application`, `Lemat.Agents`, `Lemat.Models`, `Lemat.Infrastructure`, `Lemat.Api`, `Lemat.Worker`, `Lemat.Scheduler`, `Lemat.Tools` z
    referencjami z diagramu 6.4 (Domain bez żadnych pakietów; Application bez referencji do Infrastructure/Agents/Models).
  - **Zależności:** brak.
  - **Realizuje:** 5.2 (Jakość kodu), 6.4, 6.5, 6.11.3; ADR-048, ADR-049 p. 11, ADR-075.
  - **DoD:** `dotnet build backend/Lemat.slnx -warnaserror` przechodzi na czystym klonie; graf referencji projektów odpowiada diagramowi 6.4 (sprawdź `dotnet list reference`);
    `Lemat.Domain.csproj` nie ma `PackageReference`; `dotnet format backend/Lemat.slnx --verify-no-changes` przechodzi; `.gitignore` obejmuje wymienione katalogi;
    brak kodu domenowego (tylko szkielety projektów).
  - **Pliki (TASK-001):** `.gitignore`; `backend/Lemat.slnx`, `backend/global.json`, `backend/Directory.Build.props`, `backend/Directory.Packages.props`,
    `backend/BannedSymbols.txt`, `backend/BannedSymbols.Sdk.txt`, `backend/.editorconfig`, `backend/src/Directory.Build.props`;
    `backend/src/Lemat.{Domain,Application,Agents,Models,Infrastructure,Api,Worker,Scheduler,Tools}/Lemat.<Nazwa>.csproj`;
    `backend/src/Lemat.Api/Program.cs`, `backend/src/Lemat.Worker/Program.cs`, `backend/src/Lemat.Scheduler/Program.cs`, `backend/src/Lemat.Tools/Program.cs`.
  - **Weryfikacja:** `dotnet build backend/Lemat.slnx -warnaserror` OK; `dotnet format backend/Lemat.slnx --verify-no-changes` OK; brak projektów testowych (TASK-002).

- [x] **TASK-002 [MVP] Projekty testowe, testy architektury i lekki test słownika**
  - **Opis:** Dodaj `backend/tests`: `Lemat.Domain.Tests`, `Lemat.Application.Tests`, `Lemat.Agents.Tests`, `Lemat.Models.Tests`, `Lemat.Infrastructure.Tests`,
    `Lemat.Api.Tests`, `Lemat.Architecture.Tests` (xUnit v3, Shouldly, NSubstitute, FsCheck.Xunit.v3, Testcontainers.PostgreSql, WireMock.Net,
    Mvc.Testing, NetArchTest.Rules, Microsoft.CodeAnalysis.CSharp) z referencjami z 6.4. W `Lemat.Architecture.Tests`: reguły warstw 6.5 (`ProjektDomenyBezPakietow`,
    `DomenaNiezaleznaOdInfrastruktury`, `AplikacjaBezInfrastruktury`, `AgenciTylkoPrzezPorty`, `SdkDostawcowTylkoWModelach`, `EndpointyTylkoPrzezAplikacje` — oparte na referencjach
    assembly i przestrzeniach nazw, tak by działały także na pustych projektach i sprawdzały realne typy, gdy się pojawią); katalog `Glossary/` z testem
    `ZakazaneSynonimyWIdentyfikatorach` (Roslyn: identyfikatory w `backend/src` i `backend/tests` nie zawierają tokenów `student`, `task`, `submission`, `grade`, `score`
    (wyjątek: pytanie `SCORE`), `difficulty`, `topic`, `skill`, `lesson`, `review`; tokeny dzielone na granicach wielkości liter) oraz testami `NaglowekLivingDocumentation`
    (każdy `.cs` poza `obj/bin`, `*.g.cs`, `*.Designer.cs` zaczyna się od wiersza zgodnego z wzorcem `// @Role: … | @DependsOn: … | @UsedBy: … | @Task: TASK-\d{3}(, TASK-\d{3})*`)
    i `BrakNiestandardowychZnakowWNazwachPlikow` (ścieżki z `git ls-files` ASCII, ADR-075).
  - **Zależności:** TASK-001.
  - **Realizuje:** 5.2 (Testy, Kontrola architektury), 6.4, 6.5, 3.4 (część uproszczona); ADR-049, ADR-075; 18.5 (c).
  - **DoD:** `dotnet test backend/Lemat.slnx` zielone; test `ZakazaneSynonimyWIdentyfikatorach` ma testy własne (próbka kodu z `grade`/`Student` → wykryta; `Ocena`/`SCORE` → ok);
    test nagłówka wykrywa plik bez nagłówka (test negatywny na próbce w pamięci); każda reguła warstw ma test negatywny na syntetycznym przykładzie; CI-skrypt (TASK-007) uruchamia te testy.
  - **Pliki (TASK-002):** `backend/Lemat.slnx` (folder `/tests/`), `backend/Directory.Packages.props` (pakiety testowe), `backend/tests/Directory.Build.props`;
    `backend/tests/Lemat.{Domain,Application,Agents,Models,Infrastructure,Api,Architecture}.Tests/Lemat.<Nazwa>.Tests.csproj`;
    `backend/tests/Lemat.{Domain,Application,Agents,Models,Infrastructure,Api}.Tests/AssemblySmokeTests.cs`;
    `backend/tests/Lemat.Architecture.Tests/Layers/{LayerRule,LayerRules,LayerRulesTests}.cs`, `backend/tests/Lemat.Architecture.Tests/Layers/Synthetic/SyntheticDomainType.cs`;
    `backend/tests/Lemat.Architecture.Tests/Glossary/{ForbiddenSynonymAnalyzer,ZakazaneSynonimyWIdentyfikatorachTests}.cs`;
    `backend/tests/Lemat.Architecture.Tests/Conventions/{FileNamePolicy,LivingDocumentationHeader,NaglowekLivingDocumentationTests,BrakNiestandardowychZnakowWNazwachPlikowTests}.cs`;
    `backend/tests/Lemat.Architecture.Tests/Support/RepositoryLocator.cs`.
  - **Weryfikacja:** `dotnet build backend/Lemat.slnx -warnaserror` OK; `dotnet test backend/Lemat.slnx` zielone (6 testów dymnych + 90 w Architecture.Tests); `dotnet format --verify-no-changes` OK.
    Pakiety: xunit.v3 3.2.2, Shouldly 4.3.0, NSubstitute 5.3.0, FsCheck.Xunit.v3 3.3.4, Testcontainers.PostgreSql 4.16.0, WireMock.Net 2.18.0 (`NoWarn NU1608` w Models.Tests), NetArchTest.Rules 1.3.2, Roslyn 5.0.0.
    Reguły dodatkowe poza opisem: `ModeleBezInfrastruktury`, `InfrastrukturaBezWarstwWyzszych`, `HostyNieZalezaOdSiebie`, test referencji projektów = tabela 6.4. CI-skrypt (TASK-007) jeszcze nie istnieje.

- [x] **TASK-003 [MVP] Hosty Api / Worker / Scheduler / Tools (szkielet), zdrowie, OpenAPI**
  - **Opis:** `Lemat.Api/Program.cs`: Minimal API, `AddProblemDetails`, `GET /api/v1/zdrowie`, OpenAPI przez `Microsoft.AspNetCore.OpenApi` generowane przy budowie do
    `backend/openapi/lemat-v1.json`, `UseUrls` dev `http://localhost:5080`. `Lemat.Worker` i `Lemat.Scheduler`: hosty ogólne z `/zdrowie` na porcie 8081/8082 (bez logiki).
    `Lemat.Tools/Program.cs`: dispatcher poleceń (`--help`, jeszcze bez poleceń). Logowanie JSON do konsoli (`AddJsonConsole`). Test `KontenerDiTests` (`ValidateOnBuild`,
    `ValidateScopes`) dla kontenerów `api`, `worker`, `scheduler`.
  - **Zależności:** TASK-001, TASK-002.
  - **Realizuje:** 5.2 (HTTP API, Obserwowalność — logi), 5.3, 12.1; ADR-048, ADR-061 (źródło OpenAPI).
  - **DoD:** `dotnet run --project backend/src/Lemat.Api` odpowiada `200` na `GET /api/v1/zdrowie`; po `dotnet build` istnieje `backend/openapi/lemat-v1.json` (OpenAPI 3.x, deterministyczny);
    test `Lemat.Api.Tests` (`WebApplicationFactory`) sprawdza zdrowie i Content-Type `application/json`; `KontenerDiTests` zielone dla 3 procesów.
  - **Pliki (TASK-003):** `backend/Directory.Packages.props`, `backend/src/Lemat.Api/Lemat.Api.csproj`, `backend/src/Lemat.Api/Program.cs`,
    `backend/src/Lemat.Api/Configuration/ApiHost.cs`, `backend/src/Lemat.Api/Endpoints/HealthEndpoints.cs`, `backend/src/Lemat.Api/Contracts/Common/ZdrowieWyjscie.cs`,
    `backend/src/Lemat.Worker/Lemat.Worker.csproj`, `backend/src/Lemat.Worker/Program.cs`, `backend/src/Lemat.Worker/Configuration/WorkerHost.cs`,
    `backend/src/Lemat.Scheduler/Lemat.Scheduler.csproj`, `backend/src/Lemat.Scheduler/Program.cs`, `backend/src/Lemat.Scheduler/Configuration/SchedulerHost.cs`,
    `backend/src/Lemat.Tools/Program.cs`, `backend/src/Lemat.Tools/Commands/CommandDispatcher.cs`, `backend/openapi/lemat-v1.json`,
    `backend/tests/Lemat.Api.Tests/HealthEndpointTests.cs`, `backend/tests/Lemat.Architecture.Tests/Hosts/KontenerDiTests.cs`.
  - **Weryfikacja:** `dotnet build -warnaserror` OK; testy zielone (Architecture.Tests 96, Api.Tests 3); `dotnet format --verify-no-changes` OK; `dotnet run` Api -> `GET /api/v1/zdrowie` 200 `{"status":"OK"}`,
    logi JSON, `http://localhost:5080` w Development; OpenAPI 3.1.1 generowany przy buildzie (deterministyczny) i w runtime pod `/api/v1/openapi.json`. Worker/Scheduler mają `FrameworkReference Microsoft.AspNetCore.App`.

- [x] **TASK-004 [MVP] `LematOpcje` i konfiguracja (wybór implementacji Mock/lokalnych)**
  - **Opis:** `Lemat.Application.Common.LematOpcje` (wzorzec Options, sekcja `Lemat`) z parametrami MVP z sekcji 22: limity zdjęć (5 / 10 MB; JPEG, PNG, WEBP), maks. przesłań 3 i ponowień 5, maks. kroków 40,
    progi odczytu 0.80, τ_odczyt 0.20, τ_FR25 0.20, maks. pary Q-OC-05 = 12, terminy oceny (miękki `max(10 s, 28 s − t_R)`, twardy 120 s), heartbeat SSE 15 s, timeout Agenta wyjaśniającego 8 s, progi początkowe
    0.95 (Jev) / 0.98 (zapasowy), odsetek Kontroli losowej (MVP 0.0), flaga `Poprawki:ZawszeDoWeryfikacji` (MVP true), `Konta:AutozatwierdzanieNauczycieli` (Development true), sekcje wyboru
    implementacji: `Modele:Dostawca` (`mock`|`openai-zgodny`|`azure-openai`|`anthropic`), `Magazyn:Typ` (`lokalny`), `Magazyn:Sciezka`, `Obrazy:Typ`, `Symbolika:Typ`, `Email:Typ`, `Jwt:*`.
    `appsettings.json` + `appsettings.Development.json` (domyślnie wszystko Mock/lokalne) i nadpisania zmiennymi `LEMAT_*` (`LEMAT_BAZA`, `LEMAT_MODELE_DOSTAWCA`, `LEMAT_MODELE_KLUCZ_API`,
    `LEMAT_MODELE_ENDPOINT`, `LEMAT_POTWIERDZONA_RETENCJA_DOSTAWCY`, `LEMAT_MAGAZYN_SCIEZKA`, `LEMAT_KURSOR_KLUCZ`, `LEMAT_SRODOWISKO`, `LEMAT_CZAS_TESTOWY` — nazwy robocze do potwierdzenia w HANDOFF P-01).
    `IValidateOptions` + `ValidateOnStart`: w `Production` dostawca `mock` → wyjątek startu; dostawca ≠ `mock` bez `PotwierdzonaRetencjaDostawcy=true` → wyjątek startu (A-07).
  - **Zależności:** TASK-001.
  - **Realizuje:** 22, 5.5 (konfiguracja), 6.4; ADR-049 p. 8, ADR-055 p. 6, ADR-065 p. 5; A-07.
  - **DoD:** testy jednostkowe `LematOpcje`: wartości domyślne = tabela 22, nadpisanie zmienną `LEMAT_*`, walidacja (mock w Production, brak potwierdzenia retencji, brak ścieżki magazynu);
    aplikacje `api`/`worker` startują w Development bez żadnej zmiennej środowiskowej; `.env.example` (TASK-005) zawiera wszystkie nazwy.
  - **Pliki (TASK-004):** `backend/Directory.Packages.props`, `backend/src/Lemat.Application/Lemat.Application.csproj`, `backend/src/Lemat.Application/Common/LematOpcje.cs`,
    `backend/src/Lemat.Application/Common/LematOpcjeWalidator.cs`, `backend/src/Lemat.Application/Common/LematKonfiguracja.cs`,
    `backend/src/Lemat.Api/Configuration/ApiHost.cs`, `backend/src/Lemat.Api/appsettings.json`, `backend/src/Lemat.Api/appsettings.Development.json`,
    `backend/src/Lemat.Worker/Configuration/WorkerHost.cs`, `backend/src/Lemat.Worker/appsettings.json`, `backend/src/Lemat.Worker/appsettings.Development.json`,
    `backend/src/Lemat.Scheduler/Configuration/SchedulerHost.cs`, `backend/src/Lemat.Scheduler/appsettings.json`, `backend/src/Lemat.Scheduler/appsettings.Development.json`,
    `backend/tests/Lemat.Application.Tests/Lemat.Application.Tests.csproj`, `backend/tests/Lemat.Application.Tests/Common/LematOpcjeTests.cs`,
    `backend/tests/Lemat.Architecture.Tests/Hosts/LematOpcjeHostyTests.cs`.
  - **Weryfikacja:** build `-warnaserror` OK; testy zielone (Architecture 104, Application 14, Api 3); `dotnet format --verify-no-changes` OK; `backend/openapi/lemat-v1.json` bez zmian.
    Odstępstwa: pakiety Microsoft.Extensions.Options/DI.Abstractions/Logging.Abstractions podbite do 10.0.14 + Options.ConfigurationExtensions 10.0.14 (NU1605); `ApiHost` pomija `ValidateOnStart`
    dla generatora OpenAPI (entry assembly `GetDocument.Insider`); `Obrazy/Symbolika/Email/Magazyn:Typ` akceptują na razie tylko `prosta/prosta/log/lokalny`.

- [x] **TASK-005 [MVP] docker-compose z PostgreSQL 16, `.env.example`, skrypty bazy**
  - **Opis:** `docker-compose.yml` (usługa `postgres:16`, wolumen, healthcheck, port hosta `${LEMAT_PG_PORT:-5433}` — lokalny PostgreSQL zajmuje 5432), `.env.example` (wszystkie zmienne z TASK-004,
    bez sekretów), `scripts/db-up.sh`, `scripts/db-reset.sh`, `scripts/db-local-setup.sh` (alternatywa: tworzy rolę `lemat` i bazy `lemat_dev`, `lemat_test` w lokalnym PostgreSQL 16 na 5432), connection string dla developmentu w `appsettings.Development.json`.
  - **Zależności:** brak (connection string wpisywany do opcji po TASK-004).
  - **Realizuje:** 5.4, 17.5 (środowisko lokalne), ADR-059; CLAUDE.md pkt 2.
  - **DoD:** `docker compose up -d postgres` → kontener `healthy`; `psql` łączy się z `lemat_dev` na porcie z `.env`; `CREATE EXTENSION citext; CREATE EXTENSION pg_trgm;` działa dla roli `lemat` (rola ma prawa wymagane przez migrację);
    `scripts/db-reset.sh` odtwarza pustą bazę; wariant lokalny (`db-local-setup.sh`) opisany w komentarzu skryptu; brak sekretów w repo.
  - **Pliki (TASK-005):** `docker-compose.yml`, `.env.example`, `docker/postgres/init-db.sh`, `scripts/db-up.sh`, `scripts/db-reset.sh`, `scripts/db-local-setup.sh`,
    `backend/src/Lemat.Api/appsettings.Development.json`, `backend/src/Lemat.Worker/appsettings.Development.json`, `backend/src/Lemat.Scheduler/appsettings.Development.json`.
  - **Weryfikacja:** `docker compose up -d postgres` -> `healthy`; `psql` na 5433 `lemat_dev`; `citext`/`pg_trgm` obecne; `db-reset.sh` odtwarza pustą bazę (0 tabel); `db-local-setup.sh` (5432) idempotentny;
    build `-warnaserror` 0 błędów; testy zielone (Architecture 104, Application 14, Api 3). Decyzje: `init-db.sh` tworzy też `lemat_test` i role `lemat_api/worker/scheduler` z LOGIN (ddl.md 0005); w compose rola `lemat` = superuser
    (`POSTGRES_USER`), lokalnie `CREATEDB`+`CREATEROLE` bez superusera; hasło tylko deweloperskie `lemat_dev`; `LEMAT_BAZA_MIGRACJE` puste (rola `lemat_migracje` dopiero przy migracjach, TASK-013). Nie zmieniono kodu `.cs`.

- [x] **TASK-006 [MVP] Szkielet Next.js: App Router, TypeScript strict, i18n pl-PL, KaTeX, UI**
  - **Opis:** `frontend/` (npm): Next.js (App Router), React, TypeScript `strict`, Tailwind CSS 4, Radix UI, ESLint 9 (flat config, `eslint-config-next`), Prettier, Vitest + Testing Library,
    next-intl (jedna lokalizacja `pl-PL`, bez prefiksu w URL, `messages/pl-PL.json`), TanStack Query `QueryClientProvider`, KaTeX (`src/lib/katex/Formula.tsx` — komponent `Wzor`, `MarkdownContent.tsx` —
    `TrescMarkdown` z `react-markdown` + `remark-math` + `rehype-katex`, `throwOnError: false`, `trust: false`), podstawowe komponenty `src/ui` (`Button.tsx`, `Dialog.tsx`, `List.tsx`, `Form.tsx`),
    `next.config.ts` z `rewrites` `/api/:path*` → `LEMAT_API_URL` (dev), skrypty `dev`, `build`, `typecheck`, `lint`, `test`, `gen:api` (placeholder do TASK-081), `e2e` (placeholder), `scripts/check-headers.mjs`
    (nagłówki Living Documentation w `.ts/.tsx`). Drzewo grup tras z 6.8: `(public)`, `(student)`, `(teacher)/teacher`, `(administrator)/admin` z pustymi `layout.tsx`, strona `/` z tekstem z komunikatów;
    `<html lang="pl">`; fonty KaTeX.
  - **Zależności:** brak.
  - **Realizuje:** 5.2 (Frontend, i18n, LaTeX, Dane w kliencie, UI, Testy/Lint frontendu), 6.8, 6.9 (struktura); ADR-054; NFR-08, NFR-10.
  - **DoD:** `npm --prefix frontend run typecheck`, `lint`, `test`, `build` zielone; `npm run dev` pokazuje polską stronę główną; test Vitest renderuje `$x^2+1$` przez `Wzor` bez błędu i z klasą `katex`;
    test, że `TrescMarkdown` nie wykonuje HTML z wejścia; `check-headers.mjs` przechodzi; brak tekstów na sztywno w JSX (reguła ESLint lub test na `messages`).
  - **Pliki (utworzone, wszystkie nowe, w `frontend/`):** `frontend/package.json`, `frontend/package-lock.json`, `frontend/tsconfig.json`, `frontend/next.config.ts`, `frontend/postcss.config.mjs`, `frontend/eslint.config.mjs`,
    `frontend/vitest.config.ts`, `frontend/vitest.setup.ts`, `frontend/.prettierrc.json`, `frontend/.prettierignore`, `frontend/.gitignore`, `frontend/messages/pl-PL.json`, `frontend/scripts/check-headers.mjs`,
    `frontend/src/app/layout.tsx`, `frontend/src/app/globals.css`, `frontend/src/app/(public)/layout.tsx`, `frontend/src/app/(public)/page.tsx`, `frontend/src/app/(student)/layout.tsx`,
    `frontend/src/app/(teacher)/teacher/layout.tsx`, `frontend/src/app/(administrator)/admin/layout.tsx`, `frontend/src/lib/i18n/request.ts`, `frontend/src/lib/query/QueryProvider.tsx`,
    `frontend/src/lib/katex/Formula.tsx`, `frontend/src/lib/katex/MarkdownContent.tsx`, `frontend/src/lib/katex/Formula.test.tsx`, `frontend/src/lib/katex/MarkdownContent.test.tsx`,
    `frontend/src/ui/Button.tsx`, `frontend/src/ui/Dialog.tsx`, `frontend/src/ui/List.tsx`, `frontend/src/ui/Form.tsx`, `frontend/src/ui/ui.test.tsx`, `frontend/src/messages.test.ts`.
    Dokumenty (handoff-guardian): `TASKS.md`, `HANDOFF.md`, `docs/CODEBASE_MAP.md`. Backend nietknięty.
  - **Weryfikacja:** `npm --prefix frontend run typecheck`, `lint` (0 błędów/ostrzeżeń), `test` (9 testów Vitest), `build` (`next build --webpack`) zielone; `prettier --check` OK; `check:headers` OK; `npm run dev`: `/` zwraca `<html lang="pl">`
    i polski nagłówek. Decyzje: Next 16.4, React 19, TS 5.9 (przypięte; npm latest = TS 7), ESLint 9, Vitest 3, Tailwind 4, next-intl 4, `radix-ui` (meta-pakiet); bez `@vitejs/plugin-react` (konflikt typów vite/vitest — JSX przez esbuild
    + `jsx: react-jsx`); brak tekstów w JSX wymusza reguła ESLint `react/jsx-no-literals` (`noStrings`) dla `src/**/*.tsx` poza testami (Dialog dostaje etykietę zamknięcia jako props); `next.config.ts`: `output: standalone`,
    `rewrites` `/api/:path*` -> `LEMAT_API_URL` (domyślnie `http://localhost:5080`); `build` używa `--webpack` (OQ-04); `gen:api` i `e2e` to placeholdery (TASK-081 / TASK-110); `katex.min.css` importowany w `globals.css`; `node_modules`/`.next` ignorowane (root `.gitignore` + `frontend/.gitignore`).

- [x] **TASK-007 [MVP] Skrypty weryfikacji i workflow CI**
  - **Opis:** `scripts/verify-backend.sh` (restore, build `-warnaserror`, test, `dotnet format --verify-no-changes`), `scripts/verify-frontend.sh` (ci, typecheck, lint, test, build, check-headers),
    `scripts/verify-all.sh`, `.github/workflows/ci.yml` (job backend z usługą PostgreSQL 16 / Testcontainers, job frontend, cache NuGet/npm; test kontraktu OpenAPI dojdzie w TASK-071), `README.md` — sekcja „Szybki start”
    (placeholder rozbudowywany w TASK-112).
  - **Zależności:** TASK-002, TASK-006.
  - **Realizuje:** 5.2 (CI); CLAUDE.md pkt 10.
  - **DoD:** `scripts/verify-all.sh` kończy się kodem 0 na szkielecie; workflow przechodzi walidację składni (`actionlint` lub `yamllint`); skrypty są wykonywalne i idempotentne.
  - **Pliki (utworzone/zmodyfikowane):** `scripts/verify-backend.sh`, `scripts/verify-frontend.sh`, `scripts/verify-all.sh` (opcje `--backend-only`/`--frontend-only`; wykonywalne), `.github/workflows/ci.yml`
    (job backend: usługa postgres:16 na 5433 + Testcontainers + `actions/cache` NuGet po hash csproj/`Directory.Packages.props`; job frontend: Node 22 + cache npm), `README.md` (sekcja „Szybki start”, zmodyfikowany).
    Dokumenty (handoff-guardian): `TASKS.md`, `HANDOFF.md`, `docs/CODEBASE_MAP.md`. Brak plików `.cs/.ts/.tsx` (nagłówki nie dotyczą).
  - **Weryfikacja:** `scripts/verify-all.sh` -> kod 0 (build `-warnaserror`, testy, `dotnet format`, `npm ci`/typecheck/lint/test/build/`check:headers`); `ci.yml`: `yaml.safe_load` + `yamllint` OK (`actionlint` niedostępny); `bash -n` OK.

- [ ] **TASK-008 [POST-MVP] Manifest nazw plików i testy `TestNazwPlikow`**
  - **Opis:** `Lemat.Architecture.Tests/Glossary/file-names.yaml` ze wpisami dla wszystkich plików `.cs` (6.11.2 p. 4), `FileNamesTest` (reguły a–g), `NazwyPlikowPojecUnikalne`, `DrzewaDokumentuSpelniajaRegulyNazw`; uzupełnij wpisy dla plików utworzonych w MVP.
  - **Zależności:** wszystkie `[MVP]` (lub dowolny punkt, w którym drzewo plików jest stabilne).  **Realizuje:** 6.11.2, ADR-075; 18.5 (m), C9-04.
  - **DoD:** wszystkie pliki `.cs` mają wpis i spełniają reguły (a)–(g); test negatywny dla pliku z polskim tokenem; segmenty tras `frontend/src/app` zgodne z regułą (g).

- [ ] **TASK-009 [POST-MVP] Pełny test słownika (3.4) i objęcie dokumentów planu**
  - **Opis:** `glossary-tokens.yaml` (tokeny z 3.5, `typy_glowne`), `glossary-exceptions.yaml`, cztery części testu 3.4 (typy C#, schemat bazy po migracjach, OpenAPI, schematy JSON modeli); uruchomienie na `CLAUDE.md`, `TASKS.md`, `HANDOFF.md` (część 3 i 4).
  - **Zależności:** TASK-013, TASK-071.  **Realizuje:** 3.4, ADR-049; 18.5 C5-09 p. 4.
  - **DoD:** test zielony na całym repo; każdy token ≥ 4 znaków identyfikatora ze słownika sprawdzany; wyjątki `zadan*/odpowiedz*/wariant*/krok*/wynik*/kolejk*/zdje*/wstrzym*` tylko z listy.

- [ ] **TASK-010 [POST-MVP] Obrazy Docker i pełny docker-compose (pilotaż)**
  - **Opis:** `backend/Dockerfile` (obraz `lemat-backend` z wszystkimi punktami wejścia + `efbundle`), `frontend/Dockerfile` (standalone), `docker-compose.yml` z usługami `caddy`, `web`, `api`, `worker`, `scheduler`, `migracje`, `seed` (profil `dev`), MinIO, mailpit; `deploy/Caddyfile` (`flush_interval -1` dla SSE).
  - **Zależności:** TASK-003, TASK-005, TASK-006.  **Realizuje:** 5.1, 5.3, 17.5; ADR-048.
  - **DoD:** `docker compose --profile dev up` uruchamia całość lokalnie na Mockach; healthchecki zielone; SSE nie jest buforowane przez Caddy.

- [ ] **TASK-011 [POST-MVP] `check_rtm.py` i `extract-ddl.sh` w CI**
  - **Opis:** skopiuj `docs/spec/tools/check_rtm.py` do `backend/tools/`, dodaj krok CI i weryfikację, że `extract-ddl.sh` daje brak różnic względem repozytorium.
  - **Zależności:** TASK-007, TASK-013.  **Realizuje:** 18 (sprawdzenia 1–17), ADR-063; 18.5 (r).
  - **DoD:** CI uruchamia skrypty; zmiana w `docs/spec/ddl.md` bez regeneracji plików SQL kończy CI błędem.

- [ ] **TASK-012 [POST-MVP] Granice modułów frontendu (dependency-cruiser)**
  - **Opis:** `.dependency-cruiser.cjs` z regułami (1)–(5) z 6.9 jako błąd CI + testy reguł na przykładach.
  - **Zależności:** TASK-006.  **Realizuje:** 6.9, ADR-054.
  - **DoD:** `npm run lint:deps` w CI; import spoza `index.ts` lub `fetch('/api/…')` poza `client.ts` powoduje błąd.

---

# Faza 2: Model domenowy i baza (DDL jako migracje SQL wg ADR-063, encje EF Core)

- [x] **TASK-013 [MVP] Schemat bazy: wycięcie DDL, migracje SQL, `Lemat.Tools migrate`, fixture testowy**
  - **Opis:** `backend/tools/extract-ddl.sh` wycina bloki (A) i (B) z `docs/spec/ddl.md` do `Persistence/Schema/ddl.sql` oraz plików `Persistence/Migrations/Sql/0000_schema.sql … 0007_api.sql`
    (zasoby osadzone, `ZasobSql.Wczytaj`). Minimalny `LematDbContext` (Npgsql, `EFCore.NamingConventions` snake_case), klasa migracji `SchematPoczatkowy` (`0001_InitialSchema.cs`, `[Migration("0001_SchematPoczatkowy")]`)
    wykonująca wyłącznie `migrationBuilder.Sql`; brak `ModelSnapshot`. `Lemat.Tools migrate` (stosuje migracje, idempotentnie; po migracji wywołuje `utworz_partycje_decyzji` na 12 miesięcy naprzód — U-08).
    `PostgresFixture` w `Lemat.Infrastructure.Tests` (Testcontainers PostgreSQL 16; zmienna `LEMAT_TEST_BAZA` jako alternatywa dla lokalnego PostgreSQL).
  - **Zależności:** TASK-001, TASK-002, TASK-004, TASK-005.
  - **Realizuje:** `docs/spec/ddl.md` (A, B), `docs/spec/ef-core.md` 8, 7.7; ADR-063, ADR-065 p. 6; 18.5 (i).
  - **DoD:** test integracyjny na pustej bazie bez ról i bez danych Mock: wszystkie obiekty z `ddl.md` istnieją (liczby tabel, partycji, typów enum i triggerów z nagłówka `ddl.md` są zapisane w teście; rzeczywiste liczby w raporcie);
    `MigracjeNaPustejBazieNieTworzaKonfiguracjiMock` (0 wierszy w `konfiguracja_modelu`); `Lemat.Tools migrate` uruchomione dwukrotnie nie zmienia bazy; `extract-ddl.sh` jest deterministyczny (ponowne uruchomienie → brak różnic w gicie);
    smoke funkcji `dodaj_dni_robocze` i `utworz_partycje_decyzji`; w bazie istnieją partycje `decyzja_modelu` na 12 miesięcy.
  - **Pliki (utworzone):** `backend/tools/extract-ddl.sh`, `backend/src/Lemat.Infrastructure/Persistence/Schema/ddl.sql`,
    `backend/src/Lemat.Infrastructure/Persistence/Migrations/Sql/0000_schema.sql`, `0001_functions.sql`, `0002_invariant_triggers.sql`, `0003_notifications.sql`, `0004_partitions.sql`, `0005_permissions.sql`, `0006_background_jobs.sql`, `0007_api.sql`,
    `backend/src/Lemat.Infrastructure/Persistence/LematDbContext.cs`, `backend/src/Lemat.Infrastructure/Persistence/MigratorBazy.cs`,
    `backend/src/Lemat.Infrastructure/Persistence/Migrations/0001_InitialSchema.cs`, `backend/src/Lemat.Infrastructure/Persistence/Migrations/ZasobSql.cs`,
    `backend/src/Lemat.Tools/Commands/MigrujBazeCommand.cs`,
    `backend/tests/Lemat.Infrastructure.Tests/Persistence/PostgresFixture.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/SchematBazyTests.cs`.
  - **Pliki (zmodyfikowane):** `backend/src/Lemat.Infrastructure/Lemat.Infrastructure.csproj` (EmbeddedResource), `backend/src/Lemat.Tools/Program.cs`, `backend/src/Lemat.Tools/Commands/CommandDispatcher.cs` (nagłówki @Task),
    `backend/tests/Lemat.Infrastructure.Tests/Lemat.Infrastructure.Tests.csproj` (referencja do `Lemat.Tools`). Dokumenty (handoff-guardian): `TASKS.md`, `HANDOFF.md`, `docs/CODEBASE_MAP.md`. Nagłówki @Role/@DependsOn/@UsedBy/@Task we wszystkich 9 nowych/zmienionych `.cs` sprawdzone, poprawne.
  - **Weryfikacja:** `dotnet build backend/Lemat.slnx -warnaserror` 0/0; `dotnet test` zielone (Infrastructure.Tests 15, Architecture.Tests 104), także z `LEMAT_TEST_BAZA`; `dotnet format --verify-no-changes` czysto; `extract-ddl.sh` powtórzony -> identyczne sumy, `--check` bez różnic.
    Rzeczywiste liczby po `migrate`: 61 tabel (bez partycji i `__historia_migracji`), 43 typy enum, 15 triggerów, 13 partycji `decyzja_modelu` (bieżący miesiąc + 12), `konfiguracja_modelu` = 0 wierszy, historia: `0001_SchematPoczatkowy`.
    Decyzje: tabela historii `__historia_migracji`; rola `lemat_migracje` NIE utworzona (do decyzji PO / TASK-112); role `lemat_api/worker/scheduler` tworzy 0005 jako NOLOGIN, gdy brak; testy zgodności modelu EF (ef-core.md 8) dochodzą z TASK-037.

- [x] **TASK-014 [MVP] `Lemat.Domain.Common`: agregat, identyfikatory, wspólne typy, konwencja nazw**
  - **Opis:** `Agregat<TId>` (`version`, kolekcja zdarzeń), `IZdarzenieDomenowe`, wyjątki domenowe (`WyjatekDomenowy`, `NiezmiennikNaruszonyException(kod)`), `PoziomMatury`, `RodzajZadania`, `PoziomTrudnosci` (1–5), generator UUIDv7,
    `KonwencjaNazw` (konwersje `PascalCase` ↔ `snake_case` ↔ `UPPER_SNAKE` ↔ `kebab-case`, ADR-049 / C9-03), baza interfejsów dla identyfikatorów silnie typowanych (`readonly record struct`; każdy typ `*Id` powstaje w TASK modułu, jeden plik na typ).
  - **Zależności:** TASK-001.
  - **Realizuje:** 3.5 (wiersze 9, 28, 31), 3.6, 6.2 (`Wspolne`), 6.11.2 p. 1; ADR-049, ADR-016.
  - **DoD:** testy `Lemat.Domain.Tests/Common`: round-trip konwencji dla co najmniej 30 nazw (np. `Wstepna` ↔ `WSTEPNA`, `OtwarteZDowodem` ↔ `OTWARTE_Z_DOWODEM`, `ZgloszenieBleduOdczytu` ↔ `zgloszenie_bledu_odczytu`);
    `PoziomTrudnosci` odrzuca wartości poza 1–5; `Agregat` zwiększa `version` przy zapisie zdarzenia zgodnie z kontraktem 3.6; `Lemat.Domain` nadal bez pakietów.
  - **Pliki (utworzone):** `backend/src/Lemat.Domain/Common/Aggregate.cs`, `backend/src/Lemat.Domain/Common/DifficultyLevel.cs`, `backend/src/Lemat.Domain/Common/DomainException.cs`, `backend/src/Lemat.Domain/Common/ExerciseKind.cs`, `backend/src/Lemat.Domain/Common/IDomainEvent.cs`, `backend/src/Lemat.Domain/Common/IIdentifier.cs`, `backend/src/Lemat.Domain/Common/MaturaLevel.cs`, `backend/src/Lemat.Domain/Common/NamingConvention.cs`, `backend/src/Lemat.Domain/Common/UuidGenerator.cs`, `backend/tests/Lemat.Domain.Tests/Common/AggregateTests.cs`, `backend/tests/Lemat.Domain.Tests/Common/CommonTypesTests.cs`, `backend/tests/Lemat.Domain.Tests/Common/NamingConventionTests.cs`.
  - **Weryfikacja:** build `-warnaserror` OK; `dotnet test` zielone (Domain 56, Architecture 104, Infrastructure 15); `dotnet format --verify-no-changes` czysto. Odstępstwa: `Agregat.Version` rośnie przez publiczne `PotwierdzZapis()` wołane przez `LematDbContext` (TASK-015), `Zglos()` wersji nie zmienia.

- [x] **TASK-015 [MVP] Warstwa trwałości: konwencje EF Core, `IJednostkaPracy`, tłumacz błędów bazy**
  - **Opis:** Dokończ `LematDbContext` wg `ef-core.md` 1–5: rejestracja typów enum PostgreSQL przez `MapEnum` z translatorem `UPPER_SNAKE` (rejestr wszystkich 43 typów), konwertery identyfikatorów silnie typowanych (generyczny `IdConverter<TId>`),
    pomocnicy `jsonb` (`OwnsOne/OwnsMany(...).ToJson()` oraz `JsonDocument`), token współbieżności `version`, `IJednostkaPracy` + `JednostkaPracyEf` (`BeginTransactionAsync`, `CommitAsync`, ta sama transakcja dla EF i poleceń Npgsql),
    `TlumaczBledowBazy` (`SqlState P0001` → `NiezmiennikNaruszonyException(kod)`, `23505` → kod z `KatalogIndeksowUnikalnych`), `PersistenceRegistration.DodajPersystencje`.
  - **Zależności:** TASK-013, TASK-014.
  - **Realizuje:** `docs/spec/ef-core.md` 1–5; 5.2 (Dostęp do danych), 15.1; ADR-056, ADR-063.
  - **DoD:** testy `Lemat.Infrastructure.Tests`: odczyt/zapis typu enum i identyfikatora silnie typowanego przez bazę; `RAISE EXCEPTION 'TEST_KOD'` → `NiezmiennikNaruszonyException` z kodem `TEST_KOD`; naruszenie `ux_*` → kod z katalogu;
    konflikt `version` → `DbUpdateConcurrencyException` mapowany na wyjątek aplikacyjny; `IJednostkaPracy` bez otwartej transakcji odrzuca operacje wymagające transakcji.
  - **Pliki (utworzone):** `backend/src/Lemat.Application/Common/UnitOfWork.cs`, `backend/src/Lemat.Application/Common/ProcessKind.cs`, `backend/src/Lemat.Application/Common/ConcurrencyConflictException.cs`, `backend/src/Lemat.Domain/Common/VersionBumpExclusionAttribute.cs`, `backend/src/Lemat.Infrastructure/Persistence/Conventions/UpperSnakeNameTranslator.cs`, `backend/src/Lemat.Infrastructure/Persistence/Conventions/EnumTypeMapping.cs`, `backend/src/Lemat.Infrastructure/Persistence/Conventions/IdentifierConversions.cs`, `backend/src/Lemat.Infrastructure/Persistence/Conventions/JsonbSupport.cs`, `backend/src/Lemat.Infrastructure/Persistence/Conventions/UpperSnakeEnumConverter.cs`, `backend/src/Lemat.Infrastructure/Persistence/Interceptors/TimestampInterceptor.cs`, `backend/src/Lemat.Infrastructure/Persistence/Interceptors/AggregateVersionInterceptor.cs`, `backend/src/Lemat.Infrastructure/Persistence/DatabaseErrorTranslator.cs`, `backend/src/Lemat.Infrastructure/Persistence/UnitOfWorkEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/PersistenceRegistration.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/PersistenceConventionsTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/UnitOfWorkTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/Support/TestPersistenceModel.cs`.
  - **Pliki (zmodyfikowane):** `backend/src/Lemat.Domain/Common/Aggregate.cs` (dodano `IKorzenAgregatu`), `backend/src/Lemat.Infrastructure/Persistence/LematDbContext.cs` (konwencje: string unicode, `KonwersjeIdentyfikatorow`).
  - **Weryfikacja:** build `-warnaserror` OK; `dotnet test` zielone (Domain 56, Application 14, Infrastructure 50, Architecture 104, Api 3); `dotnet format --verify-no-changes` czysto; testy bazy na `LEMAT_TEST_BAZA` (lokalny PG). Odstępstwa: hosty jeszcze nie wołają `DodajPersystencje`; rozsyłanie zdarzeń domenowych i `ZlecaniePracSql` w TASK-024; katalog `ux_*` zawiera tylko pary jednoznaczne; encje wewnętrzne niebędące owned — w TASK-016+.

- [x] **TASK-016 [MVP] Moduł Konta: agregaty, encje EF, repozytoria**
  - **Opis:** `Lemat.Domain.Accounts`: `Uzytkownik`, `Uczen`, `Nauczyciel` (flagi `WeryfikatorGlobalny`, `RecenzentTresci`, `UmowaPoufnosciAt`, `DeklarowaneMinutyWeryfikacji`), `RolaUzytkownika`, `StatusKonta`,
    `TokenOdswiezania` (rodzina tokenów), `ProbaLogowania`, `UzytkownikId/UczenId/NauczycielId`, `GrupaEksperymentu` (`procent_grupy_eksperymentu` = fnv1a(uzytkownik_id) mod 100). `Lemat.Application.Accounts`: interfejsy repozytoriów, `IKontaFasada`
    (odczyt ról/statusu, rejestracja bez HTTP). Konfiguracje EF wg `ef-core.md` 6.1; implementacje repozytoriów w `Lemat.Infrastructure.Persistence`.
  - **Zależności:** TASK-015.
  - **Realizuje:** 3.5 (wiersze 1, 2, 26, 84), 7 (`uzytkownik`, `uczen`, `nauczyciel`, `token_odswiezania`, `proba_logowania`), 8.x (statusy kont); ADR-020, ADR-044 (model).
  - **DoD:** testy domeny: tworzenie Ucznia wymaga oświadczenia 16+, e-mail normalizowany (citext), Nauczyciel startuje jako `OCZEKUJE_NA_ZATWIERDZENIE`, `GrupaEksperymentu` deterministyczna (wektory fnv1a);
    testy bazy: zapis `uzytkownik` + `uczen` w jednej transakcji (FK odroczony), duplikat e-mail → `EMAIL_ZAJETY`, CHECK `weryfikator_globalny` wymaga `umowa_poufnosci_at` i ≥ 90 min.
  - **Pliki (utworzone):** `backend/src/Lemat.Application/Accounts/AccountRegistrationInput.cs`, `backend/src/Lemat.Application/Accounts/AccountSummary.cs`, `backend/src/Lemat.Application/Accounts/AccountsFacade.cs`, `backend/src/Lemat.Application/Accounts/AccountsRegistration.cs`, `backend/src/Lemat.Application/Accounts/IAccountsFacade.cs`, `backend/src/Lemat.Application/Accounts/ILoginAttemptRepository.cs`, `backend/src/Lemat.Application/Accounts/IRefreshTokenRepository.cs`, `backend/src/Lemat.Application/Accounts/IStudentRepository.cs`, `backend/src/Lemat.Application/Accounts/ITeacherRepository.cs`, `backend/src/Lemat.Application/Accounts/IUserRepository.cs`, `backend/src/Lemat.Application/Accounts/LoginData.cs`, `backend/src/Lemat.Domain/Accounts/AccountBlocked.cs`, `backend/src/Lemat.Domain/Accounts/AccountDeletionPending.cs`, `backend/src/Lemat.Domain/Accounts/AccountRestored.cs`, `backend/src/Lemat.Domain/Accounts/AccountStatus.cs`, `backend/src/Lemat.Domain/Accounts/ExperimentGroup.cs`, `backend/src/Lemat.Domain/Accounts/GracePeriod.cs`, `backend/src/Lemat.Domain/Accounts/LoginAttempt.cs`, `backend/src/Lemat.Domain/Accounts/PermissionRevoked.cs`, `backend/src/Lemat.Domain/Accounts/RefreshToken.cs`, `backend/src/Lemat.Domain/Accounts/RefreshTokenId.cs`, `backend/src/Lemat.Domain/Accounts/Student.cs`, `backend/src/Lemat.Domain/Accounts/StudentId.cs`, `backend/src/Lemat.Domain/Accounts/Teacher.cs`, `backend/src/Lemat.Domain/Accounts/TeacherId.cs`, `backend/src/Lemat.Domain/Accounts/TeacherPermission.cs`, `backend/src/Lemat.Domain/Accounts/User.cs`, `backend/src/Lemat.Domain/Accounts/UserId.cs`, `backend/src/Lemat.Domain/Accounts/UserRole.cs`, `backend/src/Lemat.Domain/Common/Fnv1a.cs`, `backend/src/Lemat.Domain/Content/CurriculumSectionId.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Accounts/LoginAttemptEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Accounts/RefreshTokenEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Accounts/StudentEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Accounts/TeacherEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Accounts/UserEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Accounts/LoginAttemptRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Accounts/RefreshTokenRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Accounts/StudentRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Accounts/TeacherRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Accounts/UserRepositoryEf.cs`, `backend/tests/Lemat.Application.Tests/Accounts/AccountsFacadeTests.cs`, `backend/tests/Lemat.Domain.Tests/Accounts/ExperimentGroupTests.cs`, `backend/tests/Lemat.Domain.Tests/Accounts/RefreshTokenTests.cs`, `backend/tests/Lemat.Domain.Tests/Accounts/StudentTests.cs`, `backend/tests/Lemat.Domain.Tests/Accounts/TeacherTests.cs`, `backend/tests/Lemat.Domain.Tests/Accounts/UserTests.cs`, `backend/tests/Lemat.Domain.Tests/Common/Fnv1aTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/AccountsPersistenceTests.cs`.
  - **Pliki (zmodyfikowane):** `backend/src/Lemat.Infrastructure/Persistence/Conventions/EnumTypeMapping.cs`, `backend/src/Lemat.Infrastructure/Persistence/PersistenceRegistration.cs` (rejestracja `rola_uzytkownika`, `status_konta` w `Poczatkowe`; repozytoria scoped).
  - **Weryfikacja:** build `-warnaserror` 0/0; `dotnet test` zielone (Domain 122, Application 20, Infrastructure 70, Architecture 104, Api 3, Models 1, Agents 1); `dotnet format --verify-no-changes` czysto. Odstępstwa: fnv1a = 32-bit nad 16 bajtami UUID (big-endian) mod 100 (do potwierdzenia, OQ-09); hasher argon2id i rotacja tokenów z oknem tolerancji (ADR-057) w TASK-072; `GrupaEksperymentu.Strategia` pominięta do TASK-019; hosty nie wołają jeszcze `DodajModulKonta`; dodano `DzialId` (`Lemat.Domain.Content`) na potrzeby `Uczen.WybranyDzialId`.

- [x] **TASK-017 [MVP] Moduł Treści: Działy, Umiejętności, Zadania z wersjami i Kryteriami, Wkładki**
  - **Opis:** `Lemat.Domain.Content`: `Dzial`, `Umiejetnosc`, `Zadanie` + `ZadanieWersja`, `PochodzenieZadania`, `ZasiegZadania`, `StatusZadania`, `KryteriumOceniania`, `RozwiazanieWzorcowe`, `WariantRozwiazania`, `WynikKoncowyWzorcowy`,
    `OpcjeZamkniete` (`WYBOR_JEDNOKROTNY`, `PRAWDA_FALSZ` z regułą punktu), `WkladkaTeoretyczna` + `StatusWkladki`. `Lemat.Application.Content`: `ITresciFasada` (metody odczytu: `PobierzZadanieDoOcenyAsync`, `PobierzZadanieUczniaAsync`,
    dobór kandydatów dla rekomendacji, lista Działów/Umiejętności), repozytoria. Konfiguracje EF (jsonb wg 7.5.1–7.5.3).
  - **Zależności:** TASK-015, TASK-016.
  - **Realizuje:** 3.5 (wiersze 3, 6, 7, 11, 14, 36, 37, 39, 41), 7.4, 7.5.1–7.5.3, 8.4 (stany Zadania), 6.2 (Tresci); ADR-012, ADR-063.
  - **DoD:** testy bazy: `trg_suma_kryteriow` (suma punktów kryteriów wariantu = `punkty_max` wersji, naruszenie → kod), `trg_umiejetnosc_glowna_w_dziale`, `diagnostyczne` tylko dla `ZAMKNIETE`/`GLOBALNY`;
    round-trip jsonb dla `opcje_zamkniete`, `rozwiazanie_wzorcowe`, `wynik_koncowy_wzorcowy`; testy `OpcjeZamkniete.Ocen` dla obu form (PRAWDA_FALSZ: punkt tylko gdy wszystkie stwierdzenia poprawne); wersjonowanie Zadania (nowa wersja zamyka starą, `zastapiona_at`).
  - **Pliki (utworzone):** `backend/src/Lemat.Application/Content/ContentFacade.cs`, `backend/src/Lemat.Application/Content/ContentRegistration.cs`, `backend/src/Lemat.Application/Content/CurriculumSummaries.cs`, `backend/src/Lemat.Application/Content/ExerciseForGrading.cs`, `backend/src/Lemat.Application/Content/ExerciseForStudent.cs`, `backend/src/Lemat.Application/Content/IContentFacade.cs`, `backend/src/Lemat.Application/Content/ICurriculumRepository.cs`, `backend/src/Lemat.Application/Content/IExerciseRepository.cs`, `backend/src/Lemat.Application/Content/ITheoryInsertRepository.cs`, `backend/src/Lemat.Application/Content/SelectionCandidate.cs`, `backend/src/Lemat.Domain/Content/CkeSheetId.cs`, `backend/src/Lemat.Domain/Content/ClosedForm.cs`, `backend/src/Lemat.Domain/Content/ClosedOptions.cs`, `backend/src/Lemat.Domain/Content/CriteriaRules.cs`, `backend/src/Lemat.Domain/Content/CurriculumSection.cs`, `backend/src/Lemat.Domain/Content/DifficultyCalibration.cs`, `backend/src/Lemat.Domain/Content/Exercise.cs`, `backend/src/Lemat.Domain/Content/ExerciseId.cs`, `backend/src/Lemat.Domain/Content/ExerciseOrigin.cs`, `backend/src/Lemat.Domain/Content/ExerciseScope.cs`, `backend/src/Lemat.Domain/Content/ExerciseStatus.cs`, `backend/src/Lemat.Domain/Content/ExerciseVersion.cs`, `backend/src/Lemat.Domain/Content/ExerciseVersionId.cs`, `backend/src/Lemat.Domain/Content/GenerationOrderId.cs`, `backend/src/Lemat.Domain/Content/GradingCriterion.cs`, `backend/src/Lemat.Domain/Content/InsertStatus.cs`, `backend/src/Lemat.Domain/Content/ReferenceFinalResult.cs`, `backend/src/Lemat.Domain/Content/ReferenceSolution.cs`, `backend/src/Lemat.Domain/Content/Skill.cs`, `backend/src/Lemat.Domain/Content/SkillId.cs`, `backend/src/Lemat.Domain/Content/SolutionVariant.cs`, `backend/src/Lemat.Domain/Content/TheoryInsert.cs`, `backend/src/Lemat.Domain/Content/TheoryInsertId.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Content/ContentJson.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Content/CurriculumEntityTypeConfigurations.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Content/ExerciseEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Content/TheoryInsertEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Content/CurriculumRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Content/ExerciseRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Content/TheoryInsertRepositoryEf.cs`, `backend/tests/Lemat.Application.Tests/Content/ContentFacadeTests.cs`, `backend/tests/Lemat.Domain.Tests/Content/ClosedOptionsTests.cs`, `backend/tests/Lemat.Domain.Tests/Content/ContentTestData.cs`, `backend/tests/Lemat.Domain.Tests/Content/CriteriaRulesTests.cs`, `backend/tests/Lemat.Domain.Tests/Content/ExerciseTests.cs`, `backend/tests/Lemat.Domain.Tests/Content/ReferenceValuesTests.cs`, `backend/tests/Lemat.Domain.Tests/Content/TheoryInsertTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/ContentPersistenceTests.cs`.
  - **Pliki (zmodyfikowane):** `backend/src/Lemat.Infrastructure/Persistence/Conventions/EnumTypeMapping.cs`, `backend/src/Lemat.Infrastructure/Persistence/DatabaseErrorTranslator.cs`, `backend/src/Lemat.Infrastructure/Persistence/PersistenceRegistration.cs` (+5 enumów Treści w `Poczatkowe`, 4 repozytoria scoped, kod `WKLADKA_OPUBLIKOWANA_ISTNIEJE` dla `ux_wkladka_opublikowana`).
  - **Weryfikacja:** build `-warnaserror` 0/0; `dotnet test` zielone (Domain 179, Application 29, Architecture 104, Infrastructure 103, Api 3, Agents 1, Models 1); `dotnet format --verify-no-changes` czysto. Odstępstwa: jsonb przez DTO `ContentJson` + ValueConverter (nie `OwnsOne.ToJson`, bo enumy zapisałyby się jako liczby); `Zadanie.AktualnaWersjaId` jako zapisana właściwość; `Dzial`/`Umiejetnosc`/`KalibracjaTrudnosci` bez `Version`; nowe kody błędów spoza 12.2 (do TASK-071); dane referencyjne (13 Działów, Umiejętności) ładuje TASK-025; hosty nie wołają jeszcze `DodajModulTresci`.

- [x] **TASK-018 [MVP] Moduły Klasy, Modele i Kalibracja (minimum): Klasa, Konfiguracja modelu, Decyzja modelu, Próg**
  - **Opis:** `Lemat.Domain.Classes`: `Klasa` (kod zaproszenia, ważność, archiwizacja), `CzlonkostwoKlasy`, `IKlasyFasada`. `Lemat.Domain.Models`: `KonfiguracjaModelu`, `RolaModelu` (8 ról), `ParametryModelu`, `DecyzjaModelu`,
    `DecyzjaSkladowa`, `PlanPytan`, `PytanieWPlanie`, `Pytanie` (`PytanieNoul`/`PytanieScore`/`PytanieChoice`), `KontekstDecyzji`, `TerminPlanu`, `RezultatPytan`, `IModeleFasada`. `Lemat.Domain.Calibration`: `KalibratorPewnosci`
    (MVP: metoda tożsamościowa), `ProgPewnosci` (zakres 0.800–0.990, `TrybPelnejWeryfikacji` = 1.000), `ZrodloProgu`, `IKalibracjaFasada` (odczyt aktywnego progu i kalibratora dla konfiguracji i rodzaju). Konfiguracje EF wg `ef-core.md` 6.5–6.6.
  - **Zależności:** TASK-015, TASK-016.
  - **Realizuje:** 3.5 (wiersze 17, 23, 27, 30, 52, 54–56, 66, 79, 103), 7.x, 10.4.4; ADR-003, ADR-006, ADR-026.
  - **DoD:** testy: kod zaproszenia generowany/unieważniany, unikalność aktywnego członkostwa; zapis `DecyzjaModelu` trafia do właściwej partycji miesięcznej i odtwarza rozkład prawdopodobieństw;
    `ProgPewnosci` odrzuca wartości spoza zakresu, dla dowodu wymusza 1.000 (CHECK); `KalibratorPewnosci.Tozsamosciowy` zwraca wejście; walidacja `PlanPytan` (zależności między pytaniami, ADR-026).
  - **Pliki (utworzone):** `backend/src/Lemat.Application/Calibration/ActiveCalibration.cs`, `backend/src/Lemat.Application/Calibration/CalibrationFacade.cs`, `backend/src/Lemat.Application/Calibration/CalibrationRegistration.cs`, `backend/src/Lemat.Application/Calibration/ICalibrationFacade.cs`, `backend/src/Lemat.Application/Calibration/IConfidenceCalibratorRepository.cs`, `backend/src/Lemat.Application/Calibration/IConfidenceThresholdRepository.cs`, `backend/src/Lemat.Application/Classes/ClassSummary.cs`, `backend/src/Lemat.Application/Classes/ClassesFacade.cs`, `backend/src/Lemat.Application/Classes/ClassesRegistration.cs`, `backend/src/Lemat.Application/Classes/IClassRepository.cs`, `backend/src/Lemat.Application/Classes/IClassesFacade.cs`, `backend/src/Lemat.Application/Models/DecisionPlanDeadline.cs`, `backend/src/Lemat.Application/Models/IModelConfigurationRepository.cs`, `backend/src/Lemat.Application/Models/IModelDecisionRepository.cs`, `backend/src/Lemat.Application/Models/IModelsFacade.cs`, `backend/src/Lemat.Application/Models/ModelConfigurationSummary.cs`, `backend/src/Lemat.Application/Models/ModelsFacade.cs`, `backend/src/Lemat.Application/Models/ModelsRegistration.cs`, `backend/src/Lemat.Domain/Calibration/CalibrationPoints.cs`, `backend/src/Lemat.Domain/Calibration/CalibratorMethod.cs`, `backend/src/Lemat.Domain/Calibration/CalibratorSnapshot.cs`, `backend/src/Lemat.Domain/Calibration/CalibratorSource.cs`, `backend/src/Lemat.Domain/Calibration/ConfidenceCalibrator.cs`, `backend/src/Lemat.Domain/Calibration/ConfidenceCalibratorId.cs`, `backend/src/Lemat.Domain/Calibration/ConfidenceThreshold.cs`, `backend/src/Lemat.Domain/Calibration/ConfidenceThresholdId.cs`, `backend/src/Lemat.Domain/Calibration/ThresholdSnapshot.cs`, `backend/src/Lemat.Domain/Calibration/ThresholdSource.cs`, `backend/src/Lemat.Domain/Classes/Class.cs`, `backend/src/Lemat.Domain/Classes/ClassId.cs`, `backend/src/Lemat.Domain/Classes/ClassMembership.cs`, `backend/src/Lemat.Domain/Classes/ClassTransferred.cs`, `backend/src/Lemat.Domain/Classes/InvitationCode.cs`, `backend/src/Lemat.Domain/Classes/InvitationCodeGenerator.cs`, `backend/src/Lemat.Domain/Models/ChoiceQuestion.cs`, `backend/src/Lemat.Domain/Models/ComponentDecision.cs`, `backend/src/Lemat.Domain/Models/DecisionContext.cs`, `backend/src/Lemat.Domain/Models/DecisionModelUsed.cs`, `backend/src/Lemat.Domain/Models/FallbackReason.cs`, `backend/src/Lemat.Domain/Models/FormulaRepresentation.cs`, `backend/src/Lemat.Domain/Models/JevMode.cs`, `backend/src/Lemat.Domain/Models/ModelConfiguration.cs`, `backend/src/Lemat.Domain/Models/ModelConfigurationId.cs`, `backend/src/Lemat.Domain/Models/ModelDecision.cs`, `backend/src/Lemat.Domain/Models/ModelDecisionId.cs`, `backend/src/Lemat.Domain/Models/ModelParameters.cs`, `backend/src/Lemat.Domain/Models/ModelProviders.cs`, `backend/src/Lemat.Domain/Models/ModelRole.cs`, `backend/src/Lemat.Domain/Models/NoulQuestion.cs`, `backend/src/Lemat.Domain/Models/PlannedQuestion.cs`, `backend/src/Lemat.Domain/Models/Question.cs`, `backend/src/Lemat.Domain/Models/QuestionCondition.cs`, `backend/src/Lemat.Domain/Models/QuestionOptions.cs`, `backend/src/Lemat.Domain/Models/QuestionPlan.cs`, `backend/src/Lemat.Domain/Models/QuestionResult.cs`, `backend/src/Lemat.Domain/Models/QuestionType.cs`, `backend/src/Lemat.Domain/Models/ScoreQuestion.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Calibration/CalibrationJson.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Calibration/ConfidenceCalibratorEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Calibration/ConfidenceThresholdEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Classes/ClassEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Models/ModelConfigurationEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Models/ModelDecisionEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Models/ModelsJson.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Calibration/ConfidenceCalibratorRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Calibration/ConfidenceThresholdRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Classes/ClassRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Models/ModelConfigurationRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Models/ModelDecisionRepositoryEf.cs`, `backend/tests/Lemat.Application.Tests/Calibration/CalibrationFacadeTests.cs`, `backend/tests/Lemat.Application.Tests/Classes/ClassesFacadeTests.cs`, `backend/tests/Lemat.Application.Tests/Models/ModelsFacadeTests.cs`, `backend/tests/Lemat.Application.Tests/Support/PassThroughUnitOfWork.cs`, `backend/tests/Lemat.Domain.Tests/Calibration/CalibrationTests.cs`, `backend/tests/Lemat.Domain.Tests/Classes/ClassTests.cs`, `backend/tests/Lemat.Domain.Tests/Models/ComponentDecisionTests.cs`, `backend/tests/Lemat.Domain.Tests/Models/ModelConfigurationTests.cs`, `backend/tests/Lemat.Domain.Tests/Models/QuestionPlanTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/CalibrationPersistenceTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/ClassesPersistenceTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/ModelsPersistenceTests.cs`.
  - **Pliki (zmodyfikowane):** `backend/src/Lemat.Infrastructure/Persistence/Conventions/EnumTypeMapping.cs` (+5 enumów), `backend/src/Lemat.Infrastructure/Persistence/DatabaseErrorTranslator.cs` (+3 kody), `backend/src/Lemat.Infrastructure/Persistence/PersistenceRegistration.cs` (+5 repozytoriów scoped), `backend/tests/Lemat.Architecture.Tests/Glossary/ForbiddenSynonymAnalyzer.cs`, `backend/tests/Lemat.Architecture.Tests/Glossary/ZakazaneSynonimyWIdentyfikatorachTests.cs` (wąski wyjątek `PytanieScore`/`TypPytania.Score` — typ pytania Jev SCORE).
  - **Weryfikacja:** build `-warnaserror` 0/0; `dotnet test` zielone (Domain 269, Application 52, Architecture 106, Infrastructure 140, Api 3, Agents 1, Models 1); `dotnet format --verify-no-changes` czysto. Odstępstwa: fasady w `Lemat.Application.<Moduł>` (nie w Domain); `KonfiguracjaModelu` z tokenem `xmin`, `ProgPewnosci`/`KalibratorPewnosci`/`DecyzjaModelu` bez `version`; `DecyzjaModelu.RozwiazanieId` jako `Guid?` (typ `RozwiazanieId` w TASK-020); nowe kody błędów spoza 12.2 (do TASK-071); hosty nie wołają jeszcze `DodajModulKlasy/Modele/Kalibracji`; brak mocków.

- [x] **TASK-019 [MVP] Moduły Profil i Rekomendacje: Profil ucznia (Elo), Test diagnostyczny, Ścieżka nauki**
  - **Opis:** `Lemat.Domain.Profile`: `ProfilUcznia`, `OpanowanieUmiejetnosci` (`theta`, inicjalizacja `DOMYSLNA`), `PoziomOpanowania`, `WynikZadania` (+ źródło, waga Oceny wstępnej 0.5, unieważnienie), `TestDiagnostyczny` (`W_TOKU`/`ZAKONCZONY`/`POMINIETY`),
    `PozycjaTestuDiagnostycznego`, czysty model Elo (10.1.3: aktualizacja `theta`, `trudnosc_b`, propagacja w Dziale, K = 0.8, `poziom_z_b`). `Lemat.Domain.Recommendations`: `SciezkaNauki`, `ElementSciezki`
    (maszyna stanów 8.5), `Rekomendacja`, `Seria`, `CelElementu`, `TypElementuSciezki`. Fasady `IProfilFasada`, `IRekomendacjeFasada` (bez zależności od Orchestration). Konfiguracje EF wg `ef-core.md` 6.5.
  - **Zależności:** TASK-016, TASK-017.
  - **Realizuje:** 3.5 (wiersze 8, 10, 33, 57–61, 68, 69), 8.5, 8.6, 10.1.3, 9.1; ADR-010, ADR-018, ADR-030.
  - **DoD:** testy Elo na tablicy przypadków z 10.1.3 (wartości liczbowe z dokumentu), property-test FsCheck: `theta` zmienia się monotonicznie z poprawną/błędną odpowiedzią; przejścia `ElementSciezki` (każde dozwolone i każde zabronione → wyjątek);
    `ux_jeden_proponowany` → `ELEMENT_JUZ_PROPONOWANY`; `ux_test_jeden` blokuje drugi test; `WynikZadania` z Oceny wstępnej ma wagę 0.5.
  - **Pliki (utworzone):** `backend/src/Lemat.Application/Profile/IDiagnosticTestRepository.cs`, `backend/src/Lemat.Application/Profile/IDifficultyCalibrationUpdater.cs`, `backend/src/Lemat.Application/Profile/IExerciseEloParameters.cs`, `backend/src/Lemat.Application/Profile/IExerciseResultRepository.cs`, `backend/src/Lemat.Application/Profile/IProfileFacade.cs`, `backend/src/Lemat.Application/Profile/IProfileRepository.cs`, `backend/src/Lemat.Application/Profile/ProfileFacade.cs`, `backend/src/Lemat.Application/Profile/ProfileRegistration.cs`, `backend/src/Lemat.Application/Profile/ProfileSummaries.cs`, `backend/src/Lemat.Application/Recommendations/ILearningPathRepository.cs`, `backend/src/Lemat.Application/Recommendations/IRecommendationRepository.cs`, `backend/src/Lemat.Application/Recommendations/IRecommendationsFacade.cs`, `backend/src/Lemat.Application/Recommendations/RecommendationSummaries.cs`, `backend/src/Lemat.Application/Recommendations/RecommendationsFacade.cs`, `backend/src/Lemat.Application/Recommendations/RecommendationsRegistration.cs`, `backend/src/Lemat.Domain/Profile/DiagnosticTest.cs`, `backend/src/Lemat.Domain/Profile/DiagnosticTestId.cs`, `backend/src/Lemat.Domain/Profile/DiagnosticTestItem.cs`, `backend/src/Lemat.Domain/Profile/DiagnosticTestItemId.cs`, `backend/src/Lemat.Domain/Profile/EloExerciseParameters.cs`, `backend/src/Lemat.Domain/Profile/EloModel.cs`, `backend/src/Lemat.Domain/Profile/ExerciseResult.cs`, `backend/src/Lemat.Domain/Profile/ExerciseResultId.cs`, `backend/src/Lemat.Domain/Profile/MasteryInitialization.cs`, `backend/src/Lemat.Domain/Profile/MasteryLevel.cs`, `backend/src/Lemat.Domain/Profile/Profile.cs`, `backend/src/Lemat.Domain/Profile/ResultInvalidationReason.cs`, `backend/src/Lemat.Domain/Profile/ResultSource.cs`, `backend/src/Lemat.Domain/Profile/SkillMastery.cs`, `backend/src/Lemat.Domain/Profile/TestStatus.cs`, `backend/src/Lemat.Domain/Recommendations/ElementGoal.cs`, `backend/src/Lemat.Domain/Recommendations/LearningPath.cs`, `backend/src/Lemat.Domain/Recommendations/LearningPathElement.cs`, `backend/src/Lemat.Domain/Recommendations/LearningPathElementId.cs`, `backend/src/Lemat.Domain/Recommendations/LearningPathElementStatus.cs`, `backend/src/Lemat.Domain/Recommendations/LearningPathElementType.cs`, `backend/src/Lemat.Domain/Recommendations/LearningPathId.cs`, `backend/src/Lemat.Domain/Recommendations/Recommendation.cs`, `backend/src/Lemat.Domain/Recommendations/RecommendationId.cs`, `backend/src/Lemat.Domain/Recommendations/RecommendationRule.cs`, `backend/src/Lemat.Domain/Recommendations/RecommendationStrategy.cs`, `backend/src/Lemat.Domain/Recommendations/Series.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Profile/DiagnosticTestEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Profile/ExerciseResultEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Profile/ProfileEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Recommendations/LearningPathEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Configurations/Recommendations/RecommendationEntityTypeConfiguration.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Profile/DiagnosticTestRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Profile/DifficultyCalibrationUpdaterSql.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Profile/ExerciseEloParametersEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Profile/ExerciseResultRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Profile/ProfileRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Recommendations/LearningPathRepositoryEf.cs`, `backend/src/Lemat.Infrastructure/Persistence/Repositories/Recommendations/RecommendationRepositoryEf.cs`, `backend/tests/Lemat.Domain.Tests/Profile/DiagnosticTestTests.cs`, `backend/tests/Lemat.Domain.Tests/Profile/EloModelTests.cs`, `backend/tests/Lemat.Domain.Tests/Profile/ExerciseResultTests.cs`, `backend/tests/Lemat.Domain.Tests/Profile/ProfileTests.cs`, `backend/tests/Lemat.Domain.Tests/Recommendations/LearningPathElementTests.cs`, `backend/tests/Lemat.Domain.Tests/Recommendations/LearningPathTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/ProfilePersistenceTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/RecommendationsPersistenceTests.cs`, `backend/tests/Lemat.Infrastructure.Tests/Persistence/Support/ZadaniaTestowe.cs`.
  - **Pliki (zmodyfikowane):** `backend/src/Lemat.Infrastructure/Persistence/Conventions/EnumTypeMapping.cs`, `backend/src/Lemat.Infrastructure/Persistence/Interceptors/AggregateVersionInterceptor.cs`, `backend/src/Lemat.Infrastructure/Persistence/PersistenceRegistration.cs` (+8 enumów Profilu/Rekomendacji, +7 rejestracji repozytoriów/usług, interceptor wersji podnosi `version` korzenia także dla encji wewnętrznych z wymaganym FK 1:N do korzenia — potrzebne dla `ElementSciezki`).
  - **Weryfikacja:** build `-warnaserror` 0/0; `dotnet test` zielone (Domain 394, Application 52, Infrastructure 167, Architecture 106, Api 3, Agents 1, Models 1; testy bazy na lokalnym PG16 przez `LEMAT_TEST_BAZA`); `dotnet format --verify-no-changes` czysto. Odstępstwa: fasady w `Lemat.Application.<Moduł>` (`IProfilFasada`, `IRekomendacjeFasada`); `theta`/ułamek/poziom zaokrąglane do 3 miejsc w domenie; poziom DOMYŚLNY = sigma(-0.8) = 0.310 wg wzoru (przykładowy kod w architekturze miał 0.5 — OQ-10); `OdpowiedzId`/`OcenaId`/`PrzypisanieId` jako `Guid?` do TASK-020/021/034 (w testach bazy zdjęte FK `wynik_zadania` -> `odpowiedz`/`ocena`); `Seria` składana z kolumn `seria_*` (nie `OwnsOne`); kalibracja trudności atomowym SQL z 10.1.3 + wpis `dziennik_audytu` w jednym CTE (wymaga otwartej transakcji); nowe kody błędów spoza 12.2 do zmapowania w TASK-071 (lista w raporcie developera: m.in. `POZIOM_OPANOWANIA_POZA_ZAKRESEM`, `WYNIK_CUDZEGO_UCZNIA`, `TEST_NIEPRAWIDLOWY_STAN`, `ELEMENT_NIEPRAWIDLOWY_STAN`, `SERIA_PELNA`, `UZASADNIENIE_WYMAGANE`); hosty nie wołają jeszcze `DodajModulProfil`/`DodajModulRekomendacje`; brak mocków.

- [ ] **TASK-020 [MVP] Moduł Rozwiązania: Rozwiązanie (maszyna stanów), Transkrypcja, Odpowiedź, zasady Poprawek**
  - **Opis:** `Lemat.Domain.Solutions`: `Rozwiazanie` (wszystkie przejścia 8.1, `Przeslanie`, `ZdjecieRozwiazania`, `WersjaZdjecia`), `Transkrypcja`, `KrokTranskrypcji`, `ZrodloTranskrypcji`, `ObszarKroku`, `Odpowiedz`, `WyborOdpowiedzi`,
    `PoprawkaTranskrypcji`, `ZakresPoprawek`, `ZasadyPoprawekTranskrypcji` (limity twarde ADR-036: zmiana istniejących kroków i wstawienie najwyżej 2; liczba wersji Poprawek; wszystko ponad limit → `POPRAWKI_ZBYT_ROZLEGLE`).
    `IRozwiazaniaFasada`, repozytoria; konfiguracje EF wg `ef-core.md` 6.3 (w tym `trg_kontekst_proby`).
  - **Zależności:** TASK-017, TASK-019.
  - **Realizuje:** 3.5 (wiersze 12, 13, 29, 38, 70, 93, 94, 107), 3.6 (szkielet `Rozwiazanie`), 8.1, 9.4, 12.5; ADR-017, ADR-024, ADR-032, ADR-036.
  - **DoD:** `PrzejsciaRozwiazaniaTests` — tabela przypadków dla każdego przejścia z diagramu 8.1 oraz operacji z tabeli „Dozwolone operacje” (zabronione → `ROZWIAZANIE_NIEPRAWIDLOWY_STAN`); limit przesłań 3 i ponowień 5;
    `ZasadyPoprawekTranskrypcji` — przypadki graniczne (0, 2, 3 wstawione kroki; zmiana kroku bez wstawień); `trg_kontekst_proby` (Odpowiedź/Rozwiązanie z kontekstem spoza dozwolonych kombinacji → kod); zapis z `version` odrzuca przestarzały stan.

- [ ] **TASK-021 [MVP] Moduł Ocenianie (1/2): Ocena, punkty i błędy (11.1), Pewność oceny (11.2), uzasadnienie (11.7)**
  - **Opis:** `Lemat.Domain.Grading`: `Ocena` (niezmienna, wersjonowanie `OSTATECZNA`/`WSTEPNA`/`ZASTAPIONA`), `OcenaKryterium`, `StatusOceny`, `ZrodloOceny`, `TypBledu`, `Wskazowka` (jednokrotny zapis), `PewnoscSurowa`, `PewnoscOceny`, `KodNiespojnosci` (N-1…N-6),
    czysta logika `ZasadyOceniania` dla 11.1 (wariant, punkty z Q-OC-02, zależność `wymaga_nr_kryterium`, FR-20, tabela błędów, `mozliwy_blad_odczytu` (a)–(d)), 11.2 (pewność surowa = min z użytych pytań, niespójności ograniczają do 0.50, kalibracja), 11.7 (uzasadnienie). Konfiguracje EF wg 6.4.
  - **Zależności:** TASK-018, TASK-020.
  - **Realizuje:** 3.5 (wiersze 15, 16, 40, 42–45, 87–89), 11.1, 11.2, 11.7, 8.2, Z-1, Z-5; ADR-005, ADR-006, ADR-022, ADR-027; FR-19 … FR-22, FR-27, FR-28.
  - **DoD:** `Lemat.Domain.Tests/Grading`: testy dla każdego wiersza tabeli 11.1 p. 6, FR-20 (a)(b)(c), każdej niespójności N-1…N-6, zależności kryteriów, `mozliwy_blad_odczytu` (a)–(d) (błąd odczytu nie zeruje punktów za wynik);
    test bazy `trg_ocena_niezmienna` (UPDATE punktów odrzucony, `wskazowka` NULL→wartość dozwolone raz); `Ocena` nie kompiluje się z publicznym setterem punktów (test refleksji).

- [ ] **TASK-022 [MVP] Moduł Ocenianie (2/2): Trasowanie T-1…T-6 do Weryfikacji, priorytety, komunikaty (11.3)**
  - **Opis:** `ZasadyOceniania.Ocen(RezultatPytan, DaneDoReguł, MigawkaProgu, MigawkaKalibratora, ILosowanieKontroli) → RezultatOceny` składa 11.1–11.3: reguły T-1 (dowód), T-2 (poprawny wynik, błędne kroki; τ_FR25), T-3 (podejrzenie błędu odczytu),
    T-4 (niska pewność: auto tylko gdy pewność > próg i brak niespójności), T-5 (Kontrola losowa: `fnv1a(rozwiazanie_id) mod 10000`), T-6 (termin twardy / wyczerpane ponowienia), T-7 **uproszczone (U-06)**: gdy zatwierdzona Transkrypcja pochodzi z Poprawek Ucznia
    i flaga `Poprawki:ZawszeDoWeryfikacji` — powód `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`; priorytety Weryfikacji; `WidocznoscOcenyDlaUcznia` (`PELNA`/`WSTRZYMANA_BLAD_ODCZYTU`); komunikaty dla Ucznia z tabeli 11.3; `TerminyOceny` (miękki/twardy).
  - **Zależności:** TASK-021.
  - **Realizuje:** 11.3, 14.2 (terminy), 8.2; ADR-008, ADR-022, ADR-025; Z-1, Z-3; FR-23 … FR-26, FR-28, FR-33; D-3, D-5.
  - **DoD:** test dla każdej reguły T-x i ich sumowania powodów; priorytet = minimum (1: T-6; 2: T-2/T-3/T-7; 3: T-1/T-4/T-5); granica progu (pewność = próg → Weryfikacja); T-5 deterministyczne na wektorach fnv1a; komunikaty dosłownie z tabeli 11.3;
    FsCheck: (i) rodzaj `OTWARTE_Z_DOWODEM` nigdy nie daje trasy automatycznej, (ii) niepusty zbiór niespójności nigdy nie daje trasy automatycznej, (iii) powód T-3 nigdy nie obniża `punkty` poniżej wyniku z 11.1 (FR-28).

- [ ] **TASK-023 [MVP] Moduł Weryfikacje: Weryfikacja, Blokada weryfikacji, Rozstrzygnięcie, reguła odczytu Kolejki**
  - **Opis:** `Lemat.Domain.Verifications`: `Weryfikacja` (maszyna stanów 8.3), `StatusWeryfikacji`, `PowodWeryfikacji` (8 wartości z DDL), `TypKolejki` (`KLASA`/`GLOBALNA`), `BlokadaWeryfikacji` (15 min, MVP bez przedłużania — U-15), `Rozstrzygniecie`, `PowodyPoczatkowe`,
    `IWeryfikacjeFasada`, `KolejkaWeryfikacji` (zapytanie z regułą odczytu 11.4.2: Nauczyciel widzi `KLASA` ze swoim `nauczyciel_docelowy_id` oraz `GLOBALNA`, gdy ma flagę `weryfikator_globalny`), repozytoria; konfiguracje EF wg 6.4.
  - **Zależności:** TASK-018, TASK-021.
  - **Realizuje:** 3.5 (wiersze 18, 46, 47, 74–76, 90, 96), 8.3, 11.4.1–11.4.2 (MVP), ADR-021, ADR-033, ADR-034, ADR-037; FR-33.
  - **DoD:** testy domeny: podjęcie po wygaśnięciu blokady przez innego Nauczyciela, podwójne podjęcie → `WERYFIKACJA_ZAJETA`, rozstrzygnięcie tworzy `ocena_nauczyciela_id`, zmiana wersji treści vs. wersji blokady (ADR-037);
    testy bazy: `trg_weryfikacja_wersja`, `trg_powody_poczatkowe`; zapytanie Kolejki zwraca właściwe wiersze dla trzech profili Nauczyciela (z Klasą, globalny, obcy); sortowanie `priorytet`, `termin_at`.

- [ ] **TASK-024 [MVP] Infrastruktura prac w tle: zlecanie, sklep (`SKIP LOCKED`), nasłuch `LISTEN/NOTIFY`**
  - **Opis:** `Lemat.Application.Common.BackgroundJobs`: `PracaWTle`, `TypPracy` (wszystkie wartości enumu DDL), `StatusPracy`, `PulaPracy`, `ZleceniePracy`, `KatalogPrac` (wpisy: pula, priorytet, maks. prób, odstępy, tryb deduplikacji — pełne dla MVP-owych typów
    `ROZPOZNAJ_PISMO`, `OCEN_ROZWIAZANIE`, `NAPISZ_WSKAZOWKE`, `AKTUALIZUJ_PROFIL`, pozostałe oznaczone jako POST-MVP), `IZlecaniePrac`, `IHandlerPracy`, `RezultatPracy`, `IKontekstPracy`, `ISklepPracWTle`, `IKontekstScenariuszaMock`.
    `Lemat.Infrastructure.BackgroundJobs`: `ZlecaniePracSql` (`INSERT … ON CONFLICT … RETURNING` na połączeniu transakcji jednostki pracy, kopiowanie `mock_scenariusz` do ładunku), `SklepPracWTle` (pobranie z `FOR UPDATE SKIP LOCKED`,
    dzierżawa 60 s, odnowienie, zapis wyniku warunkowo `wykonawca = @ja AND status = 'W_TOKU'`, ponowienia z odstępami, stan `MARTWA`, wywołania `odzyskaj_dzierzawy`/`ponow_prace`), `NasluchPowiadomien` (osobne połączenie `LISTEN praca_w_tle`, `lemat_zdarzenia`).
  - **Zależności:** TASK-015.
  - **Realizuje:** 13.3, 13.4, 15.1, `ddl.md` (B: `0003_notifications`, `0006_background_jobs`); ADR-002, ADR-053, ADR-056, ADR-066 p. 1–3; 18.5 (e).
  - **DoD:** testy `Lemat.Infrastructure.Tests/BackgroundJobs` (PostgreSQL 16): `PodwojneZlecenieTegoSamegoKluczaNieWycofujePrzypadkuUzycia`, `SprawdzPoprawkiDoUpdateLadunku`, `ListenNieDostajePowiadomieniaPrzedCommit`, `ListenNieDostajePowiadomieniaPoRollback`,
    `ZapisWynikuPoUtracieDzierzawyJestOdrzucany`, `JednostkaPracyBezTransakcjiOdrzucaZlecenie`, dla każdego triggera `trg_*_powiadom` powiadomienie po `COMMIT`; dwa równoległe konsumenty nie pobierają tej samej pracy; ponowienie z odstępem i przejście do `MARTWA` po wyczerpaniu prób.

- [ ] **TASK-025 [MVP] Dane referencyjne: 13 Działów i Umiejętności (migracja danych)**
  - **Opis:** `Persistence/Data/skills.yaml` (kod `P.V.9`/`R.XIII.2`, nazwa, `tylko_rozszerzony`, `waga_egzaminacyjna` — w MVP równe wagi znormalizowane w obrębie poziomu, U-19) z co najmniej 3 Umiejętnościami na Dział (≥ 45 łącznie; Dział XIII tylko rozszerzony),
    migracja EF `0002_ReferenceData` (osadzony SQL generowany deterministycznie z YAML; `INSERT … ON CONFLICT DO NOTHING`) wstawiająca Działy z 7.4.
  - **Zależności:** TASK-013, TASK-017.
  - **Realizuje:** 7.4, A-04, `ddl.md` (`dzial`, `umiejetnosc`); FR-36 (część), FR-04.
  - **DoD:** po `migrate` istnieje 13 Działów (kody I … XIII) i ≥ 45 Umiejętności z unikalnymi kodami; wagi spełniają CHECK; Dział XIII ma wyłącznie Umiejętności `tylko_rozszerzony`; migracja idempotentna; test odczytu przez `ITresciFasada`.

- [ ] **TASK-026 [MVP] `Lemat.Tools seed-dev`: konta demonstracyjne, konfiguracje modeli, próg, Klasa**
  - **Opis:** Polecenie `seed-dev` (odmawia przy `LEMAT_SRODOWISKO=Production` lub istniejącym koncie Administratora spoza seeda): konto Administratora, Nauczyciela-weryfikatora globalnego (`deklarowane_minuty_weryfikacji ≥ 90`, `umowa_poufnosci_at` ustawione), Nauczyciela zwykłego,
    dwóch Uczniów (z różnymi poziomami matury), Klasę z kodem zaproszenia i jednym członkiem; aktywne `konfiguracja_modelu` dla 8 ról (dostawca wg `Lemat:Modele:Dostawca`, domyślnie `mock`), kalibrator tożsamościowy i `prog_pewnosci` 0.95 (konfiguracje decyzyjne) dla `OTWARTE_OBLICZENIOWE` oraz 1.000 dla `OTWARTE_Z_DOWODEM`.
    Hasła demo wypisywane w konsoli, nie w repo; polecenie idempotentne.
  - **Zależności:** TASK-016, TASK-018, TASK-025.
  - **Realizuje:** ADR-065 p. 6, 10.0.6, 22 (progi początkowe), D-9, D-14; ADR-071 p. 4.
  - **DoD:** po `migrate` + `seed-dev` na pustej bazie istnieją wymienione rekordy; drugie uruchomienie nie duplikuje; odmowa w `Production` (test); `dostawca=mock` dla wszystkich ról; test: aktywacja konfiguracji bez progu jest niemożliwa (kod `BRAK_PROGU_DLA_KONFIGURACJI` po stronie domeny).

- [ ] **TASK-027 [MVP] Seed: Zadania zamknięte (w tym pula diagnostyczna) i Wkładki teoretyczne**
  - **Opis:** `Persistence/Data/exercises-closed.yaml` + loader w `seed-dev`/`migrate-data`: ≥ 26 Zadań `ZAMKNIETE` `GLOBALNY` `OPUBLIKOWANE` (po jednym `diagnostyczne=true` na każdy Dział poziomu `PODSTAWOWY` (12) i `ROZSZERZONY` (13), nominalny poziom trudności 2 lub 3) oraz ≥ 10 dodatkowych
    (różne Umiejętności i poziomy 1–5); każde z `tresc_md` (Markdown + LaTeX), `opcje_zamkniete` (wybór jednokrotny lub prawda/fałsz), minimalnym `rozwiazanie_wzorcowe`, tagami Dział/Umiejętność. ≥ 1 `WkladkaTeoretyczna` `OPUBLIKOWANA` na każdą Umiejętność z `skills.yaml`
    (krótka treść Markdown + LaTeX, `szacowany_czas_min` 1–15). **Zadania są autorskie, „w stylu CKE” (U-19) — wymagają przeglądu merytorycznego przez nauczyciela matematyki (HANDOFF „Wymagane akcje od człowieka”).**
  - **Zależności:** TASK-017, TASK-025, TASK-026.
  - **Realizuje:** FR-12, FR-36, FR-06, FR-03 (pula), 7.5.1, 9.1; A-04.
  - **DoD:** loader idempotentny i przechodzi wszystkie CHECK/triggery DDL; test: dla każdego Działu poziomu istnieje ≥ 1 Zadanie diagnostyczne (raport pokrycia 12/13); każdy wiersz `opcje_zamkniete` ma dokładnie jedną poprawną opcję (wybór) lub poprawny klucz; każda Umiejętność ma Wkładkę;
    test .NET sprawdza zbalansowanie ograniczników `$…$` i nawiasów klamrowych w LaTeX każdej treści (pełne renderowanie KaTeX sprawdzają testy frontendu i E2E).

- [ ] **TASK-028 [MVP] Seed: Zadania otwarte obliczeniowe i z dowodem z Kryteriami oceniania**
  - **Opis:** `Persistence/Data/exercises-open.yaml` + loader: ≥ 24 Zadania `OTWARTE_OBLICZENIOWE` (obejmujące wszystkie Działy obu poziomów, poziomy trudności 1–5, `punkty_max` 1–6) z `rozwiazanie_wzorcowe` (warianty i kroki), `wynik_koncowy_wzorcowy` (typ + `latex` + `sympy`),
    `kryterium_oceniania` (warianty 1–2, `nr_kryterium`, `punkty_max`, `czy_wynik_koncowy`, `wymaga_nr_kryterium`) oraz ≥ 4 Zadania `OTWARTE_Z_DOWODEM` (wynik końcowy NULL, kryteria). Łącznie z TASK-027 ≥ 60 Zadań (cel: „kilkadziesiąt”). Dane autorskie (U-19).
  - **Zależności:** TASK-027.
  - **Realizuje:** FR-12, FR-19 (kryteria), FR-36, 7.5.2–7.5.3, A-05, 11.1.
  - **DoD:** loader przechodzi `trg_suma_kryteriow` i CHECK-i (suma punktów kryteriów każdego wariantu = `punkty_max`); test spójności: `czy_wynik_koncowy` występuje w każdym wariancie Zadania obliczeniowego; każdy wynik wzorcowy ma niepuste pole `sympy`; zbiór obejmuje ≥ 10 z 13 Działów dla otwartych.

- [ ] **TASK-029 [POST-MVP] Klasy: Zestaw zadań i Przypisanie (domena + EF)**
  - **Opis:** `ZestawZadan`, `PozycjaZestawu`, `Przypisanie`, `PrzypisanieUczen` (wycofanie, zmiana terminu — ADR-074), fasada, EF.  **Zależności:** TASK-018, TASK-019.  **Realizuje:** 3.5 (34, 35), ADR-074; FR-32.
  - **DoD:** testy zmiany terminu (≥ now()+1 h), wycofania, widoczności pozycji; `przypisanie_uczen` zgodne z członkostwem.

- [ ] **TASK-030 [POST-MVP] Treści zaawansowane: Zlecenie generowania, Walidacja, Przegląd, Zgłoszenie błędu w Zadaniu, Wstrzymanie w doborze, Kalibracja trudności**
  - **Zależności:** TASK-017.  **Realizuje:** 3.5 (62, 63, 83, 97, 98, 23), 8.4, ADR-015, ADR-028, ADR-029, ADR-038; FR-11, FR-13, FR-14.
  - **DoD:** maszyna stanów Zadania 8.4 (wszystkie przejścia), progi 3 zgłoszeń → wstrzymanie, atomowa aktualizacja `kalibracja_trudnosci` (test współbieżności).

- [ ] **TASK-031 [POST-MVP] Arkusz CKE i Import arkusza (domena + EF)**
  - **Zależności:** TASK-017.  **Realizuje:** 3.5 (25), 9.11, 12.9; FR-35.  **DoD:** model `ArkuszImportV1`, walidacja pakietu, statusy importu; testy na przykładowym ZIP.

- [ ] **TASK-032 [POST-MVP] Kalibracja zaawansowana: Próbka kalibracyjna, IPW, wybór Progu (11.5), Bezpiecznik (ADR-046)**
  - **Zależności:** TASK-018, TASK-023.  **Realizuje:** 3.5 (48, 50, 51, 104, 109, 110), 11.5; ADR-006, ADR-007, ADR-046; NFR-02 … NFR-04.
  - **DoD:** testy (a)–(m) z 11.5.3 w `Lemat.Domain.Tests/Calibration/SafeguardTests.cs` (18.5 C5-07), `CalibrationSampleSourceTests`; wyznaczanie progu z granicą Wilsona i `n_eff`.

- [ ] **TASK-033 [POST-MVP] Konta: RODO (Zdarzenie RODO, Okres karencji, usunięcie i przywrócenie — domena)**
  - **Zależności:** TASK-016.  **Realizuje:** 3.5 (100, 101, 105), 16.4; ADR-040, ADR-041, ADR-044; NFR-09.  **DoD:** przejścia statusów kont, tabele przywrócenia 16.4 (U/N/A), testy karencji 30 dni.

- [ ] **TASK-034 [POST-MVP] Rozwiązania: Szkic transkrypcji, Kopia treningowa, Zakres poprawek miękki, Zgłoszenie błędu odczytu (domena)**
  - **Zależności:** TASK-020.  **Realizuje:** 3.5 (72, 80, 95), ADR-035, ADR-036, ADR-073; FR-18, FR-28.  **DoD:** `numer_zapisu bigint`, testy `SzkicZ…` (18.5 o), limity miękkie.

- [ ] **TASK-035 [POST-MVP] Weryfikacje: eskalacja, SLA w dniach roboczych, Kontrola przyjęć, Pseudonim Ucznia, Szkic rozstrzygnięcia**
  - **Zależności:** TASK-023.  **Realizuje:** 3.5 (73, 75–78, 92, 99), 11.4.3, 17.7; ADR-031, ADR-033, ADR-034, ADR-043.  **DoD:** `KalendarzDniRoboczych` + `dzien_wolny`, testy eskalacji i kontroli przyjęć, format pseudonimu `U-XXXXXX`.

- [ ] **TASK-036 [POST-MVP] Powiadomienia i Audyt (domena + EF)**
  - **Zależności:** TASK-016.  **Realizuje:** 3.5, 6.2; FR-24, FR-33.  **DoD:** `powiadomienie` z UUIDv7 z aplikacji, `dziennik_audytu` tylko do dopisywania (uprawnienia 0005).

- [ ] **TASK-037 [POST-MVP] Pełne testy zgodności schematu z modelem EF**
  - **Zależności:** TASK-013 oraz wszystkie TASK domenowe.  **Realizuje:** `ef-core.md` 8; ADR-063; 18.5 (i).
  - **DoD:** `ModelEfZgodnyZKatalogiem` (a–f), `KazdyKluczObcyMaRelacjeAlboJestOdroczony`, `KazdyTriggerWKatalogu`, `OgraniczeniaZgodneZMigawka` (`constraints.txt`), `MigracjeNaPustejBazieBezRolIBezMock`.

---

# Faza 3: Agenci AI w .NET i ich narzędzia (z implementacjami Mock jako domyślnymi)

- [ ] **TASK-038 [MVP] Porty modeli i kontrakty wspólne warstwy AI**
  - **Opis:** `Lemat.Application.Models`: `IModelDecyzyjny` (`WykonajPlanAsync`), `IModelGeneratywny` (`DlaRoliAsync → KlientCzatuRoli`), `IWywolanieUstrukturyzowane`, `ISzablonyPromptow`, `ISprawdzanieSymboliczne` (+ `Rownowaznosc`),
    `IMagazynObiektow`, `IObrobkaObrazow`, `IPredyktorSukcesu`, `IOgranicznikSzybkosci` (tylko interfejs; implementacja POST-MVP), wyjątki (`ModelNiedostepnyException`, `BladModeluPrzejsciowyException`, `BladModeluTrwalyException`, `MockScenariuszNieznanyException`), `PamiecKonfiguracjiModeli`
    (odświeżana `NOTIFY konfiguracja_modelu` i co 30 s). `Lemat.Application.Agents.<Area>`: interfejsy `IAgentRekomendujacy`, `IAgentRozpoznajacy`, `IAgentOceniajacy`, `IAgentWyjasniajacy` i rekordy wejść/wyjść (rekordy C#, bez EF/ASP.NET), `IDaneDoboru`, `IRekomendacjaSynchroniczna`.
  - **Zależności:** TASK-014, TASK-018.
  - **Realizuje:** 10.0.2, 13.1, 10.1.1, 10.3.1, 10.4.1, 10.5.1, 6.6; ADR-003, ADR-055, ADR-058; NFR-06.
  - **DoD:** kontrakty kompilują się w `Lemat.Application` bez referencji do SDK dostawców; test architektury: `Lemat.Application` nie referuje `Lemat.Models`; `PamiecKonfiguracjiModeli` — test jednostkowy odświeżania po powiadomieniu; dokumentacja XML (`///`) na każdym porcie opisuje kontrakt błędów (które wyjątki są przejściowe, które trwałe).

- [ ] **TASK-039 [MVP] Mock: `MockChatClient`, `MockModelDecyzyjny`, scenariusze i dane**
  - **Opis:** `Lemat.Models.Mock`: `MockChatClient : IChatClient` (scenariusz z `IKontekstScenariuszaMock`, odpowiedzi z `Mock/Scenarios/<scenario>/<rola>.json`, generator wartości domyślnych ze schematu z atrybutem `[PrzykladMock]`), `MockModelDecyzyjny`
    (masa 0.995 na opcji zgodnej z Rozwiązaniem wzorcowym — `KatalogPytan.OpcjaWzorcowa`; rozkład per `kod_pytania` ze scenariusza), tryb ścisły `Lemat:Mock:TrybScisly`, `LEMAT_MOCK_OPOZNIENIE_MS`. Scenariusze MVP: domyślny (`auto-poprawne`), `auto-blad-rachunkowy`,
    `weryfikacja-niska-pewnosc`, `weryfikacja-poprawny-wynik-bledne-kroki`, `weryfikacja-dowod`, `rozpoznanie-nieczytelne`. `PrzesunietyTimeProvider` dla `LEMAT_CZAS_TESTOWY`.
  - **Zależności:** TASK-038.
  - **Realizuje:** 10.0.6, ADR-065, `docs/spec/prompts.md` 6; NFR-06.
  - **DoD:** testy: odpowiedź deterministyczna dla scenariusza; brak scenariusza → odpowiedź domyślna prowadząca trasą automatyczną (pewność 0.995 ≥ próg 0.95); nieznany scenariusz w trybie ścisłym → `MockScenariuszNieznanyException`;
    wartości wygenerowane ze schematu spełniają `maxLength`/`pattern`; testy nie używają sieci.

- [ ] **TASK-040 [MVP] Adaptery lokalne: magazyn plików, obróbka obrazów, proste sprawdzanie symboliczne**
  - **Opis:** `Lemat.Infrastructure.Storage.MagazynObiektowLokalny` (`IMagazynObiektow` na systemie plików: `Magazyn:Sciezka`; ochrona przed path traversal; `usun_po` zapisywane w bazie, nie w magazynie; adres podpisany HMAC z wygaśnięciem 10 min ważny dla `GET /magazyn-lokalny/{token}` — endpoint w TASK-077, U-03),
    `Lemat.Infrastructure.Sandboxes.ProstaObrobkaObrazow` (`IObrobkaObrazow`: walidacja typu po magic bytes JPEG/PNG/WEBP, limit 10 MB, odrzucenie HEIC z kodem `NIEOBSLUGIWANY_FORMAT`; **bez** usuwania EXIF/normalizacji — U-02; `MockObrobkaObrazow` do testów),
    `ProsteSprawdzanieSymboliczne` (`ISprawdzanieSymboliczne`: normalizacja LaTeX — spacje, `\cdot`→`*`, `\frac{a}{b}`→`(a)/(b)`, nawiasy, kolejność składników sumy dla prostych wielomianów; równe → `ROWNOWAZNE`, w przeciwnym razie `NIEROZSTRZYGNIETE`, **nigdy** fałszywe `NIEROWNOWAZNE`; U-05) i `FalszyweSprawdzanieSymboliczne` (testy).
  - **Zależności:** TASK-038, TASK-004.
  - **Realizuje:** 13.1, 10.0.6, ADR-045/052 (zastępowane przez U-02), ADR-050 (zastępowane przez U-05); FR-21 (częściowo), NFR-09 (częściowo).
  - **DoD:** testy magazynu (zapis/odczyt/usunięcie, traversal `../` odrzucony, wygasły token odrzucony); obróbka: prawdziwe pliki JPEG/PNG/WEBP przechodzą, plik tekstowy z rozszerzeniem `.jpg` odrzucony, > 10 MB odrzucony;
    symbolika: tablica ≥ 25 par (równoważne zapisy, zapisy różne) bez ani jednego fałszywego `NIEROWNOWAZNE`; wszystkie implementacje wybierane przez `LematOpcje` (test DI).

- [ ] **TASK-041 [MVP] Szablony promptów, `WywolanieUstrukturyzowane`, `BudowniczyWiadomosci`**
  - **Opis:** `Lemat.Agents.Common`: `SzablonyPromptow` (YAML Semantic Kernel `template_format: semantic-kernel`, `Prompts/index.yaml` para rola + wersja → plik, zasoby osadzone), `BudowniczyWiadomosci` (treść od użytkownika wyłącznie w wiadomości `user`),
    `WywolanieCzatu`; `Lemat.Models.WywolanieUstrukturyzowane` (`ChatResponseFormat.ForJsonSchema`, walidacja `JsonSchema.Net`, 1 próba naprawy). Pliki promptów dla ról MVP z `docs/spec/prompts.md`: `recognition/recognition-v1.yaml`, `generative-hints/hint-v1.yaml`,
    `decision-fallback/plan-pytan-v1.yaml` (dla `ZapasowyLlmAdapter`), schematy `Lemat.Models/Schemas/*.json` generowane z rekordów (`AIJsonUtilities.CreateJsonSchema`).
  - **Zależności:** TASK-038.
  - **Realizuje:** 6.6, 10.0.3, `docs/spec/prompts.md` 1–5; ADR-055, ADR-058 p. 4; 18.5 (h).
  - **DoD:** testy `KazdaWersjaPromptuIstnieje`, `SzablonBezZmiennychTresci` (zmienne tylko systemowe); test dryfu schematu (zapisane `Schemas/*.json` = wygenerowane); `WywolanieUstrukturyzowane`: odpowiedź poprawna, niepoprawna → naprawa → poprawna, niepoprawna dwa razy → `BladModeluTrwalyException`;
    prompty ładują się bez zależności od sieci.

- [ ] **TASK-042 [MVP] `FabrykaKlientowCzatu`: dostawcy `mock`, `openai-zgodny`, `azure-openai`, `anthropic` wybierani konfiguracją**
  - **Opis:** `Lemat.Models.Llm.FabrykaKlientowCzatu : IModelGeneratywny` — potok `ChatClientBuilder` (OpenTelemetry, limit czasu, klasyfikacja błędów → wyjątki przejściowe/trwałe, `UseFunctionInvocation`), `KlientCzatuRoli`; adaptery dostawców wyłącznie w `Lemat.Models`
    (`Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, dla `anthropic` — oficjalny SDK Anthropic z adapterem `IChatClient`, lub punkt końcowy zgodny z OpenAI; **nowy klucz dostawcy spoza ADR-058 — U-13, otwarte pytanie OQ-01**); klucze i endpointy z `LEMAT_MODELE_*` (nigdy z bazy/repo);
    `RejestrAdapterow`; strażnik startu (retencja A-07, `mock` w `Production`); rola `ROZPOZNAWANIE_PISMA` z wejściem obrazowym `DataContent` (`image/jpeg`) i wymuszonym schematem JSON.
  - **Zależności:** TASK-039, TASK-041, TASK-004.
  - **Realizuje:** 6.6 (tabela adapterów), 13.1, ADR-055, ADR-058, ADR-004; NFR-06.
  - **DoD:** test kontraktu `KontraktWywolaniaOpenAiTests` na WireMock.Net (żądanie zawiera schemat JSON, obraz i nagłówki; odpowiedzi 200/429/500/timeout → właściwe wyjątki); analogiczny test Azure; test `anthropic` na WireMock lub oznaczony `Skip` z powodem, jeśli SDK niedostępny (zapis w raporcie);
    domyślnie (bez zmiennych) fabryka zwraca klienta `mock`; `mock` w `Production` → wyjątek startu; test architektury `SdkDostawcowTylkoWModelach` zielony; żaden test nie wymaga klucza API.

- [ ] **TASK-043 [MVP] Agent rozpoznający: zdjęcia → Transkrypcja (kroki, LaTeX, ramki, pewność odczytu)**
  - **Opis:** `Lemat.Agents.Recognition.AgentRozpoznajacy : IAgentRozpoznajacy` — pobranie zdjęć z `IMagazynObiektow`, jedno wywołanie roli `ROZPOZNAWANIE_PISMA` (wszystkie zdjęcia w jednej wiadomości `user`), walidacja kontraktu (kolejne `nr`, `NrZdjecia`, ramki w granicach, pusta ramka → całe zdjęcie
    i `PewnoscOdczytu := min(p, 0.79)`), post-processing (maks. 40 kroków → `ZBYT_WIELE_KROKOW`, `czytelne=false` → `NIECZYTELNE`), błędy: timeout → `BladModeluPrzejsciowyException`, odmowa treści → `BladModeluTrwalyException`; bez Filtra danych osobowych i maskowania (U-07; filtr stosowałby handler po powrocie agenta — POST-MVP, TASK-058).
  - **Zależności:** TASK-038, TASK-039, TASK-040, TASK-041.
  - **Realizuje:** 10.3.1, 10.3.2, 10.3.4, ADR-004; FR-17.
  - **DoD:** `Lemat.Agents.Tests/Recognition` z `MockChatClient`: scenariusz domyślny (3 kroki), `rozpoznanie-nieczytelne`, > 40 kroków, ramka poza zdjęciem (przycięta), brak ramki; zgodność bajtowa treści matematycznej (LaTeX nie jest modyfikowany po drodze); test, że agent nie ma zależności od `LematDbContext`
    (`AgenciIPortyAgentowNieZalezaOdLematDbContext`); span OpenTelemetry `invoke_agent agent_rozpoznajacy`.

- [ ] **TASK-044 [MVP] Agent oceniający (1/2): stan dla Modelu decyzyjnego, katalog pytań, fabryka planu pytań**
  - **Opis:** `Lemat.Agents.Grading.BudowniczyStanuOceny` (stan 10.4.2: treść, Kryteria, Rozwiązanie wzorcowe, kroki Transkrypcji, sekcja `[SPRAWDZENIE_SYMBOLICZNE]`), `KatalogPytan` (Q-OC-00 … Q-OC-06, opcje wzorcowe),
    `FabrykaPlanuOceny` (plan z zależnościami, ≤ 12 par Q-OC-05, reszta → N-6, ADR-026), wywołania `ISprawdzanieSymboliczne` równolegle (semafor, budżet 3 s na Rozwiązanie, brak rezultatu → `NIEROZSTRZYGNIETE`).
  - **Zależności:** TASK-038, TASK-021, TASK-017, TASK-040.
  - **Realizuje:** 10.4.2, 10.4.3, 10.4.4, 10.4.6 (część), `docs/spec/prompts.md` 3.1; ADR-026; FR-19 … FR-21.
  - **DoD:** `Lemat.Agents.Tests/Grading`: test dla każdego wiersza tabeli 10.4.3 (które pytania powstają dla danego kształtu Zadania i Transkrypcji), dla każdej niespójności N-1 … N-6 (dane `Data/grading/*.json`); plan nie przekracza 12 par Q-OC-05; stan nie zawiera danych osobowych; deterministyczny.

- [ ] **TASK-045 [MVP] Agent oceniający (2/2): wykonanie planu, `RezultatOceny`, zapis decyzji**
  - **Opis:** `AgentOceniajacy : IAgentOceniajacy.OcenAsync` — `IModelDecyzyjny.WykonajPlanAsync` z terminem (miękki/twardy), przekazanie `RezultatPytan` do `ZasadyOceniania.Ocen` (Domain) — agent **nie zawiera reguł** (Z-1), zwraca `RezultatOceny`
    z listą `DecyzjaSkladowa` do zapisu w `decyzja_modelu` (zapis wykonuje handler, TASK-049); wyjątki przejściowe/trwałe; brak Opinii pomocniczej (U-14).
  - **Zależności:** TASK-044, TASK-022, TASK-039.
  - **Realizuje:** 10.4.1, 10.4.6, 11.x, Z-1, Z-4; FR-19, FR-22 … FR-26.
  - **DoD:** testy z `MockModelDecyzyjny`: scenariusz domyślny → trasa automatyczna (`OSTATECZNA`); `weryfikacja-niska-pewnosc` → T-4; `weryfikacja-poprawny-wynik-bledne-kroki` → T-2; `weryfikacja-dowod` → T-1; błąd modelu → `BladModeluPrzejsciowyException`;
    test, że agent nie ma żadnej logiki punktów (punkty = wynik `ZasadyOceniania`); każda decyzja składowa ma rozkład sumujący się do 1 (±1e-6).

- [ ] **TASK-046 [MVP] Agent wyjaśniający: Wskazówka z strażnikiem wycieku i zapasem szablonowym**
  - **Opis:** `Lemat.Agents.Explanation.AgentWyjasniajacy` (rola `GENERATYWNY_WSKAZOWKI`, prompt `wskazowka-v1`, ≤ 400 znaków, termin 8 s), `StraznikWycieku` (ekstrakcja `$…$` i liczb, porównanie z wynikiem wzorcowym i dwoma ostatnimi krokami przez `ISprawdzanieSymboliczne`),
    jedno ponowienie z dopiskiem, następnie `SzablonyWskazowek` (`ZrodloWskazowki.Szablon`); agent nigdy nie rzuca wyjątku na zewnątrz (zawsze zwraca Wskazówkę).
  - **Zależności:** TASK-041, TASK-039, TASK-040.
  - **Realizuje:** 10.5.1 … 10.5.4; FR-27; NFR-05.
  - **DoD:** `StraznikWyciekuTests` dla wyniku liczbowego, zbioru, przedziału i ostatniego kroku wzorca; scenariusz „wynik to 5” → ponowienie → szablon; błąd modelu i timeout → szablon; Wskazówka > 400 znaków odrzucona; Wskazówka dla `NIEPELNE_ROZWIAZANIE` cytuje opis kryterium (10.5.3).

- [ ] **TASK-047 [MVP] Agent rekomendujący: `POLITYKA_ELO`, predyktor Elo, uzasadnienia z szablonów, tryb synchroniczny**
  - **Opis:** `Lemat.Agents.Recommendations.AgentRekomendujacy` z krokami `IKrokPolityki` (`KrokPrzypisania` — pomijany w MVP, `KrokSerii`, `KrokPowtorki`, `KrokWyboruUmiejetnosci`, `KrokWkladki`, `KrokZadania`, `KrokBrakuZadan`), `PredyktorSukcesuElo : IPredyktorSukcesu`,
    `GeneratorUzasadnien` (`justifications.pl-PL.yaml`, odmiana liczebników), `Lemat.Application.Orchestration.Synchronous.RekomendacjaSynchroniczna : IRekomendacjaSynchroniczna` (zależności: `IAgentRekomendujacy`, fasady odczytu Profil/Tresci/Klasy/Konta; brak zależności od `IRekomendacjeFasada`).
    Bez wywołań Q-RE-01 i bez `JEV_CHOICE` w MVP (strategia eksperymentalna POST-MVP, TASK-069). Rosnąca trudność (FR-07), powtórki (FR-08), wybór Działu (FR-09), uzasadnienie (FR-10).
  - **Zależności:** TASK-019, TASK-038, TASK-041.
  - **Realizuje:** 10.1.1 … 10.1.5, 10.1.8, 6.3 (tryb synchroniczny), 9.2; ADR-010, ADR-011, ADR-023; FR-05 … FR-10, NFR-05.
  - **DoD:** `Lemat.Agents.Tests/Recommendations`: test pokrycia — każda wartość `RegulaRekomendacji` i `BrakElementu` ma szablon uzasadnienia; Uczeń z `theta` niską dostaje Wkładkę przed Zadaniem (FR-06), po serii poprawnych odpowiedzi trudność rośnie (FR-07), Umiejętność opanowana wraca jako Powtórka po terminie (FR-08), `wybrany_dzial_id` zawęża dobór (FR-09);
    uzasadnienie zawiera liczby z danych („3 z ostatnich 4 zadań…”, FR-10); decyzja deterministyczna dla stałego `TimeProvider`; p95 < 150 ms w teście wydajnościowym na danych seed.

- [ ] **TASK-048 [MVP] `Lemat.Worker`: pule prac w tle (R/S/O/P/W), pętle, dzierżawa, rejestr handlerów**
  - **Opis:** `PulaPracHostedService` (`BackgroundService` na pulę; liczby pętli z sekcji 22 możliwe do zmniejszenia w Development), pobieranie przez `SklepPracWTle`, odnawianie dzierżawy co 15 s (`PeriodicTimer`), anulowanie `CancellationToken` przy utracie dzierżawy,
    rejestr `IHandlerPracy` po `TypPracy`, ponowienia/odstępy, `PoWyczerpaniuAsync`, ustawianie `IKontekstScenariuszaMock` z ładunku, `LISTEN` budzi pętlę (`NasluchPowiadomien`), `/zdrowie`; zamknięcie z oczekiwaniem na pracę w toku.
  - **Zależności:** TASK-024, TASK-003.
  - **Realizuje:** 13.4, 6.3, ADR-053, ADR-056, ADR-066; NFR-01.
  - **DoD:** test integracyjny z pracą testową: zlecona praca zostaje wykonana w < 1 s od `COMMIT` (LISTEN), nie dwa razy przy dwóch instancjach hosta, ponowienie po błędzie przejściowym wg odstępów, `MARTWA` po wyczerpaniu prób; utrata dzierżawy anuluje handler;
    handler nieznanego typu → `MARTWA` z kodem; graceful shutdown.

- [ ] **TASK-049 [MVP] Handlery prac `ROZPOZNAJ_PISMO` i `OCEN_ROZWIAZANIE` (z trasowaniem i Weryfikacją)**
  - **Opis:** `Lemat.Application.Orchestration.Handlers.RozpoznajPismoHandler` (warunki wykonania 13.3: konto `AKTYWNE`, status, brak Transkrypcji dla `przeslanie_nr`; jednostka pracy 1: `PRZESLANE → ROZPOZNAWANIE`; wywołanie agenta poza transakcją; jednostka pracy 2: zapis Transkrypcji
    `ZrodloTranskrypcji.Agent` i `TRANSKRYPCJA_GOTOWA` albo `BLAD_ROZPOZNAWANIA` z kodem; idempotentny) oraz `OcenRozwiazanieHandler` (`ZATWIERDZONE → OCENIANIE`, wywołanie `IAgentOceniajacy`, zapis `Ocena` (`OSTATECZNA` lub `WSTEPNA`), `OcenaKryterium`, `decyzja_modelu`, utworzenie `Weryfikacja`
    z powodami i priorytetem (typ kolejki `KLASA` dla Ucznia z Klasą, inaczej `GLOBALNA`), `Rozwiazanie.ZarejestrujPierwszaOcene`, zlecenie `NAPISZ_WSKAZOWKE` i `AKTUALIZUJ_PROFIL` w jednej transakcji; T-6 po terminie twardym lub wyczerpaniu prób — Weryfikacja `BLAD_PRZETWARZANIA` bez Oceny, U-14).
  - **Zależności:** TASK-048, TASK-043, TASK-045, TASK-020, TASK-021, TASK-022, TASK-023.
  - **Realizuje:** 9.4, 9.5, 8.1, 8.2, 13.3, 14.2, 15.1; Z-3, Z-4; FR-17, FR-19, FR-23 … FR-26, NFR-01, NFR-05.
  - **DoD:** testy integracyjne (PostgreSQL + Mock): Rozwiązanie → `TRANSKRYPCJA_GOTOWA` (scenariusz domyślny) i `BLAD_ROZPOZNAWANIA` (`rozpoznanie-nieczytelne`); po zatwierdzeniu: scenariusz domyślny → `OCENIONE` + Ocena `OSTATECZNA`; `weryfikacja-niska-pewnosc` → `OCZEKUJE_NA_WERYFIKACJE`, Ocena `WSTEPNA`, Weryfikacja z powodem `NISKA_PEWNOSC`;
    `weryfikacja-dowod` → powód `ZADANIE_Z_DOWODEM`; konto `USUWANE` → praca kończy się `KONTO_USUWANE` bez efektu; powtórne wykonanie tej samej pracy nie tworzy drugiej Oceny; zapis wyniku po zmianie `version` odrzucony (`rezultat_przestarzaly`); `czas_systemowy_ms` zapisany.

- [ ] **TASK-050 [MVP] Handlery prac `NAPISZ_WSKAZOWKE` i `AKTUALIZUJ_PROFIL`**
  - **Opis:** `NapiszWskazowkeHandler` (warunek `ocena.wskazowka IS NULL`, `typ_bledu IS NOT NULL`, Ocena ≠ `ZASTAPIONA`; zapis jednokrotny `wskazowka` + `zrodlo_wskazowki`; nie dla trasy z `WSTRZYMANA_BLAD_ODCZYTU` — brak wskazówki o odczycie) i `AktualizujProfilHandler`
    (jedna transakcja: `SELECT … FROM profil_ucznia FOR UPDATE`, pętla pobierania `wynik_zadania` jeszcze nieuwzględnionych, aktualizacja `theta` Elo + propagacja, `wynik_zadania` z Oceny wstępnej waga 0.5; deduplikacja `uczen_id` wśród `OCZEKUJE`).
  - **Zależności:** TASK-048, TASK-046, TASK-019, TASK-049.
  - **Realizuje:** 13.3, 10.1.3, 10.5, 8.6; ADR-010, ADR-018, ADR-025; FR-27, FR-29.
  - **DoD:** test: po Ocenie `OSTATECZNA` powstaje `wynik_zadania`, Profil zmienia `theta` zgodnie z modelem Elo (wartość oczekiwana z testu domeny), druga równoległa `AKTUALIZUJ_PROFIL` czeka na blokadę i nie gubi aktualizacji;
    Wskazówka powstaje asynchronicznie i jest zapisana raz; błąd Agenta wyjaśniającego → Wskazówka szablonowa (`ZrodloWskazowki.Szablon`); Ocena wstępna daje Wynik zadania z wagą 0.5, rozstrzygnięcie Nauczyciela nadpisuje go zgodnie z 8.6.

- [ ] **TASK-051 [MVP] `ZapasowyLlmAdapter`: LLM jako Model decyzyjny (jedyny realny model decyzyjny w MVP)**
  - **Opis:** `Lemat.Models.Fallback.ZapasowyLlmAdapter : IModelDecyzyjny` — cały `PlanPytan` jednym wywołaniem klienta czatu roli `DECYZYJNY_ZAPASOWY` ze schematem JSON (prompt `plan-pytan-v1`), 1 próba naprawy schematu, timeout 9 s, zwrot `RezultatPytan` z rozkładami prawdopodobieństw
    (odpowiedź LLM przeliczana na rozkład wg 13.2; pewności tylko w kierunku bezpiecznym — Z-1: dla modelu zapasowego obowiązuje próg 0.98). Włączany przez konfigurację modelu (`dostawca = zapasowy-llm` z `dostawca_llm`) przy ustawionym `LEMAT_MODELE_DOSTAWCA` ≠ `mock`. Jev i `CircuitBreakerModeluDecyzyjnego` — POST-MVP (TASK-052, 053).
  - **Zależności:** TASK-042, TASK-044, TASK-045.
  - **Realizuje:** 13.2 (model zapasowy), ADR-009, ADR-003; NFR-06, NFR-07 (częściowo).
  - **DoD:** test WireMock.Net: poprawna odpowiedź JSON → `RezultatPytan` z rozkładem sumującym się do 1; odpowiedź niezgodna ze schematem → naprawa → ok / `BladModeluTrwalyException`; timeout 9 s → `BladModeluPrzejsciowyException`; test, że masa przypisana opcji jest ograniczona zgodnie z regułą 13.2;
    z `mock` w konfiguracji zachowuje się jak `MockModelDecyzyjny`; test DI: wybór adaptera wg `konfiguracja_modelu.dostawca`.

- [ ] **TASK-052 [POST-MVP] `JevAdapter` (typowany `HttpClient`, tryb wsadowy i pojedynczy)**
  - **Zależności:** TASK-045.  **Realizuje:** 13.2, A-01, A-02, ADR-003, ADR-039; Z-1.  **DoD:** testy kontraktu WireMock.Net (wsadowy ≤ 32 pytań, pojedynczy ≤ 16 równolegle, 3 fale), klucz `LEMAT_JEV_KLUCZ_API`, timeouty z 22.

- [ ] **TASK-053 [POST-MVP] `CircuitBreakerModeluDecyzyjnego`, `RejestrAdapterow`, przełączanie na model zapasowy**
  - **Zależności:** TASK-051, TASK-052.  **Realizuje:** 13.2, 9.9, ADR-009; NFR-07.  **DoD:** testy obwodu (3 kolejne błędy lub ≥ 50% w 20 s przy ≥ 6; półotwarty po 60 s; `429` nie liczy się), jedna Ocena = jeden model.

- [ ] **TASK-054 [POST-MVP] `OgranicznikSzybkosci` (kubełki w PostgreSQL) i testy (a)–(h)**
  - **Zależności:** TASK-024.  **Realizuje:** 13.2, ADR-039; 18.5 C5-07.  **DoD:** `RateLimiterTests` (a)–(e), `RateLimiterSimulation` (f)–(h, FsCheck), asercja `pojemnosc = 0` po `INSERT` w (g).

- [ ] **TASK-055 [POST-MVP] Sidecar SymPy (Python) i `SympySidecarKlient`**
  - **Zależności:** TASK-040.  **Realizuje:** ADR-050, 13.1, 17.2; FR-13, FR-21.  **DoD:** `/v1/postac-liniowa`, `/v1/rownowaznosc-krokow`, `/v1/rownowaznosc-wyniku` z limitami CPU/RAM, wynik trójwartościowy, testy `pytest` + Testcontainers; klient zastępuje `ProsteSprawdzanieSymboliczne` konfiguracją.

- [ ] **TASK-056 [POST-MVP] `Lemat.ImageSandbox` (Magick.NET) i `ImageSandboxKlient`**
  - **Zależności:** TASK-040.  **Realizuje:** ADR-052, 9.4, 16.x; NFR-08, NFR-09.  **DoD:** normalizacja do JPEG, usuwanie EXIF, HEIC, limity czasu (8 s), maskowanie i wycinki, testy kontraktowe w procesie testu.

- [ ] **TASK-057 [POST-MVP] `MagazynObiektowS3` (MinIO/S3 UE), podpisane adresy**
  - **Zależności:** TASK-040.  **Realizuje:** 5.2 (Magazyn obiektów), 16.4; NFR-09.  **DoD:** testy Testcontainers.Minio; wybór `Magazyn:Typ=s3`; podpisany URL 10 min.

- [ ] **TASK-058 [POST-MVP] Filtr danych osobowych i Słownik terminów matematycznych**
  - **Zależności:** TASK-020, TASK-043.  **Realizuje:** 10.3.1 p. 3a, 16.3, ADR-042; 18.5 C5-06.
  - **DoD:** testy reguł `a`, `b1`, `b1-i`, `b2`, `c` i 4 wierszy tabeli „Działanie filtra na Poprawkach”; zestaw C5-01 („Pole P”, „prosta k”, „suma S”, „Pole P.”); test G-2 „50 przywróceń” w `PersonalDataFilterTests.cs`.

- [ ] **TASK-059 [POST-MVP] Sprawdzenie zgodności poprawek, `SPRAWDZ_POPRAWKI_TRANSKRYPCJI`, pełna reguła T-7**
  - **Zależności:** TASK-058, TASK-049.  **Realizuje:** 10.3.3, ADR-036, ADR-046; FR-18, FR-28.  **DoD:** pula `S`, limity miękkie, T-7 pełne zastępuje U-06.

- [ ] **TASK-060 [POST-MVP] Agent generujący, plugin SymPy i handlery generowania/walidacji**
  - **Zależności:** TASK-055, TASK-038.  **Realizuje:** 10.2, 9.7, ADR-015, ADR-055; FR-11 … FR-13.  **DoD:** testy 10.2.5 (limit 8 wywołań narzędzi, scenariusze `generowanie-z-narzedziami`, `generowanie-limit-wywolan`).

- [ ] **TASK-061 [POST-MVP] Generowanie Wkładek teoretycznych (`GENERUJ_SZKIC_WKLADKI`)**
  - **Zależności:** TASK-060.  **Realizuje:** ADR-015, G-5; FR-06.  **DoD:** budżet 12.7, szkic w statusie `SZKIC`/`DO_PRZEGLADU`.

- [ ] **TASK-062 [POST-MVP] Opinia pomocnicza i `PONOW_OCENE`**
  - **Zależności:** TASK-049.  **Realizuje:** 10.4.5, 9.13, ADR-008.  **DoD:** opinia nie zmienia trasy ani punktów (test); `PONOW_OCENE` dołącza Ocenę wstępną.

- [ ] **TASK-063 [POST-MVP] Kalibracja: `KALIBRUJ`, bezpieczniki, `EWALUUJ_ZT1`, Kontrola losowa**
  - **Zależności:** TASK-032, TASK-049.  **Realizuje:** 9.8, 11.5, 17.6 (G-1, G-2); NFR-02 … NFR-04.

- [ ] **TASK-064 [POST-MVP] Scheduler: harmonogram (Cronos) i prace cykliczne**
  - **Zależności:** TASK-024, TASK-048.  **Realizuje:** 13.5, ADR-066 p. 5; `UTRZYMAJ_PARTYCJE`, `WYGAS_ELEMENTY`, `ZAMIATAJ_PROFILE`, `SPRAWDZ_ZAWIESZONE_ROZWIAZANIA`, `PORZUC_NIEZATWIERDZONE`, `SPRAWDZ_PULE_DIAGNOSTYCZNA`.  **DoD:** jedna aktywna instancja (`pg_try_advisory_lock`), `przebieg_harmonogramu`.

- [ ] **TASK-065 [POST-MVP] Eskalacja Weryfikacji, kontrola przyjęć, pseudonimizacja i maskowanie zdjęć**
  - **Zależności:** TASK-035, TASK-056.  **Realizuje:** 11.4.3, ADR-031, ADR-043; G-7.  **DoD:** zastępuje U-07 dla kolejki globalnej.

- [ ] **TASK-066 [POST-MVP] RODO: rejestr zdarzeń (S3 Object Lock), `USUN_KONTO`, `USUN_ZDJECIA`, sprzątanie obiektów**
  - **Zależności:** TASK-033, TASK-057.  **Realizuje:** 16.4, ADR-019, ADR-040, ADR-041, ADR-047; G-6; NFR-09.

- [ ] **TASK-067 [POST-MVP] E-mail: `INadawcaEmail` (MailKit) i `WYSLIJ_EMAIL`**
  - **Zależności:** TASK-048.  **Realizuje:** 5.2 (E-mail), 1.3; FR-01 (reset hasła).

- [ ] **TASK-068 [POST-MVP] Import Arkusza CKE (`IMPORTUJ_ARKUSZ`)**
  - **Zależności:** TASK-031, TASK-048.  **Realizuje:** 9.11, 12.9; FR-35.

- [ ] **TASK-069 [POST-MVP] `PredyktorSukcesuGbm` i strategia eksperymentalna `JEV_CHOICE` (Q-RE-01)**
  - **Zależności:** TASK-047, TASK-052.  **Realizuje:** 10.1.6, 10.1.7, ADR-010.

- [ ] **TASK-070 [POST-MVP] Kopie treningowe (`KOPIUJ_DO_ZBIORU_TRENINGOWEGO`, `USUN_KOPIE_TRENINGOWE`)**
  - **Zależności:** TASK-034, TASK-057.  **Realizuje:** ADR-035; FR-34, D-8.

---

# Faza 4: Endpointy API i integracja z frontendem Next.js

- [ ] **TASK-071 [MVP] Infrastruktura HTTP: ProblemDetails, JSON `snake_case`, paginacja, ETag/`version`, OpenAPI, test kontraktu**
  - **Opis:** `Lemat.Api`: `JsonApi.cs` (System.Text.Json: `snake_case`, enumy `UPPER_SNAKE` przez `KonwencjaNazw`), `IExceptionHandler` mapujący wyjątki domenowe/aplikacyjne na ProblemDetails RFC 9457 z katalogiem kodów (`Errors/error-catalog.yaml`, podzbiór 12.2 używany w MVP; komunikaty po polsku),
    `ObslugaBledowWiazania`, `Strona<T>` z kursorem HMAC (`LEMAT_KURSOR_KLUCZ`), pomocnik `If-Match` z `version` w ciele (ADR-069: `412 KONFLIKT_WERSJI`), FluentValidation (rejestracja), `FiltrScenariuszaMock` (nagłówek `X-Lemat-Mock-Scenariusz`, tylko `Development`/`E2E`),
    generowanie `backend/openapi/lemat-v1.json` z operacjami `WithName` wg ADR-049 p. 15, `frontend/tools/generate-errors.ts` (typy kodów błędów z katalogu).
  - **Zależności:** TASK-003, TASK-004, TASK-015.
  - **Realizuje:** 12.1, 12.2, 12.5 (część), 14.1, `docs/spec/api.md` 1–2; ADR-060, ADR-061, ADR-062, ADR-067, ADR-069, ADR-065.
  - **DoD:** testy `Lemat.Api.Tests`: `NiezmiennikNaruszonyException` → ProblemDetails z właściwym `type`/kodem i statusem; walidacja → `422 WALIDACJA` z listą błędów po polsku; `412 KONFLIKT_WERSJI`; kursor podrobiony → `400`; nagłówek Mock ignorowany w `Production` (test na zmiennej środowiska);
    snapshot `lemat-v1.json` zatwierdzony i porównywany w CI; enumy w JSON = wartości z DDL (test `WartosciEnumowApiRowneDdl`, C9-03).

- [ ] **TASK-072 [MVP] Konta i uwierzytelnianie: rejestracja, logowanie, odświeżanie, wylogowanie (JWT ES256, refresh w cookie)**
  - **Opis:** `HaszowanieHasel` (argon2id: m=64 MiB, t=3, p=4), `WystawcaTokenow` (JWT ES256 15 min; klucz ECDSA P-256 z konfiguracji, w Development generowany i zapisywany lokalnie), refresh token w cookie `lemat_rt` (`HttpOnly; Secure; SameSite=Strict`, rotacja, wykrycie ponownego użycia unieważnia rodzinę),
    cookie `lemat_rola`, polityki autoryzacji (`Anonimowy`, `Zalogowany`, `Uczen`, `Nauczyciel`, `Administrator`, `UczenLubNauczyciel`…), handlery `ZarejestrujUzytkownikaHandler`, `ZalogujHandler`, `OdswiezSesjeHandler`, `WylogujHandler`,
    rejestracja Ucznia tworzy `uzytkownik`, `uczen`, `profil_ucznia`, `sciezka_nauki` w jednej transakcji; rejestracja Nauczyciela: `OCZEKUJE_NA_ZATWIERDZENIE`, w Development autozatwierdzenie (U-04); limit prób logowania (`proba_logowania`, `429 LIMIT_PRZEKROCZONY`).
  - **Zależności:** TASK-071, TASK-016.
  - **Realizuje:** 12.3 (`/auth/*`), 16.1, `api.md` 3; ADR-020, ADR-070 p. 1–2, ADR-071 p. 4, ADR-072 (rejestracja jednym rekordem); FR-01, FR-02 (częściowo).
  - **DoD:** testy `Lemat.Api.Tests/Accounts`: `RejestracjaUczniaZwracaSesje`, rejestracja Nauczyciela (Development: `AKTYWNE`; produkcyjna flaga: `OCZEKUJE…` bez sesji), rola `ADMINISTRATOR` w rejestracji → `422 WALIDACJA`, zły e-mail/hasło → ten sam `401 NIEUWIERZYTELNIONY`, konto `ZABLOKOWANE` → `403 KONTO_NIEAKTYWNE`,
    odświeżenie rotuje token, ponowne użycie starego → `401 SESJA_UNIEWAZNIONA`, wylogowanie unieważnia; hasła nie występują w logach (test na logu); wymagania haseł w walidacji po polsku.

- [ ] **TASK-073 [MVP] Uczeń, słowniki i odczyt treści: `/uczniowie/ja`, `/dzialy`, `/umiejetnosci`, `/zadania/{id}`, `/wkladki-teoretyczne/{id}`**
  - **Opis:** `PobierzUczniaHandler`, `ZmienUczniaHandler` (`PATCH` JSON Merge Patch, ADR-072: `null` w `wybrany_dzial_id`/`termin_egzaminu` = wyczyszczenie, `null` w `poziom_matury`/`zgoda_na_trening` → `422`; zmiana poziomu matury w jednej transakcji: wiersze `opanowanie_umiejetnosci`, element `PROPONOWANY` → `WYGASLY`, 9.1),
    `PobierzSlownikiHandler`, `PobierzZadanieUczniaHandler` (`ZadanieUczniaWyjscie` **bez** Rozwiązania wzorcowego i klucza; wymagany dokładnie jeden z `element_sciezki_id`/`przypisanie_id`/`rozwiazanie_id`; widoczność i wersja wg ADR-071 p. 3), `PobierzWkladkeHandler` (Uczeń: tylko `OPUBLIKOWANA`).
  - **Zależności:** TASK-072, TASK-017, TASK-019.
  - **Realizuje:** 12.3, 12.8, 9.1 (zmiana poziomu), `api.md` 3; ADR-071 p. 3, ADR-072; FR-02, FR-04 (słowniki), FR-09, FR-12.
  - **DoD:** testy API: `ZmianaUczniaWyczyszczenieDzialu`, `PATCH` bez `If-Match` → `428`, ze starym `version` → `412`; zmiana poziomu matury tworzy brakujące `opanowanie_umiejetnosci` i wygasza proponowany element;
    `GET /zadania/{id}` dla Ucznia nie zawiera pól `rozwiazanie_wzorcowe`, `wynik_koncowy_wzorcowy`, `poprawna` (test na surowym JSON), bez parametru kontekstu → `422`; Zadanie spoza kontekstu → `409 ZADANIE_NIEDOSTEPNE`/`404`.

- [ ] **TASK-074 [MVP] Klasy: tworzenie, kod zaproszenia, dołączenie Ucznia, lista, usuwanie członka, opuszczenie**
  - **Opis:** `UtworzKlaseHandler`, `PobierzKlaseHandler`, `ListaKlasHandler`, `OdnowKodZaproszeniaHandler` (If-Match `K`), `ArchiwizujKlaseHandler`, `DolaczDoKlasyHandler` (`POST /klasy/dolaczenie`, `404 KOD_ZAPROSZENIA_NIEWAZNY`), `UsunCzlonkaHandler`, `OpuscKlaseHandler`
    (`DELETE /uczniowie/ja/klasy/{id}`), grupa tras `/klasy`; uprawnienia obiektowe: Nauczyciel tylko własne Klasy.
  - **Zależności:** TASK-072, TASK-018.
  - **Realizuje:** 12.3 (`/klasy*`), 12.8, ADR-071; FR-30.
  - **DoD:** testy API: pełny przepływ (Nauczyciel tworzy Klasę → Uczeń dołącza kodem → lista członków); nieważny/zarchiwizowany kod → `404 KOD_ZAPROSZENIA_NIEWAZNY`; obcy Nauczyciel → `404`/`403` (IDOR); `uczen_id` spoza Klasy → `404`; odnowienie kodu unieważnia stary; `DELETE` członka idempotentne.

- [ ] **TASK-075 [MVP] Test diagnostyczny i Profil ucznia: endpointy**
  - **Opis:** `UtworzTestDiagnostycznyHandler` (uproszczony dobór: 1 Zadanie `ZAMKNIETE` diagnostyczne na Dział poziomu Ucznia, nominalny poziom 2 z zapasem 1/3, losowanie z kandydatów; < 50% planowanych pozycji → `409 TEST_NIEDOSTEPNY` — U-09; `409 TEST_JUZ_WYKONANY`),
    `OdpowiedzNaPozycjeTestuHandler` (ponowna odpowiedź nadpisuje — ADR-074 p. 3), `ZakonczTestHandler` (jedna transakcja: status, `rezultat_ujawniony_od`, `wynik_zadania`, zlecenie `AKTUALIZUJ_PROFIL`), `PominTestHandler` (D-14), `PobierzProfilHandler` (własny; `oczekujace_wyniki`), `GET /uczniowie/{id}/profil` dla Nauczyciela (członek jego Klasy).
  - **Zależności:** TASK-073, TASK-050, TASK-027.
  - **Realizuje:** 12.3, 9.1, 10.1.3, `api.md` 3; ADR-010, ADR-074 p. 3; FR-03, FR-04, FR-29 (część), FR-31 (część).
  - **DoD:** testy API: pełny test dla Ucznia `PODSTAWOWY` (odpowiedzi poprawne i błędne) → po `AKTUALIZUJ_PROFIL` (worker w teście) `theta` zmienione, `PonownaOdpowiedzWTescieNadpisuje`, pominięcie bez Wyników zadania, drugi test → `409`; brak puli → `409 TEST_NIEDOSTEPNY`;
    odpowiedź w teście nie ujawnia klucza przed zakończeniem; Nauczyciel obcej Klasy → `403`/`404`.

- [ ] **TASK-076 [MVP] Rekomendacje, Ścieżka nauki i Odpowiedzi na Zadania zamknięte: endpointy**
  - **Opis:** `NastepnyElementHandler` (zwraca istniejący `PROPONOWANY` przed `wygasa_at`; wygasły element leniwie `WYGASLY`, U-08; wywołanie `IRekomendacjaSynchroniczna` **przed** transakcją; w transakcji `SciezkaNauki.Zaproponuj`, `Rekomendacja`),
    `PobierzSciezkeNaukiHandler` (kursor), `RozpocznijElementHandler` (idempotentny, ustala `zadanie_wersja_id`), `PominElementHandler`, `OznaczPrzeczytanieHandler` (Wkładka), `ZlozOdpowiedzHandler` (`POST /zadania/{id}/odpowiedzi`: porównanie z kluczem, `rezultat` ujawniany po odpowiedzi, `Wynik zadania`, zlecenie `AKTUALIZUJ_PROFIL`; kody `409 ELEMENT_SCIEZKI_NIEAKTYWNY`, `409 WERSJA_ZADANIA_NIEAKTUALNA`).
  - **Zależności:** TASK-073, TASK-047, TASK-050, TASK-027.
  - **Realizuje:** 12.3, 9.2, 9.3, 8.5; ADR-023, ADR-030; FR-05 … FR-10, FR-15, FR-29.
  - **DoD:** testy API: dla nowego Ucznia `nastepna` zwraca element z uzasadnieniem po polsku; ponowne wywołanie zwraca ten sam element (C-12); po serii poprawnych odpowiedzi rośnie `poziom_trudnosci`; słaba Umiejętność → najpierw Wkładka (`TypElementuSciezki.WkladkaTeoretyczna`) i `przeczytanie`;
    Zadanie zamknięte: poprawna/błędna odpowiedź → `rezultat` + `wynik_zadania` + praca `AKTUALIZUJ_PROFIL`; odpowiedź na wygasły element → `409 ELEMENT_SCIEZKI_NIEAKTYWNY`; wywołanie modelu poza transakcją (test: brak otwartej transakcji w `DecydujAsync`).

- [ ] **TASK-077 [MVP] Rozwiązania: przesłanie zdjęć (multipart), lista, adres zdjęcia i endpoint mediów lokalnych**
  - **Opis:** `PrzeslijRozwiazanieHandler` (`POST /rozwiazania`, multipart z `[AsParameters]` wg ADR-067: pola `zadanie_wersja_id`, `element_sciezki_id`/`przypisanie_id`, pliki `zdjecia` 1–5; walidacja przez `IObrobkaObrazow`; zapis do `IMagazynObiektow`; `rozwiazanie` `PRZESLANE` + `zdjecie_rozwiazania` + praca `ROZPOZNAJ_PISMO` w jednej transakcji; `202`),
    `PrzeslijNoweZdjeciaHandler` (`POST /rozwiazania/{id}/zdjecia`, limit przesłań), `PonowRozpoznanieHandler`, `PorzucRozwiazanieHandler`, `ListaRozwiazanUczniaHandler`, `GET /rozwiazania/{id}`, `GET /rozwiazania/{id}/zdjecia/{zdjecie_rozwiazania_id}/adres` (`AdresZdjeciaWyjscie` z `url`, `wygasa_at`; `?wersja_zdjecia=ORYGINALNA|MASKOWANA` — w MVP `MASKOWANA` zwraca `404`, U-07),
    `GET /magazyn-lokalny/{token}` (lokalny magazyn, podpis HMAC, U-03), endpoint nie wymaga `Idempotency-Key` w MVP (U-11).
  - **Zależności:** TASK-073, TASK-040, TASK-024, TASK-076.
  - **Realizuje:** 12.3, 9.4, 8.1, 12.5, 12.8; ADR-024, ADR-032, ADR-045 (U-02), ADR-067; FR-16, NFR-08, NFR-09 (część).
  - **DoD:** testy API: wysłanie 1–5 JPEG → `202` + `RozwiazanieWyjscie` w stanie `PRZESLANE` + praca `ROZPOZNAJ_PISMO`; 6 plików → `422`; plik > 10 MB → `413 ZDJECIE_ZA_DUZE`; nieobsługiwany format → `415 NIEOBSLUGIWANY_FORMAT`; Zadanie spoza elementu Ścieżki → `409 ZADANIE_NIEDOSTEPNE`;
    cudze Rozwiązanie → `404` (IDOR); adres zdjęcia ważny 10 min, wygasły → `410`; cudzy token mediów odrzucony; brak wpisu ręcznego Rozwiązania (ADR-024).

- [ ] **TASK-078 [MVP] Transkrypcja, Poprawki Ucznia, zatwierdzenie i odczyt Oceny**
  - **Opis:** `PoprawTranskrypcjeHandler` (`PUT /rozwiazania/{id}/transkrypcja`, `If-Match` na `version` Rozwiązania; nowa wersja Transkrypcji `POPRAWKA_UCZNIA`, `ZasadyPoprawekTranskrypcji`, `422 POPRAWKI_ZBYT_ROZLEGLE`, ≤ 20 wersji na przesłanie),
    `ZatwierdzTranskrypcjeHandler` (`POST …/zatwierdzenie` z `{transkrypcja_id, wersja}`; `409 TRANSKRYPCJA_NIEAKTUALNA`, `412 KONFLIKT_WERSJI`; jedna transakcja: `ZATWIERDZONE`, `zatwierdzono_at`, praca `OCEN_ROZWIAZANIE`; Poprawki → T-7 uproszczone, U-06),
    `PobierzOceneHandler` (`GET …/ocena`, `…/ocena/historia`; Uczeń: Ocena wstępna z komunikatem i adnotacją powodu, `WSTRZYMANA_BLAD_ODCZYTU` bez punktów; przy `WSTRZYMANA_BLAD_ODCZYTU` serializator dla Ucznia ustawia `punkty = null`, punkty kryteriów `null` i uzasadnienie „ocena w toku”, a Nauczyciel widzi punkty i prawdopodobieństwa — 12.6), `OcenaWyjscie` z `kryteria`, `wskazowka` (może być `null` do czasu `NAPISZ_WSKAZOWKE`), `komunikat`.
  - **Zależności:** TASK-077, TASK-049, TASK-021.
  - **Realizuje:** 12.3, 9.4, 9.5, 11.3 (komunikaty), 12.6; ADR-017, ADR-022, ADR-036, ADR-069; FR-18, FR-19 (widok), FR-24, FR-27, FR-28.
  - **DoD:** testy API: edycja w limitach → nowa wersja, 3 wstawione kroki → `422 POPRAWKI_ZBYT_ROZLEGLE`, `PUT` w stanie innym niż `TRANSKRYPCJA_GOTOWA` → `409 ROZWIAZANIE_NIEPRAWIDLOWY_STAN`; zatwierdzenie starej wersji → `409 TRANSKRYPCJA_NIEAKTUALNA`; po zatwierdzeniu Transkrypcji z Poprawkami Ocena jest `WSTEPNA` z powodem `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM`;
    odczyt Oceny wstępnej zawiera komunikat z 11.3; dla `WSTRZYMANA_BLAD_ODCZYTU` surowy JSON Ucznia nie zawiera punktów ani `prawdopodobienstwo` (test), a JSON Nauczyciela zawiera; odczyt cudzej Oceny → `404`.

- [ ] **TASK-079 [MVP] SSE postępu Rozwiązania (`/rozwiazania/{id}/strumien`) z odpytywaniem zapasowym**
  - **Opis:** `KoncentratorZdarzen` (`EventHub`: jedno połączenie `LISTEN lemat_zdarzenia` na proces, rozsyłanie do subskrybentów), `StrumienRozwiazania` (`TypedResults.ServerSentEvents`; zdarzenia `stan` (migawka przy połączeniu i po każdej zmianie), `koniec` (stan terminalny: `OCENIONE`, `OCZEKUJE_NA_WERYFIKACJE`, `BLAD_ROZPOZNAWANIA`, `PORZUCONE`), `ping` co 15 s, `wznow` po `min(exp, 10 min)`, `blad`;
    wznowienie z `Last-Event-ID` → migawka; autoryzacja (tylko właściciel; `404` dla cudzego); limit strumieni `429 ZA_DUZO_STRUMIENI`), kontrakty SSE z `api.md` 5; ścieżka zapasowa: `GET /rozwiazania/{id}` (już w TASK-077).
  - **Zależności:** TASK-077, TASK-024, TASK-071.
  - **Realizuje:** 12.10, `api.md` 5, ADR-051, ADR-064; NFR-01, NFR-08, FR-17, FR-24.
  - **DoD:** testy API/integracyjne: klient SSE otrzymuje kolejne `stan` podczas przejścia `PRZESLANE → … → OCENIONE` na Mockach, a na końcu `koniec` z `RozwiazanieWyjscie`; połączenie po utracie z `Last-Event-ID` dostaje aktualną migawkę; cudze Rozwiązanie → `404`; heartbeat co 15 s (sztuczny `TimeProvider`);
    odcinek bez zdarzeń nie zamyka strumienia przed `wznow`; trigger `trg_rozwiazanie_powiadom` generuje zdarzenia (test z 6.x); strumień nie buforowany (nagłówki).

- [ ] **TASK-080 [MVP] Weryfikacje: Kolejka, szczegóły, podjęcie, zwolnienie, rozstrzygnięcie (Nauczyciel)**
  - **Opis:** `KolejkaWeryfikacjiHandler` (`GET /weryfikacje?dostepnosc&powod&klasa_id&typ_kolejki&kursor`), `PobierzWeryfikacjeHandler` (`WeryfikacjaSzczegoly`: Transkrypcja, zdjęcia (adresy), Ocena wstępna z rozkładami, Kryteria, powody), `PodejmijWeryfikacjeHandler` (Blokada 15 min, `409 WERYFIKACJA_ZAJETA`),
    `ZwolnijWeryfikacjeHandler`, `RozstrzygnijWeryfikacjeHandler` (`POST …/rozstrzygniecie`, `If-Match` = wersja treści Weryfikacji, `RozstrzygniecieWejscie`: punkty per kryterium, pierwszy błędny krok, typ błędu, wariant; tworzy nową `Ocena` `OSTATECZNA` źródło `NAUCZYCIEL`, poprzednia `ZASTAPIONA`;
    `Rozwiazanie → OCENIONE`; zlecenie `AKTUALIZUJ_PROFIL`; `409 KONTO_UCZNIA_NIEAKTYWNE`; `412 KONFLIKT_WERSJI` tylko po zmianie treści), uprawnienia wg 11.4.2/12.8; bez szkicu i przedłużenia Blokady (U-15).
  - **Zależności:** TASK-078, TASK-023, TASK-074.
  - **Realizuje:** 12.3 (`/weryfikacje*`), 9.6, 11.4.2, 12.8, 8.3; ADR-021, ADR-033, ADR-037, ADR-071; FR-25, FR-26, FR-33, FR-34 (część).
  - **DoD:** testy API: Nauczyciel Klasy widzi Weryfikacje swoich Uczniów, obcy Nauczyciel `404`/pusta lista, weryfikator globalny widzi `GLOBALNA`; dwóch Nauczycieli podejmujących → drugi `409 WERYFIKACJA_ZAJETA`; po wygaśnięciu Blokady drugi podejmuje; rozstrzygnięcie tworzy nową Ocenę, stara `ZASTAPIONA` (trigger `trg_ocena_niezmienna`), Rozwiązanie `OCENIONE`, Uczeń widzi Ocenę ostateczną, Wynik zadania zaktualizowany po `AKTUALIZUJ_PROFIL`;
    rozstrzygnięcie na Weryfikacji bez podjęcia → `409 WERYFIKACJA_NIE_PODJETA`; Uczeń nie ma dostępu do endpointów `/weryfikacje` (`403`); sortowanie wg priorytetu i `termin_at`.

- [ ] **TASK-081 [MVP] Frontend: klient API, sesja (token w pamięci), obsługa błędów, TanStack Query**
  - **Opis:** `npm run gen:api` (openapi-typescript 7 z `backend/openapi/lemat-v1.json` → `src/lib/api/schema.d.ts`), `lib/api/client.ts` (`openapi-fetch`, jedyne miejsce `fetch` do `/api/*`, `Authorization: Bearer`, `cache: "no-store"`, nagłówki `If-Match`, `X-Lemat-Mock-Scenariusz` w E2E), `types.ts` (aliasy typów ze słownika),
    `errors.ts`/`handle-error.ts` (ProblemDetails → komunikaty z `messages/pl-PL.json`, `generate-errors.ts`), `lib/auth/SessionProvider.tsx` (`DostawcaSesji`: token dostępu tylko w pamięci, odświeżenie przy ładowaniu przez cookie `lemat_rt`, jedno odświeżenie naraz w obrębie karty; wielokartowość POST-MVP — U-12), `proxy.ts` (przekierowania wg cookie `lemat_rola`, wygoda nawigacji).
  - **Zależności:** TASK-006, TASK-071, TASK-072.
  - **Realizuje:** 12.11, 6.8, `api.md` 6.1, ADR-054, ADR-057 (część), ADR-061; NFR-10.
  - **DoD:** `gen:api` + `git diff --exit-code` w CI; testy Vitest: klient dodaje nagłówek i ponawia po `401` raz po odświeżeniu, `412` → błąd konfliktu, ProblemDetails → polski komunikat; token nie trafia do `localStorage`/`sessionStorage`/cookie dostępnego z JS (test); lint blokuje `fetch('/api/…')` poza `client.ts` (reguła ESLint); `typecheck` zielony na typach wygenerowanych.

- [ ] **TASK-082 [MVP] Frontend: układy ról, nawigacja, strony logowania i rejestracji**
  - **Opis:** `features/accounts` (`index.ts`, `RegisterForm`, `RegisterTeacherForm`, `LoginForm`, schematy zod, react-hook-form), strony `(public)/login`, `(public)/register`, `(public)/register/teacher`, układy `(student)/layout.tsx`, `(teacher)/teacher/layout.tsx`, `(administrator)/admin/layout.tsx`
    (nawigacja, strażnik roli, przycisk wylogowania), strona „Brak dostępu”, komunikaty w `pl-PL.json`, oświadczenie 16+ w rejestracji.
  - **Zależności:** TASK-081.
  - **Realizuje:** 6.8, 6.9 (`accounts`), 9.1; ADR-054; FR-01, NFR-10.
  - **DoD:** testy Vitest/Testing Library: walidacja formularzy po polsku (e-mail, hasło, zgoda 16+), błąd `401` → „Nieprawidłowy e-mail lub hasło”, sukces rejestracji → przekierowanie na `/start` (Uczeń) lub `/teacher` (Nauczyciel); strażnik roli przekierowuje niezalogowanego na `/login`; `typecheck`, `lint`, `build` zielone; brak tekstów na sztywno.

- [ ] **TASK-083 [MVP] Frontend: ustawienia Ucznia, Start (rekomendacja), Ścieżka nauki, Wkładka teoretyczna**
  - **Opis:** `features/learning` (`NextElementCard` — uzasadnienie rekomendacji, przyciski „Rozpocznij”/„Pomiń”, `LearningPathList`, `TheoryInsertView` z `TrescMarkdown`), `features/accounts/LevelSettingsForm` (poziom matury, termin egzaminu, wybrany Dział; `PATCH` z `If-Match`),
    strony `(student)/start`, `(student)/learning-path`, `(student)/theory-inserts/[theoryInsertId]` (`?element=`), `(student)/settings`; przekierowanie ze `start` na test diagnostyczny, gdy dostępny i niewykonany (propozycja, nie przymus — D-14).
  - **Zależności:** TASK-082, TASK-073, TASK-076.
  - **Realizuje:** 6.8, 6.9 (`learning`), `api.md` 7; FR-02, FR-05, FR-06, FR-09, FR-10, NFR-08.
  - **DoD:** testy Vitest: karta rekomendacji renderuje uzasadnienie i wzory KaTeX; „Rozpocznij” wywołuje `POST …/rozpoczecie` i przechodzi na `/exercises/{id}?element=…` (Zadanie) lub `/theory-inserts/{id}?element=…` (Wkładka); „Przeczytałem” wywołuje `…/przeczytanie`; zmiana poziomu matury wymaga potwierdzenia i wysyła `PATCH` z aktualnym `version`;
    układ działa przy szerokości 375 px (test viewport / snapshot klas Tailwind); `typecheck`, `lint`, `build` zielone.

- [ ] **TASK-084 [MVP] Frontend: Test diagnostyczny i Profil ucznia**
  - **Opis:** `features/profile` (`DiagnosticTestFlow` — pozycje, zaznaczanie Odpowiedzi, zakończenie/pominięcie, `ProfileView` — poziom opanowania per Dział i Umiejętność, stan `oczekujace_wyniki` z odpytywaniem co 1 s, maks. 15 s), strony `(student)/diagnostic-test`, `(student)/profile`.
  - **Zależności:** TASK-082, TASK-075.
  - **Realizuje:** 6.8, 9.1, `api.md` 7; FR-03, FR-04.
  - **DoD:** testy Vitest: przepływ testu (zaznacz → zakończ → ekran „Obliczamy profil” → profil z paskami opanowania), `409 TEST_NIEDOSTEPNY` → komunikat i przycisk „Pomiń”, ponowna odpowiedź nadpisuje; profil pokazuje wszystkie Działy poziomu Ucznia (12/13) i ich Umiejętności; dostępność: kontrolki dostępne z klawiatury, etykiety ARIA; `typecheck`, `lint`, `build` zielone.

- [ ] **TASK-085 [MVP] Frontend: strona Zadania — Odpowiedź zamknięta i przesłanie zdjęć kartki (aparat, kompresja)**
  - **Opis:** `features/solutions` (`ExercisePage`, `ClosedAnswerForm` (wybór jednokrotny, prawda/fałsz, ujawnienie `rezultat`), `PhotoUpload` (`<input type="file" accept="image/*" capture="environment" multiple>`, podgląd, usuwanie, 1–5 zdjęć, instrukcja „nie podpisuj kartki imieniem i nazwiskiem”,
    kompresja JPEG q=0.85 dłuższy bok ≤ 2560 px przez `createImageBitmap` + `<canvas>` w wątku głównym — Web Worker POST-MVP, U-12; HEIC zamieniany na JPEG, gdy przeglądarka go dekoduje, w przeciwnym razie komunikat), `lib/photos/compression.ts`), strona `(student)/exercises/[exerciseId]`
    (`?element=` / `?przypisanie=`); po wysłaniu przekierowanie na `/solutions/{id}`; blokada podwójnego kliknięcia (U-11).
  - **Zależności:** TASK-083, TASK-076, TASK-077.
  - **Realizuje:** 6.8, `api.md` 7, 5.2 (Zdjęcia w kliencie); FR-15, FR-16, NFR-08, NFR-09 (ostrzeżenie o danych na kartce).
  - **DoD:** testy Vitest: kompresja zmniejsza duży obraz poniżej limitu i zachowuje proporcje; formularz zamknięty wysyła `POST /zadania/{id}/odpowiedzi` i pokazuje poprawność; 6. zdjęcie odrzucone z komunikatem; wysłanie multipart zawiera pola `zadanie_wersja_id` i `zdjecia`; wzory w treści renderują się (`TrescMarkdown`);
    przycisk „Wyślij” wyłączony w trakcie wysyłania; `typecheck`, `lint`, `build` zielone.

- [ ] **TASK-086 [MVP] Frontend: Rozwiązanie — postęp przez SSE (fallback: odpytywanie), lista, widok Oceny i Wskazówki**
  - **Opis:** `lib/sse/stream.ts` (`fetch` z `Authorization` + `eventsource-parser`, `Last-Event-ID`, ponowne łączenie z wykładniczym opóźnieniem, **automatyczny fallback na odpytywanie** `GET /rozwiazania/{id}` w odstępie z pola `nastepne_odpytanie_ms` (1000 ms w stanach przejściowych), gdy SSE niedostępne; jedyne miejsce otwierające strumienie), `lib/sse/events.ts`,
    `features/solutions` (`SolutionStatus` — etapy „Odczytujemy pismo → Sprawdź odczyt → Oceniamy → Gotowe”, `GradeView` — punkty, kryteria, pierwszy błędny krok, typ błędu, Ocena wstępna z komunikatem i adnotacją powodu, Wskazówka (dociągana po `NAPISZ_WSKAZOWKE`), `ZdjecieKartki` z podpisanym URL i odświeżaniem po 9 min, `SolutionList`),
    strony `(student)/solutions` i `(student)/solutions/[solutionId]`.
  - **Zależności:** TASK-085, TASK-078, TASK-079.
  - **Realizuje:** 6.8, 12.10, 12.11, `api.md` 5–7; FR-17, FR-19, FR-24, FR-27, FR-28 (komunikaty), NFR-01.
  - **DoD:** testy Vitest z fałszywym strumieniem: kolejność stanów aktualizuje UI; zerwanie SSE → ponowne połączenie z `Last-Event-ID`; brak SSE (błąd/blokada) → odpytywanie wg `nastepne_odpytanie_ms` do stanu terminalnego (`null` kończy odpytywanie); Ocena wstępna z `WSTRZYMANA_BLAD_ODCZYTU` nie pokazuje punktów; komunikaty dosłownie z 11.3;
    wzory w kryteriach i Wskazówce renderowane przez KaTeX; `410` na zdjęciu → „Zdjęcie usunięte”; `typecheck`, `lint`, `build` zielone.

- [ ] **TASK-087 [MVP] Frontend: Edytor Transkrypcji (Poprawki Ucznia) i zatwierdzenie**
  - **Opis:** `features/solutions/TranscriptionEditor.tsx` (`EdytorTranskrypcji`): lista kroków z podglądem KaTeX i polem LaTeX, zdjęcie z ramką kroku (`ObszarKroku`), oznaczenie kroków o niskiej `pewnosc_odczytu` („Sprawdź odczyt”), edycja istniejącego kroku, wstawienie najwyżej 2 kroków, licznik limitów (ADR-036),
    „Zapisz poprawki” (`PUT …/transkrypcja` z `If-Match`, obsługa `422 POPRAWKI_ZBYT_ROZLEGLE` i `412`), „Zatwierdź i oceń” (`POST …/zatwierdzenie` z `transkrypcja_id` + `wersja`, obsługa `409 TRANSKRYPCJA_NIEAKTUALNA`), akcje „Prześlij nowe zdjęcia”/„Ponów rozpoznanie”/„Porzuć” w `BLAD_ROZPOZNAWANIA`; bez autozapisu (POST-MVP, U-12).
  - **Zależności:** TASK-086.
  - **Realizuje:** 9.4, `api.md` 3, 6.x (`solutions`), ADR-017, ADR-036; FR-18, NFR-08.
  - **DoD:** testy Vitest: edycja kroku → `PUT` z poprawnym `version`; wstawienie 3. kroku zablokowane w UI i odrzucone przez API komunikatem; konflikt `412` odświeża stan z serwera; zatwierdzenie bez poprawek działa; edytor działa przy 375 px; kroki z LaTeX z błędem składni nie wywracają strony (`throwOnError: false`);
    `typecheck`, `lint`, `build` zielone.

- [ ] **TASK-088 [MVP] Frontend: Klasy (Uczeń: dołączenie kodem; Nauczyciel: lista, szczegóły, profil Ucznia)**
  - **Opis:** `features/classes` (`JoinClassForm`, `ClassList`, `ClassDetails` z kodem zaproszenia, linkiem i listą członków, usuwanie członka, archiwizacja), strony `(public)/join/[code]` (przepływ wg 6.8: zalogowany Uczeń → „Dołącz”, niezalogowany → logowanie/rejestracja z `?kod=`, Nauczyciel → komunikat),
    `(student)/classes`, `(teacher)/teacher/classes`, `(teacher)/teacher/classes/[classId]`, `(teacher)/teacher/classes/[classId]/students/[studentId]` (`StudentProfileView` z `GET /uczniowie/{id}/profil`), `(teacher)/teacher/page.tsx` (skróty).
  - **Zależności:** TASK-082, TASK-074, TASK-075.
  - **Realizuje:** 6.8, 6.9, `api.md` 7; FR-30, FR-31 (część).
  - **DoD:** testy Vitest: dołączenie poprawnym kodem, nieważny kod → komunikat z prośbą o nowy; Nauczyciel tworzy Klasę i widzi kod; odnowienie kodu używa `If-Match`; widok Profilu Ucznia pokazuje opanowanie Działów; Uczeń nie widzi tras `/teacher`; `typecheck`, `lint`, `build` zielone.

- [ ] **TASK-089 [MVP] Frontend: Kolejka weryfikacji i panel rozstrzygnięcia (Nauczyciel)**
  - **Opis:** `features/verifications` (`VerificationList` z filtrami i powodem Weryfikacji (FR-33), `VerificationPanel`: zdjęcia kartki (`ZdjecieKartki`), Transkrypcja, Ocena wstępna z rozkładami prawdopodobieństw, Kryteria z polami punktów, wybór pierwszego błędnego kroku i typu błędu,
    przyciski „Podejmij”, „Zwolnij”, „Zatwierdź ocenę”, licznik Blokady 15 min, obsługa `409 WERYFIKACJA_ZAJETA`/`412`), strony `(teacher)/teacher/verifications` i `(teacher)/teacher/verifications/[verificationId]`; import komponentu podglądu z `solutions` tylko przez `index.ts`.
  - **Zależności:** TASK-082, TASK-080.
  - **Realizuje:** 6.8, 6.9 (`verifications`), `api.md` 7; FR-25, FR-26, FR-33, FR-34 (część).
  - **DoD:** testy Vitest: lista pokazuje powód (niska pewność / poprawny wynik przy błędnych krokach / dowód) i priorytet; podjęcie → panel edytowalny, bez podjęcia pola zablokowane; rozstrzygnięcie wysyła `POST …/rozstrzygniecie` z `If-Match` = `version`; konflikt Blokady pokazuje komunikat;
    zdjęcie kartki ładowane przez podpisany URL; `typecheck`, `lint`, `build` zielone.

- [ ] **TASK-090 [POST-MVP] `Idempotency-Key` (filtr endpointów, `klucz_idempotencji`, `SkrotZapytania`)**
  - **Zależności:** TASK-077, TASK-071.  **Realizuje:** ADR-068, `api.md` 4.1; 18.5 (n).  **DoD:** testy `PonowienieMultipartTymSamymKluczemDajePowtorke`, `PodwojneKlikniecieMultipartDajeWToku`, `PrzejecieRezerwacjiTylkoTymSamymCialem`, `KluczPoWygasnieciuJestNowy`.

- [ ] **TASK-091 [POST-MVP] Limity szybkości 12.7 (klucz konta) i odporne odświeżanie (ADR-070)**
  - **Zależności:** TASK-072.  **Realizuje:** 12.7, ADR-070.

- [ ] **TASK-092 [POST-MVP] Reset hasła (backend + strony + e-mail)**
  - **Zależności:** TASK-067, TASK-072, TASK-082.  **Realizuje:** 12.3 (`/auth/reset-hasla*`); FR-01.

- [ ] **TASK-093 [POST-MVP] Konto: eksport danych, wniosek o usunięcie, przywrócenie (backend + ustawienia)**
  - **Zależności:** TASK-066, TASK-083.  **Realizuje:** 16.4, ADR-040, ADR-041, ADR-044; NFR-09.

- [ ] **TASK-094 [POST-MVP] Administrator: użytkownicy (zatwierdzanie Nauczycieli), Klasy i Przekazanie Klasy**
  - **Zależności:** TASK-072, TASK-074, TASK-082.  **Realizuje:** `/admin/uzytkownicy`, `/admin/klasy`; ADR-044, ADR-071; D-9; zastępuje U-04.

- [ ] **TASK-095 [POST-MVP] Zestawy zadań i Przypisania (backend + frontend Nauczyciela i Ucznia)**
  - **Zależności:** TASK-029, TASK-076, TASK-082.  **Realizuje:** `/zestawy-zadan`, `/przypisania`; ADR-074; FR-32.

- [ ] **TASK-096 [POST-MVP] Powiadomienia w aplikacji (backend, SSE, lider karty)**
  - **Zależności:** TASK-036, TASK-079, TASK-082.  **Realizuje:** `/powiadomienia*`, ADR-051, ADR-057 p. 3; FR-24, FR-33.

- [ ] **TASK-097 [POST-MVP] Treści: generowanie Zadań, Przegląd, Zgłoszenia błędu, Wkładki do przeglądu, otagowanie (backend + frontend)**
  - **Zależności:** TASK-030, TASK-060, TASK-082.  **Realizuje:** `/zadania/generowanie`, `/zlecenia-generowania*`, `/zgloszenia-bledu-zadan*`, `/wkladki-teoretyczne`, `/admin/zadania/{id}/otagowanie`; FR-11, FR-14, FR-36.

- [ ] **TASK-098 [POST-MVP] Import Arkusza CKE (endpoint, SSE, strony Administratora)**
  - **Zależności:** TASK-068, TASK-082.  **Realizuje:** `/admin/arkusze-cke/*`; FR-35.

- [ ] **TASK-099 [POST-MVP] Administrator: Konfiguracje modeli, progi, kalibracja, bezpieczniki**
  - **Zależności:** TASK-063, TASK-053, TASK-082.  **Realizuje:** `/admin/konfiguracje-modeli*`, `/admin/progi-pewnosci`, `/admin/kalibracja`, `/admin/bezpieczniki*`; NFR-06, NFR-03.

- [ ] **TASK-100 [POST-MVP] Administrator: obciążenie Weryfikacji, prace w tle, harmonogram, pula diagnostyczna**
  - **Zależności:** TASK-064, TASK-065, TASK-082.  **Realizuje:** `/admin/obciazenie-weryfikacji`, `/admin/praca-w-tle*`, `/admin/harmonogram`, `/admin/pula-diagnostyczna`; G-5, G-7.

- [ ] **TASK-101 [POST-MVP] Korekty Nauczyciela (`POST /rozwiazania/{id}/korekty`) i strona Rozwiązania Nauczyciela**
  - **Zależności:** TASK-080.  **Realizuje:** 9.6.3, C8-11; FR-34.

- [ ] **TASK-102 [POST-MVP] Autozapis szkicu Transkrypcji (ADR-073) i `save-queue.ts`**
  - **Zależności:** TASK-034, TASK-087.  **Realizuje:** ADR-073, `api.md` 6.4; testy `SzkicZ…`, `autosave-slow-network.spec.ts`; 18.5 (o).

- [ ] **TASK-103 [POST-MVP] Szkic rozstrzygnięcia, przedłużenie Blokady, Zgłoszenie błędu odczytu (backend + frontend)**
  - **Zależności:** TASK-035, TASK-089.  **Realizuje:** 11.6, 9.12, ADR-033; FR-28.

- [ ] **TASK-104 [POST-MVP] Sesja wielu kart (Web Locks, BroadcastChannel), okno tolerancji refresh**
  - **Zależności:** TASK-081.  **Realizuje:** ADR-057, ADR-070 p. 4; `session-two-tabs.spec.ts`; zastępuje część U-12; 18.5 (q).

- [ ] **TASK-105 [POST-MVP] PWA (Serwist), strona offline, kompresja zdjęć w Web Workerze**
  - **Zależności:** TASK-085.  **Realizuje:** 5.2 (PWA), ADR-054; NFR-08; `next build --webpack`.

- [ ] **TASK-106 [POST-MVP] Postępy Klasy (`/klasy/{id}/postepy`) i widok postępów**
  - **Zależności:** TASK-088.  **Realizuje:** FR-31.

- [ ] **TASK-107 [POST-MVP] Pełna macierz uprawnień obiektowych 12.8 i test IDOR dla wszystkich endpointów**
  - **Zależności:** wszystkie endpointy.  **Realizuje:** ADR-071, 12.8; NFR-09.

- [ ] **TASK-108 [POST-MVP] Testy kontraktu 12.12 i kontrola dryfu OpenAPI ↔ TypeScript w CI**
  - **Zależności:** TASK-071, TASK-081.  **Realizuje:** 12.12, ADR-061.

---

# Faza 5: Testy i dopracowanie

- [ ] **TASK-109 [MVP] Test integracyjny potoku end-to-end (API + Worker w procesie, PostgreSQL, Mock)**
  - **Opis:** `Lemat.Api.Tests/Pipeline`: `WebApplicationFactory` + uruchomiony w procesie testu `PulaPracHostedService` na tej samej bazie Testcontainers; scenariusze: rejestracja → poziom matury → test diagnostyczny → rekomendacja → przesłanie zdjęć → rozpoznanie → zatwierdzenie → Ocena automatyczna → Wskazówka → Profil;
    ścieżka Weryfikacji (`weryfikacja-niska-pewnosc`, `weryfikacja-dowod`) → Nauczyciel podejmuje i rozstrzyga → Rozwiązanie `OCENIONE`; `rozpoznanie-nieczytelne` → ponowne zdjęcia → sukces; czas od zatwierdzenia do Oceny na Mockach < 5 s.
  - **Zależności:** TASK-078, TASK-080, TASK-050, TASK-079.
  - **Realizuje:** 9.1 … 9.6, NFR-01 (smoke), Z-1, Z-3, Z-4, Z-5; FR-01 … FR-10, FR-15 … FR-29, FR-33.
  - **DoD:** wszystkie wymienione scenariusze zielone w CI w < 3 min; asercje na `decyzja_modelu` (każda decyzja zapisana z rozkładem), na niezmienności Ocen (stara `ZASTAPIONA`), na tym, że żadne Rozwiązanie z dowodem, niespójnością lub Poprawkami nie kończy jako automatycznie zaliczone;
    test współbieżności: dwa workery nie wykonują tej samej pracy; test SSE: klient dostaje `koniec`.

- [ ] **TASK-110 [MVP] E2E Playwright: ścieżka Ucznia (rejestracja → test → rekomendacja → zdjęcie → Transkrypcja → Ocena)**
  - **Opis:** `frontend/playwright.config.ts` (projekty: Desktop Chrome, iPhone 13, Pixel 7; `webServer`: backend `dotnet run` z `ASPNETCORE_ENVIRONMENT=E2E`, worker, `next dev`/`start`; baza wcześniej `migrate` + `seed-dev`; `extraHTTPHeaders` dla scenariuszy Mock), `frontend/e2e/acceptance/` z testami
    `fr01-account.spec.ts`, `fr03-diagnostic-test.spec.ts`, `fr05-recommendation.spec.ts`, `fr15-closed.spec.ts`, `fr16-upload.spec.ts` (+ `fr18` edycja Transkrypcji), `fr23-auto.spec.ts`, `fr27-hint.spec.ts`, `fr29-profile-after-grade.spec.ts`; zdjęcie testowe jako fixture; `npm run e2e`.
  - **Zależności:** TASK-087, TASK-084, TASK-083, TASK-085, TASK-109.
  - **Realizuje:** 18 (kolumna Weryfikacja), ADR-065 p. 7, 17.x; FR-01 … FR-05, FR-15 … FR-18, FR-23, FR-27, FR-29, NFR-08.
  - **DoD:** wszystkie wymienione specyfikacje zielone na 3 projektach urządzeń lokalnie (`npm run e2e`) i w CI; test `auto-poprawne` kończy Oceną `OSTATECZNA` ≤ 30 s widocznego czasu; test `rozpoznanie-nieczytelne` pokazuje komunikat i pozwala wysłać nowe zdjęcia; zrzuty ekranu/trace przy błędzie; testy nie wymagają kluczy API.

- [ ] **TASK-111 [MVP] E2E Playwright: Nauczyciel — Klasa, Weryfikacja i rozstrzygnięcie**
  - **Opis:** specyfikacje `fr30-class.spec.ts` (Nauczyciel tworzy Klasę, Uczeń dołącza kodem w drugim kontekście przeglądarki), `fr24-preliminary-grade.spec.ts` (`weryfikacja-niska-pewnosc`: Uczeń widzi Ocenę wstępną), `fr25-result-wrong-steps.spec.ts`, `fr26-proof.spec.ts`, `fr33-verification-photos.spec.ts`, `fr34-correction.spec.ts`
    (Nauczyciel podejmuje, ogląda zdjęcie i rozstrzyga; Uczeń widzi Ocenę ostateczną), `fr31-progress.spec.ts` (Profil Ucznia w widoku Nauczyciela — zakres MVP: profil, bez postępów Klasy).
  - **Zależności:** TASK-110, TASK-089, TASK-088.
  - **Realizuje:** 18, 9.6, 11.3; FR-24 … FR-26, FR-30, FR-31 (część), FR-33, FR-34 (część).
  - **DoD:** specyfikacje zielone lokalnie i w CI; test, że Uczeń nie ma dostępu do `/teacher/*` (przekierowanie/`403`); test, że obcy Nauczyciel nie widzi Weryfikacji innej Klasy; dowód zawsze trafia do Nauczyciela (FR-26).

- [ ] **TASK-112 [MVP] Matryca uprawnień MVP (IDOR), README „Szybki start” i skrypt `dev-up`**
  - **Opis:** `Lemat.Api.Tests/Permissions/IdorTests` — tabela (endpoint × rola × własność zasobu) dla wszystkich endpointów MVP (ADR-071: cudze zasoby → `404`/`403`, nigdy `200`); `scripts/dev-up.sh` (PostgreSQL → `migrate` → `seed-dev` → API + Worker + `next dev`, logi, `scripts/dev-down.sh`);
    `README.md` — „Szybki start” (wymagania, 5 komend, konta demo, scenariusze Mock, jak włączyć realnego dostawcę LLM zmiennymi środowiskowymi, jak uruchomić testy i E2E).
  - **Zależności:** TASK-111.
  - **Realizuje:** ADR-071, 12.8 (część MVP), 17.5; CLAUDE.md pkt 10.
  - **DoD:** test IDOR obejmuje wszystkie endpointy z TASK-072 … TASK-080 i jest zielony (brak endpointu bez wiersza w tabeli — test refleksyjny po `EndpointDataSource`); `scripts/dev-up.sh` na czystym środowisku uruchamia aplikację i `GET /api/v1/zdrowie` + strona `/` odpowiadają `200`;
    instrukcje z README wykonane krok po kroku przez agenta bez błędów (zapis w raporcie); **MVP kompletne end-to-end** — zaktualizuj w `HANDOFF.md` sekcję „Stan prac”.

- [ ] **TASK-113 [POST-MVP] Pełne testy architektury (`BrakCykliMiedzyModulamiAplikacji`, `KontenerDiTests`, zależności modułów)**
  - **Zależności:** wszystkie `[MVP]`.  **Realizuje:** 6.2 reguła 3, 6.5; 18.5 (k).

- [ ] **TASK-114 [POST-MVP] Obserwowalność: OpenTelemetry, Prometheus, Grafana, Tempo, metryki 17.3, alerty**
  - **Zależności:** TASK-010.  **Realizuje:** 17.3, 5.2; NFR-01.

- [ ] **TASK-115 [POST-MVP] Testy obciążeniowe (G-3) i `Lemat.Simulations`**
  - **Zależności:** TASK-054, TASK-052.  **Realizuje:** 17.4, G-3, A-03; NFR-01.

- [ ] **TASK-116 [POST-MVP] Test chaosu NFR-07 (G-4) i narzędzia ewaluacji ZT-1 (G-1, G-2)**
  - **Zależności:** TASK-053, TASK-063.  **Realizuje:** 17.6; NFR-02, NFR-03, NFR-07.

- [ ] **TASK-117 [POST-MVP] Bezpieczeństwo i RODO (G-6): nagłówki, CSP, retencja, testy odtworzenia PITR**
  - **Zależności:** TASK-066, TASK-010.  **Realizuje:** 16, G-6; NFR-09.

- [ ] **TASK-118 [POST-MVP] Dostępność i urządzenia: a11y (axe), iPhone 13 / Pixel 7, wydajność strony**
  - **Zależności:** TASK-110.  **Realizuje:** NFR-08, NFR-10.

- [ ] **TASK-119 [POST-MVP] Wdrożenie produkcyjne: 2 VM, Caddy, `efbundle`, kopie, runbook**
  - **Zależności:** TASK-010, TASK-114, TASK-117.  **Realizuje:** 17.5; ADR-048.

- [ ] **TASK-120 [POST-MVP] Pełny zestaw testów akceptacyjnych `frNN-*.spec.ts` dla wszystkich FR i `check_rtm.py` zielony**
  - **Zależności:** wszystkie funkcje POST-MVP.  **Realizuje:** 18 (RTM), 18.5 (r).

---

# Macierz pokrycia: FR/NFR → TASK-ID

Legenda: **M** = realizowane w `[MVP]`; **P** = `[POST-MVP]`. „Pełne” oznacza kompletne pokrycie wymagania architekturą; „częściowe” — uproszczenie MVP z listy U-xx w `HANDOFF.md`.

| Wymaganie | MVP (TASK) | Zakres MVP | POST-MVP (TASK) |
|---|---|---|---|
| FR-01 Konto, logowanie | 016, 072, 081, 082 | pełne bez resetu hasła; Nauczyciel auto-zatwierdzany w Development (U-04) | 067, 092 (reset hasła), 094 (zatwierdzanie), 091, 104 |
| FR-02 Poziom matury, termin | 073, 083 | pełne | — |
| FR-03 Test diagnostyczny | 019, 027, 075, 084 | uproszczony: 1 Zadanie na Dział (U-09) | 100 (pula diagnostyczna, G-5) |
| FR-04 Widok Profilu | 019, 025, 075, 084 | pełne | — |
| FR-05 Następne Zadanie z Profilu | 047, 076, 083 | pełne (`POLITYKA_ELO`) | 069 (`JEV_CHOICE`) |
| FR-06 Wkładka przed Zadaniami | 027, 047, 076, 083 | Wkładki z seeda | 061, 097 (generowanie, przegląd) |
| FR-07 Rosnąca trudność | 047, 076 | pełne | — |
| FR-08 Powtórki | 047, 076 | pełne w polityce (wygasanie leniwe, U-08) | 064 |
| FR-09 Wybór Działu | 073, 083 | pełne | — |
| FR-10 Uzasadnienie rekomendacji | 047, 083 | pełne | — |
| FR-11 Generowanie Zadań | — | — | 030, 060, 097 |
| FR-12 Zadanie z treścią, wzorcem i kryteriami | 017, 027, 028, 073 | dane seed + odczyt | 060 |
| FR-13 Walidacja wygenerowanych | — | — | 030, 055, 060 |
| FR-14 Przegląd przez Nauczyciela | — | — | 030, 097 |
| FR-15 Zadanie zamknięte | 076, 085 | pełne | — |
| FR-16 Zdjęcia kartki | 040, 077, 085 | częściowe: bez normalizacji/EXIF (U-02), lokalny magazyn (U-03) | 056, 057, 090, 105 |
| FR-17 Odczyt pisma i kroki | 043, 049, 086 | pełne (Mock/LLM) | 058 (filtr danych osobowych) |
| FR-18 Podgląd i Poprawki | 020, 078, 087 | częściowe: limity twarde, bez autozapisu i Sprawdzenia zgodności (U-06, U-12) | 034, 059, 102 |
| FR-19 Ocena krok po kroku | 021, 044, 045, 049 | pełne | — |
| FR-20 Wynik końcowy nie wystarcza | 021 | pełne | — |
| FR-21 Wynikanie kroków | 040, 044 | częściowe: proste porównanie, `NIEROZSTRZYGNIETE` → pytanie/N-6 (U-05) | 055 |
| FR-22 Pewność oceny | 021, 045 | pełne (kalibrator tożsamościowy, U-10) | 032, 063 |
| FR-23 Ocena automatyczna | 022, 049 | pełne | — |
| FR-24 Ocena wstępna dla Ucznia | 022, 049, 078, 086 | pełne | 096 (powiadomienia) |
| FR-25 Poprawny wynik, błędne kroki → Weryfikacja | 022, 049 | pełne | — |
| FR-26 Dowód → Weryfikacja | 022, 049 | pełne | — |
| FR-27 Wskazanie błędu i Wskazówka | 021, 046, 050, 078, 086 | pełne | — |
| FR-28 Błąd odczytu nie obniża Oceny | 021, 022 | częściowe: reguły T-3, bez Zgłoszenia błędu odczytu i filtra (U-07) | 058, 059, 103 |
| FR-29 Ocena aktualizuje Profil | 019, 050, 076 | pełne | 064 (`ZAMIATAJ_PROFILE`) |
| FR-30 Klasy i zaproszenia | 018, 074, 088 | pełne | 094 |
| FR-31 Postępy klasy i uczniów | 075, 088 | częściowe: Profil Ucznia w widoku Nauczyciela | 106 |
| FR-32 Przypisywanie Zadań | — | — | 029, 095 |
| FR-33 Kolejka weryfikacji z powodem | 023, 080, 089 | pełne bez SLA/eskalacji (U-15) | 035, 065, 100, 103 |
| FR-34 Korekty Nauczyciela i kalibracja | 080, 089 | częściowe: rozstrzygnięcie; bez korekt po fakcie i kalibracji | 032, 063, 099, 101 |
| FR-35 Import Arkuszy CKE | — | — (dane startowe autorskie, U-19) | 031, 068, 098 |
| FR-36 Tagowanie Zadań | 017, 025, 027, 028 | dane seed (rodzaj, Dział, Umiejętność, trudność) | 097 (edycja otagowania) |
| NFR-01 Ocena ≤ 30 s | 048, 049, 079, 109 | architektura i smoke na Mockach | 114, 115 |
| NFR-02 Zgodność ≥ 90% | — | — | 063, 116 (ZT-1) |
| NFR-03 Błąd auto ≤ 2% | 022 (reguły bezpieczne, Z-3) | brak pomiaru | 032, 063, 116 |
| NFR-04 Skalibrowana pewność | 018 (kalibrator tożsamościowy) | brak kalibracji | 032, 063 |
| NFR-05 Wyjaśnialność, zapis decyzji | 018, 045, 047, 049 | pełne (`decyzja_modelu`, uzasadnienia) | — |
| NFR-06 Wymienność modeli AI | 038, 039, 042, 051 | pełne dla portów; dostawcy `openai-zgodny`, `azure-openai`, `anthropic` (U-13) | 052, 099 |
| NFR-07 Model zapasowy | 051 | częściowe: LLM jako jedyny realny model decyzyjny | 052, 053, 116 |
| NFR-08 Telefon i komputer, zdjęcie z aplikacji | 006, 085, 087, 110 | responsywny układ, `capture`; bez PWA (U-12) | 105, 118 |
| NFR-09 RODO, usuwanie zdjęć | 040 (walidacja), 077 (adresy czasowe) | **niespełnione produkcyjnie** (U-02, U-07) | 056, 057, 058, 065, 066, 093, 117 |
| NFR-10 Interfejs po polsku | 006, 071, 081–089 | pełne (`pl-PL`) | — |

**Kontrola kompletności (dla `handoff-guardian` / krytyka):** każde FR-01 … FR-36 i NFR-01 … NFR-10 występuje w macierzy dokładnie raz;
każdy TASK `[MVP]` występuje w kolumnie „MVP” co najmniej raz albo jest pozycją infrastrukturalną (001 … 015, 024, 026, 038 … 041, 048, 071, 081, 109 … 112).
