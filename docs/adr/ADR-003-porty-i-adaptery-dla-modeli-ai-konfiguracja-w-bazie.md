# ADR-003: Porty i adaptery dla modeli AI, konfiguracja w bazie

| Pole | Wartość |
|---|---|
| Status | Przyjęta; realizacja techniczna zaktualizowana przez ADR-048 (wersja 3.0) |
| Data decyzji | 2026-10-31 (iteracja 1, wersja 1.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Przyjęta; realizacja techniczna zaktualizowana przez ADR-048 (wersja 3.0).

**Realizacja w stosie .NET 10 + Next.js (wersja 3.0).** Decyzja obowiązuje. Porty są interfejsami C# w `Lemat.Application.Modele` (`IModelDecyzyjny`, `IModelGeneratywny`, `IRozpoznawaniePisma`, `ISprawdzanieSymboliczne`, `IPredyktorSukcesu`, `IOgranicznikSzybkosci`); adaptery i pakiety dostawców wyłącznie w projekcie `Lemat.Modele` (egzekwowane przez BannedApiAnalyzers i NetArchTest); modele generatywne za abstrakcją `IChatClient` (Microsoft.Extensions.AI), każdy dostawca ma implementację Mock — ADR-048, ADR-055.

## Kontekst

NFR-06, NFR-07, Jev we wczesnym dostępie.

## Decyzja

Porty z 13.1, rejestr adapterów, tabela `konfiguracja_modelu` z rolami; SDK dostawców tylko w `lemat.modele.adaptery`.

## Alternatywy

Bezpośrednie użycie SDK w agentach — odrzucone.

## Konsekwencje

Zmiana dostawcy bez zmian logiki; konieczna ewaluacja ZT-1 dla ról decyzyjnych.
