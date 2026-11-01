# ADR-074: Zmiana terminu i wycofanie Przypisania; ponowna odpowiedź w Teście diagnostycznym

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.3, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 4, wersja 3.3) |
| Bramka G-0 | nie |
| Realizuje | C8-11 p. 3 i p. 4; uzupełnia ADR-032 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.3, 12.5; [`docs/spec/ddl.md`](../spec/ddl.md) `przypisanie` |

## Status

Nowa (it. 4 pętli 3.x).

## Kontekst

C8-11 p. 3: Przypisania nie dało się zmienić ani wycofać, więc błędny
termin oznaczał ujawnienie klucza odpowiedzi Klasie w złym momencie
(ADR-032). C8-11 p. 4: semantyka ponownej odpowiedzi na pozycję Testu
diagnostycznego była nieokreślona.

## Decyzja

1. **`PATCH /przypisania/{id}`** (`Nauczyciel`, autor Przypisania,
   `If-Match: P` = `przypisanie.version`, JSON Merge Patch ADR-072)
   `ZmianaPrzypisaniaWejscie { termin }` → `200 PrzypisanieWyjscie`.
   Warunki: Przypisanie niewycofane; obecny termin w przyszłości (po
   terminie klucz mógł zostać ujawniony → `409 PRZYPISANIE_PO_TERMINIE`);
   nowy termin ≥ `now() + 1 h`. W tej samej transakcji:
   `odpowiedz.rezultat_ujawniony_od := nowy termin` dla Odpowiedzi tego
   Przypisania z `rezultat_ujawniony_od > now()`; elementy `R-PRZYPISANIE`
   dostają `wygasa_at` przeliczone z nowego terminu; Powiadomienie
   `NOWE_PRZYPISANIE` (`dane.zmiana = "TERMIN"`) dla adresatów.
2. **`POST /przypisania/{id}/wycofanie`** (`If-Match: P`,
   `WycofaniePrzypisaniaWejscie { powod (10–1000 znaków) }`) → `200
   PrzypisanieWyjscie`. Skutki w jednej transakcji:
   - `przypisanie.wycofano_at = now()`, `powod_wycofania`;
   - elementy `R-PRZYPISANIE` w stanie `PROPONOWANY` → `WYGASLY`;
   - nowe Odpowiedzi i Rozwiązania w kontekście Przypisania →
     `409 PRZYPISANIE_WYCOFANE`;
   - Rozwiązania już przesłane i Odpowiedzi już udzielone pozostają i są
     oceniane (ich Wyniki zadania aktualizują Profil ucznia, FR-29);
   - klucz odpowiedzi nie jest ujawniany, jeżeli wycofanie nastąpiło przed
     terminem (`rezultat_ujawniony_od` Odpowiedzi z przyszłą wartością →
     `NULL`, a odczyt rezultatu zwraca „Przypisanie wycofane”).
   Przypisanie wycofane jest widoczne na liście Nauczyciela z filtrem
   `?wycofane=true` i nie jest liczone w `PostepPrzypisaniaWyjscie`
   bieżących.
3. **Ponowna odpowiedź na pozycję Testu diagnostycznego** do zakończenia
   Testu nadpisuje poprzednią (`UPDATE odpowiedz SET wybor, czy_poprawna,
   punkty` w wierszu `ux_odpowiedz_test`) i zwraca `200
   ZapisOdpowiedziWyjscie { czy_zapisano: true, nadpisano: true }`;
   pierwsza odpowiedź → `201 { czy_zapisano: true, nadpisano: false }`.
   Po zakończeniu albo pominięciu Testu → `409 TEST_ZAKONCZONY`.
   Poprawność nie jest ujawniana do zakończenia, więc nadpisanie nie daje
   przewagi.

## Alternatywy

- **Usuwanie Przypisania (`DELETE`)** — odrzucona: Rozwiązania i Wyniki
  zadania odwołują się do `przypisanie_id`, a historia postępów (FR-31)
  musi pozostać.
- **Odrzucanie ponownej odpowiedzi w Teście** — odrzucona: Uczeń, który
  kliknął omyłkowo, nie mógłby poprawić odpowiedzi przed końcem Testu.

## Konsekwencje

- Nowy zasób wersjonowany `P` (Przypisanie) w 12.1 i nowy kod `409
  PRZYPISANIE_WYCOFANE` w 12.2.
- Testy: `ZmianaTerminuPrzesuwaUjawnienie`, `ZmianaPoTerminieOdrzucona`,
  `WycofanieWygaszaProponowaneElementy`, `WycofanieNieUjawniaKlucza`,
  `PonownaOdpowiedzWTescieNadpisuje`.
