# ADR-037: Wersja treści Weryfikacji oddzielona od blokady

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 4) |
| Data decyzji | 2026-11-01 (iteracja 4, wersja 2.2) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 4).

## Kontekst

Podjęcie, ponowne podjęcie, przedłużenie (co 5 min) i `ZWOLNIJ_BLOKADY` podnosiły `weryfikacja.version`, a odpowiedź przedłużenia nie zwracała ETag; rozstrzygnięcie Weryfikacji trwającej > 5 min kończyło się `412`, a sygnał „pojawiło się zgłoszenie” tracił znaczenie (C2-02).

## Decyzja

`weryfikacja.version` = wersja **treści**; zmienia ją wyłącznie trigger `trg_weryfikacja_wersja` przy zmianie powodów, zgłoszenia, Oceny weryfikowanej, `ponowna_ocena_at`, Oceny Nauczyciela, anulowania lub zamknięcia; operacje blokady i trasowania (eskalacja, `termin_at`, priorytet po 80 dniach) oraz `opinia_pomocnicza` jej nie zmieniają. Przedłużenie zwraca `{zablokowana_do, version}` i ETag; `412` przy rozstrzygnięciu zawiera `zmiany_tresci`. Scenariusz C-31.

## Alternatywy

Zwracanie nowego ETag po każdym przedłużeniu i aktualizacja w PWA — odrzucona (wyścig między odpowiedzią przedłużenia a wysyłką rozstrzygnięcia z drugiej karty nadal daje fałszywe `412`, a `412` nie odróżnia przyczyny); osobna kolumna `wersja_blokady` z `If-Match` na obie — odrzucona (blokadę i tak chroni warunek `podjeta_przez = ja AND zablokowana_do > now()`).

## Konsekwencje

`412` zawsze oznacza zmianę, którą Nauczyciel musi zobaczyć (C-28); C-23 opiera się na warunku `status = 'OCZEKUJE'`; konwencja 7.2 ma jawny wyjątek.
