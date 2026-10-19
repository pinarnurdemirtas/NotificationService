$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'EF tool restore failed.' }
    dotnet tool run dotnet-ef database update --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api -- --environment Development
    if ($LASTEXITCODE -ne 0) { throw 'Migration failed.' }
} finally { Pop-Location }
