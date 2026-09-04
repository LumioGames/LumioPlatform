$ErrorActionPreference = 'Stop'
docker compose up -d postgres
if ($LASTEXITCODE -eq 0) {
  for ($i = 0; $i -lt 30; $i++) { docker compose exec -T postgres pg_isready -U lumio *> $null; if ($LASTEXITCODE -eq 0) { break }; Start-Sleep -Seconds 1 }
  docker compose exec -T postgres psql -U lumio -d postgres -c 'CREATE DATABASE lumio_platform_test;' *> $null
} else {
  docker run -d --name lumio-platform-pg -e POSTGRES_USER=lumio -e POSTGRES_PASSWORD=lumio -e POSTGRES_DB=lumio_platform -p 5432:5432 postgres:17
  for ($i = 0; $i -lt 30; $i++) { docker exec lumio-platform-pg pg_isready -U lumio *> $null; if ($LASTEXITCODE -eq 0) { break }; Start-Sleep -Seconds 1 }
  docker exec lumio-platform-pg psql -U lumio -d postgres -c 'CREATE DATABASE lumio_platform_test;' *> $null
}
Write-Output "`$env:PLATFORM_DB_CONNECTION_STRING = 'Host=127.0.0.1;Port=5432;Database=lumio_platform;Username=lumio;Password=lumio'"
Write-Output "`$env:PLATFORM_TEST_DB_CONNECTION_STRING = 'Host=127.0.0.1;Port=5432;Database=lumio_platform_test;Username=lumio;Password=lumio'"
