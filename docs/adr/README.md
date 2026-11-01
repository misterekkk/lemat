# Architecture Decision Records

Każda decyzja architektoniczna projektu Lemat jest osobnym plikiem
`ADR-NNN-<slug>.md` (od wersji 3.4, ADR-075: slug to angielskie
tłumaczenie tytułu, małe litery ASCII, łączniki, ≤ 60 znaków; tytuł i
treść ADR pozostają po polsku). Sekcje pliku: tabela metadanych (Status, Data
decyzji, Bramka G-0, powiązania), **Status**, **Kontekst**, **Decyzja**,
**Alternatywy**, **Konsekwencje** i opcjonalnie **Uzupełnienia** (zmiany w
kolejnych iteracjach, z numerem uwagi krytyka). Statusy: `Przyjęta`,
`Zmieniona (it. N)`, `Nowa (it. N)`, `Zastąpiony przez ADR-NNN` (plik
wyjaśnia, która treść domenowa przechodzi do decyzji zastępującej) oraz
dopisek „realizacja techniczna zaktualizowana przez ADR-NNN”.

Indeks wszystkich ADR (ID, tytuł, status, G-0, link) jest w sekcji 19
dokumentu [`docs/ARCHITECTURE_DETAILED.md`](../ARCHITECTURE_DETAILED.md).
ADR-001 … ADR-047 przeniesiono z sekcji 19 wersji 2.4 bez skrótów; ADR-048
… ADR-055 dotyczą stosu .NET 10 + Next.js (wersja 3.0).
ADR-056 … ADR-059 doprecyzowują realizację .NET (wersja 3.1), a ADR-060 …
ADR-066 — kontrakty REST/SSE, błędy, schemat i odporność prac w tle
(wersja 3.2).
ADR-067 … ADR-074 (wersja 3.3) zamykają uwagi C8: wiązanie formularzy i
polityka nazw JSON, idempotencja w transakcji skutku, wersja w ciele zamiast
warunkowego `GET`, limity dla klas za NAT, autoryzacja obiektowa, `PATCH`
jako JSON Merge Patch, autozapis Poprawek jako szkic roboczy oraz zmiana i
wycofanie Przypisania.
ADR-075 (wersja 3.4) wprowadza angielskie nazwy plików i katalogów w całym
repozytorium (w tym nazwy plików ADR) przy niezmienionych identyfikatorach
ze słownika.
