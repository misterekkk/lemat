#!/usr/bin/env bash
# Pelna weryfikacja repozytorium: backend, potem frontend (TASK-007).
# Uzycie: scripts/verify-all.sh [--backend-only|--frontend-only]
set -euo pipefail
cd "$(dirname "$0")/.."
tryb="${1:-all}"
case "$tryb" in
  all|--backend-only|--frontend-only) ;;
  *) echo "Uzycie: $0 [--backend-only|--frontend-only]" >&2; exit 2 ;;
esac
[ "$tryb" = "--frontend-only" ] || scripts/verify-backend.sh
[ "$tryb" = "--backend-only" ] || scripts/verify-frontend.sh
echo "verify-all: OK"
