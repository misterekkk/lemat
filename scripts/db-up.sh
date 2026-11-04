#!/usr/bin/env bash
# Uruchamia PostgreSQL 16 z docker-compose i czeka na stan healthy (TASK-005).
set -euo pipefail
cd "$(dirname "$0")/.."
[ -f .env ] || cp .env.example .env
docker compose up -d postgres
for _ in $(seq 1 60); do
  stan=$(docker inspect -f '{{.State.Health.Status}}' lemat-postgres 2>/dev/null || echo brak)
  if [ "$stan" = "healthy" ]; then
    echo "PostgreSQL gotowy: port ${LEMAT_PG_PORT:-$(grep -E '^LEMAT_PG_PORT=' .env | cut -d= -f2)}"
    exit 0
  fi
  sleep 2
done
echo "PostgreSQL nie osiagnal stanu healthy" >&2
exit 1
