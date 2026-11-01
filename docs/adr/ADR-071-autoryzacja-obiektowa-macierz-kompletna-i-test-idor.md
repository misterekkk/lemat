# ADR-071: Autoryzacja obiektowa — kompletna macierz, reguła identyfikatorów zagnieżdżonych, widoczność Zadań dla Ucznia, rejestracja bez roli Administratora, odczyty Administratora przez `/admin/*`

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.3, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 4, wersja 3.3) |
| Bramka G-0 | nie |
| Realizuje | C8-06, C8-07, C8-12 p. 6; uzupełnia ADR-062 p. 2, ADR-028, ADR-044 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 12.3, 12.8, 12.12; [`docs/spec/api.md`](../spec/api.md) 3, 7 |

## Status

Nowa (it. 4 pętli 3.x).

## Kontekst

C8-06 i C8-07 wykazały: brak reguły, które Zadania Uczeń może czytać
(`GET /zadania/{id}` zwracał każde Zadanie po identyfikatorze); brak
wierszy macierzy 12.8 dla Powiadomień, Zleceń generowania, Testów
diagnostycznych, Importów; brak reguły spójności identyfikatorów
zagnieżdżonych; rejestrację z rolą `ADMINISTRATOR` w typie wejścia; trzy
sprzeczne źródła o odczytach Administratora; brak odczytu Klasy przez
Administratora przed Przekazaniem Klasy; brak flagi `diagnostyczne` w
odczycie; brak ścieżki poprawy otagowania Zadania.

## Decyzja

1. **Macierz 12.8 jest kompletna:** ma wiersz dla każdej ścieżki z `{id}`
   katalogu 12.3; test `MacierzUprawnienPokrywaKatalog` porównuje zbiór
   ścieżek z `EndpointDataSource` ze zbiorem ścieżek w kolumnie „Ścieżki”
   macierzy (plik `backend/tests/Lemat.Api.Tests/Uprawnienia/macierz-12-8.yaml`
   generowany z dokumentu).
2. **Identyfikatory zagnieżdżone.** Dla ścieżki
   `/{zasob}/{id}/{podzasob}/{podzasob_id}` przypadek użycia wczytuje
   podzasób jednym zapytaniem z warunkiem przynależności do rodzica i
   widoczności rodzica dla `Aktor`
   (`… WHERE z.id = @podzasob_id AND z.rozwiazanie_id = @id AND <widoczność(@id, aktor)>`).
   Niespełnienie którejkolwiek części → `404 NIE_ZNALEZIONO`. Dotyczy:
   `zdjecie_rozwiazania.rozwiazanie_id = {id}`,
   `pozycja_testu_diagnostycznego.test_diagnostyczny_id = {id}`,
   `czlonkostwo_klasy(klasa_id = {id}, uczen_id = {uczen_id})` aktywne
   (ścieżki `/klasy/{id}/uczniowie/{uczen_id}*`).
3. **Widoczność Zadania dla Ucznia.** `GET /zadania/{id}` dla roli U
   wymaga dokładnie jednego parametru kontekstu `?element_sciezki_id=`,
   `?przypisanie_id=` albo `?rozwiazanie_id=` (brak albo więcej niż jeden
   → `422 WALIDACJA`) i zwraca `ZadanieUczniaWyjscie` w wersji tego
   kontekstu:
   | Kontekst | Warunek widoczności | Wersja (`zadanie_wersja_id`) |
   |---|---|---|
   | element Ścieżki nauki | element Ucznia, `zadanie_id = {id}`, status `PROPONOWANY`, `ROZPOCZETY` albo `UKONCZONY` | `element.zadanie_wersja_id`, a dla `PROPONOWANY` — wersja aktualna |
   | Przypisanie | `przypisanie_uczen` zawiera Ucznia, Zestaw zawiera `{id}`, Przypisanie niewycofane albo wycofane po rozpoczęciu próby przez Ucznia | wersja aktualna, a po rozpoczęciu próby — wersja próby |
   | Rozwiązanie | Rozwiązanie Ucznia dla `{id}` | `rozwiazanie.zadanie_wersja_id` |

   Zadanie w statusie `WALIDACJA`, `ODRZUCONE_W_WALIDACJI`, `DO_PRZEGLADU`
   albo `ODRZUCONE` jest widoczne wyłącznie w kontekście własnego
   Rozwiązania (historia). Pozycje Testu diagnostycznego Uczeń czyta
   wyłącznie przez `GET /testy-diagnostyczne/{id}` (zawiera
   `ZadanieUczniaWyjscie`), nie przez `/zadania/{id}`.
4. **Rejestracja.** `RejestracjaWejscie.Rola` ma typ `RolaRejestracji {
   Uczen, Nauczyciel }`; wartość `ADMINISTRATOR` daje `422 WALIDACJA`
   (`TYP_NIEPRAWIDLOWY`, ADR-067 p. 6). Konto Administratora powstaje
   wyłącznie poleceniem `dotnet Lemat.Narzedzia.dll utworz-administratora
   --email <adres>` (to samo narzędzie co `seed-dev`, 10.0; hasło
   jednorazowe z wymuszoną zmianą) albo przez innego Administratora (`POST
   /admin/uzytkownicy`, audyt `ADMINISTRATOR_UTWORZONY`).
5. **Administrator nie czyta treści pracy Uczniów** (Rozwiązania, Oceny,
   Transkrypcje, Weryfikacje, zdjęcia, Przypisania, Zestawy zadań) przez
   endpointy U/N (polityki tych endpointów go wykluczają; zasada
   minimalizacji RODO). Dostaje dane zagregowane (`/admin/kalibracja`,
   `/admin/obciazenie-weryfikacji`) i odczyty administracyjne z audytem
   (`dziennik_audytu`, typ `ODCZYT_ADMINISTRACYJNY`):
   - `GET /admin/klasy?nauczyciel_id&zarchiwizowane&kursor` →
     `Strona<KlasaAdministratora>`;
   - `GET /admin/klasy/{id}` → `KlasaAdministratora` (z `version` dla
     `If-Match: K` Przekazania Klasy, liczbą członków, liczbą otwartych
     Przypisań i Weryfikacji `KLASA`, bez listy członków);
   - `GET /zadania`, `GET /zadania/{id}` (`ZadaniePelneWyjscie` z polem
     `diagnostyczne`), filtr `?diagnostyczne=true|false`.
6. **Zarządzanie bazą Zadań przez Administratora:** import (FR-35),
   `PUT /admin/zadania/{id}/otagowanie` (`If-Match: Z`,
   `OtagowanieZadaniaWejscie { dzial_id, umiejetnosci, poziom_trudnosci_nominalny }`
   → nowa wersja Zadania, ADR-012; walidacja `trg_umiejetnosc_glowna_w_dziale`;
   rodzaj Zadania jest niezmienny — zmiana rodzaju = wycofanie i nowe
   Zadanie), `PUT /admin/zadania/{id}/diagnostyczne`, Przegląd i wycofanie
   (`POST /zadania/{id}/przeglad|wycofanie`), rozpatrzenie Zgłoszeń błędu.
   `POST /zadania` pozostaje operacją Nauczyciela (słownik: Zadanie bazowe
   „dodane ręcznie przez nauczyciela”); strona `admin/zadania` nie ma
   akcji „Nowe Zadanie”.
7. **Polityki `RecenzentTresci` i `WeryfikatorGlobalny` są usunięte**
   (C8-12 p. 6): flagi `recenzent_tresci` i `weryfikator_globalny` są
   warunkami obiektowymi macierzy 12.8 sprawdzanymi w przypadku użycia
   (`Aktor.Flagi`), nie politykami grup.
8. **Test IDOR.** `KazdyEndpointZIdMaTestIdor`
   (`Lemat.Api.Tests/Uprawnienia`, `WebApplicationFactory` + dane z
   `ZbiorDanychIdor`: dwóch Uczniów, dwóch Nauczycieli w dwóch Klasach,
   Nauczyciel NR, Administrator): dla każdej ścieżki z `{id}` z
   `EndpointDataSource` i każdej roli dopuszczonej polityką — zasób innego
   użytkownika tej samej roli → `404 NIE_ZNALEZIONO`; dla ścieżek
   zagnieżdżonych — podzasób innego rodzica → `404`. Ścieżka bez przypadku
   w `ZbiorDanychIdor` kończy test błędem.

## Alternatywy

- **Administrator z pełnym odczytem przez endpointy U/N** — odrzucona:
  sprzeczna z minimalizacją (16.3) i niepotrzebna w żadnym przepływie A.
- **Rola z kodem zaproszenia Administratora w rejestracji** — odrzucona:
  anonimowy endpoint nie powinien prowadzić do roli uprzywilejowanej.

## Konsekwencje

- 12.3 dostaje `GET /admin/klasy`, `GET /admin/klasy/{id}`, `PUT
  /admin/zadania/{id}/otagowanie`, `POST /admin/uzytkownicy`, trasę `GET
  /uczniowie/ja/profil` i parametry kontekstu `GET /zadania/{id}`.
- `api.md` 7: strona `admin/klasy` czyta `GET /admin/klasy*`, strona
  `admin/pula-diagnostyczna` filtruje `GET /zadania?diagnostyczne=`.
