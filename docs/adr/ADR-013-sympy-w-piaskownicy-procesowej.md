# ADR-013: SymPy w piaskownicy procesowej

| Pole | Wartość |
|---|---|
| Status | Zastąpiony przez ADR-048 (wersja 3.0, 2026-11-01); poprzedni status: Przyjęta |
| Data decyzji | 2026-10-31 (iteracja 1, wersja 1.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Zastąpiony przez ADR-048 (wersja 3.0, 2026-11-01); poprzedni status: Przyjęta.

**Wyjaśnienie zastąpienia (wersja 3.0).** Treść domenowa obowiązuje bez zmian: SymPy jest sygnałem, a nie werdyktem; wynik trójwartościowy `ROWNOWAZNE` / `NIEROWNOWAZNE` / `NIEROZSTRZYGNIETE`; limity 2 s CPU na parę, 3 s na Rozwiązanie, 512 MiB (16.2, 22); `parse_latex` z backendem `lark`. Zmienia się osadzenie: zamiast puli procesów wewnątrz pythonowego workera SymPy działa w sidecarze `lemat-sympy` (Python 3.12, kontener bez sieci zewnętrznej) wywoływanym przez port `ISprawdzanieSymboliczne` z procesu `Lemat.Worker` przez HTTP/JSON na gnieździe Unix — szczegóły i odrzucone alternatywy .NET (AngouriMath, MathNet.Symbolics) w ADR-050.

## Kontekst

FR-21, FR-13; ryzyko zawieszenia i nadużycia parsera.

## Decyzja

Pula procesów z limitami 16.2, `parse_latex` (backend `lark`), wynik trójwartościowy.

## Alternatywy

Brak odrębnego zapisu w rejestrze wersji 2.4 — decyzja nie miała rozpatrywanych alternatyw poza opisanymi w kontekście.

## Konsekwencje

SymPy jest sygnałem, nie werdyktem — nierównoważne przekształcenia bywają poprawne (np. przejście do zbioru rozwiązań).
