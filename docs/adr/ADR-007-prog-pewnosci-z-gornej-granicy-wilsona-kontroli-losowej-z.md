# ADR-007: Próg pewności z górnej granicy Wilsona, Kontroli losowej z wagami IPW i analizy mocy

| Pole | Wartość |
|---|---|
| Status | Zmieniona (it. 2) |
| Data decyzji | 2026-10-31 (iteracja 2, wersja 2.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zmieniona (it. 2).

## Kontekst

NFR-03 wymaga mierzenia błędów ocen automatycznych, które z definicji nie trafiają do Nauczyciela; ZT-1 w wersji 1 był za mały (C1-09); Kontrola mogła nie być wykonywana (C1-10).

## Decyzja

Kontrola losowa 20% → 5% (próg 400 próbek), termin 3 dni robocze (wersja 2.1: 72 h); wybór progu 11.5.3 z `n_eff ≥ 189`, kalibracją out-of-fold (5-krotna CV) i wagami IPW skorygowanymi o odsetek wykonania Kontroli; reguły nadzoru (blokada obniżania progu, bezpiecznik braku nadzoru); ZT-1 ≥ 700 obliczeniowych z analizy mocy (11.5.1); tryb pełnej weryfikacji, gdy ZT-1 nie daje progu (G-0).

## Alternatywy

Stały próg 0.9 (niemierzalny NFR-03); kalibracja tylko na Weryfikacjach z niskiej pewności (obciążenie selekcją); traktowanie niewykonanych Kontroli jako błędów (zbyt konserwatywne przy 90% wykonania — zawyża błąd o do 10 pp).

## Konsekwencje

Obciążenie Kontrolą wliczone w model 17.7 (6% na starcie, 3% później); brak nadzoru zawsze podnosi próg, nigdy go nie obniża.

## Uzupełnienia

- **Uzupełnienie (it. 4, wersja 2.2, C2-05, C2-06):** źródło Próbki kalibracyjnej wynika z `weryfikacja.powody_poczatkowe` (powody dopisane później nie wykluczają próbki — usunięcie nielosowych braków); analiza mocy G-1(a) per rodzaj: 700 obliczeniowych z LB ≥ 87,5% i 350 dowodów z LB ≥ 85,0% (D-12); Transkrypcje z Poprawkami mają podwójny odsetek Kontroli losowej i własny bezpiecznik (11.5.2).

- **Uzupełnienie (it. 5, wersja 2.3, C3-01):** bezpieczniki trafności i poprawek reagują na dowód przekroczenia (LB Wilsona 95% > 2% przy `n_eff ≥ 50`), a nie na brak dowodu spełnienia; próba liczona dla pary (konfiguracja, rodzaj) powyżej bieżącego progu, nie dla rekordu progu — ADR-046.
