param(
    [Parameter(Mandatory)][string]$PostgresBin,
    [Parameter(Mandatory)][string]$DataDirectory
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PostgresBin 'pg_ctl.exe') stop -D $DataDirectory -m fast -w
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL could not stop.' }
