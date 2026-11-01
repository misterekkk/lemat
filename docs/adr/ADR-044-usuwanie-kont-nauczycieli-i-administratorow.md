# ADR-044: Usuwanie kont Nauczycieli i Administratorów

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 4) |
| Data decyzji | 2026-11-01 (iteracja 4, wersja 2.2) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 4).

## Kontekst

Brak przepływu i endpointu usunięcia konta Nauczyciela, a klucze obce `RESTRICT` blokowały `DELETE FROM uzytkownik`; dane Nauczyciela są danymi osobowymi (C2-14).

## Decyzja

`DELETE /nauczyciele/ja`, `POST /admin/uzytkownicy/{id}/usuniecie` (ochrona ostatniego Administratora); okres karencji 30 dni; natychmiastowe zwolnienie blokad, odebranie flag (od wersji 2.3 odroczone do `USUN_KONTO`), eskalacja Weryfikacji `KLASA`; Przekazanie Klasy przez Administratora; po karencji archiwizacja nieprzekazanych Klas i zamknięcie ich Przypisań, `WYCOFANE` dla nieprzekazanych Zadań `PRYWATNY`, pseudonimizacja uzasadnień `R-PRZYPISANIE`, klucze obce `ON DELETE SET NULL` (DDL 7.3), Oceny Nauczyciela bez wskazania osoby.

## Alternatywy

Wyłączenie z zakresu (NFR-09 wymienia Uczniów) — odrzucone (RODO dotyczy wszystkich osób); usuwanie miękkie z zachowaniem danych — odrzucone (brak podstawy przetwarzania po ustaniu umowy).

## Konsekwencje

Uczniowie nie tracą historii Ocen; Klasa bez Nauczyciela istnieje tylko jako zarchiwizowana (CHECK w DDL).

## Uzupełnienia

- **Uzupełnienie (it. 5, wersja 2.3, C3-08):** natychmiastowe skutki karencji ograniczone do zwolnienia blokad (szkice zachowane) i eskalacji z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA`; flagi odbiera dopiero `USUN_KONTO`; przywrócenie cofa eskalacje oczekujące, Klasy przekazane pozostają u nowego Nauczyciela (D-15).
