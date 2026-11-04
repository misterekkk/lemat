# ADR-045: Normalizacja zdjęć w piaskownicy procesowej `api`

| Pole | Wartość |
|---|---|
| Status | Zastąpiony przez ADR-048 (wersja 3.0, 2026-11-01); poprzedni status: Nowa (it. 4) |
| Data decyzji | 2026-11-01 (iteracja 4, wersja 2.2) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zastąpiony przez ADR-048 (wersja 3.0, 2026-11-01); poprzedni status: Nowa (it. 4).

**Wyjaśnienie zastąpienia (wersja 3.0).** Decyzja domenowa obowiązuje: normalizacja zdjęć (dekodowanie, usunięcie EXIF/GPS, zmiana rozmiaru, JPEG) odbywa się synchronicznie przed zapisem do magazynu obiektów, w piaskownicy z limitami, z odczytem nagłówka przed dekodowaniem, ≤ 12 000 px na wymiar i ≤ 40 Mpx, `422 ZDJECIE_NIEPRAWIDLOWE`, a oczekiwanie na piaskownicę > 5 s daje `503`. Zmienia się realizacja: Pillow, `ProcessPoolExecutor`, `RLIMIT_*` i `run_in_executor` zastępuje Magick.NET w osobnym procesie `Lemat.ImageSandbox` (sidecar obok każdej repliki `api` i `worker`) z limitami cgroup kontenera i `ResourceLimits` Magick.NET — ADR-052.

## Kontekst

Dekodowanie HEIC/PNG/WEBP i zmiana rozmiaru do 5 × 10 MB odbywały się w procesie `api` bez limitów, wbrew deklaracji 16.2 (bomba dekompresyjna, blokowanie pętli zdarzeń w szczycie „koniec lekcji”, C2-15).

## Decyzja

`ProcessPoolExecutor` z 2 procesami na replikę `api`, `RLIMIT_AS` 512 MiB, `RLIMIT_CPU` 5 s, 8 s czasu ściennego na plik; odczyt nagłówka przed dekodowaniem, ≤ 12 000 px na wymiar i ≤ 40 Mpx (`MAX_IMAGE_PIXELS`), `422 ZDJECIE_NIEPRAWIDLOWE`; oczekiwanie na proces > 5 s → `503`; `run_in_executor` w pętli zdarzeń.

## Alternatywy

Normalizacja w workerze przed `ROZPOZNAJ_PISMO` — odrzucona (zapis nieznormalizowanego oryginału z EXIF/GPS do S3 przed usunięciem metadanych, +0,4 s w fazie R na ścieżce krytycznej, osobna obsługa błędów formatu po `202`).

## Konsekwencje

Wykorzystanie piaskownicy ≈ 9% w szczycie (17.4); błędne pliki odrzucane synchronicznie przed zapisem.
