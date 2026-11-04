#!/usr/bin/env bash
# Alternatywa dla Dockera (TASK-005): przygotowuje lokalny PostgreSQL 16 (domyslnie port 5432).
# Tworzy role: lemat (wlasciciel baz, CREATEDB, CREATEROLE, bez superuser) oraz lemat_api/lemat_worker/lemat_scheduler,
# bazy lemat_dev i lemat_test oraz rozszerzenia citext i pg_trgm (rozszerzenia "trusted" - wystarcza wlasciciel bazy).
# Wymaga dostepu administracyjnego: uruchom jako uzytkownik postgres albo ustaw PGADMIN (np. PGADMIN="sudo -u postgres").
# Idempotentny. Haslo deweloperskie z LEMAT_PG_HASLO (domyslnie lemat_dev).
# Potem: LEMAT_BAZA="Host=localhost;Port=5432;Database=lemat_dev;Username=lemat;Password=lemat_dev"
set -euo pipefail
PORT="${LEMAT_LOCAL_PG_PORT:-5432}"
HASLO="${LEMAT_PG_HASLO:-lemat_dev}"
if [ -n "${PGADMIN:-}" ]; then ADMIN=($PGADMIN psql); elif [ "$(id -un)" = "postgres" ]; then ADMIN=(psql); else ADMIN=(su postgres -c); fi

uruchom() { # $1 = baza, stdin = SQL
  local sql; sql=$(cat)
  if [ "${ADMIN[*]}" = "su postgres -c" ]; then
    su postgres -c "psql -p $PORT -v ON_ERROR_STOP=1 -d $1" <<<"$sql"
  else
    "${ADMIN[@]}" -p "$PORT" -v ON_ERROR_STOP=1 -d "$1" <<<"$sql"
  fi
}

uruchom postgres <<SQL
DO \$\$
DECLARE r text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'lemat') THEN
        CREATE ROLE lemat LOGIN CREATEDB CREATEROLE PASSWORD '$HASLO';
    END IF;
    FOREACH r IN ARRAY ARRAY['lemat_api','lemat_worker','lemat_scheduler'] LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = r) THEN
            EXECUTE format('CREATE ROLE %I LOGIN PASSWORD %L', r, '$HASLO');
        END IF;
    END LOOP;
END \$\$;
SQL

for baza in lemat_dev lemat_test; do
  istnieje=$(uruchom postgres <<<"SELECT 1 FROM pg_database WHERE datname='$baza'" | grep -c '^ *1$' || true)
  if [ "$istnieje" = "0" ]; then
    uruchom postgres <<<"CREATE DATABASE $baza OWNER lemat"
  fi
  uruchom "$baza" <<SQL
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
SQL
done
echo "Gotowe. LEMAT_BAZA=\"Host=localhost;Port=$PORT;Database=lemat_dev;Username=lemat;Password=$HASLO\""
