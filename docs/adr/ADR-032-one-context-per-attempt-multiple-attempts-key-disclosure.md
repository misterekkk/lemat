# ADR-032: Jeden kontekst na próbę, wielokrotne próby i ujawnianie klucza po terminie

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 2).

## Kontekst

Odpowiedź bez kontekstu pozwalała powtarzać Zadanie zamknięte i zawyżać Profil; brak unikalności w Przypisaniach; klucz ujawniany od razu w Przypisaniu (C1-21).

## Decyzja

Każda Odpowiedź i każde Rozwiązanie ma dokładnie jeden kontekst: element Ścieżki nauki albo pozycję Przypisania (CHECK w DDL, Test diagnostyczny osobno); indeksy unikalne per (Uczeń, Przypisanie, Zadanie); kolejna próba tego samego Zadania w 60 dniach ma wagę × 0,5 (ADR-018); w Przypisaniu wynik i klucz Odpowiedzi są ujawniane od terminu (`rezultat_ujawniony_od`). Rozwiązania z listy Przypisań nie tworzą elementów Ścieżki nauki (Ścieżka zawiera wyłącznie propozycje agenta — PZS-1 B-3).

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

Brak duplikatów Weryfikacji dla Nauczyciela; Profil odporny na powtarzanie.

## Uzupełnienia

- **Uzupełnienie (it. 4, wersja 2.2, C1-21):** element Ścieżki nauki z reguły `R-PRZYPISANIE` był obejściem niezmiennika (próba miała tylko `element_sciezki_id`). Decyzja: klient podaje jeden kontekst, ale **kontekst efektywny** próby z elementu `R-PRZYPISANIE` zawiera także `przypisanie_id` elementu — dziedziczone w bazie przez `trg_kontekst_proby` (para dozwolona wyłącznie przy `element.przypisanie_id = przypisanie_id`). Skutki: `ux_odpowiedz_przypisanie`/`ux_rozwiazanie_przypisanie` obejmują obie drogi (C-30); `rezultat_ujawniony_od = greatest(now(), termin)` także z elementu; przydział 11.4.1 p. 1 kieruje Weryfikację do Nauczyciela Przypisania; próba liczy się do `PostepPrzypisaniaWyjscie`; CHECK w 7.3 jawnie wylicza kombinacje {E}, {P}, {E, P}, {T}. Alternatywa „rezygnacja z elementu `R-PRZYPISANIE` i kierowanie Ucznia na listę Przypisań” — odrzucona, bo Przypisania przestałyby być częścią Ścieżki nauki proponowanej przez agenta (PZS-1 B-3) i rekomendacja nie mogłaby wybierać, kiedy je zaproponować.
