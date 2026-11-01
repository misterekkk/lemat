# ADR-046: Bezpieczniki trafności i poprawek reagujące na dowód przekroczenia

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 5) |
| Data decyzji | 2026-11-01 (iteracja 5, wersja 2.3) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 5).

## Kontekst

Reguła „`n ≥ 50` i UB Wilsona > 2%” z wersji 2.2 była spełniona z konstrukcji dla 50 ≤ n < 189 przy zerze błędów (UB = z²/(n + z²)), więc na starcie produkcji i po awariach Jev podnosiła próg do 0,990, a bezpiecznik poprawek kierował każdą Poprawkę do Weryfikacji — sprzecznie z minimum `n_eff ≥ 189` z 11.5.1 i z modelem obciążenia 17.7 (C3-01).

## Decyzja

Wyzwolenie wyłącznie przy dowodzie przekroczenia: dolna granica Wilsona 95% błędu (wagi IPW) > 2% przy `n_eff ≥ 50`; próba = Kontrole losowe z 30 dni dla pary (konfiguracja, rodzaj) z pewnością (bieżący kalibrator) powyżej bieżącego progu — nie dla rekordu progu; UB > 2% przy `n_eff ≥ 189` tylko blokuje obniżanie progu; stan w tabeli `stan_bezpiecznika` (zastępuje flagę środowiskową), dezaktywacja bezpiecznika poprawek przez Administratora (nazwa od wersji 2.4; w 2.3 „zdjęcie”) ustawia `okno_od`; testy jednostkowe dla n = 50 … 188 bez błędów i dla progów `k_min` (11.5.3).

## Alternatywy

SPRT (H0: e ≤ 1%, H1: e ≥ 3%, α = β = 0,05) — odrzucony jako opcja domyślna: wymaga stanu sekwencyjnego per para i resetu po każdej zmianie progu (powrót problemu zapadki), a przy oknie kroczącym traci własności testu; samo minimum `n_eff ≥ 189` przed UB — odrzucone: przy e = 0,5% UB > 2% utrzymuje się do n ≈ 335 (11.5.1, tabela 1), więc w przedziale 189–335 bezpiecznik nadal wyzwalałby się bez dowodu.

## Konsekwencje

Przy e ≤ 0,5% fałszywe wyzwolenie w 90 dniach ≤ 0,2%, przy e = 1% ≤ 2,2% (symulacja 11.5.3); model z błędem 4% od początku okna wykrywany w 2–4 dni (model zapasowy: 12 dni), nagła degradacja 0,5% → 4% w fazie ustalonej — w medianie 13 dni (doprecyzowanie wersji 2.4: „wykrywanie” oznacza pierwsze podniesienie progu, a skuteczną reakcję gwarantuje Blokada obniżania progu z uzupełnienia it. 6); błąd 2–3% nie jest szybko wykrywany przez bezpiecznik — chroni przed nim reguła wyboru progu (UB ≤ 2% przy `n_eff ≥ 189`) i blokada obniżania; model 17.7 i G-7 = 18 pozostają ważne (scenariusz startowy 17.7).

## Uzupełnienia

- **Uzupełnienie (it. 6, wersja 2.4, C4-02, C4-04, C4-06 p. 5):** jawna kolejność dnia KALIBRUJ (kroki 1–6, 11.5.3): bezpieczniki oceniane na progu i kalibratorze sprzed przebiegu, wynik `p_nowy = max(p_dol, p0 + 0,02)` przy wyzwoleniu, jeden rekord `prog_pewnosci`; Blokada obniżania progu (`stan_bezpiecznika.aktywny` dla `TRAFNOSC`) od wyzwolenia do UB ≤ 2% przy `n_eff ≥ 189` powyżej bieżącego progu; obniżenie tylko po teście 30 dni powyżej progu docelowego (LB ≤ 2% i UB ≤ 2% przy `n_eff ≥ 189`); wyzwolenie przy 0,990 → tryb pełnej weryfikacji (1.000) z wyjściem przez dezaktywację (D-16); symulacja dynamiki (próg niemalejący w 100% przebiegów z degradacją, 61% przebiegów z degradacją jednorodną kończy w trybie pełnej weryfikacji w 30 dni); rozmiar testu przy e = 2% nazwany wprost (2,6–7,8% na ocenę, 1,1 fałszywego podniesienia na kwartał); „zdjęcie” → „dezaktywacja” dla obu typów. Odrzucone alternatywy: okno 7 dni obok 30 dni (wykrycie 7 zamiast 13 dni, ale 2,0 fałszywego podniesienia na kwartał i 28,5% kwartałów w trybie pełnej weryfikacji przy e = 2%); zatrzymanie na 0,990 bez trybu pełnej weryfikacji — przy degradacji jednorodnej błąd ocen automatycznych pozostaje > 2% bez końca (naruszenie NFR-03).

- **Uzupełnienie (pętla 3.x, it. 1, wersja 3.0, C5-02):** reguły nadzoru (`wykonanie_kontroli_30d`, bezpiecznik braku nadzoru) liczą wyłącznie Kontrole **wymagalne** (utworzone w oknie 30 dni z `termin_at ≤ now()`), a `π_wykonanie` — Kontrole zakończone albo wymagalne; pusta populacja (`null`, np. tryb pełnej weryfikacji bez Ocen automatycznych) oznacza brak reguły nadzoru, więc nie blokuje obniżania progu po dezaktywacji i nie wyzwala bezpiecznika braku nadzoru przy wyjściu z trybu pełnej weryfikacji; testy (l) i (m) w 11.5.3. Odrzucona alternatywa: `null` traktowane zachowawczo jako „zablokowane” — utrwala tryb pełnej weryfikacji (5 194 min/dobę, 17.7) mimo decyzji Administratora.
