# ADR-031: Pseudonimizacja i maskowanie zdjęć w kolejce globalnej

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2), G-0; realizacja techniczna zaktualizowana przez ADR-048 (wersja 3.0) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2), G-0; realizacja techniczna zaktualizowana przez ADR-048 (wersja 3.0).

**Realizacja w stosie .NET 10 + Next.js (wersja 3.0).** Decyzja obowiązuje. Maskowanie zdjęć wykonuje `Lemat.ImageSandbox` (Magick.NET) zamiast Pillow; koszt ≈ 50 ms na zdjęcie pozostaje założeniem do pomiaru w G-3 — ADR-052.

## Kontekst

W wersji 1 Nauczyciel spoza Klasy widział imię, nazwisko i niezamaskowane zdjęcia Ucznia (C1-02).

## Decyzja

11.4.4: pseudonim HMAC, brak Klasy i innych Rozwiązań, zdjęcia maskowane na podstawie ramek danych osobowych z Agenta rozpoznającego; informacja przy rejestracji; umowy poufności weryfikatorów (16.3).

## Alternatywy

Brak kolejki globalnej — odrzucone (FR-24…FR-26 dla Uczniów bez Klasy). Twierdzenie wersji 2.1, że anonimizacja Transkrypcji jest zbędna, bo „Transkrypcja zawiera wyłącznie rozwiązanie”, było niczym niezagwarantowane (C2-10) — zastąpione Filtrem danych osobowych (ADR-042).

## Konsekwencje

Minimalizacja danych zgodna z NFR-09; koszt: jedno maskowanie zdjęcia (Pillow, ~50 ms) na przesłanie.
