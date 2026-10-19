$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$env:DOTNET_CLI_HOME = Join-Path $root '.local/dotnet-home'
$env:APPDATA = Join-Path $root '.local/appdata'
$env:NUGET_PACKAGES = Join-Path $root '.local/nuget'
$pg = Join-Path $root '.local/pgsql/bin'
$data = Join-Path $root '.local/pgdata'
& "$pg/pg_ctl.exe" status -D $data
if ($LASTEXITCODE -ne 0) {
    Start-Process -FilePath "$pg/postgres.exe" -ArgumentList @('-D', ('"' + $data + '"')) -WindowStyle Hidden -RedirectStandardOutput "$root/.local/postgres.stdout.log" -RedirectStandardError "$root/.local/postgres.stderr.log"
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        & "$pg/pg_isready.exe" -h 127.0.0.1 -p 5432 | Out-Null
        if ($LASTEXITCODE -eq 0) { break }
        Start-Sleep -Seconds 1
    }
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL could not start.' }
}
$settings = Get-Content "$root/.local/settings.json" | ConvertFrom-Json
$env:ConnectionStrings__NotificationDb = $settings.ConnectionString
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --no-build --no-launch-profile --project "$root/src/NotificationService.Api" --urls http://127.0.0.1:5080
