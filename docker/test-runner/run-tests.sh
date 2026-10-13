#!/bin/sh
# Единая точка входа контейнера-раннера. Первый аргумент - этап (ТИОПО ЛР2, Т5):
#   unit        - TripSplit.BusinessLogic.Tests + TripSplit.DataAccess.Tests (Category!=Integration)
#   integration - TripSplit.DataAccess.Tests (Category=Integration), против стенда этой ЛР
#   e2e         - TripSplit.E2E.Tests, против WebUI стенда этой ЛР
# Результаты (TRX) пишутся в /results, чтобы их мог забрать CI даже при падении этапа (Т8).
set -eu

STAGE="${1:?Usage: run-tests.sh <unit|integration|e2e>}"
RESULTS_DIR="${RESULTS_DIR:-/results}"
mkdir -p "$RESULTS_DIR"

cd /repo/src

case "$STAGE" in
  unit)
    dotnet test TripSplit.BusinessLogic.Tests/TripSplit.BusinessLogic.Tests.csproj \
      --logger "trx;LogFileName=unit-businesslogic.trx" --results-directory "$RESULTS_DIR"
    dotnet test TripSplit.DataAccess.Tests/TripSplit.DataAccess.Tests.csproj \
      --filter "Category!=Integration" \
      --logger "trx;LogFileName=unit-dataaccess.trx" --results-directory "$RESULTS_DIR"
    ;;
  integration)
    dotnet test TripSplit.DataAccess.Tests/TripSplit.DataAccess.Tests.csproj \
      --filter "Category=Integration" \
      --logger "trx;LogFileName=integration.trx" --results-directory "$RESULTS_DIR"
    ;;
  e2e)
    dotnet test TripSplit.E2E.Tests/TripSplit.E2E.Tests.csproj \
      --logger "trx;LogFileName=e2e.trx" --results-directory "$RESULTS_DIR"
    ;;
  *)
    echo "Unknown stage '$STAGE' (expected: unit | integration | e2e)" >&2
    exit 2
    ;;
esac
