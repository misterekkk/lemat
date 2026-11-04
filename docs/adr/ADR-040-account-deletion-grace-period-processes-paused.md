# ADR-040: Okres karencji usunięcia konta: procesy wstrzymane, nie anulowane

| Pole | Wartość |
|---|---|
| Status | Nowa (it. 4) |
| Data decyzji | 2026-11-01 (iteracja 4, wersja 2.2) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Nowa (it. 4).

## Kontekst

Żądanie usunięcia natychmiast anulowało Weryfikacje i Rozwiązania, a przywrócenie w 30 dniach zostawiało trwale zawieszone Wyniki, zużyty limit zgłoszeń, Rozwiązania `ANULOWANE` z Wynikami wstępnymi i Kontrole pominięte w `π_wykonanie` (C2-08).

## Decyzja

W okresie karencji stany się nie zmieniają; handlery kończą się z `KONTO_USUWANE`, Kolejka weryfikacji pomija konta `USUWANE`, Kontrole tych kont są poza licznikiem i mianownikiem nadzoru; przywrócenie wznawia procesy (tabela 16.4: nowe `termin_at`, ponowne zlecenie rozpoznania, T-6 dla przerwanych ocen); anulowanie wykonuje `USUN_KONTO` w kroku 0. Scenariusz C-33.

## Alternatywy

Anulowanie przy żądaniu i odtwarzanie procesów przy przywróceniu — odrzucone (odtworzenie Weryfikacji wymagałoby zachowania pełnego stanu sprzed anulowania, czyli i tak wstrzymania danych).

## Konsekwencje

Przywrócenie jest bezstratne; przez 30 dni dane są przechowywane bez przetwarzania (art. 12 ust. 3 RODO — realizacja żądania w ciągu miesiąca).

## Uzupełnienia

- **Uzupełnienie (it. 5, wersja 2.3, C3-08):** rozstrzygnięcie i korekta wymagają konta Ucznia `AKTYWNE` (`409 KONTO_UCZNIA_NIEAKTYWNE`, szkic zachowany), więc w karencji nie powstaje Ocena Nauczyciela, Wynik zadania ani Próbka kalibracyjna — deklaracja „bez przetwarzania” nie ma wyjątków; przywrócenie zleca `KOPIUJ_DO_ZBIORU_TRENINGOWEGO` dla Rozwiązań bez aktualnej kopii; dla kont Nauczycieli i Administratorów — osobna tabela przywrócenia (16.4) i odroczenie odebrania flag oraz usunięcia szkiców do `USUN_KONTO`.
