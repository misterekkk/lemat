# ADR-043: SLA w dniach roboczych i tygodniowy model obciążenia z eskalacjami

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 4), G-0 |
| Data decyzji | 2026-11-01 (iteracja 4, wersja 2.2) |
| Bramka G-0 | tak — wymaga akceptacji właściciela produktu (decyzje D-x w 23.4 dokumentu głównego) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 4), G-0.

## Kontekst

Model 17.7 liczył obciążenie na 7 dni przy pracy Nauczycieli 5 dni i pomijał napływ eskalacji z Klas do kolejki `GLOBALNA`; kontrola przyjęć nie hamowała eskalacji; G-7 = 12 było spełnialne „na papierze” (C2-12).

## Decyzja

SLA = 3 dni robocze (`dodaj_dni_robocze`, tabela `dzien_wolny`); model tygodniowy (obciążenie dnia roboczego = 7/5 dobowego) z jawnym odsetkiem eskalacji 5% (A-15) i współczynnikiem 1,3; G-7 start = 18 (10 bez Klas + ≥ 8 współpracujących); `zaleglosc_globalna_min` z podziałem na źródło; kontrola przyjęć per Klasa przy `odsetek_eskalacji_7d ≥ 0,20`.

## Alternatywy

SLA 72 h kalendarzowe z akceptacją ryzyka poniedziałkowych eskalacji — odrzucona jako opcja domyślna (pierwszy tydzień pilotażu dałby lawinę `opozniona`); obowiązkowa praca weekendowa weryfikatorów — odrzucona (koszt, brak podstaw w umowach).

## Konsekwencje

Termin dla Ucznia może wynosić do 5 dni kalendarzowych (Weryfikacja z piątku → środa); wyższy koszt puli globalnej; decyzja D-13 (G-0).

## Uzupełnienia

- **Uzupełnienie (it. 6, wersja 2.4, C4-06 p. 6 i 7):** eskalacje z `powod_eskalacji = KARENCJA_KONTA_NAUCZYCIELA` nie wliczają się do `odsetek_eskalacji_7d` (per Klasa i globalnie); Klasy Nauczyciela w karencji są objęte kontrolą przyjęć wprost do Przekazania Klasy albo przywrócenia; wariant 17.7 z aktywnym bezpiecznikiem poprawek (start: `M` = 3 951 min, 20 weryfikatorów; faza ustalona: 16) i decyzja D-17 o zapasie puli.
