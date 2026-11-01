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

Pełna instrukcja uruchomienia aplikacji pojawi się w dalszych etapach prac.

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
