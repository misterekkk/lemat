# ADR-038: Przegląd Zadań zleconych przez Nauczyciela i Zgłoszenia błędu w Zadaniu

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 4), G-0 |
| Data decyzji | 2026-11-01 (iteracja 4, wersja 2.2) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 4), G-0.

## Kontekst

Zadania wygenerowane (obliczeniowe i zamknięte), także zlecone przez Nauczyciela, po Walidacji trafiały od razu do `OPUBLIKOWANE`/`GLOBALNY`, a poprawić je mogli tylko recenzent treści i Administrator; FR-14 przyznaje to prawo Nauczycielowi; zawężenie nie było w G-0 (C2-03).

## Decyzja

(1) Zadanie wygenerowane na zlecenie Nauczyciela zawsze przechodzi `WALIDACJA → DO_PRZEGLADU`; zlecający przegląda, poprawia albo odrzuca (FR-14 w pełni), wynik `PRYWATNY`; promocja do `GLOBALNY` — recenzent treści lub Administrator; po 14 dniach Zadanie trafia także do kolejki recenzentów; (2) Zadania `GLOBALNY`: każdy Nauczyciel składa **Zgłoszenie błędu w Zadaniu** (`zgloszenie_bledu_zadania`, `POST /zadania/{id}/zgloszenia-bledu`), rozpatrywane przez recenzentów i Administratorów z SLA 5 dni roboczych; 3 otwarte zgłoszenia od różnych Nauczycieli tworzą wiersz `wstrzymanie_w_doborze` (od wersji 2.3 poza `zadanie.version`, C3-09); (3) zlecenia Administratora i systemu — bez zmian (ADR-015).

## Alternatywy

Bezpośrednia poprawka Zadania `GLOBALNY` przez każdego Nauczyciela — odrzucona jako opcja domyślna (jedna osoba zmienia treść dla wszystkich Uczniów bez przeglądu; nowa wersja Zadania wpływa na kalibrację Poziomu trudności); prywatna kopia Zadania `GLOBALNY` do poprawy — odrzucona (kopia nie jest Zadaniem bazowym ani wygenerowanym w rozumieniu słownika).

## Konsekwencje

Nauczyciel ma pełne FR-14 dla Zadań, które sam zlecił, i ścieżkę zgłoszenia dla pozostałych; pula `GLOBALNY` rośnie z zleceń Nauczycieli wolniej (wymaga promocji). Zawężenie dla Zadań `GLOBALNY` — decyzja D-11 (G-0).
