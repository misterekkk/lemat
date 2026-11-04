# ADR-035: Kopie treningowe z rejestrem powiązań w bazie

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 3), G-0 |
| Data decyzji | 2026-11-01 (iteracja 3, wersja 2.1) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 3), G-0.

## Kontekst

16.4 (wersja 2.0) przewidywało kopie zdjęć do zbioru treningowego „z pseudonimem zamiast `uczen_id`” i usunięcie po odwołaniu zgody, ale nie było żadnego powiązania kopii z kontem — odwołania zgody ani usunięcia konta nie dało się wykonać (uzupełnienie C1-12).

## Decyzja

Tabela `kopia_treningowa` (7.3) jako jedyne powiązanie kopii z Uczniem; kopia zawiera zdjęcie maskowane, Transkrypcję i Ocenę ostateczną; tworzy ją `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` przy przejściu do `OCENIONE` (przed upływem `usun_po` zdjęcia); odwołanie zgody (`PATCH /uczniowie/ja`) i `USUN_KONTO` (krok 4a) ustawiają usunięcie; `ON DELETE RESTRICT` wymusza kolejność kroków.

## Alternatywy

Pseudonim bez powiązania (anonimizacja) — odrzucony, bo zgoda jest odwołalna (art. 7 ust. 3 RODO) i musi dać się wykonać; kopie oryginałów zdjęć — odrzucone (minimalizacja: oryginał może zawierać imię i nazwisko).

## Konsekwencje

Prawo do odwołania zgody i do usunięcia jest wykonalne w ≤ 1 h; zbiór treningowy nie zawiera danych identyfikacyjnych (od wersji 2.2 gwarantuje to Filtr danych osobowych, ADR-042).

## Uzupełnienia

- **Uzupełnienie (it. 4, wersja 2.2, C2-16):** kopia jest zlecana przy każdym przejściu do stanu „`OCENIONE` bez otwartej Weryfikacji” (także po zamknięciu Kontroli losowej lub zgłoszenia) i nadpisywana, gdy Ocena aktualna jest nowsza niż zapisana (`kopia_treningowa.ocena_id`).
