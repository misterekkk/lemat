#!/usr/bin/env bash
# Uruchamia caly backend POC jednym poleceniem (TASK-130): PostgreSQL, migracja, seed-poc, Lemat.Api (5080) i Lemat.Worker w tle.
# Uzycie: scripts/dev-up.sh [--stop | --reset]
#   (bez opcji)  start; idempotentny - gdy procesy juz dzialaja, tylko wypisuje adresy
#   --stop       zatrzymuje Api i Worker (PID-y z .logs/); baza zostaje
#   --reset      zatrzymuje procesy i odtwarza pusta baze (Docker: usuwa wolumen; lokalny PostgreSQL: DROP/CREATE lemat_dev) oraz magazyn zdjec
# Baza: LEMAT_BAZA z .env/srodowiska; w przeciwnym razie PostgreSQL z docker-compose (db-up.sh, port LEMAT_PG_PORT, domyslnie 5433),
# a gdy Docker jest niedostepny - lokalny PostgreSQL na 5432 (scripts/db-local-setup.sh). Logi: .logs/api.log, .logs/worker.log (ignorowane przez git).
set -euo pipefail
cd "$(dirname "$0")/.."
ROOT="$PWD"
LOGI="$ROOT/.logs"
API_URL="http://localhost:5080"
TRYB="${1:-start}"

wczytaj_env() {
  if [ -f .env ]; then
    set -a
    # shellcheck disable=SC1091
    . ./.env
    set +a
  fi
}

zatrzymaj() {
  local zatrzymano=0
  for nazwa in api worker; do
    local plik="$LOGI/$nazwa.pid"
    if [ -f "$plik" ]; then
      local pid; pid=$(cat "$plik")
      if kill -0 "$pid" 2>/dev/null; then
        kill "$pid" 2>/dev/null || true
        for _ in $(seq 1 30); do kill -0 "$pid" 2>/dev/null || break; sleep 0.2; done
        kill -9 "$pid" 2>/dev/null || true
        zatrzymano=1
      fi
      rm -f "$plik"
    fi
  done
  [ "$zatrzymano" = 1 ] && echo "Zatrzymano procesy Api i Worker." || echo "Brak dzialajacych procesow Api/Worker."
  return 0
}

pg_lokalny_dziala() { (exec 3<>/dev/tcp/127.0.0.1/5432) 2>/dev/null; }

przygotuj_baze() {
  if [ -n "${LEMAT_BAZA:-}" ]; then
    echo "Baza: LEMAT_BAZA z srodowiska/.env."
    return
  fi
  if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
    [ "$TRYB" = "--reset" ] && docker compose down -v >/dev/null 2>&1 || true
    if scripts/db-up.sh; then
      LEMAT_BAZA="Host=localhost;Port=${LEMAT_PG_PORT:-5433};Database=lemat_dev;Username=${LEMAT_PG_UZYTKOWNIK:-lemat};Password=${LEMAT_PG_HASLO:-lemat_dev}"
      export LEMAT_BAZA
      return
    fi
    echo "Docker nie uruchomil PostgreSQL - probuje lokalny PostgreSQL na 5432." >&2
  fi
  if ! pg_lokalny_dziala; then
    pg_ctlcluster 16 main start 2>/dev/null || true
    sleep 2
  fi
  pg_lokalny_dziala || { echo "Brak PostgreSQL: uruchom Docker albo lokalny PostgreSQL 16 (port 5432) albo ustaw LEMAT_BAZA." >&2; exit 1; }
  local haslo="${LEMAT_PG_HASLO:-lemat_dev}"
  if [ "$TRYB" = "--reset" ]; then
    su postgres -c "psql -p 5432 -v ON_ERROR_STOP=1 -c 'DROP DATABASE IF EXISTS lemat_dev WITH (FORCE)'" >/dev/null
  fi
  scripts/db-local-setup.sh >/dev/null
  LEMAT_BAZA="Host=localhost;Port=5432;Database=lemat_dev;Username=lemat;Password=$haslo"
  export LEMAT_BAZA
}

czekaj_na_zdrowie() {
  for _ in $(seq 1 90); do
    if curl -fsS "$API_URL/api/v1/zdrowie" >/dev/null 2>&1; then return 0; fi
    sleep 1
  done
  echo "Api nie odpowiada na $API_URL/api/v1/zdrowie - zobacz .logs/api.log" >&2
  return 1
}

wypisz_podsumowanie() {
  cat <<TXT

Backend POC dziala:
  API      $API_URL/api/v1   (zdrowie: $API_URL/api/v1/zdrowie)
  Worker   http://localhost:8081/zdrowie
  Logi     $LOGI/api.log, $LOGI/worker.log
Konta demo (Uczen, haslo z docs/POC_CONTRACT.md 6.1):
  uczen1@lemat.local   Lemat-Demo-2026!
  uczen2@lemat.local   Lemat-Demo-2026!
Dalej: scripts/poc-smoke.sh   |   zatrzymanie: scripts/dev-up.sh --stop
TXT
}

mkdir -p "$LOGI"
case "$TRYB" in
  --stop) zatrzymaj; exit 0 ;;
  --reset|start) ;;
  *) echo "Nieznana opcja: $TRYB (dozwolone: --stop, --reset)" >&2; exit 2 ;;
esac

wczytaj_env
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

if [ "$TRYB" = "--reset" ]; then
  zatrzymaj
  rm -rf "$ROOT/storage/magazyn"
fi

if [ -f "$LOGI/api.pid" ] && kill -0 "$(cat "$LOGI/api.pid")" 2>/dev/null && curl -fsS "$API_URL/api/v1/zdrowie" >/dev/null 2>&1; then
  echo "Api juz dziala."
  wypisz_podsumowanie
  exit 0
fi
zatrzymaj >/dev/null

przygotuj_baze
export LEMAT_MAGAZYN_SCIEZKA="${LEMAT_MAGAZYN_SCIEZKA:-$ROOT/storage/magazyn}"
mkdir -p "$LEMAT_MAGAZYN_SCIEZKA"

echo "== build"
: >"$LOGI/build.log"
for projekt in Lemat.Tools Lemat.Worker Lemat.Api; do
  dotnet build "backend/src/$projekt" -nologo -v q >>"$LOGI/build.log" 2>&1 || { tail -30 "$LOGI/build.log" >&2; exit 1; }
done
echo "== migrate"
dotnet backend/src/Lemat.Tools/bin/Debug/net10.0/Lemat.Tools.dll migrate
echo "== seed-poc"
dotnet backend/src/Lemat.Tools/bin/Debug/net10.0/Lemat.Tools.dll seed-poc

uruchom() { # $1 = nazwa, $2 = projekt (katalog projektu = content root dla appsettings); $! to PID samego dotnet (setsid bez forka)
  pushd "backend/src/$2" >/dev/null
  setsid nohup dotnet "bin/Debug/net10.0/$2.dll" >"$LOGI/$1.log" 2>&1 </dev/null &
  echo $! >"$LOGI/$1.pid"
  popd >/dev/null
}
echo "== start Api i Worker"
uruchom api Lemat.Api
uruchom worker Lemat.Worker
czekaj_na_zdrowie || { zatrzymaj; exit 1; }
sleep 1
kill -0 "$(cat "$LOGI/worker.pid")" 2>/dev/null || { echo "Worker zakonczyl dzialanie - zobacz .logs/worker.log" >&2; zatrzymaj; exit 1; }
wypisz_podsumowanie
