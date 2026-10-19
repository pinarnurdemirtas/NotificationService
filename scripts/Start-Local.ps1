param(
    [string]$PostgresBin,
    [string]$DataDirectory
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
# PostgreSQL may already be running as a service or from the original local installation.
# Optional arguments allow starting a portable installation before launching the API.
if ($PostgresBin -and $DataDirectory) {
    $pg = Join-Path $PostgresBin 'pg_ctl.exe'
    & $pg status -D $DataDirectory
    if ($LASTEXITCODE -ne 0) {
        Start-Process -FilePath (Join-Path $PostgresBin 'postgres.exe') -ArgumentList @('-D', ('"' + $DataDirectory + '"')) -WindowStyle Hidden
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            & (Join-Path $PostgresBin 'pg_isready.exe') -h 127.0.0.1 -p 5432 | Out-Null
            if ($LASTEXITCODE -eq 0) { break }
            Start-Sleep -Seconds 1
        }
        if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL could not start.' }
    }
} elseif ($PostgresBin -or $DataDirectory) {
    throw 'Supply both PostgresBin and DataDirectory.'
}
dotnet run --project "$root/src/NotificationService.Api" --launch-profile http
