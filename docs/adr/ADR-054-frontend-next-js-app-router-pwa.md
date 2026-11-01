# ADR-054: Frontend Next.js — App Router, grupy tras per rola, PWA, bez BFF

| Pole | Wartość |
|---|---|
| Status | Przyjęta (wersja 3.0, 2026-11-01) |
| Data decyzji | 2026-11-01 (pętla 3.x, iteracja 1, wersja 3.0) |
| Bramka G-0 | nie |
| Realizuje | ADR-048 p. 4 (frontend React 18 + Vite zastąpiony) |
| Dokument główny | [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md) — 5.2, 6.8, 6.9, 16.1 |

## Status

Przyjęta.

## Kontekst

Wymagania: NFR-08 (przeglądarka na telefonie i komputerze, zdjęcie z
aplikacji), NFR-10 (interfejs po polsku), A-10 (dwie ostatnie wersje
głównych przeglądarek), ADR-020 (access token w pamięci, refresh token w
cookie `HttpOnly`), NFR-09 (brak danych osobowych w cache urządzenia), trzy
role z odrębnymi ekranami. Właściciel projektu wybrał Next.js (App Router,
TypeScript), a role wymagają osobnych układów i strażników.

## Decyzja

1. **Wersje:** Next.js 16 (App Router), React 19, TypeScript 5.9
   (`strict`, `noUncheckedIndexedAccess`), Node.js 24 LTS, `output:
   "standalone"`, obraz `lemat-frontend` (`node:24-bookworm-slim`).
2. **Bez BFF:** serwer Next.js nie wywołuje API w imieniu użytkownika i nie
   przechowuje tokenów. Przeglądarka komunikuje się z `/api/*` (ten sam
   origin przez Caddy, bez CORS); access token JWT jest w pamięci
   (`DostawcaSesji`), odświeżany przez `POST /api/v1/auth/odswiezenie` z
   cookie `lemat_rt` (`Path=/api/v1/auth`). ADR-020 obowiązuje bez zmian.
3. **Grupy tras:** `(publiczne)`, `(uczen)` (bez prefiksu URL),
   `(nauczyciel)/nauczyciel/*`, `(administrator)/admin/*` (6.8); każda
   grupa ma `layout.tsx` z nawigacją i strażnikiem roli. `src/proxy.ts`
   (Next.js 16; dawniej `middleware.ts`) przekierowuje na podstawie cookie
   `lemat_rola` (`HttpOnly; Secure; SameSite=Strict; Path=/`, wartość roli,
   ustawiane i usuwane przez endpointy `/auth/*`); cookie nie jest
   poświadczeniem, autoryzację wykonuje API.
4. **Renderowanie:** komponenty serwerowe dla układów, nawigacji, stron
   publicznych i tekstów; dane użytkownika wyłącznie w komponentach
   klienckich przez TanStack Query 5 (`staleTime` 0 dla danych Rozwiązania
   i Weryfikacji, 60 s dla danych referencyjnych: Działy, Umiejętności);
   brak buforowania odpowiedzi API przez Next.js.
5. **Kontrakt typów:** `openapi-typescript` 7 generuje
   `src/lib/api/schema.d.ts` z `backend/openapi/lemat-v1.json`;
   `openapi-fetch` jest jedynym klientem HTTP (`src/lib/api/klient.ts`,
   dodaje `Authorization`, `Idempotency-Key`, `If-Match`, mapuje
   ProblemDetails na błędy z polem `kod`); aliasy typów (3.5) w
   `src/lib/api/typy.ts`; formularze: react-hook-form 7 + zod 4 (reguły
   12.5 odwzorowane ręcznie; serwer pozostaje źródłem prawdy).
6. **PWA:** Serwist 9 (`@serwist/next`), `app/manifest.ts`,
   `app/sw.ts`: precache zasobów statycznych i fontów KaTeX,
   `NetworkOnly` dla `/api/*`, strona `/offline`; brak cache danych
   osobowych (NFR-09).
7. **i18n:** next-intl 4 bez routingu językowego (jedna lokalizacja
   `pl-PL`), `messages/pl-PL.json` z formami ICU `one`/`few`/`many`,
   formaty `Intl` `pl-PL`, strefa wyświetlania `Europe/Warsaw`; brak
   tekstów UI w kodzie komponentów (reguła ESLint `no-literal-string` dla
   JSX w `src/app` i `src/features`).
8. **LaTeX:** KaTeX 0.16 (`throwOnError: false`, `trust: false`,
   `maxExpand: 1000`, `strict: "ignore"`); Markdown Zadań i Wkładek:
   `react-markdown` 10 + `remark-math` 6 + `rehype-katex` 7 bez HTML
   (`skipHtml`); podgląd kroku Transkrypcji renderowany przy edycji
   (debounce 300 ms).
9. **Zdjęcia:** `<input type="file" accept="image/*" capture="environment"
   multiple>`; kompresja w Web Workerze (`src/lib/zdjecia/kompresja.worker.ts`:
   `createImageBitmap` z `imageOrientation: "from-image"` +
   `OffscreenCanvas.convertToBlob({ type: "image/jpeg", quality: 0.85 })`,
   dłuższy bok ≤ 2560 px); przeglądarka bez `OffscreenCanvas` — `<canvas>`
   w wątku głównym; plik, którego przeglądarka nie dekoduje (HEIC poza
   Safari), jest wysyłany bez kompresji (≤ 10 MB) i normalizowany na
   serwerze (ADR-052).
10. **SSE:** `src/lib/sse/strumien.ts` (`fetch` + `eventsource-parser` 3,
    `Last-Event-ID`, przejście na odpytywanie) — ADR-051.
11. **Jakość:** ESLint 9 + `eslint-config-next`, dependency-cruiser 16
    (granice 6.9), Prettier 3, Vitest 3 + Testing Library, Playwright
    (Chromium, WebKit z emulacją iPhone 15) w `frontend/e2e/` przeciw Docker
    Compose z modelami Mock.

## Alternatywy

- **BFF w Next.js (tokeny w sesji serwera Next.js, RSC z danymi)** —
  odrzucone: drugi serwer z poświadczeniami użytkowników (powierzchnia
  ataku, RODO), podwójny skok sieciowy dla każdego żądania, osobny model
  sesji niezgodny z ADR-020; zysk (RSC z danymi) nie ma znaczenia dla
  aplikacji za logowaniem.
- **Trasy z prefiksem roli dla Ucznia (`/uczen/*`)** — odrzucone: Uczeń
  generuje większość ruchu z telefonu; prefiks wydłuża adresy bez zysku,
  bo kolizje rozwiązują prefiksy Nauczyciela i Administratora.
- **Biblioteka PWA `next-pwa`** — odrzucona: nieutrzymywana; Serwist jest
  jej następcą dla App Router.
- **MathJax** — odrzucony: wolniejszy i cięższy niż KaTeX przy setkach
  wzorów na stronie Transkrypcji.
- **Ręcznie pisane typy DTO** — odrzucone: rozjazd z kontraktem C#.

## Konsekwencje

- Jeden kontrakt typów od rekordów C# do TypeScript (zmiana DTO bez
  ponownej generacji psuje budowę frontendu).
- Strony z danymi nie są indeksowane ani renderowane z danymi po stronie
  serwera — świadomie, bo aplikacja działa za logowaniem.
- Granice modułów frontendu (6.9) są egzekwowane w CI tak jak granice
  backendu (6.5).
