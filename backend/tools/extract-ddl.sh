#!/usr/bin/env bash
# Wycina bloki (A) i (B) z docs/spec/ddl.md (jedyne zrodlo schematu, ADR-063) do plikow SQL (TASK-013).
#   Persistence/Schema/ddl.sql                    - blok A + blok B (pelny schemat, do przegladu i porownan)
#   Persistence/Migrations/Sql/0000_schema.sql    - blok A (tabele, typy, ograniczenia, indeksy)
#   Persistence/Migrations/Sql/000N_<opis>.sql    - pliki bloku B, podzial po znacznikach "-- ===== 000N_<opis>.sql ====="
# Skrypt jest deterministyczny: te same wejscie => te same bajty (CI sprawdza brak roznic w gicie).
# Uzycie: backend/tools/extract-ddl.sh [--check]   (--check: nie zmienia repozytorium, konczy sie kodem 1 przy roznicach)
set -euo pipefail
export LC_ALL=C
KATALOG="$(cd "$(dirname "$0")/.." && pwd)"
ZRODLO="$KATALOG/../docs/spec/ddl.md"
DOCEL="$KATALOG/src/Lemat.Infrastructure/Persistence"
TRYB="${1:-}"

[ -f "$ZRODLO" ] || { echo "Brak pliku $ZRODLO" >&2; exit 1; }

if [ "$TRYB" = "--check" ]; then
  WYJSCIE="$(mktemp -d)"
else
  WYJSCIE="$DOCEL"
fi
mkdir -p "$WYJSCIE/Schema" "$WYJSCIE/Migrations/Sql"

# Wyciaga zawartosc pierwszego bloku ```sql po naglowku zaczynajacym sie od $1 (np. "## (A)").
wytnij_blok() {
  awk -v naglowek="$1" '
    index($0, naglowek) == 1 { szukam = 1; next }
    szukam && $0 == "```sql" { wewnatrz = 1; next }
    wewnatrz && $0 == "```" { exit }
    wewnatrz { print }
  ' "$ZRODLO"
}

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"; if [ "$TRYB" = "--check" ]; then rm -rf "$WYJSCIE"; fi' EXIT
wytnij_blok "## (A)" > "$TMP/a.sql"
wytnij_blok "## (B)" > "$TMP/b.sql"
[ -s "$TMP/a.sql" ] || { echo "Nie znaleziono bloku (A) w ddl.md" >&2; exit 1; }
[ -s "$TMP/b.sql" ] || { echo "Nie znaleziono bloku (B) w ddl.md" >&2; exit 1; }

cp "$TMP/a.sql" "$WYJSCIE/Migrations/Sql/0000_schema.sql"
cat "$TMP/a.sql" "$TMP/b.sql" > "$WYJSCIE/Schema/ddl.sql"

# Blok B: podzial na pliki po znacznikach "-- ===================== NNNN_nazwa.sql ..."
rm -f "$WYJSCIE"/Migrations/Sql/000[1-9]_*.sql
awk -v katalog="$WYJSCIE/Migrations/Sql" '
  match($0, /^-- =+ [0-9][0-9][0-9][0-9]_[a-z_]+\.sql/) {
    znacznik = substr($0, RSTART, RLENGTH)
    sub(/^-- =+ /, "", znacznik)
    plik = katalog "/" znacznik
  }
  plik != "" { print > plik }
' "$TMP/b.sql"

LICZBA=$(find "$WYJSCIE/Migrations/Sql" -name '*.sql' | wc -l)
[ "$LICZBA" -eq 8 ] || { echo "Oczekiwano 8 plikow SQL (0000..0007), jest $LICZBA" >&2; exit 1; }

if [ "$TRYB" = "--check" ]; then
  for f in $(cd "$WYJSCIE" && find Schema Migrations/Sql -type f | sort); do
    cmp -s "$WYJSCIE/$f" "$DOCEL/$f" || { echo "Roznica: $f (uruchom backend/tools/extract-ddl.sh)" >&2; exit 1; }
  done
  echo "extract-ddl: bez roznic"
else
  echo "extract-ddl: zapisano $LICZBA plikow SQL i Schema/ddl.sql"
fi
