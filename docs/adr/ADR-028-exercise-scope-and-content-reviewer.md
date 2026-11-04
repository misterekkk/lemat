# ADR-028: Zasięg Zadań i recenzent treści

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2), G-0 |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2), G-0.

## Kontekst

W wersji 1 każdy Nauczyciel mógł poprawić lub wycofać dowolne Zadanie (także z Arkusza CKE), a Zadania Nauczycieli trafiały do globalnego doboru bez przeglądu (C1-22).

## Decyzja

`zadanie.zasieg` (`GLOBALNY`/`PRYWATNY`); Zadania `BAZOWE_NAUCZYCIEL` są `PRYWATNY` (tylko Przypisania autora), dopóki recenzent treści lub Administrator nie zatwierdzi ich do `GLOBALNY`; flaga `nauczyciel.recenzent_tresci`; uprawnienia 8.4; endpointy Przeglądu i wycofania dla Administratora.

## Alternatywy

Wszystkie Zadania globalne z obowiązkowym przeglądem — odrzucone (blokuje Nauczyciela, który chce użyć własnego zadania w swojej Klasie od razu).

## Konsekwencje

Zmiany treści wpływające na wszystkich Uczniów wykonują tylko osoby uprawnione; `POST /zadania` tylko dla roli Nauczyciel (Administrator importuje Arkusze CKE), co zachowuje definicję Zadania bazowego.

## Uzupełnienia

- **Uzupełnienie (it. 4, wersja 2.2, C2-03):** ograniczenie bezpośredniej poprawki Zadań `GLOBALNY` do recenzentów treści zawęża FR-14 („Nauczyciel może przejrzeć, poprawić lub odrzucić”), więc decyzja jest oznaczona G-0 (D-11); FR-14 dla Zadań zleconych przez Nauczyciela jest spełnione w pełni przez ADR-038, a dla Zadań `GLOBALNY` — przez Zgłoszenie błędu w Zadaniu.
