# ADR-020: JWT krótkotrwały + rotowany refresh token w cookie

| Pole | Wartość |
|---|---|
| Status | Przyjęta; realizacja techniczna zaktualizowana przez ADR-048 (wersja 3.0); okno tolerancji rotacji — ADR-057 (wersja 3.1) |
| Data decyzji | 2026-10-31 (iteracja 1, wersja 1.0) |
| Bramka G-0 | nie |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — numery sekcji w treści (np. 13.2, 16.4) odnoszą się do tego dokumentu |

## Status

Przyjęta; realizacja techniczna zaktualizowana przez ADR-048 (wersja 3.0).

**Realizacja w stosie .NET 10 + Next.js (wersja 3.0).** Decyzja obowiązuje z jedną zmianą realizacji: podpis JWT ES256 (ECDSA P-256) zamiast EdDSA, bo biblioteki `Microsoft.IdentityModel.*` obsługują ES256 natywnie; argon2id przez `Konscious.Security.Cryptography.Argon2` z tymi samymi parametrami (m = 64 MiB, t = 3, p = 4) — ADR-048.

## Kontekst

PWA na telefonach, bezpieczeństwo sesji.

## Decyzja

16.1.

## Alternatywy

Sesje serwerowe w bazie (dodatkowe zapytanie na każde żądanie), długożyjące JWT (brak unieważniania).

## Konsekwencje

Unieważnienie dostępu w ≤ 15 min (blokada konta unieważnia refresh tokeny natychmiast).

## Uzupełnienia

**Uzupełnienie wersji 3.1 (C6-02, ADR-057).** Rotacja refresh tokenu ma
30-sekundowe okno tolerancji: ponowne użycie tokenu zastąpionego w ciągu
30 s od rotacji, z tym samym `skrot_klienta_http` i najwyżej 5 razy, zwraca
nowy access token bez `Set-Cookie` (kolumny `token_odswiezania.zastapiony_przez`,
`zastapiony_at`, `skrot_klienta_http`, `uzycia_w_oknie_tolerancji`);
poza oknem albo z innego klienta HTTP — unieważnienie rodziny jak dotąd.
Klient odświeża token w sekcji krytycznej Web Locks
(`lemat-odswiezenie`) i rozgłasza go kartom przez `BroadcastChannel`.
