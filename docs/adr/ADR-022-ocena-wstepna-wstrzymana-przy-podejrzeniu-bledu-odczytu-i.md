# ADR-022: Ocena wstępna wstrzymana przy podejrzeniu błędu odczytu i zgłoszenie Ucznia

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2), G-0 |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2), G-0.

## Kontekst

FR-28 („błędy odczytu nie obniżają oceny”) vs. FR-24 („uczeń widzi ocenę wstępną”); w wersji 1 podejrzany błąd odczytu zerował punkty za wynik i trafiał do Profilu (C1-01).

## Decyzja

(1) moda Q-OC-04 = `BLAD_ODCZYTU` nie jest błędem Ucznia: nie zeruje punktów (FR-20), nie ustala `typ_bledu`; (2) Ocena wstępna z `mozliwy_blad_odczytu` jest pokazywana Uczniowi jako „wstrzymana”: status, krok podejrzany, komunikat — bez punktów, typu błędu i Wskazówki; (3) taka Ocena nie tworzy Wyniku zadania; (4) Uczeń może zgłosić błąd odczytu do 14 dni po Ocenie (11.6), co tworzy Weryfikację z priorytetem 2.

## Alternatywy

„ocena w najlepszej wierze” (maksimum punktów dla kryteriów, które mogły być dotknięte błędem) — odrzucona, bo pokazuje Uczniowi zawyżone punkty, które Nauczyciel potem obniża (gorsze doświadczenie niż wstrzymanie); pokazanie punktów modelu — odrzucone (łamie FR-28).

## Konsekwencje

W tym jednym przypadku FR-28 ma pierwszeństwo przed literalnym brzmieniem FR-24 (Uczeń widzi Ocenę wstępną bez punktów); wymaga akceptacji w G-0. Skuteczność mierzona w 11.8.

## Uzupełnienia

- **Uzupełnienie (it. 3, wersja 2.1):** (5) zgłoszenie błędu odczytu **zawiesza** Wynik zadania zgłoszonej Oceny do rozstrzygnięcia (unieważnienie z powodem `ZGLOSZENIE_BLEDU_ODCZYTU`, odtworzenie Profilu, 9.12), więc niezauważony przez Ucznia i przez T-3 błąd odczytu nie obniża Profilu także w czasie Weryfikacji; (6) po usunięciu zdjęć przed rozstrzygnięciem kroki podejrzane są oceniane na korzyść Ucznia, a odstępstwo wymaga komentarza (11.4.3 p. 3). Alternatywa „Wynik w mocy do rozstrzygnięcia” (wersja 2.0) — odrzucona, bo przez ≤ 72 h (SLA) Profil i rekomendacje opierałyby się na Ocenie, którą Uczeń zakwestionował z powodu odczytu.
