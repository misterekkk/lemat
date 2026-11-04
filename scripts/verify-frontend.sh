#!/usr/bin/env bash
# Weryfikacja frontendu: ci, typecheck, lint, test, build, check:headers (TASK-007).
set -euo pipefail
cd "$(dirname "$0")/../frontend"
echo "== frontend: npm ci"
npm ci
echo "== frontend: typecheck"
npm run typecheck
echo "== frontend: lint"
npm run lint
echo "== frontend: test"
npm test
echo "== frontend: build"
npm run build
echo "== frontend: check:headers"
npm run check:headers
echo "frontend: OK"
