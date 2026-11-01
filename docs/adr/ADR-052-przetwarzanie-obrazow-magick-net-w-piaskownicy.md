# ADR-052: Przetwarzanie obrazów — Magick.NET w osobnym procesie `Lemat.ImageSandbox`

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.0, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 1, wersja 3.0) |
| Bramka G-0 | nie |
| Realizuje | ADR-048 p. 4; zastępuje realizację ADR-045 (treść domenowa ADR-045 obowiązuje); aktualizuje realizację ADR-031 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 5.1, 5.3, 16.2, 22 |

## Status

Przyjęta.

## Kontekst

Wymagania wobec obróbki zdjęć (12.5, 16.2, ADR-031, ADR-036, ADR-045):
przyjęcie JPEG, PNG, HEIC i WEBP do 10 MB (5 plików na przesłanie);
odczyt nagłówka przed dekodowaniem, ≤ 12 000 px na wymiar i ≤ 40 Mpx;
dekodowanie, korekta orientacji EXIF, usunięcie wszystkich metadanych (EXIF,
GPS, XMP, ICC poza sRGB), zmiana rozmiaru do dłuższego boku 2560 px, zapis
JPEG; maskowanie ramek danych osobowych (margines 8 px) w workerze; wycinki
kroków i zdjęcie poglądowe 1024 px do Sprawdzenia zgodności poprawek
(10.3.3). Dekodery obrazów są kodem natywnym narażonym na pliki złośliwe
(bomby dekompresyjne, błędy pamięci), więc dekodowanie nie może odbywać się
w procesie obsługującym żądania ani w procesie wykonującym prace w tle
(ADR-045).

## Decyzja

1. Biblioteka: **Magick.NET-Q8-AnyCPU 14** (Apache-2.0; ImageMagick 7 z
   libjpeg-turbo, libpng, libwebp, libheif + libde265 do HEIC) w
   projekcie `Lemat.ImageSandbox`, wyłącznie tam (BannedApiAnalyzers
   blokuje `ImageMagick.*` w pozostałych projektach).
2. Proces: `Lemat.ImageSandbox` (Kestrel na gnieździe Unix
   `/run/lemat/obrazy.sock`, bez referencji do kodu Lemat) działa jako
   sidecar 1:1 z każdą repliką `api` (`obrazy-api`, 2 równoległe operacje,
   limit kontenera 1 vCPU / 1 GiB) i `worker` (`obrazy-worker`, 4 operacje,
   2 vCPU / 2 GiB); `network_mode: none`, `read_only: true`, użytkownik bez
   uprawnień, `pids_limit: 64`.
3. Limity na operację: `ResourceLimits.Memory = 512 MiB`,
   `ResourceLimits.Area = 40 000 000`, `ResourceLimits.Width =
   ResourceLimits.Height = 12 000`, `ResourceLimits.Time = 5 s`; przed
   dekodowaniem `MagickImageInfo` (odczyt nagłówka) sprawdza format z listy
   (JPEG, PNG, HEIC, WEBP — inne → `422 ZDJECIE_NIEPRAWIDLOWE`) i wymiary;
   polityka ImageMagick (`policy.xml` w obrazie) wyłącza wszystkie
   kodery poza tymi czterema i JPEG zapisu. Przekroczenie 8 s czasu
   ściennego: klient przerywa żądanie, a sidecar kończy proces
   (`Environment.FailFast`), który restartuje środowisko kontenerów
   (`restart: always`); przekroczenie limitu cgroup kończy kontener tak
   samo.
4. Kontrakt (HTTP/1.1 na gnieździe Unix): `POST /v1/normalizacja`
   (`application/octet-stream` → JPEG + nagłówki `X-Szerokosc`,
   `X-Wysokosc`, `X-Sha256`), `POST /v1/maskowanie` (JPEG + JSON ramek →
   JPEG), `POST /v1/wycinki` (JPEG + JSON obszarów → `multipart/mixed` z
   wycinkami i zdjęciem poglądowym 1024 px), `GET /zdrowie`. Klient
   `ImageSandboxKlient` (`Lemat.Infrastructure.Piaskownice`) z semaforem
   równoległości równym liczbie slotów sidecara; oczekiwanie na slot
   > 5 s → `503` w `api` (ADR-045).
5. Normalizacja JPEG: jakość 85, próbkowanie 4:2:0, profil sRGB, bez
   metadanych (`image.Strip()` po `image.AutoOrient()`).
6. Kompresja w przeglądarce (ADR-054) zamienia HEIC na JPEG, gdy przeglądarka
   dekoduje HEIC; dekodowanie HEIC po stronie serwera jest ścieżką
   rezerwową z metryką `zdjecia_heic_po_stronie_serwera_total` i
   przełącznikiem `LEMAT_HEIC_PO_STRONIE_SERWERA` (domyślnie `true`; R-24).

## Alternatywy

- **ImageSharp (SixLabors)** — odrzucona: brak dekodera HEIC; licencja
  Six Labors Split License wymaga licencji komercyjnej powyżej progu
  przychodu organizacji.
- **SkiaSharp** — odrzucona: brak HEIC w standardowych pakietach, natywny
  kod i tak wymaga izolacji.
- **NetVips (libvips)** — odrzucona: prekompilowane biblioteki libvips w
  pakietach NuGet nie zawierają dekodera HEVC (libheif tylko z AVIF), więc
  HEIC z iPhone'ów nie byłby obsługiwany.
- **Sidecar Python z Pillow + pillow-heif (jak w 2.4)** — odrzucona:
  dokłada drugi komponent pythonowy bez potrzeby, skoro Magick.NET
  pokrywa wszystkie formaty; SymPy (ADR-050) pozostaje jedynym wyjątkiem.
- **Magick.NET w procesie `api`/`worker` tylko z `ResourceLimits`** —
  odrzucona: limity ImageMagick nie chronią przed błędami pamięci w
  dekoderach natywnych; awaria zabiłaby proces obsługujący żądania lub
  prace w tle.

## Konsekwencje

- Obsługiwane wszystkie formaty 12.5, w tym HEIC; dekodery natywne
  odizolowane od procesów domenowych.
- Dodatkowe 4 kontenery w pilotażu (2 × `obrazy-api`, 2 × `obrazy-worker`)
  z tego samego obrazu `lemat-backend`; wykorzystanie ≈ 9% w szczycie
  (17.4, założenie do pomiaru w G-3).
- Ryzyko licencyjne HEVC (R-24) ograniczone przez konwersję w przeglądarce
  i przełącznik.
