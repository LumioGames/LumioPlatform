$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($env:PLATFORM_DB_CONNECTION_STRING)) { throw 'PLATFORM_DB_CONNECTION_STRING is required' }
if ([string]::IsNullOrWhiteSpace($env:PLATFORM_LISTEN_URL)) { $env:PLATFORM_LISTEN_URL = 'http://127.0.0.1:5080' }
dotnet run --project src/Lumio.Platform.App
