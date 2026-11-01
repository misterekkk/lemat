# ADR-036: Poprawki Transkrypcji ograniczone i sprawdzane ze zdjęciem

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 4), G-0 |
| Data decyzji | 2026-11-01 (iteracja 4, wersja 2.2) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 4), G-0.

## Kontekst

`PUT …/transkrypcja` przyjmował dowolną listę 1–40 kroków, a kroki zmienione były wyłączone z podejrzenia odczytu (11.1 p. 7c); Uczeń mógł sfotografować dowolną kartkę, wpisać gotowe rozwiązanie i przejść trasą automatyczną — sprzeczność z ADR-024 i PZS-1 B-1 (C2-01). FR-18 mówi o poprawie „błędnie odczytanego **fragmentu**”.

## Decyzja

(1) `TranskrypcjaWejscie` wiąże każdy krok z krokiem agenta (`nr_kroku_agenta`): bez usuwania, ≤ 2 kroki wstawione; (2) limity twarde (`422 POPRAWKI_ZBYT_ROZLEGLE`): zmienione + wstawione ≤ max(3, ⌈0,5 n⌉), odsetek zmienionych znaków ≤ 0,50; (3) limity miękkie i **Sprawdzenie zgodności poprawek** (rola `ROZPOZNAWANIE_PISMA`, praca `SPRAWDZ_POPRAWKI_TRANSKRYPCJI` startująca przy `PUT`) — niezgodność, brak rezultatu albo przekroczenie limitu miękkiego dodaje powód `POPRAWKI_NIEZGODNE_ZE_ZDJECIEM` (T-7, priorytet 2), wyłącznie w kierunku bezpiecznym (Z-1); (4) Oceny z T-7 są poza populacją progu (`WERYFIKACJA_INNY_POWOD`), Oceny automatyczne z Poprawkami mają podwójny odsetek Kontroli losowej i własny bezpiecznik (11.5.2); (5) metryki `odsetek_rozwiazan_z_poprawkami_30d`, `blad_automatycznych_z_poprawkami_30d`; G-2 mierzy wykrywalność (≥ 95%) i fałszywe alarmy (≤ 10%).

## Alternatywy

Każda Poprawka → Weryfikacja — odrzucona (FR-18 jest codzienną funkcją; podwoiłoby obciążenie Nauczycieli z 17.7); pełne wyłączenie Rozwiązań z Poprawkami z populacji progu — odrzucone, bo NFR-03 dotyczy wszystkich ocen automatycznych, więc te Rozwiązania muszą być mierzone (podwójna Kontrola losowa zamiast wyłączenia); edycja tylko kroków z `pewnosc_odczytu < 0.80` — odrzucona (agent bywa pewny błędnego odczytu, a to właśnie chroni FR-28).

## Konsekwencje

Transkrypcja pochodzi ze zdjęcia także po Poprawkach; ≈ 3% Rozwiązań obliczeniowych więcej w Weryfikacji (17.7); Uczeń z rozległymi zmianami wysyła nowe zdjęcia. Limity zawężają literalne FR-18 — decyzja D-10 (G-0).

## Uzupełnienia

- **Uzupełnienie (it. 5, wersja 2.3, C3-05):** kroki wstawione wchodzą do licznika odsetka zmienionych znaków (mianownik: wszystkie kroki agenta), krok wstawiony ≤ min(200, max(60, ⌈1,5 × mediana⌉)) znaków; autozapis Poprawek w PWA (debounce 2 s), deduplikacja Sprawdzenia po `rozwiazanie_id` tylko wśród `OCZEKUJE` z podmianą ładunku; osobna pula `S` (4 korutyny na replikę), wejście z wycinków i zdjęcia poglądowego 1024 px (A-08: p95 ≤ 4 s, limit ≥ 150/min); G-2: „wstawienia rozwiązania” ≥ 95%, fałszywe alarmy ≤ 5%; T-7 wyprowadzone z rozkładu w 17.7 (2,5–3,1%).

- **Uzupełnienie (it. 6, wersja 2.4, C4-06 p. 10):** okno oczekiwania Oceny na rezultat Sprawdzenia zgodności poprawek jest stałe — `zatwierdzono_at + 6 s` — zamiast `min(zatwierdzono_at + 6 s, termin_miekki − 9 s)`, które przy `t_R ≥ 18 s` malało do 1 s i dawało brak rezultatu dla prawie każdej Poprawki zatwierdzonej od razu po edycji; rachunek p_brak ≤ 0,5% obowiązuje dla każdego `t_R` (9.4), a termin miękki (≥ 10 s) nie jest naruszany, bo model zapasowy startuje równolegle ze Sprawdzeniem.
