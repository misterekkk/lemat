#!/bin/bash
# Inicjalizacja przy pierwszym starcie kontenera (docker-entrypoint-initdb.d).
# Tworzy baze lemat_test, role aplikacji z LOGIN (ddl.md 0005: infrastruktura tworzy je przed migracja)
# oraz rozszerzenia citext i pg_trgm. Haslo wylacznie deweloperskie (z LEMAT_PG_HASLO).
set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<SQL
CREATE DATABASE lemat_test OWNER "$POSTGRES_USER";
DO \$\$
DECLARE r text;
BEGIN
    FOREACH r IN ARRAY ARRAY['lemat_api','lemat_worker','lemat_scheduler'] LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = r) THEN
            EXECUTE format('CREATE ROLE %I LOGIN PASSWORD %L', r, '${LEMAT_PG_HASLO:-lemat_dev}');
        END IF;
    END LOOP;
END \$\$;
SQL

for baza in lemat_dev lemat_test; do
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$baza" <<SQL
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
SQL
done
