$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$env:DOTNET_CLI_HOME = "$root/.local/dotnet-home"
$env:APPDATA = "$root/.local/appdata"
$env:NUGET_PACKAGES = "$root/.local/nuget"
$env:ConnectionStrings__NotificationDb = (Get-Content "$root/.local/settings.json" | ConvertFrom-Json).ConnectionString
Push-Location $root
try {
    dotnet tool run dotnet-ef database update --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api
    if ($LASTEXITCODE -ne 0) { throw 'Migration failed.' }
} finally { Pop-Location }
