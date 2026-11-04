# ADR-027: Zgodność Oceny = punkty + pierwszy błędny krok + brakujący krok

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2), G-0 |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2), G-0.

## Kontekst

Słownik: Ocena obejmuje wskazanie błędnego kroku, a Pewność oceny to prawdopodobieństwo, że Ocena jest poprawna; wersja 1 kalibrowała tylko sumę punktów (C1-16).

## Decyzja

`probka_kalibracyjna.zgodna` = równe punkty ∧ równy `pierwszy_bledny_krok` (lub oba brak) ∧ równe `brakujacy_krok`. Typ błędu jest mierzony osobno (`zgodny_typ_bledu`), bo jest klasyfikacją pomocniczą dla Wskazówki, a nie elementem definicji Oceny w słowniku. Ta sama definicja służy NFR-02 (PZS-1 B-5).

## Alternatywy

Tylko punkty — odrzucone (Ocena z dobrą sumą i złym krokiem byłaby „poprawna”); punkty + krok + typ — odrzucone (zaniżałoby zgodność z powodu rozbieżności klasyfikacji, które nie zmieniają Oceny).

## Konsekwencje

NFR-02 i NFR-04 są trudniejsze do spełnienia, ale zgodne ze słownikiem; ZT-1 musi mieć oznaczony pierwszy błędny krok przez Nauczycieli.
