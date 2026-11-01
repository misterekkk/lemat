# ADR-047: Sprzątanie osieroconych obiektów magazynu

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 5) |
| Data decyzji | 2026-11-01 (iteracja 5, wersja 2.3) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 5).

## Kontekst

Po odtworzeniu bazy z PITR obiekty kopii treningowych i zdjęć utworzone po punkcie odtworzenia zostawały w magazynie bez wierszy `kopia_treningowa` / `zdjecie_rozwiazania`, więc odwołanie zgody i `USUN_KONTO` nie mogły ich usunąć (do reguły 24 miesięcy) — przypadek odrzucony w ADR-035; sprzątanie z E-02 obejmowało tylko prefiks `zdjecia/` (C3-06).

## Decyzja

Praca `SPRZATAJ_OSIEROCONE_OBIEKTY` dla bucketów `zbior-treningowy/`, `zdjecia/`, `zdjecia-maskowane/`: tryb `CODZIENNY` (04:30 UTC, obiekty starsze niż 1 h bez wiersza → `DELETE`) i tryb `ODTWORZENIE` (krok 2a procedury PITR przed startem workerów, obiekty nowsze niż `T − 5 min` bez wiersza → `DELETE`); metryka i alert; G-6 (d).

## Alternatywy

Zdarzenie RODO `KOPIA_TRENINGOWA_UTWORZONA` w Object Lock — odrzucone: ≈ 750 obiektów dziennie w rejestrze rozliczalności dla zdarzenia, które nie jest decyzją osoby, a i tak nie obejmuje zdjęć Rozwiązań; S3 Inventory zamiast listowania — odrzucone dla pilotażu (raport z opóźnieniem do 48 h nie spełnia kroku 2a), do ponownej oceny przy 20× A-03.

## Konsekwencje

Każdy obiekt danych osobowych w magazynie ma wiersz w bazie albo zostaje usunięty w ≤ 25 h (tryb codzienny) lub przed wznowieniem przetwarzania (odtworzenie); koszt ≈ 1 900 wywołań LIST i ≈ 6 min pracy dziennie (16.4).
