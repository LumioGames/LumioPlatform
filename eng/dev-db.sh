#!/usr/bin/env bash
set -euo pipefail
if docker compose up -d postgres; then
  for i in {1..30}; do docker compose exec -T postgres pg_isready -U lumio >/dev/null 2>&1 && break; sleep 1; done
  docker compose exec -T postgres psql -U lumio -d postgres -c 'CREATE DATABASE lumio_platform_test;' 2>/dev/null || true
else
  docker run -d --name lumio-platform-pg -e POSTGRES_USER=lumio -e POSTGRES_PASSWORD=lumio -e POSTGRES_DB=lumio_platform -p 5432:5432 postgres:17
  for i in {1..30}; do docker exec lumio-platform-pg pg_isready -U lumio >/dev/null 2>&1 && break; sleep 1; done
  docker exec lumio-platform-pg psql -U lumio -d postgres -c 'CREATE DATABASE lumio_platform_test;' 2>/dev/null || true
fi
echo "export PLATFORM_DB_CONNECTION_STRING='Host=127.0.0.1;Port=5432;Database=lumio_platform;Username=lumio;Password=lumio'"
echo "export PLATFORM_TEST_DB_CONNECTION_STRING='Host=127.0.0.1;Port=5432;Database=lumio_platform_test;Username=lumio;Password=lumio'"
