#!/usr/bin/env bash
# Weryfikacja backendu: restore, build -warnaserror, test, dotnet format (TASK-007).
# Testy integracyjne korzystaja z Testcontainers (wymagaja dzialajacego Dockera).
set -euo pipefail
cd "$(dirname "$0")/.."
SLN=backend/Lemat.slnx
echo "== backend: restore"
dotnet restore "$SLN"
echo "== backend: build -warnaserror"
dotnet build "$SLN" --no-restore -warnaserror
echo "== backend: test"
dotnet test "$SLN" --no-build
echo "== backend: format --verify-no-changes"
dotnet format "$SLN" --verify-no-changes
echo "backend: OK"
