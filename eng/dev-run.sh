#!/usr/bin/env bash
set -euo pipefail
: "${PLATFORM_DB_CONNECTION_STRING:?PLATFORM_DB_CONNECTION_STRING is required}"
export PLATFORM_LISTEN_URL="${PLATFORM_LISTEN_URL:-http://127.0.0.1:5080}"
exec dotnet run --project src/Lumio.Platform.App
