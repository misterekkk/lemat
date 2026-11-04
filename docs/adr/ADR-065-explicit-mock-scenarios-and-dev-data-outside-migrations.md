# ADR-065: Scenariusze Mock sterowane jawnie (nagłówek poza `Production`), tryb ścisły E2E i dane deweloperskie poza migracjami

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.2, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 3, wersja 3.2) |
| Bramka G-0 | nie |
| Realizuje | C7-07, C7-12; zmienia ADR-055 p. 6 i 10.0.6 wersji 3.1 |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 10.0.6, 17.5; [`docs/spec/prompts.md`](../spec/prompts.md) sekcja 6 |

## Status

Nowa (it. 3 pętli 3.x).

## Kontekst

W wersji 3.1 `MockChatClient` wybierał odpowiedź po SHA-256 wiadomości
`user` (ta sama Transkrypcja dla każdego Rozwiązania danego Zadania, cichy
fallback przy zmianie formatu), generator ze schematu ustawiał
`przekroczono_limit_krokow = true` (domyślne rozpoznanie kończyło się
`BLAD_ROZPOZNAWANIA`), a `MockModelDecyzyjny` dawał masę 0,90, czyli każda
Ocena offline szła do Weryfikacji (C7-07). Konfiguracje `mock` wstawiała
migracja danych warunkowana `ASPNETCORE_ENVIRONMENT`, której kontener
`migracje` nie musi mieć (C7-12).

## Decyzja

1. **Selektor scenariusza.** Nagłówek `X-Lemat-Mock-Scenariusz: <nazwa>`
   (`[a-z0-9-]{1,60}`) jest przyjmowany przez filtr `FiltrScenariuszaMock`
   rejestrowany **wyłącznie**, gdy `IHostEnvironment.EnvironmentName` ∈
   {`Development`, `E2E`}; w `Production` filtr nie istnieje, a nagłówek
   jest usuwany przez Caddy. Wartość trafia do `IKontekstScenariuszaMock`
   (`AsyncLocal`, `Lemat.Application.Models`), z którego `ZlecaniePracSql`
   kopiuje ją do `praca_w_tle.ladunek.mock_scenariusz` każdej pracy
   zleconej w tej jednostce pracy; `PulaPracHostedService` ustawia kontekst
   z ładunku pobranej pracy, więc scenariusz przechodzi łańcuchem
   `POST /rozwiazania` → `ROZPOZNAJ_PISMO` → (`POST …/zatwierdzenie` z tym
   samym nagłówkiem) → `OCEN_ROZWIAZANIE` → `NAPISZ_WSKAZOWKE` →
   `AKTUALIZUJ_PROFIL`. Playwright ustawia nagłówek dla całego testu
   (`extraHTTPHeaders`).
2. **Dane scenariuszy:** `backend/src/Lemat.Models/Mock/Scenarios/<scenario-folder>/<role-file>.json`
   (odpowiedź roli; dla `GENERATYWNY_ZADANIA` z listą wywołań narzędzi)
   i `<scenario-folder>/decision-primary.json` (rozkład per `kod_pytania`, z
   gwiazdką dla pytań warunkowych: `"Q-OC-05.*"`). Brak scenariusza →
   odpowiedź domyślna roli (p. 3).
3. **Odpowiedzi domyślne (bez nagłówka)** prowadzą przepływ trasą
   automatyczną: rozpoznanie — kroki z pola `tekst_alternatywny_rysunkow`
   albo 3 kroki `x = 1`, `czytelne = true`, `przekroczono_limit_krokow =
   false`, `zgodnosc_z_zadaniem = 0.95`, `pewnosc_odczytu = 0.95`, ramki =
   całe zdjęcie; Sprawdzenie zgodności poprawek — `p_zgodna_z_poprawka =
   0.97`; Wskazówka — „Sprawdź jeszcze raz krok {n}.”; generator dla
   pozostałych pól: liczba = `minimum` (albo 0), napis = `"mock"` przycięty
   do `maxLength` i zgodny z `pattern` przez wartość z atrybutu
   `[PrzykladMock("…")]` na właściwości rekordu, tablica = `minItems`
   elementów, unia z `null` = `null`, `boolean` = `false`.
   `MockModelDecyzyjny` daje masę **0,995** na opcji zgodnej z
   rozwiązaniem wzorcowym (`KatalogPytan.OpcjaWzorcowa(kod)`: Q-OC-01 —
   `tak`, Q-OC-02 — maks. punktów, Q-OC-03 — `BRAK_BLEDU`, Q-OC-05 — `tak`,
   Q-OC-06 — `nie`, Q-WA-* — wartość „poprawne”), resztę po równo; przy
   kalibratorze tożsamościowym i progu 0,95 Ocena domyślna jest
   automatyczna (Pewność oceny 0,995 ≥ 0,95).
4. **Tryb ścisły:** `Lemat:Mock:TrybScisly = true` (ustawiane w
   środowisku `E2E`) — nagłówek wskazujący nieistniejący katalog albo brak
   pliku roli w istniejącym scenariuszu → `MockScenariuszNieznanyException`
   (`BladModeluTrwalyException("MOCK_SCENARIUSZ_NIEZNANY")`), który test
   E2E wykrywa w logu i w stanie Rozwiązania (`BLAD_ROZPOZNAWANIA`
   `BLAD_DOSTAWCY`).
5. **Czas testowy:** `LEMAT_CZAS_TESTOWY=<ISO-8601>` (tylko poza
   `Production`) rejestruje `PrzesunietyTimeProvider` (czas systemowy +
   stałe przesunięcie liczone przy starcie) we wszystkich procesach;
   ziarno i stan Q-RE-01 nie zależą wtedy od dnia uruchomienia testu.
6. **Dane deweloperskie poza migracjami (C7-12):** migracje nigdy nie
   wstawiają konfiguracji `mock`. Polecenie `dotnet Lemat.Tools.dll
   seed-dev` (osobna usługa `seed` w `docker-compose.yml`, profil `dev`,
   uruchamiana po `migracje`) wstawia aktywne Konfiguracje modelu `mock`
   dla 8 ról, próg 0,95, kalibrator tożsamościowy i dane referencyjne
   demonstracyjne; odmawia działania, gdy `LEMAT_SRODOWISKO = Production`
   albo gdy baza ma konto Administratora utworzone poza `seed-dev`.
   Test `MigracjeNaPustejBazieNieTworzaKonfiguracjiMock`.
7. **Macierz scenariuszy E2E** (pliki w repozytorium, każdy z testem
   Playwright): `auto-poprawne`, `auto-blad-rachunkowy`,
   `weryfikacja-niska-pewnosc`, `weryfikacja-poprawny-wynik-bledne-kroki`,
   `weryfikacja-dowod`, `weryfikacja-podejrzenie-odczytu`,
   `weryfikacja-poprawki-niezgodne`, `kontrola-losowa` (z
   `LEMAT_KONTROLA_LOSOWA_ZIARNO`), `t6-oba-modele-niedostepne`,
   `t7-brak-sprawdzenia-poprawek`, `rozpoznanie-nieczytelne`,
   `rozpoznanie-blad-przejsciowy-potem-sukces`, `rozpoznanie-timeout`,
   `generowanie-z-narzedziami`, `generowanie-limit-wywolan`.

## Alternatywy

- **Klucz po skrócie zdjęcia** — odrzucona: normalizacja (ADR-052)
  zmienia bajty, a metadane EXIF są usuwane.
- **Kolumna `mock_scenariusz` w `rozwiazanie`** — odrzucona: pole
  testowe w schemacie produkcyjnym.
- **Migracja danych z warunkiem środowiska** — odrzucona (C7-12): wpis w
  historii migracji niezależny od tego, czy dane wstawiono.

## Konsekwencje

- Testy E2E sterują każdą ścieżką bez osobnych Zadań, a zmiana formatu
  promptu nie zmienia danych Mock po cichu.
- Kod produkcyjny ma jedną gałąź zależną od Mock: kopiowanie
  `mock_scenariusz` w `ZlecaniePracSql`, aktywne tylko przy niepustym
  kontekście (który w `Production` nigdy nie powstaje).
