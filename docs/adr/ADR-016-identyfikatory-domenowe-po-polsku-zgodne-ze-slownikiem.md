# ADR-016: Identyfikatory domenowe po polsku, zgodne ze słownikiem

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2); realizacja techniczna zaktualizowana przez ADR-048 (wersja 3.0) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2); realizacja techniczna zaktualizowana przez ADR-048 (wersja 3.0).

**Realizacja w stosie .NET 10 + Next.js (wersja 3.0).** Decyzja obowiązuje. Reguły transliteracji i form identyfikatorów dla C#, PostgreSQL, JSON, URL i TypeScript oraz nazwy projektów i przestrzeni nazw .NET rozszerza ADR-049.

## Kontekst

Wymóg zgodności ze słownikiem pojęć (Z-6); homonimy w wersji 1 (C1-03, C1-04).

## Decyzja

Reguły 3.1 (w tym „jedno słowo — jedno znaczenie” i lista dopuszczonych terminów technicznych); zmiany nazw: `DecyzjaSkladowa`, `RezultatPytan`, `RezultatOceny`, `RezultatRozpoznawania`, `Rownowaznosc`, `rezultat_walidacji`, `procent_grupy_eksperymentu`, `nr_kryterium`, `wymaga_nr_kryterium`, `wynik_koncowy_wzorcowy`, `typ_kolejki_weryfikacji`, `decyzja_modelu.wybor`; propozycja zmiany słownika PZS-1 (3.4).

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

Brak tłumaczeń między warstwami; zmiany słownika wymagają akceptacji (G-0).

## Uzupełnienia

- **Uzupełnienie (it. 6, wersja 2.4, C4-04):** „zdjęcie” = wyłącznie fotografia kartki; wyłączenie bezpiecznika = „dezaktywacja” (`POST /admin/bezpieczniki/dezaktywacja`, `dezaktywowany_przez`, `dezaktywowano_at`, `uzasadnienie_dezaktywacji`), odebranie flagi = „odebranie”, wyłączenie flagi danych = „wyłączenie”; „wstrzymanie” ma jedno znaczenie (3.1 p. 5); test CI, część 3, pilnuje tokenów `zdje*` i `wstrzym*` z listami dozwolonych użyć. Odrzucona alternatywa: „wyłączenie bezpiecznika” — słowo „wyłączenie” jest już używane dla flag danych i reguł 11.5.2 („wyłączenie powodów nielosowych”), więc łamałoby 3.1 p. 5 w ten sam sposób.

- **Uzupełnienie (pętla 3.x, it. 1, wersja 3.0, C5-04):** „blokada” ma dwa znaczenia domenowe z obowiązkowym kwalifikatorem (Blokada weryfikacji, Blokada obniżania progu); dzierżawa pracy w tle to `praca_w_tle.dzierzawa_do` (w 2.4 `zablokowana_do`); `blokad*` i `zablokow*` są tokenami zarezerwowanymi testu słownika z listą dozwolonych użyć (3.4); `stan_bezpiecznika.aktywny` oznacza wyłącznie stan aktywny bezpiecznika, a skutki wyprowadzają właściwości `BlokadaObnizaniaProgu` i `KazdaPoprawkaDoWeryfikacji`. Formy identyfikatorów w stosie .NET — ADR-049. Odrzucona alternatywa: osobna kolumna `blokada_obnizania_progu` obok `aktywny` — dwie kolumny o tej samej wartości dla typu `TRAFNOSC` i martwa kolumna dla `POPRAWKI`.
