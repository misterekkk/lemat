# Lemat

**Lemat** to platforma do nauki do matury z matematyki, w której system
agentów AI dobiera zadania do poziomu ucznia, generuje nowe zadania z tematów
sprawiających mu trudność i ocenia rozwiązania przesłane jako zdjęcie kartki.

Projekt powstaje w ramach zajęć dydaktycznych jako przypadek do analizy,
projektowania i eksperymentowania z różnymi sposobami realizacji systemu.

## Dokumentacja

Projekt będzie rozwijany stopniowo wraz z kolejnymi etapami analizy i projektowania.

Na początek:

- [Wizja](docs/vision.md)
- [Słownik pojęć](docs/glossary.md)
- [Wymagania](docs/requirements.md)
- [Architektura (wstępna)](docs/architecture.md)

## Szybki start

Wymagania: .NET 10 SDK, Node 22 + npm, Docker (PostgreSQL 16 i testy z Testcontainers).

```bash
cp .env.example .env            # lokalna konfiguracja bez sekretów (tryb Mock)
scripts/db-up.sh                # PostgreSQL 16 z docker-compose (port 5433)
scripts/verify-all.sh           # pełna weryfikacja: backend + frontend
scripts/verify-backend.sh       # restore, build -warnaserror, test, format
scripts/verify-frontend.sh      # ci, typecheck, lint, test, build, check:headers
```

## Szybki start POC (backend)

Wymagania: .NET 10 SDK, `curl`, `jq` oraz Docker (PostgreSQL 16 z `docker-compose.yml`, port 5433) albo lokalny PostgreSQL 16 na porcie 5432. Aplikacja działa bez żadnych kluczy (modele AI w trybie Mock).

```bash
scripts/dev-up.sh               # baza, migracje, seed-poc, Lemat.Api (:5080) i Lemat.Worker w tle; logi w .logs/
scripts/poc-smoke.sh            # test dymny: rejestracja → Zadania → zdjęcie JPEG → zatwierdzenie → Ocena → profil
scripts/dev-up.sh --stop        # zatrzymuje Api i Worker (baza zostaje)
scripts/dev-up.sh --reset       # odtwarza pustą bazę i magazyn zdjęć (kasuje dane lokalne!), po czym startuje
```

- API: `http://localhost:5080/api/v1` (kontrakt: `docs/POC_CONTRACT.md`), kontrola zdrowia: `/api/v1/zdrowie`; Worker: `http://localhost:8081/zdrowie`.
- Konta demo: `uczen1@lemat.local`, `uczen2@lemat.local`, hasło `Lemat-Demo-2026!` (tylko lokalne POC).
- Baza: `LEMAT_BAZA` z `.env` ma pierwszeństwo; bez niej skrypt używa Dockera (`LEMAT_PG_PORT`, domyślnie 5433), a gdy Docker nie działa — lokalnego PostgreSQL na 5432 (`scripts/db-local-setup.sh`).
- Worker musi działać obok API (ta sama baza), inaczej Rozwiązania nie przejdą do `TRANSKRYPCJA_GOTOWA` i `OCENIONE`. Rozpoznanie trwa ok. 3 s, ocena ok. 2 s (`Lemat__Poc__*` w `.env.example`).
- Test integracyjny przepływu: `dotnet test backend/tests/Lemat.Api.Tests --filter "FullyQualifiedName~PocFlowTests"`.

## Struktura

```
lemat/
├── README.md
├── docs/
│   ├── vision.md
│   ├── glossary.md
│   ├── requirements.md
│   └── architecture.md
└── src/
```
## Autorzy

Kacper Piotrowski i Igor Ptak

## Licencja

Dokumentacja w tym repozytorium jest udostępniana na licencji Creative Commons
Attribution 4.0 International (CC BY 4.0).

Kod źródłowy jest udostępniany na licencji MIT.

Treści zadań maturalnych pochodzące z arkuszy CKE podlegają prawom ich autorów.
