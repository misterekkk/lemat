# ADR-075: Angielskie nazwy plików i katalogów

| Pole | Wartość |
|---|---|
| Status | Nowa (wersja 3.4, 2026-11-01) |
| Data decyzji | 2026-11-01 (wymaganie właściciela projektu po pętli 3.x, wersja 3.4) |
| Bramka G-0 | nie — wymaganie zgłosił właściciel projektu; słownik i wymagania bez zmian |
| Zmienia | ADR-049 p. 3 (wiersze „Przestrzeń nazw C#”, „Komponenty React”, „Pozostałe pliki TS”, „Ścieżki URL API i segmenty tras Next.js”), p. 6, p. 7, p. 10, p. 11 (uzupełnienie wersji 3.4 w ADR-049) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 3.5 (kolumna „Nazwa pliku/folderu (EN)”), 6.4, 6.7, 6.8, 6.11; [`docs/spec/prompts.md`](../spec/prompts.md) 2, 6 |

## Status

Nowa (wersja 3.4). Przyjęta przez orkiestratora na podstawie wymagania
właściciela projektu: „nazwy folderów oraz plików mają być po angielsku”.

## Kontekst

ADR-016 i ADR-049 ustaliły polskie identyfikatory zgodne ze słownikiem we
wszystkich warstwach, a wraz z nimi polskie nazwy przestrzeni nazw,
projektów `Lemat.Agenci`/`Lemat.Modele`, katalogów modułów C#, plików `.cs`
(konwencja C# „nazwa pliku = nazwa typu”), katalogów i plików frontendu,
segmentów tras App Router, plików testów, SQL migracji, promptów i danych
Mock. Właściciel projektu wymaga angielskich nazw plików i katalogów:
repozytorium ma być czytelne dla narzędzi, przeglądarek kodu i
zewnętrznych współpracowników bez znajomości polskiego, a jednocześnie nie
wolno naruszyć Z-6 (słownik jest prawem) w identyfikatorach. Wymaganie
dotyczy wyłącznie nazw plików i katalogów.

## Decyzja

1. **Zakres.** Wszystkie nazwy katalogów i plików w repozytorium są
   angielskie i ASCII: dokumentacja (`docs/adr/ADR-NNN-<english-slug>.md`,
   `docs/spec/prompts.md`, `docs/spec/tools/check_rtm.py`), projekty i
   katalogi .NET, pliki `.cs`, katalogi i pliki `frontend/` (w tym segmenty
   tras App Router), pliki testów, skrypty, pliki SQL migracji, pliki
   konfiguracyjne i danych. Poza zakresem są: klucze obiektów i nazwy
   bucketów magazynu (dane w S3), zawartość archiwum ZIP importu
   `ArkuszImportV1` (`arkusz.json`, `rys/`), kod lokalizacji
   `messages/pl-PL.json`, nazwy narzucone przez narzędzia (`page.tsx`,
   `layout.tsx`, `Program.cs`, `appsettings.json`) oraz historyczne nazwy
   wersji 2.4 cytowane z dopiskiem „w 2.4”.
2. **Identyfikatory bez zmian.** Typy, właściwości i członkowie enumów
   C#/TS, tabele, kolumny i typy PostgreSQL, nazwy JSON, ścieżki REST API
   .NET, nazwy zdarzeń SSE, kody błędów, typy prac w tle, wartości
   `wersja_promptu`, identyfikatory scenariuszy Mock i identyfikatory
   migracji pozostają zgodne ze słownikiem (ADR-016, ADR-049). Zakazane
   synonimy z CLAUDE.md §5 p. 4 / 3.1 p. 4 dotyczą identyfikatorów, nie
   nazw plików (`Student.cs` z klasą `Uczen` jest poprawny).
3. **Jedna nazwa EN na pojęcie.** Tabela 3.5 ma kolumnę „Nazwa
   pliku/folderu (EN)” dla wszystkich 111 pojęć (25 ze słownika i 86 z
   PZS-1); nazwy są parami różne także bez rozróżniania wielkości liter
   (sprawdzone skryptem; test `NazwyPlikowPojecUnikalne`).
4. **Projekty i przestrzenie nazw.** Przestrzeń nazw odpowiada katalogowi,
   więc jest angielska: `Lemat.Agenci` → `Lemat.Agents`, `Lemat.Modele` →
   `Lemat.Models`, `Lemat.Narzedzia` → `Lemat.Tools`, `Lemat.Symulacje` →
   `Lemat.Simulations`, testy `Lemat.Agents.Tests`, `Lemat.Models.Tests`;
   segmenty modułów według tabeli 6.11.1 (`Konta` → `Accounts`, `Tresci` →
   `Content`, `Rozwiazania` → `Solutions`, `Ocenianie` → `Grading`,
   `Weryfikacje` → `Verifications`, `Wspolne` → `Common`, `PraceWTle` →
   `BackgroundJobs` …). Typy w przestrzeniach pozostają polskie
   (`Lemat.Domain.Grading.Ocena`).
5. **Nazwa pliku `.cs`.** Konwencja „nazwa pliku = nazwa typu” przestaje
   obowiązywać. Plik zawiera jeden typ najwyższego poziomu; plik typu
   realizującego pojęcie z 3.5 nosi nazwę EN pojęcia z opcjonalnym
   przyrostkiem roli z zamkniętej listy 6.11.2 p. 2 (`Solution.cs`,
   `SolutionStatus.cs`, `GradeOutput.cs`, `GradingAgent.cs`); plik typu
   technicznego — tłumaczenie z manifestu
   `backend/tests/Lemat.Architecture.Tests/Glossary/file-names.yaml`
   (`OgranicznikSzybkosci` → `RateLimiter.cs`). Manifest ma wpis dla
   każdego pliku `.cs` (plik → typ).
6. **Pliki nazwane dotąd identyfikatorem.** Plik dostaje nazwę angielską,
   a identyfikator przechodzi do indeksu albo atrybutu: katalogi ról
   (`DECYZYJNY_GLOWNY` → `decision-primary` …, tabela 6.11.5), indeks
   promptów `Prompts/index.yaml` ((`rola_modelu`, `wersja_promptu`) →
   plik), indeks scenariuszy `Mock/Scenarios/index.yaml` (identyfikator →
   katalog), migracje EF Core (`0001_InitialSchema.cs` z
   `[Migration("0001_SchematPoczatkowy")]`).
7. **Frontend.** Segmenty tras App Router, grupy tras (`(public)`,
   `(student)`, `(teacher)`, `(administrator)`), moduły `src/features/`,
   katalogi `src/lib/` i pliki są angielskie (mapowanie 6.11.4).
   Wynikające z tego **angielskie URL-e stron frontendu są akceptowane**
   (`/logowanie` → `/login`, `/nauczyciel/weryfikacje/{id}` →
   `/teacher/verifications/{id}`); ścieżki REST API się nie zmieniają.
   Komponent React jest w pliku `PascalCase.tsx` z nazwą EN
   (`EdytorTranskrypcji` w `TranscriptionEditor.tsx`), pozostałe pliki —
   `kebab-case.ts` (`client.ts`, `refresh.ts`). Klucze `params`
   segmentów dynamicznych są angielskie (`solutionId`) i występują
   wyłącznie w destrukturyzacji `page.tsx` do zmiennych o nazwach ze
   słownika; test słownika ma je na zamkniętej liście wyjątków.
8. **Analizatory.** `backend/.editorconfig` (6.11.3): SA1649, MA0048,
   RCS1060 — `none`; `dotnet_style_namespace_match_folder = true` i IDE0130
   — `error`; IDE1006 (nazewnictwo identyfikatorów) — bez zmian, `error`;
   `EnforceCodeStyleInBuild=true` w `Directory.Build.props`.
9. **Weryfikacja.** Test `TestNazwPlikow` (`FileNamesTest.cs`,
   `Lemat.Architecture.Tests`) sprawdza całe repozytorium (`git ls-files`):
   wpis manifestu i zadeklarowany typ dla każdego pliku `.cs`, zgodność
   przestrzeni nazw z katalogiem, budowę nazwy z kolumny 3.5 i przyrostka,
   unikalność w projekcie, brak tokenów polskich (rdzenie ≥ 4 znaków form
   bazowych 3.5 minus tokeny wspólne z angielskim) i alfabet ASCII
   `[A-Za-z0-9._()\[\]-]` w każdym segmencie ścieżki.

## Alternatywy

- **Angielskie nazwy plików tylko w frontendzie i dokumentacji, polskie w
  .NET** — odrzucone: wymaganie obejmuje wszystkie pliki i katalogi, a
  przestrzeń nazw C# musi odpowiadać katalogowi (IDE0130), więc polski
  katalog wymuszałby polską przestrzeń nazw.
- **Angielskie katalogi z polskimi przestrzeniami nazw (`RootNamespace`
  i wyłączone IDE0130)** — odrzucone: rozjazd katalog ↔ przestrzeń nazw
  utrudnia nawigację, a przestrzenie nazw są strukturą plików, nie
  pojęciami domenowymi.
- **Angielskie nazwy typów (i plików) z polskimi kolumnami** — odrzucone
  (Z-6, ADR-016, ADR-049 „Alternatywy”).
- **Przekierowania ze starych polskich URL-i stron** — odrzucone: system
  nie był uruchomiony produkcyjnie, więc nie ma zakładek ani linków do
  zachowania.

## Konsekwencje

- Nazwa pliku `.cs` nie wynika już z nazwy typu: nawigacja „Go to file”
  wymaga angielskiej nazwy, a „Go to type” — polskiej; manifest
  `file-names.yaml` i kolumna 3.5 są jedynym źródłem tej relacji.
- Każdy nowy plik `.cs` wymaga wpisu w manifeście (test kończy się błędem
  bez wpisu); nowe pojęcie wymaga nazwy EN w 3.5 razem z propozycją do
  słownika (CLAUDE.md §5 p. 7).
- Adresy stron frontendu są angielskie, a ścieżki API polskie; tabela 6.11.4
  i `docs/spec/api.md` 7 wiążą stronę z endpointami.
- Dokumentacja wersji 3.4 ma przemianowane 73 pliki ADR i dwa pliki
  `docs/spec/`; odnośniki zaktualizowano, a skrypt sprawdzający odnośniki
  nie znajduje martwych celów.
