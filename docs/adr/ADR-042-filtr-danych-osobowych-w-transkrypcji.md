# ADR-042: Filtr danych osobowych w Transkrypcji

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 4) |
| Data decyzji | 2026-11-01 (iteracja 4, wersja 2.2) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 4).

## Kontekst

Agent rozpoznający oznaczał ramki danych osobowych tylko do maskowania zdjęcia; nic nie zabraniało przepisania imienia i nazwiska do kroków, skąd trafiały do stanu Jev, promptów, obcego weryfikatora i kopii treningowej (C2-10).

## Decyzja

Reguła promptu „nie przepisuj obszarów danych osobowych”; deterministyczny filtr w Lemat przed zapisem każdej Transkrypcji i przed kopią treningową: usunięcie kroków pokrywających się z ramkami w ≥ 50%, zastąpienie imienia, nazwiska i części lokalnej e-maila z konta (porównanie po stronie Lemat, odległość Levenshteina ≤ 1 na rdzeniu ≥ 4 znaków) oraz PESEL i e-maili znacznikiem `[DANE_OSOBOWE]`; metryka i test G-2/G-6 (0 wystąpień na ≥ 50 kartkach z danymi).

## Alternatywy

Drugi model wykrywający dane osobowe w tekście — odrzucony (koszt, opóźnienie fazy R, niedeterministyczność); poleganie wyłącznie na prompcie — odrzucone (brak gwarancji).

## Konsekwencje

Deklaracja 16.3 jest egzekwowana kodem; ryzyko pozostaje dla danych osób trzecich niewystępujących na koncie (np. imię kolegi) — pokrywa je reguła promptu i ramki maskowania.

## Uzupełnienia

- **Uzupełnienie (it. 5, wersja 2.3, C3-02):** filtr nie usuwa kroków i nie zmienia `latex`; krok w ramce danych osobowych bez treści matematycznej jest maskowany w całości (`tylko_dane_osobowe`), z treścią matematyczną — zachowany z `konflikt_filtra` → T-3 (Z-1); dopasowanie nazw tylko w `tekst`, z wykluczeniem Słownika terminów matematycznych; nazwy < 4 znaków — dopasowanie dokładne; pozycje zastąpień w `zastapienia_danych_osobowych` i przywracanie słowa Poprawką (dopasowania przybliżone nie są ponownie filtrowane); G-2: 150 kartek, 0 zastąpień terminów matematycznych, ≤ 0,5% fałszywych zastąpień. Odrzucona alternatywa: usuwanie kroku z możliwością przywrócenia — usunięcie przed zatwierdzeniem zmienia Ocenę bez Weryfikacji, gdy Uczeń nie zauważy braku.

- **Uzupełnienie (it. 6, wersja 2.4, C4-03):** w regule (a) tokeny imienia i nazwiska z konta, inicjały i oznaczenie klasy nie są treścią matematyczną, więc linia „Jan Pole” jest krokiem `tylko_dane_osobowe`, a nie `konflikt_filtra`; w regule (b) token Słownika równy nazwisku jest zastępowany przy sąsiedztwie z drugim członem imienia i nazwiska lub inicjałem (b1) albo w kroku agenta z podejrzeniem danych osobowych (b2); na Poprawkach reguła (a) nie jest liczona ponownie — krok przywrócony z `tylko_dane_osobowe` nie jest maskowany, podlega limitom 9.4, Sprawdzeniu zgodności poprawek i T-7; `konflikt_filtra` liczony wyłącznie z kroków niezmienionych; G-2: osobne kryterium dla kartek z nazwiskiem-terminem i metryka `uczniowie_z_konfliktem_filtra_udzial`. Odrzucona alternatywa: wyłączenie nazwisk-terminów ze Słownika dla konkretnego Ucznia (lista per konto) — zastępowałoby „Pole trójkąta” w każdym kroku tego Ucznia i zmieniało treść matematyczną w `tekst` poza ramką. Ryzyko resztkowe: samo nazwisko-termin bez imienia i poza ramką danych osobowych (np. podpis „Pole” niewykryty jako ramka) nie jest zastępowane — chroni je reguła promptu (R-19).

- **Uzupełnienie (pętla 3.x, it. 1, wersja 3.0, C5-01):** reguła (b1) zastępuje token Słownika terminów matematycznych równy nazwisku wyłącznie przy sąsiedztwie z **pełnym** drugim członem imienia i nazwiska (≥ 2 litery, dopasowanie `DOKLADNE`); sąsiedztwo z inicjałem (b1-i) wymaga kropki po pojedynczej literze w oryginalnym tekście i działa tylko w kroku bez treści matematycznej, a w kroku z treścią matematyczną ustawia `konflikt_filtra` (T-3) zamiast zastępować; pojedyncza litera bez kropki nie jest inicjałem („Pole P trójkąta”, „prosta k”). Token wpisany przez Ucznia w miejscu znacznika `[DANE_OSOBOWE]` nie jest zastępowany ponownie żadną regułą b. G-2: „0 zastąpień tokenów Słownika w krokach z treścią matematyczną” (z osobno raportowanym wyjątkiem b1) i celowy zestaw ≥ 20 kartek „nazwisko-termin + oznaczenie równe inicjałowi”. Odrzucona alternatywa: rezygnacja z inicjałów w (b1) — linia podpisu „J. Pole” poza ramką danych osobowych zostawiałaby nazwisko w Transkrypcji; wymóg kropki i kroku bez treści matematycznej zachowuje ten przypadek bez fałszywych zastąpień w treści matematycznej.
