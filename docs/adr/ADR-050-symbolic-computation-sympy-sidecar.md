# ADR-050: Obliczenia symboliczne — sidecar SymPy w piaskownicy zamiast biblioteki .NET

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.0, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 1, wersja 3.0) |
| Bramka G-0 | nie |
| Realizuje | ADR-048 p. 4; zastępuje realizację ADR-013 (treść domenowa ADR-013 obowiązuje) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 5.1, 5.3, 6.6, 13.1, 16.2, 22 |

## Status

Przyjęta.

## Kontekst

Port `SprawdzanieSymboliczne` (13.1) ma trzy operacje: postać liniowa
wyrażenia z LaTeX, równoważność dwóch kroków i równoważność wyniku z
`WynikKoncowyWzorcowy`; wynik jest trójwartościowy (`ROWNOWAZNE`,
`NIEROWNOWAZNE`, `NIEROZSTRZYGNIETE`). Korzystają z niego: Agent oceniający
(FR-21, wynikanie kroków), Walidacja zadania (FR-13, porównanie rozwiązań
niezależnych) i Agent rozpoznający (obniżenie pewności niepoprawnego LaTeX,
ADR-004). Wymagane są: parser LaTeX zapisu szkolnego (ułamki, pierwiastki,
potęgi, funkcje trygonometryczne i logarytmy, przedziały, zbiory, układy
równań, nierówności), upraszczanie i porównanie wyrażeń, rozwiązywanie
równań i nierówności w ℝ, porównanie zbiorów rozwiązań. Limity: 2 s CPU na
parę, 3 s na Rozwiązanie, 512 MiB (16.2, 22). Wejście pochodzi z kartki
Ucznia i z LLM, więc parser musi działać w piaskownicy (R-06).

## Decyzja

1. SymPy 1.13 (`parse_latex` z backendem `lark`) działa w osobnym
   kontenerze **`lemat-sympy`** (Python 3.12, obraz
   `python:3.12-slim-bookworm`, zależności przypięte w `uv.lock`), uruchamianym
   jako sidecar 1:1 z każdą repliką `worker`.
2. Serwer: Starlette + Uvicorn nasłuchujące **wyłącznie na gnieździe Unix**
   `/run/lemat/sympy.sock` (wolumen współdzielony z `worker`, uprawnienia
   0660); kontener z `network_mode: none`, `read_only: true`, użytkownikiem
   bez uprawnień, `pids_limit: 64`, limitem 2 vCPU / 2,5 GiB.
3. Wykonanie: pula 4 procesów roboczych tworzonych przez `multiprocessing`
   (`spawn`) z `resource.setrlimit(RLIMIT_AS, 512 MiB)` i
   `RLIMIT_CPU` 2 s na zadanie (proces jest zastępowany nowym po każdym
   przekroczeniu i po 200 zadaniach); proces nadzorczy zabija proces
   roboczy po 2,2 s czasu ściennego. Przekroczenie dowolnego limitu,
   błąd parsera albo wyjątek SymPy zwraca `NIEROZSTRZYGNIETE` z polem
   `przyczyna` (`PRZEKROCZONY_CZAS`, `PRZEKROCZONA_PAMIEC`,
   `BLAD_PARSERA`, `BLAD_OBLICZEN`).
4. Kontrakt HTTP/1.1 JSON (wersja `v1`, pola `snake_case` zgodnie z
   ADR-049):
   - `POST /v1/postac-liniowa` `{ "latex": string }` → `{ "postac": string | null, "przyczyna": string | null }`
   - `POST /v1/rownowaznosc-krokow` `{ "latex_a": string, "latex_b": string }` → `{ "rownowaznosc": "ROWNOWAZNE" | "NIEROWNOWAZNE" | "NIEROZSTRZYGNIETE", "przyczyna": string | null, "czas_ms": number }`
   - `POST /v1/rownowaznosc-wyniku` `{ "latex": string, "wynik": WynikKoncowyWzorcowy }` → jak wyżej
   - `GET /zdrowie` → `200 {"status":"OK","wersja_sympy":"1.13.x"}`
   Ciała ≤ 16 KiB (LaTeX kroku ≤ 2 000 znaków, 7.3); większe → `413`.
5. Klient: `SympySidecarKlient : ISprawdzanieSymboliczne` w
   `Lemat.Infrastructure.Sandboxes`, typowany `HttpClient` z
   `SocketsHttpHandler.ConnectCallback` łączącym się z gniazdem Unix,
   timeout 2,2 s na parę i budżet 3 s na Rozwiązanie liczony przez Agenta
   oceniającego (`CancellationToken`); błąd połączenia lub timeout →
   `NIEROZSTRZYGNIETE` (SymPy jest sygnałem, nie werdyktem — ADR-013).
6. Testy: `backend/sandbox/sympy/tests` (pytest) dla obliczeń; testy
   kontraktowe klienta w `Lemat.Infrastructure.Tests` z kontenerem
   `lemat-sympy` uruchamianym przez Testcontainers; w testach
   jednostkowych domeny i agentów — `FalszyweSprawdzanieSymboliczne`
   (deterministyczne odpowiedzi z tabeli przypadków).

## Alternatywy

- **AngouriMath** (C#, MIT) — odrzucona: parser LaTeX obsługuje
  podzbiór zapisu (brak pewnej obsługi przedziałów, układów, nierówności
  wielokrotnych i zapisu szkolnego typu `\left(` / `\cdot` w kombinacjach z
  tekstem), słabsze rozwiązywanie nierówności i porównanie zbiorów, mała
  społeczność i rzadkie wydania; ryzyko fałszywych `NIEROWNOWAZNE`
  kierujących do Weryfikacji i zawyżających obciążenie 17.7.
- **MathNet.Symbolics** (F#) — odrzucona: brak parsera LaTeX, brak
  rozwiązywania równań i nierówności.
- **SymPy przez Python.NET w procesie workera** — odrzucona: interpreter
  CPython w procesie .NET dzieli pamięć i GIL z pętlami prac w tle, brak
  izolacji RLIMIT (zawieszenie parsera blokuje proces), trudna diagnostyka
  awarii natywnych.
- **Sidecar przez TCP zamiast gniazda Unix** — odrzucony: wymagałby sieci
  w kontenerze bez potrzeby; gniazdo Unix daje izolację sieciową i
  uprawnienia plikowe.
- **Własny parser LaTeX w C# + biblioteka CAS** — odrzucony: koszt i
  ryzyko większe niż utrzymanie jednego bezstanowego kontenera.

## Konsekwencje

- Zachowana semantyka i limity ADR-013 oraz wyniki 10.4/10.2 oparte na
  SymPy; brak ryzyka regresji trafności z powodu zmiany silnika.
- Jeden komponent w Pythonie (R-23): osobny obraz i jego aktualizacje
  bezpieczeństwa; komponent jest bezstanowy, bez dostępu do sieci, bazy i
  danych osobowych (dostaje wyłącznie LaTeX kroków).
- Koszt wywołania między procesami (serializacja JSON + gniazdo Unix)
  ≈ 1–3 ms na parę, pomijalny wobec budżetu 3 s (17.2).
