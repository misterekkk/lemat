#!/usr/bin/env bash
# Odtwarza pusta baze: usuwa kontener i wolumen, po czym uruchamia od nowa (TASK-005).
# UWAGA: kasuje wszystkie dane lokalnej bazy w Dockerze.
set -euo pipefail
cd "$(dirname "$0")/.."
docker compose down -v
exec scripts/db-up.sh
