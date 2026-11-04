# ADR-048: Zmiana stosu na .NET 10 + Next.js

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.0, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 1, wersja 3.0) |
| Bramka G-0 | nie — decyzja właściciela projektu podjęta przed iteracją; nie zmienia wymagań ani słownika |
| Zastępuje | stos z sekcji 5.2 wersji 2.4; ADR-001, ADR-013, ADR-014, ADR-045 (status „Zastąpiony przez ADR-048”) |
| Aktualizuje realizację | ADR-002, ADR-003, ADR-016, ADR-020, ADR-031 (decyzje obowiązują, zmienia się realizacja) |
| Szczegółowe decyzje | ADR-049 (nazewnictwo), ADR-050 (SymPy), ADR-051 (SSE), ADR-052 (obrazy), ADR-053 (prace w tle), ADR-054 (frontend), ADR-055 (warstwa AI) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — sekcje 5, 6, 3.5, 3.6 |

## Status

Przyjęta.

## Kontekst

Wersja 2.4 architektury zakładała backend w Pythonie 3.12 (FastAPI,
Pydantic v2, SQLAlchemy 2, Alembic, `asyncio`), obróbkę obrazu w Pillow,
SymPy w puli procesów workera, frontend React 18 + Vite oraz odpytywanie
zamiast strumieni. Właściciel projektu zdecydował o zmianie stosu na:
backend .NET 10 (C#, ASP.NET Core, EF Core + Npgsql, PostgreSQL 16) w
`backend/`, warstwę agentów AI w .NET (Microsoft.Extensions.AI + Semantic
Kernel), frontend Next.js (App Router, TypeScript) w `frontend/` i
komunikację REST + SSE. Reguły domenowe, liczby i decyzje biznesowe wersji
2.4 (FR/NFR, ADR-004 … ADR-047 w warstwie domenowej, parametry 22, decyzje
D-1 … D-17) pozostają w mocy; zmienia się realizacja techniczna. Część
zależności wersji 2.4 nie ma bezpośredniego odpowiednika w .NET (SymPy,
`pillow-heif`, `hypothesis`, `import-linter`), więc każdą trzeba zastąpić
konkretną decyzją.

## Decyzja

1. **Architektura procesów bez zmian merytorycznych (przejęte z ADR-001):**
   modularny monolit; jedna solucja `backend/Lemat.slnx`; procesy
   `Lemat.Api` (2 repliki), `Lemat.Worker` (2 repliki), `Lemat.Scheduler`
   (1 aktywna instancja); PostgreSQL 16 jako jedyne źródło prawdy i
   mechanizm prac w tle (ADR-002); brak mikroserwisów per agent i
   transakcji rozproszonych.
2. **Obrazy kontenerów:** `lemat-backend` (wszystkie punkty wejścia .NET:
   Api, Worker, Scheduler, ImageSandbox, `efbundle`), `lemat-frontend`
   (Next.js standalone), `lemat-sympy` (sidecar Python, ADR-050), `caddy:2`.
3. **Projekty i warstwy:** `Lemat.Domain` (bez pakietów), `Lemat.Application`
   (przypadki użycia, fasady, porty, Orkiestrator), `Lemat.Agents`,
   `Lemat.Models` (adaptery modeli i Mock), `Lemat.Infrastructure` (EF Core,
   Npgsql, S3, sidecary, e-mail, bezpieczeństwo), punkty kompozycji
   `Lemat.Api`, `Lemat.Worker`, `Lemat.Scheduler`, izolowany
   `Lemat.ImageSandbox` oraz projekty testów (sekcja 6.4). Granice
   egzekwują referencje projektów, NetArchTest.Rules,
   `Microsoft.CodeAnalysis.BannedApiAnalyzers` i test słownika (6.5).
4. **Decyzje dla zależności, które w wersji 2.4 były pythonowe lub
   React/Vite:**

   | Obszar | Wersja 2.4 | Decyzja 3.0 | Uzasadnienie |
   |---|---|---|---|
   | HTTP API | FastAPI, Uvicorn | ASP.NET Core 10 Minimal APIs, `MapGroup` per moduł, `TypedResults`, ProblemDetails RFC 9457 | mniejszy narzut niż MVC; grupy tras = moduły |
   | DTO i walidacja | Pydantic v2 | rekordy C#, System.Text.Json (konteksty źródłowe, `JsonNamingPolicy.SnakeCaseLower`), FluentValidation 12 | kontrakt JSON wersji 2.4 zachowany 1:1 (snake_case, ADR-049) |
   | OpenAPI | generowane z FastAPI | `Microsoft.AspNetCore.OpenApi` (OpenAPI 3.1), plik `backend/openapi/lemat-v1.json` generowany przy budowie | źródło typów TypeScript i testu kontraktowego |
   | ORM | SQLAlchemy 2 | EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10; Npgsql bezpośrednio dla `SKIP LOCKED`, `LISTEN/NOTIFY`, blokad doradczych | EF Core dla agregatów i optymistycznej współbieżności; surowy SQL tam, gdzie semantyka PostgreSQL jest istotą mechanizmu |
   | Migracje | Alembic | EF Core Migrations, `efbundle` w kontenerze `migracje` przed wdrożeniem; triggery, funkcje, partycje i `UNLOGGED` przez `migrationBuilder.Sql` z plików `Migrations/Sql/*.sql`; test zgodności schematu z DDL 7.3 | jedna droga zmian schematu; DDL 7.3 nadal jest schematem docelowym |
   | Prace w tle | tabela + `asyncio` | tabela `praca_w_tle` + `BackgroundService` na pulę + Npgsql (ADR-053) | transakcyjne zlecanie (ADR-002) |
   | Harmonogram | proces scheduler | `Lemat.Scheduler` + Cronos (parser cron) + `pg_try_advisory_lock` | bez dodatkowego magazynu stanu |
   | Obliczenia symboliczne | SymPy w puli procesów | sidecar `lemat-sympy` (Python 3.12, SymPy 1.13) na gnieździe Unix (ADR-050) | brak równoważnej biblioteki .NET |
   | Obraz | Pillow + `pillow-heif` w `ProcessPoolExecutor` | Magick.NET-Q8-AnyCPU 14 w procesie `Lemat.ImageSandbox` (ADR-052) | HEIC, izolacja dekoderów natywnych |
   | Modele AI | SDK dostawców w adapterach | Microsoft.Extensions.AI (`IChatClient`) + Semantic Kernel (szablony, pluginy), Jev przez `HttpClient`, Mock każdego dostawcy (ADR-055) | NFR-06, praca offline |
   | Postęp operacji | odpytywanie (ADR-014) | SSE + odpytywanie zapasowe (ADR-051) | NFR-01, NFR-08 |
   | JWT | EdDSA | ES256 (ECDSA P-256), `Microsoft.AspNetCore.Authentication.JwtBearer` | EdDSA nie jest obsługiwane natywnie przez `Microsoft.IdentityModel.*`; reszta ADR-020 bez zmian |
   | Hasła | argon2id (Python) | `Konscious.Security.Cryptography.Argon2`, m = 64 MiB, t = 3, p = 4 | te same parametry (16.1) |
   | Magazyn obiektów | SDK S3 | `AWSSDK.S3` 4 (S3 w UE, MinIO lokalnie), Object Lock | bez zmian funkcjonalnych |
   | E-mail | SMTP | MailKit 4 | |
   | Obserwowalność | OpenTelemetry, Prometheus | OpenTelemetry .NET → OTLP → OpenTelemetry Collector (eksporter Prometheus, Tempo), logi JSON `AddJsonConsole`; nazwy metryk 17.3 bez zmian | eksporter Prometheus w Collectorze zamiast pakietu w procesie |
   | Testy | pytest, hypothesis | xUnit v3, Shouldly, NSubstitute, FsCheck 3, Testcontainers 4 (PostgreSQL 16, MinIO, `lemat-sympy`), `WebApplicationFactory`, WireMock.Net; frontend: Vitest + Testing Library, Playwright | testy właściwości (FsCheck) zastępują `hypothesis` 1:1 |
   | Linter architektury | `import-linter` | referencje projektów + NetArchTest.Rules + BannedApiAnalyzers | NetArchTest jest niezależny od frameworka testów (wynik jako obiekt), działa z xUnit v3 |
   | Frontend | React 18 + Vite, `react-i18next` | Next.js 16 App Router, React 19, TypeScript 5.9, Serwist (PWA), next-intl (pl-PL), KaTeX, TanStack Query, openapi-typescript (ADR-054) | decyzja właściciela; grupy tras per rola |
   | Konfiguracja | `lemat/konfiguracja.py` + `LEMAT_*` | `LematOpcje` (Options) + `appsettings.json` + te same zmienne `LEMAT_*` | nazwy parametrów 22 bez zmian |

5. **Wersje główne** (5.2) są przypięte; zmiana wersji głównej któregoś
   elementu wymaga nowego ADR. Łatki są przypinane w
   `Directory.Packages.props`, `package-lock.json` i `uv.lock`.
6. **Sekcje dokumentu z kodem Pythona** (7, 10, 12–15) zachowują treść
   domenową i są oznaczone notką „Do przepisania na .NET w iteracji 2/3”;
   do czasu przepisania obowiązuje mapowanie 5.5 i 6.10.

## Alternatywy

- **Pozostanie przy Pythonie** — odrzucone decyzją właściciela projektu.
- **Hybryda: backend .NET + osobny serwis Python dla całej warstwy AI**
  (agenci w Pythonie) — odrzucona: przenosi granicę procesu na ścieżkę
  krytyczną NFR-01 (dodatkowe serializacje stanu i decyzji), rozdziela
  transakcję „zapis Oceny + zlecenie pracy” na dwa procesy i dubluje model
  domeny; w Pythonie zostaje wyłącznie bezstanowy SymPy (ADR-050).
- **Kontrolery MVC zamiast Minimal APIs** — odrzucone: ~100 endpointów
  12.3 grupuje się naturalnie w `MapGroup` per moduł, a filtry endpointów
  (idempotencja, ETag) są prostsze niż atrybuty akcji.
- **Dapper zamiast EF Core** — odrzucone dla agregatów (ręczne śledzenie
  zmian i tokenów współbieżności dla 59 tabel); Npgsql bez ORM pozostaje dla
  mechanizmów 13.4 i 13.2.
- **Hangfire / Quartz.NET / MassTransit** — odrzucone w ADR-053.

## Konsekwencje

- Jeden język (C#) dla domeny, agentów i infrastruktury; Python wyłącznie
  w bezstanowym sidecarze SymPy (ryzyko R-23).
- Kontrakty JSON, nazwy tabel, kolumn, typów wyliczeniowych, prac w tle i
  metryk wersji 2.4 pozostają bez zmian (ADR-049), więc sekcje 7–22 są
  ważne jako specyfikacja domenowa.
- Iteracja 2 przepisuje sekcje 7 (EF Core), 10 i 13 (agenci, porty,
  prace w tle); iteracja 3 — 12, 14, 15 (REST/SSE, błędy, współbieżność);
  iteracja 4 — audyt i RTM.
- Obecne `TASKS.md`, `HANDOFF.md` i `CLAUDE.md` opisują stos 2.4 i zostaną
  zastąpione po pętli 3.x (18.4).
